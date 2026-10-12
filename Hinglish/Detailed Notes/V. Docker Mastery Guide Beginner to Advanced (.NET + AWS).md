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
docker version                           # Docker client aur daemon ke versions dikhata hai
docker info                              # Docker daemon, storage, runtime aur system details dikhata hai
docker run hello-world                   # Test image chalata hai; message ke baad container exit hota hai
docker run -d -p 8080:80 --name web nginx # Nginx ko background me chala kar host 8080 ko container 80 se map karta hai
docker ps -a                             # Running aur stopped dono tarah ke containers list karta hai
docker logs -f web                       # web container ke naye logs live dikhata hai
docker exec -it web sh                   # Running web container me interactive shell kholta hai
docker stop web && docker rm web         # web ko gracefully stop karke container remove karta hai
```

### Practice Exercise

1. nginx container 8080 port pe chalao aur browser me open karo.
2. Container ke andar `exec` karke `/usr/share/nginx/html/index.html` edit karo, refresh karo.
3. Container delete karke dubara chalao. Tumhara edit kahan gaya? (Jawab: gaya, kyunki writable layer container ke saath delete hoti hai. Yeh Module 4 me fix hoga.)
4. `docker run --memory=50m --cpus=0.5 nginx` chalao (`--memory` RAM limit aur `--cpus` CPU quota set karta hai), phir `docker stats` se live usage verify karo.

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

### Image Deep Dive (Beginner se Medium level)

#### 1. Image kya hoti hai?

**Image = read-only template** jisme app chalane ke liye sab kuch packaged hota hai: base OS files, runtime (.NET), tumhara compiled code, dependencies, environment defaults aur startup command. Image khud "chalti" nahi hai; image se **container** banta hai. Analogy: image = class ya recipe, container = us class ka object ya bana hua khana. Ek image se hazaron containers ban sakte hain.

Docker images **OCI (Open Container Initiative) standard** follow karti hain, isliye same image Docker, containerd, Podman, ECS, EKS sab par chalti hai.

**Image vs Container (interview ka favourite):**

| Point | Image | Container |
|---|---|---|
| Nature | Read-only template | Image ka running (ya stopped) instance |
| Layers | Sirf read-only layers | Read-only layers + upar ek **writable layer** |
| State | Immutable, kabhi change nahi hoti | Runtime me files likh sakta hai (container delete = writable layer delete) |
| Command | `docker build`, `docker pull` | `docker run`, `docker start` |
| Count | Ek image | Ek image se kai containers |

#### 2. Layers aur Union Filesystem

Image ek **stack of layers** hai. Har layer pichli layer se sirf "kya badla" (diff) store karti hai. Docker `overlay2` storage driver se in layers ko jod kar ek single filesystem dikhata hai.

```
Container writable layer   (read-write, container ke saath delete hota hai)
-------------------------
Layer 4: COPY --from=build /app/publish .   (read-only)
Layer 3: RUN useradd ...                     (read-only)
Layer 2: .NET runtime files                  (read-only)
Layer 1: Debian base OS                      (read-only)
```

Zaroori baatein:

- **Kaunsi instructions layer (size) banati hain?** `RUN`, `COPY`, `ADD`. Baaki instructions (`ENV`, `CMD`, `ENTRYPOINT`, `EXPOSE`, `WORKDIR`, `LABEL`, `USER`) sirf metadata hain, size nahi badhate.
- **Copy-on-Write (CoW):** Container read-only layer ki file modify kare to wo file pehle writable layer me copy hoti hai, phir change hota hai. Original layer untouched rehti hai, isliye layers safely share hoti hain.
- **Layer sharing:** 10 .NET services same `aspnet:8.0` base use karein to base layers disk par sirf **ek baar** store hoti hain aur registry se bhi ek baar pull hoti hain.
- **Delete se size kam nahi hota:** Agar ek `RUN` me file bani aur agle `RUN` me delete ki, to purani layer me file abhi bhi hai (naye layer me sirf "whiteout" marker lagta hai). Isliye cleanup **usi RUN me** karo: `RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*`.

#### 3. Image ke andar kya hota hai (Anatomy)

Ek image 3 hisson se bani hoti hai:

1. **Layers:** compressed tar files (filesystem diffs).
2. **Image config (JSON):** ENV, CMD, ENTRYPOINT, exposed ports, user, working dir, labels, history aur layer IDs.
3. **Manifest:** JSON jo config aur layers ko digest se point karta hai. Registry se pull karte waqt pehle manifest aata hai.

**Content-addressable storage:** har cheez ka naam uske content ka SHA-256 hash hota hai. Content badla to hash badla. Isi se integrity verify hoti hai.

**Image ID vs Digest:** *Image ID* image **config** ka hash hai (local identity). *Digest* **manifest** ka hash hai (registry identity, `repo@sha256:...`). Dono alag cheezein hain, isliye `docker images` ka ID aur `docker images --digests` ka digest match nahi karte.

#### 4. Image naming: registry/namespace/repo:tag

```
123456789012.dkr.ecr.ap-south-1.amazonaws.com / team / orders-api : 1.4.2
|______________ registry ____________________| |namespace| |repo|  |tag|
```

- Registry nahi likha to default `docker.io` (Docker Hub). Tag nahi likha to default `latest`. `nginx` ka matlab hai `docker.io/library/nginx:latest`.
- `latest` sirf ek naam hai, iska matlab "newest" nahi hota. Koi bhi tag push karne ke baad hi `latest` update hota hai.
- **Ek image ke multiple tags** ho sakte hain (`1.4.2`, `1.4`, `1`, `latest`). Sab same ID/digest ko point karte hain. Tag = movable label, digest = fixed fingerprint.
- **Dangling image:** jisse koi tag nahi (`<none>:<none>`). Aksar tab bante hain jab same tag se naya build hota hai aur purani image ka tag naye par chala jata hai. `docker image prune` inhe clean karta hai.

#### 5. Base images: sahi base kaise chuno

Har Dockerfile `FROM` se shuru hota hai. Base image size, security aur compatibility teeno decide karti hai.

| Base | Approx size | Kab use karein | Dhyan rakho |
|---|---|---|---|
| `scratch` | 0 MB | Static Go/Rust binary | Shell, libc kuch nahi; .NET normal publish yahan nahi chalta |
| `alpine` | ~5-10 MB | Chhoti images | **musl libc** use karta hai, kuch native libs me problem (glibc chahiye to issue) |
| `debian-slim` | ~30-80 MB | General purpose, best compatibility | Alpine se bada |
| `distroless` | ~20-50 MB | Production, minimal attack surface | Shell nahi, debugging mushkil |

**Official .NET images (mcr.microsoft.com/dotnet/...):**

| Image | Kya hai | Kab use karein |
|---|---|---|
| `sdk:8.0` | Compiler, CLI, NuGet, sab tools (~800 MB+) | **Sirf build stage** me |
| `aspnet:8.0` | ASP.NET Core runtime (~220 MB Debian-based) | Web API / MVC ka final stage |
| `runtime:8.0` | Sirf .NET runtime, ASP.NET nahi | Console app, worker service |
| `runtime-deps:8.0` | Sirf OS dependencies, runtime nahi | **Self-contained / Native AOT** apps |
| `*-alpine` tags | Alpine based, chhoti | Size critical ho aur musl compatibility check kar li ho |
| `*-chiseled` tags | Ubuntu based, **distroless-jaisi**: no shell, no package manager, non-root | Production ke liye recommended, chhoti aur secure |

(Sizes approximate hain, version ke saath badalte hain. Interview me "SDK bada, runtime chhota, chiseled sabse minimal" bolna kaafi hai.)

**Golden rule:** build SDK image me, run runtime image me. Isi ko **multi-stage build** kehte hain (detail Module 3 me). Isse final image me compiler, source code aur build tools nahi jaate.

#### 6. Build context aur `.dockerignore`

`docker build .` me `.` **build context** hai: Docker is folder ki saari files daemon ko bhejta hai. Context bada to build slow. `.dockerignore` me `bin/`, `obj/`, `.git/`, `node_modules/`, `*.md`, `.env` daalo. Isse build fast hota hai, cache bar-bar invalidate nahi hoti aur secrets (`.env`) image me leak nahi hote.

#### 7. Build cache aur layer ordering

Docker har instruction ke liye check karta hai: instruction aur uski input files same hain? Haan to **cache hit** (layer reuse). Ek layer invalidate hui to **uske baad ki saari layers dobara build** hoti hain.

Isliye ordering: **kam badalne wali cheezein upar, zyada badalne wali neeche.**

```dockerfile
COPY *.csproj ./          # Dependencies file kam badalti hai -> upar
RUN dotnet restore        # Cache hit hoga jab tak csproj same hai
COPY . .                  # Source code roz badalta hai -> neeche
RUN dotnet publish -c Release -o /app
```

Ulta (`COPY . .` pehle) kiya to har code change par NuGet restore dobara chalega.

#### 8. ARG vs ENV, LABEL

- **ARG:** sirf build time par available, final container me nahi. Build version, base tag ke liye.
- **ENV:** build time aur runtime dono me available, image me persist hota hai (`docker inspect` me dikhta hai).
- **Secrets ko ARG ya ENV me kabhi mat daalo.** Ye `docker history` aur `docker inspect` me leak ho jate hain. BuildKit `--mount=type=secret` use karo (Module 7).
- **LABEL:** metadata (maintainer, git SHA, version) ke liye, e.g. `LABEL org.opencontainers.image.revision=$GIT_SHA`.

#### 9. Multi-architecture images

Ek tag (`aspnet:8.0`) ke peeche **manifest list (image index)** hoti hai jo har CPU architecture (`linux/amd64`, `linux/arm64`) ke liye alag image point karti hai. Docker pull ke time tumhari machine ke hisaab se sahi image chuntā hai.

Kyu matter karta hai: Apple Silicon (M1/M2) par arm64 image banti hai, jabki AWS ECS/EKS par amd64 node ho sakta hai, to `exec format error` aata hai. Solution: `docker buildx build --platform linux/amd64,linux/arm64`. AWS Graviton (arm64) par cost kam aata hai, isliye multi-arch build useful hai.

#### 10. Image security basics

- **Non-root user** se run karo (`USER app`). .NET 8 images me `app` user built-in hai.
- **Minimal base** (chiseled/distroless/alpine) = kam packages = kam vulnerabilities.
- **Version pin karo**: `aspnet:8.0.x` ya digest, `latest` nahi.
- **Scan karo**: Amazon ECR scan-on-push, Trivy, Docker Scout se CVEs check karo.
- **Secrets image me bake mat karo**, kyunki image layers koi bhi extract kar sakta hai.
- Trusted/official registry se hi pull karo.

#### 11. Image ka lifecycle (Build -> Ship -> Run)

1. **Build:** `docker build -t myapp:1.0 .` Dockerfile se image banti hai.
2. **Tag:** `docker tag myapp:1.0 <registry>/myapp:1.0`
3. **Push:** `docker push` registry (Docker Hub / ECR) me upload. Sirf **missing layers** upload hoti hain.
4. **Pull:** `docker pull` doosri machine / ECS / EKS par download.
5. **Run:** `docker run` image se container banata hai.

#### 12. Image Quick Cheat Sheet (revision ke liye)

- Image = read-only layers + config + manifest. Container = image + writable layer.
- Layers cache aur share hoti hain; delete se size kam nahi hota.
- Image ID = config hash, Digest = manifest hash.
- Tag mutable, digest immutable.
- Build SDK me, run runtime/chiseled me. Slow-changing instructions upar.
- `.dockerignore` zaroori hai. ARG build-time, ENV runtime. Secrets dono me nahi.
- Multi-arch = manifest list. Production: non-root, pinned version, scanned image.

### .NET Application ke liye Dockerfile: Step by Step (Line-by-line)

**Dockerfile kya hai?** Ek plain text file (naam exactly `Dockerfile`, koi extension nahi) jisme image banane ke instructions upar se neeche likhe hote hain. `docker build` isey padhta hai aur instruction by instruction layers banata hai.

#### Step 1: Sample project banao

```bash
dotnet new webapi -n Api -o Api          # Api naam ka ASP.NET Core Web API project Api/ folder me banata hai
cd Api                                   # Project folder ke andar jao; Dockerfile yahin banega
```

Folder structure:

```
Api/
  Api.csproj
  Program.cs
  Dockerfile        <- hum yeh banayenge
  .dockerignore     <- yeh bhi
