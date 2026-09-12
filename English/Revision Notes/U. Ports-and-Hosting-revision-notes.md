# Ports & Hosting — Quick-Revision Notes

> Quick-revision notes derived from *U. Ports-and-Hosting-Explained-Simply*. Covers every section in the same order, in Q&A + bullet form. These notes alone should be enough to brush up the topic without opening the guide.

---

## 1. The Core Analogy

- **IP = building address. Port = flat number. App = the resident.**
- A courier needs both — "221B Baker Street" is useless if 40 families live there.
- **One resident per flat** → two processes cannot bind the same port (same IP + protocol).

```
https://myapp.com:443/orders
  scheme    host   port  path
  (rules)  (machine) (app) (endpoint inside app)
```

**One line:** IP finds the machine, port finds the app on that machine.

> **Real life:** Company landline `+91-120-4567890` → reception; extension `221` → one person. Number = IP, extension = port, reception = OS.

---

## 2. What a Port Is

**Q: What is a port, technically?**
A: A **16-bit number (0–65535)**. Nothing physical. It's a label the OS uses to route an incoming packet to the right process.

- **Binding** = your app telling the OS "give me everything addressed to port 5000."
- First come, first served — one owner at a time per (IP, protocol, port).
- **TCP 443 and UDP 443 are different doors.** Web = TCP, except HTTP/3 which uses UDP 443.

> **Real life:** Your laptop right now — API on 5001, SQL Server on 1433, Redis on 6379, all on `127.0.0.1`. F5 a second copy of the API without stopping the first → instant `address already in use`.

---

## 3. Why 80 and 443

| Port | Protocol | Encryption | Certificate |
|---|---|---|---|
| 80 | HTTP | none — plain text | not needed |
| 443 | HTTPS | TLS | required |

- They are **well-known ports** — an IANA convention, nothing more. Browsers assume 80 for `http://`, 443 for `https://`.
- **There is nothing special about the digits.** The real difference between 80 and 443 is **TLS**.
- On port 80, a posted password is readable by anyone on the path. On 443, only ciphertext.
- A TLS certificate proves "I am really myapp.com", signed by a CA the browser already trusts.
- **Port 80's only remaining job:** redirect `http://` visitors to `https://`, plus serve ACME/Let's Encrypt renewal challenges.

> **Real life:** Airport Wi-Fi. Log in over `http://` and anyone running Wireshark reads `password=admin123`. Same login over `https://` and they capture only ciphertext. Same network, same password — port 80 vs 443 is the only difference. The padlock = "443 with a valid cert".

---

## 4. Why Real Sites Show No Port

- The browser always sends a port; it just **hides the default one** for the scheme.
- `google.com` → `https://google.com:443`. Open `https://google.com:443` yourself and the `:443` gets stripped from the address bar.
- **Rule:** default port → invisible. Non-default port → part of your public URL forever.
- That's the #1 reason production sits behind 80/443 — nobody hands a customer `https://shop.com:5001`.

> **Real life:** Paste `https://timesheet.mycompany.com` in Teams — everyone opens it. Paste `http://localhost:5000/swagger` — nobody can, and even with your IP they must type `:5000`. Both URLs have a port; only the default one is invisible.

---

## 5. Port Cheat Sheet

**Ranges**

| Range | Name | Note |
|---|---|---|
| 0–1023 | Well-known / privileged | **Linux: needs root to bind** |
| 1024–49151 | Registered | app/dev servers (5000, 8080) |
| 49152–65535 | Dynamic / ephemeral | OS picks for *outgoing* calls — never hard-code |

**Numbers to know:** 80 HTTP · 443 HTTPS · 5000/5001 Kestrel dev · 8080/8443 unprivileged pair · 1433 SQL Server · 5432 Postgres · 3306 MySQL · 6379 Redis · 27017 Mongo · 5672/15672 RabbitMQ · 22 SSH · 53 DNS · 25/587 SMTP.

**Q: How can one server serve 10,000 users on a single port 443?**
A: A connection is identified by the **4-tuple** (client IP + client port + server IP + server port). Each client gets its own random *ephemeral* port, so every connection is unique even though the server port is shared.

