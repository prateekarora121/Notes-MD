> **AWS Detailed Guide** · [Index](README.md) · Part III

# Messaging, Streaming & Decoupling

---

## 1. SQS & SNS

### SQS & SNS Fundamentals

**SQS (Simple Queue Service) — pull-based queue**
- Producers/consumers ko asynchronously decouple karta hai; at-least-once delivery; durable (AZs ke across replicated); automatically scale hota hai.
- Flow: producer sends karta hai → durably stored hota hai → consumer poll karta hai → processing ke dauraan message **visibility timeout** ke zariye hidden ho jaata hai → success par, consumer usse delete kar deta hai → failure par, yeh dobara visible ho jaata hai → `maxReceiveCount` retries ke baad, **DLQ** mein move ho jaata hai.

| | Standard Queue | FIFO Queue |
|---|---|---|
| Delivery | At-least-once, duplicates possible | Exactly-once processing (dedup ke saath) |
| Ordering | Guaranteed nahi | Guaranteed (per Message Group) |
| Throughput | Very high | Lower (throughput quota per message group, halaanki high-throughput mode isse badha deta hai) |
| Use case | Most workloads | Orders, payments, koi bhi cheez jisko strict per-entity order chahiye |

- **Long polling** (`WaitTimeSeconds` up to 20s): empty-receive cost kam karta hai aur short polling ke against latency improve karta hai. **Short polling** sirf servers ka ek subset sample karta hai, isliye yeh empty return kar sakta hai even jab messages exist karte hon — long polling default hona chahiye, aur ise queue level par set karna (`ReceiveMessageWaitTimeSeconds`) empty receives ke liye pay karne se cheaper hai.

**SQS limits aur knobs jo precisely poochhe jaate hain:**
| Setting | Detail |
|---|---|
| **Message size** | **256 KB maximum.** Larger payloads ke liye **SQS Extended Client Library** use karo — yeh body ko **S3** mein store karta hai aur message mein ek pointer daal deta hai. "Claim check" pattern; SNS ke liye bhi yehi apply hota hai |
| **Retention** | **Default 4 days**, configurable **60 seconds se 14 days**. Messages process hone se pehle bhi is duration ke baad delete ho jaate hain |
| **Visibility timeout** | Default 30 s, max 12 h. Aapke worst-case processing time se zyada hona chahiye warna message dobara dikh jaata hai aur do baar process ho jaata hai. Variable work ke liye, lease ko mid-processing extend karne ke liye ek "heartbeat" ki tarah **`ChangeMessageVisibility`** call karo, ek huge global timeout set karne ke bajaye |
| **Delay queue** | **Naye** messages ko unke visible hone se *pehle* 0–15 minutes tak delay karta hai. **Visibility timeout se same nahi hai**, jo message ko *receive hone ke baad* hide karta hai — yeh distinction ek favourite question hai |
| **DLQ + redrive** | `maxReceiveCount` failed receives ke baad, message DLQ mein move ho jaata hai. **Redrive** unhe wapas source queue mein move karta hai jab bug fix ho jaaye — isliye "fix, then redrive" naam lo, sirf "yeh DLQ mein jaata hai" nahi |
| **FIFO dedup** | Ya to ek content-based hash ya ek explicit `MessageDeduplicationId`, ek **5-minute** dedup window par. `MessageGroupId` woh hai jo ordering scope define karta hai — ek slow group doosron ko block nahi karta |
| **Encryption / access** | Rest mein SSE-KMS, aur cross-account ya SNS/S3/EventBridge access ke liye ek **queue policy** (resource-based) |

**SNS features naam lene worth:**
- **❗ Message filtering (filter policies)** — subscribers message **attributes** (ya, payload-based filtering ke saath, body) par ek JSON filter declare karte hain, isliye ek subscriber sirf woh events receive karta hai jo usko chahiye. Yeh "aap kaise avoid karte ho ki har consumer har event receive kare aur code mein filter kare?" ka answer hai — topic par filtering invocations, cost, aur consumer complexity bachati hai. Zyadatar candidates ke answers mein ek notable gap.
- **FIFO topics** — ordered, deduplicated fan-out, lekin yeh sirf **SQS FIFO queues** ko deliver kar sakte hain.
- Per protocol **delivery retry policies**, plus ek **subscription-level DLQ** un messages ke liye jo SNS deliver nahi kar sakta.
- **Message size 256 KB**, SQS jaisa hi, same S3 claim-check workaround ke saath.
- Cross-region aur cross-account delivery, aur protocol-specific payloads ke liye `MessageStructure=json` (SMS ke liye alag body, SQS ke liye alag).

