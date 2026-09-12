> **AWS Quick Revision Notes** · [Index](README.md) · Part III

# Well-Architected & Resilience

---

## 1. Well-Architected Framework

### Well-Architected — 6 Pillars

| Pillar | Question | .NET example |
|---|---|---|
| Operational Excellence | run/monitor/improve? | IaC, structured logging, CI/CD rollback |
| Security | protect data/systems? | IAM least-priv, Secrets Manager, encryption, WAF |
| Reliability | perform correctly/consistently? | Multi-AZ, auto-scaling, retries+backoff (Polly), DLQs |
| Performance Efficiency | resources efficient? | right-sized, caching, async, .NET AOT |
| Cost Optimization | avoid unnecessary cost? | SP/Spot, S3 lifecycle, right-sizing, tagging |
| Sustainability | minimise impact? | region, Graviton, scale-to-zero |
- **Rapid recall:** "Run it well, keep it safe, keep it up, keep it fast, keep it cheap, keep it green."
- Structure open-ended "evaluate this architecture" answers around the 6 pillars. Basis of Well-Architected Tool/Reviews.

---

## 2. Disaster Recovery

### DR Strategies

```
Backup & Restore → Pilot Light → Warm Standby → Multi-Site Active-Active
  RPO/RTO hours    RPO min/RTO    RPO/RTO min     RPO/RTO ~0
                   10s of min
```
| Strategy | Description | RTO/RPO | Notes |
|---|---|---|---|
| Backup & Restore | backups in DR, restore on disaster | hours | cheapest, AWS Backup, **test restores** |
| Pilot Light | core (DB replica) minimal running | tens of min / min | RDS cross-region replica warm, app IaC scaled 0 |
| Warm Standby | scaled-down full stack running | minutes | Global Tables/Aurora Global, smaller service count |
| Multi-Site Active-Active | 2+ regions full live | ~0 | R53 latency/weighted, Global Tables, idempotent writes |
- Follow-ups: DR Lambda concurrency needs pre-raise (default 1,000); Route 53 failover alone insufficient (TTL-bound), it's a component.

---

## 3. Testing Resilience

### Testing Resilience

- **FIS (Fault Injection Service):** managed chaos — stop/terminate EC2, throttle APIs, CPU/memory/network stress, RDS failover, kill ECS/EKS, **simulate AZ unavailable**. **Stop conditions** (CloudWatch alarm auto-abort). Hypothesis → staging → prod (business hours, watched) → treat surprises as findings.
- **Resilience Hub:** assess vs RTO/RPO targets, score, flag gaps, generate FIS templates + alarms. "Prove it works — Resilience Hub assess + FIS inject + GameDays. Most common real finding = **ASG health-check `EC2` not `ELB`** → hung app never replaced (only fault injection surfaces this)."

---

← [Migration & Data Transfer](17-migration-data-transfer.md) · [Index](README.md) · [Cross-Cutting Reference](19-cross-cutting-reference.md) →
