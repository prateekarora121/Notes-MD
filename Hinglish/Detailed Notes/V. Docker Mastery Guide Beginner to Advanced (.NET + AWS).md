# Docker Mastery Guide: Beginner to Advanced (.NET + AWS)

*Hinglish me explained | Practice exercises | Pitfalls | Interview Q&A | 5 saal ke experience level tak*

## Isko kaise use karein

Har module me 5 cheezein hain: **Concept** (Hinglish), **Key Commands**, **Practice Exercise**, **Pitfalls** aur **Interview Questions**. Pehle 6 modules foundation hain, Module 7-12 production aur AWS ke liye, aur Module 13-14 advanced + interview prep. Har exercise khud type karke karo, copy-paste se seekhna nahi hota. Tools: Docker Desktop, .NET 8 SDK, AWS CLI v2, ek AWS free-tier account.

**5 saal ke experience ka matlab kya hai?** Interviewer sirf `docker run` nahi puchega. Wo puchega: image kyu 200MB hai 80MB nahi, container kyu OOMKilled hua, ECS task kyu unhealthy hai, secrets kaise inject kiye, rollback kaise kiya. Yeh guide isi level ke liye hai.

## Module 1: Docker Fundamentals

### Concept

**Container kya hai?** Container ek isolated process hai jo host machine ka kernel share karta hai. VM me poora Guest OS hota hai (heavy, GBs, minutes me boot). Container me sirf app + uske dependencies hote hain (MBs, seconds me start).

Docker Linux ke 3 features par chalta hai:

- **Namespaces**: isolation deta hai (PID, network, mount, user). Container ko lagta hai wo akela hai machine pe.
- **cgroups**: resources limit karta hai (CPU, memory).
- **Union filesystem (overlay2)**: image layers ko stack karta hai, read-only layers ke upar ek writable layer.

**Architecture:** Docker CLI -> Docker Daemon (dockerd) -> containerd -> runc. Image registry (Docker Hub, ECR) se pull hoti hai.

**Image vs Container:** Image = class/blueprint (read-only). Container = object/running instance. Ek image se 100 containers ban sakte hain.

### Key Commands

```bash
docker version
docker info
docker run hello-world
docker run -d -p 8080:80 --name web nginx
docker ps -a
docker logs -f web
docker exec -it web sh
docker stop web && docker rm web
```

### Practice Exercise

1. nginx container 8080 port pe chalao aur browser me open karo.
2. Container ke andar `exec` karke `/usr/share/nginx/html/index.html` edit karo, refresh karo.
3. Container delete karke dubara chalao. Tumhara edit kahan gaya? (Jawab: gaya, kyunki writable layer container ke saath delete hoti hai. Yeh Module 4 me fix hoga.)
4. `docker run --memory=50m --cpus=0.5 nginx` chalao aur `docker stats` se verify karo.

### Pitfalls

- Windows containers aur Linux containers alag hote hain. .NET Core/.NET 8 ke liye almost hamesha **Linux containers** use karo (cheaper, smaller, AWS Fargate me best supported).
- Container ko VM samajhna galat hai. Container me ek hi main process hona chahiye.
- `docker run` bina `--rm` ke baar baar chalane se stopped containers jama hote rehte hain.

### Interview Questions

**Q1. Container aur VM me kya difference hai?** VM hypervisor pe chalti hai aur apna poora OS leti hai. Container host kernel share karta hai, isliye light aur fast hai. Trade-off: isolation VM jitna strong nahi hota, kyunki kernel shared hai.

**Q2. Docker internally kaise isolation deta hai?** Namespaces (kya dikhega), cgroups (kitna milega), aur overlay filesystem (layers). Yeh sab Linux kernel features hain, Docker inhe wrap karta hai.

**Q3. Image layers kya hote hain aur kyu important hain?** Har Dockerfile instruction (RUN, COPY, ADD) ek read-only layer banata hai. Layers cache hoti hain aur images ke beech share hoti hain, isliye build fast aur storage kam lagta hai.

## Module 2: Images, Containers aur Lifecycle

### Concept

Container lifecycle: **created -> running -> paused -> stopped (exited) -> removed**. Container tab tak chalta hai jab tak uska PID 1 process zinda hai. Agar main process khatam, container exit.

**CMD vs ENTRYPOINT:** ENTRYPOINT = fixed executable. CMD = default arguments jo override ho sakte hain. Dono ka combination best hai: `ENTRYPOINT ["dotnet", "App.dll"]`.

**Exec form vs Shell form:** `CMD ["dotnet","App.dll"]` (exec form) me dotnet PID 1 hota hai aur SIGTERM seedha milta hai. `CMD dotnet App.dll` (shell form) me `/bin/sh` PID 1 hota hai, signal app tak nahi pahunchta, graceful shutdown fail hota hai.

**Tags aur digest:** `myapp:1.0` mutable hai (koi bhi overwrite kar sakta hai). `myapp@sha256:abc...` immutable hai. Production me digest ya unique tag (git SHA) use karo.

### Key Commands

```bash
docker images
docker pull mcr.microsoft.com/dotnet/aspnet:8.0
docker tag myapp:latest myapp:1.0.3
docker inspect <container>
docker history myapp:latest
docker system df
docker system prune -a
docker cp web:/etc/nginx/nginx.conf ./
```

### Practice Exercise

1. `docker history` se dekho kisi image ke kitne layers hain aur kaunsa layer sabse bada hai.
2. Ek container jaan-bujhkar crash karao (`docker run alpine sh -c "exit 3"`) aur `docker inspect` se exit code nikalo.
3. `docker run --restart=on-failure:3 alpine sh -c "exit 1"` chalao aur restart count observe karo.

### Pitfalls

- **`latest` tag production me use karna**: rollback impossible ho jata hai aur pata nahi chalta kaunsa version chal raha hai.
- `docker system prune -a` running na hone wale sab images delete kar deta hai. Production server pe soch ke chalao.
- Exit code 137 = OOMKilled ya SIGKILL, 143 = SIGTERM. Interview me yeh yaad rakho.

### Interview Questions

**Q1. CMD aur ENTRYPOINT me difference?** ENTRYPOINT container ka fixed command hai. CMD uske default args deta hai. `docker run image extra-args` CMD ko override karta hai, ENTRYPOINT ko nahi (jab tak `--entrypoint` na do).

**Q2. Container start hote hi exit kyu ho jata hai?** Kyunki PID 1 process khatam ho gaya ya crash hua. `docker logs` aur `docker inspect` (ExitCode) se check karo. Common reasons: wrong CMD, missing config, port already in use, app foreground me nahi chal rahi.

**Q3. Graceful shutdown kaise hota hai?** `docker stop` pehle SIGTERM bhejta hai, 10 sec (default) wait karta hai, phir SIGKILL. .NET me `IHostApplicationLifetime` aur `HostOptions.ShutdownTimeout` se in-flight requests complete karte hain. Exec form zaroori hai taaki signal mile.

