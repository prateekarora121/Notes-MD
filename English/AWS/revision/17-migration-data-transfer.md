> **AWS Quick Revision Notes** · [Index](README.md) · Part III

# Migration & Data Transfer

---

## 1. Migration Strategy

### The 7 Rs

Retire (switch off) · Retain (leave) · Relocate (VMware Cloud, no change) · **Rehost** (lift-and-shift onto EC2, MGN — cheapest to do, most expensive to run) · **Replatform** (swap components for managed services — SQL Server→RDS, the sweet spot) · Repurchase (buy SaaS) · **Refactor** (rewrite cloud-native — highest cost/payoff, needs a business driver). "Rehost first to exit the DC, then replatform/refactor selectively with real telemetry." Tooling: Migration Hub (dashboard), Application Discovery Service (inventory), Migration Evaluator (TCO).

---

## 2. Database Migration

### DMS

Migrates DBs with source online. Homogeneous (straight move) vs heterogeneous (needs **SCT** first — SCT converts schema, DMS moves data). **CDC** = full load then continuous replication → short cutover window + rollback path. Replication instance in VPC; also ongoing replication into S3/Kinesis/OpenSearch.

---

## 3. Bulk & Hybrid Data Transfer

### Snow Family

Physical devices for offline transfer. Snowcone (~8–14 TB, edge) · **Snowball Edge** (~80 TB + local compute — workhorse) · Snowmobile (100 PB container). Reasoning: 100 TB over 1 Gbps = well over a week → Snowball wins; below ~10 TB with a fat link → DataSync over DX. KMS-encrypted, tamper-evident.

### Storage Gateway / DataSync / Transfer Family

**Storage Gateway** (hybrid appliance, local protocol + data in AWS): S3 File Gateway (NFS/SMB→S3), FSx File Gateway (SMB→FSx), Volume Gateway (iSCSI→S3+EBS snapshots), Tape Gateway (VTL→Glacier). **DataSync** = managed accelerated **online** transfer/sync (on-prem↔AWS or between AWS). **Transfer Family** = managed **SFTP/FTPS/FTP** over S3/EFS. "Storage Gateway = permanent hybrid foothold; DataSync = move/sync over network; Snow = move physically when network can't; Transfer Family = legacy file-transfer protocols over S3."

---

← [Cost & Performance](16-cost-performance.md) · [Index](README.md) · [Well-Architected & Resilience](18-well-architected-resilience.md) →
