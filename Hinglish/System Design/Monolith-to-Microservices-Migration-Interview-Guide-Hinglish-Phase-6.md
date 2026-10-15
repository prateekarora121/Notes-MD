# Monolith-to-Microservices Migration Interview Guide --- Hinglish

## Phase 6: Data Migration, Database Decomposition & Zero-Downtime Cutover

**Audience:** Senior Software Engineer, Staff Engineer, Tech Lead,
Solution Architect\
**Technology context:** .NET 8, C#, SQL Server, EF Core, AWS,
microservices\
**Style:** Hinglish (Roman Hindi + English technical terminology)

------------------------------------------------------------------------

## Phase ka objective

Monolith ko microservices mein split karte waqt code boundaries banana
comparatively easy ho sakta hai; difficult part hota hai **data
ownership, migration correctness, backward compatibility aur
zero-downtime cutover**.

Is phase ke baad aap explain kar sakenge:

-   Shared database se service-owned databases tak safely kaise move
    karein.
-   Database-per-service ka real meaning aur trade-offs.
-   Expand-and-contract schema migration.
-   Backfill, CDC, dual writes, shadow reads aur reconciliation.
-   SQL Server/EF Core ke saath migration execution.
-   AWS DMS/CDC jaise approaches kab consider karein.
-   Rollback vs roll-forward strategy.
-   Production migration ko measurable checkpoints ke saath kaise
    execute karein.

------------------------------------------------------------------------

# 1. Database decomposition difficult kyun hai?

Legacy monolith mein multiple modules ek hi relational database ko
directly access kar sakte hain. Tables, stored procedures, triggers aur
foreign keys modules ke beech hidden dependencies create karte hain.

``` mermaid
flowchart TD
    UI[Dealer UI] --> M[Legacy Monolith]
    M --> S[Sales module]
    M --> I[Inventory module]
    M --> P[Payment module]
    S --> DB[(Shared SQL Server DB)]
    I --> DB
    P --> DB
    DB --> T1[Orders]
    DB --> T2[Vehicles]
    DB --> T3[Payments]
```

Initial state mein shared database convenient lagta hai:

-   Cross-module joins easy hain.
-   Transactions straightforward hain.
-   Reporting simple ho sakti hai.

Lekin hidden coupling ke issues:

-   Inventory module `Orders` table ke internal schema par depend karta
    hai.
-   Sales code directly `Vehicles` update karta hai.
-   Ek table change multiple modules ko break kar sakta hai.
-   Database deployment aur ownership unclear ho jaati hai.
-   Independent service deployments difficult ho jaate hain.

**Key interview point:** Database split sirf tables ko different
databases mein copy karna nahi hai. Pehle ownership aur write paths
establish karne hote hain.

------------------------------------------------------------------------

# 2. Database-per-service ka matlab

Database-per-service ka principle hai: **har service apne data model aur
writes ki authoritative owner ho.**

Example:

``` mermaid
flowchart LR
    S[Sales Service] --> SDB[(Sales DB)]
    I[Inventory Service] --> IDB[(Inventory DB)]
    P[Payment Service] --> PDB[(Payment DB)]
    S -->|Reserve request / API| I
    S -->|Payment command| P
    S -. OrderCreated event .-> B[Message Broker]
    B --> I
    B --> R[Reporting read model]
```

Iska matlab zaroori nahi ki har service ke liye physical server/cluster
ho. Logical database/schema separation possible hai as an interim step,
but strong isolation tabhi milti hai jab permissions, ownership,
deployment and scaling boundaries bhi enforce hon.

## Benefits

-   Service owns its schema and write rules.
-   Independent deployments and schema evolution.
-   Clearer security and access boundaries.
-   Different storage technology choose karne ki flexibility.
-   Failure and scaling domains better isolate ho sakte hain.

## Costs and trade-offs

-   Cross-service joins simple nahi rehte.
-   Distributed transactions avoid/replace karne pad sakte hain.
-   Eventual consistency and duplicate messages handle karne padte hain.
-   More databases, monitoring, backups and operational work.
-   Reporting ke liye read models/data pipelines chahiye ho sakte hain.

## Interview answer

