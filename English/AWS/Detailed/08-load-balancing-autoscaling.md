> **AWS Detailed Guide** · [Index](README.md) · Part II

# Load Balancing, Scalability & Auto Scaling

---

## 1. Scalability Concepts

### Scalability, High Availability, Elasticity & Agility

Four words interviewers deliberately blur together. Define them cleanly and you've answered half the section.

**Scalability** = the system *can* handle more load.
- **Vertical scaling (scale up)** — a bigger instance. `t3.micro` → `m5.4xlarge`. Simple, no app changes, but there's a hard ceiling (the largest instance type) and it usually needs downtime. This is how you scale things that can't be distributed: RDS primaries, a single large cache node, legacy monoliths.
- **Horizontal scaling (scale out)** — more instances behind a load balancer. Effectively unlimited, no downtime, better fault tolerance. Requires the app to be **stateless**. This is the default for web/API tiers.

**High Availability** = surviving a failure without downtime, achieved by running in **at least two Availability Zones**. Note this is a *different goal* from scalability: you can be highly scalable in one AZ (and lose everything when that AZ fails) or highly available with two small instances that can't handle load. Interviewers probe exactly this confusion.

**Elasticity** = scaling **automatically, in both directions, matched to actual demand** — so you pay only for what you need right now. Scalability is the capability; elasticity is the automation of it.

**Agility** = how fast you can get new resources at all — minutes instead of a months-long hardware procurement cycle. It has **nothing to do with load**; it's about speed of change. This is the one people get wrong.

| Term | Question it answers | Example |
|---|---|---|
| Scalability | *Can* it grow? | An ASG whose max is 20 instances |
| Elasticity | Does it grow and shrink **by itself**? | That ASG on a target-tracking policy, scaling 2→20→2 across the day |
| High Availability | Does it survive a failure? | That ASG spread across 3 AZs behind an ALB |
| Agility | How fast can I get *anything* new? | Spinning up a whole test environment in 10 minutes via Terraform |

**A scalable-but-not-elastic example to have ready:** a fixed fleet of 20 EC2 instances sized for Black Friday. It scales (it handles the peak) but it isn't elastic (you pay for 20 instances in February). That single example demonstrates you understand the difference rather than reciting definitions.

