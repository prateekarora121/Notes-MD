# Ports & Hosting — Quick-Revision Notes

> *U. Ports-and-Hosting-Explained-Simply* se banayi gayi quick-revision notes. Saare sections usi order mein, Q&A + bullet form mein. Sirf yeh notes hi topic brush-up ke liye kaafi honi chahiye — guide kholne ki zarurat na pade.

---

## 1. The Core Analogy

- **IP = building ka address. Port = flat number. App = us flat ka resident.**
- Courier ko dono chahiye — "221B Baker Street" bekaar hai agar wahan 40 families rehti hain.
- **Ek flat, ek resident** → do process ek hi port bind nahi kar sakte (same IP + protocol).

```
https://myapp.com:443/orders
  scheme    host   port  path
  (rules)  (machine) (app) (app ke andar endpoint)
```

**Ek line:** IP machine dhoondhta hai, port us machine par app dhoondhta hai.

> **Real life:** Company landline `+91-120-4567890` → reception; extension `221` → ek banda. Number = IP, extension = port, reception = OS.

---

## 2. What a Port Is

**Q: Technically port kya hai?**
A: Ek **16-bit number (0–65535)**. Kuch physical nahi. Yeh ek label hai jise OS incoming packet ko sahi process tak pahunchane ke liye use karta hai.

- **Binding** = aapki app OS ko bolti hai "port 5000 par jo aaye, sab mujhe do."
- Pehle aao pehle pao — ek waqt par ek hi owner per (IP, protocol, port).
- **TCP 443 aur UDP 443 alag darwaaze hain.** Web = TCP, sirf HTTP/3 UDP 443 use karta hai.

> **Real life:** Abhi aapka laptop — API 5001 par, SQL Server 1433 par, Redis 6379 par, sab `127.0.0.1` par. Pehla run band kiye bina API ka doosra F5 → turant `address already in use`.

---

## 3. Why 80 and 443

| Port | Protocol | Encryption | Certificate |
|---|---|---|---|
| 80 | HTTP | koi nahi — plain text | zarurat nahi |
| 443 | HTTPS | TLS | zaroori |

- Yeh **well-known ports** hain — bas IANA ki ek convention, iske alawa kuch nahi. Browsers `http://` ke liye 80 aur `https://` ke liye 443 maan lete hain.
- **Digits mein kuch special nahi hai.** 80 aur 443 ka asli farak **TLS** hai.
- Port 80 par bheja password raste mein baithe kisi ko bhi readable hai. 443 par sirf ciphertext.
- TLS certificate prove karta hai "main sach mein myapp.com hoon", aur use ek aisi CA ne sign kiya hai jispar browser pehle se bharosa karta hai.
- **Port 80 ka bacha hua ek hi kaam:** `http://` walon ko `https://` par redirect karna, aur ACME/Let's Encrypt renewal challenges serve karna.

> **Real life:** Airport Wi-Fi. `http://` par login karo — Wireshark chalane wale ko `password=admin123` saaf padhne ko milta hai. Usi Wi-Fi par `https://` par login karo — use sirf ciphertext milta hai. Same network, same password — farak sirf port 80 vs 443. Padlock = "443 + valid cert".

---

## 4. Why Real Sites Show No Port

- Browser hamesha port bhejta hai; woh bas scheme ka **default port chhupa deta hai**.
- `google.com` → `https://google.com:443`. Khud `https://google.com:443` kholo — address bar se `:443` hat jaayega.
- **Rule:** default port → invisible. Non-default port → hamesha ke liye aapke public URL ka hissa.
- Production 80/443 ke peeche hone ki #1 wajah yehi hai — koi customer ko `https://shop.com:5001` nahi deta.

> **Real life:** Teams mein `https://timesheet.mycompany.com` paste karo — sabka khul jaata hai. `http://localhost:5000/swagger` paste karo — kisi ka nahi, aur IP dene ke baad bhi `:5000` type karna padta hai. Dono URLs mein port hai; sirf default wala invisible hai.

