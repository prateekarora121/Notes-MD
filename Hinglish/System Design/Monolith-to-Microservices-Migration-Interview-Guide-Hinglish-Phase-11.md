# Monolith-to-Microservices Migration Interview Guide --- Hinglish

## Phase 11: Resume Deep-Dive --- Parteek Arora (Staff Engineer)

> **Purpose:** Resume ke har major claim ko interview-ready story mein
> convert karna.\
> **Source:** Resume (Nagarro DMS, Common Integration Platform,
> CSRconnect, EY Connected Apps, IGT, Infosys).\
> **Note:** Neeche ke model answers resume ke claims par based hain. Jo
> details resume mein nahi hain (design choices, incidents, exact
> numbers) wahan `[apna actual detail]` placeholder hai --- sirf wahi
> bolein jo aapne sach mein kiya. Interviewer kisi bhi number ka "kaise
> measure kiya?" follow-up poochega.

------------------------------------------------------------------------

# 1. Resume ka narrative (60-second pitch)

> "Main 10 saal se enterprise .NET mein hoon --- C#, ASP.NET Core,
> .NET 8, SQL Server, DynamoDB aur AWS. Pehle EY mein ek monolith se
> microservices migration ka hissa raha. Nagarro mein main Staff Engineer
> hoon, 5 engineers ki team lead karta hoon, aur Dealership Management
> System ke integration platform par kaam karta hoon jo third-party
> DMS aur in-house DMS ke beech deals translate karta hai --- 100K+
> transactions per month, 99.9% availability. Maine Spec-Driven
> Development introduce kiya aur CI/CD automation se release cycle 5
> din se 3 din kiya."

Tip: Har claim ke peeche ek *problem → decision → trade-off → result*
story ready rakhein.

------------------------------------------------------------------------

# 2. DMS integration platform (Nagarro)

**Resume claim:** Middle layer jo third-party DMS platforms aur in-house
DMS ke beech deals import/export translate karta hai; dealer-specific
transformations aur state-specific rules (tax calculation); 3 core
services; AWS Lambda, DynamoDB, SQL Server.

## 2.1 Architecture ko kaise explain karein

``` mermaid
flowchart LR
    T[Third-party DMS] -->|inbound deal| I[Integration service]
    I --> X[Dealer-specific translation]
    X --> R[State rules / tax calculation]
    R --> D[In-house DMS]
    D -->|outbound| X
    I --> L[(Logs / audit)]
    U[Dashboard] --> L
    U -->|script changes| S[(Script store)]
    S --> X
```

Interview mein yeh cheezein ready rakhein:

-   **Idempotency:** same deal dobara aaye (retry/duplicate) to double
    processing na ho (Phase 3: Inbox/idempotency key).
-   **Failure handling:** third-party down ho to retry with backoff, DLQ,
    aur "integration pending" state --- completed business transaction
    ko rollback na karein (Phase 8).
-   **Ordering:** same deal ke multiple updates --- version/sequence se
    stale update ignore.
-   **Observability:** correlation ID per deal across inbound → translate
    → outbound (Phase 5). Aapka dashboard log viewer isi ka part hai.
-   **Multi-tenancy:** dealer-level isolation aur dealer-specific
    configuration (Phase 4/8).

## 2.2 Q&A

**Q. Dealer-specific logic ko code mein hardcode kyun nahi kiya?**
Har dealer ka mapping alag aur frequently badalta hai. Code mein
hardcode karne par har change ke liye full release chahiye. Database-
stored scripts se turnaround kam hota hai. Trade-off: runtime code
execution ka security aur testability risk --- isliye controls chahiye
(Section 3).

**Q. 99.9% availability across 12 platform services kaise measure hui?**
`[apna actual detail]` Structure: SLI define karein (successful
requests / valid requests, ya successful end-to-end transactions),
measurement window (monthly), data source (CloudWatch/New Relic), aur
exclusions (planned maintenance, client-caused errors). Samjhayein ki
99.9% ka matlab ~43 minutes/month downtime budget hai (30 din ke
mahine mein), aur committed SLA 99.5% (~3.6 hours) se zyada tight hai.