**HA vs Fault Tolerance vs Disaster Recovery:** HA = minimal downtime within a region (multi-AZ); fault tolerance = *zero* interruption from a component failure (redundancy with no impact); DR = recovering from losing an entire region, measured by **RTO/RPO** — see [Disaster Recovery Strategies](18-well-architected-resilience.md#disaster-recovery-strategies).

---

## 2. Load Balancers

### ALB vs API Gateway vs ELB (NLB/GWLB/CLB)

**ELB family**
| Type | Layer | Protocols | Best for |
|---|---|---|---|
| ALB (Application LB) | 7 | HTTP/HTTPS/WebSocket | Host/path routing, microservices, Lambda targets |
| NLB (Network LB) | 4 | TCP/UDP/TLS | Extreme throughput, static IP, low latency |
| GWLB (Gateway LB) | 3 | IP | Transparent traffic inspection (firewalls/IDS appliances) |
| CLB (Classic, deprecated) | 4/7 | Basic | Legacy only |

**ALB core concepts:** Listener (port/protocol) → Rules (host/path/header conditions) → Target Group (EC2, ECS, IP, **Lambda**) with health checks. Supports TLS termination with SNI (multiple certs on one listener), WebSocket, HTTP/2, and direct Lambda invocation as a target (Lambda returns an HTTP-shaped response, similar to API Gateway proxy integration).

**API Gateway core concepts:** REST API (feature-rich, more expensive — API keys, usage plans, request/response VTL mapping templates, caching) vs HTTP API (cheaper, lower latency, JWT/IAM auth, good default for Lambda-backed serverless) vs WebSocket API (connection-managed real-time).

**ALB vs API Gateway — the mental model that wins interviews**
> "ALB is a smart Layer-7 load balancer: I have services, route and balance traffic between them. API Gateway is a full API front door: I'm exposing an API to external clients and need auth, throttling, quotas, versioning, transformation, and monitoring built in."

| Aspect | ALB | API Gateway |
|---|---|---|
| Primary purpose | Load balancing between backend services | Publishing/managing APIs for consumers |
| Targets | EC2, ECS/EKS, IP, Lambda | Lambda, HTTP endpoints, AWS service integrations |
| Built-in throttling/quotas | No (app-level only) | Yes (usage plans, rate limits) |
| Request transformation | Limited (routing only) | Advanced (VTL mapping templates) |
| Caching | No | Yes (REST API only) |
| Pricing | Per-hour + LCU | Per-million-requests (+ cache if enabled) |
| Typical client | Internal/browser | Mobile/web/3rd-party API consumers |

**Decision examples**
- Public REST API for a mobile app needing auth/throttling/API keys, Lambda+DynamoDB backend → **API Gateway**.
- Internal microservices on ECS needing only path/host routing + TLS termination → **ALB**.
- Real-time chat: fully serverless → **API Gateway WebSocket API**; already on containers → **ALB WebSocket**.

### ELB Deep-Dive: Cross-Zone Load Balancing, 504 Timeouts & Shield DDoS Protection

**Cross-Zone Load Balancing:** when enabled, every load balancer node distributes traffic evenly across *all* registered targets in *all* enabled AZs, not just the targets in its own AZ — this evens out load when targets are unevenly distributed across AZs (e.g., 8 targets in AZ-A, 2 in AZ-B). ALB has cross-zone load balancing **on by default, always** (cannot be disabled); NLB has it **off by default** and it's a per-target-group toggle — enabling it on NLB can introduce cross-AZ data transfer charges, which is the usual reason teams leave it off for latency/cost-sensitive NLB use cases. Knowing that ALB and NLB default differently here is a good "gotcha" fact to have ready.

**HTTP 504 Gateway Timeout (ELB):** means the load balancer forwarded the request to a target but never got a timely response back. Two common root causes: an **unhealthy/slow target** (app hung, DB call blocking, thread pool exhausted) or a **traffic spike** overwhelming backend capacity before auto-scaling catches up. A third cause worth naming from standard ELB behavior: a mismatch between the **ALB idle timeout** (default 60s) and the backend's own keep-alive/response time — if your app can legitimately take longer than the ALB's configured idle timeout, you'll see 504s that have nothing to do with target health, and the fix is raising the ALB idle timeout (and your app's keep-alive) rather than chasing a phantom backend bug.

**AWS Shield — tying DDoS protection to Route 53/ELB alongside WAF:** Shield defends at the network/transport layer (L3/L4 volumetric and protocol attacks), while WAF defends at the application layer (L7 — malicious request patterns, rate limiting, bad bots) — they're complementary, not competing, and a senior answer names both together rather than picking one.
- **Shield Standard:** free, automatically enabled for **every** AWS account on Route 53, CloudFront, and ELB (ALB/NLB/CLB) — no opt-in needed, covers common, automated L3/L4 DDoS attack patterns.
- **Shield Advanced:** paid, opt-in tier adding larger-attack mitigation capacity, real-time attack visibility/metrics, integration with WAF for automatic rule creation during an attack, cost protection (credits for scaling charges incurred during an attack), and 24/7 access to the AWS DDoS Response Team (DRT).
- **Practical pairing:** Route 53 (DNS-layer resilience + Shield Standard baseline) + ELB (Shield Standard baseline, upgrade to Advanced for critical public endpoints) + WAF (L7 rate limiting/malicious pattern blocking) is the standard "defend the public edge" stack — worth stating as a layered answer rather than naming just one service.

### Load Balancing Fundamentals

