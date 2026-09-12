> **AWS Quick Revision Notes** · [Index](README.md) · Part II

# Containers: Docker, ECS, ECR & Fargate

---

## 1. Docker & Container Fundamentals

### Docker Fundamentals

| | VM | Container |
|---|---|---|
| Isolation | Full OS + kernel | Process, **shares host kernel** |
| Size | GBs | MBs |
| Start | Minutes | Seconds |

- Image (immutable template) vs container (running instance). Layers cached → instruction order matters.
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.csproj ./        # copy csproj first → restore layer caches
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .
USER $APP_UID           # non-root
ENTRYPOINT ["dotnet", "MyApi.dll"]
```
- Multi-stage (SDK out of shipped image); csproj-first caches restore; chiselled/Alpine + AOT shrink.
- **Registry→Repository→Image(tag/digest)→Container.** ❗ Tag = movable, **digest = immutable** → deploy digest/immutable tag, `:latest` anti-pattern.
- Layers: each instruction = read-only layer; container = thin writable layer (vanishes on replace). ❗ Deleting file in later layer doesn't shrink; earlier layer still extractable → never put secrets in build.
- **Base images:** aspnet:8.0 (~220MB, debug easy) / alpine (~110MB, musl) / **jammy-chiseled (~110MB, no shell, non-root — prod default)** / runtime-deps + AOT (~15-30MB). "No shell = feature."
- `.dockerignore` (bin/obj/.git/appsettings.*.json/.env) — speed + secret leak prevention.
- **ENTRYPOINT** (always runs) vs **CMD** (default args). Use **exec form** (`["dotnet","MyApi.dll"]`) — shell form wraps in `/bin/sh -c` → **SIGTERM never reaches app** → dropped in-flight requests. `ARG` (build) vs `ENV` (runtime, visible in inspect).
- ❗ **Multi-arch:** x86-built image fails on Graviton (`exec format error`) → `docker buildx build --platform linux/amd64,linux/arm64 --push` (manifest list).
- Docker Compose = local dev; AWS equivalent = ECS task definition + managed services. Volume → EFS in ECS.

---

## 2. ECS

### ECS

`Cluster → Service → Task → Container(s)`.
- **Task Definition** = JSON blueprint (image/CPU/memory/ports/env/secrets/logs/**2 IAM roles**), versioned.
- ❗ **Task execution role** (ECS agent: pull image, logs, fetch secrets) vs **Task role** (your app: S3/DynamoDB/SQS). Mixing = inexplicable AccessDenied.
- Launch types: Fargate (default) vs EC2 (cheaper high-util, GPU/Windows/daemons; Capacity Providers).
- Networking: **awsvpc** (ENI/IP/SG per task, Fargate-mandatory) vs bridge/host.
- Scaling/deploy: Service Auto Scaling (`ALBRequestCountPerTarget`), rolling update (min/max healthy %), **deployment circuit breaker** (auto-rollback — turn on), blue/green via CodeDeploy, service discovery (Cloud Map/Service Connect), EFS volumes.
- **Sidecars:** multiple containers share task namespace/lifecycle (localhost). FireLens/Fluent Bit, X-Ray daemon, Envoy, OTel. `dependsOn`/`essential` (essential:true stops task on container death).
- **Task placement (EC2):** binpack (cheapest), spread (`attribute:ecs.availability-zone` — prod default), random; constraints distinctInstance/memberOf.

**ECS on EC2:**
- ❗ **3 IAM roles:** ECS **instance role** (`ecsInstanceRole`, agent: register/pull/logs) + task execution + task role.
- Bring instances: ECS-optimised AMI + `echo "ECS_CLUSTER=prod-cluster" >> /etc/ecs/ecs.config` (omit → registers to `default`, cluster shows 0).
- **Capacity Providers + Cluster Auto Scaling:** wraps ASG; managed scaling target-tracks `CapacityProviderReservation`. Target capacity 100 (cheapest, slow) vs <100 (warm headroom). ❗ **Managed termination protection on** (else terminates instances with running tasks). Mix base+weight (On-Demand base, Spot above).
- **Two scaling loops:** Loop 1 (Service Auto Scaling — tasks, demand-driven) + Loop 2 (Cluster Auto Scaling — instances, placement-pressure). Miss Loop 1 → traffic up, tasks flat; miss Loop 2 → tasks stuck `PENDING`. **Fargate has no Loop 2** (per-vCPU premium buys that).
- `Reservation = (M/N)×100` (N running, M needed). Failure modes: no capacity provider → tasks PENDING; term protection off → kills running; missing `ECS_CLUSTER` → cluster zero; ASG max → tasks pending; target 100 → slow burst.
- **Resource:** CPU units (1024=1 vCPU); memory hard limit (OOM) vs `memoryReservation` soft. `no container instance met requirements` = CPU/memory/port conflict/constraint/ENI limit.
- Networking modes: bridge (dynamic port mapping), host (perf, port conflicts), awsvpc (ENI/SG per task, ENI limit). ❗ **Dynamic port mapping** (`hostPort:0`) → ECS registers ephemeral port with ALB TG → multiple copies per instance; SG must allow **32768-65535** from ALB.
- ❗ **Container instance draining** (`DRAINING` → relocate tasks respecting minimumHealthyPercent, wire to ASG lifecycle hook) — different layer from ALB deregistration delay (graceful deploy needs **both**).
- ECS Anywhere (on-prem via SSM); ECS vs EKS one-liner: ECS simpler/AWS-proprietary/free control plane (default); EKS = managed Kubernetes (portability, existing K8s, CNCF ecosystem, +complexity+cost).

---

## 3. Fargate & Launch-Type Choice

### Fargate

- Serverless compute for containers (not orchestrator — launch type for ECS/EKS). Task/Task Definition/Service. Networking: **awsvpc** (ENI per task). No SSH; task-level IAM role.
- Pay vCPU-sec + GB-sec while running. **Constrained vCPU/memory pairs** (0.25→0.5-1-2 GB, etc). Ephemeral storage 20 GB default (200 max) → EFS for persist. **Fargate Spot** (~70% off, 2-min SIGTERM). Platform versions; **no privileged/daemons/GPU**.

| | EC2 | Fargate |
|---|---|---|
| Server mgmt | You | AWS |
| SSH | Yes | No |
| Pricing | instance-hour, idle | per task, no idle |
| Best | steady/high, legacy, GPU | bursty/microservices |

- **Cost trap:** "Fargate always cheaper" = **false**. Bursty/low → Fargate; steady/high → EC2. Numeric: 24×7 1vCPU/2GB ≈ $17-20 EC2 vs $35-40 Fargate; 2hr/day flips.
- Fargate traps: task stopped = crash/health/OOM; healthy-but-unhealthy = wrong health path/port; no internet = private subnet no NAT/endpoint; dev-works-prod-fails = IAM/secrets/networking.

---

## 4. Container Registry

### ECR

Managed private registry (S3-backed, IAM access).
```bash
aws ecr get-login-password | docker login --username AWS --password-stdin <acct>.dkr.ecr...
docker tag myapi:1.0 <acct>.dkr.ecr.../myapi:1.0
docker push <acct>.dkr.ecr.../myapi:1.0
```
- **Enhanced scanning** (Inspector, continuous CVE), **tag immutability** (on), **lifecycle policies** (expire untagged), cross-region replication, **pull-through cache** (Docker Hub rate limits), Public Gallery.

**Hands-on debug (tasks won't start):** `describe-services` events → stopped task `stoppedReason` → execution role can't pull/log / arch mismatch / health check before start / no ECR route (NAT/endpoints) / missing env var.

---

## 5. Choosing a Platform for .NET

### .NET Compute Choice & Beanstalk

- **ECS on Fargate = default** for new .NET microservices; **Lambda** for event/spiky/glue; **EKS** only if existing K8s; **EC2** for .NET Framework/Windows/GPU. Team knows only .NET, no K8s → not EKS (over-engineering).
- **Elastic Beanstalk** = PaaS wrapper over EC2/ASG/ELB (not new primitive) — fastest `dotnet publish` to running app; blue/green via CNAME swap. Deploy strategies: rolling/immutable/blue-green.

**Fargate/ECS/EKS trade-offs (honest framing):** hands-on = Lambda/DynamoDB/EC2/S3, not containers. K8s biggest selling point (portability/ecosystem) = biggest cost (learning curve). Decision flow: existing K8s/multi-cloud → EKS; else OS/GPU/legacy → EC2; else spiky → Lambda; else steady containerised → **ECS on Fargate**.

**DR — Containers:** risk = ECR images, task defs, config, EFS. Backup = ECR replication + immutable tags, task defs (versioned) + IaC, AWS Backup for EFS. Bad release → update service to previous **task def revision**; regional → confirm image in DR ECR, register task def, create service; pre-scale before DNS shift. ⚠️ Task def pins **full image URI** (account+region) → copy to DR still points at original ECR; set ECR replication + parameterise URI; never deploy `:latest`.

---

← [Load Balancing, Scalability & Auto Scaling](08-load-balancing-autoscaling.md) · [Index](README.md) · [Relational Databases, Caching & Analytics](10-databases-caching-analytics.md) →
