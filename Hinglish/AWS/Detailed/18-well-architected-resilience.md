> **AWS Detailed Guide** · [Index](README.md) · Part III

# Well-Architected & Resilience

---

## 1. Well-Architected Framework

### AWS Well-Architected Framework — 6 Pillars

Original notes kabhi bhi Well-Architected Framework reference nahi karte, iske bawajood ki yeh ek sabse commonly asked "AWS best practices generally batao" senior/architect-level framing questions mein se ek hai.

| Pillar | Core question | .NET-relevant example |
|---|---|---|
| Operational Excellence | Kya tum systems run aur monitor kar sakte ho business value deliver karne ke liye, aur continually improve kar sakte ho? | IaC (CloudFormation/CDK/Terraform), structured logging, runbooks, CI/CD with automated rollback |
| Security | Data, systems, aur assets ko kaise protect karte ho? | IAM least privilege, Secrets Manager, encryption at rest/in transit, WAF, Security Hub |
| Reliability | Kya workload apna function correctly aur consistently perform kar sakta hai? | Multi-AZ, auto-scaling, retries with backoff/jitter (Polly in .NET), DLQs, chaos/failure testing |
| Performance Efficiency | Demand change hone pe kya tum resources efficiently use kar rahe ho? | Right-sized compute, caching (ElastiCache/CloudFront), async/event-driven patterns, .NET AOT for Lambda |
| Cost Optimization | Kya tum unnecessary costs avoid kar rahe ho? | Savings Plans/Spot mix, S3 lifecycle policies, right-sizing, tagging for cost allocation |
| Sustainability | Kya tum environmental impact minimize kar rahe ho? | Region selection, efficient instance types (Graviton/ARM64), scale-to-zero serverless patterns |

**Interview mein isko kaise use karo:** jab ek open-ended "yeh architecture kaise evaluate karoge" poocha jaaye, apne answer ko explicitly in 6 pillars ke around structure karna (chahe briefly) architect-level thinking signal karta hai, tips ka ek grab-bag nahi. Yeh formal **AWS Well-Architected Tool** aur **Well-Architected Reviews** ka basis bhi hai, jinme senior/lead engineers se frequently expect kiya jaata hai ki unhone participate ya lead kiya ho.

### Well-Architected 6 Pillars — Rapid Recall Version

Upar wali table ka ek compact, one-line-per-pillar version, purely fast memorization/recall ke liye interview pressure ke under — detailed table woh hai jisse tum study karte ho; yeh woh hai jisse tum recite karte ho:

- **Operational Excellence:** systems run aur monitor karo, aur continually iterate/improve karo.
- **Security:** risk-based controls ke through data, systems, aur assets protect karo.
- **Reliability:** failure se recover karo aur demand meet karne ke liye consistently scale karo.
- **Performance Efficiency:** computing resources efficiently use karo, chahe demand aur technology change ho.
- **Cost Optimization:** unnecessary spend avoid karo aur waste eliminate karo.
- **Sustainability:** apne workloads chalane ke environmental impact minimize karo.

**Memory hook:** "Run it well, keep it safe, keep it up, keep it fast, keep it cheap, keep it green" — six pillars, six verbs, same order mein jisme AWS unko present karta hai.

---

## 2. Disaster Recovery

### Disaster Recovery Strategies

Original notes Multi-AZ, Global Tables, aur multi-region Lambda concurrency ko individually touch karte hain lekin unko kabhi standard DR-strategy framework mein assemble nahi karte jo AWS interviews expect karte hain (Backup & Restore / Pilot Light / Warm Standby / Multi-Site Active-Active) — ek senior interview ke liye clear, material gap.

```
  cheaper, slower recovery  ------------------->  costlier, faster recovery

+------------------+  +------------------+  +------------------+  +------------------+
| Backup & Restore |->|   Pilot Light    |->|   Warm Standby   |->|    Multi-Site    |
|                  |  |                  |  |                  |  |  Active-Active   |
| RPO: hours       |  | RPO: minutes     |  | RPO: secs-mins   |  | RPO: ~0          |
| RTO: hours       |  | RTO: 10s of mins |  | RTO: minutes     |  | RTO: ~0          |
+------------------+  +------------------+  +------------------+  +------------------+
 restore from        core infra off /     scaled-down but        full capacity live
 snapshots           minimal, data        RUNNING copy          in 2+ regions
                     replicating
```
*(Left→right cost aur operational complexity increase hoti hai; left→right RPO/RTO improve hote hain.)*

