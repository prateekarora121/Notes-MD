> **AWS Quick Revision Notes** · [Index](README.md) · Part III

# Migration & Data Transfer

---

## 1. Migration Strategy

### 7 Rs

| Strategy | Meaning | When |
|---|---|---|
| Retire | shut down | audit first |
| Retain | leave as-is | mainframe/pending |
| Relocate | hypervisor move (VMware) | fast DC exit |
| Rehost | lift-and-shift EC2 (MGN) | speed/deadline; cheap to do, expensive to run |
| **Replatform** | swap components → managed (SQL→RDS) | ✅ sweet spot |
| Repurchase | drop → SaaS | commodity |
| Refactor | cloud-native rewrite | highest cost/payoff, business driver |

- "**Rehost out of DC first, then selectively replatform/refactor** once on AWS with real telemetry. Rehosting alone rarely saves money (just moves the bill)." Tools: Migration Hub, Application Discovery Service, Migration Evaluator.

---

## 2. Database Migration

### DMS

- Migrates while source online. Homogeneous vs heterogeneous (needs **SCT** for schema; SCT=schema, DMS=data). **CDC** = full load + continuous replication → short cutover + rollback path. Replication instance in VPC; also ongoing replication to S3/Kinesis/OpenSearch.

---

## 3. Bulk & Hybrid Data Transfer

### Snow Family

| Device | Capacity | Use |
|---|---|---|
| Snowcone | 8-14 TB | edge |
| Snowball Edge | ~80 TB | workhorse + local compute |
| Snowmobile | 100 PB | exabyte DC evacuation |
- **Reason:** calculate transfer time (100 TB on 1 Gbps = >1 week saturated). <10 TB / fat link → network (DataSync). KMS encrypted, tamper-evident.

### Storage Gateway / DataSync / Transfer Family

- **Storage Gateway** (hybrid appliance): S3 File Gateway (NFS/SMB→S3), FSx File Gateway (SMB), Volume Gateway (iSCSI), Tape Gateway (VTL→Glacier).
- **DataSync:** managed online transfer/sync (on-prem ↔ AWS, or AWS↔AWS) — parallelism, validation, incremental, scheduling.
- **Transfer Family:** managed SFTP/FTPS/FTP over S3/EFS.
- Distinction: Storage Gateway = permanent hybrid foothold; DataSync = move/sync over network; Snow = physical move; Transfer Family = legacy protocols to S3.

---

← [Cost & Performance](16-cost-performance.md) · [Index](README.md) · [Well-Architected & Resilience](18-well-architected-resilience.md) →
