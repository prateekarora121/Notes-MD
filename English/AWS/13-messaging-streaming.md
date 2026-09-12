> **AWS Detailed Guide** · [Index](README.md) · Part III

# Messaging, Streaming & Decoupling

---

## 1. SQS & SNS

### SQS & SNS Fundamentals

**SQS (Simple Queue Service) — pull-based queue**
- Decouples producers/consumers asynchronously; at-least-once delivery; durable (replicated across AZs); scales automatically.
- Flow: producer sends → stored durably → consumer polls → message hidden via **visibility timeout** while processing → on success, consumer deletes it → on failure, it becomes visible again → after `maxReceiveCount` retries, moves to **DLQ**.

| | Standard Queue | FIFO Queue |
|---|---|---|
| Delivery | At-least-once, possible duplicates | Exactly-once processing (with dedup) |
| Ordering | Not guaranteed | Guaranteed (per Message Group) |
| Throughput | Very high | Lower (throughput quota per message group, though high-throughput mode raises this) |
| Use case | Most workloads | Orders, payments, anything requiring strict per-entity order |

- **Long polling** (`WaitTimeSeconds` up to 20s): reduces empty-receive cost and improves latency vs short polling. **Short polling** samples only a subset of servers, so it can return empty even when messages exist — long polling should be the default, and setting it at the queue level (`ReceiveMessageWaitTimeSeconds`) is cheaper than paying for empty receives.

**SQS limits and knobs that get asked precisely:**
| Setting | Detail |
|---|---|
| **Message size** | **256 KB maximum.** For larger payloads use the **SQS Extended Client Library** — it stores the body in **S3** and puts a pointer in the message. The "claim check" pattern; the same applies to SNS |
| **Retention** | **4 days by default**, configurable **60 seconds to 14 days**. Messages are deleted after this whether processed or not |
| **Visibility timeout** | Default 30 s, max 12 h. Must exceed your worst-case processing time or the message reappears and gets processed twice. For variable work, call **`ChangeMessageVisibility`** to extend the lease mid-processing (a "heartbeat") rather than setting one huge global timeout |
| **Delay queue** | Delays **new** messages for 0–15 minutes *before they become visible at all*. **Not the same as visibility timeout**, which hides a message *after* it's received — that distinction is a favourite question |
| **DLQ + redrive** | After `maxReceiveCount` failed receives, the message moves to the DLQ. **Redrive** moves them back to the source queue once the bug is fixed — so name "fix, then redrive" rather than just "it goes to a DLQ" |
| **FIFO dedup** | Either a content-based hash or an explicit `MessageDeduplicationId`, over a **5-minute** dedup window. `MessageGroupId` is what defines the ordering scope — one slow group does not block others |
| **Encryption / access** | SSE-KMS at rest, and a **queue policy** (resource-based) for cross-account or SNS/S3/EventBridge access |

**SNS features worth naming:**
- **❗ Message filtering (filter policies)** — subscribers declare a JSON filter on message **attributes** (or, with payload-based filtering, the body), so a subscriber receives only the events it cares about. This is the answer to "how do you avoid every consumer receiving every event and filtering in code?" — filtering at the topic saves invocations, cost, and consumer complexity. A notable gap in most candidates' answers.
- **FIFO topics** — ordered, deduplicated fan-out, but they can only deliver to **SQS FIFO queues**.
- **Delivery retry policies** per protocol, plus a **subscription-level DLQ** for messages SNS can't deliver.
- **Message size 256 KB**, same as SQS, with the same S3 claim-check workaround.
- Cross-region and cross-account delivery, and `MessageStructure=json` for protocol-specific payloads (a different body for SMS than for SQS).

**SNS (Simple Notification Service) — push-based pub/sub**
- Publisher → topic → SNS pushes to *all* subscribers (fan-out) instantly. Subscribers: SQS, Lambda, HTTP(S), email/SMS, mobile push.
- Delivery retries use exponential backoff on the SNS side, but **durability and retry semantics for asynchronous processing should come from SQS**, not SNS alone — SNS→Lambda direct has no built-in DLQ-backed retry buffer the way SNS→SQS→consumer does.

| Feature | SNS | SQS |
|---|---|---|
| Pattern | Pub/Sub | Queue |
| Delivery | Push | Pull |
| Use case | Fan-out, notifications | Decoupling, buffering, guaranteed processing |
| Real-time | Yes | No (polling-based) |
| Ordering | No (Standard) | FIFO optional |
| Durability | Depends on subscriber/retry policy | High (queue itself is durable) |

