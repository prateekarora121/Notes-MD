> **AWS Detailed Guide** · [Index](README.md) · Part I

# Observability & Monitoring

---

## 1. CloudWatch — Metrics, Logs & Alarms

### CloudWatch Deep Dive

**Yeh kya hai:** AWS ka core observability platform — Metrics, Logs, Alarms, Dashboards, Events (EventBridge), aur Traces (X-Ray, technically ek separate service) ke saath integration.

**Logs**
- Hierarchy: `Log Group → Log Streams → Log Events` (jaise, `/aws/lambda/ProcessOrder` har container instance ke liye ek stream ke saath).
- **CloudWatch Logs Insights**: logs ke upar SQL-like query language.
```
fields @timestamp, @message
| filter @message like /ERROR/
| sort @timestamp desc
```
```
fields @timestamp, @duration
| filter @duration > 500
```
- Structured JSON logging fields ko directly queryable banata hai — interviews mein proactively mention karne layak ek strong, low-effort win.

**Metrics (per service key ones jaanna zaruri hai)**
| Service | Key metrics |
|---|---|
| Lambda | Invocations, Errors, Duration, Throttles, ConcurrentExecutions, IteratorAge (stream sources) |
| API Gateway | 2xx/4xx/5xx, Latency (p50/p90/p99), IntegrationLatency |
| SQS | ApproximateNumberOfMessagesVisible, ApproximateAgeOfOldestMessage |
| DynamoDB | ReadThrottleEvents, WriteThrottleEvents, ConsumedCapacity |
| EC2 | CPUUtilization, DiskReadOps/DiskWriteOps |

Aap custom business metrics bhi publish kar sakte ho (`PutMetricData`) — orders/minute, payment failure rate, pipeline throughput.

**Metric Math:** aapko multiple existing metrics ko ek derived formula mein combine karne deta hai *CloudWatch ke andar hi*, bina ek separate custom metric publish kiye ya client-side compute kiye. Canonical example ek error-rate percentage hai: `(m1 / m2) * 100` jahan `m1` = `5xxRate` (ya `Errors`) aur `m2` = `TotalRequests` (ya `Invocations`) — yeh aapko ek single alarm-able, graphable series ("5xx error rate %") deta hai do separate raw-count graphs ko eyeball karke apne dimaag mein division karne ke bajaye. "Raw error *count* ke bajaye error *rate* par kaise alarm karoge" ke answer ke roop mein naam lene layak — raw counts varying traffic volumes par misleading hote hain, aur Metric Math extra instrumentation code ke bina isse normalize karne ka built-in tarika hai.

**Alarms → Action patterns**
| Alarm | Metric | Action |
|---|---|---|
| Queue backlog | `ApproximateNumberOfMessagesVisible > 1000` | SNS alert / scale consumer |
| DynamoDB throttling | `WriteThrottleEvents > 0` | Auto-scale provisioned capacity |
| Lambda failures | `Errors > 5%` | PagerDuty/email |
| API 5xx spike | `5xxErrorRate > 2%` | Alert dev team |

**EventBridge (pehle CloudWatch Events):** AWS services, aapki apps, aur SaaS sources se events ko Lambda/SQS/SNS/Step Functions/ECS tak route karta hai — scheduled rules shamil hain (`cron(0 1 * * ? *)`).

**CloudWatch vs CloudTrail (ek classic trick question):** CloudWatch = observability (*behavior/performance* ke baare mein logs/metrics/alarms); CloudTrail = governance/audit (*kisne kaunsa API call kiya, kab* ka record). Yeh different questions ka answer dete hain aur interchangeable nahi hain.

**Embedded Metric Format (EMF):** structured JSON log format jise CloudWatch automatically metrics mein extract kar leta hai — Lambda se high-cardinality custom metrics ke liye useful hai bina extra `PutMetricData` calls (aur unki associated API cost/throttling) ke.

**Interview-ready closing summary:** "CloudWatch AWS ki core observability service hai — Logs, Metrics, Alarms, EventBridge, aur Dashboards mujhe issues early detect karne dete hain, Logs Insights ke through failures debug karne dete hain, aur Lambda, SQS, API Gateway, aur DynamoDB ke across production mein remediation automate karne dete hain."

### Container Insights, the CloudWatch Agent & Proactive Monitoring

**❗ Woh gap jo sabse zyada log miss karte hain:** default EC2 metrics jo CloudWatch hypervisor se collect karta hai unmein CPU, network, aur disk *I/O* shamil hain — lekin **memory usage aur filesystem free space nahi**, kyunki unhe guest OS ke andar visibility chahiye hoti hai. Memory, swap, aur disk-space metrics paane ke liye aapko **CloudWatch Agent** install karna padta hai (SSM ke through, ideally AMI mein baked). "Memory par kaise alarm karte ho?" poochhe jaane par "CloudWatch memory metric" bolna ek classic wrong answer hai.

