> **AWS Detailed Guide** · [Index](README.md) · Part III

# Global Edge Services

> **Geography vocabulary pehle,** kyunki answers isi par depend karte hain: ek **Region** ek geographic area hai (e.g. `us-east-1`); ek **Availability Zone** ek region ke andar ek ya zyada discrete data centres hai, failure ke liye isolated lekin low-latency fibre se linked; ek **Edge Location / Point of Presence** worldwide 600+ CloudFront caches mein se ek hai — regions se kaafi zyada numerous, aur caching aur AWS backbone mein entry ke liye use hota hai, aapke workloads run karne ke liye nahi.

---

## 1. CloudFront

### CloudFront (CDN)

**Yeh kya hai:** ek global **content delivery network** — yeh aapke content ko users ke kareeb edge locations par cache karta hai, isliye requests locally serve hoti hain, origin tak travel karne ke bajaye.

**Speed se aage aapko actually kya milta hai:**
- Aapke origin se door users ke liye lower latency aur higher throughput.
- **Origin offload** — cached hits kabhi aapke ALB/S3/EC2 tak nahi pahunchte, load aur egress cost dono kam karte hain (CloudFront→origin traffic AWS origins se free hota hai).
- **AWS Shield Standard built in**, plus edge par **WAF** integration — isliye attacks aapki infrastructure tak pahunchne se pehle absorb ho jaate hain.
- Ek free ACM certificate ke saath **edge par TLS termination**, HTTP/2 aur HTTP/3, aur automatic compression.

**Object model:** ek **Distribution** ke ek ya zyada **Origins** hote hain (S3 bucket, ALB, API Gateway, EC2, MediaStore, ya koi bhi HTTP server, non-AWS bhi shamil), aur **Cache Behaviours** jo path patterns (`/api/*`, `/static/*`) ko ek origin plus apni caching, header/cookie/query-string forwarding, aur TLS settings ke saath map karte hain. Isi tareeke se ek domain S3 se cached static assets serve karta hai aur uncached `/api/*` ek ALB se.

**Caching controls:** **cache key** (request ke kaunse parts response ko unique banate hain — jitna kam ho *utna* forward karo, kyunki har header ya cookie forward karna hit rate destroy kar deta hai), **TTLs** (minimum/maximum/default, origin ke `Cache-Control` headers se override hote hain), aur paths ko early purge karne ke liye **invalidations**. Invalidations ek free monthly allowance se aage billed hote hain aur slow hote hain — **better practice hai versioned filenames ya query strings** (`app.a1b2c3.js`) taaki naye content ka naya cache key ho aur kuch purge karne ki zarurat na pade.

