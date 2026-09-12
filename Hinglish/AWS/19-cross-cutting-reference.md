> **AWS Detailed Guide** · [Index](README.md) · Part IV

# Cross-Cutting Reference

## Best Practices

**Lambda / Serverless**
- Ek function = ek responsibility; "fat Lambda" business logic se bacho.
- Async programming use karo; functions aur packages ko lightweight rakho.
- Infra ko CloudFormation/SAM/CDK/Terraform se manage karo, console clicks se nahi.
- Latency-sensitive production APIs ke liye Provisioned Concurrency; cold starts ko aur kam karne ke liye .NET AOT.
- Long-running work ko Step Functions mein externalize karo.
- Lambda ke liye VPC usage minimize karo jab tak private resource access zaruri na ho; ek baar VPC ke andar ho to VPC Endpoints prefer karo.
- DB connections/clients handler ke bahar create karo taaki warm reuse survive kar sake.

**Messaging**
- Hamesha at-least-once delivery ke liye design karo — idempotency optional nahi hai.
- SNS fan-out topic ke neeche har consumer ke liye ek SQS queue.
- SQS messages sirf successful processing ke baad delete karo.
- SQS visibility timeout ko consumer ke max processing time ke saath align karo (aur thoda zyada rakho).
- Jab strict ordering/exactly-once matter karta ho to FIFO + `MessageGroupId`/`MessageDeduplicationId` use karo.

**IAM**
- Automated kaam ke liye roles use karo, users nahi; CI/CD ke liye static keys ke jagah OIDC.
- Default se least privilege; permissions incrementally add karo, "just in case" nahi.
- Har service ke liye ek role, clearly named; short session durations; AssumeRole activity par CloudTrail on rakho.

**Data**
- DynamoDB: pehle access patterns design karo, phir keys/indexes — reverse order mein nahi.
- RDS: HA (Multi-AZ) concerns ko read-scaling (read replica) concerns se separate rakho; inhe conflate mat karo.
- S3: storage class aur lifecycle rules ko actual access pattern ke basis par choose karo, guesswork se nahi — agar sure nahi ho to Intelligent-Tiering use karo.

**Networking**
- HA ke liye har AZ mein NAT Gateway; AWS-service-only traffic ke liye NAT ke jagah VPC Endpoints.
- Databases hamesha private (ideally isolated, no-NAT) subnets mein.
- Security Groups primary defense ke roop mein; NACLs sparingly, coarse subnet blocking ke liye.

**Observability**
- Har jagah structured JSON logging; logs aur X-Ray trace IDs ko correlate karo.
- Un metrics par alarms lagao jo actually user pain predict karte hain (queue depth, p99 latency, throttle counts) — sirf CPU% nahi.
- Log retention policies explicitly set karo; "Never Expire" ko silent cost leak ki tarah mat chhodo.

---

## Common Pitfalls (Cross-Cutting)

- **AWS async messaging mein kahin bhi exactly-once assume karna** — Lambda, SQS, aur SNS sab default se at-least-once hote hain; idempotency application ki responsibility hai, platform ki guarantee nahi.
- **Yeh assume karna ki DNS failover (Route 53) instant hai** — yeh TTL-bound hota hai; fast reaction ke liye load-balancer-level health-based routing ke saath combine karo.
- RDS mein **Multi-AZ (HA) ko Read Replicas (scale) ke saath conflate karna** — different mechanisms, different purposes, different failover semantics.
- **Fargate ko EC2 se "always cheaper" maan lena** — yeh sirf bursty/low-utilization workloads ke liye cheaper hota hai.
- **NAT Gateway ka public subnet mein hona bhool jaana**, ya VPC-bound Lambda/CodeBuild/ECS tasks ke liye jinhe outbound internet chahiye, isko poori tarah bhool jaana.
- **DB transaction commit hone se pehle domain events publish karna** — isse "read model eventually committed write ko reflect karta hai" wala invariant toot jaata hai jis par CQRS depend karta hai.
- **Credentials hardcode karna, roles/OIDC use karne ke jagah.**
- IAM mein **trust policy aur permission policy ke beech separation ignore karna** — dono zaruri hain, independently evaluate hote hain.
- **DynamoDB Scan ko hot path mein use karna**, ya low-cardinality/time-based partition key choose karna jo hot partition create karta hai.
- **Yeh believe karna ki CloudWatch alarms hi "observability" hain** traces (X-Ray) ke bina, jo explain karte hain ki metric *kyun* move hua.

---

## Sample Interview Q&A

