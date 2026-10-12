# Monolith-to-Microservices Migration Interview Guide --- Hinglish

## Phase 9: Advanced Interview Scenarios, Architecture Trade-offs & Mock Interviews

**Audience:** Senior Software Engineer, Staff Engineer, Tech Lead,
Solution Architect\
**Technology context:** .NET 8, C#, SQL Server, EF Core, AWS,
ECS/Fargate, SQS/SNS, OpenTelemetry\
**Style:** Hinglish (Roman Hindi + English technical terminology)

------------------------------------------------------------------------

## Phase objective

Phase 9 ka focus sirf definitions yaad karna nahi, balki follow-up
questions handle karna hai. Senior/Staff/Architect interviews mein
interviewer usually poochta hai:

-   Aapne yeh architecture kyun choose ki?
-   Agar dependency fail ho jaye to kya hoga?
-   Migration ke dauraan data loss kaise avoid karoge?
-   Trade-off kya hai, aur alternative kab better hoga?
-   Production incident mein pehle kya investigate karoge?
-   Team ko design implement karne ke liye kaise align karoge?

Answer structure: **Context → Options → Decision → Trade-offs → Failure
handling → Validation**.

------------------------------------------------------------------------

# 1. Architecture decision framework

Kisi bhi design decision ko defend karne ke liye yeh checklist use
karein:

1.  Business problem kya hai?
2.  Functional and non-functional requirements kya hain?
3.  Constraints: team size, skills, budget, legacy dependencies,
    compliance?
4.  Realistic options kya hain?
5.  Har option ka operational cost and failure mode kya hai?
6.  Chosen option kis requirement ko best satisfy karta hai?
7.  Kaunse signals batayenge ki decision successful hai?
8.  Kya migration/rollback path available hai?
9.  Decision reversible hai ya expensive-to-reverse?
10. Kya simpler alternative sufficient hai?

## Example: REST vs messaging

**Context:** Order create karne ke baad compliance workflow several
minutes le sakta hai.

-   Option A: synchronous REST call and wait.
-   Option B: durable async event/command.
-   Decision: order ko durable `Pending` state mein save karke async
    workflow start karna.
-   Trade-off: client ko immediate final result nahi milega; eventual
    consistency and workflow monitoring required.
-   Safeguards: transactional outbox, idempotency, explicit states,
    retries, DLQ, status API.
-   Validation: order completion time, stuck-order count, queue age,
    duplicate rate.

Interview mein "messaging is better" bolna enough nahi. Explain karein
ki asynchronous workflow business requirement ko kaise fit karta hai.

------------------------------------------------------------------------

# 2. Scenario 1 --- Should we split this monolith now?

**Prompt:** Legacy monolith deploy karna slow hai, but production
incidents rare hain. Business microservices maang raha hai.

### Strong answer

"Main immediately service extraction start nahi karunga. Pehle pain
points quantify karunga: release lead time, change failure rate, scaling
bottlenecks, team coordination, incident blast radius and module
coupling. Agar problem primarily code organization hai, modular monolith
plus module boundaries lower-risk option ho sakta hai. Agar Inventory ya
Integration capability independently scale/release karni padti hai, ek
bounded pilot extract karunga. Success metrics and rollback criteria
define karke evidence-based expansion karunga."

### Follow-up: Pilot service kaise choose karoge?

Choose a capability with: - Clear business boundary and owner. -
Manageable data ownership. - Meaningful independent deployment/scaling
need. - Limited synchronous dependencies. - Measurable success
criteria. - Reversible routing and compatibility strategy.

Avoid starting with the most coupled, highest-risk domain merely because
it is technically interesting.

------------------------------------------------------------------------

# 3. Scenario 2 --- Shared database: temporary or permanent?

**Prompt:** Five services same SQL Server database use karte hain. Team
says separate databases expensive hain.

### Strong answer

