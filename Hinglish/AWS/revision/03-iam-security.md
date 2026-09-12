> **AWS Quick Revision Notes** · [Index](README.md) · Part I

# IAM & Security

---

## 1. IAM Foundations

### Overview

- IAM decides *who* can do *what* on *which resource*. Building blocks: Identity → Policy → Auth (kaun) vs Authz (kya kar sakte).
- Every API call: **Authentication** (`InvalidClientTokenId` if fails) then **Authorization** (`AccessDenied`).
- **Facts:** IAM **global** (no region), **free**, **eventually consistent** (fresh role/policy may fail first CI attempt → retry). Root = sign-up email, unlimited power.
- **Root rules:** MFA immediately, never daily use, never share, **never create access keys**. Day-one: create admin identity (ideally Identity Center), lock root away.
- **Root-only actions (trick):** close account, change root email/password/support plan, restore IAM billing permission, RI Marketplace seller, S3 MFA Delete / delete deny-all bucket policy, some tax invoices. `AdministratorAccess` ≠ everything.
- **Shared responsibility:** AWS runs IAM securely; you = users/roles/policies, least privilege, rotate credentials, enable+review tooling. One-liner: "AWS guarantees IAM works & is secure as a service; how I configure it is on me."

---

## 2. Identities & Access

### Users, Groups & Permissions

- **User** = person (long-term credentials). **Group** = permission container: only users, **no nesting**, user in multiple groups = union, group = no identity/no Principal.
- **Deny-by-default:** new user with no policy can do nothing.
- **Policy attach:** identity-based (user/group/role, no Principal) vs resource-based (S3/SQS/KMS/Lambda, Principal mandatory).
- **Cross-account 2 ways:** (1) **Resource-based policy** — caller keeps identity, both sides must allow. (2) **AssumeRole** — caller *becomes* role, only role's permissions apply. Contrast: "resource policy = tum khud raho / AssumeRole = tum koi aur ban jaao."
- **Audit diff:** resource-policy → Account B's CloudTrail records real identity; AssumeRole → shows `assumed-role/...` (must correlate with A's trail).

### Password Policy

Min length (up to 128), char types, expiration/rotation, reuse prevention. Weak alone — real controls = MFA + Identity Center (eliminate long-lived human creds).

### MFA

- Types: virtual (TOTP app, multiple tokens), FIDO/U2F key (phishing-resistant), hardware TOTP, passkeys/biometrics. Up to **8 per user** (register backup).
- ❗ MFA natively protects **console**; CLI/API needs policy condition (`"Bool":{"aws:MultiFactorAuthPresent":"true"}`) + `sts:GetSessionToken`/`AssumeRole --serial-number/--token-code`.
- Trust policy MFA requirement = standard cross-account production control.

### Access to AWS & Access Keys

- 3 doors, 1 API (SigV4): Console (password+MFA), CLI (access key/temp), SDK (role).
- Access key ID ≈ username, secret ≈ password (shown once). **Max 2 keys per user** (for zero-downtime rotation). Never commit/bake/share.
- Best practice: **don't use keys** where role works (EC2/ECS/Lambda/GitHub Actions).
- `aws sts get-caller-identity` = single most useful IAM debug.
- **CloudShell** = browser terminal, no keys on laptop.
- **Credential provider chain (order):** CLI params → **env vars** → shared file → container creds → **EC2 IMDS** (last). ❗ Stale `AWS_ACCESS_KEY_ID` env var silently shadows instance role → `get-caller-identity` reveals `user/...` not `assumed-role/...`.

---

## 3. Policies, Roles & Least Privilege

### Policy Types & Structure

| Type | Manage | Use |
|---|---|---|
| AWS managed | AWS | Quick start (too broad) |
| **Customer managed** | You | ✅ Real work — versioned (5), reusable, visible |
| Inline | You | Rare one-offs (avoid — invisible to audits) |