"Database-per-service ka goal database count badhana nahi, data
ownership enforce karna hai. Main service boundaries aur write ownership
define karunga, direct cross-service writes eliminate karunga, aur
cross-service reads ke liye APIs, events ya purpose-built read models
choose karunga. Migration incremental hogi so that legacy application
compatibility maintain rahe."

------------------------------------------------------------------------

# 3. First step: Discover table ownership and dependencies

Database move karne se pehle discovery karein.

## 3.1 Inventory

Collect karein:

-   Tables, views, stored procedures, functions and triggers.
-   Foreign keys and cross-schema references.
-   SQL Agent jobs, scheduled jobs and ETL pipelines.
-   ORM mappings and raw SQL queries.
-   Reporting/BI access.
-   External applications and integrations.
-   Row counts, table size, write rate and peak transaction volume.
-   Retention, compliance, PII and backup requirements.
-   Existing transaction boundaries and consistency expectations.

## 3.2 Find all writers

Har important entity/table ke liye answer karein:

-   Kaun read karta hai?
-   Kaun insert/update/delete karta hai?
-   Kaunse triggers data modify karte hain?
-   Kya batch job direct updates karti hai?
-   Kya external integration same database access karti hai?
-   Kya application ke bahar manual SQL changes hote hain?

**Migration blocker:** Agar Inventory Service supposedly `Vehicles` own
karti hai, lekin Sales API, nightly job aur reporting process sab us
table ko write karte hain, ownership abhi establish nahi hui.

## 3.3 Build an ownership matrix

  ----------------------------------------------------------------------------
  Data/table     Current        Proposed owner  Cross-service   Migration risk
                 writers                        consumers       
  -------------- -------------- --------------- --------------- --------------
  Orders         Sales API,     Sales           Inventory,      High
                 batch job                      reporting       

  Vehicles       Inventory,     Inventory       Sales,          High
                 Sales                          compliance      

  Payments       Payment module Payments        Sales, finance  High

  Dealer         Multiple       Dealer          All services    Medium
  settings       modules        Configuration                   
  ----------------------------------------------------------------------------

Risk table size alone se determine nahi hota. Write frequency,
transaction coupling, business criticality, hidden consumers and
recovery options bhi matter karte hain.

------------------------------------------------------------------------

# 4. Choose the migration pattern

Har migration same technique se nahi karni chahiye.

  -----------------------------------------------------------------------
  Pattern                 Useful when             Main risk
  ----------------------- ----------------------- -----------------------
  Stop-the-world          Small app, accepted     Downtime
  migration               maintenance window      

  Big-bang cutover        Strong test coverage    High blast radius
                          and coordinated release 
                          possible                

  Strangler/incremental   Large legacy system,    Temporary dual paths
  extraction              continuous delivery     and complexity
                          needed                  

  Expand-and-contract     Old and new application Compatibility gaps
  schema change           versions overlap        

  Backfill + CDC          Large dataset with      Lag and reconciliation
                          ongoing writes          complexity

  Shadow reads            Validate new path       Extra load and
                          before switching user   mismatched results
                          traffic                 

  Blue/green cutover      Infrastructure supports Data compatibility and
                          parallel environments   routing complexity
  -----------------------------------------------------------------------

A strong answer explicitly states the choice depends on traffic, dataset
size, consistency needs, acceptable downtime, team capability and
rollback constraints.

------------------------------------------------------------------------

# 5. Expand-and-contract migration

Expand-and-contract is a compatibility-first schema evolution pattern.

Suppose legacy column `DealerCode` ko new schema mein `DealerId` se
replace karna hai.

## Phase A: Expand

-   New column/table add karein.
-   Existing column remove/rename abhi na karein.
-   Old application ko work karne dein.
-   New code ko old and new schema compatibility ke saath deploy karein,
    where needed.

## Phase B: Migrate data

-   Historical rows backfill karein.
-   New writes ko new representation mein populate karein.
-   Agar old and new columns temporarily coexist karte hain,
    synchronization strategy define karein.
-   Counts, nulls, duplicates and business invariants reconcile karein.

## Phase C: Switch reads

-   New column/table se reads enable karein.
-   Shadow comparison or canary traffic se verify karein.
-   Mismatch rate, latency and errors monitor karein.

