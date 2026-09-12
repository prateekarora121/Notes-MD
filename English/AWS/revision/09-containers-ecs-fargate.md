> **AWS Quick Revision Notes** · [Index](README.md) · Part II

# Containers: Docker, ECS, ECR & Fargate

---

## 1. Docker & Container Fundamentals

### Docker Fundamentals

Container = app+deps, shares host kernel (vs VM = own kernel/hypervisor). MBs/seconds/high density/weaker isolation.
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.csproj ./           # copy csproj first → restore layer caches
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final    # runtime-only, smaller
WORKDIR /app
COPY --from=build /app .
USER $APP_UID              # not root
ENTRYPOINT ["dotnet", "MyApi.dll"]
```
Multi-stage (SDK out of image), csproj-before-source (cache restore), chiselled/AOT shrink further. Container = immutable artifact promoted unchanged dev→prod (kills "works on my machine"; config from env).
**Vocab:** Registry→Repository→Image (tag=movable / digest=immutable hash)→Container. Deploy by **digest/immutable tag**, never `:latest`. Layers = read-only stack + thin writable layer (changes vanish on replace → state to volume/EFS/S3/DB). **❗ Deleting a file in a later layer doesn't shrink the image + earlier-layer content still extractable** (never bake secrets). Base images: aspnet:8.0 (~220 MB, shell) / alpine (~110) / **jammy-chiseled (~110, no shell, non-root — prod default)** / runtime-deps-chiseled + **AOT (~15–30 MB)**. "No shell" = feature (no pivot). `.dockerignore` (avoid uploading bin/obj/.git, avoid baking `appsettings.*.json`/`.env`/creds). **`ENTRYPOINT`** (what it is, hard override) vs **`CMD`** (default args); use **exec form** (shell form → no `SIGTERM` → dropped requests on deploy). `ARG` (build) vs `ENV` (runtime, `docker inspect` — not secrets). **❗ Multi-arch**: x86 image won't run on Graviton (`exec format error`) → `docker buildx build --platform linux/amd64,linux/arm64 --push`. Compose = local/dev; volumes for persistence.

---

## 2. ECS

### ECS

AWS's own orchestrator (simpler than K8s, free control plane). `Cluster → Service → Task → Container(s)`. Task Definition (JSON, versioned, image/CPU/memory/ports/env/secrets/logs + two roles), Service (keeps N healthy, registers ALB).
**❗ Two roles:** **task execution role** (ECS agent — ECR pull, logs, fetch secrets, before code) vs **task role** (app code — S3/DynamoDB/SQS). Adding app perms to execution role = inexplicable AccessDenied.
Launch types: Fargate (default) vs EC2 (patch instances, cheaper high steady, GPU/Windows/daemons; Capacity Providers). Networking `awsvpc` (ENI/IP/SG per task, **mandatory Fargate**) / bridge / host. Scaling: target tracking on `ALBRequestCountPerTarget`; rolling update min/max %; **deployment circuit breaker** (auto-rollback — turn on); blue/green via CodeDeploy; service discovery Cloud Map/Service Connect; EFS for shared storage. **Sidecars** (share task net/lifecycle, `localhost`): FireLens/Fluent Bit, X-Ray daemon, Envoy, OTel Collector; `dependsOn`/`essential`. Task placement (EC2 only): `binpack` (cost) / `spread` (resilience — `attribute:ecs.availability-zone` prod default) / random; constraints distinctInstance/memberOf.

**ECS on EC2:** **three IAM roles** — adds **ECS instance role** (`ecsInstanceRole`, on instance profile, agent registers/pulls/logs). Get instances into cluster:
```bash
echo "ECS_CLUSTER=prod-cluster" >> /etc/ecs/ecs.config    # omit → registers to 'default', cluster shows 0
```
**Capacity Providers + Cluster Auto Scaling:** wraps an ASG, managed scaling target-tracks `CapacityProviderReservation`. Target % (100 = just fits/slow; <100 warm headroom); **managed termination protection on** (else terminates instances with tasks); base+weight for On-Demand+Spot.
**Two scaling loops:** Loop 1 Service Auto Scaling (tasks, demand-driven) + Loop 2 Cluster Auto Scaling (instances, placement-pressure via capacity provider). Missing L1 → traffic up, tasks flat; missing L2 → tasks stuck `PENDING`. **On Fargate Loop 2 doesn't exist** (that's the per-vCPU premium). `Reservation = (M needed / N running)×100`: >100 scale out, <100 scale in.
Failure modes: PENDING + ASG never grows → no capacity provider · scale-in kills running tasks → termination protection off · instances launch but cluster shows 0 → `ECS_CLUSTER=` missing · ASG at max, still pending → raise max · slow burst → target at 100 (lower / warm pools).
Resources: **1024 CPU units = 1 vCPU**; `memory` hard limit (OOM-kill) vs `memoryReservation` soft. "unable to place task" → CPU/memory, host port conflict, placement constraint, ENI limit. **❗ Dynamic port mapping** (`hostPort:0` in bridge → random port, ECS registers with ALB → multiple copies per host; SG must allow **ephemeral 32768–65535** from ALB SG). **Container instance draining** (`DRAINING` — relocate tasks, wire to ASG lifecycle hook) ≠ ALB connection draining (in-flight requests) — graceful deploy needs both.
Choose EC2 over Fargate: GPU, sustained high util, privileged/sysctls, per-host daemon, storage >200 GB, per-host licensing/RI, high task density. **ECS Anywhere** (on-prem via SSM, no ELB/awsvpc). **ECS vs EKS:** ECS simpler/proprietary/free control plane (pick unless a reason); EKS = managed K8s (portability, existing K8s, CNCF, + complexity + control-plane cost).

---

## 3. Fargate & Launch-Type Choice

### Fargate

Serverless container compute — supply image + CPU/memory; a launch type for **ECS/EKS**, not an orchestrator. **Task** (running container), **Task Definition** (blueprint), **Service** (keeps N healthy). `awsvpc` only (ENI per task, IP exhaustion risk in small subnets). No SSH/host; task-level IAM. Pay vCPU-sec + GB-sec running only. **Sizing = valid vCPU/memory pairs** (0.25→0.5-1-2 GB, etc.) — invalid combo rejected. **Ephemeral 20 GB default (→200 GB)**, gone on stop (persist → EFS). **Fargate Spot** ~70% off, 2-min SIGTERM (mix via capacity provider strategy — base on FARGATE, burst on FARGATE_SPOT). No privileged/daemons/GPU → EC2. Platform versions (`LATEST`).
| | EC2 | Fargate |
|---|---|---|
| Server mgmt | You | AWS |
| SSH | Yes | No |
| Pricing | Instance-hour + idle | Per task, no idle |
| Isolation | Shared host possible | Dedicated ENI/kernel |
| Best for | Steady/high, legacy, GPU | Bursty/microservices, ops simplicity |

**Cost trap:** "Fargate always cheaper" = **false**. Fargate wins bursty/low; EC2 wins steady high (no per-task premium). Rule of thumb: 24×7 1vCPU/2GB ≈ $17–20 EC2 (t3.small) vs $35–40 Fargate; 2hr/day flips to ~$20 EC2 vs ~$6–8 Fargate. False claims: always cheaper / replaces EC2 / SSH into Fargate / no networking limits.

### EC2 vs Fargate Traps

EC2: low CPU but slow → disk/network/single-thread · public IP changed → no EIP · ASG didn't scale → metric/cooldown · unreachable despite running → SG/NACL/route/public IP · Spot terminated → expected (2-min EventBridge). Fargate: task stopped → crash/health/OOM · healthy but unhealthy → wrong health path/port · can't reach internet → private subnet no NAT/endpoint · dev works prod fails → IAM/secrets/networking. Mental model: steady→EC2, bursty→Fargate, control→EC2, simplicity→Fargate.

---

## 4. Container Registry

### ECR

Managed private registry (S3-backed, IAM + repo policies).
```bash
aws ecr get-login-password | docker login --username AWS --password-stdin <acct>.dkr.ecr.us-east-1.amazonaws.com
docker tag myapi:1.0 <acct>.dkr.ecr.us-east-1.amazonaws.com/myapi:1.0 && docker push ...
```
**Enhanced scanning** (Inspector, continuous CVE rescanning), **tag immutability** (turn on), lifecycle policies (expire old — storage bill), cross-region/account replication, **pull-through cache** (avoid Docker Hub rate limits), Public Gallery.

**Hands-on Fargate:** create-repository (IMMUTABLE + scanOnPush) → create-cluster → register-task-definition (family + two roles + awsvpc + awslogs) → create-service behind ALB across 3 AZs with `deploymentCircuitBreaker`. Debug won't-start: `describe-services` events → stopped task `stoppedReason` → usually execution role can't pull ECR/write logs, wrong image arch, health check before startup, no ECR route from private subnet (NAT / ECR+S3 endpoints), or missing env var. `ecs execute-command` = ECS Exec shell.

---

## 5. Choosing a Platform for .NET

### ECS vs EKS vs Fargate vs Lambda for .NET

Default new .NET microservices → **ECS on Fargate**. Lambda → event-driven glue/spiky. EKS only if existing K8s investment (introducing K8s just for .NET migration = over-engineering). EC2 → .NET Framework/Windows/IIS or GPU. Lambda = no .NET Framework (needs Core/5+). Cold: EC2 ASG minutes, Fargate task seconds, EKS-Fargate similar, Lambda ms-s. "Team knows only .NET no K8s — EKS for greenfield?" → No unless concrete multi-cloud/portability need.

### Deploying .NET: Beanstalk vs ECS vs Lambda

**Beanstalk** = PaaS wrapper over EC2/ASG/ELB (fastest `dotnet publish`→running; less control; not a new primitive — it's orchestration tooling). ECS = default modern. Lambda = event-driven/spiky. Deploy strategies: Beanstalk (rolling/immutable/blue-green via CNAME swap), ECS (rolling / blue-green CodeDeploy two target groups), Lambda (versions+aliases linear/canary via CodeDeploy).

**Containers DR:** at risk = ECR images, task defs, service config, EFS state. Backup = **ECR replication rules** + immutable tags + versioned task defs + IaC + AWS Backup (EFS). Bad release → previous task def revision (`update-service --task-definition my-app:41`). Regional → image in DR ECR + register + create service. **❗ Task def pins image by full URI (account+region)** → copying to another region still points at original ECR → can't pull on regional outage. Set up ECR replication + parameterise URI per region. Scale DR service before shifting traffic. Never `:latest`.

---

← [Load Balancing, Scalability & Auto Scaling](08-load-balancing-autoscaling.md) · [Index](README.md) · [Relational Databases, Caching & Analytics](10-databases-caching-analytics.md) →