**SNS (Simple Notification Service) — push-based pub/sub**
- Publisher → topic → SNS *sabhi* subscribers ko push karta hai (fan-out) instantly. Subscribers: SQS, Lambda, HTTP(S), email/SMS, mobile push.
- Delivery retries SNS side par exponential backoff use karte hain, lekin **asynchronous processing ke liye durability aur retry semantics SQS se aani chahiye**, sirf SNS se nahi — SNS→Lambda direct ke paas koi built-in DLQ-backed retry buffer nahi hai jaise SNS→SQS→consumer ke paas hai.

| Feature | SNS | SQS |
|---|---|---|
| Pattern | Pub/Sub | Queue |
| Delivery | Push | Pull |
| Use case | Fan-out, notifications | Decoupling, buffering, guaranteed processing |
| Real-time | Haan | Nahi (polling-based) |
| Ordering | Nahi (Standard) | FIFO optional |
| Durability | Subscriber/retry policy par depend karta hai | High (queue khud durable hai) |

**SNS → SQS fan-out pattern** (notes mein sabse common AWS microservices pattern): ek publisher ek domain event (`order_created`) ek SNS topic ko emit karta hai; har interested microservice (Billing, Notification, Analytics, Inventory) ka apna **khud ka** SQS queue us topic ke saath subscribed hota hai. Benefits: agar ek consumer down hai to koi message loss nahi, per consumer independent scaling/failure, koi risk nahi ki ek slow consumer doosron ko block kar de.

**SQS alone kab use karein:** async processing, retry+DLQ, bursty load buffering, worker-pool consumption (file processing, transcoding, batch/ETL).

**SNS alone kab use karein:** heterogeneous consumer types ko broadcast karna, low-latency pub/sub, email/SMS/mobile notifications.

**Kab combine karein (event-driven microservices ke liye recommended default):** SNS fan-out deta hai; SQS durability, retries, aur per-consumer isolation deta hai.

---

## 2. CQRS Patterns in .NET

### CQRS with SNS/SQS in .NET

**Golden rule:** domain event ko SNS par sirf write-side DB transaction successfully commit hone **ke baad** publish karo — commit se pehle speculatively kabhi publish na karo.

```csharp
public record OrderCreatedEvent(Guid EventId, Guid OrderId, decimal Amount, DateTime CreatedAt);

public class SnsPublisher
{
    private readonly IAmazonSimpleNotificationService _sns;
    private readonly string _topicArn;

    public SnsPublisher(IAmazonSimpleNotificationService sns, IConfiguration config)
    {
        _sns = sns;
        _topicArn = config["AWS:SNS:OrderCreatedTopicArn"];
    }

    public async Task PublishAsync(OrderCreatedEvent evt)
    {
        var message = JsonSerializer.Serialize(evt);
        var request = new PublishRequest
        {
            TopicArn = _topicArn,
            Message = message,
            MessageAttributes =
            {
                ["eventType"] = new MessageAttributeValue { DataType = "String", StringValue = "OrderCreated" }
            }
        };
        await _sns.PublishAsync(request);
    }
}

// Command handler
public async Task CreateOrderAsync(CreateOrderCommand cmd)
{
    await _db.SaveChangesAsync();               // 1. commit write DB first
    await _snsPublisher.PublishAsync(            // 2. publish AFTER commit succeeds
        new OrderCreatedEvent(Guid.NewGuid(), cmd.OrderId, cmd.Amount, DateTime.UtcNow));
}
```

**Consumer (BackgroundService) critical SNS envelope detail ke saath:**

```csharp
public class OrderCreatedConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly string _queueUrl;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = 5,
                WaitTimeSeconds = 20 // long polling
            });

            foreach (var message in response.Messages)
            {
                try
                {
                    await ProcessMessageAsync(message);
                    await _sqs.DeleteMessageAsync(_queueUrl, message.ReceiptHandle); // delete only after success
                }
                catch (Exception ex)
                {
                    // do NOT delete — SQS will retry, then DLQ after maxReceiveCount
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }

    private async Task ProcessMessageAsync(Message message)
    {
        // SNS wraps the real payload inside an envelope — a classic interview detail
        var snsEnvelope = JsonSerializer.Deserialize<SnsEnvelope>(message.Body);
        var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(snsEnvelope.Message);

        if (await AlreadyProcessed(evt.EventId)) return;   // idempotency check — mandatory
        await UpdateReadDatabase(evt);
    }
}

public class SnsEnvelope
{
    public string Type { get; set; }
    public string Message { get; set; }
    public string MessageId { get; set; }
    public string TopicArn { get; set; }
}
```

