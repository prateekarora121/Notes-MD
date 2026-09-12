> **AWS Quick Revision Notes** · [Index](README.md) · Part I

# IAM & Security

---

## 1. IAM Foundations

### Overview, Root & Shared Responsibility

Every API call: **1. Authentication** (who are you? valid creds) → **2. Authorization** (allowed? some policy permits). Fail auth = `InvalidClientTokenId`/signature; fail authz = `AccessDenied`.
- **IAM is global** (no region), **free**, **eventually consistent** (fresh role/policy takes seconds — breaks CI/CD create-role-then-use pipelines on first run, succeeds on retry).
- **Root** = sign-up email, unlimited, policies can't restrict it. Rules: MFA immediately, never daily use, never share, **never create access keys for root**. Day one: create admin (ideally Identity Center), lock root away.
- **Root-only actions** (trick — `AdministratorAccess` can't do these): close account; change account name/root email/password; change/cancel Support plan; restore IAM billing access; RI Marketplace seller; **S3 MFA Delete** / delete a deny-all bucket policy; some tax invoices.
- **Shared responsibility:** AWS runs IAM as a global HA service + tooling; **you** create/organize identities, apply least privilege, rotate/protect creds, enable+review tooling, remove leavers. "AWS guarantees IAM works; how I configure it is on me."

---

## 2. Identities & Access

### Users, Groups & Permissions

- **User** = one person (or legacy app that can't use a role); long-term creds (password + access keys).
- **Group** = permission container: **users only, no nesting**, user in multiple groups = **union**, group is **not an identity** (can't log in as, can't be a `Principal`).
- **Deny-by-default:** new user with no policy can do *nothing*.
- **Identity-based** (User/Group/Role, no `Principal`) vs **resource-based** (on resource, `Principal` mandatory — S3/SQS/SNS/KMS/Lambda).
- **Cross-account two ways:** (1) **resource-based policy** — caller keeps own identity, both sides must allow; (2) **AssumeRole** — caller *becomes* role, loses own perms for session. "Resource policy = stay yourself; AssumeRole = become someone else."
- **Audit difference:** resource-policy path → target account's CloudTrail shows the *real* caller ARN; AssumeRole → shows `assumed-role/.../session` (must correlate with source account's trail). Compliance "attribute on our own" → resource-policy path.

### Password Policy

Min length (≤128), char types, self-change, expiration, prevent reuse. Cheapest brute-force defence — but real controls = MFA + eliminating long-lived human creds (Identity Center).

### MFA

Something you physically have. Mandatory on root, expected on every human console user.
- Virtual (TOTP app, holds multiple tokens), FIDO/U2F key (phishing-resistant), hardware TOTP, passkeys/biometrics.
- **Up to 8 devices per user** (register a backup).
- **❗ MFA natively protects console only.** CLI/API needs policy condition `"Bool":{"aws:MultiFactorAuthPresent":"true"}` + `sts:GetSessionToken` (IAM user) or `AssumeRole` with `--serial-number`/`--token-code`. MFA in a **trust policy** = standard control for prod cross-account.

### Access to AWS: Console, CLI, SDK & Keys

Three front doors, one REST API (SigV4). Access keys: secret shown once; **max 2 keys** (deliberate — for zero-downtime rotation). Never commit/bake/share. Prefer roles. `aws sts get-caller-identity` = "who am I?" (best debug command). **CloudShell** = browser terminal, no keys on laptop.
**❗ Credential provider chain order:** CLI params → **env vars** → shared credentials file → container creds → **EC2 instance profile (IMDS, last)**. So a stale `AWS_ACCESS_KEY_ID` silently shadows the instance role → AccessDenied for perms the role has. `get-caller-identity` showing `user/...` instead of `assumed-role/...` reveals it.

---

## 3. Policies, Roles & Least Privilege

### Policy Types & Structure

| Type | Managed by | When |
|---|---|---|
| AWS managed | AWS (auto-updated) | Quick start, broad — almost always too wide |
| **Customer managed** | You | ✅ **Right answer** — versioned (5 kept, rollback), reusable, visible |
| Inline | You, embedded | Rare one-offs; invisible to audits |

**Anatomy:** `Version` (always `2012-10-17` — the *language* version), `Id`, `Statement`, `Sid`, `Effect`, `Principal` (resource/trust only), `Action` (`s3:Get*`; `NotAction` = "except"), `Resource` (ARNs; some APIs need `*`), `Condition`.

**❗ `bucket` vs `bucket/*`:** `arn:aws:s3:::my-bucket` = bucket (for `ListBucket`); `.../*` = objects (for `GetObject`). Wrong one → "download works but `aws s3 ls` = AccessDenied" or vice-versa.

**ARN:** `arn:partition:service:region:account:resource`. IAM/S3 = empty region.

**Condition keys:** `aws:MultiFactorAuthPresent`, `aws:SourceIp`, `aws:RequestedRegion`, `aws:PrincipalOrgID`, `aws:PrincipalTag`/`aws:ResourceTag` (ABAC), `aws:SecureTransport`, `sts:ExternalId`, `aws:SourceArn`/`aws:SourceAccount`.

**Evaluation:** Explicit Deny → Explicit Allow → implicit default Deny. **Explicit Deny always wins** (even over Admin). Full chain: any Deny → SCP → permission boundary → session policy → identity OR resource policy → else Allow. **SCPs and boundaries only subtract, never grant.**

**RBAC vs ABAC:** RBAC = policy per job function (grows with org). ABAC = tag-driven (`aws:ResourceTag/Team = ${aws:PrincipalTag/Team}`), one policy scales to any number of teams. ABAC needs 3 pieces: (1) the policy, (2) how principal gets its tag (static role tag, or **session tags** needing `sts:TagSession` in trust policy, or IdP mapping), (3) **control who can set tags** (else self-grant). Gotchas: not every service supports `aws:ResourceTag` on every action (real designs = ABAC+RBAC); **tags case-sensitive**; untagged = invisible/denied (pair with tag-on-create).

**Condition operators:** `StringEquals`/`StringLike` (wildcards), `ArnEquals`/`ArnLike`, `Bool`, `IpAddress`, `NumericLessThan`/`DateGreaterThan`, `Null` (key present?). Modifiers: `...IfExists` (enforce only if present); `ForAllValues:`/`ForAnyValue:` (**`ForAllValues:` also true when key absent** → pair with `Null`).

**Policy variables:** `${aws:username}` etc. — one policy, private folder per user.

**Limits:** managed policy 6,144 chars; 10 managed attached (raisable). Hitting limit → several focused policies.

**Writing from scratch:** list exact API calls (from code/CloudTrail) → exact ARNs → conditions → test in **policy simulator** → refine via **Access Advisor** / Access Analyzer generate-from-CloudTrail.

**❗ `NotAction` footgun:** `Allow`+`NotAction` = accidental admin (everything except X). Only useful with `Deny` (region lock).

### IAM Roles, Policies, AssumeRole

**Role** = identity with permissions but **no permanent creds**, belongs to nobody; anything trusted assumes it for temporary expiring creds. "A uniform on a hook — put it on, get its powers, take it off; nothing to leak/rotate."

**Trust vs Permission policy:**
| | Trust policy | Permission policy |
|---|---|---|
| Lives | On role (Trust relationships tab) | Attached to role |
| Answers | Who can assume? | What can it do once assumed? |
| `Principal` | ✅ Required | ❌ Not allowed |
| `Resource` | ❌ (role *is* the resource) | ✅ Required |
| Action | `sts:AssumeRole`/`...WithWebIdentity`/`...WithSAML`/`sts:TagSession` | Service APIs |

Both required, evaluated separately. Every role has exactly one trust policy (mandatory). Identify at a glance: identity = Resource/no Principal; trust = Principal/no Resource; bucket/queue/key = both. `aws:PrincipalOrgID` trusts any org account without enumerating.

**AssumeRole mechanics:** caller auths → STS checks target trust policy → issues temp creds (15 min–12 h) → role's permission policy governs. Trust checked ONCE at assume; permissions checked EVERY call.

**Cross-account = two-key lock (AND, not OR):** GATE 1 = caller's own identity policy must allow `sts:AssumeRole` on role ARN; GATE 2 = target role's trust policy names+conditions pass; GATE 3 = target role's permission policy. Diagnose: can't assume at all → trust (G2) + caller's `sts:AssumeRole` (G1); assumed but denied → permission policy.

**`Principal` targets:** `:root` (any principal in account) · `:role/AppRole` (right default; ⚠ delete+recreate breaks trust — new principal ID) · `Service:lambda.amazonaws.com` (execution role) · `Federated:...oidc-provider` (⚠ MUST pair with `sub` condition or any repo can assume).

**Grants vs ceilings:** GRANTS (identity policies + resource policy) add power; CEILINGS (SCP account / boundary role / session policy session) only subtract. `EFFECTIVE = grant ∩ every ceiling`. A boundary with `AdministratorAccess` grants nothing (attach only a boundary → role can do nothing).

**Session policies:** passed at assume time (`--policy` 2048 chars / `--policy-arns` ≤10), this-session-only, invisible in console, can't grant beyond role. One broad role, many narrow sessions (multi-tenant clamp to one tenant's prefix).

