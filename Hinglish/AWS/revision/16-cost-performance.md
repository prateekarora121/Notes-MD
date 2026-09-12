> **AWS Quick Revision Notes** · [Index](README.md) · Part III

# Cost & Performance

### Cost Optimization: Savings Plans, Reserved, Spot
| Option | Commit | Discount | Best |
|---|---|---|---|
| On-Demand | none | 0% | unpredictable/dev |
| Compute SP | 1-3yr $/hr | ~66% | steady baseline, EC2+Fargate+Lambda flexible |
| EC2 Instance SP | 1-3yr | higher | stable known family |
| RI | 1-3yr | ~Instance SP | legacy |
| Spot | none | ~90% | fault-tolerant batch/CI |

- **Layer:** SP (baseline) + On-Demand (variable) + Spot (burst/batch) = three-tier.
- Compute SP applies to Fargate/Lambda (missed lever). Spot for CI runners, ETL, SQS worker fleets.
- **Right-sizing + eliminate idle (unattached EBS, idle NAT, over-provisioned RDS) usually higher-ROI than switching pricing models** — correct *first* answer before "buy Savings Plans."

---

← [Management, Organizations & Billing](15-management-org-billing.md) · [Index](README.md) · [Migration & Data Transfer](17-migration-data-transfer.md) →
