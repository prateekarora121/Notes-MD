# AWS — Interview Revision Notes

> Quick-revision Q&A `K. AWS-Interview-Guide.md` se derive kiya gaya hai. Source ke har section ko cover karta hai.

## How to Use This Guide: Resume-Aligned Priority Map

### Tier 1 — Bulletproof hona chahiye (mere resume par explicitly claim kiya gaya)

**Q: Is resume ke liye kaunse AWS topics bulletproof hone chahiye, aur kyun?**

A:

- **Lambda** — named skill + delivery bullet (scheduled jobs, 12 services, 99.9% uptime)
- **DynamoDB** — named skill, "DynamoDB-backed microservices" ship kiya gaya
- **Terraform/CDKTF** — headline IaC skill, deployment-dashboard bullet mein use kiya gaya
- **GitHub Actions → AWS** — named CI/CD skill (OIDC keyless pattern hi AWS wala half hai)
- **IAM roles & policies** — unavoidable, upar wale sab ke underlying hota hai
- **S3** — named skill, saath hi Terraform state backend bhi
- **EC2** — named skill, CDKTF ke through provision kiya gaya
- **CloudWatch** — "log maintenance and health checks, 99.9% uptime" mein implied hai

### Tier 2 — Confidently reason karna chahiye, saath mein hands-on gaps ke baare mein honest rehna chahiye

**Q: Kaunse topics Tier 2 mein aate hain, aur jab hands-on time na ho to sahi framing kya hai?**

A: ECS/Fargate, RDS/Aurora, VPC design, SQS/SNS/EventBridge, ALB/ASG, CloudFront, Step Functions, Secrets Manager — yeh sab "how would you architect X" wale design questions mein surface hote hain. Framing jo kaam karti hai: *"I haven't operated Fargate in production — my container work has been Docker locally and ECS-adjacent. What I can walk you through is how I'd choose between it and EC2 for this workload, and what I'd want to validate before committing."* Honest boundary + reasoning, confident wrong answer se better hota hai.

### Tier 3 — Recognise karo, place karo, aur ek clean sentence do

**Q: Kaunse services ko sirf "recognise and place" depth chahiye?**

A: Kinesis, Amazon MQ, Athena/Redshift/Glue, GuardDuty/Inspector/Macie/Config, Organizations/Control Tower/RAM, Direct Connect/VPN, Snow Family/DMS/Storage Gateway, Local Zones/Outposts, CloudHSM. Goal: har service jo problem solve karti hai woh pata ho aur pata ho ki kab use karna hai — naam recognize na karna hi actual gap hai.

### Azure → AWS Translation (mere paas AZ-900 hai, aur Cosmos DB + Azure Blob par ship kiya hai)

**Q: Cosmos DB aur Azure Blob Storage ko unke AWS equivalents se map karo.**

A:

- **Cosmos DB → DynamoDB** — dono NoSQL hain, partition keys aur provisioned/on-demand throughput ke saath. Cosmos ke paas 5 tunable consistency levels + multi-model APIs hain; DynamoDB sirf eventual ya strong deta hai. RU/s ↔ RCU/WCU; dono low-cardinality partition keys ko hot partitions se punish karte hain.
- **Azure Blob Storage → S3** — containers ↔ buckets; blob tiers (Hot/Cool/Archive) ↔ storage classes; SAS tokens ↔ pre-signed URLs (yeh volunteer karne wali strong baat hai).

**Q: Baaki remaining Azure → AWS mappings kaunse hain jo jaanna zaroori hai?**

A:

- Azure Functions → Lambda (Consumption ↔ standard, Premium pre-warmed ↔ provisioned concurrency)
- App Service → Elastic Beanstalk / ECS Fargate
- Azure SQL Database → RDS / Aurora
- Azure AD (Entra ID) → IAM + IAM Identity Center (Entra ID ek identity *provider* hai; IAM account ke *within* authorization hai; SAML/OIDC federation se bridge hota hai)
- Azure Key Vault → KMS (keys) + Secrets Manager (secrets) — AWS Key Vault ka kaam do services mein split kar deta hai
- Azure Monitor/App Insights → CloudWatch (metrics/logs) + X-Ray (tracing)
- Azure DevOps Pipelines → CodePipeline / GitHub Actions
- ARM/Bicep → CloudFormation (Terraform dono ke against kaam karta hai)
- Resource Groups → koi direct equivalent nahi hai; AWS accounts ke through isolate karta hai, Azure subscriptions/resource groups ke through (tags + CFN stacks sabse closest analog hain)
- Azure Service Bus → SQS + SNS (ya native AMQP lift-and-shift ke liye Amazon MQ)

### Resume Deep-Dives — Woh Follow-Ups Jo Expect Karni Chahiye

**Q: "scheduled Lambda jobs, 99.9% uptime" bullet ke liye, jobs kaise schedule hote hain aur log maintenance kaise hoti hai?**

A:

- **Scheduling**: EventBridge scheduled rules (`cron(...)`/`rate(...)`) ya EventBridge Scheduler (newer, higher-scale, one-time schedules, time zones, built-in retry/DLQ). "EventBridge" bolo, "CloudWatch Events" nahi.
- **Log maintenance**: CloudWatch Logs retention policies (log groups default mein Never Expire hote hain — ek silent unbounded cost leak), metric filters (log patterns ko alarmable metrics mein badalna), subscription filters (logs ko onward stream karna), cheap long retention ke liye S3 + lifecycle to Glacier mein export.

**Q: 99.9% uptime claim ko kaise justify karoge, aur health-check ke baare mein trap question kya hai?**

A: 99.9% = ~43 min downtime/month budget. Credible answer isko measured alarms (Metric Math, raw counts nahi) se tie karta hai, noise cut karne ke liye composite alarms, on-call ke liye SNS, per-service dashboard. Trap: *"what if the health-check Lambda itself fails?"* — function ke apne `Errors`/`Throttles` **aur** missing data (`treat-missing-data: breaching`) par alarm hona chahiye, kyunki dead monitor "all healthy" jaisa hi dikhta hai.

**Q: GitHub Actions + CDKTF/Terraform deployment-dashboard bullet ke liye, pipeline authenticate kaise karta hai aur safe kaise rehta hai?**

A:

- **Auth**: GitHub OIDC ek IAM role assume karta hai — secrets mein koi static keys nahi; trust policy `sub` se repo + branch/environment tak scoped hai.
- **Safety**: PR par plan (read-only role) → merge par apply (privileged role) saved plan file se, manual prod approval ke liye GitHub environment protection rules, `tfsec`/`checkov` gates.
- **State**: S3 backend + DynamoDB lock table (concurrent pipeline runs guard karta hai).

**Q: "DynamoDB-backed microservices" bullet ke liye, kitni depth expected hai?**

A: Sabse pehle access patterns, partition-key cardinality/hot partitions, GSI vs LSI, on-demand vs provisioned capacity, Query vs Scan, idempotency ke liye conditional writes, 400 KB item limit, single-table design — yeh resume ka sabse product-tied AWS claim hai, sabse zyada revise karna.

**Q: "tell me about your AWS experience" ka 60-second answer structure kya hai?**

A: Concrete services used (DynamoDB, Lambda, GitHub Actions → CDKTF/Terraform → Lambda/DynamoDB/EC2/S3) + measurable outcome (40% less manual intervention, release cycle 5→3 days) + unprompted honest boundary (ECS/Fargate, RDS ke baare mein reasoned-about hai lekin production-operated nahi).

---

# PART I — Tier 1: Resume-Claimed Core

## Serverless & Lambda

### AWS Lambda Deep Dive

**Q: AWS Lambda kya hai aur uski key characteristics kya hain?**

A: Serverless compute — code upload karo, AWS trigger par run karta hai, per-invocation + duration billed hota hai. Event-driven (API Gateway, S3, SNS, DynamoDB Streams, SQS, EventBridge), fully managed, per concurrent request auto-scale karta hai, Node.js/Python/.NET 6/8 (AOT + arm64)/Java/Go/custom runtimes support karta hai.

**Q: Lambda execution lifecycle (Firecracker micro-VM) walk through karo.**

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

1. **INIT (cold start)** — naya micro-VM, runtime bootstrap, static initializers, DI container build, DB connections — yeh sab "outside the handler" code hai.
2. **INVOKE** — handler event + context ke saath run hota hai, timeout ke andar finish hona chahiye (max 15 min).
3. **FREEZE (warm reuse)** — response ke baad environment freeze ho jaata hai; globals/connections/`/tmp` persist karte hain, jisse 1–10ms warm invocations milte hain.
4. **SHUTDOWN** — AWS idle/outdated environments reclaim karta hai; koi shutdown hook nahi hota, state lost ho jaata hai roll back nahi hota.

**Q: Runtimes ke across cold start vs warm start latency compare karo, aur cold starts mitigate kaise karte ho?**

A: Cold: Java/.NET JIT 300–1500ms, Node/Python 50–200ms, .NET Native AOT 50–100ms. Warm: 1–10ms. Mitigations: .NET AOT, small deployment package, heavy DI graphs avoid karo, VPC avoid karo jab tak required na ho, latency-sensitive APIs ke liye Provisioned Concurrency.

**Q: Teen trigger categories kya hain aur unki failure semantics kya hain?**

A:

- **Synchronous** (API Gateway, ALB, Step Functions, direct Invoke) — caller ko error dikhta hai aur usko retry karna padta hai.
- **Asynchronous** (S3, SNS, CloudWatch Events, SES, EventBridge) — Lambda automatically retry karta hai; exhaustion par DLQ.
- **Poll-based / event source mapping** (DynamoDB Streams, Kinesis, MSK, SQS) — Lambda internally poll karta hai, success tak ya maxReceiveCount/DLQ tak retry karta hai.

**Q: Lambda ke cons kya hain / kab use nahi karna chahiye?**

A: Cold-start latency tight tail-latency SLAs ke liye unsuitable hai; 15-min hard timeout; AWS event model par vendor lock-in; max 10GB memory / 10GB `/tmp`; harder cross-function observability (X-Ray + structured logging chahiye).

**Q: Lambda ke liye kaunse .NET-specific considerations matter karte hain?**

A:

- Latency-sensitive functions ke liye **.NET 8 Native AOT** prefer karo — no JIT warm-up, smaller package; trade-off: no reflection-based DI magic (`System.Text.Json` source generators chahiye), kuch reflection-heavy libraries trimming ke under break ho jaati hain.
- `Amazon.Lambda.AspNetCoreServer` API Gateway/ALB ke peeche full ASP.NET Core app host karta hai — easy lift-and-shift, heavier cold start.
- Cold start pe pura `IServiceProvider` graph build karne se bacho — static field ke roop mein cache karo.
- `DbContext`/connection pool **handler ke bahar** banao (static/singleton); high concurrency par RDS Proxy/connection-pool exhaustion pe dhyan rakho.

**Q: Lambda versions, aliases, aur canary deploys explain karo.**

A: `$LATEST` mutable hota hai; version publish karne se ek immutable numbered snapshot banta hai. Alias ek named movable pointer hota hai (`prod` → v7) jo do versions ke across weighted routing support karta hai — canary/linear deploys ke peeche yeh hi mechanism hai (10% traffic shift karo, alarms dekho, complete karo ya roll back karo). CodeDeploy isko automate karta hai (`Canary10Percent5Minutes`) alarm-triggered auto-rollback ke saath.

**Q: Lambda Layers aur Destinations kya hain?**

A: **Layers** shared dependencies/Extensions ko function code se separately package karti hain (max 5 layers, 250MB unzipped). **Destinations** async invocation ke *result* (`onSuccess`/`onFailure`) ko SQS/SNS/EventBridge/Lambda tak route karte hain — bare DLQ se better hain kyunki inmein response/error payload aur request context bhi include hota hai, sirf original event nahi.

**Q: Async Lambda invokes ka default retry behavior kya hai, aur Reserved/Provisioned concurrency aur SnapStart mein kya difference hai?**

A: Async invokes default mein **2** baar retry hote hain (3 total attempts) ek event age limit ke saath. Reserved concurrency function ki capacity share ko cap+guarantee karta hai; Provisioned concurrency cold starts remove karne ke liye environments pre-initialize karta hai; SnapStart sirf Java ke liye ek cold-start fix hai — .NET ke liye Native AOT + trimming + provisioned concurrency use karo.

### Lambda Concurrency Model

**Q: Reserved vs Provisioned concurrency — one-liner kya hai?**

A: **Reserved = capacity guarantee/cap** (extra cost nahi, limit se upar throttle karta hai). **Provisioned = cold starts eliminate karna** (N warm environments pre-init karta hai, use ho ya na ho hourly billed hota hai, provisioned amount se upar cold scaling par fall back karta hai).

**Q: Lambda ke burst-scaling rules kya hain?**

A: Default regional concurrency limit 1,000 concurrent executions hai (increase kiya ja sakta hai). Pehle ~1,000 instantly scale hote hain; uske aage, +500 new environments/minute jab tak limit hit nahi hoti. Concurrency limit = kitna scale kar sakte ho; burst rate = kitna fast.

**Q: Lambda concurrency size kaise karte ho, aur multi-region concurrency kaise kaam karta hai?**

A:

```
Required Concurrency ≈ Peak RPS × Avg Duration (seconds) × Safety Factor (1.3–2.0)
```

Example: 500 msgs/sec × 1.2s duration ≈ 600 concurrency (safety factor se pehle). Har region ka apna independent concurrency pool/burst behavior hota hai. Active-active: reserved/provisioned ko har region mein separately provision karo. Active-passive DR: failover se *pehle* DR region ki concurrency limit pre-raise karo, warna cold default-limit region failover load ke under throttle ho jaayega.

**Q: Rapid-fire — Lambda 2nd time faster kyun chala, aur kya yeh exactly-once guarantee de sakta hai?**

A: 2nd time faster = warm start (environment reuse, "cached logic" nahi). Nahi — Lambda sirf at-least-once hai; idempotency mandatory hai. Aur bhi: timeout wall-clock hota hai (sirf CPU nahi, network waits bhi include hain); SQS messages reprocess hote hain kyunki woh success ke baad hi delete hote hain; DB writes transactional/rolled back nahi hote crash par; VPC placement ENI attachment ki wajah se cold-start latency add karta hai; business logic Lambda mein nahi rehna chahiye ("fat Lambda" anti-pattern); 15 min se aage koi run nahi (Step Functions/ECS/Batch use karo).

**Q: ENI vs VPC Endpoint — kab kaunsa chahiye?**

A: **ENI** woh network interface hai jo Lambda VPC ke andar attach karta hai private resources (RDS, internal ALB) reach karne ke liye — cold-start overhead add karta hai. **VPC Endpoint** (S3/DynamoDB ke liye Gateway, baaki sab ke liye Interface/PrivateLink) VPC-bound Lambda ko NAT/internet ke bina AWS services tak pahunchne deta hai — lower latency/cost, no public exposure. Rule: ENI sirf jab private VPC resources reach karna zaroori ho; ek baar VPC mein aane ke baad NAT avoid karne ke liye Endpoints use karo.

### Lambda vs ECS vs Fargate

**Q: Lambda, ECS on the EC2 launch type, aur Fargate mein kaise choose karte ho?**

A: Workload-shape-driven, aur asli axis hai **capacity kiski hai**: event-driven/spiky/short-lived → **Lambda**; container-based *aur* host chahiye (GPU, custom AMI/kernel, daemons, Spot/RI tuning) → **ECS on the EC2 launch type**; container-based aur host ki koi requirement nahi → **ECS/EKS on Fargate**. Note karo ki ECS orchestrator hai aur Fargate uske liye capacity provider — rival product nahi.

```
                  Workload ki shape kya hai?
                              |
        +---------------------+---------------------+
        |                                           |
 event-driven, spiky,                    long-running, steady,
 short-lived (< 15 min)                  container-based
        |                                           |
        v                                           v
     LAMBDA                        Host par control chahiye kya?
                                   (GPU, custom kernel/AMI,
                                    daemons, Spot/RI tuning)
                                                |
                                    +-----------+-----------+
                                   haan                     nahi
                                    |                       |
                                    v                       v
                          ECS on EC2 launch type    ECS / EKS on FARGATE
                          (capacity meri)           (capacity AWS ki)
```

**Q: Teeno ke across cost, scaling speed, aur IAM model compare karo.**

A:

- **Cost inversion**: low/spiky traffic par Lambda sabse sasta; Fargate beech mein (running rehne tak per task vCPU/GB-second); steady high density par EC2 launch type sabse sasta — task count chahe kuch bhi ho, aap per instance-hour pay karte ho, aur Spot/RI/Savings Plans apply hote hain.
- **Scaling speed**: Lambda seconds mein; Fargate per task (~30–60s image pull + ENI attach ke liye); EC2 launch type sabse slow jab cluster khud badhana pade (instance boot, storage attach, cluster mein register).
- **IAM**: Lambda ek execution role use karta hai; ECS tasks ek **task role** (app permissions) plus ek alag **task execution role** (images pull karna, logs likhna) use karte hain — dono ko confuse karna classic trap hai.
- **Fargate ki limits hi EC2 launch type par rukne ki wajah hain**: na privileged containers, na daemonsets, na GPU.

### Serverless & the S3 → Lambda Trigger Pattern

**Q: "serverless" ka matlab kya hai, aur kaunse AWS services isme count hote hain?**

A: Provision/patch karne ke liye koi server nahi, zero se auto-scale, sirf use ka pay, built-in HA. Set: Lambda, Fargate, S3, DynamoDB, SQS/SNS/EventBridge, API Gateway, Step Functions, Aurora Serverless v2.

**Q: Canonical S3 → Lambda trigger flow aur uska permission model walk through karo.**

A:

1. `s3:ObjectCreated:*` par S3 event notification configure karo, optionally prefix/suffix se filtered.
2. S3 Lambda ko event ke saath invoke karta hai jisme **bucket + key only** hota hai (object nahi) — function `GetObject` call karta hai.
3. Function ko ek **resource-based policy** chahiye jo `s3.amazonaws.com` ko invoke karne allow kare, aur uske execution role ko `s3:GetObject`/`s3:PutObject` chahiye. Confused-deputy guard ke liye `SourceAccount`/`SourceArn` conditions use karo.

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

**Q: S3-triggered Lambdas ke woh do gotchas kya hain jo guaranteed-to-be-asked hain?**

A:

- **Infinite recursion** — output ko same bucket mein us path ke under wapas likhna jise trigger abhi bhi match karta hai, unbounded invoke loop cause karta hai; fix ek alag destination bucket ya non-overlapping prefixes se karo (AWS ke paas ab recursive-invocation detection bhi hai, lekin architecture fix aapka hi hai).
- **At-least-once delivery** — S3 notifications ek se zyada baar/out of order deliver ho sakte hain, isliye handlers idempotent hone chahiye (object key + ETag/version ID par key karo).

**Q: S3 events ko native notifications ke jagah EventBridge ke through kab route karoge?**

A: Jab content-based filtering chahiye, per event multiple targets chahiye, DLQ ke saath retries chahiye, ya archive/replay chahiye — native S3 notifications sirf per event type ek destination support karti hain.

---

## DynamoDB

### DynamoDB Deep Dive

**Q: DynamoDB kya hai aur partitioning kaise kaam karti hai?**

A: Fully managed NoSQL key-value/document store, agar key design correct ho to scale par single-digit-ms latency deta hai. Partition Key hash physical partition decide karta hai (~3,000 RCU/1,000 WCU per partition, directional hai contractual nahi). Good PK = high cardinality + even access distribution. Bad PK (low cardinality, time-based, "celebrity" key) → hot partition → throttling, tab bhi jab table-level metrics fine dikh rahe ho. **Adaptive Capacity** unevenness ko automatically smooth kar deti hai lekin bad key design ko fix nahi karti.

**Q: Sort Key ordering ke alawa kya unlock karta hai?**

A: Range queries, time-series lookups, one-to-many relationships, hierarchical/single-table aggregates (`PK=ORDER#555`, `SK=META#/ITEM#1/EVENT#CREATED` ek hi Query mein fetch), aur rank-encoded SK ke through sorted views.

**Q: GSI vs LSI — full comparison.**

A:

| | GSI | LSI |
|---|---|---|
| Partition key | Base table se different | Base table jaisa hi |
| Sort key | Apna, optional | Base table se different |
| Created | Kisi bhi time | Sirf table creation par |
| Consistency | Sirf Eventual | Strongly consistent ho sakta hai |
| Capacity | Apna throughput | Base table capacity share karta hai |

Cost note: har base write ek GSI mein bhi write kar sakta hai (write amplification) — sirf needed attributes project karo.

**Q: On-Demand vs Provisioned capacity — kab kaunsa choose karte ho?**

A: On-Demand: capacity planning nahi chahiye, pay-per-request, spiky/unknown traffic ke liye best. Provisioned (+Auto Scaling): RCU/WCU sizing chahiye, steady predictable volume par cheaper, Reserved Capacity discounts support karta hai.

**Q: Query almost always Scan se preferred kyun hota hai?**

A: Query PK-targeted hai (O(matched items)); Scan poore table/index ko read karta hai phir filter karta hai — expensive aur slow, sirf rare admin/analytics jobs ke liye acceptable hai.

**Q: Conditional writes full transactions ke bina concurrency control kaise dete hain?**

A: `PutItem` ke saath `ConditionExpression: attribute_not_exists(idempotencyKey)` idempotency deta hai; `UpdateItem` ke saath status-equality condition optimistic locking/state-machine transitions deta hai — dono server-side atomic checks hain, koi distributed lock nahi, aur koi 2× transaction cost nahi.

**Q: `TransactWriteItems`/`TransactGetItems` kab use karte ho, aur uska cost kya hai?**

A: Genuine all-or-nothing invariants ke liye (inventory decrement + order creation, money transfer), 100 items/tables tak, 4 MB aggregate (Sept 2022 mein 25 se raise kiya gaya — purana material abhi bhi 25 bolta hai) ACID deta hai. Cost ~2× capacity plus added latency hai — default choice nahi hai.

**Q: TTL kaise behave karta hai, aur kis ke liye kabhi use nahi karna chahiye?**

A: Attribute-driven, best-effort async deletion — hours tak lag ho sakta hai (kuch cases mein ~48h documented). Idempotency keys/sessions/dedup/temp workflow state ke liye good hai. Time-sensitive compliance deletion deadlines ke liye kabhi rely na karo.

**Q: DynamoDB Streams kis liye use hote hain?**

A: Time-ordered change log (insert/update/delete), ~24h retained. CQRS read-model projections, event-driven pipelines (Streams → Lambda → SNS/SQS/EventBridge), aur CDC/audit trails/search-index sync ka backbone.

**Q: Global Tables multi-region writes aur conflicts kaise handle karte hain?**

A: Multi-region, multi-active replication; users nearest region par read/write karte hain; conflict resolution last-writer-wins hai; replication asynchronous/eventually consistent hai — writes ko idempotent/conflict-tolerant design karo.

**Q: Single-table design kya hai, aur 400 KB item limit ka workaround kya hai?**

A: Multiple entity types (User, Order, Item, Event) PK/SK convention ke through ek table share karte hain, modeling effort ko fewer round-trips aur no joins ke liye trade karte hain. 400 KB (hard cap, attribute names+values include hote hain) se bade items ke liye: blob ko S3 mein store karo, DynamoDB mein pointer rakho, ya same PK ke under multiple items mein vertically partition karo.

#### Capacity Maths & Hot-Partition Mitigation

**Q: RCU aur WCU precisely define karo, aur ek example work karo.**

A: 1 RCU = one strongly consistent read of up to 4KB/s, ya do eventually consistent reads of 4KB/s (eventual reads half cost hote hain). 1 WCU = one write up to 1KB/s; transactional reads/writes 2× cost karte hain. Example: 100 reads/sec of 10KB items, eventually consistent → 10KB rounds to 3×4KB units ÷2 (eventual) = 1.5→2 RCU/read × 100 = ~200 RCU.

**Q: Low-cardinality PK (jaise `STATUS#PENDING` ya ek date) se cause hui hot partition ko kaise fix karte ho?**

A: Write sharding — ek calculated suffix append karo, e.g. `PK = ORDER#2026-08-10#3` (shard = hash(orderId) % 10), writes ko N partitions ke across spread karta hai. Trade-off: reads ko ab saare N shards query karke merge karna padta hai, isliye sirf wahi shard karo jahan hot spot real ho. Adaptive Capacity help karti hai lekin good key design ka substitute nahi hai.

```
PK = ORDER#2026-08-10#3        // shard = hash(orderId) % 10
```

**Q: GSI overloading aur sparse indexes kya hain?**

A: **Overloading** — ek GSI generic keys (`GSI1PK`/`GSI1SK`) ke saath multiple access patterns serve karta hai kyunki different entity types unhe differently populate karte hain. **Sparse index** — ek item GSI mein sirf tab appear hota hai jab uske paas index ka key attribute ho, isliye `GSI1PK` sirf unprocessed orders par likhna ek tiny, cheap-to-scan "work queue" index deta hai.

#### DynamoDB in .NET — Woh Code Jo Aapse Likhne Ko Kaha Jaayega

**Q: DynamoDB .NET SDK ke teen layers kya hain, aur har ek kab use karte ho?**

A: **Low-level** (`AmazonDynamoDBClient` + `AttributeValue` dicts) — full control, conditions, transactions, single-table design. **Document model** (`Table`+`Document`) — POCOs ke bina schema-flexible. **Object persistence** (`DynamoDBContext`+`[DynamoDBTable]`) — simple one-entity-per-table CRUD; single-table design ke liye poorly fit hota hai.

**Q: Lambda mein `AmazonDynamoDBClient` singleton kyun hona chahiye?**

A: Yeh thread-safe hai; per request ek create karna ek real performance bug hai. Isko handler ke bahar banao taaki warm invocations mein survive kare aur connections reuse kare.

**Q: Conditional `PutItem` use karke idempotency pattern dikhao.**

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

**Q: GSI ko key condition ke saath query karna dikhao, aur hot path mein `Scan` kyun avoid karna chahiye?**

A: `Query` GSI ke against key condition ke saath sirf matching partitions target karta hai; `Scan` poora table/index read karta hai aur baad mein filter karta hai, jo expensive aur slow hota hai.

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

**Q: Ceiling ke saath atomic counter kaise implement karte ho, aur object-persistence model ke saath optimistic locking kaise kaam karti hai?**

