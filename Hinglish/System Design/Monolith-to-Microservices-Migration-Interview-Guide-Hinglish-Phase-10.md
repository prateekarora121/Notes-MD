# Monolith-to-Microservices Migration Interview Guide --- Hinglish

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
