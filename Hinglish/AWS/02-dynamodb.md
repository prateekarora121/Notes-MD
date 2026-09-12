> **AWS Detailed Guide** · [Index](README.md) · Part I

# DynamoDB

> **Tier 1 — bulletproof.** Ek hi AWS data store jo mera resume ek shipped product se tie karta hai ("DynamoDB-backed microservices"), aur yeh *Cloud* aur *Databases* skills dono ke under listed hai. Yeh section sabse hard revise karo. DynamoDB ke liye caching (**DAX**) [ElastiCache & Caching Patterns](10-databases-caching-analytics.md#elasticache--caching-patterns) mein hai.

---

## 1. DynamoDB Core

### DynamoDB Deep Dive

**Yeh kya hai:** fully managed NoSQL key-value/document store jo consistent single-digit-millisecond latency deta hai effectively unlimited scale par — *agar* key design correct ho. Schema-less: har item ke different attributes ho sakte hain, jo fast-evolving microservices ke suit karta hai lekin correctness burden ko application par shift karta hai.

**Partitioning**
- **Partition Key (PK)** ka hash decide karta hai ki item kaunse physical partition mein store hoga. Har partition ki bounded throughput hoti hai (historically documented around 3,000 RCU / 1,000 WCU per partition — exact numbers internal hain aur AWS unka contractually guarantee nahi karta, directional treat karo).
- Ek good PK mein **high cardinality** aur **even access distribution** hoti hai. Bad PK choice (low cardinality, time-based, ya ek "celebrity" key jise disproportionate traffic mil rahi ho) → **hot partition** → throttling even jab table-level metrics theek dikh rahe hain.
- **Adaptive Capacity** cold partitions se unused capacity ko hot partitions mein automatically shift karta hai — yeh real-world unevenness ko smooth karta hai lekin ek fundamentally bad key design ko **fix nahi karta**.

**Sort Key (SK) — yeh actually kya unlock karta hai**
| Pattern | Example |
|---|---|
| Range queries | `PK=USER#123, SK BETWEEN ORDER#2025-01-01 AND ORDER#2025-01-31` |
| Time-series | `PK=DEVICE#A100, SK=<ISO timestamp>`, query `SK > now-1h` |
| One-to-many | `PK=USER#123, SK=ORDER#<date>` (ek user ke under multiple orders) |
| Hierarchical/single-table | `PK=ORDER#555, SK=META# / ITEM#1 / EVENT#CREATED` — ek Query mein pura aggregate fetch |
| Sorted views | SK deterministic ordering ke liye priority/rank encode karta hai |

**GSI vs LSI**

Base table par aap sirf uske PK se Query kar sakte ho, toh har dusra access pattern ek **secondary index** maangta hai — same data ka alternate view, different key ke saath, jo DynamoDB aapke liye maintain karta hai.

**Mental model.** Table ko ek filing cabinet ki tarah socho: **PK matlab kaunsa drawer**, aur **SK matlab us drawer ke andar items ka order**. Aap drawer sirf tab khol sakte ho jab uska label pehle se pata ho.
```
Orders:  PK = customerId    SK = orderDate

drawer "C1" ──▶ [2026-01-05] [2026-03-11] [2026-08-22]     ← SK se sorted
drawer "C2" ──▶ [2026-02-01] [2026-07-30]
```
Toh *"C1 ke orders Jan se Aug ke beech"* ek sasti Query hai, par *"order O-500 kahan hai?"* — yahan kholne ke liye koi drawer label hi nahi hai, aur index ke bina yeh full Scan hai. Index ek dusra raasta deta hai:
- **LSI = wahi drawer, andar se re-sorted.**
- **GSI = poora naya cabinet, different label se re-filed.**

| | GSI (Global Secondary Index) | LSI (Local Secondary Index) |
|---|---|---|
| Partition key | **Kuch bhi** — base table se different | Base table jaisa hi **hona hi chahiye** |
| Sort key | Apna, optional | Base table se different |
| Physically kahan | Peeche se ek **alag table**, asynchronously replicated — isliye "global" | Base item ke **usi partition mein** — isliye "local" |
| Created | Kisi bhi waqt; baad mein drop bhi kar sakte ho | **Sirf table creation ke waqt** — baad mein add/remove kabhi nahi |
| Consistency | **Sirf eventual — strongly consistent kabhi nahi** (different partition + async replication ise physically impossible bana dete hain) | **Strongly consistent ho sakta hai** (co-located hai, isliye possible hai) |
| Capacity | Apni RCU/WCU | Base table capacity share karta hai |
| Max per table | 20 | 5 |
| Size limit | Koi nahi | ⚠️ **10 GB per partition-key value**, base item collection *aur* uske saare LSIs milakar |
| Cost consideration | Har base write GSI ko bhi write kar sakta hai (write amplification) — sirf needed attributes project karo | Creation-time constraint ki wajah se rarely used |

**Do constraints jo practically decision settle kar dete hain:**
- **LSI ka 10 GB ceiling** per partition-key value hai, base collection plus uske har LSI ko milakar. Cross kiya to writes `ItemCollectionSizeLimitExceededException` se fail hone lagti hain — toh ek unbounded collection (`PK=DEALER#7` ke neeche saare orders) eventually khud ko brick kar deti hai. Creation-time-only ke saath milakar, isi liye **~95% real designs GSI use karte hain**.
- **Throttled GSI base table ki writes bhi throttle kar deta hai** (provisioned capacity par). Ek index ko under-provision karna poore write path ko le doobta hai — surprising failure mode hai, aur favourite question bhi.

**Kaunsa choose karein:**
```
Different partition key chahiye?                    → GSI  (LSI mein PK badal hi nahi sakte)
Sirf different sort order chahiye, aur us read par
strong consistency bhi chahiye?                     → LSI
```
Base `Orders` table (`PK=ORDER#123`, `SK=META#`):

| Access pattern | Index | Keys |
|---|---|---|
| Ek order ke items `createdAt` order mein | **LSI** | `PK=ORDER#123` (same), `SK=createdAt` |
| Ek customer ke saare orders | **GSI** | `PK=CUSTOMER#42`, `SK=2026-08#ORDER#123` — PK badal gaya, toh GSI hi *hona padega* |

**Projections — write-amplification ka lever**

Index ko poora item hold karne ki zarurat nahi hai. Jo aap usmein copy karte ho wo **projection** hai, aur yehi index ke write cost ka main dial hai:

| `ProjectionType` | Index mein kya copy hota hai | Write cost |
|---|---|---|
| `KEYS_ONLY` | Sirf base table keys + index keys | Sabse kam |
| `INCLUDE` | Keys + naam se listed attributes | Tuned — **usually sahi jawab** |
| `ALL` | Poora item | Sabse zyada — full write amplification |

⚠️ **Fetch-back penalty:** agar Query koi aisa attribute maange jo index project nahi karta, DynamoDB use **chupchap base table se wapas read** karta hai — extra RCU aur extra latency, aur batane ke liye koi error nahi. Isliye projection soch kar karo: query ke result rows jo render karte hain bas utna hi `INCLUDE` karo, usse zyada nahi.

**One-line answer:** "PK decide karta hai item kis partition mein jaayega aur SK us partition ke andar uska order — toh koi bhi aisa access pattern jo *known PK, optional SK range* nahi hai, use secondary index chahiye. LSI base table ka partition key wahi rakhta hai aur sirf sort key badalta hai, isliye wo usi partition mein rehta hai: strongly consistent ho sakta hai, par table ke saath hi banana padta hai aur uska item collection per partition key 10 GB par capped hai. GSI koi bhi partition key use kar sakta hai, toh physically wo ek alag asynchronously-replicated table hai: kabhi bhi add/drop karo, apni capacity, par eventually consistent — hamesha. Practically main GSI hi uthata hoon jab tak specifically strongly consistent re-sort na chahiye, kyunki creation-time-only aur 10 GB ceiling LSI ko long-term liability bana dete hain. Dono mein projection hi asli cost lever hai — `INCLUDE` sirf wo karo jo query actually render karti hai, kyunki non-projected attribute maangne par base table par silent fetch-back ho jaata hai."

**Capacity modes**
| | On-Demand | Provisioned (+ Auto Scaling) |
|---|---|---|
| Planning | Koi nahi | RCU/WCU sizing required |
| Cost | Pay-per-request | Steady, predictable volume par cheaper; Reserved Capacity discount support karta hai |
| Best for | Spiky/unknown traffic (serverless, SQS bursts) | Stable, forecastable load |

**Query vs Scan**
- **Query**: PK-targeted (optionally SK range/filter) — O(matched items), yeh operation hai jise aapko hamesha optimize karna chahiye.
- **Scan**: pura table/index padhta hai, baad mein filter karta hai — expensive, slow, hot paths mein avoid karo; sirf rare admin/analytics jobs ke liye acceptable.

**Conditional writes — underrated superpower**
- Idempotency: `PutItem` `ConditionExpression: attribute_not_exists(idempotencyKey)` ke saath.
- Optimistic locking / state machine transitions: `UpdateItem ... ConditionExpression: status = :pending` `PROCESSING` par flip karne se pehle — server-side atomic check, koi distributed lock ki zarurat nahi.
- Yeh aapko concurrency control deta hai **bina** full Transactions ka 2× cost pay kiye.

**Yeh kaam kaise karta hai:** har single-item write (`PutItem`/`UpdateItem`/`DeleteItem`) atomic hota hai, aur DynamoDB `ConditionExpression` ko **server par, item ke apne lock ke andar** evaluate karta hai. Read aur write ke beech koi gap nahi bachta jisme dusra request ghus sake — aur wahi gap band karne ke liye distributed lock hota hai, isliye condition uski zarurat hi khatam kar deta hai.

**(a) Idempotency — duplicate deliveries suppress karo**
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
    // Duplicate — yeh SUCCESS hai, error nahi. Wahi response wapas do jo original ne diya tha.
    return new { created = false, orderId = ex.Item["orderId"].S };
}
```
`ReturnValuesOnConditionCheckFailure = ALL_OLD` existing item ko **exception ke andar hi** wapas de deta hai, ek extra `GetItem` bach jaata hai. Naam lene layak hai — yeh detail most candidates miss karte hain.

**(b) State-machine transition — compare-and-swap, exactly ek winner**

Naive version mein race hai:
```csharp
// ❌ RACE: do Lambdas dono PENDING padhte hain, dono aage badhte hain, card do baar charge
var order = await GetOrder(id);                 // A padhta hai PENDING; B bhi PENDING
if (order.Status == "PENDING") {                // dono ke liye true
    await SetStatus(id, "PROCESSING");
    await ChargeCard();                         // do baar charge ho gaya
}
```
Condition read+check+write ko ek single atomic server-side operation mein collapse kar deta hai:
```csharp
await client.UpdateItemAsync(new UpdateItemRequest {
    TableName = "Orders",
    Key = new() { ["orderId"] = new(id) },
    UpdateExpression    = "SET #s = :processing, lockedBy = :worker, lockedAt = :now",
    ConditionExpression = "#s = :pending",                  // ← poora trick yehi hai
    ExpressionAttributeNames  = new() { ["#s"] = "status" }, // 'status' reserved word hai
    ExpressionAttributeValues = new() {
        [":processing"] = new("PROCESSING"),
        [":pending"]    = new("PENDING"),
        [":worker"]     = new(workerId),
        [":now"]        = new(DateTime.UtcNow.ToString("O"))
    }
});
```
Worker A ko `PENDING` milta hai → condition pass → flip kar deta hai. Worker B ko ab `PROCESSING` milta hai → condition fail → turant throw hota hai aur cleanly exit kar jaata hai. **Exactly ek winner, guaranteed.**

**(c) Version-number variant** — jab aapko named state ke bajaye ek generic "main soch raha tha, tab kisi ne modify kiya kya?" check chahiye:
```csharp
UpdateExpression    = "SET #data = :d, version = version + :one",
ConditionExpression = "version = :expectedVersion"
```
Failure ka matlab kisi aur ne beech mein likh diya — dobara read karke retry karo.

**Distributed lock ki zarurat kyun nahi.** Classic sequence hai *lock acquire karo → read → validate → write → lock release karo*, aur uske saath lock TTL tuning, crash ke baad orphaned locks, stale holders ko block karne ke liye fencing tokens, aur ek aur infrastructure piece jo down ho sakta hai — sab jhelna padta hai. Conditional write yeh poora block **ek network call** mein kar deta hai — `status` attribute *hi* lock hai, toh expire, orphan, fence, ya release karne ke liye kuch bhi nahi bachta.

**Cost — 2× kahan se aata hai**
| Operation (1 KB item) | Write cost |
|---|---|
| `PutItem` / `UpdateItem` | **1 WCU** |
| Wahi, `ConditionExpression` **ke saath** | **1 WCU** — condition free hai |
| `TransactWriteItems` | **2 WCU** per item (two-phase commit: prepare + commit) |

Ek caveat batana: condition **fail** hone par bhi 1 WCU lagta hai — write hua nahi, phir bhi charge hua. Heavy contention mein yeh matter karta hai, phir bhi transaction se sasta rehta hai.

**Conditional write kab *kaafi nahi* hai.** Item boundary hi atomicity boundary hai, toh ek hi case hai jise plain condition express nahi kar sakta — *ek item par condition, aur write dusre item par*. `TransactWriteItems` mein iske liye dedicated `ConditionCheck` action hai. Neeche *Transactions* dekho.

**Transactions (`TransactWriteItems`/`TransactGetItems`)**
- Ek call mein 100 items/tables tak ACID (4 MB aggregate size limit). Note: September 2022 tak yeh 25 items thi — older material aur older exam guides abhi bhi 25 kehte hain.
- Sirf genuine all-or-nothing business invariants ke liye use karo (inventory decrement + order creation, money transfer) — ~2× capacity aur added latency cost karta hai, isliye default mein iske liye mat jao.

**TTL:** attribute-driven, best-effort async deletion — practically ghanton lag sakte hain (notes explicitly up to ~48 hours flag karte hain kuch documented cases mein), exact-to-the-second nahi. Idempotency keys, sessions, dedup records, temporary workflow state ke liye good. Time-sensitive compliance deletion deadlines ke liye TTL par **rely mat karo**.

**Streams:** time-ordered change log (insert/update/delete), ~24 hours retained. In patterns ka backbone hai:
- CQRS read-model projections (normalized write table → Streams → Lambda → denormalized read table/GSI)
- Event-driven pipelines (Streams → Lambda → SNS/SQS/EventBridge)
- Change-data-capture / audit trails / search-index sync (OpenSearch, S3)

**Global Tables:** multi-region, multi-active replication; users nearest region se read/write karte hain; conflict resolution **last-writer-wins** hai, replication asynchronous/eventually consistent hai — writes ko idempotent/conflict-tolerant design karo.

**Single-table design**
- Multiple entity types (User, Order, Item, Event) ek table mein PK/SK convention ke through — upfront modeling effort trade karta hai far fewer round-trips aur no joins ke liye.
- Notes se real pattern: `PK=ORDER#123` `SK=META# / ITEM#1 / EVENT#<ts>` ke saath ek single Query mein poora order aggregate (header + items + event history) return karta hai.