A: `UpdateItem` server-side mutate karta hai, isliye koi read-modify-write race nahi hoti:

```csharp
UpdateExpression    = "SET #v = #v + :inc",
ConditionExpression = "#v < :max"          // atomic increment with a ceiling
```

Object-persistence model ke saath, `[DynamoDBVersion]` automatically optimistic locking deta hai — SDK version condition add karta hai aur conflict par throw karta hai.

**Q: Pagination loop mein `LastEvaluatedKey?.Count > 0` check karna zaroori kyun hai, sirf `!= null` nahi?**

A: `Query`/`Scan` per call 1MB tak cap hoti hai; `LastEvaluatedKey` finish hone par ek **empty dictionary** (null nahi) ke roop mein aata hai, isliye sirf `!= null` check karna forever loop kar deta hai. SDK v3 ka `Paginators.QueryAsync` isko automatically handle karta hai.

```csharp
Dictionary<string, AttributeValue>? start = null;
do {
    var page = await client.QueryAsync(new QueryRequest { /* … */ ExclusiveStartKey = start });
    Process(page.Items);
    start = page.LastEvaluatedKey?.Count > 0 ? page.LastEvaluatedKey : null;
} while (start != null);
```

**Q: `BatchWriteItem` aur `TransactWriteItems` mein kya difference hai?**

A: `BatchWriteItem` (≤25 items) ek throughput optimization hai jo **partially succeed** ho sakti hai — `UnprocessedItems` ko backoff ke saath resubmit karna padta hai; koi atomicity/conditions nahi. `TransactWriteItems` all-or-nothing ACID hai, ~2× capacity cost par:

```csharp
await client.TransactWriteItemsAsync(new TransactWriteItemsRequest {
    TransactItems = new() {
        new() { Put    = new Put    { TableName = "Orders",    /* … */ } },
        new() { Update = new Update { TableName = "Inventory", /* decrement stock */ } }
    }
});
```

**Q: DynamoDB ke key .NET gotchas list karo.**

A: Money ke liye `decimal` use karo `double` nahi (Number type arbitrary-precision hai); `DynamoDBContext` type metadata cache karta hai isliye isko long-lived rakho; SDK already throttling ko backoff ke saath retry karta hai (apna khud stack mat karo — `MaxErrorRetry` tune karo); tuning karte waqt `ReturnConsumedCapacity` use karo; `Scan` ko admin/backfill ke liye reserve karo, ideally sparse GSI par.

**Q: Streams-triggered Lambda change events handle karte hue dikhao, aur idempotent kyun hona chahiye?**

A: DynamoDB Streams read models aur audit logs ke peeche wala CDC pattern hai; same record ek se zyada baar deliver ho sakta hai, isliye handler ko re-delivery tolerate karna chahiye:

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

**Q: Rapid-fire — kya DynamoDB relational hai, kya GSI strongly consistent ho sakta hai, aur kya LSIs table creation ke baad add ho sakte hain?**

A: Relational nahi hai — NoSQL, koi joins/FKs nahi. GSIs **kabhi** strongly consistent nahi ho sakte. LSIs creation ke baad **add nahi** ho sakte (table creation par hi define karna padta hai); GSIs kabhi bhi add ho sakte hain.

**Q: Rapid-fire — kya Scan Query se faster hai, kya ek PK duplicate ho sakta hai, kya DynamoDB default mein strongly consistent hai, kya TTL instantly delete karta hai, kya transactions zyada cost karte hain?**

A: Scan faster nahi hai — yeh poora table read karta hai. Ek PK repeat *ho sakta hai* agar SKs different hain (yeh composite keys hai). Default read consistency **eventual** hai, strong nahi. TTL deletion best-effort hai aur hours tak le sakta hai. Transactions roughly 2× normal capacity cost karte hain.

**Q: DynamoDB ka senior-level one-sentence summary kya hai?**

A: "DynamoDB query flexibility ko massive, predictable scalability ke liye trade karta hai. Efficient usage correct partition key design, denormalized access-pattern-first modeling, aur scans, hot partitions, unnecessary indexes se bachne par depend karta hai."

---

## IAM & Security

### IAM Overview, Root Account & Shared Responsibility

**Q: IAM actually kya govern karta hai, aur har AWS API call kaunse do checks se guzarta hai?**

A: IAM decide karta hai ki *kaun* *kya* kar sakta hai *kaunse resource* par — yeh data store nahi karta ya workloads run nahi karta. Har call (1) **Authentication** se pass hota hai — kya credentials valid hain (password/access key/STS token)? aur (2) **Authorization** se — kya ek policy iss resource par yeh action permit karti hai? Auth fail hone par `InvalidClientTokenId` milta hai; authz fail hone par `AccessDenied` milta hai — dono ko distinguish karna IAM debugging ka step one hai.

**Q: IAM ke baare mein kaunse warm-up facts interviewers expect karte hain?**

A: IAM **global** hai (koi region selection nahi); yeh **free** hai; yeh **eventually consistent** hai (naya role first use pe fail ho sakta hai, retry pe succeed hota hai); **root account** sign-up par create hota hai, sign-up email se identified hota hai, aur uske paas woh power hai jise koi policy restrict nahi kar sakti.

**Q: Sirf root account kya kar sakta hai jo `AdministratorAccess` nahi kar sakta?**

A: Account close karna; account name/root email/root password change karna; Support plan change ya cancel karna; user ka revoked billing-management permission restore karna; Reserved Instance Marketplace seller ke roop mein register karna; S3 MFA Delete enable karna ya all-principal-denying bucket policy remove karna; kuch tax invoices dekhna.

**Q: Chaar root-account rules kya hain, aur IAM ka shared-responsibility split kya hai?**

A: MFA immediately enable karo, daily use kabhi mat karo, kabhi share mat karo, iske liye access keys kabhi mat banao. AWS IAM ko global service ke roop mein secure karta hai aur tooling deta hai (MFA, Access Analyzer, credential reports, CloudTrail); aap identities organize karne, least privilege apply karne, credentials rotate karne, aur us tooling ko actually review karne ke responsible ho.

### Users, Groups & Permissions

**Q: IAM Groups ke baare mein hard rules kya hain?**

A: Ek group sirf users hold karta hai (koi nested groups nahi); ek user multiple groups mein ho sakta hai aur effective permissions **union** hote hain; ek user zero groups mein ho sakta hai; group ek **identity nahi** hai — yeh login nahi kar sakta aur policy `Principal` ke roop mein appear nahi ho sakta.

```
Account
├── Group: Developers  → [Parteek, Ravi]
├── Group: Operations  → [Ravi, Sara]     ← Ravi is in two groups: permissions add up
└── Group: Audit       → [Sara]
```

**Q: Bilkul naya IAM user jiske paas koi attached policy nahi hai, kya kar sakta hai?**

A: Kuch nahi — S3 buckets list bhi nahi kar sakta. IAM deny-by-default hai; har permission explicitly grant karni padti hai.

**Q: Identity-based vs resource-based policy — structural difference kya hai?**

A: Identity-based (User/Group/Role ke saath attached) mein koi `Principal` nahi hota — jo identity usko hold kar rahi hai wahi principal hai. Resource-based (S3 bucket policy, SQS/KMS/Lambda resource policy) mein `Principal` naam **karna zaroori** hai.

**Q: Cross-account access ke do tareeke kya hain, aur woh kaise different hain?**

A: **Resource-based policy** — caller apni identity rakhta hai, resource ko directly call karta hai; resource policy ko unhe name karna padta hai AND unka apna account bhi call allow karna padta hai (dono sides). **AssumeRole** — caller us session ke liye role *ban* jaata hai, apni original permissions chhod deta hai; sirf role ki permissions apply hoti hain.

### Hands-On: Users & Groups

**Q: S3 read access chahiye wale user ko banate waqt correct habit kya hai, aur credentials CSV immediately download kyun karni chahiye?**

A: Permissions ko ek **group** par attach karo (e.g. managed policy ke saath ek `Developers` group banao), phir user ko group mein add karo — kabhi individual users par directly policies attach mat karo. Console password/secret **ek baar** dikhata hai; agar kho jaaye, credential delete karke reissue karo "look it up" karne ke jagah.

```bash
aws iam create-group  --group-name Developers
aws iam attach-group-policy --group-name Developers \
  --policy-arn arn:aws:iam::aws:policy/ReadOnlyAccess
aws iam create-user   --user-name parteek
aws iam add-user-to-group --user-name parteek --group-name Developers
aws iam list-groups-for-user --user-name parteek     # verify
```

**Q: URL/screenshot se hi IAM user sign-in ko root sign-in se kaise distinguish karte ho?**

A: IAM users account-specific URL/alias (`https://my-company.signin.aws.amazon.com/console`) ke through sign in karte hain; root generic sign-in page par **email address** se sign in karta hai.

### Policy Types, Structure & Password Policy

**Q: AWS managed vs customer managed vs inline policy — real work ke liye kaunsa recommend karte ho?**

A: **Customer managed** — versioned (5 versions, rollback-capable), identities ke across reusable, audits mein visible. AWS managed policies convenient hoti hain lekin needed se broader hoti hain. Inline policies audits ke liye invisible hoti hain, non-reusable hoti hain, aur identity ke saath die karti hain — inhe avoid karo.

**Q: Policy document ki anatomy field by field dikhao.**

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

`Version` policy **language** version hai (hamesha `2012-10-17`, aapke document ka apna version nahi). `Principal` sirf resource-based/trust policies mein use hota hai, kabhi identity-based mein nahi. `Resource` kabhi kabhi `"*"` hona zaroori hota hai kyunki kuch APIs (bahut se `List*`/`Describe*`) resource-level permissions support nahi karte.

**Q: `bucket` vs `bucket/*` ARN gotcha, aur general ARN format explain karo.**

A: `arn:aws:s3:::my-bucket` bucket khud hai — `s3:ListBucket` ke liye required hai. `arn:aws:s3:::my-bucket/*` uske andar wale objects hain — `s3:GetObject` ke liye required hai. Ek miss karne par "download works but `aws s3 ls` returns AccessDenied" hota hai ya vice versa.

```
arn:partition:service:region:account-id:resource
arn:aws:iam::123456789012:user/parteek              ← IAM is global, so region is empty
arn:aws:s3:::my-bucket/file.txt                     ← S3 names are global, so region+account empty
arn:aws:dynamodb:us-east-1:123456789012:table/Orders
```

**Q: Org-level guardrails hone ke baad full policy evaluation order kya hai?**

A: Kahin bhi Explicit Deny → outright win karta hai. Otherwise: SCP allow karta hai? → Permissions boundary allow karta hai? → Session policy allow karta hai? → Identity ya resource policy allow karta hai? → warna implicit Deny. **SCPs aur boundaries sirf permissions le sakte hain, kabhi grant nahi kar sakte.**

```
Request
 ├─ Any explicit Deny anywhere?          → DENY (nothing can override this)
 ├─ SCP (Organizations) permits it?      → no → DENY
 ├─ Permissions boundary permits it?     → no → DENY
 ├─ Session policy permits it?           → no → DENY
 ├─ Identity policy OR resource policy allows it?  → neither → DENY (implicit)
 └─ else                                 → ALLOW
```

**Q: RBAC vs ABAC — kab kaunsa use karte ho?**

A: RBAC = job function ke hisaab se permissions, policy count org ke saath badhti hai. ABAC = tags-driven permissions (`aws:PrincipalTag/Team` ko `aws:ResourceTag/Team` se match karna chahiye) — ek policy kisi bhi number of teams tak scale ho jaati hai bina edits ke; kisi bhi number of microservices ke across IAM scale karne ka standard answer hai.

**Q: Password Policy kya enforce karne deti hai, aur uske baare mein honest senior take kya hai?**

A: Minimum length (128 chars tak), required character types, self-change allowed/blocked, expiration + reuse prevention. Yeh sabse cheap brute-force defense hai, lekin real controls MFA aur IAM Identity Center ke through long-lived human credentials khatam karna hain.

#### IAM Policy Structure — Full Explanation

**Q: Kisi bhi IAM policy statement ko ek sentence ke roop mein kaise padhte ho?**

A: **Effect** (allow/deny) — **Action** (yeh API calls) — **Resource** (in things par) — **Condition** (lekin sirf jab yeh true ho).

**Q: Admin starting point se ek least-privilege S3 policy step by step build karo.**

A:

```json
// Step 1 (admin, never ship): {"Effect":"Allow","Action":"*","Resource":"*"}
// Step 2 (scope service):    {"Effect":"Allow","Action":"s3:*","Resource":"*"}
// Step 3 (scope resource):   {"Effect":"Allow","Action":"s3:*","Resource":"arn:aws:s3:::my-bucket/*"}
// Step 4 (scope actions+prefix — least privilege):
{ "Effect": "Allow", "Action": ["s3:GetObject","s3:PutObject"], "Resource": "arn:aws:s3:::my-bucket/uploads/*" }
```

**Q: Broad-allow + narrow-deny guardrail pattern use karke ek normal multi-statement policy dikhao.**

A: Multiple statements independently evaluate hote hain aur combine hote hain — koi ordering nahi, koi fall-through nahi; koi bhi `Deny` win karta hai, warna koi bhi `Allow` grant karta hai:

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

**Q: `Allow` + `NotAction` dangerous kyun hai, lekin `Deny` + `NotAction` useful kyun hai?**

A: `{"Effect":"Allow","NotAction":"iam:*","Resource":"*"}` **har action ko IAM ke siwa** grant karta hai — near-admin disguised as restrictive; review mein isko ek finding treat karo. `Deny` ke saath, yeh standard region-lock pattern hai: sab kuch deny karo **except** global services, approved regions ke bahar:

```json
{ "Effect": "Deny",
  "NotAction": ["iam:*", "sts:*", "route53:*", "cloudfront:*", "support:*"],
  "Resource": "*",
  "Condition": { "StringNotEquals": { "aws:RequestedRegion": ["us-east-1", "ap-south-1"] } } }
```

**Q: `ForAllValues:` condition operators ke saath trap kya hai?**

A: `ForAllValues:` pass hota hai agar multi-valued request key ke har value match kare — lekin yeh **true bhi return karta hai jab key bilkul absent ho**, isliye sirf ek `Allow` mein yeh intended se zyada grant kar sakta hai. Load-bearing hone par isko `Null` check ke saath pair karo, e.g. tenant isolation:

```json
"Condition": { "ForAllValues:StringEquals": { "dynamodb:LeadingKeys": ["${aws:PrincipalTag/TenantId}"] } }
```

**Q: Policy variables kya hain, aur jaanne wale size/count limits kya hain?**

A: Evaluation time par substitute hote hain, e.g. `${aws:username}` ek resource ARN mein har user ko ek shared policy se apna S3 folder deta hai:

```json
{ "Effect": "Allow", "Action": "s3:*",
  "Resource": "arn:aws:s3:::company-bucket/home/${aws:username}/*" }
```

Limits: managed policy 6,144 chars par capped hai; inline budgets 2,048/5,120/10,240 chars (user/group/role); default mein per identity 10 managed policies.

**Q: Scratch se ek tight policy likhne ka recommended process kya hai?**

A: Exact API calls list karo (code se ya broad sandbox permissions wali CloudTrail se) → exact ARNs likho (sirf wahan `"*"` jahan API mein resource-level permissions nahi hain) → conditions add karo (region/MFA/IP/tag) → Policy Simulator mein test karo → Access Advisor ya Access Analyzer ke generate-from-CloudTrail-history feature se refine karo.

### MFA (Multi-Factor Authentication)

**Q: Kaunse MFA device types exist karte hain, aur CLI/API coverage ke baare mein key nuance kya hai?**

A: Virtual MFA (TOTP app), FIDO/U2F key (phishing-resistant, TOTP ke unlike), hardware TOTP token, passkeys/biometrics (FIDO2). MFA natively sirf **console sign-in** protect karta hai; CLI/API ke liye ek policy condition (`aws:MultiFactorAuthPresent`) plus `sts:GetSessionToken` ya `AssumeRole --serial-number/--token-code` ke through ek MFA-backed session chahiye.

**Q: Ek user kitne MFA devices register kar sakta hai, aur standard cross-account production control kya hai?**

A: Per user 8 devices tak (backup register karo). Trust policy mein `sts:AssumeRole` par MFA require karna cross-account production access ka standard control hai.

### Access to AWS: Console, CLI, SDK & Access Keys

**Q: Access-key rotation discipline kya hai, aur 2-key limit deliberate kyun hai?**

A: Per user max 2 keys, specifically zero-downtime rotation allow karne ke liye: key #2 create karo → roll out karo → verify karo traffic use kar raha hai → **delete** karo (sirf deactivate nahi) key #1. Kabhi keys ko Git mein commit mat karo, AMIs/images mein bake mat karo, ya use mat karo jab role/OIDC available ho.

**Q: AWS mein teen front doors kya hain, aur single most useful IAM debug command kya hai?**

A: Console (username+password+MFA, humans ke liye), CLI (access key ya temporary role credentials, scripting ke liye), SDK (same, normally ek role se supplied, application code ke liye) — sab ultimately same SigV4-signed REST API call karte hain.

```bash
aws configure                    # writes ~/.aws/credentials + ~/.aws/config
aws configure --profile dev      # named profile
aws sts get-caller-identity      # "who am I?" — the single most useful IAM debug command
aws iam list-access-keys --user-name parteek
aws s3 ls --profile dev
```

**Q: Default credential provider chain order kya hai, aur woh ek classic bug kyun cause karta hai?**

A: (1) explicit CLI/SDK params, (2) environment variables, (3) shared credentials/config file, (4) container credentials (ECS task role), (5) EC2 instance profile via IMDS — **last**. Ek stale `AWS_ACCESS_KEY_ID` env var silently EC2/ECS role ko shadow kar deta hai, jinke paas role clearly permissions rakhta hai unke liye `AccessDenied` cause karta hai. `aws sts get-caller-identity` isko reveal karta hai (`user/...` dikhata hai `assumed-role/...` ke jagah).

### Hands-On: MFA & Access Keys

**Q: Hands-on ek naya access key kaise create aur verify karte ho?**

A: Security credentials → *Create access key* → CLI use case choose karo → warning acknowledge karo → `.csv` download karo.

```bash
aws configure
# AWS Access Key ID:     AKIA...
# AWS Secret Access Key: ****
# Default region name:   us-east-1
# Default output format: json
aws sts get-caller-identity      # confirms which identity the key belongs to
```

**Q: Access-key rotation drill command by command kya hai?**

A:

```bash
aws iam create-access-key --user-name parteek          # key #2
# deploy, verify traffic uses key #2
aws iam update-access-key --user-name parteek --access-key-id AKIA_OLD --status Inactive
# soak — revert to Active if anything breaks
aws iam delete-access-key --user-name parteek --access-key-id AKIA_OLD
```

### IAM Roles, Policies, AssumeRole

**Q: IAM role vs user ke wallet ID card wali "uniform on a hook" analogy kya hai?**

A: User ke credentials forever rakha jaane wala permanent ID card hote hain; role ek uniform hai jo pehno, use karo, aur utaar do — temporary credentials, leak/rotate karne ko kuch nahi, sirf assumed hote waqt granted.

**Q: Trust policy vs permission policy — #1 confusion point kya hai?**

A: Trust policy role ke *Trust relationships* tab mein rehti hai, "who can assume this role" ka answer deti hai, `Principal` chahiye hoti hai, koi `Resource` nahi (role khud hi **resource** hai), action `sts:AssumeRole` hoti hai. Permission policy role par attached hoti hai, "assume hone ke baad kya kar sakta hai" ka answer deti hai, `Resource` chahiye hoti hai, `Principal` forbid karti hai. Dono required hain, separately evaluate hote hain, merge karne ko kuch nahi.

**Q: Account IDs enumerate kiye bina apni AWS Organization ke kisi bhi account ko kaise trust karte ho?**

A: Trust policy condition mein `aws:PrincipalOrgID` use karo:

```json
"Condition": { "StringEquals": { "aws:PrincipalOrgID": "o-abc123xyz" } }
```

**Q: Lambda execution role ki trust policy kya hai, aur STS ke through AssumeRole mechanics flow kaisa dikhta hai?**

A: STS ke through AssumeRole: caller authenticate hota hai → STS target role ki trust policy check karta hai → STS temporary credentials issue karta hai (Access Key, Secret Key, Session Token, 15min–12h expire hota hai) → caller unhe use karta hai, role ki permission policy se governed hota hai.

```json
{
  "Version": "2012-10-17",
  "Statement": [
    { "Effect": "Allow", "Principal": { "Service": "lambda.amazonaws.com" }, "Action": "sts:AssumeRole" }
  ]
}
```

**Q: Cross-account AssumeRole mechanics end to end walk through karo.**

A: Account B ek role banata hai jo Account A ka ARN trust karta hai; Account B ek scoped permission policy attach karta hai; Account A ke caller ko us role ARN par uski **apni** `sts:AssumeRole` permission chahiye (sirf trust kaafi nahi); caller `AssumeRole` call karta hai, temporary creds milte hain (`AccessKeyId`+`SecretAccessKey`+`SessionToken`, 15min–12h), unhe Account B ke resource ke against use karta hai.

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

**Q: Confused-deputy problem kya hai, aur External ID aur GitHub OIDC iska ek-ek version kaise solve karte hain?**

A: Ek shared role jo bahut se customers serve karta hai, use galat account ke against act karne ke liye trick kiya ja sakta hai. **External ID** (`sts:ExternalId` condition) — vendor/third-party access ek unique ID tak scoped hota hai jo aap unhe issue karte ho. **GitHub OIDC** — `Federated` principal GitHub OIDC provider hai, condition `aud=sts.amazonaws.com` aur `sub` StringLike `repo:org/repo:*` check karti hai — koi static keys nahi, short-lived, per repo/branch scoped.

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

**Q: Ek role actually different compute types tak kaise reach karta hai?**

A: **EC2** — ek **instance profile** ke through (exactly ek role hold karne wala thin wrapper; console silently create karta hai, Terraform/CFN mein explicitly declare karna padta hai). **Lambda** — `lambda.amazonaws.com` se assumed execution role. **ECS** — do separate roles: **task execution role** (ECS agent — image pull karta hai, logs likhta hai) vs **task role** (aapka app code); dono ko mix karna ek real production bug hai. **EKS** — IRSA ya EKS Pod Identity ek K8s service account ko per pod ek IAM role se map karta hai.

**Q: IMDSv2 security ke liye kyun matter karta hai, aur hamesha kya set karna chahiye?**

A: IMDSv1 ek plain `GET` ka answer deta hai, isliye koi bhi SSRF bug instance role ke credentials steal kar sakta hai. IMDSv2 pehle session token ke liye ek `PUT` require karta hai, jo simple SSRF nahi kar sakta. Launch template mein hamesha `HttpTokens: required` aur `HttpPutResponseHopLimit: 1` set karo.

```bash
# IMDSv2 (session-oriented, and what you should require)
TOKEN=$(curl -X PUT "http://169.254.169.254/latest/api/token" \
  -H "X-aws-ec2-metadata-token-ttl-seconds: 21600")
curl -H "X-aws-ec2-metadata-token: $TOKEN" \
  http://169.254.169.254/latest/meta-data/iam/security-credentials/
```

**Q: `iam:PassRole` kya hai, yeh privilege-escalation risk kyun hai, aur isko kaise scope karte ho?**

A: Yeh "let me *hand* this role to a service" hai (`sts:AssumeRole` = "let me *become* it" se different). Unrestricted, `lambda:CreateFunction` + unrestricted `PassRole` ek user ko `AdministratorAccess` ke saath ek Lambda create karne deta hai aur admin ke roop mein code run karne deta hai. Specific role ARNs par `iam:PassedToService` condition ke saath scope karo.

```json
{
  "Effect": "Allow",
  "Action": "iam:PassRole",
  "Resource": "arn:aws:iam::123456789012:role/lambda-prod-order-writer-role",
  "Condition": { "StringEquals": { "iam:PassedToService": "lambda.amazonaws.com" } }
}
```

**Q: Human access ke liye modern answer kya hai, aur IAM users kab abhi bhi sense banate hain?**

A: **IAM Identity Center** (permission sets har account ke liye roles ke roop mein materialize hote hain, `aws sso login` ke through short-lived credentials, single offboarding point) — humans ke liye, IAM users nahi. IAM users ko legacy apps ke liye reserve karo jo role assume nahi kar sakti, plus ek break-glass account.

### Hands-On: IAM Roles

**Q: EC2 instance par "role beats access keys" argument prove karne wale do commands kaunse hain?**

A: Role attach karne se pehle: `aws s3 ls` → "Unable to locate credentials". Attach karne ke baad: `aws sts get-caller-identity` → `assumed-role/...` ARN, phir `aws s3 ls` kaam karta hai — box par kahin bhi zero access keys ke saath, aur change bina reboot ke effective ho gaya.

```bash
# BEFORE attaching the role, on the instance:
aws s3 ls
# → "Unable to locate credentials"

# AFTER attaching:
aws sts get-caller-identity
# → arn:aws:sts::123456789012:assumed-role/ec2-dev-s3-reader-role/i-0abc123
aws s3 ls        # works — and there are no access keys anywhere on the box
```

**Q: CLI se ek cross-account role kaise assume karte ho, aur verify kaise karte ho ki kaam kiya?**

A: Ya to `aws sts assume-role` se one-off, ya — day-to-day work ke liye cleaner — `~/.aws/config` mein ek named profile se CLI ko assumption khud karne do:

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

**Q: Har IAM security tool ko us question se pair karo jo woh answer karta hai.**

A:

- **Credential Report** — kya exist nahi karna chahiye (no MFA, stale/never-used keys)
- **Access Advisor** — kya over-granted hai (services allowed hain lekin unused hain)
- **IAM Access Analyzer** — kya externally exposed hai, plus policy validation aur generate-policy-from-CloudTrail
- **Policy Simulator** — kya yeh principal X karne ke allowed hoga, change ship karne se pehle
- **CloudTrail** — kaun, kya, kab, kahan se kiya
- **AWS Config** — continuous compliance (`iam-user-mfa-enabled`, `access-keys-rotated`, etc.)

