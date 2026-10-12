# Monolith-to-Microservices Migration Interview Guide --- Hinglish

## Phase 8: Microservices Architecture Patterns & End-to-End System Design

**Audience:** Senior Software Engineer, Staff Engineer, Tech Lead,
Solution Architect\
**Technology context:** .NET 8, C#, SQL Server, AWS, ECS/Fargate,
SQS/SNS, API Gateway\
**Style:** Hinglish (Roman Hindi + English technical terminology)

------------------------------------------------------------------------

## Phase objective

Is phase mein individual patterns ko isolated definitions ke roop mein
nahi, balki complete system design mein combine karna seekhenge.
Interviewer usually yeh dekhna chahta hai ki aap requirements clarify
karte hain, boundaries choose karte hain, scale estimate karte hain,
trade-offs explain karte hain aur failure cases cover karte hain.

Is phase ke baad aap: - System-design interview ko structured way mein
drive kar sakenge. - API Gateway, BFF, service discovery, caching and
rate limiting choose kar sakenge. - Saga, Outbox, CQRS, Event Sourcing
and idempotency ke trade-offs explain kar sakenge. - Multi-tenant
DMS/order processing architecture design kar sakenge. - Capacity
estimates and bottlenecks calculate kar sakenge. - Reliability,
security, data consistency and deployment decisions justify kar sakenge.

------------------------------------------------------------------------

# 1. System-design interview ka structure

Ek senior-level system-design answer ko usually is order mein present
karein:

1.  **Clarify requirements:** users, workflows, scale, latency,
    availability, compliance.
2.  **Define scope:** in-scope and out-of-scope capabilities.
3.  **Estimate load:** requests/sec, storage, peak factors, read/write
    ratio.
4.  **Identify domain boundaries:** business capabilities and data
    ownership.
5.  **Draw high-level architecture:** client, gateway, services,
    databases, broker.
6.  **Explain key workflows:** synchronous and asynchronous paths.
7.  **Discuss data consistency:** transaction boundaries, idempotency,
    Saga/Outbox.
8.  **Scale and performance:** caching, partitioning, concurrency,
    backpressure.
9.  **Reliability and security:** retries, timeouts, auth, tenant
    isolation, observability.
10. **Deployment and evolution:** migration, canary, rollback and
    backward compatibility.
11. **Trade-offs:** what you deliberately did not build and why.

**Interview tip:** Diagram banane se pehle requirements clarify karein.
Har system ko 20 microservices ki zaroorat nahi hoti.

------------------------------------------------------------------------

# 2. Requirements for a DMS order-processing system

Assume a dealership management platform that supports:

-   Dealers and user roles.
-   Vehicle inventory and availability.
-   Order creation and status tracking.
-   Payment authorization.
-   Compliance checks and document workflows.
-   External DMS integrations.
-   Audit logs and operational reporting.

## Functional requirements

-   Dealer user order create kar sake.
-   Inventory availability verify/reserve ho.
-   Payment authorization attempt ho.
-   Compliance workflow trigger ho.
-   Order status track ho.
-   External integration events safely process hon.
-   Failed asynchronous processing retry and recovery support kare.

## Non-functional requirements

Clarify rather than invent hard guarantees:

-   Expected daily/monthly transactions and peak request rate.
-   p95/p99 latency targets.
-   Availability and recovery objectives.
-   Data retention and audit requirements.
-   Tenant/dealer data isolation.
-   Payment correctness and duplicate prevention.
-   Eventual consistency tolerance for status and reporting.
-   Regional deployment and disaster recovery expectations.

For an interview, if requirements are missing, state assumptions
explicitly and ask interviewer to confirm them.

------------------------------------------------------------------------

# 3. Domain boundaries and service ownership

Possible starting boundaries:

  ------------------------------------------------------------------------------
  Service                 Owns                           Typical operations
  ----------------------- ------------------------------ -----------------------
  Dealer/Identity         Dealer profile references,     Get dealer context,
                          role mapping                   manage roles

  Inventory               Vehicle availability and       Search, reserve,
                          reservation                    release

  Sales/Orders            Order lifecycle and order      Create order, query
                          state                          status

  Payments                Payment                        Authorize, capture,
                          authorization/capture/refund   refund
                          state                          

  Compliance              Compliance cases and decision  Validate, request
                          state                          documents

  Integration             External system adapters and   Import/export,
                          mapping                        transform, retry

  Notifications           Notification delivery state    Email/SMS notifications

  Reporting               Read models and analytical     Dashboard and reporting
                          views                          queries
  ------------------------------------------------------------------------------