| Strategy | Description | RTO/RPO | .NET/AWS implementation notes |
|---|---|---|---|
| **Backup & Restore** | DR region mein regular backups (RDS snapshots, DynamoDB PITR/backups, S3 cross-region replication); disaster pe restore karo | Hours (RTO/RPO) | Sabse cheap; AWS Backup se automate karo; restores regularly test karo — ek untested backup ek DR plan nahi hai |
| **Pilot Light** | Core infra (DB replica, minimal config) DR region mein minimal scale pe hamesha running; baaki failover pe provision hota hai | RTO: tens of minutes; RPO: minutes | RDS cross-region read replica warm rakha jaata hai; app tier (ECS/EC2) IaC mein defined lekin zero/minimal pe scaled jab tak zarurat na ho |
| **Warm Standby** | DR region mein scaled-down lekin fully functional full stack continuously running | RTO/RPO: minutes or less | Data ke liye DynamoDB Global Tables ya Aurora Global Database; DR region mein smaller ECS/Fargate service count, failover pe scaled up |
| **Multi-Site Active-Active** | 2+ regions mein simultaneously full production capacity live, real traffic serve karti hui | RTO/RPO: near zero | Regions ke across Route 53 latency/weighted routing; DynamoDB Global Tables ya Aurora Global Database; conflict-tolerant/idempotent write design chahiye |

**Interviewer follow-up jo expect karna hai:** "DR ke liye Lambda concurrency planning kaise change hoti hai?" — concurrency section se tie back karo: ek passive DR region ke paas abhi bhi default 1,000 concurrency limit hai jab tak pre-raise na ki jaaye; ek warm/active-active strategy ko woh headroom disaster *se pehle* provision karna zaruri hai, uske dauran nahi.

**Interviewer follow-up jo expect karna hai:** "Route 53 failover — kya woh khud DR ke liye kaafi hai?" — nahi; upar Route 53 section ke hisab se, DNS failover TTL-bound hai aur instant nahi hai. Yeh Pilot Light/Warm Standby/Active-Active ka ek component hai, khud se ek complete DR strategy nahi.

---

## 3. Testing Resilience

### Testing Resilience: Fault Injection Simulator & Resilience Hub

Kisi bhi DR answer ke baad jo question aata hai woh hai **"tumhe kaise pata ki yeh kaam karta hai?"** — aur "hamne runbook document kar diya" ek weak reply hai. Ek untested DR plan ek assumption hai, capability nahi.

**AWS Fault Injection Service (FIS)** — managed **chaos engineering**. Tum ek experiment template define karte ho jo ek real, controlled fault inject karta hai aur observe karta hai ki system design ke hisab se behave karta hai kya:
- Available faults: **EC2 instances stop/terminate karna**, API calls throttle ya fail karna, **CPU/memory/disk/network stress** inject karna, **network latency ya packet loss** add karna, **ek RDS instance ko fail over karna**, ECS tasks ya EKS pods kill karna, aur — sabse bada wala — ek **entire AZ unavailable** ho jaane ko simulate karna.
- **Stop conditions** critical safety feature hain: FIS experiment ko automatically abort kar deta hai agar tumne nominate ki hui koi CloudWatch alarm breach ho jaaye, isliye ek test khud outage nahi ban sakta.
- Discipline: ek hypothesis banao ("agar hum ek AZ lose karte hain, to ALB un targets ko drain karta hai aur ASG unko 3 minutes ke andar bina kisi 5xx ke replace kar deta hai"), staging mein run karo, phir production mein business hours ke dauran team ke dekhte hue, aur kisi bhi surprise ko ek finding treat karo.

**AWS Resilience Hub** — ek application ko tumhare stated **RTO/RPO targets** ke against assess karta hai, uski resilience score karta hai, gaps flag karta hai (multi-AZ app tier ke peeche ek single-AZ database, missing backups, koi cross-region copy nahi), fixes recommend karta hai, aur unko validate karne ke liye **FIS experiment templates plus CloudWatch alarms** generate karta hai. Yeh "we think we're resilient" ko ek target ke against ek measured number mein badal deta hai.

**Answer jo land karta hai:** "Main per-workload RTO/RPO define karunga, jo DR strategy unko meet karti hai woh pick karunga, phir **isko prove** karunga — targets ke against assess karne ke liye Resilience Hub aur actual failure inject karne ke liye FIS, CloudWatch stop conditions ke sath taaki experiment safe rahe. Schedule pe **GameDays**, kyunki ek DR plan jo saal mein exercise nahi hua woh ek hypothesis hai. Yeh bhi plainly kehne layak hai: sabse common real finding infrastructure nahi hoti, yeh **ASG health-check type jo `ELB` ke bajaye `EC2` pe reh gaya** hota hai, isliye ek hung application kabhi replace nahi hota — exactly waisi chiz jo sirf ek fault injection test surface karta hai." (Dekho [Auto Scaling Groups](08-load-balancing-autoscaling.md#auto-scaling-groups-asg).)

---

← [Migration & Data Transfer](17-migration-data-transfer.md) · [Index](README.md) · [Cross-Cutting Reference](19-cross-cutting-reference.md) →
