> **AWS Quick Revision Notes** · [Index](README.md) · Part I

# Serverless & Lambda

---

## 1. Lambda Core

### Lambda Deep Dive

**What:** serverless compute — upload code, AWS runs on trigger, pay per invocation + duration(ms)×memory. No server/patch/scale.
- Event-driven (API GW, S3, SNS, DynamoDB Streams, SQS, EventBridge…), auto-scales per concurrent request, multi-AZ by default.
- Runtimes: Node, Python, .NET 6/8 (AOT + arm64), Java, Go, custom runtime (Rust).

**Execution model — Firecracker micro-VM lifecycle:** `INIT (cold) → INVOKE → FREEZE (warm reuse, never guaranteed) → SHUTDOWN (no hook, state lost)`.
1. **INIT (cold start):** new micro-VM, runtime bootstrap, static initializers, DI build, DB setup — all "outside the handler" code.
2. **INVOKE:** handler runs; must finish inside timeout (max 15 min).
3. **FREEZE:** env frozen; globals/connections/`/tmp` persist → warm = 1–10 ms.
4. **SHUTDOWN:** env reclaimed; no shutdown hook.

**INIT halves:** *runtime bootstrap* = platform's (micro-VM, CoreCLR/JIT, runtime client poll loop → why .NET cold = 300–1500 ms vs Node 50–200). *Static initializers* = mine (CLR runs once per type per load context, thread-safe, before first access). **Trap:** a static initializer that throws poisons the type for the whole environment's life.

**Cold vs warm:** Java/.NET (JIT) 300–1500 ms; Node/Python 50–200; **.NET Native AOT 50–100** (big win). Warm 1–10 ms. Mitigate: AOT, small package, avoid heavy DI graphs, avoid VPC unless needed, provisioned concurrency.

**Triggers & failure semantics:**
| Type | Examples | Failure |
|---|---|---|
| Synchronous | API GW, ALB, Step Functions, Invoke | Caller sees error, caller retries |
| Asynchronous | S3, SNS, EventBridge, SES | Lambda auto-retries (default 2 → 3 total); DLQ on exhaustion |
| Poll (event source mapping) | DynamoDB Streams, Kinesis, MSK, SQS | Lambda polls; retries until success or maxReceiveCount/DLQ |

**Cons / when NOT:** cold-start tail unfit for <50 ms SLA; 15-min hard timeout; vendor lock-in; max 10 GB memory + 10 GB `/tmp`; harder distributed observability.

**.NET specifics:** prefer **.NET 8 Native AOT** (no reflection DI magic — need `System.Text.Json` source generators; some trimmed libs break); `Amazon.Lambda.AspNetCoreServer` hosts full ASP.NET Core (heavier cold start); cache `IServiceProvider` / `DbContext` as **static** outside handler; watch RDS connection exhaustion (each concurrent env = own connection).

**Deployment surface:** `$LATEST` mutable; **publishing a version** = immutable snapshot. **Alias** = movable pointer (`prod`→v7) supporting **weighted routing** = canary/linear; **CodeDeploy** automates (`Canary10Percent5Minutes`) with alarm-triggered rollback. **Layers** = shared deps/extensions (5 max, 250 MB unzipped). **Destinations** route async result (onSuccess/onFailure with response+context — better than bare DLQ). Async `RetryAttempts` default 2. **Reserved** = cap+guarantee; **Provisioned** = pre-warm; **SnapStart = Java only** (.NET uses AOT + provisioned concurrency).

### Lambda Concurrency Model

| | Reserved | Provisioned |
|---|---|---|
| Purpose | Guarantee + cap for one function | Eliminate cold starts |
| Mechanism | Carves account/region pool | Pre-inits N warm envs |
| Cost | None extra | Billed hourly regardless |
| Above limit | Throttled | Falls back to cold scaling |

One-liner: **Reserved = guarantee capacity. Provisioned = eliminate cold starts.**

**Burst:** default regional limit 1,000 (increasable); first ~1,000 instant, then +500/min to limit. *Limit = how far; burst = how fast.* Each region = independent pool. Active-passive DR: pre-raise limit in DR region before failover.