This is a candidate decomposition, not a mandatory list. Start from
business capabilities and team ownership; avoid creating a separate
service for every table or CRUD entity.

## Service boundary principles

-   One service owns authoritative writes for a domain entity.
-   Other services use APIs/events or read models, not direct database
    writes.
-   Related data that needs strong local transaction consistency often
    belongs together.
-   Separate service only when independent change/scaling/security/team
    ownership justifies operational cost.
-   Keep cross-service synchronous call chains short.

------------------------------------------------------------------------

# 4. High-level architecture

``` mermaid
flowchart TD
    UI[Dealer Web UI] --> GW[API Gateway]
    GW --> BFF[Dealer BFF]
    BFF --> S[Sales / Orders]
    BFF --> I[Inventory]
    S --> SDB[(Sales DB)]
    I --> IDB[(Inventory DB)]
    S --> P[Payments]
    P --> PDB[(Payments DB)]
    S --> O[(Sales Outbox)]
    O --> PUB[Outbox Publisher]
    PUB --> Q[Message Broker]
    Q --> C[Compliance Service]
    Q --> R[Reporting Read Model]
    Q --> N[Notifications]
    S --> OBS[Logs, Metrics, Traces]
    I --> OBS
    P --> OBS
    C --> OBS
```

This diagram is conceptual. BFF may call several services, but should
not become a second monolith with all business logic. For high-latency
workflows, APIs can return accepted/pending state and clients can
poll/subscribe for status updates.

------------------------------------------------------------------------

# 5. API Gateway vs BFF vs service discovery

## API Gateway

Common concerns: - External routing. - TLS termination (depending on
topology). - Authentication integration. - Rate limiting and request
size controls. - WAF/security policies and request-level telemetry.

Gateway mein domain business logic or complex orchestration accumulate
karna avoid karein.

## BFF (Backend for Frontend)

BFF UI-specific needs ke liye APIs compose/shape karta hai. Example:
dashboard ke liye order summary, inventory availability and notification
count combine karna.

Benefits: - UI-specific response models. - Client round trips reduce ho
sakte hain. - Different clients (web/mobile/partner) ke liye tailored
APIs.

Risks: - Too much business logic BFF mein aa sakta hai. - BFF fan-out
latency and failure dependencies introduce karta hai. - API composition
calls need timeouts and partial-failure policy.

## Service discovery

Service-to-service location resolution mechanism. ECS service discovery,
load balancers or Kubernetes DNS can provide it. Discovery tells caller
where to reach a service; it does not replace authentication,
authorization, health checks or resilience.

**Interview answer:** Gateway external edge ko manage karta hai; BFF
client-specific composition karta hai; service discovery internal
service locations resolve karta hai. Responsibilities overlap ho sakti
hain, but roles conceptually different hain.

------------------------------------------------------------------------

# 6. Order creation: synchronous vs asynchronous design

## Option A: Synchronous all-the-way

``` mermaid
sequenceDiagram
    participant UI as Dealer UI
    participant S as Sales API
    participant I as Inventory
    participant P as Payments
    participant C as Compliance
    participant DB as Sales DB

    UI->>S: POST /orders
    S->>I: Reserve vehicle
    I-->>S: Reserved
    S->>P: Authorize payment
    P-->>S: Authorized
    S->>C: Validate compliance
    C-->>S: Approved
    S->>DB: Save order
    DB-->>S: Commit
    S-->>UI: 201 Created
```

Advantages: straightforward response and easier to understand for small
workflows.

Risks: - Latency sums across dependencies. - Any slow dependency
increases end-to-end latency. - Failure handling and compensation become
complex. - Caller must hold a connection/request while workflow runs.

## Option B: Durable asynchronous workflow

