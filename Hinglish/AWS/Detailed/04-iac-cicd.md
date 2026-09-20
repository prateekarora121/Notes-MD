> **AWS Detailed Guide** · [Index](README.md) · Part I

# Infrastructure as Code & CI/CD

---

## 1. AWS Native CI/CD

### AWS CodeCommit

**What it is:** AWS ka apna managed Git repository service — same Git semantics/CLI jo aap already use karte ho (clone, push, pull, branches, "pull requests" ke through PRs), bas hosted aur access-controlled IAM ke through hota hai ek third-party SaaS account ke bajaye. Yeh CodePipeline ke liye kai valid **Source** stage providers mein se ek hai, GitHub, Bitbucket, aur S3 ke saath (third-party wale ke liye CodeStar Connections ke through — neeche trap-scenario table dekho).

**Yeh interviews mein kyun aata hai jabki practice mein GitHub dominate karta hai:** CodeCommit ka exist karna jaanna — aur yeh ki yeh IAM-native hai (repo access same policies/roles se controlled hai jo account ki har cheez control karte hain, koi separate SaaS permission model reconcile nahi karna padta) — yehi actual point hai jo test kiya ja raha hai, yeh claim nahi ki aap real team ke liye GitHub ke upar isko choose karoge. Ek shop ke liye jo already GitHub par standardized hai, migrate karne ki rarely koi reason hoti hai; CodeCommit ka main edge un teams ke liye entirely third-party auth/connection dependency avoid karna hai jo sab kuch ek AWS account boundary ke andar chahte hain.

### AWS CodeBuild

**What it is:** fully managed CI service — code compile karta hai, tests run karta hai, build artifacts (JAR/DLL/Docker image/zip) on-demand, isolated build containers mein produce karta hai. Koi Jenkins servers patch/scale nahi karne padte.

**Core concepts**
- **Build Project**: source, environment, build steps, artifact destination ka config.
- **Build Environment**: OS + runtime + compute size + privileged mode (Docker-in-Docker builds ke liye zaroori).
- **buildspec.yml**: build ka script-as-YAML.

```yaml
version: 0.2
phases:
  install:
    commands: [echo Installing dependencies]
  pre_build:
    commands: [echo Pre-build steps]
  build:
    commands: [echo Building application]
  post_build:
    commands: [echo Build completed]
artifacts:
  files: ['**/*']
```

**Phases:** INSTALL (deps/runtime) → PRE_BUILD (ECR login, validation) → BUILD (compile/test) → POST_BUILD (package, push image).

**IAM:** har project ke paas ek service role hota hai jo source read, CloudWatch log write, S3 artifact upload, ECR push grant karta hai. Buildspec mein kabhi AWS credentials embed mat karo.

**VPC builds:** private RDS/APIs tak pohochne ke liye zaroori — subnets, security groups, aur (commonly forgotten) internet access ke liye ek **NAT Gateway** chahiye; NAT bhoolna "standalone mein kaam karta hai, VPC mein fail hota hai" bug ka #1 reason hai.

**Cost model:** sirf build minutes × compute size ka pay karo — koi idle cost nahi. Smallest sufficient compute type, fail fast, aur dependency caching (S3 ya local cache — Maven/npm/NuGet packages) ke through optimize karo.

**Interview-ready summary:** "CodeBuild code compile karta hai, tests run karta hai, aur buildspec files use karke artifacts produce karta hai, isolated on-demand containers mein, automatically scale karte hue aur CodePipeline ke saath integrate karte hue bina build servers manage kiye."

### AWS CodePipeline

**What it is:** fully managed CD **orchestrator** — yeh khud compile, test, ya deploy nahi karta; yeh doosri services (CodeBuild, CodeDeploy, ECS, Lambda, CloudFormation) ke across Source → Build → Test → Deploy stages coordinate karta hai.

**Core concepts:** Pipeline (workflow definition) → Stage (Source/Build/Test/Deploy/Approval, sequentially run hote hain) → Action (ek stage ke andar single task, e.g., "run CodeBuild", "manual approval"; ek stage ke andar multiple actions parallel mein run ho sakte hain).