**Origin ko secure karna — `OAC`:** **Origin Access Control** (legacy **OAI** ka modern replacement) CloudFront ko ek **private** S3 bucket ko signed requests bhejne deta hai, isliye bucket internet ke liye fully closed rehta hai aur sirf aapke distribution se readable hota hai. Yeh static site hosting ke liye correct pattern hai (dekho [S3 Static Website Hosting](05-s3.md#s3-static-website-hosting)), aur OAC — OAI ke unlike — SSE-KMS aur saare HTTP methods bhi support karta hai.

**Content tak access restrict karna:**
| Mechanism | Use for |
|---|---|
| **Signed URL** | Ek URL ke liye ek specific file — ek single paid download, ek report |
| **Signed Cookie** | Har baar URL generate kiye bina **multiple** files/whole sections — ek subscriber ki video library |
| **Geo restriction** | Country ke basis par allowlist ya blocklist, licensing ya compliance ke liye |

**❗ CloudFront signed URL vs S3 presigned URL** ek frequently asked pairing hai: ek **S3 presigned URL** S3 API se generate hota hai, *generating principal* ki IAM permissions carry karta hai, S3 ko directly hit karta hai, aur CloudFront ke cache aur protections ko bypass kar deta hai. Ek **CloudFront signed URL** ek CloudFront key pair se banaya jaata hai, edge se serve hota hai (isliye aapko caching, WAF, Shield, aur logging milti hai), aur kisi bhi origin type ko cover kar sakta hai — aur yeh aapko bucket private rakhne deta hai. Content *through* CDN deliver hone par CloudFront signing use karo; direct, short-lived programmatic access (jaise browser uploads) ke liye S3 presigning use karo.

**Edge compute:**
| | **CloudFront Functions** | **Lambda@Edge** |
|---|---|---|
| Runtime | Lightweight JavaScript, sub-millisecond | Node.js / Python, up to 5–30 s |
| Runs at | Sirf edge locations | Regional edge caches |
| Triggers | Viewer request / viewer response | Sabhi chaar: viewer + **origin** request/response |
| Doosri services/network call kar sakta hai? | ❌ | ✅ |
| Use for | Header manipulation, URL rewrites, redirects, simple auth-token checks, A/B cookie assignment | Origin selection logic, ek database ya API ko calls, image transformation, heavier auth |

Naam lene worth doosre details: **Price Classes** (All / 200 / 100) sabse expensive regions exclude karke global coverage ko cost ke saath trade karte hain; **Origin Groups** automatic origin failover dete hain; **field-level encryption** specific form fields (jaise ek card number) ko ek public key se encrypt karta hai taaki sirf intended downstream service unhe padh sake; aur **standard vs real-time logs** analysis ke liye vs live monitoring ke liye.

**❗ Classic CloudFront gotcha:** CloudFront ke liye ek custom-domain certificate **`us-east-1` mein request/import karna zaruri hai**, aapka origin ya users kahin bhi ho — kyunki CloudFront ek global service hai jo N. Virginia se managed hoti hai. Ek ALB ke liye certificates, iske against, ALB ke apne region mein hone chahiye.

---

## 2. Global Accelerator

### AWS Global Accelerator

**Yeh kya hai:** do **static anycast IP addresses** jo aapki application ke aage rehte hain. Client traffic sabse nearest edge location par **AWS private global backbone** mein enter karta hai aur internally sabse closest healthy regional endpoint tak travel karta hai, public internet ke across traverse karne ke bajaye.

- **Layer 4 par TCP aur UDP** ke liye kaam karta hai — isliye yeh *koi bhi* protocol accelerate karta hai: gaming, VoIP, IoT, MQTT, custom TCP — sirf HTTP nahi.
- Endpoints **ALBs, NLBs, EC2 instances, ya Elastic IPs** hote hain, region ke hisaab se grouped. **Traffic dials** regions ke beech percentages shift karte hain aur **endpoint weights** ek ke andar; yahi tareeka hai controlled regional cutover ya regions ke across blue/green karne ka.
- **Failover fast hai (~30 seconds) aur DNS-independent hai** — IPs kabhi change nahi hote, isliye koi client, resolver, ya ISP TTL cache ise delay nahi kar sakta. Yeh Route 53 latency/failover routing ke against iska single biggest advantage hai.
- "Hume ek partner allowlist ke liye ek **static IP** chahiye" problem solve karta hai jo ek ALB nahi kar sakta (dekho [Load Balancing Fundamentals](08-load-balancing-autoscaling.md#load-balancing-fundamentals)).

### CloudFront vs Global Accelerator

Dono same network par edge services hain, isliye comparison constantly poochha jaata hai:

| | **CloudFront** | **Global Accelerator** |
|---|---|---|
| Layer | 7 (HTTP/HTTPS) | **4 (TCP/UDP — koi bhi protocol)** |
| Content cache karta hai? | ✅ **Haan** — yehi point hai | ❌ Nahi, kabhi bhi caching nahi karta — yeh pure network routing hai |
| Entry point | Edge location, DNS se resolve hota hai | **2 static anycast IPs** |
| Best for | Static assets, websites, video streaming, cacheable APIs | Non-HTTP protocols, gaming, VoIP, IoT; multi-region failover; static-IP requirements |
| Failover speed | Origin groups / DNS-dependent | **~30 s, no DNS dependency** |
| Cacheable content improve karta hai | ✅ Dramatically | Sirf better network path ke zariye |

**One-liner:** "CloudFront HTTP content ko edge par cache karta hai; Global Accelerator kuch bhi cache nahi karta — yeh sirf koi bhi TCP/UDP traffic ko AWS backbone par jaldi la deta hai aur mujhe two static IPs deta hai fast regional failover ke saath. Cacheable web content → CloudFront. Dynamic, non-HTTP, ya static-IP/fast-failover requirements → Global Accelerator. Yeh dono combine ho sakte hain."

### Hands-On: CloudFront & Global Accelerator

```bash
# CloudFront in front of a private S3 bucket (the standard static-site setup)
aws cloudfront create-origin-access-control --origin-access-control-config \
  'Name=s3-oac,SigningProtocol=sigv4,SigningBehavior=always,OriginAccessControlOriginType=s3'
# then: create the distribution with the S3 origin + that OAC, default root object index.html
# and add the generated bucket policy allowing cloudfront.amazonaws.com with AWS:SourceArn = distribution ARN

aws cloudfront create-invalidation --distribution-id E123ABC --paths "/index.html" "/static/*"
aws cloudfront get-distribution --id E123ABC --query 'Distribution.Status'   # InProgress → Deployed

# Global Accelerator
aws globalaccelerator create-accelerator --name prod-ga --ip-address-type IPV4
aws globalaccelerator create-listener --accelerator-arn <arn> --protocol TCP --port-ranges FromPort=443,ToPort=443
aws globalaccelerator create-endpoint-group --listener-arn <arn> \
  --endpoint-group-region us-east-1 --traffic-dial-percentage 100 \
  --endpoint-configurations EndpointId=<alb-arn>,Weight=100,ClientIPPreservationEnabled=true
```
**Verification habits:** CloudFront ke liye, **`X-Cache`** response header check karo (`Hit from cloudfront` vs `Miss from cloudfront`) aur **cache hit ratio** metric dekho — ek low ratio almost hamesha matlab hai ki aap cache key mein bahut zyada headers/cookies/query strings forward kar rahe ho. Global Accelerator ke liye, confirm karo ki do static IPs resolve hote hain aur traffic dial shift karne se pehle endpoint health `HEALTHY` hai.

**Disaster Recovery — Global Edge (CloudFront, Route 53, Global Accelerator)**

| | |
|---|---|
| **Actually risk par kya hai** | Distribution aur DNS configuration. Route 53 aur CloudFront **global** services hain, toh regional outage inhe girati nahi — yeh aapka failover *control point* hain |
| **Backup mechanism** | IaC. Route 53 AWS ki sabse durable services mein se hai; risk misconfiguration hai, loss nahi |
| **Realistic RPO / RTO** | RPO ~0. RTO propagation se bandha hai: CloudFront config **5–15 minutes**, DNS aapke record **TTL** se bandha |

**Recovery runbook:**
1. **Origin failure:** CloudFront **origin group** configure ho to specified status codes par secondary origin par failover automatic hai — koi human action nahi.
2. **Regional failure:** health check se juda Route 53 **failover record** traffic automatically shift kar deta hai; warna record update karo aur TTL ka wait karo.
3. **Static fallback serve karo:** origin ko ek S3 maintenance page par repoint karo — recover karte waqt timeout se kaafi behtar.
4. **Cut over ke baad cache invalidate karo**, warna clients pre-incident objects hi paate rehte hain.

⚠️ **Gotcha:** **edge failover pehle se wire hona chahiye, kyunki incident ke dauraan use configure karna incident se hi dheere hai.** CloudFront config change propagate hone mein 5–15 minutes leta hai, aur DNS record par 3600-second TTL ka matlab ek ghante tak clients dead region par hi jaate rahenge. **Jin records ko fail over karna hai un par TTL kam (60s) rakho** aur origin groups aur health checks pehle se configure karo — DR mein edge services ki poori value yehi hai ki yeh ek control point hai jise regional outage chhu bhi nahi sakti.

---

## 3. Edge Infrastructure

### Local Zones, Outposts & Wavelength

Teen tareeke jinse AWS ek region ko apne data centres se aage extend karta hai — sab "hume kisi cheez ke kareeb AWS chahiye" ke answers hain:

| | What it is | Use it for |
|---|---|---|
| **Local Zones** | Ek region ka extension jo ek **major metro area** mein rakha gaya hai, services ke ek subset ke saath (EC2, EBS, ECS, kuch ELB/RDS). Aap opt in karte ho, phir usme ek subnet banate ho | Ek specific city ke end users ke liye **single-digit-millisecond latency** — real-time gaming, live video production, remote workstations |
| **Outposts** | Physical **AWS racks aapke apne data centre mein installed**, same APIs run karte hue, AWS se managed | Data-residency mandates, ya on-prem systems (ek factory floor, ek hospital) ke liye bahut low latency jo cloud mein move nahi ho sakte |
| **Wavelength Zones** | AWS compute jo **5G telco networks ke andar** embedded hai, isliye traffic carrier network kabhi nahi chhodta | Ultra-low-latency mobile: AR/VR, connected vehicles, live mobile inference |

Framing jo lands: "Regions aur AZs zyadatar latency needs cover karte hain. Local Zones compute ko ek metro mein le jaate hain; Outposts ise *meri* building mein le jaate hain; Wavelength ise carrier ke 5G network mein le jaata hai. Har ek progressively zyada specialised hai — aur progressively zyada expensive kam services availability ke saath."

---

← [Messaging, Streaming & Decoupling](13-messaging-streaming.md) · [Index](README.md) · [Management, Organizations & Billing](15-management-org-billing.md) →
