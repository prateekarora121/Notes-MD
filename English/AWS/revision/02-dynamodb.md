> **AWS Quick Revision Notes** · [Index](README.md) · Part I

# DynamoDB

---

## 1. DynamoDB Core

### Deep Dive

**What:** fully managed NoSQL key-value/document, single-digit-ms latency at unlimited scale *if key design is correct*. Schema-less → correctness burden on app.

**Partitioning:** PK hash → physical partition (bounded, ~3,000 RCU/1,000 WCU per partition — directional, not contractual). Good PK = **high cardinality + even access**. Bad PK (low cardinality, time-based, "celebrity" key) → **hot partition** → throttling while table metrics look fine. **Adaptive Capacity** smooths but doesn't fix bad key design.

**Sort Key unlocks:** range queries, time-series, one-to-many, hierarchical single-table (`PK=ORDER#555, SK=META#/ITEM#1/EVENT#CREATED`), sorted views. Mental model: PK = which drawer, SK = order inside drawer; you can only open a drawer whose label you know.

**GSI vs LSI:**
| | GSI | LSI |
|---|---|---|
| Partition key | **Anything** | **Must match base** |
| Sort key | Own, optional | Different from base |
| Stored | Separate table, async replicated | Same partition as base item |
| Created | Any time; droppable | **Only at table creation** |
| Consistency | **Eventual only, never strong** | **Can be strong** |
| Capacity | Own RCU/WCU | Shares base |
| Max/table | 20 | 5 |
| Size | None | ⚠ **10 GB per partition-key value** across item collection + LSIs |

Two settling constraints: LSI 10 GB ceiling + creation-time-only → **~95% of designs use GSIs**. **A throttled GSI throttles the base table's writes** (provisioned). Reach for GSI unless you specifically need a strongly-consistent re-sort.

**Projections (write-amplification lever):** `KEYS_ONLY` (lowest) · `INCLUDE` (tuned — usually right) · `ALL` (full amplification). **❗ Fetch-back penalty:** querying a non-projected attribute silently reads from base table (extra RCU + latency, no error). Project only what the query renders.

**Capacity modes:** On-Demand (no planning, spiky/unknown, RRU/WRU) vs Provisioned+Auto Scaling (cheaper steady, Reserved Capacity discount).

**Query vs Scan:** Query = PK-targeted, O(matched). Scan = whole table, filter after — expensive, avoid in hot paths; admin/analytics only.

**Conditional writes (underrated):** atomic server-side check inside item's own lock — no distributed lock needed.
```csharp
// Idempotency
ConditionExpression = "attribute_not_exists(idempotencyKey)",
ReturnValuesOnConditionCheckFailure = ReturnValuesOnConditionCheckFailure.ALL_OLD
// catch ConditionalCheckFailedException = duplicate = SUCCESS, return same response (item is in ex)
```
```csharp
// State machine CAS — exactly one winner
UpdateExpression="SET #s = :processing", ConditionExpression="#s = :pending"
// Worker A passes → flips; Worker B now sees PROCESSING → fails → exits clean
```
```csharp
// Version variant
UpdateExpression="SET #data=:d, version=version+:one", ConditionExpression="version=:expectedVersion"
```
The `status` attribute *is* the lock — nothing to expire/orphan/fence/release.

**Cost:** `PutItem`/`UpdateItem` (1 KB) = 1 WCU; **with a condition = still 1 WCU** (condition free); `TransactWriteItems` = **2 WCU/item**. A *failed* condition still costs 1 WCU. Plain condition can't guard a write to *another* item — use `TransactWriteItems` `ConditionCheck`.

**Transactions:** ACID across ≤100 items/tables (4 MB), was 25 pre-Sep 2022. Only for genuine all-or-nothing invariants (~2× cost + latency).

**TTL:** attribute-driven, best-effort async delete (can lag hours, docs note ~48h) — good for idempotency keys/sessions/dedup; **not** for compliance deletion deadlines.

**Streams:** ordered change log (insert/update/delete), ~24h retention. Backbone for CQRS read-model projection, event pipelines, CDC/audit/search-index sync.

**Global Tables:** multi-region multi-active; nearest region; conflict = **last-writer-wins**, async/eventual → design idempotent writes.

**Single-table design:** many entity types via PK/SK convention → fewer round-trips, no joins.

**400 KB item limit** (names+values): big blobs → S3 + pointer in DynamoDB.

