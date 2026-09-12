> **AWS Detailed Guide** · [Index](README.md) · Part I

# IAM & Security

---

## 1. IAM Foundations

### IAM Overview, Root Account & Shared Responsibility

**Why IAM exists:** AWS secures the infrastructure (shared responsibility model); you control *who* can do *what* on *which resource*. IAM does not store data, process requests, or run workloads — it only decides permission.

**Three building blocks:** Identity (User/Role/Service) → Policy (JSON rules) → Authentication (who are you) vs Authorization (what can you do).

**Every single AWS API call answers two questions, in this order:**
1. **Authentication** — *who are you?* Do you hold valid credentials (password, access key, or a temporary STS token)?
2. **Authorization** — *are you allowed to do this?* Does some policy permit this action on this resource?

Both must pass. Fail authentication and you get an `InvalidClientTokenId`/signature error; fail authorization and you get `AccessDenied` — telling those two apart is the first step in any IAM debugging session.

**Facts interviewers use as warm-up questions:**
| Fact | Detail |
|---|---|
| **IAM is global, not regional** | You never pick a region for IAM. One set of users/roles/policies works in every region. (Contrast with almost everything else in this guide.) |
| **IAM is free** | No charge for users, roles, groups, or policies. |
| **Eventually consistent** | IAM data is replicated worldwide, so a brand-new policy or role can take a few seconds to take effect everywhere. This is why CI/CD pipelines that create a role and immediately use it sometimes fail on the first attempt and succeed on retry — a real gotcha worth naming. |
| **Root account** | Created with the AWS account, identified by the sign-up email, and has unlimited power that policies cannot restrict. |