**Idempotency tracking:**
```csharp
private async Task<bool> AlreadyProcessed(Guid eventId) =>
    await _db.ProcessedEvents.AnyAsync(x => x.EventId == eventId);
```

**DLQ mechanics:** `maxReceiveCount` configure karo (e.g., 5); usse zyada baar failed receives without deletion ke baad, SQS message ko automatically configured DLQ mein move kar deta hai. Recovery hai: inspect karo → root cause fix karo → source queue mein redrive karo (console ya automation).

**Registration (`Program.cs`):**
```csharp
builder.Services.AddAWSService<IAmazonSimpleNotificationService>();
builder.Services.AddAWSService<IAmazonSQS>();
builder.Services.AddSingleton<SnsPublisher>();
builder.Services.AddHostedService<OrderCreatedConsumer>();
```

**Iss pattern ke liye common interview traps:** DB commit *se pehle* SNS ko publish karna; multiple unrelated consumers ke across ek SQS queue share karna; SNS envelope unwrap karna bhool jaana; koi idempotency tracking nahi; processing complete hone se pehle SQS message delete karna (crash par message loss guarantee karta hai).

### CQRS + SNS/SQS Interview Pitfalls

| Pitfall / Question | Wrong instinct | Correct senior answer |
|---|---|---|
| "Read DB ko writes immediately reflect karna chahiye" | Strong consistency expect karna | CQRS design se eventually consistent hai; UI ko pending/optimistic state dikhana chahiye, polling/WebSocket/read-your-own-write cache use karo |
| "SQS directly kyun nahi SNS ke bajaye?" | "Ek queue hi kaafi hai" | SNS producer ko N consumers se decouple karta hai; har ek ka apna queue hota hai aur independently fail/scale hota hai |
| "SNS publish ho gaya lekin DB commit fail ho gaya" | Pehle publish karo, baad mein DB | Sirf commit ke baad publish karo; stronger guarantees ke liye **Outbox Pattern** use karo (event + data ko same DB transaction mein likho, ek separate relay use publish karta hai) |
| "Same message do baar process ho gaya" | "AWS once guarantee karta hai" | SQS at-least-once hai; consumers idempotent hone chahiye (EventId tracking, upserts, ya FIFO + `MessageDeduplicationId`) |
| "DLQ delivery kis cheez se hoti hai?" | "DLQ manual hai" | Consumer exception, message delete nahi hua, `maxReceiveCount` exceed ho gaya, ya visibility timeout misconfiguration |
| "Delete processing se pehle ya baad mein?" | Dedupe ke liye pehle delete karo | Sirf success **ke baad** delete karo — pehle delete karne se crash par silent data loss ka risk hai |
| "Per consumer ek queue kyun?" | Shared queue simpler hai | Shared queues consumer interference aur coupled scaling/retry behavior cause karti hain |
| "Events ko version kaise karein?" | Schema ko in place change karo | Explicitly version karo (`OrderCreated_v2`) ya changes backward-compatible rakho; events contracts hote hain |
| "Kya commands bhi events jaise async ho sakte hain?" | Sab kuch async | Commands synchronous hote hain (caller ko result chahiye); sirf side-effect events async hote hain |
| "DB triggers ke bajaye events kyun nahi?" | "Triggers simpler hain" | Triggers invisible hote hain, version karna hard hai, non-portable; explicit events observable aur testable hote hain |

**60-second wrap-up answer:** "CQRS with SNS/SQS mein sabse badi pitfalls hain immediate consistency, exactly-once delivery, ya shared queues assume karna. Events sirf successful commits ke baad publish honi chahiye, consumers idempotent hone chahiye, aur har consumer ko apna queue chahiye ek DLQ ke saath. Ordering, retries, aur replay explicitly design karne padte hain — warna silent data loss ya duplicate side effects mil jaate hain."

---

## 3. EventBridge & Event-Driven Architecture

### EventBridge Deep Dive