```

#### Step 2: Sabse simple Dockerfile (single-stage, sirf samajhne ke liye)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o out
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "out/Api.dll"]
```

Yeh chalta hai, lekin final image me **poora SDK (~800 MB+)** aur source code reh jata hai. Production me yeh galat hai, isliye Step 3 ka multi-stage version use karte hain.

#### Step 3: Production style Dockerfile (multi-stage) aur har line ka matlab

```dockerfile
# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Api.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "Api.dll"]
```

| Line | Kya karti hai aur kyu |
|---|---|
| `# syntax=docker/dockerfile:1` | Parser directive. BuildKit ko batata hai ki Dockerfile ka latest stable syntax use kare (cache mounts, secrets jaise features ke liye). Optional hai, lekin **sabse pehli line** honi chahiye. |
| `FROM ...sdk:8.0 AS build` | **Stage 1 shuru.** Base image .NET 8 **SDK** hai (compiler, NuGet, CLI). `AS build` is stage ko naam deta hai taaki baad me `--from=build` se refer kar sakein. Yeh stage sirf building ke liye hai, final image me nahi jayega. |
| `WORKDIR /src` | Agli sab instructions ka working directory `/src` set karta hai (folder na ho to bana deta hai). `cd` ki tarah, bas har baar likhna nahi padta. |
| `COPY Api.csproj .` | Sirf project file (dependencies ki list) build context se `/src` me copy karta hai. **Source code abhi nahi.** Maqsad: restore layer ko cache karna. |
| `RUN dotnet restore` | NuGet packages download karta hai. Kyunki upar sirf csproj copy hui, jab tak csproj nahi badalti yeh layer **cache se aati hai** aur har code change par packages dobara download nahi hote. |
| `COPY . .` | Ab baaki saara source code copy hota hai (`.dockerignore` wali files chhod kar). Pehla `.` = host ka build context, doosra `.` = container ka current dir (`/src`). Yeh layer har code change par badlegi, isliye restore ke **baad** rakhi hai. |
| `RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false` | App compile karke deployable output `/app/publish` me banata hai. `-c Release` = optimized build, `-o` = output folder, `--no-restore` = restore dobara mat karo (upar ho chuka), `/p:UseAppHost=false` = native `Api` executable mat banao, sirf `Api.dll` chahiye kyunki hum `dotnet Api.dll` se chalate hain (chhota output). |
| `FROM ...aspnet:8.0 AS final` | **Stage 2 shuru.** Naya, fresh, chhota base: sirf ASP.NET **runtime** (SDK nahi). Stage 1 ki saari files yahan se gayab hain jab tak hum explicitly copy na karein. |
| `WORKDIR /app` | Runtime image me app ka folder `/app` set karta hai. |
| `ENV ASPNETCORE_URLS=http://+:8080` | Kestrel ko bolta hai ki container ke **saare network interfaces** par port 8080 sunna hai. `+` = all interfaces. 8080 isliye kyunki non-root user 1024 se neeche ke ports bind nahi kar sakta. |
| `EXPOSE 8080` | Sirf **documentation/metadata**: batata hai ki app 8080 par sunti hai. Port actually publish nahi karta; woh `docker run -p` se hota hai. |
| `COPY --from=build /app/publish .` | Stage 1 (`build`) se **sirf published output** `/app` me laata hai. Isi line se multi-stage ka fayda aata hai: SDK, source, obj/bin sab peeche reh jata hai. |
| `USER app` | Aage ka process **non-root** `app` user (UID 1654) se chalta hai. .NET 8 images me yeh user pehle se maujood hai. Security best practice. |
| `ENTRYPOINT ["dotnet", "Api.dll"]` | Container start hone par yeh command chalti hai. **Exec form** (JSON array) use kiya taaki `dotnet` PID 1 bane aur `docker stop` ka SIGTERM seedha app tak pahunche (graceful shutdown). `Api.dll` ka naam project ke **AssemblyName** se match hona chahiye. |

