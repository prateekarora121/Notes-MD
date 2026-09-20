# AWS Interview Guide (Senior .NET Full-Stack / Lead Level)

> Consolidated from personal notes. Audience: 10-year .NET full-stack developer deploying workloads to AWS, prepping for senior/lead interviews. Fundamentals assumed; focus is on nuance, trade-offs, "why", gotchas, and interviewer follow-ups.
>
> Sections marked **[new content]** were added during consolidation to fill gaps versus current (2026) senior AWS interview expectations. Everything else is reorganized/de-duplicated from the original notes with technically correct content preserved.

> Topic-wise split of the original single-file guide. Condensed/expanded counterpart: [Quick Revision Notes](revision/README.md).

---

## Start Here

- **[Resume-Aligned Priority Map](/Hinglish/AWS/Detailed/00-resume-aligned-priority-map.md)** - tiering of every topic below, the Azure→AWS translation table, and the resume follow-ups to expect. Read this first; it tells you which of the files below to revise hardest.

## PART I — Tier 1: Resume-Claimed Core

> **Bulletproof required.** Every topic in this part is explicitly named on my resume (Lambda, DynamoDB, EC2, S3, Terraform/CDKTF, GitHub Actions) or unavoidable in using them (IAM, CloudWatch). Interviewers drill hardest on claimed skills, so these sections get revised first and most often.

- [Serverless & Lambda](01-serverless-lambda.md)
- [DynamoDB](02-dynamodb.md)
- [IAM & Security](03-iam-security.md)
- [Infrastructure as Code & CI/CD](04-iac-cicd.md)
- [S3](05-s3.md)
- [EC2 & Instance Storage](06-ec2-instance-storage.md)
- [Observability & Monitoring](07-observability-monitoring.md)

## PART II — Tier 2: Design-Level Confidence

> **Reason about it; be honest about hands-on gaps.** These appear in "how would you architect X?" questions, where structured reasoning matters more than operational war stories. Where I lack production experience the section says so explicitly — that framing consistently lands better than a confident wrong answer.

- [Load Balancing, Scalability & Auto Scaling](08-load-balancing-autoscaling.md)
- [Containers: Docker, ECS, ECR & Fargate](09-containers-ecs-fargate.md)
- [Relational Databases, Caching & Analytics](10-databases-caching-analytics.md)
- [Networking](11-networking.md)
- [Security Services](12-security-services.md)

## PART III — Tier 3: Breadth — Recognise and Place

> **One clean sentence each.** Nobody expects a .NET full-stack engineer to have operated these. But not *recognising* a service name reads as a gap, whereas knowing what problem it solves reads as breadth. Learn the decision boundaries, not the configuration details.

- [Messaging, Streaming & Decoupling](13-messaging-streaming.md)
- [Global Edge Services](14-global-edge-services.md)
- [Management, Organizations & Billing](15-management-org-billing.md)
- [Cost & Performance](16-cost-performance.md)
- [Migration & Data Transfer](17-migration-data-transfer.md)
- [Well-Architected & Resilience](18-well-architected-resilience.md)

## PART IV — Cross-Cutting Reference

> Consolidated best practices, pitfalls, long-form Q&A, and the changelog of how this guide was assembled.

- [Cross-Cutting Reference](19-cross-cutting-reference.md)
- [Changelog — Summary of Additions](20-changelog-additions.md)

---

## Quick Revision Notes

Every topic above has a condensed counterpart in [revision/](revision/README.md) - same filenames, same order.
