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

| Dimension | Shared DB with uncontrolled access | Separate DB per service |
|---|---|---|
| Ownership | Ambiguous ho sakti hai | Clear ownership define karna easier |
| Cross-module joins | Easy | APIs, events, read models ya analytics layer chahiye |
| Schema changes | Multiple consumers affect ho sakte hain | Mostly service-local, contracts phir bhi compatible hone chahiye |
| Local ACID transaction | Cross-module writes easy | Service boundary ke bahar distributed workflow chahiye |
| Independent deployment | Database coupling ki wajah se limited | Better potential, but contract compatibility required |
| Fault isolation | Shared database bottleneck common | Better isolation possible, not automatic |
| Operational cost | Lower initially | Higher: backups, monitoring, capacity, credentials |
| Reporting | Direct joins convenient | Reporting/read model/warehouse may be needed |
| Security | Broad access ka risk | Least-privilege boundaries easier |
| Migration effort | Lower initial effort | Data movement and consistency planning required |

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

Suppose Sales Service vehicle select karne ke liye
`InventoryDB.Vehicles` table ko directly query karti hai. Yeh convenient
lag sakta hai, lekin Sales ab Inventory Service ke supported contract
ke bajay uske storage design par depend kar rahi hai. Isse kuch problems
paida hoti hain:

- **Schema coupling:** Inventory team column rename kare, table split
  kare, data normalize kare ya index/query behavior badle, toh Sales
  query fail ya unexpectedly slow ho sakti hai. Database migration ab
  ek service ka internal change nahi rehta; har direct consumer ko
  coordinate karna padta hai.
- **Independent deployment ka loss:** Inventory apna schema safely
  evolve nahi kar sakti jab tak Sales aur doosre consumers update na
  hon. Teams ke releases aur rollback ek doosre se bandhne lagte hain.
- **Business rules bypass hona:** Database se rows padhne par Sales
  Inventory ke validation, authorization, availability rules ya
  reservation workflow ko skip kar sakti hai. Table ka data padhna
  supported business operation perform karne ke barabar nahi hota.
- **Hidden aur unstable contract:** SQL query, table names, joins aur
  assumed meanings code mein chhupe hue contracts ban jaate hain.
  Inventory team ko in consumers ka pata na ho, toh change ka impact
  assess karna mushkil hota hai.
- **Security aur audit ka risk:** Shared credentials ya broad read
  permissions least-privilege access ko mushkil bana sakte hain.
  Database audit se query dikh sakti hai, lekin business intent (jaise
  kis sale ke liye vehicle check hua) trace karna aasaan nahi hota.
- **Availability aur load coupling:** Sales ka database connection,
  connection pool, query load aur database outage Inventory ke saath
  share hota hai. Sales ki heavy query Inventory workload ko impact kar
  sakti hai, aur database issue dono services ko affect kar sakta hai.
- **Data ownership unclear hona:** Agar Sales direct writes bhi karti
  hai, toh Inventory apne invariants aur authoritative state ko reliably
  enforce nahi kar pati. Direct reads bhi internal schema ko consumers
  ke liye de facto public API bana sakti hain.

Iske bajay Sales ko Inventory ke supported API ya event se wahi business
operation/data lena chahiye jo usse chahiye. Misal ke liye, current
availability ya vehicle reserve karna Inventory API se poochhein;
Inventory apne rules apply karke stable response de. Agar Sales ko
historical sale ke liye vehicle ka naam ya agreed price preserve karna
hai, toh required values ka snapshot Sales ke apne record mein rakh
sakti hai. Isse current availability ka ownership Inventory ke paas
hi rehta hai.

Agar transitional period mein shared database unavoidable hai, explicit
ownership aur exit plan define karein:

- Har table/schema ka authoritative owner document karein; doosri
  services ke writes block karke sirf required read permissions dein.
- Har service ke liye alag database identity/credentials rakhein; shared
  admin ya application credentials use na karein.
- Schema boundaries aur migration process define karein. Breaking
  changes ke liye backward-compatible rollout rakhein: pehle additive
  change, phir consumers migrate, aur aakhir mein purana schema remove.
- Direct queries, owners, consumers aur data freshness expectations
  document karein; critical access ke liye monitoring aur audit add
  karein.
- Shared DB ko permanent service contract na banne dein. API/event ya
  owned read-model par move karne ka roadmap aur target date rakhein.

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

