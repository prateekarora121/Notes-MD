# AWS — Interview Revision Notes

> Quick-revision Q&A derived from `K. AWS-Interview-Guide.md`. Covers every section of the source.

## How to Use This Guide: Resume-Aligned Priority Map

### Tier 1 — Must be bulletproof (explicitly claimed on my resume)

**Q: Which AWS topics must be bulletproof for this resume, and why?**

A:

- **Lambda** — named skill + delivery bullet (scheduled jobs, 12 services, 99.9% uptime)
- **DynamoDB** — named skill, "DynamoDB-backed microservices" shipped
- **Terraform/CDKTF** — headline IaC skill, used in deployment-dashboard bullet
- **GitHub Actions → AWS** — named CI/CD skill (OIDC keyless pattern is the AWS half)
- **IAM roles & policies** — unavoidable, underlies all of the above
- **S3** — named skill, also the Terraform state backend
- **EC2** — named skill, provisioned via CDKTF
- **CloudWatch** — implied by "log maintenance and health checks, 99.9% uptime"

### Tier 2 — Must reason about confidently, while being honest about hands-on gaps

**Q: Which topics are Tier 2, and what's the right framing when you lack hands-on time?**

A: ECS/Fargate, RDS/Aurora, VPC design, SQS/SNS/EventBridge, ALB/ASG, CloudFront, Step Functions, Secrets Manager — these surface in "how would you architect X" design questions. Framing that works: *"I haven't operated Fargate in production — my container work has been Docker locally and ECS-adjacent. What I can walk you through is how I'd choose between it and EC2 for this workload, and what I'd want to validate before committing."* Honest boundary + reasoning beats a confident wrong answer.

### Tier 3 — Recognise, place, and give one clean sentence

**Q: Which services only need "recognise and place" depth?**

A: Kinesis, Amazon MQ, Athena/Redshift/Glue, GuardDuty/Inspector/Macie/Config, Organizations/Control Tower/RAM, Direct Connect/VPN, Snow Family/DMS/Storage Gateway, Local Zones/Outposts, CloudHSM. Goal: know the problem each solves and when to reach for it — not recognizing the name is the actual gap.

### Azure → AWS Translation (I hold AZ-900, and shipped on Cosmos DB + Azure Blob)

**Q: Map Cosmos DB and Azure Blob Storage to their AWS equivalents.**

A:

- **Cosmos DB → DynamoDB** — both NoSQL with partition keys and provisioned/on-demand throughput. Cosmos has 5 tunable consistency levels + multi-model APIs; DynamoDB gives eventual or strong only. RU/s ↔ RCU/WCU; both punish low-cardinality partition keys with hot partitions.
- **Azure Blob Storage → S3** — containers ↔ buckets; blob tiers (Hot/Cool/Archive) ↔ storage classes; SAS tokens ↔ pre-signed URLs (strong thing to volunteer).

**Q: What are the remaining Azure → AWS mappings worth knowing?**

A:

- Azure Functions → Lambda (Consumption ↔ standard, Premium pre-warmed ↔ provisioned concurrency)
- App Service → Elastic Beanstalk / ECS Fargate
- Azure SQL Database → RDS / Aurora
- Azure AD (Entra ID) → IAM + IAM Identity Center (Entra ID is an identity *provider*; IAM is authorization *within* an account; bridge via SAML/OIDC federation)
- Azure Key Vault → KMS (keys) + Secrets Manager (secrets) — AWS splits what Key Vault does in one service
- Azure Monitor/App Insights → CloudWatch (metrics/logs) + X-Ray (tracing)
- Azure DevOps Pipelines → CodePipeline / GitHub Actions
- ARM/Bicep → CloudFormation (Terraform works against both)
- Resource Groups → no direct equivalent; AWS isolates via accounts, Azure via subscriptions/resource groups (tags + CFN stacks are the closest analog)
- Azure Service Bus → SQS + SNS (or Amazon MQ for native AMQP lift-and-shift)

### Resume Deep-Dives — The Follow-Ups I Should Expect

**Q: For the "scheduled Lambda jobs, 99.9% uptime" bullet, how are jobs scheduled and how is log maintenance done?**

A:

- **Scheduling**: EventBridge scheduled rules (`cron(...)`/`rate(...)`) or EventBridge Scheduler (newer, higher-scale, one-time schedules, time zones, built-in retry/DLQ). Say "EventBridge", not "CloudWatch Events".
- **Log maintenance**: CloudWatch Logs retention policies (log groups default to Never Expire — a silent unbounded cost leak), metric filters (turn log patterns into alarmable metrics), subscription filters (stream logs onward), export to S3 + lifecycle to Glacier for cheap long retention.

**Q: How would you justify a 99.9% uptime claim, and what's the trap question about the health-check itself?**

A: 99.9% = ~43 min downtime/month budget. Credible answer ties it to measured alarms (Metric Math, not raw counts), composite alarms to cut noise, SNS to on-call, per-service dashboard. Trap: *"what if the health-check Lambda itself fails?"* — must alarm on the function's own `Errors`/`Throttles` **and** on missing data (`treat-missing-data: breaching`), since a dead monitor looks identical to "all healthy."

**Q: For the GitHub Actions + CDKTF/Terraform deployment-dashboard bullet, how does the pipeline authenticate and stay safe?**

A:

- **Auth**: GitHub OIDC assuming an IAM role — no static keys in secrets; trust policy scoped by `sub` to repo + branch/environment.
- **Safety**: plan on PR (read-only role) → apply on merge (privileged role) from a saved plan file, GitHub environment protection rules for manual prod approval, `tfsec`/`checkov` gates.
- **State**: S3 backend + DynamoDB lock table (guards concurrent pipeline runs).

**Q: For the "DynamoDB-backed microservices" bullet, what's the expected depth?**

A: Access patterns first, partition-key cardinality/hot partitions, GSI vs LSI, on-demand vs provisioned capacity, Query vs Scan, conditional writes for idempotency, 400 KB item limit, single-table design — this is the resume's most product-tied AWS claim, revise hardest.

**Q: What's the 60-second "tell me about your AWS experience" answer structure?**

A: Concrete services used (DynamoDB, Lambda, GitHub Actions → CDKTF/Terraform → Lambda/DynamoDB/EC2/S3) + measurable outcome (40% less manual intervention, release cycle 5→3 days) + unprompted honest boundary (ECS/Fargate, RDS reasoned-about but not production-operated).

---

# PART I — Tier 1: Resume-Claimed Core

## Serverless & Lambda

### AWS Lambda Deep Dive

**Q: What is AWS Lambda and what are its key characteristics?**

A: Serverless compute — upload code, AWS runs it on trigger, billed per invocation + duration. Event-driven (API Gateway, S3, SNS, DynamoDB Streams, SQS, EventBridge), fully managed, auto-scales per concurrent request, supports Node.js/Python/.NET 6/8 (AOT + arm64)/Java/Go/custom runtimes.

**Q: Walk through the Lambda execution lifecycle (Firecracker micro-VM).**

A:

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

1. **INIT (cold start)** — new micro-VM, runtime bootstrap, static initializers, DI container build, DB connections — all "outside the handler" code.
2. **INVOKE** — handler runs with event + context, must finish within timeout (max 15 min).
3. **FREEZE (warm reuse)** — environment frozen after response; globals/connections/`/tmp` persist, giving 1–10ms warm invocations.
4. **SHUTDOWN** — AWS reclaims idle/outdated environments; no shutdown hook, state is lost not rolled back.

**Q: Compare cold start vs warm start latency across runtimes, and how do you mitigate cold starts?**

A: Cold: Java/.NET JIT 300–1500ms, Node/Python 50–200ms, .NET Native AOT 50–100ms. Warm: 1–10ms. Mitigations: .NET AOT, small deployment package, avoid heavy DI graphs, avoid VPC unless required, Provisioned Concurrency for latency-sensitive APIs.

**Q: What are the three trigger categories and their failure semantics?**

A:

- **Synchronous** (API Gateway, ALB, Step Functions, direct Invoke) — caller sees the error and must retry.
- **Asynchronous** (S3, SNS, CloudWatch Events, SES, EventBridge) — Lambda retries automatically; DLQ on exhaustion.
- **Poll-based / event source mapping** (DynamoDB Streams, Kinesis, MSK, SQS) — Lambda polls internally, retries until success or maxReceiveCount/DLQ.

**Q: What are Lambda's cons / when should you not use it?**

A: Cold-start latency unsuitable for tight tail-latency SLAs; 15-min hard timeout; vendor lock-in to AWS event model; max 10GB memory / 10GB `/tmp`; harder cross-function observability (needs X-Ray + structured logging).

**Q: What .NET-specific considerations matter for Lambda?**

A:

- Prefer **.NET 8 Native AOT** for latency-sensitive functions — no JIT warm-up, smaller package; trade-off: no reflection-based DI magic (need `System.Text.Json` source generators), some reflection-heavy libraries break under trimming.
- `Amazon.Lambda.AspNetCoreServer` hosts a full ASP.NET Core app behind API Gateway/ALB — easy lift-and-shift, heavier cold start.
- Avoid building a full `IServiceProvider` graph per cold start — cache it as a static field.
- Create `DbContext`/connection pool **outside the handler** (static/singleton); watch RDS Proxy/connection-pool exhaustion at high concurrency.

**Q: Explain Lambda versions, aliases, and canary deploys.**

A: `$LATEST` is mutable; publishing a version creates an immutable numbered snapshot. An alias is a named movable pointer (`prod` → v7) supporting weighted routing across two versions — the mechanism behind canary/linear deploys (shift 10% traffic, watch alarms, complete or roll back). CodeDeploy automates this (`Canary10Percent5Minutes`) with alarm-triggered auto-rollback.

**Q: What are Lambda Layers and Destinations?**

A: **Layers** package shared dependencies/Extensions separately from function code (max 5 layers, 250MB unzipped). **Destinations** route the *result* of an async invocation (`onSuccess`/`onFailure`) to SQS/SNS/EventBridge/Lambda — better than a bare DLQ because they include the response/error payload and request context, not just the original event.

**Q: What's the default retry behavior for async Lambda invokes, and what's the difference between Reserved/Provisioned concurrency and SnapStart?**

A: Async invokes retry **2** times by default (3 total attempts) with an event age limit. Reserved concurrency caps+guarantees a function's capacity share; Provisioned concurrency pre-initializes environments to remove cold starts; SnapStart is a Java-only cold-start fix — for .NET use Native AOT + trimming + provisioned concurrency instead.

### Lambda Concurrency Model

**Q: Reserved vs Provisioned concurrency — what's the one-liner?**

A: **Reserved = guarantee/cap capacity** (no extra cost, throttles above limit). **Provisioned = eliminate cold starts** (pre-inits N warm environments, billed hourly regardless of use, falls back to cold scaling above the provisioned amount).

**Q: What are Lambda's burst-scaling rules?**

A: Default regional concurrency limit is 1,000 concurrent executions (increasable). First ~1,000 scale instantly; beyond that, +500 new environments/minute until the limit is hit. Concurrency limit = how far you can scale; burst rate = how fast.

**Q: How do you size Lambda concurrency, and how does multi-region concurrency work?**

A:

```
Required Concurrency ≈ Peak RPS × Avg Duration (seconds) × Safety Factor (1.3–2.0)
```

Example: 500 msgs/sec × 1.2s duration ≈ 600 concurrency (before safety factor). Each region has an independent concurrency pool/burst behavior. Active-active: provision reserved/provisioned separately per region. Active-passive DR: pre-raise the DR region's concurrency limit *before* failover, or a cold default-limit region throttles under failover load.

**Q: Rapid-fire — why did Lambda run faster the 2nd time, and can it guarantee exactly-once?**

A: Faster 2nd time = warm start (environment reuse, not "cached logic"). No — Lambda is at-least-once only; idempotency is mandatory. Also: timeout is wall-clock (includes network waits, not just CPU); SQS messages reprocess because they're only deleted after success; DB writes aren't transactional/rolled back on crash; VPC placement adds cold-start latency via ENI attachment; business logic shouldn't live in Lambda ("fat Lambda" anti-pattern); no run beyond 15 min (use Step Functions/ECS/Batch).

**Q: ENI vs VPC Endpoint — when do you need each?**

A: **ENI** is the network interface Lambda attaches inside a VPC to reach private resources (RDS, internal ALB) — adds cold-start overhead. **VPC Endpoint** (Gateway for S3/DynamoDB, Interface/PrivateLink for most others) lets a VPC-bound Lambda reach AWS services without NAT/internet — lower latency/cost, no public exposure. Rule: ENI only when you must reach private VPC resources; use Endpoints to avoid NAT once already in a VPC.

### Lambda vs ECS vs Fargate

**Q: How do you choose between Lambda, ECS on the EC2 launch type, and Fargate?**

A: Workload-shape-driven, and the real axis is **who owns the capacity**: event-driven/spiky/short-lived → **Lambda**; container-based *and* I need the host (GPU, custom AMI/kernel, daemons, Spot/RI tuning) → **ECS on the EC2 launch type**; container-based with no host requirement → **ECS/EKS on Fargate**. Note that ECS is the orchestrator and Fargate is a capacity provider for it — not a rival product.

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

**Q: Compare cost, scaling speed, and IAM model across the three.**

A:

- **Cost inversion**: Lambda cheapest at low/spiky traffic; Fargate in the middle (per task vCPU/GB-second while running); EC2 launch type cheapest at steady high density — you pay per instance-hour regardless of task count, and Spot/RI/Savings Plans apply.
- **Scaling speed**: Lambda in seconds; Fargate per task (~30–60s for image pull + ENI attach); EC2 launch type slowest whenever the cluster itself must grow (instance boot, storage attach, register with the cluster).
- **IAM**: Lambda uses one execution role; ECS tasks use a **task role** (app permissions) plus a separate **task execution role** (pulling images, writing logs) — conflating the two is a classic trap.
- **Fargate's limits are the reason to stay on the EC2 launch type**: no privileged containers, no daemonsets, no GPU.

### Serverless & the S3 → Lambda Trigger Pattern

**Q: What does "serverless" mean, and which AWS services count?**

A: No servers to provision/patch, auto-scale from zero, pay only for use, built-in HA. Set: Lambda, Fargate, S3, DynamoDB, SQS/SNS/EventBridge, API Gateway, Step Functions, Aurora Serverless v2.

**Q: Walk through the canonical S3 → Lambda trigger flow and its permission model.**

A:

1. Configure an S3 event notification on `s3:ObjectCreated:*`, optionally filtered by prefix/suffix.
2. S3 invokes Lambda with an event containing **bucket + key only** (not the object) — function calls `GetObject`.
3. Function needs a **resource-based policy** allowing `s3.amazonaws.com` to invoke it, and its execution role needs `s3:GetObject`/`s3:PutObject`. Use `SourceAccount`/`SourceArn` conditions as the confused-deputy guard.

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

**Q: What are the two guaranteed-to-be-asked gotchas of S3-triggered Lambdas?**

A:

- **Infinite recursion** — writing output back into the same bucket under a path the trigger still matches causes an unbounded invoke loop; fix with a different destination bucket or non-overlapping prefixes (AWS also has recursive-invocation detection now, but the architecture fix is yours).
- **At-least-once delivery** — S3 notifications can be delivered more than once/out of order, so handlers must be idempotent (key on object key + ETag/version ID).

**Q: When would you route S3 events through EventBridge instead of native notifications?**

A: When you need content-based filtering, multiple targets per event, retries with a DLQ, or archive/replay — native S3 notifications support only one destination per event type.

---

## DynamoDB

### DynamoDB Deep Dive

**Q: What is DynamoDB and how does partitioning work?**

A: Fully managed NoSQL key-value/document store with single-digit-ms latency at scale, if key design is correct. Partition Key hash decides physical partition (~3,000 RCU/1,000 WCU per partition, directional not contractual). Good PK = high cardinality + even access distribution. Bad PK (low cardinality, time-based, "celebrity" key) → hot partition → throttling even when table-level metrics look fine. **Adaptive Capacity** smooths unevenness automatically but does not fix bad key design.

**Q: What does the Sort Key unlock, beyond just ordering?**

A: Range queries, time-series lookups, one-to-many relationships, hierarchical/single-table aggregates (`PK=ORDER#555`, `SK=META#/ITEM#1/EVENT#CREATED` fetched in one Query), and sorted views via rank-encoded SK.

**Q: GSI vs LSI — full comparison.**

A:

| | GSI | LSI |
|---|---|---|
| Partition key | Different from base table | Same as base table |
| Sort key | Own, optional | Different from base table |
| Created | Any time | Only at table creation |
| Consistency | Eventual only | Can be strongly consistent |
| Capacity | Own throughput | Shares base table capacity |

Cost note: every base write may also write to a GSI (write amplification) — project only needed attributes.

**Q: On-Demand vs Provisioned capacity — when do you pick each?**

A: On-Demand: no capacity planning, pay-per-request, best for spiky/unknown traffic. Provisioned (+Auto Scaling): RCU/WCU sizing required, cheaper at steady predictable volume, supports Reserved Capacity discounts.

**Q: Why is Query almost always preferred over Scan?**

A: Query is PK-targeted (O(matched items)); Scan reads the entire table/index then filters — expensive and slow, acceptable only for rare admin/analytics jobs.

**Q: How do conditional writes give you concurrency control without full transactions?**

A: `PutItem` with `ConditionExpression: attribute_not_exists(idempotencyKey)` gives idempotency; `UpdateItem` with a status-equality condition gives optimistic locking/state-machine transitions — both server-side atomic checks, no distributed lock, and no 2× transaction cost.

**Q: When do you reach for `TransactWriteItems`/`TransactGetItems`, and what's the cost?**

A: ACID across up to 100 items/tables, 4 MB aggregate (raised from 25 in Sept 2022 — older material still says 25), for genuine all-or-nothing invariants (inventory decrement + order creation, money transfer). Costs ~2× capacity plus added latency — not a default choice.

**Q: How does TTL behave, and what should it never be used for?**

A: Attribute-driven, best-effort async deletion — can lag hours (documented up to ~48h in some cases). Good for idempotency keys/sessions/dedup/temp workflow state. Never rely on it for time-sensitive compliance deletion deadlines.

**Q: What are DynamoDB Streams used for?**

A: Time-ordered change log (insert/update/delete), retained ~24h. Backbone for CQRS read-model projections, event-driven pipelines (Streams → Lambda → SNS/SQS/EventBridge), and CDC/audit trails/search-index sync.

**Q: How do Global Tables handle multi-region writes and conflicts?**

A: Multi-region, multi-active replication; users read/write nearest region; conflict resolution is last-writer-wins; replication is asynchronous/eventually consistent — design writes to be idempotent/conflict-tolerant.

**Q: What is single-table design, and what's the 400 KB item limit workaround?**

A: Multiple entity types (User, Order, Item, Event) share one table via PK/SK convention, trading modeling effort for fewer round-trips and no joins. For items exceeding 400 KB (hard cap, includes attribute names+values): store the blob in S3, keep a pointer in DynamoDB, or vertically partition into multiple items under the same PK.

#### Capacity Maths & Hot-Partition Mitigation

**Q: Define RCU and WCU precisely, and work an example.**

A: 1 RCU = one strongly consistent read of up to 4KB/s, or two eventually consistent reads of 4KB/s (eventual reads are half cost). 1 WCU = one write up to 1KB/s; transactional reads/writes cost 2×. Example: 100 reads/sec of 10KB items, eventually consistent → 10KB rounds to 3×4KB units ÷2 (eventual) = 1.5→2 RCU/read × 100 = ~200 RCU.

**Q: How do you fix a hot partition caused by low-cardinality PK (e.g., `STATUS#PENDING` or a date)?**

A: Write sharding — append a calculated suffix, e.g. `PK = ORDER#2026-08-10#3` (shard = hash(orderId) % 10), spreading writes across N partitions. Trade-off: reads must now query all N shards and merge, so only shard where the hot spot is real. Adaptive Capacity helps but isn't a substitute for good key design.

```
PK = ORDER#2026-08-10#3        // shard = hash(orderId) % 10
```

**Q: What are GSI overloading and sparse indexes?**

A: **Overloading** — one GSI with generic keys (`GSI1PK`/`GSI1SK`) serves multiple access patterns because different entity types populate them differently. **Sparse index** — an item appears in a GSI only if it has the index's key attribute, so writing `GSI1PK` only on unprocessed orders yields a tiny, cheap-to-scan "work queue" index.

#### DynamoDB in .NET — The Code You'd Be Asked to Write

**Q: What are the three DynamoDB .NET SDK layers, and when do you use each?**

A: **Low-level** (`AmazonDynamoDBClient` + `AttributeValue` dicts) — full control, conditions, transactions, single-table design. **Document model** (`Table`+`Document`) — schema-flexible without POCOs. **Object persistence** (`DynamoDBContext`+`[DynamoDBTable]`) — simple one-entity-per-table CRUD; fits single-table design poorly.

**Q: Why must `AmazonDynamoDBClient` be a singleton in Lambda?**

A: It's thread-safe; creating one per request is a real performance bug. Create it outside the handler so it survives warm invocations and reuses connections.

**Q: Show the idempotency pattern using a conditional `PutItem`.**

A:

```csharp
try {
    await client.PutItemAsync(new PutItemRequest {
        TableName = "Orders",
        Item = new() { ["PK"] = new("ORDER#"+orderId), ["SK"] = new("META#"), ["status"] = new("PENDING") },
        ConditionExpression = "attribute_not_exists(PK)"
    });
} catch (ConditionalCheckFailedException) {
    // Already processed — this is SUCCESS, not an error.
}
```

**Q: Show how to query a GSI with a key condition, and why should `Scan` be avoided in a hot path?**

A: `Query` against a GSI with a key condition targets only matching partitions; `Scan` reads the entire table/index and filters afterward, which is expensive and slow.

```csharp
var resp = await client.QueryAsync(new QueryRequest {
    TableName = "Orders",
    IndexName = "GSI1",
    KeyConditionExpression = "GSI1PK = :pk AND begins_with(GSI1SK, :prefix)",
    ExpressionAttributeValues = new() {
        [":pk"]     = new("CUSTOMER#42"),
        [":prefix"] = new("ORDER#2026-08")
    },
    Limit = 25
});
```

**Q: How do you implement an atomic counter with a ceiling, and how does optimistic locking work with the object-persistence model?**

A: `UpdateItem` mutates server-side, so there's no read-modify-write race:

```csharp
UpdateExpression    = "SET #v = #v + :inc",
ConditionExpression = "#v < :max"          // atomic increment with a ceiling
```

With the object-persistence model, `[DynamoDBVersion]` gives you optimistic locking automatically — the SDK adds a version condition and throws on conflict.

**Q: Why must the pagination loop check `LastEvaluatedKey?.Count > 0`, not just `!= null`?**

A: `Query`/`Scan` cap at 1MB per call; `LastEvaluatedKey` comes back as an **empty dictionary** (not null) when finished, so checking only `!= null` loops forever. SDK v3's `Paginators.QueryAsync` handles this automatically.

```csharp
Dictionary<string, AttributeValue>? start = null;
do {
    var page = await client.QueryAsync(new QueryRequest { /* … */ ExclusiveStartKey = start });
    Process(page.Items);
    start = page.LastEvaluatedKey?.Count > 0 ? page.LastEvaluatedKey : null;
} while (start != null);
```

**Q: What's the difference between `BatchWriteItem` and `TransactWriteItems`?**

A: `BatchWriteItem` (≤25 items) is a throughput optimization that can **partially succeed** — you must resubmit `UnprocessedItems` with backoff; no atomicity/conditions. `TransactWriteItems` is all-or-nothing ACID, at ~2× capacity cost:

```csharp
await client.TransactWriteItemsAsync(new TransactWriteItemsRequest {
    TransactItems = new() {
        new() { Put    = new Put    { TableName = "Orders",    /* … */ } },
        new() { Update = new Update { TableName = "Inventory", /* decrement stock */ } }
    }
});
```

**Q: List key .NET gotchas for DynamoDB.**