---

## 5. Port Cheat Sheet

**Ranges**

| Range | Naam | Note |
|---|---|---|
| 0–1023 | Well-known / privileged | **Linux: bind karne ko root chahiye** |
| 1024–49151 | Registered | app/dev servers (5000, 8080) |
| 49152–65535 | Dynamic / ephemeral | OS *outgoing* calls ke liye chunta hai — hard-code na karo |

**Yaad rakhne wale numbers:** 80 HTTP · 443 HTTPS · 5000/5001 Kestrel dev · 8080/8443 unprivileged jodi · 1433 SQL Server · 5432 Postgres · 3306 MySQL · 6379 Redis · 27017 Mongo · 5672/15672 RabbitMQ · 22 SSH · 53 DNS · 25/587 SMTP.

**Q: Ek server ek hi port 443 par 10,000 users kaise serve karta hai?**
A: Connection **4-tuple** se pehchana jaata hai (client IP + client port + server IP + server port). Har client ko apna random *ephemeral* port milta hai, isliye server port shared hone ke baawajood har connection unique hai.

```
Aapki app 10.0.5.23:52741  ---->  SQL Server 10.0.9.10:1433
          ephemeral (random)                well-known (fixed)
```

- **Ephemeral port exhaustion** = classic incident, jab har request par `new HttpClient()` banta hai. Fix: `HttpClient` reuse / `IHttpClientFactory`.

> **Real life:** 10 tabs khole hue `netstat -ano | findstr ESTABLISHED` → remote port lagbhag hamesha 443, local port har row mein alag (52741, 52742…). Incident version: nightly export jismein har record par `new HttpClient()` — 200 test rows par theek, prod mein ~16,000 ke baad `SocketException`. Fix: `IHttpClientFactory`.

---

## 6. Who Owns the Port in .NET

| | .NET Framework (ASP.NET on IIS) | .NET Core / .NET 5+ |
|---|---|---|
| Web server | IIS | **Kestrel, aapke hi process ke andar** |
| Aapki app | IIS ka load kiya plugin | khud web server |
| Port ka owner | IIS | aapki app |
| IIS ke bina chalti hai | nahi | haan — `dotnet MyApp.dll` |
| Cross-platform | nahi | haan (Windows/Linux/Docker) |

**IIS hosting models (Windows):**
- **In-process** — .NET Core 3.0 se default; request IIS worker process ke andar hi aapki app mein jaati hai, koi extra hop nahi, faster.
- **Out-of-process** — IIS 80/443 ka owner, aur random local port par Kestrel ko reverse-proxy karta hai.

**Dono soorat mein:** public port us cheez ka hai jo internet ki taraf hai; aapki app ka port internal detail hai.

> **Real life:** Purani app = IIS install karo, role add karo, IIS Manager mein site banao, binding set karo, tab serve hoti hai. .NET 8 rewrite = publish folder khaali Linux VM par copy karo, `dotnet MyApp.dll`, 5000 par jawab. IIS install hi nahi.

---

## 7. Where the Port Comes From (6 Places)

| # | Mechanism | Example |
|---|---|---|
| 1 | Code — `ConfigureKestrel` | `options.ListenAnyIP(8080)` |
| 2 | `appsettings.json` | `"Kestrel": { "Endpoints": { "Http": { "Url": "http://*:8080" } } }` |
| 3 | `launchSettings.json` | `"applicationUrl": "https://localhost:7043"` — **sirf dev tooling** |
| 4 | Env var | `ASPNETCORE_URLS="http://+:8080;https://+:8443"` |
| 5 | Command line | `dotnet MyApp.dll --urls "http://0.0.0.0:8080"` |
| 6 | Kuch set nahi | `http://localhost:5000` + `https://localhost:5001` par fallback |

