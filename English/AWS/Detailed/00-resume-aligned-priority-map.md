> **AWS Detailed Guide** · [Index](README.md)

# How to Use This Guide: Resume-Aligned Priority Map

This guide is deliberately broader than my hands-on experience. Interviewers do **not** distribute questions evenly — they drill hardest on what the resume *claims*, and skim the rest. So the sections below are weighted:

**My resume's AWS surface, verbatim:** `AWS (Lambda, DynamoDB, EC2, S3)` · `Terraform / CDKTF (Infrastructure as Code)` · `GitHub Actions (CI/CD)`, with the summary phrased as *"growing depth in AWS"* — which is deliberate and honest, and sets the expectation I should meet rather than oversell.

### Tier 1 — Must be bulletproof (explicitly claimed on my resume)

| Topic | Why | Where |
|---|---|---|
| **Lambda** | Named skill *and* a delivery bullet (scheduled jobs, 12 services, 99.9% uptime) | [Deep Dive](01-serverless-lambda.md#aws-lambda-deep-dive) · [Concurrency](01-serverless-lambda.md#lambda-concurrency-model) · [Scheduled jobs pattern](#resume-deep-dives--the-follow-ups-i-should-expect) |
| **DynamoDB** | Named skill, listed under both Cloud *and* Databases, plus "DynamoDB-backed microservices" | [Deep Dive](02-dynamodb.md#dynamodb-deep-dive) · [Trick Questions](02-dynamodb.md#dynamodb-trick-questions) · [DAX](10-databases-caching-analytics.md#elasticache--caching-patterns) |
| **Terraform / CDKTF** | Headline IaC skill and the tool in my deployment-dashboard bullet | [In Practice](04-iac-cicd.md#terraformcdktf-in-practice--depth-questions-to-expect) · [vs CloudFormation](04-iac-cicd.md#cloudformation-vs-terraformcdktf) |
| **GitHub Actions → AWS** | Named CI/CD skill; the OIDC keyless pattern is the AWS half of it | [IAM Roles / OIDC](03-iam-security.md#iam-roles-policies-assumerole) · [Terraform CI/CD](04-iac-cicd.md#terraformcdktf-in-practice--depth-questions-to-expect) |
| **IAM roles & policies** | Unavoidable — every one of the above needs a role, and it's where most AWS debugging lands | [IAM & Security](03-iam-security.md#iam--security) · [Rapid-Fire](03-iam-security.md#iam-rapid-fire-qa) |
| **S3** | Named skill; also my Terraform state backend | [Buckets & Objects](05-s3.md#s3-buckets--objects) → [S3 Security](05-s3.md#s3-security-encryption--its-four-types) |
| **EC2** | Named skill, provisioned via CDKTF | [Fundamentals](06-ec2-instance-storage.md#ec2-fundamentals) → [Purchasing Options](06-ec2-instance-storage.md#ec2-purchasing-options--the-complete-set) |
| **CloudWatch** | Implied by "log maintenance and health checks, 99.9% uptime" | [Deep Dive](07-observability-monitoring.md#cloudwatch-deep-dive) · [Container Insights & Synthetics](07-observability-monitoring.md#container-insights-the-cloudwatch-agent--proactive-monitoring) |

### Tier 2 — Must reason about confidently, while being honest about hands-on gaps

ECS/Fargate · RDS/Aurora · VPC design · SQS/SNS/EventBridge · ALB/ASG · CloudFront · Step Functions · Secrets Manager. These come up in **design** questions ("how would you architect X?") where reasoning matters more than operational war stories. The guide already flags where I lack hands-on time — see [Fargate/ECS/EKS Trade-offs](09-containers-ecs-fargate.md#fargateecseks-trade-offs--reasoning-without-hands-on-time) and the RDS framing note in [Multi-AZ vs Read Replica](10-databases-caching-analytics.md#multi-az-vs-read-replica--the-1-confused-pair).

**The framing that works** (and which I should use rather than bluffing): *"I haven't operated Fargate in production — my container work has been Docker locally and ECS-adjacent. What I can walk you through is how I'd choose between it and EC2 for this workload, and what I'd want to validate before committing."* Interviewers consistently reward that over a confident wrong answer, and it also protects me from a follow-up I can't survive.

### Tier 3 — Recognise, place, and give one clean sentence

Kinesis · Amazon MQ · Athena/Redshift/Glue · GuardDuty/Inspector/Macie/Config · Organizations/Control Tower/RAM · Direct Connect/VPN · Snow Family/DMS/Storage Gateway · Local Zones/Outposts · CloudHSM. Know what problem each solves and when you'd reach for it. Nobody expects depth here from a .NET full-stack engineer — but **not recognising the name** reads as a gap, whereas one accurate sentence reads as breadth.

### Azure → AWS Translation (I hold AZ-900, and shipped on Cosmos DB + Azure Blob)

My resume shows real Azure delivery (Cosmos DB, Azure Blob Storage at EY) plus AZ-900. Interviewers who spot that **will** ask "you've used Azure — how does that map?" Having the translation ready turns a perceived gap into demonstrated transferable depth.

| Azure (on my resume / AZ-900) | AWS equivalent | Worth noting |
|---|---|---|
| **Cosmos DB** | **DynamoDB** | Both NoSQL with partition keys and provisioned/on-demand throughput. Cosmos offers 5 tunable consistency levels and multi-model APIs; DynamoDB gives you eventual or strong only. **RU/s ↔ RCU/WCU** is the closest analogy, and both punish a low-cardinality partition key with a hot partition |
| **Azure Blob Storage** | **S3** | Containers ↔ buckets; blob tiers (Hot/Cool/Archive) ↔ storage classes; SAS tokens ↔ **pre-signed URLs** — that SAS↔presigned mapping is a strong thing to volunteer |
| Azure Functions | **Lambda** | Consumption plan ↔ standard Lambda; Premium plan pre-warmed instances ↔ **provisioned concurrency** |
| App Service | Elastic Beanstalk / ECS Fargate | |
| Azure SQL Database | **RDS / Aurora** | |
| Azure AD (Entra ID) | **IAM + IAM Identity Center** | Entra ID is an identity *provider*; IAM is authorisation *within* an account. Entra ID → AWS via **SAML/OIDC federation** is the real-world bridge |
| Azure Key Vault | **KMS + Secrets Manager** | AWS splits it: KMS for keys, Secrets Manager for secrets |
| Azure Monitor / App Insights | **CloudWatch + X-Ray** | Again split: metrics/logs vs distributed tracing |
| Azure DevOps Pipelines | CodePipeline / **GitHub Actions** | |
| ARM / Bicep | **CloudFormation** (and Terraform works against both) | The reason Terraform is a portable skill — same tool, both clouds |
| Resource Groups | *No direct equivalent* — closest is **tags + CloudFormation stacks**; account/OU boundaries do the heavy isolation | A genuine structural difference worth naming: AWS isolates with **accounts**, Azure with subscriptions/resource groups |
| Azure Service Bus | **SQS + SNS** (or **Amazon MQ** for AMQP compatibility) | Service Bus queues+topics map to SQS+SNS; if the app speaks AMQP natively, Amazon MQ is the lift-and-shift path |

### Resume Deep-Dives — The Follow-Ups I Should Expect

Each resume bullet invites a specific technical drill-down. These are the ones where a vague answer would undercut the claim.

Each bullet below is also repeated at the end of its own topic section, so a revision pass through Lambda, DynamoDB, IaC, or Observability ends on the resume question that section has to answer: [Lambda](01-serverless-lambda.md#resume-follow-ups--the-scheduled-lambda-jobs-999-uptime-bullet) · [DynamoDB](02-dynamodb.md#resume-follow-ups--the-dynamodb-backed-microservices-bullet) · [Deployment Dashboard](04-iac-cicd.md#resume-follow-ups--the-deployment-dashboard-bullet) · [Measuring 99.9%](07-observability-monitoring.md#resume-follow-ups--measuring-the-999-claim)

**Bullet: "Build scheduled AWS Lambda jobs for automated log maintenance and health checks, sustaining 99.9% uptime across 12 platform services."**

Expect: *how are they scheduled? what does "log maintenance" mean? what does a health check actually check? how does that produce 99.9%?*

- **Scheduling** — **EventBridge scheduled rules** (`cron(0 2 * * ? *)` / `rate(5 minutes)`), or **EventBridge Scheduler** for the newer, higher-scale option with one-time schedules, time zones, and built-in retry/DLQ. Say EventBridge, not "CloudWatch Events" — same service, current name.
- **Log maintenance** — the concrete levers are **CloudWatch Logs retention policies** (log groups default to **Never Expire**, which is a silent, unbounded cost leak — setting retention is often the single biggest CloudWatch saving), **metric filters** to turn log patterns into alarmable metrics, **subscription filters** to stream logs onward, and **export to S3** with lifecycle rules to Glacier for anything needing long retention cheaply.
- **Health checks** — be precise about the layer: a Lambda probing service endpoints and publishing a **custom CloudWatch metric** (`PutMetricData`, or cheaper via **EMF**) is one design; **CloudWatch Synthetics canaries** are the managed version of the same idea; **Route 53 health checks** and **ALB target-group health checks** operate at DNS and load-balancer level respectively. Naming which one and why shows the distinction is understood.
- **Justifying 99.9%** — that's an error budget of roughly **43 minutes of downtime per month**. The credible answer ties the number to measurement: alarms on availability/error-rate metrics (via **Metric Math**, not raw counts), composite alarms to cut noise, SNS to on-call, and a dashboard per service. If asked "how did you *know* it was 99.9%?", the honest answer is which metric was measured over what window — not a marketing figure.

  **Scripted answer — *"how did you know it was 99.9%?"*** Deliver it as a measurement, not a feeling — denominator, definition of success, then the actual arithmetic:

  > "It's a measured number, not a guess. **Denominator:** our flow took roughly 420,000 deal-export requests a month. **Definition of success:** the payload reached the downstream DMS via DataPower and came back `200 OK` with a valid ack inside 30 seconds — everything else counted as a failure, including 5xx, timeouts, and malformed-payload rejects. **The arithmetic:** that month `total = 421,538` and `failed = 388`, so `(421538 − 388) / 421538 = 99.908%` — that's why I say 99.9%; it's the number those 388 failures produce.
  >
  > And we **managed** against it: 99.9% means only ~43 minutes of downtime allowed per month (30 × 24 × 60 × 0.001 = 43.2 min). We tracked the error budget, and in any month that burned more than 50% of it, we held feature releases and did reliability work first.
  >
  > One clarification: that's **observed** availability, not a contractual SLA. Our committed SLA was 99.5% and we delivered better than it. It's also our service only — end-to-end, including the dealer's own DMS, it sat closer to 99.5%, because a number of dealer systems go down overnight for maintenance."

  Those last two distinctions — *observed vs committed*, and *our service vs end-to-end* — are the real signal: they show the number was owned rather than over-claimed.
- Likely trap: *"what happens if the health-check Lambda itself fails?"* → the monitor needs monitoring: alarm on the function's `Errors`/`Throttles` **and** on **missing data** (`treat-missing-data: breaching`), because a monitor that stops running silently looks identical to "everything is fine."

**Bullet: "Deployment Dashboard UI integrating GitHub Actions with CDKTF/Terraform for AWS infrastructure provisioning… cutting manual deployment intervention by 40% and release cycle from 5 days to 3."**

Expect: *how does the pipeline authenticate to AWS? how do you prevent a bad apply? who approves prod?*
- **Authentication: GitHub OIDC assuming an IAM role** — no static access keys in GitHub secrets, with the trust policy scoped by `sub` to a specific repo **and branch/environment**. This is the single highest-value thing to lead with, and the JSON is in [IAM Roles](03-iam-security.md#iam-roles-policies-assumerole).
- **Safety: plan on PR (read-only role) → apply on merge (privileged role) from a saved plan file**, plus GitHub **environment protection rules** for a manual prod approval, and `tfsec`/`checkov` gates. See [Terraform in Practice](04-iac-cicd.md#terraformcdktf-in-practice--depth-questions-to-expect).
- **State: S3 backend + DynamoDB lock table** — and be ready for "what if two pipelines run at once?"

**Bullet: "CSRconnect features… DynamoDB-backed microservices."**

Expect the full DynamoDB drill: **access patterns first**, partition-key cardinality and hot partitions, **GSI vs LSI**, on-demand vs provisioned capacity, `Query` vs `Scan`, conditional writes for idempotency, the 400 KB item limit, and single-table design. All covered in [DynamoDB Deep Dive](02-dynamodb.md#dynamodb-deep-dive) and [Trick Questions](02-dynamodb.md#dynamodb-trick-questions) — this is the section to revise hardest, because it's the one AWS service my resume ties to a shipped product.

**Bullet: "Runtime dynamic-mapping mechanism that compiles dealer-specific business logic into in-memory assemblies (DLLs)."**

Not an AWS bullet, but it's the most distinctive engineering claim on the resume and will draw questions — expect probes on `AssemblyLoadContext` and unloadability, memory/leak risk from accumulating assemblies, caching compiled delegates, security of compiling input at runtime, and how it's tested. Worth preparing alongside the AWS material, since a weak answer *here* costs more than a weak answer on Kinesis.

**The 60-second "tell me about your AWS experience" answer:**
> "My hands-on AWS work is serverless and IaC-centred. At Nagarro I build .NET 8 microservices for a Dealership Management System with **DynamoDB**-backed services, run scheduled **Lambda** jobs for log maintenance and health checks across 12 platform services, and built a deployment dashboard that wires **GitHub Actions** to **CDKTF/Terraform** to provision **Lambda, DynamoDB, EC2, and S3** — which cut manual deployment intervention about 40% and took our release cycle from five days to three. Where I'm deliberately still growing is the container and relational side — ECS/Fargate and RDS I can reason about and design with, but I haven't operated them in production, and I'd rather tell you that than overstate it."

That structure — concrete services, a measurable outcome, then an unprompted honest boundary — is what makes the "growing depth in AWS" phrasing on my resume a strength rather than a hedge.

---

[Index](README.md) · [Serverless & Lambda](01-serverless-lambda.md) →