Original notes EventBridge ko sirf passing mein mention karte hain (as "CloudWatch Events renamed" aur ek Lambda trigger ke taur par) — modern event-driven .NET architectures on AWS ke liye EventBridge kitna central hai, iske hisaab se yeh apna alag treatment deserve karta hai.

**Yeh plain SNS/SQS se aage kya add karta hai:**
- **Event buses** — default bus (AWS service events), custom buses (aapke application events), aur partner buses (SaaS integrations jaise Stripe, Auth0, PagerDuty, Datadog).
- Rule level par **content-based filtering** — JSON event pattern matching (field values, prefixes, numeric ranges) ke basis par route karo bina har consumer mein filtering code likhe.
- **Schema Registry** — event schemas discover aur version karna, ek schema se strongly-typed code bindings (.NET ke liye bhi) generate karna.
- **Archive & Replay** — ek pattern match karne wale saare events record karna aur baad mein unhe replay karna (ek downstream bug fix ke baad reprocess karne ke liye bahut useful hai, bina messages ko SQS mein indefinitely rakhne ki zarurat ke).
- **Targets**: Lambda, SQS, SNS, Step Functions, ECS RunTask, Kinesis, API destinations (arbitrary HTTPS endpoints managed retries ke saath — third-party/legacy .NET webhooks call karne ke liye great).

**EventBridge vs SNS — kaunsa kab pick karein**
| | EventBridge | SNS |
|---|---|---|
| Routing logic | Per rule rich content-based filtering | Coarse (topic-level, subscriptions par optional filter policies) |
| Schema management | Built-in registry/versioning | Kuch nahi |
| Scheduled/cron | Native (`schedule: cron(...)`) | Nahi |
| SaaS source integration | Native partner event buses | Nahi |
| Latency | Slightly higher (near-real-time, hamesha sub-second nahi) | Real-time |
| Simplicity | Zyada moving parts | Simpler mental model |

**.NET example — scheduled cleanup rule (CLI/CloudFormation snippet):**
```yaml
Resources:
  NightlyCleanupRule:
    Type: AWS::Events::Rule
    Properties:
      ScheduleExpression: "cron(0 1 * * ? *)"
      Targets:
        - Arn: !GetAtt CleanupFunction.Arn
          Id: "CleanupTarget"
```

**Interview takeaway:** EventBridge **cross-service domain events routing logic ke saath** aur scheduled jobs ke liye right default hai; SNS+SQS simple, high-throughput fan-out ke liye right default rehta hai jahan aapko content filtering ya schema registry ki zarurat nahi. Kaafi real architectures dono use karte hain — bounded contexts ke beech coarse routing ke liye EventBridge, ek bounded context ke andar same-team consumers ko fan-out ke liye SNS/SQS.

### Event-Driven Architecture Reference Flow

```
  Client
    |  POST /orders
    v
  API Gateway
    |  invoke
    v
  Lambda (Ingest) ----PutItem status=PENDING + idempotencyKey----> DynamoDB
    |
    |  Publish "order_created"
    v
  SNS Topic
    +----fan-out----> SQS (Billing) --------+
    +----fan-out----> SQS (Notification)    |
                                            |  poll & invoke
                                            v
                                    Lambda (Processor)
                                            |
                +---------------------------+---------------------------+
                v                           v                           v
      ConditionalUpdate            Update status =            after maxReceiveCount
      PENDING -> PROCESSING        COMPLETED                  failures -> DLQ
           (DynamoDB)                (DynamoDB)

  Every downstream step must be idempotent: SQS is at-least-once, so the
  same message can legitimately arrive twice.
```

Yeh ek single diagram original notes ke order-processing pattern (Lambda + SQS + DynamoDB) ko SNS fan-out pattern ke saath tie karta hai — yeh dono source material mein separately documented the lekin real systems mein almost hamesha combined hote hain, aur interviewers expect karte hain ki aap explain karo yeh kaise compose hote hain.

**Production readiness checklist (original notes se, poori tarah preserved — yeh genuinely senior-level hai aur ek mental checklist ke taur par rakhna worth hai):**
- Ingest par idempotency (idempotency key ke zariye dedupe)
- DLQ + alerting configured (CloudWatch alarm DLQ depth > 0 par)
- Races prevent karne ke liye DynamoDB mein conditional updates
- Queue depth, Lambda errors, DynamoDB throttling par monitoring/alarms
- Least-privilege IAM + DynamoDB/SQS par SSE-KMS encryption
- Throughput/throttling behavior ke liye load testing
- On-demand ya autoscaled DynamoDB capacity
- SQS visibility timeout > Lambda max execution time
- Idempotent external integrations (payment/fulfillment)
- End-to-end X-Ray/OpenTelemetry tracing
- DLQ handling/replay ke liye documented operational runbooks
- Cost monitoring/budget alerts
- IAM Access Analyzer + secret scanning
- Backup/PITR + tested restore
- Documented schema evolution strategy

