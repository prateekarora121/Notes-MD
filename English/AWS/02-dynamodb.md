> **AWS Detailed Guide** · [Index](README.md) · Part I

# DynamoDB

> **Tier 1 — bulletproof.** The one AWS data store my resume ties to a shipped product ("DynamoDB-backed microservices"), and it's listed under both *Cloud* and *Databases* skills. Revise this section hardest. Caching for DynamoDB (**DAX**) sits with [ElastiCache & Caching Patterns](10-databases-caching-analytics.md#elasticache--caching-patterns).

---

## 1. DynamoDB Core

### DynamoDB Deep Dive

**What it is:** fully managed NoSQL key-value/document store with consistent single-digit-millisecond latency at effectively unlimited scale — *if* the key design is correct. Schema-less: each item can have different attributes, which suits fast-evolving microservices but shifts correctness burden onto the application.

**Partitioning**
- The **Partition Key (PK)** hash decides which physical partition stores an item. Each partition has bounded throughput (historically documented around 3,000 RCU / 1,000 WCU per partition — exact numbers are internal and AWS doesn't guarantee them contractually, treat as directional).
- A good PK has **high cardinality** and **even access distribution**. Bad PK choice (low cardinality, time-based, or one "celebrity" key getting disproportionate traffic) → **hot partition** → throttling even when table-level metrics look fine.
- **Adaptive Capacity** shifts unused capacity from cold partitions to hot ones automatically — it smooths real-world unevenness but does **not** fix a fundamentally bad key design.

**Sort Key (SK) — what it actually unlocks**
| Pattern | Example |
|---|---|
| Range queries | `PK=USER#123, SK BETWEEN ORDER#2025-01-01 AND ORDER#2025-01-31` |
| Time-series | `PK=DEVICE#A100, SK=<ISO timestamp>`, query `SK > now-1h` |
| One-to-many | `PK=USER#123, SK=ORDER#<date>` (multiple orders under one user) |
| Hierarchical/single-table | `PK=ORDER#555, SK=META# / ITEM#1 / EVENT#CREATED` — fetch whole aggregate in one Query |
| Sorted views | SK encodes priority/rank for deterministic ordering |

**GSI vs LSI**

The base table can only be queried by its PK, so every other access pattern needs a **secondary index** — an alternate view of the same data under a different key, maintained for you by DynamoDB.

**The mental model.** Think of the table as a filing cabinet: the **PK is which drawer**, the **SK is the order of items inside that drawer**. You can only open a drawer whose label you already know.
```
Orders:  PK = customerId    SK = orderDate

drawer "C1" ──▶ [2026-01-05] [2026-03-11] [2026-08-22]     ← sorted by SK
drawer "C2" ──▶ [2026-02-01] [2026-07-30]
```
So *"C1's orders between Jan and Aug"* is a cheap Query, but *"where is order O-500?"* has no drawer label to open — that is a full Scan unless an index gives you a second way in:
- **LSI = the same drawer, re-sorted inside it.**
- **GSI = a whole new cabinet, re-filed under a different label.**

| | GSI (Global Secondary Index) | LSI (Local Secondary Index) |
|---|---|---|
| Partition key | **Anything** — different from base table | **Must match** the base table |
| Sort key | Own, optional | Different from base table |
| Physically stored | A **separate table** behind the scenes, replicated asynchronously — hence "global" | **In the same partition** as the base item — hence "local" |
| Created | Any time; can also be dropped later | **Only at table creation** — never added or removed afterwards |
| Consistency | **Eventual only — never strongly consistent** (different partition + async replication makes it physically impossible) | **Can be strongly consistent** (co-located, so it's achievable) |
| Capacity | Own RCU/WCU | Shares base table capacity |
| Max per table | 20 | 5 |
| Size limit | None | ⚠️ **10 GB per partition-key value**, counted across the base item collection *and* all its LSIs |
| Cost consideration | Every base write may also write to GSI (write amplification) — project only needed attributes | Rarely used because of the creation-time constraint |

**Two constraints that settle the choice in practice:**
- **LSI's 10 GB ceiling** is per partition-key value across the base collection plus every LSI on it. Exceed it and writes start failing with `ItemCollectionSizeLimitExceededException` — so an unbounded collection (every order under `PK=DEALER#7`) eventually bricks itself. Combined with creation-time-only, this is why **~95% of real designs use GSIs**.
- **A throttled GSI throttles the base table's writes** (on provisioned capacity). Under-provisioning an index takes down the entire write path — a surprising failure mode, and a favourite question.

**Which one to reach for:**
```
Need a different partition key?                     → GSI  (an LSI cannot change PK)
Only a different sort order, and you need
strong consistency on that read?                    → LSI
```
Base `Orders` table (`PK=ORDER#123`, `SK=META#`):

| Access pattern | Index | Keys |
|---|---|---|
| One order's items in `createdAt` order | **LSI** | `PK=ORDER#123` (unchanged), `SK=createdAt` |
| All orders for one customer | **GSI** | `PK=CUSTOMER#42`, `SK=2026-08#ORDER#123` — PK changed, so it *must* be a GSI |

**Projections — the write-amplification lever**

An index need not hold the whole item. What you copy into it is the **projection**, and it is the main dial on index write cost:

| `ProjectionType` | Copied into the index | Write cost |
|---|---|---|
| `KEYS_ONLY` | Base table keys + index keys only | Lowest |
| `INCLUDE` | Keys + a named list of attributes | Tuned — **usually the right answer** |
| `ALL` | The entire item | Highest — full write amplification |

⚠️ **The fetch-back penalty:** if a Query asks for an attribute the index does not project, DynamoDB silently reads it back from the base table — extra RCU and extra latency, with no error to signal it. So project deliberately: `INCLUDE` exactly what the query's result rows render, and nothing more.

**One-line answer:** "PK decides which partition an item lands in and SK decides its order within that partition — so any access pattern that isn't *known PK, optional SK range* needs a secondary index. An LSI keeps the base table's partition key and changes only the sort key, so it lives in the same partition: it can be strongly consistent, but it must be created with the table and its item collection is capped at 10 GB per partition key. A GSI can use any partition key, so physically it's a separate asynchronously-replicated table: add or drop it any time, its own capacity, but eventually consistent — always. In practice I reach for a GSI unless I specifically need a strongly consistent re-sort, because creation-time-only plus the 10 GB ceiling make LSIs a long-term liability. On either one the projection is the real cost lever — `INCLUDE` only what the query actually renders, because requesting a non-projected attribute triggers a silent fetch back to the base table."

**Capacity modes**
| | On-Demand | Provisioned (+ Auto Scaling) |
|---|---|---|
| Planning | None | RCU/WCU sizing required |
| Cost | Pay-per-request | Cheaper at steady, predictable volume; supports Reserved Capacity discount |
| Best for | Spiky/unknown traffic (serverless, SQS bursts) | Stable, forecastable load |

**Query vs Scan**
- **Query**: PK-targeted (optionally SK range/filter) — O(matched items), the operation you should always be optimizing for.
- **Scan**: reads the *entire* table/index, filters after the fact — expensive, slow, avoid in hot paths; acceptable only for rare admin/analytics jobs.

**Conditional writes — the underrated superpower**
- Idempotency: `PutItem` with `ConditionExpression: attribute_not_exists(idempotencyKey)`.
- Optimistic locking / state machine transitions: `UpdateItem ... ConditionExpression: status = :pending` before flipping to `PROCESSING` — server-side atomic check, no distributed lock needed.
- This gets you concurrency control **without** paying the 2× cost of full Transactions.

**Why it works:** every single-item write (`PutItem`/`UpdateItem`/`DeleteItem`) is atomic, and DynamoDB evaluates the `ConditionExpression` **server-side, inside the item's own lock**. There is no gap between the read and the write for another request to slip into — that gap is exactly what a distributed lock exists to close, so the condition removes the need for one.

**(a) Idempotency — suppress duplicate deliveries**
```csharp
try
{
    await client.PutItemAsync(new PutItemRequest {
        TableName = "Orders",
        Item = new Dictionary<string, AttributeValue> {
            ["idempotencyKey"] = new(requestId),        // partition key
            ["orderId"]        = new(newOrderId),
            ["status"]         = new("PENDING")
        },
        ConditionExpression = "attribute_not_exists(idempotencyKey)",
        ReturnValuesOnConditionCheckFailure = ReturnValuesOnConditionCheckFailure.ALL_OLD
    });
    return new { created = true, orderId = newOrderId };
}
catch (ConditionalCheckFailedException ex)
{
    // Duplicate — SUCCESS, not an error. Return the SAME response the original gave.
    return new { created = false, orderId = ex.Item["orderId"].S };
}
```
`ReturnValuesOnConditionCheckFailure = ALL_OLD` hands back the existing item **in the exception itself**, saving a follow-up `GetItem`. Worth naming — it's a detail most candidates miss.

**(b) State-machine transition — compare-and-swap, exactly one winner**

The naive version has a race:
```csharp
// ❌ RACE: two Lambdas both read PENDING, both proceed, card charged twice
var order = await GetOrder(id);                 // A reads PENDING; B reads PENDING
if (order.Status == "PENDING") {                // true for both
    await SetStatus(id, "PROCESSING");
    await ChargeCard();                         // charged twice
}
```
The condition collapses read+check+write into one atomic server-side operation:
```csharp
await client.UpdateItemAsync(new UpdateItemRequest {
    TableName = "Orders",
    Key = new() { ["orderId"] = new(id) },
    UpdateExpression    = "SET #s = :processing, lockedBy = :worker, lockedAt = :now",
    ConditionExpression = "#s = :pending",                  // ← the whole trick
    ExpressionAttributeNames  = new() { ["#s"] = "status" }, // 'status' is a reserved word
    ExpressionAttributeValues = new() {
        [":processing"] = new("PROCESSING"),
        [":pending"]    = new("PENDING"),
        [":worker"]     = new(workerId),
        [":now"]        = new(DateTime.UtcNow.ToString("O"))
    }
});
```
Worker A finds `PENDING` → condition passes → flips it. Worker B now finds `PROCESSING` → condition fails → throws immediately and exits cleanly. **Exactly one winner, guaranteed.**

**(c) Version-number variant** — for a generic "did anyone modify this while I was thinking?" check rather than a named state:
```csharp
UpdateExpression    = "SET #data = :d, version = version + :one",
ConditionExpression = "version = :expectedVersion"
```
A failure means someone else wrote in between — re-read and retry.

**Why no distributed lock is needed.** The classic sequence is *acquire lock → read → validate → write → release lock*, and it drags along lock TTL tuning, orphaned locks after a crash, fencing tokens to block stale holders, and one more piece of infrastructure that can go down. A conditional write is that entire block in **one network call** — the `status` attribute *is* the lock, so there is nothing to expire, orphan, fence, or release.

**Cost — where the 2× comes from**
| Operation (1 KB item) | Write cost |
|---|---|
| `PutItem` / `UpdateItem` | **1 WCU** |
| Same **with** a `ConditionExpression` | **1 WCU** — the condition is free |
| `TransactWriteItems` | **2 WCU** per item (two-phase commit: prepare + commit) |

One caveat to state: a **failed** condition still costs 1 WCU — the write didn't happen but you're charged. Under heavy contention that matters, though it stays cheaper than a transaction.

**When a conditional write is *not* enough.** The item boundary is the atomicity boundary, so the one case a plain condition cannot express is *a condition on one item guarding a write to another* — `TransactWriteItems` has a dedicated `ConditionCheck` action for exactly that. See *Transactions* below.

**Transactions (`TransactWriteItems`/`TransactGetItems`)**
- ACID across up to 100 items/tables in one call (4 MB aggregate size limit). Note: this was 25 items until September 2022 — older material and older exam guides still say 25.
- Use only for genuine all-or-nothing business invariants (inventory decrement + order creation, money transfer) — costs ~2× capacity and added latency, so don't reach for it by default.

**TTL:** attribute-driven, best-effort async deletion — can lag by hours in practice (notes explicitly flag up to ~48 hours in some documented cases), not exact-to-the-second. Good for idempotency keys, sessions, dedup records, temporary workflow state. **Do not** rely on TTL for time-sensitive compliance deletion deadlines.

**Streams:** time-ordered change log (insert/update/delete), retained ~24 hours. The backbone pattern for:
- CQRS read-model projections (normalized write table → Streams → Lambda → denormalized read table/GSI)
- Event-driven pipelines (Streams → Lambda → SNS/SQS/EventBridge)
- Change-data-capture / audit trails / search-index sync (OpenSearch, S3)

**Global Tables:** multi-region, multi-active replication; users read/write nearest region; conflict resolution is **last-writer-wins**, replication is asynchronous/eventually consistent — design writes to be idempotent/conflict-tolerant.

**Single-table design**
- Multiple entity types (User, Order, Item, Event) in one table via PK/SK convention — trades upfront modeling effort for far fewer round-trips and no joins.
- Real pattern from the notes: `PK=ORDER#123` with `SK=META# / ITEM#1 / EVENT#<ts>` returns the entire order aggregate (header + items + event history) in a single Query.

**400 KB item limit:** hard cap including attribute names+values. Large objects (images, big docs) → store the blob in S3, keep only a pointer/key in DynamoDB; or vertically partition into multiple items under the same PK.

#### Capacity Maths & Hot-Partition Mitigation

**The unit definitions get asked directly**, and doing the arithmetic out loud separates a real answer from a memorised one.

| Unit | Buys | Effective rate |
|---|---|---|
| **1 RCU** | one **strongly consistent** read of up to **4 KB** | 1/sec |
| **1 RCU** | **two** *eventually consistent* reads of up to 4 KB | 2/sec |
| **1 RCU** | half a *transactional* read of 4 KB (txn reads cost 2×) | 0.5/sec |
| **1 WCU** | one write of up to **1 KB** | 1/sec |
| **1 WCU** | half a *transactional* write of 1 KB (txn writes cost 2×) | 0.5/sec |

Note the asymmetry: reads get **4 KB** per unit, writes only **1 KB** — writes are 4× dearer per byte, which is why the design advice is to denormalise for reads and minimise write amplification.

**Rounding — where marks are lost.** Always round **up**, per item, per operation:
- 5 KB item, strongly consistent read → `ceil(5/4)` = **2 RCU**
- 0.5 KB write → `ceil(0.5/1)` = **1 WCU** (a full KB either way, so many tiny writes waste capacity)
- 1.5 KB write → **2 WCU**
- **Read example:** 100 reads/sec of 10 KB items, eventually consistent → `ceil(10/4) = 3` units strongly consistent → ÷2 for eventual consistency = `1.5` → round up = **2 RCU per read** → × 100 = **~200 RCU**
- **Write example:** 50 writes/sec of 2.5 KB items → `ceil(2.5/1) = 3` WCU each → × 50 = **150 WCU**; make them transactional and it is **300 WCU**

**Naming precision:** RCU/WCU are the **provisioned**, per-second-reserved units. On-demand bills the same 4 KB / 1 KB sizing as **RRU/WRU** (Read/Write *Request* Units) — per request rather than per second reserved. Using the two pairs interchangeably is a small tell.

**One-line answer:** "RCU and WCU are per-second throughput units — one RCU is a strongly consistent read of up to 4 KB, or two eventually consistent ones; one WCU is a 1 KB write. Both round up per item, transactions double them, and GSIs consume their own capacity. The subtleties are that capacity is charged on data read *before* filtering, so a filtered Scan costs the same as an unfiltered one, and that a single partition tops out around 3,000 RCU / 1,000 WCU — so a hot key throttles while table-level metrics still look healthy."

**Write sharding — the fix for a hot partition.** If a partition key has low cardinality (`STATUS#PENDING`, or a date like `2026-08-10`), all traffic lands on one partition and you throttle while table-level capacity looks fine. Add a calculated suffix to spread it:
```
PK = ORDER#2026-08-10#3        // shard = hash(orderId) % 10
```
Writes now spread across 10 partitions. The trade-off to state: **reads must now query all 10 shards and merge**, so only shard where the write hot-spot is real. **Adaptive Capacity** helps automatically but is not a substitute for a well-chosen key.

**GSI overloading and sparse indexes** — two single-table techniques worth naming:
- **Overloading**: one GSI whose keys are generic (`GSI1PK`/`GSI1SK`) serves several different access patterns because different entity types write different values into them.
- **Sparse index**: an item only appears in a GSI if it *has* the index's key attribute. So writing `GSI1PK` only on unprocessed orders gives you an index containing *just* the work queue — cheap to scan, because it's tiny by construction.

#### DynamoDB in .NET — The Code You'd Be Asked to Write

**Three SDK layers, and when to use each:**
| Layer | Type | Use when |
|---|---|---|
| **Low-level** | `AmazonDynamoDBClient` + `AttributeValue` dictionaries | Full control — conditions, transactions, and **single-table design** (where one table holds many entity types) |
| Document model | `Table` + `Document` | Schema-flexible access without POCOs |
| **Object persistence** | `DynamoDBContext` + `[DynamoDBTable]` attributes | Simple one-entity-per-table CRUD. ⚠ It assumes one type per table, so it fits **single-table design poorly** |

**❗ Reuse the client.** `AmazonDynamoDBClient` is thread-safe and should be a **singleton** — in Lambda, create it **outside the handler** so it survives warm invocations and reuses connections. Creating one per request is a real and common performance bug.

**Conditional write for idempotency** — covered in full under *Conditional writes* above (`attribute_not_exists` + `ReturnValuesOnConditionCheckFailure`). In single-table design the key is `attribute_not_exists(PK)` rather than a dedicated idempotency attribute; everything else is identical, and `ConditionalCheckFailedException` is still the **expected path** on a duplicate delivery, not a failure.

**Query a GSI with a key condition** (never `Scan` in a hot path):
```csharp
var resp = await client.QueryAsync(new QueryRequest {
    TableName = "Orders",
    IndexName = "GSI1",
    KeyConditionExpression = "GSI1PK = :pk AND begins_with(GSI1SK, :prefix)",
    ExpressionAttributeValues = new() {
        [":pk"]     = new("CUSTOMER#42"),
        [":prefix"] = new("ORDER#2026-08")
    },
    Limit = 25
});
```

**Atomic counter and optimistic locking** — `UpdateItem` mutates server-side, so no read-modify-write race:
```csharp
UpdateExpression    = "SET #v = #v + :inc",
ConditionExpression = "#v < :max"          // atomic increment with a ceiling
```
With the object-persistence model, `[DynamoDBVersion]` gives you optimistic locking automatically — the SDK adds a version condition and throws on conflict.

**Transactions** — all-or-nothing across up to 100 items:
```csharp
await client.TransactWriteItemsAsync(new TransactWriteItemsRequest {
    TransactItems = new() {
        new() { Put    = new Put    { TableName = "Orders",    /* … */ } },
        new() { Update = new Update { TableName = "Inventory", /* decrement stock */ } }
    }
});
```
Fails **wholesale** on any single condition failure — one bad item cancels the entire call, and `TransactionCanceledException.CancellationReasons` tells you *which* item and why.

**Pagination — the loop people get wrong.** `Query`/`Scan` return at most **1 MB** per call, so a missing pagination loop silently truncates results:
```csharp
Dictionary<string, AttributeValue>? start = null;
do {
    var page = await client.QueryAsync(new QueryRequest { /* … */ ExclusiveStartKey = start });
    Process(page.Items);
    start = page.LastEvaluatedKey?.Count > 0 ? page.LastEvaluatedKey : null;
} while (start != null);
```
Note `LastEvaluatedKey` comes back as an **empty dictionary**, not null, when finished — checking `!= null` alone loops forever. (SDK v3's `Paginators.QueryAsync` handles this for you.)

**Batch writes** — `BatchWriteItem` takes up to 25 items and can **partially succeed**: you must re-submit `UnprocessedItems` with backoff. It's a throughput optimisation, not a transaction — no atomicity, no conditions.

**A Streams-triggered Lambda** (the CDC pattern behind read models and audit logs):
```csharp
public async Task Handler(DynamoDBEvent evnt, ILambdaContext ctx)
{
    foreach (var record in evnt.Records)
    {
        if (record.EventName == "INSERT") { /* record.Dynamodb.NewImage */ }
        // idempotent: the same record can be delivered more than once
    }
}
```

**.NET gotchas worth knowing:**
- Use **`decimal`**, not `double`, for money — DynamoDB's Number type is arbitrary-precision and `double` loses it.
- `DynamoDBContext` caches type metadata, so make it long-lived rather than per-request.
- The SDK **already retries throttling** with exponential backoff — don't add your own retry loop on top; tune `MaxErrorRetry` instead.
- Set `ReturnConsumedCapacity` while tuning to see the real RCU/WCU cost of each access pattern.
- **Reserve `Scan` for admin/backfill jobs**, and even then prefer a **sparse GSI** so the "scan" is over a tiny index.

**Limitations to state plainly in an interview (shows maturity, not weakness):**
- No joins, no ad hoc SQL-style queries, no `LIKE`.
- Large scans are expensive; bad PK design silently degrades performance.
- Uniqueness is enforceable only on the primary key, not arbitrary attributes.
- Not suited for complex reporting/analytics — export to Redshift/Athena/OpenSearch via Streams for that.

**Pagination:** results paginate at a 1MB page boundary; API returns `LastEvaluatedKey`, which you resend as `ExclusiveStartKey`. In your public API, base64-encode this as an opaque cursor token.

**Interview-ready 2-line summary:** "DynamoDB is a fully managed, low-latency, horizontally scalable NoSQL database. The real power comes from good key design, GSIs, conditional writes, and Streams to drive event-driven microservice workflows."

---

## 2. Trick Questions & Pitfalls

### DynamoDB Trick Questions

| Question | Answer |
|---|---|
| Is DynamoDB relational? | No — NoSQL key-value/document, no joins/FKs. |
| Is Scan faster than Query? | No — Scan reads the whole table; Query is index-optimized. |
| Can a PK be duplicated? | Yes, if SKs differ (that's the point of composite keys). |
| Strongly consistent by default? | No — eventually consistent reads by default; strong consistency must be explicitly requested (and GSIs can **never** be strongly consistent). |
| Can a GSI be added after table creation? | Yes. LSI cannot — LSIs must be defined at table creation. |
| Max item size? | 400 KB. |
| Do transactions cost more? | Yes — roughly 2× the capacity of equivalent non-transactional writes/reads. |
| Does TTL delete instantly? | No — best-effort, can take hours. |

**Senior-level summary (memorize):** "The deal with DynamoDB is simple: you give up the freedom to *ask anything*, and in return your speed does not drop no matter how much the data grows. So do the work backwards — first write down which questions you need to ask, and only then design the table. Pick a partition key with enough variety that the load spreads out, do not be afraid to keep copies of data, and do not do these three things: read the whole table, send all the traffic to one key, and build indexes you do not need."

**Worked example — the Deployment Dashboard (the resume bullet, modelled)**

The same principles on a real table. The dashboard fronts GitHub Actions → CDKTF/Terraform runs across 12 platform services, and it is read far more than it is written: a handful of deployments a day, but the board sits open in everyone's browser.

**1. Access patterns, written down before the schema:**

| | Pattern |
|---|---|
| **D1** | One service's deployment history, newest first |
| **D2** | Everything **in flight right now**, across all services — the landing tile |
| **D3** | One deployment's detail, including its Terraform plan |
| **D4** | What went to **prod** on a given day |
| **D5** | Find a deployment by its GitHub Actions run ID |
| **D6** | A service's **current live version per environment** — the most-read item on the board |

**2. Keys — and the partition key that looked obvious and was wrong.** `PK = ENV#prod` is the tempting choice and a hot partition by construction: four possible values, and prod takes most of the traffic. `PK = STATUS#IN_PROGRESS` is worse — low cardinality *and* rewritten on every transition. What works is the service:
```
PK = SERVICE#dms-export     SK = DEPLOY#2026-08-22T10:15:03Z#run-8871
```
| PK | SK | Item | Size | Writes |
|---|---|---|---|---|
| `SERVICE#dms-export` | `DEPLOY#<ts>#<runId>` | env, version, status, commit SHA + message, actor, duration | ~1 KB | ~5 — one per status transition |
| `SERVICE#dms-export` | `CURRENT#prod` | live-version pointer — **D6**, the hottest read on the board | 0.3 KB | 1 per successful deploy |
| `SERVICE#dms-export` | `META#` | display name, owning team, repo | 0.5 KB | rare |
| `RUN#8871` | `PTR#` | → the service PK/SK above — **D5** | 0.2 KB | once |
| `DEPLOY#run-8871` | `PLAN#` | **S3 key** of the Terraform plan output | 0.2 KB | once |

Twelve services is admittedly low cardinality, and the honest answer is that it's fine *at this write rate* — a few deployments per service per day is nowhere near the ~1,000 WCU per-partition ceiling. If this were CI runs at thousands per minute the PK would need a shard suffix. **Cardinality only matters relative to write rate**, and saying so is a better answer than reciting "high cardinality" as a rule.

**3. Denormalise for reads.** The board's rows render service display name, owning team, commit message and actor — which live on the `META#` item and in GitHub, not on the deployment. They're copied into the deployment item at write time. Two reasons: the board is read constantly and written a few times a day, and the copy is *correct* — a deployment record should show the commit message and owning team **as they were at deploy time**, not as they are now.

**4. The Scan trap — D2.** The obvious implementation of "what's running now" is a `Scan` with `FilterExpression: status = :in_progress`. That reads every deployment ever recorded and charges for all of it, getting slower every month while the answer stays 0–3 rows. Instead, a **sparse GSI**: write `GSI1PK = "IN_FLIGHT"` when a deployment starts and **remove the attribute** when it reaches a terminal state. The index then holds only live deployments, so D2 costs ~1 RCU no matter how much history accumulates.

**5. Restraint on indexes.** It's tempting to add a GSI per filter — env, status, commit, actor, date. Five `ALL`-projection GSIs would multiply each of the ~5 status transitions by six. What shipped instead: **one** sparse GSI (D2), **one** `ENV#…#DATE#…` GSI (D4), and for D5 a **write-once pointer item** rather than a GSI — because a GSI keyed on `runId` would be maintained on all five transitions, whereas the pointer is written once and never changes. *A write-once pointer item beats a GSI whenever the lookup key never changes.*

**6. Hot/cold split.** A Terraform plan is easily hundreds of KB and can approach the 400 KB item limit, so it lives in S3 with only its key in DynamoDB. Inline, each of those five status transitions would have cost ~400 WCU instead of 1.

**7. Conditional writes, twice over.** GitHub webhooks retry, so the run pointer is created with `attribute_not_exists(PK)` and a duplicate delivery becomes a no-op. Status transitions use `ConditionExpression: #status = :expected`, so a retried webhook cannot move `SUCCEEDED` back to `APPLYING`. Worth knowing for this bullet specifically: **Terraform's own S3-backend lock table works exactly this way** — a `PutItem` on `LockID` with `attribute_not_exists` — which is why a crashed apply leaves a stale lock that needs `terraform force-unlock`.

| Pattern | How it's served | Cost |
|---|---|---|
| D1 | `Query PK=SERVICE#x`, `begins_with(SK,'DEPLOY#')`, `ScanIndexForward=false` | ~1 RCU/page |
| D2 | `Query` on the sparse `GSI1` | ~1 RCU |
| D3 | `GetItem` + one S3 GET for the plan | ~0.5 RCU |
| D4 | `Query GSI2 PK=ENV#prod#DATE#2026-08-22` | ~1 RCU |
| D5 | `GetItem PK=RUN#8871` → then D3 | 2 × ~0.5 RCU |
| D6 | `GetItem PK=SERVICE#x, SK=CURRENT#prod` | 0.5 RCU |

**No `Scan` anywhere on the read path** — which is exactly what the summary above is asking for.

---

## 3. Resume Follow-Ups

### Resume Follow-Ups — The "DynamoDB-Backed Microservices" Bullet

> *"CSRconnect features… DynamoDB-backed microservices."*

Expect the full DynamoDB drill — and expect this to be the hardest-pressed bullet on the resume, because it is the one AWS service that ties the claim to a shipped product. A vague answer costs more here than on Kinesis.

- **Access patterns first** — the only correct opening move. Name the actual queries before the key schema.
- **Key design** — partition-key cardinality and hot partitions; what the PK/SK actually were and why.
- **Indexes** — **GSI vs LSI**: LSI at table-creation time only and sharing the partition's 10 GB limit; GSI eventually consistent with its own capacity.
- **Capacity** — on-demand vs provisioned, and which one a bursty export workload justified.
- **`Query` vs `Scan`** — and be ready for *"where does your code still Scan, and why is that acceptable?"*
- **Writes** — conditional writes for idempotency, transactions and their ~2× capacity cost, the 400 KB item limit and what you did when a payload approached it.
- **Single-table design** — have an opinion, including where you deliberately did *not* apply it.

All of it is worked through in [DynamoDB Deep Dive](#dynamodb-deep-dive) and [DynamoDB Trick Questions](#dynamodb-trick-questions); the other resume bullets are in [Resume Deep-Dives](00-resume-aligned-priority-map.md#resume-deep-dives--the-follow-ups-i-should-expect).

**Disaster Recovery — DynamoDB**

| | |
|---|---|
| **What's actually at risk** | Table data, GSIs, and the 24-hour Streams window |
| **Backup mechanism** | **PITR** (continuous, 35 days, restore to any *second*), **on-demand backups** (kept indefinitely), **AWS Backup** for cross-account/cross-region copies, **Global Tables** for active-active |
| **Realistic RPO / RTO** | PITR RPO ~5 minutes; Global Tables ~1 second. RTO is the honest problem: a restore **creates a new table** and rebuilds every GSI — minutes for small tables, **hours** for large ones |

**Recovery runbook:**
1. **Logical corruption (bad batch job, bad migration):** `aws dynamodb restore-table-to-point-in-time --target-table-name Orders-restored --restore-date-time <just before the bad write>`.
2. **Repoint the app** — read the table name from **SSM Parameter Store** or an env var, never a hardcoded constant. This one design choice is the difference between a 5-minute cutover and a redeploy of every consumer.
3. **Regional failure with Global Tables:** nothing to restore — point the SDK at the replica region. Conflict resolution is last-writer-wins, which is why the writes had to be idempotent in the first place.
4. **Reconcile the gap** between your RPO and the incident from the upstream DLQ/source, replaying through the same idempotent write path.

⚠️ **The gotcha:** **you cannot restore in place** — every restore is a new table, so DR without a name-indirection layer means a code deploy in the middle of an incident. And **PITR must be enabled before the incident**; turning it on afterwards gives you nothing. Enable it at table-creation time in the IaC module so it can never be forgotten.

---

← [Serverless & Lambda](01-serverless-lambda.md) · [Index](README.md) · [IAM & Security](03-iam-security.md) →