## Phase D: Contract

-   Ensure old application versions no longer require old schema.
-   Old reads/writes stop karein.
-   Observation window complete karein.
-   Old column/table remove karein in a separate deployment.

``` mermaid
flowchart TD
    A[Current schema: old column] --> B[Expand: add new column]
    B --> C[Deploy backward-compatible code]
    C --> D[Backfill and synchronize]
    D --> E[Validate and switch reads]
    E --> F[Stop old writes]
    F --> G[Wait for rollback window / verify]
    G --> H[Contract: remove old column]
```

**Important:** Rename/drop operation ko same release mein code
deployment ke saath combine karna risky hai. Old and new application
versions overlap kar sakte hain during rolling deployment.

## Interview answer

"Main schema ko ek atomic big-bang change ke roop mein nahi karunga.
Pehle additive expansion, phir compatible application release, data
migration and validation, read/write cutover, observation window, and
finally destructive cleanup. Contract step tabhi jab old code paths aur
rollback dependency retire ho chuki ho."

------------------------------------------------------------------------

# 6. Backfill: Historical data ko new store mein move karna

Backfill ka matlab existing historical data ko new
database/service-owned store mein populate karna.

## 6.1 Safe backfill principles

1.  **Define a consistent source:** Source-of-truth aur ownership
    clearly define karein.
2.  **Chunk data:** Large single transaction avoid karein.
3.  **Use stable ordering:** Primary key or deterministic cursor.
4.  **Make it restartable:** Last processed key/checkpoint persist
    karein.
5.  **Make writes idempotent:** Same chunk repeat hone par
    duplicates/corruption na ho.
6.  **Throttle:** Production workload ke saath database ko overload na
    karein.
7.  **Capture errors:** Failed records ko track karein; silently skip na
    karein.
8.  **Reconcile:** Counts plus meaningful business invariants compare
    karein.
9.  **Monitor lag:** Backfill progress and remaining work measure
    karein.
10. **Plan recovery:** Restart, retry and rollback procedure test
    karein.

## 6.2 Checkpointed batch algorithm

``` text
checkpoint = lastSuccessfullyProcessedKey

while more source rows exist:
    rows = read next batch where key > checkpoint
           ordered by key
           limited to batchSize

    transform and validate rows
    upsert rows into destination using stable business key

    commit destination batch
    persist checkpoint only after destination commit succeeds

    emit progress metrics
```

Checkpoint ordering important hai. Agar checkpoint destination commit se
pehle advance ho gaya, crash ke baad rows permanently skip ho sakti
hain. Destination commit ke baad checkpoint update ho, aur retry
duplicate-safe ho.

Agar destination commit ho gaya but checkpoint persist nahi hua, same
batch replay ho sakti hai; isi liye idempotent upsert zaroori hai.

## 6.3 SQL Server paging

Large migration ke liye `OFFSET ... FETCH` bahut high offsets par
expensive ho sakta hai. Stable keyset pagination often better hoti hai:

``` sql
SELECT TOP (@BatchSize)
    OrderId,
    DealerId,
    CreatedAt,
    Status
FROM dbo.Orders
WHERE OrderId > @LastOrderId
ORDER BY OrderId;
```

Yeh example tabhi valid hai jab `OrderId` ordering stable and unique ho.
Composite key ke liye cursor condition accordingly define karein.
Concurrent inserts and updates ke liye snapshot/high-watermark/CDC
strategy explicitly decide karein.

## 6.4 Data transformation

Migration ke dauraan:

-   Null/default handling.
-   Time zone and timestamp normalization.
-   Enum/status mapping.
-   Decimal precision/currency.
-   Legacy invalid records.
-   Duplicate business keys.
-   PII/security policy.
-   Referential integrity across newly separated data.

Transformation rules version-control mein rakhein and test
representative legacy records ke against.

------------------------------------------------------------------------

# 7. CDC (Change Data Capture) and change replication

Large dataset ko copy karte waqt source par writes continue ho sakti
hain. Sirf ek initial copy karne se destination stale ho jayega.

Common pattern:

1.  Initial snapshot/backfill start.
2.  Source changes capture karna.
3.  Snapshot data load.
4.  Captured changes ko correct ordering/position ke saath replay.
5.  Lag drain hone dena.
6.  Reconciliation run karna.
7.  Cutover decision lena.

