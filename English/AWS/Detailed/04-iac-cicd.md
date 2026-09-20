> **AWS Detailed Guide** · [Index](README.md) · Part I

# Infrastructure as Code & CI/CD

---

## 1. AWS Native CI/CD

### AWS CodeCommit

**What it is:** AWS's own managed Git repository service — same Git semantics/CLI you already use (clone, push, pull, branches, PRs via "pull requests"), just hosted and access-controlled through IAM instead of a third-party SaaS account. It's one of several valid **Source** stage providers for CodePipeline, alongside GitHub, Bitbucket, and S3 (via CodeStar Connections for the third-party ones — see the trap-scenario table below).

**Why it comes up in interviews even though GitHub dominates in practice:** knowing CodeCommit exists — and that it's IAM-native (repo access controlled by the same policies/roles as everything else in the account, no separate SaaS permission model to reconcile) — is the actual point being tested, not a claim that you'd choose it over GitHub for a real team. For a shop already standardized on GitHub, there's rarely a reason to migrate; CodeCommit's main edge is avoiding a third-party auth/connection dependency entirely for teams that want everything inside one AWS account boundary.

### AWS CodeBuild

**What it is:** fully managed CI service — compiles code, runs tests, produces build artifacts (JAR/DLL/Docker image/zip) in on-demand, isolated build containers. No Jenkins servers to patch/scale.

**Core concepts**
- **Build Project**: config for source, environment, build steps, artifact destination.
- **Build Environment**: OS + runtime + compute size + privileged mode (needed for Docker-in-Docker builds).
- **buildspec.yml**: the build's script-as-YAML.

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

**IAM:** each project has a service role granting source read, CloudWatch log write, S3 artifact upload, ECR push. Never embed AWS credentials in buildspec.

**VPC builds:** needed to reach private RDS/APIs — requires subnets, security groups, and (commonly forgotten) a **NAT Gateway** for internet access; forgetting NAT is the #1 "works standalone, fails in VPC" bug.

**Cost model:** pay for build minutes × compute size only — no idle cost. Optimize via smallest sufficient compute type, failing fast, and dependency caching (S3 or local cache — Maven/npm/NuGet packages).

**Interview-ready summary:** "CodeBuild compiles code, runs tests, and produces artifacts using buildspec files, in isolated on-demand containers, scaling automatically and integrating with CodePipeline without managing build servers."

### AWS CodePipeline

**What it is:** fully managed CD **orchestrator** — it does not compile, test, or deploy itself; it coordinates Source → Build → Test → Deploy stages across other services (CodeBuild, CodeDeploy, ECS, Lambda, CloudFormation).

**Core concepts:** Pipeline (workflow definition) → Stage (Source/Build/Test/Deploy/Approval, run sequentially) → Action (single task within a stage, e.g., "run CodeBuild", "manual approval"; multiple actions within a stage can run in parallel).

**Deploy targets:** ECS/Fargate, EC2 via CodeDeploy, Lambda, Elastic Beanstalk, CloudFormation — with rolling, blue-green, or canary (Lambda) strategies.

**CodeDeploy's Deployment Group:** the logical group of compute targets (a set of EC2 instances/ASG, an ECS service, or a Lambda function+alias) that a given CodeDeploy application actually deploys to — configured as the target of the pipeline's Deploy stage. It's also where the deployment strategy (in-place vs blue/green, rolling percentages, CloudWatch-alarm-triggered automatic rollback) is bound to a concrete set of targets, rather than being an abstract setting on the pipeline itself.

**Manual approval stage:** pauses the pipeline pending human sign-off — standard practice before production deploys/compliance gates.

**Pipeline execution states** — the lifecycle of a single pipeline run, worth having memorized verbatim:
| State | Meaning |
|---|---|
| **Started** | Pipeline execution has started |
| **Succeeded** | All stages completed successfully |
| **Failed** | A stage or action failed |
| **Stopped** | Execution was manually stopped |

**IAM roles involved:** the **pipeline service role** (lets CodePipeline invoke CodeBuild/CodeDeploy/access S3 artifacts) is distinct from **action roles** used by individual integrated services — least privilege on both.

**CodePipeline + ECS flow:** Source → Build (Docker build in CodeBuild) → push to ECR → Deploy stage updates the ECS service to pull the new image tag.