## Module 3: Dockerfile for .NET (Multi-stage, Caching)

### Concept

Sabse important module. .NET ke liye **multi-stage build** standard hai: build stage me poora SDK (\~800MB), final stage me sirf runtime (\~100-220MB). Compiler aur source code production image me nahi jaata.

**Layer caching ka rule:** jo cheez kam change hoti hai (csproj, NuGet restore) usse pehle rakho, jo zyada change hoti hai (source code) usse baad me. Isse `dotnet restore` cache ho jata hai.

### Production-ready Dockerfile

```dockerfile
# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Pehle sirf csproj copy karo -> restore layer cache hogi
COPY ["Api/Api.csproj", "Api/"]
COPY ["Core/Core.csproj", "Core/"]
RUN dotnet restore "Api/Api.csproj"

# 2. Ab source copy karo
COPY . .
RUN dotnet publish "Api/Api.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

# 3. Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
# Non-root user (.NET 8 images me 'app' user built-in hai)
USER app
ENTRYPOINT ["dotnet", "Api.dll"]
```

**.dockerignore** (zaroori hai):

```
**/bin
**/obj
.git
.vs
*.md
Dockerfile*
docker-compose*
**/appsettings.Development.json
```

**Base image options:** `aspnet:8.0` (Debian, standard), `aspnet:8.0-alpine` (chhota, lekin musl libc issues, ICU/globalization config chahiye), `aspnet:8.0-jammy-chiseled` (Ubuntu chiseled, no shell, no package manager, minimum attack surface, best for production).

### Practice Exercise

1. `dotnet new webapi -n Api` banao, upar wala Dockerfile likho, build karo: `docker build -t api:1.0 .`
2. Sirf ek source file change karke dubara build karo. Dekho `restore` layer cached hai ya nahi.
3. csproj ka order tod ke (pehle `COPY . .` phir restore) build karo aur time compare karo.
4. Same app ko `aspnet:8.0`, `-alpine` aur `-jammy-chiseled` pe build karke `docker images` me size compare karo.
5. `docker run -p 8080:8080 api:1.0` chalao aur `curl localhost:8080/weatherforecast` test karo.

### Pitfalls

- **.NET 8 me default port 80 se 8080 ho gaya hai** (non-root ke liye). Port mismatch se ECS/ALB health check fail hota hai. Sabse common bug.
- `COPY . .` pehle karne se har code change pe NuGet restore dobara chalta hai.
- `.dockerignore` na hone se `bin/obj` copy hote hain aur build context GBs ka ho jata hai.
- Alpine pe `System.Globalization.Invariant` ya ICU ka issue: `InvariantGlobalization=true` set karo ya `icu-libs` install karo.
- Secrets (connection strings) Dockerfile me ENV ya COPY se bake karna. Image history me wo leak ho jate hain.
- Chiseled image me shell nahi hota, `docker exec -it sh` fail hoga. Debug ke liye ephemeral debug container use karo.

### Interview Questions

**Q1. Multi-stage build kyu use karte hain?** Image size aur attack surface kam karne ke liye. SDK, compilers, source code final image me nahi jaate. Sirf published output aur runtime jaata hai.

**Q2. Layer caching optimise kaise karte ho?** Stable instructions upar, frequently changing neeche. csproj copy -> restore -> baaki source copy -> publish. Saath me `.dockerignore` aur BuildKit cache mounts (`RUN --mount=type=cache,target=/root/.nuget/packages`).

**Q3. Container non-root kyu chalana chahiye?** Agar attacker container break kare to host pe root privileges na mile. .NET 8 images me `USER app` (UID 1654) available hai. Non-root ko port 1024 se upar bind karna padta hai, isliye 8080.

**Q4. COPY aur ADD me difference?** ADD me tar auto-extract aur remote URL support hai, isliye unpredictable hai. Best practice: hamesha COPY use karo, ADD sirf tar extract ke liye.

**Q5. ARG aur ENV me difference?** ARG sirf build time pe hota hai. ENV runtime pe bhi rehta hai. Dono image history me visible hote hain, isliye secrets ke liye kabhi nahi (BuildKit `--mount=type=secret` use karo).

## Module 4: Volumes, Bind Mounts aur Data Persistence

### Concept

Container ka writable layer temporary hai. Data persist karna hai to teen options:

- **Named Volume**: Docker manage karta hai (`/var/lib/docker/volumes`). Production/DB ke liye best.
- **Bind Mount**: host ka specific folder container me mount hota hai. Development me hot-reload ke liye.
- **tmpfs**: sirf memory me, secrets/temp ke liye.

**AWS context:** ECS Fargate me Docker volumes nahi hote. Wahan **EFS** (shared persistent) ya Fargate ephemeral storage (20GB default, 200GB tak) use hota hai. Stateless design best hai: files S3 me, sessions Redis/ElastiCache me, DB RDS me.

### Key Commands

```bash
docker volume create sqldata
docker run -d --name sql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='Str0ng!Pass' \
  -v sqldata:/var/opt/mssql -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
docker volume ls
docker volume inspect sqldata
docker run -v $(pwd)/logs:/app/logs api:1.0
```

### Practice Exercise

1. SQL Server container volume ke saath chalao, table banao, container `rm` karo, dubara same volume se chalao. Data bacha?
2. Bina volume ke yahi karo aur difference dekho.
3. Bind mount se `appsettings.json` override karo aur app me change verify karo.
4. Volume ka backup lo: `docker run --rm -v sqldata:/data -v $(pwd):/backup alpine tar czf /backup/sql.tar.gz /data`.

### Pitfalls

- Volume ke bina DB container chalana aur data khona. Sabse classic galti.
- Windows/Mac bind mounts slow hote hain (file sync). Linux/WSL2 filesystem me project rakho.
- Bind mount host files ko overwrite/hide kar deta hai, container ke andar ki existing files gayab ho jati hain.
- Non-root user ko volume me write permission nahi milti (`chown` ya `--user` fix karo).
- Prod DB ko container me chalana AWS pe generally galat hai. **RDS** use karo (backups, Multi-AZ, patching).

### Interview Questions

**Q1. Volume aur bind mount me kya difference hai?** Volume Docker-managed hai, portable hai, backup/driver support milta hai. Bind mount host path pe depend karta hai, tight coupling hoti hai. Production me volume, dev me bind mount.

**Q2. Container stateless kyu hona chahiye?** Taaki kabhi bhi kill/replace/scale ho sake bina data loss ke. Autoscaling me naye containers aate-jaate hain. State external stores me rakho (RDS, S3, ElastiCache).

**Q3. ECS Fargate me persistent storage kaise dete ho?** EFS volume task definition me mount karte hain. Ya better: data S3/RDS me rakho, Fargate ephemeral storage sirf temp ke liye.

## Module 5: Docker Networking