| Dimension | Choreography | Orchestration |
|---|---|---|
| Coordination | Events ke through decentralized | Central workflow coordinator |
| Simple workflows | Often straightforward | Extra component ho sakta hai |
| Complex workflows | Event chains hard to follow ho sakti hain | State and transitions clearer |
| Coupling | Event contracts par coupling | Orchestrator participant contracts par depend |
| Failure visibility | Distributed correlation required | Workflow state centralized ho sakti hai |
| Risk | Hidden event chains | Orchestrator becoming overly central |

## 4.4 Saga design checklist

Har saga step ke liye yeh details define karein:

- **Command/input:** Step ko kaunsa request ya data milega?
- **Successful outcome:** Success ko kaise identify aur persist karenge?
- **Timeout behavior:** Response na aaye toh status unknown hai ya failed;
  next action kya hoga?
- **Retry policy:** Kya retry hoga, kitni baar, kis backoff ke saath, aur
  kaunse errors retryable hain?
- **Idempotency key:** Duplicate command/retry se wahi operation dobara
  apply toh nahi hoga?
- **Local transaction boundary:** Step ke kaunse state changes ek atomic
  local transaction mein commit honge?
- **Event produced:** Success ya failure par kaunsa event publish hoga,
  aur uski delivery kaise reliable hogi?
- **Compensation action:** Step ke baad saga fail ho toh business effect
  ko kaise reverse ya offset karenge? Har action reversible nahi hota.
- **Terminal failure handling:** Retries khatam ya compensation fail ho
  toh workflow ki final state kya hogi?
- **Manual intervention path:** Operator ko kya context, action aur
  recovery instructions milengi?
- **Correlation/trace ID:** Is step ko poore workflow aur logs/traces
  mein kaise trace karenge?
- **Workflow state persistence:** Saga ka current step, attempts aur
  outcome process restart ke baad kaise recover honge?

**Critical nuance:** Timeout ke baad blind compensation mat karein.
Payment authorization request ka response timeout hua ho sakta hai jab
provider ne actually authorization succeed kar di ho. Status query,
idempotent retry ya reconciliation required ho sakti hai.

------------------------------------------------------------------------

# 5. Transactional Outbox Pattern

## 5.1 Problem: database write + event publish

Short answer: **SQL database update aur broker par event publish aam
taur par ek hi atomic transaction mein nahi hote.** Database aur message
broker alag systems hain; dono ke apne transaction managers hote hain.
Database transaction commit hone ke baad broker publish fail ho sakta
hai, aur broker publish hone ke baad database rollback ho sakta hai.

Suppose Inventory Service vehicle reserve karke `VehicleReserved` event
publish karna chahti hai:

Agar DB commit successful ho, lekin publish se pehle process crash ho
jaye, vehicle reserved hai lekin downstream services ko event nahi
milega.

Reverse order bhi dangerous hai: event publish ho gaya, lekin DB
transaction rollback ho gayi. Consumers ko aisa fact mil jayega jo
source database mein commit hi nahi hua.

## 5.2 Outbox solution

Outbox mein **event ko broker par same transaction mein publish nahi
karte**. Uske badle business data aur event ka record (outbox row) ek hi
database mein **same local transaction** ke andar save hota hai:

1. Database transaction shuru karein.
2. Reservation/business tables update karein.
3. Outbox table mein `VehicleReserved` event ka type aur payload insert
   karein.
4. Transaction commit karein. Dono writes commit hongi, ya dono rollback
   hongi.

Alag background publisher committed outbox rows padhta hai aur broker
par event bhejta hai. Publish ke baad row ko published mark karta hai.
Is tarah application crash hone par committed event outbox mein rehta
hai aur publisher baad mein retry kar sakta hai. Publish aur
`PublishedUtc` update ke beech crash hua toh event dobara publish ho
sakta hai; isliye consumer ko idempotent/deduplicate karna chahiye.

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

Outbox database aur broker ke beech distributed ACID transaction ya
exactly-once delivery provide nahi karta. Yeh database mein state aur
event-intent ko atomically save karta hai, phir retryable, aam taur par
at-least-once delivery deta hai. Isliye duplicate-safe consumer zaroori
hai.

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

-- Publisher sirf unpublished rows poll karta hai; index ke bina table
-- grow hone par har poll full scan karega.
CREATE INDEX IX_OutboxMessages_Unpublished
    ON dbo.OutboxMessages (OccurredUtc)
    WHERE PublishedUtc IS NULL;
