> **AWS Quick Revision Notes** · [Index](README.md) · Part II

# Relational Databases, Caching & Analytics

---

## 1. Choosing a Data Store

### Databases Overview

Purpose-built: Relational OLTP (RDS/Aurora — joins/txns/ORM), Key-value NoSQL (DynamoDB, DocumentDB), In-memory (ElastiCache/MemoryDB), Warehouse OLAP (Redshift), Query-in-place (Athena), Search (OpenSearch), Graph (Neptune), Time-series (Timestream), Ledger (QLDB), Wide-column (Keyspaces).
- **OLTP** (many small concurrent indexed r/w → RDS/DynamoDB) vs **OLAP** (few large scans/aggregations → Redshift/Athena). Don't run analytics on OLTP primary.

---

## 2. RDS — Multi-AZ, Replicas & Operations

### RDS Multi-AZ vs Read Replicas vs Aurora

| | Multi-AZ | Read Replica | Aurora |
|---|---|---|---|
| Purpose | HA/DR | read scaling | HA+scale, distributed storage |
| Replication | Synchronous | Asynchronous | storage-layer semi-sync |
| Standby readable? | No (classic; DB Cluster yes) | Yes | Yes (reader endpoint) |
| Failover | Auto 60-120s | Manual promotion (breaks replication) | Auto <30s |
| Cross-region | No | Yes | Yes (Global Database) |

