> **AWS Quick Revision Notes** · [Index](README.md) · Part I

# EC2 & Instance Storage

---

## 1. EC2 Fundamentals

### Fundamentals

Instance = VM; AMI = template. Families: `t` burstable, `m` balanced, `c` compute, `r` memory, `g`/`p` GPU, `i`/`d` storage. Storage: EBS (persistent, network) vs Instance Store (ephemeral, physical). SG stateful. **Stop** = EBS persists, ID kept, stop paying compute (still EBS); **Terminate** = instance + root EBS (by default) destroyed.
Pricing: On-Demand (highest, no risk) · Reserved (1–3y, locked) · Savings Plans (1–3y $/hr, flexible) · Spot (~90% off, 2-min reclaim).
Choose EC2: full OS control, custom kernel, legacy, stateful, GPU, steady utilization. Limits: you patch/scale, idle costs.

### Instance Types, User Data & Metadata

Decode `m5dn.2xlarge`: family/generation/attributes/size. Attributes: `g`=**Graviton ARM64 (~20% cheaper)**, `a`=AMD, `n`=network, `d`=local NVMe, `b`=block-optimized. Sizes linear (large=2 vCPU, 2xlarge=8). **.NET supports ARM64 since .NET 6 → `t4g`/`m7g` ~20% saving on recompile.**

**User Data:** startup script run **once, first boot, as root** (cloud-init Linux / EC2Launch Windows). Limit **16 KB** (base64). Doesn't re-run on reboot. Debug: `/var/log/cloud-init-output.log`. Senior: **golden AMI + thin user data**.
```bash
#!/bin/bash          # omit shebang → silently ignored
yum update -y        # -y auto-answers (no keyboard)
yum install -y amazon-cloudwatch-agent   # memory/disk not default metrics
systemctl enable --now amazon-cloudwatch-agent  # enable=on-boot, --now=start now
```
"App gone after reboot" → script ran once; fix = a systemd **service**, not re-running user data. >16 KB → fetch script from S3 (instance role supplies creds). **❗ Failed user-data doesn't fail the instance** (status green, software missing) → check the log.

**IMDS:** `http://169.254.169.254/latest/meta-data/` — link-local, no internet/routing/creds. `.../iam/security-credentials/my-role` = how SDK gets role creds (auto-refreshed). User data (you, config, changes only by stop+edit) vs metadata (AWS, facts, current). **❗ Require IMDSv2** (`HttpTokens: required`) — v1 GET → SSRF steals creds (Capital One).

---

## 2. EC2 Networking & Interfaces

### Security Groups & Ports

Stateful virtual firewall on an **ENI** (not instance). **Allow-only** (deny = NACL); **stateful** (return auto-allowed); default deny inbound/allow outbound; regional/one VPC; multiple SGs = union; changes immediate; **can reference another SG** (best practice).
```
ALB-SG: 443 from 0.0.0.0/0 · App-SG: 8080 from ALB-SG · DB-SG: 5432 from App-SG
```
| | SG | NACL |
|---|---|---|
| Attaches | ENI/instance | Subnet |
| Rules | Allow only | Allow + Deny |
| State | Stateful | **Stateless** (allow both directions) |
| Eval | Union of allows | Numbered order, first match |

**NACL gotcha:** stateless → must allow **ephemeral 1024–65535** for return. **Debug rule:** timeout/hang = network (SG/NACL/route/subnet); **"connection refused"** = reached host, app down / wrong port.
**Ports:** 22 SSH/SFTP, 21/20 FTP, 3389 RDP, 80/443, 53 DNS, 25/587/465 SMTP, **3306 MySQL/Aurora-MySQL, 5432 PostgreSQL/Aurora-PG/Redshift, 1433 SQL Server**, 1521 Oracle, 6379/11211 Redis/Memcached, 27017 Mongo/DocDB, **2049 NFS/EFS**. (22 vs 21: SSH+SFTP both 22.)

### Public/Private/Elastic IP

| | Private | Public | Elastic |
|---|---|---|---|
| Reach | VPC only | Internet | Internet |
| Survives stop/start? | Yes | **No (new one)** | Yes |
| Cost | Free | Per-hour (IPv4 since Feb 2024) | Per-hour + extra when unattached |

**Gotcha:** public IP changes on stop/start → breaks hardcoded DNS/allowlists. EIP solves it but ~5/region (design smell). Prefer LB (stable DNS), Route 53 alias, NAT Gateway. Legit EIP: partner firewall allowlist, NAT, manual failover.

### Placement Groups

| Strategy | Placement | Trade-off | Use |
|---|---|---|---|
| Cluster | Same rack, 1 AZ | Best network, worst blast radius | HPC, tightly-coupled |
| Spread | Distinct hardware across AZs | Max isolation, **7/AZ/group** | Small critical (quorum) |
| Partition | Partitions on own racks, 7/AZ | Isolation, hundreds | Kafka/Cassandra/HDFS (rack-aware) |

