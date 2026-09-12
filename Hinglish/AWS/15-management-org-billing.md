> **AWS Detailed Guide** · [Index](README.md) · Part III

# Management, Organizations & Billing

---

## 1. Organizations & Governance

### AWS Organizations

**Yeh kya hai:** Many AWS accounts ko ek hierarchy ke tarah central management — ek **management (payer) account**, **Organizational Units (OUs)**, aur **member accounts**.

**Multi-account bilkul kyun** (question ke peeche ka question):
- **Ek account AWS ka strongest isolation boundary hai.** Ek compromised ya misconfigured dev account production resources ko touch nahi kar sakta, kyunki woh bilkul same account mein nahi hain — ek account ke andar IAM separation se far zyada strong.
- Security incidents aur mistakes dono ke liye **blast-radius containment**.
- **Service quotas per account hote hain**, isliye ek runaway workload production ke Lambda concurrency ya EIP limits consume nahi kar sakta.
- Team/environment ke liye clean **cost attribution**, aur clean environment separation.

**Organizations kya deta hai:** [consolidated billing](#consolidated-billing), **[SCPs](#service-control-policies-scps)**, organization-wide **CloudTrail** trails, **[RAM](#aws-resource-access-manager-ram)** resource sharing, GuardDuty/Config/Security Hub/Access Analyzer ke liye delegated administration, plus **tag policies**, **backup policies**, aur **AI services opt-out policies**.

Practical notes: accounts org mein ya **create** hote hain ya **invite** hote hain; ek account close karna ek slow, deliberate process hai (deletion se pehle ek suspension period), isliye account creation ko semi-permanent treat karo. **Best practice: management account mein koi workloads nahi honi chahiye** — sirf billing aur org administration — kyunki SCPs isko restrict nahi kar sakte (neeche) aur isliye yeh tumhari estate ki sabse privileged jagah hai.

### Service Control Policies (SCPs)

[Policy Types & Structure](03-iam-security.md#policy-types--structure) aur [Least Privilege & Permission Boundaries](03-iam-security.md#least-privilege--permission-boundaries-in-practice) mein policy-evaluation terms mein cover kiya gaya hai. Organisation-level specifics:

- **SCPs filters hain, kabhi grants nahi.** Yeh ek account ke liye *maximum* available permissions define karte hain. Ek action ko allow hone ke liye ab bhi ek IAM policy chahiye; SCP sirf decide karta hai ki use allowed hone diya ja sakta hai kya bilkul.
- Yeh ek **OU ya account** pe apply hote hain aur tree ke down inherited hote hain.
- **Yeh member account ke root user ko bhi restrict karte hain** — jo inko real guardrail banata hai.
- **❗ Management account SCPs se bilkul affected nahi hota**, chahe tum unko kahin bhi attach karo. Yeh sabse-asked SCP gotcha hai, aur wajah hai ki tum workloads ko management account se bahar rakhte ho.
- Service-linked roles bhi exempt hote hain.
- **`FullAWSAccess`** by default attached hota hai. Strategies: **deny-list** (FullAWSAccess rakho aur explicit `Deny` statements add karo — most common) ya **allow-list** (usko remove karo aur exactly woh enumerate karo jo permitted hai — tighter, bahut zyada work).

Typical real SCPs jo naam karne layak hain: approved regions ke siva sab deny karo (`aws:RequestedRegion`), organization leave karna deny karo, **CloudTrail/Config/GuardDuty** disable karna deny karo, log-archive buckets delete karna deny karo, S3 Block Public Access disable karna deny karo, aur member accounts mein root-user actions deny karo.

### AWS Control Tower

**Ek automated, opinionated landing zone.** Ek setup wizard tumhe deta hai: dedicated **log-archive** aur **audit** accounts ke sath ek multi-account structure, human access ke liye **IAM Identity Center**, org-wide CloudTrail aur Config, aur guardrails ka ek baseline.

- **Guardrails (ab "controls")** teen flavours mein aate hain: **preventive** (SCPs ke tarah implement, action block hota hai), **detective** (Config rules ke tarah implement, violation report hoti hai), aur **proactive** (CloudFormation hooks, resource deployment se pehle block hota hai). Har ek **mandatory**, **strongly recommended**, ya **elective** categorised hai.
- **Account Factory** naye accounts ko ek standard, pre-configured baseline pe vest karta hai — isliye ek naye team ka account already logging, already guardrailed, already SSO se wired hokar aata hai.
- Yeh Organizations, Config, CloudTrail, IAM Identity Center, aur Service Catalog ke **upar ek layer** hai, koi naya underlying service nahi.

**Interview framing:** "Scratch se secure multi-account AWS environment kaise set up karoge?" → **Control Tower**, kyunki ek landing zone hand-build karna (org structure, SCPs, centralised logging, SSO, guardrails, account vending) hafton ka kaam hai jo subtly galat karna easy hai. Isko hand se sirf tab banao jab tumhe ek aisa structure chahiye jo Control Tower express nahi kar sakta.

### AWS Resource Access Manager (RAM)

Apni organisation ke across **specific resources share karo** bina duplicate kiye aur bina cross-account IAM roles ke.

Commonly shared: **VPC subnets**, **Transit Gateways**, Route 53 Resolver rules, License Manager configurations, Aurora clusters, aur EC2 Capacity Reservations/Dedicated Hosts.

**Woh pattern jo matter karta hai:** ek central **networking account** ek well-designed VPC own karta hai aur **apne subnets share** karta hai workload accounts ko. Har team apne resources shared subnets *mein* launch karti hai, isliye tumhe ek coherent network milta hai bina VPC peering, bina overlapping-CIDR problems, aur bina per-team network design ke — jabki accounts ke beech IAM boundaries intact rehti hain. Yehi hai jaise most large AWS estates actually banaye jaate hain.

**RAM vs cross-account roles:** RAM **resource ko khud** share karta hai (subnet ek baar exist karta hai, many accounts se use hota hai); ek cross-account role owning account mein **act karne ki permission** share karta hai. Different problems ke liye different mechanisms.

---

## 2. Billing & Cost Management

### Consolidated Billing

Har account ke across ek bill, management account se paid. Do effects jo actually paisa bachate hain — aur wajah "we're moving accounts into an Organization" ek cost initiative hai, sirf governance nahi:

1. **Aggregated volume discounts.** Tiered pricing (S3 storage, data transfer) sab accounts ke **combined** usage pe calculate hota hai, isliye har koi cheaper tiers tak jyada jaldi pahunchta hai.
2. **Reserved Instance aur Savings Plan sharing.** Ek account mein unused RI/SP commitment automatically **kisi bhi doosre** org account mein matching usage cover kar deta hai. Yeh usually bigger win hota hai, aur yeh **per account disable ki ja sakti hai** jab kisi team ko guaranteed capacity ya strictly separated billing chahiye ho.

Supporting tooling: attribution ke liye **cost allocation tags** aur **cost categories**, aur Athena/QuickSight mein line-item analysis ke liye S3 ko deliver hone wali **Cost and Usage Report (CUR)**.

**❗ Cost-allocation-tag gotcha:** tags ko Billing console mein explicitly **activate** karna hota hai cost reports mein appear hone se pehle, aur activation **retroactive nahi hai** — historic spend kabhi tagged nahi hota. Din ek pe hi apni tagging strategy set up karo aur keys activate karo; Organizations ka tag policy feature consistency enforce karne ka tarika hai.

### Cost Explorer

Analysis tool: **13 months tak** ki history, plus ek **12-month forecast** ke sath cost aur usage visualise karo.

- Service, **linked account**, region, instance type, usage type, aur **cost allocation tag** se group aur filter karo. Monthly/daily granularity included hai; **hourly aur resource-level granularity extra cost** karta hai.
- Built-in recommendations: **EC2 rightsizing**, aur **utilisation aur coverage reports** ke sath **Reserved Instance / Savings Plans purchase recommendations** — woh reports jo batati hain ki tumne already khareede hue commitments actually use ho rahe hain kya.
- Kisi bhi deeper chiz ke liye, **CUR** ko S3 mein export karo aur usko Athena se query karo ya QuickSight mein visualise karo.

### AWS Budgets

Thresholds set karo aur alert lo. Char budget types: **cost**, **usage**, **RI/SP utilisation**, aur **RI/SP coverage**.

- **Actual** *ya* **forecasted** spend pe alerts, SNS, email, ya Chatbot (Slack/Teams) ke through delivered. Forecast-based alert useful wala hai — yeh tumhe mid-month mein warn karta hai ki tum overrun ke track pe ho, after the fact nahi.
- **❗ Budget Actions strong answer hain:** ek budget automatically ek **IAM ya SCP deny policy** apply kar sakta hai, ya **EC2/RDS instances stop** kar sakta hai, jab ek threshold breach ho — ek notification ko ek actual control mein badalte hue. Ek sandbox ya training account cap karne ke liye ideal.
- Pehle do budgets free hain.

### Cost Anomaly Detection

**Tumhare apne spend patterns pe machine learning**, **unusual** cost pe alert karta hai — **koi threshold tumhe set nahi karna**.

- AWS service, linked account, cost category, ya cost allocation tag se monitor karta hai.
- Alerts mein **root-cause analysis** included hota hai (kaunsa service, kaunsa account, kaunsi usage type spike drive kiya).

**❗ Budgets vs Anomaly Detection woh distinction hai jo state karna hai:** Budgets answer karte hain *"jab mein ek limit cross karun jo maine define ki thi tab mujhe batao"* — inko chahiye ki tumhe right number pata ho, aur woh ek small service mein 300% spike miss kar dete hain jo overall budget ke under rehti hai. Anomaly Detection answer karta hai *"jab kuch weird ho tab mujhe batao"* — yeh runaway Lambda recursion, bhoola hua GPU instance, ya day one pe misconfigured NAT-gateway data transfer catch karta hai. **Dono run karo**: governance aur forecasting ke liye budgets, surprises catch karne ke liye anomaly detection.

### Free Tier, Pricing & Estimating Cost

**Free Tier ke teen types** — distinction hi question hai:
- **Always free** — permanently, limits ke andar: **1 M Lambda requests + 400,000 GB-seconds/month**, **25 GB DynamoDB** storage, 10 CloudWatch custom metrics, 25 GB SNS.
- **12 months free** naye accounts ke liye — 750 h/month `t2/t3.micro` EC2, 5 GB S3 Standard, 750 h RDS.
- **Trials** — short-term, service-specific (e.g. GuardDuty ke 30 days, Inspector, Macie).

**❗ Free Tier trap jo naam karne layak hai:** isko exceed karna kuch bhi stop nahi karta, yeh sirf bill kar deta hai — aur classic surprise charges hain ek **NAT Gateway** (~$32/month ek byte traffic se pehle, kabhi free-tier eligible nahi), ek **unattached Elastic IP** ya koi public IPv4 address, **`Never Expire` retention wale CloudWatch Logs**, orphaned **EBS snapshots**, aur **cross-AZ/egress data transfer**. Din ek pe ek **$1 Budget alert** set karo ([Budgets](#aws-budgets)) aur [Cost Anomaly Detection](#cost-anomaly-detection) on karo.

**AWS pricing fundamentals jo state karne hain:** pay-as-you-go, **reserve karke kam pay karo** (RI/Savings Plans), **jitna zyada use karo unit price kam hoti hai** (tiered volume pricing — [Consolidated Billing](#consolidated-billing) savings ke peeche ka mechanism), aur AWS grow karne pe kam pay karo. Almost universally, **data transfer *in* free hai, data transfer *out* charged hai**, aur cross-AZ traffic dono directions mein charged hai — yehi wajah hai VPC endpoints aur same-AZ placement sirf latency levers nahi, cost levers bhi hain.

**Estimating tools:** forward-looking architecture estimates ke liye **AWS Pricing Calculator** (shareable, exportable — ek design review ya client proposal ke liye right artefact), aur on-prem-versus-AWS business cases ke liye **Migration Evaluator**/**TCO** analysis. [Cost Explorer](#cost-explorer) se contrast karo, jo **retrospective** hai: Pricing Calculator estimate karta hai ek design *kya* cost karega, Cost Explorer analyse karta hai tumne *kya* spend kiya.

**Disaster Recovery — Organizations, Accounts & Backup Governance**

| | |
|---|---|
| **Actually risk par kya hai** | Ek poora account, SCPs, Control Tower configuration — aur worst case mein, aapke backups bhi us cheez ke saath jiska wo backup the |
| **Backup mechanism** | **AWS Backup vaults with Vault Lock**, cross-account backup copies, Organizations config IaC mein, aur **90 din** ka account-closure recovery window |
| **Realistic RPO / RTO** | Poori tarah is par depend karta hai ki backups ek *alag* account mein hain ya nahi. Nahi hain to RTO "kabhi nahi" hai |

**Recovery runbook:**
1. **Closed account:** AWS Support ke through **90 din** ke andar dobara khola ja sakta hai — uske baad permanent hai.
2. **SCP lockout:** **management account** se fix karo, jispar SCPs kabhi apply nahi hote. Yehi is escape hatch ke hone ki wajah hai, toh management-account access working aur alag credentialed rakho.
3. **Account compromise:** cross-account backup vault se ek **clean account** mein restore karo — us account mein wapas kabhi nahi jispar aapko abhi bharosa nahi hai.
4. **Baseline re-apply karo** (SCPs, guardrails, Config rules) IaC se, workloads ko wapas aane dene se pehle.

⚠️ **Gotcha, aur yehi wo hai jo incident ko company event bana deta hai:** **workload ke usi account mein rakhe backups backups nahi hain.** Compromised ya galti se delete hua account dono le jaata hai. Recovery points ek **alag backup account** mein rakho **Vault Lock compliance mode** ke saath, jo root se bhi delete nahi ho sakta — yehi asli ransomware aur insider-risk control hai. Aur SCPs pehle sandbox OU par test karo: ek galat `Deny` poore OU se har insaan ko lock out kar sakta hai, admins bhi shamil.

---

## 3. Advisory & Support

### Trusted Advisor

Paanch/che pillars ke across ek automated account-level review: **cost optimisation, performance, security, fault tolerance, service limits (quotas)**, aur operational excellence.

- **❗ Support-tier gated:** Basic aur Developer plans ko sirf **core security checks aur service-quota checks** milti hain. **Full check set, programmatic API access, aur weekly email reports ke liye Business ya Enterprise Support chahiye** — ek commonly tested detail.
- Representative findings: idle/underutilised EC2 instances, **unassociated Elastic IPs**, idle load balancers, unattached EBS volumes, low-utilisation RDS, **port 22/3389 pe `0.0.0.0/0` ke liye open security groups**, **root account pe koi MFA nahi**, public repositories mein mile exposed access keys, RDS without Multi-AZ, open permissions wale S3 buckets, aur approaching service quotas.

**Yeh apne neighbours se kaise related hai:** **Trusted Advisor** sab paanch pillars ke across broad, shallow, best-practice sweep hai; **Compute Optimizer** real utilisation history se EC2/ASG/Lambda/EBS ke liye deep ML-based **rightsizing** karta hai; **Cost Explorer** cost *analysis aur commitment* recommendations karta hai; **Config** continuous, customisable **compliance** rules karta hai; **Security Hub** formal standards ke against security findings aggregate karta hai. Trusted Advisor woh jagah hai jahan se ek unfamiliar account pe start karte ho; baaki depth ke liye jaate hain.

### AWS Support Plans

Isliye poocha jaata hai kyunki yeh real features gate karta hai (upar wala Trusted Advisor sabse obvious example hai) aur kyunki yeh kisi bhi cost conversation ka hissa hai.

| Plan | Cost | Technical support | Notable inclusions |
|---|---|---|---|
| **Basic** | **Free**, har account | ❌ Kuch nahi (sirf billing/account support) | Documentation, forums, **Personal Health Dashboard**, core Trusted Advisor security/quota checks |
| **Developer** | ~$29/mo se | **Business-hours email**, 1 primary contact | General guidance <24 h; system-impaired <12 h. Practice mein **sirf non-production** |
| **Business** | ~$100/mo se (ya spend ka 3–10%) | **24/7 phone, chat, email**, unlimited contacts | ✅ **Full Trusted Advisor** + API, production-down <1 h, **third-party software support**, Infrastructure Event Management (extra), AWS Health API |
| **Enterprise On-Ramp** | ~$5,500/mo se | 24/7 + **Technical Account Managers ka pool** | Business-critical-down **<30 min**, Cost Optimization/Well-Architected reviews |
| **Enterprise** | ~$15,000/mo se | 24/7 + ek **designated TAM** | Business-critical-down **<15 min**, concierge billing, Incident Detection & Response, training credits |

**Do facts jo actually tested hote hain:** **Business production ke liye minimum tier hai**, kyunki yeh pehla tier hai jisme 24/7 technical support, 1-hour production-down response, aur full Trusted Advisor check set + API hai. Aur ek **TAM** (designated technical advisor) sirf **Enterprise On-Ramp/Enterprise** pe aata hai.

---

← [Global Edge Services](14-global-edge-services.md) · [Index](README.md) · [Cost & Performance](16-cost-performance.md) →
