# Monolith-to-Microservices Migration

## Master Interview Preparation Guide --- Hinglish

**Audience:** Senior Software Engineer, Staff Engineer, Tech Lead, and
Solution Architect interviews\
**Technology focus:** .NET 8, C#, ASP.NET Core, SQL Server, EF Core,
AWS, SQS/SNS, ECS/Fargate, CI/CD\
**Style:** Roman Hindi + English technical terminology

This master guide consolidates all eleven phases into one searchable
Markdown document. The original phase content is retained below in
sequence. Use the table of contents to navigate, or search for concepts
such as **Strangler Fig**, **Outbox**, **Saga**, **CDC**,
**OpenTelemetry**, **canary deployment**, and **idempotency**.

## Table of Contents

1.  [Phase 1 --- Foundations, Assessment, and Decomposition](#phase-1)
    --- source file:
    `Monolith-to-Microservices-Migration-Interview-Guide-Hinglish-Phase-1.md`

2.  [Phase 2 --- Data Ownership and Distributed Transactions](#phase-2)
    --- source file:
    `Monolith-to-Microservices-Migration-Interview-Guide-Hinglish-Phase-2.md`

3.  [Phase 3 --- Service Communication and Messaging](#phase-3) ---
    source file:
    `Monolith-to-Microservices-Migration-Interview-Guide-Hinglish-Phase-3.md`

4.  [Phase 4 --- API Gateway, UI Integration, and Security](#phase-4)
    --- source file:
    `Monolith-to-Microservices-Migration-Interview-Guide-Hinglish-Phase-4.md`

5.  [Phase 5 --- Observability, Reliability, and Operations](#phase-5)
    --- source file:
    `Monolith-to-Microservices-Migration-Interview-Guide-Hinglish-Phase-5.md`

6.  [Phase 6 --- Data Migration and Cutover](#phase-6) --- source file:
    `Monolith-to-Microservices-Migration-Interview-Guide-Hinglish-Phase-6.md`

7.  [Phase 7 --- Deployment, CI/CD, and Infrastructure](#phase-7) ---
    source file:
    `Monolith-to-Microservices-Migration-Interview-Guide-Hinglish-Phase-7.md`

8.  [Phase 8 --- System Design and Architecture](#phase-8) --- source
    file:
    `Monolith-to-Microservices-Migration-Interview-Guide-Hinglish-Phase-8.md`

9.  [Phase 9 --- Architecture Decisions and Mock Interviews](#phase-9)
    --- source file:
    `Monolith-to-Microservices-Migration-Interview-Guide-Hinglish-Phase-9.md`

10. [Phase 10 --- Testing Strategy and Migration Verification](#phase-10)
    --- source file:
    `Monolith-to-Microservices-Migration-Interview-Guide-Hinglish-Phase-10.md`

11. [Phase 11 --- Resume Deep-Dive](#phase-11) --- source file:
    `Monolith-to-Microservices-Migration-Interview-Guide-Hinglish-Phase-11.md`

## How to use this guide

1.  **First pass:** Read the foundations and architecture sections to
    build a coherent migration story.
2.  **Second pass:** Study data ownership, communication, security,
    observability, and deployment trade-offs.
3.  **Practice:** Answer the scenario-based questions aloud, using the
    DMS examples where relevant.
4.  **Interview framing:** State the business problem, explain options
    and trade-offs, describe failure handling, then cover observability
    and rollback.
5.  **Important:** Treat code and architecture snippets as
    interview-oriented examples. Validate production details such as
    package versions, security configuration, retry policies, and type
    consistency before reusing them in a real system.

------------------------------------------------------------------------

<a id="phase-1"></a>

# Phase 1 --- Foundations, Assessment, and Service Decomposition

## 1. Monolith se Microservices tak: Big Picture

### 1.1 Monolithic application kya hoti hai?

Monolith mein application ke multiple business modules ek deployable
application ke andar hote hain. Example: DMS application mein Inventory,
Sales, Customer, Payment aur Compliance modules ek hi ASP.NET Core
application aur aksar ek shared SQL Server database use karte hain.

Monolith ka matlab automatically bad architecture nahi hota. Agar
modules well-separated hain, deployment simple hai, aur team/product
scale manageable hai, toh modular monolith excellent choice ho sakta
hai.

### 1.2 Modular monolith

Modular monolith ek single deployable application hoti hai, lekin
codebase clear business modules mein divided hota hai. Modules ke public
interfaces defined hote hain aur ek module doosre module ki internal
implementation ko directly access nahi karta.

**Benefit:** Microservices ki tarah network, distributed tracing aur
cross-service consistency ka operational burden nahi aata, lekin
code-level boundaries improve hoti hain.

### 1.3 Distributed monolith

Distributed monolith technically multiple services hoti hain, lekin
services tightly coupled hoti hain. Example: Sales deploy karne ke liye
Inventory aur Customer services ko bhi deploy karna padta hai; har
request 5 services ko synchronously call karti hai; services same tables
ko update karti hain.

Is architecture mein microservices ka operational overhead aa jata hai,
lekin independent deployment aur autonomy ka benefit nahi milta.

### 1.4 Microservices architecture

Microservices architecture mein application business capabilities ke
around chhoti, independently deployable services mein organized hoti
hai. Har service ek clearly defined responsibility own karti hai, apne
data ke liye authoritative hoti hai, aur APIs/events ke through doosri
services se interact karti hai.

Microservice ka size lines of code se define nahi hota. Boundary
business responsibility, data ownership, team ownership, change
frequency aur operational needs se define hoti hai.

### 1.5 Architecture comparison

| Dimension | Monolith | Modular Monolith | Microservices |
|---|---|---|---|
| Deployment | One deployable unit | One deployable unit | Multiple independent deployables |
| Code boundaries | Weak ya strong ho sakti hain | Explicit modules | Service boundaries |
| Data | Usually shared | Often shared, module-owned | Ideally service-owned |
| Network failures | Mostly in-process | Mostly in-process | First-class concern |
| Transactions | Easier local ACID transactions | Easier local ACID transactions | Cross-service consistency is harder |
| Scaling | Often whole app | Often whole app | Per-service scaling possible |
| Operations | Simpler | Simpler-to-moderate | More complex |
| Team autonomy | Limited by coupling | Better code ownership | Strong when boundaries are good |
| Best fit | Small/medium systems | Many systems needing modularity | Systems needing independent scale/deployment/ownership |

### 1.6 Diagram: Monolith vs. target architecture

Monolith ka simple structure:

``` mermaid
flowchart TD
    UI[Web UI] --> APP[ASP.NET Core Monolith]
    APP --> INV[Inventory Module]
    APP --> SALES[Sales Module]
    APP --> CUST[Customer Module]
    APP --> PAY[Payment Module]
    APP --> COMP[Compliance Module]
    INV --> DB[(Shared SQL Server)]
    SALES --> DB
    CUST --> DB
    PAY --> DB
    COMP --> DB
    APP --> EXT[External DMS / Payment Systems]
```

Target microservices architecture ka high-level view:

``` mermaid
flowchart TD
    UI[Web / Mobile UI] --> GW[API Gateway or BFF]
    GW --> INV[Inventory Service]
    GW --> SALES[Sales Service]
    GW --> CUST[Customer Service]
    GW --> PAY[Payment Service]
    GW --> COMP[Compliance Service]
    INV --> DB1[(Inventory DB)]
    SALES --> DB2[(Sales DB)]
    CUST --> DB3[(Customer DB)]
    PAY --> DB4[(Payment DB)]
    COMP --> DB5[(Compliance DB)]
    INV -. Events .-> BUS[Event Broker]
    SALES -. Events .-> BUS
    PAY -. Events .-> BUS
    BUS -. Subscriptions .-> COMP
    BUS -. Subscriptions .-> REPORT[Reporting / Read Model]
    GW --> OBS[Logs, Metrics, Traces]
    INV --> OBS
    SALES --> OBS
    PAY --> OBS
```

**Diagram ka meaning:** UI generally gateway/BFF ke through backend tak
pahunchti hai. Services apna data own karti hain. Events ke through
downstream systems ko changes bataye ja sakte hain. Observability
cross-service request ko diagnose karne mein help karti hai.

### 1.7 Microservices kab choose karein?

Microservices consider karein jab measurable problems hon, jaise:

-   Ek module ko doosre modules se kaafi zyada scale karna padta ho.
-   Har release mein unrelated modules ka regression risk ho.
-   Teams independently release karna chahti hon.
-   Business capabilities ke clear boundaries aur ownership hon.
-   Different capabilities ki reliability, security ya deployment
    requirements materially different hon.

Microservices sirf resume trend, "modern architecture" label, ya
application ko technically fancy banane ke liye adopt nahi karni
chahiye.

**Modular monolith prefer karein** agar team chhoti hai, domain
boundaries unclear hain, independent scaling ki need nahi hai, ya
deployment/monitoring automation abhi mature nahi hai.

### Interview-ready answer: "Why migrate a monolith to microservices?"

"Main pehle migration ka business reason validate karunga. Agar problem
code organization ki hai, toh modular monolith enough ho sakta hai. Agar
independent deployment, per-capability scaling, fault isolation ya team
autonomy ki real need hai, toh main business capabilities aur DDD
bounded contexts ke basis par boundaries identify karunga. Phir
Strangler Fig approach se ek low-risk but valuable capability extract
karunga, uska data ownership establish karunga, observability aur CI/CD
validate karunga, aur measured results ke basis par next services
migrate karunga. Main big-bang rewrite avoid karunga."

------------------------------------------------------------------------

## 2. Migration Part A --- Assessment and Planning

Migration ka pehla step code ko services mein split karna nahi hai.
Pehla step system aur business constraints ko samajhna hai.

### 2.1 Assessment checklist

1.  **Business pain points:** Releases slow kyun hain? Kaunse workflows
    frequently change hote hain?
2.  **Runtime profile:** Kaunse endpoints slow hain? Kaunse modules high
    CPU, memory ya database load generate karte hain?
3.  **Dependency map:** Kaunse modules doosre modules ko call karte
    hain? Kya circular dependencies hain?
4.  **Data coupling:** Kya modules doosre modules ki tables directly
    read/write karte hain?
5.  **Release coupling:** Kya ek small change ke liye full application
    deploy karni padti hai?
6.  **Team ownership:** Kaunse teams kaunse business capabilities own
    karti hain?
7.  **Reliability:** Kya ek module ki failure poore application ko
    affect karti hai?
8.  **Security/compliance:** Kya kuch data ya workflows ke liye distinct
    access controls chahiye?
9.  **Operational maturity:** Kya automated tests, CI/CD, metrics, logs,
    tracing aur rollback available hain?
10. **Success metrics:** Migration se kya improve hona chahiye aur kaise
    measure hoga?

### 2.2 Baseline metrics

Migration se pehle baseline record karein:

-   Deployment frequency.
-   Lead time for change.
-   Change failure rate.
-   Mean time to recovery (MTTR).
-   p50, p95 aur p99 latency.
-   Error rate.
-   Throughput.
-   Infrastructure cost.
-   Team release coordination time.

Agar migration ke baad deployment independence improve hui lekin total
operational cost bahut badh gaya, toh us trade-off ko quantify karna
chahiye.

### 2.3 Migration strategy flowchart

``` mermaid
flowchart TD
    A[Assess business and technical pain] --> B{Are independent deployment or scaling benefits needed?}
    B -- No --> C[Improve or modularize monolith]
    B -- Yes --> D[Map dependencies and business capabilities]
    D --> E{Are boundaries understood and testable?}
    E -- No --> F[Modularize, add tests, observe dependencies]
    F --> D
    E -- Yes --> G[Choose pilot capability]
    G --> H[Define service contract and data ownership]
    H --> I[Extract incrementally with routing and rollback]
    I --> J[Measure outcomes]
    J --> K{Benefits demonstrated?}
    K -- No --> L[Adjust boundary or stop further extraction]
    K -- Yes --> M[Repeat for next capability]
```

### 2.4 Pilot service kaise choose karein?

Pilot aisa select karein jahan business value ho, lekin blast radius
manageable ho.

Evaluate: - Boundary reasonably clear hai? - Dependencies limited
hain? - Data ownership establish kar sakte hain? - Tests likh sakte
hain? - Rollback possible hai? - Business team available hai? - Success
measurable hai?

DMS mein Dealer Configuration ya a relatively isolated reporting/query
capability pilot ho sakti hai, lekin final choice actual dependency
analysis par depend karegi. Sales/Payment jaise high-risk workflows ko
first extraction banana zaroori nahi.

### 2.5 Deliverables before extraction

-   Current-state architecture diagram.
-   Module dependency map.
-   Initial bounded-context hypothesis.
-   Data access map.
-   Pilot service decision.
-   API/event contract draft.
-   Security and identity approach.
-   Observability baseline.
-   CI/CD and deployment plan.
-   Cutover and rollback plan.
-   Success metrics and owners.

### Interview questions --- Assessment

**Q1. Migration start karne se pehle kya assess karoge?**

**Short answer:** Business pain, module dependencies, database coupling,
release process, reliability, team ownership aur operational readiness
assess karunga.

**Detailed answer:** Main pehle measurable problem define karunga. Phir
static aur runtime dependency maps banaunga, shared-table access
identify karunga, deployment bottlenecks aur production metrics review
karunga. Agar boundaries unclear hain, pehle modularization aur
automated tests karunga. Uske baad ek pilot capability select karke
success metrics aur rollback strategy define karunga.

**Follow-up:** Agar business bolta hai "humein microservices chahiye,"
toh kya karoge?

**Strong response:** Main technology choice ko objective nahi maanunga.
Pehle problem aur expected outcome validate karunga. Ho sakta hai
modular monolith lower-cost solution ho.

**Q2. Sabse pehle kaunsi service extract karni chahiye?**

**Answer:** Jo capability valuable, reasonably independent, testable aur
safely reversible ho. Sabse complex ya sabse critical module
automatically best pilot nahi hota.

**Q3. Migration successful kaise measure karoge?**

**Answer:** Predefined metrics compare karunga: deployment lead time,
independent releases, incident rate, p95 latency, MTTR, infrastructure
cost aur team coordination overhead. Sirf number of services ko success
metric nahi maanunga.

------------------------------------------------------------------------

## 3. Migration Part B --- Service Decomposition

Service decomposition ka goal code ko chhote folders ya deployables mein
todna nahi hai. Goal hai business responsibilities ko aise boundaries
mein organize karna jahan change, data ownership aur deployment
independently manage ho sakein.

## 3.1 Decomposition by Business Capability

Business capability batati hai ki business kya kaam karta hai. Example:
inventory manage karna, sales process karna, customer records manage
karna, payment collect karna.

DMS capabilities ka example:

``` mermaid
flowchart TD
    DMS[Dealership Management System]
    DMS --> INV[Vehicle Inventory]
    DMS --> SALES[Sales and Transactions]
    DMS --> CUST[Customer Management]
    DMS --> PAY[Payments]
    DMS --> COMP[Compliance]
    DMS --> DEALER[Dealer Management]
    DMS --> CONFIG[Dealer Configuration]
    DMS --> REPORT[Reporting and Analytics]
```

Yeh capability map candidate boundaries suggest karta hai, final
microservice count decide nahi karta. Do closely related capabilities ek
service mein reh sakti hain agar unki consistency, ownership aur change
patterns strongly coupled hon.

### Business capability vs. technical layer

**Bad decomposition:** - Controller Service. - Repository Service. -
Database Service. - Validation Service.

Is design mein ek business operation complete karne ke liye multiple
technical services ko coordinate karna padega.

**Better starting point:** - Inventory Service. - Sales Service. -
Customer Service. - Compliance Service.

Yeh business language mein responsibilities express karte hain.

## 3.2 Domain-Driven Design (DDD) se decomposition

DDD complex business domain ko samajhne aur models/boundaries define
karne mein help karta hai.

### Domain

Business problem space. DMS ka domain vehicle dealership operations ho
sakta hai.

### Subdomain

Domain ka ek specific area.

-   **Core subdomain:** Business ka differentiating capability, jaise
    complex dealer-specific transaction/compliance rules.
-   **Supporting subdomain:** Business ko support karta hai, jaise
    dealer workflow configuration.
-   **Generic subdomain:** Common capability jo market se buy ya
    standard implementation se provide ho sakti hai, jaise generic
    identity management.

Classification business context par depend karti hai. Har organization
ke liye same capability core nahi hoti.

### Bounded Context

Bounded Context ek explicit boundary hai jiske andar domain model aur
terminology ka consistent meaning hota hai.

Example: Inventory Context mein
`VehicleStatus = Available / Reserved / Sold` ho sakta hai. Compliance
Context mein vehicle ka model registration, jurisdiction aur compliance
evidence par focus kar sakta hai. Dono contexts same real-world vehicle
ko represent karte hain, lekin unka model aur rules alag ho sakte hain.

### Subdomain, bounded context, microservice --- difference

  -----------------------------------------------------------------------
  Concept                 Main question           Example
  ----------------------- ----------------------- -----------------------
  Domain                  Business kis problem    Dealership operations
                          space mein operate      
                          karta hai?              

  Subdomain               Business ka kaunsa area Inventory, Sales,
                          hai?                    Compliance

  Bounded Context         Ek consistent domain    Inventory Context
                          model ki boundary kya   
                          hai?                    

  Microservice            Kaunsi independently    Inventory Service
                          deployable/runtime unit 
                          is responsibility ko    
                          own karegi?             
  -----------------------------------------------------------------------

Ye concepts related hain, interchangeable nahi. Ek bounded context ek
microservice se map ho sakta hai, lekin deployment, scale, team ya
complexity ke reasons se mapping different bhi ho sakti hai.

### DDD context map

``` mermaid
flowchart LR
    subgraph SalesContext[Sales Bounded Context]
        SALES[Sale / Deal Model]
    end
    subgraph InventoryContext[Inventory Bounded Context]
        INV[Vehicle Availability Model]
    end
    subgraph ComplianceContext[Compliance Bounded Context]
        COMP[Registration / Evidence Model]
    end
    SALES -->|Reserve vehicle command / API| INV
    INV -->|VehicleReserved integration event| SALES
    SALES -->|SaleConfirmed event| COMP
    COMP -->|ComplianceStatusChanged event| SALES
```

**Explanation:** Sales context apna sale model own karta hai. Inventory
context authoritative availability own karta hai. Sales apni database
mein Inventory ki tables directly update nahi karta. API/command
reservation request kar sakti hai, aur event baad mein confirmed state
publish kar sakta hai.

### Ubiquitous Language

Team aur code mein business terms ka shared, precise use. Agar business
"reserved vehicle," "sale confirmed," aur "compliance approved" ko
different states maanta hai, toh code aur API contracts mein bhi unka
clear distinction hona chahiye.

### Entity, Value Object, Aggregate

-   **Entity:** Identity hoti hai, aur time ke saath state change ho
    sakti hai. Example: `Vehicle` ya `Sale`.
-   **Value Object:** Identity ke bajaye value se define hota hai.
    Example: `Money`, `VehicleRegistrationNumber` (domain rules ke
    hisaab se).
-   **Aggregate:** Related entities/value objects ka consistency
    boundary. External code normally aggregate root ke through changes
    karta hai.
-   **Aggregate Root:** Aggregate ka entry point, jo invariants protect
    karta hai.

Illustrative C# example:

``` csharp
public sealed record Money(decimal Amount, string Currency);

public sealed class Sale
{
    public Guid Id { get; private set; }
    public Guid VehicleId { get; private set; }
    public Money AgreedPrice { get; private set; }
    public string Status { get; private set; } = "Draft";

    private Sale() { } // Example for ORM materialization

    public Sale(Guid id, Guid vehicleId, Money agreedPrice)
    {
        if (agreedPrice.Amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(agreedPrice));

        Id = id;
        VehicleId = vehicleId;
        AgreedPrice = agreedPrice;
    }

    public void Confirm()
    {
        if (Status != "Draft")
            throw new InvalidOperationException("Only a draft sale can be confirmed.");

        Status = "Confirmed";
    }
}
```

Yeh illustrative domain example hai, complete production model nahi.
Real system mein currency validation, persistence mapping, domain
events, concurrency handling aur state transitions ko explicit define
karna hoga.

### Domain Event vs. Integration Event

-   **Domain Event:** Ek bounded context ke andar domain mein kuch
    meaningful happen hua.
-   **Integration Event:** Ek service doosre systems ko inform karne ke
    liye event publish karti hai, generally broker/API contract ke
    through.

Internal domain model ko directly public integration contract bana dena
coupling create kar sakta hai. Integration event ko stable, versioned
contract ke roop mein design karna better hota hai.

### Anti-Corruption Layer (ACL)

ACL ek translation layer hai jo external/legacy system ke model ko apne
domain model se isolate karti hai. Example: legacy dealership platform
`VHC_STAT = "A"` bhejta hai, lekin Inventory Service usse apne
`Available` state mein translate karti hai. Legacy codes poore domain
mein leak nahi hone chahiye.

## 3.3 Service boundary choose karne ke criteria

  -----------------------------------------------------------------------
  Criterion              Strong boundary ka       Warning sign
                         signal                   
  ---------------------- ------------------------ -----------------------
  Cohesion               Service ek focused       Unrelated
                         business capability own  responsibilities ek
                         karti hai                saath hain

  Coupling               Few stable contracts     Har request multiple
                                                  services par depend hai

  Data ownership         Ek service authoritative Multiple services same
                         writes own karti hai     tables update karti
                                                  hain

  Change frequency       Capability independently Har change ke liye
                         change hoti hai          multiple services
                                                  coordinated release
                                                  karti hain

  Consistency            Local invariants ek      Normal operation ke
                         boundary ke andar        liye frequent
                         enforce ho sakte hain    distributed
                                                  transactions chahiye

  Scaling                Different workload needs Split se meaningful
                         hain                     operational benefit
                                                  nahi

  Team ownership         Clear team responsible   Ownership ambiguous hai
                         hai                      

  Operations             Team service ko          Service maintain karne
                         deploy/monitor/support   ka operational
                         kar sakti hai            capability nahi
  -----------------------------------------------------------------------

### Common decomposition mistakes

1.  Har database table ko service banana.
2.  Har CRUD endpoint ko alag service banana.
3.  Controller, repository aur database ko separate services banana.
4.  Shared database tables ko multiple services se update karna.
5.  Har cross-service operation ko synchronous call chain banana.
6.  Boundaries ko sirf current code folders se infer karna.
7.  Business rules ko API Gateway mein move kar dena.
8.  Microservices split karna lekin deployment, monitoring aur ownership
    central bottleneck hi rehna.

## 3.4 Service boundary decision example

**Question:** Kya Customer aur Sales ko separate services banana
chahiye?

**Reasoning:** - Kya Customer capability multiple business workflows
mein independently use hoti hai? - Kya customer profile aur sale
transaction ke change patterns different hain? - Kya separate ownership
useful hai? - Kya Sales ko customer details ke liye har request par
synchronous call karna padega? - Kya Sales ko customer name/address ka
limited snapshot local sale record mein rakhna chahiye? - Kya PII access
aur retention requirements alag hain?

Answer universal nahi hai. Agar clear ownership aur independent
lifecycle ka benefit hai, separate services useful ho sakti hain. Agar
split unnecessary network calls aur operational cost create karega, toh
pehle modular monolith mein separate bounded modules rakhna better ho
sakta hai.

------------------------------------------------------------------------

## 4. Migration Pattern --- Strangler Fig

Strangler Fig pattern mein naya system ek hi baar mein poora replace
nahi hota. Selected functionality ko gradually extract kiya jata hai,
jabki baaki functionality monolith mein chalti rehti hai.

### Typical flow

``` mermaid
flowchart TD
    CLIENT[UI / API Client] --> ROUTER[Gateway / Routing Layer]
    ROUTER -->|Not yet migrated| MONO[Legacy Monolith]
    ROUTER -->|Extracted capability| NEW[New Microservice]
    NEW --> DB[(Service-owned data)]
    NEW --> OBS[Metrics / Logs / Traces]
    MONO --> LEGACYDB[(Legacy Database)]
```

### Step-by-step approach

1.  Existing business behavior aur contracts document karein.
2.  Candidate capability ke tests add karein.
3.  Service API/event contract define karein.
4.  Service boundary aur authoritative data owner decide karein.
5.  Implementation ko new service mein move karein.
6.  Traffic ko initially limited/safe route se new service tak bhejein.
7.  Functional results, latency, errors aur data correctness compare
    karein.
8.  Rollback path verify karein.
9.  Traffic gradually increase karein.
10. Jab new service authoritative ho jaye, monolith ke obsolete code aur
    data writes retire karein.

### Important caution: data dual-write

Monolith aur new service dono mein independently writes karna risky hai.
Agar ek write succeed aur doosri fail ho, data diverge kar sakta hai.
Dual-write ko simple temporary trick na samjhein. Reliable
event/outbox-based replication, single-writer ownership, CDC,
reconciliation aur explicit cutover strategy ki zaroorat ho sakti hai.

### Interview-ready answer: "How would you migrate without downtime?"

"Main Strangler Fig approach use karunga. Pehle behavior aur
dependencies map karunga, phir ek bounded capability ka contract aur
data ownership define karunga. New service deploy karke health checks,
metrics, logs aur rollback validate karunga. Routing ko gradually new
service par shift karunga, business results aur data reconcile karunga,
aur issues aane par traffic monolith par wapas route karunga. Database
writes ke liye clear single-writer strategy rakhunga, kyunki
uncontrolled dual-write data inconsistency create kar sakta hai."

------------------------------------------------------------------------

## 5. Phase 1 --- Quick Revision

### 30-second revision

-   Microservices migration business problem se start hoti hai,
    technology trend se nahi.
-   Modular monolith ko consider kiye bina microservices choose nahi
    karni chahiye.
-   Business capabilities batati hain business kya karta hai.
-   DDD bounded contexts domain model aur language ki boundaries define
    karte hain.
-   Bounded context aur microservice related hain, lekin exact
    one-to-one mapping mandatory nahi.
-   Strong service boundary mein cohesion high, coupling low aur data
    ownership clear hoti hai.
-   Strangler Fig incremental migration enable karta hai.
-   Pilot service valuable, testable, reasonably independent aur
    reversible honi chahiye.
-   Migration success ko business/engineering metrics se measure karein,
    service count se nahi.

### Phase 1 checklist

-   [ ] Migration ka business reason documented hai.
-   [ ] Current-state architecture diagram available hai.
-   [ ] Module and database dependencies mapped hain.
-   [ ] Baseline metrics recorded hain.
-   [ ] Modular monolith option evaluate kiya gaya hai.
-   [ ] Business capabilities identify ki gayi hain.
-   [ ] Initial DDD bounded-context hypothesis documented hai.
-   [ ] Pilot service selected with rationale.
-   [ ] API/event contract ka initial design hai.
-   [ ] Data ownership strategy discussed hai.
-   [ ] Deployment, observability and rollback plan defined hai.
-   [ ] Success criteria and owners documented hain.

------------------------------------------------------------------------

## Phase 1 Interview Practice --- Top 10 Questions

### 1. Monolith aur microservices mein key difference kya hai?

Monolith mein application usually ek deployable unit hoti hai.
Microservices mein business capabilities multiple independently
deployable services mein divided hoti hain. Is independence ke saath
network failures, distributed data consistency, observability aur
operational complexity bhi aati hai.

### 2. Kya microservices hamesha better hoti hain?

Nahi. Agar independent deployment/scaling ki need nahi hai aur team
chhoti hai, modular monolith simpler aur cheaper ho sakta hai.
Microservices tab choose karein jab business benefits operational cost
justify karte hon.

### 3. Business capability kya hai?

Business capability batati hai ki organization kaunsa business function
perform karti hai, jaise Inventory Management ya Compliance Processing.
Yeh technical layer jaise Controller ya Repository ke equivalent nahi
hai.

### 4. Subdomain aur bounded context mein kya difference hai?

Subdomain business problem space ka part hai. Bounded Context ek
consistent domain model aur language ki boundary hai. Dono related hain,
lekin same concept nahi hain.

### 5. Kya one bounded context = one microservice?

Zaroori nahi. One-to-one mapping common ho sakti hai, lekin deployment,
team ownership, scaling aur complexity ke basis par multiple
arrangements possible hain. Boundary ko business cohesion aur
operational needs se justify karna chahiye.

### 6. Distributed monolith kya hai?

Multiple deployed services jo tightly coupled hain---jaise coordinated
deployments, shared table writes aur long synchronous call
chains---distributed monolith bana sakti hain. Ismein microservices ki
complexity hoti hai lekin autonomy limited hoti hai.

### 7. Strangler Fig pattern kya solve karta hai?

Yeh existing monolith ko incrementally replace karne deta hai. Selected
traffic new service ko route hota hai, jabki remaining functionality
monolith mein rehti hai. Isse big-bang rewrite ka risk reduce ho sakta
hai.

### 8. Service boundary identify karne ka best signal kya hai?

Koi single signal enough nahi. Business cohesion, data ownership, change
frequency, transaction boundaries, independent scaling aur team
ownership ko together evaluate karna chahiye.

### 9. Sabse pehle high-traffic service extract karni chahiye?

Not necessarily. High traffic ka matlab high risk bhi ho sakta hai.
Pilot ko business value, boundary clarity, dependency count, testability
aur rollback safety ke basis par choose karein.

### 10. Migration success kaise measure karoge?

Deployment lead time, release independence, incident rate, MTTR, p95/p99
latency, throughput, cost aur team coordination overhead compare
karunga. Service count success metric nahi hai.

------------------------------------------------------------------------

## Phase 1 --- Key Interview Message

**"I would not start by splitting the codebase. I would start by
validating the business drivers, mapping dependencies, identifying
business capabilities and bounded contexts, and improving modularity and
testability. Then I would use the Strangler Fig pattern to extract one
valuable, low-risk capability, establish clear data ownership, add
observability and automated deployment, and use measurable outcomes to
guide the next migration steps."**

------------------------------------------------------------------------

<a id="phase-2"></a>

# Phase 2 --- Data Ownership and Distributed Transactions

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

------------------------------------------------------------------------

<a id="phase-3"></a>

# Phase 3 --- Service Communication and Messaging

> **Phase 3: Inter-Service Communication, Messaging, and Reliable Event
> Processing**\
> **Audience:** Senior Software Engineer, Staff Engineer, Tech Lead,
> Solution Architect\
> **Technology focus:** .NET 8, ASP.NET Core, HttpClientFactory, Amazon
> SQS, SNS, EventBridge, RabbitMQ/Kafka concepts\
> **Running example:** Automotive Dealership Management System (DMS)

## Phase 3 ka objective

Phase 1 mein humne service boundaries aur DDD cover kiya. Phase 2 mein
data ownership, database-per-service, Saga aur Outbox samjha. Ab focus
hai: **services ek doosre se communicate kaise karein, aur failures ke
bawajood business workflows reliable kaise rahen?**

Do main communication styles hain:

-   **Synchronous:** Caller request bhejta hai aur response ka wait
    karta hai, jaise REST ya gRPC.
-   **Asynchronous:** Sender message/event publish karta hai aur
    receiver baad mein process karta hai, jaise SQS, SNS, EventBridge,
    RabbitMQ ya Kafka.

Koi ek style har jagah best nahi. Choice latency, consistency, failure
tolerance, throughput, ordering, operational complexity aur user
experience par depend karti hai.

------------------------------------------------------------------------

# 1. Synchronous vs. Asynchronous Communication

## 1.1 Synchronous request-response

Example: Sales Service ko vehicle reserve karne se pehle Inventory
Service se authoritative decision chahiye.

``` mermaid
sequenceDiagram
    participant UI as Web UI
    participant S as Sales Service
    participant I as Inventory Service
    UI->>S: Create sale request
    S->>I: Reserve vehicle
    I-->>S: Reservation accepted / rejected
    S-->>UI: Request result
```

**Hinglish explanation:** Sales request tab tak complete nahi ho sakti
jab tak Inventory reservation ka result na de. Isliye synchronous API
useful hai. Lekin agar Inventory slow ya unavailable hai, toh Sales
request bhi delay/fail ho sakti hai.

### Benefits

-   Immediate response.
-   Request/response interaction simple hota hai.
-   Current authoritative decision lena possible hota hai.
-   Debugging flow often straightforward hota hai.

### Trade-offs

-   Runtime dependency: caller ko downstream service available chahiye.
-   Network latency add hoti hai.
-   Timeout ke baad outcome ambiguous ho sakta hai.
-   Long call chains cascading failures create kar sakti hain.
-   Retry duplicate side effects cause kar sakti hai, agar operation
    idempotent na ho.

## 1.2 Asynchronous messaging

Example: Sale confirm hone ke baad Sales Service `SaleConfirmed` event
publish karti hai. Compliance aur Reporting services apne time par event
consume karti hain.

``` mermaid
flowchart LR
    SALES[Sales Service] -->|SaleConfirmed| BUS[Message Broker]
    BUS --> COMP[Compliance Consumer]
    BUS --> REPORT[Reporting Consumer]
    BUS --> CRM[Dealer Notification Consumer]
```

Sender ko har consumer ke response ka wait nahi karna padta. Lekin
consumer processing delay ho sakta hai aur system ko eventual
consistency tolerate karni hoti hai.

### Benefits

-   Temporal coupling kam hoti hai.
-   Consumers independently scale kar sakte hain.
-   Temporary consumer outage ke dauran messages retain ho sakte hain,
    broker configuration aur retention par depend karta hai.
-   One event multiple consumers ko notify kar sakta hai.

### Trade-offs

-   Eventual consistency.
-   Duplicate delivery.
-   Out-of-order processing.
-   Retry aur dead-letter queue management.
-   Message schema/versioning.
-   End-to-end debugging ke liye correlation and tracing required.

## 1.3 Decision rule

  -----------------------------------------------------------------------
  Requirement             Commonly suitable       Caveat
                          approach                
  ----------------------- ----------------------- -----------------------
  User ko immediate       Synchronous API/command Timeout and retry
  reservation result                              semantics define karein
  chahiye                                         

  Multiple systems ko     Integration event       Consumers eventual
  completed sale ke baare                         consistency handle
  mein inform karna                               karein

  Dashboard ko            Local read model        Projection lag monitor
  high-volume combined                            karein
  data dikhana                                    

  Long-running business   Async workflow/Saga     Durable state and
  workflow                                        recovery required

  Large file or batch     Queue/event-driven      Idempotency and
  processing              processing              back-pressure design
                                                  karein

  Tiny internal call with REST or gRPC            Operational context aur
  low latency budget                              contract stability
                                                  evaluate karein
  -----------------------------------------------------------------------

**Interview point:** Asynchronous communication ka matlab automatically
faster system nahi. Isse caller wait kam kar sakta hai, lekin total work
aur end-to-end completion time phir bhi ho sakta hai.

------------------------------------------------------------------------

# 2. REST APIs in ASP.NET Core

## 2.1 REST ka role

REST over HTTP common request-response communication style hai. Service
resources aur operations expose karti hai, HTTP methods/status codes use
karti hai, aur contract ko version/compatibility ke saath evolve karti
hai.

Example: - `POST /reservations` --- reservation create karne ka
request. - `GET /vehicles/{id}` --- vehicle details. -
`GET /reservations/{id}` --- reservation status. -
`POST /payments/{id}/capture` --- payment capture request, subject to
business and provider semantics.

Endpoint naming alone REST design ko guarantee nahi karta. Contract,
status codes, validation, authorization, idempotency aur compatibility
important hain.

## 2.2 HttpClientFactory kyun?

Har request par manually new `HttpClient` create karna socket/resource
management issues aur DNS refresh behavior problems cause kar sakta hai.
ASP.NET Core mein `IHttpClientFactory` named/typed clients, handler
lifetime management, configuration aur testability provide karta hai.

Illustrative .NET 8 setup:

``` csharp
builder.Services
    .AddHttpClient<IInventoryClient, InventoryClient>(client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["Services:InventoryBaseUrl"]!);
        client.Timeout = TimeSpan.FromSeconds(3);
    });
```

Typed client:

``` csharp
public interface IInventoryClient
{
    Task<ReservationResult?> ReserveAsync(
        ReserveVehicleRequest request,
        CancellationToken cancellationToken);
}

public sealed class InventoryClient(HttpClient httpClient) : IInventoryClient
{
    public async Task<ReservationResult?> ReserveAsync(
        ReserveVehicleRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "/reservations", request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            return new ReservationResult(false, "Vehicle is no longer available");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ReservationResult>(
            cancellationToken: cancellationToken);
    }
}

public sealed record ReserveVehicleRequest(
    Guid VehicleId,
    Guid SaleId,
    string IdempotencyKey);

public sealed record ReservationResult(bool Accepted, string? Message);
```

**Important:** Yeh illustrative client hai. Production code mein API
authentication, request/response contract versioning, safe logging,
error mapping, resilience policies, idempotency semantics aur
cancellation behavior explicitly design karein.

## 2.3 Timeout aur cancellation

Timeout define karein, warna caller resources long time tak hold kar
sakta hai. `CancellationToken` downstream HTTP request tak pass karein.
Lekin caller cancellation ka matlab yeh nahi ki remote operation
definitely cancel ya rollback ho gaya. Remote service request process
kar chuki ho sakti hai.

Example: - Sales ne Inventory ko reservation request bheji. - Sales ka
timeout hit ho gaya. - Inventory ne reservation commit kar di, response
network mein lost ho gaya.

Caller ko blind new reservation create nahi karni chahiye. Same
idempotency key se retry ya reservation status query use karein.

## 2.4 HTTP status codes aur error contracts

Useful conventions: - `200 OK`: successful request with response. -
`201 Created`: resource created. - `202 Accepted`: request accepted for
asynchronous processing; work complete hona guaranteed nahi. -
`204 No Content`: success without response body. - `400 Bad Request`:
invalid request shape/validation. - `401 Unauthorized`: valid
authentication credentials missing/invalid. - `403 Forbidden`:
authenticated caller allowed nahi. - `404 Not Found`: resource not
found. - `409 Conflict`: current resource state ke saath conflict,
e.g. vehicle already reserved. - `429 Too Many Requests`: rate limit. -
`500`/`503`: server error/service temporarily unavailable, exact meaning
API contract mein clear ho.

Consistent error response use karein, jaise Problem Details
(`application/problem+json`) with stable error codes. Internal stack
traces ya secrets client ko expose na karein.

------------------------------------------------------------------------

# 3. Retries, Timeouts, Circuit Breakers, and Bulkheads

## 3.1 Retry kab karna chahiye?

Retry transient failures ke liye useful hai, jaise temporary network
error, throttling ya short-lived service unavailability. Har error
retryable nahi.

Usually do not retry blindly: - Validation failure (`400`). -
Authorization failure (`401`/`403`). - Business conflict (`409`). -
Non-idempotent operation without an idempotency mechanism.

Retries ko bounded rakhein, exponential backoff aur jitter use karein.
Multiple layers par retries multiply ho sakti hain: API Gateway 3
retries, service 3 retries, client 3 retries (yaani har layer par 3
attempts) = 3 x 3 x 3 = 27 downstream attempts. Agar har layer 3
*retries* (4 total attempts) kare, to yeh 4 x 4 x 4 = 64 ho jata hai.
Naive chain mein yeh amplification dependency ko aur gira sakta hai.

## 3.2 Exponential backoff and jitter

Backoff retry ke beech delay gradually increase karta hai. Jitter random
variation add karta hai, taaki saare clients ek saath retry na karein.

``` text
Attempt 1: immediate
Attempt 2: short delay + jitter
Attempt 3: longer delay + jitter
Then: fail, fallback, queue, or surface an error
```

Exact delay, retry count aur retryable status codes business operation
aur service SLO ke hisaab se select karein.

## 3.3 Circuit breaker

Circuit breaker repeated calls fail hone par temporary requests stop
karta hai, taaki unhealthy dependency par continuous load na pade.

``` mermaid
stateDiagram-v2
    [*] --> Closed
    Closed --> Open: Failure threshold reached
    Open --> HalfOpen: Cooldown elapsed
    HalfOpen --> Closed: Probe succeeds
    HalfOpen --> Open: Probe fails
```

-   **Closed:** Calls normally pass.
-   **Open:** Calls short-circuit/fail fast or use fallback.
-   **Half-open:** Limited probe requests test karti hain ki dependency
    recover hui ya nahi.

Circuit breaker data correctness solve nahi karta; yeh repeated failing
calls ka load reduce karta hai.

## 3.4 Bulkhead

Bulkhead resources isolate karta hai. Example: Compliance calls ke liye
limited concurrency pool ho, taaki slow Compliance dependency Sales ke
saare worker capacity exhaust na kar de.

## 3.5 Resilience flow

``` mermaid
flowchart TD
    A[Request to dependency] --> B{Within timeout?}
    B -- Yes --> C[Return response]
    B -- No --> D{Retryable and safe?}
    D -- Yes --> E[Bounded retry with backoff + jitter]
    E --> F{Retry budget remains?}
    F -- Yes --> A
    F -- No --> G[Circuit breaker / fallback / error]
    D -- No --> G
```

**Production rule:** Retry, timeout aur circuit breaker ko individually
configure karna enough nahi. Unka combined budget end-to-end latency SLO
ke andar hona chahiye.

------------------------------------------------------------------------

# 4. gRPC vs. REST

## 4.1 gRPC kya hai?

gRPC strongly typed service contracts aur Protocol Buffers use kar sakta
hai. HTTP/2 transport, code generation aur streaming support internal
service communication ke liye useful ho sakte hain.

## 4.2 Comparison

| Dimension | REST/JSON | gRPC/Protobuf |
|---|---|---|
| Human readability | High | Binary payload, less directly readable |
| Browser/client support | Broad | Browser support often needs gRPC-Web/proxy considerations |
| Contract | OpenAPI commonly used | `.proto` schema |
| Payload size | Often larger JSON | Often smaller binary encoding |
| Streaming | Possible, but implementation varies | First-class streaming patterns |
| Tooling | Very broad | Strong code generation/tooling |
| Compatibility | Careful API evolution needed | Careful protobuf field evolution needed |
| Good fit | Public APIs, broad integrations | Internal typed RPC, low-latency/streaming needs |

gRPC automatically faster in every workload nahi hota. Network, payload,
serialization, call count, connection reuse, and server behavior
benchmark karein.

------------------------------------------------------------------------

# 5. Messaging Concepts: Commands, Events, Queues, Topics

## 5.1 Command vs. event

-   **Command:** Request/intent: `ReserveVehicle`, `AuthorizePayment`.
-   **Event:** Past fact: `VehicleReserved`, `PaymentAuthorized`.

Command usually ek logical owner/handler ko target karta hai. Event
multiple interested consumers ko notify kar sakta hai.

Event naming past tense mein rakhna helpful hai, kyunki event batata hai
ki kya ho chuka hai. Command ko event ki tarah treat karna ambiguity
create kar sakta hai.

## 5.2 Domain event vs. integration event

-   Domain event: ek bounded context ke andar domain change,
    e.g. `SaleConfirmed` internal domain event.
-   Integration event: service boundary ke paar publish kiya gaya
    contract, e.g. `SaleConfirmed.v1`.

Har internal domain event ko broker par publish karna zaroori nahi.
Integration event stable contract ho, internal class structure ka direct
mirror nahi.

## 5.3 Queue vs. pub-sub vs. event stream

  -----------------------------------------------------------------------
  Concept                 Typical behavior        Example use
  ----------------------- ----------------------- -----------------------
  Queue                   Message generally one   Background job or work
                          competing consumer      distribution
                          instance/group process  
                          karta hai               

  Pub-sub topic/event bus Multiple subscriptions  Sale confirmed par
                          same event ko           Compliance and
                          independently receive   Reporting react
                          kar sakti hain          

  Event stream/log        Ordered append-oriented High-volume event
                          retained event history; processing and replay
                          consumer                
                          offsets/positions track 
                          kar sakte hain          

  Command queue           Work request deliver    Generate report,
                          karna                   process payment request
  -----------------------------------------------------------------------

Exact semantics product ke hisaab se vary karte hain; queue/topic naming
se delivery guarantees infer na karein---provider documentation aur
configuration check karein.

------------------------------------------------------------------------

# 6. AWS Messaging: SQS, SNS, EventBridge

## 6.1 Amazon SQS

SQS managed message queue hai. Common use cases: - Background
processing. - Work buffering. - Decoupling producer and worker. - Load
spikes absorb karna.

Important concepts: - Visibility timeout. - Message retention. - Redrive
policy and dead-letter queue. - Long polling. - Standard vs. FIFO
queue. - At-least-once processing assumptions for Standard queues. -
FIFO ordering/deduplication features ke exact scope aur limitations.

**Visibility timeout** message ko temporarily other consumers se hide
karta hai while consumer processes it. Agar processing timeout se zyada
lambi ho, message redeliver ho sakta hai. Processing duration, extension
strategy, idempotency aur acknowledgement behavior align karein.

## 6.2 Amazon SNS

SNS pub-sub messaging provide karta hai. Ek topic ke multiple
subscribers ho sakte hain, including supported queue and endpoint types.

Typical pattern: - Service publishes event to SNS. - SNS fans out to
multiple SQS queues. - Each consumer apni queue independently process
karti hai.

``` mermaid
flowchart LR
    SALES[Sales Service] --> SNS[SNS Topic: SaleConfirmed]
    SNS --> Q1[SQS Compliance Queue]
    SNS --> Q2[SQS Reporting Queue]
    SNS --> Q3[SQS Notification Queue]
    Q1 --> C1[Compliance Worker]
    Q2 --> C2[Reporting Worker]
    Q3 --> C3[Notification Worker]
```

Is design mein consumer teams independent scaling/retry/visibility
configure kar sakti hain. Queue backlog aur DLQ ko monitor karna zaroori
hai.

## 6.3 Amazon EventBridge

EventBridge event routing bus provide karta hai. Rules event
content/source ke basis par targets select kar sakte hain. Useful for
routing business/system events to multiple targets, integrating AWS
services, and filtering events without producer knowing each consumer.

EventBridge ko generic durable queue ka exact substitute assume na
karein. Delivery, retry, retention, replay, ordering aur target behavior
selected bus/rule/target features aur configuration par depend karte
hain.

## 6.4 SQS vs. SNS vs. EventBridge

  --------------------------------------------------------------------------------------
  Dimension           SQS                      SNS                     EventBridge
  ------------------- ------------------------ ----------------------- -----------------
  Primary role        Queue/work buffering     Pub-sub fan-out         Event routing bus

  Consumer model      Queue consumers compete  Multiple subscribers    Rules route
                      for messages                                     events to targets

  Filtering/routing   Consumer-side or         Subscription filter     Rich
                      supported filter         policies                event-pattern
                      patterns in integrations                         rules

  Backlog visibility  Queue depth and age      Depends on              Delivery/target
                      metrics                  subscriber/endpoint     metrics and
                                                                       configured
                                                                       destinations

  Typical use         Background jobs, durable Fan-out                 Event-driven
                      work queue               notifications/events    AWS/service
                                                                       integration

  Ordering            Standard/FIFO-specific   Depends on              Do not assume
                      semantics                protocol/subscription   global ordering

  DLQ/retries         Queue redrive and        Delivery policies vary  Target retry/DLQ
                      consumer handling        by endpoint             features depend
                                                                       on configuration
  --------------------------------------------------------------------------------------

Selection exact requirements aur service quotas/costs ke basis par
karein. Current AWS behavior implement karte waqt official documentation
verify karein.

## 6.5 Example: DMS sale event

1.  Sales Service local DB mein sale confirm karti hai.
2.  Same transaction mein Outbox row write hoti hai.
3.  Publisher event broker tak publish karta hai.
4.  Broker event ko Compliance aur Reporting targets tak route karta
    hai.
5.  Har consumer idempotently process karti hai.
6.  Failures retry hote hain; poison messages DLQ mein ja sakte hain.
7.  Dashboard queue age, delivery failures aur consumer lag track karta
    hai.

``` mermaid
sequenceDiagram
    participant S as Sales Service
    participant DB as Sales DB + Outbox
    participant P as Outbox Publisher
    participant B as Broker
    participant C as Compliance Consumer
    participant R as Reporting Consumer
    S->>DB: Commit Sale + Outbox event
    P->>DB: Read unpublished event
    P->>B: Publish SaleConfirmed
    B-->>C: Deliver event
    B-->>R: Deliver event
    C->>C: Validate + idempotent processing
    R->>R: Update reporting projection
```

------------------------------------------------------------------------

# 7. Delivery Semantics, Ordering, and Idempotency

## 7.1 At-most-once

Message zero ya one time process ho sakta hai; duplicate avoid karne ke
badle loss risk ho sakta hai, depending on system.

## 7.2 At-least-once

Message one or more times deliver/process ho sakta hai. Reliable systems
commonly duplicate handling ke saath at-least-once delivery use karte
hain.

## 7.3 Exactly-once

"Exactly once" phrase carefully use karein. Broker ke andar specific
processing guarantees ho sakti hain, lekin external database, payment
provider, email service aur business side effects ke across end-to-end
exactly-once outcome automatically guarantee nahi hota.

Practical strategy: - Stable message/event ID. - Idempotency key for
business commands. - Consumer deduplication/inbox. - Atomic local state
update + processed-message record. - External provider idempotency where
supported. - Reconciliation for ambiguous outcomes.

## 7.4 Ordering

Global ordering expensive aur often unnecessary hai. Business ko usually
specific entity/key ke liye ordering chahiye hoti hai, e.g. same
`VehicleId` ke state transitions.

Possible controls: - Partition/group by business key where broker
supports it. - Include aggregate version/sequence number. - Reject or
defer stale events. - Design consumers to tolerate
duplicate/out-of-order events. - Rebuild read model from authoritative
state if required.

Example: `VehicleSold` event pehle arrive ho gaya aur older
`VehicleAvailable` event baad mein process hua. Consumer
sequence/version check ke bina vehicle ko incorrectly available mark kar
sakta hai.

## 7.5 Dead-letter queue (DLQ)

DLQ un messages ko isolate karne ke liye use hoti hai jo defined
retry/redrive process ke baad process nahi ho paate.

DLQ strategy mein define karein: - Max receive attempts. - Alarm
threshold. - Owner/on-call team. - Safe inspection tooling. - PII/secret
redaction. - Replay process. - Root-cause analysis. - Replay
idempotency. - Retention and deletion policy.

DLQ ko "message problem solve ho gayi" na samjhein. DLQ backlog
unresolved work hai, jiske liye alert, ownership aur recovery process
chahiye.

------------------------------------------------------------------------

# 8. Event Contract Design and Versioning

## 8.1 Event payload design

Event mein business consumers ke liye required facts hon, unnecessary
database internals nahi.

Example:

``` json
{
  "eventId": "d2a6d1a0-0000-4000-8000-000000000001",
  "eventType": "SaleConfirmed",
  "eventVersion": 1,
  "occurredUtc": "2026-10-12T10:30:00Z",
  "correlationId": "f8c9b2b0-0000-4000-8000-000000000002",
  "saleId": "a3d6e700-0000-4000-8000-000000000003",
  "dealerId": "b4e7f800-0000-4000-8000-000000000004",
  "vehicleId": "c5f80900-0000-4000-8000-000000000005"
}
```

IDs illustrative hain. Real contract mein PII minimize karein aur
payload size/retention consider karein.

## 8.2 Backward-compatible evolution

Safer changes often include adding optional fields while preserving old
semantics. Risky changes: - Removing/renaming fields consumers depend
on. - Changing field meaning without versioning. - Changing
units/currency representation. - Reusing enum values for different
meanings. - Making previously optional data mandatory.

Versioning strategy: - Define owner and schema registry/documentation. -
Consumer-driven contract tests where useful. - Additive evolution
first. - Measure old consumer usage before removing fields. - Use
explicit new event type/version for breaking changes. - Never assume all
consumers deploy simultaneously.

## 8.3 Correlation and causation

-   **Correlation ID:** Related operations/workflow ko connect karta
    hai.
-   **Causation ID:** Kaunsa command/event current message ko trigger
    karne ka cause bana.
-   **Trace ID:** Distributed tracing context identify karta hai.

In concepts ko interchangeable mat samjhein. They can be propagated
together for troubleshooting.

------------------------------------------------------------------------

# 9. Messaging Failure Scenarios

## Scenario A: Consumer DB commit ke baad crash

Consumer business update aur inbox record commit kar deta hai, lekin
broker acknowledgement se pehle crash hota hai. Message redeliver hota
hai. Inbox deduplication duplicate side effect avoid karti hai.

## Scenario B: Consumer repeatedly fails

Transient errors par bounded retry. Permanent schema/validation error ya
retry exhaustion par DLQ. Alert trigger karein, payload safely inspect
karein, issue fix karein, phir controlled replay karein.

## Scenario C: Event publisher publish ke baad crash

Publisher broker publish kar chuka hai, lekin outbox row ko published
mark nahi kar paya. Restart par duplicate publish possible. Consumer
idempotency required hai.

## Scenario D: Queue backlog grow ho rahi hai

Check: - Incoming message rate vs. processing throughput. - Consumer
concurrency. - Downstream dependency latency. - Visibility timeout and
retry behavior. - Poison message rate. - Database connection pool
saturation. - Recent deployments. - Broker throttling/quotas.

Blindly consumer count increase na karein agar downstream DB already
saturated hai.

## Scenario E: Event out of order

Business key/version sequence validate karein. Stale event ignore/defer
karein as per domain rules. Agar workflow strict order require karta
hai, broker partition/group and producer sequencing design karein.

------------------------------------------------------------------------

# 10. .NET 8 Messaging Consumer --- Conceptual Example

Real AWS SDK code configuration aur message shape ke hisaab se vary
karega. Neeche conceptual handler ka focus idempotency boundary par hai:

``` csharp
public sealed class SaleConfirmedHandler(AppDbContext db)
{
    public async Task HandleAsync(
        SaleConfirmed message,
        CancellationToken cancellationToken)
    {
        var alreadyProcessed = await db.ProcessedMessages
            .AnyAsync(x => x.MessageId == message.EventId, cancellationToken);

        if (alreadyProcessed)
            return;

        // Apply local business update.
        var projection = await db.SaleProjections
            .SingleOrDefaultAsync(x => x.SaleId == message.SaleId, cancellationToken);

        if (projection is null)
        {
            projection = new SaleProjection { SaleId = message.SaleId };
            db.SaleProjections.Add(projection);
        }

        projection.Status = "Confirmed";

        db.ProcessedMessages.Add(new ProcessedMessage
        {
            MessageId = message.EventId,
            ProcessedUtc = DateTime.UtcNow
        });

        try
        {
            // Unique constraint on MessageId concurrent duplicates ko rokta hai.
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Production mein verify karein ki yeh specifically unique-key
            // violation hai (provider-specific error number). Agar haan, to
            // dusra consumer already process kar chuka hai -> duplicate treat
            // karke ack karein. Baaki errors rethrow karein.
            // Note: failed SaveChanges ke baad is DbContext ko reuse na karein.
            return;
        }
    }
}

public sealed record SaleConfirmed(
    Guid EventId,
    Guid SaleId,
    Guid VehicleId,
    DateTime OccurredUtc);

public sealed class ProcessedMessage
{
    public Guid MessageId { get; set; }   // unique index/primary key
    public DateTime ProcessedUtc { get; set; }
}

public sealed class SaleProjection
{
    public Guid SaleId { get; set; }
    public string Status { get; set; } = "Pending";
}
```

### Important production notes

-   `EventId` aur `ProcessedMessage.MessageId` dono `Guid` hain; dono
    jagah consistent type rakhein.
-   `catch (DbUpdateException)` blanket hai; production mein sirf
    unique-constraint violation ko duplicate maanein, baaki errors retry/
    DLQ path par jaane dein.
-   Deduplication table par unique constraint/index required hai.
-   Concurrent duplicate deliveries ke liye database
    uniqueness/concurrency handling implement karein; sirf initial
    `AnyAsync` check race condition se protect nahi karta.
-   Handler aur message acknowledgement ka order broker SDK ke model ke
    hisaab se carefully implement karein.
-   External side effects ke liye idempotency/reconciliation alag se
    required ho sakti hai.
-   Message schema validation, retry classification, DLQ, metrics,
    structured logs aur trace context add karein.

------------------------------------------------------------------------

# 11. Communication Anti-Patterns

## 11.1 Synchronous call chain

`API Gateway → Sales → Inventory → Pricing → Customer → Compliance`

Har dependency total latency badha sakti hai aur availability multiply
kar sakti hai. Agar call chain mein har dependency 99.9% available ho
aur failures independent hon, end-to-end success probability individual
dependencies se lower ho sakti hai. Real systems mein failures
independent bhi nahi hote.

Improvements: - Workflow ke liye asynchronous processing where
acceptable. - Query ke liye local read model. - Timeout and concurrency
budget. - Caching only for data where staleness is safe. - Graceful
degradation. - Avoid unnecessary service hops.

## 11.2 Retry storms

Multiple layers independently aggressive retries karte hain, jisse
already unhealthy service par load aur badh jata hai.

Fix: - Retry budget. - Exponential backoff + jitter. - Retry only
transient failures. - One primary retry layer where possible. - Circuit
breaker and rate limiting. - Queue-based buffering for async workloads.

## 11.3 Shared event contract as internal model

Internal C# domain classes directly publish karna consumers ko internal
refactors se couple kar sakta hai.

Fix: stable integration DTO/schema define karein, version and test
contracts, sensitive fields minimize karein.

## 11.4 Fire-and-forget without durability

Background task ko request handler se fire-and-forget start karna
process restart par work lose kar sakta hai.

Fix: durable queue/outbox/background processing mechanism use karein;
job status and retries track karein.

------------------------------------------------------------------------

# 12. Phase 3 Interview Questions and Answers

## Q1. Synchronous aur asynchronous communication mein difference?

**Short answer:** Synchronous caller response ka wait karta hai;
asynchronous sender message/event deliver karta hai aur receiver
independently process kar sakta hai.

**Detailed answer:** Immediate authoritative response ke liye REST/gRPC
useful hain. Multiple consumers ko notify karne, long-running workflows
aur temporary outages absorb karne ke liye messaging useful hai.
Messaging temporal coupling reduce karti hai, lekin eventual
consistency, duplicate delivery aur observability challenges introduce
karti hai.

**Follow-up:** Kya async always better hai?\
Nahi. Async workflow user ko "processing" state dikhane aur recovery
complexity require kar sakta hai. Requirements ke basis par choose
karein.

## Q2. REST ya gRPC kaise choose karoge?

REST broad client compatibility, public APIs aur human-readable JSON
contracts ke liye practical hai. gRPC typed internal RPC, code
generation aur streaming needs ke liye useful ho sakta hai. Benchmark
and operational support evaluate karunga; gRPC ko automatically faster
assume nahi karunga.

## Q3. SQS, SNS aur EventBridge mein difference?

SQS queue/work buffering, SNS pub-sub fan-out aur EventBridge event
routing/filtering ke liye common choices hain. Exact delivery, retry,
ordering aur DLQ behavior configuration aur target type par depend karta
hai.

## Q4. Command aur event mein difference?

Command intent/request hai, jaise `ReserveVehicle`. Event past fact hai,
jaise `VehicleReserved`. Command ko process karne se event emit ho sakta
hai, lekin command accepted hona aur business action completed hona alag
states hain.

## Q5. At-least-once delivery ka implication?

Consumer same message multiple times receive kar sakta hai. Handler ko
idempotent banana, stable event ID use karna aur deduplication ko
business update ke saath atomically persist karna chahiye.

## Q6. Exactly-once processing possible hai?

Specific broker/system boundary ke andar limited guarantees available ho
sakti hain, lekin end-to-end external side effects ke across
exactly-once assume nahi karna chahiye. Practical design idempotency,
deduplication, transactional boundaries aur reconciliation use karta
hai.

## Q7. Circuit breaker aur retry mein difference?

Retry transient failure par same operation dobara try karta hai. Circuit
breaker repeated failures par calls temporarily stop karke dependency ko
recover hone ka time deta hai. Dono bounded aur operation semantics ke
mutabiq configure hone chahiye.

## Q8. Timeout ke baad request retry karna safe hai?

Sirf tab jab operation idempotent ho ya idempotency key/status lookup se
duplicate side effect avoid ho. Timeout remote operation ke outcome ko
unknown bana sakta hai.

## Q9. DLQ kya hai?

Dead-letter queue retry/redrive policy ke baad repeatedly failing
messages isolate karti hai. DLQ ke liye alerting, ownership, safe
inspection, root-cause fix aur controlled replay process hona chahiye.

## Q10. Event ordering kaise handle karoge?

Jahan possible ho business key ke basis par partition/grouping,
aggregate version/sequence number, stale-event checks aur idempotent
state transitions use karunga. Global ordering assume nahi karunga.

## Q11. Queue backlog grow ho rahi hai, kya check karoge?

Producer rate vs consumer throughput, age of oldest message,
concurrency, downstream latency, retry/DLQ rates, DB saturation aur
recent deployments check karunga. Bottleneck identify kiye bina
consumers scale nahi karunga.

## Q12. Event schema evolve kaise karoge?

Additive backward-compatible changes prefer karunga, consumer contract
tests maintain karunga, breaking changes ke liye explicit versioning aur
staged migration karunga, aur old consumers ka usage verify kiye bina
fields remove nahi karunga.

## Q13. Correlation ID aur trace ID same hain?

Not necessarily. Correlation ID business workflow ko relate kar sakta
hai; trace ID tracing system ke distributed trace ko identify karta hai.
Dono propagate ho sakte hain, lekin semantics different hain.

## Q14. Outbox ke baad duplicate events kyun possible hain?

Broker publish success ke baad publisher published marker update hone se
pehle crash ho sakta hai. Restart par event dobara publish ho sakta hai.
Isliye consumer idempotency still required hai.

## Q15. Async messaging se coupling completely khatam ho jaati hai?

Nahi. Services event schema, semantics, delivery behavior aur timing
expectations par depend karti hain. Messaging temporal coupling reduce
karti hai, contract coupling eliminate nahi.

------------------------------------------------------------------------

# 13. Scenario-Based Interview Practice

## Scenario 1: Sales request mein Inventory API slow hai

**Approach:** 1. p95/p99 latency and traces inspect karein. 2. Inventory
database/query latency check karein. 3. End-to-end timeout budget define
karein. 4. Retry only safe transient failures; use idempotency key. 5.
Reservation result ambiguous ho toh status query/reconciliation karein.
6. Circuit breaker/bulkhead use karein where appropriate. 7. UI ko
meaningful pending/failure status return karein.

**Interview answer:** "I would first identify whether latency is in the
network, Inventory service, or its database using traces and metrics. I
would set bounded timeouts, avoid aggressive retries, and make
reservation idempotent. Because a timeout does not prove the reservation
failed, I would reconcile the operation using its idempotency key or
reservation status before issuing a new business action."

## Scenario 2: SaleConfirmed event duplicate process ho raha hai

**Approach:** - Stable `eventId`. - Unique processed-message
constraint. - Business update and inbox record in one local
transaction. - Duplicate acknowledged without repeating side effect. -
External side effects use provider idempotency or workflow state.

## Scenario 3: SQS backlog badh rahi hai

**Approach:** - Queue age/depth, incoming and completed rates inspect. -
Consumer errors and retry counts inspect. - Downstream DB/API bottleneck
identify. - Concurrency cautiously adjust. - Visibility timeout align. -
Poison messages DLQ mein isolate. - Recovery rate and business impact
monitor.

## Scenario 4: Consumer ko VehicleSold ke baad VehicleAvailable event milta hai

**Approach:** - Event version/sequence number validate. - Older event
reject/defer according to domain rules. - State transition invariants
enforce. - Agar strict ordering required hai, broker
partitioning/grouping design karein. - Invalid transition ke liye alert
and reconciliation.

------------------------------------------------------------------------

# 14. Phase 3 Quick Revision Cheat Sheet

  ---------------------------------------------------------------------
  Concept                            Remember this
  ---------------------------------- ----------------------------------
  REST                               Request-response; caller
                                     downstream availability/latency
                                     par depend karta hai

  gRPC                               Typed RPC and streaming; use when
                                     contract/performance needs justify
                                     it

  Queue                              Work buffering and competing
                                     consumers

  Pub-sub                            Multiple subscribers independently
                                     react

  Command                            Intent/request

  Event                              Past fact

  Retry                              Transient failures; bounded and
                                     idempotency-aware

  Timeout                            Caller wait limit; remote outcome
                                     may be unknown

  Circuit breaker                    Repeated failing calls ko
                                     temporarily stop karta hai

  Bulkhead                           Resource pools isolate karta hai

  At-least-once                      Duplicate delivery possible

  Idempotency                        Repeat request se unintended
                                     repeated effect na ho

  DLQ                                Failed messages ke liye owned
                                     recovery queue

  Event versioning                   Contracts evolve without breaking
                                     consumers

  Correlation ID                     Related workflow operations
                                     connect karta hai

  Trace ID                           Distributed trace identify karta
                                     hai
  ---------------------------------------------------------------------

## Phase 3 checklist

-   [ ] Har call ke liye sync vs async decision justified hai.
-   [ ] HTTP contracts and error codes consistent hain.
-   [ ] HttpClientFactory and cancellation tokens use kiye gaye hain.
-   [ ] Timeouts end-to-end latency budget ke andar hain.
-   [ ] Retries bounded hain aur idempotency semantics clear hain.
-   [ ] Circuit breaker/bulkhead actual failure mode ke hisaab se use
    hote hain.
-   [ ] Queue/topic semantics documented hain.
-   [ ] Events stable, versioned contracts hain.
-   [ ] Consumers duplicates safely handle karte hain.
-   [ ] Ordering requirements business key ke basis par defined hain.
-   [ ] DLQ alerting and replay ownership clear hai.
-   [ ] Queue age, backlog, retries, failures and consumer throughput
    monitored hain.
-   [ ] Correlation and trace context propagate hota hai.

## Phase 3 --- Key Interview Message

"I choose synchronous communication when a caller needs an immediate
authoritative response, and asynchronous messaging when independent
consumers need to react or work can complete later. I treat timeouts as
ambiguous outcomes, keep retries bounded, make side-effecting operations
idempotent, and design for duplicate and out-of-order messages. With
AWS, I select SQS, SNS, or EventBridge based on queueing, fan-out, and
routing needs, and I monitor backlog, age, retry rates, DLQs, and
end-to-end traces."

------------------------------------------------------------------------

<a id="phase-4"></a>

# Phase 4 --- API Gateway, UI Integration, and Security

> **Phase 4: UI Integration, API Gateway, BFF, and Security**\
> **Audience:** Senior Software Engineer, Staff Engineer, Tech Lead,
> Solution Architect\
> **Technology focus:** ASP.NET Core, .NET 8, API Gateway, OAuth 2.0,
> OpenID Connect, JWT\
> **Running example:** Automotive Dealership Management System (DMS)

## Phase 4 ka objective

Phase 3 mein humne services ke beech REST, gRPC aur asynchronous
messaging cover ki. Ab sawal hai: **frontend ko multiple backend
services ke saath securely aur efficiently integrate kaise karna
chahiye?**

Agar ek DMS web UI ko Inventory, Sales, Customer, Payment aur Compliance
services se interact karna hai, toh browser ko har service ki location,
contract aur authentication details directly manage karni pad sakti
hain. API Gateway aur BFF is complexity ko centralize ya client-specific
backend mein organize kar sakte hain.

Is phase mein hum API Gateway, BFF, frontend integration,
micro-frontends, authentication, authorization aur service-to-service
security cover karenge.

------------------------------------------------------------------------

# 1. Frontend Integration Options

## 1.1 Option A: Frontend directly calls every microservice

``` mermaid
flowchart TD
    UI[Web UI] --> INV[Inventory API]
    UI --> SALES[Sales API]
    UI --> CUST[Customer API]
    UI --> COMP[Compliance API]
```

### Benefits

-   Simple systems mein extra gateway component ki zaroorat nahi.
-   Frontend service APIs ko directly consume kar sakta hai.
-   Small internal applications ya tightly controlled environments mein
    practical ho sakta hai.

### Risks

-   Browser ko multiple service URLs aur contracts manage karne padte
    hain.
-   CORS, authentication, versioning aur rate limiting
    repeated/configure karne pad sakte hain.
-   UI multiple services ki availability aur latency par directly depend
    kar sakti hai.
-   Internal topology client tak leak ho sakti hai.
-   UI ko five calls karne pad sakte hain to render one page.
-   Public browser ko service discovery ya private endpoints expose nahi
    karne chahiye.

Direct access automatically insecure nahi, lekin security boundaries,
token audience, CORS aur API exposure carefully design karna hoga.

## 1.2 Option B: API Gateway

API Gateway client-facing entry point hota hai. Yeh routing aur
cross-cutting policies ke liye useful hai.

``` mermaid
flowchart TD
    UI[Web / Mobile UI] --> GW[API Gateway]
    GW --> INV[Inventory Service]
    GW --> SALES[Sales Service]
    GW --> CUST[Customer Service]
    GW --> PAY[Payment Service]
    GW --> COMP[Compliance Service]
```

Common gateway responsibilities: - Request routing. - TLS termination,
based on deployment architecture. - Authentication integration/token
validation, depending on gateway capability. - Rate limiting and
request-size limits. - Request/response headers and limited
transformation. - API version routing. - Access logs, metrics and trace
propagation. - Coarse-grained request policies.

Gateway ko business rules ka central home nahi banana chahiye. Example:
"vehicle available hai ya nahi?" ka authoritative decision Inventory
Service mein hona chahiye, gateway mein nahi.

### API Gateway ke benefits

-   Clients ko stable entry point.
-   Cross-cutting policies ek jagah configure karna easier.
-   Internal services public network par expose karne ki need reduce ho
    sakti hai.
-   Routing and version transition manage kar sakta hai.

### Trade-offs

-   Gateway bottleneck ya single failure point ban sakta hai if not
    deployed redundantly.
-   Too much logic gateway mein daalne se coupling badhti hai.
-   Configuration and policy changes ka blast radius large ho sakta hai.
-   Gateway service-to-service authorization replace nahi karta.
-   Gateway ke baad har service ko apne business
    authorization/invariants enforce karne chahiye.

## 1.3 API Gateway failure and scaling

Gateway ko highly available deploy karein, capacity and latency monitor
karein, rate limits define karein, aur configuration changes test
karein. Gateway failure ke impact ko load balancers, multiple
instances/managed service capabilities, health checks and safe rollout
se reduce karein.

Metrics: - Request rate. - p95/p99 latency. - 4xx/5xx rates. - Throttled
requests. - Upstream latency. - Upstream timeout rate. -
Connection/resource saturation.

------------------------------------------------------------------------

# 2. Backend for Frontend (BFF)

## 2.1 BFF kya hai?

Backend for Frontend ek client-specific backend layer hai jo particular
frontend ke use cases ke liye APIs expose karti hai. Web app aur mobile
app ki requirements different hon toh separate BFFs useful ho sakte
hain.

Example: - Web dashboard ko detailed inventory, sales and compliance
summary chahiye. - Mobile sales app ko compact vehicle search, customer
lookup aur sale status chahiye. - Dono clients ka payload, latency
budget aur interaction flow different ho sakta hai.

``` mermaid
flowchart TD
    WEB[Dealer Web UI] --> WBFF[Web BFF]
    MOB[Mobile Sales App] --> MBFF[Mobile BFF]
    WBFF --> INV[Inventory Service]
    WBFF --> SALES[Sales Service]
    WBFF --> COMP[Compliance Service]
    MBFF --> INV
    MBFF --> SALES
    SALES --> DB[(Sales DB)]
    INV --> IDB[(Inventory DB)]
    COMP --> CDB[(Compliance DB)]
```

BFF data aggregation, client-specific response shaping aur orchestration
for presentation use cases kar sakta hai. Domain invariants,
authoritative writes aur core business rules domain services mein rehne
chahiye.

## 2.2 API Gateway vs. BFF

  -----------------------------------------------------------------------
  Dimension               API Gateway             BFF
  ----------------------- ----------------------- -----------------------
  Primary purpose         Common entry point and  Specific client
                          cross-cutting policies  experience ke liye
                                                  backend

  Typical scope           Routing, TLS, rate      Aggregation, response
                          limits, auth            shaping,
                          integration, telemetry  client-specific
                                                  workflows

  Client-specific?        Usually common          Yes, by design

  Business rules          Core business rules     Core domain rules nahi
                          nahi hone chahiye       hone chahiye

  Example                 `/api/sales` route to   Web dashboard summary
                          Sales Service           API

  Risk                    Becomes "smart" central Duplicate logic across
                          monolith                BFFs
  -----------------------------------------------------------------------

A system mein gateway aur BFF dono ho sakte hain: browser → gateway →
web BFF → domain services. Lekin simple application mein dono layers add
karna unnecessary complexity ho sakta hai.

## 2.3 BFF kab use karein?

Useful when: - Multiple clients ke response shapes materially different
hain. - Frontend ko many backend calls aggregate karni padti hain. -
Mobile bandwidth/latency constraints hain. - Client-specific release
cadence or experience needs hain.

Avoid when: - Sirf ek thin frontend hai aur aggregation simple hai. -
BFF core business rules duplicate karne lagta hai. - Har client ke liye
separate backend maintain karna team capacity se bahar hai.

## 2.4 BFF mein caching

Caching tab useful hai jab: - Data read-heavy ho. - Staleness acceptable
ho. - Cache key user/tenant/authorization context ko correctly include
kare. - Invalidation/freshness rules defined hon.

Sensitive dealer/customer data ko shared cache mein authorization
boundary ke bina cache na karein. Reservation availability jaisi rapidly
changing authoritative state ke liye stale cache ko final decision
source na banayein.

------------------------------------------------------------------------

# 3. API Composition and Dashboard Aggregation

DMS dashboard ko inventory count, pending sales, compliance status aur
payment summaries chahiye ho sakti hain.

## 3.1 Simple API composition

BFF/Gateway ya dedicated query endpoint several services call karke
combined response return karta hai.

``` mermaid
sequenceDiagram
    participant UI as Web UI
    participant BFF as Dashboard BFF
    participant I as Inventory Service
    participant S as Sales Service
    participant C as Compliance Service
    UI->>BFF: GET /dashboard
    par Inventory summary
        BFF->>I: GET /summary
        I-->>BFF: Inventory totals
    and Sales summary
        BFF->>S: GET /summary
        S-->>BFF: Sales totals
    and Compliance summary
        BFF->>C: GET /summary
        C-->>BFF: Pending cases
    end
    BFF-->>UI: Combined dashboard response
```

Parallel calls latency reduce kar sakti hain compared with sequential
calls, lekin overall request slowest dependency ke response par depend
kar sakti hai.

## 3.2 When a read model is better

Agar dashboard high traffic hai, multiple services aggregate karta hai,
aur near-real-time data enough hai, event-driven read model use karein.
BFF local projection query karega instead of every page load par many
services call karna.

Trade-off: - Faster and more resilient reads. - Projection lag and
eventual consistency. - Extra data pipeline and repair process.

------------------------------------------------------------------------

# 4. Micro-Frontends: Kab Useful Hain?

## 4.1 Concept

Micro-frontends frontend ko independently owned/deployed UI areas mein
divide karte hain. Example: Inventory UI, Sales UI aur Compliance UI
different teams manage kar sakti hain.

``` mermaid
flowchart TD
    SHELL[Dealer Portal Shell]
    SHELL --> INVUI[Inventory Micro-Frontend]
    SHELL --> SALESUI[Sales Micro-Frontend]
    SHELL --> COMPUI[Compliance Micro-Frontend]
    INVUI --> BFF[API Gateway / BFF]
    SALESUI --> BFF
    COMPUI --> BFF
```

### Benefits

-   Team-level frontend ownership.
-   Independent release possible.
-   Large UI teams parallel work kar sakti hain.
-   Distinct workflows ke release cadence separate ho sakte hain.

### Risks

-   Shared navigation, authentication, design system and routing
    complexity.
-   Bundle size and duplicated dependencies.
-   Inconsistent UX.
-   Cross-micro-frontend communication complexity.
-   Integration testing and runtime failure handling.
-   Multiple deployments can create version compatibility issues.

Microservices architecture ka matlab micro-frontends mandatory nahi.
Agar ek frontend team ek cohesive app maintain kar sakti hai, modular
frontend often simpler hai.

------------------------------------------------------------------------

# 5. Authentication vs. Authorization

## 5.1 Authentication

Authentication answer karta hai: **"Aap kaun ho?"**

Examples: - User identity provider ke through sign in karta hai. -
Application token validate karti hai. - Service caller ki identity
verify karti hai.

## 5.2 Authorization

Authorization answer karta hai: **"Aapko kya karne ki permission hai?"**

Examples: - Dealer user sirf apne dealership ka data access kar sakta
hai. - Sales representative sale create kar sakta hai, lekin refund
approve nahi. - Compliance officer compliance case update kar sakta hai.

Valid JWT hona automatically har resource access ki permission nahi
deta. Authentication aur authorization alag checks hain.

## 5.3 Security architecture

``` mermaid
sequenceDiagram
    participant U as User / Browser
    participant IDP as Identity Provider
    participant GW as API Gateway
    participant S as Sales Service
    participant DB as Sales DB
    U->>IDP: Sign in
    IDP-->>U: Tokens / authorization result
    U->>GW: API request + access token
    GW->>GW: Validate token / gateway policies
    GW->>S: Forward authenticated request/context
    S->>S: Validate identity and business authorization
    S->>DB: Read/write allowed sale data
    DB-->>S: Result
    S-->>U: Response via gateway
```

Exact token flow application type, identity provider and architecture
par depend karta hai. Access token ko intended API audience ke liye
issue/validate karna chahiye. ID token primarily client ko user
authentication information dene ke liye hota hai; use blindly API access
token ki tarah send/accept na karein.

------------------------------------------------------------------------

# 6. OAuth 2.0, OpenID Connect, and JWT

## 6.1 OAuth 2.0

OAuth 2.0 authorization framework hai. Yeh define karta hai ki client
access token kaise obtain/use kare to protected resources access kar
sake. OAuth by itself user authentication protocol nahi hai.

## 6.2 OpenID Connect (OIDC)

OIDC OAuth 2.0 ke upar identity layer add karta hai. Ismein ID token aur
user authentication claims define kiye jaate hain.

## 6.3 JWT

JWT ek token format hai jo claims carry kar sakta hai. Signed JWT ki
signature validate karni hoti hai; relevant claims bhi check karne hote
hain: - Issuer (`iss`). - Audience (`aud`). - Expiration (`exp`). -
Not-before (`nbf`), if present. - Signing key/algorithm as configured. -
Required scopes/roles/claims.

JWT encrypted hona zaroori nahi; signed JWT payload generally readable
hota hai. Isliye passwords, secrets, payment card details ya unnecessary
PII token mein na daalein.

## 6.4 Typical browser flow

For a browser-based application, use a well-reviewed OIDC flow suitable
for the client architecture, typically Authorization Code Flow with
PKCE. Token storage and refresh strategy threat model ke hisaab se
decide karein. Browser local storage mein long-lived sensitive tokens
rakhne ke risks evaluate karein; a BFF/session-cookie design can reduce
direct token exposure to JavaScript but introduces its own
cookie/CSRF/session considerations.

Avoid: - Implicit flow for modern apps. - Access token ko URL query
string mein rakhna. - JWT decode karne ko validation samajhna. - Token
signature, issuer, audience, expiry validate na karna. - ID token ko API
access token treat karna.

------------------------------------------------------------------------

# 7. .NET 8 JWT Authentication Example

Illustrative ASP.NET Core configuration using an identity provider's
authority and expected audience:

``` csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SalesWrite", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(ctx =>
                  // OAuth2 "scope" claim aksar space-separated single string
                  // hota hai ("sales.read sales.write"); kuch providers "scp"
                  // use karte hain (string ya multiple claims).
                  ctx.User.Claims
                     .Where(c => c.Type is "scope" or "scp")
                     .SelectMany(c => c.Value.Split(' ',
                         StringSplitOptions.RemoveEmptyEntries))
                     .Contains("sales.write")));
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/sales", () => Results.Accepted())
   .RequireAuthorization("SalesWrite");

app.Run();
```

**Important production notes:** - Claim shape and scope representation
identity provider par depend karti hai;
`scope` aksar space-separated string hota hai, isliye exact-match
`RequireClaim("scope", "sales.write")` multi-scope token par fail ho
sakta hai; example mein scope ko split karke check kiya gaya hai. Provider
ke actual claim format (`scope` vs `scp`) ke hisaab se adapt karein. - `Authority` and `Audience` correct
configure karein. - Issuer/audience/lifetime/signature validation
disable na karein. - Role/scope checks ke saath dealer/tenant-level
resource authorization bhi enforce karein. - Secrets configuration files
mein commit na karein. - Production HTTPS, key rotation, clock skew,
token lifetime and logging/redaction plan define karein. - Gateway token
validate kare tab bhi downstream service ko trust boundary ke hisaab se
authentication/authorization enforce karni chahiye.

------------------------------------------------------------------------

# 8. Service-to-Service Security

Browser-to-API authentication se alag, services ko ek doosre ki identity
verify karni hoti hai.

Common approaches: - OAuth 2.0 client credentials flow for
machine-to-machine access. - Workload identity / cloud-native
identity. - Mutual TLS where appropriate. - Short-lived credentials and
least privilege. - Private networking plus identity-based
authorization. - Secret manager for credentials when static secrets are
unavoidable.

Network ke andar hone ko trusted identity assume na karein. "Internal
service" bhi compromised ya misconfigured ho sakti hai.

## 8.1 Service identity example

Sales Service Inventory API ko call karti hai. Sales ke workload
identity ko sirf required Inventory operation/scope ki permission milni
chahiye. Sales credential ko Payment refund approve karne ki permission
nahi milni chahiye.

## 8.2 Security controls

-   Least privilege.
-   Token audience/scope validation.
-   Per-service identity.
-   Secrets rotation.
-   TLS in transit.
-   Encryption at rest.
-   Rate limiting and abuse protection.
-   Audit logs.
-   PII minimization.
-   Dependency and container scanning.
-   Authorization checks close to data/business operation.
-   Tenant/dealer boundary enforcement.

------------------------------------------------------------------------

# 9. API Protection and Multi-Tenant Authorization

DMS mein ek dealership user ko doosri dealership ka customer, sale ya
vehicle data access nahi karna chahiye.

Authorization checks mein: 1. Caller identity validate. 2. Required
operation permission/scope check. 3. Requested resource ka dealer/tenant
ownership check. 4. Business state/invariant validate. 5. Audit
event/log where appropriate.

Sirf UI mein buttons hide karna security control nahi. API/server side
par authorization mandatory hai.

Example conceptual code:

``` csharp
using System.Security.Claims;

app.MapGet("/dealers/{dealerId:guid}/sales",
    async (Guid dealerId, ClaimsPrincipal user, ISalesRepository repo,
           CancellationToken ct) =>
    {
        var permittedDealerIds = user.FindAll("dealer_id")
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!permittedDealerIds.Contains(dealerId.ToString()))
            return Results.Forbid();

        var sales = await repo.GetByDealerAsync(dealerId, ct);
        return Results.Ok(sales);
    })
   .RequireAuthorization();
```

Yeh simplified example hai. Production implementation mein trusted claim
source, delegated permissions, policy-based authorization, data
filtering, tenant isolation and auditing ko central design ka part
banayein. User-supplied `dealerId` ko trusted authorization evidence
nahi samjhein.

------------------------------------------------------------------------

# 10. API Gateway and BFF Anti-Patterns

## Anti-pattern 1: Gateway contains business rules

**Problem:** Vehicle pricing, sale approval aur compliance decisions
gateway mein implemented hain.\
**Risk:** Gateway all business domains ka tightly coupled central
application ban jata hai.\
**Better:** Domain rules authoritative service mein rakhein.

## Anti-pattern 2: BFF duplicates domain logic

**Problem:** Web BFF aur Mobile BFF alag-alag sale eligibility rules
implement karte hain.\
**Risk:** Different clients different decisions return kar sakte hain.\
**Better:** BFF presentation/aggregation handle kare; domain service
business invariants enforce kare.

## Anti-pattern 3: Every client gets a microservice

**Problem:** Har tiny screen ke liye separate BFF maintain hota hai.\
**Risk:** Excessive deployment and maintenance overhead.\
**Better:** Client-specific backend tab add karein jab meaningful
differences justify karein.

## Anti-pattern 4: Gateway is the only security layer

**Problem:** Downstream service assume karti hai ki gateway se aaya
request automatically authorized hai.\
**Risk:** Direct/internal calls authorization bypass kar sakti hain.\
**Better:** Service identity, token trust, resource authorization and
least privilege enforce karein.

## Anti-pattern 5: JWT contains too much data

**Problem:** Token mein full customer profile ya sensitive details store
karna.\
**Risk:** Exposure, stale claims, oversized headers.\
**Better:** Minimum claims; sensitive/current data authorized APIs se
fetch karein.

------------------------------------------------------------------------

# 11. Phase 4 Interview Questions and Answers

## Q1. API Gateway kya hai?

**Short answer:** API Gateway client requests ka common entry point hai
jo routing aur cross-cutting policies provide karta hai.

**Detailed answer:** Gateway service routing, TLS, rate limiting,
request-size policies, API version routing and telemetry mein help kar
sakta hai. Lekin core business rules domain services mein hone chahiye.
Gateway ko highly available aur monitored rakhna chahiye, kyunki client
traffic us par depend kar sakta hai.

## Q2. API Gateway aur BFF mein difference?

Gateway common entry point and cross-cutting policies manage karta hai.
BFF particular client---jaise web ya mobile---ke use cases ke liye data
aggregate/shape karta hai. Dono ek architecture mein coexist kar sakte
hain, but simple apps mein separate layers unnecessary ho sakti hain.

## Q3. BFF kab introduce karoge?

Jab web/mobile clients ke API needs materially different hon, multiple
backend calls aggregate karni padti hon, ya client-specific
performance/payload requirements hon. BFF ko domain business rules
duplicate nahi karne dunga.

## Q4. Micro-frontends kab useful hain?

Large frontend organization, independent team ownership and release
cadence ke liye useful ho sakte hain. Small/cohesive team ke liye
modular single frontend usually simpler hai. Microservices automatically
micro-frontends require nahi karti.

## Q5. Authentication vs. authorization?

Authentication identity establish karta hai---caller kaun hai.
Authorization decide karta hai caller particular action/resource access
kar sakta hai ya nahi. Valid token ka matlab unrestricted access nahi.

## Q6. OAuth 2.0 vs. OpenID Connect?

OAuth 2.0 protected resources access karne ke liye authorization
framework hai. OIDC OAuth 2.0 par identity/authentication layer add
karta hai. OIDC ID token ko API access token ke saath confuse nahi karna
chahiye.

## Q7. JWT validate kaise karoge?

Signature/key, issuer, audience, expiration and applicable claims/scopes
validate karunga. Token decode karna validation nahi. Sensitive
information JWT payload mein store nahi karunga, kyunki signed JWT
necessarily encrypted nahi hota.

## Q8. Gateway token validate karta hai, kya service ko authorization karni chahiye?

Haan. Service ko trusted identity context verify karna aur
business/resource-level authorization enforce karna chahiye. Gateway
controls useful hain, lekin service ke business invariants ka substitute
nahi.

## Q9. Dealer-level authorization kaise enforce karoge?

Caller ke trusted identity/claims se permitted dealer scope establish
karunga, phir every relevant resource operation par dealer ownership
check karunga. UI filtering enough nahi; server-side query and operation
enforce karna zaroori hai.

## Q10. Service-to-service authentication kaise karoge?

OAuth client credentials/workload identity/mTLS jaise options use kar
sakta hoon. Har service ko least privilege aur short-lived credentials
dunga. Network location alone ko trust proof nahi maanunga.

## Q11. Gateway bottleneck ban raha hai---kya karoge?

Gateway latency, upstream latency, request rate, throttling, 5xx,
connection saturation and deployment changes check karunga.
Capacity/replicas or managed scaling, caching where safe, request
optimization and high availability evaluate karunga. Blindly gateway
bypass karna security and governance regressions create kar sakta hai.

## Q12. Dashboard five services se data fetch karta hai. Best design?

Low traffic/simple aggregation mein BFF parallel API calls use kar sakta
hai. High traffic dashboard with near-real-time needs ke liye event-fed
read model better ho sakta hai. Choice freshness, latency, failure
tolerance and operational complexity par depend karegi.

## Q13. Browser mein JWT local storage mein rakhna safe hai?

Threat model par depend karta hai. JavaScript-accessible storage XSS ke
case mein token theft risk create karta hai. BFF/session-cookie pattern
token exposure reduce kar sakta hai, but CSRF, cookie flags, session
management and deployment complexity handle karne honge. Universal
one-line answer avoid karein.

## Q14. Gateway ke baad internal services ko public expose karna chahiye?

Usually only required endpoints expose karein. Internal services private
networking and service identity ke saath accessible ho sakti hain. But
private network alone authorization ka replacement nahi.

## Q15. BFF mein business validation rakh sakte hain?

Presentation-level validation---jaise required UI fields---rakh sakte
hain. Lekin authoritative invariants, jaise vehicle already sold hai ya
sale approval allowed hai, domain service mein enforce hone chahiye.

------------------------------------------------------------------------

# 12. Scenario-Based Interview Practice

## Scenario 1: Dealer dashboard 6 services call karta hai aur slow hai

**Solution:** 1. Distributed traces se identify karein kaunse calls slow
hain. 2. BFF sequential calls ko safe parallel calls mein convert kar
sakta hai. 3. Per-call timeout and partial response policy define
karein. 4. Non-critical panel fail hone par graceful degradation
consider karein. 5. High-volume use case mein event-fed read model
evaluate karein. 6. Authorization boundaries and cache keys verify
karein.

**Interview answer:** "I would first use traces to identify whether
latency comes from sequential aggregation or one slow dependency. For
independent summaries I may parallelize calls and define a strict
latency budget. If the dashboard is high-volume and slight staleness is
acceptable, I would consider a dedicated read model. I would not move
domain rules into the BFF just to reduce calls."

## Scenario 2: User URL mein doosre dealer ka ID change kar deta hai

**Solution:** Server-side resource authorization enforce karein.
User-supplied ID trusted permission nahi. Query ko caller ke permitted
tenant/dealer scope se constrain karein, unauthorized access audit
karein, and tests include karein.

## Scenario 3: Gateway down ho gaya

**Solution:** Multi-instance/managed high-availability deployment,
health checks, upstream monitoring, safe config rollout and tested
recovery plan. Gateway ko business state ka single source of truth na
banayein.

## Scenario 4: Mobile app ko Web UI se smaller payload chahiye

**Solution:** BFF client-specific response shape provide kar sakta hai,
but business rules shared domain services mein remain karenge. Contracts
versioned and documented honge.

------------------------------------------------------------------------

# 13. Phase 4 Quick Revision Cheat Sheet

  ---------------------------------------------------------------------
  Concept                            Remember this
  ---------------------------------- ----------------------------------
  API Gateway                        Common entry point and
                                     cross-cutting policies

  BFF                                Client-specific
                                     aggregation/response shaping

  Micro-frontends                    Independent UI areas; not
                                     mandatory with microservices

  Authentication                     Who are you?

  Authorization                      What may you do?

  OAuth 2.0                          Authorization framework

  OIDC                               Identity/authentication layer over
                                     OAuth 2.0

  JWT                                Token format; signature and claims
                                     must be validated

  Access token                       Intended for API/resource access

  ID token                           Authentication information for
                                     client

  Least privilege                    Minimum permissions needed

  Resource authorization             Check actual resource/tenant
                                     ownership

  Gateway resilience                 Redundancy, latency/error metrics,
                                     safe rollout

  BFF caching                        Only with freshness and
                                     tenant-aware cache keys

  Business invariants                Enforce in authoritative domain
                                     service
  ---------------------------------------------------------------------

## Phase 4 checklist

-   [ ] Direct UI-to-service exposure evaluated.
-   [ ] Gateway responsibilities clearly scoped.
-   [ ] BFF justified by client-specific requirements.
-   [ ] Business logic remains in domain services.
-   [ ] Gateway is highly available and monitored.
-   [ ] Authentication and authorization are distinct.
-   [ ] OAuth/OIDC token types are used correctly.
-   [ ] JWT issuer, audience, signature and lifetime validated.
-   [ ] Service-to-service identity and least privilege defined.
-   [ ] Dealer/tenant resource authorization enforced server-side.
-   [ ] PII and tokens are not logged or unnecessarily cached.
-   [ ] Rate limits and abuse protection configured.
-   [ ] UI/BFF contracts have tests and compatibility strategy.

## Phase 4 --- Key Interview Message

"I would normally expose a controlled client-facing entry point through
an API Gateway, and introduce a BFF only when web/mobile clients have
meaningful differences in aggregation or response needs. The gateway
handles routing and cross-cutting policies; domain services remain
responsible for business invariants and resource authorization. I would
use an appropriate OAuth 2.0/OIDC flow, validate tokens correctly,
enforce tenant-level authorization in the service, and secure
service-to-service calls with identity and least privilege rather than
trusting network location alone."

------------------------------------------------------------------------

<a id="phase-5"></a>

# Phase 5 --- Observability, Reliability, and Operations

## Phase 5: Observability, Monitoring, Resilience & Production Troubleshooting

**Audience:** Senior Software Engineer, Staff Engineer, Tech Lead,
Solution Architect\
**Technology context:** .NET 8, ASP.NET Core, C#, SQL Server, AWS,
microservices\
**Style:** Hinglish (Roman Hindi + English technical terminology)

------------------------------------------------------------------------

## Is phase ka goal

Microservices mein code likhna sirf aadha kaam hai. Production mein jab
8--10 services, databases, queues aur external APIs ek request ko
process karte hain, tab issue ko jaldi locate karna equally important
hota hai.

Is phase ke baad aap confidently explain kar paayenge:

-   Logs, metrics aur distributed traces ka role.
-   RED aur USE monitoring methodologies.
-   .NET 8 application mein structured logging aur OpenTelemetry.
-   Trace context ko HTTP aur asynchronous messages ke through propagate
    karna.
-   AWS CloudWatch aur AWS X-Ray ko observability architecture mein use
    karna.
-   Liveness, readiness aur startup health checks.
-   Timeouts, retries, circuit breakers, bulkheads aur graceful
    degradation.
-   SLI, SLO, SLA aur error budgets.
-   Production incident ko evidence-based tareeke se investigate karna.

------------------------------------------------------------------------

# 1. Monitoring vs Observability

## 1.1 Monitoring kya hai?

Monitoring predefined signals ko continuously track karta hai. Example:

-   CPU utilization 90% se upar chala gaya.
-   HTTP 5xx error rate 5% cross kar gaya.
-   Queue mein 10,000 messages pending hain.
-   Database connections available nahi hain.

Monitoring ka typical question hota hai: **"Kya koi known problem ho
rahi hai?"**

## 1.2 Observability kya hai?

Observability system ke outputs---logs, metrics, traces aur events---ke
basis par system ki internal state samajhne ki capability hai. Iska
focus unknown ya unexpected problems ko diagnose karna bhi hai.

Typical question: **"Yeh problem kyun ho rahi hai, aur request kis
dependency par slow hui?"**

  -----------------------------------------------------------------------
  Area                    Monitoring              Observability
  ----------------------- ----------------------- -----------------------
  Primary focus           Known failure           Known + unknown failure
                          conditions              diagnosis

  Typical output          Dashboard, alert        Logs, metrics, traces,
                                                  correlations

  Example                 API 5xx rate badh gaya  Kaunsa endpoint,
                                                  dependency ya query 5xx
                                                  cause kar rahi hai?

  Best use                Detection               Detection +
                                                  investigation
  -----------------------------------------------------------------------

Dono alternatives nahi hain. Effective production engineering ko
monitoring aur observability dono chahiye.

------------------------------------------------------------------------

# 2. The Three Pillars: Logs, Metrics, Traces

``` mermaid
flowchart TD
    R[Incoming request] --> A[Sales API]
    A --> I[Inventory service]
    A --> P[Payment service]
    A --> D[(Sales database)]
    A --> L[Logs]
    A --> M[Metrics]
    A --> T[Distributed traces]
    L --> O[Observability platform]
    M --> O
    T --> O
    O --> X[Dashboards, alerts, incident investigation]
```

## 2.1 Logs --- kya hua?

Logs individual events ka record hain.

Example:

``` json
{
  "Timestamp": "2026-10-12T12:15:20Z",
  "Level": "Error",
  "Service": "Sales.Api",
  "TraceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "OrderId": "ORD-123",
  "Event": "PaymentAuthorizationFailed",
  "ErrorCode": "PAYMENT_TIMEOUT"
}
```

Logs detail dete hain, lekin bahut high-volume logs expensive ho sakte
hain. Har successful request ke liye verbose debug logs permanently
store karna zaroori nahi.

## 2.2 Metrics --- system ka overall behaviour kaisa hai?

Metrics numeric time-series data hain, jaise:

-   Request rate: 250 requests/second.
-   Error rate: 2.3%.
-   Latency: p50, p95, p99.
-   CPU/memory utilization.
-   Queue depth and message age.
-   Database connection pool saturation.

Metrics dashboards aur alerting ke liye generally efficient hote hain.
High-cardinality labels---jaise har individual `OrderId` ko metric label
banana---avoid karein.

## 2.3 Traces --- request ne kahan-kahan travel kiya?

Distributed trace ek logical request ka end-to-end journey dikhata hai.
Har operation ko span represent karta hai.

``` mermaid
sequenceDiagram
    participant C as Client
    participant G as API Gateway
    participant S as Sales API
    participant I as Inventory
    participant P as Payment
    participant DB as Sales DB

    C->>G: POST /orders
    G->>S: Forward request
    S->>I: Reserve stock
    I-->>S: 200 OK
    S->>P: Authorize payment
    Note over P: 2.4 seconds delay
    P-->>S: Timeout
    S->>DB: Save failed attempt
    DB-->>S: Commit
    S-->>G: 504 Gateway Timeout
    G-->>C: 504 Gateway Timeout
```

Trace se pata lag sakta hai ki request ka most expensive span payment
dependency tha. Logs batayenge ki timeout ke waqt payment client ne kya
error record kiya.

**Interview line:** Logs explain individual events, metrics reveal
patterns and trends, and traces show the path and latency breakdown of a
distributed request.

------------------------------------------------------------------------

# 3. Structured Logging in .NET 8

## 3.1 Structured logging kyun?

String interpolation:

``` csharp
logger.LogInformation($"Order {orderId} created for dealer {dealerId}");
```

Isse final text milta hai, lekin structured properties ka benefit
logging provider ke hisaab se lose ho sakta hai.

Preferred:

``` csharp
logger.LogInformation(
    "Order {OrderId} created for dealer {DealerId}",
    orderId,
    dealerId);
```

Properties `OrderId` aur `DealerId` alag fields ke roop mein searchable
ho sakti hain.

## 3.2 Correlated log scope

``` csharp
using System.Diagnostics;

public sealed class OrderService(
    ILogger<OrderService> logger)
{
    public async Task CreateOrderAsync(
        string orderId,
        string dealerId,
        CancellationToken cancellationToken)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["OrderId"] = orderId,
            ["DealerId"] = dealerId
        });

        logger.LogInformation("Order creation started");

        // Business operation goes here.
        await Task.Delay(10, cancellationToken);

        logger.LogInformation("Order creation completed");
    }
}
```

Logging provider aur configuration ke hisaab se scope properties log
records mein include hoti hain. Production setup mein structured JSON
output enable karein.

## 3.3 Log levels

  Level         Use case
  ------------- -----------------------------------------------------
  Trace         Extremely detailed diagnostics; usually temporary
  Debug         Developer troubleshooting
  Information   Meaningful business/system lifecycle events
  Warning       Unexpected condition, but request may still succeed
  Error         Operation failed and needs investigation
  Critical      Application/system is in a severe state

Rules of thumb:

-   Expected validation failure ko automatically `Error` mat banayein;
    context ke hisaab se `Information` ya `Warning` ho sakta hai.
-   Har exception ko multiple layers par repeatedly log na karein.
    Logging ownership clear rakhein.
-   Exception object pass karein, sirf `exception.Message` nahi.
-   Passwords, access tokens, secrets, payment data aur unnecessary
    personal information log na karein.
-   Logs mein raw request/response bodies blindly capture na karein.
-   Retention, access control, redaction aur encryption define karein.

## 3.4 Request context aur correlation

ASP.NET Core ka `Activity` infrastructure W3C trace context use kar
sakta hai. Logging provider configured ho to `TraceId`/`SpanId` logs ke
saath enrich ho sakte hain. Verify karein ki deployed
provider/configuration mein ye fields actually emit ho rahi hain.

``` csharp
using System.Diagnostics;

app.MapGet("/diagnostics", (ILogger<Program> logger) =>
{
    var activity = Activity.Current;

    logger.LogInformation(
        "Diagnostic request received. TraceId={TraceId}, SpanId={SpanId}",
        activity?.TraceId.ToString(),
        activity?.SpanId.ToString());

    return Results.Ok(new
    {
        traceId = activity?.TraceId.ToString()
    });
});
```

Client ko internal exception details ya sensitive trace metadata expose
na karein. Trace ID expose karna acceptable ho sakta hai, lekin policy
aur threat model ke hisaab se decide karein.

------------------------------------------------------------------------

# 4. Metrics: RED and USE

## 4.1 RED method --- request-driven services

RED ka use HTTP APIs aur request/response services ke liye hota hai.

-   **Rate:** Kitni requests aa rahi hain?
-   **Errors:** Kitni requests fail ho rahi hain?
-   **Duration:** Requests kitna time le rahi hain?

Example dashboard:

  Metric             Example Interpretation
  -------------- ----------- --------------------------------
  Request rate     300 req/s Current load
  Error rate            4.5% Failure level
  p50 latency          80 ms Typical request
  p95 latency         900 ms Slow tail
  p99 latency          2.8 s Slowest-1% threshold

Average latency akela misleading ho sakta hai. 99 requests 50 ms mein
aur 1 request 5 seconds mein complete ho, to average normal lag sakta
hai while kuch users ko serious delay milega. Percentiles aur
distribution dekhein.

## 4.2 USE method --- infrastructure/resources

USE ka focus resources par hai:

-   **Utilization:** Resource kitna busy hai?
-   **Saturation:** Resource ke liye kitni queue/waiting hai?
-   **Errors:** Resource-related failures kitni hain?

Examples:

  -----------------------------------------------------------------------
  Resource          Utilization       Saturation        Errors
  ----------------- ----------------- ----------------- -----------------
  CPU               CPU percentage    Run queue /       Throttling events
                                      throttling        

  Database pool     Active            Waiters / pool    Connection
                    connections       exhaustion        failures

  SQS consumer      Consumer          Queue depth and   Processing
                    throughput        oldest-message    failures
                                      age               

  ECS tasks         CPU/memory        Pending tasks /   Task restarts
                                      scaling lag       
  -----------------------------------------------------------------------

**Important:** 90% CPU apne aap incident prove nahi karta. Agar latency,
errors aur saturation normal hain, high CPU expected workload bhi ho
sakta hai. User impact aur capacity signals ko combine karein.

## 4.3 Business metrics bhi monitor karein

Technical health ke saath business outcomes track karein:

-   Orders created per minute.
-   Successful vehicle transfer/compliance workflows.
-   Payment authorization success rate.
-   Dealer-specific transaction failures.
-   Orders stuck in `Pending`.
-   Events published vs consumed.
-   Time from order submission to completion.

Kabhi-kabhi API 200 return karti hai, lekin business workflow queue mein
stuck rehta hai. Business metrics aisi hidden failures expose karte
hain.

## 4.4 Cardinality pitfalls

Metric labels ko bounded rakhein.

**Avoid:** `http_requests_total{order_id="ORD-123456"}`

Har order ke liye new time series create ho sakti hai. Isse metrics
backend ka cost aur memory bahut badh sakta hai.

**Better labels:** `service`, `route_template`, `method`, `status_code`,
`region`.

Individual `OrderId`, `DealerId`, user ID ya trace ID ko logs/traces
mein rakhein, metric labels mein generally nahi. High-cardinality
business dimensions ki zaroorat ho to deliberate cost and aggregation
design karein.

------------------------------------------------------------------------

# 5. Distributed Tracing: Trace, Span, Context

## 5.1 Core concepts

-   **Trace:** End-to-end request journey.
-   **Span:** Ek operation, jaise `POST /orders`, `ReserveInventory`,
    SQL query ya HTTP call.
-   **Parent span:** Calling operation.
-   **Span attributes:** Operation ke useful metadata.
-   **Span events:** Span ke andar time-stamped events.
-   **Trace context:** Identifiers jo services ke beech propagate hote
    hain.

W3C Trace Context mein commonly `traceparent` aur optional `tracestate`
headers use hote hain. Proper instrumentation configured ho to .NET
`HttpClient` outgoing calls ke saath trace context propagate kar sakta
hai.

## 5.2 Trace ID vs correlation ID

Dono related hain, but identical concepts nahi:

-   `TraceId`: Distributed tracing system ka trace identify karta hai.
-   `CorrelationId`: Application/business request ko correlate karne ke
    liye custom identifier ho sakta hai.
-   `OrderId`: Business entity identifier.
-   `SpanId`: Trace ke ek specific operation ko identify karta hai.

Ek request ke liye same trace ID across services useful hai. Custom
correlation ID ki zaroorat ho sakti hai for business workflows, but use
validated aur consistently propagate karein. Incoming user-provided IDs
ko blindly trusted security identity na samjhein.

## 5.3 HTTP propagation

Typical journey:

1.  Gateway request receive karta hai.
2.  Gateway trace create/continue karta hai.
3.  Sales API downstream HTTP request bhejti hai.
4.  `HttpClient` trace context propagate karta hai, if
    tracing/instrumentation configured hai.
5.  Inventory API child span record karti hai.
6.  Trace backend spans ko same trace mein group karta hai.

Reverse proxy ya gateway agar headers strip/replace kare, trace broken
ho sakta hai. External callers se incoming trace headers accept karne ke
trust/sampling rules bhi define karein.

## 5.4 Async messaging propagation

Queue-based flow mein HTTP call complete hone ke baad processing
continue hoti hai. Producer ko trace context message attributes/headers
mein propagate karna chahiye; consumer ko context extract karke
processing span create karna chahiye.

``` mermaid
flowchart LR
    A[Sales API span] --> B[Publish OrderCreated]
    B --> C[SQS message with trace context]
    C --> D[Inventory consumer span]
    D --> E[Inventory DB span]
```

AWS SQS, SNS aur other messaging systems ke liye exact propagation
implementation library/instrumentation configuration par depend karega.
Sirf payload mein custom `TraceId` field daal dena full distributed
tracing ke barabar nahi hota.

------------------------------------------------------------------------

# 6. OpenTelemetry in ASP.NET Core (.NET 8)

OpenTelemetry vendor-neutral instrumentation, collection aur export ke
liye widely used standard hai. Typical architecture:

``` mermaid
flowchart LR
    A[ASP.NET Core service] --> B[OpenTelemetry SDK]
    B --> C[OTel Collector]
    C --> D[Trace backend]
    C --> E[Metrics backend]
    C --> F[Log backend]
    D --> G[Dashboards and alerting]
    E --> G
    F --> G
```

Collector optional ho sakta hai, lekin centralized processing, batching,
filtering aur backend routing ke liye useful hai.

## 6.1 Packages

Project mein appropriate stable versions ke packages add karein, aligned
with your .NET/OpenTelemetry dependency policy. Typical packages:

-   `OpenTelemetry.Extensions.Hosting`
-   `OpenTelemetry.Instrumentation.AspNetCore`
-   `OpenTelemetry.Instrumentation.Http`
-   An exporter matching the destination, e.g. OTLP exporter.

Package versions aur exporter support deployment environment ke saath
verify karein.

## 6.2 Example `Program.cs`

``` csharp
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var serviceName = builder.Environment.ApplicationName;

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: serviceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        // Configure an exporter for your selected backend:
        // .AddOtlpExporter()
        )
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        // Add runtime or custom meters when required.
        // Configure a metrics exporter for your backend.
        );

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { status = "ok" }));
app.MapHealthChecks("/health");

app.Run();
```

Yeh instrumentation ka illustrative starting point hai. Production mein
exporter endpoint, authentication, sampling, resource attributes
(`service.version`, environment), batching, shutdown flush, and secure
transport configure karein. Default setup ko blindly production-ready
assume na karein.

## 6.3 Custom Activity

``` csharp
using System.Diagnostics;

public static class Telemetry
{
    public static readonly ActivitySource Source =
        new("Dms.Sales");

    public static Activity? StartOrderProcessing(string orderId)
    {
        var activity = Source.StartActivity("ProcessOrder");

        activity?.SetTag("order.type", "vehicle-sale");
        // Avoid adding sensitive values as span attributes.
        activity?.SetTag("order.id", orderId);

        return activity;
    }
}
```

Custom `ActivitySource` ko tracing configuration mein register karein,
e.g. `tracing.AddSource("Dms.Sales")`; warna custom spans expected
exporter tak nahi pahunch sakte.

## 6.4 Sampling

Har request ka full trace retain karna high-traffic systems mein costly
ho sakta hai.

Common approaches:

-   **Head sampling:** Request start par sampling decision.
-   **Tail sampling:** Trace ke collected data ko dekh kar decision,
    typically collector/backend side.
-   **Always-on:** Low traffic/dev environments mein useful, production
    cost consider karein.
-   **Error/slow trace preference:** Backend/collector capabilities ke
    hisaab se error ya slow traces retain karne ki policy.

Sampling ka trade-off: cost kam hoti hai, but low-frequency issues miss
ho sakte hain. Critical transactions, compliance needs aur incident
investigation requirements consider karein.

------------------------------------------------------------------------

# 7. AWS CloudWatch and X-Ray

AWS-centric environment mein services ka selection architecture, team
skill aur cost model par depend karega.

## 7.1 CloudWatch

Common uses:

-   Application/service logs.
-   Infrastructure and application metrics.
-   Dashboards and alarms.
-   ECS/Lambda and other AWS resource telemetry.
-   Queue depth, errors, duration, throttles.
-   Log Insights-style querying for investigations.

Useful metrics examples:

-   Lambda `Errors`, `Duration`, `Throttles`.
-   SQS `ApproximateNumberOfMessagesVisible` and age-related metrics.
-   ECS/container CPU and memory metrics.
-   API Gateway 4xx/5xx and latency.
-   Database connection count, CPU, storage and latency signals.

Exact metric names and availability service/configuration ke hisaab se
verify karein.

## 7.2 AWS X-Ray and OpenTelemetry

AWS X-Ray *service* distributed trace analysis ke liye abhi bhi
supported hai, lekin X-Ray SDKs aur X-Ray Daemon 25 Feb 2026 se
maintenance mode mein hain (sirf security fixes; end of support
Feb 2027 ke aas-paas). AWS ab OpenTelemetry (ADOT / OTel SDK + collector)
ko primary instrumentation path recommend karta hai. Isliye naye
services mein OTel se instrument karein aur X-Ray ko trace *backend* ke
roop mein use karein; purane X-Ray SDK par bane services ke liye
migration plan rakhein. Deployment se pehle current AWS guidance verify
karein.

## 7.3 Practical selection

  ---------------------------------------------------------------------
  Requirement                        Possible option
  ---------------------------------- ----------------------------------
  AWS infrastructure metrics and     CloudWatch
  alarms                             

  AWS-centric distributed trace      OpenTelemetry (ADOT) + X-Ray backend
  analysis                           

  Vendor-neutral instrumentation     OpenTelemetry

  Metrics dashboards                 CloudWatch dashboards or
                                     Prometheus/Grafana stack

  Centralized searchable logs        CloudWatch Logs or another
                                     approved log platform
  ---------------------------------------------------------------------

Ek saath bahut tools introduce na karein. Pehle define karein: kaunse
signals, kaunse SLOs, retention, access control, alert owner aur
estimated telemetry cost.

------------------------------------------------------------------------

# 8. Health Checks: Liveness, Readiness, Startup

## 8.1 Liveness

Question: **"Kya process alive hai aur restart karna chahiye?"**

Liveness endpoint ko normally external dependency failures par unhealthy
nahi banana chahiye. Agar database temporarily down hai aur liveness
false return karta hai, orchestrator saare app instances restart kar
sakta hai---recovery aur difficult ho sakti hai.

## 8.2 Readiness

Question: **"Kya yeh instance abhi traffic receive karne ke liye ready
hai?"**

Readiness check dependency availability, initialization, configuration
loading, or application state consider kar sakta hai. Lekin har
downstream outage par every instance ko unready kar dena traffic ko
completely block bhi kar sakta hai. Dependency check design carefully
karein.

## 8.3 Startup

Question: **"Kya app ko initialize hone ke liye extra time chahiye?"**

Large initialization, cache warm-up, migrations or configuration loading
ke cases mein startup probe useful ho sakta hai. Production database
migrations ko every replica startup par race-prone tareeke se run karna
avoid karein.

``` mermaid
flowchart TD
    A[Container starts] --> B{Startup check passes?}
    B -- No --> C[Allow startup grace period / restart by policy]
    B -- Yes --> D[Liveness check]
    D -- Fails --> E[Restart process according to orchestrator policy]
    D -- Passes --> F[Readiness check]
    F -- Not ready --> G[Keep instance out of traffic]
    F -- Ready --> H[Serve traffic]
```

## 8.4 ASP.NET Core sample

``` csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
// Register dependency checks deliberately, for example a DB check
// using the appropriate provider and package.

var app = builder.Build();

app.MapHealthChecks("/health/live");

// Keep readiness checks separate from liveness checks.
// Add only checks that represent whether this instance should receive traffic.
app.MapHealthChecks("/health/ready");

app.Run();
```

Sample mein dono endpoints same checks use kar rahe hain, isliye
production mein separate registrations/tags/predicates define karna
better hoga. Illustrative setup ko directly copy karke
readiness/liveness semantics identical na rakhein.

------------------------------------------------------------------------

# 9. Resilience: Timeout, Retry, Circuit Breaker, Bulkhead

Distributed systems mein failures normal hain. Resilience ka goal har
failure ko hide karna nahi; bounded impact aur predictable recovery
ensure karna hai.

## 9.1 Timeout

Har network operation ke paas bounded timeout hona chahiye. Agar caller
ka total deadline 2 seconds hai, to har downstream call ko independent
2-second timeout dena total latency ko 2 seconds se kaafi zyada kar
sakta hai.

Example budget:

-   Gateway/client budget: 2.0 s
-   Sales API internal work: 0.2 s
-   Inventory call: 0.5 s
-   Payment call: 0.8 s
-   Remaining time: serialization, network and safety margin

Yeh numbers example hain; actual values observed latency and business
requirement se derive karein.

## 9.2 Retry with exponential backoff and jitter

Transient errors ke liye limited retries useful hain.

-   Exponential backoff: retry delay progressively increase.
-   Jitter: clients ke retries ko randomize karna, so all clients same
    time par retry na karein.
-   Maximum attempts and overall deadline define karein.
-   Sirf transient/retryable errors retry karein.
-   Non-idempotent operation ko blindly retry na karein.

Agar 100 clients simultaneously 3 retries karte hain, struggling
dependency par traffic multiple times increase ho sakta hai. Retry storm
avoid karein.

## 9.3 Circuit breaker

Circuit breaker dependency failure ke baad temporarily calls block karta
hai.

``` mermaid
stateDiagram-v2
    [*] --> Closed
    Closed --> Open: Failure threshold reached
    Open --> HalfOpen: Cooldown expires
    HalfOpen --> Closed: Probe succeeds
    HalfOpen --> Open: Probe fails
```

-   **Closed:** Requests flow normally.
-   **Open:** Calls fail fast or use fallback.
-   **Half-open:** Limited test requests determine whether dependency
    recovered.

Circuit breaker ka purpose failing dependency ko recover karne ka chance
dena aur caller resources protect karna hai.

## 9.4 Bulkhead

Bulkhead resources isolate karta hai. Example: payment calls ke liye
separate concurrency limit, so slow payment provider poore application
ke worker capacity ko exhaust na kare.

## 9.5 Graceful degradation

Example:

-   Inventory search recommendations unavailable: cached/basic results
    return.
-   Optional analytics service down: order creation continue, analytics
    event queue mein publish ho.
-   Payment authorization unavailable: payment ko "successful" mark na
    karein; operation ko explicit pending/retry/manual-review state mein
    rakhein.

Fallback business correctness compromise nahi karna chahiye.

------------------------------------------------------------------------

# 10. SLI, SLO, SLA and Error Budgets

## 10.1 Definitions

-   **SLI (Service Level Indicator):** Measured signal, e.g. successful
    requests / eligible requests.
-   **SLO (Service Level Objective):** Internal reliability target,
    e.g. 99.9% eligible requests successful over 30 days.
-   **SLA (Service Level Agreement):** Customer-facing contractual
    commitment, possibly with credits or remedies.
-   **Error budget:** SLO ke target ke andar allowed unreliability.

SLO and SLA ko interchangeable mat samjhein. SLA legal/commercial
commitment ho sakta hai; SLO engineering target.

## 10.2 Example

Suppose service ka monthly SLO 99.9% successful eligible requests hai.
Simplified request-based budget:

-   Total eligible requests: 10,000,000
-   Allowed unsuccessful requests: 0.1% = 10,000
-   Actual failures: 3,000
-   Budget consumed: 30%

Yeh request-based example hai. Availability-based SLOs time
windows/minutes par calculate ho sakte hain; correct denominator and
exclusions explicitly define karein.

Latency SLO example:

-   99% eligible `GET /vehicles` requests under 300 ms over rolling 28
    days.
-   Measurement definition mein timeouts, cancelled requests, internal
    errors and traffic exclusions ko clearly document karein.

## 10.3 Error budget ka use

Error budget consume ho raha hai to team:

-   High-risk releases pause/reduce kar sakti hai.
-   Reliability work prioritize kar sakti hai.
-   Capacity and dependency issues investigate kar sakti hai.
-   Rollback and deployment guardrails strengthen kar sakti hai.

SLO ka purpose teams ko punish karna nahi; delivery speed aur
reliability ke beech informed decisions enable karna hai.

------------------------------------------------------------------------

# 11. Alerting That Helps Instead of Creating Noise

Good alert user impact ya imminent reliability risk identify karta hai,
clear owner rakhta hai, aur action demand karta hai.

## 11.1 Good alert characteristics

-   Actionable: engineer ko next step samajh aaye.
-   Symptom-oriented: user-facing error/latency or business workflow
    delay.
-   Low noise: transient single data point par paging nahi.
-   Contextual: dashboard/runbook/owner included.
-   Correct severity: page only when immediate response needed.

## 11.2 Example alert candidates

-   Sales API 5xx rate sustained threshold se upar.
-   p95 latency SLO breach with enough traffic.
-   Payment authorization success rate drops sharply.
-   SQS oldest-message age indicates SLA risk.
-   Consumer failures/DLQ growth beyond expected range.
-   Database connection pool saturation plus request latency increase.
-   SLO error-budget burn rate unexpectedly high.

CPU 80% ko automatically paging alert na banayein unless it correlates
with saturation or impact. Warning vs page policy service criticality ke
hisaab se define karein.

------------------------------------------------------------------------

# 12. Production Incident: Step-by-Step Investigation

## Scenario

Dealership Management System (DMS) mein dealers report karte hain ki
order creation slow hai. Sales API p95 latency 300 ms se 2.5 seconds ho
gayi, aur kuch requests 504 return kar rahi hain.

### Step 1: Impact scope establish karein

-   Sab dealers affected hain ya specific dealer/region?
-   One endpoint or all endpoints?
-   Start time, error rate, affected transaction count.
-   Is this a business-critical workflow?
-   Koi deployment/configuration/traffic change hua?

### Step 2: Metrics se locate karein

-   Sales API RED: request rate, error rate, p95/p99.
-   Database USE: connection pool, query latency, CPU, lock waits.
-   SQS: queue depth and oldest-message age.
-   Inventory/payment dependency latency.
-   ECS task restarts, CPU, memory and scaling lag.

### Step 3: Distributed trace inspect karein

Slow traces filter karein. Span durations compare karein:

-   Gateway → Sales API: 30 ms
-   Sales API internal: 100 ms
-   Inventory call: 150 ms
-   Payment call: 2.0 s
-   SQL save: 100 ms

Agar payment span dominant hai, to database tuning se start karna likely
wrong direction hoga.

### Step 4: Correlated logs search karein

Trace ID se related logs search karein:

-   Payment timeout?
-   Retry attempts?
-   Provider status code?
-   Circuit breaker state?
-   Dealer-specific configuration?
-   Exception stack and dependency response category?

Sensitive request/response body ko logs mein dump na karein.

### Step 5: Recent changes and dependency health

-   Recent deployment or configuration rollout?
-   External provider incident?
-   Certificate/token expiry?
-   DNS/network changes?
-   Retry count or timeout changed?
-   Traffic spike or one dealer causing abnormal load?

### Step 6: Mitigate safely

Potential mitigations:

-   Disable/rollback problematic release.
-   Reduce concurrency to protect a failing dependency.
-   Use circuit breaker and a valid business fallback.
-   Route traffic away from unhealthy instances.
-   Pause non-critical background work if it is starving critical
    processing.
-   Queue asynchronous work only if the business workflow can safely
    support it.

Payment ko fake success mark karna safe mitigation nahi hai.

### Step 7: Verify recovery

-   p95/p99 back within target?
-   Error rate and business completion rate recovered?
-   Queue backlog draining?
-   DLQ growth stopped?
-   No duplicate orders or payments?
-   Dealer workflows actually completing?

### Step 8: Blameless postmortem

Document:

-   Customer/business impact.
-   Timeline.
-   Detection gap.
-   Technical root cause and contributing factors.
-   Why safeguards did not prevent or limit impact.
-   Action items with owners and deadlines.
-   SLO/alert/runbook updates.

Root cause sirf "developer mistake" nahi hona chahiye. Systemic
contributors---insufficient timeout budget, missing alert, unsafe retry
policy, or lack of load testing---identify karein.

------------------------------------------------------------------------

# 13. DMS Observability Architecture

``` mermaid
flowchart TD
    U[Dealer UI] --> G[API Gateway / BFF]
    G --> S[Sales API on ECS]
    S --> I[Inventory Service]
    S --> P[Payment Provider]
    S --> DB[(Sales DB)]
    S --> Q[SQS]
    Q --> W[Background Worker / Lambda]
    W --> D[(DynamoDB or service DB)]

    S -. Logs, metrics, traces .-> O[OpenTelemetry instrumentation]
    I -. Logs, metrics, traces .-> O
    W -. Logs, metrics, traces .-> O
    O --> C[Collector / exporter]
    C --> CW[CloudWatch logs and metrics]
    C --> X[Trace backend, e.g. AWS X-Ray via OTel]
    CW --> A[Dashboards, alerts, runbooks]
    X --> A
```

## Design checklist

-   Every service has a stable `service.name`, version and environment.
-   Trace context propagates through HTTP and asynchronous messaging.
-   Structured logs contain trace ID and useful business identifiers.
-   Metrics use bounded-cardinality labels.
-   Health checks have correct liveness/readiness semantics.
-   Critical workflows have business-level SLIs.
-   Alerts map to owners and runbooks.
-   Telemetry is secured, redacted, retained and budgeted.
-   Sampling policy balances cost and incident visibility.
-   Deployment markers appear on dashboards so regressions correlate
    with releases.

------------------------------------------------------------------------

# 14. Interview Questions and Answers

## Q1. Observability aur monitoring mein kya difference hai?

**30-second answer:** Monitoring known conditions detect karta hai;
observability logs, metrics aur traces se internal system behaviour
diagnose karne mein help karti hai. Production mein dono
chahiye---monitoring alert raise karta hai, observability root cause
investigate karne mein help karti hai.

**Follow-up:** Unknown issue kaise find karoge?\
Metrics mein anomaly dekho, traces se slow/failing dependency locate
karo, aur trace ID se structured logs correlate karo.

## Q2. Logs, metrics aur traces ka use kab karte ho?

-   Logs: specific event/error context.
-   Metrics: trends, rates, percentiles, alerts.
-   Traces: cross-service request path and latency breakdown.

Inko correlate karne se investigation fastest hoti hai.

## Q3. RED vs USE?

RED request-oriented services ke liye Rate, Errors, Duration. USE
infrastructure/resources ke liye Utilization, Saturation, Errors.
Microservice dashboard mein usually dono useful hain.

## Q4. Average latency enough kyun nahi hai?

Average tail latency hide kar sakta hai. p95/p99 show karte hain ki
slowest requests ka experience kaisa hai. Traffic volume aur latency
histogram/distribution bhi context dete hain.

## Q5. Trace ID aur correlation ID mein kya difference hai?

Trace ID distributed trace identify karta hai; correlation ID
application-defined identifier ho sakta hai. Dono same value use kar
sakte hain if policy permits, but concepts different hain. Business IDs
such as OrderId ko trace ID ka replacement na banayein.

## Q6. OpenTelemetry kyun use karoge?

Vendor-neutral instrumentation standard hai. App ko specific backend se
tightly couple kiye bina traces, metrics and supported logs
collect/export karne ka consistent approach deta hai. Actual benefit
instrumentation coverage, context propagation and exporter/collector
configuration par depend karta hai.

## Q7. Liveness aur readiness ka difference?

Liveness batata hai process ko restart karna chahiye ya nahi. Readiness
batata hai instance traffic serve karne ke liye ready hai ya nahi.
Downstream DB outage par liveness fail karna restart storm cause kar
sakta hai.

## Q8. Retry storm kaise prevent karoge?

Bounded retries, exponential backoff, jitter, deadlines, retryable-error
classification, idempotency and circuit breakers. Retry budget and
concurrency limit bhi define karunga.

## Q9. Circuit breaker aur retry mein difference?

Retry same operation ko transient failure ke baad dobara attempt karta
hai. Circuit breaker repeated dependency failures par calls temporarily
stop karta hai, so caller resources aur failing dependency protected
rahen.

## Q10. SLI, SLO, SLA kya hain?

SLI measured reliability signal hai; SLO internal target; SLA
customer-facing agreement. Error budget SLO target ke andar allowed
unreliability quantify karta hai.

## Q11. High CPU hai, kya alert page karna chahiye?

Sirf CPU percentage se nahi. Check request latency/error rate,
saturation, throttling and capacity trend. Page tab karein jab user
impact ya imminent reliability risk actionable ho.

## Q12. Queue backlog kaise diagnose karoge?

Queue depth ke saath oldest-message age, producer rate, consumer
throughput, processing latency, failure/retry counts, DLQ and consumer
capacity dekho. Depth alone workload ke context ke bina misleading ho
sakti hai.

## Q13. Production mein trace missing kyun ho sakta hai?

Instrumentation absent, sampling, context headers lost, unsupported
message propagation, collector/exporter misconfiguration, or backend
ingestion failure. End-to-end test and trace/metric/log pipeline health
verify karunga.

## Q14. Telemetry cost kaise control karoge?

High-cardinality labels avoid, debug logs sampling/retention control,
traces sampling, noisy spans filter, duplicate telemetry reduce,
retention tiers use, and cost dashboards monitor.
Security/compliance-required data ko cost ke naam par blindly drop nahi
karunga.

## Q15. Incident ke baad kya deliverables honge?

Blameless postmortem, impact timeline, root cause and contributing
factors, customer communication if required, owners/deadlines for
corrective actions, updated alerts/runbooks, and tests/guardrails to
prevent recurrence.

------------------------------------------------------------------------

# 15. Scenario-Based Interview Practice

## Scenario A: p95 latency increases, but error rate remains low

**Approach:** 1. Traffic and endpoint distribution check. 2. Trace
latency breakdown. 3. Dependency latency and database waits. 4. Resource
saturation and queueing. 5. Recent deployments/config changes. 6. Decide
mitigation based on dominant bottleneck.

Low errors ka matlab system healthy zaroori nahi; users timeout hone se
pehle slow responses face kar sakte hain.

## Scenario B: API healthy, but orders are stuck in Pending

**Approach:** - Business completion metric and state-age distribution. -
SQS queue depth and oldest-message age. - Consumer concurrency, errors,
DLQ, poison messages. - Outbox publish lag and event schema
compatibility. - Idempotency/duplicate handling. - Reprocess safely
after fixing cause.

HTTP 200 sirf request acceptance prove karta hai, end-to-end business
completion nahi.

## Scenario C: Every service shows elevated latency simultaneously

**Approach:** - Shared dependencies: database, DNS, network, identity
provider, message broker. - Traffic surge, connection pool saturation,
noisy neighbour. - Shared config or deployment rollout. - Resource and
dependency metrics. - Mitigate at shared bottleneck rather than
restarting every service without evidence.

## Scenario D: Errors rise immediately after release

**Approach:** - Deployment markers and version dimensions. - Compare new
vs previous version metrics. - Trace/log exception patterns. - Rollback
if safe and impact warrants it. - Validate data/schema backward
compatibility. - Preserve evidence and follow up with automated
regression tests.

------------------------------------------------------------------------

# 16. Practical Revision Checklist

Before a senior/staff/architect interview, be ready to explain:

-   [ ] Logs, metrics, traces and their correlation.
-   [ ] RED and USE with service-specific examples.
-   [ ] p50/p95/p99 and why averages can mislead.
-   [ ] Structured logging with `ILogger<T>` and scopes.
-   [ ] W3C Trace Context and async message propagation.
-   [ ] OpenTelemetry instrumentation, exporter and sampling.
-   [ ] CloudWatch and tracing-backend responsibilities.
-   [ ] Liveness vs readiness vs startup.
-   [ ] Timeout budgets, retries, jitter, circuit breakers and
    bulkheads.
-   [ ] Business-level SLIs and error budgets.
-   [ ] Alert quality, runbooks and incident response.
-   [ ] A realistic production incident with evidence, mitigation and
    postmortem.

## 60-second interview summary

"Microservices mein observability ko main architecture ka first-class
concern maanta hoon. Har service mein structured logs, RED metrics,
resource saturation metrics aur distributed tracing instrument karta
hoon. OpenTelemetry se vendor-neutral instrumentation aur context
propagation standardize kar sakte hain, while CloudWatch and an
appropriate tracing backend operational visibility provide karte hain.
Health checks mein liveness aur readiness ko separate rakhta hoon.
Reliability ke liye bounded timeouts, carefully classified retries with
jitter, circuit breakers, bulkheads and explicit SLOs use karta hoon.
Incident ke waqt pehle customer impact aur metrics establish karta hoon,
traces se bottleneck locate karta hoon, correlated logs se context
validate karta hoon, safely mitigate karta hoon, aur blameless
postmortem ke through systemic improvements track karta hoon."

------------------------------------------------------------------------

<a id="phase-6"></a>

# Phase 6 --- Data Migration and Cutover

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

  --------------------------------------------------------------------------
  Data/table    Current       Proposed owner  Cross-service   Migration risk
                writers                       consumers       
  ------------- ------------- --------------- --------------- --------------
  Orders        Sales API,    Sales           Inventory,      High
                batch job                     reporting       

  Vehicles      Inventory,    Inventory       Sales,          High
                Sales                         compliance      

  Payments      Payment       Payments        Sales, finance  High
                module                                        

  Dealer        Multiple      Dealer          All services    Medium
  settings      modules       Configuration                   
  --------------------------------------------------------------------------

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

------------------------------------------------------------------------

<a id="phase-7"></a>

# Phase 7 --- Deployment, CI/CD, and Infrastructure

## Phase 7: Deployment, CI/CD, Containers & ECS/Kubernetes Operations

**Audience:** Senior Software Engineer, Staff Engineer, Tech Lead,
Solution Architect\
**Technology context:** .NET 8, C#, Docker, GitHub Actions,
Terraform/OpenTofu (or AWS CDK), AWS ECS/Fargate, Kubernetes\
**Style:** Hinglish (Roman Hindi + English technical terminology)

------------------------------------------------------------------------

## Phase objective

Microservices ka benefit tabhi milta hai jab services independently
build, test, deploy, observe aur rollback ho sakein. Agar 20 services ko
ek saath release karna pade, to operationally humne distributed monolith
create kiya ho sakta hai.

Is phase mein aap cover karenge:

-   Containerization aur .NET 8 Docker best practices.
-   CI/CD pipeline design and release gates.
-   Rolling, blue/green and canary deployments.
-   AWS ECS/Fargate vs Kubernetes.
-   Configuration, secrets, feature flags and environment separation.
-   Database migration compatibility during releases.
-   Infrastructure as Code and deployment safety.
-   Rollback, roll-forward and production release scenarios.

------------------------------------------------------------------------

# 1. Independent deployability: Microservices ka practical test

A service independently deployable tab hai jab usko release karne ke
liye unrelated services ko same time deploy karna required na ho.

Iske liye:

-   API/event contracts backward-compatible hon.
-   Service apne data writes own kare.
-   Configuration and secrets independently manage hon.
-   Build/test/deploy pipeline per service ya clearly bounded deployable
    unit ho.
-   Health checks and observability release validation support karein.
-   Rollback/roll-forward strategy defined ho.

**Interview point:** Independent deployment ka matlab "har service ko
har waqt deploy karna" nahi. Matlab service ko unrelated service
releases ke bina safely deploy karne ki capability.

------------------------------------------------------------------------

# 2. Typical CI/CD pipeline

``` mermaid
flowchart TD
    A[Commit / Pull Request] --> B[Restore and build]
    B --> C[Unit tests]
    C --> D[Static analysis and dependency scan]
    D --> E[Integration / contract tests]
    E --> F[Build immutable container image]
    F --> G[Generate SBOM and scan image]
    G --> H[Publish image to registry]
    H --> I[Deploy to test environment]
    I --> J[Smoke tests and health checks]
    J --> K{Release gate passes?}
    K -- No --> L[Stop / notify / investigate]
    K -- Yes --> M[Canary or blue-green production release]
    M --> N[Observe metrics and business outcomes]
    N --> O{Healthy?}
    O -- Yes --> P[Complete release]
    O -- No --> Q[Rollback / halt rollout]
```

## Recommended pipeline stages

1.  **Fast checks:** formatting, build, unit tests.
2.  **Quality and security:** static analysis, dependency/container
    scan, secret scan.
3.  **Contract/integration tests:** API schemas, event compatibility,
    database integration.
4.  **Artifact build:** build once, tag immutable image by commit/build
    ID.
5.  **Artifact publish:** push to approved registry.
6.  **Deploy lower environment:** integration, smoke and migration
    checks.
7.  **Release approval/gates:** risk-based; avoid unnecessary manual
    bottlenecks.
8.  **Production rollout:** rolling, canary or blue/green depending on
    risk.
9.  **Post-deploy verification:** error rate, latency, business metrics,
    queue health.
10. **Automatic halt/rollback:** if agreed health gates fail.

Avoid rebuilding a different artifact for every environment. Promote the
same immutable image through environments; inject environment-specific
configuration at deployment time.

------------------------------------------------------------------------

# 3. Dockerizing a .NET 8 service

## 3.1 Multi-stage Dockerfile

Multi-stage build se build tools final runtime image se separate rehte
hain.

``` dockerfile
# Illustrative baseline. Pin approved base-image versions/digests in production.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore layer cache ke liye saare referenced .csproj (aur
# Directory.Build.props / Directory.Packages.props / nuget.config) pehle copy karein.
COPY ["Directory.Build.props", "Directory.Packages.props", "nuget.config", "./"]
COPY ["Sales.Api/Sales.Api.csproj", "Sales.Api/"]
COPY ["Sales.Domain/Sales.Domain.csproj", "Sales.Domain/"]
COPY ["Sales.Infrastructure/Sales.Infrastructure.csproj", "Sales.Infrastructure/"]
RUN dotnet restore "Sales.Api/Sales.Api.csproj"

COPY . .
WORKDIR /src/Sales.Api
RUN dotnet publish "Sales.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# Non-root user (.NET 8 images mein APP_UID defined hai); port 8080 non-root ke liye theek hai.
USER $APP_UID
ENTRYPOINT ["dotnet", "Sales.Api.dll"]
```

Adjust paths and project names for the repository layout. Production
pipelines should pin approved base image versions or digests and rebuild
regularly for security updates.

## 3.2 Best practices

-   Use multi-stage builds.
-   Use `.dockerignore` to exclude `.git`, `bin`, `obj`, local secrets
    and irrelevant files.
-   Do not bake credentials into image layers or build arguments.
-   Run as non-root where the base image/platform supports it.
-   Prefer minimal runtime images while ensuring required native
    dependencies are present.
-   Pin and regularly update base images.
-   Scan images and dependencies.
-   Use immutable image tags/digests.
-   Configure graceful shutdown and cancellation.
-   Keep logs on stdout/stderr for container log collection.
-   Keep application state outside the container; do not rely on local
    filesystem persistence.
-   Set CPU/memory requests and limits appropriately for the chosen
    runtime.
-   Use explicit health checks at the orchestrator/platform layer.

## 3.3 `.dockerignore` example

``` text
.git
.github
.vs
.vscode
**/bin/
**/obj/
**/TestResults/
.env
appsettings.Development.json
secrets/
```

Do not exclude files required for the build. Secret/config exclusions
should match your actual repo practices.

## 3.4 Configuration

Environment-specific values should be provided through approved
configuration mechanisms, not hard-coded into image.

In ASP.NET Core, environment variables can override configuration
values. Nested keys commonly use double underscores,
e.g. `ConnectionStrings__SalesDb`.

Secrets should come from an approved secret store or platform
integration. Do not commit production credentials or copy them into
Docker images.

------------------------------------------------------------------------

# 4. Container lifecycle and graceful shutdown

When ECS/Kubernetes stops a task/pod, the process may receive a
termination signal and have a limited grace period before forceful
termination.

The service should:

-   Stop accepting new work when appropriate.
-   Finish or safely cancel in-flight requests.
-   Stop queue polling and finish/abandon messages according to
    visibility/lease semantics.
-   Flush telemetry/logging where supported.
-   Avoid acknowledging a message before durable processing succeeds.
-   Respect cancellation tokens.
-   Use idempotency so interrupted work can be retried safely.

For ASP.NET Core, host shutdown tokens and request cancellation should
be propagated to I/O operations. A `CancellationToken` is not a
guarantee that every database/network operation can be cancelled
instantly; provider behaviour and timeout configuration matter.

------------------------------------------------------------------------

# 5. Rolling vs blue/green vs canary deployment

## 5.1 Rolling deployment

Instances are replaced in batches.

**Advantages** - Resource-efficient. - Often simple to operate. - No
full duplicate environment required.

**Risks** - Old and new versions coexist. - Schema/API/event contracts
must be compatible. - A bad version can affect some traffic before
detection. - Rollback may require another rollout and compatible
data/schema.

## 5.2 Blue/green deployment

Two environments/target groups are maintained:

-   Blue = current version.
-   Green = candidate version.

Test Green, then shift routing when gates pass. Keep Blue available
during the agreed rollback window.

**Advantages:** Fast traffic switch and clear environment separation.\
**Risks:** Extra capacity/cost; data and background workers may be
shared; switching traffic back does not reverse writes already made by
Green.

## 5.3 Canary deployment

Small traffic cohort first, then increase gradually.

Example rollout: - 1--5% canary - 10--25% - 50% - 100%

Percentages are illustrative; select cohort size and observation period
based on traffic volume, risk and SLOs.

``` mermaid
flowchart TD
    A[Build candidate release] --> B[Deploy candidate]
    B --> C[Smoke and compatibility tests]
    C --> D[Route small canary cohort]
    D --> E[Compare error rate, latency and business KPIs]
    E --> F{Gates pass?}
    F -- No --> G[Stop rollout and assess rollback]
    F -- Yes --> H[Increase traffic gradually]
    H --> I{Full rollout healthy?}
    I -- No --> G
    I -- Yes --> J[Complete deployment]
```

## 5.4 Strategy comparison

| Strategy | Resource cost | Detection speed | Rollback characteristics | Good fit |
|---|---|---|---|---|
| Rolling | Usually lower | Depends on batch/gates | Rollout/redeploy; mixed versions | Routine compatible releases |
| Blue/green | Higher during overlap | Can validate full candidate environment | Traffic switch may be quick; data rollback still complex | High-risk releases needing parallel environment |
| Canary | Variable | Good if metrics and traffic are sufficient | Halt/shift traffic back if data compatibility allows | Gradual exposure and SLO-based gates |

No strategy guarantees zero downtime automatically. Health checks, load
balancing, compatible contracts, capacity, connection draining and
database changes all matter.

------------------------------------------------------------------------

# 6. AWS ECS/Fargate vs Kubernetes

## 6.1 ECS/Fargate

-   AWS-native container orchestration.
-   Fargate manages much of the underlying compute infrastructure.
-   Integrates with IAM, CloudWatch, load balancers, service discovery
    and deployment controls.
-   Often lower operational overhead for teams committed to AWS.

## 6.2 Kubernetes

-   Broad ecosystem and portability across supported environments.
-   Rich primitives for scheduling, rollout, service discovery,
    configuration and extensibility.
-   Requires cluster/platform expertise unless using a managed offering.
-   Adds operational complexity: upgrades, policies, networking,
    observability and add-ons still need ownership.

## 6.3 Decision matrix

  -----------------------------------------------------------------------
  Consideration           ECS/Fargate             Kubernetes
  ----------------------- ----------------------- -----------------------
  AWS integration         Strong native           Available, but often
                          integration             requires platform
                                                  integration

  Cluster management      Fargate reduces         Managed control plane
                          infrastructure          helps, but
                          management              workload/platform
                                                  operations remain

  Portability             More AWS-oriented       Broader
                                                  ecosystem/portability

  Learning/operations     Often simpler for       More concepts and
                          AWS-focused teams       platform
                                                  responsibilities

  Ecosystem extensibility AWS service model       Extensive Kubernetes
                                                  ecosystem

  Best choice             Team wants AWS-native   Existing Kubernetes
                          operations with less    platform/skills or
                          cluster management      specific ecosystem
                                                  requirements
  -----------------------------------------------------------------------

**Interview answer:** "I would not choose Kubernetes just because the
system has microservices. I'd evaluate operational capability,
portability needs, deployment requirements, policy/networking
complexity, AWS integration and total cost. For an AWS-centric team
without Kubernetes platform expertise, ECS/Fargate can be a pragmatic
choice."

------------------------------------------------------------------------

# 7. Configuration, secrets and feature flags

## 7.1 Configuration principles

-   Separate deployable artifact from environment configuration.
-   Validate required configuration at startup.
-   Use typed options and validation for important settings.
-   Avoid secrets in source control, image layers, logs or plain-text
    deployment files.
-   Rotate credentials without rebuilding the application where the
    platform supports it.
-   Give each service least-privilege permissions.
-   Audit access to secrets and production configuration.

## 7.2 Feature flags

Feature flags separate deployment from feature activation.

Use cases: - Enable a new read path for a small dealer cohort. - Switch
from legacy inventory lookup to new service. - Disable an optional
non-critical feature during an incident. - Gradually roll out a
workflow.

Flags need ownership, expiry/review dates, auditability and safe
defaults. Old flags should be removed after rollout. A feature flag is
not a substitute for authorization or a security boundary.

## 7.3 Migration-specific feature flags

Example:

``` csharp
if (featureFlags.UseNewInventoryRead)
{
    return await newInventoryClient.GetVehicleAsync(vehicleId, cancellationToken);
}

return await legacyInventoryRepository.GetVehicleAsync(vehicleId, cancellationToken);
```

This is illustrative; actual flag provider and fallback rules depend on
the app. Ensure the two paths have compatible semantics, and do not
silently fall back to stale data for a business-critical operation
without a defined policy.

------------------------------------------------------------------------

# 8. Database changes during deployment

A rolling release can run old and new application versions
simultaneously. Therefore, database changes must preserve compatibility.

Safer order:

1.  Add new nullable column/table/index or other compatible structure.
2.  Deploy code that can tolerate both old and new structure.
3.  Backfill data separately.
4.  Switch reads/writes after validation.
5.  Observe and verify old version is retired.
6.  Remove obsolete structure in a later release.

Risky pattern:

1.  Rename/drop a column immediately.
2.  Deploy code expecting only new schema.
3.  Old instances continue to run and fail.

For large tables, inspect locks, transaction log growth, index
operations, deployment timeout and migration duration. Do not
automatically run EF Core migrations concurrently from every replica at
application startup.

------------------------------------------------------------------------

# 9. Deployment safety gates

A release should have measurable go/no-go criteria.

Potential gates:

-   Unit/integration/contract tests pass.
-   Image vulnerability policy passes or approved exceptions exist.
-   Service becomes ready.
-   Smoke tests pass.
-   Error rate remains within agreed threshold.
-   p95/p99 latency remains within target.
-   Business completion rate is healthy.
-   Queue lag and DLQ do not regress.
-   Database error/lock metrics remain healthy.
-   No significant increase in duplicate operations or data mismatches.

Use a suitable observation window. Very low traffic can make percentage
metrics noisy, so combine rate, counts, absolute error budgets and
business signals.

**Automatic rollback is not always safe.** If the candidate release has
made irreversible schema changes or business side effects, a blind
rollback may worsen data inconsistency. In those cases, halt rollout,
preserve evidence and follow the rehearsed recovery strategy.

------------------------------------------------------------------------

# 10. Infrastructure as Code and release governance

Terraform/OpenTofu, AWS CDK ya Pulumi jaise IaC tool infrastructure
changes ko application delivery ke saath version kar sakte hain.
Note: CDK for Terraform (CDKTF) ko HashiCorp ne Dec 2025 mein deprecate/
archive kar diya; naye projects mein use na karein. Existing CDKTF code
ko `cdktf synth --hcl` se plain Terraform HCL mein migrate kiya ja sakta
hai, ya OpenTofu/Pulumi/AWS CDK ki taraf move karein.

Good practices:

-   Review plans/diffs before apply.
-   Separate environments and permissions.
-   Use remote state with locking and restricted access where
    applicable.
-   Avoid secrets in state or outputs; state itself may contain
    sensitive values.
-   Apply least privilege to CI identities.
-   Pin provider/module versions under a controlled update process.
-   Detect drift and define whether/how it is reconciled.
-   Use separate approvals for high-risk production infrastructure
    changes.
-   Keep infrastructure and application rollout dependencies explicit.

CI/CD identity should receive only required deployment permissions.
Avoid long-lived static AWS access keys when an appropriate short-lived
role/OIDC integration is available.

------------------------------------------------------------------------

# 11. Deployment architecture for a .NET microservice on AWS

``` mermaid
flowchart TD
    DEV[Developer / Pull Request] --> CI[CI: Build, tests, scans]
    CI --> IMG[Immutable container image]
    IMG --> REG[Container registry]
    REG --> CD[Deployment workflow]
    CD --> ECS[ECS service / Fargate tasks]
    ECS --> ALB[Application Load Balancer]
    ALB --> U[Users / API Gateway]
    ECS --> SM[Secrets/config integration]
    ECS --> DB[(Service-owned database)]
    ECS --> Q[SQS / event broker]
    ECS --> TEL[Logs, metrics, traces]
    TEL --> CW[CloudWatch / trace backend]
    CW --> G[Release gates and alerts]
    G -->|Healthy| DONE[Complete rollout]
    G -->|Unhealthy| STOP[Halt / rollback or roll forward]
```

Architecture details depend on whether API Gateway is placed before ALB,
how the service is networked, and whether workloads use ECS or
Kubernetes. The diagram shows conceptual relationships, not a mandatory
AWS topology.

------------------------------------------------------------------------

# 12. Interview Questions and Answers

## Q1. Microservice independently deployable kab hai?

Jab usko release karne ke liye unrelated services ka synchronized
release required na ho. API/event contracts, schema compatibility,
configuration, tests and deployment pipeline independent release enable
karein.

## Q2. Rolling, blue/green and canary mein difference?

Rolling instances ko batches mein replace karta hai. Blue/green parallel
environments maintain karke routing switch karta hai. Canary candidate
ko small traffic cohort par expose karke metrics validate karta hai
before expanding.

## Q3. Docker multi-stage build kyun?

SDK/build tools ko build stage tak limit kar sakte hain; final image
mein published application and runtime hota hai. Isse image surface/size
reduce ho sakti hai and build/runtime concerns separate hote hain.

## Q4. Container mein application state local disk par kyun avoid karte hain?

Container replacement or rescheduling par local state lost ho sakti hai.
Durable business state database/object store mein aur transient shared
work approved distributed storage/queue mein rakhein.

## Q5. ECS/Fargate ya Kubernetes choose kaise karoge?

Operational maturity, portability, AWS integration, platform skills,
policy/networking needs, deployment model, ecosystem and total cost
evaluate karunga. Microservices hone se Kubernetes automatically
necessary nahi hota.

## Q6. Same image all environments mein promote kyun?

Artifact reproducibility improve hoti hai and environment-to-environment
build differences avoid hote hain. Configuration environment-specific
hoti hai, artifact immutable rehta hai.

## Q7. Deployment ke baad p95 latency badh gayi. Kya karoge?

Release marker ke against metrics compare, new vs old version
traces/logs inspect, business outcomes and dependency metrics validate.
Rollout halt/rollback only if safe; otherwise rehearsed roll-forward/fix
plan use karunga.

## Q8. Feature flag aur deployment mein difference?

Deployment code ko environment mein release karta hai; feature flag
capability ko activate/deactivate kar sakta hai without new binary
deployment. Flags need ownership and cleanup; authorization ke
replacement nahi hain.

## Q9. Database migration rolling deployment ko kaise affect karti hai?

Old/new versions simultaneously run kar sakte hain. Expand-and-contract,
backward-compatible schema, separate backfill and delayed destructive
cleanup use karunga.

## Q10. Graceful shutdown kyun important hai?

In-flight requests and queue messages safely complete/cancel ho sakein,
telemetry flush ho aur duplicate/lost work reduce ho. Shutdown deadline,
cancellation and idempotency design ka part hain.

## Q11. Automatic rollback hamesha safe hai?

Nahi. Data/schema changes and external side effects rollback ko unsafe
bana sakte hain. Release gates rollout halt kar sakte hain, but recovery
decision rehearsed rollback/roll-forward plan se hona chahiye.

## Q12. Pipeline mein kaunse tests include karoge?

Unit tests, integration tests, API/event contract tests, migration
compatibility tests, security/dependency/image scans, smoke tests and
post-deployment health/business checks. Test selection risk and release
time ke hisaab se layer ki jaati hai.

## Q13. Production secret CI pipeline mein kaise handle karoge?

Prefer short-lived federated identity/role, least privilege, secret
manager integration and masked logs. Static credentials or secrets baked
into image avoid karunga.

## Q14. Canary rollout mein 1% traffic enough hai?

Always nahi. Enough traffic and observation time chahiye to meaningful
errors/latency signal mil sake. Critical low-frequency business
operations ke liye synthetic checks and business metrics bhi use
karunga.

## Q15. IaC deployment mein drift detect hua. Kya karoge?

Current state and desired configuration compare, drift ka cause
identify, plan review and approved reconciliation. Production resources
ko blindly overwrite ya manual changes ignore nahi karunga.

------------------------------------------------------------------------

# 13. Scenario-based practice

## Scenario A: New release passes health check but order completion falls

Health endpoint only process/readiness verify kar sakta hai. Business
metric, queue consumer lag, event contract, DB errors and end-to-end
synthetic transaction check karein. Rollout gate ko business outcomes
include karna chahiye.

## Scenario B: New version and old version fail after schema change

Likely backward compatibility issue. Stop rollout, inspect migration
ordering and application version compatibility. If safe, restore
compatible schema/code state; otherwise roll forward with additive
schema and compatible code. Production recovery should follow tested
runbook.

## Scenario C: ECS tasks restart repeatedly

Inspect task stopped reason, container exit code, memory/CPU, startup
configuration, secret access, health-check grace period, dependency
startup and application logs. Restart count alone root cause nahi
batata.

## Scenario D: Canary error rate rises, but only for one dealer

Check tenant/dealer configuration, data shape, permissions, routing
cohort, feature flag and dealer-specific downstream dependencies. Ensure
dashboards allow safe segmentation without unbounded metric cardinality
or leaking sensitive data.

------------------------------------------------------------------------

# 14. Practical release checklist

### Before release

-   [ ] Tests and contract compatibility pass.
-   [ ] Immutable image built, scanned and published.
-   [ ] Configuration and secrets validated.
-   [ ] Database changes backward-compatible.
-   [ ] Migration/backfill plan and recovery steps reviewed.
-   [ ] Health checks, dashboards and alerts ready.
-   [ ] Rollout strategy and abort thresholds documented.

### During release

-   [ ] Deploy candidate using approved strategy.
-   [ ] Verify readiness and smoke tests.
-   [ ] Observe API RED metrics and resource saturation.
-   [ ] Observe business completion, queue lag and data errors.
-   [ ] Expand canary only when gates pass.
-   [ ] Halt rollout if impact crosses threshold.

### After release

-   [ ] Confirm all expected tasks/instances use the intended version.
-   [ ] Validate business workflow end to end.
-   [ ] Verify no increase in duplicates, DLQ or data mismatches.
-   [ ] Confirm alerts and telemetry are healthy.
-   [ ] Record deployment outcome and follow-up actions.
-   [ ] Remove obsolete feature flags and temporary permissions when
    safe.

------------------------------------------------------------------------

# 15. 60-second interview summary

"Microservices ka benefit independent deployability se aata hai, isliye
main har service ke liye automated build, tests, security scans,
immutable container artifacts and deployment gates design karta hoon.
.NET 8 apps ke liye multi-stage Docker build, secure configuration,
graceful shutdown and health checks use karta hoon. Release risk ke
basis par rolling, blue/green or canary choose karta hoon, and
API/event/database changes ko backward-compatible rakhta hoon.
AWS-centric platform mein ECS/Fargate often practical option ho sakta
hai; Kubernetes tab choose karunga jab portability, existing platform
capability or ecosystem requirements justify karein. Production rollout
ke baad technical metrics ke saath business outcomes monitor karta hoon.
Automatic rollback ko blindly trigger nahi karta---data changes and
irreversible side effects ke context mein rehearsed rollback or
roll-forward plan follow karta hoon."

------------------------------------------------------------------------

# 16. Final revision checklist

-   [ ] Independent deployability.
-   [ ] CI/CD stages and immutable artifact promotion.
-   [ ] Multi-stage Docker build and image security.
-   [ ] Graceful shutdown and stateless containers.
-   [ ] Rolling vs blue/green vs canary.
-   [ ] ECS/Fargate vs Kubernetes trade-offs.
-   [ ] Config, secrets and feature flags.
-   [ ] Expand-and-contract during rolling releases.
-   [ ] Deployment gates and business health signals.
-   [ ] IaC reviews, state protection and least privilege.
-   [ ] Rollback vs roll-forward.
-   [ ] A real release incident with investigation and mitigation.

------------------------------------------------------------------------

<a id="phase-8"></a>

# Phase 8 --- System Design and Architecture

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

  --------------------------------------------------------------------------
  Service               Owns                           Typical operations
  --------------------- ------------------------------ ---------------------
  Dealer/Identity       Dealer profile references,     Get dealer context,
                        role mapping                   manage roles

  Inventory             Vehicle availability and       Search, reserve,
                        reservation                    release

  Sales/Orders          Order lifecycle and order      Create order, query
                        state                          status

  Payments              Payment                        Authorize, capture,
                        authorization/capture/refund   refund
                        state                          

  Compliance            Compliance cases and decision  Validate, request
                        state                          documents

  Integration           External system adapters and   Import/export,
                        mapping                        transform, retry

  Notifications         Notification delivery state    Email/SMS
                                                       notifications

  Reporting             Read models and analytical     Dashboard and
                        views                          reporting queries
  --------------------------------------------------------------------------

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
    Pending --> Failed: Nothing to undo
    InventoryReserved --> PaymentPending
    InventoryReserved --> CompensationPending: Step failed
    PaymentPending --> PaymentAuthorized
    PaymentPending --> CompensationPending: Payment failed
    PaymentAuthorized --> CompliancePending
    CompliancePending --> Confirmed
    CompliancePending --> CompensationPending: Compliance rejected
    CompensationPending --> Cancelled: Payment voided, inventory released
    Confirmed --> Completed
```

Yeh diagram neeche wale "Workflow state model" ke saath consistent hai:
`Failed` sirf tab jab undo karne ko koi side effect nahi hai (e.g. Pending
validation reject); jahan side effects ho chuke hain wahan
`CompensationPending` se hoke `Cancelled` terminal state aati hai.

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

  -----------------------------------------------------------------------
  Model                 Advantages                Trade-offs
  --------------------- ------------------------- -----------------------
  Shared tables with    Lower cost, simpler       Strong
  `TenantId`            operations                query/authorization
                                                  discipline required

  Separate schema per   Some logical separation   Migration and
  tenant                                          operations complexity

  Separate database per Stronger                  Higher cost and fleet
  tenant                isolation/customization   management

  Hybrid                Tiered isolation for      More complexity and
                        different customers       support burden
  -----------------------------------------------------------------------

Choose based on regulatory needs, tenant count, data volume,
customization and recovery requirements.

------------------------------------------------------------------------

# 13. Capacity estimation

Assume for interview illustration: - 250,000 order-creation requests
per month, all during business hours. - Business hours: 10 AM--7 PM,
9 hours/day. - 22 working days/month. - Traffic is uneven; peak factor
assumed 5× average. - Each order request may generate several internal
calls/events (fan-out, estimated separately below).

Average business-hours request rate:

-   Total seconds in business hours = 22 × 9 × 3600 = 712,800 seconds.
-   Average rate = 250,000 / 712,800 ≈ 0.35 order requests/second.
-   At an illustrative 5× peak factor ≈ 1.75 order requests/second.
-   Agar har order ~10 internal calls/events generate kare (assumption),
    to peak internal load ≈ 17.5 calls/second.

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
    Pending --> Failed: Nothing to undo
    InventoryReserved --> PaymentPending
    InventoryReserved --> CompensationPending: Step failed
    PaymentPending --> PaymentAuthorized
    PaymentPending --> CompensationPending: Payment failed
    PaymentAuthorized --> CompliancePending
    CompliancePending --> Confirmed
    CompliancePending --> CompensationPending: Compliance rejected
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

<a id="phase-9"></a>

# Phase 9 --- Architecture Decisions and Mock Interviews

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

  ---------------------------------------------------------------------
  Requirement                        Likely approach
  ---------------------------------- ----------------------------------
  Update a single service-owned      Local ACID transaction
  entity atomically                  

  DB update and event publication    Transactional outbox
  must stay aligned                  

  Dashboard can be slightly stale    Async read model / CQRS

  Long-running order workflow across Saga with explicit states
  services                           

  Payment result ambiguous after     Idempotency + status
  timeout                            lookup/reconciliation

  Cross-service invariant must be    Reconsider boundary; coordinate
  strict                             workflow or keep invariant in one
                                     owner

  Historical replay is a core        Consider Event Sourcing after
  business requirement               evaluating complexity
  ---------------------------------------------------------------------

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

You now have a ten-phase interview-preparation series covering
migration foundations, data ownership, communication, security,
observability, database migration, deployment, architecture patterns,
mock interviews and testing strategy.

**Next step:** Scenario sections ko mock-interview drills ki tarah use
karein, aur **Phase 10 --- Testing Strategy** se testing/verification
questions cover karein (microservices interviews mein yeh aksar follow-up
hota hai).

------------------------------------------------------------------------

<a id="phase-10"></a>

# Phase 10 --- Testing Strategy and Migration Verification

## Phase 10: Testing Strategy for Microservices & Migration Verification

> **Audience:** Senior Software Engineer, Staff Engineer, Tech Lead,
> Solution Architect\
> **Technology focus:** .NET 8, xUnit, ASP.NET Core, SQL Server, AWS,
> messaging, CI/CD\
> **Running example:** Automotive Dealership Management System (DMS)

Is phase ka goal hai interview mein yeh bol paana: "Distributed system
mein sirf unit tests kaafi nahi; hum test strategy ko risk ke hisaab se
layers mein design karte hain, aur migration ke dauran data aur behavior
ki equivalence bhi *prove* karte hain."

------------------------------------------------------------------------

# 1. Microservices mein testing alag kyun hai?

Monolith mein ek process, ek DB transaction aur ek deployable hota hai.
Microservices mein failures network, messaging, versioning aur data
boundaries par aate hain. Naye risks:

-   Service A ka change Service B ko tod sakta hai (contract drift).
-   Event schema change se consumers silently fail ho sakte hain.
-   Duplicate/out-of-order messages ya partial failures (saga) aate hain.
-   Environment ke bina poori system ko ek jagah end-to-end test karna
    slow aur flaky hota hai.
-   Migration mein old aur new system ka data/behavior match hona chahiye.

------------------------------------------------------------------------

# 2. Test pyramid / testing layers

``` mermaid
flowchart TB
    E[End-to-end & synthetic business checks - few]
    C[Contract tests - consumer/provider]
    I[Integration tests - service + real DB/broker]
    U[Unit tests - many, fast]
    U --> I --> C --> E
```

  -------------------------------------------------------------------------
  Layer             Kya verify hota hai         Speed      Typical tools
  ----------------- --------------------------- ---------- ----------------
  Unit              Domain logic, validators,   Very fast  xUnit/NUnit,
                    state transitions                      FluentAssertions

  Integration       Service + real SQL Server / Medium     WebApplicationFactory,
                    queue adapter, EF mappings             Testcontainers

  Contract          Consumer expectation vs     Fast       Pact (or
                    provider behavior / event              schema checks)
                    schema

  End-to-end        Critical business journey   Slow       Playwright/API
                    across services                        suites, synthetic
                                                           monitors

  Non-functional    Load, resilience, security  Varies     k6/NBomber/JMeter,
                                                           chaos tools
  -------------------------------------------------------------------------

Principle: zyada tests neeche (fast, deterministic), kam tests upar
(slow, brittle). E2E sirf critical journeys ke liye rakhein.

------------------------------------------------------------------------

# 3. Unit tests

DMS example: `Order` aggregate ke state transitions.

``` csharp
public class OrderTests
{
    [Fact]
    public void Confirm_when_not_paid_throws()
    {
        var order = Order.CreatePending(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => order.Confirm());
    }
}
```

Focus: business rules, invariants, state machine ke valid/invalid
transitions (Phase 8 ka order state model), idempotency decisions.
External dependencies ko mock/fake karein; time aur GUID jaisi
non-determinism ko abstract karein.

------------------------------------------------------------------------

# 4. Integration tests (real dependencies)

Mocked DB par pass hone wale tests production behavior (SQL translation,
constraints, transactions) ki guarantee nahi dete. Real SQL Server
container use karein.

``` csharp
public class SalesApiTests : IClassFixture<SalesApiFactory>
{
    private readonly HttpClient _client;

    public SalesApiTests(SalesApiFactory factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task Post_sales_returns_accepted()
    {
        var response = await _client.PostAsJsonAsync("/sales",
            new { vehicleId = Guid.NewGuid(), dealerId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }
}
```

`SalesApiFactory` ek `WebApplicationFactory<Program>` subclass ho sakti
hai jo Testcontainers se SQL Server start kare, connection string
override kare aur auth ko test scheme se replace kare. (Isse `Program`
class test project ko visible honi chahiye, e.g. `public partial class
Program {}`.) Exact package versions/config apne project ke hisaab se
verify karein.

Kya test karein:

-   EF Core mappings, migrations apply hoti hain ya nahi.
-   Unique constraints (Outbox/Inbox dedup table) aur concurrency.
-   Transactional outbox: business row + outbox row ek hi transaction mein.
-   Authorization policies (scope, tenant/dealer isolation).

------------------------------------------------------------------------

# 5. Consumer-driven contract tests

Problem: Sales service Inventory API call karti hai. Inventory team
response field rename kar de to Sales production mein toot sakti hai,
chahe dono ke unit tests pass hon.

Contract testing flow (Pact-style):

1.  Consumer (Sales) apni expectations contract file ke roop mein publish
    karta hai.
2.  Provider (Inventory) CI mein us contract ke against verify karta hai.
3.  Agar provider change contract todta hai, provider ka build fail
    hota hai --- deploy se pehle.

``` mermaid
sequenceDiagram
    participant S as Sales (consumer)
    participant B as Contract broker
    participant I as Inventory (provider)
    S->>B: Publish contract
    I->>B: Fetch contracts
    I->>I: Verify provider against contract in CI
    I-->>B: Publish verification result
```

Events ke liye: message contracts / schema compatibility checks (e.g.
JSON Schema ya schema registry) --- backward-compatible changes allow
karein (optional field add), breaking changes (field remove/rename/type
change) ko versioning (`VehicleReserved.v2`) ke saath introduce karein.

Contract tests E2E ka replacement nahi; woh integration breakages ko
*jaldi aur sasta* pakadte hain.

------------------------------------------------------------------------

# 6. Messaging, saga aur failure-path tests

Happy path kaafi nahi. Distributed workflow ke liye test karein:

-   **Duplicate delivery:** same message do baar → side effect ek hi
    baar (Inbox/idempotency).
-   **Out-of-order events:** stale event ignore/defer.
-   **Poison message:** repeated failure → DLQ, aur DLQ alarm.
-   **Saga compensation:** payment fail → inventory release; compliance
    reject → payment void; terminal state `Cancelled`.
-   **Timeouts/retries:** bounded retries, backoff, retry amplification.
-   **Outbox publisher crash:** publish ke baad aur `PublishedUtc` mark
    se pehle crash → duplicate publication consumers handle karte hain.

``` csharp
[Fact]
public async Task Duplicate_event_is_applied_once()
{
    var message = new SaleConfirmed(Guid.NewGuid(), saleId, vehicleId,
                                    DateTime.UtcNow);

    await handler.HandleAsync(message, ct);
    await handler.HandleAsync(message, ct);   // redelivery

    Assert.Equal(1, await db.ProcessedMessages.CountAsync());
}
```

Concurrent duplicate ke liye parallel `Task.WhenAll` test bhi likhein,
kyunki sirf `AnyAsync` check race se protect nahi karta.

------------------------------------------------------------------------

# 7. Migration-specific verification

Migration mein sabse bada risk hai "naya system galat answer deta hai
aur kisi ko pata nahi chalta". Isliye:

## 7.1 Characterization tests

Legacy monolith ke current behavior ko pin karne wale tests pehle likhein
(golden master). Extraction ke baad wahi tests new service par chalayein.
Isse refactor ke dauran behavior regression pakadta hai.

## 7.2 Backfill and reconciliation tests

-   Row counts, checksums/hash by key range, aggregates (sum of amounts,
    count by status) --- source vs target.
-   Sampling: random records ka field-by-field compare.
-   Edge cases: NULLs, encoding, time zones/UTC conversion, decimal
    precision, soft-deleted rows.
-   Re-runnable/idempotent backfill: dobara chalane par duplicates na
    bane.

## 7.3 Shadow reads / traffic comparison

Production read traffic ko new service par bhi bhejein (response user ko
na jaye) aur outputs compare karein. Differences ko categorize karein
(real bug vs acceptable difference). Side-effect wale writes ko shadow
mode mein avoid karein ya isolate karein.

## 7.4 Cutover rehearsal

Staging mein full cutover + rollback rehearse karein: data lag,
feature-flag flip, rollback ya roll-forward decision, aur runbook ke
steps. Go/no-go criteria measurable rakhein (error rate, reconciliation
mismatch = 0 ya agreed threshold, latency).

------------------------------------------------------------------------

# 8. Non-functional testing

## Load / performance

Phase 8 ke capacity estimates ko load test se validate karein. Realistic
traffic mix, ramp-up, peak factor, aur dependency latency model karein.
Metrics: p95/p99 latency, error rate, saturation (CPU, DB connections,
queue depth). Production-like data volume ke saath test karein.

## Resilience / chaos

Controlled experiments: dependency down, slow responses, packet loss,
instance kill, queue backlog. Verify karein: timeouts, circuit breaker,
bulkhead, graceful degradation, aur alerts. Pehle non-prod mein, phir
chhote blast radius ke saath production-like environment mein.

## Security testing

Authn/authz negative tests (expired token, wrong audience, missing
scope, cross-tenant access), dependency/vulnerability scanning, container
image scanning, secrets scanning, aur IaC policy checks CI mein.

------------------------------------------------------------------------

# 9. Testing in production (safely)

Pre-production environment production ko poori tarah replicate nahi
karta. Isliye complement mein:

-   **Synthetic monitoring:** critical business journeys (login → create
    order → confirm) ko periodic automated probe se chalayein.
-   **Canary + SLO gates:** Phase 7 ke canary release mein error rate,
    latency aur business metrics par automatic halt.
-   **Feature flags / dark launch:** code deploy ho, behavior gradually
    enable ho.
-   **Observability as test oracle:** traces, metrics aur logs
    (Phase 5) se failures jaldi detect karein.

Test data ke liye real PII use na karein; synthetic ya masked data
rakhein, aur synthetic transactions ko reports/billing se alag tag karein.

------------------------------------------------------------------------

# 10. Testing in CI/CD pipeline

  ------------------------------------------------------------------------
  Stage              Gate
  ------------------ -----------------------------------------------------
  Pull request       Build, unit tests, static analysis, dependency scan

  Merge to main      Integration tests (Testcontainers), contract
                     verification, container image scan

  Pre-prod           Smoke + critical-path E2E, migration scripts dry run,
                     load test (scheduled/nightly or pre-release)

  Production         Canary with SLO gates, synthetic checks, automated
                     halt/rollback rules
  ------------------------------------------------------------------------

Tips: flaky tests ko quarantine karke fix karein (ignore nahi), tests
independent aur parallel-safe rakhein, aur pipeline time budget maintain
karein --- fast feedback sabse zaroori hai.

------------------------------------------------------------------------

# 11. Common anti-patterns

-   Sirf E2E tests par depend karna (slow, flaky, late feedback).
-   Database ko mock karke "integration" test kehna.
-   Shared long-lived test environment jo sab teams ke tests ko
    corrupt kare.
-   Contract/schema change ko bina consumer check ke deploy karna.
-   Migration ke baad sirf row count compare karna (values check nahi).
-   Happy path hi test karna; retries, duplicates aur compensation
    ignore karna.
-   Coverage percentage ko goal bana dena, risk ko nahi.

------------------------------------------------------------------------

# 12. Interview questions and model answers

**Q1. Microservices ke liye aap testing strategy kaise design karenge?**
Risk-based layered strategy: fast unit tests, real-dependency
integration tests, consumer-driven contract tests, limited E2E for
critical journeys, plus load/resilience tests aur production synthetic
monitoring. Pipeline gates ke saath.

**Q2. Contract test aur integration test mein kya farak hai?**
Integration test ek service ko real dependencies (DB/broker) ke saath
check karta hai. Contract test do services ke beech API/event agreement
verify karta hai, bina dono ko ek saath deploy kiye.

**Q3. Event schema safely kaise evolve karenge?**
Backward-compatible additions (optional fields), schema compatibility
checks CI mein, breaking changes ke liye new versioned event type aur
consumers migrate hone tak dono support.

**Q4. Migration ke baad data sahi hai yeh kaise prove karenge?**
Count + checksum + aggregate reconciliation, sampled field-level
compare, edge-case tests, shadow reads, aur cutover rehearsal. Mismatch
threshold pehle se define karke go/no-go decide karein.

**Q5. Flaky E2E tests ka kya karenge?**
Quarantine, root cause (shared state, timing, env), lower layer par
coverage shift (contract/integration), aur E2E ko minimum critical set
tak rakhna.

**Q6. Idempotent consumer ko kaise test karenge?**
Same message dobara aur concurrently deliver karke assert karein ki side
effect ek hi baar hua; DB unique constraint ke saath real DB par test.

**Q7. Saga compensation kaise test karenge?**
Har step par failure inject karke expected compensation sequence aur
final terminal state verify karein; compensation khud bhi retry/idempotent
honi chahiye.

**Q8. Production mein testing kyun zaroori hai?**
Pre-prod mein real traffic mix, data volume aur dependency behavior
reproduce nahi hota. Canary, synthetic checks aur feature flags risk ko
controlled tareeqe se kam karte hain.

------------------------------------------------------------------------

# 13. Scenario drills

**Scenario A --- Inventory API field rename.**
Inventory team `vin` ko `vehicleIdentificationNumber` rename karna chahti
hai. Answer: pehle consumer contracts check karein; additive change
(dono fields) → consumers migrate → purani field deprecate → remove.
Provider CI contract verification se breakage block karein.

**Scenario B --- Backfill ke baad 0.02% rows mismatch.**
Answer: mismatch categorize karein (timezone, precision, soft-delete,
in-flight writes during backfill); CDC/delta sync se gap close karein;
reconciliation dobara chalayein; threshold aur business impact ke basis
par go/no-go.

**Scenario C --- Black Friday se pehle load readiness.**
Answer: traffic model, production-like data, ramp + spike tests,
dependency limits (DB connections, queue consumers), autoscaling
validation, aur failure injection; results ko SLO se compare karein.

------------------------------------------------------------------------

# 14. Quick revision checklist

-   [ ] Test pyramid aur har layer ka purpose.
-   [ ] Real DB/broker ke saath integration tests (Testcontainers).
-   [ ] Consumer-driven contracts + event schema compatibility.
-   [ ] Duplicate, out-of-order, poison, compensation tests.
-   [ ] Characterization tests before extraction.
-   [ ] Reconciliation (counts, checksums, aggregates, sampling).
-   [ ] Shadow reads and cutover rehearsal with go/no-go criteria.
-   [ ] Load, chaos aur security testing.
-   [ ] Canary + SLO gates + synthetic monitoring.
-   [ ] CI/CD stage-wise gates; flaky tests fix, ignore nahi.

------------------------------------------------------------------------

## Phase 10 --- Key Interview Message

> **"Main testing ko layers mein risk ke hisaab se design karta hoon:
> fast unit tests, real-dependency integration tests, consumer-driven
> contracts, aur limited E2E. Migration mein characterization tests,
> reconciliation aur shadow reads se equivalence prove karta hoon,
> failure paths (duplicates, sagas, DLQ) explicitly test karta hoon, aur
> production mein canary, SLO gates aur synthetic checks se residual
> risk control karta hoon."**

------------------------------------------------------------------------

<a id="phase-11"></a>

# Phase 11 --- Resume Deep-Dive

## Phase 11: Resume Deep-Dive --- Parteek Arora (Staff Engineer)

> **Purpose:** Resume ke har major claim ko interview-ready story mein
> convert karna.\
> **Source:** Resume (Nagarro DMS, Common Integration Platform,
> CSRconnect, EY Connected Apps, IGT, Infosys).\
> **Note:** Neeche ke model answers resume ke claims par based hain. Jo
> details resume mein nahi hain (design choices, incidents, exact
> numbers) wahan `[apna actual detail]` placeholder hai --- sirf wahi
> bolein jo aapne sach mein kiya. Interviewer kisi bhi number ka "kaise
> measure kiya?" follow-up poochega.

------------------------------------------------------------------------

# 1. Resume ka narrative (60-second pitch)

> "Main 10 saal se enterprise .NET mein hoon --- C#, ASP.NET Core,
> .NET 8, SQL Server, DynamoDB aur AWS. Pehle EY mein ek monolith se
> microservices migration ka hissa raha. Nagarro mein main Staff Engineer
> hoon, 5 engineers ki team lead karta hoon, aur Dealership Management
> System ke integration platform par kaam karta hoon jo third-party
> DMS aur in-house DMS ke beech deals translate karta hai --- 100K+
> transactions per month, 99.9% availability. Maine Spec-Driven
> Development introduce kiya aur CI/CD automation se release cycle 5
> din se 3 din kiya."

Tip: Har claim ke peeche ek *problem → decision → trade-off → result*
story ready rakhein.

------------------------------------------------------------------------

# 2. DMS integration platform (Nagarro)

**Resume claim:** Middle layer jo third-party DMS platforms aur in-house
DMS ke beech deals import/export translate karta hai; dealer-specific
transformations aur state-specific rules (tax calculation); 3 core
services; AWS Lambda, DynamoDB, SQL Server.

## 2.1 Architecture ko kaise explain karein

``` mermaid
flowchart LR
    T[Third-party DMS] -->|inbound deal| I[Integration service]
    I --> X[Dealer-specific translation]
    X --> R[State rules / tax calculation]
    R --> D[In-house DMS]
    D -->|outbound| X
    I --> L[(Logs / audit)]
    U[Dashboard] --> L
    U -->|script changes| S[(Script store)]
    S --> X
```

Interview mein yeh cheezein ready rakhein:

-   **Idempotency:** same deal dobara aaye (retry/duplicate) to double
    processing na ho (Phase 3: Inbox/idempotency key).
-   **Failure handling:** third-party down ho to retry with backoff, DLQ,
    aur "integration pending" state --- completed business transaction
    ko rollback na karein (Phase 8).
-   **Ordering:** same deal ke multiple updates --- version/sequence se
    stale update ignore.
-   **Observability:** correlation ID per deal across inbound → translate
    → outbound (Phase 5). Aapka dashboard log viewer isi ka part hai.
-   **Multi-tenancy:** dealer-level isolation aur dealer-specific
    configuration (Phase 4/8).

## 2.2 Q&A

**Q. Dealer-specific logic ko code mein hardcode kyun nahi kiya?**
Har dealer ka mapping alag aur frequently badalta hai. Code mein
hardcode karne par har change ke liye full release chahiye. Database-
stored scripts se turnaround kam hota hai. Trade-off: runtime code
execution ka security aur testability risk --- isliye controls chahiye
(Section 3).

**Q. 99.9% availability across 12 platform services kaise measure hui?**
`[apna actual detail]` Structure: SLI define karein (successful
requests / valid requests, ya successful end-to-end transactions),
measurement window (monthly), data source (CloudWatch/New Relic), aur
exclusions (planned maintenance, client-caused errors). Samjhayein ki
99.9% ka matlab ~43 minutes/month downtime budget hai (30 din ke
mahine mein), aur committed SLA 99.5% (~3.6 hours) se zyada tight hai.

**Q. 100K+ transactions/month ka scale kaisa hai?**
Yeh average ~0.04 transactions/second hai (100,000 / 2.59M seconds),
yaani throughput challenge nahi, correctness/reliability challenge hai:
duplicates, partial failures, data translation errors, aur auditability.
Isse honestly bolna maturity dikhata hai --- scale inflate na karein.

**Q. Agar ek third-party DMS slow ho jaye to?**
Timeouts, circuit breaker, bulkhead (us dealer ka traffic dusron ko na
roke), queue buffering, aur alert. Retry bounded aur jittered
(Phase 3).

------------------------------------------------------------------------

# 3. Roslyn runtime scripting (sabse distinctive feature)

**Resume claim:** Dealer-specific mapping logic database scripts ke roop
mein store hota hai aur runtime par Roslyn se compile hota hai; bina
full deployment ke mapping change.

## 3.1 Concept

Roslyn .NET compiler platform hai. Aap C# source ko runtime par parse
aur compile karke executable delegate bana sakte hain (Microsoft.CodeAnalysis
scripting/compilation APIs).

``` csharp
// Illustrative sketch --- package versions aur API details verify karein.
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

var options = ScriptOptions.Default
    .WithReferences(typeof(DealMapper).Assembly)   // sirf allowed assemblies
    .WithImports("System", "System.Linq");

var script = CSharpScript.Create<DealDto>(
    scriptSource, options, globalsType: typeof(MappingGlobals));

var compiled = script.Compile();             // diagnostics check karein
var result = await script.RunAsync(new MappingGlobals { Input = deal });
```

## 3.2 Risks aur controls (interview ka core)

  --------------------------------------------------------------------------
  Risk                       Control
  -------------------------- -----------------------------------------------
  Arbitrary code execution   Allow-listed references/imports; scripts ko
  (file/network/process      sirf pure mapping functions tak limit karein;
  access)                    least-privilege runtime identity

  Infinite loop / heavy      Execution timeout + cancellation token; resource
  CPU / memory               limits; separate worker/process jahan possible

  Bad script production      Validation on save: compile diagnostics, test
  tod de                     cases, dry-run against sample payloads

  Un-audited changes         Version history, who/when/why, approval workflow,
                             audit log

  Rollback                   Previous script version ek click mein restore

  Compile cost / latency     Compiled delegate ko cache karein (key: script
                             id + version/hash); invalidate on update

  Memory leak (dynamic       Roslyn script assemblies unload nahi hoti jab
  assemblies)                tak `AssemblyLoadContext` collectible na ho;
                             versions/recompilation ko bound karein

  Multi-instance             Cache invalidation across instances (version
  inconsistency              check ya event) --- sab instances ek hi
                             version use karein
  --------------------------------------------------------------------------

**Important honest point:** Roslyn scripting *sandbox nahi hai*.
Allow-listing se risk kam hota hai, lekin untrusted code ke liye strong
isolation (alag process/container, restricted permissions) chahiye. Agar
scripts sirf trusted internal users likhte hain (dashboard access
controlled), to threat model alag hai --- yeh clearly bolein.

## 3.3 Q&A

**Q. Is design ka alternative kya hai?**
(1) Configuration-driven mapping (JSON/rules engine) --- safer, kam
flexible. (2) Plugin assemblies --- strongly typed lekin deployment
chahiye. (3) Expression engines (e.g. limited DSL) --- safer. Roslyn
sabse flexible hai par security/ops cost sabse zyada. Choose karne ka
reason: `[apna actual detail: flexibility vs release speed]`.

**Q. Script ko production mein promote kaise karte hain?**
Draft → validate (compile + sample tests) → review/approve → versioned
publish → cache invalidate → monitor error rate → rollback agar spike.
Yeh canary/feature-flag idea ka hi version hai (Phase 7).

**Q. Script runtime error aaye to transaction ka kya?**
Deal ko failed/pending state mein rakhein, error + script version log
karein (traceability), retry/manual-review queue, aur dusre deals ko
affect na hone dein.

**Q. Script testing kaise karein?**
Golden input/output samples per dealer, regression suite, aur "new
script vs current script" comparison (shadow run) --- Phase 10 ke
characterization/shadow ideas ka reuse.

------------------------------------------------------------------------

# 4. Common Integration Platform (CI/CD & deployment dashboard)

**Resume claims:** ~40% Deployment Dashboard UI; Terraform/CDKTF +
GitHub Actions; deployment efficiency 40%; release cycle 5 → 3 din;
JFrog Artifactory, SonarQube, Husky, Lighthouse, New Relic.

**Q. Release cycle 5 se 3 din kaise hua?**
`[apna actual detail]` Typical structure: bottleneck identify kiya
(manual steps, environment promotion, approvals, rollback) → automate
kiya (pipeline, artifact promotion, infra as code) → measure kiya
(lead time before/after). "Kaun sa step hataya" aur "kya manual gate
rakha aur kyun" batayein.

**Q. Artifact promotion ka principle?**
Build once, deploy the same artifact across environments (Artifactory
mein immutable versioned artifact); environment-specific config
alag. Isse "staging mein chala, prod mein alag binary" risk khatam hota
hai.

**Q. Rollback kaise handle hota tha?**
Dashboard se previous artifact version redeploy; lekin DB/schema changes
ho chuke ho to blind rollback unsafe --- expand-and-contract aur
backward compatibility (Phase 6/7).

**Q. SonarQube, Husky, Lighthouse ka role?**
SonarQube: quality gates (code smells, coverage, vulnerabilities).
Husky: Git hooks (lint/test pre-commit). Lighthouse: frontend
performance/accessibility checks. New Relic: runtime APM/observability.
Gates ko enforce karne ka rule batayein (kya build fail karta hai).

**CDKTF flag:** CDKTF Dec 2025 mein deprecated/archived hai. Interviewer
puche to: "Hamare existing stack CDKTF par hai; maine plan banaya hai
`cdktf synth --hcl` se plain Terraform HCL (ya OpenTofu) par migrate
karne ka, taaki unmaintained dependency na rahe." `[apna actual plan
ho to hi bolein]`. Resume mein bhi "Terraform/CDKTF" ko "Terraform
(CDKTF → HCL migration)" ya sirf "Terraform" karne par vichar karein.

------------------------------------------------------------------------

# 5. Spec-Driven Development (SDD) aur AI-assisted delivery

**Resume claims:** SDD introduce kiya; ~25% feature delivery time kam;
manual unit-test authoring 40% kam; 80%+ coverage maintained.

## 5.1 Kaise explain karein

SDD: code se pehle specification (behavior, acceptance criteria,
edge cases) → plan → task breakdown → review gates → implementation
(AI assistant ke saath) → tests. Spec single source of truth hoti hai
jo AI ko structured context deti hai.

## 5.2 Interviewer ke tough follow-ups

**Q. 25% aur 40% kaise measure kiya?**
`[apna actual detail]` Batayein: baseline (pehle ke N features ka
average cycle time), comparison (SDD ke baad ke N features), kya
count hua (ticket start → merge/deploy), aur confounders (feature
size, team change). Agar rough estimate hai to "approximately" aur
sample size honestly bolein. Unmeasurable claim defend na karein.

**Q. AI-generated code ki quality kaise ensure karte ho?**
Spec + tests pehle, human code review mandatory, static analysis
(SonarQube), coverage gates, security review, aur small PRs. "Coverage
80% ka matlab quality nahi" --- mutation testing/meaningful assertions
par bhi baat karein.

**Q. Risks?**
Plausible-but-wrong code, security/licensing issues, over-reliance,
data leakage (sensitive code/data prompts mein na daalein), aur
reviewers ka skill erosion. Mitigation: policy, review standards,
approved tools.

**Q. Team ne adopt kaise kiya?**
Pilot → template (spec/plan/tasks) → review checklist → retro se
iterate. Resistance kaise handle ki: `[apna actual detail]`.

------------------------------------------------------------------------

# 6. Data stores: SQL Server, DynamoDB, Cosmos DB

## 6.1 DynamoDB (resume mein 3 projects)

Key concepts jo poochhe jaate hain:

-   **Access-pattern-first modelling:** pehle queries decide karein,
    phir partition key (PK) + sort key (SK) design.
-   **Hot partition:** skewed key (e.g. ek dealer ka bahut traffic) →
    key sharding/high-cardinality keys.
-   **GSI/LSI:** alternate access patterns; GSI eventually consistent.
-   **Consistency:** reads default eventually consistent; strongly
    consistent read option (GSI par nahi).
-   **Transactions:** `TransactWriteItems` limited (item count/size
    limits --- current docs verify karein).
-   **Capacity:** on-demand vs provisioned; throttling; retries.
-   **No joins:** denormalization; single-table design ka trade-off
    (readability vs efficiency).
-   **Idempotency:** conditional writes (`attribute_not_exists`) se
    dedup --- Inbox pattern ka DynamoDB version.

**Q. SQL Server vs DynamoDB kab?**
Relational integrity, ad-hoc queries, complex joins/reporting → SQL
Server. Predictable key-based access, scale/latency, flexible schema →
DynamoDB. Aapke projects mein dono kyun use hue: `[apna actual
detail]`.

## 6.2 Cosmos DB aur Azure Blob (EY)

-   **Cosmos DB:** partition key choice, RU (request unit) based cost,
    consistency levels (strong → eventual), cross-partition query cost.
-   **Blob Storage:** large files (e.g. Excel exports/bulk data) DB se
    bahar; metadata DB mein, file blob mein; SAS tokens/pre-signed
    access.
-   **Q. 100,000+ tax records bulk retrieval/Excel export kaise
    efficient banaya?** `[apna actual detail]` Possible points:
    pagination/streaming, async job + status polling (long request na
    rakhein), chunked processing, avoid loading everything in memory,
    background export + download link.

------------------------------------------------------------------------

# 7. EY: Monolith → microservices story

Resume mein migration ka experience hai; isko Phase 1-3, 6 ke
framework se bolein:

1.  **Why:** `[apna actual detail]` (scaling, release independence,
    team autonomy).
2.  **How:** incremental (Strangler Fig), pehle low-risk capability.
3.  **Data:** monolith DB se service-owned data (Cosmos DB/Blob) ki
    taraf; backfill + reconciliation.
4.  **Trade-offs:** operational complexity, distributed failures,
    eventual consistency.
5.  **Result:** `[apna actual detail]`.

**Honest framing:** agar aap migration *team ka hissa* the (lead nahi),
to "main X component par kaam kiya" kahein --- resume bhi "worked ... as
part of the migration" kehta hai. Overclaim se bachein.

------------------------------------------------------------------------

# 8. Leadership (5 engineers)

**Resume claim:** 3 saal team lead --- task allocation, sprint planning,
estimation, code review, technical direction, interview participation,
client/Product Owner collaboration.

Prepare stories (STAR format):

-   **Underperformer / skill gap** ko kaise handle kiya.
-   **Technical disagreement** (aap vs senior/peer) kaise resolve hua.
-   **Production incident** mein leadership (Phase 5 postmortem
    framework).
-   **Estimation miss** aur client expectation management.
-   **Mentoring:** kisi engineer ki growth ka concrete example.
-   **Code review standards:** kya enforce karte ho (tests, naming,
    security, small PRs).
-   **Staff-level scope:** team ke bahar influence --- SDD adoption,
    release automation, standards.

**Q. Staff Engineer aur Senior Engineer mein farak?**
Scope aur leverage: Senior apne tasks/features ko deliver karta hai;
Staff multiple teams/systems ke technical direction, standards, risk
aur long-term decisions par impact karta hai, aur doosron ko
effective banata hai.

------------------------------------------------------------------------

# 9. Frontend (Angular/TypeScript) aur dashboard

Dashboard ke liye interview angles:

-   **State management:** component state vs RxJS/store; subscriptions
    ka cleanup (memory leaks).
-   **Log viewer:** pagination/virtual scroll, server-side filtering,
    time-range queries --- millions of log lines browser mein na
    load karein.
-   **Script editor risk:** role-based access, audit, confirmation,
    validation feedback before publish.
-   **Security:** XSS (log content sanitize), token handling, CSRF/CORS
    (Phase 4).
-   **Performance:** lazy loading, change detection strategy
    (`OnPush`), bundle size, Lighthouse metrics.

------------------------------------------------------------------------

# 10. Earlier roles --- kya poochha ja sakta hai

-   **Infosys (trading order entry):** financial correctness, 10,000+
    accounts, order idempotency, audit trail, transactions.
-   **IGT (voucher, refunds via SOAP/ODS):** SOAP integration, refund
    to original payment, bulk Excel-driven workflow, error handling.
-   **EY Market Price Reference Guide:** calculation correctness,
    decimal precision, data modelling.

Inke liye 1-2 line ka "kya seekha jo ab staff-level decisions mein kaam
aata hai" ready rakhein.

------------------------------------------------------------------------

# 11. Resume gaps / honest answers

  ----------------------------------------------------------------------
  Topic                    Honest, strong answer pattern
  ------------------------ ---------------------------------------------
  Kubernetes               "Mera focus Lambda/ECS-style AWS services par
                           raha; K8s concepts (Phase 7) jaanta hoon,
                           production ownership `[apna actual level]`."

  Event streaming (Kafka)  SQS/SNS-style messaging ka experience batayein;
                           Kafka ke trade-offs conceptually explain karein.

  Azure depth              AZ-900 + EY project; deeper Azure services par
                           honest level batayein.

  Formal system design     Phase 8 ke framework (requirements → capacity
  at huge scale            → components → failures) se approach dikhayein;
                           DMS ka scale (100K/month) honestly bolein.
  ----------------------------------------------------------------------

------------------------------------------------------------------------

# 12. Resume improvement suggestions

-   Metrics ke saath **measurement method** ek line mein add karein
    (e.g. "measured over last 10 features").
-   "Terraform/CDKTF" → "Terraform" (ya migration plan ke saath).
-   Roslyn scripting ko ek separate bullet mein highlight karein
    (security controls ke saath) --- yeh differentiator hai.
-   "Contributed to ..." jaise vague verbs ko ownership-clear verbs se
    replace karein jahan aapne actually lead kiya.
-   Summary mein ek impact line add karein (availability, release
    time, team size).

------------------------------------------------------------------------

# 13. Practice questions (resume-based)

1.  DMS integration platform ka end-to-end flow whiteboard par samjhao.
2.  Roslyn scripts ko secure aur reliable kaise banaoge?
3.  99.9% availability ka SLI/SLO aur measurement kaise define kiya?
4.  Release cycle 5 → 3 din: bottleneck kya tha, kya automate kiya?
5.  SDD ke 25%/40% numbers ka evidence kya hai?
6.  DynamoDB mein hot partition kaise pehchanoge/fix karoge?
7.  EY migration mein aapka exact role aur ek failure/learning?
8.  Team ke ek low performer ko kaise handle kiya?
9.  CDKTF deprecated hai --- aapka plan?
10. Agar aapko DMS platform dobara design karna ho to kya alag
    karoge?

------------------------------------------------------------------------

## Phase 11 --- Key Interview Message

> **"Mera experience integration-heavy enterprise systems mein hai:
> reliable deal translation across third-party platforms, runtime-
> configurable dealer logic with guardrails, aur automated, measurable
> delivery pipelines. Main numbers ko measurement method ke saath bolta
> hoon, trade-offs honestly batata hoon, aur team ko technical direction
> deta hoon."**

------------------------------------------------------------------------

## Final revision strategy

-   Explain **why** a service boundary exists, not just how to split
    code.
-   Prefer **business capability boundaries** and explicit data
    ownership over splitting by technical layer.
-   Make distributed workflows resilient with **timeouts, idempotency,
    Outbox/Inbox, Saga, retries with limits, and DLQs**.
-   Migrate incrementally with **Strangler Fig**, expand-and-contract
    schema changes, reconciliation, and a rollback plan.
-   Demonstrate production readiness through **independent deployment,
    observability, SLOs, security, capacity planning, and incident
    response**.
-   In system-design answers, make trade-offs explicit: consistency vs
    availability, synchronous simplicity vs asynchronous resilience, and
    operational complexity vs independent scaling.

**End of master guide.**