"Separate physical database per service is not an automatic requirement
on day one. I'd first enforce logical ownership: which service can write
which tables, scoped DB permissions, no cross-service direct writes, and
documented read paths. A shared physical SQL Server may remain a
transitional or cost-driven choice, but if every service freely queries
and modifies every table, we have distributed deployment units without
real data boundaries. I'd migrate incrementally when independent schema
evolution or isolation benefits justify the cost."

### Follow-up: Cross-service join ka alternative?

-   API composition for low-volume, interactive queries.
-   Read model/projection for repeated dashboard/report queries.
-   Events to maintain denormalized query models.
-   Avoid synchronous fan-out across many services for every page load.

------------------------------------------------------------------------

# 4. Scenario 3 --- Payment API timed out. Retry or not?

**Prompt:** Payment provider request timed out. Client doesn't know
whether charge succeeded.

### Strong answer

"Timeout means outcome is unknown, not necessarily failed. I would not
issue a new independent charge blindly. Use provider-supported
idempotency key and stable operation ID, persist an explicit
`Pending/Unknown` state, query provider status if supported, and
reconcile later. Retries must reuse the same idempotency identity. If
provider cannot guarantee idempotency, design a manual/reconciliation
path before automatic retrying."

### Key distinction

-   Transport timeout: response didn't arrive.
-   Business failure: provider explicitly rejected.
-   Unknown outcome: request may have succeeded remotely but response
    was lost.

This distinction is critical for payments, inventory reservations and
other scarce/irreversible operations.

------------------------------------------------------------------------

# 5. Scenario 4 --- Outbox backlog growing

**Prompt:** Order DB commits succeed, but published events are delayed.

### Investigation

1.  Measure outbox row count and oldest unpublished age.
2.  Check publisher errors, credentials, network and broker health.
3.  Inspect database query/index/lock performance.
4.  Check publisher concurrency and batch size.
5.  Verify poison events or schema serialization failures.
6.  Confirm retries are bounded and duplicate-safe.
7.  Ensure retention/cleanup isn't deleting unpublished rows.

### Mitigation

-   Restore publisher/broker path.
-   Scale publisher only if DB/broker capacity allows.
-   Pause unnecessary work if it competes for constrained resources.
-   Alert on age, not just row count.
-   Never delete backlog to make dashboard green without an approved
    recovery decision.

------------------------------------------------------------------------

# 6. Scenario 5 --- A consumer receives duplicate events

**Prompt:** Same `OrderCreated` event arrives three times.

### Strong answer

"Message delivery may be at-least-once, so consumer correctness cannot
depend on receiving exactly one delivery. I'd use a stable event ID,
durable deduplication or idempotent state transition, and commit the
processed marker with the business update where feasible. If consumer
triggers an external side effect, that side effect needs its own
idempotency key or reconciliation strategy."

### Common traps

-   Deduplication only in memory.
-   Deduplication key based on non-unique payload fields.
-   Marking event processed before business transaction commits.
-   Assuming FIFO delivery alone prevents duplicate business effects.
-   Returning success before durable state update.

------------------------------------------------------------------------

# 7. Scenario 6 --- One dealer overloads the system

**Prompt:** One dealer's bulk import saturates shared capacity; other
dealers experience high latency.

### Design response

-   Measure load by tenant/dealer in logs/traces and carefully bounded
    metrics.
-   Add per-tenant quotas/rate limits and fair scheduling.
-   Separate bulk workloads from interactive API capacity.
-   Use queue-based ingestion with bounded consumers.
-   Apply admission control and backpressure.
-   Consider tenant isolation or partitioning if the workload justifies
    it.
-   Avoid using raw dealer IDs as unlimited metric labels.

Trade-off: strict per-tenant quotas improve fairness but can constrain
legitimate bursts. Use plan-specific quotas, burst allowances and clear
rejection/retry semantics.

------------------------------------------------------------------------

# 8. Scenario 7 --- A downstream service is slow

**Prompt:** Sales API calls Inventory, Payment and Compliance. Payment
becomes slow.

### Response framework

