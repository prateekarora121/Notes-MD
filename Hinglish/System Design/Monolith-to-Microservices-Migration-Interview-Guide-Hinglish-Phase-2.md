# Monolith to Microservices Migration --- Interview Guide (Hinglish)

> **Phase 2: Database Decomposition, Data Ownership, and Consistency**\
> **Running example:** Automotive Dealership Management System (DMS)\
> **Technology focus:** .NET 8, C#, SQL Server, Entity Framework Core,
> AWS

## Phase 2 ka objective

Phase 1 mein humne migration assessment, business capability
decomposition, DDD aur service boundaries cover kiye. Is phase mein
focus hai ek difficult real-world problem par: **jab monolith ka shared
database multiple microservices mein divide karna ho, toh data
ownership, queries, transactions, migration aur consistency kaise manage
karte hain?**

Important principle: database ko physically split kar dena apne aap mein
successful microservices architecture nahi banata. Har service ke paas
clear data ownership, stable contracts, operational support aur
migration strategy honi chahiye.

------------------------------------------------------------------------

# 1. Shared Database vs. Database per Service

## 1.1 Monolith mein shared database kyun common hota hai?

Traditional monolith mein Inventory, Sales, Customer aur Compliance
modules same SQL Server database ke tables use kar sakte hain. Isse
joins aur ACID transactions straightforward rehte hain.

Example: - `Vehicles` table. - `Sales` table. - `Customers` table. -
`Payments` table. - `ComplianceRecords` table.

Ek sale create karte waqt application ek transaction mein sale record
insert, vehicle status update aur payment record create kar sakti
hai---agar saari writes same database aur compatible transaction
boundary mein hain.

Problem tab aati hai jab modules independently deploy hone wali services
ban jaati hain, lekin woh same tables ko directly read/write karti rehti
hain. Phir schema changes, permissions, releases aur ownership tightly
coupled rehte hain.

## 1.2 Shared database ke variants

"Shared database" ek hi arrangement nahi hota.

1.  **Same database, same tables, uncontrolled access:** Highest
    coupling.
2.  **Same physical database, separate schemas:** Logical boundaries
    improve hoti hain, lekin operational/physical coupling rehti hai.
3.  **Same database, explicit table ownership:** Har table ka ek
    authoritative writer hota hai; other services access APIs/events se
    karti hain.
4.  **Separate database per service:** Service apne schema aur lifecycle
    ko independently manage karti hai.
5.  **Separate database instances/accounts:** Stronger infrastructure
    isolation, lekin cost aur operations bhi zyada.

Transitional migration mein same physical SQL Server instance par
separate databases/schemas acceptable ho sakte hain, provided ownership
clear ho aur services doosre service ki tables ko directly update na
karein.

## 1.3 Diagram: shared database vs. database per service

``` mermaid
flowchart TD
    subgraph Shared[Transitional shared database]
        S1[Sales Service] --> DB[(Shared SQL Server)]
        S2[Inventory Service] --> DB
        S3[Customer Service] --> DB
    end
```

Is arrangement mein agar services same tables ko freely update karti
hain, toh service boundaries mostly deployment boundaries reh jaati
hain. Schema migration ek service ko doosri service ke release ke saath
coordinate karne par majboor kar sakti hai.

``` mermaid
flowchart TD
    SALES[Sales Service] --> SDB[(Sales DB)]
    INV[Inventory Service] --> IDB[(Inventory DB)]
    CUST[Customer Service] --> CDB[(Customer DB)]
    PAY[Payment Service] --> PDB[(Payment DB)]
    SALES -->|API or event contract| INV
    SALES -. consumes event .-> BUS[Message Broker]
    PAY -. publishes event .-> BUS
```

Is design mein Sales Service sale records ka authoritative owner hai;
Inventory vehicle availability ka; Payment payment state ka. Data
duplication ho sakti hai, lekin authoritative write ownership clear
rehti hai.