---

## 4. Streaming

### Amazon Kinesis

**Yeh kis liye hai:** scale par real-time **streaming** data — clickstreams, IoT telemetry, application logs, metrics, change feeds. SQS ke against distinguishing feature yeh hai ki ek stream ek **replayable, ordered log** hai jise **multiple independent consumers** har ek poora padh sakte hain, ek queue ke bajaye jahan ek message ek baar consume hota hai aur delete ho jaata hai.

**Chaar family members:**
| Service | What it does |
|---|---|
| **Kinesis Data Streams** | Raw, low-level stream jispar aap consumers banate ho. Real-time (~200 ms), replayable, per shard ordered |
| **Data Firehose** | Fully managed **delivery** — koi code nahi, koi shards nahi. Buffer karta hai aur directly S3, Redshift, OpenSearch, Splunk, ya ek HTTP endpoint mein load karta hai |
| **Managed Service for Apache Flink** (pehle Kinesis Data Analytics) | Ek stream *par* SQL ya Flink processing — windowed aggregations, anomaly detection, enrichment |
| **Kinesis Video Streams** | Playback aur ML ke liye video ingestion |

**Data Streams mechanics — yahan detail questions rehte hain:**
- Data **shards** mein rehta hai. Throughput per shard hai: **1 MB/s ya 1,000 records/s in**, aur consumers ke across shared **2 MB/s out** — ya **Enhanced Fan-Out** ke saath **per consumer 2 MB/s** (ek push model, ~70 ms latency, up to 20 consumers).
- **Partition key** decide karta hai ki record kaunsi shard mein jaata hai, aur **ordering sirf ek shard ke andar guaranteed hai**. Isliye ek partition key choose karo jo evenly distribute bhi kare *aur* jin records ko order mein rehna hai unhe saath rakhe (e.g. `customerId`). Ek low-cardinality key ek **hot shard** create karta hai — exactly wahi failure shape jaisa ek DynamoDB hot partition.
- **Retention** default mein 24 hours hai, extendable **365 days** tak. Ek record padhne se woh delete nahi hota, jo hi **replay** possible banata hai — ek consumer bug fix karne ke baad ek hafte ke events reprocess karo.
- **Capacity modes:** *Provisioned* (aap shard count manage karte ho, steady scale par cheaper) ya **On-Demand** (auto-scale hota hai, throughput ke hisaab se pay karo — jab load unknown ho to right default).
- Consumers **KCL** (ya ek Lambda event-source mapping) use karte hain aur apni position **checkpoint** karte hain; ek Lambda consumer shards ko parallel mein process karta hai, per shard ek concurrent invocation.

**Firehose vs Data Streams** woh pairing hai jo poochhi jaati hai: Firehose **near**-real-time hai (size se buffer karta hai, e.g. 1–128 MB, ya time se, e.g. 60 s), **serverless hai koi shards manage karne ki zarurat nahi**, records ko ek Lambda se transform kar sakta hai aur way mein Parquet/ORC mein convert kar sakta hai, aur **replay nahi kar sakta** — deliver hone ke baad, Firehose se gone hai. Data Streams true real-time hai, replayable hai, aur aapko shards/consumers manage karne padte hain. "Yeh data S3/OpenSearch mein reliably bina code ke daal do" → **Firehose**. "Multiple consumers, replay, sub-second, custom processing" → **Data Streams**.

**Decision table jo interviewers really chahte hain:**
| | SQS | SNS | EventBridge | Kinesis Data Streams |
|---|---|---|---|---|
| Model | Queue — ek consumer group, process hone ke baad message delete | Pub/sub push, subscribers ko fan-out | Content-based routing rules ke saath event bus | Ordered, replayable stream log |
| Consumers | Competing consumers work share karte hain | Har subscriber ko ek copy milti hai | Har matching rule ko ek copy milti hai | **Kaafi independent consumers har ek sab kuch padhte hain** |
| Ordering | Sirf FIFO queues | Nahi | Nahi | **Haan, per shard** |
| Replay | ❌ (delete hone ke baad, gone) | ❌ | ✅ Archive & Replay ke zariye | ✅ **retention window ke andar** |
| Retention | Up to 14 days | N/A (no storage) | Archive-based | Up to **365 days** |
| Throughput shape | Effectively unlimited, per-message | Per-message | Per-event | **Per shard provisioned**, high volume |
| Reach for it when | Work decouple karna, buffering, retries + DLQ | Simple fan-out notifications | SaaS/AWS-service events, filtering, schemas | High-volume analytics, multiple readers, replay |