```
Your app 10.0.5.23:52741  ---->  SQL Server 10.0.9.10:1433
         ephemeral (random)               well-known (fixed)
```

- **Ephemeral port exhaustion** = the classic incident from `new HttpClient()` per request. Fix: reuse `HttpClient` / `IHttpClientFactory`.

> **Real life:** `netstat -ano | findstr ESTABLISHED` with 10 tabs open → remote port almost always 443, local port different every row (52741, 52742…). Incident version: nightly export with `new HttpClient()` per record — fine on 200 test rows, `SocketException` past ~16,000 in prod. Fix: `IHttpClientFactory`.

---

## 6. Who Owns the Port in .NET

| | .NET Framework (ASP.NET on IIS) | .NET Core / .NET 5+ |
|---|---|---|
| Web server | IIS | **Kestrel, inside your own process** |
| Your app is | a plugin loaded by IIS | the web server itself |
| Owns the port | IIS | your app |
| Runs without IIS | no | yes — `dotnet MyApp.dll` |
| Cross-platform | no | yes (Windows/Linux/Docker) |

**IIS hosting models (Windows):**
- **In-process** — default since .NET Core 3.0; request enters your app inside the IIS worker process, no extra hop, faster.
- **Out-of-process** — IIS owns 80/443 and reverse-proxies to Kestrel on a random local port.

**Either way:** the public port belongs to whatever faces the internet; your app's port is an internal detail.

> **Real life:** Old app = install IIS, add the role, create a site in IIS Manager, set a binding, only then it serves. .NET 8 rewrite = copy the publish folder to a bare Linux VM, `dotnet MyApp.dll`, answers on 5000. No IIS installed at all.

---

## 7. Where the Port Comes From (6 Places)

| # | Mechanism | Example |
|---|---|---|
| 1 | Code — `ConfigureKestrel` | `options.ListenAnyIP(8080)` |
| 2 | `appsettings.json` | `"Kestrel": { "Endpoints": { "Http": { "Url": "http://*:8080" } } }` |
| 3 | `launchSettings.json` | `"applicationUrl": "https://localhost:7043"` — **dev tooling only** |
| 4 | Env var | `ASPNETCORE_URLS="http://+:8080;https://+:8443"` |
| 5 | Command line | `dotnet MyApp.dll --urls "http://0.0.0.0:8080"` |
| 6 | Nothing set | falls back to `http://localhost:5000` + `https://localhost:5001` |

- Among the **configuration-driven** options: command line > env var > `appsettings.json`.
- **Gotcha:** `--urls`/`ASPNETCORE_URLS` and explicit `ConfigureKestrel().Listen*()` are two different mechanisms — calling `Listen*()` in code takes over and the URLs value is **ignored** (Kestrel logs a warning).
- `launchSettings.json` is never deployed — zero effect in production. It's why every new project opens on a different random port.

**Binding addresses — memorise this:**

| Binding | Reachable from |
|---|---|
| `localhost` / `127.0.0.1` / `ListenLocalhost` | this machine only |
| `0.0.0.0` / `*` / `+` / `ListenAnyIP` | all interfaces — outside world |

> Works locally, "connection refused" in Docker or from a colleague's machine? **99% it's bound to `localhost` instead of `0.0.0.0`.**

> **Real life:** One artifact, three environments — laptop `7043` (launchSettings), QA container `ASPNETCORE_URLS=http://+:8080` (compose), prod same var from k8s YAML. Nothing rebuilt between QA and prod; a prod port change is a config edit + restart, not a release.

---

## 8. Dev vs Prod