## 1.4 Comparison table

  -----------------------------------------------------------------------
  Dimension               Shared DB with          Separate DB per service
                          uncontrolled access     
  ----------------------- ----------------------- -----------------------
  Ownership               Ambiguous ho sakti hai  Clear ownership define
                                                  karna easier

  Cross-module joins      Easy                    APIs, events, read
                                                  models ya analytics
                                                  layer chahiye

  Schema changes          Multiple consumers      Mostly service-local,
                          affect ho sakte hain    contracts phir bhi
                                                  compatible hone chahiye

  Local ACID transaction  Cross-module writes     Service boundary ke
                          easy                    bahar distributed
                                                  workflow chahiye

  Independent deployment  Database coupling ki    Better potential, but
                          wajah se limited        contract compatibility
                                                  required

  Fault isolation         Shared database         Better isolation
                          bottleneck common       possible, not automatic

  Operational cost        Lower initially         Higher: backups,
                                                  monitoring, capacity,
                                                  credentials

  Reporting               Direct joins convenient Reporting/read
                                                  model/warehouse may be
                                                  needed

  Security                Broad access ka risk    Least-privilege
                                                  boundaries easier

  Migration effort        Lower initial effort    Data movement and
                                                  consistency planning
                                                  required
  -----------------------------------------------------------------------

### Interview point

**Database per service ka matlab hamesha ek alag physical database
server nahi hota.** Logical database/schema separation bhi ho sakti hai.
Core requirement hai data ownership aur unauthorized cross-service
writes ko prevent karna. Physical isolation business, compliance,
scaling aur operational requirements par depend karti hai.

------------------------------------------------------------------------

# 2. Database per Service Pattern

## 2.1 Is pattern ka meaning

Har service apne business data ki authoritative owner hoti hai. Other
services uski private tables directly query ya update nahi karti.

Example: - Inventory Service owns vehicle availability and inventory
metadata. - Sales Service owns sale lifecycle and agreed sale price. -
Customer Service owns customer profile. - Payment Service owns payment
authorization/capture/refund state. - Compliance Service owns compliance
workflow and evidence.

Service apne database technology ka choice bhi kar sakti hai, lekin
polyglot persistence sirf tab use karein jab real need ho. Har service
ke liye different database choose karna mandatory nahi.

## 2.2 Data ownership ka practical meaning

Ownership ka matlab sirf "ye table is service ke naam par hai" nahi.
Iska matlab: - Service data invariants enforce karti hai. - Sirf service
ki approved code paths authoritative writes karti hain. - Schema
migrations service team own karti hai. - Data retention, access control
aur recovery process defined hai. - Other services supported APIs/events
se data access karti hain. - Contract changes consumers ko break nahi
karte.

## 2.3 Entity duplication kab acceptable hai?

Suppose Sales record ko transaction complete hone ke baad vehicle ka
agreed price, VIN ya customer display name preserve karna hai. Sales
apne sale record mein required historical snapshot store kar sakti hai.
Iska matlab yeh nahi ki Sales Inventory ki current availability own
karti hai.

**Rule of thumb:** authoritative current state ek owner ke paas rakho;
doosre contexts mein required business facts ka snapshot/read model
rakhna valid ho sakta hai.

## 2.4 Direct database access kyun avoid karna chahiye?

Agar Sales Service `InventoryDB.Vehicles` table ko directly query karti
hai: - Inventory schema change Sales ko break kar sakta hai. - Inventory
service apne internal model ko independently evolve nahi kar sakti. -
Access control aur audit difficult ho sakte hain. - Service contract
invisible ho jata hai. - Database availability shared runtime dependency
ban sakti hai.

Agar transitional period mein shared database unavoidable hai, explicit
ownership, separate credentials, permissions, schema boundaries aur
migration roadmap establish karein.

------------------------------------------------------------------------

# 3. Cross-Service Data Access

Ek service ko doosri service ka data chahiye toh one-size-fits-all
solution nahi hota. Data freshness, latency, availability, consistency
aur query shape ke basis par choose karein.

## 3.1 Option A: Synchronous API call

Example: Sales Service vehicle reserve karne ke liye Inventory Service
ki API call karti hai.

``` mermaid
sequenceDiagram
    participant UI as Web UI
    participant S as Sales Service
    participant I as Inventory Service
    participant DB as Inventory DB
    UI->>S: Create sale request
    S->>I: Reserve vehicle
    I->>DB: Check availability and reserve
    DB-->>I: Reservation committed
    I-->>S: Reservation confirmed
    S-->>UI: Sale workflow started
```