**Diagnosis by symptom:** can't assume → trust (G2)+caller `sts:AssumeRole` (G1) · assumed but all denied → no permission policy (maybe only a boundary) · allowed but denied → ceiling clamping or explicit Deny · works one account not another → SCP or target resource policy · worked an hour then broke → creds expired / role chaining (hard 1h cap) · console right but fails → session policy.

**Four docs, cross-account (Lambda A→DynamoDB B):** source role trust (`Service:lambda`, no B) + source permission (`sts:AssumeRole` on B's role ARN — **G1**, pin ARN not `*`, no `dynamodb:*`) + target trust (`Principal` = A's role ARN — **G2**) + target permission (`dynamodb:GetItem/Query` on table — **G3**, `/index/*` ARN **required** for GSI Query). A's permission names B's role; B's trust names A's role. The call uses two clients (creds change hands); `RoleSessionName` (put request ID) = what B's CloudTrail shows.

**Traps:** target perms on source role (most common) · recreating trusted role breaks trust (principal ID) · Terraform circular dependency (build ARN as string / bootstrap with `:root`) · re-assuming every invoke (cache in warm container).

**External ID:** `Condition:{StringEquals:{sts:ExternalId:"vendor-id"}}` — prevents confused-deputy for third-party vendors.

**GitHub OIDC (keyless CI/CD):**
```json
{ "Effect":"Allow","Principal":{"Federated":"arn:...:oidc-provider/token.actions.githubusercontent.com"},
  "Action":"sts:AssumeRoleWithWebIdentity",
  "Condition":{"StringEquals":{"token.actions.githubusercontent.com:aud":"sts.amazonaws.com"},
    "StringLike":{"token.actions.githubusercontent.com:sub":"repo:my-org/my-repo:*"}}}
```

