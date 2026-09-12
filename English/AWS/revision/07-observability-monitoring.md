> **AWS Quick Revision Notes** · [Index](README.md) · Part I

# Observability & Monitoring

---

## 1. CloudWatch — Metrics, Logs & Alarms

### CloudWatch Deep Dive

Metrics, Logs, Alarms, Dashboards, EventBridge, + X-Ray (separate).
**Logs:** `Log Group → Streams → Events`. **Logs Insights** (SQL-like):
```
fields @timestamp, @message | filter @message like /ERROR/ | sort @timestamp desc
```
Structured JSON logging → queryable fields.
**Metrics per service:** Lambda (Invocations/Errors/Duration/Throttles/ConcurrentExecutions/IteratorAge), API GW (2xx/4xx/5xx, Latency p50/90/99, IntegrationLatency), SQS (ApproximateNumberOfMessagesVisible, AgeOfOldestMessage), DynamoDB (Read/WriteThrottleEvents, ConsumedCapacity), EC2 (CPUUtilization, DiskOps). Custom via `PutMetricData`.
**Metric Math:** combine metrics into a derived series in CloudWatch, e.g. error-rate `(m1/m2)*100` — alarm on a **rate not raw count** (raw counts mislead at varying volume).
**Alarm→action:** queue backlog→scale; DynamoDB throttle→autoscale; Lambda Errors>5%→page; API 5xx>2%→alert.
**EventBridge** = routes events (incl. scheduled `cron`). **CloudWatch vs CloudTrail:** observability (behaviour/perf) vs governance (who called which API). **EMF** = structured JSON log auto-extracted to metrics (high-cardinality without PutMetricData cost).

### Container Insights / Agent / Proactive

**❗ Default EC2 metrics exclude memory + disk free** (need CloudWatch Agent inside guest). **Container Insights** (ECS/EKS cluster/task/pod), **Lambda Insights** (per-invocation memory/init), **Application Signals** (APM/SLOs on OTel), **Synthetics canaries** (scheduled external checks — "is it up at 3 a.m.?"), **RUM** (real-user browser), **Logs Insights** + subscription filters.
```bash
aws cloudwatch put-metric-alarm --alarm-name api-p99-latency --namespace AWS/ApplicationELB \
  --metric-name TargetResponseTime --extended-statistic p99 --period 60 --evaluation-periods 3 \
  --threshold 1.5 --comparison-operator GreaterThanThreshold --treat-missing-data notBreaching \
  --alarm-actions arn:aws:sns:...:oncall
aws logs tail /aws/lambda/my-fn --follow --format short
```
**Set `--treat-missing-data` deliberately** (a down service stops publishing → alarm stuck INSUFFICIENT_DATA); alarm on **rates via Metric Math** not raw counts.

**Measuring 99.9%:** log maintenance = retention (default Never Expire = leak), metric/subscription filters, S3 export→Glacier. Number = success/failure count (custom metric/EMF/metric filter) → availability via Metric Math `(total-failed)/total*100`. Composite alarms + SNS + per-service dashboard. `treat-missing-data`. Why a dip happened = X-Ray (tracing) not metrics.

**Observability DR:** at risk = log groups, dashboards, alarms, 15mo metric history (losing observability *during* incident is its own outage). Backup = IaC for dashboards/alarms; export/subscription-filter to S3; metrics can't snapshot. Recovery: re-apply from IaC; query S3 logs with Athena; re-create subscription filters (silently lost on log-group recreate); verify alarms re-armed to `OK` not `INSUFFICIENT_DATA`. **❗ CloudWatch is regional — a regional outage blinds you when you need sight** (ship cross-region). Keep observability in a separate stack from the workload.

---

## 2. Tracing

### CloudWatch vs X-Ray

CloudWatch = "is something wrong / aggregate?" (logs+metrics, service level). X-Ray = "where in this request?" (traces/spans, request level, cross-service). Combine: alarm fires → pull trace ID from structured log → open in X-Ray → see which downstream call. .NET: `ILogger`→CloudWatch; `AWSXRayRecorder`/`AWSSDKHandler` auto-instrument.

---

## 3. Audit & Service Health

### CloudTrail

Audit log of every API call: who (identity + session), what, when, source IP, success.
| Type | Covers | Default? |
|---|---|---|
| Management | Control-plane (RunInstances, AssumeRole) | ✅ free, 90-day history |
| **Data** | Data-plane (S3 GetObject/Put/Delete, Lambda Invoke, DynamoDB item) | **❌ enable + costs extra** |
| Insights | ML anomaly | ❌ opt-in |

**❗ "Who deleted that S3 object?" not answerable from default CloudTrail** (data events off, enable before incident). Event history (90d free) vs **Trail** (→ S3 indefinite, + CloudWatch Logs for alarms). Org trails → central locked log-archive account. Log file validation (tamper proof). Query with **Athena**; CloudTrail Lake. **❗ Not real-time (~15 min lag)** — for immediate reaction use EventBridge patterns. **3-way:** CloudWatch (performing?) / CloudTrail (who did what?) / Config (config + drift?).

### AWS Health Dashboard

**Service Health** = public status page (generic). **Account Health** = personalised events on your resources (instance retirement, RDS maintenance, EOL, region issues). **Health API + EventBridge** = automate (retirement notice → Lambda drains+replaces). Answer both.

---

← [EC2 & Instance Storage](06-ec2-instance-storage.md) · [Index](README.md) · [Load Balancing, Scalability & Auto Scaling](08-load-balancing-autoscaling.md) →