**Kab use karein?** - Caller ko immediate response chahiye. - Current
authoritative decision zaroori hai. - Operation ka response user
workflow ko unblock karta hai.

**Trade-offs:** - Sales request Inventory latency aur availability par
depend karti hai. - Timeout ka matlab operation definitely fail hua, yeh
assume nahi kar sakte; reservation commit ho chuki ho sakti hai. -
Retries idempotent operation ke saath design honi chahiye. - Synchronous
chain lambi ho toh latency aur cascading failure risk badhta hai.

**Important:** Vehicle availability ka authoritative decision Inventory
Service kare. Sales locally cached availability ko final authority maan
kar reservation confirm na kare.

## 3.2 Option B: Asynchronous events

Inventory Service `VehicleReserved` event publish karti hai. Sales,
Compliance aur Reporting apne local state ko update kar sakte hain.

``` mermaid
flowchart LR
    INV[Inventory Service] -->|VehicleReserved| BUS[Message Broker]
    BUS --> SALES[Sales Consumer]
    BUS --> COMP[Compliance Consumer]
    BUS --> REPORT[Reporting Consumer]
    SALES --> SDB[(Sales Read / Workflow Data)]
    COMP --> CDB[(Compliance DB)]
    REPORT --> RDB[(Reporting Store)]
```

**Kab use karein?** - Consumers ko change ke baare mein inform karna
hai. - Immediate request-response required nahi. - Multiple downstream
systems independently react kar sakte hain. - Eventual consistency
acceptable hai.

**Trade-offs:** - Consumer update immediately visible na ho. - Duplicate
delivery possible ho sakti hai. - Events out of order aa sakte hain. -
Schema versioning, retries, dead-letter handling aur monitoring required
hain.

Events ko commands ki tarah design na karein. `VehicleReserved` ek fact
hai jo ho chuka; `ReserveVehicle` ek request/command hai.

## 3.3 Option C: Local read model / CQRS

Agar Sales UI ko repeatedly vehicle name, make, model, dealer aur
availability summary display karni hai, har page load par multiple
services call karna expensive ho sakta hai. Sales ya dedicated query
layer events se local read model maintain kar sakti hai.

``` mermaid
flowchart TD
    INV[Inventory Service] -->|VehicleCreated / VehicleUpdated| BUS[Event Broker]
    BUS --> PROJ[Read Model Projector]
    PROJ --> READDB[(Sales Query Store)]
    UI[Sales UI] --> API[Sales Query API]
    API --> READDB
```

Read model queries ke liye optimized hota hai. Lekin uski freshness
source events aur processing delay par depend karti hai. Reservation
jaisa authoritative command phir bhi Inventory Service ko jaana chahiye.

### CQRS kya hai?

Command Query Responsibility Segregation mein writes/commands aur
reads/queries ke models ko separate kiya ja sakta hai. CQRS ka matlab
automatically separate microservices ya event sourcing nahi. Simple
application mein separate read/write models same service aur same
database mein bhi ho sakte hain.

## 3.4 Option D: API composition

API composition mein ek API multiple services se data fetch karke
combined response return karti hai. Example: Dealer dashboard Sales,
Inventory aur Compliance summary combine karta hai.

Use when: - Query limited services ko touch karti hai. - Freshness
important hai. - Aggregation complexity manageable hai.

Avoid when: - Har page par dozens of synchronous calls hain. - Latency
budget exceed hota hai. - Aggregator business logic ka central dumping
ground ban raha hai.

## 3.5 Option E: CDC and reporting store

Change Data Capture (CDC) database changes detect karke downstream
systems ko propagate kar sakta hai. CDC useful ho sakta hai legacy data
migration ya analytics replication ke liye. Lekin CDC ko bina thought ke
service contract ka substitute na banayein: raw database changes domain
semantics express nahi karte aur schema details consumers ke saath
couple kar sakte hain.

