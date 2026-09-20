> **AWS Detailed Guide** · [Index](README.md) · Part II

# Relational Databases, Caching & Analytics

> **Tier 2 — reason about, be honest about hands-on.** RDS/Aurora are not part of my hands-on AWS experience (SQL Server on-prem and Cosmos DB are) — see the framing note in [Multi-AZ vs Read Replica](#multi-az-vs-read-replica--the-1-confused-pair). Analytics services here are Tier 3: know the shape and when to reach for them.

---

## 1. Choosing a Data Store

### Databases & Analytics Overview: Choosing the Right Store

AWS deliberately offers **purpose-built** databases rather than one general-purpose engine. The interview question is almost never "what is DynamoDB?" — it's "which would you pick, and why?"

| Category | Service | Pick it when |
|---|---|---|
| **Relational (OLTP)** | RDS (SQL Server, PostgreSQL, MySQL, MariaDB, Oracle), **Aurora** | You need joins, transactions, referential integrity, ad-hoc queries, or an existing EF Core/ORM codebase |
| **Key-value / document (NoSQL)** | **DynamoDB**, DocumentDB (MongoDB-compatible) | Known access patterns, single-digit-ms latency, huge or spiky scale, no complex joins |
| **In-memory cache** | **ElastiCache** (Redis / Memcached), MemoryDB | Sub-millisecond reads, session state, leaderboards, relieving read pressure on a database |
| **Data warehouse (OLAP)** | **Redshift** | Complex analytical queries over TB–PB of structured data, BI dashboards |
| **Query-in-place** | **Athena** | Ad-hoc SQL directly over S3 with no infrastructure at all |
| **Search** | OpenSearch Service | Full-text search, log analytics, observability dashboards |
| **Graph** | Neptune | Relationships are the primary query: social graphs, fraud rings, recommendations |
| **Time series** | Timestream | IoT/metric data with time-based rollups and retention tiers |
| **Ledger** | QLDB | Cryptographically verifiable, immutable transaction history |
| **Wide-column** | Keyspaces (Cassandra) | Existing Cassandra workloads |

**OLTP vs OLAP is the framing to lead with:** OLTP is many small, concurrent, indexed reads/writes (an order-entry API → RDS/DynamoDB); OLAP is few large scans and aggregations over history (a revenue dashboard → Redshift/Athena). Running analytical queries against your OLTP primary is the classic architectural mistake — the fix is a read replica for light reporting, or a proper warehouse/lake for real analytics.

---

## 2. RDS — Multi-AZ, Replicas & Operations

### RDS Multi-AZ vs Read Replicas vs Aurora

The original notes never covered RDS despite it being one of the most common .NET-on-AWS database choices (SQL Server/PostgreSQL/MySQL via RDS is far more common for .NET shops than DynamoDB for primary OLTP workloads) — this is a material gap for a senior interview.

| Feature | Multi-AZ (standby) | Read Replica | Aurora (Multi-AZ cluster) |
|---|---|---|---|
| Purpose | High availability / DR | Read scalability, offload reporting | HA + scalability, AWS-native distributed storage |
| Replication | Synchronous to standby | Asynchronous | Semi-synchronous within storage layer |
| Standby usable for reads? | No (classic Multi-AZ) — **Multi-AZ DB Cluster** (newer) does allow reader endpoints | Yes — that's its purpose | Yes, via reader endpoint |
| Failover | Automatic (typically 60–120s) | Manual promotion (breaks replication) | Automatic, typically faster (<30s) |
| Cross-region | No (classic Multi-AZ is single-region) | Yes (cross-region read replicas supported) | Yes (Aurora Global Database) |
| .NET connection string implication | App connects to one endpoint; failover is transparent (DNS-based) but requires connection retry logic (Polly, EF Core resiliency) | App must route read-only queries to replica endpoint explicitly (read/write splitting in code or via a proxy) | Similar — reader/writer endpoints, RDS Proxy recommended for connection pooling across failover |

**Interview nuance to land:** Multi-AZ is for **availability**, not scalability — the standby doesn't serve traffic in classic Multi-AZ. Read replicas are for **scaling reads**, not HA — promoting one is a manual, replication-breaking operation and shouldn't be your primary DR plan. A senior answer distinguishes these instead of conflating "Multi-AZ" and "read replica" as interchangeable resilience mechanisms — a very common junior-level confusion this fills.

**RDS Proxy** (worth name-dropping): pools and multiplexes connections in front of RDS/Aurora — critical for Lambda-to-RDS patterns where each concurrent execution environment would otherwise open its own DB connection and exhaust the database's max connection limit under burst concurrency.

### Multi-AZ vs Read Replica — The #1 Confused Pair

Framing note: RDS is not part of my confirmed hands-on AWS experience (Lambda, DynamoDB, EC2, and S3 via Terraform/CDKTF are) — the following is conceptual/comparative knowledge I'd bring to a discussion of relational database strategy on AWS, not a claim of having operated RDS/Aurora in production myself.

This pairing is, by a wide margin, the most commonly confused RDS concept at every seniority level, so it earns a dedicated, drill-style callout beyond the comparison table above.

**The mistake, stated plainly:** candidates (and even some production architectures) treat "Multi-AZ" and "Read Replica" as if either one gives you both high availability *and* read scaling. They don't — each does exactly one of those two jobs, and reaching for the wrong one is a real, recurring production design mistake.

```
                    What problem are you solving?
                                 |
      +--------------------------+--------------------------+
      |                                                     |
 "I need the DB to survive                    "My read traffic is
  an AZ failure"                               overwhelming one instance"
      |                                                     |
      v                                                     v
+--------------------------+              +--------------------------+
|    MULTI-AZ STANDBY      |              |      READ REPLICA        |
+--------------------------+              +--------------------------+
| Synchronous replication  |              | Asynchronous replication |
|                          |              |                          |
| Standby is NOT readable  |              | Fully readable -- that    |
| (classic Multi-AZ)       |              | IS its entire purpose    |
|                          |              |                          |
| Automatic failover,      |              | Promotion is MANUAL and  |
| typically 60-120s        |              | breaks replication       |
|                          |              |                          |
| x Does NOT help with     |              | x Does NOT give you      |
|   read scaling           |              |   automatic HA failover  |
+--------------------------+              +--------------------------+

          Two mechanisms, two different problems.
          Using a read replica as your DR plan, or expecting a
          Multi-AZ standby to absorb read load, is the same mistake.
```

| | Multi-AZ (Standby) | Read Replica |
|---|---|---|
| Solves | Availability / disaster recovery | Read throughput / reporting offload |
| Does **not** solve | Read scaling (classic Multi-AZ standby serves no traffic) | Automatic HA (promotion is manual and breaks the replication link) |
| Replication mode | Synchronous | Asynchronous |
| Can you query the secondary? | No, in classic Multi-AZ (the newer **Multi-AZ DB Cluster** feature does add readable reader endpoints — know this distinction, it's a common "gotcha, that changed" follow-up) | Yes — that's the entire point |
| What happens on primary failure? | Automatic failover to standby, transparent via the same DNS endpoint | Nothing automatic — you must manually promote a replica, and promotion permanently breaks its replication relationship to the old primary |
| Can it span regions? | No (classic Multi-AZ is single-region only) | Yes — cross-region read replicas are explicitly supported |

**The drill answer to have ready verbatim:** "Multi-AZ is about *surviving failure* — it's a synchronous standby that AWS fails over to automatically, but in the classic (non-cluster) form it doesn't serve any read traffic, so it does nothing for scaling. Read Replicas are about *scaling reads* — asynchronous copies you explicitly route reporting/read traffic to, but promoting one to primary is a manual, replication-breaking operation, so it's not a substitute for real HA. Using a read replica as your DR plan, or expecting a Multi-AZ standby to absorb read load, are both the same category of mistake: conflating two mechanisms that solve different problems."

**Aurora, introduced more fully:** Aurora is AWS's own MySQL- and PostgreSQL-compatible relational engine (not a distinct SQL dialect — client drivers/ORMs like EF Core's Npgsql or MySQL providers work against it unchanged). Its key architectural difference from standard RDS is that replication happens at the **storage layer**, not by shipping database logs between full instances — Aurora separates compute from a shared, distributed, auto-scaling storage volume that replicates across AZs beneath the engine. This is why Aurora replica lag is typically much lower than standard RDS read-replica lag — commonly cited as sub-10-seconds, and often near-instant in practice — though exact lag is workload-dependent and should be treated as directional rather than a guaranteed number. Aurora also auto-scales storage (no manual volume resizing the way standard RDS requires) and supports both a Multi-AZ cluster mode (fast automatic failover, typically well under the classic Multi-AZ failover window) and Aurora Global Database (cross-region, for the Warm Standby/Active-Active DR strategies covered later in this guide).

**How I'd frame Aurora vs standard RDS if asked which I'd choose:** Aurora generally wins when you want RDS-compatible tooling/ORM support but with better availability characteristics, faster failover, and less manual storage management — at a higher cost per compute unit than equivalent standard RDS. Standard RDS (or RDS for SQL Server specifically, which Aurora does not support — Aurora is MySQL/PostgreSQL-compatible only) remains the right call for SQL Server-based .NET shops, or when Aurora's cost premium isn't justified by the workload's availability/scale needs.

### Relational Databases & RDS — The Operational Surface

**Relational fundamentals interviewers still ask about:** tables/rows/columns with a fixed schema; **primary and foreign keys** enforcing referential integrity; **normalisation** to remove duplication (vs the deliberate denormalisation DynamoDB requires); **ACID** guarantees (Atomicity, Consistency, Isolation, Durability); **indexes** to avoid full scans; and **joins** to combine tables at query time. The contrast to draw: relational databases optimise for *flexible querying* of normalised data, NoSQL optimises for *known access patterns* on denormalised data.

**What RDS manages vs what stays yours** — this is the shared-responsibility answer for databases:
| AWS handles | You still handle |
|---|---|
| OS and database engine patching (in your maintenance window) | **Schema design, indexes, and query tuning** |
| Automated backups, snapshots, point-in-time recovery | Choosing retention, and **testing that restores work** |
| Multi-AZ failover, replica provisioning | Deciding Multi-AZ vs replicas (see above), and **connection retry logic** |
| Hardware, storage, and monitoring infrastructure | Instance sizing, storage type, and cost |
| Encryption capability | Enabling encryption at creation, and key management |
| — | **No OS/shell access at all** — you cannot install an agent or a custom extension |

That last row is the key limitation: RDS gives you no host access. When you genuinely need it (a legacy Oracle/SQL Server setup requiring custom binaries), the answer is **RDS Custom** or self-managing the engine on EC2.

**The operational features worth naming:**
- **Automated backups** — daily full snapshot plus continuous transaction logs, enabling **point-in-time recovery (PITR)** to any second within the retention window (1–35 days). Retention `0` disables them. Automated backups are **deleted when you delete the instance** unless you take a final snapshot.
- **Manual snapshots** — kept until *you* delete them, and shareable across accounts/regions. The distinction "automated backups expire, manual snapshots don't" is a standard question.
- **Maintenance window** — when AWS applies patches; may cause a brief failover on Multi-AZ (which is why Multi-AZ makes patching near-transparent).
- **Storage autoscaling** — RDS grows the volume automatically when you approach the threshold, preventing the "database is full at 2 a.m." outage.
- **Read replicas** — up to 5 (15 for Aurora), can be **cross-region**, and can be promoted to standalone.
- **Encryption** — at rest via KMS (must be enabled **at creation**; to encrypt an existing instance you snapshot → copy the snapshot encrypted → restore), in transit via TLS. **IAM database authentication** lets an application authenticate with an IAM token instead of a stored password — the natural pairing with an EC2/Lambda role, and a strong answer to "how do you avoid database passwords?" (the other being **Secrets Manager** with rotation).
- Always place RDS in **private subnets** with a security group referencing only the app tier's SG.

#### RDS Custom — for Oracle & SQL Server

**The problem it solves.** Standard RDS gives you **no OS access and no DB superuser**, which is fine until a workload genuinely needs them. Typical blockers, and they're common in .NET/enterprise estates:
- SQL Server features that live outside the database engine — **SSIS / SSRS / SSAS**, linked servers, custom **CLR assemblies**, filesystem access for BULK INSERT
- Oracle features needing `SYS` — Data Guard, APEX, custom patches, specific PSU levels
- Third-party monitoring or backup agents that must be **installed on the host**
- A vendor application that only certifies against a specific OS/patch combination

**What RDS Custom is:** the deliberate middle ground.
| | **RDS** | **RDS Custom** | **DB on EC2** |
|---|---|---|---|
| OS access (SSH/RDP, sudo/admin) | ❌ | ✅ | ✅ |
| DB superuser (`SYS`, `sa`) | ❌ | ✅ | ✅ |
| Install host agents / native features | ❌ | ✅ | ✅ |
| Automated backups, PITR, Multi-AZ | ✅ AWS | ✅ AWS, **conditionally** | ❌ you build it |
| Engine/OS patching | ✅ AWS | **You initiate** | ❌ you |
| Engines | All | **Oracle and SQL Server only** | Anything |

**❗ The concept that makes it distinctive — the support perimeter.** AWS automation keeps working *as long as you stay inside a supported configuration*. Change something outside it (break the agent, alter the storage layout, remove required IAM permissions) and the instance moves to **`unsupported-configuration`**: automation stops, backups may halt, and **fixing it is your job**. So the trade isn't "managed plus root access" — it's "managed *until you break it*".

**Automation pause** is the companion feature: you can suspend RDS Custom automation (default 60 minutes, up to **24 hours**) to perform host maintenance without RDS "correcting" your changes mid-flight. Naming this shows real familiarity.

**Setup prerequisites worth knowing:** it needs an **IAM instance profile**, an S3 bucket for artifacts, and — notably — a **customer-managed KMS key** (an AWS-managed key won't do). SQL Server and Oracle both use a **Custom Engine Version (CEV)** so the exact build is pinned and reproducible.

**How to choose:** *"Standard RDS by default. **RDS Custom** when I need OS or superuser access but still want AWS handling backups, PITR, and Multi-AZ — accepting that I own anything I break. **Database on EC2** only when even RDS Custom's supported configuration is too restrictive, or the engine isn't Oracle/SQL Server."* For a .NET shop the trigger is usually SSIS/SSRS, CLR, or a per-core licensing arrangement.

#### RDS Security — Consolidated

The bullets above cover pieces of this; here it is as the layered answer to *"how do you secure RDS?"*

**1. Network — the layer that prevents the headline breach.** Private subnets only, a **DB subnet group across ≥2 AZs**, and a security group whose inbound rule references the **app tier's SG rather than a CIDR**. Critically, **`PubliclyAccessible = false`**: a publicly accessible instance plus a permissive SG is exactly how databases end up exposed to the internet. Reach it for admin work via **[Session Manager port forwarding](12-security-services.md#aws-systems-manager-ssm)**, not a public endpoint.

**2. Encryption at rest** — KMS, and it **must be enabled at creation**. To encrypt an existing instance: snapshot → **copy the snapshot with encryption** → restore. It covers the instance, automated backups, snapshots, and read replicas. Sharing an **encrypted** snapshot cross-account requires a **customer-managed** key; sharing an *unencrypted* one publicly is a genuine leak vector.

**3. Encryption in transit** — enforce rather than hope: `rds.force_ssl=1` (PostgreSQL) or `require_secure_transport=ON` (MySQL). Clients must trust the **RDS CA bundle**; in .NET that means `Encrypt=True` **without** `TrustServerCertificate=true` for SQL Server, or `SSL Mode=Require` for Npgsql. See [Encryption in Transit](12-security-services.md#encryption-in-transit-tls--end-to-end).

**4. Authentication — three options, in increasing strength:**
| Option | Notes |
|---|---|
| Master password in config | ❌ Never |
| **Secrets Manager** with rotation | Good — but read the [rotation pitfalls](03-iam-security.md#secrets-manager--pitfalls) |
| **IAM database authentication** | ✅ Strongest: a 15-minute token generated from the app's role, no stored password at all. Caveat — it has a **connection-rate limit**, so pair it with **[RDS Proxy](#rds-proxy)** for high-churn or Lambda workloads |
| **Kerberos / AWS Managed Microsoft AD** | For SQL Server domain authentication |

**5. Authorization inside the database is still yours.** AWS controls who can *reach* the database; it has no opinion on your schema grants. The app should use a least-privilege database user — **not `sa`, not the RDS master user** — and that's a very common real-world gap.

**6. Auditing — know which tool answers which question:**
- **CloudTrail** logs the **API** (`CreateDBInstance`, `ModifyDBInstance`, `DeleteDBInstance`) — *not* the SQL anyone ran.
- **Engine logs** (error, slow query, audit, general) can be **published to CloudWatch Logs** for retention, metric filters, and alarms.
- **Database Activity Streams** (Aurora, plus RDS for Oracle/SQL Server) give a near-real-time, **tamper-resistant** stream of database activity to Kinesis — the point being that even a DBA with full privileges cannot erase their own trail. That's the answer to "how do you audit *privileged* database access?"

**7. Patching and lifecycle** — AWS patches the engine in your **maintenance window**; you own the window choice and the `AutoMinorVersionUpgrade` flag. Enable **deletion protection** and always take a **final snapshot**; back the whole thing with **AWS Backup** so retention is policy-driven rather than per-instance.

**8. Performance Insights** deserves a security note: it can surface **query text**, which may contain sensitive literals — it has its own KMS key and IAM permissions, so treat access to it as data access.

**One-liner:** *"Private subnets and SG-to-SG rules so it's unreachable from the internet; KMS at creation and forced TLS so the data is encrypted both ways; IAM database auth or Secrets Manager so there's no password in config; least-privilege database users inside; and Database Activity Streams plus engine logs in CloudWatch so privileged access is auditable. CloudTrail tells me who changed the instance — it does not tell me who ran the query."*

### RDS Proxy

A fully managed **connection pool** that sits between your application and RDS/Aurora.

**The problem it solves:** a relational database has a hard `max_connections` limit and each connection is expensive (memory, a backend process). **Lambda** is the pathological case — every concurrent execution environment opens its own connection, so a burst to 1,000 concurrent executions tries to open 1,000 connections and the database refuses them all. Traditional connection pooling doesn't help, because each Lambda sandbox has its own pool of one.

**What RDS Proxy gives you:**
- **Pools and multiplexes** connections, so hundreds of clients share a small number of real database connections.
- **Cuts failover time by up to ~66%** — the proxy holds client connections open and re-points them at the new primary, so the application often doesn't see an error at all instead of waiting on DNS propagation and retrying.
- **Enforces IAM authentication** and pulls credentials from **Secrets Manager**, so no database password ever appears in application config.
- Runs **inside your VPC** and is never publicly accessible.

**When to reach for it:** Lambda-to-RDS (almost always), any application with many short-lived connections, and anywhere you want faster, quieter failover. **When not to:** a single long-lived container app that already pools connections well gets little benefit for the extra hop and cost. For .NET specifically, note that ADO.NET/EF Core already pool per process — so RDS Proxy is about *cross-process* pooling and failover smoothing.

#### Pattern: Lambda → RDS Proxy → RDS — Full Explanation

**What the three pieces are:**
| Piece | What it is | The relevant property |
|---|---|---|
| **Lambda** | Serverless compute; each concurrent invocation runs in **its own execution environment** (its own process) | Scales to hundreds or thousands of environments in seconds, each one short-lived |
| **RDS / Aurora** | A managed relational database | Has a **hard `max_connections` ceiling**, and each connection is expensive — PostgreSQL forks a **process per connection** (~5–10 MB each) |
| **RDS Proxy** | A managed connection pool **inside your VPC**, between the two | Holds a small pool of real DB connections and **multiplexes** many client connections onto them |

**Why Lambda + RDS breaks without a proxy — the execution model is the whole answer.**

A normal ASP.NET Core app on EC2/ECS is **one process** with an ADO.NET pool of ~100 connections, reused across thousands of requests. Efficient, because the pool is shared.

Lambda breaks that assumption: **each execution environment handles one invocation at a time and has its own private pool.** So a pool of 100 inside a Lambda is meaningless — it only ever needs one connection, but there are now *N* separate pools where N = your concurrency.

```
CONCURRENCY 500  ->  500 execution environments  ->  500 separate connections
db.t3.medium PostgreSQL max_connections ≈ 420
                        ↓
   FATAL: too many connections / remaining connection slots are reserved
```
And it's not just the ceiling — it's **churn**. Environments are constantly created and destroyed, so you get a storm of connect/TLS-handshake/disconnect cycles, each costing the database real CPU. The database spends its time managing connections instead of answering queries.

**What the proxy changes:**
```
   BEFORE                                AFTER
   Lambda x500                           Lambda x500
      |                                     |
      | 500 connections                     | 500 connections
      v                                     v
   +--------+                          +-------------+
   |  RDS   |  <- refuses them         | RDS PROXY   |  warm pool + multiplexing
   +--------+                          +------+------+
                                              | ~20-50 real connections
                                              v
                                          +--------+
                                          |  RDS   |  comfortable
                                          +--------+
```
Lambda points at the **proxy endpoint** instead of the DB endpoint. That's the only application change.

**❗ Connection pinning — the gotcha that decides whether the proxy actually helps.** Multiplexing only works while a session is **stateless**. If a session does something session-scoped, the proxy must **pin** that client to one DB connection for the rest of the session, and you lose the sharing benefit for it. Common causes:
- Explicit transactions held open across statements
- `SET` session variables, temp tables, advisory locks
- Prepared statements (protocol-dependent), and `USE database` on MySQL

Watch the **`DatabaseConnectionsCurrentlySessionPinned`** CloudWatch metric. If it's high, the proxy is doing little for you and the fix is application-side — keep transactions short, avoid session state. **Naming pinning unprompted is a strong senior signal**, because it's the difference between "I've read the docs" and "I've operated this."

**Setup requirements** (the practical bits that trip deployments up):
- The **Lambda must be VPC-attached** in the same VPC as the proxy. Note this means the function loses default internet access — add a NAT gateway or **VPC endpoints** for any other AWS calls it makes. (The old cold-start penalty for VPC Lambdas was largely removed by Hyperplane ENIs, so that's no longer the objection it once was.)
- The proxy needs a **Secrets Manager secret** holding the DB credentials, plus an IAM role to read it.
- Security groups: Lambda SG → proxy SG on **5432/3306**, and proxy SG → RDS SG.
- **IAM database authentication** removes the password from the app entirely — grant the Lambda's execution role `rds-db:connect` and generate a token instead:
```csharp
var token = RDSAuthTokenGenerator.GenerateAuthToken(
    "my-proxy.proxy-abc123.us-east-1.rds.amazonaws.com", 5432, "app_user");
// use the token as the password; combine with SSL Mode=Require
```
- **Cost:** billed per vCPU of the target DB instance per hour — cheap relative to an outage, but not free, which matters for small workloads.

**When to use it — real use cases:**
1. **Serverless API on Lambda over a relational database.** The canonical case: API Gateway → Lambda → RDS/Aurora with spiky traffic. This is *the* reason RDS Proxy exists.
2. **A relational schema you can't move to DynamoDB** — joins, ad-hoc reporting, an existing EF Core model, or a legacy database other systems also read.
3. **Bursty event-driven writes** — an S3 upload or SQS batch fans out to hundreds of concurrent Lambdas that all need to write to RDS.
4. **Reducing failover blast radius for *any* client, not just Lambda** — during a Multi-AZ failover the proxy re-points connections, so ECS/EC2 apps often see no error rather than a reconnect storm.
5. **Eliminating database passwords** from application config via IAM auth + Secrets Manager rotation.

**When *not* to — and the alternatives worth naming:**
| Situation | Better answer |
|---|---|
| Greenfield, simple known access patterns | **DynamoDB** — it's an HTTP API with **no connection concept at all**, so the problem never exists. The strongest answer when the data model allows it |
| You want serverless-to-relational with *zero* connection management | **Aurora Data API** — an HTTPS/IAM endpoint for Aurora, no persistent connections, no VPC attachment needed. Trade-off: higher per-query latency and not suited to chatty workloads |
| One long-lived ECS/EC2 service | Its own in-process pool is already correct; the proxy adds a hop and cost for little gain |
| Very low Lambda concurrency (a few per minute) | May not justify the cost — though **reserved concurrency** set below your connection budget is a blunt, free mitigation that caps the damage |
| Sessions that pin heavily | Fix the application first; the proxy can't multiplex stateful sessions |

**In an interview.** This turns up in three shapes: *"How would you connect Lambda to a relational database?"*, *"Your Lambda intermittently fails with `too many connections` — diagnose it"*, and *"Why is Lambda + RDS often called an anti-pattern?"*

A model answer:
> *"The root cause is Lambda's execution model — each concurrent invocation is a separate process with its own connection, so connection count scales with concurrency and there's no shared pool to help. At a few hundred concurrent invocations you exhaust `max_connections` on a small instance, and the churn of connect/disconnect cycles costs the database real CPU on top. The fix is **RDS Proxy**, which holds a warm pool and multiplexes many client connections onto a few real ones, and as a bonus cuts failover time by re-pointing connections instead of making clients reconnect. Two caveats I'd check: **pinning** — if sessions hold transactions open or use session state, the proxy pins connections and the benefit disappears — and the fact that the Lambda has to be VPC-attached, so it needs a NAT gateway or VPC endpoints for other AWS calls. If it were greenfield and the access patterns were simple, I'd ask whether **DynamoDB** is the right store instead, since it has no connection model at all."*

**Follow-ups to be ready for:** *Why not just raise `max_connections`?* (each connection costs memory; you'd starve the DB of the RAM it needs for buffers, and you're treating a symptom). *Does the proxy help cold starts?* (not directly, but it removes the TLS+auth handshake to the database from the critical path). *What about Aurora Serverless?* (it scales compute, not the connection model — you still want the proxy, or the Data API). *How do you know pinning is happening?* (the CloudWatch metric above).

**What's actually being tested:** whether you understand that **Lambda is not a web server** — that concurrency means *processes*, not threads — rather than reciting "use RDS Proxy". Candidates who explain *why* the pooling assumption breaks always land better than those who name the service.

*Framing note for me: my Lambda work has been **DynamoDB**-backed (see [Resume Deep-Dives](00-resume-aligned-priority-map.md#resume-deep-dives--the-follow-ups-i-should-expect)), so I have not hit this in production. I'd present it as reasoning and design knowledge, and say so — the honest version lands better than implying operational experience I don't have.*

### ECS → RDS: The Four Layers of a Database Connection

> Interactive version of this walkthrough: [ecs-to-rds-connection-explained.html](../../assets/ecs-to-rds-connection-explained.html) — open in a browser.

**The single most useful idea here:** a task reaching a database crosses **four independent layers — network, DNS, credentials, pool — and they fail independently, with different symptoms.** A timeout is never a credentials problem, and an `AccessDenied` is never a security-group problem. Naming the layer before touching config is what actually cuts debugging time.

```text
  ┌─ VPC — one per account, platform-provisioned ──────────────────────────────┐
  │  ┌─ PRIVATE SUBNETS — no NAT, no IGW anywhere in this path ─────────────┐  │
  │  │                                                                      │  │
  │  │   ┌─ EC2 container instance (ECS capacity provider) ─────────────┐   │  │
  │  │   │  ECS task · bridge network mode                              │   │  │
  │  │   │  app + AWS SDK reads env vars: DB host, secret ARNs,         │   │  │
  │  │   │  vault type = Secrets Manager                                │   │  │
  │  │   │                    │                                         │   │  │
  │  │   │   ①  task-role creds  ──►  169.254.170.2 (link-local)        │   │  │
  │  │   │   ②  GetSecretValue(ARN)  ──►  AWS Secrets Manager           │   │  │
  │  │   │   ③  kms:Decrypt          ──►  AWS KMS (customer key)        │   │  │
  │  │   │                    │  ①–③ are HTTPS AWS API calls           │   │  │
  │  │   │  ┌─ Instance ENI ──┴──────────────────────────────────────┐  │   │  │
  │  │   │  │  bridge mode ⇒ the task has NO ENI of its own, so THIS │  │   │  │
  │  │   │  │  is the source IP *and source SG* of all DB traffic    │  │   │  │
  │  │   │  └────────────────────────┬──────────────────────────────┘  │   │  │
  │  │   └───────────────────────────┼─────────────────────────────────┘   │  │
  │  │                               │  ④ traffic leaves via instance ENI  │  │
  │  │   ┌─ DATABASE SECURITY GROUP — the only gate on this path ──────┐   │  │
  │  │   │  ingress: TCP <db port>  source = the FLEET SG (SG-to-SG)   │   │  │
  │  │   │  no matching rule ⇒ packets silently dropped ⇒ client hangs │   │  │
  │  │   └───────────────────────────┬─────────────────────────────────┘   │  │
  │  │                               │  ⑤ TCP connect — VPC-local route,   │  │
  │  │                               ▼     never NAT, never the internet   │  │
  │  │   ┌─ Amazon RDS (Oracle) ───────────────────────────────────────┐   │  │
  │  │   │  DB subnet group: ≥2 subnets across ≥2 AZs, own subnet tier │   │  │
  │  │   │  publicly_accessible = false · endpoint ⇒ PRIVATE IP        │   │  │
  │  │   └─────────────────────────────────────────────────────────────┘   │  │
  │  └──────────────────────────────────────────────────────────────────────┘  │
  └────────────────────────────────────────────────────────────────────────────┘
```

**Steps ①–③ are AWS API calls over HTTPS; steps ④–⑤ are the actual database connection and never leave the VPC.**

| Layer | What it needs | Symptom when broken |
|---|---|---|
| **Network** | The **database SG** must allow ingress on the DB port **from the compute SG**. Same VPC ⇒ local routing, no NAT/IGW | **Hangs, then times out.** Packets are dropped, so nothing ever replies |
| **DNS** | App uses the **endpoint hostname**; the VPC resolver (VPC base **+2**) returns a private IP. Needs DNS support/hostnames enabled on the VPC | Resolution error — or, from outside the VPC, resolving to a *public* IP |
| **Credentials** | Task role needs `secretsmanager:GetSecretValue` on the ARN **and** `kms:Decrypt` on the encrypting key | `AccessDenied` |
| **Pool** | `tasks × pool size` must fit under the server's **session limit** | Session-limit errors — **but only under load** |

⚠️ **Triage shortcut:** **timeout → network.** **Connection refused → wrong port.** **Any error message *returned by the database* → you got through the network**, so skip straight to credentials.

**Why the source is the instance's SG, not the task's:** in **bridge** network mode the task has no ENI of its own, so the container instance's primary ENI supplies both the source IP and the source security group. That is why the database's ingress rule is written *"from the fleet SG"* — and why every task on that cluster shares one identity at the network layer. With `awsvpc` mode each task gets its own ENI and its own SG, which is what makes per-service database access possible at all (see [ECS](09-containers-ecs-fargate.md#ecs-elastic-container-service)).

**SG-to-SG referencing is itself a signal:** it only works **inside one VPC** (or same-region peering). If the rule references a security group rather than a CIDR, the compute and the database are in the same VPC by construction — no peering, no NAT, no route table to check.

**Multi-AZ failover and pooled connections:** failover **repoints DNS**, but existing pooled connections stay bound to the **dead IP** and keep failing until something recycles them. This is why a connection pool needs a validation query and a bounded connection lifetime, and it is the strongest argument for [RDS Proxy](#rds-proxy) in front of a pool you do not control.

#### Two IAM roles, because there are two secret-delivery patterns

The role confusion here is the most common cause of a permission error that looks impossible — *the policy is correct, but attached to the wrong role.*

| | **Pattern A — ECS-injected** | **Pattern B — runtime SDK fetch** |
|---|---|---|
| Mechanism | The task definition's `secrets` block; the **ECS agent** resolves the secret **before the container starts** and injects it as a plain environment variable | The container starts with only an **ARN** in the env var; the **application** calls Secrets Manager while running |
| Role used | **Execution role** — the role the *agent* assumes (also used for pulling images) | **Task role** — the role your *application code* assumes |
| Needs | `GetSecretValue` (+ `kms:Decrypt`) on the execution role | `GetSecretValue` **and** `kms:Decrypt` on the task role |
| Trade-off | Value is visible in `describe-tasks` and `docker inspect`; **rotation needs a task restart** | Password is **never an environment variable**; the app can **re-fetch after rotation without a redeploy** |
| Typical use | Third-party licence keys and similar low-churn values | **Database credentials** |

**How the SDK finds the task role without any code:** the agent exposes a link-local credentials endpoint at **`169.254.170.2`**, and the SDK picks it up automatically from `AWS_CONTAINER_CREDENTIALS_RELATIVE_URI`. Nothing in the application configures it.

⚠️ **A missing KMS grant is the #1 cause of an `AccessDenied` that "should" have worked** — the secret's own policy reads fine, but a customer-managed key means reading the secret requires `kms:Decrypt` as well.

#### The connection budget — the gap nobody wires up

Connections are held **per task**, so the total scales with autoscaling:

```text
total connections = task count × pool size per task
```

Nothing in a typical configuration connects those two numbers, which means **scaling out quietly increases database pressure** and the failure only appears under load. Derive the ceiling instead of hardcoding it — the deploy stack already injects the autoscaling capacity into the container (`ASG_CAPACITY_MIN` / `ASG_CAPACITY_MAX`):

```text
max pool per task = floor(session budget for this app / ASG_CAPACITY_MAX)
```

**Why this is the right shape:** the ceiling **falls automatically** when someone raises `autoscaling.capacity.max`. A hardcoded pool size silently breaks the invariant instead.

**Worked example:** session budget 300, max tasks 7 → `floor(300 / 7) = 42` connections per task. Leave headroom for admin sessions and for **deployments**, during which old and new tasks briefly overlap — so plan for `max + minimum_healthy_percent` worth of tasks, not just `max`.

#### The change worth making: stop the compute stack editing the database's SG

A common anti-pattern: the **compute** stack writes an ingress rule onto a security group the **database** stack owns. Terraform sees no conflict, so two consumer stacks can race, and the database team has no way to enumerate who depends on them.

| | **Today** | **Target — the "client SG" pattern** |
|---|---|---|
| Who owns the rule | Compute stack writes into the database's SG | **Database stack owns both SGs and the rule between them** |
| Source of ingress | The shared **fleet SG** — so *every* service on the cluster can reach the database | A dedicated, **empty `db-client` SG** that acts as a **capability token** |
| Compute stack's job | Writes a rule it doesn't own | **Attaches** `db-client`, never writes |
| Auditing | Hunt through consumer repositories | **List who has `db-client` attached** |
| Revoking access | Find and delete rules across repos | **One rule** |

**Interview framing:** *"The database's security group is a contract the database team owns. If consumers write into it, you've inverted the ownership — you can't audit dependants and you can't revoke cleanly. The fix is an empty client SG that the database stack trusts: attaching it is the grant, detaching it is the revocation, and `describe-network-interfaces` filtered on that SG is your dependant list."*

---

## 3. Aurora

### Aurora Advanced Features

Beyond the storage-layer architecture described above, these are the Aurora features that turn a basic answer into a strong one:

| Feature | What it does |
|---|---|
| **6 copies across 3 AZs** | Every write is replicated six ways. Aurora tolerates losing **2 copies for writes** and **3 copies for reads** with no availability impact, and self-heals bad blocks |
| **Up to 15 read replicas** | vs 5 on standard RDS, with much lower lag because replicas read the *shared* storage volume rather than replaying logs |
| **Reader / writer / custom endpoints** | The **writer endpoint** always points at the current primary (failover is transparent); the **reader endpoint** load-balances across replicas; **custom endpoints** target a chosen subset — e.g. route heavy reporting queries to two larger replicas so they can't affect the API's replicas |
| **Aurora Serverless v2** | Scales capacity in fine-grained ACUs in **under a second**, from a fraction of a unit to hundreds — for spiky, unpredictable, or dev/test workloads where a fixed instance is either too small or mostly idle. (v1 scaled slowly and paused; v2 is the version to reference) |
| **Aurora Global Database** | One primary region plus up to 5 secondary read-only regions, with **typical replication under 1 second** and cross-region failover usually **under a minute**. This is the engine behind Warm Standby / Active-Active DR — see [Disaster Recovery Strategies](18-well-architected-resilience.md#disaster-recovery-strategies) |
| **Database cloning** | A copy-on-write clone of a whole database in minutes with almost no extra storage cost — the right way to give QA or a data scientist production-like data without a restore |
| **Backtrack** | Rewinds the cluster in place to a point in time **without a restore** (MySQL-compatible). Recovery from a bad migration in minutes rather than hours |
| **Fast database cloning + zero-downtime patching + Blue/Green Deployments** | Managed blue/green creates a synchronised copy of the cluster for you to upgrade and test, then switches over in ~a minute |
| **Aurora Machine Learning** | Call SageMaker/Comprehend directly from SQL — e.g. inline fraud scoring |

**One-liner:** "Aurora is RDS-compatible on the wire but a different animal underneath — compute separated from a distributed, self-healing, 6-way-replicated storage layer, which is where the low replica lag, 15 replicas, instant cloning, backtrack, and fast failover all come from."

---

## 4. Caching

### ElastiCache & Caching Patterns

**What it is:** managed in-memory caching — **Redis** or **Memcached** — delivering sub-millisecond reads. Its main job in an architecture is taking read load off a database and making expensive computations cheap to repeat.

**Redis vs Memcached — a table you should be able to reproduce:**
| | **Redis** | **Memcached** |
|---|---|---|
| Data structures | Rich: strings, lists, sets, **sorted sets**, hashes, streams, bitmaps, HyperLogLog | Simple key/value strings only |
| Persistence | ✅ Snapshots + AOF | ❌ Purely in-memory |
| Replication / HA | ✅ Read replicas, **Multi-AZ with automatic failover** | ❌ No replication |
| Backup & restore | ✅ | ❌ |
| Scaling | Cluster mode: shards + replicas | Horizontal sharding across nodes; **multi-threaded** |
| Transactions / Pub-Sub / Lua | ✅ | ❌ |
| Choose it for | Almost everything: session store, leaderboards, rate limiting, pub/sub, queues | Pure, simple, sharded cache where losing the whole cache is fine and multi-threaded throughput per node matters |

**Default recommendation is Redis** — the only real reason to pick Memcached is a genuinely simple cache that benefits from its multi-threaded model and needs no persistence or failover.

**Caching patterns to name explicitly:**
- **Lazy loading / cache-aside** — check the cache; on a miss, read the database and populate the cache. Only cached data is ever requested data, but every miss pays the full latency and there's a **cache stampede** risk when a hot key expires (mitigate with a short lock or staggered TTLs).
- **Write-through** — write to the cache and the database together. Reads are always warm, but you cache data nobody may read and every write is slower.
- **Write-behind** — write to the cache and flush to the database asynchronously. Fastest writes, but risks data loss on node failure.
- **TTL / eviction** — every cached item needs an expiry, and the eviction policy (`allkeys-lru` etc.) decides what goes when memory fills. **A cache with no TTL strategy is a stale-data bug waiting to happen** — that's the sentence to say.

**Concrete uses:** database query result caching, **session state** for a stateless web tier (the correct alternative to [sticky sessions](08-load-balancing-autoscaling.md#sticky-sessions-session-affinity)), **rate limiting** counters, **leaderboards** via Redis sorted sets, distributed locks, and pub/sub fan-out. In .NET this is `IDistributedCache` via `AddStackExchangeRedisCache`, and `HybridCache` in .NET 9 for a combined in-process + distributed tier.

**Operational notes:** ElastiCache lives in your VPC with a security group on port **6379** (Redis) or **11211** (Memcached); it's **not** publicly reachable. Enable **encryption in transit/at rest** and **Redis AUTH** or RBAC. Use **cluster mode** to shard beyond one node's memory, and **Multi-AZ with automatic failover** for production. **MemoryDB for Redis** is the durable variant — multi-AZ transaction log, usable as a *primary* database rather than just a cache.

**The cache-invalidation question:** the honest senior answer is that invalidation is the hard part, and the strategy depends on tolerance for staleness — short TTLs for cheap eventual correctness, explicit invalidation on write for correctness-critical data, and versioned cache keys (`user:123:v7`) to sidestep deletion entirely.

**DAX (DynamoDB Accelerator)** — the DynamoDB-specific cache, and worth distinguishing from ElastiCache because interviewers ask which you'd use:
- An **in-VPC, write-through cache cluster that is API-compatible with DynamoDB**, taking eventually-consistent reads from single-digit **milliseconds to microseconds**.
- **The key advantage: no application caching logic.** You point the DAX client at the cluster instead of the DynamoDB endpoint and cache-aside handling, invalidation, and population all disappear — because DAX sits transparently in front of the table.
- Two caches inside it: an **item cache** (for `GetItem`/`BatchGetItem`) and a **query cache** (for `Query`/`Scan` result sets), each with its own TTL.
- **Limitations to state:** it only helps **eventually-consistent** reads (a strongly-consistent read passes straight through to DynamoDB), it's a cluster you pay for by the hour rather than serverless, and writes go through to the table so it doesn't accelerate them.

**DAX vs ElastiCache for a DynamoDB workload:** DAX when you want to cache DynamoDB reads with **zero code change** and you're fine with eventual consistency. ElastiCache when you need to cache something *other* than raw table reads — computed aggregates, joined/enriched objects, session state, rate-limit counters, leaderboards — or you want full control over keys and eviction. **A read-heavy DynamoDB table hitting a hot partition or RCU cost ceiling → DAX** is the answer that shows you know the purpose-built option exists.

#### ElastiCache — Pitfalls

**❗ 1. Cache stampede (thundering herd).** A popular key expires and hundreds of concurrent requests all miss at once, hitting the database simultaneously — so the cache *causes* the outage it was meant to prevent. Three mitigations worth naming: **jittered TTLs** (so keys don't expire in lockstep), a **short-lived lock** (`SET key NX EX 5`) so exactly one caller repopulates while the rest wait or serve stale, and **serve-stale-while-revalidate**. This is the single most likely caching question.

**❗ 2. Hot key.** A key lives on **exactly one shard**, so one disproportionately popular key saturates a single node no matter how many shards you add — **cluster mode does not help.** Fix with a small **client-side/in-process cache** for that key (in .NET, `HybridCache` or `MemoryCache` in front of Redis), or by splitting it into `leaderboard:{0..9}` and merging. Same shape as a DynamoDB hot partition.

**3. No TTL means stale data forever *and* memory exhaustion.** Every entry needs an expiry, and the cluster needs an eviction policy. The default **`noeviction`** makes *writes start failing* once memory is full — for a cache you almost always want **`allkeys-lru`**. Leave headroom (`reserved-memory-percent` ~25%) so failover and background saves have room to fork.

**4. Watch the right metrics.** `CacheHitRate` (a low rate means you're paying for a cache that isn't working), **`Evictions`** (rising = too small or no TTLs), `DatabaseMemoryUsagePercentage`, `SwapUsage`, and `CurrConnections`. A cache with a 20% hit rate is worse than no cache — it adds a hop and still hits the database.

**5. Failover is not transparent to the client.** On primary failover the endpoint behind the DNS name changes, and clients must reconnect. Use the **configuration endpoint** (cluster mode) or the primary/reader endpoints — never a node's own address. In .NET, `ConnectionMultiplexer` should be a **singleton** created once with `abortConnect=false`, plus a retry policy; without that flag an app can permanently fail to start if Redis is briefly unavailable at boot.

**6. Redis is single-threaded — one slow command blocks everything.** `KEYS *` on a large keyspace, a big `DEL` of a huge collection, or an expensive Lua script stalls **every** other client. Use **`SCAN`** instead of `KEYS`, and **`UNLINK`** instead of `DEL` for large values. This is a real production-incident answer.

**7. Big keys and serialization cost.** A multi-megabyte value causes latency spikes and skews shard memory. And caching a large object graph can cost more in serialize/deserialise than the database query it replaced — measure rather than assume the cache is faster.

**8. ❗ It is a cache, not a database.** Node failure loses whatever wasn't replicated or persisted. If you need durability, that's **MemoryDB for Redis** (multi-AZ transaction log), not ElastiCache. Teams that quietly promote ElastiCache to a system of record discover this during a failover.

**9. Connection pressure from Lambda** — the same shape as the [RDS Proxy problem](#pattern-lambda--rds-proxy--rds--full-explanation): each concurrent execution environment opens its own connection. Redis tolerates far more connections than a relational database, but reuse the multiplexer **outside the handler** rather than connecting per invocation.

**10. Some settings are creation-time only.** **In-transit encryption** and cluster mode can't be toggled on an existing cluster — changing them means a new cluster and a migration. Decide before you provision.

**11. Cost.** `cache.t*` nodes are **burstable** with CPU-credit mechanics like the EC2 T-family, so a steadily busy cache on a `t3` will throttle. Reserved nodes cut cost for steady workloads, and right-sizing matters because you pay per node-hour whether the cache is being hit or not.

**Disaster Recovery — RDS, Aurora & ElastiCache**

| | |
|---|---|
| **What's actually at risk** | Relational data. Cache is rebuildable — *unless* you've quietly made Redis a system of record, in which case it needs real backups |
| **Backup mechanism** | **Automated backups + PITR** (up to 35 days), **manual snapshots** (kept indefinitely), **cross-region automated backup replication**, **cross-region read replicas**, **Aurora Global Database**, Redis RDB snapshots, **AWS Backup** |
| **Realistic RPO / RTO** | Multi-AZ failover 60–120s (HA, not DR). PITR RPO ~5 min, RTO 10s of minutes. **Aurora Global: RPO <1s, failover <1 min** |

**Recovery runbook:**
1. **AZ failure:** nothing to do — Multi-AZ fails over to the standby automatically and the **endpoint DNS name stays the same**. This is why apps connect to the endpoint, never to an IP.
2. **Regional failure — promote the read replica.** This is the core move:
   ```
   aws rds promote-read-replica --db-instance-identifier orders-dr-replica
   ```
   Promotion breaks replication and makes it a standalone writable primary (a minute or two). For Aurora Global Database use `failover-global-cluster` instead — it's faster and keeps the cluster topology.
3. **Repoint the application** by updating the connection string **in Secrets Manager / SSM**, not in app config — then the app picks up the new endpoint on its next secret refresh with no redeploy. Alternatively keep a Route 53 CNAME in front of the DB endpoint and just re-point the CNAME.
4. **Logical corruption (a bad `DELETE` or migration):** replicas are useless here — restore via PITR to a **new** instance just before the bad statement, verify, then cut over.

⚠️ **The gotcha, and it's the most commonly missed thing in this whole section:** **Multi-AZ is not disaster recovery.** It is same-region HA against hardware/AZ failure, and the standby is a *synchronous replica* — so a bad `DELETE FROM orders` is replicated to it instantly and faithfully. Only PITR and snapshots protect against logical damage. Second trap: **automated backups are deleted when you delete the instance** unless you take a final snapshot — so a "clean up the old instance" ticket can destroy your only recovery point.

---

## 5. Analytics

### Athena

**Serverless, interactive SQL directly over data in S3.** No servers, no clusters, no loading — you define a table over S3 objects and query them where they sit (Presto/Trino under the hood).

- **Pricing is per TB of data scanned** (~$5/TB), which makes cost a *query-design* problem, not an infrastructure problem. Three levers cut it dramatically:
  1. **Columnar formats** — Parquet/ORC instead of CSV/JSON, so only the columns you select are read (often 10× less scanned).
  2. **Partitioning** — lay data out as `s3://bucket/logs/year=2026/month=08/day=08/` so a `WHERE year=2026 AND month=08` clause skips everything else. **Partition projection** avoids the metadata lookup entirely for time-series layouts.
  3. **Compression** (Snappy/GZIP) and avoiding `SELECT *`.
- Schema comes from the **AWS Glue Data Catalog** (a Glue crawler can infer it automatically).
- **Federated queries** can reach beyond S3 into RDS/DynamoDB/CloudWatch via connectors.
- Results land back in an S3 "query results" bucket — remember a lifecycle rule on it.

**Athena vs Redshift** — the standard comparison: Athena is serverless, pay-per-query, best for **ad-hoc and infrequent** analysis over a data lake with no ETL; Redshift is a provisioned (or serverless) **warehouse** with its own optimised storage, best for **frequent, complex, high-concurrency BI** where sustained query volume makes a cluster cheaper and faster. "Occasional queries over S3 logs → Athena; a dashboard hundreds of analysts hit all day → Redshift."

**Where Athena shows up in practice:** querying **CloudTrail** logs ("who deleted that bucket?"), **VPC Flow Logs**, ALB/S3 access logs, and cost/usage reports. Naming those makes the answer concrete.

**The rest of the analytics family in one line each:** **Glue** — serverless ETL plus the Data Catalog every other service reads. **Redshift** — the warehouse (Redshift Serverless removes the cluster sizing decision; Spectrum queries S3 directly). **EMR** — managed Hadoop/Spark for heavy custom processing. **QuickSight** — serverless BI dashboards with SPICE in-memory acceleration, the visualisation layer over Athena/Redshift. **Kinesis Data Analytics / Managed Flink** — SQL/Flink over streaming data. **Lake Formation** — permissions and governance over a data lake.

---

← [Containers: Docker, ECS, ECR & Fargate](09-containers-ecs-fargate.md) · [Index](README.md) · [Networking](11-networking.md) →
