> **AWS Detailed Guide** · [Index](README.md) · Part II

# Containers: Docker, ECS, ECR & Fargate

> **Tier 2 — reason about, be honest about hands-on.** Docker I use; ECS/Fargate I can design with but have not operated in production. The honest framing is in [Fargate/ECS/EKS Trade-offs](#fargateecseks-trade-offs--reasoning-without-hands-on-time) — use it rather than bluffing.

---

## 1. Docker & Container Fundamentals

### Docker & Container Fundamentals

**What a container is:** your application plus its dependencies packaged into one immutable artifact that runs identically anywhere. Unlike a VM it **shares the host's kernel** rather than booting its own OS.

| | Virtual Machine | Container |
|---|---|---|
| Isolation | Full OS + own kernel, via hypervisor | Process-level, **shares the host kernel** |
| Size | GBs | MBs |
| Start time | Minutes | **Seconds or less** |
| Density per host | Low | High |
| Trade-off | Stronger isolation | Far cheaper and faster, weaker kernel-level isolation |

**Image vs container:** an **image** is the immutable template; a **container** is a running instance of it. (The class-vs-object analogy lands well.) Images are built from a **Dockerfile** in **layers**, and layers are cached — which is why instruction order matters enormously for build speed.

```dockerfile
# Multi-stage build — the .NET pattern worth knowing by heart
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.csproj ./                 # copy csproj first so restore layer caches
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final   # runtime-only, much smaller
WORKDIR /app
COPY --from=build /app .
USER $APP_UID                    # don't run as root
ENTRYPOINT ["dotnet", "MyApi.dll"]
```
Three points to make about this: **multi-stage builds** keep the SDK out of the shipped image (hundreds of MB saved); **copying the `.csproj` before the source** means `dotnet restore` is cached and only re-runs when dependencies change; and the **chiselled/Alpine** runtime variants plus **Native AOT** shrink it further, which matters for cold starts and ECR storage.

```bash
docker build -t myapi:1.0 .
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development myapi:1.0
docker ps / docker logs <id> / docker exec -it <id> sh
```
**Why any of this is an interview topic:** the container is the *immutable artifact* you promote unchanged from dev to prod — the same bits that passed CI are the bits that run in production. That's what actually kills "works on my machine", and it's the reason config comes from the environment (env vars, Parameter Store, Secrets Manager) rather than being baked into the image.

#### Docker Images — The Detail Worth Knowing

**Get the vocabulary straight first**, because interviewers use these interchangeably and precision reads well:
```
REGISTRY            ECR, Docker Hub                       — the server that stores repositories
 └─ REPOSITORY      123456789012.dkr.ecr…/myapi           — all versions of one image
     └─ IMAGE       myapi:1.4.2   (tag)                   — a mutable, human-friendly pointer
     └─ IMAGE       myapi@sha256:9f2a…  (digest)          — the immutable content hash
         └─ CONTAINER                                     — a running instance of that image
```
**Tag vs digest is the one that matters operationally.** A **tag is a movable label** — `myapi:1.4.2` can be repointed at different bits tomorrow. A **digest is a cryptographic hash of the content** and can never mean anything else. So deploy by **digest** (or an immutable tag) for reproducibility and reliable rollback; `:latest` is the anti-pattern, because "roll back to the previous latest" is not a thing that exists. This is why **[ECR tag immutability](#ecr-elastic-container-registry)** is worth turning on.

**Layers, properly.** Each Dockerfile instruction creates a **read-only layer**, and the image is those layers stacked by a union filesystem. A running container adds a thin **writable layer** on top — which is why **container filesystem changes vanish when it's replaced** (state must go to a volume, EFS, S3, or a database).

Two consequences that get asked:
- **Layers are shared and cached.** Ten containers from the same image don't store ten copies; and pulling a new version only downloads the layers that changed. This is why instruction order matters — put the things that change *least* (base image, dependency restore) **before** the things that change *most* (your source), exactly as the `.csproj`-before-source trick does above.
- **❗ Deleting a file in a later layer does not shrink the image**, and anything present in an earlier layer is **still extractable**. So `COPY secrets.json . && RUN rm secrets.json` leaves the secret in the image permanently. Never put credentials in a build; use build secrets or inject at runtime.

**Base image choice — a real decision, not a detail:**
| Variant | Size (approx.) | Trade-off |
|---|---|---|
| `mcr.microsoft.com/dotnet/aspnet:8.0` | ~220 MB | Full Debian; has a shell and package manager — easiest to debug |
| `…aspnet:8.0-alpine` | ~110 MB | musl libc instead of glibc — small, but watch for native-dependency issues |
| **`…aspnet:8.0-jammy-chiseled`** | **~110 MB** | **Ubuntu chiselled — no shell, no package manager, non-root by default.** Much smaller attack surface. The strong default for production .NET |
| `…runtime-deps:8.0-jammy-chiseled` + **Native AOT** | **~15–30 MB** | Self-contained single binary, fastest startup. Best for Lambda container images and cold-start-sensitive work |

**"No shell" is a feature, not a limitation** — an attacker who achieves RCE has no shell to pivot with. The cost is that `docker exec … sh` no longer works, so you debug through logs and metrics instead. Naming that trade-off is the senior version of the answer.

**`.dockerignore` — small file, two real problems solved.** Everything in the build context is sent to the daemon, so without it you upload `bin/`, `obj/`, `node_modules/`, and `.git` on every build (slow), and risk `COPY . .` baking **`appsettings.Development.json`, `.env`, or `.aws/credentials`** into the image (a genuine secret leak):
```
bin/
obj/
.git/
.vs/
**/node_modules
**/appsettings.*.json
.env
Dockerfile
```

**`ENTRYPOINT` vs `CMD`** — used in the Dockerfile above and worth being able to explain:
- **`ENTRYPOINT`** = the executable that always runs. Hard to override (needs `--entrypoint`).
- **`CMD`** = the default *arguments*, trivially overridden by anything you append to `docker run`.
```dockerfile
ENTRYPOINT ["dotnet", "MyApi.dll"]     # always runs this
CMD ["--environment=Production"]       # default arg; `docker run img --environment=Staging` overrides it
```
Rule: `ENTRYPOINT` for *what the container is*, `CMD` for *how it's configured by default*. Also prefer the **exec form** (`["dotnet","MyApi.dll"]`) over the shell form — the shell form wraps your process in `/bin/sh -c`, so **your app never receives `SIGTERM`** and gets killed instead of shutting down gracefully. In ECS/Kubernetes that turns every deploy into dropped in-flight requests.

Related: **`ARG` vs `ENV`.** `ARG` exists only at build time; `ENV` persists into the running container (and is visible via `docker inspect` — so not for secrets).

**❗ Multi-arch images and Graviton.** An image built on an x86 laptop will **not run** on an ARM64 Graviton instance — the task simply fails to start with an `exec format error`, which is a genuinely confusing first encounter. Since the guide recommends Graviton for a ~20% .NET saving (see [EC2 Instance Types](06-ec2-instance-storage.md#ec2-instance-types-user-data--metadata)), you need to build for the target:
```bash
docker buildx build --platform linux/amd64,linux/arm64 -t <ecr>/myapi:1.4.2 --push .
```
That pushes a **manifest list** — one tag serving both architectures, with each host pulling the right one. This is the practical prerequisite for the Graviton cost saving, and it's the kind of detail that shows you've actually shipped containers.

**Local development** uses **Docker Compose** to run the app plus its dependencies together (`docker compose up`) — a Postgres and a Redis container alongside your API, so a new developer needs no local installs. Worth knowing Compose is a **local/dev tool**: in AWS the equivalent responsibilities belong to an **ECS task definition** (multi-container) and real managed services (RDS, ElastiCache). Note also that a **container writes to an ephemeral layer**, so local persistence needs a **volume** (`-v ./data:/data`) — and in ECS the equivalent is an **EFS volume** mounted into the task.

**Image hygiene, as a checklist:** multi-stage build · chiselled/AOT base · non-root `USER` · pinned base-image tag or digest (not `:latest`) · `.dockerignore` · no secrets in any layer · scanned on push ([ECR enhanced scanning](#ecr-elastic-container-registry)) · a `HEALTHCHECK` or an ALB-checked `/health` endpoint · exec-form `ENTRYPOINT` so `SIGTERM` reaches the app.

---

## 2. ECS

### ECS (Elastic Container Service)

> **Where bridge vs `awsvpc` mode actually bites:** in bridge mode the task has no ENI, so the *instance* SG is the source of all database traffic — [ECS → RDS: The Four Layers of a Database Connection](10-databases-caching-analytics.md#ecs--rds-the-four-layers-of-a-database-connection).

AWS's **own** container orchestrator — simpler than Kubernetes, deeply integrated with IAM/ALB/CloudWatch, and with no control-plane cost.

**The object model, outermost in:**
```
Cluster  →  Service  →  Task  →  Container(s)
              ↑          ↑
        desired count   instance of a Task Definition
```
- **Task Definition** — the JSON blueprint: image, CPU/memory, port mappings, environment variables, secrets (from Secrets Manager/Parameter Store), log configuration, and **the two IAM roles**. It's versioned; every change creates a new revision.
- **Task** — one running instance of a task definition (one or more co-located containers).
- **Service** — keeps N tasks running and healthy, replaces failures, and registers/deregisters them with an ALB target group.
- **Cluster** — the logical grouping and, for the EC2 launch type, the pool of instances.

**❗ The two roles — the most common real-world ECS mistake:**
| Role | Used by | Needs permissions for |
|---|---|---|
| **Task execution role** | The ECS agent, *before* your code runs | Pulling the image from **ECR**, writing to **CloudWatch Logs**, fetching secrets referenced in the task definition |
| **Task role** | **Your application code** | S3, DynamoDB, SQS — whatever the app actually calls |

Adding your app's DynamoDB permission to the *execution* role produces an `AccessDenied` that looks inexplicable. See [IAM Roles](03-iam-security.md#iam-roles-policies-assumerole).

**Launch types:**
- **Fargate** — serverless; you specify CPU/memory and AWS runs it. No instances to patch or scale. Higher per-vCPU price, no idle waste. Default choice.
- **EC2** — you run and patch the instances (with the ECS agent) and pack tasks onto them. Cheaper at high, steady utilisation, and required for GPU, Windows-specific host needs, or host-level daemons. **Capacity Providers** (with managed scaling) let the cluster add/remove EC2 capacity as tasks demand it.

**Networking modes:** **`awsvpc`** gives every task **its own ENI, private IP, and security group** — this is **mandatory for Fargate** and the right choice generally, because it means you can write security-group rules per service. `bridge` and `host` are the older EC2-launch-type modes (host shares the instance's network stack; bridge maps ports and complicates dynamic port registration).

**Scaling and deployment:**
- **Service Auto Scaling** — target tracking on `ECSServiceAverageCPUUtilization`, memory, or (best) **`ALBRequestCountPerTarget`**; the same "scale on a demand-correlated metric" principle as [ASGs](08-load-balancing-autoscaling.md#auto-scaling-groups-asg).
- **Rolling update** with `minimumHealthyPercent` / `maximumPercent` controlling how many tasks may be down or extra during a deploy.
- **Deployment circuit breaker** — automatically rolls back a deployment whose tasks fail to stabilise. Turn it on; it's the difference between a failed deploy and an outage.
- **Blue/green via CodeDeploy** — shifts traffic between two target groups with canary/linear options and automatic rollback on CloudWatch alarms.
- **Service discovery** via **Cloud Map** (DNS names for services) or **Service Connect** for service-to-service traffic.
- **Persistent/shared storage** via **EFS** volumes mounted into tasks (see [EFS](06-ec2-instance-storage.md#efs-elastic-file-system)).

**Sidecar containers.** A task definition can hold **several containers that share the task's network namespace and lifecycle** — so they reach each other on `localhost` and start/stop together. That's the sidecar pattern, and the standard AWS examples are the **FireLens/Fluent Bit** log router, the **AWS X-Ray daemon**, an **Envoy** proxy for App Mesh/Service Connect, and the **OpenTelemetry Collector**. Use `dependsOn` and `essential` to control ordering — `essential: true` means the whole task stops if that container dies, which is what you want for your app container and *not* for a best-effort log shipper. The interview point: sidecars keep cross-cutting concerns (logging, tracing, mTLS) out of your application image.

**Task placement (EC2 launch type only — Fargate handles this for you):**
- **Strategies:** `binpack` (pack tasks tightly onto fewest instances — cheapest, best for cost), `spread` (distribute across AZs or instances — most resilient, and `spread` on `attribute:ecs.availability-zone` is the usual production default), `random`.
- **Constraints:** `distinctInstance` (never two of these on one host) and `memberOf` with an expression (e.g. only GPU or Graviton instances).

Cost versus resilience is the trade-off to name: `binpack` minimises instance count and therefore spend; `spread` survives losing a host or an AZ. Most teams spread across AZs and binpack within them.

#### ECS on the EC2 Launch Type

**What changes versus Fargate:** you own the servers. The cluster becomes a pool of **container instances** — EC2 instances running the **ECS container agent**, registered to the cluster. ECS schedules tasks onto them; you keep them alive, patched, and correctly sized.

**❗ There are three IAM roles, not two.** The doc above covers the two *task* roles; the EC2 launch type adds a third, and forgetting it is the classic "my instances never show up in the cluster" failure:

| Role | Attached to | Used by | Needs permissions for |
|---|---|---|---|
| **ECS instance role** (`ecsInstanceRole`, policy `AmazonEC2ContainerServiceforEC2Role`) | The **EC2 instance profile** | The **ECS agent** on the host | Registering the instance with the cluster, pulling from ECR, writing logs |
| **Task execution role** | The task definition | ECS, *on behalf of* the task | ECR pull, CloudWatch Logs, fetching secrets |
| **Task role** | The task definition | **Your application code** | S3, DynamoDB, SQS — whatever the app calls |

Fargate only has the latter two, because there's no instance for you to own.

**Getting instances into the cluster.** Use the **ECS-optimised AMI** (ECS agent + container runtime pre-installed), or install the agent yourself. Then the one line people forget:
```bash
#!/bin/bash
echo "ECS_CLUSTER=prod-cluster" >> /etc/ecs/ecs.config
```
Omit it and the instance launches fine, registers itself with the cluster named `default`, and your actual cluster shows **0 container instances** while everything looks healthy in EC2.

**Capacity Providers + Cluster Auto Scaling** — the right way to size the pool. A capacity provider wraps an **ASG**; with **managed scaling** enabled, ECS publishes a `CapacityProviderReservation` metric and target-tracks it, so the ASG grows when tasks can't be placed and shrinks when capacity is idle.
- **Target capacity %** — `100` means "scale so tasks just fit" (cheapest, slowest to place new tasks); below 100 keeps warm headroom so tasks start immediately.
- **❗ Managed termination protection must be on**, otherwise the ASG will happily terminate an instance that still has running tasks on it.
- Mix providers with **base and weight** — e.g. base of 2 tasks on On-Demand, everything above that on **EC2 Spot**.

#### How EC2, ASG, Capacity Provider, Cluster, Service & Tasks Fit Together

```
       +-------------------------------------------------------------------+
       |                          ECS CLUSTER                              |
       |     the boundary: holds services AND registered instances         |
       +--------------------+---------------------------+------------------+
                            |                           |
     -- LOOP 1: scale TASKS -+                           +- LOOP 2: scale INSTANCES --
                            |                           |
                   +--------v---------+        +---------v-------------+
                   |     SERVICE      | picks  |  CAPACITY PROVIDER    |
                   |  desiredCount=6  |------->|  wraps exactly 1 ASG  |
                   |                  |  via a |  + managed scaling    |
                   | (Service Auto    |strategy|                       |
                   |  Scaling)        |        |  <<< THE BRIDGE >>>   |
                   +--------+---------+        +---------+-------------+
                            |                            |
                            | maintains                  | target-tracks
                            |                            | CapacityProviderReservation
                   +--------v---------+        +---------v-------------+
                   |      TASKS       |        |         ASG           |
                   |  6 containers    |        |  min / desired / max  |
                   +--------+---------+        +---------+-------------+
                            |                            |
                            | scheduled onto             | launches / terminates
                            |                            |
                            +------------+---------------+
                                         |
                            +------------v-------------------------+
                            |       CONTAINER INSTANCES            |
                            |  EC2 + ECS agent, registered to the  |
                            |  cluster via ECS_CLUSTER=<name>      |
                            +--------------------------------------+
```

**Reading it as ownership — who knows about what:**
| Object | What it is | What it knows |
|---|---|---|
| **ECS Cluster** | A logical boundary holding services and registered instances | Nothing about EC2 or ASGs **directly** |
| **Container instance** | One EC2 instance running the ECS agent, registered to the cluster (1 EC2 instance = 1 container instance) | Which cluster it joined |
| **ASG** | Owns the EC2 lifecycle — launches and terminates instances | **Nothing about ECS, tasks, or containers** |
| **Capacity Provider** | **The bridge.** Wraps exactly one ASG and is attached to the cluster | Both worlds — it's the *only* object that lets ECS influence the ASG |
| **Service** | Keeps N tasks running; selects capacity via a **capacity provider strategy** | Task count and which provider(s) to use |
| **Task** | The running container(s) | The instance it was placed on |

**❗ The insight the diagram exists to make: there are two independent scaling loops.**

| | **Service Auto Scaling** (Loop 1) | **Cluster Auto Scaling** (Loop 2) |
|---|---|---|
| Scales | **Tasks** (`desiredCount`) | **EC2 instances** (ASG desired capacity) |
| Driven by | Application demand — `ALBRequestCountPerTarget`, CPU, SQS backlog | **Task placement pressure** — tasks that don't fit |
| Mechanism | Application Auto Scaling target tracking | Capacity provider **managed scaling** |
| Missing it means | Traffic rises but task count stays flat | Tasks sit **`PENDING`** forever with nowhere to run |

They're complementary: **Loop 1 decides how many tasks you want; Loop 2 makes sure there's somewhere to put them.** Scaling tasks without scaling instances just produces pending tasks; scaling instances without scaling tasks just produces an idle bill.

**On Fargate, Loop 2 doesn't exist** — AWS supplies the capacity, so there's no ASG, no capacity provider wrapping one, and no instance registration. That absence *is* what the Fargate per-vCPU premium buys.

**How the reservation metric actually works** — one number drives Loop 2:
```
CapacityProviderReservation = (M / N) x 100

  N = instances currently running in the ASG
  M = instances ECS calculates it NEEDS for running + pending tasks
```
- **M > N** → metric above 100 → tasks can't fit → ASG scales **out**
- **M < N** → metric below 100 → spare capacity → ASG scales **in**
- **Target capacity `100`** means "aim for exactly enough" — cheapest, but a new task waits for an instance to boot. Set it to **~80** to hold roughly 20% warm headroom so tasks start immediately.

**Failure modes, and which object is at fault:**
| Symptom | Which object |
|---|---|
| Tasks stuck `PENDING`, ASG never grows | **No capacity provider**, or managed scaling off — ECS has no way to *ask* for instances |
| Scale-in terminates instances with running tasks | **Managed termination protection off** |
| Instances launch fine but the cluster shows zero | **Agent** — `ECS_CLUSTER=` missing from user data, so it registered to `default` |
| ASG at max, tasks still pending | **ASG `max`** — the ceiling is yours to raise, ECS won't exceed it |
| Capacity arrives too slowly during a burst | **Target capacity at 100** — no headroom. Lower it, or use ASG warm pools |

**One-liner:** *"The ASG owns instances, the service owns tasks, and the capacity provider is the only thing connecting the two — which is why ECS cannot scale your EC2 fleet without one, and why Fargate needs none of this."*

**Resource allocation — the numbers ECS actually schedules on:**
- **CPU is in "CPU units": 1024 units = 1 vCPU.**
- Memory has two settings: **`memory` is a hard limit** (exceed it and the container is OOM-killed) and **`memoryReservation` is a soft limit** (the amount guaranteed and used for scheduling; the container may burst above it if the host has room). Best practice is to set the soft limit for scheduling and the hard limit as a safety ceiling.
- ECS only places a task where the **remaining** CPU *and* memory fit, which produces the single most common EC2-launch-type error:

> `unable to place a task because no container instance met all of its requirements`

Causes, in the order worth checking: not enough remaining CPU/memory on any instance · a **host port conflict** · no instance satisfying a placement constraint (see [task placement](#ecs-elastic-container-service) above) · the per-instance **ENI limit** reached in `awsvpc` mode.

**Networking modes — a choice you only get on EC2:**
| Mode | Behaviour |
|---|---|
| **`bridge`** | Docker's default — container ports mapped to host ports. Enables dynamic port mapping (below) |
| **`host`** | The container shares the host's network stack directly. Best performance, no mapping, but **port conflicts** and no per-task security group |
| **`awsvpc`** | Each task gets its own ENI, private IP, and security group (same as Fargate). Cleanest security model, but **capped by the ENI-per-instance limit** — raise it with ENI trunking |
| `none` | No external networking |

**❗ Dynamic port mapping — the classic EC2-launch-type question.** In `bridge` mode, set **`hostPort: 0`** (or omit it) and Docker assigns a random ephemeral host port; **ECS then registers that specific port with the ALB target group automatically**. This is what lets you run **several copies of the same container on one instance** — with a static `hostPort` the second task can't be placed, because the port is taken.

The catch: the instance's security group must allow the **ephemeral range 32768–65535** from the ALB's security group, not just port 80. This is the answer to *"how do you run three replicas of a service on one EC2 host behind an ALB?"* — and it's a genuine advantage of `bridge` over `awsvpc` for high task density.

**❗ Container instance draining — and don't confuse it with connection draining.** Before terminating an instance, set it to **`DRAINING`**: ECS stops placing new tasks there and gracefully relocates the running ones elsewhere, respecting the service's `minimumHealthyPercent`. Wire it to an **ASG lifecycle hook** on terminate so scale-in, AMI refreshes, and Spot interruptions don't just kill running tasks.

That is a *different layer* from **[ALB connection draining / deregistration delay](08-load-balancing-autoscaling.md#connection-draining--deregistration-delay)**, which is about letting in-flight HTTP requests finish. A graceful deploy needs **both**: container instance draining to move the tasks, and deregistration delay to let their requests complete.

**When to choose EC2 over Fargate:**
- **GPU** workloads, or specific host hardware
- **Sustained high utilisation** — cheaper per vCPU, especially with Savings Plans or Spot
- **Privileged containers**, custom `sysctls`/kernel parameters, or host device access
- **A per-host daemon** (one log/monitoring agent per instance rather than a sidecar per task)
- **Ephemeral storage beyond Fargate's 200 GB**, or specific EBS volume requirements
- **Per-host software licensing**, or existing Reserved Instances to absorb
- Very high task density, where Fargate's per-task premium accumulates

**What you take on in exchange** — state this honestly, because it's the actual trade: patching and rotating the ECS-optimised AMI, keeping the agent current, capacity headroom and bin-packing decisions, instance draining on scale-in, and cluster-level monitoring. Fargate's higher per-vCPU price is buying all of that away.

**ECS Anywhere** is the same control plane extended to **your own on-prem servers**, registered as container instances via the SSM agent — for hybrid or data-residency requirements. Note the limitation: external instances get no ELB integration and no `awsvpc` networking.

**The interview answer:** *"Fargate unless something forces EC2 — GPU, privileged containers, host daemons, or sustained utilisation where the per-vCPU price wins. If I'm on EC2 I'd use a capacity provider with managed scaling and termination protection rather than scaling the ASG myself, spread tasks across AZs, and make sure the instance role exists alongside the task and execution roles. The two details I'd verify are dynamic port mapping with the ephemeral range open to the ALB, and container instance draining wired to an ASG lifecycle hook so deploys and scale-in don't kill running tasks."*

**ECS vs EKS in one line:** ECS is simpler, AWS-proprietary, free control plane — pick it unless you have a real reason. EKS is managed **Kubernetes** — pick it for portability across clouds, an existing Kubernetes investment/team, or the CNCF ecosystem (Helm, operators, service meshes), and accept the added complexity and control-plane cost.

---

## 3. Fargate & Launch-Type Choice

### AWS Fargate

**Definition:** Serverless compute engine for containers — you supply a Docker image + CPU/memory; AWS handles the underlying servers, scaling, OS, and patching. Fargate is not an orchestrator itself — it's a launch type for **ECS** or **EKS**.

**Core concepts**
- **Task** — one running container (or co-located group). **Task Definition** — blueprint (image, CPU/memory, env vars, IAM role). **Service** — keeps N tasks running/healthy.
- Networking: `awsvpc` mode only — every task gets its own ENI (strong isolation, but IP exhaustion is a real capacity constraint in small subnets).
- No SSH/host access; task-level IAM role instead of instance role.

**Pricing:** pay for vCPU-seconds + GB-seconds only while the task runs — zero idle cost.

**Sizing is constrained, not free-form.** You pick from **valid vCPU/memory pairs**, not arbitrary numbers: 0.25 vCPU → 0.5/1/2 GB; 0.5 vCPU → 1–4 GB; 1 vCPU → 2–8 GB; 2 vCPU → 4–16 GB; 4 vCPU → 8–30 GB; and 8/16 vCPU tiers for larger workloads. Ask for a combination that isn't on the list and the task definition is rejected — a common first-time surprise. Memory scales in fixed steps per vCPU tier, so "I just need a bit more RAM" sometimes means paying for another vCPU.

**Ephemeral storage:** each task gets **20 GB by default**, configurable up to **200 GB** — and it's *ephemeral*, gone when the task stops. For anything that must persist or be shared between tasks, mount **[EFS](06-ec2-instance-storage.md#efs-elastic-file-system)**. This is also the constraint that bites large container images or build/scratch workloads.

**Fargate Spot:** the same interruption model as EC2 Spot — up to **~70% cheaper**, with a **2-minute `SIGTERM` warning** before reclamation. Ideal for batch jobs, CI runners, and queue consumers where SQS absorbs the interruption. A **capacity provider strategy** lets one service mix them, e.g. *"2 tasks always on `FARGATE`, everything above that on `FARGATE_SPOT`"* — baseline reliability with cheap burst. This also depends on your app handling `SIGTERM`, which is the exec-form `ENTRYPOINT` point from the Docker section.

**Also worth knowing:** Fargate **platform versions** control the underlying runtime (use `LATEST` unless pinning for a reason); there's **no privileged mode, no host-level daemons/DaemonSets, and no GPU support** — those requirements push you to the EC2 launch type; and Windows containers are supported but with a higher per-task floor.

**EC2 vs Fargate**
| Aspect | EC2 | Fargate |
|---|---|---|
| Server management | You | AWS |
| OS/SSH access | Yes | No |
| Scaling | ASG (minutes) | Automatic (faster) |
| Pricing | Instance-hour, idle cost | Per task, no idle cost |
| Security isolation | Shared host possible | Strong (dedicated ENI/kernel per task) |
| Best for | Steady/high utilization, legacy, GPU | Bursty/microservices, ops simplicity priority |

**Cost trap (interview favorite):** "Fargate is always cheaper" is **false**. Fargate wins for bursty/low-utilization workloads; EC2 wins for steady, high-utilization workloads because you're not paying the Fargate per-task premium on idle-free capacity you'd have used anyway. Numeric rule of thumb from the notes: a 24×7 1 vCPU/2GB service costs roughly $17–20/mo on EC2 (t3.small) vs $35–40/mo on Fargate; a job running 2 hrs/day at the same size flips to ~$20/mo (EC2, mostly idle) vs ~$6–8/mo (Fargate).

**Common false interview claims:** "Fargate is always cheaper", "Fargate replaces EC2", "You can SSH into Fargate", "Fargate has no networking limits" — all false.

### EC2 vs Fargate Cost & Trap Scenarios

**Sample EC2 trap Q&A**
- *CPU is low but app is slow* → bottleneck likely disk I/O, network latency, or a single-threaded hot path — CPU% alone is misleading.
- *Public IP changed after restart* → public IPs aren't static unless you attach an Elastic IP.
- *ASG didn't scale during a spike* → check scaling policy metric choice and cooldown period length.
- *Instance unreachable despite "running"* → check security group, NACL, route table, and whether it even has a public IP.
- *Spot instance terminated suddenly* → expected behavior; AWS can reclaim spot capacity anytime (2-minute warning via EventBridge).

**Sample Fargate trap Q&A**
- *Task stopped unexpectedly* → app crashed, failed health check, or hit its memory limit (OOM-killed).
- *"Healthy" task but service marks it unhealthy* → container health check failing despite the process being alive (e.g., wrong health check path/port).
- *Fargate task can't reach internet* → it's in a private subnet with no NAT Gateway/VPC endpoint.
- *Works in dev, fails in prod* → almost always IAM role, secrets, or networking (subnet/SG) differences between environments.

**Final mental model:** Steady load → EC2. Bursty load → Fargate. Need control → EC2. Need simplicity → Fargate. Idle-cost sensitive → Fargate. High, constant utilization → EC2.

---

## 4. Container Registry

### ECR (Elastic Container Registry)

AWS's managed private Docker registry — images stored in S3 under the hood, access controlled by **IAM** (plus repository policies for cross-account), and integrated with ECS, EKS, and **Lambda container images**.

```bash
aws ecr get-login-password --region us-east-1 | docker login --username AWS \
  --password-stdin 123456789012.dkr.ecr.us-east-1.amazonaws.com
docker tag myapi:1.0 123456789012.dkr.ecr.us-east-1.amazonaws.com/myapi:1.0
docker push          123456789012.dkr.ecr.us-east-1.amazonaws.com/myapi:1.0
```
Features worth naming as best practice:
- **Image scanning** — *basic* scans on push against the CVE database; **enhanced scanning** uses **Amazon Inspector** for continuous rescanning of both OS and language-package dependencies as new CVEs are published. Enhanced is the right answer for anything production.
- **Tag immutability** — prevents overwriting an existing tag. Turn it on: mutable tags mean `myapi:1.0` can silently become different bits, which destroys reproducibility and makes rollback unreliable. Deploy by **digest** or immutable version tags, never `:latest`.
- **Lifecycle policies** — expire untagged images and keep only the last N tagged ones. Without this, ECR quietly becomes one of your larger storage bills.
- **Cross-region/cross-account replication** — so a multi-region deployment doesn't pull across regions at scale.
- **Pull-through cache** — caches upstream public images (Docker Hub, ECR Public, MCR) in your registry, avoiding Docker Hub rate limits in CI. A very practical thing to mention.
- **ECR Public Gallery** for public images.

---

## 5. Choosing a Platform for .NET

### ECS vs EKS vs Fargate vs Lambda for .NET Workloads

The original notes cover Lambda-vs-ECS and EC2-vs-Fargate individually but never directly answer the very common senior .NET-on-AWS question: *"You're moving a .NET microservices platform to AWS — how do you choose the compute layer?"*

| Criterion | Lambda | ECS (Fargate) | EKS (Fargate or managed nodes) | EC2 (self-managed) |
|---|---|---|---|---|
| .NET fit | Great for event handlers, APIs with ASP.NET Core minimal hosting via `Amazon.Lambda.AspNetCoreServer`, but cold starts hurt latency-sensitive sync APIs unless AOT + Provisioned Concurrency | Great — standard container deployment, no code changes, full ASP.NET Core hosting model | Same as ECS but adds Kubernetes complexity — only worth it if you already run K8s elsewhere (multi-cloud, existing manifests, team expertise) | Full control, good for Windows containers/.NET Framework (legacy) workloads needing IIS |
| Operational overhead | Lowest | Low (no cluster to manage) | Highest (control plane concepts, CRDs, Helm, networking) | Highest (patching, scaling, OS) |
| Team skill fit | Any .NET team | Any .NET/DevOps team | Needs existing K8s expertise | Needs sysadmin/infra expertise |
| Cost at steady state | Poor (per-invocation billing adds up) | Good | Good | Best (if fully utilized) |
| .NET Framework (not Core) support | No (Lambda requires .NET Core/5+) | Yes, via Windows containers on EC2 launch type (not Fargate) | Yes, Windows node groups | Yes |
| Startup-latency-sensitive sync API | Risky without Provisioned Concurrency + AOT | Best default choice | Best default choice | Best default choice |
| Best use case | Webhooks, S3/SQS/DynamoDB Stream processors, cron, glue | Line-of-business APIs, internal microservices | Only if already multi-cloud/K8s-standardized | Legacy .NET Framework/IIS lift-and-shift |

**Senior-level recommendation pattern:** default new .NET microservices to **ECS on Fargate** (best balance of operational simplicity and control for a typical .NET shop). Use **Lambda** for event-driven glue and spiky/idle-heavy workloads. Reach for **EKS** only when there's an existing organizational Kubernetes investment — introducing K8s purely for a .NET-on-AWS migration is usually over-engineering. Use **EC2** (with Windows containers or full Windows Server) only for .NET Framework workloads that can't be ported to .NET Core/8+, or for workloads needing GPU/specialized hardware.

**Interviewer follow-up to expect:** "Your team knows only .NET/C#, no Kubernetes — would you still pick EKS for a green-field microservices platform?" Correct senior answer: no — pick ECS/Fargate unless there's a concrete multi-cloud or portability requirement that justifies the K8s learning curve and operational tax.

### Fargate/ECS/EKS Trade-offs — Reasoning Without Hands-On Time

Honesty framing up front, since this is worth stating explicitly in an interview: my hands-on AWS provisioning experience (via Terraform/CDKTF) is Lambda, DynamoDB, EC2, and S3 — I haven't personally run Fargate, ECS, or EKS in production. What follows is how I'd reason through the trade-offs if asked to make this decision, not a claim of direct operational experience with the container orchestrators themselves.

**Expanded comparison — operational overhead, cost model, cold start, and use-case fit**

| Dimension | EC2 (self-managed) | ECS on Fargate | EKS (managed control plane) | Lambda |
|---|---|---|---|---|
| Who patches the OS/kernel | You | AWS | AWS (nodes) or you (self-managed node groups) | AWS (fully abstracted) |
| Who manages the orchestrator/control plane | N/A (no orchestrator) or you (self-hosted) | AWS (ECS control plane is free, always managed) | AWS manages the control plane (charged hourly per cluster), but you still manage node groups unless using Fargate profiles | N/A |
| Cost model | Instance-hour, regardless of load | Per-task vCPU/GB-second, while running | Cluster fee + node/Fargate task cost | Per-invocation + duration |
| Cold start behavior | None once running; ASG scale-out takes minutes (boot OS, attach storage, register) | Task startup is seconds (pull image, start container) — no "cold start" in the Lambda sense, but not instant either | Similar to Fargate for Fargate-backed pods; for managed EC2 node groups, bounded by node/ASG scale-out time like raw EC2 | True cold start on first/scaled-out invocation (ms–low seconds), mitigated by Provisioned Concurrency |
| Operational overhead | Highest — patching, scaling policy tuning, capacity planning | Low — no servers, no cluster; you manage task definitions/services | Highest of the container options — cluster upgrades, CRDs, networking plugins (CNI), Helm charts, IAM-to-Kubernetes-RBAC mapping (IRSA) | Lowest — no infrastructure concept at all |
| Best use-case fit | Steady, high utilization; OS-level requirements; legacy/stateful; GPU | Standard containerized microservices with no existing K8s investment | Organizations already standardized on Kubernetes (often multi-cloud, or migrating from on-prem K8s) | Event-driven, spiky, short-lived work |
| Multi-cloud portability | Low (AWS-specific tooling even if OS is portable) | Low (ECS is AWS-proprietary) | High (Kubernetes API is portable across clouds/on-prem) | Lowest (heavily AWS-event-model-coupled) |

**How I'd frame the trade-off conversation if asked "why not EKS for everything, since Kubernetes is the industry standard":**

Kubernetes' biggest selling point — portability and a rich ecosystem (Helm, operators, service mesh) — is also its biggest cost: a real control-plane learning curve (CRDs, RBAC, networking/CNI, admission controllers) that a pure ECS or Fargate user never has to pay. For a .NET shop without existing Kubernetes investment, that operational tax usually isn't justified unless there's a concrete multi-cloud requirement or the org already has platform engineers who live in Kubernetes daily. I'd frame my recommendation the same way AWS itself frames the ECS-vs-EKS choice: ECS if you want AWS-native simplicity and don't need portability; EKS if you need Kubernetes-API compatibility for tooling, multi-cloud strategy, or existing team expertise.

**A decision flow I'd talk through out loud in an interview:**

```
   Existing Kubernetes investment or multi-cloud requirement?
                              |
              +---------------+---------------+
             yes                              no
              |                               |
              v                               v
            EKS            Need OS-level access, GPU,
                           or a legacy/stateful app?
                                          |
                          +---------------+---------------+
                         yes                              no
                          |                               |
                          v                               v
                        EC2          Spiky / event-driven, short-lived,
                                     idle-heavy?
                                                    |
                                    +---------------+---------------+
                                   yes                             no
                                    |                    (steady containerised
                                    v                       service)
                                 LAMBDA                          |
                                                                 v
                                                         ECS on FARGATE
```

**What I'd want to learn hands-on before claiming deep Fargate/ECS/EKS expertise:** task definition/service tuning under real load (deployment circuit breakers, min/max healthy percent during rolling deploys), service discovery (Cloud Map/App Mesh), and — for EKS specifically — IRSA (IAM Roles for Service Accounts) mapping and cluster upgrade mechanics. Naming this gap directly, rather than overstating familiarity, is itself the senior-level move here.

### Deploying .NET to AWS: Elastic Beanstalk vs ECS vs Lambda Custom Runtime

The original notes never directly discuss Elastic Beanstalk, despite it being a common AWS Certified/senior-interview topic and a legitimate, low-effort .NET deployment path.

| Option | What it is | Pros | Cons | When to use |
|---|---|---|---|---|
| **Elastic Beanstalk** | PaaS wrapper around EC2/ASG/ELB/RDS with a managed platform (incl. .NET on Windows/Linux) | Fastest path from `dotnet publish` to a running, load-balanced, auto-scaled app; AWS manages the underlying EC2/ASG/ELB stack; supports blue/green via environment swap | Less control than raw ECS; platform upgrades can be disruptive; "PaaS lock-in" feel; scaling granularity coarser than ECS | Small-to-mid teams wanting managed infra without container/K8s investment; quick MVPs; teams new to AWS |
| **ECS (Fargate or EC2 launch type)** | Container orchestration, AWS-native | Full control over container spec, fine-grained scaling, no idle cost (Fargate), integrates cleanly with CodePipeline/CodeBuild | Requires Docker packaging discipline, task definition management | Default choice for most modern .NET microservices |
| **Lambda (ASP.NET Core minimal API hosting or custom runtime)** | Serverless — package as zip or container image | No infra at all, scales to zero, cheap for spiky traffic | Cold starts, 15-min limit, harder local debugging parity, connection pooling nuances (RDS Proxy often needed) | Event-driven APIs, low/spiky traffic, backend-for-frontend functions |

**Nuance interviewers probe for:** Elastic Beanstalk is *not* a separate compute primitive — under the hood it still provisions EC2 + ASG + ELB (or ECS, for the Docker platform). The value-add is the deployment/orchestration tooling (`eb deploy`, environment configs, rolling/immutable/blue-green deployment policies), not a new runtime. Knowing this distinction (Beanstalk = orchestration layer, not new infrastructure) is what separates a mid-level from a senior answer.

**Deployment strategy comparison (all three support some form of zero/low-downtime deploy):**
- Elastic Beanstalk: rolling, rolling-with-additional-batch, immutable, or blue/green (swap CNAME between environments).
- ECS: rolling update via service deployment configuration, or blue/green via CodeDeploy + two target groups.
- Lambda: versions + aliases, with linear/canary traffic shifting via CodeDeploy.

**Disaster Recovery — Containers (ECS / ECR / Fargate)**

| | |
|---|---|
| **What's actually at risk** | Container images in ECR, task definitions, service configuration, and any EFS-backed state |
| **Backup mechanism** | **ECR cross-region/cross-account replication rules**, **immutable tags**, task definitions (AWS versions these for you) + IaC, **AWS Backup** for EFS |
| **Realistic RPO / RTO** | Images/config RPO ~0. RTO minutes — register the task definition and create the service |

**Recovery runbook:**
1. **Bad release:** roll back by updating the service to the **previous task definition revision** — `aws ecs update-service --task-definition my-app:41`. Revisions are immutable, which is what makes this safe.
2. **Regional failure:** confirm the image is in the DR region's ECR (via a replication rule set up in advance), register the task def there, create the service against the DR cluster and target group.
3. **EFS state:** restore from an AWS Backup recovery point into a new file system and update the volume configuration.
4. Scale the DR service up **before** shifting traffic — Fargate task startup plus ALB target registration is minutes, and shifting DNS first just serves errors.

⚠️ **The gotcha:** **a task definition pins the image by full URI, including the registry's account and region.** Copy that task def to another region and it still points at the *original* ECR — so on a real regional outage it cannot pull, and the service fails to start with an error that looks like a permissions problem. Set up ECR replication **and** parameterise the image URI per region. And never deploy `:latest` — without immutable tags or a digest, "roll back to the previous revision" doesn't actually change the image.

---

## 6. Hands-On

### Hands-On: ECS with Fargate

```bash
# 1. Registry + image
aws ecr create-repository --repository-name myapi --image-tag-mutability IMMUTABLE \
  --image-scanning-configuration scanOnPush=true
# (build, tag, push as above)

# 2. Cluster
aws ecs create-cluster --cluster-name prod-cluster

# 3. Task definition (family + the two roles + awsvpc + awslogs)
aws ecs register-task-definition --cli-input-json file://taskdef.json

# 4. Service behind an ALB target group, across 3 AZs
aws ecs create-service --cluster prod-cluster --service-name myapi-svc \
  --task-definition myapi:1 --desired-count 2 --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-a,subnet-b,subnet-c],securityGroups=[sg-app],assignPublicIp=DISABLED}" \
  --load-balancers targetGroupArn=arn:...,containerName=myapi,containerPort=8080 \
  --deployment-configuration "deploymentCircuitBreaker={enable=true,rollback=true}"

# 5. Verify / debug
aws ecs describe-services --cluster prod-cluster --services myapi-svc \
  --query 'services[0].{running:runningCount,desired:desiredCount,events:events[0:3]}'
aws ecs execute-command --cluster prod-cluster --task <id> --container myapi \
  --interactive --command "/bin/sh"      # ECS Exec — shell into a running task
```
**Debugging order when tasks won't start** (a good practical answer): `describe-services` **events** first, then the **stopped task's `stoppedReason`**. In practice it's almost always one of — the **task execution role** can't pull from ECR or write logs, the image architecture doesn't match (an ARM image on x86 or vice versa), the **health check** fails before the app finishes starting (raise the ALB health-check grace period), no route to ECR from a private subnet (needs a NAT gateway or **VPC endpoints for ECR + S3**), or the container exits immediately because of a missing environment variable.

---

← [Load Balancing, Scalability & Auto Scaling](08-load-balancing-autoscaling.md) · [Index](README.md) · [Relational Databases, Caching & Analytics](10-databases-caching-analytics.md) →
