> **AWS Quick Revision Notes** · [Index](README.md) · Part I

# DynamoDB

**Q: Kya hai?** A: Fully managed NoSQL key-value/document, single-digit-ms latency at scale — *agar* key design correct. Schema-less; correctness burden app par.

**Partitioning:** PK hash → physical partition (~3,000 RCU/1,000 WCU per partition, directional). Good PK = high cardinality + even distribution. Bad → **hot partition** throttling (table metrics healthy dikhte hue). **Adaptive Capacity** smooth karta hai par bad design fix nahi.

**Sort Key unlocks:** range queries, time-series, one-to-many, hierarchical single-table, sorted views.

---

## 1. DynamoDB Core

### GSI vs LSI

Base table sirf PK se query; har dusra access pattern → secondary index.
- **LSI = wahi drawer, re-sorted.** **GSI = naya cabinet, re-filed.**

| | GSI | LSI |
|---|---|---|
| Partition key | **Kuch bhi** | Base jaisa hi |
| Physically | Alag table, async replicated | Same partition |
| Created | Kabhi bhi (add/drop) | **Sirf table creation par** |
| Consistency | **Sirf eventual** | Strongly consistent ho sakta |
| Capacity | Apni | Base share |
| Max | 20 | 5 |
| Size limit | Koi nahi | ⚠️ **10 GB per PK value** |

**Do settling constraints:** LSI 10 GB ceiling (`ItemCollectionSizeLimitExceededException`) + creation-time-only → ~95% designs GSI use karte hain. Throttled GSI base table writes bhi throttle karta hai (provisioned).

**Projections (write-amp lever):** `KEYS_ONLY` (cheapest) / `INCLUDE` (usually right) / `ALL` (most). ⚠️ Non-projected attribute maango → silent **fetch-back** from base table (extra RCU, no error).

**Capacity modes:** On-Demand (spiky/unknown, RRU/WRU) vs Provisioned+Auto Scaling (stable, cheaper, reserved capacity).

**Query vs Scan:** Query = PK-targeted, O(matched). Scan = full table, filter after — avoid in hot paths.

### Conditional Writes (superpower)

Server-side atomic check inside item's lock → no distributed lock needed.
- **Idempotency:** `PutItem ConditionExpression: attribute_not_exists(idempotencyKey)`, `ReturnValuesOnConditionCheckFailure=ALL_OLD` (existing item in exception, saves GetItem). `ConditionalCheckFailedException` = expected success path for duplicates.
- **State machine / compare-and-swap:** `UpdateItem ... ConditionExpression: #s = :pending` → exactly one winner.
- **Version variant:** `ConditionExpression: version = :expectedVersion`.
- **Cost:** conditional write = 1 WCU (condition free, even on fail); `TransactWriteItems` = **2 WCU** per item.
- **Kaafi nahi kab:** condition ek item par, write dusre par → `TransactWriteItems` + `ConditionCheck`.

**Transactions:** up to 100 items/tables ACID (4 MB), ~2× capacity — sirf genuine all-or-nothing invariants.

**TTL:** attribute-driven, best-effort async delete (ghanton, up to ~48h) — sessions/dedup; compliance deletion ke liye NAHI.

**Streams:** time-ordered change log (~24h) — CQRS projections, event pipelines, CDC/audit/search-sync.

**Global Tables:** multi-region active-active, **last-writer-wins**, async — idempotent writes.

**Single-table design:** multiple entities in one table via PK/SK; fewer round-trips, no joins. `PK=ORDER#123 SK=META#/ITEM#1/EVENT#<ts>` → one Query = full aggregate.

**400 KB item limit:** large blobs → S3, pointer in DynamoDB; ya vertical partition.

### Capacity Maths

| Unit | Gives | Rate |
|---|---|---|
| 1 RCU | strong read ≤4 KB | 1/s |
| 1 RCU | 2 eventual reads ≤4 KB | 2/s |
| 1 RCU | 0.5 txn read | 0.5/s |
| 1 WCU | write ≤1 KB | 1/s |

- **Round UP per item.** 5 KB strong read = 2 RCU; 0.5 KB write = 1 WCU; 1.5 KB write = 2 WCU.
- Reads 4 KB, writes 1 KB → writes 4× costlier per byte (denormalise reads, minimise write amp).
- Filtered Scan cost = unfiltered (charged on data read **before** filter).
- Provisioned = RCU/WCU (per-second); On-demand = RRU/WRU (per-request).

**Write sharding (hot fix):** `PK = ORDER#2026-08-10#<hash%10>` — spreads writes; reads now query+merge all shards. Only where hotspot real.

**GSI overloading** (`GSI1PK`/`GSI1SK` generic keys serving many patterns) + **sparse index** (item appears only if it has key attr → tiny work-queue index).

### DynamoDB in .NET

| Layer | Type | Kab |
|---|---|---|
| Low-level | `AmazonDynamoDBClient` + `AttributeValue` | Full control, conditions, txns, single-table |
| Document | `Table`+`Document` | Schema-flexible |
| Object persistence | `DynamoDBContext` + `[DynamoDBTable]` | Simple 1-entity-per-table (poor fit for single-table) |

- ❗ **Client = singleton** (thread-safe), handler ke bahar in Lambda.
- GSI query: `KeyConditionExpression = "GSI1PK = :pk AND begins_with(GSI1SK, :prefix)"`, `IndexName="GSI1"`. Never Scan hot path.
- Atomic counter: `SET #v = #v + :inc, ConditionExpression: #v < :max`. `[DynamoDBVersion]` = auto optimistic locking.
- **Pagination loop:** `LastEvaluatedKey` khatam hone par **empty dict** (not null) — `start = page.LastEvaluatedKey?.Count > 0 ? ... : null`.
- `BatchWriteItem` 25 items, partial success → re-submit `UnprocessedItems` with backoff (no atomicity).
- .NET gotchas: `decimal` not `double` for money; long-lived `DynamoDBContext`; SDK auto-retries throttling (`MaxErrorRetry`); `ReturnConsumedCapacity` for tuning.

---

## 2. Trick Questions & Pitfalls

### Trick Questions

Relational? No. Scan faster? No. PK duplicate? Yes if SK different. Strongly consistent default? No (eventual; GSI never strong). GSI add after creation? Yes; LSI no. Max item? 400 KB. Txns extra cost? ~2×. TTL instant? No.

**Worked example (Deployment Dashboard):** access patterns first (D1-D6); `PK=SERVICE#dms-export SK=DEPLOY#<ts>#<runId>`; `SERVICE#x SK=CURRENT#prod` pointer (hottest read); denormalise display fields; **D2 "in-flight"** → sparse GSI (`GSI1PK=IN_FLIGHT`, remove attr on terminal); write-once pointer for `RUN#8871` (no GSI, key never changes); big Terraform plan → S3, key in DDB; conditional writes for webhook retries. Terraform's own S3 lock table = `PutItem attribute_not_exists(LockID)` → crashed apply = stale lock → `terraform force-unlock`.

**DR — DynamoDB:** risk = table/GSI + 24h stream window. Backup = **PITR** (35 days, any second), on-demand backups, AWS Backup (cross-acct/region), Global Tables. PITR RPO ~5min, Global ~1s; RTO = restore builds **new table** (minutes→hours). Repoint via SSM param (not hardcoded). ⚠️ No in-place restore; PITR must be enabled **before** incident.

---

← [Serverless & Lambda](01-serverless-lambda.md) · [Index](README.md) · [IAM & Security](03-iam-security.md) →