#### Step 4: `.dockerignore` banao (Api/ folder me)

```
**/bin
**/obj
.git
.vs
.vscode
*.md
Dockerfile*
docker-compose*
**/appsettings.Development.json
```

Yeh `COPY . .` me unwanted files (purane build output, git history) jaane se rokta hai. Bina iske `obj/` ki purani restore files Linux build ko corrupt kar sakti hain.

#### Step 5: Build, run aur verify karo

```bash
docker build -t api:1.0 .                          # Dockerfile se image build karta hai; -t naam:tag deta hai; . = build context
docker images api                                  # Banayi hui image aur uski size dikhata hai
docker run -d --name api -p 8080:8080 api:1.0      # Container background me chalata hai; host 8080 ko container 8080 se jodta hai
curl http://localhost:8080/weatherforecast         # Endpoint ko request bhej kar app test karta hai
docker logs api                                    # App ke console logs dikhata hai
docker exec api whoami                             # Container ke andar current user batata hai (app aana chahiye, root nahi)
docker stop api && docker rm api                   # Container ko gracefully rok kar delete karta hai
```

#### Step 6: Multi-project solution ke liye variation

Real projects me aksar solution me `Api`, `Core`, `Infrastructure` jaise multiple projects hote hain. Tab Dockerfile **solution root** me rakhte hain aur har csproj ko uske folder structure ke saath copy karte hain (restore ke liye):

