> **AWS Detailed Guide** · [Index](README.md)

# Is Guide Ko Kaise Use Karein: Resume-Aligned Priority Map

Yeh guide jaan-boojh kar mere hands-on experience se zyada broad hai. Interviewers questions ko evenly distribute **nahi** karte — wo us par sabse zyada drill karte hain jo resume *claim* karta hai, aur baaki ko skim kar jaate hain. Isliye neeche diye gaye sections ko weight ke hisab se rakha gaya hai:

**Mere resume ki AWS surface, verbatim:** `AWS (Lambda, DynamoDB, EC2, S3)` · `Terraform / CDKTF (Infrastructure as Code)` · `GitHub Actions (CI/CD)`, aur summary *"growing depth in AWS"* jaise phrase mein likha gaya hai — jo deliberate aur honest hai, aur ek expectation set karta hai jise mujhe meet karna chahiye, oversell nahi.

### Tier 1 — Bulletproof hona chahiye (explicitly mere resume par claim kiya gaya)

| Topic | Kyun | Kahaan |
|---|---|---|
| **Lambda** | Named skill *aur* ek delivery bullet bhi (scheduled jobs, 12 services, 99.9% uptime) | [Deep Dive](01-serverless-lambda.md#aws-lambda-deep-dive) · [Concurrency](01-serverless-lambda.md#lambda-concurrency-model) · [Scheduled jobs pattern](#resume-deep-dives--woh-follow-ups-jo-mujhe-expect-karne-chahiye) |
| **DynamoDB** | Named skill, Cloud *aur* Databases dono ke under listed, plus "DynamoDB-backed microservices" | [Deep Dive](02-dynamodb.md#dynamodb-deep-dive) · [Trick Questions](02-dynamodb.md#dynamodb-trick-questions) · [DAX](10-databases-caching-analytics.md#elasticache--caching-patterns) |
| **Terraform / CDKTF** | Headline IaC skill aur mere deployment-dashboard bullet ka tool | [In Practice](04-iac-cicd.md#terraformcdktf-in-practice--depth-questions-to-expect) · [vs CloudFormation](04-iac-cicd.md#cloudformation-vs-terraformcdktf) |
| **GitHub Actions → AWS** | Named CI/CD skill; OIDC keyless pattern iska AWS half hai | [IAM Roles / OIDC](03-iam-security.md#iam-roles-policies-assumerole) · [Terraform CI/CD](04-iac-cicd.md#terraformcdktf-in-practice--depth-questions-to-expect) |
| **IAM roles & policies** | Unavoidable — upar diye sabko role chahiye, aur yahin par zyadatar AWS debugging land hoti hai | [IAM & Security](03-iam-security.md#iam--security) · [Rapid-Fire](03-iam-security.md#iam-rapid-fire-qa) |
| **S3** | Named skill; mera Terraform state backend bhi | [Buckets & Objects](05-s3.md#s3-buckets--objects) → [S3 Security](05-s3.md#s3-security-encryption--its-four-types) |
| **EC2** | Named skill, CDKTF ke through provision kiya gaya | [Fundamentals](06-ec2-instance-storage.md#ec2-fundamentals) → [Purchasing Options](06-ec2-instance-storage.md#ec2-purchasing-options--the-complete-set) |
| **CloudWatch** | "log maintenance and health checks, 99.9% uptime" mein implied | [Deep Dive](07-observability-monitoring.md#cloudwatch-deep-dive) · [Container Insights & Synthetics](07-observability-monitoring.md#container-insights-the-cloudwatch-agent--proactive-monitoring) |

### Tier 2 — Confidently reason karna zaroori, saath mein hands-on gaps ke baare mein honest rehna

ECS/Fargate · RDS/Aurora · VPC design · SQS/SNS/EventBridge · ALB/ASG · CloudFront · Step Functions · Secrets Manager. Yeh sab **design** questions mein aate hain ("aap X ko kaise architect karoge?") jahan operational war stories se zyada reasoning matter karti hai. Guide mein already flag kiya gaya hai ki kahan mere paas hands-on time kam hai — dekho [Fargate/ECS/EKS Trade-offs](09-containers-ecs-fargate.md#fargateecseks-trade-offs--hands-on-time-ke-bina-reasoning) aur RDS framing note [Multi-AZ vs Read Replica](10-databases-caching-analytics.md#multi-az-vs-read-replica--1-confused-pair) mein.

**Woh framing jo kaam karti hai** (aur jo mujhe bluff karne se better use karni chahiye): *"Maine Fargate ko production mein operate nahi kiya — mera container work Docker locally aur ECS-adjacent raha hai. Jo main aapko walk through kar sakta hoon woh yeh hai ki main isse aur EC2 ke beech kaise choose karunga is workload ke liye, aur commit karne se pehle main kya validate karna chahunga."* Interviewers isko ek confident wrong answer se zyada reward karte hain, aur yeh mujhe ek aise follow-up se bhi protect karta hai jise main survive nahi kar sakta.

### Tier 3 — Recognise karo, place karo, aur ek clean sentence do

Kinesis · Amazon MQ · Athena/Redshift/Glue · GuardDuty/Inspector/Macie/Config · Organizations/Control Tower/RAM · Direct Connect/VPN · Snow Family/DMS/Storage Gateway · Local Zones/Outposts · CloudHSM. Yeh jaanna zaroori hai ki har ek kaunsa problem solve karta hai aur kab use karna hai. Ek .NET full-stack engineer se yahan depth koi expect nahi karta — lekin **naam recognise na karna** ek gap jaisa lagta hai, jabki ek accurate sentence breadth jaisa lagta hai.

### Azure → AWS Translation (Mere paas AZ-900 hai, aur Cosmos DB + Azure Blob par shipped hai)

Mera resume real Azure delivery dikhata hai (Cosmos DB, Azure Blob Storage EY mein) plus AZ-900. Interviewers jo yeh spot karte hain wo **zaroor** poochenge "aapne Azure use kiya hai — yeh kaise map hota hai?" Translation ready hone se ek perceived gap demonstrated transferable depth ban jaata hai.

| Azure (mere resume / AZ-900 par) | AWS equivalent | Worth noting |
|---|---|---|
| **Cosmos DB** | **DynamoDB** | Dono NoSQL hain partition keys aur provisioned/on-demand throughput ke saath. Cosmos 5 tunable consistency levels aur multi-model APIs deta hai; DynamoDB sirf eventual ya strong deta hai. **RU/s ↔ RCU/WCU** sabse close analogy hai, aur dono ek low-cardinality partition key ko hot partition se punish karte hain |
| **Azure Blob Storage** | **S3** | Containers ↔ buckets; blob tiers (Hot/Cool/Archive) ↔ storage classes; SAS tokens ↔ **pre-signed URLs** — us SAS↔presigned mapping ko volunteer karna ek strong move hai |
| Azure Functions | **Lambda** | Consumption plan ↔ standard Lambda; Premium plan ke pre-warmed instances ↔ **provisioned concurrency** |
| App Service | Elastic Beanstalk / ECS Fargate | |
| Azure SQL Database | **RDS / Aurora** | |
| Azure AD (Entra ID) | **IAM + IAM Identity Center** | Entra ID ek identity *provider* hai; IAM ek account ke *andar* authorisation hai. Entra ID → AWS via **SAML/OIDC federation** hi real-world bridge hai |
| Azure Key Vault | **KMS + Secrets Manager** | AWS ise split karta hai: keys ke liye KMS, secrets ke liye Secrets Manager |
| Azure Monitor / App Insights | **CloudWatch + X-Ray** | Yahan bhi split hai: metrics/logs vs distributed tracing |
| Azure DevOps Pipelines | CodePipeline / **GitHub Actions** | |
| ARM / Bicep | **CloudFormation** (aur Terraform dono clouds par kaam karta hai) | Isi liye Terraform ek portable skill hai — same tool, both clouds |
| Resource Groups | *Koi direct equivalent nahi* — closest hai **tags + CloudFormation stacks**; account/OU boundaries heavy isolation ka kaam karti hain | Ek genuine structural difference jo naam lene layak hai: AWS **accounts** se isolate karta hai, Azure subscriptions/resource groups se |
| Azure Service Bus | **SQS + SNS** (ya **Amazon MQ** AMQP compatibility ke liye) | Service Bus queues+topics SQS+SNS mein map hote hain; agar app natively AMQP bolta hai to Amazon MQ hi lift-and-shift path hai |

### Resume Deep-Dives — Woh Follow-Ups Jo Mujhe Expect Karne Chahiye

Har resume bullet ek specific technical drill-down invite karta hai. Yeh wo hain jahan ek vague answer claim ko undercut kar dega.

Neeche ke har bullet apne topic section ke end par bhi repeat kiya gaya hai, taaki Lambda, DynamoDB, IaC, ya Observability ka revision pass usi resume question par khatam ho jo us section ko answer karna hai: [Lambda](01-serverless-lambda.md#resume-follow-ups--scheduled-lambda-jobs-999-uptime-bullet) · [DynamoDB](02-dynamodb.md#resume-follow-ups--dynamodb-backed-microservices-bullet) · [Deployment Dashboard](04-iac-cicd.md#resume-follow-ups--deployment-dashboard-bullet) · [99.9% Measure Karna](07-observability-monitoring.md#resume-follow-ups--999-claim-ko-measure-karna)

**Bullet: "Build scheduled AWS Lambda jobs for automated log maintenance and health checks, sustaining 99.9% uptime across 12 platform services."**

Expect karo: *kaise schedule hote hain? "log maintenance" ka matlab kya hai? health check actually kya check karta hai? yeh 99.9% kaise produce karta hai?*

- **Scheduling** — **EventBridge scheduled rules** (`cron(0 2 * * ? *)` / `rate(5 minutes)`), ya **EventBridge Scheduler** newer, higher-scale option ke liye one-time schedules, time zones, aur built-in retry/DLQ ke saath. EventBridge kaho, "CloudWatch Events" nahi — same service, current name.
- **Log maintenance** — concrete levers hain **CloudWatch Logs retention policies** (log groups default mein **Never Expire** hote hain, jo ek silent, unbounded cost leak hai — retention set karna kaafi baar sabse badi CloudWatch saving hoti hai), **metric filters** log patterns ko alarmable metrics mein badalne ke liye, **subscription filters** logs ko aage stream karne ke liye, aur **export to S3** lifecycle rules ke saath Glacier tak, kisi bhi cheap long retention ke liye.
- **Health checks** — layer ke baare mein precise raho: ek Lambda jo service endpoints probe karta hai aur ek **custom CloudWatch metric** publish karta hai (`PutMetricData`, ya cheaper via **EMF**) ek design hai; **CloudWatch Synthetics canaries** isi idea ka managed version hai; **Route 53 health checks** aur **ALB target-group health checks** respectively DNS aur load-balancer level par operate karte hain. Kaunsa aur kyun batana distinction ko samajhna dikhata hai.
- **99.9% justify karna** — yeh roughly **43 minutes ka downtime per month** ka error budget hai. Credible answer number ko measurement se tie karta hai: availability/error-rate metrics par alarms (**Metric Math** ke through, raw counts nahi), noise cut karne ke liye composite alarms, on-call ke liye SNS, aur per-service dashboard. Agar poocha jaaye "aapko kaise pata chala ki yeh 99.9% hai?", toh honest answer hai ki kaunsa metric kis window mein measure kiya gaya tha — koi marketing figure nahi.

  **Scripted answer — *"aapko kaise pata chala ki yeh 99.9% hai?"*** Ise measurement ki tarah bolo, feeling ki tarah nahi — denominator, success ki definition, aur actual arithmetic:

  > "Yeh measured number hai, guess nahi. **Denominator:** hamare flow mein monthly ~4.2 lakh deal-export requests aate the. **Success ki definition:** DataPower se downstream DMS ko payload gaya aur 30 seconds ke andar `200 OK` + valid ack mila — baaki sab failure count hota tha, 5xx, timeout, aur malformed-payload rejects bhi. **Actual maths:** us month `total = 421,538`, `failed = 388`, toh `(421538 − 388) / 421538 = 99.908%` — isliye main 99.9% keh raha hoon, yeh 388 failures ka number hai.
  >
  > Aur hum isko **manage** bhi karte the: 99.9% ka matlab mahine mein sirf ~43 minute downtime allowed (30 × 24 × 60 × 0.001 = 43.2 min). Error budget track hota tha — jis mahine 50% se zyada budget khatam ho jaata, us mahine feature release rok kar reliability work pehle karte the.
  >
  > Ek clarification: yeh **observed** availability hai, contractual SLA nahi. Committed SLA 99.5% tha, humne usse better diya. Aur yeh sirf hamari service ka number hai — end-to-end, dealer ke DMS ke saath, ~99.5% rehta tha, kyunki kai dealer systems raat ko maintenance pe down hote the."

  Aakhri do distinctions — *observed vs committed*, aur *our-service vs end-to-end* — hi asli signal hain: yeh dikhate hain ki number owned kiya gaya tha, over-claim nahi.
- Likely trap: *"agar health-check Lambda khud fail ho jaaye toh kya hoga?"* → monitor ko bhi monitor karna padta hai: function ke `Errors`/`Throttles` **aur** **missing data** (`treat-missing-data: breaching`) par alarm, kyunki ek monitor jo silently chalna band kar de, "sab theek hai" jaisa hi dikhta hai.

**Bullet: "Deployment Dashboard UI integrating GitHub Actions with CDKTF/Terraform for AWS infrastructure provisioning… cutting manual deployment intervention by 40% and release cycle from 5 days to 3."**

Expect karo: *pipeline AWS ko kaise authenticate karta hai? bad apply ko kaise prevent karte ho? prod kaun approve karta hai?*
- **Authentication: GitHub OIDC ek IAM role assume karta hai** — GitHub secrets mein koi static access keys nahi, trust policy `sub` ke through ek specific repo **aur branch/environment** ko scope kiya gaya. Yeh sabse high-value cheez hai lead karne ke liye, aur JSON [IAM Roles](03-iam-security.md#iam-roles-policies-assumerole) mein hai.
- **Safety: PR par plan (read-only role) → merge par apply (privileged role) ek saved plan file se**, plus GitHub **environment protection rules** manual prod approval ke liye, aur `tfsec`/`checkov` gates. Dekho [Terraform in Practice](04-iac-cicd.md#terraformcdktf-in-practice--depth-questions-to-expect).
- **State: S3 backend + DynamoDB lock table** — aur "agar do pipelines same time chal jaayein toh?" ke liye ready raho.

**Bullet: "CSRconnect features… DynamoDB-backed microservices."**

Expect karo full DynamoDB drill: **access patterns pehle**, partition-key cardinality aur hot partitions, **GSI vs LSI**, on-demand vs provisioned capacity, `Query` vs `Scan`, idempotency ke liye conditional writes, 400 KB item limit, aur single-table design. Sab [DynamoDB Deep Dive](02-dynamodb.md#dynamodb-deep-dive) aur [Trick Questions](02-dynamodb.md#dynamodb-trick-questions) mein cover kiya gaya hai — yeh section sabse hard revise karna hai, kyunki yeh ek AWS service hai jo mere resume ko shipped product se tie karti hai.

**Bullet: "Runtime dynamic-mapping mechanism that compiles dealer-specific business logic into in-memory assemblies (DLLs)."**

AWS bullet nahi hai, lekin resume ka sabse distinctive engineering claim hai aur questions draw karega — `AssemblyLoadContext` aur unloadability par probes expect karo, accumulating assemblies se memory/leak risk, compiled delegates caching, runtime par input compile karne ki security, aur yeh kaise test hota hai. AWS material ke saath isse bhi prepare karna worth hai, kyunki yahan ek weak answer Kinesis par weak answer se zyada cost karta hai.

**60-second ka "apne AWS experience ke baare mein batao" answer:**
> "Mera hands-on AWS work serverless aur IaC-centred hai. Nagarro mein main .NET 8 microservices banata hoon ek Dealership Management System ke liye **DynamoDB**-backed services ke saath, scheduled **Lambda** jobs chalata hoon log maintenance aur health checks ke liye 12 platform services ke across, aur ek deployment dashboard banaya jo **GitHub Actions** ko **CDKTF/Terraform** se wire karta hai **Lambda, DynamoDB, EC2, aur S3** provision karne ke liye — jisne manual deployment intervention ko roughly 40% cut kiya aur hamara release cycle five days se three din kar diya. Jahan main deliberately abhi bhi grow kar raha hoon woh hai container aur relational side — ECS/Fargate aur RDS ke baare mein main reason aur design kar sakta hoon, lekin maine unhe production mein operate nahi kiya, aur main aapko yeh batana chahunga overstate karne se better."

Woh structure — concrete services, ek measurable outcome, phir ek unprompted honest boundary — yehi cheez mere resume par "growing depth in AWS" phrasing ko ek hedge ke bajaye ek strength banati hai.

---

[Index](README.md) · [Serverless & Lambda](01-serverless-lambda.md) →