### Concept

Network drivers:

- **bridge** (default): ek host pe containers ka private network. Port publish karna padta hai (`-p host:container`).
- **user-defined bridge**: containers **naam se ek dusre ko resolve** kar sakte hain (built-in DNS). Default bridge pe yeh nahi hota.
- **host**: container host ka network directly use karta hai, no isolation.
- **none**: no network.
- **overlay**: multi-host (Swarm).

**AWS context:** ECS Fargate me `awsvpc` mode hota hai: har task ko apna ENI aur private IP milta hai. Security Groups task level pe lagte hain. Same task ke containers `localhost` pe baat karte hain.

### Key Commands

```bash
docker network create appnet
docker run -d --name db --network appnet mcr.microsoft.com/mssql/server:2022-latest ...
docker run -d --name api --network appnet -p 8080:8080 \
  -e ConnectionStrings__Default="Server=db;..." api:1.0
docker network inspect appnet
```

### Practice Exercise

1. Do containers default bridge pe chalao, ek se dusre ko naam se ping karo (fail hoga). Phir user-defined network pe karo (pass hoga).
2. .NET API ko SQL container se naam (`Server=db`) se connect karao.
3. Ek port publish na karo aur host se access try karo, phir `-p` lagao. Samjho EXPOSE sirf documentation hai.

### Pitfalls

- Container ke andar `localhost` ka matlab **wo container khud** hai, host ya dusra container nahi. Beginners ki #1 galti: API me `Server=localhost` likhna.
- `EXPOSE` port publish nahi karta, sirf documentation hai.
- `ASPNETCORE_URLS=http://localhost:8080` set karne se bahar se access nahi hota. `http://+:8080` ya `0.0.0.0` use karo.
- Host network mode production me security risk hai.
- Environment variable me nested config: `ConnectionStrings__Default` (double underscore), colon nahi.

### Interview Questions

**Q1. Default bridge aur user-defined bridge me difference?** User-defined bridge me automatic DNS-based service discovery hai, better isolation hai, aur containers ko live attach/detach kar sakte ho. Default bridge me sirf IP se baat hoti hai.

**Q2. API container DB container se connect nahi ho raha, kaise debug karoge?** Same network check karo, DB ready hai ya nahi (health check), connection string me service name hai ya localhost, port sahi hai, `docker exec` se `nc -zv db 1433` ya `curl`. Logs dekho.

**Q3. EXPOSE aur -p me kya farak hai?** EXPOSE metadata hai. `-p` actual host port mapping karta hai.

## Module 6: Docker Compose (Multi-container .NET App)

### Concept

Compose ek YAML file se multi-container app define karta hai: API + SQL + Redis. Local dev aur integration tests ke liye best. **Production orchestration nahi hai**, wahan ECS/EKS use hota hai.

### docker-compose.yml

```yaml
services:
  api:
    build:
      context: .
      dockerfile: Api/Dockerfile
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__Default=Server=db;Database=AppDb;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True
      - Redis__Host=cache:6379
    depends_on:
      db:
        condition: service_healthy
    restart: unless-stopped

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: ${SA_PASSWORD}
    volumes:
      - sqldata:/var/opt/mssql
    healthcheck:
      test: /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$$MSSQL_SA_PASSWORD" -Q "SELECT 1" || exit 1
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 20s

  cache:
    image: redis:7-alpine

volumes:
  sqldata:
```

`.env` file me `SA_PASSWORD=Str0ng!Pass123` rakho aur `.env` ko `.gitignore` me daalo.

### Key Commands

```bash
docker compose up -d --build
docker compose ps
docker compose logs -f api
docker compose exec api sh
docker compose down -v   # volumes bhi delete
docker compose config    # final merged config dekho
```

### Practice Exercise

1. Upar ki compose file se API + SQL + Redis stack chalao.
2. `depends_on` ko bina healthcheck ke chalao, race condition (API pehle start, DB baad me) reproduce karo. Phir `service_healthy` se fix karo.
3. `docker-compose.override.yml` banao dev-specific settings ke liye (bind mount, debug env).
4. EF Core migrations ko startup pe ya alag `migrator` service se chalao.

### Pitfalls

- `depends_on` bina `condition` ke sirf start order deta hai, readiness nahi. App me bhi **retry/Polly** lagao.
- `docker compose down -v` volumes uda deta hai (DB data loss).
- Passwords compose file me hardcode karke git me push karna.
- Compose v1 (`docker-compose`) deprecated hai, ab `docker compose` (v2) use karo.
- SQL Server container ko minimum 2GB RAM chahiye warna silently crash hota hai.

### Interview Questions

**Q1. depends\_on readiness guarantee karta hai?** Nahi. Default me sirf container start order hai. `condition: service_healthy` + healthcheck lagao, aur app me retry policy rakho.

**Q2. Compose production me use kar sakte hain?** Single-host chhote setup me ho sakta hai, lekin AWS pe scaling, self-healing, rolling deploy, ALB integration ke liye ECS/EKS use karte hain.

**Q3. Multiple environments (dev/test) kaise handle karoge?** `-f docker-compose.yml -f docker-compose.prod.yml` se override files, `.env` files, aur profiles (`profiles: ["debug"]`).

## Module 7: Image Optimization aur Security

### Concept

Production me image **chhoti, secure aur reproducible** honi chahiye.

**Size kam karne ke tarike:** multi-stage build, chiseled/alpine base, `.dockerignore`, ek hi `RUN` me install + cleanup, `dotnet publish` flags (`-p:PublishTrimmed=true` sirf tab jab trimming-safe ho, `-p:PublishReadyToRun=true` startup fast karne ke liye, Native AOT minimal APIs ke liye).

**Security checklist:**

- Non-root user (`USER app`)
- Read-only root filesystem (`--read-only` ya ECS `readonlyRootFilesystem`)
- Minimal base image (chiseled)
- Vulnerability scan (ECR scan on push, Trivy, Docker Scout)
- Secrets image me nahi, runtime pe inject (Secrets Manager / SSM Parameter Store)
- `--cap-drop=ALL` aur `--security-opt no-new-privileges`
- Image signing aur digest pinning
- Base image regular update karo (patching)

**BuildKit secrets** (build time pe private NuGet feed ke liye):

```dockerfile
RUN --mount=type=secret,id=nuget_pat \
    dotnet nuget add source https://feed.example/v3/index.json -n private \
    -u user -p $(cat /run/secrets/nuget_pat) --store-password-in-clear-text \
 && dotnet restore
```

```bash
DOCKER_BUILDKIT=1 docker build --secret id=nuget_pat,src=./pat.txt -t api:1.0 .
```

### Practice Exercise

