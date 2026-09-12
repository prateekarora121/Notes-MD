> **AWS Quick Revision Notes** · [Index](README.md) · Part II

# Load Balancing, Scalability & Auto Scaling

---

## 1. Scalability Concepts

### Scalability / HA / Elasticity / Agility

- **Scalability** = capability to handle load (vertical: bigger instance, hard ceiling, downtime; horizontal: more instances, needs stateless).
- **HA** = survive failure with no downtime (≥2 AZs) — different goal from scalability.
- **Elasticity** = automatic bidirectional scaling with demand (scalability's automation).
- **Agility** = how fast you get new resources (nothing to do with load).
- **Scalable-but-not-elastic:** fixed 20-instance Black Friday fleet.
- HA (multi-AZ, min downtime) vs Fault Tolerance (zero interruption) vs DR (region loss, RTO/RPO).

---

## 2. Load Balancers

### ALB vs API Gateway vs ELB

| Type | Layer | Best for |
|---|---|---|
| ALB | 7 | host/path routing, microservices, Lambda targets |
| NLB | 4 | throughput, static IP, low latency |
| GWLB | 3 | traffic inspection appliances (GENEVE 6081) |
| CLB | 4/7 | legacy |

- ALB: Listener → Rules → Target Group (EC2/ECS/IP/Lambda), TLS/SNI, WebSocket, HTTP/2, direct Lambda.
- API Gateway: REST (feature-rich, VTL, caching, API keys) vs HTTP (cheaper, JWT/IAM, serverless default) vs WebSocket.
- **ALB vs API GW:** "ALB = smart L7 balancer between my services; API GW = full API front door (auth/throttling/quotas/versioning/transformation)." API GW has built-in throttling/quotas/caching/transformation; ALB doesn't.
- Decisions: public mobile API + auth → API GW; internal microservices path routing → ALB; real-time chat → API GW WebSocket or ALB WebSocket.

### ELB Deep-Dive

- **Cross-zone LB:** ALB **default on always**; NLB **default off** (per-TG toggle, cross-AZ transfer cost).
- **504 Gateway Timeout:** LB forwarded but no timely response → unhealthy/slow target, traffic spike, or **ALB idle timeout (60s) vs backend mismatch**.
- **Shield (DDoS):** L3/4 network; **WAF** L7 app — complementary. Shield Standard (free, auto, R53/CloudFront/ELB) vs Advanced (paid, SRT, cost protection, WAF included). Pairing: R53 + ELB + WAF.

### LB Fundamentals & Target Groups

- 4 objects: Listener → Rules → Target Group → Targets (health-checked). LB = managed multi-AZ fleet.
- **Target group** = targets + health check + behaviour. Types: instance / **ip** (Fargate/awsvpc required) / lambda / alb. Health check configured **here** (path/port/matcher `200-299`).
- **Slow start** (ramp new target — .NET/JVM cold JIT), **least_outstanding_requests** (vs round_robin for variable durations), protocol version (HTTP1/2/gRPC).
- Target states: initial → healthy/unhealthy → **draining** → unused.
- **Weighted target groups** = canary/blue-green at LB (CodeDeploy ECS).
- ❗ **ALB = no static IP, only DNS** → static IP need = NLB / NLB-fronting-ALB / Global Accelerator.
- ALB puts client IP in **`X-Forwarded-For`** → ASP.NET Core needs `ForwardedHeadersMiddleware`.
- NLB: static IP per AZ (+EIP), preserves source IP.
- Health check → point at endpoint exercising dependencies (`AddHealthChecks()` with DB/cache probes, not just Kestrel 200).

### Sticky Sessions

- ALB: duration (`AWSALB` cookie) or application (`AWSALBAPP`). NLB: source-IP/flow hash. CLB: `AWSELB`.
- Trade-offs: undermines even distribution; sessions lost on scale-in anyway.
- **Senior answer:** externalise state (ElastiCache Redis / DynamoDB), `IDistributedCache` not in-process `ISession`.

### Connection Draining / Deregistration Delay

- Same feature (CLB "connection draining" / ALB "deregistration delay"). Target enters `draining` (no new requests, in-flight finish). Default 300s, 0-3600. Tune to longest legitimate request.
- **Zero-drop deploy:** deregistration delay + ELB health checks on ASG + lifecycle hooks + instance refresh.

---

## 3. Auto Scaling

### Auto Scaling Groups

- min/desired/max; launch template (versioning, mixed instances, On-Demand/Spot).
- ❗ **Health-check gotcha:** default **EC2 only** (hypervisor status) → hung app returning 500s never replaced. Set to **ELB**.
- **Scaling policies:** Target tracking (✅ default, "keep metric at X"), Step scaling (aggressive), Simple (legacy), Scheduled (known patterns), Predictive (ML, cyclical).
- **Scale on right metric:** web → `ALBRequestCountPerTarget`; worker → **SQS backlog per instance** (not CPU — I/O-blocked worker shows low CPU).
- Cooldown/warm-up, lifecycle hooks (`Pending:Wait`/`Terminating:Wait`), termination policy, instance refresh (rolling AMI update), scale-in protection, warm pools.
```bash
aws autoscaling create-auto-scaling-group ... --health-check-type ELB --health-check-grace-period 120
```
- **Best practices:** stateless app, scale out not up, ≥2 AZs, ELB health check, demand-correlated metric, externalise state, scheduled/predictive for known events, test scale-in, cap max.

**DR — LB/ASG:** all config, ASGs self-healing. Re-apply IaC in DR; **pre-scale DR ASG** (min=0 can't absorb surge); shift traffic via Route 53 failover/weighted incrementally; confirm health-check `ELB`. ⚠️ Recreated ALB = new DNS name (front with Route 53 alias); `EC2` health-check leaves hung app running.

---

← [Observability & Monitoring](07-observability-monitoring.md) · [Index](README.md) · [Containers: Docker, ECS, ECR & Fargate](09-containers-ecs-fargate.md) →
