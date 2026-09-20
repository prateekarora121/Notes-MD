> **AWS Detailed Guide** · [Index](README.md) · Part III

# Cost & Performance

### Cost Optimization: Savings Plans, Reserved, Spot

Original notes EC2 pricing models ko ek list mein cover karte hain lekin unko kabhi ek purchasing-decision interview question ke liye contrast nahi karte ("Hamara AWS compute bill 30% kaise reduce karoge?") — yeh section us gap ko directly fill karta hai.

| Option | Commitment | Discount vs On-Demand | Flexibility | Best for |
|---|---|---|---|---|
| On-Demand | None | 0% (baseline) | Full | Unpredictable/dev/test workloads |
| Compute Savings Plans | 1 or 3 yr, $/hr commitment | Up to ~66% | EC2/Fargate/Lambda ke across apply, koi bhi instance family/region | Steady baseline usage, flexible architecture |
| EC2 Instance Savings Plans | 1 or 3 yr | Compute SP se higher discount | Region mein instance family se locked | Very stable, known instance family needs |
| Reserved Instances (RI) | 1 or 3 yr | Instance SP jaisa | Least flexible (specific instance attributes) | Legacy — naye commitments ke liye mostly Savings Plans se superseded |
| Spot Instances | None | Up to ~90% | ~2-min warning ke sath reclaim ho sakta hai | Fault-tolerant, stateless, batch/CI workloads |

**Senior-level cost strategy talking points:**
- Commitments ko layer karo: tumhari **predictable baseline** ke liye Savings Plan, **variable middle** ke liye On-Demand, **fault-tolerant burst/batch** capacity ke liye Spot — ek common "three-tier" cost architecture.
- Fargate/Lambda-heavy .NET shops ke liye, Compute Savings Plans serverless compute pe bhi apply hote hain — ek frequently-missed lever jo teams EC2-only assume karte hain.
- Spot CI/CD build agents (CodeBuild self-hosted runners EC2 Spot pe), batch ETL, aur SQS ke peeche stateless worker fleets ke liye well-suited hai — queue reclaim disruption absorb kar leti hai.
- Right-sizing (Compute Optimizer recommendations) aur idle resources eliminate karna (unattached EBS volumes, idle NAT Gateways, over-provisioned RDS instances) usually pricing models switch karne se higher-ROI hai, aur "Savings Plans khareedo" pe jump karne se pehle correct *first* answer hai.
- Cost Explorer + Budgets + anomaly detection ko kisi bhi production AWS account ka required hissa treat karna chahiye, afterthought nahi — isko us "cost monitoring & budget alerts" item se tie back karo jo pehle production-readiness checklist mein flag kiya gaya hai.

---

← [Management, Organizations & Billing](15-management-org-billing.md) · [Index](README.md) · [Migration & Data Transfer](17-migration-data-transfer.md) →
