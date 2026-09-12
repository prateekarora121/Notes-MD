> **AWS Quick Revision Notes** · [Index](README.md)

# How to Use This Guide: Resume-Aligned Priority Map

**Resume AWS surface (verbatim):** `AWS (Lambda, DynamoDB, EC2, S3)` · `Terraform / CDKTF` · `GitHub Actions (CI/CD)`, summary says *"growing depth in AWS"* (honest calibration, not a hedge). Interviewers drill hardest on what's *claimed*, skim the rest — so revise by tier.

**Tier 1 — bulletproof (explicitly on resume):** Lambda, DynamoDB, Terraform/CDKTF, GitHub Actions→AWS (OIDC), IAM roles/policies, S3, EC2, CloudWatch.

**Tier 2 — reason confidently, be honest about hands-on gaps:** ECS/Fargate, RDS/Aurora, VPC design, SQS/SNS/EventBridge, ALB/ASG, CloudFront, Step Functions, Secrets Manager. Framing that works: *"I haven't operated Fargate in production — I can walk you through how I'd choose it vs EC2 and what I'd validate before committing."* Honest boundary beats a confident wrong answer.

**Tier 3 — recognise, place, one clean sentence:** Kinesis, Amazon MQ, Athena/Redshift/Glue, GuardDuty/Inspector/Macie/Config, Organizations/Control Tower/RAM, Direct Connect/VPN, Snow/DMS/Storage Gateway, Local Zones/Outposts, CloudHSM. Not recognising a name reads as a gap; one accurate sentence reads as breadth.

### Azure → AWS Translation (AZ-900 held; shipped Cosmos DB + Azure Blob at EY)

| Azure | AWS | Worth volunteering |
|---|---|---|
| Cosmos DB | DynamoDB | RU/s ↔ RCU/WCU; both punish a low-cardinality partition key with a hot partition. Cosmos = 5 consistency levels; DynamoDB = eventual or strong only |
| Azure Blob | S3 | Containers↔buckets; tiers↔storage classes; **SAS tokens ↔ pre-signed URLs** |
| Azure Functions | Lambda | Consumption↔standard; Premium pre-warmed↔**provisioned concurrency** |
| App Service | Beanstalk / ECS Fargate | |
| Azure SQL | RDS / Aurora | |
| Azure AD (Entra) | IAM + IAM Identity Center | Entra = IdP; IAM = authz in-account. Bridge = **SAML/OIDC federation** |
| Key Vault | KMS + Secrets Manager | AWS splits keys (KMS) from secrets (Secrets Manager) |
| Azure Monitor/App Insights | CloudWatch + X-Ray | Split: metrics/logs vs tracing |
| ARM/Bicep | CloudFormation | Terraform is portable across both → why it's a durable skill |
| Resource Groups | ~tags + stacks | **Structural difference: AWS isolates with accounts, Azure with subscriptions/RGs** |
| Service Bus | SQS+SNS (or Amazon MQ for AMQP) | |

### Resume Deep-Dives — Follow-Ups to Expect

**Bullet — "Scheduled Lambda jobs for log maintenance + health checks, 99.9% uptime across 12 services":**
- **Scheduling** → **EventBridge scheduled rules** (`cron(0 2 * * ? *)` / `rate(5 minutes)`) or **EventBridge Scheduler** (one-time, time zones, retry/DLQ). Say "EventBridge", not "CloudWatch Events".
- **Log maintenance** → CloudWatch Logs **retention policies** (default = **Never Expire** = silent cost leak), metric filters, subscription filters, export to S3→Glacier.
- **Health checks** → a Lambda publishing a **custom CloudWatch metric** (`PutMetricData`, cheaper via **EMF**); vs **Synthetics canaries** (managed), **Route 53 health checks** (DNS), **ALB target-group checks** (LB layer).
- **Justify 99.9%** = ~43 min/month error budget (`30×24×60×0.001`). Measured, not felt: *denominator* (~420k requests/mo), *definition of success* (200 OK + valid ack < 30s), *arithmetic* (`(421538−388)/421538 = 99.908%`). Distinguish **observed vs committed SLA** (committed was 99.5%) and **our service vs end-to-end** (~99.5% with dealer DMS).
- Trap: *"what if the health-check Lambda fails?"* → monitor the monitor: alarm on `Errors`/`Throttles` **and missing data** (`treat-missing-data: breaching`).

**Bullet — Deployment Dashboard (GitHub Actions + CDKTF/Terraform, −40% manual intervention, 5→3 day release):**
- **Auth: GitHub OIDC assuming an IAM role** — no static keys; trust policy scoped by `sub` to repo + branch/environment. Lead with this.
- **Safety: plan on PR (read-only role) → apply on merge (privileged role) from a saved plan file**; GitHub environment protection rules for prod approval; `tfsec`/`checkov`.
- **State: S3 backend + DynamoDB lock table.** Be ready for "two pipelines apply at once?"
- Know the denominator behind 40% and 5→3.

**Bullet — "DynamoDB-backed microservices":** full drill — access patterns first, PK cardinality/hot partitions, GSI vs LSI, on-demand vs provisioned, Query vs Scan, conditional writes for idempotency, 400 KB item limit, single-table design.

**Bullet — runtime dynamic-mapping compiling DLLs (not AWS but distinctive):** expect `AssemblyLoadContext`/unloadability, memory leak from accumulating assemblies, caching compiled delegates, security of compiling input at runtime, testing.

**60-sec "your AWS experience":** serverless + IaC-centred — .NET 8 microservices with DynamoDB, scheduled Lambda log/health jobs across 12 services, deployment dashboard wiring GitHub Actions → CDKTF/Terraform to provision Lambda/DynamoDB/EC2/S3 (−40% manual, 5→3 days). Growing side: containers + relational (ECS/Fargate, RDS) — can design, haven't operated in prod, rather say so than overstate.

---

[Index](README.md) · [Serverless & Lambda](01-serverless-lambda.md) →
