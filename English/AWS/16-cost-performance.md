> **AWS Detailed Guide** · [Index](README.md) · Part III

# Cost & Performance

### Cost Optimization: Savings Plans, Reserved, Spot

The original notes cover EC2 pricing models in a list but never contrast them for a purchasing-decision interview question ("How would you reduce our AWS compute bill by 30%?") — this section fills that gap directly.

| Option | Commitment | Discount vs On-Demand | Flexibility | Best for |
|---|---|---|---|---|
| On-Demand | None | 0% (baseline) | Full | Unpredictable/dev/test workloads |
| Compute Savings Plans | 1 or 3 yr, $/hr commitment | Up to ~66% | Applies across EC2/Fargate/Lambda, any instance family/region | Steady baseline usage, flexible architecture |
| EC2 Instance Savings Plans | 1 or 3 yr | Higher discount than Compute SP | Locked to instance family in a region | Very stable, known instance family needs |
| Reserved Instances (RI) | 1 or 3 yr | Similar to Instance SP | Least flexible (specific instance attributes) | Legacy — mostly superseded by Savings Plans for new commitments |
| Spot Instances | None | Up to ~90% | Can be reclaimed with ~2-min warning | Fault-tolerant, stateless, batch/CI workloads |

**Senior-level cost strategy talking points:**
- Layer commitments: Savings Plan for your **predictable baseline**, On-Demand for the **variable middle**, Spot for **fault-tolerant burst/batch** capacity — a common "three-tier" cost architecture.
- For Fargate/Lambda-heavy .NET shops, Compute Savings Plans apply even to serverless compute — a frequently-missed lever teams assume is EC2-only.
- Spot is well-suited to CI/CD build agents (CodeBuild self-hosted runners on EC2 Spot), batch ETL, and stateless worker fleets behind SQS — the queue absorbs the reclaim disruption.
- Right-sizing (Compute Optimizer recommendations) and eliminating idle resources (unattached EBS volumes, idle NAT Gateways, over-provisioned RDS instances) is usually higher-ROI than switching pricing models, and is the correct *first* answer before jumping to "buy Savings Plans."
- Cost Explorer + Budgets + anomaly detection should be treated as a required part of any production AWS account, not an afterthought — tie this back to the "cost monitoring & budget alerts" item already flagged in the production-readiness checklist earlier in this guide.

---

← [Management, Organizations & Billing](15-management-org-billing.md) · [Index](README.md) · [Migration & Data Transfer](17-migration-data-transfer.md) →