1.  Trace the request and locate the dominant span.
2.  Check dependency RED metrics and resource saturation.
3.  Verify timeout budget and retry amplification.
4.  Protect callers with bulkheads/circuit breakers.
5.  Decide whether workflow can continue asynchronously.
6.  Return explicit pending/unknown state if the business semantics
    permit.
7.  Do not fake a successful payment or inventory reservation.
8.  Add a regression/load test and improve alerting.

A circuit breaker can protect capacity, but it does not fix the
dependency. Business fallback must preserve correctness.

------------------------------------------------------------------------

# 9. Scenario 8 --- New service data doesn't match legacy

**Prompt:** Shadow reads show 0.2% mismatches before cutover.

### Strong answer

"Percentage alone doesn't tell whether cutover is safe. I'd classify
mismatches by severity and type: missing records, duplicate records,
status mapping, rounding, time zones, stale CDC, or legitimate
representation differences. I'd block cutover for unexplained critical
invariants such as payment amount or vehicle ownership. Fix root cause,
rerun idempotent reconciliation and document accepted differences with
business approval."

Important: - Count equality does not prove correctness. - Compare
critical business invariants. - Verify replication lag and deletion
semantics. - Keep an audit trail for accepted exceptions.

------------------------------------------------------------------------

# 10. Scenario 9 --- New deployment causes errors

**Prompt:** Canary error rate rises from 0.2% to 3%; overall traffic is
low.

### Strong answer

-   Halt rollout based on agreed gates.
-   Compare candidate vs stable version and deployment markers.
-   Use traces/logs to identify error classes.
-   Check schema/API/event compatibility.
-   Decide rollback only if data side effects and compatibility make it
    safe.
-   If rollback unsafe, stop traffic expansion and follow roll-forward
    runbook.
-   Verify business outcomes and data integrity after mitigation.

Low traffic can make percentages noisy; use minimum sample counts,
absolute error counts and synthetic/business checks.

------------------------------------------------------------------------

# 11. Scenario 10 --- Queue backlog and SLA risk

**Prompt:** SQS queue has 40,000 visible messages.

### Investigation

Depth alone is insufficient. Check: - Oldest-message age. - Incoming vs
processed rate. - Consumer errors and retry count. - DLQ count and
poison messages. - Consumer CPU/memory and downstream throttling. -
Visibility timeout vs processing duration. - Ordering/partition
requirements.

If arrival rate is higher than processing rate, backlog grows. Scaling
consumers helps only if downstream dependencies have capacity and
messages can be processed in parallel safely.

Approximate drain time if arrival stops:
`Drain time ≈ backlog / effective processing rate`

If backlog = 40,000 and sustained processing = 200 messages/sec, ideal
drain time ≈ 200 seconds, excluding failures, variable processing time
and new arrivals.

------------------------------------------------------------------------

# 12. Scenario 11 --- How do you choose consistency?

  -----------------------------------------------------------------------
  Requirement                         Likely approach
  ----------------------------------- -----------------------------------
  Update a single service-owned       Local ACID transaction
  entity atomically                   

  DB update and event publication     Transactional outbox
  must stay aligned                   

  Dashboard can be slightly stale     Async read model / CQRS

  Long-running order workflow across  Saga with explicit states
  services                            

  Payment result ambiguous after      Idempotency + status
  timeout                             lookup/reconciliation

  Cross-service invariant must be     Reconsider boundary; coordinate
  strict                              workflow or keep invariant in one
                                      owner

  Historical replay is a core         Consider Event Sourcing after
  business requirement                evaluating complexity
  -----------------------------------------------------------------------

The most useful design move may be changing the domain boundary so a
strong invariant is owned by one service rather than coordinating every
operation across services.

------------------------------------------------------------------------

# 13. Scenario 12 --- Observability says API is healthy, business is not

**Prompt:** HTTP 200 rates are normal, but orders remain `Pending` for
hours.

### Response

-   Define business SLI: percentage of orders completed within target
    time.