1. Apni image ko Trivy se scan karo: `trivy image api:1.0`. HIGH/CRITICAL CVEs list karo aur base image update karke dubara scan karo.
2. Container ko `--read-only --tmpfs /tmp --cap-drop=ALL --user 1654` ke saath chalao. Agar app crash kare to dekho kya write kar rahi thi.
3. Image ko 220MB se kam karo (chiseled + ReadyToRun) aur before/after table banao.
4. `docker history --no-trunc` se check karo ki koi secret leak to nahi ho raha.

### Pitfalls

- ENV/ARG me password daalna. `docker history` ya `docker inspect` se koi bhi padh lega.
- Trimming enable karke reflection/JSON serialization/EF Core tod dena. Pehle test karo.
- Alpine + `PublishReadyToRun` ka RID match karna padta hai (`linux-musl-x64`).
- Read-only filesystem me .NET ko `/tmp` chahiye (DataProtection keys, temp files). tmpfs mount do.
- Sirf scan karna kaafi nahi, **fix process** bhi chahiye (CI me fail threshold).

### Interview Questions

**Q1. Image size 1GB hai, kaise kam karoge?** SDK image runtime ke liye use ho rahi hogi to multi-stage lagao. Phir aspnet runtime ya chiseled base, `.dockerignore`, layers merge, aur unnecessary packages hatao. `dive` tool se layer-wise waste dekho.

**Q2. Secrets container me kaise handle karte ho?** Image me kabhi nahi. AWS me Secrets Manager/SSM Parameter Store task definition ke `secrets` field se env var ya file ke roop me inject hote hain, task role se access control hota hai. Local me user-secrets ya compose secrets.

**Q3. Container escape se kaise bachoge?** Non-root, dropped capabilities, no privileged mode, read-only FS, seccomp/AppArmor profiles, updated host/runtime, aur Fargate use karo (host AWS manage karta hai).

**Q4. Digest pinning kyu?** Tag mutable hota hai, base image ke andar silently change ho sakta hai. Digest immutable hai, reproducible builds milte hain.

## Module 8: Logging, Health Checks, Debugging aur Resource Limits

### Concept

**Logging:** Container ko logs **stdout/stderr** pe likhne chahiye, file me nahi. Docker log driver unhe collect karta hai. .NET me `Console` logger + JSON format (Serilog compact JSON) use karo. AWS me `awslogs` driver logs CloudWatch me bhejta hai.

**Health checks:** .NET me `builder.Services.AddHealthChecks()` aur `app.MapHealthChecks("/health")`. Do type ke endpoints rakho: **liveness** (process zinda hai?) aur **readiness** (dependencies ready hain? DB reachable?). Dockerfile me `HEALTHCHECK` tab hi lagao jab image me curl/wget ho. Chiseled me nahi hota, isliye ECS/ALB health check use karo.

**Resource limits:** .NET container-aware hai. Memory limit set karoge to GC heap us hisaab se adjust hota hai (default max heap = 75% of limit). CPU limit se `Environment.ProcessorCount` aur thread pool prabhavit hota hai.

**OOMKilled:** memory limit cross hui to kernel process kill karta hai (exit 137). `docker inspect` me `OOMKilled: true` dikhta hai.

### Key Commands

```bash
docker logs --tail 100 -f --since 10m api
docker stats
docker inspect --format='{{.State.OOMKilled}} {{.State.ExitCode}}' api
docker top api
docker events
docker run --memory=256m --memory-swap=256m api:1.0
```

```csharp
builder.Services.AddHealthChecks()
    .AddSqlServer(connStr, tags: new[] { "ready" });
app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });
```

### Practice Exercise

1. API me live aur ready endpoints banao, DB band karke dekho ready fail ho aur live pass rahe.
2. Container ko `--memory=100m` ke saath chalao, memory leak simulate karo (static list me data bharo) aur OOMKilled reproduce karo.
3. Serilog se JSON logs stdout pe likho aur `docker logs` me verify karo.
4. `dotnet-counters` ya `dotnet-dump` ko sidecar/ephemeral container se attach karke GC metrics dekho.

### Pitfalls

- Logs file me likhna. Container delete = logs gayab, aur disk bhi bharta hai.
- Default `json-file` log driver me rotation na hone se host disk full (`max-size`, `max-file` set karo).
- Health check me heavy dependency check (har 10 sec DB query) load badhata hai. Liveness me dependency check **mat** karo, warna temporary DB issue se saare containers restart ho jayenge.
- Memory limit bina set kiye chalana: ek container poore host ko kha sakta hai.
- `HEALTHCHECK` ke liye curl image me install na hona.
- Startup slow hone par health check jaldi fail ho jana (`start_period` use karo).

### Interview Questions

**Q1. Container exit 137 hai, kya hua hoga?** SIGKILL mila. Aksar OOMKilled (memory limit exceed). `docker inspect` me OOMKilled check karo, memory limit badhao ya leak fix karo, GC settings (`DOTNET_GCHeapHardLimit`) dekho. ECS me task stopped reason me dikhta hai.

**Q2. Liveness aur readiness me difference?** Liveness: app hung/deadlock to nahi, fail hone par restart. Readiness: traffic lene ke liye ready hai ya nahi (DB/cache connected), fail hone par traffic rokte hain, restart nahi.

**Q3. .NET container me memory limit ka GC pe kya asar?** GC cgroup limit padhta hai. Default heap hard limit 75% hoti hai. Server GC vs Workstation GC aur `DOTNET_gcServer`, `DOTNET_GCHeapHardLimitPercent` se tuning hoti hai. Chhote containers me Workstation GC behtar.

**Q4. Running container debug kaise karoge jisme shell nahi hai?** `docker debug`, ya same network namespace me debug container (`--network container:api` / `--pid container:api`), Kubernetes me `kubectl debug`, ECS me ECS Exec (SSM based).

## Module 9: Amazon ECR (Registry)

### Concept

**ECR** AWS ka private container registry hai. IAM se access control, image scanning, lifecycle policies, cross-region replication, aur ECS/EKS ke saath native integration.

Image URI format: `<account-id>.dkr.ecr.<region>.amazonaws.com/<repo>:<tag>`

### Key Commands

```bash
REGION=ap-south-1
ACCOUNT=123456789012

aws ecr create-repository --repository-name myapp/api --region $REGION \
  --image-scanning-configuration scanOnPush=true \
  --image-tag-mutability IMMUTABLE

aws ecr get-login-password --region $REGION | \
  docker login --username AWS --password-stdin $ACCOUNT.dkr.ecr.$REGION.amazonaws.com

GIT_SHA=$(git rev-parse --short HEAD)
docker build -t myapp/api:$GIT_SHA .
docker tag myapp/api:$GIT_SHA $ACCOUNT.dkr.ecr.$REGION.amazonaws.com/myapp/api:$GIT_SHA
docker push $ACCOUNT.dkr.ecr.$REGION.amazonaws.com/myapp/api:$GIT_SHA

aws ecr describe-image-scan-findings --repository-name myapp/api --image-id imageTag=$GIT_SHA
```

