# Ports (80, 443, 5000...) & Hosting — Explained in the Simplest Way

Audience: anyone who has run a .NET web app and wondered *"why does it say `localhost:5001`, and why does the real website not need a number?"*
Goal: after this file, you should be able to explain ports to a junior dev **without** using the word "socket".

## Table of Contents

1. [The One Analogy That Explains Everything](#1-the-one-analogy-that-explains-everything)
2. [What a Port Actually Is](#2-what-a-port-actually-is)
3. [Why 80 and 443 Specifically?](#3-why-80-and-443-specifically)
4. [Why You Never Type the Port on Real Websites](#4-why-you-never-type-the-port-on-real-websites)
5. [The Full Port Cheat Sheet](#5-the-full-port-cheat-sheet)
6. [Now the .NET Part — Who Owns the Port?](#6-now-the-net-part--who-owns-the-port)
7. [Where Ports Are Configured in a .NET App (All 6 Places)](#7-where-ports-are-configured-in-a-net-app-all-6-places)
8. [Development vs Production — Two Different Worlds](#8-development-vs-production--two-different-worlds)
9. [The Reverse Proxy Pattern (This Is How Real Apps Are Hosted)](#9-the-reverse-proxy-pattern-this-is-how-real-apps-are-hosted)
10. [Docker & Containers — Two Ports, Not One](#10-docker--containers--two-ports-not-one)
11. [Common Errors and What They Really Mean](#11-common-errors-and-what-they-really-mean)
12. [HTTP to HTTPS Redirection & HSTS](#12-http-to-https-redirection--hsts)
13. [Ports vs Firewalls / Security Groups](#13-ports-vs-firewalls--security-groups)
14. [Interview One-Liners](#14-interview-one-liners)
15. [Mental Model Recap](#15-mental-model-recap)

---

## 1. The One Analogy That Explains Everything

Think of a big apartment building.

```
   IP address     =  the building's street address   ->  "10.0.5.23"
   Port number    =  the flat/door number inside     ->  "443"
   Application    =  the person living in that flat  ->  your .NET app
```

- A courier needs **both** to deliver a parcel. "Deliver to 221B Baker Street" is useless if 40 families live there. You need **221B Baker Street, Flat 443**.
- One building (one server) can hold **many flats** (many apps), each with a different number.
- **Two families cannot live in the same flat at the same time.** That is literally the "port already in use" error.

So:

```
https://myapp.com:443/orders
|      |          |    |
|      |          |    +-- path      -> which page/endpoint inside the app
|      |          +------- PORT      -> which app on that machine
|      +------------------ host      -> which machine (resolved to an IP by DNS)
+------------------------- scheme    -> which language/rules to speak (HTTP or HTTPS)
```

**IP finds the machine. Port finds the app on that machine.**

> **Real life —** You dial a company's landline `+91-120-4567890` and reach the reception of the whole building. Then you dial extension `221` to reach one specific person. The phone number is the **IP**; the extension is the **port**; reception is the **OS** transferring your call to the right desk. Nobody can reach that person with the phone number alone.

---

## 2. What a Port Actually Is

A port is just **a number between 0 and 65535**. Nothing physical. No wire, no hardware, no cable.

It is a label the operating system uses to decide *which running program* should receive an incoming network message.

```
                 One server, one IP: 10.0.5.23
   +--------------------------------------------------------+
   |  Operating System (the receptionist)                   |
   |                                                        |
   |   incoming: "for port 443"  --->  .NET Web App         |
   |   incoming: "for port 1433" --->  SQL Server           |
   |   incoming: "for port 6379" --->  Redis                |
   |   incoming: "for port 22"   --->  SSH                  |
   |   incoming: "for port 9999" --->  nobody home => ERROR |
   +--------------------------------------------------------+
```

Two things to lock in:

1. **The OS is the receptionist.** Your app says "I want to *listen* on port 5000" (this is called *binding*). From then on, the OS hands every packet addressed to 5000 to your app.
2. **A port is not a resource you buy or create.** You just claim a number — first come, first served, one owner at a time (per IP + protocol).

> Small detail worth knowing: TCP port 443 and UDP port 443 are *different* doors. Almost all web traffic is TCP, so you can ignore this until you meet HTTP/3, which uses UDP 443.

> **Real life —** Look at your laptop right now. Visual Studio is debugging your API on **5001**, SQL Server is on **1433**, a Redis container is on **6379** — all on the same `127.0.0.1`. One IP, three apps, three numbers, zero conflict. Now hit F5 a second time on the same API without stopping the first run: instantly `address already in use`. That one experiment is this whole section.

---

## 3. Why 80 and 443 Specifically?

Because **the world agreed** on them. That is honestly the whole reason.

| Port | Used for | Meaning |
|---|---|---|
| **80** | HTTP | plain text, unencrypted |
| **443** | HTTPS | HTTP wrapped in TLS = encrypted |

These are called **well-known ports** — an official list (maintained by IANA) of "if you offer this kind of service, listen here so everyone can find you without asking."

- The browser sees `http://` and silently assumes **port 80**.
- The browser sees `https://` and silently assumes **port 443**.

That is the *entire* significance. There is nothing magical inside the numbers 80 or 443. If tomorrow everyone agreed HTTPS lives on 9999, browsers would default to 9999 and nothing else would change.

**The difference between 80 and 443 is not the number — it is TLS.**

```
Port 80  (HTTP)                    Port 443 (HTTPS)
------------------------------     ---------------------------------------
Browser -> "GET /login"            Browser -> [TLS handshake: certificate,
password=admin123  (readable!)                 agree on encryption keys]
                                            -> #$%^&*@!<encrypted bytes>
Anyone on the Wi-Fi can read it.   Anyone on the Wi-Fi sees only garbage.
```

Port 443 also requires a **TLS certificate** — a file that proves "I really am myapp.com", signed by an authority the browser already trusts. Port 80 needs no certificate, which is exactly why it is unsafe.

Today, port 80 is kept alive for one job only: **to catch people who typed `http://` and redirect them to `https://`.** (Plus serving Let's Encrypt certificate-renewal challenges.)

> **Real life —** Free airport Wi-Fi. You log into a site that is still on `http://`. Anyone on that same Wi-Fi running Wireshark sees your `password=admin123` in readable text — they did not "hack" anything, they just watched. Log into a `https://` site on the same Wi-Fi and they capture the same packets, but all they see is encrypted bytes. Same Wi-Fi, same laptop, same password — the only difference is port 80 vs 443. The padlock in your address bar is literally "I'm on 443 with a valid certificate."

---

## 4. Why You Never Type the Port on Real Websites

You *do* — the browser just hides it.

| You type | Browser actually connects to |
|---|---|
| `google.com` | `https://google.com:443` |
| `http://example.com` | `http://example.com:80` |
| `https://myapp.com:8443` | `https://myapp.com:8443` (non-default, so it must be shown) |

Try it: open `https://google.com:443` — it works, and the browser strips the `:443` from the address bar because it is the default.

So the rule is simple:

> **If your app listens on the default port for its scheme, users never see a port. If it listens anywhere else, the port becomes part of your public URL forever.**

This is the number one reason production apps are put behind port 80/443 — nobody wants to hand a customer `https://shop.com:5001`.

> **Real life —** You paste `https://timesheet.mycompany.com` in a Teams chat; everyone clicks and it opens. You paste `http://localhost:5000/swagger` from your running API and nobody can open it — and even after you give them your machine's IP, they must type `:5000` by hand. The first URL had a port too (443); it was just invisible because it was the default.

---

## 5. The Full Port Cheat Sheet

**The three ranges**

| Range | Name | Who uses it | Note |
|---|---|---|---|
| 0 – 1023 | Well-known / privileged | HTTP, HTTPS, SSH, SQL... | On Linux, **needs root/admin** to bind |
| 1024 – 49151 | Registered | apps, dev servers (5000, 8080) | Anyone can bind |
| 49152 – 65535 | Dynamic / ephemeral | OS picks these for *outgoing* calls | Never hard-code these |

That privileged rule is a real-world gotcha: a .NET app running as a normal user **cannot** bind port 80 or 443 on Linux. It binds 8080 instead, and something in front (nginx, IIS, a load balancer, Kubernetes) owns the real 80/443.

**Ports a .NET developer meets constantly**

| Port | Service |
|---|---|
| 80 | HTTP |
| 443 | HTTPS |
| 5000 / 5001 | Kestrel dev defaults (5000 HTTP, 5001 HTTPS) |
| 8080 / 8443 | The classic "unprivileged HTTP / HTTPS" pair (containers) |
| 1433 | SQL Server |
| 5432 | PostgreSQL |
| 3306 | MySQL |
| 6379 | Redis |
| 27017 | MongoDB |
| 5672 / 15672 | RabbitMQ (AMQP / management UI) |
| 22 | SSH |
| 53 | DNS |
| 25 / 587 | SMTP (mail) |

**Ephemeral ports — the half nobody explains**

When *your* app calls a database, your app also gets a port — a random high one:

```
Your app  10.0.5.23:52741   ------>   SQL Server  10.0.9.10:1433
          ^^^^^^^^^^^^^^^^                        ^^^^^^^^^^^^^^^
          random ephemeral port                   fixed, well-known port
          (the "return address")                  (the "known door")
```

A connection is always a **pair** of endpoints. This is why one server can serve 10,000 users on a single port 443 — each user's connection is unique because *their* port differs.

> This is also the root of the classic production incident "ephemeral port exhaustion" — code that creates a new `HttpClient` per request leaks these ports until the machine runs out. Hence: reuse `HttpClient` / use `IHttpClientFactory`.

> **Real life —** Open ten browser tabs, then run `netstat -ano | findstr ESTABLISHED`. You will see dozens of rows where the *remote* port is almost always `443` but your *local* port keeps changing — 52741, 52742, 52743. That column is the ephemeral port range in action.
>
> And the incident version: a nightly export job created `new HttpClient()` inside a `foreach` over records. It ran fine in testing with 200 rows. In production, somewhere past ~16,000 rows it started throwing `SocketException: Only one usage of each socket address is normally permitted` — every dead `HttpClient` was still holding an ephemeral port in `TIME_WAIT`. The fix was one line: inject `IHttpClientFactory` instead.

---

## 6. Now the .NET Part — Who Owns the Port?

Every ASP.NET Core app contains a built-in web server called **Kestrel**. Kestrel is the thing that binds the port and listens.

```
Old world (.NET Framework, ASP.NET on IIS)
------------------------------------------
   Browser --> IIS (owns port 80/443) --> loads your DLL inside IIS
   IIS *is* the web server. Your app is a plugin inside it.

New world (.NET Core / .NET 5+)
------------------------------------------
   Browser --> Kestrel (inside your own .exe, owns the port) --> your code
   Your app *is* the web server. It can run with no IIS at all.
```

That single change is why a modern .NET app is just `dotnet MyApp.dll` and it starts serving — and why it can run identically on Windows, Linux, and in Docker.

There are two ways IIS can still be involved on Windows:

| Model | What happens | When used |
|---|---|---|
| **Out-of-process** | IIS owns 80/443, forwards to Kestrel on a random local port | Older setups, or when you need a different process boundary |
| **In-process** (default) | The request goes straight into your app inside the IIS worker process — no extra hop | Default for IIS-published apps since .NET Core 3.0; faster |

Either way, **the public port belongs to whatever faces the internet**, and your app's own port is an internal detail.

> **Real life —** The old ASP.NET app: to serve a single request on a new server you had to install IIS, add the ASP.NET role, create a site in IIS Manager, point it at the folder, and pick a binding. The .NET 8 rewrite of the same app: copy the publish folder to a bare Linux VM, type `dotnet MyApp.dll`, and it answers on `http://localhost:5000` — IIS not installed, no web server configured, nothing but the runtime. That difference *is* "who owns the port".

---

## 7. Where Ports Are Configured in a .NET App (All 6 Places)

Listed from lowest to highest priority — **later ones win**.

**(1) Code — explicit and absolute**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);                     // HTTP  on 8080
    options.ListenAnyIP(8443, o => o.UseHttps());  // HTTPS on 8443
});

var app = builder.Build();
app.MapGet("/", () => "Hello");
app.Run();
```

Handy variants: `options.ListenLocalhost(5000)` (only this machine can reach it) vs `options.ListenAnyIP(5000)` (reachable from the network — required inside Docker).

**(2) `appsettings.json`**

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http":  { "Url": "http://*:8080" },
      "Https": { "Url": "https://*:8443" }
    }
  }
}
```

**(3) `launchSettings.json` — development only, never deployed**

```json
{
  "profiles": {
    "MyApp": {
      "commandName": "Project",
      "applicationUrl": "https://localhost:7043;http://localhost:5043"
    }
  }
}
```

This file is why every new project opens on a different random port, and why **it has zero effect in production** — it is used only by `dotnet run` / Visual Studio / VS Code.

**(4) Environment variable — the production favourite**

```bash
ASPNETCORE_URLS="http://+:8080"
# multiple:
ASPNETCORE_URLS="http://+:8080;https://+:8443"
```

**(5) Command line**

```bash
dotnet MyApp.dll --urls "http://0.0.0.0:8080"
```

**(6) Nothing at all** → Kestrel falls back to `http://localhost:5000` and `https://localhost:5001`.

> Note on precedence: `--urls` / `ASPNETCORE_URLS` and an explicit `ConfigureKestrel(...).Listen*()` are two different mechanisms — if you call `Listen*()` in code, it takes over and the URLs value is ignored (Kestrel logs a warning saying so). The ordering above is the practical one for the *configuration-driven* options.

**What `*`, `+`, `0.0.0.0` and `localhost` mean** (this trips up everyone once):

| Binding | Meaning |
|---|---|
| `localhost` / `127.0.0.1` | Only this machine. Nothing external can connect. |
| `0.0.0.0` / `*` / `+` | All network interfaces — reachable from outside |

If your app works locally but "refuses connection" inside Docker or from a colleague's machine, **99% of the time it is bound to `localhost` instead of `0.0.0.0`.**

> **Real life —** One build artifact, three environments, zero code changes:
> - On your laptop it opens on `https://localhost:7043` — that came from `launchSettings.json`.
> - The QA container runs the identical DLL with `ASPNETCORE_URLS=http://+:8080` in the compose file.
> - Production runs the identical DLL again, with the same variable injected by the Kubernetes deployment YAML.
>
> Nobody rebuilt anything between QA and prod. That is the whole point of keeping the port in configuration instead of in code — and the reason a "port change" in production is a config edit and a restart, not a release.

---

## 8. Development vs Production — Two Different Worlds

| | Development (your laptop) | Production |
|---|---|---|
| Port | 5000/5001, 7043... whatever is free | 80 / 443 to the outside world |
| Who owns 80/443 | nobody | IIS / nginx / load balancer / ingress |
| Certificate | `dotnet dev-certs https` — self-signed, trusted only by you | Real CA-issued cert (Let's Encrypt, ACM, corporate CA) |
| App itself listens on | 5000/5001 | typically 8080, internal only |
| Port visible in URL | yes, always | never |

Why dev doesn't use 80/443:

1. Something on your machine (IIS, Skype historically) may already own port 80.
2. On Linux/macOS you would need `sudo` for every debug run.
3. Multiple projects run side by side — they *must* differ.

First-time HTTPS locally:

```bash
dotnet dev-certs https --trust
```

This generates a self-signed certificate and adds it to your trust store. Without it you get the browser's "Your connection is not private" wall on `https://localhost:5001`.

> **Real life —** A normal working day: the API solution is running on `7043` and the Angular BFF on `7156`, both open in Visual Studio at once, both debugging happily. If either had been hard-coded to port 80, the second F5 would have died on startup. And the first time you ever opened `https://localhost:7043`, Chrome showed "Your connection is not private" until you ran `dotnet dev-certs https --trust` once — the local, miniature version of production's "who signed this certificate?" problem.

---

## 9. The Reverse Proxy Pattern (This Is How Real Apps Are Hosted)

This diagram is the single most useful thing in this file:

```
                             THE INTERNET
                                  |
                      DNS: myapp.com -> 203.0.113.10
                                  |
                                  v
        +-------------------------------------------------+
        |  Reverse proxy / Load balancer                  |
        |  listens on PORT 80  -> redirect to 443         |
        |  listens on PORT 443 -> holds the TLS cert,     |
        |                         decrypts the traffic    |
        +-------------------------------------------------+
             |                        |                      |
             |  plain HTTP, inside the private network       |
             v                        v                      v
      +--------------+        +--------------+        +--------------+
      | .NET app #1  |        | .NET app #2  |        | .NET app #3  |
      | Kestrel :8080|        | Kestrel :8080|        | Kestrel :8080|
      +--------------+        +--------------+        +--------------+
```

Notice:

- **Only the proxy needs 80/443.** Three identical app copies all happily use 8080 because they are on different machines/containers.
- **TLS terminates at the proxy.** The certificate lives there, not in your app. Your app usually speaks plain HTTP internally — simpler, and one place to renew certificates.
- **Scaling out doesn't change ports.** You add a 4th copy on 8080; the proxy just starts sending traffic to it.

The proxy is nginx, IIS, Apache, YARP, AWS ALB, Azure Application Gateway, or a Kubernetes ingress — same role in every case.

**The one thing you must configure in .NET behind a proxy:** without help, your app thinks every request is plain HTTP coming from the proxy's IP. Fix it:

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
```

Skip this and you get wrong client IPs in your logs, `http://` links generated inside `https://` pages, and redirect loops.

> **Real life —** `api.company.com` sits on an AWS Application Load Balancer. Behind it, three EC2 instances each run Kestrel on **8080** — the same number on all three, because they are three different machines. The TLS certificate for `api.company.com` lives in ACM and is attached to the ALB only. When it auto-renews, **nothing on the three servers changes** — no restart, no file copy, no downtime. Autoscaling adds a fourth instance: also 8080, and the ALB just starts sending it traffic.
>
> Compare that with the old model, where the certificate had to be installed and renewed on every single IIS box. That is why "TLS terminates at the proxy" is worth remembering.

---

## 10. Docker & Containers — Two Ports, Not One

Inside a container, the app is in its own tiny network. Two separate numbers exist and beginners mix them up constantly.

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .
ENV ASPNETCORE_URLS=http://+:8080      # <-- port INSIDE the container
EXPOSE 8080                            # <-- documentation only, opens nothing
ENTRYPOINT ["dotnet", "MyApp.dll"]
```

```bash
docker run -p 443:8080 myapp
#              ^^^  ^^^^
#              |    +-- container port: where Kestrel listens
#              +------- host port: what the outside world connects to
```

Read `-p` as **outside : inside**.

```
   Browser -> host machine :443  ==mapped==>  container :8080  -> Kestrel
```

Two rules that save hours:

1. **`EXPOSE` publishes nothing.** It is a label for humans and tooling. Only `-p` (or a compose/k8s port mapping) actually opens a path.
2. **Bind to `+` / `0.0.0.0`, never `localhost`.** Inside a container, `localhost` means *the container itself*, so the mapped traffic never reaches your app — you get an instant "connection reset". This is the single most common containerised-.NET bug.

Since .NET 8, the official container images already default to port **8080** (they used to default to 80) precisely because containers now run as a non-root user, which cannot bind port 80.

> **Real life —** A tester says "v2 broke the search response". You want both versions running side by side to compare:
>
> ```bash
> docker run -d -p 5001:8080 myapi:v1
> docker run -d -p 5002:8080 myapi:v2
> ```
>
> Both containers listen on **8080 internally** and neither complains — because each container has its own private network. You hit `localhost:5001` and `localhost:5002` and diff the two responses. The only thing that would clash is reusing host port `5001` twice.

---

## 11. Common Errors and What They Really Mean

| Error | Plain-English cause | Fix |
|---|---|---|
| `Failed to bind to address http://127.0.0.1:5000: address already in use` | Another process already lives in that flat — often a previous run of your own app | Kill it, or change the port |
| `ERR_CONNECTION_REFUSED` | Nobody is listening on that port. The machine answered "no such flat" | Is the app running? Right port? Bound to `0.0.0.0`? |
| Request just hangs, then times out | Something is listening, but a firewall/security group silently dropped the packet | Open the port in the firewall / NSG / security group |
| `ERR_SSL_PROTOCOL_ERROR` on `https://...:5000` | You spoke HTTPS to a port serving plain HTTP — wrong language at the right door | Use the HTTP port, or the HTTPS one |
| `Permission denied` binding port 80 on Linux | Ports below 1024 need root | Use 8080 and put a proxy in front |
| `502 Bad Gateway` | The proxy is fine; the app behind it is dead or on a different port | Check the app and the proxy's upstream port |
| Endless `http -> https` redirect loop | Proxy terminated TLS, app sees HTTP, redirects again, forever | `UseForwardedHeaders` (section 9) |

**Finding who owns a port**

```powershell
# Windows
netstat -ano | findstr :5000
Get-Process -Id <PID>
```

```bash
# Linux / macOS
lsof -i :5000
ss -tulpn | grep 5000
```

> **Real life —** The most common version of this, once a week, on every .NET team: you finish debugging and close the browser tab instead of pressing **Stop**. Visual Studio leaves `MyApi.exe` alive in the background. Next F5:
>
> ```
> Failed to bind to address https://127.0.0.1:5001: address already in use.
> ```
>
> You did not break anything — the previous *you* is still living in that flat. `netstat -ano | findstr :5001` gives the PID, Task Manager ends it, F5 works. Ninety percent of "port already in use" tickets are exactly this.

---

## 12. HTTP to HTTPS Redirection & HSTS

Standard production setup: **port 80 exists only to push people to port 443.**

```csharp
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();               // tell browsers: never use http for this site again
}

app.UseHttpsRedirection();       // 307/308 redirect from http:// to https://
```

- `UseHttpsRedirection` catches the request that arrived on port 80 and answers "go to `https://` instead". It needs to know the HTTPS port — it infers it from the configured HTTPS endpoint, or you set `ASPNETCORE_HTTPS_PORT` / `HttpsRedirectionOptions.HttpsPort`.
- `UseHsts` sends a `Strict-Transport-Security` header. After the first visit, the **browser itself** refuses to make a plain-HTTP request to your domain — the redirect stops being necessary, closing the window where a first request could be intercepted.

Two cautions:

- **Never enable HSTS in development.** The browser caches it per host, and `localhost` is shared by all your projects — you will force *every* local project to HTTPS and then wonder why. (The ASP.NET Core template already guards it with `IsDevelopment()` for this reason.)
- Behind a TLS-terminating proxy, redirection is usually done **at the proxy**, and `UseForwardedHeaders` is mandatory if you also keep it in the app.

> **Real life —** Someone once removed the `IsDevelopment()` guard around `UseHsts()` "to make dev match prod". Three months later, on a *completely different* project, a plain-HTTP API on `http://localhost:5000` refused to load in Chrome — the browser kept rewriting it to `https://localhost:5000` and failing, while the same URL worked fine in Postman and in Firefox. The reason: HSTS is remembered **per host**, and `localhost` is one shared host across every project on that machine. The app was innocent; the browser had been told months earlier "never speak HTTP to localhost again". Fix: `chrome://net-internals/#hsts` → delete `localhost` — and put the `IsDevelopment()` guard back.

---

## 13. Ports vs Firewalls / Security Groups

Your app binding a port is only half the journey. Every hop in between can still say no.

```
Browser
   |    (1) DNS: does myapp.com resolve to an IP?
   |    (2) Cloud firewall / Security Group: is port 443 allowed inbound?
   |    (3) OS firewall on the machine: is port 443 allowed?
   |    (4) Is a process actually LISTENING on 443?
   v    (5) Is that process your app, bound to 0.0.0.0 and not localhost?
Your .NET app
```

The symptom tells you the layer:

- **Refused, instantly** → nothing is listening (layer 4 or 5). Your app is the problem.
- **Hangs, then times out** → a firewall dropped it silently (layer 2 or 3). Your app is fine; the network is not.

That distinction alone will save you hours of debugging the wrong thing.

> **Real life —** First deployment to a new EC2 instance. You SSH in and run `curl http://localhost:8080/health` → `200 OK`, app logs look perfect. From your laptop's browser, the same URL spins for 30 seconds and then times out. Nothing in the app logs — because the request never arrived.
>
> The **timeout** was the clue: a refusal would have come back instantly. Something upstream was swallowing packets silently. The security group had inbound rules for SSH (22) only — nobody had added 443. Two minutes in the console, and it worked. Half an hour of reading app logs would have found nothing, because the app was never the problem.

---

## 14. Interview One-Liners

- **"What is a port?"** — A 16-bit number the OS uses to route an incoming connection to the right process. IP identifies the machine; port identifies the application on it.
- **"Difference between 80 and 443?"** — Both carry HTTP. 443 wraps it in TLS, so it is encrypted and requires a certificate. They are conventions (well-known ports), so browsers assume 80 for `http://` and 443 for `https://` and hide them from the URL.
- **"Why does your app run on 8080 in production, not 80?"** — Ports under 1024 are privileged on Linux, and modern containers run as non-root. A reverse proxy or load balancer owns 80/443, terminates TLS, and forwards to Kestrel on 8080.
- **"Can two apps share a port?"** — Not on the same IP and protocol. But one app on port 443 can serve thousands of clients, because each connection is identified by the full pair of (client IP + client port + server IP + server port).
- **"What changed from ASP.NET to ASP.NET Core in hosting?"** — ASP.NET Framework apps were plugins inside IIS, which owned the port. ASP.NET Core apps embed Kestrel and own the port themselves, so they self-host and run cross-platform; IIS/nginx become optional reverse proxies.
- **"Where can the port come from?"** — `--urls` on the command line, `ASPNETCORE_URLS`, the `Kestrel:Endpoints` section of `appsettings.json`, or `ConfigureKestrel` in code (which overrides the URL-based ones); 5000/5001 if nothing is set. `launchSettings.json` applies to local dev tooling only.

> **Real life —** The question actually asked in interviews is rarely "what is a port?" — it is **"how is your application hosted?"** A 20-second answer that covers this whole file:
>
> *"It's a containerised .NET 8 service. Kestrel listens on 8080 inside the container, as a non-root user, so we never need a privileged port. An ALB owns 443 with an ACM certificate and terminates TLS; port 80 exists only to redirect to 443. Because TLS terminates at the load balancer, we enable `UseForwardedHeaders` so client IPs and the original scheme are correct in our logs. Port comes from `ASPNETCORE_URLS`, so the same image runs in every environment."*
>
> Notice it never mentions `launchSettings.json` — that is how the interviewer knows you have actually deployed something.

---

## 15. Mental Model Recap

Read these six lines whenever ports get confusing again:

1. **IP = building, port = flat number, app = resident.** One resident per flat.
2. **80 and 443 are only conventions** — universally agreed defaults so nobody has to type a number. The real difference between them is TLS, not the digits.
3. **A port other than the scheme's default becomes part of your URL forever.** That is why production hides behind 80/443.
4. **In .NET Core, your app owns the port** (Kestrel is inside your exe) — unlike the old IIS model.
5. **Bind `0.0.0.0`, not `localhost`,** any time something outside the machine or container must reach you.
6. **Connection refused = nothing listening. Timeout = a firewall ate it.** Different problem, different fix.

> **Real life —** One ordinary Tuesday morning, and how each line pays for itself:
>
> | What happened | Which line explains it |
> |---|---|
> | F5 fails: "5001 already in use" | 1 — yesterday's process still lives in that flat |
> | Colleague can't reach your dev API over the LAN | 5 — you bound `localhost`, not `0.0.0.0` |
> | QA complains the URL shows `:5001` in the address bar | 3 — not behind the proxy on 443 yet |
> | Prod URL just hangs from outside the office | 6 — timeout, so a firewall rule, not your code |
>
> Four unrelated-looking problems in one morning; all four are on this list.
