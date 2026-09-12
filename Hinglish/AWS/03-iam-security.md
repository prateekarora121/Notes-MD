> **AWS Detailed Guide** · [Index](README.md) · Part I

# IAM & Security

---

## 1. IAM Foundations

### IAM Overview, Root Account & Shared Responsibility

**IAM kyun exist karta hai:** AWS infrastructure ko secure karta hai (shared responsibility model); aap control karte ho ki *kaun* *kya* *kis resource* par kar sakta hai. IAM data store nahi karta, requests process nahi karta, ya workloads run nahi karta — yeh sirf permission decide karta hai.

**Teen building blocks:** Identity (User/Role/Service) → Policy (JSON rules) → Authentication (aap kaun ho) vs Authorization (aap kya kar sakte ho).

**Har single AWS API call in order mein do questions ka answer deta hai:**
1. **Authentication** — *aap kaun ho?* Aapke paas valid credentials hain kya (password, access key, ya ek temporary STS token)?
2. **Authorization** — *aapko yeh karne ki permission hai kya?* Koi policy iss resource par yeh action allow karti hai kya?

Dono pass hone chahiye. Authentication fail karo to `InvalidClientTokenId`/signature error milta hai; authorization fail karo to `AccessDenied` milta hai — inn dono mein farak batana kisi bhi IAM debugging session ka pehla step hai.

**Facts jo interviewers warm-up questions ke roop mein use karte hain:**
| Fact | Detail |
|---|---|
| **IAM global hai, regional nahi** | Aap IAM ke liye kabhi region choose nahi karte. Ek set users/roles/policies ka har region mein kaam karta hai. (Contrast is guide ki almost har baaki cheez se.) |
| **IAM free hai** | Users, roles, groups, ya policies ke liye koi charge nahi. |
| **Eventually consistent** | IAM data worldwide replicate hota hai, isliye ek brand-new policy ya role ko everywhere effective hone mein kuch seconds lag sakte hain. Isi liye CI/CD pipelines jo ek role create karte hain aur immediately use karte hain kabhi-kabhi first attempt par fail hote hain aur retry par succeed karte hain — ek real gotcha naam lene layak. |
| **Root account** | AWS account ke saath create hota hai, sign-up email se identified hota hai, aur unlimited power rakhta hai jise policies restrict nahi kar sakti. |