**Sizing:** `Required Concurrency ≈ Peak RPS × Avg Duration(s) × Safety(1.3–2.0)`. E.g. 500/s × 1.2 s ≈ 600. SQS-driven async: cap concurrency below peak — SQS absorbs backlog as back-pressure, protecting the DB.

**Traps:** faster 2nd run = warm start (not logic caching) · timeout = wall-clock incl. network waits · SQS reprocess = deleted only after success · no transaction rollback, idempotency required · **exactly-once impossible (at-least-once only)** · VPC slower = ENI attach (much reduced since 2019 Hyperplane) · no "fat Lambda" · 15-min cap.

**ENI vs VPC Endpoint:** ENI = interface Lambda attaches to reach private VPC resources (RDS, internal ALB), adds cold-start cost. VPC Endpoint (Gateway for S3/DynamoDB, Interface/PrivateLink for others) = reach AWS services without NAT/internet. Rule: ENI only to reach private resources; endpoints to avoid NAT cost/latency.

---

## 2. Choosing Compute

### Lambda vs ECS vs Fargate

Deciding axis = **who owns the capacity (the EC2 instances)**: Lambda = no host at all; Fargate = host exists but is AWS's; ECS-on-EC2 = instances in my VPC/ASG (SSH, GPU, custom AMI, daemons, Spot/RI bin-packing possible — at the price of patching + slower scaling + idle cost).
- **ECS is the orchestrator; Fargate is a capacity provider** (for ECS/EKS) — not rival products. Real comparison = ECS *on EC2 launch type* vs ECS *on Fargate*.
- **Cost inversion:** Lambda cheapest at low/spiky; Fargate middle; EC2 wins at steady high density.
- **Scaling speed:** Lambda seconds; Fargate per task (~30–60 s); EC2 slowest (instance boot).
- **IAM:** Lambda = execution role; ECS = task role (app) + task execution role (image pull/logs) — conflating the two is a classic trap.

---

## 3. Event-Driven Patterns

### Serverless & S3 → Lambda Trigger

**Serverless set:** Lambda, Fargate, S3, DynamoDB, SQS/SNS/EventBridge, API GW, Step Functions, Aurora Serverless v2.

**Canonical flow:** S3 event notification on `s3:ObjectCreated:*` (prefix/suffix filter) → S3 invokes Lambda with bucket+key (not the object → `GetObject`) → function needs a **resource-based policy** allowing `s3.amazonaws.com` + execution role with `s3:GetObject`/`PutObject`.
```json
{ "Effect":"Allow","Principal":{"Service":"s3.amazonaws.com"},
  "Action":"lambda:InvokeFunction","Resource":"arn:...:function:process-upload",
  "Condition":{"StringEquals":{"AWS:SourceAccount":"123456789012"},
    "ArnLike":{"AWS:SourceArn":"arn:aws:s3:::my-upload-bucket"}}}
```
`SourceAccount`/`SourceArn` = confused-deputy guard.
- **❗ Infinite recursion:** writing output back to the same bucket under a matching path re-fires the function forever. Fix: different bucket, or output prefix that can't match (`uploads/`→`processed/`). AWS has recursion *detection* but the architecture fix is yours.
- **At-least-once + possible reorder** → handler must be **idempotent** (key on object key + ETag/version).
- **Route via EventBridge** for content filtering, multiple targets, retries+DLQ, archive/replay.

---

## 4. Resume Follow-Ups

### Lambda Resume Follow-Up + DR

12 functions vs 1 with 12 schedules: both defensible — blast radius/IAM scoping vs fewer cold starts/single artifact.

**DR:** nothing durable lives *in* Lambda (code in Git, config in IaC); at risk = in-flight events + event-source wiring. Rollback = repoint alias to previous version (`aws lambda update-alias --function-version 41`). Regional failure = `terraform apply` in DR region, recreate event source mappings (regional, not part of function), reset provisioned concurrency. **❗ Without a DLQ configured beforehand, events lost in an outage are gone** (async retries twice then drops). DynamoDB/Kinesis stream triggers have only **24h** retention — a longer outage loses change events permanently.

---

← [Resume-Aligned Priority Map](00-resume-aligned-priority-map.md) · [Index](README.md) · [DynamoDB](02-dynamodb.md) →