**SNS → SQS fan-out pattern** (the most common AWS microservices pattern in the notes): one publisher emits a domain event (`order_created`) to an SNS topic; each interested microservice (Billing, Notification, Analytics, Inventory) has its **own** SQS queue subscribed to that topic. Benefits: no message loss if one consumer is down, independent scaling/failure per consumer, no risk of one slow consumer blocking others.

**When to use SQS alone:** async processing, retry+DLQ, buffering bursty load, worker-pool consumption (file processing, transcoding, batch/ETL).

**When to use SNS alone:** broadcasting to multiple heterogeneous consumer types, low-latency pub/sub, email/SMS/mobile notifications.

**When to combine (recommended default for event-driven microservices):** SNS gives fan-out; SQS gives durability, retries, and per-consumer isolation.

---

## 2. CQRS Patterns in .NET

### CQRS with SNS/SQS in .NET

**Golden rule:** publish the domain event to SNS **only after** the write-side DB transaction commits successfully — never publish speculatively before commit.

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

**Consumer (BackgroundService) with the critical SNS envelope detail:**

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

**DLQ mechanics:** configure `maxReceiveCount` (e.g., 5); after that many failed receives without deletion, SQS auto-moves the message to the configured DLQ. Recovery is inspect → fix root cause → redrive to source queue (console or automation).

**Registration (`Program.cs`):**
```csharp
builder.Services.AddAWSService<IAmazonSimpleNotificationService>();
builder.Services.AddAWSService<IAmazonSQS>();
builder.Services.AddSingleton<SnsPublisher>();
builder.Services.AddHostedService<OrderCreatedConsumer>();
```

**Common interview traps for this pattern:** publishing to SNS *before* DB commit; sharing one SQS queue across multiple unrelated consumers; forgetting to unwrap the SNS envelope; no idempotency tracking; deleting the SQS message before processing completes (guarantees message loss on crash).

### CQRS + SNS/SQS Interview Pitfalls

| Pitfall / Question | Wrong instinct | Correct senior answer |
|---|---|---|
| "Read DB should reflect writes immediately" | Expecting strong consistency | CQRS is eventually consistent by design; UI should show pending/optimistic state, use polling/WebSocket/read-your-own-write cache |
| "Why not SQS directly instead of SNS?" | "One queue is enough" | SNS decouples producer from N consumers; each gets its own queue and fails/scales independently |
| "SNS published but DB commit failed" | Publish first, DB later | Publish only after commit; for stronger guarantees use the **Outbox Pattern** (write event + data in the same DB transaction, a separate relay publishes it) |
| "Same message processed twice" | "AWS guarantees once" | SQS is at-least-once; consumers must be idempotent (EventId tracking, upserts, or FIFO + `MessageDeduplicationId`) |
| "What causes DLQ delivery?" | "DLQ is manual" | Consumer exception, message not deleted, `maxReceiveCount` exceeded, or visibility timeout misconfiguration |
| "Delete before or after processing?" | Delete first to dedupe | Delete only **after** success — deleting first risks silent data loss on crash |
| "Why one queue per consumer?" | Shared queue is simpler | Shared queues cause consumer interference and coupled scaling/retry behavior |
| "How do you version events?" | Change schema in place | Version explicitly (`OrderCreated_v2`) or keep changes backward-compatible; events are contracts |
| "Can commands be async like events?" | Everything async | Commands are synchronous (caller needs a result); only side-effect events are async |
| "Why not DB triggers instead of events?" | "Triggers are simpler" | Triggers are invisible, hard to version, non-portable; explicit events are observable and testable |

**60-second wrap-up answer:** "The biggest pitfalls in CQRS with SNS/SQS are assuming immediate consistency, exactly-once delivery, or shared queues. Events must publish only after successful commits, consumers must be idempotent, and each consumer needs its own queue with a DLQ. Ordering, retries, and replay have to be designed explicitly — otherwise you get silent data loss or duplicate side effects."

---

## 3. EventBridge & Event-Driven Architecture

### EventBridge Deep Dive

The original notes mention EventBridge only in passing (as "CloudWatch Events renamed" and as a Lambda trigger) — given how central EventBridge is to modern event-driven .NET architectures on AWS, it deserves its own treatment.