**Root account rules (chaaron bolo):** usse immediately MFA enable karo; daily work ke liye kabhi use mat karo; kabhi share mat karo; **uske liye kabhi access keys create mat karo** (AWS ab actively isko block/warn karta hai). Naye account par day-one task: ek admin identity create karo (ideally ek IAM Identity Center user — dekho [IAM Roles](#iam-roles-policies-assumerole)), verify karo ki aap usse use kar sakte ho, phir root ko lock away kar do.

**Cheezein jo *sirf* root kar sakta hai** (ek favourite trick question, kyunki instinct hota hai "AdministratorAccess sab kuch kar sakta hai" — nahi kar sakta):
- AWS account close karna
- Account name, root email, ya root password change karna
- AWS Support plan change ya cancel karna
- Billing manage karne ki ek IAM user ki permission restore karna, ek baar revoke hone ke baad
- Reserved Instance Marketplace mein seller ke roop mein register hona
- S3 bucket par MFA Delete enable karna, ya ek S3 bucket policy delete karna jo sab principals ko deny karti ho (classic "maine apne hi bucket se khud ko lock kar liya" recovery)
- Kuch tax invoices dekhna

**IAM ke liye Shared Responsibility Model** — AWS cloud ko secure karta hai, aap jo usmein daalte ho usse secure karte ho:
| AWS iske liye responsible hai | Aap iske liye responsible ho |
|---|---|
| IAM ko ek global, highly available service ke roop mein chalana | Users, groups, roles, policies create aur organise karna |
| Underlying infrastructure ko patch aur secure karna | **Least privilege** apply karna (dekho [Least Privilege & Permission Boundaries](#least-privilege--permission-boundaries-in-practice)) |
| Service khud ka vulnerability analysis aur compliance validation | Credentials rotate aur protect karna; unused ones delete karna |
| Tooling provide karna (MFA support, Access Analyzer, credential reports, CloudTrail) | Us tooling ko **enable karna aur actually review karna** |
| Tenant isolation — kabhi bhi aapka data kisi doosre customer ko leak na hone dena | Leavers remove karna; audit karna ki kaun kya kar sakta hai |

**Bolne layak one-liner:** "AWS guarantee karta hai ki IAM kaam karta hai aur *ek service ke roop mein* secure hai; main isse kaise configure karta hoon woh entirely mujh par hai. AWS mujhe khushi-khushi `Resource: *` par `Action: *` likhne dega — woh mera problem hai, unka nahi."

---

## 2. Identities & Access

### Users, Groups & Permissions

**IAM User** = ek physical person (ya ek legacy application jo genuinely role use nahi kar sakta). Iske paas **long-term credentials** hote hain: ek console password aur/ya access keys. Yeh apne aap expire nahi hote, isi liye automated kaam ke liye roles preferred hain.

**IAM Group** = ek container jo sirf many users ko ek saath permissions attach karne ke liye use hota hai. Rules jo interviewers test karte hain:
- Ek group mein **sirf users** hote hain — ❌ **ek group mein doosra group nahi ho sakta** (no nesting).
- Ek user **multiple groups** mein ho sakta hai; effective permissions unn sabki **union** hoti hain (plus jo bhi directly user par attached hai).
- Ek user **zero groups** mein ho sakta hai (allowed, recommended nahi).
- Ek group **koi identity nahi hai**: aap "ek group ke roop mein" log in nahi kar sakte, aur ek group ARN kisi policy mein `Principal` ke roop mein appear nahi ho sakta. Sirf users, roles, aur services hi principals ho sakte hain.

```
Account
├── Group: Developers  → [Parteek, Ravi]
├── Group: Operations  → [Ravi, Sara]     ← Ravi is in two groups: permissions add up
└── Group: Audit       → [Sara]
```

**Groups kyun matter karte hain:** policy ko group par ek baar attach karo, 50 users par 50 baar nahi. Joiner → group mein add karo. Leaver → remove karo. Permissions consistent aur auditable rehti hain, jo ki poora point hai.

**Permissions deny-by-default hoti hain.** Ek bilkul nayi IAM user bina policy ke *kuch bhi* nahi kar sakti — yeh S3 buckets list bhi nahi kar sakti ya EC2 dashboard bhi nahi dekh sakti. AWS mein kuch bhi implicitly allowed nahi hai; har permission wo hai jo aapne deliberately grant ki hai. "Ek new user out of the box kya kar sakta hai?" ka yeh hi correct answer hai.

**Policy types by attach kis chiz par hoti hai** — **identity-based** (User, Group ya Role par; koi `Principal` nahi, kyunki jo hold karta hai wahi principal hai) versus **resource-based** (resource par khud — S3 bucket, SQS queue, KMS key, Lambda; `Principal` mandatory hai). Poora treatment [Policy Types & Structure](#policy-types--structure) mein hai. *Yahan* jo matter karta hai wo yeh hai ki dono account boundaries ke across kaise behave karte hain:

**Same-account vs cross-account (ek precise distinction jo correct paana zaroori hai):**
- **Same account** — ek identity-based policy *ya* action allow karne wali resource-based policy kaafi hai.
- **Cross-account via ek resource-based policy** — caller apni identity rakhta hai aur resource ko directly call karta hai; resource policy mein unka naam hona chahiye, **aur** unka apna account bhi unhe wo call karne allow karna chahiye. Dono sides. S3, SQS, SNS, KMS, Lambda par apply hota hai.
- **Cross-account via AssumeRole** — caller *ban jaata hai* wo role aur uss session ke liye apni original permissions chhod deta hai. Sirf role ki permissions apply hoti hain. Dekho [IAM Roles](#iam-roles-policies-assumerole).

Woh "resource policy = aap khud rehte ho; AssumeRole = aap koi aur ban jaate ho" contrast "cross-account access ke do tarike kya hain?" ka sabse crisp answer hai.

**Dono paths, side by side.** Poore example ka scenario: **Account A (`111111111111`)** ki ek app ko **Account B (`222222222222`)** ke `partner-exports` bucket se objects padhne hain.

**Path 1 — resource-based policy (caller khud hi rehta hai).** Dono taraf zaruri hai, aur dusra half hi wo hai jo log bhool jaate hain.

*Account B* — bucket policy, caller ko `Principal` ke roop mein naam se:
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
*Account A* — `ExportReader` par identity policy, jo use call karne ki ijaazat deti hai:
```json
{
  "Effect": "Allow",
  "Action": ["s3:GetObject", "s3:ListBucket"],
  "Resource": ["arn:aws:s3:::partner-exports",
               "arn:aws:s3:::partner-exports/*"]
}
```
Call mein STS ki zarurat hi nahi — app apne hi credentials se seedha B ke bucket par jaati hai:
```csharp
var s3 = new AmazonS3Client();               // kahin bhi AssumeRole nahi
var obj = await s3.GetObjectAsync("partner-exports", "deals/2026-08-23.json");
```

**Path 2 — AssumeRole (caller koi aur ban jaata hai).** Wahi teen pieces, shape different: *Account B* ke paas ek **trust policy** hai jo batati hai kaun is role mein badal sakta hai, plus ek permission policy jo batati hai role kya kar sakta hai; *Account A* ko us role ARN par `sts:AssumeRole` chahiye; phir caller apni identity temporary credentials se trade karta hai. Poora walkthrough aur C# call [IAM Roles, Policies, AssumeRole](#iam-roles-policies-assumerole) mein hai.

**Dono mein se choose kaise karein:**

| | Resource-based policy | AssumeRole |
|---|---|---|
| Resource ko kaunsi identity dikhti hai | Caller ki apni | Assumed role |
| Effect mein kaunsi permissions | Caller ki **∩** resource policy | **Sirf** role ki — us session ke liye caller ki hat jaati hain |
| Extra API call | Koi nahi | `sts:AssumeRole`; credentials expire hote hain aur refresh chahiye |
| Kiske liye available | Sirf un services ke liye jinke paas resource policies *hain* — S3, SQS, SNS, KMS, Lambda, Secrets Manager, API Gateway (aur 2024 se DynamoDB) | Kuch bhi, un services ke liye bhi jinke paas resource policy nahi |
| Kab best hai | Ek narrow, durable grant — ek partner ek bucket ya queue padh raha ho | Broad ya multi-service access; jahan session tagging ya External ID chahiye |

⚠️ **Audit ka farak, aur yahi detail ek acche answer ko alag karti hai:** **resource-based policy** ke saath Account B ka CloudTrail caller ko `arn:aws:iam::111111111111:role/ExportReader` record karta hai — *asli* identity, target account ke apne trail mein. **AssumeRole** ke saath B ka trail `arn:aws:sts::222222222222:assumed-role/CrossAccountReadRole/ExportSession` dikhata hai, toh yeh pata karne ke liye ki actually kisne kiya, Account A ke trail ke `AssumeRole` event se correlate karna padta hai. Agar compliance requirement yeh hai ki "target account khud har access attribute kar sake", to wo resource-policy path ke favour mein argument hai.

### Password Policy

**Password policy** (IAM → Account settings) — aap kya enforce kar sakte ho:
- Minimum length (AWS 128 characters tak allow karta hai)
- Required character types: uppercase, lowercase, number, non-alphanumeric
- Users ko apna password change karne allow ya prevent karo
- N din ke baad **Expiration** (forced rotation), aur ek expired password user ko lock out karta hai ya self-reset karne deta hai
- Last N passwords **reuse** prevent karo

**Yeh kyun poocha jaata hai:** yeh brute-force aur credential-stuffing ke against sabse cheap defence hai. Lekin honest senior version bhi bolo — akele password policy weak hai; real controls **MFA** (neeche) aur long-lived human credentials ko entirely IAM Identity Center ke through eliminate karna hain.

### MFA (Multi-Factor Authentication)

**Idea ek line mein:** ek password steal, phish, ya leak ho sakta hai; MFA *aisi cheez jo aapke paas physically hai* add karta hai, isliye ek stolen password alone useless hota hai.

Root account ke liye mandatory, aur console access wale har human user par expected.

| Device type | Yeh kya hai |
|---|---|
| **Virtual MFA device** | Phone/laptop par TOTP app — Google Authenticator, Authy, Duo Mobile. Ek device **multiple tokens** hold kar sakta hai (root plus several users). Free, aur common choice. |
| **FIDO / U2F security key** | Physical USB key jaise YubiKey. Ek key **multiple root accounts aur IAM users** serve kar sakti hai. Phishing-resistant, jo TOTP nahi hai. |
| **Hardware TOTP token** | Key fob ya display card (Gemalto; AWS GovCloud ke liye SurePassID). Wahan use hota hai jahan floor par phones allowed nahi hain. |
| **Passkeys / biometrics** | FIDO2 — Face ID, Touch ID, Windows Hello. |

Details jo ek real answer ko memorised se alag karte hain:
- AWS **har user par multiple MFA devices (up to 8)** support karta hai — ek backup register karo taaki ek lost phone lockout na ban jaaye.
- MFA natively **console sign-in** ko protect karta hai. **CLI/API** ke liye ek policy condition (`"Bool": {"aws:MultiFactorAuthPresent": "true"}`) se enforce karte ho aur caller ek MFA-backed session `sts:GetSessionToken` (IAM users ke liye) ya `sts:AssumeRole` `--serial-number`/`--token-code` ke saath lekar obtain karta hai. "MFA console cover karta hai lekin CLI ko policy condition chahiye" wo point hai jo zyadatar candidates miss karte hain.
- Ek **trust policy** mein `sts:AssumeRole` par MFA require karna cross-account production access ke liye standard control hai.
- Lost device: root registered email + phone verification se recover karta hai; ek IAM user ke liye admin purana device deactivate karta hai aur naya assign karta hai.

### Access to AWS: Console, CLI, SDK & Access Keys

Teen front doors, ek back end — sab kuch ultimately same **AWS REST API** call karta hai SigV4-signed requests ke saath:
| Route | Credential | Best for |
|---|---|---|
| **Management Console** | Username + password (+ MFA) | Humans, exploration, one-off fixes |
| **AWS CLI** | Access key ID + secret (ya temporary role credentials) | Scripting, automation, debugging |
| **AWS SDK** (.NET, Python, JS…) | Same, lekin normally ek role se supplied | Application code |

**Access keys — IAM ka risky part:**
- Access key ID ≈ username, secret access key ≈ password. **Secret ek baar dikhta hai**; lose karo aur key delete karke naya banao.
- **Ek user par maximum 2 access keys**, aur yeh limit deliberate hai: yeh zero-downtime rotation possible banane ke liye exist karta hai — key #2 create karo → roll it out karo → verify karo → key #1 **delete karo**. (Deactivate karke chodna half-done version hai.)
- Keys ko kabhi Git mein commit mat karo, ek AMI ya container image mein bake mat karo, CI secrets mein paste mat karo jab OIDC available ho, ya people/services ke beech share mat karo.
- **Best practice hai unhe bilkul use na karna** jahan role kaam kar sakta ho. EC2/ECS/Lambda par .NET service ke liye, ya ek GitHub Actions pipeline ke liye, static key ka koi good reason nahi hai — dekho [IAM Roles](#iam-roles-policies-assumerole).

```bash
aws configure                    # writes ~/.aws/credentials + ~/.aws/config
aws configure --profile dev      # named profile
aws sts get-caller-identity      # "who am I?" — the single most useful IAM debug command
aws iam list-access-keys --user-name parteek
aws s3 ls --profile dev
```

**AWS CloudShell** — ek browser terminal jo already aapki console identity se authenticated hai, isliye laptop par bilkul koi keys nahi. Har region mein available nahi; ~1 GB home-directory persistence.

**Default credential provider chain (order matter karta hai, aur yeh ek real debugging trap hai):**
1. Explicit CLI/SDK parameters
2. **Environment variables** (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_SESSION_TOKEN`)
3. Shared credentials file (`~/.aws/credentials`) / config file profile
4. Container credentials (ECS task role endpoint)
5. **EC2 instance profile via IMDS**

Instance role **last** hai. Toh ek stale `AWS_ACCESS_KEY_ID` jo shell, systemd unit, ya Dockerfile mein reh gaya ho silently EC2/ECS role ko shadow kar deta hai, aur aapko `AccessDenied` milta hai un permissions ke liye jo aapke role ke paas clearly hain. `aws sts get-caller-identity` immediately isko reveal karta hai — agar yeh ek `user/...` ARN print kare jahan aap `assumed-role/...` expect kar rahe the, wahi aapka answer hai.

---

## 3. Policies, Roles & Least Privilege

### Policy Types & Structure

**Teen tarah ki policy — jaano kaunsi recommend karni hai:**
| Type | Kaun manage karta hai | Reusable | Kab use karo |
|---|---|---|---|
| **AWS managed** | AWS — jaise AWS APIs add karta hai auto-update hoti hai | Haan | Quick start / broad roles: `ReadOnlyAccess`, `AdministratorAccess`, `AmazonS3ReadOnlyAccess`. Convenient lekin almost hamesha zarurat se broad. |
| **Customer managed** | Aap | Haan — many identities par attach karo | ✅ **Real work ke liye right answer.** Versioned (up to 5 versions retained, isliye ek bad change rollback kar sakte ho), reusable, aur aap dekh sakte ho yeh kahan-kahan attached hai. |
| **Inline** | Aap | Nahi — ek user/group/role mein embedded, usi ke saath delete hota hai | Rare genuinely one-off grants. Avoid karo: audits ke liye invisible, reuse impossible, no version history. |

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

| Field | Matlab | Notes |
|---|---|---|
| `Version` | Policy **language** version | Hamesha `"2012-10-17"`. Aapki policy ka apna version nahi — ek common misread. Purana `2008-10-17` policy variables support nahi karta. |
| `Id` | Optional policy identifier | Identity policies mein rarely used |
| `Statement` | Ek ya zyada permission blocks | Required |
| `Sid` | Statement ID — humans ke liye ek label | Optional, lekin CloudTrail/debugging ko kaafi easy banata hai |
| `Effect` | `Allow` ya `Deny` | Required |
| `Principal` | Yeh *kispe* apply hota hai | **Sirf** resource-based policies aur trust policies mein. Identity-based policy mein kabhi nahi. |
| `Action` | `service:Operation` ke roop mein API calls | Wildcards allowed (`s3:Get*`). `NotAction` = "yeh sab except" — powerful aur galat samajhna easy. |
| `Resource` | ARNs jinpar actions apply hote hain | Kuch APIs resource-level permissions support nahi karte aur `"*"` require karte hain (e.g. kaafi `List*`/`Describe*` calls) — jaanna zaroori hai taaki `"*"` ko automatically sloppiness na samjha jaaye. |
| `Condition` | Extra rules ki *kab* statement apply hota hai | Yahin par least privilege actually enforce hota hai |

**`bucket` vs `bucket/*` gotcha** (constantly poocha jaata hai): `arn:aws:s3:::my-bucket` **bucket** hai — `s3:ListBucket` ke liye chahiye. `arn:aws:s3:::my-bucket/*` **andar ke objects** hain — `s3:GetObject` ke liye chahiye. Yeh galat karo aur milega "main file download kar sakta hoon agar mujhe uska naam pata ho lekin `aws s3 ls` AccessDenied deta hai", ya vice-versa.

**ARN format:**
```
arn:partition:service:region:account-id:resource
arn:aws:iam::123456789012:user/parteek              ← IAM is global, so region is empty
arn:aws:s3:::my-bucket/file.txt                     ← S3 names are global, so region+account empty
arn:aws:dynamodb:us-east-1:123456789012:table/Orders
```

**Condition keys jo memorize karne layak hain:**
| Key | Use |
|---|---|
| `aws:MultiFactorAuthPresent` | Destructive/sensitive actions ke liye MFA require karo |
| `aws:SourceIp` | Office/VPN CIDR ranges tak restrict karo |
| `aws:RequestedRegion` | Activity ko approved regions par pin karo |
| `aws:PrincipalOrgID` | Sirf mere AWS Organization se identities |
| `aws:PrincipalTag` / `aws:ResourceTag` | Tag-based access (ABAC, neeche) |
| `aws:SecureTransport` | HTTPS force karo |
| `sts:ExternalId` | Third-party role assumption — dekho confused deputy [IAM Roles](#iam-roles-policies-assumerole) mein |
| `aws:SourceArn` / `aws:SourceAccount` | Ek service principal ko ek specific caller tak narrow karo |

**Evaluation order (memorize karo):** Explicit Deny → Explicit Allow → implicit Default Deny. Koi matching policy nahi = koi access nahi. Ek explicit Deny **hamesha jeetta hai**, `AdministratorAccess` ke upar bhi.

Poora evaluation path jab org-level guardrails exist karte hain — yeh isi answer ka senior version hai:
```
Request
 ├─ Any explicit Deny anywhere?          → DENY (nothing can override this)
 ├─ SCP (Organizations) permits it?      → no → DENY
 ├─ Permissions boundary permits it?     → no → DENY
 ├─ Session policy permits it?           → no → DENY
 ├─ Identity policy OR resource policy allows it?  → neither → DENY (implicit)
 └─ else                                 → ALLOW
```
Note karo iska matlab kya hai: **SCPs aur boundaries sirf permissions le sakte hain, kabhi grant nahi kar sakte.** Dekho [Least Privilege & Permission Boundaries](#least-privilege--permission-boundaries-in-practice) boundary-vs-SCP detail ke liye.

**RBAC vs ABAC:**
- **RBAC** (role-based) — job function ke hisab se permissions; aap har team ya project ke liye naya role/policy create karte ho. Simple, lekin policy count org ke saath grow karta hai.
- **ABAC** (attribute-based) — permissions **tags** se driven hoti hain: "aap ek resource par action kar sakte ho jiska `Team` tag aapke apne `PrincipalTag/Team` se match karta ho". Ek policy kisi bhi number of teams/projects tak scale karti hai bina edits ke. "200 microservices ke across IAM ko kaise scale karoge?" ka standard answer hai.

**Ek poora ABAC example.** Teen pieces line up hone chahiye, aur dusra wahi hai jahan zyadatar attempts chupchap fail hote hain.

*1. Policy* — ek policy, har engineer ke role par attached, jise team add hone par kabhi edit nahi karna padta:
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
Ise ek sentence ki tarah padho: *un sabhi instances par yeh actions allow karo jinka `Team` tag mere apne `Team` tag ke barabar hai.* Kal 30vi team onboard karo aur yeh policy badalti hi nahi — yehi poori value proposition hai.

*2. Principal ko uska tag kaise milta hai* — wo step jo usually missing hota hai. Do raaste:
- **Static** — role ko khud tag karo: `aws iam tag-role --role-name Engineer --tags Key=Team,Value=payments`. Theek hai jab ek role ek team ko serve karta ho.
- **Dynamic (session tags)** — caller assume karte waqt tag deta hai, toh *ek* role har team ko serve karta hai:
  ```
  aws sts assume-role --role-arn arn:aws:iam::111111111111:role/Engineer \
      --role-session-name alice --tags Key=Team,Value=payments
  ```
  Iske liye role ki trust policy mein `sts:AssumeRole` ke saath **`sts:TagSession`** chahiye — chhod diya to call reject ho jaati hai. IAM Identity Center ya SAML/OIDC IdP ke saath tag ek directory attribute se map hota hai, aur insaanon ke liye yeh isi tarah scale karta hai.

*3. Kaun tags set kar sakta hai us par control rakho — iske bina ABAC control hi nahi hai.* Agar ek engineer instance ko retag kar sakta hai, to wo khud ko uska access de sakta hai. Toh tagging action par bhi constraint lagao:
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

**Wo before/after jo case bana deta hai:**

| | RBAC | ABAC |
|---|---|---|
| 30 teams × 4 environments | ~120 policies likhni aur maintain karni | **1 policy** |
| Nayi team onboard karna | Naya role + nayi policy + review cycle | Ek tag set karo |
| Risk kahan rehta hai | Policy sprawl aur drift | **Tag integrity** — isi liye piece 3 mandatory hai |

⚠️ **Teen gotchas jo naam lene layak hain:**
- **Har service har action par `aws:ResourceTag` support nahi karti.** EC2, RDS, Lambda, DynamoDB tables aur S3 objects zyadatar karte hain; bahut se `List*`/`Describe*` calls koi resource-level condition support hi nahi karte aur unhe `"Resource": "*"` bina tag condition ke chahiye. Toh real designs **ABAC + RBAC combined** hote hain, ABAC RBAC ko replace nahi karta — pure-ABAC model ka wada karne se pehle service authorization reference check karo.
- **Tag keys aur values case-sensitive hain.** `Team=Payments` ek `PrincipalTag/Team` = `payments` se match **nahi** karega. "Policy theek dikh rahi hai par deny ho raha hai" ka sabse common karan yehi hai.
- **Untagged resources invisible hote hain, open nahi** — condition match hi nahi karti, toh access deny hota hai. ABAC ke saath `aws:RequestTag`-enforced tag-on-create rakho taaki kuch bhi untagged land hi na kar sake.

#### Policy Document Ki Anatomy

**Har policy ko ek sentence ke roop mein padho.** Char fields sara meaning carry karte hain:

> **`Effect`** *(allow ya deny)* — **`Action`** *(yeh API calls)* — **`Resource`** *(inn cheezon par)* — **`Condition`** *(lekin sirf jab yeh true ho)*.

Toh upar wala anatomy example yeh padhta hai: *"**Allow** `GetObject` aur `ListBucket` **par** my-bucket aur uske andar sab kuch, **lekin sirf agar** caller MFA se authenticated ho."* Ek baar policies ko isi tarah padhna shuru karo, likhna guesswork nahi rehta.

**Ek policy ko step-by-step banate hue.** Absolute minimum se shuru karo aur precision add karo:

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
Note karo step 4 **prefix** (`uploads/*`) ko bhi narrow karta hai, sirf bucket nahi — resource ARNs ek path tak specific ho sakte hain.

**Wildcards** `Action` aur `Resource` mein kaam karte hain: `*` kisi bhi number of characters match karta hai, `?` exactly ek character match karta hai. `s3:Get*` `GetObject`, `GetBucketPolicy`, aur "Get" se shuru hone wala baaki sab cover karta hai — convenient, lekin yeh silently future APIs ko grant kar deta hai jo AWS us prefix ke saath add kare, isi liye explicit action lists kisi bhi sensitive chiz ke liye safer hain.

**Multiple statements** **independently** evaluate hote hain, aur results combine hote hain. Koi ordering nahi hai aur koi fall-through nahi — har statement check hota hai, koi bhi `Deny` jeetta hai, warna koi bhi `Allow` grant karta hai. Toh yeh ek normal, readable shape hai:
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
Broad allow + narrow deny standard **guardrail** pattern hai, aur yeh exactly isliye kaam karta hai kyunki Deny hamesha Allow se jeetta hai.

**❗ `NotAction` / `NotResource` — field table jis footgun ki warning deta hai.** `NotAction` matlab "inke **siva** sab kuch".
- **`Deny`** ke saath yeh genuinely useful hai — classic region lock:
```json
{ "Effect": "Deny",
  "NotAction": ["iam:*", "sts:*", "route53:*", "cloudfront:*", "support:*"],
  "Resource": "*",
  "Condition": { "StringNotEquals": { "aws:RequestedRegion": ["us-east-1", "ap-south-1"] } } }
```
*"Mere do approved regions ke bahar sab kuch deny karo, except global services jinka koi region nahi hai."*
- **`Allow`** ke saath yeh almost hamesha ek mistake hai. `{"Effect":"Allow","NotAction":"iam:*","Resource":"*"}` **IAM ke siva AWS mein har action** grant karta hai — effectively administrator, likha gaya hai aise andaaz mein jo restrictive lagta hai. Agar review mein `Allow` + `NotAction` dikhe, use ek finding treat karo.

**Condition operators** — `Condition` block `{ Operator: { key: value } }` hai, aur right operator choose karna matter karta hai:
| Operator | Use |
|---|---|
| `StringEquals` / `StringNotEquals` | Exact match, case-sensitive |
| `StringLike` / `StringNotLike` | **Wildcards** ke saath match — `repo:my-org/my-repo:*` style patterns ke liye use karne wala |
| `ArnEquals` / `ArnLike` | ARN comparison (`ArnLike` wildcards allow karta hai) |
| `Bool` | `true`/`false` — e.g. `aws:SecureTransport` TLS require karne ke liye |
| `IpAddress` / `NotIpAddress` | CIDR ranges |
| `NumericLessThan`, `DateGreaterThan` | Numbers aur timestamps (temporary access windows) |
| `Null` | Test karta hai ki ek key present hai ki nahi |

Do modifiers jo log ko trip karte hain:
- **`...IfExists`** (e.g. `StringEqualsIfExists`) — "yeh **sirf tab** enforce karo jab request mein key present ho." Iske bina, ek request jo simply key omit karti hai condition fail karti hai.
- **`ForAllValues:` / `ForAnyValue:`** — set operators, jab ek request key mein **multiple** values hain tab chahiye. `ForAnyValue:` pass hota hai agar **kam se kam ek** value match kare. `ForAllValues:` pass hota hai agar **har** value match kare — aur yahan trap hai: **`ForAllValues:` bhi true return karta hai jab key bilkul absent ho**, isliye ek `Allow` mein alone use kiya jaaye to yeh intended se zyada permit kar sakta hai. Jab yeh load-bearing ho toh isse ek `Null` check ke saath pair karo. Yehi operator hai multi-tenant DynamoDB example mein [Least Privilege & Permission Boundaries](#least-privilege--permission-boundaries-in-practice) ke under:
```json
"Condition": { "ForAllValues:StringEquals": { "dynamodb:LeadingKeys": ["${aws:PrincipalTag/TenantId}"] } }
```
*"Yeh request jo bhi partition key touch kare, wo caller par tagged tenant ID ke equal hona chahiye"* — ek policy jo safely har tenant ko isolate karti hai.

**Policy variables** wo hain jo `Version: 2012-10-17` unlock karta hai (aur isi liye purana `2008-10-17` obsolete hai). Yeh evaluation time par substitute hote hain:
```json
{ "Effect": "Allow", "Action": "s3:*",
  "Resource": "arn:aws:s3:::company-bucket/home/${aws:username}/*" }
```
Ek policy, har user par attached, har ek ko apna private folder deti hai. Common variables: `${aws:username}`, `${aws:userid}`, `${aws:PrincipalTag/Team}`.

**Jaanne layak limits** (yeh aapko sab ek giant document mein daalne ke bajaye deliberate hone force karte hain): ek **managed policy 6,144 characters tak capped hai**; inline policy budgets hain 2,048 chars per user, 5,120 per group, 10,240 per role; aur ek identity ke paas default se **10 managed policies attach** ho sakti hain. Scale par managed-policy limit hit karna normal hai aur answer several focused policies hai, ek giant policy nahi.

**Ek policy scratch se kaise likhein:**
1. **Exact API calls list karo** jo workload karta hai — code mein SDK calls se, ya ek sandbox mein broad permissions ke saath usse run karke aur **CloudTrail** padh kar.
2. **Exact ARNs likho.** `"*"` sirf wahan use karo jahan API genuinely resource-level permissions support nahi karta.
3. **Conditions add karo** context ke liye jo required hona chahiye (region, MFA, source IP, tag match).
4. Ship karne se pehle [policy simulator](#iam-security-tools) mein **test karo**.
5. **Access Advisor** last-accessed data use karke chalne ke baad **refine karo**, ya **Access Analyzer se CloudTrail history se policy generate karo** — jo "ek tight policy kaise likhte ho?" ka sabse strong answer hai.

**Mistakes jo sabse zyada aati hain:**
| Mistake | Kya hota hai |
|---|---|
| `bucket/*` ke saath bucket ARN bhool jaana | `GetObject` kaam karta hai, `ListBucket` AccessDenied deta hai (upar wala gotcha dekho) |
| `Allow` + `NotAction` | Accidental administrator |
| `StringEquals` jahan wildcard chahiye tha | Condition kabhi match nahi karta; sab deny ho jaata hai aur policy correct dikhti hai |
| `"Version": "2024-01-01"` | Invalid — yeh **language** version hai, aur sirf `2012-10-17` use hona chahiye |
| Identity-based policy mein `Principal` | Rejected — `Principal` sirf resource-based aur trust policies mein hota hai |
| Ek explicit `Deny` ko `Allow` se override hone ki expectation | Kabhi nahi hota, kahin bhi |
| Ek customer-managed policy ko in place edit karna bina rollback plan ke | Yeh versioned hai (5 kept) — `set-default-policy-version` use karo rollback ke liye |

### IAM Roles, Policies, AssumeRole

**Ek role actually kya hai:** ek identity jo permissions carry karti hai lekin uske paas **koi permanent credentials nahi** hote aur wo kisi ki bhi nahi hoti. Kuch bhi jo *trusted* hai use **assume** kar sakta hai aur **temporary credentials** paa sakta hai jo expire ho jaate hain.

Plain-English analogy: ek user ek personal ID card hai jise aap apne wallet mein hamesha rakhte ho; ek role ek **hook par latka uniform** hai — aap pehnte ho, uski powers milti hain, aur shift khatam hote hi utaar dete ho. Leak karne layak kuch nahi, rotate karne layak kuch nahi.

**IAM Role vs IAM User**
| | IAM User | IAM Role |
|---|---|---|
| Credentials | Long-lived access keys | Temporary (STS), auto-rotated |
| Best for | Rare — human break-glass access | Services, automation, cross-account, CI/CD |
| Security posture | Higher risk (leak-prone, manual rotation) | Lower risk, CloudTrail se auditable |

**Trust Policy vs Permission Policy — #1 confusion point**
| | Trust Policy | Permission Policy |
|---|---|---|
| Kahan hoti hai | Role par khud (*Trust relationships* tab) | Role par attached |
| Answer karti hai | *Kaun* is role ko assume kar sakta hai? | Assume hone ke baad role *kya* kar sakta hai? |
| **`Principal`** | ✅ **Required** | ❌ **Not allowed** |
| **`Resource`** | ❌ Use nahi hota — **role hi *resource* hai** | ✅ Required |
| **`Action`** | `sts:AssumeRole` (ya federated callers ke liye `sts:AssumeRoleWithWebIdentity` / `...WithSAML`; session tags pass karne ke liye `sts:TagSession`) | Service APIs, e.g. `dynamodb:GetItem` |
| Example principal | `lambda.amazonaws.com`, doosra account ARN, OIDC provider | *(n/a)* |

Dono **required** hain aur **separately** evaluate hote hain — yeh do distinct documents hain role par alag alag points par attached, ek document nahi jise merge karna ho. (Related lekin different error: ek *identity-based* policy mein `Principal` daalna `MalformedPolicyDocument` ke roop mein outright rejected hota hai.)

**Trust policy ek line mein:**
- **Har role ke paas ek hoti hai, mandatorily** — ek role bina trust policy ke exist nahi kar sakta. Yeh aapke liye create hoti hai jab aap console mein ek trusted entity pick karte ho, aur baad mein **Trust relationships** tab par edit hoti hai.
- Yeh ek **resource-based policy** hai jo ek role par attached hoti hai, isi liye ise `Principal` chahiye — dekho [Users, Groups & Permissions](#users-groups--permissions).
- **`Principal`/`Resource` split kisi bhi policy ko ek glance mein identify karne ka sabse fast tarika hai:** identity policy = `Resource`, `Principal` nahi; trust policy = `Principal`, `Resource` nahi; bucket/queue/key policy = **dono**.
- Ek trust policy mein **multiple statements/principals** ho sakte hain — e.g. ek service aur ek specific external role dono ko trust karna.
- **`aws:PrincipalOrgID`** use karo apne AWS Organization ke *kisi bhi* account ko trust karne ke liye bina account IDs enumerate kiye:
```json
"Condition": { "StringEquals": { "aws:PrincipalOrgID": "o-abc123xyz" } }
```

**AssumeRole mechanics (STS ke through):**
1. Caller authenticate hota hai.
2. STS target role ki trust policy check karta hai.
3. STS temporary credentials issue karta hai (Access Key, Secret Key, Session Token, 15 min–12 hrs mein expire hote hain).
4. Caller unn credentials use karta hai; role ki permission policy govern karti hai ki wo actually kya kar sakte hain.

**Poori cheez ek picture mein.** Do policies, do sawaal, do alag-alag moments par check hote hain:

```
                            ┌──────────────────────────────┐
                            │          IAM ROLE            │
                            └───────┬──────────────┬───────┘
                                    │              │
                   ┌────────────────┘              └────────────────┐
                   ▼                                               ▼
        ╔══════════════════════════╗                    ╔══════════════════════════╗
        ║      TRUST POLICY        ║                    ║   PERMISSION POLICIES    ║
        ║   KAUN mujhe ban sakta?  ║                    ║   Phir main KYA kar      ║
        ║                          ║                    ║   sakta hoon?            ║
        ╚══════════════════════════╝                    ╚══════════════════════════╝
                   │                                               │
          EK BAAR check hota hai,                        HAR subsequent API call
          assume ke waqt                                 par check hota hai
```

Role assume hi nahi ho raha → trust policy. Assume ho gaya par call deny ho rahi hai → permission policy. Bas yeh ek split zyadatar IAM tickets diagnose kar deta hai.

**Cross-account ek two-key lock hai.** Koi bhi account akela access grant nahi kar sakta:

```
   ACCOUNT A (111111111111)            STS               ACCOUNT B (222222222222)
   ────────────────────────            ───               ────────────────────────
   ┌─────────────┐
   │  Principal  │
   └──────┬──────┘
          │   ╔════════════════ GATE 1 ════════════════╗
          │   ║ Caller ki APNI identity policy mein    ║
          │   ║ Allow sts:AssumeRole on <RoleB ARN>    ║
          │   ║ hona chahiye. Nahi hai → deny, chahe   ║
          │   ║ B aap par bharosa karta ho.            ║
          │   ╚════════════════════════════════════════╝
          │
          │  ① sts:AssumeRole(RoleB)      ┌───────┐
          ├──────────────────────────────▶│  STS  │② RoleB ki TRUST POLICY padhta hai
          │                               │       │  ╔═══════ GATE 2 ═══════╗
          │                               │       │  ║ Principal match?     ║
          │                               │       │  ║ Conditions pass?     ║
          │                               └───┬───┘  ╚══════════════════════╝
          │  ③ temporary credentials          │
          │◀──────────────────────────────────┘
          │     AccessKey + Secret + SessionToken   (15 min – 12 h)
          │
          │  ④ B ke resources call karo              ┌──────────────────────┐
          └─────────────────────────────────────────▶│ RoleB PERMISSION     │
                                                      │ POLICY  ╔ GATE 3 ╗  │
                                                      │         ╚════════╝  │
                                                      └──────────────────────┘
```

Gate 1 aur Gate 2 **AND** hain, OR nahi — yeh cross-account ka sabse common failure hai.

**`Principal` kis par point kar sakta hai,** aur har ek ka trap:

```
"Principal": { "AWS": "arn:aws:iam::111111111111:root" }
     └─▶ Us account ka KOI BHI principal — decision A ke admins par chhod diya
"Principal": { "AWS": "arn:aws:iam::111111111111:role/AppRole" }
     └─▶ EK specific role. Yehi sahi default hai.
         ⚠ us role ko delete karke dobara banao aur trust chupchap toot jaata hai
           (wahi ARN, naya principal ID)
"Principal": { "Service": "lambda.amazonaws.com" }
     └─▶ Ek AWS service — isi se yeh "execution role" banta hai
"Principal": { "Federated": "arn:aws:iam::111:oidc-provider/token.actions.githubusercontent.com" }
     └─▶ Ek IdP; Action ban jaata hai sts:AssumeRoleWithWebIdentity
         ⚠ iske saath `sub` par Condition HONI CHAHIYE, warna duniya ka
           KOI BHI GitHub repo aapka role assume kar sakta hai
```

**Ab permission side — grants versus ceilings.** Yehi distinction "par policy toh allow kar rahi hai" wali confusion explain karta hai:

```
   ╔═══════════════════════╗            ╔═══════════════════════════╗
   ║  GRANTS (power dete)  ║            ║  CEILINGS (sirf ghatate)  ║
   ╠═══════════════════════╣            ╠═══════════════════════════╣
   ║ • Role par attached   ║            ║ • SCP            (account)║
   ║   identity policies   ║            ║ • Permission boundary     ║
   ║ • Target par resource ║            ║                   (role)  ║
   ║   policy              ║            ║ • Session policy (session)║
   ╚═══════════════════════╝            ╚═══════════════════════════╝
              └──────────────┬────────────────────┘
                             ▼
              EFFECTIVE = grant ∩ har ceiling
```

Toh ek permission boundary jismein `AdministratorAccess` hai wo **kuch bhi grant nahi karti** — sirf boundary lagao aur koi permission policy nahi, to role kuch bhi nahi kar sakta. Poori evaluation chain text form mein [Policy Types & Structure](#policy-types--structure) mein hai.

**Role ki permissions physically kahan attach hoti hain, aur quotas:**

```
   IAM ROLE
     ├─ Trust policy .............. exactly 1, mandatory
     ├─ Managed policies .......... 10 tak attached (20 tak raise ho sakta hai)
     ├─ Inline policies ........... total 10,240 characters
     └─ Permission boundary ....... 0 ya 1
```

**Session policies — wo layer jise koi place nahi kar paata.** Assume ke waqt pass hoti hai, role par store nahi hoti:

```
   Caller ──── sts:AssumeRole ────▶ STS ────▶ temporary credentials
                    ├── --policy '<json>'      inline, 2,048 chars
                    └── --policy-arns a,b,c    10 managed ARNs tak
                              ▼
              Sirf IS SESSION par lagti hai. Console mein invisible.
              Jo role ke paas pehle se nahi hai wo grant nahi kar sakti.
```

Inka point: **ek broad role, bahut si narrow sessions.** Ek multi-tenant service har request par wahi role assume karti hai par har session ko ek tenant ke key prefix tak clamp kar deti hai — toh ek bug tenants ke across nahi ja sakta, chahe role khud ja sakta ho.

**Symptom se diagnose karna:**

| Symptom | Kaunsi layer check karein |
|---|---|
| Role assume hi nahi ho raha | Trust policy (Gate 2) **aur** caller ka apna `sts:AssumeRole` (Gate 1) |
| Assume ho gaya, par har call deny | Koi permission policy attached nahi — shayad sirf boundary hai |
| Policy clearly allow karti hai, phir bhi deny | Koi ceiling clamp kar rahi hai (SCP / boundary / session policy), ya ek explicit `Deny` |
| Ek account mein chalta hai, dusre mein nahi | SCP different hai, ya target ki resource policy aapka naam nahi leti |
| Ek ghanta chala, phir toot gaya | Credentials expire — ya **role chaining**, jo sessions ko **1 ghante** par hard-cap karta hai, `MaxSessionDuration` chahe kuch bhi ho |
| Console mein policies sahi dikhti hain par access fail | Session policy — wo console mein dikhti hi nahi |

**Chaaron documents, end to end.** Cross-account ke liye *do* roles aur *chaar* policies chahiye, aur jo log bhool jaate hain wo yeh hai ki **source role ki apni trust policy hoti hai** jiska dusre account se koi lena-dena nahi. Scenario: **Account A (`111111111111`)** ka ek Lambda **Account B (`222222222222`)** ki DynamoDB table padhta hai.

```
  ACCOUNT A — 111111111111                   ACCOUNT B — 222222222222
  ┌─────────────────────────────┐            ┌─────────────────────────────┐
  │  OrderExportLambdaRole      │            │  DealsTableReaderRole       │
  ├─────────────────────────────┤            ├─────────────────────────────┤
  │ ① TRUST POLICY              │            │ ③ TRUST POLICY              │
  │   Principal: lambda.amaz…   │            │   Principal: role A ka ARN ─┼──┐
  │   Lambda is role ko kaise   │            │   ◀── GATE 2                │  │
  │   banta hai. Account B      │            │                             │  │
  │   kahin nahi aata.          │            │                             │  │
  ├─────────────────────────────┤            ├─────────────────────────────┤  │
  │ ② PERMISSION POLICY         │            │ ④ PERMISSION POLICY         │  │
  │   sts:AssumeRole            │            │   dynamodb:GetItem, Query   │  │
  │   Resource: role B ka ARN ──┼───┐        │   Resource: table/Deals     │  │
  │   ◀── GATE 1                │   │        │   ◀── GATE 3                │  │
  └─────────────────────────────┘   │        └─────────────────────────────┘  │
                                    └────────────────────────────────────────┘
                        ② B ka role naam leta hai  ·  ③ A ka role
                              dono ek dusre par point karte hain
```

**① Source role — trust policy (Account A).** *Kaun is role mein badal sakta hai?* Lambda service. Yeh ek normal execution-role trust policy hai — dhyaan do ki Account B ka zikr hi nahi hai:
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

**② Source role — permission policy (Account A).** *Yeh kya kar sakta hai?* Target role assume karna, plus apna local kaam. **Yeh Gate 1 hai:**
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
`Resource` ko exact role ARN par pin karo — yahan `"*"` Lambda ko *koi bhi* aisa role assume karne deta hai jo use trust karta ho. Aur dhyaan do kya **nahi** hai: koi `dynamodb:*` nahi. Table dusre account mein hai, toh is role par DynamoDB grant kuch bhi nahi karta.

**③ Target role — trust policy (Account B).** *Kaun is role mein badal sakta hai?* A ka wo specific role. **Yeh Gate 2 hai:**
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
`sts:TagSession` sirf tab daalo jab aap actually session tags pass karte ho. `aws:PrincipalOrgID` apni hi organisation ke *andar* sahi extra guard hai; third-party vendor ke liye `sts:ExternalId` use karo (neeche).

**④ Target role — permission policy (Account B).** *Assume hone ke baad yeh kya kar sakta hai?* Ek table padhna. **Yeh Gate 3 hai:**
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
GSI par `Query` karne ke liye `/index/*` ARN **zaruri** hai — sirf table ARN diya to har index query `AccessDenied` deti hai, jo aapke code ka bug lagta hai.

**Matrix — symmetry hi yaad rakhne wali cheez hai:**

| | **Trust policy** — kaun mujhe ban sakta hai | **Permission policy** — main kya kar sakta hoon |
|---|---|---|
| **Source role** (A) | `lambda.amazonaws.com` | **B ke role ARN** par `sts:AssumeRole` |
| **Target role** (B) | **A ka role ARN** | Table par `dynamodb:GetItem`/`Query` |

A ki permission policy B ke role ka naam leti hai; B ki trust policy A ke role ka naam leti hai. Dono hone chahiye — koi bhi account akela yeh access nahi de sakta.

**Call — do clients, kyunki credentials haath badalte hain:**
```csharp
// Ambient credentials = SOURCE role (Lambda execution role)
var sts = new AmazonSecurityTokenServiceClient();

var assumed = await sts.AssumeRoleAsync(new AssumeRoleRequest {
    RoleArn         = "arn:aws:iam::222222222222:role/DealsTableReaderRole",
    RoleSessionName = $"order-export-{context.AwsRequestId}"   // B ke CloudTrail mein dikhta hai
});

// Naya client, TARGET role ke temporary credentials se banaya gaya
var ddb = new AmazonDynamoDBClient(assumed.Credentials);
var deal = await ddb.GetItemAsync("Deals", key);
```
`RoleSessionName` wahi hai jo Account B ko `assumed-role/DealsTableReaderRole/<name>` ke roop mein dikhta hai — usmein request ID daalo, warna cross-account attribution impossible hai.

**Chaar traps:**

| Trap | Detail |
|---|---|
| Target permissions source role par daal dena | Sabse common wiring error. B ki permissions B ke role par hoti hain; source role ko sirf `sts:AssumeRole` chahiye. |
| Trusted role ko dobara banana | `Principal: <role ARN>` internally us role ke **unique principal ID** ke roop mein store hota hai. Role A ko delete karke usi naam se dobara banao aur B ka trust chupchap match karna band kar deta hai — B ki trust policy dobara save karke theek hota hai. |
| Terraform circular dependency | B ki trust policy A ke role ARN ko reference karti hai, jo pehle apply par exist nahi karta. Ya ARN ko resource reference ke bajaye string banao, ya `:root` se bootstrap karke dusre pass mein tighten karo. |
| Har invocation par dobara assume karna | Credentials 15 min–12 h chalte hain. Unhe warm container mein cache karo aur expiry par refresh karo, na ki per request ek STS round trip do. |

**External ID (vendor/third-party access — "confused deputy" attacks prevent karta hai):**
```json
{
  "Effect": "Allow",
  "Principal": { "AWS": "arn:aws:iam::111111111111:root" },
  "Action": "sts:AssumeRole",
  "Condition": { "StringEquals": { "sts:ExternalId": "vendor-unique-id-123" } }
}
```

**GitHub Actions → AWS via OIDC (modern, keyless CI/CD — senior interviews mein yeh expect karo):**
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
GitHub secrets mein koi static AWS keys stored nahi; short-lived, auditable, per repo/branch scoped.

**Cross-account & trust-policy scenario patterns jo ready rakhne layak hain:**
1. **Ek AWS service ek role assume kar rahi hai** (Lambda, EC2, ECS, CodeBuild) — trust policy mein ek service principal.
2. **Cross-account access** — target account ka role calling account/role ARN ko trust karta hai; caller ko apni side par bhi `sts:AssumeRole` permission chahiye.
3. **Cross-account + External ID** — third-party/vendor access, confused-deputy prevent karta hai.
4. **Condition-restricted trust** — `aws:SourceArn`/`aws:SourceAccount` se trust ko ek specific source service/resource tak narrow karo.
5. **OIDC federation** — GitHub Actions ya koi doosra CI system, no long-lived keys.
6. **SAML federation** — corporate AD/Okta/Entra ID users ko console/CLI access milta hai `sts:AssumeRoleWithSAML` se, ya (preferably aaj) IAM Identity Center se.

**Temporary credentials teen parts hain, do nahi:** `AccessKeyId`, `SecretAccessKey`, **aur ek `SessionToken`** — plus ek expiry timestamp. Session token hi hai jo unhe temporary banata hai; pehli do se signed lekin token ke bina request fail ho jaati hai. Yeh detail jaanna ek quick credibility signal hai, aur yeh batata hai ki aapko `AWS_SESSION_TOKEN` ko doosre env vars ke saath propagate kyun karna chahiye.

**STS API reference:**
| API | Use |
|---|---|
| `AssumeRole` | Main wala — apne khud ke ya doosre account mein ek role assume karna |
| `AssumeRoleWithSAML` | Ek SAML 2.0 IdP (ADFS, Okta, Entra ID) se aane wale callers |
| `AssumeRoleWithWebIdentity` | OIDC/web identity — GitHub Actions, Google/Facebook logins (mobile apps ke liye, AWS Cognito Identity Pools ko wrapper ke roop mein recommend karta hai) |
| `GetSessionToken` | Ek **IAM user** ke liye MFA-backed temporary credentials (role nahi) |
| `GetFederationToken` | Ek federated *user* ke liye temporary credentials |
| `GetCallerIdentity` | "Main kaun hoon?" — **koi permissions bilkul nahi** chahiye, isi liye yeh hamesha ek debugging probe ke roop mein kaam karta hai |

**Session duration aur role chaining:** default 1 hour; role ki *Maximum session duration* setting 1–12 hours allow karti hai (`--duration-seconds` usse exceed nahi kar sakta). **Role chaining** — ek assumed role se doosra role assume karna — **1 hour par hard-capped** hai aur extend nahi ho sakta, jo "hamara long-running batch job exactly 60 minutes mein die ho jaata hai" ka ek common cause hai. Ek meaningful `--role-session-name` bhi pass karo: yeh CloudTrail mein appear hota hai, aur trace karne ka yeh hi tarika hai ki *kaunsa human* ne ek shared role use kiya.

**Ek role actually aapke compute tak kaise pahunchta hai** (yeh part easily hand-wave ho jaata hai aur probe kiya jaata hai):
| Compute | Delivery mechanism |
|---|---|
| **EC2** | Ek **instance profile** — ek container jismein exactly **ek** role hota hai, aur wahi cheez actually instance par attached hoti hai. Ek role EC2 ko directly attach nahi ho sakta. Console silently role jaise hi naam se instance profile create kar deta hai; **CLI/CloudFormation/Terraform ke saath aapko khud isse create karna padta hai**, isi liye "role exist karta hai lekin instance usse use nahi kar sakta" ek kaafi common IaC bug hai. |
| **Lambda** | Execution role, invoke time par `lambda.amazonaws.com` se assumed |
| **ECS** | **Do alag roles** — **task execution role** (ECS agent use karta hai image ECR se pull karne aur logs likhne ke liye) vs **task role** (*aapke application code* se use hota hai). Inhe mix karna ek genuine production bug hai: aapki app ko DynamoDB par `AccessDenied` milta hai kyunki permission execution role mein add hui thi. |
| **EKS** | **IRSA** (IAM Roles for Service Accounts) ya newer **EKS Pod Identity** — ek Kubernetes service account ek IAM role se OIDC provider ke through map hota hai, isliye har pod ko node ka role share karne ke bajaye apna least-privilege role milta hai. |

**EC2 instance profile ek line mein:**
- **Yeh kya hai:** ek role ke around ek thin **wrapper**. Role permissions hold karta hai; profile wo object hai jise EC2 actually attach kar sakta hai. Do naam ek feel hone wali cheez ke liye, isi liye yeh confuse karta hai.
- **Ek profile mein exactly ek role hota hai** — lekin same role many profiles mein baith sakta hai.
- **Console isse hide karta hai.** EC2 console mein role pick karo aur wo background mein matching profile create kar deta hai, isliye zyadatar log kabhi seekhte nahi ki yeh exist karta hai — jab tak wo **Terraform/CloudFormation** likhte hain, jahan yeh ek separate resource hai jise declare aur reference karna padta hai.
- **Ek running instance par isse attach ya swap karo** — kuch minutes mein effect hota hai, **no reboot**, kyunki SDK simply IMDS se naye credentials pick kar leta hai.
- **Failure signature:** role IAM mein correct dikhta hai lekin instance behave karta hai jaise usse koi permissions hi na ho → ya toh koi instance profile exist nahi karta, ya profile exist karta hai bina role ke.

**IMDS — credentials physically EC2 par kaise aate hain:**
```bash
# IMDSv2 (session-oriented, and what you should require)
TOKEN=$(curl -X PUT "http://169.254.169.254/latest/api/token" \
  -H "X-aws-ec2-metadata-token-ttl-seconds: 21600")
curl -H "X-aws-ec2-metadata-token: $TOKEN" \
  http://169.254.169.254/latest/meta-data/iam/security-credentials/
```
SDK yeh automatically karta hai aur **credentials expire hone se pehle refresh kar deta hai** — isi liye aapko unhe kabhi cache ya manually manage nahi karna chahiye. **IMDSv1 vs IMDSv2** ek real security question hai: v1 ek plain `GET` ka answer deta hai, isliye aapki app mein koi SSRF bug (ya ek misconfigured reverse proxy) instance ke credentials fetch karne ke liye trick ho sakta hai — kuch well-known breaches ka root cause. **IMDSv2 ko pehle token paane ke liye ek `PUT` chahiye**, jo ek simple SSRF nahi kar sakta. Apne Terraform/CloudFormation launch template mein hamesha `HttpTokens: required` (aur `HttpPutResponseHopLimit: 1`) set karo.

**Service-linked roles:** predefined roles jo *ek AWS service ke owned* hote hain (e.g. `AWSServiceRoleForECS`, `AWSServiceRoleForAutoScaling`). Service trust policy aur permissions manage karta hai; aap unhe edit nahi kar sakte, aur generally unhe sirf tab delete kiya ja sakta hai jab service ko unki zarurat na rahe. Point yeh hai: yeh exist karte hain taaki ek service aapke account mein act kar sake bina aapko uski trust relationship hand-build kiye.

**`iam:PassRole` — wo privilege-escalation control jise log bhool jaate hain.** `sts:AssumeRole` hai "mujhe iss role *ban* jaane do". `iam:PassRole` hai "mujhe yeh role ek AWS service ko *hand over* karne do" — ek EC2 instance ko role ke saath launch karne, ek execution role ke saath Lambda create karne, ya ek ECS task definition register karne ke liye chahiye. Yeh kyun matter karta hai: ek user jiske paas `lambda:CreateFunction` plus **unrestricted** `iam:PassRole` ho, `AdministratorAccess` wala ek Lambda create kar sakta hai aur admin ke roop mein arbitrary code run kar sakta hai — ek seemingly modest permission set se ek full escalation. Hamesha `PassRole` ko specific role ARNs tak scope karo, aur `iam:PassedToService` conditions add karo:
```json
{
  "Effect": "Allow",
  "Action": "iam:PassRole",
  "Resource": "arn:aws:iam::123456789012:role/lambda-prod-order-writer-role",
  "Condition": { "StringEquals": { "iam:PassedToService": "lambda.amazonaws.com" } }
}
```

**IAM Identity Center (pehle AWS SSO) — *human* access ke liye modern answer.** Users ek built-in identity store ya aapke corporate IdP (Entra ID/Okta) mein rehte hain; aap **permission sets** assign karte ho, jinhe Identity Center har member account mein roles ke roop mein materialise kar deta hai. Log `aws sso login` se short-lived credentials paate hain, ek leaver ko offboard karne ki ek jagah hoti hai, aur kahin bhi koi long-lived keys exist nahi karti. Toh jab poocha jaaye *"kya humein abhi bhi IAM users create karte rehna chahiye?"* answer hai: **nahi, humans ke liye nahi** — Identity Center ya federation, IAM users sirf legacy apps ke liye reserved jo genuinely role assume nahi kar sakti aur ek break-glass account ke liye.

**Interviews mein recite karne layak mental model:** "Trust Policy = building mein enter karne ki permission kisko hai. Permission Policy = andar aane ke baad wo kaunse rooms access kar sakte hain. AssumeRole = darwaze par issue hone wala temporary access badge."

### Least Privilege & Permission Boundaries in Practice

**Anti-pattern (real .NET/Lambda code mein constantly dikhta hai):**
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

**Permission boundaries** SCPs se ek separate mechanism hain: ek boundary ek *role ya user* par attached hoti hai aur define karti hai ki wo identity kabhi kitni maximum permissions rakh sakti hai, uski attached policies kuch bhi kahein — commonly platform/security teams use karte hain apni application teams ko apne roles/policies create karne dene ke liye ek safe ceiling ke andar (e.g., "aap kisi bhi role create kar sakte ho, lekin yeh iss boundary policy se kabhi exceed nahi kar sakta"). Yeh **SCP** (Service Control Policy) se different hai, jo AWS Organizations level par entire accounts/OUs par apply hoti hai, individual identities par nahi.

---

## 4. Secrets & Parameters

### Secrets Manager vs Parameter Store

> **Containers mein *do* roles kaam karte hain:** **execution role** ECS agent ke inject kiye secrets resolve karta hai, **task role** se aapka code runtime par fetch karta hai — dekhein [ECS → RDS: Database Connection ki Chaar Layers](10-databases-caching-analytics.md#ecs--rds-database-connection-ki-chaar-layers).

Original notes multiple jagah "Secrets Manager or Parameter Store" reference karte hain secrets secure karne ke liye (Lambda/ECS/CodeBuild sections), lekin unhe kabhi actually compare nahi karte — ek direct comparison ek bahut common senior AWS interview question hai.

| | Secrets Manager | SSM Parameter Store |
|---|---|---|
| Cost | Per-secret + per-API-call charge | Standard tier free; Advanced tier mein small charge |
| Automatic rotation | Built-in (native RDS/Redshift/DocumentDB integrations, ya custom Lambda rotation function) | Native rotation nahi — khud banana padta hai |
| Versioning | Haan | Haan |
| Encryption | KMS, always encrypted | KMS optional (SecureString) ya plaintext (String) |
| Max size | 64 KB | 4 KB (Standard) / 8 KB (Advanced) |
| Cross-account/cross-region replication | Native replication support | Manual/custom |
| Typical .NET use | DB connection strings, rotation chahiye wali API keys | App config, feature flags, non-rotating settings |

**Senior-level guidance:** kisi bhi chiz ke liye jise rotation chahiye ya jo ek genuine credential hai (DB passwords, third-party API keys), **Secrets Manager** use karo; configuration values aur secrets jinhe automatic rotation nahi chahiye, unke liye scale par cost control ke liye **Parameter Store** use karo (hundreds/thousands of config values). Ek common cost-optimization talking point: kaafi teams pure configuration ke liye Secrets Manager over-use karte hain, un values ke liye rotation/API-call overhead pay karte hain jo kabhi rotate nahi hoti — Parameter Store (SecureString) wahan correct, cheaper tool hai.

**.NET retrieval example:**
```csharp
var client = new AmazonSecretsManagerClient();
var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = "prod/orders/db" });
var connectionString = response.SecretString;
```

#### Secrets Manager — Pitfalls

Service adopt karna easy hai aur subtly galat karna bhi easy hai. Yeh wo failures hain jo actually hoti hain:

**❗ 1. Rotation aapki application down kar sakta hai.** Rotation secret ka ek **naya version** banata hai aur **`AWSCURRENT`** staging label ko usse move karta hai; purana version **`AWSPREVIOUS`** ban jaata hai. Agar aapki app secret ko **startup par ek baar** padhti hai aur usse forever cache karti hai, toh yeh purana password use karti rehti hai — aur jaise hi rotation usse invalidate karta hai, har connection 3 baje fail ho jaata hai bina koi deploy hue.

Fix yeh hai ki **auth failure par re-fetch karo**, poll mat karo: auth error catch karo, `AWSCURRENT` phir se pull karo, ek baar retry karo. RDS credentials ki managed rotation ke liye, **[RDS Proxy](10-databases-caching-analytics.md#rds-proxy)** ya **IAM database authentication** prefer karo, jo password ko app se entirely remove kar dete hain. Four-step rotation Lambda (`createSecret` → `setSecret` → `testSecret` → `finishSecret`) ke paas bhi ek **two-user strategy** hai exactly isi wajah se ki swap ke dauraan hamesha ek valid credential ho — naam lene layak.

**❗ 2. Har request par `GetSecretValue` call mat karo.** Yeh ek network call hai jiski **throttling quota** hai, aur aap per 10,000 calls pay karte ho. Ek busy API jo yeh per request karti hai throttle ho jaayegi aur har call mein latency add karegi. Ise memory mein TTL ke saath cache karo — **AWS Secrets Manager caching library**, ya **Parameters and Secrets Lambda extension** (ek local HTTP cache sidecar). Yeh service ke saath sabse common performance mistake hai.

**3. Scale par cost surprises.** ~$0.40 per secret per month *plus* API calls. Yeh 20 database credentials ke liye trivial hai aur **2,000 config values** ke liye material hai — jo exactly upar flagged over-use pattern hai. Feature flags aur app settings **Parameter Store Standard (free)** mein belong karti hain.

**4. Jab secret customer-managed key use karta hai toh IAM *aur* KMS dono permissions chahiye** — `secretsmanager:GetSecretValue` **aur** `kms:Decrypt`. [Fargate/S3/CMK example](12-security-services.md#worked-example-fargate-task-ko-kms-encrypted-s3-data-ka-access-dena) jaisa hi trap: IAM policy right dikhti hai aur call abhi bhi fail hota hai.

**5. Ek private subnet ko VPC endpoint chahiye.** `com.amazonaws.<region>.secretsmanager` ek **interface** endpoint hai — koi gateway option nahi hai. Iske bina (ya ek NAT gateway) call clearly error karne ke bajaye hang ho jaata hai.

**6. Deletion mein mandatory 7–30 din ka recovery window hai.** Aap same name se ek secret immediately recreate nahi kar sakte, jo teardown-and-recreate CI pipelines ko break karta hai. `ForceDeleteWithoutRecovery` isse bypass karta hai — aur aapka safety net remove kar deta hai.

**7. Injected secrets sirf start par resolve hote hain.** ECS `valueFrom` aur Lambda environment variables tab populate hote hain jab **task start** hota hai ya execution environment initialise hota hai. Secret rotate karna running task ko **update nahi** karta — aapko redeploy karna padta hai, ya code mein padhna padta hai. Log assume karte hain ki injection ka matlab live updates hai; nahi hota.

**8. Rotation Lambda ko database tak network access chahiye.** Agar DB private subnets mein hai, rotation function VPC-attached hona chahiye right security groups ke saath — warna rotation silently fail hoti hai aur aapko yeh tab pata chalta hai jab secret stale hoti hai.

**9. Cross-region replication opt-in hai, aur replicas read-only hain.** DR ke liye deliberately enable karo; assume mat karo ki ek multi-region app local copy par write kar sakti hai.

**10. Environment variables mein secrets visible hote hain.** Kuch bhi env var ke roop mein injected `docker inspect`, task-definition JSON, aur often crash dumps ya logs mein dikhta hai. Code mein fetch karna aur memory mein hold karna stronger hai; ECS `valueFrom` kam se kam literal ko task definition se bahar rakhta hai, lekin yeh abhi bhi container ke environment mein land karta hai.

**One-line summary:** *"Rotate hone wale credentials ke liye Secrets Manager, configuration ke liye Parameter Store — aur value ko TTL ke saath cache karo jabki auth failure par re-fetch karo, kyunki real production incident ek leaked secret nahi hota, ek rotated secret hota hai jise app ne kabhi phir se nahi padha."*

---

## 5. Tooling, Pitfalls & Drills

### IAM Security Tools

| Tool | Scope | Yeh aapko kya deta hai |
|---|---|---|
| **Credential Report** | Poora account (CSV download) | Per user ek row: password enabled/last used/last changed, **MFA active yes/no**, access key age, key last-used date aur service. Bina MFA wale users, 90 din se purani keys, aur kabhi use na hui keys (delete karna safe) dhundhne ka sabse fast tarika. |
| **Access Advisor** (last-accessed data) | Per user/role/group/policy | Identity ko kaunse **services** granted kiye gaye the aur **kab last use hua**. "AdministratorAccess kyunki yeh kaam kar gaya" se least privilege tak jaane ka practical route: 40 services grant hue, 12 mahine mein 4 use hue → baaki 36 remove karo. |
| **IAM Access Analyzer** | Account / Organization | Aapke account ya org ke **bahar** shared resources ke findings (public buckets, over-broad trust policies, KMS keys); likhte waqt **policy validation**; **unused access findings** (unused roles, keys, permissions); aur **CloudTrail history se ek least-privilege policy generate karna** — "ek tight policy kaise likhte ho?" ka sabse best answer. |
| **Policy Simulator** | Policy testing | "Kya yeh principal X par Y karne allowed hoga?" ka answer deta hai **call karke bina**, aur dikhata hai kaunsa statement decide karta hai. Ise ship karne se pehle use karo, aur prove karo ki koi SCP ya boundary real blocker hai. |
| **CloudTrail** | Audit trail | Har API call: kaun, kab, source IP, kaunsa role session (isliye `--role-session-name`). "Bucket kisne delete kiya yeh kaise pata karte ho?" ka answer aur unusual `AssumeRole` activity par alert kaise karte ho. |
| **AWS Config** | Continuous compliance | Managed rules jaise `iam-user-mfa-enabled`, `access-keys-rotated`, `iam-policy-no-statements-with-admin-access` — ek one-off audit ko ek always-on check with remediation mein badal deta hai. |

```bash
aws iam generate-credential-report
aws iam get-credential-report --query Content --output text | base64 -d > report.csv
aws iam get-account-authorization-details > iam-snapshot.json     # full policy/role dump for offline review
aws accessanalyzer list-findings --analyzer-arn <arn>
```

**Interview mein yeh kaise frame karo:** sirf tools ka naam mat lo — har ek ko us question se pair karo jo wo answer karta hai. "Credential report bataata hai *kya nahi hona chahiye* (stale keys, missing MFA); Access Advisor bataata hai *kya over-granted* hai; Access Analyzer bataata hai *kya externally exposed* hai aur real CloudTrail usage se tighter policy generate kar sakta hai; simulator change ship hone se pehle verify karne deta hai."

### IAM Pitfalls

| Pitfall | Yeh kyun hota hai | Fix |
|---|---|---|
| Trust vs permission policy confuse karna | "Maine S3 access diya lekin AssumeRole abhi bhi fail ho raha hai" | Dono required hain; trust policy ka `sts:AssumeRole` grant action permissions se separately check karo |
| `AdministratorAccess` overuse karna | "Just kaam karwane ke liye" | Read-only se start karo, incrementally add karo, action+resource se scope karo |
| Bhool jaana ki explicit Deny hamesha jeetta hai | SCPs, permission boundaries, resource policies silently ek Allow ko override kar sakte hain | Debugging karte waqt: SCP → permission boundary → resource policy → identity policy, isi order mein check karo |
| Credentials hardcode karna | Convenience | Roles use karo; SDK ko credentials automatically fetch karne do; CI/CD ke liye OIDC |
| VPC mein single-AZ Lambda ENIs | Function ek AZ outage ke dauraan fail ho jaata hai | Lambda ke VPC config ko multiple subnets/AZs ke across configure karo — IAM khud AZ-agnostic hai, execution nahi |
| Ek role ko many services ke across reuse karna | Permissions creep, audit karna hard, compromise hone par blast radius | Per service ek role, clear naming convention |
| Overly broad trust principal (`"Principal": "*"`) | Koi bhi role assume kar sakta hai | Specific account/service/OIDC provider tak restrict karo, `aws:SourceArn`/`aws:SourceAccount` conditions add karo |
| Policies par koi conditions nahi | Ek action ko resource/context scope kiye bina allow karna | `aws:SourceArn`, `aws:SourceVpc`, `s3:prefix`, `kms:EncryptionContext` use karo |
| Permission boundaries ignore karna | "Mera role X kyun nahi kar sakta jab uski policy allow karti hai?" | Boundary *maximum* possible permission define karti hai — role policy boundary ka subset hoti hai, enterprise AWS orgs mein common |
| Yeh assume karna ki roles kabhi expire nahi hote | App kuch hours ke baad break ho jaati hai | STS credentials expire hote hain (15 min–12 hrs); SDK ko auto-refresh karne do, manually kabhi cache mat karo |
| Deep role chaining (A→B→C→D) | Debugging nightmare, shrinking max session duration | Chains shallow rakho, direct trust relationships prefer karo |
| AssumeRole ka koi CloudTrail auditing nahi | Kisne kya assume kiya iska koi visibility nahi | CloudTrail enable karo, unusual `AssumeRole`/`AssumeRoleWithWebIdentity` activity par alert karo |
| Yeh bhool jaana ki resource-based policy bhi required hai | Identity policy allow karti hai, lekin SQS/KMS/S3 resource policy nahi karti | Kuch services **dono sides** ko access allow karna require karti hain (S3 cross-account, SQS, KMS, SNS) |
| Poor role naming (`test-role`, `my-role`) | Audits confusing, risky reuse | `service-env-purpose-role` use karo, e.g. `lambda-prod-order-writer-role` |
| "Set and forget" IAM | Jaise services add/remove hoti hain waise permissions creep hoti hai | Periodic reviews + IAM Access Analyzer |
| Unrestricted `iam:PassRole` | `lambda:CreateFunction` ke saath harmless lagta hai, lekin admin tak ek full privilege-escalation path hai | `PassRole` ko specific role ARNs + `iam:PassedToService` condition tak scope karo |
| EC2 par **IMDSv1** enabled chodna | Older AMIs/launch templates par default; koi bhi SSRF bug phir instance role ke credentials steal kar sakta hai | Launch template/Terraform mein IMDSv2 require karo (`HttpTokens: required`, hop limit 1) |
| Ek instance par stale `AWS_ACCESS_KEY_ID` env vars | Env vars credential chain mein instance profile ke **upar** baithte hain, isliye silently role ko shadow karte hain | `aws sts get-caller-identity` — agar yeh `user/...` dikhaye, `assumed-role/...` nahi, env vars unset karo |
| ECS **task execution role** mein permission add karna jab actually **task role** mein chahiye tha | Dono log ke sar mein "ECS role" hi hote hain | Execution role = ECS agent (ECR pull, logs); task role = aapki app code ki permissions |
| **Root** user ke liye access keys create karna, ya root ko day-to-day use karna | "Yeh account tha jo already mere paas tha" | Root par MFA, root keys kabhi create mat karo, day one par ek admin identity create karo (ideally Identity Center) |
| Yeh assume karna ki `AdministratorAccess` literally sab kuch kar sakta hai | Yeh root-only actions perform nahi kar sakta (account close, support plan change, S3 MFA-delete) | Root-only list jaano — dekho [IAM Overview](#iam-overview-root-account--shared-responsibility) |

**Golden debugging checklist "mere role ke paas permission hai lekin access abhi bhi fail ho raha hai" ke liye:** execution role permission → trust policy → resource-based policy (bucket/queue/key policy) → KMS key policy (agar encrypted hai) → SCP/permission boundary.

### IAM Rapid-Fire Q&A

Fundamentals ke liye short-answer drill. Longer scenario answers [Sample Interview Q&A](19-cross-cutting-reference.md#sample-interview-qa) mein hain.

**Q: IAM regional hai ya global?**
A: Global. IAM ke liye aap kabhi region select nahi karte, aur same users/roles/policies har jagah apply hoti hain. Side effect: yeh eventually consistent hai, isliye ek freshly created role kuch seconds tak usable na ho sakta hai.

**Q: User vs Group vs Role — ek sentence mein har ek?**
A: Ek user long-term credentials wala ek person hai; ek group sirf users hold karne wala ek permission container hai (no nesting, koi identity nahi, `Principal` nahi ban sakta); ek role permissions hai bina permanent credentials ke jise ek trusted service/account/federated identity temporarily assume karti hai.

**Q: Ek bilkul nayi IAM user bina koi policy attached kiye kya kar sakti hai?**
A: Kuch bhi nahi — bucket list bhi nahi kar sakti. IAM deny-by-default hai; har permission wo hai jo aapne explicitly grant ki hai.

**Q: Ek policy ek action allow karti hai aur doosri deny karti hai. Kya hota hai?**
A: **Explicit Deny hamesha jeetta hai**, aur kuch bhi ise override nahi kar sakta — na `AdministratorAccess`, na koi resource policy. Agar koi policy action ka mention nahi karti, yeh ek implicit deny hai, toh wo bhi denied hai.

**Q: EC2 par ek app ke liye role access keys se better kyun hai?**
A: Box par ya code/Git mein kuch bhi secret store nahi hota, credentials temporary hote hain aur SDK expiry se pehle auto-refresh karta hai, permissions ko redeploy kiye bina centrally change kiya ja sakta hai, aur rotate ya leak karne layak kuch nahi hai. Do commands mein prove karo: role se pehle `aws s3 ls` "Unable to locate credentials" se fail hota hai, role ke baad kaam karta hai.

**Q: Ek role par do policies kya hoti hain, aur pata kaise chalta hai kaunsi broken hai?**
A: **Trust policy** (kaun assume kar sakta hai — iske paas `Principal` hota hai) aur **permissions policy** (assumed hone ke baad kya kar sakta hai). Agar `sts:AssumeRole` khud "not authorized to perform sts:AssumeRole" se fail ho, yeh trust policy hai; agar aap successfully assume kar lete ho lekin API call fail ho jaata hai, yeh permissions policy hai.

**Q: EC2 par ek application ko S3 access kaise dete ho?**
A: Ek role banao jo `ec2.amazonaws.com` ko trust kare, ek scoped S3 policy attach karo, role ko instance par attach karo — **instance profile** ke through, jo ki actually attach hota hai (ek role directly EC2 ko attach nahi ho sakta). SDK phir automatically IMDS se credentials pick karta hai.

**Q: Cross-account access ke do tarike kya hain?**
A: (1) **AssumeRole** — target account ka role caller ke account ko trust karta hai aur caller ke paas `sts:AssumeRole` hoti hai; caller *ban jaata hai* wo role aur uss session ke liye apni permissions lose kar deta hai. (2) **Resource-based policy** — bucket/queue/key policy directly outside principal ka naam leti hai; caller apni identity *rakhta* hai, aur dono sides ko allow karna padta hai.

**Q: STS exactly kya return karta hai, aur kitne time ke liye?**
A: `AccessKeyId`, `SecretAccessKey`, aur ek **`SessionToken`**, plus ek expiry — role ki maximum session duration ke hisab se 15 minutes se 12 hours tak. **Role chaining 1 hour par hard-capped hai** aur extend nahi ho sakta.

**Q: Inline vs managed policy — kaunsi recommend karoge?**
A: Customer-managed. Yeh reusable hai, versioned hai (5 versions, isliye rollback kar sakte ho), aur aap dekh sakte ho yeh kahan-kahan attached hai. Inline policies audits ke liye invisible, unreusable hoti hain, aur identity ke saath hi die ho jaati hain. AWS-managed policies shuru karne ke liye theek hain lekin almost hamesha zarurat se broad hoti hain.

**Q: Permissions boundary vs SCP?**
A: Ek boundary **user ya role** par attach hoti hai aur uski maximum permissions cap karti hai (effective = policy ∩ boundary); ek SCP **Organizations account/OU** level par attach hoti hai aur account ke sabko limit karti hai, uske root user ko bhi. Koi bhi kabhi kuch *grant* nahi karta — dono sirf subtract kar sakte hain.

**Q: Confused deputy problem kya hai aur ise kaise fix karte ho?**
A: Ek third party jo ek role hold karti hai jo unke sabhi customers ko serve karta hai, unhe trick kiya ja sakta hai *aapke* account ke against unka access use karne ke liye. Fix: wo aapko ek unique **External ID** issue karte hain aur aapki trust policy usse `sts:ExternalId` condition ke through require karti hai. AWS service principals ke liye, equivalent controls `aws:SourceArn`/`aws:SourceAccount` hain.

**Q: `iam:PassRole` kyun matter karta hai?**
A: Yeh ek *role ko ek service ko hand karne* ki permission hai, `sts:AssumeRole` (ek ban jaana) se alag. Unrestricted, yeh `lambda:CreateFunction` ko full admin mein badal deta hai — admin execution role ke saath ek function create karo aur jo chaho wo run karo. Ise specific role ARNs tak `iam:PassedToService` condition ke saath scope karo.

**Q: Kya `AdministratorAccess` account mein sab kuch kar sakta hai?**
A: Nahi. Root-only actions rehte hain — account close karna, root email/support plan change karna, S3 MFA-delete, RI Marketplace registration. Yeh ek favourite trick question hai.

**Q: CLI/API calls ke liye MFA kaise enforce karte ho, sirf console ke liye nahi?**
A: MFA natively sirf console sign-in ko protect karta hai. CLI/API ke liye aap ek policy condition `"Bool": {"aws:MultiFactorAuthPresent": "true"}` add karte ho aur caller `sts:GetSessionToken` (IAM user) ya `AssumeRole` `--serial-number`/`--token-code` ke saath ek MFA-backed session obtain karta hai. **Trust policy** mein MFA require karna production cross-account access ke liye standard control hai.

**Q: Aapne abhi inherit kiya ek account mein over-permissioned identities kaise dhundhoge?**
A: Kya nahi hona chahiye (no MFA, stale/never-used keys) ke liye credential report, granted lekin kabhi use na hui services ke liye **Access Advisor** last-accessed data, external exposure aur unused-access findings ke liye **IAM Access Analyzer**, aur actual usage se tight policy rebuild karne ke liye uska generate-policy-from-CloudTrail feature. Ship karne se pehle har proposed change ko **policy simulator** mein verify karo.

**Q: Aaj bhi IAM users create karna chahiye?**
A: Humans ke liye nahi — **IAM Identity Center** (ya SAML/OIDC federation) use karo taaki access centrally managed, short-lived ho, aur offboarding ek jagah ho. IAM users sirf legacy apps ke liye rakho jo genuinely role assume nahi kar sakti, plus ek break-glass account.

**Q (scenario): Hamara nightly batch job almost exactly 60 minutes mein die ho jaata hai. Kyun?**
A: Role chaining — job ek already-assumed role se ek role assume karta hai, jo session ko 1 hour tak cap kar deta hai role ki max-duration setting kuch bhi ho. Ya to target role ko base identity se directly assume karo (taaki 1–12 hours available hon), ya job ko ek session hold karne ke bajaye credentials refresh karne do.

**Q (scenario): EC2 role clearly `s3:GetObject` allow karta hai, lekin app abhi bhi AccessDenied deti hai. Kahan dekhoge?**
A: Order mein: kya app actually role use kar rahi hai (`aws sts get-caller-identity` — `assumed-role/...` ke bajaye ek `user/...` ARN matlab stale `AWS_*` env vars instance profile ko shadow kar rahe hain, kyunki env vars credential chain mein IMDS se upar rank karte hain); phir bucket policy; phir KMS key policy agar object SSE-KMS encrypted hai; phir SCP/permissions boundary. Yeh bhi confirm karo ki instance profile bilkul exist karta hai — Terraform/CloudFormation ke saath role bina profile ke exist kar sakta hai.

**Disaster Recovery — IAM & Security**

| | |
|---|---|
| **Actually risk par kya hai** | Roles, policies, trust relationships, Identity Center assignments. IAM **global** hai, toh regional outage ise chhuta hi nahi — realistic disaster *aap khud* ho, ek bad apply ya deletion se |
| **Backup mechanism** | **IaC hi ek matra backup hai** — IAM mein snapshot facility nahi hai. **AWS Config** batata hai kya badla; **CloudTrail** batata hai kisne badla |
| **Realistic RPO / RTO** | RPO = last commit. RTO minutes |

**Recovery runbook:**
1. **Identify karo:** AWS Config se us role ka resource timeline; CloudTrail se `DeleteRole` / `PutRolePolicy` event, actor ke saath.
2. **IaC se re-apply karo.** Console mein haath se fix mat karo — wahi drift banta hai jisse baad mein ladna padega.
3. **Jo bhi exposed hua** use rotate karo: access keys, aur wo har secret jo compromised principal padh sakta tha.
4. **Verify karo** IAM Access Analyzer se ki aapne intended boundary restore ki hai, usse zyada kuch nahi.

⚠️ **Gotcha, aur yehi wo hai jo actually chubhta hai:** **aap wahi IAM tod sakte ho jiski zarurat IAM theek karne ke liye hai.** Ek **break-glass role** rakho vaulted MFA credential ke saath, jo usi pipeline se managed *na* ho jo use delete kar sakti hai. Aur yeh jaan lo: deleted role ko usi naam se dobara banane par use **naya unique principal ID** milta hai — ARN-based trusts recover ho jaate hain, par principal ID par pinned kuch bhi chupchap toota rehta hai, jo incident ke dauraan bahut confusing ghanta hota hai.

---

## 6. Hands-On

### Hands-On: Users & Groups

1. IAM console → **Users** → *Create user* → naam do.
2. **Provide user access to the AWS Management Console** tick karo sirf tab jab ek human ko UI ki zarurat ho.
3. Auto-generated ya custom password choose karo; *user must create a new password at next sign-in* ticked chodo.
4. **Permissions** → *Add user to group* → ek group create/select karo (e.g. ek `admin` group jo `AdministratorAccess` carry kare). Group par attach karna, user par nahi, demonstrate karne layak habit hai.
5. Create karo → **`.csv` download karo**. Password aur secret **ek baar** dikhte hain aur uske baad unrecoverable hain — aap unhe "look up" nahi karte, delete aur reissue karte ho.
6. **Account alias** (IAM dashboard → *Account Alias*) ugly sign-in URL `https://123456789012.signin.aws.amazon.com/console` ko `https://my-company.signin.aws.amazon.com/console` mein badal deta hai.

```bash
aws iam create-group  --group-name Developers
aws iam attach-group-policy --group-name Developers \
  --policy-arn arn:aws:iam::aws:policy/ReadOnlyAccess
aws iam create-user   --user-name parteek
aws iam add-user-to-group --user-name parteek --group-name Developers
aws iam list-groups-for-user --user-name parteek     # verify
```

**Point banaane layak:** IAM users *ek specific account* mein us account URL/alias se sign in karte hain; root **email address** se sign in karta hai. Isi se aap ek screenshot se bata sakte ho ki koi kaunsa use kar raha hai.

### Hands-On: MFA & Access Keys

**Virtual MFA enable karo:** IAM → Users → *your user* → **Security credentials** → *Assign MFA device* → naam do → **Authenticator app** → QR code scan karo → **do consecutive codes** enter karo (AWS pair use karta hai time drift sync karne ke liye) → sign out aur back in karo confirm karne ke liye.

**Access key create aur use karo:** Security credentials → *Create access key* → CLI use case choose karo → warning acknowledge karo → `.csv` download karo.
```bash
aws configure
# AWS Access Key ID:     AKIA...
# AWS Secret Access Key: ****
# Default region name:   us-east-1
# Default output format: json
aws sts get-caller-identity      # confirms which identity the key belongs to
```

**Rotation drill jo recite karne layak hai:**
```bash
aws iam create-access-key  --user-name parteek       # key #2 (now at the limit of 2)
# update apps/CI to key #2, deploy, and verify traffic is using it
aws iam update-access-key  --user-name parteek --access-key-id AKIA_OLD --status Inactive
# soak: if anything breaks, flip it back to Active
aws iam delete-access-key  --user-name parteek --access-key-id AKIA_OLD
```

### Hands-On: IAM Roles

**EC2 → S3 role (yeh demo roles ko click karwata hai):**
1. IAM → **Roles** → *Create role* → **AWS service** → **EC2**. (Yeh aapke liye trust policy likh deta hai.)
2. Permissions attach karo — demo ke liye `AmazonS3ReadOnlyAccess`; real life mein ek scoped customer-managed policy.
3. Naam do `ec2-dev-s3-reader-role` → Create.
4. EC2 → instance select karo → **Actions → Security → Modify IAM role** → select karo → Update. **Immediately effect hota hai, no reboot.**

```bash
# BEFORE attaching the role, on the instance:
aws s3 ls
# → "Unable to locate credentials"

# AFTER attaching:
aws sts get-caller-identity
# → arn:aws:sts::123456789012:assumed-role/ec2-dev-s3-reader-role/i-0abc123
aws s3 ls        # works — and there are no access keys anywhere on the box
```
Yeh before/after do commands mein roles vs access keys ka poora argument hai.

**Cross-account role:** *Create role* → **AWS account** → *Another AWS account* → trusting account ID enter karo → optionally **Require MFA** aur/ya **Require external ID** tick karo → permissions attach karo → role ARN doosre account ko hand over karo. Wo console **Switch role** se use karte hain, ya:
```bash
aws sts assume-role \
  --role-arn arn:aws:iam::222222222222:role/CrossAccountReadRole \
  --role-session-name parteek-audit-2026-08 \
  --duration-seconds 3600
# export the three values, then verify you actually became the role:
aws sts get-caller-identity
```
Ya, day-to-day work ke liye cleaner, CLI ko `~/.aws/config` ke through assumption khud karne do:
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
