> **AWS Quick Revision Notes** · [Index](README.md) · Part I

# Infrastructure as Code & CI/CD

---

## 1. AWS Native CI/CD

### CodeCommit

AWS-managed Git, IAM-native (no separate SaaS auth). Interview point = knowing it's IAM-native, not that you'd pick it over GitHub. One of several Source providers (GitHub/Bitbucket/S3 via **CodeStar Connections**).

### CodeBuild

Managed CI in isolated on-demand containers. **Build Project** (config), **Build Environment** (OS+runtime+compute+privileged mode for Docker), **buildspec.yml**.
```yaml
version: 0.2
phases:
  install: { commands: [echo deps] }
  pre_build: { commands: [echo ECR login] }
  build: { commands: [echo compile/test] }
  post_build: { commands: [echo package/push] }
artifacts: { files: ['**/*'] }
```
Phases: INSTALL→PRE_BUILD→BUILD→POST_BUILD. Service role (source read, logs, S3 artifact, ECR push) — never embed keys. **VPC builds need a NAT Gateway** (#1 "works standalone, fails in VPC" bug). Pay per build-minute × compute.

### CodePipeline

Managed CD **orchestrator** (doesn't build/deploy itself). Pipeline→Stage→Action (actions in a stage can run parallel). Deploy targets: ECS/Fargate, EC2 via CodeDeploy, Lambda, Beanstalk, CloudFormation. **CodeDeploy Deployment Group** = concrete set of targets + strategy (in-place/blue-green, rollout %, alarm rollback). States: Started/Succeeded/Failed/Stopped. Pipeline service role vs action roles. **Billed per active pipeline/month, not per run.** vs CodeDeploy (deploy step only) vs Jenkins (self-hosted, max flexibility).

### CodePipeline/CodeBuild Traps

Fail immediately at start = **CodeStar Connection** expired/needs one-time "Update pending connection" handshake · triggers but CodeBuild doesn't start = pipeline role missing perm · works manually not in pipeline = different IAM roles · old code despite success = stale artifact / wrong S3 path · buildspec not found = misnamed/wrong dir · Docker fails = privileged mode off / no ECR login · stuck In Progress = manual approval / long build · can't push ECR = missing `ecr:PutImage`/`GetAuthorizationToken` · fails only in VPC = no NAT/endpoint · ECS runs old image = `:latest` static tag · multiple triggers = webhook + polling both on. **Memorize:** most failures are IAM/artifact/Docker-privileges/VPC/role-boundary — CI/CD debugging in AWS is a permissions exercise.

---

## 2. Infrastructure as Code — CloudFormation vs Terraform

### CloudFormation vs Terraform vs CDKTF

| | CloudFormation | Terraform | CDKTF |
|---|---|---|---|
| Scope | AWS-only | Multi-cloud | Multi-cloud (Terraform underneath) |
| Language | JSON/YAML | HCL | TS/Python/C#/Java/Go → synth to TF JSON |
| State | AWS-managed | **You own it** (S3 + DynamoDB lock) | Same as Terraform |
| Rollback | Automatic | No auto-rollback (partial apply) | Same |
| Lock-in | Total | None | None |
| Modules | SAM/samples | Large registry | Growing, smaller |

State backend:
```hcl
terraform { backend "s3" {
  bucket="my-org-terraform-state"; key="prod/app/terraform.tfstate"
  region="us-east-1"; dynamodb_table="terraform-state-lock"; encrypt=true } }
```
HCL vs CDKTF-TS = same end-state/provider/state, only authoring ergonomics differ (types, IDE, loops vs `for_each`). **Summary:** CFN = AWS-native + state-free (simpler ops, AWS lock-in); Terraform = portability + module ecosystem at cost of owning state (S3+DynamoDB lock — infra I already operate); CDKTF = Terraform underneath in a real language.

### Terraform/CDKTF in Practice (heavily weighted — claimed primary tool)

Workflow: `init → fmt -check → validate → plan -out=tf.plan → apply tf.plan → destroy`. **Apply a saved plan file** = real CI gate (bare `apply` re-plans → what runs ≠ what was reviewed).
**❗ `for_each` vs `count`:** `count` = positional index → removing a middle item shifts indices → destroys+recreates untouched resources. `for_each` = keyed by stable string → removing one affects only it. Use `for_each` for anything add/remove; `count` only for on/off toggle.
Environments: workspaces (cheap, dev/test) vs **directory-per-env** (prod — separate state/backend/creds). Modules = typed variables/outputs, **version-pin** (`?ref=v1.4.0`).
**❗ State contains secrets in plaintext** — `sensitive=true` only redacts CLI output. Encrypt state bucket (SSE-KMS), block public, restrict to pipeline role, enable versioning; keep real secrets in Secrets Manager/Parameter Store.
Adopt/fix: `terraform import`, `state list/show/mv/rm` (`rm` = drop from state, keep in AWS), `plan -refresh-only` (drift). Commit **`.terraform.lock.hcl`**; pin provider versions.
**CI/CD:** PR → fmt/validate/tflint/tfsec/checkov → plan → post as PR comment; merge → apply saved plan (protected env, prod approval). **GitHub OIDC**; **read-only role for plan, privileged for apply**.
CFN equivalents: **Nested stacks** (modules), **StackSets** (multi-account/region), change sets, drift detection, stack policies, `DeletionPolicy: Retain/Snapshot`, SAM.
Rapid-fire: console change → `plan` shows drift, `apply` reverts · two applies at once → **DynamoDB lock** waits/fails · state lost → S3 versioning restore, else `import` · test IaC → validate+plan, tflint, tfsec/checkov/OPA, **Terratest** · CDK vs CDKTF → CDK synths CFN (AWS-only), CDKTF synths Terraform (multi-cloud).

**Deployment Dashboard follow-up:** OIDC auth (scope `sub` to repo+branch) · plan-PR/apply-merge saved plan + env protection + tfsec · S3 backend + DynamoDB lock · know the denominator behind 40% and 5→3.

**IaC DR:** **Terraform state** at risk (losing it is worse than losing infra). Backup = S3 versioning (non-negotiable) + DynamoDB lock + Git + versioned artifacts. Recovery: restore prior state version by `--version-id`; `force-unlock` a stale lock (confirm no apply running); always `plan` post-restore; `import` orphans. **❗ Lost state makes Terraform think nothing exists → next apply tries to CREATE existing resources** → name conflicts / duplicate infra. S3 versioning is the single highest-value line.

---

← [IAM & Security](03-iam-security.md) · [Index](README.md) · [S3](05-s3.md) →