**Root account rules (say all four):** enable MFA on it immediately; never use it for daily work; never share it; **never create access keys for it** (AWS now actively blocks/warns on this). Day-one task on a new account: create an admin identity (ideally an IAM Identity Center user — see [IAM Roles](#iam-roles-policies-assumerole)), verify you can use it, then lock root away.

**Things *only* root can do** (a favourite trick question, because the instinct is "AdministratorAccess can do everything" — it can't):
- Close the AWS account
- Change the account name, root email, or root password
- Change or cancel the AWS Support plan
- Restore an IAM user's permission to manage billing after it's been revoked
- Register as a seller in the Reserved Instance Marketplace
- Enable MFA Delete on an S3 bucket, or delete an S3 bucket policy that denies all principals (the classic "I locked myself out of my own bucket" recovery)
- View certain tax invoices

**Shared Responsibility Model for IAM** — AWS secures the cloud, you secure what you put in it:
| AWS is responsible for | You are responsible for |
|---|---|
| Running IAM as a global, highly available service | Creating and organising users, groups, roles, policies |
| Patching and securing the underlying infrastructure | Applying **least privilege** (see [Least Privilege & Permission Boundaries](#least-privilege--permission-boundaries-in-practice)) |
| Vulnerability analysis and compliance validation of the service itself | Rotating and protecting credentials; deleting unused ones |
| Providing the tooling (MFA support, Access Analyzer, credential reports, CloudTrail) | **Enabling and actually reviewing** that tooling |
| Tenant isolation — never leaking your data to another customer | Removing leavers; auditing who can do what |

**One-liner to recite:** "AWS guarantees IAM works and is secure *as a service*; how I configure it is entirely on me. AWS will happily let me write `Action: *` on `Resource: *` — that's my problem, not theirs."

---

## 2. Identities & Access

### Users, Groups & Permissions

**IAM User** = one physical person (or one legacy application that genuinely cannot use a role). Holds **long-term credentials**: a console password and/or access keys. These do not expire on their own, which is exactly why roles are preferred for anything automated.

**IAM Group** = a container used only to attach permissions to many users at once. The rules interviewers test:
- A group contains **only users** — ❌ **a group cannot contain another group** (no nesting).
- A user can be in **multiple groups**; the effective permissions are the **union** of all of them (plus anything attached directly to the user).
- A user can be in **zero groups** (allowed, not recommended).
- A group is **not an identity**: you cannot log in "as a group", and a group ARN cannot appear as a `Principal` in a policy. Only users, roles, and services can be principals.

```
Account
├── Group: Developers  → [Parteek, Ravi]
├── Group: Operations  → [Ravi, Sara]     ← Ravi is in two groups: permissions add up
└── Group: Audit       → [Sara]
```

**Why groups matter:** attach the policy once to the group instead of 50 times to 50 users. Joiner → add to group. Leaver → remove. Permissions stay consistent and auditable, which is the whole point.

**Permissions are deny-by-default.** A brand-new IAM user with no policy can do *nothing* — it cannot even list S3 buckets or see the EC2 dashboard. Nothing in AWS is implicitly allowed; every permission is something you deliberately granted. This is the correct answer to "what can a new user do out of the box?"

**Policy types by what they attach to** — **identity-based** (on a User, Group or Role; no `Principal`, since the holder *is* the principal) versus **resource-based** (on the resource itself — S3 bucket, SQS queue, KMS key, Lambda; `Principal` is mandatory). Full treatment in [Policy Types & Structure](#policy-types--structure). What matters *here* is how the two behave across account boundaries:

**Same-account vs cross-account (a precise distinction worth getting right):**
- **Same account** — either an identity-based policy *or* a resource-based policy allowing the action is sufficient.
- **Cross-account via a resource-based policy** — the caller keeps their own identity and calls the resource directly; the resource policy must name them, **and** their own account must allow them to make the call. Both sides. Applies to S3, SQS, SNS, KMS, Lambda.
- **Cross-account via AssumeRole** — the caller *becomes* the role and gives up their original permissions for that session. Only the role's permissions apply. See [IAM Roles](#iam-roles-policies-assumerole).

That "resource policy = you stay yourself; AssumeRole = you become someone else" contrast is the crispest way to answer "what are the two ways to do cross-account access?"

**The two paths, side by side.** Scenario throughout: an app in **Account A (`111111111111`)** needs to read objects from the `partner-exports` bucket in **Account B (`222222222222`)**.

**Path 1 — resource-based policy (the caller stays itself).** Both sides are required, and the second half is the one people forget.

*Account B* — bucket policy, naming the caller as `Principal`:
```json
{
  "Version": "2012-10-17",
  "Statement": [{
    "Sid": "AllowAccountAExportReader",
    "Effect": "Allow",
    "Principal": { "AWS": "arn:aws:iam::111111111111:role/ExportReader" },
    "Action": ["s3:GetObject", "s3:ListBucket"],
    "Resource": ["arn:aws:s3:::partner-exports",
                 "arn:aws:s3:::partner-exports/*"]
  }]
}
```
*Account A* — identity policy on `ExportReader`, allowing it to make the call at all:
```json
{
  "Effect": "Allow",
  "Action": ["s3:GetObject", "s3:ListBucket"],
  "Resource": ["arn:aws:s3:::partner-exports",
               "arn:aws:s3:::partner-exports/*"]
}
```
The call itself needs no STS — the app uses its own credentials against B's bucket directly:
```csharp
var s3 = new AmazonS3Client();               // no AssumeRole anywhere
var obj = await s3.GetObjectAsync("partner-exports", "deals/2026-08-23.json");
```

**Path 2 — AssumeRole (the caller becomes someone else).** Same three pieces, different shape: *Account B* has a **trust policy** saying who may become the role plus a permission policy saying what the role may do; *Account A* needs `sts:AssumeRole` on that role ARN; then the caller trades its identity for temporary credentials. Full walkthrough and the C# call are under [IAM Roles, Policies, AssumeRole](#iam-roles-policies-assumerole).

**Choosing between them:**

| | Resource-based policy | AssumeRole |
|---|---|---|
| Identity the resource sees | The caller's own | The assumed role |
| Permissions in effect | Caller's **∩** resource policy | **Only** the role's — the caller's are dropped for that session |
| Extra API call | None | `sts:AssumeRole`; credentials expire and must be refreshed |
| Available for | Only services that *have* resource policies — S3, SQS, SNS, KMS, Lambda, Secrets Manager, API Gateway (and DynamoDB since 2024) | Anything, including services with no resource policy |
| Best for | One narrow, durable grant — a partner reading one bucket or queue | Broad or multi-service access; anything needing session tagging or an External ID |

⚠️ **The audit difference, which is the detail that separates a good answer here:** with a **resource-based policy**, CloudTrail in Account B records the caller as `arn:aws:iam::111111111111:role/ExportReader` — the *real* identity, in the target account's own trail. With **AssumeRole**, B's trail shows `arn:aws:sts::222222222222:assumed-role/CrossAccountReadRole/ExportSession`, so identifying who actually did it means correlating with the `AssumeRole` event in Account A's trail. If a compliance requirement is "the target account must be able to attribute every access on its own", that argues for the resource-policy path.

### Password Policy

**Password policy** (IAM → Account settings) — what you can enforce:
- Minimum length (AWS allows up to 128 characters)
- Required character types: uppercase, lowercase, number, non-alphanumeric
- Allow or prevent users changing their own password
- **Expiration** after N days (forced rotation), and whether an expired password locks the user out or lets them self-reset
- **Prevent reuse** of the last N passwords

**Why it's asked:** it's the cheapest defence against brute-force and credential-stuffing. But say the honest senior version too — a password policy alone is weak; the real controls are **MFA** (below) and eliminating long-lived human credentials entirely via IAM Identity Center.

### MFA (Multi-Factor Authentication)

**The idea in one line:** a password can be stolen, phished, or leaked; MFA adds *something you physically have*, so a stolen password alone is useless.

Mandatory for the root account, and expected on every human user with console access.

| Device type | What it is |
|---|---|
| **Virtual MFA device** | TOTP app on a phone/laptop — Google Authenticator, Authy, Duo Mobile. One device can hold **multiple tokens** (root plus several users). Free, and the common choice. |
| **FIDO / U2F security key** | Physical USB key such as a YubiKey. One key can serve **multiple root accounts and IAM users**. Phishing-resistant, which TOTP is not. |
| **Hardware TOTP token** | Key fob or display card (Gemalto; SurePassID for AWS GovCloud). Used where phones aren't allowed on the floor. |
| **Passkeys / biometrics** | FIDO2 — Face ID, Touch ID, Windows Hello. |

Details that separate a real answer from a memorised one:
- AWS supports **multiple MFA devices per user (up to 8)** — register a backup so a lost phone isn't a lockout.
- MFA natively protects **console sign-in**. For **CLI/API** you enforce it with a policy condition (`"Bool": {"aws:MultiFactorAuthPresent": "true"}`) and the caller obtains an MFA-backed session via `sts:GetSessionToken` (for IAM users) or `sts:AssumeRole` with `--serial-number`/`--token-code`. "MFA covers the console but the CLI needs a policy condition" is the point most candidates miss.
- Requiring MFA on `sts:AssumeRole` in a **trust policy** is the standard control for cross-account production access.
- Lost device: root recovers via registered email + phone verification; for an IAM user an admin deactivates the old device and assigns a new one.

### Access to AWS: Console, CLI, SDK & Access Keys

Three front doors, one back end — everything ultimately calls the same **AWS REST API** with SigV4-signed requests:
| Route | Credential | Best for |
|---|---|---|
| **Management Console** | Username + password (+ MFA) | Humans, exploration, one-off fixes |
| **AWS CLI** | Access key ID + secret (or temporary role credentials) | Scripting, automation, debugging |
| **AWS SDK** (.NET, Python, JS…) | Same, but normally supplied by a role | Application code |

**Access keys — the risky part of IAM:**
- Access key ID ≈ username, secret access key ≈ password. The **secret is displayed once**; lose it and you delete the key and create a new one.
- **Maximum 2 access keys per user**, and that limit is deliberate: it exists to make zero-downtime rotation possible — create key #2 → roll it out → verify → **delete** key #1. (Deactivating and leaving it is the half-done version.)
- Never commit keys to Git, bake them into an AMI or container image, paste them into CI secrets when OIDC is available, or share them between people/services.
- **Best practice is to not use them at all** where a role will do. For a .NET service on EC2/ECS/Lambda, or a GitHub Actions pipeline, there is no good reason for a static key — see [IAM Roles](#iam-roles-policies-assumerole).

```bash
aws configure                    # writes ~/.aws/credentials + ~/.aws/config
aws configure --profile dev      # named profile
aws sts get-caller-identity      # "who am I?" — the single most useful IAM debug command
aws iam list-access-keys --user-name parteek
aws s3 ls --profile dev
```

**AWS CloudShell** — a browser terminal already authenticated as your console identity, so no keys on your laptop at all. Not available in every region; ~1 GB of home-directory persistence.

**Default credential provider chain (order matters, and this is a real debugging trap):**
1. Explicit CLI/SDK parameters
2. **Environment variables** (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_SESSION_TOKEN`)
3. Shared credentials file (`~/.aws/credentials`) / config file profile
4. Container credentials (ECS task role endpoint)
5. **EC2 instance profile via IMDS**

The instance role is **last**. So a stale `AWS_ACCESS_KEY_ID` left in a shell, systemd unit, or Dockerfile silently shadows the EC2/ECS role, and you get `AccessDenied` for permissions your role clearly has. `aws sts get-caller-identity` immediately reveals it — if it prints a `user/...` ARN where you expected `assumed-role/...`, that's your answer.

---

## 3. Policies, Roles & Least Privilege

### Policy Types & Structure

**Three kinds of policy — know which one to recommend:**
| Type | Managed by | Reusable | When to use |
|---|---|---|---|
| **AWS managed** | AWS — auto-updated as AWS adds APIs | Yes | Quick start / broad roles: `ReadOnlyAccess`, `AdministratorAccess`, `AmazonS3ReadOnlyAccess`. Convenient but almost always broader than you need. |
| **Customer managed** | You | Yes — attach to many identities | ✅ **The right answer for real work.** Versioned (up to 5 versions retained, so you can roll back a bad change), reusable, and you can see everywhere it's attached. |
| **Inline** | You | No — embedded in one user/group/role, deleted with it | Rare genuinely one-off grants. Avoid: invisible to audits, impossible to reuse, no version history. |

**Policy anatomy:**
```json
{
  "Version": "2012-10-17",
  "Id": "OrdersBucketReadPolicy",
  "Statement": [
    {
      "Sid": "AllowReadOnOrdersBucket",
      "Effect": "Allow",
      "Action": ["s3:GetObject", "s3:ListBucket"],
      "Resource": [
        "arn:aws:s3:::my-bucket",
        "arn:aws:s3:::my-bucket/*"
      ],
      "Condition": {
        "Bool": { "aws:MultiFactorAuthPresent": "true" }
      }
    }
  ]
}
```

| Field | Meaning | Notes |
|---|---|---|
| `Version` | Policy **language** version | Always `"2012-10-17"`. Not your policy's own version — a common misread. The older `2008-10-17` doesn't support policy variables. |
| `Id` | Optional policy identifier | Rarely used in identity policies |
| `Statement` | One or more permission blocks | Required |
| `Sid` | Statement ID — a label for humans | Optional, but makes CloudTrail/debugging far easier |
| `Effect` | `Allow` or `Deny` | Required |
| `Principal` | *Who* this applies to | **Only** in resource-based policies and trust policies. Never in an identity-based policy. |
| `Action` | API calls as `service:Operation` | Wildcards allowed (`s3:Get*`). `NotAction` = "everything except" — powerful and easy to get wrong. |
| `Resource` | ARNs the actions apply to | Some APIs don't support resource-level permissions and require `"*"` (e.g. many `List*`/`Describe*` calls) — worth knowing so `"*"` isn't automatically treated as sloppiness. |
| `Condition` | Extra rules for *when* the statement applies | Where least privilege actually gets enforced |

**The `bucket` vs `bucket/*` gotcha** (asked constantly): `arn:aws:s3:::my-bucket` is the **bucket** — needed for `s3:ListBucket`. `arn:aws:s3:::my-bucket/*` is the **objects inside** — needed for `s3:GetObject`. Get this wrong and you get "I can download a file if I know its name but `aws s3 ls` returns AccessDenied", or vice-versa.

**ARN format:**
```
arn:partition:service:region:account-id:resource
arn:aws:iam::123456789012:user/parteek              ← IAM is global, so region is empty
arn:aws:s3:::my-bucket/file.txt                     ← S3 names are global, so region+account empty
arn:aws:dynamodb:us-east-1:123456789012:table/Orders
```

**Condition keys worth memorising:**
| Key | Use |
|---|---|
| `aws:MultiFactorAuthPresent` | Require MFA for destructive/sensitive actions |
| `aws:SourceIp` | Restrict to office/VPN CIDR ranges |
| `aws:RequestedRegion` | Pin activity to approved regions |
| `aws:PrincipalOrgID` | Only identities from my AWS Organization |
| `aws:PrincipalTag` / `aws:ResourceTag` | Tag-based access (ABAC, below) |
| `aws:SecureTransport` | Force HTTPS |
| `sts:ExternalId` | Third-party role assumption — see confused deputy in [IAM Roles](#iam-roles-policies-assumerole) |
| `aws:SourceArn` / `aws:SourceAccount` | Narrow a service principal to one specific caller |

**Evaluation order (memorize):** Explicit Deny → Explicit Allow → implicit Default Deny. No matching policy = no access. An explicit Deny **always wins**, even over `AdministratorAccess`.

Full evaluation path once org-level guardrails exist — this is the senior version of the same answer:
```
Request
 ├─ Any explicit Deny anywhere?          → DENY (nothing can override this)
 ├─ SCP (Organizations) permits it?      → no → DENY
 ├─ Permissions boundary permits it?     → no → DENY
 ├─ Session policy permits it?           → no → DENY
 ├─ Identity policy OR resource policy allows it?  → neither → DENY (implicit)
 └─ else                                 → ALLOW
```
Note what this means: **SCPs and boundaries can only take permissions away, never grant them.** See [Least Privilege & Permission Boundaries](#least-privilege--permission-boundaries-in-practice) for boundary-vs-SCP detail.

**RBAC vs ABAC:**
- **RBAC** (role-based) — permissions per job function; you create a new role/policy per team or project. Simple, but the policy count grows with the org.
- **ABAC** (attribute-based) — permissions driven by **tags**: "you may act on a resource whose `Team` tag matches your own `PrincipalTag/Team`". One policy scales to any number of teams/projects without edits. The standard "how would you scale IAM across 200 microservices?" answer.

**A complete ABAC example.** Three pieces have to line up, and the second is where most attempts quietly fail.

*1. The policy* — one policy, attached to every engineer's role, that never needs editing when a team is added:
```json
{
  "Version": "2012-10-17",
  "Statement": [{
    "Sid": "ActOnOwnTeamsInstances",
    "Effect": "Allow",
    "Action": ["ec2:StartInstances", "ec2:StopInstances", "ec2:RebootInstances"],
    "Resource": "arn:aws:ec2:*:*:instance/*",
    "Condition": {
      "StringEquals": { "aws:ResourceTag/Team": "${aws:PrincipalTag/Team}" }
    }
  }]
}
```
Read it as the sentence: *allow these actions on any instance whose `Team` tag equals my own `Team` tag.* Onboard a 30th team tomorrow and this policy does not change — that is the entire value proposition.

*2. How the principal gets its tag* — the step that's usually missing. Two routes:
- **Static** — tag the role itself: `aws iam tag-role --role-name Engineer --tags Key=Team,Value=payments`. Fine when one role serves one team.
- **Dynamic (session tags)** — the caller supplies the tag at assume time, so *one* role serves every team:
  ```
  aws sts assume-role --role-arn arn:aws:iam::111111111111:role/Engineer \
      --role-session-name alice --tags Key=Team,Value=payments
  ```
  This requires **`sts:TagSession`** in the role's trust policy alongside `sts:AssumeRole` — omit it and the call is rejected. With IAM Identity Center or a SAML/OIDC IdP the tag is mapped from a directory attribute instead, which is how this scales for humans.

*3. Control who can set tags — without this, ABAC is not a control at all.* If an engineer can retag an instance, they can grant themselves access to it. So constrain the tagging action too:
```json
{
  "Sid": "OnlyTagWithOwnTeam",
  "Effect": "Allow",
  "Action": ["ec2:CreateTags", "ec2:DeleteTags"],
  "Resource": "arn:aws:ec2:*:*:instance/*",
  "Condition": {
    "StringEquals": { "aws:RequestTag/Team": "${aws:PrincipalTag/Team}" },
    "ForAllValues:StringEquals": { "aws:TagKeys": ["Team", "Environment"] }
  }
}
```

**The before/after that makes the case:**

| | RBAC | ABAC |
|---|---|---|
| 30 teams × 4 environments | ~120 policies to write and maintain | **1 policy** |
| Onboarding a new team | New role + new policy + review cycle | Set one tag |
| Where the risk lives | Policy sprawl and drift | **Tag integrity** — which is why piece 3 is mandatory |

⚠️ **Three gotchas worth naming:**
- **Not every service supports `aws:ResourceTag` on every action.** EC2, RDS, Lambda, DynamoDB tables and S3 objects largely do; many `List*`/`Describe*` calls support no resource-level condition at all and need `"Resource": "*"` with no tag condition. So real designs end up **ABAC combined with RBAC**, not ABAC replacing it — check the service authorization reference before promising a pure-ABAC model.
- **Tag keys and values are case-sensitive.** `Team=Payments` will not match a `PrincipalTag/Team` of `payments`. This is the single most common cause of "the policy looks right but it's denied".
- **Untagged resources are invisible, not open** — the condition simply doesn't match, so access is denied. Pair ABAC with `aws:RequestTag`-enforced tag-on-create so nothing can land untagged in the first place.

#### Anatomy of a Policy Document

**Read every policy as one sentence.** Four fields carry all the meaning:

> **`Effect`** *(allow or deny)* — **`Action`** *(these API calls)* — **`Resource`** *(on these things)* — **`Condition`** *(but only when this is true)*.

So the anatomy example above reads: *"**Allow** `GetObject` and `ListBucket` **on** my-bucket and everything in it, **but only if** the caller authenticated with MFA."* Once you read policies that way, writing them stops being guesswork.

**Building one up, step by step.** Start with the absolute minimum and add precision:

```json
// ❌ Step 1 — works, but it's admin. Never ship this.
{ "Effect": "Allow", "Action": "*", "Resource": "*" }

// ⚠ Step 2 — scope the service. Still every S3 action on every bucket.
{ "Effect": "Allow", "Action": "s3:*", "Resource": "*" }

// ⚠ Step 3 — scope the resource. Still allows delete.
{ "Effect": "Allow", "Action": "s3:*", "Resource": "arn:aws:s3:::my-bucket/*" }

// ✅ Step 4 — scope the actions too. This is least privilege.
{ "Effect": "Allow",
  "Action": ["s3:GetObject", "s3:PutObject"],
  "Resource": "arn:aws:s3:::my-bucket/uploads/*" }
```
Note step 4 also narrows the **prefix** (`uploads/*`), not just the bucket — resource ARNs can be as specific as a path.

**Wildcards** work in `Action` and `Resource`: `*` matches any number of characters, `?` matches exactly one. `s3:Get*` covers `GetObject`, `GetBucketPolicy`, and everything else beginning "Get" — convenient, but it silently grants future APIs AWS adds with that prefix, which is why explicit action lists are safer for anything sensitive.

**Multiple statements** are evaluated **independently**, and the results are combined. There's no ordering and no fall-through — every statement is checked, any `Deny` wins, otherwise any `Allow` grants. So this is a normal, readable shape:
```json
"Statement": [
  { "Sid": "ReadWholeBucket", "Effect": "Allow",
    "Action": ["s3:GetObject", "s3:ListBucket"],
    "Resource": ["arn:aws:s3:::my-bucket", "arn:aws:s3:::my-bucket/*"] },

  { "Sid": "NeverTouchTheArchive", "Effect": "Deny",
    "Action": "s3:*",
    "Resource": "arn:aws:s3:::my-bucket/archive/*" }
]
```
Broad allow + narrow deny is the standard **guardrail** pattern, and it works precisely because Deny always beats Allow.

**❗ `NotAction` / `NotResource` — the footgun the field table warns about.** `NotAction` means "everything **except** these".
- With **`Deny`** it's genuinely useful — the classic region lock:
```json
{ "Effect": "Deny",
  "NotAction": ["iam:*", "sts:*", "route53:*", "cloudfront:*", "support:*"],
  "Resource": "*",
  "Condition": { "StringNotEquals": { "aws:RequestedRegion": ["us-east-1", "ap-south-1"] } } }
```
*"Deny everything outside my two approved regions, except the global services that have no region."*
- With **`Allow`** it is almost always a mistake. `{"Effect":"Allow","NotAction":"iam:*","Resource":"*"}` grants **every action in AWS except IAM** — effectively administrator, written in a way that looks restrictive. If you see `Allow` + `NotAction` in a review, treat it as a finding.

**Condition operators** — the `Condition` block is `{ Operator: { key: value } }`, and picking the right operator matters:
| Operator | Use |
|---|---|
| `StringEquals` / `StringNotEquals` | Exact match, case-sensitive |
| `StringLike` / `StringNotLike` | Match **with wildcards** — the one to use for `repo:my-org/my-repo:*` style patterns |
| `ArnEquals` / `ArnLike` | ARN comparison (`ArnLike` allows wildcards) |
| `Bool` | `true`/`false` — e.g. `aws:SecureTransport` to require TLS |
| `IpAddress` / `NotIpAddress` | CIDR ranges |
| `NumericLessThan`, `DateGreaterThan` | Numbers and timestamps (temporary access windows) |
| `Null` | Tests whether a key is **present at all** |

Two modifiers that trip people up:
- **`...IfExists`** (e.g. `StringEqualsIfExists`) — "enforce this **only if** the key is present in the request." Without it, a request that simply omits the key fails the condition.
- **`ForAllValues:` / `ForAnyValue:`** — set operators, needed when a request key holds **multiple** values. `ForAnyValue:` passes if **at least one** value matches. `ForAllValues:` passes if **every** value matches — and here's the trap: **`ForAllValues:` also returns true when the key is absent entirely**, so used alone in an `Allow` it can permit more than you intended. Pair it with a `Null` check when it's load-bearing. This is the operator in the multi-tenant DynamoDB example under [Least Privilege & Permission Boundaries](#least-privilege--permission-boundaries-in-practice):
```json
"Condition": { "ForAllValues:StringEquals": { "dynamodb:LeadingKeys": ["${aws:PrincipalTag/TenantId}"] } }
```
*"Every partition key this request touches must equal the tenant ID tagged on the caller"* — one policy that safely isolates every tenant.

**Policy variables** are what `Version: 2012-10-17` unlocked (and why the older `2008-10-17` is obsolete). They're substituted at evaluation time:
```json
{ "Effect": "Allow", "Action": "s3:*",
  "Resource": "arn:aws:s3:::company-bucket/home/${aws:username}/*" }
```
One policy, attached to every user, gives each their own private folder. Common variables: `${aws:username}`, `${aws:userid}`, `${aws:PrincipalTag/Team}`.

**Limits worth knowing** (they force you to be deliberate rather than pile everything into one document): a **managed policy is capped at 6,144 characters**; inline policy budgets are 2,048 chars per user, 5,120 per group, 10,240 per role; and an identity can have **10 managed policies attached** by default. Hitting the managed-policy limit is normal at scale and the answer is several focused policies, not one giant one.

**How to write one from scratch:**
1. **List the exact API calls** the workload makes — from the SDK calls in code, or by running it with broad permissions in a sandbox and reading **CloudTrail**.
2. **Write the exact ARNs.** Only use `"*"` where the API genuinely doesn't support resource-level permissions.
3. **Add conditions** for the context that should be required (region, MFA, source IP, tag match).
4. **Test it in the [policy simulator](#iam-security-tools)** before it ships.
5. **Refine after it runs** using **Access Advisor** last-accessed data, or have **Access Analyzer generate the policy from CloudTrail history** — which is the strongest answer to "how do you write a tight policy?"

**The mistakes that come up most:**
| Mistake | What happens |
|---|---|
| Forgetting the bucket ARN alongside `bucket/*` | `GetObject` works, `ListBucket` returns AccessDenied (see the gotcha above) |
| `Allow` + `NotAction` | Accidental administrator |
| `StringEquals` where a wildcard was needed | Condition never matches; everything is denied and the policy looks correct |
| `"Version": "2024-01-01"` | Invalid — it's the **language** version, and only `2012-10-17` should be used |
| `Principal` in an identity-based policy | Rejected — `Principal` belongs only in resource-based and trust policies |
| Expecting an `Allow` to override an explicit `Deny` | It never does, anywhere |
| Editing a customer-managed policy in place with no rollback plan | It's versioned (5 kept) — use `set-default-policy-version` to roll back |

### IAM Roles, Policies, AssumeRole

**What a role actually is:** an identity that carries permissions but has **no permanent credentials** and belongs to nobody. Anything *trusted* can **assume** it and receive **temporary credentials that expire**.

Plain-English analogy: a user is a personal ID card you keep in your wallet forever; a role is a **uniform hanging on a hook** — you put it on, you get its powers, and when the shift ends you take it off. Nothing to leak, nothing to rotate.

**IAM Role vs IAM User**
| | IAM User | IAM Role |
|---|---|---|
| Credentials | Long-lived access keys | Temporary (STS), auto-rotated |
| Best for | Rare — human break-glass access | Services, automation, cross-account, CI/CD |
| Security posture | Higher risk (leak-prone, manual rotation) | Lower risk, auditable via CloudTrail |

**Trust Policy vs Permission Policy — the #1 confusion point**
| | Trust Policy | Permission Policy |
|---|---|---|
| Lives | On the role itself (*Trust relationships* tab) | Attached to the role |
| Answers | *Who* can assume this role? | *What* can the role do once assumed? |
| **`Principal`** | ✅ **Required** | ❌ **Not allowed** |
| **`Resource`** | ❌ Not used — **the role *is* the resource** | ✅ Required |
| **`Action`** | `sts:AssumeRole` (or `sts:AssumeRoleWithWebIdentity` / `...WithSAML` for federated callers; `sts:TagSession` to pass session tags) | Service APIs, e.g. `dynamodb:GetItem` |
| Example principal | `lambda.amazonaws.com`, another account ARN, OIDC provider | *(n/a)* |

Both are **required** and evaluated **separately** — they're two distinct documents attached at different points on the role, not one document, so there is nothing to merge. (Related but different error: putting a `Principal` into an *identity-based* policy is rejected outright as `MalformedPolicyDocument`.)

**Trust policy in short:**
- **Every role has one, mandatorily** — a role cannot exist without a trust policy. It's created for you when you pick a trusted entity in the console, and edited afterwards on the **Trust relationships** tab.
- It is a **resource-based policy** that happens to be attached to a role, which is why it needs a `Principal` — see [Users, Groups & Permissions](#users-groups--permissions).
- **The `Principal`/`Resource` split is the quickest way to identify any policy at a glance:** identity policy = `Resource`, no `Principal`; trust policy = `Principal`, no `Resource`; bucket/queue/key policy = **both**.
- One trust policy can hold **multiple statements/principals** — e.g. trusting both a service and a specific external role.
- Use **`aws:PrincipalOrgID`** to trust *any* account in your AWS Organization without enumerating account IDs:
```json
"Condition": { "StringEquals": { "aws:PrincipalOrgID": "o-abc123xyz" } }
```

**AssumeRole mechanics (via STS):**
1. Caller authenticates.
2. STS checks the target role's trust policy.
3. STS issues temporary credentials (Access Key, Secret Key, Session Token, expiring in 15 min–12 hrs).
4. Caller uses those credentials; the role's permission policy governs what they can actually do.

**The whole thing as one picture.** Two policies, two questions, checked at two different moments:

```
                            ┌──────────────────────────────┐
                            │          IAM ROLE            │
                            └───────┬──────────────┬───────┘
                                    │              │
                   ┌────────────────┘              └────────────────┐
                   ▼                                               ▼
        ╔══════════════════════════╗                    ╔══════════════════════════╗
        ║      TRUST POLICY        ║                    ║   PERMISSION POLICIES    ║
        ║   WHO can become me?     ║                    ║   WHAT can I then do?    ║
        ╚══════════════════════════╝                    ╚══════════════════════════╝
                   │                                               │
          checked ONCE, at                              checked on EVERY
          assume time                                   subsequent API call
```

Can't assume the role at all → trust policy. Assumed it but the call is denied → permission policy. That single split diagnoses most IAM tickets.

**Cross-account is a two-key lock.** Neither account can grant access unilaterally:

```
   ACCOUNT A (111111111111)            STS               ACCOUNT B (222222222222)
   ────────────────────────            ───               ────────────────────────
   ┌─────────────┐
   │  Principal  │
   └──────┬──────┘
          │   ╔════════════════ GATE 1 ════════════════╗
          │   ║ Caller's OWN identity policy must      ║
          │   ║ Allow sts:AssumeRole on <RoleB ARN>.   ║
          │   ║ Missing → denied even if B trusts you. ║
          │   ╚════════════════════════════════════════╝
          │
          │  ① sts:AssumeRole(RoleB)      ┌───────┐
          ├──────────────────────────────▶│  STS  │② reads RoleB's TRUST POLICY
          │                               │       │  ╔═══════ GATE 2 ═══════╗
          │                               │       │  ║ Principal matches?   ║
          │                               │       │  ║ Conditions pass?     ║
          │                               └───┬───┘  ╚══════════════════════╝
          │  ③ temporary credentials          │
          │◀──────────────────────────────────┘
          │     AccessKey + Secret + SessionToken   (15 min – 12 h)
          │
          │  ④ call B's resources                    ┌──────────────────────┐
          └─────────────────────────────────────────▶│ RoleB PERMISSION     │
                                                      │ POLICY  ╔ GATE 3 ╗  │
                                                      │         ╚════════╝  │
                                                      └──────────────────────┘
```

Gates 1 and 2 are **AND**, not OR — this is the single most common cross-account failure.

**What `Principal` can point at,** with the trap attached to each:

```
"Principal": { "AWS": "arn:aws:iam::111111111111:root" }
     └─▶ ANY principal in that account — delegates the decision to A's admins
"Principal": { "AWS": "arn:aws:iam::111111111111:role/AppRole" }
     └─▶ ONE specific role. The right default.
         ⚠ delete + recreate that role and the trust silently breaks
           (same ARN, new principal ID)
"Principal": { "Service": "lambda.amazonaws.com" }
     └─▶ An AWS service — this is what makes it an "execution role"
"Principal": { "Federated": "arn:aws:iam::111:oidc-provider/token.actions.githubusercontent.com" }
     └─▶ An IdP; Action becomes sts:AssumeRoleWithWebIdentity
         ⚠ MUST be paired with a Condition on `sub`, or ANY GitHub repo
           on earth can assume your role
```

**Now the permission side — grants versus ceilings.** This is the distinction that explains most "but the policy allows it" confusion:

```
   ╔═══════════════════════╗            ╔═══════════════════════════╗
   ║  GRANTS  (add power)  ║            ║  CEILINGS (only subtract) ║
   ╠═══════════════════════╣            ╠═══════════════════════════╣
   ║ • Identity policies   ║            ║ • SCP            (account)║
   ║   attached to the role║            ║ • Permission boundary     ║
   ║ • Resource policy on  ║            ║                   (role)  ║
   ║   the target          ║            ║ • Session policy (session)║
   ╚═══════════════════════╝            ╚═══════════════════════════╝
              └──────────────┬────────────────────┘
                             ▼
              EFFECTIVE = grant ∩ every ceiling
```

So a permission boundary containing `AdministratorAccess` grants **nothing** — attach only a boundary and no permission policy and the role can do nothing at all. For the full evaluation chain in text form see [Policy Types & Structure](#policy-types--structure).

**Where the role's permissions physically attach, and the quotas:**

```
   IAM ROLE
     ├─ Trust policy .............. exactly 1, mandatory
     ├─ Managed policies .......... up to 10 attached (raisable to 20)
     ├─ Inline policies ........... 10,240 characters total
     └─ Permission boundary ....... 0 or 1
```

**Session policies — the layer nobody can place.** Passed at assume time, not stored on the role:

```
   Caller ──── sts:AssumeRole ────▶ STS ────▶ temporary credentials
                    ├── --policy '<json>'      inline, 2,048 chars
                    └── --policy-arns a,b,c    up to 10 managed ARNs
                              ▼
              Applies to THIS SESSION only. Invisible in the console.
              Cannot grant anything the role doesn't already have.
```

The point of them: **one broad role, many narrow sessions.** A multi-tenant service assumes the same role per request but clamps each session to one tenant's key prefix, so a bug cannot cross tenants even though the role itself could.

**Diagnosing from the symptom:**

| Symptom | Layer to check |
|---|---|
| Can't assume the role at all | Trust policy (Gate 2) **and** the caller's own `sts:AssumeRole` (Gate 1) |
| Assumed fine, but every call denied | No permission policy attached — perhaps only a boundary |
| Policy clearly allows it, still denied | A ceiling is clamping it (SCP / boundary / session policy), or an explicit `Deny` |
| Works in one account, not another | SCP differs, or the target's resource policy doesn't name you |
| Worked for an hour, then broke | Credentials expired — or **role chaining**, which hard-caps sessions at **1 hour** regardless of `MaxSessionDuration` |
| Console shows the right policies but access fails | Session policy — it does not appear in the console |

**The four documents, end to end.** Cross-account needs *two* roles and *four* policies, and the one people forget is that the **source role has its own trust policy** which has nothing to do with the other account. Scenario: a Lambda in **Account A (`111111111111`)** reads a DynamoDB table in **Account B (`222222222222`)**.

```
  ACCOUNT A — 111111111111                   ACCOUNT B — 222222222222
  ┌─────────────────────────────┐            ┌─────────────────────────────┐
  │  OrderExportLambdaRole      │            │  DealsTableReaderRole       │
  ├─────────────────────────────┤            ├─────────────────────────────┤
  │ ① TRUST POLICY              │            │ ③ TRUST POLICY              │
  │   Principal: lambda.amaz…   │            │   Principal: role A's ARN ──┼──┐
  │   how the Lambda becomes    │            │   ◀── GATE 2                │  │
  │   this role. Account B      │            │                             │  │
  │   appears nowhere.          │            │                             │  │
  ├─────────────────────────────┤            ├─────────────────────────────┤  │
  │ ② PERMISSION POLICY         │            │ ④ PERMISSION POLICY         │  │
  │   sts:AssumeRole            │            │   dynamodb:GetItem, Query   │  │
  │   Resource: role B's ARN ───┼───┐        │   Resource: table/Deals     │  │
  │   ◀── GATE 1                │   │        │   ◀── GATE 3                │  │
  └─────────────────────────────┘   │        └─────────────────────────────┘  │
                                    └────────────────────────────────────────┘
                        ② names B's role  ·  ③ names A's role
                              they point at each other
```

**① Source role — trust policy (Account A).** *Who may become this role?* The Lambda service. This is an ordinary execution-role trust policy — note that Account B is not mentioned:
```json
{
  "Version": "2012-10-17",
  "Statement": [{
    "Sid": "AllowLambdaToAssume",
    "Effect": "Allow",
    "Principal": { "Service": "lambda.amazonaws.com" },
    "Action": "sts:AssumeRole",
    "Condition": {
      "StringEquals": { "aws:SourceAccount": "111111111111" },
      "ArnLike": {
        "aws:SourceArn": "arn:aws:lambda:ap-south-1:111111111111:function:order-export"
      }
    }
  }]
}
```

**② Source role — permission policy (Account A).** *What may it do?* Assume the target role, plus its own local work. **This is Gate 1:**
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "AssumeReaderRoleInAccountB",
      "Effect": "Allow",
      "Action": "sts:AssumeRole",
      "Resource": "arn:aws:iam::222222222222:role/DealsTableReaderRole"
    },
    {
      "Sid": "OwnAccountLogging",
      "Effect": "Allow",
      "Action": ["logs:CreateLogStream", "logs:PutLogEvents"],
      "Resource": "arn:aws:logs:ap-south-1:111111111111:log-group:/aws/lambda/order-export:*"
    }
  ]
}
```
Pin `Resource` to the exact role ARN — `"*"` here lets the Lambda assume *any* role that happens to trust it. And note what is **absent**: no `dynamodb:*`. The table is in another account, so a DynamoDB grant on this role does nothing at all.

**③ Target role — trust policy (Account B).** *Who may become this role?* That specific role in A. **This is Gate 2:**
```json
{
  "Version": "2012-10-17",
  "Statement": [{
    "Sid": "TrustOrderExportRoleInAccountA",
    "Effect": "Allow",
    "Principal": { "AWS": "arn:aws:iam::111111111111:role/OrderExportLambdaRole" },
    "Action": ["sts:AssumeRole", "sts:TagSession"],
    "Condition": {
      "StringEquals": { "aws:PrincipalOrgID": "o-abc123xyz" }
    }
  }]
}
```
Include `sts:TagSession` only if you actually pass session tags. `aws:PrincipalOrgID` is the right extra guard *within* your own organisation; for a third-party vendor use `sts:ExternalId` instead (below).

**④ Target role — permission policy (Account B).** *What may it do once assumed?* Read one table. **This is Gate 3:**
```json
{
  "Version": "2012-10-17",
  "Statement": [{
    "Sid": "ReadDealsTableOnly",
    "Effect": "Allow",
    "Action": ["dynamodb:GetItem", "dynamodb:Query"],
    "Resource": [
      "arn:aws:dynamodb:ap-south-1:222222222222:table/Deals",
      "arn:aws:dynamodb:ap-south-1:222222222222:table/Deals/index/*"
    ]
  }]
}
```
The `/index/*` ARN is **required** to `Query` a GSI — grant only the table ARN and every index query returns `AccessDenied`, which reads like a bug in your code.

**The matrix — the symmetry is the thing to remember:**

| | **Trust policy** — who can become me | **Permission policy** — what I can do |
|---|---|---|
| **Source role** (A) | `lambda.amazonaws.com` | `sts:AssumeRole` on **B's role ARN** |
| **Target role** (B) | **A's role ARN** | `dynamodb:GetItem`/`Query` on the table |

A's permission policy names B's role; B's trust policy names A's role. Both must exist — neither account can grant the access alone.

**The call — two clients, because the credentials change hands:**
```csharp
// Ambient credentials = the SOURCE role (the Lambda execution role)
var sts = new AmazonSecurityTokenServiceClient();

var assumed = await sts.AssumeRoleAsync(new AssumeRoleRequest {
    RoleArn         = "arn:aws:iam::222222222222:role/DealsTableReaderRole",
    RoleSessionName = $"order-export-{context.AwsRequestId}"   // appears in B's CloudTrail
});

// New client built from the TARGET role's temporary credentials
var ddb = new AmazonDynamoDBClient(assumed.Credentials);
var deal = await ddb.GetItemAsync("Deals", key);
```
`RoleSessionName` is what Account B sees as `assumed-role/DealsTableReaderRole/<name>` — put a request ID in it or cross-account attribution is impossible.

**Four traps:**

| Trap | Detail |
|---|---|
| Target permissions on the source role | The most common wiring error. B's permissions belong on B's role; the source role needs only `sts:AssumeRole`. |
| Recreating the trusted role | `Principal: <role ARN>` is stored internally as that role's **unique principal ID**. Delete and recreate role A under the same name and B's trust silently stops matching — re-save B's trust policy to fix. |
| Terraform circular dependency | B's trust policy references A's role ARN, which may not exist on a first apply. Either build the ARN as a string rather than a resource reference, or bootstrap with `:root` and tighten on a second pass. |
| Re-assuming every invocation | Credentials last 15 min–12 h. Cache them in the warm container and refresh on expiry rather than paying an STS round trip per request. |

**External ID (vendor/third-party access — prevents "confused deputy" attacks):**
```json
{
  "Effect": "Allow",
  "Principal": { "AWS": "arn:aws:iam::111111111111:root" },
  "Action": "sts:AssumeRole",
  "Condition": { "StringEquals": { "sts:ExternalId": "vendor-unique-id-123" } }
}
```

**GitHub Actions → AWS via OIDC (modern, keyless CI/CD — expect this in senior interviews):**
```json
{
  "Effect": "Allow",
  "Principal": { "Federated": "arn:aws:iam::111111111111:oidc-provider/token.actions.githubusercontent.com" },
  "Action": "sts:AssumeRoleWithWebIdentity",
  "Condition": {
    "StringEquals": { "token.actions.githubusercontent.com:aud": "sts.amazonaws.com" },
    "StringLike": { "token.actions.githubusercontent.com:sub": "repo:my-org/my-repo:*" }
  }
}
```
No static AWS keys stored in GitHub secrets; short-lived, auditable, scoped per repo/branch.

**Cross-account & trust-policy scenario patterns worth having ready:**
1. **AWS service assuming a role** (Lambda, EC2, ECS, CodeBuild) — a service principal in the trust policy.
2. **Cross-account access** — the target account's role trusts the calling account/role ARN; the caller needs `sts:AssumeRole` permission on their own side too.
3. **Cross-account + External ID** — third-party/vendor access, prevents confused-deputy.
4. **Condition-restricted trust** — narrow trust to one specific source service/resource via `aws:SourceArn`/`aws:SourceAccount`.
5. **OIDC federation** — GitHub Actions or another CI system, no long-lived keys.
6. **SAML federation** — corporate AD/Okta/Entra ID users get console/CLI access via `sts:AssumeRoleWithSAML`, or (preferably today) via IAM Identity Center.

**Temporary credentials are three parts, not two:** `AccessKeyId`, `SecretAccessKey`, **and a `SessionToken`** — plus an expiry timestamp. The session token is what makes them temporary; a request signed with the first two but missing the token fails. Knowing this detail is a quick credibility signal, and it explains why you must propagate `AWS_SESSION_TOKEN` alongside the other two env vars.

**STS API reference:**
| API | Use |
|---|---|
| `AssumeRole` | The main one — assume a role in your own or another account |
| `AssumeRoleWithSAML` | Callers arriving from a SAML 2.0 IdP (ADFS, Okta, Entra ID) |
| `AssumeRoleWithWebIdentity` | OIDC/web identity — GitHub Actions, Google/Facebook logins (for mobile apps, AWS recommends Cognito Identity Pools as the wrapper) |
| `GetSessionToken` | MFA-backed temporary credentials for an **IAM user** (not a role) |
| `GetFederationToken` | Temporary credentials for a federated *user* |
| `GetCallerIdentity` | "Who am I?" — requires **no permissions at all**, which is why it always works as a debugging probe |

**Session duration and role chaining:** default 1 hour; the role's *Maximum session duration* setting allows 1–12 hours (`--duration-seconds` cannot exceed it). **Role chaining** — using one assumed role to assume another — is **hard-capped at 1 hour** and cannot be extended, which is a common cause of "our long-running batch job dies exactly 60 minutes in". Also pass a meaningful `--role-session-name`: it appears in CloudTrail, and it's the only way to trace *which human* used a shared role.

**How a role actually reaches your compute** (the part that's easy to hand-wave and gets probed):
| Compute | Delivery mechanism |
|---|---|
| **EC2** | An **instance profile** — a container holding exactly **one** role, and the thing actually attached to the instance. A role cannot be attached to EC2 directly. The console creates the instance profile silently with the same name as the role; with **CLI/CloudFormation/Terraform you must create it yourself**, which is why "the role exists but the instance can't use it" is such a common IaC bug. |
| **Lambda** | Execution role, assumed by `lambda.amazonaws.com` at invoke time |
| **ECS** | **Two different roles** — the **task execution role** (used by the ECS agent to pull the image from ECR and write logs) vs the **task role** (used by *your application code*). Mixing them up is a genuine production bug: your app gets `AccessDenied` on DynamoDB because the permission was added to the execution role. |
| **EKS** | **IRSA** (IAM Roles for Service Accounts) or the newer **EKS Pod Identity** — a Kubernetes service account maps to an IAM role via an OIDC provider, so each pod gets its own least-privilege role instead of sharing the node's role. |

**EC2 instance profile in short:**
- **What it is:** a thin **wrapper around a role**. The role holds the permissions; the profile is the object EC2 can actually attach. Two names for what feels like one thing, which is why it confuses people.
- **One profile holds exactly one role** — but the same role can sit in many profiles.
- **The console hides it.** Pick a role in the EC2 console and it creates the matching profile behind the scenes, so most people never learn it exists — until they write **Terraform/CloudFormation**, where it's a separate resource you must declare and reference.
- **Attach or swap it on a running instance** — it takes effect within a couple of minutes, **no reboot**, because the SDK simply picks up new credentials from IMDS.
- **Failure signature:** the role looks correct in IAM but the instance behaves as if it has no permissions at all → either no instance profile exists, or the profile exists with no role in it.

**IMDS — how the credentials physically arrive on EC2:**
```bash
# IMDSv2 (session-oriented, and what you should require)
TOKEN=$(curl -X PUT "http://169.254.169.254/latest/api/token" \
  -H "X-aws-ec2-metadata-token-ttl-seconds: 21600")
curl -H "X-aws-ec2-metadata-token: $TOKEN" \
  http://169.254.169.254/latest/meta-data/iam/security-credentials/
```
The SDK does this automatically and **refreshes the credentials before they expire** — which is why you never cache or manually manage them. **IMDSv1 vs IMDSv2** is a real security question: v1 answers a plain `GET`, so any SSRF bug in your app (or a misconfigured reverse proxy) can be tricked into fetching the instance's credentials — the root cause of several well-known breaches. **IMDSv2 requires a `PUT` to obtain a token first**, which a simple SSRF cannot do. Always set `HttpTokens: required` (and `HttpPutResponseHopLimit: 1`) in your Terraform/CloudFormation launch template.

**Service-linked roles:** predefined roles *owned by* an AWS service (e.g. `AWSServiceRoleForECS`, `AWSServiceRoleForAutoScaling`). The service manages the trust policy and permissions; you cannot edit them, and they can generally only be deleted once the service no longer needs them. The point: they exist so a service can act in your account without you hand-building its trust relationship.

**`iam:PassRole` — the privilege-escalation control people forget.** `sts:AssumeRole` is "let me *become* this role". `iam:PassRole` is "let me *hand* this role to an AWS service" — needed to launch an EC2 instance with a role, create a Lambda with an execution role, or register an ECS task definition. Why it matters: a user with `lambda:CreateFunction` plus **unrestricted** `iam:PassRole` can create a Lambda with `AdministratorAccess` and run arbitrary code as admin — a full escalation from a seemingly modest permission set. Always scope `PassRole` to specific role ARNs, and add `iam:PassedToService` conditions:
```json
{
  "Effect": "Allow",
  "Action": "iam:PassRole",
  "Resource": "arn:aws:iam::123456789012:role/lambda-prod-order-writer-role",
  "Condition": { "StringEquals": { "iam:PassedToService": "lambda.amazonaws.com" } }
}
```

**IAM Identity Center (formerly AWS SSO) — the modern answer for *human* access.** Users live in a built-in identity store or your corporate IdP (Entra ID/Okta); you assign **permission sets**, which Identity Center materialises as roles in each member account. People get short-lived credentials via `aws sso login`, there is one place to offboard a leaver, and no long-lived keys exist anywhere. So when asked *"should we still be creating IAM users?"* the answer is: **no, not for humans** — Identity Center or federation, with IAM users reserved for legacy apps that genuinely cannot assume a role and one break-glass account.

**Mental model to recite in interviews:** "Trust Policy = who's allowed to enter the building. Permission Policy = which rooms they can access once inside. AssumeRole = the temporary access badge issued at the door."

### Least Privilege & Permission Boundaries in Practice

**Anti-pattern (seen constantly in real .NET/Lambda code):**
```json
{ "Effect": "Allow", "Action": "dynamodb:*", "Resource": "*" }
```

**Least-privilege version:**
```json
{
  "Effect": "Allow",
  "Action": ["dynamodb:GetItem", "dynamodb:PutItem", "dynamodb:UpdateItem"],
  "Resource": "arn:aws:dynamodb:us-east-1:123456789012:table/Orders",
  "Condition": { "ForAllValues:StringEquals": { "dynamodb:LeadingKeys": ["${aws:PrincipalTag/TenantId}"] } }
}
```

**Permission boundaries** are a separate mechanism from SCPs: a boundary is attached to a *role or user* and defines the maximum permissions that identity can ever have, regardless of what its attached policies say — commonly used by platform/security teams to let application teams create their own roles/policies within a safe ceiling (e.g., "you may create any role, but it can never exceed this boundary policy"). This is different from an **SCP** (Service Control Policy), which applies at the AWS Organizations level to entire accounts/OUs, not individual identities.

---

## 4. Secrets & Parameters

### Secrets Manager vs Parameter Store

> **In containers there are *two* roles in play:** the **execution role** resolves secrets injected by the ECS agent, the **task role** is what your code uses to fetch them at runtime — see [ECS → RDS: The Four Layers of a Database Connection](10-databases-caching-analytics.md#ecs--rds-the-four-layers-of-a-database-connection).

The original notes reference "Secrets Manager or Parameter Store" for securing secrets in multiple places (Lambda/ECS/CodeBuild sections) but never actually compare them — a direct comparison is a very common senior AWS interview question.

| | Secrets Manager | SSM Parameter Store |
|---|---|---|
| Cost | Per-secret + per-API-call charge | Standard tier free; Advanced tier has a small charge |
| Automatic rotation | Built-in (native RDS/Redshift/DocumentDB integrations, or custom Lambda rotation function) | No native rotation — must build it yourself |
| Versioning | Yes | Yes |
| Encryption | KMS, always encrypted | KMS optional (SecureString) or plaintext (String) |
| Max size | 64 KB | 4 KB (Standard) / 8 KB (Advanced) |
| Cross-account/cross-region replication | Native replication support | Manual/custom |
| Typical .NET use | DB connection strings, API keys needing rotation | App config, feature flags, non-rotating settings |

**Senior-level guidance:** use **Secrets Manager** for anything that needs rotation or is a genuine credential (DB passwords, third-party API keys); use **Parameter Store** for configuration values and secrets that don't need automatic rotation, to control cost at scale (hundreds/thousands of config values). A common cost-optimization talking point: many teams over-use Secrets Manager for pure configuration, paying rotation/API-call overhead for values that never rotate — Parameter Store (SecureString) is the correct, cheaper tool there.

**.NET retrieval example:**
```csharp
var client = new AmazonSecretsManagerClient();
var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = "prod/orders/db" });
var connectionString = response.SecretString;
```

#### Secrets Manager — Pitfalls

The service is easy to adopt and easy to get subtly wrong. These are the failures that actually happen:

**❗ 1. Rotation can take your application down.** Rotation creates a **new version** of the secret and moves the **`AWSCURRENT`** staging label to it; the old version becomes **`AWSPREVIOUS`**. If your app reads the secret **once at startup** and caches it forever, it keeps using the old password — and the moment rotation invalidates it, every connection fails at 3 a.m. with no deploy having happened.

The fix is to **re-fetch on authentication failure**, not to poll: catch the auth error, pull `AWSCURRENT` again, retry once. For managed rotation of RDS credentials, prefer **[RDS Proxy](10-databases-caching-analytics.md#rds-proxy)** or **IAM database authentication**, which remove the password from the app entirely. The four-step rotation Lambda (`createSecret` → `setSecret` → `testSecret` → `finishSecret`) also has a **two-user strategy** precisely so there's always one valid credential during the swap — worth naming.

**❗ 2. Don't call `GetSecretValue` on every request.** It's a network call with a **throttling quota**, and you pay per 10,000 calls. A busy API doing this per request will throttle and add latency to every call. Cache it in memory with a TTL — the **AWS Secrets Manager caching library**, or the **Parameters and Secrets Lambda extension** (a local HTTP cache sidecar). This is the most common performance mistake with the service.

**3. Cost surprises at scale.** ~$0.40 per secret per month *plus* API calls. That's trivial for 20 database credentials and material for **2,000 config values** — which is exactly the over-use pattern flagged above. Feature flags and app settings belong in **Parameter Store Standard (free)**.

**4. Both IAM *and* KMS permissions are required** when the secret uses a customer-managed key — `secretsmanager:GetSecretValue` **and** `kms:Decrypt`. Same trap as the [Fargate/S3/CMK example](12-security-services.md#worked-example-giving-a-fargate-task-access-to-kms-encrypted-s3-data): the IAM policy looks right and the call still fails.

**5. A private subnet needs a VPC endpoint.** `com.amazonaws.<region>.secretsmanager` is an **interface** endpoint — there's no gateway option. Without it (or a NAT gateway) the call hangs rather than erroring clearly.

**6. Deletion has a mandatory 7–30 day recovery window.** You cannot immediately recreate a secret with the same name, which breaks teardown-and-recreate CI pipelines. `ForceDeleteWithoutRecovery` bypasses it — and removes your safety net.

**7. Injected secrets are resolved once, at start.** ECS `valueFrom` and Lambda environment variables are populated when the **task starts** or the execution environment initialises. Rotating the secret does **not** update a running task — you must redeploy, or read it in code. People assume injection means live updates; it doesn't.

**8. Rotation Lambda needs network access to the database.** If the DB is in private subnets, the rotation function must be VPC-attached with the right security groups — otherwise rotation silently fails and you discover it when the secret is stale.

**9. Cross-region replication is opt-in, and replicas are read-only.** Enable it deliberately for DR; don't assume a multi-region app can write to the local copy.

**10. Secrets in environment variables are visible.** Anything injected as an env var shows up in `docker inspect`, task-definition JSON, and often in crash dumps or logs. Fetching in code and holding it in memory is stronger; ECS `valueFrom` at least keeps the literal out of the task definition, but it still lands in the container's environment.

**The one-line summary:** *"Secrets Manager for credentials that rotate, Parameter Store for configuration — and cache the value with a TTL while re-fetching on auth failure, because the real production incident isn't a leaked secret, it's a rotated one the app never re-read."*

---

## 5. Tooling, Pitfalls & Drills

### IAM Security Tools

| Tool | Scope | What it gives you |
|---|---|---|
| **Credential Report** | Whole account (CSV download) | One row per user: password enabled/last used/last changed, **MFA active yes/no**, access key age, key last-used date and service. The fastest way to find users without MFA, keys older than 90 days, and keys that have **never** been used (safe to delete). |
| **Access Advisor** (last-accessed data) | Per user/role/group/policy | Which **services** the identity was granted access to and **when it last used them**. This is the practical route from "AdministratorAccess because it worked" to least privilege: granted 40 services, used 4 in 12 months → remove the other 36. |
| **IAM Access Analyzer** | Account / Organization | Findings for resources shared **outside** your account or org (public buckets, over-broad trust policies, KMS keys); **policy validation** as you write; **unused access findings** (unused roles, keys, permissions); and **generate a least-privilege policy from CloudTrail history** — the single best "how do you write a tight policy?" answer. |
| **Policy Simulator** | Policy testing | Answers "would this principal be allowed to do X on Y?" **without making the call**, and shows which statement decided it. Use it before shipping a policy change, and to prove why an SCP or boundary is the real blocker. |
| **CloudTrail** | Audit trail | Every API call: who, when, source IP, which role session (hence `--role-session-name`). The answer to "how do you find out who deleted the bucket?" and how you alert on unusual `AssumeRole` activity. |
| **AWS Config** | Continuous compliance | Managed rules like `iam-user-mfa-enabled`, `access-keys-rotated`, `iam-policy-no-statements-with-admin-access` — turns a one-off audit into an always-on check with remediation. |

```bash
aws iam generate-credential-report
aws iam get-credential-report --query Content --output text | base64 -d > report.csv
aws iam get-account-authorization-details > iam-snapshot.json     # full policy/role dump for offline review
aws accessanalyzer list-findings --analyzer-arn <arn>
```

**How to frame this in an interview:** don't just name the tools — pair each with the question it answers. "Credential report tells me *what shouldn't exist* (stale keys, missing MFA); Access Advisor tells me *what's over-granted*; Access Analyzer tells me *what's exposed externally* and can generate the tighter policy from real CloudTrail usage; the simulator lets me verify the change before it ships."

### IAM Pitfalls

| Pitfall | Why it happens | Fix |
|---|---|---|
| Confusing trust vs permission policy | "I gave S3 access but AssumeRole still fails" | Both are required; check trust policy `sts:AssumeRole` grant separately from the action permissions |
| Overusing `AdministratorAccess` | "Just to make it work" | Start read-only, add incrementally, scope by action+resource |
| Forgetting explicit Deny always wins | SCPs, permission boundaries, resource policies can silently override an Allow | When debugging: check SCP → permission boundary → resource policy → identity policy, in that order |
| Hardcoding credentials | Convenience | Use roles; let the SDK fetch credentials automatically; OIDC for CI/CD |
| Single-AZ Lambda ENIs in a VPC | Function fails during an AZ outage | Configure the Lambda's VPC config across multiple subnets/AZs — IAM itself is AZ-agnostic, execution is not |
| Reusing one role across many services | Permissions creep, hard to audit, blast radius if compromised | One role per service, clear naming convention |
| Overly broad trust principal (`"Principal": "*"`) | Anyone can assume the role | Restrict to specific account/service/OIDC provider, add `aws:SourceArn`/`aws:SourceAccount` conditions |
| No conditions on policies | Allowing an action without scoping resource/context | Use `aws:SourceArn`, `aws:SourceVpc`, `s3:prefix`, `kms:EncryptionContext` |
| Ignoring permission boundaries | "Why can't my role do X even though its policy allows it?" | Boundary defines the *maximum* possible permission — role policy is a subset of the boundary, common in enterprise AWS orgs |
| Assuming roles never expire | App breaks after a few hours | STS credentials expire (15 min–12 hrs); let the SDK auto-refresh, never cache manually |
| Deep role chaining (A→B→C→D) | Debugging nightmare, shrinking max session duration | Keep chains shallow, prefer direct trust relationships |
| No CloudTrail auditing of AssumeRole | No visibility into who assumed what | Enable CloudTrail, alert on unusual `AssumeRole`/`AssumeRoleWithWebIdentity` activity |
| Forgetting resource-based policy is also required | Identity policy allows it, but SQS/KMS/S3 resource policy doesn't | Some services require **both sides** to allow access (S3 cross-account, SQS, KMS, SNS) |
| Poor role naming (`test-role`, `my-role`) | Confusing audits, risky reuse | Use `service-env-purpose-role`, e.g. `lambda-prod-order-writer-role` |
| "Set and forget" IAM | Permissions creep as services are added/removed over time | Periodic reviews + IAM Access Analyzer |
| Unrestricted `iam:PassRole` | Looks harmless next to `lambda:CreateFunction`, but is a full privilege-escalation path to admin | Scope `PassRole` to specific role ARNs + `iam:PassedToService` condition |
| Leaving **IMDSv1** enabled on EC2 | Default on older AMIs/launch templates; any SSRF bug can then steal the instance role's credentials | Require IMDSv2 (`HttpTokens: required`, hop limit 1) in the launch template/Terraform |
| Stale `AWS_ACCESS_KEY_ID` env vars on an instance | Env vars sit **above** the instance profile in the credential chain, so they silently shadow the role | `aws sts get-caller-identity` — if it shows `user/...` not `assumed-role/...`, unset the env vars |
| Permission added to the ECS **task execution role** instead of the **task role** | Both are "the ECS role" in people's heads | Execution role = ECS agent (ECR pull, logs); task role = your app code's permissions |
| Creating access keys for the **root** user, or using root day-to-day | "It was the account I already had" | MFA root, never create root keys, create an admin identity (ideally Identity Center) on day one |
| Assuming `AdministratorAccess` can do literally everything | It cannot perform the root-only actions (close account, change support plan, S3 MFA-delete) | Know the root-only list — see [IAM Overview](#iam-overview-root-account--shared-responsibility) |

**Golden debugging checklist for "my role has permission but access still fails":** execution role permission → trust policy → resource-based policy (bucket/queue/key policy) → KMS key policy (if encrypted) → SCP/permission boundary.

### IAM Rapid-Fire Q&A

Short-answer drill for the fundamentals. The longer scenario answers live in [Sample Interview Q&A](19-cross-cutting-reference.md#sample-interview-qa).

**Q: Is IAM regional or global?**
A: Global. You never select a region for IAM, and the same users/roles/policies apply everywhere. Side effect: it's eventually consistent, so a freshly created role may not be usable for a few seconds.

**Q: User vs Group vs Role — in one sentence each?**
A: A user is one person with long-term credentials; a group is a permission container holding users only (no nesting, not an identity, can't be a `Principal`); a role is permissions with no permanent credentials that a trusted service/account/federated identity assumes temporarily.

**Q: What can a brand-new IAM user do with no policy attached?**
A: Nothing — not even list buckets. IAM is deny-by-default; every permission is one you explicitly granted.

**Q: One policy allows an action and another denies it. What happens?**
A: **Explicit Deny always wins**, and nothing can override it — not `AdministratorAccess`, not a resource policy. If no policy mentions the action at all, it's an implicit deny, so also denied.

**Q: Why is a role better than access keys for an app on EC2?**
A: Nothing secret is stored on the box or in code/Git, credentials are temporary and auto-refreshed by the SDK before expiry, permissions can be changed centrally without redeploying, and there's nothing to rotate or leak. Prove it in two commands: `aws s3 ls` fails with "Unable to locate credentials" before the role, works after.

**Q: What are the two policies on a role, and how do you tell which one is broken?**
A: The **trust policy** (who may assume it — has a `Principal`) and the **permissions policy** (what it can do once assumed). If `sts:AssumeRole` itself fails with "not authorized to perform sts:AssumeRole", it's the trust policy; if you assume successfully but the API call fails, it's the permissions policy.

**Q: How do you give an application on EC2 access to S3?**
A: Create a role trusting `ec2.amazonaws.com`, attach a scoped S3 policy, attach the role to the instance — via the **instance profile**, which is what actually gets attached (a role can't attach to EC2 directly). The SDK then picks credentials up from IMDS automatically.

**Q: What are the two ways to do cross-account access?**
A: (1) **AssumeRole** — the target account's role trusts the caller's account and the caller has `sts:AssumeRole`; the caller *becomes* the role and loses their own permissions for that session. (2) **Resource-based policy** — the bucket/queue/key policy names the outside principal directly; the caller *keeps* their own identity, and both sides must allow it.

**Q: What exactly does STS return, and for how long?**
A: `AccessKeyId`, `SecretAccessKey`, and a **`SessionToken`**, plus an expiry — 15 minutes to 12 hours depending on the role's maximum session duration. **Role chaining is hard-capped at 1 hour** and can't be extended.

**Q: Inline vs managed policy — which do you recommend?**
A: Customer-managed. It's reusable, versioned (5 versions, so you can roll back), and you can see everywhere it's attached. Inline policies are invisible to audits, unreusable, and die with the identity. AWS-managed policies are fine to start with but are almost always broader than you need.

**Q: Permissions boundary vs SCP?**
A: A boundary attaches to a **user or role** and caps its maximum permissions (effective = policy ∩ boundary); an SCP attaches at the **Organizations account/OU** level and limits everyone in the account, including its root user. Neither ever *grants* anything — both can only subtract.

**Q: What's the confused deputy problem and how do you fix it?**
A: A third party holding a role that serves all their customers could be tricked into using their access against *your* account. Fix: they issue you a unique **External ID** and your trust policy requires it via a `sts:ExternalId` condition. For AWS service principals, the equivalent controls are `aws:SourceArn`/`aws:SourceAccount`.

**Q: Why does `iam:PassRole` matter?**
A: It's the permission to *hand a role to a service*, distinct from `sts:AssumeRole` (becoming one). Unrestricted, it turns `lambda:CreateFunction` into full admin — create a function with an admin execution role and run whatever you like. Scope it to specific role ARNs with an `iam:PassedToService` condition.

**Q: Can `AdministratorAccess` do everything in the account?**
A: No. Root-only actions remain — closing the account, changing the root email/support plan, S3 MFA-delete, RI Marketplace registration. It's a favourite trick question.

**Q: How do you enforce MFA for CLI/API calls, not just the console?**
A: MFA natively protects console sign-in only. For CLI/API you add a policy condition `"Bool": {"aws:MultiFactorAuthPresent": "true"}` and the caller obtains an MFA-backed session via `sts:GetSessionToken` (IAM user) or `AssumeRole` with `--serial-number`/`--token-code`. Requiring MFA in the **trust policy** is the standard control for production cross-account access.

**Q: How would you find over-permissioned identities in an account you just inherited?**
A: Credential report for what shouldn't exist (no MFA, stale/never-used keys), **Access Advisor** last-accessed data for services granted but never used, **IAM Access Analyzer** for external exposure and unused-access findings, and its generate-policy-from-CloudTrail feature to rebuild a tight policy from actual usage. Verify each proposed change in the **policy simulator** before shipping it.

**Q: Should you still create IAM users today?**
A: Not for humans — use **IAM Identity Center** (or SAML/OIDC federation) so access is centrally managed, short-lived, and offboarding happens in one place. Keep IAM users only for legacy apps that genuinely cannot assume a role, plus one break-glass account.

**Q (scenario): Our nightly batch job dies almost exactly 60 minutes in. Why?**
A: Role chaining — the job assumes a role from an already-assumed role, which caps the session at 1 hour regardless of the role's max-duration setting. Either assume the target role directly from the base identity (so 1–12 hours is available), or make the job refresh credentials rather than holding one session.

**Q (scenario): The EC2 role clearly allows `s3:GetObject`, but the app still gets AccessDenied. Where do you look?**
A: In order: is the app actually using the role (`aws sts get-caller-identity` — an ARN of `user/...` instead of `assumed-role/...` means stale `AWS_*` env vars are shadowing the instance profile, since env vars rank above IMDS in the credential chain); then the bucket policy; then the KMS key policy if the object is SSE-KMS encrypted; then SCP/permissions boundary. Also confirm an instance profile exists at all — with Terraform/CloudFormation the role can exist without one.

**Disaster Recovery — IAM & Security**

| | |
|---|---|
| **What's actually at risk** | Roles, policies, trust relationships, Identity Center assignments. IAM is **global**, so a regional outage doesn't touch it — the realistic disaster is *you*, via a bad apply or a deletion |
| **Backup mechanism** | **IaC is the only backup** — IAM has no snapshot facility. **AWS Config** gives you the timeline of what changed; **CloudTrail** gives you who did it |
| **Realistic RPO / RTO** | RPO = last commit. RTO minutes |

**Recovery runbook:**
1. **Identify** the change: AWS Config → resource timeline for the role; CloudTrail → `DeleteRole` / `PutRolePolicy` event with the actor.
2. **Re-apply from IaC.** Don't hand-fix in the console — that creates the drift you'll fight later.
3. **Rotate anything exposed** during the incident: access keys, and any secret the compromised principal could read.
4. **Verify** with IAM Access Analyzer that you restored the intended boundary and not something broader.

⚠️ **The gotcha, and it's the one that actually hurts:** **you can break the IAM you need in order to fix IAM.** Keep a **break-glass role** with a vaulted MFA credential that is *not* managed by the same pipeline that could delete it. And know this: recreating a deleted role with the same name gives it a **new unique principal ID** — ARN-based trusts recover, but anything pinned to the principal ID stays silently broken, which is a very confusing hour to spend during an incident.

---

## 6. Hands-On

### Hands-On: Users & Groups

1. IAM console → **Users** → *Create user* → name it.
2. Tick **Provide user access to the AWS Management Console** only if a human needs the UI.
3. Choose auto-generated or custom password; leave *user must create a new password at next sign-in* ticked.
4. **Permissions** → *Add user to group* → create/select a group (e.g. an `admin` group carrying `AdministratorAccess`). Attaching to the group, not the user, is the habit to demonstrate.
5. Create → **download the `.csv`**. The password and secret are shown **once** and are unrecoverable afterwards — you delete and reissue rather than "look them up".
6. **Account alias** (IAM dashboard → *Account Alias*) turns the ugly sign-in URL `https://123456789012.signin.aws.amazon.com/console` into `https://my-company.signin.aws.amazon.com/console`.

```bash
aws iam create-group  --group-name Developers
aws iam attach-group-policy --group-name Developers \
  --policy-arn arn:aws:iam::aws:policy/ReadOnlyAccess
aws iam create-user   --user-name parteek
aws iam add-user-to-group --user-name parteek --group-name Developers
aws iam list-groups-for-user --user-name parteek     # verify
```

**Point worth making:** IAM users sign in *to a specific account* via that account URL/alias; root signs in by **email address**. That's how you tell from a screenshot which one someone is using.

### Hands-On: MFA & Access Keys

**Enable virtual MFA:** IAM → Users → *your user* → **Security credentials** → *Assign MFA device* → name it → **Authenticator app** → scan the QR code → enter **two consecutive codes** (AWS uses the pair to sync the time drift) → sign out and back in to confirm.

**Create and use an access key:** Security credentials → *Create access key* → choose the CLI use case → acknowledge the warning → download the `.csv`.
```bash
aws configure
# AWS Access Key ID:     AKIA...
# AWS Secret Access Key: ****
# Default region name:   us-east-1
# Default output format: json
aws sts get-caller-identity      # confirms which identity the key belongs to
```

**Rotation drill worth being able to recite:**
```bash
aws iam create-access-key  --user-name parteek       # key #2 (now at the limit of 2)
# update apps/CI to key #2, deploy, and verify traffic is using it
aws iam update-access-key  --user-name parteek --access-key-id AKIA_OLD --status Inactive
# soak: if anything breaks, flip it back to Active
aws iam delete-access-key  --user-name parteek --access-key-id AKIA_OLD
```

### Hands-On: IAM Roles

**EC2 → S3 role (the demo that makes roles click):**
1. IAM → **Roles** → *Create role* → **AWS service** → **EC2**. (This writes the trust policy for you.)
2. Attach permissions — `AmazonS3ReadOnlyAccess` for the demo; a scoped customer-managed policy in real life.
3. Name it `ec2-dev-s3-reader-role` → Create.
4. EC2 → select instance → **Actions → Security → Modify IAM role** → select it → Update. **Takes effect immediately, no reboot.**

```bash
# BEFORE attaching the role, on the instance:
aws s3 ls
# → "Unable to locate credentials"

# AFTER attaching:
aws sts get-caller-identity
# → arn:aws:sts::123456789012:assumed-role/ec2-dev-s3-reader-role/i-0abc123
aws s3 ls        # works — and there are no access keys anywhere on the box
```
That before/after is the whole argument for roles over access keys, in two commands.

**Cross-account role:** *Create role* → **AWS account** → *Another AWS account* → enter the trusting account ID → optionally tick **Require MFA** and/or **Require external ID** → attach permissions → hand the role ARN to the other account. They use it via console **Switch role**, or:
```bash
aws sts assume-role \
  --role-arn arn:aws:iam::222222222222:role/CrossAccountReadRole \
  --role-session-name parteek-audit-2026-08 \
  --duration-seconds 3600
# export the three values, then verify you actually became the role:
aws sts get-caller-identity
```
Or, cleaner for day-to-day work, let the CLI do the assumption for you via `~/.aws/config`:
```ini
[profile prod-audit]
role_arn       = arn:aws:iam::222222222222:role/CrossAccountReadRole
source_profile = dev
mfa_serial     = arn:aws:iam::111111111111:mfa/parteek
```
```bash
aws s3 ls --profile prod-audit    # CLI assumes the role and caches/refreshes the session
```

---

← [DynamoDB](02-dynamodb.md) · [Index](README.md) · [Infrastructure as Code & CI/CD](04-iac-cicd.md) →
