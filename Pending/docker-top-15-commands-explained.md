# Top 15 Docker Commands — Detailed Explanation (Hinglish)

---

## 1. `docker build -t name:tag .`

Dockerfile se image banata hai.

- **`-t name:tag`** — image ko naam aur version tag deta hai (e.g., `myapp:v1`). Agar tag na do to `latest` default ho jata hai.
- **`.`** — ye **build context** hai, matlab current directory ka content Docker daemon ko bheja jata hai. Dockerfile mein `COPY` isi context se files uthata hai — context ke bahar ki file copy nahi kar sakte.

```bash
docker build -t myapp:v1 .
docker build -t myapp:v1 -f Dockerfile.prod .   # custom Dockerfile
```

**Interview point:** *"`.` current directory ka Dockerfile nahi, balki build context hai — isiliye `.dockerignore` important hai, warna `node_modules`/`.git` bhi daemon ko bhej dete hain aur build slow ho jata hai."*

---

## 2. `docker images`

Saari locally available images list karta hai — REPOSITORY, TAG, IMAGE ID, CREATED, SIZE ke saath.

```bash
docker images
docker images -q        # sirf IDs (scripting ke liye)
```

**Interview point:** *"Agar TAG mein `<none>` dikhe, wo **dangling image** hai — matlab naya build hua aur purani image ka tag chhin gaya. Ye disk space khata hai, `docker image prune` se clean hoti hai."*

---

## 3. `docker run -d -p 8080:80 --name x image`

Image se **naya container** create karke start karta hai.

- **`-d`** — detached mode, background mein chalega, terminal free rahega.
- **`-p 8080:80`** — port mapping. **Pehla host ka, doosra container ka.** Matlab browser mein `localhost:8080` khologe to container ke port `80` pe jayega.
- **`--name x`** — container ko custom naam. Na do to Docker random naam deta hai (jaise `nostalgic_tesla`).

```bash
docker run -d -p 8080:80 --name mynginx nginx
docker run -it ubuntu bash      # interactive shell ke saath
docker run --rm alpine echo hi  # exit hote hi auto-delete
```

**Interview point:** *"`run` hamesha naya container banata hai. Same naam se dobara `run` karoge to error aayega ki naam already in use hai — us case mein `docker start` chahiye."*

---

## 4. `docker ps` / `docker ps -a`

- **`docker ps`** — sirf **running** containers.
- **`docker ps -a`** — **saare** containers, stopped/exited bhi.

```bash
docker ps
docker ps -a
docker ps -q            # sirf IDs
```

**Interview point (bahut common):** *"Agar `docker run` ke baad `docker ps` mein container nahi dikh raha, matlab wo start hote hi exit ho gaya. `docker ps -a` se exit code dekho, phir `docker logs` se reason pata karo — usually main process crash ho gaya ya complete ho gaya."*

---

## 5. `docker logs -f <container>`

Container ke **stdout/stderr** logs dikhata hai.

- **`-f`** — follow mode, live logs stream karta rahega (jaise `tail -f`).

```bash
docker logs myapp
docker logs -f myapp
docker logs --tail 100 myapp        # last 100 lines
docker logs --since 10m myapp       # last 10 minutes
```

**Interview point:** *"Containerized app ko file mein log nahi karna chahiye — stdout pe log karo, taaki `docker logs` (aur production mein log aggregators) usse pick kar sakein. Ye 12-factor app principle hai."*

---

## 6. `docker exec -it <container> sh`

**Running** container ke andar command chalata hai — debugging ka sabse zyada use hone wala tool.

- **`-i`** — interactive (stdin open rakhta hai).
- **`-t`** — TTY allocate karta hai (proper terminal experience).
- **`sh`** — shell. Alpine images mein `bash` nahi hota, `sh` use karna padta hai. Ubuntu/Debian based mein `bash` chalega.

```bash
docker exec -it myapp sh
docker exec -it myapp bash
docker exec myapp ls /app        # ek single command, shell ke bina
```

