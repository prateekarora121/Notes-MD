> **AWS Quick Revision Notes** · [Index](README.md) · Part I

# EC2 & Instance Storage

---

## 1. EC2 Fundamentals

### EC2 Fundamentals

- Instance (VM) + AMI (template). Families: `t` (burstable), `m` (balanced), `c` (compute), `r` (memory), `g`/`p` (GPU), `i`/`d` (storage). Storage: EBS (persistent, network) vs Instance Store (ephemeral, local).
- Lifecycle: Stop (EBS persists, stop paying compute) vs Terminate (root EBS destroyed default).
- Pricing: On-Demand / Reserved / Savings Plans / Spot (2-min warning).
- Choose EC2 for: full OS control, custom kernel, legacy, stateful, GPU, steady long-running.

### Instance Types, User Data, Metadata

- Decode `m5dn.2xlarge`: family/generation/attributes/size. Attributes: `g`=**Graviton ARM64 (~20% cheaper)**, `a`=AMD, `n`=network, `d`=local NVMe, `b`=block-optimised. Sizes: large=2 vCPU, doubling.
- **Graviton + .NET:** .NET 6+ supports ARM64 → ~20% saving, recompile only.
- **User Data:** startup script, runs **once, first boot, root** (cloud-init/EC2Launch). 16 KB limit (base64). ❗ Doesn't re-run on reboot → use `systemctl enable` (proper service) not force re-run. Failed script ≠ failed instance (green status, no software) → debug `/var/log/cloud-init-output.log`. **Golden AMI + thin user data** = answer.
- **IMDS:** `http://169.254.169.254/latest/meta-data/` (link-local, no creds needed). SDK gets role creds here + auto-refresh. User data (you provide) vs metadata (AWS provides). ❗ **IMDSv2 required** (`HttpTokens: required`) — SSRF defence.

---

## 2. EC2 Networking & Interfaces

### Security Groups & Ports