**400 KB item limit:** hard cap jismein attribute names+values include hain. Large objects (images, big docs) → blob ko S3 mein store karo, DynamoDB mein sirf pointer/key rakho; ya same PK ke under multiple items mein vertically partition karo.

#### Capacity Maths & Hot-Partition Mitigation

**Unit definitions directly poochi jaati hain**, aur arithmetic zubaani kar dena ek real answer ko ek memorised answer se alag karta hai.

| Unit | Kya deta hai | Effective rate |
|---|---|---|
| **1 RCU** | ek **strongly consistent** read up to **4 KB** | 1/sec |
| **1 RCU** | **do** *eventually consistent* reads up to 4 KB | 2/sec |
| **1 RCU** | aadha *transactional* read of 4 KB (txn reads 2× cost) | 0.5/sec |
| **1 WCU** | ek write up to **1 KB** | 1/sec |
| **1 WCU** | aadha *transactional* write of 1 KB (txn writes 2× cost) | 0.5/sec |

Asymmetry note karo: reads ko per unit **4 KB** milta hai, writes ko sirf **1 KB** — matlab writes per byte 4× mehngi hain, aur isi liye design advice hai ki reads ke liye denormalise karo aur write amplification kam rakho.

**Rounding — yahan marks jaate hain.** Hamesha **up** round karo, per item, per operation:
- 5 KB item, strongly consistent read → `ceil(5/4)` = **2 RCU**
- 0.5 KB write → `ceil(0.5/1)` = **1 WCU** (dono case mein poora KB, toh bahut si chhoti writes capacity waste karti hain)
- 1.5 KB write → **2 WCU**
- **Read example:** 100 reads/sec, 10 KB items, eventually consistent → `ceil(10/4) = 3` units strongly consistent → eventual consistency ke liye ÷2 = `1.5` → round up = **2 RCU per read** → × 100 = **~200 RCU**
- **Write example:** 50 writes/sec, 2.5 KB items → `ceil(2.5/1) = 3` WCU each → × 50 = **150 WCU**; transactional bana do to **300 WCU**