---

## 5. Orchestration

### Step Functions: Orchestration vs Choreography

**Yeh kya hai:** ek managed **state machine** jo multiple services ko ek workflow mein coordinate karta hai, declaratively **ASL (Amazon States Language)** JSON mein defined. Yeh sequencing, branching, parallelism, retries, error handling, timeouts, aur human approval steps handle karta hai — isliye logic *configuration mein rehta hai ek visual execution history ke saath*, glue code mein buried nahi.

**Yeh ek architectural answer ke taur par kyun matter karta hai:** iske bina, multi-step business processes end mein Lambdas invoking Lambdas ban jaate hain, retry aur compensation logic har ek mein hand-rolled, aur dekhne ka koi tareeka nahi ki execution actually kahan fail hua.

**State types list karne ke liye:** `Task` (kaam karo), `Choice` (branch), `Parallel` (fan out aur join), `Map` (ek collection ke over iterate karo — **Distributed Map** millions of items tak scale hota hai, e.g. har S3 object ke liye ek execution), `Wait`, `Pass`, `Succeed`, `Fail`.

**Standard vs Express — comparison jo poochha jaata hai:**
| | **Standard** | **Express** |
|---|---|---|
| Max duration | **1 year** | **5 minutes** |
| Execution model | Exactly-once, fully durable | At-least-once |
| History | Full visual history retained (90 days) | Sirf CloudWatch Logs |
| Pricing | **Per state transition** | Per request + duration (high volume par kaafi cheaper) |
| Use for | Long-running business processes, order fulfilment, human approval, ETL | High-volume, short-lived event processing (streaming, IoT ingestion) |

**Features jo isko choose karne worth banate hain:**
- **Built-in `Retry` aur `Catch`** har state par, backoff rate, max attempts, aur error-type matching ke saath — declarative resilience, har function mein Polly ke bajaye.
- **200+ direct SDK integrations** — DynamoDB, SQS, ECS `RunTask`, SNS, Lambda, even ek doosra state machine call karo, **bina Lambda likhe** sirf call ko marshal karne ke liye. Yeh "glue Lambda" ki poori classes remove kar deta hai.
- **`.sync` / callback patterns** — ek ECS task ya Glue job ka finish hona wait karo, ya ek **task token** ke liye pause karo jab tak koi external system (ya ek human) callback na kare. Yahi tareeka hai approval workflows banane ka.
- **Saga pattern** — services ke across distributed transactions ke liye, har step par `Catch` **compensating actions** trigger karta hai (payment refund karo, inventory release karo). Yeh "microservices ke across transactions kaise karte ho jab koi two-phase commit na ho?" ka standard answer hai.

**Orchestration vs choreography — lead karne wali senior framing:**
| | **Orchestration** (Step Functions) | **Choreography** (EventBridge / SNS+SQS) |
|---|---|---|
| Control | Ek central coordinator process hold karta hai | Har service events par independently react karta hai |
| Visibility | ✅ Ek jagah poora flow dikhata hai aur kahan fail hua | ❌ Flow emergent hai — services ke across trace karna padta hai |
| Coupling | Coordinator saare participants ko jaanta hai | Publishers ko nahi pata subscribers exist karte hain |
| Change cost | Ek definition update karo | Kisi ko touch kiye bina ek subscriber add karo |
| Best for | Ek defined business process ordering, compensation, aur ek known end state ke saath | Loose, extensible fan-out jahan naye consumers time ke saath appear hote hain |

**Answer jo score karta hai:** "Main dono use karunga, different layers par. Choreography (EventBridge/SNS) bounded contexts ke beech, taaki teams coordination ke bina consumers add kar sakein. Orchestration (Step Functions) ek bounded context *ke andar* ek multi-step process ke liye jisme ordering, compensation, aur ek auditable execution history chahiye — kyunki purely event-driven chain mein ek failed order debug karna matlab hai flow ko chhe services ke logs se reconstruct karna, jabki Step Functions mujhe exact failed state dikhata hai."

