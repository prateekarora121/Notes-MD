> **AWS Quick Revision Notes** · [Index](README.md) · Part I

# Observability & Monitoring

---

## 1. CloudWatch — Metrics, Logs & Alarms

### CloudWatch

- **Logs:** Log Group → Streams → Events. Logs Insights (SQL-like). Structured JSON → queryable fields.
- **Metrics (per service):** Lambda (Invocations/Errors/Duration/Throttles/IteratorAge), API GW (2xx/4xx/5xx/Latency), SQS (MessagesVisible/AgeOfOldest), DynamoDB (ThrottleEvents/ConsumedCapacity), EC2 (CPU/DiskOps). Custom via `PutMetricData`.
- **Metric Math:** combine metrics in-CloudWatch → error **rate** `(m1/m2)*100` (raw counts misleading at varying traffic).
- **Alarms → Actions:** queue backlog→scale, DynamoDB throttle→auto-scale, Lambda errors→page, 5xx→alert.
- **EventBridge** = renamed CloudWatch Events; scheduled rules.
- **EMF** = structured JSON auto-extracted to metrics (high-cardinality, cheaper).
- CloudWatch (observability/how performing) vs CloudTrail (audit/who did what).

### Health Dashboard, Container Insights

- **Service Health Dashboard** (public, generic) vs **Account Health Dashboard** (your resources: retirements, maintenance, EOL). Health API + EventBridge → automate response.
- ❗ **Default EC2 metrics lack memory + disk-space** (need **CloudWatch Agent** via SSM/AMI). "Alarm on memory" ≠ default metric.
- **Container Insights** (ECS/EKS), **Lambda Insights**, **Application Signals** (OTel APM/SLOs), **Synthetics canaries** (scripted external checks — "3am up?"), **RUM** (real users), **Logs Insights** + subscription filters.
- Alarm design: set **`treat-missing-data`** deliberately (down service stops publishing → `INSUFFICIENT_DATA` forever); alarm on **rates** (Metric Math) not raw counts.

---

## 2. Tracing

### CloudWatch vs X-Ray

- CloudWatch = "is something wrong + aggregate" (logs/metrics). X-Ray = "where exactly in this request" (traces/spans cross-service).
- .NET: `AWSXRayRecorder` middleware, `AWSSDKHandler` auto-instruments.
- Combine: alarm fires → pull trace ID from structured log → open X-Ray → see which downstream call.

---

## 3. Audit & Service Health

### CloudTrail

- Account audit log: who/what/when/source IP/success.
- **Events:** Management (control-plane, default free, 90d history), **Data** (S3 GetObject/PutObject, Lambda Invoke — ❗ **off by default, cost extra**), Insights (ML anomalies).
- ❗ "Who deleted S3 object?" not answerable by default (data events off) — enable **before** incident.
- Trail → S3 (indefinite) + CloudWatch Logs (alarms). Org trails → central locked log-archive account. Log file validation. Query at scale via **Athena**; CloudTrail Lake. ❗ Not real-time (~15min) — use EventBridge for immediate.

| | CloudWatch | CloudTrail | Config |
|---|---|---|---|
| Answers | How performing? | Who did what? | Config + drift? |

---

## 4. Resume Follow-Ups

### 99.9% Measurement

- Log maintenance = retention (default Never Expire = cost leak), metric/subscription filters, S3 export→Glacier.
- Availability = per-service success/fail count → Metric Math `(total-failed)/total*100`; composite alarms; `treat-missing-data`; per-service dashboard. Why it dipped = X-Ray.

**DR — Observability:** risk = log groups/dashboards/alarms + 15mo metric history (losing observability during incident = outage). Backup = dashboards/alarms in **IaC**, log retention + S3 export (metrics can't snapshot). Re-apply IaC; logs already in S3→Athena; recreate subscription filters (per-log-group, vanish on recreate); verify alarms `OK` not `INSUFFICIENT_DATA`. ⚠️ CloudWatch regional — cross-region tooling for regional-outage diagnosis; keep observability in separate stack from workload.

---

← [EC2 & Instance Storage](06-ec2-instance-storage.md) · [Index](README.md) · [Load Balancing, Scalability & Auto Scaling](08-load-balancing-autoscaling.md) →