-   Measure age/count by workflow state.
-   Inspect outbox age, queue age, consumer throughput and DLQ.
-   Trace representative stuck orders.
-   Verify state transitions and event schema compatibility.
-   Alert on oldest pending age, not only HTTP errors.
-   Create an idempotent repair/replay runbook with audit controls.

Technical health and business health must both be monitored.

------------------------------------------------------------------------

# 14. Scenario 13 --- Should we use Event Sourcing?

### Strong answer

"I'd start with domain requirements: is the full sequence of state
transitions a first-class requirement for audit, temporal queries,
replay or multiple projections? If yes, Event Sourcing may be justified.
If we only need reliable integration events, a relational current-state
model with transactional outbox and audit history may be simpler. Event
Sourcing introduces replay, event schema evolution, concurrency,
retention and projection-rebuild complexity, so I would justify it with
concrete requirements."

------------------------------------------------------------------------

# 15. Scenario 14 --- How to decompose a very large monolith?

### Approach

1.  Map business capabilities and current module dependencies.
2.  Identify shared tables and all writers.
3.  Create modular boundaries inside the monolith first if needed.
4.  Establish one authoritative writer per domain.
5.  Select a pilot with clear boundary and measurable benefit.
6.  Use Strangler routing to shift selected functionality.
7.  Use additive schema changes, backfill/CDC/outbox as appropriate.
8.  Shadow reads and reconcile.
9.  Canary cutover and monitor business SLIs.
10. Remove old path only after compatibility and recovery window.

Avoid a big-bang rewrite unless the system and business constraints
genuinely permit it.

------------------------------------------------------------------------

# 16. Mock Interview Round 1 --- Architecture

## Interviewer: Design an order-processing system for dealerships.

**Candidate response outline:**

**Requirements:** Order creation, vehicle reservation, payment
authorization, compliance checks, status tracking, tenant isolation and
auditability.

**Services:** Sales owns order lifecycle; Inventory owns vehicle
availability; Payments owns payment operations; Compliance owns
decision/workflow state; Integration handles external DMS adapters.

**API:** Gateway for edge policies; BFF for UI-specific composition if
needed.

**Workflow:** Sales stores initial order state plus outbox event in one
local transaction. Return `202 Accepted` with order ID if processing is
asynchronous. A durable orchestrator coordinates inventory, payment and
compliance.

**Consistency:** Local ACID per service, outbox for event publication,
idempotent consumers, Saga compensation and reconciliation for
cross-service workflow.

**Data:** Service-owned writes; reporting read model for cross-domain
dashboard.

**Reliability:** Timeouts, bounded retries, jitter, circuit breakers,
DLQ, trace propagation, RED metrics and business completion SLO.

**Security:** Validate identity and dealer authorization for every
operation; avoid trusting body-supplied `DealerId`.

**Deployment:** Compatible schema changes, automated contract tests,
canary rollout and safe rollback/roll-forward plan.

## Interviewer follow-up: What if inventory reservation succeeds but payment fails?

Candidate: - Order moves to a defined payment-failed/pending state. -
Retry only if error is transient and operation is idempotent. - If
payment is definitively failed, issue an idempotent inventory-release
command. - Track compensation status; don't mark workflow failed until
required compensation is complete or explicitly escalated. - Monitor
stuck compensation and support manual recovery.

## Interviewer follow-up: What if payment timed out?

Candidate: - Mark outcome unknown/pending, not definitely failed. -
Query provider status or retry with same provider idempotency key. -
Reconcile before starting a new charge. - Avoid releasing inventory if
business rules require reservation until payment outcome is known.

------------------------------------------------------------------------

# 17. Mock Interview Round 2 --- Migration

## Interviewer: Migrate Orders from shared SQL Server to a new Sales database with no planned downtime.

**Candidate response outline:**

1.  Inventory all table writers/readers, jobs, triggers, reports and
    external consumers.
