> **AWS Quick Revision Notes** · [Index](README.md) · Part II

# Relational Databases, Caching & Analytics

---

## 1. Choosing a Data Store

### Databases & Analytics Overview

Purpose-built stores — interview = "which and why":
Relational OLTP → RDS/Aurora (joins/transactions/ORM). NoSQL → DynamoDB/DocumentDB (known patterns, ms latency, scale). Cache → ElastiCache/MemoryDB. Warehouse OLAP → Redshift. Query-in-place → Athena. Search → OpenSearch. Graph → Neptune. Time series → Timestream. Ledger → QLDB. Wide-column → Keyspaces.
**OLTP** (many small indexed reads/writes → RDS/DynamoDB) vs **OLAP** (few large scans/aggregations → Redshift/Athena). Running analytics on the OLTP primary = classic mistake (fix = read replica / warehouse).

---

## 2. RDS — Multi-AZ, Replicas & Operations

### RDS Multi-AZ vs Read Replicas vs Aurora

| | Multi-AZ standby | Read Replica | Aurora Multi-AZ cluster |
|---|---|---|---|
| Purpose | HA/DR | Read scaling | HA + scaling |
| Replication | Synchronous | Async | Semi-sync (storage) |
| Readable secondary? | No (classic; **Multi-AZ DB Cluster** does) | Yes | Yes (reader endpoint) |
| Failover | Auto 60–120s | Manual promote (breaks replication) | Auto <30s |
| Cross-region | No (classic) | Yes | Yes (Global DB) |

Multi-AZ = **availability** not scale; Read Replica = **scaling reads** not HA (promotion is manual/breaking). **RDS Proxy** pools/multiplexes for Lambda-to-RDS.

### Multi-AZ vs Read Replica (#1 confused pair)

"I need to survive AZ failure" → Multi-AZ (sync, standby not readable in classic, auto failover, **no read scaling**). "Read traffic overwhelming one instance" → Read Replica (async, readable = its purpose, promotion manual+breaks replication, **no auto HA**, cross-region OK). Using a read replica as DR plan OR expecting a standby to absorb reads = same mistake. **Aurora:** MySQL/PG-compatible (drivers unchanged), replication at **storage layer** (not log shipping) → low replica lag (often sub-10s), storage auto-scales, Multi-AZ cluster + Global Database. Aurora vs standard RDS: Aurora for better availability/faster failover/less storage mgmt at higher cost; standard RDS for **SQL Server (Aurora is MySQL/PG only)** or when Aurora premium isn't justified.

### RDS Operational Surface

Relational fundamentals: fixed schema, PK/FK integrity, normalisation, ACID, indexes, joins (optimise flexible querying of normalised data vs NoSQL known-patterns denormalised).
AWS handles patching/backups/failover/hardware/encryption; **you** handle schema/indexes/tuning, retention + test restores, Multi-AZ-vs-replica choice + retry logic, sizing, enabling encryption. **No OS/shell access** (need it → RDS Custom or EC2). Features: automated backups (daily + tx logs → **PITR** 1–35 days; **deleted with instance unless final snapshot**), manual snapshots (kept until you delete, shareable), maintenance window, storage autoscaling, read replicas (5 / 15 Aurora, cross-region, promotable), encryption (KMS at creation; **IAM DB auth** = token not password), private subnets + SG-to-SG.

**RDS Custom** (Oracle/SQL Server only): OS access + DB superuser + host agents, but AWS still does backups/PITR/Multi-AZ **conditionally**. **❗ Support perimeter** — go outside supported config → `unsupported-configuration`, automation stops, fixing is yours. **Automation pause** (up to 24h) for host maintenance. Needs instance profile + S3 + **customer-managed KMS key** + CEV. Trigger for .NET = SSIS/SSRS, CLR, per-core licensing.