- **#1 confused pair:** Multi-AZ = availability (standby serves no reads classic); Read Replica = read scaling (promotion manual/breaking, not HA/DR). Using replica as DR or expecting Multi-AZ to absorb reads = same mistake.
- ⚠️ **Multi-AZ ≠ DR** — synchronous standby replicates `DELETE FROM orders` faithfully. Logical damage → PITR/snapshots only.
- **Aurora:** MySQL/Postgres-compatible (EF Core Npgsql/MySQL unchanged), storage-layer replication (sub-10s lag typical), storage auto-scale, Multi-AZ cluster + Global Database. Wins for availability/failover/less storage mgmt at higher cost. RDS SQL Server (Aurora can't) for SQL-Server .NET shops.
- **RDS Proxy:** pools/multiplexes connections — critical for Lambda-to-RDS.

### RDS Operational Surface

- Relational fundamentals: PK/FK integrity, normalisation, ACID, indexes, joins. Relational = flexible querying; NoSQL = known access patterns.
- **AWS manages:** OS/engine patching (maintenance window), automated backups/snapshots/PITR, Multi-AZ failover, hardware. **You:** schema/indexes/query tuning, retention + test restores, Multi-AZ vs replica decision, connection retry, sizing, **no OS/shell access**.
- Features: automated backups (daily + tx logs → PITR 1-35 days; deleted with instance unless final snapshot), manual snapshots (kept till you delete, shareable), maintenance window, storage autoscaling, read replicas (5, Aurora 15, cross-region, promotable), encryption (KMS at creation; TLS; IAM DB auth), place in **private subnets** + SG-to-SG.
- **RDS Custom** (Oracle/SQL Server): OS access + DB superuser (SSIS/SSRS/CLR/linked servers, Oracle SYS) while keeping backups/PITR/Multi-AZ. ❗ **support perimeter** — break config → `unsupported-configuration`, automation stops. Automation pause (60min-24h). Needs instance profile + S3 + **customer-managed KMS key** + CEV.
- **RDS Security (layered):** private subnets + DB subnet group ≥2 AZ + SG-to-SG + `PubliclyAccessible=false`; encryption at rest (KMS at creation, snapshot-copy for existing); TLS enforce (`rds.force_ssl=1` / `require_secure_transport`, .NET `Encrypt=True` no `TrustServerCertificate`); auth (never config password → Secrets Manager rotation → **IAM DB auth** strongest, pair with RDS Proxy → Kerberos); least-priv DB user (not `sa`); auditing (CloudTrail=API, engine logs→CloudWatch, **Database Activity Streams**=tamper-resistant); patching + deletion protection + final snapshot + AWS Backup; Performance Insights (query text sensitive).

### RDS Proxy

- Managed connection pool between app and RDS/Aurora.
- **Problem:** RDS hard `max_connections`; Lambda = each concurrent env opens own connection (500 concurrency → 500 connections → refused). Traditional pooling doesn't help (per-sandbox pool size 1).
- **Gives:** pool/multiplex, **~66% faster failover** (re-points connections), enforces IAM auth + Secrets Manager, in-VPC.
- **Pattern Lambda→RDS Proxy→RDS:** Lambda points at **proxy endpoint** (only app change). ❗ **Connection pinning** — session state (open transactions, `SET` vars, temp tables, prepared statements) forces pin, loses multiplexing benefit → watch `DatabaseConnectionsCurrentlySessionPinned`; fix app-side. Setup: Lambda VPC-attached (needs NAT/endpoints for other AWS), Secrets Manager secret + IAM role, SGs 5432/3306. IAM DB auth removes password (`rds-db:connect` + `RDSAuthTokenGenerator.GenerateAuthToken`).
- **Kab nahi:** greenfield simple → DynamoDB (no connection concept); zero connection mgmt → Aurora Data API; long-lived container (own pool); very low concurrency (reserved concurrency cap). Model answer emphasises **Lambda = processes not threads**.

### ECS → RDS: Database Connection ki Chaar Layers
Interactive version: [ecs-to-rds-connection-explained.html](../../../assets/ecs-to-rds-connection-explained.html).

**Core idea: network / DNS / credentials / pool INDEPENDENTLY fail hoti hain, alag symptoms ke saath.** Timeout kabhi credentials nahi; `AccessDenied` kabhi security group nahi. Pehle layer ka naam lo.

| Layer | Kya chahiye | Kaise tootta hai |
|---|---|---|
| **Network** | DB SG par DB port ka ingress **compute SG se**; same VPC ⇒ local routing, NAT/IGW nahi | **hang → timeout** (packets drop, koi reply nahi) |
| **DNS** | endpoint hostname → private IP, VPC resolver se (**base +2**); DNS support/hostnames on | resolution error, ya VPC ke bahar se *public* IP |
| **Credentials** | task role: ARN par `GetSecretValue` **+ `kms:Decrypt`** | `AccessDenied` |
| **Pool** | `tasks × pool size` < server session limit | session-limit errors **sirf load par** |

⚠️ **Triage:** timeout → network · refused → galat port · **koi bhi error *database se* → network theek hai, credentials par jao.**

- **Bridge mode ⇒ task ka apna ENI nahi**, isliye **instance ENI** hi source IP *aur source SG* hai → isi wajah se ingress "fleet SG se", aur **cluster ka har task ek hi network identity share karta hai**. `awsvpc` = per-task ENI/SG = per-service access.
- **SG-to-SG referencing sirf ek VPC ke andar** (ya same-region peering) chalta hai → agar rule SG name karta hai, to compute aur DB same VPC mein hain, by construction.
- ❗ **Multi-AZ failover DNS repoint karta hai, lekin pooled connections mare hue IP se bandhe rehte hain** → validation query + bounded connection lifetime chahiye; [RDS Proxy](#rds-proxy) ka sabse strong argument.
- **① task-role creds `169.254.170.2` se** (SDK `AWS_CONTAINER_CREDENTIALS_RELATIVE_URI` se dhoondhta hai) → **② `GetSecretValue`** → **③ `kms:Decrypt`** → **④⑤ DB par TCP, VPC-local, NAT nahi.** ①–③ HTTPS API calls, ④–⑤ actual DB connection.

**Do roles, do secret patterns** — inhe confuse karna hi classic "policy sahi, role galat" error hai:
| | Pattern A — ECS-injected (`secrets` block) | Pattern B — runtime SDK fetch |
|---|---|---|
| Role | **Execution role** (agent, start se pehle) | **Task role** (aapka code, runtime par) |
| Result | value **plain env var** ban jaati hai | env var mein sirf **ARN** |
| Trade-off | `describe-tasks`/`docker inspect` mein dikhta hai; **rotation ke liye restart** | password kabhi env mein nahi; **rotation ke baad re-fetch, bina redeploy** |
| Kiske liye | licence keys | **DB credentials** |

⚠️ **Missing `kms:Decrypt` #1 "impossible" `AccessDenied` hai** — secret ki policy khud theek padhti hai.

**Connection budget:** `total = tasks × pool per task`. Inhe kuch wire nahi karta, isliye scale out karne par DB pressure chup-chaap badhta hai. Inject kiye gaye `ASG_CAPACITY_MAX` se derive karo: `max pool = floor(session budget / ASG_CAPACITY_MAX)` — phir jab koi autoscaling max badhata hai to ceiling **apne aap gir jaati hai**. Budget 300 / 7 tasks = **42**; admin sessions aur deploy overlap ke liye headroom rakho (`max + minimum_healthy_percent`, sirf `max` nahi).

**Client-SG pattern (jo change karna chahiye):** compute stack ko DB ke SG mein ingress likhne na do — Terraform ko conflict nahi dikhta, consumers race karte hain, aur DB team dependants list nahi kar sakti; shared **fleet SG** se source hone par cluster ki *har* service DB tak pahunch jaati hai. Iske bajaye **database stack dono SGs aur rule owns kare**: ek khaali **`db-client` SG capability token** ki tarah, jise compute sirf **attach** karta hai. Auditing = kisne attach kiya hai woh list karo; revocation = ek rule.

---

## 3. Aurora

### Aurora Advanced

- 6 copies/3 AZs (tolerate 2-copy write loss, self-heal), 15 read replicas (lower lag, shared storage), reader/writer/custom endpoints, **Serverless v2** (sub-second ACU scaling), **Global Database** (5 regions, <1s replication, <1min failover), database cloning (copy-on-write), Backtrack (in-place rewind), Blue/Green, Aurora ML.

---

## 4. Caching

### ElastiCache & Caching

- Managed in-memory (Redis/Memcached), sub-ms reads. Offload DB reads.

| | Redis | Memcached |
|---|---|---|
| Structures | rich (sorted sets, streams) | simple k/v |
| Persistence | ✅ | ❌ |
| HA | ✅ Multi-AZ failover | ❌ |
| Scaling | shards+replicas | multi-threaded sharding |

Default = Redis.
- **Patterns:** lazy-loading/cache-aside (miss → DB → populate; risk stampede), write-through (warm reads, cache-everything), write-behind (fastest, data-loss risk), TTL/eviction (mandatory).
- Uses: query cache, **session state** (correct sticky-session alternative), rate limiting, leaderboards (sorted sets), pub/sub. .NET `IDistributedCache` (`AddStackExchangeRedisCache`), `HybridCache`.
- Ops: VPC 6379/11211 (not public), encryption + AUTH/RBAC, cluster mode, Multi-AZ. **MemoryDB** = durable variant.
- Invalidation = hard part: short TTL (eventual), explicit invalidation on write, versioned keys (`user:123:v7`).
- **DAX:** DynamoDB-specific in-VPC write-through cache (ms→µs, eventual only), **no app caching logic** (point client at cluster). Item cache + query cache. Only eventual reads, cluster (hourly), writes pass through. **DAX vs ElastiCache:** DAX = zero-code DynamoDB read cache; ElastiCache = anything else (aggregates/session/counters). Read-heavy DDB hitting hot partition/RCU ceiling → DAX.
- **Pitfalls:** ❗ cache stampede (jittered TTLs, short lock, serve-stale); ❗ hot key (one shard — client-side cache / split key); no TTL = stale + OOM (`allkeys-lru`, not `noeviction`); metrics (`CacheHitRate`/`Evictions`); failover not transparent (config endpoint, singleton `ConnectionMultiplexer` `abortConnect=false`); Redis single-threaded (`SCAN` not `KEYS`, `UNLINK` not `DEL`); big keys/serialization; **it's a cache not DB** (durable → MemoryDB); Lambda connection reuse; creation-time-only settings; burstable `cache.t*` credits.

**DR — RDS/Aurora/ElastiCache:** risk = relational data (cache rebuildable). Backup = automated+PITR (35d), manual snapshots, cross-region backup replication + read replicas, Aurora Global, Redis RDB, AWS Backup. Multi-AZ 60-120s (HA), PITR RPO ~5min, Aurora Global RPO <1s/<1min. AZ failure → nothing (endpoint DNS same); regional → `promote-read-replica` (breaks replication) or `failover-global-cluster`; repoint via Secrets Manager/Route 53 CNAME; logical corruption → PITR to new instance. ⚠️ Multi-AZ ≠ DR; deleting instance deletes automated backups (final snapshot).

---

## 5. Analytics

### Athena

- Serverless SQL on S3 (Presto/Trino). **Pay per TB scanned** (~$5) → cost = query design: columnar (Parquet/ORC), **partitioning** (`year=/month=/day=`, partition projection), compression, no `SELECT *`. Schema from Glue Data Catalog. Federated queries. Results → S3 (lifecycle rule).
- **Athena vs Redshift:** Athena serverless/pay-per-query/ad-hoc; Redshift provisioned warehouse/frequent-complex-BI. Athena for CloudTrail/Flow Logs/access logs.
- Family: Glue (ETL + Catalog), Redshift (warehouse, Spectrum), EMR (Hadoop/Spark), QuickSight (BI + SPICE), Managed Flink (streaming), Lake Formation (governance).

---

← [Containers: Docker, ECS, ECR & Fargate](09-containers-ecs-fargate.md) · [Index](README.md) · [Networking](11-networking.md) →
