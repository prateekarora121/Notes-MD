> **AWS Quick Revision Notes** · [Index](README.md) · Part II

# Security Services

---

## 1. Overview — Which Service Answers Which Question

### Overview (question → service)

Who can do what → IAM; traffic flood → Shield+WAF; malicious traffic → WAF/Network Firewall; keys → KMS/CloudHSM; certs → ACM; secrets → Secrets Manager/Parameter Store; happening now → **GuardDuty**; weaknesses → **Inspector**; sensitive data → **Macie**; misconfig/drift → **Config**; one place → **Security Hub**; how it happened → **Detective**+CloudTrail; prove AWS compliant → **Artifact**.

---

## 2. Edge & Network Protection

### Shield & WAF

- Attack shapes: volumetric (L3/4), protocol, application (L7). 
- **Shield Standard** (free, auto, R53/CloudFront/GA/ELB) vs **Advanced** (~$3k/mo, SRT 24/7, cost protection, WAF included).
- **WAF** = L7 firewall on **CloudFront/ALB/API GW/AppSync/Cognito** (❗ **not NLB**). Web ACL → rules/rule groups (priority). Managed Rule Groups (Core/OWASP, SQLi, IP reputation, Anonymous IP, Bot Control). **Rate-based rules** (credential stuffing). Actions: Allow/Block/Count/CAPTCHA/Challenge. ❗ **Deploy in `Count` mode first**. Regional except CloudFront (global, us-east-1).
- **Firewall Manager:** central WAF/Shield/SG/Network Firewall across org.
- Canonical edge: `R53 → CloudFront (Shield+WAF) → ALB (Shield) → app`.

### Network Firewall

- Managed stateful firewall + IPS/IDS at VPC (all traffic, not just HTTP). Domain-name egress filtering, Suricata IPS rules. Dedicated firewall subnet per AZ. For controlled egress filtering, intrusion detection, non-HTTP inspection.

| | Layer | Scope | Deny |
|---|---|---|---|
| SG | 4 | ENI | allow only |
| NACL | 4 | subnet | ✅ stateless IP/port |
| Network Firewall | 3-7 | VPC | ✅ stateful, domains, IPS |
| WAF | 7 | CloudFront/ALB/API GW | ✅ HTTP-aware |
| GWLB | 3 | VPC | 3rd-party appliances |

---

## 3. Encryption & Certificates

### KMS & CloudHSM

- **KMS:** managed keys (FIPS HSM-backed). Types: AWS-owned, AWS-managed (`aws/s3`, free, auto-rotate, no policy edit), **CMK** (own, key policy, optional annual rotation, 7-30 day deletion window). ❗ **Key policy mandatory + authoritative** — IAM alone insufficient ("IAM allows, KMS denies" = key policy). **Envelope encryption** (Encrypt API ≤4 KB → `GenerateDataKey` for large data, S3/EBS internally) → KMS request quotas matter (S3 Bucket Keys). Multi-Region keys (cross-region decrypt). Every call in CloudTrail.
- **CloudHSM:** single-tenant dedicated hardware, you manage keys entirely (AWS no access/recovery), FIPS 140-2 L3. Only for regulatory sole-custody / custom crypto.

**Worked example (Fargate → KMS-encrypted S3):** 4 things must be true: (1) **task role** (not execution) needs S3 **+ `kms:Decrypt`** (`kms:ViaService: s3.<region>.amazonaws.com`); (2) ❗ **KMS key policy** must also allow the role; (3) bucket policy (cross-account/deny-non-HTTPS); (4) ❗ **network path** — S3 free gateway endpoint, **KMS needs interface endpoint** or NAT (else hangs/timeout). Errors map to which is wrong. Execution role also needs `kms:Decrypt` if task def pulls CMK-encrypted secret.

**Encryption in transit (TLS terminates each hop):** CloudFront→origin (origin protocol policy), ALB→target (target group protocol — VPC HTTP defensible but not "end-to-end"), true E2E = HTTPS to target (ALB doesn't validate target cert → self-signed ok), mTLS (ALB client certs / App Mesh service-to-service), TLS version (security policy 1.2+). Enforce: S3 (`aws:SecureTransport:false` deny), ALB (HTTP:80→redirect HTTPS), RDS (`force_ssl`), ElastiCache (creation-time), EFS (`-o tls`), AWS APIs (HTTPS-only). Certs: ACM public / ACM Private CA.

### ACM

- Free public TLS + **auto-renewal** (the value). **DNS validation** (CNAME, forever auto-renew — always choose) vs email. Integrations: ALB/NLB/CloudFront/API GW/Beanstalk. ❗ **Public cert private key can't be exported** (can't install on EC2/on-prem → terminate at ALB/CloudFront, or ACM Private CA). ❗ **CloudFront cert must be us-east-1**; ALB cert in ALB's region. Imported certs = no auto-renewal. SNI.

