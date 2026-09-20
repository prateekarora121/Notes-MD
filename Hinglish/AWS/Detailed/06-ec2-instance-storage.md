> **AWS Detailed Guide** · [Index](README.md) · Part I

# EC2 & Instance Storage

> **Tier 1 — bulletproof.** Ek named resume skill, CDKTF/Terraform ke through provisioned. EBS/EFS/AMI material yahan hai kyunki yeh practice mein EC2 run karne se inseparable hai.

---

## 1. EC2 Fundamentals

### EC2 Fundamentals

**Core concepts**
- **Instance** — running VM. **AMI** — template (OS + software) jo ise launch karne ke liye use hota hai.
- **Instance families**: `t` (burstable, e.g. t3/t4g), `m` (balanced), `c` (compute-optimized), `r` (memory-optimized), plus GPU (`g`/`p`) aur storage-optimized (`i`/`d`) families.
- **Storage**: EBS (persistent, network-attached) vs Instance Store (ephemeral, physically attached, stop/terminate par lost).
- **Networking**: ENI, Security Groups (stateful), subnets.

**Lifecycle:** Launch → Running → Stop/Start → Terminate.
- **Stop**: EBS-backed data persists; instance ID kept; aap compute ka pay karna band kar dete ho (lekin EBS ka abhi bhi pay karte ho).
- **Terminate**: instance aur (default se) root EBS volume destroyed.

**Pricing models**
| Model | Commitment | Relative cost | Risk |
|---|---|---|---|
| On-Demand | None | Highest | None |
| Reserved Instances | 1–3 yrs | Lower | Locked in |
| Savings Plans | 1–3 yrs $/hr commitment | Lower, RI se more flexible | Locked in $ amount |
| Spot | None | Cheapest | 2-min warning ke saath reclaim ho sakta hai |

**EC2 kab choose karein:** full OS control, custom kernel modules, legacy apps, stateful workloads, GPU/specialized hardware, steady utilization wali long-running services.

**Limitations:** aap patching/scaling own karte ho; idle instances abhi bhi cost karti hain; serverless/Fargate se operationally more moving parts.

### EC2 Instance Types, User Data & Metadata

**Ek instance type name decode karna** — `m5dn.2xlarge`:
```
m      5       dn        .2xlarge
│      │       │          └─ size (vCPU/memory scale)
│      │       └─ extra attributes
│      └─ generation (higher = newer, usually better price/performance)
└─ family (workload class)
```
| Letter | Meaning |
|---|---|
| `t` | Burstable — baseline CPU + credits (neeche CPU-credit trap dekho) |
| `m` | General purpose, ~4 GiB RAM per vCPU |
| `c` | Compute optimised, ~2 GiB per vCPU |
| `r` / `x` / `z` | Memory optimised, ~8 GiB+ per vCPU |
| `i` / `d` | Storage optimised — local NVMe/HDD instance store |
| `g` / `p` / `inf` / `trn` | GPU / ML accelerators |
| **attribute `g`** | **AWS Graviton (ARM64)** — typically ~20% cheaper aur better price/performance |
| attribute `a` | AMD processors (Intel equivalents se cheaper) |
| attribute `i` | Intel |
| attribute `n` | Network optimised (higher bandwidth) |
| attribute `d` | Local NVMe instance-store disks attached |
| attribute `b` | Block-storage optimised (higher EBS throughput) |

Sizes linearly scale karte hain: `large` = 2 vCPU, `xlarge` = 4, `2xlarge` = 8, aur memory unke saath double hoti hai. Isliye `m6g.2xlarge` "general purpose, 6th gen, Graviton, 8 vCPU / 32 GiB" ke roop mein padha jaata hai.

**Graviton point .NET ke liye unprompted banane layak hai:** .NET ne .NET 6 se ARM64 support kiya hai, isliye ek `t4g`/`m7g` instance usually ek modern .NET web/API workload ke liye ek straight ~20% saving hai ek recompile ke saath aur bina code changes ke. Isko naam lena ek generic "instance type pick karo" answer ko ek cost-optimisation answer banata hai.

#### User Data — Quick Recall