``` mermaid
sequenceDiagram
    participant UI as Dealer UI
    participant S as Sales API
    participant DB as Sales DB
    participant O as Outbox Publisher
    participant Q as Message Broker
    participant I as Inventory
    participant P as Payments
    participant C as Compliance

    UI->>S: POST /orders (Idempotency-Key)
    S->>DB: Create Pending order + Outbox event
    DB-->>S: Commit
    S-->>UI: 202 Accepted + OrderId
    O->>Q: Publish OrderRequested
    Q->>I: Reserve inventory
    I-->>Q: InventoryReserved / ReservationFailed
    Q->>P: Authorize payment when valid
    P-->>Q: PaymentAuthorized / PaymentFailed
    Q->>C: Start compliance workflow
    C-->>Q: Compliance result
```

This diagram is illustrative; an actual workflow should have an
orchestrator/state machine or well-defined event choreography, explicit
transition rules and failure/compensation paths. Do not blindly invoke
each next step without checking current order state.

Advantages: - Request returns quickly after durable acceptance. -
Workflow can retry and recover. - Long-running tasks do not occupy HTTP
requests.

Trade-offs: - User sees `Pending` or `Processing`. - Eventual
consistency. - More operational complexity: retries, DLQ, state
transitions, deduplication, reconciliation.

------------------------------------------------------------------------

# 7. Saga: Managing multi-service workflows

Saga coordinates local transactions across services. It does not provide
a single ACID transaction across all service databases.

## Orchestration vs choreography

**Orchestration:** A coordinator owns workflow state and commands the
participants.

Pros: explicit flow, easier central status visibility, clearer
timeout/compensation management.\
Cons: coordinator can become a central logic hub if poorly designed.

**Choreography:** Services react to events and emit further events.

Pros: less central coordination, event-driven autonomy.\
Cons: flow becomes harder to follow as event chains grow; cycles and
hidden dependencies can emerge.

For a business-critical DMS order flow, orchestration may be easier to
reason about because the order has explicit states and compensations.

``` mermaid
stateDiagram-v2
    [*] --> Pending
    Pending --> InventoryReserved
    InventoryReserved --> PaymentAuthorized
    PaymentAuthorized --> CompliancePending
    CompliancePending --> Confirmed
    InventoryReserved --> Failed
    PaymentAuthorized --> CompensationPending: Compliance rejected
    CompensationPending --> PaymentVoided
    PaymentVoided --> InventoryReleased
    InventoryReleased --> Failed
```

## Compensation examples

-   Inventory reserved, payment fails → release inventory reservation.
-   Payment authorized, compliance rejects → void authorization/refund
    according to payment state.
-   Notification fails → retry notification; usually do not cancel the
    order.
-   External DMS export fails → mark integration pending and retry;
    don't necessarily roll back completed business transaction.

Compensation is a new business action, not a time machine. A refund does
not erase the fact that a payment was previously authorized/captured.

## Saga design requirements

-   Durable workflow state.
-   Idempotent command handlers.
-   Correlation IDs and stable operation IDs.
-   Explicit timeout and retry policy per step.
-   Compensation rules per business action.
-   Dead-letter/manual-recovery path.
-   Monitoring for long-running/stuck states.
-   Concurrency protection on state transitions.

------------------------------------------------------------------------

# 8. Transactional Outbox, Inbox and idempotency

## Outbox

Business state update and outbox record commit in one local DB
transaction. A publisher delivers the event asynchronously.

## Inbox/deduplication

Consumer records processed message/operation ID and applies business
change transactionally, where feasible.

## Idempotency

Repeated request/message should not create repeated business effect.

For `POST /orders`: - Client sends an `Idempotency-Key`. - Server
associates key with authenticated tenant/client and operation. - Persist
key, request fingerprint and outcome under a uniqueness constraint. -
Same key + same payload returns prior result. - Same key + different
payload should be rejected according to API contract. - Define retention
and replay policy.

``` mermaid
flowchart TD
    A[Receive command with stable ID] --> B{Already processed?}
    B -- Yes --> C[Return stored outcome / no duplicate effect]
    B -- No --> D[Validate and execute local transaction]
    D --> E[Persist business state + deduplication record]
    E --> F[Commit]
    F --> G[Return outcome]
```

A cache-only idempotency key can be insufficient if the cache expires
before a delayed retry. Durable business-operation deduplication is
often required for high-value transactions.

------------------------------------------------------------------------

# 9. CQRS and read models

CQRS (Command Query Responsibility Segregation) separates write
operations from read models where different requirements justify it.