**Naming precision:** RCU/WCU **provisioned**, per-second-reserved units hain. On-demand wahi 4 KB / 1 KB sizing **RRU/WRU** (Read/Write *Request* Units) ke naam se bill karta hai — per request, na ki per second reserved. Dono pairs ko interchangeably use karna ek chhota tell hai.

**One-line answer:** "RCU aur WCU per-second throughput units hain — ek RCU matlab up to 4 KB ka ek strongly consistent read, ya do eventually consistent reads; ek WCU matlab 1 KB ki ek write. Dono per item round up hote hain, transactions inhe double kar dete hain, aur GSIs apni alag capacity consume karte hain. Subtleties do hain: capacity us data par charge hoti hai jo filtering se **pehle** read hua, isliye filtered Scan ki cost unfiltered ke barabar hi hai; aur ek single partition ~3,000 RCU / 1,000 WCU par top out ho jaata hai — toh ek hot key throttle karta hai jabki table-level metrics healthy dikhte rehte hain."

**Write sharding — hot partition ka fix.** Agar ek partition key ki low cardinality hai (`STATUS#PENDING`, ya ek date jaisa `2026-08-10`), sara traffic ek partition par land karta hai aur aap throttle karte ho jab table-level capacity theek dikh rahi ho. Suffix add karo spread karne ke liye:
```
PK = ORDER#2026-08-10#3        // shard = hash(orderId) % 10
```
Writes ab 10 partitions ke across spread hote hain. State karna trade-off: **reads ko ab sabhi 10 shards query karke merge karna padta hai**, isliye sirf wahan shard karo jahan write hot-spot real ho. **Adaptive Capacity** automatically help karta hai lekin ek well-chosen key ka substitute nahi hai.