```bash
aws iam generate-credential-report
aws iam get-credential-report --query Content --output text | base64 -d > report.csv
aws iam get-account-authorization-details > iam-snapshot.json     # full policy/role dump for offline review
aws accessanalyzer list-findings --analyzer-arn <arn>
```

### IAM Pitfalls

**Q: "my role has permission but access still fails" wala golden debugging checklist kya hai?**

A: Execution role permission → trust policy → resource-based policy (bucket/queue/key) → KMS key policy (agar encrypted hai) → SCP/permission boundary.

**Q: Paanch common IAM pitfalls aur unke fixes naam karo.**

A:

- **Trust vs permission policy confuse karna** — dono ko separately check karo.
- **`AdministratorAccess` overuse karna** — read-only se start karo, incrementally add karo.
- **Overly broad trust `Principal: "*"`** — specific account/service/OIDC provider + `SourceArn`/`SourceAccount` tak restrict karo.
- **Stale env-var keys instance role ko shadow kar rahi hain** — credential chain mein env vars IMDS se upar rank karte hain.
- **Permission ECS execution role mein add kiya gaya task role ke jagah** — execution role = agent, task role = app code.

### Secrets Manager vs Parameter Store

**Q: Secrets Manager ko Parameter Store ke jagah kab choose karte ho?**

A: **Secrets Manager** — rotation chahiye ya genuine credential hai (DB passwords, third-party API keys); built-in RDS/Redshift/DocumentDB rotation, KMS-always-encrypted, 64KB max, native cross-region replication. **Parameter Store** — configuration/feature flags/non-rotating settings; Standard tier free hai, 4KB/8KB max, no native rotation, manual replication. Pure config ke liye Secrets Manager over-use karna (thousands of values) needless rotation/API-call overhead pay karta hai.

```csharp
var client = new AmazonSecretsManagerClient();
var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = "prod/orders/db" });
var connectionString = response.SecretString;
```

#### Secrets Manager — Pitfalls

**Q: Secret rotation application ko down kyun kar sakta hai, aur fix kya hai?**

A: Rotation ek naya version create karta hai aur `AWSCURRENT` label usme move kar deta hai (purana `AWSPREVIOUS` ban jaata hai); ek app jo startup par secret ek baar read karti hai aur forever cache karti hai, woh rotation complete hone par invalidated password use karti rehti hai. Fix: `AWSCURRENT` ko auth failure par re-fetch karo aur ek baar retry karo — ya better, app se password poori tarah remove karne ke liye RDS Proxy/IAM database authentication use karo.

**Q: Secrets Manager ke saath sabse common performance mistake kya hai, aur usse kaise avoid karte ho?**

A: Har request par `GetSecretValue` call karna — yeh ek throttled, per-10,000-calls-billed network call hai. Secrets Manager caching library ya Parameters and Secrets Lambda extension (local HTTP cache sidecar) ke through TTL ke saath memory mein cache karo.

**Q: Teen aur Secrets Manager gotchas jaanne layak list karo.**

A:

- Customer-managed key use karte waqt **dono** `secretsmanager:GetSecretValue` **aur** `kms:Decrypt` chahiye.
- Deletion ka ek mandatory 7–30 day recovery window hota hai (teardown/recreate CI todta hai jab tak `ForceDeleteWithoutRecovery` use na karo).
- ECS `valueFrom`/Lambda env-var injection **start par ek baar hi** resolve hota hai — secret rotate karna running task update nahi karta; redeploy karna padta hai ya code mein read karna padta hai.

### Least Privilege & Permission Boundaries in Practice

**Q: `{"Effect":"Allow","Action":"dynamodb:*","Resource":"*"}` ko tenant isolation ke saath least privilege ke roop mein rewrite karo.**

A: Anti-pattern (real .NET/Lambda code mein constantly dekha jaata hai):

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

**Q: Permission boundary vs SCP — structural difference kya hai?**

A: Ek **permission boundary** individual role/user par attach hoti hai aur uski maximum permissions ko cap karti hai uski apni attached policies ke bawajood — platform teams isko use karti hain app teams ko ek ceiling ke andar self-serve roles dene ke liye. Ek **SCP** AWS Organizations account/OU level par attach hoti hai, account mein root sahit sabko limit karti hai. Koi bhi kabhi grant nahi karti — dono sirf subtract karte hain.

### IAM Rapid-Fire Q&A

**Q: IAM regional hai ya global?**

A: Global — koi region selection nahi, everywhere same identities; side effect eventual consistency hai (naya role briefly usable hone mein fail ho sakta hai).

**Q: User vs Group vs Role, har ek ek sentence mein?**

A: User = long-term credentials wala ek insaan. Group = permission container jo sirf users hold karta hai (no nesting, principal nahi). Role = permanent credentials ke bina permissions, ek trusted entity dwara temporarily assumed.

**Q: Ek policy allow karti hai, doosri deny karti hai — kaun jeetega?**

A: Explicit Deny hamesha jeetta hai, `AdministratorAccess` ko bhi override karke; koi matching policy na ho to implicit deny hota hai.

**Q: Role par kaunse do policies hoti hain, aur kaunsi broken hai yeh kaise pata karte ho?**

A: Trust policy (kaun assume kar sakta hai — `Principal` hoti hai) aur permission policy (kya kar sakta hai). Agar `AssumeRole` khud fail ho → trust policy; agar assumption succeed hoti hai lekin API call fail ho jaaye → permission policy.

**Q: STS exactly kya return karta hai, aur kitni der ke liye?**

A: `AccessKeyId`, `SecretAccessKey`, `SessionToken`, plus expiry — max session duration ke hisaab se 15 min se 12h tak. Role chaining hard-capped hai 1 hour par.

**Q: Kya `AdministratorAccess` account mein sab kuch kar sakta hai?**

A: Nahi — root-only actions abhi bhi rehte hain (account close karna, support plan change karna, S3 MFA-delete, RI Marketplace seller registration, etc.).

**Q (scenario): Ek nightly batch job almost exactly 60 minutes mein die ho jaata hai — kyun?**

A: Role chaining — ek already-assumed role se role assume karna session ko 1 hour tak cap kar deta hai, target role ki max-duration setting ke bawajood. Target role ko base identity se directly assume karo, ya ek long session hold karne ke jagah credentials refresh karo.

**Q (scenario): EC2 role clearly `s3:GetObject` allow karta hai, lekin app ko AccessDenied milta hai — kahan dekhoge?**

A: Pehle `aws sts get-caller-identity` (stale env vars jo instance profile ko shadow karte hain `user/...` dikhate hain `assumed-role/...` nahi); phir bucket policy; phir KMS key policy agar SSE-KMS hai; phir SCP/permission boundary; yeh bhi confirm karo ki instance profile actually exist karta hai (IaC bina instance profile ke role create kar sakta hai).

---

## Infrastructure as Code & CI/CD

### AWS CodeCommit

**Q: CodeCommit kya hai, aur practice mein GitHub ke dominate karne ke bawajood yeh interviews mein kyun aata hai?**

A: AWS ki apni managed Git repo service — same Git semantics, lekin third-party SaaS account ke jagah IAM ke through access-controlled. Point yeh test karna hai ki isko IAM-native recognize karo (reconcile karne ko koi separate permission model nahi), yeh nahi ki real team ke liye GitHub ke jagah isko choose karoge.

### AWS CodeBuild

**Q: CodeBuild kya hai, aur uske core concepts kya hain?**

A: Fully managed CI service — code compile karta hai, tests run karta hai, on-demand isolated containers mein artifacts produce karta hai, Jenkins patch/scale karne ki zarurat nahi. **Build Project** (source/env/steps/artifact config), **Build Environment** (OS+runtime+compute size+Docker-in-Docker ke liye privileged mode), **buildspec.yml** (script YAML ke roop mein) with phases INSTALL → PRE_BUILD → BUILD → POST_BUILD.

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

**Q: #1 "works standalone, fails in VPC" CodeBuild bug kya hai, aur cost kaise control hota hai?**

A: Bhool jaana ki build VPC ke andar run hone par private RDS/APIs reach karne ke liye **NAT Gateway** chahiye. Cost per-build-minute × compute size hai (koi idle cost nahi); smallest sufficient compute, fail-fast, aur dependency caching (S3 ya local Maven/npm/NuGet cache) se optimize karo.

### AWS CodePipeline

**Q: CodePipeline actually kya karta hai, aur uske core concepts kya hain?**

A: Yeh ek **orchestrator** hai — khud compile/test/deploy nahi karta, CodeBuild/CodeDeploy/ECS/Lambda/CloudFormation ke across Source→Build→Test→Deploy stages coordinate karta hai. Pipeline (workflow) → Stage (sequential) → Action (stage ke andar ek task; ek stage ke andar actions parallel run ho sakte hain).

**Q: CodeDeploy Deployment Group kya hai, aur chaar pipeline execution states kya hain?**

A: Deployment Group = logical target set (EC2/ASG, ECS service, ya Lambda function+alias) jispar ek CodeDeploy application deploy karta hai, aur jahan strategy (in-place/blue-green, rolling %, alarm-triggered rollback) bind hoti hai. Pipeline execution states: **Started, Succeeded, Failed, Stopped**.

**Q: CodePipeline kaise billed hoti hai, aur yeh CodeDeploy aur Jenkins se kaise different hai?**

A: Per active pipeline per month billed hoti hai, per execution nahi (us layer par runs free hain). CodePipeline pura release workflow orchestrate karta hai; CodeDeploy sirf deployment step handle karta hai; Jenkins fully custom/self-hosted hai maximum flexibility ke saath.

### CodePipeline/CodeBuild Trap Scenarios

**Q: Ek pipeline pehli stage par immediately fail ho jaati hai — likely cause kya hai?**

A: Source-stage authentication broken — commonly ek expired/revoked **CodeStar Connections** connection GitHub/Bitbucket ko, jisko console mein manual "Update pending connection" re-authorization chahiye.

**Q: Teen aur common CodePipeline/CodeBuild trap symptoms aur root causes list karo.**

A:

- Docker build CodeBuild mein fail hota hai lekin locally kaam karta hai → privileged mode enable nahi hai, ya ECR login missing hai.
- Successful build ke baad ECS purani image run karta hai → service update nahi hua, ya tag static `:latest` hai unique tag/digest ke jagah.
- Build sirf VPC ke andar fail hota hai → AWS service access ke liye NAT Gateway/VPC endpoint missing hai.

**Q: AWS mein CI/CD failures debug karne ka senior-level summary kya hai?**

A: Zyada tar failures IAM misconfigurations, artifact-handling mistakes, missing Docker privileges, VPC networking gaps, ya role-boundary confusion hote hain — AWS CI/CD debug karna primarily ek permissions exercise hai, build-command exercise nahi.

### CloudFormation vs Terraform/CDKTF

**Q: CloudFormation, Terraform, aur CDKTF ko scope, state management, aur rollback par compare karo.**

A:

| | CloudFormation | Terraform | CDKTF |
|---|---|---|---|
| Scope | AWS-only | Multi-cloud | Multi-cloud (peeche Terraform) |
| Language | JSON/YAML | HCL | TypeScript/Python/C#/Java/Go → Terraform JSON mein synthesize hota hai |
| State | AWS-managed, no file | Aap khud own karte ho — S3 backend + DynamoDB lock table | Terraform jaisa hi |
| Rollback on failure | Automatic | Kuch nahi — remediation khud manage karo | Terraform jaisa hi |

Classic production state backend — state file ke liye S3, sirf locking ke liye DynamoDB:

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

**Q: Terraform state ek security concern kyun hai, aur mitigation kya hai?**

A: State mein **plaintext** mein secrets hote hain (RDS passwords, generated keys) — `sensitive = true` sirf CLI output redact karta hai, state file khud nahi. Mitigate karo: state bucket encrypt karo (SSE-KMS), public access block karo, bucket policy ko pipeline role tak restrict karo, versioning enable karo, aur real secrets ko Terraform se pass karne ke jagah Secrets Manager/Parameter Store mein rakho.

**Q: CDKTF plain Terraform se kya change karta hai, aur kya same rehta hai?**

A: CDKTF sirf **authoring language** replace karta hai — aap TypeScript/Python/C#/etc likhte ho, `cdktf synth` isko same JSON mein compile karta hai jo Terraform normally consume karta hai, phir standard Terraform CLI le leta hai. State model, backend, aur engine unchanged hote hain.

Concrete side-by-side — HCL vs CDKTF (TypeScript) mein ek S3 bucket:

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

### Terraform/CDKTF in Practice — Expect Karne Layak Depth Questions

**Q: Ek saved plan file apply karna ek trustworthy CI/CD pipeline ke liye kyun matter karta hai?**

A: Saved plan ke bina `terraform apply` apply time par re-plan karta hai, isliye jo run hota hai woh review kiye gaye se different ho sakta hai. `terraform plan -out=tf.plan` phir `terraform apply tf.plan` guarantee karta hai ki applied change exactly reviewed wala hi hai — real gate aur rubber stamp ke beech ka difference.

```bash
terraform init         # download providers/modules, configure backend
terraform fmt -check   # formatting gate in CI
terraform validate     # syntax/type check, no AWS calls
terraform plan -out=tf.plan     # the dry-run diff — the artifact a reviewer should read
terraform apply tf.plan         # apply exactly what was reviewed, no re-plan drift
terraform destroy
```

**Q: `for_each` vs `count` — highest-value practical Terraform question kya hai, aur uska right answer kya hai?**

A: `count` **position** se index karta hai — list ke beech se ek item remove karna baad ke har index ko shift kar deta hai, jisse Terraform un resources ko destroy/recreate kar deta hai jinhe change nahi hona chahiye tha. `for_each` ek **stable string** se key karta hai, isliye ek remove karna sirf usi ko affect karta hai. `for_each` use karo kisi bhi cheez ke liye jise aap add/remove karoge; `count` ko simple on/off toggle ke liye reserve karo.

**Q: Workspaces vs directory-per-environment — production pattern kaunsa hai?**

A: **Directory (ya repo) per environment** ek shared module ke saath production pattern hai — separate state/backend/credentials, environments legitimately different ho sakte hain. **Workspaces** per workspace ek shared backend key share karte hain — cheap hai lekin galat environment ke against `apply` run karne ka risk hai; sirf dev/test variants ke liye fine hai.

**Q: State-file recovery process kya hai, aur concurrent-apply corruption kaise prevent karte ho?**

A: State bucket par **S3 object versioning** se restore karo (ya last resort ke roop mein `terraform import` resources ko wapas import karo). Concurrent applies **DynamoDB lock table** se prevent hote hain — second pipeline run wait karta hai ya fail hota hai.

**Q: Ek console-created ("ClickOps") resource ko Terraform management mein kaise adopt karte ho, aur delete kiye bina state se kaise remove karte ho?**

A: `terraform import aws_s3_bucket.app_data my-existing-bucket` usko management ke under le aata hai. `terraform state rm` usko state se remove karta hai AWS mein delete **kiye bina** — jab ek resource kahin aur manage karna zaroori ho tab yeh escape hatch hai.

```bash
terraform import aws_s3_bucket.app_data my-existing-bucket   # bring unmanaged resources under management
terraform state list / show / mv / rm                        # refactor or drop state entries
terraform plan -refresh-only                                 # detect drift without proposing changes
```

**Q: Recommended GitHub Actions + Terraform CI/CD pattern kya hai?**

A: PR open hua → fmt/validate/tflint/tfsec-checkov → `plan` ko PR comment ke roop mein post kiya (read-only role). PR merge hua → saved plan se `apply` (privileged role, protected environment, prod ke liye manual approval). **GitHub OIDC assuming an AWS role** ke through authenticate karo — secrets mein koi long-lived keys nahi.

```
PR opened   → fmt, validate, tflint, tfsec/checkov → terraform plan → post plan as a PR comment
PR merged   → terraform apply <saved plan>          (protected environment, manual approval for prod)
```

**Q: CloudFormation ke Terraform modules aur multi-account deployment ke equivalents kya hain?**

A: **Nested stacks** = modules (per-stack resource limits ke around bhi kaam karte hain). **StackSets** = management account se bahut se accounts/regions ke across ek template deploy karna — us specific use case ke liye Terraform ke multi-state/provider-per-account approach se genuinely easier hai.

**Q: CDK vs CDKTF — kya difference hai?**

A: AWS CDK **CloudFormation** synthesize karta hai (AWS-only, AWS-managed state). **CDKTF** **Terraform** synthesize karta hai (multi-cloud, self-managed state). Same authoring ergonomics, different engine peeche.

---

## S3

### S3 Buckets & Objects

**Q: What is S3, and what are the bucket naming/scoping rules?**

A: Infinitely scalable **object** storage hai (filesystem nahi) — flat key/value store, 11 nines durability, koi capacity provision karne ki zarurat nahi. Bucket names globally unique hote hain (DNS-embedded), 3–63 chars, lowercase/numbers/hyphens/dots, **ek region** mein permanently rehte hain, aur nested nahi ho sakte.

**Q: What is the S3 key really, and what changed about S3's consistency model in Dec 2020?**

A: Key **full flat path** hai — S3 mein real directories nahi hote; "folders" prefixes + `/` delimiter se render hote hain. Dec 2020 se, S3 **strong read-after-write consistency** deta hai PUTs/overwrites/DELETEs aur consistent LIST ke liye — "eventually consistent for overwrites" ek outdated answer hai.

**Q: What's the object size limit, and when is multipart upload required?**

A: Max object size 5TB hai; ek single PUT 5GB par cap hoti hai, isliye multipart upload uske aage **mandatory** hai aur ~100MB se upar recommended hai.

**Q: Show the core S3 CLI commands, and the difference between `aws s3` and `aws s3api`.**

A: `aws s3` high-level convenience layer hai (`cp`, `sync`, `mv`); `aws s3api` raw per-call API expose karta hai (`put-object`, `put-bucket-policy`) jab exact control chahiye.

```bash
aws s3 mb s3://my-unique-bucket-name --region us-east-1
aws s3 cp ./report.pdf s3://my-bucket/invoices/2026/08/report.pdf
aws s3 ls s3://my-bucket/invoices/2026/ --recursive --human-readable --summarize
aws s3 sync ./local-dir s3://my-bucket/prefix/ --delete    # ⚠ --delete removes remote extras
```

### S3 Bucket Policies & Access Control

**Q: What four mechanisms grant S3 access, and which should you avoid?**

A: **IAM policy** (identity-based, apne khud ke workloads ke liye default), **bucket policy** (resource-based, cross-account/whole-bucket rules, `Principal` chahiye), **ACLs** (legacy — avoid karo; Object Ownership = "Bucket owner enforced" se disable karo), **Access Points** (scale par many-team access).

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

A: Char independent toggles hain (block new/all public ACLs, block new/all public bucket policies), 2023 se by default on hain, account aur bucket level dono par available hain. **Account-level BPA bucket-level ko override karta hai** — agar ek `Principal: "*"` bucket policy ka koi effect nahi hai, BPA hi wajah hai.

### S3 Static Website Hosting

**Q: Why must you put CloudFront in front of an S3 static website for anything real?**

A: S3 website endpoint **HTTP only** hai — HTTPS support nahi. CloudFront + Origin Access Control (OAC) TLS deta hai ACM se, custom domain, caching, aur bucket ko fully private rehne deta hai.

```
http://my-bucket.s3-website-us-east-1.amazonaws.com
http://my-bucket.s3-website.us-east-1.amazonaws.com     # region-dependent format
```

**Q: REST endpoint vs website endpoint — what's the difference?**

A: REST endpoint (`bucket.s3.region.amazonaws.com`) HTTPS + IAM/SigV4 support karta hai lekin index-document behavior nahi. Website endpoint index/error documents aur redirects deta hai lekin HTTP-only aur public-read hai.

### S3 Versioning & Replication

**Q: What does versioning actually protect against, and what's the cost gotcha?**

A: Accidental overwrite/delete se protect karta hai — ek DELETE bas ek **delete marker** add karta hai (data remove nahi hota; permanent deletion ke liye version ID chahiye). Ek baar enable hone ke baad sirf **suspend** ho sakta hai, kabhi off nahi. Cost gotcha: aap har version ke liye pay karte ho — hamesha noncurrent versions expire karne wali ek lifecycle rule ke saath pair karo.

**Q: CRR vs SRR, and what are the replication rules/gotchas?**

A: CRR (cross-region) = DR/latency/compliance; SRR (same-region) = log aggregation, prod→test seeding. Rules: versioning dono sides par required hai; replication **asynchronous aur not retroactive** hai (backfill ke liye S3 Batch Replication use karo); delete markers optionally replicate hote hain lekin **permanent version deletes kabhi replicate nahi hote**; **no chaining** (A→B→C se C mein A ke objects propagate nahi hote).

### S3 Performance, Analytics & Cost Tooling

**Q: What's S3's baseline request-rate limit, and how do you scale beyond it?**

A: 3,500 PUT/COPY/POST/DELETE aur 5,500 GET/HEAD requests per second, **per prefix**, prefixes ki number par koi limit nahi — keys ko kai prefixes ke across spread karke aur parallel mein read/write karke scale karo.

**Q: What's the difference between multipart upload, Transfer Acceleration, byte-range fetch, and S3 Select?**

A: **Multipart upload** — parallel, resumable parts, >5GB ke liye required. **Transfer Acceleration** — uploads ko nearest CloudFront edge ke through AWS backbone par route karta hai, long-distance uploads ke liye. **Byte-range fetch** — different ranges ke parallel GETs, ya partial reads. **S3 Select** — ek object par server-side SQL, transfer/CPU kam karta hai (many objects ke across queries ke liye Athena use karo).

### S3 Batch Operations

**Q: You have 40 million objects that are unencrypted or in the wrong storage class — what's the right tool?**

A: **S3 Batch Operations** — manifest ke roop mein ek S3 Inventory report (ya apna khud ka CSV) generate karo, phir ek Batch Operations job run karo (copy, replace tags/ACLs, Glacier se restore, Object Lock apply, ya per object ek Lambda invoke) managed retries aur completion report ke saath. Ek custom loop script nahi.

### S3 Requester Pays

**Q: What does Requester Pays change, and what must the requester do differently?**

A: Requester request + egress costs pay karta hai jabki owner storage pay karta rehta hai. Requester ko authenticated principal hona chahiye (anonymous access nahi) aur har request mein `x-amz-request-payer: requester` bhejna chahiye, ya 403 milega. Use case: large public datasets distribute karna dusron ke egress bills absorb kiye bina.

### S3 Best Practices

**Q: List the core S3 best practices to recite.**

A: Account-level BPA on + ACLs disable; encryption/HTTPS enforce karo `Deny` bucket policy se; versioning + noncurrent versions expire karne wali lifecycle + incomplete multipart uploads abort; unknown hone par Storage Class Analysis ya Intelligent-Tiering driven lifecycle; throughput ke liye keys ko prefixes ke across spread karo; public content ke liye CloudFront+OAC, kabhi public bucket nahi; jo cheez lose nahi kar sakte uske liye CRR (versioning ka substitute nahi).

### S3 Shared Responsibility Model

**Q: What's the one-liner for S3's shared responsibility split?**

A: "S3 ne kabhi mera data lose nahi kiya — lekin S3 mujhe usko public banane bhi de dega. Durability AWS ka job hai; access control aur retention mera hai." AWS: infrastructure, 11-nines durability, encryption options. Aap: bucket/IAM policies, BPA, versioning/replication/lifecycle config, logs enable aur review karna.

### S3 Storage Classes & Lifecycle Policies

**Q: Rank the S3 storage classes by retrieval speed and minimum storage duration.**

A: Standard (immediate, no minimum) → Intelligent-Tiering (immediate, auto-tiers) → Standard-IA (immediate, 30-day min) → One Zone-IA (immediate, 30-day min, single AZ) → Glacier Instant Retrieval (immediate, 90-day min) → Glacier Flexible Retrieval (minutes–hours, 90-day min) → Glacier Deep Archive (~12h, 180-day min).

**Q: What are the storage-class gotchas an interviewer expects?**

A: Standard-IA/One Zone-IA retrieval fee charge karte hain (sirf genuinely infrequent access ke liye); minimum-duration charges apply hote hain early delete/transition par bhi; Intelligent-Tiering mein ek small per-object monitoring fee hai lekin guesswork remove karta hai; versioned old versions ko apni khud ki lifecycle rule chahiye warna aap har historical version ke liye forever pay karte ho.

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

A: **Logs** — Standard→IA 30d par→Glacier Flexible 90d par→365d par expire (early often read, later rarely, compliance window). **Backups** — Deep Archive mein almost immediately (insurance data, 12h restore theek hai, cheapest long-term tier). **Unpredictable access** (shared data lake, user uploads) — Intelligent-Tiering day 1 se.

**Q: What's the minimum-duration billing trap, concretely?**

A: Standard-IA (30-day min) ya Glacier (90-day min) mein transition karke phir minimum elapse hone se pehle delete/transition karna abhi bhi full minimum bill karta hai — quickly-deleted data par aggressive short-interval lifecycle rules Standard par chhodne aur directly expire karne se *zyada* cost kar sakte hain.

**Q: How do you fix the "old versions cost money forever" problem in Terraform?**

A: `aws_s3_bucket_lifecycle_configuration` ke andar ek `noncurrent_version_expiration { noncurrent_days = 90 }` rule add karo — noncurrent (old) versions expire karta hai unhe indefinitely accumulate hone dene ke bajaye.

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

- **SSE-S3** — AWS key hold karta hai (AES-256), koi audit trail nahi, free, Jan 2023 se default.
- **SSE-KMS** — aapki KMS key, har decrypt CloudTrail mein logged, `s3:GetObject` **aur** `kms:Decrypt` dono chahiye.
- **DSSE-KMS** — KMS key twice apply hoti hai, strict dual-layer regulatory mandates ke liye.
- **SSE-C** — aap per request key supply karte ho, S3 kabhi store nahi karta; HTTPS mandatory hai; key kho do, object kho do.

**Q: Why can a high-throughput app get KMS-throttled even though S3 is fine, and what's the fix?**

A: SSE-KMS object par har GET/PUT KMS `Decrypt`/`GenerateDataKey` call karta hai, jiska per-region request quota hota hai. Fix: **S3 Bucket Keys** — bucket-level key se per-object keys derive karke KMS request traffic ko 99% tak reduce karta hai.

### S3 CORS

**Q: When is a bucket-level CORS rule required, and what commonly trips people up?**