**What it adds over plain SNS/SQS:**
- **Event buses** — default bus (AWS service events), custom buses (your application events), and partner buses (SaaS integrations like Stripe, Auth0, PagerDuty, Datadog).
- **Content-based filtering** at the rule level — route based on JSON event pattern matching (field values, prefixes, numeric ranges) without writing filtering code in every consumer.
- **Schema Registry** — discover and version event schemas, generate strongly-typed code bindings (including for .NET) from a schema.
- **Archive & Replay** — record all events matching a pattern and replay them later (very useful for reprocessing after a downstream bug fix, without needing to keep messages in SQS indefinitely).
- **Targets**: Lambda, SQS, SNS, Step Functions, ECS RunTask, Kinesis, API destinations (arbitrary HTTPS endpoints with managed retries — great for calling third-party/legacy .NET webhooks).

**EventBridge vs SNS — when to pick which**
| | EventBridge | SNS |
|---|---|---|
| Routing logic | Rich content-based filtering per rule | Coarse (topic-level, optional filter policies on subscriptions) |
| Schema management | Built-in registry/versioning | None |
| Scheduled/cron | Native (`schedule: cron(...)`) | No |
| SaaS source integration | Native partner event buses | No |
| Latency | Slightly higher (near-real-time, not always sub-second) | Real-time |
| Simplicity | More moving parts | Simpler mental model |

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

**Interview takeaway:** EventBridge is the right default for **cross-service domain events with routing logic** and scheduled jobs; SNS+SQS remains the right default for simple, high-throughput fan-out where you don't need content filtering or a schema registry. Many real architectures use both — EventBridge for coarse routing between bounded contexts, SNS/SQS within a bounded context for fan-out to same-team consumers.

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

This single diagram ties together the order-processing pattern from the original notes (Lambda + SQS + DynamoDB) with the SNS fan-out pattern — the two were documented separately in the source material but are almost always combined in real systems, and interviewers expect you to explain how they compose.

**Production readiness checklist (from the original notes, preserved in full — this is genuinely senior-level and worth keeping verbatim as a mental checklist):**
- Idempotency on ingest (dedupe via idempotency key)
- DLQ + alerting configured (CloudWatch alarm on DLQ depth > 0)
- Conditional updates in DynamoDB to prevent races
- Monitoring/alarms on queue depth, Lambda errors, DynamoDB throttling
- Least-privilege IAM + SSE-KMS encryption on DynamoDB/SQS
- Load testing for throughput/throttling behavior
- On-demand or autoscaled DynamoDB capacity
- SQS visibility timeout > Lambda max execution time
- Idempotent external integrations (payment/fulfillment)
- X-Ray/OpenTelemetry tracing end-to-end
- Documented operational runbooks for DLQ handling/replay
- Cost monitoring/budget alerts
- IAM Access Analyzer + secret scanning
- Backup/PITR + tested restore
- Documented schema evolution strategy

---

## 4. Streaming

### Amazon Kinesis

**What it's for:** real-time **streaming** data at scale — clickstreams, IoT telemetry, application logs, metrics, change feeds. The distinguishing feature versus SQS is that a stream is a **replayable, ordered log** that **multiple independent consumers** can each read in full, rather than a queue where a message is consumed once and deleted.

**The four family members:**
| Service | What it does |
|---|---|
| **Kinesis Data Streams** | The raw, low-level stream you build consumers against. Real-time (~200 ms), replayable, ordered per shard |
| **Data Firehose** | Fully managed **delivery** — no code, no shards. Buffers and loads straight into S3, Redshift, OpenSearch, Splunk, or an HTTP endpoint |
| **Managed Service for Apache Flink** (formerly Kinesis Data Analytics) | SQL or Flink processing *over* a stream — windowed aggregations, anomaly detection, enrichment |
| **Kinesis Video Streams** | Video ingestion for playback and ML |