**Cost model:** billed per active pipeline per month, **not** per execution — number of runs is free at the CodePipeline layer (CodeBuild/ECS costs are separate).

**CodePipeline vs CodeDeploy vs Jenkins**
| | CodePipeline | CodeDeploy | Jenkins |
|---|---|---|---|
| Scope | Orchestrates whole release workflow | Only the deployment step | Fully custom, self-hosted |
| Management | AWS-managed | AWS-managed | Self-managed |
| Flexibility | AWS-native integrations only | Deployment-specific | Maximum (any plugin/script) |

**Interview-ready summary:** "CodePipeline automates the build/test/deploy workflow by orchestrating other AWS services through stages and actions, ensuring reliable, repeatable, auditable releases — it doesn't build or deploy anything itself."

### CodePipeline/CodeBuild Trap Scenarios

| Symptom | Root cause |
|---|---|
| Pipeline execution fails immediately after start (before any stage really runs) | Source-stage authentication is broken/expired — a GitHub OAuth token or, more commonly today, an expired/revoked **CodeStar Connections** connection to GitHub/Bitbucket. CodeStar Connections requires a one-time manual "Update pending connection" handshake in the console when first created (and again if the connection is deleted/recreated); pipelines silently fail at the very first stage until someone re-authorizes it |
| Pipeline triggers but CodeBuild doesn't start | Pipeline service role missing permission to start the CodeBuild project |
| CodeBuild works manually but fails inside CodePipeline | Different IAM roles — CodePipeline's invoking role may lack permissions CodeBuild's own role has |
| Deployment uses old code despite pipeline success | Stale cached artifact or deploy stage pointing at wrong S3 path/version |
| "buildspec.yml not found" | File missing, misnamed, or in the wrong directory relative to configured source root |
| Docker build fails in CodeBuild but works locally | Privileged mode not enabled, or missing ECR login step |
| Pipeline stuck "In Progress" | Waiting manual approval, or a long-running build still executing |
| Can't push image to ECR | Service role missing `ecr:PutImage`/`ecr:GetAuthorizationToken` |
| Build fails only inside VPC | No NAT Gateway/VPC endpoint for required AWS service access |
| Build times out despite correct commands | Compute type too small, or build timeout configured too low |
| Multiple triggers per single git push | Both webhook trigger and CodePipeline polling enabled simultaneously |
| Artifacts missing despite build success | Incorrect `artifacts` section paths in buildspec.yml |
| CodeBuild can't read Secrets Manager | Service role missing `secretsmanager:GetSecretValue` |
| ECS runs old image after successful build | ECS service not updated, or image tag is static (`:latest`) instead of a unique tag/digest |
| Works in dev, fails in prod | Cross-account IAM/resource permission misconfiguration |
| Cost spikes suddenly | Frequent triggers, oversized compute type, no dependency caching |

**Senior-level summary (memorize):** "Most CodePipeline/CodeBuild failures are IAM misconfigurations, incorrect artifact handling, missing Docker privileges, VPC networking gaps, or role-boundary confusion — not build command errors. Debugging CI/CD in AWS is primarily a permissions exercise."

---

## 2. Infrastructure as Code — CloudFormation vs Terraform

### CloudFormation vs Terraform/CDKTF

The original notes (and the CI/CD sections above) only mention CloudFormation in passing as one of several possible CodePipeline deploy targets — they never actually compare it to Terraform/CDKTF, which is my actual IaC tool for provisioning Lambda, DynamoDB, EC2, and S3. This is exactly the kind of "contrast your real tools against the AWS-native option" question a senior AWS interview is likely to ask, so it's worth being able to speak to directly and in the first person here.

**Core comparison**