Operational reporting ke liye warehouse, lakehouse, reporting database
ya event-fed read model use ho sakta hai. Reporting query ke liye har
request par service databases ko join karna avoid karein.

------------------------------------------------------------------------

# 4. Distributed Transactions and Eventual Consistency

## 4.1 Local ACID vs. distributed workflow

Same SQL Server database mein ek local transaction multiple related rows
atomically update kar sakti hai. Lekin jab Inventory, Sales aur Payment
ke paas separate databases hain, ek normal EF Core transaction in sab
databases ko automatically atomic unit nahi banati.

Example vehicle sale: 1. Inventory vehicle reserve kare. 2. Payment
authorize ho. 3. Sales transaction confirm ho. 4. Inventory sold status
mein transition kare. 5. Compliance workflow start ho.

Agar step 3 fail ho gaya lekin payment authorize ho chuki hai, system ko
recovery behavior define karna padega.

## 4.2 Eventual consistency

Eventual consistency ka matlab hai ki independent services ke
copies/read models kuch time ke liye different ho sakte hain, lekin
successful event processing aur no further changes ki condition mein woh
converge karte hain.

Example: Inventory ne vehicle reserve kar diya hai. Reporting dashboard
ko `VehicleReserved` event 2 seconds baad milta hai. Is interval mein
dashboard old availability show kar sakta hai. Lekin authoritative
reservation decision Inventory Service mein hi hota hai.

Eventual consistency ka matlab "data kabhi bhi wrong ho sakta hai" nahi.
System ko allowed staleness, invariants, recovery aur user-visible
states clearly define karne chahiye.

## 4.3 Saga pattern

Saga ek long-running business transaction ko multiple local transactions
mein divide karti hai. Har successful step apna local commit karta hai.
Agar workflow aage continue nahi kar sakta, previously completed steps
ke liye compensating actions execute kiye ja sakte hain.

Compensation database rollback jaisi exact undo guarantee nahi hoti.
Example, payment authorization release kar sakte hain, lekin already
captured/refunded payment ki external processing time aur rules apply
honge.

### Saga orchestration flow

``` mermaid
sequenceDiagram
    participant O as Sales Saga Orchestrator
    participant I as Inventory Service
    participant P as Payment Service
    participant S as Sales Service
    O->>I: Reserve vehicle
    I-->>O: Reservation confirmed
    O->>P: Authorize payment
    alt Payment authorized
        P-->>O: Authorization successful
        O->>S: Confirm sale
        S-->>O: Sale confirmed
        O-->>O: Saga completed
    else Payment declined
        P-->>O: Authorization declined
        O->>I: Release reservation
        I-->>O: Reservation released
        O-->>O: Saga failed / compensated
    end
```

### Choreography vs. orchestration

**Choreography:** Services events publish/consume karke next step
trigger karti hain. Central coordinator nahi hota. Simple workflows mein
useful, lekin complex workflows mein overall state aur failure path
understand karna harder ho sakta hai.

**Orchestration:** Ek orchestrator workflow state maintain karta hai aur
participants ko commands bhejta hai. Complex business processes aur
explicit recovery logic ke liye clearer ho sakta hai, lekin orchestrator
ko carefully design karna padta hai.

  -----------------------------------------------------------------------
  Dimension               Choreography            Orchestration
  ----------------------- ----------------------- -----------------------
  Coordination            Events ke through       Central workflow
                          decentralized           coordinator

  Simple workflows        Often straightforward   Extra component ho
                                                  sakta hai

  Complex workflows       Event chains hard to    State and transitions
                          follow ho sakti hain    clearer

  Coupling                Event contracts par     Orchestrator
                          coupling                participant contracts
                                                  par depend

  Failure visibility      Distributed correlation Workflow state
                          required                centralized ho sakti
                                                  hai

  Risk                    Hidden event chains     Orchestrator becoming
                                                  overly central
  -----------------------------------------------------------------------

## 4.4 Saga design checklist

Har step ke liye define karein: - Command/input. - Successful outcome. -
Timeout behavior. - Retry policy. - Idempotency key. - Local transaction
boundary. - Event produced. - Compensation action, if possible. -
Terminal failure handling. - Manual intervention path. -
Correlation/trace ID. - Workflow state persistence.

