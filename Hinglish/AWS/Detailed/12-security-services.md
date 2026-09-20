> **AWS Detailed Guide** · [Index](README.md) · Part II

# Security Services

---

## 1. Overview — Which Service Answers Which Question

### Overview: Kaunsa Service Kaunsa Question Answer Karta Hai

Yahan organised sound karne ka fastest tarika yeh hai ki list recite karne ke bajaye services ko questions se map karo:

| The question | The service |
|---|---|
| Who can do what? | **IAM** (dekho [IAM & Security](03-iam-security.md#iam--security)) |
| Is someone flooding me with traffic? | **Shield** (L3/4) + **WAF** (L7) |
| Is malicious traffic reaching my app? | **WAF**, **Network Firewall** |
| Are my keys managed properly? | **KMS**, **CloudHSM** |
| Are my certificates valid and renewing? | **ACM** |
| Where are my secrets? | **Secrets Manager** / Parameter Store (dekho [Secrets Manager vs Parameter Store](03-iam-security.md#secrets-manager-vs-parameter-store)) |
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

**Teen attack shapes** jo services se pehle naam karne layak hain: **volumetric** (L3/4 — UDP reflection/amplification, SYN floods; goal bandwidth saturate karna hota hai), **protocol** (TCP/IP behaviour exploit karna), aur **application-layer** (L7 — HTTP floods, Slowloris; low bandwidth lekin per request expensive kyunki har ek tumhari application aur database ko hit karta hai).

**AWS Shield** — [ELB Deep-Dive](08-load-balancing-autoscaling.md#elb-deep-dive-cross-zone-load-balancing-504-timeouts--shield-ddos-protection) mein Shield notes bhi dekho.
- **Shield Standard**: free, automatically **har** account ke liye on, Route 53, CloudFront, Global Accelerator, aur ELB ko common L3/4 attacks se protect karta hai.
- **Shield Advanced**: paid (~$3,000/month, org-wide), isme larger-scale mitigation, **24/7 Shield Response Team (SRT) ka access**, attack ke dauran incur hui scaling charges ke liye **cost-protection credits**, health-based detection, aur **WAF bina extra charge ke included** hota hai.

**AWS WAF** — Layer-7 firewall. **CloudFront, ALB, API Gateway, AppSync, aur Cognito user pools** ke sath attach hota hai (note: **NLB pe nahi**, kyunki WAF ko HTTP context chahiye hota hai — ek common trick question).

- Structure: ek **Web ACL** mein **rules** aur **rule groups** hote hain, jo priority order mein evaluate hote hain.
- **AWS Managed Rule Groups** bina rules likhe most needs cover karte hain: **Core rule set (OWASP-style)**, SQL injection, known-bad inputs, **IP reputation**, **Anonymous IP** (Tor/VPN/proxy), aur **Bot Control**.
- Rule types: IP set match, **geo match**, string/regex match, size constraint, SQLi/XSS detection, aur **rate-based rules** — built-in rate limiter (e.g. koi bhi IP jo 5 minutes mein 2,000 requests exceed kare use block karna), jo "credential stuffing ya scraping kaise rokte ho?" ka answer hai.
- Actions: **Allow**, **Block**, **Count**, **CAPTCHA**, **Challenge**.
- **❗ Best practice: har naya rule pehle `Count` mode mein deploy karo**, logs dekho ki yeh kya *block karta* agar Block hota, phir `Block` pe switch karo. Directly Block pe jaana teams ka apna legitimate traffic down karne ka tarika hai — yeh baat kaho aur tum sound karoge jaise tumne actually WAF run kiya hai.
- CloudWatch Logs / S3 / Firehose mein log karo, aur Athena se query karo.
- WAF **regional** hai, except CloudFront ke liye jahan Web ACL **global (created in `us-east-1`)** hota hai.

**AWS Firewall Manager** WAF rules, Shield Advanced protections, security-group policies, aur Network Firewall rules ko centrally **Organization ke har account** ke across apply karta hai — aur automatically newly created resources pe bhi. Yeh "har account mein baseline WAF rules kaise guarantee karte ho?" ka answer hai.

**Canonical layered edge:** `Route 53 → CloudFront (Shield + WAF at the edge) → ALB (Shield) → private app tier`. CloudFront pe attack block karna matlab yeh tumhare ALB, compute, ya database capacity ko kabhi consume nahi karta.

### AWS Network Firewall

Ek **managed, stateful network firewall aur IPS/IDS VPC level pe**, jo **sab** traffic inspect karta hai — sirf HTTP nahi.

- Capabilities: stateful traffic filtering, **egress ke liye domain-name filtering** (`*.microsoft.com` allow karo, baaki sab block karo), protocol detection, aur deep packet inspection ke liye **Suricata-compatible IPS rules**.
- Deployment: har AZ mein ek dedicated **firewall subnet**, jisme route tables IGW/NAT tak pahunchne se pehle traffic ko firewall endpoints ke through direct karte hain.
- **Yeh sahi answer kahan hai:** compliance ke liye controlled **egress filtering** ("workloads sirf approved allowlist of domains tak pahunch sakte hain"), network layer pe intrusion detection, aur woh traffic inspect karna jo WAF nahi dekh sakta kyunki woh HTTP nahi hota.

**Yeh baaki sab se jo traffic filter karte hain kaise differ karta hai:**
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

**KMS (Key Management Service)** — managed encryption keys jo FIPS 140-validated HSMs se backed hain, aur essentially har AWS service (S3, EBS, RDS, Secrets Manager, Lambda env vars…) ke sath integrated hain.

- **Key types:** *AWS owned* (invisible, shared), *AWS managed* (`aws/s3`, `aws/ebs` — free, auto-rotated, lekin policy edit nahi kar sakte), aur **customer managed keys (CMKs)** — woh jo tum khud create karte ho, apni **key policy** ke sath, optional **automatic annual rotation**, tags, aur ek mandatory **7–30 din ka waiting period deletion se pehle** (deliberately, kyunki key delete karna us key se encrypted sab data destroy kar deta hai).
- **❗ Key policy mandatory aur authoritative hai.** Most resources ke unlike, `kms:Decrypt` grant karne wali ek IAM policy **apne aap mein sufficient nahi hai** — key ki apni resource policy ko bhi principal allow karna hi hoga (directly, ya `kms:CallerAccount` pattern se IAM ko delegate karke). "IAM allow bolta hai lekin KMS abhi bhi deny karta hai" ka jawab key policy hai. **Grants** service-to-service delegation ke liye temporary, programmatic alternative hain.
- **Envelope encryption — jaano yeh kyun exist karta hai:** `Encrypt` API sirf **4 KB tak** ka data handle kar sakta hai. Isliye kisi bhi bade data ke liye, `GenerateDataKey` ek plaintext data key plus ek encrypted copy return karta hai; tum apna data locally plaintext key se encrypt karte ho, usko discard karte ho, aur encrypted key ko ciphertext ke sath store karte ho. S3/EBS internally exactly yehi karte hain, aur yehi wajah hai high throughput pe **KMS request quotas** matter karte hain (isliye S3 Bucket Keys — dekho [S3 Encryption](05-s3.md#s3-security-encryption--its-four-types)).
- **Multi-Region keys** key material ko regions ke across replicate karte hain taaki jo region A mein encrypt hua tha use region B mein decrypt kar sako — encrypted data ke cross-region DR ke liye zaruri.
- Har KMS API call **CloudTrail** mein logged hoti hai, jo SSE-S3 ke over SSE-KMS ka audit advantage hai.

**CloudHSM** — tumhare VPC mein **single-tenant, dedicated hardware** HSMs.
| | **KMS** | **CloudHSM** |
|---|---|---|
| Tenancy | Multi-tenant, managed service | **Dedicated hardware, single tenant** |
| Key control | AWS HSM manage karta hai; policy tum control karte ho | **Tum** keys entirely manage karte ho — **AWS ke paas koi access nahi hai aur inko recover nahi kar sakta** |
| FIPS level | 140-2/3 validated | **140-2 Level 3** |
| Integration | Almost har AWS service ke sath native | PKCS#11/JCE/CNG ke through — mostly apni hi application |
| Use when | Default for everything | Exclusive key custody ke liye regulatory mandate, custom crypto (e.g. SQL Server TDE apni keys ke sath), ya ek offloaded CA |

**One-liner:** "KMS jab tak koi regulator specifically require na kare ki AWS possibly bhi meri keys access nahi kar sakta — tab CloudHSM, yeh accept karte hue ki agar main keys lose kar dun, to data gone hai."

#### Worked Example: Fargate Task Ko KMS-Encrypted S3 Data Ka Access Dena

Ek near-perfect interview scenario, kyunki isme **char** alag things true hone chahiye aur candidates usually ek ya do naam karte hain. *"Mera Fargate task bucket read nahi kar sakta, lekin IAM policy clearly `s3:GetObject` allow karti hai."*

**1. TASK role — na ki task execution role.** Execution role image pull karta hai aur secrets inject karta hai; **tumhara application code** **task role** ke under run hota hai. Isko S3 *aur* KMS dono permissions chahiye:
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
Objects likhne ke liye additionally **`kms:GenerateDataKey`** (envelope encryption — upar dekho), plus `s3:PutObject` chahiye. **`kms:ViaService`** condition least-privilege touch hai: role key ko *sirf S3 ke through* use kar sakta hai, kabhi directly nahi.

**2. ❗ KMS KEY POLICY ko bhi us role ko allow karna zaruri hai.** Yeh woh step hai jo miss ho jaata hai, aur yeh specifically KMS ka hai: key ki resource policy **mandatory aur authoritative** hai, isliye ek IAM policy alone *sufficient nahi hai* jab tak key policy IAM ko delegate na kare.
```json
{
  "Sid": "AllowTaskRoleToDecrypt",
  "Effect": "Allow",
  "Principal": { "AWS": "arn:aws:iam::111122223333:role/my-app-task-role" },
  "Action": ["kms:Decrypt", "kms:DescribeKey"],
  "Resource": "*"
}
```
Key policy ke andar `"Resource": "*"` ka matlab hai *yehi key*. (Alternative standard "delegate to IAM" statement hai jo account root ko `kms:*` grant karta hai — jo IAM policies alone ko kaam karne deta hai.)

**3. Bucket policy**, agar koi hai. Same-account, bina kisi restrictive bucket policy ke → kuch nahi chahiye. Lekin ek bucket policy jo unencrypted transport **deny** karti hai ya specific key require karti hai woh tumhe abhi bhi block karegi, aur **cross-account** access ke liye ek explicit `Allow` wahan bhi chahiye hoga.

**4. ❗ Network path — jo koi mention nahi karta.** Ek Fargate task **private subnet** mein *dono* services tak pahunchna chahiye. S3 ke paas ek **free gateway endpoint** hai; **KMS ke paas koi gateway endpoint nahi hai — usko ek interface endpoint** (`com.amazonaws.<region>.kms`) ya NAT gateway chahiye. Isliye perfect IAM wala ek task simply **hang ho jaayega aur timeout ho jaayega** agar tumne S3 endpoint add kiya aur KMS wala bhool gaye.

**Error padho yeh dhoondhne ke liye ki chaar mein se kaunsa galat hai:**
| Error | Cause |
|---|---|
| `AccessDenied` on `GetObject` | Task role mein S3 permission |
| `KMS.AccessDeniedException`, ya *"The ciphertext refers to a customer master key that does not exist… or you are not allowed to access"* | **`kms:Decrypt` missing hai, ya key policy role ko naam nahi karti** — misleading "does not exist" wording ke bawajood |
| Request hang hota hai, phir timeout | **Koi network path nahi** — VPC endpoint ya NAT missing |
| Dev mein kaam karta hai, prod mein fail hota hai | Ek per-environment CMK jiski key policy kabhi update nahi hui |

**Woh nuance jo volunteer karne layak hai:** agar *task definition* Secrets Manager ya Parameter Store se ek secret pull karti hai jo ek **customer-managed** key se encrypted hai, to **task execution role** ko *bhi* `kms:Decrypt` chahiye — kyunki yeh decryption tumhara code start hone se pehle hoti hai. Isliye ek single task legitimately do alag roles pe do alag keys ke liye `kms:Decrypt` chahta ho sakta hai.

**Cross-account variant:** sirf ek **customer-managed** key kaam karti hai (AWS-managed keys share nahi ki ja sakti). Phir tumhe chahiye ki key policy external principal ko naam kare, external principal ki IAM policy `kms:Decrypt` allow kare, aur bucket policy read allow kare — ya tum programmatic, temporary delegation ke liye ek **KMS grant** use karo.

#### Encryption in Transit (TLS) — End to End

Encryption at rest ek checkbox hai; **in transit ek architecture decision hai, kyunki TLS har hop pe terminate hota hai aur har ek ek separate choice hai**:
```
Client --TLS(ACM)--> CloudFront --TLS--> ALB --TLS or plain HTTP?--> ECS task --TLS?--> RDS
                                  ^                    ^                        ^
                          viewer protocol       target group protocol    force_ssl / sslmode
```
- **CloudFront → origin:** *Origin Protocol Policy* (`https-only` ya `match-viewer`). ALB ko `http-only` ka matlab hai ki internet-facing leg encrypted hai aur AWS-internal leg nahi.
- **ALB → target:** **target group protocol** se set hota hai. VPC ke andar HTTP extremely common aur perfectly defensible hai — lekin yeh "end to end encrypted" **nahi** hai, aur tumhe woh kehna chahiye jo tumne implement kiya hai, stronger wala claim mat karo.
- **True end-to-end** ka matlab hai ALB→target hop pe bhi HTTPS, jisko task pe ek certificate chahiye. Useful detail: **ALB target ka certificate validate nahi karta**, isliye wahan ek self-signed cert acceptable hai — tum hop encrypt kar rahe ho, backend authenticate nahi kar rahe.
- **mTLS**: ek ALB **client** certificates require aur verify kar sakta hai (listener pe `mutual authentication`), partner/B2B ya IoT callers ke liye. **App Mesh / ECS Service Connect** services ke *beech* mTLS provide karte hain.
- **TLS version** — listener ki **security policy** ko TLS 1.2 (ya 1.3) minimum pe set karo. Minimum version naam kiye bina "we use TLS" bolna wahi hai jo auditors actually query karte hain.

**Isko hope karne ke bajaye kaise enforce karte ho:**
| Layer | Enforcement |
|---|---|
| S3 | `Deny` with `aws:SecureTransport: false` (dekho [S3 Bucket Policies](05-s3.md#s3-bucket-policies--access-control)) |
| ALB | Ek HTTP:80 listener jiska sirf action **redirect to HTTPS** ho |
| RDS | PostgreSQL `rds.force_ssl=1` / MySQL `require_secure_transport`; client `sslmode=Require` |
| ElastiCache | Cluster creation pe **in-transit encryption** enable karo (baad mein on nahi kar sakte) |
| EFS | `-o tls` ke sath mount karo |
| AWS APIs | Already HTTPS-only (DynamoDB, SQS, KMS…), isliye "is DynamoDB encrypted in transit?" ek yes-by-default hai |

**Certificates** public endpoints ke liye **ACM** se aate hain aur internal ke liye **ACM Private CA** se — yeh yaad rakhte hue ki ek ACM *public* certificate ki private key **export nahi ki ja sakti**, isliye woh directly ek task ya EC2 instance pe install nahi ki ja sakti (dekho [ACM](#acm-aws-certificate-manager) neeche).

### ACM (AWS Certificate Manager)

**Free public TLS certificates automatic renewal ke sath** — renewal hi real value hai, kyunki expired certificates most common self-inflicted outages mein se ek hain.

- **Validation:** **DNS validation** (ek CNAME record add karo; jab tak record wahan rehta hai, tab tak **forever auto-renews** hota hai; hamesha yehi choose karo) ya **email validation** (manual, agar koi link pe click na kare to renewal break kar deta hai).
- **Integrations:** ALB/NLB, **CloudFront**, API Gateway, AppSync, Elastic Beanstalk.
- **❗ Tum ek ACM *public* certificate ki private key export nahi kar sakte.** Isliye tum isko directly ek EC2 instance ya on-prem server pe install nahi kar sakte — TLS ko ek ALB/CloudFront pe terminate karo, ya **ACM Private CA** use karo (jo export allow karta hai, internal certs ke liye, aur paid hai). Yeh limitation ek bahut common question hai.
- **❗ CloudFront ke liye certificates `us-east-1` mein hone chahiye**; ALB ke liye certificates ALB ke region mein hone chahiye. Dekho [CloudFront](14-global-edge-services.md#cloudfront-cdn).
- **Imported** certificates (kisi external CA se) ko **koi automatic renewal nahi** milta — tumhe khud rotate karna padta hai. Expiry ko ACM `DaysToExpiry` metric, ek AWS Config rule, ya ACM expiry events pe ek EventBridge rule se monitor karo.
- **SNI** ek ALB listener ko many certificates/domains serve karne deta hai.

---

## 4. Operations & Governance

### AWS Systems Manager (SSM)

Ek operations-and-management suite jo interviews mein mainly **ek killer feature** ke through aata hai, lekin baaki bhi jaanna zaruri hai.

**❗ Session Manager — "EC2 instance pe shell kaise lete ho?" ka answer.**

Reflex answer hai "public subnet ke ek bastion host ke through SSH." Modern answer hai **Session Manager**, aur difference substantial hai:
- **Koi SSH keys** nahi jinko distribute, rotate, ya leak karna pade.
- **Koi open inbound ports nahi** — port 22 bhi nahi. SSM Agent SSM endpoints ke liye ek **outbound** connection banata hai, isliye instance **poori tarah private subnet mein bina kisi inbound rule ke** reh sakta hai.
- **Koi bastion host nahi** run, patch, aur pay karna.
- **Access IAM se controlled hota hai**, isliye tum ek policy se shell access grant aur revoke karte ho, aur **har session CloudTrail mein logged hoti hai** aur keystroke-by-keystroke S3/CloudWatch Logs mein record ki ja sakti hai — jo audit story ek bastion match nahi kar sakta.
- Windows (PowerShell/RDP via port forwarding) ke sath-sath Linux ke liye bhi kaam karta hai.

**Requirements** (aur isliye usual failure causes): **SSM Agent** installed aur running (Amazon Linux 2/2023 aur recent Windows AMIs pe pre-installed), `AmazonSSMManagedInstanceCore` wala ek **instance profile**, aur SSM endpoints tak network reachability — ya to NAT gateway ke through, ya, ek genuinely isolated subnet ke liye, `ssm`, `ssmmessages`, aur `ec2messages` ke liye **interface VPC endpoints**.

```bash
aws ssm start-session --target i-0abc123
# Port-forward a private RDS/RDP endpoint to localhost — replaces an SSH tunnel through a bastion
aws ssm start-session --target i-0abc123 \
  --document-name AWS-StartPortForwardingSessionToRemoteHost \
  --parameters '{"host":["mydb.abc.us-east-1.rds.amazonaws.com"],"portNumber":["5432"],"localPortNumber":["5432"]}'
```

**Systems Manager ka baaki hissa:**
| Capability | What it does |
|---|---|
| **Parameter Store** | Configuration aur secrets storage — yeh SSM ka *hissa hai* (dekho [Secrets Manager vs Parameter Store](03-iam-security.md#secrets-manager-vs-parameter-store)) |
| **Patch Manager** | **patch baselines** aur **maintenance windows** ke through ek schedule pe OS patches scan aur apply karta hai, EC2 *aur* on-prem servers ke across. Yeh [EC2 shared responsibility model](06-ec2-instance-storage.md#ec2-shared-responsibility-model) ke "guest OS patching tumhari responsibility hai" wale hisse ka concrete answer hai |
| **Run Command** | Bina SSH ke, full audit logging ke sath, tag se ek fleet ke across ek command ya script execute karo — "sab instances pe service restart karo jinme tag `role=web` hai" |
| **State Manager** | Desired configuration ko continuously enforce karta hai (agent installed, service running) aur drift correct karta hai |
| **Automation (runbooks)** | Multi-step operational workflows — patch-and-reboot, AMI creation, ya [AWS Config](#aws-config) ke **auto-remediation actions** invoke karta hai |
| **Inventory / Fleet Manager** | Fleet ke across installed software, patch level, aur configuration collect karta hai; connect kiye bina instances browse aur manage karo |
| **Compliance** | Har instance ke liye patch aur configuration compliance report karta hai |

**Isko kahan volunteer karna hai:** jab bhi koi question EC2 ko scale pe access, patch, ya configure karne ke around hoti hai. "200 instances kaise patch karte ho?" → maintenance windows ke sath Patch Manager, ya freshly baked golden AMI se [ASG instance refresh](08-load-balancing-autoscaling.md#auto-scaling-groups-asg) ke through instances replace karo — immutable infrastructure ek stronger answer hai jahan workload allow karta hai.

### AWS Artifact

Ek **compliance documents ke liye self-service portal** — AWS ke audit reports (**SOC 1/2/3, PCI DSS AOC, ISO 27001/27017/27018, FedRAMP**, aur country-specific attestations) plus woh **agreements** jo tum online accept karte ho (**HIPAA BAA**, GDPR data-processing addendum).

**Avoid karne wala trick:** Artifact kuch bhi scan, monitor, ya secure **nahi** karta. Yeh ek document repository hai. Iska role practice mein **shared responsibility model** hai — jab tumhara auditor evidence maange ki *underlying infrastructure* compliant hai, tum use Artifact se download karte ho; line ke upar sab kuch (tumhari configurations, tumhari access controls) tumhe khud Config, CloudTrail, aur Security Hub se evidence karna padta hai.

### AWS Config

**Har resource ki configuration ko time ke sath record karta hai, aur rules ke against evaluate karta hai.**

- **Configuration items** aur ek full **change history** produce karta hai — isliye tum exactly dekh sakte ho ki last Tuesday ek security group kaisa dikhta tha, aur usko kya change kiya (linking to the **CloudTrail** event aur isliye identity).
- **Rules**: AWS **managed rules** common bar cover karte hain — `s3-bucket-public-read-prohibited`, `encrypted-volumes`, `iam-user-mfa-enabled`, `rds-instance-public-access-check`, `required-tags` — plus Lambda ya CloudFormation Guard mein **custom rules**.
- **Remediation actions** (SSM Automation documents ke through) ek violation ko **auto-fix** kar sakte hain — e.g. jaise hi koi Block Public Access disable kare use waapis re-enable karna. Auto-remediation woh answer hai jo "we detect drift" ko "we prevent drift" se alag karta hai.
- **Conformance packs** rules ko ek compliance framework mein bundle karte hain; **aggregators** Organization ke sab accounts aur regions ke across findings ko roll up karte hain.
- Yeh **regional** hai aur tum record kiye gaye per configuration item plus per rule evaluation pe pay karte ho — yeh scope karne layak hai ki kaunse resource types record karne hain.

---

## 5. Detection & Response

### GuardDuty

**Intelligent threat detection** — continuously **CloudTrail management events, VPC Flow Logs, aur DNS logs** (plus optional S3 data events, EKS audit logs, RDS login activity, Lambda network activity, aur **EBS malware scanning**) ko machine learning aur threat intelligence feeds se analyse karta hai.

- **Agentless aur log-free setup** — yeh un sources ko directly padhta hai, isliye install karne ke liye kuch nahi hai aur **isko enable karne ke liye logs ko khud on (ya pay) karna nahi padta**. Ek click, ya ek delegated administrator account se organisation-wide.
- **Typical findings:** crypto-mining, known-malicious IPs ya domains ke sath communication, **EC2 instance credentials outside AWS se use ho rahe hain** (yaani stolen role credentials — dekho [IAM Roles](03-iam-security.md#iam-roles-policies-assumerole) mein IMDSv2), anomalous locations se unusual API calls ya console logins, port scanning, aur tumhare account ke against reconnaissance.
- **Response automate karo:** findings **EventBridge** mein jaati hain, isliye ek high-severity finding ek Lambda trigger kar sakti hai jo ek instance ka security group isolate kare, ek role ke sessions revoke kare, ya ek ticket open kare — ek console mein baithe rehne ke bajaye jise koi nahi padhta.

### Inspector

**EC2 instances, ECR container images, aur Lambda functions** ke liye **automated vulnerability management**.

- **Continuous aur event-driven**, scheduled nahi: jab tum ek naya image deploy karo, ek instance launch karo, ya jab ek **naya CVE publish** ho, yeh automatically rescan karta hai — isliye ek package jo kal clean tha aaj flag ho jaata hai.
- EC2 ke liye **SSM agent** use karta hai, CVEs ko **network reachability** ke sath correlate karta hai (ek unreachable vulnerability genuinely lower risk hoti hai), aur ek prioritised risk score produce karta hai.
- Yeh **ECR enhanced scanning** ke peeche ka engine hai (dekho [ECR](09-containers-ecs-fargate.md#ecr-elastic-container-registry)).

**❗ Inspector vs GuardDuty woh pairing hai jo poochha jaata hai:** **Inspector weaknesses dhoondhta hai** — unpatched CVEs, vulnerable dependencies ("darwaaze ka lock weak hai"). **GuardDuty active threats dhoondhta hai** — abhi ho raha malicious behaviour ("koi lock pick kar raha hai"). Yeh complementary hain; dono ko us distinction ke sath naam karna complete answer hai.

### Macie

**S3 ke liye ML-based sensitive-data discovery.** Yeh tumhare buckets ka inventory banata hai (kisi bhi public, unencrypted, ya externally shared ko flag karte hue) aur object contents scan karta hai **PII, credentials, financial data, aur health data** classify karne ke liye, apne formats (policy numbers, internal IDs) ke liye regex se custom data identifiers ke sath.

Findings **Security Hub** aur **EventBridge** tak jaati hain. Yeh kahan apna cost earn karta hai: GDPR/HIPAA/PCI data-classification requirements, aur "kya hamare paas customer PII kisi data-lake bucket mein baitha hai jise koi yaad nahi rakhta?" jaisa question answer karna — ek aisa question jo otherwise scale pe unanswerable hai.

### Security Hub & Detective

**Security Hub** **single pane of glass** hai: yeh **GuardDuty, Inspector, Macie, IAM Access Analyzer, Config, Firewall Manager**, aur partner tools se findings ko normalise aur aggregate karta hai ek format mein, phir tumhe **security standards** (AWS Foundational Security Best Practices, **CIS Benchmark**, PCI DSS) ke against score karta hai. Cross-account aur cross-region aggregation, EventBridge ke through automated actions ke sath.

**Detective** ek finding leke ek **interactive behaviour graph** banata hai CloudTrail, VPC Flow Logs, aur GuardDuty data se taaki tum root cause investigate kar sako — us role ne aur kya kiya, us IP ne aur kya touch kiya, behaviour kab start hua. Security Hub tumhe batata hai *kya* galat hai; Detective tumhe samajhne mein help karta hai *yeh kaise hua aur kitna spread hua*.

---

## 6. Summary

### Defence in Depth — Summary Answer

Yeh layered story do jab poocha jaaye "AWS pe ek workload kaise secure karoge?":
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
**One-liner:** "Koi single control hi answer nahi hai — point yeh hai ki ek layer pe failure breach cause karne ke liye sufficient nahi honi chahiye. Aur jo layer main kisi real incident mein sabse pehle check karunga woh IAM hai, kyunki AWS pe most breaches permission ya configuration failures hoti hain, infrastructure failures nahi."

---

← [Networking](11-networking.md) · [Index](README.md) · [Messaging, Streaming & Decoupling](13-messaging-streaming.md) →