```dockerfile
COPY ["Api/Api.csproj", "Api/"]
COPY ["Core/Core.csproj", "Core/"]
RUN dotnet restore "Api/Api.csproj"
COPY . .
RUN dotnet publish "Api/Api.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false
```

Build command solution root se chalta hai: `docker build -t api:1.0 -f Api/Dockerfile .` (`-f` Dockerfile ka path, last `.` build context). Poora production-ready version Module 3 me hai.

#### Dockerfile instruction reference (interview ke liye yaad rakho)

| Instruction | Kaam | Layer banata hai? |
|---|---|---|
| `FROM` | Base image choose karta hai (har stage ki shuruaat) | Haan (base layers) |
| `WORKDIR` | Working directory set karta hai | Nahi (metadata) |
| `COPY` | Host (ya doosre stage) se files copy karta hai | Haan |
| `ADD` | COPY jaisa + tar auto-extract + URL (avoid karo) | Haan |
| `RUN` | **Build time** par command chalata hai (restore, publish, apt-get) | Haan |
| `ENV` | Environment variable (build + runtime) | Nahi |
| `ARG` | Sirf build-time variable | Nahi |
| `EXPOSE` | Port document karta hai, publish nahi | Nahi |
| `USER` | Process ka user set karta hai | Nahi |
| `ENTRYPOINT` | Container ka fixed startup executable | Nahi |
| `CMD` | ENTRYPOINT ke default arguments | Nahi |
| `HEALTHCHECK` | Container health check command define karta hai | Nahi |
| `LABEL` | Image metadata (version, git SHA) | Nahi |

**RUN vs CMD vs ENTRYPOINT:** `RUN` image **banate waqt** chalta hai (result image me save). `CMD`/`ENTRYPOINT` container **start hone par** chalte hain.

#### Common errors aur fix

- **`Api.dll` not found / container turant exit:** csproj ka AssemblyName alag hai. `ls /app` karke asli dll naam dekho aur ENTRYPOINT me wahi likho.
- **`COPY failed: file not found`:** build context galat hai ya path Dockerfile ke relative nahi, **context** ke relative hota hai. `.dockerignore` ne file toh nahi hata di, yeh bhi check karo.
- **App chal rahi hai par `localhost:8080` khulta nahi:** `-p 8080:8080` bhoola, ya `ASPNETCORE_URLS` me `localhost` likha hai (container ke andar `+` ya `0.0.0.0` chahiye).
- **Har build par NuGet restore dobara chal raha hai:** `COPY . .` restore se pehle likh diya; order sudharo.
- **`permission denied` non-root par:** port 80 bind kar rahe ho; 8080 use karo, ya files ka ownership check karo.

### Key Commands