**Q. 100K+ transactions/month ka scale kaisa hai?**
Yeh average ~0.04 transactions/second hai (100,000 / 2.59M seconds),
yaani throughput challenge nahi, correctness/reliability challenge hai:
duplicates, partial failures, data translation errors, aur auditability.
Isse honestly bolna maturity dikhata hai --- scale inflate na karein.

**Q. Agar ek third-party DMS slow ho jaye to?**
Timeouts, circuit breaker, bulkhead (us dealer ka traffic dusron ko na
roke), queue buffering, aur alert. Retry bounded aur jittered
(Phase 3).

------------------------------------------------------------------------

# 3. Roslyn runtime scripting (sabse distinctive feature)

**Resume claim:** Dealer-specific mapping logic database scripts ke roop
mein store hota hai aur runtime par Roslyn se compile hota hai; bina
full deployment ke mapping change.

## 3.1 Concept

Roslyn .NET compiler platform hai. Aap C# source ko runtime par parse
aur compile karke executable delegate bana sakte hain (Microsoft.CodeAnalysis
scripting/compilation APIs).

``` csharp
// Illustrative sketch --- package versions aur API details verify karein.
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

var options = ScriptOptions.Default
    .WithReferences(typeof(DealMapper).Assembly)   // sirf allowed assemblies
    .WithImports("System", "System.Linq");

var script = CSharpScript.Create<DealDto>(
    scriptSource, options, globalsType: typeof(MappingGlobals));

var compiled = script.Compile();             // diagnostics check karein
var result = await script.RunAsync(new MappingGlobals { Input = deal });
```

## 3.2 Risks aur controls (interview ka core)

  --------------------------------------------------------------------------
  Risk                       Control
  -------------------------- -----------------------------------------------
  Arbitrary code execution   Allow-listed references/imports; scripts ko
  (file/network/process      sirf pure mapping functions tak limit karein;
  access)                    least-privilege runtime identity

  Infinite loop / heavy      Execution timeout + cancellation token; resource
  CPU / memory               limits; separate worker/process jahan possible

  Bad script production      Validation on save: compile diagnostics, test
  tod de                     cases, dry-run against sample payloads

  Un-audited changes         Version history, who/when/why, approval workflow,
                             audit log

  Rollback                   Previous script version ek click mein restore

  Compile cost / latency     Compiled delegate ko cache karein (key: script
                             id + version/hash); invalidate on update

  Memory leak (dynamic       Roslyn script assemblies unload nahi hoti jab
  assemblies)                tak `AssemblyLoadContext` collectible na ho;
                             versions/recompilation ko bound karein

  Multi-instance             Cache invalidation across instances (version
  inconsistency              check ya event) --- sab instances ek hi
                             version use karein
  --------------------------------------------------------------------------

**Important honest point:** Roslyn scripting *sandbox nahi hai*.
Allow-listing se risk kam hota hai, lekin untrusted code ke liye strong
isolation (alag process/container, restricted permissions) chahiye. Agar
scripts sirf trusted internal users likhte hain (dashboard access
controlled), to threat model alag hai --- yeh clearly bolein.

## 3.3 Q&A

**Q. Is design ka alternative kya hai?**
(1) Configuration-driven mapping (JSON/rules engine) --- safer, kam
flexible. (2) Plugin assemblies --- strongly typed lekin deployment
chahiye. (3) Expression engines (e.g. limited DSL) --- safer. Roslyn
sabse flexible hai par security/ops cost sabse zyada. Choose karne ka
reason: `[apna actual detail: flexibility vs release speed]`.

**Q. Script ko production mein promote kaise karte hain?**
Draft → validate (compile + sample tests) → review/approve → versioned
publish → cache invalidate → monitor error rate → rollback agar spike.
Yeh canary/feature-flag idea ka hi version hai (Phase 7).