**Critical nuance:** Timeout ke baad blind compensation mat karein.
Payment authorization request ka response timeout hua ho sakta hai jab
provider ne actually authorization succeed kar di ho. Status query,
idempotent retry ya reconciliation required ho sakti hai.

------------------------------------------------------------------------

# 5. Transactional Outbox Pattern

## 5.1 Problem: database write + event publish

Suppose Inventory Service: 1. SQL database mein vehicle reserve karti
hai. 2. Broker par `VehicleReserved` event publish karti hai.

Agar DB commit successful ho, lekin publish se pehle process crash ho
jaye, vehicle reserved hai lekin downstream services ko event nahi
milega.

Reverse order bhi dangerous hai: event publish ho gaya, lekin DB
transaction rollback ho gayi. Consumers ko aisa fact mil jayega jo
source database mein commit hi nahi hua.

## 5.2 Outbox solution

Business data aur outbox event record ko **same local database
transaction** mein save karein. Separate background publisher outbox se
event broker tak publish kare. Publisher crash ke baad same event dobara
publish kar sakta hai, isliye consumers ko idempotent hona chahiye.

``` mermaid
flowchart TD
    CMD[Reserve Vehicle Command] --> APP[Inventory Application Service]
    APP --> TX[Single DB Transaction]
    TX --> VEH[(Vehicle / Reservation Tables)]
    TX --> OUT[(Outbox Table)]
    OUT --> PUB[Background Outbox Publisher]
    PUB --> BUS[Message Broker]
    BUS --> CON[Consumer]
```

Outbox database aur broker ke beech ek distributed ACID transaction
provide nahi karta. Yeh reliable publication ke liye local atomic write
plus retryable delivery design deta hai.

### Simplified SQL schema

``` sql
CREATE TABLE dbo.OutboxMessages
(
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    EventType NVARCHAR(200) NOT NULL,
    Payload NVARCHAR(MAX) NOT NULL,
    OccurredUtc DATETIME2 NOT NULL,
    PublishedUtc DATETIME2 NULL,
    Attempts INT NOT NULL DEFAULT 0
);
```

Production design mein indexing, retention, lease/locking strategy,
poison event handling, schema version, partitioning aur cleanup job
consider karna hoga.

### Simplified C# transactional outline

``` csharp
await using var transaction =
    await dbContext.Database.BeginTransactionAsync(cancellationToken);

reservation.Status = ReservationStatus.Reserved;

dbContext.Reservations.Update(reservation);
dbContext.OutboxMessages.Add(new OutboxMessage
{
    Id = Guid.NewGuid(),
    EventType = "VehicleReserved.v1",
    Payload = JsonSerializer.Serialize(new
    {
        ReservationId = reservation.Id,
        VehicleId = reservation.VehicleId
    }),
    OccurredUtc = DateTime.UtcNow
});

await dbContext.SaveChangesAsync(cancellationToken);
await transaction.CommitAsync(cancellationToken);
```

Yeh illustrative code hai. Entity configuration, serialization contract,
concurrency control, publisher locking, message headers, retries,
idempotency aur UTC clock strategy ko production mein explicitly
implement karna hoga. `PublishedUtc` ko DB commit ke saath atomically
update nahi kiya ja sakta, kyunki broker alag system hai; publish aur
marking ke beech crash hone par duplicate publication possible hai.

------------------------------------------------------------------------

# 6. Inbox, Idempotency, and Duplicate Delivery

## 6.1 Duplicate messages kyun hoti hain?

At-least-once delivery mein broker ya publisher same message dobara
deliver kar sakta hai. Example: consumer business update commit kar deta
hai, lekin acknowledgement se pehle crash ho jata hai. Broker message
redeliver karta hai.

Isliye message handler ko assume karna chahiye ki duplicates possible
hain.

## 6.2 Idempotency

Operation idempotent hai agar same logical request ko multiple times
apply karne se unintended repeated side effects nahi hote.

Example: `ReserveVehicle` command ko `CommandId` ya `IdempotencyKey` ke
saath bheja ja sakta hai. Same key ke repeated requests same logical
outcome return karein, instead of multiple reservations create karna.