**Temp creds = 3 parts** (`AccessKeyId`, `SecretAccessKey`, **`SessionToken`**) + expiry. Propagate `AWS_SESSION_TOKEN`.

**STS APIs:** `AssumeRole`, `AssumeRoleWithSAML`, `AssumeRoleWithWebIdentity` (OIDC), `GetSessionToken` (MFA for IAM user), `GetFederationToken`, `GetCallerIdentity` (no perms needed → always works as probe). Session default 1h (role max 1–12h); **role chaining hard-capped at 1h** ("batch job dies at 60 min").

**Role → compute delivery:** EC2 = **instance profile** (holds exactly one role; console creates silently, CLI/CFN/Terraform you create it → "role exists but instance can't use it"); Lambda = execution role; ECS = **task execution role** (agent: ECR pull, logs) vs **task role** (app code); EKS = **IRSA / Pod Identity**. Attach/swap instance profile on running instance — takes effect ~minutes, no reboot.

**IMDS:** SDK fetches creds from `169.254.169.254` and auto-refreshes. **IMDSv1 = plain GET** → any SSRF steals role creds (Capital One). **IMDSv2 requires PUT for a token first** → set `HttpTokens: required`, `HttpPutResponseHopLimit: 1`.

**Service-linked roles:** owned by a service, you can't edit. **`iam:PassRole`:** "hand a role to a service" (vs `AssumeRole` = become one). Unrestricted + `lambda:CreateFunction` = full admin escalation. Scope to specific ARNs + `iam:PassedToService` condition.

**IAM Identity Center** = modern human access (permission sets → roles per account, `aws sso login`, one offboarding place). "Should we still create IAM users?" → **No for humans** — Identity Center/federation; IAM users only for legacy apps that can't assume a role + one break-glass.

Mental model: "Trust = who enters the building. Permission = which rooms. AssumeRole = the temporary badge at the door."

### Least Privilege & Permission Boundaries

Anti-pattern `dynamodb:*` on `*` → scope action + resource + `dynamodb:LeadingKeys = ${aws:PrincipalTag/TenantId}`. **Permission boundary** = on a role/user, max it can ever have (platform team lets app teams create roles within a ceiling). **SCP** = at Organizations OU/account level. Neither grants.

---

## 4. Secrets & Parameters

### Secrets Manager vs Parameter Store

| | Secrets Manager | SSM Parameter Store |
|---|---|---|
| Cost | Per-secret + per-call | Standard free; Advanced small |
| Rotation | **Built-in** (RDS/custom Lambda) | None (build it) |
| Encryption | KMS always | KMS optional (SecureString) or plaintext |
| Max size | 64 KB | 4 KB / 8 KB |
| Cross-region | Native | Manual |

