> **AWS Detailed Guide** · [Index](README.md) · Part I

# Serverless & Lambda

> **Tier 1 — bulletproof.** Lambda mere resume par named hai *aur* ek delivery bullet ko backup karta hai (scheduled log-maintenance aur health-check jobs 12 platform services ke across, 99.9% uptime). Yahan sabse deep questioning expect karo. Dekho [Resume Deep-Dives](00-resume-aligned-priority-map.md#resume-deep-dives--woh-follow-ups-jo-mujhe-expect-karne-chahiye) un follow-ups ke liye jo woh bullet invite karta hai.

---

## 1. Lambda Core

### AWS Lambda Deep Dive

**Yeh kya hai:** Serverless compute — code upload karo, AWS trigger par run karta hai, aap per-invocation + duration pay karte ho. Koi server/patch/scale management nahi.

**Key characteristics**
- Event-driven (API Gateway, S3, SNS, DynamoDB Streams, SQS, EventBridge, etc.)
- Fully managed, concurrent request ke hisab se auto-scale
- Per invocation count + duration(ms) × memory billed
- Runtimes: Node.js, Python, .NET 6/8 (AOT + arm64), Java, Go, custom runtime via Lambda Runtime API (Rust etc.)

**Real-world use cases**
| Category | Example |
|---|---|
| API backend | Low/medium traffic APIs ke liye API Gateway + Lambda |
| Event processing | S3 upload → resize/scan; DynamoDB Streams → audit log; Kinesis/Kafka → real-time |
| Automation/cron | EventBridge schedule → cleanup Lambda |
| Orchestration glue | Multi-service workflows ke liye Step Functions + Lambda |
| Data transformation | Small/medium ETL, CSV→JSON |
| Edge | CloudFront customization ke liye Lambda@Edge |

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

1. **INIT (cold start):** naya micro-VM, runtime bootstrap, static initializers, DI container build, DB connection setup — sab "handler ke bahar" wale code mein.
2. **INVOKE:** handler event + context ke saath run hota hai; configured timeout ke andar finish hona chahiye (max 15 min).
3. **FREEZE (warm reuse):** response ke baad environment freeze ho jaata hai; globals, connections, `/tmp` persist karte hain — isi liye warm invocations 1–10ms ke hote hain.
4. **SHUTDOWN:** AWS idle/outdated environments reclaim karta hai. Koi shutdown hook nahi — state lost ho jaati hai, rollback nahi hoti.

**INIT decomposed: "runtime bootstrap" vs "static initializers"** — step 1 ke do halves: ek platform ka, ek mera.

| Step | Kya hota hai | Owner |
|---|---|---|
| Micro-VM | Firecracker VM banta hai, deployment package download aur mount hota hai | AWS |
| Runtime load | .NET ke liye: **CoreCLR** start hota hai — GC heap reserve, thread pool spin up, JIT initialise, `AssemblyLoadContext.Default` create | .NET |
| Runtime client | `Amazon.Lambda.RuntimeSupport` **Runtime API** (`/runtime/invocation/next`) poll karna shuru karta hai — yeh loop hi ek process ko Lambda banata hai | AWS runtime |
| Assembly load | Meri DLL load hoti hai, handler type aur method reflection se resolve hote hain (Native AOT ke bina) | .NET |
| **Static initializers** | Mere `static` fields aur static constructors — CLR unhe **per type per load context ek baar**, thread-safely, first access se pehle chalata hai. Handler ke bahar ka sab kuch yahan pay hota hai; andar ka sab kuch per invocation | Mera |

**One-line answer:** "Runtime bootstrap INIT ka platform-side half hai — micro-VM, CoreCLR, JIT, runtime client ka poll loop — aur isi liye .NET cold starts 300–1500ms lagte hain jahan Node 50–200ms leta hai. Static initializers mera half hain: CLR unhe per type per load context ek baar, thread-safely, first access se pehle chalata hai, toh handler ke bahar ka sab kuch per environment ek baar pay hota hai aur warm invokes par reuse hota hai. Trap yeh hai ki jo static initializer throw karta hai, woh us environment ki poori life ke liye type ko poison kar deta hai."

**Cold start vs warm start**
- Cold: Java/.NET (JIT) → 300–1500ms; Node/Python → 50–200ms; **.NET Native AOT** → 50–100ms (JIT-based .NET par huge win).
- Warm: 1–10ms — reused environment, init code already execute hua hota hai.
- Mitigations: .NET AOT, small deployment package, heavy DI container graphs avoid karo, VPC avoid karo jab tak required na ho, latency-sensitive APIs ke liye Provisioned Concurrency use karo.

**Triggers**
| Type | Examples | Failure semantics |
|---|---|---|
| Synchronous | API Gateway, ALB, Step Functions, direct Invoke | Caller ko error dikhta hai; caller ko retry karna padta hai |
| Asynchronous | S3, SNS, CloudWatch Events, SES, EventBridge | Lambda automatically retry karta hai; exhaustion par DLQ |
| Poll-based (event source mapping) | DynamoDB Streams, Kinesis, MSK, SQS | Lambda internally poll karta hai; success ya maxReceiveCount/DLQ tak retry |

**Fayde**
- Zero server management, auto-scale, pay-per-use, default se multi-AZ, deep AWS integration, trivial deployment (zip/image).

**Nuksan / kab NAHI use karna chahiye**
- Cold start latency <50ms SLA APIs ke tail ke liye unsuitable
- 15-minute hard timeout — koi long-running jobs nahi
- AWS event model ka vendor lock-in
- Max 10GB memory, 10GB ephemeral `/tmp` — ML training/huge batch ke liye nahi
- Distributed functions ke across observability harder (X-Ray + structured logging chahiye)

**Cost model:** invocations + GB-seconds (duration × memory) + optional Provisioned Concurrency hourly charge + AWS network ke bahar data transfer.

**[new content] .NET-specific Lambda considerations**
- Latency-sensitive Lambdas ke liye **.NET 8 Native AOT** prefer karo — JIT warm-up eliminate karta hai, smaller deployment package, lekin: koi runtime reflection-based DI magic nahi (source-generated JSON serialization required hai `System.Text.Json` source generators ke through), kuch third-party libraries jo reflection use karti hain trimming ke under break kar sakti hain.
- `Amazon.Lambda.AspNetCoreServer` aapko API Gateway/ALB ke peeche almost koi code changes ke bina ek full minimal API/ASP.NET Core app host karne deta hai — existing Web APIs ke lift-and-shift ke liye useful, heavier cold start ke cost par pure function handler se.
- Agar zaroorat nahi hai to har cold start par full `IServiceCollection`/`IServiceProvider` graph build karne se bacho — dependencies manually construct karo ya `IServiceProvider` ko static field ke roop mein cache karo taaki yeh warm invocations ke across survive kare.
- Lambda ke andar EF Core: `DbContext`/connection pool handler ke **bahar** create karo (static/singleton) taaki warm invocations ke across reuse ho sake; high concurrency par RDS Proxy ya connection pool exhaustion se careful raho (har concurrent execution environment = apna alag connection footprint).

**Versions, aliases & layers — deployment surface** (kisi bhi "Lambda ko safely kaise deploy karte ho?" question ke liye zaroori):
- **`$LATEST`** mutable hai; **version publish karna** code + configuration ka ek immutable, numbered snapshot banata hai.
- Ek **alias** ek named, movable pointer hota hai ek version ki taraf (`prod` → v7). Aliases do versions ke across **weighted routing** support karte hain, jo ki **canary/linear deploy** karne ka tarika hai — 10% traffic v8 ko shift karo, CloudWatch alarms dekho, phir complete karo ya alias move karke rollback karo. **CodeDeploy** exactly isi ko automate karta hai `Canary10Percent5Minutes`-style configs aur automatic alarm-triggered rollback ke saath.
- **Layers** shared dependencies ya **Lambda Extensions** (CloudWatch Lambda Insights, Parameters & Secrets extension) ko function code se alag package karte hain — smaller deployment packages aur shared library update karne ki ek jagah. Limit: function ke liye 5 layers, 250 MB unzipped total.
- **Destinations** ek **asynchronous** invocation ke *result* ko route karte hain — `onSuccess` aur `onFailure` — SQS/SNS/EventBridge/Lambda ko. Yeh ek bare DLQ se strictly better hai kyunki record mein **response/error payload aur request context** dono hote hain, sirf original event nahi.
- Async invokes ke liye **`RetryAttempts`** default **2** hai (toh total 3 attempts tak) ek event age limit ke saath — yeh jaanna zaroori hai isse pehle ki aap conclude karo "Lambda ne meri event silently drop kar di".
- **Reserved concurrency** ek function ka share cap *aur* guarantee karta hai; **provisioned concurrency** cold starts remove karne ke liye environments pre-initialise karta hai. **SnapStart** sirf **Java** ke liye cold-start fix hai — .NET ke liye equivalent levers hain **Native AOT**, trimming, aur provisioned concurrency (upar wale .NET considerations dekho).

**Interview-ready lifecycle answer:** "Har Lambda invocation ek Firecracker micro-VM mein chalta hai. Cold start par AWS VM provision karta hai, runtime load karta hai aur handler chalne se pehle global/static initializers execute karta hai. Handler return hone ke baad, AWS us environment ko freeze karke reuse kar sakta hai next invocation ke liye (warm start) — yeh reuse ek optimization hai, guarantee nahi. Idle ya replace kiye gaye environments bina shutdown hook ke torn down hote hain, isliye koi unflushed state lost ho jaati hai."

### Lambda Concurrency Model

**Reserved vs Provisioned Concurrency**

| | Reserved Concurrency | Provisioned Concurrency |
|---|---|---|
| Purpose | Ek function ke liye capacity guarantee + cap | Cold starts eliminate karna |
| Mechanism | Account/region concurrency pool ka hissa carve karta hai | N warm environments pre-initialize karta hai |
| Cost | Extra nahi | Invocation ho ya na ho, hourly billed |
| Effect above limit | Throttled | Normal (cold) scaling par fall back karta hai |

One-liner: **Reserved = capacity guarantee. Provisioned = cold starts eliminate.**

**Burst scaling rules**
- Default regional concurrency limit: 1,000 concurrent executions (increasable).
- Burst behavior: pehle ~1,000 concurrent executions instantly scale hoti hain; uske baad, account/region limit hit hone tak +500 new environments/minute.
- Concurrency limit = **kitna** scale kar sakte ho; burst rate = **kitni fast**.

**Multi-region concurrency**
- Har region ka independent concurrency pool aur independent burst behavior hota hai.
- Active-active: traffic split karo aur reserved/provisioned concurrency separately per region provision karo.
- Active-passive (DR): failover se **pehle** DR region mein concurrency limit pre-raise karo — default limits ke saath ek cold DR region failover load ke under throttle karega.

**Sizing formula**

```
Required Concurrency ≈ Peak RPS × Avg Duration (seconds) × Safety Factor (1.3–2.0)
```

Example: 500 msgs/sec × 1.2s duration ≈ 600 concurrency (safety factor se pehle).

- Latency-critical APIs: Reserved ≈ required concurrency; Provisioned ≈ p95 load.
- SQS-driven async Lambdas: aap concurrency ko theoretical peak se neeche *cap* kar sakte ho — SQS backlog ko back-pressure ke roop mein absorb kar leta hai, downstream DBs ko protect karta hai.

**Interview traps aur correct answers (condensed)**
| Trap question | Correct senior answer |
|---|---|
| Lambda 2nd time faster kyun chala? | Warm start — execution environment reused hua; "logic" ka caching nahi. |
| Fast code hone ke bawajood timeout kyun hua? | Timeout wall-clock hota hai, network waits (DB, downstream API) bhi include karta hai, sirf CPU nahi. |
| SQS message reprocess kyun hua? | Message sirf successful execution ke baad delete hota hai; failures ke case mein visibility timeout ke baad wo phir visible ho jaata hai. |
| DB write ke baad Lambda crash ho gaya — rollback? | Koi transaction awareness nahi hai. Partial writes persist karte hain; idempotency required hai. |
| Kya Lambda exactly-once guarantee kar sakta hai? | Nahi — sirf at-least-once. Idempotency mandatory hai, optional nahi. |
| VPC mein Lambda slower kyun hai? | ENI attachment cold-start latency add karta hai (2019 ke Hyperplane ENI improvements se significantly mitigate hua hai, lekin abhi bhi non-zero hai, especially infrequently-invoked functions ke liye). |
| Lambda business logic hold kare kya? | Nahi — "fat Lambda" anti-pattern hai; orchestrate/validate karo aur testable services/libraries ko delegate karo. |
| Lambda forever chal sakta hai kya? | Nahi — 15-minute hard cap; longer work ke liye Step Functions/ECS/Batch use karo. |

**ENI vs VPC Endpoint (mental model)**
- **ENI**: network interface jo Lambda VPC ke andar rakhne par attach hota hai — private resources (RDS, internal ALB) tak reach karne ke liye required. Cold-start overhead add karta hai.
- **VPC Endpoint** (S3/DynamoDB ke liye Gateway, zyadatar ke liye Interface/PrivateLink): ek VPC-bound Lambda ko NAT/internet ke **bina** AWS services tak reach karne deta hai — lower latency, lower cost, no public exposure.
- Rule: ENI sirf tab use karo jab private VPC resources tak pahunchna zaroori ho; VPC Endpoints use karo NAT costs/latency avoid karne ke liye jab aap already VPC mein ho.

---

## 2. Choosing Compute

### Lambda vs ECS vs Fargate

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

| Dimension | Lambda | ECS on EC2 launch type | ECS/EKS on Fargate |
|---|---|---|---|
| Unit of deployment | Function (zip ya image) | Meri instances par container task | Container task, koi instance nahi |
| Capacity management | Koi nahi | Meri — ASG, AMIs, patching, bin-packing | Koi nahi (AWS ki) |
| Max run time | 15 min | Unbounded | Unbounded |
| Start-up cost | Cold start (ms–s) | Instances warm hone ke baad koi nahi; cluster badhana pade to slow | Task start ~30–60s (image pull + ENI attach) |
| Scaling speed | Seconds, per request | Sabse slow — nayi instance boot ho kar cluster join kare | Beech mein — per task, koi instance boot nahi |
| Pricing | Per invocation + GB-second | Per instance-hour, task density chahe kuch bhi ho; Spot/RI/Savings Plans apply | Running rehne tak per task vCPU/GB-second |
| Host access | Koi nahi | Full — SSH, daemons, GPU, custom kernel | Koi nahi — na host, na privileged mode, na daemonsets, na GPU |
| Best for | Spiky/event-driven, glue code | GPU, custom AMI, high steady density, tight cost tuning | Predictable microservices, bina capacity ops |

**Key senior talking points**
- **ECS orchestrator hai; Fargate uske liye ek capacity provider hai** (aur EKS ke liye bhi) — yeh competing products nahi hain. Asli comparison hai ECS *on the EC2 launch type* vs ECS *on Fargate*; "ECS vs Fargate" ko do rival products ki tarah treat karna ek common slip hai.
- **Deciding axis hai capacity kiski hai** — "capacity" matlab woh actual EC2 instances jinpe containers chalte hain. Lambda pe koi host dikhta hi nahi; Fargate pe host hota hai lekin AWS ka hai; EC2 launch type pe instances mere hi VPC aur ASG mein baithte hain, jahan main SSH kar sakta hoon. Unhe own karna hi GPU instance types, custom ya hardened AMI, per-host agents (ECS `DAEMON` strategy, EKS DaemonSets), aur bin-packing ke saath Spot/RI pricing ko **possible banata hai** — inmein se koi bhi choice Fargate deta hi nahi. Aur own karne ki keemat hai AMI patching, slower scaling, aur idle instances ka bill.
- Lambda "speed, scale, minimal ops ke liye optimize karta hai"; containers "control, predictability, long-running work ke liye optimize karte hain." Sahi choice workload-shape-driven hai, preference-driven nahi.
- Lambda aur ECS/Fargate dono SQS se triggered/consume ho sakte hain.
- **Cost inversion:** low/spiky traffic par Lambda sabse sasta, Fargate beech mein, aur steady high density par EC2 launch type jeetta hai — kyunki aap tasks ke bajaye instances ke liye pay karte ho, aur uske upar Spot aur Savings Plans layer kar sakte ho.
- **Scaling speed capacity ke hi order mein aati hai:** Lambda seconds mein, Fargate per task, EC2 launch type sabse slow kyunki nayi instance ko boot hona, storage attach karna, aur cluster mein register hona padta hai.
- **IAM differ karta hai:** Lambda ek *execution role* use karta hai; ECS tasks ek *task role* (app permissions) plus ek alag *task execution role* (images pull karna, logs likhna) use karte hain — in do ECS roles ko confuse karna classic trap hai.

---

## 3. Event-Driven Patterns

### Serverless & S3 → Lambda Trigger Pattern

**"Serverless" ka matlab kya hai:** koi server provision ya patch karne ki zarurat nahi, zero se automatic scaling, **sirf jo use karo uska pay karo**, aur high availability built-in. AWS serverless set jo naam lene layak hai: **Lambda**, **Fargate**, **S3**, **DynamoDB**, **SQS/SNS/EventBridge**, **API Gateway**, **Step Functions**, aur **Aurora Serverless v2**.

**Canonical S3-triggers-Lambda flow** (thumbnail generation, virus scanning, CSV ingestion, document processing):
1. `s3:ObjectCreated:*` par ek **S3 event notification** configure karo, optionally prefix aur suffix se filtered (`uploads/`, `.csv`).
2. S3 Lambda ko ek event ke saath invoke karta hai jismein **bucket name aur object key** hoti hai — khud object nahi, isliye function `GetObject` se usse fetch karta hai.
3. Function ko ek **resource-based policy** chahiye jo `s3.amazonaws.com` ko usse invoke karne allow kare (console yeh automatically add karta hai), aur uske **execution role** ko source par `s3:GetObject` aur destination par `s3:PutObject` chahiye.

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
`SourceAccount`/`SourceArn` service principals ke liye **confused-deputy** guard hain — dekho [IAM Roles](03-iam-security.md#iam-roles-policies-assumerole).

**Do gotchas jo practically pooche jaana guaranteed hai:**
- **❗ Infinite recursion.** Agar function apna output **same bucket mein wapas** write karta hai ek path ke under jo trigger abhi bhi match karta hai, har write function ko phir se fire karta hai — ek unbounded invocation loop matching bill ke saath. Fix: ek **alag bucket** mein likho, ya ek prefix filter use karo jo output se match na kar sake (`uploads/` in, `processed/` out). AWS ke paas ab recursive-invocation *detection* hai jo loop ko halt karta hai, lekin architecture fix aapka hai.
- **At-least-once delivery.** S3 event notifications **ek se zyada baar** deliver ho sakti hain aur occasionally out of order bhi, isliye handler **idempotent** hona chahiye — work ko object key + ETag/version ID par key karo, aur re-processing ko harmless banao.

**Kab EventBridge ke through route karna hai:** bucket par **EventBridge notifications** enable karna content-based filtering deta hai, ek event ke liye **multiple targets**, DLQ ke saath retries, aur archive/replay — S3 ke native one-destination-per-event-type notifications ke against. Ek simple trigger se aage kuch bhi ho, EventBridge better answer hai (dekho [EventBridge Deep Dive](13-messaging-streaming.md#eventbridge-deep-dive)).

---

## 4. Resume Follow-Ups

### Resume Follow-Ups — "Scheduled Lambda Jobs, 99.9% Uptime" Bullet

> *"Build scheduled AWS Lambda jobs for automated log maintenance and health checks, sustaining 99.9% uptime across 12 platform services."*

Expect karo: *kaise schedule hote hain? health check actually kya check karta hai? yeh 99.9% kaise produce karta hai?* Sab resume bullets ki consolidated list [Resume Deep-Dives](00-resume-aligned-priority-map.md#resume-deep-dives--woh-follow-ups-jo-mujhe-expect-karne-chahiye) mein hai; yahan sirf Lambda-specific parts hain.

- **Scheduling** — **EventBridge scheduled rules** (`cron(0 2 * * ? *)` / `rate(5 minutes)`), ya **EventBridge Scheduler** one-time schedules, time zones, aur built-in retry/DLQ ke liye. EventBridge kaho, "CloudWatch Events" nahi — same service, current name.
- **Health-check function khud** — ek Lambda jo service endpoints probe karta hai aur ek **custom CloudWatch metric** publish karta hai (`PutMetricData`, ya cheaper via **EMF**). Isse managed alternatives ke against place kar sako: **Synthetics canaries** (same idea, managed), **Route 53 health checks** (DNS layer), **ALB target-group checks** (load-balancer layer).
- **12 functions vs ek function ke 12 schedules** — dono defensible hain, lekin reason hona chahiye: ek taraf blast radius aur per-service IAM scoping, doosri taraf kam cold starts aur ek hi deploy artifact.
- **Trap: *"agar health-check Lambda khud fail ho jaaye toh?"*** → monitor ko bhi monitor karna padta hai. Function ke `Errors`/`Throttles` **aur** **missing data** (`treat-missing-data: breaching`) par alarm, kyunki ek monitor jo silently chalna band kar de, "sab theek hai" jaisa hi dikhta hai.
- **99.9% justify karna** — yeh roughly **43 minutes downtime per month** ka error budget hai (`30 × 24 × 60 × 0.001`, jahan `0.001` matlab `1 − SLA` failure fraction). Number measurement se aana chahiye, feeling se nahi:

  > "Yeh measured number hai, guess nahi. **Denominator:** hamare flow mein monthly ~4.2 lakh deal-export requests aate the. **Success ki definition:** DataPower se downstream DMS ko payload gaya aur 30 seconds ke andar `200 OK` + valid ack mila — baaki sab failure count hota tha, 5xx, timeout, aur malformed-payload rejects bhi. **Actual maths:** us month `total = 421,538`, `failed = 388`, toh `(421538 − 388) / 421538 = 99.908%` — isliye main 99.9% keh raha hoon.
  >
  > Aur hum isko **manage** bhi karte the: jis mahine error budget 50% se zyada khatam ho jaata, us mahine feature release rok kar reliability work pehle karte the. Ek clarification — yeh **observed** availability hai, contractual SLA nahi; committed SLA 99.5% tha aur humne usse better diya. Aur yeh sirf hamari service ka number hai. End-to-end, dealer ke apne DMS ke saath, ~99.5% rehta tha, kyunki kai dealer systems raat ko maintenance pe down hote the."

  *Observed vs committed* aur *our-service vs end-to-end* — yehi asli signal hain, yeh dikhate hain ki number owned kiya gaya tha, over-claim nahi. Isko produce karne wali instrumentation [99.9% Claim Ko Measure Karna](07-observability-monitoring.md#resume-follow-ups--999-claim-ko-measure-karna) mein hai.

**Disaster Recovery — Lambda & Serverless**

| | |
|---|---|
| **Actually risk par kya hai** | Lambda ke *andar* kuch durable rehta hi nahi — code Git mein hai, config IaC mein. Aap actually **in-flight events** aur event-source wiring khote ho |
| **Backup mechanism** | Git + built artifact S3/ECR mein; **versions aur aliases** rollback mechanism hain; **DLQ / on-failure destination** hi events ka ek matra backup hai |
| **Realistic RPO / RTO** | Code RPO ~0 (Git). RTO minutes — ek IaC apply. Event RPO = jo DLQ ne pakda |

**Recovery runbook:**
1. **Bad deploy:** redeploy mat karo — alias ko previous version par point kar do. `aws lambda update-alias --name prod --function-version 41`. Instant, aur yehi wajah hai ki deploy alias ke through hota hai, `$LATEST` par nahi.
2. **Regional failure:** wahi module DR region par `terraform apply` karo, phir event source mappings dobara banao (`aws lambda create-event-source-mapping`) — ESMs regional hote hain aur function ka hissa **nahi** hote.
3. **Lost events re-drive karo:** SQS DLQ mein native redrive hai (`aws sqs start-message-move-task`); async DLQ ke liye ek chhota re-invoke consumer.
4. DR region mein **provisioned concurrency** reset karo — wo function ke saath travel nahi karti, aur failover surge par cold starts hi RTO todte hain.

⚠️ **Gotcha:** **pehle se DLQ configure nahi kiya to outage ke dauraan khoye events bina kisi record ke chale jaate hain** — Lambda async par do baar retry karta hai aur phir drop kar deta hai. Aur DynamoDB/Kinesis stream trigger ki retention sirf **24 ghante** hai: usse lambi outage change events **permanently** kho deti hai, table backups chahe kitne bhi acche hon.

---

← [Resume-Aligned Priority Map](00-resume-aligned-priority-map.md) · [Index](README.md) · [DynamoDB](02-dynamodb.md) →