Example: - Sales service owns order writes. - Reporting read model
consumes order, inventory and compliance events. - Dashboard queries a
denormalized read store rather than joining multiple service databases
live.

``` mermaid
flowchart LR
    C[Commands] --> S[Sales Service]
    S --> DB[(Sales DB)]
    DB --> O[Outbox]
    O --> Q[Events]
    Q --> P[Projection Consumer]
    P --> R[(Reporting Read Model)]
    U[Dealer Dashboard] --> R
```

Benefits: - Read model can be optimized for dashboard queries. -
Cross-service reporting avoids runtime fan-out. - Read scaling can be
independent.

Costs: - Read model may be stale. - Projections need replay/rebuild
strategy. - Event schema and ordering require design. - More storage and
operational complexity.

CQRS does not require Event Sourcing. A conventional relational database
can store current state and publish events through an outbox while
separate read projections serve queries.

------------------------------------------------------------------------

# 10. Event Sourcing: when it is and isn't appropriate

Event Sourcing stores state-changing events as the primary record;
current state is derived by replaying events or reading projections.

Example events: - `VehicleReserved` - `PaymentAuthorized` -
`ComplianceApproved` - `OrderConfirmed`

Potential benefits: - Historical business transitions are explicit. -
Audit/replay scenarios can be powerful. - Different projections can be
rebuilt from the event stream.

Costs: - Event schema evolution and replay compatibility. - Event
ordering and concurrency. - Rebuilding projections and operational
tooling. - PII deletion/retention challenges. - Developers must reason
about event history, not only current rows.

Use it when event history and domain requirements justify the
complexity. Do not introduce Event Sourcing simply because the system
already uses a message broker. For many DMS workflows, relational
current-state tables + transactional outbox + audit history are
sufficient.

------------------------------------------------------------------------

# 11. Caching, rate limiting and backpressure

## Caching

Useful for: - Dealer configuration. - Reference data. - Read-heavy
inventory summaries where staleness is acceptable. - UI metadata and
static reference values.

Design questions: - TTL and invalidation. - Cache key includes
tenant/dealer where relevant. - What happens on cache miss or cache
outage? - Can stale data violate inventory/payment correctness? - Cache
stampede protection. - Sensitive-data retention and access isolation.

Do not use cache as the source of truth for scarce inventory reservation
or payment status unless the consistency design explicitly supports it.

## Rate limiting

Apply limits by suitable identity/dimension: - Client/API key. -
Tenant/dealer. - Endpoint/resource cost. - User or partner integration.

A global limit alone can let one noisy tenant consume all capacity.
Tenant limits need fair sharing and carefully defined burst behaviour.

## Backpressure

When consumers cannot keep up: - Bound queue consumer concurrency. -
Avoid unbounded in-memory queues. - Apply admission control or return
`429`/`503` where appropriate. - Monitor oldest-message age and queue
depth. - Scale consumers only when downstream capacity can handle it. -
Use DLQ for repeatedly failing messages and a controlled replay process.

------------------------------------------------------------------------

# 12. Multi-tenancy and security boundaries

For a dealership platform, tenant/dealer isolation is a core
architectural concern.

## Typical controls

-   Authenticate user/service identity.
-   Resolve dealer/tenant membership from trusted identity/context.
-   Authorize every operation against tenant and resource.
-   Enforce data access at service boundary and, where appropriate,
    database/query layer.
-   Include tenant in cache keys and idempotency scopes.
-   Avoid trusting arbitrary client-supplied `DealerId`.
-   Use least-privilege service identities.
-   Audit sensitive access and cross-tenant administrative actions.

A JWT claim can inform identity/authorization, but token validity alone
does not mean user may access every dealer resource. Validate issuer,
audience, signature, expiry and required claims, then enforce
resource-level authorization.

## Isolation choices

  -------------------------------------------------------------------------
  Model                   Advantages                Trade-offs
  ----------------------- ------------------------- -----------------------
  Shared tables with      Lower cost, simpler       Strong
  `TenantId`              operations                query/authorization
                                                    discipline required

  Separate schema per     Some logical separation   Migration and
  tenant                                            operations complexity

  Separate database per   Stronger                  Higher cost and fleet
  tenant                  isolation/customization   management

  Hybrid                  Tiered isolation for      More complexity and
                          different customers       support burden
  -------------------------------------------------------------------------