**Q. Script runtime error aaye to transaction ka kya?**
Deal ko failed/pending state mein rakhein, error + script version log
karein (traceability), retry/manual-review queue, aur dusre deals ko
affect na hone dein.

**Q. Script testing kaise karein?**
Golden input/output samples per dealer, regression suite, aur "new
script vs current script" comparison (shadow run) --- Phase 10 ke
characterization/shadow ideas ka reuse.

------------------------------------------------------------------------

# 4. Common Integration Platform (CI/CD & deployment dashboard)

**Resume claims:** ~40% Deployment Dashboard UI; Terraform/CDKTF +
GitHub Actions; deployment efficiency 40%; release cycle 5 → 3 din;
JFrog Artifactory, SonarQube, Husky, Lighthouse, New Relic.

**Q. Release cycle 5 se 3 din kaise hua?**
`[apna actual detail]` Typical structure: bottleneck identify kiya
(manual steps, environment promotion, approvals, rollback) → automate
kiya (pipeline, artifact promotion, infra as code) → measure kiya
(lead time before/after). "Kaun sa step hataya" aur "kya manual gate
rakha aur kyun" batayein.

**Q. Artifact promotion ka principle?**
Build once, deploy the same artifact across environments (Artifactory
mein immutable versioned artifact); environment-specific config
alag. Isse "staging mein chala, prod mein alag binary" risk khatam hota
hai.

**Q. Rollback kaise handle hota tha?**
Dashboard se previous artifact version redeploy; lekin DB/schema changes
ho chuke ho to blind rollback unsafe --- expand-and-contract aur
backward compatibility (Phase 6/7).

**Q. SonarQube, Husky, Lighthouse ka role?**
SonarQube: quality gates (code smells, coverage, vulnerabilities).
Husky: Git hooks (lint/test pre-commit). Lighthouse: frontend
performance/accessibility checks. New Relic: runtime APM/observability.
Gates ko enforce karne ka rule batayein (kya build fail karta hai).

