> **AWS Detailed Guide** · [Index](README.md) · Part IV

# Cross-Cutting Reference

## Best Practices

**Lambda / Serverless**
- One function = one responsibility; avoid "fat Lambda" business logic.
- Use async programming; keep functions and packages lightweight.
- Manage infra via CloudFormation/SAM/CDK/Terraform, not console clicks.
- Provisioned Concurrency for latency-sensitive production APIs; .NET AOT to shrink cold starts further.
- Externalize long-running work to Step Functions.
- Minimize VPC usage for Lambda unless private resource access is required; prefer VPC Endpoints once inside a VPC.
- Create DB connections/clients outside the handler to survive warm reuse.

**Messaging**
- Always design for at-least-once delivery — idempotency is not optional.
- One SQS queue per consumer under an SNS fan-out topic.
- Delete SQS messages only after successful processing.
- Align SQS visibility timeout with (and slightly exceed) consumer max processing time.
- Use FIFO + `MessageGroupId`/`MessageDeduplicationId` when strict ordering/exactly-once matters.

**IAM**
- Roles over users for anything automated; OIDC over static keys for CI/CD.
- Least privilege by default; add permissions incrementally, not "just in case."
- One role per service, clearly named; short session durations; CloudTrail on all AssumeRole activity.

**Data**
- DynamoDB: design access patterns first, then keys/indexes — not the other way around.
- RDS: separate HA (Multi-AZ) concerns from read-scaling (read replica) concerns; don't conflate them.
- S3: pick storage class and lifecycle rules based on actual access pattern, not guesswork — use Intelligent-Tiering when unsure.

**Networking**
- NAT Gateway per AZ for HA; VPC Endpoints over NAT for AWS-service-only traffic.
- Databases always in private (ideally isolated, no-NAT) subnets.
- Security Groups as primary defense; NACLs sparingly for coarse subnet blocking.

**Observability**
- Structured JSON logging everywhere; correlate logs and X-Ray trace IDs.
- Alarms on the metrics that actually predict user pain (queue depth, p99 latency, throttle counts) — not just CPU%.
- Set log retention policies explicitly; don't leave "Never Expire" as a silent cost leak.

---

## Common Pitfalls (Cross-Cutting)

- **Assuming exactly-once anywhere in AWS async messaging** — Lambda, SQS, and SNS are all at-least-once by default; idempotency is the application's responsibility, not the platform's guarantee.
- **Assuming DNS failover (Route 53) is instant** — it's TTL-bound; combine with load-balancer-level health-based routing for fast reaction.
- **Conflating Multi-AZ (HA) with Read Replicas (scale)** in RDS — different mechanisms, different purposes, different failover semantics.
- **Treating Fargate as "always cheaper" than EC2** — it's cheaper only for bursty/low-utilization workloads.
- **Forgetting NAT Gateway must live in a public subnet**, or forgetting it entirely for VPC-bound Lambda/CodeBuild/ECS tasks that need outbound internet.
- **Publishing domain events before the DB transaction commits** — breaks the "read model eventually reflects a committed write" invariant that CQRS depends on.
- **Hardcoding credentials instead of using roles/OIDC.**
- **Ignoring the separation between trust policy and permission policy** in IAM — both are needed, evaluated independently.
- **Using DynamoDB Scan in a hot path**, or choosing a low-cardinality/time-based partition key that creates a hot partition.
- **Believing CloudWatch alarms alone constitute "observability"** without traces (X-Ray) to explain *why* a metric moved.

---

## Sample Interview Q&A

**Q: Walk me through how you'd design a resilient order-processing pipeline on AWS for a .NET system.**
A: API Gateway/ALB → Lambda or ECS ingest service writes to DynamoDB with status `PENDING` (idempotency key checked via conditional write) → publishes `order_created` to an SNS topic → SNS fans out to per-consumer SQS queues (billing, notification, inventory) → each Lambda/ECS worker processes its queue, using DynamoDB conditional updates for state transitions, with a DLQ configured (`maxReceiveCount`) and CloudWatch alarms on DLQ depth and queue age. Everything downstream is idempotent because SQS is at-least-once. I'd instrument with structured logs + X-Ray tracing tied by a correlation ID, and load-test to validate DynamoDB capacity mode and Lambda concurrency sizing before go-live.

**Q: Your Lambda-backed API has unacceptable p99 latency due to cold starts. What do you do, in order?**
A: First confirm it's actually cold starts (CloudWatch Logs `Init Duration` in the `REPORT` line, not just slow code) → migrate to .NET 8 Native AOT if not already → trim package size and remove VPC attachment if not strictly needed (or add VPC Endpoints if it is needed) → add Provisioned Concurrency sized to p95 traffic → re-measure. I would not jump straight to Provisioned Concurrency before ruling out cheaper architectural fixes (AOT, VPC removal), since Provisioned Concurrency has an ongoing hourly cost.

**Q: When would you choose DynamoDB over RDS for a new .NET service, and when would you not?**
A: DynamoDB when access patterns are known upfront, need is single-digit-ms latency at high/spiky scale, and the data model tolerates denormalization (no complex ad hoc joins/reporting). RDS (or Aurora) when the domain genuinely needs relational integrity, ad hoc queries, complex joins/reporting, or the team's existing tooling/ORM (EF Core) and skill set make relational the faster, lower-risk path. I wouldn't force DynamoDB onto a reporting-heavy back office system just because it's "cloud-native" — that's cargo-culting, not architecture.

**Q: Explain the difference between a Trust Policy and a Permission Policy, and why AWS keeps them separate.**
A: Trust policy defines *who* can assume a role (the principal); permission policy defines *what* that role can do once assumed. They're kept separate because the two questions have different threat models and different owners in practice (a security team might own trust boundaries/cross-account access, while a service team owns what their own service's role can touch) — merging them into one document would conflate "can enter" with "can do," which AWS explicitly disallows at the API level.

**Q: Your DynamoDB table is throttling even though total consumed capacity looks well under the provisioned limit. Why, and what do you do?**
A: Classic hot partition — traffic is concentrated on one partition key value even though aggregate table-level capacity looks fine, because DynamoDB enforces limits per-partition, not just per-table. Adaptive Capacity helps smooth this automatically but isn't a fix for a bad key design. The real fix is redesigning the partition key for higher cardinality (e.g., adding a random/bucketed suffix) or introducing a GSI with a better-distributed key for that access pattern.

**Q: How would you explain the trade-off between ECS/Fargate and Lambda for a new .NET microservice to a non-technical stakeholder?**
A: Lambda is like renting a car only when you need to drive — you pay per trip, no maintenance, but there's a moment of "starting the engine" each time you haven't driven recently (cold start), and you can't take a trip longer than 15 minutes. ECS/Fargate is like leasing a car that's always running and ready — no start-up delay and no trip-length limit, but you're paying for it even during the minutes you're not driving. For spiky, short-lived work, Lambda is cheaper and simpler; for steady, always-on services, ECS/Fargate is more predictable and cost-effective.

---

← [Well-Architected & Resilience](18-well-architected-resilience.md) · [Index](README.md) · [Changelog — Summary of Additions](20-changelog-additions.md) →