**Capacity maths:** 1 RCU = one strong read ≤4 KB (or 2 eventual, or 0.5 transactional); 1 WCU = one write ≤1 KB (0.5 transactional). Reads get 4 KB/unit, writes 1 KB → writes 4× dearer. **Round up per item per op.** E.g. 100 reads/s × 10 KB eventual → ceil(10/4)=3 →÷2=1.5→round=2 RCU ×100 = ~200 RCU. Provisioned = RCU/WCU (per-second); on-demand = RRU/WRU (per-request).

**Write sharding (hot-partition fix):** `PK = ORDER#2026-08-10#3` (shard = hash%10). Trade-off: reads must query all shards + merge — only shard where write hot-spot is real.

**GSI overloading** (generic `GSI1PK`/`GSI1SK` serving many patterns) + **sparse index** (item appears only if it has the key attribute → cheap work-queue index).

**.NET SDK layers:** low-level (`AmazonDynamoDBClient` + `AttributeValue` — full control, single-table); document model (`Table`/`Document`); object persistence (`DynamoDBContext` — one-type-per-table, fits single-table poorly). **❗ Reuse the client as a singleton, create outside the handler.**
- Query a GSI (never Scan hot path): `KeyConditionExpression = "GSI1PK = :pk AND begins_with(GSI1SK, :prefix)"`.
- `[DynamoDBVersion]` = optimistic locking automatic.
- Transactions fail wholesale; `TransactionCanceledException.CancellationReasons` says which.
- **Pagination:** 1 MB/call; loop on `LastEvaluatedKey` — it returns an **empty dict** (not null) when done → `!= null` alone loops forever. SDK v3 `Paginators` handle it.
- `BatchWriteItem` ≤25, can partially succeed → resubmit `UnprocessedItems` with backoff; no atomicity/conditions.
- **Gotchas:** use `decimal` not `double` for money; long-lived `DynamoDBContext`; SDK already retries throttling (tune `MaxErrorRetry`); `ReturnConsumedCapacity` while tuning; reserve Scan for admin (prefer sparse GSI).

**Limitations to state:** no joins/SQL/`LIKE`; uniqueness only on PK; not for reporting (export to Redshift/Athena/OpenSearch via Streams).

---

## 2. Trick Questions & Pitfalls

### Trick Questions

Relational? No. Scan faster than Query? No. PK duplicated? Yes if SKs differ. Strongly consistent by default? No (eventual; GSIs never strong). GSI after creation? Yes (LSI no). Max item? 400 KB. Transactions cost more? ~2×. TTL instant? No (hours).

**Memorize:** you give up "ask anything" for guaranteed speed at scale. Work backwards — write access patterns first, then design. High-cardinality PK, denormalize freely, and never: scan the table, hammer one key, or build unneeded indexes.

**Worked example — Deployment Dashboard:** read-heavy. Wrong PK: `ENV#prod` (4 values, hot) or `STATUS#IN_PROGRESS` (low cardinality + rewritten). Right: `PK=SERVICE#dms-export, SK=DEPLOY#<ts>#<runId>`; plus `CURRENT#prod` pointer (hottest read), `META#`, `RUN#8871→PTR#` (D5), plan in S3 with key in DynamoDB. **Cardinality only matters relative to write rate** (12 services fine at a few deploys/day). Denormalize display fields into deploy item (correct *as-of* deploy time). D2 "what's running now" = **sparse GSI** (`GSI1PK=IN_FLIGHT`, removed at terminal state) not a Scan. One sparse GSI + one date GSI + a write-once pointer (beats a GSI when lookup key never changes). Conditional writes for webhook retries. *Terraform's own S3-backend lock = `PutItem` on `LockID` with `attribute_not_exists`* → crashed apply leaves stale lock → `terraform force-unlock`.

**DR:** at risk = table data, GSIs, 24h Streams window. Backup = **PITR** (continuous, 35 days, any second), on-demand backups (indefinite), AWS Backup (cross-acct/region), Global Tables (~1 s). RPO PITR ~5 min. **RTO is the problem: restore creates a NEW table + rebuilds every GSI (hours for large).** ❗ **Cannot restore in place** → need name-indirection (read table name from SSM/env, never hardcoded). **PITR must be enabled before the incident** — enable in IaC at creation.

---

← [Serverless & Lambda](01-serverless-lambda.md) · [Index](README.md) · [IAM & Security](03-iam-security.md) →
