> **AWS Quick Revision Notes** · [Index](README.md) · Part IV

# Cross-Cutting Reference

## Best Practices
**Lambda/Serverless:** one function one responsibility (no fat Lambda); async, lightweight; IaC not console; provisioned concurrency + AOT for latency-sensitive; Step Functions for long work; minimise VPC (endpoints once in); create clients/connections outside handler.
**Messaging:** design for at-least-once (idempotency mandatory); one SQS queue per consumer under SNS fan-out; delete after success; visibility timeout > consumer max; FIFO + Group/DedupId when order/exactly-once matters.
**IAM:** roles over users; OIDC over static keys; least privilege incrementally; one role per service, named; short sessions; CloudTrail on AssumeRole.
**Data:** DynamoDB access patterns first; RDS separate HA (Multi-AZ) from read-scaling (replica); S3 storage class by real access pattern (Intelligent-Tiering if unsure).
**Networking:** NAT per AZ; endpoints over NAT for AWS traffic; DBs in private/isolated subnets; SGs primary defence.
**Observability:** structured JSON + correlate X-Ray trace IDs; alarm on user-pain metrics (queue depth/p99/throttles) not CPU; explicit log retention.

## Common Pitfalls (Cross-Cutting)
Assuming exactly-once (all async is at-least-once — idempotency is yours) · assuming DNS failover instant (TTL-bound) · conflating Multi-AZ (HA) with Read Replicas (scale) · Fargate "always cheaper" (only bursty/low) · NAT must be in a public subnet / forgetting it for VPC Lambda/CodeBuild/ECS · publishing events before DB commit · hardcoding creds vs roles/OIDC · ignoring trust-vs-permission policy separation · DynamoDB Scan in hot path / low-cardinality PK · believing CloudWatch alarms alone = observability (no X-Ray to explain why).

## Sample Interview Q&A
- **Resilient order pipeline:** API GW/ALB → Lambda/ECS ingest → DynamoDB PENDING (idempotency key via conditional write) → SNS → per-consumer SQS (billing/notification/inventory) → workers with conditional state updates + DLQ + alarms on DLQ depth/age. Idempotent downstream (at-least-once). Structured logs + X-Ray by correlation ID; load-test capacity mode + concurrency before go-live.
- **Lambda p99 cold starts, in order:** confirm it's cold starts (`Init Duration` in REPORT) → .NET 8 Native AOT → trim package + remove VPC if not needed (or add endpoints if it is) → Provisioned Concurrency sized to p95 → re-measure. Don't jump to provisioned concurrency (ongoing cost) before cheaper fixes.
- **DynamoDB over RDS?** DynamoDB when access patterns known, ms latency at spiky scale, tolerates denormalization (no ad-hoc joins). RDS/Aurora when relational integrity/ad-hoc/joins/reporting or existing EF Core. Don't force DynamoDB on a reporting-heavy system ("cargo-culting").
- **Trust vs Permission policy separation:** who can assume vs what once assumed; different threat models + owners (security owns trust boundaries, service team owns what the role touches); AWS disallows merging at the API level.
- **DynamoDB throttling under provisioned limit:** hot partition (traffic on one PK value, limits enforced per-partition). Adaptive Capacity smooths but doesn't fix bad key. Fix = higher-cardinality PK (random/bucketed suffix) or a better-distributed GSI.
- **ECS/Fargate vs Lambda to a non-technical stakeholder:** Lambda = rent a car per trip (pay per trip, no maintenance, engine-start delay = cold start, max 15-min trips); Fargate = lease a car always running (no start delay, no trip limit, pay even when idle). Spiky short → Lambda; steady always-on → Fargate.

---

← [Well-Architected & Resilience](18-well-architected-resilience.md) · [Index](README.md) · [Changelog — Summary of Additions](20-changelog-additions.md) →