**RDS Security (layered):** (1) network — private subnets, DB subnet group ≥2 AZs, SG-to-SG, **`PubliclyAccessible=false`**, Session Manager port-forward for admin. (2) encryption at rest — KMS **at creation** (existing → snapshot→copy encrypted→restore); cross-account encrypted snapshot needs customer-managed key. (3) in transit — `rds.force_ssl=1` / `require_secure_transport`; .NET `Encrypt=True` without `TrustServerCertificate`, or `SSL Mode=Require`. (4) auth — master password ❌ / Secrets Manager+rotation / **IAM DB auth (strongest, 15-min token, has connection-rate limit → pair RDS Proxy)** / Kerberos. (5) authz inside DB is yours (least-priv user, not `sa`/master). (6) audit — CloudTrail (API not SQL), engine logs→CloudWatch, **Database Activity Streams** (tamper-resistant, even DBA can't erase). (7) patching in maintenance window + deletion protection + final snapshot + AWS Backup. (8) Performance Insights can surface query text (treat as data access).

### RDS Proxy

Managed connection pool between app and RDS/Aurora. Problem: DB has hard `max_connections`; **Lambda pathological** — each concurrent env opens its own connection (burst to 1,000 = 1,000 connections refused), and per-env pool of one doesn't help. Gives: pool + multiplex, **~66% faster failover** (re-points connections), enforces IAM auth + Secrets Manager, in-VPC. Use: Lambda-to-RDS (almost always), many short-lived connections, faster failover. Not: single long-lived pooled app.

**Lambda→RDS Proxy→RDS:** Lambda = process per concurrent invoke (own private pool of one). CONCURRENCY 500 → 500 connections > db.t3.medium PG ~420 → FATAL too many connections + churn (connect/TLS/disconnect storm costs DB CPU). Proxy: warm pool + multiplex 500 clients onto ~20–50 real; app points at **proxy endpoint** (only change). **❗ Connection pinning** — multiplexing only works while session is stateless; explicit transactions / `SET` vars / temp tables / prepared statements pin a client to one connection (lose sharing). Watch `DatabaseConnectionsCurrentlySessionPinned`; fix app-side (short transactions). Setup: Lambda VPC-attached (loses internet → NAT/endpoints), Secrets Manager secret + IAM role, SGs Lambda→proxy→RDS on 5432/3306, IAM DB auth (`RDSAuthTokenGenerator.GenerateAuthToken`); billed per target vCPU/hour. Not: greenfield simple → DynamoDB (no connection concept); zero connection mgmt → Aurora Data API (higher latency); one long-lived container → own pool; very low concurrency → maybe reserved concurrency cap. What's tested: understanding **Lambda is not a web server** (concurrency = processes not threads).

### ECS → RDS: The Four Layers of a Database Connection
Interactive version: [ecs-to-rds-connection-explained.html](../../../assets/ecs-to-rds-connection-explained.html).

**Core idea: network / DNS / credentials / pool fail INDEPENDENTLY, with different symptoms.** Timeout is never credentials; `AccessDenied` is never a security group. Name the layer first.

| Layer | Needs | Breaks as |
|---|---|---|
| **Network** | DB SG ingress on DB port **from the compute SG**; same VPC ⇒ local routing, no NAT/IGW | **hangs → timeout** (packets dropped, nothing replies) |
| **DNS** | endpoint hostname → private IP via VPC resolver (**base +2**); DNS support/hostnames on | resolution error, or a *public* IP from outside the VPC |
| **Credentials** | task role: `GetSecretValue` on the ARN **+ `kms:Decrypt`** | `AccessDenied` |
| **Pool** | `tasks × pool size` < server session limit | session-limit errors **only under load** |

⚠️ **Triage:** timeout → network · refused → wrong port · **any error *from the database* → network is fine, go to credentials.**

- **Bridge mode ⇒ the task has no ENI**, so the **instance ENI** is the source IP *and source SG* → hence ingress "from the fleet SG", and **every task on the cluster shares one network identity**. `awsvpc` = per-task ENI/SG = per-service access.
- **SG-to-SG referencing only works inside one VPC** (or same-region peering) → if the rule names an SG, compute and DB are in the same VPC by construction.
- ❗ **Multi-AZ failover repoints DNS, but pooled connections stay bound to the dead IP** → need a validation query + bounded connection lifetime; the strongest argument for [RDS Proxy](#rds-proxy).
- **① task-role creds from `169.254.170.2`** (SDK finds it via `AWS_CONTAINER_CREDENTIALS_RELATIVE_URI`) → **② `GetSecretValue`** → **③ `kms:Decrypt`** → **④⑤ TCP to the DB, VPC-local, never NAT.** ①–③ are HTTPS API calls, ④–⑤ are the DB connection.

**Two roles, two secret patterns** — confusing them is the classic "correct policy, wrong role" error:
| | Pattern A — ECS-injected (`secrets` block) | Pattern B — runtime SDK fetch |
|---|---|---|
| Role | **Execution role** (the agent, pre-start) | **Task role** (your code, at runtime) |
| Result | value becomes a **plain env var** | only an **ARN** in the env var |
| Trade-off | visible in `describe-tasks`/`docker inspect`; **rotation needs a restart** | password never in env; **re-fetch after rotation, no redeploy** |
| Use for | licence keys | **DB credentials** |

⚠️ **Missing `kms:Decrypt` is the #1 "impossible" `AccessDenied`** — the secret policy itself reads fine.

**Connection budget:** `total = tasks × pool per task`. Nothing wires these together, so scaling out quietly raises DB pressure. Derive it from the injected `ASG_CAPACITY_MAX`: `max pool = floor(session budget / ASG_CAPACITY_MAX)` — the ceiling then **falls automatically** when someone raises the autoscaling max. Budget 300 / 7 tasks = **42**; leave headroom for admin sessions and deploy overlap (`max + minimum_healthy_percent`, not just `max`).

**Client-SG pattern (the change worth making):** don't let the compute stack write ingress into the DB's SG — Terraform sees no conflict, consumers race, and the DB team can't enumerate dependants; sourcing from the shared **fleet SG** also lets *every* cluster service reach the DB. Instead the **database stack owns both SGs and the rule**: an empty **`db-client` SG as a capability token**, which compute merely **attaches**. Auditing = list who has it attached; revocation = one rule.

---

## 3. Aurora

### Aurora Advanced

6 copies/3 AZs (tolerate 2-loss writes, 3-loss reads, self-heal), up to 15 read replicas (lower lag — read shared storage), reader/writer/custom endpoints, **Serverless v2** (sub-second ACU scaling), **Global Database** (5 secondary regions, <1s replication, <1min failover — Warm Standby/Active-Active), cloning (copy-on-write), **Backtrack** (rewind in place, MySQL), managed Blue/Green, Aurora ML.

---

## 4. Caching

### ElastiCache & Caching Patterns

Managed Redis/Memcached, sub-ms. Redis (rich structures, persistence, replication/Multi-AZ failover, backup, transactions/pub-sub/Lua) vs Memcached (simple K/V, no persistence/replication, multi-threaded). **Default Redis.** Patterns: **cache-aside/lazy** (miss→DB→populate; stampede risk), **write-through** (write both; caches unread data), **write-behind** (async flush; data-loss risk), **TTL/eviction** (`allkeys-lru`). "A cache with no TTL is a stale-data bug." Uses: query cache, session state (correct sticky-session fix), rate limiting, leaderboards (sorted sets), locks, pub/sub. .NET `IDistributedCache`/`AddStackExchangeRedisCache`, `HybridCache` (.NET 9). VPC + SG port 6379/11211, encryption + AUTH/RBAC, cluster mode to shard, Multi-AZ failover. **MemoryDB** = durable Redis (usable as primary DB). Cache invalidation = the hard part (short TTL / explicit / versioned keys).
**DAX** = DynamoDB-specific, in-VPC write-through, API-compatible → **no caching logic** (point DAX client at cluster). Item cache + query cache. Only eventual reads (strong passes through), cluster you pay hourly, writes go through. DAX (cache DynamoDB reads zero-code, eventual) vs ElastiCache (cache computed/enriched/session/counters). Read-heavy DynamoDB hitting hot partition → **DAX**.
**Pitfalls:** (1) **stampede** — hot key expires, hundreds miss → jittered TTL / short lock (`SET key NX EX 5`) / serve-stale-revalidate. (2) **hot key** on one shard — cluster mode doesn't help → client-side cache / split key. (3) no TTL = stale + OOM (default `noeviction` fails writes → `allkeys-lru`, reserve ~25%). (4) metrics: `CacheHitRate`, `Evictions`, memory/swap/connections (20% hit rate worse than none). (5) failover not transparent (use configuration/primary endpoint; `ConnectionMultiplexer` singleton with `abortConnect=false`). (6) **Redis single-threaded** — `KEYS *`/big `DEL`/Lua block everyone → use `SCAN`/`UNLINK`. (7) big keys + serialization cost. (8) **it's a cache not a DB** (durability → MemoryDB). (9) Lambda connection pressure (reuse multiplexer outside handler). (10) in-transit encryption + cluster mode = creation-time only. (11) `cache.t*` burstable CPU credits.

**RDS/Aurora/ElastiCache DR:** at risk = relational data (cache rebuildable unless Redis-as-record). Backup = automated backups+PITR (35d), manual snapshots (indefinite), cross-region backup replication + read replicas, Aurora Global, Redis RDB, AWS Backup. Multi-AZ failover 60–120s (HA not DR); PITR RPO ~5min; **Aurora Global RPO <1s, failover <1min**. AZ failure → auto (endpoint DNS stays same). Regional → `promote-read-replica` (breaks replication) / Aurora `failover-global-cluster`; repoint via Secrets Manager/SSM or Route 53 CNAME. Logical corruption → replicas useless, PITR to new instance. **❗ Multi-AZ is NOT DR** (sync standby faithfully replicates `DELETE FROM orders`); only PITR/snapshots protect logical damage. **Automated backups deleted with the instance** unless final snapshot.

---

## 5. Analytics

### Athena

Serverless SQL over S3 (Presto/Trino), no loading. **Per-TB scanned (~$5/TB)** → cost is query-design: (1) columnar Parquet/ORC (~10× less), (2) partitioning + partition projection, (3) compression + avoid `SELECT *`. Schema from **Glue Data Catalog**. Federated queries to RDS/DynamoDB. **Athena vs Redshift:** Athena serverless/ad-hoc/infrequent over a lake; Redshift provisioned warehouse for frequent/complex/high-concurrency BI. Shows up: query CloudTrail/VPC Flow Logs/ALB logs/CUR. Family: Glue (ETL+Catalog), Redshift (warehouse, Serverless, Spectrum), EMR (Hadoop/Spark), QuickSight (BI + SPICE), Managed Flink (streaming), Lake Formation (lake governance).

---

← [Containers: Docker, ECS, ECR & Fargate](09-containers-ecs-fargate.md) · [Index](README.md) · [Networking](11-networking.md) →