**What a load balancer buys you:** spread traffic over many targets, one stable DNS name in front of disposable instances, automatic removal of unhealthy targets, TLS termination in one place, and cross-AZ high availability. The ELB itself is a managed, auto-scaling, multi-AZ fleet — you never patch or size it.

**The four objects, in order:** **Listener** (port + protocol) → **Rules** (conditions: host, path, header, query string, source IP, HTTP method) → **Target Group** (EC2, IP, Lambda, or another ALB) → **Targets**, each continuously **health-checked**.

**Target groups in short** — the object that does most of the actual work, and the one you tune:
- **What it is:** a named group of targets **plus their health check and traffic behaviour**. It exists independently of the load balancer — one target group can be used by several load balancers, and one load balancer can route to many target groups.
- **Target types:** `instance` (register by instance ID) · **`ip`** (any IP in the VPC, or on-prem via Direct Connect/VPN — **and the type required for Fargate and `awsvpc` ECS tasks**, since those have their own ENIs) · `lambda` (ALB only) · `alb` (an NLB fronting an ALB, to combine a static IP with Layer-7 routing).
- **Health check** is configured here, not on the load balancer: protocol, path, **port** (`traffic-port` or an override — e.g. app on 8080, health endpoint on 8081), interval, timeout, healthy/unhealthy thresholds, and **success codes** (the *matcher*, default `200`, often widened to `200-299`).
- **Three attributes worth knowing beyond [stickiness](#sticky-sessions-session-affinity) and [deregistration delay](#connection-draining--deregistration-delay):**
  - **Slow start** — ramps traffic to a *new* target over 30–900s instead of giving it a full share immediately. **Off by default, and genuinely useful for .NET/JVM**, where a fresh process has a cold JIT and empty caches: without it, the new target's p99 spikes or it fails health checks under full load the moment it joins.
  - **Load balancing algorithm** — `round_robin` (default) vs **`least_outstanding_requests`**. Round robin will happily hand a request to a target already stuck on a slow one; LOR is the better choice whenever request durations vary a lot.
  - **Protocol version** — HTTP1 / HTTP2 / **gRPC**, set on the target group; a gRPC backend needs this or it simply won't work.
- **Target states:** `initial` → `healthy` / `unhealthy` → `draining` (being removed, finishing in-flight requests) → `unused`. Reading these is usually the fastest way to diagnose "the ALB returns 503".
- **Weighted target groups:** a listener rule can forward across **multiple target groups by weight** — which is canary/blue-green **at the load balancer**, and the mechanism CodeDeploy uses for ECS blue/green deployments.

For the ALB/NLB/GWLB/CLB comparison table and the ALB-vs-API-Gateway decision, see [ALB vs API Gateway vs ELB](#alb-vs-api-gateway-vs-elb-nlbgwlbclb). The specifics worth adding here:

**ALB (Layer 7)** — routes on HTTP content: host, path, header, query string, method, source IP. Supports **SNI** (many TLS certificates on one listener), HTTP/2, gRPC, WebSocket, **Lambda targets**, native redirect and fixed-response actions, and built-in **Cognito/OIDC authentication** at the listener. Because it terminates HTTP, the original client IP arrives in the **`X-Forwarded-For`** header (plus `X-Forwarded-Proto` and `-Port`) — in ASP.NET Core you must enable `ForwardedHeadersMiddleware` or every client will look like the load balancer, which silently breaks rate limiting, geo-logic, and audit logs.

**NLB (Layer 4)** — millions of requests per second at ultra-low latency. Two properties that decide questions: it can have **one static IP per AZ (and supports Elastic IPs)**, and it **preserves the client source IP** with no header needed. Can pass TLS straight through to targets, or terminate it. Targets can be instances, IPs (including on-prem via Direct Connect), or even an ALB.

**❗ An ALB has no static IP — only a DNS name**, and the IPs behind it change. If a client or partner firewall requires a fixed IP, the answer is **NLB**, **NLB fronting an ALB**, or **Global Accelerator** (see [Global Accelerator](14-global-edge-services.md#aws-global-accelerator)). This is one of the most commonly asked ELB questions.

**Gateway Load Balancer — GWLB (Layer 3)** — transparently routes all traffic through a fleet of third-party inspection appliances (firewall, IDS/IPS) using **GENEVE on port 6081**, preserving the original packet. It's a traffic-inspection insertion point, not a conventional load balancer. Pair it with [AWS Network Firewall](12-security-services.md#aws-network-firewall) as the AWS-native alternative.

**Classic Load Balancer — CLB** — the legacy Layer 4/7 balancer, now deprecated. It predates target groups, so it registers instances directly and supports neither host/path routing, SNI multi-certificate listeners, nor Lambda targets. The only correct answer for "we're on EC2-Classic"; otherwise migrate to ALB (HTTP) or NLB (TCP/UDP). Note the terminology difference it leaves behind: CLB calls it **"connection draining"**, ALB/NLB call the same thing **"deregistration delay"** (see [Connection Draining](#connection-draining--deregistration-delay)).

**Health checks:** protocol, port, and path (`/health`), plus interval, timeout, and healthy/unhealthy thresholds. A target failing the threshold stops receiving traffic and — if the ASG is configured for it — gets replaced. **Point the check at an endpoint that actually exercises the app's dependencies** (a `/health` that only returns `200 OK` from Kestrel will happily report healthy while the database connection pool is exhausted). ASP.NET Core's `AddHealthChecks()` with DB/cache probes is the right implementation.

### Sticky Sessions (Session Affinity)

**What it does:** pins a given client to the same target for the duration of a session, so in-process session state stays valid.

| Load balancer | Mechanism |
|---|---|
| **ALB** | **Duration-based** — LB-generated `AWSALB` cookie with a configurable duration (1 second to 7 days); or **application-based** — the LB honours *your* app's cookie via `AWSALBAPP`, so the app controls session lifetime |
| CLB | `AWSELB` cookie, or an application cookie |
| **NLB** | No cookies (it's Layer 4) — stickiness is by **source IP / flow hash** per target group |

**The trade-offs to name:** stickiness undermines even load distribution (one pinned client can hot-spot a target), and when a target is removed on scale-in or deploy, those sessions are **lost anyway** — so it never actually guarantees session survival.

**The senior answer:** stickiness is a workaround for a stateful app tier. The real fix is to externalise session state so any instance can serve any request — **ElastiCache for Redis** or **DynamoDB**. In .NET that's `IDistributedCache` (`AddStackExchangeRedisCache`) instead of in-process `ISession`. Sticky sessions are legitimate for legacy applications you can't refactor, or where a genuinely expensive per-user in-memory context makes affinity worth the cost — say that rather than declaring stickiness simply "bad".

### Connection Draining / Deregistration Delay

Same feature, two names: **"connection draining"** on CLB, **"deregistration delay"** on ALB/NLB target groups.

**What happens:** when a target is being removed (scale-in, deploy, manual deregistration) it enters `draining` state — **no new requests are routed to it**, but in-flight requests are allowed to complete for up to the configured delay. After that, remaining connections are closed.

- Default **300 seconds**; configurable **0–3600**.
- Tune it to your **longest legitimate request**. Set it too low and you cut off large file uploads or slow reports mid-flight (users see 502/504); set it too high and every deploy and scale-in crawls.
- Set it to `0` only for workloads with genuinely instantaneous requests where deploy speed matters more.

**"How do you deploy without dropping requests?"** — the complete answer chains three things: an appropriate **deregistration delay**, **ELB health checks** on the ASG so a bad instance is caught, and **ASG lifecycle hooks** (below) so an instance is warmed before it serves and drained before it dies. Add **ASG instance refresh** for a rolling replacement.

---

## 3. Auto Scaling

### Auto Scaling Groups (ASG)

**What it gives you:** maintains a target number of instances, replaces failed ones automatically, spans multiple AZs (that's the HA part), and registers/deregisters targets with the load balancer as it scales.

**Core settings:** **minimum** (never fewer), **desired** (current target, what scaling policies change), **maximum** (never more, and your cost ceiling). Instances launch from a **launch template** — the modern replacement for the deprecated *launch configuration*; launch templates support versioning, mixed instance types, and mixed On-Demand/Spot.

**❗ The health-check gotcha:** an ASG's health-check type defaults to **EC2 only**, which means it only replaces instances whose *hypervisor-level* status checks fail. An instance whose application has hung — or is returning 500s — looks perfectly healthy to EC2 and is **never replaced**, even while the ALB has already stopped sending it traffic. You must set the health check type to **ELB** so the ASG acts on the load balancer's application-level view. This is a genuine production incident pattern and a very common interview question.

**Scaling policies:**
| Policy | How it works | When to use |
|---|---|---|
| **Target tracking** | "Keep this metric at this value" — e.g. average CPU at 40%. AWS creates and manages the alarms | ✅ **The default recommendation.** Simplest and handles most cases |
| **Step scaling** | CloudWatch alarm → add/remove N instances, with different steps by alarm severity | When you need aggressive response to big breaches (e.g. +1 at 60% CPU, +4 at 85%) |
| **Simple scaling** | One alarm → one adjustment, then wait for cooldown | Legacy; step scaling supersedes it |
| **Scheduled scaling** | Change min/desired/max at a specific time | Known patterns: business hours, a marketing launch, month-end batch |
| **Predictive scaling** | ML on historical traffic, scales **ahead** of forecast demand | Cyclical daily/weekly traffic where reactive scaling is always a few minutes late |

**What metric to scale on — the senior differentiator.** CPU is the default and often the wrong signal. Better choices:
- **`ALBRequestCountPerTarget`** for a web/API tier — directly proportional to demand, and it reacts before CPU does.
- **SQS queue depth** for a worker tier — specifically **backlog per instance** (`ApproximateNumberOfMessagesVisible` ÷ running instances) as a target-tracking metric. This is the canonical answer for "how would you scale a queue-consuming service?", and it's much better than CPU because a worker blocked on I/O shows low CPU while the backlog grows.
- Custom application metrics (p99 latency, active connections, thread-pool saturation) published to CloudWatch.

**The rest of the ASG surface worth knowing:**
- **Cooldown / warm-up** — a pause after a scaling action so metrics can settle before the next one, preventing thrash. Target tracking uses *instance warm-up* instead: new instances aren't counted in the metric until they're actually ready.
- **Lifecycle hooks** — pause an instance in `Pending:Wait` (bootstrap, warm caches, register with a service, run smoke tests) or `Terminating:Wait` (drain connections, flush logs, deregister) before it proceeds. The hook for "do something custom on the way in or out".
- **Termination policy** — default order: the AZ with the most instances first, then the oldest launch template/configuration, then the instance closest to the next billing hour. Configurable, and `OldestInstance` is common when you want gradual rotation.
- **Instance refresh** — rolling replacement of every instance with a new launch-template version (e.g. a patched AMI), honouring a minimum healthy percentage. How you ship AMI updates without downtime.
- **Scale-in protection** — exclude specific instances from scale-in, for a node holding work that can't be interrupted.
- **Warm pools** — keep pre-initialised, stopped instances ready so scale-out skips boot and bootstrap time; the answer for apps with slow startup where predictive scaling isn't enough.

```bash
aws autoscaling create-auto-scaling-group --auto-scaling-group-name web-asg \
  --launch-template LaunchTemplateName=web-lt,Version='$Latest' \
  --min-size 2 --max-size 10 --desired-capacity 2 \
  --vpc-zone-identifier "subnet-a,subnet-b,subnet-c" \
  --target-group-arns arn:aws:elasticloadbalancing:... \
  --health-check-type ELB --health-check-grace-period 120     # ← ELB, not EC2
aws autoscaling start-instance-refresh --auto-scaling-group-name web-asg
```

---

## 4. Best Practices & Shared Responsibility

### Scalability Best Practices

- **Make the app tier stateless** — no in-process session, no local file writes that matter. Everything else on this list depends on it.
- **Scale out, not up**, for anything that can be distributed; reserve vertical scaling for the things that can't (databases, single-node caches).
- **Minimum 2 instances across at least 2 AZs** (3 is better) — a single instance is not highly available regardless of instance type.
- **Set the ASG health check type to ELB**, and make `/health` genuinely check dependencies.
- **Scale on a demand-correlated metric** (requests per target, queue backlog), not CPU by reflex.
- **Externalise session and cache state** to ElastiCache/DynamoDB.
- **Use scheduled or predictive scaling for known events** — reactive scaling always lags a spike by the boot time of an instance.
- **Test scale-in, not just scale-out.** Most scaling bugs (dropped requests, lost work, orphaned locks) surface on the way *down*, which is what deregistration delay and lifecycle hooks exist for.
- **Cap `max` deliberately** — it's both a cost ceiling and a blast-radius limit if a bug or an attack drives artificial load.

### Scalability & Load Balancing Shared Responsibility Model

| AWS is responsible for | You are responsible for |
|---|---|
| Running and scaling the ELB fleet itself (multi-AZ, patched, no capacity to manage) | **Choosing the right LB type** (ALB/NLB/GWLB) for the protocol and requirements |
| The ASG control plane — replacing failed instances, honouring your policies | **Setting min/desired/max, the scaling policy, and the metric that drives it** |
| Health-check infrastructure | **Writing a health endpoint that reflects real application health** |
| AZ-level infrastructure availability | **Actually spanning multiple AZs** — AWS won't do it for you |
| TLS termination capability, managed certificates via ACM | Certificate lifecycle, cipher/TLS policy selection, and HTTPS redirect rules |
| Providing lifecycle hooks and deregistration delay | Tuning them so deploys and scale-in don't drop in-flight requests |
| — | **Designing the app to be stateless** so horizontal scaling is possible at all |

**Disaster Recovery — Load Balancing & Auto Scaling**

| | |
|---|---|
| **What's actually at risk** | ALB/NLB and their listeners and target groups, ASGs, launch templates. All config, no data |
| **Backup mechanism** | **IaC.** ASGs are self-healing by design, so the recovery story is mostly "re-apply and let it converge" |
| **Realistic RPO / RTO** | RPO ~0. RTO minutes — plus however long instance warm-up actually takes |

**Recovery runbook:**
1. **Re-apply from IaC** in the DR region.
2. **Pre-scale the DR ASG before shifting traffic.** A DR ASG sitting at `min=0` cannot absorb a failover surge — you get a thundering herd against zero capacity, which looks exactly like the outage you're recovering from.
3. **Shift traffic** with Route 53 **failover or weighted** records against a health check, moving in increments rather than all at once.
4. **Confirm the ASG health check type is `ELB`, not `EC2`.**

⚠️ **The gotcha:** **an ALB gets a brand-new DNS name when it's recreated**, so anything that hardcoded the hostname breaks — always front it with a Route 53 alias record so the name you publish is yours, not AWS's. And the classic real-world finding, worth repeating because it's so common: **ASG health check type left on `EC2` means a hung application is never replaced** — the instance is "running", so the ASG is satisfied while every request fails. (See [Auto Scaling Groups](#auto-scaling-groups-asg).)

---

← [Observability & Monitoring](07-observability-monitoring.md) · [Index](README.md) · [Containers: Docker, ECS, ECR & Fargate](09-containers-ecs-fargate.md) →