A: Required hai jab bhi ek different origin par ek web page bucket ke against directly assets load karta hai ya fetch/XHR calls karta hai — CORS bucket par configure hona chahiye, app par nahi. Gotchas: `AllowedOrigins` scheme+host+port exactly match karna chahiye; `ExposeHeaders` chahiye JS ko response headers jaise `ETag` read karne ke liye; presigned browser `PUT` uploads ko almost hamesha ek CORS rule chahiye.

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

A: Ek object version permanently delete karne ya versioning suspend karne ke liye ek MFA code chahiye. Pehle versioning enabled chahiye; sirf **root account** dwara CLI/API se toggle ho sakta hai (console se nahi). Kai orgs Object Lock prefer karte hain iske bajaye kyunki yeh root credentials handout karne se bachta hai.

### S3 Access Logs (and the Warning)

**Q: What's the critical warning about S3 server access logging, and what's the alternative for security-relevant audit?**

A: **Kabhi ek bucket ko khud ko log mat karo** — logging khud ek request hoti hai, ek infinite logging loop aur runaway bill create karti hai; hamesha ek separate dedicated bucket ko log karo. "Kisne yeh object delete kiya" ke liye, access logs (free-ish, delayed, best-effort) ke bajaye **CloudTrail data events** (near-real-time, guaranteed delivery, IAM identity include karta hai) use karo.

### S3 Pre-Signed URLs

**Q: What permissions does a presigned URL carry, and what's the most common presigned-URL bug in serverless apps?**

A: Yeh **generator ki permissions** carry karta hai — link jiske paas bhi ho use kar sakta hai, ek bearer token jaisa. Bug: agar **temporary role credentials** (Lambda/ECS/EC2) se generate hua hai, URL kaam karna band kar deta hai jab woh credentials expire ho jate hain (~1h), requested expiry chahe kuch bhi ho.

**Q: What's the senior framing for when to use presigned URLs?**

A: Direct browser upload/download (presigned PUT/GET ya presigned POST policy) taki large files aapke Lambda/ECS compute tier se kabhi na guzre — "apna compute tier se file stream mat karo."

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

A: **Governance** — jin users ke paas `s3:BypassGovernanceRetention` hai woh override kar sakte hain; ek escape hatch ke saath ek guardrail. **Compliance** — koi nahi, root bhi nahi, retention expire hone tak delete/alter kar sakta; genuinely immutable, "ransomware ya admin rights wale ek malicious insider se backups protect karo" ka strongest answer.

**Q: What's Legal Hold, and how does it differ from a retention period?**

A: Ek independent on/off flag koi expiry ke bina, `s3:PutObjectLegalHold` se controlled — litigation holds ke liye use hota hai jaha end date unknown ho, kisi bhi Object Lock retention period se separate.

### S3 Access Points & Object Lambda

**Q: What problem do S3 Access Points solve, and what does Object Lambda add?**

A: Access Points har team/use case ko apna named endpoint + policy dete hain, taki ek giant bucket policy ko sabko serve na karna pade — VPC-restricted ho sakte hain. **Object Lambda** GET path par ek Lambda run karta hai data transform karne ke liye (PII redact karo, images resize karo, rows filter karo) doosri copy maintain kiye bina.

### S3 Security Best Practices & Shared Responsibility

**Q: What's the defense-in-depth layering for securing an S3 bucket, top to bottom?**

A: Block Public Access → bucket policy/IAM → encryption at rest and in transit → versioning + Object Lock → logging and monitoring → VPC endpoint (taki traffic kabhi AWS network se bahar na jaye). S3 breaches essentially hamesha configuration failures hoti hain, durability failures kabhi nahi.

---

## EC2 & Instance Storage

### EC2 Fundamentals

**Q: What's the difference between Stop and Terminate?**

A: **Stop** — EBS data persist karta hai, instance ID kept rehta hai, aap compute ke liye pay karna band kar dete ho lekin EBS ke liye still pay karte ho. **Terminate** — instance aur (default se) root EBS volume destroy ho jate hain.

**Q: List the four EC2 pricing models and their risk/commitment trade-off.**

A: On-Demand (no commitment, highest cost, no risk); Reserved Instances (1–3yr, lower cost, locked in); Savings Plans (1–3yr $/hr commitment, RI se zyada flexible); Spot (no commitment, cheapest, 2-minute warning ke saath reclaimable).

### EC2 Instance Types, User Data & Metadata

**Q: Decode `m6g.2xlarge`, and why is the Graviton (`g`) attribute worth mentioning unprompted for .NET workloads?**

A: `m` = general purpose (~4GiB/vCPU), `6` = 6th gen, `g` = Graviton (ARM64), `2xlarge` = 8 vCPU/32GiB. .NET ne .NET 6 se ARM64 support kiya hai, isliye Graviton instance par move karna usually ek recompile ke saath ~20% cost saving deta hai, code changes nahi.

```
m      5       dn        .2xlarge
│      │       │          └─ size (vCPU/memory scale)
│      │       └─ extra attributes
│      └─ generation (higher = newer, usually better price/performance)
└─ family (workload class)
```

#### User Data — Quick Recall / Full Explanation

**Q: What is EC2 user data, when does it run, and what's the most common mistake with it?**

A: Ek startup script (max 16KB) jise cloud-init/EC2Launch **sirf ek baar, root ke roop mein, first boot par** run karta hai — reboot par yeh re-run nahi hota. Most common mistakes: `#!/bin/bash` shebang omit karna (script silently ignore ho jata hai), aur yeh expect karna ki user data se started ek app reboot survive kar lega (fix: usko ek proper systemd service ke roop mein install karo, user data re-run nahi).

```bash
#!/bin/bash
yum update -y
yum install -y amazon-cloudwatch-agent
systemctl enable --now amazon-cloudwatch-agent
```

**Q: The 16KB user-data limit is small — what's the pattern for a larger bootstrap?**

A: User data ko tiny rakho aur baaki S3 se fetch karo, instance ke IAM role se credentials use karke (box par koi keys nahi):

```bash
#!/bin/bash
aws s3 cp s3://my-bucket/bootstrap.sh /tmp/bootstrap.sh
bash /tmp/bootstrap.sh
```

**Q: Why does a failed user-data script not fail the instance, and where do you debug it?**

A: EC2 `running` report karta hai aur status checks green pass hote hain chahe script error ho jaye — kuch bhi failure automatically surface nahi karta. `/var/log/cloud-init-output.log` pehle check karo jab bhi "instance up aa gaya lekin kuch install nahi hua."

**Q: Why is "golden AMI + thin user data" the senior-preferred pattern over heavy user-data bootstrapping?**

A: Heavy user data slow hoti hai (har launch par hundreds of MB re-download karta hai, exactly jab ek ASG ko fast scale karna hota hai), reproducible nahi (package versions launch date se drift karte hain), aur fragile (ek brief repo outage ek instance chhod deta hai jo "healthy" boot hota hai bina runtime installed ke — trigger karta hai ek ASG launch loop kyunki health checks fail hote hain aur replacements same failure forever repeat karte hain).

#### Instance Metadata (IMDS)

**Q: What is IMDS, and why does it work with no internet access or credentials?**

A: Ek link-local service (`169.254.169.254`) jise har instance apne baare mein facts ke liye query karta hai, locally Nitro hypervisor se answered hota hai — kabhi internet ke over route nahi hota. Sabse important use: instance role ke temporary IAM credentials fetch karna, jise SDK automatically refresh karta hai.

```bash
curl http://169.254.169.254/latest/meta-data/instance-id
curl http://169.254.169.254/latest/meta-data/placement/availability-zone
curl http://169.254.169.254/latest/meta-data/iam/security-credentials/my-role   # ← temporary IAM credentials
```

**Q: User data vs instance metadata — what's the directional difference?**

A: User data = instructions jo **aap** instance configure karne ke liye dete ho ("startup par yeh karo"). Instance metadata = facts jo **AWS** instance ke baare mein deta hai ("mai kaun hu, mere credentials kya hain") — same endpoint family se deliver hote hain lekin opposite direction mein.

**Q: Why must you require IMDSv2, and what's the historical breach it prevents?**

A: IMDSv1 ek plain GET ka answer deta hai, isliye aapki app mein ek SSRF bug ko metadata endpoint par aim kiya ja sakta hai instance role ke credentials steal karne ke liye (Capital One breach ke peeche ka mechanism). IMDSv2 ko session token pehle lene ke liye header ke saath ek PUT chahiye — simple SSRF woh nahi kar sakta. `HttpTokens: required` se enforce karo.

### Security Groups, Their Properties & Classic Ports

**Q: What are the defining properties of a Security Group, and how does it differ from a NACL?**

A: SG: ek ENI se attach hota hai (instance se nahi), **allow-only** rules, **stateful** (return traffic auto-allowed), default deny-all-inbound/allow-all-outbound, changes immediately apply hote hain. NACL: ek subnet se attach hota hai, allow **aur** deny rules, **stateless** (dono directions explicitly allow karna hoga — ephemeral port range 1024–65535 bhi return traffic ke liye), numbered order mein evaluate hota hai (first match wins).

**Q: Why should security groups reference other security groups instead of CIDR blocks?**

A: `App-SG: inbound 8080 from ALB-SG` (ek CIDR nahi) ka matlab hai app tier sirf ALB ke through reachable rehti hai aur automatically kaam karti rehti hai jab instance IPs change hoti hain ya ASG scale karta hai — kabhi bhi rule updates ki zarurat nahi.

```
ALB-SG:  inbound 443 from 0.0.0.0/0
App-SG:  inbound 8080 from ALB-SG      ← not a CIDR
DB-SG:   inbound 5432 from App-SG      ← not a CIDR
```

**Q: "Connection times out" vs "connection refused" — what does each tell you?**

A: Timeout/hang = network-layer problem (SG, NACL, route table, wrong subnet). "Connection refused" = network host tak sahi se pahunch gaya; us port par kuch listen nahi kar raha — ek application-layer problem.

### Public IP vs Private IP vs Elastic IP

**Q: What's the classic Elastic IP gotcha, and what's the senior alternative to reaching for one?**

A: Ek public IP stop par release hota hai aur start par ek **naya assign** hota hai — jo bhi usko hardcode kiya gaya ho (DNS, partner allowlists) break ho jata hai. EIP ke bajaye, ek load balancer (stable DNS name), Route 53 alias record, ya outbound-only needs ke liye ek NAT Gateway prefer karo. Legitimate EIP uses: partner firewall allowlisting a fixed IP, NAT gateway, fast manual failover.

### Placement Groups

**Q: Cluster vs Spread vs Partition placement groups — what's the trade-off of each?**

A: **Cluster** — same rack, single AZ; best network performance, worst blast radius (ek rack failure sab kuch le jati hai). **Spread** — distinct hardware AZs ke across; max isolation, 7 instances/AZ/group tak capped. **Partition** — grouped racks, 7 partitions/AZ tak; rack-aware distributed systems (Kafka, Cassandra, HDFS) ke liye middle ground.

### Elastic Network Interfaces (ENIs)

**Q: What's the cheap-failover trick using secondary ENIs?**

A: Primary ENI (`eth0`) detach nahi ho sakta, lekin ek secondary ENI detach aur ek different instance se **same AZ mein** reattach ho sakta hai — ENI ko move karna (uski IP, MAC, aur SGs ke saath) traffic ko usko follow karwa deta hai ek standby instance tak.

### EC2 Hibernate

**Q: How does Hibernate differ from Stop, and what are its key requirements?**

A: Hibernate RAM ko encrypted root EBS volume mein dump karta hai aur shutdown ho jata hai; restart par RAM restore hoti hai aur processes resume hote hain — no boot, no warm-up. Requires: EBS-encrypted root volume RAM image ke liye enough badi, supported family/size RAM 150GiB se under, sirf **launch par** enabled, max 60 din hibernated.

### EC2 Purchasing Options — The Complete Set

**Q: Dedicated Instances vs Dedicated Hosts — what's the deciding factor?**

A: Dono single-tenant hardware hain, lekin sirf **Dedicated Hosts** physical server expose karte hain (sockets/cores/host affinity) — per-socket/per-core BYOL licensing (Windows Server, SQL Server, Oracle) ke liye required. Agar question bring your own license mention kare, answer Dedicated Hosts hai.

**Q: Capacity Reservation vs Reserved Instance/Savings Plan — what's the distinction?**

A: RI/Savings Plan = ek **billing** construct hai (discount, koi capacity guarantee nahi). Capacity Reservation = ek **capacity** construct hai (guaranteed availability On-Demand rates par, koi discount nahi). Guarantee + discount pane ke liye dono combine karo.

### EC2 Shared Responsibility Model

**Q: What's the one-liner for EC2's shared-responsibility split?**

A: "AWS host *ke liye* security responsible hai aur hypervisor ke neeche sab kuch; guest OS se upar — patching, firewall rules, keys, IAM, encryption, backups — sab mera hai. Lambda usme se most of that line AWS par move kar deta hai; EC2 usko mere par rakhta hai."

### EC2 Sizing, Pricing Decisions & CPU Credit Gotchas

**Q: How do you actually size an EC2 instance rather than just naming pricing models?**

A: Workload shape se start karo (CPU-bound→`c`, memory-bound→`r`, general→`m`, bursty/idle→`t`); CloudWatch ke through real load ke under CPU/memory/network benchmark karo (+ memory ke liye CloudWatch Agent); ~40–60% steady-state utilization target karo; AWS Compute Optimizer ko actual usage history se right-sizing recommend karne do, guessing ke bajaye.

**Q: Explain the T-family CPU credit trap as a production-incident scenario.**

A: T-instances baseline se neeche run karte waqt CPU credits earn karte hain aur baseline se upar burst karne ke liye spend karte hain. **Standard mode** mein, credit balance exhaust hona CPU ko hard-throttle karke baseline tak wapas kar deta hai — "app hours tak fine tha, phir suddenly sluggish," typically ek sustained load period (batch job, traffic spike) se jo credit balance se zyada chal gaya. Diagnosis: `CPUCreditBalance`/`CPUSurplusCreditBalance` check karo, sirf `CPUUtilization` nahi (jo deceptively capped lagta hai, 100% par pegged nahi). **Unlimited mode** throttling avoid karta hai lekin sustained bursting ke liye extra bill karta hai — ek sign ki aap T-family se outgrow ho gaye ho.

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

A: Predictable 12+ month capacity need → Savings Plan/RI. ~2-min interruption tolerate karta hai → Spot. Brand-new/unknown steady-state → pehle On-Demand, baad mein commit karo. EC2+Fargate+Lambda ke across spans karta hai → Compute Savings Plan (flexible) EC2 Instance SP ke upar. Business-hours-only dev/test → On-Demand + scheduled stop/start, commitment discount nahi.

### EBS vs EFS vs S3

**Q: One-sentence decision rule for EBS vs EFS vs S3?**

A: Ek instance ko fast disk chahiye → **EBS**. Many instances ko concurrently same files chahiye → **EFS** (ya Windows par FSx). Unlimited HTTP clients, object access → **S3**.

### EBS Volumes

**Q: What single fact about EBS explains most of its behavior, and what are the key properties?**

A: EBS ek **network drive** hai, physical disk nahi — isliye network latency hoti hai, aur yeh detach/reattach ho sakta hai aur instance se survive karta hai. Properties: **ek AZ** mein locked (snapshot+recreate se move karo); ek time mein ek instance (Multi-Attach ko chhod ke); provisioned capacity usage chahe kuch bhi ho, billed hoti hai; online resize (sirf grow, kabhi shrink nahi); `DeleteOnTermination` default **root ke liye true, additional volumes ke liye false** hai — ek real cost/data-loss trap.

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

A: (1) Jo device name aap request karte ho (`/dev/sdf`) wo Linux Nitro instances par use nahi hota (`/dev/nvme1n1`) — hamesha pehle `lsblk` karo, kyunki galat device par `mkfs` chalane se root volume destroy ho jata hai:

```
NAME          SIZE TYPE MOUNTPOINTS
nvme0n1         8G disk
└─nvme0n1p1     8G part /          <- root volume, already mounted
nvme1n1       100G disk            <- the volume I just attached
```

NVMe numbering reboots ke across bhi stable nahi hai — ek device ko real volume ID se map karo hardcode karne ke bajaye:

```bash
sudo nvme id-ctrl -v /dev/nvme1n1 | grep -i sn    # serial = the EBS volume ID (vol-0abc…)
ls -l /dev/disk/by-id/                            # stable nvme-Amazon_Elastic_Block_Store_vol… symlinks
```

(2) `mount` temporary hai — bina ek `/etc/fstab` entry ke (UUID se, `nofail` ke saath), mount reboot par vanish ho jata hai, aur writes silently root disk par jane lagte hain:

```bash
sudo blkid /dev/nvme1n1        # -> UUID="a1b2c3d4-…" TYPE="xfs"
# /etc/fstab
# UUID=a1b2c3d4-…  /data  xfs  defaults,nofail  0  2
sudo umount /data && sudo mount -a && df -h /data   # ← TEST before rebooting
```

### EBS Volume Types

**Q: gp3 vs gp2 — what's the headline improvement, and which volume types can be boot volumes?**

A: gp3 **IOPS ko size se decouple** karta hai (baseline 3,000 IOPS/125MB/s size chahe kuch bhi ho); gp2 mein sirf IOPS pane ke liye size over-provision karni padti thi (3 IOPS/GB). Sirf SSD types (gp2/gp3/io1/io2) boot volumes ho sakte hain — HDD types (st1/sc1) nahi ho sakte.

### EBS Snapshots

**Q: How are EBS snapshots incremental, and what's the standard DR move?**

A: Pehla snapshot saare used blocks copy karta hai; baad wale snapshots sirf changed blocks copy karte hain; ek purana snapshot delete karna kabhi ek newer snapshot ko break nahi karta. Snapshots region-scoped hote hain lekin AZ-independent — ek snapshot ko dusre region mein copy karna standard EBS DR move hai.

```bash
aws ec2 create-snapshot --volume-id vol-abc --description "pre-upgrade 2026-08-08"
aws ec2 copy-snapshot --source-region us-east-1 --source-snapshot-id snap-abc \
  --destination-region us-west-2 --encrypted          # DR copy
aws ec2 create-volume --snapshot-id snap-abc --availability-zone us-east-1b --volume-type gp3
```

**Q: What automates snapshot lifecycle, and what's the commonly forgotten cost leak?**

A: **Data Lifecycle Manager (DLM)** scheduled creation/retention automate karta hai (cross-service version ke liye AWS Backup). Cost leak: decommissioned volumes se years ke nightly snapshots unnoticed pile up ho jate hain.

### AMIs (Amazon Machine Images)

**Q: What's the distinction between an EBS snapshot and an AMI?**

A: Ek snapshot ek **disk** backup karta hai; ek AMI ek **bootable machine** backup karta hai (root+additional volume snapshots plus block-device mapping aur launch permissions). AMIs region-scoped hote hain — har region mein copy karna zaroori hai jaha launch karna ho.

**Q: What's a silent cost leak with AMIs?**

A: Ek AMI deregister karna uske underlying snapshots delete **nahi** karta — woh manually cleanup hone tak cost karte rehte hain.

```bash
aws ec2 create-image --instance-id i-abc --name "dotnet8-base-2026-08" --description "golden AMI"
aws ec2 copy-image --source-region us-east-1 --source-image-id ami-abc --region us-west-2 --name "dotnet8-base"
```

#### The Golden AMI Pattern — Full Explanation

**Q: Quantify why baking dependencies into a golden AMI matters for Auto Scaling.**

A: Thick user data: launch→traffic serve karna 4–6 min. Thin user data (golden AMI): 60–90s. Auto Scaling sirf useful hai agar fast ho — ek 5–6 minute gap ka matlab existing instances timeouts serve karte hain jabki naye boot ho rahe hote hain, aur aap hamesha us traffic ke liye scale kar rahe ho jo aapko minutes pehle thi.

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

A: Agar ek external repo thick user-data boot ke dauraan briefly unreachable ho jaye, install fail ho jata hai lekin instance abhi bhi "successfully" boot hota hai bina runtime ke → app kabhi start nahi hoti → health check fail hota hai → ASG usko terminate aur replace kar deta hai → same failure forever repeat hota hai, zero healthy capacity. Baking external dependencies ko boot path se entirely remove kar deta hai.

**Q: What does EC2 Image Builder automate, and what's the underrated pipeline stage?**

A: Recipe (base AMI + components) → Build (temp instance, components run karo, snapshot lo) → **Test** (naya AMI boot karo, smoke tests run karo, agar broken hai toh pipeline fail karo — Inspector ke saath CVE scanning ke liye integrate karta hai) → Distribute (regions/accounts mein copy karo) → Schedule (monthly/CVE-triggered rebuilds). Test phase underrated hai — yeh ek broken image ko launch template tak pahunchne se rokta hai.

```
1. RECIPE       base AMI + components: patch OS - install runtime - install agents - CIS hardening
2. BUILD        spins up a temp instance, runs the components, snapshots it
3. TEST         boots the new AMI and runs smoke tests — fails the pipeline if broken
4. DISTRIBUTE   copies the AMI to every region, shares it to every account
5. SCHEDULE     re-runs monthly, or on a critical CVE, so patches actually land
```

**Q: What's the mindset shift from patching servers to "immutable infrastructure"?**

A: Servers ko in-place patch karna band karo aur unhe **replace** karna shuru karo — ek naya AMI banao, instance refresh ke through rollout karo, aur jo peeche reh gaya usko clean up karo:

```
Image Builder produces AMI v43 -> update the launch template ->
ASG instance refresh (rolling replacement) -> deregister old AMIs AND delete their snapshots
```

### Instance Store

**Q: What's the one-liner distinguishing instance store from EBS?**

A: "Instance store fastest aur least durable option hai — EBS ek network drive hai jo instance se outlive karta hai, instance store local hardware hai jo nahi karta." Stop/hibernate/terminate/host failure par data lost ho jata hai (sirf reboot survive karta hai); scratch/cache/self-replicating distributed DBs ke liye correct hai, kabhi bhi irreplaceable kisi cheez ke liye nahi.

### EBS Multi-Attach

**Q: What's the point everyone misses about EBS Multi-Attach?**

A: Ek normal filesystem (ext4/XFS/NTFS) **khud ko corrupt** kar lega agar do instances ek saath same volume mount kar lein — Multi-Attach sirf ek cluster-aware filesystem (GFS2, OCFS2) ke saath ya ek app jo apna khud ka raw block locking manage karti hai, uske saath kaam karta hai. Yeh general shared-storage ka answer nahi hai (wo EFS hai) — yeh clustered HA apps jaise Oracle RAC ke liye hai jinko concurrent raw block access chahiye. Limits: io1/io2 only, ek AZ, 16 Nitro instances max.

### EBS Encryption

**Q: What does EBS encryption cover, and how do you encrypt an existing unencrypted volume?**

A: Data at rest cover karta hai, instance aur volume ke beech data in transit, saare snapshots, aur un snapshots se banaye gaye saare volumes (encryption propagate hoti hai). Existing data encrypt karne ke liye: unencrypted volume ka snapshot lo → snapshot ko `--encrypted` + ek KMS key ke saath **copy** karo (encryption copy step par introduce hoti hai) → encrypted snapshot se ek naya volume banao → usko instance par swap karo.

### EFS (Elastic File System)

**Q: Why can't EFS serve a Windows .NET Framework app's shared storage need, and what's the alternative?**

A: EFS POSIX/NFSv4.1 hai, **Linux only** — Windows support nahi. Windows ke liye, **FSx for Windows File Server** use karo (SMB, AD-integrated); high-performance Linux/HPC ke liye, FSx for Lustre.

**Q: Why isn't EFS the default choice even though it's more flexible than EBS?**

A: Yeh roughly **gp3 se 3× per GB** cost karta hai — sirf isko use karo jab aapko genuinely concurrent multi-AZ shared access chahiye ho, default ke roop mein nahi. Bursting throughput mode credit trap bhi dekho (T-family CPU credits jaisi hi shape) — Elastic mode spiky/unknown workloads ke liye safer default hai.

```bash
sudo mount -t efs -o tls fs-0123456789abcdef:/ /mnt/efs      # amazon-efs-utils, TLS in transit
```

### EFS vs EBS vs Instance Store

**Q: What's the one decision sentence covering all three storage types?**

A: "Ek instance ko fast disk chahiye → EBS. Many instances ko ek saath same files chahiye → EFS (ya Windows par FSx). Mujhe maximum speed chahiye aur data rebuild kar sakta hu → instance store."

### EC2 Storage Shared Responsibility Model

**Q: In the EC2 storage shared-responsibility split, what's on you vs AWS?**

A: AWS: EBS/EFS infrastructure ki durability/replication, failed hardware replace karna, encryption capability provide karna. Aap: snapshot/AMI restores lena aur test karna, volume type/sizing IOPS choose karna, encryption **enable** karna aur keys manage karna, EFS mount targets (NFS 2049) par security groups, aur yeh jaanna ki instance store ephemeral hai.

---

## Observability & Monitoring

### CloudWatch Deep Dive

**Q: What's the Logs hierarchy, and what does Logs Insights let you do?**

A: Log Group → Log Streams → Log Events (jaise `/aws/lambda/ProcessOrder`, per container instance ek stream). **Logs Insights** logs ke upar ek SQL-like query language hai (`fields ... | filter ... | sort ...`); structured JSON logging fields ko directly queryable banata hai.

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

A: Existing metrics ko ek derived formula mein combine karta hai **CloudWatch ke andar hi**, koi extra instrumentation nahi — jaise, `(m1/m2)*100` ek 5xx error-**rate** percentage ke liye do raw-count graphs eyeball karne ke bajaye. "Aap raw count ke bajaye ek rate par kaise alarm karte ho" ka answer, kyunki varying traffic volumes par raw counts mislead karte hain.

**Q: CloudWatch vs CloudTrail — the classic trick question answer?**

A: CloudWatch = observability (logs/metrics/alarms **behavior/performance** ke baare mein). CloudTrail = governance/audit (kaun si API kisne call ki, kab). Different questions hain, interchangeable nahi.

**Q: What is Embedded Metric Format (EMF)?**

A: Ek structured JSON log format jise CloudWatch automatically metrics mein extract kar leta hai — Lambda se high-cardinality custom metrics ke liye useful hai extra `PutMetricData` calls aur unke API cost/throttling ke bina.

