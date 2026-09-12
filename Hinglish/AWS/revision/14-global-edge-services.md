> **AWS Quick Revision Notes** · [Index](README.md) · Part III

# Global Edge Services

> Region (geo area) / AZ (isolated DCs in region) / Edge Location (600+ CloudFront caches, for caching + backbone entry).

---

## 1. CloudFront

### CloudFront (CDN)

- Global CDN caches content at edge. Gives: lower latency, **origin offload** (cached hits skip ALB/S3, free CloudFront→origin), Shield Standard + WAF at edge, TLS/HTTP2/3 + compression.
- **Model:** Distribution → Origins (S3/ALB/API GW/EC2/any HTTP) + Cache Behaviours (path patterns → origin + caching/forwarding/TLS).
- **Caching:** cache key (forward minimal — every header/cookie kills hit rate), TTLs, invalidations (billed/slow → prefer **versioned filenames** `app.a1b2c3.js`).
- **OAC** (Origin Access Control, replaces OAI): CloudFront → private S3 (bucket fully closed, supports SSE-KMS + all methods).
- Access control: Signed URL (one file), Signed Cookie (multiple files/sections), Geo restriction.
- ❗ **CloudFront signed URL vs S3 presigned URL:** presigned = S3 API, generator's IAM perms, hits S3 directly, bypasses cache; CloudFront signed = CloudFront key pair, edge-served (cache/WAF/Shield/logging), any origin, keeps bucket private. Content through CDN → CloudFront signing; direct programmatic → S3 presign.
- **Edge compute:** CloudFront Functions (lightweight JS, sub-ms, viewer only, no network) vs Lambda@Edge (Node/Python, 5-30s, all 4 triggers, can call services).
- Price Classes, Origin Groups (failover), field-level encryption, standard/real-time logs.
- ❗ **CloudFront cert must be us-east-1** (global service, N. Virginia managed).

---

## 2. Global Accelerator

### Global Accelerator

- **2 static anycast IPs**, traffic enters nearest edge → AWS backbone → nearest healthy endpoint. **L4 TCP/UDP** (any protocol: gaming/VoIP/IoT). Endpoints = ALB/NLB/EC2/EIP. Traffic dials + endpoint weights. **Fast ~30s DNS-independent failover** (IPs never change). Solves static-IP-for-partner-allowlist.

| | CloudFront | Global Accelerator |
|---|---|---|
| Layer | 7 HTTP | 4 TCP/UDP |
| Caches | ✅ | ❌ |
| Entry | edge (DNS) | 2 static IPs |
| Failover | origin groups/DNS | ~30s, no DNS |

---

## 3. Edge Infrastructure

### Local Zones / Outposts / Wavelength

- **Local Zones** = region extension in metro (single-digit-ms for a city). **Outposts** = AWS racks in your DC (data residency, on-prem low latency). **Wavelength** = compute in 5G telco network (ultra-low mobile).

**DR — Global Edge:** R53/CloudFront global (regional outage = control point). Backup = IaC. RPO ~0; RTO = propagation (CloudFront 5-15min, DNS TTL). Origin failure → CloudFront **origin group**; regional → R53 failover record; static fallback (S3 maintenance page); invalidate after cutover. ⚠️ Wire edge failover **beforehand** (config propagation slow); keep failover-record TTL low (60s).

---

← [Messaging, Streaming & Decoupling](13-messaging-streaming.md) · [Index](README.md) · [Management, Organizations & Billing](15-management-org-billing.md) →
