> **AWS Detailed Guide** · [Index](README.md) · Part I

# Serverless & Lambda

> **Tier 1 — bulletproof.** Lambda is named on my resume *and* backs a delivery bullet (scheduled log-maintenance and health-check jobs across 12 platform services, 99.9% uptime). Expect the deepest questioning here. See [Resume Deep-Dives](00-resume-aligned-priority-map.md#resume-deep-dives--the-follow-ups-i-should-expect) for the follow-ups that bullet invites.

---

## 1. Lambda Core

### AWS Lambda Deep Dive

**What it is:** Serverless compute — upload code, AWS runs it on trigger, you pay per invocation + duration. No server/patch/scale management.

**Key characteristics**
- Event-driven (API Gateway, S3, SNS, DynamoDB Streams, SQS, EventBridge, etc.)
- Fully managed, auto-scales per concurrent request
- Billed per invocation count + duration(ms) × memory
- Runtimes: Node.js, Python, .NET 6/8 (AOT + arm64), Java, Go, custom runtime via Lambda Runtime API (Rust etc.)

**Real-world use cases**
| Category | Example |
|---|---|
| API backend | API Gateway + Lambda for low/medium traffic APIs |
| Event processing | S3 upload → resize/scan; DynamoDB Streams → audit log; Kinesis/Kafka → real-time |
| Automation/cron | EventBridge schedule → cleanup Lambda |
| Orchestration glue | Step Functions + Lambda for multi-service workflows |
| Data transformation | Small/medium ETL, CSV→JSON |
| Edge | Lambda@Edge for CloudFront customization |

**Execution model — Firecracker micro-VM lifecycle**

```
        first invocation / scale-out / redeploy
                        |
                        v
              +-------------------+
              |       INIT        |  runtime + global init loaded
              +---------+---------+
                        |
                        v
              +-------------------+
      +------>|      INVOKE       |  your handler runs
      |       +---------+---------+
      |                 | handler returns
      | warm reuse      v
      | (never          +-------------------+
      |  guaranteed)    |      FREEZE       |
      +-----------------+---------+---------+
                        |
                        | idle timeout / platform update
                        v
              +-------------------+
              |     SHUTDOWN      |  environment destroyed
              +-------------------+
```

1. **INIT (cold start):** new micro-VM, runtime bootstrap, static initializers, DI container build, DB connection setup — all in your "outside the handler" code.
2. **INVOKE:** handler runs with event + context; must finish inside configured timeout (max 15 min).
3. **FREEZE (warm reuse):** environment frozen after response; globals, connections, `/tmp` persist — this is why warm invocations are 1–10ms.
4. **SHUTDOWN:** AWS reclaims idle/outdated environments. No shutdown hook — state is lost, not rolled back.

**INIT decomposed: "runtime bootstrap" vs "static initializers"** — the two halves of step 1: one is the platform's, one is mine.

| Step | What happens | Owner |
|---|---|---|
| Micro-VM | Firecracker VM created, deployment package downloaded and mounted | AWS |
| Runtime load | For .NET: **CoreCLR** starts — GC heap reserved, thread pool spun up, JIT initialised, `AssemblyLoadContext.Default` created | .NET |
| Runtime client | `Amazon.Lambda.RuntimeSupport` begins polling the **Runtime API** (`/runtime/invocation/next`) — this loop is what makes a process a Lambda | AWS runtime |
| Assembly load | My DLL loaded, handler type and method resolved by reflection (unless Native AOT) | .NET |
| **Static initializers** | My `static` fields and static constructors — the CLR runs them **once per type per load context**, thread-safely, before first access. Everything outside the handler is paid here; everything inside is paid per invocation | Mine |

**One-line answer:** "Runtime bootstrap is the platform's half of INIT — micro-VM, CoreCLR, JIT, the runtime client's poll loop — and it's why .NET cold starts cost 300–1500ms where Node costs 50–200. Static initializers are my half: the CLR runs them once per type per load context, thread-safely, before first access, so anything outside the handler is paid once per environment and reused on warm invokes. The trap is that a static initializer that throws poisons the type for the entire life of that environment."

**Cold start vs warm start**
- Cold: Java/.NET (JIT) → 300–1500ms; Node/Python → 50–200ms; **.NET Native AOT** → 50–100ms (huge win over JIT-based .NET).
- Warm: 1–10ms — reused environment, init code already executed.
- Mitigations: .NET AOT, small deployment package, avoid heavy DI container graphs, avoid VPC unless required, use Provisioned Concurrency for latency-sensitive APIs.

**Triggers**
| Type | Examples | Failure semantics |
|---|---|---|
| Synchronous | API Gateway, ALB, Step Functions, direct Invoke | Caller sees the error; caller must retry |
| Asynchronous | S3, SNS, CloudWatch Events, SES, EventBridge | Lambda retries automatically; DLQ on exhaustion |
| Poll-based (event source mapping) | DynamoDB Streams, Kinesis, MSK, SQS | Lambda polls internally; retries until success or maxReceiveCount/DLQ |

**Pros**
- Zero server management, auto-scale, pay-per-use, multi-AZ by default, deep AWS integration, trivial deployment (zip/image).

**Cons / when NOT to use**
- Cold start latency unsuitable for <50ms SLA APIs at the tail
- 15-minute hard timeout — no long-running jobs
- Vendor lock-in to AWS event model
- Max 10GB memory, 10GB ephemeral `/tmp` — not for ML training/huge batch
- Harder observability across distributed functions (need X-Ray + structured logging)

**Cost model:** invocations + GB-seconds (duration × memory) + optional Provisioned Concurrency hourly charge + data transfer if outside AWS network.

**[new content] .NET-specific Lambda considerations**
- Prefer **.NET 8 Native AOT** for latency-sensitive Lambdas — eliminates JIT warm-up, smaller deployment package, but: no runtime reflection-based DI magic (source-generated JSON serialization required via `System.Text.Json` source generators), some third-party libraries using reflection may break under trimming.
- `Amazon.Lambda.AspNetCoreServer` lets you host a full minimal API/ASP.NET Core app behind API Gateway/ALB with almost no code changes — useful for lift-and-shift of existing Web APIs, at the cost of a heavier cold start than a pure function handler.
- Avoid building a full `IServiceCollection`/`IServiceProvider` graph per cold start if you don't need it — construct dependencies manually or cache the `IServiceProvider` as a static field so it survives across warm invocations.
- EF Core inside Lambda: create the `DbContext`/connection pool **outside** the handler (static/singleton) to reuse across warm invocations; be careful with RDS Proxy or connection pool exhaustion at high concurrency (each concurrent execution environment = its own connection footprint).

**Versions, aliases & layers — the deployment surface** (needed for any "how do you deploy a Lambda safely?" question):
- **`$LATEST`** is mutable; **publishing a version** creates an immutable, numbered snapshot of code + configuration.
- An **alias** is a named, movable pointer to a version (`prod` → v7). Aliases support **weighted routing** across two versions, which is how you do a **canary/linear deploy** — shift 10% of traffic to v8, watch CloudWatch alarms, then complete or roll back by moving the alias. **CodeDeploy** automates exactly this with `Canary10Percent5Minutes`-style configs and automatic alarm-triggered rollback.
- **Layers** package shared dependencies or the **Lambda Extensions** used by agents (CloudWatch Lambda Insights, Parameters & Secrets extension) separately from function code — smaller deployment packages and one place to update a shared library. Limit: 5 layers per function, 250 MB unzipped total.
- **Destinations** route the *result* of an **asynchronous** invocation — `onSuccess` and `onFailure` — to SQS/SNS/EventBridge/Lambda. This is strictly better than a bare DLQ because the record includes the **response/error payload and the request context**, not just the original event.
- **`RetryAttempts`** for async invokes defaults to **2** (so up to 3 total attempts) with an event age limit — worth knowing before you conclude "Lambda silently dropped my event".
- **Reserved concurrency** caps *and* guarantees a function's share; **provisioned concurrency** pre-initialises environments to remove cold starts. **SnapStart** is the cold-start fix for **Java only** — for .NET the equivalent levers are **Native AOT**, trimming, and provisioned concurrency (see the .NET considerations above).

**Interview-ready lifecycle answer:** "Each Lambda invocation runs in a Firecracker micro-VM. On cold start AWS provisions the VM, loads the runtime and executes global/static initializers before the handler runs. After the handler returns, AWS may freeze and reuse that environment for a subsequent invocation (warm start) — this reuse is an optimization, not a guarantee. Idle or replaced environments are torn down with no shutdown hook, so any unflushed state is lost."

### Lambda Concurrency Model

**Reserved vs Provisioned Concurrency**

| | Reserved Concurrency | Provisioned Concurrency |
|---|---|---|
| Purpose | Guarantee + cap capacity for one function | Eliminate cold starts |
| Mechanism | Carves out part of account/region concurrency pool | Pre-initializes N warm environments |
| Cost | None extra | Billed hourly regardless of invocation |
| Effect above limit | Throttled | Falls back to normal (cold) scaling |

One-liner: **Reserved = guarantee capacity. Provisioned = eliminate cold starts.**

**Burst scaling rules**
- Default regional concurrency limit: 1,000 concurrent executions (increasable).
- Burst behavior: first ~1,000 concurrent executions scale instantly; beyond that, +500 new environments/minute until the account/region limit is hit.
- Concurrency limit = **how far** you can scale; burst rate = **how fast**.

**Multi-region concurrency**
- Each region has an independent concurrency pool and independent burst behavior.
- Active-active: split traffic and provision reserved/provisioned concurrency separately per region.
- Active-passive (DR): pre-raise the concurrency limit in the DR region *before* failover — a cold DR region with default limits will throttle under failover load.

**Sizing formula**

```
Required Concurrency ≈ Peak RPS × Avg Duration (seconds) × Safety Factor (1.3–2.0)
```

Example: 500 msgs/sec × 1.2s duration ≈ 600 concurrency (before safety factor).

- Latency-critical APIs: Reserved ≈ required concurrency; Provisioned ≈ p95 load.
- SQS-driven async Lambdas: you can *cap* concurrency below theoretical peak — SQS absorbs the backlog as back-pressure, protecting downstream DBs.

**Interview traps and correct answers (condensed)**
| Trap question | Correct senior answer |
|---|---|
| Why did Lambda run faster the 2nd time? | Warm start — execution environment reused; not caching of "logic". |
| Why did it time out despite fast code? | Timeout is wall-clock, includes network waits (DB, downstream API), not CPU-only. |
| Why did SQS message get reprocessed? | Message is deleted only after successful execution; failures make it visible again after the visibility timeout. |
| Lambda crashed after a DB write — rollback? | No transaction awareness. Partial writes persist; idempotency required. |
| Can Lambda guarantee exactly-once? | No — at-least-once only. Idempotency is mandatory, not optional. |
| Why is Lambda slower in a VPC? | ENI attachment adds cold-start latency (mitigated significantly since the 2019 Hyperplane ENI improvements, but still non-zero, especially for infrequently-invoked functions). |
| Should Lambda hold business logic? | No — "fat Lambda" anti-pattern; orchestrate/validate/delegate to testable services/libraries. |
| Can Lambda run forever? | No — 15-minute hard cap; use Step Functions/ECS/Batch for longer work. |

**ENI vs VPC Endpoint (mental model)**
- **ENI**: network interface Lambda attaches when placed inside a VPC — required to reach private resources (RDS, internal ALB). Adds cold-start overhead.
- **VPC Endpoint** (Gateway for S3/DynamoDB, Interface/PrivateLink for most others): lets a VPC-bound Lambda reach AWS services *without* NAT/internet — lower latency, lower cost, no public exposure.
- Rule: use ENI only when you must reach private VPC resources; use VPC Endpoints to avoid NAT costs/latency once you're in a VPC anyway.

---

## 2. Choosing Compute

### Lambda vs ECS vs Fargate

```
                  What is the workload shape?
                              |
        +---------------------+---------------------+
        |                                           |
 event-driven, spiky,                    long-running, steady,
 short-lived (< 15 min)                  container-based
        |                                           |
        v                                           v
     LAMBDA                        Do I need control of the host?
                                   (GPU, custom kernel/AMI,
                                    daemons, Spot/RI tuning)
                                                |
                                    +-----------+-----------+
                                   yes                      no
                                    |                       |
                                    v                       v
                          ECS on EC2 launch type    ECS / EKS on FARGATE
                          (capacity is mine)        (capacity is AWS's)
```

| Dimension | Lambda | ECS on EC2 launch type | ECS/EKS on Fargate |
|---|---|---|---|
| Unit of deployment | Function (zip or image) | Container task on my instances | Container task, no instances |
| Capacity management | None | Mine — ASG, AMIs, patching, bin-packing | None (AWS) |
| Max run time | 15 min | Unbounded | Unbounded |
| Start-up cost | Cold start (ms–s) | None once instances are warm; slow when the cluster must grow | Task start ~30–60s (image pull + ENI attach) |
| Scaling speed | Seconds, per request | Slowest — a new instance must boot and join the cluster | Middle — per task, no instance boot |
| Pricing | Per invocation + GB-second | Per instance-hour regardless of task density; Spot/RI/Savings Plans apply | Per task vCPU/GB-second while running |
| Host access | None | Full — SSH, daemons, GPU, custom kernel | None — no host, no privileged mode, no daemonsets, no GPU |
| Best for | Spiky/event-driven, glue code | GPU, custom AMI, high steady density, tight cost tuning | Predictable microservices without capacity ops |

**Key senior talking points**
- **ECS is the orchestrator; Fargate is a capacity provider for it** (and for EKS) — they are not competing products. The real comparison is ECS *on the EC2 launch type* vs ECS *on Fargate*, and treating "ECS vs Fargate" as two rival products is a common slip.
- **The deciding axis is who owns the capacity** — "capacity" meaning the actual EC2 instances the containers run on. On Lambda there is no host I can see at all; on Fargate a host exists but it is AWS's; on the EC2 launch type the instances sit in my own VPC and ASG, where I can SSH to them. Owning them is what makes GPU instance types, a custom or hardened AMI, per-host agents (ECS `DAEMON` strategy, EKS DaemonSets) and bin-packing with Spot/RI pricing possible **at all** — none of those are choices Fargate exposes. The price of owning them is AMI patching, slower scaling, and paying for idle instances.
- Lambda "optimizes for speed, scale, minimal ops"; containers "optimize for control, predictability, long-running work." The right choice is workload-shape-driven, not preference-driven.
- Both Lambda and ECS/Fargate can be triggered by / consume from SQS.
- **Cost inversion:** Lambda is cheapest at low/spiky traffic, Fargate sits in the middle, and the EC2 launch type wins at steady high density — you pay for instances rather than tasks, and can layer Spot and Savings Plans on top.
- **Scaling speed ranks the same way capacity does:** Lambda in seconds, Fargate per task, EC2 launch type slowest because a new instance must boot, attach storage, and register with the cluster.
- **IAM differs:** Lambda uses an *execution role*; ECS tasks use a *task role* (app permissions) plus a separate *task execution role* (pulling images, writing logs) — conflating the two ECS roles is a classic trap.

---

## 3. Event-Driven Patterns

### Serverless & the S3 → Lambda Trigger Pattern

**What "serverless" means:** no servers to provision or patch, automatic scaling from zero, **pay only for what you use**, and high availability built in. The AWS serverless set worth listing: **Lambda**, **Fargate**, **S3**, **DynamoDB**, **SQS/SNS/EventBridge**, **API Gateway**, **Step Functions**, and **Aurora Serverless v2**.

**The canonical S3-triggers-Lambda flow** (thumbnail generation, virus scanning, CSV ingestion, document processing):
1. Configure an **S3 event notification** on `s3:ObjectCreated:*`, optionally filtered by prefix and suffix (`uploads/`, `.csv`).
2. S3 invokes the Lambda with an event containing the **bucket name and object key** — not the object itself, so the function fetches it with `GetObject`.
3. The function needs a **resource-based policy** allowing `s3.amazonaws.com` to invoke it (the console adds this automatically), and its **execution role** needs `s3:GetObject` on the source and `s3:PutObject` on the destination.

```json
{
  "Effect": "Allow",
  "Principal": { "Service": "s3.amazonaws.com" },
  "Action": "lambda:InvokeFunction",
  "Resource": "arn:aws:lambda:us-east-1:123456789012:function:process-upload",
  "Condition": {
    "StringEquals": { "AWS:SourceAccount": "123456789012" },
    "ArnLike":      { "AWS:SourceArn": "arn:aws:s3:::my-upload-bucket" }
  }
}
```
`SourceAccount`/`SourceArn` are the **confused-deputy** guard for service principals — see [IAM Roles](03-iam-security.md#iam-roles-policies-assumerole).

**Two gotchas that are practically guaranteed to be asked:**
- **❗ Infinite recursion.** If the function writes its output **back into the same bucket** under a path the trigger still matches, each write fires the function again — an unbounded invocation loop with a matching bill. Fix by writing to a **different bucket**, or by using a prefix filter that cannot match the output (`uploads/` in, `processed/` out). AWS now has recursive-invocation *detection* that halts the loop, but the architecture fix is yours.
- **At-least-once delivery.** S3 event notifications can be delivered **more than once** and occasionally out of order, so the handler must be **idempotent** — key the work on the object key + ETag/version ID, and make re-processing harmless.

**When to route through EventBridge instead:** enabling **EventBridge notifications** on the bucket gives you content-based filtering, **multiple targets** for one event, retries with a DLQ, and archive/replay — versus S3's one-destination-per-event-type native notifications. For anything beyond a single simple trigger, EventBridge is the better answer (see [EventBridge Deep Dive](13-messaging-streaming.md#eventbridge-deep-dive)).

---

## 4. Resume Follow-Ups

### Resume Follow-Ups — The "Scheduled Lambda Jobs, 99.9% Uptime" Bullet

> *"Build scheduled AWS Lambda jobs for automated log maintenance and health checks, sustaining 99.9% uptime across 12 platform services."*

Expect: *how are they scheduled? what does a health check actually check? how does that produce 99.9%?* The consolidated list of every resume follow-up is in [Resume Deep-Dives](00-resume-aligned-priority-map.md#resume-deep-dives--the-follow-ups-i-should-expect); these are the Lambda-specific parts.

- **Scheduling** — **EventBridge scheduled rules** (`cron(0 2 * * ? *)` / `rate(5 minutes)`), or **EventBridge Scheduler** for one-time schedules, time zones, and built-in retry/DLQ. Say EventBridge, not "CloudWatch Events" — same service, current name.
- **The health-check function itself** — a Lambda probing service endpoints and publishing a **custom CloudWatch metric** (`PutMetricData`, or cheaper via **EMF**). Be able to place it against the managed alternatives: **Synthetics canaries** (same idea, managed), **Route 53 health checks** (DNS layer), **ALB target-group checks** (load-balancer layer).
- **12 functions vs one function with 12 schedules** — either is defensible, but have a reason: blast radius and per-service IAM scoping on one side, fewer cold starts and a single deploy artifact on the other.
- **Trap: *"what if the health-check Lambda itself fails?"*** → the monitor needs monitoring. Alarm on the function's `Errors`/`Throttles` **and** on **missing data** (`treat-missing-data: breaching`), because a monitor that silently stops running looks identical to "everything is fine."
- **Justifying the 99.9%** — that's an error budget of roughly **43 minutes of downtime per month** (`30 × 24 × 60 × 0.001`, where `0.001` is the `1 − SLA` failure fraction). The number has to come from a measurement, not a feeling:

  > "It's a measured number, not a guess. **Denominator:** our flow took roughly 420,000 deal-export requests a month. **Definition of success:** the payload reached the downstream DMS and came back `200 OK` with a valid ack inside 30 seconds — everything else counted as a failure, including 5xx, timeouts, and malformed-payload rejects. **The arithmetic:** that month `total = 421,538` and `failed = 388`, so `(421538 − 388) / 421538 = 99.908%` — that's why I say 99.9%.
  >
  > And we managed against it: in any month that burned more than 50% of the error budget, we held feature releases and did reliability work first. One clarification — that's **observed** availability, not a contractual SLA; our committed SLA was 99.5% and we delivered better than it. And it's our service only. End-to-end, including the dealer's own DMS, it sat closer to 99.5%, because a number of dealer systems go down overnight for maintenance."

  *Observed vs committed* and *our service vs end-to-end* are the real signal — they show the number was owned rather than over-claimed. The instrumentation that produces it lives in [Measuring the 99.9% Claim](07-observability-monitoring.md#resume-follow-ups--measuring-the-999-claim).

**Disaster Recovery — Lambda & Serverless**

| | |
|---|---|
| **What's actually at risk** | Nothing durable lives *in* Lambda — code is in Git, config in IaC. What you actually lose is **in-flight events** and the event-source wiring |
| **Backup mechanism** | Git + built artifact in S3/ECR; **versions and aliases** are the rollback mechanism; **DLQ / on-failure destination** is the only backup of the events themselves |
| **Realistic RPO / RTO** | Code RPO ~0 (Git). RTO minutes — an IaC apply. Event RPO = whatever the DLQ caught |

**Recovery runbook:**
1. **Bad deploy:** don't redeploy — repoint the alias at the previous version. `aws lambda update-alias --name prod --function-version 41`. Instant, and it's why you deploy through an alias rather than `$LATEST`.
2. **Regional failure:** `terraform apply` the same module against the DR region, then recreate event source mappings (`aws lambda create-event-source-mapping`) — ESMs are regional and are *not* part of the function.
3. **Re-drive lost events:** SQS DLQ has native redrive (`aws sqs start-message-move-task`); for an async DLQ, a small re-invoke consumer.
4. Reset **provisioned concurrency** in the DR region — it does not travel with the function and cold starts on a failover surge are what actually breaks the RTO.

⚠️ **The gotcha:** **without a DLQ configured beforehand, events lost during an outage are gone with no record** — Lambda retries twice on async and then drops. And a DynamoDB/Kinesis stream trigger has only **24 hours** of retention: an outage longer than that loses change events *permanently*, no matter how good your table backups are.

---

← [Resume-Aligned Priority Map](00-resume-aligned-priority-map.md) · [Index](README.md) · [DynamoDB](02-dynamodb.md) →