- **Container Insights** — **ECS aur EKS** ke liye cluster, service, task, aur pod-level CPU/memory/network/disk metrics plus auto-generated dashboards. Agent problem ka container equivalent — iske bina aap task ke andar blind ho.
- **Lambda Insights** — functions ke liye per-invocation memory, CPU, aur init duration.
- **CloudWatch Application Signals** — OpenTelemetry par built APM-style service-level views (latency, error rate, throughput, SLOs), metrics ko traces se automatically tie karke.
- **CloudWatch Synthetics (canaries)** — scripted checks jo ek schedule par bahar se aapke endpoints call karte hain, taaki aap outage detect kar sako **user report karne se pehle**. "Bina traffic ke raat 3 baje kaise pata chale site up hai?" ka answer.
- **CloudWatch RUM** — real browsers se real-user monitoring: page load times, JS errors, Core Web Vitals. Synthetics ke saath pair hota hai: RUM batata hai users kya experience karte hain, Synthetics batata hai ek known-good request kya experience karta hai.
- **CloudWatch Logs Insights** — logs ke liye query language; **subscription filters** logs ko near-real-time mein Lambda/Firehose/OpenSearch tak stream karte hain.

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
**Do alarm-design details state karne layak:** **`--treat-missing-data`** deliberately set karo (ek metric jo publish hona band ho jaaye kyunki service *down* hai, agar aap yeh na set karo to alarm ko forever `INSUFFICIENT_DATA` mein chhod degi), aur raw counts ke bajaye Metric Math ke through **rates** par alarm karo, kyunki traffic volume swings ke saath raw error counts meaningless ho jaate hain.

---

## 2. Tracing

### CloudWatch vs X-Ray: Complementary, Not Competing

Original notes X-Ray ko sirf briefly mention karte hain ("CloudWatch = Logs + Metrics; X-Ray = Tracing + Service maps") bina depth ke — yeh isse expand karta hai, kyunki "CloudWatch vs X-Ray, kab kaunsa use karte ho" ek standard senior observability question hai.

| | CloudWatch | X-Ray |
|---|---|---|
| Answers | "Is something wrong, and what does the aggregate look like?" | "Where exactly in this specific request's path did it go wrong/slow?" |
| Data shape | Logs (text), Metrics (time-series numbers) | Traces (request-scoped spans/segments across services) |
| Granularity | Service/function level | Individual request level, cross-service |
| .NET integration | `ILogger` → CloudWatch Logs via provider; custom metrics via SDK | `AWSXRayRecorder` middleware / `Amazon.XRay.Recorder.Handlers.AspNetCore` for ASP.NET Core; AWS SDK calls auto-instrumented via `AWSSDKHandler` |
| Typical use | Alarms, dashboards, aggregate error rate | Root-causing a specific slow/failing request across Lambda→DynamoDB→external API |

**Yeh practice mein kaise combine hote hain:** CloudWatch alarm elevated p99 latency ya error rate par fire hota hai → aap structured log line se trace ID pull karte ho (X-Ray trace ID ko correlation field ke roop mein log karo) → us trace ko X-Ray mein khol ke exactly dekho kaunse downstream call (DynamoDB, SQS, external HTTP) ne latency add ki ya throw kiya. Koi bhi ek tool akela aapko "kuch galat hai" aur "yahi exact reason hai" dono nahi deta — production-grade .NET-on-AWS observability ko dono chahiye, aapke structured logs mein ek shared trace/correlation ID ke through wired together.

---

## 3. Audit & Service Health

### CloudTrail

**Yeh kya hai:** aapke AWS account ka **audit log** — har API call ka record, chahe woh console, CLI, SDK, ya doosri AWS service se aaya ho. Har event record karta hai **kaun** (IAM identity, role session name shamil), **kya** (API), **kab**, **kaunsi source IP se**, aur **success hua ya nahi**.

**Teen event types — yeh distinction hi exam question hai:**
| Type | Covers | Logged by default? |
|---|---|---|
| **Management events** | Control-plane operations: `RunInstances`, `CreateBucket`, `AssumeRole`, `PutBucketPolicy` | ✅ Yes, free, and visible in **Event history for 90 days** |
| **Data events** | **Data-plane** operations: S3 `GetObject`/`PutObject`/`DeleteObject`, Lambda `Invoke`, DynamoDB item-level access | **❌ No — you must enable them, and they cost extra** (high volume) |
| **Insights events** | ML-detected unusual activity — an abnormal spike in a given API call | ❌ Opt-in |

**❗ Yeh jo gotcha leads karta hai:** "us S3 object ko kisne delete kiya?" default CloudTrail se **answerable nahi** hai, kyunki object-level deletes **data events** hain aur default se off hain. Jo audit trail chahiye woh incident se *pehle* enable hona padta hai. Yeh unprompted point out karne layak genuinely achhi baat hai.