| | Development | Production |
|---|---|---|
| Port | 5000/5001, 7043… whatever's free | 80/443 externally, app itself usually 8080 |
| Owns 80/443 | nobody | IIS / nginx / LB / ingress |
| Certificate | `dotnet dev-certs https --trust` (self-signed) | real CA cert (Let's Encrypt, ACM, corporate CA) |
| Port in URL | always visible | never |

**Why dev avoids 80/443:** something may already own 80; Linux/macOS would need `sudo` on every debug run; parallel projects must differ.

> **Real life:** API on `7043` and Angular BFF on `7156` both debugging in VS at the same time — hard-code either to port 80 and the second F5 dies. And `dotnet dev-certs https --trust`, run once, is what stopped Chrome's "not private" warning on localhost.

---

## 9. Reverse Proxy Pattern

```
Internet -> proxy/LB  :80  -> redirect to 443
                      :443 -> TLS cert lives here, decrypts
              |          |          |     (plain HTTP, private network)
              v          v          v
        Kestrel:8080  Kestrel:8080  Kestrel:8080
```

- **Only the proxy needs 80/443.** N app copies all reuse 8080 because they're on different hosts/containers.
- **TLS terminates at the proxy** — one place to hold and renew certificates; app speaks plain HTTP internally.
- Scaling out doesn't change ports.
- Proxy = nginx, IIS, Apache, YARP, AWS ALB, Azure App Gateway, k8s ingress — same role.

**Q: What must you configure in .NET when behind a proxy?**

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
```

Without it: wrong client IPs in logs, `http://` links generated on `https://` pages, redirect loops.

> **Real life:** `api.company.com` on an ALB, three EC2 instances all on 8080, ACM cert attached to the ALB only. Cert auto-renews → nothing changes on the three servers. Autoscaling adds a 4th → also 8080. Old model: install and renew the cert on every IIS box.

---

## 10. Docker — Two Ports

```dockerfile
ENV ASPNETCORE_URLS=http://+:8080   # port INSIDE the container
EXPOSE 8080                          # documentation only — opens nothing
```

```bash
docker run -p 443:8080 myapp     # read as  outside : inside
```

- **`EXPOSE` publishes nothing** — only `-p` / compose / k8s mapping opens a path.
- **Never bind `localhost` inside a container** — it means the container itself, so mapped traffic never reaches the app → instant connection reset. Most common containerised-.NET bug.
- **.NET 8+ images default to 8080** (was 80) because containers now run as non-root, which can't bind 80.

> **Real life:** Comparing two builds — `docker run -d -p 5001:8080 myapi:v1` and `docker run -d -p 5002:8080 myapi:v2`. Both internally 8080, no clash (separate container networks); the clash would be reusing host port 5001.

---

## 11. Error → Cause Table

| Error | Cause | Fix |
|---|---|---|
| `Failed to bind… address already in use` | another process owns the port (often your own previous run) | kill it or change port |
| `ERR_CONNECTION_REFUSED` | nothing listening — machine actively said "no such flat" | app running? right port? bound to `0.0.0.0`? |
| hangs then times out | something listening, but firewall silently dropped the packet | open port in firewall / NSG / SG |
| `ERR_SSL_PROTOCOL_ERROR` | spoke HTTPS to a plain-HTTP port — right door, wrong language | use the matching port |
| `Permission denied` on port 80 (Linux) | ports <1024 are privileged | use 8080 + proxy |
| `502 Bad Gateway` | proxy is fine, app behind it is dead or on another port | check app + proxy upstream port |
| endless `http → https` loop | proxy terminated TLS, app sees HTTP and redirects forever | `UseForwardedHeaders` |

**Who owns a port:**
```powershell
netstat -ano | findstr :5000 ; Get-Process -Id <PID>   # Windows
```
```bash
lsof -i :5000        # or: ss -tulpn | grep 5000        # Linux/macOS
```

> **Real life:** Closed the browser tab instead of pressing Stop → `MyApi.exe` still alive → next F5: `Failed to bind… 5001 already in use`. `netstat -ano | findstr :5001` → PID → end it in Task Manager. ~90% of "port in use" tickets are this.

---

## 12. HTTPS Redirection & HSTS

```csharp
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseHttpsRedirection();
```

- **`UseHttpsRedirection`** — catches the port-80 request and answers 307/308 → `https://`. Needs to know the HTTPS port (inferred from the configured HTTPS endpoint, or set `ASPNETCORE_HTTPS_PORT` / `HttpsRedirectionOptions.HttpsPort`).
- **`UseHsts`** — sends `Strict-Transport-Security`; after the first visit the **browser itself** refuses plain HTTP for that domain, closing the interceptable-first-request window.
- **Never enable HSTS in dev** — it's cached per host and `localhost` is shared by all your projects. (Template guards it with `IsDevelopment()` for exactly this reason.)
- Behind a TLS-terminating proxy, redirection is usually done at the proxy; `UseForwardedHeaders` is mandatory if you also keep it in the app.

> **Real life:** Someone dropped the `IsDevelopment()` guard around `UseHsts()`. Months later a *different* plain-HTTP project on `localhost:5000` wouldn't load in Chrome (fine in Postman/Firefox) — HSTS is cached per host and `localhost` is shared by every project. Fix: `chrome://net-internals/#hsts` → delete `localhost`, restore the guard.

---

## 13. Ports vs Firewalls

```
(1) DNS resolves?  (2) cloud SG/firewall allows 443?  (3) OS firewall allows 443?
(4) anything LISTENING on 443?  (5) is it your app, bound to 0.0.0.0?
```

**The diagnostic that matters:**
- **Refused instantly** → nothing listening (layer 4/5) → **your app's problem**.
- **Hangs then times out** → firewall dropped it silently (layer 2/3) → **the network's problem**.

> **Real life:** New EC2 box — on the instance `curl http://localhost:8080/health` = 200, app logs clean; from your laptop the browser hangs 30s then times out, nothing in the logs. The *timeout* (not a refusal) said "network, not app": the security group allowed only SSH 22, never 443.

---

## 14. Interview One-Liners

- **Port?** 16-bit number the OS uses to route a connection to the right process. IP = machine, port = app on it.
- **80 vs 443?** Same HTTP; 443 wraps it in TLS, needs a certificate. Both are conventions, so browsers assume and hide them.
- **Why 8080 in prod, not 80?** <1024 is privileged on Linux and containers run non-root; a proxy/LB owns 80/443, terminates TLS, forwards to Kestrel on 8080.
- **Two apps, one port?** Not on the same IP+protocol — but one app on 443 serves thousands of clients because connections are identified by the full 4-tuple.
- **ASP.NET → ASP.NET Core hosting change?** IIS-plugin model → Kestrel embedded in your app, which owns the port; self-hosted, cross-platform, IIS/nginx become optional proxies.
- **In-process vs out-of-process IIS?** In-process (default since 3.0) runs inside the IIS worker — faster, no extra hop. Out-of-process reverse-proxies to Kestrel on a local port.
- **Where can the port come from?** `--urls`, `ASPNETCORE_URLS`, `Kestrel:Endpoints` in `appsettings.json`, or `ConfigureKestrel` in code (which overrides the URL-based ones); else 5000/5001. `launchSettings.json` = dev tooling only.

> **Real life —** the 20-second answer to "how is your app hosted?", which is the question actually asked:
>
> *"Containerised .NET 8. Kestrel on 8080 inside the container as non-root, so no privileged port. An ALB owns 443 with an ACM cert and terminates TLS; port 80 only redirects. Since TLS terminates at the LB we enable `UseForwardedHeaders` so client IPs and scheme are right in the logs. The port comes from `ASPNETCORE_URLS`, so the same image runs everywhere."*
>
> It never mentions `launchSettings.json` — that's the tell that you've actually deployed something.

---

## 15. Six-Line Recap

1. IP = building, port = flat, app = resident. One resident per flat.
2. 80/443 are conventions; the real difference is TLS, not the digits.
3. A non-default port becomes part of your URL forever → prod hides behind 80/443.
4. In .NET Core, **your app owns the port** (Kestrel is inside your exe).
5. Bind `0.0.0.0`, not `localhost`, whenever anything outside the machine/container must reach you.
6. **Refused = nothing listening. Timeout = firewall ate it.**

> **Real life:** one Tuesday morning — F5 fails "5001 in use" (line 1); colleague can't reach your dev API over the LAN (line 5); QA sees `:5001` in the address bar (line 3); prod URL hangs from outside the office (line 6). Four different-looking problems, all four on this list.