**GSI overloading aur sparse indexes** — do single-table techniques jo naam lene layak hain:
- **Overloading**: ek GSI jiski keys generic hain (`GSI1PK`/`GSI1SK`) multiple access patterns serve karta hai kyunki different entity types unmein different values write karte hain.
- **Sparse index**: ek item GSI mein sirf tab appear hota hai jab uske paas index ka key attribute *ho*. Toh `GSI1PK` sirf unprocessed orders par likhna aapko ek index deta hai jismein *sirf* work queue ho — cheap to scan, kyunki yeh construction se hi tiny hai.

#### DynamoDB in .NET — Wo Code Jo Aapse Likhne Ko Kaha Jaayega

**Three SDK layers, aur kab kaunsa use karo:**
| Layer | Type | Kab use karo |
|---|---|---|
| **Low-level** | `AmazonDynamoDBClient` + `AttributeValue` dictionaries | Full control — conditions, transactions, aur **single-table design** (jahan ek table many entity types hold karta hai) |
| Document model | `Table` + `Document` | POCOs ke bina schema-flexible access |
| **Object persistence** | `DynamoDBContext` + `[DynamoDBTable]` attributes | Simple one-entity-per-table CRUD. ⚠ Yeh ek type per table assume karta hai, isliye **single-table design** ke saath poorly fit hota hai |