**Lifecycle policy** (purani images auto-delete, cost bachao):

```json
{
  "rules": [{
    "rulePriority": 1,
    "description": "Keep last 20 images",
    "selection": { "tagStatus": "any", "countType": "imageCountMoreThan", "countNumber": 20 },
    "action": { "type": "expire" }
  }]
}
```

### Practice Exercise

1. ECR repo banao (immutable tags + scan on push), apni .NET image push karo.
2. Same tag dubara push karo aur IMMUTABLE ki wajah se error dekho.
3. Lifecycle policy lagao aur verify karo.
4. Ek IAM user/role banao jise sirf push permission ho (`ecr:PutImage`, `ecr:InitiateLayerUpload` etc.) aur ek jise sirf pull.
5. Cross-region replication enable karo.

### Pitfalls

- ECR login token **12 ghante** me expire hota hai. Pipeline me har run pe login karo.
- Region mismatch: repo ap-south-1 me, login us-east-1 me.
- `latest` tag + mutable repo = rollback nightmare.
- Lifecycle policy na hone se storage bill badhta jata hai.
- ECS task execution role me ECR pull permission na hona (`AmazonECSTaskExecutionRolePolicy`).
- Private subnet me ECR pull ke liye NAT Gateway ya **VPC endpoints** (ecr.api, ecr.dkr, S3 gateway, CloudWatch logs) chahiye.
- Apple Silicon (M1/M2) pe build karke push karna: image arm64 hoti hai, Fargate x86 pe fail (`exec format error`). `--platform linux/amd64` ya buildx use karo.

### Interview Questions

**Q1. ECS task image pull nahi kar pa raha (CannotPullContainerError), kya check karoge?** Execution role ki ECR permissions, image URI/tag sahi hai, network (private subnet me NAT/VPC endpoints), security group outbound 443, repo policy, aur architecture mismatch.

**Q2. ECR vs Docker Hub?** ECR: IAM integration, AWS ke andar low latency, no rate limits jaise Docker Hub, scanning, replication. Docker Hub me anonymous pull rate limit hai.

**Q3. Immutable tags kyu?** Traceability aur safe rollback. Har deployment ka exact artifact fix rehta hai.

## Module 10: .NET Deployment on AWS (ECS Fargate, EKS, App Runner, Beanstalk)

### Concept: AWS options ka comparison

| Service | Kab use karein | Complexity |
| --- | --- | --- |
| **App Runner** | Simple web API, kam ops, auto-scale | Low |
| **ECS on Fargate** | Most .NET microservices, no server management | Medium |
| **ECS on EC2** | GPU, cost optimization at scale, custom AMI | Medium-High |
| **EKS** | Kubernetes ecosystem/multi-cloud, complex orchestration | High |
| **Elastic Beanstalk (Docker)** | Legacy lift-and-shift, simple | Low |
| **Lambda container image** | Event-driven, 10GB tak image | Low-Medium |

**ECS Fargate ke core concepts:**

- **Cluster**: logical grouping.
- **Task Definition**: blueprint (image, CPU/memory, env, secrets, ports, log config, roles). Docker Compose ka ECS equivalent.
- **Service**: desired count maintain karta hai, ALB se jodta hai, rolling/blue-green deploy karta hai.
- **Task Role** vs **Execution Role**: Execution role = ECS agent ke liye (ECR pull, logs likhna, secrets fetch). Task role = tumhari app ke liye (S3, SQS, DynamoDB access). **Interview ka favourite sawal.**

**Architecture:** Route 53 -> ALB (public subnet) -> ECS Tasks (private subnets, multi-AZ) -> RDS (private, Multi-AZ). Secrets Manager, CloudWatch, ECR VPC endpoints ke through.

### Task Definition (sample)

```json
{
  "family": "api",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "512",
  "memory": "1024",
  "executionRoleArn": "arn:aws:iam::123456789012:role/ecsTaskExecutionRole",
  "taskRoleArn": "arn:aws:iam::123456789012:role/apiTaskRole",
  "runtimePlatform": { "cpuArchitecture": "X86_64", "operatingSystemFamily": "LINUX" },
  "containerDefinitions": [{
    "name": "api",
    "image": "123456789012.dkr.ecr.ap-south-1.amazonaws.com/myapp/api:abc123",
    "portMappings": [{ "containerPort": 8080 }],
    "essential": true,
    "environment": [{ "name": "ASPNETCORE_ENVIRONMENT", "value": "Production" }],
    "secrets": [{ "name": "ConnectionStrings__Default",
                  "valueFrom": "arn:aws:secretsmanager:ap-south-1:123456789012:secret:prod/db" }],
    "healthCheck": { "command": ["CMD-SHELL", "curl -f http://localhost:8080/health/live || exit 1"],
                     "interval": 30, "timeout": 5, "retries": 3, "startPeriod": 30 },
    "stopTimeout": 30,
    "readonlyRootFilesystem": true,
    "logConfiguration": { "logDriver": "awslogs",
      "options": { "awslogs-group": "/ecs/api", "awslogs-region": "ap-south-1", "awslogs-stream-prefix": "ecs" } }
  }]
}
```

### Key Commands (AWS CLI)

```bash
aws ecs create-cluster --cluster-name prod
aws ecs register-task-definition --cli-input-json file://taskdef.json
aws ecs create-service --cluster prod --service-name api --task-definition api:1 \
  --desired-count 2 --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-a,subnet-b],securityGroups=[sg-123],assignPublicIp=DISABLED}" \
  --load-balancers targetGroupArn=arn:aws:elasticloadbalancing:...,containerName=api,containerPort=8080 \
  --deployment-configuration "deploymentCircuitBreaker={enable=true,rollback=true},minimumHealthyPercent=100,maximumPercent=200"

aws ecs update-service --cluster prod --service api --task-definition api:2
aws ecs describe-services --cluster prod --services api
aws ecs execute-command --cluster prod --task <id> --container api --interactive --command "/bin/sh"
```

### Practice Exercise

1. VPC (2 public + 2 private subnets), ALB, target group (health path `/health/ready`, port 8080) banao.
2. ECR image se ECS Fargate service chalao (2 tasks) private subnets me.
3. DB connection string Secrets Manager se inject karo, **task role** me sirf required permissions do.
4. Naya version deploy karo (rolling), phir jaan-bujhkar broken image deploy karo aur **deployment circuit breaker** se auto-rollback dekho.
5. Service Auto Scaling lagao (CPU 60% target tracking), load test se scale-out verify karo.
6. ECS Exec enable karke running task me ghuso.
7. Same app ko App Runner pe bhi deploy karo aur effort/cost compare karo.

### Pitfalls