**Q: Mujhe walk through karo ki tum AWS par .NET system ke liye resilient order-processing pipeline kaise design karoge.**
Jawab: API Gateway/ALB → Lambda ya ECS ingest service DynamoDB mein status `PENDING` ke saath write karta hai (idempotency key conditional write se check hota hai) → SNS topic par `order_created` publish karta hai → SNS per-consumer SQS queues (billing, notification, inventory) mein fan out karta hai → har Lambda/ECS worker apni queue process karta hai, state transitions ke liye DynamoDB conditional updates use karke, DLQ configured hone ke saath (`maxReceiveCount`) aur DLQ depth aur queue age par CloudWatch alarms ke saath. Downstream sab kuch idempotent hai kyunki SQS at-least-once hota hai. Main structured logs + X-Ray tracing ko ek correlation ID se tied karke instrument karunga, aur go-live se pehle DynamoDB capacity mode aur Lambda concurrency sizing validate karne ke liye load-test karunga.

**Q: Tumhare Lambda-backed API mein cold starts ki wajah se unacceptable p99 latency hai. Order mein tum kya karoge?**
Jawab: Pehle confirm karo ki yeh actually cold starts hain (CloudWatch Logs ki `REPORT` line mein `Init Duration`, sirf slow code nahi) → agar already nahi kiya hai to .NET 8 Native AOT par migrate karo → package size trim karo aur VPC attachment remove karo agar strictly zaruri nahi hai (ya agar zaruri hai to VPC Endpoints add karo) → p95 traffic ke size ki Provisioned Concurrency add karo → phir se measure karo. Main cheaper architectural fixes (AOT, VPC removal) ko rule out karne se pehle directly Provisioned Concurrency par nahi jaunga, kyunki Provisioned Concurrency ka ongoing hourly cost hota hai.

**Q: Naye .NET service ke liye tum DynamoDB kab choose karoge RDS ke upar, aur kab nahi?**
Jawab: DynamoDB jab access patterns pehle se known hon, need single-digit-ms latency ho high/spiky scale par, aur data model denormalization tolerate kare (koi complex ad hoc joins/reporting nahi). RDS (ya Aurora) jab domain ko genuinely relational integrity, ad hoc queries, complex joins/reporting chahiye, ya team ke existing tooling/ORM (EF Core) aur skill set relational ko faster, lower-risk path banate hain. Main DynamoDB ko reporting-heavy back office system par force nahi karunga sirf isliye ki yeh "cloud-native" hai — yeh cargo-culting hai, architecture nahi.

**Q: Trust Policy aur Permission Policy ke beech difference explain karo, aur AWS unhe separate kyun rakhta hai.**
Jawab: Trust policy define karti hai ki *kaun* role assume kar sakta hai (principal); permission policy define karti hai ki assume karne ke baad wo role *kya* kar sakti hai. Yeh separate rakhe jaate hain kyunki dono questions ke different threat models hote hain aur practice mein different owners hote hain (ek security team trust boundaries/cross-account access own kar sakti hai, jabki ek service team apni service ke role ka access own karti hai) — inhe ek document mein merge karna "can enter" ko "can do" ke saath conflate kar dega, jo AWS explicitly API level par disallow karta hai.

**Q: Tumhari DynamoDB table throttle ho rahi hai jabki total consumed capacity provisioned limit se kaafi kam lag rahi hai. Kyun, aur kya karoge?**
Jawab: Classic hot partition — traffic ek partition key value par concentrated hai jabki aggregate table-level capacity theek lag rahi hai, kyunki DynamoDB limits per-partition enforce karta hai, sirf per-table nahi. Adaptive Capacity isko automatically smooth karne mein help karti hai lekin bad key design ka fix nahi hai. Real fix higher cardinality ke liye partition key redesign karna hai (jaise random/bucketed suffix add karna) ya us access pattern ke liye better-distributed key wali GSI introduce karna hai.

**Q: Tum ECS/Fargate aur Lambda ke trade-off ko naye .NET microservice ke liye ek non-technical stakeholder ko kaise explain karoge?**
Jawab: Lambda ek car rent karne jaisa hai sirf jab tumhe drive karna ho — per trip pay karte ho, koi maintenance nahi, lekin har baar jab tumne recently drive nahi ki ho to "engine start karne" ka ek moment hota hai (cold start), aur tum 15 minutes se longer trip nahi le sakte. ECS/Fargate ek car lease karne jaisa hai jo hamesha running aur ready hai — koi start-up delay nahi aur trip-length limit nahi, lekin tum us minutes ke liye bhi pay kar rahe ho jab tum drive nahi kar rahe. Spiky, short-lived work ke liye Lambda cheaper aur simpler hai; steady, always-on services ke liye ECS/Fargate zyada predictable aur cost-effective hai.

---

← [Well-Architected & Resilience](18-well-architected-resilience.md) · [Index](README.md) · [Changelog — Summary of Additions](20-changelog-additions.md) →