**❗ Client reuse karo.** `AmazonDynamoDBClient` thread-safe hai aur ek **singleton** hona chahiye — Lambda mein, ise handler ke **bahar** create karo taaki yeh warm invocations mein survive kare aur connections reuse kare. Per request ek banana ek real aur common performance bug hai.

**Idempotency ke liye conditional write** — poora treatment upar *Conditional writes* mein hai (`attribute_not_exists` + `ReturnValuesOnConditionCheckFailure`). Single-table design mein key `attribute_not_exists(PK)` hoti hai, dedicated idempotency attribute ke bajaye; baaki sab wahi hai, aur `ConditionalCheckFailedException` duplicate delivery par ab bhi **expected path** hai, failure nahi.

**GSI ko key condition ke saath query karo** (hot path mein kabhi `Scan` mat karo):
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

**Atomic counter aur optimistic locking** — `UpdateItem` server-side mutate karta hai, isliye read-modify-write race nahi hoti:
```csharp
UpdateExpression    = "SET #v = #v + :inc",
ConditionExpression = "#v < :max"          // atomic increment with a ceiling
```
Object-persistence model ke saath, `[DynamoDBVersion]` aapko automatically optimistic locking deta hai — SDK ek version condition add karta hai aur conflict par throw karta hai.

**Transactions** — 100 items tak all-or-nothing:
```csharp
await client.TransactWriteItemsAsync(new TransactWriteItemsRequest {
    TransactItems = new() {
        new() { Put    = new Put    { TableName = "Orders",    /* … */ } },
        new() { Update = new Update { TableName = "Inventory", /* decrement stock */ } }
    }
});
```
Kisi bhi **ek** condition failure par poora call **wholesale** cancel hota hai — aur `TransactionCanceledException.CancellationReasons` batata hai ki *kaunsa* item fail hua aur kyun.

**Pagination — wo loop jise log galat karte hain.** `Query`/`Scan` per call max **1 MB** return karte hain, toh ek missing pagination loop silently results truncate kar deta hai:
```csharp
Dictionary<string, AttributeValue>? start = null;
do {
    var page = await client.QueryAsync(new QueryRequest { /* … */ ExclusiveStartKey = start });
    Process(page.Items);
    start = page.LastEvaluatedKey?.Count > 0 ? page.LastEvaluatedKey : null;
} while (start != null);
```
Note karo `LastEvaluatedKey` khatam hone par ek **empty dictionary** ke roop mein aata hai, null nahi — sirf `!= null` check karna forever loop kar dega. (SDK v3 ka `Paginators.QueryAsync` yeh aapke liye handle karta hai.)