- **Configuration-driven** options mein: command line > env var > `appsettings.json`.
- **Gotcha:** `--urls`/`ASPNETCORE_URLS` aur explicit `ConfigureKestrel().Listen*()` do alag mechanisms hain — code mein `Listen*()` call karne par woh control le leta hai aur URLs value **ignore** ho jaati hai (Kestrel warning log karta hai).
- `launchSettings.json` kabhi deploy nahi hoti — production mein zero effect. Isi wajah se har naya project alag random port par khulta hai.

**Binding addresses — yeh yaad kar lo:**

| Binding | Kahan se reachable |
|---|---|
| `localhost` / `127.0.0.1` / `ListenLocalhost` | sirf yeh machine |
| `0.0.0.0` / `*` / `+` / `ListenAnyIP` | saare interfaces — bahar ki duniya |

> Locally chalti hai, par Docker mein ya colleague ki machine se "connection refused"? **99% woh `0.0.0.0` ki jagah `localhost` par bind hai.**

> **Real life:** Ek artifact, teen environments — laptop `7043` (launchSettings), QA container `ASPNETCORE_URLS=http://+:8080` (compose), prod mein wahi var k8s YAML se. QA aur prod ke beech kuch rebuild nahi hua; prod mein port change = config edit + restart, release nahi.

---

## 8. Dev vs Prod