2.  Establish Sales as authoritative owner for order writes.
3.  Add compatible schema and deploy code that can coexist with legacy
    version.
4.  Snapshot/backfill in checkpointed, idempotent batches.
5.  Capture ongoing changes with a validated CDC or outbox/event
    strategy.
6.  Track replication lag and reconcile counts, key fields and business
    invariants.
7.  Shadow read new Sales database without side effects.
8.  Canary read traffic, then coordinate write ownership transfer.
9.  Stop legacy writes and prevent stale instances from writing.
10. Verify end-to-end order completion, duplicate rates, errors and lag.
11. Retain tested recovery plan; rollback may need reverse sync after
    new writes begin.
12. Contract old schema only after old dependencies are retired.

## Follow-up: Can we dual-write to both databases?

Candidate: "Not naively. Two independent commits can diverge. I'd prefer
one authoritative writer and transactional outbox/CDC replication. If
temporary dual writes are unavoidable, I need durable operation IDs,
idempotent writes, reconciliation, divergence alerts and a retirement
deadline."

------------------------------------------------------------------------

# 18. Mock Interview Round 3 --- Production Incident

## Interviewer: Order API p95 latency increased from 300 ms to 2 seconds.

**Candidate response:**

1.  Confirm impact scope and time window.
2.  Check request rate, error rate, p95/p99 and affected
    endpoint/tenant.
3.  Compare traces to identify slow spans.
4.  Check dependency latency, DB waits, pool saturation and queue lag.
5.  Correlate logs using trace ID and inspect release/config changes.
6.  Apply bounded mitigation---halt rollout, reduce concurrency,
    circuit-break failing dependency, or roll back if safe.
7.  Verify business completion and data integrity after recovery.
8.  Create a blameless postmortem with owners and prevention actions.

## Follow-up: CPU is 95%; should you scale out?

Candidate: "Possibly, but first check whether CPU is the bottleneck and
whether downstream dependencies can absorb more traffic. If the issue is
lock contention or a saturated DB, scaling app replicas can worsen load.
I'd correlate utilization, saturation, latency and errors before
scaling."

------------------------------------------------------------------------

# 19. Mock Interview Round 4 --- Staff/Architect leadership

## Q1. Product wants a full microservices rewrite in one quarter. What do you do?

Clarify business outcomes and deadlines, quantify migration risk,
propose incremental extraction with a pilot and milestones, and explain
the opportunity cost of a rewrite. Present options with measurable
criteria rather than rejecting the request abstractly.

## Q2. Two teams disagree about service ownership.

Facilitate a domain workshop using business capabilities, invariants,
write ownership, change frequency, operational responsibility and team
boundaries. Document the decision and revisit trigger. Avoid deciding
ownership only by current table layout.

## Q3. Team wants Kubernetes but has limited operations experience.

Compare requirements, total cost and operational maturity. Recommend a
managed/simple platform if it satisfies needs; identify what additional
capabilities or staffing would justify Kubernetes. Make the choice
evidence-based.

## Q4. How do you measure migration success?

Combine: - Deployment lead time and release independence. - Change
failure rate and recovery time. - Service SLOs and incident impact. -
Business workflow completion and latency. - Data mismatch/duplicate
rate. - Operational cost and developer cognitive load. - Reduction in
cross-team coordination and release coupling.

Microservice count is not a success metric.

## Q5. How do you communicate an architectural trade-off?

State the decision and context, list realistic alternatives, compare
reliability/latency/cost/complexity, explain why chosen option fits
current constraints, document risks and revisit triggers, then assign
measurable validation.

------------------------------------------------------------------------

# 20. Rapid-fire revision

**Monolith vs microservices?** Deployment/ownership boundaries and
trade-offs, not merely code size.

**Database-per-service?** Service owns writes and schema evolution;
physical separation can be incremental.

**Outbox?** Atomically store business change and event in one local
transaction.

**Saga?** Coordinate local transactions and compensations across
services.

**CQRS?** Separate command and query models where needs justify it.

