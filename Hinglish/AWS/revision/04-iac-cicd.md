> **AWS Quick Revision Notes** · [Index](README.md) · Part I

# Infrastructure as Code & CI/CD

---

## 1. AWS Native CI/CD

### CodeCommit

Managed Git, **IAM-native** access. Interview point = it exists + IAM-native. Practice: GitHub dominates.

### CodeBuild

Managed CI. Build Project + Build Environment + `buildspec.yml`.
```yaml
version: 0.2
phases:
  install: {commands: [echo deps]}
  pre_build: {commands: [ECR login]}
  build: {commands: [compile/test]}
  post_build: {commands: [package/push]}
artifacts: {files: ['**/*']}
```
- Phases: INSTALL→PRE_BUILD→BUILD→POST_BUILD. Service role per project. **VPC builds need NAT Gateway** (#1 "works standalone, fails in VPC"). Pay build-minutes only; cache deps (NuGet).

### CodePipeline

Managed CD **orchestrator** (doesn't build/deploy). Pipeline→Stage→Action. **CodeDeploy Deployment Group** = compute targets + strategy (in-place/blue-green/canary, alarm rollback). Manual approval stage. States: Started/Succeeded/Failed/Stopped. Pipeline service role + action roles. Billed per active pipeline/month (executions free).

**CodePipeline vs CodeDeploy vs Jenkins:** orchestrate whole / deploy only / fully custom self-hosted.

**Trap scenarios:** immediate fail = CodeStar Connections needs one-time handshake; no CodeBuild = missing pipeline role permission; manual-works-pipeline-fails = different roles; old code = stale artifact/`:latest`; buildspec not found; Docker fails = no privileged mode / ECR login; stuck = approval/long build; ECR push = missing `ecr:PutImage`/`GetAuthorizationToken`; VPC fail = no NAT; multiple triggers = webhook + polling both. **Most failures = IAM, not build commands.**

---

## 2. Infrastructure as Code — CloudFormation vs Terraform

### CloudFormation vs Terraform vs CDKTF

| | CloudFormation | Terraform | CDKTF |
|---|---|---|---|
| Scope | AWS-only | Multi-cloud | Multi-cloud (Terraform underneath) |
| Language | JSON/YAML | HCL | TS/Python/C#/Java/Go → synth |
| State | AWS-managed | **You own** (S3 + DynamoDB lock) | Same as Terraform |
| Rollback | Auto on fail | None (partial state) | None |
| Lock-in | Total | None | None |

```hcl
terraform {
  backend "s3" {
    bucket = "my-org-terraform-state"; key = "prod/app/terraform.tfstate"
    region = "us-east-1"; dynamodb_table = "terraform-state-lock"; encrypt = true } }
```
- State-file point = biggest CFN advantage (AWS manages); Terraform pushes to you (S3 + DynamoDB lock via conditional writes — same primitives as app DynamoDB).
- **CDKTF** replaces *authoring* language (HCL → real programming language), same engine/state. `cdktf synth` → JSON → Terraform CLI.
- CFN reasonable for forever-AWS-only teams (legitimate trade-off).

### Terraform/CDKTF in Practice

```bash
terraform init; terraform fmt -check; terraform validate
terraform plan -out=tf.plan          # the reviewable artifact
terraform apply tf.plan              # apply exactly what was reviewed
```
- **Saved plan file** = what makes a pipeline trustworthy (no re-plan drift).
- ❗ **`for_each` vs `count`:** `count` indexes by position (remove middle → destroys/recreates subsequent); `for_each` keys by **stable string**. Use `for_each` for add/remove, `count` for on/off toggle.
- **Env separation:** workspaces (cheap, risky) vs **directory-per-env + shared module** (prod pattern).
- **Modules:** typed vars/outputs, **version-pinned** (`?ref=v1.4.0`).
- ❗ **State contains secrets in plaintext** (`sensitive=true` only redacts CLI). → encrypt state bucket (SSE-KMS), block public, restrict to pipeline role, versioning; keep secrets in Secrets Manager/Parameter Store.
- **Adopt existing:** `terraform import`, `state list/show/mv/rm`, `plan -refresh-only` (drift). `state rm` removes from state without deleting AWS.
- Pin `required_version`, providers; commit `.terraform.lock.hcl`.
- **CI/CD:** PR → fmt/validate/tflint/tfsec/checkov → plan → PR comment; merge → apply. **GitHub OIDC** assumes role; plan job = read-only role, apply job = privileged.
- **CFN equivalents:** nested stacks (modules), **StackSets** (multi-account/region), change sets, drift detection, stack policies, `DeletionPolicy: Retain/Snapshot`, SAM.
- **Rapid-fire:** console change → `plan` shows drift, `apply` reverts; two applies → DynamoDB lock; state lost/corrupt → S3 versioning restore / `import`; test IaC → validate+plan+tflint+tfsec/checkov+Terratest; CDK vs CDKTF → CDK synth CloudFormation (AWS-only), CDKTF synth Terraform (multi-cloud).

**DR — IaC/CI/CD:** risk = **Terraform state** (worse to lose than infra). Backup = S3 **versioning** (non-negotiable) + DynamoDB lock; pipeline defs in Git. Corrupt state → restore version-id; stale lock → `force-unlock`; always `plan` after restore; missing resources → `import`. ⚠️ Losing state = Terraform thinks nothing exists → next apply creates duplicates/name conflicts.

---

← [IAM & Security](03-iam-security.md) · [Index](README.md) · [S3](05-s3.md) →