Doosri properties:
- **Event history** (90 days, in-console, free) vs ek **Trail** — ek trail events ko **S3** mein deliver karta hai indefinite retention ke liye, optionally **CloudWatch Logs** mein bhi taaki aap metric filters aur **alarms** build kar sako (jaise root-account usage par, `DeleteTrail` par, ya IAM policy changes par alert).
- **Organization trails** AWS Organization ke har account ko ek central bucket mein capture karti hain — standard multi-account audit design, usually ek dedicated log-archive account mein bucket locked down (Object Lock + restricted policy) ke saath taaki ek account admin bhi tamper na kar sake.
- **Log file validation** digest files produce karta hai taaki aap cryptographically prove kar sako ki logs altered ya deleted nahi hue.
- **Athena se scale par query karo** — S3 mein CloudTrail logs plus Athena forensic questions ka answer dene ka practical tarika hai (dekho [Athena](10-databases-caching-analytics.md#athena)); **CloudTrail Lake** managed alternative hai.
- **❗ Real-time nahi hai** — delivery typically ~15 minutes tak lag karti hai. Immediate reaction chahiye to CloudTrail poll karne ke bajaye **EventBridge** rules event pattern par use karo.

**Wo three-way comparison jo interviewers ko pasand hai:**
| | **CloudWatch** | **CloudTrail** | **AWS Config** |
|---|---|---|---|
| Question it answers | "**How is it performing?**" | "**Who did what, when?**" | "**What does the configuration look like, and did it drift?**" |
| Data | Metrics, logs, alarms | API call audit records | Resource configuration snapshots + change history |
| Typical use | Alerting, dashboards, debugging behaviour | Security forensics, compliance audit | Compliance rules, drift detection, "show me this SG's config last Tuesday" |

### AWS Health Dashboard

Do cheezein confusingly similar names ke saath:
- **Service Health Dashboard** — saare AWS services aur regions ke liye **public** status page. Generic; aapke apne resources ke baare mein kuch nahi batata.
- **Aapka Account Health Dashboard** — **aapke** specific resources ko affect karne wale **personalised** events: ek EC2 instance degraded hardware par jise retirement chahiye, scheduled RDS maintenance, ek EBS volume jise action chahiye, certificate ya runtime **end-of-life** notices, aur region/service issues *un regions mein jo aap actually use karte ho*.

**AWS Health API** un events ko programmatically expose karta hai, aur **EventBridge integration** aapko responses automate karne deta hai — jaise ek instance-retirement notice ek Lambda trigger karta hai jo forced stop se pehle instance ko drain aur replace kar deta hai. Yehi automation angle senior answer hai.

**Interview framing:** "Public Service Health Dashboard mujhe batata hai ki AWS ko lagta hai koi service degraded hai ya nahi; **Account** Health Dashboard aur Health API mujhe batate hain ki yeh **mere** resources ke liye degraded hai ya nahi, plus scheduled maintenance aur retirement notices jinpar mujhe act karna hai. Main Health API ko EventBridge mein wire karunga taaki maintenance events automatically ek ticket khole, na ki koi email padhte hue notice kare."

---

## 4. Resume Follow-Ups

### Resume Follow-Ups — 99.9% Claim Ko Measure Karna

Bullet *"…automated log maintenance and health checks, sustaining 99.9% uptime across 12 platform services"* seedha isi section mein aata hai, kyunki *"aapko kaise pata chala ki yeh 99.9% hai?"* ek CloudWatch question hai. Bola jaane wala answer aur arithmetic [Lambda bullet](01-serverless-lambda.md#resume-follow-ups--scheduled-lambda-jobs-999-uptime-bullet) ke saath hai; yahan woh instrumentation belong karti hai jo number produce karti hai.

- **"Log maintenance", concretely** — **retention policies** (log groups default mein **Never Expire** hote hain, ek silent unbounded cost leak; retention set karna kaafi baar sabse badi CloudWatch saving hoti hai), log patterns ko alarmable metrics mein badalne ke liye **metric filters**, logs ko aage stream karne ke liye **subscription filters**, aur cheap long retention ke liye lifecycle rules ke saath **export to S3** → Glacier.
- **Availability number kahan se aata hai** — per-service success/failure count (custom metric via `PutMetricData` ya **EMF**, ya structured logs par metric filter), phir **Metric Math** se availability derive karo — `(total - failed) / total * 100` — raw counts par alarm karne ke bajaye. Raw-count alarms traffic volume badalte hi toot jaate hain; ratio nahi tootta.
- **Ispar alarm** — Metric Math expression par alarm, noise cut karne ke liye **composite alarms** (ek root cause paanch baar page na kare), on-call ke liye SNS, aur per-service ek dashboard.
- **`treat-missing-data`** — ise deliberately set karo. Ek metric jo publish hona band ho jaaye, wahi failure mode hai jo sabse zyada green dashboard ke peeche chhupta hai.
- **Agar woh *kyun* dip hua** par push karein (*kya* dip hua ke bajaye), toh woh tracing question hai, metrics nahi — dekho [CloudWatch vs X-Ray](#cloudwatch-vs-x-ray-complementary-not-competing).

---

← [EC2 & Instance Storage](06-ec2-instance-storage.md) · [Index](README.md) · [Load Balancing, Scalability & Auto Scaling](08-load-balancing-autoscaling.md) →