```bash
docker images                                             # Local machine par maujood images list karta hai
docker pull mcr.microsoft.com/dotnet/aspnet:8.0           # .NET 8 ASP.NET runtime image registry se download karta hai
docker tag myapp:latest myapp:1.0.3                       # Existing image ko ek naya tag deta hai
docker inspect <container>                                # Container ka detailed JSON metadata dikhata hai
docker history myapp:latest                               # Image banane wali layers aur unke commands dikhata hai
docker system df                                           # Docker images, containers, volumes ki disk usage dikhata hai
docker system prune -a                                     # Unused containers, networks, cache aur images clean karta hai
docker cp web:/etc/nginx/nginx.conf ./                    # Container se Nginx config current host directory me copy karta hai
```

**Image-specific extra commands:**

```bash
docker build -t myapp:1.0.3 .                             # Current folder ke Dockerfile se image build karta hai aur tag deta hai
docker build --no-cache -t myapp:dev .                    # Cache ignore karke poori image fresh build karta hai
docker images --digests                                   # Images ke saath unke registry digests (sha256) dikhata hai
docker images -f dangling=true                            # Sirf untagged (<none>) dangling images list karta hai
docker image inspect myapp:1.0.3 --format '{{.Id}}'       # Image ka ID (config hash) nikalta hai
docker image inspect myapp:1.0.3 --format '{{.Config.Env}} {{.Config.Entrypoint}}'  # Image me baked ENV aur ENTRYPOINT dikhata hai
docker history --no-trunc myapp:1.0.3                     # Har layer ki poori command aur size dikhata hai (leaked secrets bhi yahin dikh sakte hain)
docker image prune                                        # Sirf dangling images delete karta hai
docker image prune -a                                     # Kisi bhi container se use na ho rahi saari images delete karta hai
docker rmi myapp:1.0.3                                    # Image ka tag/image locally delete karta hai
docker save -o myapp.tar myapp:1.0.3                      # Image ko layers aur metadata ke saath tar file me save karta hai (offline transfer)
docker load -i myapp.tar                                  # save kari hui tar file se image load karta hai
docker buildx build --platform linux/amd64,linux/arm64 -t myapp:1.0.3 .   # Ek hi baar me multi-arch image build karta hai
docker manifest inspect mcr.microsoft.com/dotnet/aspnet:8.0   # Tag ke peeche ki manifest list (architectures) dikhata hai
```

**`save/load` vs `export/import`:** `save/load` **image** par kaam karte hain (layers, tags, history safe). `export/import` **container ka flattened filesystem** deta hai (layers, history, CMD/ENV metadata gayab). Image transfer ke liye `save/load` use karo.

### Practice Exercise

1. `docker history <image>` image ki layers aur unke build commands dikhata hai; isse layer count aur size inspect karo.
2. `docker run alpine sh -c "exit 3"` Alpine container ke andar command chala kar exit code 3 ke saath band hota hai; `docker inspect` se exit code dekho.
3. `docker run --restart=on-failure:3 alpine sh -c "exit 1"` failure par maximum 3 restart attempts set karta hai; `docker inspect` se restart count observe karo.
4. Same app ke liye `COPY . .` pehle aur `COPY *.csproj` + `restore` pehle, dono Dockerfile banao. Ek line code badal kar rebuild karo aur cache hit/miss compare karo.
5. `docker images` me `aspnet:8.0`, `sdk:8.0` aur `aspnet:8.0-alpine` ki size compare karo; `docker history` se bade layers dekho.
6. Ek RUN me file banao aur agle RUN me delete karo, phir `docker history` se dekho ki size kam nahi hui. Dono ek RUN me karke compare karo.
7. `docker save` se image tar banao, `docker rmi` karo, phir `docker load` se wapas laao.
8. Upar ka `Api` project banao, Step 2 (single-stage) aur Step 3 (multi-stage) dono Dockerfile se build karo aur `docker images` me size compare karo.
9. Step 5 ke saare commands type karke chalao; `docker exec api whoami` se confirm karo ki user `app` hai.
10. ENTRYPOINT me galat dll naam likhkar container chalao, `docker logs` aur exit code dekho, phir fix karo.

### Pitfalls

- **`latest` tag production me use karna**: rollback impossible ho jata hai aur pata nahi chalta kaunsa version chal raha hai.
- `docker system prune -a` running na hone wale sab images delete kar deta hai. Production server pe soch ke chalao.
- **Secrets ko ARG/ENV me daalna**: `docker history` aur `docker inspect` me dikh jate hain.
- **`.dockerignore` na hona**: `.git`, `bin/`, `obj/`, `.env` image me chale jate hain aur build slow hota hai.
- **Mac M1/M2 par build karke amd64 server par run karna**: `exec format error`; `--platform` use karo.
- **Delete karke size kam hone ki ummeed**: layers append-only hain, cleanup usi RUN me karo.
- Exit code 137 = OOMKilled ya SIGKILL, 143 = SIGTERM. Interview me yeh yaad rakho.

### Interview Questions

**Q1. CMD aur ENTRYPOINT me difference?** ENTRYPOINT container ka fixed command hai. CMD uske default args deta hai. `docker run image extra-args` CMD ko override karta hai, ENTRYPOINT ko nahi (jab tak `--entrypoint` na do).

