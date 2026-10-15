# Monolith to Microservices Migration --- Interview Guide (Hinglish)

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

  -----------------------------------------------------------------------
  Concept                             Remember this
  ----------------------------------- -----------------------------------
  REST                                Request-response; caller downstream
                                      availability/latency par depend
                                      karta hai

  gRPC                                Typed RPC and streaming; use when
                                      contract/performance needs justify
                                      it

  Queue                               Work buffering and competing
                                      consumers

  Pub-sub                             Multiple subscribers independently
                                      react

  Command                             Intent/request

  Event                               Past fact

  Retry                               Transient failures; bounded and
                                      idempotency-aware

  Timeout                             Caller wait limit; remote outcome
                                      may be unknown

  Circuit breaker                     Repeated failing calls ko
                                      temporarily stop karta hai

  Bulkhead                            Resource pools isolate karta hai

  At-least-once                       Duplicate delivery possible

  Idempotency                         Repeat request se unintended
                                      repeated effect na ho

  DLQ                                 Failed messages ke liye owned
                                      recovery queue

  Event versioning                    Contracts evolve without breaking
                                      consumers

  Correlation ID                      Related workflow operations connect
                                      karta hai

  Trace ID                            Distributed trace identify karta
                                      hai
  -----------------------------------------------------------------------

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
