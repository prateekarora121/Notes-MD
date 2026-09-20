> **AWS Detailed Guide** · [Index](README.md) · Part I

# EC2 & Instance Storage

> **Tier 1 — bulletproof.** A named resume skill, provisioned via CDKTF/Terraform. EBS/EFS/AMI material lives here because it's inseparable from running EC2 in practice.

---

## 1. EC2 Fundamentals

### EC2 Fundamentals

**Core concepts**
- **Instance** — running VM. **AMI** — template (OS + software) used to launch it.
- **Instance families**: `t` (burstable, e.g. t3/t4g), `m` (balanced), `c` (compute-optimized), `r` (memory-optimized), plus GPU (`g`/`p`) and storage-optimized (`i`/`d`) families.
- **Storage**: EBS (persistent, network-attached) vs Instance Store (ephemeral, physically attached, lost on stop/terminate).
- **Networking**: ENI, Security Groups (stateful), subnets.

**Lifecycle:** Launch → Running → Stop/Start → Terminate.
- **Stop**: EBS-backed data persists; instance ID kept; you stop paying compute (but still pay for EBS).
- **Terminate**: instance and (by default) root EBS volume destroyed.

**Pricing models**
| Model | Commitment | Relative cost | Risk |
|---|---|---|---|
| On-Demand | None | Highest | None |
| Reserved Instances | 1–3 yrs | Lower | Locked in |
| Savings Plans | 1–3 yrs $/hr commitment | Lower, more flexible than RI | Locked in $ amount |
| Spot | None | Cheapest | Can be reclaimed with 2-min warning |

**When to choose EC2:** full OS control, custom kernel modules, legacy apps, stateful workloads, GPU/specialized hardware, long-running services with steady utilization.

**Limitations:** you own patching/scaling; idle instances still cost money; more moving parts operationally than serverless/Fargate.

### EC2 Instance Types, User Data & Metadata

**Decoding an instance type name** — `m5dn.2xlarge`:
```
m      5       dn        .2xlarge
│      │       │          └─ size (vCPU/memory scale)
│      │       └─ extra attributes
│      └─ generation (higher = newer, usually better price/performance)
└─ family (workload class)
```
| Letter | Meaning |
|---|---|
| `t` | Burstable — baseline CPU + credits (see the CPU-credit trap below) |
| `m` | General purpose, ~4 GiB RAM per vCPU |
| `c` | Compute optimised, ~2 GiB per vCPU |
| `r` / `x` / `z` | Memory optimised, ~8 GiB+ per vCPU |
| `i` / `d` | Storage optimised — local NVMe/HDD instance store |
| `g` / `p` / `inf` / `trn` | GPU / ML accelerators |
| **attribute `g`** | **AWS Graviton (ARM64)** — typically ~20% cheaper and better price/performance |
| attribute `a` | AMD processors (cheaper than Intel equivalents) |
| attribute `i` | Intel |
| attribute `n` | Network optimised (higher bandwidth) |
| attribute `d` | Local NVMe instance-store disks attached |
| attribute `b` | Block-storage optimised (higher EBS throughput) |

Sizes scale linearly: `large` = 2 vCPU, `xlarge` = 4, `2xlarge` = 8, and memory doubles with them. So `m6g.2xlarge` reads as "general purpose, 6th gen, Graviton, 8 vCPU / 32 GiB".

**The Graviton point is worth making unprompted for .NET:** .NET has supported ARM64 since .NET 6, so a `t4g`/`m7g` instance is usually a straight ~20% saving for a modern .NET web/API workload with a recompile and no code changes. Naming that turns a generic "pick an instance type" answer into a cost-optimisation answer.

#### User Data — Quick Recall