**Deploy targets:** ECS/Fargate, EC2 via CodeDeploy, Lambda, Elastic Beanstalk, CloudFormation — rolling, blue-green, ya canary (Lambda) strategies ke saath.

**CodeDeploy's Deployment Group:** compute targets ka logical group (EC2 instances/ASG ka ek set, ek ECS service, ya ek Lambda function+alias) jispar ek given CodeDeploy application actually deploy karta hai — pipeline ke Deploy stage ke target ke roop mein configured. Yeh wo jagah bhi hai jahan deployment strategy (in-place vs blue/green, rolling percentages, CloudWatch-alarm-triggered automatic rollback) ek concrete set of targets se bound hoti hai, na ki pipeline par khud ek abstract setting ke roop mein.

**Manual approval stage:** pipeline ko human sign-off pending pause karta hai — production deploys/compliance gates se pehle standard practice.

**Pipeline execution states** — ek single pipeline run ka lifecycle, verbatim memorize karne layak:
| State | Meaning |
|---|---|
| **Started** | Pipeline execution start ho gaya hai |
| **Succeeded** | Saare stages successfully complete ho gaye |
| **Failed** | Ek stage ya action fail ho gaya |
| **Stopped** | Execution manually stop kiya gaya tha |

**IAM roles involved:** **pipeline service role** (jo CodePipeline ko CodeBuild/CodeDeploy invoke karne/S3 artifacts access karne deta hai) individual integrated services dwara use ki jaane wali **action roles** se distinct hai — dono par least privilege.

**CodePipeline + ECS flow:** Source → Build (CodeBuild mein Docker build) → ECR mein push → Deploy stage ECS service ko naya image tag pull karne ke liye update karta hai.

**Cost model:** har active pipeline per month billed hota hai, **execution ke per nahi** — CodePipeline layer par runs ki number free hai (CodeBuild/ECS costs separate hain).

**CodePipeline vs CodeDeploy vs Jenkins**
| | CodePipeline | CodeDeploy | Jenkins |
|---|---|---|---|
| Scope | Poore release workflow ko orchestrate karta hai | Sirf deployment step | Fully custom, self-hosted |
| Management | AWS-managed | AWS-managed | Self-managed |
| Flexibility | Sirf AWS-native integrations | Deployment-specific | Maximum (koi bhi plugin/script) |

**Interview-ready summary:** "CodePipeline stages aur actions ke through doosri AWS services ko orchestrate karke build/test/deploy workflow automate karta hai, reliable, repeatable, auditable releases ensure karte hue — yeh khud kuch build ya deploy nahi karta."

### CodePipeline/CodeBuild Trap Scenarios

| Symptom | Root cause |
|---|---|
| Pipeline execution start hone ke immediately baad fail ho jaata hai (koi stage really run hone se pehle) | Source-stage authentication broken/expired hai — ek GitHub OAuth token ya, aaj zyada commonly, ek expired/revoked **CodeStar Connections** connection GitHub/Bitbucket tak. CodeStar Connections ko pehli baar create hone par console mein ek one-time manual "Update pending connection" handshake chahiye (aur phir se agar connection delete/recreate ho); pipelines silently very first stage par fail hote rehte hain jab tak koi isko re-authorize na kare |
| Pipeline trigger hoti hai lekin CodeBuild start nahi hota | Pipeline service role ke paas CodeBuild project start karne ki permission missing hai |
| CodeBuild manually kaam karta hai lekin CodePipeline ke andar fail hota hai | Different IAM roles — CodePipeline ke invoking role mein wo permissions missing ho sakti hain jo CodeBuild ke apne role mein hoti hain |
| Deployment pipeline success ke baad bhi old code use karta hai | Stale cached artifact ya deploy stage wrong S3 path/version par point kar raha hai |
| "buildspec.yml not found" | File missing hai, misnamed hai, ya configured source root ke relative wrong directory mein hai |
| Docker build CodeBuild mein fail hota hai lekin locally kaam karta hai | Privileged mode enabled nahi hai, ya ECR login step missing hai |
| Pipeline "In Progress" mein stuck hai | Manual approval wait kar raha hai, ya ek long-running build abhi bhi execute ho raha hai |
| ECR mein image push nahi ho pa raha | Service role mein `ecr:PutImage`/`ecr:GetAuthorizationToken` missing hai |
| Build sirf VPC ke andar fail hota hai | Required AWS service access ke liye koi NAT Gateway/VPC endpoint nahi hai |
| Build correct commands ke baad bhi timeout ho jaata hai | Compute type bahut chhota hai, ya build timeout bahut low configured hai |
| Ek single git push par multiple triggers | Webhook trigger aur CodePipeline polling dono simultaneously enabled hain |
| Build success ke baad bhi artifacts missing hain | buildspec.yml mein `artifacts` section ke paths incorrect hain |
| CodeBuild Secrets Manager read nahi kar sakta | Service role mein `secretsmanager:GetSecretValue` missing hai |
| Successful build ke baad ECS old image run karta hai | ECS service update nahi hua, ya image tag static hai (`:latest`) unique tag/digest ke bajaye |
| Dev mein kaam karta hai, prod mein fail hota hai | Cross-account IAM/resource permission misconfiguration |
| Cost suddenly spike ho jaata hai | Frequent triggers, oversized compute type, koi dependency caching nahi |

