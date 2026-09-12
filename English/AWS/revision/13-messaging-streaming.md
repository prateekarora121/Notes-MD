> **AWS Quick Revision Notes** · [Index](README.md) · Part III

# Messaging, Streaming & Decoupling

---

## 1. SQS & SNS

### SQS & SNS Fundamentals

**SQS** = pull queue, at-least-once, durable. Flow: send → stored → poll → **visibility timeout** hides while processing → delete on success → visible again on failure → **DLQ** after `maxReceiveCount`.
Standard (at-least-once, no order, very high throughput) vs FIFO (exactly-once dedup, ordered per Message Group, lower throughput). **Long polling** (`WaitTimeSeconds` ≤20s) default over short.
Knobs: **256 KB max** (larger → **Extended Client + S3 pointer** = claim check) · retention **4d default (60s–14d)** · **visibility timeout** 30s/max 12h (must exceed processing; extend via `ChangeMessageVisibility` heartbeat) · **delay queue** (0–15 min before visible — ≠ visibility timeout) · **DLQ + redrive** ("fix then redrive") · FIFO dedup (5-min window, `MessageGroupId` = ordering scope) · SSE-KMS + queue policy.
**SNS** = push pub/sub fan-out (SQS/Lambda/HTTP/email/SMS/mobile). Features: **❗ filter policies** (subscriber filters on attributes/body — avoid every consumer getting every event), FIFO topics (→ SQS FIFO only), per-protocol retry + subscription DLQ, 256 KB. Durability/retry come from **SQS not SNS**.
| Feature | SNS | SQS |
|---|---|---|
| Pattern | Pub/Sub push | Queue pull |
| Use | Fan-out/notifications | Decouple/buffer/guaranteed |
| Ordering | No (Standard) | FIFO optional |
| Durability | Depends on subscriber | High |

**SNS→SQS fan-out** (most common pattern): one publisher → SNS topic → each service its own SQS queue → no loss if one down, independent scale/failure, no slow-consumer blocking.

---

## 2. CQRS Patterns in .NET

### CQRS with SNS/SQS in .NET

**Golden rule: publish to SNS only AFTER the write-side DB commit** (never speculatively).
```csharp
await _db.SaveChangesAsync();                    // 1. commit first
await _snsPublisher.PublishAsync(new OrderCreatedEvent(...));  // 2. after commit
```
Publisher sets `TopicArn` + `Message` + `MessageAttributes` (eventType). Consumer (BackgroundService): `ReceiveMessageAsync` (MaxNumberOfMessages=5, WaitTimeSeconds=20) → try process → **delete only after success** → catch: don't delete (SQS retries → DLQ). **❗ Unwrap the SNS envelope** (`SnsEnvelope.Message` holds the real payload). **Idempotency check mandatory** (`ProcessedEvents.AnyAsync(EventId)`). DI: `AddAWSService<IAmazonSimpleNotificationService>()` + `IAmazonSQS` + `AddHostedService`. Traps: publish before commit; shared queue across consumers; forget envelope; no idempotency; delete before processing (message loss on crash).

### CQRS Pitfalls

Read DB not immediate → eventually consistent by design (show pending/optimistic). Why SNS not SQS directly → decouples producer from N consumers. SNS published but commit failed → publish after commit / **Outbox Pattern** (event+data in same tx, relay publishes). Processed twice → at-least-once, be idempotent (or FIFO+dedup). DLQ cause → exception + not deleted + maxReceiveCount. Delete before/after → after success. One queue per consumer → shared causes interference/coupled scaling. Version events → `OrderCreated_v2` / backward-compat (events are contracts). Commands async? → commands sync (caller needs result), only events async. Triggers vs events → events observable/testable. **Wrap-up:** don't assume immediate consistency, exactly-once, or shared queues; publish after commit; idempotent consumers; own queue + DLQ each; design ordering/retry/replay explicitly.

---

## 3. EventBridge & Event-Driven Architecture

### EventBridge Deep Dive

Adds over SNS/SQS: **event buses** (default/custom/partner SaaS), **content-based filtering** per rule, **Schema Registry** (versioning + typed .NET bindings), **Archive & Replay**, targets (Lambda/SQS/SNS/Step Functions/ECS RunTask/Kinesis/API destinations). **EventBridge vs SNS:** EventBridge (rich filtering, schema, native cron, SaaS buses, slightly higher latency) vs SNS (coarse, simpler, real-time). EventBridge = cross-service domain events + routing + scheduled jobs; SNS+SQS = simple high-throughput fan-out. Often both (EventBridge between bounded contexts, SNS/SQS within).
```yaml
NightlyCleanupRule:
  Type: AWS::Events::Rule
  Properties: { ScheduleExpression: "cron(0 1 * * ? *)", Targets: [{Arn: !GetAtt CleanupFunction.Arn, Id: CleanupTarget}] }
```

### Event-Driven Reference Flow

