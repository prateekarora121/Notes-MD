# Monolith to Microservices Migration --- Interview Guide (Hinglish)

> **Audience:** Senior Software Engineer, Staff Engineer, Tech Lead,
> Solution Architect\
> **Technology focus:** .NET 8, C#, ASP.NET Core, SQL Server, AWS,
> messaging, CI/CD\
> **Running example:** Automotive Dealership Management System (DMS)

## How to use this guide

Yeh guide interview preparation aur practical architecture decisions
dono ke liye hai. Technical terms English mein rakhe gaye hain, lekin
explanations simple Roman Hinglish mein hain. Har phase mein concept,
DMS example, diagrams, trade-offs, interview answers aur practical
checks diye gaye hain.

Yeh guide 10 phases mein organized hai. **Phase 1** foundations,
migration assessment, decomposition, DDD aur incremental migration
strategy cover karta hai. Baaki phases database ownership, communication,
security, observability, data migration, deployment, architecture
patterns, mock interviews aur testing strategy (Phase 10) cover karte
hain.

------------------------------------------------------------------------

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

  ------------------------------------------------------------------------
  Criterion               Strong boundary ka       Warning sign
                          signal                   
  ----------------------- ------------------------ -----------------------
  Cohesion                Service ek focused       Unrelated
                          business capability own  responsibilities ek
                          karti hai                saath hain

  Coupling                Few stable contracts     Har request multiple
                                                   services par depend hai

  Data ownership          Ek service authoritative Multiple services same
                          writes own karti hai     tables update karti
                                                   hain

  Change frequency        Capability independently Har change ke liye
                          change hoti hai          multiple services
                                                   coordinated release
                                                   karti hain

  Consistency             Local invariants ek      Normal operation ke
                          boundary ke andar        liye frequent
                          enforce ho sakte hain    distributed
                                                   transactions chahiye

  Scaling                 Different workload needs Split se meaningful
                          hain                     operational benefit
                                                   nahi

  Team ownership          Clear team responsible   Ownership ambiguous hai
                          hai                      

  Operations              Team service ko          Service maintain karne
                          deploy/monitor/support   ka operational
                          kar sakti hai            capability nahi
  ------------------------------------------------------------------------

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
