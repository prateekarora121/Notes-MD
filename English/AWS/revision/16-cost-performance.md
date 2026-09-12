> **AWS Quick Revision Notes** · [Index](README.md) · Part III

# Cost & Performance

### Cost Optimization: Savings Plans, Reserved, Spot
| Option | Commitment | Discount | Flexibility | Best for |
|---|---|---|---|---|
| On-Demand | None | 0% | Full | Unpredictable/dev/test |
| Compute SP | 1–3y $/hr | ~66% | EC2+Fargate+Lambda, any family/region | Steady baseline, flexible |
| EC2 Instance SP | 1–3y | Higher | Locked family+region | Very stable known family |
| RI | 1–3y | ~Instance SP | Least flexible | Legacy (SPs supersede) |
| Spot | None | ~90% | 2-min reclaim | Fault-tolerant/batch/CI |

Strategy: **three-tier** — SP for predictable baseline, On-Demand for variable middle, Spot for fault-tolerant burst. Compute SP applies to serverless (missed lever). Spot for CI runners / batch / SQS-fronted workers. **Right-sizing + killing idle (unattached EBS, idle NAT, over-provisioned RDS) beats switching pricing models first.** Cost Explorer + Budgets + Anomaly = required in prod.

---

← [Management, Organizations & Billing](15-management-org-billing.md) · [Index](README.md) · [Migration & Data Transfer](17-migration-data-transfer.md) →