| | Development | Production |
|---|---|---|
| Port | 5000/5001, 7043… jo free ho | bahar 80/443, app khud aam taur par 8080 |
| 80/443 ka owner | koi nahi | IIS / nginx / LB / ingress |
| Certificate | `dotnet dev-certs https --trust` (self-signed) | asli CA cert (Let's Encrypt, ACM, corporate CA) |
| URL mein port | hamesha dikhta hai | kabhi nahi |

**Dev 80/443 kyun avoid karta hai:** port 80 pehle se kisi ke paas ho sakta hai; Linux/macOS par har debug run ke liye `sudo` lagta; saath chalte projects ke ports alag hone hi chahiye.

> **Real life:** API `7043` par aur Angular BFF `7156` par, dono VS mein saath debug — kisi ek mein port 80 hard-code hota toh doosra F5 mar jaata. Aur `dotnet dev-certs https --trust` ek baar chalane se hi Chrome ka localhost par "not private" warning band hua.

---

## 9. Reverse Proxy Pattern

```
Internet -> proxy/LB  :80  -> 443 par redirect
                      :443 -> TLS cert yahin hai, decrypt yahin
              |          |          |     (plain HTTP, private network)
              v          v          v
        Kestrel:8080  Kestrel:8080  Kestrel:8080
```

- **Sirf proxy ko 80/443 chahiye.** N app copies sab 8080 reuse karti hain kyunki woh alag hosts/containers par hain.
- **TLS proxy par terminate hota hai** — certificate rakhne aur renew karne ki ek hi jagah; app andar plain HTTP bolti hai.
- Scale out karne se ports nahi badalte.
- Proxy = nginx, IIS, Apache, YARP, AWS ALB, Azure App Gateway, k8s ingress — role wahi.

**Q: Proxy ke peeche .NET mein kya configure karna zaroori hai?**

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
```

Iske bina: logs mein galat client IPs, `https://` pages par bane `http://` links, redirect loops.

> **Real life:** `api.company.com` ALB par, teen EC2 instances sab 8080 par, ACM cert sirf ALB se attached. Cert auto-renew hota hai → teen servers par kuch nahi badalta. Autoscaling chautha add karta hai → woh bhi 8080. Purane model mein cert har IIS box par install aur renew karna padta tha.

---

## 10. Docker — Two Ports

```dockerfile
ENV ASPNETCORE_URLS=http://+:8080   # container ke ANDAR ka port
EXPOSE 8080                          # sirf documentation — kuch open nahi karta
```

```bash
docker run -p 443:8080 myapp     # padho:  bahar : andar
```

- **`EXPOSE` kuch publish nahi karta** — raasta sirf `-p` / compose / k8s mapping kholta hai.
- **Container ke andar `localhost` par kabhi bind na karo** — uska matlab khud container hai, toh mapped traffic app tak pahunchta hi nahi → turant connection reset. Containerised .NET ka sabse common bug.
- **.NET 8+ images default 8080** rakhti hain (pehle 80 tha) kyunki containers ab non-root chalte hain, jo 80 bind nahi kar sakta.

> **Real life:** Do builds compare karne ke liye — `docker run -d -p 5001:8080 myapi:v1` aur `docker run -d -p 5002:8080 myapi:v2`. Dono andar 8080, koi clash nahi (alag container networks); clash tab hoti jab host port 5001 dobara use karte.

---

## 11. Error → Cause Table

| Error | Wajah | Fix |
|---|---|---|
| `Failed to bind… address already in use` | port pehle se kisi process ka hai (aksar aapka hi pichhla run) | kill karo ya port badlo |
| `ERR_CONNECTION_REFUSED` | koi listen nahi kar raha — machine ne saaf bola "aisa flat nahi" | app chal rahi hai? port sahi? `0.0.0.0` par bind? |
| latakta hai phir timeout | koi listen kar raha hai, par firewall ne chupchaap drop kiya | firewall / NSG / SG mein port kholo |
| `ERR_SSL_PROTOCOL_ERROR` | plain-HTTP port se HTTPS bola — sahi darwaaza, galat bhaasha | matching port use karo |
| Linux par port 80 par `Permission denied` | 1024 se neeche ke ports privileged hain | 8080 + proxy |
| `502 Bad Gateway` | proxy theek, peeche wali app mari hui ya doosre port par | app aur proxy ka upstream port check karo |
| na rukne wala `http → https` loop | proxy ne TLS terminate kiya, app ko HTTP dikha aur woh hamesha redirect karti rehti hai | `UseForwardedHeaders` |

**Port ka owner kaun:**
```powershell
netstat -ano | findstr :5000 ; Get-Process -Id <PID>   # Windows
```
```bash
lsof -i :5000        # ya: ss -tulpn | grep 5000        # Linux/macOS
```

> **Real life:** Stop dabane ki jagah browser tab band kiya → `MyApi.exe` zinda → agla F5: `Failed to bind… 5001 already in use`. `netstat -ano | findstr :5001` → PID → Task Manager se end. "Port in use" ke ~90% tickets yehi hain.

---

## 12. HTTPS Redirection & HSTS

```csharp
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseHttpsRedirection();
```

- **`UseHttpsRedirection`** — port 80 wali request pakadkar 307/308 → `https://` bhejta hai. Use HTTPS port pata hona chahiye (configured HTTPS endpoint se andaza, ya `ASPNETCORE_HTTPS_PORT` / `HttpsRedirectionOptions.HttpsPort` set karo).
- **`UseHsts`** — `Strict-Transport-Security` header bhejta hai; pehli visit ke baad **browser khud** us domain par plain HTTP se mana kar deta hai, jisse pehli request intercept hone ka window band ho jaata hai.
- **Dev mein HSTS kabhi enable na karo** — woh per host cache hota hai aur `localhost` aapke saare projects mein shared hai. (Template isi wajah se `IsDevelopment()` guard rakhta hai.)
- TLS-terminating proxy ke peeche redirection aam taur par proxy par hoti hai; app mein bhi rakho toh `UseForwardedHeaders` compulsory hai.

> **Real life:** Kisi ne `UseHsts()` ke aage se `IsDevelopment()` guard hata diya. Mahine baad ek *alag* plain-HTTP project `localhost:5000` par Chrome mein khulna band ho gaya (Postman/Firefox mein theek) — HSTS per host cache hota hai aur `localhost` sab projects mein shared hai. Fix: `chrome://net-internals/#hsts` → `localhost` delete, guard wapas.

---

## 13. Ports vs Firewalls

```
(1) DNS resolve hota hai?  (2) cloud SG/firewall 443 allow karta hai?  (3) OS firewall 443 allow karta hai?
(4) 443 par koi LISTEN kar raha hai?  (5) woh aapki app hai, 0.0.0.0 par bind?
```

**Kaam ka diagnostic:**
- **Turant refused** → koi listen nahi kar raha (layer 4/5) → **aapki app ki problem**.
- **Latakta hai phir timeout** → firewall ne chupchaap drop kiya (layer 2/3) → **network ki problem**.

> **Real life:** Naya EC2 box — instance par `curl http://localhost:8080/health` = 200, logs saaf; laptop ke browser se 30s latakta hai phir timeout, logs mein kuch nahi. *Timeout* (refusal nahi) ne bata diya "network, app nahi": security group mein sirf SSH 22 allowed tha, 443 kabhi nahi.

---

## 14. Interview One-Liners

- **Port?** 16-bit number jise OS connection ko sahi process tak pahunchane ke liye use karta hai. IP = machine, port = us par app.
- **80 vs 443?** Dono HTTP; 443 use TLS mein wrap karta hai, certificate chahiye. Dono conventions hain, isliye browsers maan lete hain aur chhupa dete hain.
- **Prod mein 8080 kyun, 80 kyun nahi?** Linux par <1024 privileged hai aur containers non-root chalte hain; proxy/LB 80/443 own karta hai, TLS terminate karta hai, Kestrel ko 8080 par forward karta hai.
- **Do apps, ek port?** Same IP+protocol par nahi — par 443 par ek app hazaaron clients serve karti hai, kyunki connections poore 4-tuple se pehchane jaate hain.
- **ASP.NET → ASP.NET Core hosting mein kya badla?** IIS-plugin model → Kestrel aapki app ke andar, aur port ka owner aapki app; self-hosted, cross-platform, IIS/nginx optional proxy ban gaye.
- **In-process vs out-of-process IIS?** In-process (3.0 se default) IIS worker ke andar chalta hai — faster, no extra hop. Out-of-process local port par Kestrel ko reverse-proxy karta hai.
- **Port kahan se aa sakta hai?** `--urls`, `ASPNETCORE_URLS`, `appsettings.json` ka `Kestrel:Endpoints`, ya code mein `ConfigureKestrel` (jo URL-based waalon ko override karta hai); warna 5000/5001. `launchSettings.json` = sirf dev tooling.

> **Real life —** "aapki app host kaise hoti hai?" ka 20-second jawaab, kyunki asli sawaal yehi poocha jaata hai:
>
> *"Containerised .NET 8. Container ke andar Kestrel 8080 par, non-root, isliye privileged port ki zarurat nahi. ALB 443 ka owner hai, ACM cert ke saath TLS wahin terminate hota hai; port 80 sirf redirect karta hai. TLS LB par terminate hone ki wajah se hum `UseForwardedHeaders` enable karte hain taaki logs mein client IP aur scheme sahi rahein. Port `ASPNETCORE_URLS` se aata hai, isliye wahi image har environment mein chalti hai."*
>
> Ismein `launchSettings.json` ka zikr nahi hai — yehi tell hai ki aapne sach mein deploy kiya hai.

---

## 15. Six-Line Recap

1. IP = building, port = flat, app = resident. Ek flat, ek resident.
2. 80/443 conventions hain; asli farak TLS hai, digits nahi.
3. Non-default port hamesha ke liye URL ka hissa ban jaata hai → isliye prod 80/443 ke peeche chhupta hai.
4. .NET Core mein **port aapki app ka hai** (Kestrel aapki exe ke andar).
5. `0.0.0.0` par bind karo, `localhost` par nahi — jab bhi machine/container ke bahar se kisi ko pahunchna ho.
6. **Refused = koi listen nahi kar raha. Timeout = firewall kha gaya.**

> **Real life:** ek Tuesday morning — F5 fail "5001 in use" (line 1); colleague LAN se aapki dev API tak nahi pahunch pa raha (line 5); QA ko address bar mein `:5001` dikh raha hai (line 3); office ke bahar se prod URL latakta hai (line 6). Chaar alag-alag dikhne wale problems, chaaron isi list par.