Choose based on regulatory needs, tenant count, data volume,
customization and recovery requirements.

------------------------------------------------------------------------

# 13. Capacity estimation

Assume for interview illustration: - 250,000 requests per month during
business hours. - Business hours: 10 AM--7 PM, 9 hours/day. - 22 working
days/month. - Traffic is uneven; peak factor assumed 5× average. - Each
order request may generate several internal calls/events.

Average business-hours request rate:

-   Total seconds in business hours = 22 × 9 × 3600 = 712,800 seconds.
-   Average rate = 250,000 / 712,800 ≈ 0.35 requests/second.
-   At an illustrative 5× peak factor ≈ 1.76 requests/second.

This is a simple average over assumed working days. Actual peak can be
much higher, so use real traffic distribution and load tests. Internal
fan-out and asynchronous events must be estimated separately.

## Little's Law

`Concurrency ≈ Throughput × Average latency`

If throughput = 100 requests/second and average in-flight time = 0.2
seconds, average concurrency ≈ 20 requests.

Use consistent units. This is a steady-state approximation; tail
latency, burstiness and queueing need additional headroom.

## Storage estimate

If 250,000 order records/month and average retained record + indexes +
metadata consumes 4 KB:

-   Monthly raw logical data ≈ 1 GB.
-   12 months ≈ 12 GB before replicas, indexes, logs, backups and
    related event/audit data.

The 4 KB estimate is illustrative. Real storage depends on schema,
indexes, row overhead, retention and transaction logs. For production
sizing, measure representative data.

------------------------------------------------------------------------

# 14. Bottlenecks and failure modes

  -----------------------------------------------------------------------
  Failure                 Signal                  Mitigation
  ----------------------- ----------------------- -----------------------
  Inventory service slow  Inventory span latency  Timeout, bulkhead,
                          increases               capacity investigation;
                                                  preserve reservation
                                                  correctness

  Payment provider outage Authorization           Circuit breaker,
                          failures/timeouts       bounded retries,
                                                  explicit pending/failed
                                                  state

  Queue backlog           Oldest-message age      Inspect consumer
                          rises                   throughput/errors,
                                                  scale safely,
                                                  backpressure

  Database saturation     Connection waits,       Query/connection
                          locks, high latency     tuning, bounded
                                                  concurrency, capacity
                                                  analysis

  Duplicate events        Repeated                Idempotency and durable
                          order/inventory effects deduplication

  Stale reporting         Dashboard lag grows     Monitor projection lag,
                                                  consumer recovery and
                                                  rebuild process

  Tenant hotspot          One dealer drives       Fair-use limits,
                          disproportionate load   tenant-aware metrics,
                                                  partitioning if
                                                  justified

  Bad deployment          Errors rise by release  Halt rollout, safe
                          version                 rollback/roll-forward

  Broker unavailable      Outbox backlog grows    Keep durable outbox,
                                                  alert on age, restore
                                                  publisher/broker path

  Partial dependency      Only some workflows     Graceful degradation
  outage                  fail                    and explicit workflow
                                                  state
  -----------------------------------------------------------------------

------------------------------------------------------------------------

# 15. Full order workflow: design decisions

## API contract

Example:

``` http
POST /api/orders
Authorization: Bearer <token>
Idempotency-Key: 6ddc3d4e-7c1d-4d45-9d2d-123456789abc
Content-Type: application/json
```

``` json
{
  "dealerId": "dealer-42",
  "vehicleId": "vehicle-987",
  "customerReference": "customer-ref-123"
}
```

In a secure design, `dealerId` in the body must be checked against
authenticated user's authorized dealer context. Do not assume a supplied
ID grants access.

Possible response for a durable async workflow:

``` http
HTTP/1.1 202 Accepted
Location: /api/orders/order-123
```

``` json
{
  "orderId": "order-123",
  "status": "Pending",
  "statusUrl": "/api/orders/order-123"
}
```

Use `201 Created` when the API contract considers the resource created
at that point. Use `202 Accepted` when processing is accepted but not
complete; document semantics clearly.

## Workflow state model

