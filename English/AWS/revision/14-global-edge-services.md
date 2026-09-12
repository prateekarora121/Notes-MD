> **AWS Quick Revision Notes** · [Index](README.md) · Part III

# Global Edge Services

**Geography:** Region (area) · AZ (isolated DCs, low-latency fibre) · Edge Location/PoP (600+ CloudFront caches, for caching/backbone entry, not workloads).

---

## 1. CloudFront

### CloudFront (CDN)

Caches at edge near users. Beyond speed: origin offload (cached hits skip origin, free CloudFront→AWS-origin egress), **Shield Standard built in + WAF**, TLS at edge (free ACM), HTTP/2/3, compression. Object model: **Distribution** → **Origins** (S3/ALB/API GW/EC2/any HTTP) → **Cache Behaviours** (path patterns → origin + caching/forwarding/TLS). Cache controls: **cache key** (forward as little as possible — forwarding all headers/cookies destroys hit rate), TTLs, invalidations (slow + billed → prefer **versioned filenames** `app.a1b2c3.js`). **OAC** (modern, replaces OAI — signs requests to private S3, supports SSE-KMS + all methods). Restrict: **Signed URL** (one file) / **Signed Cookie** (many files) / **Geo restriction**. **❗ CloudFront signed URL vs S3 presigned:** presigned = S3 API, generator's IAM perms, direct to S3 (bypasses cache/WAF); CloudFront signed = key pair, through edge (keeps cache/WAF/Shield/logging), any origin, bucket stays private. Edge compute: **CloudFront Functions** (lightweight JS, sub-ms, viewer only, no network — headers/rewrites/redirects/simple auth) vs **Lambda@Edge** (Node/Python, viewer+origin, can call services — origin selection, DB/API, image transform). Price Classes (All/200/100), Origin Groups (failover), field-level encryption, standard/real-time logs. **❗ CloudFront custom-domain cert must be in us-east-1**.

---

## 2. Global Accelerator

### Global Accelerator

**Two static anycast IPs** fronting the app; traffic enters AWS backbone at nearest edge → closest healthy regional endpoint. **L4 TCP/UDP** (any protocol — gaming/VoIP/IoT). Endpoints ALB/NLB/EC2/EIP; traffic dials (regions) + weights (within region). **Failover ~30s DNS-independent** (IPs never change) — biggest edge over Route 53. Solves the static-IP requirement an ALB can't.

### CloudFront vs Global Accelerator

| | CloudFront | Global Accelerator |
|---|---|---|
| Layer | 7 HTTP | **4 TCP/UDP any** |
| Caches | ✅ | ❌ never |
| Entry | Edge via DNS | **2 static anycast IPs** |
| Best for | Static/websites/video/cacheable API | Non-HTTP, gaming/VoIP/IoT, multi-region failover, static IP |
| Failover | Origin groups/DNS | ~30s no DNS |

Cacheable web → CloudFront; dynamic/non-HTTP/static-IP/fast-failover → Global Accelerator. Combinable.

---

## 3. Edge Infrastructure

### Local Zones, Outposts, Wavelength

Local Zones (region extension in a metro, subset of services — single-digit-ms to a city) · Outposts (**AWS racks in your DC** — residency, low latency to on-prem) · Wavelength (compute in **5G telco networks** — ultra-low-latency mobile). Progressively specialised + expensive.

**Global Edge DR:** at risk = distribution + DNS config (Route 53 + CloudFront **global** → regional outage doesn't take them; they're the failover control point). Backup = IaC (Route 53 very durable, risk is misconfig). RTO propagation-bound: CloudFront 5–15 min, DNS = TTL. Origin failure → CloudFront **origin group** auto-failover; regional → Route 53 failover records + health check; static S3 fallback page; invalidate after cutover. **❗ Edge failover must be pre-wired** (config change slower than the incident) → low TTLs (60s) on failover records + pre-configured origin groups/health checks.

---

← [Messaging, Streaming & Decoupling](13-messaging-streaming.md) · [Index](README.md) · [Management, Organizations & Billing](15-management-org-billing.md) →