## 6.3 Inbox/deduplication pattern

Consumer processed message IDs store karta hai. Message processing aur
processed-ID record ko same local transaction mein persist karna
important hai, jahan possible ho.

``` mermaid
sequenceDiagram
    participant B as Broker
    participant C as Consumer
    participant DB as Consumer DB
    B->>C: Message ID = M123
    C->>DB: Check processed message ID
    alt Already processed
        DB-->>C: Found
        C-->>B: Acknowledge duplicate
    else New message
        C->>DB: Apply business change + record M123
        DB-->>C: Commit
        C-->>B: Acknowledge
    end
```

Agar side effect external payment provider ko call karta hai, local
deduplication alone enough nahi ho sakti. Provider-supported idempotency
keys, durable workflow state aur reconciliation use karein.

------------------------------------------------------------------------

# 7. Data Migration from Monolith to Service-Owned Database

## 7.1 Recommended incremental approach

1.  **Ownership define karein:** Kaunsi service authoritative writer
    hogi?
2.  **Schema and data inventory:** Tables, foreign keys, stored
    procedures, jobs, triggers aur reports identify karein.
3.  **Dependencies remove karein:** Direct access ko supported contract
    se replace karne ka plan banayein.
4.  **Target schema create karein:** New service ke domain model ke
    mutabiq schema define karein.
5.  **Initial backfill:** Historical/current data copy karein.
6.  **Incremental changes:** CDC, outbox events ya controlled
    replication se changes catch up karein.
7.  **Reconciliation:** Counts, totals, critical business invariants aur
    sample records compare karein.
8.  **Cutover:** Clear point par writes new owner ko shift karein.
9.  **Observe:** Error rates, lag, duplicates, stale reads aur business
    metrics monitor karein.
10. **Retire legacy writes:** Old code paths aur permissions remove
    karein after validation.

## 7.2 Dual-write risk

Agar monolith aur microservice dono independently same logical data
update karte hain, partial failure se divergence hota hai.

Example: - Monolith `Vehicle.Status = Sold` update kar deta hai. -
Service DB update fail ho jata hai. - UI ya downstream consumer
conflicting status dekhte hain.

Safer options: - A single authoritative writer during each migration
stage. - Transactional outbox for committed changes. - CDC for
controlled replication where appropriate. - Idempotent consumers. -
Reconciliation and repair process. - A defined cutover boundary.

Dual-write ko sirf `try { writeA(); writeB(); }` se solve nahi kiya ja
sakta.

## 7.3 Zero-downtime migration considerations

Zero downtime ka promise tabhi reasonable hai jab: - Old aur new schema
temporarily compatible hain. - Deployments backward compatible hain. -
Replication lag monitor hota hai. - Cutover criteria defined hain. -
Writes ke liye single-writer/ownership rule clear hai. - Rollback data
semantics ke saath possible hai.

Rollback sirf traffic ko old endpoint par bhejna nahi. Agar new service
ne new authoritative data create kiya hai, toh rollback ke liye data
reconciliation ya forward-fix plan chahiye ho sakta hai.

------------------------------------------------------------------------

# 8. Data Strategy Decision Matrix

  -----------------------------------------------------------------------
  Requirement             Candidate approach      Main trade-off
  ----------------------- ----------------------- -----------------------
  User needs immediate    Synchronous command/API Caller availability and
  authoritative                                   latency dependency
  reservation                                     

  Multiple services need  Integration event       Eventual consistency
  notification                                    and duplicate handling

  UI needs fast combined  Local read model / CQRS Projection lag and
  read                                            operational complexity

  Cross-domain            Reporting store /       Data freshness and
  dashboard/reporting     warehouse               pipeline management

  Legacy DB changes must  CDC, with explicit      Database schema
  be replicated           contract strategy       coupling if used
                                                  carelessly

  Related steps span      Saga                    Compensation and
  service databases                               workflow state
                                                  complexity

  DB write and event must Transactional Outbox    Duplicate publication
  not diverge                                     and publisher
                                                  operations

  Consumer must safely    Idempotency / Inbox     Deduplication storage
  handle redelivery                               and retention
  -----------------------------------------------------------------------