``` mermaid
stateDiagram-v2
    [*] --> Pending
    Pending --> InventoryReserved
    Pending --> Failed
    InventoryReserved --> PaymentPending
    PaymentPending --> PaymentAuthorized
    PaymentPending --> Failed
    PaymentAuthorized --> CompliancePending
    CompliancePending --> Confirmed
    CompliancePending --> CompensationPending
    CompensationPending --> Cancelled
    Confirmed --> Completed
```

Each transition should be validated atomically against current
state/version. Handle duplicate/out-of-order events and define terminal
states.

## Consistency decisions

-   Order creation and initial state: local Sales DB transaction.
-   Event publication: transactional outbox.
-   Inventory reservation: Inventory service local transaction with
    idempotency.
-   Payment: Payment service tracks provider operation and idempotency
    key.
-   Compliance: async workflow if it can take time.
-   Dashboard: CQRS/read model with documented staleness.
-   Cross-service failure: Saga with compensation/manual recovery.
-   Audit: immutable or append-only history according to
    business/compliance requirements.

------------------------------------------------------------------------

# 16. How to defend trade-offs in an interview

### "Why not keep the monolith?"

If the system is small, team boundaries are unclear and
scaling/deployment are not limiting factors, a modular monolith may be
simpler. Microservices are justified when independent ownership, release
cadence, scaling, reliability isolation or domain complexity outweigh
distributed-system cost.

### "Why not one database for all services?"

A shared database can be a valid transition step. But permanent
cross-service writes couple schemas and deployments. I would first
enforce ownership and gradually move access behind APIs/events/read
models.

### "Why not use synchronous REST everywhere?"

REST is useful for immediate request/response needs. Long-running
workflows and event-driven integration often benefit from durable async
processing. I would avoid deep synchronous call chains and choose
consistency based on business requirements.

### "Why not Event Source everything?"

Event Sourcing adds schema/replay/concurrency and operational
complexity. Use it where audit/history/replay is central to domain
requirements; otherwise current-state relational storage plus
outbox/audit history may be simpler.

### "Why not Kubernetes?"

Orchestration should match team capability and platform needs.
ECS/Fargate may provide a lower-operations AWS-native path; Kubernetes
makes sense when portability, existing platform investment or ecosystem
requirements justify the added complexity.

------------------------------------------------------------------------

# 17. Interview questions and answers

## Q1. How do you start a system-design interview?

Clarify functional and non-functional requirements, define scope,
estimate load, establish domain boundaries, draw high-level
architecture, then deep dive into data consistency, failure modes,
security, scaling and trade-offs.

## Q2. How many microservices would you create for a DMS?

I would not start with a fixed number. I would identify business
capabilities and data ownership, then create independently deployable
services only where change, scaling, security or team ownership justify
the cost.

## Q3. Why use an API Gateway?

To centralize external routing and edge concerns such as TLS, request
limits, rate limiting and authentication integration. It should not
become the place for all domain orchestration.

## Q4. When would you use a BFF?

When a particular client needs a tailored response or composition layer.
I would keep domain rules in domain services and guard against BFF
fan-out latency and excessive business logic.

## Q5. Saga orchestration or choreography?

For a business-critical order workflow with explicit state and
compensation, orchestration may be easier to reason about. Choreography
works for simpler event reactions, but complex event chains can become
hard to understand.

## Q6. What does the Outbox pattern solve?

It atomically records business state and an event in one local DB
transaction, reducing the risk that the DB commits but the event is
lost. Publishing can still duplicate events, so consumers must be
idempotent.

## Q7. Is exactly-once processing guaranteed by a message broker?

End-to-end business exactly-once effect generally requires
application-level idempotency and transaction design. A broker's
delivery guarantees alone do not make database writes and external side
effects globally atomic.

## Q8. CQRS vs Event Sourcing?

CQRS separates command and query models. Event Sourcing stores
state-changing events as the primary record. They can be used together
but neither requires the other.

## Q9. How do you prevent duplicate orders?

Use an idempotency key scoped to authenticated tenant/client and
operation, persist it with a uniqueness constraint and stable outcome,
and make downstream consumers/operations idempotent too.

## Q10. How do you design for one noisy dealer?

Measure tenant-aware load, apply fair-use/rate limits, bound expensive
operations, and consider workload isolation or partitioning if evidence
supports it. Avoid exposing tenant identifiers as unbounded metric
labels.

## Q11. How do you estimate capacity?