**Batch writes** — `BatchWriteItem` 25 items tak leta hai aur **partially succeed** kar sakta hai: aapko backoff ke saath `UnprocessedItems` re-submit karna padta hai. Yeh throughput optimisation hai, transaction nahi — koi atomicity nahi, koi conditions nahi.

**Ek Streams-triggered Lambda** (CDC pattern jo read models aur audit logs ke peeche hai):
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

**.NET gotchas jo jaanne layak hain:**
- Money ke liye **`decimal`** use karo, `double` nahi — DynamoDB ka Number type arbitrary-precision hai aur `double` usse lose kar deta hai.
- `DynamoDBContext` type metadata cache karta hai, isliye ise long-lived banao, per-request nahi.
- SDK **already throttling retry karta hai** exponential backoff ke saath — apna retry loop upar mat add karo; `MaxErrorRetry` tune karo instead.
- Tuning ke waqt `ReturnConsumedCapacity` set karo har access pattern ka real RCU/WCU cost dekhne ke liye.
- **`Scan` ko admin/backfill jobs ke liye reserve karo**, aur tab bhi ek **sparse GSI** prefer karo taaki "scan" ek tiny index par ho.

**Interview mein plainly state karne layak limitations (maturity dikhata hai, weakness nahi):**
- No joins, no ad hoc SQL-style queries, no `LIKE`.
- Large scans expensive hain; bad PK design silently performance degrade karta hai.
- Uniqueness sirf primary key par enforceable hai, arbitrary attributes par nahi.
- Complex reporting/analytics ke liye suited nahi — uske liye Streams ke through Redshift/Athena/OpenSearch mein export karo.

**Pagination:** results ek 1MB page boundary par paginate hote hain; API `LastEvaluatedKey` return karta hai, jise aap `ExclusiveStartKey` ke roop mein resend karte ho. Apne public API mein, ise ek opaque cursor token ke roop mein base64-encode karo.

**Interview-ready 2-line summary:** "DynamoDB ek fully managed, low-latency, horizontally scalable NoSQL database hai. Real power good key design, GSIs, conditional writes, aur event-driven microservice workflows drive karne ke liye Streams se aati hai."

---

## 2. Trick Questions & Pitfalls

### DynamoDB Trick Questions

| Question | Jawab |
|---|---|
| DynamoDB relational hai kya? | Nahi — NoSQL key-value/document, no joins/FKs. |
| Scan Query se faster hai kya? | Nahi — Scan poora table padhta hai; Query index-optimized hai. |
| Ek PK duplicate ho sakta hai kya? | Haan, agar SKs different hain (composite keys ka yehi point hai). |
| Default se strongly consistent hai kya? | Nahi — default se eventually consistent reads; strong consistency explicitly request karni padti hai (aur GSIs **kabhi** strongly consistent nahi ho sakte). |
| Table creation ke baad GSI add ho sakta hai kya? | Haan. LSI nahi ho sakta — LSIs table creation ke waqt hi define hone chahiye. |
| Max item size? | 400 KB. |
| Transactions extra cost karte hain kya? | Haan — equivalent non-transactional writes/reads ke roughly 2× capacity. |
| TTL instantly delete karta hai kya? | Nahi — best-effort, ghante lag sakte hain. |

**Senior-level summary (memorize karo):** "DynamoDB ka deal seedha hai: aap 'kuch bhi poochne' ki azadi chhodte ho, badle mein yeh milta hai ki data chahe kitna badh jaaye, speed nahi girti. Isliye kaam ulta karo — pehle list banao ki kaun-kaun se sawaal poochhne hain, uske baad table design karo. Partition key aisi chuno jisme variety ho taaki load faila rahe, data ki copy rakhne se mat daro, aur teen cheezein mat karo: poora table padhna, sara traffic ek key par daalna, aur bina zarurat ke index banana."

**Worked example — Deployment Dashboard (resume bullet, modelled)**

Wahi principles ek real table par. Dashboard GitHub Actions → CDKTF/Terraform runs ko 12 platform services ke across front karta hai, aur likha jaane se kaafi zyada padha jaata hai: din mein gine-chune deployments, par board sabke browser mein khula rehta hai.

**1. Access patterns, schema se pehle likhe gaye:**

| | Pattern |
|---|---|
| **D1** | Ek service ki deployment history, newest first |
| **D2** | **Abhi jo in flight hai**, saari services ke across — landing tile |
| **D3** | Ek deployment ka detail, uske Terraform plan ke saath |
| **D4** | Ek din **prod** mein kya gaya |
| **D5** | GitHub Actions run ID se deployment dhundho |
| **D6** | Ek service ka **current live version per environment** — board par sabse zyada padha jaane wala item |