- **Port/health check mismatch**: container 8080 pe sun raha hai, target group 80 pe check kar raha hai. Tasks baar-baar kill aur restart hote hain.
- Health check grace period na dena. Slow .NET startup (migrations, warm-up) me ECS task ko unhealthy samajh ke maar deta hai (`healthCheckGracePeriodSeconds`).
- Execution role aur Task role confuse karna. App S3 access nahi kar paati kyunki permission execution role me daali.
- Fargate CPU/memory ke valid combinations fix hain (0.25 vCPU = 512MB-2GB, 0.5 = 1-4GB, 1 = 2-8GB...). Invalid combo pe registration fail hota hai.
- Public IP + public subnet me tasks chalana. Private subnet + ALB rakho.
- Database migrations ko har task startup pe chalana. Multiple tasks race karte hain. Alag one-off ECS task (`run-task`) ya CI step se chalao.
- ALB idle timeout (60s) aur Kestrel keep-alive mismatch se random 502/504.
- `stopTimeout` kam rakhna: in-flight requests drop hote hain deployment me. ALB deregistration delay (300s default) bhi tune karo.
- ALB ke peeche HTTPS terminate hota hai, app ko `X-Forwarded-Proto` ke liye `UseForwardedHeaders()` chahiye, warna redirect loop/wrong URLs.
- Cost: Fargate Spot sirf fault-tolerant workloads ke liye.

### Interview Questions

**Q1. Task Role aur Execution Role me difference?** Execution role ECS/Fargate agent use karta hai: ECR se image pull, CloudWatch logs, Secrets Manager se secrets fetch karke inject. Task role tumhari application code ke AWS API calls ke liye hai (S3, SQS). App credentials SDK container credential endpoint se leti hai, keys hardcode nahi karte.

**Q2. ECS vs EKS kab choose karoge?** ECS: AWS-native, simple, kam operational overhead, chhoti/medium teams. EKS: Kubernetes portability, rich ecosystem (Helm, operators, service mesh), complex scheduling, multi-cloud. Dono Fargate ya EC2 pe chal sakte hain.

**Q3. Zero-downtime deployment kaise karte ho?** Rolling update (`minimumHealthyPercent=100`, `maximumPercent=200`), sahi health checks, deregistration delay, graceful shutdown (SIGTERM handle), ya CodeDeploy se blue/green with traffic shifting (canary/linear). Circuit breaker auto-rollback ke liye.

**Q4. Tasks bar-bar restart ho rahe hain, kaise investigate karoge?** ECS service events, stopped task reason (OOM, health check fail, essential container exit), CloudWatch logs, ALB target health reason codes, memory/CPU limits, port mapping, secrets permission, startup time vs grace period.

**Q5. Auto scaling kaise setup karte ho?** Application Auto Scaling: target tracking on CPU/memory ya ALBRequestCountPerTarget, min/max tasks, cooldowns. Queue workers ke liye SQS queue depth custom metric.

**Q6. 502 vs 503 vs 504 ALB pe?** 502: backend ne invalid response diya ya connection tod diya (keep-alive mismatch, crash). 503: koi healthy target nahi. 504: backend timeout se zyada time laga.

**Q7. Fargate vs EC2 launch type?** Fargate: serverless, per-task billing, no patching, thoda costly at steady high load. EC2: control, Reserved/Spot se sasta at scale, GPU/privileged/daemon workloads, lekin instance patching aur capacity management aapki zimmedari.

## Module 11: CI/CD Pipeline (GitHub Actions / CodePipeline) for .NET + ECS

### Concept

Pipeline stages: **Checkout -> Restore/Build -> Unit Test -> Docker build -> Scan -> Push to ECR -> Deploy to ECS (staging) -> Integration/smoke test -> Approval -> Deploy (prod)**.

Authentication ke liye long-lived access keys ki jagah **OIDC (GitHub) -> IAM role** use karo.

### GitHub Actions example

```yaml
name: deploy
on:
  push:
    branches: [main]
permissions:
  id-token: write
  contents: read
jobs:
  build-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - run: dotnet test --configuration Release
      - uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: arn:aws:iam::123456789012:role/github-deploy
          aws-region: ap-south-1
      - id: ecr
        uses: aws-actions/amazon-ecr-login@v2
      - name: Build and push
        run: |
          IMAGE=${{ steps.ecr.outputs.registry }}/myapp/api:${{ github.sha }}
          docker build -t $IMAGE .
          docker push $IMAGE
          echo "IMAGE=$IMAGE" >> $GITHUB_ENV
      - name: Render task definition
        id: render
        uses: aws-actions/amazon-ecs-render-task-definition@v1
        with:
          task-definition: taskdef.json
          container-name: api
          image: ${{ env.IMAGE }}
      - uses: aws-actions/amazon-ecs-deploy-task-definition@v2
        with:
          task-definition: ${{ steps.render.outputs.task-definition }}
          service: api
          cluster: prod
          wait-for-service-stability: true
```

### Practice Exercise

1. Upar ka pipeline apne repo me lagao, OIDC role banao (trust policy sirf tumhare repo/branch ke liye).
2. Pipeline me Trivy scan step add karo jo CRITICAL pe fail kare.
3. Staging aur prod environments alag karo, prod ke liye manual approval lagao.
4. Rollback drill: purane task definition revision pe `update-service` karke 2 minute me rollback karo.
5. Docker layer caching GitHub Actions me (`docker/build-push-action` + `cache-from/cache-to type=gha`) lagao aur build time compare karo.

### Pitfalls

- IAM access keys GitHub secrets me store karna. OIDC use karo.
- CI role ko `AdministratorAccess` dena. Least privilege (ECR push + ecs:UpdateService + iam:PassRole specific roles).
- `iam:PassRole` bhoolna: deploy fail hota hai.
- Tests Docker build ke baad chalana (late feedback). Pehle test, ya Dockerfile me test stage rakho.
- Build agent pe layer cache na hona, har build 5+ minute.
- `wait-for-service-stability` na lagana: pipeline green, lekin deployment actually fail.
- Database schema change aur app deploy ka order. Backward-compatible migrations (expand/contract pattern) rakho taaki rollback safe ho.

### Interview Questions

**Q1. Rollback strategy kya hai?** Immutable tags + purani task definition revision. `update-service` se pichli revision, ya circuit breaker auto-rollback. DB migrations backward-compatible rakhte hain.

**Q2. Blue/green aur rolling me difference?** Rolling: purane tasks dhire dhire naye se replace hote hain, ek hi service. Blue/Green: dono environments side by side, traffic ek jhatke me ya gradually shift (CodeDeploy), instant rollback, lekin double capacity chahiye.

**Q3. Pipeline me AWS credentials kaise secure karte ho?** OIDC federation se short-lived credentials, least-privilege role, branch/repo conditions trust policy me.

## Module 12: Config, Secrets, Observability aur Scaling on AWS

### Concept

**Configuration (12-factor):** config environment se aani chahiye. .NET me `appsettings.json` -> `appsettings.{Env}.json` -> env vars -> secrets, last wala override karta hai. Env var me `Section__Key` format.