- SG = stateful virtual firewall on **ENI**. **Allow only** (no deny — that's NACL). Default: all inbound denied, all outbound allowed. Regional, VPC-locked. Changes apply immediately. **Reference SG not CIDR** (survives IP changes): `App-SG: 8080 from ALB-SG`.

| | SG | NACL |
|---|---|---|
| Attach | ENI | Subnet |
| Rules | Allow only | Allow + Deny |
| State | Stateful | Stateless (both directions + **ephemeral 1024-65535**) |
| Eval | union of allows | numbered order, first match |

- **Debug:** timeout/hang = network (SG/NACL/route/subnet); "connection refused" = reached host, **app** down/wrong port.
- **Ports:** 22 SSH+SFTP, 21/20 FTP, 3389 RDP, 80/443 HTTP(S), 53 DNS, 25/587/465 SMTP, **3306** MySQL/Aurora MySQL, **5432** Postgres/Aurora/Redshift, **1433** SQL Server, 1521 Oracle, 6379/11211 Redis/Memcached, 27017 Mongo/DocumentDB, **2049 NFS/EFS**.

### IPs & Placement

| | Private | Public | Elastic IP |
|---|---|---|---|
| Reachable | VPC only | Internet | Internet |
| Survives stop/start | Yes | **No (new one)** | Yes |
| Movable | No | No | Yes |

- ❗ Public IP changes on stop → breaks hardcoded DNS/allowlists. Prefer LB + Route 53 + NAT Gateway over EIP.
- **Placement Groups:** Cluster (same rack, best network, worst blast radius — HPC), Spread (distinct hardware, 7/AZ, critical few), Partition (racks, 7 partitions/AZ, Kafka/Cassandra rack-aware).
- **ENI:** virtual NIC, AZ-bound; primary `eth0` can't detach; secondary movable in same AZ (cheap failover). Source/dest check disable for NAT/router. VPC Lambda creates ENIs.
- **Hibernate:** RAM → encrypted root EBS; resume without boot. Root EBS encrypted, RAM <150 GiB, enable at launch, max 60 days.

---

## 3. Lifecycle, Purchasing & Sizing

### Purchasing Options (complete)

| Option | Buying | Key |
|---|---|---|
| On-Demand | pay-as-go | default for unknown |
| Reserved | 1-3yr config | Standard (biggest, locked) vs Convertible |
| Savings Plans | 1-3yr $/hr | Compute SP (EC2+Fargate+Lambda, flexible) vs EC2 Instance SP (deeper, pinned) |
| Spot | spare, ~90% off | 2-min notice, checkpointing |
| Dedicated Instances | single-tenant HW | no host visibility |
| Dedicated Hosts | whole physical server | **BYOL socket/core licensing** |
| Capacity Reservations | reserved capacity | guaranteed availability, no discount |

- **Dedicated Instances vs Hosts:** only Host exposes sockets/cores (BYOL). **Capacity Reservation vs RI:** capacity guarantee vs billing discount.

**Shared responsibility (EC2 near "your job" end):** AWS = physical/hypervisor/hardware/AZ. You = **guest OS patching**, app, SG/NACL/subnet, key pairs, IAM role + IMDSv2, enable encryption, backups, multi-AZ architecture.

### EC2 Sizing, Pricing, CPU Credits

- **Sizing = vCPU:memory ratio decision.** CPU-bound→`c` (2GiB/vCPU), memory→`r` (8GiB), general→`m` (4GiB), dev/bursty→`t`. Benchmark (memory needs CloudWatch Agent), target 40-60% steady. Compute Optimizer recommends. Horizontal > vertical for stateless.
- **T-family CPU credit trap:** burstable = baseline + earn/spend credits. Standard mode = throttle to baseline when depleted ("app fine for hours then slow"); Unlimited = extra billing. Diagnose via `CPUCreditBalance`/`CPUSurplusCreditBalance` (not just `CPUUtilization`). T-family only for genuine idle troughs; sustained load → `m`/`c`/`r`.
- **Decide On-Demand/RI/Spot/SP:** predictable 12mo+ → SP/RI; interruptible → Spot; new/unknown → On-Demand first; multi-compute → Compute SP; business-hours dev → On-Demand + scheduler; stateful no-checkpoint → avoid Spot.
- **EC2 vs Lambda/Fargate justification:** full OS/kernel/GPU need; steady high utilization (70-90% 24/7); existing IaC (secondary factor only); long-running in-memory state. Counter-signal: "simpler to reason about" = comfort-driven not workload-driven.

---

## 4. Block & File Storage

### EBS/EFS/S3, Volumes, Snapshots, AMIs

| | EBS | EFS | S3 |
|---|---|---|---|
| Type | Block (network drive) | Managed NFS | Object |
| Attach | 1 instance (16 Multi-Attach) | Many/multi-AZ | HTTP |
| AZ | Single (snapshot to move) | Multi-AZ | Region |
| Capacity | Provisioned | Elastic | Unlimited |

**EBS = network drive:** AZ-locked (snapshot to move); one instance (except Multi-Attach); provisioned capacity (pay allocated); online resize (can't shrink); `DeleteOnTermination` **true for root, false for data** (asymmetry — orphaned data volumes cost); persists independently.
- **Attach gotchas:** ❗ `/dev/sdf` requested → Linux shows `/dev/nvme1n1` (Nitro). **Always `lsblk` first** (wrong `mkfs` destroys root volume). ❗ `mount` temporary — need `/etc/fstab` (by **UUID**, with **`nofail`**) else vanishes on reboot / lockout. Test `mount -a` before reboot.

**Volume types:** **gp3** (default, 3,000 IOPS/125 MB/s baseline, IOPS independent of size, ~20% cheaper than gp2), gp2 (legacy), **io2 Block Express** (256K IOPS, 99.999%), io1, st1/sc1 (HDD, no boot). Only SSD boots.

**Snapshots:** point-in-time, S3-managed, **incremental**. Region-scoped, AZ-independent; cross-region copy = DR. FSR (removes lazy-load penalty), Snapshot Archive (~75% cheaper, 24-72h restore), Recycle Bin, **DLM** (scheduled — "how to backup EBS?"), AWS Backup. Cost leak: forgotten snapshots.

**AMIs:** bootable machine template (vs snapshot = disk). **Region-scoped** (copy per region). Create reboots by default (`--no-reboot` risky). **Golden AMI:** bake OS patches/runtime/agents/deps, thin user data → faster ASG scale-out (~90s vs 5-6min), identical fleet, no boot-time external repo (network blip = ASG launch loop). EC2 Image Builder (recipe/build/test/distribute/schedule) or Packer. Deregister ≠ delete snapshots. Immutable infra: rebuild + roll, not in-place patch.

**Instance Store:** local NVMe, highest IOPS, **ephemeral** (lost on stop/terminate/host fail; survives reboot). Scratch/cache/replicated DB nodes.

**EBS Multi-Attach:** io1/io2, 16 Nitro instances same AZ. ❗ Needs cluster-aware FS (GFS2/OCFS2) — normal FS corrupts. Not shared web storage (that's EFS).

**EBS Encryption:** KMS, covers at-rest + in-transit + snapshots + derived volumes. Enable by default. **Encrypt existing:** snapshot → copy `--encrypted` → new volume → swap. Default-key snapshot can't be shared (use CMK).

**EFS:** managed NFS, multi-AZ concurrent. ❗ **Linux only** (Windows → **FSx for Windows**; HPC → FSx for Lustre). Elastic (pay stored, ~3× gp3). Mount target per AZ + SG (**NFS 2049**). Modes: General Purpose/Max I/O; Bursting/Provisioned/Elastic; classes Standard/One Zone/IA/Archive. Uses: shared content, Lambda file storage.

| | EBS | EFS | Instance Store |
|---|---|---|---|
| Attach | 1 | Many multi-AZ | 1 local |
| Survives instance death | Yes | Yes | **No** |
| OS | Any | Linux only | Any |

**DR — EC2/Storage:** risk = instance store (unrecoverable, lost on stop), EBS, config. Backup = **EBS snapshots** + DLM, **AMIs**, cross-region copy. Instance fail → launch from golden AMI (auto if ASG); volume corrupt → create-volume from snapshot; regional → launch from **pre-copied** AMI + Route 53. ⚠️ Snapshots/AMIs regional — copy as build step; instance store never backed up.

---

← [S3](05-s3.md) · [Index](README.md) · [Observability & Monitoring](07-observability-monitoring.md) →