**Related orchestration/eventing pieces naam lene worth:** **EventBridge Pipes** (ek point-to-point source→filter→enrich→target connector, jo woh Lambda replace kar deta hai jo aap SQS→Step Functions move karne ke liye likhte the), **EventBridge Scheduler** (scale par managed cron, CloudWatch Events scheduled rules ko supersede karta hai), aur **AWS Batch** (long-running, non-container-native jobs ke liye managed batch compute — Lambda ki 15 minutes se exceed karne wale work ke liye right home jab aapko poora ECS service nahi chahiye).

---

## 6. Managed Brokers

### Amazon MQ

**Managed Apache ActiveMQ ya RabbitMQ.** Yeh kyun exist karta hai — aur SQS/SNS se ise choose karne ki sirf ek wajah — hai **protocol compatibility**: yeh **open standards** bolta hai jo existing enterprise applications already use karti hain (**AMQP 0-9-1/1.0, MQTT, STOMP, OpenWire, JMS, WSS**), jabki SQS aur SNS proprietary AWS APIs expose karte hain.

- Aapke VPC ke andar **instances par brokers** ke taur par run hota hai (serverless nahi), **Multi-AZ active/standby failover** aur durable storage ke saath.
- Kyunki yeh broker-based hai, yeh **SQS jaisa scale nahi karta** — aap brokers ko size aur monitor karte ho, aur throughput ki ek ceiling hoti hai.
- Legacy apps ko chahiye woh messaging semantics support karta hai: topics *aur* queues, message selectors, transactions, aur request/reply.

**Interview answer:** "Agar main ek on-prem application ko **lift-and-shift** kar raha hoon jo already JMS/AMQP/MQTT bolti hai aur main uski messaging layer rewrite nahi karna chahta, to Amazon MQ migration path hai. Agar main AWS par kuch naya bana raha hoon, main SQS/SNS/EventBridge use karta hoon — yeh serverless, cheaper hain, aur broker management ke bina scale karte hain." Migration ke dauraan SQS adopt karne ke liye purely ek app ki messaging layer rewrite karna woh mistake hai jise avoid karne ke liye yeh service exist karta hai.

**Disaster Recovery — Messaging & Streaming**

| | |
|---|---|
| **Actually risk par kya hai** | In-flight messages. SQS **14 din** tak rakhta hai, Kinesis **default 24 ghante** (365 tak extendable), EventBridge **kuch nahi** rakhta jab tak Archive configure na ho, aur **SNS kuch bhi nahi rakhta** |
| **Backup mechanism** | **SQS DLQs**, **EventBridge Archive + Replay**, Kinesis extended retention, aur `SNS → SQS` fan-out pattern taaki ek durable copy exist kare |
| **Realistic RPO / RTO** | Retention se bandha hua hai, aapke backups se nahi. RTO minutes — redrive karne ka |

**Recovery runbook:**
1. **Poison messages / failed consumer:** consumer theek karo, phir DLQ redrive karo — `aws sqs start-message-move-task --source-arn <dlq-arn>` unhe natively source queue par wapas le jaata hai.
2. **EventBridge:** archive se exact incident window par **replay** shuru karo (`aws events start-replay --event-start-time … --event-end-time …`).
3. **Kinesis:** stored sequence number se dobara padho, ya agar checkpoint hi chala gaya to `TRIM_HORIZON` se.
4. **Idempotently reconcile karo** — replay *jaan-boojh kar* duplicates banata hai, toh yeh sirf tab kaam karta hai jab consumers DynamoDB section wale conditional-write idempotency pattern ke saath bane the.

⚠️ **Gotcha:** **SNS akela durable nahi hai.** Koi subscriber na ho, ya subscriber apni retry policy khatam kar de, to message chala gaya — **kahin koi record nahi**. Isi liye jo cheez matter karti hai wo `SNS → SQS` hoti hai, seedha `SNS → Lambda` nahi. Aur second-order point jo logon ko pakadta hai: **replay recovery option hai hi tabhi jab aapne pehle se idempotency banayi thi.** Nahi banayi, to ek din ke events replay karke gap bharna us gap se bada data problem paida karta hai.

---

← [Security Services](12-security-services.md) · [Index](README.md) · [Global Edge Services](14-global-edge-services.md) →