| | CloudFormation | Terraform | CDKTF |
|---|---|---|---|
| Scope | AWS-only | Multi-cloud (AWS, Azure, GCP, and hundreds of other providers) | Multi-cloud — it's Terraform under the hood |
| Language | JSON/YAML templates | HCL (HashiCorp Configuration Language) | General-purpose languages (TypeScript, Python, C#, Java, Go) that synthesize to Terraform's underlying JSON config |
| State management | Managed by AWS — no separate state file to store/lock yourself | You own the state file — local (fine for solo/demo use) or, in any real team setting, a remote backend (S3 bucket + DynamoDB table for state locking is the classic pattern) | Same as Terraform — CDKTF still produces and relies on Terraform state; the backend configuration is unchanged, only the authoring language differs |
| Cost | Free — you pay only for the AWS resources it provisions | Free (open-source core); Terraform Cloud/Enterprise adds paid collaboration features | Free — same licensing as Terraform |
| Drift detection | Native (`Detect Drift` in the console/API) | Via `terraform plan` (diffs real infra against state) | Same as Terraform — `cdktf plan` wraps the same mechanism |
| Rollback on failure | Automatic rollback of the stack on failed deployment (built-in) | No automatic rollback — a failed `apply` can leave a partially-applied state; you manage remediation (re-run apply, or fix and re-plan) | Same as Terraform |
| Vendor lock-in | Total (AWS-only, by definition) | None — same tool works across clouds, portable skill/tooling investment | None — same portability as Terraform, plus the added benefit of using a language your team already knows instead of learning HCL |
| Ecosystem/community modules | AWS-provided sample templates + SAM for serverless | Very large module registry (`registry.terraform.io`), broad community | Growing, but smaller than raw Terraform's HCL module ecosystem since CDKTF is newer |

**The state-file point is worth dwelling on, because it directly touches DynamoDB, which I do have hands-on experience with:** CloudFormation's biggest operational advantage is that AWS manages state for you — there's no file to lose, corrupt, or fight over between team members. Terraform (and therefore CDKTF) pushes that responsibility onto you: the classic production-grade setup is an S3 bucket holding the state file plus a DynamoDB table used purely for **state locking** (preventing two people/pipelines from running `apply` concurrently and corrupting state). That's a real operational cost CloudFormation doesn't have — but it's also exactly the kind of infrastructure I'm already comfortable operating, since it's the same DynamoDB primitives (a simple table, conditional writes for the lock item) I'd use in an application context.

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

**Why I'd still reach for Terraform/CDKTF over CloudFormation even on an AWS-only project:** multi-cloud portability isn't always the deciding factor — the module ecosystem, the more expressive planning workflow (`terraform plan` as a genuine dry-run diff, not just a changeset preview), and consistent tooling across any future non-AWS work are the practical reasons. CloudFormation is a perfectly reasonable choice for a team that is AWS-only forever and wants to avoid owning a state backend — that's a legitimate trade-off, not a wrong answer, and I'd say so if asked to defend Terraform as "the only right choice."

**CDKTF specifically — what it changes and what it doesn't:** CDKTF doesn't replace Terraform's engine or state model — it replaces the *authoring* language. Instead of writing HCL, you write TypeScript/Python/C#/Java/Go that calls CDKTF's provider bindings, and `cdktf synth` compiles that into the same JSON Terraform normally consumes, then hands off to the standard Terraform CLI underneath. The appeal for a .NET-background engineer is being able to use a strongly-typed, familiar language (loops, functions, classes, package management) instead of learning HCL's declarative syntax and its more limited expression language.

**Concrete side-by-side — an S3 bucket in HCL vs CDKTF (TypeScript):**

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

Same declared end-state, same underlying Terraform provider and state file — the only real difference is authoring ergonomics (types, IDE autocomplete, ability to write a loop/function to generate repeated resources instead of HCL's `for_each`/`count`).

**Interview-ready summary:** "CloudFormation is AWS-native and state-free — AWS manages it for you, which is genuinely simpler operationally, but it locks you into AWS only. Terraform trades that simplicity for multi-cloud portability and a much larger module ecosystem, at the cost of owning your own state file — typically an S3 backend with DynamoDB locking, which is infrastructure I'm already comfortable operating. CDKTF is Terraform underneath; it just lets me write that infrastructure in a real programming language instead of HCL, which fits better with my .NET/C# background."

### Terraform/CDKTF in Practice — Depth Questions to Expect

> **Why this section is weighted heavily:** Terraform/CDKTF is a headline skill on my resume and my actual provisioning tool for Lambda, DynamoDB, EC2, and S3. Interviewers push hardest on the things you claim as *primary* tools, so the questions below are the likeliest deep-dive area of any AWS conversation I have — more so than most of the service breadth elsewhere in this guide.

**The core workflow, and the two commands that matter in a review:**
```bash
terraform init         # download providers/modules, configure backend
terraform fmt -check   # formatting gate in CI
terraform validate     # syntax/type check, no AWS calls
terraform plan -out=tf.plan     # the dry-run diff — the artifact a reviewer should read
terraform apply tf.plan         # apply exactly what was reviewed, no re-plan drift
terraform destroy
```
**The point to make about `plan`:** applying a **saved plan file** is what makes a pipeline trustworthy — `terraform apply` without one re-plans at apply time, so what runs may not be what was reviewed. That's the difference between a real CI/CD gate and a rubber stamp.

**❗ `for_each` vs `count` — the highest-value practical Terraform question.** `count` indexes resources by **position** (`aws_instance.web[0]`, `[1]`, `[2]`). Remove the middle item from the list and every subsequent resource shifts index — so Terraform plans to **destroy and recreate** resources that should have been untouched. `for_each` keys them by a **stable string** (`aws_instance.web["api"]`), so removing one affects only that one. **Use `for_each` for anything you'll add to or remove from; reserve `count` for a simple on/off toggle (`count = var.enabled ? 1 : 0`).** Getting this wrong is a real production incident, which is exactly why it's asked.

**Environment separation — workspaces vs directories:**
| Approach | Reality |
|---|---|
| **Workspaces** (`terraform workspace new prod`) | One codebase, one backend key per workspace. Cheap, but environments share the same configuration and it's easy to run `apply` against the wrong one. Fine for dev/test variants |
| **Directory (or repo) per environment** with a shared module | ✅ The pattern for prod. Separate state, separate backend, separate credentials/roles, and environments can legitimately differ (instance sizes, replica counts). Verbose but explicit |

**Modules** are the reuse unit: a module takes typed `variables`, produces `outputs`, and gets **version-pinned** when sourced from a registry or Git tag (`?ref=v1.4.0`). Never source a module from an unpinned branch — someone else's merge becomes your production change. The usual shape is a small set of internal modules (`vpc`, `lambda-function`, `dynamodb-table`) composed per environment.

**❗ The security point that separates a real answer: Terraform state contains secrets in plaintext.** RDS passwords, generated keys, and anything marked `sensitive` are all written to the state file — `sensitive = true` only redacts them from **CLI output**, not from state. So: **encrypt the state bucket (SSE-KMS), block public access, restrict the bucket policy to the pipeline role, enable versioning** (state corruption recovery), and keep real secrets in **Secrets Manager/Parameter Store**, referencing them at runtime rather than passing them through Terraform. This is a question I should expect precisely because I claim the tool.

**Adopting existing infrastructure and fixing state:**
```bash
terraform import aws_s3_bucket.app_data my-existing-bucket   # bring unmanaged resources under management
terraform state list / show / mv / rm                        # refactor or drop state entries
terraform plan -refresh-only                                 # detect drift without proposing changes
```
`terraform state rm` removes a resource from state **without deleting it in AWS** — the escape hatch when something must be managed elsewhere. Import is how you deal with the very common reality of console-created ("ClickOps") resources.

**Provider and version discipline:** pin the `required_version` and provider versions, and commit **`.terraform.lock.hcl`** so every machine and pipeline resolves identical provider builds. Unpinned providers mean a plan that differed yesterday for no reason you can see.

**The CI/CD pattern — and this is where it connects to GitHub Actions and IAM:**
```
PR opened   → fmt, validate, tflint, tfsec/checkov → terraform plan → post plan as a PR comment
PR merged   → terraform apply <saved plan>          (protected environment, manual approval for prod)
```
Authenticate the workflow with **GitHub OIDC assuming an AWS role** — no long-lived access keys in GitHub secrets (the trust policy is in [IAM Roles](03-iam-security.md#iam-roles-policies-assumerole)). Give the plan job a **read-only** role and the apply job the privileged one, so a malicious PR can't apply anything. Scan with **tfsec/checkov** for misconfiguration (public buckets, unencrypted volumes, `0.0.0.0/0` ingress) before it ever reaches AWS — shifting the [AWS Config](12-security-services.md#aws-config) checks left.

**CloudFormation's equivalents, for completeness** (asked as "how would you do this if the shop were CFN-only?"):
- **Nested stacks** — a parent stack composing child stacks; the CFN analogue of modules, and the way around the per-stack resource limit.
- **StackSets** — deploy one template across **many accounts and regions** from the management account in a single operation, with automatic deployment to new accounts in an OU. The multi-account baseline tool; the Terraform equivalent is multiple state files or a provider-per-account loop, which is genuinely more work.
- **Change sets** (preview), **drift detection**, **stack policies** (protect resources from update), **`DeletionPolicy: Retain`/`Snapshot`** (protect data on stack delete), and **SAM** as the serverless-focused transform over CFN.

**Rapid-fire answers to have ready:**
| Question | Answer |
|---|---|
| Someone changed a resource in the console — what happens? | `plan` shows drift; the next `apply` reverts it to code. That reconciliation is the value of IaC — and the reason console changes to managed resources should be blocked by IAM |
| Two pipelines run `apply` at once? | The **DynamoDB lock table** makes the second wait or fail — that's precisely why it exists |
| State file corrupted or lost? | Restore from S3 **object versioning**; failing that, `import` resources back in. This is why versioning on the state bucket is non-optional |
| How do you test IaC? | `validate` + `plan` in CI, `tflint`, policy-as-code (**tfsec/checkov/OPA**), and **Terratest** for real provision-assert-destroy tests against a sandbox account |
| Why not just use CDK (not CDKTF)? | AWS CDK synthesises **CloudFormation** — AWS-only, AWS-managed state. **CDKTF** synthesises **Terraform** — multi-cloud, self-managed state. Same authoring ergonomics, different engine underneath |

---

## 3. Resume Follow-Ups

### Resume Follow-Ups — The Deployment Dashboard Bullet

> *"Deployment Dashboard UI integrating GitHub Actions with CDKTF/Terraform for AWS infrastructure provisioning… cutting manual deployment intervention by 40% and release cycle from 5 days to 3."*

Expect: *how does the pipeline authenticate to AWS? how do you prevent a bad apply? who approves prod?*

- **Authentication: GitHub OIDC assuming an IAM role** — no static access keys in GitHub secrets, trust policy scoped via `sub` to a specific repo **and** branch/environment. Lead with this; it is the highest-value thing in the bullet, and the JSON is in [IAM Roles, Policies, AssumeRole](03-iam-security.md#iam-roles-policies-assumerole).
- **Safety: plan on PR (read-only role) → apply on merge (privileged role) from the saved plan file**, plus GitHub **environment protection rules** for manual prod approval, and `tfsec`/`checkov` gates. Depth in [Terraform/CDKTF in Practice](#terraformcdktf-in-practice--depth-questions-to-expect).
- **State: S3 backend + DynamoDB lock table** — and be ready for *"what if two pipelines apply at the same time?"*
- **The 40% and the 5→3 days** — same rule as the 99.9% claim: know the denominator and the window. How many deployments per month, measured over what period, and exactly which manual steps the dashboard removed. A percentage without a denominator reads as decoration.

The other resume bullets are in [Resume Deep-Dives](00-resume-aligned-priority-map.md#resume-deep-dives--the-follow-ups-i-should-expect).

**Disaster Recovery — IaC & CI/CD (Terraform state)**

| | |
|---|---|
| **What's actually at risk** | **Terraform state** — losing it is worse than losing infrastructure. Also pipeline definitions and build artifacts |
| **Backup mechanism** | S3 backend with **versioning** (non-negotiable) + optional CRR; the **DynamoDB lock table**; pipeline definitions in Git; artifacts in S3/ECR with versioning |
| **Realistic RPO / RTO** | State RPO = last apply. RTO minutes, *if* versioning was on |

**Recovery runbook:**
1. **Corrupted or truncated state:** list versions (`aws s3api list-object-versions --bucket tfstate --prefix prod/terraform.tfstate`) and restore the previous one by copying that `--version-id` over the current key.
2. **Stale lock after a crashed apply:** `terraform force-unlock <LOCK_ID>` — but confirm no apply is genuinely running first.
3. **Always `terraform plan` before applying** post-restore, and read it as a drift report — it tells you what reality did while state was wrong.
4. **Resources that exist but aren't in state:** `terraform import` them back rather than letting the next apply try to recreate them.

⚠️ **The gotcha:** **lost state doesn't destroy anything — it makes Terraform think nothing exists**, so the next `apply` tries to *create* resources that are already there, and you get name conflicts on the lucky path and duplicated infrastructure on the unlucky one. S3 versioning on the state bucket is the single highest-value line in the whole backend config, and it's worth actually testing a rollback rather than assuming it works.

---

← [IAM & Security](03-iam-security.md) · [Index](README.md) · [S3](05-s3.md) →