``` mermaid
flowchart LR
    DB[(Legacy SQL Server)] --> S[Snapshot / Initial Backfill]
    DB --> C[CDC / Change Stream]
    S --> N[(New service database)]
    C --> R[Ordered change applier]
    R --> N
    N --> V[Reconciliation and validation]
    V --> X[Cutover gate]
```

## CDC ke important questions

-   Snapshot ka consistent point kaise define hoga?
-   CDC stream mein source position/LSN kaise track hoga?
-   Ordering per entity/partition kaise preserve hogi?
-   Duplicate/replayed changes kaise handle honge?
-   Deletes destination tak kaise pahunchengi?
-   Schema changes CDC pipeline ko kaise affect karengi?
-   Replication lag ka maximum acceptable threshold kya hai?
-   CDC outage/restart ke baad gap kaise detect hoga?

SQL Server CDC, transaction log-based replication and AWS Database
Migration Service (AWS DMS) are possible tools/patterns, but
capabilities depend on source/target engine, version, configuration and
task mode. Current support and limitations verify karein.

**Important distinction:** CDC data replication ka mechanism hai; it
does not automatically define domain ownership or make cross-service
transactions correct.

------------------------------------------------------------------------

# 8. Dual writes: Why they are dangerous

Dual write means same business operation ke andar do independent systems
ko update karna.

Example:

``` csharp
await legacyDb.SaveChangesAsync(cancellationToken);
await newServiceClient.UpdateInventoryAsync(request, cancellationToken);
```

Problem: first write succeed, second fail. Ab systems diverge.

``` mermaid
sequenceDiagram
    participant A as Application
    participant L as Legacy DB
    participant N as New DB

    A->>L: Commit order update
    L-->>A: Success
    A->>N: Update new store
    N-->>A: Timeout
    Note over L,N: Legacy and new data diverge
```

Retries bhi automatically solve nahi karte: second operation ambiguous
ho sakti hai---timeout ka matlab destination ne commit nahi kiya, yeh
guaranteed nahi.

## Better alternatives

-   **Transactional outbox:** Business update and event record same
    local DB transaction mein commit; publisher later sends event.
-   **CDC:** Source database changes capture and replicate.
-   **Single-writer ownership:** Ek store authoritative ho; other store
    derived/read-only replica ho.
-   **Durable workflow/state machine:** Multi-step business process
    explicit states, retries and compensation ke saath.

## Temporary dual-write kab acceptable ho sakta hai?

Only if consciously designed, observable, reconciled, and temporary. You
need:

-   Stable operation/event ID.
-   Idempotent destination writes.
-   Retry and dead-letter handling.
-   Reconciliation reports.
-   Clear source of truth.
-   Divergence alerts.
-   Removal deadline and rollback plan.

**Interview line:** "I avoid naive dual writes because two independent
commits are not atomic. I prefer one authoritative writer plus
outbox/CDC and an idempotent consumer, with reconciliation as an
independent safety net."

------------------------------------------------------------------------

# 9. Transactional Outbox and migration events

A common migration path: legacy transaction commits business data and an
outbox event in the same database transaction. A publisher reads outbox
rows and sends events to a broker; the new service consumes them
idempotently.

``` mermaid
flowchart TD
    A[Legacy application] --> T[Single DB transaction]
    T --> B[(Business tables)]
    T --> O[(Outbox table)]
    O --> P[Outbox publisher]
    P --> Q[Message broker]
    Q --> C[New service consumer]
    C --> D[(New service DB)]
```

Key details:

-   Outbox event and business update commit atomically in the source DB.
-   Publisher may send the same event more than once.
-   Consumer must deduplicate/idempotently apply.
-   Outbox rows need retry, monitoring, retention and cleanup.
-   Poison messages should be isolated and investigated.
-   Event schema compatibility matters during rolling migration.

Outbox improves reliability but does not magically create exactly-once
delivery across the whole distributed system.

------------------------------------------------------------------------

# 10. Shadow reads and comparison

Shadow reads route a copy of read traffic to the new service/store while
user-visible responses continue to come from the legacy path.