Name the trade-off. Partition number exposed to instance (replica placement).

### ENIs

Virtual NIC: primary + secondary IPs, EIP per IP, MAC, SGs, source/dest check. **Bound to one AZ**; primary `eth0` can't detach; secondary can move to another instance **same AZ** (cheap failover — IP/MAC/SGs follow). Count capped by instance type (container density). Disable source/dest check for NAT/router. Lambda-in-VPC creates ENIs (spread subnets across AZs).

---

## 3. Lifecycle, Purchasing & Sizing

### Hibernate

RAM → encrypted root EBS, resume where left off (no boot/warm-up, same ID/private IP). Requires: root EBS **encrypted + large enough for RAM**, supported family, **RAM <150 GiB**, enabled **at launch**, max 60 days. For long-init apps. Not an ASG substitute.

### Purchasing Options (complete)

On-Demand · **Reserved** (Standard = biggest discount/locked family; Convertible = exchangeable) · **Savings Plans** (Compute SP spans EC2+Fargate+Lambda/any family; EC2 Instance SP deeper/pinned) · **Spot** (~90%, 2-min notice, needs checkpointing) · **Dedicated Instances** (single-tenant hardware, no host visibility) · **Dedicated Hosts** (whole physical server, socket/core visibility → **BYOL Windows/SQL/Oracle**) · **Capacity Reservations** (guaranteed capacity in an AZ, no discount/commitment).
**Dedicated Instances vs Hosts:** only Hosts expose the physical server (for per-socket/core BYOL). **Capacity Reservation vs RI:** RI = billing construct (discount, no capacity); Capacity Reservation = capacity construct (guarantee, no discount).

### EC2 Shared Responsibility

AWS = physical/hypervisor/hardware/AZ availability/encryption features. You = **guest OS patching (the big one)**, app/deps, SG/NACL/subnet, key pairs, IAM role + IMDSv2, turning encryption on, backups (snapshots/AMIs + test restore), multi-AZ architecture. "Lambda moves most of the line onto AWS; EC2 keeps it on me."

### EC2 Sizing, Pricing & CPU Credits

Sizing = vCPU:memory ratio: CPU-bound→`c` (2 GiB/vCPU); memory-bound→`r` (8+); general→`m` (4); bursty/idle→`t`. Benchmark (memory needs CloudWatch agent), target 40–60% steady; **Compute Optimizer** recommends. Horizontal (stateless web) preferred over vertical (single cache/monolith).
**T-family CPU credit trap:** baseline % + earn credits below baseline, spend to burst. **Standard mode**: credits exhausted → hard-throttled to baseline → "fine for hours then sluggish". **Unlimited mode**: bursts continue, billed extra (cost surprise). Diagnose: check `CPUCreditBalance`/`CPUSurplusCreditBalance`, not just `CPUUtilization` (throttled T-instance looks "healthy" below 100%). T-family only for genuine idle troughs; sustained high CPU → `m`/`c`/`r`.
Purchase decision: predictable 12mo+ → SP/RI; interruptible → Spot; new/unknown → On-Demand then commit; spans compute types → Compute SP; business-hours dev → On-Demand + scheduled stop; stateful no-checkpoint → avoid Spot. Right-sizing + killing idle beats switching pricing models first.
Justify EC2 vs serverless: hard OS/kernel requirement, steady 70–90% utilization, long-running in-memory state; existing IaC is a *secondary* factor, never the primary.

---

## 4. Block & File Storage

### EBS vs EFS vs S3

EBS = block (1 instance, DB volumes); EFS = managed NFS (many instances/AZs, shared content); S3 = object (HTTP, assets/backups/lake).

### EBS Volumes