**Q2. Container start hote hi exit kyu ho jata hai?** Kyunki PID 1 process khatam ho gaya ya crash hua. `docker logs` aur `docker inspect` (ExitCode) se check karo. Common reasons: wrong CMD, missing config, port already in use, app foreground me nahi chal rahi.

**Q3. Graceful shutdown kaise hota hai?** `docker stop` pehle SIGTERM bhejta hai, 10 sec (default) wait karta hai, phir SIGKILL. .NET me `IHostApplicationLifetime` aur `HostOptions.ShutdownTimeout` se in-flight requests complete karte hain. Exec form zaroori hai taaki signal mile.

**Q4. Image aur container me difference?** Image read-only layered template hai. Container uska running instance hai jisme ek extra writable layer hoti hai. Container delete hone par writable layer ka data jata hai, image safe rehti hai. Persistent data ke liye volume use karo.

**Q5. Image layers kaise kaam karti hain aur copy-on-write kya hai?** Har RUN/COPY/ADD ek read-only layer banata hai, overlay2 inhe merge karke ek filesystem dikhata hai. Container kisi existing file ko modify kare to wo file writable layer me copy hoti hai (CoW), neeche ki layer untouched rehti hai. Isi wajah se layers multiple containers me share hoti hain.

**Q6. Image size kaise kam karoge?** Multi-stage build (SDK me build, aspnet/chiseled me run), chhota base image, `.dockerignore`, RUN commands combine karke usi layer me cleanup, `--no-install-recommends`, unnecessary tools na rakhna, aur `dotnet publish -c Release` (optionally trimming) use karna. `docker history` se bade layers dhundo.

**Q7. Image ID aur digest me kya farq hai?** Image ID local image config ka SHA-256 hai. Digest registry me manifest ka SHA-256 hai. Deploy me digest ya unique tag use karo kyunki wo exact bytes ko pin karta hai.

**Q8. Dockerfile me instruction order cache ko kaise affect karta hai?** Ek layer invalidate hui to uske baad ki sab layers rebuild hoti hain. Isliye `csproj` copy + `dotnet restore` pehle, source `COPY . .` baad me rakhte hain.

**Q9. `aspnet`, `runtime`, `sdk` aur `runtime-deps` images me kya difference hai?** `sdk` build tools ke saath (build stage), `aspnet` web apps ka runtime, `runtime` non-web apps ka runtime, `runtime-deps` self-contained/AOT apps ke liye sirf OS dependencies. Production final stage me sdk kabhi nahi.

**Q10. Ek file delete karne ke baad bhi image ka size kam kyu nahi hua?** Kyunki file purani layer me abhi bhi hai, delete naye layer me sirf whiteout marker banata hai. Solution: install aur cleanup ek hi RUN me, ya multi-stage build.

**Q11. Dangling image kya hai?** Jis image ka koi tag nahi (`<none>:<none>`), aksar same tag se rebuild karne par bachi purani image. `docker image prune` se hatao.

**Q12. Multi-arch image kya hoti hai?** Ek tag ke peeche manifest list jo amd64/arm64 jaise alag architectures ki images ko point karti hai. `buildx --platform` se banti hai. Arch mismatch par `exec format error` aata hai.

**Q13. Secrets Dockerfile me kaise handle karoge?** ARG/ENV/COPY se kabhi nahi, kyunki `docker history`/layers me leak hote hain. BuildKit `RUN --mount=type=secret` build time ke liye, aur runtime me AWS Secrets Manager / SSM Parameter Store ya ECS task secrets se inject karo.

**Q14. `docker commit` kya hai aur kyu avoid karte hain?** Running container ki state se naya image banata hai. Reproducible nahi hai aur audit trail nahi hota, isliye hamesha Dockerfile se build karo (Infrastructure as Code approach).

**Q15. .NET app ka Dockerfile line by line samjhao.** Stage 1: `sdk` image par `WORKDIR`, csproj copy, `dotnet restore` (cached), poora source copy, `dotnet publish -c Release`. Stage 2: `aspnet` runtime image, `ENV ASPNETCORE_URLS` se port 8080, `COPY --from=build` se sirf publish output, `USER app` se non-root, aur exec form `ENTRYPOINT ["dotnet","Api.dll"]`. Har step ka reason bhi batao: restore cache, chhota image, security, graceful shutdown.

**Q16. Dockerfile me pehle csproj aur baad me poora source kyu copy karte hain?** Docker layer cache ke liye. Dependencies kam badalti hain, code roz. csproj copy + restore ek alag layer ban jati hai jo tab tak cache hit rehti hai jab tak packages same hain. `COPY . .` pehle kiya to har code change par restore dobara chalega.

**Q17. `RUN` aur `CMD`/`ENTRYPOINT` me difference?** `RUN` build time par chalta hai aur uska result image layer me save hota hai (jaise `dotnet publish`). `CMD`/`ENTRYPOINT` container start hone par chalte hain aur image me sirf metadata ke roop me rehte hain.

**Q18. `EXPOSE` port publish karta hai kya?** Nahi. Yeh sirf documentation hai. Host se access ke liye `docker run -p host:container` (ya Compose `ports`) chahiye. `EXPOSE` + `-P` flag se random host port map ho sakta hai.

