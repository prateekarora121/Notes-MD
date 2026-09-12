> **AWS Detailed Guide** · [Index](README.md) · Part I

# Observability & Monitoring

---

## 1. CloudWatch — Metrics, Logs & Alarms

### CloudWatch Deep Dive

**What it is:** AWS's core observability platform — Metrics, Logs, Alarms, Dashboards, Events (EventBridge), and integration with Traces (X-Ray, technically a separate service).

**Logs**
- Hierarchy: `Log Group → Log Streams → Log Events` (e.g., `/aws/lambda/ProcessOrder` with one stream per container instance).
- **CloudWatch Logs Insights**: SQL-like query language over logs.
```
fields @timestamp, @message
| filter @message like /ERROR/
| sort @timestamp desc
```
```
fields @timestamp, @duration
| filter @duration > 500
```
- Structured JSON logging makes fields directly queryable — a strong, low-effort win worth mentioning proactively in interviews.

**Metrics (know the key ones per service)**
| Service | Key metrics |
|---|---|
| Lambda | Invocations, Errors, Duration, Throttles, ConcurrentExecutions, IteratorAge (stream sources) |
| API Gateway | 2xx/4xx/5xx, Latency (p50/p90/p99), IntegrationLatency |
| SQS | ApproximateNumberOfMessagesVisible, ApproximateAgeOfOldestMessage |
| DynamoDB | ReadThrottleEvents, WriteThrottleEvents, ConsumedCapacity |
| EC2 | CPUUtilization, DiskReadOps/DiskWriteOps |

You can also publish custom business metrics (`PutMetricData`) — orders/minute, payment failure rate, pipeline throughput.

**Metric Math:** lets you combine multiple existing metrics into a derived formula *inside* CloudWatch itself, without publishing a separate custom metric or computing it client-side. The canonical example is an error-rate percentage: `(m1 / m2) * 100` where `m1` = `5xxRate` (or `Errors`) and `m2` = `TotalRequests` (or `Invocations`) — this gives you a single alarm-able, graphable series ("5xx error rate %") instead of eyeballing two separate raw-count graphs and doing the division in your head. Worth naming as the answer to "how would you alarm on an error *rate* rather than a raw error *count*" — raw counts are misleading at varying traffic volumes, and Metric Math is the built-in way to normalize for that without extra instrumentation code.

**Alarms → Action patterns**
| Alarm | Metric | Action |
|---|---|---|
| Queue backlog | `ApproximateNumberOfMessagesVisible > 1000` | SNS alert / scale consumer |
| DynamoDB throttling | `WriteThrottleEvents > 0` | Auto-scale provisioned capacity |
| Lambda failures | `Errors > 5%` | PagerDuty/email |
| API 5xx spike | `5xxErrorRate > 2%` | Alert dev team |

**EventBridge (formerly CloudWatch Events):** routes events from AWS services, your apps, and SaaS sources to Lambda/SQS/SNS/Step Functions/ECS — includes scheduled rules (`cron(0 1 * * ? *)`).

**CloudWatch vs CloudTrail (a classic trick question):** CloudWatch = observability (logs/metrics/alarms about *behavior/performance*); CloudTrail = governance/audit (a record of *who called which API, when*). They answer different questions and are not interchangeable.

**Embedded Metric Format (EMF):** structured JSON log format that CloudWatch automatically extracts into metrics — useful for high-cardinality custom metrics from Lambda without extra `PutMetricData` calls (and their associated API cost/throttling).

**Interview-ready closing summary:** "CloudWatch is AWS's core observability service — Logs, Metrics, Alarms, EventBridge, and Dashboards let me detect issues early, debug failures via Logs Insights, and automate remediation across Lambda, SQS, API Gateway, and DynamoDB in production."

### Container Insights, the CloudWatch Agent & Proactive Monitoring

**❗ The gap most people miss:** the default EC2 metrics CloudWatch collects from the hypervisor include CPU, network, and disk *I/O* — but **not memory usage and not filesystem free space**, because those require visibility inside the guest OS. You must install the **CloudWatch Agent** (via SSM, ideally baked into your AMI) to get memory, swap, and disk-space metrics. Being asked "how do you alarm on memory?" and answering "CloudWatch memory metric" is a classic wrong answer.