A: Use `decimal` not `double` for money (Number type is arbitrary-precision); `DynamoDBContext` caches type metadata so keep it long-lived; the SDK already retries throttling with backoff (don't stack your own — tune `MaxErrorRetry`); use `ReturnConsumedCapacity` while tuning; reserve `Scan` for admin/backfill, ideally over a sparse GSI.

**Q: Show a Streams-triggered Lambda handling change events, and why must it be idempotent?**

A: DynamoDB Streams is the CDC pattern behind read models and audit logs; the same record can be delivered more than once, so the handler must tolerate re-delivery:

```csharp
public async Task Handler(DynamoDBEvent evnt, ILambdaContext ctx)
{
    foreach (var record in evnt.Records)
    {
        if (record.EventName == "INSERT") { /* record.Dynamodb.NewImage */ }
        // idempotent: the same record can be delivered more than once
    }
}
```

### DynamoDB Trick Questions

**Q: Rapid-fire — is DynamoDB relational, can a GSI be strongly consistent, and can LSIs be added after table creation?**

A: Not relational — NoSQL, no joins/FKs. GSIs can **never** be strongly consistent. LSIs **cannot** be added after creation (must be defined at table creation); GSIs can be added anytime.

**Q: Rapid-fire — is Scan faster than Query, can a PK be duplicated, is DynamoDB strongly consistent by default, does TTL delete instantly, do transactions cost more?**

A: Scan is not faster — it reads the whole table. A PK *can* repeat if SKs differ (that's composite keys). Default read consistency is **eventual**, not strong. TTL deletion is best-effort and can take hours. Transactions cost roughly 2× normal capacity.

**Q: What's the senior-level one-sentence summary of DynamoDB?**

A: "DynamoDB trades query flexibility for massive, predictable scalability. Efficient usage depends on correct partition key design, denormalized access-pattern-first modeling, and avoiding scans, hot partitions, and unnecessary indexes."

---

## IAM & Security

### IAM Overview, Root Account & Shared Responsibility

**Q: What does IAM actually govern, and what two checks does every AWS API call go through?**

A: IAM decides *who* can do *what* on *which resource* — it doesn't store data or run workloads. Every call passes (1) **Authentication** — are your credentials valid (password/access key/STS token)? and (2) **Authorization** — does a policy permit this action on this resource? Failing auth gives `InvalidClientTokenId`; failing authz gives `AccessDenied` — distinguishing them is step one of IAM debugging.

**Q: What warm-up facts about IAM do interviewers expect?**

A: IAM is **global** (no region selection); it's **free**; it's **eventually consistent** (a new role can fail on first use, succeed on retry); the **root account** is created at sign-up, identified by the sign-up email, and has power no policy can restrict.

**Q: What can only the root account do, that `AdministratorAccess` cannot?**

A: Close the account; change account name/root email/root password; change or cancel the Support plan; restore a user's revoked billing-management permission; register as a Reserved Instance Marketplace seller; enable S3 MFA Delete or remove an all-principal-denying bucket policy; view certain tax invoices.

**Q: What are the four root-account rules, and IAM's shared-responsibility split?**

A: Enable MFA immediately, never use it daily, never share it, never create access keys for it. AWS secures IAM as a global service and provides tooling (MFA, Access Analyzer, credential reports, CloudTrail); you're responsible for organizing identities, applying least privilege, rotating credentials, and actually reviewing that tooling.

### Users, Groups & Permissions

**Q: What are the hard rules about IAM Groups?**

A: A group holds only users (no nested groups); a user can belong to multiple groups with effective permissions being the **union**; a user can be in zero groups; a group is **not an identity** — it cannot log in and cannot appear as a policy `Principal`.

```
Account
├── Group: Developers  → [Parteek, Ravi]
├── Group: Operations  → [Ravi, Sara]     ← Ravi is in two groups: permissions add up
└── Group: Audit       → [Sara]
```

**Q: What can a brand-new IAM user with no attached policy do?**

A: Nothing — not even list S3 buckets. IAM is deny-by-default; every permission must be explicitly granted.

**Q: Identity-based vs resource-based policy — what's the structural difference?**

A: Identity-based (attached to User/Group/Role) has no `Principal` — the identity holding it *is* the principal. Resource-based (S3 bucket policy, SQS/KMS/Lambda resource policy) **must** name a `Principal`.

**Q: What are the two ways to do cross-account access, and how do they differ?**

A: **Resource-based policy** — caller keeps their own identity, calls the resource directly; the resource policy must name them AND their own account must allow the call (both sides). **AssumeRole** — caller *becomes* the role for that session, giving up their original permissions; only the role's permissions apply.

### Hands-On: Users & Groups

**Q: What's the correct habit when creating a user who needs S3 read access, and why download the credentials CSV immediately?**

A: Attach permissions to a **group** (e.g. create a `Developers` group with a managed policy), then add the user to the group — never attach policies to individual users directly. The console shows the password/secret **once**; if lost, you delete and reissue the credential rather than "look it up".

```bash
aws iam create-group  --group-name Developers
aws iam attach-group-policy --group-name Developers \
  --policy-arn arn:aws:iam::aws:policy/ReadOnlyAccess
aws iam create-user   --user-name parteek
aws iam add-user-to-group --user-name parteek --group-name Developers
aws iam list-groups-for-user --user-name parteek     # verify
```

**Q: How do you distinguish an IAM user sign-in from a root sign-in just from a URL/screenshot?**

A: IAM users sign in via the account-specific URL/alias (`https://my-company.signin.aws.amazon.com/console`); root signs in by **email address** at the generic sign-in page.

### Policy Types, Structure & Password Policy

**Q: AWS managed vs customer managed vs inline policy — which do you recommend for real work?**

A: **Customer managed** — versioned (5 versions, rollback-capable), reusable across identities, visible in audits. AWS managed policies are convenient but broader than needed. Inline policies are invisible to audits, non-reusable, and die with the identity — avoid them.

**Q: Show the anatomy of a policy document, field by field.**

A:

```json
{
  "Version": "2012-10-17",
  "Id": "OrdersBucketReadPolicy",
  "Statement": [
    {
      "Sid": "AllowReadOnOrdersBucket",
      "Effect": "Allow",
      "Action": ["s3:GetObject", "s3:ListBucket"],
      "Resource": [
        "arn:aws:s3:::my-bucket",
        "arn:aws:s3:::my-bucket/*"
      ],
      "Condition": {
        "Bool": { "aws:MultiFactorAuthPresent": "true" }
      }
    }
  ]
}
```

`Version` is the policy **language** version (always `2012-10-17`, not your document's own version). `Principal` is only ever used in resource-based/trust policies, never in an identity-based one. `Resource` sometimes must be `"*"` because some APIs (many `List*`/`Describe*`) don't support resource-level permissions.

**Q: Explain the `bucket` vs `bucket/*` ARN gotcha, and the general ARN format.**

A: `arn:aws:s3:::my-bucket` is the bucket itself — required for `s3:ListBucket`. `arn:aws:s3:::my-bucket/*` is the objects inside — required for `s3:GetObject`. Missing one gives "download works but `aws s3 ls` returns AccessDenied" or vice versa.

```
arn:partition:service:region:account-id:resource
arn:aws:iam::123456789012:user/parteek              ← IAM is global, so region is empty
arn:aws:s3:::my-bucket/file.txt                     ← S3 names are global, so region+account empty
arn:aws:dynamodb:us-east-1:123456789012:table/Orders
```

**Q: What is the full policy evaluation order once org-level guardrails exist?**

A: Explicit Deny anywhere → wins outright. Otherwise: SCP allows? → Permissions boundary allows? → Session policy allows? → Identity or resource policy allows? → else implicit Deny. **SCPs and boundaries can only take permissions away, never grant them.**

```
Request
 ├─ Any explicit Deny anywhere?          → DENY (nothing can override this)
 ├─ SCP (Organizations) permits it?      → no → DENY
 ├─ Permissions boundary permits it?     → no → DENY
 ├─ Session policy permits it?           → no → DENY
 ├─ Identity policy OR resource policy allows it?  → neither → DENY (implicit)
 └─ else                                 → ALLOW
```

**Q: RBAC vs ABAC — when do you reach for each?**

A: RBAC = permissions per job function, policy count grows with the org. ABAC = permissions driven by tags (`aws:PrincipalTag/Team` must match `aws:ResourceTag/Team`) — one policy scales to any number of teams without edits; the standard answer for scaling IAM across many microservices.

**Q: What does the Password Policy let you enforce, and what's the honest senior take on it?**

A: Minimum length (up to 128 chars), required character types, self-change allowed/blocked, expiration + reuse prevention. It's the cheapest brute-force defense, but the real controls are MFA and eliminating long-lived human credentials via IAM Identity Center.

#### IAM Policy Structure — Full Explanation

**Q: How do you read any IAM policy statement as one sentence?**

A: **Effect** (allow/deny) — **Action** (these API calls) — **Resource** (on these things) — **Condition** (but only when this is true).

**Q: Build a least-privilege S3 policy from an admin starting point, step by step.**

A:

```json
// Step 1 (admin, never ship): {"Effect":"Allow","Action":"*","Resource":"*"}
// Step 2 (scope service):    {"Effect":"Allow","Action":"s3:*","Resource":"*"}
// Step 3 (scope resource):   {"Effect":"Allow","Action":"s3:*","Resource":"arn:aws:s3:::my-bucket/*"}
// Step 4 (scope actions+prefix — least privilege):
{ "Effect": "Allow", "Action": ["s3:GetObject","s3:PutObject"], "Resource": "arn:aws:s3:::my-bucket/uploads/*" }
```

**Q: Show a normal multi-statement policy using the broad-allow + narrow-deny guardrail pattern.**

A: Multiple statements are evaluated independently and combined — no ordering, no fall-through; any `Deny` wins, otherwise any `Allow` grants:

```json
"Statement": [
  { "Sid": "ReadWholeBucket", "Effect": "Allow",
    "Action": ["s3:GetObject", "s3:ListBucket"],
    "Resource": ["arn:aws:s3:::my-bucket", "arn:aws:s3:::my-bucket/*"] },

  { "Sid": "NeverTouchTheArchive", "Effect": "Deny",
    "Action": "s3:*",
    "Resource": "arn:aws:s3:::my-bucket/archive/*" }
]
```

**Q: Why is `Allow` + `NotAction` dangerous, but `Deny` + `NotAction` useful?**

A: `{"Effect":"Allow","NotAction":"iam:*","Resource":"*"}` grants **every action except IAM** — near-admin disguised as restrictive; treat as a finding in review. With `Deny`, it's the standard region-lock pattern: deny everything **except** global services, outside approved regions:

```json
{ "Effect": "Deny",
  "NotAction": ["iam:*", "sts:*", "route53:*", "cloudfront:*", "support:*"],
  "Resource": "*",
  "Condition": { "StringNotEquals": { "aws:RequestedRegion": ["us-east-1", "ap-south-1"] } } }
```

**Q: What's the trap with `ForAllValues:` condition operators?**

A: `ForAllValues:` passes if every value in a multi-valued request key matches — but it also returns **true when the key is absent entirely**, so alone in an `Allow` it can grant more than intended. Pair it with a `Null` check when load-bearing, e.g. tenant isolation:

```json
"Condition": { "ForAllValues:StringEquals": { "dynamodb:LeadingKeys": ["${aws:PrincipalTag/TenantId}"] } }
```

**Q: What are policy variables, and what are the size/count limits to know?**

A: Substituted at evaluation time, e.g. `${aws:username}` in a resource ARN gives every user their own S3 folder from one shared policy:

```json
{ "Effect": "Allow", "Action": "s3:*",
  "Resource": "arn:aws:s3:::company-bucket/home/${aws:username}/*" }
```

Limits: managed policy capped at 6,144 chars; inline budgets 2,048/5,120/10,240 chars (user/group/role); 10 managed policies per identity by default.

**Q: What's the recommended process for writing a tight policy from scratch?**

A: List exact API calls (from code or CloudTrail with broad sandbox permissions) → write exact ARNs (only `"*"` where the API has no resource-level permissions) → add conditions (region/MFA/IP/tag) → test in the Policy Simulator → refine using Access Advisor or Access Analyzer's generate-from-CloudTrail-history feature.

### MFA (Multi-Factor Authentication)

**Q: What MFA device types exist, and what's the key nuance about CLI/API coverage?**

A: Virtual MFA (TOTP app), FIDO/U2F key (phishing-resistant, unlike TOTP), hardware TOTP token, passkeys/biometrics (FIDO2). MFA natively protects only **console sign-in**; CLI/API needs a policy condition (`aws:MultiFactorAuthPresent`) plus an MFA-backed session via `sts:GetSessionToken` or `AssumeRole --serial-number/--token-code`.

**Q: How many MFA devices can a user register, and what's the standard cross-account production control?**

A: Up to 8 devices per user (register a backup). Requiring MFA on `sts:AssumeRole` in the **trust policy** is the standard control for cross-account production access.

### Access to AWS: Console, CLI, SDK & Access Keys

**Q: What's the access-key rotation discipline, and why is the 2-key limit deliberate?**

A: Max 2 keys per user, specifically to allow zero-downtime rotation: create key #2 → roll out → verify traffic uses it → **delete** (not just deactivate) key #1. Never commit keys to Git, bake into AMIs/images, or use them when a role/OIDC is available.

**Q: What are the three front doors into AWS, and what's the single most useful IAM debug command?**

A: Console (username+password+MFA, for humans), CLI (access key or temporary role credentials, for scripting), SDK (same, normally supplied by a role, for application code) — all ultimately call the same SigV4-signed REST API.

```bash
aws configure                    # writes ~/.aws/credentials + ~/.aws/config
aws configure --profile dev      # named profile
aws sts get-caller-identity      # "who am I?" — the single most useful IAM debug command
aws iam list-access-keys --user-name parteek
aws s3 ls --profile dev
```

**Q: What is the default credential provider chain order, and why does that cause a classic bug?**

A: (1) explicit CLI/SDK params, (2) environment variables, (3) shared credentials/config file, (4) container credentials (ECS task role), (5) EC2 instance profile via IMDS — **last**. A stale `AWS_ACCESS_KEY_ID` env var silently shadows the EC2/ECS role, causing `AccessDenied` for permissions the role clearly has. `aws sts get-caller-identity` reveals it (shows `user/...` instead of `assumed-role/...`).

### Hands-On: MFA & Access Keys

**Q: How do you create and verify a new access key hands-on?**

A: Security credentials → *Create access key* → choose the CLI use case → acknowledge the warning → download the `.csv`.

```bash
aws configure
# AWS Access Key ID:     AKIA...
# AWS Secret Access Key: ****
# Default region name:   us-east-1
# Default output format: json
aws sts get-caller-identity      # confirms which identity the key belongs to
```

**Q: What's the access-key rotation drill, command by command?**

A:

```bash
aws iam create-access-key --user-name parteek          # key #2
# deploy, verify traffic uses key #2
aws iam update-access-key --user-name parteek --access-key-id AKIA_OLD --status Inactive
# soak — revert to Active if anything breaks
aws iam delete-access-key --user-name parteek --access-key-id AKIA_OLD
```

### IAM Roles, Policies, AssumeRole

**Q: What's the "uniform on a hook" analogy for an IAM role vs a user's wallet ID card?**

A: A user's credentials are a permanent ID card kept forever; a role is a uniform you put on, use, and take off — temporary credentials, nothing to leak or rotate, granted only while assumed.

**Q: Trust policy vs permission policy — the #1 confusion point?**

A: Trust policy lives on the role's *Trust relationships* tab, answers "who can assume this role", requires `Principal`, no `Resource` (the role **is** the resource), action is `sts:AssumeRole`. Permission policy is attached to the role, answers "what can it do once assumed", requires `Resource`, forbids `Principal`. Both required, evaluated separately — nothing to merge.

**Q: How do you trust any account in your AWS Organization without enumerating account IDs?**

A: Use `aws:PrincipalOrgID` in the trust policy condition:

```json
"Condition": { "StringEquals": { "aws:PrincipalOrgID": "o-abc123xyz" } }
```

**Q: What is a Lambda execution role's trust policy, and what does the AssumeRole mechanics flow via STS look like?**

A: AssumeRole via STS: caller authenticates → STS checks the target role's trust policy → STS issues temporary credentials (Access Key, Secret Key, Session Token, expiring 15min–12h) → caller uses them, governed by the role's permission policy.

```json
{
  "Version": "2012-10-17",
  "Statement": [
    { "Effect": "Allow", "Principal": { "Service": "lambda.amazonaws.com" }, "Action": "sts:AssumeRole" }
  ]
}
```

**Q: Walk through the cross-account AssumeRole mechanics end to end.**

A: Account B creates a role trusting Account A's ARN; Account B attaches a scoped permission policy; Account A's caller needs its **own** `sts:AssumeRole` permission on that role ARN (trust alone isn't enough); caller calls `AssumeRole`, gets temporary creds (`AccessKeyId`+`SecretAccessKey`+`SessionToken`, 15min–12h), uses them against Account B's resource.

```csharp
var stsClient = new AmazonSecurityTokenServiceClient();
var request = new AssumeRoleRequest
{
    RoleArn = "arn:aws:iam::222222222222:role/CrossAccountReadRole",
    RoleSessionName = "CrossAccountSession"
};
var response = await stsClient.AssumeRoleAsync(request);
var creds = response.Credentials; // use against Account B resources
```

**Q: What's the confused-deputy problem, and how do External ID and GitHub OIDC each solve a version of it?**

A: A shared role serving many customers could be tricked into acting against the wrong account. **External ID** (`sts:ExternalId` condition) — vendor/third-party access is scoped to a unique ID you issue them. **GitHub OIDC** — `Federated` principal is the GitHub OIDC provider, condition checks `aud=sts.amazonaws.com` and `sub` StringLike `repo:org/repo:*` — no static keys, short-lived, scoped per repo/branch.

```json
{
  "Effect": "Allow",
  "Principal": { "AWS": "arn:aws:iam::111111111111:root" },
  "Action": "sts:AssumeRole",
  "Condition": { "StringEquals": { "sts:ExternalId": "vendor-unique-id-123" } }
}
```

```json
{
  "Effect": "Allow",
  "Principal": { "Federated": "arn:aws:iam::111111111111:oidc-provider/token.actions.githubusercontent.com" },
  "Action": "sts:AssumeRoleWithWebIdentity",
  "Condition": {
    "StringEquals": { "token.actions.githubusercontent.com:aud": "sts.amazonaws.com" },
    "StringLike": { "token.actions.githubusercontent.com:sub": "repo:my-org/my-repo:*" }
  }
}
```

**Q: How does a role actually reach different compute types?**

A: **EC2** — via an **instance profile** (thin wrapper holding exactly one role; console creates it silently, Terraform/CFN requires declaring it explicitly). **Lambda** — execution role assumed by `lambda.amazonaws.com`. **ECS** — two separate roles: **task execution role** (ECS agent — pulls image, writes logs) vs **task role** (your app code); mixing them up is a real production bug. **EKS** — IRSA or EKS Pod Identity maps a K8s service account to an IAM role per pod.

**Q: Why does IMDSv2 matter for security, and what should you always set?**

A: IMDSv1 answers a plain `GET`, so any SSRF bug can steal the instance role's credentials. IMDSv2 requires a `PUT` for a session token first, which simple SSRF can't do. Always set `HttpTokens: required` and `HttpPutResponseHopLimit: 1` in the launch template.

```bash
# IMDSv2 (session-oriented, and what you should require)
TOKEN=$(curl -X PUT "http://169.254.169.254/latest/api/token" \
  -H "X-aws-ec2-metadata-token-ttl-seconds: 21600")
curl -H "X-aws-ec2-metadata-token: $TOKEN" \
  http://169.254.169.254/latest/meta-data/iam/security-credentials/
```

**Q: What is `iam:PassRole`, why is it a privilege-escalation risk, and how do you scope it?**

A: It's "let me *hand* this role to a service" (distinct from `sts:AssumeRole` = "let me *become* it"). Unrestricted, `lambda:CreateFunction` + unrestricted `PassRole` lets a user create a Lambda with `AdministratorAccess` and run code as admin. Scope it to specific role ARNs with an `iam:PassedToService` condition.

```json
{
  "Effect": "Allow",
  "Action": "iam:PassRole",
  "Resource": "arn:aws:iam::123456789012:role/lambda-prod-order-writer-role",
  "Condition": { "StringEquals": { "iam:PassedToService": "lambda.amazonaws.com" } }
}
```

**Q: What's the modern answer for human access, and when do IAM users still make sense?**

A: **IAM Identity Center** (permission sets materialized as per-account roles, short-lived credentials via `aws sso login`, single offboarding point) — not IAM users, for humans. Reserve IAM users for legacy apps that can't assume a role, plus one break-glass account.

### Hands-On: IAM Roles

**Q: What two commands prove the "role beats access keys" argument on an EC2 instance?**

A: Before attaching the role: `aws s3 ls` → "Unable to locate credentials". After attaching: `aws sts get-caller-identity` → `assumed-role/...` ARN, then `aws s3 ls` works — with zero access keys anywhere on the box, and the change took effect with no reboot.

```bash
# BEFORE attaching the role, on the instance:
aws s3 ls
# → "Unable to locate credentials"

# AFTER attaching:
aws sts get-caller-identity
# → arn:aws:sts::123456789012:assumed-role/ec2-dev-s3-reader-role/i-0abc123
aws s3 ls        # works — and there are no access keys anywhere on the box
```

**Q: How do you assume a cross-account role from the CLI, and verify it worked?**

A: Either one-off with `aws sts assume-role`, or — cleaner for day-to-day work — let the CLI do the assumption for you via a named profile in `~/.aws/config`:

```bash
aws sts assume-role \
  --role-arn arn:aws:iam::222222222222:role/CrossAccountReadRole \
  --role-session-name parteek-audit-2026-08 \
  --duration-seconds 3600
# export the three values, then verify you actually became the role:
aws sts get-caller-identity
```

```ini
[profile prod-audit]
role_arn       = arn:aws:iam::222222222222:role/CrossAccountReadRole
source_profile = dev
mfa_serial     = arn:aws:iam::111111111111:mfa/parteek
```

```bash
aws s3 ls --profile prod-audit    # CLI assumes the role and caches/refreshes the session
```

### IAM Security Tools

**Q: Pair each IAM security tool with the question it answers.**

A:

- **Credential Report** — what shouldn't exist (no MFA, stale/never-used keys)
- **Access Advisor** — what's over-granted (services allowed but unused)
- **IAM Access Analyzer** — what's exposed externally, plus policy validation and generate-policy-from-CloudTrail
- **Policy Simulator** — would this principal be allowed to do X, before shipping the change
- **CloudTrail** — who did what, when, from where
- **AWS Config** — continuous compliance (`iam-user-mfa-enabled`, `access-keys-rotated`, etc.)

```bash
aws iam generate-credential-report
aws iam get-credential-report --query Content --output text | base64 -d > report.csv
aws iam get-account-authorization-details > iam-snapshot.json     # full policy/role dump for offline review
aws accessanalyzer list-findings --analyzer-arn <arn>
```

### IAM Pitfalls

**Q: What's the golden debugging checklist when "my role has permission but access still fails"?**

A: Execution role permission → trust policy → resource-based policy (bucket/queue/key) → KMS key policy (if encrypted) → SCP/permission boundary.

**Q: Name five common IAM pitfalls and their fixes.**

A:

- **Confusing trust vs permission policy** — check both separately.
- **Overusing `AdministratorAccess`** — start read-only, add incrementally.
- **Overly broad trust `Principal: "*"`** — restrict to specific account/service/OIDC provider + `SourceArn`/`SourceAccount`.
- **Stale env-var keys shadowing the instance role** — env vars rank above IMDS in the credential chain.
- **Permission added to ECS execution role instead of task role** — execution role = agent, task role = app code.

### Secrets Manager vs Parameter Store

**Q: When do you choose Secrets Manager over Parameter Store?**

A: **Secrets Manager** — needs rotation or is a genuine credential (DB passwords, third-party API keys); built-in RDS/Redshift/DocumentDB rotation, KMS-always-encrypted, 64KB max, native cross-region replication. **Parameter Store** — configuration/feature flags/non-rotating settings; Standard tier free, 4KB/8KB max, no native rotation, manual replication. Over-using Secrets Manager for pure config (thousands of values) pays needless rotation/API-call overhead.

```csharp
var client = new AmazonSecretsManagerClient();
var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = "prod/orders/db" });
var connectionString = response.SecretString;
```

#### Secrets Manager — Pitfalls

**Q: Why can secret rotation take an application down, and what's the fix?**

A: Rotation creates a new version and moves the `AWSCURRENT` label to it (old becomes `AWSPREVIOUS`); an app that reads the secret once at startup and caches it forever keeps using the invalidated password once rotation completes. Fix: re-fetch `AWSCURRENT` on auth failure and retry once — or better, use RDS Proxy/IAM database authentication to remove the password from the app entirely.

**Q: What's the most common performance mistake with Secrets Manager, and how do you avoid it?**

A: Calling `GetSecretValue` on every request — it's a throttled, per-10,000-calls-billed network call. Cache in memory with a TTL via the Secrets Manager caching library or the Parameters and Secrets Lambda extension (local HTTP cache sidecar).

**Q: List three other Secrets Manager gotchas worth knowing.**

A:

- Needs **both** `secretsmanager:GetSecretValue` **and** `kms:Decrypt` when using a customer-managed key.
- Deletion has a mandatory 7–30 day recovery window (breaks teardown/recreate CI unless you use `ForceDeleteWithoutRecovery`).
- ECS `valueFrom`/Lambda env-var injection resolves **once at start** — rotating the secret doesn't update a running task; you must redeploy or read it in code.

### Least Privilege & Permission Boundaries in Practice

**Q: Rewrite `{"Effect":"Allow","Action":"dynamodb:*","Resource":"*"}` as least privilege with tenant isolation.**

A: Anti-pattern (seen constantly in real .NET/Lambda code):

```json
{ "Effect": "Allow", "Action": "dynamodb:*", "Resource": "*" }
```

Least-privilege version:

```json
{
  "Effect": "Allow",
  "Action": ["dynamodb:GetItem", "dynamodb:PutItem", "dynamodb:UpdateItem"],
  "Resource": "arn:aws:dynamodb:us-east-1:123456789012:table/Orders",
  "Condition": { "ForAllValues:StringEquals": { "dynamodb:LeadingKeys": ["${aws:PrincipalTag/TenantId}"] } }
}
```

**Q: Permission boundary vs SCP — what's the structural difference?**

A: A **permission boundary** attaches to an individual role/user and caps its maximum permissions regardless of its own attached policies — used by platform teams to let app teams self-serve roles within a ceiling. An **SCP** attaches at the AWS Organizations account/OU level, limiting everyone in the account including root. Neither ever grants — both only subtract.

### IAM Rapid-Fire Q&A

**Q: Is IAM regional or global?**

A: Global — no region selection, same identities everywhere; side effect is eventual consistency (a fresh role may briefly fail to be usable).

**Q: User vs Group vs Role, in one sentence each?**

A: User = one person with long-term credentials. Group = permission container holding users only (no nesting, not a principal). Role = permissions with no permanent credentials, assumed temporarily by a trusted entity.

**Q: One policy allows an action, another denies it — what wins?**

A: Explicit Deny always wins, overriding even `AdministratorAccess`; no matching policy at all means implicit deny.

**Q: What are the two policies on a role, and how do you tell which is broken?**

A: Trust policy (who can assume — has `Principal`) and permission policy (what it can do). If `AssumeRole` itself fails → trust policy; if assumption succeeds but the API call fails → permission policy.

**Q: What exactly does STS return, and for how long?**

A: `AccessKeyId`, `SecretAccessKey`, `SessionToken`, plus expiry — 15 min to 12h depending on max session duration. Role chaining is hard-capped at 1 hour.

**Q: Can `AdministratorAccess` do everything in an account?**

A: No — root-only actions remain (close account, change support plan, S3 MFA-delete, RI Marketplace seller registration, etc.).

**Q (scenario): A nightly batch job dies almost exactly 60 minutes in — why?**

A: Role chaining — assuming a role from an already-assumed role caps the session at 1 hour regardless of the target role's max-duration setting. Assume the target role directly from the base identity, or refresh credentials rather than holding one long session.

**Q (scenario): EC2 role clearly allows `s3:GetObject`, but the app gets AccessDenied — where do you look?**

A: `aws sts get-caller-identity` first (stale env vars shadowing the instance profile show as `user/...` not `assumed-role/...`); then bucket policy; then KMS key policy if SSE-KMS; then SCP/permission boundary; also confirm an instance profile actually exists (IaC can create a role without one).

---

## Infrastructure as Code & CI/CD

### AWS CodeCommit

**Q: What is CodeCommit, and why does it come up in interviews despite GitHub dominating in practice?**

A: AWS's own managed Git repo service — same Git semantics, but access-controlled through IAM instead of a third-party SaaS account. The point being tested is recognizing it's IAM-native (no separate permission model to reconcile), not that you'd choose it over GitHub for a real team.

### AWS CodeBuild

**Q: What is CodeBuild, and what are its core concepts?**

A: Fully managed CI service — compiles code, runs tests, produces artifacts in on-demand isolated containers, no Jenkins to patch/scale. **Build Project** (source/env/steps/artifact config), **Build Environment** (OS+runtime+compute size+privileged mode for Docker-in-Docker), **buildspec.yml** (script as YAML) with phases INSTALL → PRE_BUILD → BUILD → POST_BUILD.

```yaml
version: 0.2
phases:
  install:
    commands: [echo Installing dependencies]
  pre_build:
    commands: [echo Pre-build steps]
  build:
    commands: [echo Building application]
  post_build:
    commands: [echo Build completed]
artifacts:
  files: ['**/*']
```

**Q: What's the #1 "works standalone, fails in VPC" CodeBuild bug, and how is cost controlled?**

A: Forgetting a **NAT Gateway** when the build runs inside a VPC to reach private RDS/APIs. Cost is pay-per-build-minute × compute size (no idle cost); optimize with smallest sufficient compute, fail-fast, and dependency caching (S3 or local Maven/npm/NuGet cache).

### AWS CodePipeline

**Q: What does CodePipeline actually do, and what are its core concepts?**

A: It's an **orchestrator** — doesn't compile/test/deploy itself, coordinates Source→Build→Test→Deploy stages across CodeBuild/CodeDeploy/ECS/Lambda/CloudFormation. Pipeline (workflow) → Stage (sequential) → Action (task within a stage; actions within a stage can run in parallel).

**Q: What is a CodeDeploy Deployment Group, and what are the four pipeline execution states?**

A: Deployment Group = the logical target set (EC2/ASG, ECS service, or Lambda function+alias) a CodeDeploy application deploys to, and where the strategy (in-place/blue-green, rolling %, alarm-triggered rollback) is bound. Pipeline execution states: **Started, Succeeded, Failed, Stopped**.

**Q: How is CodePipeline billed, and how does it differ from CodeDeploy and Jenkins?**

A: Billed per active pipeline per month, not per execution (runs are free at that layer). CodePipeline orchestrates the whole release workflow; CodeDeploy only handles the deployment step; Jenkins is fully custom/self-hosted with maximum flexibility.

### CodePipeline/CodeBuild Trap Scenarios

**Q: A pipeline fails immediately at the very first stage — what's the likely cause?**

A: Source-stage authentication broken — commonly an expired/revoked **CodeStar Connections** connection to GitHub/Bitbucket, which needs a manual "Update pending connection" re-authorization in the console.

**Q: List three other common CodePipeline/CodeBuild trap symptoms and root causes.**

A:

- Docker build fails in CodeBuild but works locally → privileged mode not enabled, or missing ECR login.
- ECS runs old image after a successful build → service not updated, or tag is static `:latest` instead of unique tag/digest.
- Build fails only inside a VPC → missing NAT Gateway/VPC endpoint for AWS service access.

**Q: What's the senior-level summary of debugging CI/CD failures in AWS?**

A: Most failures are IAM misconfigurations, artifact-handling mistakes, missing Docker privileges, VPC networking gaps, or role-boundary confusion — debugging AWS CI/CD is primarily a permissions exercise, not a build-command exercise.

### CloudFormation vs Terraform/CDKTF

**Q: Compare CloudFormation, Terraform, and CDKTF on scope, state management, and rollback.**

A:

| | CloudFormation | Terraform | CDKTF |
|---|---|---|---|
| Scope | AWS-only | Multi-cloud | Multi-cloud (Terraform under the hood) |
| Language | JSON/YAML | HCL | TypeScript/Python/C#/Java/Go → synthesizes to Terraform JSON |
| State | AWS-managed, no file | You own it — S3 backend + DynamoDB lock table | Same as Terraform |
| Rollback on failure | Automatic | None — manage remediation yourself | Same as Terraform |

The classic production state backend — S3 for the state file, DynamoDB purely for locking:

```hcl
# Terraform backend config — S3 for state, DynamoDB for locking
terraform {
  backend "s3" {
    bucket         = "my-org-terraform-state"
    key            = "prod/app/terraform.tfstate"
    region         = "us-east-1"
    dynamodb_table = "terraform-state-lock"
    encrypt        = true
  }
}
```

**Q: Why is Terraform state a security concern, and what's the mitigation?**

A: State contains secrets in **plaintext** (RDS passwords, generated keys) — `sensitive = true` only redacts CLI output, not the state file itself. Mitigate: encrypt the state bucket (SSE-KMS), block public access, restrict bucket policy to the pipeline role, enable versioning, and keep real secrets in Secrets Manager/Parameter Store instead of passing them through Terraform.

**Q: What does CDKTF change versus plain Terraform, and what stays the same?**

A: CDKTF replaces only the **authoring language** — you write TypeScript/Python/C#/etc., `cdktf synth` compiles it into the same JSON Terraform normally consumes, then the standard Terraform CLI takes over. State model, backend, and engine are unchanged.

Concrete side-by-side — an S3 bucket in HCL vs CDKTF (TypeScript):

```hcl
# Terraform (HCL)
resource "aws_s3_bucket" "app_data" {
  bucket = "my-app-data-prod"
}

resource "aws_s3_bucket_versioning" "app_data_versioning" {
  bucket = aws_s3_bucket.app_data.id
  versioning_configuration {
    status = "Enabled"
  }
}
```

```typescript
// CDKTF (TypeScript) — equivalent resource
import { S3Bucket } from "@cdktf/provider-aws/lib/s3-bucket";
import { S3BucketVersioningA } from "@cdktf/provider-aws/lib/s3-bucket-versioning";

const appData = new S3Bucket(this, "app_data", {
  bucket: "my-app-data-prod",
});

new S3BucketVersioningA(this, "app_data_versioning", {
  bucket: appData.id,
  versioningConfiguration: {
    status: "Enabled",
  },
});
```

### Terraform/CDKTF in Practice — Depth Questions to Expect

**Q: Why does applying a saved plan file matter for a trustworthy CI/CD pipeline?**

A: `terraform apply` without a saved plan re-plans at apply time, so what runs may differ from what was reviewed. `terraform plan -out=tf.plan` then `terraform apply tf.plan` guarantees the applied change is exactly the reviewed one — the difference between a real gate and a rubber stamp.

```bash
terraform init         # download providers/modules, configure backend
terraform fmt -check   # formatting gate in CI
terraform validate     # syntax/type check, no AWS calls
terraform plan -out=tf.plan     # the dry-run diff — the artifact a reviewer should read
terraform apply tf.plan         # apply exactly what was reviewed, no re-plan drift
terraform destroy
```

**Q: `for_each` vs `count` — what's the highest-value practical Terraform question, and the right answer?**

A: `count` indexes by **position** — removing a middle list item shifts every subsequent index, causing Terraform to destroy/recreate resources that shouldn't have changed. `for_each` keys by a **stable string**, so removing one only affects that one. Use `for_each` for anything you'll add/remove from; reserve `count` for a simple on/off toggle.

**Q: Workspaces vs directory-per-environment — which is the production pattern?**

A: **Directory (or repo) per environment** with a shared module is the production pattern — separate state/backend/credentials, environments can legitimately differ. **Workspaces** share one backend key per workspace — cheap but risks running `apply` against the wrong environment; fine for dev/test variants only.

**Q: What's the state-file recovery process, and how do you prevent concurrent-apply corruption?**

A: Restore from **S3 object versioning** on the state bucket (or `terraform import` resources back in as a last resort). Concurrent applies are prevented by the **DynamoDB lock table** — the second pipeline run waits or fails.

**Q: How do you adopt a console-created ("ClickOps") resource into Terraform management, and how do you remove one from state without deleting it?**

A: `terraform import aws_s3_bucket.app_data my-existing-bucket` brings it under management. `terraform state rm` removes it from state **without** deleting it in AWS — the escape hatch when a resource must be managed elsewhere.

```bash
terraform import aws_s3_bucket.app_data my-existing-bucket   # bring unmanaged resources under management
terraform state list / show / mv / rm                        # refactor or drop state entries
terraform plan -refresh-only                                 # detect drift without proposing changes
```

**Q: What's the recommended GitHub Actions + Terraform CI/CD pattern?**

A: PR opened → fmt/validate/tflint/tfsec-checkov → `plan` posted as a PR comment (read-only role). PR merged → `apply` from the saved plan (privileged role, protected environment, manual approval for prod). Authenticate via **GitHub OIDC assuming an AWS role** — no long-lived keys in secrets.

```
PR opened   → fmt, validate, tflint, tfsec/checkov → terraform plan → post plan as a PR comment
PR merged   → terraform apply <saved plan>          (protected environment, manual approval for prod)
```

**Q: What are CloudFormation's equivalents to Terraform modules and multi-account deployment?**

A: **Nested stacks** = modules (also work around per-stack resource limits). **StackSets** = deploy one template across many accounts/regions from the management account — genuinely easier than Terraform's multi-state/provider-per-account approach for that specific use case.

**Q: CDK vs CDKTF — what's the difference?**

A: AWS CDK synthesizes **CloudFormation** (AWS-only, AWS-managed state). **CDKTF** synthesizes **Terraform** (multi-cloud, self-managed state). Same authoring ergonomics, different engine underneath.

---

## S3

### S3 Buckets & Objects

**Q: What is S3, and what are the bucket naming/scoping rules?**

A: Infinitely scalable **object** storage (not a filesystem) — flat key/value store, 11 nines durability, no capacity to provision. Bucket names are globally unique (DNS-embedded), 3–63 chars, lowercase/numbers/hyphens/dots, live in **one region** permanently, and cannot be nested.

**Q: What is the S3 key really, and what changed about S3's consistency model in Dec 2020?**

A: The key is the **full flat path** — S3 has no real directories; "folders" are rendered from prefixes + `/` delimiter. Since Dec 2020, S3 gives **strong read-after-write consistency** for PUTs/overwrites/DELETEs and consistent LIST — "eventually consistent for overwrites" is an outdated answer.

**Q: What's the object size limit, and when is multipart upload required?**

A: Max object size 5TB; a single PUT caps at 5GB, so multipart upload is **mandatory** beyond that and recommended above ~100MB.

**Q: Show the core S3 CLI commands, and the difference between `aws s3` and `aws s3api`.**

A: `aws s3` is the high-level convenience layer (`cp`, `sync`, `mv`); `aws s3api` exposes the raw per-call API (`put-object`, `put-bucket-policy`) when exact control is needed.

```bash
aws s3 mb s3://my-unique-bucket-name --region us-east-1
aws s3 cp ./report.pdf s3://my-bucket/invoices/2026/08/report.pdf
aws s3 ls s3://my-bucket/invoices/2026/ --recursive --human-readable --summarize
aws s3 sync ./local-dir s3://my-bucket/prefix/ --delete    # ⚠ --delete removes remote extras
```

### S3 Bucket Policies & Access Control

**Q: What four mechanisms grant S3 access, and which should you avoid?**

A: **IAM policy** (identity-based, default for your own workloads), **bucket policy** (resource-based, cross-account/whole-bucket rules, needs `Principal`), **ACLs** (legacy — avoid; disable via Object Ownership = "Bucket owner enforced"), **Access Points** (many-team access at scale).

**Q: Write a bucket policy denying unencrypted uploads and insecure transport.**

A:

```json
{ "Sid": "DenyUnEncryptedUploads", "Effect": "Deny", "Principal": "*", "Action": "s3:PutObject",
  "Resource": "arn:aws:s3:::my-bucket/*",
  "Condition": { "StringNotEquals": { "s3:x-amz-server-side-encryption": "aws:kms" } } },
{ "Sid": "DenyInsecureTransport", "Effect": "Deny", "Principal": "*", "Action": "s3:*",
  "Resource": ["arn:aws:s3:::my-bucket", "arn:aws:s3:::my-bucket/*"],
  "Condition": { "Bool": { "aws:SecureTransport": "false" } } }
```

**Q: What is Block Public Access (BPA), and what happens if account-level and bucket-level settings conflict?**

A: Four independent toggles (block new/all public ACLs, block new/all public bucket policies), on by default since 2023, available at account and bucket level. **Account-level BPA overrides bucket-level** — if a `Principal: "*"` bucket policy has no effect, BPA is why.

### S3 Static Website Hosting

**Q: Why must you put CloudFront in front of an S3 static website for anything real?**

A: The S3 website endpoint is **HTTP only** — no HTTPS support. CloudFront + Origin Access Control (OAC) gives TLS via ACM, custom domain, caching, and lets the bucket stay fully private.

```
http://my-bucket.s3-website-us-east-1.amazonaws.com
http://my-bucket.s3-website.us-east-1.amazonaws.com     # region-dependent format
```

**Q: REST endpoint vs website endpoint — what's the difference?**

A: REST endpoint (`bucket.s3.region.amazonaws.com`) supports HTTPS + IAM/SigV4 but no index-document behavior. Website endpoint gives index/error documents and redirects but is HTTP-only and public-read.

### S3 Versioning & Replication

**Q: What does versioning actually protect against, and what's the cost gotcha?**

A: Protects against accidental overwrite/delete — a DELETE just adds a **delete marker** (data isn't removed; permanent deletion requires the version ID). Once enabled it can only be **suspended**, never off. Cost gotcha: you pay for every version — always pair with a lifecycle rule expiring noncurrent versions.

**Q: CRR vs SRR, and what are the replication rules/gotchas?**

A: CRR (cross-region) = DR/latency/compliance; SRR (same-region) = log aggregation, prod→test seeding. Rules: versioning required on both sides; replication is **asynchronous and not retroactive** (use S3 Batch Replication to backfill); delete markers optionally replicate but **permanent version deletes never replicate**; **no chaining** (A→B→C doesn't propagate A's objects to C).

### S3 Performance, Analytics & Cost Tooling

**Q: What's S3's baseline request-rate limit, and how do you scale beyond it?**

A: 3,500 PUT/COPY/POST/DELETE and 5,500 GET/HEAD requests per second, **per prefix**, with no limit on number of prefixes — scale by spreading keys across many prefixes and reading/writing in parallel.

**Q: What's the difference between multipart upload, Transfer Acceleration, byte-range fetch, and S3 Select?**

A: **Multipart upload** — parallel, resumable parts, required >5GB. **Transfer Acceleration** — routes uploads through the nearest CloudFront edge over the AWS backbone, for long-distance uploads. **Byte-range fetch** — parallel GETs of different ranges, or partial reads. **S3 Select** — server-side SQL over one object, cutting transfer/CPU (use Athena for queries across many objects).

### S3 Batch Operations

**Q: You have 40 million objects that are unencrypted or in the wrong storage class — what's the right tool?**

A: **S3 Batch Operations** — generate an S3 Inventory report (or your own CSV) as the manifest, then run a Batch Operations job (copy, replace tags/ACLs, restore from Glacier, apply Object Lock, or invoke a Lambda per object) with managed retries and a completion report. Not a custom loop script.

### S3 Requester Pays

**Q: What does Requester Pays change, and what must the requester do differently?**

A: The requester pays request + egress costs while the owner still pays storage. Requester must be an authenticated principal (no anonymous access) and must send `x-amz-request-payer: requester` per request, or get a 403. Use case: distributing large public datasets without absorbing others' egress bills.

### S3 Best Practices

**Q: List the core S3 best practices to recite.**

A: Account-level BPA on + disable ACLs; enforce encryption/HTTPS via `Deny` bucket policy; versioning + lifecycle expiring noncurrent versions + abort incomplete multipart uploads; lifecycle driven by Storage Class Analysis or Intelligent-Tiering when unknown; spread keys across prefixes for throughput; CloudFront+OAC for public content, never a public bucket; CRR for what you can't lose (not a substitute for versioning).

### S3 Shared Responsibility Model

**Q: What's the one-liner for S3's shared responsibility split?**

A: "S3 has never lost my data — but S3 will absolutely let me make it public. Durability is AWS's job; access control and retention are mine." AWS: infrastructure, 11-nines durability, encryption options. You: bucket/IAM policies, BPA, versioning/replication/lifecycle config, enabling and reviewing logs.

### S3 Storage Classes & Lifecycle Policies

**Q: Rank the S3 storage classes by retrieval speed and minimum storage duration.**

A: Standard (immediate, no minimum) → Intelligent-Tiering (immediate, auto-tiers) → Standard-IA (immediate, 30-day min) → One Zone-IA (immediate, 30-day min, single AZ) → Glacier Instant Retrieval (immediate, 90-day min) → Glacier Flexible Retrieval (minutes–hours, 90-day min) → Glacier Deep Archive (~12h, 180-day min).

**Q: What are the storage-class gotchas an interviewer expects?**

A: Standard-IA/One Zone-IA charge a retrieval fee (only for genuinely infrequent access); minimum-duration charges apply even on early delete/transition; Intelligent-Tiering has a small per-object monitoring fee but removes guesswork; versioned old versions need their own lifecycle rule or you pay for every historical version forever.

**Lifecycle policy pattern (typical senior answer):**

```json
{
  "Rules": [
    {
      "ID": "MoveToIAThenGlacier",
      "Status": "Enabled",
      "Filter": { "Prefix": "logs/" },
      "Transitions": [
        { "Days": 30, "StorageClass": "STANDARD_IA" },
        { "Days": 90, "StorageClass": "GLACIER" }
      ],
      "Expiration": { "Days": 365 }
    }
  ]
}
```

### S3 Lifecycle Rules in Practice — Real Patterns & Terraform

**Q: What lifecycle pattern fits application logs vs database backups vs unpredictable-access data?**

A: **Logs** — Standard→IA at 30d→Glacier Flexible at 90d→expire at 365d (read often early, rarely later, compliance window). **Backups** — straight to Deep Archive almost immediately (insurance data, 12h restore is fine, cheapest long-term tier). **Unpredictable access** (shared data lake, user uploads) — Intelligent-Tiering from day 1.

**Q: What's the minimum-duration billing trap, concretely?**

A: Transitioning to Standard-IA (30-day min) or Glacier (90-day min) and then deleting/transitioning again before that minimum elapses still bills for the full minimum — aggressive short-interval lifecycle rules on quickly-deleted data can cost *more* than leaving it on Standard and expiring directly.

**Q: How do you fix the "old versions cost money forever" problem in Terraform?**

A: Add a `noncurrent_version_expiration { noncurrent_days = 90 }` rule inside `aws_s3_bucket_lifecycle_configuration` — expires noncurrent (old) versions instead of letting them accumulate indefinitely.

```hcl
resource "aws_s3_bucket" "logs" {
  bucket = "my-app-logs-prod"
}

resource "aws_s3_bucket_lifecycle_configuration" "logs_lifecycle" {
  bucket = aws_s3_bucket.logs.id

  rule {
    id     = "logs-tiering"
    status = "Enabled"

    filter {
      prefix = "logs/"
    }

    transition {
      days          = 30
      storage_class = "STANDARD_IA"
    }

    transition {
      days          = 90
      storage_class = "GLACIER"
    }

    expiration {
      days = 365
    }
  }

  rule {
    id     = "backups-to-deep-archive"
    status = "Enabled"

    filter {
      prefix = "backups/"
    }

    transition {
      days          = 1
      storage_class = "DEEP_ARCHIVE"
    }

    # No expiration — backups are retained indefinitely (or tie to a compliance-driven expiration instead)
  }

  rule {
    id     = "expire-noncurrent-versions"
    status = "Enabled"

    filter {}

    noncurrent_version_expiration {
      noncurrent_days = 90
    }
  }
}
```

### S3 Security: Encryption & Its Four Types

**Q: Compare SSE-S3, SSE-KMS, DSSE-KMS, and SSE-C.**

A:

- **SSE-S3** — AWS holds the key (AES-256), no audit trail, free, default since Jan 2023.
- **SSE-KMS** — your KMS key, every decrypt logged in CloudTrail, needs both `s3:GetObject` **and** `kms:Decrypt`.
- **DSSE-KMS** — KMS key applied twice, for strict dual-layer regulatory mandates.
- **SSE-C** — you supply the key per request, S3 never stores it; HTTPS mandatory; lose the key, lose the object.

**Q: Why can a high-throughput app get KMS-throttled even though S3 is fine, and what's the fix?**

A: Every GET/PUT on an SSE-KMS object calls KMS `Decrypt`/`GenerateDataKey`, which has a per-region request quota. Fix: **S3 Bucket Keys** — reduces KMS request traffic by up to 99% via a bucket-level key deriving per-object keys.

### S3 CORS

**Q: When is a bucket-level CORS rule required, and what commonly trips people up?**

A: Required whenever a web page on a different origin loads assets or makes fetch/XHR calls directly against the bucket — CORS must be configured on the bucket, not the app. Gotchas: `AllowedOrigins` must match scheme+host+port exactly; `ExposeHeaders` is needed for JS to read response headers like `ETag`; presigned browser `PUT` uploads almost always need a CORS rule.

```json
[
  {
    "AllowedOrigins": ["https://www.example.com"],
    "AllowedMethods": ["GET", "PUT"],
    "AllowedHeaders": ["*"],
    "ExposeHeaders": ["ETag"],
    "MaxAgeSeconds": 3000
  }
]
```

### S3 MFA Delete

**Q: What does MFA Delete protect, and what are its constraints?**

A: Requires an MFA code to permanently delete an object version or suspend versioning. Requires versioning enabled first; can only be toggled by the **root account** via CLI/API (not console). Many orgs prefer Object Lock instead since it avoids handing out root credentials.

### S3 Access Logs (and the Warning)

**Q: What's the critical warning about S3 server access logging, and what's the alternative for security-relevant audit?**

A: **Never log a bucket to itself** — logging is itself a request, creating an infinite logging loop and runaway bill; always log to a separate dedicated bucket. For "who deleted this object", use **CloudTrail data events** (near-real-time, guaranteed delivery, includes IAM identity) instead of access logs (free-ish, delayed, best-effort).

### S3 Pre-Signed URLs

**Q: What permissions does a presigned URL carry, and what's the most common presigned-URL bug in serverless apps?**

A: It carries the **generator's permissions** — anyone with the link can use it, like a bearer token. Bug: if generated with **temporary role credentials** (Lambda/ECS/EC2), the URL stops working when those credentials expire (~1h), regardless of the requested expiry.

**Q: What's the senior framing for when to use presigned URLs?**

A: Direct browser upload/download (presigned PUT/GET or presigned POST policy) so large files never traverse your Lambda/ECS compute tier — "don't stream the file through your compute."

```bash
aws s3 presign s3://my-bucket/private/report.pdf --expires-in 3600
```

```csharp
var request = new GetPreSignedUrlRequest
{
    BucketName = "my-bucket",
    Key        = "private/report.pdf",
    Expires    = DateTime.UtcNow.AddMinutes(15),
    Verb       = HttpVerb.GET          // HttpVerb.PUT for direct browser upload
};
string url = s3Client.GetPreSignedURL(request);
```

### S3 Object Lock & Glacier Vault Lock

**Q: Governance mode vs Compliance mode — what's the distinction, and which survives a malicious admin?**

A: **Governance** — users with `s3:BypassGovernanceRetention` can override; a guardrail with an escape hatch. **Compliance** — nobody, not even root, can delete/alter until retention expires; genuinely immutable, the strongest answer to "protect backups from ransomware or a malicious insider with admin rights."

**Q: What's Legal Hold, and how does it differ from a retention period?**

A: An independent on/off flag with no expiry, controlled by `s3:PutObjectLegalHold` — used for litigation holds where the end date is unknown, separate from any Object Lock retention period.

### S3 Access Points & Object Lambda

**Q: What problem do S3 Access Points solve, and what does Object Lambda add?**

A: Access Points give each team/use case its own named endpoint + policy, so one giant bucket policy doesn't have to serve everyone — can be VPC-restricted. **Object Lambda** runs a Lambda on the GET path to transform data (redact PII, resize images, filter rows) without maintaining a second copy.

### S3 Security Best Practices & Shared Responsibility

**Q: What's the defense-in-depth layering for securing an S3 bucket, top to bottom?**

A: Block Public Access → bucket policy/IAM → encryption at rest and in transit → versioning + Object Lock → logging and monitoring → VPC endpoint (so traffic never leaves the AWS network). S3 breaches are essentially always configuration failures, never durability failures.

---

## EC2 & Instance Storage

### EC2 Fundamentals

**Q: What's the difference between Stop and Terminate?**

A: **Stop** — EBS data persists, instance ID kept, you stop paying compute but still pay for EBS. **Terminate** — instance and (by default) the root EBS volume are destroyed.

**Q: List the four EC2 pricing models and their risk/commitment trade-off.**

A: On-Demand (no commitment, highest cost, no risk); Reserved Instances (1–3yr, lower cost, locked in); Savings Plans (1–3yr $/hr commitment, more flexible than RI); Spot (no commitment, cheapest, reclaimable with a 2-minute warning).

### EC2 Instance Types, User Data & Metadata

**Q: Decode `m6g.2xlarge`, and why is the Graviton (`g`) attribute worth mentioning unprompted for .NET workloads?**

A: `m` = general purpose (~4GiB/vCPU), `6` = 6th gen, `g` = Graviton (ARM64), `2xlarge` = 8 vCPU/32GiB. .NET has supported ARM64 since .NET 6, so moving to a Graviton instance is usually a ~20% cost saving with just a recompile, no code changes.

```
m      5       dn        .2xlarge
│      │       │          └─ size (vCPU/memory scale)
│      │       └─ extra attributes
│      └─ generation (higher = newer, usually better price/performance)
└─ family (workload class)
```

#### User Data — Quick Recall / Full Explanation

**Q: What is EC2 user data, when does it run, and what's the most common mistake with it?**

A: A startup script (max 16KB) that cloud-init/EC2Launch runs **once, as root, on first boot only** — it does not re-run on reboot. Most common mistakes: omitting the `#!/bin/bash` shebang (script silently ignored), and expecting an app started via user data to survive a reboot (fix: install it as a proper systemd service, not re-run user data).

```bash
#!/bin/bash
yum update -y
yum install -y amazon-cloudwatch-agent
systemctl enable --now amazon-cloudwatch-agent
```

**Q: The 16KB user-data limit is small — what's the pattern for a larger bootstrap?**

A: Keep user data tiny and fetch the rest from S3, using the instance's IAM role for credentials (no keys on the box):

```bash
#!/bin/bash
aws s3 cp s3://my-bucket/bootstrap.sh /tmp/bootstrap.sh
bash /tmp/bootstrap.sh
```

**Q: Why does a failed user-data script not fail the instance, and where do you debug it?**

A: EC2 reports `running` and status checks pass green even if the script errored — nothing surfaces the failure automatically. Check `/var/log/cloud-init-output.log` first whenever "the instance came up but nothing's installed."

**Q: Why is "golden AMI + thin user data" the senior-preferred pattern over heavy user-data bootstrapping?**

A: Heavy user data is slow (re-downloads hundreds of MB on every launch, exactly when an ASG needs to scale fast), not reproducible (package versions drift by launch date), and fragile (a brief repo outage leaves an instance that boots "healthy" with no runtime installed — triggering an ASG launch loop as health checks fail and replacements repeat the same failure forever).

#### Instance Metadata (IMDS)

**Q: What is IMDS, and why does it work with no internet access or credentials?**

A: A link-local service (`169.254.169.254`) every instance queries for facts about itself, answered locally by the Nitro hypervisor — never routes over the internet. Most important use: fetching the instance role's temporary IAM credentials, which the SDK refreshes automatically.

```bash
curl http://169.254.169.254/latest/meta-data/instance-id
curl http://169.254.169.254/latest/meta-data/placement/availability-zone
curl http://169.254.169.254/latest/meta-data/iam/security-credentials/my-role   # ← temporary IAM credentials
```

**Q: User data vs instance metadata — what's the directional difference?**

A: User data = instructions **you** provide to configure the instance ("do this on startup"). Instance metadata = facts **AWS** provides about the instance ("who am I, what are my credentials") — delivered through the same endpoint family but opposite direction.

**Q: Why must you require IMDSv2, and what's the historical breach it prevents?**

A: IMDSv1 answers a plain GET, so an SSRF bug in your app can be aimed at the metadata endpoint to steal the instance role's credentials (the mechanism behind the Capital One breach). IMDSv2 requires a PUT with a header to get a session token first — simple SSRF can't do that. Enforce with `HttpTokens: required`.

### Security Groups, Their Properties & Classic Ports

**Q: What are the defining properties of a Security Group, and how does it differ from a NACL?**

A: SG: attaches to an ENI (not the instance), **allow-only** rules, **stateful** (return traffic auto-allowed), default deny-all-inbound/allow-all-outbound, changes apply immediately. NACL: attaches to a subnet, allow **and** deny rules, **stateless** (must explicitly allow both directions — including the ephemeral port range 1024–65535 for return traffic), evaluated in numbered order (first match wins).

**Q: Why should security groups reference other security groups instead of CIDR blocks?**

A: `App-SG: inbound 8080 from ALB-SG` (not a CIDR) means the app tier stays reachable only through the ALB and keeps working automatically as instance IPs change or the ASG scales — no rule updates ever needed.

```
ALB-SG:  inbound 443 from 0.0.0.0/0
App-SG:  inbound 8080 from ALB-SG      ← not a CIDR
DB-SG:   inbound 5432 from App-SG      ← not a CIDR
```

**Q: "Connection times out" vs "connection refused" — what does each tell you?**

A: Timeout/hang = network-layer problem (SG, NACL, route table, wrong subnet). "Connection refused" = the network reached the host fine; nothing is listening on that port — an application-layer problem.

### Public IP vs Private IP vs Elastic IP

**Q: What's the classic Elastic IP gotcha, and what's the senior alternative to reaching for one?**

A: A public IP is released on stop and a **new one assigned on start** — anything hardcoded to it (DNS, partner allowlists) breaks. Rather than an EIP, prefer a load balancer (stable DNS name), Route 53 alias record, or a NAT Gateway for outbound-only needs. Legitimate EIP uses: partner firewall allowlisting a fixed IP, NAT gateway, fast manual failover.

### Placement Groups

**Q: Cluster vs Spread vs Partition placement groups — what's the trade-off of each?**

A: **Cluster** — same rack, single AZ; best network performance, worst blast radius (one rack failure takes everything). **Spread** — distinct hardware across AZs; max isolation, capped at 7 instances/AZ/group. **Partition** — grouped racks, up to 7 partitions/AZ; middle ground for rack-aware distributed systems (Kafka, Cassandra, HDFS).

### Elastic Network Interfaces (ENIs)

**Q: What's the cheap-failover trick using secondary ENIs?**

A: The primary ENI (`eth0`) can't be detached, but a secondary ENI can be detached and reattached to a different instance **in the same AZ** — moving the ENI (with its IP, MAC, and SGs) makes traffic follow it to a standby instance.

### EC2 Hibernate

**Q: How does Hibernate differ from Stop, and what are its key requirements?**

A: Hibernate dumps RAM to the encrypted root EBS volume and shuts down; on restart RAM is restored and processes resume — no boot, no warm-up. Requires: EBS-encrypted root volume large enough for the RAM image, supported family/size with RAM under 150GiB, enabled **at launch** only, max 60 days hibernated.

### EC2 Purchasing Options — The Complete Set

**Q: Dedicated Instances vs Dedicated Hosts — what's the deciding factor?**

A: Both are single-tenant hardware, but only **Dedicated Hosts** expose the physical server (sockets/cores/host affinity) — required for per-socket/per-core BYOL licensing (Windows Server, SQL Server, Oracle). If the question mentions bringing your own license, the answer is Dedicated Hosts.

**Q: Capacity Reservation vs Reserved Instance/Savings Plan — what's the distinction?**

A: RI/Savings Plan = a **billing** construct (discount, no capacity guarantee). Capacity Reservation = a **capacity** construct (guaranteed availability at On-Demand rates, no discount). Combine both to get guarantee + discount.

### EC2 Shared Responsibility Model

**Q: What's the one-liner for EC2's shared-responsibility split?**

A: "AWS is responsible for security *of* the host and everything under the hypervisor; from the guest OS upward — patching, firewall rules, keys, IAM, encryption, backups — it's mine. Lambda moves most of that line onto AWS; EC2 keeps it on me."

### EC2 Sizing, Pricing Decisions & CPU Credit Gotchas

**Q: How do you actually size an EC2 instance rather than just naming pricing models?**

A: Start from workload shape (CPU-bound→`c`, memory-bound→`r`, general→`m`, bursty/idle→`t`); benchmark CPU/memory/network under real load via CloudWatch (+ CloudWatch Agent for memory); target ~40–60% steady-state utilization; let AWS Compute Optimizer recommend right-sizing from actual usage history rather than guessing.

**Q: Explain the T-family CPU credit trap as a production-incident scenario.**

A: T-instances earn CPU credits while running below baseline and spend them to burst above it. In **Standard mode**, exhausting the credit balance hard-throttles CPU back to baseline — "the app was fine for hours, then suddenly sluggish," typically from a sustained load period (batch job, traffic spike) outlasting the credit balance. Diagnosis: check `CPUCreditBalance`/`CPUSurplusCreditBalance`, not just `CPUUtilization` (which looks deceptively capped, not pegged at 100%). **Unlimited mode** avoids throttling but bills extra for sustained bursting — a sign you've outgrown T-family.

```
    CPU running BELOW baseline
              |
              |  earns credits
              v
    +--------------------+
    |   Credit Balance   |
    +---------+----------+
              |
              |  spent when CPU > baseline
              v
     Burst ABOVE baseline
              |
              v
    Credit balance hits zero?
              |
    +---------+-----------+
    |                     |
 Standard mode      Unlimited mode
    |                     |
    v                     v
 Throttled back      Bursts continue,
 to baseline --      but billed extra
 a hard cap, and     per vCPU-hour
 the app slows       beyond baseline
 down                (cost surprise)
```

**Q: What's the decision process for choosing On-Demand vs Reserved/Savings Plan vs Spot — not just the definitions?**

A: Predictable 12+ month capacity need → Savings Plan/RI. Tolerates ~2-min interruption → Spot. Brand-new/unknown steady-state → On-Demand first, commit later. Spans EC2+Fargate+Lambda → Compute Savings Plan (flexible) over EC2 Instance SP. Business-hours-only dev/test → On-Demand + scheduled stop/start, not a commitment discount.

### EBS vs EFS vs S3

**Q: One-sentence decision rule for EBS vs EFS vs S3?**

A: One instance needs a fast disk → **EBS**. Many instances need the same files concurrently → **EFS** (or FSx on Windows). Unlimited HTTP clients, object access → **S3**.

### EBS Volumes

**Q: What single fact about EBS explains most of its behavior, and what are the key properties?**

A: EBS is a **network drive**, not a physical disk — hence network latency, and it can be detached/reattached and survives the instance. Properties: locked to **one AZ** (move via snapshot+recreate); one instance at a time (except Multi-Attach); provisioned capacity billed regardless of usage; online resize (grow only, never shrink); `DeleteOnTermination` defaults **true for root, false for additional volumes** — a real cost/data-loss trap.

```bash
aws ec2 create-volume --availability-zone us-east-1a --size 100 --volume-type gp3 --encrypted
aws ec2 attach-volume --volume-id vol-abc --instance-id i-abc --device /dev/sdf
# on the instance: format (first time only!) and mount
lsblk                                   # confirm the device is visible
sudo mkfs -t xfs /dev/nvme1n1           # ⚠ destroys data — never run on a volume with data
sudo mkdir /data && sudo mount /dev/nvme1n1 /data
# add to /etc/fstab (by UUID, not device name) so it survives a reboot
```

**Q: What are the two classic EBS mounting gotchas?**

A: (1) The device name you request (`/dev/sdf`) isn't what Linux uses on Nitro instances (`/dev/nvme1n1`) — always `lsblk` first, since running `mkfs` on the wrong device destroys the root volume:

```
NAME          SIZE TYPE MOUNTPOINTS
nvme0n1         8G disk
└─nvme0n1p1     8G part /          <- root volume, already mounted
nvme1n1       100G disk            <- the volume I just attached
```

NVMe numbering isn't stable across reboots either — map a device back to a real volume ID instead of hardcoding it:

```bash
sudo nvme id-ctrl -v /dev/nvme1n1 | grep -i sn    # serial = the EBS volume ID (vol-0abc…)
ls -l /dev/disk/by-id/                            # stable nvme-Amazon_Elastic_Block_Store_vol… symlinks
```

(2) `mount` is temporary — without an `/etc/fstab` entry (by UUID, with `nofail`), the mount vanishes on reboot, and writes silently go to the root disk instead:

```bash
sudo blkid /dev/nvme1n1        # -> UUID="a1b2c3d4-…" TYPE="xfs"
# /etc/fstab
# UUID=a1b2c3d4-…  /data  xfs  defaults,nofail  0  2
sudo umount /data && sudo mount -a && df -h /data   # ← TEST before rebooting
```

### EBS Volume Types

**Q: gp3 vs gp2 — what's the headline improvement, and which volume types can be boot volumes?**

A: gp3 decouples **IOPS from size** (baseline 3,000 IOPS/125MB/s regardless of size); gp2 required over-provisioning size just to get IOPS (3 IOPS/GB). Only SSD types (gp2/gp3/io1/io2) can be boot volumes — HDD types (st1/sc1) cannot.

### EBS Snapshots

**Q: How are EBS snapshots incremental, and what's the standard DR move?**

A: First snapshot copies all used blocks; later snapshots copy only changed blocks; deleting an old snapshot never breaks a newer one. Snapshots are region-scoped but AZ-independent — copying a snapshot to another region is the standard EBS DR move.

```bash
aws ec2 create-snapshot --volume-id vol-abc --description "pre-upgrade 2026-08-08"
aws ec2 copy-snapshot --source-region us-east-1 --source-snapshot-id snap-abc \
  --destination-region us-west-2 --encrypted          # DR copy
aws ec2 create-volume --snapshot-id snap-abc --availability-zone us-east-1b --volume-type gp3
```

**Q: What automates snapshot lifecycle, and what's the commonly forgotten cost leak?**

A: **Data Lifecycle Manager (DLM)** automates scheduled creation/retention (AWS Backup for the cross-service version). Cost leak: years of nightly snapshots from decommissioned volumes piling up unnoticed.

### AMIs (Amazon Machine Images)

**Q: What's the distinction between an EBS snapshot and an AMI?**

A: A snapshot backs up a **disk**; an AMI backs up a **bootable machine** (root+additional volume snapshots plus block-device mapping and launch permissions). AMIs are region-scoped — must be copied to every region you launch in.

**Q: What's a silent cost leak with AMIs?**

A: Deregistering an AMI does **not** delete its underlying snapshots — they keep costing money until manually cleaned up.

```bash
aws ec2 create-image --instance-id i-abc --name "dotnet8-base-2026-08" --description "golden AMI"
aws ec2 copy-image --source-region us-east-1 --source-image-id ami-abc --region us-west-2 --name "dotnet8-base"
```

#### The Golden AMI Pattern — Full Explanation

**Q: Quantify why baking dependencies into a golden AMI matters for Auto Scaling.**

A: Thick user data: launch→serving traffic in 4–6 min. Thin user data (golden AMI): 60–90s. Auto Scaling is only useful if it's fast — a 5–6 minute gap means existing instances serve timeouts while new ones boot, and you're always scaling for traffic you had minutes ago.

```bash
# THICK user data — install at boot:  launch -> serving traffic = 4-6 min
yum update -y                                # 2-4 min, 200+ MB
rpm -Uvh https://packages.microsoft.com/...  # external repo dependency
yum install -y dotnet-runtime-8.0            # ~60s
yum install -y amazon-cloudwatch-agent       # ~30s
aws s3 cp s3://builds/myapp.zip /opt/ && unzip ... && systemctl start myapp

# THIN user data — golden AMI already has OS patches, .NET 8, agents, deps
#                                            launch -> serving traffic = 60-90s
echo "ASPNETCORE_ENVIRONMENT=Production" > /etc/myapp.env
aws s3 cp s3://builds/myapp-v42.zip /opt/app/ && systemctl start myapp
```

**Q: What's the "ASG launch loop" failure mode, and how does baking prevent it?**

A: If an external repo is briefly unreachable during thick user-data boot, the install fails but the instance still boots "successfully" with no runtime — app never starts → health check fails → ASG terminates and replaces it → same failure repeats forever, zero healthy capacity. Baking removes external dependencies from the boot path entirely.

**Q: What does EC2 Image Builder automate, and what's the underrated pipeline stage?**

A: Recipe (base AMI + components) → Build (temp instance, run components, snapshot) → **Test** (boot new AMI, run smoke tests, fail pipeline if broken — integrates with Inspector for CVE scanning) → Distribute (copy to regions/accounts) → Schedule (monthly/CVE-triggered rebuilds). The Test phase is underrated — it stops a broken image reaching the launch template.

```
1. RECIPE       base AMI + components: patch OS - install runtime - install agents - CIS hardening
2. BUILD        spins up a temp instance, runs the components, snapshots it
3. TEST         boots the new AMI and runs smoke tests — fails the pipeline if broken
4. DISTRIBUTE   copies the AMI to every region, shares it to every account
5. SCHEDULE     re-runs monthly, or on a critical CVE, so patches actually land
```

**Q: What's the mindset shift from patching servers to "immutable infrastructure"?**

A: Stop patching servers in place and start **replacing** them — build a new AMI, roll it out via instance refresh, and clean up what's left behind:

```
Image Builder produces AMI v43 -> update the launch template ->
ASG instance refresh (rolling replacement) -> deregister old AMIs AND delete their snapshots
```

### Instance Store

**Q: What's the one-liner distinguishing instance store from EBS?**

A: "Instance store is the fastest and least durable option — EBS is a network drive that outlives the instance, instance store is local hardware that doesn't." Data is lost on stop/hibernate/terminate/host failure (survives reboot only); correct for scratch/cache/self-replicating distributed DBs, never for anything irreplaceable.

### EBS Multi-Attach

**Q: What's the point everyone misses about EBS Multi-Attach?**

A: A normal filesystem (ext4/XFS/NTFS) will **corrupt itself** if two instances mount the same volume at once — Multi-Attach only works with a cluster-aware filesystem (GFS2, OCFS2) or an app managing its own raw block locking. It is not the general shared-storage answer (that's EFS) — it's for clustered HA apps like Oracle RAC needing concurrent raw block access. Limits: io1/io2 only, one AZ, 16 Nitro instances max.

### EBS Encryption

**Q: What does EBS encryption cover, and how do you encrypt an existing unencrypted volume?**

A: Covers data at rest, data in transit between instance and volume, all snapshots, and all volumes created from those snapshots (encryption propagates). To encrypt existing data: snapshot the unencrypted volume → **copy** the snapshot with `--encrypted` + a KMS key (encryption is introduced at the copy step) → create a new volume from the encrypted snapshot → swap it onto the instance.

### EFS (Elastic File System)

**Q: Why can't EFS serve a Windows .NET Framework app's shared storage need, and what's the alternative?**

A: EFS is POSIX/NFSv4.1, **Linux only** — no Windows support. For Windows, use **FSx for Windows File Server** (SMB, AD-integrated); for high-performance Linux/HPC, FSx for Lustre.

**Q: Why isn't EFS the default choice even though it's more flexible than EBS?**

A: It costs roughly **3× gp3 per GB** — use it only when you genuinely need concurrent multi-AZ shared access, not as a default. Also watch the **Bursting throughput mode credit trap** (same shape as T-family CPU credits) — Elastic mode is the safer default for spiky/unknown workloads.

```bash
sudo mount -t efs -o tls fs-0123456789abcdef:/ /mnt/efs      # amazon-efs-utils, TLS in transit
```

### EFS vs EBS vs Instance Store

**Q: What's the one decision sentence covering all three storage types?**

A: "One instance needs a fast disk → EBS. Many instances need the same files at once → EFS (or FSx on Windows). I need maximum speed and can rebuild the data → instance store."

### EC2 Storage Shared Responsibility Model

**Q: In the EC2 storage shared-responsibility split, what's on you vs AWS?**

A: AWS: durability/replication of EBS/EFS infrastructure, replacing failed hardware, providing encryption capability. You: taking and testing snapshot/AMI restores, choosing volume type/sizing IOPS, **enabling** encryption and managing keys, security groups on EFS mount targets (NFS 2049), and knowing instance store is ephemeral.

---

## Observability & Monitoring

### CloudWatch Deep Dive

**Q: What's the Logs hierarchy, and what does Logs Insights let you do?**

A: Log Group → Log Streams → Log Events (e.g. `/aws/lambda/ProcessOrder`, one stream per container instance). **Logs Insights** is a SQL-like query language over logs (`fields ... | filter ... | sort ...`); structured JSON logging makes fields directly queryable.

```
fields @timestamp, @message
| filter @message like /ERROR/
| sort @timestamp desc
```

```
fields @timestamp, @duration
| filter @duration > 500
```

**Q: What is Metric Math, and what's the canonical use case?**

A: Combines existing metrics into a derived formula **inside CloudWatch**, no extra instrumentation — e.g. `(m1/m2)*100` for a 5xx error-**rate** percentage instead of eyeballing two raw-count graphs. The answer to "how do you alarm on a rate rather than a raw count," since raw counts mislead at varying traffic volumes.

**Q: CloudWatch vs CloudTrail — the classic trick question answer?**

A: CloudWatch = observability (logs/metrics/alarms about **behavior/performance**). CloudTrail = governance/audit (who called which API, when). Different questions, not interchangeable.

**Q: What is Embedded Metric Format (EMF)?**

A: A structured JSON log format CloudWatch automatically extracts into metrics — useful for high-cardinality custom metrics from Lambda without extra `PutMetricData` calls and their API cost/throttling.

### CloudWatch vs X-Ray: Complementary, Not Competing

**Q: CloudWatch vs X-Ray — what question does each answer, and how do they combine in practice?**

A: CloudWatch answers "is something wrong, and what does the aggregate look like" (logs/metrics, service-level). X-Ray answers "where exactly in this specific request's path did it go wrong/slow" (traces, request-level, cross-service). In practice: a CloudWatch alarm fires on elevated p99/error rate → pull the X-Ray trace ID from the structured log → open that trace to see exactly which downstream call added latency. Wire them together via a shared trace/correlation ID in structured logs.

### CloudTrail

**Q: Management events vs Data events vs Insights events — which are logged by default, and what's the gotcha?**

A: **Management** (control-plane: `RunInstances`, `AssumeRole`) — logged by default, free, 90-day Event History. **Data** (data-plane: S3 `GetObject`/`DeleteObject`, Lambda `Invoke`) — **not logged by default**, costs extra. **Insights** (ML-detected unusual activity) — opt-in. Gotcha: "who deleted that S3 object" is unanswerable from default CloudTrail because object-level deletes are data events, off by default — must be enabled *before* the incident.

**Q: Event History vs a Trail — what's the difference, and what's an Organization trail?**

A: Event History = 90 days, in-console, free. A **Trail** delivers events to S3 for indefinite retention (optionally also CloudWatch Logs for alarms). **Organization trail** captures every account in the AWS Org into one central bucket — the standard multi-account audit design, typically in a locked-down log-archive account.

**Q: Is CloudTrail real-time? What's the alternative for immediate reaction?**

A: No — delivery lags up to ~15 minutes. For immediate reaction, use **EventBridge** rules on the event pattern rather than polling CloudTrail.

**Q: Three-way comparison — CloudWatch vs CloudTrail vs AWS Config, one question each?**

A: CloudWatch — "how is it performing?" CloudTrail — "who did what, when?" AWS Config — "what does the configuration look like, and did it drift?"

### AWS Health Dashboard

**Q: Service Health Dashboard vs Account Health Dashboard — what's the difference?**

A: **Service Health Dashboard** — public, generic status page for all AWS services/regions, tells you nothing about your resources. **Account Health Dashboard** — personalized events affecting *your* resources (degraded hardware, scheduled maintenance, EOL notices). Wire the **Health API** into EventBridge to automate responses (e.g., auto-drain an instance before a forced retirement).

### Container Insights, the CloudWatch Agent & Proactive Monitoring

**Q: Why doesn't a default CloudWatch memory alarm work on an EC2 instance?**

A: Default hypervisor-level EC2 metrics cover CPU/network/disk I/O but **not memory or filesystem free space** — those require visibility inside the guest OS via the **CloudWatch Agent** (installed via SSM, ideally baked into the AMI). Answering "CloudWatch memory metric" without the agent is a classic wrong answer.

**Q: What's the difference between Container Insights, Lambda Insights, Synthetics, and RUM?**

A: **Container Insights** — cluster/service/task/pod metrics for ECS/EKS. **Lambda Insights** — per-invocation memory/CPU/init duration. **Synthetics (canaries)** — scripted external checks catching an outage before a user reports it. **RUM** — real-user monitoring from actual browsers (load times, JS errors, Core Web Vitals) — pairs with Synthetics (RUM = what users experience, Synthetics = what a known-good request experiences).

**Q: What two alarm-design details matter beyond just picking a threshold?**

A: Set `--treat-missing-data` deliberately (a metric that stops publishing because the service is *down* leaves an alarm stuck `INSUFFICIENT_DATA` forever otherwise); alarm on **rates via Metric Math** rather than raw counts, since raw counts mislead when traffic volume swings.

```bash
# Alarm on p99 latency using an extended statistic
aws cloudwatch put-metric-alarm --alarm-name api-p99-latency \
  --namespace AWS/ApplicationELB --metric-name TargetResponseTime \
  --extended-statistic p99 --period 60 --evaluation-periods 3 --threshold 1.5 \
  --comparison-operator GreaterThanThreshold --treat-missing-data notBreaching \
  --alarm-actions arn:aws:sns:us-east-1:123456789012:oncall

# Logs Insights: slowest Lambda invocations
aws logs start-query --log-group-name /aws/lambda/my-fn \
  --start-time $(date -d '1 hour ago' +%s) --end-time $(date +%s) \
  --query-string 'fields @timestamp, @duration, @requestId | filter @type="REPORT" | sort @duration desc | limit 20'

aws logs tail /aws/lambda/my-fn --follow --format short      # live tail while debugging
```

---

# PART II — Tier 2: Design-Level Confidence

## Containers: Docker, ECS, ECR & Fargate

### AWS Fargate

**Q: What is Fargate, and how is task sizing constrained?**

A: A serverless compute engine for containers — a launch type for ECS/EKS, not an orchestrator itself; you supply an image + CPU/memory, AWS handles servers/scaling/patching, billed per vCPU-second + GB-second with zero idle cost. Sizing is constrained to **valid vCPU/memory pairs** (e.g. 1 vCPU → 2–8GB) — an arbitrary combination is rejected.

**Q: What's Fargate's ephemeral storage limit, and how do you get persistent/shared storage?**

A: 20GB default, up to 200GB, but ephemeral — gone when the task stops. For persistence or sharing between tasks, mount **EFS**.

**Q: What's Fargate Spot, and how do you blend it with standard Fargate?**

A: Same interruption model as EC2 Spot (~70% cheaper, 2-minute SIGTERM warning). A **capacity provider strategy** mixes them per service, e.g. "2 tasks always on FARGATE, everything above on FARGATE_SPOT" — baseline reliability with cheap burst, provided the app handles SIGTERM (needs exec-form ENTRYPOINT).

**Q: Is "Fargate is always cheaper than EC2" true?**

A: No — false. Fargate wins for bursty/low-utilization workloads; EC2 wins for steady, high-utilization workloads (no per-task premium on capacity you'd use anyway). Rule of thumb: a 24×7 steady 1vCPU/2GB service is cheaper on EC2; a job running 2h/day at the same size is cheaper on Fargate.

### EC2 vs Fargate Cost & Trap Scenarios

**Q: Rapid-fire — CPU is low but the app is slow; a Fargate task can't reach the internet; works in dev fails in prod. What's the likely cause of each?**

A: Low CPU but slow app → bottleneck is likely disk I/O, network latency, or a single-threaded hot path, not CPU. Fargate can't reach internet → private subnet with no NAT Gateway/VPC endpoint. Dev works, prod fails → almost always IAM role, secrets, or networking (subnet/SG) differences between environments.

**Q: What's the final mental model for choosing EC2 vs Fargate?**

A: Steady load → EC2. Bursty load → Fargate. Need control → EC2. Need simplicity → Fargate. Idle-cost sensitive → Fargate. High constant utilization → EC2.

### Docker & Container Fundamentals

**Q: VM vs Container — what's the core isolation difference?**

A: A VM has its own full OS/kernel via a hypervisor (GBs, minutes to boot, low density, strong isolation). A container **shares the host kernel** (MBs, seconds to start, high density, weaker kernel-level isolation but far cheaper/faster).

**Q: Write a multi-stage .NET Dockerfile and explain why layer order matters.**

A:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "MyApi.dll"]
```

Multi-stage keeps the SDK out of the shipped image; copying `.csproj` before source means `dotnet restore` is cached and only reruns when dependencies change — layers that change least go first.

```bash
docker build -t myapi:1.0 .
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development myapi:1.0
docker ps / docker logs <id> / docker exec -it <id> sh
```

#### Docker Images — The Detail Worth Knowing

**Q: Get the registry/repository/image/tag/digest vocabulary straight.**

A:

```
REGISTRY            ECR, Docker Hub                       — the server that stores repositories
 └─ REPOSITORY      123456789012.dkr.ecr…/myapi           — all versions of one image
     └─ IMAGE       myapi:1.4.2   (tag)                   — a mutable, human-friendly pointer
     └─ IMAGE       myapi@sha256:9f2a…  (digest)          — the immutable content hash
         └─ CONTAINER                                     — a running instance of that image
```

**Q: Tag vs digest — why does it matter operationally?**

A: A **tag** is a movable label (`myapi:1.4.2` can be repointed to different bits tomorrow); a **digest** is an immutable content hash. Deploy by digest or an immutable tag for reproducibility — `:latest` is an anti-pattern because "roll back to the previous latest" doesn't exist.

**Q: What does `.dockerignore` actually solve, and what's a typical one for a .NET app?**

A: Without it, the whole build context (including `bin/`, `obj/`, `node_modules/`, `.git`) uploads to the daemon on every build (slow), and `COPY . .` risks baking `appsettings.Development.json`/`.env`/`.aws/credentials` into the image (a real secret leak):

```
bin/
obj/
.git/
.vs/
**/node_modules
**/appsettings.*.json
.env
Dockerfile
```

**Q: Why doesn't deleting a file in a later Docker layer shrink the image or remove it from history?**

A: Each instruction creates a read-only layer; a later `RUN rm file` only hides the file in the union filesystem — it's still extractable from the earlier layer. `COPY secrets.json . && RUN rm secrets.json` leaves the secret in the image permanently — never put credentials in a build.

**Q: What's the production-default .NET base image, and why is "no shell" a feature?**

A: `aspnet:8.0-jammy-chiseled` (~110MB, Ubuntu chiselled, no shell, no package manager, non-root by default) — smallest attack surface; an attacker with RCE has nothing to pivot with. Cost: `docker exec sh` doesn't work, so you debug via logs/metrics instead.

**Q: ENTRYPOINT vs CMD, and why must you use exec form?**

A: `ENTRYPOINT` = the always-run executable (hard to override); `CMD` = default arguments (trivially overridden). Exec form (`["dotnet","MyApi.dll"]`) lets the process receive `SIGTERM` directly for graceful shutdown; shell form wraps it in `/bin/sh -c`, so the app never sees SIGTERM and gets killed instead — turning every deploy into dropped in-flight requests.

```dockerfile
ENTRYPOINT ["dotnet", "MyApi.dll"]     # always runs this
CMD ["--environment=Production"]       # default arg; `docker run img --environment=Staging` overrides it
```

**Q: Why does an x86-built image fail on a Graviton instance, and how do you fix it?**

A: It fails with `exec format error` — architecture mismatch. Fix: `docker buildx build --platform linux/amd64,linux/arm64 ... --push` produces a manifest list serving both architectures from one tag.

```bash
docker buildx build --platform linux/amd64,linux/arm64 -t <ecr>/myapi:1.4.2 --push .
```

### ECS (Elastic Container Service)

**Q: What's the ECS object model, outermost in?**

A: Cluster → Service (desired count) → Task (instance of a Task Definition) → Container(s).

```
Cluster  →  Service  →  Task  →  Container(s)
              ↑          ↑
        desired count   instance of a Task Definition
```

**Q: What's the most common real-world ECS IAM mistake?**

A: Adding the app's DynamoDB permission to the **task execution role** (used by the ECS agent for ECR pull/logs/secrets) instead of the **task role** (used by your application code) — produces an inexplicable `AccessDenied`.

**Q: `awsvpc` vs `bridge` vs `host` networking — which is mandatory for Fargate?**

A: `awsvpc` gives every task its own ENI/private IP/security group — **mandatory for Fargate**, and the generally preferred mode since it allows per-service SG rules. `bridge`/`host` are older EC2-launch-type modes.

**Q: What does the deployment circuit breaker do, and why turn it on?**

A: Automatically rolls back a deployment whose tasks fail to stabilize — the difference between a failed deploy and an outage.

**Q: `binpack` vs `spread` task placement — what's the cost/resilience trade-off?**

A: `binpack` packs tasks onto the fewest instances (cheapest); `spread` distributes across AZs/instances (most resilient, survives losing a host or AZ). Most teams spread across AZs and binpack within them.

#### ECS on the EC2 Launch Type

**Q: What's the third ECS IAM role that only exists on the EC2 launch type, and what does forgetting it cause?**

A: The **ECS instance role** (`ecsInstanceRole`) — attached to the EC2 instance profile, used by the ECS agent to register with the cluster and pull from ECR. Forgetting `ECS_CLUSTER=<name>` in user data means the instance registers to the `default` cluster instead — your real cluster shows 0 container instances while everything looks healthy in EC2.

```bash
#!/bin/bash
echo "ECS_CLUSTER=prod-cluster" >> /etc/ecs/ecs.config
```

**Q: What is Capacity Provider managed scaling, and what's the target-capacity trade-off?**

A: A capacity provider wraps an ASG; ECS publishes `CapacityProviderReservation` and target-tracks it, growing the ASG when tasks can't be placed. Target capacity `100` = "scale so tasks just fit" (cheapest, slowest to place new tasks); below 100 keeps warm headroom for immediate placement. **Managed termination protection must be on**, or the ASG can terminate an instance with running tasks.

#### How EC2, ASG, Capacity Provider, Cluster, Service & Tasks Fit Together

**Q: What are the two independent ECS scaling loops, and what happens if you only wire up one?**

A: **Loop 1 (Service Auto Scaling)** scales tasks based on application demand. **Loop 2 (Cluster Auto Scaling)** scales EC2 instances based on task-placement pressure via the capacity provider. Missing Loop 2 means tasks sit `PENDING` forever with nowhere to run; missing Loop 1 means traffic rises but task count stays flat. On Fargate, Loop 2 doesn't exist — AWS supplies capacity directly.

```
       +-------------------------------------------------------------------+
       |                          ECS CLUSTER                              |
       |     the boundary: holds services AND registered instances         |
       +--------------------+---------------------------+------------------+
                            |                           |
     -- LOOP 1: scale TASKS -+                           +- LOOP 2: scale INSTANCES --
                            |                           |
                   +--------v---------+        +---------v-------------+
                   |     SERVICE      | picks  |  CAPACITY PROVIDER    |
                   |  desiredCount=6  |------->|  wraps exactly 1 ASG  |
                   |                  |  via a |  + managed scaling    |
                   | (Service Auto    |strategy|                       |
                   |  Scaling)        |        |  <<< THE BRIDGE >>>   |
                   +--------+---------+        +---------+-------------+
                            |                            |
                            | maintains                  | target-tracks
                            |                            | CapacityProviderReservation
                   +--------v---------+        +---------v-------------+
                   |      TASKS       |        |         ASG           |
                   |  6 containers    |        |  min / desired / max  |
                   +--------+---------+        +---------+-------------+
                            |                            |
                            | scheduled onto             | launches / terminates
                            |                            |
                            +------------+---------------+
                                         |
                            +------------v-------------------------+
                            |       CONTAINER INSTANCES            |
                            |  EC2 + ECS agent, registered to the  |
                            |  cluster via ECS_CLUSTER=<name>      |
                            +--------------------------------------+
```

**Q: How does the `CapacityProviderReservation` metric drive ASG scaling?**

A: `(M/N)×100` where N = instances currently in the ASG, M = instances ECS calculates it needs. M>N → above 100 → scale out; M<N → below 100 → scale in.

```
CapacityProviderReservation = (M / N) x 100

  N = instances currently running in the ASG
  M = instances ECS calculates it NEEDS for running + pending tasks
```

**Q: What causes "unable to place a task because no container instance met all of its requirements"?**

A: In order to check: not enough remaining CPU/memory on any instance; a host port conflict; no instance satisfying a placement constraint; the per-instance ENI limit reached in `awsvpc` mode.

**Q: What's dynamic port mapping, and what security group rule does it require?**

A: In `bridge` mode, `hostPort: 0` lets Docker assign a random ephemeral host port, and ECS registers that specific port with the ALB target group — letting several copies of the same container run on one instance. Requires the instance SG to allow the **ephemeral range 32768–65535** from the ALB's SG, not just port 80.

**Q: Container instance draining vs ALB connection draining — how do they differ, and why do you need both?**

A: **Container instance draining** (`DRAINING` state) stops new task placement and relocates running tasks before instance termination — wired to an ASG lifecycle hook. **ALB deregistration delay** lets in-flight HTTP requests finish. A graceful deploy needs both: draining moves tasks, deregistration delay lets their requests complete.

**Q: ECS vs EKS in one line?**

A: ECS is simpler, AWS-proprietary, free control plane — default choice. EKS is managed Kubernetes — pick it for multi-cloud portability, existing K8s investment, or the CNCF ecosystem, accepting the added complexity and control-plane cost.

### ECR (Elastic Container Registry)

**Q: Basic vs enhanced ECR image scanning — which is right for production?**

A: Basic scans once on push against a CVE database. **Enhanced scanning** uses Amazon Inspector for continuous rescanning of OS and language-package dependencies as new CVEs publish — the right answer for production.

```bash
aws ecr get-login-password --region us-east-1 | docker login --username AWS \
  --password-stdin 123456789012.dkr.ecr.us-east-1.amazonaws.com
docker tag myapi:1.0 123456789012.dkr.ecr.us-east-1.amazonaws.com/myapi:1.0
docker push          123456789012.dkr.ecr.us-east-1.amazonaws.com/myapi:1.0
```

**Q: Why turn on ECR tag immutability, and what's the pull-through cache for?**

A: Tag immutability prevents `myapi:1.0` from silently becoming different bits, preserving reproducible rollback. **Pull-through cache** caches upstream public images (Docker Hub, MCR) in your registry, avoiding Docker Hub rate limits in CI.

### Hands-On: ECS with Fargate

**Q: What's the debugging order when ECS tasks won't start?**

A: `describe-services` events first, then the stopped task's `stoppedReason`. Usual causes: task execution role can't pull from ECR/write logs; image architecture mismatch (ARM vs x86); health check fails before app finishes starting; no route to ECR from a private subnet (needs NAT or VPC endpoints); container exits from a missing env var.

```bash
# 1. Registry + image
aws ecr create-repository --repository-name myapi --image-tag-mutability IMMUTABLE \
  --image-scanning-configuration scanOnPush=true
# (build, tag, push as above)

# 2. Cluster
aws ecs create-cluster --cluster-name prod-cluster

# 3. Task definition (family + the two roles + awsvpc + awslogs)
aws ecs register-task-definition --cli-input-json file://taskdef.json

# 4. Service behind an ALB target group, across 3 AZs
aws ecs create-service --cluster prod-cluster --service-name myapi-svc \
  --task-definition myapi:1 --desired-count 2 --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-a,subnet-b,subnet-c],securityGroups=[sg-app],assignPublicIp=DISABLED}" \
  --load-balancers targetGroupArn=arn:...,containerName=myapi,containerPort=8080 \
  --deployment-configuration "deploymentCircuitBreaker={enable=true,rollback=true}"

# 5. Verify / debug
aws ecs describe-services --cluster prod-cluster --services myapi-svc \
  --query 'services[0].{running:runningCount,desired:desiredCount,events:events[0:3]}'
aws ecs execute-command --cluster prod-cluster --task <id> --container myapi \
  --interactive --command "/bin/sh"      # ECS Exec — shell into a running task
```

### ECS vs EKS vs Fargate vs Lambda for .NET Workloads

**Q: A .NET microservices platform is moving to AWS — how do you choose the compute layer, and what if the interviewer pushes for EKS?**

A: Default new .NET microservices to **ECS on Fargate** (best simplicity/control balance). Use **Lambda** for event-driven glue and spiky/idle-heavy work. Reach for **EKS** only with an existing organizational Kubernetes investment. Use **EC2** (Windows containers/full Windows Server) only for .NET Framework workloads that can't port to .NET Core/8+, or GPU/specialized hardware needs. If pushed on "why not EKS for a green-field team with no K8s experience" — correct answer is still no, unless there's a concrete multi-cloud/portability requirement justifying the learning curve.

**Q: Does Lambda support .NET Framework (not Core)?**

A: No — Lambda requires .NET Core/5+. ECS EC2 launch type (Windows containers) or EC2 directly support .NET Framework/IIS lift-and-shift; Fargate does not support Windows-launch-type .NET Framework the same way EC2 does.

### Fargate/ECS/EKS Trade-offs — Reasoning Without Hands-On Time

**Q: How would you honestly frame a lack of hands-on Fargate/ECS/EKS production experience in an interview?**

A: State it directly: hands-on AWS provisioning experience is Lambda/DynamoDB/EC2/S3 via Terraform/CDKTF — reasoning through container orchestrator trade-offs is conceptual, not claimed operational experience. Then walk the decision flow (existing K8s/multi-cloud need? → EKS; OS/GPU/legacy need? → EC2; spiky/event-driven? → Lambda; else → ECS on Fargate) rather than bluffing depth.

```
   Existing Kubernetes investment or multi-cloud requirement?
                              |
              +---------------+---------------+
             yes                              no
              |                               |
              v                               v
            EKS            Need OS-level access, GPU,
                           or a legacy/stateful app?
                                          |
                          +---------------+---------------+
                         yes                              no
                          |                               |
                          v                               v
                        EC2          Spiky / event-driven, short-lived,
                                     idle-heavy?
                                                    |
                                    +---------------+---------------+
                                   yes                             no
                                    |                    (steady containerised
                                    v                       service)
                                 LAMBDA                          |
                                                                 v
                                                         ECS on FARGATE
```

**Q: Why isn't Kubernetes "just use it, it's the industry standard" the right default for a .NET shop with no K8s investment?**

A: Kubernetes' biggest selling point (portability, Helm/operators/service-mesh ecosystem) is also its biggest cost — a real control-plane learning curve (CRDs, RBAC, CNI, admission controllers) that ECS/Fargate users never pay. Without a concrete multi-cloud requirement or existing K8s-fluent platform team, that operational tax usually isn't justified.

### Deploying .NET to AWS: Elastic Beanstalk vs ECS vs Lambda Custom Runtime

**Q: What is Elastic Beanstalk really, under the hood — and what's the senior-vs-mid-level distinction to make?**

A: Beanstalk is **not** a separate compute primitive — it still provisions EC2+ASG+ELB (or ECS for the Docker platform) underneath. The value-add is deployment/orchestration tooling (`eb deploy`, rolling/immutable/blue-green environment configs), not a new runtime. Naming that (orchestration layer, not new infrastructure) is the senior-level distinction.

**Q: Compare deployment strategies across Beanstalk, ECS, and Lambda.**

A: **Beanstalk** — rolling, rolling-with-additional-batch, immutable, or blue/green (CNAME swap). **ECS** — rolling update via service deployment config, or blue/green via CodeDeploy + two target groups. **Lambda** — versions + aliases with linear/canary shifting via CodeDeploy.

---

## Relational Databases, Caching & Analytics

### RDS Multi-AZ vs Read Replicas vs Aurora

**Q: What's the core interview nuance to land about Multi-AZ vs Read Replica?**

A: Multi-AZ is for **availability**, not scalability — the classic standby doesn't serve traffic. Read replicas are for **scaling reads**, not HA — promotion is manual and breaks replication, so it's not a DR plan. Conflating the two is a very common junior-level confusion.

**Q: Why does Lambda-to-RDS need RDS Proxy specifically?**

A: Each concurrent Lambda execution environment would otherwise open its own DB connection, exhausting the database's `max_connections` limit under burst concurrency — RDS Proxy pools/multiplexes them.

### Multi-AZ vs Read Replica — The #1 Confused Pair

**Q: Recite the drill answer distinguishing Multi-AZ from Read Replica verbatim.**

A: "Multi-AZ is about surviving failure — a synchronous standby AWS fails over to automatically, but in the classic form it doesn't serve read traffic, so it does nothing for scaling. Read Replicas are about scaling reads — asynchronous copies you route reporting/read traffic to, but promoting one to primary is manual and replication-breaking, so it's not a substitute for real HA. Using a read replica as your DR plan, or expecting a Multi-AZ standby to absorb read load, are the same category of mistake."

```
                    What problem are you solving?
                                 |
      +--------------------------+--------------------------+
      |                                                     |
 "I need the DB to survive                    "My read traffic is
  an AZ failure"                               overwhelming one instance"
      |                                                     |
      v                                                     v
+--------------------------+              +--------------------------+
|    MULTI-AZ STANDBY      |              |      READ REPLICA        |
+--------------------------+              +--------------------------+
| Synchronous replication  |              | Asynchronous replication |
|                          |              |                          |
| Standby is NOT readable  |              | Fully readable -- that    |
| (classic Multi-AZ)       |              | IS its entire purpose    |
|                          |              |                          |
| Automatic failover,      |              | Promotion is MANUAL and  |
| typically 60-120s        |              | breaks replication       |
|                          |              |                          |
| x Does NOT help with     |              | x Does NOT give you      |
|   read scaling           |              |   automatic HA failover  |
+--------------------------+              +--------------------------+

          Two mechanisms, two different problems.
          Using a read replica as your DR plan, or expecting a
          Multi-AZ standby to absorb read load, is the same mistake.
```

**Q: What changed with the newer Multi-AZ DB Cluster feature?**

A: Unlike classic Multi-AZ (standby not readable), Multi-AZ DB Cluster adds **readable reader endpoints** on the standby — a common "gotcha, that changed" interview follow-up.

**Q: What's Aurora's key architectural difference from standard RDS, and why does that matter for replica lag?**

A: Aurora separates compute from a shared, distributed, auto-scaling storage volume replicated across AZs at the storage layer — not by shipping logs between full instances. This is why Aurora replica lag is typically much lower (often sub-10-seconds, sometimes near-instant) than standard RDS read-replica lag.

**Q: When would you pick standard RDS over Aurora?**

A: Aurora is MySQL/PostgreSQL-compatible only — it does **not support SQL Server**. For SQL Server-based .NET shops, or when Aurora's cost premium isn't justified by the workload's availability/scale needs, standard RDS remains the right call.

### Databases & Analytics Overview: Choosing the Right Store

**Q: What's the framing to lead with when asked "which database would you pick"?**

A: OLTP vs OLAP. OLTP = many small concurrent indexed reads/writes (order-entry API → RDS/DynamoDB). OLAP = few large scans/aggregations over history (revenue dashboard → Redshift/Athena). Running analytical queries against your OLTP primary is the classic mistake — fix with a read replica for light reporting or a proper warehouse/lake for real analytics.

**Q: One-line purpose for Redshift, OpenSearch, Neptune, Timestream, and QLDB?**

A: **Redshift** — OLAP warehouse for TB–PB BI queries. **OpenSearch** — full-text search/log analytics. **Neptune** — graph, when relationships are the primary query. **Timestream** — time-series IoT/metrics with rollups/retention tiers. **QLDB** — cryptographically verifiable immutable ledger.

### Relational Databases & RDS — The Operational Surface

**Q: What's the single biggest limitation of standard RDS, and when do you need to escape it?**

A: **No OS/shell access at all** — you cannot install an agent or custom extension. Escape via **RDS Custom** (Oracle/SQL Server only) or self-managing the engine on EC2.

**Q: Automated backups vs manual snapshots — what's the key difference?**

A: Automated backups (daily full + continuous transaction logs, enabling PITR to any second within a 1–35 day retention window) are **deleted when you delete the instance** unless you take a final snapshot. Manual snapshots are kept until *you* delete them and are shareable across accounts/regions.

**Q: What's IAM database authentication, and why is it a strong answer to "how do you avoid database passwords"?**

A: Lets an application authenticate with a short-lived IAM token instead of a stored password — pairs naturally with an EC2/Lambda role. The other strong answer is Secrets Manager with rotation.

#### RDS Custom — for Oracle & SQL Server

**Q: What problem does RDS Custom solve, and what's its distinctive risk?**

A: Standard RDS gives no OS access/DB superuser, blocking things like SSIS/SSRS/CLR assemblies (SQL Server) or Data Guard/APEX (Oracle). RDS Custom gives OS + superuser access while AWS still handles backups/PITR/Multi-AZ **conditionally** — but breaking the supported configuration (altering the agent, storage layout, IAM permissions) moves the instance to `unsupported-configuration`: automation stops and fixing it is your job. "Managed until you break it."

**Q: What are the RDS Custom setup prerequisites?**

A: IAM instance profile, an S3 bucket for artifacts, and a **customer-managed KMS key** (AWS-managed key won't do); SQL Server/Oracle use a Custom Engine Version (CEV) to pin the exact build.

#### RDS Security — Consolidated

**Q: Give the one-liner answer to "how do you secure RDS?"**

A: "Private subnets and SG-to-SG rules so it's unreachable from the internet; KMS at creation and forced TLS so data is encrypted both ways; IAM database auth or Secrets Manager so there's no password in config; least-privilege database users inside; and Database Activity Streams plus engine logs in CloudWatch so privileged access is auditable. CloudTrail tells me who changed the instance — it does not tell me who ran the query."

**Q: What's the strongest authentication option for RDS, and its caveat?**

A: **IAM database authentication** — a 15-minute token from the app's role, no stored password. Caveat: it has a connection-rate limit, so pair with **RDS Proxy** for high-churn/Lambda workloads.

**Q: How do you audit *privileged* database access (a DBA with full rights), as opposed to just API calls?**

A: **Database Activity Streams** (Aurora, plus RDS for Oracle/SQL Server) — a near-real-time, tamper-resistant stream to Kinesis that even a full-privilege DBA cannot erase. CloudTrail only logs the API (`CreateDBInstance`), not the SQL that ran.

### Athena

**Q: What is Athena, and what three levers cut its per-TB-scanned cost dramatically?**

A: Serverless SQL directly over S3 data (Presto/Trino under the hood), billed per TB scanned (~$5/TB). Cut cost via (1) **columnar formats** (Parquet/ORC vs CSV/JSON — reads only selected columns), (2) **partitioning** (`year=2026/month=08/...` so a WHERE clause skips irrelevant data; partition projection avoids the metadata lookup), (3) **compression** + avoiding `SELECT *`.

**Q: Athena vs Redshift — when do you pick each?**

A: Athena — serverless, pay-per-query, ad-hoc/infrequent analysis over a data lake, no ETL. Redshift — provisioned/serverless warehouse, frequent/complex/high-concurrency BI where sustained volume makes a cluster cheaper and faster. "Occasional queries over S3 logs → Athena; a dashboard hundreds of analysts hit all day → Redshift."

### RDS Proxy

**Q: Why does Lambda + RDS break without a proxy, concretely?**

A: Each concurrent Lambda execution environment is its own process with its own private connection pool — a pool of 100 inside one Lambda is meaningless since it only ever needs 1 connection, but there are now N separate pools where N = concurrency. 500 concurrent executions → 500 connections against a `max_connections` ceiling of ~420 on a `db.t3.medium` → `FATAL: too many connections`, plus CPU wasted on connect/TLS-handshake/disconnect churn.

```
CONCURRENCY 500  ->  500 execution environments  ->  500 separate connections
db.t3.medium PostgreSQL max_connections ≈ 420
                        ↓
   FATAL: too many connections / remaining connection slots are reserved
```

**Q: What does RDS Proxy actually give you?**

A: Pools/multiplexes connections so hundreds of clients share few real DB connections; cuts failover time ~66% (re-points held-open connections instead of clients reconnecting); enforces IAM auth pulling credentials from Secrets Manager (no password in config); runs inside your VPC, never public.

```
   BEFORE                                AFTER
   Lambda x500                           Lambda x500
      |                                     |
      | 500 connections                     | 500 connections
      v                                     v
   +--------+                          +-------------+
   |  RDS   |  <- refuses them         | RDS PROXY   |  warm pool + multiplexing
   +--------+                          +------+------+
                                              | ~20-50 real connections
                                              v
                                          +--------+
                                          |  RDS   |  comfortable
                                          +--------+
```

**Q: What is connection pinning, and why does it undermine the proxy?**

A: If a session does something session-scoped (explicit transactions held open, `SET` session variables, temp tables, advisory locks, `USE database` on MySQL), the proxy must pin that client to one real DB connection for the rest of the session, losing the multiplexing benefit. Watch `DatabaseConnectionsCurrentlySessionPinned` — if high, fix the application (short transactions, no session state), not the proxy config.

**Q: What are the RDS Proxy setup requirements, and its alternatives when it's not the right tool?**

A: Lambda must be **VPC-attached** in the same VPC as the proxy (loses default internet access — needs NAT/VPC endpoints); proxy needs a Secrets Manager secret + IAM role. Alternatives: **DynamoDB** (no connection concept at all, the strongest answer when the data model allows it); **Aurora Data API** (HTTPS/IAM endpoint, zero connection management, but higher per-query latency); a single long-lived ECS/EC2 service's own in-process pool is already sufficient.

IAM database authentication removes the password from the app entirely — grant the Lambda's execution role `rds-db:connect` and generate a token instead:

```csharp
var token = RDSAuthTokenGenerator.GenerateAuthToken(
    "my-proxy.proxy-abc123.us-east-1.rds.amazonaws.com", 5432, "app_user");
// use the token as the password; combine with SSL Mode=Require
```

**Q: What's being tested when an interviewer asks "why is Lambda + RDS often an anti-pattern"?**

A: Whether you understand Lambda's concurrency model is **processes, not threads** — connection count scales with concurrency and there's no shared pool — rather than just naming "RDS Proxy" as a magic fix.

### Aurora Advanced Features

**Q: How does Aurora's storage replication work, and what failure can it tolerate with no availability impact?**

A: **6 copies across 3 AZs** — tolerates losing 2 copies for writes and 3 for reads with no availability impact, self-healing bad blocks.

**Q: Writer endpoint vs reader endpoint vs custom endpoint — what's each for?**

A: **Writer** — always points at the current primary, failover transparent. **Reader** — load-balances across replicas. **Custom** — targets a chosen subset, e.g. routing heavy reporting queries to larger replicas so they don't affect the API's replicas.

**Q: What do Aurora Serverless v2, Global Database, and Backtrack each solve?**

A: **Serverless v2** — scales capacity in fine-grained ACUs in under a second, for spiky/dev-test workloads. **Global Database** — 1 primary + up to 5 read-only secondary regions, <1s typical replication, <1min cross-region failover — the engine behind Warm Standby/Active-Active DR. **Backtrack** — rewinds the cluster in place to a point in time without a restore, for recovering from a bad migration in minutes.

### ElastiCache & Caching Patterns

**Q: Redis vs Memcached — when do you default to Redis, and why would you still pick Memcached?**

A: Default to Redis (rich data structures, persistence, Multi-AZ automatic failover, transactions/pub-sub). Pick Memcached only for a genuinely simple cache that benefits from its multi-threaded model and needs no persistence/failover.

**Q: Name the four caching patterns and their trade-offs.**

A: **Lazy loading/cache-aside** — read DB on miss, populate cache; risks a cache-stampede on a hot key expiry. **Write-through** — write cache+DB together; reads always warm, writes slower. **Write-behind** — write cache, flush to DB async; fastest writes, risks data loss on node failure. **TTL/eviction** — every item needs an expiry; no TTL strategy is a stale-data bug waiting to happen.

**Q: DAX vs ElastiCache for a DynamoDB workload — which do you pick?**

A: **DAX** — in-VPC write-through cache API-compatible with DynamoDB, zero application caching code, but only helps **eventually-consistent** reads and doesn't accelerate writes. **ElastiCache** — for caching something *other* than raw table reads (computed aggregates, session state, rate limits, leaderboards) or full control over keys/eviction. A read-heavy table hitting a hot partition/RCU ceiling → DAX.

#### ElastiCache — Pitfalls

**Q: What's a cache stampede, and what are the three mitigations?**

A: A popular key expires and hundreds of concurrent requests miss simultaneously, hammering the database — the cache causes the outage it was meant to prevent. Mitigate with jittered TTLs, a short-lived lock so one caller repopulates while others wait, or serve-stale-while-revalidate.

**Q: What's a "hot key" problem, and why doesn't adding shards fix it?**

A: A key lives on exactly one shard — one disproportionately popular key saturates that node regardless of cluster size. Fix with a small client-side/in-process cache for that key, or split it into `key:{0..9}` and merge. Same shape as a DynamoDB hot partition.

**Q: Why is Redis being single-threaded a real production-incident risk?**

A: One slow command (`KEYS *` on a large keyspace, a big `DEL`, an expensive Lua script) blocks **every** other client. Use `SCAN` instead of `KEYS` and `UNLINK` instead of `DEL` for large values.

**Q: Is ElastiCache durable? What's the alternative if you need durability?**

A: No — it's a cache; node failure loses unreplicated/unpersisted data. For durability as a primary data store, use **MemoryDB for Redis** (multi-AZ transaction log).

---

## Networking

### VPC, Subnets, NAT — Complete Model

**Q: What actually makes a subnet "public" vs "private"?**

A: Nothing about its name or a flag — only its **route table**. Public: has a `0.0.0.0/0 → Internet Gateway` route. Private: no direct IGW route; outbound (if any) goes through a NAT Gateway/Instance instead.

**Q: Where must a NAT Gateway live, and what's the cost gotcha?**

A: In a **public** subnet — one in a private subnet is non-functional. NAT bills per-hour **plus per-GB processed**; for AWS-service-only traffic (S3, DynamoDB, Secrets Manager), use **VPC Endpoints** instead — cheaper, lower latency, and off the public internet.

**Q: List four common false claims about VPC networking.**

A: "Public subnet = automatic internet access" (false — needs a public IP + IGW route + permissive SG); "NAT allows inbound traffic" (false — outbound-only); "subnet name/tag decides security behavior" (false — only route tables/SGs/NACLs matter); "one route table per VPC" (false — usually one per subnet-group).

### VPC Reference Architecture

**Q: Describe the canonical 3-tier VPC layout.**

A: Public subnet per AZ (ALB + NAT Gateway + optional bastion) → private app subnet per AZ (ECS/EC2 compute) → private isolated data subnet per AZ (RDS, no route to NAT/IGW at all). Both AZs' app subnets talk to the RDS primary; the standby carries no traffic until failover.

```
                            +------------+
                            |  Internet  |
                            +-----+------+
                                  |
                        +---------v----------+
                        |  Internet Gateway  |
                        +----+----------+----+
                             |          |
+----------------------------|----------|-----------------------------+
| VPC  10.0.0.0/16           |          |                             |
|                            v          v                             |
|      AVAILABILITY ZONE A               AVAILABILITY ZONE B          |
|  +--------------------------+      +--------------------------+     |
|  | PUBLIC      10.0.0.0/24  |      | PUBLIC      10.0.1.0/24  |     |
|  | ALB . NAT GW . Bastion   |      | ALB . NAT GW             |     |
|  +------------+-------------+      +------------+-------------+     |
|               | outbound via NAT                | outbound via NAT  |
|  +------------v-------------+      +------------v-------------+     |
|  | PRIVATE APP 10.0.10.0/24 |      | PRIVATE APP 10.0.11.0/24 |     |
|  | ECS tasks / EC2 app      |      | ECS tasks / EC2 app      |     |
|  +------------+-------------+      +------------+-------------+     |
|               |                                 |                   |
|  +------------v-------------+      +------------v-------------+     |
|  | PRIVATE DB  10.0.20.0/24 |      | PRIVATE DB  10.0.21.0/24 |     |
|  | RDS PRIMARY              |<====>| RDS STANDBY              |     |
|  +--------------------------+ sync +--------------------------+     |
+---------------------------------------------------------------------+

  Both AZs' app subnets talk to the RDS PRIMARY; the standby carries no
  traffic and only takes over on failover. No subnet is "public" because
  of its name -- only because its route table points 0.0.0.0/0 at the IGW.
```

### VPC/Subnet/NAT/SG Rapid-Fire Drill Sheet

**Q: Rapid-fire — NAT Gateway vs Internet Gateway, and can NAT allow inbound traffic?**

A: IGW = two-way internet access for public-subnet resources. NAT Gateway = outbound-only for private-subnet resources. NAT can never allow inbound — always outbound-only by design.

**Q: Rapid-fire — SG vs NACL on statefulness, attachment point, and allow/deny capability?**

A: SG: stateful, attaches to ENI/instance, allow-only. NACL: stateless, attaches to subnet, allow AND deny. SGs are the primary defense layer in practice; NACLs are used sparingly for coarse subnet-level blocking.

### VPC Flow Logs

**Q: What does a `REJECT` record prove vs no record at all?**

A: A `REJECT` record proves traffic **arrived** and was blocked by an SG or NACL. **No record at all** means the packets never got there (wrong route table/subnet, no IGW/NAT) — that distinction narrows almost any network fault fast.

**Q: How do you tell from Flow Logs whether an SG or a NACL is the culprit?**

A: SGs are stateful, so a SG block shows only the inbound `REJECT`. NACLs are stateless, so a NACL misconfiguration typically shows `REJECT` on the **return path too** — rejects in both directions point at the NACL.

**Q: What traffic is never captured by VPC Flow Logs?**

A: Traffic to the Amazon DNS server, DHCP, the instance metadata endpoint (`169.254.169.254`), Windows license activation, and traffic to the reserved VPC router address.

### VPC Peering

**Q: What are the three hard constraints of VPC Peering?**

A: (1) CIDR blocks must not overlap, no exceptions. (2) **Peering is not transitive** — A peers B, B peers C, A cannot reach C; you need a direct A↔C peering (the single most-asked peering question). (3) No edge-to-edge routing — you can't use a peer's IGW/NAT/VPN/Direct Connect.

**Q: Why doesn't peering scale, and what's the fix?**

A: Fully connecting *n* VPCs needs **n(n−1)/2** peering connections — 10 VPCs = 45 connections. **Transit Gateway** (hub-and-spoke, transitive routing, *n* attachments instead) exists specifically to solve this mesh explosion.

### Transit Gateway

**Q: What can Transit Gateway do that VPC Peering cannot, and what's the cost trade-off?**

A: Supports **transitive routing** (A → TGW → C works) and scales to thousands of attachments with per-attachment TGW route tables for segmentation. Cost: charged per attachment-hour **plus** per GB — more expensive than peering for a simple two-VPC case.

### VPC Endpoints & PrivateLink

**Q: Gateway endpoint vs Interface endpoint (PrivateLink) — what's the cost and scope difference?**

A: **Gateway endpoint** — S3 and DynamoDB only, a route-table entry pointing at a prefix list, no ENI, **free**. **Interface endpoint** — most other AWS services + SaaS + your own services, an ENI with a private IP in your subnet, per-hour + per-GB cost.

**Q: What are the two most common "the endpoint exists but nothing uses it" causes for an interface endpoint?**

A: Missing **private DNS enabled** (so the standard service hostname still resolves to the public endpoint instead of the private IP), and the endpoint's security group not allowing inbound 443 from the client subnets.

**Q: How do you publish your own internal service across accounts via PrivateLink?**

A: Put an **NLB** in front of it and expose it as an endpoint service; consumers in other VPCs/accounts create interface endpoints to reach it — no peering, no overlapping-CIDR problem, no internet exposure.

### Hybrid Connectivity: Site-to-Site VPN & Direct Connect

**Q: Site-to-Site VPN vs Direct Connect — compare setup time, encryption, and cost.**

A: **VPN** — IPsec over the public internet, minutes-to-hours setup, encrypted by default, cheap. **Direct Connect** — dedicated private fibre, weeks-to-months setup, **not encrypted by default** (add VPN-over-DX or MACsec), high fixed port cost but cheaper egress at volume.

**Q: What's the standard HA answer for hybrid connectivity resilience?**

A: Two DX connections at two different DX locations for full redundancy, or more cheaply, one DX connection with a Site-to-Site VPN as automatic backup.

### Hands-On: VPC

**Q: What's the debug order for "my instance can't reach the internet"?**

A: Route table (0.0.0.0/0 pointing at IGW for public / NAT for private) → is the NAT Gateway actually in a public subnet? → security group outbound → NACL both directions → does the instance have a public IP at all → read Flow Logs for ACCEPT/REJECT.

```bash
# VPC + one public and one private subnet
aws ec2 create-vpc --cidr-block 10.0.0.0/16 --tag-specifications 'ResourceType=vpc,Tags=[{Key=Name,Value=prod-vpc}]'
aws ec2 create-subnet --vpc-id vpc-abc --cidr-block 10.0.1.0/24 --availability-zone us-east-1a  # public
aws ec2 create-subnet --vpc-id vpc-abc --cidr-block 10.0.11.0/24 --availability-zone us-east-1a # private

# Internet gateway + public route (this route is what MAKES the subnet public)
aws ec2 create-internet-gateway
aws ec2 attach-internet-gateway --vpc-id vpc-abc --internet-gateway-id igw-abc
aws ec2 create-route --route-table-id rtb-public --destination-cidr-block 0.0.0.0/0 --gateway-id igw-abc

# NAT gateway (in the PUBLIC subnet) + private route
aws ec2 allocate-address --domain vpc
aws ec2 create-nat-gateway --subnet-id subnet-public --allocation-id eipalloc-abc
aws ec2 create-route --route-table-id rtb-private --destination-cidr-block 0.0.0.0/0 --nat-gateway-id nat-abc

# Free S3 gateway endpoint so private subnets skip the NAT for S3 traffic
aws ec2 create-vpc-endpoint --vpc-id vpc-abc --service-name com.amazonaws.us-east-1.s3 \
  --vpc-endpoint-type Gateway --route-table-ids rtb-private

# Flow logs for debugging
aws ec2 create-flow-logs --resource-type VPC --resource-ids vpc-abc --traffic-type ALL \
  --log-destination-type cloud-watch-logs --log-group-name /aws/vpc/flowlogs \
  --deliver-logs-permission-arn arn:aws:iam::123456789012:role/flowlogsRole
```

### Route 53

**Q: Walk through how DNS resolution actually works, and where Route 53 sits in that chain.**

A:

```
Browser cache → OS cache → Recursive resolver (ISP / 8.8.8.8)
   → Root nameserver (.)            "ask the .com servers"
   → TLD nameserver (.com)          "ask ns-123.awsdns-45.com"
   → Authoritative nameserver       "example.com A = 52.1.2.3"   ← Route 53 lives here
   → answer cached at every hop for the length of the TTL
```

Route 53 is the **authoritative nameserver** — delegation from the registrar's NS records to Route 53's 4 nameservers is what makes it authoritative for the domain. A common real failure: creating the hosted zone but never updating the registrar, or recreating a hosted zone (which issues *different* nameservers).

**Q: A vs ALIAS record — why is ALIAS preferred for AWS targets?**

A: ALIAS is free, resolves without an extra lookup, and works **at the zone apex** (`example.com`) — a CNAME cannot do that by DNS spec, since CNAMEs can't be used at the apex.

**Q: What's the TTL trade-off, and what's standard practice before a planned cutover?**

A: High TTL = fewer queries/lower cost but stale answers linger after a change; low TTL = fast propagation but more queries/cost. Before a migration, lower the TTL well in advance (at least one old-TTL period ahead), make the change, then raise it back.

**Q: Name the routing policies and one distinguishing use case each.**

A: Simple (single endpoint), Weighted (canary/A-B split), Failover (primary/secondary via health checks), Latency-based (lowest measured latency region), Geolocation (legal/regional restriction), Geoproximity (bias-weighted by geography, needs Traffic Flow), Multi-value answer (simple client-side distribution, not a real LB).

**Q: Why is Route 53 alone insufficient for sub-second failover?**

A: DNS is not instant — TTL caching at resolvers/ISPs/clients bounds how fast a failover propagates. Combine with ALB/NLB-level health-based removal for fast reaction; use Route 53 for macro/region-level failover, or pair with **Global Accelerator** (static anycast IPs, sub-minute health-check failover, entry IPs never change).

**Q: What's a Private Hosted Zone gotcha?**

A: It must be **explicitly associated with each VPC** that needs to resolve it — "works in one VPC, not another" almost always means a missing association.

### API Gateway Auth & Integration Patterns

**Q: Compare the four API Gateway auth mechanisms and when to use each.**

A: **Cognito User Pools (JWT authorizer)** — standard username/password or social login, validated at the gateway, no custom auth code. **IAM authorization** — SigV4-signed service-to-service calls within your org. **Lambda custom authorizer** — legacy tokens or non-standard schemes. **API keys + usage plans** — partner/B2B throttle/quota, not real authentication.

---

## Load Balancing, Scalability & Auto Scaling

### Scalability, High Availability, Elasticity & Agility

**Q: Define Scalability, High Availability, Elasticity, and Agility precisely — and give a "scalable but not elastic" example.**

A: **Scalability** — the system *can* handle more load (vertical=bigger instance, hard ceiling; horizontal=more instances, needs statelessness). **High Availability** — surviving failure without downtime via ≥2 AZs (a different goal from scalability — you can be scalable in one AZ and still lose everything). **Elasticity** — scaling automatically in both directions matched to demand (scalability is the capability, elasticity is its automation). **Agility** — how fast you can get new resources at all, unrelated to load. Example: a fixed fleet of 20 instances sized for Black Friday scales (handles peak) but isn't elastic (you pay for 20 in February).

**Q: HA vs Fault Tolerance vs DR — what's the distinction?**

A: HA = minimal downtime within a region (multi-AZ). Fault tolerance = zero interruption from a component failure (redundancy with no impact). DR = recovering from losing an entire region, measured by RTO/RPO.

### ALB vs API Gateway vs ELB (NLB/GWLB/CLB)

**Q: What's the mental model for choosing ALB vs API Gateway?**

A: "ALB is a smart Layer-7 load balancer — I have services, route and balance traffic between them. API Gateway is a full API front door — I'm exposing an API to external clients and need auth, throttling, quotas, versioning, transformation, and monitoring built in."

**Q: ALB vs NLB vs GWLB — what layer and protocol does each operate at?**

A: ALB = Layer 7, HTTP/HTTPS/WebSocket, host/path routing + Lambda targets. NLB = Layer 4, TCP/UDP/TLS, extreme throughput + static IP. GWLB = Layer 3, IP, transparent traffic inspection for firewall/IDS appliances.

### ELB Deep-Dive: Cross-Zone Load Balancing, 504 Timeouts & Shield DDoS Protection

**Q: Is cross-zone load balancing on by default for ALB and NLB?**

A: ALB — on by default, **always** (cannot be disabled). NLB — off by default, a per-target-group toggle (enabling it can add cross-AZ data transfer charges, why teams often leave it off for cost/latency-sensitive NLB use).

**Q: What are three root causes of an ELB 504 Gateway Timeout?**

A: An unhealthy/slow target (app hung, DB call blocking); a traffic spike overwhelming backend capacity before auto-scaling reacts; or a mismatch between the ALB idle timeout (default 60s) and the backend's legitimate response time — fix by raising the ALB idle timeout, not chasing a phantom bug.

**Q: Shield Standard vs Shield Advanced — what's covered, and how does WAF fit in?**

A: **Shield Standard** — free, automatic on every account for Route 53/CloudFront/ELB, covers common L3/L4 DDoS. **Shield Advanced** — paid opt-in, larger-attack mitigation, real-time visibility, WAF integration, cost protection, 24/7 DRT access. **WAF** defends L7 (malicious patterns, rate limiting) — complementary to Shield, not competing; name both together.

### Load Balancing Fundamentals

**Q: What are the four load-balancer objects in order, and what target type does Fargate require?**

A: Listener (port+protocol) → Rules (host/path/header/query/IP/method conditions) → Target Group (EC2/IP/Lambda/ALB) → Targets (health-checked). Fargate and `awsvpc` ECS tasks require the **`ip`** target type, since they have their own ENIs.

**Q: What's Slow Start, and why does it matter for .NET/JVM targets specifically?**

A: Ramps traffic to a new target gradually over 30–900s instead of a full share immediately — off by default. Genuinely useful for .NET/JVM because a fresh process has a cold JIT and empty caches; without it, a newly joined target's p99 spikes or it fails health checks the moment it takes full load.

**Q: Why does an ALB have no static IP, and what's the fix when a partner firewall needs one?**

A: An ALB only has a DNS name — the IPs behind it change. Fix: **NLB**, **NLB fronting an ALB**, or **Global Accelerator**.

**Q: What ASP.NET Core middleware do you need behind an ALB, and why?**

A: `ForwardedHeadersMiddleware`, because ALB terminates HTTP and passes the original client IP via `X-Forwarded-For` — without the middleware every client looks like the load balancer, silently breaking rate limiting, geo-logic, and audit logs.

### Sticky Sessions (Session Affinity)

**Q: What are ALB's two stickiness mechanisms, and what's the fundamental trade-off?**

A: **Duration-based** (LB-generated `AWSALB` cookie) or **application-based** (LB honours the app's own `AWSALBAPP` cookie). Trade-off: stickiness undermines even load distribution (one client can hot-spot a target), and sessions are lost anyway when a target is removed on scale-in/deploy — it never truly guarantees session survival. The senior fix is externalizing session state (ElastiCache/DynamoDB, `IDistributedCache` in .NET) rather than relying on stickiness.

### Connection Draining / Deregistration Delay

**Q: What's the "deploy without dropping requests" complete answer?**

A: Chain three things: an appropriate **deregistration delay** (default 300s, tuned to your longest legitimate request), **ELB health checks** on the ASG, and **ASG lifecycle hooks** so an instance is warmed before serving and drained before dying — plus **ASG instance refresh** for rolling replacement.

### Auto Scaling Groups (ASG)

**Q: What's the ASG health-check-type gotcha, and why is it a real production incident?**

A: Default health-check type is **EC2 only** — replaces instances only on hypervisor-level failure. An instance whose app has hung or returns 500s looks healthy to EC2 and is **never replaced**, even though the ALB stopped sending it traffic. Must set health-check type to **ELB** so the ASG acts on the load balancer's application-level view.

**Q: What's the senior differentiator for choosing an ASG scaling metric over CPU?**

A: `ALBRequestCountPerTarget` for a web/API tier (proportional to demand, reacts before CPU); **SQS backlog per instance** for a worker tier — canonical because a worker blocked on I/O shows low CPU while the backlog grows, so CPU would miss the signal entirely.

**Q: What do ASG lifecycle hooks let you do, and what's a warm pool for?**

A: Lifecycle hooks pause an instance in `Pending:Wait` (bootstrap/warm caches/register) or `Terminating:Wait` (drain/flush/deregister) before it proceeds. **Warm pools** keep pre-initialized, stopped instances ready so scale-out skips boot/bootstrap time — for apps with slow startup where predictive scaling isn't enough.

```bash
aws autoscaling create-auto-scaling-group --auto-scaling-group-name web-asg \
  --launch-template LaunchTemplateName=web-lt,Version='$Latest' \
  --min-size 2 --max-size 10 --desired-capacity 2 \
  --vpc-zone-identifier "subnet-a,subnet-b,subnet-c" \
  --target-group-arns arn:aws:elasticloadbalancing:... \
  --health-check-type ELB --health-check-grace-period 120     # ← ELB, not EC2
aws autoscaling start-instance-refresh --auto-scaling-group-name web-asg
```

### Scalability Best Practices

**Q: List the core scalability best practices to recite.**

A: Stateless app tier; scale out not up for anything distributable; minimum 2 instances across ≥2 AZs; ASG health check type = ELB with a real `/health`; scale on a demand-correlated metric not CPU; externalize session/cache state; scheduled/predictive scaling for known events; test scale-in (not just scale-out) since most bugs surface on the way down; cap `max` deliberately as a cost/blast-radius ceiling.

### Scalability & Load Balancing Shared Responsibility Model

**Q: In the scalability/LB shared-responsibility split, what's on you vs AWS?**

A: AWS: running/scaling the ELB fleet, the ASG control plane, health-check infrastructure, AZ-level availability, TLS termination capability. You: choosing the right LB type, setting min/desired/max and the scaling metric, writing a health endpoint that reflects real health, actually spanning multiple AZs, certificate lifecycle, tuning lifecycle hooks/deregistration delay, and designing the app to be stateless.

---

## Messaging, Streaming & Decoupling

### SQS & SNS Fundamentals

**Q: Standard vs FIFO SQS queues — compare delivery, ordering, and throughput.**

A: **Standard** — at-least-once (possible duplicates), no ordering guarantee, very high throughput. **FIFO** — exactly-once processing (with dedup), guaranteed order per Message Group, lower throughput (though high-throughput mode raises this).

**Q: Delay queue vs visibility timeout — what's the difference, and why is it a favorite trap?**

A: **Delay queue** delays **new** messages 0–15 min before they become visible at all. **Visibility timeout** hides a message **after** it's received, while being processed (default 30s, max 12h) — must exceed worst-case processing time or the message reappears and gets processed twice. Use `ChangeMessageVisibility` as a heartbeat for variable-length work rather than one huge global timeout.

**Q: What's the SQS/SNS "claim check" pattern, and what's the message size limit it works around?**

A: Both cap messages at **256KB**. The **SQS/SNS Extended Client Library** stores the body in S3 and puts a pointer in the message for larger payloads.

**Q: What does SNS message filtering solve, and why is it a notable gap in most candidates' answers?**

A: Subscribers declare a JSON filter on message attributes (or body), so each receives only events it cares about — avoids every consumer receiving every event and filtering in code, saving invocations/cost/complexity at the topic level.

**Q: Why combine SNS + SQS rather than use either alone for event-driven microservices?**

A: SNS gives fan-out (push to N heterogeneous subscribers); SQS gives durability, retries, DLQ, and per-consumer isolation — one publisher, each interested microservice gets its own queue, so no consumer's slowness/downtime blocks or drops events for another.

### CQRS with SNS/SQS in .NET

**Q: What's the golden rule for when to publish a domain event to SNS?**

A: Publish only **after** the write-side DB transaction commits successfully — never speculatively before commit. For stronger guarantees, use the **Outbox Pattern** (write event + data in the same DB transaction, a separate relay publishes it).

```csharp
public record OrderCreatedEvent(Guid EventId, Guid OrderId, decimal Amount, DateTime CreatedAt);

public class SnsPublisher
{
    private readonly IAmazonSimpleNotificationService _sns;
    private readonly string _topicArn;

    public SnsPublisher(IAmazonSimpleNotificationService sns, IConfiguration config)
    {
        _sns = sns;
        _topicArn = config["AWS:SNS:OrderCreatedTopicArn"];
    }

    public async Task PublishAsync(OrderCreatedEvent evt)
    {
        var message = JsonSerializer.Serialize(evt);
        var request = new PublishRequest
        {
            TopicArn = _topicArn,
            Message = message,
            MessageAttributes =
            {
                ["eventType"] = new MessageAttributeValue { DataType = "String", StringValue = "OrderCreated" }
            }
        };
        await _sns.PublishAsync(request);
    }
}

// Command handler
public async Task CreateOrderAsync(CreateOrderCommand cmd)
{
    await _db.SaveChangesAsync();               // 1. commit write DB first
    await _snsPublisher.PublishAsync(            // 2. publish AFTER commit succeeds
        new OrderCreatedEvent(Guid.NewGuid(), cmd.OrderId, cmd.Amount, DateTime.UtcNow));
}
```

**Q: What's the classic SNS-envelope bug when consuming via SQS?**

A: SNS wraps the real payload inside an envelope (`Type`, `Message`, `MessageId`, `TopicArn`) — forgetting to unwrap `snsEnvelope.Message` before deserializing the actual event is a common mistake.

```csharp
public class OrderCreatedConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly string _queueUrl;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = 5,
                WaitTimeSeconds = 20 // long polling
            });

            foreach (var message in response.Messages)
            {
                try
                {
                    await ProcessMessageAsync(message);
                    await _sqs.DeleteMessageAsync(_queueUrl, message.ReceiptHandle); // delete only after success
                }
                catch (Exception ex)
                {
                    // do NOT delete — SQS will retry, then DLQ after maxReceiveCount
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }

    private async Task ProcessMessageAsync(Message message)
    {
        // SNS wraps the real payload inside an envelope — a classic interview detail
        var snsEnvelope = JsonSerializer.Deserialize<SnsEnvelope>(message.Body);
        var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(snsEnvelope.Message);

        if (await AlreadyProcessed(evt.EventId)) return;   // idempotency check — mandatory
        await UpdateReadDatabase(evt);
    }
}

public class SnsEnvelope
{
    public string Type { get; set; }
    public string Message { get; set; }
    public string MessageId { get; set; }
    public string TopicArn { get; set; }
}
```

**Idempotency tracking:**

```csharp
private async Task<bool> AlreadyProcessed(Guid eventId) =>
    await _db.ProcessedEvents.AnyAsync(x => x.EventId == eventId);
```

**Q: When should you delete an SQS message — before or after processing?**

A: **After** successful processing only. Deleting before/immediately risks silent data loss on a crash mid-processing; not deleting on failure lets SQS retry naturally, then DLQ after `maxReceiveCount`.

**Registration (`Program.cs`):**

```csharp
builder.Services.AddAWSService<IAmazonSimpleNotificationService>();
builder.Services.AddAWSService<IAmazonSQS>();
builder.Services.AddSingleton<SnsPublisher>();
builder.Services.AddHostedService<OrderCreatedConsumer>();
```

### CQRS + SNS/SQS Interview Pitfalls

**Q: Give the 60-second wrap-up answer on CQRS + SNS/SQS pitfalls.**

A: "The biggest pitfalls are assuming immediate consistency, exactly-once delivery, or shared queues. Events must publish only after successful commits, consumers must be idempotent, and each consumer needs its own queue with a DLQ. Ordering, retries, and replay have to be designed explicitly — otherwise you get silent data loss or duplicate side effects."

**Q: Why are DB triggers a worse alternative to explicit events for cross-service notification?**

A: Triggers are invisible, hard to version, and non-portable; explicit events are observable, testable contracts.

### EventBridge Deep Dive

**Q: What does EventBridge add over plain SNS/SQS?**

A: Event buses (default/custom/partner), rich **content-based filtering** at the rule level (no per-consumer filter code), a **Schema Registry** with versioning/typed code generation, **Archive & Replay**, and targets including API destinations (arbitrary HTTPS with managed retries) and native scheduled/cron rules.

**Q: When do you pick EventBridge over SNS?**

A: EventBridge for cross-service domain events needing rich routing logic, schema management, or scheduled jobs. SNS+SQS for simple, high-throughput fan-out with no filtering/schema need. Many architectures use both — EventBridge between bounded contexts, SNS/SQS for fan-out within one.

**Q: Show a .NET-relevant EventBridge scheduled rule (CLI/CloudFormation).**

A:

```yaml
Resources:
  NightlyCleanupRule:
    Type: AWS::Events::Rule
    Properties:
      ScheduleExpression: "cron(0 1 * * ? *)"
      Targets:
        - Arn: !GetAtt CleanupFunction.Arn
          Id: "CleanupTarget"
```

### Event-Driven Architecture Reference Flow

**Q: In the reference event-driven order-processing flow (API Gateway → Lambda → DynamoDB → SNS → SQS → Lambda), what single property must every downstream step have, and why?**

A: **Idempotency** — SQS is at-least-once, so the same message can legitimately arrive twice; every step (ingest, processor, external integrations) must handle duplicate delivery safely.

```
  Client
    |  POST /orders
    v
  API Gateway
    |  invoke
    v
  Lambda (Ingest) ----PutItem status=PENDING + idempotencyKey----> DynamoDB
    |
    |  Publish "order_created"
    v
  SNS Topic
    +----fan-out----> SQS (Billing) --------+
    +----fan-out----> SQS (Notification)    |
                                            |  poll & invoke
                                            v
                                    Lambda (Processor)
                                            |
                +---------------------------+---------------------------+
                v                           v                           v
      ConditionalUpdate            Update status =            after maxReceiveCount
      PENDING -> PROCESSING        COMPLETED                  failures -> DLQ
           (DynamoDB)                (DynamoDB)

  Every downstream step must be idempotent: SQS is at-least-once, so the
  same message can legitimately arrive twice.
```

**Q: Name five items from the production-readiness checklist for an event-driven pipeline.**

A: Idempotency on ingest via dedup key; DLQ + alarm on depth>0; conditional updates in DynamoDB to prevent races; SQS visibility timeout > Lambda max execution time; X-Ray/OpenTelemetry tracing end-to-end. (Also: least-privilege IAM + SSE-KMS, load testing, documented DLQ replay runbooks, schema evolution strategy.)

### Amazon Kinesis

**Q: What's the fundamental difference between a Kinesis stream and an SQS queue?**

A: A stream is a **replayable, ordered log** that multiple independent consumers can each read in full; a queue message is consumed once and deleted (competing consumers share the work).

**Q: What decides which shard a record lands on, and what's the failure mode of a bad choice?**

A: The **partition key**. Ordering is guaranteed only within a shard, so pick a key that both distributes evenly and keeps records needing order together (e.g. `customerId`). A low-cardinality key creates a **hot shard** — same failure shape as a DynamoDB hot partition.

**Q: Firehose vs Data Streams — when do you pick each?**

A: **Firehose** — near-real-time, buffered, serverless/no shards, can transform via Lambda + convert to Parquet/ORC, **cannot replay**. "Just get data into S3/OpenSearch reliably, no code" → Firehose. **Data Streams** — true real-time, replayable (up to 365 days retention), you manage shards/consumers. "Multiple consumers, replay, sub-second, custom processing" → Data Streams.

**Q: Compare SQS, SNS, EventBridge, and Kinesis Data Streams on replay capability.**

A: SQS — no replay (deleted once processed). SNS — no replay (no storage). EventBridge — yes, via Archive & Replay. Kinesis Data Streams — yes, within the retention window (up to 365 days), and uniquely supports many independent consumers each reading everything.

### Step Functions: Orchestration vs Choreography

**Q: Standard vs Express Step Functions workflows — compare duration, execution model, and pricing.**

A: **Standard** — max 1 year, exactly-once fully durable, full visual history (90 days), priced per state transition; for long-running business processes/human approval/ETL. **Express** — max 5 minutes, at-least-once, CloudWatch Logs only, priced per request+duration (cheaper at high volume); for high-volume short-lived event processing.

**Q: How does Step Functions implement the Saga pattern for distributed transactions?**

A: `Catch` on each step triggers compensating actions (refund payment, release inventory) — the standard answer to "how do you do transactions across microservices with no two-phase commit."

**Q: Orchestration vs choreography — what's the senior framing, and when do you use both?**

A: Orchestration (Step Functions) = a central coordinator holds the process, full visibility, best for a defined business process needing ordering/compensation/audit trail. Choreography (EventBridge/SNS+SQS) = each service reacts independently, best for loose extensible fan-out. Use both at different layers: choreography between bounded contexts, orchestration within one for multi-step processes.

### Amazon MQ

**Q: What's the one reason to choose Amazon MQ over SQS/SNS?**

A: **Protocol compatibility** — it speaks open standards (AMQP, MQTT, STOMP, OpenWire, JMS, WSS) that existing enterprise apps already use, whereas SQS/SNS expose proprietary AWS APIs. It's broker-based (runs on instances in your VPC, Multi-AZ active/standby), so it doesn't scale like SQS — the migration path for lift-and-shift, not for new AWS-native builds.

---

## Global Edge Services

### CloudFront (CDN)

**Q: Define Region, Availability Zone, and Edge Location/PoP.**

A: **Region** — a geographic area (`us-east-1`). **AZ** — one or more discrete data centers within a region, isolated for failure, linked by low-latency fibre. **Edge Location/PoP** — one of 600+ CloudFront caches worldwide, for caching and backbone entry, not for running workloads.

**Q: What does CloudFront give you beyond raw speed?**

A: Origin offload (cached hits never reach origin, cutting load and egress cost — CloudFront→origin from AWS origins is free), built-in Shield Standard + WAF integration at the edge, and TLS termination with a free ACM cert.

**Q: What's the better practice than CloudFront invalidations for cache busting?**

A: **Versioned filenames/query strings** (`app.a1b2c3.js`) — new content gets a new cache key, nothing needs purging. Invalidations are slow and billed beyond a free monthly allowance.

**Q: What is OAC, and why is it better than the legacy OAI?**

A: **Origin Access Control** lets CloudFront sign requests to a private S3 bucket so it stays fully closed to the internet — unlike OAI, it also supports SSE-KMS and all HTTP methods.

**Q: CloudFront signed URL vs S3 presigned URL — what's the difference?**

A: **S3 presigned URL** — generated by the S3 API, carries the generating principal's IAM permissions, hits S3 directly, bypasses CloudFront's cache/protections. **CloudFront signed URL** — created with a CloudFront key pair, served through the edge (keeps caching/WAF/Shield/logging), covers any origin type, lets the bucket stay private. Use CloudFront signing when content is delivered through the CDN; S3 presigning for direct short-lived programmatic access.

**Q: CloudFront Functions vs Lambda@Edge — what's the difference?**

A: **CloudFront Functions** — lightweight JS, sub-millisecond, viewer request/response only, no network calls; for header manipulation, URL rewrites, simple auth checks. **Lambda@Edge** — Node.js/Python, up to 5–30s, all four trigger points including origin request/response, can call other services; for origin selection logic, DB/API calls, heavier auth.

**Q: What's the classic CloudFront certificate region gotcha?**

A: A custom-domain cert for CloudFront must be requested/imported in **`us-east-1`** regardless of origin/user location, because CloudFront is managed globally from N. Virginia. ALB certs, by contrast, must be in the ALB's own region.

### AWS Global Accelerator

**Q: What is Global Accelerator, and what's its biggest advantage over Route 53 failover routing?**

A: Two static anycast IPs fronting your app; traffic enters the AWS private backbone at the nearest edge and travels internally to the closest healthy regional endpoint. Failover is **~30 seconds and DNS-independent** — the IPs never change, so no client/resolver/ISP TTL cache can delay it.

**Q: What protocols does Global Accelerator work with, that CloudFront can't?**

A: Layer 4, TCP **and UDP** — any protocol (gaming, VoIP, IoT, MQTT, custom TCP), not just HTTP.

### CloudFront vs Global Accelerator

**Q: Give the one-liner distinguishing CloudFront from Global Accelerator.**

A: "CloudFront caches HTTP content at the edge; Global Accelerator doesn't cache anything — it just gets any TCP/UDP traffic onto the AWS backbone sooner and gives two static IPs with fast regional failover. Cacheable web content → CloudFront. Dynamic, non-HTTP, or static-IP/fast-failover requirements → Global Accelerator. They can be combined."

### Local Zones, Outposts & Wavelength

**Q: Local Zones vs Outposts vs Wavelength — what does each extend, and to where?**

A: **Local Zones** — region extension into a major metro area (subset of services) for single-digit-ms latency to a specific city. **Outposts** — physical AWS racks in your own data center, for data-residency or on-prem latency needs. **Wavelength** — AWS compute embedded inside 5G telco networks, for ultra-low-latency mobile (AR/VR, connected vehicles).

### Hands-On: CloudFront & Global Accelerator

**Q: How do you verify CloudFront cache performance, and diagnose a low hit ratio?**

A: Check the `X-Cache` response header (`Hit from cloudfront` vs `Miss from cloudfront`) and the cache hit ratio metric — a low ratio almost always means forwarding too many headers/cookies/query strings into the cache key.

```bash
# CloudFront in front of a private S3 bucket (the standard static-site setup)
aws cloudfront create-origin-access-control --origin-access-control-config \
  'Name=s3-oac,SigningProtocol=sigv4,SigningBehavior=always,OriginAccessControlOriginType=s3'
# then: create the distribution with the S3 origin + that OAC, default root object index.html
# and add the generated bucket policy allowing cloudfront.amazonaws.com with AWS:SourceArn = distribution ARN

aws cloudfront create-invalidation --distribution-id E123ABC --paths "/index.html" "/static/*"
aws cloudfront get-distribution --id E123ABC --query 'Distribution.Status'   # InProgress → Deployed

# Global Accelerator
aws globalaccelerator create-accelerator --name prod-ga --ip-address-type IPV4
aws globalaccelerator create-listener --accelerator-arn <arn> --protocol TCP --port-ranges FromPort=443,ToPort=443
aws globalaccelerator create-endpoint-group --listener-arn <arn> \
  --endpoint-group-region us-east-1 --traffic-dial-percentage 100 \
  --endpoint-configurations EndpointId=<alb-arn>,Weight=100,ClientIPPreservationEnabled=true
```

---

## Security Services

### Overview: Which Service Answers Which Question

**Q: Map "is something bad happening right now" vs "what weaknesses do I have" vs "where is my sensitive data" vs "is anything misconfigured/drifting" to their services.**

A: Bad happening now → **GuardDuty**. Weaknesses → **Inspector**. Sensitive data location → **Macie**. Misconfiguration/drift → **AWS Config**. (Single pane over all of them → Security Hub; root-cause investigation → Detective.)

### DDoS Protection: Shield & WAF

**Q: Name the three DDoS attack shapes.**

A: **Volumetric** (L3/4 — saturate bandwidth, e.g. UDP reflection/amplification, SYN floods); **protocol** (exploiting TCP/IP behavior); **application-layer** (L7 — HTTP floods, Slowloris; low bandwidth but expensive per request since each hits the app/DB).

**Q: Shield Standard vs Advanced — what's included in each?**

A: **Standard** — free, automatic on every account, covers Route 53/CloudFront/Global Accelerator/ELB against common L3/4 attacks. **Advanced** — paid (~$3,000/mo org-wide), larger-scale mitigation, 24/7 SRT access, cost-protection credits, health-based detection, and **WAF included at no extra charge**.

**Q: Why doesn't WAF attach to an NLB, and what's the WAF best practice before going live with a new rule?**

A: WAF needs HTTP context — NLB is Layer 4, so WAF only attaches to CloudFront/ALB/API Gateway/AppSync/Cognito. Best practice: deploy every new rule in **Count mode** first, review logs for what it would have blocked, then switch to Block — going straight to Block is how teams take down their own legitimate traffic.

**Q: What does AWS Firewall Manager centralize?**

A: WAF rules, Shield Advanced protections, security-group policies, and Network Firewall rules across every account in an Organization, applied automatically to newly created resources too.

### AWS Network Firewall

**Q: What can Network Firewall inspect that WAF and Security Groups cannot?**

A: All traffic (Layer 3–7, not just HTTP) at the VPC level — stateful filtering, **domain-name filtering for egress** (allowlist `*.microsoft.com`), and Suricata-compatible IPS rules for deep packet inspection. Right answer for compliance-driven egress allowlisting and non-HTTP intrusion detection.

### KMS & CloudHSM

**Q: AWS owned vs AWS managed vs customer managed KMS keys — what's the difference?**

A: **AWS owned** — invisible, shared. **AWS managed** (`aws/s3`, `aws/ebs`) — free, auto-rotated, policy not editable. **Customer managed (CMK)** — you create it, your own key policy, optional annual auto-rotation, mandatory 7–30 day deletion waiting period.

**Q: Why is "IAM says allow but KMS still denies" a real failure mode?**

A: The key's **resource (key) policy is mandatory and authoritative** — an IAM policy granting `kms:Decrypt` alone is not sufficient unless the key policy delegates to IAM (or names the principal directly). This is unlike most AWS resources.

**Q: What is envelope encryption, and why does it exist?**

A: The `Encrypt` API only handles up to 4KB. For larger data, `GenerateDataKey` returns a plaintext data key + an encrypted copy; you encrypt data locally with the plaintext key, discard it, store the encrypted key alongside the ciphertext. This is what S3/EBS do internally, and it's why KMS request quotas matter at high throughput (hence S3 Bucket Keys).

**Q: KMS vs CloudHSM — what's the deciding factor?**

A: "KMS unless a regulator specifically requires that AWS cannot possibly access my keys — then CloudHSM (single-tenant dedicated hardware, you manage keys entirely, AWS cannot recover them if lost)."

#### Worked Example: Giving a Fargate Task Access to KMS-Encrypted S3 Data

**Q: A Fargate task's IAM policy clearly allows `s3:GetObject` but it still can't read the KMS-encrypted bucket — what four things must all be true?**

A: (1) The **task role** (not task execution role) needs both `s3:GetObject` and `kms:Decrypt`/`DescribeKey`, ideally scoped with `kms:ViaService`. (2) The **KMS key policy** must also name that role — mandatory and authoritative, separate from IAM. (3) The **bucket policy**, if restrictive, must allow it too (mandatory for cross-account). (4) The **network path** — S3 has a free gateway endpoint, but **KMS has no gateway endpoint**, needing an interface endpoint or NAT, or the request just hangs and times out.

The task role's IAM policy:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    { "Sid": "ReadObjects", "Effect": "Allow",
      "Action": ["s3:GetObject", "s3:ListBucket"],
      "Resource": ["arn:aws:s3:::my-bucket", "arn:aws:s3:::my-bucket/*"] },

    { "Sid": "DecryptWithTheCmk", "Effect": "Allow",
      "Action": ["kms:Decrypt", "kms:DescribeKey"],
      "Resource": "arn:aws:kms:us-east-1:111122223333:key/1234abcd-...",
      "Condition": { "StringEquals": { "kms:ViaService": "s3.us-east-1.amazonaws.com" } } }
  ]
}
```

The KMS key policy must separately name that same role:

```json
{
  "Sid": "AllowTaskRoleToDecrypt",
  "Effect": "Allow",
  "Principal": { "AWS": "arn:aws:iam::111122223333:role/my-app-task-role" },
  "Action": ["kms:Decrypt", "kms:DescribeKey"],
  "Resource": "*"
}
```

**Q: How do you read the error to diagnose which of the four is wrong?**

A: `AccessDenied` on `GetObject` → S3 permission missing. `KMS.AccessDeniedException` (or the misleading "CMK does not exist... or you are not allowed to access") → `kms:Decrypt` missing or key policy doesn't name the role. Hangs then times out → missing VPC endpoint/NAT. Works in dev, fails in prod → per-environment CMK whose key policy was never updated.

#### Encryption in Transit (TLS) — End to End

**Q: Why is "we use TLS end to end" often an overstatement?**

A: TLS terminates at every hop separately — CloudFront→origin (Origin Protocol Policy), ALB→target (target group protocol, often plain HTTP inside the VPC, which is defensible but not "end to end"). True end-to-end needs HTTPS on the ALB→target hop too, and the ALB doesn't validate the target's cert (a self-signed cert is fine there — you're encrypting the hop, not authenticating the backend).

```
Client --TLS(ACM)--> CloudFront --TLS--> ALB --TLS or plain HTTP?--> ECS task --TLS?--> RDS
                                  ^                    ^                        ^
                          viewer protocol       target group protocol    force_ssl / sslmode
```

**Q: How do you enforce (not just hope for) TLS at each AWS layer?**

A: S3 — `Deny` with `aws:SecureTransport: false`. ALB — HTTP:80 listener whose only action is redirect to HTTPS. RDS — `rds.force_ssl=1`/`require_secure_transport`. ElastiCache — enable in-transit encryption at cluster **creation** (can't add later). EFS — mount with `-o tls`.

### ACM (AWS Certificate Manager)

**Q: DNS validation vs email validation for ACM certs — which do you always choose, and why?**

A: **DNS validation** — add a CNAME, auto-renews forever while the record stays in place. Email validation is manual and breaks renewal if nobody clicks the link. Always choose DNS.

**Q: Why can't you install an ACM public certificate directly on an EC2 instance?**

A: The private key of an ACM public certificate **cannot be exported** — terminate TLS at an ALB/CloudFront instead, or use **ACM Private CA** (which does allow export, for internal certs, paid).

### AWS Systems Manager (SSM)

**Q: How does Session Manager replace a bastion host, and what does it require?**

A: No SSH keys, no open inbound ports (SSM Agent makes an **outbound** connection, so the instance can sit in a fully private subnet with no inbound rules), no bastion to run/patch/pay for, access controlled by IAM, every session logged to CloudTrail and optionally recorded keystroke-by-keystroke. Requires: SSM Agent running, an instance profile with `AmazonSSMManagedInstanceCore`, and network reachability to SSM endpoints (NAT or interface VPC endpoints for `ssm`/`ssmmessages`/`ec2messages`).

```bash
aws ssm start-session --target i-0abc123
# Port-forward a private RDS/RDP endpoint to localhost — replaces an SSH tunnel through a bastion
aws ssm start-session --target i-0abc123 \
  --document-name AWS-StartPortForwardingSessionToRemoteHost \
  --parameters '{"host":["mydb.abc.us-east-1.rds.amazonaws.com"],"portNumber":["5432"],"localPortNumber":["5432"]}'
```

**Q: Name three other SSM capabilities beyond Session Manager.**

A: **Patch Manager** (scheduled OS patching via baselines/maintenance windows — the concrete answer to "guest OS patching is your responsibility"); **Run Command** (execute a script across a tagged fleet, no SSH, full audit log); **State Manager** (enforces desired configuration continuously, corrects drift).

### AWS Artifact

**Q: What does AWS Artifact actually provide, and what's the trick to avoid claiming about it?**

A: A self-service portal for compliance documents (SOC 1/2/3, PCI DSS AOC, ISO 27001, FedRAMP, HIPAA BAA). It does **not** scan, monitor, or secure anything — it's a document repository proving the underlying infrastructure's compliance; everything above the line you must evidence yourself with Config/CloudTrail/Security Hub.

### GuardDuty

**Q: What does GuardDuty analyze, and does enabling it require turning on the underlying logs?**

A: Continuously analyzes CloudTrail management events, VPC Flow Logs, and DNS logs (plus optional S3 data events, EKS audit logs, RDS login activity, Lambda network activity, EBS malware scanning) via ML/threat intel. **Agentless and log-free to set up** — reads sources directly, no need to turn on or pay for the logs themselves.

**Q: What's a typical high-value GuardDuty finding for a resume claiming Lambda/EC2 work?**

A: EC2 instance credentials being used from **outside AWS** — i.e. stolen role credentials (ties back to the IMDSv2/SSRF risk). Findings route to EventBridge to automate response (isolate an SG, revoke sessions).

### Inspector

**Q: Inspector vs GuardDuty — what's the complete answer to this pairing?**

A: **Inspector finds weaknesses** — unpatched CVEs, vulnerable dependencies ("the door has a weak lock"), continuous/event-driven rescanning on new image/instance/CVE. **GuardDuty finds active threats** — malicious behavior happening now ("someone is picking the lock"). Complementary; name both with that distinction.

### Macie

**Q: What does Macie do, and what question does it uniquely answer?**

A: ML-based sensitive-data discovery for S3 — inventories buckets (public/unencrypted/externally-shared) and classifies PII/credentials/financial/health data in object contents. Answers "do we have customer PII sitting in a data-lake bucket nobody remembered?" at scale.

### AWS Config

**Q: What's the difference between "we detect drift" and "we prevent drift" in AWS Config terms?**

A: Detection = managed/custom **rules** evaluating resource configuration against policy (`s3-bucket-public-read-prohibited`, `iam-user-mfa-enabled`). Prevention = **remediation actions** via SSM Automation that auto-fix a violation (e.g., re-enable Block Public Access the moment someone disables it) — that's what separates detecting from actually preventing.

### Security Hub & Detective

**Q: Security Hub vs Detective — what question does each answer?**

A: **Security Hub** — normalizes/aggregates findings from GuardDuty/Inspector/Macie/Access Analyzer/Config into one pane, scores against standards (CIS, PCI DSS): "what is wrong?" **Detective** — builds an interactive behavior graph from CloudTrail/Flow Logs/GuardDuty to investigate root cause: "how did it happen and how far did it spread?"

### Defence in Depth — The Summary Answer

**Q: Give the one-liner defense-in-depth summary, and which layer would you check first in a real incident?**

A: "No single control is the answer — a failure at any one layer isn't sufficient to cause a breach. The layer I'd check first in any real incident is IAM, because on AWS most breaches are permission or configuration failures, not infrastructure failures." Layers: Edge (Route53/CloudFront/Shield/WAF) → Network (VPC/SG/NACL/Network Firewall) → Identity (IAM/MFA/SCPs) → Data (KMS/TLS/Object Lock) → Secrets (Secrets Manager/IAM DB auth) → Detect (GuardDuty/Inspector/Macie/Config) → Audit (CloudTrail/Flow Logs) → Aggregate (Security Hub) → Respond (Detective).

```
Edge          Route 53 (DNSSEC) → CloudFront + Shield + WAF
Network       VPC, private subnets, Security Groups, NACLs, Network Firewall, VPC endpoints
Identity      IAM least privilege, roles not keys, MFA, SCPs, permissions boundaries
Data          KMS/CloudHSM encryption at rest, TLS in transit (ACM), S3 Object Lock
Secrets       Secrets Manager / Parameter Store, IAM DB auth — never in code
Detect        GuardDuty (threats) · Inspector (vulnerabilities) · Macie (sensitive data) · Config (drift)
Audit         CloudTrail (org trail → locked log-archive account), VPC Flow Logs, Access Analyzer
Aggregate     Security Hub (one pane) → EventBridge → automated remediation
Respond       Detective for investigation, documented runbooks, tested restores
```

---

# PART III — Tier 3: Breadth — Recognise and Place

## Management, Organizations & Billing

### AWS Organizations

**Q: Why use multiple AWS accounts instead of one account with IAM separation?**

A: An account is the **strongest isolation boundary** AWS offers — a compromised dev account cannot touch prod resources at all, unlike IAM separation within one account. Also: blast-radius containment, per-account service quotas (a runaway workload can't consume prod's Lambda concurrency), and clean cost attribution.

**Q: What's the best practice for the management (payer) account, and why?**

A: It should hold **no workloads** — only billing and org administration — because SCPs cannot restrict it, making it the most privileged place in the estate.

### Service Control Policies (SCPs)

**Q: What's the single most-asked SCP gotcha?**

A: **The management account is not affected by SCPs at all**, no matter where attached — the reason you keep workloads out of it. SCPs also restrict even the root user of a *member* account (unlike IAM policies).

**Q: Deny-list vs allow-list SCP strategy — what's the trade-off?**

A: Deny-list (keep `FullAWSAccess`, add explicit `Deny` statements) — most common, less work. Allow-list (remove `FullAWSAccess`, enumerate exactly what's permitted) — tighter, much more work.

### Consolidated Billing

**Q: What are the two mechanisms by which consolidated billing actually saves money?**

A: (1) **Aggregated volume discounts** — tiered pricing calculated on combined usage across accounts, reaching cheaper tiers sooner. (2) **RI/Savings Plan sharing** — unused commitment in one account automatically covers matching usage in any other org account (usually the bigger win; can be disabled per account).

**Q: What's the cost-allocation-tag gotcha?**

A: Tags must be explicitly **activated** in the Billing console before appearing in cost reports, and activation is **not retroactive** — historic spend never gets tagged.

### AWS Control Tower

**Q: What does Control Tower give you in one setup wizard, and what are the three guardrail flavors?**

A: A multi-account structure with log-archive/audit accounts, IAM Identity Center, org-wide CloudTrail/Config, and baseline guardrails. Guardrails: **preventive** (SCPs — action blocked), **detective** (Config rules — violation reported), **proactive** (CloudFormation hooks — resource blocked before deployment).

**Q: "How would you set up a secure multi-account AWS environment from scratch?" — what's the answer?**

A: **Control Tower** — hand-building a landing zone (org structure, SCPs, centralized logging, SSO, guardrails, account vending) is weeks of work that's easy to get subtly wrong.

### AWS Resource Access Manager (RAM)

**Q: What's the standard multi-account networking pattern using RAM?**

A: A central **networking account** owns one well-designed VPC and shares its subnets to workload accounts via RAM — one coherent network, no VPC peering, no overlapping-CIDR problems, no per-team network design, while IAM boundaries stay intact.

**Q: RAM vs cross-account roles — what's the difference?**

A: RAM shares the **resource itself** (the subnet exists once, used by many accounts); a cross-account role shares **permission to act** in the owning account.

### Cost Explorer

**Q: What granularity is free vs paid in Cost Explorer, and what recommendations does it produce?**

A: Monthly/daily granularity is included; **hourly and resource-level granularity costs extra**. Built-in recommendations: EC2 rightsizing, RI/Savings Plans purchase recommendations with utilization/coverage reports.

### AWS Budgets

**Q: What's the strong answer beyond just alerting — Budget Actions?**

A: A budget can automatically **apply an IAM/SCP deny policy** or **stop EC2/RDS instances** when a threshold is breached — turning a notification into an actual control, ideal for capping a sandbox account.

### Cost Anomaly Detection

**Q: Budgets vs Cost Anomaly Detection — what's the distinction?**

A: Budgets answer "tell me when I cross a limit I defined" — needs you to know the right number, misses a 300% spike in a small service that stays under the overall budget. Anomaly Detection answers "tell me when something is weird" via ML on your own spend, no threshold needed — catches runaway Lambda recursion or a forgotten GPU instance day one. Run both.

### Trusted Advisor

**Q: What's the support-tier gating for Trusted Advisor, and what are its six pillars?**

A: Basic/Developer plans get only core security + service-quota checks. **Full check set, API access, and weekly reports require Business or Enterprise Support.** Pillars: cost optimization, performance, security, fault tolerance, service limits, operational excellence.

**Q: How does Trusted Advisor relate to Compute Optimizer, Cost Explorer, Config, and Security Hub?**

A: Trusted Advisor is the broad, shallow, best-practice sweep across all pillars — where you start on an unfamiliar account. Compute Optimizer does deep ML rightsizing; Cost Explorer does cost analysis/commitment recommendations; Config does continuous customizable compliance; Security Hub aggregates against formal standards — those are where you go for depth.

### AWS Support Plans

**Q: What's the minimum support tier for production, and why?**

A: **Business** — first tier with 24/7 technical support, 1-hour production-down response, and the full Trusted Advisor check set + API. A designated **TAM** only arrives at Enterprise On-Ramp/Enterprise.

### Free Tier, Pricing & Estimating Cost

**Q: Name the three kinds of Free Tier, and the classic surprise charges that aren't covered by it.**

A: **Always free** (1M Lambda requests/mo, 25GB DynamoDB), **12 months free** for new accounts (750h t2/t3.micro EC2), **trials** (GuardDuty 30 days). Surprise charges: NAT Gateway (~$32/mo before any traffic, never free-tier eligible), unattached Elastic IPs, CloudWatch Logs with Never Expire retention, orphaned EBS snapshots, cross-AZ/egress data transfer.

**Q: What's the universal AWS data-transfer pricing rule?**

A: Data transfer **in** is free, data transfer **out** is charged, and cross-AZ traffic is charged in **both** directions — why VPC endpoints and same-AZ placement are cost levers, not just latency ones.

**Q: Pricing Calculator vs Cost Explorer — what's the difference?**

A: Pricing Calculator estimates what a design **will** cost (forward-looking, for design reviews). Cost Explorer analyzes what you **did** spend (retrospective).

---

## Cost & Performance

### Cost Optimization: Savings Plans, Reserved, Spot

**Q: What's the "three-tier" cost architecture talking point for a 30% compute-bill reduction question?**

A: Layer commitments: Savings Plan for the **predictable baseline**, On-Demand for the **variable middle**, Spot for **fault-tolerant burst/batch** capacity. Also: right-sizing and eliminating idle resources (unattached EBS, idle NAT Gateways, over-provisioned RDS) is usually higher-ROI than switching pricing models — the correct *first* answer before "buy Savings Plans."

**Q: Do Compute Savings Plans apply to serverless compute?**

A: Yes — Compute Savings Plans apply to EC2, Fargate, **and Lambda**, a frequently-missed lever teams assume is EC2-only.

---

## Migration & Data Transfer

### The 7 Rs — Migration Strategies

**Q: Name the 7 Rs and identify the "usual sweet spot" strategy.**

A: Retire, Retain, Relocate, Rehost (lift-and-shift, via MGN), **Replatform** (lift-and-reshape — swap self-managed SQL Server for RDS, self-hosted queue for SQS — the usual sweet spot: real savings without a rewrite), Repurchase (buy SaaS), Refactor/Re-architect (highest cost/risk, highest payoff).

**Q: What's the honest senior sequencing answer for a large migration?**

A: "Rehost first to get out of the data center, then replatform and refactor selectively once running on AWS with real telemetry. Trying to refactor everything during a migration is how migrations slip by a year. Rehosting alone rarely saves money — the business case has to include the follow-on replatforming."

### Database Migration Service (DMS)

**Q: Homogeneous vs heterogeneous DMS migration — what tool handles schema conversion?**

A: Homogeneous (SQL Server→RDS SQL Server) is a straight data move. Heterogeneous (Oracle→PostgreSQL/Aurora) needs the **Schema Conversion Tool (SCT)** first — SCT converts the schema, DMS moves the data.

**Q: What does DMS's Change Data Capture (CDC) enable?**

A: Full load, then continuous replication of ongoing changes — keeps source and target in sync so cutover is a short window instead of a long outage. Running CDC in reverse afterward gives a rollback path.

### Snow Family

**Q: How do you decide between Snowball and network transfer (DataSync)?**

A: Work out the transfer time: 100TB over a 1Gbps link at realistic utilization is well over a week of saturated bandwidth you also need for production — a Snowball that arrives in days wins. Below ~10TB, or with a fat dedicated link, network transfer is simpler.

**Q: Snowcone vs Snowball Edge vs Snowmobile — capacity and use case?**

A: Snowcone (~8–14TB, small rugged edge collection), Snowball Edge (~80TB, TB-to-PB migrations plus local compute), Snowmobile (up to 100PB, a 45-foot container, exabyte-scale data-center evacuation).

### Storage Gateway, DataSync & Transfer Family

**Q: Distinguish Storage Gateway, DataSync, Snow Family, and Transfer Family in one sentence each.**

A: **Storage Gateway** — permanent hybrid foothold, on-prem systems keep using NFS/SMB/iSCSI/tape while data lives in AWS. **DataSync** — moving/syncing data over the network. **Snow Family** — moving data physically when the network can't. **Transfer Family** — exposing S3 over legacy SFTP/FTPS/FTP.

**Q: S3 File Gateway vs Volume Gateway vs Tape Gateway — what protocol does each present?**

A: S3 File Gateway — NFS/SMB file shares backed by S3. Volume Gateway — iSCSI block volumes backed by S3+EBS snapshots. Tape Gateway — a virtual tape library (VTL) backed by S3 Glacier, for retiring physical tape while keeping existing backup software.

---

## Well-Architected & Resilience

### AWS Well-Architected Framework — 6 Pillars

**Q: Name the 6 Well-Architected pillars and their core question.**

A: **Operational Excellence** — can you run/monitor and continually improve? **Security** — how do you protect data/systems/assets? **Reliability** — can it perform consistently and recover from failure? **Performance Efficiency** — using resources efficiently as demand changes? **Cost Optimization** — avoiding unnecessary cost? **Sustainability** — minimizing environmental impact?

**Q: Why structure an open-ended "how would you evaluate this architecture" answer around the 6 pillars?**

A: It signals architect-level thinking rather than a grab-bag of tips, and is the basis for the AWS Well-Architected Tool and formal Well-Architected Reviews.

### Well-Architected 6 Pillars — Rapid Recall Version

**Q: Recite the memory hook for the 6 pillars in order.**

A: "Run it well, keep it safe, keep it up, keep it fast, keep it cheap, keep it green" — Operational Excellence, Security, Reliability, Performance Efficiency, Cost Optimization, Sustainability.

### Disaster Recovery Strategies

**Q: Name the four DR strategies in order of cost/RTO/RPO trade-off.**

A: **Backup & Restore** (RTO/RPO hours, cheapest) → **Pilot Light** (RTO tens of minutes, RPO minutes — core infra always running minimal) → **Warm Standby** (RTO/RPO minutes or less — scaled-down full stack running continuously) → **Multi-Site Active-Active** (RTO/RPO near zero — full capacity live in 2+ regions).

```
  cheaper, slower recovery  ------------------->  costlier, faster recovery

+------------------+  +------------------+  +------------------+  +------------------+
| Backup & Restore |->|   Pilot Light    |->|   Warm Standby   |->|    Multi-Site    |
|                  |  |                  |  |                  |  |  Active-Active   |
| RPO: hours       |  | RPO: minutes     |  | RPO: secs-mins   |  | RPO: ~0          |
| RTO: hours       |  | RTO: 10s of mins |  | RTO: minutes     |  | RTO: ~0          |
+------------------+  +------------------+  +------------------+  +------------------+
 restore from        core infra off /     scaled-down but        full capacity live
 snapshots           minimal, data        RUNNING copy          in 2+ regions
                     replicating
```
*(Cost and operational complexity increase left→right; RPO/RTO improve left→right.)*

**Q: What AWS services back Warm Standby and Active-Active data replication?**

A: DynamoDB Global Tables or Aurora Global Database, combined with Route 53 latency/weighted routing across regions — requiring conflict-tolerant/idempotent write design for Active-Active.

**Q: Is Route 53 failover enough for DR by itself?**

A: No — DNS failover is TTL-bound and not instant. It's one component of Pilot Light/Warm Standby/Active-Active, not a complete DR strategy alone.

**Q: How does DR change Lambda concurrency planning?**

A: A passive DR region still has the default 1,000 concurrency limit unless pre-raised — a warm/active-active strategy requires provisioning that headroom *before* the disaster, not during it.

### Testing Resilience: Fault Injection Simulator & Resilience Hub

**Q: What faults can AWS Fault Injection Service (FIS) inject, and what's the critical safety feature?**

A: Stop/terminate EC2, throttle/fail API calls, CPU/memory/disk/network stress, network latency/packet loss, fail over RDS, kill ECS/EKS tasks/pods, simulate an entire AZ becoming unavailable. Safety feature: **stop conditions** — FIS aborts automatically if a nominated CloudWatch alarm breaches, so a test can't become the outage.

**Q: What does AWS Resilience Hub do, and what's the most common real finding it surfaces?**

A: Assesses an app against stated RTO/RPO targets, scores resilience, flags gaps (single-AZ DB behind a multi-AZ app tier, missing backups), and generates FIS experiment templates + CloudWatch alarms to validate fixes. The most common real finding: **ASG health-check type left on `EC2` instead of `ELB`**, so a hung application is never replaced.

**Q: What's the complete answer to "how do you know your DR plan actually works"?**

A: "Define RTO/RPO per workload, pick the DR strategy that meets them, then prove it — Resilience Hub to assess against targets, FIS to inject the actual failure with CloudWatch stop conditions for safety, and GameDays on a schedule, because a DR plan that hasn't been exercised in a year is a hypothesis, not a capability."

---

# PART IV — Cross-Cutting Reference

## Best Practices

**Q: What are the core Lambda/serverless best practices?**

A: One function = one responsibility, no "fat Lambda" business logic; manage infra via IaC, not console clicks; Provisioned Concurrency + .NET AOT for latency-sensitive APIs; externalize long-running work to Step Functions; minimize VPC usage unless private resource access is required (prefer VPC Endpoints once inside); create DB connections/clients outside the handler.

**Q: What are the core messaging best practices?**

A: Design for at-least-once delivery always — idempotency is not optional; one SQS queue per consumer under an SNS fan-out topic; delete messages only after successful processing; align visibility timeout with (slightly exceeding) max processing time; use FIFO + `MessageGroupId`/`MessageDeduplicationId` when strict ordering/exactly-once matters.

**Q: What are the core IAM, data, and networking best practices?**

A: **IAM** — roles over users for anything automated, OIDC over static keys, least privilege incrementally, one role per service. **Data** — DynamoDB: access patterns first then keys/indexes; RDS: keep HA (Multi-AZ) and read-scaling (replicas) separate; S3: pick storage class/lifecycle from actual access pattern, Intelligent-Tiering when unsure. **Networking** — NAT Gateway per AZ for HA, VPC Endpoints over NAT for AWS-service traffic, databases always in private/isolated subnets, SGs as primary defense.

## Common Pitfalls (Cross-Cutting)

**Q: List the top cross-cutting AWS pitfalls to have ready.**

A:

- Assuming exactly-once anywhere in AWS async messaging (Lambda/SQS/SNS are all at-least-once).
- Assuming Route 53 DNS failover is instant (it's TTL-bound).
- Conflating Multi-AZ (HA) with Read Replicas (scale) in RDS.
- Treating Fargate as "always cheaper" than EC2 (only true for bursty/low-utilization).
- Forgetting NAT Gateway must live in a public subnet, or forgetting it for VPC-bound Lambda/CodeBuild/ECS needing outbound internet.
- Publishing domain events before the DB transaction commits.
- Hardcoding credentials instead of roles/OIDC.
- Ignoring the separation between IAM trust policy and permission policy.
- Using DynamoDB Scan in a hot path, or a low-cardinality/time-based partition key.
- Believing CloudWatch alarms alone constitute "observability" without traces (X-Ray) to explain *why*.

## Sample Interview Q&A

**Q: Walk through designing a resilient order-processing pipeline on AWS for a .NET system.**

A: API Gateway/ALB → Lambda/ECS ingest writes to DynamoDB with status `PENDING` (idempotency key via conditional write) → publishes `order_created` to SNS → fans out to per-consumer SQS queues (billing, notification, inventory) → each worker processes its queue with DynamoDB conditional updates for state transitions, DLQ (`maxReceiveCount`) + CloudWatch alarms on DLQ depth/age. Everything downstream is idempotent since SQS is at-least-once. Instrument with structured logs + X-Ray tied by a correlation ID; load-test to validate DynamoDB capacity mode and Lambda concurrency sizing before go-live.

**Q: A Lambda-backed API has unacceptable p99 latency from cold starts — what's the fix order?**

A: Confirm it's actually cold starts (CloudWatch `Init Duration` in the REPORT line) → migrate to .NET 8 Native AOT → trim package size, remove VPC attachment if not needed (or add VPC Endpoints if it is) → add Provisioned Concurrency sized to p95 → re-measure. Don't jump straight to Provisioned Concurrency before ruling out cheaper architectural fixes, since it has an ongoing hourly cost.

**Q: When would you choose DynamoDB over RDS for a new .NET service, and when would you not?**

A: DynamoDB when access patterns are known upfront, need single-digit-ms latency at high/spiky scale, and the model tolerates denormalization. RDS/Aurora when the domain needs relational integrity, ad hoc queries/reporting, or existing EF Core tooling makes relational the lower-risk path. Don't force DynamoDB onto a reporting-heavy back office system just because it's "cloud-native" — that's cargo-culting, not architecture.

**Q: Why does AWS keep Trust Policy and Permission Policy separate rather than one document?**

A: They answer different questions with different owners in practice — a security team might own trust boundaries/cross-account access, while a service team owns what their own service's role can touch. Merging "can enter" with "can do" into one document would conflate two different threat models, which AWS disallows at the API level.

**Q: A DynamoDB table is throttling even though total consumed capacity looks well under the provisioned limit — why, and what's the fix?**

A: Classic hot partition — DynamoDB enforces limits per-partition, not per-table, so traffic concentrated on one key value throttles even when aggregate capacity looks fine. Adaptive Capacity smooths it automatically but isn't a fix for bad key design; the real fix is redesigning the partition key for higher cardinality (random/bucketed suffix) or a better-distributed GSI.

**Q: Explain ECS/Fargate vs Lambda to a non-technical stakeholder.**

A: "Lambda is like renting a car only when you need to drive — pay per trip, no maintenance, but a moment of 'starting the engine' if you haven't driven recently (cold start), and no trip longer than 15 minutes. ECS/Fargate is like leasing a car that's always running and ready — no startup delay, no trip-length limit, but you pay for it even when not driving. Spiky short-lived work → Lambda (cheaper, simpler). Steady always-on services → ECS/Fargate (more predictable, cost-effective)."

