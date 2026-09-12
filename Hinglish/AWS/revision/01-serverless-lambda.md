> **AWS Quick Revision Notes** · [Index](README.md) · Part I

# Serverless & Lambda

---

## 1. Lambda Core

### Lambda Deep Dive

**Q: Lambda kya hai?** A: Serverless compute — code upload, AWS trigger par run karta hai, per-invocation + duration (ms × memory) billed. Event-driven, auto-scale, multi-AZ default.

**Runtimes:** Node, Python, .NET 6/8 (AOT + arm64), Java, Go, custom (Rust).

**Execution model (Firecracker micro-VM):** INIT (cold start — VM, runtime bootstrap, static initializers, DI, DB connect — "handler ke bahar" code) → INVOKE (handler runs, max 15 min) → FREEZE (warm reuse, globals/connections/`/tmp` persist, 1-10ms) → SHUTDOWN (no hook, state lost).

- **INIT do halves:** runtime bootstrap (platform: CoreCLR, JIT, runtime client poll loop) + static initializers (mera: CLR per type per load context ek baar, thread-safe). **Trap:** throwing static initializer poore environment ke liye type poison kar deta hai.
- **Cold vs warm:** .NET/Java (JIT) 300-1500ms; Node/Python 50-200ms; **.NET Native AOT 50-100ms**. Warm 1-10ms.
- **Mitigations:** AOT, small package, avoid heavy DI graphs, avoid VPC unless needed, Provisioned Concurrency for latency-sensitive.

**Triggers & failure semantics:**
| Type | Examples | Semantics |
|---|---|---|
| Sync | API GW, ALB, Step Functions, Invoke | Caller ko error; caller retries |
| Async | S3, SNS, EventBridge, SES | Lambda auto-retries (default **2**, total 3); DLQ/destination on exhaustion |
| Poll (event source mapping) | DynamoDB Streams, Kinesis, SQS, MSK | Internally polls; retry till success/maxReceiveCount/DLQ |

**Kab NAHI:** <50ms tail latency SLA, >15 min work, >10GB memory/`/tmp`, vendor lock-in.

**.NET specifics:** .NET 8 Native AOT (source-gen JSON, trimming caveats); `Amazon.Lambda.AspNetCoreServer` for lift-and-shift; cache `IServiceProvider`/`DbContext`/client as **static** (handler ke bahar) for warm reuse; RDS connection exhaustion → RDS Proxy.

**Deployment surface:**
- `$LATEST` mutable; **publish version** = immutable snapshot; **alias** = movable pointer (`prod`→v7) supporting **weighted routing** (canary). **CodeDeploy** automates (`Canary10Percent5Minutes` + alarm rollback).
- **Layers:** shared deps/extensions; 5 layers, 250 MB unzipped.
- **Destinations:** async result routing (`onSuccess`/`onFailure`) — carries response + request context (better than DLQ).
- **SnapStart = Java only**; .NET → AOT + provisioned concurrency.

### Lambda Concurrency

| | Reserved | Provisioned |
|---|---|---|
| Purpose | Capacity guarantee + cap | Cold starts eliminate |
| Cost | Extra nahi | Hourly billed |
| Above limit | Throttled | Falls back to cold scaling |

One-liner: **Reserved = capacity guarantee. Provisioned = cold starts eliminate.**

- Default regional limit 1,000; burst ~1,000 instant, phir +500/min.
- **Sizing:** `Concurrency ≈ Peak RPS × Avg Duration(s) × Safety(1.3-2.0)`. Multi-region: independent pools; DR region par failover se **pehle** limit pre-raise karo.
- **SQS-driven:** concurrency cap kar sakte ho — backlog back-pressure absorb karta hai, DB protect.

**Traps:** 2nd time faster = warm start (env reuse); timeout = wall-clock (network waits include); SQS reprocess = delete only after success; DB write + crash = no transaction awareness, idempotency mandatory; exactly-once = nahi (at-least-once only); VPC slower = ENI attachment; fat Lambda = anti-pattern; 15-min hard cap.

**ENI vs VPC Endpoint:** ENI = private resource access (cold-start cost); VPC Endpoint (Gateway for S3/DynamoDB, Interface/PrivateLink for rest) = reach AWS services without NAT/internet.

---

## 2. Choosing Compute

### Lambda vs ECS vs Fargate

- **ECS = orchestrator; Fargate = capacity provider** (not rival products). Real comparison: ECS on EC2 vs ECS on Fargate.
- **Deciding axis = capacity kiski hai.** EC2 launch type owns instances (SSH, GPU, custom AMI, daemons, Spot/RI bin-packing) but pays AMI patching + slower scaling + idle bill.
- **Cost inversion:** low/spiky → Lambda cheapest; steady high-density → EC2 wins (Spot/Savings Plans).
- **IAM:** Lambda = execution role; ECS = task role (app) + task execution role (pull/logs) — confusing these = classic trap.

---

## 3. Event-Driven Patterns

### Serverless & S3→Lambda Trigger

- "Serverless" = no provisioning, scale-to-zero, pay-per-use, HA built-in. Set: Lambda, Fargate, S3, DynamoDB, SQS/SNS/EventBridge, API GW, Step Functions, Aurora Serverless v2.
- **Flow:** S3 event notification (`s3:ObjectCreated:*`, prefix/suffix filter) → Lambda gets **bucket+key** (not object) → GetObject. Needs resource-based policy allowing `s3.amazonaws.com` + execution role `s3:GetObject`/`PutObject`.

```json
{ "Effect": "Allow", "Principal": {"Service": "s3.amazonaws.com"},
  "Action": "lambda:InvokeFunction", "Resource": "arn:aws:lambda:...:function:process-upload",
  "Condition": { "StringEquals": {"AWS:SourceAccount": "123456789012"},
    "ArnLike": {"AWS:SourceArn": "arn:aws:s3:::my-upload-bucket"} } }
```
- **Gotchas:** ❗ **infinite recursion** (output same bucket+matching prefix → fix: separate bucket/prefix; AWS has recursion detection); **at-least-once** delivery → idempotent handler (key on object key + ETag).
- **EventBridge route kab:** content filtering, multiple targets, DLQ retries, archive/replay.

**DR — Lambda:** durable kuch nahi (code in Git, config in IaC); risk = in-flight events + event-source wiring. Backup = versions/aliases (rollback), DLQ. Bad deploy → point alias to previous version (`update-alias --function-version 41`). Regional failure → re-apply IaC + recreate event-source mappings (regional, not part of function). ⚠️ No DLQ = lost events vanish; stream trigger retention 24h.

---

← [Resume-Aligned Priority Map](00-resume-aligned-priority-map.md) · [Index](README.md) · [DynamoDB](02-dynamodb.md) →