**2. Keys — aur wo partition key jo obvious lagti thi par galat thi.** `PK = ENV#prod` tempting choice hai aur by construction hot partition hai: chaar possible values, aur prod hi zyadatar traffic leta hai. `PK = STATUS#IN_PROGRESS` isse bhi kharab hai — low cardinality *aur* har transition par rewrite. Jo kaam karta hai wo service hai:
```
PK = SERVICE#dms-export     SK = DEPLOY#2026-08-22T10:15:03Z#run-8871
```
| PK | SK | Item | Size | Writes |
|---|---|---|---|---|
| `SERVICE#dms-export` | `DEPLOY#<ts>#<runId>` | env, version, status, commit SHA + message, actor, duration | ~1 KB | ~5 — per status transition ek |
| `SERVICE#dms-export` | `CURRENT#prod` | live-version pointer — **D6**, board ka hottest read | 0.3 KB | successful deploy par 1 |
| `SERVICE#dms-export` | `META#` | display name, owning team, repo | 0.5 KB | rare |
| `RUN#8871` | `PTR#` | → upar wala service PK/SK — **D5** | 0.2 KB | ek baar |
| `DEPLOY#run-8871` | `PLAN#` | Terraform plan output ka **S3 key** | 0.2 KB | ek baar |

Baarah services ki cardinality admittedly low hai, aur honest jawab yeh hai ki *is write rate par* yeh theek hai — per service din ke gine-chune deployments ~1,000 WCU per-partition ceiling ke aas-paas bhi nahi hain. Agar yeh CI runs hote hazaaron per minute, to PK ko shard suffix chahiye hota. **Cardinality sirf write rate ke relative matter karti hai**, aur yeh bol dena "high cardinality chahiye" rattne se better answer hai.

**3. Reads ke liye denormalise.** Board ki rows service display name, owning team, commit message aur actor render karti hain — jo `META#` item par aur GitHub mein hain, deployment item par nahi. Inhe write time par deployment item mein copy kiya jaata hai. Do wajah: board constantly padha jaata hai aur din mein do-chaar baar likha jaata hai, aur copy *correct* bhi hai — ek deployment record ko commit message aur owning team **wahi dikhani chahiye jo deploy ke waqt thi**, aaj wali nahi.

**4. Scan ka trap — D2.** "Abhi kya chal raha hai" ka obvious implementation ek `Scan` hai `FilterExpression: status = :in_progress` ke saath. Wo aaj tak ka har deployment padhta hai aur sab ka charge leta hai, har mahine dheere hota jaata hai — jabki jawab 0–3 rows hi rehta hai. Iski jagah ek **sparse GSI**: deployment shuru hone par `GSI1PK = "IN_FLIGHT"` likho, aur terminal state par wo **attribute hata do**. Tab index mein sirf live deployments rehte hain, toh D2 ~1 RCU ka rehta hai — history kitni bhi jama ho jaaye.

**5. Indexes par restraint.** Har filter ke liye ek GSI banane ka man karta hai — env, status, commit, actor, date. Paanch `ALL`-projection GSIs har ek ~5 status transitions ko chhah se multiply kar dete. Jo actually shipped hua: **ek** sparse GSI (D2), **ek** `ENV#…#DATE#…` GSI (D4), aur D5 ke liye GSI ke bajaye ek **write-once pointer item** — kyunki `runId` par GSI paanchon transitions par maintain hota, jabki pointer ek baar likha jaata hai aur kabhi badalta nahi. *Jab lookup key kabhi badalti hi nahi, write-once pointer item GSI se better hai.*

**6. Hot/cold split.** Terraform plan aasani se sau-do sau KB ka hota hai aur 400 KB item limit ke paas pahunch sakta hai, isliye wo S3 mein rehta hai aur DynamoDB mein sirf uski key. Inline hota to un paanch status transitions mein se har ek ~400 WCU ka padta, 1 ke bajaye.

**7. Conditional writes, do jagah.** GitHub webhooks retry karte hain, isliye run pointer `attribute_not_exists(PK)` ke saath banta hai aur duplicate delivery no-op ban jaati hai. Status transitions `ConditionExpression: #status = :expected` use karte hain, toh retried webhook `SUCCEEDED` ko wapas `APPLYING` nahi kar sakta. Is bullet ke liye khaas jaanne layak: **Terraform ka apna S3-backend lock table bilkul isi tarah kaam karta hai** — `LockID` par `PutItem` with `attribute_not_exists` — aur isi wajah se crashed apply ek stale lock chhod jaata hai jise `terraform force-unlock` karna padta hai.