> **Ek line mein:** ek startup script jo aap launch par supply karte ho, jise instance **ek baar, first boot par, root ke roop mein** run karta hai (Linux par cloud-init / Windows par EC2Launch ke through).
>
> - Limit **16 KB** (API ko pass karte waqt base64-encoded). Larger bootstraps ko iske bajaye S3 se ek script download karna chahiye.
> - Yeh subsequent reboots par re-run **nahi** hota jab tak aap explicitly configure na karo (`cloud-init-per`, ya ek `#cloud-config` directive).
> - Isko `/var/log/cloud-init-output.log` par debug karo — "mera instance aa gaya lekin kuch bhi install nahi hua" ka pehla check karne ki jagah.
> - Senior nuance: scale par heavy user-data bootstrapping slow aur fragile hai. Dependencies ko ek **custom AMI** mein bake karo (dekho [AMIs](#amis-amazon-machine-images)) ya ek container image mein, aur user data ko sirf config tak rakho. **"Golden AMI + thin user data"** wahi answer hai jo interviewers sunna chahte hain.

```bash
#!/bin/bash
yum update -y
yum install -y amazon-cloudwatch-agent
systemctl enable --now amazon-cloudwatch-agent
```

#### User Data — The Full Explanation

**Yeh kis problem ko solve karta hai.** Ek newly launched EC2 instance ek **bare operating system** hai — kuch bhi installed nahi, kuch bhi configured nahi. Kuch to setup commands run karna hoga. Aap SSH mein ja sakte ho aur unko type kar sakte ho, jo ek instance ke liye fine hai aur twenty instances ke liye useless hai jo ek Auto Scaling Group dwara 3 baje raat ko automatically launch hue hain. User data wahi script hai jo machine **apne upar** boot karte waqt run karti hai, isliye kisi ko login nahi karna padta. Agar ek ASG load ke under scale out karta hai, yehi *sirf* mechanism hai jisse wo naye instances khud ko configure karte hain.

**Yeh kahan se aata hai.** Console mein yeh literally ek text box hai — launch screen par *Advanced details → User data*. Ek launch template ya Terraform mein yeh `user_data` field hai. Aap shell commands daalte ho; bas yehi hai. Naam AWS ka hai, matlab hai "data jo *user* instance ko supply karta hai"; **"startup script" ek honest description hai.**

**Yeh kaun run karta hai.** Zyadatar Linux AMIs **cloud-init** pre-installed ke saath aate hain. Boot ke during yeh aapka script fetch karta hai aur ise **`root`** ke roop mein execute karta hai — full administrator, isliye koi `sudo` nahi chahiye. Aap khud cloud-init install ya invoke nahi karte; yeh automatically user data ke liye dekhta hai. Windows AMIs same job ke liye **EC2Launch** use karte hain.

**Example script, line by line:**

| Line | What it does |
|---|---|
| `#!/bin/bash` | **Shebang** — cloud-init ko batata hai "isko bash se run karo." **Yeh first line omit karo aur script silently ignore ho jaata hai**, jo genuinely ek common mistake hai |
| `yum update -y` | Saare installed packages update karta hai. `yum` Amazon Linux/RHEL ka package manager hai (Ubuntu `apt` use karta hai). **`-y` har prompt ko auto-answer karta hai** — essential, kyunki koi human aur koi keyboard nahi hai. Ek command jo "47 packages install karein? [y/n]" poochne ke liye stop ho jaaye forever hang ho jaayegi |
| `yum install -y amazon-cloudwatch-agent` | CloudWatch agent install karta hai. Yeh specifically kyun? Kyunki EC2 default se memory usage ya free disk space CloudWatch ko **nahi** report karta — inko guest OS ke andar ek agent chalte hue chahiye (dekho [Container Insights](07-observability-monitoring.md#container-insights-the-cloudwatch-agent--proactive-monitoring)) |
| `systemctl enable --now amazon-cloudwatch-agent` | `systemctl` background services manage karta hai. Yeh **do** cheezein karta hai: `enable` = "har future boot par automatically start karo", `--now` = "aur abhi start karo" |

**"Ek baar run hota hai" itna zyada kyun matter karta hai jitna yeh lagta hai.** cloud-init record karta hai ki yeh already run ho chuka hai (`/var/lib/cloud/` mein), isliye **reboot par aapka script phir se execute nahi hota.** Yeh logon ko constantly catch karta hai:

> *"Maine user data mein `dotnet MyApp.dll` daala. Yeh kaam kiya. Phir instance reboot hua aur mera app gaya."*

Naturally — script doosri baar kabhi run nahi hui. **Fix user data ko har boot par re-run karne ke liye force karna nahi hai; yeh ek proper service install karna hai** taaki OS har boot par aapka app start kare. Yehi exactly wo hai jo `systemctl enable` upar example mein karta hai: script ek baar run hoti hai, aur uske *effects* persist karte hain. User data ko har boot par re-run karne ki chahat usually ek signal hai ki aapko iske bajaye ek systemd service banana chahiye tha.

**16 KB limit, aur base64.** 16 KB chhota hai — kuch sau lines. base64 sirf ek encoding hai jo text ko safely ek API se pass hone deta hai; console aur CLI (`--user-data file://script.sh`) yeh aapke liye handle karte hain, isliye aap rarely hand se encode karte ho. Jab aapka real setup limit se badh jaaye, user data ko tiny rakho aur rest fetch karo:

```bash
#!/bin/bash
aws s3 cp s3://my-bucket/bootstrap.sh /tmp/bootstrap.sh
bash /tmp/bootstrap.sh
```
Yeh box par koi credentials ke bina kaam karta hai kyunki instance ka **IAM role** unko supply karta hai (neeche dekho).

**❗ Log kyun matter karta hai: ek failed user-data script instance ko fail nahi karti.** EC2 instance ko `running` report karta hai, status checks green pass hote hain — aur aapka software simply wahan nahi hai. Kuch bhi error ko surface nahi karta. Isliye jab koi kahe *"instance aa gaya lekin kuch bhi install nahi hua,"* `/var/log/cloud-init-output.log` (aapke script ka stdout/stderr) har baar pehli jagah hai dekhne ki.

**"Golden AMI + thin user data" better pattern kyun hai.** Example script ek fine demo hai aur ek poor production practice, teen reasons se: yeh **slow** hai (hundreds of MB *har* launch par download hota hai, exactly jab ek ASG load ke under scale out kar raha hota hai), **not reproducible** (aaj launch kiya gaya instance last month launch kiye gaye se different package versions paata hai, isliye aapka "identical" fleet actually identical nahi hai), aur **fragile** (ek briefly unreachable package repo instance ko successfully booting chhod deta hai software missing ke saath, abhi bhi healthy dikhte hue).

Isliye: **sab kuch ek baar ek AMI mein bake karo** aur user data ko sirf per-instance configuration tak rakho — main kaunsa environment hoon, main kaunse cluster ko join karta hoon. **EC2 Image Builder pipeline aur ASG-launch-loop failure mode including full treatment [The Golden AMI Pattern](#golden-ami-pattern--full-explanation) mein hai.**

#### Instance Metadata (IMDS)

**What it is:** ek service jise har instance query kar sakta hai **apne baare mein** facts jaanne ke liye, ek special address `http://169.254.169.254/latest/meta-data/` par.

Wo address **link-local** hai — yeh sirf machine ke apne network link par exist karta hai, kabhi internet ke over route nahi hota, aur kahin bhi ek real server nahi hai (Nitro hypervisor locally isko answer karta hai). Isliye yeh **koi internet access, koi VPC routing, aur koi credentials ke bina** kaam karta hai.

```bash
curl http://169.254.169.254/latest/meta-data/instance-id
curl http://169.254.169.254/latest/meta-data/placement/availability-zone
curl http://169.254.169.254/latest/meta-data/iam/security-credentials/my-role   # ← temporary IAM credentials
```

**Wo last wala important hai.** Yehi tarika hai jisse aapki application apne IAM role credentials obtain karti hai — AWS SDK automatically is endpoint ko call karta hai aur expire hone se pehle unhe refresh karta hai. Yehi poora mechanism hai "role use karo taaki instance par koi access keys stored na hon" ke peeche.

**User data vs instance metadata** — conflate karna easy hai, kyunki user data *same service ke through deliver* hoti hai (`http://169.254.169.254/latest/user-data`). Lekin wo opposite directions mein point karte hain:

| | **User data** | **Instance metadata** |
|---|---|---|
| Who provides it | **Aap**, launch par | **AWS**, automatically |
| What it is | Instance ko *configure* karne ki instructions | Instance ke *baare mein* facts |
| Answers | "Startup par yeh karo" | "Main kaun hoon? Mere role ke credentials kya hain?" |
| Changes later | Sirf instance stop karke aur edit karke | Current reality reflect karta hai |

**❗ Hamesha IMDSv2 require karo.** Kyunki IMDSv1 ek plain `GET` ko answer karta hai, aapki application mein koi bhi **SSRF** bug — jahan ek attacker *aapke server* ko unka chosen URL fetch karne de sakta hai — `169.254.169.254` par aim kiya ja sakta hai aapke IAM role credentials retrieve karne ke liye, jinhe wo phir apni machine se use karte hain. Yehi mechanism Capital One breach ke peeche hai. **IMDSv2 ko pehle ek session token obtain karne ke liye ek header ke saath ek `PUT` chahiye**, jise simple SSRF perform nahi kar sakta. Launch template mein `HttpTokens: required` se isko enforce karo. Full treatment ke liye dekho [IAM Roles](03-iam-security.md#iam-roles-policies-assumerole).

---

## 2. EC2 Networking & Interfaces

### Security Groups, Unki Properties & Classic Ports

> **SG-to-SG referencing practice mein** — *fleet* SG se source kiya database ingress rule cluster ke har task ko access kyun de deta hai, aur uski jagah kya karna chahiye: [ECS → RDS: Database Connection ki Chaar Layers](10-databases-caching-analytics.md#ecs--rds-database-connection-ki-chaar-layers).

**Security group kya hota hai:** ek stateful virtual firewall jo **ENI** se attached hota hai (instance se nahi — ek instance jiske do ENIs hain, uske har interface par different rules ho sakte hain).

**Properties jo on demand list karne aani chahiye:**
- **Allow rules only.** Aap physically ek security group mein "deny" rule likh hi nahi sakte. Specific IPs ko deny karna NACL ka kaam hai.
- **Stateful** — agar aap inbound traffic allow karte ho, to response automatically outbound rules se independent, bahar jaane ke liye allow ho jaata hai (aur vice versa). Yeh NACLs se sabse bada difference hai.
- **Default posture:** saara inbound **denied**, saara outbound **allowed**.
- **Scope:** regional, aur ek VPC tak locked. Kai instances par attach ho sakta hai; ek ENI multiple SGs carry kar sakta hai (rules ka **union** hota hai — most permissive wins, kyunki koi denies nahi hote).
- **Changes immediately apply** hoti hain — koi restart nahi, koi re-attach nahi.
- **Ek SG doosre SG ko reference kar sakta hai** ek CIDR ke bajaye — yeh pattern jo matter karta hai (neeche).
- VPC ka **default** SG saara outbound plus saara inbound *apne aap se* allow karta hai, matlab isse share karne wale instances aapas mein freely baat kar sakte hain.

**CIDRs ke bajaye security groups reference karo** — yeh practical best-practice answer hai:
```
ALB-SG:  inbound 443 from 0.0.0.0/0
App-SG:  inbound 8080 from ALB-SG      ← not a CIDR
DB-SG:   inbound 5432 from App-SG      ← not a CIDR
```
App tier ab ALB ke through hi reachable hai, aur yeh tab bhi kaam karta rehta hai jab instance IPs change hote hain ya ASG scale karta hai — kabhi bhi rule updates ki zarurat nahi.

**Security Group vs NACL:**
| | Security Group | Network ACL |
|---|---|---|
| Attaches to | ENI / instance | Subnet |
| Rules | **Allow only** | Allow **and** Deny |
| State | **Stateful** (return traffic auto-allowed) | **Stateless** (must allow both directions explicitly) |
| Evaluation | All rules evaluated, union of allows | Rules in **numbered order**, first match wins |
| Typical use | Primary access control | Coarse subnet-level blocking, e.g. blacklisting an IP range |

**NACL gotcha:** kyunki NACLs stateless hote hain, aapko return traffic ke liye **ephemeral port range (1024–65535)** bhi allow karna padta hai. Ek custom-NACL subnet mein "mera SG sahi hai lekin traffic phir bhi fail ho raha hai" almost hamesha yehi hota hai.

**Debugging ka woh answer jo interviewers ko pasand hai:**
- **Connection times out / hangs** → network-layer problem: security group, NACL, route table, ya wrong subnet.
- **"Connection refused"** → network host tak sahi se pahunch gaya; us port par kuch bhi listen nahi kar raha, matlab **aapki application** down hai ya wrong interface par bound hai.

Iss distinction ko ek sentence mein sahi paana ek ghante ki real debugging bacha deta hai aur reliably impress karta hai.

**Classic ports jo memorise honi chahiye:**
| Port | Protocol / Service |
|---|---|
| 22 | SSH (also SFTP, and SCP) |
| 21 / 20 | FTP control / FTP data |
| 3389 | RDP (Windows) |
| 80 / 443 | HTTP / HTTPS |
| 53 | DNS |
| 25 / 587 / 465 | SMTP / SMTP+STARTTLS / SMTPS |
| **3306** | MySQL, MariaDB, **Aurora MySQL** |
| **5432** | PostgreSQL, Aurora PostgreSQL, Redshift |
| **1433** | Microsoft SQL Server |
| 1521 | Oracle |
| 6379 / 11211 | Redis / Memcached (ElastiCache) |
| 27017 | MongoDB / DocumentDB |
| 2049 | NFS — **EFS mount target** |

Note 22 vs 21: SSH aur SFTP dono port **22** par chalte hain; plain FTP 21 hota hai. Yeh specific pair ek favourite quiz question hai.

### Public IP vs Private IP vs Elastic IP

| | Private IP | Public IP | Elastic IP |
|---|---|---|---|
| Reachable from | Inside the VPC only | Internet | Internet |
| Assigned by | VPC subnet CIDR | AWS pool, automatically | You, and it's yours until released |
| **Survives stop/start?** | **Yes** | **No — you get a different one** | **Yes** |
| Cost | Free | Charged per hour for public IPv4 (since Feb 2024, whether attached or not) | Charged per hour, **and charged extra when *not* attached** to a running instance |
| Movable | No | No | Yes — remap to another instance/ENI in seconds |

**Classic gotcha:** ek public IP stop hone par release ho jaata hai aur start hone par **naya assign hota hai**. Jo bhi usse hardcoded hai — DNS records, kisi partner ke firewall allowlists, ek config file — woh sab break ho jaata hai. Yeh "maintenance ke baad mera integration kyun band ho gaya?" wala #1 scenario hai.

**Elastic IP** yeh solve karta hai, lekin senior answer hai *usse mat pakdo*. AWS default se region mein sirf ~5 deta hai exactly isliye kyunki yeh ek design smell hote hain. Prefer karo:
- ek **load balancer** front mein (DNS name stable rehta hai, uske peeche wale instances disposable hote hain),
- **Route 53** ek alias record ke saath,
- aur outbound-only needs ke liye, ek **NAT Gateway** (jiska apna stable EIP hota hai).

Legitimate EIP uses: ek partner firewall jo sirf ek fixed IP allowlist kar sakta hai, ek NAT gateway, ya ek fast manual failover jahan aap address ko standby instance par remap karte ho.

### Placement Groups

Yeh hai kaise aap EC2 se poochte ho ki instances physically *kahan* land karein. Aap strategy choose karte ho; AWS placement karta hai.

| Strategy | Placement | Trade-off | Use for |
|---|---|---|---|
| **Cluster** | Packed onto the same rack in a **single AZ** | Best network: low latency, high per-flow throughput (10+ Gbps). **Worst blast radius — one rack failure takes everything** | HPC, tightly-coupled compute, big-data jobs that need fast node-to-node chatter |
| **Spread** | Each instance on **distinct underlying hardware**, across AZs | Maximum isolation. Hard limit of **7 running instances per AZ per group** | Small numbers of critical instances that must never fail together (e.g. a 3-node quorum) |
| **Partition** | Instances grouped into **partitions**, each partition on its own set of racks; up to **7 partitions per AZ** | Isolation between partitions, hundreds of instances | HDFS/Hadoop, **Kafka**, Cassandra — anything that is itself rack-aware and replicates across partitions |

**Achhe se kaise answer karo:** sirf definition nahi, trade-off ka naam lo. "Cluster network performance deta hai correlated failure ki cost par; spread isolation deta hai lekin per AZ 7 tak cap karta hai; partition distributed systems ke liye middle ground hai jo already replica placement samajhte hain." Partition groups instance ko partition number bhi expose karte hain, isi se Kafka/Cassandra replicas ko sahi se place kar paate hain.

### Elastic Network Interfaces (ENIs)

Ek virtual network card. Har ENI carry karta hai: ek primary private IPv4 + optional secondary private IPs, har private IP ke liye ek Elastic IP, ek MAC address, ek ya zyada security groups, aur ek source/destination check flag.

Yeh facts jo poochhe jaate hain:
- Ek ENI **ek AZ tak bound** hota hai aur doosre AZ mein move nahi kiya ja sakta (yeh ek subnet ka hissa hota hai).
- **Primary ENI (`eth0`) detach nahi ki ja sakti** instance se. Secondary ENIs detach ho sakti hain aur ek different instance ko **same AZ mein** attach ho sakti hain — yehi cheap failover trick hai: ENI (aur uska IP, MAC, aur SGs) ek standby instance par move karo aur traffic follow karega.
- Aapko kitne ENIs aur IPs milte hain yeh **instance type se capped** hota hai — ek small instance zyada host nahi kar sakta. Yeh container density ke liye matter karta hai.
- **Source/destination check** disable karo jab instance ko traffic forward karna ho jo usne originate nahi ki (ek NAT instance, ya ek software router/firewall appliance).
- **VPC mein Lambda** private resources tak pahunchne ke liye ENIs create karta hai — isi wajah se VPC-attached Lambdas ke historically worse cold starts hote the, aur isi wajah se aap Lambda ke subnets ko AZs ke across spread karte ho (dekho [IAM Pitfalls](03-iam-security.md#iam-pitfalls) table).

---

## 3. Lifecycle, Purchasing & Sizing

### EC2 Hibernate

**Yeh kya karta hai:** instance ka **RAM encrypted root EBS volume par dump** kar deta hai aur shut down ho jaata hai. Start hone par, RAM restore ho jaata hai aur processes wahin se resume ho jaate hain jahan chhode the — koi boot nahi, koi application warm-up nahi, same instance ID aur private IP.

| | Stop | Hibernate | Terminate |
|---|---|---|---|
| RAM contents | Lost | **Preserved on the root EBS volume** | Lost |
| Boot on restart | Full OS boot | Resumes from memory image | N/A |
| Root EBS volume | Kept | Kept (and holds the RAM dump) | Deleted by default |
| Instance ID / private IP | Kept | Kept | Gone |
| Compute charges | Stopped | Stopped (you still pay for the larger EBS) | Stopped |

**Requirements** (jo list hibernate ko practice mein fail karati hai): root volume **EBS, encrypted, aur RAM image ko poora hold karne jitni bhi badi** honi chahiye; instance ek supported family/size hona chahiye jiska **RAM 150 GiB se kam** ho; hibernation **launch ke time** enable hona chahiye (baad mein on nahi kar sakte); instance-store root volumes supported nahi hain; aur ek instance maximum **60 days** tak hibernated reh sakta hai.

**Yeh kab right answer hai:** long-initialising applications (ek service jo model load karne ya cache warm karne mein minutes bitaata hai), licence-server ya dev boxes jo aapko instantly wapas chahiye, aur woh kuch bhi jahan cold-start time hi real cost hai. Yeh ASG ka substitute nahi hai — yeh ek single-instance optimisation hai.

### EC2 Purchasing Options — The Complete Set

Upar wali pricing table char options cover karti hai jo sab naam lete hain. Yeh woh options hain jo ek complete answer ko partial answer se alag karti hain:

| Option | What you're buying | Key detail |
|---|---|---|
| **On-Demand** | Pay per second/hour, no commitment | Highest rate, zero risk. Correct default for a new workload with unknown steady state |
| **Reserved Instances** | 1 or 3-year commitment to a specific config | **Standard RI** = biggest discount, locked to instance family; **Convertible RI** = smaller discount, can exchange family/OS/tenancy. Can be sold on the RI Marketplace (root-only action) |
| **Savings Plans** | 1 or 3-year commitment to a **$/hour spend** | **Compute SP** spans EC2 + Fargate + Lambda and any family/region (most flexible); **EC2 Instance SP** is deeper discount but pinned to a family+region |
| **Spot Instances** | Spare capacity at up to ~90% off | Reclaimed with a **2-minute** interruption notice. Needs checkpointing/idempotent work. Spot *Fleet* blends Spot+On-Demand across pools to reduce interruption risk |
| **Dedicated Instances** | Your instances run on hardware **not shared with other AWS customers** | You don't see or control the host. Instances may land on different hosts after stop/start |
| **Dedicated Hosts** | A whole **physical server** reserved for you, with visibility into sockets/cores | The answer for **BYOL socket/core-based licensing** (Windows Server, SQL Server, Oracle) and hard compliance mandates. Most expensive |
| **Capacity Reservations** | Reserved **capacity** in a specific AZ, held whether or not you use it | Pay On-Demand rates — **no discount and no term commitment**. This is about *guaranteed availability*, not price (DR standby, a known launch event). Combine with a Savings Plan to also get the discount |

**Dedicated Instances vs Dedicated Hosts** classic confusion pair hai: dono single-tenant hardware dete hain, lekin sirf ek **Dedicated Host** physical server (sockets, cores, host affinity) expose karta hai — jo exactly per-socket/per-core BYOL licensing ko chahiye hota hai. Agar question apna Windows/SQL Server/Oracle licence laane ka mention karta hai, to answer Dedicated Hosts hai.

**Capacity Reservation vs Reserved Instance** doosra hai: ek RI/Savings Plan ek **billing** construct hai (discount, koi capacity guarantee nahi); ek Capacity Reservation ek **capacity** construct hai (guarantee, koi discount nahi). Yeh cleanly bolna ek strong signal hai.

### EC2 Shared Responsibility Model

Lambda ya S3 ke comparison mein EC2 spectrum ke "yeh aapka kaam hai" wale end ke bahut kareeb hota hai — yehi contrast *answer* hai.

| AWS is responsible for | You are responsible for |
|---|---|
| Physical hosts, data centres, power, physical network | **Guest OS patching and hardening** — the big one, and what people forget |
| The hypervisor and host OS isolation between tenants | Application code, runtime, and dependency patching |
| Replacing failed hardware; the underlying EBS/network infrastructure | **Security group rules**, NACLs, and which subnet the instance sits in |
| Making the AZ/region infrastructure available | **Key pair management** (and not baking private keys into AMIs) |
| Compliance of the underlying infrastructure | **IAM role** attached to the instance, and requiring IMDSv2 |
| Providing encryption features (EBS/EFS encryption, KMS) | **Turning encryption on**, and managing/rotating keys |
| — | Backups: EBS snapshots, AMIs, and testing that they restore |
| — | Architecting for failure across AZs (a single instance is not highly available, no matter what AWS does) |

**One-liner:** "EC2 ke liye, AWS host *ki* security aur hypervisor ke neeche ki har cheez ke liye responsible hai; guest OS se upar — patching, firewall rules, keys, IAM, encryption, backups — yeh sab mera hai. Lambda us line ko mostly AWS par move kar deta hai; EC2 usse mere upar rakhta hai."

### EC2 Sizing, Pricing Decisions & CPU Credit Gotchas

Existing EC2 section pricing model ke *naam* cover karta hai lekin yeh nahi ki ek senior engineer actually ek box **size** karne ya real workload ke liye purchase options ke beech **choose** karne mein kaise reason karta hai — aur ismein T-family CPU credit gotcha missing hai, jo EC2 ke sabse commonly asked "production incident explain karo" questions mein se ek hai.

**Instance size actually kaise pick karte hain (sirf family nahi)**

Sizing ek vCPU-to-memory *ratio* decision hai, "budget mein fit hone wala sabse bada pick karo" decision nahi:
- **Workload shape** se start karo: CPU-bound (video encoding, compilation, hashing) → `c`-family (2 GiB RAM per vCPU); memory-bound (in-memory caches, large JVM/.NET heaps, big EF Core result sets) → `r`-family (8 GiB per vCPU); no strong lean wala general-purpose web/API tier → `m`-family (4 GiB per vCPU); dev/test, low/bursty CPU idle troughs ke saath → `t`-family (bhi ~4 GiB per vCPU, lekin *burstable*, neeche dekho).
- Commit karne se pehle benchmark karo: CloudWatch `CPUUtilization`, memory (CloudWatch Agent ke through — memory ek default EC2 metric nahi hai), aur network metrics real/representative load ke under use karo, phir roughly 40–60% average ke target steady-state utilization par size karo — spikes ke liye headroom rakhte hue, itna oversized na ho ki aap idle capacity ke liye pay kar rahe ho.
- AWS Compute Optimizer yeh analysis aapke liye actual usage history se karega aur right-sized family/size recommend karega — "guess mat karo, measure karo" wale answer ke roop mein name-drop karne layak.
- Vertical (bigger instance) vs horizontal (ALB/ASG ke peeche zyada instances) scaling trade-off: stateless web/API tiers ke liye almost hamesha horizontal scaling preferred hota hai (better fault tolerance, finer-grained cost control, rolling deploys support karta hai); vertical scaling kabhi-kabhi unavoidable hota hai un workloads ke liye jo distribute nahi ho sakti (ek single large in-memory cache node, kuch legacy monoliths).

**T-family CPU credit gotcha (interview trap ka favourite)**

Burstable (`t3`, `t4g`, `t2`) instances cheap hote hain kyunki yeh ek *baseline* CPU performance ke saath provisioned hote hain (e.g., ek full core ka 20–40%, family/size-dependent) aur baseline se neeche chalte hue **CPU credits** earn karte hain. Zarurat padne par baseline se upar burst karne mein credits spend hote hain.

```
    CPU running BELOW baseline
              |
              |  earns credits
              v
    +--------------------+
    |   Credit Balance   |
    +---------+----------+
              |
              |  spent when CPU > baseline
              v
     Burst ABOVE baseline
              |
              v
    Credit balance hits zero?
              |
    +---------+-----------+
    |                     |
 Standard mode      Unlimited mode
    |                     |
    v                     v
 Throttled back      Bursts continue,
 to baseline --      but billed extra
 a hard cap, and     per vCPU-hour
 the app slows       beyond baseline
 down                (cost surprise)
```

- **Standard mode**: credit balance exhaust hone par, CPU baseline percentage tak hard-throttle ho jaata hai — yehi classic "app hours tak fine tha, phir suddenly bina kisi obvious reason ke sluggish/unresponsive ho gaya" wala production incident hai. Root cause almost hamesha ek sustained load period hota hai (batch job, traffic spike, backup/reindex) jo accumulated credit balance se zyada chala.
- **Unlimited mode**: bursts indefinitely continue kar sakte hain, lekin AWS aapse baseline se zyada sustained usage ke liye extra bill karta hai (per vCPU-hour) — availability protect karta hai lekin ek cost surprise de sakta hai agar ek T-instance quietly 24/7 hot chal rahi hai (yeh sign hai ki aap T-family se outgrow kar gaye ho aur `m`/`c` par move karna chahiye).
- **Interview scenario mein diagnosis:** "Hamara T3 instance sustained load ke under slow ho gaya, lekin CPUUtilization graphs 100% par pegged nahi lagte" → `CPUCreditBalance` aur `CPUSurplusCreditBalance` CloudWatch metrics check karo, sirf `CPUUtilization` nahi — ek throttled T-instance CPU ko 100% se kaafi neeche capped dikha sakta hai kyunki yeh baseline par held hai, jo deceptively "healthy" lagta hai jab tak aapko credits specifically check karna na pata ho.
- **Rule of thumb:** T-family un workloads ke liye right hai jinme genuine idle troughs hoti hain (dev/test, low-traffic APIs, bursty-but-brief admin jobs); yeh kisi bhi sustained high-CPU period wale workload ke liye *wrong* choice hai (nightly batch processing, backup windows, steady-state compute-heavy services) — woh `m`/`c`/`r` family instances par belong karte hain jinme koi credit mechanism khatam hone wala nahi hota.

**On-Demand vs Reserved vs Spot vs Savings Plans — actually decide kaise karte ho (sirf definitions nahi)**

Iss guide ka later Cost Optimization section discount percentages already table kar deta hai; jo senior-level skill yahan test ho rahi hai woh **decision process** hai, definitions nahi:

| Question to ask yourself | Points toward |
|---|---|
| Is this workload's capacity need predictable 12+ months out? | Savings Plan / Reserved Instance |
| Could this workload be interrupted with ~2 minutes' notice without breaking correctness? | Spot |
| Is this a brand-new workload with unknown steady-state yet? | On-Demand first, commit later once usage data exists |
| Does the workload span multiple compute types (EC2 + Fargate + Lambda)? | Compute Savings Plan (flexible across compute types) over EC2 Instance Savings Plan/RI |
| Is this dev/test that's only running business hours? | On-Demand + scheduled stop/start (Instance Scheduler) — commitment-based discounts don't help if the instance isn't running most of the time anyway |
| Is the workload stateful with no cheap way to checkpoint/resume? | Avoid Spot — the 2-minute reclaim notice isn't enough to gracefully drain long-lived state |

**Interview-ready answer:** "Main seedhe 'Savings Plans khareedo' par nahi jaaunga — main pehle confirm karunga ki workload commit karne ke liye stable aur predictable hai, check karunga ki yeh interruption tolerate kar sakta hai kya (Spot candidate), aur tab hi ek Compute Savings Plan (flexible, EC2/Fargate/Lambda ke across) versus ek EC2/Instance Savings Plan (deeper discount, less flexible) ke beech choose karunga based on kitna confident hoon instance family fixed rehne ke baare mein."

**EC2 sahi compute choice kab hai Lambda/Fargate ke against — woh decision jo interviewer actually chahta hai**

Iss guide mein already ek Lambda-vs-ECS-vs-EC2-vs-Fargate comparison table aur ek dedicated .NET compute-decision section hai — yahan add karne layak angle hai kaise EC2 ko specifically *justify* karo jab aap woh ho jisne isse Terraform/CDKTF ke through provision kiya, ek serverless default choose karne ke bajaye:

- **Full OS/kernel access ek hard requirement hai** — custom kernel modules, specific driver versions, GPU workloads, ya software jo assume karta hai ki woh host ka owner hai (kuch legacy .NET Framework/COM-interop scenarios) — Fargate aur Lambda dono OS ko abstract away kar dete hain, jo ek feature hai jab tak yeh blocker nahi ban jaata.
- **Steady, high, predictable utilization** — agar ek service 70–90% CPU par 24/7 chalti hai, EC2 (especially ek Savings Plan ke saath) usually sabse cheap option hota hai; Fargate ka per-task premium aur Lambda ka per-invocation billing dono yeh comparison haar jaate hain jab utilization consistently high ho.
- **Aapke paas already IaC investment hai** — "Main already EC2 ko Terraform/CDKTF ke through provision kar raha hoon, isliye operational tooling (state, modules, CI plan/apply pipeline) ek sunk cost hai jo EC2 ko marginally cheap banata hai *operate* karne ke liye, sirf chalane ke liye nahi" ek legitimate, honest talking point hai — lekin isse kabhi primary justification nahi hona chahiye. Ek interviewer "kyunki mujhe yehi pata hai" wali baat ko push back karega, aur rightly so — workload-shape argument (steady utilization, OS-level requirement) lead karo aur existing Terraform tooling ko secondary, practical factor ke roop mein mention karo.
- **Long-running processes jinka in-memory state trivially externalize nahi kiya ja sakta** — jaise ek stateful cache ya ek process jo ek large warmed-up in-memory model hold karta hai — EC2 ko Lambda ke stateless-between-invocations model ke against favour karta hai, halanki ECS/Fargate EFS ya ek sticky task ke saath kabhi-kabhi yeh satisfy bhi kar sakta hai.
- **Counter-signal jo dekhna chahiye:** agar aap khud ko EC2 ko purely "iske baare mein reason karna simpler hai" ya "main containers seekhna nahi chahta" par justify karte paate ho, yeh ek comfort-driven answer hai, workload-driven nahi — senior interviewers specifically yeh sun rahe hote hain ki aap "mujhe kya pata hai" aur "workload ko kya chahiye" ko separate karte ho ya nahi.

---

## 4. Block & File Storage

### EBS vs EFS vs S3

| | EBS | EFS | S3 |
|---|---|---|---|
| Type | Block storage | Managed NFS (file) | Object storage |
| Attach model | One EC2 instance (or Multi-Attach for specific volume types) | Many instances/AZs concurrently | HTTP API, unlimited clients |
| Use case | DB data volumes, boot volumes | Shared config/content across a fleet, Lambda file storage extension | Static assets, backups, data lake, logs |
| Scaling | Manual resize | Elastic, auto-scales | Effectively unlimited |
| .NET relevance | RDS/self-managed SQL Server data files | Shared session state/uploads across ECS tasks | Blob storage equivalent to Azure Blob Storage |

### EBS Volumes

**EBS actually kya hota hai:** ek **network drive**, physical disk nahi. Yeh single fact iske zyada tar behaviour explain karta hai — network latency hoti hai, isse detach aur reattach kiya ja sakta hai, aur yeh instance se zyada jeeta hai.

**Properties jo poochhi jaati hain:**
- **Ek Availability Zone tak locked.** Ek `us-east-1a` volume `us-east-1b` instance se attach nahi ho sakta. Ise move karne ke liye aap snapshot lo aur target AZ mein ek naya volume create karo — yehi *the* answer hai "AZs/regions ke across ek volume kaise move karoon?" ke liye.
- **Ek time par ek instance** (Multi-Attach ke siwa, neeche dekho).
- **Provisioned capacity** — aap jo GB aur IOPS *provision* karte ho uske liye pay karte ho, jo use karte ho uske liye nahi. Ek 1 TB volume jismein 4 GB hai uska cost ek full volume jitna hi hoga.
- **Online resize ho sakta hai** (Elastic Volumes) — size badhao, type change karo, IOPS change karo, koi downtime nahi. Aap volume ko **shrink nahi kar sakte** — ek smaller volume create karo aur copy karo.
- **`DeleteOnTermination`** default se **root volume ke liye true** aur **additional volumes ke liye false** hota hai. Yeh asymmetry ek real interview question hai aur ek real production incident bhi: instance terminate karo aur root volume (aapke logs ke saath) chala jaata hai, jab ki orphaned data volumes quietly forever cost accrue karti hain.
- Volumes instance se independently persist karte hain — detach karo, same AZ mein kahin aur reattach karo, aur data intact rehta hai.

```bash
aws ec2 create-volume --availability-zone us-east-1a --size 100 --volume-type gp3 --encrypted
aws ec2 attach-volume --volume-id vol-abc --instance-id i-abc --device /dev/sdf
# on the instance: format (first time only!) and mount
lsblk                                   # confirm the device is visible
sudo mkfs -t xfs /dev/nvme1n1           # ⚠ destroys data — never run on a volume with data
sudo mkdir /data && sudo mount /dev/nvme1n1 /data
# add to /etc/fstab (by UUID, not device name) so it survives a reboot
```
**Do hands-on gotchas jo naam lene layak hain:** device names remap ho jaate hain (aap `/dev/sdf` maangte ho, Nitro `/dev/nvme1n1` dikhata hai), aur agar aap `/etc/fstab` entry bhool jaao to mount next reboot par silently gayab ho jaata hai. Dono neeche explain kiye gaye hain, kyunki dono trivia nahi, real production incidents hain.

**Gotcha 1 — jo device name aap request karte ho woh Linux use karne wala naam nahi hota.** Aap `--device /dev/sdf` se attach karte ho, lekin kisi bhi Nitro instance (M5/C5/T3/R5 aur newer) par disks NVMe hote hain, isliye Linux ise `/dev/nvme1n1` bolta hai. `/dev/sdf` sirf ek label hai jo AWS apne console mein record karta hai, aur `mkfs -t xfs /dev/sdf` "No such file or directory" ke saath fail hota hai.

Isliye **hamesha pehle `lsblk` run karo** aur volume ko size + "no partitions, no mountpoint" se identify karo:
```
NAME          SIZE TYPE MOUNTPOINTS
nvme0n1         8G disk
└─nvme0n1p1     8G part /          <- root volume, already mounted
nvme1n1       100G disk            <- the volume I just attached
```
Yeh guess ke bajaye care deserve karta hai kyunki: **wrong device par `mkfs` run karna aapka root volume format kar deta hai aur instance destroy kar deta hai.** Aur NVMe numbering stable nahi hoti — kai volumes attached hone par, aaj ka `nvme1n1` reboot ke baad `nvme2n1` ho sakta hai, isliye NVMe names bhi hardcode mat karo. Ek device ko real volume ID par map karne ke liye:
```bash
sudo nvme id-ctrl -v /dev/nvme1n1 | grep -i sn    # serial = the EBS volume ID (vol-0abc…)
ls -l /dev/disk/by-id/                            # stable nvme-Amazon_Elastic_Block_Store_vol… symlinks
```

**Gotcha 2 — `mount` temporary hai; `/etc/fstab` ke bina yeh reboot par gayab ho jaata hai.** `mount` sirf running system ko change karta hai, memory mein held. Reboot ke baad `/data` phir se **root** volume par ek ordinary empty folder ban jaata hai, aur ek saath teen cheezein galat ho jaati hain:
1. Application `/data` par likhte rehna jaari rakhta hai — ab yeh 100 GB data volume ke bajaye chhoti **root** disk fill kar raha hai.
2. Existing data lagta hai gayab ho gaya (yeh safe hai EBS volume par, sirf us path se attached nahi hai).
3. **Sabse nasty wala:** jab aap baad mein volume ko `/data` par remount karte ho, root-disk `/data` par jo bhi likha gaya woh **invisible** ho jaata hai — ek directory ke upar mount karna uske contents ko hide kar deta hai. Ab aapka data do jagah split hai aur usme se aadha hidden hai.

Fix, filesystem **UUID** use karke (yeh filesystem ke andar stored hota hai, isliye device renaming se survive karta hai):
```bash
sudo blkid /dev/nvme1n1        # -> UUID="a1b2c3d4-…" TYPE="xfs"
# /etc/fstab
# UUID=a1b2c3d4-…  /data  xfs  defaults,nofail  0  2
sudo umount /data && sudo mount -a && df -h /data   # ← TEST before rebooting
```
**`nofail` optional nahi hai:** iske bina, ek instance jiska volume boot ke time missing hai emergency mode mein drop ho sakta hai aur aap **SSH access completely lose kar sakte ho** — classic "maine fstab edit ki aur ab main apne server mein ghus nahi sakta," jise undo karne ke liye ek rescue-instance procedure chahiye hota hai. Aur `mount -a` woh step hai jo sab skip karte hain: agar yeh succeed hota hai, reboot bhi hoga. Ek broken fstab *reboot karke* discover karna hi logon ke khud ko lock out karne ka tarika hai.

**One-line version:** "`/dev/sdf` maango, lekin real NVMe name dhoondne ke liye hamesha `lsblk` karo — aur bina `/etc/fstab` entry (UUID se, `nofail` ke saath) ek `mount` next reboot par silently gayab ho jaata hai."

### EBS Volume Types

| Type | Class | Performance | Boot volume? | Use for |
|---|---|---|---|---|
| **gp3** | SSD, general purpose | Baseline **3,000 IOPS / 125 MB/s regardless of size**, up to 16,000 IOPS / 1,000 MB/s — IOPS provisioned **independently of capacity** | ✅ | **The default choice.** ~20% cheaper than gp2 |
| gp2 | SSD, general purpose | 3 IOPS per GB (so you had to over-provision *size* just to get IOPS), max 16,000 | ✅ | Legacy — migrate to gp3 |
| **io2 Block Express** | SSD, provisioned IOPS | Up to 256,000 IOPS, sub-millisecond latency, **99.999% durability** | ✅ | Mission-critical databases, large SQL Server/Oracle workloads |
| io1 | SSD, provisioned IOPS | Up to 64,000 IOPS | ✅ | Older generation of the above |
| st1 | **HDD**, throughput optimised | Up to 500 MB/s, low IOPS | ❌ | Big sequential reads: log processing, data warehouse, ETL |
| sc1 | **HDD**, cold | Up to 250 MB/s, cheapest per GB | ❌ | Infrequently accessed archive data that must still be a filesystem |

Do facts jo almost hamesha question hote hain: **sirf SSD types (gp2/gp3/io1/io2) boot volumes ho sakte hain** — HDD types nahi ho sakte. Aur **gp3 ka headline improvement IOPS ko size se decouple karna hai**: gp2 ke under, 6,000 IOPS chahiye to aapko ek 2 TB volume provision karne ki zarurat pad jaati thi jo aapko chahiye nahi thi. Yeh keh paana types naam lene aur unhe samajhne ke beech ka difference hai.

### EBS Snapshots

**Yeh kya hain:** ek volume ke point-in-time backups, **S3 mein AWS-managed** stored hote hain (ek bucket mein nahi jo aap dekh sakte ho), aur **incremental** hote hain — pehla snapshot har used block copy karta hai, baad wale sirf changed blocks copy karte hain. Ek old snapshot delete karna kabhi bhi ek newer ko break nahi karta; AWS jo bhi blocks abhi bhi referenced hain unhe rakhta hai.

Key behaviours:
- Aap ek attached, running volume ko snapshot **kar sakte ho**, lekin ek database ke liye pehle quiesce/flush karna chahiye (ya DB ka apna backup use karo) — warna aapko crash-consistent, application-consistent nahi, image milta hai.
- Ek snapshot **region-scoped but AZ-independent** hota hai: ise **kisi bhi AZ** mein restore karo, ya **kisi doosre region mein copy** karo — yeh copy standard EBS disaster-recovery move hai.
- **Fast Snapshot Restore (FSR)** lazy-loading penalty hata deta hai (ek snapshot se fresh volume normally first touch of each block par slow hota hai). Har snapshot per AZ extra cost karta hai; sirf un snapshots ke liye enable karo jo aap time pressure ke under restore karte ho.
- **Snapshot Archive** tier ~75% cheaper hai lekin restore hone mein **24–72 hours** leta hai — compliance retention ke liye, recovery ke liye kabhi nahi.
- **Recycle Bin** aapko ek retention rule set karne deta hai taaki deleted snapshots/AMIs recover ho saken — ek fat-fingered ya malicious deletion ke against guard.
- **Data Lifecycle Manager (DLM)** ek schedule par snapshot creation/retention automate karta hai, aur yeh "EBS ka backup kaise karte ho?" ka correct answer hai (CLI call karne wale ek cron job ke bajaye). AWS Backup iska bigger, cross-service version hai.

```bash
aws ec2 create-snapshot --volume-id vol-abc --description "pre-upgrade 2026-08-08"
aws ec2 copy-snapshot --source-region us-east-1 --source-snapshot-id snap-abc \
  --destination-region us-west-2 --encrypted          # DR copy
aws ec2 create-volume --snapshot-id snap-abc --availability-zone us-east-1b --volume-type gp3
```
**Mention karne layak cost leak:** snapshots sabse commonly forgotten AWS charge hain — decommissioned volumes ke saal-bhar ke nightly snapshots. DLM retention rules, ya ek Config rule, isse rokne ka tarika hai.

### AMIs (Amazon Machine Images)

**AMI kya hota hai:** ek *machine* ke liye ek launch template — root (aur kisi bhi additional) volumes ke snapshots, plus block-device mapping aur launch permissions. Ek EBS snapshot ek **disk** backup karta hai; ek AMI ek **bootable machine** backup karta hai. Yehi distinction hai jo interviewers probe karte hain.

- **AMIs region-scoped hote hain.** Aapko har region mein jahan aap launch karte ho ek AMI **copy** karna padta hai — isi wajah se ek multi-region deployment pipeline mein ek AMI-copy step hota hai.
- Types: AWS-provided (Amazon Linux, Windows Server), **Marketplace** (licence charge carry kar sakta hai), aur **aapki apni custom AMIs**.
- **Ek create karna default se instance ko reboot karta hai** taaki filesystem consistent rahe. `--no-reboot` downtime avoid karta hai lekin ek corrupt/inconsistent image ka risk leta hai — sirf tab safe hai jab app quiesced ho.
- **Golden AMI pattern:** OS patches, runtime (jaise .NET runtime), agents (CloudWatch, SSM), aur aapki app ki dependencies ko pre-bake karo ek AMI mein, phir user data thin rakho. ASG scale-out installing everything at boot se faster, more reliable hota hai. **EC2 Image Builder** yeh building, patching, testing, aur distributing ko ek schedule par automate karta hai.
- **Ek AMI deregister karna uske snapshots delete nahi karta** — ek aur silent cost leak, aur jaanne layak ek achhi detail.

```bash
aws ec2 create-image --instance-id i-abc --name "dotnet8-base-2026-08" --description "golden AMI"
aws ec2 copy-image --source-region us-east-1 --source-image-id ami-abc --region us-west-2 --name "dotnet8-base"
```

#### Golden AMI Pattern — Full Explanation

**Idea:** har cheez **ek baar, image build karte waqt** install karo — har baar jab instance launch ho tab nahi. "Baking" ka matlab hai ek machine boot karna, sab install karna, aur ise ek reusable AMI mein snapshot karna. Corporate IT ki tarah jo aapko ek blank laptop plus install checklist ke bajaye ek pre-imaged laptop deta hai.

**Before and after:**
```bash
# THICK user data — install at boot:  launch -> serving traffic = 4-6 min
yum update -y                                # 2-4 min, 200+ MB
rpm -Uvh https://packages.microsoft.com/...  # external repo dependency
yum install -y dotnet-runtime-8.0            # ~60s
yum install -y amazon-cloudwatch-agent       # ~30s
aws s3 cp s3://builds/myapp.zip /opt/ && unzip ... && systemctl start myapp

# THIN user data — golden AMI already has OS patches, .NET 8, agents, deps
#                                            launch -> serving traffic = 60-90s
echo "ASPNETCORE_ENVIRONMENT=Production" > /etc/myapp.env
aws s3 cp s3://builds/myapp-v42.zip /opt/app/ && systemctl start myapp
```

**Teen reasons jo yeh minutes matter karte hain** (doosra aur teesra woh hain jo ek good answer ko generic answer se alag karte hain):
1. **Auto Scaling sirf tab useful hai jab yeh fast ho.** Ek subah 9 baje ki spike alarm fire karti hai, ASG 5 instances launch karta hai — aur thick user data ke saath yeh 5–6 minutes tak useless rehte hain, matlab aapke *existing* instances 5–6 minutes tak timeouts serve karte rehte hain. Slow launches ka matlab hai aap hamesha uss traffic ke liye scale kar rahe ho jo aapke paas kai minutes pehle tha.
2. **Aapka fleet actually identical nahi hai.** `yum update -y` jo bhi *uss moment* current hai woh pull karta hai, isliye ek January instance ke package versions ek March instance se different honge, same config se — jisse worst kind ka bug report bante hai: *"yeh sirf kuch instances par fail hota hai."*
3. **❗ Ek network blip ek ASG launch loop ban jaata hai.** Agar ek external repo 20 seconds ke liye unreachable hai to install fail hota hai, script exit ho jaata hai, aur **instance phir bhi "successfully" boot ho jaata hai** — sirf koi runtime nahi hota. Phir: app kabhi start nahi hoti → ALB health check fail → ASG terminate kar deta hai → ek replacement launch karta hai → same failure → **repeat forever**, zero healthy capacity ke saath. Aapke boot path ka har external dependency isi ka ek chance hai, aur baking unhe sab hata deta hai.

**Kya bake karo, aur har ek kyun:**
| Bake in | Why |
|---|---|
| **OS patches** | Avoids a 200 MB download per launch, and makes the patch level *known* rather than "whatever was current" |
| **Runtime** (.NET 8) | The largest install, and it depends on an external repo |
| **CloudWatch agent** | EC2 reports no memory or disk-space metrics without it |
| **SSM agent** | Enables Session Manager (shell with no SSH keys) and Patch Manager |
| **App dependencies** | Native libs, fonts, certificates — usually not the app itself |

**"Thin user data"** ka matlab tab sirf yeh hai jo genuinely per instance ya environment differ karta hai: kaunsa environment, kaunsa cluster join karna hai, kaunsa config fetch karna hai. **Config, installation nahi.**

**EC2 Image Builder** manual launch → install → snapshot cycle ko ek pipeline mein badal deta hai:
```
1. RECIPE       base AMI + components: patch OS - install runtime - install agents - CIS hardening
2. BUILD        spins up a temp instance, runs the components, snapshots it
3. TEST         boots the new AMI and runs smoke tests — fails the pipeline if broken
4. DISTRIBUTE   copies the AMI to every region, shares it to every account
5. SCHEDULE     re-runs monthly, or on a critical CVE, so patches actually land
```
**Test phase** underrated part hai — yeh ek broken image ko aapke launch template tak pahunchne se rokta hai — aur yeh **Amazon Inspector** ke saath integrate hota hai release se pehle CVEs scan karne ke liye. **Packer** iska alternative hai aur naturally Terraform/CDKTF ke saath same toolchain mein fit hota hai, clouds ke across kaam karta hai.

**Mindset shift:** aap servers patch karna band karte ho aur unhe **replace** karna start karte ho.
```
Image Builder produces AMI v43 -> update the launch template ->
ASG instance refresh (rolling replacement) -> deregister old AMIs AND delete their snapshots
```
Yehi **immutable infrastructure** hai: in-place patching snowflakes banata hai jo apart drift karte hain, jab ki image rebuild karna aur fleet roll karna har instance ko identical rakhta hai.

**Honest trade-offs:** build pipeline maintain karne layak real infrastructure hai; AMI sprawl aur orphaned snapshots agar clean up na karo to paisa cost karte hain; aur iteration slower hai, kyunki ek dependency change karne ka matlab hai ek image rebuild karna — isi wajah se kai teams dev mein thick user data aur prod mein golden AMIs use karti hain. **Container parallel:** ek Docker image containers ke liye *ek golden AMI hai* — same philosophy, kaafi faster builds — isi wajah se pattern "custom AMI **or container image**" ke roop mein stated hota hai. ECS/Fargate par yeh aapko free milta hai.

**Interview answer:** *"Main ek golden AMI bake karunga patched OS, runtime, aur agents ke saath EC2 Image Builder ke through ek monthly schedule par, aur user data ko sirf per-instance config tak rakhunga. Yeh ASG scale-out ko ~5 minutes se ~90 seconds tak cut karta hai, identical instances guarantee karta hai, aur boot path se external repos hata deta hai — kyunki ek repo briefly unreachable hone se aapko ek instance milta hai jo healthy boot hota hai bina runtime ke, jo ek ASG launch loop ban jaata hai. Patching phir instance refresh ke through rebuild-and-roll ban jaata hai, in-place patching ke bajaye."* Jo test ho raha hai woh yeh hai ki kya aap **launch time ko ek availability concern** ke roop mein dekhte ho aur immutable-infrastructure terms mein sochte ho.

### Instance Store

**Host par physically attached NVMe disks** — network storage nahi. Yeh isse kisi bhi EC2 storage ke sabse highest possible IOPS (millions) aur lowest latency deta hai, aur uski ek defining limitation bhi:

- **Ephemeral.** Instance **stop, hibernate, terminate** hone par ya underlying host fail hone par data lost ho jaata hai. Yeh reboot **survive kar leta hai**.
- Snapshot, resize, ya detach/reattach nahi ho sakta. Capacity instance type se fixed hai (`d`/`i` families).
- Replication aur durability ke liye aap responsible ho — full stop.

**Correct uses:** scratch space, temp files, buffers, caches, aur distributed databases jo already nodes ke across replicate karti hain (Cassandra, Elasticsearch/OpenSearch data nodes). **Wrong use:** kuch bhi jo aap kisi doosre source se rebuild nahi kar sakte.

**One-liner:** "Instance store fastest aur least durable option hai — EBS ek network drive hai jo instance se zyada jeeta hai, instance store local hardware hai jo nahi."

### EBS Multi-Attach

Ek **single io1/io2** volume ko **same AZ mein 16 tak Nitro instances** ke saath simultaneously attach karne deta hai, har ek ko full read/write access ke saath.

Woh point jo sab miss karte hain: **ek normal filesystem (ext4, XFS, NTFS) khud ko corrupt kar lega** agar do instances ise ek saath mount karein, kyunki har ek metadata independently cache karta hai. Multi-Attach sirf ek **cluster-aware filesystem** (GFS2, OCFS2) ya ek application jo raw block access aur apni khud ki locking manage karta hai, uske saath kaam karta hai. Toh yeh general web fleet ke liye shared-storage ka answer *nahi* hai — woh **EFS** hai. Multi-Attach clustered HA applications ke liye exist karta hai jinhe concurrent raw block access chahiye, jaise Oracle RAC-style setups.

Constraints: sirf io1/io2, sirf ek AZ, 16 instances max, sirf Nitro instances.

### EBS Encryption

**KMS (AES-256)** ke through encryption at rest, aur yeh log jo expect karte hain usse zyada cover karta hai:
- Volume par data at rest
- **Instance aur volume ke beech data in transit**
- Volume se banaye gaye saare **snapshots**
- Un snapshots se banaye gaye saare **volumes** (encryption automatically propagate hota hai)

Performance impact negligible hai (Nitro hardware dwara handle hota hai), isliye ise off rakhne ka koi achha reason nahi hai. Account/region level par **EBS encryption by default** on karo taaki kisi ko yaad rakhne ki zarurat na pade.

**Ek existing unencrypted volume ko encrypt kaise karte ho** — yeh exact procedure ek common question hai, kyunki aap encryption ko in place flip nahi kar sakte:
1. Unencrypted volume ko snapshot karo.
2. Snapshot ko **copy** karo, `--encrypted` aur ek KMS key specify karke (copy step hi hai jahan encryption introduce hota hai).
3. Encrypted snapshot se ek naya volume create karo.
4. Instance stop karo, old volume detach karo, naya volume same device name par attach karo, start karo.

Sharing note: aap ek unencrypted snapshot publicly share kar sakte ho, lekin **default AWS-managed key** se encrypted ek snapshot **share hi nahi ho sakta** — aapko ek **customer-managed KMS key** use karna hoga aur doosre account ko usse access grant karna hoga. Yehi gotcha hai "doosra account mera snapshot kyun use nahi kar sakta?" ke peeche.

### EFS (Elastic File System)

**Managed NFS** jise multiple **AZs** ke across kai instances **ek saath** mount kar sakte hain — shared-filesystem ka answer, jahan EBS single-attach ka answer hai.

- **POSIX/NFSv4.1 — sirf Linux.** ❗ **EFS Windows support nahi karta.** Windows workloads ke liye aap **Amazon FSx for Windows File Server** use karte ho (SMB, Active Directory integrated). High-performance Linux/HPC ke liye **FSx for Lustre** hai. Jab koi Windows .NET Framework app ke liye shared storage ke baare mein poochhe to FSx ka naam lena ek strong differentiator hai.
- **Truly elastic** — petabytes tak automatically grow aur shrink hota hai; aap **jo GB actually store hota hai uske liye pay karte ho**, koi provisioning nahi. Lekin yeh roughly **gp3 se 3× cost** karta hai, isliye yeh default nahi hai — use karo jab aapko genuinely concurrent shared access chahiye.
- Access ek **mount target per AZ** ke through hota hai, har ek ke saath ek security group. Isse client SG se inbound **NFS port 2049** allow karna hi hoga — yehi "EFS mount hangs" ka #1 cause hai.
- **Performance modes:** *General Purpose* (default, lowest latency) vs *Max I/O* (higher throughput/parallelism, slightly higher latency, thousands of clients ke liye).
- **Throughput modes:** *Bursting* (size ke saath scale hota hai, credits exhaust ho sakte hain — T-family CPU credits jaisa hi trap shape), *Provisioned* (fixed, small-but-busy filesystems ke liye), *Elastic* (auto, spiky/unknown workloads ke liye best default).
- **Storage classes:** Standard, **One Zone** (~47% cheaper, single-AZ — dev ke liye theek hai, production HA ke liye nahi), plus Infrequent Access aur Archive tiers, **lifecycle management** ke saath jo N days ke access na hone ke baad files automatically move kar deta hai.
- KMS ke through encryption at rest, TLS ke through in transit.

```bash
sudo mount -t efs -o tls fs-0123456789abcdef:/ /mnt/efs      # amazon-efs-utils, TLS in transit
```
**Typical uses:** ek ECS ya EC2 fleet ke across shared uploads/content, CMS (WordPress) document roots, shared config, CI build caches, aur **Lambda file storage** un functions ke liye jinhe 512 MB `/tmp` se zyada ya invocations ke beech shared state chahiye.

### EFS vs EBS vs Instance Store

| | EBS | EFS | Instance Store |
|---|---|---|---|
| Storage type | Block (network drive) | File (managed NFS) | Block (local hardware) |
| Attach | 1 instance (16 with Multi-Attach + cluster FS) | **Many instances, many AZs** | 1 instance, physically |
| AZ scope | **Single AZ** — snapshot to move | **Multi-AZ** (or One Zone class) | Single host |
| Capacity | **Provisioned** — pay for what you allocate | **Elastic** — pay for what you store | Fixed by instance type |
| Durability if instance dies | Survives | Survives | **Lost** |
| Performance | Good, tunable IOPS | Good, scales with load | **Highest** |
| OS support | Any | **Linux only** (Windows → FSx) | Any |
| Relative cost per GB | Baseline | ~3× EBS | Included in instance price |
| Typical use | Boot volumes, database data files | Shared content across a fleet | Cache, scratch, replicated DB nodes |

**Decision sentence:** "Ek instance ko fast disk chahiye → EBS. Kai instances ko ek saath same files chahiye → EFS (ya Windows par FSx). Mujhe maximum speed chahiye aur main data rebuild kar sakta hoon → instance store."

### EC2 Storage Shared Responsibility Model

| AWS is responsible for | You are responsible for |
|---|---|
| Durability of the EBS/EFS infrastructure; replicating EBS within its AZ | **Taking snapshots/AMIs and testing that they restore** |
| Replacing failed underlying hardware transparently | Choosing the right volume type and sizing IOPS/throughput |
| Providing encryption capability (KMS-integrated) | **Enabling encryption** and managing/rotating the keys |
| Physically destroying decommissioned drives | **Filesystem-level data protection**, access permissions, and what you put on the disk |
| Availability of the EFS mount targets | Security groups on those mount targets (NFS 2049), and the data your clients write |
| — | Knowing **instance store is ephemeral** and replicating anything that matters |

**Disaster Recovery — EC2 & Instance Storage**

| | |
|---|---|
| **Actually risk par kya hai** | **Instance store data (unrecoverable — stop par bhi chala jaata hai, sirf terminate par nahi)**, EBS volumes, machine configuration |
| **Backup mechanism** | **EBS snapshots** (incremental, S3 mein stored), scheduled snapshots + retention ke liye **Data Lifecycle Manager**, whole-machine recovery ke liye **AMIs**, cross-region snapshot/AMI copy |
| **Realistic RPO / RTO** | RPO = aapka DLM schedule (typically ghante). RTO minutes — AMI se launch |

**Recovery runbook:**
1. **Instance failure:** launch template se last **golden AMI** se launch karo — agar wo ASG ke peeche hai to yeh automatic hai aur karne ko kuch nahi.
2. **Volume corruption:** `aws ec2 create-volume --snapshot-id snap-xxx --availability-zone <az>`, phir kharab volume detach karke naya usi device name par attach karo.
3. **Regional failure:** us **AMI se launch karo jo aapne pehle se DR region mein copy ki thi** (`aws ec2 copy-image`), phir Elastic IP reattach/reallocate karo aur Route 53 record update karo.
4. Confirm karo ki **ASG launch template DR-region ki AMI ID reference kar raha hai** — AMI IDs region-specific hoti hain, toh copy kiya hua launch template ek aisi AMI par point karta hai jo wahan exist hi nahi karti.

⚠️ **Gotcha:** **snapshots aur AMIs regional hote hain**, toh copy na ki gayi snapshot exactly us scenario mein bekaar hai jiske liye li gayi thi. AMIs ko DR region mein **build step ke roop mein** copy karo, incident ke waqt nahi. Aur instance-store ki baat seedha bol do: wo **kabhi** backup nahi hoti aur ek simple *stop* use kho deta hai — toh koi bhi cheez jo matter karti hai wahan rehne hi nahi di jaati.

---

← [S3](05-s3.md) · [Index](README.md) · [Observability & Monitoring](07-observability-monitoring.md) →