```json
{ "Version": "2012-10-17", "Statement": [{
  "Sid": "...", "Effect": "Allow",
  "Action": ["s3:GetObject","s3:ListBucket"],
  "Resource": ["arn:aws:s3:::my-bucket","arn:aws:s3:::my-bucket/*"],
  "Condition": {"Bool": {"aws:MultiFactorAuthPresent": "true"}} }] }
```
- `Version` = **language** version (always `2012-10-17`, not policy version). `Principal` only in resource/trust policies. `NotAction` = "all except" (footgun with `Allow`).
- ❗ **`bucket` vs `bucket/*`:** bucket ARN for `ListBucket`, `/*` for `GetObject`.
- **ARN:** `arn:partition:service:region:account:resource`. IAM/S3 = empty region.
- **Condition keys:** `aws:MultiFactorAuthPresent`, `aws:SourceIp`, `aws:RequestedRegion`, `aws:PrincipalOrgID`, `aws:PrincipalTag`/`ResourceTag` (ABAC), `aws:SecureTransport`, `sts:ExternalId`, `aws:SourceArn`/`SourceAccount`.
- **Evaluation:** Explicit Deny → Explicit Allow → default Deny. **Deny always wins.** Full: any Deny → SCP → boundary → session policy → identity OR resource allow → else Allow. **SCPs/boundaries only subtract, never grant.**
- **RBAC vs ABAC:** ABAC = tag-driven (`aws:ResourceTag/Team = ${aws:PrincipalTag/Team}`), scales to N teams with 1 policy. Needs: policy + principal tag (static role tag or session tag with `sts:TagSession`) + control who can set tags. Gotchas: not all services support `aws:ResourceTag` (→ ABAC+RBAC combined); tags case-sensitive; untagged = invisible/denied.
- **Wildcards:** `*` any, `?` one char; `s3:Get*` silently grants future APIs → explicit lists safer.
- **Modifiers:** `...IfExists` (enforce only if key present); `ForAllValues:`/`ForAnyValue:` (set operators; `ForAllValues:` **true if key absent** → pair with `Null`).
- **Policy variables:** `${aws:username}` in resource → per-user folders.
- **Limits:** managed 6,144 chars; 10 managed policies attached (default). Several focused policies > one giant.
- **Write a policy:** list API calls (CloudTrail) → exact ARNs → conditions → test in simulator → refine via Access Advisor / generate-from-CloudTrail.

### IAM Roles & AssumeRole

- Role = identity with permissions, **no permanent credentials**, assumable by trusted entity → temporary creds (STS). Analogy: uniform on a hook.
- **Trust policy vs Permission policy:**

| | Trust Policy | Permission Policy |
|---|---|---|
| Where | Role itself | Attached to role |
| Answers | *Kaun* assume kar sakta? | Assume ke baad *kya*? |
| Principal | ✅ Required | ❌ Not allowed |
| Resource | ❌ (role hi resource) | ✅ Required |
| Action | `sts:AssumeRole`/`...WithWebIdentity`/`...WithSAML`/`sts:TagSession` | service APIs |