``` mermaid
flowchart TD
    U[User request] --> L[Legacy read path]
    L --> R[Return response to user]
    L -. Async shadow request .-> N[New service read path]
    N --> C[Compare normalized results]
    C --> M[Record mismatch metrics]
```

Rules:

-   Shadow path should not perform business side effects.
-   It should be rate-limited so it doesn't overload the new service.
-   Normalize fields where ordering/timestamps are legitimately
    different.
-   Avoid logging sensitive full payloads.
-   Compare meaningful invariants, not just raw JSON equality.
-   Track mismatch rate by endpoint, entity type and release version.

Example comparison fields:

-   Order status.
-   Dealer ownership.
-   Item count and totals.
-   Currency/precision.
-   Compliance flags.
-   Availability state.

Shadow reads validate behaviour, but they do not by themselves prove
write correctness or future load capacity.

------------------------------------------------------------------------

# 11. Reconciliation: Prove the data matches

Reconciliation independent process hai jo legacy and destination data
compare karta hai.

## Levels of reconciliation

1.  **Counts:** Record count by entity/status/date bucket.
2.  **Aggregates:** Totals, sums, min/max timestamps.
3.  **Checksums/hashes:** Useful for identifying candidate mismatched
    ranges; hash canonicalization must be stable.
4.  **Record-level comparison:** Key fields for sampled or all records.
5.  **Business invariants:** No duplicate active vehicle assignment,
    order totals reconcile, payment references valid.
6.  **Relationship checks:** Cross-service references resolve according
    to the new model.

Count equality is necessary in some migrations but not sufficient. One
missing order plus one duplicate order can still yield the same count.

Example report:

  Check                            Legacy   Destination Result
  --------------------------- ----------- ------------- -------------------------------
  Orders in scope               1,250,000     1,250,000 Count matches
  Active orders                    85,000        84,998 Investigate
  Sum of eligible totals               ₹X       ₹X+₹500 Investigate
  Missing dealer references             0             2 Cutover blocker
  CDC lag                             ---     4 seconds Compare with agreed threshold

Numbers are illustrative only.

Every mismatch should have a disposition: fixed, accepted with
documented reason, excluded under approved rule, or blocking cutover.

------------------------------------------------------------------------

# 12. Cutover strategies

## 12.1 Big-bang cutover

-   Stop writes or enter maintenance mode.
-   Drain in-flight transactions.
-   Apply remaining changes.
-   Reconcile and verify invariants.
-   Switch traffic.
-   Monitor and retain rollback capability.

Simple conceptually, but downtime and risk can be high.

## 12.2 Incremental/canary cutover

-   Route a small cohort/percentage to new path.
-   Compare errors, latency and business outcomes.
-   Increase traffic in stages.
-   Keep a clear abort threshold.
-   Ensure old and new versions can coexist with current schema.

Canary selection can be by dealer, region, tenant, endpoint or
percentage, provided routing and data ownership are safe.

## 12.3 Read cutover vs write cutover

Reads can sometimes be shadowed or switched independently. Writes are
more sensitive because two active writers can diverge.

Before write cutover, define:

-   Exact ownership transfer point.
-   How in-flight writes complete.
-   How events/CDC catch up.
-   How stale old instances are prevented from writing.
-   How rollback handles writes already accepted by the new owner.

------------------------------------------------------------------------

# 13. Rollback vs roll-forward

Rollback is not always "switch traffic back." If new service has
accepted writes, legacy store may be stale.

## Rollback questions

-   Can new data be replayed into the old system?
-   Is reverse replication supported and tested?
-   Are schema changes backward-compatible?
-   Can old application versions read new data representation?
-   Are events duplicated if replayed?
-   Could rollback cause lost updates or duplicate payments/orders?

## Safer decision model

-   **Before new writes begin:** Traffic rollback may be
    straightforward.
-   **After new writes begin:** Need a defined
    reverse-sync/reconciliation plan, or roll forward may be safer.
-   **Irreversible business side effect:** Reconcile and compensate
    explicitly; don't assume DB restore undoes an external payment.
-   **Schema contraction completed:** Rollback may require forward-fix
    or a restore/forward migration plan.

A database backup is essential, but backup alone is not a complete
cutover rollback strategy.

