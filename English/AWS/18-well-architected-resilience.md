> **AWS Detailed Guide** · [Index](README.md) · Part III

# Well-Architected & Resilience

---

## 1. Well-Architected Framework

### AWS Well-Architected Framework — 6 Pillars

The original notes never reference the Well-Architected Framework, despite it being one of the most commonly asked "tell me about AWS best practices generally" senior/architect-level framing questions.

| Pillar | Core question | .NET-relevant example |
|---|---|---|
| Operational Excellence | Can you run and monitor systems to deliver business value, and continually improve? | IaC (CloudFormation/CDK/Terraform), structured logging, runbooks, CI/CD with automated rollback |
| Security | How do you protect data, systems, and assets? | IAM least privilege, Secrets Manager, encryption at rest/in transit, WAF, Security Hub |
| Reliability | Can the workload perform its function correctly and consistently? | Multi-AZ, auto-scaling, retries with backoff/jitter (Polly in .NET), DLQs, chaos/failure testing |
| Performance Efficiency | Are you using resources efficiently as demand changes? | Right-sized compute, caching (ElastiCache/CloudFront), async/event-driven patterns, .NET AOT for Lambda |
| Cost Optimization | Are you avoiding unnecessary costs? | Savings Plans/Spot mix, S3 lifecycle policies, right-sizing, tagging for cost allocation |
| Sustainability | Are you minimizing environmental impact? | Region selection, efficient instance types (Graviton/ARM64), scale-to-zero serverless patterns |

**How to use this in an interview:** when asked an open-ended "how would you evaluate this architecture," structuring your answer explicitly around these 6 pillars (even briefly) signals architect-level thinking rather than a grab-bag of tips. It's also the basis for the **AWS Well-Architected Tool** and formal **Well-Architected Reviews**, which senior/lead engineers are frequently expected to have participated in or led.

### Well-Architected 6 Pillars — Rapid Recall Version

A compact, one-line-per-pillar version of the table above, purely for fast memorization/recall under interview pressure — the detailed table is what you study from; this is what you recite from:

- **Operational Excellence:** run and monitor systems, and continually iterate/improve.
- **Security:** protect data, systems, and assets through risk-based controls.
- **Reliability:** recover from failure and scale to meet demand consistently.
- **Performance Efficiency:** use computing resources efficiently, even as demand and technology change.
- **Cost Optimization:** avoid unnecessary spend and eliminate waste.
- **Sustainability:** minimize the environmental impact of running your workloads.

**Memory hook:** "Run it well, keep it safe, keep it up, keep it fast, keep it cheap, keep it green" — six pillars, six verbs, in the same order AWS presents them.

---

## 2. Disaster Recovery

### Disaster Recovery Strategies

The original notes touch on Multi-AZ, Global Tables, and multi-region Lambda concurrency individually but never assemble them into the standard DR-strategy framework AWS interviews expect (Backup & Restore / Pilot Light / Warm Standby / Multi-Site Active-Active) — a clear, material gap for a senior interview.

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
*(Cost and operational complexity increase left→right; RPO/RTO improve left→right.)*

| Strategy | Description | RTO/RPO | .NET/AWS implementation notes |
|---|---|---|---|
| **Backup & Restore** | Regular backups (RDS snapshots, DynamoDB PITR/backups, S3 cross-region replication) to a DR region; restore on disaster | Hours (RTO/RPO) | Cheapest; automate via AWS Backup; test restores regularly — an untested backup is not a DR plan |
| **Pilot Light** | Core infra (DB replica, minimal config) always running in DR region at minimal scale; rest is provisioned on failover | RTO: tens of minutes; RPO: minutes | RDS cross-region read replica kept warm; app tier (ECS/EC2) defined in IaC but scaled to zero/minimal until needed |
| **Warm Standby** | Scaled-down but fully functional full stack running in DR region continuously | RTO/RPO: minutes or less | DynamoDB Global Tables or Aurora Global Database for data; smaller ECS/Fargate service count in DR region, scaled up on failover |
| **Multi-Site Active-Active** | Full production capacity live in 2+ regions simultaneously, serving real traffic | RTO/RPO: near zero | Route 53 latency/weighted routing across regions; DynamoDB Global Tables or Aurora Global Database; requires conflict-tolerant/idempotent write design |

**Interviewer follow-up to expect:** "How does Lambda concurrency planning change for DR?" — tie back to the concurrency section: a passive DR region still has the default 1,000 concurrency limit unless pre-raised; a warm/active-active strategy requires provisioning that headroom *before* the disaster, not during it.

**Interviewer follow-up to expect:** "Route 53 failover — is that enough for DR by itself?" — no; per the Route 53 section above, DNS failover is TTL-bound and not instant. It's one component of Pilot Light/Warm Standby/Active-Active, not a complete DR strategy on its own.

---

## 3. Testing Resilience

### Testing Resilience: Fault Injection Simulator & Resilience Hub

The question that follows any DR answer is **"how do you know it works?"** — and "we documented the runbook" is a weak reply. An untested DR plan is an assumption, not a capability.

**AWS Fault Injection Service (FIS)** — managed **chaos engineering**. You define an experiment template that injects a real, controlled fault and observe whether the system behaves as designed:
- Faults available: **stop/terminate EC2 instances**, throttle or fail API calls, inject **CPU/memory/disk/network stress**, add **network latency or packet loss**, **fail over an RDS instance**, kill ECS tasks or EKS pods, and — the big one — simulate an **entire AZ becoming unavailable**.
- **Stop conditions** are the critical safety feature: FIS aborts the experiment automatically if a CloudWatch alarm you nominate breaches, so a test can't become the outage.
- The discipline: form a hypothesis ("if we lose one AZ, the ALB drains those targets and the ASG replaces them within 3 minutes with no 5xx"), run it in staging, then in production during business hours with the team watching, and treat any surprise as a finding.

**AWS Resilience Hub** — assesses an application against your stated **RTO/RPO targets**, scores its resilience, flags gaps (a single-AZ database behind a multi-AZ app tier, missing backups, no cross-region copy), recommends fixes, and generates **FIS experiment templates plus CloudWatch alarms** to validate them. It turns "we think we're resilient" into a measured number against a target.

**The answer that lands:** "I'd define RTO/RPO per workload, pick the DR strategy that meets them, then **prove it** — Resilience Hub to assess against the targets and FIS to inject the actual failure, with CloudWatch stop conditions so the experiment is safe. **GameDays** on a schedule, because a DR plan that hasn't been exercised in a year is a hypothesis. Also worth saying plainly: the most common real finding isn't infrastructure, it's the **ASG health-check type left on `EC2` instead of `ELB`**, so a hung application is never replaced — which is exactly the kind of thing only a fault injection test surfaces." (See [Auto Scaling Groups](08-load-balancing-autoscaling.md#auto-scaling-groups-asg).)

---

← [Migration & Data Transfer](17-migration-data-transfer.md) · [Index](README.md) · [Cross-Cutting Reference](19-cross-cutting-reference.md) →
