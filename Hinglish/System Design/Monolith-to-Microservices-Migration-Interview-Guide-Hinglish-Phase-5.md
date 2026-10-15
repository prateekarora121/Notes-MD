# Monolith-to-Microservices Migration Interview Guide --- Hinglish

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

  -----------------------------------------------------------------------
  Requirement                         Possible option
  ----------------------------------- -----------------------------------
  AWS infrastructure metrics and      CloudWatch
  alarms                              

  AWS-centric distributed trace       OpenTelemetry (ADOT) instrumentation
  analysis                            + X-Ray as backend

  Vendor-neutral instrumentation      OpenTelemetry

  Metrics dashboards                  CloudWatch dashboards or
                                      Prometheus/Grafana stack

  Centralized searchable logs         CloudWatch Logs or another approved
                                      log platform
  -----------------------------------------------------------------------

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