| Pattern | Kaise serve hota hai | Cost |
|---|---|---|
| D1 | `Query PK=SERVICE#x`, `begins_with(SK,'DEPLOY#')`, `ScanIndexForward=false` | ~1 RCU/page |
| D2 | Sparse `GSI1` par `Query` | ~1 RCU |
| D3 | `GetItem` + plan ke liye ek S3 GET | ~0.5 RCU |
| D4 | `Query GSI2 PK=ENV#prod#DATE#2026-08-22` | ~1 RCU |
| D5 | `GetItem PK=RUN#8871` → phir D3 | 2 × ~0.5 RCU |
| D6 | `GetItem PK=SERVICE#x, SK=CURRENT#prod` | 0.5 RCU |

**Read path par kahin bhi `Scan` nahi** — aur upar wala summary exactly yehi maang raha hai.

---

## 3. Resume Follow-Ups

### Resume Follow-Ups — "DynamoDB-Backed Microservices" Bullet

> *"CSRconnect features… DynamoDB-backed microservices."*

Poora DynamoDB drill expect karo — aur yeh resume ka sabse hard-pressed bullet hoga, kyunki yeh ek hi AWS service hai jo claim ko ek shipped product se tie karti hai. Yahan vague answer Kinesis par vague answer se zyada cost karta hai.

- **Access patterns pehle** — yehi ek sahi opening move hai. Key schema se pehle actual queries batao.
- **Key design** — partition-key cardinality aur hot partitions; PK/SK actually kya the aur kyun.
- **Indexes** — **GSI vs LSI**: LSI sirf table-creation time par aur partition ki 10 GB limit share karta hai; GSI eventually consistent, apni capacity ke saath.
- **Capacity** — on-demand vs provisioned, aur ek bursty export workload ne kaunsa justify kiya.
- **`Query` vs `Scan`** — aur *"tumhara code aaj bhi kahan Scan karta hai, aur woh acceptable kyun hai?"* ke liye ready raho.
- **Writes** — idempotency ke liye conditional writes, transactions aur unki ~2× capacity cost, 400 KB item limit aur payload us limit ke paas pahunchne par kya kiya.
- **Single-table design** — opinion rakho, including woh jagah jahan deliberately *nahi* apply kiya.

Yeh sab [DynamoDB Deep Dive](#dynamodb-deep-dive) aur [DynamoDB Trick Questions](#dynamodb-trick-questions) mein worked through hai; baaki resume bullets [Resume Deep-Dives](00-resume-aligned-priority-map.md#resume-deep-dives--woh-follow-ups-jo-mujhe-expect-karne-chahiye) mein hain.

**Disaster Recovery — DynamoDB**

| | |
|---|---|
| **Actually risk par kya hai** | Table data, GSIs, aur Streams ki 24-ghante ki window |
| **Backup mechanism** | **PITR** (continuous, 35 din, kisi bhi *second* par restore), **on-demand backups** (indefinitely rakhe jaate hain), cross-account/cross-region copies ke liye **AWS Backup**, active-active ke liye **Global Tables** |
| **Realistic RPO / RTO** | PITR RPO ~5 minutes; Global Tables ~1 second. RTO honest problem hai: restore ek **nayi table banata hai** aur har GSI rebuild karta hai — chhoti tables ke liye minutes, badi ke liye **ghante** |

**Recovery runbook:**
1. **Logical corruption (kharab batch job, kharab migration):** `aws dynamodb restore-table-to-point-in-time --target-table-name Orders-restored --restore-date-time <bad write se thoda pehle>`.
2. **App ko repoint karo** — table ka naam **SSM Parameter Store** ya env var se padho, kabhi hardcoded constant se nahi. Bas yeh ek design choice 5-minute cutover aur har consumer ke redeploy ke beech ka farak hai.
3. **Global Tables ke saath regional failure:** restore karne ko kuch nahi — SDK ko replica region par point kar do. Conflict resolution last-writer-wins hai, isi liye writes pehle se idempotent honi chahiye thi.
4. **RPO aur incident ke beech ka gap** upstream DLQ/source se reconcile karo, usi idempotent write path se replay karke.

⚠️ **Gotcha:** **aap in-place restore nahi kar sakte** — har restore nayi table hai, toh name-indirection layer ke bina DR ka matlab incident ke beech mein code deploy karna hai. Aur **PITR incident se pehle enable hona chahiye**; baad mein on karne se kuch nahi milta. Use IaC module mein table-creation ke waqt hi enable karo taaki bhoola hi na ja sake.

---

← [Serverless & Lambda](01-serverless-lambda.md) · [Index](README.md) · [IAM & Security](03-iam-security.md) →
