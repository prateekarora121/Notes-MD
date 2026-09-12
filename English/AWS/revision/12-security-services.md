> **AWS Quick Revision Notes** · [Index](README.md) · Part II

# Security Services

---

## 1. Overview — Which Service Answers Which Question

### Overview (question → service)

Who can do what → IAM. Flooding me → Shield (L3/4) + WAF (L7). Malicious traffic → WAF/Network Firewall. Keys → KMS/CloudHSM. Certs → ACM. Secrets → Secrets Manager/Parameter Store. Bad happening now → **GuardDuty**. Weaknesses → **Inspector**. Sensitive data where → **Macie**. Misconfigured/drifting → **Config**. One place → Security Hub. How did it happen → Detective + CloudTrail. Prove AWS compliant → Artifact.

---

## 2. Edge & Network Protection

### DDoS: Shield & WAF

Attack shapes: volumetric (L3/4), protocol, application (L7). **Shield Standard** free/auto (Route53/CloudFront/GA/ELB, L3/4). **Shield Advanced** (~$3k/mo, larger mitigation, **SRT**, cost protection, WAF included). **WAF** = L7, attaches to CloudFront/ALB/API GW/AppSync/Cognito (**not NLB** — needs HTTP). Web ACL → rules/rule groups by priority; **managed rule groups** (Core/OWASP, SQLi, IP reputation, Anonymous IP, Bot Control); **rate-based rules** (credential stuffing); actions Allow/Block/Count/CAPTCHA/Challenge. **❗ Deploy new rules in Count mode first**. Regional except CloudFront (global, us-east-1). **Firewall Manager** = apply WAF/Shield/SG/Network Firewall org-wide + to new resources. Canonical edge: `Route 53 → CloudFront (Shield+WAF) → ALB (Shield) → private tier`.

### Network Firewall

Managed stateful firewall + IPS/IDS at VPC level, all protocols. Domain-name egress filtering, Suricata IPS rules. Firewall subnet per AZ. For compliance egress allowlists / network IDS / non-HTTP inspection. vs SG (L4, allow-only), NACL (L4, stateless), WAF (L7 HTTP), GWLB (third-party insertion).

---

## 3. Encryption & Certificates

### KMS & CloudHSM

KMS = managed keys (FIPS HSMs), integrated everywhere. Key types: AWS-owned / AWS-managed (`aws/s3`, free, can't edit policy) / **customer-managed CMK** (own key policy, optional annual rotation, **7–30 day delete waiting period**). **❗ Key policy mandatory + authoritative** — IAM `kms:Decrypt` alone insufficient unless key policy allows (directly or delegates to IAM). **Envelope encryption** (Encrypt API ≤4 KB → `GenerateDataKey` for larger; why KMS quotas matter → S3 Bucket Keys). Multi-Region keys (cross-region DR). Every call in CloudTrail. **CloudHSM** = single-tenant dedicated hardware in VPC, you manage keys entirely (**AWS can't recover them**), FIPS 140-2 L3, via PKCS#11/JCE/CNG. "KMS unless a regulator requires AWS can't access keys → CloudHSM (lose keys = data gone)."

**Fargate + KMS-encrypted S3 (4 things must be true):** (1) **task role** (not execution) needs S3 + `kms:Decrypt`/`DescribeKey` (+ `GenerateDataKey` for writes); `kms:ViaService` = least-priv. (2) **❗ KMS key policy must also name the role** (mandatory/authoritative). (3) bucket policy (cross-account/deny). (4) **❗ network path** — S3 has free gateway endpoint but **KMS needs an interface endpoint** (or NAT) → else hang/timeout. Errors: `AccessDenied` GetObject → S3 perm; `KMS.AccessDeniedException`/"key does not exist" → kms:Decrypt or key policy; hang → network path; dev-works-prod-fails → per-env CMK key policy. Nuance: secret from Secrets Manager with CMK → **execution role also needs `kms:Decrypt`** (before code starts).

**Encryption in transit (TLS terminates at every hop):** `Client--TLS(ACM)-->CloudFront--TLS-->ALB--TLS or plain?-->ECS--TLS?-->RDS`. CloudFront→origin = Origin Protocol Policy; ALB→target = target group protocol (HTTP inside VPC common but not "end-to-end"); true e2e = HTTPS on ALB→target (ALB doesn't validate target cert → self-signed OK). mTLS (ALB mutual auth / App Mesh between services). Set TLS 1.2/1.3 minimum. Enforce: S3 `aws:SecureTransport:false` Deny, ALB HTTP:80 redirect to HTTPS, RDS `force_ssl`, ElastiCache in-transit at creation, EFS `-o tls`, AWS APIs already HTTPS. Certs from ACM (public) / ACM Private CA (internal — public key can't export).

### ACM