**Senior-level summary (memorize):** "Zyadatar CodePipeline/CodeBuild failures IAM misconfigurations, incorrect artifact handling, missing Docker privileges, VPC networking gaps, ya role-boundary confusion hote hain — build command errors nahi. AWS mein CI/CD debug karna primarily ek permissions exercise hai."

---

## 2. Infrastructure as Code — CloudFormation vs Terraform

### CloudFormation vs Terraform/CDKTF

Original notes (aur upar ki CI/CD sections) sirf CloudFormation ko passing mein mention karte hain ek possible CodePipeline deploy target ke roop mein — wo isko kabhi actually Terraform/CDKTF se compare nahi karte, jo Lambda, DynamoDB, EC2, aur S3 provision karne ke liye mera actual IaC tool hai. Yeh exactly wahi kism ka "apne real tools ko AWS-native option ke against contrast karo" question hai jo ek senior AWS interview likely poochega, isliye yahan directly aur first person mein isko speak karna worth hai.

**Core comparison**

| | CloudFormation | Terraform | CDKTF |
|---|---|---|---|
| Scope | AWS-only | Multi-cloud (AWS, Azure, GCP, aur sau se zyada doosre providers) | Multi-cloud — hood ke neeche yeh Terraform hai |
| Language | JSON/YAML templates | HCL (HashiCorp Configuration Language) | General-purpose languages (TypeScript, Python, C#, Java, Go) jo Terraform ke underlying JSON config mein synthesize hoti hain |
| State management | AWS dwara managed — khud store/lock karne ke liye koi separate state file nahi | Aap state file khud own karte ho — local (solo/demo use ke liye fine) ya, kisi real team setting mein, ek remote backend (S3 bucket + state locking ke liye DynamoDB table classic pattern hai) | Terraform jaisa hi — CDKTF abhi bhi Terraform state produce karta hai aur uspar rely karta hai; backend configuration unchanged hai, sirf authoring language different hai |
| Cost | Free — sirf jo AWS resources yeh provision karta hai unka pay karo | Free (open-source core); Terraform Cloud/Enterprise paid collaboration features add karta hai | Free — Terraform jaisi hi licensing |
| Drift detection | Native (console/API mein `Detect Drift`) | `terraform plan` ke through (real infra ko state ke against diff karta hai) | Terraform jaisa hi — `cdktf plan` same mechanism ko wrap karta hai |
| Rollback on failure | Failed deployment par stack ka automatic rollback (built-in) | Koi automatic rollback nahi — ek failed `apply` ek partially-applied state chhod sakta hai; aap remediation manage karte ho (apply re-run karo, ya fix karke re-plan karo) | Terraform jaisa hi |
| Vendor lock-in | Total (definition se AWS-only) | None — same tool clouds ke across kaam karta hai, portable skill/tooling investment | None — Terraform jaisi hi portability, plus HCL seekhne ke bajaye aisi language use karne ka added benefit jo aapki team already jaanti hai |
| Ecosystem/community modules | AWS-provided sample templates + serverless ke liye SAM | Bahut large module registry (`registry.terraform.io`), broad community | Growing hai, lekin raw Terraform ke HCL module ecosystem se chhota hai kyunki CDKTF newer hai |

**State-file wala point dwell karne layak hai, kyunki yeh directly DynamoDB ko touch karta hai, jispar mujhe hands-on experience hai:** CloudFormation ka biggest operational advantage yeh hai ki AWS aapke liye state manage karta hai — koi file lose, corrupt, ya team members ke beech fight karne ke liye nahi hai. Terraform (aur isliye CDKTF) us responsibility ko aap par push kar deta hai: classic production-grade setup ek S3 bucket hai state file hold karta hua plus ek DynamoDB table sirf **state locking** ke liye use hota hai (do log/pipelines ko concurrently `apply` run karke state corrupt karne se rokta hai). Yeh ek real operational cost hai jo CloudFormation ke paas nahi hai — lekin yeh exactly wahi kism ki infrastructure bhi hai jise operate karne mein main already comfortable hoon, kyunki yeh same DynamoDB primitives hain (ek simple table, lock item ke liye conditional writes) jo main ek application context mein use karunga.

```hcl
# Terraform backend config — S3 for state, DynamoDB for locking
terraform {
  backend "s3" {
    bucket         = "my-org-terraform-state"
    key            = "prod/app/terraform.tfstate"
    region         = "us-east-1"
    dynamodb_table = "terraform-state-lock"
    encrypt        = true
  }
}
```

**Ek AWS-only project par bhi main CloudFormation ke bajaye Terraform/CDKTF ke liye kyun reach karunga:** multi-cloud portability hamesha deciding factor nahi hoti — module ecosystem, more expressive planning workflow (`terraform plan` ek genuine dry-run diff ke roop mein, sirf ek changeset preview nahi), aur kisi bhi future non-AWS work ke across consistent tooling practical reasons hain. CloudFormation ek perfectly reasonable choice hai un teams ke liye jo forever AWS-only hain aur state backend own karne se avoid karna chahte hain — yeh ek legitimate trade-off hai, wrong answer nahi, aur agar poocha jaaye Terraform ko "the only right choice" defend karne ke liye to main yehi kahunga.

**CDKTF specifically — yeh kya change karta hai aur kya nahi:** CDKTF Terraform ke engine ya state model ko replace nahi karta — yeh *authoring* language ko replace karta hai. HCL likhne ke bajaye, aap TypeScript/Python/C#/Java/Go likhte ho jo CDKTF ke provider bindings ko call karta hai, aur `cdktf synth` usko same JSON mein compile karta hai jo Terraform normally consume karta hai, phir standard Terraform CLI underneath ko hand-off kar deta hai. Ek .NET-background engineer ke liye appeal ek strongly-typed, familiar language (loops, functions, classes, package management) use kar paana hai HCL ki declarative syntax aur uski more limited expression language seekhne ke bajaye.

**Concrete side-by-side — HCL vs CDKTF (TypeScript) mein ek S3 bucket:**

```hcl
# Terraform (HCL)
resource "aws_s3_bucket" "app_data" {
  bucket = "my-app-data-prod"
}

resource "aws_s3_bucket_versioning" "app_data_versioning" {
  bucket = aws_s3_bucket.app_data.id
  versioning_configuration {
    status = "Enabled"
  }
}
```

```typescript
// CDKTF (TypeScript) — equivalent resource
import { S3Bucket } from "@cdktf/provider-aws/lib/s3-bucket";
import { S3BucketVersioningA } from "@cdktf/provider-aws/lib/s3-bucket-versioning";

const appData = new S3Bucket(this, "app_data", {
  bucket: "my-app-data-prod",
});

new S3BucketVersioningA(this, "app_data_versioning", {
  bucket: appData.id,
  versioningConfiguration: {
    status: "Enabled",
  },
});
```

Same declared end-state, same underlying Terraform provider aur state file — sirf real difference authoring ergonomics hai (types, IDE autocomplete, HCL ke `for_each`/`count` ke bajaye repeated resources generate karne ke liye ek loop/function likhne ki ability).

**Interview-ready summary:** "CloudFormation AWS-native aur state-free hai — AWS aapke liye ise manage karta hai, jo operationally genuinely simpler hai, lekin yeh aapko sirf AWS mein lock kar deta hai. Terraform us simplicity ko multi-cloud portability aur ek much larger module ecosystem ke liye trade karta hai, apna khud ka state file own karne ki cost par — typically DynamoDB locking ke saath ek S3 backend, jo infrastructure hai jise operate karne mein main already comfortable hoon. CDKTF underneath Terraform hai; yeh mujhe sirf HCL ke bajaye ek real programming language mein wo infrastructure likhne deta hai, jo mere .NET/C# background ke saath better fit karta hai."

### Terraform/CDKTF in Practice — Depth Questions to Expect

> **Yeh section itna heavily weighted kyun hai:** Terraform/CDKTF mere resume par ek headline skill hai aur Lambda, DynamoDB, EC2, aur S3 ke liye mera actual provisioning tool hai. Interviewers un cheezon par hardest push karte hain jinhe aap *primary* tools ke roop mein claim karte ho, isliye neeche ke questions mere kisi bhi AWS conversation ka likeliest deep-dive area hain — yahan tak ki guide mein kahin aur ki most service breadth se bhi zyada.

**Core workflow, aur do commands jo ek review mein matter karte hain:**
```bash
terraform init         # download providers/modules, configure backend
terraform fmt -check   # formatting gate in CI
terraform validate     # syntax/type check, no AWS calls
terraform plan -out=tf.plan     # the dry-run diff — the artifact a reviewer should read
terraform apply tf.plan         # apply exactly what was reviewed, no re-plan drift
terraform destroy
```
**`plan` ke baare mein point yeh banana hai:** ek **saved plan file** ko apply karna wahi cheez hai jo ek pipeline ko trustworthy banati hai — bina ek ke `terraform apply` apply time par re-plan karta hai, isliye jo run hota hai wo woh nahi ho sakta jo review kiya gaya tha. Yehi difference hai ek real CI/CD gate aur ek rubber stamp ke beech.

**❗ `for_each` vs `count` — highest-value practical Terraform question.** `count` resources ko **position** se index karta hai (`aws_instance.web[0]`, `[1]`, `[2]`). List se middle item remove karo aur har subsequent resource ka index shift ho jaata hai — isliye Terraform un resources ko **destroy aur recreate** karne ka plan banata hai jinhe untouched rehna chahiye tha. `for_each` unko ek **stable string** se key karta hai (`aws_instance.web["api"]`), isliye ek ko remove karna sirf usi ek ko affect karta hai. **`for_each` use karo kisi bhi cheez ke liye jise aap add/remove karoge; `count` ko simple on/off toggle ke liye reserve karo (`count = var.enabled ? 1 : 0`).** Isko wrong karna ek real production incident hai, yehi exactly wajah hai ki yeh poocha jaata hai.

**Environment separation — workspaces vs directories:**
| Approach | Reality |
|---|---|
| **Workspaces** (`terraform workspace new prod`) | Ek codebase, har workspace ke liye ek backend key. Cheap, lekin environments same configuration share karte hain aur wrong environment ke against `apply` run karna easy hai. Dev/test variants ke liye fine |
| **Directory (ya repo) per environment** with a shared module | ✅ Prod ke liye pattern. Separate state, separate backend, separate credentials/roles, aur environments legitimately different ho sakte hain (instance sizes, replica counts). Verbose lekin explicit |

**Modules** reuse unit hain: ek module typed `variables` leta hai, `outputs` produce karta hai, aur **version-pinned** hota hai jab registry ya Git tag se sourced ho (`?ref=v1.4.0`). Kabhi bhi ek unpinned branch se module source mat karo — kisi doosre ka merge aapka production change ban jaata hai. Usual shape internal modules ka ek small set hota hai (`vpc`, `lambda-function`, `dynamodb-table`) jo per environment compose kiya jaata hai.

**❗ Security point jo ek real answer ko alag karta hai: Terraform state secrets ko plaintext mein contain karta hai.** RDS passwords, generated keys, aur `sensitive` marked kuch bhi sab state file mein likhe jaate hain — `sensitive = true` unko sirf **CLI output** se redact karta hai, state se nahi. Isliye: **state bucket ko encrypt karo (SSE-KMS), public access block karo, bucket policy ko pipeline role tak restrict karo, versioning enable karo** (state corruption recovery), aur real secrets ko **Secrets Manager/Parameter Store** mein rakho, unko Terraform ke through pass karne ke bajaye runtime par reference karo. Yeh ek question hai jo mujhe precisely expect karna chahiye kyunki main is tool ko claim karta hoon.

**Existing infrastructure adopt karna aur state fix karna:**
```bash
terraform import aws_s3_bucket.app_data my-existing-bucket   # bring unmanaged resources under management
terraform state list / show / mv / rm                        # refactor or drop state entries
terraform plan -refresh-only                                 # detect drift without proposing changes
```
`terraform state rm` ek resource ko state se remove karta hai **AWS mein delete kiye bina** — escape hatch jab kuch elsewhere managed hona zaroori ho. Import wahi tarika hai jisse aap console-created ("ClickOps") resources ki bahut common reality se deal karte ho.

**Provider aur version discipline:** `required_version` aur provider versions ko pin karo, aur **`.terraform.lock.hcl`** commit karo taaki har machine aur pipeline identical provider builds resolve kare. Unpinned providers ka matlab ek plan hai jo kal alag tha bina kisi wajah ke jo aap dekh sakte ho.

**CI/CD pattern — aur yahan yeh GitHub Actions aur IAM se connect hota hai:**
```
PR opened   → fmt, validate, tflint, tfsec/checkov → terraform plan → post plan as a PR comment
PR merged   → terraform apply <saved plan>          (protected environment, manual approval for prod)
```
Workflow ko **GitHub OIDC ek AWS role assume karte hue** authenticate karo — GitHub secrets mein koi long-lived access keys nahi (trust policy [IAM Roles](03-iam-security.md#iam-roles-policies-assumerole) mein hai). Plan job ko ek **read-only** role do aur apply job ko privileged wala, taaki ek malicious PR kuch bhi apply na kar sake. **tfsec/checkov** se misconfiguration ke liye scan karo (public buckets, unencrypted volumes, `0.0.0.0/0` ingress) isse pehle ki yeh kabhi AWS tak pohoche — [AWS Config](12-security-services.md#aws-config) checks ko left shift karte hue.

**CloudFormation ke equivalents, completeness ke liye** (poocha jaata hai "shop CFN-only hoti to aap yeh kaise karte?" ke roop mein):
- **Nested stacks** — ek parent stack jo child stacks compose karti hai; modules ka CFN analogue, aur per-stack resource limit ke around jaane ka tarika.
- **StackSets** — management account se ek single operation mein **kai accounts aur regions** ke across ek template deploy karo, OU mein naye accounts ko automatic deployment ke saath. Multi-account baseline tool; Terraform equivalent multiple state files ya ek provider-per-account loop hai, jo genuinely much zyada work hai.
- **Change sets** (preview), **drift detection**, **stack policies** (resources ko update se protect karo), **`DeletionPolicy: Retain`/`Snapshot`** (stack delete par data protect karo), aur CFN ke upar serverless-focused transform ke roop mein **SAM**.

**Rapid-fire answers taiyar rakhne ke liye:**
| Question | Answer |
|---|---|
| Kisi ne console mein ek resource change kiya — kya hota hai? | `plan` drift dikhata hai; next `apply` isko code ke according revert kar deta hai. Yehi IaC ki value hai — aur wajah hai ki managed resources mein console changes IAM se blocked hone chahiye |
| Do pipelines ek saath `apply` run karte hain? | **DynamoDB lock table** second ko wait ya fail karwa deti hai — yehi exactly wajah hai jiske liye yeh exist karti hai |
| State file corrupt ya lost ho jaye? | S3 **object versioning** se restore karo; failing that, resources ko wapas `import` karo. Yehi wajah hai ki state bucket par versioning non-optional hai |
| IaC test kaise karte ho? | CI mein `validate` + `plan`, `tflint`, policy-as-code (**tfsec/checkov/OPA**), aur ek sandbox account ke against real provision-assert-destroy tests ke liye **Terratest** |
| CDK (na ki CDKTF) kyun na use karein? | AWS CDK **CloudFormation** synthesise karta hai — AWS-only, AWS-managed state. **CDKTF** **Terraform** synthesise karta hai — multi-cloud, self-managed state. Same authoring ergonomics, underneath different engine |

---

## 3. Resume Follow-Ups

### Resume Follow-Ups — Deployment Dashboard Bullet

> *"Deployment Dashboard UI integrating GitHub Actions with CDKTF/Terraform for AWS infrastructure provisioning… cutting manual deployment intervention by 40% and release cycle from 5 days to 3."*

Expect karo: *pipeline AWS ko kaise authenticate karta hai? bad apply ko kaise prevent karte ho? prod kaun approve karta hai?*

- **Authentication: GitHub OIDC ek IAM role assume karta hai** — GitHub secrets mein koi static access keys nahi, trust policy `sub` ke through ek specific repo **aur** branch/environment tak scoped. Isse lead karo; bullet mein yeh sabse high-value cheez hai, aur JSON [IAM Roles, Policies, AssumeRole](03-iam-security.md#iam-roles-policies-assumerole) mein hai.
- **Safety: PR par plan (read-only role) → merge par apply (privileged role) saved plan file se**, plus manual prod approval ke liye GitHub **environment protection rules**, aur `tfsec`/`checkov` gates. Depth [Terraform/CDKTF in Practice](#terraformcdktf-in-practice--depth-questions-to-expect) mein.
- **State: S3 backend + DynamoDB lock table** — aur *"agar do pipelines same time par apply karein toh?"* ke liye ready raho.
- **40% aur 5→3 days** — 99.9% wala hi rule: denominator aur window pata hona chahiye. Mahine mein kitne deployments, kis period par measure kiya, aur dashboard ne exactly kaunse manual steps hataye. Denominator ke bina percentage decoration lagta hai.

Baaki resume bullets [Resume Deep-Dives](00-resume-aligned-priority-map.md#resume-deep-dives--woh-follow-ups-jo-mujhe-expect-karne-chahiye) mein hain.

**Disaster Recovery — IaC & CI/CD (Terraform state)**

| | |
|---|---|
| **Actually risk par kya hai** | **Terraform state** — ise khona infrastructure khone se bura hai. Saath mein pipeline definitions aur build artifacts |
| **Backup mechanism** | S3 backend **versioning** ke saath (non-negotiable) + optional CRR; **DynamoDB lock table**; pipeline definitions Git mein; artifacts S3/ECR mein versioning ke saath |
| **Realistic RPO / RTO** | State RPO = last apply. RTO minutes, *agar* versioning on thi |

**Recovery runbook:**
1. **Corrupted ya truncated state:** versions list karo (`aws s3api list-object-versions --bucket tfstate --prefix prod/terraform.tfstate`) aur us `--version-id` ko current key par copy karke previous restore karo.
2. **Crashed apply ke baad stale lock:** `terraform force-unlock <LOCK_ID>` — par pehle confirm karo ki koi apply genuinely chal nahi raha.
3. **Restore ke baad hamesha `terraform plan` chalao** apply se pehle, aur use drift report ki tarah padho — wo batata hai ki state galat hone ke dauraan reality ne kya kiya.
4. **Jo resources exist karte hain par state mein nahi hain:** unhe `terraform import` karo, warna next apply unhe dobara banane ki koshish karega.

⚠️ **Gotcha:** **state khone se kuch destroy nahi hota — Terraform ko lagta hai ki kuch exist hi nahi karta**, toh next `apply` un resources ko *create* karne jaata hai jo pehle se hain, aur lucky case mein name conflict milta hai, unlucky case mein duplicate infrastructure. State bucket par S3 versioning poore backend config ki sabse valuable line hai, aur rollback ko maan lene ke bajaye actually test karna chahiye.

---

← [IAM & Security](03-iam-security.md) · [Index](README.md) · [S3](05-s3.md) →
