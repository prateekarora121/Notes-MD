> **AWS Detailed Guide** · [Index](README.md) · Part II

# Containers: Docker, ECS, ECR & Fargate

> **Tier 2 — reason about, be honest about hands-on.** Docker main use karta hoon; ECS/Fargate ke saath main design kar sakta hoon lekin production mein operate nahi kiya. Honest framing [Fargate/ECS/EKS Trade-offs](#fargateecseks-trade-offs--hands-on-time-ke-bina-reasoning) mein hai — bluff karne ke bajaye usko use karo.

---

## 1. Docker & Container Fundamentals

### Docker & Container Fundamentals

**Container kya hai:** tumhari application plus iski dependencies ek immutable artifact mein packaged jo har jagah identically chalti hai. VM ke unlike yeh apna OS boot karne ke bajaye **host ka kernel share karta hai**.

| | Virtual Machine | Container |
|---|---|---|
| Isolation | Full OS + own kernel, hypervisor ke through | Process-level, **host kernel share karta hai** |
| Size | GBs | MBs |
| Start time | Minutes | **Seconds ya usse kam** |
| Density per host | Low | High |
| Trade-off | Stronger isolation | Kaafi cheaper aur faster, weaker kernel-level isolation |

**Image vs container:** ek **image** immutable template hai; ek **container** iska ek running instance hai. (Class-vs-object analogy yahan achhe se fit hoti hai.) Images ek **Dockerfile** se **layers** mein build hoti hain, aur layers cached hote hain — isliye instruction order build speed ke liye bahut matter karta hai.

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
Iske baare mein teen points banane hain: **multi-stage builds** SDK ko shipped image se bahar rakhte hain (hundreds of MB save hote hain); **`.csproj` ko source se pehle copy karna** matlab `dotnet restore` cached hota hai aur sirf tab re-run hota hai jab dependencies change hoti hain; aur **chiselled/Alpine** runtime variants plus **Native AOT** isko aur shrink karte hain, jo cold starts aur ECR storage ke liye matter karta hai.

```bash
docker build -t myapi:1.0 .
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development myapi:1.0
docker ps / docker logs <id> / docker exec -it <id> sh
```
**Yeh interview topic kyun hai:** container woh *immutable artifact* hai jo tum dev se prod tak unchanged promote karte ho — same bits jo CI se pass hui woh hi bits production mein chalti hain. Yeh actually "works on my machine" ko kill karta hai, aur isi wajah se config environment se aata hai (env vars, Parameter Store, Secrets Manager) image mein bake hone ke bajaye.

#### Docker Images — Woh Detail Jo Jaanna Zaruri Hai

**Pehle vocabulary straight karo**, kyunki interviewers isko interchangeably use karte hain aur precision achha lagta hai:
```
REGISTRY            ECR, Docker Hub                       — the server that stores repositories
 └─ REPOSITORY      123456789012.dkr.ecr…/myapi           — all versions of one image
     └─ IMAGE       myapi:1.4.2   (tag)                   — a mutable, human-friendly pointer
     └─ IMAGE       myapi@sha256:9f2a…  (digest)          — the immutable content hash
         └─ CONTAINER                                     — a running instance of that image
```
**Tag vs digest wahi hai jo operationally matter karta hai.** Ek **tag ek movable label hai** — `myapi:1.4.2` kal different bits par repoint ho sakta hai. Ek **digest content ka ek cryptographic hash hai** aur kabhi kuch aur mean nahi kar sakta. Isliye reproducibility aur reliable rollback ke liye **digest** (ya ek immutable tag) se deploy karo; `:latest` anti-pattern hai, kyunki "roll back to the previous latest" jaisi koi chiz exist nahi karti. Isi liye **[ECR tag immutability](#ecr-elastic-container-registry)** on karna worth hai.

**Layers, properly.** Har Dockerfile instruction ek **read-only layer** create karta hai, aur image woh layers hain jo ek union filesystem se stack ki gayi hain. Ek running container upar ek thin **writable layer** add karta hai — isi liye **container filesystem changes uske replace hone par vanish ho jaate hain** (state ko volume, EFS, S3, ya ek database mein jaana chahiye).

Do consequences jo puchi jaati hain:
- **Layers shared aur cached hote hain.** Same image se dus containers dus copies store nahi karte; aur ek naya version pull karne se sirf jo layers change huyi woh download hoti hain. Isi liye instruction order matter karta hai — jo cheezein *sabse kam* change hoti hain (base image, dependency restore) unhe unn cheezon **se pehle** rakho jo *sabse zyada* change hoti hain (tumhara source), exactly jaisa `.csproj`-before-source trick upar karta hai.
- **❗ Baad ke layer mein file delete karna image ko shrink nahi karta**, aur jo bhi ek earlier layer mein present hai woh **abhi bhi extractable** hai. Isliye `COPY secrets.json . && RUN rm secrets.json` secret ko permanently image mein leave kar deta hai. Build mein kabhi credentials mat daalo; build secrets use karo ya runtime par inject karo.

**Base image choice — ek real decision, koi detail nahi:**
| Variant | Size (approx.) | Trade-off |
|---|---|---|
| `mcr.microsoft.com/dotnet/aspnet:8.0` | ~220 MB | Full Debian; ek shell aur package manager hai — debug karna sabse easy |
| `…aspnet:8.0-alpine` | ~110 MB | glibc ke bajaye musl libc — small, lekin native-dependency issues ke liye dhyan rakho |
| **`…aspnet:8.0-jammy-chiseled`** | **~110 MB** | **Ubuntu chiselled — no shell, no package manager, non-root by default.** Bahut smaller attack surface. Production .NET ke liye strong default |
| `…runtime-deps:8.0-jammy-chiseled` + **Native AOT** | **~15–30 MB** | Self-contained single binary, fastest startup. Lambda container images aur cold-start-sensitive work ke liye best |

**"No shell" ek feature hai, limitation nahi** — jo attacker RCE achieve kare uske paas pivot karne ke liye koi shell nahi hoga. Cost yeh hai ki `docker exec … sh` ab kaam nahi karta, isliye tum logs aur metrics se debug karte ho. Yeh trade-off naam se batana senior version ka answer hai.

**`.dockerignore` — chhota file, do real problems solve karta hai.** Build context mein sab kuch daemon ko bheja jaata hai, isliye iske bina tum har build par `bin/`, `obj/`, `node_modules/`, aur `.git` upload karte ho (slow), aur risk hota hai `COPY . .` **`appsettings.Development.json`, `.env`, ya `.aws/credentials`** ko image mein bake karne ka (ek genuine secret leak):
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

**`ENTRYPOINT` vs `CMD`** — upar Dockerfile mein use hua aur explain karna worth hai:
- **`ENTRYPOINT`** = woh executable jo hamesha chalta hai. Override karna hard hai (`--entrypoint` chahiye).
- **`CMD`** = default *arguments*, jo `docker run` ke aage kuch bhi append karke trivially override ho jaate hain.
```dockerfile
ENTRYPOINT ["dotnet", "MyApi.dll"]     # always runs this
CMD ["--environment=Production"]       # default arg; `docker run img --environment=Staging` overrides it
```
Rule: `ENTRYPOINT` iske liye ki *container kya hai*, `CMD` iske liye ki *yeh default kaise configure hota hai*. Bhi **exec form** (`["dotnet","MyApi.dll"]`) ko **shell form** se prefer karo — shell form tumhare process ko `/bin/sh -c` mein wrap karta hai, isliye **tumhari app `SIGTERM` kabhi receive nahi karti** aur gracefully shutdown karne ke bajaye kill ho jaati hai. ECS/Kubernetes mein yeh har deploy ko dropped in-flight requests bana deta hai.

Related: **`ARG` vs `ENV`.** `ARG` sirf build time par exist karta hai; `ENV` running container mein persist hota hai (aur `docker inspect` se visible hota hai — isliye secrets ke liye nahi).

**❗ Multi-arch images aur Graviton.** Ek x86 laptop par build ki gayi image ek ARM64 Graviton instance par **nahi chalegi** — task simply `exec format error` ke saath start hone mein fail hota hai, jo pehli baar genuinely confusing hota hai. Guide Graviton ko ~20% .NET saving ke liye recommend karta hai (dekho [EC2 Instance Types](06-ec2-instance-storage.md#ec2-instance-types-user-data--metadata)), isliye tumhe target ke liye build karna padta hai:
```bash
docker buildx build --platform linux/amd64,linux/arm64 -t <ecr>/myapi:1.4.2 --push .
```
Yeh ek **manifest list** push karta hai — ek tag jo dono architectures serve karta hai, har host sahi wala pull karta hai. Yeh Graviton cost saving ka practical prerequisite hai, aur yeh us kism ki detail hai jo dikhata hai ki tumne actually containers ship kiye hain.

**Local development** ke liye **Docker Compose** use hota hai jo app aur uski dependencies saath chalata hai (`docker compose up`) — ek Postgres aur ek Redis container tumhari API ke saath, taaki naye developer ko koi local installs na chahiye. Jaanne layak: Compose ek **local/dev tool** hai: AWS mein equivalent responsibilities ek **ECS task definition** (multi-container) aur real managed services (RDS, ElastiCache) ki hoti hain. Yeh bhi note karo ki ek **container ek ephemeral layer mein likhta hai**, isliye local persistence ke liye ek **volume** chahiye (`-v ./data:/data`) — aur ECS mein equivalent ek **EFS volume** hai jo task mein mount hota hai.

**Image hygiene, ek checklist ke roop mein:** multi-stage build · chiselled/AOT base · non-root `USER` · pinned base-image tag ya digest (na `:latest`) · `.dockerignore` · koi layer mein secrets nahi · push par scanned ([ECR enhanced scanning](#ecr-elastic-container-registry)) · ek `HEALTHCHECK` ya ek ALB-checked `/health` endpoint · exec-form `ENTRYPOINT` taaki `SIGTERM` app tak pahunche.

---

## 2. ECS

### ECS (Elastic Container Service)

> **Bridge vs `awsvpc` mode ka asli farak kahan padta hai:** bridge mode mein task ka ENI nahi hota, isliye *instance* ka SG hi saare database traffic ka source hai — [ECS → RDS: Database Connection ki Chaar Layers](10-databases-caching-analytics.md#ecs--rds-database-connection-ki-chaar-layers).

AWS ka **apna** container orchestrator — Kubernetes se simpler, IAM/ALB/CloudWatch ke saath deeply integrated, aur koi control-plane cost nahi.

**Object model, outermost se in tak:**
```
Cluster  →  Service  →  Task  →  Container(s)
              ↑          ↑
        desired count   instance of a Task Definition
```
- **Task Definition** — JSON blueprint: image, CPU/memory, port mappings, environment variables, secrets (Secrets Manager/Parameter Store se), log configuration, aur **do IAM roles**. Yeh versioned hai; har change ek naya revision create karta hai.
- **Task** — ek task definition ka running instance (ek ya zyada co-located containers).
- **Service** — N tasks ko running aur healthy rakhta hai, failures replace karta hai, aur unko ek ALB target group ke saath register/deregister karta hai.
- **Cluster** — logical grouping aur, EC2 launch type ke liye, instances ka pool.

**❗ Do roles — sabse common real-world ECS mistake:**
| Role | Kisse use hota hai | Kaunsi permissions chahiye |
|---|---|---|
| **Task execution role** | ECS agent, tumhare code chalne se *pehle* | ECR se image pull karna, CloudWatch Logs likhna, task definition mein referenced secrets fetch karna |
| **Task role** | **Tumhara application code** | S3, DynamoDB, SQS — jo bhi app actually call kare |

Tumhare app ki DynamoDB permission ko *execution* role mein daalna ek `AccessDenied` produce karta hai jo inexplicable lagta hai. Dekho [IAM Roles](03-iam-security.md#iam-roles-policies-assumerole).

**Launch types:**
- **Fargate** — serverless; tum CPU/memory specify karte ho aur AWS usko chalata hai. Koi instances patch/scale karne ko nahi. Higher per-vCPU price, no idle waste. Default choice.
- **EC2** — tum instances chalate aur patch karte ho (ECS agent ke saath) aur unpar tasks pack karte ho. High, steady utilisation par cheaper, aur GPU, Windows-specific host needs, ya host-level daemons ke liye required. **Capacity Providers** (managed scaling ke saath) cluster ko EC2 capacity add/remove karne dete hain jaise tasks demand karein.

**Networking modes:** **`awsvpc`** har task ko **apna ENI, private IP, aur security group** deta hai — yeh **Fargate ke liye mandatory** hai aur generally right choice hai, kyunki iska matlab hai tum per-service security-group rules likh sakte ho. `bridge` aur `host` older EC2-launch-type modes hain (host instance ka network stack share karta hai; bridge ports map karta hai aur dynamic port registration complicate karta hai).

**Scaling and deployment:**
- **Service Auto Scaling** — `ECSServiceAverageCPUUtilization`, memory, ya (best) **`ALBRequestCountPerTarget`** par target tracking; same "demand-correlated metric par scale karo" principle jo [ASGs](08-load-balancing-autoscaling.md#auto-scaling-groups-asg) mein hai.
- **Rolling update** `minimumHealthyPercent` / `maximumPercent` ke saath control karta hai ki deploy ke dauran kitne tasks down ya extra ho sakte hain.
- **Deployment circuit breaker** — automatically ek deployment ko rollback karta hai jiske tasks stabilise nahi ho paate. Isko on karo; yeh ek failed deploy aur ek outage ke beech ka difference hai.
- **Blue/green via CodeDeploy** — do target groups ke beech traffic shift karta hai canary/linear options ke saath aur CloudWatch alarms par automatic rollback.
- **Service discovery** **Cloud Map** (services ke liye DNS names) ya service-to-service traffic ke liye **Service Connect** ke through.
- **Persistent/shared storage** **EFS** volumes ke through jo tasks mein mount hote hain (dekho [EFS](06-ec2-instance-storage.md#efs-elastic-file-system)).

**Sidecar containers.** Ek task definition **kayi containers hold kar sakta hai jo task ki network namespace aur lifecycle share karte hain** — isliye yeh ek dusre ko `localhost` par reach kar sakte hain aur saath start/stop hote hain. Yehi sidecar pattern hai, aur standard AWS examples hain **FireLens/Fluent Bit** log router, **AWS X-Ray** daemon, App Mesh/Service Connect ke liye ek **Envoy** proxy, aur **OpenTelemetry Collector**. Ordering control karne ke liye `dependsOn` aur `essential` use karo — `essential: true` matlab whole task stop ho jaata hai agar woh container die kare, jo tum apne app container ke liye chahte ho aur *nahi* ek best-effort log shipper ke liye. Interview point: sidecars cross-cutting concerns (logging, tracing, mTLS) ko tumhari application image se bahar rakhte hain.

**Task placement (sirf EC2 launch type — Fargate isko tumhare liye handle karta hai):**
- **Strategies:** `binpack` (tasks ko fewest instances par tightly pack karo — cheapest, cost ke liye best), `spread` (AZs ya instances ke across distribute karo — most resilient, aur `spread` `attribute:ecs.availability-zone` par usual production default hai), `random`.
- **Constraints:** `distinctInstance` (kabhi bhi ek host par do na ho) aur `memberOf` ek expression ke saath (jaise, sirf GPU ya Graviton instances).

Cost versus resilience woh trade-off hai jo naam se batana hai: `binpack` instance count aur isliye spend minimise karta hai; `spread` ek host ya ek AZ khone se survive karta hai. Zyada teams AZs ke across spread karte hain aur unke andar binpack karte hain.

#### EC2 Launch Type par ECS

**Fargate ke versus kya change hota hai:** tum servers khud own karte ho. Cluster ek **container instances** ka pool ban jaata hai — EC2 instances jo **ECS container agent** chala rahe hain, cluster mein registered. ECS tasks unpar schedule karta hai; tum unhe alive, patched, aur correctly sized rakhte ho.

**❗ Yahan teen IAM roles hain, do nahi.** Upar wala doc do *task* roles cover karta hai; EC2 launch type ek teesra add karta hai, aur isko forget karna classic "my instances never show up in the cluster" failure hai:

| Role | Kisse attached | Kisse use hota hai | Kaunsi permissions chahiye |
|---|---|---|---|
| **ECS instance role** (`ecsInstanceRole`, policy `AmazonEC2ContainerServiceforEC2Role`) | **EC2 instance profile** | Host par **ECS agent** | Instance ko cluster ke saath register karna, ECR se pull karna, logs likhna |
| **Task execution role** | Task definition | ECS, task ke *taraf se* | ECR pull, CloudWatch Logs, secrets fetch karna |
| **Task role** | Task definition | **Tumhara application code** | S3, DynamoDB, SQS — jo bhi app call kare |

Fargate ke paas sirf baad wale do hain, kyunki tumhare liye koi instance own karne wala nahi hai.

**Instances ko cluster mein laana.** **ECS-optimised AMI** use karo (ECS agent + container runtime pre-installed), ya agent khud install karo. Phir woh ek line jo log bhool jaate hain:
```bash
#!/bin/bash
echo "ECS_CLUSTER=prod-cluster" >> /etc/ecs/ecs.config
```
Isko omit karo aur instance fine launch hota hai, khud ko `default` naam ke cluster mein register kar leta hai, aur tumhara actual cluster **0 container instances** dikhata hai jabki EC2 mein sab healthy dikh raha hai.

**Capacity Providers + Cluster Auto Scaling** — pool ko size karne ka right way. Ek capacity provider ek **ASG** ko wrap karta hai; **managed scaling** enabled hone par, ECS ek `CapacityProviderReservation` metric publish karta hai aur usko target-track karta hai, isliye ASG grow karta hai jab tasks place nahi ho pate aur shrink karta hai jab capacity idle hai.
- **Target capacity %** — `100` matlab "scale so tasks just fit" (cheapest, naye tasks place karne mein slowest); 100 se kam warm headroom rakhta hai taaki tasks immediately start hon.
- **❗ Managed termination protection on hona chahiye**, warna ASG happily ek instance terminate kar dega jispar abhi bhi running tasks hain.
- Providers ko **base and weight** se mix karo — jaise, On-Demand par base of 2 tasks, uske upar sab kuch **EC2 Spot** par.

#### EC2, ASG, Capacity Provider, Cluster, Service & Tasks Kaise Fit Hote Hain

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

**Isko ownership ke roop mein padhna — kisko kis baare mein pata hai:**
| Object | Yeh kya hai | Isko kya pata hai |
|---|---|---|
| **ECS Cluster** | Ek logical boundary jo services aur registered instances hold karta hai | EC2 ya ASGs ke baare mein **directly** kuch nahi |
| **Container instance** | Ek EC2 instance jo ECS agent chala raha hai, cluster mein registered (1 EC2 instance = 1 container instance) | Yeh kaunse cluster mein join hua |
| **ASG** | EC2 lifecycle ko owns karta hai — instances launch aur terminate karta hai | **ECS, tasks, ya containers ke baare mein kuch nahi** |
| **Capacity Provider** | **The bridge.** Exactly ek ASG ko wrap karta hai aur cluster se attached hai | Dono worlds — yeh *sirf* object hai jo ECS ko ASG ko influence karne deta hai |
| **Service** | N tasks running rakhta hai; ek **capacity provider strategy** se capacity select karta hai | Task count aur kaunse provider(s) use karna hai |
| **Task** | Running container(s) | Woh instance jahan yeh place hui |

**❗ Woh insight jiske liye diagram exist karta hai: do independent scaling loops hain.**

| | **Service Auto Scaling** (Loop 1) | **Cluster Auto Scaling** (Loop 2) |
|---|---|---|
| Scale karta hai | **Tasks** (`desiredCount`) | **EC2 instances** (ASG desired capacity) |
| Driven by | Application demand — `ALBRequestCountPerTarget`, CPU, SQS backlog | **Task placement pressure** — tasks jo fit nahi ho pate |
| Mechanism | Application Auto Scaling target tracking | Capacity provider **managed scaling** |
| Miss karna matlab | Traffic badhta hai lekin task count flat rehta hai | Tasks kahi place karne ki jagah nahi milti, forever **`PENDING`** rehte hain |

Yeh complementary hain: **Loop 1 decide karta hai tumhe kitne tasks chahiye; Loop 2 confirm karta hai unko rakhne ke liye kahi jagah hai.** Instances scale kiye bina tasks scale karna sirf pending tasks produce karta hai; tasks scale kiye bina instances scale karna sirf ek idle bill produce karta hai.

**Fargate par, Loop 2 exist hi nahi karta** — AWS capacity supply karta hai, isliye koi ASG nahi, usko wrap karne wala koi capacity provider nahi, aur koi instance registration nahi. Yehi absence hai jo Fargate ka per-vCPU premium buy karta hai.

**Reservation metric actually kaise kaam karta hai** — ek number Loop 2 drive karta hai:
```
CapacityProviderReservation = (M / N) x 100

  N = instances currently running in the ASG
  M = instances ECS calculates it NEEDS for running + pending tasks
```
- **M > N** → metric 100 se upar → tasks fit nahi ho pate → ASG scale **out** karta hai
- **M < N** → metric 100 se neeche → spare capacity → ASG scale **in** karta hai
- **Target capacity `100`** matlab "exactly enough ke liye aim karo" — cheapest, lekin naya task ek instance boot hone ka wait karta hai. Isko **~80** set karo taaki roughly 20% warm headroom rahe aur tasks immediately start hon.

**Failure modes, aur kaunsa object fault mein hai:**
| Symptom | Kaunsa object |
|---|---|
| Tasks stuck `PENDING`, ASG kabhi grow nahi hota | **Koi capacity provider nahi**, ya managed scaling off — ECS ke paas instances *ask* karne ka koi tareeka nahi |
| Scale-in running tasks wale instances terminate kar deta hai | **Managed termination protection off** |
| Instances fine launch hote hain lekin cluster zero dikhata hai | **Agent** — user data se `ECS_CLUSTER=` missing, isliye `default` mein register hua |
| ASG max par hai, tasks abhi bhi pending hain | **ASG `max`** — ceiling tumhari raise karne ki hai, ECS usko exceed nahi karega |
| Burst ke dauran capacity bahut slowly aati hai | **Target capacity 100 par** — koi headroom nahi. Isko lower karo, ya ASG warm pools use karo |

**One-liner:** *"ASG instances owns karta hai, service tasks owns karta hai, aur capacity provider hi sirf woh chiz hai jo dono ko connect karti hai — isi liye ECS bina uske tumhare EC2 fleet ko scale nahi kar sakta, aur Fargate ko iski koi zarurat nahi."*

**Resource allocation — jo numbers ECS actually schedule karta hai unpar:**
- **CPU "CPU units" mein hai: 1024 units = 1 vCPU.**
- Memory ke do settings hain: **`memory` ek hard limit hai** (exceed karo aur container OOM-killed ho jaata hai) aur **`memoryReservation` ek soft limit hai** (guaranteed amount jo scheduling ke liye use hota hai; container usse upar burst kar sakta hai agar host mein room ho). Best practice yeh hai ki scheduling ke liye soft limit set karo aur safety ceiling ke roop mein hard limit.
- ECS ek task sirf wahan place karta hai jaha **remaining** CPU *aur* memory fit ho, jo single sabse common EC2-launch-type error produce karta hai:

> `unable to place a task because no container instance met all of its requirements`

Causes, worth checking wale order mein: kisi instance par not enough remaining CPU/memory · ek **host port conflict** · koi instance jo placement constraint satisfy na kare (dekho [task placement](#ecs-elastic-container-service) upar) · `awsvpc` mode mein per-instance **ENI limit** reach ho gayi.

**Networking modes — ek choice jo tumhe sirf EC2 par milta hai:**
| Mode | Behaviour |
|---|---|
| **`bridge`** | Docker ka default — container ports host ports mein mapped. Dynamic port mapping enable karta hai (neeche) |
| **`host`** | Container host ka network stack directly share karta hai. Best performance, no mapping, lekin **port conflicts** aur koi per-task security group nahi |
| **`awsvpc`** | Har task ko apna ENI, private IP, aur security group milta hai (Fargate jaisa). Cleanest security model, lekin **ENI-per-instance limit se capped** — ENI trunking se isko raise karo |
| `none` | Koi external networking nahi |

**❗ Dynamic port mapping — classic EC2-launch-type question.** `bridge` mode mein, **`hostPort: 0`** set karo (ya omit karo) aur Docker ek random ephemeral host port assign karta hai; **ECS phir automatically us specific port ko ALB target group ke saath register karta hai**. Yeh isi liye hai ki tum ek instance par ek hi container ki **multiple copies** chala sakte ho — ek static `hostPort` ke saath second task place nahi ho sakta, kyunki port already liya gaya hai.

Catch: instance ka security group ALB ke security group se **ephemeral range 32768–65535** allow karna chahiye, sirf port 80 nahi. Yehi answer hai *"tum ek EC2 host par ALB ke peeche ek service ke teen replicas kaise chalate ho?"* ka — aur yeh `awsvpc` ke over `bridge` ka genuine advantage hai high task density ke liye.

**❗ Container instance draining — aur isko connection draining se confuse mat karo.** Ek instance terminate karne se pehle, usko **`DRAINING`** set karo: ECS wahan naye tasks place karna stop kar deta hai aur gracefully running ones ko kahi aur relocate karta hai, service ke `minimumHealthyPercent` ka respect karte hue. Isko ek **ASG lifecycle hook** se terminate par wire karo taaki scale-in, AMI refreshes, aur Spot interruptions running tasks ko just kill na karein.

Yeh **[ALB connection draining / deregistration delay](08-load-balancing-autoscaling.md#connection-draining--deregistration-delay)** se *different layer* hai, jo in-flight HTTP requests ko finish hone dene ke baare mein hai. Ek graceful deploy ko **dono** chahiye: tasks move karne ke liye container instance draining, aur unke requests complete hone dene ke liye deregistration delay.

**EC2 ko Fargate se kab choose karo:**
- **GPU** workloads, ya specific host hardware
- **Sustained high utilisation** — per vCPU cheaper, especially Savings Plans ya Spot ke saath
- **Privileged containers**, custom `sysctls`/kernel parameters, ya host device access
- **Ek per-host daemon** (per instance ek log/monitoring agent, per task sidecar ke bajaye)
- **Fargate ke 200 GB se zyada ephemeral storage**, ya specific EBS volume requirements
- **Per-host software licensing**, ya existing Reserved Instances absorb karne ke liye
- Bahut high task density, jaha Fargate ka per-task premium accumulate hota hai

**Iske badle tum kya lete ho** — isko honestly state karo, kyunki yehi actual trade hai: ECS-optimised AMI patch aur rotate karna, agent current rakhna, capacity headroom aur bin-packing decisions, scale-in par instance draining, aur cluster-level monitoring. Fargate ki higher per-vCPU price yeh sab buy kar rahi hai.

**ECS Anywhere** same control plane hai jo **tumhare apne on-prem servers** tak extend hota hai, container instances ke roop mein SSM agent ke through registered — hybrid ya data-residency requirements ke liye. Limitation note karo: external instances ko koi ELB integration aur koi `awsvpc` networking nahi milta.

**Interview answer:** *"Fargate jab tak kuch EC2 force na kare — GPU, privileged containers, host daemons, ya sustained utilisation jaha per-vCPU price jeete. Agar main EC2 par hoon to main ASG khud scale karne ke bajaye managed scaling aur termination protection wale ek capacity provider use karunga, tasks ko AZs ke across spread karunga, aur confirm karunga ki instance role task aur execution roles ke saath exist karta hai. Do details jo main verify karunga woh hain ephemeral range ALB ke liye open ke saath dynamic port mapping, aur ek ASG lifecycle hook se wired container instance draining taaki deploys aur scale-in running tasks ko kill na karein."*

**Ek line mein ECS vs EKS:** ECS simpler hai, AWS-proprietary hai, free control plane hai — koi real reason na ho to isko choose karo. EKS managed **Kubernetes** hai — clouds ke across portability ke liye, existing Kubernetes investment/team ke liye, ya CNCF ecosystem (Helm, operators, service meshes) ke liye isko choose karo, aur added complexity aur control-plane cost accept karo.

---

## 3. Fargate & Launch-Type Choice

### AWS Fargate

**Definition:** Containers ke liye serverless compute engine — tum ek Docker image + CPU/memory supply karte ho; AWS underlying servers, scaling, OS, aur patching handle karta hai. Fargate khud ek orchestrator nahi hai — yeh **ECS** ya **EKS** ke liye ek launch type hai.

**Core concepts**
- **Task** — ek running container (ya co-located group). **Task Definition** — blueprint (image, CPU/memory, env vars, IAM role). **Service** — N tasks ko running/healthy rakhta hai.
- Networking: sirf `awsvpc` mode — har task ko apna ENI milta hai (strong isolation, lekin IP exhaustion small subnets mein ek real capacity constraint hai).
- Koi SSH/host access nahi; instance role ke bajaye task-level IAM role.

**Pricing:** vCPU-seconds + GB-seconds ka pay karo sirf jab task chal rahi ho — zero idle cost.

**Sizing constrained hai, free-form nahi.** Tumhe **valid vCPU/memory pairs** se choose karna hai, arbitrary numbers nahi: 0.25 vCPU → 0.5/1/2 GB; 0.5 vCPU → 1–4 GB; 1 vCPU → 2–8 GB; 2 vCPU → 4–16 GB; 4 vCPU → 8–30 GB; aur bade workloads ke liye 8/16 vCPU tiers. Jo combination list mein nahi hai usko maango aur task definition reject ho jaata hai — ek common first-time surprise. Memory per vCPU tier fixed steps mein scale karti hai, isliye "mujhe thoda zyada RAM chahiye" kabhi kabhi matlab hota hai ek aur vCPU ke liye pay karna.

**Ephemeral storage:** har task ko **default 20 GB** milta hai, jo **200 GB** tak configurable hai — aur yeh *ephemeral* hai, task stop hone par gayab ho jaata hai. Jo bhi persist ya tasks ke beech share hona chahiye, uske liye **[EFS](06-ec2-instance-storage.md#efs-elastic-file-system)** mount karo. Yehi constraint hai jo large container images ya build/scratch workloads ko bites karta hai.

**Fargate Spot:** EC2 Spot jaisa hi interruption model — up to **~70% cheaper**, ek **2-minute `SIGTERM` warning** ke saath reclamation se pehle. Batch jobs, CI runners, aur queue consumers ke liye ideal jaha SQS interruption absorb karta hai. Ek **capacity provider strategy** se ek service dono mix kar sakta hai, jaise *"2 tasks always on `FARGATE`, everything above that on `FARGATE_SPOT`"* — baseline reliability cheap burst ke saath. Yeh bhi depend karta hai tumhare app ke `SIGTERM` handle karne par, jo Docker section ka exec-form `ENTRYPOINT` point hai.

**Yeh bhi jaanna zaruri hai:** Fargate **platform versions** underlying runtime control karte hain (kisi reason se pin na karna ho to `LATEST` use karo); **koi privileged mode nahi, no host-level daemons/DaemonSets, aur no GPU support** — yeh requirements tumhe EC2 launch type ki taraf push karti hain; aur Windows containers supported hain lekin higher per-task floor ke saath.

**EC2 vs Fargate**
| Aspect | EC2 | Fargate |
|---|---|---|
| Server management | Tum | AWS |
| OS/SSH access | Yes | No |
| Scaling | ASG (minutes) | Automatic (faster) |
| Pricing | Instance-hour, idle cost | Per task, no idle cost |
| Security isolation | Shared host possible | Strong (dedicated ENI/kernel per task) |
| Best for | Steady/high utilization, legacy, GPU | Bursty/microservices, ops simplicity priority |

**Cost trap (interview favorite):** "Fargate hamesha cheaper hai" **false** hai. Fargate bursty/low-utilization workloads ke liye jeet ta hai; EC2 steady, high-utilization workloads ke liye jeet ta hai kyunki tum Fargate per-task premium pay nahi kar rahe idle-free capacity par jo tum vaise bhi use karte. Notes se numeric rule of thumb: ek 24×7 1 vCPU/2GB service roughly $17–20/mo EC2 par cost karta hai (t3.small) vs $35–40/mo Fargate par; same size ka ek job jo 2 hrs/day chalta hai flip ho jaata hai ~$20/mo (EC2, mostly idle) vs ~$6–8/mo (Fargate) mein.

**Common false interview claims:** "Fargate hamesha cheaper hai", "Fargate EC2 ko replace karta hai", "Tum Fargate mein SSH kar sakte ho", "Fargate ki networking limits nahi hain" — sab false hain.

### EC2 vs Fargate Cost & Trap Scenarios

**Sample EC2 trap Q&A**
- *CPU low hai lekin app slow hai* → bottleneck likely disk I/O, network latency, ya ek single-threaded hot path hai — CPU% alone misleading hai.
- *Restart ke baad public IP change ho gaya* → public IPs static nahi hote jab tak tum ek Elastic IP attach na karo.
- *ASG spike ke dauran scale nahi hua* → scaling policy metric choice aur cooldown period length check karo.
- *Instance "running" hone ke bavajood unreachable hai* → security group, NACL, route table, aur kya isko public IP hai bhi ya nahi check karo.
- *Spot instance suddenly terminate ho gaya* → expected behavior hai; AWS spot capacity kabhi bhi reclaim kar sakta hai (2-minute warning via EventBridge).

**Sample Fargate trap Q&A**
- *Task unexpectedly stop ho gaya* → app crash hua, health check fail hua, ya memory limit hit hua (OOM-killed).
- *"Healthy" task lekin service usko unhealthy mark karta hai* → container health check fail ho raha hai process alive hone ke bavajood (jaise, wrong health check path/port).
- *Fargate task internet tak nahi pahunch sakta* → yeh ek private subnet mein hai bina NAT Gateway/VPC endpoint ke.
- *Dev mein kaam karta hai, prod mein fail hota hai* → almost always IAM role, secrets, ya networking (subnet/SG) differences environments ke beech.

**Final mental model:** Steady load → EC2. Bursty load → Fargate. Control chahiye → EC2. Simplicity chahiye → Fargate. Idle-cost sensitive → Fargate. High, constant utilization → EC2.

---

## 4. Container Registry

### ECR (Elastic Container Registry)

AWS ka managed private Docker registry — images S3 mein under the hood stored hoti hain, access **IAM** se controlled hota hai (plus cross-account ke liye repository policies), aur ECS, EKS, aur **Lambda container images** ke saath integrated hai.

```bash
aws ecr get-login-password --region us-east-1 | docker login --username AWS \
  --password-stdin 123456789012.dkr.ecr.us-east-1.amazonaws.com
docker tag myapi:1.0 123456789012.dkr.ecr.us-east-1.amazonaws.com/myapi:1.0
docker push          123456789012.dkr.ecr.us-east-1.amazonaws.com/myapi:1.0
```
Best practice ke roop mein naam layak features:
- **Image scanning** — push par *basic* scans CVE database ke against; **enhanced scanning** naye CVEs publish hone par OS aur language-package dependencies dono ke continuous rescanning ke liye **Amazon Inspector** use karta hai. Production ke kisi bhi cheez ke liye enhanced right answer hai.
- **Tag immutability** — existing tag ko overwrite karne se rokta hai. Isko on karo: mutable tags matlab `myapi:1.0` silently different bits ban sakta hai, jo reproducibility destroy karta hai aur rollback ko unreliable banata hai. **Digest** se deploy karo ya immutable version tags se, kabhi `:latest` se nahi.
- **Lifecycle policies** — untagged images expire karte hain aur sirf last N tagged ones rakhte hain. Iske bina, ECR silently tumhare bade storage bills mein se ek ban jaata hai.
- **Cross-region/cross-account replication** — taaki ek multi-region deployment scale par cross regions pull na kare.
- **Pull-through cache** — upstream public images (Docker Hub, ECR Public, MCR) tumhare registry mein cache karta hai, CI mein Docker Hub rate limits avoid karta hai. Naam lene layak ek bahut practical chiz.
- Public images ke liye **ECR Public Gallery**.

---

## 5. Choosing a Platform for .NET

### .NET Workloads ke liye ECS vs EKS vs Fargate vs Lambda

Original notes Lambda-vs-ECS aur EC2-vs-Fargate ko individually cover karte hain lekin kabhi directly bahut common senior .NET-on-AWS question ka answer nahi dete: *"Tum ek .NET microservices platform ko AWS par move kar rahe ho — compute layer kaise choose karoge?"*

| Criterion | Lambda | ECS (Fargate) | EKS (Fargate ya managed nodes) | EC2 (self-managed) |
|---|---|---|---|---|
| .NET fit | Event handlers, `Amazon.Lambda.AspNetCoreServer` ke through ASP.NET Core minimal hosting wale APIs ke liye great, lekin cold starts latency-sensitive sync APIs ko hurt karte hain jab tak AOT + Provisioned Concurrency na ho | Great — standard container deployment, no code changes, full ASP.NET Core hosting model | ECS jaisa hi lekin Kubernetes complexity add karta hai — sirf worth hai agar tum kahi aur K8s already chala rahe ho (multi-cloud, existing manifests, team expertise) | Full control, Windows containers/.NET Framework (legacy) workloads ke liye good jinko IIS chahiye |
| Operational overhead | Lowest | Low (koi cluster manage nahi karna) | Highest (control plane concepts, CRDs, Helm, networking) | Highest (patching, scaling, OS) |
| Team skill fit | Kisi bhi .NET team | Kisi bhi .NET/DevOps team | Existing K8s expertise chahiye | Sysadmin/infra expertise chahiye |
| Cost at steady state | Poor (per-invocation billing add up hoti hai) | Good | Good | Best (agar fully utilized ho) |
| .NET Framework (Core nahi) support | No (Lambda ko .NET Core/5+ chahiye) | Yes, Windows containers ke through EC2 launch type par (Fargate par nahi) | Yes, Windows node groups | Yes |
| Startup-latency-sensitive sync API | Provisioned Concurrency + AOT ke bina risky | Best default choice | Best default choice | Best default choice |
| Best use case | Webhooks, S3/SQS/DynamoDB Stream processors, cron, glue | Line-of-business APIs, internal microservices | Sirf agar already multi-cloud/K8s-standardized ho | Legacy .NET Framework/IIS lift-and-shift |

**Senior-level recommendation pattern:** naye .NET microservices ko default **ECS on Fargate** par rakho (typical .NET shop ke liye operational simplicity aur control ka best balance). Event-driven glue aur spiky/idle-heavy workloads ke liye **Lambda** use karo. **EKS** ka use tabhi karo jab existing organizational Kubernetes investment ho — sirf .NET-on-AWS migration ke liye K8s introduce karna usually over-engineering hai. **EC2** (Windows containers ya full Windows Server ke saath) sirf un .NET Framework workloads ke liye use karo jo .NET Core/8+ mein port nahi ho sakte, ya GPU/specialized hardware chahiye wale workloads ke liye.

**Interviewer follow-up jo expect karna chahiye:** "Tumhari team ko sirf .NET/C# pata hai, koi Kubernetes nahi — kya tum ab bhi ek green-field microservices platform ke liye EKS choose karoge?" Correct senior answer: nahi — ECS/Fargate choose karo jab tak koi concrete multi-cloud ya portability requirement na ho jo K8s learning curve aur operational tax justify kare.

### Fargate/ECS/EKS Trade-offs — Hands-On Time Ke Bina Reasoning

Pehle honesty framing, kyunki interview mein isko explicitly state karna worth hai: mera hands-on AWS provisioning experience (Terraform/CDKTF ke through) Lambda, DynamoDB, EC2, aur S3 hai — maine personally Fargate, ECS, ya EKS production mein nahi chalaya. Aage jo hai woh yeh hai ki agar mujhse yeh decision banane ke liye kaha jaaye to main trade-offs kaise reason karunga, containers orchestrators ke saath direct operational experience ka claim nahi.

**Expanded comparison — operational overhead, cost model, cold start, aur use-case fit**

| Dimension | EC2 (self-managed) | ECS on Fargate | EKS (managed control plane) | Lambda |
|---|---|---|---|---|
| OS/kernel kaun patch karta hai | Tum | AWS | AWS (nodes) ya tum (self-managed node groups) | AWS (fully abstracted) |
| Orchestrator/control plane kaun manage karta hai | N/A (koi orchestrator nahi) ya tum (self-hosted) | AWS (ECS control plane free hai, hamesha managed) | AWS control plane manage karta hai (hourly per cluster charged), lekin tum node groups manage karte hi ho jab tak Fargate profiles na use karo | N/A |
| Cost model | Instance-hour, load ke bavajood | Per-task vCPU/GB-second, chalte hue | Cluster fee + node/Fargate task cost | Per-invocation + duration |
| Cold start behavior | Ek baar running hone par kuch nahi; ASG scale-out mein minutes lagte hain (OS boot, storage attach, register) | Task startup seconds mein hota hai (image pull, container start) — Lambda sense mein koi "cold start" nahi, lekin instant bhi nahi | Fargate-backed pods ke liye Fargate jaisa; managed EC2 node groups ke liye, raw EC2 ki tarah node/ASG scale-out time se bounded | First/scaled-out invocation par true cold start (ms–low seconds), Provisioned Concurrency se mitigated |
| Operational overhead | Highest — patching, scaling policy tuning, capacity planning | Low — no servers, no cluster; task definitions/services manage karte ho | Container options mein sabse highest — cluster upgrades, CRDs, networking plugins (CNI), Helm charts, IAM-to-Kubernetes-RBAC mapping (IRSA) | Lowest — koi infrastructure concept hi nahi |
| Best use-case fit | Steady, high utilization; OS-level requirements; legacy/stateful; GPU | Standard containerized microservices jinke paas existing K8s investment nahi | Organizations jo already Kubernetes par standardized hain (often multi-cloud, ya on-prem K8s se migrate ho rahe) | Event-driven, spiky, short-lived work |
| Multi-cloud portability | Low (AWS-specific tooling even if OS portable hai) | Low (ECS AWS-proprietary hai) | High (Kubernetes API clouds/on-prem ke across portable hai) | Lowest (heavily AWS-event-model-coupled) |

**Agar puchha jaaye "Kubernetes industry standard hai to EKS sab kuch ke liye kyun nahi" to trade-off conversation kaise frame karunga:**

Kubernetes ka sabse bada selling point — portability aur ek rich ecosystem (Helm, operators, service mesh) — iski sabse badi cost bhi hai: ek real control-plane learning curve (CRDs, RBAC, networking/CNI, admission controllers) jo ek pure ECS ya Fargate user ko kabhi pay nahi karna padta. Ek .NET shop ke liye jiske paas existing Kubernetes investment nahi hai, yeh operational tax usually justified nahi hoti jab tak koi concrete multi-cloud requirement na ho ya org ke paas already platform engineers hon jo daily Kubernetes mein rehte hain. Main apni recommendation ko wahi frame se dunga jaise AWS khud ECS-vs-EKS choice ko frame karta hai: ECS agar tumhe AWS-native simplicity chahiye aur portability nahi chahiye; EKS agar tumhe Kubernetes-API compatibility chahiye tooling, multi-cloud strategy, ya existing team expertise ke liye.

**Ek decision flow jo main interview mein out loud discuss karunga:**

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

**Deep Fargate/ECS/EKS expertise claim karne se pehle jo hands-on seekhna chahunga:** real load ke under task definition/service tuning (deployment circuit breakers, rolling deploys ke dauran min/max healthy percent), service discovery (Cloud Map/App Mesh), aur — specifically EKS ke liye — IRSA (IAM Roles for Service Accounts) mapping aur cluster upgrade mechanics. Isko directly naam dena, familiarity overstate karne ke bajaye, khud senior-level move hai.

### .NET ko AWS par Deploy Karna: Elastic Beanstalk vs ECS vs Lambda Custom Runtime

Original notes kabhi directly Elastic Beanstalk discuss nahi karte, iske ek common AWS Certified/senior-interview topic hone aur ek legitimate, low-effort .NET deployment path hone ke bavajood.

| Option | Yeh kya hai | Fayde | Nuksan | Kab use karein |
|---|---|---|---|---|
| **Elastic Beanstalk** | EC2/ASG/ELB/RDS ke around ek PaaS wrapper ek managed platform ke saath (Windows/Linux par .NET included) | `dotnet publish` se ek running, load-balanced, auto-scaled app tak fastest path; AWS underlying EC2/ASG/ELB stack manage karta hai; environment swap ke through blue/green support karta hai | Raw ECS se kam control; platform upgrades disruptive ho sakte hain; "PaaS lock-in" feel; ECS se coarser scaling granularity | Small-to-mid teams jinhe container/K8s investment ke bina managed infra chahiye; quick MVPs; AWS mein naye teams |
| **ECS (Fargate ya EC2 launch type)** | Container orchestration, AWS-native | Container spec par full control, fine-grained scaling, no idle cost (Fargate), CodePipeline/CodeBuild ke saath cleanly integrate hota hai | Docker packaging discipline chahiye, task definition management | Zyada modern .NET microservices ke liye default choice |
| **Lambda (ASP.NET Core minimal API hosting ya custom runtime)** | Serverless — zip ya container image ke roop mein package karo | Koi infra nahi bilkul, zero tak scale hota hai, spiky traffic ke liye cheap | Cold starts, 15-min limit, local debugging parity harder, connection pooling nuances (RDS Proxy often chahiye) | Event-driven APIs, low/spiky traffic, backend-for-frontend functions |

**Interviewers jo nuance probe karte hain:** Elastic Beanstalk ek separate compute primitive *nahi* hai — under the hood yeh abhi bhi EC2 + ASG + ELB (ya, Docker platform ke liye, ECS) provision karta hai. Value-add deployment/orchestration tooling hai (`eb deploy`, environment configs, rolling/immutable/blue-green deployment policies), koi naya runtime nahi. Yeh distinction jaanna (Beanstalk = orchestration layer, naya infrastructure nahi) woh chiz hai jo mid-level ko senior answer se separate karti hai.

**Deployment strategy comparison (sab teen kisi form ka zero/low-downtime deploy support karte hain):**
- Elastic Beanstalk: rolling, rolling-with-additional-batch, immutable, ya blue/green (environments ke beech CNAME swap).
- ECS: service deployment configuration ke through rolling update, ya CodeDeploy + two target groups ke through blue/green.
- Lambda: versions + aliases, CodeDeploy ke through linear/canary traffic shifting ke saath.

**Disaster Recovery — Containers (ECS / ECR / Fargate)**

| | |
|---|---|
| **Actually risk par kya hai** | ECR mein container images, task definitions, service configuration, aur koi bhi EFS-backed state |
| **Backup mechanism** | **ECR cross-region/cross-account replication rules**, **immutable tags**, task definitions (inhe AWS khud version karta hai) + IaC, EFS ke liye **AWS Backup** |
| **Realistic RPO / RTO** | Images/config RPO ~0. RTO minutes — task definition register karo aur service banao |

**Recovery runbook:**
1. **Bad release:** service ko **previous task definition revision** par update karke rollback karo — `aws ecs update-service --task-definition my-app:41`. Revisions immutable hain, isi liye yeh safe hai.
2. **Regional failure:** confirm karo ki image DR region ke ECR mein hai (pehle se set ki gayi replication rule se), wahan task def register karo, aur DR cluster + target group ke against service banao.
3. **EFS state:** AWS Backup recovery point se ek naye file system mein restore karo aur volume configuration update karo.
4. Traffic shift karne se **pehle** DR service scale up karo — Fargate task startup plus ALB target registration minutes leta hai, aur pehle DNS shift kar dene se sirf errors serve hote hain.

⚠️ **Gotcha:** **task definition image ko poore URI se pin karta hai, jismein registry ka account aur region shamil hai.** Us task def ko dusre region mein copy karo aur wo abhi bhi *original* ECR par point karta hai — toh real regional outage mein wo pull hi nahi kar sakta, aur service ek aisi error ke saath start hone se fail hoti hai jo permissions problem jaisi lagti hai. ECR replication set karo **aur** image URI ko per-region parameterise karo. Aur `:latest` kabhi deploy mat karo — immutable tags ya digest ke bina "previous revision par rollback" actually image badalta hi nahi.

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
**Tasks start na hone par debugging order** (ek achha practical answer): pehle `describe-services` **events**, phir **stopped task ka `stoppedReason`**. Practice mein yeh almost always in mein se ek hota hai — **task execution role** ECR se pull nahi kar sakta ya logs nahi likh sakta, image architecture match nahi karta (x86 par ek ARM image ya vice versa), **health check** app ke start finish karne se pehle fail ho jaata hai (ALB health-check grace period raise karo), private subnet se ECR tak koi route nahi (NAT gateway ya **ECR + S3 ke liye VPC endpoints** chahiye), ya missing environment variable ki wajah se container immediately exit ho jaata hai.

---

← [Load Balancing, Scalability & Auto Scaling](08-load-balancing-autoscaling.md) · [Index](README.md) · [Relational Databases, Caching & Analytics](10-databases-caching-analytics.md) →