> **In one line:** a startup script you supply at launch, which the instance runs **once, on first boot, as root** (via cloud-init on Linux / EC2Launch on Windows).
>
> - Limit **16 KB** (base64-encoded when passed to the API). Larger bootstraps should download a script from S3 instead.
> - It does **not** re-run on subsequent reboots unless you explicitly configure it to (`cloud-init-per`, or a `#cloud-config` directive).
> - Debug it at `/var/log/cloud-init-output.log` — the first place to look when "my instance came up but nothing is installed".
> - Senior nuance: heavy user-data bootstrapping is slow and fragile at scale. Bake dependencies into a **custom AMI** (see [AMIs](#amis-amazon-machine-images)) or a container image, and keep user data to config only. **"Golden AMI + thin user data"** is the answer interviewers are listening for.

```bash
#!/bin/bash
yum update -y
yum install -y amazon-cloudwatch-agent
systemctl enable --now amazon-cloudwatch-agent
```

#### User Data — The Full Explanation

**The problem it solves.** A newly launched EC2 instance is a **bare operating system** — nothing installed, nothing configured. Something has to run the setup commands. You *could* SSH in and type them, which is fine for one instance and useless for twenty launched automatically by an Auto Scaling Group at 3 a.m. User data is the script the machine runs **on itself** while booting, so nobody has to log in. If an ASG scales out under load, this is the *only* mechanism by which those new instances configure themselves.

**Where it comes from.** In the console it's literally a text box — *Advanced details → User data* — on the launch screen. In a launch template or Terraform it's the `user_data` field. You put shell commands in; that's all it is. The name is AWS's, meaning "data the *user* supplies to the instance"; **"startup script" is a more honest description.**

**Who runs it.** Most Linux AMIs ship with **cloud-init** pre-installed. During boot it fetches your script and executes it **as `root`** — full administrator, so no `sudo` needed. You never install or invoke cloud-init yourself; it looks for user data automatically. Windows AMIs use **EC2Launch** for the same job.

**The example script, line by line:**

| Line | What it does |
|---|---|
| `#!/bin/bash` | The **shebang** — tells cloud-init "run this with bash." **Omit this first line and the script is silently ignored**, which is a genuinely common mistake |
| `yum update -y` | Updates all installed packages. `yum` is the Amazon Linux/RHEL package manager (Ubuntu uses `apt`). **The `-y` auto-answers every prompt** — essential, because there is no human and no keyboard. A command that stopped to ask "Install 47 packages? [y/n]" would hang forever |
| `yum install -y amazon-cloudwatch-agent` | Installs the CloudWatch agent. Why this specifically? Because EC2 does **not** report memory usage or free disk space to CloudWatch by default — those require an agent running inside the guest OS (see [Container Insights](07-observability-monitoring.md#container-insights-the-cloudwatch-agent--proactive-monitoring)) |
| `systemctl enable --now amazon-cloudwatch-agent` | `systemctl` manages background services. This does **two** things: `enable` = "start automatically on every future boot", `--now` = "and start it right now" |

**Why "runs once" matters more than it sounds.** cloud-init records that it has already run (in `/var/lib/cloud/`), so **on reboot your script does not execute again.** This catches people out constantly:

> *"I put `dotnet MyApp.dll` in user data. It worked. Then the instance rebooted and my app was gone."*

Naturally — the script never ran a second time. **The fix is not to force user data to re-run; it's to install a proper service** so the OS starts your app on every boot. That is precisely what `systemctl enable` does in the example above: the script runs once, and its *effects* persist. Wanting user data to re-run every boot is usually a signal you should have created a systemd service instead.

**The 16 KB limit, and base64.** 16 KB is small — a few hundred lines. base64 is simply an encoding that lets text pass safely through an API; the console and CLI (`--user-data file://script.sh`) handle it for you, so you rarely encode by hand. When your real setup outgrows the limit, keep user data tiny and fetch the rest:

```bash
#!/bin/bash
aws s3 cp s3://my-bucket/bootstrap.sh /tmp/bootstrap.sh
bash /tmp/bootstrap.sh
```
This works with no credentials on the box because the instance's **IAM role** supplies them (see below).

**❗ Why the log matters: a failed user-data script does not fail the instance.** EC2 reports the instance as `running`, status checks pass green — and your software simply isn't there. Nothing surfaces the error. So when someone says *"the instance came up but nothing's installed,"* `/var/log/cloud-init-output.log` (your script's stdout/stderr) is the first place to look, every time.

**Why "golden AMI + thin user data" is the better pattern.** The example script is a fine demo and a poor production practice, for three reasons: it's **slow** (hundreds of MB downloaded on *every* launch, exactly when an ASG is scaling out under load), **not reproducible** (an instance launched today gets different package versions than one launched last month, so your "identical" fleet isn't), and **fragile** (a briefly unreachable package repo leaves the instance booting successfully with software missing, still looking healthy).

So: **bake everything into an AMI once** and keep user data to per-instance configuration only — which environment am I, which cluster do I join. **Full treatment, including the EC2 Image Builder pipeline and the ASG-launch-loop failure mode, is in [The Golden AMI Pattern](#the-golden-ami-pattern--full-explanation).**

#### Instance Metadata (IMDS)

**What it is:** a service every instance can query to learn facts **about itself**, at the special address `http://169.254.169.254/latest/meta-data/`.

That address is **link-local** — it exists only on the machine's own network link, never routes over the internet, and isn't a real server anywhere (the Nitro hypervisor answers it locally). So it works with **no internet access, no VPC routing, and no credentials**.

```bash
curl http://169.254.169.254/latest/meta-data/instance-id
curl http://169.254.169.254/latest/meta-data/placement/availability-zone
curl http://169.254.169.254/latest/meta-data/iam/security-credentials/my-role   # ← temporary IAM credentials
```

**That last one is the important one.** It is how your application obtains its IAM role credentials — the AWS SDK calls this endpoint automatically and refreshes them before they expire. This is the entire mechanism behind "use a role so there are no access keys stored on the instance."

**User data vs instance metadata** — easy to conflate, because user data is *delivered through* the same service (`http://169.254.169.254/latest/user-data`). But they point in opposite directions:

| | **User data** | **Instance metadata** |
|---|---|---|
| Who provides it | **You**, at launch | **AWS**, automatically |
| What it is | Instructions to *configure* the instance | Facts *about* the instance |
| Answers | "Do this on startup" | "Who am I? What are my role's credentials?" |
| Changes later | Only by stopping the instance and editing it | Reflects current reality |

**❗ Always require IMDSv2.** Because IMDSv1 answers a plain `GET`, any **SSRF** bug in your application — where an attacker can make *your server* fetch a URL of their choosing — can be aimed at `169.254.169.254` to retrieve your IAM role credentials, which they then use from their own machine. This is the mechanism behind the Capital One breach. **IMDSv2 requires a `PUT` with a header to obtain a session token first**, which simple SSRF cannot perform. Enforce it with `HttpTokens: required` in the launch template. See [IAM Roles](03-iam-security.md#iam-roles-policies-assumerole) for the full treatment.

---

## 2. EC2 Networking & Interfaces

### Security Groups, Their Properties & Classic Ports

> **SG-to-SG referencing in practice** — why a database ingress rule sourced from a *fleet* SG grants every task on the cluster access, and what to do instead: [ECS → RDS: The Four Layers of a Database Connection](10-databases-caching-analytics.md#ecs--rds-the-four-layers-of-a-database-connection).

**What a security group is:** a stateful virtual firewall attached to an **ENI** (not to an instance — one instance with two ENIs can have different rules per interface).

**Properties to be able to list on demand:**
- **Allow rules only.** You physically cannot write a "deny" rule in a security group. Denying specific IPs is a NACL job.
- **Stateful** — if you allow inbound traffic, the response is automatically allowed back out (and vice versa), regardless of the outbound rules. This is the single biggest difference from NACLs.
- **Default posture:** all inbound **denied**, all outbound **allowed**.
- **Scope:** regional, and locked to one VPC. Can be attached to many instances; one ENI can carry multiple SGs (rules are the **union** — most permissive wins, since there are no denies).
- **Changes apply immediately** — no restart, no re-attach.
- **A SG can reference another SG** instead of a CIDR — the pattern that matters (below).
- The **default** SG of a VPC allows all outbound plus all inbound *from itself*, i.e. instances sharing it can talk freely.

**Reference security groups, not CIDRs** — this is the practical best-practice answer:
```
ALB-SG:  inbound 443 from 0.0.0.0/0
App-SG:  inbound 8080 from ALB-SG      ← not a CIDR
DB-SG:   inbound 5432 from App-SG      ← not a CIDR
```
The app tier is now unreachable except through the ALB, and it keeps working when instance IPs change or the ASG scales — no rule updates ever needed.

**Security Group vs NACL:**
| | Security Group | Network ACL |
|---|---|---|
| Attaches to | ENI / instance | Subnet |
| Rules | **Allow only** | Allow **and** Deny |
| State | **Stateful** (return traffic auto-allowed) | **Stateless** (must allow both directions explicitly) |
| Evaluation | All rules evaluated, union of allows | Rules in **numbered order**, first match wins |
| Typical use | Primary access control | Coarse subnet-level blocking, e.g. blacklisting an IP range |

**The NACL gotcha:** because NACLs are stateless, you must also allow the **ephemeral port range (1024–65535)** for return traffic. "My SG is right but traffic still fails" in a custom-NACL subnet is nearly always this.

**The debugging answer interviewers love:**
- **Connection times out / hangs** → network-layer problem: security group, NACL, route table, or wrong subnet.
- **"Connection refused"** → the network reached the host fine; nothing is listening on that port, i.e. **your application** is down or bound to the wrong interface.

Getting that distinction right in one sentence saves an hour of real debugging and reliably impresses.

**Classic ports to have memorised:**
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

Note 22 vs 21: SSH and SFTP both ride port **22**; plain FTP is 21. That specific pair is a favourite quiz question.

### Public IP vs Private IP vs Elastic IP

| | Private IP | Public IP | Elastic IP |
|---|---|---|---|
| Reachable from | Inside the VPC only | Internet | Internet |
| Assigned by | VPC subnet CIDR | AWS pool, automatically | You, and it's yours until released |
| **Survives stop/start?** | **Yes** | **No — you get a different one** | **Yes** |
| Cost | Free | Charged per hour for public IPv4 (since Feb 2024, whether attached or not) | Charged per hour, **and charged extra when *not* attached** to a running instance |
| Movable | No | No | Yes — remap to another instance/ENI in seconds |

**The classic gotcha:** a public IP is released on stop and a **new one is assigned on start**. Anything hardcoded to it — DNS records, firewall allowlists at a partner, a config file — breaks. This is the #1 "why did my integration stop working after maintenance?" scenario.

**Elastic IP** solves it, but the senior answer is *don't reach for one*. AWS gives you only ~5 per region by default precisely because they're a design smell. Prefer:
- a **load balancer** in front (DNS name stays stable, instances behind it are disposable),
- **Route 53** with an alias record,
- and for outbound-only needs, a **NAT Gateway** (which has its own stable EIP).

Legitimate EIP uses: a partner firewall that can only allowlist a fixed IP, a NAT gateway, or a fast manual failover where you remap the address to a standby instance.

### Placement Groups

How you ask EC2 to influence *where* instances physically land. You choose the strategy; AWS does the placement.

| Strategy | Placement | Trade-off | Use for |
|---|---|---|---|
| **Cluster** | Packed onto the same rack in a **single AZ** | Best network: low latency, high per-flow throughput (10+ Gbps). **Worst blast radius — one rack failure takes everything** | HPC, tightly-coupled compute, big-data jobs that need fast node-to-node chatter |
| **Spread** | Each instance on **distinct underlying hardware**, across AZs | Maximum isolation. Hard limit of **7 running instances per AZ per group** | Small numbers of critical instances that must never fail together (e.g. a 3-node quorum) |
| **Partition** | Instances grouped into **partitions**, each partition on its own set of racks; up to **7 partitions per AZ** | Isolation between partitions, hundreds of instances | HDFS/Hadoop, **Kafka**, Cassandra — anything that is itself rack-aware and replicates across partitions |

**How to answer well:** name the trade-off, not just the definition. "Cluster buys you network performance at the cost of correlated failure; spread buys you isolation but caps you at 7 per AZ; partition is the middle ground for distributed systems that already understand replica placement." Partition groups also expose the partition number to the instance, which is how Kafka/Cassandra place replicas correctly.

### Elastic Network Interfaces (ENIs)

A virtual network card. Each ENI carries: one primary private IPv4 + optional secondary private IPs, one Elastic IP per private IP, a MAC address, one or more security groups, and a source/destination check flag.

Facts that get asked:
- An ENI is **bound to one AZ** and cannot be moved to another AZ (it belongs to a subnet).
- The **primary ENI (`eth0`) cannot be detached** from an instance. Secondary ENIs can be detached and attached to a different instance **in the same AZ** — which is the cheap failover trick: move the ENI (and its IP, MAC, and SGs) to a standby instance and traffic follows it.
- How many ENIs and IPs you get is **capped by instance type** — a small instance can't host many. This matters for container density.
- Disable the **source/destination check** when the instance must forward traffic it didn't originate (a NAT instance, or a software router/firewall appliance).
- **Lambda in a VPC** creates ENIs to reach private resources — the reason VPC-attached Lambdas historically had worse cold starts, and why you spread the Lambda's subnets across AZs (see the [IAM Pitfalls](03-iam-security.md#iam-pitfalls) table).

---

## 3. Lifecycle, Purchasing & Sizing

### EC2 Hibernate

**What it does:** dumps the instance's **RAM to the encrypted root EBS volume** and shuts down. On start, the RAM is restored and processes resume where they left off — no boot, no application warm-up, same instance ID and private IP.

| | Stop | Hibernate | Terminate |
|---|---|---|---|
| RAM contents | Lost | **Preserved on the root EBS volume** | Lost |
| Boot on restart | Full OS boot | Resumes from memory image | N/A |
| Root EBS volume | Kept | Kept (and holds the RAM dump) | Deleted by default |
| Instance ID / private IP | Kept | Kept | Gone |
| Compute charges | Stopped | Stopped (you still pay for the larger EBS) | Stopped |

**Requirements** (the list that makes hibernate fail in practice): the root volume must be **EBS, encrypted, and large enough to hold the whole RAM image**; the instance must be a supported family/size with **RAM under 150 GiB**; hibernation must be enabled **at launch** (you cannot turn it on later); instance-store root volumes are not supported; and an instance can stay hibernated for a maximum of **60 days**.

**When it's the right answer:** long-initialising applications (a service that spends minutes loading a model or warming a cache), licence-server or dev boxes you want back instantly, and anything where cold-start time is the real cost. Not a substitute for an ASG — it's a single-instance optimisation.

### EC2 Purchasing Options — The Complete Set

The pricing table above covers the four everyone names. These are the ones that separate a complete answer from a partial one:

| Option | What you're buying | Key detail |
|---|---|---|
| **On-Demand** | Pay per second/hour, no commitment | Highest rate, zero risk. Correct default for a new workload with unknown steady state |
| **Reserved Instances** | 1 or 3-year commitment to a specific config | **Standard RI** = biggest discount, locked to instance family; **Convertible RI** = smaller discount, can exchange family/OS/tenancy. Can be sold on the RI Marketplace (root-only action) |
| **Savings Plans** | 1 or 3-year commitment to a **$/hour spend** | **Compute SP** spans EC2 + Fargate + Lambda and any family/region (most flexible); **EC2 Instance SP** is deeper discount but pinned to a family+region |
| **Spot Instances** | Spare capacity at up to ~90% off | Reclaimed with a **2-minute** interruption notice. Needs checkpointing/idempotent work. Spot *Fleet* blends Spot+On-Demand across pools to reduce interruption risk |
| **Dedicated Instances** | Your instances run on hardware **not shared with other AWS customers** | You don't see or control the host. Instances may land on different hosts after stop/start |
| **Dedicated Hosts** | A whole **physical server** reserved for you, with visibility into sockets/cores | The answer for **BYOL socket/core-based licensing** (Windows Server, SQL Server, Oracle) and hard compliance mandates. Most expensive |
| **Capacity Reservations** | Reserved **capacity** in a specific AZ, held whether or not you use it | Pay On-Demand rates — **no discount and no term commitment**. This is about *guaranteed availability*, not price (DR standby, a known launch event). Combine with a Savings Plan to also get the discount |

**Dedicated Instances vs Dedicated Hosts** is the classic confusion pair: both give you single-tenant hardware, but only a **Dedicated Host** exposes the physical server (sockets, cores, host affinity) — which is exactly what per-socket/per-core BYOL licensing requires. If the question mentions bringing your own Windows/SQL Server/Oracle licence, the answer is Dedicated Hosts.

**Capacity Reservation vs Reserved Instance** is the other one: an RI/Savings Plan is a **billing** construct (discount, no capacity guarantee); a Capacity Reservation is a **capacity** construct (guarantee, no discount). Saying that cleanly is a strong signal.

### EC2 Shared Responsibility Model

EC2 sits far to the "your job" end of the spectrum compared with Lambda or S3 — that contrast *is* the answer.

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

**One-liner:** "For EC2, AWS is responsible for security *of* the host and everything under the hypervisor; from the guest OS upward — patching, firewall rules, keys, IAM, encryption, backups — it's mine. Lambda moves most of that line onto AWS; EC2 keeps it on me."

### EC2 Sizing, Pricing Decisions & CPU Credit Gotchas

The existing EC2 section covers the pricing model *names* but not how a senior engineer actually reasons about **sizing** a box or **choosing** between the purchase options for a real workload — and it's missing the T-family CPU credit gotcha, which is one of the most commonly asked "explain a production incident" EC2 questions.

**How to actually pick an instance size (not just a family)**

Sizing is a vCPU-to-memory *ratio* decision, not a "pick the biggest one that fits the budget" decision:
- Start from the **workload shape**: CPU-bound (video encoding, compilation, hashing) → `c`-family (2 GiB RAM per vCPU); memory-bound (in-memory caches, large JVM/.NET heaps, big EF Core result sets) → `r`-family (8 GiB per vCPU); general-purpose web/API tier with no strong lean either way → `m`-family (4 GiB per vCPU); dev/test, low/bursty CPU with idle troughs → `t`-family (also ~4 GiB per vCPU, but *burstable*, see below).
- Benchmark before committing: use CloudWatch `CPUUtilization`, memory (via CloudWatch Agent — memory isn't a default EC2 metric), and network metrics under real/representative load, then size to a target steady-state utilization of roughly 40–60% average — leaving headroom for spikes without being so oversized that you're paying for idle capacity.
- AWS Compute Optimizer will do this analysis for you from actual usage history and recommend a right-sized family/size — worth name-dropping as the "don't guess, measure" answer.
- Vertical (bigger instance) vs horizontal (more instances behind an ALB/ASG) scaling trade-off: horizontal scaling is almost always preferred for stateless web/API tiers (better fault tolerance, finer-grained cost control, supports rolling deploys); vertical scaling is sometimes unavoidable for workloads that can't be distributed (a single large in-memory cache node, some legacy monoliths).

**The T-family CPU credit gotcha (a favorite interview trap)**

Burstable (`t3`, `t4g`, `t2`) instances are cheap because they're provisioned with a *baseline* CPU performance (e.g., 20–40% of a full core, family/size-dependent) and earn **CPU credits** while running below that baseline. Credits are spent to burst above baseline when needed.

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

- **Standard mode**: once the credit balance is exhausted, CPU is hard-throttled back down to the baseline percentage — this is the classic "app was fine for hours, then suddenly became sluggish/unresponsive for no obvious reason" production incident. The root cause is almost always a sustained load period (batch job, traffic spike, backup/reindex) that outlasted the accumulated credit balance.
- **Unlimited mode**: bursts are allowed to continue indefinitely, but AWS bills you extra (per vCPU-hour) for sustained usage beyond baseline — protects availability but can produce a cost surprise if a T-instance is quietly running hot 24/7 (a sign you've outgrown the T-family and should move to `m`/`c`).
- **Diagnosis in an interview scenario:** "Our T3 instance got slow under sustained load, but CPUUtilization graphs don't look pegged at 100%" → check the `CPUCreditBalance` and `CPUSurplusCreditBalance` CloudWatch metrics, not just `CPUUtilization` — a throttled T-instance can show CPU capped well below 100% because it's being held at baseline, which looks deceptively "healthy" unless you know to look at credits specifically.
- **Rule of thumb:** T-family is right for workloads with genuine idle troughs (dev/test, low-traffic APIs, bursty-but-brief admin jobs); it is the *wrong* choice for anything with a sustained high-CPU period (nightly batch processing, backup windows, steady-state compute-heavy services) — those belong on `m`/`c`/`r` family instances with no credit mechanism to run out of.

**On-Demand vs Reserved vs Spot vs Savings Plans — how you actually decide (not just what they are)**

The Cost Optimization section later in this guide already tables out the discount percentages; the senior-level skill being tested here is the **decision process**, not the definitions:

| Question to ask yourself | Points toward |
|---|---|
| Is this workload's capacity need predictable 12+ months out? | Savings Plan / Reserved Instance |
| Could this workload be interrupted with ~2 minutes' notice without breaking correctness? | Spot |
| Is this a brand-new workload with unknown steady-state yet? | On-Demand first, commit later once usage data exists |
| Does the workload span multiple compute types (EC2 + Fargate + Lambda)? | Compute Savings Plan (flexible across compute types) over EC2 Instance Savings Plan/RI |
| Is this dev/test that's only running business hours? | On-Demand + scheduled stop/start (Instance Scheduler) — commitment-based discounts don't help if the instance isn't running most of the time anyway |
| Is the workload stateful with no cheap way to checkpoint/resume? | Avoid Spot — the 2-minute reclaim notice isn't enough to gracefully drain long-lived state |

**Interview-ready answer:** "I wouldn't jump straight to 'buy Savings Plans' — I'd first confirm the workload is stable and predictable enough to commit to, check whether it can tolerate interruption (Spot candidate), and only then choose between a Compute Savings Plan (flexible, spans EC2/Fargate/Lambda) versus an EC2/Instance Savings Plan (deeper discount, less flexible) based on how confident I am in the instance family staying fixed."

**When is EC2 the right compute choice vs Lambda/Fargate — the decision an interviewer actually wants**

This guide already has a Lambda-vs-ECS-vs-EC2-vs-Fargate comparison table and a dedicated .NET compute-decision section — the angle worth adding here is how to *justify* EC2 specifically when you're the one who provisioned it via Terraform/CDKTF rather than picking a serverless default:

- **Full OS/kernel access is a hard requirement** — custom kernel modules, specific driver versions, GPU workloads, or software that assumes it owns the host (some legacy .NET Framework/COM-interop scenarios) — Fargate and Lambda both abstract the OS away, which is a feature until it's a blocker.
- **Steady, high, predictable utilization** — if a service runs at 70–90% CPU 24/7, EC2 (especially with a Savings Plan) is usually the cheapest option; Fargate's per-task premium and Lambda's per-invocation billing both lose that comparison once utilization is consistently high.
- **You already have the IaC investment** — "I'm provisioning EC2 via Terraform/CDKTF already, so the operational tooling (state, modules, CI plan/apply pipeline) is a sunk cost that makes EC2 marginally cheaper to *operate*, not just to run" is a legitimate, honest talking point — but it should never be the *primary* justification. An interviewer will push back on "because that's what I already know" as the main reason, and rightly so — lead with the workload-shape argument (steady utilization, OS-level requirement) and mention existing Terraform tooling as a secondary, practical factor.
- **Long-running processes with in-memory state that can't be trivially externalized** — e.g., a stateful cache or a process holding a large warmed-up in-memory model — favors EC2 over Lambda's stateless-between-invocations model, though ECS/Fargate with EFS or a sticky task can sometimes also satisfy this.
- **Counter-signal to watch for:** if you find yourself justifying EC2 purely on "it's simpler to reason about" or "I don't want to learn containers," that's a comfort-driven answer, not a workload-driven one — senior interviewers are specifically listening for whether you separate "what I know" from "what the workload needs."

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

**What EBS actually is:** a **network drive**, not a physical disk. That single fact explains most of its behaviour — there's network latency, it can be detached and reattached, and it survives the instance.

**Properties that get asked:**
- **Locked to one Availability Zone.** An `us-east-1a` volume cannot attach to an `us-east-1b` instance. To move it you snapshot it and create a new volume in the target AZ — that's *the* answer to "how do I move a volume across AZs/regions?"
- **One instance at a time** (except Multi-Attach, below).
- **Provisioned capacity** — you pay for the GB and IOPS you *provision*, not what you use. A 1 TB volume holding 4 GB costs the same as a full one.
- **Can be resized online** (Elastic Volumes) — grow size, change type, change IOPS with no downtime. You **cannot shrink** a volume; you create a smaller one and copy.
- **`DeleteOnTermination`** defaults to **true for the root volume** and **false for additional volumes**. This asymmetry is a real interview question and a real production incident: terminate an instance and the root volume (with your logs) is gone, while orphaned data volumes quietly accrue cost forever.
- Volumes persist independently of the instance — detach, reattach elsewhere in the same AZ, and the data is intact.

```bash
aws ec2 create-volume --availability-zone us-east-1a --size 100 --volume-type gp3 --encrypted
aws ec2 attach-volume --volume-id vol-abc --instance-id i-abc --device /dev/sdf
# on the instance: format (first time only!) and mount
lsblk                                   # confirm the device is visible
sudo mkfs -t xfs /dev/nvme1n1           # ⚠ destroys data — never run on a volume with data
sudo mkdir /data && sudo mount /dev/nvme1n1 /data
# add to /etc/fstab (by UUID, not device name) so it survives a reboot
```
**Two hands-on gotchas worth naming:** device names get remapped (you ask for `/dev/sdf`, Nitro shows `/dev/nvme1n1`), and if you forget the `/etc/fstab` entry the mount silently disappears on the next reboot. Both are explained below, because both are real production incidents rather than trivia.

**Gotcha 1 — the device name you request is not the name Linux uses.** You attach with `--device /dev/sdf`, but on any Nitro instance (M5/C5/T3/R5 and newer) disks are NVMe, so Linux calls it `/dev/nvme1n1`. `/dev/sdf` is only a label AWS records in its own console, and `mkfs -t xfs /dev/sdf` fails with "No such file or directory".

So **always run `lsblk` first** and identify the volume by size + "no partitions, no mountpoint":
```
NAME          SIZE TYPE MOUNTPOINTS
nvme0n1         8G disk
└─nvme0n1p1     8G part /          <- root volume, already mounted
nvme1n1       100G disk            <- the volume I just attached
```
Why this deserves care rather than a guess: **running `mkfs` on the wrong device formats your root volume and destroys the instance.** And NVMe numbering isn't stable — with several volumes attached, today's `nvme1n1` can be `nvme2n1` after a reboot, so don't hardcode NVMe names either. To map a device back to a real volume ID:
```bash
sudo nvme id-ctrl -v /dev/nvme1n1 | grep -i sn    # serial = the EBS volume ID (vol-0abc…)
ls -l /dev/disk/by-id/                            # stable nvme-Amazon_Elastic_Block_Store_vol… symlinks
```

**Gotcha 2 — `mount` is temporary; without `/etc/fstab` it vanishes on reboot.** `mount` only changes the running system, held in memory. After a reboot `/data` is once again an ordinary empty folder on the **root** volume, and three things go wrong at once:
1. The application keeps writing to `/data` — now filling the small **root** disk instead of the 100 GB data volume.
2. The existing data looks like it disappeared (it's safe on the EBS volume, just not attached to that path).
3. **The nasty one:** when you later remount the volume onto `/data`, anything written to the root-disk `/data` becomes **invisible** — mounting over a directory hides its contents. Now your data is split across two places and half of it is hidden.

The fix, using the filesystem **UUID** (stored inside the filesystem, so it survives device renaming):
```bash
sudo blkid /dev/nvme1n1        # -> UUID="a1b2c3d4-…" TYPE="xfs"
# /etc/fstab
# UUID=a1b2c3d4-…  /data  xfs  defaults,nofail  0  2
sudo umount /data && sudo mount -a && df -h /data   # ← TEST before rebooting
```
**`nofail` is not optional:** without it, an instance whose volume is missing at boot can drop into emergency mode and you **lose SSH access entirely** — the classic "I edited fstab and now I can't get into my server," which needs a rescue-instance procedure to undo. And `mount -a` is the step everyone skips: if it succeeds, the reboot will too. Discovering a broken fstab *by rebooting* is how people lock themselves out.

**The one-line version:** "Ask for `/dev/sdf`, but always `lsblk` to find the real NVMe name — and a `mount` with no `/etc/fstab` entry (by UUID, with `nofail`) silently disappears on the next reboot."

### EBS Volume Types

| Type | Class | Performance | Boot volume? | Use for |
|---|---|---|---|---|
| **gp3** | SSD, general purpose | Baseline **3,000 IOPS / 125 MB/s regardless of size**, up to 16,000 IOPS / 1,000 MB/s — IOPS provisioned **independently of capacity** | ✅ | **The default choice.** ~20% cheaper than gp2 |
| gp2 | SSD, general purpose | 3 IOPS per GB (so you had to over-provision *size* just to get IOPS), max 16,000 | ✅ | Legacy — migrate to gp3 |
| **io2 Block Express** | SSD, provisioned IOPS | Up to 256,000 IOPS, sub-millisecond latency, **99.999% durability** | ✅ | Mission-critical databases, large SQL Server/Oracle workloads |
| io1 | SSD, provisioned IOPS | Up to 64,000 IOPS | ✅ | Older generation of the above |
| st1 | **HDD**, throughput optimised | Up to 500 MB/s, low IOPS | ❌ | Big sequential reads: log processing, data warehouse, ETL |
| sc1 | **HDD**, cold | Up to 250 MB/s, cheapest per GB | ❌ | Infrequently accessed archive data that must still be a filesystem |

Two facts that are almost always the question: **only SSD types (gp2/gp3/io1/io2) can be boot volumes** — HDD types cannot. And **gp3's headline improvement is decoupling IOPS from size**: under gp2, needing 6,000 IOPS forced you to provision a 2 TB volume you didn't need. Being able to say that is the difference between naming the types and understanding them.

### EBS Snapshots

**What they are:** point-in-time backups of a volume, stored in **S3 managed by AWS** (not in a bucket you can see), and **incremental** — the first snapshot copies every used block, later ones copy only changed blocks. Deleting an old snapshot never breaks a newer one; AWS keeps whatever blocks are still referenced.

Key behaviours:
- You **can** snapshot an attached, running volume, but for a database you should quiesce/flush first (or use the DB's own backup) — otherwise you get a crash-consistent, not application-consistent, image.
- A snapshot is **region-scoped but AZ-independent**: restore it into **any AZ**, or **copy it to another region** — that copy is the standard EBS disaster-recovery move.
- **Fast Snapshot Restore (FSR)** removes the lazy-loading penalty (a fresh volume from a snapshot is normally slow on first touch of each block). Costs extra per snapshot per AZ; enable it only for snapshots you restore under time pressure.
- **Snapshot Archive** tier is ~75% cheaper but takes **24–72 hours** to restore — for compliance retention, never for recovery.
- **Recycle Bin** lets you set a retention rule so deleted snapshots/AMIs can be recovered — the guard against a fat-fingered or malicious deletion.
- **Data Lifecycle Manager (DLM)** automates snapshot creation/retention on a schedule, and it's the correct answer to "how do you back up EBS?" (rather than a cron job calling the CLI). AWS Backup is the bigger, cross-service version.

```bash
aws ec2 create-snapshot --volume-id vol-abc --description "pre-upgrade 2026-08-08"
aws ec2 copy-snapshot --source-region us-east-1 --source-snapshot-id snap-abc \
  --destination-region us-west-2 --encrypted          # DR copy
aws ec2 create-volume --snapshot-id snap-abc --availability-zone us-east-1b --volume-type gp3
```
**Cost leak to mention:** snapshots are the most commonly forgotten AWS charge — years of nightly snapshots from decommissioned volumes. DLM retention rules, or a Config rule, is how you stop it.

### AMIs (Amazon Machine Images)

**What an AMI is:** a launch template for a *machine* — snapshots of the root (and any additional) volumes, plus the block-device mapping and launch permissions. An EBS snapshot backs up a **disk**; an AMI backs up a **bootable machine**. That's the distinction interviewers probe.

- **AMIs are region-scoped.** You must **copy** an AMI to every region you launch in — the reason a multi-region deployment pipeline has an AMI-copy step.
- Types: AWS-provided (Amazon Linux, Windows Server), **Marketplace** (may carry a licence charge), and **your own custom AMIs**.
- **Creating one reboots the instance by default** so the filesystem is consistent. `--no-reboot` avoids downtime but risks a corrupt/inconsistent image — only safe if the app is quiesced.
- **The Golden AMI pattern:** pre-bake OS patches, the runtime (e.g. the .NET runtime), agents (CloudWatch, SSM), and your app's dependencies into an AMI, then keep user data thin. Faster, more reliable ASG scale-out than installing everything at boot. **EC2 Image Builder** automates building, patching, testing, and distributing these on a schedule.
- **Deregistering an AMI does not delete its snapshots** — another silent cost leak, and a good detail to know.

```bash
aws ec2 create-image --instance-id i-abc --name "dotnet8-base-2026-08" --description "golden AMI"
aws ec2 copy-image --source-region us-east-1 --source-image-id ami-abc --region us-west-2 --name "dotnet8-base"
```

#### The Golden AMI Pattern — Full Explanation

**The idea:** install everything **once, when you build the image** — not every time an instance launches. "Baking" means booting a machine, installing everything, and snapshotting it into a reusable AMI. Like corporate IT handing you a pre-imaged laptop rather than a blank one plus an install checklist.

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

**Three reasons those minutes matter** (the second and third are the ones that separate a good answer from a generic one):
1. **Auto Scaling is only useful if it's fast.** A 9 a.m. spike fires the alarm, the ASG launches 5 instances — and with thick user data they're useless for 5–6 minutes, meaning 5–6 minutes of your *existing* instances serving timeouts. Slow launches mean you're always scaling for the traffic you had several minutes ago.
2. **Your fleet isn't actually identical.** `yum update -y` pulls whatever is current *at that moment*, so a January instance has different package versions than a March one from the same config — producing the worst kind of bug report: *"it only fails on some instances."*
3. **❗ A network blip becomes an ASG launch loop.** If an external repo is unreachable for 20 seconds the install fails, the script exits, and **the instance still boots "successfully"** — just with no runtime. Then: app never starts → ALB health check fails → ASG terminates it → launches a replacement → same failure → **repeat forever**, with zero healthy capacity. Every external dependency in your boot path is a chance for this, and baking removes them all.

**What to bake, and why each one:**
| Bake in | Why |
|---|---|
| **OS patches** | Avoids a 200 MB download per launch, and makes the patch level *known* rather than "whatever was current" |
| **Runtime** (.NET 8) | The largest install, and it depends on an external repo |
| **CloudWatch agent** | EC2 reports no memory or disk-space metrics without it |
| **SSM agent** | Enables Session Manager (shell with no SSH keys) and Patch Manager |
| **App dependencies** | Native libs, fonts, certificates — usually not the app itself |

**"Thin user data"** then means only what genuinely differs per instance or environment: which environment, which cluster to join, which config to fetch. **Config, not installation.**

**EC2 Image Builder** turns the manual launch → install → snapshot cycle into a pipeline:
```
1. RECIPE       base AMI + components: patch OS - install runtime - install agents - CIS hardening
2. BUILD        spins up a temp instance, runs the components, snapshots it
3. TEST         boots the new AMI and runs smoke tests — fails the pipeline if broken
4. DISTRIBUTE   copies the AMI to every region, shares it to every account
5. SCHEDULE     re-runs monthly, or on a critical CVE, so patches actually land
```
The **test phase** is the underrated part — it stops a broken image reaching your launch template — and it integrates with **Amazon Inspector** to scan for CVEs before release. **Packer** is the alternative and fits naturally alongside Terraform/CDKTF as the same toolchain, working across clouds.

**The mindset shift:** you stop patching servers and start **replacing** them.
```
Image Builder produces AMI v43 -> update the launch template ->
ASG instance refresh (rolling replacement) -> deregister old AMIs AND delete their snapshots
```
That's **immutable infrastructure**: patching in place creates snowflakes that drift apart, whereas rebuilding the image and rolling the fleet keeps every instance identical.

**Honest trade-offs:** the build pipeline is real infrastructure to maintain; AMI sprawl and orphaned snapshots cost money if you don't clean up; and iteration is slower, since changing one dependency means rebuilding an image — which is why many teams use thick user data in dev and golden AMIs in prod. **The container parallel:** a Docker image *is* a golden AMI for containers — same philosophy, much faster builds — which is why the pattern is stated as "custom AMI **or container image**". On ECS/Fargate you get it for free.

**The interview answer:** *"I'd bake a golden AMI with the patched OS, runtime, and agents via EC2 Image Builder on a monthly schedule, and keep user data to per-instance config only. It cuts ASG scale-out from ~5 minutes to ~90 seconds, guarantees identical instances, and removes external repos from the boot path — because a repo being briefly unreachable otherwise gives you an instance that boots healthy with no runtime, which becomes an ASG launch loop. Patching then becomes rebuild-and-roll via instance refresh rather than patching in place."* What's being tested is whether you see **launch time as an availability concern** and think in immutable-infrastructure terms.

### Instance Store

**Physically attached NVMe disks on the host** — not network storage. That gives it the highest possible IOPS (millions) and lowest latency of any EC2 storage, and also its one defining limitation:

- **Ephemeral.** Data is lost when the instance **stops, hibernates, terminates**, or the underlying host fails. It **does** survive a reboot.
- Cannot be snapshotted, resized, or detached/reattached. Capacity is fixed by instance type (the `d`/`i` families).
- You are responsible for replication and durability — full stop.

**Correct uses:** scratch space, temp files, buffers, caches, and distributed databases that already replicate across nodes (Cassandra, Elasticsearch/OpenSearch data nodes). **Wrong use:** anything you can't rebuild from another source.

**The one-liner:** "Instance store is the fastest and least durable option — EBS is a network drive that outlives the instance, instance store is local hardware that doesn't."

### EBS Multi-Attach

Lets a **single io1/io2** volume attach to **up to 16 Nitro instances in the same AZ** simultaneously, each with full read/write access.

The point everyone misses: **a normal filesystem (ext4, XFS, NTFS) will corrupt itself** if two instances mount it at once, because each caches metadata independently. Multi-Attach only works with a **cluster-aware filesystem** (GFS2, OCFS2) or an application that manages raw block access and its own locking. So it is *not* the shared-storage answer for a general web fleet — that's **EFS**. Multi-Attach exists for clustered HA applications that need concurrent raw block access, such as Oracle RAC-style setups.

Constraints: io1/io2 only, one AZ only, 16 instances max, Nitro instances only.

### EBS Encryption

Encryption at rest via **KMS (AES-256)**, and it covers more than people expect:
- Data at rest on the volume
- **Data in transit between the instance and the volume**
- All **snapshots** created from the volume
- All **volumes created from those snapshots** (encryption propagates automatically)

Performance impact is negligible (handled by the Nitro hardware), so there is no good reason to leave it off. Turn on **EBS encryption by default** at the account/region level so nobody has to remember.

**How to encrypt an existing unencrypted volume** — this exact procedure is a common question, because you cannot flip encryption on in place:
1. Snapshot the unencrypted volume.
2. **Copy** the snapshot, specifying `--encrypted` and a KMS key (the copy step is where encryption is introduced).
3. Create a new volume from the encrypted snapshot.
4. Stop the instance, detach the old volume, attach the new one at the same device name, start.

Sharing note: you can share an unencrypted snapshot publicly, but a snapshot encrypted with the **default AWS-managed key cannot be shared at all** — you must use a **customer-managed KMS key** and grant the other account access to it. That's the gotcha behind "why can't the other account use my snapshot?"

### EFS (Elastic File System)

**Managed NFS** that many instances across **multiple AZs** can mount **at the same time** — the shared-filesystem answer, where EBS is the single-attach answer.

- **POSIX/NFSv4.1 — Linux only.** ❗ **EFS does not support Windows.** For Windows workloads you use **Amazon FSx for Windows File Server** (SMB, Active Directory integrated). For high-performance Linux/HPC there's **FSx for Lustre**. Naming FSx when someone asks about shared storage for a Windows .NET Framework app is a strong differentiator.
- **Truly elastic** — grows and shrinks automatically to petabytes; you **pay per GB actually stored**, with no provisioning. But it costs roughly **3× gp3 per GB**, so it isn't a default — use it when you genuinely need concurrent shared access.
- Access is via a **mount target per AZ**, each with a security group. It must allow inbound **NFS port 2049** from the client SG — that's the #1 "EFS mount hangs" cause.
- **Performance modes:** *General Purpose* (default, lowest latency) vs *Max I/O* (higher throughput/parallelism, slightly higher latency, for thousands of clients).
- **Throughput modes:** *Bursting* (scales with size, can exhaust credits — same trap shape as T-family CPU credits), *Provisioned* (fixed, for small-but-busy filesystems), *Elastic* (auto, best default for spiky/unknown workloads).
- **Storage classes:** Standard, **One Zone** (~47% cheaper, single-AZ — fine for dev, not for production HA), plus Infrequent Access and Archive tiers, with **lifecycle management** to move files automatically after N days without access.
- Encryption at rest via KMS, in transit via TLS.

```bash
sudo mount -t efs -o tls fs-0123456789abcdef:/ /mnt/efs      # amazon-efs-utils, TLS in transit
```
**Typical uses:** shared uploads/content across an ECS or EC2 fleet, CMS (WordPress) document roots, shared config, CI build caches, and **Lambda file storage** for functions needing more than the 512 MB `/tmp` or shared state between invocations.

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

**The decision sentence:** "One instance needs a fast disk → EBS. Many instances need the same files at once → EFS (or FSx on Windows). I need maximum speed and can rebuild the data → instance store."

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
| **What's actually at risk** | **Instance store data (unrecoverable — gone on stop, not just terminate)**, EBS volumes, machine configuration |
| **Backup mechanism** | **EBS snapshots** (incremental, stored in S3), **Data Lifecycle Manager** for scheduled snapshots + retention, **AMIs** for whole-machine recovery, cross-region snapshot/AMI copy |
| **Realistic RPO / RTO** | RPO = your DLM schedule (typically hours). RTO minutes to launch from an AMI |

**Recovery runbook:**
1. **Instance failure:** launch from the last **golden AMI** via the launch template — if it's behind an ASG this is automatic and there's nothing to do.
2. **Volume corruption:** `aws ec2 create-volume --snapshot-id snap-xxx --availability-zone <az>`, then detach the bad volume and attach the new one at the same device name.
3. **Regional failure:** launch from the **AMI you copied to the DR region in advance** (`aws ec2 copy-image`), then reattach/reallocate the Elastic IP and update the Route 53 record.
4. Confirm the **ASG launch template references the DR-region AMI ID** — AMI IDs are region-specific, so a copied launch template points at an AMI that doesn't exist there.

⚠️ **The gotcha:** **snapshots and AMIs are regional**, so an un-copied snapshot is worthless in exactly the scenario you took it for. Copy AMIs to the DR region **as a build step**, not at incident time. And say the instance-store point plainly: it is **never** backed up and a simple *stop* loses it — so nothing that matters is allowed to live there.

---

← [S3](05-s3.md) · [Index](README.md) · [Observability & Monitoring](07-observability-monitoring.md) →
