> **AWS Quick Revision Notes** · [Index](README.md) · Part III

# Well-Architected & Resilience

---

## 1. Well-Architected Framework

### 6 Pillars

Operational Excellence (run/monitor/improve — IaC, logging, CI/CD rollback) · Security (protect — IAM, Secrets Manager, encryption, WAF) · Reliability (correct/consistent — Multi-AZ, autoscaling, retries+backoff/jitter (Polly), DLQs, chaos testing) · Performance Efficiency (efficient resources — right-sizing, caching, async, AOT) · Cost Optimization (avoid waste — SP/Spot, lifecycle, tagging) · Sustainability (env impact — region, Graviton, scale-to-zero). Structure open-ended architecture answers around these; basis for Well-Architected Tool/Reviews.
**Rapid recall:** "Run it well, keep it safe, keep it up, keep it fast, keep it cheap, keep it green."

---

## 2. Disaster Recovery

### Disaster Recovery Strategies

`Backup&Restore → Pilot Light → Warm Standby → Multi-Site Active-Active` (cheaper/slower → costlier/faster).
| Strategy | RTO/RPO | Implementation |
|---|---|---|
| Backup & Restore | Hours | Snapshots/PITR/CRR to DR region; AWS Backup; **test restores** |
| Pilot Light | RTO 10s min / RPO min | Core (DB replica) warm; app tier IaC scaled to zero until failover |
| Warm Standby | Minutes or less | Scaled-down full stack running; Global Tables/Aurora Global; scale up on failover |
| Multi-Site Active-Active | ~0 | Full capacity in 2+ regions; Route 53 latency/weighted; conflict-tolerant/idempotent writes |

Lambda concurrency for DR: passive DR region still has default 1,000 limit unless pre-raised. Route 53 failover alone ≠ DR (TTL-bound).

---

## 3. Testing Resilience

### Testing Resilience: FIS & Resilience Hub

"How do you know it works?" — untested DR = assumption. **AWS Fault Injection Service (FIS)** = managed chaos: stop/terminate EC2, throttle/fail APIs, CPU/memory/disk/network stress, latency/loss, RDS failover, kill tasks/pods, **simulate a whole AZ down**. **Stop conditions** (abort on a CloudWatch alarm) = the safety feature. Hypothesis → staging → prod business-hours with team watching → surprise = finding. **Resilience Hub** = assess app vs RTO/RPO targets, score, flag gaps, generate FIS templates + alarms. "Define RTO/RPO, pick strategy, **prove it** (Resilience Hub + FIS + stop conditions), GameDays on a schedule. Most common finding = ASG health-check type on `EC2` not `ELB` → hung app never replaced."

---

← [Migration & Data Transfer](17-migration-data-transfer.md) · [Index](README.md) · [Cross-Cutting Reference](19-cross-cutting-reference.md) →