- Both required, separately evaluated. Role assume nahi ho raha → trust policy; assumed but call denied → permission policy.
- **`aws:PrincipalOrgID`** trusts whole org without enumerating accounts.
- **AssumeRole mechanics:** caller auth → STS checks trust policy → issues temp creds (AccessKey + Secret + **SessionToken**, 15min-12h) → permission policy governs.
- **Cross-account = two-key lock:** Gate 1 (caller's identity policy `sts:AssumeRole` on RoleB) AND Gate 2 (RoleB trust policy names caller) — **AND, not OR**. Gate 3 = RoleB permission policy.
- **Principal targets:** `:root` (any principal in account), `role/AppRole` (specific — right default; delete+recreate breaks trust via new principal ID), `Service` (execution role), `Federated` (IdP — ❗ MUST condition on `sub` else any repo can assume).
- **Grants vs ceilings:** grants = identity + resource policies; ceilings = SCP + boundary + session policy. `EFFECTIVE = grant ∩ ceilings`. Boundary with `AdministratorAccess` grants nothing alone.
- **Role attachments:** trust policy (1, mandatory), managed (10→20), inline (10,240 chars), boundary (0-1). **Session policies** = passed at assume-time (`--policy`/`--policy-arns`), invisible in console, can't grant beyond role.
- **Diagnose by symptom:** assume fails → trust+caller's AssumeRole; assumed all deny → no permission policy (only boundary); allow but deny → ceiling/explicit Deny; works one account not other → SCP/resource policy; broke after 1hr → creds expire or **role chaining (hard 1-hour cap)**; console-fine-but-fail → session policy.

**4-doc cross-account example (Lambda A→DynamoDB B):**
- ① Source role trust (A): `Principal: lambda.amazonaws.com` (B not mentioned).
- ② Source role permission (A): `sts:AssumeRole` on RoleB ARN (Gate 1) + own logging. Pin exact ARN, no `dynamodb:*`.
- ③ Target role trust (B): `Principal: <A's role ARN>` + `sts:AssumeRole`,`sts:TagSession` + `aws:PrincipalOrgID` (Gate 2).
- ④ Target role permission (B): `dynamodb:GetItem`,`Query` on `table/Deals` + **`/index/*`** (Gate 3 — GSI needs index ARN).

```csharp
var sts = new AmazonSecurityTokenServiceClient();
var assumed = await sts.AssumeRoleAsync(new AssumeRoleRequest {
    RoleArn = "arn:aws:iam::222222222222:role/DealsTableReaderRole",
    RoleSessionName = $"order-export-{context.AwsRequestId}" });   // shows in B's CloudTrail
var ddb = new AmazonDynamoDBClient(assumed.Credentials);
```
**Traps:** target permissions on source role; recreate trusted role (new principal ID breaks trust); Terraform circular dep; re-assume per invocation (cache creds).

- **External ID:** vendor access, `sts:ExternalId` condition → confused-deputy fix.
- **GitHub OIDC:** `Federated: oidc-provider/token.actions.githubusercontent.com`, `sts:AssumeRoleWithWebIdentity`, condition `aud=sts.amazonaws.com` + `StringLike sub: repo:my-org/my-repo:*`. No static keys.
- **STS APIs:** `AssumeRole` (main), `AssumeRoleWithSAML`, `AssumeRoleWithWebIdentity`, `GetSessionToken` (IAM user MFA), `GetFederationToken`, `GetCallerIdentity` (no perms needed).
- **Session duration:** default 1h, role max 1-12h; **role chaining hard-capped 1h**. Pass meaningful `--role-session-name`.
- **Role delivery to compute:** EC2 = **instance profile** (wrapper, 1 role; console auto-creates, IaC must declare — common bug); Lambda = execution role; ECS = **task role (app) + task execution role (pull/logs)**; EKS = **IRSA / Pod Identity**.
- **IMDS:** creds at `169.254.169.254`. ❗ **IMDSv1 = plain GET** (SSRF steals creds — Capital One); **IMDSv2 = PUT for token first**. Set `HttpTokens: required`, `HttpPutResponseHopLimit: 1`.
- **`iam:PassRole`** = hand role to service (vs AssumeRole = become). Unrestricted + `lambda:CreateFunction` = full escalation. Scope to specific ARNs + `iam:PassedToService`.
- **IAM Identity Center (ex-SSO)** = modern human access; permission sets → roles per account; `aws sso login` short-lived; one place to offboard. "Still create IAM users?" → no for humans; only legacy apps + break-glass.
- Mental model: "Trust = building mein enter kaun; Permission = andar konse rooms; AssumeRole = temporary badge."

### Least Privilege & Boundaries

- Anti-pattern: `dynamodb:* on *`. Least-priv: specific actions + table ARN + `ForAllValues:StringEquals dynamodb:LeadingKeys = ${aws:PrincipalTag/TenantId}`.
- **Boundary** = on role/user (max ceiling); **SCP** = on account/OU. Both only subtract.

---

## 4. Secrets & Parameters

### Secrets Manager vs Parameter Store

| | Secrets Manager | Parameter Store |
|---|---|---|
| Cost | Per-secret + API | Standard free |
| Rotation | Built-in | No native |
| Max size | 64 KB | 4/8 KB |
| Use | DB creds, rotating keys | Config, feature flags |

- **Pitfalls:** ❗ rotation can break app (cache + `AWSCURRENT`/`AWSPREVIOUS` — re-fetch on auth failure, prefer RDS Proxy/IAM DB auth); don't `GetSecretValue` per request (cache w/ TTL — Lambda extension); scale cost; CMK needs IAM **and** `kms:Decrypt`; private subnet needs interface endpoint; 7-30 day deletion window; injected secrets resolve only at start; rotation Lambda needs DB network access; replicas read-only; env-var secrets visible in `docker inspect`.
- One-liner: rotating creds → Secrets Manager, config → Parameter Store, cache w/ TTL + re-fetch on auth failure.

---

## 5. Tooling, Pitfalls & Drills

### IAM Security Tools

- **Credential Report** (CSV): per-user MFA, key age, last-used → find stale keys / missing MFA.
- **Access Advisor:** granted services + last-used → downscope.
- **Access Analyzer:** external exposure findings, policy validation, unused-access, **generate policy from CloudTrail** (best "tight policy" answer).
- **Policy Simulator:** test before shipping.
- **CloudTrail:** who did what.
- **AWS Config:** continuous compliance rules.

### Pitfalls & Debugging

- **Golden checklist "role has permission but access fails":** execution role permission → trust policy → resource-based policy → KMS key policy → SCP/boundary.
- Key pitfalls: trust vs permission confusion; `AdministratorAccess` overuse; forgetting Deny wins; hardcoded creds; single-AZ VPC Lambda ENIs; role reuse; `Principal:*`; no conditions; unrestricted `PassRole`; IMDSv1 left on; stale env vars; task vs execution role; root access keys.

### Rapid-Fire Q&A (key)

- IAM global, eventually consistent. New user = nothing. Explicit Deny always wins. Role > keys on EC2 (nothing on box, temp, central change, auto-refresh). Cross-account 2 ways. STS = 3 parts (SessionToken). Customer-managed > inline. Boundary vs SCP both subtract. Confused deputy → External ID. `PassRole` = escalation. `AdministratorAccess` ≠ root-only actions. MFA CLI needs policy condition. Role chaining dies at 60min. EC2 AccessDenied → `get-caller-identity` (env-var shadow) → bucket policy → KMS → SCP/boundary → instance profile exists?

**DR — IAM:** IAM global (regional outage doesn't touch); risk = you (bad apply/delete). Backup = **IaC only** (no snapshot); Config = what changed, CloudTrail = who. RPO = last commit. ⚠️ Keep **break-glass role** (vaulted MFA, not managed by the deleting pipeline); recreated role gets new principal ID.

---

← [DynamoDB](02-dynamodb.md) · [Index](README.md) · [Infrastructure as Code & CI/CD](04-iac-cicd.md) →
