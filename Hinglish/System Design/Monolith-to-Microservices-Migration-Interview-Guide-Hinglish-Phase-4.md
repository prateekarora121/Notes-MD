# Monolith to Microservices Migration --- Interview Guide (Hinglish)

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
              .RequireClaim("scope", "sales.write"));
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
`RequireClaim("scope", "sales.write")` ko provider ke actual claims
format ke hisaab se adapt karein. - `Authority` and `Audience` correct
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

  -----------------------------------------------------------------------
  Concept                             Remember this
  ----------------------------------- -----------------------------------
  API Gateway                         Common entry point and
                                      cross-cutting policies

  BFF                                 Client-specific
                                      aggregation/response shaping

  Micro-frontends                     Independent UI areas; not mandatory
                                      with microservices

  Authentication                      Who are you?

  Authorization                       What may you do?

  OAuth 2.0                           Authorization framework

  OIDC                                Identity/authentication layer over
                                      OAuth 2.0

  JWT                                 Token format; signature and claims
                                      must be validated

  Access token                        Intended for API/resource access

  ID token                            Authentication information for
                                      client

  Least privilege                     Minimum permissions needed

  Resource authorization              Check actual resource/tenant
                                      ownership

  Gateway resilience                  Redundancy, latency/error metrics,
                                      safe rollout

  BFF caching                         Only with freshness and
                                      tenant-aware cache keys

  Business invariants                 Enforce in authoritative domain
                                      service
  -----------------------------------------------------------------------

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