Start with average and peak traffic, payload size, read/write mix,
downstream fan-out, concurrency and data retention. Validate assumptions
with load tests and production telemetry rather than treating rough
estimates as final sizing.

## Q12. When would you choose eventual consistency?

For workflows where immediate cross-service consistency is not a
business requirement, such as reporting projections or notifications.
For inventory reservation/payment correctness, enforce local invariants
and explicit workflow state rather than relying on stale read models.

## Q13. How do you keep a multi-tenant system secure?

Validate identity, authorize resource access on every operation, derive
tenant context from trusted claims/membership, scope queries/cache
keys/idempotency correctly, use least privilege and audit sensitive
cross-tenant access.

## Q14. What happens if the payment provider times out?

Timeout does not necessarily mean payment failed. Track an explicit
unknown/pending state, use provider idempotency and
reconciliation/status lookup where supported, and avoid blindly issuing
a second charge.

## Q15. How do you evolve event schemas safely?

Prefer additive backward-compatible changes, version/document contracts,
deploy consumers that tolerate old/new forms, monitor deserialization
failures, and remove old fields only after consumers are migrated.

## Q16. What is your approach to a stuck order?

Inspect workflow state age, last successful transition, outbox/queue
lag, retries, DLQ, dependency health and correlated traces/logs. Recover
idempotently through a runbook, and use manual intervention only with
audit and state-transition safeguards.

## Q17. When is a modular monolith better?

When the team is small, domain boundaries are still evolving,
independent scaling/deployment is not necessary, and distributed
operations would outweigh benefits. Strong module boundaries can be a
useful intermediate step.

## Q18. How would you migrate without a big-bang rewrite?

Use a Strangler approach: route selected capabilities to new services,
establish data ownership, use compatible schema changes and outbox/CDC
where suitable, validate with shadow reads/reconciliation, and cut over
incrementally.

------------------------------------------------------------------------

# 18. Final architecture checklist

-   [ ] Requirements and assumptions explicitly stated.
-   [ ] Domain boundaries reflect business capabilities.
-   [ ] Every entity has a clear authoritative writer.
-   [ ] Cross-service reads use APIs/events/read models intentionally.
-   [ ] Synchronous call chains have deadlines and bounded concurrency.
-   [ ] Async workflows have durable state, idempotency, retries and
    DLQ.
-   [ ] Outbox is used where DB/event atomicity matters.
-   [ ] Saga compensation and manual recovery are defined.
-   [ ] Tenant authorization is enforced at resource level.
-   [ ] Metrics, logs and traces support end-to-end troubleshooting.
-   [ ] Capacity estimates include peak factors and downstream fan-out.
-   [ ] Deployment, schema evolution and rollback/roll-forward are
    planned.
-   [ ] Operational cost is justified against a modular monolith
    alternative.

------------------------------------------------------------------------

# 19. 90-second system-design answer

"I would start by clarifying DMS order-processing requirements, peak
traffic, latency/availability targets, tenant isolation and consistency
needs. I'd define business capabilities such as Sales, Inventory,
Payments and Compliance, with one authoritative writer per domain. An
API Gateway handles edge policies, while a BFF composes UI-specific data
without owning domain rules. Order creation would persist the initial
order state and an outbox event in one Sales DB transaction, then return
a durable pending response if the workflow is asynchronous. A workflow
orchestrator or carefully bounded choreography coordinates inventory
reservation, payment authorization and compliance, with idempotency,
timeouts, retries and explicit compensation. Each service owns its
database; reporting uses a read model rather than cross-service joins.
I'd instrument logs, RED metrics and distributed traces, enforce
tenant-level authorization, and deploy with compatible schema changes
and canary gates. Finally, I'd validate capacity through peak estimates
and load tests, define SLOs, and prepare recovery procedures for
provider outages, duplicate events, queue backlog and partially
completed orders. I'd choose a modular monolith instead if the team and
operational needs don't justify the distributed-system complexity."

------------------------------------------------------------------------

## Suggested next phase

**Phase 9 --- Advanced Interview Scenarios, Architecture Trade-offs &
Mock Interviews** - Migration strategy decision cases. - Deep-dive
architecture questions. - Trade-off discussions for senior/staff
roles. - .NET/AWS design follow-ups. - Mock interview rounds with model
answers.