**Event Sourcing?** Store events as primary state history; use only when
replay/audit needs justify complexity.

**Idempotency?** Repeated operation produces no duplicate business
effect.

**At-least-once delivery?** Consumer must tolerate duplicate delivery.

**Circuit breaker?** Temporarily stop calls to a failing dependency.

**Readiness?** Should this instance receive traffic?

**Liveness?** Should this process be restarted?

**CDC?** Capture source data changes for replication/synchronization.

**Expand-and-contract?** Add compatible schema, migrate usage, remove
old structure later.

**Canary?** Expose small traffic cohort and expand after validation.

**SLO?** Internal reliability objective measured through an SLI.

**Trace vs correlation ID?** Trace ID belongs to distributed trace;
correlation ID may be application-defined.

**Exactly-once?** End-to-end business effect needs application-level
transaction/idempotency design.

**When not to use microservices?** When independent
scaling/deployment/ownership benefits do not justify distributed
complexity.

------------------------------------------------------------------------

# 21. Personal answer framework for senior interviews

Use this response pattern for almost any design question:

1.  "First, I'd clarify the business and non-functional requirements."
2.  "I see two or three realistic options..."
3.  "Given these constraints, I'd choose..."
4.  "The trade-off is..."
5.  "The main failure modes are..."
6.  "I'd mitigate them through..."
7.  "I'd measure success using..."
8.  "I'd revisit the decision if..."

This structure shows judgment, not just pattern memorization.

------------------------------------------------------------------------

# 22. Final 2-minute interview pitch

"My approach to monolith-to-microservices migration is incremental and
business-driven. I first map capabilities, dependencies and data
writers, then identify boundaries where independent ownership,
deployment or scaling creates measurable value. I avoid splitting only
by technical layers or tables. For each service, I establish data
ownership, stable API/event contracts and operational responsibility.
During migration I use backward-compatible schema evolution,
checkpointed backfill, CDC or outbox-based propagation where
appropriate, shadow validation and reconciliation before cutover. For
distributed workflows, I use local transactions, idempotency and Saga
compensation rather than assuming global ACID or exactly-once delivery.
I design timeouts, bounded retries, circuit breakers, observability and
SLOs from the beginning. Deployment strategy is risk-based---rolling,
canary or blue/green---with compatibility and recovery plans. Finally, I
measure success through business outcomes, reliability, deployment
independence, incident recovery and operating cost---not the number of
services created. If the organization doesn't yet need independent
deployment or scaling, a well-structured modular monolith may be the
better intermediate or long-term choice."

------------------------------------------------------------------------

# 23. Final readiness checklist

Before the interview, practise speaking aloud about:

-   [ ] Why microservices---and when not to use them.
-   [ ] How you choose service boundaries.
-   [ ] Shared database decomposition and ownership.
-   [ ] Expand-and-contract, backfill, CDC and reconciliation.
-   [ ] Outbox, Inbox, idempotency and duplicate delivery.
-   [ ] Saga, CQRS and Event Sourcing trade-offs.
-   [ ] REST vs async messaging.
-   [ ] Timeout, retry, circuit breaker and bulkhead decisions.
-   [ ] OpenTelemetry, RED/USE and production incident diagnosis.
-   [ ] SLI/SLO/SLA and business-level monitoring.
-   [ ] Docker, CI/CD, ECS/Fargate vs Kubernetes.
-   [ ] Multi-tenant authorization and noisy-neighbour protection.
-   [ ] Capacity estimation, peak factors and queue drain time.
-   [ ] Rollback vs roll-forward after data-changing releases.
-   [ ] A 2-minute architecture pitch and a real project story with
    measurable impact.

------------------------------------------------------------------------

## End of Phase 9

You now have a nine-phase interview-preparation series covering
migration foundations, data ownership, communication, security,
observability, database migration, deployment, architecture patterns and
mock interviews.

**Suggested next step:** Consolidate the phases into a single master
guide and use the scenario sections as mock-interview drills.
