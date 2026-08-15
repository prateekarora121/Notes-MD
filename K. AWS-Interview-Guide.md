# AWS Interview Guide (Senior .NET Full-Stack / Lead Level)

> Consolidated from personal notes. Audience: 10-year .NET full-stack developer deploying workloads to AWS, prepping for senior/lead interviews. Fundamentals assumed; focus is on nuance, trade-offs, "why", gotchas, and interviewer follow-ups.
>
> Sections marked **[new content]** were added during consolidation to fill gaps versus current (2026) senior AWS interview expectations. Everything else is reorganized/de-duplicated from the original notes with technically correct content preserved.

---

## Table of Contents

1. [How to Use This Guide: Resume-Aligned Priority Map](#how-to-use-this-guide-resume-aligned-priority-map)
   - [Tier 1 — Must be bulletproof (explicitly claimed on my resume)](#tier-1--must-be-bulletproof-explicitly-claimed-on-my-resume)
   - [Tier 2 — Must reason about confidently, while being honest about hands-on gaps](#tier-2--must-reason-about-confidently-while-being-honest-about-hands-on-gaps)
   - [Tier 3 — Recognise, place, and give one clean sentence](#tier-3--recognise-place-and-give-one-clean-sentence)
   - [Azure → AWS Translation (I hold AZ-900, and shipped on Cosmos DB + Azure Blob)](#azure--aws-translation-i-hold-az-900-and-shipped-on-cosmos-db--azure-blob)
   - [Resume Deep-Dives — The Follow-Ups I Should Expect](#resume-deep-dives--the-follow-ups-i-should-expect)

**PART I — Tier 1: Resume-Claimed Core**

2. [Serverless & Lambda](#serverless--lambda)
   - [AWS Lambda Deep Dive](#aws-lambda-deep-dive)
   - [Lambda Concurrency Model](#lambda-concurrency-model)
   - [Lambda vs ECS vs EC2 vs Fargate](#lambda-vs-ecs-vs-ec2-vs-fargate)
   - [Serverless & the S3 → Lambda Trigger Pattern](#serverless--the-s3--lambda-trigger-pattern)
3. [DynamoDB](#dynamodb)
   - [DynamoDB Deep Dive](#dynamodb-deep-dive)
   - [DynamoDB Trick Questions](#dynamodb-trick-questions)
4. [IAM & Security](#iam--security)
   - [IAM Overview, Root Account & Shared Responsibility](#iam-overview-root-account--shared-responsibility)
   - [Users, Groups & Permissions](#users-groups--permissions)
   - [Hands-On: Users & Groups](#hands-on-users--groups)
   - [Policy Types, Structure & Password Policy](#policy-types-structure--password-policy)
   - [MFA (Multi-Factor Authentication)](#mfa-multi-factor-authentication)
   - [Access to AWS: Console, CLI, SDK & Access Keys](#access-to-aws-console-cli-sdk--access-keys)
   - [Hands-On: MFA & Access Keys](#hands-on-mfa--access-keys)
   - [IAM Roles, Policies, AssumeRole](#iam-roles-policies-assumerole)
   - [Hands-On: IAM Roles](#hands-on-iam-roles)
   - [IAM Security Tools](#iam-security-tools)
   - [IAM Pitfalls](#iam-pitfalls)
   - [[new content] Secrets Manager vs Parameter Store](#new-content-secrets-manager-vs-parameter-store)
   - [[new content] Least Privilege & Permission Boundaries in Practice](#new-content-least-privilege--permission-boundaries-in-practice)
   - [IAM Rapid-Fire Q&A](#iam-rapid-fire-qa)
5. [Infrastructure as Code & CI/CD](#infrastructure-as-code--cicd)
   - [AWS CodeCommit](#aws-codecommit)
   - [AWS CodeBuild](#aws-codebuild)
   - [AWS CodePipeline](#aws-codepipeline)
   - [CodePipeline/CodeBuild Trap Scenarios](#codepipelinecodebuild-trap-scenarios)
   - [[gaps] CloudFormation vs Terraform/CDKTF](#gaps-cloudformation-vs-terraformcdktf)
   - [Terraform/CDKTF in Practice — Depth Questions to Expect](#terraformcdktf-in-practice--depth-questions-to-expect)
6. [S3](#s3)
   - [S3 Buckets & Objects](#s3-buckets--objects)
   - [S3 Bucket Policies & Access Control](#s3-bucket-policies--access-control)
   - [S3 Static Website Hosting](#s3-static-website-hosting)
   - [S3 Versioning & Replication](#s3-versioning--replication)
   - [S3 Performance, Analytics & Cost Tooling](#s3-performance-analytics--cost-tooling)
   - [S3 Batch Operations](#s3-batch-operations)
   - [S3 Requester Pays](#s3-requester-pays)
   - [S3 Best Practices](#s3-best-practices)
   - [S3 Shared Responsibility Model](#s3-shared-responsibility-model)
   - [[new content] S3 Storage Classes & Lifecycle Policies](#new-content-s3-storage-classes--lifecycle-policies)
   - [[gaps] S3 Lifecycle Rules in Practice — Real Patterns & Terraform](#gaps-s3-lifecycle-rules-in-practice--real-patterns--terraform)
   - [S3 Security: Encryption & Its Four Types](#s3-security-encryption--its-four-types)
   - [S3 CORS](#s3-cors)
   - [S3 MFA Delete](#s3-mfa-delete)
   - [S3 Access Logs (and the Warning)](#s3-access-logs-and-the-warning)
   - [S3 Pre-Signed URLs](#s3-pre-signed-urls)
   - [S3 Object Lock & Glacier Vault Lock](#s3-object-lock--glacier-vault-lock)
   - [S3 Access Points & Object Lambda](#s3-access-points--object-lambda)
   - [S3 Security Best Practices & Shared Responsibility](#s3-security-best-practices--shared-responsibility)
7. [EC2 & Instance Storage](#ec2--instance-storage)
   - [EC2 Fundamentals](#ec2-fundamentals)
   - [EC2 Instance Types, User Data & Metadata](#ec2-instance-types-user-data--metadata)
   - [Security Groups, Their Properties & Classic Ports](#security-groups-their-properties--classic-ports)
   - [Public IP vs Private IP vs Elastic IP](#public-ip-vs-private-ip-vs-elastic-ip)
   - [Placement Groups](#placement-groups)
   - [Elastic Network Interfaces (ENIs)](#elastic-network-interfaces-enis)
   - [EC2 Hibernate](#ec2-hibernate)
   - [EC2 Purchasing Options — The Complete Set](#ec2-purchasing-options--the-complete-set)
   - [EC2 Shared Responsibility Model](#ec2-shared-responsibility-model)
   - [[gaps] EC2 Sizing, Pricing Decisions & CPU Credit Gotchas](#gaps-ec2-sizing-pricing-decisions--cpu-credit-gotchas)
   - [[new content] EBS vs EFS vs S3](#new-content-ebs-vs-efs-vs-s3)
   - [EBS Volumes](#ebs-volumes)
   - [EBS Volume Types](#ebs-volume-types)
   - [EBS Snapshots](#ebs-snapshots)
   - [AMIs (Amazon Machine Images)](#amis-amazon-machine-images)
   - [Instance Store](#instance-store)
   - [EBS Multi-Attach](#ebs-multi-attach)
   - [EBS Encryption](#ebs-encryption)
   - [EFS (Elastic File System)](#efs-elastic-file-system)
   - [EFS vs EBS vs Instance Store](#efs-vs-ebs-vs-instance-store)
   - [EC2 Storage Shared Responsibility Model](#ec2-storage-shared-responsibility-model)
8. [Observability & Monitoring](#observability--monitoring)
   - [CloudWatch Deep Dive](#cloudwatch-deep-dive)
   - [[new content] CloudWatch vs X-Ray: Complementary, Not Competing](#new-content-cloudwatch-vs-x-ray-complementary-not-competing)
   - [CloudTrail](#cloudtrail)
   - [AWS Health Dashboard](#aws-health-dashboard)
   - [Container Insights, the CloudWatch Agent & Proactive Monitoring](#container-insights-the-cloudwatch-agent--proactive-monitoring)

**PART II — Tier 2: Design-Level Confidence**

9. [Containers: Docker, ECS, ECR & Fargate](#containers-docker-ecs-ecr--fargate)
   - [AWS Fargate](#aws-fargate)
   - [EC2 vs Fargate Cost & Trap Scenarios](#ec2-vs-fargate-cost--trap-scenarios)
   - [Docker & Container Fundamentals](#docker--container-fundamentals)
   - [ECS (Elastic Container Service)](#ecs-elastic-container-service)
   - [ECR (Elastic Container Registry)](#ecr-elastic-container-registry)
   - [Hands-On: ECS with Fargate](#hands-on-ecs-with-fargate)
   - [[new content] ECS vs EKS vs Fargate vs Lambda for .NET Workloads](#new-content-ecs-vs-eks-vs-fargate-vs-lambda-for-net-workloads)
   - [[gaps] Fargate/ECS/EKS Trade-offs — Reasoning Without Hands-On Time](#gaps-fargateecseks-trade-offs--reasoning-without-hands-on-time)
   - [[new content] Deploying .NET to AWS: Elastic Beanstalk vs ECS vs Lambda Custom Runtime](#new-content-deploying-net-to-aws-elastic-beanstalk-vs-ecs-vs-lambda-custom-runtime)
10. [Relational Databases, Caching & Analytics](#relational-databases-caching--analytics)
   - [[new content] RDS Multi-AZ vs Read Replicas vs Aurora](#new-content-rds-multi-az-vs-read-replicas-vs-aurora)
   - [[gaps] Multi-AZ vs Read Replica — The #1 Confused Pair](#gaps-multi-az-vs-read-replica--the-1-confused-pair)
   - [Databases & Analytics Overview: Choosing the Right Store](#databases--analytics-overview-choosing-the-right-store)
   - [Relational Databases & RDS — The Operational Surface](#relational-databases--rds--the-operational-surface)
   - [Athena](#athena)
   - [RDS Proxy](#rds-proxy)
   - [Aurora Advanced Features](#aurora-advanced-features)
   - [ElastiCache & Caching Patterns](#elasticache--caching-patterns)
11. [Networking](#networking)
   - [VPC, Subnets, NAT — Complete Model](#vpc-subnets-nat--complete-model)
   - [[new content] VPC Reference Architecture](#new-content-vpc-reference-architecture)
   - [[gaps] VPC/Subnet/NAT/SG Rapid-Fire Drill Sheet](#gaps-vpcsubnetnatsg-rapid-fire-drill-sheet)
   - [VPC Flow Logs](#vpc-flow-logs)
   - [VPC Peering](#vpc-peering)
   - [Transit Gateway](#transit-gateway)
   - [VPC Endpoints & PrivateLink](#vpc-endpoints--privatelink)
   - [Hybrid Connectivity: Site-to-Site VPN & Direct Connect](#hybrid-connectivity-site-to-site-vpn--direct-connect)
   - [Hands-On: VPC](#hands-on-vpc)
   - [Route 53](#route-53)
   - [API Gateway Auth & Integration Patterns](#api-gateway-auth--integration-patterns)
12. [Load Balancing, Scalability & Auto Scaling](#load-balancing-scalability--auto-scaling)
   - [Scalability, High Availability, Elasticity & Agility](#scalability-high-availability-elasticity--agility)
   - [ALB vs API Gateway vs ELB (NLB/GWLB/CLB)](#alb-vs-api-gateway-vs-elb-nlbgwlbclb)
   - [ELB Deep-Dive: Cross-Zone Load Balancing, 504 Timeouts & Shield DDoS Protection](#elb-deep-dive-cross-zone-load-balancing-504-timeouts--shield-ddos-protection)
   - [Load Balancing Fundamentals](#load-balancing-fundamentals)
   - [Sticky Sessions (Session Affinity)](#sticky-sessions-session-affinity)
   - [Connection Draining / Deregistration Delay](#connection-draining--deregistration-delay)
   - [Auto Scaling Groups (ASG)](#auto-scaling-groups-asg)
   - [Scalability Best Practices](#scalability-best-practices)
   - [Scalability & Load Balancing Shared Responsibility Model](#scalability--load-balancing-shared-responsibility-model)
13. [Messaging, Streaming & Decoupling](#messaging-streaming--decoupling)
   - [SQS & SNS Fundamentals](#sqs--sns-fundamentals)
   - [CQRS with SNS/SQS in .NET](#cqrs-with-snssqs-in-net)
   - [CQRS + SNS/SQS Interview Pitfalls](#cqrs--snssqs-interview-pitfalls)
   - [[new content] EventBridge Deep Dive](#new-content-eventbridge-deep-dive)
   - [[new content] Event-Driven Architecture Reference Flow](#new-content-event-driven-architecture-reference-flow)
   - [Amazon Kinesis](#amazon-kinesis)
   - [Step Functions: Orchestration vs Choreography](#step-functions-orchestration-vs-choreography)
   - [Amazon MQ](#amazon-mq)
14. [Global Edge Services](#global-edge-services)
   - [CloudFront (CDN)](#cloudfront-cdn)
   - [AWS Global Accelerator](#aws-global-accelerator)
   - [CloudFront vs Global Accelerator](#cloudfront-vs-global-accelerator)
   - [Local Zones, Outposts & Wavelength](#local-zones-outposts--wavelength)
   - [Hands-On: CloudFront & Global Accelerator](#hands-on-cloudfront--global-accelerator)
15. [Security Services](#security-services)
   - [Overview: Which Service Answers Which Question](#overview-which-service-answers-which-question)
   - [DDoS Protection: Shield & WAF](#ddos-protection-shield--waf)
   - [AWS Network Firewall](#aws-network-firewall)
   - [KMS & CloudHSM](#kms--cloudhsm)
   - [ACM (AWS Certificate Manager)](#acm-aws-certificate-manager)
   - [AWS Systems Manager (SSM)](#aws-systems-manager-ssm)
   - [AWS Artifact](#aws-artifact)
   - [GuardDuty](#guardduty)
   - [Inspector](#inspector)
   - [Macie](#macie)
   - [AWS Config](#aws-config)
   - [Security Hub & Detective](#security-hub--detective)
   - [Defence in Depth — The Summary Answer](#defence-in-depth--the-summary-answer)

**PART III — Tier 3: Breadth — Recognise and Place**

16. [Management, Organizations & Billing](#management-organizations--billing)
   - [AWS Organizations](#aws-organizations)
   - [Service Control Policies (SCPs)](#service-control-policies-scps)
   - [Consolidated Billing](#consolidated-billing)
   - [AWS Control Tower](#aws-control-tower)
   - [AWS Resource Access Manager (RAM)](#aws-resource-access-manager-ram)
   - [Cost Explorer](#cost-explorer)
   - [AWS Budgets](#aws-budgets)
   - [Cost Anomaly Detection](#cost-anomaly-detection)
   - [Trusted Advisor](#trusted-advisor)
   - [AWS Support Plans](#aws-support-plans)
   - [Free Tier, Pricing & Estimating Cost](#free-tier-pricing--estimating-cost)
17. [Cost & Performance](#cost--performance)
   - [[new content] Cost Optimization: Savings Plans, Reserved, Spot](#new-content-cost-optimization-savings-plans-reserved-spot)
18. [Migration & Data Transfer](#migration--data-transfer)
   - [The 7 Rs — Migration Strategies](#the-7-rs--migration-strategies)
   - [Database Migration Service (DMS)](#database-migration-service-dms)
   - [Snow Family](#snow-family)
   - [Storage Gateway, DataSync & Transfer Family](#storage-gateway-datasync--transfer-family)
19. [Well-Architected & Resilience](#well-architected--resilience)
   - [[new content] AWS Well-Architected Framework — 6 Pillars](#new-content-aws-well-architected-framework--6-pillars)
   - [[gaps] Well-Architected 6 Pillars — Rapid Recall Version](#gaps-well-architected-6-pillars--rapid-recall-version)
   - [[new content] Disaster Recovery Strategies](#new-content-disaster-recovery-strategies)
   - [Testing Resilience: Fault Injection Simulator & Resilience Hub](#testing-resilience-fault-injection-simulator--resilience-hub)

**PART IV — Cross-Cutting Reference**

20. [Best Practices](#best-practices)
21. [Common Pitfalls (Cross-Cutting)](#common-pitfalls-cross-cutting)
22. [Sample Interview Q&A](#sample-interview-qa)
23. [Summary of Additions](#summary-of-additions)
24. [Summary of [gaps] Additions (This Pass)](#summary-of-gaps-additions-this-pass)
25. [Summary of [iam-core] Additions (This Pass)](#summary-of-iam-core-additions-this-pass)
26. [Summary of [services-core] Additions (This Pass)](#summary-of-services-core-additions-this-pass)
27. [Summary of [resume-aligned] Restructure & Additions (This Pass)](#summary-of-resume-aligned-restructure--additions-this-pass)
   - [The restructure](#the-restructure)
   - [Resume-alignment content added](#resume-alignment-content-added)
   - [Remaining service gaps closed](#remaining-service-gaps-closed)
   - [Contradictions Flagged During Consolidation](#contradictions-flagged-during-consolidation)

## How to Use This Guide: Resume-Aligned Priority Map

This guide is deliberately broader than my hands-on experience. Interviewers do **not** distribute questions evenly — they drill hardest on what the resume *claims*, and skim the rest. So the sections below are weighted:

**My resume's AWS surface, verbatim:** `AWS (Lambda, DynamoDB, EC2, S3)` · `Terraform / CDKTF (Infrastructure as Code)` · `GitHub Actions (CI/CD)`, with the summary phrased as *"growing depth in AWS"* — which is deliberate and honest, and sets the expectation I should meet rather than oversell.

### Tier 1 — Must be bulletproof (explicitly claimed on my resume)

| Topic | Why | Where |
|---|---|---|
| **Lambda** | Named skill *and* a delivery bullet (scheduled jobs, 12 services, 99.9% uptime) | [Deep Dive](#aws-lambda-deep-dive) · [Concurrency](#lambda-concurrency-model) · [Scheduled jobs pattern](#resume-deep-dives--the-follow-ups-i-should-expect) |
| **DynamoDB** | Named skill, listed under both Cloud *and* Databases, plus "DynamoDB-backed microservices" | [Deep Dive](#dynamodb-deep-dive) · [Trick Questions](#dynamodb-trick-questions) · [DAX](#elasticache--caching-patterns) |
| **Terraform / CDKTF** | Headline IaC skill and the tool in my deployment-dashboard bullet | [In Practice](#terraformcdktf-in-practice--depth-questions-to-expect) · [vs CloudFormation](#gaps-cloudformation-vs-terraformcdktf) |
| **GitHub Actions → AWS** | Named CI/CD skill; the OIDC keyless pattern is the AWS half of it | [IAM Roles / OIDC](#iam-roles-policies-assumerole) · [Terraform CI/CD](#terraformcdktf-in-practice--depth-questions-to-expect) |
| **IAM roles & policies** | Unavoidable — every one of the above needs a role, and it's where most AWS debugging lands | [IAM & Security](#iam--security) · [Rapid-Fire](#iam-rapid-fire-qa) |
| **S3** | Named skill; also my Terraform state backend | [Buckets & Objects](#s3-buckets--objects) → [S3 Security](#s3-security-encryption--its-four-types) |
| **EC2** | Named skill, provisioned via CDKTF | [Fundamentals](#ec2-fundamentals) → [Purchasing Options](#ec2-purchasing-options--the-complete-set) |
| **CloudWatch** | Implied by "log maintenance and health checks, 99.9% uptime" | [Deep Dive](#cloudwatch-deep-dive) · [Container Insights & Synthetics](#container-insights-the-cloudwatch-agent--proactive-monitoring) |

### Tier 2 — Must reason about confidently, while being honest about hands-on gaps

ECS/Fargate · RDS/Aurora · VPC design · SQS/SNS/EventBridge · ALB/ASG · CloudFront · Step Functions · Secrets Manager. These come up in **design** questions ("how would you architect X?") where reasoning matters more than operational war stories. The guide already flags where I lack hands-on time — see [Fargate/ECS/EKS Trade-offs](#gaps-fargateecseks-trade-offs--reasoning-without-hands-on-time) and the RDS framing note in [Multi-AZ vs Read Replica](#gaps-multi-az-vs-read-replica--the-1-confused-pair).

**The framing that works** (and which I should use rather than bluffing): *"I haven't operated Fargate in production — my container work has been Docker locally and ECS-adjacent. What I can walk you through is how I'd choose between it and EC2 for this workload, and what I'd want to validate before committing."* Interviewers consistently reward that over a confident wrong answer, and it also protects me from a follow-up I can't survive.

### Tier 3 — Recognise, place, and give one clean sentence

Kinesis · Amazon MQ · Athena/Redshift/Glue · GuardDuty/Inspector/Macie/Config · Organizations/Control Tower/RAM · Direct Connect/VPN · Snow Family/DMS/Storage Gateway · Local Zones/Outposts · CloudHSM. Know what problem each solves and when you'd reach for it. Nobody expects depth here from a .NET full-stack engineer — but **not recognising the name** reads as a gap, whereas one accurate sentence reads as breadth.

### Azure → AWS Translation (I hold AZ-900, and shipped on Cosmos DB + Azure Blob)

My resume shows real Azure delivery (Cosmos DB, Azure Blob Storage at EY) plus AZ-900. Interviewers who spot that **will** ask "you've used Azure — how does that map?" Having the translation ready turns a perceived gap into demonstrated transferable depth.

| Azure (on my resume / AZ-900) | AWS equivalent | Worth noting |
|---|---|---|
| **Cosmos DB** | **DynamoDB** | Both NoSQL with partition keys and provisioned/on-demand throughput. Cosmos offers 5 tunable consistency levels and multi-model APIs; DynamoDB gives you eventual or strong only. **RU/s ↔ RCU/WCU** is the closest analogy, and both punish a low-cardinality partition key with a hot partition |
| **Azure Blob Storage** | **S3** | Containers ↔ buckets; blob tiers (Hot/Cool/Archive) ↔ storage classes; SAS tokens ↔ **pre-signed URLs** — that SAS↔presigned mapping is a strong thing to volunteer |
| Azure Functions | **Lambda** | Consumption plan ↔ standard Lambda; Premium plan pre-warmed instances ↔ **provisioned concurrency** |
| App Service | Elastic Beanstalk / ECS Fargate | |
| Azure SQL Database | **RDS / Aurora** | |
| Azure AD (Entra ID) | **IAM + IAM Identity Center** | Entra ID is an identity *provider*; IAM is authorisation *within* an account. Entra ID → AWS via **SAML/OIDC federation** is the real-world bridge |
| Azure Key Vault | **KMS + Secrets Manager** | AWS splits it: KMS for keys, Secrets Manager for secrets |
| Azure Monitor / App Insights | **CloudWatch + X-Ray** | Again split: metrics/logs vs distributed tracing |
| Azure DevOps Pipelines | CodePipeline / **GitHub Actions** | |
| ARM / Bicep | **CloudFormation** (and Terraform works against both) | The reason Terraform is a portable skill — same tool, both clouds |
| Resource Groups | *No direct equivalent* — closest is **tags + CloudFormation stacks**; account/OU boundaries do the heavy isolation | A genuine structural difference worth naming: AWS isolates with **accounts**, Azure with subscriptions/resource groups |
| Azure Service Bus | **SQS + SNS** (or **Amazon MQ** for AMQP compatibility) | Service Bus queues+topics map to SQS+SNS; if the app speaks AMQP natively, Amazon MQ is the lift-and-shift path |

### Resume Deep-Dives — The Follow-Ups I Should Expect

Each resume bullet invites a specific technical drill-down. These are the ones where a vague answer would undercut the claim.

**Bullet: "Build scheduled AWS Lambda jobs for automated log maintenance and health checks, sustaining 99.9% uptime across 12 platform services."**

Expect: *how are they scheduled? what does "log maintenance" mean? what does a health check actually check? how does that produce 99.9%?*

- **Scheduling** — **EventBridge scheduled rules** (`cron(0 2 * * ? *)` / `rate(5 minutes)`), or **EventBridge Scheduler** for the newer, higher-scale option with one-time schedules, time zones, and built-in retry/DLQ. Say EventBridge, not "CloudWatch Events" — same service, current name.
- **Log maintenance** — the concrete levers are **CloudWatch Logs retention policies** (log groups default to **Never Expire**, which is a silent, unbounded cost leak — setting retention is often the single biggest CloudWatch saving), **metric filters** to turn log patterns into alarmable metrics, **subscription filters** to stream logs onward, and **export to S3** with lifecycle rules to Glacier for anything needing long retention cheaply.
- **Health checks** — be precise about the layer: a Lambda probing service endpoints and publishing a **custom CloudWatch metric** (`PutMetricData`, or cheaper via **EMF**) is one design; **CloudWatch Synthetics canaries** are the managed version of the same idea; **Route 53 health checks** and **ALB target-group health checks** operate at DNS and load-balancer level respectively. Naming which one and why shows the distinction is understood.
- **Justifying 99.9%** — that's an error budget of roughly **43 minutes of downtime per month**. The credible answer ties the number to measurement: alarms on availability/error-rate metrics (via **Metric Math**, not raw counts), composite alarms to cut noise, SNS to on-call, and a dashboard per service. If asked "how did you *know* it was 99.9%?", the honest answer is which metric was measured over what window — not a marketing figure.
- Likely trap: *"what happens if the health-check Lambda itself fails?"* → the monitor needs monitoring: alarm on the function's `Errors`/`Throttles` **and** on **missing data** (`treat-missing-data: breaching`), because a monitor that stops running silently looks identical to "everything is fine."

**Bullet: "Deployment Dashboard UI integrating GitHub Actions with CDKTF/Terraform for AWS infrastructure provisioning… cutting manual deployment intervention by 40% and release cycle from 5 days to 3."**

Expect: *how does the pipeline authenticate to AWS? how do you prevent a bad apply? who approves prod?*
- **Authentication: GitHub OIDC assuming an IAM role** — no static access keys in GitHub secrets, with the trust policy scoped by `sub` to a specific repo **and branch/environment**. This is the single highest-value thing to lead with, and the JSON is in [IAM Roles](#iam-roles-policies-assumerole).
- **Safety: plan on PR (read-only role) → apply on merge (privileged role) from a saved plan file**, plus GitHub **environment protection rules** for a manual prod approval, and `tfsec`/`checkov` gates. See [Terraform in Practice](#terraformcdktf-in-practice--depth-questions-to-expect).
- **State: S3 backend + DynamoDB lock table** — and be ready for "what if two pipelines run at once?"

**Bullet: "CSRconnect features… DynamoDB-backed microservices."**

Expect the full DynamoDB drill: **access patterns first**, partition-key cardinality and hot partitions, **GSI vs LSI**, on-demand vs provisioned capacity, `Query` vs `Scan`, conditional writes for idempotency, the 400 KB item limit, and single-table design. All covered in [DynamoDB Deep Dive](#dynamodb-deep-dive) and [Trick Questions](#dynamodb-trick-questions) — this is the section to revise hardest, because it's the one AWS service my resume ties to a shipped product.

**Bullet: "Runtime dynamic-mapping mechanism that compiles dealer-specific business logic into in-memory assemblies (DLLs)."**

Not an AWS bullet, but it's the most distinctive engineering claim on the resume and will draw questions — expect probes on `AssemblyLoadContext` and unloadability, memory/leak risk from accumulating assemblies, caching compiled delegates, security of compiling input at runtime, and how it's tested. Worth preparing alongside the AWS material, since a weak answer *here* costs more than a weak answer on Kinesis.

**The 60-second "tell me about your AWS experience" answer:**
> "My hands-on AWS work is serverless and IaC-centred. At Nagarro I build .NET 8 microservices for a Dealership Management System with **DynamoDB**-backed services, run scheduled **Lambda** jobs for log maintenance and health checks across 12 platform services, and built a deployment dashboard that wires **GitHub Actions** to **CDKTF/Terraform** to provision **Lambda, DynamoDB, EC2, and S3** — which cut manual deployment intervention about 40% and took our release cycle from five days to three. Where I'm deliberately still growing is the container and relational side — ECS/Fargate and RDS I can reason about and design with, but I haven't operated them in production, and I'd rather tell you that than overstate it."

That structure — concrete services, a measurable outcome, then an unprompted honest boundary — is what makes the "growing depth in AWS" phrasing on my resume a strength rather than a hedge.

---

# PART I — Tier 1: Resume-Claimed Core

> **Bulletproof required.** Every topic in this part is explicitly named on my resume (Lambda, DynamoDB, EC2, S3, Terraform/CDKTF, GitHub Actions) or unavoidable in using them (IAM, CloudWatch). Interviewers drill hardest on claimed skills, so these sections get revised first and most often.

## Serverless & Lambda

> **Tier 1 — bulletproof.** Lambda is named on my resume *and* backs a delivery bullet (scheduled log-maintenance and health-check jobs across 12 platform services, 99.9% uptime). Expect the deepest questioning here. See [Resume Deep-Dives](#resume-deep-dives--the-follow-ups-i-should-expect) for the follow-ups that bullet invites.

### AWS Lambda Deep Dive

**What it is:** Serverless compute — upload code, AWS runs it on trigger, you pay per invocation + duration. No server/patch/scale management.

**Key characteristics**
- Event-driven (API Gateway, S3, SNS, DynamoDB Streams, SQS, EventBridge, etc.)
- Fully managed, auto-scales per concurrent request
- Billed per invocation count + duration(ms) × memory
- Runtimes: Node.js, Python, .NET 6/8 (AOT + arm64), Java, Go, custom runtime via Lambda Runtime API (Rust etc.)

**Real-world use cases**
| Category | Example |
|---|---|
| API backend | API Gateway + Lambda for low/medium traffic APIs |
| Event processing | S3 upload → resize/scan; DynamoDB Streams → audit log; Kinesis/Kafka → real-time |
| Automation/cron | EventBridge schedule → cleanup Lambda |
| Orchestration glue | Step Functions + Lambda for multi-service workflows |
| Data transformation | Small/medium ETL, CSV→JSON |
| Edge | Lambda@Edge for CloudFront customization |

**Execution model — Firecracker micro-VM lifecycle**

```
        first invocation / scale-out / redeploy
                        |
                        v
              +-------------------+
              |       INIT        |  runtime + global init loaded
              +---------+---------+
                        |
                        v
              +-------------------+
      +------>|      INVOKE       |  your handler runs
      |       +---------+---------+
      |                 | handler returns
      | warm reuse      v
      | (never          +-------------------+
      |  guaranteed)    |      FREEZE       |
      +-----------------+---------+---------+
                        |
                        | idle timeout / platform update
                        v
              +-------------------+
              |     SHUTDOWN      |  environment destroyed
              +-------------------+
```

1. **INIT (cold start):** new micro-VM, runtime bootstrap, static initializers, DI container build, DB connection setup — all in your "outside the handler" code.
2. **INVOKE:** handler runs with event + context; must finish inside configured timeout (max 15 min).
3. **FREEZE (warm reuse):** environment frozen after response; globals, connections, `/tmp` persist — this is why warm invocations are 1–10ms.
4. **SHUTDOWN:** AWS reclaims idle/outdated environments. No shutdown hook — state is lost, not rolled back.

**Cold start vs warm start**
- Cold: Java/.NET (JIT) → 300–1500ms; Node/Python → 50–200ms; **.NET Native AOT** → 50–100ms (huge win over JIT-based .NET).
- Warm: 1–10ms — reused environment, init code already executed.
- Mitigations: .NET AOT, small deployment package, avoid heavy DI container graphs, avoid VPC unless required, use Provisioned Concurrency for latency-sensitive APIs.

**Triggers**
| Type | Examples | Failure semantics |
|---|---|---|
| Synchronous | API Gateway, ALB, Step Functions, direct Invoke | Caller sees the error; caller must retry |
| Asynchronous | S3, SNS, CloudWatch Events, SES, EventBridge | Lambda retries automatically; DLQ on exhaustion |
| Poll-based (event source mapping) | DynamoDB Streams, Kinesis, MSK, SQS | Lambda polls internally; retries until success or maxReceiveCount/DLQ |

**Pros**
- Zero server management, auto-scale, pay-per-use, multi-AZ by default, deep AWS integration, trivial deployment (zip/image).

**Cons / when NOT to use**
- Cold start latency unsuitable for <50ms SLA APIs at the tail
- 15-minute hard timeout — no long-running jobs
- Vendor lock-in to AWS event model
- Max 10GB memory, 10GB ephemeral `/tmp` — not for ML training/huge batch
- Harder observability across distributed functions (need X-Ray + structured logging)

**Cost model:** invocations + GB-seconds (duration × memory) + optional Provisioned Concurrency hourly charge + data transfer if outside AWS network.

**[new content] .NET-specific Lambda considerations**
- Prefer **.NET 8 Native AOT** for latency-sensitive Lambdas — eliminates JIT warm-up, smaller deployment package, but: no runtime reflection-based DI magic (source-generated JSON serialization required via `System.Text.Json` source generators), some third-party libraries using reflection may break under trimming.
- `Amazon.Lambda.AspNetCoreServer` lets you host a full minimal API/ASP.NET Core app behind API Gateway/ALB with almost no code changes — useful for lift-and-shift of existing Web APIs, at the cost of a heavier cold start than a pure function handler.
- Avoid building a full `IServiceCollection`/`IServiceProvider` graph per cold start if you don't need it — construct dependencies manually or cache the `IServiceProvider` as a static field so it survives across warm invocations.
- EF Core inside Lambda: create the `DbContext`/connection pool **outside** the handler (static/singleton) to reuse across warm invocations; be careful with RDS Proxy or connection pool exhaustion at high concurrency (each concurrent execution environment = its own connection footprint).

**Versions, aliases & layers — the deployment surface** (needed for any "how do you deploy a Lambda safely?" question):
- **`$LATEST`** is mutable; **publishing a version** creates an immutable, numbered snapshot of code + configuration.
- An **alias** is a named, movable pointer to a version (`prod` → v7). Aliases support **weighted routing** across two versions, which is how you do a **canary/linear deploy** — shift 10% of traffic to v8, watch CloudWatch alarms, then complete or roll back by moving the alias. **CodeDeploy** automates exactly this with `Canary10Percent5Minutes`-style configs and automatic alarm-triggered rollback.
- **Layers** package shared dependencies or the **Lambda Extensions** used by agents (CloudWatch Lambda Insights, Parameters & Secrets extension) separately from function code — smaller deployment packages and one place to update a shared library. Limit: 5 layers per function, 250 MB unzipped total.
- **Destinations** route the *result* of an **asynchronous** invocation — `onSuccess` and `onFailure` — to SQS/SNS/EventBridge/Lambda. This is strictly better than a bare DLQ because the record includes the **response/error payload and the request context**, not just the original event.
- **`RetryAttempts`** for async invokes defaults to **2** (so up to 3 total attempts) with an event age limit — worth knowing before you conclude "Lambda silently dropped my event".
- **Reserved concurrency** caps *and* guarantees a function's share; **provisioned concurrency** pre-initialises environments to remove cold starts. **SnapStart** is the cold-start fix for **Java only** — for .NET the equivalent levers are **Native AOT**, trimming, and provisioned concurrency (see the .NET considerations above).

**Interview-ready lifecycle answer:** "Each Lambda invocation runs in a Firecracker micro-VM. On cold start AWS provisions the VM, loads the runtime and executes global/static initializers before the handler runs. After the handler returns, AWS may freeze and reuse that environment for a subsequent invocation (warm start) — this reuse is an optimization, not a guarantee. Idle or replaced environments are torn down with no shutdown hook, so any unflushed state is lost."

---

### Lambda Concurrency Model

**Reserved vs Provisioned Concurrency**

| | Reserved Concurrency | Provisioned Concurrency |
|---|---|---|
| Purpose | Guarantee + cap capacity for one function | Eliminate cold starts |
| Mechanism | Carves out part of account/region concurrency pool | Pre-initializes N warm environments |
| Cost | None extra | Billed hourly regardless of invocation |
| Effect above limit | Throttled | Falls back to normal (cold) scaling |

One-liner: **Reserved = guarantee capacity. Provisioned = eliminate cold starts.**

**Burst scaling rules**
- Default regional concurrency limit: 1,000 concurrent executions (increasable).
- Burst behavior: first ~1,000 concurrent executions scale instantly; beyond that, +500 new environments/minute until the account/region limit is hit.
- Concurrency limit = **how far** you can scale; burst rate = **how fast**.

**Multi-region concurrency**
- Each region has an independent concurrency pool and independent burst behavior.
- Active-active: split traffic and provision reserved/provisioned concurrency separately per region.
- Active-passive (DR): pre-raise the concurrency limit in the DR region *before* failover — a cold DR region with default limits will throttle under failover load.

**Sizing formula**

```
Required Concurrency ≈ Peak RPS × Avg Duration (seconds) × Safety Factor (1.3–2.0)
```

Example: 500 msgs/sec × 1.2s duration ≈ 600 concurrency (before safety factor).

- Latency-critical APIs: Reserved ≈ required concurrency; Provisioned ≈ p95 load.
- SQS-driven async Lambdas: you can *cap* concurrency below theoretical peak — SQS absorbs the backlog as back-pressure, protecting downstream DBs.

**Interview traps and correct answers (condensed)**
| Trap question | Correct senior answer |
|---|---|
| Why did Lambda run faster the 2nd time? | Warm start — execution environment reused; not caching of "logic". |
| Why did it time out despite fast code? | Timeout is wall-clock, includes network waits (DB, downstream API), not CPU-only. |
| Why did SQS message get reprocessed? | Message is deleted only after successful execution; failures make it visible again after the visibility timeout. |
| Lambda crashed after a DB write — rollback? | No transaction awareness. Partial writes persist; idempotency required. |
| Can Lambda guarantee exactly-once? | No — at-least-once only. Idempotency is mandatory, not optional. |
| Why is Lambda slower in a VPC? | ENI attachment adds cold-start latency (mitigated significantly since the 2019 Hyperplane ENI improvements, but still non-zero, especially for infrequently-invoked functions). |
| Should Lambda hold business logic? | No — "fat Lambda" anti-pattern; orchestrate/validate/delegate to testable services/libraries. |
| Can Lambda run forever? | No — 15-minute hard cap; use Step Functions/ECS/Batch for longer work. |

**ENI vs VPC Endpoint (mental model)**
- **ENI**: network interface Lambda attaches when placed inside a VPC — required to reach private resources (RDS, internal ALB). Adds cold-start overhead.
- **VPC Endpoint** (Gateway for S3/DynamoDB, Interface/PrivateLink for most others): lets a VPC-bound Lambda reach AWS services *without* NAT/internet — lower latency, lower cost, no public exposure.
- Rule: use ENI only when you must reach private VPC resources; use VPC Endpoints to avoid NAT costs/latency once you're in a VPC anyway.

---

### Lambda vs ECS vs EC2 vs Fargate

```
                      What is the workload shape?
                                  |
      +---------------------------+---------------------------+
      |                           |                           |
 event-driven,            long-running, steady,        full OS control,
 spiky, short-lived       container-based              legacy, GPU, stateful
      |                           |                           |
      v                           v                           |
   LAMBDA              Need OS / kernel access?                |
                                  |                           |
                      +-----------+-----------+                |
                     yes                      no               |
                      |                       |                |
                      v                       v                v
             EC2 + self-managed        ECS / EKS               EC2
             containers or ASG         on FARGATE
```

| Dimension | Lambda | ECS/EKS (Fargate) | EC2 |
|---|---|---|---|
| Server mgmt | None | None (Fargate) | You |
| Max run time | 15 min | Unbounded | Unbounded |
| Cold start | Yes (ms–s) | Minimal (containers stay up) | N/A once running |
| Scaling | Automatic, per-request | Service auto-scaling policies | ASG, manual or policy-based |
| Pricing | Per invocation + duration | Per vCPU/GB-hour while running | Per instance-hour regardless of load |
| Best for | Spiky/event-driven, glue code | Predictable microservices, long tasks | Legacy, stateful, custom kernel/GPU |
| Control over runtime | Minimal | Container-level | Full |

**Key senior talking points**
- Lambda "optimizes for speed, scale, minimal ops"; ECS/EC2 "optimize for control, predictability, long-running work." The right choice is workload-shape-driven, not preference-driven.
- Both Lambda and ECS/Fargate can be triggered by/consume from SQS.
- Lambda scales in seconds; ECS/Fargate task scaling takes longer (container pull, boot); EC2 ASG scaling is the slowest (OS boot, storage attach, service start).
- Cost inversion: Lambda cheaper at low/spiky traffic; ECS/EC2 cheaper at steady high traffic due to per-request/duration billing vs flat capacity billing.
- IAM model differs: Lambda uses an *execution role*; ECS tasks use a *task role* (and a separate *task execution role* for pulling images/writing logs) — a common interview trap is conflating the two ECS roles.

---

### Serverless & the S3 → Lambda Trigger Pattern

**What "serverless" means:** no servers to provision or patch, automatic scaling from zero, **pay only for what you use**, and high availability built in. The AWS serverless set worth listing: **Lambda**, **Fargate**, **S3**, **DynamoDB**, **SQS/SNS/EventBridge**, **API Gateway**, **Step Functions**, and **Aurora Serverless v2**.

**The canonical S3-triggers-Lambda flow** (thumbnail generation, virus scanning, CSV ingestion, document processing):
1. Configure an **S3 event notification** on `s3:ObjectCreated:*`, optionally filtered by prefix and suffix (`uploads/`, `.csv`).
2. S3 invokes the Lambda with an event containing the **bucket name and object key** — not the object itself, so the function fetches it with `GetObject`.
3. The function needs a **resource-based policy** allowing `s3.amazonaws.com` to invoke it (the console adds this automatically), and its **execution role** needs `s3:GetObject` on the source and `s3:PutObject` on the destination.

```json
{
  "Effect": "Allow",
  "Principal": { "Service": "s3.amazonaws.com" },
  "Action": "lambda:InvokeFunction",
  "Resource": "arn:aws:lambda:us-east-1:123456789012:function:process-upload",
  "Condition": {
    "StringEquals": { "AWS:SourceAccount": "123456789012" },
    "ArnLike":      { "AWS:SourceArn": "arn:aws:s3:::my-upload-bucket" }
  }
}
```
`SourceAccount`/`SourceArn` are the **confused-deputy** guard for service principals — see [IAM Roles](#iam-roles-policies-assumerole).

**Two gotchas that are practically guaranteed to be asked:**
- **❗ Infinite recursion.** If the function writes its output **back into the same bucket** under a path the trigger still matches, each write fires the function again — an unbounded invocation loop with a matching bill. Fix by writing to a **different bucket**, or by using a prefix filter that cannot match the output (`uploads/` in, `processed/` out). AWS now has recursive-invocation *detection* that halts the loop, but the architecture fix is yours.
- **At-least-once delivery.** S3 event notifications can be delivered **more than once** and occasionally out of order, so the handler must be **idempotent** — key the work on the object key + ETag/version ID, and make re-processing harmless.

**When to route through EventBridge instead:** enabling **EventBridge notifications** on the bucket gives you content-based filtering, **multiple targets** for one event, retries with a DLQ, and archive/replay — versus S3's one-destination-per-event-type native notifications. For anything beyond a single simple trigger, EventBridge is the better answer (see [EventBridge Deep Dive](#new-content-eventbridge-deep-dive)).

---

## DynamoDB

> **Tier 1 — bulletproof.** The one AWS data store my resume ties to a shipped product ("DynamoDB-backed microservices"), and it's listed under both *Cloud* and *Databases* skills. Revise this section hardest. Caching for DynamoDB (**DAX**) sits with [ElastiCache & Caching Patterns](#elasticache--caching-patterns).

### DynamoDB Deep Dive

**What it is:** fully managed NoSQL key-value/document store with consistent single-digit-millisecond latency at effectively unlimited scale — *if* the key design is correct. Schema-less: each item can have different attributes, which suits fast-evolving microservices but shifts correctness burden onto the application.

**Partitioning**
- The **Partition Key (PK)** hash decides which physical partition stores an item. Each partition has bounded throughput (historically documented around 3,000 RCU / 1,000 WCU per partition — exact numbers are internal and AWS doesn't guarantee them contractually, treat as directional).
- A good PK has **high cardinality** and **even access distribution**. Bad PK choice (low cardinality, time-based, or one "celebrity" key getting disproportionate traffic) → **hot partition** → throttling even when table-level metrics look fine.
- **Adaptive Capacity** shifts unused capacity from cold partitions to hot ones automatically — it smooths real-world unevenness but does **not** fix a fundamentally bad key design.

**Sort Key (SK) — what it actually unlocks**
| Pattern | Example |
|---|---|
| Range queries | `PK=USER#123, SK BETWEEN ORDER#2025-01-01 AND ORDER#2025-01-31` |
| Time-series | `PK=DEVICE#A100, SK=<ISO timestamp>`, query `SK > now-1h` |
| One-to-many | `PK=USER#123, SK=ORDER#<date>` (multiple orders under one user) |
| Hierarchical/single-table | `PK=ORDER#555, SK=META# / ITEM#1 / EVENT#CREATED` — fetch whole aggregate in one Query |
| Sorted views | SK encodes priority/rank for deterministic ordering |

**GSI vs LSI**
| | GSI (Global Secondary Index) | LSI (Local Secondary Index) |
|---|---|---|
| Partition key | Different from base table | Same as base table |
| Sort key | Own, optional | Different from base table |
| Created | Any time | Only at table creation |
| Consistency | Eventual only | Can be strongly consistent |
| Capacity | Own throughput | Shares base table capacity |
| Cost consideration | Every base write may also write to GSI (write amplification) — project only needed attributes | Rarely used due to creation-time constraint |

**Capacity modes**
| | On-Demand | Provisioned (+ Auto Scaling) |
|---|---|---|
| Planning | None | RCU/WCU sizing required |
| Cost | Pay-per-request | Cheaper at steady, predictable volume; supports Reserved Capacity discount |
| Best for | Spiky/unknown traffic (serverless, SQS bursts) | Stable, forecastable load |

**Query vs Scan**
- **Query**: PK-targeted (optionally SK range/filter) — O(matched items), the operation you should always be optimizing for.
- **Scan**: reads the *entire* table/index, filters after the fact — expensive, slow, avoid in hot paths; acceptable only for rare admin/analytics jobs.

**Conditional writes — the underrated superpower**
- Idempotency: `PutItem` with `ConditionExpression: attribute_not_exists(idempotencyKey)`.
- Optimistic locking / state machine transitions: `UpdateItem ... ConditionExpression: status = :pending` before flipping to `PROCESSING` — server-side atomic check, no distributed lock needed.
- This gets you concurrency control **without** paying the 2× cost of full Transactions.

**Transactions (`TransactWriteItems`/`TransactGetItems`)**
- ACID across up to 100 items/tables in one call (4 MB aggregate size limit). Note: this was 25 items until September 2022 — older material and older exam guides still say 25.
- Use only for genuine all-or-nothing business invariants (inventory decrement + order creation, money transfer) — costs ~2× capacity and added latency, so don't reach for it by default.

**TTL:** attribute-driven, best-effort async deletion — can lag by hours in practice (notes explicitly flag up to ~48 hours in some documented cases), not exact-to-the-second. Good for idempotency keys, sessions, dedup records, temporary workflow state. **Do not** rely on TTL for time-sensitive compliance deletion deadlines.

**Streams:** time-ordered change log (insert/update/delete), retained ~24 hours. The backbone pattern for:
- CQRS read-model projections (normalized write table → Streams → Lambda → denormalized read table/GSI)
- Event-driven pipelines (Streams → Lambda → SNS/SQS/EventBridge)
- Change-data-capture / audit trails / search-index sync (OpenSearch, S3)

**Global Tables:** multi-region, multi-active replication; users read/write nearest region; conflict resolution is **last-writer-wins**, replication is asynchronous/eventually consistent — design writes to be idempotent/conflict-tolerant.

**Single-table design**
- Multiple entity types (User, Order, Item, Event) in one table via PK/SK convention — trades upfront modeling effort for far fewer round-trips and no joins.
- Real pattern from the notes: `PK=ORDER#123` with `SK=META# / ITEM#1 / EVENT#<ts>` returns the entire order aggregate (header + items + event history) in a single Query.

**400 KB item limit:** hard cap including attribute names+values. Large objects (images, big docs) → store the blob in S3, keep only a pointer/key in DynamoDB; or vertically partition into multiple items under the same PK.

#### Capacity Maths & Hot-Partition Mitigation

**The unit definitions get asked directly**, and being able to do the arithmetic out loud separates a real answer from a memorised one:
- **1 RCU** = one **strongly consistent** read of up to **4 KB/s**, or **two eventually consistent** reads of 4 KB/s. (So eventually consistent reads are half the cost — a genuine cost lever.)
- **1 WCU** = one write of up to **1 KB/s**. Transactional writes cost **2×**; transactional reads cost 2×.
- Worked example: 100 reads/sec of 10 KB items, eventually consistent → 10 KB rounds up to **3× 4 KB units**, ÷2 for eventual consistency = **1.5 → 2 RCU per read**, × 100 = **~200 RCU**.

**Write sharding — the fix for a hot partition.** If a partition key has low cardinality (`STATUS#PENDING`, or a date like `2026-08-10`), all traffic lands on one partition and you throttle while table-level capacity looks fine. Add a calculated suffix to spread it:
```
PK = ORDER#2026-08-10#3        // shard = hash(orderId) % 10
```
Writes now spread across 10 partitions. The trade-off to state: **reads must now query all 10 shards and merge**, so only shard where the write hot-spot is real. **Adaptive Capacity** helps automatically but is not a substitute for a well-chosen key.

**GSI overloading and sparse indexes** — two single-table techniques worth naming:
- **Overloading**: one GSI whose keys are generic (`GSI1PK`/`GSI1SK`) serves several different access patterns because different entity types write different values into them.
- **Sparse index**: an item only appears in a GSI if it *has* the index's key attribute. So writing `GSI1PK` only on unprocessed orders gives you an index containing *just* the work queue — cheap to scan, because it's tiny by construction.

#### DynamoDB in .NET — The Code You'd Be Asked to Write

**Three SDK layers, and when to use each:**
| Layer | Type | Use when |
|---|---|---|
| **Low-level** | `AmazonDynamoDBClient` + `AttributeValue` dictionaries | Full control — conditions, transactions, and **single-table design** (where one table holds many entity types) |
| Document model | `Table` + `Document` | Schema-flexible access without POCOs |
| **Object persistence** | `DynamoDBContext` + `[DynamoDBTable]` attributes | Simple one-entity-per-table CRUD. ⚠ It assumes one type per table, so it fits **single-table design poorly** |

**❗ Reuse the client.** `AmazonDynamoDBClient` is thread-safe and should be a **singleton** — in Lambda, create it **outside the handler** so it survives warm invocations and reuses connections. Creating one per request is a real and common performance bug.

**Conditional write for idempotency** — the single most important snippet, because it's how you make an at-least-once pipeline safe:
```csharp
try
{
    await client.PutItemAsync(new PutItemRequest {
        TableName = "Orders",
        Item = new Dictionary<string, AttributeValue> {
            ["PK"]     = new("ORDER#" + orderId),
            ["SK"]     = new("META#"),
            ["status"] = new("PENDING")
        },
        ConditionExpression = "attribute_not_exists(PK)"   // only if it isn't there yet
    });
}
catch (ConditionalCheckFailedException)
{
    // Already processed — this is SUCCESS, not an error. Swallow and return.
}
```
The point to make: `ConditionalCheckFailedException` is **the expected path** on a duplicate delivery, not a failure. Treating it as an error is how teams turn safe retries into false alarms.

**Query a GSI with a key condition** (never `Scan` in a hot path):
```csharp
var resp = await client.QueryAsync(new QueryRequest {
    TableName = "Orders",
    IndexName = "GSI1",
    KeyConditionExpression = "GSI1PK = :pk AND begins_with(GSI1SK, :prefix)",
    ExpressionAttributeValues = new() {
        [":pk"]     = new("CUSTOMER#42"),
        [":prefix"] = new("ORDER#2026-08")
    },
    Limit = 25
});
```

**Atomic counter and optimistic locking** — `UpdateItem` mutates server-side, so no read-modify-write race:
```csharp
UpdateExpression    = "SET #v = #v + :inc",
ConditionExpression = "#v < :max"          // atomic increment with a ceiling
```
With the object-persistence model, `[DynamoDBVersion]` gives you optimistic locking automatically — the SDK adds a version condition and throws on conflict.

**Transactions** — all-or-nothing across up to 100 items:
```csharp
await client.TransactWriteItemsAsync(new TransactWriteItemsRequest {
    TransactItems = new() {
        new() { Put    = new Put    { TableName = "Orders",    /* … */ } },
        new() { Update = new Update { TableName = "Inventory", /* decrement stock */ } }
    }
});
```
Costs **2× WCU** and fails wholesale on any condition failure — use it where correctness demands it, not by default.

**Pagination — the loop people get wrong.** `Query`/`Scan` return at most **1 MB** per call, so a missing pagination loop silently truncates results:
```csharp
Dictionary<string, AttributeValue>? start = null;
do {
    var page = await client.QueryAsync(new QueryRequest { /* … */ ExclusiveStartKey = start });
    Process(page.Items);
    start = page.LastEvaluatedKey?.Count > 0 ? page.LastEvaluatedKey : null;
} while (start != null);
```
Note `LastEvaluatedKey` comes back as an **empty dictionary**, not null, when finished — checking `!= null` alone loops forever. (SDK v3's `Paginators.QueryAsync` handles this for you.)

**Batch writes** — `BatchWriteItem` takes up to 25 items and can **partially succeed**: you must re-submit `UnprocessedItems` with backoff. It's a throughput optimisation, not a transaction — no atomicity, no conditions.

**A Streams-triggered Lambda** (the CDC pattern behind read models and audit logs):
```csharp
public async Task Handler(DynamoDBEvent evnt, ILambdaContext ctx)
{
    foreach (var record in evnt.Records)
    {
        if (record.EventName == "INSERT") { /* record.Dynamodb.NewImage */ }
        // idempotent: the same record can be delivered more than once
    }
}
```

**.NET gotchas worth knowing:**
- Use **`decimal`**, not `double`, for money — DynamoDB's Number type is arbitrary-precision and `double` loses it.
- `DynamoDBContext` caches type metadata, so make it long-lived rather than per-request.
- The SDK **already retries throttling** with exponential backoff — don't add your own retry loop on top; tune `MaxErrorRetry` instead.
- Set `ReturnConsumedCapacity` while tuning to see the real RCU/WCU cost of each access pattern.
- **Reserve `Scan` for admin/backfill jobs**, and even then prefer a **sparse GSI** so the "scan" is over a tiny index.

**Limitations to state plainly in an interview (shows maturity, not weakness):**
- No joins, no ad hoc SQL-style queries, no `LIKE`.
- Large scans are expensive; bad PK design silently degrades performance.
- Uniqueness is enforceable only on the primary key, not arbitrary attributes.
- Not suited for complex reporting/analytics — export to Redshift/Athena/OpenSearch via Streams for that.

**Pagination:** results paginate at a 1MB page boundary; API returns `LastEvaluatedKey`, which you resend as `ExclusiveStartKey`. In your public API, base64-encode this as an opaque cursor token.

**Interview-ready 2-line summary:** "DynamoDB is a fully managed, low-latency, horizontally scalable NoSQL database. The real power comes from good key design, GSIs, conditional writes, and Streams to drive event-driven microservice workflows."

### DynamoDB Trick Questions

| Question | Answer |
|---|---|
| Is DynamoDB relational? | No — NoSQL key-value/document, no joins/FKs. |
| Is Scan faster than Query? | No — Scan reads the whole table; Query is index-optimized. |
| Can a PK be duplicated? | Yes, if SKs differ (that's the point of composite keys). |
| Strongly consistent by default? | No — eventually consistent reads by default; strong consistency must be explicitly requested (and GSIs can **never** be strongly consistent). |
| Can a GSI be added after table creation? | Yes. LSI cannot — LSIs must be defined at table creation. |
| Max item size? | 400 KB. |
| Do transactions cost more? | Yes — roughly 2× the capacity of equivalent non-transactional writes/reads. |
| Does TTL delete instantly? | No — best-effort, can take hours. |

**Senior-level summary (memorize):** "DynamoDB trades query flexibility for massive, predictable scalability. Efficient usage depends on correct partition key design, denormalized access-pattern-first modeling, and avoiding scans, hot partitions, and unnecessary indexes."

---

## IAM & Security

### IAM Overview, Root Account & Shared Responsibility

**Why IAM exists:** AWS secures the infrastructure (shared responsibility model); you control *who* can do *what* on *which resource*. IAM does not store data, process requests, or run workloads — it only decides permission.

**Three building blocks:** Identity (User/Role/Service) → Policy (JSON rules) → Authentication (who are you) vs Authorization (what can you do).

**Every single AWS API call answers two questions, in this order:**
1. **Authentication** — *who are you?* Do you hold valid credentials (password, access key, or a temporary STS token)?
2. **Authorization** — *are you allowed to do this?* Does some policy permit this action on this resource?

Both must pass. Fail authentication and you get an `InvalidClientTokenId`/signature error; fail authorization and you get `AccessDenied` — telling those two apart is the first step in any IAM debugging session.

**Facts interviewers use as warm-up questions:**
| Fact | Detail |
|---|---|
| **IAM is global, not regional** | You never pick a region for IAM. One set of users/roles/policies works in every region. (Contrast with almost everything else in this guide.) |
| **IAM is free** | No charge for users, roles, groups, or policies. |
| **Eventually consistent** | IAM data is replicated worldwide, so a brand-new policy or role can take a few seconds to take effect everywhere. This is why CI/CD pipelines that create a role and immediately use it sometimes fail on the first attempt and succeed on retry — a real gotcha worth naming. |
| **Root account** | Created with the AWS account, identified by the sign-up email, and has unlimited power that policies cannot restrict. |

**Root account rules (say all four):** enable MFA on it immediately; never use it for daily work; never share it; **never create access keys for it** (AWS now actively blocks/warns on this). Day-one task on a new account: create an admin identity (ideally an IAM Identity Center user — see [IAM Roles](#iam-roles-policies-assumerole)), verify you can use it, then lock root away.

**Things *only* root can do** (a favourite trick question, because the instinct is "AdministratorAccess can do everything" — it can't):
- Close the AWS account
- Change the account name, root email, or root password
- Change or cancel the AWS Support plan
- Restore an IAM user's permission to manage billing after it's been revoked
- Register as a seller in the Reserved Instance Marketplace
- Enable MFA Delete on an S3 bucket, or delete an S3 bucket policy that denies all principals (the classic "I locked myself out of my own bucket" recovery)
- View certain tax invoices

**Shared Responsibility Model for IAM** — AWS secures the cloud, you secure what you put in it:
| AWS is responsible for | You are responsible for |
|---|---|
| Running IAM as a global, highly available service | Creating and organising users, groups, roles, policies |
| Patching and securing the underlying infrastructure | Applying **least privilege** (see [Least Privilege & Permission Boundaries](#new-content-least-privilege--permission-boundaries-in-practice)) |
| Vulnerability analysis and compliance validation of the service itself | Rotating and protecting credentials; deleting unused ones |
| Providing the tooling (MFA support, Access Analyzer, credential reports, CloudTrail) | **Enabling and actually reviewing** that tooling |
| Tenant isolation — never leaking your data to another customer | Removing leavers; auditing who can do what |

**One-liner to recite:** "AWS guarantees IAM works and is secure *as a service*; how I configure it is entirely on me. AWS will happily let me write `Action: *` on `Resource: *` — that's my problem, not theirs."

### Users, Groups & Permissions

**IAM User** = one physical person (or one legacy application that genuinely cannot use a role). Holds **long-term credentials**: a console password and/or access keys. These do not expire on their own, which is exactly why roles are preferred for anything automated.

**IAM Group** = a container used only to attach permissions to many users at once. The rules interviewers test:
- A group contains **only users** — ❌ **a group cannot contain another group** (no nesting).
- A user can be in **multiple groups**; the effective permissions are the **union** of all of them (plus anything attached directly to the user).
- A user can be in **zero groups** (allowed, not recommended).
- A group is **not an identity**: you cannot log in "as a group", and a group ARN cannot appear as a `Principal` in a policy. Only users, roles, and services can be principals.

```
Account
├── Group: Developers  → [Parteek, Ravi]
├── Group: Operations  → [Ravi, Sara]     ← Ravi is in two groups: permissions add up
└── Group: Audit       → [Sara]
```

**Why groups matter:** attach the policy once to the group instead of 50 times to 50 users. Joiner → add to group. Leaver → remove. Permissions stay consistent and auditable, which is the whole point.

**Permissions are deny-by-default.** A brand-new IAM user with no policy can do *nothing* — it cannot even list S3 buckets or see the EC2 dashboard. Nothing in AWS is implicitly allowed; every permission is something you deliberately granted. This is the correct answer to "what can a new user do out of the box?"

**Policy types by what they attach to**
- **Identity-based**: attached to a User, Group, or Role. No `Principal` field — the identity holding the policy *is* the principal.
- **Resource-based**: attached to the resource itself (S3 bucket policy, SQS queue policy, KMS key policy, Lambda resource policy). **Must** name a `Principal`.

**Same-account vs cross-account (a precise distinction worth getting right):**
- **Same account** — either an identity-based policy *or* a resource-based policy allowing the action is sufficient.
- **Cross-account via a resource-based policy** — the caller keeps their own identity and calls the resource directly; the resource policy must name them, **and** their own account must allow them to make the call. Both sides. Applies to S3, SQS, SNS, KMS, Lambda.
- **Cross-account via AssumeRole** — the caller *becomes* the role and gives up their original permissions for that session. Only the role's permissions apply. See [IAM Roles](#iam-roles-policies-assumerole).

That "resource policy = you stay yourself; AssumeRole = you become someone else" contrast is the crispest way to answer "what are the two ways to do cross-account access?"

### Hands-On: Users & Groups

1. IAM console → **Users** → *Create user* → name it.
2. Tick **Provide user access to the AWS Management Console** only if a human needs the UI.
3. Choose auto-generated or custom password; leave *user must create a new password at next sign-in* ticked.
4. **Permissions** → *Add user to group* → create/select a group (e.g. an `admin` group carrying `AdministratorAccess`). Attaching to the group, not the user, is the habit to demonstrate.
5. Create → **download the `.csv`**. The password and secret are shown **once** and are unrecoverable afterwards — you delete and reissue rather than "look them up".
6. **Account alias** (IAM dashboard → *Account Alias*) turns the ugly sign-in URL `https://123456789012.signin.aws.amazon.com/console` into `https://my-company.signin.aws.amazon.com/console`.

```bash
aws iam create-group  --group-name Developers
aws iam attach-group-policy --group-name Developers \
  --policy-arn arn:aws:iam::aws:policy/ReadOnlyAccess
aws iam create-user   --user-name parteek
aws iam add-user-to-group --user-name parteek --group-name Developers
aws iam list-groups-for-user --user-name parteek     # verify
```

**Point worth making:** IAM users sign in *to a specific account* via that account URL/alias; root signs in by **email address**. That's how you tell from a screenshot which one someone is using.

### Policy Types, Structure & Password Policy

**Three kinds of policy — know which one to recommend:**
| Type | Managed by | Reusable | When to use |
|---|---|---|---|
| **AWS managed** | AWS — auto-updated as AWS adds APIs | Yes | Quick start / broad roles: `ReadOnlyAccess`, `AdministratorAccess`, `AmazonS3ReadOnlyAccess`. Convenient but almost always broader than you need. |
| **Customer managed** | You | Yes — attach to many identities | ✅ **The right answer for real work.** Versioned (up to 5 versions retained, so you can roll back a bad change), reusable, and you can see everywhere it's attached. |
| **Inline** | You | No — embedded in one user/group/role, deleted with it | Rare genuinely one-off grants. Avoid: invisible to audits, impossible to reuse, no version history. |

**Policy anatomy:**
```json
{
  "Version": "2012-10-17",
  "Id": "OrdersBucketReadPolicy",
  "Statement": [
    {
      "Sid": "AllowReadOnOrdersBucket",
      "Effect": "Allow",
      "Action": ["s3:GetObject", "s3:ListBucket"],
      "Resource": [
        "arn:aws:s3:::my-bucket",
        "arn:aws:s3:::my-bucket/*"
      ],
      "Condition": {
        "Bool": { "aws:MultiFactorAuthPresent": "true" }
      }
    }
  ]
}
```

| Field | Meaning | Notes |
|---|---|---|
| `Version` | Policy **language** version | Always `"2012-10-17"`. Not your policy's own version — a common misread. The older `2008-10-17` doesn't support policy variables. |
| `Id` | Optional policy identifier | Rarely used in identity policies |
| `Statement` | One or more permission blocks | Required |
| `Sid` | Statement ID — a label for humans | Optional, but makes CloudTrail/debugging far easier |
| `Effect` | `Allow` or `Deny` | Required |
| `Principal` | *Who* this applies to | **Only** in resource-based policies and trust policies. Never in an identity-based policy. |
| `Action` | API calls as `service:Operation` | Wildcards allowed (`s3:Get*`). `NotAction` = "everything except" — powerful and easy to get wrong. |
| `Resource` | ARNs the actions apply to | Some APIs don't support resource-level permissions and require `"*"` (e.g. many `List*`/`Describe*` calls) — worth knowing so `"*"` isn't automatically treated as sloppiness. |
| `Condition` | Extra rules for *when* the statement applies | Where least privilege actually gets enforced |

**The `bucket` vs `bucket/*` gotcha** (asked constantly): `arn:aws:s3:::my-bucket` is the **bucket** — needed for `s3:ListBucket`. `arn:aws:s3:::my-bucket/*` is the **objects inside** — needed for `s3:GetObject`. Get this wrong and you get "I can download a file if I know its name but `aws s3 ls` returns AccessDenied", or vice-versa.

**ARN format:**
```
arn:partition:service:region:account-id:resource
arn:aws:iam::123456789012:user/parteek              ← IAM is global, so region is empty
arn:aws:s3:::my-bucket/file.txt                     ← S3 names are global, so region+account empty
arn:aws:dynamodb:us-east-1:123456789012:table/Orders
```

**Condition keys worth memorising:**
| Key | Use |
|---|---|
| `aws:MultiFactorAuthPresent` | Require MFA for destructive/sensitive actions |
| `aws:SourceIp` | Restrict to office/VPN CIDR ranges |
| `aws:RequestedRegion` | Pin activity to approved regions |
| `aws:PrincipalOrgID` | Only identities from my AWS Organization |
| `aws:PrincipalTag` / `aws:ResourceTag` | Tag-based access (ABAC, below) |
| `aws:SecureTransport` | Force HTTPS |
| `sts:ExternalId` | Third-party role assumption — see confused deputy in [IAM Roles](#iam-roles-policies-assumerole) |
| `aws:SourceArn` / `aws:SourceAccount` | Narrow a service principal to one specific caller |

**Evaluation order (memorize):** Explicit Deny → Explicit Allow → implicit Default Deny. No matching policy = no access. An explicit Deny **always wins**, even over `AdministratorAccess`.

Full evaluation path once org-level guardrails exist — this is the senior version of the same answer:
```
Request
 ├─ Any explicit Deny anywhere?          → DENY (nothing can override this)
 ├─ SCP (Organizations) permits it?      → no → DENY
 ├─ Permissions boundary permits it?     → no → DENY
 ├─ Session policy permits it?           → no → DENY
 ├─ Identity policy OR resource policy allows it?  → neither → DENY (implicit)
 └─ else                                 → ALLOW
```
Note what this means: **SCPs and boundaries can only take permissions away, never grant them.** See [Least Privilege & Permission Boundaries](#new-content-least-privilege--permission-boundaries-in-practice) for boundary-vs-SCP detail.

**RBAC vs ABAC:**
- **RBAC** (role-based) — permissions per job function; you create a new role/policy per team or project. Simple, but the policy count grows with the org.
- **ABAC** (attribute-based) — permissions driven by **tags**: "you may act on a resource whose `Team` tag matches your own `PrincipalTag/Team`". One policy scales to any number of teams/projects without edits. The standard "how would you scale IAM across 200 microservices?" answer.

#### IAM Policy Structure — Full Explanation

**Read every policy as one sentence.** Four fields carry all the meaning:

> **`Effect`** *(allow or deny)* — **`Action`** *(these API calls)* — **`Resource`** *(on these things)* — **`Condition`** *(but only when this is true)*.

So the anatomy example above reads: *"**Allow** `GetObject` and `ListBucket` **on** my-bucket and everything in it, **but only if** the caller authenticated with MFA."* Once you read policies that way, writing them stops being guesswork.

**Building one up, step by step.** Start with the absolute minimum and add precision:

```json
// ❌ Step 1 — works, but it's admin. Never ship this.
{ "Effect": "Allow", "Action": "*", "Resource": "*" }

// ⚠ Step 2 — scope the service. Still every S3 action on every bucket.
{ "Effect": "Allow", "Action": "s3:*", "Resource": "*" }

// ⚠ Step 3 — scope the resource. Still allows delete.
{ "Effect": "Allow", "Action": "s3:*", "Resource": "arn:aws:s3:::my-bucket/*" }

// ✅ Step 4 — scope the actions too. This is least privilege.
{ "Effect": "Allow",
  "Action": ["s3:GetObject", "s3:PutObject"],
  "Resource": "arn:aws:s3:::my-bucket/uploads/*" }
```
Note step 4 also narrows the **prefix** (`uploads/*`), not just the bucket — resource ARNs can be as specific as a path.

**Wildcards** work in `Action` and `Resource`: `*` matches any number of characters, `?` matches exactly one. `s3:Get*` covers `GetObject`, `GetBucketPolicy`, and everything else beginning "Get" — convenient, but it silently grants future APIs AWS adds with that prefix, which is why explicit action lists are safer for anything sensitive.

**Multiple statements** are evaluated **independently**, and the results are combined. There's no ordering and no fall-through — every statement is checked, any `Deny` wins, otherwise any `Allow` grants. So this is a normal, readable shape:
```json
"Statement": [
  { "Sid": "ReadWholeBucket", "Effect": "Allow",
    "Action": ["s3:GetObject", "s3:ListBucket"],
    "Resource": ["arn:aws:s3:::my-bucket", "arn:aws:s3:::my-bucket/*"] },

  { "Sid": "NeverTouchTheArchive", "Effect": "Deny",
    "Action": "s3:*",
    "Resource": "arn:aws:s3:::my-bucket/archive/*" }
]
```
Broad allow + narrow deny is the standard **guardrail** pattern, and it works precisely because Deny always beats Allow.

**❗ `NotAction` / `NotResource` — the footgun the field table warns about.** `NotAction` means "everything **except** these".
- With **`Deny`** it's genuinely useful — the classic region lock:
```json
{ "Effect": "Deny",
  "NotAction": ["iam:*", "sts:*", "route53:*", "cloudfront:*", "support:*"],
  "Resource": "*",
  "Condition": { "StringNotEquals": { "aws:RequestedRegion": ["us-east-1", "ap-south-1"] } } }
```
*"Deny everything outside my two approved regions, except the global services that have no region."*
- With **`Allow`** it is almost always a mistake. `{"Effect":"Allow","NotAction":"iam:*","Resource":"*"}` grants **every action in AWS except IAM** — effectively administrator, written in a way that looks restrictive. If you see `Allow` + `NotAction` in a review, treat it as a finding.

**Condition operators** — the `Condition` block is `{ Operator: { key: value } }`, and picking the right operator matters:
| Operator | Use |
|---|---|
| `StringEquals` / `StringNotEquals` | Exact match, case-sensitive |
| `StringLike` / `StringNotLike` | Match **with wildcards** — the one to use for `repo:my-org/my-repo:*` style patterns |
| `ArnEquals` / `ArnLike` | ARN comparison (`ArnLike` allows wildcards) |
| `Bool` | `true`/`false` — e.g. `aws:MultiFactorAuthPresent` |
| `IpAddress` / `NotIpAddress` | CIDR ranges |
| `NumericLessThan`, `DateGreaterThan` | Numbers and timestamps (temporary access windows) |
| `Null` | Tests whether a key is **present at all** |

Two modifiers that trip people up:
- **`...IfExists`** (e.g. `StringEqualsIfExists`) — "enforce this **only if** the key is present in the request." Without it, a request that simply omits the key fails the condition.
- **`ForAllValues:` / `ForAnyValue:`** — set operators, needed when a request key holds **multiple** values. `ForAnyValue:` passes if **at least one** value matches. `ForAllValues:` passes if **every** value matches — and here's the trap: **`ForAllValues:` also returns true when the key is absent entirely**, so used alone in an `Allow` it can permit more than you intended. Pair it with a `Null` check when it's load-bearing. This is the operator in the multi-tenant DynamoDB example under [Least Privilege & Permission Boundaries](#new-content-least-privilege--permission-boundaries-in-practice):
```json
"Condition": { "ForAllValues:StringEquals": { "dynamodb:LeadingKeys": ["${aws:PrincipalTag/TenantId}"] } }
```
*"Every partition key this request touches must equal the tenant ID tagged on the caller"* — one policy that safely isolates every tenant.

**Policy variables** are what `Version: 2012-10-17` unlocked (and why the older `2008-10-17` is obsolete). They're substituted at evaluation time:
```json
{ "Effect": "Allow", "Action": "s3:*",
  "Resource": "arn:aws:s3:::company-bucket/home/${aws:username}/*" }
```
One policy, attached to every user, gives each their own private folder. Common variables: `${aws:username}`, `${aws:userid}`, `${aws:PrincipalTag/Team}`.

**Limits worth knowing** (they force you to be deliberate rather than pile everything into one document): a **managed policy is capped at 6,144 characters**; inline policy budgets are 2,048 chars per user, 5,120 per group, 10,240 per role; and an identity can have **10 managed policies attached** by default. Hitting the managed-policy limit is normal at scale and the answer is several focused policies, not one giant one.

**How to write one from scratch:**
1. **List the exact API calls** the workload makes — from the SDK calls in code, or by running it with broad permissions in a sandbox and reading **CloudTrail**.
2. **Write the exact ARNs.** Only use `"*"` where the API genuinely doesn't support resource-level permissions.
3. **Add conditions** for the context that should be required (region, MFA, source IP, tag match).
4. **Test it in the [policy simulator](#iam-security-tools)** before it ships.
5. **Refine after it runs** using **Access Advisor** last-accessed data, or have **Access Analyzer generate the policy from CloudTrail history** — which is the strongest answer to "how do you write a tight policy?"

**The mistakes that come up most:**
| Mistake | What happens |
|---|---|
| Forgetting the bucket ARN alongside `bucket/*` | `GetObject` works, `ListBucket` returns AccessDenied (see the gotcha above) |
| `Allow` + `NotAction` | Accidental administrator |
| `StringEquals` where a wildcard was needed | Condition never matches; everything is denied and the policy looks correct |
| `"Version": "2024-01-01"` | Invalid — it's the **language** version, and only `2012-10-17` should be used |
| `Principal` in an identity-based policy | Rejected — `Principal` belongs only in resource-based and trust policies |
| Expecting an `Allow` to override an explicit `Deny` | It never does, anywhere |
| Editing a customer-managed policy in place with no rollback plan | It's versioned (5 kept) — use `set-default-policy-version` to roll back |

**Password policy** (IAM → Account settings) — what you can enforce:
- Minimum length (AWS allows up to 128 characters)
- Required character types: uppercase, lowercase, number, non-alphanumeric
- Allow or prevent users changing their own password
- **Expiration** after N days (forced rotation), and whether an expired password locks the user out or lets them self-reset
- **Prevent reuse** of the last N passwords

**Why it's asked:** it's the cheapest defence against brute-force and credential-stuffing. But say the honest senior version too — a password policy alone is weak; the real controls are **MFA** (below) and eliminating long-lived human credentials entirely via IAM Identity Center.

### MFA (Multi-Factor Authentication)

**The idea in one line:** a password can be stolen, phished, or leaked; MFA adds *something you physically have*, so a stolen password alone is useless.

Mandatory for the root account, and expected on every human user with console access.

| Device type | What it is |
|---|---|
| **Virtual MFA device** | TOTP app on a phone/laptop — Google Authenticator, Authy, Duo Mobile. One device can hold **multiple tokens** (root plus several users). Free, and the common choice. |
| **FIDO / U2F security key** | Physical USB key such as a YubiKey. One key can serve **multiple root accounts and IAM users**. Phishing-resistant, which TOTP is not. |
| **Hardware TOTP token** | Key fob or display card (Gemalto; SurePassID for AWS GovCloud). Used where phones aren't allowed on the floor. |
| **Passkeys / biometrics** | FIDO2 — Face ID, Touch ID, Windows Hello. |

Details that separate a real answer from a memorised one:
- AWS supports **multiple MFA devices per user (up to 8)** — register a backup so a lost phone isn't a lockout.
- MFA natively protects **console sign-in**. For **CLI/API** you enforce it with a policy condition (`"Bool": {"aws:MultiFactorAuthPresent": "true"}`) and the caller obtains an MFA-backed session via `sts:GetSessionToken` (for IAM users) or `sts:AssumeRole` with `--serial-number`/`--token-code`. "MFA covers the console but the CLI needs a policy condition" is the point most candidates miss.
- Requiring MFA on `sts:AssumeRole` in a **trust policy** is the standard control for cross-account production access.
- Lost device: root recovers via registered email + phone verification; for an IAM user an admin deactivates the old device and assigns a new one.

### Access to AWS: Console, CLI, SDK & Access Keys

Three front doors, one back end — everything ultimately calls the same **AWS REST API** with SigV4-signed requests:
| Route | Credential | Best for |
|---|---|---|
| **Management Console** | Username + password (+ MFA) | Humans, exploration, one-off fixes |
| **AWS CLI** | Access key ID + secret (or temporary role credentials) | Scripting, automation, debugging |
| **AWS SDK** (.NET, Python, JS…) | Same, but normally supplied by a role | Application code |

**Access keys — the risky part of IAM:**
- Access key ID ≈ username, secret access key ≈ password. The **secret is displayed once**; lose it and you delete the key and create a new one.
- **Maximum 2 access keys per user**, and that limit is deliberate: it exists to make zero-downtime rotation possible — create key #2 → roll it out → verify → **delete** key #1. (Deactivating and leaving it is the half-done version.)
- Never commit keys to Git, bake them into an AMI or container image, paste them into CI secrets when OIDC is available, or share them between people/services.
- **Best practice is to not use them at all** where a role will do. For a .NET service on EC2/ECS/Lambda, or a GitHub Actions pipeline, there is no good reason for a static key — see [IAM Roles](#iam-roles-policies-assumerole).

```bash
aws configure                    # writes ~/.aws/credentials + ~/.aws/config
aws configure --profile dev      # named profile
aws sts get-caller-identity      # "who am I?" — the single most useful IAM debug command
aws iam list-access-keys --user-name parteek
aws s3 ls --profile dev
```

**AWS CloudShell** — a browser terminal already authenticated as your console identity, so no keys on your laptop at all. Not available in every region; ~1 GB of home-directory persistence.

**Default credential provider chain (order matters, and this is a real debugging trap):**
1. Explicit CLI/SDK parameters
2. **Environment variables** (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_SESSION_TOKEN`)
3. Shared credentials file (`~/.aws/credentials`) / config file profile
4. Container credentials (ECS task role endpoint)
5. **EC2 instance profile via IMDS**

The instance role is **last**. So a stale `AWS_ACCESS_KEY_ID` left in a shell, systemd unit, or Dockerfile silently shadows the EC2/ECS role, and you get `AccessDenied` for permissions your role clearly has. `aws sts get-caller-identity` immediately reveals it — if it prints a `user/...` ARN where you expected `assumed-role/...`, that's your answer.

### Hands-On: MFA & Access Keys

**Enable virtual MFA:** IAM → Users → *your user* → **Security credentials** → *Assign MFA device* → name it → **Authenticator app** → scan the QR code → enter **two consecutive codes** (AWS uses the pair to sync the time drift) → sign out and back in to confirm.

**Create and use an access key:** Security credentials → *Create access key* → choose the CLI use case → acknowledge the warning → download the `.csv`.
```bash
aws configure
# AWS Access Key ID:     AKIA...
# AWS Secret Access Key: ****
# Default region name:   us-east-1
# Default output format: json
aws sts get-caller-identity      # confirms which identity the key belongs to
```

**Rotation drill worth being able to recite:**
```bash
aws iam create-access-key  --user-name parteek       # key #2 (now at the limit of 2)
# update apps/CI to key #2, deploy, and verify traffic is using it
aws iam update-access-key  --user-name parteek --access-key-id AKIA_OLD --status Inactive
# soak: if anything breaks, flip it back to Active
aws iam delete-access-key  --user-name parteek --access-key-id AKIA_OLD
```

### IAM Roles, Policies, AssumeRole

**What a role actually is:** an identity that carries permissions but has **no permanent credentials** and belongs to nobody. Anything *trusted* can **assume** it and receive **temporary credentials that expire**.

Plain-English analogy: a user is a personal ID card you keep in your wallet forever; a role is a **uniform hanging on a hook** — you put it on, you get its powers, and when the shift ends you take it off. Nothing to leak, nothing to rotate.

**IAM Role vs IAM User**
| | IAM User | IAM Role |
|---|---|---|
| Credentials | Long-lived access keys | Temporary (STS), auto-rotated |
| Best for | Rare — human break-glass access | Services, automation, cross-account, CI/CD |
| Security posture | Higher risk (leak-prone, manual rotation) | Lower risk, auditable via CloudTrail |

**Trust Policy vs Permission Policy — the #1 confusion point**
| | Trust Policy | Permission Policy |
|---|---|---|
| Lives | On the role itself (*Trust relationships* tab) | Attached to the role |
| Answers | *Who* can assume this role? | *What* can the role do once assumed? |
| **`Principal`** | ✅ **Required** | ❌ **Not allowed** |
| **`Resource`** | ❌ Not used — **the role *is* the resource** | ✅ Required |
| **`Action`** | `sts:AssumeRole` (or `sts:AssumeRoleWithWebIdentity` / `...WithSAML` for federated callers; `sts:TagSession` to pass session tags) | Service APIs, e.g. `dynamodb:GetItem` |
| Example principal | `lambda.amazonaws.com`, another account ARN, OIDC provider | *(n/a)* |

Both are **required** and evaluated **separately** — they're two distinct documents attached at different points on the role, not one document, so there is nothing to merge. (Related but different error: putting a `Principal` into an *identity-based* policy is rejected outright as `MalformedPolicyDocument`.)

**Trust policy in short:**
- **Every role has one, mandatorily** — a role cannot exist without a trust policy. It's created for you when you pick a trusted entity in the console, and edited afterwards on the **Trust relationships** tab.
- It is a **resource-based policy** that happens to be attached to a role, which is why it needs a `Principal` — see [Users, Groups & Permissions](#users-groups--permissions).
- **The `Principal`/`Resource` split is the quickest way to identify any policy at a glance:** identity policy = `Resource`, no `Principal`; trust policy = `Principal`, no `Resource`; bucket/queue/key policy = **both**.
- One trust policy can hold **multiple statements/principals** — e.g. trusting both a service and a specific external role.
- Use **`aws:PrincipalOrgID`** to trust *any* account in your AWS Organization without enumerating account IDs:
```json
"Condition": { "StringEquals": { "aws:PrincipalOrgID": "o-abc123xyz" } }
```

**AssumeRole mechanics (via STS):**
1. Caller authenticates.
2. STS checks the target role's trust policy.
3. STS issues temporary credentials (Access Key, Secret Key, Session Token, expiring in 15 min–12 hrs).
4. Caller uses those credentials; the role's permission policy governs what they can actually do.

**Example — Lambda execution role trust policy:**
```json
{
  "Version": "2012-10-17",
  "Statement": [
    { "Effect": "Allow", "Principal": { "Service": "lambda.amazonaws.com" }, "Action": "sts:AssumeRole" }
  ]
}
```

**Cross-account access pattern (the most interview-relevant IAM scenario):**
1. Account B creates a role with a trust policy allowing Account A's root/specific role ARN to assume it.
2. Account B attaches a permission policy scoping what that role can do (e.g., `s3:GetObject` on one bucket).
3. Account A's caller needs its **own** `sts:AssumeRole` permission targeting that specific role ARN — without this, AssumeRole fails even if Account B's trust policy is correct.
4. Caller calls `sts:AssumeRole`, gets temporary credentials, uses them against Account B's resource.

```csharp
var stsClient = new AmazonSecurityTokenServiceClient();
var request = new AssumeRoleRequest
{
    RoleArn = "arn:aws:iam::222222222222:role/CrossAccountReadRole",
    RoleSessionName = "CrossAccountSession"
};
var response = await stsClient.AssumeRoleAsync(request);
var creds = response.Credentials; // use against Account B resources
```

**External ID (vendor/third-party access — prevents "confused deputy" attacks):**
```json
{
  "Effect": "Allow",
  "Principal": { "AWS": "arn:aws:iam::111111111111:root" },
  "Action": "sts:AssumeRole",
  "Condition": { "StringEquals": { "sts:ExternalId": "vendor-unique-id-123" } }
}
```

**GitHub Actions → AWS via OIDC (modern, keyless CI/CD — expect this in senior interviews):**
```json
{
  "Effect": "Allow",
  "Principal": { "Federated": "arn:aws:iam::111111111111:oidc-provider/token.actions.githubusercontent.com" },
  "Action": "sts:AssumeRoleWithWebIdentity",
  "Condition": {
    "StringEquals": { "token.actions.githubusercontent.com:aud": "sts.amazonaws.com" },
    "StringLike": { "token.actions.githubusercontent.com:sub": "repo:my-org/my-repo:*" }
  }
}
```
No static AWS keys stored in GitHub secrets; short-lived, auditable, scoped per repo/branch.

**Cross-account & trust-policy scenario patterns worth having ready:**
1. **AWS service assuming a role** (Lambda, EC2, ECS, CodeBuild) — a service principal in the trust policy.
2. **Cross-account access** — the target account's role trusts the calling account/role ARN; the caller needs `sts:AssumeRole` permission on their own side too.
3. **Cross-account + External ID** — third-party/vendor access, prevents confused-deputy.
4. **Condition-restricted trust** — narrow trust to one specific source service/resource via `aws:SourceArn`/`aws:SourceAccount`.
5. **OIDC federation** — GitHub Actions or another CI system, no long-lived keys.
6. **SAML federation** — corporate AD/Okta/Entra ID users get console/CLI access via `sts:AssumeRoleWithSAML`, or (preferably today) via IAM Identity Center.

**Temporary credentials are three parts, not two:** `AccessKeyId`, `SecretAccessKey`, **and a `SessionToken`** — plus an expiry timestamp. The session token is what makes them temporary; a request signed with the first two but missing the token fails. Knowing this detail is a quick credibility signal, and it explains why you must propagate `AWS_SESSION_TOKEN` alongside the other two env vars.

**STS API reference:**
| API | Use |
|---|---|
| `AssumeRole` | The main one — assume a role in your own or another account |
| `AssumeRoleWithSAML` | Callers arriving from a SAML 2.0 IdP (ADFS, Okta, Entra ID) |
| `AssumeRoleWithWebIdentity` | OIDC/web identity — GitHub Actions, Google/Facebook logins (for mobile apps, AWS recommends Cognito Identity Pools as the wrapper) |
| `GetSessionToken` | MFA-backed temporary credentials for an **IAM user** (not a role) |
| `GetFederationToken` | Temporary credentials for a federated *user* |
| `GetCallerIdentity` | "Who am I?" — requires **no permissions at all**, which is why it always works as a debugging probe |

**Session duration and role chaining:** default 1 hour; the role's *Maximum session duration* setting allows 1–12 hours (`--duration-seconds` cannot exceed it). **Role chaining** — using one assumed role to assume another — is **hard-capped at 1 hour** and cannot be extended, which is a common cause of "our long-running batch job dies exactly 60 minutes in". Also pass a meaningful `--role-session-name`: it appears in CloudTrail, and it's the only way to trace *which human* used a shared role.

**How a role actually reaches your compute** (the part that's easy to hand-wave and gets probed):
| Compute | Delivery mechanism |
|---|---|
| **EC2** | An **instance profile** — a container holding exactly **one** role, and the thing actually attached to the instance. A role cannot be attached to EC2 directly. The console creates the instance profile silently with the same name as the role; with **CLI/CloudFormation/Terraform you must create it yourself**, which is why "the role exists but the instance can't use it" is such a common IaC bug. |
| **Lambda** | Execution role, assumed by `lambda.amazonaws.com` at invoke time |
| **ECS** | **Two different roles** — the **task execution role** (used by the ECS agent to pull the image from ECR and write logs) vs the **task role** (used by *your application code*). Mixing them up is a genuine production bug: your app gets `AccessDenied` on DynamoDB because the permission was added to the execution role. |
| **EKS** | **IRSA** (IAM Roles for Service Accounts) or the newer **EKS Pod Identity** — a Kubernetes service account maps to an IAM role via an OIDC provider, so each pod gets its own least-privilege role instead of sharing the node's role. |

**EC2 instance profile in short:**
- **What it is:** a thin **wrapper around a role**. The role holds the permissions; the profile is the object EC2 can actually attach. Two names for what feels like one thing, which is why it confuses people.
- **One profile holds exactly one role** — but the same role can sit in many profiles.
- **The console hides it.** Pick a role in the EC2 console and it creates the matching profile behind the scenes, so most people never learn it exists — until they write **Terraform/CloudFormation**, where it's a separate resource you must declare and reference.
- **Attach or swap it on a running instance** — it takes effect within a couple of minutes, **no reboot**, because the SDK simply picks up new credentials from IMDS.
- **Failure signature:** the role looks correct in IAM but the instance behaves as if it has no permissions at all → either no instance profile exists, or the profile exists with no role in it.

**IMDS — how the credentials physically arrive on EC2:**
```bash
# IMDSv2 (session-oriented, and what you should require)
TOKEN=$(curl -X PUT "http://169.254.169.254/latest/api/token" \
  -H "X-aws-ec2-metadata-token-ttl-seconds: 21600")
curl -H "X-aws-ec2-metadata-token: $TOKEN" \
  http://169.254.169.254/latest/meta-data/iam/security-credentials/
```
The SDK does this automatically and **refreshes the credentials before they expire** — which is why you never cache or manually manage them. **IMDSv1 vs IMDSv2** is a real security question: v1 answers a plain `GET`, so any SSRF bug in your app (or a misconfigured reverse proxy) can be tricked into fetching the instance's credentials — the root cause of several well-known breaches. **IMDSv2 requires a `PUT` to obtain a token first**, which a simple SSRF cannot do. Always set `HttpTokens: required` (and `HttpPutResponseHopLimit: 1`) in your Terraform/CloudFormation launch template.

**Service-linked roles:** predefined roles *owned by* an AWS service (e.g. `AWSServiceRoleForECS`, `AWSServiceRoleForAutoScaling`). The service manages the trust policy and permissions; you cannot edit them, and they can generally only be deleted once the service no longer needs them. The point: they exist so a service can act in your account without you hand-building its trust relationship.

**`iam:PassRole` — the privilege-escalation control people forget.** `sts:AssumeRole` is "let me *become* this role". `iam:PassRole` is "let me *hand* this role to an AWS service" — needed to launch an EC2 instance with a role, create a Lambda with an execution role, or register an ECS task definition. Why it matters: a user with `lambda:CreateFunction` plus **unrestricted** `iam:PassRole` can create a Lambda with `AdministratorAccess` and run arbitrary code as admin — a full escalation from a seemingly modest permission set. Always scope `PassRole` to specific role ARNs, and add `iam:PassedToService` conditions:
```json
{
  "Effect": "Allow",
  "Action": "iam:PassRole",
  "Resource": "arn:aws:iam::123456789012:role/lambda-prod-order-writer-role",
  "Condition": { "StringEquals": { "iam:PassedToService": "lambda.amazonaws.com" } }
}
```

**IAM Identity Center (formerly AWS SSO) — the modern answer for *human* access.** Users live in a built-in identity store or your corporate IdP (Entra ID/Okta); you assign **permission sets**, which Identity Center materialises as roles in each member account. People get short-lived credentials via `aws sso login`, there is one place to offboard a leaver, and no long-lived keys exist anywhere. So when asked *"should we still be creating IAM users?"* the answer is: **no, not for humans** — Identity Center or federation, with IAM users reserved for legacy apps that genuinely cannot assume a role and one break-glass account.

**Mental model to recite in interviews:** "Trust Policy = who's allowed to enter the building. Permission Policy = which rooms they can access once inside. AssumeRole = the temporary access badge issued at the door."

### Hands-On: IAM Roles

**EC2 → S3 role (the demo that makes roles click):**
1. IAM → **Roles** → *Create role* → **AWS service** → **EC2**. (This writes the trust policy for you.)
2. Attach permissions — `AmazonS3ReadOnlyAccess` for the demo; a scoped customer-managed policy in real life.
3. Name it `ec2-dev-s3-reader-role` → Create.
4. EC2 → select instance → **Actions → Security → Modify IAM role** → select it → Update. **Takes effect immediately, no reboot.**

```bash
# BEFORE attaching the role, on the instance:
aws s3 ls
# → "Unable to locate credentials"

# AFTER attaching:
aws sts get-caller-identity
# → arn:aws:sts::123456789012:assumed-role/ec2-dev-s3-reader-role/i-0abc123
aws s3 ls        # works — and there are no access keys anywhere on the box
```
That before/after is the whole argument for roles over access keys, in two commands.

**Cross-account role:** *Create role* → **AWS account** → *Another AWS account* → enter the trusting account ID → optionally tick **Require MFA** and/or **Require external ID** → attach permissions → hand the role ARN to the other account. They use it via console **Switch role**, or:
```bash
aws sts assume-role \
  --role-arn arn:aws:iam::222222222222:role/CrossAccountReadRole \
  --role-session-name parteek-audit-2026-08 \
  --duration-seconds 3600
# export the three values, then verify you actually became the role:
aws sts get-caller-identity
```
Or, cleaner for day-to-day work, let the CLI do the assumption for you via `~/.aws/config`:
```ini
[profile prod-audit]
role_arn       = arn:aws:iam::222222222222:role/CrossAccountReadRole
source_profile = dev
mfa_serial     = arn:aws:iam::111111111111:mfa/parteek
```
```bash
aws s3 ls --profile prod-audit    # CLI assumes the role and caches/refreshes the session
```

### IAM Security Tools

| Tool | Scope | What it gives you |
|---|---|---|
| **Credential Report** | Whole account (CSV download) | One row per user: password enabled/last used/last changed, **MFA active yes/no**, access key age, key last-used date and service. The fastest way to find users without MFA, keys older than 90 days, and keys that have **never** been used (safe to delete). |
| **Access Advisor** (last-accessed data) | Per user/role/group/policy | Which **services** the identity was granted access to and **when it last used them**. This is the practical route from "AdministratorAccess because it worked" to least privilege: granted 40 services, used 4 in 12 months → remove the other 36. |
| **IAM Access Analyzer** | Account / Organization | Findings for resources shared **outside** your account or org (public buckets, over-broad trust policies, KMS keys); **policy validation** as you write; **unused access findings** (unused roles, keys, permissions); and **generate a least-privilege policy from CloudTrail history** — the single best "how do you write a tight policy?" answer. |
| **Policy Simulator** | Policy testing | Answers "would this principal be allowed to do X on Y?" **without making the call**, and shows which statement decided it. Use it before shipping a policy change, and to prove why an SCP or boundary is the real blocker. |
| **CloudTrail** | Audit trail | Every API call: who, when, source IP, which role session (hence `--role-session-name`). The answer to "how do you find out who deleted the bucket?" and how you alert on unusual `AssumeRole` activity. |
| **AWS Config** | Continuous compliance | Managed rules like `iam-user-mfa-enabled`, `access-keys-rotated`, `iam-policy-no-statements-with-admin-access` — turns a one-off audit into an always-on check with remediation. |

```bash
aws iam generate-credential-report
aws iam get-credential-report --query Content --output text | base64 -d > report.csv
aws iam get-account-authorization-details > iam-snapshot.json     # full policy/role dump for offline review
aws accessanalyzer list-findings --analyzer-arn <arn>
```

**How to frame this in an interview:** don't just name the tools — pair each with the question it answers. "Credential report tells me *what shouldn't exist* (stale keys, missing MFA); Access Advisor tells me *what's over-granted*; Access Analyzer tells me *what's exposed externally* and can generate the tighter policy from real CloudTrail usage; the simulator lets me verify the change before it ships."

### IAM Pitfalls

| Pitfall | Why it happens | Fix |
|---|---|---|
| Confusing trust vs permission policy | "I gave S3 access but AssumeRole still fails" | Both are required; check trust policy `sts:AssumeRole` grant separately from the action permissions |
| Overusing `AdministratorAccess` | "Just to make it work" | Start read-only, add incrementally, scope by action+resource |
| Forgetting explicit Deny always wins | SCPs, permission boundaries, resource policies can silently override an Allow | When debugging: check SCP → permission boundary → resource policy → identity policy, in that order |
| Hardcoding credentials | Convenience | Use roles; let the SDK fetch credentials automatically; OIDC for CI/CD |
| Single-AZ Lambda ENIs in a VPC | Function fails during an AZ outage | Configure the Lambda's VPC config across multiple subnets/AZs — IAM itself is AZ-agnostic, execution is not |
| Reusing one role across many services | Permissions creep, hard to audit, blast radius if compromised | One role per service, clear naming convention |
| Overly broad trust principal (`"Principal": "*"`) | Anyone can assume the role | Restrict to specific account/service/OIDC provider, add `aws:SourceArn`/`aws:SourceAccount` conditions |
| No conditions on policies | Allowing an action without scoping resource/context | Use `aws:SourceArn`, `aws:SourceVpc`, `s3:prefix`, `kms:EncryptionContext` |
| Ignoring permission boundaries | "Why can't my role do X even though its policy allows it?" | Boundary defines the *maximum* possible permission — role policy is a subset of the boundary, common in enterprise AWS orgs |
| Assuming roles never expire | App breaks after a few hours | STS credentials expire (15 min–12 hrs); let the SDK auto-refresh, never cache manually |
| Deep role chaining (A→B→C→D) | Debugging nightmare, shrinking max session duration | Keep chains shallow, prefer direct trust relationships |
| No CloudTrail auditing of AssumeRole | No visibility into who assumed what | Enable CloudTrail, alert on unusual `AssumeRole`/`AssumeRoleWithWebIdentity` activity |
| Forgetting resource-based policy is also required | Identity policy allows it, but SQS/KMS/S3 resource policy doesn't | Some services require **both sides** to allow access (S3 cross-account, SQS, KMS, SNS) |
| Poor role naming (`test-role`, `my-role`) | Confusing audits, risky reuse | Use `service-env-purpose-role`, e.g. `lambda-prod-order-writer-role` |
| "Set and forget" IAM | Permissions creep as services are added/removed over time | Periodic reviews + IAM Access Analyzer |
| Unrestricted `iam:PassRole` | Looks harmless next to `lambda:CreateFunction`, but is a full privilege-escalation path to admin | Scope `PassRole` to specific role ARNs + `iam:PassedToService` condition |
| Leaving **IMDSv1** enabled on EC2 | Default on older AMIs/launch templates; any SSRF bug can then steal the instance role's credentials | Require IMDSv2 (`HttpTokens: required`, hop limit 1) in the launch template/Terraform |
| Stale `AWS_ACCESS_KEY_ID` env vars on an instance | Env vars sit **above** the instance profile in the credential chain, so they silently shadow the role | `aws sts get-caller-identity` — if it shows `user/...` not `assumed-role/...`, unset the env vars |
| Permission added to the ECS **task execution role** instead of the **task role** | Both are "the ECS role" in people's heads | Execution role = ECS agent (ECR pull, logs); task role = your app code's permissions |
| Creating access keys for the **root** user, or using root day-to-day | "It was the account I already had" | MFA root, never create root keys, create an admin identity (ideally Identity Center) on day one |
| Assuming `AdministratorAccess` can do literally everything | It cannot perform the root-only actions (close account, change support plan, S3 MFA-delete) | Know the root-only list — see [IAM Overview](#iam-overview-root-account--shared-responsibility) |

**Golden debugging checklist for "my role has permission but access still fails":** execution role permission → trust policy → resource-based policy (bucket/queue/key policy) → KMS key policy (if encrypted) → SCP/permission boundary.

### [new content] Secrets Manager vs Parameter Store

The original notes reference "Secrets Manager or Parameter Store" for securing secrets in multiple places (Lambda/ECS/CodeBuild sections) but never actually compare them — a direct comparison is a very common senior AWS interview question.

| | Secrets Manager | SSM Parameter Store |
|---|---|---|
| Cost | Per-secret + per-API-call charge | Standard tier free; Advanced tier has a small charge |
| Automatic rotation | Built-in (native RDS/Redshift/DocumentDB integrations, or custom Lambda rotation function) | No native rotation — must build it yourself |
| Versioning | Yes | Yes |
| Encryption | KMS, always encrypted | KMS optional (SecureString) or plaintext (String) |
| Max size | 64 KB | 4 KB (Standard) / 8 KB (Advanced) |
| Cross-account/cross-region replication | Native replication support | Manual/custom |
| Typical .NET use | DB connection strings, API keys needing rotation | App config, feature flags, non-rotating settings |

**Senior-level guidance:** use **Secrets Manager** for anything that needs rotation or is a genuine credential (DB passwords, third-party API keys); use **Parameter Store** for configuration values and secrets that don't need automatic rotation, to control cost at scale (hundreds/thousands of config values). A common cost-optimization talking point: many teams over-use Secrets Manager for pure configuration, paying rotation/API-call overhead for values that never rotate — Parameter Store (SecureString) is the correct, cheaper tool there.

**.NET retrieval example:**
```csharp
var client = new AmazonSecretsManagerClient();
var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = "prod/orders/db" });
var connectionString = response.SecretString;
```

#### Secrets Manager — Pitfalls

The service is easy to adopt and easy to get subtly wrong. These are the failures that actually happen:

**❗ 1. Rotation can take your application down.** Rotation creates a **new version** of the secret and moves the **`AWSCURRENT`** staging label to it; the old version becomes **`AWSPREVIOUS`**. If your app reads the secret **once at startup** and caches it forever, it keeps using the old password — and the moment rotation invalidates it, every connection fails at 3 a.m. with no deploy having happened.

The fix is to **re-fetch on authentication failure**, not to poll: catch the auth error, pull `AWSCURRENT` again, retry once. For managed rotation of RDS credentials, prefer **[RDS Proxy](#rds-proxy)** or **IAM database authentication**, which remove the password from the app entirely. The four-step rotation Lambda (`createSecret` → `setSecret` → `testSecret` → `finishSecret`) also has a **two-user strategy** precisely so there's always one valid credential during the swap — worth naming.

**❗ 2. Don't call `GetSecretValue` on every request.** It's a network call with a **throttling quota**, and you pay per 10,000 calls. A busy API doing this per request will throttle and add latency to every call. Cache it in memory with a TTL — the **AWS Secrets Manager caching library**, or the **Parameters and Secrets Lambda extension** (a local HTTP cache sidecar). This is the most common performance mistake with the service.

**3. Cost surprises at scale.** ~$0.40 per secret per month *plus* API calls. That's trivial for 20 database credentials and material for **2,000 config values** — which is exactly the over-use pattern flagged above. Feature flags and app settings belong in **Parameter Store Standard (free)**.

**4. Both IAM *and* KMS permissions are required** when the secret uses a customer-managed key — `secretsmanager:GetSecretValue` **and** `kms:Decrypt`. Same trap as the [Fargate/S3/CMK example](#worked-example-giving-a-fargate-task-access-to-kms-encrypted-s3-data): the IAM policy looks right and the call still fails.

**5. A private subnet needs a VPC endpoint.** `com.amazonaws.<region>.secretsmanager` is an **interface** endpoint — there's no gateway option. Without it (or a NAT gateway) the call hangs rather than erroring clearly.

**6. Deletion has a mandatory 7–30 day recovery window.** You cannot immediately recreate a secret with the same name, which breaks teardown-and-recreate CI pipelines. `ForceDeleteWithoutRecovery` bypasses it — and removes your safety net.

**7. Injected secrets are resolved once, at start.** ECS `valueFrom` and Lambda environment variables are populated when the **task starts** or the execution environment initialises. Rotating the secret does **not** update a running task — you must redeploy, or read it in code. People assume injection means live updates; it doesn't.

**8. Rotation Lambda needs network access to the database.** If the DB is in private subnets, the rotation function must be VPC-attached with the right security groups — otherwise rotation silently fails and you discover it when the secret is stale.

**9. Cross-region replication is opt-in, and replicas are read-only.** Enable it deliberately for DR; don't assume a multi-region app can write to the local copy.

**10. Secrets in environment variables are visible.** Anything injected as an env var shows up in `docker inspect`, task-definition JSON, and often in crash dumps or logs. Fetching in code and holding it in memory is stronger; ECS `valueFrom` at least keeps the literal out of the task definition, but it still lands in the container's environment.

**The one-line summary:** *"Secrets Manager for credentials that rotate, Parameter Store for configuration — and cache the value with a TTL while re-fetching on auth failure, because the real production incident isn't a leaked secret, it's a rotated one the app never re-read."*

### [new content] Least Privilege & Permission Boundaries in Practice

The notes mention "least privilege" as a principle repeatedly but never show a concrete before/after example — adding one closes that gap.

**Anti-pattern (seen constantly in real .NET/Lambda code):**
```json
{ "Effect": "Allow", "Action": "dynamodb:*", "Resource": "*" }
```

**Least-privilege version:**
```json
{
  "Effect": "Allow",
  "Action": ["dynamodb:GetItem", "dynamodb:PutItem", "dynamodb:UpdateItem"],
  "Resource": "arn:aws:dynamodb:us-east-1:123456789012:table/Orders",
  "Condition": { "ForAllValues:StringEquals": { "dynamodb:LeadingKeys": ["${aws:PrincipalTag/TenantId}"] } }
}
```

**Permission boundaries** are a separate mechanism from SCPs: a boundary is attached to a *role or user* and defines the maximum permissions that identity can ever have, regardless of what its attached policies say — commonly used by platform/security teams to let application teams create their own roles/policies within a safe ceiling (e.g., "you may create any role, but it can never exceed this boundary policy"). This is different from an **SCP** (Service Control Policy), which applies at the AWS Organizations level to entire accounts/OUs, not individual identities.

### IAM Rapid-Fire Q&A

Short-answer drill for the fundamentals. The longer scenario answers live in [Sample Interview Q&A](#sample-interview-qa).

**Q: Is IAM regional or global?**
A: Global. You never select a region for IAM, and the same users/roles/policies apply everywhere. Side effect: it's eventually consistent, so a freshly created role may not be usable for a few seconds.

**Q: User vs Group vs Role — in one sentence each?**
A: A user is one person with long-term credentials; a group is a permission container holding users only (no nesting, not an identity, can't be a `Principal`); a role is permissions with no permanent credentials that a trusted service/account/federated identity assumes temporarily.

**Q: What can a brand-new IAM user do with no policy attached?**
A: Nothing — not even list buckets. IAM is deny-by-default; every permission is one you explicitly granted.

**Q: One policy allows an action and another denies it. What happens?**
A: **Explicit Deny always wins**, and nothing can override it — not `AdministratorAccess`, not a resource policy. If no policy mentions the action at all, it's an implicit deny, so also denied.

**Q: Why is a role better than access keys for an app on EC2?**
A: Nothing secret is stored on the box or in code/Git, credentials are temporary and auto-refreshed by the SDK before expiry, permissions can be changed centrally without redeploying, and there's nothing to rotate or leak. Prove it in two commands: `aws s3 ls` fails with "Unable to locate credentials" before the role, works after.

**Q: What are the two policies on a role, and how do you tell which one is broken?**
A: The **trust policy** (who may assume it — has a `Principal`) and the **permissions policy** (what it can do once assumed). If `sts:AssumeRole` itself fails with "not authorized to perform sts:AssumeRole", it's the trust policy; if you assume successfully but the API call fails, it's the permissions policy.

**Q: How do you give an application on EC2 access to S3?**
A: Create a role trusting `ec2.amazonaws.com`, attach a scoped S3 policy, attach the role to the instance — via the **instance profile**, which is what actually gets attached (a role can't attach to EC2 directly). The SDK then picks credentials up from IMDS automatically.

**Q: What are the two ways to do cross-account access?**
A: (1) **AssumeRole** — the target account's role trusts the caller's account and the caller has `sts:AssumeRole`; the caller *becomes* the role and loses their own permissions for that session. (2) **Resource-based policy** — the bucket/queue/key policy names the outside principal directly; the caller *keeps* their own identity, and both sides must allow it.

**Q: What exactly does STS return, and for how long?**
A: `AccessKeyId`, `SecretAccessKey`, and a **`SessionToken`**, plus an expiry — 15 minutes to 12 hours depending on the role's maximum session duration. **Role chaining is hard-capped at 1 hour** and can't be extended.

**Q: Inline vs managed policy — which do you recommend?**
A: Customer-managed. It's reusable, versioned (5 versions, so you can roll back), and you can see everywhere it's attached. Inline policies are invisible to audits, unreusable, and die with the identity. AWS-managed policies are fine to start with but are almost always broader than you need.

**Q: Permissions boundary vs SCP?**
A: A boundary attaches to a **user or role** and caps its maximum permissions (effective = policy ∩ boundary); an SCP attaches at the **Organizations account/OU** level and limits everyone in the account, including its root user. Neither ever *grants* anything — both can only subtract.

**Q: What's the confused deputy problem and how do you fix it?**
A: A third party holding a role that serves all their customers could be tricked into using their access against *your* account. Fix: they issue you a unique **External ID** and your trust policy requires it via a `sts:ExternalId` condition. For AWS service principals, the equivalent controls are `aws:SourceArn`/`aws:SourceAccount`.

**Q: Why does `iam:PassRole` matter?**
A: It's the permission to *hand a role to a service*, distinct from `sts:AssumeRole` (becoming one). Unrestricted, it turns `lambda:CreateFunction` into full admin — create a function with an admin execution role and run whatever you like. Scope it to specific role ARNs with an `iam:PassedToService` condition.

**Q: Can `AdministratorAccess` do everything in the account?**
A: No. Root-only actions remain — closing the account, changing the root email/support plan, S3 MFA-delete, RI Marketplace registration. It's a favourite trick question.

**Q: How do you enforce MFA for CLI/API calls, not just the console?**
A: MFA natively protects console sign-in only. For CLI/API you add a policy condition `"Bool": {"aws:MultiFactorAuthPresent": "true"}` and the caller obtains an MFA-backed session via `sts:GetSessionToken` (IAM user) or `AssumeRole` with `--serial-number`/`--token-code`. Requiring MFA in the **trust policy** is the standard control for production cross-account access.

**Q: How would you find over-permissioned identities in an account you just inherited?**
A: Credential report for what shouldn't exist (no MFA, stale/never-used keys), **Access Advisor** last-accessed data for services granted but never used, **IAM Access Analyzer** for external exposure and unused-access findings, and its generate-policy-from-CloudTrail feature to rebuild a tight policy from actual usage. Verify each proposed change in the **policy simulator** before shipping it.

**Q: Should you still create IAM users today?**
A: Not for humans — use **IAM Identity Center** (or SAML/OIDC federation) so access is centrally managed, short-lived, and offboarding happens in one place. Keep IAM users only for legacy apps that genuinely cannot assume a role, plus one break-glass account.

**Q (scenario): Our nightly batch job dies almost exactly 60 minutes in. Why?**
A: Role chaining — the job assumes a role from an already-assumed role, which caps the session at 1 hour regardless of the role's max-duration setting. Either assume the target role directly from the base identity (so 1–12 hours is available), or make the job refresh credentials rather than holding one session.

**Q (scenario): The EC2 role clearly allows `s3:GetObject`, but the app still gets AccessDenied. Where do you look?**
A: In order: is the app actually using the role (`aws sts get-caller-identity` — an ARN of `user/...` instead of `assumed-role/...` means stale `AWS_*` env vars are shadowing the instance profile, since env vars rank above IMDS in the credential chain); then the bucket policy; then the KMS key policy if the object is SSE-KMS encrypted; then SCP/permissions boundary. Also confirm an instance profile exists at all — with Terraform/CloudFormation the role can exist without one.

---

## Infrastructure as Code & CI/CD

### AWS CodeCommit

**What it is:** AWS's own managed Git repository service — same Git semantics/CLI you already use (clone, push, pull, branches, PRs via "pull requests"), just hosted and access-controlled through IAM instead of a third-party SaaS account. It's one of several valid **Source** stage providers for CodePipeline, alongside GitHub, Bitbucket, and S3 (via CodeStar Connections for the third-party ones — see the trap-scenario table below).

**Why it comes up in interviews even though GitHub dominates in practice:** knowing CodeCommit exists — and that it's IAM-native (repo access controlled by the same policies/roles as everything else in the account, no separate SaaS permission model to reconcile) — is the actual point being tested, not a claim that you'd choose it over GitHub for a real team. For a shop already standardized on GitHub, there's rarely a reason to migrate; CodeCommit's main edge is avoiding a third-party auth/connection dependency entirely for teams that want everything inside one AWS account boundary.

### AWS CodeBuild

**What it is:** fully managed CI service — compiles code, runs tests, produces build artifacts (JAR/DLL/Docker image/zip) in on-demand, isolated build containers. No Jenkins servers to patch/scale.

**Core concepts**
- **Build Project**: config for source, environment, build steps, artifact destination.
- **Build Environment**: OS + runtime + compute size + privileged mode (needed for Docker-in-Docker builds).
- **buildspec.yml**: the build's script-as-YAML.

```yaml
version: 0.2
phases:
  install:
    commands: [echo Installing dependencies]
  pre_build:
    commands: [echo Pre-build steps]
  build:
    commands: [echo Building application]
  post_build:
    commands: [echo Build completed]
artifacts:
  files: ['**/*']
```

**Phases:** INSTALL (deps/runtime) → PRE_BUILD (ECR login, validation) → BUILD (compile/test) → POST_BUILD (package, push image).

**IAM:** each project has a service role granting source read, CloudWatch log write, S3 artifact upload, ECR push. Never embed AWS credentials in buildspec.

**VPC builds:** needed to reach private RDS/APIs — requires subnets, security groups, and (commonly forgotten) a **NAT Gateway** for internet access; forgetting NAT is the #1 "works standalone, fails in VPC" bug.

**Cost model:** pay for build minutes × compute size only — no idle cost. Optimize via smallest sufficient compute type, failing fast, and dependency caching (S3 or local cache — Maven/npm/NuGet packages).

**Interview-ready summary:** "CodeBuild compiles code, runs tests, and produces artifacts using buildspec files, in isolated on-demand containers, scaling automatically and integrating with CodePipeline without managing build servers."

### AWS CodePipeline

**What it is:** fully managed CD **orchestrator** — it does not compile, test, or deploy itself; it coordinates Source → Build → Test → Deploy stages across other services (CodeBuild, CodeDeploy, ECS, Lambda, CloudFormation).

**Core concepts:** Pipeline (workflow definition) → Stage (Source/Build/Test/Deploy/Approval, run sequentially) → Action (single task within a stage, e.g., "run CodeBuild", "manual approval"; multiple actions within a stage can run in parallel).

**Deploy targets:** ECS/Fargate, EC2 via CodeDeploy, Lambda, Elastic Beanstalk, CloudFormation — with rolling, blue-green, or canary (Lambda) strategies.

**CodeDeploy's Deployment Group:** the logical group of compute targets (a set of EC2 instances/ASG, an ECS service, or a Lambda function+alias) that a given CodeDeploy application actually deploys to — configured as the target of the pipeline's Deploy stage. It's also where the deployment strategy (in-place vs blue/green, rolling percentages, CloudWatch-alarm-triggered automatic rollback) is bound to a concrete set of targets, rather than being an abstract setting on the pipeline itself.

**Manual approval stage:** pauses the pipeline pending human sign-off — standard practice before production deploys/compliance gates.

**Pipeline execution states** — the lifecycle of a single pipeline run, worth having memorized verbatim:
| State | Meaning |
|---|---|
| **Started** | Pipeline execution has started |
| **Succeeded** | All stages completed successfully |
| **Failed** | A stage or action failed |
| **Stopped** | Execution was manually stopped |

**IAM roles involved:** the **pipeline service role** (lets CodePipeline invoke CodeBuild/CodeDeploy/access S3 artifacts) is distinct from **action roles** used by individual integrated services — least privilege on both.

**CodePipeline + ECS flow:** Source → Build (Docker build in CodeBuild) → push to ECR → Deploy stage updates the ECS service to pull the new image tag.

**Cost model:** billed per active pipeline per month, **not** per execution — number of runs is free at the CodePipeline layer (CodeBuild/ECS costs are separate).

**CodePipeline vs CodeDeploy vs Jenkins**
| | CodePipeline | CodeDeploy | Jenkins |
|---|---|---|---|
| Scope | Orchestrates whole release workflow | Only the deployment step | Fully custom, self-hosted |
| Management | AWS-managed | AWS-managed | Self-managed |
| Flexibility | AWS-native integrations only | Deployment-specific | Maximum (any plugin/script) |

**Interview-ready summary:** "CodePipeline automates the build/test/deploy workflow by orchestrating other AWS services through stages and actions, ensuring reliable, repeatable, auditable releases — it doesn't build or deploy anything itself."

### CodePipeline/CodeBuild Trap Scenarios

| Symptom | Root cause |
|---|---|
| Pipeline execution fails immediately after start (before any stage really runs) | Source-stage authentication is broken/expired — a GitHub OAuth token or, more commonly today, an expired/revoked **CodeStar Connections** connection to GitHub/Bitbucket. CodeStar Connections requires a one-time manual "Update pending connection" handshake in the console when first created (and again if the connection is deleted/recreated); pipelines silently fail at the very first stage until someone re-authorizes it |
| Pipeline triggers but CodeBuild doesn't start | Pipeline service role missing permission to start the CodeBuild project |
| CodeBuild works manually but fails inside CodePipeline | Different IAM roles — CodePipeline's invoking role may lack permissions CodeBuild's own role has |
| Deployment uses old code despite pipeline success | Stale cached artifact or deploy stage pointing at wrong S3 path/version |
| "buildspec.yml not found" | File missing, misnamed, or in the wrong directory relative to configured source root |
| Docker build fails in CodeBuild but works locally | Privileged mode not enabled, or missing ECR login step |
| Pipeline stuck "In Progress" | Waiting manual approval, or a long-running build still executing |
| Can't push image to ECR | Service role missing `ecr:PutImage`/`ecr:GetAuthorizationToken` |
| Build fails only inside VPC | No NAT Gateway/VPC endpoint for required AWS service access |
| Build times out despite correct commands | Compute type too small, or build timeout configured too low |
| Multiple triggers per single git push | Both webhook trigger and CodePipeline polling enabled simultaneously |
| Artifacts missing despite build success | Incorrect `artifacts` section paths in buildspec.yml |
| CodeBuild can't read Secrets Manager | Service role missing `secretsmanager:GetSecretValue` |
| ECS runs old image after successful build | ECS service not updated, or image tag is static (`:latest`) instead of a unique tag/digest |
| Works in dev, fails in prod | Cross-account IAM/resource permission misconfiguration |
| Cost spikes suddenly | Frequent triggers, oversized compute type, no dependency caching |

**Senior-level summary (memorize):** "Most CodePipeline/CodeBuild failures are IAM misconfigurations, incorrect artifact handling, missing Docker privileges, VPC networking gaps, or role-boundary confusion — not build command errors. Debugging CI/CD in AWS is primarily a permissions exercise."

### [gaps] CloudFormation vs Terraform/CDKTF

The original notes (and the CI/CD sections above) only mention CloudFormation in passing as one of several possible CodePipeline deploy targets — they never actually compare it to Terraform/CDKTF, which is my actual IaC tool for provisioning Lambda, DynamoDB, EC2, and S3. This is exactly the kind of "contrast your real tools against the AWS-native option" question a senior AWS interview is likely to ask, so it's worth being able to speak to directly and in the first person here.

**Core comparison**

| | CloudFormation | Terraform | CDKTF |
|---|---|---|---|
| Scope | AWS-only | Multi-cloud (AWS, Azure, GCP, and hundreds of other providers) | Multi-cloud — it's Terraform under the hood |
| Language | JSON/YAML templates | HCL (HashiCorp Configuration Language) | General-purpose languages (TypeScript, Python, C#, Java, Go) that synthesize to Terraform's underlying JSON config |
| State management | Managed by AWS — no separate state file to store/lock yourself | You own the state file — local (fine for solo/demo use) or, in any real team setting, a remote backend (S3 bucket + DynamoDB table for state locking is the classic pattern) | Same as Terraform — CDKTF still produces and relies on Terraform state; the backend configuration is unchanged, only the authoring language differs |
| Cost | Free — you pay only for the AWS resources it provisions | Free (open-source core); Terraform Cloud/Enterprise adds paid collaboration features | Free — same licensing as Terraform |
| Drift detection | Native (`Detect Drift` in the console/API) | Via `terraform plan` (diffs real infra against state) | Same as Terraform — `cdktf plan` wraps the same mechanism |
| Rollback on failure | Automatic rollback of the stack on failed deployment (built-in) | No automatic rollback — a failed `apply` can leave a partially-applied state; you manage remediation (re-run apply, or fix and re-plan) | Same as Terraform |
| Vendor lock-in | Total (AWS-only, by definition) | None — same tool works across clouds, portable skill/tooling investment | None — same portability as Terraform, plus the added benefit of using a language your team already knows instead of learning HCL |
| Ecosystem/community modules | AWS-provided sample templates + SAM for serverless | Very large module registry (`registry.terraform.io`), broad community | Growing, but smaller than raw Terraform's HCL module ecosystem since CDKTF is newer |

**The state-file point is worth dwelling on, because it directly touches DynamoDB, which I do have hands-on experience with:** CloudFormation's biggest operational advantage is that AWS manages state for you — there's no file to lose, corrupt, or fight over between team members. Terraform (and therefore CDKTF) pushes that responsibility onto you: the classic production-grade setup is an S3 bucket holding the state file plus a DynamoDB table used purely for **state locking** (preventing two people/pipelines from running `apply` concurrently and corrupting state). That's a real operational cost CloudFormation doesn't have — but it's also exactly the kind of infrastructure I'm already comfortable operating, since it's the same DynamoDB primitives (a simple table, conditional writes for the lock item) I'd use in an application context.

```hcl
# Terraform backend config — S3 for state, DynamoDB for locking
terraform {
  backend "s3" {
    bucket         = "my-org-terraform-state"
    key            = "prod/app/terraform.tfstate"
    region         = "us-east-1"
    dynamodb_table = "terraform-state-lock"
    encrypt        = true
  }
}
```

**Why I'd still reach for Terraform/CDKTF over CloudFormation even on an AWS-only project:** multi-cloud portability isn't always the deciding factor — the module ecosystem, the more expressive planning workflow (`terraform plan` as a genuine dry-run diff, not just a changeset preview), and consistent tooling across any future non-AWS work are the practical reasons. CloudFormation is a perfectly reasonable choice for a team that is AWS-only forever and wants to avoid owning a state backend — that's a legitimate trade-off, not a wrong answer, and I'd say so if asked to defend Terraform as "the only right choice."

**CDKTF specifically — what it changes and what it doesn't:** CDKTF doesn't replace Terraform's engine or state model — it replaces the *authoring* language. Instead of writing HCL, you write TypeScript/Python/C#/Java/Go that calls CDKTF's provider bindings, and `cdktf synth` compiles that into the same JSON Terraform normally consumes, then hands off to the standard Terraform CLI underneath. The appeal for a .NET-background engineer is being able to use a strongly-typed, familiar language (loops, functions, classes, package management) instead of learning HCL's declarative syntax and its more limited expression language.

**Concrete side-by-side — an S3 bucket in HCL vs CDKTF (TypeScript):**

```hcl
# Terraform (HCL)
resource "aws_s3_bucket" "app_data" {
  bucket = "my-app-data-prod"
}

resource "aws_s3_bucket_versioning" "app_data_versioning" {
  bucket = aws_s3_bucket.app_data.id
  versioning_configuration {
    status = "Enabled"
  }
}
```

```typescript
// CDKTF (TypeScript) — equivalent resource
import { S3Bucket } from "@cdktf/provider-aws/lib/s3-bucket";
import { S3BucketVersioningA } from "@cdktf/provider-aws/lib/s3-bucket-versioning";

const appData = new S3Bucket(this, "app_data", {
  bucket: "my-app-data-prod",
});

new S3BucketVersioningA(this, "app_data_versioning", {
  bucket: appData.id,
  versioningConfiguration: {
    status: "Enabled",
  },
});
```

Same declared end-state, same underlying Terraform provider and state file — the only real difference is authoring ergonomics (types, IDE autocomplete, ability to write a loop/function to generate repeated resources instead of HCL's `for_each`/`count`).

**Interview-ready summary:** "CloudFormation is AWS-native and state-free — AWS manages it for you, which is genuinely simpler operationally, but it locks you into AWS only. Terraform trades that simplicity for multi-cloud portability and a much larger module ecosystem, at the cost of owning your own state file — typically an S3 backend with DynamoDB locking, which is infrastructure I'm already comfortable operating. CDKTF is Terraform underneath; it just lets me write that infrastructure in a real programming language instead of HCL, which fits better with my .NET/C# background."

### Terraform/CDKTF in Practice — Depth Questions to Expect

> **Why this section is weighted heavily:** Terraform/CDKTF is a headline skill on my resume and my actual provisioning tool for Lambda, DynamoDB, EC2, and S3. Interviewers push hardest on the things you claim as *primary* tools, so the questions below are the likeliest deep-dive area of any AWS conversation I have — more so than most of the service breadth elsewhere in this guide.

**The core workflow, and the two commands that matter in a review:**
```bash
terraform init         # download providers/modules, configure backend
terraform fmt -check   # formatting gate in CI
terraform validate     # syntax/type check, no AWS calls
terraform plan -out=tf.plan     # the dry-run diff — the artifact a reviewer should read
terraform apply tf.plan         # apply exactly what was reviewed, no re-plan drift
terraform destroy
```
**The point to make about `plan`:** applying a **saved plan file** is what makes a pipeline trustworthy — `terraform apply` without one re-plans at apply time, so what runs may not be what was reviewed. That's the difference between a real CI/CD gate and a rubber stamp.

**❗ `for_each` vs `count` — the highest-value practical Terraform question.** `count` indexes resources by **position** (`aws_instance.web[0]`, `[1]`, `[2]`). Remove the middle item from the list and every subsequent resource shifts index — so Terraform plans to **destroy and recreate** resources that should have been untouched. `for_each` keys them by a **stable string** (`aws_instance.web["api"]`), so removing one affects only that one. **Use `for_each` for anything you'll add to or remove from; reserve `count` for a simple on/off toggle (`count = var.enabled ? 1 : 0`).** Getting this wrong is a real production incident, which is exactly why it's asked.

**Environment separation — workspaces vs directories:**
| Approach | Reality |
|---|---|
| **Workspaces** (`terraform workspace new prod`) | One codebase, one backend key per workspace. Cheap, but environments share the same configuration and it's easy to run `apply` against the wrong one. Fine for dev/test variants |
| **Directory (or repo) per environment** with a shared module | ✅ The pattern for prod. Separate state, separate backend, separate credentials/roles, and environments can legitimately differ (instance sizes, replica counts). Verbose but explicit |

**Modules** are the reuse unit: a module takes typed `variables`, produces `outputs`, and gets **version-pinned** when sourced from a registry or Git tag (`?ref=v1.4.0`). Never source a module from an unpinned branch — someone else's merge becomes your production change. The usual shape is a small set of internal modules (`vpc`, `lambda-function`, `dynamodb-table`) composed per environment.

**❗ The security point that separates a real answer: Terraform state contains secrets in plaintext.** RDS passwords, generated keys, and anything marked `sensitive` are all written to the state file — `sensitive = true` only redacts them from **CLI output**, not from state. So: **encrypt the state bucket (SSE-KMS), block public access, restrict the bucket policy to the pipeline role, enable versioning** (state corruption recovery), and keep real secrets in **Secrets Manager/Parameter Store**, referencing them at runtime rather than passing them through Terraform. This is a question I should expect precisely because I claim the tool.

**Adopting existing infrastructure and fixing state:**
```bash
terraform import aws_s3_bucket.app_data my-existing-bucket   # bring unmanaged resources under management
terraform state list / show / mv / rm                        # refactor or drop state entries
terraform plan -refresh-only                                 # detect drift without proposing changes
```
`terraform state rm` removes a resource from state **without deleting it in AWS** — the escape hatch when something must be managed elsewhere. Import is how you deal with the very common reality of console-created ("ClickOps") resources.

**Provider and version discipline:** pin the `required_version` and provider versions, and commit **`.terraform.lock.hcl`** so every machine and pipeline resolves identical provider builds. Unpinned providers mean a plan that differed yesterday for no reason you can see.

**The CI/CD pattern — and this is where it connects to GitHub Actions and IAM:**
```
PR opened   → fmt, validate, tflint, tfsec/checkov → terraform plan → post plan as a PR comment
PR merged   → terraform apply <saved plan>          (protected environment, manual approval for prod)
```
Authenticate the workflow with **GitHub OIDC assuming an AWS role** — no long-lived access keys in GitHub secrets (the trust policy is in [IAM Roles](#iam-roles-policies-assumerole)). Give the plan job a **read-only** role and the apply job the privileged one, so a malicious PR can't apply anything. Scan with **tfsec/checkov** for misconfiguration (public buckets, unencrypted volumes, `0.0.0.0/0` ingress) before it ever reaches AWS — shifting the [AWS Config](#aws-config) checks left.

**CloudFormation's equivalents, for completeness** (asked as "how would you do this if the shop were CFN-only?"):
- **Nested stacks** — a parent stack composing child stacks; the CFN analogue of modules, and the way around the per-stack resource limit.
- **StackSets** — deploy one template across **many accounts and regions** from the management account in a single operation, with automatic deployment to new accounts in an OU. The multi-account baseline tool; the Terraform equivalent is multiple state files or a provider-per-account loop, which is genuinely more work.
- **Change sets** (preview), **drift detection**, **stack policies** (protect resources from update), **`DeletionPolicy: Retain`/`Snapshot`** (protect data on stack delete), and **SAM** as the serverless-focused transform over CFN.

**Rapid-fire answers to have ready:**
| Question | Answer |
|---|---|
| Someone changed a resource in the console — what happens? | `plan` shows drift; the next `apply` reverts it to code. That reconciliation is the value of IaC — and the reason console changes to managed resources should be blocked by IAM |
| Two pipelines run `apply` at once? | The **DynamoDB lock table** makes the second wait or fail — that's precisely why it exists |
| State file corrupted or lost? | Restore from S3 **object versioning**; failing that, `import` resources back in. This is why versioning on the state bucket is non-optional |
| How do you test IaC? | `validate` + `plan` in CI, `tflint`, policy-as-code (**tfsec/checkov/OPA**), and **Terratest** for real provision-assert-destroy tests against a sandbox account |
| Why not just use CDK (not CDKTF)? | AWS CDK synthesises **CloudFormation** — AWS-only, AWS-managed state. **CDKTF** synthesises **Terraform** — multi-cloud, self-managed state. Same authoring ergonomics, different engine underneath |

---

## S3

> **Tier 1 — bulletproof.** A named resume skill, and also where my Terraform state lives — so bucket policies, encryption, and versioning are doubly relevant (see [Terraform in Practice](#terraformcdktf-in-practice--depth-questions-to-expect)).

### S3 Buckets & Objects

**What S3 is:** infinitely scalable **object** storage — not a filesystem. You get a flat key/value store with an HTTP API, `99.999999999%` (11 nines) durability, and no capacity to provision.

**Bucket rules that come up as questions:**
- The name must be **globally unique across every AWS account in the world** (it's part of a DNS name), 3–63 characters, lowercase letters/numbers/hyphens/dots only, must start and end alphanumerically, cannot look like an IP address, and cannot contain uppercase or underscores.
- A bucket **lives in one region** even though the namespace is global. Data does not leave that region unless you replicate it.
- Buckets are not nested. There is no such thing as a bucket inside a bucket.

**Objects:**
- The **key is the full path** — `invoices/2026/08/inv-001.pdf` is one flat key, not three folders. S3 has **no real directories**; the console renders "folders" from **prefixes** and the `/` delimiter. This matters because it explains prefix-based performance tuning and why `ListObjects` with a delimiter is how you fake a directory listing.
- **Max object size 5 TB.** A **single PUT is capped at 5 GB** — beyond that **multipart upload is mandatory**, and it's recommended above ~100 MB anyway.
- Each object carries system metadata, up to 10 **tags** (useful for lifecycle rules, cost allocation, and ABAC), and a **version ID** if versioning is on.
- **Strong read-after-write consistency** for PUTs, overwrites, and DELETEs, plus consistent LIST — since December 2020. Any interview answer that still says "S3 is eventually consistent for overwrites" is out of date; the correct nuance is that S3 *was* eventually consistent for overwrites/deletes and no longer is.

```bash
aws s3 mb s3://my-unique-bucket-name --region us-east-1
aws s3 cp ./report.pdf s3://my-bucket/invoices/2026/08/report.pdf
aws s3 ls s3://my-bucket/invoices/2026/ --recursive --human-readable --summarize
aws s3 sync ./local-dir s3://my-bucket/prefix/ --delete    # ⚠ --delete removes remote extras
```
`aws s3` is the high-level convenience layer (`cp`, `sync`, `mv`); `aws s3api` exposes the raw per-call API (`put-object`, `put-bucket-policy`) when you need exact control.

### S3 Bucket Policies & Access Control

Four mechanisms can grant S3 access; knowing which to reach for is the question:
| Mechanism | Attaches to | Use for |
|---|---|---|
| **IAM policy** (identity-based) | User/group/role | "What may *this principal* touch?" — the default for your own workloads |
| **Bucket policy** (resource-based) | The bucket | Cross-account access, whole-bucket rules, and **enforcing conditions** (require HTTPS, require encryption). Needs a `Principal` |
| **ACLs** (legacy) | Bucket or individual object | ❌ Avoid. Set **Object Ownership = "Bucket owner enforced"** to disable ACLs entirely — the modern recommendation |
| **Access Points** | A named endpoint on the bucket | Many-team/large-scale access — see [S3 Access Points](#s3-access-points--object-lambda) |

**The two bucket policies you should be able to write from memory** — both are guardrails, not grants:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "DenyUnEncryptedUploads",
      "Effect": "Deny",
      "Principal": "*",
      "Action": "s3:PutObject",
      "Resource": "arn:aws:s3:::my-bucket/*",
      "Condition": { "StringNotEquals": { "s3:x-amz-server-side-encryption": "aws:kms" } }
    },
    {
      "Sid": "DenyInsecureTransport",
      "Effect": "Deny",
      "Principal": "*",
      "Action": "s3:*",
      "Resource": ["arn:aws:s3:::my-bucket", "arn:aws:s3:::my-bucket/*"],
      "Condition": { "Bool": { "aws:SecureTransport": "false" } }
    }
  ]
}
```
Note both `Resource` entries in the second statement — the bucket ARN *and* the `/*` object ARN. Omitting one is the classic mistake (see the ARN gotcha in [Policy Types, Structure & Password Policy](#policy-types-structure--password-policy)).

**Block Public Access (BPA)** — four independent toggles, available at both **account** and **bucket** level, **on by default** since 2023:
1. Block *new* public ACLs
2. Block *all* public ACLs (including existing)
3. Block *new* public bucket policies
4. Block *all* public access granted by any bucket policy

Account-level BPA **overrides** bucket-level settings, which is exactly the point: it's the org-wide guard against the "accidentally public S3 bucket" breach headline. If a bucket policy granting `"Principal": "*"` mysteriously has no effect, BPA is why. And the correct answer to "how do you make sure no bucket in the org is ever public?" is account-level BPA plus an SCP, backed by **IAM Access Analyzer** findings (see [IAM Security Tools](#iam-security-tools)).

### S3 Static Website Hosting

Enable *Static website hosting* on the bucket, set an **index document** (`index.html`) and **error document**, and S3 serves the content over a website endpoint:
```
http://my-bucket.s3-website-us-east-1.amazonaws.com
http://my-bucket.s3-website.us-east-1.amazonaws.com     # region-dependent format
```
You must also allow public reads via a bucket policy **and turn the relevant Block Public Access settings off** — a `403 Forbidden` on a freshly enabled website is almost always BPA or a missing `s3:GetObject` grant.

**❗ The website endpoint is HTTP only — it does not support HTTPS.** So for anything real: put **CloudFront in front**, keep the bucket **fully private**, and use **Origin Access Control (OAC)** so only CloudFront can read it. That gives you TLS (via ACM), a custom domain, caching, and compression. See [CloudFront](#cloudfront-cdn).

Note the two endpoint styles differ: the **REST** endpoint (`my-bucket.s3.us-east-1.amazonaws.com`) supports HTTPS and IAM/SigV4 but no index-document behaviour; the **website** endpoint gives you index/error documents and redirects but is HTTP-only and public-read. That distinction is a favourite question.

### S3 Versioning & Replication

**Versioning** is a bucket-level setting that keeps every version of every object.
- Once enabled it can only be **suspended**, never turned off. Existing objects from before enablement get version ID **`null`**.
- A `DELETE` doesn't remove data — it adds a **delete marker** that hides the object. Delete the marker and the object is back. **Permanent deletion requires specifying the version ID.**
- Protects against accidental overwrite *and* accidental delete, and it's a **prerequisite for replication and for MFA Delete**.
- **Cost warning:** you now pay for every version. Always pair versioning with a lifecycle rule that transitions and eventually expires **noncurrent** versions (see [S3 Lifecycle Rules](#gaps-s3-lifecycle-rules-in-practice--real-patterns--terraform)).

**Replication** copies objects automatically to another bucket:
| | CRR (Cross-Region) | SRR (Same-Region) |
|---|---|---|
| Purpose | DR, lower latency for a distant user base, compliance/data residency | Log aggregation across accounts, prod→test data seeding, live replica for a different team |

Rules and gotchas — this list is where the marks are:
- **Versioning must be enabled on both source and destination**, and S3 needs an **IAM role** to do the copying.
- Replication is **asynchronous**, and **not retroactive** — objects that existed before you enabled the rule are *not* copied. Use **S3 Batch Replication** for the backfill.
- **Delete markers can optionally be replicated; permanent deletes (by version ID) are never replicated** — deliberately, so a malicious or mistaken delete can't propagate to your DR copy.
- **No chaining:** if A replicates to B and B replicates to C, objects from A do **not** reach C.
- Can replicate **across accounts** (add a destination bucket policy), and can change storage class or ownership on the way.
- **Replication Time Control (RTC)** adds a **15-minute** replication SLA plus metrics/notifications — for compliance-driven RPO commitments.
- **Multi-Region Access Points** put one global endpoint in front of buckets in several regions with automatic failover — the "active-active S3" answer.

### S3 Performance, Analytics & Cost Tooling

**Baseline performance:** **3,500 PUT/COPY/POST/DELETE** and **5,500 GET/HEAD requests per second, per prefix** — and there's no limit on the number of prefixes. So the way to scale S3 is to **spread keys across many prefixes** and read/write in parallel. Naming that number, and that it's *per prefix*, is the whole question.

**Upload and download optimisations:**
| Feature | What it does | Use when |
|---|---|---|
| **Multipart upload** | Splits a file into parts uploaded **in parallel**, retryable per part, resumable | **Required >5 GB**, recommended >100 MB. Add a lifecycle rule to abort **incomplete multipart uploads** after N days — otherwise the orphaned parts are billed forever and are invisible in the console |
| **Transfer Acceleration** | Client uploads to the nearest **CloudFront edge**, then travels the AWS backbone to the bucket | Long-distance uploads (users in Asia → bucket in us-east-1). Costs extra; test with the AWS speed comparison tool first |
| **Byte-range fetch** | Parallel `GET`s of different ranges of one object; or fetch just the first N bytes | Speeding up large downloads, or reading only a file header/metadata |
| **S3 Select / Glacier Select** | Runs **SQL** over a single CSV/JSON/Parquet object **server-side**, returning only matching rows | Cuts data transfer and client CPU dramatically. For queries across many objects, use **[Athena](#athena)** instead |

**Analytics and visibility:**
- **S3 Storage Lens** — organisation-wide dashboards of usage and activity across all accounts and buckets, with recommendations. The answer to "how do you understand S3 spend across the org?"
- **Storage Class Analysis** — observes access patterns and recommends when to transition objects to IA. Feeds your lifecycle rules with data instead of guesses.
- **S3 Inventory** — a scheduled CSV/ORC/Parquet report of all objects and their metadata (encryption status, replication status, size, class). It's also the **manifest** you feed to Batch Operations.
- **CloudWatch metrics** — request metrics, bucket size, object counts; plus `4xx`/`5xx` error rates for debugging.
- **Event notifications** — S3 can fire on `ObjectCreated`, `ObjectRemoved`, etc. to **Lambda, SQS, SNS, or EventBridge**. EventBridge is now the richer target (filtering, replay, multiple destinations, archive) and the one to name for anything beyond a single simple trigger.

### S3 Batch Operations

Runs a single operation across **billions of objects** from a manifest (an **S3 Inventory** report or your own CSV), with managed retries, progress tracking, and a completion report.

Supported operations: copy objects, replace/delete **tags**, replace ACLs, **restore from Glacier**, apply **Object Lock** retention or legal hold, and **invoke a Lambda function per object** (which makes it arbitrary).

**Where it's the right answer:** "we have 40 million existing objects that are unencrypted / in the wrong storage class / missing a tag — how do you fix them?" You don't write a script that loops; you generate an Inventory report and run a Batch Operations job. Same for bulk-restoring an archive tier or re-processing a data set through Lambda.

### S3 Requester Pays

Normally the bucket owner pays for storage **and** for requests and data transfer out. With **Requester Pays** enabled, the **requester pays the request and egress costs** while the owner still pays for storage.

- The requester must be an **authenticated AWS principal** — anonymous access is not allowed on a Requester Pays bucket.
- They must explicitly opt in per request by sending **`x-amz-request-payer: requester`** (`--request-payer requester` on the CLI); without it the call fails with `403`.

**Use case:** distributing large shared datasets (scientific data, ML training corpora, public data lakes) without absorbing other people's egress bills. Egress is usually the dominant cost, so this is a real, sizeable shift.

### S3 Best Practices

- **Block Public Access on at the account level**; disable ACLs (`Bucket owner enforced`); grant via bucket policy/IAM, never ACLs.
- **Enforce encryption and HTTPS with a `Deny` bucket policy** rather than trusting every client to do the right thing.
- **Versioning on for anything important**, always paired with a lifecycle rule expiring noncurrent versions — and a rule to **abort incomplete multipart uploads**.
- **Lifecycle by measured access pattern**, using Storage Class Analysis; **Intelligent-Tiering** when the pattern is genuinely unknown.
- **Spread keys across prefixes** for high-throughput workloads; use multipart upload for large objects.
- **CloudFront + OAC in front of a private bucket** for any public content — never a public bucket.
- **Enable access logging or CloudTrail data events** for audit (see [S3 Access Logs](#s3-access-logs-and-the-warning)).
- **Replicate (CRR) what you can't lose**, and remember replication is not a substitute for versioning — it won't save you from a bad overwrite that gets replicated.
- **Watch the silent cost leaks:** old versions, incomplete multipart uploads, forgotten Inventory/log buckets, and cross-region transfer.

### S3 Shared Responsibility Model

S3 is the far end of the spectrum from EC2 — AWS owns almost all of the infrastructure, so nearly every real S3 incident is a **configuration** failure, which is precisely the point to make.

| AWS is responsible for | You are responsible for |
|---|---|
| Infrastructure, and delivering 11 nines of durability by replicating across AZs | **Bucket policies, IAM policies, and Block Public Access settings** |
| Availability of the service and the API | **Versioning, replication, and lifecycle configuration** |
| Providing encryption options (SSE-S3, SSE-KMS, DSSE-KMS) | **Choosing and enforcing** an encryption mode; managing KMS keys |
| Physically destroying decommissioned disks | Classifying your data and deciding what may be stored at all |
| Providing logging/audit capability (access logs, CloudTrail data events) | **Enabling** logging and actually reviewing it |
| Compliance certifications of the underlying platform | Retention/compliance controls: Object Lock, MFA Delete, legal holds |

**One-liner:** "S3 has never lost my data — but S3 will absolutely let me make it public. Durability is AWS's job; access control and retention are mine."

### [new content] S3 Storage Classes & Lifecycle Policies

| Storage Class | Use Case | Availability | Min Storage Duration | Retrieval |
|---|---|---|---|---|
| S3 Standard | Frequently accessed, general purpose | 99.99% | None | Immediate |
| S3 Intelligent-Tiering | Unknown/changing access patterns | 99.9% | None | Immediate (auto-moves between tiers) |
| S3 Standard-IA | Infrequent access, needs millisecond retrieval | 99.9% | 30 days | Immediate |
| S3 One Zone-IA | Infrequent, re-creatable data, single AZ | 99.5% | 30 days | Immediate |
| S3 Glacier Instant Retrieval | Archive needing millisecond access | 99.9% | 90 days | Immediate |
| S3 Glacier Flexible Retrieval | Archive, occasional access | 99.99% | 90 days | Minutes–hours |
| S3 Glacier Deep Archive | Long-term compliance archive | 99.99% | 180 days | ~12 hours |

**Lifecycle policy pattern (typical senior answer):**
```json
{
  "Rules": [
    {
      "ID": "MoveToIAThenGlacier",
      "Status": "Enabled",
      "Filter": { "Prefix": "logs/" },
      "Transitions": [
        { "Days": 30, "StorageClass": "STANDARD_IA" },
        { "Days": 90, "StorageClass": "GLACIER" }
      ],
      "Expiration": { "Days": 365 }
    }
  ]
}
```

**Gotchas an interviewer expects you to know**
- Standard-IA/One Zone-IA charge a *retrieval fee* — cheap storage, expensive if you read often; only use for genuinely infrequent access.
- Minimum storage duration charges apply even if you delete/transition early (e.g., deleting a Glacier object after 10 days still bills for 90 days).
- Intelligent-Tiering has a small monthly monitoring fee per object but removes the guesswork — good default for unpredictable access patterns.
- Versioning + lifecycle rules can interact unexpectedly: old versions also need their own transition/expiration rules, or you silently keep paying for every historical version.
- S3 is strongly consistent for all operations since Dec 2020 (no more "eventual consistency for overwrite PUTS" caveat — a common outdated interview answer to avoid).

### [gaps] S3 Lifecycle Rules in Practice — Real Patterns & Terraform

The existing storage-class table above is the "what" — this section adds the "which rule, for which data, and why," since interviewers commonly ask for a concrete lifecycle policy rather than just the class definitions. It also adds a Terraform example, since that's the candidate's actual provisioning tool for AWS infrastructure.

**Common real-world lifecycle patterns, by data category**

| Data category | Typical rule | Why |
|---|---|---|
| Application/access logs | Standard → Standard-IA at 30 days → Glacier Flexible Retrieval at 90 days → Expire at 365 days | Logs are read frequently in the first month (debugging recent issues), rarely after that, and usually have a compliance/retention window rather than indefinite value |
| Database/system backups | Standard → Glacier Deep Archive at 1–7 days (often immediately) | Backups are "insurance" — you hope to never read them; Deep Archive's ~12-hour retrieval time is acceptable for a disaster-recovery restore, and it's the cheapest tier by far for long-term retention |
| Frequently changing / unpredictable access datasets | Intelligent-Tiering from day 1 | When you genuinely don't know the access pattern (shared data lake, multi-team bucket, user-uploaded content of unknown popularity), Intelligent-Tiering removes the guesswork at the cost of a small per-object monitoring fee — cheaper than guessing wrong and paying retrieval fees on Standard-IA |
| Compliance/audit records (e.g., financial, healthcare retention mandates) | Standard-IA or Glacier Flexible Retrieval → Glacier Deep Archive, with **no** expiration rule (or expiration tied to the legal retention period, often 7+ years) | Regulatory retention periods override normal cost-optimization instincts — get the retention period from legal/compliance, not from "what feels efficient" |
| Temporary/staging data (e.g., ETL intermediate files, upload scratch space) | Standard → Expire at 1–7 days, no transition at all | If data is genuinely disposable within days, transitioning it to a cheaper class isn't worth the complexity — just expire it; transitions have their own minimum-duration billing gotcha (see below) that can backfire on very short-lived data |

**The minimum-duration billing trap, restated concretely:** if you transition an object to Standard-IA (30-day minimum) or Glacier (90-day minimum) and then delete or transition it again before that minimum elapses, you're still billed as if it sat there the full minimum duration. This means aggressive, short-interval lifecycle rules on data that's actually deleted quickly can end up *costing more* than just leaving it on Standard and expiring it directly — always sanity-check the actual object lifetime against the target class's minimum duration before adding a transition rule.

**Terraform example — `aws_s3_bucket_lifecycle_configuration` (the resource the candidate would actually write):**

```hcl
resource "aws_s3_bucket" "logs" {
  bucket = "my-app-logs-prod"
}

resource "aws_s3_bucket_lifecycle_configuration" "logs_lifecycle" {
  bucket = aws_s3_bucket.logs.id

  rule {
    id     = "logs-tiering"
    status = "Enabled"

    filter {
      prefix = "logs/"
    }

    transition {
      days          = 30
      storage_class = "STANDARD_IA"
    }

    transition {
      days          = 90
      storage_class = "GLACIER"
    }

    expiration {
      days = 365
    }
  }

  rule {
    id     = "backups-to-deep-archive"
    status = "Enabled"

    filter {
      prefix = "backups/"
    }

    transition {
      days          = 1
      storage_class = "DEEP_ARCHIVE"
    }

    # No expiration — backups are retained indefinitely (or tie to a compliance-driven expiration instead)
  }

  rule {
    id     = "expire-noncurrent-versions"
    status = "Enabled"

    filter {}

    noncurrent_version_expiration {
      noncurrent_days = 90
    }
  }
}
```

Note the third rule — this directly addresses the versioning gotcha already flagged in the notes above: if versioning is enabled on the bucket, noncurrent (old) versions accumulate and need their own expiration rule, or every historical version silently keeps costing money forever. `noncurrent_version_expiration` is the Terraform-level fix for that specific trap.

**Interview-ready one-liner:** "I'd pick the lifecycle rule from the data's actual read pattern, not its age alone — logs decay in value fast so they tier down and expire; backups go straight to Deep Archive because I never expect to read them until disaster recovery; anything with an unpredictable access pattern goes to Intelligent-Tiering so I'm not guessing. And I always check a class's minimum storage duration against the object's real lifetime before adding a transition, since transitioning something that gets deleted almost immediately after can cost more than not transitioning it at all."

### S3 Security: Encryption & Its Four Types

S3 security is a **defence-in-depth** story, and the strongest way to answer "how do you secure an S3 bucket?" is to walk the layers rather than name one feature: **Block Public Access** → **bucket policy / IAM** → **encryption at rest and in transit** → **versioning + Object Lock** → **logging and monitoring** → **VPC endpoint** so traffic never leaves the AWS network.

| Type | Who holds the key | Audit trail | Use when |
|---|---|---|---|
| **SSE-S3** | AWS, entirely — an AES-256 key S3 manages for you | None per-object | **The default.** Since January 2023 every new object is encrypted with at least SSE-S3 automatically. Free |
| **SSE-KMS** | **Your KMS key** (AWS-managed or customer-managed) | ✅ Every decrypt logged in CloudTrail; access controlled by the **key policy** | You need auditability, key rotation, or an extra permission boundary — a principal needs **both** `s3:GetObject` *and* `kms:Decrypt` |
| **DSSE-KMS** | Your KMS key, applied **twice** (dual-layer) | ✅ | Strict regulatory mandates that specify two independent layers of encryption |
| **SSE-C** | **You** — the key travels in every request header and S3 **never stores it** | Limited | You must retain sole custody of keys. **HTTPS is mandatory**, and if you lose the key the object is unrecoverable |
| **Client-side** | You, before the object ever reaches AWS | N/A to S3 | Zero-trust in the provider; use the AWS Encryption SDK. You own all key management and rotation |

**The SSE-KMS gotcha worth knowing:** every S3 GET/PUT on a KMS-encrypted object makes a `Decrypt`/`GenerateDataKey` call against KMS, which has a **per-region request quota**. A high-throughput application can get **KMS-throttled** (`ThrottlingException`) even though S3 itself is fine. The fix is **S3 Bucket Keys**, which reduce KMS request traffic by up to **99%** by using a bucket-level key to derive per-object keys. Naming that turns a textbook answer into an operational one.

**Encryption in transit** is just HTTPS/TLS — and you don't ask nicely for it, you **enforce it** with the `aws:SecureTransport` deny statement shown in [S3 Bucket Policies](#s3-bucket-policies--access-control). Likewise, to force a *specific* encryption mode, deny `s3:PutObject` when `s3:x-amz-server-side-encryption` isn't what you require.

### S3 CORS

**Cross-Origin Resource Sharing** — a browser refuses to let a page on origin A read a response from origin B unless B explicitly allows it. If a web page loads assets, fonts, or makes `fetch`/XHR calls directly to an S3 bucket on a different origin, **the CORS rules must be configured on the bucket**, not in your application.

```json
[
  {
    "AllowedOrigins": ["https://www.example.com"],
    "AllowedMethods": ["GET", "PUT"],
    "AllowedHeaders": ["*"],
    "ExposeHeaders": ["ETag"],
    "MaxAgeSeconds": 3000
  }
]
```
The browser sends a preflight **`OPTIONS`** request first; S3 answers with `Access-Control-Allow-Origin` and friends. Two things that trip people up: `AllowedOrigins` must match **scheme + host + port exactly** (`https://example.com` ≠ `https://www.example.com`), and `ExposeHeaders` is required for JavaScript to read headers like `ETag` — a common cause of a "working" upload where the SDK can't read the response. Presigned browser uploads (below) almost always need a CORS rule allowing `PUT`.

### S3 MFA Delete

Requires an MFA code before anyone can **permanently delete an object version** or **suspend versioning** on the bucket.

Constraints that make it a distinctive question:
- **Versioning must be enabled** first.
- It can only be enabled/disabled by the **bucket owner — the root account** — and only via the **CLI/API**, not the console.
- It protects the two genuinely destructive operations, not ordinary deletes (which just create delete markers).

It's the textbook control against ransomware-style or malicious deletion of backups; in practice most orgs use **Object Lock** instead, because it doesn't require handing anyone root credentials.

### S3 Access Logs (and the Warning)

**Server access logging** writes a record of every request against a bucket into a **different target bucket**: requester, bucket, time, action, response status, bytes, and error code.

**⚠ The warning this topic exists for: never set the target bucket to the bucket being logged.** Writing a log file is itself a request, which generates another log file, which generates another — an **infinite logging loop** that grows exponentially and produces a very large, very surprising bill. Always log to a **separate, dedicated logs bucket**.

Other properties: delivery is **best-effort and delayed** (minutes to hours), so it's for audit and analysis, not real-time alerting. Logs are unencrypted-by-default plain text and quickly become huge — apply a lifecycle rule.

**Access logs vs CloudTrail data events:** access logs are free-ish, delayed, and best-effort. **CloudTrail data events** cost more but give near-real-time, guaranteed delivery, the **IAM identity** of the caller, and integration with EventBridge/Athena/Security Hub. For "who deleted this object?" or anything security-relevant, the answer is **CloudTrail data events**; server access logs are for traffic analysis and billing attribution.

### S3 Pre-Signed URLs

A time-limited URL that carries **the permissions of whoever generated it**, letting an anonymous holder GET or PUT one specific object without any AWS credentials of their own.

```bash
aws s3 presign s3://my-bucket/private/report.pdf --expires-in 3600
```
```csharp
var request = new GetPreSignedUrlRequest
{
    BucketName = "my-bucket",
    Key        = "private/report.pdf",
    Expires    = DateTime.UtcNow.AddMinutes(15),
    Verb       = HttpVerb.GET          // HttpVerb.PUT for direct browser upload
};
string url = s3Client.GetPreSignedURL(request);
```

Facts that decide the question:
- Expiry: up to **7 days** with SigV4 when signed by an IAM user's long-term key; the CLI default is 1 hour.
- **❗ If you generate the URL with temporary role credentials (Lambda, ECS task, EC2 instance role), the URL stops working when those credentials expire** — often ~1 hour — no matter what expiry you asked for. This is the single most common presigned-URL bug in serverless apps.
- The URL inherits the generator's permissions, so a URL made by an over-privileged role is a real exposure. Anyone with the link can use it — treat it as a bearer token, keep expiry short, and prefer HTTPS-only.

**Where it's the right pattern:** letting an authenticated user download a private file without proxying gigabytes through your API; and **direct browser upload** (presigned `PUT`, or a presigned POST policy for form uploads with size/type conditions) so large files never traverse your Lambda/ECS tier at all. That "don't stream the file through your compute" point is the senior framing.

### S3 Object Lock & Glacier Vault Lock

**Object Lock** implements **WORM** (write once, read many) — an object version cannot be deleted or overwritten for a defined period. Requires **versioning**, and must be enabled at **bucket creation**.

**Two retention modes — the distinction is the exam question:**
| Mode | Who can override retention |
|---|---|
| **Governance** | Users holding the special `s3:BypassGovernanceRetention` permission can shorten or remove the lock. It's a guardrail against accident, with a documented escape hatch |
| **Compliance** | **Nobody** — not an administrator, not the root user. The object cannot be deleted or altered until the retention period expires. Genuinely immutable |

Plus **Legal Hold** — an on/off flag, independent of any retention period, with **no expiry**, controlled by the separate `s3:PutObjectLegalHold` permission. Used for litigation holds where you don't know the end date.

**Glacier Vault Lock** is the equivalent for S3 Glacier *vaults*: you attach a vault lock policy and, once locked, **it can never be changed** — the answer for regulatory regimes like SEC 17a-4.

**Interview framing:** Object Lock in Compliance mode is the strongest answer to "how do you protect backups from ransomware or a malicious insider with admin rights?" — because it's the one control that even root cannot bypass.

### S3 Access Points & Object Lambda

**Access Points** are named network endpoints attached to a bucket, each with **its own DNS name and its own access-point policy**. Instead of one giant, unreadable bucket policy trying to serve twelve teams, each team gets an access point scoped to their prefix and permissions.

- Each access point has its own policy, so bucket policies stay simple and delegation is clean.
- An access point can be **restricted to a VPC**, guaranteeing that data is only reachable from inside your network.
- **Multi-Region Access Points** provide one global endpoint over buckets in several regions with automatic failover.

**S3 Object Lambda Access Points** run a **Lambda function on the `GET` path**, transforming the object as it's retrieved without storing a second copy: redact PII for one audience, convert or resize images, filter rows, or add watermarks. The point to make is that it avoids maintaining multiple derived copies of the same data.

### S3 Security Best Practices & Shared Responsibility

- **Block Public Access at the account level**, and disable ACLs (`Bucket owner enforced`).
- **Enforce** encryption and TLS with `Deny` conditions in the bucket policy; use **SSE-KMS + S3 Bucket Keys** where you need auditability without KMS throttling.
- **Versioning + Object Lock (Compliance mode)** for backups and anything with a retention obligation.
- **Presigned URLs, short-lived**, instead of making objects public — and generate them from a least-privileged role.
- **CloudTrail data events** for security-relevant audit; server access logs for traffic analysis, **always to a separate bucket**.
- **Gateway VPC endpoint for S3** so traffic from private subnets never traverses the internet (and it's free — see [VPC Endpoints](#vpc-endpoints--privatelink)).
- **Access Points** rather than an ever-growing bucket policy once multiple teams are involved.
- **Monitor continuously:** IAM Access Analyzer for external exposure, **Macie** for discovering sensitive data (see [Macie](#macie)), AWS Config rules for public-access and encryption drift.

For the shared-responsibility split, see [S3 Shared Responsibility Model](#s3-shared-responsibility-model) — the short version being that S3 breaches are essentially never durability failures, they're **configuration** failures on the customer's side of the line.

---

## EC2 & Instance Storage

> **Tier 1 — bulletproof.** A named resume skill, provisioned via CDKTF/Terraform. EBS/EFS/AMI material lives here because it's inseparable from running EC2 in practice.

### EC2 Fundamentals

**Core concepts**
- **Instance** — running VM. **AMI** — template (OS + software) used to launch it.
- **Instance families**: `t` (burstable, e.g. t3/t4g), `m` (balanced), `c` (compute-optimized), `r` (memory-optimized), plus GPU (`g`/`p`) and storage-optimized (`i`/`d`) families.
- **Storage**: EBS (persistent, network-attached) vs Instance Store (ephemeral, physically attached, lost on stop/terminate).
- **Networking**: ENI, Security Groups (stateful), subnets.

**Lifecycle:** Launch → Running → Stop/Start → Terminate.
- **Stop**: EBS-backed data persists; instance ID kept; you stop paying compute (but still pay for EBS).
- **Terminate**: instance and (by default) root EBS volume destroyed.

**Pricing models**
| Model | Commitment | Relative cost | Risk |
|---|---|---|---|
| On-Demand | None | Highest | None |
| Reserved Instances | 1–3 yrs | Lower | Locked in |
| Savings Plans | 1–3 yrs $/hr commitment | Lower, more flexible than RI | Locked in $ amount |
| Spot | None | Cheapest | Can be reclaimed with 2-min warning |

**When to choose EC2:** full OS control, custom kernel modules, legacy apps, stateful workloads, GPU/specialized hardware, long-running services with steady utilization.

**Limitations:** you own patching/scaling; idle instances still cost money; more moving parts operationally than serverless/Fargate.

### EC2 Instance Types, User Data & Metadata

**Decoding an instance type name** — `m5dn.2xlarge`:
```
m      5       dn        .2xlarge
│      │       │          └─ size (vCPU/memory scale)
│      │       └─ extra attributes
│      └─ generation (higher = newer, usually better price/performance)
└─ family (workload class)
```
| Letter | Meaning |
|---|---|
| `t` | Burstable — baseline CPU + credits (see the CPU-credit trap below) |
| `m` | General purpose, ~4 GiB RAM per vCPU |
| `c` | Compute optimised, ~2 GiB per vCPU |
| `r` / `x` / `z` | Memory optimised, ~8 GiB+ per vCPU |
| `i` / `d` | Storage optimised — local NVMe/HDD instance store |
| `g` / `p` / `inf` / `trn` | GPU / ML accelerators |
| **attribute `g`** | **AWS Graviton (ARM64)** — typically ~20% cheaper and better price/performance |
| attribute `a` | AMD processors (cheaper than Intel equivalents) |
| attribute `i` | Intel |
| attribute `n` | Network optimised (higher bandwidth) |
| attribute `d` | Local NVMe instance-store disks attached |
| attribute `b` | Block-storage optimised (higher EBS throughput) |

Sizes scale linearly: `large` = 2 vCPU, `xlarge` = 4, `2xlarge` = 8, and memory doubles with them. So `m6g.2xlarge` reads as "general purpose, 6th gen, Graviton, 8 vCPU / 32 GiB".

**The Graviton point is worth making unprompted for .NET:** .NET has supported ARM64 since .NET 6, so a `t4g`/`m7g` instance is usually a straight ~20% saving for a modern .NET web/API workload with a recompile and no code changes. Naming that turns a generic "pick an instance type" answer into a cost-optimisation answer.

#### User Data — Quick Recall

> **In one line:** a startup script you supply at launch, which the instance runs **once, on first boot, as root** (via cloud-init on Linux / EC2Launch on Windows).
>
> - Limit **16 KB** (base64-encoded when passed to the API). Larger bootstraps should download a script from S3 instead.
> - It does **not** re-run on subsequent reboots unless you explicitly configure it to (`cloud-init-per`, or a `#cloud-config` directive).
> - Debug it at `/var/log/cloud-init-output.log` — the first place to look when "my instance came up but nothing is installed".
> - Senior nuance: heavy user-data bootstrapping is slow and fragile at scale. Bake dependencies into a **custom AMI** (see [AMIs](#amis-amazon-machine-images)) or a container image, and keep user data to config only. **"Golden AMI + thin user data"** is the answer interviewers are listening for.

```bash
#!/bin/bash
yum update -y
yum install -y amazon-cloudwatch-agent
systemctl enable --now amazon-cloudwatch-agent
```

#### User Data — The Full Explanation

**The problem it solves.** A newly launched EC2 instance is a **bare operating system** — nothing installed, nothing configured. Something has to run the setup commands. You *could* SSH in and type them, which is fine for one instance and useless for twenty launched automatically by an Auto Scaling Group at 3 a.m. User data is the script the machine runs **on itself** while booting, so nobody has to log in. If an ASG scales out under load, this is the *only* mechanism by which those new instances configure themselves.

**Where it comes from.** In the console it's literally a text box — *Advanced details → User data* — on the launch screen. In a launch template or Terraform it's the `user_data` field. You put shell commands in; that's all it is. The name is AWS's, meaning "data the *user* supplies to the instance"; **"startup script" is a more honest description.**

**Who runs it.** Most Linux AMIs ship with **cloud-init** pre-installed. During boot it fetches your script and executes it **as `root`** — full administrator, so no `sudo` needed. You never install or invoke cloud-init yourself; it looks for user data automatically. Windows AMIs use **EC2Launch** for the same job.

**The example script, line by line:**

| Line | What it does |
|---|---|
| `#!/bin/bash` | The **shebang** — tells cloud-init "run this with bash." **Omit this first line and the script is silently ignored**, which is a genuinely common mistake |
| `yum update -y` | Updates all installed packages. `yum` is the Amazon Linux/RHEL package manager (Ubuntu uses `apt`). **The `-y` auto-answers every prompt** — essential, because there is no human and no keyboard. A command that stopped to ask "Install 47 packages? [y/n]" would hang forever |
| `yum install -y amazon-cloudwatch-agent` | Installs the CloudWatch agent. Why this specifically? Because EC2 does **not** report memory usage or free disk space to CloudWatch by default — those require an agent running inside the guest OS (see [Container Insights](#container-insights-the-cloudwatch-agent--proactive-monitoring)) |
| `systemctl enable --now amazon-cloudwatch-agent` | `systemctl` manages background services. This does **two** things: `enable` = "start automatically on every future boot", `--now` = "and start it right now" |

**Why "runs once" matters more than it sounds.** cloud-init records that it has already run (in `/var/lib/cloud/`), so **on reboot your script does not execute again.** This catches people out constantly:

> *"I put `dotnet MyApp.dll` in user data. It worked. Then the instance rebooted and my app was gone."*

Naturally — the script never ran a second time. **The fix is not to force user data to re-run; it's to install a proper service** so the OS starts your app on every boot. That is precisely what `systemctl enable` does in the example above: the script runs once, and its *effects* persist. Wanting user data to re-run every boot is usually a signal you should have created a systemd service instead.

**The 16 KB limit, and base64.** 16 KB is small — a few hundred lines. base64 is simply an encoding that lets text pass safely through an API; the console and CLI (`--user-data file://script.sh`) handle it for you, so you rarely encode by hand. When your real setup outgrows the limit, keep user data tiny and fetch the rest:

```bash
#!/bin/bash
aws s3 cp s3://my-bucket/bootstrap.sh /tmp/bootstrap.sh
bash /tmp/bootstrap.sh
```
This works with no credentials on the box because the instance's **IAM role** supplies them (see below).

**❗ Why the log matters: a failed user-data script does not fail the instance.** EC2 reports the instance as `running`, status checks pass green — and your software simply isn't there. Nothing surfaces the error. So when someone says *"the instance came up but nothing's installed,"* `/var/log/cloud-init-output.log` (your script's stdout/stderr) is the first place to look, every time.

**Why "golden AMI + thin user data" is the better pattern.** The example script is a fine demo and a poor production practice, for three reasons: it's **slow** (hundreds of MB downloaded on *every* launch, exactly when an ASG is scaling out under load), **not reproducible** (an instance launched today gets different package versions than one launched last month, so your "identical" fleet isn't), and **fragile** (a briefly unreachable package repo leaves the instance booting successfully with software missing, still looking healthy).

So: **bake everything into an AMI once** and keep user data to per-instance configuration only — which environment am I, which cluster do I join. **Full treatment, including the EC2 Image Builder pipeline and the ASG-launch-loop failure mode, is in [The Golden AMI Pattern](#the-golden-ami-pattern--full-explanation).**

#### Instance Metadata (IMDS)

**What it is:** a service every instance can query to learn facts **about itself**, at the special address `http://169.254.169.254/latest/meta-data/`.

That address is **link-local** — it exists only on the machine's own network link, never routes over the internet, and isn't a real server anywhere (the Nitro hypervisor answers it locally). So it works with **no internet access, no VPC routing, and no credentials**.

```bash
curl http://169.254.169.254/latest/meta-data/instance-id
curl http://169.254.169.254/latest/meta-data/placement/availability-zone
curl http://169.254.169.254/latest/meta-data/iam/security-credentials/my-role   # ← temporary IAM credentials
```

**That last one is the important one.** It is how your application obtains its IAM role credentials — the AWS SDK calls this endpoint automatically and refreshes them before they expire. This is the entire mechanism behind "use a role so there are no access keys stored on the instance."

**User data vs instance metadata** — easy to conflate, because user data is *delivered through* the same service (`http://169.254.169.254/latest/user-data`). But they point in opposite directions:

| | **User data** | **Instance metadata** |
|---|---|---|
| Who provides it | **You**, at launch | **AWS**, automatically |
| What it is | Instructions to *configure* the instance | Facts *about* the instance |
| Answers | "Do this on startup" | "Who am I? What are my role's credentials?" |
| Changes later | Only by stopping the instance and editing it | Reflects current reality |

**❗ Always require IMDSv2.** Because IMDSv1 answers a plain `GET`, any **SSRF** bug in your application — where an attacker can make *your server* fetch a URL of their choosing — can be aimed at `169.254.169.254` to retrieve your IAM role credentials, which they then use from their own machine. This is the mechanism behind the Capital One breach. **IMDSv2 requires a `PUT` with a header to obtain a session token first**, which simple SSRF cannot perform. Enforce it with `HttpTokens: required` in the launch template. See [IAM Roles](#iam-roles-policies-assumerole) for the full treatment.

### Security Groups, Their Properties & Classic Ports

**What a security group is:** a stateful virtual firewall attached to an **ENI** (not to an instance — one instance with two ENIs can have different rules per interface).

**Properties to be able to list on demand:**
- **Allow rules only.** You physically cannot write a "deny" rule in a security group. Denying specific IPs is a NACL job.
- **Stateful** — if you allow inbound traffic, the response is automatically allowed back out (and vice versa), regardless of the outbound rules. This is the single biggest difference from NACLs.
- **Default posture:** all inbound **denied**, all outbound **allowed**.
- **Scope:** regional, and locked to one VPC. Can be attached to many instances; one ENI can carry multiple SGs (rules are the **union** — most permissive wins, since there are no denies).
- **Changes apply immediately** — no restart, no re-attach.
- **A SG can reference another SG** instead of a CIDR — the pattern that matters (below).
- The **default** SG of a VPC allows all outbound plus all inbound *from itself*, i.e. instances sharing it can talk freely.

**Reference security groups, not CIDRs** — this is the practical best-practice answer:
```
ALB-SG:  inbound 443 from 0.0.0.0/0
App-SG:  inbound 8080 from ALB-SG      ← not a CIDR
DB-SG:   inbound 5432 from App-SG      ← not a CIDR
```
The app tier is now unreachable except through the ALB, and it keeps working when instance IPs change or the ASG scales — no rule updates ever needed.

**Security Group vs NACL:**
| | Security Group | Network ACL |
|---|---|---|
| Attaches to | ENI / instance | Subnet |
| Rules | **Allow only** | Allow **and** Deny |
| State | **Stateful** (return traffic auto-allowed) | **Stateless** (must allow both directions explicitly) |
| Evaluation | All rules evaluated, union of allows | Rules in **numbered order**, first match wins |
| Typical use | Primary access control | Coarse subnet-level blocking, e.g. blacklisting an IP range |

**The NACL gotcha:** because NACLs are stateless, you must also allow the **ephemeral port range (1024–65535)** for return traffic. "My SG is right but traffic still fails" in a custom-NACL subnet is nearly always this.

**The debugging answer interviewers love:**
- **Connection times out / hangs** → network-layer problem: security group, NACL, route table, or wrong subnet.
- **"Connection refused"** → the network reached the host fine; nothing is listening on that port, i.e. **your application** is down or bound to the wrong interface.

Getting that distinction right in one sentence saves an hour of real debugging and reliably impresses.

**Classic ports to have memorised:**
| Port | Protocol / Service |
|---|---|
| 22 | SSH (also SFTP, and SCP) |
| 21 / 20 | FTP control / FTP data |
| 3389 | RDP (Windows) |
| 80 / 443 | HTTP / HTTPS |
| 53 | DNS |
| 25 / 587 / 465 | SMTP / SMTP+STARTTLS / SMTPS |
| **3306** | MySQL, MariaDB, **Aurora MySQL** |
| **5432** | PostgreSQL, Aurora PostgreSQL, Redshift |
| **1433** | Microsoft SQL Server |
| 1521 | Oracle |
| 6379 / 11211 | Redis / Memcached (ElastiCache) |
| 27017 | MongoDB / DocumentDB |
| 2049 | NFS — **EFS mount target** |

Note 22 vs 21: SSH and SFTP both ride port **22**; plain FTP is 21. That specific pair is a favourite quiz question.

### Public IP vs Private IP vs Elastic IP

| | Private IP | Public IP | Elastic IP |
|---|---|---|---|
| Reachable from | Inside the VPC only | Internet | Internet |
| Assigned by | VPC subnet CIDR | AWS pool, automatically | You, and it's yours until released |
| **Survives stop/start?** | **Yes** | **No — you get a different one** | **Yes** |
| Cost | Free | Charged per hour for public IPv4 (since Feb 2024, whether attached or not) | Charged per hour, **and charged extra when *not* attached** to a running instance |
| Movable | No | No | Yes — remap to another instance/ENI in seconds |

**The classic gotcha:** a public IP is released on stop and a **new one is assigned on start**. Anything hardcoded to it — DNS records, firewall allowlists at a partner, a config file — breaks. This is the #1 "why did my integration stop working after maintenance?" scenario.

**Elastic IP** solves it, but the senior answer is *don't reach for one*. AWS gives you only ~5 per region by default precisely because they're a design smell. Prefer:
- a **load balancer** in front (DNS name stays stable, instances behind it are disposable),
- **Route 53** with an alias record,
- and for outbound-only needs, a **NAT Gateway** (which has its own stable EIP).

Legitimate EIP uses: a partner firewall that can only allowlist a fixed IP, a NAT gateway, or a fast manual failover where you remap the address to a standby instance.

### Placement Groups

How you ask EC2 to influence *where* instances physically land. You choose the strategy; AWS does the placement.

| Strategy | Placement | Trade-off | Use for |
|---|---|---|---|
| **Cluster** | Packed onto the same rack in a **single AZ** | Best network: low latency, high per-flow throughput (10+ Gbps). **Worst blast radius — one rack failure takes everything** | HPC, tightly-coupled compute, big-data jobs that need fast node-to-node chatter |
| **Spread** | Each instance on **distinct underlying hardware**, across AZs | Maximum isolation. Hard limit of **7 running instances per AZ per group** | Small numbers of critical instances that must never fail together (e.g. a 3-node quorum) |
| **Partition** | Instances grouped into **partitions**, each partition on its own set of racks; up to **7 partitions per AZ** | Isolation between partitions, hundreds of instances | HDFS/Hadoop, **Kafka**, Cassandra — anything that is itself rack-aware and replicates across partitions |

**How to answer well:** name the trade-off, not just the definition. "Cluster buys you network performance at the cost of correlated failure; spread buys you isolation but caps you at 7 per AZ; partition is the middle ground for distributed systems that already understand replica placement." Partition groups also expose the partition number to the instance, which is how Kafka/Cassandra place replicas correctly.

### Elastic Network Interfaces (ENIs)

A virtual network card. Each ENI carries: one primary private IPv4 + optional secondary private IPs, one Elastic IP per private IP, a MAC address, one or more security groups, and a source/destination check flag.

Facts that get asked:
- An ENI is **bound to one AZ** and cannot be moved to another AZ (it belongs to a subnet).
- The **primary ENI (`eth0`) cannot be detached** from an instance. Secondary ENIs can be detached and attached to a different instance **in the same AZ** — which is the cheap failover trick: move the ENI (and its IP, MAC, and SGs) to a standby instance and traffic follows it.
- How many ENIs and IPs you get is **capped by instance type** — a small instance can't host many. This matters for container density.
- Disable the **source/destination check** when the instance must forward traffic it didn't originate (a NAT instance, or a software router/firewall appliance).
- **Lambda in a VPC** creates ENIs to reach private resources — the reason VPC-attached Lambdas historically had worse cold starts, and why you spread the Lambda's subnets across AZs (see the [IAM Pitfalls](#iam-pitfalls) table).

### EC2 Hibernate

**What it does:** dumps the instance's **RAM to the encrypted root EBS volume** and shuts down. On start, the RAM is restored and processes resume where they left off — no boot, no application warm-up, same instance ID and private IP.

| | Stop | Hibernate | Terminate |
|---|---|---|---|
| RAM contents | Lost | **Preserved on the root EBS volume** | Lost |
| Boot on restart | Full OS boot | Resumes from memory image | N/A |
| Root EBS volume | Kept | Kept (and holds the RAM dump) | Deleted by default |
| Instance ID / private IP | Kept | Kept | Gone |
| Compute charges | Stopped | Stopped (you still pay for the larger EBS) | Stopped |

**Requirements** (the list that makes hibernate fail in practice): the root volume must be **EBS, encrypted, and large enough to hold the whole RAM image**; the instance must be a supported family/size with **RAM under 150 GiB**; hibernation must be enabled **at launch** (you cannot turn it on later); instance-store root volumes are not supported; and an instance can stay hibernated for a maximum of **60 days**.

**When it's the right answer:** long-initialising applications (a service that spends minutes loading a model or warming a cache), licence-server or dev boxes you want back instantly, and anything where cold-start time is the real cost. Not a substitute for an ASG — it's a single-instance optimisation.

### EC2 Purchasing Options — The Complete Set

The pricing table above covers the four everyone names. These are the ones that separate a complete answer from a partial one:

| Option | What you're buying | Key detail |
|---|---|---|
| **On-Demand** | Pay per second/hour, no commitment | Highest rate, zero risk. Correct default for a new workload with unknown steady state |
| **Reserved Instances** | 1 or 3-year commitment to a specific config | **Standard RI** = biggest discount, locked to instance family; **Convertible RI** = smaller discount, can exchange family/OS/tenancy. Can be sold on the RI Marketplace (root-only action) |
| **Savings Plans** | 1 or 3-year commitment to a **$/hour spend** | **Compute SP** spans EC2 + Fargate + Lambda and any family/region (most flexible); **EC2 Instance SP** is deeper discount but pinned to a family+region |
| **Spot Instances** | Spare capacity at up to ~90% off | Reclaimed with a **2-minute** interruption notice. Needs checkpointing/idempotent work. Spot *Fleet* blends Spot+On-Demand across pools to reduce interruption risk |
| **Dedicated Instances** | Your instances run on hardware **not shared with other AWS customers** | You don't see or control the host. Instances may land on different hosts after stop/start |
| **Dedicated Hosts** | A whole **physical server** reserved for you, with visibility into sockets/cores | The answer for **BYOL socket/core-based licensing** (Windows Server, SQL Server, Oracle) and hard compliance mandates. Most expensive |
| **Capacity Reservations** | Reserved **capacity** in a specific AZ, held whether or not you use it | Pay On-Demand rates — **no discount and no term commitment**. This is about *guaranteed availability*, not price (DR standby, a known launch event). Combine with a Savings Plan to also get the discount |

**Dedicated Instances vs Dedicated Hosts** is the classic confusion pair: both give you single-tenant hardware, but only a **Dedicated Host** exposes the physical server (sockets, cores, host affinity) — which is exactly what per-socket/per-core BYOL licensing requires. If the question mentions bringing your own Windows/SQL Server/Oracle licence, the answer is Dedicated Hosts.

**Capacity Reservation vs Reserved Instance** is the other one: an RI/Savings Plan is a **billing** construct (discount, no capacity guarantee); a Capacity Reservation is a **capacity** construct (guarantee, no discount). Saying that cleanly is a strong signal.

### EC2 Shared Responsibility Model

EC2 sits far to the "your job" end of the spectrum compared with Lambda or S3 — that contrast *is* the answer.

| AWS is responsible for | You are responsible for |
|---|---|
| Physical hosts, data centres, power, physical network | **Guest OS patching and hardening** — the big one, and what people forget |
| The hypervisor and host OS isolation between tenants | Application code, runtime, and dependency patching |
| Replacing failed hardware; the underlying EBS/network infrastructure | **Security group rules**, NACLs, and which subnet the instance sits in |
| Making the AZ/region infrastructure available | **Key pair management** (and not baking private keys into AMIs) |
| Compliance of the underlying infrastructure | **IAM role** attached to the instance, and requiring IMDSv2 |
| Providing encryption features (EBS/EFS encryption, KMS) | **Turning encryption on**, and managing/rotating keys |
| — | Backups: EBS snapshots, AMIs, and testing that they restore |
| — | Architecting for failure across AZs (a single instance is not highly available, no matter what AWS does) |

**One-liner:** "For EC2, AWS is responsible for security *of* the host and everything under the hypervisor; from the guest OS upward — patching, firewall rules, keys, IAM, encryption, backups — it's mine. Lambda moves most of that line onto AWS; EC2 keeps it on me."

---

### [gaps] EC2 Sizing, Pricing Decisions & CPU Credit Gotchas

The existing EC2 section covers the pricing model *names* but not how a senior engineer actually reasons about **sizing** a box or **choosing** between the purchase options for a real workload — and it's missing the T-family CPU credit gotcha, which is one of the most commonly asked "explain a production incident" EC2 questions.

**How to actually pick an instance size (not just a family)**

Sizing is a vCPU-to-memory *ratio* decision, not a "pick the biggest one that fits the budget" decision:
- Start from the **workload shape**: CPU-bound (video encoding, compilation, hashing) → `c`-family (2 GiB RAM per vCPU); memory-bound (in-memory caches, large JVM/.NET heaps, big EF Core result sets) → `r`-family (8 GiB per vCPU); general-purpose web/API tier with no strong lean either way → `m`-family (4 GiB per vCPU); dev/test, low/bursty CPU with idle troughs → `t`-family (also ~4 GiB per vCPU, but *burstable*, see below).
- Benchmark before committing: use CloudWatch `CPUUtilization`, memory (via CloudWatch Agent — memory isn't a default EC2 metric), and network metrics under real/representative load, then size to a target steady-state utilization of roughly 40–60% average — leaving headroom for spikes without being so oversized that you're paying for idle capacity.
- AWS Compute Optimizer will do this analysis for you from actual usage history and recommend a right-sized family/size — worth name-dropping as the "don't guess, measure" answer.
- Vertical (bigger instance) vs horizontal (more instances behind an ALB/ASG) scaling trade-off: horizontal scaling is almost always preferred for stateless web/API tiers (better fault tolerance, finer-grained cost control, supports rolling deploys); vertical scaling is sometimes unavoidable for workloads that can't be distributed (a single large in-memory cache node, some legacy monoliths).

**The T-family CPU credit gotcha (a favorite interview trap)**

Burstable (`t3`, `t4g`, `t2`) instances are cheap because they're provisioned with a *baseline* CPU performance (e.g., 20–40% of a full core, family/size-dependent) and earn **CPU credits** while running below that baseline. Credits are spent to burst above baseline when needed.

```
    CPU running BELOW baseline
              |
              |  earns credits
              v
    +--------------------+
    |   Credit Balance   |
    +---------+----------+
              |
              |  spent when CPU > baseline
              v
     Burst ABOVE baseline
              |
              v
    Credit balance hits zero?
              |
    +---------+-----------+
    |                     |
 Standard mode      Unlimited mode
    |                     |
    v                     v
 Throttled back      Bursts continue,
 to baseline --      but billed extra
 a hard cap, and     per vCPU-hour
 the app slows       beyond baseline
 down                (cost surprise)
```

- **Standard mode**: once the credit balance is exhausted, CPU is hard-throttled back down to the baseline percentage — this is the classic "app was fine for hours, then suddenly became sluggish/unresponsive for no obvious reason" production incident. The root cause is almost always a sustained load period (batch job, traffic spike, backup/reindex) that outlasted the accumulated credit balance.
- **Unlimited mode**: bursts are allowed to continue indefinitely, but AWS bills you extra (per vCPU-hour) for sustained usage beyond baseline — protects availability but can produce a cost surprise if a T-instance is quietly running hot 24/7 (a sign you've outgrown the T-family and should move to `m`/`c`).
- **Diagnosis in an interview scenario:** "Our T3 instance got slow under sustained load, but CPUUtilization graphs don't look pegged at 100%" → check the `CPUCreditBalance` and `CPUSurplusCreditBalance` CloudWatch metrics, not just `CPUUtilization` — a throttled T-instance can show CPU capped well below 100% because it's being held at baseline, which looks deceptively "healthy" unless you know to look at credits specifically.
- **Rule of thumb:** T-family is right for workloads with genuine idle troughs (dev/test, low-traffic APIs, bursty-but-brief admin jobs); it is the *wrong* choice for anything with a sustained high-CPU period (nightly batch processing, backup windows, steady-state compute-heavy services) — those belong on `m`/`c`/`r` family instances with no credit mechanism to run out of.

**On-Demand vs Reserved vs Spot vs Savings Plans — how you actually decide (not just what they are)**

The Cost Optimization section later in this guide already tables out the discount percentages; the senior-level skill being tested here is the **decision process**, not the definitions:

| Question to ask yourself | Points toward |
|---|---|
| Is this workload's capacity need predictable 12+ months out? | Savings Plan / Reserved Instance |
| Could this workload be interrupted with ~2 minutes' notice without breaking correctness? | Spot |
| Is this a brand-new workload with unknown steady-state yet? | On-Demand first, commit later once usage data exists |
| Does the workload span multiple compute types (EC2 + Fargate + Lambda)? | Compute Savings Plan (flexible across compute types) over EC2 Instance Savings Plan/RI |
| Is this dev/test that's only running business hours? | On-Demand + scheduled stop/start (Instance Scheduler) — commitment-based discounts don't help if the instance isn't running most of the time anyway |
| Is the workload stateful with no cheap way to checkpoint/resume? | Avoid Spot — the 2-minute reclaim notice isn't enough to gracefully drain long-lived state |

**Interview-ready answer:** "I wouldn't jump straight to 'buy Savings Plans' — I'd first confirm the workload is stable and predictable enough to commit to, check whether it can tolerate interruption (Spot candidate), and only then choose between a Compute Savings Plan (flexible, spans EC2/Fargate/Lambda) versus an EC2/Instance Savings Plan (deeper discount, less flexible) based on how confident I am in the instance family staying fixed."

**When is EC2 the right compute choice vs Lambda/Fargate — the decision an interviewer actually wants**

This guide already has a Lambda-vs-ECS-vs-EC2-vs-Fargate comparison table and a dedicated .NET compute-decision section — the angle worth adding here is how to *justify* EC2 specifically when you're the one who provisioned it via Terraform/CDKTF rather than picking a serverless default:

- **Full OS/kernel access is a hard requirement** — custom kernel modules, specific driver versions, GPU workloads, or software that assumes it owns the host (some legacy .NET Framework/COM-interop scenarios) — Fargate and Lambda both abstract the OS away, which is a feature until it's a blocker.
- **Steady, high, predictable utilization** — if a service runs at 70–90% CPU 24/7, EC2 (especially with a Savings Plan) is usually the cheapest option; Fargate's per-task premium and Lambda's per-invocation billing both lose that comparison once utilization is consistently high.
- **You already have the IaC investment** — "I'm provisioning EC2 via Terraform/CDKTF already, so the operational tooling (state, modules, CI plan/apply pipeline) is a sunk cost that makes EC2 marginally cheaper to *operate*, not just to run" is a legitimate, honest talking point — but it should never be the *primary* justification. An interviewer will push back on "because that's what I already know" as the main reason, and rightly so — lead with the workload-shape argument (steady utilization, OS-level requirement) and mention existing Terraform tooling as a secondary, practical factor.
- **Long-running processes with in-memory state that can't be trivially externalized** — e.g., a stateful cache or a process holding a large warmed-up in-memory model — favors EC2 over Lambda's stateless-between-invocations model, though ECS/Fargate with EFS or a sticky task can sometimes also satisfy this.
- **Counter-signal to watch for:** if you find yourself justifying EC2 purely on "it's simpler to reason about" or "I don't want to learn containers," that's a comfort-driven answer, not a workload-driven one — senior interviewers are specifically listening for whether you separate "what I know" from "what the workload needs."

---

### [new content] EBS vs EFS vs S3

| | EBS | EFS | S3 |
|---|---|---|---|
| Type | Block storage | Managed NFS (file) | Object storage |
| Attach model | One EC2 instance (or Multi-Attach for specific volume types) | Many instances/AZs concurrently | HTTP API, unlimited clients |
| Use case | DB data volumes, boot volumes | Shared config/content across a fleet, Lambda file storage extension | Static assets, backups, data lake, logs |
| Scaling | Manual resize | Elastic, auto-scales | Effectively unlimited |
| .NET relevance | RDS/self-managed SQL Server data files | Shared session state/uploads across ECS tasks | Blob storage equivalent to Azure Blob Storage |

### EBS Volumes

**What EBS actually is:** a **network drive**, not a physical disk. That single fact explains most of its behaviour — there's network latency, it can be detached and reattached, and it survives the instance.

**Properties that get asked:**
- **Locked to one Availability Zone.** An `us-east-1a` volume cannot attach to an `us-east-1b` instance. To move it you snapshot it and create a new volume in the target AZ — that's *the* answer to "how do I move a volume across AZs/regions?"
- **One instance at a time** (except Multi-Attach, below).
- **Provisioned capacity** — you pay for the GB and IOPS you *provision*, not what you use. A 1 TB volume holding 4 GB costs the same as a full one.
- **Can be resized online** (Elastic Volumes) — grow size, change type, change IOPS with no downtime. You **cannot shrink** a volume; you create a smaller one and copy.
- **`DeleteOnTermination`** defaults to **true for the root volume** and **false for additional volumes**. This asymmetry is a real interview question and a real production incident: terminate an instance and the root volume (with your logs) is gone, while orphaned data volumes quietly accrue cost forever.
- Volumes persist independently of the instance — detach, reattach elsewhere in the same AZ, and the data is intact.

```bash
aws ec2 create-volume --availability-zone us-east-1a --size 100 --volume-type gp3 --encrypted
aws ec2 attach-volume --volume-id vol-abc --instance-id i-abc --device /dev/sdf
# on the instance: format (first time only!) and mount
lsblk                                   # confirm the device is visible
sudo mkfs -t xfs /dev/nvme1n1           # ⚠ destroys data — never run on a volume with data
sudo mkdir /data && sudo mount /dev/nvme1n1 /data
# add to /etc/fstab (by UUID, not device name) so it survives a reboot
```
**Two hands-on gotchas worth naming:** device names get remapped (you ask for `/dev/sdf`, Nitro shows `/dev/nvme1n1`), and if you forget the `/etc/fstab` entry the mount silently disappears on the next reboot. Both are explained below, because both are real production incidents rather than trivia.

**Gotcha 1 — the device name you request is not the name Linux uses.** You attach with `--device /dev/sdf`, but on any Nitro instance (M5/C5/T3/R5 and newer) disks are NVMe, so Linux calls it `/dev/nvme1n1`. `/dev/sdf` is only a label AWS records in its own console, and `mkfs -t xfs /dev/sdf` fails with "No such file or directory".

So **always run `lsblk` first** and identify the volume by size + "no partitions, no mountpoint":
```
NAME          SIZE TYPE MOUNTPOINTS
nvme0n1         8G disk
└─nvme0n1p1     8G part /          <- root volume, already mounted
nvme1n1       100G disk            <- the volume I just attached
```
Why this deserves care rather than a guess: **running `mkfs` on the wrong device formats your root volume and destroys the instance.** And NVMe numbering isn't stable — with several volumes attached, today's `nvme1n1` can be `nvme2n1` after a reboot, so don't hardcode NVMe names either. To map a device back to a real volume ID:
```bash
sudo nvme id-ctrl -v /dev/nvme1n1 | grep -i sn    # serial = the EBS volume ID (vol-0abc…)
ls -l /dev/disk/by-id/                            # stable nvme-Amazon_Elastic_Block_Store_vol… symlinks
```

**Gotcha 2 — `mount` is temporary; without `/etc/fstab` it vanishes on reboot.** `mount` only changes the running system, held in memory. After a reboot `/data` is once again an ordinary empty folder on the **root** volume, and three things go wrong at once:
1. The application keeps writing to `/data` — now filling the small **root** disk instead of the 100 GB data volume.
2. The existing data looks like it disappeared (it's safe on the EBS volume, just not attached to that path).
3. **The nasty one:** when you later remount the volume onto `/data`, anything written to the root-disk `/data` becomes **invisible** — mounting over a directory hides its contents. Now your data is split across two places and half of it is hidden.

The fix, using the filesystem **UUID** (stored inside the filesystem, so it survives device renaming):
```bash
sudo blkid /dev/nvme1n1        # -> UUID="a1b2c3d4-…" TYPE="xfs"
# /etc/fstab
# UUID=a1b2c3d4-…  /data  xfs  defaults,nofail  0  2
sudo umount /data && sudo mount -a && df -h /data   # ← TEST before rebooting
```
**`nofail` is not optional:** without it, an instance whose volume is missing at boot can drop into emergency mode and you **lose SSH access entirely** — the classic "I edited fstab and now I can't get into my server," which needs a rescue-instance procedure to undo. And `mount -a` is the step everyone skips: if it succeeds, the reboot will too. Discovering a broken fstab *by rebooting* is how people lock themselves out.

**The one-line version:** "Ask for `/dev/sdf`, but always `lsblk` to find the real NVMe name — and a `mount` with no `/etc/fstab` entry (by UUID, with `nofail`) silently disappears on the next reboot."

### EBS Volume Types

| Type | Class | Performance | Boot volume? | Use for |
|---|---|---|---|---|
| **gp3** | SSD, general purpose | Baseline **3,000 IOPS / 125 MB/s regardless of size**, up to 16,000 IOPS / 1,000 MB/s — IOPS provisioned **independently of capacity** | ✅ | **The default choice.** ~20% cheaper than gp2 |
| gp2 | SSD, general purpose | 3 IOPS per GB (so you had to over-provision *size* just to get IOPS), max 16,000 | ✅ | Legacy — migrate to gp3 |
| **io2 Block Express** | SSD, provisioned IOPS | Up to 256,000 IOPS, sub-millisecond latency, **99.999% durability** | ✅ | Mission-critical databases, large SQL Server/Oracle workloads |
| io1 | SSD, provisioned IOPS | Up to 64,000 IOPS | ✅ | Older generation of the above |
| st1 | **HDD**, throughput optimised | Up to 500 MB/s, low IOPS | ❌ | Big sequential reads: log processing, data warehouse, ETL |
| sc1 | **HDD**, cold | Up to 250 MB/s, cheapest per GB | ❌ | Infrequently accessed archive data that must still be a filesystem |

Two facts that are almost always the question: **only SSD types (gp2/gp3/io1/io2) can be boot volumes** — HDD types cannot. And **gp3's headline improvement is decoupling IOPS from size**: under gp2, needing 6,000 IOPS forced you to provision a 2 TB volume you didn't need. Being able to say that is the difference between naming the types and understanding them.

### EBS Snapshots

**What they are:** point-in-time backups of a volume, stored in **S3 managed by AWS** (not in a bucket you can see), and **incremental** — the first snapshot copies every used block, later ones copy only changed blocks. Deleting an old snapshot never breaks a newer one; AWS keeps whatever blocks are still referenced.

Key behaviours:
- You **can** snapshot an attached, running volume, but for a database you should quiesce/flush first (or use the DB's own backup) — otherwise you get a crash-consistent, not application-consistent, image.
- A snapshot is **region-scoped but AZ-independent**: restore it into **any AZ**, or **copy it to another region** — that copy is the standard EBS disaster-recovery move.
- **Fast Snapshot Restore (FSR)** removes the lazy-loading penalty (a fresh volume from a snapshot is normally slow on first touch of each block). Costs extra per snapshot per AZ; enable it only for snapshots you restore under time pressure.
- **Snapshot Archive** tier is ~75% cheaper but takes **24–72 hours** to restore — for compliance retention, never for recovery.
- **Recycle Bin** lets you set a retention rule so deleted snapshots/AMIs can be recovered — the guard against a fat-fingered or malicious deletion.
- **Data Lifecycle Manager (DLM)** automates snapshot creation/retention on a schedule, and it's the correct answer to "how do you back up EBS?" (rather than a cron job calling the CLI). AWS Backup is the bigger, cross-service version.

```bash
aws ec2 create-snapshot --volume-id vol-abc --description "pre-upgrade 2026-08-08"
aws ec2 copy-snapshot --source-region us-east-1 --source-snapshot-id snap-abc \
  --destination-region us-west-2 --encrypted          # DR copy
aws ec2 create-volume --snapshot-id snap-abc --availability-zone us-east-1b --volume-type gp3
```
**Cost leak to mention:** snapshots are the most commonly forgotten AWS charge — years of nightly snapshots from decommissioned volumes. DLM retention rules, or a Config rule, is how you stop it.

### AMIs (Amazon Machine Images)

**What an AMI is:** a launch template for a *machine* — snapshots of the root (and any additional) volumes, plus the block-device mapping and launch permissions. An EBS snapshot backs up a **disk**; an AMI backs up a **bootable machine**. That's the distinction interviewers probe.

- **AMIs are region-scoped.** You must **copy** an AMI to every region you launch in — the reason a multi-region deployment pipeline has an AMI-copy step.
- Types: AWS-provided (Amazon Linux, Windows Server), **Marketplace** (may carry a licence charge), and **your own custom AMIs**.
- **Creating one reboots the instance by default** so the filesystem is consistent. `--no-reboot` avoids downtime but risks a corrupt/inconsistent image — only safe if the app is quiesced.
- **The Golden AMI pattern:** pre-bake OS patches, the runtime (e.g. the .NET runtime), agents (CloudWatch, SSM), and your app's dependencies into an AMI, then keep user data thin. Faster, more reliable ASG scale-out than installing everything at boot. **EC2 Image Builder** automates building, patching, testing, and distributing these on a schedule.
- **Deregistering an AMI does not delete its snapshots** — another silent cost leak, and a good detail to know.

```bash
aws ec2 create-image --instance-id i-abc --name "dotnet8-base-2026-08" --description "golden AMI"
aws ec2 copy-image --source-region us-east-1 --source-image-id ami-abc --region us-west-2 --name "dotnet8-base"
```

#### The Golden AMI Pattern — Full Explanation

**The idea:** install everything **once, when you build the image** — not every time an instance launches. "Baking" means booting a machine, installing everything, and snapshotting it into a reusable AMI. Like corporate IT handing you a pre-imaged laptop rather than a blank one plus an install checklist.

**Before and after:**
```bash
# THICK user data — install at boot:  launch -> serving traffic = 4-6 min
yum update -y                                # 2-4 min, 200+ MB
rpm -Uvh https://packages.microsoft.com/...  # external repo dependency
yum install -y dotnet-runtime-8.0            # ~60s
yum install -y amazon-cloudwatch-agent       # ~30s
aws s3 cp s3://builds/myapp.zip /opt/ && unzip ... && systemctl start myapp

# THIN user data — golden AMI already has OS patches, .NET 8, agents, deps
#                                            launch -> serving traffic = 60-90s
echo "ASPNETCORE_ENVIRONMENT=Production" > /etc/myapp.env
aws s3 cp s3://builds/myapp-v42.zip /opt/app/ && systemctl start myapp
```

**Three reasons those minutes matter** (the second and third are the ones that separate a good answer from a generic one):
1. **Auto Scaling is only useful if it's fast.** A 9 a.m. spike fires the alarm, the ASG launches 5 instances — and with thick user data they're useless for 5–6 minutes, meaning 5–6 minutes of your *existing* instances serving timeouts. Slow launches mean you're always scaling for the traffic you had several minutes ago.
2. **Your fleet isn't actually identical.** `yum update -y` pulls whatever is current *at that moment*, so a January instance has different package versions than a March one from the same config — producing the worst kind of bug report: *"it only fails on some instances."*
3. **❗ A network blip becomes an ASG launch loop.** If an external repo is unreachable for 20 seconds the install fails, the script exits, and **the instance still boots "successfully"** — just with no runtime. Then: app never starts → ALB health check fails → ASG terminates it → launches a replacement → same failure → **repeat forever**, with zero healthy capacity. Every external dependency in your boot path is a chance for this, and baking removes them all.

**What to bake, and why each one:**
| Bake in | Why |
|---|---|
| **OS patches** | Avoids a 200 MB download per launch, and makes the patch level *known* rather than "whatever was current" |
| **Runtime** (.NET 8) | The largest install, and it depends on an external repo |
| **CloudWatch agent** | EC2 reports no memory or disk-space metrics without it |
| **SSM agent** | Enables Session Manager (shell with no SSH keys) and Patch Manager |
| **App dependencies** | Native libs, fonts, certificates — usually not the app itself |

**"Thin user data"** then means only what genuinely differs per instance or environment: which environment, which cluster to join, which config to fetch. **Config, not installation.**

**EC2 Image Builder** turns the manual launch → install → snapshot cycle into a pipeline:
```
1. RECIPE       base AMI + components: patch OS - install runtime - install agents - CIS hardening
2. BUILD        spins up a temp instance, runs the components, snapshots it
3. TEST         boots the new AMI and runs smoke tests — fails the pipeline if broken
4. DISTRIBUTE   copies the AMI to every region, shares it to every account
5. SCHEDULE     re-runs monthly, or on a critical CVE, so patches actually land
```
The **test phase** is the underrated part — it stops a broken image reaching your launch template — and it integrates with **Amazon Inspector** to scan for CVEs before release. **Packer** is the alternative and fits naturally alongside Terraform/CDKTF as the same toolchain, working across clouds.

**The mindset shift:** you stop patching servers and start **replacing** them.
```
Image Builder produces AMI v43 -> update the launch template ->
ASG instance refresh (rolling replacement) -> deregister old AMIs AND delete their snapshots
```
That's **immutable infrastructure**: patching in place creates snowflakes that drift apart, whereas rebuilding the image and rolling the fleet keeps every instance identical.

**Honest trade-offs:** the build pipeline is real infrastructure to maintain; AMI sprawl and orphaned snapshots cost money if you don't clean up; and iteration is slower, since changing one dependency means rebuilding an image — which is why many teams use thick user data in dev and golden AMIs in prod. **The container parallel:** a Docker image *is* a golden AMI for containers — same philosophy, much faster builds — which is why the pattern is stated as "custom AMI **or container image**". On ECS/Fargate you get it for free.

**The interview answer:** *"I'd bake a golden AMI with the patched OS, runtime, and agents via EC2 Image Builder on a monthly schedule, and keep user data to per-instance config only. It cuts ASG scale-out from ~5 minutes to ~90 seconds, guarantees identical instances, and removes external repos from the boot path — because a repo being briefly unreachable otherwise gives you an instance that boots healthy with no runtime, which becomes an ASG launch loop. Patching then becomes rebuild-and-roll via instance refresh rather than patching in place."* What's being tested is whether you see **launch time as an availability concern** and think in immutable-infrastructure terms.

### Instance Store

**Physically attached NVMe disks on the host** — not network storage. That gives it the highest possible IOPS (millions) and lowest latency of any EC2 storage, and also its one defining limitation:

- **Ephemeral.** Data is lost when the instance **stops, hibernates, terminates**, or the underlying host fails. It **does** survive a reboot.
- Cannot be snapshotted, resized, or detached/reattached. Capacity is fixed by instance type (the `d`/`i` families).
- You are responsible for replication and durability — full stop.

**Correct uses:** scratch space, temp files, buffers, caches, and distributed databases that already replicate across nodes (Cassandra, Elasticsearch/OpenSearch data nodes). **Wrong use:** anything you can't rebuild from another source.

**The one-liner:** "Instance store is the fastest and least durable option — EBS is a network drive that outlives the instance, instance store is local hardware that doesn't."

### EBS Multi-Attach

Lets a **single io1/io2** volume attach to **up to 16 Nitro instances in the same AZ** simultaneously, each with full read/write access.

The point everyone misses: **a normal filesystem (ext4, XFS, NTFS) will corrupt itself** if two instances mount it at once, because each caches metadata independently. Multi-Attach only works with a **cluster-aware filesystem** (GFS2, OCFS2) or an application that manages raw block access and its own locking. So it is *not* the shared-storage answer for a general web fleet — that's **EFS**. Multi-Attach exists for clustered HA applications that need concurrent raw block access, such as Oracle RAC-style setups.

Constraints: io1/io2 only, one AZ only, 16 instances max, Nitro instances only.

### EBS Encryption

Encryption at rest via **KMS (AES-256)**, and it covers more than people expect:
- Data at rest on the volume
- **Data in transit between the instance and the volume**
- All **snapshots** created from the volume
- All **volumes created from those snapshots** (encryption propagates automatically)

Performance impact is negligible (handled by the Nitro hardware), so there is no good reason to leave it off. Turn on **EBS encryption by default** at the account/region level so nobody has to remember.

**How to encrypt an existing unencrypted volume** — this exact procedure is a common question, because you cannot flip encryption on in place:
1. Snapshot the unencrypted volume.
2. **Copy** the snapshot, specifying `--encrypted` and a KMS key (the copy step is where encryption is introduced).
3. Create a new volume from the encrypted snapshot.
4. Stop the instance, detach the old volume, attach the new one at the same device name, start.

Sharing note: you can share an unencrypted snapshot publicly, but a snapshot encrypted with the **default AWS-managed key cannot be shared at all** — you must use a **customer-managed KMS key** and grant the other account access to it. That's the gotcha behind "why can't the other account use my snapshot?"

### EFS (Elastic File System)

**Managed NFS** that many instances across **multiple AZs** can mount **at the same time** — the shared-filesystem answer, where EBS is the single-attach answer.

- **POSIX/NFSv4.1 — Linux only.** ❗ **EFS does not support Windows.** For Windows workloads you use **Amazon FSx for Windows File Server** (SMB, Active Directory integrated). For high-performance Linux/HPC there's **FSx for Lustre**. Naming FSx when someone asks about shared storage for a Windows .NET Framework app is a strong differentiator.
- **Truly elastic** — grows and shrinks automatically to petabytes; you **pay per GB actually stored**, with no provisioning. But it costs roughly **3× gp3 per GB**, so it isn't a default — use it when you genuinely need concurrent shared access.
- Access is via a **mount target per AZ**, each with a security group. It must allow inbound **NFS port 2049** from the client SG — that's the #1 "EFS mount hangs" cause.
- **Performance modes:** *General Purpose* (default, lowest latency) vs *Max I/O* (higher throughput/parallelism, slightly higher latency, for thousands of clients).
- **Throughput modes:** *Bursting* (scales with size, can exhaust credits — same trap shape as T-family CPU credits), *Provisioned* (fixed, for small-but-busy filesystems), *Elastic* (auto, best default for spiky/unknown workloads).
- **Storage classes:** Standard, **One Zone** (~47% cheaper, single-AZ — fine for dev, not for production HA), plus Infrequent Access and Archive tiers, with **lifecycle management** to move files automatically after N days without access.
- Encryption at rest via KMS, in transit via TLS.

```bash
sudo mount -t efs -o tls fs-0123456789abcdef:/ /mnt/efs      # amazon-efs-utils, TLS in transit
```
**Typical uses:** shared uploads/content across an ECS or EC2 fleet, CMS (WordPress) document roots, shared config, CI build caches, and **Lambda file storage** for functions needing more than the 512 MB `/tmp` or shared state between invocations.

### EFS vs EBS vs Instance Store

| | EBS | EFS | Instance Store |
|---|---|---|---|
| Storage type | Block (network drive) | File (managed NFS) | Block (local hardware) |
| Attach | 1 instance (16 with Multi-Attach + cluster FS) | **Many instances, many AZs** | 1 instance, physically |
| AZ scope | **Single AZ** — snapshot to move | **Multi-AZ** (or One Zone class) | Single host |
| Capacity | **Provisioned** — pay for what you allocate | **Elastic** — pay for what you store | Fixed by instance type |
| Durability if instance dies | Survives | Survives | **Lost** |
| Performance | Good, tunable IOPS | Good, scales with load | **Highest** |
| OS support | Any | **Linux only** (Windows → FSx) | Any |
| Relative cost per GB | Baseline | ~3× EBS | Included in instance price |
| Typical use | Boot volumes, database data files | Shared content across a fleet | Cache, scratch, replicated DB nodes |

**The decision sentence:** "One instance needs a fast disk → EBS. Many instances need the same files at once → EFS (or FSx on Windows). I need maximum speed and can rebuild the data → instance store."

### EC2 Storage Shared Responsibility Model

| AWS is responsible for | You are responsible for |
|---|---|
| Durability of the EBS/EFS infrastructure; replicating EBS within its AZ | **Taking snapshots/AMIs and testing that they restore** |
| Replacing failed underlying hardware transparently | Choosing the right volume type and sizing IOPS/throughput |
| Providing encryption capability (KMS-integrated) | **Enabling encryption** and managing/rotating the keys |
| Physically destroying decommissioned drives | **Filesystem-level data protection**, access permissions, and what you put on the disk |
| Availability of the EFS mount targets | Security groups on those mount targets (NFS 2049), and the data your clients write |
| — | Knowing **instance store is ephemeral** and replicating anything that matters |

---

## Observability & Monitoring

### CloudWatch Deep Dive

**What it is:** AWS's core observability platform — Metrics, Logs, Alarms, Dashboards, Events (EventBridge), and integration with Traces (X-Ray, technically a separate service).

**Logs**
- Hierarchy: `Log Group → Log Streams → Log Events` (e.g., `/aws/lambda/ProcessOrder` with one stream per container instance).
- **CloudWatch Logs Insights**: SQL-like query language over logs.
```
fields @timestamp, @message
| filter @message like /ERROR/
| sort @timestamp desc
```
```
fields @timestamp, @duration
| filter @duration > 500
```
- Structured JSON logging makes fields directly queryable — a strong, low-effort win worth mentioning proactively in interviews.

**Metrics (know the key ones per service)**
| Service | Key metrics |
|---|---|
| Lambda | Invocations, Errors, Duration, Throttles, ConcurrentExecutions, IteratorAge (stream sources) |
| API Gateway | 2xx/4xx/5xx, Latency (p50/p90/p99), IntegrationLatency |
| SQS | ApproximateNumberOfMessagesVisible, ApproximateAgeOfOldestMessage |
| DynamoDB | ReadThrottleEvents, WriteThrottleEvents, ConsumedCapacity |
| EC2 | CPUUtilization, DiskReadOps/DiskWriteOps |

You can also publish custom business metrics (`PutMetricData`) — orders/minute, payment failure rate, pipeline throughput.

**Metric Math:** lets you combine multiple existing metrics into a derived formula *inside* CloudWatch itself, without publishing a separate custom metric or computing it client-side. The canonical example is an error-rate percentage: `(m1 / m2) * 100` where `m1` = `5xxRate` (or `Errors`) and `m2` = `TotalRequests` (or `Invocations`) — this gives you a single alarm-able, graphable series ("5xx error rate %") instead of eyeballing two separate raw-count graphs and doing the division in your head. Worth naming as the answer to "how would you alarm on an error *rate* rather than a raw error *count*" — raw counts are misleading at varying traffic volumes, and Metric Math is the built-in way to normalize for that without extra instrumentation code.

**Alarms → Action patterns**
| Alarm | Metric | Action |
|---|---|---|
| Queue backlog | `ApproximateNumberOfMessagesVisible > 1000` | SNS alert / scale consumer |
| DynamoDB throttling | `WriteThrottleEvents > 0` | Auto-scale provisioned capacity |
| Lambda failures | `Errors > 5%` | PagerDuty/email |
| API 5xx spike | `5xxErrorRate > 2%` | Alert dev team |

**EventBridge (formerly CloudWatch Events):** routes events from AWS services, your apps, and SaaS sources to Lambda/SQS/SNS/Step Functions/ECS — includes scheduled rules (`cron(0 1 * * ? *)`).

**CloudWatch vs CloudTrail (a classic trick question):** CloudWatch = observability (logs/metrics/alarms about *behavior/performance*); CloudTrail = governance/audit (a record of *who called which API, when*). They answer different questions and are not interchangeable.

**Embedded Metric Format (EMF):** structured JSON log format that CloudWatch automatically extracts into metrics — useful for high-cardinality custom metrics from Lambda without extra `PutMetricData` calls (and their associated API cost/throttling).

**Interview-ready closing summary:** "CloudWatch is AWS's core observability service — Logs, Metrics, Alarms, EventBridge, and Dashboards let me detect issues early, debug failures via Logs Insights, and automate remediation across Lambda, SQS, API Gateway, and DynamoDB in production."

### [new content] CloudWatch vs X-Ray: Complementary, Not Competing

The original notes mention X-Ray only briefly ("CloudWatch = Logs + Metrics; X-Ray = Tracing + Service maps") without depth — this expands it, since "CloudWatch vs X-Ray, when do you use each" is a standard senior observability question.

| | CloudWatch | X-Ray |
|---|---|---|
| Answers | "Is something wrong, and what does the aggregate look like?" | "Where exactly in this specific request's path did it go wrong/slow?" |
| Data shape | Logs (text), Metrics (time-series numbers) | Traces (request-scoped spans/segments across services) |
| Granularity | Service/function level | Individual request level, cross-service |
| .NET integration | `ILogger` → CloudWatch Logs via provider; custom metrics via SDK | `AWSXRayRecorder` middleware / `Amazon.XRay.Recorder.Handlers.AspNetCore` for ASP.NET Core; AWS SDK calls auto-instrumented via `AWSSDKHandler` |
| Typical use | Alarms, dashboards, aggregate error rate | Root-causing a specific slow/failing request across Lambda→DynamoDB→external API |

**How they combine in practice:** CloudWatch alarm fires on elevated p99 latency or error rate → you pull the trace ID from the structured log line (log the X-Ray trace ID as a correlation field) → open that trace in X-Ray to see exactly which downstream call (DynamoDB, SQS, external HTTP) added the latency or threw. Neither tool alone gives you both "something's wrong" and "here's exactly why" — production-grade .NET-on-AWS observability needs both, wired together via a shared trace/correlation ID in your structured logs.

### CloudTrail

**What it is:** the **audit log of your AWS account** — a record of every API call, whether it came from the console, CLI, SDK, or another AWS service. Each event records **who** (the IAM identity, including the role session name), **what** (the API), **when**, **from which source IP**, and **whether it succeeded**.

**Three event types — the distinction is the exam question:**
| Type | Covers | Logged by default? |
|---|---|---|
| **Management events** | Control-plane operations: `RunInstances`, `CreateBucket`, `AssumeRole`, `PutBucketPolicy` | ✅ Yes, free, and visible in **Event history for 90 days** |
| **Data events** | **Data-plane** operations: S3 `GetObject`/`PutObject`/`DeleteObject`, Lambda `Invoke`, DynamoDB item-level access | **❌ No — you must enable them, and they cost extra** (high volume) |
| **Insights events** | ML-detected unusual activity — an abnormal spike in a given API call | ❌ Opt-in |

**❗ The gotcha this leads to:** "who deleted that S3 object?" is **not** answerable from default CloudTrail, because object-level deletes are **data events** and are off by default. The audit trail you need has to be enabled *before* the incident. That's a genuinely good thing to point out unprompted.

Other properties:
- **Event history** (90 days, in-console, free) vs a **Trail** — a trail delivers events to **S3** for indefinite retention, optionally also to **CloudWatch Logs** so you can build metric filters and **alarms** (e.g. alert on root-account usage, on `DeleteTrail`, or on IAM policy changes).
- **Organization trails** capture every account in the AWS Organization into one central bucket — the standard multi-account audit design, usually in a dedicated log-archive account with the bucket locked down (Object Lock + restricted policy) so even an account admin can't tamper with it.
- **Log file validation** produces digest files so you can cryptographically prove logs weren't altered or deleted.
- **Query at scale with Athena** — CloudTrail logs in S3 plus Athena is the practical way to answer forensic questions (see [Athena](#athena)); **CloudTrail Lake** is the managed alternative.
- **❗ Not real-time** — delivery typically lags by up to ~15 minutes. For anything needing immediate reaction, use **EventBridge** rules on the event pattern instead of polling CloudTrail.

**The three-way comparison interviewers love:**
| | **CloudWatch** | **CloudTrail** | **AWS Config** |
|---|---|---|---|
| Question it answers | "**How is it performing?**" | "**Who did what, when?**" | "**What does the configuration look like, and did it drift?**" |
| Data | Metrics, logs, alarms | API call audit records | Resource configuration snapshots + change history |
| Typical use | Alerting, dashboards, debugging behaviour | Security forensics, compliance audit | Compliance rules, drift detection, "show me this SG's config last Tuesday" |

### AWS Health Dashboard

Two things with confusingly similar names:
- **Service Health Dashboard** — the **public** status page for all AWS services and regions. Generic; tells you nothing about your own resources.
- **Your Account Health Dashboard** — **personalised** events that affect **your** specific resources: an EC2 instance on degraded hardware needing retirement, scheduled RDS maintenance, an EBS volume needing action, certificate or runtime **end-of-life** notices, and region/service issues *in the regions you actually use*.

**The AWS Health API** exposes those events programmatically, and the **EventBridge integration** lets you automate responses — e.g. an instance-retirement notice triggers a Lambda that drains and replaces the instance before the forced stop. That automation angle is the senior answer.

**Interview framing:** "The public Service Health Dashboard tells me whether AWS thinks a service is degraded; the **Account** Health Dashboard and Health API tell me whether it's degraded **for my resources**, plus scheduled maintenance and retirement notices I need to act on. I'd wire the Health API into EventBridge so maintenance events open a ticket automatically rather than being noticed by someone reading email."

### Container Insights, the CloudWatch Agent & Proactive Monitoring

**❗ The gap most people miss:** the default EC2 metrics CloudWatch collects from the hypervisor include CPU, network, and disk *I/O* — but **not memory usage and not filesystem free space**, because those require visibility inside the guest OS. You must install the **CloudWatch Agent** (via SSM, ideally baked into your AMI) to get memory, swap, and disk-space metrics. Being asked "how do you alarm on memory?" and answering "CloudWatch memory metric" is a classic wrong answer.

- **Container Insights** — cluster, service, task, and pod-level CPU/memory/network/disk metrics plus auto-generated dashboards for **ECS and EKS**. The container equivalent of the agent problem: without it you're blind inside the task.
- **Lambda Insights** — per-invocation memory, CPU, and init duration for functions.
- **CloudWatch Application Signals** — APM-style service-level views (latency, error rate, throughput, SLOs) built on OpenTelemetry, tying metrics to traces automatically.
- **CloudWatch Synthetics (canaries)** — scripted checks that call your endpoints on a schedule from outside, so you detect an outage **before a user reports it**. The answer to "how do you know your site is up when there's no traffic at 3 a.m.?"
- **CloudWatch RUM** — real-user monitoring from actual browsers: page load times, JS errors, Core Web Vitals. Pairs with Synthetics: RUM tells you what users experience, Synthetics tells you what a known-good request experiences.
- **CloudWatch Logs Insights** — the query language for logs; **subscription filters** stream logs onward to Lambda/Firehose/OpenSearch in near-real-time.

```bash
# Alarm on p99 latency using an extended statistic
aws cloudwatch put-metric-alarm --alarm-name api-p99-latency \
  --namespace AWS/ApplicationELB --metric-name TargetResponseTime \
  --extended-statistic p99 --period 60 --evaluation-periods 3 --threshold 1.5 \
  --comparison-operator GreaterThanThreshold --treat-missing-data notBreaching \
  --alarm-actions arn:aws:sns:us-east-1:123456789012:oncall

# Logs Insights: slowest Lambda invocations
aws logs start-query --log-group-name /aws/lambda/my-fn \
  --start-time $(date -d '1 hour ago' +%s) --end-time $(date +%s) \
  --query-string 'fields @timestamp, @duration, @requestId | filter @type="REPORT" | sort @duration desc | limit 20'

aws logs tail /aws/lambda/my-fn --follow --format short      # live tail while debugging
```
**Two alarm-design details worth stating:** set **`--treat-missing-data`** deliberately (a metric that stops being published because the service is *down* will leave an alarm in `INSUFFICIENT_DATA` forever if you don't), and alarm on **rates via Metric Math** rather than raw counts, since raw error counts are meaningless when traffic volume swings.

---

# PART II — Tier 2: Design-Level Confidence

> **Reason about it; be honest about hands-on gaps.** These appear in "how would you architect X?" questions, where structured reasoning matters more than operational war stories. Where I lack production experience the section says so explicitly — that framing consistently lands better than a confident wrong answer.

## Containers: Docker, ECS, ECR & Fargate

> **Tier 2 — reason about, be honest about hands-on.** Docker I use; ECS/Fargate I can design with but have not operated in production. The honest framing is in [Fargate/ECS/EKS Trade-offs](#gaps-fargateecseks-trade-offs--reasoning-without-hands-on-time) — use it rather than bluffing.

### AWS Fargate

**Definition:** Serverless compute engine for containers — you supply a Docker image + CPU/memory; AWS handles the underlying servers, scaling, OS, and patching. Fargate is not an orchestrator itself — it's a launch type for **ECS** or **EKS**.

**Core concepts**
- **Task** — one running container (or co-located group). **Task Definition** — blueprint (image, CPU/memory, env vars, IAM role). **Service** — keeps N tasks running/healthy.
- Networking: `awsvpc` mode only — every task gets its own ENI (strong isolation, but IP exhaustion is a real capacity constraint in small subnets).
- No SSH/host access; task-level IAM role instead of instance role.

**Pricing:** pay for vCPU-seconds + GB-seconds only while the task runs — zero idle cost.

**Sizing is constrained, not free-form.** You pick from **valid vCPU/memory pairs**, not arbitrary numbers: 0.25 vCPU → 0.5/1/2 GB; 0.5 vCPU → 1–4 GB; 1 vCPU → 2–8 GB; 2 vCPU → 4–16 GB; 4 vCPU → 8–30 GB; and 8/16 vCPU tiers for larger workloads. Ask for a combination that isn't on the list and the task definition is rejected — a common first-time surprise. Memory scales in fixed steps per vCPU tier, so "I just need a bit more RAM" sometimes means paying for another vCPU.

**Ephemeral storage:** each task gets **20 GB by default**, configurable up to **200 GB** — and it's *ephemeral*, gone when the task stops. For anything that must persist or be shared between tasks, mount **[EFS](#efs-elastic-file-system)**. This is also the constraint that bites large container images or build/scratch workloads.

**Fargate Spot:** the same interruption model as EC2 Spot — up to **~70% cheaper**, with a **2-minute `SIGTERM` warning** before reclamation. Ideal for batch jobs, CI runners, and queue consumers where SQS absorbs the interruption. A **capacity provider strategy** lets one service mix them, e.g. *"2 tasks always on `FARGATE`, everything above that on `FARGATE_SPOT`"* — baseline reliability with cheap burst. This also depends on your app handling `SIGTERM`, which is the exec-form `ENTRYPOINT` point from the Docker section.

**Also worth knowing:** Fargate **platform versions** control the underlying runtime (use `LATEST` unless pinning for a reason); there's **no privileged mode, no host-level daemons/DaemonSets, and no GPU support** — those requirements push you to the EC2 launch type; and Windows containers are supported but with a higher per-task floor.

**EC2 vs Fargate**
| Aspect | EC2 | Fargate |
|---|---|---|
| Server management | You | AWS |
| OS/SSH access | Yes | No |
| Scaling | ASG (minutes) | Automatic (faster) |
| Pricing | Instance-hour, idle cost | Per task, no idle cost |
| Security isolation | Shared host possible | Strong (dedicated ENI/kernel per task) |
| Best for | Steady/high utilization, legacy, GPU | Bursty/microservices, ops simplicity priority |

**Cost trap (interview favorite):** "Fargate is always cheaper" is **false**. Fargate wins for bursty/low-utilization workloads; EC2 wins for steady, high-utilization workloads because you're not paying the Fargate per-task premium on idle-free capacity you'd have used anyway. Numeric rule of thumb from the notes: a 24×7 1 vCPU/2GB service costs roughly $17–20/mo on EC2 (t3.small) vs $35–40/mo on Fargate; a job running 2 hrs/day at the same size flips to ~$20/mo (EC2, mostly idle) vs ~$6–8/mo (Fargate).

**Common false interview claims:** "Fargate is always cheaper", "Fargate replaces EC2", "You can SSH into Fargate", "Fargate has no networking limits" — all false.

---

### EC2 vs Fargate Cost & Trap Scenarios

**Sample EC2 trap Q&A**
- *CPU is low but app is slow* → bottleneck likely disk I/O, network latency, or a single-threaded hot path — CPU% alone is misleading.
- *Public IP changed after restart* → public IPs aren't static unless you attach an Elastic IP.
- *ASG didn't scale during a spike* → check scaling policy metric choice and cooldown period length.
- *Instance unreachable despite "running"* → check security group, NACL, route table, and whether it even has a public IP.
- *Spot instance terminated suddenly* → expected behavior; AWS can reclaim spot capacity anytime (2-minute warning via EventBridge).

**Sample Fargate trap Q&A**
- *Task stopped unexpectedly* → app crashed, failed health check, or hit its memory limit (OOM-killed).
- *"Healthy" task but service marks it unhealthy* → container health check failing despite the process being alive (e.g., wrong health check path/port).
- *Fargate task can't reach internet* → it's in a private subnet with no NAT Gateway/VPC endpoint.
- *Works in dev, fails in prod* → almost always IAM role, secrets, or networking (subnet/SG) differences between environments.

**Final mental model:** Steady load → EC2. Bursty load → Fargate. Need control → EC2. Need simplicity → Fargate. Idle-cost sensitive → Fargate. High, constant utilization → EC2.

---

### Docker & Container Fundamentals

**What a container is:** your application plus its dependencies packaged into one immutable artifact that runs identically anywhere. Unlike a VM it **shares the host's kernel** rather than booting its own OS.

| | Virtual Machine | Container |
|---|---|---|
| Isolation | Full OS + own kernel, via hypervisor | Process-level, **shares the host kernel** |
| Size | GBs | MBs |
| Start time | Minutes | **Seconds or less** |
| Density per host | Low | High |
| Trade-off | Stronger isolation | Far cheaper and faster, weaker kernel-level isolation |

**Image vs container:** an **image** is the immutable template; a **container** is a running instance of it. (The class-vs-object analogy lands well.) Images are built from a **Dockerfile** in **layers**, and layers are cached — which is why instruction order matters enormously for build speed.

```dockerfile
# Multi-stage build — the .NET pattern worth knowing by heart
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.csproj ./                 # copy csproj first so restore layer caches
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final   # runtime-only, much smaller
WORKDIR /app
COPY --from=build /app .
USER $APP_UID                    # don't run as root
ENTRYPOINT ["dotnet", "MyApi.dll"]
```
Three points to make about this: **multi-stage builds** keep the SDK out of the shipped image (hundreds of MB saved); **copying the `.csproj` before the source** means `dotnet restore` is cached and only re-runs when dependencies change; and the **chiselled/Alpine** runtime variants plus **Native AOT** shrink it further, which matters for cold starts and ECR storage.

```bash
docker build -t myapi:1.0 .
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development myapi:1.0
docker ps / docker logs <id> / docker exec -it <id> sh
```
**Why any of this is an interview topic:** the container is the *immutable artifact* you promote unchanged from dev to prod — the same bits that passed CI are the bits that run in production. That's what actually kills "works on my machine", and it's the reason config comes from the environment (env vars, Parameter Store, Secrets Manager) rather than being baked into the image.

#### Docker Images — The Detail Worth Knowing

**Get the vocabulary straight first**, because interviewers use these interchangeably and precision reads well:
```
REGISTRY            ECR, Docker Hub                       — the server that stores repositories
 └─ REPOSITORY      123456789012.dkr.ecr…/myapi           — all versions of one image
     └─ IMAGE       myapi:1.4.2   (tag)                   — a mutable, human-friendly pointer
     └─ IMAGE       myapi@sha256:9f2a…  (digest)          — the immutable content hash
         └─ CONTAINER                                     — a running instance of that image
```
**Tag vs digest is the one that matters operationally.** A **tag is a movable label** — `myapi:1.4.2` can be repointed at different bits tomorrow. A **digest is a cryptographic hash of the content** and can never mean anything else. So deploy by **digest** (or an immutable tag) for reproducibility and reliable rollback; `:latest` is the anti-pattern, because "roll back to the previous latest" is not a thing that exists. This is why **[ECR tag immutability](#ecr-elastic-container-registry)** is worth turning on.

**Layers, properly.** Each Dockerfile instruction creates a **read-only layer**, and the image is those layers stacked by a union filesystem. A running container adds a thin **writable layer** on top — which is why **container filesystem changes vanish when it's replaced** (state must go to a volume, EFS, S3, or a database).

Two consequences that get asked:
- **Layers are shared and cached.** Ten containers from the same image don't store ten copies; and pulling a new version only downloads the layers that changed. This is why instruction order matters — put the things that change *least* (base image, dependency restore) **before** the things that change *most* (your source), exactly as the `.csproj`-before-source trick does above.
- **❗ Deleting a file in a later layer does not shrink the image**, and anything present in an earlier layer is **still extractable**. So `COPY secrets.json . && RUN rm secrets.json` leaves the secret in the image permanently. Never put credentials in a build; use build secrets or inject at runtime.

**Base image choice — a real decision, not a detail:**
| Variant | Size (approx.) | Trade-off |
|---|---|---|
| `mcr.microsoft.com/dotnet/aspnet:8.0` | ~220 MB | Full Debian; has a shell and package manager — easiest to debug |
| `…aspnet:8.0-alpine` | ~110 MB | musl libc instead of glibc — small, but watch for native-dependency issues |
| **`…aspnet:8.0-jammy-chiseled`** | **~110 MB** | **Ubuntu chiselled — no shell, no package manager, non-root by default.** Much smaller attack surface. The strong default for production .NET |
| `…runtime-deps:8.0-jammy-chiseled` + **Native AOT** | **~15–30 MB** | Self-contained single binary, fastest startup. Best for Lambda container images and cold-start-sensitive work |

**"No shell" is a feature, not a limitation** — an attacker who achieves RCE has no shell to pivot with. The cost is that `docker exec … sh` no longer works, so you debug through logs and metrics instead. Naming that trade-off is the senior version of the answer.

**`.dockerignore` — small file, two real problems solved.** Everything in the build context is sent to the daemon, so without it you upload `bin/`, `obj/`, `node_modules/`, and `.git` on every build (slow), and risk `COPY . .` baking **`appsettings.Development.json`, `.env`, or `.aws/credentials`** into the image (a genuine secret leak):
```
bin/
obj/
.git/
.vs/
**/node_modules
**/appsettings.*.json
.env
Dockerfile
```

**`ENTRYPOINT` vs `CMD`** — used in the Dockerfile above and worth being able to explain:
- **`ENTRYPOINT`** = the executable that always runs. Hard to override (needs `--entrypoint`).
- **`CMD`** = the default *arguments*, trivially overridden by anything you append to `docker run`.
```dockerfile
ENTRYPOINT ["dotnet", "MyApi.dll"]     # always runs this
CMD ["--environment=Production"]       # default arg; `docker run img --environment=Staging` overrides it
```
Rule: `ENTRYPOINT` for *what the container is*, `CMD` for *how it's configured by default*. Also prefer the **exec form** (`["dotnet","MyApi.dll"]`) over the shell form — the shell form wraps your process in `/bin/sh -c`, so **your app never receives `SIGTERM`** and gets killed instead of shutting down gracefully. In ECS/Kubernetes that turns every deploy into dropped in-flight requests.

Related: **`ARG` vs `ENV`.** `ARG` exists only at build time; `ENV` persists into the running container (and is visible via `docker inspect` — so not for secrets).

**❗ Multi-arch images and Graviton.** An image built on an x86 laptop will **not run** on an ARM64 Graviton instance — the task simply fails to start with an `exec format error`, which is a genuinely confusing first encounter. Since the guide recommends Graviton for a ~20% .NET saving (see [EC2 Instance Types](#ec2-instance-types-user-data--metadata)), you need to build for the target:
```bash
docker buildx build --platform linux/amd64,linux/arm64 -t <ecr>/myapi:1.4.2 --push .
```
That pushes a **manifest list** — one tag serving both architectures, with each host pulling the right one. This is the practical prerequisite for the Graviton cost saving, and it's the kind of detail that shows you've actually shipped containers.

**Local development** uses **Docker Compose** to run the app plus its dependencies together (`docker compose up`) — a Postgres and a Redis container alongside your API, so a new developer needs no local installs. Worth knowing Compose is a **local/dev tool**: in AWS the equivalent responsibilities belong to an **ECS task definition** (multi-container) and real managed services (RDS, ElastiCache). Note also that a **container writes to an ephemeral layer**, so local persistence needs a **volume** (`-v ./data:/data`) — and in ECS the equivalent is an **EFS volume** mounted into the task.

**Image hygiene, as a checklist:** multi-stage build · chiselled/AOT base · non-root `USER` · pinned base-image tag or digest (not `:latest`) · `.dockerignore` · no secrets in any layer · scanned on push ([ECR enhanced scanning](#ecr-elastic-container-registry)) · a `HEALTHCHECK` or an ALB-checked `/health` endpoint · exec-form `ENTRYPOINT` so `SIGTERM` reaches the app.

### ECS (Elastic Container Service)

AWS's **own** container orchestrator — simpler than Kubernetes, deeply integrated with IAM/ALB/CloudWatch, and with no control-plane cost.

**The object model, outermost in:**
```
Cluster  →  Service  →  Task  →  Container(s)
              ↑          ↑
        desired count   instance of a Task Definition
```
- **Task Definition** — the JSON blueprint: image, CPU/memory, port mappings, environment variables, secrets (from Secrets Manager/Parameter Store), log configuration, and **the two IAM roles**. It's versioned; every change creates a new revision.
- **Task** — one running instance of a task definition (one or more co-located containers).
- **Service** — keeps N tasks running and healthy, replaces failures, and registers/deregisters them with an ALB target group.
- **Cluster** — the logical grouping and, for the EC2 launch type, the pool of instances.

**❗ The two roles — the most common real-world ECS mistake:**
| Role | Used by | Needs permissions for |
|---|---|---|
| **Task execution role** | The ECS agent, *before* your code runs | Pulling the image from **ECR**, writing to **CloudWatch Logs**, fetching secrets referenced in the task definition |
| **Task role** | **Your application code** | S3, DynamoDB, SQS — whatever the app actually calls |

Adding your app's DynamoDB permission to the *execution* role produces an `AccessDenied` that looks inexplicable. See [IAM Roles](#iam-roles-policies-assumerole).

**Launch types:**
- **Fargate** — serverless; you specify CPU/memory and AWS runs it. No instances to patch or scale. Higher per-vCPU price, no idle waste. Default choice.
- **EC2** — you run and patch the instances (with the ECS agent) and pack tasks onto them. Cheaper at high, steady utilisation, and required for GPU, Windows-specific host needs, or host-level daemons. **Capacity Providers** (with managed scaling) let the cluster add/remove EC2 capacity as tasks demand it.

**Networking modes:** **`awsvpc`** gives every task **its own ENI, private IP, and security group** — this is **mandatory for Fargate** and the right choice generally, because it means you can write security-group rules per service. `bridge` and `host` are the older EC2-launch-type modes (host shares the instance's network stack; bridge maps ports and complicates dynamic port registration).

**Scaling and deployment:**
- **Service Auto Scaling** — target tracking on `ECSServiceAverageCPUUtilization`, memory, or (best) **`ALBRequestCountPerTarget`**; the same "scale on a demand-correlated metric" principle as [ASGs](#auto-scaling-groups-asg).
- **Rolling update** with `minimumHealthyPercent` / `maximumPercent` controlling how many tasks may be down or extra during a deploy.
- **Deployment circuit breaker** — automatically rolls back a deployment whose tasks fail to stabilise. Turn it on; it's the difference between a failed deploy and an outage.
- **Blue/green via CodeDeploy** — shifts traffic between two target groups with canary/linear options and automatic rollback on CloudWatch alarms.
- **Service discovery** via **Cloud Map** (DNS names for services) or **Service Connect** for service-to-service traffic.
- **Persistent/shared storage** via **EFS** volumes mounted into tasks (see [EFS](#efs-elastic-file-system)).

**Sidecar containers.** A task definition can hold **several containers that share the task's network namespace and lifecycle** — so they reach each other on `localhost` and start/stop together. That's the sidecar pattern, and the standard AWS examples are the **FireLens/Fluent Bit** log router, the **AWS X-Ray daemon**, an **Envoy** proxy for App Mesh/Service Connect, and the **OpenTelemetry Collector**. Use `dependsOn` and `essential` to control ordering — `essential: true` means the whole task stops if that container dies, which is what you want for your app container and *not* for a best-effort log shipper. The interview point: sidecars keep cross-cutting concerns (logging, tracing, mTLS) out of your application image.

**Task placement (EC2 launch type only — Fargate handles this for you):**
- **Strategies:** `binpack` (pack tasks tightly onto fewest instances — cheapest, best for cost), `spread` (distribute across AZs or instances — most resilient, and `spread` on `attribute:ecs.availability-zone` is the usual production default), `random`.
- **Constraints:** `distinctInstance` (never two of these on one host) and `memberOf` with an expression (e.g. only GPU or Graviton instances).

Cost versus resilience is the trade-off to name: `binpack` minimises instance count and therefore spend; `spread` survives losing a host or an AZ. Most teams spread across AZs and binpack within them.

#### ECS on the EC2 Launch Type

**What changes versus Fargate:** you own the servers. The cluster becomes a pool of **container instances** — EC2 instances running the **ECS container agent**, registered to the cluster. ECS schedules tasks onto them; you keep them alive, patched, and correctly sized.

**❗ There are three IAM roles, not two.** The doc above covers the two *task* roles; the EC2 launch type adds a third, and forgetting it is the classic "my instances never show up in the cluster" failure:

| Role | Attached to | Used by | Needs permissions for |
|---|---|---|---|
| **ECS instance role** (`ecsInstanceRole`, policy `AmazonEC2ContainerServiceforEC2Role`) | The **EC2 instance profile** | The **ECS agent** on the host | Registering the instance with the cluster, pulling from ECR, writing logs |
| **Task execution role** | The task definition | ECS, *on behalf of* the task | ECR pull, CloudWatch Logs, fetching secrets |
| **Task role** | The task definition | **Your application code** | S3, DynamoDB, SQS — whatever the app calls |

Fargate only has the latter two, because there's no instance for you to own.

**Getting instances into the cluster.** Use the **ECS-optimised AMI** (ECS agent + container runtime pre-installed), or install the agent yourself. Then the one line people forget:
```bash
#!/bin/bash
echo "ECS_CLUSTER=prod-cluster" >> /etc/ecs/ecs.config
```
Omit it and the instance launches fine, registers itself with the cluster named `default`, and your actual cluster shows **0 container instances** while everything looks healthy in EC2.

**Capacity Providers + Cluster Auto Scaling** — the right way to size the pool. A capacity provider wraps an **ASG**; with **managed scaling** enabled, ECS publishes a `CapacityProviderReservation` metric and target-tracks it, so the ASG grows when tasks can't be placed and shrinks when capacity is idle.
- **Target capacity %** — `100` means "scale so tasks just fit" (cheapest, slowest to place new tasks); below 100 keeps warm headroom so tasks start immediately.
- **❗ Managed termination protection must be on**, otherwise the ASG will happily terminate an instance that still has running tasks on it.
- Mix providers with **base and weight** — e.g. base of 2 tasks on On-Demand, everything above that on **EC2 Spot**.

#### How EC2, ASG, Capacity Provider, Cluster, Service & Tasks Fit Together

```
       +-------------------------------------------------------------------+
       |                          ECS CLUSTER                              |
       |     the boundary: holds services AND registered instances         |
       +--------------------+---------------------------+------------------+
                            |                           |
     -- LOOP 1: scale TASKS -+                           +- LOOP 2: scale INSTANCES --
                            |                           |
                   +--------v---------+        +---------v-------------+
                   |     SERVICE      | picks  |  CAPACITY PROVIDER    |
                   |  desiredCount=6  |------->|  wraps exactly 1 ASG  |
                   |                  |  via a |  + managed scaling    |
                   | (Service Auto    |strategy|                       |
                   |  Scaling)        |        |  <<< THE BRIDGE >>>   |
                   +--------+---------+        +---------+-------------+
                            |                            |
                            | maintains                  | target-tracks
                            |                            | CapacityProviderReservation
                   +--------v---------+        +---------v-------------+
                   |      TASKS       |        |         ASG           |
                   |  6 containers    |        |  min / desired / max  |
                   +--------+---------+        +---------+-------------+
                            |                            |
                            | scheduled onto             | launches / terminates
                            |                            |
                            +------------+---------------+
                                         |
                            +------------v-------------------------+
                            |       CONTAINER INSTANCES            |
                            |  EC2 + ECS agent, registered to the  |
                            |  cluster via ECS_CLUSTER=<name>      |
                            +--------------------------------------+
```

**Reading it as ownership — who knows about what:**
| Object | What it is | What it knows |
|---|---|---|
| **ECS Cluster** | A logical boundary holding services and registered instances | Nothing about EC2 or ASGs **directly** |
| **Container instance** | One EC2 instance running the ECS agent, registered to the cluster (1 EC2 instance = 1 container instance) | Which cluster it joined |
| **ASG** | Owns the EC2 lifecycle — launches and terminates instances | **Nothing about ECS, tasks, or containers** |
| **Capacity Provider** | **The bridge.** Wraps exactly one ASG and is attached to the cluster | Both worlds — it's the *only* object that lets ECS influence the ASG |
| **Service** | Keeps N tasks running; selects capacity via a **capacity provider strategy** | Task count and which provider(s) to use |
| **Task** | The running container(s) | The instance it was placed on |

**❗ The insight the diagram exists to make: there are two independent scaling loops.**

| | **Service Auto Scaling** (Loop 1) | **Cluster Auto Scaling** (Loop 2) |
|---|---|---|
| Scales | **Tasks** (`desiredCount`) | **EC2 instances** (ASG desired capacity) |
| Driven by | Application demand — `ALBRequestCountPerTarget`, CPU, SQS backlog | **Task placement pressure** — tasks that don't fit |
| Mechanism | Application Auto Scaling target tracking | Capacity provider **managed scaling** |
| Missing it means | Traffic rises but task count stays flat | Tasks sit **`PENDING`** forever with nowhere to run |

They're complementary: **Loop 1 decides how many tasks you want; Loop 2 makes sure there's somewhere to put them.** Scaling tasks without scaling instances just produces pending tasks; scaling instances without scaling tasks just produces an idle bill.

**On Fargate, Loop 2 doesn't exist** — AWS supplies the capacity, so there's no ASG, no capacity provider wrapping one, and no instance registration. That absence *is* what the Fargate per-vCPU premium buys.

**How the reservation metric actually works** — one number drives Loop 2:
```
CapacityProviderReservation = (M / N) x 100

  N = instances currently running in the ASG
  M = instances ECS calculates it NEEDS for running + pending tasks
```
- **M > N** → metric above 100 → tasks can't fit → ASG scales **out**
- **M < N** → metric below 100 → spare capacity → ASG scales **in**
- **Target capacity `100`** means "aim for exactly enough" — cheapest, but a new task waits for an instance to boot. Set it to **~80** to hold roughly 20% warm headroom so tasks start immediately.

**Failure modes, and which object is at fault:**
| Symptom | Which object |
|---|---|
| Tasks stuck `PENDING`, ASG never grows | **No capacity provider**, or managed scaling off — ECS has no way to *ask* for instances |
| Scale-in terminates instances with running tasks | **Managed termination protection off** |
| Instances launch fine but the cluster shows zero | **Agent** — `ECS_CLUSTER=` missing from user data, so it registered to `default` |
| ASG at max, tasks still pending | **ASG `max`** — the ceiling is yours to raise, ECS won't exceed it |
| Capacity arrives too slowly during a burst | **Target capacity at 100** — no headroom. Lower it, or use ASG warm pools |

**One-liner:** *"The ASG owns instances, the service owns tasks, and the capacity provider is the only thing connecting the two — which is why ECS cannot scale your EC2 fleet without one, and why Fargate needs none of this."*

**Resource allocation — the numbers ECS actually schedules on:**
- **CPU is in "CPU units": 1024 units = 1 vCPU.**
- Memory has two settings: **`memory` is a hard limit** (exceed it and the container is OOM-killed) and **`memoryReservation` is a soft limit** (the amount guaranteed and used for scheduling; the container may burst above it if the host has room). Best practice is to set the soft limit for scheduling and the hard limit as a safety ceiling.
- ECS only places a task where the **remaining** CPU *and* memory fit, which produces the single most common EC2-launch-type error:

> `unable to place a task because no container instance met all of its requirements`

Causes, in the order worth checking: not enough remaining CPU/memory on any instance · a **host port conflict** · no instance satisfying a placement constraint (see [task placement](#ecs-elastic-container-service) above) · the per-instance **ENI limit** reached in `awsvpc` mode.

**Networking modes — a choice you only get on EC2:**
| Mode | Behaviour |
|---|---|
| **`bridge`** | Docker's default — container ports mapped to host ports. Enables dynamic port mapping (below) |
| **`host`** | The container shares the host's network stack directly. Best performance, no mapping, but **port conflicts** and no per-task security group |
| **`awsvpc`** | Each task gets its own ENI, private IP, and security group (same as Fargate). Cleanest security model, but **capped by the ENI-per-instance limit** — raise it with ENI trunking |
| `none` | No external networking |

**❗ Dynamic port mapping — the classic EC2-launch-type question.** In `bridge` mode, set **`hostPort: 0`** (or omit it) and Docker assigns a random ephemeral host port; **ECS then registers that specific port with the ALB target group automatically**. This is what lets you run **several copies of the same container on one instance** — with a static `hostPort` the second task can't be placed, because the port is taken.

The catch: the instance's security group must allow the **ephemeral range 32768–65535** from the ALB's security group, not just port 80. This is the answer to *"how do you run three replicas of a service on one EC2 host behind an ALB?"* — and it's a genuine advantage of `bridge` over `awsvpc` for high task density.

**❗ Container instance draining — and don't confuse it with connection draining.** Before terminating an instance, set it to **`DRAINING`**: ECS stops placing new tasks there and gracefully relocates the running ones elsewhere, respecting the service's `minimumHealthyPercent`. Wire it to an **ASG lifecycle hook** on terminate so scale-in, AMI refreshes, and Spot interruptions don't just kill running tasks.

That is a *different layer* from **[ALB connection draining / deregistration delay](#connection-draining--deregistration-delay)**, which is about letting in-flight HTTP requests finish. A graceful deploy needs **both**: container instance draining to move the tasks, and deregistration delay to let their requests complete.

**When to choose EC2 over Fargate:**
- **GPU** workloads, or specific host hardware
- **Sustained high utilisation** — cheaper per vCPU, especially with Savings Plans or Spot
- **Privileged containers**, custom `sysctls`/kernel parameters, or host device access
- **A per-host daemon** (one log/monitoring agent per instance rather than a sidecar per task)
- **Ephemeral storage beyond Fargate's 200 GB**, or specific EBS volume requirements
- **Per-host software licensing**, or existing Reserved Instances to absorb
- Very high task density, where Fargate's per-task premium accumulates

**What you take on in exchange** — state this honestly, because it's the actual trade: patching and rotating the ECS-optimised AMI, keeping the agent current, capacity headroom and bin-packing decisions, instance draining on scale-in, and cluster-level monitoring. Fargate's higher per-vCPU price is buying all of that away.

**ECS Anywhere** is the same control plane extended to **your own on-prem servers**, registered as container instances via the SSM agent — for hybrid or data-residency requirements. Note the limitation: external instances get no ELB integration and no `awsvpc` networking.

**The interview answer:** *"Fargate unless something forces EC2 — GPU, privileged containers, host daemons, or sustained utilisation where the per-vCPU price wins. If I'm on EC2 I'd use a capacity provider with managed scaling and termination protection rather than scaling the ASG myself, spread tasks across AZs, and make sure the instance role exists alongside the task and execution roles. The two details I'd verify are dynamic port mapping with the ephemeral range open to the ALB, and container instance draining wired to an ASG lifecycle hook so deploys and scale-in don't kill running tasks."*

**ECS vs EKS in one line:** ECS is simpler, AWS-proprietary, free control plane — pick it unless you have a real reason. EKS is managed **Kubernetes** — pick it for portability across clouds, an existing Kubernetes investment/team, or the CNCF ecosystem (Helm, operators, service meshes), and accept the added complexity and control-plane cost.

### ECR (Elastic Container Registry)

AWS's managed private Docker registry — images stored in S3 under the hood, access controlled by **IAM** (plus repository policies for cross-account), and integrated with ECS, EKS, and **Lambda container images**.

```bash
aws ecr get-login-password --region us-east-1 | docker login --username AWS \
  --password-stdin 123456789012.dkr.ecr.us-east-1.amazonaws.com
docker tag myapi:1.0 123456789012.dkr.ecr.us-east-1.amazonaws.com/myapi:1.0
docker push          123456789012.dkr.ecr.us-east-1.amazonaws.com/myapi:1.0
```
Features worth naming as best practice:
- **Image scanning** — *basic* scans on push against the CVE database; **enhanced scanning** uses **Amazon Inspector** for continuous rescanning of both OS and language-package dependencies as new CVEs are published. Enhanced is the right answer for anything production.
- **Tag immutability** — prevents overwriting an existing tag. Turn it on: mutable tags mean `myapi:1.0` can silently become different bits, which destroys reproducibility and makes rollback unreliable. Deploy by **digest** or immutable version tags, never `:latest`.
- **Lifecycle policies** — expire untagged images and keep only the last N tagged ones. Without this, ECR quietly becomes one of your larger storage bills.
- **Cross-region/cross-account replication** — so a multi-region deployment doesn't pull across regions at scale.
- **Pull-through cache** — caches upstream public images (Docker Hub, ECR Public, MCR) in your registry, avoiding Docker Hub rate limits in CI. A very practical thing to mention.
- **ECR Public Gallery** for public images.

### Hands-On: ECS with Fargate

```bash
# 1. Registry + image
aws ecr create-repository --repository-name myapi --image-tag-mutability IMMUTABLE \
  --image-scanning-configuration scanOnPush=true
# (build, tag, push as above)

# 2. Cluster
aws ecs create-cluster --cluster-name prod-cluster

# 3. Task definition (family + the two roles + awsvpc + awslogs)
aws ecs register-task-definition --cli-input-json file://taskdef.json

# 4. Service behind an ALB target group, across 3 AZs
aws ecs create-service --cluster prod-cluster --service-name myapi-svc \
  --task-definition myapi:1 --desired-count 2 --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-a,subnet-b,subnet-c],securityGroups=[sg-app],assignPublicIp=DISABLED}" \
  --load-balancers targetGroupArn=arn:...,containerName=myapi,containerPort=8080 \
  --deployment-configuration "deploymentCircuitBreaker={enable=true,rollback=true}"

# 5. Verify / debug
aws ecs describe-services --cluster prod-cluster --services myapi-svc \
  --query 'services[0].{running:runningCount,desired:desiredCount,events:events[0:3]}'
aws ecs execute-command --cluster prod-cluster --task <id> --container myapi \
  --interactive --command "/bin/sh"      # ECS Exec — shell into a running task
```
**Debugging order when tasks won't start** (a good practical answer): `describe-services` **events** first, then the **stopped task's `stoppedReason`**. In practice it's almost always one of — the **task execution role** can't pull from ECR or write logs, the image architecture doesn't match (an ARM image on x86 or vice versa), the **health check** fails before the app finishes starting (raise the ALB health-check grace period), no route to ECR from a private subnet (needs a NAT gateway or **VPC endpoints for ECR + S3**), or the container exits immediately because of a missing environment variable.

### [new content] ECS vs EKS vs Fargate vs Lambda for .NET Workloads

The original notes cover Lambda-vs-ECS and EC2-vs-Fargate individually but never directly answer the very common senior .NET-on-AWS question: *"You're moving a .NET microservices platform to AWS — how do you choose the compute layer?"*

| Criterion | Lambda | ECS (Fargate) | EKS (Fargate or managed nodes) | EC2 (self-managed) |
|---|---|---|---|---|
| .NET fit | Great for event handlers, APIs with ASP.NET Core minimal hosting via `Amazon.Lambda.AspNetCoreServer`, but cold starts hurt latency-sensitive sync APIs unless AOT + Provisioned Concurrency | Great — standard container deployment, no code changes, full ASP.NET Core hosting model | Same as ECS but adds Kubernetes complexity — only worth it if you already run K8s elsewhere (multi-cloud, existing manifests, team expertise) | Full control, good for Windows containers/.NET Framework (legacy) workloads needing IIS |
| Operational overhead | Lowest | Low (no cluster to manage) | Highest (control plane concepts, CRDs, Helm, networking) | Highest (patching, scaling, OS) |
| Team skill fit | Any .NET team | Any .NET/DevOps team | Needs existing K8s expertise | Needs sysadmin/infra expertise |
| Cost at steady state | Poor (per-invocation billing adds up) | Good | Good | Best (if fully utilized) |
| .NET Framework (not Core) support | No (Lambda requires .NET Core/5+) | Yes, via Windows containers on EC2 launch type (not Fargate) | Yes, Windows node groups | Yes |
| Startup-latency-sensitive sync API | Risky without Provisioned Concurrency + AOT | Best default choice | Best default choice | Best default choice |
| Best use case | Webhooks, S3/SQS/DynamoDB Stream processors, cron, glue | Line-of-business APIs, internal microservices | Only if already multi-cloud/K8s-standardized | Legacy .NET Framework/IIS lift-and-shift |

**Senior-level recommendation pattern:** default new .NET microservices to **ECS on Fargate** (best balance of operational simplicity and control for a typical .NET shop). Use **Lambda** for event-driven glue and spiky/idle-heavy workloads. Reach for **EKS** only when there's an existing organizational Kubernetes investment — introducing K8s purely for a .NET-on-AWS migration is usually over-engineering. Use **EC2** (with Windows containers or full Windows Server) only for .NET Framework workloads that can't be ported to .NET Core/8+, or for workloads needing GPU/specialized hardware.

**Interviewer follow-up to expect:** "Your team knows only .NET/C#, no Kubernetes — would you still pick EKS for a green-field microservices platform?" Correct senior answer: no — pick ECS/Fargate unless there's a concrete multi-cloud or portability requirement that justifies the K8s learning curve and operational tax.

---

### [gaps] Fargate/ECS/EKS Trade-offs — Reasoning Without Hands-On Time

Honesty framing up front, since this is worth stating explicitly in an interview: my hands-on AWS provisioning experience (via Terraform/CDKTF) is Lambda, DynamoDB, EC2, and S3 — I haven't personally run Fargate, ECS, or EKS in production. What follows is how I'd reason through the trade-offs if asked to make this decision, not a claim of direct operational experience with the container orchestrators themselves.

**Expanded comparison — operational overhead, cost model, cold start, and use-case fit**

| Dimension | EC2 (self-managed) | ECS on Fargate | EKS (managed control plane) | Lambda |
|---|---|---|---|---|
| Who patches the OS/kernel | You | AWS | AWS (nodes) or you (self-managed node groups) | AWS (fully abstracted) |
| Who manages the orchestrator/control plane | N/A (no orchestrator) or you (self-hosted) | AWS (ECS control plane is free, always managed) | AWS manages the control plane (charged hourly per cluster), but you still manage node groups unless using Fargate profiles | N/A |
| Cost model | Instance-hour, regardless of load | Per-task vCPU/GB-second, while running | Cluster fee + node/Fargate task cost | Per-invocation + duration |
| Cold start behavior | None once running; ASG scale-out takes minutes (boot OS, attach storage, register) | Task startup is seconds (pull image, start container) — no "cold start" in the Lambda sense, but not instant either | Similar to Fargate for Fargate-backed pods; for managed EC2 node groups, bounded by node/ASG scale-out time like raw EC2 | True cold start on first/scaled-out invocation (ms–low seconds), mitigated by Provisioned Concurrency |
| Operational overhead | Highest — patching, scaling policy tuning, capacity planning | Low — no servers, no cluster; you manage task definitions/services | Highest of the container options — cluster upgrades, CRDs, networking plugins (CNI), Helm charts, IAM-to-Kubernetes-RBAC mapping (IRSA) | Lowest — no infrastructure concept at all |
| Best use-case fit | Steady, high utilization; OS-level requirements; legacy/stateful; GPU | Standard containerized microservices with no existing K8s investment | Organizations already standardized on Kubernetes (often multi-cloud, or migrating from on-prem K8s) | Event-driven, spiky, short-lived work |
| Multi-cloud portability | Low (AWS-specific tooling even if OS is portable) | Low (ECS is AWS-proprietary) | High (Kubernetes API is portable across clouds/on-prem) | Lowest (heavily AWS-event-model-coupled) |

**How I'd frame the trade-off conversation if asked "why not EKS for everything, since Kubernetes is the industry standard":**

Kubernetes' biggest selling point — portability and a rich ecosystem (Helm, operators, service mesh) — is also its biggest cost: a real control-plane learning curve (CRDs, RBAC, networking/CNI, admission controllers) that a pure ECS or Fargate user never has to pay. For a .NET shop without existing Kubernetes investment, that operational tax usually isn't justified unless there's a concrete multi-cloud requirement or the org already has platform engineers who live in Kubernetes daily. I'd frame my recommendation the same way AWS itself frames the ECS-vs-EKS choice: ECS if you want AWS-native simplicity and don't need portability; EKS if you need Kubernetes-API compatibility for tooling, multi-cloud strategy, or existing team expertise.

**A decision flow I'd talk through out loud in an interview:**

```
   Existing Kubernetes investment or multi-cloud requirement?
                              |
              +---------------+---------------+
             yes                              no
              |                               |
              v                               v
            EKS            Need OS-level access, GPU,
                           or a legacy/stateful app?
                                          |
                          +---------------+---------------+
                         yes                              no
                          |                               |
                          v                               v
                        EC2          Spiky / event-driven, short-lived,
                                     idle-heavy?
                                                    |
                                    +---------------+---------------+
                                   yes                             no
                                    |                    (steady containerised
                                    v                       service)
                                 LAMBDA                          |
                                                                 v
                                                         ECS on FARGATE
```

**What I'd want to learn hands-on before claiming deep Fargate/ECS/EKS expertise:** task definition/service tuning under real load (deployment circuit breakers, min/max healthy percent during rolling deploys), service discovery (Cloud Map/App Mesh), and — for EKS specifically — IRSA (IAM Roles for Service Accounts) mapping and cluster upgrade mechanics. Naming this gap directly, rather than overstating familiarity, is itself the senior-level move here.

---

### [new content] Deploying .NET to AWS: Elastic Beanstalk vs ECS vs Lambda Custom Runtime

The original notes never directly discuss Elastic Beanstalk, despite it being a common AWS Certified/senior-interview topic and a legitimate, low-effort .NET deployment path.

| Option | What it is | Pros | Cons | When to use |
|---|---|---|---|---|
| **Elastic Beanstalk** | PaaS wrapper around EC2/ASG/ELB/RDS with a managed platform (incl. .NET on Windows/Linux) | Fastest path from `dotnet publish` to a running, load-balanced, auto-scaled app; AWS manages the underlying EC2/ASG/ELB stack; supports blue/green via environment swap | Less control than raw ECS; platform upgrades can be disruptive; "PaaS lock-in" feel; scaling granularity coarser than ECS | Small-to-mid teams wanting managed infra without container/K8s investment; quick MVPs; teams new to AWS |
| **ECS (Fargate or EC2 launch type)** | Container orchestration, AWS-native | Full control over container spec, fine-grained scaling, no idle cost (Fargate), integrates cleanly with CodePipeline/CodeBuild | Requires Docker packaging discipline, task definition management | Default choice for most modern .NET microservices |
| **Lambda (ASP.NET Core minimal API hosting or custom runtime)** | Serverless — package as zip or container image | No infra at all, scales to zero, cheap for spiky traffic | Cold starts, 15-min limit, harder local debugging parity, connection pooling nuances (RDS Proxy often needed) | Event-driven APIs, low/spiky traffic, backend-for-frontend functions |

**Nuance interviewers probe for:** Elastic Beanstalk is *not* a separate compute primitive — under the hood it still provisions EC2 + ASG + ELB (or ECS, for the Docker platform). The value-add is the deployment/orchestration tooling (`eb deploy`, environment configs, rolling/immutable/blue-green deployment policies), not a new runtime. Knowing this distinction (Beanstalk = orchestration layer, not new infrastructure) is what separates a mid-level from a senior answer.

**Deployment strategy comparison (all three support some form of zero/low-downtime deploy):**
- Elastic Beanstalk: rolling, rolling-with-additional-batch, immutable, or blue/green (swap CNAME between environments).
- ECS: rolling update via service deployment configuration, or blue/green via CodeDeploy + two target groups.
- Lambda: versions + aliases, with linear/canary traffic shifting via CodeDeploy.

---

## Relational Databases, Caching & Analytics

> **Tier 2 — reason about, be honest about hands-on.** RDS/Aurora are not part of my hands-on AWS experience (SQL Server on-prem and Cosmos DB are) — see the framing note in [Multi-AZ vs Read Replica](#gaps-multi-az-vs-read-replica--the-1-confused-pair). Analytics services here are Tier 3: know the shape and when to reach for them.

### [new content] RDS Multi-AZ vs Read Replicas vs Aurora

The original notes never covered RDS despite it being one of the most common .NET-on-AWS database choices (SQL Server/PostgreSQL/MySQL via RDS is far more common for .NET shops than DynamoDB for primary OLTP workloads) — this is a material gap for a senior interview.

| Feature | Multi-AZ (standby) | Read Replica | Aurora (Multi-AZ cluster) |
|---|---|---|---|
| Purpose | High availability / DR | Read scalability, offload reporting | HA + scalability, AWS-native distributed storage |
| Replication | Synchronous to standby | Asynchronous | Semi-synchronous within storage layer |
| Standby usable for reads? | No (classic Multi-AZ) — **Multi-AZ DB Cluster** (newer) does allow reader endpoints | Yes — that's its purpose | Yes, via reader endpoint |
| Failover | Automatic (typically 60–120s) | Manual promotion (breaks replication) | Automatic, typically faster (<30s) |
| Cross-region | No (classic Multi-AZ is single-region) | Yes (cross-region read replicas supported) | Yes (Aurora Global Database) |
| .NET connection string implication | App connects to one endpoint; failover is transparent (DNS-based) but requires connection retry logic (Polly, EF Core resiliency) | App must route read-only queries to replica endpoint explicitly (read/write splitting in code or via a proxy) | Similar — reader/writer endpoints, RDS Proxy recommended for connection pooling across failover |

**Interview nuance to land:** Multi-AZ is for **availability**, not scalability — the standby doesn't serve traffic in classic Multi-AZ. Read replicas are for **scaling reads**, not HA — promoting one is a manual, replication-breaking operation and shouldn't be your primary DR plan. A senior answer distinguishes these instead of conflating "Multi-AZ" and "read replica" as interchangeable resilience mechanisms — a very common junior-level confusion this fills.

**RDS Proxy** (worth name-dropping): pools and multiplexes connections in front of RDS/Aurora — critical for Lambda-to-RDS patterns where each concurrent execution environment would otherwise open its own DB connection and exhaust the database's max connection limit under burst concurrency.

### [gaps] Multi-AZ vs Read Replica — The #1 Confused Pair

Framing note: RDS is not part of my confirmed hands-on AWS experience (Lambda, DynamoDB, EC2, and S3 via Terraform/CDKTF are) — the following is conceptual/comparative knowledge I'd bring to a discussion of relational database strategy on AWS, not a claim of having operated RDS/Aurora in production myself.

This pairing is, by a wide margin, the most commonly confused RDS concept at every seniority level, so it earns a dedicated, drill-style callout beyond the comparison table above.

**The mistake, stated plainly:** candidates (and even some production architectures) treat "Multi-AZ" and "Read Replica" as if either one gives you both high availability *and* read scaling. They don't — each does exactly one of those two jobs, and reaching for the wrong one is a real, recurring production design mistake.

```
                    What problem are you solving?
                                 |
      +--------------------------+--------------------------+
      |                                                     |
 "I need the DB to survive                    "My read traffic is
  an AZ failure"                               overwhelming one instance"
      |                                                     |
      v                                                     v
+--------------------------+              +--------------------------+
|    MULTI-AZ STANDBY      |              |      READ REPLICA        |
+--------------------------+              +--------------------------+
| Synchronous replication  |              | Asynchronous replication |
|                          |              |                          |
| Standby is NOT readable  |              | Fully readable -- that    |
| (classic Multi-AZ)       |              | IS its entire purpose    |
|                          |              |                          |
| Automatic failover,      |              | Promotion is MANUAL and  |
| typically 60-120s        |              | breaks replication       |
|                          |              |                          |
| x Does NOT help with     |              | x Does NOT give you      |
|   read scaling           |              |   automatic HA failover  |
+--------------------------+              +--------------------------+

          Two mechanisms, two different problems.
          Using a read replica as your DR plan, or expecting a
          Multi-AZ standby to absorb read load, is the same mistake.
```

| | Multi-AZ (Standby) | Read Replica |
|---|---|---|
| Solves | Availability / disaster recovery | Read throughput / reporting offload |
| Does **not** solve | Read scaling (classic Multi-AZ standby serves no traffic) | Automatic HA (promotion is manual and breaks the replication link) |
| Replication mode | Synchronous | Asynchronous |
| Can you query the secondary? | No, in classic Multi-AZ (the newer **Multi-AZ DB Cluster** feature does add readable reader endpoints — know this distinction, it's a common "gotcha, that changed" follow-up) | Yes — that's the entire point |
| What happens on primary failure? | Automatic failover to standby, transparent via the same DNS endpoint | Nothing automatic — you must manually promote a replica, and promotion permanently breaks its replication relationship to the old primary |
| Can it span regions? | No (classic Multi-AZ is single-region only) | Yes — cross-region read replicas are explicitly supported |

**The drill answer to have ready verbatim:** "Multi-AZ is about *surviving failure* — it's a synchronous standby that AWS fails over to automatically, but in the classic (non-cluster) form it doesn't serve any read traffic, so it does nothing for scaling. Read Replicas are about *scaling reads* — asynchronous copies you explicitly route reporting/read traffic to, but promoting one to primary is a manual, replication-breaking operation, so it's not a substitute for real HA. Using a read replica as your DR plan, or expecting a Multi-AZ standby to absorb read load, are both the same category of mistake: conflating two mechanisms that solve different problems."

**Aurora, introduced more fully:** Aurora is AWS's own MySQL- and PostgreSQL-compatible relational engine (not a distinct SQL dialect — client drivers/ORMs like EF Core's Npgsql or MySQL providers work against it unchanged). Its key architectural difference from standard RDS is that replication happens at the **storage layer**, not by shipping database logs between full instances — Aurora separates compute from a shared, distributed, auto-scaling storage volume that replicates across AZs beneath the engine. This is why Aurora replica lag is typically much lower than standard RDS read-replica lag — commonly cited as sub-10-seconds, and often near-instant in practice — though exact lag is workload-dependent and should be treated as directional rather than a guaranteed number. Aurora also auto-scales storage (no manual volume resizing the way standard RDS requires) and supports both a Multi-AZ cluster mode (fast automatic failover, typically well under the classic Multi-AZ failover window) and Aurora Global Database (cross-region, for the Warm Standby/Active-Active DR strategies covered later in this guide).

**How I'd frame Aurora vs standard RDS if asked which I'd choose:** Aurora generally wins when you want RDS-compatible tooling/ORM support but with better availability characteristics, faster failover, and less manual storage management — at a higher cost per compute unit than equivalent standard RDS. Standard RDS (or RDS for SQL Server specifically, which Aurora does not support — Aurora is MySQL/PostgreSQL-compatible only) remains the right call for SQL Server-based .NET shops, or when Aurora's cost premium isn't justified by the workload's availability/scale needs.

### Databases & Analytics Overview: Choosing the Right Store

AWS deliberately offers **purpose-built** databases rather than one general-purpose engine. The interview question is almost never "what is DynamoDB?" — it's "which would you pick, and why?"

| Category | Service | Pick it when |
|---|---|---|
| **Relational (OLTP)** | RDS (SQL Server, PostgreSQL, MySQL, MariaDB, Oracle), **Aurora** | You need joins, transactions, referential integrity, ad-hoc queries, or an existing EF Core/ORM codebase |
| **Key-value / document (NoSQL)** | **DynamoDB**, DocumentDB (MongoDB-compatible) | Known access patterns, single-digit-ms latency, huge or spiky scale, no complex joins |
| **In-memory cache** | **ElastiCache** (Redis / Memcached), MemoryDB | Sub-millisecond reads, session state, leaderboards, relieving read pressure on a database |
| **Data warehouse (OLAP)** | **Redshift** | Complex analytical queries over TB–PB of structured data, BI dashboards |
| **Query-in-place** | **Athena** | Ad-hoc SQL directly over S3 with no infrastructure at all |
| **Search** | OpenSearch Service | Full-text search, log analytics, observability dashboards |
| **Graph** | Neptune | Relationships are the primary query: social graphs, fraud rings, recommendations |
| **Time series** | Timestream | IoT/metric data with time-based rollups and retention tiers |
| **Ledger** | QLDB | Cryptographically verifiable, immutable transaction history |
| **Wide-column** | Keyspaces (Cassandra) | Existing Cassandra workloads |

**OLTP vs OLAP is the framing to lead with:** OLTP is many small, concurrent, indexed reads/writes (an order-entry API → RDS/DynamoDB); OLAP is few large scans and aggregations over history (a revenue dashboard → Redshift/Athena). Running analytical queries against your OLTP primary is the classic architectural mistake — the fix is a read replica for light reporting, or a proper warehouse/lake for real analytics.

### Relational Databases & RDS — The Operational Surface

**Relational fundamentals interviewers still ask about:** tables/rows/columns with a fixed schema; **primary and foreign keys** enforcing referential integrity; **normalisation** to remove duplication (vs the deliberate denormalisation DynamoDB requires); **ACID** guarantees (Atomicity, Consistency, Isolation, Durability); **indexes** to avoid full scans; and **joins** to combine tables at query time. The contrast to draw: relational databases optimise for *flexible querying* of normalised data, NoSQL optimises for *known access patterns* on denormalised data.

**What RDS manages vs what stays yours** — this is the shared-responsibility answer for databases:
| AWS handles | You still handle |
|---|---|
| OS and database engine patching (in your maintenance window) | **Schema design, indexes, and query tuning** |
| Automated backups, snapshots, point-in-time recovery | Choosing retention, and **testing that restores work** |
| Multi-AZ failover, replica provisioning | Deciding Multi-AZ vs replicas (see above), and **connection retry logic** |
| Hardware, storage, and monitoring infrastructure | Instance sizing, storage type, and cost |
| Encryption capability | Enabling encryption at creation, and key management |
| — | **No OS/shell access at all** — you cannot install an agent or a custom extension |

That last row is the key limitation: RDS gives you no host access. When you genuinely need it (a legacy Oracle/SQL Server setup requiring custom binaries), the answer is **RDS Custom** or self-managing the engine on EC2.

**The operational features worth naming:**
- **Automated backups** — daily full snapshot plus continuous transaction logs, enabling **point-in-time recovery (PITR)** to any second within the retention window (1–35 days). Retention `0` disables them. Automated backups are **deleted when you delete the instance** unless you take a final snapshot.
- **Manual snapshots** — kept until *you* delete them, and shareable across accounts/regions. The distinction "automated backups expire, manual snapshots don't" is a standard question.
- **Maintenance window** — when AWS applies patches; may cause a brief failover on Multi-AZ (which is why Multi-AZ makes patching near-transparent).
- **Storage autoscaling** — RDS grows the volume automatically when you approach the threshold, preventing the "database is full at 2 a.m." outage.
- **Read replicas** — up to 5 (15 for Aurora), can be **cross-region**, and can be promoted to standalone.
- **Encryption** — at rest via KMS (must be enabled **at creation**; to encrypt an existing instance you snapshot → copy the snapshot encrypted → restore), in transit via TLS. **IAM database authentication** lets an application authenticate with an IAM token instead of a stored password — the natural pairing with an EC2/Lambda role, and a strong answer to "how do you avoid database passwords?" (the other being **Secrets Manager** with rotation).
- Always place RDS in **private subnets** with a security group referencing only the app tier's SG.

#### RDS Custom — for Oracle & SQL Server

**The problem it solves.** Standard RDS gives you **no OS access and no DB superuser**, which is fine until a workload genuinely needs them. Typical blockers, and they're common in .NET/enterprise estates:
- SQL Server features that live outside the database engine — **SSIS / SSRS / SSAS**, linked servers, custom **CLR assemblies**, filesystem access for BULK INSERT
- Oracle features needing `SYS` — Data Guard, APEX, custom patches, specific PSU levels
- Third-party monitoring or backup agents that must be **installed on the host**
- A vendor application that only certifies against a specific OS/patch combination

**What RDS Custom is:** the deliberate middle ground.
| | **RDS** | **RDS Custom** | **DB on EC2** |
|---|---|---|---|
| OS access (SSH/RDP, sudo/admin) | ❌ | ✅ | ✅ |
| DB superuser (`SYS`, `sa`) | ❌ | ✅ | ✅ |
| Install host agents / native features | ❌ | ✅ | ✅ |
| Automated backups, PITR, Multi-AZ | ✅ AWS | ✅ AWS, **conditionally** | ❌ you build it |
| Engine/OS patching | ✅ AWS | **You initiate** | ❌ you |
| Engines | All | **Oracle and SQL Server only** | Anything |

**❗ The concept that makes it distinctive — the support perimeter.** AWS automation keeps working *as long as you stay inside a supported configuration*. Change something outside it (break the agent, alter the storage layout, remove required IAM permissions) and the instance moves to **`unsupported-configuration`**: automation stops, backups may halt, and **fixing it is your job**. So the trade isn't "managed plus root access" — it's "managed *until you break it*".

**Automation pause** is the companion feature: you can suspend RDS Custom automation (default 60 minutes, up to **24 hours**) to perform host maintenance without RDS "correcting" your changes mid-flight. Naming this shows real familiarity.

**Setup prerequisites worth knowing:** it needs an **IAM instance profile**, an S3 bucket for artifacts, and — notably — a **customer-managed KMS key** (an AWS-managed key won't do). SQL Server and Oracle both use a **Custom Engine Version (CEV)** so the exact build is pinned and reproducible.

**How to choose:** *"Standard RDS by default. **RDS Custom** when I need OS or superuser access but still want AWS handling backups, PITR, and Multi-AZ — accepting that I own anything I break. **Database on EC2** only when even RDS Custom's supported configuration is too restrictive, or the engine isn't Oracle/SQL Server."* For a .NET shop the trigger is usually SSIS/SSRS, CLR, or a per-core licensing arrangement.

#### RDS Security — Consolidated

The bullets above cover pieces of this; here it is as the layered answer to *"how do you secure RDS?"*

**1. Network — the layer that prevents the headline breach.** Private subnets only, a **DB subnet group across ≥2 AZs**, and a security group whose inbound rule references the **app tier's SG rather than a CIDR**. Critically, **`PubliclyAccessible = false`**: a publicly accessible instance plus a permissive SG is exactly how databases end up exposed to the internet. Reach it for admin work via **[Session Manager port forwarding](#aws-systems-manager-ssm)**, not a public endpoint.

**2. Encryption at rest** — KMS, and it **must be enabled at creation**. To encrypt an existing instance: snapshot → **copy the snapshot with encryption** → restore. It covers the instance, automated backups, snapshots, and read replicas. Sharing an **encrypted** snapshot cross-account requires a **customer-managed** key; sharing an *unencrypted* one publicly is a genuine leak vector.

**3. Encryption in transit** — enforce rather than hope: `rds.force_ssl=1` (PostgreSQL) or `require_secure_transport=ON` (MySQL). Clients must trust the **RDS CA bundle**; in .NET that means `Encrypt=True` **without** `TrustServerCertificate=true` for SQL Server, or `SSL Mode=Require` for Npgsql. See [Encryption in Transit](#encryption-in-transit-tls--end-to-end).

**4. Authentication — three options, in increasing strength:**
| Option | Notes |
|---|---|
| Master password in config | ❌ Never |
| **Secrets Manager** with rotation | Good — but read the [rotation pitfalls](#secrets-manager--pitfalls) |
| **IAM database authentication** | ✅ Strongest: a 15-minute token generated from the app's role, no stored password at all. Caveat — it has a **connection-rate limit**, so pair it with **[RDS Proxy](#rds-proxy)** for high-churn or Lambda workloads |
| **Kerberos / AWS Managed Microsoft AD** | For SQL Server domain authentication |

**5. Authorization inside the database is still yours.** AWS controls who can *reach* the database; it has no opinion on your schema grants. The app should use a least-privilege database user — **not `sa`, not the RDS master user** — and that's a very common real-world gap.

**6. Auditing — know which tool answers which question:**
- **CloudTrail** logs the **API** (`CreateDBInstance`, `ModifyDBInstance`, `DeleteDBInstance`) — *not* the SQL anyone ran.
- **Engine logs** (error, slow query, audit, general) can be **published to CloudWatch Logs** for retention, metric filters, and alarms.
- **Database Activity Streams** (Aurora, plus RDS for Oracle/SQL Server) give a near-real-time, **tamper-resistant** stream of database activity to Kinesis — the point being that even a DBA with full privileges cannot erase their own trail. That's the answer to "how do you audit *privileged* database access?"

**7. Patching and lifecycle** — AWS patches the engine in your **maintenance window**; you own the window choice and the `AutoMinorVersionUpgrade` flag. Enable **deletion protection** and always take a **final snapshot**; back the whole thing with **AWS Backup** so retention is policy-driven rather than per-instance.

**8. Performance Insights** deserves a security note: it can surface **query text**, which may contain sensitive literals — it has its own KMS key and IAM permissions, so treat access to it as data access.

**One-liner:** *"Private subnets and SG-to-SG rules so it's unreachable from the internet; KMS at creation and forced TLS so the data is encrypted both ways; IAM database auth or Secrets Manager so there's no password in config; least-privilege database users inside; and Database Activity Streams plus engine logs in CloudWatch so privileged access is auditable. CloudTrail tells me who changed the instance — it does not tell me who ran the query."*

### Athena

**Serverless, interactive SQL directly over data in S3.** No servers, no clusters, no loading — you define a table over S3 objects and query them where they sit (Presto/Trino under the hood).

- **Pricing is per TB of data scanned** (~$5/TB), which makes cost a *query-design* problem, not an infrastructure problem. Three levers cut it dramatically:
  1. **Columnar formats** — Parquet/ORC instead of CSV/JSON, so only the columns you select are read (often 10× less scanned).
  2. **Partitioning** — lay data out as `s3://bucket/logs/year=2026/month=08/day=08/` so a `WHERE year=2026 AND month=08` clause skips everything else. **Partition projection** avoids the metadata lookup entirely for time-series layouts.
  3. **Compression** (Snappy/GZIP) and avoiding `SELECT *`.
- Schema comes from the **AWS Glue Data Catalog** (a Glue crawler can infer it automatically).
- **Federated queries** can reach beyond S3 into RDS/DynamoDB/CloudWatch via connectors.
- Results land back in an S3 "query results" bucket — remember a lifecycle rule on it.

**Athena vs Redshift** — the standard comparison: Athena is serverless, pay-per-query, best for **ad-hoc and infrequent** analysis over a data lake with no ETL; Redshift is a provisioned (or serverless) **warehouse** with its own optimised storage, best for **frequent, complex, high-concurrency BI** where sustained query volume makes a cluster cheaper and faster. "Occasional queries over S3 logs → Athena; a dashboard hundreds of analysts hit all day → Redshift."

**Where Athena shows up in practice:** querying **CloudTrail** logs ("who deleted that bucket?"), **VPC Flow Logs**, ALB/S3 access logs, and cost/usage reports. Naming those makes the answer concrete.

**The rest of the analytics family in one line each:** **Glue** — serverless ETL plus the Data Catalog every other service reads. **Redshift** — the warehouse (Redshift Serverless removes the cluster sizing decision; Spectrum queries S3 directly). **EMR** — managed Hadoop/Spark for heavy custom processing. **QuickSight** — serverless BI dashboards with SPICE in-memory acceleration, the visualisation layer over Athena/Redshift. **Kinesis Data Analytics / Managed Flink** — SQL/Flink over streaming data. **Lake Formation** — permissions and governance over a data lake.

### RDS Proxy

A fully managed **connection pool** that sits between your application and RDS/Aurora.

**The problem it solves:** a relational database has a hard `max_connections` limit and each connection is expensive (memory, a backend process). **Lambda** is the pathological case — every concurrent execution environment opens its own connection, so a burst to 1,000 concurrent executions tries to open 1,000 connections and the database refuses them all. Traditional connection pooling doesn't help, because each Lambda sandbox has its own pool of one.

**What RDS Proxy gives you:**
- **Pools and multiplexes** connections, so hundreds of clients share a small number of real database connections.
- **Cuts failover time by up to ~66%** — the proxy holds client connections open and re-points them at the new primary, so the application often doesn't see an error at all instead of waiting on DNS propagation and retrying.
- **Enforces IAM authentication** and pulls credentials from **Secrets Manager**, so no database password ever appears in application config.
- Runs **inside your VPC** and is never publicly accessible.

**When to reach for it:** Lambda-to-RDS (almost always), any application with many short-lived connections, and anywhere you want faster, quieter failover. **When not to:** a single long-lived container app that already pools connections well gets little benefit for the extra hop and cost. For .NET specifically, note that ADO.NET/EF Core already pool per process — so RDS Proxy is about *cross-process* pooling and failover smoothing.

#### Pattern: Lambda → RDS Proxy → RDS — Full Explanation

**What the three pieces are:**
| Piece | What it is | The relevant property |
|---|---|---|
| **Lambda** | Serverless compute; each concurrent invocation runs in **its own execution environment** (its own process) | Scales to hundreds or thousands of environments in seconds, each one short-lived |
| **RDS / Aurora** | A managed relational database | Has a **hard `max_connections` ceiling**, and each connection is expensive — PostgreSQL forks a **process per connection** (~5–10 MB each) |
| **RDS Proxy** | A managed connection pool **inside your VPC**, between the two | Holds a small pool of real DB connections and **multiplexes** many client connections onto them |

**Why Lambda + RDS breaks without a proxy — the execution model is the whole answer.**

A normal ASP.NET Core app on EC2/ECS is **one process** with an ADO.NET pool of ~100 connections, reused across thousands of requests. Efficient, because the pool is shared.

Lambda breaks that assumption: **each execution environment handles one invocation at a time and has its own private pool.** So a pool of 100 inside a Lambda is meaningless — it only ever needs one connection, but there are now *N* separate pools where N = your concurrency.

```
CONCURRENCY 500  ->  500 execution environments  ->  500 separate connections
db.t3.medium PostgreSQL max_connections ≈ 420
                        ↓
   FATAL: too many connections / remaining connection slots are reserved
```
And it's not just the ceiling — it's **churn**. Environments are constantly created and destroyed, so you get a storm of connect/TLS-handshake/disconnect cycles, each costing the database real CPU. The database spends its time managing connections instead of answering queries.

**What the proxy changes:**
```
   BEFORE                                AFTER
   Lambda x500                           Lambda x500
      |                                     |
      | 500 connections                     | 500 connections
      v                                     v
   +--------+                          +-------------+
   |  RDS   |  <- refuses them         | RDS PROXY   |  warm pool + multiplexing
   +--------+                          +------+------+
                                              | ~20-50 real connections
                                              v
                                          +--------+
                                          |  RDS   |  comfortable
                                          +--------+
```
Lambda points at the **proxy endpoint** instead of the DB endpoint. That's the only application change.

**❗ Connection pinning — the gotcha that decides whether the proxy actually helps.** Multiplexing only works while a session is **stateless**. If a session does something session-scoped, the proxy must **pin** that client to one DB connection for the rest of the session, and you lose the sharing benefit for it. Common causes:
- Explicit transactions held open across statements
- `SET` session variables, temp tables, advisory locks
- Prepared statements (protocol-dependent), and `USE database` on MySQL

Watch the **`DatabaseConnectionsCurrentlySessionPinned`** CloudWatch metric. If it's high, the proxy is doing little for you and the fix is application-side — keep transactions short, avoid session state. **Naming pinning unprompted is a strong senior signal**, because it's the difference between "I've read the docs" and "I've operated this."

**Setup requirements** (the practical bits that trip deployments up):
- The **Lambda must be VPC-attached** in the same VPC as the proxy. Note this means the function loses default internet access — add a NAT gateway or **VPC endpoints** for any other AWS calls it makes. (The old cold-start penalty for VPC Lambdas was largely removed by Hyperplane ENIs, so that's no longer the objection it once was.)
- The proxy needs a **Secrets Manager secret** holding the DB credentials, plus an IAM role to read it.
- Security groups: Lambda SG → proxy SG on **5432/3306**, and proxy SG → RDS SG.
- **IAM database authentication** removes the password from the app entirely — grant the Lambda's execution role `rds-db:connect` and generate a token instead:
```csharp
var token = RDSAuthTokenGenerator.GenerateAuthToken(
    "my-proxy.proxy-abc123.us-east-1.rds.amazonaws.com", 5432, "app_user");
// use the token as the password; combine with SSL Mode=Require
```
- **Cost:** billed per vCPU of the target DB instance per hour — cheap relative to an outage, but not free, which matters for small workloads.

**When to use it — real use cases:**
1. **Serverless API on Lambda over a relational database.** The canonical case: API Gateway → Lambda → RDS/Aurora with spiky traffic. This is *the* reason RDS Proxy exists.
2. **A relational schema you can't move to DynamoDB** — joins, ad-hoc reporting, an existing EF Core model, or a legacy database other systems also read.
3. **Bursty event-driven writes** — an S3 upload or SQS batch fans out to hundreds of concurrent Lambdas that all need to write to RDS.
4. **Reducing failover blast radius for *any* client, not just Lambda** — during a Multi-AZ failover the proxy re-points connections, so ECS/EC2 apps often see no error rather than a reconnect storm.
5. **Eliminating database passwords** from application config via IAM auth + Secrets Manager rotation.

**When *not* to — and the alternatives worth naming:**
| Situation | Better answer |
|---|---|
| Greenfield, simple known access patterns | **DynamoDB** — it's an HTTP API with **no connection concept at all**, so the problem never exists. The strongest answer when the data model allows it |
| You want serverless-to-relational with *zero* connection management | **Aurora Data API** — an HTTPS/IAM endpoint for Aurora, no persistent connections, no VPC attachment needed. Trade-off: higher per-query latency and not suited to chatty workloads |
| One long-lived ECS/EC2 service | Its own in-process pool is already correct; the proxy adds a hop and cost for little gain |
| Very low Lambda concurrency (a few per minute) | May not justify the cost — though **reserved concurrency** set below your connection budget is a blunt, free mitigation that caps the damage |
| Sessions that pin heavily | Fix the application first; the proxy can't multiplex stateful sessions |

**In an interview.** This turns up in three shapes: *"How would you connect Lambda to a relational database?"*, *"Your Lambda intermittently fails with `too many connections` — diagnose it"*, and *"Why is Lambda + RDS often called an anti-pattern?"*

A model answer:
> *"The root cause is Lambda's execution model — each concurrent invocation is a separate process with its own connection, so connection count scales with concurrency and there's no shared pool to help. At a few hundred concurrent invocations you exhaust `max_connections` on a small instance, and the churn of connect/disconnect cycles costs the database real CPU on top. The fix is **RDS Proxy**, which holds a warm pool and multiplexes many client connections onto a few real ones, and as a bonus cuts failover time by re-pointing connections instead of making clients reconnect. Two caveats I'd check: **pinning** — if sessions hold transactions open or use session state, the proxy pins connections and the benefit disappears — and the fact that the Lambda has to be VPC-attached, so it needs a NAT gateway or VPC endpoints for other AWS calls. If it were greenfield and the access patterns were simple, I'd ask whether **DynamoDB** is the right store instead, since it has no connection model at all."*

**Follow-ups to be ready for:** *Why not just raise `max_connections`?* (each connection costs memory; you'd starve the DB of the RAM it needs for buffers, and you're treating a symptom). *Does the proxy help cold starts?* (not directly, but it removes the TLS+auth handshake to the database from the critical path). *What about Aurora Serverless?* (it scales compute, not the connection model — you still want the proxy, or the Data API). *How do you know pinning is happening?* (the CloudWatch metric above).

**What's actually being tested:** whether you understand that **Lambda is not a web server** — that concurrency means *processes*, not threads — rather than reciting "use RDS Proxy". Candidates who explain *why* the pooling assumption breaks always land better than those who name the service.

*Framing note for me: my Lambda work has been **DynamoDB**-backed (see [Resume Deep-Dives](#resume-deep-dives--the-follow-ups-i-should-expect)), so I have not hit this in production. I'd present it as reasoning and design knowledge, and say so — the honest version lands better than implying operational experience I don't have.*

### Aurora Advanced Features

Beyond the storage-layer architecture described above, these are the Aurora features that turn a basic answer into a strong one:

| Feature | What it does |
|---|---|
| **6 copies across 3 AZs** | Every write is replicated six ways. Aurora tolerates losing **2 copies for writes** and **3 copies for reads** with no availability impact, and self-heals bad blocks |
| **Up to 15 read replicas** | vs 5 on standard RDS, with much lower lag because replicas read the *shared* storage volume rather than replaying logs |
| **Reader / writer / custom endpoints** | The **writer endpoint** always points at the current primary (failover is transparent); the **reader endpoint** load-balances across replicas; **custom endpoints** target a chosen subset — e.g. route heavy reporting queries to two larger replicas so they can't affect the API's replicas |
| **Aurora Serverless v2** | Scales capacity in fine-grained ACUs in **under a second**, from a fraction of a unit to hundreds — for spiky, unpredictable, or dev/test workloads where a fixed instance is either too small or mostly idle. (v1 scaled slowly and paused; v2 is the version to reference) |
| **Aurora Global Database** | One primary region plus up to 5 secondary read-only regions, with **typical replication under 1 second** and cross-region failover usually **under a minute**. This is the engine behind Warm Standby / Active-Active DR — see [Disaster Recovery Strategies](#new-content-disaster-recovery-strategies) |
| **Database cloning** | A copy-on-write clone of a whole database in minutes with almost no extra storage cost — the right way to give QA or a data scientist production-like data without a restore |
| **Backtrack** | Rewinds the cluster in place to a point in time **without a restore** (MySQL-compatible). Recovery from a bad migration in minutes rather than hours |
| **Fast database cloning + zero-downtime patching + Blue/Green Deployments** | Managed blue/green creates a synchronised copy of the cluster for you to upgrade and test, then switches over in ~a minute |
| **Aurora Machine Learning** | Call SageMaker/Comprehend directly from SQL — e.g. inline fraud scoring |

**One-liner:** "Aurora is RDS-compatible on the wire but a different animal underneath — compute separated from a distributed, self-healing, 6-way-replicated storage layer, which is where the low replica lag, 15 replicas, instant cloning, backtrack, and fast failover all come from."

### ElastiCache & Caching Patterns

**What it is:** managed in-memory caching — **Redis** or **Memcached** — delivering sub-millisecond reads. Its main job in an architecture is taking read load off a database and making expensive computations cheap to repeat.

**Redis vs Memcached — a table you should be able to reproduce:**
| | **Redis** | **Memcached** |
|---|---|---|
| Data structures | Rich: strings, lists, sets, **sorted sets**, hashes, streams, bitmaps, HyperLogLog | Simple key/value strings only |
| Persistence | ✅ Snapshots + AOF | ❌ Purely in-memory |
| Replication / HA | ✅ Read replicas, **Multi-AZ with automatic failover** | ❌ No replication |
| Backup & restore | ✅ | ❌ |
| Scaling | Cluster mode: shards + replicas | Horizontal sharding across nodes; **multi-threaded** |
| Transactions / Pub-Sub / Lua | ✅ | ❌ |
| Choose it for | Almost everything: session store, leaderboards, rate limiting, pub/sub, queues | Pure, simple, sharded cache where losing the whole cache is fine and multi-threaded throughput per node matters |

**Default recommendation is Redis** — the only real reason to pick Memcached is a genuinely simple cache that benefits from its multi-threaded model and needs no persistence or failover.

**Caching patterns to name explicitly:**
- **Lazy loading / cache-aside** — check the cache; on a miss, read the database and populate the cache. Only cached data is ever requested data, but every miss pays the full latency and there's a **cache stampede** risk when a hot key expires (mitigate with a short lock or staggered TTLs).
- **Write-through** — write to the cache and the database together. Reads are always warm, but you cache data nobody may read and every write is slower.
- **Write-behind** — write to the cache and flush to the database asynchronously. Fastest writes, but risks data loss on node failure.
- **TTL / eviction** — every cached item needs an expiry, and the eviction policy (`allkeys-lru` etc.) decides what goes when memory fills. **A cache with no TTL strategy is a stale-data bug waiting to happen** — that's the sentence to say.

**Concrete uses:** database query result caching, **session state** for a stateless web tier (the correct alternative to [sticky sessions](#sticky-sessions-session-affinity)), **rate limiting** counters, **leaderboards** via Redis sorted sets, distributed locks, and pub/sub fan-out. In .NET this is `IDistributedCache` via `AddStackExchangeRedisCache`, and `HybridCache` in .NET 9 for a combined in-process + distributed tier.

**Operational notes:** ElastiCache lives in your VPC with a security group on port **6379** (Redis) or **11211** (Memcached); it's **not** publicly reachable. Enable **encryption in transit/at rest** and **Redis AUTH** or RBAC. Use **cluster mode** to shard beyond one node's memory, and **Multi-AZ with automatic failover** for production. **MemoryDB for Redis** is the durable variant — multi-AZ transaction log, usable as a *primary* database rather than just a cache.

**The cache-invalidation question:** the honest senior answer is that invalidation is the hard part, and the strategy depends on tolerance for staleness — short TTLs for cheap eventual correctness, explicit invalidation on write for correctness-critical data, and versioned cache keys (`user:123:v7`) to sidestep deletion entirely.

**DAX (DynamoDB Accelerator)** — the DynamoDB-specific cache, and worth distinguishing from ElastiCache because interviewers ask which you'd use:
- An **in-VPC, write-through cache cluster that is API-compatible with DynamoDB**, taking eventually-consistent reads from single-digit **milliseconds to microseconds**.
- **The key advantage: no application caching logic.** You point the DAX client at the cluster instead of the DynamoDB endpoint and cache-aside handling, invalidation, and population all disappear — because DAX sits transparently in front of the table.
- Two caches inside it: an **item cache** (for `GetItem`/`BatchGetItem`) and a **query cache** (for `Query`/`Scan` result sets), each with its own TTL.
- **Limitations to state:** it only helps **eventually-consistent** reads (a strongly-consistent read passes straight through to DynamoDB), it's a cluster you pay for by the hour rather than serverless, and writes go through to the table so it doesn't accelerate them.

**DAX vs ElastiCache for a DynamoDB workload:** DAX when you want to cache DynamoDB reads with **zero code change** and you're fine with eventual consistency. ElastiCache when you need to cache something *other* than raw table reads — computed aggregates, joined/enriched objects, session state, rate-limit counters, leaderboards — or you want full control over keys and eviction. **A read-heavy DynamoDB table hitting a hot partition or RCU cost ceiling → DAX** is the answer that shows you know the purpose-built option exists.

#### ElastiCache — Pitfalls

**❗ 1. Cache stampede (thundering herd).** A popular key expires and hundreds of concurrent requests all miss at once, hitting the database simultaneously — so the cache *causes* the outage it was meant to prevent. Three mitigations worth naming: **jittered TTLs** (so keys don't expire in lockstep), a **short-lived lock** (`SET key NX EX 5`) so exactly one caller repopulates while the rest wait or serve stale, and **serve-stale-while-revalidate**. This is the single most likely caching question.

**❗ 2. Hot key.** A key lives on **exactly one shard**, so one disproportionately popular key saturates a single node no matter how many shards you add — **cluster mode does not help.** Fix with a small **client-side/in-process cache** for that key (in .NET, `HybridCache` or `MemoryCache` in front of Redis), or by splitting it into `leaderboard:{0..9}` and merging. Same shape as a DynamoDB hot partition.

**3. No TTL means stale data forever *and* memory exhaustion.** Every entry needs an expiry, and the cluster needs an eviction policy. The default **`noeviction`** makes *writes start failing* once memory is full — for a cache you almost always want **`allkeys-lru`**. Leave headroom (`reserved-memory-percent` ~25%) so failover and background saves have room to fork.

**4. Watch the right metrics.** `CacheHitRate` (a low rate means you're paying for a cache that isn't working), **`Evictions`** (rising = too small or no TTLs), `DatabaseMemoryUsagePercentage`, `SwapUsage`, and `CurrConnections`. A cache with a 20% hit rate is worse than no cache — it adds a hop and still hits the database.

**5. Failover is not transparent to the client.** On primary failover the endpoint behind the DNS name changes, and clients must reconnect. Use the **configuration endpoint** (cluster mode) or the primary/reader endpoints — never a node's own address. In .NET, `ConnectionMultiplexer` should be a **singleton** created once with `abortConnect=false`, plus a retry policy; without that flag an app can permanently fail to start if Redis is briefly unavailable at boot.

**6. Redis is single-threaded — one slow command blocks everything.** `KEYS *` on a large keyspace, a big `DEL` of a huge collection, or an expensive Lua script stalls **every** other client. Use **`SCAN`** instead of `KEYS`, and **`UNLINK`** instead of `DEL` for large values. This is a real production-incident answer.

**7. Big keys and serialization cost.** A multi-megabyte value causes latency spikes and skews shard memory. And caching a large object graph can cost more in serialize/deserialise than the database query it replaced — measure rather than assume the cache is faster.

**8. ❗ It is a cache, not a database.** Node failure loses whatever wasn't replicated or persisted. If you need durability, that's **MemoryDB for Redis** (multi-AZ transaction log), not ElastiCache. Teams that quietly promote ElastiCache to a system of record discover this during a failover.

**9. Connection pressure from Lambda** — the same shape as the [RDS Proxy problem](#pattern-lambda--rds-proxy--rds--full-explanation): each concurrent execution environment opens its own connection. Redis tolerates far more connections than a relational database, but reuse the multiplexer **outside the handler** rather than connecting per invocation.

**10. Some settings are creation-time only.** **In-transit encryption** and cluster mode can't be toggled on an existing cluster — changing them means a new cluster and a migration. Decide before you provision.

**11. Cost.** `cache.t*` nodes are **burstable** with CPU-credit mechanics like the EC2 T-family, so a steadily busy cache on a `t3` will throttle. Reserved nodes cut cost for steady workloads, and right-sizing matters because you pay per node-hour whether the cache is being hit or not.

---

## Networking

### VPC, Subnets, NAT — Complete Model

**What a VPC is:** a logically isolated virtual network — "your own private data center inside AWS." You control IP range, subnets, routing, internet access, and security boundary (Security Groups + NACLs).

**CIDR block:** defined at VPC creation (e.g., `10.0.0.0/16`); cannot be changed after creation (you can add secondary CIDR blocks, but the original design constraint stands — plan IP space carefully up front, especially for future VPC peering/Transit Gateway where overlapping CIDRs cause real pain).

**Subnets**
- A subnet is a slice of the VPC CIDR, tied to exactly one Availability Zone.
- **There is no such thing as an inherently "public" or "private" subnet** — that behavior comes entirely from the subnet's route table, not its name or any flag.

**Public vs private subnet — the real rule**
| | Public Subnet | Private Subnet |
|---|---|---|
| Route to Internet Gateway? | Yes | No |
| Inbound from internet? | Possible (if resource has public IP + SG allows) | No |
| Outbound to internet? | Yes, directly | Only via NAT Gateway/Instance |

**Internet Gateway (IGW):** one per VPC, must be attached to the VPC; the public subnet's route table must have a `0.0.0.0/0 → igw-xxxx` route.

**Route tables:** every subnet is associated with exactly one route table; a route table can be shared by multiple subnets. Typical routes: `0.0.0.0/0 → IGW` (public) or `0.0.0.0/0 → NAT Gateway` (private).

**NAT (Network Address Translation)**
- Purpose: let private-subnet resources reach the internet **outbound only** — inbound internet traffic is always blocked regardless of NAT.
- Flow: `Private Subnet → NAT Gateway (in a PUBLIC subnet) → Internet Gateway → Internet`.
- **NAT Gateway must live in a public subnet** — a NAT Gateway placed in a private subnet is simply invalid/non-functional.

| | NAT Gateway | NAT Instance |
|---|---|---|
| Management | Fully managed | Self-managed EC2 |
| HA | Built-in within AZ | You build it |
| Scaling | Automatic | Manual |
| Recommendation | **Default choice** | Legacy/very specific cost cases only |

**Cost gotcha:** NAT Gateway bills per-hour *plus* per-GB processed — high outbound traffic from private resources is a common surprise cost spike. For AWS-service-only traffic (S3, DynamoDB, Secrets Manager, SQS, etc.), use **VPC Endpoints** instead of routing through NAT — cheaper, lower latency, and keeps traffic off the public internet entirely.

**Security layers**
| | Security Group | Network ACL |
|---|---|---|
| Statefulness | Stateful (return traffic auto-allowed) | Stateless (must explicitly allow both directions) |
| Attached to | ENI/instance | Subnet |
| Rule type | Allow only | Allow AND deny |
| Typical usage | Primary defense — use heavily | Sparingly, for coarse subnet-level blocking |

**Placement rules of thumb**
- Public subnet: load balancers, bastion hosts, NAT Gateways.
- Private subnet: application servers, ECS tasks, RDS — **never put a database directly in a public subnet.**

**Common misconceptions (explicitly false)**
- "Public subnet = automatic internet access" — false; needs a public IP *and* a route to an IGW *and* permissive SG.
- "NAT allows inbound traffic" — false; NAT is outbound-only by design.
- "Subnet name/tag decides security behavior" — false; only route tables and SGs/NACLs matter.
- "One route table per VPC" — false; a VPC can (and usually does) have multiple route tables, one per subnet-group.

**High-availability note:** NAT Gateways are AZ-scoped. For proper HA, deploy one NAT Gateway per AZ so an AZ failure doesn't take down outbound internet access for every private subnet in the VPC (a single shared NAT Gateway across AZs works but creates a cross-AZ dependency and extra data-transfer cost).

### [new content] VPC Reference Architecture

```
                            +------------+
                            |  Internet  |
                            +-----+------+
                                  |
                        +---------v----------+
                        |  Internet Gateway  |
                        +----+----------+----+
                             |          |
+----------------------------|----------|-----------------------------+
| VPC  10.0.0.0/16           |          |                             |
|                            v          v                             |
|      AVAILABILITY ZONE A               AVAILABILITY ZONE B          |
|  +--------------------------+      +--------------------------+     |
|  | PUBLIC      10.0.0.0/24  |      | PUBLIC      10.0.1.0/24  |     |
|  | ALB . NAT GW . Bastion   |      | ALB . NAT GW             |     |
|  +------------+-------------+      +------------+-------------+     |
|               | outbound via NAT                | outbound via NAT  |
|  +------------v-------------+      +------------v-------------+     |
|  | PRIVATE APP 10.0.10.0/24 |      | PRIVATE APP 10.0.11.0/24 |     |
|  | ECS tasks / EC2 app      |      | ECS tasks / EC2 app      |     |
|  +------------+-------------+      +------------+-------------+     |
|               |                                 |                   |
|  +------------v-------------+      +------------v-------------+     |
|  | PRIVATE DB  10.0.20.0/24 |      | PRIVATE DB  10.0.21.0/24 |     |
|  | RDS PRIMARY              |<====>| RDS STANDBY              |     |
|  +--------------------------+ sync +--------------------------+     |
+---------------------------------------------------------------------+

  Both AZs' app subnets talk to the RDS PRIMARY; the standby carries no
  traffic and only takes over on failover. No subnet is "public" because
  of its name -- only because its route table points 0.0.0.0/0 at the IGW.
```

This is the canonical 3-tier VPC layout senior interviewers expect: public subnet per AZ (ALB + NAT), private app subnet per AZ (compute), private isolated data subnet per AZ (RDS with no route to NAT/IGW at all — DB subnets typically don't even need outbound internet).

### [gaps] VPC/Subnet/NAT/SG Rapid-Fire Drill Sheet

The prose above already covers the VPC networking model in depth — this is a condensed, last-minute-review version of the same fundamentals, formatted as quick-recall drill Q&A rather than explanatory prose. Use this the morning of an interview; use the sections above to actually *understand* it first.

| Drill question | One-line answer |
|---|---|
| What makes a subnet "public"? | Its route table has a `0.0.0.0/0 → Internet Gateway` route — nothing else. |
| What makes a subnet "private"? | No direct route to an Internet Gateway; outbound internet (if any) goes through a NAT Gateway/Instance instead. |
| NAT Gateway vs Internet Gateway — one-line difference? | IGW = two-way internet access for public-subnet resources; NAT Gateway = outbound-only internet access for private-subnet resources. |
| Where must a NAT Gateway live? | In a **public** subnet — a NAT Gateway in a private subnet doesn't work. |
| Can NAT allow inbound traffic from the internet? | No — NAT is outbound-only by design, always. |
| Security Group vs NACL — statefulness? | SG is stateful (return traffic auto-allowed); NACL is stateless (must explicitly allow both directions). |
| Security Group vs NACL — attaches to what? | SG attaches to an ENI/instance; NACL attaches to a subnet. |
| Security Group vs NACL — can either explicitly Deny? | SG: allow-only. NACL: allow AND deny rules. |
| Which is the primary defense layer in practice? | Security Groups — use heavily; NACLs sparingly for coarse subnet-level blocking. |
| Where should a database subnet route to? | Nowhere on the internet — no route to IGW or NAT at all, ideally (isolated private/data subnet). |
| Does a route table belong to a VPC or a subnet? | Each subnet is associated with exactly one route table; a VPC typically has multiple route tables (not just one). |
| Is a NAT Gateway AZ-scoped or region-scoped? | AZ-scoped — deploy one per AZ for HA, or accept a cross-AZ dependency with a single shared one. |
| What's the #1 wrong assumption about "public subnet"? | That it grants automatic internet access — it still needs a resource with a public IP *and* a permissive Security Group on top of the IGW route. |

---

### VPC Flow Logs

**What they capture:** **metadata about IP traffic** — not payloads. Enable them at the **VPC**, **subnet**, or **individual ENI** level, and send them to **CloudWatch Logs**, **S3**, or **Data Firehose**.

Each record contains source/destination address and port, protocol, packet and byte counts, the time window, and — the field that matters most — **`action`: `ACCEPT` or `REJECT`**.

**Why they're the answer to "how do you debug connectivity?"**: a `REJECT` record proves traffic *arrived* and was blocked by a security group or NACL; **no record at all** means the packets never got there (wrong route table, wrong subnet, no IGW/NAT). That distinction, combined with the timeout-vs-connection-refused rule from [Security Groups](#security-groups-their-properties--classic-ports), narrows almost any network fault in minutes.

- A useful nuance: because **security groups are stateful**, a SG block shows only the inbound `REJECT`; because **NACLs are stateless**, a NACL misconfiguration typically shows `REJECT` on the **return** path too. Seeing rejects in both directions points at the NACL.
- Query them with **Athena** (if delivered to S3) or **CloudWatch Logs Insights** (if delivered to CloudWatch).
- Also used for security analytics — port-scan and exfiltration detection — and for **cross-AZ / NAT data-transfer cost attribution**.
- **Not captured:** traffic to the Amazon DNS server, DHCP, the **instance metadata endpoint `169.254.169.254`**, Windows license activation, and traffic to the reserved VPC router address. Knowing this list explains "why isn't my traffic showing up?"

### VPC Peering

A private, one-to-one network connection between two VPCs — same account or different, same region or different — using AWS's internal network (no IGW, NAT, or VPN involved).

**The three constraints that are the interview question:**
1. **❗ CIDR blocks must not overlap.** No exceptions, no NAT workaround. This is why IP-space planning at VPC creation matters so much.
2. **❗ Peering is NOT transitive.** If A peers with B and B peers with C, **A cannot reach C**. You must create a direct A↔C peering. This is the single most-asked VPC peering question.
3. **No edge-to-edge routing** — you cannot use a peer's internet gateway, NAT gateway, VPN, or Direct Connect connection.

Also: you must add routes on **both** VPCs' route tables, and update security groups. In the same region you can **reference a security group in the peered VPC** by ID (across accounts too), which is much better than hardcoding CIDRs.

**Why peering doesn't scale:** connecting *n* VPCs fully requires **n(n−1)/2** peering connections — 10 VPCs means 45 connections, each with route-table entries on both sides. That mesh explosion is precisely what Transit Gateway exists to solve.

### Transit Gateway

A **regional hub-and-spoke router**. Every VPC, Site-to-Site VPN, and Direct Connect gateway attaches once to the TGW, and the TGW routes between them.

- **It supports transitive routing** — the thing peering cannot do. A → TGW → C works.
- Scales to thousands of attachments; connecting *n* VPCs takes *n* attachments, not n(n−1)/2 connections.
- **TGW route tables per attachment** give you network segmentation — e.g. a route table that lets prod VPCs reach shared services but not each other, and keeps non-prod fully isolated. This is how real multi-account networks are built.
- **Inter-region TGW peering** connects hubs across regions over the AWS backbone.
- Supports **multicast**, which neither peering nor VPN does.
- Works with **Resource Access Manager** to share one central TGW across all accounts in the organisation (see [RAM](#aws-resource-access-manager-ram)).
- **Cost:** charged per attachment-hour **plus** per GB processed — so it's more expensive than peering for a simple two-VPC case.

| | VPC Peering | Transit Gateway |
|---|---|---|
| Topology | Point-to-point mesh | **Hub and spoke** |
| Transitive routing | ❌ | ✅ |
| Scale | Poor beyond ~5 VPCs | Thousands of attachments |
| Cost | **No hourly charge** (only cross-AZ/region data transfer) | Per attachment-hour + per GB |
| Segmentation | Route tables per VPC | **TGW route tables per attachment** |
| Best for | Two or three VPCs, cost-sensitive, high bandwidth | Multi-account/multi-VPC networks, hybrid connectivity |

### VPC Endpoints & PrivateLink

**The problem:** an instance in a **private** subnet calling S3, DynamoDB, or Secrets Manager normally routes out through a **NAT Gateway** to a public endpoint — which costs money per GB and sends traffic over the internet. VPC endpoints keep it entirely inside the AWS network.

| Type | Works with | How it works | Cost |
|---|---|---|---|
| **Gateway endpoint** | **S3 and DynamoDB only** | A **route-table entry** pointing a prefix list at the endpoint. No ENI, no IP | **Free** |
| **Interface endpoint** (**PrivateLink**) | Most AWS services, plus SaaS and your own services | An **ENI with a private IP** in your subnet, with a security group | Per hour **+ per GB** |
| **Gateway Load Balancer endpoint** | Third-party inspection appliances | Directs traffic to a GWLB (see [Load Balancing](#load-balancing-fundamentals)) | Per hour + per GB |

**Facts that decide questions:**
- "How do I reach S3 from a private subnet without a NAT Gateway?" → **Gateway endpoint, and it's free.** That's both the security answer and a real cost optimisation, since NAT per-GB charges on S3 traffic are a common surprise bill.
- Interface endpoints need **private DNS enabled** to make the standard service hostname (`secretsmanager.us-east-1.amazonaws.com`) resolve to the private IP; without it your SDK still goes to the public endpoint. Their security group must allow **inbound 443** from the client subnets. Both are frequent "the endpoint exists but nothing uses it" causes.
- Interface endpoints are reachable **from on-premises** over Direct Connect/VPN; gateway endpoints are **not**.
- Restrict them further with an **endpoint policy** (a resource policy on the endpoint) — e.g. this endpoint may only reach these buckets.
- **PrivateLink for your own service:** put an **NLB** in front of it and expose it as an endpoint service; consumers in other VPCs/accounts create interface endpoints to reach it — no peering, no overlapping-CIDR problem, no exposure to the internet. This is the standard way to publish an internal service across a large organisation.

### Hybrid Connectivity: Site-to-Site VPN & Direct Connect

| | **Site-to-Site VPN** | **Direct Connect (DX)** |
|---|---|---|
| Medium | **IPsec over the public internet** | **Dedicated private fibre** to an AWS Direct Connect location |
| Setup time | Minutes to hours | **Weeks to months** (physical circuit provisioning) |
| Bandwidth | ~1.25 Gbps per tunnel (scale with multiple tunnels/ECMP) | 1 / 10 / 100 Gbps dedicated, or sub-1 Gbps hosted |
| Latency | Variable — it's the internet | **Consistent and predictable** |
| Encryption | **Encrypted by default** (IPsec) | **❗ Not encrypted by default** — add a VPN over DX, or MACsec |
| Cost | Cheap hourly + data transfer | High fixed port cost, but **materially cheaper egress at volume** |
| Use for | Quick setup, branch offices, backup path, low-to-moderate volume | Large sustained data transfer, latency-sensitive hybrid apps, regulatory requirements against internet transit |

- **VPN components:** a **Customer Gateway** (your side — the physical/software device, plus its public IP) and a **Virtual Private Gateway** on the VPC (or a Transit Gateway attachment). AWS provisions **two tunnels to two different endpoints** for redundancy — using only one is a common single point of failure.
- **Direct Connect Gateway** lets one DX connection reach VPCs in **multiple regions and accounts**.
- **The standard HA answer:** two DX connections at **two different DX locations** for full redundancy; or, more cheaply, **one DX with a Site-to-Site VPN as automatic backup** — a very common real-world design and a good answer to "how do you make hybrid connectivity resilient?"
- **AWS Client VPN** is the different product for *individual users* (laptops) connecting into the VPC, as opposed to site-to-site networks.

### Hands-On: VPC

```bash
# VPC + one public and one private subnet
aws ec2 create-vpc --cidr-block 10.0.0.0/16 --tag-specifications 'ResourceType=vpc,Tags=[{Key=Name,Value=prod-vpc}]'
aws ec2 create-subnet --vpc-id vpc-abc --cidr-block 10.0.1.0/24 --availability-zone us-east-1a  # public
aws ec2 create-subnet --vpc-id vpc-abc --cidr-block 10.0.11.0/24 --availability-zone us-east-1a # private

# Internet gateway + public route (this route is what MAKES the subnet public)
aws ec2 create-internet-gateway
aws ec2 attach-internet-gateway --vpc-id vpc-abc --internet-gateway-id igw-abc
aws ec2 create-route --route-table-id rtb-public --destination-cidr-block 0.0.0.0/0 --gateway-id igw-abc

# NAT gateway (in the PUBLIC subnet) + private route
aws ec2 allocate-address --domain vpc
aws ec2 create-nat-gateway --subnet-id subnet-public --allocation-id eipalloc-abc
aws ec2 create-route --route-table-id rtb-private --destination-cidr-block 0.0.0.0/0 --nat-gateway-id nat-abc

# Free S3 gateway endpoint so private subnets skip the NAT for S3 traffic
aws ec2 create-vpc-endpoint --vpc-id vpc-abc --service-name com.amazonaws.us-east-1.s3 \
  --vpc-endpoint-type Gateway --route-table-ids rtb-private

# Flow logs for debugging
aws ec2 create-flow-logs --resource-type VPC --resource-ids vpc-abc --traffic-type ALL \
  --log-destination-type cloud-watch-logs --log-group-name /aws/vpc/flowlogs \
  --deliver-logs-permission-arn arn:aws:iam::123456789012:role/flowlogsRole
```
**Debug order for "my instance can't reach the internet":** route table (is there a `0.0.0.0/0` and does it point at an IGW for public / NAT for private?) → is the NAT Gateway actually **in a public subnet**? → security group outbound → NACL both directions → does the instance have a public IP at all (public subnet only) → then read the **flow logs** for `ACCEPT`/`REJECT`.

### Route 53

**What it is:** highly available, scalable DNS service that is also a traffic-control layer (health checks, routing policies, failover) — not just static DNS.

**How DNS resolution actually works** — worth being able to walk through, because several Route 53 answers depend on it:
```
Browser cache → OS cache → Recursive resolver (ISP / 8.8.8.8)
   → Root nameserver (.)            "ask the .com servers"
   → TLD nameserver (.com)          "ask ns-123.awsdns-45.com"
   → Authoritative nameserver       "example.com A = 52.1.2.3"   ← Route 53 lives here
   → answer cached at every hop for the length of the TTL
```
Route 53's name comes from **port 53**, the DNS port. When you create a public hosted zone, Route 53 gives you **4 nameservers**, and you point your registrar's NS records at them — that delegation is what makes Route 53 authoritative for the domain. A very common real-world failure is creating the hosted zone but never updating the registrar, or deleting and recreating a hosted zone (which issues **different** nameservers).

**Record types you should know:**
| Record | Purpose |
|---|---|
| **A** | Hostname → IPv4 address |
| **AAAA** | Hostname → IPv6 address |
| **CNAME** | Hostname → another hostname. **Cannot be used at the zone apex** (`example.com`), only on subdomains |
| **ALIAS** | Route 53-specific: hostname → an **AWS resource** (ALB, CloudFront, S3 website, API Gateway, another Route 53 record). Free, works **at the apex**, and health-check aware |
| **NS** | Delegates a zone to its nameservers |
| **SOA** | Start of authority — zone metadata |
| **MX** | Mail servers, with priority values |
| **TXT** | Arbitrary text — SPF/DKIM/DMARC for email, and domain-ownership verification (ACM certificate validation uses **CNAME** records for this) |
| **SRV** | Service location: host + port |
| **PTR** | Reverse DNS (IP → name) |
| **CAA** | Restricts which certificate authorities may issue certs for the domain |

**TTL (Time To Live)** — how many seconds resolvers may cache a record. It's a direct trade-off: a **high TTL** (e.g. 24 h) means fewer Route 53 queries (lower cost) but stale answers linger after a change; a **low TTL** (e.g. 60 s) means fast propagation but more queries and cost. The standard practice before a planned migration or cutover is to **lower the TTL well in advance** (at least one old-TTL period ahead), make the change, then raise it again. **ALIAS records to AWS resources have no TTL you set** — Route 53 manages it.

**Core concepts:** Domain Name, Hosted Zone (public = internet-resolvable, private = VPC-only), DNS Records (A/AAAA, CNAME, ALIAS, MX, TXT, NS, SOA).

**Why ALIAS > CNAME for AWS targets:** ALIAS records are free, resolve at the DNS layer without an extra lookup, and — critically — **work at the zone apex** (`example.com`, not just `www.example.com`), which a CNAME cannot do by DNS spec.

**Routing policies**
| Policy | Use case |
|---|---|
| Simple | Single endpoint, no failover |
| Weighted | Canary/A-B traffic shifting (e.g., 90/10 split) |
| Failover | Primary/secondary HA via health checks |
| Latency-based | Route to the AWS region with lowest measured latency |
| Geolocation | Legal/regional content restrictions |
| Geoproximity | Route based on the geographic location of users **and** resources, with a configurable "bias" to shift more/less traffic toward a given region — requires **Route 53 Traffic Flow**, unlike the other policies |
| Multi-value answer | Simple client-side load distribution (not a real load balancer) |

**Health checks:** monitor HTTP/HTTPS/TCP endpoints, can integrate with CloudWatch alarms; health checks alone do **not** reroute traffic — you still need a Failover (or similar) routing policy attached.

**Route 53 vs Load Balancer** — they operate at different layers and are complementary, not competing:
- Route 53: DNS-level, global, region-aware, coarse-grained.
- ALB/NLB: request-level, regional, fine-grained (per-request routing, sticky sessions, real-time health-based removal).

**Critical trap-question theme:** DNS is **not** instant. TTL caching at resolvers/ISPs/clients means:
- Updating a record doesn't immediately redirect all clients.
- Failover routing is not instant — it's bounded by TTL plus client-side caching behavior.
- Route 53 should never be your *only* HA mechanism for sub-second failover requirements — combine with ALB/NLB-level health-based removal for fast reaction, and Route 53 for macro/region-level failover.

**Private Hosted Zones:** internal DNS resolvable only inside associated VPC(s) — e.g., `db.internal → RDS endpoint`. A private hosted zone must be explicitly associated with each VPC that needs to resolve it (a common trap: "works in one VPC, not another" = missing association).

**Route 53 Resolver:** the DNS query-forwarding service that sits behind every VPC's default DNS resolution. For hybrid setups (VPC ↔ on-premises), you attach **inbound endpoints** (let on-prem resolvers query your VPC's private hosted zones) and **outbound endpoints** (let VPC resources forward queries to on-prem DNS servers via **Resolver rules**) — this is the standard mechanism for resolving `*.internal` on-prem names from Lambda/EC2/ECS inside a VPC, and vice versa, without standing up your own DNS forwarders.

**AWS Global Accelerator, and how it differs from plain Route 53 latency routing:** Global Accelerator gives you two static anycast IPs that front your application and routes client traffic over AWS's private global network backbone (instead of the public internet) to the closest healthy regional endpoint (ALB, NLB, or EC2). Route 53 can point a domain at those Global Accelerator static IPs, combining Route 53's DNS-layer control with Global Accelerator's network-layer performance and fast (sub-minute) health-check-based failover — a stronger option than Route 53 latency-based routing alone when TTL-caching delays on failover are unacceptable, since the entry-point IPs never change even as Global Accelerator reroutes underneath them.

**DNSSEC:** a Route 53 security best practice that cryptographically signs DNS responses (via a Key-Signing Key/Zone-Signing Key chain of trust) so resolvers can verify a response hasn't been spoofed or tampered with in transit — mitigates DNS cache-poisoning and spoofing attacks. Route 53 supports DNSSEC signing for public hosted zones; enabling it is a one-time hardening step worth naming alongside IAM policy restrictions and AWS Organizations-level change control when asked "how would you secure Route 53?"

### API Gateway Auth & Integration Patterns

**Authentication/authorization options** (API Gateway supports several, and knowing when to reach for each is the senior-level part of the answer):
| Mechanism | How it works | Best for |
|---|---|---|
| **Cognito User Pools (JWT authorizer)** | API Gateway validates a JWT issued by a Cognito User Pool (or any OIDC-compliant IdP) directly at the gateway, before the request ever reaches your Lambda/backend | Standard username/password or social-login user auth for public APIs — no custom auth code to write/maintain |
| IAM authorization | Caller signs the request with SigV4; API Gateway checks IAM policy | Service-to-service calls within your own AWS account/org |
| Lambda custom authorizer | Your own Lambda inspects the token/headers and returns an IAM policy | Legacy tokens, non-standard auth schemes, or logic too custom for a built-in authorizer |
| API keys + usage plans | Simple key checked against a usage plan (throttle/quota) | Partner/B2B API monetization, not real authentication |

**Cognito specifically:** a **User Pool** is the user directory + token issuer (handles sign-up/sign-in, hosted UI, MFA, and issues ID/access JWTs); an **Identity Pool** is the separate mechanism for exchanging those tokens (or third-party IdP tokens) for temporary AWS credentials when a client needs to call AWS services directly. For API Gateway, you almost always want a **User Pool** JWT authorizer — Identity Pools matter when a mobile/SPA client needs direct, scoped AWS SDK access (e.g., uploading straight to S3) rather than going through your API. The same Cognito User Pool can also be wired up as the OIDC identity provider on an **ALB listener rule**, letting the load balancer authenticate users before forwarding to targets — useful when you're on ALB rather than API Gateway but still want managed login without writing auth code into every backend service.

**VPC Link:** lets API Gateway (REST or HTTP API) securely reach resources inside a private VPC — an internal ALB/NLB, or (for HTTP APIs specifically) a Cloud Map service registry entry — without exposing those resources to the public internet. REST APIs require a VPC Link backed by an NLB; HTTP APIs support the newer VPC Link v2, which can target an ALB or Cloud Map directly, one less hop than the REST-API NLB requirement. This is the standard pattern for exposing an internal-only ECS/EC2 service through a public API Gateway front door without opening it up directly.

**CORS (Cross-Origin Resource Sharing):** required whenever a browser-based client on one origin calls an API Gateway endpoint on another. API Gateway can auto-generate the required `OPTIONS` preflight method and `Access-Control-Allow-*` response headers per resource, or you can hand-roll them in your Lambda proxy integration response — the common trap is enabling CORS on the API but forgetting to also return the headers from the Lambda's actual (non-OPTIONS) response, which still fails the browser's CORS check even though the preflight succeeds.

**Request validation via JSON Schema models:** API Gateway can validate incoming requests *before* invoking the backend, using a **request model** (a JSON Schema definition of the expected body shape) and a **request validator** configured per method/API to check the body, the query-string/header parameters, or both. This rejects malformed requests at the gateway with a 400, saving a Lambda invocation (and its cost/cold-start) on input that was never going to succeed anyway — a good example of "fail fast at the edge" that's worth naming when asked about API Gateway best practices.

**Direct service integrations (bypassing Lambda):** API Gateway can integrate directly with certain AWS services — most commonly **DynamoDB** (GetItem/PutItem/Query mapped straight from the HTTP request via a VTL mapping template), but the same "AWS service integration" mechanism extends to **Step Functions** (start an execution directly from an API call) and **Kinesis** (PutRecord straight from the API, useful for high-volume ingestion endpoints). The senior-level point to make: this isn't just a cost optimization — it removes an entire compute layer (and its cold start, patching, and failure surface) for simple CRUD-shaped or fire-and-forget endpoints where a Lambda would add no real logic beyond marshalling the request. The trade-off is VTL mapping templates are clunkier to write/debug than Lambda code, so this pattern is best reserved for genuinely thin passthrough endpoints, not anything needing real business logic.

## Load Balancing, Scalability & Auto Scaling

### Scalability, High Availability, Elasticity & Agility

Four words interviewers deliberately blur together. Define them cleanly and you've answered half the section.

**Scalability** = the system *can* handle more load.
- **Vertical scaling (scale up)** — a bigger instance. `t3.micro` → `m5.4xlarge`. Simple, no app changes, but there's a hard ceiling (the largest instance type) and it usually needs downtime. This is how you scale things that can't be distributed: RDS primaries, a single large cache node, legacy monoliths.
- **Horizontal scaling (scale out)** — more instances behind a load balancer. Effectively unlimited, no downtime, better fault tolerance. Requires the app to be **stateless**. This is the default for web/API tiers.

**High Availability** = surviving a failure without downtime, achieved by running in **at least two Availability Zones**. Note this is a *different goal* from scalability: you can be highly scalable in one AZ (and lose everything when that AZ fails) or highly available with two small instances that can't handle load. Interviewers probe exactly this confusion.

**Elasticity** = scaling **automatically, in both directions, matched to actual demand** — so you pay only for what you need right now. Scalability is the capability; elasticity is the automation of it.

**Agility** = how fast you can get new resources at all — minutes instead of a months-long hardware procurement cycle. It has **nothing to do with load**; it's about speed of change. This is the one people get wrong.

| Term | Question it answers | Example |
|---|---|---|
| Scalability | *Can* it grow? | An ASG whose max is 20 instances |
| Elasticity | Does it grow and shrink **by itself**? | That ASG on a target-tracking policy, scaling 2→20→2 across the day |
| High Availability | Does it survive a failure? | That ASG spread across 3 AZs behind an ALB |
| Agility | How fast can I get *anything* new? | Spinning up a whole test environment in 10 minutes via Terraform |

**A scalable-but-not-elastic example to have ready:** a fixed fleet of 20 EC2 instances sized for Black Friday. It scales (it handles the peak) but it isn't elastic (you pay for 20 instances in February). That single example demonstrates you understand the difference rather than reciting definitions.

**HA vs Fault Tolerance vs Disaster Recovery:** HA = minimal downtime within a region (multi-AZ); fault tolerance = *zero* interruption from a component failure (redundancy with no impact); DR = recovering from losing an entire region, measured by **RTO/RPO** — see [Disaster Recovery Strategies](#new-content-disaster-recovery-strategies).

---

### ALB vs API Gateway vs ELB (NLB/GWLB/CLB)

**ELB family**
| Type | Layer | Protocols | Best for |
|---|---|---|---|
| ALB (Application LB) | 7 | HTTP/HTTPS/WebSocket | Host/path routing, microservices, Lambda targets |
| NLB (Network LB) | 4 | TCP/UDP/TLS | Extreme throughput, static IP, low latency |
| GWLB (Gateway LB) | 3 | IP | Transparent traffic inspection (firewalls/IDS appliances) |
| CLB (Classic, deprecated) | 4/7 | Basic | Legacy only |

**ALB core concepts:** Listener (port/protocol) → Rules (host/path/header conditions) → Target Group (EC2, ECS, IP, **Lambda**) with health checks. Supports TLS termination with SNI (multiple certs on one listener), WebSocket, HTTP/2, and direct Lambda invocation as a target (Lambda returns an HTTP-shaped response, similar to API Gateway proxy integration).

**API Gateway core concepts:** REST API (feature-rich, more expensive — API keys, usage plans, request/response VTL mapping templates, caching) vs HTTP API (cheaper, lower latency, JWT/IAM auth, good default for Lambda-backed serverless) vs WebSocket API (connection-managed real-time).

**ALB vs API Gateway — the mental model that wins interviews**
> "ALB is a smart Layer-7 load balancer: I have services, route and balance traffic between them. API Gateway is a full API front door: I'm exposing an API to external clients and need auth, throttling, quotas, versioning, transformation, and monitoring built in."

| Aspect | ALB | API Gateway |
|---|---|---|
| Primary purpose | Load balancing between backend services | Publishing/managing APIs for consumers |
| Targets | EC2, ECS/EKS, IP, Lambda | Lambda, HTTP endpoints, AWS service integrations |
| Built-in throttling/quotas | No (app-level only) | Yes (usage plans, rate limits) |
| Request transformation | Limited (routing only) | Advanced (VTL mapping templates) |
| Caching | No | Yes (REST API only) |
| Pricing | Per-hour + LCU | Per-million-requests (+ cache if enabled) |
| Typical client | Internal/browser | Mobile/web/3rd-party API consumers |

**Decision examples**
- Public REST API for a mobile app needing auth/throttling/API keys, Lambda+DynamoDB backend → **API Gateway**.
- Internal microservices on ECS needing only path/host routing + TLS termination → **ALB**.
- Real-time chat: fully serverless → **API Gateway WebSocket API**; already on containers → **ALB WebSocket**.

---

### ELB Deep-Dive: Cross-Zone Load Balancing, 504 Timeouts & Shield DDoS Protection

**Cross-Zone Load Balancing:** when enabled, every load balancer node distributes traffic evenly across *all* registered targets in *all* enabled AZs, not just the targets in its own AZ — this evens out load when targets are unevenly distributed across AZs (e.g., 8 targets in AZ-A, 2 in AZ-B). ALB has cross-zone load balancing **on by default, always** (cannot be disabled); NLB has it **off by default** and it's a per-target-group toggle — enabling it on NLB can introduce cross-AZ data transfer charges, which is the usual reason teams leave it off for latency/cost-sensitive NLB use cases. Knowing that ALB and NLB default differently here is a good "gotcha" fact to have ready.

**HTTP 504 Gateway Timeout (ELB):** means the load balancer forwarded the request to a target but never got a timely response back. Two common root causes: an **unhealthy/slow target** (app hung, DB call blocking, thread pool exhausted) or a **traffic spike** overwhelming backend capacity before auto-scaling catches up. A third cause worth naming from standard ELB behavior: a mismatch between the **ALB idle timeout** (default 60s) and the backend's own keep-alive/response time — if your app can legitimately take longer than the ALB's configured idle timeout, you'll see 504s that have nothing to do with target health, and the fix is raising the ALB idle timeout (and your app's keep-alive) rather than chasing a phantom backend bug.

**AWS Shield — tying DDoS protection to Route 53/ELB alongside WAF:** Shield defends at the network/transport layer (L3/L4 volumetric and protocol attacks), while WAF defends at the application layer (L7 — malicious request patterns, rate limiting, bad bots) — they're complementary, not competing, and a senior answer names both together rather than picking one.
- **Shield Standard:** free, automatically enabled for **every** AWS account on Route 53, CloudFront, and ELB (ALB/NLB/CLB) — no opt-in needed, covers common, automated L3/L4 DDoS attack patterns.
- **Shield Advanced:** paid, opt-in tier adding larger-attack mitigation capacity, real-time attack visibility/metrics, integration with WAF for automatic rule creation during an attack, cost protection (credits for scaling charges incurred during an attack), and 24/7 access to the AWS DDoS Response Team (DRT).
- **Practical pairing:** Route 53 (DNS-layer resilience + Shield Standard baseline) + ELB (Shield Standard baseline, upgrade to Advanced for critical public endpoints) + WAF (L7 rate limiting/malicious pattern blocking) is the standard "defend the public edge" stack — worth stating as a layered answer rather than naming just one service.

---

### Load Balancing Fundamentals

**What a load balancer buys you:** spread traffic over many targets, one stable DNS name in front of disposable instances, automatic removal of unhealthy targets, TLS termination in one place, and cross-AZ high availability. The ELB itself is a managed, auto-scaling, multi-AZ fleet — you never patch or size it.

**The four objects, in order:** **Listener** (port + protocol) → **Rules** (conditions: host, path, header, query string, source IP, HTTP method) → **Target Group** (EC2, IP, Lambda, or another ALB) → **Targets**, each continuously **health-checked**.

**Target groups in short** — the object that does most of the actual work, and the one you tune:
- **What it is:** a named group of targets **plus their health check and traffic behaviour**. It exists independently of the load balancer — one target group can be used by several load balancers, and one load balancer can route to many target groups.
- **Target types:** `instance` (register by instance ID) · **`ip`** (any IP in the VPC, or on-prem via Direct Connect/VPN — **and the type required for Fargate and `awsvpc` ECS tasks**, since those have their own ENIs) · `lambda` (ALB only) · `alb` (an NLB fronting an ALB, to combine a static IP with Layer-7 routing).
- **Health check** is configured here, not on the load balancer: protocol, path, **port** (`traffic-port` or an override — e.g. app on 8080, health endpoint on 8081), interval, timeout, healthy/unhealthy thresholds, and **success codes** (the *matcher*, default `200`, often widened to `200-299`).
- **Three attributes worth knowing beyond [stickiness](#sticky-sessions-session-affinity) and [deregistration delay](#connection-draining--deregistration-delay):**
  - **Slow start** — ramps traffic to a *new* target over 30–900s instead of giving it a full share immediately. **Off by default, and genuinely useful for .NET/JVM**, where a fresh process has a cold JIT and empty caches: without it, the new target's p99 spikes or it fails health checks under full load the moment it joins.
  - **Load balancing algorithm** — `round_robin` (default) vs **`least_outstanding_requests`**. Round robin will happily hand a request to a target already stuck on a slow one; LOR is the better choice whenever request durations vary a lot.
  - **Protocol version** — HTTP1 / HTTP2 / **gRPC**, set on the target group; a gRPC backend needs this or it simply won't work.
- **Target states:** `initial` → `healthy` / `unhealthy` → `draining` (being removed, finishing in-flight requests) → `unused`. Reading these is usually the fastest way to diagnose "the ALB returns 503".
- **Weighted target groups:** a listener rule can forward across **multiple target groups by weight** — which is canary/blue-green **at the load balancer**, and the mechanism CodeDeploy uses for ECS blue/green deployments.

For the ALB/NLB/GWLB/CLB comparison table and the ALB-vs-API-Gateway decision, see [ALB vs API Gateway vs ELB](#alb-vs-api-gateway-vs-elb-nlbgwlbclb). The specifics worth adding here:

**ALB (Layer 7)** — routes on HTTP content: host, path, header, query string, method, source IP. Supports **SNI** (many TLS certificates on one listener), HTTP/2, gRPC, WebSocket, **Lambda targets**, native redirect and fixed-response actions, and built-in **Cognito/OIDC authentication** at the listener. Because it terminates HTTP, the original client IP arrives in the **`X-Forwarded-For`** header (plus `X-Forwarded-Proto` and `-Port`) — in ASP.NET Core you must enable `ForwardedHeadersMiddleware` or every client will look like the load balancer, which silently breaks rate limiting, geo-logic, and audit logs.

**NLB (Layer 4)** — millions of requests per second at ultra-low latency. Two properties that decide questions: it can have **one static IP per AZ (and supports Elastic IPs)**, and it **preserves the client source IP** with no header needed. Can pass TLS straight through to targets, or terminate it. Targets can be instances, IPs (including on-prem via Direct Connect), or even an ALB.

**❗ An ALB has no static IP — only a DNS name**, and the IPs behind it change. If a client or partner firewall requires a fixed IP, the answer is **NLB**, **NLB fronting an ALB**, or **Global Accelerator** (see [Global Accelerator](#aws-global-accelerator)). This is one of the most commonly asked ELB questions.

**Gateway Load Balancer — GWLB (Layer 3)** — transparently routes all traffic through a fleet of third-party inspection appliances (firewall, IDS/IPS) using **GENEVE on port 6081**, preserving the original packet. It's a traffic-inspection insertion point, not a conventional load balancer. Pair it with [AWS Network Firewall](#aws-network-firewall) as the AWS-native alternative.

**Classic Load Balancer — CLB** — the legacy Layer 4/7 balancer, now deprecated. It predates target groups, so it registers instances directly and supports neither host/path routing, SNI multi-certificate listeners, nor Lambda targets. The only correct answer for "we're on EC2-Classic"; otherwise migrate to ALB (HTTP) or NLB (TCP/UDP). Note the terminology difference it leaves behind: CLB calls it **"connection draining"**, ALB/NLB call the same thing **"deregistration delay"** (see [Connection Draining](#connection-draining--deregistration-delay)).

**Health checks:** protocol, port, and path (`/health`), plus interval, timeout, and healthy/unhealthy thresholds. A target failing the threshold stops receiving traffic and — if the ASG is configured for it — gets replaced. **Point the check at an endpoint that actually exercises the app's dependencies** (a `/health` that only returns `200 OK` from Kestrel will happily report healthy while the database connection pool is exhausted). ASP.NET Core's `AddHealthChecks()` with DB/cache probes is the right implementation.

### Sticky Sessions (Session Affinity)

**What it does:** pins a given client to the same target for the duration of a session, so in-process session state stays valid.

| Load balancer | Mechanism |
|---|---|
| **ALB** | **Duration-based** — LB-generated `AWSALB` cookie with a configurable duration (1 second to 7 days); or **application-based** — the LB honours *your* app's cookie via `AWSALBAPP`, so the app controls session lifetime |
| CLB | `AWSELB` cookie, or an application cookie |
| **NLB** | No cookies (it's Layer 4) — stickiness is by **source IP / flow hash** per target group |

**The trade-offs to name:** stickiness undermines even load distribution (one pinned client can hot-spot a target), and when a target is removed on scale-in or deploy, those sessions are **lost anyway** — so it never actually guarantees session survival.

**The senior answer:** stickiness is a workaround for a stateful app tier. The real fix is to externalise session state so any instance can serve any request — **ElastiCache for Redis** or **DynamoDB**. In .NET that's `IDistributedCache` (`AddStackExchangeRedisCache`) instead of in-process `ISession`. Sticky sessions are legitimate for legacy applications you can't refactor, or where a genuinely expensive per-user in-memory context makes affinity worth the cost — say that rather than declaring stickiness simply "bad".

### Connection Draining / Deregistration Delay

Same feature, two names: **"connection draining"** on CLB, **"deregistration delay"** on ALB/NLB target groups.

**What happens:** when a target is being removed (scale-in, deploy, manual deregistration) it enters `draining` state — **no new requests are routed to it**, but in-flight requests are allowed to complete for up to the configured delay. After that, remaining connections are closed.

- Default **300 seconds**; configurable **0–3600**.
- Tune it to your **longest legitimate request**. Set it too low and you cut off large file uploads or slow reports mid-flight (users see 502/504); set it too high and every deploy and scale-in crawls.
- Set it to `0` only for workloads with genuinely instantaneous requests where deploy speed matters more.

**"How do you deploy without dropping requests?"** — the complete answer chains three things: an appropriate **deregistration delay**, **ELB health checks** on the ASG so a bad instance is caught, and **ASG lifecycle hooks** (below) so an instance is warmed before it serves and drained before it dies. Add **ASG instance refresh** for a rolling replacement.

### Auto Scaling Groups (ASG)

**What it gives you:** maintains a target number of instances, replaces failed ones automatically, spans multiple AZs (that's the HA part), and registers/deregisters targets with the load balancer as it scales.

**Core settings:** **minimum** (never fewer), **desired** (current target, what scaling policies change), **maximum** (never more, and your cost ceiling). Instances launch from a **launch template** — the modern replacement for the deprecated *launch configuration*; launch templates support versioning, mixed instance types, and mixed On-Demand/Spot.

**❗ The health-check gotcha:** an ASG's health-check type defaults to **EC2 only**, which means it only replaces instances whose *hypervisor-level* status checks fail. An instance whose application has hung — or is returning 500s — looks perfectly healthy to EC2 and is **never replaced**, even while the ALB has already stopped sending it traffic. You must set the health check type to **ELB** so the ASG acts on the load balancer's application-level view. This is a genuine production incident pattern and a very common interview question.

**Scaling policies:**
| Policy | How it works | When to use |
|---|---|---|
| **Target tracking** | "Keep this metric at this value" — e.g. average CPU at 40%. AWS creates and manages the alarms | ✅ **The default recommendation.** Simplest and handles most cases |
| **Step scaling** | CloudWatch alarm → add/remove N instances, with different steps by alarm severity | When you need aggressive response to big breaches (e.g. +1 at 60% CPU, +4 at 85%) |
| **Simple scaling** | One alarm → one adjustment, then wait for cooldown | Legacy; step scaling supersedes it |
| **Scheduled scaling** | Change min/desired/max at a specific time | Known patterns: business hours, a marketing launch, month-end batch |
| **Predictive scaling** | ML on historical traffic, scales **ahead** of forecast demand | Cyclical daily/weekly traffic where reactive scaling is always a few minutes late |

**What metric to scale on — the senior differentiator.** CPU is the default and often the wrong signal. Better choices:
- **`ALBRequestCountPerTarget`** for a web/API tier — directly proportional to demand, and it reacts before CPU does.
- **SQS queue depth** for a worker tier — specifically **backlog per instance** (`ApproximateNumberOfMessagesVisible` ÷ running instances) as a target-tracking metric. This is the canonical answer for "how would you scale a queue-consuming service?", and it's much better than CPU because a worker blocked on I/O shows low CPU while the backlog grows.
- Custom application metrics (p99 latency, active connections, thread-pool saturation) published to CloudWatch.

**The rest of the ASG surface worth knowing:**
- **Cooldown / warm-up** — a pause after a scaling action so metrics can settle before the next one, preventing thrash. Target tracking uses *instance warm-up* instead: new instances aren't counted in the metric until they're actually ready.
- **Lifecycle hooks** — pause an instance in `Pending:Wait` (bootstrap, warm caches, register with a service, run smoke tests) or `Terminating:Wait` (drain connections, flush logs, deregister) before it proceeds. The hook for "do something custom on the way in or out".
- **Termination policy** — default order: the AZ with the most instances first, then the oldest launch template/configuration, then the instance closest to the next billing hour. Configurable, and `OldestInstance` is common when you want gradual rotation.
- **Instance refresh** — rolling replacement of every instance with a new launch-template version (e.g. a patched AMI), honouring a minimum healthy percentage. How you ship AMI updates without downtime.
- **Scale-in protection** — exclude specific instances from scale-in, for a node holding work that can't be interrupted.
- **Warm pools** — keep pre-initialised, stopped instances ready so scale-out skips boot and bootstrap time; the answer for apps with slow startup where predictive scaling isn't enough.

```bash
aws autoscaling create-auto-scaling-group --auto-scaling-group-name web-asg \
  --launch-template LaunchTemplateName=web-lt,Version='$Latest' \
  --min-size 2 --max-size 10 --desired-capacity 2 \
  --vpc-zone-identifier "subnet-a,subnet-b,subnet-c" \
  --target-group-arns arn:aws:elasticloadbalancing:... \
  --health-check-type ELB --health-check-grace-period 120     # ← ELB, not EC2
aws autoscaling start-instance-refresh --auto-scaling-group-name web-asg
```

### Scalability Best Practices

- **Make the app tier stateless** — no in-process session, no local file writes that matter. Everything else on this list depends on it.
- **Scale out, not up**, for anything that can be distributed; reserve vertical scaling for the things that can't (databases, single-node caches).
- **Minimum 2 instances across at least 2 AZs** (3 is better) — a single instance is not highly available regardless of instance type.
- **Set the ASG health check type to ELB**, and make `/health` genuinely check dependencies.
- **Scale on a demand-correlated metric** (requests per target, queue backlog), not CPU by reflex.
- **Externalise session and cache state** to ElastiCache/DynamoDB.
- **Use scheduled or predictive scaling for known events** — reactive scaling always lags a spike by the boot time of an instance.
- **Test scale-in, not just scale-out.** Most scaling bugs (dropped requests, lost work, orphaned locks) surface on the way *down*, which is what deregistration delay and lifecycle hooks exist for.
- **Cap `max` deliberately** — it's both a cost ceiling and a blast-radius limit if a bug or an attack drives artificial load.

### Scalability & Load Balancing Shared Responsibility Model

| AWS is responsible for | You are responsible for |
|---|---|
| Running and scaling the ELB fleet itself (multi-AZ, patched, no capacity to manage) | **Choosing the right LB type** (ALB/NLB/GWLB) for the protocol and requirements |
| The ASG control plane — replacing failed instances, honouring your policies | **Setting min/desired/max, the scaling policy, and the metric that drives it** |
| Health-check infrastructure | **Writing a health endpoint that reflects real application health** |
| AZ-level infrastructure availability | **Actually spanning multiple AZs** — AWS won't do it for you |
| TLS termination capability, managed certificates via ACM | Certificate lifecycle, cipher/TLS policy selection, and HTTPS redirect rules |
| Providing lifecycle hooks and deregistration delay | Tuning them so deploys and scale-in don't drop in-flight requests |
| — | **Designing the app to be stateless** so horizontal scaling is possible at all |

---

## Messaging, Streaming & Decoupling

### SQS & SNS Fundamentals

**SQS (Simple Queue Service) — pull-based queue**
- Decouples producers/consumers asynchronously; at-least-once delivery; durable (replicated across AZs); scales automatically.
- Flow: producer sends → stored durably → consumer polls → message hidden via **visibility timeout** while processing → on success, consumer deletes it → on failure, it becomes visible again → after `maxReceiveCount` retries, moves to **DLQ**.

| | Standard Queue | FIFO Queue |
|---|---|---|
| Delivery | At-least-once, possible duplicates | Exactly-once processing (with dedup) |
| Ordering | Not guaranteed | Guaranteed (per Message Group) |
| Throughput | Very high | Lower (throughput quota per message group, though high-throughput mode raises this) |
| Use case | Most workloads | Orders, payments, anything requiring strict per-entity order |

- **Long polling** (`WaitTimeSeconds` up to 20s): reduces empty-receive cost and improves latency vs short polling. **Short polling** samples only a subset of servers, so it can return empty even when messages exist — long polling should be the default, and setting it at the queue level (`ReceiveMessageWaitTimeSeconds`) is cheaper than paying for empty receives.

**SQS limits and knobs that get asked precisely:**
| Setting | Detail |
|---|---|
| **Message size** | **256 KB maximum.** For larger payloads use the **SQS Extended Client Library** — it stores the body in **S3** and puts a pointer in the message. The "claim check" pattern; the same applies to SNS |
| **Retention** | **4 days by default**, configurable **60 seconds to 14 days**. Messages are deleted after this whether processed or not |
| **Visibility timeout** | Default 30 s, max 12 h. Must exceed your worst-case processing time or the message reappears and gets processed twice. For variable work, call **`ChangeMessageVisibility`** to extend the lease mid-processing (a "heartbeat") rather than setting one huge global timeout |
| **Delay queue** | Delays **new** messages for 0–15 minutes *before they become visible at all*. **Not the same as visibility timeout**, which hides a message *after* it's received — that distinction is a favourite question |
| **DLQ + redrive** | After `maxReceiveCount` failed receives, the message moves to the DLQ. **Redrive** moves them back to the source queue once the bug is fixed — so name "fix, then redrive" rather than just "it goes to a DLQ" |
| **FIFO dedup** | Either a content-based hash or an explicit `MessageDeduplicationId`, over a **5-minute** dedup window. `MessageGroupId` is what defines the ordering scope — one slow group does not block others |
| **Encryption / access** | SSE-KMS at rest, and a **queue policy** (resource-based) for cross-account or SNS/S3/EventBridge access |

**SNS features worth naming:**
- **❗ Message filtering (filter policies)** — subscribers declare a JSON filter on message **attributes** (or, with payload-based filtering, the body), so a subscriber receives only the events it cares about. This is the answer to "how do you avoid every consumer receiving every event and filtering in code?" — filtering at the topic saves invocations, cost, and consumer complexity. A notable gap in most candidates' answers.
- **FIFO topics** — ordered, deduplicated fan-out, but they can only deliver to **SQS FIFO queues**.
- **Delivery retry policies** per protocol, plus a **subscription-level DLQ** for messages SNS can't deliver.
- **Message size 256 KB**, same as SQS, with the same S3 claim-check workaround.
- Cross-region and cross-account delivery, and `MessageStructure=json` for protocol-specific payloads (a different body for SMS than for SQS).

**SNS (Simple Notification Service) — push-based pub/sub**
- Publisher → topic → SNS pushes to *all* subscribers (fan-out) instantly. Subscribers: SQS, Lambda, HTTP(S), email/SMS, mobile push.
- Delivery retries use exponential backoff on the SNS side, but **durability and retry semantics for asynchronous processing should come from SQS**, not SNS alone — SNS→Lambda direct has no built-in DLQ-backed retry buffer the way SNS→SQS→consumer does.

| Feature | SNS | SQS |
|---|---|---|
| Pattern | Pub/Sub | Queue |
| Delivery | Push | Pull |
| Use case | Fan-out, notifications | Decoupling, buffering, guaranteed processing |
| Real-time | Yes | No (polling-based) |
| Ordering | No (Standard) | FIFO optional |
| Durability | Depends on subscriber/retry policy | High (queue itself is durable) |

**SNS → SQS fan-out pattern** (the most common AWS microservices pattern in the notes): one publisher emits a domain event (`order_created`) to an SNS topic; each interested microservice (Billing, Notification, Analytics, Inventory) has its **own** SQS queue subscribed to that topic. Benefits: no message loss if one consumer is down, independent scaling/failure per consumer, no risk of one slow consumer blocking others.

**When to use SQS alone:** async processing, retry+DLQ, buffering bursty load, worker-pool consumption (file processing, transcoding, batch/ETL).

**When to use SNS alone:** broadcasting to multiple heterogeneous consumer types, low-latency pub/sub, email/SMS/mobile notifications.

**When to combine (recommended default for event-driven microservices):** SNS gives fan-out; SQS gives durability, retries, and per-consumer isolation.

### CQRS with SNS/SQS in .NET

**Golden rule:** publish the domain event to SNS **only after** the write-side DB transaction commits successfully — never publish speculatively before commit.

```csharp
public record OrderCreatedEvent(Guid EventId, Guid OrderId, decimal Amount, DateTime CreatedAt);

public class SnsPublisher
{
    private readonly IAmazonSimpleNotificationService _sns;
    private readonly string _topicArn;

    public SnsPublisher(IAmazonSimpleNotificationService sns, IConfiguration config)
    {
        _sns = sns;
        _topicArn = config["AWS:SNS:OrderCreatedTopicArn"];
    }

    public async Task PublishAsync(OrderCreatedEvent evt)
    {
        var message = JsonSerializer.Serialize(evt);
        var request = new PublishRequest
        {
            TopicArn = _topicArn,
            Message = message,
            MessageAttributes =
            {
                ["eventType"] = new MessageAttributeValue { DataType = "String", StringValue = "OrderCreated" }
            }
        };
        await _sns.PublishAsync(request);
    }
}

// Command handler
public async Task CreateOrderAsync(CreateOrderCommand cmd)
{
    await _db.SaveChangesAsync();               // 1. commit write DB first
    await _snsPublisher.PublishAsync(            // 2. publish AFTER commit succeeds
        new OrderCreatedEvent(Guid.NewGuid(), cmd.OrderId, cmd.Amount, DateTime.UtcNow));
}
```

**Consumer (BackgroundService) with the critical SNS envelope detail:**

```csharp
public class OrderCreatedConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly string _queueUrl;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = 5,
                WaitTimeSeconds = 20 // long polling
            });

            foreach (var message in response.Messages)
            {
                try
                {
                    await ProcessMessageAsync(message);
                    await _sqs.DeleteMessageAsync(_queueUrl, message.ReceiptHandle); // delete only after success
                }
                catch (Exception ex)
                {
                    // do NOT delete — SQS will retry, then DLQ after maxReceiveCount
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }

    private async Task ProcessMessageAsync(Message message)
    {
        // SNS wraps the real payload inside an envelope — a classic interview detail
        var snsEnvelope = JsonSerializer.Deserialize<SnsEnvelope>(message.Body);
        var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(snsEnvelope.Message);

        if (await AlreadyProcessed(evt.EventId)) return;   // idempotency check — mandatory
        await UpdateReadDatabase(evt);
    }
}

public class SnsEnvelope
{
    public string Type { get; set; }
    public string Message { get; set; }
    public string MessageId { get; set; }
    public string TopicArn { get; set; }
}
```

**Idempotency tracking:**
```csharp
private async Task<bool> AlreadyProcessed(Guid eventId) =>
    await _db.ProcessedEvents.AnyAsync(x => x.EventId == eventId);
```

**DLQ mechanics:** configure `maxReceiveCount` (e.g., 5); after that many failed receives without deletion, SQS auto-moves the message to the configured DLQ. Recovery is inspect → fix root cause → redrive to source queue (console or automation).

**Registration (`Program.cs`):**
```csharp
builder.Services.AddAWSService<IAmazonSimpleNotificationService>();
builder.Services.AddAWSService<IAmazonSQS>();
builder.Services.AddSingleton<SnsPublisher>();
builder.Services.AddHostedService<OrderCreatedConsumer>();
```

**Common interview traps for this pattern:** publishing to SNS *before* DB commit; sharing one SQS queue across multiple unrelated consumers; forgetting to unwrap the SNS envelope; no idempotency tracking; deleting the SQS message before processing completes (guarantees message loss on crash).

### CQRS + SNS/SQS Interview Pitfalls

| Pitfall / Question | Wrong instinct | Correct senior answer |
|---|---|---|
| "Read DB should reflect writes immediately" | Expecting strong consistency | CQRS is eventually consistent by design; UI should show pending/optimistic state, use polling/WebSocket/read-your-own-write cache |
| "Why not SQS directly instead of SNS?" | "One queue is enough" | SNS decouples producer from N consumers; each gets its own queue and fails/scales independently |
| "SNS published but DB commit failed" | Publish first, DB later | Publish only after commit; for stronger guarantees use the **Outbox Pattern** (write event + data in the same DB transaction, a separate relay publishes it) |
| "Same message processed twice" | "AWS guarantees once" | SQS is at-least-once; consumers must be idempotent (EventId tracking, upserts, or FIFO + `MessageDeduplicationId`) |
| "What causes DLQ delivery?" | "DLQ is manual" | Consumer exception, message not deleted, `maxReceiveCount` exceeded, or visibility timeout misconfiguration |
| "Delete before or after processing?" | Delete first to dedupe | Delete only **after** success — deleting first risks silent data loss on crash |
| "Why one queue per consumer?" | Shared queue is simpler | Shared queues cause consumer interference and coupled scaling/retry behavior |
| "How do you version events?" | Change schema in place | Version explicitly (`OrderCreated_v2`) or keep changes backward-compatible; events are contracts |
| "Can commands be async like events?" | Everything async | Commands are synchronous (caller needs a result); only side-effect events are async |
| "Why not DB triggers instead of events?" | "Triggers are simpler" | Triggers are invisible, hard to version, non-portable; explicit events are observable and testable |

**60-second wrap-up answer:** "The biggest pitfalls in CQRS with SNS/SQS are assuming immediate consistency, exactly-once delivery, or shared queues. Events must publish only after successful commits, consumers must be idempotent, and each consumer needs its own queue with a DLQ. Ordering, retries, and replay have to be designed explicitly — otherwise you get silent data loss or duplicate side effects."

### [new content] EventBridge Deep Dive

The original notes mention EventBridge only in passing (as "CloudWatch Events renamed" and as a Lambda trigger) — given how central EventBridge is to modern event-driven .NET architectures on AWS, it deserves its own treatment.

**What it adds over plain SNS/SQS:**
- **Event buses** — default bus (AWS service events), custom buses (your application events), and partner buses (SaaS integrations like Stripe, Auth0, PagerDuty, Datadog).
- **Content-based filtering** at the rule level — route based on JSON event pattern matching (field values, prefixes, numeric ranges) without writing filtering code in every consumer.
- **Schema Registry** — discover and version event schemas, generate strongly-typed code bindings (including for .NET) from a schema.
- **Archive & Replay** — record all events matching a pattern and replay them later (very useful for reprocessing after a downstream bug fix, without needing to keep messages in SQS indefinitely).
- **Targets**: Lambda, SQS, SNS, Step Functions, ECS RunTask, Kinesis, API destinations (arbitrary HTTPS endpoints with managed retries — great for calling third-party/legacy .NET webhooks).

**EventBridge vs SNS — when to pick which**
| | EventBridge | SNS |
|---|---|---|
| Routing logic | Rich content-based filtering per rule | Coarse (topic-level, optional filter policies on subscriptions) |
| Schema management | Built-in registry/versioning | None |
| Scheduled/cron | Native (`schedule: cron(...)`) | No |
| SaaS source integration | Native partner event buses | No |
| Latency | Slightly higher (near-real-time, not always sub-second) | Real-time |
| Simplicity | More moving parts | Simpler mental model |

**.NET example — scheduled cleanup rule (CLI/CloudFormation snippet):**
```yaml
Resources:
  NightlyCleanupRule:
    Type: AWS::Events::Rule
    Properties:
      ScheduleExpression: "cron(0 1 * * ? *)"
      Targets:
        - Arn: !GetAtt CleanupFunction.Arn
          Id: "CleanupTarget"
```

**Interview takeaway:** EventBridge is the right default for **cross-service domain events with routing logic** and scheduled jobs; SNS+SQS remains the right default for simple, high-throughput fan-out where you don't need content filtering or a schema registry. Many real architectures use both — EventBridge for coarse routing between bounded contexts, SNS/SQS within a bounded context for fan-out to same-team consumers.

### [new content] Event-Driven Architecture Reference Flow

```
  Client
    |  POST /orders
    v
  API Gateway
    |  invoke
    v
  Lambda (Ingest) ----PutItem status=PENDING + idempotencyKey----> DynamoDB
    |
    |  Publish "order_created"
    v
  SNS Topic
    +----fan-out----> SQS (Billing) --------+
    +----fan-out----> SQS (Notification)    |
                                            |  poll & invoke
                                            v
                                    Lambda (Processor)
                                            |
                +---------------------------+---------------------------+
                v                           v                           v
      ConditionalUpdate            Update status =            after maxReceiveCount
      PENDING -> PROCESSING        COMPLETED                  failures -> DLQ
           (DynamoDB)                (DynamoDB)

  Every downstream step must be idempotent: SQS is at-least-once, so the
  same message can legitimately arrive twice.
```

This single diagram ties together the order-processing pattern from the original notes (Lambda + SQS + DynamoDB) with the SNS fan-out pattern — the two were documented separately in the source material but are almost always combined in real systems, and interviewers expect you to explain how they compose.

**Production readiness checklist (from the original notes, preserved in full — this is genuinely senior-level and worth keeping verbatim as a mental checklist):**
- Idempotency on ingest (dedupe via idempotency key)
- DLQ + alerting configured (CloudWatch alarm on DLQ depth > 0)
- Conditional updates in DynamoDB to prevent races
- Monitoring/alarms on queue depth, Lambda errors, DynamoDB throttling
- Least-privilege IAM + SSE-KMS encryption on DynamoDB/SQS
- Load testing for throughput/throttling behavior
- On-demand or autoscaled DynamoDB capacity
- SQS visibility timeout > Lambda max execution time
- Idempotent external integrations (payment/fulfillment)
- X-Ray/OpenTelemetry tracing end-to-end
- Documented operational runbooks for DLQ handling/replay
- Cost monitoring/budget alerts
- IAM Access Analyzer + secret scanning
- Backup/PITR + tested restore
- Documented schema evolution strategy

### Amazon Kinesis

**What it's for:** real-time **streaming** data at scale — clickstreams, IoT telemetry, application logs, metrics, change feeds. The distinguishing feature versus SQS is that a stream is a **replayable, ordered log** that **multiple independent consumers** can each read in full, rather than a queue where a message is consumed once and deleted.

**The four family members:**
| Service | What it does |
|---|---|
| **Kinesis Data Streams** | The raw, low-level stream you build consumers against. Real-time (~200 ms), replayable, ordered per shard |
| **Data Firehose** | Fully managed **delivery** — no code, no shards. Buffers and loads straight into S3, Redshift, OpenSearch, Splunk, or an HTTP endpoint |
| **Managed Service for Apache Flink** (formerly Kinesis Data Analytics) | SQL or Flink processing *over* a stream — windowed aggregations, anomaly detection, enrichment |
| **Kinesis Video Streams** | Video ingestion for playback and ML |

**Data Streams mechanics — this is where the detail questions live:**
- Data lives in **shards**. Throughput is per shard: **1 MB/s or 1,000 records/s in**, and **2 MB/s out** shared across consumers — or **2 MB/s per consumer** with **Enhanced Fan-Out** (a push model, ~70 ms latency, up to 20 consumers).
- The **partition key** decides which shard a record lands on, and **ordering is guaranteed only within a shard**. So pick a partition key that both distributes evenly *and* keeps records that must stay in order together (e.g. `customerId`). A low-cardinality key creates a **hot shard** — exactly the same failure shape as a DynamoDB hot partition.
- **Retention** is 24 hours by default, extendable to **365 days**. Reading a record does not delete it, which is what makes **replay** possible — reprocess a week of events after fixing a consumer bug.
- **Capacity modes:** *Provisioned* (you manage shard count, cheaper at steady scale) or **On-Demand** (auto-scales, pay per throughput — the right default when the load is unknown).
- Consumers use the **KCL** (or a Lambda event-source mapping) and **checkpoint** their position; a Lambda consumer processes shards in parallel, one concurrent invocation per shard.

**Firehose vs Data Streams** is the pairing that gets asked: Firehose is **near**-real-time (it buffers by size, e.g. 1–128 MB, or time, e.g. 60 s), **serverless with no shards to manage**, can transform records with a Lambda and convert to Parquet/ORC on the way, and **cannot replay** — once delivered, it's gone from Firehose. Data Streams is true real-time, replayable, and requires you to manage shards/consumers. "Just get this data into S3/OpenSearch reliably with no code" → **Firehose**. "Multiple consumers, replay, sub-second, custom processing" → **Data Streams**.

**The decision table interviewers are really after:**
| | SQS | SNS | EventBridge | Kinesis Data Streams |
|---|---|---|---|---|
| Model | Queue — one consumer group, message deleted after processing | Pub/sub push, fan-out to subscribers | Event bus with content-based routing rules | Ordered, replayable stream log |
| Consumers | Competing consumers share the work | Each subscriber gets a copy | Each matching rule gets a copy | **Many independent consumers each read everything** |
| Ordering | FIFO queues only | No | No | **Yes, per shard** |
| Replay | ❌ (once deleted, gone) | ❌ | ✅ via Archive & Replay | ✅ **within the retention window** |
| Retention | Up to 14 days | N/A (no storage) | Archive-based | Up to **365 days** |
| Throughput shape | Effectively unlimited, per-message | Per-message | Per-event | **Provisioned per shard**, high volume |
| Reach for it when | Decoupling work, buffering, retries + DLQ | Simple fan-out notifications | SaaS/AWS-service events, filtering, schemas | High-volume analytics, multiple readers, replay |

### Step Functions: Orchestration vs Choreography

**What it is:** a managed **state machine** that coordinates multiple services into a workflow, defined declaratively in **ASL (Amazon States Language)** JSON. It handles sequencing, branching, parallelism, retries, error handling, timeouts, and human approval steps — so that logic lives in *configuration with a visual execution history*, not buried in glue code.

**Why it matters as an architectural answer:** without it, multi-step business processes end up as Lambdas invoking Lambdas, with retry and compensation logic hand-rolled in each one and no way to see where an execution actually failed.

**State types to be able to list:** `Task` (do work), `Choice` (branch), `Parallel` (fan out and join), `Map` (iterate over a collection — **Distributed Map** scales to millions of items, e.g. one execution per S3 object), `Wait`, `Pass`, `Succeed`, `Fail`.

**Standard vs Express — the comparison that gets asked:**
| | **Standard** | **Express** |
|---|---|---|
| Max duration | **1 year** | **5 minutes** |
| Execution model | Exactly-once, fully durable | At-least-once |
| History | Full visual history retained (90 days) | CloudWatch Logs only |
| Pricing | **Per state transition** | Per request + duration (much cheaper at high volume) |
| Use for | Long-running business processes, order fulfilment, human approval, ETL | High-volume, short-lived event processing (streaming, IoT ingestion) |

**The features that make it worth choosing:**
- **Built-in `Retry` and `Catch`** per state, with backoff rate, max attempts, and error-type matching — declarative resilience instead of Polly in every function.
- **Over 200 direct SDK integrations** — call DynamoDB, SQS, ECS `RunTask`, SNS, Lambda, even another state machine, **without writing a Lambda** just to marshal the call. This removes whole classes of "glue Lambda".
- **`.sync` / callback patterns** — wait for an ECS task or Glue job to finish, or pause for a **task token** until an external system (or a human) calls back. That's how approval workflows are built.
- **The Saga pattern** — for distributed transactions across services, `Catch` on each step triggers **compensating actions** (refund the payment, release the inventory). This is the standard answer to "how do you do transactions across microservices when there's no two-phase commit?"

**Orchestration vs choreography — the senior framing to lead with:**
| | **Orchestration** (Step Functions) | **Choreography** (EventBridge / SNS+SQS) |
|---|---|---|
| Control | A central coordinator holds the process | Each service reacts to events independently |
| Visibility | ✅ One place shows the whole flow and where it failed | ❌ Flow is emergent — you must trace across services |
| Coupling | Coordinator knows all participants | Publishers don't know subscribers exist |
| Change cost | Update one definition | Add a subscriber without touching anyone |
| Best for | A defined business process with ordering, compensation, and a known end state | Loose, extensible fan-out where new consumers appear over time |

**The answer that scores:** "I'd use both, at different layers. Choreography (EventBridge/SNS) between bounded contexts, so teams add consumers without coordination. Orchestration (Step Functions) *within* a bounded context for a multi-step process that needs ordering, compensation, and an auditable execution history — because debugging a failed order in a purely event-driven chain means reconstructing the flow from logs across six services, whereas Step Functions shows me the exact failed state."

**Related orchestration/eventing pieces worth naming:** **EventBridge Pipes** (a point-to-point source→filter→enrich→target connector, replacing the Lambda you'd otherwise write to move SQS→Step Functions), **EventBridge Scheduler** (managed cron at scale, superseding CloudWatch Events scheduled rules), and **AWS Batch** (managed batch compute for long-running, non-container-native jobs — the right home for work exceeding Lambda's 15 minutes when you don't want a full ECS service).

### Amazon MQ

**Managed Apache ActiveMQ or RabbitMQ.** The reason it exists — and the only reason to choose it over SQS/SNS — is **protocol compatibility**: it speaks the **open standards** existing enterprise applications already use (**AMQP 0-9-1/1.0, MQTT, STOMP, OpenWire, JMS, WSS**), whereas SQS and SNS expose proprietary AWS APIs.

- Runs as **brokers on instances inside your VPC** (not serverless), with **Multi-AZ active/standby failover** and durable storage.
- Because it's broker-based, it **does not scale like SQS** — you size and monitor brokers, and throughput has a ceiling.
- Supports the messaging semantics legacy apps expect: topics *and* queues, message selectors, transactions, and request/reply.

**The interview answer:** "If I'm **lift-and-shifting** an on-prem application that already speaks JMS/AMQP/MQTT and I don't want to rewrite its messaging layer, Amazon MQ is the migration path. If I'm building something new on AWS, I use SQS/SNS/EventBridge instead — they're serverless, cheaper, and scale without broker management." Rewriting an app's messaging layer purely to adopt SQS during a migration is the mistake this service exists to avoid.

---

## Global Edge Services

> **The geography vocabulary first,** because answers depend on it: a **Region** is a geographic area (e.g. `us-east-1`); an **Availability Zone** is one or more discrete data centres within a region, isolated for failure but linked by low-latency fibre; an **Edge Location / Point of Presence** is one of 600+ CloudFront caches worldwide — far more numerous than regions, and used for caching and entry into the AWS backbone, not for running your workloads.

### CloudFront (CDN)

**What it is:** a global **content delivery network** — it caches your content at edge locations near users, so requests are served locally instead of travelling to your origin.

**What you actually get, beyond speed:**
- Lower latency and higher throughput for users far from your origin.
- **Origin offload** — cached hits never reach your ALB/S3/EC2, cutting both load and egress cost (CloudFront→origin traffic is free from AWS origins).
- **AWS Shield Standard built in**, plus **WAF** integration at the edge — so attacks are absorbed before reaching your infrastructure.
- **TLS termination at the edge** with a free ACM certificate, HTTP/2 and HTTP/3, and automatic compression.

**The object model:** a **Distribution** has one or more **Origins** (S3 bucket, ALB, API Gateway, EC2, MediaStore, or any HTTP server, including non-AWS), and **Cache Behaviours** that map path patterns (`/api/*`, `/static/*`) to an origin plus its own caching, header/cookie/query-string forwarding, and TLS settings. That's how one domain serves cached static assets from S3 and uncached `/api/*` from an ALB.

**Caching controls:** the **cache key** (which parts of the request make a response unique — forward as *little* as possible, since forwarding every header or cookie destroys the hit rate), **TTLs** (minimum/maximum/default, overridden by origin `Cache-Control` headers), and **invalidations** to purge paths early. Invalidations are billed beyond a free monthly allowance and are slow — **the better practice is versioned filenames or query strings** (`app.a1b2c3.js`) so new content has a new cache key and nothing needs purging.

**Securing the origin — `OAC`:** **Origin Access Control** (the modern replacement for the legacy **OAI**) lets CloudFront sign requests to a **private** S3 bucket, so the bucket stays fully closed to the internet and is readable only via your distribution. This is the correct pattern for static site hosting (see [S3 Static Website Hosting](#s3-static-website-hosting)), and OAC — unlike OAI — also supports SSE-KMS and all HTTP methods.

**Restricting access to content:**
| Mechanism | Use for |
|---|---|
| **Signed URL** | One specific file per URL — a single paid download, one report |
| **Signed Cookie** | **Multiple** files/whole sections without generating a URL each time — a subscriber's video library |
| **Geo restriction** | Allowlist or blocklist by country, for licensing or compliance |

**❗ CloudFront signed URL vs S3 presigned URL** is a frequently asked pairing: an **S3 presigned URL** is generated by the S3 API, carries the *generating principal's* IAM permissions, hits S3 directly, and bypasses CloudFront's cache and protections. A **CloudFront signed URL** is created with a CloudFront key pair, is served through the edge (so you keep caching, WAF, Shield, and logging), and can cover any origin type — and it lets you keep the bucket private. Use CloudFront signing when content is delivered *through* the CDN; use S3 presigning for direct, short-lived programmatic access such as browser uploads.

**Edge compute:**
| | **CloudFront Functions** | **Lambda@Edge** |
|---|---|---|
| Runtime | Lightweight JavaScript, sub-millisecond | Node.js / Python, up to 5–30 s |
| Runs at | Edge locations only | Regional edge caches |
| Triggers | Viewer request / viewer response | All four: viewer + **origin** request/response |
| Can call other services / network? | ❌ | ✅ |
| Use for | Header manipulation, URL rewrites, redirects, simple auth-token checks, A/B cookie assignment | Origin selection logic, calls to a database or API, image transformation, heavier auth |

Other details worth naming: **Price Classes** (All / 200 / 100) trade global coverage for cost by excluding the most expensive regions; **Origin Groups** give automatic origin failover; **field-level encryption** encrypts specific form fields (like a card number) with a public key so only the intended downstream service can read them; and **standard vs real-time logs** for analysis versus live monitoring.

**❗ The classic CloudFront gotcha:** a custom-domain certificate for CloudFront **must be requested/imported in `us-east-1`**, regardless of where your origin or users are — because CloudFront is a global service managed from N. Virginia. Certificates for an ALB, by contrast, must be in the ALB's own region.

### AWS Global Accelerator

**What it is:** two **static anycast IP addresses** that front your application. Client traffic enters the **AWS private global backbone** at the nearest edge location and travels internally to the closest healthy regional endpoint, instead of traversing the public internet.

- Works at **Layer 4 for TCP and UDP** — so it accelerates *any* protocol: gaming, VoIP, IoT, MQTT, custom TCP — not just HTTP.
- Endpoints are **ALBs, NLBs, EC2 instances, or Elastic IPs**, grouped by region. **Traffic dials** shift percentages between regions and **endpoint weights** within one; that's how you do controlled regional cutover or blue/green across regions.
- **Failover is fast (~30 seconds) and DNS-independent** — the IPs never change, so no client, resolver, or ISP TTL cache can delay it. That's its single biggest advantage over Route 53 latency/failover routing.
- Solves the "we need a **static IP** for a partner allowlist" problem that an ALB can't (see [Load Balancing Fundamentals](#load-balancing-fundamentals)).

### CloudFront vs Global Accelerator

Both are edge services on the same network, so the comparison gets asked constantly:

| | **CloudFront** | **Global Accelerator** |
|---|---|---|
| Layer | 7 (HTTP/HTTPS) | **4 (TCP/UDP — any protocol)** |
| Caches content? | ✅ **Yes** — that's the point | ❌ No caching, ever — it's pure network routing |
| Entry point | Edge location, resolved by DNS | **2 static anycast IPs** |
| Best for | Static assets, websites, video streaming, cacheable APIs | Non-HTTP protocols, gaming, VoIP, IoT; multi-region failover; static-IP requirements |
| Failover speed | Origin groups / DNS-dependent | **~30 s, no DNS dependency** |
| Improves cacheable content | ✅ Dramatically | Only via better network path |

**The one-liner:** "CloudFront caches HTTP content at the edge; Global Accelerator doesn't cache anything — it just gets any TCP/UDP traffic onto the AWS backbone sooner and gives me two static IPs with fast regional failover. Cacheable web content → CloudFront. Dynamic, non-HTTP, or static-IP/fast-failover requirements → Global Accelerator. They can be combined."

### Local Zones, Outposts & Wavelength

Three ways AWS extends a region beyond its own data centres — all answers to "we need AWS closer to something":

| | What it is | Use it for |
|---|---|---|
| **Local Zones** | An extension of a region placed in a **major metro area**, with a subset of services (EC2, EBS, ECS, some ELB/RDS). You opt in, then create a subnet in it | **Single-digit-millisecond latency** to end users in a specific city — real-time gaming, live video production, remote workstations |
| **Outposts** | Physical **AWS racks installed in your own data centre**, running the same APIs, managed by AWS | Data-residency mandates, or very low latency to on-prem systems (a factory floor, a hospital) that can't move to the cloud |
| **Wavelength Zones** | AWS compute embedded **inside 5G telco networks**, so traffic never leaves the carrier network | Ultra-low-latency mobile: AR/VR, connected vehicles, live mobile inference |

The framing that lands: "Regions and AZs cover most latency needs. Local Zones move compute into a metro; Outposts move it into *my* building; Wavelength moves it into the carrier's 5G network. Each is progressively more specialised — and progressively more expensive with fewer services available."

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
**Verification habits:** for CloudFront, check the **`X-Cache`** response header (`Hit from cloudfront` vs `Miss from cloudfront`) and watch the **cache hit ratio** metric — a low ratio almost always means you're forwarding too many headers/cookies/query strings into the cache key. For Global Accelerator, confirm the two static IPs resolve and that endpoint health is `HEALTHY` before shifting the traffic dial.

---

## Security Services

### Overview: Which Service Answers Which Question

The fastest way to sound organised here is to map services to questions rather than reciting a list:

| The question | The service |
|---|---|
| Who can do what? | **IAM** (see [IAM & Security](#iam--security)) |
| Is someone flooding me with traffic? | **Shield** (L3/4) + **WAF** (L7) |
| Is malicious traffic reaching my app? | **WAF**, **Network Firewall** |
| Are my keys managed properly? | **KMS**, **CloudHSM** |
| Are my certificates valid and renewing? | **ACM** |
| Where are my secrets? | **Secrets Manager** / Parameter Store (see [Secrets Manager vs Parameter Store](#new-content-secrets-manager-vs-parameter-store)) |
| **Is something bad happening right now?** | **GuardDuty** (threat detection) |
| **What weaknesses do I have?** | **Inspector** (vulnerability scanning) |
| **Where is my sensitive data?** | **Macie** |
| **Is anything misconfigured or drifting?** | **AWS Config** |
| Can I see everything in one place? | **Security Hub** |
| How did this incident actually happen? | **Detective** + **CloudTrail** |
| Can I prove AWS is compliant to my auditor? | **Artifact** |

### DDoS Protection: Shield & WAF

**The three attack shapes** worth naming before the services: **volumetric** (L3/4 — UDP reflection/amplification, SYN floods; goal is to saturate bandwidth), **protocol** (exploiting TCP/IP behaviour), and **application-layer** (L7 — HTTP floods, Slowloris; low bandwidth but expensive per request because each one hits your application and database).

**AWS Shield** — see also the Shield notes in [ELB Deep-Dive](#elb-deep-dive-cross-zone-load-balancing-504-timeouts--shield-ddos-protection).
- **Shield Standard**: free, automatically on for **every** account, protecting Route 53, CloudFront, Global Accelerator, and ELB against common L3/4 attacks.
- **Shield Advanced**: paid (~$3,000/month, org-wide), adding larger-scale mitigation, **24/7 access to the Shield Response Team (SRT)**, **cost-protection credits** for scaling charges incurred during an attack, health-based detection, and **WAF included at no extra charge**.

**AWS WAF** — the Layer-7 firewall. Attaches to **CloudFront, ALB, API Gateway, AppSync, and Cognito user pools** (note: **not** to an NLB, because WAF needs HTTP context — a common trick question).

- Structure: a **Web ACL** contains **rules** and **rule groups**, evaluated in priority order.
- **AWS Managed Rule Groups** cover most needs without writing rules: the **Core rule set (OWASP-style)**, SQL injection, known-bad inputs, **IP reputation**, **Anonymous IP** (Tor/VPN/proxy), and **Bot Control**.
- Rule types: IP set match, **geo match**, string/regex match, size constraint, SQLi/XSS detection, and **rate-based rules** — the built-in rate limiter (e.g. block any IP exceeding 2,000 requests per 5 minutes), which is the answer to "how do you stop credential stuffing or scraping?"
- Actions: **Allow**, **Block**, **Count**, **CAPTCHA**, **Challenge**.
- **❗ Best practice: deploy every new rule in `Count` mode first**, watch the logs to see what it *would* have blocked, then switch to `Block`. Going straight to Block is how teams take down their own legitimate traffic — say this and you sound like you've actually run WAF.
- Log to CloudWatch Logs / S3 / Firehose, and query with Athena.
- WAF is **regional**, except for CloudFront where the Web ACL is **global (created in `us-east-1`)**.

**AWS Firewall Manager** centrally applies WAF rules, Shield Advanced protections, security-group policies, and Network Firewall rules across **every account in an Organization** — and automatically to newly created resources. It's the answer to "how do you guarantee every account has the baseline WAF rules?"

**The canonical layered edge:** `Route 53 → CloudFront (Shield + WAF at the edge) → ALB (Shield) → private app tier`. Blocking an attack at CloudFront means it never consumes your ALB, compute, or database capacity.

### AWS Network Firewall

A **managed, stateful network firewall and IPS/IDS at the VPC level**, inspecting **all** traffic — not just HTTP.

- Capabilities: stateful traffic filtering, **domain-name filtering for egress** (allow `*.microsoft.com`, block everything else), protocol detection, and **Suricata-compatible IPS rules** for deep packet inspection.
- Deployment: a dedicated **firewall subnet in each AZ**, with route tables directing traffic through the firewall endpoints before it reaches an IGW/NAT.
- **Where it's the right answer:** controlled **egress filtering** for compliance ("workloads may only reach an approved allowlist of domains"), intrusion detection at the network layer, and inspecting traffic that a WAF can't see because it isn't HTTP.

**How it differs from everything else that filters traffic:**
| | Layer | Scope | Deny rules |
|---|---|---|---|
| **Security Group** | 4 | ENI/instance | ❌ Allow only |
| **NACL** | 4 | Subnet | ✅ but stateless, IP/port only |
| **Network Firewall** | 3–7 | **VPC**, all protocols | ✅ Stateful, domain names, IPS signatures |
| **WAF** | 7 | CloudFront/ALB/API GW | ✅ HTTP content-aware |
| **GWLB** | 3 | VPC | Insertion point for **third-party** appliances |

### KMS & CloudHSM

**KMS (Key Management Service)** — managed encryption keys backed by FIPS 140-validated HSMs, integrated with essentially every AWS service (S3, EBS, RDS, Secrets Manager, Lambda env vars…).

- **Key types:** *AWS owned* (invisible, shared), *AWS managed* (`aws/s3`, `aws/ebs` — free, auto-rotated, but you can't edit the policy), and **customer managed keys (CMKs)** — the ones you create, with your own **key policy**, optional **automatic annual rotation**, tags, and a mandatory **7–30 day waiting period before deletion** (deliberately, since deleting a key destroys all data encrypted with it).
- **❗ The key policy is mandatory and authoritative.** Unlike most resources, an IAM policy granting `kms:Decrypt` is **not sufficient on its own** — the key's own resource policy must also allow the principal (directly, or by delegating to IAM with the `kms:CallerAccount` pattern). "IAM says allow but KMS still denies" is the key policy. **Grants** are the temporary, programmatic alternative for service-to-service delegation.
- **Envelope encryption — know why it exists:** the `Encrypt` API can only handle **up to 4 KB** of data. So for anything larger, `GenerateDataKey` returns a plaintext data key plus an encrypted copy; you encrypt your data locally with the plaintext key, discard it, and store the encrypted key alongside the ciphertext. That's exactly what S3/EBS do internally, and it's why **KMS request quotas** matter at high throughput (hence S3 Bucket Keys — see [S3 Encryption](#s3-security-encryption--its-four-types)).
- **Multi-Region keys** replicate key material across regions so you can decrypt in region B what was encrypted in region A — needed for cross-region DR of encrypted data.
- Every KMS API call is logged in **CloudTrail**, which is the audit advantage of SSE-KMS over SSE-S3.

**CloudHSM** — **single-tenant, dedicated hardware** HSMs in your VPC.
| | **KMS** | **CloudHSM** |
|---|---|---|
| Tenancy | Multi-tenant, managed service | **Dedicated hardware, single tenant** |
| Key control | AWS manages the HSM; you control policy | **You** manage keys entirely — **AWS has no access and cannot recover them** |
| FIPS level | 140-2/3 validated | **140-2 Level 3** |
| Integration | Native with ~every AWS service | Via PKCS#11/JCE/CNG — mostly your own application |
| Use when | Default for everything | Regulatory mandate for exclusive key custody, custom crypto (e.g. SQL Server TDE with your own keys), or an offloaded CA |

**One-liner:** "KMS unless a regulator specifically requires that AWS cannot possibly access my keys — then CloudHSM, accepting that if I lose the keys, the data is gone."

#### Worked Example: Giving a Fargate Task Access to KMS-Encrypted S3 Data

A near-perfect interview scenario, because it needs **four** separate things to be true and candidates usually name one or two. *"My Fargate task can't read the bucket, but the IAM policy clearly allows `s3:GetObject`."*

**1. The TASK role — not the task execution role.** The execution role pulls the image and injects secrets; **your application code** runs under the **task role**. It needs S3 *and* KMS permissions:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    { "Sid": "ReadObjects", "Effect": "Allow",
      "Action": ["s3:GetObject", "s3:ListBucket"],
      "Resource": ["arn:aws:s3:::my-bucket", "arn:aws:s3:::my-bucket/*"] },

    { "Sid": "DecryptWithTheCmk", "Effect": "Allow",
      "Action": ["kms:Decrypt", "kms:DescribeKey"],
      "Resource": "arn:aws:kms:us-east-1:111122223333:key/1234abcd-...",
      "Condition": { "StringEquals": { "kms:ViaService": "s3.us-east-1.amazonaws.com" } } }
  ]
}
```
Writing objects additionally needs **`kms:GenerateDataKey`** (envelope encryption — see above), plus `s3:PutObject`. The **`kms:ViaService`** condition is the least-privilege touch: the role may use the key *only through S3*, never directly.

**2. ❗ The KMS KEY POLICY must also allow that role.** This is the step that's missed, and it's specific to KMS: the key's resource policy is **mandatory and authoritative**, so an IAM policy alone is *not sufficient* unless the key policy delegates to IAM.
```json
{
  "Sid": "AllowTaskRoleToDecrypt",
  "Effect": "Allow",
  "Principal": { "AWS": "arn:aws:iam::111122223333:role/my-app-task-role" },
  "Action": ["kms:Decrypt", "kms:DescribeKey"],
  "Resource": "*"
}
```
`"Resource": "*"` inside a key policy means *this key*. (The alternative is the standard "delegate to IAM" statement granting the account root `kms:*` — which is what lets IAM policies alone work.)

**3. The bucket policy**, if there is one. Same-account with no restrictive bucket policy → nothing needed. But a bucket policy that **denies** unencrypted transport or requires a specific key will still block you, and **cross-account** access needs an explicit `Allow` there too.

**4. ❗ The network path — the one nobody mentions.** A Fargate task in a **private subnet** must reach *both* services. S3 has a **free gateway endpoint**; **KMS has no gateway endpoint — it needs an interface endpoint** (`com.amazonaws.<region>.kms`) or a NAT gateway. So a task with perfect IAM will simply **hang and time out** if you added the S3 endpoint and forgot the KMS one.

**Read the error to find which of the four is wrong:**
| Error | Cause |
|---|---|
| `AccessDenied` on `GetObject` | The S3 permission in the task role |
| `KMS.AccessDeniedException`, or *"The ciphertext refers to a customer master key that does not exist… or you are not allowed to access"* | **`kms:Decrypt` missing, or the key policy doesn't name the role** — despite the misleading "does not exist" wording |
| Request hangs, then times out | **No network path** — missing VPC endpoint or NAT |
| Works in dev, fails in prod | A per-environment CMK whose key policy was never updated |

**The nuance worth volunteering:** if the *task definition* pulls a secret from Secrets Manager or Parameter Store that's encrypted with a **customer-managed** key, then the **task execution role** *also* needs `kms:Decrypt` — because that decryption happens before your code starts. So a single task can legitimately need `kms:Decrypt` on **two different roles for two different keys**.

**Cross-account variant:** only a **customer-managed** key works (AWS-managed keys cannot be shared). You then need the key policy to name the external principal, the external principal's IAM policy to allow `kms:Decrypt`, and the bucket policy to allow the read — or you use a **KMS grant** for programmatic, temporary delegation.

#### Encryption in Transit (TLS) — End to End

Encryption at rest is a checkbox; **in transit is an architecture decision, because TLS terminates at every hop and each one is a separate choice**:
```
Client --TLS(ACM)--> CloudFront --TLS--> ALB --TLS or plain HTTP?--> ECS task --TLS?--> RDS
                                  ^                    ^                        ^
                          viewer protocol       target group protocol    force_ssl / sslmode
```
- **CloudFront → origin:** the *Origin Protocol Policy* (`https-only` or `match-viewer`). `http-only` to an ALB means the internet-facing leg is encrypted and the AWS-internal leg is not.
- **ALB → target:** set by the **target group protocol**. HTTP inside the VPC is extremely common and perfectly defensible — but it is **not** "encrypted end to end", and you should say which you've implemented rather than claiming the stronger one.
- **True end-to-end** means HTTPS on the ALB→target hop too, which needs a certificate on the task. Useful detail: **the ALB does not validate the target's certificate**, so a self-signed cert is acceptable there — you're encrypting the hop, not authenticating the backend.
- **mTLS**: an ALB can require and verify **client** certificates (`mutual authentication` on the listener), for partner/B2B or IoT callers. **App Mesh / ECS Service Connect** provide mTLS *between services*.
- **TLS version** — set the listener's **security policy** to a TLS 1.2 (or 1.3) minimum. "We use TLS" without naming the minimum version is what auditors actually query.

**How you enforce it rather than hope for it:**
| Layer | Enforcement |
|---|---|
| S3 | `Deny` with `aws:SecureTransport: false` (see [S3 Bucket Policies](#s3-bucket-policies--access-control)) |
| ALB | An HTTP:80 listener whose only action is **redirect to HTTPS** |
| RDS | PostgreSQL `rds.force_ssl=1` / MySQL `require_secure_transport`; client `sslmode=Require` |
| ElastiCache | Enable **in-transit encryption** at cluster creation (it can't be turned on later) |
| EFS | Mount with `-o tls` |
| AWS APIs | Already HTTPS-only (DynamoDB, SQS, KMS…), which is why "is DynamoDB encrypted in transit?" is a yes-by-default |

**Certificates** come from **ACM** for public endpoints and **ACM Private CA** for internal ones — remembering that an ACM *public* certificate's private key **cannot be exported**, so it can't be installed on a task or EC2 instance directly (see [ACM](#acm-aws-certificate-manager) below).

### ACM (AWS Certificate Manager)

**Free public TLS certificates with automatic renewal** — the renewal is the real value, since expired certificates are one of the most common self-inflicted outages.

- **Validation:** **DNS validation** (add a CNAME record; **auto-renews forever** once the record stays in place — always choose this) or **email validation** (manual, breaks renewal if nobody clicks the link).
- **Integrations:** ALB/NLB, **CloudFront**, API Gateway, AppSync, Elastic Beanstalk.
- **❗ You cannot export the private key of an ACM *public* certificate.** So you cannot install one directly on an EC2 instance or an on-prem server — terminate TLS at an ALB/CloudFront instead, or use **ACM Private CA** (which does allow export, for internal certs, and is paid). This limitation is a very common question.
- **❗ Certificates for CloudFront must live in `us-east-1`**; certificates for an ALB must be in the ALB's region. See [CloudFront](#cloudfront-cdn).
- **Imported** certificates (from an external CA) get **no automatic renewal** — you must rotate them yourself. Monitor expiry with the ACM `DaysToExpiry` metric, an AWS Config rule, or an EventBridge rule on ACM expiry events.
- **SNI** lets one ALB listener serve many certificates/domains.

### AWS Systems Manager (SSM)

An operations-and-management suite that shows up in interviews mainly through **one killer feature**, but the rest is worth knowing.

**❗ Session Manager — the answer to "how do you get a shell on an EC2 instance?"**

The reflex answer is "SSH via a bastion host in a public subnet." The modern answer is **Session Manager**, and the difference is substantial:
- **No SSH keys** to distribute, rotate, or leak.
- **No open inbound ports** — not even 22. The SSM Agent makes an **outbound** connection to the SSM endpoints, so the instance can sit in a **fully private subnet with no inbound rules at all**.
- **No bastion host** to run, patch, and pay for.
- **Access is controlled by IAM**, so you grant and revoke shell access with a policy, and **every session is logged to CloudTrail** and can be recorded keystroke-by-keystroke to S3/CloudWatch Logs — which is the audit story a bastion can't match.
- Works for Windows (PowerShell/RDP via port forwarding) as well as Linux.

**Requirements** (and therefore the usual failure causes): the **SSM Agent** installed and running (pre-installed on Amazon Linux 2/2023 and recent Windows AMIs), an **instance profile** with `AmazonSSMManagedInstanceCore`, and network reachability to the SSM endpoints — either via a NAT gateway or, for a genuinely isolated subnet, **interface VPC endpoints for `ssm`, `ssmmessages`, and `ec2messages`** (see [VPC Endpoints](#vpc-endpoints--privatelink)).

```bash
aws ssm start-session --target i-0abc123
# Port-forward a private RDS/RDP endpoint to localhost — replaces an SSH tunnel through a bastion
aws ssm start-session --target i-0abc123 \
  --document-name AWS-StartPortForwardingSessionToRemoteHost \
  --parameters '{"host":["mydb.abc.us-east-1.rds.amazonaws.com"],"portNumber":["5432"],"localPortNumber":["5432"]}'
```

**The rest of Systems Manager:**
| Capability | What it does |
|---|---|
| **Parameter Store** | Configuration and secrets storage — it's *part of* SSM (see [Secrets Manager vs Parameter Store](#new-content-secrets-manager-vs-parameter-store)) |
| **Patch Manager** | Scans and applies OS patches on a schedule via **patch baselines** and **maintenance windows**, across EC2 *and* on-prem servers. This is the concrete answer to the "guest OS patching is your responsibility" half of the [EC2 shared responsibility model](#ec2-shared-responsibility-model) |
| **Run Command** | Execute a command or script across a fleet by tag, with no SSH and full audit logging — "restart the service on all instances tagged `role=web`" |
| **State Manager** | Enforces desired configuration continuously (agent installed, service running) and corrects drift |
| **Automation (runbooks)** | Multi-step operational workflows — patch-and-reboot, AMI creation, or the **auto-remediation actions** invoked by [AWS Config](#aws-config) |
| **Inventory / Fleet Manager** | Collects installed software, patch level, and configuration across the fleet; browse and manage instances without connecting |
| **Compliance** | Reports patch and configuration compliance per instance |

**Where to volunteer it:** whenever a question involves accessing, patching, or configuring EC2 at scale. "How do you patch 200 instances?" → Patch Manager with maintenance windows, or replace instances from a freshly baked golden AMI via [ASG instance refresh](#auto-scaling-groups-asg) — immutable infrastructure being the stronger answer where the workload allows it.

### AWS Artifact

A **self-service portal for compliance documents** — AWS's audit reports (**SOC 1/2/3, PCI DSS AOC, ISO 27001/27017/27018, FedRAMP**, and country-specific attestations) plus **agreements** you accept online (the **HIPAA BAA**, the GDPR data-processing addendum).

**The trick to avoid:** Artifact does **not** scan, monitor, or secure anything. It's a document repository. Its role is the **shared responsibility model** in practice — when your auditor asks for evidence that the *underlying infrastructure* is compliant, you download it from Artifact; everything above the line (your configurations, your access controls) you have to evidence yourself with Config, CloudTrail, and Security Hub.

### GuardDuty

**Intelligent threat detection** — continuously analyses **CloudTrail management events, VPC Flow Logs, and DNS logs** (plus optional S3 data events, EKS audit logs, RDS login activity, Lambda network activity, and **EBS malware scanning**) using machine learning and threat intelligence feeds.

- **Agentless and log-free to set up** — it reads those sources directly, so there is nothing to install and **enabling it does not require you to turn on (or pay for) the logs themselves**. One click, or organisation-wide from a delegated administrator account.
- **Typical findings:** crypto-mining, communication with known-malicious IPs or domains, **EC2 instance credentials being used from outside AWS** (i.e. stolen role credentials — see IMDSv2 in [IAM Roles](#iam-roles-policies-assumerole)), unusual API calls or console logins from anomalous locations, port scanning, and reconnaissance against your account.
- **Automate the response:** findings go to **EventBridge**, so a high-severity finding can trigger a Lambda that isolates an instance's security group, revokes a role's sessions, or opens a ticket — rather than sitting in a console nobody reads.

### Inspector

**Automated vulnerability management** for **EC2 instances, ECR container images, and Lambda functions**.

- **Continuous and event-driven**, not scheduled: it rescans automatically when you deploy a new image, launch an instance, or when a **new CVE is published** — so a package that was clean yesterday gets flagged today.
- Uses the **SSM agent** for EC2, correlates CVEs with **network reachability** (an unreachable vulnerability is genuinely lower risk), and produces a prioritised risk score.
- It's the engine behind **ECR enhanced scanning** (see [ECR](#ecr-elastic-container-registry)).

**❗ Inspector vs GuardDuty is the pairing that gets asked:** **Inspector finds weaknesses** — unpatched CVEs, vulnerable dependencies ("the door has a weak lock"). **GuardDuty finds active threats** — malicious behaviour happening now ("someone is picking the lock"). They're complementary; naming both with that distinction is the complete answer.

### Macie

**ML-based sensitive-data discovery for S3.** It inventories your buckets (flagging any that are public, unencrypted, or shared externally) and scans object contents to classify **PII, credentials, financial data, and health data**, with custom data identifiers via regex for your own formats (policy numbers, internal IDs).

Findings flow to **Security Hub** and **EventBridge**. Where it earns its cost: GDPR/HIPAA/PCI data-classification requirements, and answering "do we have customer PII sitting in a data-lake bucket nobody remembered?" — a question that's otherwise unanswerable at scale.

### AWS Config

**Records the configuration of every resource over time, and evaluates it against rules.**

- Produces **configuration items** and a full **change history** — so you can view exactly what a security group looked like last Tuesday, and what changed it (linking to the **CloudTrail** event and therefore the identity).
- **Rules**: AWS **managed rules** cover most of the common bar — `s3-bucket-public-read-prohibited`, `encrypted-volumes`, `iam-user-mfa-enabled`, `rds-instance-public-access-check`, `required-tags` — plus **custom rules** in Lambda or CloudFormation Guard.
- **Remediation actions** (via SSM Automation documents) can **auto-fix** a violation — e.g. re-enable Block Public Access the moment someone disables it. Auto-remediation is the answer that separates "we detect drift" from "we prevent drift".
- **Conformance packs** bundle rules into a compliance framework; **aggregators** roll findings up across all accounts and regions in the Organization.
- It's **regional** and you pay per configuration item recorded plus per rule evaluation — worth scoping which resource types you record.

### Security Hub & Detective

**Security Hub** is the **single pane of glass**: it normalises and aggregates findings from **GuardDuty, Inspector, Macie, IAM Access Analyzer, Config, Firewall Manager**, and partner tools into one format, then scores you against **security standards** (AWS Foundational Security Best Practices, **CIS Benchmark**, PCI DSS). Cross-account and cross-region aggregation, with automated actions via EventBridge.

**Detective** takes a finding and builds an **interactive behaviour graph** from CloudTrail, VPC Flow Logs, and GuardDuty data so you can investigate root cause — what else that role did, what else that IP touched, when the behaviour started. Security Hub tells you *what* is wrong; Detective helps you work out *how it happened and how far it spread*.

### Defence in Depth — The Summary Answer

The layered story to give when asked "how do you secure a workload on AWS?":
```
Edge          Route 53 (DNSSEC) → CloudFront + Shield + WAF
Network       VPC, private subnets, Security Groups, NACLs, Network Firewall, VPC endpoints
Identity      IAM least privilege, roles not keys, MFA, SCPs, permissions boundaries
Data          KMS/CloudHSM encryption at rest, TLS in transit (ACM), S3 Object Lock
Secrets       Secrets Manager / Parameter Store, IAM DB auth — never in code
Detect        GuardDuty (threats) · Inspector (vulnerabilities) · Macie (sensitive data) · Config (drift)
Audit         CloudTrail (org trail → locked log-archive account), VPC Flow Logs, Access Analyzer
Aggregate     Security Hub (one pane) → EventBridge → automated remediation
Respond       Detective for investigation, documented runbooks, tested restores
```
**One-liner:** "No single control is the answer — the point is that a failure at any one layer isn't sufficient to cause a breach. And the layer I'd check first in any real incident is IAM, because on AWS most breaches are permission or configuration failures, not infrastructure failures."

---

# PART III — Tier 3: Breadth — Recognise and Place

> **One clean sentence each.** Nobody expects a .NET full-stack engineer to have operated these. But not *recognising* a service name reads as a gap, whereas knowing what problem it solves reads as breadth. Learn the decision boundaries, not the configuration details.

## Management, Organizations & Billing

### AWS Organizations

**What it is:** central management of many AWS accounts as one hierarchy — a **management (payer) account**, **Organizational Units (OUs)**, and **member accounts**.

**Why multi-account at all** (the question behind the question):
- **An account is the strongest isolation boundary AWS offers.** A compromised or misconfigured dev account cannot touch production resources, because they aren't in the same account at all — far stronger than IAM separation within one account.
- **Blast-radius containment** for both security incidents and mistakes.
- **Service quotas are per account**, so a runaway workload can't consume production's Lambda concurrency or EIP limits.
- Clean **cost attribution** per team/environment, and clean environment separation.

**What Organizations gives you:** [consolidated billing](#consolidated-billing), **[SCPs](#service-control-policies-scps)**, organization-wide **CloudTrail** trails, **[RAM](#aws-resource-access-manager-ram)** resource sharing, delegated administration for GuardDuty/Config/Security Hub/Access Analyzer, plus **tag policies**, **backup policies**, and **AI services opt-out policies**.

Practical notes: accounts are either **created** in the org or **invited** into it; closing an account is a slow, deliberate process (a suspension period before deletion), so treat account creation as semi-permanent. **Best practice: the management account should hold no workloads** — only billing and org administration — because SCPs cannot restrict it (below) and it's therefore the most privileged place in your estate.

### Service Control Policies (SCPs)

Covered in policy-evaluation terms under [Policy Types, Structure & Password Policy](#policy-types-structure--password-policy) and [Least Privilege & Permission Boundaries](#new-content-least-privilege--permission-boundaries-in-practice). The organisation-level specifics:

- **SCPs are filters, never grants.** They define the *maximum* available permissions for an account. An action still needs an IAM policy to allow it; the SCP only decides whether it's permitted to be allowed at all.
- They apply to an **OU or account** and are inherited down the tree.
- **They restrict even the root user of a member account** — which is what makes them the real guardrail.
- **❗ The management account is not affected by SCPs at all**, no matter where you attach them. That's the single most-asked SCP gotcha, and the reason you keep workloads out of the management account.
- Service-linked roles are also exempt.
- **`FullAWSAccess`** is attached by default. Strategies: **deny-list** (keep FullAWSAccess and add explicit `Deny` statements — most common) or **allow-list** (remove it and enumerate exactly what's permitted — tighter, much more work).

Typical real SCPs to be able to name: deny all regions except approved ones (`aws:RequestedRegion`), deny leaving the organization, deny disabling **CloudTrail/Config/GuardDuty**, deny deleting log-archive buckets, deny disabling S3 Block Public Access, and deny root-user actions in member accounts.

### Consolidated Billing

One bill across every account, paid by the management account. The two effects that actually save money — and the reason "we're moving accounts into an Organization" is a cost initiative, not just a governance one:

1. **Aggregated volume discounts.** Tiered pricing (S3 storage, data transfer) is calculated on the **combined** usage of all accounts, so everyone reaches the cheaper tiers sooner.
2. **Reserved Instance and Savings Plan sharing.** Unused RI/SP commitment in one account automatically covers matching usage in **any other** account in the org. This is usually the bigger win, and it can be **disabled per account** when a team needs guaranteed capacity or strictly separated billing.

Supporting tooling: **cost allocation tags** and **cost categories** for attribution, and the **Cost and Usage Report (CUR)** delivered to S3 for line-item analysis in Athena/QuickSight.

**❗ The cost-allocation-tag gotcha:** tags must be explicitly **activated** in the Billing console before they appear in cost reports, and activation is **not retroactive** — historic spend never gets tagged. Set up your tagging strategy and activate the keys on day one; the tag policy feature in Organizations is how you enforce consistency.

### AWS Control Tower

**An automated, opinionated landing zone.** One setup wizard gives you: a multi-account structure with dedicated **log-archive** and **audit** accounts, **IAM Identity Center** for human access, org-wide CloudTrail and Config, and a baseline of guardrails.

- **Guardrails (now "controls")** come in three flavours: **preventive** (implemented as SCPs — the action is blocked), **detective** (implemented as Config rules — the violation is reported), and **proactive** (CloudFormation hooks — the resource is blocked before deployment). Each is categorised **mandatory**, **strongly recommended**, or **elective**.
- **Account Factory** vests new accounts to a standard, pre-configured baseline — so a new team's account arrives already logging, already guardrailed, already wired to SSO.
- It's a **layer on top of** Organizations, Config, CloudTrail, IAM Identity Center, and Service Catalog rather than a new service underneath.

**Interview framing:** "How would you set up a secure multi-account AWS environment from scratch?" → **Control Tower**, because hand-building a landing zone (org structure, SCPs, centralised logging, SSO, guardrails, account vending) is weeks of work that's easy to get subtly wrong. Build it by hand only when you need a structure Control Tower can't express.

### AWS Resource Access Manager (RAM)

**Share specific resources across accounts** in your organisation without duplicating them and without cross-account IAM roles.

Commonly shared: **VPC subnets**, **Transit Gateways**, Route 53 Resolver rules, License Manager configurations, Aurora clusters, and EC2 Capacity Reservations/Dedicated Hosts.

**The pattern that matters:** a central **networking account** owns one well-designed VPC and **shares its subnets** to workload accounts. Each team launches resources *into* the shared subnets, so you get one coherent network with no VPC peering, no overlapping-CIDR problems, and no per-team network design — while IAM boundaries between accounts stay intact. This is how most large AWS estates are actually built.

**RAM vs cross-account roles:** RAM shares the **resource itself** (the subnet exists once, used by many accounts); a cross-account role shares **permission to act** in the owning account. Different mechanisms for different problems.

### Cost Explorer

The analysis tool: visualise cost and usage with up to **13 months of history**, plus a **12-month forecast**.

- Group and filter by service, **linked account**, region, instance type, usage type, and **cost allocation tag**. Monthly/daily granularity is included; **hourly and resource-level granularity costs extra**.
- Built-in recommendations: **EC2 rightsizing**, and **Reserved Instance / Savings Plans purchase recommendations** with **utilisation and coverage reports** — the reports that tell you whether commitments you already bought are actually being used.
- For anything deeper, export the **CUR** to S3 and query it with Athena or visualise it in QuickSight.

### AWS Budgets

Set thresholds and get alerted. Four budget types: **cost**, **usage**, **RI/SP utilisation**, and **RI/SP coverage**.

- Alerts on **actual** *or* **forecasted** spend, delivered via SNS, email, or Chatbot (Slack/Teams). The forecast-based alert is the useful one — it warns you mid-month that you're on track to overrun, rather than after the fact.
- **❗ Budget Actions are the strong answer:** a budget can automatically **apply an IAM or SCP deny policy**, or **stop EC2/RDS instances**, when a threshold is breached — turning a notification into an actual control. Ideal for capping a sandbox or a training account.
- First two budgets are free.

### Cost Anomaly Detection

**Machine learning on your own spend patterns**, alerting on **unusual** cost — with **no threshold for you to set**.

- Monitors by AWS service, linked account, cost category, or cost allocation tag.
- Alerts include **root-cause analysis** (which service, which account, which usage type drove the spike).

**❗ Budgets vs Anomaly Detection is the distinction to state:** Budgets answer *"tell me when I cross a limit I defined"* — they need you to know the right number, and they miss a 300% spike in a small service that stays under the overall budget. Anomaly Detection answers *"tell me when something is weird"* — it catches the runaway Lambda recursion, the forgotten GPU instance, or the misconfigured NAT-gateway data transfer on day one. **Run both**: budgets for governance and forecasting, anomaly detection for catching surprises.

### Trusted Advisor

An automated account-level review across five/six pillars: **cost optimisation, performance, security, fault tolerance, service limits (quotas)**, and operational excellence.

- **❗ Support-tier gated:** Basic and Developer plans get only the **core security checks and service-quota checks**. The **full check set, programmatic API access, and weekly email reports require Business or Enterprise Support** — a commonly tested detail.
- Representative findings: idle/underutilised EC2 instances, **unassociated Elastic IPs**, idle load balancers, unattached EBS volumes, low-utilisation RDS, **security groups open to `0.0.0.0/0` on port 22/3389**, **no MFA on the root account**, exposed access keys found in public repositories, RDS without Multi-AZ, S3 buckets with open permissions, and approaching service quotas.

**How it relates to the neighbours:** **Trusted Advisor** is the broad, shallow, best-practice sweep across all five pillars; **Compute Optimizer** does deep ML-based **rightsizing** for EC2/ASG/Lambda/EBS from real utilisation history; **Cost Explorer** does cost *analysis and commitment* recommendations; **Config** does continuous, customisable **compliance** rules; **Security Hub** aggregates security findings against formal standards. Trusted Advisor is where you start on an unfamiliar account; the others are where you go for depth.

### AWS Support Plans

Asked because it gates real features (Trusted Advisor above being the obvious one) and because it's part of any cost conversation.

| Plan | Cost | Technical support | Notable inclusions |
|---|---|---|---|
| **Basic** | **Free**, every account | ❌ None (only billing/account support) | Documentation, forums, **Personal Health Dashboard**, core Trusted Advisor security/quota checks |
| **Developer** | From ~$29/mo | **Business-hours email**, 1 primary contact | General guidance <24 h; system-impaired <12 h. **Non-production only** in practice |
| **Business** | From ~$100/mo (or 3–10% of spend) | **24/7 phone, chat, email**, unlimited contacts | ✅ **Full Trusted Advisor** + API, production-down <1 h, **third-party software support**, Infrastructure Event Management (extra), AWS Health API |
| **Enterprise On-Ramp** | From ~$5,500/mo | 24/7 + **pool of Technical Account Managers** | Business-critical-down **<30 min**, Cost Optimization/Well-Architected reviews |
| **Enterprise** | From ~$15,000/mo | 24/7 + a **designated TAM** | Business-critical-down **<15 min**, concierge billing, Incident Detection & Response, training credits |

**The two facts that actually get tested:** **Business is the minimum tier for production**, because it's the first with 24/7 technical support, a 1-hour production-down response, and the full Trusted Advisor check set + API. And a **TAM** (designated technical advisor) only arrives at **Enterprise On-Ramp/Enterprise**.

### Free Tier, Pricing & Estimating Cost

**Three kinds of Free Tier** — the distinction is the question:
- **Always free** — permanently, within limits: **1 M Lambda requests + 400,000 GB-seconds/month**, **25 GB DynamoDB** storage, 10 CloudWatch custom metrics, 25 GB SNS.
- **12 months free** for new accounts — 750 h/month of `t2/t3.micro` EC2, 5 GB S3 Standard, 750 h RDS.
- **Trials** — short-term, service-specific (e.g. GuardDuty's 30 days, Inspector, Macie).

**❗ The Free Tier trap worth naming:** exceeding it doesn't stop anything, it just bills you — and the classic surprise charges are a **NAT Gateway** (~$32/month before a byte of traffic, never free-tier eligible), an **unattached Elastic IP** or any public IPv4 address, **CloudWatch Logs with `Never Expire` retention**, orphaned **EBS snapshots**, and **cross-AZ/egress data transfer**. Set a **$1 Budget alert on day one** ([Budgets](#aws-budgets)) and turn on [Cost Anomaly Detection](#cost-anomaly-detection).

**The AWS pricing fundamentals to state:** pay-as-you-go, **pay less by reserving** (RI/Savings Plans), **pay less per unit as you use more** (tiered volume pricing — the mechanism behind [Consolidated Billing](#consolidated-billing) savings), and pay less as AWS grows. Almost universally, **data transfer *in* is free, data transfer *out* is charged**, and cross-AZ traffic is charged in both directions — which is why VPC endpoints and same-AZ placement are cost levers, not just latency ones.

**Estimating tools:** the **AWS Pricing Calculator** for forward-looking architecture estimates (shareable, exportable — the right artefact for a design review or a client proposal), and the **Migration Evaluator**/**TCO** analysis for on-prem-versus-AWS business cases. Contrast with [Cost Explorer](#cost-explorer), which is **retrospective**: Pricing Calculator estimates what a design *will* cost, Cost Explorer analyses what you *did* spend.

---

## Cost & Performance

### [new content] Cost Optimization: Savings Plans, Reserved, Spot

The original notes cover EC2 pricing models in a list but never contrast them for a purchasing-decision interview question ("How would you reduce our AWS compute bill by 30%?") — this section fills that gap directly.

| Option | Commitment | Discount vs On-Demand | Flexibility | Best for |
|---|---|---|---|---|
| On-Demand | None | 0% (baseline) | Full | Unpredictable/dev/test workloads |
| Compute Savings Plans | 1 or 3 yr, $/hr commitment | Up to ~66% | Applies across EC2/Fargate/Lambda, any instance family/region | Steady baseline usage, flexible architecture |
| EC2 Instance Savings Plans | 1 or 3 yr | Higher discount than Compute SP | Locked to instance family in a region | Very stable, known instance family needs |
| Reserved Instances (RI) | 1 or 3 yr | Similar to Instance SP | Least flexible (specific instance attributes) | Legacy — mostly superseded by Savings Plans for new commitments |
| Spot Instances | None | Up to ~90% | Can be reclaimed with ~2-min warning | Fault-tolerant, stateless, batch/CI workloads |

**Senior-level cost strategy talking points:**
- Layer commitments: Savings Plan for your **predictable baseline**, On-Demand for the **variable middle**, Spot for **fault-tolerant burst/batch** capacity — a common "three-tier" cost architecture.
- For Fargate/Lambda-heavy .NET shops, Compute Savings Plans apply even to serverless compute — a frequently-missed lever teams assume is EC2-only.
- Spot is well-suited to CI/CD build agents (CodeBuild self-hosted runners on EC2 Spot), batch ETL, and stateless worker fleets behind SQS — the queue absorbs the reclaim disruption.
- Right-sizing (Compute Optimizer recommendations) and eliminating idle resources (unattached EBS volumes, idle NAT Gateways, over-provisioned RDS instances) is usually higher-ROI than switching pricing models, and is the correct *first* answer before jumping to "buy Savings Plans."
- Cost Explorer + Budgets + anomaly detection should be treated as a required part of any production AWS account, not an afterthought — tie this back to the "cost monitoring & budget alerts" item already flagged in the production-readiness checklist earlier in this guide.

---

## Migration & Data Transfer

### The 7 Rs — Migration Strategies

The framework to reach for whenever a question starts "we have an on-prem application and want to move it to AWS." Naming the strategy *and* justifying the choice is the whole answer.

| Strategy | What it means | When it's right |
|---|---|---|
| **Retire** | Switch it off — nobody uses it | Always audit first; a surprising share of an estate is dead weight |
| **Retain** | Leave it where it is, for now | Mainframe dependencies, a pending vendor decision, or something being replaced anyway |
| **Relocate** | Move at the hypervisor level with no change (VMware Cloud on AWS) | Large VMware estate needing to exit a data centre fast |
| **Rehost** ("lift and shift") | Move as-is onto EC2, no code change | Speed and a hard deadline; cheapest to *do*, most expensive to *run*. Use **AWS Application Migration Service (MGN)** |
| **Replatform** ("lift and reshape") | Keep the app, swap components for managed services — self-managed SQL Server → **RDS**, self-hosted queue → **SQS** | ✅ The usual sweet spot: real operational savings without a rewrite |
| **Repurchase** | Drop it and buy SaaS | Commodity functions — email, CRM, ticketing |
| **Refactor / Re-architect** | Rewrite cloud-native (containers, serverless, managed data stores) | Highest cost and risk, highest long-term payoff — justify it with a business driver (scale, release velocity), never with "cloud-native is better" |

**The honest senior answer:** "**Rehost first to get out of the data centre, then replatform and refactor selectively once it's running on AWS with real telemetry.** Trying to refactor everything during a migration is how migrations slip by a year. And rehosting alone rarely saves money — it just moves the bill — so the business case has to include the follow-on replatforming."

Supporting tooling: **Migration Hub** (single dashboard tracking progress across tools), **Application Discovery Service** (inventories the on-prem estate and its dependencies before you plan), and the **Migration Evaluator** for the TCO business case.

### Database Migration Service (DMS)

Migrates databases with **the source staying online** during the migration.

- **Homogeneous** (SQL Server → RDS SQL Server, PostgreSQL → Aurora PostgreSQL) is a straight data move.
- **Heterogeneous** (Oracle/SQL Server → PostgreSQL/Aurora) needs the **Schema Conversion Tool (SCT)** first to convert schema, stored procedures, and views — SCT converts the *schema*, DMS moves the *data*. Keeping those two straight is the question.
- **Change Data Capture (CDC)** is the important part: DMS does a full load, then **continuously replicates ongoing changes**, so you keep both databases in sync and cut over during a short window instead of a long outage. Leaving CDC running in reverse also gives you a rollback path.
- Runs on a replication instance in your VPC; also does **ongoing replication** into S3/Kinesis/OpenSearch for analytics, not just migration.

### Snow Family

**Physical devices AWS ships you** for offline data transfer — the answer when moving data over the network would take too long.

| Device | Capacity | Use for |
|---|---|---|
| **Snowcone** | ~8–14 TB | Small, rugged, edge collection; can even ship via a drone/vehicle |
| **Snowball Edge** | ~80 TB usable (Storage Optimized) / compute-optimised variants | The workhorse: TB-to-PB migrations, plus **local compute** (EC2/Lambda) at a disconnected edge site |
| **Snowmobile** | Up to **100 PB** — a 45-foot shipping container | Exabyte-scale data-centre evacuation |

**The reasoning to show, not just the names:** work out the transfer time. 100 TB over a 1 Gbps link, at realistic utilisation, is **well over a week** of saturated bandwidth you also need for production — so a Snowball that arrives in days wins. Below roughly 10 TB, or with a fat dedicated link, network transfer (DataSync over Direct Connect) is simpler. Data is encrypted with KMS and the devices are tamper-evident.

### Storage Gateway, DataSync & Transfer Family

**Storage Gateway** — a hybrid appliance (usually a VM on-prem) that gives local systems a familiar protocol while the data actually lives in AWS:
| Type | Presents | Backed by | Use for |
|---|---|---|---|
| **S3 File Gateway** | **NFS / SMB** file shares | S3 objects | Letting existing apps and users write "files" that land in S3, with a local cache |
| **FSx File Gateway** | SMB | FSx for Windows | Low-latency on-prem access to Windows file shares in AWS |
| **Volume Gateway** | **iSCSI** block volumes (cached or stored mode) | S3 + EBS snapshots | Backing up on-prem block storage; DR restore into EC2 |
| **Tape Gateway** | A **virtual tape library** (VTL) | S3 Glacier | Retiring physical tape backup while keeping the existing backup software |

**DataSync** — managed, accelerated **online** transfer and ongoing sync between on-prem (NFS/SMB/HDFS/object) and AWS (S3, EFS, FSx), or between AWS storage services. It handles parallelism, integrity validation, incremental sync, and scheduling — the right tool for repeated bulk transfer, where Storage Gateway is for *continuous hybrid access*.

**Transfer Family** — fully managed **SFTP/FTPS/FTP** endpoints in front of S3 or EFS. The answer when a partner or legacy system can only speak SFTP and you don't want to run and patch an SFTP server.

**The distinction to state:** "**Storage Gateway** keeps a permanent hybrid foothold — on-prem systems keep using NFS/SMB/iSCSI/tape while the data lives in AWS. **DataSync** is for moving or syncing data over the network. **Snow Family** is for moving data physically when the network can't. **Transfer Family** is for exposing S3 over legacy file-transfer protocols."

---

## Well-Architected & Resilience

### [new content] AWS Well-Architected Framework — 6 Pillars

The original notes never reference the Well-Architected Framework, despite it being one of the most commonly asked "tell me about AWS best practices generally" senior/architect-level framing questions.

| Pillar | Core question | .NET-relevant example |
|---|---|---|
| Operational Excellence | Can you run and monitor systems to deliver business value, and continually improve? | IaC (CloudFormation/CDK/Terraform), structured logging, runbooks, CI/CD with automated rollback |
| Security | How do you protect data, systems, and assets? | IAM least privilege, Secrets Manager, encryption at rest/in transit, WAF, Security Hub |
| Reliability | Can the workload perform its function correctly and consistently? | Multi-AZ, auto-scaling, retries with backoff/jitter (Polly in .NET), DLQs, chaos/failure testing |
| Performance Efficiency | Are you using resources efficiently as demand changes? | Right-sized compute, caching (ElastiCache/CloudFront), async/event-driven patterns, .NET AOT for Lambda |
| Cost Optimization | Are you avoiding unnecessary costs? | Savings Plans/Spot mix, S3 lifecycle policies, right-sizing, tagging for cost allocation |
| Sustainability | Are you minimizing environmental impact? | Region selection, efficient instance types (Graviton/ARM64), scale-to-zero serverless patterns |

**How to use this in an interview:** when asked an open-ended "how would you evaluate this architecture," structuring your answer explicitly around these 6 pillars (even briefly) signals architect-level thinking rather than a grab-bag of tips. It's also the basis for the **AWS Well-Architected Tool** and formal **Well-Architected Reviews**, which senior/lead engineers are frequently expected to have participated in or led.

### [gaps] Well-Architected 6 Pillars — Rapid Recall Version

A compact, one-line-per-pillar version of the table above, purely for fast memorization/recall under interview pressure — the detailed table is what you study from; this is what you recite from:

- **Operational Excellence:** run and monitor systems, and continually iterate/improve.
- **Security:** protect data, systems, and assets through risk-based controls.
- **Reliability:** recover from failure and scale to meet demand consistently.
- **Performance Efficiency:** use computing resources efficiently, even as demand and technology change.
- **Cost Optimization:** avoid unnecessary spend and eliminate waste.
- **Sustainability:** minimize the environmental impact of running your workloads.

**Memory hook:** "Run it well, keep it safe, keep it up, keep it fast, keep it cheap, keep it green" — six pillars, six verbs, in the same order AWS presents them.

### [new content] Disaster Recovery Strategies

The original notes touch on Multi-AZ, Global Tables, and multi-region Lambda concurrency individually but never assemble them into the standard DR-strategy framework AWS interviews expect (Backup & Restore / Pilot Light / Warm Standby / Multi-Site Active-Active) — a clear, material gap for a senior interview.

```
  cheaper, slower recovery  ------------------->  costlier, faster recovery

+------------------+  +------------------+  +------------------+  +------------------+
| Backup & Restore |->|   Pilot Light    |->|   Warm Standby   |->|    Multi-Site    |
|                  |  |                  |  |                  |  |  Active-Active   |
| RPO: hours       |  | RPO: minutes     |  | RPO: secs-mins   |  | RPO: ~0          |
| RTO: hours       |  | RTO: 10s of mins |  | RTO: minutes     |  | RTO: ~0          |
+------------------+  +------------------+  +------------------+  +------------------+
 restore from        core infra off /     scaled-down but        full capacity live
 snapshots           minimal, data        RUNNING copy          in 2+ regions
                     replicating
```
*(Cost and operational complexity increase left→right; RPO/RTO improve left→right.)*

| Strategy | Description | RTO/RPO | .NET/AWS implementation notes |
|---|---|---|---|
| **Backup & Restore** | Regular backups (RDS snapshots, DynamoDB PITR/backups, S3 cross-region replication) to a DR region; restore on disaster | Hours (RTO/RPO) | Cheapest; automate via AWS Backup; test restores regularly — an untested backup is not a DR plan |
| **Pilot Light** | Core infra (DB replica, minimal config) always running in DR region at minimal scale; rest is provisioned on failover | RTO: tens of minutes; RPO: minutes | RDS cross-region read replica kept warm; app tier (ECS/EC2) defined in IaC but scaled to zero/minimal until needed |
| **Warm Standby** | Scaled-down but fully functional full stack running in DR region continuously | RTO/RPO: minutes or less | DynamoDB Global Tables or Aurora Global Database for data; smaller ECS/Fargate service count in DR region, scaled up on failover |
| **Multi-Site Active-Active** | Full production capacity live in 2+ regions simultaneously, serving real traffic | RTO/RPO: near zero | Route 53 latency/weighted routing across regions; DynamoDB Global Tables or Aurora Global Database; requires conflict-tolerant/idempotent write design |

**Interviewer follow-up to expect:** "How does Lambda concurrency planning change for DR?" — tie back to the concurrency section: a passive DR region still has the default 1,000 concurrency limit unless pre-raised; a warm/active-active strategy requires provisioning that headroom *before* the disaster, not during it.

**Interviewer follow-up to expect:** "Route 53 failover — is that enough for DR by itself?" — no; per the Route 53 section above, DNS failover is TTL-bound and not instant. It's one component of Pilot Light/Warm Standby/Active-Active, not a complete DR strategy on its own.

### Testing Resilience: Fault Injection Simulator & Resilience Hub

The question that follows any DR answer is **"how do you know it works?"** — and "we documented the runbook" is a weak reply. An untested DR plan is an assumption, not a capability.

**AWS Fault Injection Service (FIS)** — managed **chaos engineering**. You define an experiment template that injects a real, controlled fault and observe whether the system behaves as designed:
- Faults available: **stop/terminate EC2 instances**, throttle or fail API calls, inject **CPU/memory/disk/network stress**, add **network latency or packet loss**, **fail over an RDS instance**, kill ECS tasks or EKS pods, and — the big one — simulate an **entire AZ becoming unavailable**.
- **Stop conditions** are the critical safety feature: FIS aborts the experiment automatically if a CloudWatch alarm you nominate breaches, so a test can't become the outage.
- The discipline: form a hypothesis ("if we lose one AZ, the ALB drains those targets and the ASG replaces them within 3 minutes with no 5xx"), run it in staging, then in production during business hours with the team watching, and treat any surprise as a finding.

**AWS Resilience Hub** — assesses an application against your stated **RTO/RPO targets**, scores its resilience, flags gaps (a single-AZ database behind a multi-AZ app tier, missing backups, no cross-region copy), recommends fixes, and generates **FIS experiment templates plus CloudWatch alarms** to validate them. It turns "we think we're resilient" into a measured number against a target.

**The answer that lands:** "I'd define RTO/RPO per workload, pick the DR strategy that meets them, then **prove it** — Resilience Hub to assess against the targets and FIS to inject the actual failure, with CloudWatch stop conditions so the experiment is safe. **GameDays** on a schedule, because a DR plan that hasn't been exercised in a year is a hypothesis. Also worth saying plainly: the most common real finding isn't infrastructure, it's the **ASG health-check type left on `EC2` instead of `ELB`**, so a hung application is never replaced — which is exactly the kind of thing only a fault injection test surfaces." (See [Auto Scaling Groups](#auto-scaling-groups-asg).)

---

# PART IV — Cross-Cutting Reference

> Consolidated best practices, pitfalls, long-form Q&A, and the changelog of how this guide was assembled.

## Best Practices

**Lambda / Serverless**
- One function = one responsibility; avoid "fat Lambda" business logic.
- Use async programming; keep functions and packages lightweight.
- Manage infra via CloudFormation/SAM/CDK/Terraform, not console clicks.
- Provisioned Concurrency for latency-sensitive production APIs; .NET AOT to shrink cold starts further.
- Externalize long-running work to Step Functions.
- Minimize VPC usage for Lambda unless private resource access is required; prefer VPC Endpoints once inside a VPC.
- Create DB connections/clients outside the handler to survive warm reuse.

**Messaging**
- Always design for at-least-once delivery — idempotency is not optional.
- One SQS queue per consumer under an SNS fan-out topic.
- Delete SQS messages only after successful processing.
- Align SQS visibility timeout with (and slightly exceed) consumer max processing time.
- Use FIFO + `MessageGroupId`/`MessageDeduplicationId` when strict ordering/exactly-once matters.

**IAM**
- Roles over users for anything automated; OIDC over static keys for CI/CD.
- Least privilege by default; add permissions incrementally, not "just in case."
- One role per service, clearly named; short session durations; CloudTrail on all AssumeRole activity.

**Data**
- DynamoDB: design access patterns first, then keys/indexes — not the other way around.
- RDS: separate HA (Multi-AZ) concerns from read-scaling (read replica) concerns; don't conflate them.
- S3: pick storage class and lifecycle rules based on actual access pattern, not guesswork — use Intelligent-Tiering when unsure.

**Networking**
- NAT Gateway per AZ for HA; VPC Endpoints over NAT for AWS-service-only traffic.
- Databases always in private (ideally isolated, no-NAT) subnets.
- Security Groups as primary defense; NACLs sparingly for coarse subnet blocking.

**Observability**
- Structured JSON logging everywhere; correlate logs and X-Ray trace IDs.
- Alarms on the metrics that actually predict user pain (queue depth, p99 latency, throttle counts) — not just CPU%.
- Set log retention policies explicitly; don't leave "Never Expire" as a silent cost leak.

---

## Common Pitfalls (Cross-Cutting)

- **Assuming exactly-once anywhere in AWS async messaging** — Lambda, SQS, and SNS are all at-least-once by default; idempotency is the application's responsibility, not the platform's guarantee.
- **Assuming DNS failover (Route 53) is instant** — it's TTL-bound; combine with load-balancer-level health-based routing for fast reaction.
- **Conflating Multi-AZ (HA) with Read Replicas (scale)** in RDS — different mechanisms, different purposes, different failover semantics.
- **Treating Fargate as "always cheaper" than EC2** — it's cheaper only for bursty/low-utilization workloads.
- **Forgetting NAT Gateway must live in a public subnet**, or forgetting it entirely for VPC-bound Lambda/CodeBuild/ECS tasks that need outbound internet.
- **Publishing domain events before the DB transaction commits** — breaks the "read model eventually reflects a committed write" invariant that CQRS depends on.
- **Hardcoding credentials instead of using roles/OIDC.**
- **Ignoring the separation between trust policy and permission policy** in IAM — both are needed, evaluated independently.
- **Using DynamoDB Scan in a hot path**, or choosing a low-cardinality/time-based partition key that creates a hot partition.
- **Believing CloudWatch alarms alone constitute "observability"** without traces (X-Ray) to explain *why* a metric moved.

---

## Sample Interview Q&A

**Q: Walk me through how you'd design a resilient order-processing pipeline on AWS for a .NET system.**
A: API Gateway/ALB → Lambda or ECS ingest service writes to DynamoDB with status `PENDING` (idempotency key checked via conditional write) → publishes `order_created` to an SNS topic → SNS fans out to per-consumer SQS queues (billing, notification, inventory) → each Lambda/ECS worker processes its queue, using DynamoDB conditional updates for state transitions, with a DLQ configured (`maxReceiveCount`) and CloudWatch alarms on DLQ depth and queue age. Everything downstream is idempotent because SQS is at-least-once. I'd instrument with structured logs + X-Ray tracing tied by a correlation ID, and load-test to validate DynamoDB capacity mode and Lambda concurrency sizing before go-live.

**Q: Your Lambda-backed API has unacceptable p99 latency due to cold starts. What do you do, in order?**
A: First confirm it's actually cold starts (CloudWatch Logs `Init Duration` in the `REPORT` line, not just slow code) → migrate to .NET 8 Native AOT if not already → trim package size and remove VPC attachment if not strictly needed (or add VPC Endpoints if it is needed) → add Provisioned Concurrency sized to p95 traffic → re-measure. I would not jump straight to Provisioned Concurrency before ruling out cheaper architectural fixes (AOT, VPC removal), since Provisioned Concurrency has an ongoing hourly cost.

**Q: When would you choose DynamoDB over RDS for a new .NET service, and when would you not?**
A: DynamoDB when access patterns are known upfront, need is single-digit-ms latency at high/spiky scale, and the data model tolerates denormalization (no complex ad hoc joins/reporting). RDS (or Aurora) when the domain genuinely needs relational integrity, ad hoc queries, complex joins/reporting, or the team's existing tooling/ORM (EF Core) and skill set make relational the faster, lower-risk path. I wouldn't force DynamoDB onto a reporting-heavy back office system just because it's "cloud-native" — that's cargo-culting, not architecture.

**Q: Explain the difference between a Trust Policy and a Permission Policy, and why AWS keeps them separate.**
A: Trust policy defines *who* can assume a role (the principal); permission policy defines *what* that role can do once assumed. They're kept separate because the two questions have different threat models and different owners in practice (a security team might own trust boundaries/cross-account access, while a service team owns what their own service's role can touch) — merging them into one document would conflate "can enter" with "can do," which AWS explicitly disallows at the API level.

**Q: Your DynamoDB table is throttling even though total consumed capacity looks well under the provisioned limit. Why, and what do you do?**
A: Classic hot partition — traffic is concentrated on one partition key value even though aggregate table-level capacity looks fine, because DynamoDB enforces limits per-partition, not just per-table. Adaptive Capacity helps smooth this automatically but isn't a fix for a bad key design. The real fix is redesigning the partition key for higher cardinality (e.g., adding a random/bucketed suffix) or introducing a GSI with a better-distributed key for that access pattern.

**Q: How would you explain the trade-off between ECS/Fargate and Lambda for a new .NET microservice to a non-technical stakeholder?**
A: Lambda is like renting a car only when you need to drive — you pay per trip, no maintenance, but there's a moment of "starting the engine" each time you haven't driven recently (cold start), and you can't take a trip longer than 15 minutes. ECS/Fargate is like leasing a car that's always running and ready — no start-up delay and no trip-length limit, but you're paying for it even during the minutes you're not driving. For spiky, short-lived work, Lambda is cheaper and simpler; for steady, always-on services, ECS/Fargate is more predictable and cost-effective.

---

## Summary of Additions

The following **[new content]** sections were added to close gaps versus a 2026 senior/lead .NET-on-AWS interview bar. The original notes were strong on Lambda internals, DynamoDB, IAM/AssumeRole, SNS/SQS/CQRS, CloudWatch, EC2/Fargate, CI/CD (CodeBuild/CodePipeline), and VPC/Route53/ALB — but had no coverage at all of several near-universal senior interview topics.

1. **ECS vs EKS vs Fargate vs Lambda for .NET Workloads** — the original notes compare Lambda-vs-ECS and EC2-vs-Fargate separately but never give a direct, decision-table answer to the standard "how do you choose compute for a .NET microservices migration" question, including the .NET Framework/Windows-container angle.
2. **Deploying .NET to AWS: Elastic Beanstalk vs ECS vs Lambda Custom Runtime** — Elastic Beanstalk was entirely absent from the notes despite being a legitimate, commonly-asked-about .NET deployment path.
3. **S3 Storage Classes & Lifecycle Policies** — S3 was not covered at all in the source notes; storage class trade-offs and lifecycle JSON are near-guaranteed interview material.
4. **EBS vs EFS vs S3** — basic storage-type comparison, also absent from the source.
5. **VPC Reference Architecture (diagram)** — the notes explained VPC/subnet/NAT concepts textually but never assembled them into the canonical multi-AZ 3-tier diagram interviewers expect you to be able to draw.
6. **RDS Multi-AZ vs Read Replicas vs Aurora** — RDS was not covered at all, despite being more common than DynamoDB for primary OLTP in most .NET shops; this fills a major gap and the very common Multi-AZ/read-replica confusion.
7. **EventBridge Deep Dive** — EventBridge was mentioned only in passing as a Lambda trigger/CloudWatch Events rename; given its centrality to modern event-driven .NET architectures, it needed its own treatment (event buses, schema registry, archive/replay, EventBridge vs SNS decision table).
8. **Event-Driven Architecture Reference Flow (diagram)** — ties the order-processing (Lambda+SQS+DynamoDB) and SNS fan-out patterns together into one sequence diagram, since the source documented them as separate write-ups despite them typically being combined in practice.
9. **Secrets Manager vs Parameter Store** — referenced repeatedly in the source but never actually compared; added a direct comparison table and .NET code snippet.
10. **Least Privilege & Permission Boundaries in Practice** — "least privilege" was stated as a principle throughout the notes without a concrete before/after example or an explanation of permission boundaries vs SCPs.
11. **CloudWatch vs X-Ray: Complementary, Not Competing** — X-Ray was mentioned only in one line; expanded into a proper comparison and .NET integration notes, since "when do you use CloudWatch vs X-Ray" is a standard observability interview question.
12. **Cost Optimization: Savings Plans, Reserved, Spot** — EC2 pricing models were listed but never contrasted for a purchasing-decision question; added a comparison table and a "three-tier" cost strategy talking point.
13. **AWS Well-Architected Framework — 6 Pillars** — completely absent from the source, despite being the standard framework senior/architect interviews use to structure "how would you evaluate this architecture" questions.
14. **Disaster Recovery Strategies (Backup & Restore / Pilot Light / Warm Standby / Multi-Site Active-Active)** — the source touched on Multi-AZ, Global Tables, and multi-region Lambda concurrency individually but never assembled the standard DR-strategy framework AWS interviews expect.

---

## Summary of [gaps] Additions (This Pass)

This is a second gap-fill pass over the guide, tagged **[gaps]** (distinct from the **[new content]** tag used in the first pass) so both passes remain individually identifiable. All seven additions below are inserted next to their most relevant existing section rather than as standalone material.

1. **EC2 Sizing, Pricing Decisions & CPU Credit Gotchas** — the existing EC2 Fundamentals section named the pricing models but didn't cover how to actually size an instance (vCPU/memory ratio reasoning) or the T-family CPU credit exhaustion gotcha, which is a very commonly asked "diagnose this production slowdown" question; also added a decision framework for On-Demand/Reserved/Spot/Savings Plans and an honest "how I'd justify EC2 vs serverless" angle given my Terraform/CDKTF-provisioned EC2 experience.
2. **S3 Lifecycle Rules in Practice — Real Patterns & Terraform** — the existing S3 storage-class table needed concrete "which rule for which data" real-world patterns (logs, backups, compliance data, scratch data) plus an actual `aws_s3_bucket_lifecycle_configuration` Terraform example, since Terraform is my real provisioning tool.
3. **Fargate/ECS/EKS Trade-offs — Reasoning Without Hands-On Time** — added an expanded comparison table (operational overhead, cost model, cold start, use-case fit) across EC2/ECS-Fargate/EKS/Lambda, explicitly framed as trade-off reasoning rather than hands-on claims, since Fargate/ECS/EKS are not part of my confirmed AWS experience.
4. **Multi-AZ vs Read Replica — The #1 Confused Pair** — turned the most commonly confused RDS concept into its own dedicated drill-style callout with a decision-flow diagram, and introduced Aurora more fully (storage-layer replication, sub-10-second-typical lag, storage auto-scaling) — framed as conceptual/comparative knowledge since RDS isn't part of my hands-on experience.
5. **VPC/Subnet/NAT/SG Rapid-Fire Drill Sheet** — the existing VPC section is thorough prose; this adds a condensed table-format cheat-sheet version of the same fundamentals for fast last-minute recall, distinct from (and pointing back to) the detailed explanation.
6. **Well-Architected 6 Pillars — Rapid Recall Version** — a one-line-per-pillar memorization aid placed right after the existing detailed pillars table, for fast recall under interview pressure without re-deriving the full table each time.
7. **CloudFormation vs Terraform/CDKTF** — this was genuinely new ground; the original notes only mentioned CloudFormation in passing as a CodePipeline deploy target. Added a full comparison (state management, cost, drift detection, rollback, ecosystem) plus a concrete HCL-vs-CDKTF-TypeScript S3 bucket example, written in the first person since Terraform/CDKTF is my actual confirmed tool.

---

## Summary of [iam-core] Additions (This Pass)

Third pass, scoped entirely to **[IAM & Security](#iam--security)**. The section was previously written as senior *nuance* material — excellent on roles, trust-vs-permission policy, AssumeRole, cross-account, External ID, OIDC, and pitfalls — but it assumed the IAM fundamentals rather than covering them. This pass restructured the section into the order a full IAM syllabus is taught, kept all existing content, and filled the gaps. Unlike passes 1 and 2, headings are **not** tagged, because the additions are woven into the section rather than bolted on; the earlier `[new content]` tags are preserved where they were.

**Previously absent entirely, now covered:**
1. **IAM Overview essentials** — global-not-regional, free, eventual consistency (and why that breaks a CI/CD create-role-then-use-it pipeline on first run), authentication vs authorization as two distinct failure modes.
2. **Root account & the root-only actions list** — closing the account, support plan, S3 MFA-delete, RI Marketplace, etc. Directly answers the trick question "can `AdministratorAccess` do everything?"
3. **Shared Responsibility Model for IAM** — was a half-sentence aside; now a proper two-column table with a recitable one-liner.
4. **Users & Groups** — completely missing before. Group rules interviewers actually test: users only, **no nesting**, multiple-group union, a group is not a `Principal`. Plus deny-by-default for a new user.
5. **Policy types** — the AWS-managed vs **customer-managed** (versioned, 5 versions, rollback) vs inline comparison was absent; this is a standard question and the recommendation ("customer-managed") needed stating.
6. **Full policy field table** — the original had a 4-field summary; added `Version` (language version, not policy version), `Id`, `Sid`, `Principal` (resource/trust policies only), `NotAction`, plus the `bucket` vs `bucket/*` ARN gotcha, ARN format, and a condition-key reference table.
7. **Password policy** — zero coverage before, and it's an explicit syllabus topic.
8. **MFA** — **zero hits across the entire 1787-line file** before this pass. Added device types, up-to-8 devices per user, and the point most candidates miss: MFA covers the console natively, but CLI/API enforcement needs `aws:MultiFactorAuthPresent` + `GetSessionToken`/`AssumeRole` token codes.
9. **Access to AWS & access keys** — console/CLI/SDK over one REST API, the 2-key limit and *why* (zero-downtime rotation), CloudShell, and the **credential provider chain order** with the stale-env-var-shadows-the-instance-role debugging trap.
10. **IAM Security Tools** — Access Analyzer was named once in passing; added credential report, Access Advisor last-accessed, Access Analyzer's four capabilities incl. generate-policy-from-CloudTrail, policy simulator, CloudTrail, AWS Config rules, each paired with the question it answers.
11. **Hands-on walkthroughs** (users/groups, MFA + key rotation drill, EC2→S3 role, cross-account switch-role incl. the `~/.aws/config` `role_arn`/`source_profile` pattern) — short click-and-CLI form with verification commands.
12. **IAM Rapid-Fire Q&A** — 20 short-answer drills for the fundamentals, complementing the existing long-form [Sample Interview Q&A](#sample-interview-qa).

**Role sub-topics added to the existing (already strong) roles section:**
13. **EC2 instance profile** — a role can't attach to EC2 directly; the console creates the profile silently but CLI/CloudFormation/Terraform do not, which is a real IaC bug class.
14. **IMDS, and IMDSv1 vs IMDSv2** — the SSRF credential-theft path and `HttpTokens: required` as the fix.
15. **`iam:PassRole`** — distinct from `sts:AssumeRole`, and the privilege-escalation path when left unscoped.
16. **STS API table**, temp creds being **three** parts (the `SessionToken`), session durations, and **role chaining's hard 1-hour cap** (the "job dies at 60 minutes" scenario).
17. **Service-linked roles**, **ECS task role vs task execution role**, **EKS IRSA/Pod Identity**, and **IAM Identity Center** as the current answer to "should we still create IAM users?"
18. **Six new pitfall rows** on the existing table: unscoped `PassRole`, IMDSv1 left on, env-var creds shadowing a role, task-vs-execution-role confusion, root access keys, and the `AdministratorAccess` misconception.

---

## Summary of [services-core] Additions (This Pass)

Fourth pass. Where the `[iam-core]` pass filled the IAM fundamentals, this one does the same across **every remaining service domain** in a full AWS syllabus. The guide was previously deep on Lambda, DynamoDB, SQS/SNS, VPC basics, CloudWatch, and CI/CD but had **no coverage at all** of roughly 60% of the standard service surface. Like the previous pass, headings are **not** tagged — the material is woven into the existing sections in teaching order — and all pre-existing content was preserved and cross-linked rather than replaced.

**Two new top-level sections were created:** [Global Edge Services](#global-edge-services), [Load Balancing, Scalability & Auto Scaling](#load-balancing-scalability--auto-scaling), [Security Services](#security-services), and [Management, Organizations & Billing](#management-organizations--billing). Three existing sections were renamed to match their expanded scope (Databases → **Databases, Analytics & Caching**; Messaging → **Messaging, Streaming & Decoupling**; Observability → **Observability & Monitoring**).

**EC2** — previously only "EC2 Fundamentals" plus a sizing/pricing gaps section. Added: instance-type name decoding incl. the **Graviton/ARM64 cost angle for .NET**; **user data** and the golden-AMI-vs-thin-bootstrap trade-off; **security group properties** (allow-only, stateful, SG-referencing-SG) with the SG-vs-NACL table and the **timeout-vs-connection-refused** debugging rule; **classic ports**; **public vs private vs Elastic IP** and the stop/start IP-change gotcha; **placement groups** (cluster/spread/partition); **ENIs**; **EC2 Hibernate** and its requirement list; the **complete purchasing options set** — the previously missing **Dedicated Instances vs Dedicated Hosts** (BYOL licensing) and **Capacity Reservations vs RIs** (capacity vs billing) pairs; and the **EC2 shared responsibility model**.

**EC2 Instance Storage** — the section had only a three-column EBS/EFS/S3 summary table. Added full treatment of **EBS volumes** (AZ-locked, the `DeleteOnTermination` root-vs-data asymmetry, online resize), **volume types** (gp3's decoupling of IOPS from size, io2 Block Express, why HDD types can't boot), **snapshots** (incremental, cross-region DR copy, FSR, Archive tier, DLM, Recycle Bin), **AMIs** (region-scoped, golden AMI, deregister-doesn't-delete-snapshots), **instance store**, **EBS Multi-Attach** and why it needs a cluster-aware filesystem, **EBS encryption** incl. the snapshot-copy procedure to encrypt an existing volume, **EFS** (Linux-only → **FSx for Windows**, mount targets on port 2049, performance/throughput modes), a three-way **EFS vs EBS vs instance store** table, and the storage shared-responsibility split.

**S3** — the section previously covered only storage classes and lifecycle rules. Added: **buckets & objects** (global name uniqueness, flat namespace/prefixes, 5 TB max, 5 GB single-PUT limit, and the correction that S3 is now **strongly consistent**); **bucket policies** with the two guardrail policies (deny-unencrypted, deny-non-HTTPS) and the four-mechanism access table; **Block Public Access**; **static website hosting** and the HTTP-only endpoint limitation; **versioning & replication** (delete markers, not-retroactive, non-transitive, RTC); **performance** (3,500/5,500 requests per **prefix**, multipart, Transfer Acceleration, byte-range fetch, S3 Select) plus Storage Lens/Inventory/Storage Class Analysis; **Batch Operations**; **Requester Pays**; best practices; and the shared-responsibility model.

**S3 Security** — an entire absent domain. Added the **four encryption types** (SSE-S3/SSE-KMS/DSSE-KMS/SSE-C plus client-side) with the **KMS-throttling → S3 Bucket Keys** operational detail; **CORS**; **MFA Delete**; **access logs** including the **infinite-logging-loop warning** and the access-logs-vs-CloudTrail-data-events distinction; **pre-signed URLs** with the temporary-credentials expiry trap; **Object Lock** (Governance vs Compliance vs Legal Hold) and Glacier Vault Lock; **Access Points and Object Lambda**.

**Scalability & Load Balancing** — added **scalability vs high availability vs elasticity vs agility** with the scalable-but-not-elastic example; load-balancing fundamentals incl. **ALB has no static IP** (→ NLB/Global Accelerator) and `X-Forwarded-For` handling in ASP.NET Core; **sticky sessions** and why externalising session state is the real fix; **connection draining / deregistration delay**; and a full **ASG** treatment — launch templates, the **health-check-type EC2-vs-ELB trap**, all five scaling policies, **scaling on queue backlog rather than CPU**, cooldown/warm-up, lifecycle hooks, termination policy, instance refresh, warm pools.

**Databases, Analytics & Caching** — added the **purpose-built database selection table** and OLTP-vs-OLAP framing; **relational/RDS operational surface** (automated backups vs manual snapshots, PITR, maintenance windows, storage autoscaling, IAM DB auth, no-OS-access); **Athena** (per-TB pricing and the three levers that cut it, Glue Catalog, vs Redshift) plus one-liners for Glue/Redshift/EMR/QuickSight/Lake Formation; **RDS Proxy** in depth (the Lambda connection-exhaustion problem, ~66% faster failover); **Aurora advanced features** (6 copies/3 AZs, 15 replicas, endpoint types, **Serverless v2**, **Global Database**, cloning, backtrack, blue/green); and **ElastiCache** (Redis vs Memcached table, cache-aside/write-through/write-behind, TTL and eviction, MemoryDB).

**Streaming & Decoupling** — added **Kinesis** (Data Streams shard mechanics, partition-key hot-shard risk, retention/replay, Enhanced Fan-Out, on-demand mode; **Firehose vs Data Streams**; Managed Flink) with an **SQS vs SNS vs EventBridge vs Kinesis** decision table, and **Amazon MQ** (protocol compatibility as the only reason to choose it over SQS/SNS).

**Networking** — added **VPC Flow Logs** (the `ACCEPT`/`REJECT` debugging technique, the stateful-vs-stateless reject signature, and what isn't captured); **VPC Peering** (non-overlapping CIDRs, **non-transitive**, no edge-to-edge routing, n(n−1)/2 mesh problem); **Transit Gateway** with the peering-vs-TGW table; **VPC Endpoints** (gateway **free, S3/DynamoDB only** vs interface/PrivateLink, private DNS and SG requirements, PrivateLink for your own service); **hybrid connectivity** (Site-to-Site VPN vs Direct Connect, DX not encrypted by default, the DX+VPN-backup HA answer); and a VPC hands-on with a connectivity debug order.

**Route 53** — the section was already strong; added the **DNS resolution walkthrough**, the full **record-type table** (incl. why CNAME can't sit at the apex), and **TTL** mechanics with the lower-TTL-before-migration practice.

**Global Edge Services** — CloudFront had three passing mentions. Added full **CloudFront** coverage (distributions/origins/behaviours, cache key discipline, invalidation vs versioned filenames, **OAC**, signed URLs vs signed cookies, **CloudFront signed URL vs S3 presigned URL**, geo restriction, price classes, origin groups, **CloudFront Functions vs Lambda@Edge**, and the **certificate-must-be-in-us-east-1** gotcha); **Global Accelerator**; the **CloudFront vs Global Accelerator** table; and **Local Zones / Outposts / Wavelength**.

**Containers & Serverless** — added **Docker fundamentals** (containers vs VMs, layers, the multi-stage .NET Dockerfile); **ECS** (object model, **task role vs task execution role**, launch types, `awsvpc` networking, capacity providers, deployment circuit breaker, service discovery) with an ECS-vs-EKS one-liner; **ECR** (enhanced scanning via Inspector, tag immutability, lifecycle policies, pull-through cache); an ECS hands-on with a task-won't-start debug order; and the **S3 → Lambda trigger** pattern including the **infinite-recursion** hazard and at-least-once idempotency requirement.

**Observability** — added **CloudTrail** in depth (management vs **data** vs Insights events, the "object deletes aren't logged by default" gotcha, organization trails, log-file validation, ~15-minute delay) with the **CloudWatch vs CloudTrail vs Config** three-way table; the **AWS Health Dashboard** (public vs *account* dashboard, Health API + EventBridge automation); and **Container Insights / CloudWatch Agent** including the fact that **memory and disk-space are not default EC2 metrics**, plus Synthetics, RUM, and Application Signals.

**Security Services** — an entirely new section. Added a question-to-service routing table; **Shield Standard vs Advanced** and **WAF** (managed rule groups, rate-based rules, the **deploy-in-Count-mode-first** practice, WAF can't attach to an NLB), **Firewall Manager**; **Network Firewall** with a five-way traffic-filtering comparison; **KMS** (key types, the **mandatory key policy**, **envelope encryption** and the 4 KB limit, multi-region keys) **vs CloudHSM**; **ACM** (DNS validation auto-renewal, **private key cannot be exported** so you can't use it on EC2, us-east-1 for CloudFront); **Artifact** (a document repository, not a scanner); **GuardDuty** (threats) **vs Inspector** (vulnerabilities) — stated as an explicit pair; **Macie**; **AWS Config** (rules, auto-remediation, conformance packs); **Security Hub & Detective**; and a layered defence-in-depth summary.

**Management, Organizations & Billing** — another entirely new section. Added **Organizations** (why an account is the strongest isolation boundary, keep workloads out of the management account); **SCPs** at org level including the **management account is exempt** gotcha and deny-list vs allow-list strategies; **Consolidated Billing** (aggregated volume tiers + **RI/SP sharing** as the real savings, and the non-retroactive cost-allocation-tag trap); **Control Tower** (landing zone, preventive/detective/proactive guardrails, Account Factory); **RAM** (shared-subnet central-networking pattern); **Cost Explorer**; **Budgets** incl. **Budget Actions**; **Cost Anomaly Detection** with the explicit **Budgets vs Anomaly Detection** distinction; and **Trusted Advisor** including its **support-tier gating** and how it relates to Compute Optimizer/Config/Security Hub.

## Summary of [resume-aligned] Restructure & Additions (This Pass)

Fifth and final pass. Two things happened: the document was **restructured around my actual experience level per my resume**, and the last genuine content gaps were closed.

### The restructure

The guide previously ran in conventional AWS-documentation order (Compute → Storage → Networking → …), which weights every service equally. Interviewers don't: they drill hardest on what the resume **claims** and skim the rest. So the document is now organised into four tiered parts, with the [Resume-Aligned Priority Map](#how-to-use-this-guide-resume-aligned-priority-map) at the front as the navigation layer.

- **Part I — Tier 1: Resume-Claimed Core.** The topics my resume names explicitly (**Lambda, DynamoDB, EC2, S3, Terraform/CDKTF, GitHub Actions**) plus the two that are unavoidable in using them (**IAM, CloudWatch**). These now come *first*, in that order.
- **Part II — Tier 2: Design-Level Confidence.** Containers, relational databases/caching/analytics, networking, scalability, messaging, edge, security services — where reasoning matters more than operational history, and where the guide states my hands-on gaps explicitly.
- **Part III — Tier 3: Breadth.** Management/billing, cost, migration, well-architected/DR — recognise, place, one clean sentence.
- **Part IV — Cross-Cutting Reference.** Best practices, pitfalls, long-form Q&A, and these changelogs.

Three previously-mixed sections were **split along tier lines** so a section is never half Tier 1 and half Tier 2:
| Was | Became |
|---|---|
| `## Compute` (Lambda + EC2 + containers together) | **`## Serverless & Lambda`** (Tier 1) · **`## EC2 & Instance Storage`** (Tier 1) · **`## Containers: Docker, ECS, ECR & Fargate`** (Tier 2) |
| `## Storage` (S3 + EBS/EFS together) | **`## S3`** (Tier 1) · EBS/EFS/AMI material folded into **`## EC2 & Instance Storage`**, where it actually belongs operationally |
| `## Databases, Analytics & Caching` (DynamoDB + RDS + analytics together) | **`## DynamoDB`** (Tier 1) · **`## Relational Databases, Caching & Analytics`** (Tier 2/3) |

`## CI/CD` was renamed **`## Infrastructure as Code & CI/CD`** and promoted into Part I, since Terraform/CDKTF and GitHub Actions are headline resume skills rather than an afterthought. Each new section header carries a one-line **tier banner** stating why it's weighted that way. The restructure was done mechanically with verification that all 157 subsections survived — no content was rewritten or dropped, and the **Table of Contents is now generated from the document itself** rather than maintained by hand.

### Resume-alignment content added

1. **[Resume-Aligned Priority Map](#how-to-use-this-guide-resume-aligned-priority-map)** — Tier 1/2/3 tables naming which sections to revise hardest and why, plus the honest-framing script for Tier 2 gaps ("I haven't operated Fargate in production; here's how I'd choose…"), which protects against follow-ups I can't survive.
2. **[Azure → AWS Translation](#azure--aws-translation-i-hold-az-900-and-shipped-on-cosmos-db--azure-blob)** — my resume shows real Azure delivery (**Cosmos DB, Azure Blob Storage** at EY) plus **AZ-900**, so "how does that map?" is a near-certain question. Includes the mappings worth volunteering unprompted: Cosmos RU/s ↔ DynamoDB RCU/WCU, **Blob SAS tokens ↔ S3 pre-signed URLs**, Key Vault splitting into KMS + Secrets Manager, and the structural difference that AWS isolates with **accounts** where Azure uses subscriptions/resource groups.
3. **[Resume Deep-Dives](#resume-deep-dives--the-follow-ups-i-should-expect)** — the specific follow-ups each resume bullet invites, with answers. Most substantively, the *"scheduled Lambda jobs for log maintenance and health checks, 99.9% uptime across 12 services"* bullet is unpacked into what it actually implies: **EventBridge scheduled rules/Scheduler**, **CloudWatch Logs retention** (log groups default to `Never Expire` — a real cost leak), metric/subscription filters, S3 export, the four different layers a "health check" could mean, 99.9% as a **43-minutes-per-month error budget**, and the trap question *"what monitors the monitor?"* Also the GitHub-OIDC-plus-plan-on-PR answer for the deployment-dashboard bullet, and a note to prepare the non-AWS **runtime-compiled-assemblies** bullet, since a weak answer there costs more than a weak answer on Kinesis.
4. **A 60-second "tell me about your AWS experience" answer** built from the resume's own numbers, ending on an unprompted honest boundary — which is what makes the resume's *"growing depth in AWS"* phrasing read as calibration rather than hedging.
5. **[Terraform/CDKTF in Practice](#terraformcdktf-in-practice--depth-questions-to-expect)** — the single largest depth addition, because it's a claimed *primary* tool and therefore the likeliest deep-dive: **`for_each` vs `count`** (index shifting destroys resources), workspaces vs directory-per-environment, module version pinning, **secrets living in plaintext in state** and how to protect it, `import`/state surgery for ClickOps resources, the plan-on-PR/apply-on-merge pipeline with **least-privilege split roles**, and CloudFormation's **StackSets/nested stacks** equivalents.

### Remaining service gaps closed

6. **SQS/SNS operational detail** — 256 KB limit and the **Extended Client / S3 claim-check** pattern, retention, **delay queue vs visibility timeout** (a commonly confused pair), DLQ **redrive**, FIFO deduplication windows, and **SNS filter policies** (filtering at the topic rather than in every consumer).
7. **[Step Functions](#step-functions-orchestration-vs-choreography)** — previously only passing mentions. Added state types, **Standard vs Express**, Distributed Map, direct SDK integrations, callback/task tokens, the **Saga pattern** for cross-service transactions, and an explicit **orchestration vs choreography** table — plus EventBridge **Pipes**/**Scheduler** and AWS Batch.
8. **[AWS Systems Manager](#aws-systems-manager-ssm)** — was **zero mentions**, and **Session Manager** is the modern answer to "how do you get a shell on an instance?" (no keys, no bastion, no inbound ports, IAM-controlled, CloudTrail-audited). Plus Patch Manager as the concrete answer to the guest-OS-patching half of the EC2 shared responsibility model, Run Command, State Manager, and Automation runbooks.
9. **[Migration & Data Transfer](#migration--data-transfer)** — an entirely absent domain: the **7 Rs**, DMS + SCT + **CDC** for near-zero-downtime cutover, **Snow Family** with the transfer-time reasoning that justifies it, and **Storage Gateway vs DataSync vs Transfer Family**.
10. **Lambda versions, aliases, layers & destinations** — needed for the **canary/linear deploy via weighted alias** answer, plus async retry defaults and why **SnapStart is Java-only** (for .NET the levers are Native AOT and provisioned concurrency).
11. **[DAX](#elasticache--caching-patterns)** — the DynamoDB-specific cache, with the DAX-vs-ElastiCache decision.
12. **[Support plans](#aws-support-plans)** — with the two tested facts: **Business is the minimum production tier**, and a designated **TAM** starts at Enterprise On-Ramp.
13. **[Free Tier, pricing & estimating](#free-tier-pricing--estimating-cost)** — always-free vs 12-month vs trial, the surprise-charge list (**NAT Gateway, idle Elastic IPs, `Never Expire` log retention, orphaned snapshots, egress**), and **Pricing Calculator (prospective) vs Cost Explorer (retrospective)**.
14. **[Resilience testing](#testing-resilience-fault-injection-simulator--resilience-hub)** — **Fault Injection Service** (AZ-outage simulation, CloudWatch **stop conditions**) and **Resilience Hub** (scoring against RTO/RPO), so the DR section can answer *"how do you know it works?"*

### Contradictions Flagged During Consolidation

- The source file contained a **large exact duplicate** of the entire AWS Lambda deep-dive section (lines ~1–3247 repeat verbatim, including the Lambda vs ECS Q&A) — this was a copy-paste artifact in the original notes, not a genuine contradiction; content was merged once, no conflicting facts existed between the two copies.
- The **DynamoDB "Trick Interview Questions" block also appears twice verbatim** in the source (immediately back-to-back) — same treatment: merged into a single section, no factual conflict.
- No genuine factual contradictions (i.e., two *different* claims about the same fact) were found between sections — the original notes were internally consistent aside from the copy-paste duplication noted above.
- **Corrected during the [iam-core] pass:** the original "Policy types" bullet claimed a resource-based policy "enables cross-account access without the caller having any identity-based grant on their own side beyond `sts:AssumeRole` permission" — this conflated the two distinct cross-account mechanisms (with a resource-based policy no role is assumed at all, so `sts:AssumeRole` is irrelevant; with AssumeRole the resource policy is usually unnecessary). Rewritten in [Users, Groups & Permissions](#users-groups--permissions) as an explicit "resource policy = you stay yourself / AssumeRole = you become someone else" contrast, with the both-sides-must-allow rule stated per mechanism.
- **Corrected during the [services-core] pass:** any lingering "S3 is eventually consistent for overwrites and deletes" framing is now explicitly out of date — S3 has offered **strong read-after-write consistency** for all operations, including overwrites, deletes, and LIST, since December 2020. Stated directly in [S3 Buckets & Objects](#s3-buckets--objects) because the old behaviour is still widely repeated in interview prep material.
- One figure was flagged as **unverified** rather than guessed: the per-partition DynamoDB throughput figures (~3,000 RCU/1,000 WCU per partition) are directional/historical numbers from AWS documentation at the time of note-taking, not a contractually guaranteed limit — marked as directional in the DynamoDB section rather than stated as a hard fact.