```

Production mein in concerns ko explicitly design karein:

- **Indexing:** Publisher ko pending rows baar-baar scan na karni
  padein. Status aur ordering/filter ke hisaab se index banayein, jaise
  unpublished rows ko `OccurredUtc` ke order mein fetch karna. Index
  actual polling query se match hona chahiye; extra indexes har insert
  ko mehnga karte hain.
- **Retention:** Decide karein ki successfully published event records
  kitne din rakhne hain. Debugging, replay, compliance aur recovery ki
  zaroorat retention period ko affect karti hai. Failed/unpublished
  events ko normal published-event policy ke tahat delete na karein.
- **Lease/locking strategy:** Agar multiple publisher instances chal
  rahe hon, toh rows ko atomically claim karein, taaki do publishers ek
  hi row ko concurrently process na karein. Lease mein owner aur expiry
  rakhein; publisher crash ho toh expiry ke baad doosra worker row claim
  kar sake. Claim chhote batches mein karein aur broker call ke dauran
  lamba database transaction/lock hold na karein.
  **Example:** `Worker-A` event `E123` ko claim karta hai. Row par
  `LockedBy = Worker-A` aur `LockExpiresUtc = 10:05` set hota hai.
  `Worker-B` us waqt tak is unexpired lease wali row ko skip karta hai.
  Agar `Worker-A` crash ho jaye aur 10:05 tak lease renew na kare, toh
  10:05 ke baad `Worker-B` atomically owner/expiry update karke event
  claim aur publish kar sakta hai. Successful publish ke baad worker row
  ko published mark kare, aur update `LockedBy` se match bhi kare, taaki
  purana worker kisi naye owner ki claim ko overwrite na kar de. Lease
  publish ke expected time se lambi rakhein ya long publish par renew
  karein. Lease duplicate publish ko poori tarah nahi rokta: broker ne
  event accept kar liya ho par worker ko acknowledgement na mila ho, toh
  retry duplicate bhej sakta hai; consumer idempotency phir bhi zaroori
  hai.
- **Poison event handling:** Jo event repeated attempts ke baad bhi
  publish nahi hota, woh poison event ho sakta hai (invalid payload,
  incompatible schema ya permanent broker rejection). Attempts/error
  record karein, alert dein, aur event ko inspectable failed/dead-letter
  state mein rakhein. Root cause fix aur controlled replay/manual
  remediation ke bina usse silently drop na karein.
- **Schema version:** Event ke saath explicit event type aur schema
  version store karein, jaise `VehicleReserved.v1`. Consumers ko
  backward-compatible changes support karne dein; incompatible change
  ke liye naya version aur migration plan rakhein. Sirf current code se
  purane outbox payloads ko deserialize karna assume na karein.
- **Partitioning:** Bahut high volume par, outbox ko time ya workload ke
  hisaab se partition karna scans aur purane data removal ko manageable
  bana sakta hai. Partition key, indexes aur primary/unique keys ko
  database ki partitioning restrictions ke saath design karein; chhote
  outbox ke liye partitioning unnecessary complexity ho sakti hai.
- **Cleanup job:** Published aur retention period se purane rows ko
  scheduled job se chhote batches mein delete/archive karein, taaki
  transaction log, locks aur database load control mein rahen. Active
  leases, unpublished/failed rows aur replay ke liye required records ko
  protect karein; cleanup metrics aur failures monitor karein.

### Simplified C# transactional outline

``` csharp
// Agar EF Core retrying execution strategy enabled hai (jaise
// EnableRetryOnFailure), to user-initiated transaction ko execution
// strategy ke andar wrap karna zaroori hai; warna runtime exception aata hai.
var strategy = dbContext.Database.CreateExecutionStrategy();

await strategy.ExecuteAsync(async () =>
{
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
});
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

| Requirement | Candidate approach | Main trade-off |
|---|---|---|
| User needs immediate authoritative reservation | Synchronous command/API | Caller availability and latency dependency |
| Multiple services need notification | Integration event | Eventual consistency and duplicate handling |
| UI needs fast combined read | Local read model / CQRS | Projection lag and operational complexity |
| Cross-domain dashboard/reporting | Reporting store / warehouse | Data freshness and pipeline management |
| Legacy DB changes must be replicated | CDC, with explicit contract strategy | Database schema coupling if used carelessly |
| Related steps span service databases | Saga | Compensation and workflow state complexity |
| DB write and event must not diverge | Transactional Outbox | Duplicate publication and publisher operations |
| Consumer must safely handle redelivery | Idempotency / Inbox | Deduplication storage and retention |

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