`Client → API GW → Lambda (Ingest, PutItem status=PENDING + idempotencyKey → DynamoDB) → SNS → fan-out to SQS (Billing/Notification) → Lambda Processor (ConditionalUpdate PENDING→PROCESSING, status=COMPLETED, else DLQ after maxReceiveCount)`. Every step idempotent (SQS at-least-once). **Production checklist:** idempotency on ingest · DLQ + alarm on depth>0 · conditional updates · alarms on queue depth/Lambda errors/DynamoDB throttling · least-priv IAM + SSE-KMS · load testing · on-demand/autoscaled capacity · visibility timeout > Lambda max exec · idempotent external integrations · X-Ray/OTel · DLQ runbooks · cost/budget alerts · Access Analyzer + secret scanning · backup/PITR + tested restore · schema evolution strategy.

---

## 4. Streaming

### Kinesis

Real-time **streaming** — replayable ordered log, multiple independent consumers each read in full (vs SQS consume-once).
| Service | Does |
|---|---|
| Data Streams | Raw stream, ~200ms, replayable, ordered per shard |
| **Firehose** | Managed delivery (no code/shards) → S3/Redshift/OpenSearch/Splunk/HTTP |
| Managed Flink | SQL/Flink over stream |
| Video Streams | Video |

**Data Streams:** **shards** — 1 MB/s or 1,000 rec/s in, 2 MB/s out shared (or 2 MB/s/consumer with **Enhanced Fan-Out**, ≤20). **Partition key** decides shard + ordering only within shard (low cardinality = hot shard). Retention 24h default → 365d; reading doesn't delete → **replay**. Provisioned vs **On-Demand**. KCL/Lambda ESM checkpoint (one concurrent invoke per shard).
**Firehose vs Data Streams:** Firehose near-real-time (buffer size/time), serverless no shards, Lambda transform + Parquet convert, **can't replay**. Data Streams true real-time, replayable, manage shards. "Just get data into S3/OpenSearch, no code" → Firehose; "multiple consumers, replay, sub-second, custom" → Data Streams.
| | SQS | SNS | EventBridge | Kinesis |
|---|---|---|---|---|
| Model | Queue, deleted after | Push fan-out | Bus + routing rules | Ordered replayable log |
| Consumers | Competing | Each a copy | Each rule a copy | **Many read everything** |
| Ordering | FIFO only | No | No | Per shard |
| Replay | ❌ | ❌ | ✅ Archive | ✅ retention window |
| Retention | ≤14d | N/A | Archive | ≤365d |

---

## 5. Orchestration

### Step Functions

Managed state machine (ASL JSON): sequencing, branching, parallelism, retries, error handling, timeouts, human approval — logic as config with visual history. States: `Task`/`Choice`/`Parallel`/`Map` (**Distributed Map** = millions)/`Wait`/`Pass`/`Succeed`/`Fail`.
| | Standard | Express |
|---|---|---|
| Max | 1 year | 5 min |
| Model | Exactly-once durable | At-least-once |
| History | Full visual (90d) | CloudWatch Logs |
| Pricing | Per state transition | Per request + duration |
| Use | Long processes, approval, ETL | High-volume short event processing |

Features: built-in `Retry`/`Catch` per state (declarative resilience vs Polly everywhere), 200+ direct SDK integrations (no glue Lambda), `.sync`/callback (task token for approvals), **Saga pattern** (Catch → compensating actions — cross-service transactions without 2PC).
**Orchestration** (Step Functions — central coordinator, one place shows flow/failure) vs **choreography** (EventBridge/SNS+SQS — services react to events, flow emergent). Answer: both — choreography between bounded contexts, orchestration within (ordering/compensation/auditable history). Related: **EventBridge Pipes** (source→filter→enrich→target), **EventBridge Scheduler** (cron at scale), **AWS Batch** (long-running > Lambda 15 min).

---

## 6. Managed Brokers

### Amazon MQ

Managed ActiveMQ/RabbitMQ. Only reason vs SQS/SNS = **protocol compatibility** (AMQP/MQTT/STOMP/OpenWire/JMS/WSS — open standards, SQS/SNS are proprietary). Brokers on instances in VPC (not serverless), Multi-AZ active/standby, doesn't scale like SQS. "**Lift-and-shift** an app already speaking JMS/AMQP/MQTT → Amazon MQ; building new → SQS/SNS/EventBridge."

**Messaging DR:** at risk = in-flight messages (SQS ≤14d, Kinesis 24h default→365, EventBridge nothing unless Archive, **SNS nothing**). Backup = DLQs, EventBridge Archive+Replay, Kinesis extended retention, SNS→SQS fan-out. Poison messages → fix + redrive DLQ (`start-message-move-task`); EventBridge → `start-replay` over window; Kinesis → re-read sequence / TRIM_HORIZON. **❗ SNS alone isn't durable** (no subscriber / exhausted retry = gone) → `SNS→SQS` not `SNS→Lambda`. **Replay only recovers if you built for idempotency** (else replay makes a bigger problem).

---

← [Security Services](12-security-services.md) · [Index](README.md) · [Global Edge Services](14-global-edge-services.md) →
