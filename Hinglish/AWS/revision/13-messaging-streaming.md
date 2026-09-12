> **AWS Quick Revision Notes** · [Index](README.md) · Part III

# Messaging, Streaming & Decoupling

---

## 1. SQS & SNS

### SQS & SNS

- **SQS (pull queue):** decouple async, at-least-once, durable, auto-scale. Flow: send → stored → poll → visibility timeout hides → success delete → failure re-visible → maxReceiveCount → DLQ.

| | Standard | FIFO |
|---|---|---|
| Delivery | at-least-once, dups | exactly-once |
| Ordering | no | per message group |
| Throughput | very high | lower |

- **Long polling** (`WaitTimeSeconds` 20s) default. Limits: **256 KB** (larger → Extended Client / S3 claim-check), retention default 4d (60s-14d), visibility timeout (> processing time; `ChangeMessageVisibility` heartbeat), **delay queue** (0-15min before visible ≠ visibility timeout), DLQ + **redrive**, FIFO dedup (5-min window, `MessageGroupId` scopes ordering), SSE-KMS + queue policy.
- **SNS (push pub/sub):** publisher → topic → all subscribers (fan-out). ❗ **Message filtering (filter policies)** — subscriber declares JSON filter on attributes. FIFO topics (→ SQS FIFO only). Delivery retry + subscription DLQ. 256 KB.

| | SNS | SQS |
|---|---|---|
| Pattern | Pub/Sub push | Queue pull |
| Use | fan-out/notifications | decoupling/buffering |
| Durability | subscriber-dependent | high (queue durable) |

- **SNS→SQS fan-out** = most common pattern: publisher emits `order_created` to SNS; each microservice has own SQS queue → no loss if consumer down, independent scaling, no slow-consumer blocking.

---

## 2. CQRS Patterns in .NET

### CQRS with SNS/SQS (.NET)

- **Golden rule:** publish to SNS **only after** DB commit succeeds.
```csharp
await _db.SaveChangesAsync();               // 1. commit first
await _snsPublisher.PublishAsync(...);      // 2. publish after
```
- Consumer (BackgroundService): long poll → try process → **delete only after success** (catch → don't delete → SQS retries → DLQ). ❗ **Unwrap SNS envelope** (`snsEnvelope.Message`). Idempotency check (`AlreadyProcessed(eventId)`) mandatory.
- **Pitfalls:** publish before commit (use **Outbox Pattern**); shared queue across consumers; forgetting SNS envelope; no idempotency; delete before success; eventual consistency (show pending UI); version events (`OrderCreated_v2`); commands sync/events async; explicit events > DB triggers.

---

## 3. EventBridge & Event-Driven Architecture

### EventBridge

- Over SNS/SQS: event buses (default/custom/partner SaaS), **content-based filtering** (JSON pattern per rule), Schema Registry (+ .NET bindings), **Archive & Replay**, targets (Lambda/SQS/SNS/Step Functions/ECS/Kinesis/API destinations).

| | EventBridge | SNS |
|---|---|---|
| Routing | rich content filter | coarse |
| Schema | registry | none |
| Scheduled | native cron | no |
| SaaS | partner buses | no |

- EventBridge = cross-service domain events + scheduled; SNS+SQS = simple high-throughput fan-out. Real systems use both.

### Reference Flow

`Client → API GW → Lambda (PutItem PENDING + idempotencyKey) → SNS → fan-out SQS (Billing/Notification) → Lambda Processor (ConditionalUpdate PENDING→PROCESSING, status COMPLETED, maxReceiveCount → DLQ)`. Every step idempotent (at-least-once).
- **Production checklist:** idempotency, DLQ + alerting, conditional updates, monitoring, least-priv + SSE-KMS, load testing, on-demand/autoscaled capacity, visibility timeout > Lambda max, idempotent external integrations, X-Ray tracing, runbooks, cost alerts, Access Analyzer, backup/PITR, schema evolution.

---

## 4. Streaming

### Kinesis

- Real-time streaming, **replayable ordered log**, multiple independent consumers (vs queue delete-on-consume).
| Service | What |
|---|---|
| Data Streams | raw stream, ~200ms, replayable, shards |
| Firehose | managed delivery (S3/Redshift/OpenSearch/Splunk), no code |
| Managed Flink | SQL/Flink on stream |
| Video Streams | video |
- Data Streams: shards (1 MB/s in, 2 MB/s shared out or **Enhanced Fan-Out** 2 MB/s per consumer). Partition key → shard (ordering per shard); low cardinality = **hot shard**. Retention 24h (up to 365d) → replay. Provisioned vs On-Demand. KCL/Lambda + checkpoint.
- **Firehose vs Data Streams:** Firehose near-real-time, serverless, transform, **no replay**; Data Streams real-time, replayable, manage shards.

| | SQS | SNS | EventBridge | Kinesis |
|---|---|---|---|---|
| Model | queue | pub/sub | routing bus | ordered stream |
| Replay | ❌ | ❌ | ✅ archive | ✅ retention |
| Ordering | FIFO | no | no | per shard |

---

## 5. Orchestration

### Step Functions

- Managed state machine (ASL JSON) — sequencing/branching/parallel/retries/errors/timeouts/approvals; logic in config + visual history.
- States: Task/Choice/Parallel/**Map** (Distributed Map = millions)/Wait/Pass/Succeed/Fail.

| | Standard | Express |
|---|---|---|
| Max | 1 year | 5 min |
| Model | exactly-once | at-least-once |
| Pricing | per transition | per request+duration |
| Use | long business processes | high-volume short |

- Built-in `Retry`/`Catch`; 200+ direct SDK integrations (no glue Lambda); `.sync`/callback (task token for approvals); **Saga pattern** (compensating actions — distributed transactions).
- **Orchestration (Step Functions, central coordinator, visible)** vs **Choreography (EventBridge/SNS+SQS, services react independently, extensible)**. Use both at different layers.
- Related: EventBridge Pipes (source→filter→enrich→target), EventBridge Scheduler (managed cron), AWS Batch (>15min non-container work).

---

## 6. Managed Brokers

### Amazon MQ

- Managed ActiveMQ/RabbitMQ. Only reason over SQS/SNS = **protocol compatibility** (AMQP/MQTT/STOMP/OpenWire/JMS/WSS). Broker-based (not serverless), Multi-AZ, doesn't scale like SQS. For **lift-and-shift** of existing JMS/AMQP apps; new AWS → SQS/SNS/EventBridge.

**DR — Messaging:** risk = in-flight messages (SQS 14d, Kinesis 24h/365, EventBridge nothing unless Archive, **SNS nothing**). Backup = SQS DLQs, EventBridge Archive+Replay, Kinesis extended retention, SNS→SQS for durable copy. Poison → fix + redrive; EventBridge → replay window; Kinesis → re-read from sequence/TRIM_HORIZON; idempotent reconcile. ⚠️ SNS not durable alone (SNS→SQS matters); replay recovery needs pre-built idempotency.

---

← [Security Services](12-security-services.md) · [Index](README.md) · [Global Edge Services](14-global-edge-services.md) →
