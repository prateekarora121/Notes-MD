> **AWS Quick Revision Notes** · [Index](README.md)

# How to Use This Guide: Resume-Aligned Priority Map

**Idea:** interviewers har service ko equal weight nahi dete — jo resume *claim* karta hai uspar drill karte hain. Resume surface: `AWS (Lambda, DynamoDB, EC2, S3)`, `Terraform/CDKTF`, `GitHub Actions`, "growing depth in AWS".

**Tier 1 — bulletproof (resume par explicitly named):** Lambda · DynamoDB · Terraform/CDKTF · GitHub Actions→AWS (OIDC) · IAM roles/policies · S3 · EC2 · CloudWatch.

**Tier 2 — confidently reason karo, hands-on gaps par honest raho:** ECS/Fargate · RDS/Aurora · VPC design · SQS/SNS/EventBridge · ALB/ASG · CloudFront · Step Functions · Secrets Manager.
- **Framing script:** *"Maine Fargate production mein operate nahi kiya — main isse aur EC2 ke beech kaise choose karunga aur commit se pehle kya validate karunga, woh walk through kar sakta hoon."* Confident wrong answer se better land karta hai.

**Tier 3 — recognise + place + ek clean sentence:** Kinesis · MQ · Athena/Redshift/Glue · GuardDuty/Inspector/Macie/Config · Organizations/Control Tower/RAM · Direct Connect/VPN · Snow/DMS/Storage Gateway · Local Zones/Outposts · CloudHSM.

### Azure → AWS Translation (AZ-900, Cosmos DB + Blob shipped)

| Azure | AWS | Note |
|---|---|---|
| Cosmos DB | DynamoDB | RU/s ↔ RCU/WCU; dono low-cardinality PK ko hot-partition se punish karte hain |
| Blob Storage | S3 | Containers↔buckets; tiers↔storage classes; **SAS tokens ↔ pre-signed URLs** |
| Azure Functions | Lambda | Premium pre-warmed ↔ provisioned concurrency |
| Azure SQL | RDS/Aurora | |
| Azure AD (Entra) | IAM + IAM Identity Center | Entra = IdP; bridge = SAML/OIDC federation |
| Key Vault | KMS + Secrets Manager | AWS split karta hai: keys→KMS, secrets→Secrets Manager |
| Azure Monitor/App Insights | CloudWatch + X-Ray | metrics/logs vs tracing |
| ARM/Bicep | CloudFormation (Terraform dono par) | |
| Resource Groups | tags + stacks; **AWS accounts se isolate** karta hai | structural difference worth naming |
| Service Bus | SQS+SNS (ya MQ for AMQP) | |

### Resume Deep-Dives — expected follow-ups

- **"Scheduled Lambda jobs, log maintenance + health checks, 99.9% uptime across 12 services":**
  - **Scheduling:** EventBridge scheduled rules (`cron(0 2 * * ? *)` / `rate(5 minutes)`) ya EventBridge Scheduler. "EventBridge" kaho, "CloudWatch Events" nahi.
  - **Log maintenance:** CloudWatch Logs **retention** (default **Never Expire** = silent cost leak), metric filters, subscription filters, export→S3→Glacier.
  - **Health checks:** Lambda probe → custom metric (`PutMetricData`/EMF); vs Synthetics canaries (managed), Route 53 checks (DNS), ALB target checks (LB).
  - **99.9% = ~43 min/month error budget** (`30×24×60×0.001`). Measured: `total=421,538, failed=388 → 99.908%`. Distinguish **observed vs committed SLA**, aur **our-service vs end-to-end**.
  - **Trap:** "health-check Lambda khud fail ho jaaye?" → monitor ko monitor karo: `Errors`/`Throttles` + **missing data** (`treat-missing-data: breaching`).
- **"Deployment Dashboard, GitHub Actions + CDKTF/Terraform, 40% less manual, 5→3 days":** auth = **GitHub OIDC assumes IAM role** (no static keys, `sub` scoped to repo+branch); safety = plan-on-PR (read-only role) → apply-on-merge (privileged, saved plan) + environment protection + tfsec/checkov; state = **S3 backend + DynamoDB lock table**; percentages ke denominator pata ho.
- **"DynamoDB-backed microservices":** full drill — access patterns first, PK cardinality/hot partitions, GSI vs LSI, on-demand vs provisioned, Query vs Scan, conditional writes for idempotency, 400 KB limit, single-table design.
- **Non-AWS bullet "runtime-compiled assemblies (DLLs)":** `AssemblyLoadContext` unloadability, memory leaks, compiled-delegate caching, security of compiling input — prepare karo, yahan weak answer Kinesis se zyada cost karta hai.

**60-sec "AWS experience" answer:** concrete services + ek measurable outcome + ek unprompted honest boundary (containers/RDS ko design kar sakta hoon, operate nahi kiya).

---

[Index](README.md) · [Serverless & Lambda](01-serverless-lambda.md) →