**Secrets:** Secrets Manager (rotation support, thoda costly) vs SSM Parameter Store (sasta, simple config). .NET me `AWSSDK.Extensions.NETCore.Setup` + `Amazon.Extensions.Configuration.SystemsManager` se direct config provider mil jata hai.

**Observability ke 3 pillars:**

- **Logs**: CloudWatch Logs (structured JSON), Logs Insights queries.
- **Metrics**: CloudWatch Container Insights, custom metrics, Prometheus/Grafana.
- **Traces**: OpenTelemetry + AWS X-Ray / ADOT collector sidecar.

**Sidecar pattern:** ek task me multiple containers (app + otel-collector / nginx / datadog agent). Same task me `localhost` pe baat karte hain.

**Data Protection keys:** ASP.NET Core multiple containers me cookies/antiforgery tokens ke liye shared key ring chahta hai. Keys ko S3/SSM/Redis/DB me persist karo, warna scale-out me login/anti-forgery errors aate hain.

### Practice Exercise

1. DB password Secrets Manager me rakho aur ECS task me inject karo.
2. Serilog JSON + correlation ID middleware lagao, CloudWatch Logs Insights me ek request ko end-to-end trace karo.
3. OpenTelemetry + ADOT sidecar se traces X-Ray me bhejo.
4. CloudWatch alarm banao (5xx rate, CPU, unhealthy host count) aur SNS se notify karo.
5. ASP.NET Core DataProtection keys ko S3/Redis me persist karo aur 2 tasks ke beech login test karo.

### Pitfalls

- Secret value change karne ke baad running tasks ko naya value nahi milta. Secrets task **start** pe inject hote hain, naya deployment (force new deployment) chahiye.
- Logs me PII/secrets print karna.
- CloudWatch log group retention na set karna (cost infinite).
- Scale-out pe in-memory cache/session use karna. Distributed cache (ElastiCache Redis) use karo.
- DataProtection keys local filesystem pe: tasks restart ya scale par users logout.
- Sidecar ko `essential: false` na karna ya essential karna: galat setting se poora task restart.

### Interview Questions

**Q1. Multiple containers me session/auth consistent kaise rakhoge?** Stateless design: JWT ya distributed session (Redis), shared DataProtection key ring (S3/SSM/DB), sticky sessions se bachna.

**Q2. Secrets Manager vs Parameter Store?** Secrets Manager: automatic rotation (RDS integration), cross-account sharing, per-secret cost. Parameter Store: free standard tier, config + simple secrets, rotation nahi.

**Q3. Production incident: latency badh gayi, kaise debug karoge?** ALB metrics (TargetResponseTime, 5xx), ECS CPU/memory, task count/scale events, X-Ray traces se slow dependency (DB/external API), RDS Performance Insights, connection pool exhaustion, thread pool starvation (`dotnet-counters`), recent deployment correlate karo.

## Module 13: Advanced Docker (BuildKit, buildx, Internals, Kubernetes bridge)

### Concept

**BuildKit features:** parallel stage builds, cache mounts, secret mounts, SSH mounts, remote cache (`--cache-to type=registry` / ECR cache). Cache mount se NuGet packages har build me dobara download nahi hote:

```dockerfile
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "Api/Api.csproj"
```

**Multi-arch builds (buildx):** ARM64 (Graviton) pe Fargate/EC2 sasta padta hai (\~20% tak). Ek image dono architectures ke liye:

```bash
docker buildx create --use
docker buildx build --platform linux/amd64,linux/arm64 \
  -t $ACCOUNT.dkr.ecr.$REGION.amazonaws.com/myapp/api:$GIT_SHA --push .
```

.NET me cross-arch build ke liye SDK stage me `--platform=$BUILDPLATFORM` aur `dotnet publish -a $TARGETARCH` use karo, QEMU emulation slow hoti hai.

**Internals jo senior se expected hain:**

- Image = manifest + config + layer tarballs (content-addressable, sha256).
- Container runtime stack: `dockerd -> containerd -> runc`. ECS/EKS (containerd) Docker daemon use nahi karte; Kubernetes ne dockershim hata diya.
- `overlay2`: lowerdir (image layers) + upperdir (writable) + merged view. Copy-on-write.
- PID 1 problem: zombie reaping aur signal handling. `docker run --init` ya `tini` use karo.
- Docker socket (`/var/run/docker.sock`) mount karna = host pe root access dene jaisa.

**Kubernetes bridge (EKS):** Compose ki jagah Deployment + Service + Ingress; liveness/readiness probes, resource requests/limits, HPA, ConfigMap/Secret (External Secrets Operator), IRSA (pod level IAM role, Task Role ka equivalent). ECS ke concepts seedha map hote hain: Task Definition \~ Pod spec, Service \~ Deployment, ALB \~ Ingress.

### Practice Exercise

1. BuildKit cache mount lagao aur cold vs warm build time compare karo.
2. buildx se multi-arch image ECR me push karo, `docker buildx imagetools inspect` se dono manifests dekho.
3. Graviton (arm64) Fargate task chalao (`cpuArchitecture: ARM64`) aur cost/performance compare karo.
4. Ek image ka tar export karo (`docker save`) aur andar ke layer blobs aur manifest manually dekho.
5. `kind` ya `minikube` pe apni image deploy karo (Deployment + Service + probes).
6. `docker run --init` ke saath aur bina ke SIGTERM behaviour dekho.

### Pitfalls

- Docker socket mount karke container ke andar se containers chalana (CI me common, security risk). Kaniko/BuildKit rootless prefer karo.
- Multi-arch build QEMU se chalana: 10x slow. Native runners ya cross-compile.
- Remote cache ko invalidate/clean na karna: cache bloat.
- Compose ki cheezein (depends\_on, build) ECS/K8s me directly nahi chalti. Mental model badalna padta hai.
- Windows containers + Fargate possible hai lekin costly aur limited. .NET Framework legacy app ho to pehle .NET 8 migrate karne ka plan banao.

### Interview Questions

**Q1. Docker image andar se kaise bani hoti hai?** Manifest layers ki list + config JSON (env, cmd, history) rakhta hai. Har layer ek tar archive hai jiska sha256 digest hota hai. Registry me layers blobs ke roop me store hoti hain aur share hoti hain.

**Q2. PID 1 problem kya hai?** Container me PID 1 ko kernel special treat karta hai: default signal handlers nahi hote (SIGTERM ignore ho sakta hai) aur orphan zombie processes reap karne ki zimmedari hoti hai. Exec form ya `--init`/tini se solve hota hai.

**Q3. Docker daemon ke bina image kaise build karoge CI me?** Kaniko, BuildKit rootless, Buildah, ya AWS CodeBuild (privileged mode). Docker socket mount avoid karo.

