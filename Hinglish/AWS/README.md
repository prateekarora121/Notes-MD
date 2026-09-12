# AWS Interview Guide (Senior .NET Full-Stack / Lead Level)

> Personal notes se consolidate kiya gaya hai. Audience: 10-year .NET full-stack developer jo AWS par workloads deploy karta hai aur senior/lead interviews ke liye prepare kar raha hai. Fundamentals already pata hona assume kiya gaya hai; focus nuance, trade-offs, "why", gotchas, aur interviewer follow-ups par hai.
>
> Jin sections par **[new content]** mark hai, wo consolidation ke dauraan add kiye gaye taaki current (2026) senior AWS interview expectations ke gaps fill ho sakein. Baaki sab original notes se reorganize/de-duplicate kiya gaya hai, aur technically correct content preserve kiya gaya hai.

> Topic-wise split of the original single-file guide. Condensed/expanded counterpart: [Quick Revision Notes](revision/README.md).

---

## Start Here

- **[Resume-Aligned Priority Map](00-resume-aligned-priority-map.md)** - tiering of every topic below, the Azure→AWS translation table, and the resume follow-ups to expect. Read this first; it tells you which of the files below to revise hardest.

## PART I — Tier 1: Resume-Claimed Core

> **Bulletproof required.** Iss part ka har topic explicitly mere resume par named hai (Lambda, DynamoDB, EC2, S3, Terraform/CDKTF, GitHub Actions) ya unhe use karne mein unavoidable hai (IAM, CloudWatch). Interviewers claimed skills par sabse zyada drill karte hain, isliye yeh sections sabse pehle aur sabse zyada revise hote hain.

- [Serverless & Lambda](01-serverless-lambda.md)
- [DynamoDB](02-dynamodb.md)
- [IAM & Security](03-iam-security.md)
- [Infrastructure as Code & CI/CD](04-iac-cicd.md)
- [S3](05-s3.md)
- [EC2 & Instance Storage](06-ec2-instance-storage.md)
- [Observability & Monitoring](07-observability-monitoring.md)

## PART II — Tier 2: Design-Level Confidence

> **Iske baare mein reason karo; hands-on gaps ke baare mein honest raho.** Yeh "X ko kaise architect karoge?" jaise questions mein aate hain, jahan structured reasoning operational war stories se zyada matter karti hai. Jahan mujhe production experience nahi hai wahan section explicitly yeh bolta hai — yeh framing consistently ek confident wrong answer se better land karti hai.

- [Load Balancing, Scalability & Auto Scaling](08-load-balancing-autoscaling.md)
- [Containers: Docker, ECS, ECR & Fargate](09-containers-ecs-fargate.md)
- [Relational Databases, Caching & Analytics](10-databases-caching-analytics.md)
- [Networking](11-networking.md)
- [Security Services](12-security-services.md)

## PART III — Tier 3: Breadth — Recognise and Place

> **Har ek ke liye ek clean sentence.** Kisi bhi .NET full-stack engineer se yeh expect nahi kiya jaata ki usne yeh services operate ki hongi. Lekin service naam ko *recognise* na karna ek gap ki tarah padhta hai, jabki yeh jaanna ki woh kaunsa problem solve karta hai breadth ki tarah padhta hai. Decision boundaries seekho, configuration details nahi.

- [Messaging, Streaming & Decoupling](13-messaging-streaming.md)
- [Global Edge Services](14-global-edge-services.md)
- [Management, Organizations & Billing](15-management-org-billing.md)
- [Cost & Performance](16-cost-performance.md)
- [Migration & Data Transfer](17-migration-data-transfer.md)
- [Well-Architected & Resilience](18-well-architected-resilience.md)

## PART IV — Cross-Cutting Reference

> Consolidated best practices, pitfalls, long-form Q&A, aur yeh guide kaise assemble hui uska changelog.

- [Cross-Cutting Reference](19-cross-cutting-reference.md)
- [Changelog — Summary of Additions](20-changelog-additions.md)

---

## Quick Revision Notes

Every topic above has a condensed counterpart in [revision/](revision/README.md) - same filenames, same order.