------------------------------------------------------------------------

# 14. SQL Server and EF Core migration practices

## 14.1 EF Core migrations

For schema evolution:

-   Review generated migration scripts.
-   Prefer additive changes first.
-   Test against production-like data volume.
-   Identify table rebuilds, long locks, index creation and transaction
    behaviour.
-   Use deployment-controlled migration execution.
-   Avoid every application replica racing to run migrations at startup.
-   Verify permissions and rollback/forward plan.

For production, reviewed SQL scripts or migration bundles can provide
more controlled deployment than unreviewed runtime migration. The right
approach depends on release and operational policy.

## 14.2 Example additive migration

``` csharp
public partial class AddDealerId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "DealerId",
            table: "Orders",
            type: "uniqueidentifier",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DealerId",
            table: "Orders");
    }
}
```

This is an illustrative additive schema change. Production migration
must account for existing rows, indexes, constraints, data backfill and
compatibility. `Down()` is not automatically safe after new application
code has written data that depends on the column.

## 14.3 Avoid expensive backfills inside schema deployment

A migration that adds a column and updates millions of rows in one
deployment transaction can cause long locks, log growth, blocking and
rollback pain.

Better pattern:

1.  Add nullable/new structure.
2.  Deploy compatible code.
3.  Run a separately monitored, checkpointed backfill.
4.  Validate.
5.  Switch reads/writes.
6.  Add constraints or remove old structure later.

## 14.4 SQL Server operational concerns

-   Transaction log growth and backup/log chain implications.
-   Locking/blocking and long-running transactions.
-   Index creation impact.
-   Deadlocks during mixed-version deployments.
-   Replication/CDC lag and retention.
-   Isolation level and snapshot consistency.
-   Query plan changes and missing indexes.
-   Connection pool saturation during migration load.

Use production-like rehearsal and measurable abort thresholds.

------------------------------------------------------------------------

# 15. AWS migration considerations

Possible AWS components include:

-   **AWS Database Migration Service (AWS DMS):** Database migration and
    supported ongoing replication scenarios, subject to
    engine/configuration limitations.
-   **Amazon SQS/SNS/EventBridge:** Application-level event propagation,
    not a replacement for every database replication need.
-   **AWS CloudWatch:** Migration task metrics, application metrics/logs
    and alarms where supported.
-   **AWS Secrets Manager/Parameter Store:** Secure migration
    connection/configuration management.
-   **AWS ECS/Lambda:** Run controlled migration workers or backfill
    jobs, with checkpointing and bounded concurrency.
-   **S3:** Store approved exports, checkpoints or reconciliation
    artifacts when appropriate and access-controlled.

Do not assume a service supports every source/target combination or
every schema feature. Validate DMS engine support, CDC requirements, LOB
handling, constraints, indexes, permissions, networking and
version-specific limitations before committing to the design.

For a backfill worker: - Batch size should be configurable. - Use
bounded concurrency. - Use cancellation tokens and graceful shutdown. -
Persist checkpoint durably. - Emit progress, lag, error and throughput
metrics. - Ensure duplicate-safe destination writes. - Keep credentials
in approved secret storage.

------------------------------------------------------------------------

# 16. Example migration runbook

## Before migration

-   [ ] Define source of truth and ownership boundaries.
-   [ ] Inventory all readers/writers, jobs, triggers and external
    consumers.
-   [ ] Agree acceptable downtime, data loss tolerance and consistency
    model.
-   [ ] Establish baseline latency, throughput and error rates.
-   [ ] Validate schema mapping and transformation rules.
-   [ ] Test backfill, CDC restart and reconciliation on representative
    data.
-   [ ] Define cutover and abort criteria.
-   [ ] Verify backup/restore and recovery procedures.
-   [ ] Prepare dashboards, alerts and named incident owners.
-   [ ] Rehearse the complete runbook.

## During migration

-   [ ] Start initial snapshot/backfill.
-   [ ] Track progress, failures, resource impact and replication lag.
-   [ ] Start/apply captured changes from a consistent source position.
-   [ ] Keep destination writes idempotent.
-   [ ] Run reconciliation continuously or at defined checkpoints.
-   [ ] Pause/slow migration if source production health degrades.
-   [ ] Do not cut over while critical mismatches remain unexplained.