------------------------------------------------------------------------

# 9. Common Database and Consistency Anti-Patterns

## Anti-pattern 1: Shared database with many writers

**Problem:** Multiple services same table update karti hain.\
**Consequence:** Schema and release coupling; ownership unclear.\
**Improvement:** One authoritative writer, explicit contract, gradual
data ownership transition.

## Anti-pattern 2: Cross-service SQL joins for operational APIs

**Problem:** Sales API directly Inventory aur Customer tables join karti
hai.\
**Consequence:** Service autonomy false ho jaati hai.\
**Improvement:** API composition, local read model, or reporting store
based on freshness and latency needs.

## Anti-pattern 3: Every read must call another service

**Problem:** Dashboard render karne ke liye 8 synchronous calls.\
**Consequence:** High latency and cascading failures.\
**Improvement:** Read model, caching where safe, API composition, or
asynchronous projection.

## Anti-pattern 4: Blind dual writes

**Problem:** Two databases ko sequentially update karna without reliable
coordination.\
**Consequence:** Partial failures and divergent state.\
**Improvement:** Single writer, outbox/CDC, controlled migration and
reconciliation.

## Anti-pattern 5: "Exactly once" assumption

**Problem:** Team assume karti hai broker event exactly once process
karega.\
**Consequence:** Duplicate payment/booking or repeated side effects.\
**Improvement:** Idempotent business operation, durable deduplication,
provider idempotency keys and reconciliation.

## Anti-pattern 6: Saga compensation as database rollback

**Problem:** Assume karna ki completed external action perfectly undo ho
sakti hai.\
**Consequence:** Incorrect recovery design.\
**Improvement:** Business-specific compensation, explicit terminal
states and manual remediation path.

------------------------------------------------------------------------

# 10. Phase 2 Interview Questions and Answers

## Q1. Database per Service pattern kya hai?

**30-second answer:** Har microservice apne data ki authoritative owner
hoti hai. Other services private tables directly access nahi karti;
APIs, events ya read models ke through required data use karti hain.

**Detailed Hinglish answer:** Iska purpose sirf databases ko physically
alag karna nahi, data ownership aur schema autonomy establish karna hai.
Inventory vehicle availability own karti hai; Sales sale lifecycle own
karti hai. Agar Sales ko vehicle reserve karna hai, toh Inventory ko
command/API request bhejti hai. Sales Inventory ki table update nahi
karti. Isse coupling reduce hoti hai, lekin cross-service queries aur
consistency ko explicitly design karna padta hai.

**Follow-up:** Kya har service ke liye separate database server
chahiye?\
**Answer:** Nahi. Logical database/schema separation sufficient ho sakti
hai; physical isolation operational, compliance aur performance needs
par depend karti hai.

## Q2. Kya shared database microservices mein allowed hai?

**Answer:** Transitional arrangement mein haan, especially migration ke
early stages mein. Lekin uncontrolled cross-service writes aur schema
coupling long-term autonomy ko undermine karte hain. Clear ownership,
permissions, contracts aur migration plan zaroori hain.

## Q3. Cross-service join kaise karoge?

**Answer:** Query ki freshness, latency aur complexity ke basis par
choose karunga. Small number of services aur fresh data ke liye API
composition; high-volume dashboard ke liye local read model; analytics
ke liye reporting store/warehouse. Service-owned databases ko
operational request mein directly join nahi karunga.

## Q4. Eventual consistency kya hai?

**Answer:** Different services ke views kuch time ke liye differ kar
sakte hain, lekin successful processing ke baad converge karte hain.
System ko staleness expectations aur authoritative source define karna
chahiye. Critical reservation decision source Inventory Service par hona
chahiye.

## Q5. Saga pattern kyun use karte hain?

**Answer:** Jab business workflow multiple services ki local
transactions span karta hai aur single ACID transaction practical nahi,
Saga steps ko coordinate karti hai. Failure par business-specific
compensating actions run hoti hain. Compensation exact rollback
guarantee nahi hoti.

## Q6. Choreography vs. orchestration?