Secrets Manager for rotating credentials; Parameter Store for config/non-rotating secrets (cost at scale). Over-using Secrets Manager for pure config is a common cost mistake.
```csharp
var r = await new AmazonSecretsManagerClient().GetSecretValueAsync(new(){SecretId="prod/orders/db"});
var connStr = r.SecretString;
```
**Pitfalls:** (1) **rotation can take you down** — app reading secret once at startup keeps stale password → fails at 3 a.m. Fix: **re-fetch on auth failure** (or RDS Proxy / IAM DB auth). Rotation Lambda = `createSecret→setSecret→testSecret→finishSecret`, two-user strategy. (2) don't call `GetSecretValue` per request (throttle + cost) → **cache with TTL** (caching lib / Parameters-and-Secrets extension). (3) cost at scale (~$0.40/secret/mo + calls). (4) CMK needs both `secretsmanager:GetSecretValue` **and `kms:Decrypt`**. (5) private subnet needs the **interface** endpoint (no gateway option). (6) deletion has 7–30 day recovery window. (7) injected secrets (ECS `valueFrom`, Lambda env) resolved **once at start** — rotation doesn't update running tasks. (8) rotation Lambda needs DB network access. (9) cross-region replicas read-only. (10) env-var secrets visible in `docker inspect`. **One-liner:** *"Secrets Manager for rotating creds, Parameter Store for config — cache with TTL, re-fetch on auth failure; the real incident is a rotated secret the app never re-read."*

---

## 5. Tooling, Pitfalls & Drills

### IAM Security Tools

- **Credential Report** (CSV): per-user MFA, key age, key last-used → find no-MFA users, stale/never-used keys.
- **Access Advisor** (last-accessed): services granted but unused → route to least privilege.
- **IAM Access Analyzer:** external-exposure findings, policy validation, unused-access findings, **generate policy from CloudTrail** (best "how do you write a tight policy?").
- **Policy Simulator:** "would X be allowed?" without calling, shows which statement decided.
- **CloudTrail:** who/when/IP/session. **AWS Config:** continuous compliance rules (`iam-user-mfa-enabled`, `access-keys-rotated`).

### Pitfalls

Trust vs permission confusion · overusing Admin · forgetting Deny wins · hardcoding creds · single-AZ Lambda ENIs · one role many services · `Principal:"*"` · no conditions · ignoring boundaries · assuming roles don't expire · deep chaining · no AssumeRole audit · forgetting resource policy also required · poor naming (`service-env-purpose-role`) · set-and-forget · **unrestricted `iam:PassRole`** · **IMDSv1 left on** · stale env-var creds shadowing role · task vs execution role · root keys · assuming Admin can do everything.
**Golden debug checklist:** execution role perm → trust policy → resource-based policy → KMS key policy → SCP/boundary.

### Rapid-Fire

IAM global (eventually consistent). User=person/long creds; Group=perm container (users only, no nesting, not a Principal); Role=no perm creds, assumed temporarily. New user = nothing. Deny wins always. Role > keys: nothing on box, temp auto-refreshed, central change, nothing to leak. Two role policies: trust (`Principal`) vs permission; `sts:AssumeRole` fails → trust; assumed but API fails → permission. EC2→S3: role trusting `ec2.amazonaws.com` + scoped policy + **instance profile**. Cross-account 2 ways (AssumeRole vs resource policy). STS returns 3 parts + expiry (15 min–12 h; chaining 1h cap). Inline vs managed → customer-managed. Boundary vs SCP (both only subtract). Confused deputy → External ID / SourceArn+SourceAccount. `iam:PassRole` = escalation. Admin ≠ everything (root-only). Enforce MFA for CLI = policy condition + GetSessionToken/AssumeRole token. Find over-permissioned → credential report + Access Advisor + Access Analyzer + simulator. Still create IAM users? No for humans. Batch dies at 60 min → role chaining. EC2 role allows S3 but AccessDenied → `get-caller-identity` (stale env vars), bucket policy, KMS key policy, SCP/boundary, instance profile exists?

**IAM DR:** at risk = roles/policies/trust/Identity Center assignments (global → regional outage doesn't touch; real disaster is a bad apply/deletion). Backup = **IaC only** (Config = timeline, CloudTrail = who). RPO = last commit. **❗ You can break the IAM you need to fix IAM** → keep a **break-glass role** with vaulted MFA outside the pipeline. Recreated role with same name = **new principal ID** (ARN trusts recover, principal-ID trusts stay broken).

---

← [DynamoDB](02-dynamodb.md) · [Index](README.md) · [Infrastructure as Code & CI/CD](04-iac-cicd.md) →