**Q4. ECS aur Kubernetes ke concepts map karo.** Task Definition \~ Pod template, Service \~ Deployment+Service, Target Group+ALB \~ Ingress/Service type LoadBalancer, Task Role \~ IRSA / Pod Identity, Container Insights \~ Prometheus/Container Insights, Service Auto Scaling \~ HPA.

**Q5. Cold start kaise kam karoge .NET containers me?** ReadyToRun/Native AOT, trimmed image, kam startup work (lazy init, migrations bahar), warm min capacity, App Runner/Lambda container ke liye provisioned concurrency/SnapStart alternatives.

## Module 14: Rapid-Fire Interview Round (5 Years Experience Level)

Yeh sawal alag-alag topics se aate hain. Jawab bolke practice karo, STAR format me apne project ka example jodo.

**1. Apne project ka deployment architecture explain karo.** Structure: source -> CI (build/test/scan) -> ECR -> ECS/EKS -> ALB -> RDS/Redis/S3. Phir batao: environments, scaling, secrets, monitoring, rollback, cost optimization. Numbers do (image size 90MB, deploy time 6 min, 99.9% uptime).

**2. Docker ne kaunsi real problem solve ki tumhare project me?** "Works on my machine" khatam, environment parity, fast onboarding (compose se 10 min me local setup), consistent CI/CD, horizontal scaling.

**3. Production me container crash loop me hai. Step-by-step batao.** Stopped reason/exit code -> logs -> health check path/port -> secrets/env -> memory limit -> recent change/diff -> purani revision pe rollback -> RCA.

**4. Docker image me tum kya-kya security practices follow karte ho?** Chiseled/minimal base, non-root, scan in CI + ECR, no secrets in layers, digest pinning, read-only FS, least privilege task role, private subnets, VPC endpoints, regular base image patching.

**5. CMD, ENTRYPOINT, RUN me kab kya?** RUN build time pe chalta hai (layer banata hai). CMD/ENTRYPOINT container start pe.

**6. Docker networking me container se container baat nahi kar raha.** Same network? DNS name? Port? Firewall/SG? App `0.0.0.0` pe bind hai? `docker network inspect`.

**7. `docker stop` vs `docker kill`?** Stop: SIGTERM + grace period + SIGKILL. Kill: seedha SIGKILL (ya custom signal).

**8. Volume data ka backup/restore?** Temporary container se tar. Production me RDS snapshots, S3 versioning, EFS backup (AWS Backup).

**9. Config environment-wise kaise manage karte ho?** Env vars + Parameter Store/Secrets Manager, ek hi image sab environments me (build once, deploy many), image me environment-specific cheez nahi.

**10. Image tag strategy kya rakhte ho?** Git SHA (immutable) + semantic version release pe. `latest` sirf local dev.

**11. Docker Swarm vs Kubernetes vs ECS?** Swarm simple lekin ecosystem chhota/declining. K8s industry standard, complex. ECS AWS-native simple. Choice team skill + portability need pe.

**12. Container me DB chalana chahiye production me?** Generally nahi on AWS: managed RDS use karo (HA, backups, patching). Container DB sirf dev/test (Testcontainers) ke liye.

**13. Testcontainers kya hai?** .NET integration tests me real SQL/Redis container programmatically chalane ke liye library (`Testcontainers.MsSql`). In-memory fake se zyada reliable.

**14. Ek hi image multiple environments me kaise?** "Build once, configure at runtime". ASPNETCORE\_ENVIRONMENT + external config. Image promote karo (dev -> stage -> prod), rebuild mat karo.

**15. Cost optimization on AWS containers?** Right-sizing CPU/memory (Compute Optimizer), Graviton, Fargate Spot (stateless/batch), Savings Plans, ECR lifecycle, CloudWatch log retention, NAT Gateway cost kam karne ko VPC endpoints, scale-in policies, nights/weekends dev environment band.

**16. Tumhari image 5 min me build hoti hai, 1 min kaise?** Layer order, cache mounts, remote cache, `.dockerignore`, parallel stages, self-hosted/larger runners, smaller context, tests parallel.

**17. Graceful shutdown ECS me kaise ensure karoge?** Exec form entrypoint, SIGTERM handle (`IHostApplicationLifetime`), `stopTimeout`, ALB deregistration delay align, background workers me cancellation tokens.

**18. Sensitive data logs me na jaye, kaise ensure karte ho?** Structured logging with destructuring policies/masking, log review in code review, CloudWatch Logs data protection policies.

## Module 15: 8-Week Study Plan aur Capstone Project

| Week | Focus | Deliverable |
| --- | --- | --- |
| 1 | Module 1-2: fundamentals, lifecycle | 20 commands comfortably, notes |
| 2 | Module 3: Dockerfile .NET | Multi-stage <120MB image |
| 3 | Module 4-5: volumes, networking | API + SQL on custom network |
| 4 | Module 6-7: Compose, security | Compose stack + Trivy clean scan |
| 5 | Module 8-9: health, logging, ECR | Pushed image + lifecycle policy |
| 6 | Module 10: ECS Fargate | Live API behind ALB in private subnets |
| 7 | Module 11-12: CI/CD, secrets, observability | Auto-deploy pipeline + dashboards |
| 8 | Module 13-14: advanced + mock interviews | Multi-arch image, 3 mock interviews |

### Capstone Project (resume-worthy)

**"Orders Platform" on AWS:**

- 2 .NET 8 services (Orders API + Background Worker) + SQS between them, RDS SQL Server/PostgreSQL, ElastiCache Redis, S3 for files.
- Multi-stage Dockerfiles, chiseled images, non-root, <120MB each.
- Local: docker compose + Testcontainers integration tests.
- AWS: ECR (immutable + scan), ECS Fargate in private subnets, ALB with HTTPS (ACM), Secrets Manager, task roles per service, autoscaling (CPU + SQS depth).
- CI/CD: GitHub Actions with OIDC, Trivy gate, staging -> manual approval -> prod, circuit breaker rollback.
- Observability: structured JSON logs, CloudWatch alarms, OpenTelemetry traces in X-Ray.
- Infra as Code: Terraform ya AWS CDK (.NET) se poora stack.
- Documentation: architecture diagram, runbook (rollback, incident steps), cost estimate.

### Final Self-Check (kya tum 5 saal wale ho?)

- Kya tum bina Google kiye bata sakte ho ki exit code 137 ka matlab kya hai aur fix kaise hoga?
- Kya tum ek chhoti, non-root, cache-friendly .NET Dockerfile 5 minute me likh sakte ho?
- Kya tum Task Role vs Execution Role whiteboard pe samjha sakte ho?
- Kya tum bina downtime ke deploy aur 2 minute me rollback demonstrate kar sakte ho?
- Kya tum 502/503/504 ka root cause alag-alag bata sakte ho?
- Kya tum cost, security aur reliability ke trade-offs justify kar sakte ho?

Agar 6 me se 5 par "haan" hai, to aap interview ke liye ready ho. Baaki me us module ka exercise dobara karo.