**Data Streams mechanics — this is where the detail questions live:**
- Data lives in **shards**. Throughput is per shard: **1 MB/s or 1,000 records/s in**, and **2 MB/s out** shared across consumers — or **2 MB/s per consumer** with **Enhanced Fan-Out** (a push model, ~70 ms latency, up to 20 consumers).
- The **partition key** decides which shard a record lands on, and **ordering is guaranteed only within a shard**. So pick a partition key that both distributes evenly *and* keeps records that must stay in order together (e.g. `customerId`). A low-cardinality key creates a **hot shard** — exactly the same failure shape as a DynamoDB hot partition.
- **Retention** is 24 hours by default, extendable to **365 days**. Reading a record does not delete it, which is what makes **replay** possible — reprocess a week of events after fixing a consumer bug.
- **Capacity modes:** *Provisioned* (you manage shard count, cheaper at steady scale) or **On-Demand** (auto-scales, pay per throughput — the right default when the load is unknown).
- Consumers use the **KCL** (or a Lambda event-source mapping) and **checkpoint** their position; a Lambda consumer processes shards in parallel, one concurrent invocation per shard.

**Firehose vs Data Streams** is the pairing that gets asked: Firehose is **near**-real-time (it buffers by size, e.g. 1–128 MB, or time, e.g. 60 s), **serverless with no shards to manage**, can transform records with a Lambda and convert to Parquet/ORC on the way, and **cannot replay** — once delivered, it's gone from Firehose. Data Streams is true real-time, replayable, and requires you to manage shards/consumers. "Just get this data into S3/OpenSearch reliably with no code" → **Firehose**. "Multiple consumers, replay, sub-second, custom processing" → **Data Streams**.

**The decision table interviewers are really after:**
| | SQS | SNS | EventBridge | Kinesis Data Streams |
|---|---|---|---|---|
| Model | Queue — one consumer group, message deleted after processing | Pub/sub push, fan-out to subscribers | Event bus with content-based routing rules | Ordered, replayable stream log |
| Consumers | Competing consumers share the work | Each subscriber gets a copy | Each matching rule gets a copy | **Many independent consumers each read everything** |
| Ordering | FIFO queues only | No | No | **Yes, per shard** |
| Replay | ❌ (once deleted, gone) | ❌ | ✅ via Archive & Replay | ✅ **within the retention window** |
| Retention | Up to 14 days | N/A (no storage) | Archive-based | Up to **365 days** |
| Throughput shape | Effectively unlimited, per-message | Per-message | Per-event | **Provisioned per shard**, high volume |
| Reach for it when | Decoupling work, buffering, retries + DLQ | Simple fan-out notifications | SaaS/AWS-service events, filtering, schemas | High-volume analytics, multiple readers, replay |

---

## 5. Orchestration

### Step Functions: Orchestration vs Choreography

**What it is:** a managed **state machine** that coordinates multiple services into a workflow, defined declaratively in **ASL (Amazon States Language)** JSON. It handles sequencing, branching, parallelism, retries, error handling, timeouts, and human approval steps — so that logic lives in *configuration with a visual execution history*, not buried in glue code.

**Why it matters as an architectural answer:** without it, multi-step business processes end up as Lambdas invoking Lambdas, with retry and compensation logic hand-rolled in each one and no way to see where an execution actually failed.

**State types to be able to list:** `Task` (do work), `Choice` (branch), `Parallel` (fan out and join), `Map` (iterate over a collection — **Distributed Map** scales to millions of items, e.g. one execution per S3 object), `Wait`, `Pass`, `Succeed`, `Fail`.

**Standard vs Express — the comparison that gets asked:**
| | **Standard** | **Express** |
|---|---|---|
| Max duration | **1 year** | **5 minutes** |
| Execution model | Exactly-once, fully durable | At-least-once |
| History | Full visual history retained (90 days) | CloudWatch Logs only |
| Pricing | **Per state transition** | Per request + duration (much cheaper at high volume) |
| Use for | Long-running business processes, order fulfilment, human approval, ETL | High-volume, short-lived event processing (streaming, IoT ingestion) |

**The features that make it worth choosing:**
- **Built-in `Retry` and `Catch`** per state, with backoff rate, max attempts, and error-type matching — declarative resilience instead of Polly in every function.
- **Over 200 direct SDK integrations** — call DynamoDB, SQS, ECS `RunTask`, SNS, Lambda, even another state machine, **without writing a Lambda** just to marshal the call. This removes whole classes of "glue Lambda".
- **`.sync` / callback patterns** — wait for an ECS task or Glue job to finish, or pause for a **task token** until an external system (or a human) calls back. That's how approval workflows are built.
- **The Saga pattern** — for distributed transactions across services, `Catch` on each step triggers **compensating actions** (refund the payment, release the inventory). This is the standard answer to "how do you do transactions across microservices when there's no two-phase commit?"