### CloudWatch vs X-Ray: Complementary, Not Competing

**Q: CloudWatch vs X-Ray — what question does each answer, and how do they combine in practice?**

A: CloudWatch answer karta hai "kuch galat hai kya, aur aggregate kaisa dikhta hai" (logs/metrics, service-level). X-Ray answer karta hai "exactly kaha is specific request ke path mein yeh galat/slow gaya" (traces, request-level, cross-service). Practice mein: ek CloudWatch alarm elevated p99/error rate par fire hota hai → structured log se X-Ray trace ID pull karo → us trace ko open karo dekhne ke liye ki exactly kaunsi downstream call ne latency add ki. Inhe wire karo ek shared trace/correlation ID structured logs mein.

### CloudTrail

**Q: Management events vs Data events vs Insights events — which are logged by default, and what's the gotcha?**

A: **Management** (control-plane: `RunInstances`, `AssumeRole`) — default se logged, free, 90-day Event History. **Data** (data-plane: S3 `GetObject`/`DeleteObject`, Lambda `Invoke`) — **default se logged nahi**, extra cost karta hai. **Insights** (ML-detected unusual activity) — opt-in. Gotcha: "us S3 object ko kisne delete kiya" default CloudTrail se unanswerable hai kyunki object-level deletes data events hain, default se off — incident se *pehle* enable hona chahiye.

**Q: Event History vs a Trail — what's the difference, and what's an Organization trail?**

A: Event History = 90 din, in-console, free. Ek **Trail** events ko S3 mein deliver karta hai indefinite retention ke liye (optionally CloudWatch Logs mein bhi alarms ke liye). **Organization trail** AWS Org ke har account ko ek central bucket mein capture karta hai — standard multi-account audit design, typically ek locked-down log-archive account mein.

**Q: Is CloudTrail real-time? What's the alternative for immediate reaction?**

A: Nahi — delivery ~15 minutes tak lag kar sakti hai. Immediate reaction ke liye, CloudTrail poll karne ke bajaye event pattern par **EventBridge** rules use karo.

**Q: Three-way comparison — CloudWatch vs CloudTrail vs AWS Config, one question each?**

A: CloudWatch — "yeh kaisa perform kar raha hai?" CloudTrail — "kisne kya kiya, kab?" AWS Config — "configuration kaisa dikhta hai, aur kya drift hua?"

### AWS Health Dashboard

**Q: Service Health Dashboard vs Account Health Dashboard — what's the difference?**

A: **Service Health Dashboard** — public, saare AWS services/regions ke liye generic status page, aapke resources ke baare mein kuch nahi batata. **Account Health Dashboard** — *aapke* resources ko affect karne wale personalized events (degraded hardware, scheduled maintenance, EOL notices). **Health API** ko EventBridge mein wire karo responses automate karne ke liye (jaise, ek forced retirement se pehle auto-drain karna ek instance).

### Container Insights, the CloudWatch Agent & Proactive Monitoring

**Q: Why doesn't a default CloudWatch memory alarm work on an EC2 instance?**

A: Default hypervisor-level EC2 metrics CPU/network/disk I/O cover karte hain lekin **memory ya filesystem free space nahi** — unko guest OS ke andar visibility chahiye **CloudWatch Agent** ke through (SSM se install, ideally AMI mein baked). "CloudWatch memory metric" answer dena agent ke bina ek classic wrong answer hai.

**Q: What's the difference between Container Insights, Lambda Insights, Synthetics, and RUM?**

A: **Container Insights** — ECS/EKS ke liye cluster/service/task/pod metrics. **Lambda Insights** — per-invocation memory/CPU/init duration. **Synthetics (canaries)** — scripted external checks jo outage ko user report karne se pehle catch karte hain. **RUM** — actual browsers se real-user monitoring (load times, JS errors, Core Web Vitals) — Synthetics ke saath pairs karta hai (RUM = users kya experience karte hain, Synthetics = ek known-good request kya experience karta hai).

**Q: What two alarm-design details matter beyond just picking a threshold?**

A: `--treat-missing-data` deliberately set karo (ek metric jo publishing band kar deta hai kyunki service *down* hai, alarm ko warna forever `INSUFFICIENT_DATA` mein stuck chhod deta hai); raw counts ke bajaye **Metric Math** ke through rates par alarm karo, kyunki traffic volume swing hone par raw counts mislead karte hain.

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

A: Containers ke liye ek serverless compute engine — ECS/EKS ke liye ek launch type, khud ek orchestrator nahi; aap ek image + CPU/memory supply karte ho, AWS servers/scaling/patching handle karta hai, per vCPU-second + GB-second billed hota hai zero idle cost ke saath. Sizing **valid vCPU/memory pairs** tak constrained hai (jaise 1 vCPU → 2–8GB) — ek arbitrary combination reject ho jata hai.

**Q: What's Fargate's ephemeral storage limit, and how do you get persistent/shared storage?**

A: 20GB default, 200GB tak, lekin ephemeral — task stop hone par gone. Persistence ya tasks ke beech sharing ke liye, **EFS** mount karo.

**Q: What's Fargate Spot, and how do you blend it with standard Fargate?**

A: EC2 Spot jaisa hi interruption model (~70% cheaper, 2-minute SIGTERM warning). Ek **capacity provider strategy** unhe per service mix karta hai, jaise "2 tasks always on FARGATE, upar sab kuch FARGATE_SPOT par" — baseline reliability cheap burst ke saath, agar app SIGTERM handle kare (exec-form ENTRYPOINT chahiye).

**Q: Is "Fargate is always cheaper than EC2" true?**

A: Nahi — false. Fargate bursty/low-utilization workloads ke liye jeeta hai; EC2 steady, high-utilization workloads ke liye jeeta hai (jo capacity aap anyway use karte, uspar koi per-task premium nahi). Rule of thumb: ek 24×7 steady 1vCPU/2GB service EC2 par cheaper hai; ek job jo 2h/day same size par chalti hai Fargate par cheaper hai.

### EC2 vs Fargate Cost & Trap Scenarios

**Q: Rapid-fire — CPU is low but the app is slow; a Fargate task can't reach the internet; works in dev fails in prod. What's the likely cause of each?**

A: Low CPU lekin slow app → bottleneck likely disk I/O, network latency, ya ek single-threaded hot path hai, CPU nahi. Fargate internet tak nahi pahunch sakta → private subnet me NAT Gateway/VPC endpoint nahi hai. Dev mein kaam karta hai, prod mein fail → almost hamesha IAM role, secrets, ya networking (subnet/SG) differences environments ke beech.

**Q: What's the final mental model for choosing EC2 vs Fargate?**

A: Steady load → EC2. Bursty load → Fargate. Control chahiye → EC2. Simplicity chahiye → Fargate. Idle-cost sensitive → Fargate. High constant utilization → EC2.

### Docker & Container Fundamentals

**Q: VM vs Container — what's the core isolation difference?**

A: Ek VM ka apna full OS/kernel hota hai ek hypervisor ke through (GBs, boot hone mein minutes, low density, strong isolation). Ek container **host kernel share karta hai** (MBs, seconds mein start, high density, weaker kernel-level isolation lekin far cheaper/faster).

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

Multi-stage SDK ko shipped image se bahar rakhta hai; source se pehle `.csproj` copy karna matlab `dotnet restore` cached ho jata hai aur sirf tab rerun hota hai jab dependencies change hoti hain — jo layers sabse kam change hoti hain, woh pehle jaati hain.

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

A: Ek **tag** ek movable label hai (`myapi:1.4.2` ko kal different bits par repoint kiya ja sakta hai); ek **digest** ek immutable content hash hai. Reproducibility ke liye digest ya ek immutable tag se deploy karo — `:latest` ek anti-pattern hai kyunki "previous latest par rollback karo" jaisa kuch exist nahi karta.

**Q: What does `.dockerignore` actually solve, and what's a typical one for a .NET app?**

A: Iske bina, poora build context (`bin/`, `obj/`, `node_modules/`, `.git` including) har build par daemon tak upload hota hai (slow), aur `COPY . .` risk karta hai `appsettings.Development.json`/`.env`/`.aws/credentials` ko image mein bake karne ka (ek real secret leak):

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

A: Har instruction ek read-only layer create karti hai; ek baad wala `RUN rm file` sirf file ko union filesystem mein hide karta hai — yeh abhi bhi earlier layer se extractable hai. `COPY secrets.json . && RUN rm secrets.json` secret ko image mein permanently chhod deta hai — kabhi credentials ko build mein na daalo.

**Q: What's the production-default .NET base image, and why is "no shell" a feature?**

A: `aspnet:8.0-jammy-chiseled` (~110MB, Ubuntu chiselled, no shell, no package manager, non-root by default) — smallest attack surface; ek attacker with RCE ke paas pivot karne ke liye kuch nahi hai. Cost: `docker exec sh` kaam nahi karta, isliye aap logs/metrics ke through debug karte ho.

**Q: ENTRYPOINT vs CMD, and why must you use exec form?**

A: `ENTRYPOINT` = always-run executable (override karna hard hai); `CMD` = default arguments (trivially overridden). Exec form (`["dotnet","MyApi.dll"]`) process ko directly `SIGTERM` receive karne deta hai graceful shutdown ke liye; shell form isko `/bin/sh -c` mein wrap kar deta hai, isliye app kabhi SIGTERM nahi dekhti aur kill ho jati hai — har deploy ko dropped in-flight requests mein badal deta hai.

```dockerfile
ENTRYPOINT ["dotnet", "MyApi.dll"]     # always runs this
CMD ["--environment=Production"]       # default arg; `docker run img --environment=Staging` overrides it
```

**Q: Why does an x86-built image fail on a Graviton instance, and how do you fix it?**

A: Yeh `exec format error` se fail hota hai — architecture mismatch. Fix: `docker buildx build --platform linux/amd64,linux/arm64 ... --push` ek manifest list produce karta hai jo dono architectures ko ek tag se serve karti hai.

```bash
docker buildx build --platform linux/amd64,linux/arm64 -t <ecr>/myapi:1.4.2 --push .
```

### ECS (Elastic Container Service)

**Q: What's the ECS object model, outermost in?**

A: Cluster → Service (desired count) → Task (ek Task Definition ka instance) → Container(s).

```
Cluster  →  Service  →  Task  →  Container(s)
              ↑          ↑
        desired count   instance of a Task Definition
```

**Q: What's the most common real-world ECS IAM mistake?**

A: App ki DynamoDB permission ko **task execution role** mein add karna (ECR pull/logs/secrets ke liye ECS agent use karta hai) **task role** ke bajaye (jo aapke application code use karta hai) — ek inexplicable `AccessDenied` produce karta hai.

**Q: `awsvpc` vs `bridge` vs `host` networking — which is mandatory for Fargate?**

A: `awsvpc` har task ko apna khud ka ENI/private IP/security group deta hai — **Fargate ke liye mandatory**, aur generally preferred mode kyunki yeh per-service SG rules allow karta hai. `bridge`/`host` older EC2-launch-type modes hain.

**Q: What does the deployment circuit breaker do, and why turn it on?**

A: Automatically ek deployment ko rollback karta hai jiske tasks stabilize karne mein fail ho jate hain — ek failed deploy aur ek outage ke beech ka difference.

**Q: `binpack` vs `spread` task placement — what's the cost/resilience trade-off?**

A: `binpack` tasks ko kam se kam instances par pack karta hai (cheapest); `spread` AZs/instances ke across distribute karta hai (most resilient, ek host ya AZ lose karne se survive karta hai). Most teams AZs ke across spread karte hain aur unke andar binpack karte hain.

#### ECS on the EC2 Launch Type

**Q: What's the third ECS IAM role that only exists on the EC2 launch type, and what does forgetting it cause?**

A: **ECS instance role** (`ecsInstanceRole`) — EC2 instance profile se attached, cluster se register hone aur ECR se pull karne ke liye ECS agent use karta hai. User data mein `ECS_CLUSTER=<name>` bhoolna matlab instance `default` cluster mein register ho jata hai instead — aapka real cluster 0 container instances dikhata hai jabki sab EC2 mein healthy dikh raha hota hai.

```bash
#!/bin/bash
echo "ECS_CLUSTER=prod-cluster" >> /etc/ecs/ecs.config
```

**Q: What is Capacity Provider managed scaling, and what's the target-capacity trade-off?**

A: Ek capacity provider ek ASG ko wrap karta hai; ECS `CapacityProviderReservation` publish karta hai aur usko target-track karta hai, ASG ko grow karta hai jab tasks place nahi ho pate. Target capacity `100` = "scale jab tak tasks just fit karein" (cheapest, naye tasks place karne mein slowest); 100 se neeche immediate placement ke liye warm headroom rakhta hai. **Managed termination protection on hona chahiye**, warna ASG running tasks wale instance ko terminate kar sakta hai.

#### How EC2, ASG, Capacity Provider, Cluster, Service & Tasks Fit Together

**Q: What are the two independent ECS scaling loops, and what happens if you only wire up one?**

A: **Loop 1 (Service Auto Scaling)** application demand ke basis par tasks scale karta hai. **Loop 2 (Cluster Auto Scaling)** capacity provider ke through task-placement pressure ke basis par EC2 instances scale karta hai. Loop 2 missing hona matlab tasks forever `PENDING` baithe rehte hain kaha run karne ki jagah nahi hoti; Loop 1 missing hona matlab traffic rise hoti hai lekin task count flat rehta hai. Fargate par, Loop 2 exist nahi karta — AWS directly capacity supply karta hai.

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

A: `(M/N)×100` jaha N = ASG mein currently instances, M = instances jo ECS calculate karta hai usko chahiye. M>N → 100 se upar → scale out; M<N → 100 se neeche → scale in.

```
CapacityProviderReservation = (M / N) x 100

  N = instances currently running in the ASG
  M = instances ECS calculates it NEEDS for running + pending tasks
```

**Q: What causes "unable to place a task because no container instance met all of its requirements"?**

A: Check karne ka order: kisi instance par enough remaining CPU/memory nahi; ek host port conflict; koi instance jo placement constraint satisfy na kare; `awsvpc` mode mein per-instance ENI limit reach ho gaya.

**Q: What's dynamic port mapping, and what security group rule does it require?**

A: `bridge` mode mein, `hostPort: 0` Docker ko ek random ephemeral host port assign karne deta hai, aur ECS us specific port ko ALB target group ke saath register karta hai — ek instance par same container ki kai copies chalne deta hai. Instance SG ko **ephemeral range 32768–65535** ALB ke SG se allow karna zaroori hai, sirf port 80 nahi.

**Q: Container instance draining vs ALB connection draining — how do they differ, and why do you need both?**

A: **Container instance draining** (`DRAINING` state) naye task placement rokta hai aur instance termination se pehle running tasks relocate karta hai — ek ASG lifecycle hook se wired. **ALB deregistration delay** in-flight HTTP requests ko finish hone deta hai. Ek graceful deploy ko dono chahiye: draining tasks move karta hai, deregistration delay unke requests complete hone deta hai.

**Q: ECS vs EKS in one line?**

A: ECS simpler hai, AWS-proprietary, free control plane — default choice. EKS managed Kubernetes hai — isko choose karo multi-cloud portability, existing K8s investment, ya CNCF ecosystem ke liye, added complexity aur control-plane cost accept karke.

### ECR (Elastic Container Registry)

**Q: Basic vs enhanced ECR image scanning — which is right for production?**

A: Basic push par ek baar ek CVE database ke against scan karta hai. **Enhanced scanning** Amazon Inspector use karta hai OS aur language-package dependencies ke continuous rescanning ke liye jab naye CVEs publish hote hain — production ke liye right answer.

```bash
aws ecr get-login-password --region us-east-1 | docker login --username AWS \
  --password-stdin 123456789012.dkr.ecr.us-east-1.amazonaws.com
docker tag myapi:1.0 123456789012.dkr.ecr.us-east-1.amazonaws.com/myapi:1.0
docker push          123456789012.dkr.ecr.us-east-1.amazonaws.com/myapi:1.0
```

**Q: Why turn on ECR tag immutability, and what's the pull-through cache for?**

A: Tag immutability `myapi:1.0` ko silently different bits ban jane se rokta hai, reproducible rollback preserve karta hai. **Pull-through cache** upstream public images (Docker Hub, MCR) ko aapke registry mein cache karta hai, CI mein Docker Hub rate limits avoid karta hai.

### Hands-On: ECS with Fargate

**Q: What's the debugging order when ECS tasks won't start?**

A: `describe-services` events pehle, phir stopped task ka `stoppedReason`. Usual causes: task execution role ECR se pull/logs write nahi kar sakta; image architecture mismatch (ARM vs x86); health check app finish start hone se pehle fail hota hai; private subnet se ECR tak koi route nahi (NAT ya VPC endpoints chahiye); ek missing env var se container exit ho jata hai.

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

A: Naye .NET microservices ko default **ECS on Fargate** par bhejo (best simplicity/control balance). Event-driven glue aur spiky/idle-heavy work ke liye **Lambda** use karo. **EKS** tak sirf pahuncho jab ek existing organizational Kubernetes investment ho. **EC2** (Windows containers/full Windows Server) sirf un .NET Framework workloads ke liye use karo jo .NET Core/8+ mein port nahi ho sakte, ya GPU/specialized hardware needs. Agar "green-field team ke liye jisko koi K8s experience nahi hai, EKS kyu nahi" par push kiya jaye — correct answer abhi bhi na hai, jab tak koi concrete multi-cloud/portability requirement na ho jo learning curve justify kare.

**Q: Does Lambda support .NET Framework (not Core)?**

A: Nahi — Lambda ko .NET Core/5+ chahiye. ECS EC2 launch type (Windows containers) ya directly EC2 .NET Framework/IIS lift-and-shift support karte hain; Fargate EC2 ki tarah Windows-launch-type .NET Framework support nahi karta.

### Fargate/ECS/EKS Trade-offs — Reasoning Without Hands-On Time

**Q: How would you honestly frame a lack of hands-on Fargate/ECS/EKS production experience in an interview?**

A: Directly state karo: hands-on AWS provisioning experience Lambda/DynamoDB/EC2/S3 via Terraform/CDKTF hai — container orchestrator trade-offs ke through reasoning conceptual hai, operational experience claim nahi kiya ja raha. Phir decision flow walk karo (existing K8s/multi-cloud need? → EKS; OS/GPU/legacy need? → EC2; spiky/event-driven? → Lambda; else → ECS on Fargate) depth bluff karne ke bajaye.

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

A: Kubernetes ka biggest selling point (portability, Helm/operators/service-mesh ecosystem) uska biggest cost bhi hai — ek real control-plane learning curve (CRDs, RBAC, CNI, admission controllers) jo ECS/Fargate users kabhi pay nahi karte. Bina ek concrete multi-cloud requirement ya existing K8s-fluent platform team ke, wo operational tax usually justified nahi hota.

### Deploying .NET to AWS: Elastic Beanstalk vs ECS vs Lambda Custom Runtime

**Q: What is Elastic Beanstalk really, under the hood — and what's the senior-vs-mid-level distinction to make?**

A: Beanstalk **ek separate compute primitive nahi hai** — yeh abhi bhi underneath EC2+ASG+ELB (ya Docker platform ke liye ECS) provision karta hai. Value-add deployment/orchestration tooling hai (`eb deploy`, rolling/immutable/blue-green environment configs), naya runtime nahi. Yeh naam lena (orchestration layer, naya infrastructure nahi) senior-level distinction hai.

**Q: Compare deployment strategies across Beanstalk, ECS, and Lambda.**

A: **Beanstalk** — rolling, rolling-with-additional-batch, immutable, ya blue/green (CNAME swap). **ECS** — service deployment config se rolling update, ya CodeDeploy + two target groups se blue/green. **Lambda** — versions + aliases with linear/canary shifting CodeDeploy ke through.

---
## Relational Databases, Caching & Analytics

### RDS Multi-AZ vs Read Replicas vs Aurora

**Q: Multi-AZ vs Read Replica ke baare mein core interview nuance kya hai jo land karna chahiye?**

A: Multi-AZ **availability** ke liye hai, scalability ke liye nahi — classic standby traffic serve nahi karta. Read replicas **reads scale karne** ke liye hain, HA ke liye nahi — promotion manual hai aur replication break kar deta hai, isliye yeh koi DR plan nahi hai. Dono ko confuse karna ek bahut common junior-level confusion hai.

**Q: Lambda-to-RDS ko specifically RDS Proxy ki zarurat kyun hoti hai?**

A: Har concurrent Lambda execution environment warna apna khud ka DB connection kholega, jisse burst concurrency ke under database ki `max_connections` limit exhaust ho jaati hai — RDS Proxy unhe pool/multiplex karta hai.

### Multi-AZ vs Read Replica — Sabse Zyada Confused Pair

**Q: Multi-AZ ko Read Replica se distinguish karne wala drill answer verbatim recite karo.**

A: "Multi-AZ failure se survive karne ke baare mein hai — ek synchronous standby jise AWS automatically fail over karta hai, lekin classic form mein yeh read traffic serve nahi karta, isliye yeh scaling ke liye kuch nahi karta. Read Replicas reads scale karne ke baare mein hain — asynchronous copies jinhe aap reporting/read traffic route karte ho, lekin ek ko primary mein promote karna manual aur replication-breaking hai, isliye yeh real HA ka substitute nahi hai. Ek read replica ko apna DR plan use karna, ya ek Multi-AZ standby se read load absorb karne ki expectation rakhna, same category ki mistake hain."

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

**Q: Newer Multi-AZ DB Cluster feature ke saath kya change hua?**

A: Classic Multi-AZ (standby readable nahi) ke unlike, Multi-AZ DB Cluster standby par **readable reader endpoints** add karta hai — ek common "gotcha, that changed" interview follow-up.

**Q: Aurora ki standard RDS se key architectural difference kya hai, aur yeh replica lag ke liye kyun matter karta hai?**

A: Aurora compute ko ek shared, distributed, auto-scaling storage volume se separate karta hai jo AZs ke across storage layer par replicated hota hai — full instances ke beech logs ship karke nahi. Yahi wajah hai ki Aurora replica lag typically standard RDS read-replica lag se kaafi kam hota hai (aksar sub-10-seconds, kabhi kabhi near-instant).

**Q: Aurora ke bajaye standard RDS kab pick karoge?**

A: Aurora sirf MySQL/PostgreSQL-compatible hai — yeh SQL Server **support nahi karta**. SQL Server-based .NET shops ke liye, ya jab Aurora ka cost premium workload ki availability/scale needs se justify nahi hota, standard RDS hi sahi call rehta hai.

### Databases & Analytics Overview: Sahi Store Choose Karna

**Q: "Aap kaunsa database pick karoge" poochne par kaunsi framing se lead karna chahiye?**

A: OLTP vs OLAP. OLTP = kaafi small concurrent indexed reads/writes (order-entry API → RDS/DynamoDB). OLAP = kam large scans/aggregations history ke upar (revenue dashboard → Redshift/Athena). Apne OLTP primary ke against analytical queries run karna classic mistake hai — light reporting ke liye read replica ya real analytics ke liye ek proper warehouse/lake se fix karo.

**Q: Redshift, OpenSearch, Neptune, Timestream, aur QLDB ke liye one-line purpose?**

A: **Redshift** — TB–PB BI queries ke liye OLAP warehouse. **OpenSearch** — full-text search/log analytics. **Neptune** — graph, jab relationships primary query hon. **Timestream** — rollups/retention tiers ke saath time-series IoT/metrics. **QLDB** — cryptographically verifiable immutable ledger.

### Relational Databases & RDS — Operational Surface

**Q: Standard RDS ki sabse badi limitation kya hai, aur usse escape kab karna padta hai?**

A: **Koi OS/shell access bilkul nahi** — aap ek agent ya custom extension install nahi kar sakte. **RDS Custom** (sirf Oracle/SQL Server) ke through ya EC2 par engine self-manage karke escape karo.

**Q: Automated backups vs manual snapshots — key difference kya hai?**

A: Automated backups (daily full + continuous transaction logs, jo 1–35 day retention window ke andar kisi bhi second tak PITR enable karte hain) **instance delete karne par delete ho jaate hain** jab tak aap final snapshot na lein. Manual snapshots tab tak rakhe jaate hain jab tak *aap* unhe delete na karo aur yeh accounts/regions ke across shareable hote hain.

**Q: IAM database authentication kya hai, aur "aap database passwords se kaise bachte ho" ka yeh strong answer kyun hai?**

A: Ek application ko stored password ke bajaye ek short-lived IAM token se authenticate karne deta hai — ek EC2/Lambda role ke saath naturally pairs karta hai. Doosra strong answer rotation ke saath Secrets Manager hai.

#### RDS Custom — Oracle & SQL Server Ke Liye

**Q: RDS Custom kaunsi problem solve karta hai, aur uska distinctive risk kya hai?**

A: Standard RDS koi OS access/DB superuser nahi deta, jo SSIS/SSRS/CLR assemblies (SQL Server) ya Data Guard/APEX (Oracle) jaisi cheezein block karta hai. RDS Custom OS + superuser access deta hai jabki AWS abhi bhi backups/PITR/Multi-AZ **conditionally** handle karta hai — lekin supported configuration break karna (agent, storage layout, IAM permissions alter karna) instance ko `unsupported-configuration` mein move kar deta hai: automation stop ho jaata hai aur fix karna aapka job hai. "Managed until you break it."

**Q: RDS Custom setup prerequisites kya hain?**

A: IAM instance profile, artifacts ke liye ek S3 bucket, aur ek **customer-managed KMS key** (AWS-managed key kaam nahi karti); SQL Server/Oracle exact build pin karne ke liye Custom Engine Version (CEV) use karte hain.

#### RDS Security — Consolidated

**Q: "RDS ko kaise secure karte ho?" ka one-liner answer do.**

A: "Private subnets aur SG-to-SG rules taaki yeh internet se unreachable ho; creation par KMS aur forced TLS taaki data dono taraf encrypted rahe; koi config mein password na ho isliye IAM database auth ya Secrets Manager; andar least-privilege database users; aur CloudWatch mein Database Activity Streams plus engine logs taaki privileged access auditable ho. CloudTrail mujhe batata hai ki instance kisne change kiya — yeh nahi batata ki query kisne run ki."