**Q19. Final image me SDK kyu nahi rakhte?** SDK bada hai (~800 MB+), usme compiler aur tools hote hain jo attack surface badhate hain. Runtime ko sirf compiled output chahiye. Isliye multi-stage build me SDK sirf build stage me rehta hai.

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

**Dockerfile instructions ka matlab:** `FROM` base/build image chunta hai; `WORKDIR` agle steps ka working directory set karta hai; `COPY` build context se files image me laata hai; `RUN` build ke waqt command chalata hai; `EXPOSE` intended container port document karta hai (publish nahi karta); `ENV` default environment variable set karta hai; `USER` process ka user set karta hai; `ENTRYPOINT` container start hone par main process chalata hai.

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

1. `dotnet new webapi -n Api` .NET Web API project scaffold karta hai; upar wala Dockerfile likhkar `docker build -t api:1.0 .` se current folder ka image `api:1.0` tag ke saath build karo.
2. Sirf ek source file change karke dubara build karo. Dekho `restore` layer cached hai ya nahi.
3. csproj ka order tod ke (pehle `COPY . .` phir restore) build karo aur time compare karo.
4. Same app ko `aspnet:8.0`, `-alpine` aur `-jammy-chiseled` pe build karke `docker images` me size compare karo.
5. `docker run -p 8080:8080 api:1.0` container port 8080 ko host ke 8080 se publish karta hai; `curl localhost:8080/weatherforecast` HTTP endpoint ko request bhejkar response test karta hai.

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
# SQL Server ke data ko container ke bahar persist karne ke liye named volume banao
docker volume create sqldata
# SQL Server container chalao; credentials env vars me aur data sqldata volume me rakho
docker run -d --name sql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='Str0ng!Pass' \
  -v sqldata:/var/opt/mssql -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
docker volume ls                                           # Docker-managed volumes list karta hai
docker volume inspect sqldata                              # sqldata volume ka mount path aur metadata dikhata hai
docker run -v $(pwd)/logs:/app/logs api:1.0               # Host ke logs folder ko container ke /app/logs par mount karta hai
```

### Practice Exercise

1. SQL Server container volume ke saath chalao, table banao, container `rm` karo, dubara same volume se chalao. Data bacha?
2. Bina volume ke yahi karo aur difference dekho.
3. Bind mount se `appsettings.json` override karo aur app me change verify karo.
4. Volume ka backup lo: `docker run --rm -v sqldata:/data -v $(pwd):/backup alpine tar czf /backup/sql.tar.gz /data` ek temporary Alpine container chalata hai, `sqldata` ko `/data` aur current host directory ko `/backup` par mount karta hai, phir `/data` ka gzip-compressed tar archive host par banata hai; `--rm` container exit ke baad use hata deta hai.

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
# User-defined bridge network banao, jahan containers DNS name se ek doosre ko dhoondh sakte hain
docker network create appnet
# Database container ko appnet network par start karo (ellipsis baaki required options ka placeholder hai)
docker run -d --name db --network appnet mcr.microsoft.com/mssql/server:2022-latest ...
# API ko usi network par chalao, port publish karo, aur DB hostname `db` set karo
docker run -d --name api --network appnet -p 8080:8080 \
  -e ConnectionStrings__Default="Server=db;..." api:1.0
docker network inspect appnet                              # Network settings aur connected containers dikhata hai
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

**Q2. API container DB container se connect nahi ho raha, kaise debug karoge?** Same network check karo, DB ready hai ya nahi (health check), connection string me service name hai ya localhost, port sahi hai; `docker exec` running container me command chalata hai, `nc -zv db 1433` TCP port 1433 tak connection test karta hai, aur `curl` HTTP endpoint test karta hai. Logs dekho.

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
docker compose up -d --build       # Services build/create/start karta hai aur background me chalaata hai
docker compose ps                  # Compose project ke containers aur unki state dikhata hai
docker compose logs -f api         # api service ke logs live follow karta hai
docker compose exec api sh         # Running api service ke andar shell kholta hai
docker compose down -v             # Containers/network hataata hai aur named volumes bhi delete karta hai
docker compose config              # Variables/overrides ke baad final Compose configuration dikhata hai
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
# Private NuGet source ko secret mount se credential do; phir dependencies restore karo
RUN --mount=type=secret,id=nuget_pat \
    dotnet nuget add source https://feed.example/v3/index.json -n private \
    -u user -p $(cat /run/secrets/nuget_pat) --store-password-in-clear-text \
 && dotnet restore
```

```bash
DOCKER_BUILDKIT=1 docker build --secret id=nuget_pat,src=./pat.txt -t api:1.0 . # Secret mount ke saath BuildKit build chalao
```

### Practice Exercise