**CDKTF flag:** CDKTF Dec 2025 mein deprecated/archived hai. Interviewer
puche to: "Hamare existing stack CDKTF par hai; maine plan banaya hai
`cdktf synth --hcl` se plain Terraform HCL (ya OpenTofu) par migrate
karne ka, taaki unmaintained dependency na rahe." `[apna actual plan
ho to hi bolein]`. Resume mein bhi "Terraform/CDKTF" ko "Terraform
(CDKTF → HCL migration)" ya sirf "Terraform" karne par vichar karein.

------------------------------------------------------------------------

# 5. Spec-Driven Development (SDD) aur AI-assisted delivery

**Resume claims:** SDD introduce kiya; ~25% feature delivery time kam;
manual unit-test authoring 40% kam; 80%+ coverage maintained.

## 5.1 Kaise explain karein

SDD: code se pehle specification (behavior, acceptance criteria,
edge cases) → plan → task breakdown → review gates → implementation
(AI assistant ke saath) → tests. Spec single source of truth hoti hai
jo AI ko structured context deti hai.

## 5.2 Interviewer ke tough follow-ups

**Q. 25% aur 40% kaise measure kiya?**
`[apna actual detail]` Batayein: baseline (pehle ke N features ka
average cycle time), comparison (SDD ke baad ke N features), kya
count hua (ticket start → merge/deploy), aur confounders (feature
size, team change). Agar rough estimate hai to "approximately" aur
sample size honestly bolein. Unmeasurable claim defend na karein.

**Q. AI-generated code ki quality kaise ensure karte ho?**
Spec + tests pehle, human code review mandatory, static analysis
(SonarQube), coverage gates, security review, aur small PRs. "Coverage
80% ka matlab quality nahi" --- mutation testing/meaningful assertions
par bhi baat karein.

**Q. Risks?**
Plausible-but-wrong code, security/licensing issues, over-reliance,
data leakage (sensitive code/data prompts mein na daalein), aur
reviewers ka skill erosion. Mitigation: policy, review standards,
approved tools.

**Q. Team ne adopt kaise kiya?**
Pilot → template (spec/plan/tasks) → review checklist → retro se
iterate. Resistance kaise handle ki: `[apna actual detail]`.

------------------------------------------------------------------------

# 6. Data stores: SQL Server, DynamoDB, Cosmos DB

## 6.1 DynamoDB (resume mein 3 projects)

Key concepts jo poochhe jaate hain:

-   **Access-pattern-first modelling:** pehle queries decide karein,
    phir partition key (PK) + sort key (SK) design.
-   **Hot partition:** skewed key (e.g. ek dealer ka bahut traffic) →
    key sharding/high-cardinality keys.
-   **GSI/LSI:** alternate access patterns; GSI eventually consistent.
-   **Consistency:** reads default eventually consistent; strongly
    consistent read option (GSI par nahi).
-   **Transactions:** `TransactWriteItems` limited (item count/size
    limits --- current docs verify karein).
-   **Capacity:** on-demand vs provisioned; throttling; retries.
-   **No joins:** denormalization; single-table design ka trade-off
    (readability vs efficiency).
-   **Idempotency:** conditional writes (`attribute_not_exists`) se
    dedup --- Inbox pattern ka DynamoDB version.

**Q. SQL Server vs DynamoDB kab?**
Relational integrity, ad-hoc queries, complex joins/reporting → SQL
Server. Predictable key-based access, scale/latency, flexible schema →
DynamoDB. Aapke projects mein dono kyun use hue: `[apna actual
detail]`.

## 6.2 Cosmos DB aur Azure Blob (EY)

-   **Cosmos DB:** partition key choice, RU (request unit) based cost,
    consistency levels (strong → eventual), cross-partition query cost.
-   **Blob Storage:** large files (e.g. Excel exports/bulk data) DB se
    bahar; metadata DB mein, file blob mein; SAS tokens/pre-signed
    access.
-   **Q. 100,000+ tax records bulk retrieval/Excel export kaise
    efficient banaya?** `[apna actual detail]` Possible points:
    pagination/streaming, async job + status polling (long request na
    rakhein), chunked processing, avoid loading everything in memory,
    background export + download link.

------------------------------------------------------------------------

# 7. EY: Monolith → microservices story

Resume mein migration ka experience hai; isko Phase 1-3, 6 ke
framework se bolein:

1.  **Why:** `[apna actual detail]` (scaling, release independence,
    team autonomy).
2.  **How:** incremental (Strangler Fig), pehle low-risk capability.
3.  **Data:** monolith DB se service-owned data (Cosmos DB/Blob) ki
    taraf; backfill + reconciliation.
4.  **Trade-offs:** operational complexity, distributed failures,
    eventual consistency.
5.  **Result:** `[apna actual detail]`.

**Honest framing:** agar aap migration *team ka hissa* the (lead nahi),
to "main X component par kaam kiya" kahein --- resume bhi "worked ... as
part of the migration" kehta hai. Overclaim se bachein.

------------------------------------------------------------------------

# 8. Leadership (5 engineers)

**Resume claim:** 3 saal team lead --- task allocation, sprint planning,
estimation, code review, technical direction, interview participation,
client/Product Owner collaboration.

Prepare stories (STAR format):

-   **Underperformer / skill gap** ko kaise handle kiya.
-   **Technical disagreement** (aap vs senior/peer) kaise resolve hua.
-   **Production incident** mein leadership (Phase 5 postmortem
    framework).
-   **Estimation miss** aur client expectation management.
-   **Mentoring:** kisi engineer ki growth ka concrete example.
-   **Code review standards:** kya enforce karte ho (tests, naming,
    security, small PRs).
-   **Staff-level scope:** team ke bahar influence --- SDD adoption,
    release automation, standards.

**Q. Staff Engineer aur Senior Engineer mein farak?**
Scope aur leverage: Senior apne tasks/features ko deliver karta hai;
Staff multiple teams/systems ke technical direction, standards, risk
aur long-term decisions par impact karta hai, aur doosron ko
effective banata hai.

------------------------------------------------------------------------

# 9. Frontend (Angular/TypeScript) aur dashboard

Dashboard ke liye interview angles:

-   **State management:** component state vs RxJS/store; subscriptions
    ka cleanup (memory leaks).
-   **Log viewer:** pagination/virtual scroll, server-side filtering,
    time-range queries --- millions of log lines browser mein na
    load karein.
-   **Script editor risk:** role-based access, audit, confirmation,
    validation feedback before publish.
-   **Security:** XSS (log content sanitize), token handling, CSRF/CORS
    (Phase 4).
-   **Performance:** lazy loading, change detection strategy
    (`OnPush`), bundle size, Lighthouse metrics.

------------------------------------------------------------------------

# 10. Earlier roles --- kya poochha ja sakta hai

-   **Infosys (trading order entry):** financial correctness, 10,000+
    accounts, order idempotency, audit trail, transactions.
-   **IGT (voucher, refunds via SOAP/ODS):** SOAP integration, refund
    to original payment, bulk Excel-driven workflow, error handling.
-   **EY Market Price Reference Guide:** calculation correctness,
    decimal precision, data modelling.

Inke liye 1-2 line ka "kya seekha jo ab staff-level decisions mein kaam
aata hai" ready rakhein.

------------------------------------------------------------------------

# 11. Resume gaps / honest answers

  ----------------------------------------------------------------------
  Topic                    Honest, strong answer pattern
  ------------------------ ---------------------------------------------
  Kubernetes               "Mera focus Lambda/ECS-style AWS services par
                           raha; K8s concepts (Phase 7) jaanta hoon,
                           production ownership `[apna actual level]`."

  Event streaming (Kafka)  SQS/SNS-style messaging ka experience batayein;
                           Kafka ke trade-offs conceptually explain karein.

  Azure depth              AZ-900 + EY project; deeper Azure services par
                           honest level batayein.

  Formal system design     Phase 8 ke framework (requirements → capacity
  at huge scale            → components → failures) se approach dikhayein;
                           DMS ka scale (100K/month) honestly bolein.
  ----------------------------------------------------------------------

------------------------------------------------------------------------

# 12. Resume improvement suggestions

-   Metrics ke saath **measurement method** ek line mein add karein
    (e.g. "measured over last 10 features").
-   "Terraform/CDKTF" → "Terraform" (ya migration plan ke saath).
-   Roslyn scripting ko ek separate bullet mein highlight karein
    (security controls ke saath) --- yeh differentiator hai.
-   "Contributed to ..." jaise vague verbs ko ownership-clear verbs se
    replace karein jahan aapne actually lead kiya.
-   Summary mein ek impact line add karein (availability, release
    time, team size).

------------------------------------------------------------------------

# 13. Practice questions (resume-based)

1.  DMS integration platform ka end-to-end flow whiteboard par samjhao.
2.  Roslyn scripts ko secure aur reliable kaise banaoge?
3.  99.9% availability ka SLI/SLO aur measurement kaise define kiya?
4.  Release cycle 5 → 3 din: bottleneck kya tha, kya automate kiya?
5.  SDD ke 25%/40% numbers ka evidence kya hai?
6.  DynamoDB mein hot partition kaise pehchanoge/fix karoge?
7.  EY migration mein aapka exact role aur ek failure/learning?
8.  Team ke ek low performer ko kaise handle kiya?
9.  CDKTF deprecated hai --- aapka plan?
10. Agar aapko DMS platform dobara design karna ho to kya alag
    karoge?

------------------------------------------------------------------------

## Phase 11 --- Key Interview Message

> **"Mera experience integration-heavy enterprise systems mein hai:
> reliable deal translation across third-party platforms, runtime-
> configurable dealer logic with guardrails, aur automated, measurable
> delivery pipelines. Main numbers ko measurement method ke saath bolta
> hoon, trade-offs honestly batata hoon, aur team ko technical direction
> deta hoon."**