A **network drive** (not physical). **AZ-locked** (move = snapshot→new volume in target AZ); one instance at a time (except Multi-Attach); **provisioned capacity** (pay allocated); online resize (grow only, can't shrink); **`DeleteOnTermination` = true for root, false for extra** (root+logs gone on terminate, orphaned data volumes accrue cost).
```bash
lsblk                          # find real NVMe name first
sudo mkfs -t xfs /dev/nvme1n1  # ⚠ never on a volume with data
```
**Gotcha 1:** requested `/dev/sdf` ≠ Linux `/dev/nvme1n1` (Nitro); `lsblk` first (wrong device = format root). NVMe numbering not stable. **Gotcha 2:** `mount` is temporary — without `/etc/fstab` (by **UUID**, with **`nofail`**) it vanishes on reboot (app fills root disk, data "disappears", remount hides root-disk writes). `nofail` prevents boot lockout; `mount -a` before rebooting.

### EBS Volume Types

| Type | Class | Perf | Boot? | Use |
|---|---|---|---|---|
| **gp3** | SSD GP | 3,000 IOPS/125 MB/s baseline (IOPS **independent of size**), to 16k/1000 | ✅ | **Default**, ~20% cheaper than gp2 |
| gp2 | SSD GP | 3 IOPS/GB | ✅ | Legacy |
| io2 Block Express | SSD PIOPS | to 256k IOPS, 99.999% | ✅ | Mission-critical DBs |
| io1 | SSD PIOPS | to 64k | ✅ | Older |
| st1 | HDD throughput | 500 MB/s | ❌ | Sequential logs/ETL |
| sc1 | HDD cold | 250 MB/s cheapest | ❌ | Archive |

Only SSD boots; gp3 decouples IOPS from size.

### EBS Snapshots

Point-in-time, in **AWS-managed S3**, **incremental** (deleting old never breaks newer). Snapshot running volume OK but quiesce DB (crash- vs app-consistent). Region-scoped, AZ-independent → restore any AZ / **copy to another region (DR move)**. FSR (removes lazy-load penalty, extra cost), Snapshot Archive (~75% cheaper, 24–72h restore), Recycle Bin (recover deletions), **DLM** (scheduled — correct answer for "how to back up EBS"). Cost leak: forgotten nightly snapshots.

### AMIs

Launch template for a **machine** (root+volumes + block mapping + launch perms); snapshot = disk, AMI = bootable machine. **Region-scoped → copy to each region** (multi-region pipeline step). Creating reboots by default (`--no-reboot` risks inconsistency). **Golden AMI** = pre-bake OS/runtime/agents/deps, thin user data (EC2 Image Builder automates). **Deregistering doesn't delete snapshots** (cost leak).

**Golden AMI pattern:** install once at build, not per launch. Three reasons minutes matter: (1) ASG scale-out useless for 5–6 min under a spike; (2) `yum update` = non-identical fleet ("fails on some instances"); (3) **❗ network blip → install fails but instance boots "healthy" → health check fails → ASG terminates+relaunches → launch loop, zero capacity.** Bake OS patches/runtime/CloudWatch+SSM agents/deps; thin user data = config only. EC2 Image Builder pipeline: RECIPE→BUILD→**TEST** (underrated — stops broken image, integrates Inspector)→DISTRIBUTE→SCHEDULE. **Packer** = alternative alongside Terraform. Mindset: stop patching, start **replacing** (immutable infra via instance refresh). Trade-offs: pipeline to maintain, AMI sprawl, slower iteration (thick in dev, golden in prod). Docker image = golden AMI for containers.

### Instance Store

Physical NVMe on host — highest IOPS/lowest latency. **Ephemeral** (lost on stop/hibernate/terminate/host failure; survives reboot). Can't snapshot/resize/detach. Uses: scratch/cache, replicated DB nodes (Cassandra/OpenSearch). "Fastest + least durable."

### EBS Multi-Attach

One io1/io2 to ≤16 Nitro instances (same AZ), full R/W. **A normal filesystem corrupts** — needs cluster-aware FS (GFS2/OCFS2) or app-managed locking. Not the shared-storage answer (that's EFS); for Oracle RAC-style.

### EBS Encryption

KMS AES-256: data at rest, **in transit instance↔volume**, all snapshots, all volumes from them (propagates). Negligible perf (Nitro). Enable "encryption by default" account/region. **Encrypt existing (can't flip in place):** snapshot → **copy with `--encrypted` + KMS key** → new volume → detach/attach. Sharing: default AWS-managed-key snapshot can't be shared → use customer-managed key.

### EFS

Managed NFS, many instances **multi-AZ** concurrently. **POSIX/NFSv4.1 — Linux only (❗ Windows → FSx for Windows; HPC → FSx for Lustre)**. Truly elastic, pay per GB stored, ~3× gp3. **Mount target per AZ** with SG allowing **NFS 2049** (#1 "EFS mount hangs"). Modes: General Purpose vs Max I/O; Bursting/Provisioned/**Elastic** throughput; classes Standard/One Zone/IA/Archive + lifecycle. KMS + TLS.
```bash
sudo mount -t efs -o tls fs-0123456789abcdef:/ /mnt/efs
```
Uses: shared uploads/content across fleet, CMS, CI cache, Lambda file storage.

### EFS vs EBS vs Instance Store

"One fast disk → EBS. Many instances, same files → EFS (FSx on Windows). Max speed + rebuildable → instance store."

**EC2 Storage DR:** at risk = **instance store (unrecoverable, gone on stop)**, EBS, config. Backup = snapshots (incremental) + DLM + AMIs + cross-region copy. RPO = DLM schedule; RTO minutes from AMI. **❗ Snapshots/AMIs are regional** — copy AMIs to DR region as a **build step**. ASG launch template must reference DR-region AMI ID. Instance store never backed up (stop loses it).

---

← [S3](05-s3.md) · [Index](README.md) · [Observability & Monitoring](07-observability-monitoring.md) →