Free public TLS + **auto-renewal** (real value). **DNS validation** (CNAME, auto-renews forever) vs email (breaks renewal). Integrates ALB/NLB/CloudFront/API GW/Beanstalk. **❗ Can't export private key of a public cert** → can't install on EC2 (terminate at ALB/CloudFront, or ACM Private CA). **❗ CloudFront certs must be in us-east-1** (ALB certs in ALB's region). Imported certs = no auto-renewal (monitor `DaysToExpiry`). SNI = many certs one listener.

---

## 4. Operations & Governance

### Systems Manager (SSM)

**❗ Session Manager** = shell on EC2 without SSH keys / open ports / bastion. Agent makes **outbound** connection → instance in fully private subnet. IAM-controlled + CloudTrail-logged + keystroke recording. Requires SSM Agent + instance profile `AmazonSSMManagedInstanceCore` + reachability (NAT or interface endpoints `ssm`/`ssmmessages`/`ec2messages`).
```bash
aws ssm start-session --target i-0abc123
aws ssm start-session --target i-0abc123 --document-name AWS-StartPortForwardingSessionToRemoteHost \
  --parameters '{"host":["mydb...rds.amazonaws.com"],"portNumber":["5432"],"localPortNumber":["5432"]}'
```
Also: **Parameter Store**, **Patch Manager** (concrete answer to "patch 200 instances" / guest-OS-patching), Run Command, State Manager, Automation runbooks (Config auto-remediation), Inventory/Fleet Manager, Compliance. "Patch 200 instances?" → Patch Manager + windows, or replace from golden AMI via instance refresh (immutable = stronger).

### Artifact

Self-service **compliance document portal** (SOC/PCI/ISO/FedRAMP + agreements like HIPAA BAA). Does **not** scan/monitor — it's the shared-responsibility evidence for the *underlying infra*; your side you evidence with Config/CloudTrail/Security Hub.

### AWS Config

Records config over time + evaluates against rules. Configuration items + change history (SG last Tuesday + linking CloudTrail event). Managed rules (`s3-bucket-public-read-prohibited`, `encrypted-volumes`, `iam-user-mfa-enabled`) + custom. **Remediation actions** (SSM Automation auto-fix — re-enable BPA). Conformance packs + aggregators. Regional, pay per item + evaluation.

---

## 5. Detection & Response

### GuardDuty

Intelligent threat detection over CloudTrail/VPC Flow/DNS logs (+ optional S3/EKS/RDS/Lambda/EBS malware) via ML + threat intel. **Agentless + log-free to enable** (reads sources directly). Findings: crypto-mining, malicious IP/domain comms, **EC2 role creds used from outside AWS** (stolen creds), anomalous API/console, port scans. Findings → **EventBridge** → auto-response Lambda.

### Inspector

Vulnerability management for EC2/ECR images/Lambda. **Continuous + event-driven** (rescans on deploy/launch/new CVE). SSM agent for EC2, correlates network reachability, prioritised score. Engine behind ECR enhanced scanning. **❗ Inspector (finds weaknesses — "weak lock") vs GuardDuty (finds active threats — "someone picking the lock")** — complementary.

### Macie

ML sensitive-data discovery for S3 (flags public/unencrypted/shared buckets, classifies PII/credentials/financial/health, custom regex). → Security Hub + EventBridge. For GDPR/HIPAA/PCI + "do we have PII in a forgotten bucket?"

### Security Hub & Detective

**Security Hub** = single pane, normalises GuardDuty/Inspector/Macie/Access Analyzer/Config/Firewall Manager, scores vs standards (Foundational, CIS, PCI), cross-account/region + EventBridge. **Detective** = behaviour graph from CloudTrail/Flow/GuardDuty for root-cause investigation. Security Hub = *what's wrong*; Detective = *how it happened + how far*.

---

## 6. Summary

### Defence in Depth

Edge (Route53 DNSSEC → CloudFront+Shield+WAF) · Network (VPC/private subnets/SG/NACL/Network Firewall/endpoints) · Identity (IAM least-priv, roles not keys, MFA, SCP, boundaries) · Data (KMS/CloudHSM + TLS + Object Lock) · Secrets (Secrets Manager/Parameter Store, IAM DB auth) · Detect (GuardDuty/Inspector/Macie/Config) · Audit (CloudTrail org trail → locked log-archive, Flow Logs, Access Analyzer) · Aggregate (Security Hub → EventBridge → remediation) · Respond (Detective, runbooks, tested restores). "No single control is the answer; check IAM first — most AWS breaches are permission/config failures."

**Security Services DR:** at risk = **KMS keys (the one genuinely unrecoverable loss)**, secrets, certs. Backup = Multi-Region keys, 7–30 day delete window, Secrets Manager cross-region replicas, ACM auto-renewal. Recovery: `cancel-key-deletion` (within window); multi-Region key decrypts same ciphertext in DR; promote secret replica; re-issue certs (DNS validation, CloudFront in us-east-1). **❗ Deleted KMS key = every object encrypted with it permanently unreadable, no escalation** → alarm on `ScheduleKeyDeletion` (paging). Single-region CMK silently blocks cross-region recovery.

---

← [Networking](11-networking.md) · [Index](README.md) · [Messaging, Streaming & Decoupling](13-messaging-streaming.md) →