**Q: RDS ke liye sabse strongest authentication option kya hai, aur uska caveat?**

A: **IAM database authentication** — app ke role se ek 15-minute token, koi stored password nahi. Caveat: iski connection-rate limit hai, isliye high-churn/Lambda workloads ke liye **RDS Proxy** ke saath pair karo.

**Q: Aap *privileged* database access (full rights wale ek DBA) ko kaise audit karte ho, sirf API calls ke opposite mein?**

A: **Database Activity Streams** (Aurora, plus RDS for Oracle/SQL Server) — Kinesis ko ek near-real-time, tamper-resistant stream jise ek full-privilege DBA bhi erase nahi kar sakta. CloudTrail sirf API (`CreateDBInstance`) log karta hai, jo SQL run hua use nahi.

### Athena

**Q: Athena kya hai, aur kaunse teen levers uska per-TB-scanned cost dramatically cut karte hain?**

A: S3 data ke upar directly Serverless SQL (under the hood Presto/Trino), per TB scanned billed hota hai (~$5/TB). Cost cut karo (1) **columnar formats** (Parquet/ORC vs CSV/JSON — sirf selected columns read karta hai), (2) **partitioning** (`year=2026/month=08/...` taaki ek WHERE clause irrelevant data skip kare; partition projection metadata lookup avoid karta hai), (3) **compression** + `SELECT *` avoid karna.

**Q: Athena vs Redshift — kab kaunsa pick karoge?**

A: Athena — serverless, pay-per-query, data lake ke upar ad-hoc/infrequent analysis, koi ETL nahi. Redshift — provisioned/serverless warehouse, frequent/complex/high-concurrency BI jahan sustained volume ek cluster ko cheaper aur faster banata hai. "S3 logs ke upar occasional queries → Athena; ek dashboard jise sau analysts din bhar hit karte hain → Redshift."

### RDS Proxy

**Q: Lambda + RDS proxy ke bina concretely kyun break hota hai?**

A: Har concurrent Lambda execution environment apna khud ka process hai apne khud ke private connection pool ke saath — ek Lambda ke andar 100 ka pool meaningless hai kyunki usse kabhi bhi sirf 1 connection ki zarurat padti hai, lekin ab N separate pools hain jahan N = concurrency. 500 concurrent executions → 500 connections ek `db.t3.medium` par ~420 ki `max_connections` ceiling ke against → `FATAL: too many connections`, plus connect/TLS-handshake/disconnect churn par CPU waste.

```
CONCURRENCY 500  ->  500 execution environments  ->  500 separate connections
db.t3.medium PostgreSQL max_connections ≈ 420
                        ↓
   FATAL: too many connections / remaining connection slots are reserved
```

**Q: RDS Proxy actually aapko kya deta hai?**

A: Connections ko pool/multiplex karta hai taaki sau clients kam real DB connections share karein; failover time ~66% cut karta hai (held-open connections ko re-point karta hai clients ke reconnect karne ke bajaye); Secrets Manager se credentials pull karke IAM auth enforce karta hai (config mein no password); aapke VPC ke andar run karta hai, kabhi public nahi.

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

**Q: Connection pinning kya hai, aur yeh proxy ko kyun undermine karta hai?**

A: Agar ek session kuch session-scoped karti hai (explicit transactions held open, `SET` session variables, temp tables, advisory locks, MySQL par `USE database`), to proxy ko us client ko session ke baaki hisse ke liye ek real DB connection par pin karna padta hai, jisse multiplexing benefit lost ho jaata hai. `DatabaseConnectionsCurrentlySessionPinned` watch karo — agar high ho, to application fix karo (short transactions, no session state), proxy config nahi.

**Q: RDS Proxy setup requirements kya hain, aur jab yeh sahi tool na ho to iske alternatives?**

A: Lambda ko proxy ke same VPC mein **VPC-attached** hona chahiye (default internet access lose ho jaata hai — NAT/VPC endpoints chahiye); proxy ko ek Secrets Manager secret + IAM role chahiye. Alternatives: **DynamoDB** (koi connection concept bilkul nahi, jab data model allow kare to strongest answer); **Aurora Data API** (HTTPS/IAM endpoint, zero connection management, lekin higher per-query latency); ek single long-lived ECS/EC2 service ka apna khud ka in-process pool already sufficient hai.

IAM database authentication password ko app se poori tarah remove karta hai — Lambda ke execution role ko `rds-db:connect` grant karo aur ek token generate karo instead:

```csharp
var token = RDSAuthTokenGenerator.GenerateAuthToken(
    "my-proxy.proxy-abc123.us-east-1.rds.amazonaws.com", 5432, "app_user");
// use the token as the password; combine with SSL Mode=Require
```

**Q: Jab ek interviewer poochta hai "Lambda + RDS aksar anti-pattern kyun hai", tab kya test kiya ja raha hai?**

A: Kya aap samajhte ho ki Lambda ka concurrency model **processes hai, threads nahi** — connection count concurrency ke saath scale karta hai aur koi shared pool nahi hota — sirf "RDS Proxy" ko magic fix ke roop mein naam lene ke bajaye.

### Aurora Advanced Features

**Q: Aurora ka storage replication kaise kaam karta hai, aur woh kaunsi failure ko no availability impact ke saath tolerate kar sakta hai?**

A: **6 copies, 3 AZs ke across** — writes ke liye 2 copies aur reads ke liye 3 lose hone ko no availability impact ke saath tolerate karta hai, bad blocks ko self-heal karta hai.

**Q: Writer endpoint vs reader endpoint vs custom endpoint — har ek kis liye hai?**

A: **Writer** — hamesha current primary ko point karta hai, failover transparent. **Reader** — replicas ke across load-balance karta hai. **Custom** — ek chosen subset target karta hai, jaise heavy reporting queries ko bigger replicas par route karna taaki API ke replicas affect na hon.

**Q: Aurora Serverless v2, Global Database, aur Backtrack har ek kya solve karta hai?**

A: **Serverless v2** — capacity ko fine-grained ACUs mein ek second se kam mein scale karta hai, spiky/dev-test workloads ke liye. **Global Database** — 1 primary + up to 5 read-only secondary regions, <1s typical replication, <1min cross-region failover — Warm Standby/Active-Active DR ke peeche ka engine. **Backtrack** — restore ke bina cluster ko in place mein ek point in time par rewind karta hai, ek bad migration se minutes mein recover karne ke liye.

### ElastiCache & Caching Patterns

**Q: Redis vs Memcached — kab default Redis ko karte ho, aur Memcached kab still pick karoge?**

A: Redis ko default karo (rich data structures, persistence, Multi-AZ automatic failover, transactions/pub-sub). Memcached sirf ek genuinely simple cache ke liye pick karo jo uske multi-threaded model se benefit karta hai aur jisko persistence/failover ki zarurat nahi.

**Q: Chaar caching patterns aur unke trade-offs naam do.**

A: **Lazy loading/cache-aside** — miss par DB read karo, cache populate karo; ek hot key expiry par cache-stampede ka risk. **Write-through** — cache+DB ek saath likho; reads hamesha warm, writes slower. **Write-behind** — cache likho, DB ko async flush karo; fastest writes, node failure par data loss ka risk. **TTL/eviction** — har item ko expiry chahiye; koi TTL strategy na hona ek stale-data bug wait kar raha hai.

**Q: DynamoDB workload ke liye DAX vs ElastiCache — kaunsa pick karoge?**

A: **DAX** — in-VPC write-through cache jo DynamoDB ke API-compatible hai, zero application caching code, lekin sirf **eventually-consistent** reads ko help karta hai aur writes accelerate nahi karta. **ElastiCache** — raw table reads se alag kisi cheez ko cache karne ke liye (computed aggregates, session state, rate limits, leaderboards) ya keys/eviction par full control ke liye. Ek read-heavy table jo ek hot partition/RCU ceiling hit kar rahi hai → DAX.

#### ElastiCache — Pitfalls

**Q: Cache stampede kya hai, aur teen mitigations kya hain?**

A: Ek popular key expire ho jaati hai aur sau concurrent requests simultaneously miss hote hain, database ko hammer karte hain — cache wo outage cause karta hai jo prevent karne ke liye ban tha. Jittered TTLs se, ek short-lived lock se taaki ek caller repopulate kare jabki dusre wait karein, ya serve-stale-while-revalidate se mitigate karo.

**Q: "Hot key" problem kya hai, aur shards add karna usse kyun fix nahi karta?**

A: Ek key exactly ek shard par rehti hai — ek disproportionately popular key cluster size ke bawajood us node ko saturate karti hai. Us key ke liye ek small client-side/in-process cache se fix karo, ya usse `key:{0..9}` mein split karo aur merge karo. DynamoDB hot partition jaisi hi shape.

**Q: Redis single-threaded hone se ek real production-incident risk kyun banta hai?**

A: Ek slow command (ek large keyspace par `KEYS *`, ek badi `DEL`, ek expensive Lua script) **har** dusre client ko block kar deta hai. `KEYS` ke bajaye `SCAN` aur large values ke liye `DEL` ke bajaye `UNLINK` use karo.

**Q: Kya ElastiCache durable hai? Agar durability chahiye to alternative kya hai?**

A: Nahi — yeh ek cache hai; node failure se unreplicated/unpersisted data lose ho jaata hai. Ek primary data store ke roop mein durability ke liye, **MemoryDB for Redis** (multi-AZ transaction log) use karo.

---

## Networking

### VPC, Subnets, NAT — Complete Model

**Q: Ek subnet ko actually "public" vs "private" kya banata hai?**

A: Uske naam ya ek flag ke baare mein kuch nahi — sirf uska **route table**. Public: iske paas ek `0.0.0.0/0 → Internet Gateway` route hota hai. Private: koi direct IGW route nahi; outbound (agar koi ho) NAT Gateway/Instance ke through jaata hai instead.

**Q: NAT Gateway kahan rehna zaroori hai, aur cost gotcha kya hai?**

A: Ek **public** subnet mein — private subnet mein ek non-functional hota hai. NAT per-hour **plus per-GB processed** bill karta hai; AWS-service-only traffic (S3, DynamoDB, Secrets Manager) ke liye, **VPC Endpoints** use karo instead — cheaper, lower latency, aur public internet se off.

**Q: VPC networking ke baare mein chaar common false claims list karo.**

A: "Public subnet = automatic internet access" (galat — public IP + IGW route + permissive SG chahiye); "NAT allows inbound traffic" (galat — outbound-only); "subnet name/tag security behavior decide karta hai" (galat — sirf route tables/SGs/NACLs matter karte hain); "one route table per VPC" (galat — usually per subnet-group ek).

### VPC Reference Architecture

**Q: Canonical 3-tier VPC layout describe karo.**

A: Har AZ ke liye public subnet (ALB + NAT Gateway + optional bastion) → har AZ ke liye private app subnet (ECS/EC2 compute) → har AZ ke liye private isolated data subnet (RDS, NAT/IGW ka koi route bilkul nahi). Dono AZs ke app subnets RDS primary se baat karte hain; standby failover tak koi traffic carry nahi karta.

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

**Q: Rapid-fire — NAT Gateway vs Internet Gateway, aur kya NAT inbound traffic allow kar sakta hai?**

A: IGW = public-subnet resources ke liye two-way internet access. NAT Gateway = private-subnet resources ke liye outbound-only. NAT kabhi inbound allow nahi kar sakta — design se hamesha outbound-only.

**Q: Rapid-fire — statefulness, attachment point, aur allow/deny capability par SG vs NACL?**

A: SG: stateful, ENI/instance se attach hota hai, allow-only. NACL: stateless, subnet se attach hota hai, allow AND deny. Practice mein SGs primary defense layer hain; NACLs sparingly coarse subnet-level blocking ke liye use hote hain.

### VPC Flow Logs

**Q: Ek `REJECT` record kya prove karta hai vs koi record bilkul na hona?**

A: Ek `REJECT` record prove karta hai ki traffic **pahuncha** aur ek SG ya NACL ne usse block kiya. **Koi record bilkul na hona** ka matlab hai packets kabhi wahan pahunche hi nahi (galat route table/subnet, no IGW/NAT) — yeh distinction almost kisi bhi network fault ko fast narrow kar deta hai.

**Q: Flow Logs se aap kaise batao ki SG ya NACL culprit hai?**

A: SGs stateful hote hain, isliye ek SG block sirf inbound `REJECT` dikhata hai. NACLs stateless hote hain, isliye ek NACL misconfiguration typically **return path par bhi** `REJECT` dikhata hai — dono directions mein rejects NACL ki taraf point karte hain.

**Q: Kaunsa traffic VPC Flow Logs dwara kabhi capture nahi hota?**

A: Amazon DNS server ka traffic, DHCP, instance metadata endpoint (`169.254.169.254`), Windows license activation, aur reserved VPC router address ka traffic.

### VPC Peering

**Q: VPC Peering ki teen hard constraints kya hain?**

A: (1) CIDR blocks overlap nahi karne chahiye, no exceptions. (2) **Peering transitive nahi hai** — A B ko peer karta hai, B C ko peer karta hai, A C tak nahi pahunch sakta; aapko direct A↔C peering chahiye (single most-asked peering question). (3) No edge-to-edge routing — aap ek peer ka IGW/NAT/VPN/Direct Connect use nahi kar sakte.

**Q: Peering scale kyun nahi karta, aur fix kya hai?**

A: *n* VPCs ko fully connect karne ke liye **n(n−1)/2** peering connections chahiye — 10 VPCs = 45 connections. **Transit Gateway** (hub-and-spoke, transitive routing, *n* attachments instead) specifically is mesh explosion ko solve karne ke liye exist karta hai.

### Transit Gateway

**Q: Transit Gateway VPC Peering se kya kar sakta hai jo Peering nahi kar sakti, aur cost trade-off kya hai?**

A: **Transitive routing** support karta hai (A → TGW → C kaam karta hai) aur segmentation ke liye per-attachment TGW route tables ke saath thousands of attachments tak scale karta hai. Cost: per-attachment-hour **plus** per GB charged hota hai — ek simple two-VPC case ke liye peering se zyada expensive.

### VPC Endpoints & PrivateLink

**Q: Gateway endpoint vs Interface endpoint (PrivateLink) — cost aur scope difference kya hai?**

A: **Gateway endpoint** — sirf S3 aur DynamoDB, ek route-table entry jo ek prefix list par point karti hai, no ENI, **free**. **Interface endpoint** — most other AWS services + SaaS + apni khud ki services, ek ENI jiska aapke subnet mein ek private IP hota hai, per-hour + per-GB cost.

**Q: Ek interface endpoint ke liye "endpoint exist karta hai lekin koi use nahi karta" ke do sabse common causes kya hain?**

A: Missing **private DNS enabled** (jisse standard service hostname abhi bhi private IP ke bajaye public endpoint resolve karta hai), aur endpoint ka security group client subnets se inbound 443 allow nahi kar raha.

**Q: Aap apni internal service ko PrivateLink ke through cross-accounts kaise publish karte ho?**

A: Uske saamne ek **NLB** rakho aur usse ek endpoint service ke roop mein expose karo; dusre VPCs/accounts mein consumers usse reach karne ke liye interface endpoints create karte hain — no peering, no overlapping-CIDR problem, no internet exposure.

### Hybrid Connectivity: Site-to-Site VPN & Direct Connect

**Q: Site-to-Site VPN vs Direct Connect — setup time, encryption, aur cost compare karo.**

A: **VPN** — public internet par IPsec, minutes-to-hours setup, default encrypted, cheap. **Direct Connect** — dedicated private fibre, weeks-to-months setup, **default encrypted nahi** (VPN-over-DX ya MACsec add karo), high fixed port cost lekin volume par cheaper egress.

**Q: Hybrid connectivity resilience ke liye standard HA answer kya hai?**

A: Full redundancy ke liye do different DX locations par do DX connections, ya cheaper way mein, automatic backup ke roop mein Site-to-Site VPN ke saath ek DX connection.

### Hands-On: VPC

**Q: "Mera instance internet tak nahi reach kar sakta" ke liye debug order kya hai?**

A: Route table (public ke liye IGW ko point karta 0.0.0.0/0, private ke liye NAT) → kya NAT Gateway actually ek public subnet mein hai? → security group outbound → NACL dono directions → kya instance ke paas bilkul public IP hai → ACCEPT/REJECT ke liye Flow Logs read karo.

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

**Q: DNS resolution actually kaise kaam karta hai, aur us chain mein Route 53 kahan baithta hai, walk through karo.**

A:

```
Browser cache → OS cache → Recursive resolver (ISP / 8.8.8.8)
   → Root nameserver (.)            "ask the .com servers"
   → TLD nameserver (.com)          "ask ns-123.awsdns-45.com"
   → Authoritative nameserver       "example.com A = 52.1.2.3"   ← Route 53 lives here
   → answer cached at every hop for the length of the TTL
```

Route 53 **authoritative nameserver** hai — registrar ke NS records se Route 53 ke 4 nameservers tak delegation hi usse domain ke liye authoritative banata hai. Ek common real failure: hosted zone create karna lekin registrar ko kabhi update na karna, ya ek hosted zone recreate karna (jo *different* nameservers issue karta hai).

**Q: A vs ALIAS record — ALIAS ko AWS targets ke liye kyun preferred kiya jaata hai?**

A: ALIAS free hai, ek extra lookup ke bina resolve hota hai, aur **zone apex** (`example.com`) par kaam karta hai — ek CNAME DNS spec se yeh nahi kar sakta, kyunki CNAMEs apex par use nahi ho sakte.

**Q: TTL trade-off kya hai, aur ek planned cutover se pehle standard practice kya hai?**

A: High TTL = fewer queries/lower cost lekin change ke baad stale answers linger karte hain; low TTL = fast propagation lekin zyada queries/cost. Ek migration se pehle, TTL ko well in advance kam karo (kam se kam ek old-TTL period pehle), change karo, phir usse wapas raise karo.

**Q: Routing policies naam do aur har ek ka one distinguishing use case.**

A: Simple (single endpoint), Weighted (canary/A-B split), Failover (health checks ke through primary/secondary), Latency-based (lowest measured latency region), Geolocation (legal/regional restriction), Geoproximity (geography se bias-weighted, Traffic Flow chahiye), Multi-value answer (simple client-side distribution, real LB nahi).

**Q: Sub-second failover ke liye Route 53 alone kyun insufficient hai?**

A: DNS instant nahi hai — resolvers/ISPs/clients par TTL caching bound karta hai ki failover kitni fast propagate ho sakta hai. Fast reaction ke liye ALB/NLB-level health-based removal ke saath combine karo; macro/region-level failover ke liye Route 53 use karo, ya **Global Accelerator** ke saath pair karo (static anycast IPs, sub-minute health-check failover, entry IPs kabhi change nahi hote).

**Q: Private Hosted Zone ka gotcha kya hai?**

A: Isse **har VPC ke saath explicitly associate karna** zaroori hai jise usse resolve karne ki zarurat hai — "ek VPC mein kaam karta hai, dusre mein nahi" ka almost hamesha matlab hota hai ek missing association.

### API Gateway Auth & Integration Patterns

**Q: Chaar API Gateway auth mechanisms compare karo aur har ek kab use karna hai.**

A: **Cognito User Pools (JWT authorizer)** — standard username/password ya social login, gateway par validated, koi custom auth code nahi. **IAM authorization** — aapke org ke andar SigV4-signed service-to-service calls. **Lambda custom authorizer** — legacy tokens ya non-standard schemes. **API keys + usage plans** — partner/B2B throttle/quota, real authentication nahi.

---

## Load Balancing, Scalability & Auto Scaling

### Scalability, High Availability, Elasticity & Agility

**Q: Scalability, High Availability, Elasticity, aur Agility ko precisely define karo — aur ek "scalable but not elastic" example do.**

A: **Scalability** — system zyada load handle *kar sakta* hai (vertical=bigger instance, hard ceiling; horizontal=zyada instances, statelessness chahiye). **High Availability** — ≥2 AZs ke through downtime ke bina failure se survive karna (scalability se ek alag goal — aap ek AZ mein scalable ho sakte ho aur phir bhi sab kuch lose kar sakte ho). **Elasticity** — demand ke match dono directions mein automatically scale karna (scalability capability hai, elasticity uska automation hai). **Agility** — aap naye resources bilkul kitni fast paa sakte ho, load se unrelated. Example: Black Friday ke liye sized 20 instances ki ek fixed fleet scale karti hai (peak handle karti hai) lekin elastic nahi hai (aap February mein 20 ke liye pay karte ho).

**Q: HA vs Fault Tolerance vs DR — distinction kya hai?**

A: HA = ek region ke andar minimal downtime (multi-AZ). Fault tolerance = ek component failure se zero interruption (redundancy with no impact). DR = ek poori region lose karne se recover karna, RTO/RPO se measured.

### ALB vs API Gateway vs ELB (NLB/GWLB/CLB)

**Q: ALB vs API Gateway choose karne ke liye mental model kya hai?**

A: "ALB ek smart Layer-7 load balancer hai — mere paas services hain, unke beech traffic route aur balance karta hoon. API Gateway ek full API front door hai — main external clients ko ek API expose kar raha hoon aur mujhe built-in auth, throttling, quotas, versioning, transformation, aur monitoring chahiye."

**Q: ALB vs NLB vs GWLB — har ek kaunse layer aur protocol par operate karta hai?**

A: ALB = Layer 7, HTTP/HTTPS/WebSocket, host/path routing + Lambda targets. NLB = Layer 4, TCP/UDP/TLS, extreme throughput + static IP. GWLB = Layer 3, IP, firewall/IDS appliances ke liye transparent traffic inspection.

### ELB Deep-Dive: Cross-Zone Load Balancing, 504 Timeouts & Shield DDoS Protection

**Q: ALB aur NLB ke liye cross-zone load balancing by default on hai kya?**

A: ALB — by default on, **hamesha** (disable nahi kiya ja sakta). NLB — by default off, ek per-target-group toggle (isse enable karne se cross-AZ data transfer charges add ho sakte hain, isi wajah se teams cost/latency-sensitive NLB use ke liye isse aksar off rakhti hain).

**Q: ELB 504 Gateway Timeout ke teen root causes kya hain?**

A: Ek unhealthy/slow target (app hung, DB call blocking); ek traffic spike jo auto-scaling react karne se pehle backend capacity ko overwhelm kar deta hai; ya ALB idle timeout (default 60s) aur backend ke legitimate response time ke beech ek mismatch — ek phantom bug chase karne ke bajaye ALB idle timeout raise karke fix karo.

**Q: Shield Standard vs Shield Advanced — kya cover hota hai, aur WAF kahan fit hota hai?**

A: **Shield Standard** — free, har account par Route 53/CloudFront/ELB ke liye automatic, common L3/L4 DDoS cover karta hai. **Shield Advanced** — paid opt-in, larger-attack mitigation, real-time visibility, WAF integration, cost protection, 24/7 DRT access. **WAF** L7 defend karta hai (malicious patterns, rate limiting) — Shield ka complementary hai, competing nahi; dono ko saath naam do.

### Load Balancing Fundamentals

**Q: Order mein load-balancer ke chaar objects kya hain, aur Fargate ko kaunsa target type chahiye?**

A: Listener (port+protocol) → Rules (host/path/header/query/IP/method conditions) → Target Group (EC2/IP/Lambda/ALB) → Targets (health-checked). Fargate aur `awsvpc` ECS tasks ko **`ip`** target type chahiye, kyunki unke apne ENIs hote hain.

**Q: Slow Start kya hai, aur yeh .NET/JVM targets ke liye specifically kyun matter karta hai?**

A: Ek naye target par traffic ko immediately full share ke bajaye 30–900s ke over gradually ramp karta hai — by default off. .NET/JVM ke liye genuinely useful hai kyunki ek fresh process mein cold JIT aur empty caches hote hain; iske bina, ek newly joined target ka p99 spike ho jaata hai ya woh full load lete hi health checks fail kar deta hai.

**Q: ALB ke paas static IP kyun nahi hota, aur jab ek partner firewall ko ek chahiye to fix kya hai?**

A: Ek ALB ke paas sirf ek DNS name hota hai — uske peeche wale IPs change hote rehte hain. Fix: **NLB**, **NLB ek ALB ke saamne**, ya **Global Accelerator**.

**Q: Ek ALB ke peeche aapko kaunsa ASP.NET Core middleware chahiye, aur kyun?**

A: `ForwardedHeadersMiddleware`, kyunki ALB HTTP terminate karta hai aur `X-Forwarded-For` ke through original client IP pass karta hai — middleware ke bina har client load balancer jaisa dikhta hai, silently rate limiting, geo-logic, aur audit logs break karta hai.

### Sticky Sessions (Session Affinity)

**Q: ALB ke do stickiness mechanisms kya hain, aur fundamental trade-off kya hai?**

A: **Duration-based** (LB-generated `AWSALB` cookie) ya **application-based** (LB app ke apne `AWSALBAPP` cookie ko honour karta hai). Trade-off: stickiness even load distribution ko undermine karta hai (ek client ek target ko hot-spot kar sakta hai), aur jab scale-in/deploy par ek target remove hota hai to sessions vaise bhi lost ho jaate hain — yeh kabhi truly session survival guarantee nahi karta. Senior fix stickiness par rely karne ke bajaye session state ko externalize karna hai (ElastiCache/DynamoDB, .NET mein `IDistributedCache`).

### Connection Draining / Deregistration Delay

**Q: "Requests drop kiye bina deploy karna" ka complete answer kya hai?**

A: Teen cheezon ko chain karo: ek appropriate **deregistration delay** (default 300s, aapki longest legitimate request ke saath tuned), ASG par **ELB health checks**, aur **ASG lifecycle hooks** taaki ek instance serve karne se pehle warm ho aur die karne se pehle drain ho — plus rolling replacement ke liye **ASG instance refresh**.

### Auto Scaling Groups (ASG)

**Q: ASG health-check-type gotcha kya hai, aur yeh ek real production incident kyun hai?**

A: Default health-check type **EC2 only** hai — sirf hypervisor-level failure par instances replace karta hai. Ek instance jiska app hang ho gaya ho ya 500s return kar raha ho EC2 ko healthy dikhta hai aur **kabhi replace nahi hota**, chahe ALB ne usse traffic bhejna band kar diya ho. Health-check type ko **ELB** set karna zaroori hai taaki ASG load balancer ke application-level view par act kare.

