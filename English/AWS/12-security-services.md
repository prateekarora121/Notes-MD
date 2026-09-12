> **AWS Detailed Guide** · [Index](README.md) · Part II

# Security Services

---

## 1. Overview — Which Service Answers Which Question

### Overview: Which Service Answers Which Question

The fastest way to sound organised here is to map services to questions rather than reciting a list:

| The question | The service |
|---|---|
| Who can do what? | **IAM** (see [IAM & Security](03-iam-security.md#iam--security)) |
| Is someone flooding me with traffic? | **Shield** (L3/4) + **WAF** (L7) |
| Is malicious traffic reaching my app? | **WAF**, **Network Firewall** |
| Are my keys managed properly? | **KMS**, **CloudHSM** |
| Are my certificates valid and renewing? | **ACM** |
| Where are my secrets? | **Secrets Manager** / Parameter Store (see [Secrets Manager vs Parameter Store](03-iam-security.md#secrets-manager-vs-parameter-store)) |
| **Is something bad happening right now?** | **GuardDuty** (threat detection) |
| **What weaknesses do I have?** | **Inspector** (vulnerability scanning) |
| **Where is my sensitive data?** | **Macie** |
| **Is anything misconfigured or drifting?** | **AWS Config** |
| Can I see everything in one place? | **Security Hub** |
| How did this incident actually happen? | **Detective** + **CloudTrail** |
| Can I prove AWS is compliant to my auditor? | **Artifact** |

---

## 2. Edge & Network Protection

### DDoS Protection: Shield & WAF

**The three attack shapes** worth naming before the services: **volumetric** (L3/4 — UDP reflection/amplification, SYN floods; goal is to saturate bandwidth), **protocol** (exploiting TCP/IP behaviour), and **application-layer** (L7 — HTTP floods, Slowloris; low bandwidth but expensive per request because each one hits your application and database).

**AWS Shield** — see also the Shield notes in [ELB Deep-Dive](08-load-balancing-autoscaling.md#elb-deep-dive-cross-zone-load-balancing-504-timeouts--shield-ddos-protection).
- **Shield Standard**: free, automatically on for **every** account, protecting Route 53, CloudFront, Global Accelerator, and ELB against common L3/4 attacks.
- **Shield Advanced**: paid (~$3,000/month, org-wide), adding larger-scale mitigation, **24/7 access to the Shield Response Team (SRT)**, **cost-protection credits** for scaling charges incurred during an attack, health-based detection, and **WAF included at no extra charge**.

**AWS WAF** — the Layer-7 firewall. Attaches to **CloudFront, ALB, API Gateway, AppSync, and Cognito user pools** (note: **not** to an NLB, because WAF needs HTTP context — a common trick question).

- Structure: a **Web ACL** contains **rules** and **rule groups**, evaluated in priority order.
- **AWS Managed Rule Groups** cover most needs without writing rules: the **Core rule set (OWASP-style)**, SQL injection, known-bad inputs, **IP reputation**, **Anonymous IP** (Tor/VPN/proxy), and **Bot Control**.
- Rule types: IP set match, **geo match**, string/regex match, size constraint, SQLi/XSS detection, and **rate-based rules** — the built-in rate limiter (e.g. block any IP exceeding 2,000 requests per 5 minutes), which is the answer to "how do you stop credential stuffing or scraping?"
- Actions: **Allow**, **Block**, **Count**, **CAPTCHA**, **Challenge**.
- **❗ Best practice: deploy every new rule in `Count` mode first**, watch the logs to see what it *would* have blocked, then switch to `Block`. Going straight to Block is how teams take down their own legitimate traffic — say this and you sound like you've actually run WAF.
- Log to CloudWatch Logs / S3 / Firehose, and query with Athena.
- WAF is **regional**, except for CloudFront where the Web ACL is **global (created in `us-east-1`)**.

**AWS Firewall Manager** centrally applies WAF rules, Shield Advanced protections, security-group policies, and Network Firewall rules across **every account in an Organization** — and automatically to newly created resources. It's the answer to "how do you guarantee every account has the baseline WAF rules?"

**The canonical layered edge:** `Route 53 → CloudFront (Shield + WAF at the edge) → ALB (Shield) → private app tier`. Blocking an attack at CloudFront means it never consumes your ALB, compute, or database capacity.

### AWS Network Firewall

A **managed, stateful network firewall and IPS/IDS at the VPC level**, inspecting **all** traffic — not just HTTP.

- Capabilities: stateful traffic filtering, **domain-name filtering for egress** (allow `*.microsoft.com`, block everything else), protocol detection, and **Suricata-compatible IPS rules** for deep packet inspection.
- Deployment: a dedicated **firewall subnet in each AZ**, with route tables directing traffic through the firewall endpoints before it reaches an IGW/NAT.
- **Where it's the right answer:** controlled **egress filtering** for compliance ("workloads may only reach an approved allowlist of domains"), intrusion detection at the network layer, and inspecting traffic that a WAF can't see because it isn't HTTP.

**How it differs from everything else that filters traffic:**
| | Layer | Scope | Deny rules |
|---|---|---|---|
| **Security Group** | 4 | ENI/instance | ❌ Allow only |
| **NACL** | 4 | Subnet | ✅ but stateless, IP/port only |
| **Network Firewall** | 3–7 | **VPC**, all protocols | ✅ Stateful, domain names, IPS signatures |
| **WAF** | 7 | CloudFront/ALB/API GW | ✅ HTTP content-aware |
| **GWLB** | 3 | VPC | Insertion point for **third-party** appliances |

---

## 3. Encryption & Certificates

### KMS & CloudHSM

**KMS (Key Management Service)** — managed encryption keys backed by FIPS 140-validated HSMs, integrated with essentially every AWS service (S3, EBS, RDS, Secrets Manager, Lambda env vars…).

- **Key types:** *AWS owned* (invisible, shared), *AWS managed* (`aws/s3`, `aws/ebs` — free, auto-rotated, but you can't edit the policy), and **customer managed keys (CMKs)** — the ones you create, with your own **key policy**, optional **automatic annual rotation**, tags, and a mandatory **7–30 day waiting period before deletion** (deliberately, since deleting a key destroys all data encrypted with it).
- **❗ The key policy is mandatory and authoritative.** Unlike most resources, an IAM policy granting `kms:Decrypt` is **not sufficient on its own** — the key's own resource policy must also allow the principal (directly, or by delegating to IAM with the `kms:CallerAccount` pattern). "IAM says allow but KMS still denies" is the key policy. **Grants** are the temporary, programmatic alternative for service-to-service delegation.
- **Envelope encryption — know why it exists:** the `Encrypt` API can only handle **up to 4 KB** of data. So for anything larger, `GenerateDataKey` returns a plaintext data key plus an encrypted copy; you encrypt your data locally with the plaintext key, discard it, and store the encrypted key alongside the ciphertext. That's exactly what S3/EBS do internally, and it's why **KMS request quotas** matter at high throughput (hence S3 Bucket Keys — see [S3 Encryption](05-s3.md#s3-security-encryption--its-four-types)).
- **Multi-Region keys** replicate key material across regions so you can decrypt in region B what was encrypted in region A — needed for cross-region DR of encrypted data.
- Every KMS API call is logged in **CloudTrail**, which is the audit advantage of SSE-KMS over SSE-S3.

**CloudHSM** — **single-tenant, dedicated hardware** HSMs in your VPC.
| | **KMS** | **CloudHSM** |
|---|---|---|
| Tenancy | Multi-tenant, managed service | **Dedicated hardware, single tenant** |
| Key control | AWS manages the HSM; you control policy | **You** manage keys entirely — **AWS has no access and cannot recover them** |
| FIPS level | 140-2/3 validated | **140-2 Level 3** |
| Integration | Native with ~every AWS service | Via PKCS#11/JCE/CNG — mostly your own application |
| Use when | Default for everything | Regulatory mandate for exclusive key custody, custom crypto (e.g. SQL Server TDE with your own keys), or an offloaded CA |

**One-liner:** "KMS unless a regulator specifically requires that AWS cannot possibly access my keys — then CloudHSM, accepting that if I lose the keys, the data is gone."

#### Worked Example: Giving a Fargate Task Access to KMS-Encrypted S3 Data

A near-perfect interview scenario, because it needs **four** separate things to be true and candidates usually name one or two. *"My Fargate task can't read the bucket, but the IAM policy clearly allows `s3:GetObject`."*

**1. The TASK role — not the task execution role.** The execution role pulls the image and injects secrets; **your application code** runs under the **task role**. It needs S3 *and* KMS permissions:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    { "Sid": "ReadObjects", "Effect": "Allow",
      "Action": ["s3:GetObject", "s3:ListBucket"],
      "Resource": ["arn:aws:s3:::my-bucket", "arn:aws:s3:::my-bucket/*"] },

    { "Sid": "DecryptWithTheCmk", "Effect": "Allow",
      "Action": ["kms:Decrypt", "kms:DescribeKey"],
      "Resource": "arn:aws:kms:us-east-1:111122223333:key/1234abcd-...",
      "Condition": { "StringEquals": { "kms:ViaService": "s3.us-east-1.amazonaws.com" } } }
  ]
}
```
Writing objects additionally needs **`kms:GenerateDataKey`** (envelope encryption — see above), plus `s3:PutObject`. The **`kms:ViaService`** condition is the least-privilege touch: the role may use the key *only through S3*, never directly.

**2. ❗ The KMS KEY POLICY must also allow that role.** This is the step that's missed, and it's specific to KMS: the key's resource policy is **mandatory and authoritative**, so an IAM policy alone is *not sufficient* unless the key policy delegates to IAM.
```json
{
  "Sid": "AllowTaskRoleToDecrypt",
  "Effect": "Allow",
  "Principal": { "AWS": "arn:aws:iam::111122223333:role/my-app-task-role" },
  "Action": ["kms:Decrypt", "kms:DescribeKey"],
  "Resource": "*"
}
```
`"Resource": "*"` inside a key policy means *this key*. (The alternative is the standard "delegate to IAM" statement granting the account root `kms:*` — which is what lets IAM policies alone work.)

**3. The bucket policy**, if there is one. Same-account with no restrictive bucket policy → nothing needed. But a bucket policy that **denies** unencrypted transport or requires a specific key will still block you, and **cross-account** access needs an explicit `Allow` there too.

**4. ❗ The network path — the one nobody mentions.** A Fargate task in a **private subnet** must reach *both* services. S3 has a **free gateway endpoint**; **KMS has no gateway endpoint — it needs an interface endpoint** (`com.amazonaws.<region>.kms`) or a NAT gateway. So a task with perfect IAM will simply **hang and time out** if you added the S3 endpoint and forgot the KMS one.

**Read the error to find which of the four is wrong:**
| Error | Cause |
|---|---|
| `AccessDenied` on `GetObject` | The S3 permission in the task role |
| `KMS.AccessDeniedException`, or *"The ciphertext refers to a customer master key that does not exist… or you are not allowed to access"* | **`kms:Decrypt` missing, or the key policy doesn't name the role** — despite the misleading "does not exist" wording |
| Request hangs, then times out | **No network path** — missing VPC endpoint or NAT |
| Works in dev, fails in prod | A per-environment CMK whose key policy was never updated |

**The nuance worth volunteering:** if the *task definition* pulls a secret from Secrets Manager or Parameter Store that's encrypted with a **customer-managed** key, then the **task execution role** *also* needs `kms:Decrypt` — because that decryption happens before your code starts. So a single task can legitimately need `kms:Decrypt` on **two different roles for two different keys**.

**Cross-account variant:** only a **customer-managed** key works (AWS-managed keys cannot be shared). You then need the key policy to name the external principal, the external principal's IAM policy to allow `kms:Decrypt`, and the bucket policy to allow the read — or you use a **KMS grant** for programmatic, temporary delegation.

#### Encryption in Transit (TLS) — End to End

Encryption at rest is a checkbox; **in transit is an architecture decision, because TLS terminates at every hop and each one is a separate choice**:
```
Client --TLS(ACM)--> CloudFront --TLS--> ALB --TLS or plain HTTP?--> ECS task --TLS?--> RDS
                                  ^                    ^                        ^
                          viewer protocol       target group protocol    force_ssl / sslmode
```
- **CloudFront → origin:** the *Origin Protocol Policy* (`https-only` or `match-viewer`). `http-only` to an ALB means the internet-facing leg is encrypted and the AWS-internal leg is not.
- **ALB → target:** set by the **target group protocol**. HTTP inside the VPC is extremely common and perfectly defensible — but it is **not** "encrypted end to end", and you should say which you've implemented rather than claiming the stronger one.
- **True end-to-end** means HTTPS on the ALB→target hop too, which needs a certificate on the task. Useful detail: **the ALB does not validate the target's certificate**, so a self-signed cert is acceptable there — you're encrypting the hop, not authenticating the backend.
- **mTLS**: an ALB can require and verify **client** certificates (`mutual authentication` on the listener), for partner/B2B or IoT callers. **App Mesh / ECS Service Connect** provide mTLS *between services*.
- **TLS version** — set the listener's **security policy** to a TLS 1.2 (or 1.3) minimum. "We use TLS" without naming the minimum version is what auditors actually query.

**How you enforce it rather than hope for it:**
| Layer | Enforcement |
|---|---|
| S3 | `Deny` with `aws:SecureTransport: false` (see [S3 Bucket Policies](05-s3.md#s3-bucket-policies--access-control)) |
| ALB | An HTTP:80 listener whose only action is **redirect to HTTPS** |
| RDS | PostgreSQL `rds.force_ssl=1` / MySQL `require_secure_transport`; client `sslmode=Require` |
| ElastiCache | Enable **in-transit encryption** at cluster creation (it can't be turned on later) |
| EFS | Mount with `-o tls` |
| AWS APIs | Already HTTPS-only (DynamoDB, SQS, KMS…), which is why "is DynamoDB encrypted in transit?" is a yes-by-default |

**Certificates** come from **ACM** for public endpoints and **ACM Private CA** for internal ones — remembering that an ACM *public* certificate's private key **cannot be exported**, so it can't be installed on a task or EC2 instance directly (see [ACM](#acm-aws-certificate-manager) below).

### ACM (AWS Certificate Manager)

**Free public TLS certificates with automatic renewal** — the renewal is the real value, since expired certificates are one of the most common self-inflicted outages.

- **Validation:** **DNS validation** (add a CNAME record; **auto-renews forever** once the record stays in place — always choose this) or **email validation** (manual, breaks renewal if nobody clicks the link).
- **Integrations:** ALB/NLB, **CloudFront**, API Gateway, AppSync, Elastic Beanstalk.
- **❗ You cannot export the private key of an ACM *public* certificate.** So you cannot install one directly on an EC2 instance or an on-prem server — terminate TLS at an ALB/CloudFront instead, or use **ACM Private CA** (which does allow export, for internal certs, and is paid). This limitation is a very common question.
- **❗ Certificates for CloudFront must live in `us-east-1`**; certificates for an ALB must be in the ALB's region. See [CloudFront](14-global-edge-services.md#cloudfront-cdn).
- **Imported** certificates (from an external CA) get **no automatic renewal** — you must rotate them yourself. Monitor expiry with the ACM `DaysToExpiry` metric, an AWS Config rule, or an EventBridge rule on ACM expiry events.
- **SNI** lets one ALB listener serve many certificates/domains.

---

## 4. Operations & Governance

### AWS Systems Manager (SSM)

An operations-and-management suite that shows up in interviews mainly through **one killer feature**, but the rest is worth knowing.

**❗ Session Manager — the answer to "how do you get a shell on an EC2 instance?"**

The reflex answer is "SSH via a bastion host in a public subnet." The modern answer is **Session Manager**, and the difference is substantial:
- **No SSH keys** to distribute, rotate, or leak.
- **No open inbound ports** — not even 22. The SSM Agent makes an **outbound** connection to the SSM endpoints, so the instance can sit in a **fully private subnet with no inbound rules at all**.
- **No bastion host** to run, patch, and pay for.
- **Access is controlled by IAM**, so you grant and revoke shell access with a policy, and **every session is logged to CloudTrail** and can be recorded keystroke-by-keystroke to S3/CloudWatch Logs — which is the audit story a bastion can't match.
- Works for Windows (PowerShell/RDP via port forwarding) as well as Linux.

**Requirements** (and therefore the usual failure causes): the **SSM Agent** installed and running (pre-installed on Amazon Linux 2/2023 and recent Windows AMIs), an **instance profile** with `AmazonSSMManagedInstanceCore`, and network reachability to the SSM endpoints — either via a NAT gateway or, for a genuinely isolated subnet, **interface VPC endpoints for `ssm`, `ssmmessages`, and `ec2messages`**.

```bash
aws ssm start-session --target i-0abc123
# Port-forward a private RDS/RDP endpoint to localhost — replaces an SSH tunnel through a bastion
aws ssm start-session --target i-0abc123 \
  --document-name AWS-StartPortForwardingSessionToRemoteHost \
  --parameters '{"host":["mydb.abc.us-east-1.rds.amazonaws.com"],"portNumber":["5432"],"localPortNumber":["5432"]}'
```

**The rest of Systems Manager:**
| Capability | What it does |
|---|---|
| **Parameter Store** | Configuration and secrets storage — it's *part of* SSM (see [Secrets Manager vs Parameter Store](03-iam-security.md#secrets-manager-vs-parameter-store)) |
| **Patch Manager** | Scans and applies OS patches on a schedule via **patch baselines** and **maintenance windows**, across EC2 *and* on-prem servers. This is the concrete answer to the "guest OS patching is your responsibility" half of the [EC2 shared responsibility model](06-ec2-instance-storage.md#ec2-shared-responsibility-model) |
| **Run Command** | Execute a command or script across a fleet by tag, with no SSH and full audit logging — "restart the service on all instances tagged `role=web`" |
| **State Manager** | Enforces desired configuration continuously (agent installed, service running) and corrects drift |
| **Automation (runbooks)** | Multi-step operational workflows — patch-and-reboot, AMI creation, or the **auto-remediation actions** invoked by [AWS Config](#aws-config) |
| **Inventory / Fleet Manager** | Collects installed software, patch level, and configuration across the fleet; browse and manage instances without connecting |
| **Compliance** | Reports patch and configuration compliance per instance |

**Where to volunteer it:** whenever a question involves accessing, patching, or configuring EC2 at scale. "How do you patch 200 instances?" → Patch Manager with maintenance windows, or replace instances from a freshly baked golden AMI via [ASG instance refresh](08-load-balancing-autoscaling.md#auto-scaling-groups-asg) — immutable infrastructure being the stronger answer where the workload allows it.

### AWS Artifact

A **self-service portal for compliance documents** — AWS's audit reports (**SOC 1/2/3, PCI DSS AOC, ISO 27001/27017/27018, FedRAMP**, and country-specific attestations) plus **agreements** you accept online (the **HIPAA BAA**, the GDPR data-processing addendum).

**The trick to avoid:** Artifact does **not** scan, monitor, or secure anything. It's a document repository. Its role is the **shared responsibility model** in practice — when your auditor asks for evidence that the *underlying infrastructure* is compliant, you download it from Artifact; everything above the line (your configurations, your access controls) you have to evidence yourself with Config, CloudTrail, and Security Hub.

### AWS Config

**Records the configuration of every resource over time, and evaluates it against rules.**

- Produces **configuration items** and a full **change history** — so you can view exactly what a security group looked like last Tuesday, and what changed it (linking to the **CloudTrail** event and therefore the identity).
- **Rules**: AWS **managed rules** cover most of the common bar — `s3-bucket-public-read-prohibited`, `encrypted-volumes`, `iam-user-mfa-enabled`, `rds-instance-public-access-check`, `required-tags` — plus **custom rules** in Lambda or CloudFormation Guard.
- **Remediation actions** (via SSM Automation documents) can **auto-fix** a violation — e.g. re-enable Block Public Access the moment someone disables it. Auto-remediation is the answer that separates "we detect drift" from "we prevent drift".
- **Conformance packs** bundle rules into a compliance framework; **aggregators** roll findings up across all accounts and regions in the Organization.
- It's **regional** and you pay per configuration item recorded plus per rule evaluation — worth scoping which resource types you record.

---

## 5. Detection & Response

### GuardDuty

**Intelligent threat detection** — continuously analyses **CloudTrail management events, VPC Flow Logs, and DNS logs** (plus optional S3 data events, EKS audit logs, RDS login activity, Lambda network activity, and **EBS malware scanning**) using machine learning and threat intelligence feeds.

- **Agentless and log-free to set up** — it reads those sources directly, so there is nothing to install and **enabling it does not require you to turn on (or pay for) the logs themselves**. One click, or organisation-wide from a delegated administrator account.
- **Typical findings:** crypto-mining, communication with known-malicious IPs or domains, **EC2 instance credentials being used from outside AWS** (i.e. stolen role credentials — see IMDSv2 in [IAM Roles](03-iam-security.md#iam-roles-policies-assumerole)), unusual API calls or console logins from anomalous locations, port scanning, and reconnaissance against your account.
- **Automate the response:** findings go to **EventBridge**, so a high-severity finding can trigger a Lambda that isolates an instance's security group, revokes a role's sessions, or opens a ticket — rather than sitting in a console nobody reads.

### Inspector

**Automated vulnerability management** for **EC2 instances, ECR container images, and Lambda functions**.

- **Continuous and event-driven**, not scheduled: it rescans automatically when you deploy a new image, launch an instance, or when a **new CVE is published** — so a package that was clean yesterday gets flagged today.
- Uses the **SSM agent** for EC2, correlates CVEs with **network reachability** (an unreachable vulnerability is genuinely lower risk), and produces a prioritised risk score.
- It's the engine behind **ECR enhanced scanning** (see [ECR](09-containers-ecs-fargate.md#ecr-elastic-container-registry)).

**❗ Inspector vs GuardDuty is the pairing that gets asked:** **Inspector finds weaknesses** — unpatched CVEs, vulnerable dependencies ("the door has a weak lock"). **GuardDuty finds active threats** — malicious behaviour happening now ("someone is picking the lock"). They're complementary; naming both with that distinction is the complete answer.

### Macie

**ML-based sensitive-data discovery for S3.** It inventories your buckets (flagging any that are public, unencrypted, or shared externally) and scans object contents to classify **PII, credentials, financial data, and health data**, with custom data identifiers via regex for your own formats (policy numbers, internal IDs).

Findings flow to **Security Hub** and **EventBridge**. Where it earns its cost: GDPR/HIPAA/PCI data-classification requirements, and answering "do we have customer PII sitting in a data-lake bucket nobody remembered?" — a question that's otherwise unanswerable at scale.

### Security Hub & Detective

**Security Hub** is the **single pane of glass**: it normalises and aggregates findings from **GuardDuty, Inspector, Macie, IAM Access Analyzer, Config, Firewall Manager**, and partner tools into one format, then scores you against **security standards** (AWS Foundational Security Best Practices, **CIS Benchmark**, PCI DSS). Cross-account and cross-region aggregation, with automated actions via EventBridge.

**Detective** takes a finding and builds an **interactive behaviour graph** from CloudTrail, VPC Flow Logs, and GuardDuty data so you can investigate root cause — what else that role did, what else that IP touched, when the behaviour started. Security Hub tells you *what* is wrong; Detective helps you work out *how it happened and how far it spread*.

---

## 6. Summary

### Defence in Depth — The Summary Answer

The layered story to give when asked "how do you secure a workload on AWS?":
```
Edge          Route 53 (DNSSEC) → CloudFront + Shield + WAF
Network       VPC, private subnets, Security Groups, NACLs, Network Firewall, VPC endpoints
Identity      IAM least privilege, roles not keys, MFA, SCPs, permissions boundaries
Data          KMS/CloudHSM encryption at rest, TLS in transit (ACM), S3 Object Lock
Secrets       Secrets Manager / Parameter Store, IAM DB auth — never in code
Detect        GuardDuty (threats) · Inspector (vulnerabilities) · Macie (sensitive data) · Config (drift)
Audit         CloudTrail (org trail → locked log-archive account), VPC Flow Logs, Access Analyzer
Aggregate     Security Hub (one pane) → EventBridge → automated remediation
Respond       Detective for investigation, documented runbooks, tested restores
```
**One-liner:** "No single control is the answer — the point is that a failure at any one layer isn't sufficient to cause a breach. And the layer I'd check first in any real incident is IAM, because on AWS most breaches are permission or configuration failures, not infrastructure failures."

---

← [Networking](11-networking.md) · [Index](README.md) · [Messaging, Streaming & Decoupling](13-messaging-streaming.md) →