- **Container Insights** — cluster, service, task, and pod-level CPU/memory/network/disk metrics plus auto-generated dashboards for **ECS and EKS**. The container equivalent of the agent problem: without it you're blind inside the task.
- **Lambda Insights** — per-invocation memory, CPU, and init duration for functions.
- **CloudWatch Application Signals** — APM-style service-level views (latency, error rate, throughput, SLOs) built on OpenTelemetry, tying metrics to traces automatically.
- **CloudWatch Synthetics (canaries)** — scripted checks that call your endpoints on a schedule from outside, so you detect an outage **before a user reports it**. The answer to "how do you know your site is up when there's no traffic at 3 a.m.?"
- **CloudWatch RUM** — real-user monitoring from actual browsers: page load times, JS errors, Core Web Vitals. Pairs with Synthetics: RUM tells you what users experience, Synthetics tells you what a known-good request experiences.
- **CloudWatch Logs Insights** — the query language for logs; **subscription filters** stream logs onward to Lambda/Firehose/OpenSearch in near-real-time.

```bash
# Alarm on p99 latency using an extended statistic
aws cloudwatch put-metric-alarm --alarm-name api-p99-latency \
  --namespace AWS/ApplicationELB --metric-name TargetResponseTime \
  --extended-statistic p99 --period 60 --evaluation-periods 3 --threshold 1.5 \
  --comparison-operator GreaterThanThreshold --treat-missing-data notBreaching \
  --alarm-actions arn:aws:sns:us-east-1:123456789012:oncall

# Logs Insights: slowest Lambda invocations
aws logs start-query --log-group-name /aws/lambda/my-fn \
  --start-time $(date -d '1 hour ago' +%s) --end-time $(date +%s) \
  --query-string 'fields @timestamp, @duration, @requestId | filter @type="REPORT" | sort @duration desc | limit 20'

aws logs tail /aws/lambda/my-fn --follow --format short      # live tail while debugging
```
**Two alarm-design details worth stating:** set **`--treat-missing-data`** deliberately (a metric that stops being published because the service is *down* will leave an alarm in `INSUFFICIENT_DATA` forever if you don't), and alarm on **rates via Metric Math** rather than raw counts, since raw error counts are meaningless when traffic volume swings.

---

## 2. Tracing

### CloudWatch vs X-Ray: Complementary, Not Competing

The original notes mention X-Ray only briefly ("CloudWatch = Logs + Metrics; X-Ray = Tracing + Service maps") without depth — this expands it, since "CloudWatch vs X-Ray, when do you use each" is a standard senior observability question.

| | CloudWatch | X-Ray |
|---|---|---|
| Answers | "Is something wrong, and what does the aggregate look like?" | "Where exactly in this specific request's path did it go wrong/slow?" |
| Data shape | Logs (text), Metrics (time-series numbers) | Traces (request-scoped spans/segments across services) |
| Granularity | Service/function level | Individual request level, cross-service |
| .NET integration | `ILogger` → CloudWatch Logs via provider; custom metrics via SDK | `AWSXRayRecorder` middleware / `Amazon.XRay.Recorder.Handlers.AspNetCore` for ASP.NET Core; AWS SDK calls auto-instrumented via `AWSSDKHandler` |
| Typical use | Alarms, dashboards, aggregate error rate | Root-causing a specific slow/failing request across Lambda→DynamoDB→external API |

**How they combine in practice:** CloudWatch alarm fires on elevated p99 latency or error rate → you pull the trace ID from the structured log line (log the X-Ray trace ID as a correlation field) → open that trace in X-Ray to see exactly which downstream call (DynamoDB, SQS, external HTTP) added the latency or threw. Neither tool alone gives you both "something's wrong" and "here's exactly why" — production-grade .NET-on-AWS observability needs both, wired together via a shared trace/correlation ID in your structured logs.

---

## 3. Audit & Service Health

### CloudTrail

**What it is:** the **audit log of your AWS account** — a record of every API call, whether it came from the console, CLI, SDK, or another AWS service. Each event records **who** (the IAM identity, including the role session name), **what** (the API), **when**, **from which source IP**, and **whether it succeeded**.

**Three event types — the distinction is the exam question:**
| Type | Covers | Logged by default? |
|---|---|---|
| **Management events** | Control-plane operations: `RunInstances`, `CreateBucket`, `AssumeRole`, `PutBucketPolicy` | ✅ Yes, free, and visible in **Event history for 90 days** |
| **Data events** | **Data-plane** operations: S3 `GetObject`/`PutObject`/`DeleteObject`, Lambda `Invoke`, DynamoDB item-level access | **❌ No — you must enable them, and they cost extra** (high volume) |
| **Insights events** | ML-detected unusual activity — an abnormal spike in a given API call | ❌ Opt-in |

**❗ The gotcha this leads to:** "who deleted that S3 object?" is **not** answerable from default CloudTrail, because object-level deletes are **data events** and are off by default. The audit trail you need has to be enabled *before* the incident. That's a genuinely good thing to point out unprompted.

Other properties:
- **Event history** (90 days, in-console, free) vs a **Trail** — a trail delivers events to **S3** for indefinite retention, optionally also to **CloudWatch Logs** so you can build metric filters and **alarms** (e.g. alert on root-account usage, on `DeleteTrail`, or on IAM policy changes).
- **Organization trails** capture every account in the AWS Organization into one central bucket — the standard multi-account audit design, usually in a dedicated log-archive account with the bucket locked down (Object Lock + restricted policy) so even an account admin can't tamper with it.
- **Log file validation** produces digest files so you can cryptographically prove logs weren't altered or deleted.
- **Query at scale with Athena** — CloudTrail logs in S3 plus Athena is the practical way to answer forensic questions (see [Athena](10-databases-caching-analytics.md#athena)); **CloudTrail Lake** is the managed alternative.
- **❗ Not real-time** — delivery typically lags by up to ~15 minutes. For anything needing immediate reaction, use **EventBridge** rules on the event pattern instead of polling CloudTrail.

**The three-way comparison interviewers love:**
| | **CloudWatch** | **CloudTrail** | **AWS Config** |
|---|---|---|---|
| Question it answers | "**How is it performing?**" | "**Who did what, when?**" | "**What does the configuration look like, and did it drift?**" |
| Data | Metrics, logs, alarms | API call audit records | Resource configuration snapshots + change history |
| Typical use | Alerting, dashboards, debugging behaviour | Security forensics, compliance audit | Compliance rules, drift detection, "show me this SG's config last Tuesday" |

### AWS Health Dashboard

Two things with confusingly similar names:
- **Service Health Dashboard** — the **public** status page for all AWS services and regions. Generic; tells you nothing about your own resources.
- **Your Account Health Dashboard** — **personalised** events that affect **your** specific resources: an EC2 instance on degraded hardware needing retirement, scheduled RDS maintenance, an EBS volume needing action, certificate or runtime **end-of-life** notices, and region/service issues *in the regions you actually use*.

**The AWS Health API** exposes those events programmatically, and the **EventBridge integration** lets you automate responses — e.g. an instance-retirement notice triggers a Lambda that drains and replaces the instance before the forced stop. That automation angle is the senior answer.

**Interview framing:** "The public Service Health Dashboard tells me whether AWS thinks a service is degraded; the **Account** Health Dashboard and Health API tell me whether it's degraded **for my resources**, plus scheduled maintenance and retirement notices I need to act on. I'd wire the Health API into EventBridge so maintenance events open a ticket automatically rather than being noticed by someone reading email."

---

## 4. Resume Follow-Ups

### Resume Follow-Ups — Measuring the 99.9% Claim

The bullet *"…automated log maintenance and health checks, sustaining 99.9% uptime across 12 platform services"* lands squarely in this section, because *"how did you know it was 99.9%?"* is a CloudWatch question. The spoken answer and the arithmetic sit with [the Lambda bullet](01-serverless-lambda.md#resume-follow-ups--the-scheduled-lambda-jobs-999-uptime-bullet); what belongs here is the instrumentation that produces the number.

- **"Log maintenance", concretely** — **retention policies** (log groups default to **Never Expire**, a silent unbounded cost leak; setting retention is often the single biggest CloudWatch saving), **metric filters** to turn log patterns into alarmable metrics, **subscription filters** to stream logs onward, and **export to S3** with lifecycle rules into Glacier for cheap long retention.
- **Where the availability number comes from** — a success/failure count per service (custom metric via `PutMetricData` or **EMF**, or a metric filter over structured logs), then availability derived with **Metric Math** — `(total - failed) / total * 100` — rather than alarming on raw counts. Raw-count alarms break the moment traffic volume changes; a ratio does not.
- **Alarming on it** — alarm on the Metric Math expression, **composite alarms** so one root cause does not page five times, SNS to on-call, and one dashboard per service.
- **`treat-missing-data`** — set it deliberately. A metric that stops being published is the failure mode that most often hides behind a green dashboard.
- **If they push on *why* a service dipped** rather than *whether* it did, that is a tracing question, not a metrics one — see [CloudWatch vs X-Ray](#cloudwatch-vs-x-ray-complementary-not-competing).

---

← [EC2 & Instance Storage](06-ec2-instance-storage.md) · [Index](README.md) · [Load Balancing, Scalability & Auto Scaling](08-load-balancing-autoscaling.md) →