## At cutover

-   [ ] Freeze or coordinate writes if required by design.
-   [ ] Drain in-flight operations.
-   [ ] Bring replication lag within agreed threshold.
-   [ ] Validate critical counts, totals, references and business
    invariants.
-   [ ] Switch a controlled cohort or route.
-   [ ] Verify end-to-end business completion, not only HTTP success.
-   [ ] Monitor error rate, latency, duplicate operations and queue lag.
-   [ ] Keep the old path available only under a clear
    ownership/rollback rule.

## After cutover

-   [ ] Confirm the new service is the only authoritative writer.
-   [ ] Stop obsolete dual writes and old consumers.
-   [ ] Continue reconciliation during the observation window.
-   [ ] Archive migration evidence and document exceptions.
-   [ ] Contract old schema only after compatibility and rollback
    dependencies are retired.
-   [ ] Remove temporary permissions and migration infrastructure.
-   [ ] Run blameless review and capture lessons learned.

------------------------------------------------------------------------

# 17. Interview Questions and Answers

## Q1. Database-per-service ka main benefit kya hai?

Service apne data ka owner hoti hai, so schema and writes independently
evolve kar sakte hain. But cross-service joins, transactions and
reporting more complex ho jaate hain. Main benefit data ownership
hai---not simply separate database servers.

## Q2. Shared database se microservices mein migrate kaise karoge?

Table inventory and writer discovery se start karunga; business
capability ke basis par ownership assign karunga; additive schema and
compatibility introduce karunga; backfill/CDC use karunga where
suitable; reconciliation and shadow validation ke baad controlled
cutover karunga; finally old writes/schema remove karunga.

## Q3. Expand-and-contract kya hai?

Pehle backward-compatible additive changes, then application/data
migration and traffic switch, and finally destructive cleanup. Isse
old/new app versions rolling deployment ke dauraan coexist kar sakte
hain.

## Q4. CDC aur backfill ka difference?

Backfill historical existing data copy karta hai. CDC source ke ongoing
changes capture karta hai. Large live migration mein initial backfill
plus CDC catch-up commonly use ho sakta hai.

## Q5. Dual writes dangerous kyun hain?

Do independent commits atomic nahi hote. Ek write succeed aur doosra
fail ho sakta hai. Better approach single authoritative writer with
outbox/CDC, idempotent consumers and reconciliation.

## Q6. Outbox pattern kya solve karta hai?

Business data aur event record ko same local database transaction mein
commit karke "DB committed but event lost" gap reduce karta hai.
Publisher retries kar sakta hai, so consumers must tolerate duplicate
delivery.

## Q7. Backfill restartable kaise banayenge?

Stable key cursor, durable checkpoint, bounded batches, idempotent
upserts, error tracking and reconciliation. Destination batch commit ke
baad checkpoint advance karunga, so crash/replay safe rahe.

## Q8. Count match hai, kya cutover safe hai?

Not necessarily. Duplicate plus missing records count ko match kar sakte
hain. Critical field comparison, aggregates, relationship integrity and
business invariants bhi validate karunga.

## Q9. Shadow reads ka limitation?

They validate read behaviour but not write correctness. Extra load
create karte hain and only sampled/covered paths validate hote hain.
Shadow path must be side-effect-free and comparison logic meaningful ho.

## Q10. Rollback always possible kyun nahi?

New system may have accepted writes or irreversible side effects. Legacy
store stale ho sakta hai. Rollback requires reverse-sync or a proven
reconciliation/compensation plan; otherwise roll-forward safer ho sakta
hai.

## Q11. Production schema migration kaise safely run karoge?

Reviewed scripts/bundles, backward-compatible steps, controlled
migration runner, production-like rehearsal, lock/log monitoring,
separate backfill, clear abort and recovery plan. App startup par every
replica ko migrations race karne nahi dunga.

## Q12. CDC lag high hai; cutover karna chahiye?

Generally nahi, jab tak agreed consistency requirement satisfy na ho.
First source throughput, consumer throughput, lock/contention, errors
and CDC retention/position investigate karunga. Cutover gate measurable
lag threshold and reconciliation criteria par based hona chahiye.