**Answer:** Choreography mein services events se next actions trigger
karti hain. Orchestration mein coordinator workflow state aur next steps
manage karta hai. Simple workflows choreography se manageable ho sakte
hain; complex recovery paths orchestration mein clearer ho sakte hain.

## Q7. Outbox pattern ka problem statement kya hai?

**Answer:** Database commit aur broker publish do independent operations
hain. Crash ke beech mein event lost ya false event published ho sakta
hai. Outbox business data aur event record ko same local transaction
mein save karta hai; publisher baad mein event deliver karta hai.

## Q8. Outbox exactly-once delivery guarantee karta hai?

**Answer:** Nahi. Publisher broker par event bhej sakta hai, phir
published marker update hone se pehle crash ho sakta hai. Event
duplicate publish ho sakta hai. Isliye idempotent
consumers/deduplication chahiye.

## Q9. Inbox pattern kya hai?

**Answer:** Consumer processed message IDs persist karta hai, taaki
redelivery par duplicate business side effect avoid ho. Business update
aur deduplication record ko same local transaction mein commit karna
important hai.

## Q10. Payment request timeout ho toh payment failed maanoge?

**Answer:** Nahi. Timeout ka matlab response nahi mila; provider ne
authorization succeed ki ho sakti hai. Same idempotency key se safe
retry, provider status lookup aur reconciliation use karunga before
compensation.

## Q11. Dual-write problem kya hai?

**Answer:** Do independent stores ko update karte waqt ek success aur
doosra failure ho sakta hai, resulting in divergent state. Single-writer
ownership, outbox/CDC-based propagation, idempotency, reconciliation aur
controlled cutover safer approach hain.

## Q12. CQRS kya microservices ke liye mandatory hai?

**Answer:** Nahi. CQRS reads aur writes ke models ko separate karta hai.
Simple service same model/database use kar sakti hai. CQRS tab useful
hai jab read workload, query shape, scaling ya model needs writes se
significantly different hon.

## Q13. CDC aur domain events mein difference kya hai?

**Answer:** CDC database-level changes capture karta hai;
domain/integration events business meaning express karte hain. CDC
migration/replication ke liye useful ho sakta hai, lekin raw table
changes ko stable business contract samajhna risky hai.

## Q14. Zero-downtime data migration kaise karoge?

**Answer:** Backward-compatible schema, initial backfill, incremental
synchronization, replication lag monitoring, reconciliation, explicit
single-writer cutover aur rollback plan use karunga. Zero downtime
architecture aur operational discipline dono par depend karta hai.

## Q15. Reporting multiple databases se kaise karoge?

**Answer:** Dedicated reporting store, warehouse/lakehouse ya event-fed
read model use karunga. Operational requests mein multiple service
databases ko direct join karne se coupling, latency aur availability
issues aate hain.

------------------------------------------------------------------------

# 11. Phase 2 Quick Revision Checklist

-   [ ] Har service ki authoritative data ownership define hai.
-   [ ] Shared database ke transitional risks samjhe gaye hain.
-   [ ] Direct cross-service table writes avoid kiye gaye hain.
-   [ ] API vs. event vs. read-model choice requirements se justified
    hai.
-   [ ] Eventual consistency aur acceptable staleness documented hain.
-   [ ] Saga steps, retries, timeouts aur compensations defined hain.
-   [ ] Outbox publication duplicates handle kar sakti hai.
-   [ ] Consumers idempotent hain.
-   [ ] External payment side effects ke liye idempotency/reconciliation
    plan hai.
-   [ ] Initial backfill, incremental sync aur reconciliation designed
    hain.
-   [ ] Single-writer cutover aur rollback plan documented hain.
-   [ ] Reporting strategy service ownership ko undermine nahi karti.

## Phase 2 --- Key Interview Message

"Database decomposition is primarily about ownership, not merely
creating separate database servers. I would establish one authoritative
writer per business capability, use synchronous APIs for immediate
authoritative decisions, integration events for asynchronous
propagation, and read models for query-heavy views. For cross-service
workflows, I would use Saga where appropriate, transactional Outbox for
reliable event publication, idempotent consumers for duplicate delivery,
and a controlled backfill/cutover strategy for migration."
