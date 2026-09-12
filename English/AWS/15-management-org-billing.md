> **AWS Detailed Guide** · [Index](README.md) · Part III

# Management, Organizations & Billing

---

## 1. Organizations & Governance

### AWS Organizations

**What it is:** central management of many AWS accounts as one hierarchy — a **management (payer) account**, **Organizational Units (OUs)**, and **member accounts**.

**Why multi-account at all** (the question behind the question):
- **An account is the strongest isolation boundary AWS offers.** A compromised or misconfigured dev account cannot touch production resources, because they aren't in the same account at all — far stronger than IAM separation within one account.
- **Blast-radius containment** for both security incidents and mistakes.
- **Service quotas are per account**, so a runaway workload can't consume production's Lambda concurrency or EIP limits.
- Clean **cost attribution** per team/environment, and clean environment separation.

**What Organizations gives you:** [consolidated billing](#consolidated-billing), **[SCPs](#service-control-policies-scps)**, organization-wide **CloudTrail** trails, **[RAM](#aws-resource-access-manager-ram)** resource sharing, delegated administration for GuardDuty/Config/Security Hub/Access Analyzer, plus **tag policies**, **backup policies**, and **AI services opt-out policies**.

Practical notes: accounts are either **created** in the org or **invited** into it; closing an account is a slow, deliberate process (a suspension period before deletion), so treat account creation as semi-permanent. **Best practice: the management account should hold no workloads** — only billing and org administration — because SCPs cannot restrict it (below) and it's therefore the most privileged place in your estate.

### Service Control Policies (SCPs)

Covered in policy-evaluation terms under [Policy Types & Structure](03-iam-security.md#policy-types--structure) and [Least Privilege & Permission Boundaries](03-iam-security.md#least-privilege--permission-boundaries-in-practice). The organisation-level specifics:

- **SCPs are filters, never grants.** They define the *maximum* available permissions for an account. An action still needs an IAM policy to allow it; the SCP only decides whether it's permitted to be allowed at all.
- They apply to an **OU or account** and are inherited down the tree.
- **They restrict even the root user of a member account** — which is what makes them the real guardrail.
- **❗ The management account is not affected by SCPs at all**, no matter where you attach them. That's the single most-asked SCP gotcha, and the reason you keep workloads out of the management account.
- Service-linked roles are also exempt.
- **`FullAWSAccess`** is attached by default. Strategies: **deny-list** (keep FullAWSAccess and add explicit `Deny` statements — most common) or **allow-list** (remove it and enumerate exactly what's permitted — tighter, much more work).

Typical real SCPs to be able to name: deny all regions except approved ones (`aws:RequestedRegion`), deny leaving the organization, deny disabling **CloudTrail/Config/GuardDuty**, deny deleting log-archive buckets, deny disabling S3 Block Public Access, and deny root-user actions in member accounts.

### AWS Control Tower

**An automated, opinionated landing zone.** One setup wizard gives you: a multi-account structure with dedicated **log-archive** and **audit** accounts, **IAM Identity Center** for human access, org-wide CloudTrail and Config, and a baseline of guardrails.

- **Guardrails (now "controls")** come in three flavours: **preventive** (implemented as SCPs — the action is blocked), **detective** (implemented as Config rules — the violation is reported), and **proactive** (CloudFormation hooks — the resource is blocked before deployment). Each is categorised **mandatory**, **strongly recommended**, or **elective**.
- **Account Factory** vests new accounts to a standard, pre-configured baseline — so a new team's account arrives already logging, already guardrailed, already wired to SSO.
- It's a **layer on top of** Organizations, Config, CloudTrail, IAM Identity Center, and Service Catalog rather than a new service underneath.

**Interview framing:** "How would you set up a secure multi-account AWS environment from scratch?" → **Control Tower**, because hand-building a landing zone (org structure, SCPs, centralised logging, SSO, guardrails, account vending) is weeks of work that's easy to get subtly wrong. Build it by hand only when you need a structure Control Tower can't express.

### AWS Resource Access Manager (RAM)

**Share specific resources across accounts** in your organisation without duplicating them and without cross-account IAM roles.

Commonly shared: **VPC subnets**, **Transit Gateways**, Route 53 Resolver rules, License Manager configurations, Aurora clusters, and EC2 Capacity Reservations/Dedicated Hosts.

**The pattern that matters:** a central **networking account** owns one well-designed VPC and **shares its subnets** to workload accounts. Each team launches resources *into* the shared subnets, so you get one coherent network with no VPC peering, no overlapping-CIDR problems, and no per-team network design — while IAM boundaries between accounts stay intact. This is how most large AWS estates are actually built.

**RAM vs cross-account roles:** RAM shares the **resource itself** (the subnet exists once, used by many accounts); a cross-account role shares **permission to act** in the owning account. Different mechanisms for different problems.

---

## 2. Billing & Cost Management

### Consolidated Billing

One bill across every account, paid by the management account. The two effects that actually save money — and the reason "we're moving accounts into an Organization" is a cost initiative, not just a governance one:

1. **Aggregated volume discounts.** Tiered pricing (S3 storage, data transfer) is calculated on the **combined** usage of all accounts, so everyone reaches the cheaper tiers sooner.
2. **Reserved Instance and Savings Plan sharing.** Unused RI/SP commitment in one account automatically covers matching usage in **any other** account in the org. This is usually the bigger win, and it can be **disabled per account** when a team needs guaranteed capacity or strictly separated billing.

Supporting tooling: **cost allocation tags** and **cost categories** for attribution, and the **Cost and Usage Report (CUR)** delivered to S3 for line-item analysis in Athena/QuickSight.

**❗ The cost-allocation-tag gotcha:** tags must be explicitly **activated** in the Billing console before they appear in cost reports, and activation is **not retroactive** — historic spend never gets tagged. Set up your tagging strategy and activate the keys on day one; the tag policy feature in Organizations is how you enforce consistency.

### Cost Explorer

The analysis tool: visualise cost and usage with up to **13 months of history**, plus a **12-month forecast**.

- Group and filter by service, **linked account**, region, instance type, usage type, and **cost allocation tag**. Monthly/daily granularity is included; **hourly and resource-level granularity costs extra**.
- Built-in recommendations: **EC2 rightsizing**, and **Reserved Instance / Savings Plans purchase recommendations** with **utilisation and coverage reports** — the reports that tell you whether commitments you already bought are actually being used.
- For anything deeper, export the **CUR** to S3 and query it with Athena or visualise it in QuickSight.

### AWS Budgets

Set thresholds and get alerted. Four budget types: **cost**, **usage**, **RI/SP utilisation**, and **RI/SP coverage**.

- Alerts on **actual** *or* **forecasted** spend, delivered via SNS, email, or Chatbot (Slack/Teams). The forecast-based alert is the useful one — it warns you mid-month that you're on track to overrun, rather than after the fact.
- **❗ Budget Actions are the strong answer:** a budget can automatically **apply an IAM or SCP deny policy**, or **stop EC2/RDS instances**, when a threshold is breached — turning a notification into an actual control. Ideal for capping a sandbox or a training account.
- First two budgets are free.

### Cost Anomaly Detection

**Machine learning on your own spend patterns**, alerting on **unusual** cost — with **no threshold for you to set**.

- Monitors by AWS service, linked account, cost category, or cost allocation tag.
- Alerts include **root-cause analysis** (which service, which account, which usage type drove the spike).

**❗ Budgets vs Anomaly Detection is the distinction to state:** Budgets answer *"tell me when I cross a limit I defined"* — they need you to know the right number, and they miss a 300% spike in a small service that stays under the overall budget. Anomaly Detection answers *"tell me when something is weird"* — it catches the runaway Lambda recursion, the forgotten GPU instance, or the misconfigured NAT-gateway data transfer on day one. **Run both**: budgets for governance and forecasting, anomaly detection for catching surprises.

### Free Tier, Pricing & Estimating Cost

**Three kinds of Free Tier** — the distinction is the question:
- **Always free** — permanently, within limits: **1 M Lambda requests + 400,000 GB-seconds/month**, **25 GB DynamoDB** storage, 10 CloudWatch custom metrics, 25 GB SNS.
- **12 months free** for new accounts — 750 h/month of `t2/t3.micro` EC2, 5 GB S3 Standard, 750 h RDS.
- **Trials** — short-term, service-specific (e.g. GuardDuty's 30 days, Inspector, Macie).

**❗ The Free Tier trap worth naming:** exceeding it doesn't stop anything, it just bills you — and the classic surprise charges are a **NAT Gateway** (~$32/month before a byte of traffic, never free-tier eligible), an **unattached Elastic IP** or any public IPv4 address, **CloudWatch Logs with `Never Expire` retention**, orphaned **EBS snapshots**, and **cross-AZ/egress data transfer**. Set a **$1 Budget alert on day one** ([Budgets](#aws-budgets)) and turn on [Cost Anomaly Detection](#cost-anomaly-detection).

**The AWS pricing fundamentals to state:** pay-as-you-go, **pay less by reserving** (RI/Savings Plans), **pay less per unit as you use more** (tiered volume pricing — the mechanism behind [Consolidated Billing](#consolidated-billing) savings), and pay less as AWS grows. Almost universally, **data transfer *in* is free, data transfer *out* is charged**, and cross-AZ traffic is charged in both directions — which is why VPC endpoints and same-AZ placement are cost levers, not just latency ones.

**Estimating tools:** the **AWS Pricing Calculator** for forward-looking architecture estimates (shareable, exportable — the right artefact for a design review or a client proposal), and the **Migration Evaluator**/**TCO** analysis for on-prem-versus-AWS business cases. Contrast with [Cost Explorer](#cost-explorer), which is **retrospective**: Pricing Calculator estimates what a design *will* cost, Cost Explorer analyses what you *did* spend.

**Disaster Recovery — Organizations, Accounts & Backup Governance**

| | |
|---|---|
| **What's actually at risk** | An entire account, SCPs, Control Tower configuration — and, in the worst case, your backups along with the thing they were backing up |
| **Backup mechanism** | **AWS Backup vaults with Vault Lock**, cross-account backup copies, Organizations config in IaC, and the **90-day** account-closure recovery window |
| **Realistic RPO / RTO** | Depends entirely on whether backups live in a *different* account. If they don't, RTO is "never" |

**Recovery runbook:**
1. **Closed account:** it can be reopened through AWS Support within **90 days** — after that it's permanent.
2. **SCP lockout:** fix from the **management account**, which SCPs never apply to. This is the only reason that escape hatch exists, so keep management-account access working and separately credentialed.
3. **Account compromise:** restore into a **clean account** from the cross-account backup vault — never back into the account you don't yet trust.
4. **Re-apply the baseline** (SCPs, guardrails, Config rules) from IaC before letting workloads back in.

⚠️ **The gotcha, and it's the one that turns an incident into a company event:** **backups stored in the same account as the workload are not backups.** A compromised or mistakenly-deleted account takes both. Put recovery points in a **separate backup account** with **Vault Lock in compliance mode**, which cannot be deleted even by root — that is the actual ransomware and insider-risk control. And test SCPs on a sandbox OU first: one wrong `Deny` can lock every human out of an entire OU, admins included.

---

## 3. Advisory & Support

### Trusted Advisor

An automated account-level review across five/six pillars: **cost optimisation, performance, security, fault tolerance, service limits (quotas)**, and operational excellence.

- **❗ Support-tier gated:** Basic and Developer plans get only the **core security checks and service-quota checks**. The **full check set, programmatic API access, and weekly email reports require Business or Enterprise Support** — a commonly tested detail.
- Representative findings: idle/underutilised EC2 instances, **unassociated Elastic IPs**, idle load balancers, unattached EBS volumes, low-utilisation RDS, **security groups open to `0.0.0.0/0` on port 22/3389**, **no MFA on the root account**, exposed access keys found in public repositories, RDS without Multi-AZ, S3 buckets with open permissions, and approaching service quotas.

**How it relates to the neighbours:** **Trusted Advisor** is the broad, shallow, best-practice sweep across all five pillars; **Compute Optimizer** does deep ML-based **rightsizing** for EC2/ASG/Lambda/EBS from real utilisation history; **Cost Explorer** does cost *analysis and commitment* recommendations; **Config** does continuous, customisable **compliance** rules; **Security Hub** aggregates security findings against formal standards. Trusted Advisor is where you start on an unfamiliar account; the others are where you go for depth.

### AWS Support Plans

Asked because it gates real features (Trusted Advisor above being the obvious one) and because it's part of any cost conversation.

| Plan | Cost | Technical support | Notable inclusions |
|---|---|---|---|
| **Basic** | **Free**, every account | ❌ None (only billing/account support) | Documentation, forums, **Personal Health Dashboard**, core Trusted Advisor security/quota checks |
| **Developer** | From ~$29/mo | **Business-hours email**, 1 primary contact | General guidance <24 h; system-impaired <12 h. **Non-production only** in practice |
| **Business** | From ~$100/mo (or 3–10% of spend) | **24/7 phone, chat, email**, unlimited contacts | ✅ **Full Trusted Advisor** + API, production-down <1 h, **third-party software support**, Infrastructure Event Management (extra), AWS Health API |
| **Enterprise On-Ramp** | From ~$5,500/mo | 24/7 + **pool of Technical Account Managers** | Business-critical-down **<30 min**, Cost Optimization/Well-Architected reviews |
| **Enterprise** | From ~$15,000/mo | 24/7 + a **designated TAM** | Business-critical-down **<15 min**, concierge billing, Incident Detection & Response, training credits |

**The two facts that actually get tested:** **Business is the minimum tier for production**, because it's the first with 24/7 technical support, a 1-hour production-down response, and the full Trusted Advisor check set + API. And a **TAM** (designated technical advisor) only arrives at **Enterprise On-Ramp/Enterprise**.

---

← [Global Edge Services](14-global-edge-services.md) · [Index](README.md) · [Cost & Performance](16-cost-performance.md) →