## Q13. New service ko legacy DB read access de sakte hain?

Temporary transition mein tightly scoped read-only access useful ho
sakta hai, but permanent direct access ownership boundary ko undermine
karta hai. Explicit removal plan, access controls and telemetry honi
chahiye.

## Q14. Eventual consistency business ko kaise explain karoge?

Write ke baad some read models/services short delay ke liye stale ho
sakte hain. User-facing workflow ko pending/processing state,
version/timestamp, retry or refresh semantics ke saath design karna
chahiye. Strong consistency where business invariant requires it,
eventual consistency where acceptable.

## Q15. Migration successful kaise measure karoge?

Technical metrics: lag, errors, latency, data mismatch, duplicate rate.
Business metrics: successful completed orders, payment correctness,
vehicle/compliance invariants and dealer impact. Cutover completion
alone success proof nahi hai.

------------------------------------------------------------------------

# 18. Scenario-based interview answers

## Scenario A: Backfill ke dauraan production SQL Server slow ho gaya

**Response:** 1. Source CPU, IO, locks, log growth, query latency and
application p95 check. 2. Batch size/concurrency reduce or pause
backfill. 3. Long transactions and missing indexes inspect. 4. Confirm
checkpoint/restart safety. 5. Resume gradually under explicit resource
guardrails. 6. Keep business traffic priority above migration
throughput.

## Scenario B: CDC lag keeps increasing

**Response:** - Compare source change rate with destination apply
throughput. - Inspect consumer errors, throttling, locks and destination
constraints. - Check CDC retention/position and whether a gap/reload is
needed. - Scale safely or reduce source load if possible. - Do not cut
over until lag is within agreed gate and reconciliation passes.

## Scenario C: New database has 100 fewer orders but total count nearly matches

**Response:** - Partition comparison by date, status, dealer and source
key ranges. - Check duplicates and filters/defaults in transformation. -
Compare deletes and soft-delete semantics. - Inspect failed
batch/checkpoint logs. - Correct the data, rerun idempotently and
revalidate. - Treat unexplained critical mismatch as cutover blocker.

## Scenario D: Cutover succeeded, then new service begins failing

**Response:** - Assess impact and preserve telemetry. - Determine
whether rollback is safe based on new writes. - If reversible and
rehearsed, switch traffic under runbook. - If new writes have diverged,
use defined reverse-sync/reconciliation or roll forward. - Avoid ad hoc
dual writers during incident. - Validate orders, payments, references
and queue processing after recovery.

------------------------------------------------------------------------

# 19. 60-second interview summary

"Database decomposition mein main pehle table inventory, hidden writers
and business ownership identify karta hoon. Database-per-service ka
objective independent data ownership hai, not simply multiple databases.
Migration ke liye backward-compatible expand-and-contract changes,
checkpointed idempotent backfill, and live-write catch-up ke liye CDC or
outbox-based events choose kar sakta hoon. Naive dual writes avoid karta
hoon because two independent commits atomic nahi hote. Cutover se pehle
shadow reads, record-level reconciliation, business invariants and lag
thresholds validate karta hoon. Rollback plan ko new writes and
irreversible side effects ke context mein design karta hoon. SQL
Server/EF Core migrations ko reviewed, staged and observable rakhta
hoon, aur old schema tabhi contract karta hoon jab compatibility and
recovery dependencies retire ho chuki hon."

------------------------------------------------------------------------

# 20. Final revision checklist

-   [ ] Service data ownership and writer inventory.
-   [ ] Database-per-service benefits and trade-offs.
-   [ ] Expand-and-contract and backward compatibility.
-   [ ] Restartable, checkpointed, idempotent backfill.
-   [ ] CDC snapshot, ordering, lag and retention concerns.
-   [ ] Risks of dual writes and why outbox helps.
-   [ ] Shadow read limitations.
-   [ ] Reconciliation beyond record counts.
-   [ ] Read cutover vs write cutover.
-   [ ] Rollback vs roll-forward decisions.
-   [ ] SQL Server locking, transaction log and EF Core deployment.
-   [ ] AWS DMS applicability and limitations.
-   [ ] A migration runbook with measurable go/no-go criteria.