1. Apni image ko Trivy se scan karo: `trivy image api:1.0` image ke packages/dependencies me known vulnerabilities report karta hai. HIGH/CRITICAL CVEs list karo aur base image update karke dubara scan karo.
2. Container ko `--read-only --tmpfs /tmp --cap-drop=ALL --user 1654` ke saath chalao: root filesystem read-only, `/tmp` memory-backed writable, Linux capabilities removed, aur process UID 1654 par chalega. Agar app crash kare to dekho kya write kar rahi thi.
3. Image ko 220MB se kam karo (chiseled + ReadyToRun) aur before/after table banao.
4. `docker history --no-trunc <image>` poori layer history dikhata hai; check karo ki build commands me koi secret to record nahi hua.

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
docker logs --tail 100 -f --since 10m api                         # Pichhle 10 minute ke aakhri 100 logs live follow karta hai
docker stats                                                      # Running containers ka live CPU/memory/I/O usage dikhata hai
docker inspect --format='{{.State.OOMKilled}} {{.State.ExitCode}}' api # OOMKilled state aur exit code print karta hai
docker top api                                                     # api container ke andar chal rahe processes dikhata hai
docker events                                                     # Docker daemon ke real-time events stream karta hai
docker run --memory=256m --memory-swap=256m api:1.0               # Container memory aur memory+swap limit 256 MB set karta hai
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
4. `dotnet-counters` live .NET runtime counters (jaise GC aur allocation) monitor karta hai; `dotnet-dump` process memory dump collect/analyze karta hai. In tools ko sidecar/ephemeral container se attach karke GC metrics dekho.

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
# Region aur AWS account ID variables set karo; baaki commands inhe reuse karte hain
REGION=ap-south-1
ACCOUNT=123456789012

# ECR repo banao, push par image scan chalao aur tags ko immutable rakho
aws ecr create-repository --repository-name myapp/api --region $REGION \
  --image-scanning-configuration scanOnPush=true \
  --image-tag-mutability IMMUTABLE

# ECR se temporary login password lekar Docker CLI ko authenticate karo
aws ecr get-login-password --region $REGION | \
  docker login --username AWS --password-stdin $ACCOUNT.dkr.ecr.$REGION.amazonaws.com

# Current Git commit ka short SHA image version ke roop me lo
GIT_SHA=$(git rev-parse --short HEAD)
docker build -t myapp/api:$GIT_SHA . # Current folder ke Dockerfile se SHA-tagged image build karo
docker tag myapp/api:$GIT_SHA $ACCOUNT.dkr.ecr.$REGION.amazonaws.com/myapp/api:$GIT_SHA # Image ko ECR URI wala tag do
docker push $ACCOUNT.dkr.ecr.$REGION.amazonaws.com/myapp/api:$GIT_SHA # Image ko ECR me upload karo

aws ecr describe-image-scan-findings --repository-name myapp/api --image-id imageTag=$GIT_SHA # Is image tag ki scan findings dekho
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
# ECS cluster banao jisme production service rahegi
aws ecs create-cluster --cluster-name prod
# taskdef.json se task definition register karo
aws ecs register-task-definition --cli-input-json file://taskdef.json
# Fargate service banao: 2 tasks, private networking, ALB target aur rollback settings
aws ecs create-service --cluster prod --service-name api --task-definition api:1 \
  --desired-count 2 --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-a,subnet-b],securityGroups=[sg-123],assignPublicIp=DISABLED}" \
  --load-balancers targetGroupArn=arn:aws:elasticloadbalancing:...,containerName=api,containerPort=8080 \
  --deployment-configuration "deploymentCircuitBreaker={enable=true,rollback=true},minimumHealthyPercent=100,maximumPercent=200"

# Service ko task definition api:2 par deploy/update karo
aws ecs update-service --cluster prod --service api --task-definition api:2
# Service aur uske deployments ki current state dekho
aws ecs describe-services --cluster prod --services api
# ECS Exec se selected running task ke container me interactive shell kholo
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
      - run: dotnet test --configuration Release # Release mode me test projects chalao
      - uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: arn:aws:iam::123456789012:role/github-deploy
          aws-region: ap-south-1
      - id: ecr
        uses: aws-actions/amazon-ecr-login@v2
      - name: Build and push
        run: |
          # ECR registry, repository aur commit SHA ko mila kar image tag banao
          IMAGE=${{ steps.ecr.outputs.registry }}/myapp/api:${{ github.sha }}
          # Image build karke ECR registry me push karo
          docker build -t $IMAGE .
          docker push $IMAGE
          # IMAGE value ko baad ke workflow steps ke liye export karo
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
# NuGet package cache reuse karo, taaki har build me packages dobara download na hon
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "Api/Api.csproj"
```

**Multi-arch builds (buildx):** ARM64 (Graviton) pe Fargate/EC2 sasta padta hai (\~20% tak). Ek image dono architectures ke liye:

```bash
# Naya Buildx builder banao aur use current builder ke roop me select karo
docker buildx create --use
# Image ko amd64 aur arm64 dono architectures ke liye build karke registry me push karo
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
2. buildx se multi-arch image ECR me push karo; `docker buildx imagetools inspect <image>` registry ke image index/manifest me amd64 aur arm64 entries dikhata hai.
3. Graviton (arm64) Fargate task chalao (`cpuArchitecture: ARM64`) aur cost/performance compare karo.
4. Ek image ka tar export karo (`docker save <image> -o image.tar` image aur layers ko tar archive me likhta hai) aur andar ke layer blobs aur manifest manually dekho.
5. `kind` ya `minikube` pe apni image deploy karo (Deployment + Service + probes).
6. `docker run --init <image>` container ke aage init process chalata hai, jo signals forward aur zombie processes reap karta hai; iske saath aur bina ke SIGTERM behaviour dekho.

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