**Interview point:** *"`docker exec` aur `docker attach` alag hain — `exec` naya process start karta hai container ke andar (safe), jabki `attach` main process ke stdin/stdout se jodta hai, aur exit karne pe container hi band ho sakta hai."*

---

## 7. `docker stop` / `start` / `restart <container>`

Container ka lifecycle manage karta hai.

- **`stop`** — **graceful** shutdown: SIGTERM bhejta hai, app ko cleanup ka time deta hai (default 10 seconds), phir SIGKILL.
- **`start`** — stopped container ko wapas chalata hai (**wahi container, apna data/state ke saath** — naya nahi banta).
- **`restart`** — stop + start ek saath.

```bash
docker stop myapp
docker stop -t 30 myapp     # 30 sec grace period
docker start myapp
docker restart myapp
docker kill myapp           # turant SIGKILL, no grace
```

**Interview point:** *"`stop` vs `kill` ka fark poochte hain — `stop` graceful hai (app connections close kar sakta hai, in-flight requests complete kar sakta hai), `kill` forceful hai. Production mein hamesha `stop` prefer karo."*

---

## 8. `docker rm -f <container>`

Container delete karta hai.

- Bina `-f` ke, container **stopped hona chahiye** — running container delete karne pe error aayega.
- **`-f`** — force: stop karke phir delete karta hai (ek step mein).

```bash
docker rm myapp             # sirf stopped container
docker rm -f myapp          # running ko bhi force delete
docker rm -f $(docker ps -aq)   # saare containers delete (⚠️ careful)
```

**Interview point:** *"Container delete hone pe uske andar ka data bhi chala jata hai — isliye persistent data hamesha volume mein rakho, container ke writable layer mein nahi."*

---

## 9. `docker rmi <image>`

Image delete karta hai.

- Agar koi container (running ya stopped) us image ko use kar raha hai, delete nahi hoga — pehle containers hataane padenge.

```bash
docker rmi myapp:v1
docker rmi -f myapp:v1          # force
docker rmi $(docker images -q)  # saari images (⚠️ careful)
```

**Interview point:** *"`docker rm` container delete karta hai, `docker rmi` image (`i` = image). Ye do alag cheezein hain — image blueprint hai, container uska running instance."*

---

## 10. `docker volume create / ls / rm`

Persistent data ke liye volumes manage karta hai. Container ephemeral hai — delete hone pe uska data chala jata hai. Volume container ke bahar rehta hai, isliye data persist karta hai.

```bash
docker volume create mydata
docker volume ls
docker volume inspect mydata      # actual host path dekhne ke liye
docker volume rm mydata
docker volume prune               # unused volumes clean

# Use karna
docker run -d -v mydata:/var/lib/mysql mysql
```

**Interview point:** *"Do tarike hain — **named volume** (`-v mydata:/path`) Docker manage karta hai, portable hai, production ke liye recommended. **Bind mount** (`-v /host/path:/path`) host ki specific directory mount karta hai, development mein live code reload ke liye useful but host filesystem pe depend karta hai."*

---

## 11. `docker network create / ls`

Container-to-container communication ke liye networks manage karta hai.

```bash
docker network create appnet
docker network ls
docker network inspect appnet     # kaunse containers connected hain
docker network rm appnet

# Use karna
docker run -d --name mydb --network appnet postgres
docker run -d --name myapp --network appnet myapp:v1
# ab myapp se "mydb" hostname se connect kar sakte ho
```

**Interview point (important):** *"Custom network banane ka main fayda **automatic DNS resolution** hai — us network ke containers ek doosre ko **container name se** reach kar sakte hain (`mydb:5432`). Default bridge network mein ye nahi milta, wahan IP se kaam chalana padta hai jo restart pe badal sakti hai."*

---

## 12. `docker inspect <container|image>`

Container ya image ka **complete metadata** JSON format mein deta hai — IP address, mounts, env variables, network settings, state, exit code, restart policy, sab kuch.

```bash
docker inspect myapp
docker inspect myapp | grep IPAddress
docker inspect -f '{{.State.Status}}' myapp                  # sirf status
docker inspect -f '{{.NetworkSettings.IPAddress}}' myapp      # sirf IP
```

