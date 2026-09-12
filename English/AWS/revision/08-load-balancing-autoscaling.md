> **AWS Quick Revision Notes** · [Index](README.md) · Part II

# Load Balancing, Scalability & Auto Scaling

---

## 1. Scalability Concepts

### Scalability / HA / Elasticity / Agility

- **Scalability** = *can* handle more. **Vertical** (bigger instance, ceiling, downtime — for RDS primary/single cache/monolith); **horizontal** (more instances behind LB, unlimited, needs **stateless** — default for web/API).
- **HA** = survive failure = ≥2 AZs (different goal from scalability).
- **Elasticity** = auto-scales both directions to demand.
- **Agility** = speed to get anything new (nothing to do with load).
- Scalable-not-elastic example: 20 fixed instances sized for Black Friday (handles peak, pays for 20 in Feb). HA vs fault tolerance (zero interruption) vs DR (lose a region, RTO/RPO).

---

## 2. Load Balancers

### ALB vs API Gateway vs ELB

| Type | Layer | Protocol | Best for |
|---|---|---|---|
| ALB | 7 | HTTP/WS | Host/path routing, microservices, Lambda targets |
| NLB | 4 | TCP/UDP/TLS | Extreme throughput, static IP, low latency |
| GWLB | 3 | IP | Traffic inspection appliances (GENEVE 6081) |
| CLB | 4/7 | Basic | Legacy |

ALB: Listener→Rules (host/path/header)→Target Group (EC2/ECS/IP/Lambda), SNI, WS, HTTP/2. API GW: REST (feature-rich, keys/usage plans/VTL/cache) vs HTTP (cheaper, JWT/IAM) vs WebSocket. **Mental model:** ALB = smart L7 LB between services; API GW = full API front door (auth/throttle/quota/versioning/transform). Public mobile REST + Lambda/DynamoDB → API GW; internal ECS path routing → ALB.

### ELB Deep-Dive

**Cross-Zone LB:** every node spreads across all AZs' targets. **ALB on by default (can't disable); NLB off by default** (enabling can add cross-AZ transfer cost). **504** = LB forwarded but no timely response (unhealthy/slow target, spike, or **ALB idle-timeout (60s) < backend response** → raise idle timeout). **Shield** (L3/4) + **WAF** (L7) complementary: Shield Standard free/auto on Route53/CloudFront/GA/ELB; Advanced paid (larger mitigation, DRT, cost protection, WAF included). Layered edge = Route53 + ELB + WAF.

### Load Balancing Fundamentals

Listener→Rules→Target Group→Targets (health-checked). **Target group** does the work: types `instance`/`ip` (**required for Fargate/awsvpc**)/`lambda`/`alb`; health check here (path/port/interval/matcher default 200 → often 200-299); attributes **slow start** (ramp new target — useful for .NET/JVM cold JIT), **least_outstanding_requests** vs round_robin, protocol version (HTTP1/2/gRPC); states initial→healthy/unhealthy→draining→unused; **weighted target groups** = canary at the LB. **ALB** = L7 content routing, SNI, gRPC/WS, Lambda, Cognito/OIDC auth; original client IP in **`X-Forwarded-For`** (enable `ForwardedHeadersMiddleware` in ASP.NET Core). **NLB** = L4, millions RPS, **static IP per AZ + EIP**, **preserves client source IP** no header. **❗ ALB has no static IP (only DNS)** → NLB / NLB-fronting-ALB / Global Accelerator. **GWLB** = L3 inspection. **CLB** = legacy ("connection draining" vs ALB/NLB "deregistration delay"). Point health check at real deps (`AddHealthChecks()` with DB/cache), not just Kestrel 200.

### Sticky Sessions

ALB: duration-based (`AWSALB` cookie 1s–7d) or app-based (`AWSALBAPP`). NLB: source-IP/flow hash. Trade-offs: undermines even distribution; sessions lost anyway on scale-in. **Real fix = externalise session** (ElastiCache Redis / DynamoDB; `IDistributedCache`). Sticky legit for legacy/expensive per-user context.

### Connection Draining / Deregistration Delay

Removed target → `draining`: no new requests, in-flight finish up to delay. Default **300s**, 0–3600. Tune to longest legitimate request. Deploy without drops = deregistration delay + ELB health checks on ASG + lifecycle hooks + instance refresh.

---

## 3. Auto Scaling

### Auto Scaling Groups

Maintains target count, replaces failures, spans AZs (HA), registers with LB. Min/desired/max (max = cost ceiling + blast radius). **Launch template** (versioned, mixed types, On-Demand/Spot) replaces launch configuration.
**❗ Health-check type defaults to EC2** → only replaces hypervisor failures; a hung app (500s) looks healthy and is **never replaced** → set type to **ELB**. Genuine production incident + common question.
Policies: **target tracking** (default — keep metric at value) · step (severity steps) · simple (legacy) · scheduled (known events) · **predictive** (ML ahead of demand).
**Scale metric (differentiator):** `ALBRequestCountPerTarget` (web) or **SQS backlog per instance** (worker — CPU misleads, blocked I/O = low CPU + growing backlog) over CPU. Plus cooldown/warm-up, lifecycle hooks (Pending:Wait / Terminating:Wait), termination policy, instance refresh, scale-in protection, warm pools.
```bash
aws autoscaling create-auto-scaling-group --auto-scaling-group-name web-asg \
  --launch-template LaunchTemplateName=web-lt,Version='$Latest' \
  --min-size 2 --max-size 10 --desired-capacity 2 --vpc-zone-identifier "subnet-a,subnet-b,subnet-c" \
  --target-group-arns arn:... --health-check-type ELB --health-check-grace-period 120
```

---

## 4. Best Practices & Shared Responsibility

### Best Practices + Shared Responsibility + DR

Stateless app tier · scale out · ≥2 AZs · ELB health-check type + real `/health` · demand-correlated metric · externalise session · scheduled/predictive for known events · **test scale-in** (dropped requests/lost work surface on the way down) · cap max.
AWS = ELB fleet + ASG control plane + health infra. You = LB type, min/desired/max + metric, real health endpoint, spanning AZs, cert lifecycle, tuning lifecycle/deregistration, **stateless design**.
**DR:** all config no data → IaC + self-healing. **Pre-scale DR ASG before shifting traffic** (min=0 → thundering herd). Route 53 failover/weighted incrementally. **❗ Recreated ALB gets new DNS name** → front with Route 53 alias. Classic finding: ASG health-check type on `EC2` → hung app never replaced.

---

← [Observability & Monitoring](07-observability-monitoring.md) · [Index](README.md) · [Containers: Docker, ECS, ECR & Fargate](09-containers-ecs-fargate.md) →
