> **AWS Detailed Guide** · [Index](README.md) · Part III

# Migration & Data Transfer

---

## 1. Migration Strategy

### The 7 Rs — Migration Strategies

The framework to reach for whenever a question starts "we have an on-prem application and want to move it to AWS." Naming the strategy *and* justifying the choice is the whole answer.

| Strategy | What it means | When it's right |
|---|---|---|
| **Retire** | Switch it off — nobody uses it | Always audit first; a surprising share of an estate is dead weight |
| **Retain** | Leave it where it is, for now | Mainframe dependencies, a pending vendor decision, or something being replaced anyway |
| **Relocate** | Move at the hypervisor level with no change (VMware Cloud on AWS) | Large VMware estate needing to exit a data centre fast |
| **Rehost** ("lift and shift") | Move as-is onto EC2, no code change | Speed and a hard deadline; cheapest to *do*, most expensive to *run*. Use **AWS Application Migration Service (MGN)** |
| **Replatform** ("lift and reshape") | Keep the app, swap components for managed services — self-managed SQL Server → **RDS**, self-hosted queue → **SQS** | ✅ The usual sweet spot: real operational savings without a rewrite |
| **Repurchase** | Drop it and buy SaaS | Commodity functions — email, CRM, ticketing |
| **Refactor / Re-architect** | Rewrite cloud-native (containers, serverless, managed data stores) | Highest cost and risk, highest long-term payoff — justify it with a business driver (scale, release velocity), never with "cloud-native is better" |

**The honest senior answer:** "**Rehost first to get out of the data centre, then replatform and refactor selectively once it's running on AWS with real telemetry.** Trying to refactor everything during a migration is how migrations slip by a year. And rehosting alone rarely saves money — it just moves the bill — so the business case has to include the follow-on replatforming."

Supporting tooling: **Migration Hub** (single dashboard tracking progress across tools), **Application Discovery Service** (inventories the on-prem estate and its dependencies before you plan), and the **Migration Evaluator** for the TCO business case.

---

## 2. Database Migration

### Database Migration Service (DMS)

Migrates databases with **the source staying online** during the migration.

- **Homogeneous** (SQL Server → RDS SQL Server, PostgreSQL → Aurora PostgreSQL) is a straight data move.
- **Heterogeneous** (Oracle/SQL Server → PostgreSQL/Aurora) needs the **Schema Conversion Tool (SCT)** first to convert schema, stored procedures, and views — SCT converts the *schema*, DMS moves the *data*. Keeping those two straight is the question.
- **Change Data Capture (CDC)** is the important part: DMS does a full load, then **continuously replicates ongoing changes**, so you keep both databases in sync and cut over during a short window instead of a long outage. Leaving CDC running in reverse also gives you a rollback path.
- Runs on a replication instance in your VPC; also does **ongoing replication** into S3/Kinesis/OpenSearch for analytics, not just migration.

---

## 3. Bulk & Hybrid Data Transfer

### Snow Family

**Physical devices AWS ships you** for offline data transfer — the answer when moving data over the network would take too long.

| Device | Capacity | Use for |
|---|---|---|
| **Snowcone** | ~8–14 TB | Small, rugged, edge collection; can even ship via a drone/vehicle |
| **Snowball Edge** | ~80 TB usable (Storage Optimized) / compute-optimised variants | The workhorse: TB-to-PB migrations, plus **local compute** (EC2/Lambda) at a disconnected edge site |
| **Snowmobile** | Up to **100 PB** — a 45-foot shipping container | Exabyte-scale data-centre evacuation |

**The reasoning to show, not just the names:** work out the transfer time. 100 TB over a 1 Gbps link, at realistic utilisation, is **well over a week** of saturated bandwidth you also need for production — so a Snowball that arrives in days wins. Below roughly 10 TB, or with a fat dedicated link, network transfer (DataSync over Direct Connect) is simpler. Data is encrypted with KMS and the devices are tamper-evident.

### Storage Gateway, DataSync & Transfer Family

**Storage Gateway** — a hybrid appliance (usually a VM on-prem) that gives local systems a familiar protocol while the data actually lives in AWS:
| Type | Presents | Backed by | Use for |
|---|---|---|---|
| **S3 File Gateway** | **NFS / SMB** file shares | S3 objects | Letting existing apps and users write "files" that land in S3, with a local cache |
| **FSx File Gateway** | SMB | FSx for Windows | Low-latency on-prem access to Windows file shares in AWS |
| **Volume Gateway** | **iSCSI** block volumes (cached or stored mode) | S3 + EBS snapshots | Backing up on-prem block storage; DR restore into EC2 |
| **Tape Gateway** | A **virtual tape library** (VTL) | S3 Glacier | Retiring physical tape backup while keeping the existing backup software |

**DataSync** — managed, accelerated **online** transfer and ongoing sync between on-prem (NFS/SMB/HDFS/object) and AWS (S3, EFS, FSx), or between AWS storage services. It handles parallelism, integrity validation, incremental sync, and scheduling — the right tool for repeated bulk transfer, where Storage Gateway is for *continuous hybrid access*.

**Transfer Family** — fully managed **SFTP/FTPS/FTP** endpoints in front of S3 or EFS. The answer when a partner or legacy system can only speak SFTP and you don't want to run and patch an SFTP server.

**The distinction to state:** "**Storage Gateway** keeps a permanent hybrid foothold — on-prem systems keep using NFS/SMB/iSCSI/tape while the data lives in AWS. **DataSync** is for moving or syncing data over the network. **Snow Family** is for moving data physically when the network can't. **Transfer Family** is for exposing S3 over legacy file-transfer protocols."

---

← [Cost & Performance](16-cost-performance.md) · [Index](README.md) · [Well-Architected & Resilience](18-well-architected-resilience.md) →
