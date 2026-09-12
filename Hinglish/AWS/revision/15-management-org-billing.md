> **AWS Quick Revision Notes** · [Index](README.md) · Part III

# Management, Organizations & Billing

---

## 1. Organizations & Governance

### Organizations

- Central management: management (payer) account + OUs + member accounts.
- **Multi-account why:** **account = strongest isolation boundary** (compromised dev can't touch prod), blast-radius, per-account quotas, cost attribution.
- Gives: consolidated billing, SCPs, org CloudTrail, RAM, delegated admin, tag/backup/AI-opt-out policies.
- **Best practice: no workloads in management account** (SCPs can't restrict it — most privileged).

### SCPs

- **Filters, never grants** — define max permissions (still need IAM policy). Apply to OU/account, inherited. **Restrict member root user** (real guardrail). ❗ **Management account exempt** (most-asked gotcha). `FullAWSAccess` default → deny-list (common) vs allow-list. Typical: deny non-approved regions, deny org leave, deny disable CloudTrail/Config/GuardDuty, deny delete log buckets, deny disable BPA, deny root actions.

### Control Tower

- Automated landing zone: log-archive + audit accounts, Identity Center, org CloudTrail/Config, guardrails. **Guardrails (controls):** preventive (SCP), detective (Config), proactive (CFN hooks); mandatory/strongly-recommended/elective. **Account Factory** (standard baseline vending). Layer over Organizations/Config/CloudTrail/Identity Center/Service Catalog. "Set up secure multi-account?" → Control Tower.

### RAM

- Share specific resources across org (VPC subnets, TGW, Resolver rules, Aurora clusters). Pattern: central **networking account** shares subnets → coherent network, no peering/overlapping-CIDR. RAM shares resource itself; cross-account role shares permission to act.

---

## 2. Billing & Cost Management

### Consolidated Billing

- One bill. Saves via: **aggregated volume discounts** (combined usage → cheaper tiers) + **RI/SP sharing** (unused commitment covers other accounts, can disable per account). Tools: cost allocation tags, cost categories, CUR. ❗ **Tag gotcha:** must activate tags in Billing console, **not retroactive**.

### Cost Tools

- **Cost Explorer:** 13mo history + 12mo forecast; group by service/account/tag; rightsizing + RI/SP recommendations. Deeper → CUR + Athena.
- **Budgets:** cost/usage/RI-SP utilisation/coverage; actual or forecasted alerts. ❗ **Budget Actions** (auto IAM/SCP deny or stop EC2/RDS). First 2 free.
- **Cost Anomaly Detection:** ML on spend, no threshold, root-cause. ❗ **Budgets (limit I defined) vs Anomaly Detection (something weird)** — run both.
- **Trusted Advisor:** 5-6 pillars (cost/perf/security/fault-tolerance/quotas/ops). ❗ **Support-tier gated** (full checks + API need Business/Enterprise). Findings: idle EC2, unassociated EIPs, open 22/3389, no root MFA, exposed keys, open S3. vs Compute Optimizer (deep rightsizing), Cost Explorer (cost analysis), Config (compliance), Security Hub (security).

### Free Tier & Pricing

- Always free (1M Lambda req + 400k GB-s, 25 GB DynamoDB) / 12-month (750h t2.micro, 5 GB S3) / trials.
- ❗ **Free Tier trap:** exceeding just bills. Surprise charges: **NAT Gateway** (~$32/mo), **unattached EIP / public IPv4**, `Never Expire` logs, orphaned snapshots, cross-AZ/egress. Set $1 Budget + Anomaly Detection day one.
- Pricing: pay-as-go, reserve to save, tiered volume, data-in free / data-out charged, cross-AZ charged both ways. Tools: **Pricing Calculator** (prospective) vs **Cost Explorer** (retrospective).

**DR — Organizations:** risk = whole account, SCPs, Control Tower, backups. Backup = **AWS Backup vaults + Vault Lock**, cross-account copies, config IaC, 90-day account-closure window. Closed account → reopen within 90 days; SCP lockout → fix from management account (exempt); compromise → restore to clean account; re-apply baseline. ⚠️ **Backups in same account = not backups** → separate backup account + **Vault Lock compliance mode** (root can't delete); test SCPs on sandbox OU.

---

## 3. Advisory & Support

### Support Plans

| Plan | Support | Notable |
|---|---|---|
| Basic | none | free, PHD, core TA |
| Developer | business-hours email | non-prod |
| **Business** | 24/7 phone/chat, <1h prod-down | **full TA + API**, min for production |
| Enterprise On-Ramp | +TAM pool, <30min | |
| Enterprise | designated **TAM**, <15min | concierge |

Tested: **Business = min for production**; **TAM from Enterprise On-Ramp**.

---

← [Global Edge Services](14-global-edge-services.md) · [Index](README.md) · [Cost & Performance](16-cost-performance.md) →
