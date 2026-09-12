> **AWS Quick Revision Notes** · [Index](README.md) · Part IV

# Cross-Cutting Reference

## 20. Best Practices
- **Lambda:** one responsibility, async, IaC not clicks, provisioned concurrency + AOT for latency, Step Functions for long work, minimise VPC (endpoints if VPC), DB clients outside handler.
- **Messaging:** design at-least-once (idempotency mandatory), SNS fan-out + per-consumer SQS, delete after success, visibility timeout ≥ max processing, FIFO for strict order.
- **IAM:** roles not users, OIDC not static keys, least privilege incremental, one role per service, short sessions, CloudTrail on AssumeRole.
- **Data:** DynamoDB access patterns first; RDS separate HA from read-scaling; S3 storage class by access pattern (Intelligent-Tiering if unsure).
- **Networking:** NAT per AZ, VPC endpoints for AWS traffic, DB in private subnets, SG primary + NACL sparingly.
- **Observability:** structured JSON + correlate trace IDs, alarm on user-pain metrics (queue depth/p99/throttles), explicit log retention.

## 21. Common Pitfalls (Cross-Cutting)
- Assuming exactly-once (Lambda/SQS/SNS at-least-once — idempotency = app responsibility).
- Assuming DNS failover instant (TTL-bound; combine with LB health routing).
- Conflating Multi-AZ (HA) with Read Replicas (scale).
- "Fargate always cheaper" (only bursty/low-util).
- NAT Gateway not in public subnet / forgetting it for VPC Lambda/CodeBuild/ECS outbound.
- Publishing domain events before DB commit.
- Hardcoding credentials.
- Ignoring trust vs permission policy separation.
- DynamoDB Scan in hot path / low-cardinality PK.
- Believing alarms alone = observability (no traces for *why*).

## 22. Sample Interview Q&A
- **Resilient order pipeline:** API GW/ALB → ingest (PENDING + idempotency conditional write) → SNS `order_created` → per-consumer SQS → workers (DynamoDB conditional updates, DLQ + alarms), idempotent downstream, structured logs + X-Ray, load-test capacity/concurrency.
- **Cold-start p99:** confirm cold start (`Init Duration`) → .NET 8 AOT → trim package + remove VPC (or add endpoints) → provisioned concurrency at p95 → re-measure. Cheaper fixes before provisioned concurrency (hourly cost).
- **DynamoDB vs RDS:** DynamoDB = known access patterns, single-digit-ms, high/spiky scale, denormalisation ok. RDS/Aurora = relational integrity, ad-hoc/joins/reporting, existing EF Core. Don't force DynamoDB on reporting-heavy back office (cargo-culting).
- **Trust vs Permission Policy:** who can assume (principal) vs what after assume; separate because different threat models + owners (security team vs service team); merging conflates "can enter" with "can do."
- **DynamoDB throttling with low consumed capacity:** hot partition (per-partition limits); Adaptive Capacity smooths not fixes; redesign PK for cardinality or better-distributed GSI.
- **ECS/Fargate vs Lambda (non-technical):** Lambda = rent car per trip (pay per use, no maintenance, "engine start" delay, 15-min limit); ECS/Fargate = lease always-ready car (no start delay/limit, pay for idle). Spiky → Lambda; steady → Fargate.

---

← [Well-Architected & Resilience](18-well-architected-resilience.md) · [Index](README.md) · [Changelog — Summary of Additions](20-changelog-additions.md) →