---

## 4. Operations & Governance

### Systems Manager (SSM)

- ❗ **Session Manager** = "shell into EC2": no SSH keys, **no open inbound ports** (agent outbound to SSM), no bastion, IAM-controlled, CloudTrail-logged. Needs SSM Agent + instance profile (`AmazonSSMManagedInstanceCore`) + reachability (NAT or `ssm`/`ssmmessages`/`ec2messages` endpoints).
```bash
aws ssm start-session --target i-0abc123
aws ssm start-session --target i-0abc123 --document-name AWS-StartPortForwardingSessionToRemoteHost ...
```
- Also: Parameter Store, **Patch Manager** (guest-OS patching answer), Run Command, State Manager, Automation runbooks, Inventory/Fleet Manager, Compliance. "Patch 200 instances?" → Patch Manager or ASG instance refresh from golden AMI.

### Artifact

Self-service compliance docs portal (SOC/PCI/ISO/FedRAMP + HIPAA BAA/GDPR DPA). ❗ Doesn't scan/monitor — document repository for shared-responsibility evidence.

---

## 5. Detection & Response

### GuardDuty / Inspector / Macie

- **GuardDuty:** intelligent threat detection (CloudTrail/Flow Logs/DNS + optional S3/EKS/RDS/Lambda/EBS malware). **Agentless, log-free setup.** Findings: crypto-mining, malicious IPs, **EC2 creds used outside AWS**, anomalous API/logins. → EventBridge automate (isolate SG, revoke sessions).
- **Inspector:** vulnerability mgmt (EC2/ECR/Lambda). Continuous/event-driven (new CVE rescan). Engine behind ECR enhanced scanning.
- ❗ **Inspector (weaknesses/CVEs "lock is weak") vs GuardDuty (active threats "someone picking lock").**
- **Macie:** ML sensitive-data discovery in S3 (PII/creds/financial/health), custom identifiers. → Security Hub/EventBridge. GDPR/HIPAA/PCI.

### Config / Security Hub / Detective

- **Config:** records resource config over time + rules (`s3-bucket-public-read-prohibited`, `encrypted-volumes`, `iam-user-mfa-enabled`). Change history + CloudTrail link. **Remediation actions** (SSM Automation — "prevent drift"). Conformance packs, aggregators. Regional, pay per item + evaluation.
- **Security Hub:** single pane, aggregates GuardDuty/Inspector/Macie/Access Analyzer/Config/Firewall Manager, scores vs standards (FSBP/CIS/PCI). → EventBridge.
- **Detective:** behaviour graph from CloudTrail/Flow Logs/GuardDuty for root-cause investigation.

---

## 6. Summary

### Defence in Depth

Edge (R53 DNSSEC → CloudFront+Shield+WAF) → Network (VPC/subnets/SG/NACL/Network Firewall/endpoints) → Identity (IAM least-priv, roles, MFA, SCP, boundaries) → Data (KMS/CloudHSM, TLS/ACM, Object Lock) → Secrets (Secrets Manager/Parameter Store, IAM DB auth) → Detect (GuardDuty/Inspector/Macie/Config) → Audit (CloudTrail org trail, Flow Logs, Access Analyzer) → Aggregate (Security Hub → EventBridge remediation) → Respond (Detective, runbooks). "No single control; check IAM first (most breaches = permission/config)."

**DR — Security Services:** risk = **KMS keys (only unrecoverable loss)** + secrets/certs. Backup = Multi-Region keys, 7-30 day deletion window, Secrets replicas, ACM auto-renew. Accidental delete → `cancel-key-deletion` (within window); regional → multi-Region key replica decrypts same ciphertext; secrets → promote replica; certs → reissue DNS-validated (CloudFront us-east-1). ⚠️ Deleted KMS key = permanent data loss (alarm on `ScheduleKeyDeletion`); single-region CMK blocks cross-region recovery.

---

← [Networking](11-networking.md) · [Index](README.md) · [Messaging, Streaming & Decoupling](13-messaging-streaming.md) →
