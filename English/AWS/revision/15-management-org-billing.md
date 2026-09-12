> **AWS Quick Revision Notes** · [Index](README.md) · Part III

# Management, Organizations & Billing

---

## 1. Organizations & Governance

### Organizations

Central multi-account: management (payer) account, OUs, member accounts. Why multi-account: **account = strongest isolation boundary** (compromised dev can't touch prod), blast-radius, **per-account quotas**, cost attribution. Gives: consolidated billing, **SCPs**, org CloudTrail, RAM, delegated admin, tag/backup/AI-opt-out policies. Accounts created or invited; closing is slow. **Management account should hold no workloads** (SCPs can't restrict it → most privileged).

### SCPs

**Filters never grants** — max available permissions; action still needs an IAM allow. Apply to OU/account, inherited. **Restrict even member-account root.** **❗ Management account not affected by SCPs at all** (top gotcha). Service-linked roles exempt. `FullAWSAccess` default → deny-list (keep + add Deny, common) or allow-list (remove + enumerate). Typical: deny non-approved regions, deny leaving org, deny disabling CloudTrail/Config/GuardDuty, deny deleting log buckets, deny disabling S3 BPA, deny root actions.

### Control Tower

Automated opinionated **landing zone**: multi-account (dedicated log-archive + audit accounts), Identity Center, org CloudTrail+Config, guardrails. **Guardrails/controls**: preventive (SCPs — blocked), detective (Config — reported), proactive (CFN hooks — blocked before deploy); mandatory/strongly-recommended/elective. **Account Factory** (vend new accounts to baseline). Layer on top of Organizations/Config/CloudTrail/Identity Center/Service Catalog. "Secure multi-account from scratch?" → Control Tower (hand-building = weeks, easy to get wrong).

### RAM

Share specific resources cross-account without duplicating / cross-account roles. Shared: **VPC subnets**, TGWs, Resolver rules, License Manager, Aurora clusters, Capacity Reservations. **Pattern:** central networking account owns one VPC, **shares subnets** to workload accounts (one coherent network, no peering/CIDR problems, IAM boundaries intact). RAM shares the **resource** (subnet exists once); cross-account role shares **permission to act**.

---

## 2. Billing & Cost Management

### Consolidated Billing

One bill via management account. Saves money: (1) **aggregated volume discounts** (combined usage reaches cheaper tiers), (2) **RI/SP sharing** (unused commitment covers any account — usually the bigger win, disable-able per account). Cost allocation tags + cost categories + **CUR** to S3. **❗ Cost-allocation tags must be activated in Billing + not retroactive** (activate day one).

### Cost Explorer / Budgets / Anomaly Detection

**Cost Explorer:** analysis, 13mo history + 12mo forecast, group/filter by service/account/region/type/tag (hourly/resource-level extra), rightsizing + RI/SP recommendations + utilisation/coverage reports. Deeper → CUR + Athena/QuickSight.
**Budgets:** cost/usage/RI-SP utilisation/coverage; alerts on actual **or forecasted** (SNS/email/Chatbot); **❗ Budget Actions** = auto-apply IAM/SCP deny or stop EC2/RDS on breach (cap a sandbox). First two free.
**Cost Anomaly Detection:** ML on your spend, **no threshold to set**, root-cause. **❗ Budgets vs Anomaly:** Budgets = "cross a limit I defined" (needs the right number, misses a 300% spike under budget); Anomaly = "something weird" (catches runaway Lambda/forgotten GPU/NAT). **Run both.**

### Free Tier & Pricing

Three kinds: **always free** (1M Lambda requests + 400k GB-s, 25 GB DynamoDB), **12 months** (750h t2/t3.micro, 5 GB S3), **trials** (GuardDuty 30d). **❗ Free Tier trap** — exceeding just bills; surprise charges = **NAT Gateway (~$32/mo, never free), unattached EIP / any public IPv4, `Never Expire` CloudWatch Logs, orphaned EBS snapshots, cross-AZ/egress transfer**. Set a **$1 Budget alert + Anomaly Detection day one**. Pricing: pay-as-you-go, reserve to save, tiered volume, **data in free / out charged / cross-AZ charged both ways** (VPC endpoints + same-AZ = cost levers). **Pricing Calculator** (prospective estimate) vs **Cost Explorer** (retrospective).

**Org/Accounts DR:** at risk = whole account, SCPs, Control Tower config, backups if co-located. Backup = **AWS Backup vaults + Vault Lock**, cross-account copies, IaC, 90-day account-closure window. Closed account reopens via Support ≤90 days. SCP lockout → fix from management account (never subject to SCPs). Compromise → restore into a **clean account** from cross-account vault. **❗ Backups in the same account as the workload are not backups** → separate backup account + **Vault Lock compliance mode** (not deletable even by root) = the ransomware/insider control. Test SCPs on a sandbox OU first.

---

## 3. Advisory & Support

### Trusted Advisor

Account review across cost/performance/security/fault tolerance/service limits(/ops). **❗ Support-tier gated** — Basic/Developer get only core security + quota checks; **full set + API + weekly email = Business/Enterprise**. Findings: idle EC2, unassociated EIPs, unattached EBS, SGs open to `0.0.0.0/0` on 22/3389, no root MFA, exposed keys, RDS no Multi-AZ, open S3, approaching quotas. Broad shallow sweep vs Compute Optimizer (deep rightsizing) / Cost Explorer (cost recs) / Config (compliance) / Security Hub (security standards).

### Support Plans

| Plan | Cost | Support | Notable |
|---|---|---|---|
| Basic | Free | None | Docs, Personal Health Dashboard, core TA |
| Developer | ~$29/mo | Business-hours email | Non-prod |
| **Business** | ~$100/mo | 24/7 phone/chat/email | ✅ Full TA + API, prod-down <1h, 3rd-party support |
| Enterprise On-Ramp | ~$5,500/mo | 24/7 + TAM pool | Critical <30min |
| Enterprise | ~$15,000/mo | 24/7 + designated TAM | Critical <15min |

**Business = minimum for production; TAM only at Enterprise On-Ramp/Enterprise.**

---

← [Global Edge Services](14-global-edge-services.md) · [Index](README.md) · [Cost & Performance](16-cost-performance.md) →