**Interview point:** *"Debugging mein kaam aata hai — jaise container ki actual IP pata karna, ya check karna ki volume sahi mount hua ya nahi, ya exit code dekhna jab container crash hua ho. `-f` (format) flag se specific field nikal sakte ho poora JSON padhe bina."*

---

## 13. `docker stats`

Saare running containers ka **live resource usage** — CPU%, memory usage/limit, network I/O, disk I/O.

```bash
docker stats
docker stats myapp              # sirf ek container
docker stats --no-stream        # ek snapshot, live update nahi
```

**Interview point:** *"Ye quick troubleshooting ke liye hai — jaise koi container memory leak kar raha hai ya CPU spike ho raha hai. Proper production monitoring ke liye ye kaafi nahi, wahan Prometheus/Grafana ya APM tools use karte hain."*

---

## 14. `docker system prune -a`

Unused Docker resources clean karke **disk space free** karta hai.

- Bina `-a`: stopped containers, unused networks, **dangling** images (`<none>` tagged) delete karta hai.
- **`-a`**: upar wala sab + **saari unused images** (jo kisi container se attached nahi hain).

```bash
docker system df                    # pehle dekho kitni space kis cheez ne li
docker system prune                 # safe cleanup
docker system prune -a              # aggressive cleanup
docker system prune -a --volumes    # ⚠️ volumes bhi — data loss risk
```

**Interview point:** *"CI/CD servers pe ye regularly chalana padta hai, warna har build nayi image layers chhod jata hai aur disk bhar jati hai. `--volumes` flag se bachna chahiye jab tak pakka na ho ki koi zaroori data volume mein nahi hai."*

---

## 15. `docker-compose up -d` / `down`

Multi-container applications ko ek YAML file (`docker-compose.yml`) se manage karta hai — app + database + cache sab ek command se.

- **`up -d`** — saari services build/create/start karo, background mein.
- **`down`** — saare containers stop + remove karo (networks bhi).

```bash
docker-compose up -d
docker-compose up -d --build     # rebuild karke start
docker-compose down
docker-compose down -v           # ⚠️ volumes bhi delete
docker-compose ps
docker-compose logs -f web       # ek service ke logs
docker-compose exec web sh       # ek service mein shell
```

**Interview point:** *"Compose single-host orchestration ke liye hai — local development aur small deployments mein perfect. Multi-host, auto-scaling, self-healing chahiye to Kubernetes pe jaana padta hai. Compose ka `depends_on` sirf **start order** control karta hai, ye wait nahi karta ki dependency actually ready ho — uske liye healthcheck configure karna padta hai."*

---

## Sabse Common Confusion Points (Interview Gotchas)

| Confusion | Clarity |
|---|---|
| `docker rm` vs `docker rmi` | `rm` = container, `rmi` = image (`i` for image) |
| `-p 8080:80` | Pehla **host**, doosra **container** |
| `docker run` vs `docker start` | `run` = naya container banao, `start` = existing ko chalao |
| `docker stop` vs `docker kill` | `stop` graceful (SIGTERM→SIGKILL), `kill` turant (SIGKILL) |
| `docker exec` vs `docker attach` | `exec` naya process (safe), `attach` main process se jodta hai (risky) |
| `bash` vs `sh` | Alpine images mein `bash` nahi hota, `sh` use karo |
| `-f` flag | Logs mein = follow, `rm` mein = force, `build` mein = file |

---

## Last-Minute Revision Block

```bash
docker build -t name:tag .            # build image
docker images                          # list images
docker run -d -p 8080:80 --name x img  # create + start container
docker ps / docker ps -a               # list running / all containers
docker logs -f <container>             # follow logs
docker exec -it <container> sh         # shell inside container
docker stop / start / restart <c>      # lifecycle
docker rm -f <container>               # delete container (force)
docker rmi <image>                     # delete image
docker volume create / ls / rm         # persistent storage
docker network create / ls             # container networking
docker inspect <container|image>       # full metadata
docker stats                           # live resource usage
docker system prune -a                 # cleanup disk space
docker-compose up -d / down            # multi-container management
```