**Orchestration vs choreography — the senior framing to lead with:**
| | **Orchestration** (Step Functions) | **Choreography** (EventBridge / SNS+SQS) |
|---|---|---|
| Control | A central coordinator holds the process | Each service reacts to events independently |
| Visibility | ✅ One place shows the whole flow and where it failed | ❌ Flow is emergent — you must trace across services |
| Coupling | Coordinator knows all participants | Publishers don't know subscribers exist |
| Change cost | Update one definition | Add a subscriber without touching anyone |
| Best for | A defined business process with ordering, compensation, and a known end state | Loose, extensible fan-out where new consumers appear over time |

**The answer that scores:** "I'd use both, at different layers. Choreography (EventBridge/SNS) between bounded contexts, so teams add consumers without coordination. Orchestration (Step Functions) *within* a bounded context for a multi-step process that needs ordering, compensation, and an auditable execution history — because debugging a failed order in a purely event-driven chain means reconstructing the flow from logs across six services, whereas Step Functions shows me the exact failed state."

**Related orchestration/eventing pieces worth naming:** **EventBridge Pipes** (a point-to-point source→filter→enrich→target connector, replacing the Lambda you'd otherwise write to move SQS→Step Functions), **EventBridge Scheduler** (managed cron at scale, superseding CloudWatch Events scheduled rules), and **AWS Batch** (managed batch compute for long-running, non-container-native jobs — the right home for work exceeding Lambda's 15 minutes when you don't want a full ECS service).

---

## 6. Managed Brokers

### Amazon MQ

**Managed Apache ActiveMQ or RabbitMQ.** The reason it exists — and the only reason to choose it over SQS/SNS — is **protocol compatibility**: it speaks the **open standards** existing enterprise applications already use (**AMQP 0-9-1/1.0, MQTT, STOMP, OpenWire, JMS, WSS**), whereas SQS and SNS expose proprietary AWS APIs.

- Runs as **brokers on instances inside your VPC** (not serverless), with **Multi-AZ active/standby failover** and durable storage.
- Because it's broker-based, it **does not scale like SQS** — you size and monitor brokers, and throughput has a ceiling.
- Supports the messaging semantics legacy apps expect: topics *and* queues, message selectors, transactions, and request/reply.

**The interview answer:** "If I'm **lift-and-shifting** an on-prem application that already speaks JMS/AMQP/MQTT and I don't want to rewrite its messaging layer, Amazon MQ is the migration path. If I'm building something new on AWS, I use SQS/SNS/EventBridge instead — they're serverless, cheaper, and scale without broker management." Rewriting an app's messaging layer purely to adopt SQS during a migration is the mistake this service exists to avoid.

**Disaster Recovery — Messaging & Streaming**

| | |
|---|---|
| **What's actually at risk** | In-flight messages. SQS holds up to **14 days**, Kinesis **24 hours by default** (extendable to 365), EventBridge holds **nothing** unless you configured an Archive, and **SNS holds nothing at all** |
| **Backup mechanism** | **SQS DLQs**, **EventBridge Archive + Replay**, Kinesis extended retention, and the `SNS → SQS` fan-out pattern so a durable copy exists |
| **Realistic RPO / RTO** | Bounded by retention, not by your backups. RTO minutes to redrive |

**Recovery runbook:**
1. **Poison messages / failed consumer:** fix the consumer, then redrive the DLQ — `aws sqs start-message-move-task --source-arn <dlq-arn>` moves them back to the source queue natively.
2. **EventBridge:** start a **replay** from the archive over the exact incident window (`aws events start-replay --event-start-time … --event-end-time …`).
3. **Kinesis:** re-read from the stored sequence number, or from `TRIM_HORIZON` if the checkpoint itself is gone.
4. **Reconcile idempotently** — replay *deliberately* produces duplicates, so this only works if the consumers were built with the conditional-write idempotency pattern from the DynamoDB section.

⚠️ **The gotcha:** **SNS on its own is not durable.** No subscriber, or a subscriber that exhausts its retry policy, means the message is gone with **no record anywhere** — which is why anything that matters is `SNS → SQS`, not `SNS → Lambda` directly. And the second-order point that catches people out: **replay is only a recovery option if you built for idempotency beforehand.** If you didn't, replaying a day of events to fix a gap creates a bigger data problem than the gap.

---

← [Security Services](12-security-services.md) · [Index](README.md) · [Global Edge Services](14-global-edge-services.md) →