**Q: CPU ke upar ek ASG scaling metric choose karne ke liye senior differentiator kya hai?**

A: Ek web/API tier ke liye `ALBRequestCountPerTarget` (demand ke proportional, CPU se pehle react karta hai); ek worker tier ke liye **SQS backlog per instance** — canonical hai kyunki ek worker jo I/O par blocked hai low CPU dikhata hai jabki backlog grow karta hai, isliye CPU signal ko completely miss kar dega.

**Q: ASG lifecycle hooks aapko kya karne dete hain, aur warm pool kis liye hai?**

A: Lifecycle hooks proceed karne se pehle ek instance ko `Pending:Wait` (bootstrap/warm caches/register) ya `Terminating:Wait` (drain/flush/deregister) mein pause karte hain. **Warm pools** pre-initialized, stopped instances ko ready rakhte hain taaki scale-out boot/bootstrap time skip kar de — un apps ke liye jinka startup slow hai jahan predictive scaling kaafi nahi hai.

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

**Q: Recite karne layak core scalability best practices list karo.**

A: Stateless app tier; kisi bhi distributable cheez ke liye scale out, scale up nahi; ≥2 AZs ke across minimum 2 instances; ek real `/health` ke saath ASG health check type = ELB; CPU nahi, demand-correlated metric par scale karo; session/cache state externalize karo; known events ke liye scheduled/predictive scaling; scale-in bhi test karo (sirf scale-out nahi) kyunki most bugs way down par surface hote hain; cost/blast-radius ceiling ke roop mein `max` deliberately cap karo.

### Scalability & Load Balancing Shared Responsibility Model

**Q: Scalability/LB shared-responsibility split mein, kya aap par hai vs AWS par?**

A: AWS: ELB fleet run/scale karna, ASG control plane, health-check infrastructure, AZ-level availability, TLS termination capability. Aap: sahi LB type choose karna, min/desired/max aur scaling metric set karna, ek health endpoint likhna jo real health reflect kare, actually multiple AZs ke across span karna, certificate lifecycle, lifecycle hooks/deregistration delay tune karna, aur app ko stateless design karna.

---

## Messaging, Streaming & Decoupling

### SQS & SNS Fundamentals

**Q: Standard vs FIFO SQS queues — delivery, ordering, aur throughput compare karo.**

A: **Standard** — at-least-once (possible duplicates), koi ordering guarantee nahi, bahut high throughput. **FIFO** — exactly-once processing (dedup ke saath), Message Group ke hisaab se guaranteed order, lower throughput (halaanki high-throughput mode isse raise karta hai).

**Q: Delay queue vs visibility timeout — difference kya hai, aur yeh ek favorite trap kyun hai?**

A: **Delay queue** naye messages ko unke visible hone se pehle 0–15 min delay karta hai. **Visibility timeout** ek message ko receive hone ke **baad** hide karta hai, processing ke dauraan (default 30s, max 12h) — worst-case processing time se zyada hona chahiye warna message reappear ho jaata hai aur twice process hota hai. Ek huge global timeout ke bajaye variable-length work ke liye heartbeat ke roop mein `ChangeMessageVisibility` use karo.

**Q: SQS/SNS "claim check" pattern kya hai, aur yeh kaunsa message size limit ke around kaam karta hai?**

A: Dono messages ko **256KB** par cap karte hain. **SQS/SNS Extended Client Library** larger payloads ke liye body ko S3 mein store karti hai aur message mein ek pointer daalti hai.

**Q: SNS message filtering kya solve karta hai, aur yeh most candidates ke answers mein ek notable gap kyun hai?**

A: Subscribers message attributes (ya body) par ek JSON filter declare karte hain, taaki har ek sirf woh events receive kare jinki usse fikar hai — har consumer ko har event receive karke code mein filter karne se bachaata hai, topic level par invocations/cost/complexity save karta hai.

**Q: Event-driven microservices ke liye SNS + SQS kyun combine karo, dono ko alone use karne ke bajaye?**

A: SNS fan-out deta hai (N heterogeneous subscribers ko push); SQS durability, retries, DLQ, aur per-consumer isolation deta hai — ek publisher, har interested microservice ki apni khud ki queue hoti hai, isliye kisi consumer ki slowness/downtime kisi dusre ke liye events block/drop nahi karta.

### CQRS with SNS/SQS in .NET

**Q: SNS par ek domain event kab publish karna hai, uska golden rule kya hai?**

A: Write-side DB transaction successfully commit hone ke **baad** hi publish karo — commit se pehle speculatively kabhi nahi. Stronger guarantees ke liye, **Outbox Pattern** use karo (same DB transaction mein event + data likho, ek separate relay usse publish karta hai).

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

**Q: SQS ke through consume karte waqt classic SNS-envelope bug kya hai?**

A: SNS real payload ko ek envelope ke andar wrap karta hai (`Type`, `Message`, `MessageId`, `TopicArn`) — actual event deserialize karne se pehle `snsEnvelope.Message` unwrap karna bhoolna ek common mistake hai.

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

**Q: Ek SQS message ko kab delete karna chahiye — processing se pehle ya baad?**

A: Successful processing ke **baad** hi. Pehle/immediately delete karna crash mid-processing par silent data loss ka risk banata hai; failure par delete na karna SQS ko naturally retry karne deta hai, phir `maxReceiveCount` ke baad DLQ.

**Registration (`Program.cs`):**

```csharp
builder.Services.AddAWSService<IAmazonSimpleNotificationService>();
builder.Services.AddAWSService<IAmazonSQS>();
builder.Services.AddSingleton<SnsPublisher>();
builder.Services.AddHostedService<OrderCreatedConsumer>();
```

### CQRS + SNS/SQS Interview Pitfalls

**Q: CQRS + SNS/SQS pitfalls par 60-second wrap-up answer do.**

A: "Sabse badi pitfalls immediate consistency, exactly-once delivery, ya shared queues assume karna hain. Events sirf successful commits ke baad publish hone chahiye, consumers idempotent hone chahiye, aur har consumer ki apni queue ek DLQ ke saath honi chahiye. Ordering, retries, aur replay ko explicitly design karna padta hai — warna aapko silent data loss ya duplicate side effects milte hain."

**Q: Cross-service notification ke liye DB triggers explicit events se worse alternative kyun hain?**

A: Triggers invisible, version karna hard, aur non-portable hote hain; explicit events observable, testable contracts hote hain.

### EventBridge Deep Dive

**Q: EventBridge plain SNS/SQS ke upar kya add karta hai?**

A: Event buses (default/custom/partner), rule level par rich **content-based filtering** (per-consumer filter code ke bina), versioning/typed code generation ke saath ek **Schema Registry**, **Archive & Replay**, aur targets jinme API destinations (managed retries ke saath arbitrary HTTPS) aur native scheduled/cron rules shamil hain.

**Q: SNS ke upar EventBridge kab pick karoge?**

A: Rich routing logic, schema management, ya scheduled jobs chahne wale cross-service domain events ke liye EventBridge. Simple, high-throughput fan-out ke liye jisme filtering/schema ki zarurat nahi, SNS+SQS. Kaafi architectures dono use karte hain — bounded contexts ke beech EventBridge, ek ke andar fan-out ke liye SNS/SQS.

**Q: Ek .NET-relevant EventBridge scheduled rule (CLI/CloudFormation) dikhao.**

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

**Q: Reference event-driven order-processing flow (API Gateway → Lambda → DynamoDB → SNS → SQS → Lambda) mein, har downstream step mein kaunsi single property honi chahiye, aur kyun?**

A: **Idempotency** — SQS at-least-once hai, isliye same message legitimately do baar aa sakta hai; har step (ingest, processor, external integrations) ko duplicate delivery safely handle karna chahiye.

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

**Q: Ek event-driven pipeline ke production-readiness checklist se paanch items naam do.**

A: Dedup key ke through ingest par idempotency; depth>0 par DLQ + alarm; races prevent karne ke liye DynamoDB mein conditional updates; SQS visibility timeout > Lambda max execution time; end-to-end X-Ray/OpenTelemetry tracing. (Bhi: least-privilege IAM + SSE-KMS, load testing, documented DLQ replay runbooks, schema evolution strategy.)

### Amazon Kinesis

**Q: Ek Kinesis stream aur ek SQS queue ke beech fundamental difference kya hai?**

A: Ek stream ek **replayable, ordered log** hai jise multiple independent consumers har ek fully read kar sakte hain; ek queue message ek baar consume ho jaata hai aur delete ho jaata hai (competing consumers work share karte hain).

**Q: Kaunsa shard par ek record land karega yeh kya decide karta hai, aur ek bad choice ka failure mode kya hai?**

A: **Partition key**. Ordering sirf ek shard ke andar guaranteed hai, isliye ek aisi key choose karo jo evenly distribute kare aur order chahne wale records ko saath rakhe (jaise `customerId`). Ek low-cardinality key ek **hot shard** create karti hai — DynamoDB hot partition jaisi hi failure shape.

**Q: Firehose vs Data Streams — kab kaunsa pick karoge?**

A: **Firehose** — near-real-time, buffered, serverless/no shards, Lambda ke through transform kar sakta hai + Parquet/ORC mein convert kar sakta hai, **replay nahi kar sakta**. "Bas data ko S3/OpenSearch mein reliably daalo, no code" → Firehose. **Data Streams** — true real-time, replayable (up to 365 days retention), aap shards/consumers manage karte ho. "Multiple consumers, replay, sub-second, custom processing" → Data Streams.

**Q: Replay capability par SQS, SNS, EventBridge, aur Kinesis Data Streams compare karo.**

A: SQS — no replay (process hone ke baad delete). SNS — no replay (no storage). EventBridge — haan, Archive & Replay ke through. Kinesis Data Streams — haan, retention window ke andar (up to 365 days), aur uniquely kaafi independent consumers ko har ek sab kuch read karne ka support karta hai.

### Step Functions: Orchestration vs Choreography

**Q: Standard vs Express Step Functions workflows — duration, execution model, aur pricing compare karo.**

A: **Standard** — max 1 year, exactly-once fully durable, full visual history (90 days), state transition ke hisaab se priced; long-running business processes/human approval/ETL ke liye. **Express** — max 5 minutes, at-least-once, sirf CloudWatch Logs, request+duration ke hisaab se priced (high volume par cheaper); high-volume short-lived event processing ke liye.

**Q: Step Functions Saga pattern ko distributed transactions ke liye kaise implement karta hai?**

A: Har step par `Catch` compensating actions trigger karta hai (payment refund, inventory release) — "no two-phase commit ke saath microservices ke across transactions kaise karte ho" ka standard answer.

**Q: Orchestration vs choreography — senior framing kya hai, aur dono kab use karte ho?**

A: Orchestration (Step Functions) = ek central coordinator process hold karta hai, full visibility, ek defined business process ke liye best jise ordering/compensation/audit trail chahiye. Choreography (EventBridge/SNS+SQS) = har service independently react karti hai, loose extensible fan-out ke liye best. Dono ko different layers par use karo: bounded contexts ke beech choreography, ek ke andar multi-step processes ke liye orchestration.

### Amazon MQ

**Q: SQS/SNS ke upar Amazon MQ choose karne ka ek reason kya hai?**

A: **Protocol compatibility** — yeh open standards (AMQP, MQTT, STOMP, OpenWire, JMS, WSS) bolta hai jo existing enterprise apps already use karti hain, jabki SQS/SNS proprietary AWS APIs expose karte hain. Yeh broker-based hai (aapke VPC mein instances par run karta hai, Multi-AZ active/standby), isliye yeh SQS jaisa scale nahi karta — lift-and-shift ke liye migration path, naye AWS-native builds ke liye nahi.

---

## Global Edge Services

### CloudFront (CDN)

**Q: Region, Availability Zone, aur Edge Location/PoP define karo.**

A: **Region** — ek geographic area (`us-east-1`). **AZ** — region ke andar ek ya zyada discrete data centers, failure ke liye isolated, low-latency fibre se linked. **Edge Location/PoP** — worldwide 600+ CloudFront caches mein se ek, caching aur backbone entry ke liye, workloads run karne ke liye nahi.

**Q: CloudFront raw speed ke alawa aur kya deta hai?**

A: Origin offload (cached hits kabhi origin tak nahi jaate, isse load aur egress cost dono kam ho jaate hain — AWS origins se CloudFront→origin free hota hai), edge par built-in Shield Standard + WAF integration, aur free ACM cert ke saath TLS termination.

**Q: Cache busting ke liye CloudFront invalidations se better practice kya hai?**

A: **Versioned filenames/query strings** (`app.a1b2c3.js`) — naye content ko naya cache key milta hai, kuch purge karne ki zarurat nahi hoti. Invalidations slow hote hain aur free monthly allowance ke aage billed hote hain.

**Q: OAC kya hai, aur yeh legacy OAI se better kyun hai?**

A: **Origin Access Control** CloudFront ko private S3 bucket ko signed requests bhejne deta hai, isliye bucket internet ke liye fully closed rehta hai — OAI ke unlike, yeh SSE-KMS aur saare HTTP methods bhi support karta hai.

**Q: CloudFront signed URL vs S3 presigned URL — difference kya hai?**

A: **S3 presigned URL** — S3 API se generate hota hai, generating principal ke IAM permissions carry karta hai, directly S3 ko hit karta hai, CloudFront ka cache/protections bypass kar deta hai. **CloudFront signed URL** — CloudFront key pair se banta hai, edge ke through serve hota hai (caching/WAF/Shield/logging bana rehta hai), kisi bhi origin type ko cover karta hai, bucket ko private rehne deta hai. CloudFront signing use karo jab content CDN ke through deliver ho raha ho; S3 presigning direct short-lived programmatic access ke liye.

**Q: CloudFront Functions vs Lambda@Edge — difference kya hai?**

A: **CloudFront Functions** — lightweight JS, sub-millisecond, sirf viewer request/response, koi network calls nahi; header manipulation, URL rewrites, simple auth checks ke liye. **Lambda@Edge** — Node.js/Python, 5–30s tak, saare chaar trigger points including origin request/response, dusre services ko call kar sakta hai; origin selection logic, DB/API calls, heavier auth ke liye.

**Q: CloudFront ka classic certificate region gotcha kya hai?**

A: CloudFront ke liye custom-domain cert **`us-east-1`** mein hi request/import karna padta hai, origin/user location chahe kahin bhi ho, kyunki CloudFront globally N. Virginia se manage hota hai. ALB certs, iske contrast mein, ALB ke apne region mein hone chahiye.

### AWS Global Accelerator

**Q: Global Accelerator kya hai, aur Route 53 failover routing ke upar iska sabse bada advantage kya hai?**

A: Do static anycast IPs jo aapke app ke front mein hote hain; traffic nearest edge par AWS private backbone mein enter karta hai aur internally closest healthy regional endpoint tak jaata hai. Failover **~30 seconds aur DNS-independent** hota hai — IPs kabhi change nahi hote, isliye koi client/resolver/ISP TTL cache delay nahi kar sakta.

**Q: Global Accelerator kaunse protocols ke saath kaam karta hai, jo CloudFront nahi kar sakta?**

A: Layer 4, TCP **aur UDP** — koi bhi protocol (gaming, VoIP, IoT, MQTT, custom TCP), sirf HTTP nahi.

### CloudFront vs Global Accelerator

**Q: CloudFront aur Global Accelerator ko distinguish karne wali one-liner do.**

A: "CloudFront edge par HTTP content cache karta hai; Global Accelerator kuch bhi cache nahi karta — yeh sirf kisi bhi TCP/UDP traffic ko AWS backbone par jaldi laata hai aur do static IPs plus fast regional failover deta hai. Cacheable web content → CloudFront. Dynamic, non-HTTP, ya static-IP/fast-failover requirements → Global Accelerator. Dono ko combine bhi kiya ja sakta hai."

### Local Zones, Outposts & Wavelength

**Q: Local Zones vs Outposts vs Wavelength — har ek kya extend karta hai, aur kahan tak?**

A: **Local Zones** — region ka extension ek major metro area mein (services ka subset) taaki kisi specific city ke liye single-digit-ms latency mile. **Outposts** — physical AWS racks aapke apne data center mein, data-residency ya on-prem latency needs ke liye. **Wavelength** — AWS compute jo 5G telco networks ke andar embedded hota hai, ultra-low-latency mobile (AR/VR, connected vehicles) ke liye.

### Hands-On: CloudFront & Global Accelerator

**Q: CloudFront cache performance kaise verify karte ho, aur low hit ratio kaise diagnose karte ho?**

A: `X-Cache` response header check karo (`Hit from cloudfront` vs `Miss from cloudfront`) aur cache hit ratio metric — low ratio almost hamesha matlab cache key mein bahut zyada headers/cookies/query strings forward ho rahe hain.

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

**Q: "kya abhi kuch bura ho raha hai" vs "mujhme kaunsi weaknesses hain" vs "mera sensitive data kahan hai" vs "kahin kuch misconfigured/drifting hai" — yeh sab kaunse services se map hote hain?**

A: Abhi bura ho raha hai → **GuardDuty**. Weaknesses → **Inspector**. Sensitive data location → **Macie**. Misconfiguration/drift → **AWS Config**. (Sabka single pane → Security Hub; root-cause investigation → Detective.)

### DDoS Protection: Shield & WAF

**Q: Teen DDoS attack shapes ke naam batao.**

A: **Volumetric** (L3/4 — bandwidth saturate karna, e.g. UDP reflection/amplification, SYN floods); **protocol** (TCP/IP behavior exploit karna); **application-layer** (L7 — HTTP floods, Slowloris; bandwidth kam lekin per request expensive kyunki har request app/DB ko hit karta hai).

**Q: Shield Standard vs Advanced — dono mein kya included hai?**

A: **Standard** — free, har account par automatic, Route 53/CloudFront/Global Accelerator/ELB ko common L3/4 attacks se cover karta hai. **Advanced** — paid (~$3,000/mo org-wide), larger-scale mitigation, 24/7 SRT access, cost-protection credits, health-based detection, aur **WAF bina extra charge ke included**.

**Q: WAF NLB par attach kyun nahi hota, aur naya rule live karne se pehle WAF best practice kya hai?**

A: WAF ko HTTP context chahiye — NLB Layer 4 hai, isliye WAF sirf CloudFront/ALB/API Gateway/AppSync/Cognito par attach hota hai. Best practice: har naya rule pehle **Count mode** mein deploy karo, logs review karo ki yeh kya block karta, phir Block par switch karo — directly Block par jaana ek common tareeka hai apna hi legitimate traffic down karne ka.

**Q: AWS Firewall Manager kya centralize karta hai?**

A: WAF rules, Shield Advanced protections, security-group policies, aur Network Firewall rules — Organization ke har account mein, naye create hone wale resources par bhi automatically apply hota hai.

### AWS Network Firewall

**Q: Network Firewall kya inspect kar sakta hai jo WAF aur Security Groups nahi kar sakte?**

A: Saara traffic (Layer 3–7, sirf HTTP nahi) VPC level par — stateful filtering, **egress ke liye domain-name filtering** (allowlist `*.microsoft.com`), aur deep packet inspection ke liye Suricata-compatible IPS rules. Compliance-driven egress allowlisting aur non-HTTP intrusion detection ke liye right answer.

### KMS & CloudHSM

**Q: AWS owned vs AWS managed vs customer managed KMS keys — difference kya hai?**

A: **AWS owned** — invisible, shared. **AWS managed** (`aws/s3`, `aws/ebs`) — free, auto-rotated, policy editable nahi. **Customer managed (CMK)** — aap create karte ho, apni key policy, optional annual auto-rotation, mandatory 7–30 day deletion waiting period.

**Q: "IAM allow keh raha hai lekin KMS phir bhi deny kar raha hai" ek real failure mode kyun hai?**

A: Key ki **resource (key) policy mandatory aur authoritative hoti hai** — sirf `kms:Decrypt` dene wali IAM policy sufficient nahi hai jab tak key policy IAM ko delegate na kare (ya principal ko directly naam se na le). Yeh most AWS resources ke unlike hai.

**Q: Envelope encryption kya hai, aur yeh kyun exist karta hai?**

A: `Encrypt` API sirf 4KB tak handle karta hai. Bade data ke liye, `GenerateDataKey` ek plaintext data key + uski encrypted copy return karta hai; aap plaintext key se locally data encrypt karte ho, usko discard kar dete ho, aur encrypted key ko ciphertext ke saath store karte ho. S3/EBS internally yahi karte hain, aur isi liye high throughput par KMS request quotas matter karte hain (isi liye S3 Bucket Keys hain).

**Q: KMS vs CloudHSM — deciding factor kya hai?**

A: "KMS use karo, jab tak koi regulator specifically require na kare ki AWS mere keys ko bilkul access na kar sake — tab CloudHSM (single-tenant dedicated hardware, keys aap khud fully manage karte ho, agar lost ho jaaye toh AWS recover nahi kar sakta)."

#### Worked Example: Giving a Fargate Task Access to KMS-Encrypted S3 Data

**Q: Fargate task ki IAM policy clearly `s3:GetObject` allow karti hai lekin phir bhi woh KMS-encrypted bucket read nahi kar pa raha — kaunsi chaar cheezein sach honi chahiye?**

A: (1) **Task role** (task execution role nahi) ko `s3:GetObject` aur `kms:Decrypt`/`DescribeKey` dono chahiye, ideally `kms:ViaService` ke saath scoped. (2) **KMS key policy** mein bhi usi role ka naam hona chahiye — mandatory aur authoritative, IAM se separate. (3) **Bucket policy**, agar restrictive hai, usko bhi allow karna chahiye (cross-account ke liye mandatory). (4) **Network path** — S3 ka free gateway endpoint hota hai, lekin **KMS ka gateway endpoint nahi hota**, isliye interface endpoint ya NAT chahiye, warna request hang ho ke timeout ho jaayega.

Task role ki IAM policy:

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

KMS key policy mein bhi separately wahi role naam se hona chahiye:

```json
{
  "Sid": "AllowTaskRoleToDecrypt",
  "Effect": "Allow",
  "Principal": { "AWS": "arn:aws:iam::111122223333:role/my-app-task-role" },
  "Action": ["kms:Decrypt", "kms:DescribeKey"],
  "Resource": "*"
}
```

**Q: In chaaron mein se kaunsa galat hai, yeh diagnose karne ke liye error kaise padhein?**

A: `GetObject` par `AccessDenied` → S3 permission missing. `KMS.AccessDeniedException` (ya misleading "CMK does not exist... or you are not allowed to access") → `kms:Decrypt` missing ya key policy mein role ka naam nahi hai. Hang hoke timeout → VPC endpoint/NAT missing. Dev mein kaam karta hai, prod mein fail hota hai → per-environment CMK jiski key policy kabhi update nahi hui.

#### Encryption in Transit (TLS) — End to End

**Q: "we use TLS end to end" kehna often overstatement kyun hota hai?**

A: TLS har hop par alag-alag terminate hota hai — CloudFront→origin (Origin Protocol Policy), ALB→target (target group protocol, often plain HTTP VPC ke andar, jo defensible hai lekin "end to end" nahi hai). True end-to-end ke liye ALB→target hop par bhi HTTPS chahiye, aur ALB target ka cert validate nahi karta (waha self-signed cert bhi chalega — aap hop ko encrypt kar rahe ho, backend ko authenticate nahi kar rahe).

```
Client --TLS(ACM)--> CloudFront --TLS--> ALB --TLS or plain HTTP?--> ECS task --TLS?--> RDS
                                  ^                    ^                        ^
                          viewer protocol       target group protocol    force_ssl / sslmode
```

**Q: AWS ke har layer par TLS ko kaise enforce karte ho (sirf hope nahi karte)?**

A: S3 — `aws:SecureTransport: false` ke saath `Deny`. ALB — HTTP:80 listener jiska sirf action HTTPS par redirect ho. RDS — `rds.force_ssl=1`/`require_secure_transport`. ElastiCache — in-transit encryption cluster **creation** ke time enable karo (baad mein add nahi ho sakta). EFS — `-o tls` ke saath mount karo.

### ACM (AWS Certificate Manager)

**Q: ACM certs ke liye DNS validation vs email validation — hamesha kaunsa choose karte ho, aur kyun?**

A: **DNS validation** — CNAME add karo, record wahin rehne tak forever auto-renew hota hai. Email validation manual hoti hai aur agar koi link par click nahi karta toh renewal break ho jaata hai. Hamesha DNS choose karo.

**Q: ACM public certificate directly EC2 instance par install kyun nahi kar sakte?**

A: ACM public certificate ki private key **export nahi ho sakti** — iske jagah TLS ko ALB/CloudFront par terminate karo, ya **ACM Private CA** use karo (jo export allow karta hai, internal certs ke liye, paid).

### AWS Systems Manager (SSM)

**Q: Session Manager bastion host ko kaise replace karta hai, aur iske liye kya chahiye?**

A: Koi SSH keys nahi, koi open inbound ports nahi (SSM Agent ek **outbound** connection banata hai, isliye instance fully private subnet mein bina inbound rules ke reh sakta hai), koi bastion run/patch/pay karne ki zarurat nahi, access IAM se control hota hai, har session CloudTrail mein logged hota hai aur optionally keystroke-by-keystroke record ho sakta hai. Chahiye: SSM Agent chal raha ho, `AmazonSSMManagedInstanceCore` wala instance profile, aur SSM endpoints (`ssm`/`ssmmessages`/`ec2messages`) tak network reachability (NAT ya interface VPC endpoints).

```bash
aws ssm start-session --target i-0abc123
# Port-forward a private RDS/RDP endpoint to localhost — replaces an SSH tunnel through a bastion
aws ssm start-session --target i-0abc123 \
  --document-name AWS-StartPortForwardingSessionToRemoteHost \
  --parameters '{"host":["mydb.abc.us-east-1.rds.amazonaws.com"],"portNumber":["5432"],"localPortNumber":["5432"]}'
```

**Q: Session Manager ke alawa teen aur SSM capabilities batao.**

A: **Patch Manager** (baselines/maintenance windows ke through scheduled OS patching — "guest OS patching is your responsibility" ka concrete jawab); **Run Command** (tagged fleet par script execute karo, koi SSH nahi, full audit log); **State Manager** (desired configuration ko continuously enforce karta hai, drift ko correct karta hai).

### AWS Artifact

**Q: AWS Artifact actually kya provide karta hai, aur kaunsi claim usme se avoid karni chahiye?**

A: Compliance documents (SOC 1/2/3, PCI DSS AOC, ISO 27001, FedRAMP, HIPAA BAA) ke liye ek self-service portal. Yeh kuch bhi **scan, monitor, ya secure nahi karta** — yeh sirf ek document repository hai jo underlying infrastructure ki compliance prove karta hai; line ke upar sab kuch aapko khud Config/CloudTrail/Security Hub se evidence karna hoga.

### GuardDuty

**Q: GuardDuty kya analyze karta hai, aur kya isko enable karne ke liye underlying logs on karna zaroori hai?**

A: CloudTrail management events, VPC Flow Logs, aur DNS logs (plus optional S3 data events, EKS audit logs, RDS login activity, Lambda network activity, EBS malware scanning) ko ML/threat intel ke through continuously analyze karta hai. **Agentless aur log-free setup** — sources ko directly padhta hai, logs ko on karne ya unke liye pay karne ki zarurat nahi.

**Q: Lambda/EC2 work claim karne wale resume ke liye typical high-value GuardDuty finding kya hai?**

A: EC2 instance credentials ka **AWS ke bahar se** use hona — yaani stolen role credentials (IMDSv2/SSRF risk se related). Findings EventBridge par route hoti hain response automate karne ke liye (SG isolate karna, sessions revoke karna).

### Inspector

**Q: Inspector vs GuardDuty — is pairing ka complete answer kya hai?**

A: **Inspector weaknesses dhundhta hai** — unpatched CVEs, vulnerable dependencies ("darwaze ka lock weak hai"), naye image/instance/CVE par continuous/event-driven rescanning. **GuardDuty active threats dhundhta hai** — abhi ho raha malicious behavior ("koi lock pick kar raha hai"). Complementary hain; interview mein dono ka naam is distinction ke saath lo.

### Macie

**Q: Macie kya karta hai, aur yeh uniquely kaunsa question answer karta hai?**

A: S3 ke liye ML-based sensitive-data discovery — buckets ka inventory banata hai (public/unencrypted/externally-shared) aur object contents mein PII/credentials/financial/health data classify karta hai. Yeh scale par "kya humare data-lake bucket mein customer PII pada hai jo kisi ko yaad nahi" wale question ka jawab deta hai.

### AWS Config

**Q: AWS Config ke terms mein "we detect drift" aur "we prevent drift" mein kya difference hai?**

A: Detection = managed/custom **rules** jo resource configuration ko policy ke against evaluate karte hain (`s3-bucket-public-read-prohibited`, `iam-user-mfa-enabled`). Prevention = SSM Automation ke through **remediation actions** jo violation ko automatically fix kar dete hain (e.g., jaise hi koi Block Public Access disable kare, usko phir se re-enable kar dena) — yehi detecting ko actually preventing se alag karta hai.

### Security Hub & Detective

**Q: Security Hub vs Detective — har ek kaunsa question answer karta hai?**

A: **Security Hub** — GuardDuty/Inspector/Macie/Access Analyzer/Config ki findings ko normalize/aggregate karke one pane mein laata hai, standards (CIS, PCI DSS) ke against score karta hai: "kya galat hai?" **Detective** — CloudTrail/Flow Logs/GuardDuty se ek interactive behavior graph banata hai root cause investigate karne ke liye: "yeh kaise hua aur kitna spread hua?"

### Defence in Depth — The Summary Answer

**Q: Defense-in-depth ki one-liner summary do, aur real incident mein sabse pehle kaunsa layer check karoge?**

A: "Koi single control hi answer nahi hai — kisi bhi ek layer ka fail hona breach cause karne ke liye sufficient nahi hona chahiye. Kisi bhi real incident mein main sabse pehle IAM check karunga, kyunki AWS par zyada tar breaches permission ya configuration failures hoti hain, infrastructure failures nahi." Layers: Edge (Route53/CloudFront/Shield/WAF) → Network (VPC/SG/NACL/Network Firewall) → Identity (IAM/MFA/SCPs) → Data (KMS/TLS/Object Lock) → Secrets (Secrets Manager/IAM DB auth) → Detect (GuardDuty/Inspector/Macie/Config) → Audit (CloudTrail/Flow Logs) → Aggregate (Security Hub) → Respond (Detective).

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

**Q: Ek account mein IAM separation ke jagah multiple AWS accounts kyun use karein?**

A: Ek account AWS ka diya hua **strongest isolation boundary** hai — compromised dev account prod resources ko bilkul touch nahi kar sakta, ek account ke andar IAM separation ke unlike. Iske alawa: blast-radius containment, per-account service quotas (ek runaway workload prod ka Lambda concurrency consume nahi kar sakta), aur clean cost attribution.

**Q: Management (payer) account ke liye best practice kya hai, aur kyun?**

A: Isme **koi workload nahi honi chahiye** — sirf billing aur org administration — kyunki SCPs isko restrict nahi kar sakti, isliye yeh estate ka sabse privileged jagah ban jaata hai.

### Service Control Policies (SCPs)

**Q: Sabse zyada pucha jaane wala SCP gotcha kya hai?**

A: **Management account SCPs se bilkul bhi affected nahi hota**, kahin bhi attach karo — isi liye workloads ko usme se bahar rakhte ho. SCPs *member* account ke root user ko bhi restrict karti hain (IAM policies ke unlike).

**Q: Deny-list vs allow-list SCP strategy — trade-off kya hai?**

A: Deny-list (`FullAWSAccess` rakho, explicit `Deny` statements add karo) — sabse common, kam kaam. Allow-list (`FullAWSAccess` remove karo, exactly wahi enumerate karo jo permitted hai) — tighter, bahut zyada kaam.

### Consolidated Billing

**Q: Consolidated billing actually kaunse do mechanisms se money save karta hai?**

A: (1) **Aggregated volume discounts** — accounts ke combined usage par tiered pricing calculate hoti hai, cheaper tiers jaldi reach hote hain. (2) **RI/Savings Plan sharing** — ek account ka unused commitment automatically org ke kisi bhi doosre account ke matching usage ko cover kar deta hai (usually bigger win; per account disable kiya ja sakta hai).

**Q: Cost-allocation-tag gotcha kya hai?**

A: Tags ko Billing console mein explicitly **activate** karna padta hai cost reports mein dikhne se pehle, aur activation **retroactive nahi hota** — historic spend kabhi tag nahi hoti.

### AWS Control Tower

**Q: Control Tower ek setup wizard mein kya deta hai, aur teen guardrail flavors kya hain?**

A: Log-archive/audit accounts, IAM Identity Center, org-wide CloudTrail/Config, aur baseline guardrails ke saath ek multi-account structure. Guardrails: **preventive** (SCPs — action block ho jaata hai), **detective** (Config rules — violation report hoti hai), **proactive** (CloudFormation hooks — deployment se pehle resource block ho jaata hai).

**Q: "How would you set up a secure multi-account AWS environment from scratch?" — jawab kya hai?**

A: **Control Tower** — landing zone hand-build karna (org structure, SCPs, centralized logging, SSO, guardrails, account vending) weeks ka kaam hai jo easily subtly galat ho sakta hai.

### AWS Resource Access Manager (RAM)

**Q: RAM use karke standard multi-account networking pattern kya hai?**

A: Ek central **networking account** ek well-designed VPC owns karta hai aur uske subnets ko RAM ke through workload accounts ko share karta hai — ek coherent network, koi VPC peering nahi, koi overlapping-CIDR problems nahi, per-team network design ki zarurat nahi, jabki IAM boundaries intact rehte hain.

**Q: RAM vs cross-account roles — difference kya hai?**

A: RAM **resource ko khud** share karta hai (subnet ek hi baar exist karta hai, many accounts use karte hain); cross-account role **act karne ki permission** share karta hai owning account mein.

### Cost Explorer

**Q: Cost Explorer mein free vs paid granularity kya hai, aur kaunsi recommendations produce hoti hain?**

A: Monthly/daily granularity included hai; **hourly aur resource-level granularity extra cost karti hai**. Built-in recommendations: EC2 rightsizing, utilization/coverage reports ke saath RI/Savings Plans purchase recommendations.

### AWS Budgets

**Q: Sirf alerting se aage strong answer kya hai — Budget Actions?**

A: Threshold breach hone par budget automatically **IAM/SCP deny policy apply** kar sakta hai ya **EC2/RDS instances stop** kar sakta hai — ek notification ko actual control mein badal dena, sandbox account ko cap karne ke liye ideal.

### Cost Anomaly Detection

**Q: Budgets vs Cost Anomaly Detection — distinction kya hai?**

A: Budgets ka answer hai "mujhe batao jab main defined limit cross karun" — isme aapko right number pata hona chahiye, ek chhoti service mein 300% spike miss ho jaata hai jab tak overall budget ke under hi rahe. Anomaly Detection ka answer hai "mujhe batao jab kuch weird ho" apne spend par ML ke through, koi threshold ki zarurat nahi — runaway Lambda recursion ya bhoole hue GPU instance ko day one par hi catch kar leta hai. Dono run karo.

### Trusted Advisor

**Q: Trusted Advisor ke liye support-tier gating kya hai, aur uske six pillars kya hain?**

A: Basic/Developer plans ko sirf core security + service-quota checks milte hain. **Full check set, API access, aur weekly reports ke liye Business ya Enterprise Support chahiye.** Pillars: cost optimization, performance, security, fault tolerance, service limits, operational excellence.

**Q: Trusted Advisor ka Compute Optimizer, Cost Explorer, Config, aur Security Hub se kya relation hai?**

A: Trusted Advisor ek unfamiliar account par shuru karne ke liye broad, shallow, best-practice sweep hai saare pillars ke across. Compute Optimizer deep ML rightsizing karta hai; Cost Explorer cost analysis/commitment recommendations karta hai; Config continuous customizable compliance deta hai; Security Hub formal standards ke against findings aggregate karta hai — depth ke liye yeh jagah hain.

### AWS Support Plans

**Q: Production ke liye minimum support tier kya hai, aur kyun?**

A: **Business** — 24/7 technical support, 1-hour production-down response, aur full Trusted Advisor check set + API wala pehla tier. Ek designated **TAM** sirf Enterprise On-Ramp/Enterprise mein milta hai.

### Free Tier, Pricing & Estimating Cost

**Q: Free Tier ke teen types ke naam batao, aur classic surprise charges jo isse cover nahi hote.**

A: **Always free** (1M Lambda requests/mo, 25GB DynamoDB), naye accounts ke liye **12 months free** (750h t2/t3.micro EC2), **trials** (GuardDuty 30 days). Surprise charges: NAT Gateway (~$32/mo bina kisi traffic ke, kabhi free-tier eligible nahi), unattached Elastic IPs, Never Expire retention wale CloudWatch Logs, orphaned EBS snapshots, cross-AZ/egress data transfer.

**Q: Universal AWS data-transfer pricing rule kya hai?**

A: Data transfer **in** free hota hai, data transfer **out** charged hota hai, aur cross-AZ traffic **dono** directions mein charged hota hai — isi liye VPC endpoints aur same-AZ placement sirf latency levers nahi, cost levers bhi hain.

**Q: Pricing Calculator vs Cost Explorer — difference kya hai?**

A: Pricing Calculator estimate karta hai ek design **kitna cost karega** (forward-looking, design reviews ke liye). Cost Explorer analyze karta hai aapne **kitna spend kiya** (retrospective).

---

## Cost & Performance

### Cost Optimization: Savings Plans, Reserved, Spot

**Q: 30% compute-bill reduction question ke liye "three-tier" cost architecture talking point kya hai?**

A: Commitments ko layer karo: **predictable baseline** ke liye Savings Plan, **variable middle** ke liye On-Demand, aur **fault-tolerant burst/batch** capacity ke liye Spot. Iske alawa: right-sizing aur idle resources ko eliminate karna (unattached EBS, idle NAT Gateways, over-provisioned RDS) usually pricing models switch karne se higher-ROI hota hai — "buy Savings Plans" se pehle ka correct *first* answer.

**Q: Kya Compute Savings Plans serverless compute par apply hote hain?**

A: Haan — Compute Savings Plans EC2, Fargate, **aur Lambda** par apply hote hain, ek frequently-missed lever jisko teams EC2-only maan lete hain.

---

## Migration & Data Transfer

### The 7 Rs — Migration Strategies

**Q: 7 Rs ke naam batao aur "usual sweet spot" strategy identify karo.**

A: Retire, Retain, Relocate, Rehost (lift-and-shift, MGN ke through), **Replatform** (lift-and-reshape — self-managed SQL Server ko RDS se swap karna, self-hosted queue ko SQS se — usual sweet spot: rewrite ke bina real savings), Repurchase (SaaS buy karna), Refactor/Re-architect (highest cost/risk, highest payoff).

**Q: Ek large migration ke liye honest senior sequencing answer kya hai?**

A: "Pehle data center se bahar aane ke liye rehost karo, phir AWS par real telemetry ke saath running hone ke baad selectively replatform aur refactor karo. Migration ke dauraan sab kuch refactor karne ki koshish karna migrations ko saal bhar slip karne ka tareeka hai. Sirf rehosting rarely money save karta hai — business case mein follow-on replatforming include hona chahiye."

### Database Migration Service (DMS)

**Q: Homogeneous vs heterogeneous DMS migration — schema conversion kaunsa tool handle karta hai?**

A: Homogeneous (SQL Server→RDS SQL Server) ek straight data move hai. Heterogeneous (Oracle→PostgreSQL/Aurora) ko pehle **Schema Conversion Tool (SCT)** chahiye — SCT schema convert karta hai, DMS data move karta hai.

**Q: DMS ka Change Data Capture (CDC) kya enable karta hai?**

A: Full load, phir ongoing changes ki continuous replication — source aur target ko sync mein rakhta hai taaki cutover long outage ke jagah ek short window ho. Baad mein CDC ko reverse mein chalana ek rollback path deta hai.

### Snow Family

**Q: Snowball aur network transfer (DataSync) ke beech kaise decide karte ho?**

A: Transfer time nikaalo: 100TB ko 1Gbps link par realistic utilization ke saath move karna saturated bandwidth ka ek week se zyada hota hai jo production ke liye bhi chahiye — ek Snowball jo days mein pahunch jaaye woh wins. ~10TB se kam, ya ek fat dedicated link ke saath, network transfer simpler hai.

**Q: Snowcone vs Snowball Edge vs Snowmobile — capacity aur use case?**

A: Snowcone (~8–14TB, chhoti rugged edge collection), Snowball Edge (~80TB, TB-to-PB migrations plus local compute), Snowmobile (100PB tak, ek 45-foot container, exabyte-scale data-center evacuation).

### Storage Gateway, DataSync & Transfer Family

**Q: Storage Gateway, DataSync, Snow Family, aur Transfer Family ko ek-ek sentence mein distinguish karo.**

A: **Storage Gateway** — permanent hybrid foothold, on-prem systems NFS/SMB/iSCSI/tape use karte rehte hain jabki data AWS mein rehta hai. **DataSync** — network par data move/sync karna. **Snow Family** — jab network kaam na kare, data physically move karna. **Transfer Family** — legacy SFTP/FTPS/FTP ke through S3 ko expose karna.

**Q: S3 File Gateway vs Volume Gateway vs Tape Gateway — har ek kaunsa protocol present karta hai?**

A: S3 File Gateway — NFS/SMB file shares S3 se backed. Volume Gateway — iSCSI block volumes S3+EBS snapshots se backed. Tape Gateway — ek virtual tape library (VTL) S3 Glacier se backed, physical tape retire karne ke liye jabki existing backup software use hota rahe.

---

## Well-Architected & Resilience

### AWS Well-Architected Framework — 6 Pillars

**Q: 6 Well-Architected pillars ke naam aur unka core question batao.**

A: **Operational Excellence** — kya aap run/monitor kar sakte ho aur continually improve kar sakte ho? **Security** — data/systems/assets ko kaise protect karte ho? **Reliability** — kya yeh consistently perform kar sakta hai aur failure se recover kar sakta hai? **Performance Efficiency** — demand change hone par resources efficiently use karna? **Cost Optimization** — unnecessary cost avoid karna? **Sustainability** — environmental impact minimize karna?

**Q: Open-ended "how would you evaluate this architecture" answer ko 6 pillars ke around structure karna kyun important hai?**

A: Yeh grab-bag of tips ke jagah architect-level thinking signal karta hai, aur AWS Well-Architected Tool aur formal Well-Architected Reviews ka basis hai.

### Well-Architected 6 Pillars — Rapid Recall Version

**Q: 6 pillars ke liye order mein memory hook recite karo.**

A: "Run it well, keep it safe, keep it up, keep it fast, keep it cheap, keep it green" — Operational Excellence, Security, Reliability, Performance Efficiency, Cost Optimization, Sustainability.

### Disaster Recovery Strategies

**Q: Cost/RTO/RPO trade-off ke order mein chaar DR strategies ke naam batao.**

A: **Backup & Restore** (RTO/RPO hours, sabse cheap) → **Pilot Light** (RTO tens of minutes, RPO minutes — core infra hamesha minimal running) → **Warm Standby** (RTO/RPO minutes ya usse kam — scaled-down full stack continuously running) → **Multi-Site Active-Active** (RTO/RPO near zero — full capacity 2+ regions mein live).

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

**Q: Warm Standby aur Active-Active data replication ko kaunse AWS services back karte hain?**

A: DynamoDB Global Tables ya Aurora Global Database, saath mein regions ke across Route 53 latency/weighted routing — Active-Active ke liye conflict-tolerant/idempotent write design chahiye.

**Q: Kya Route 53 failover apne aap mein DR ke liye enough hai?**

A: Nahi — DNS failover TTL-bound hota hai aur instant nahi hota. Yeh Pilot Light/Warm Standby/Active-Active ka ek component hai, apne aap mein ek complete DR strategy nahi.

**Q: DR Lambda concurrency planning ko kaise change karta hai?**

A: Passive DR region mein bhi default 1,000 concurrency limit rehta hai jab tak pre-raise na kiya jaaye — warm/active-active strategy ko disaster se *pehle* headroom provision karna padta hai, uske dauraan nahi.

### Testing Resilience: Fault Injection Simulator & Resilience Hub

**Q: AWS Fault Injection Service (FIS) kaunse faults inject kar sakta hai, aur critical safety feature kya hai?**

A: EC2 stop/terminate karna, API calls throttle/fail karna, CPU/memory/disk/network stress, network latency/packet loss, RDS fail over karna, ECS/EKS tasks/pods kill karna, poore AZ ka unavailable ho jaana simulate karna. Safety feature: **stop conditions** — agar nominated CloudWatch alarm breach ho jaaye toh FIS automatically abort ho jaata hai, taaki test khud outage na ban jaaye.

**Q: AWS Resilience Hub kya karta hai, aur iska sabse common real finding kya hai?**

A: Ek app ko stated RTO/RPO targets ke against assess karta hai, resilience score karta hai, gaps flag karta hai (single-AZ DB behind multi-AZ app tier, missing backups), aur fixes validate karne ke liye FIS experiment templates + CloudWatch alarms generate karta hai. Sabse common real finding: **ASG health-check type `ELB` ke jagah `EC2` par choda hua**, isliye hung application kabhi replace nahi hota.

**Q: "how do you know your DR plan actually works" ka complete answer kya hai?**

A: "Har workload ke liye RTO/RPO define karo, jo unhe meet kare woh DR strategy pick karo, phir prove karo — Resilience Hub se targets ke against assess karo, FIS se safety ke liye CloudWatch stop conditions ke saath actual failure inject karo, aur schedule par GameDays karo, kyunki ek saal se exercise na hua DR plan ek hypothesis hai, capability nahi."

---

# PART IV — Cross-Cutting Reference

## Best Practices

**Q: Core Lambda/serverless best practices kya hain?**

A: Ek function = ek responsibility, koi "fat Lambda" business logic nahi; infra ko IaC se manage karo, console clicks se nahi; latency-sensitive APIs ke liye Provisioned Concurrency + .NET AOT; long-running work ko Step Functions mein externalize karo; VPC usage minimize karo jab tak private resource access na chahiye ho (VPC ke andar ek baar aa jaane par VPC Endpoints prefer karo); handler ke bahar DB connections/clients create karo.

**Q: Core messaging best practices kya hain?**

A: Hamesha at-least-once delivery ke liye design karo — idempotency optional nahi hai; SNS fan-out topic ke neeche ek SQS queue per consumer; messages ko successful processing ke baad hi delete karo; visibility timeout ko max processing time ke (thoda zyada) align karo; strict ordering/exactly-once matter karne par FIFO + `MessageGroupId`/`MessageDeduplicationId` use karo.

**Q: Core IAM, data, aur networking best practices kya hain?**

A: **IAM** — kisi bhi automated cheez ke liye roles, users nahi, static keys ke jagah OIDC, incrementally least privilege, per-service ek role. **Data** — DynamoDB: pehle access patterns, phir keys/indexes; RDS: HA (Multi-AZ) aur read-scaling (replicas) ko separate rakho; S3: actual access pattern se storage class/lifecycle pick karo, confusion ho toh Intelligent-Tiering. **Networking** — HA ke liye per-AZ NAT Gateway, AWS-service traffic ke liye NAT ke upar VPC Endpoints, databases hamesha private/isolated subnets mein, SGs primary defense ke roop mein.

## Common Pitfalls (Cross-Cutting)

**Q: Ready rakhne wale top cross-cutting AWS pitfalls list karo.**

A:

- AWS async messaging (Lambda/SQS/SNS sab at-least-once hote hain) mein kahin bhi exactly-once assume karna.
- Route 53 DNS failover instant hai yeh assume karna (yeh TTL-bound hota hai).
- RDS mein Multi-AZ (HA) ko Read Replicas (scale) ke saath confuse karna.
- Fargate ko EC2 se "always cheaper" maan lena (sirf bursty/low-utilization ke liye true hai).
- NAT Gateway ko public subnet mein rakhna bhool jaana, ya VPC-bound Lambda/CodeBuild/ECS ko outbound internet ke liye isse bhool jaana.
- DB transaction commit hone se pehle domain events publish karna.
- Roles/OIDC ke jagah credentials hardcode karna.
- IAM trust policy aur permission policy ke beech separation ignore karna.
- Hot path mein DynamoDB Scan use karna, ya ek low-cardinality/time-based partition key.
- Bina traces (X-Ray) ke *kyun* explain karne, sirf CloudWatch alarms ko "observability" maan lena.

## Sample Interview Q&A

**Q: .NET system ke liye AWS par ek resilient order-processing pipeline design karke walk through karo.**

A: API Gateway/ALB → Lambda/ECS ingest DynamoDB mein status `PENDING` ke saath likhta hai (conditional write ke through idempotency key) → SNS par `order_created` publish karta hai → per-consumer SQS queues (billing, notification, inventory) mein fan out hota hai → har worker apni queue process karta hai state transitions ke liye DynamoDB conditional updates ke saath, DLQ (`maxReceiveCount`) + DLQ depth/age par CloudWatch alarms ke saath. Downstream sab kuch idempotent hai kyunki SQS at-least-once hai. Structured logs + X-Ray ko ek correlation ID se tie karke instrument karo; go-live se pehle DynamoDB capacity mode aur Lambda concurrency sizing validate karne ke liye load-test karo.

**Q: Ek Lambda-backed API mein cold starts se unacceptable p99 latency hai — fix order kya hai?**

A: Confirm karo ki yeh actually cold starts hi hain (CloudWatch REPORT line mein `Init Duration`) → .NET 8 Native AOT par migrate karo → package size trim karo, agar zarurat nahi hai toh VPC attachment remove karo (ya zarurat hai toh VPC Endpoints add karo) → p95 ke size ka Provisioned Concurrency add karo → phir se measure karo. Cheaper architectural fixes rule out karne se pehle directly Provisioned Concurrency par mat jao, kyunki iska ongoing hourly cost hota hai.

**Q: Naye .NET service ke liye RDS ke jagah DynamoDB kab choose karoge, aur kab nahi?**

A: DynamoDB jab access patterns pehle se pata hote hain, high/spiky scale par single-digit-ms latency chahiye, aur model denormalization tolerate karta hai. RDS/Aurora jab domain ko relational integrity, ad hoc queries/reporting chahiye, ya existing EF Core tooling relational ko lower-risk path banata hai. Ek reporting-heavy back office system par DynamoDB ko sirf isliye force mat karo kyunki yeh "cloud-native" hai — yeh cargo-culting hai, architecture nahi.

**Q: AWS Trust Policy aur Permission Policy ko ek document ke jagah separate kyun rakhta hai?**

A: Practice mein yeh alag-alag questions ka answer dete hain jinke alag owners hote hain — ek security team trust boundaries/cross-account access own kar sakti hai, jabki ek service team apni service ka role kya touch kar sakta hai woh own karti hai. "kya enter kar sakta hai" ko "kya kar sakta hai" ke saath ek document mein merge karna do different threat models ko conflate kar dega, jo AWS API level par disallow karta hai.

**Q: Ek DynamoDB table throttle ho raha hai jabki total consumed capacity provisioned limit se kaafi neeche dikh raha hai — kyun, aur fix kya hai?**

A: Classic hot partition — DynamoDB limits per-partition enforce karta hai, per-table nahi, isliye ek key value par concentrated traffic throttle karta hai chahe aggregate capacity fine dikhe. Adaptive Capacity automatically smooth kar deta hai lekin bad key design ka fix nahi hai; real fix hai partition key ko higher cardinality ke liye redesign karna (random/bucketed suffix) ya ek better-distributed GSI.

**Q: ECS/Fargate vs Lambda ko ek non-technical stakeholder ko explain karo.**

A: "Lambda ek car rent karne jaisa hai sirf jab drive karni ho — per trip pay karo, koi maintenance nahi, lekin agar recently drive nahi ki hai toh 'engine start karne' ka ek moment (cold start), aur koi trip 15 minutes se lambi nahi. ECS/Fargate ek car lease karne jaisa hai jo hamesha running aur ready rehti hai — koi startup delay nahi, koi trip-length limit nahi, lekin drive na karne par bhi pay karte ho. Spiky short-lived work → Lambda (cheaper, simpler). Steady always-on services → ECS/Fargate (more predictable, cost-effective)."

