# Deployment Strategies — Senior .NET Interview Guide

> Consolidated from personal notes + gap-filled for senior/lead-level .NET full-stack interviews (2026).

## Table of Contents

1. [Core Concepts](#core-concepts)
   - [What "Deployment Strategy" Actually Means](#what-deployment-strategy-actually-means)
   - [Blue-Green Deployment](#blue-green-deployment)
   - [Canary Deployment](#canary-deployment)
   - [Rolling Deployment](#rolling-deployment)
   - [[new content] Comparison Table: Blue-Green vs Canary vs Rolling vs Recreate](#new-content-comparison-table-blue-green-vs-canary-vs-rolling-vs-recreate)
2. [Feature Flags / Feature Toggles](#feature-flags--feature-toggles)
   - [What Is a Feature Toggle](#what-is-a-feature-toggle)
   - [Why Use Feature Toggles](#why-use-feature-toggles)
   - [Implementing Feature Toggles with LaunchDarkly in .NET](#implementing-feature-toggles-with-launchdarkly-in-net)
   - [Testing Feature Toggles](#testing-feature-toggles)
   - [Real-World Use Cases](#real-world-use-cases)
   - [[new content] Feature Flags in a Microservices Architecture](#new-content-feature-flags-in-a-microservices-architecture)
   - [[new content] Feature Flag Types and Anti-Patterns](#new-content-feature-flag-types-and-anti-patterns)
3. [Intermediate Topics](#intermediate-topics)
   - [[new content] Zero-Downtime Database Migrations](#new-content-zero-downtime-database-migrations)
   - [[new content] Rollback Strategy for Stateful Services](#new-content-rollback-strategy-for-stateful-services)
   - [[new content] Deployment in Kubernetes / AKS / EKS](#new-content-deployment-in-kubernetes--aks--eks)
4. [Advanced Topics](#advanced-topics)
   - [[new content] Progressive Delivery](#new-content-progressive-delivery)
   - [[new content] GitOps](#new-content-gitops)
   - [[new content] Deployment Strategies: Microservices vs Monolith](#new-content-deployment-strategies-microservices-vs-monolith)
   - [[new content] Dark Launches / Shadow Traffic / A-B Testing at Infrastructure Level](#new-content-dark-launches--shadow-traffic--ab-testing-at-infrastructure-level)
   - [[new content] CI/CD Pipeline Design for .NET (Azure DevOps / GitHub Actions)](#new-content-cicd-pipeline-design-for-net-azure-devops--github-actions)
   - [[gaps] AWS-Specific Deployment Mechanics](#gaps-aws-specific-deployment-mechanics)
5. [Best Practices](#best-practices)
6. [Common Pitfalls](#common-pitfalls)
7. [Sample Interview Q&A](#sample-interview-qa)
8. [Summary of Additions](#summary-of-additions)
9. [Summary of \[gaps\] Additions (This Pass)](#summary-of-gaps-additions-this-pass)

---

## Core Concepts

### What "Deployment Strategy" Actually Means

Deployment strategies exist to answer three questions simultaneously: how do we ship changes to production **without downtime**, how do we **limit blast radius** when something goes wrong, and how do we **roll back fast** when it does. Every strategy below is a different trade-off between infrastructure cost, deployment speed, and risk exposure — there's no single "best" strategy; the right one depends on statefulness of the service, regulatory/compliance constraints, team maturity (monitoring, automation), and cost tolerance.

An interviewer asking about deployment strategies is rarely just asking "define blue-green." They're probing whether you've actually operated a production system through a bad deploy — do you understand traffic shifting mechanics, session affinity, database compatibility windows, and observability requirements that make each strategy work in practice.

### Blue-Green Deployment

**How it works:**
- Two identical, fully-provisioned environments: **Blue** (current production) and **Green** (new version).
- Green is deployed and tested in isolation (smoke tests, synthetic traffic) while Blue continues serving all real traffic.
- Once Green is verified stable, traffic is switched from Blue to Green — usually atomically, via a router/load balancer/DNS/CNAME swap.
- If issues arise post-switch, rollback is just flipping traffic back to Blue.

**Advantages**
- Zero downtime during the cutover itself.
- Instant rollback (traffic switch, not a redeploy).
- Full testing/validation of Green against production-like infrastructure before any real user sees it.

**Disadvantages**
- Expensive — you pay for double the infrastructure (even if only briefly).
- Needs a reliable traffic-switching mechanism (load balancer, service mesh, DNS) — DNS-based switching in particular suffers from TTL/caching delays, which is a common gotcha.
- Database/schema state is shared or must be handled carefully (see [Zero-Downtime Database Migrations](#new-content-zero-downtime-database-migrations) below) — the "two identical environments" model breaks down fast once you have a shared, stateful backing store.

**Real-world example — AWS Elastic Beanstalk:**

```bash
# Deploy the new version in Green
eb create green-environment

# Swap Blue and Green environments (CNAME swap)
eb swap-environment-cnames --source-environment blue-environment --destination-environment green-environment
```

Scenario walkthrough (banking app):
1. Blue (v1.0) is live and serving all traffic.
2. v2.0 is deployed into Green.
3. Automated + manual tests run against Green.
4. If Green passes, swap traffic to Green.
5. If v2.0 misbehaves, swap back to Blue instantly.

**Interviewer follow-ups to expect:** "What happens to in-flight requests during the swap?" (connection draining / graceful shutdown needed), "What about the database?" (usually shared, so this is really blue-green at the *app tier* only, not full-stack), "How do you handle session state?" (sticky sessions break blue-green — prefer stateless services + distributed cache/session store).

### Canary Deployment

**How it works:**
- Deploy the new version to a small slice of users/traffic (e.g., 5–10%).
- Monitor error rates, latency, business metrics.
- If healthy, progressively increase traffic (e.g., 5% → 25% → 50% → 100%).
- If problems surface at any stage, halt and roll back that slice.

**Advantages**
- Minimizes blast radius — only a small user cohort is exposed to a bad release.
- Data-driven rollout: real production signals (not just synthetic tests) gate promotion.
- Gradual, controlled exposure instead of an all-or-nothing cutover.

**Disadvantages**
- Slower to reach 100% rollout — multiple stages, each needing a soak/bake period.
- Requires real traffic-splitting infrastructure (service mesh, ingress with weighted routing, or an API gateway) plus solid observability to detect regressions at a statistically meaningful sample size.

**Real-world example — Netflix**, rolling out a new streaming feature:
1. Deploy v2.0 to 5% of users.
2. Monitor performance, error logs, user feedback.
3. If healthy, bump to 50%.
4. If still healthy, go to 100%.

**Kubernetes implementation (from notes):**

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: my-app
spec:
  replicas: 10
  selector:
    matchLabels:
      app: my-app
  template:
    metadata:
      labels:
        app: my-app
        version: canary
    spec:
      containers:
      - name: my-app
        image: my-app:v2.0
        ports:
        - containerPort: 80
```

Weighted traffic split via Ingress:

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: my-app-ingress
spec:
  rules:
  - http:
      paths:
      - path: /my-app
        backend:
          service:
            name: my-app
            port:
              number: 80
        weight: 20 # Send 20% traffic to Canary
```

> **Note (accuracy flag):** Plain Kubernetes `Ingress` (networking.k8s.io/v1) does not natively support traffic-weight annotations — the `weight` field shown is not part of the vanilla Ingress spec. Weighted canary routing like this is typically achieved via an ingress controller extension (e.g., **NGINX Ingress** `nginx.ingress.kubernetes.io/canary-weight` annotation), a service mesh (**Istio VirtualService** weighted routing), or a progressive-delivery controller (**Flagger**, **Argo Rollouts**). The YAML in the original notes illustrates *intent* correctly but the raw manifest as written would not work unmodified on stock Kubernetes — flagging this as a simplification rather than a contradiction, since it's the only source describing it. (verify against your ingress controller's actual annotation syntax before using in a real cluster.)

**Interviewer follow-ups:** "How do you decide the traffic split increments and bake time?" (SLO-based automated gates, not gut feel), "How do you pick which users see canary?" (random sampling vs. sticky-by-user-id — consistency matters for UX), "What's your automatic rollback trigger?" (error rate / latency SLO breach → auto-abort, ideally via a controller like Flagger/Argo Rollouts, not a human watching a dashboard).

### Rolling Deployment

> **[new content]** — not in original notes; this is a core comparison point interviewers expect you to know alongside blue-green/canary.

- Instances of the old version are replaced by the new version incrementally, a few at a time (e.g., Kubernetes `Deployment` default `RollingUpdate` strategy with `maxSurge`/`maxUnavailable`).
- No second full environment needed — cheaper than blue-green.
- Rollback is *not instant* — you have to roll the same incremental process in reverse, which takes time proportional to fleet size.
- Mid-rollout, **two versions run simultaneously** serving live traffic — this has the same "N and N+1 compatibility" requirement as canary and blue-green (API/schema backward compatibility is mandatory during the transition window).
- Good default for stateless services where cost matters more than instant rollback.

### [new content] Comparison Table: Blue-Green vs Canary vs Rolling vs Recreate

| Strategy | Infra Cost | Rollout Speed | Rollback Speed | Risk Exposure | Needs Traffic Splitting? | Typical Use Case |
|---|---|---|---|---|---|---|
| **Recreate** (stop old, start new) | Low | Fast | Slow (redeploy old) | High (downtime) | No | Dev/test environments, non-critical batch jobs |
| **Rolling** | Low–Medium | Medium | Medium (reverse rollout) | Medium | No (LB just drains) | Default for stateless microservices, cost-sensitive teams |
| **Blue-Green** | High (2x infra) | Fast cutover | Instant (flip traffic back) | Low (fully tested before switch) | Yes (router/DNS/LB swap) | Regulated/critical systems needing instant, clean rollback |
| **Canary** | Medium (extra small fleet) | Slow (staged) | Fast (stop rollout, drain canary) | Lowest (limited blast radius) | Yes (weighted routing) | High-traffic consumer apps, gradual validated rollout |

```mermaid
flowchart LR
    subgraph BlueGreen[Blue-Green]
    A1[100% Blue] -->|instant switch| A2[100% Green]
    end
    subgraph Canary[Canary]
    B1[100% v1] --> B2[95% v1 / 5% v2] --> B3[50% v1 / 50% v2] --> B4[100% v2]
    end
    subgraph Rolling[Rolling]
    C1[Pod1:v1 Pod2:v1 Pod3:v1] --> C2[Pod1:v2 Pod2:v1 Pod3:v1] --> C3[Pod1:v2 Pod2:v2 Pod3:v1] --> C4[Pod1:v2 Pod2:v2 Pod3:v2]
    end
```

---

## Feature Flags / Feature Toggles

### What Is a Feature Toggle

A Feature Toggle (Feature Flag) decouples **deployment** from **release**: code ships to production dark (disabled), and is turned on independently — for specific users, percentages, or environments — without a redeploy. This is the mechanism that underlies trunk-based development and continuous delivery: you can merge and deploy incomplete or risky work continuously as long as it's flag-guarded.

Feature toggles let teams:
- Gradually roll out features to specific users.
- Test in production without affecting everyone.
- Instantly disable a feature if an issue occurs — no redeploy required.

### Why Use Feature Toggles

- **Risk-free deployments** — features are off by default, enabled progressively.
- **Instant rollback** — disable the flag instead of rolling back the whole deployment (much faster MTTR than any deployment-strategy-level rollback).
- **A/B testing** — enable features for different cohorts and measure impact.
- **Continuous delivery** — ship incomplete work behind a flag, finish it later, flip it on when ready.

### Implementing Feature Toggles with LaunchDarkly in .NET

**1. Create a LaunchDarkly account**, create a project, get the SDK key.

**2. Install the SDK:**

```bash
dotnet add package LaunchDarkly.ServerSdk
```

**3. Configure the SDK key** in `appsettings.json`:

```json
{
  "LaunchDarkly": {
    "SdkKey": "your-launchdarkly-sdk-key"
  }
}
```

> **Note (secrets handling — gap flagged):** Never commit a real SDK key into `appsettings.json` in source control. In production, use **Azure Key Vault**, **User Secrets** (dev), or environment variables/App Configuration, and bind via `IConfiguration` as shown — just don't check the literal key in.

**4. Initialize the LaunchDarkly client:**

```csharp
using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server;
using Microsoft.Extensions.Configuration;
using System;

public class LaunchDarklyService : IDisposable
{
    private readonly LdClient _ldClient;

    public LaunchDarklyService(IConfiguration configuration)
    {
        var sdkKey = configuration["LaunchDarkly:SdkKey"];
        _ldClient = new LdClient(sdkKey);
    }

    public bool IsFeatureEnabled(string featureFlagKey, string userKey)
    {
        var user = User.WithKey(userKey);
        return _ldClient.BoolVariation(featureFlagKey, user, false); // Default is false
    }

    public void Dispose()
    {
        _ldClient.Dispose();
    }
}
```

> **Note (lifetime gotcha — gap flagged):** `LdClient` maintains a persistent streaming connection and internal caches; it is expensive to construct and must be a **singleton** for the lifetime of the app, not created per-request. Register it as `services.AddSingleton<LaunchDarklyService>()` in DI. Constructing a new `LdClient` per request (a mistake beginners make) will exhaust connections and add multi-second startup latency to every call, since `LdClient` blocks briefly on initial connection by default.

**5. Use the toggle in a controller:**

```csharp
using Microsoft.AspNetCore.Mvc;

[Route("api/feature")]
[ApiController]
public class FeatureController : ControllerBase
{
    private readonly LaunchDarklyService _ldService;

    public FeatureController(LaunchDarklyService ldService)
    {
        _ldService = ldService;
    }

    [HttpGet("dark-mode")]
    public IActionResult GetFeatureStatus()
    {
        string featureFlagKey = "dark-mode";
        string userKey = "user-123"; // dynamic, from the authenticated user

        bool isEnabled = _ldService.IsFeatureEnabled(featureFlagKey, userKey);

        return Ok(new
        {
            message = isEnabled
                ? "Dark Mode is ENABLED for you!"
                : "Dark Mode is DISABLED for you."
        });
    }
}
```

### Testing Feature Toggles

1. In the LaunchDarkly dashboard, create a flag with key `dark-mode` and target a specific user (`user-123`).
2. Call the endpoint: `GET http://localhost:5000/api/feature/dark-mode`.
3. Enabled → `{ "message": "Dark Mode is ENABLED for you!" }`
4. Disabled → `{ "message": "Dark Mode is DISABLED for you." }`

### Real-World Use Cases

- **Facebook Dark Mode** — limited rollout before general availability.
- **Netflix UI updates** — A/B testing UI variants across user segments.
- **Amazon promotions** — discounts scoped to Prime members only.

### [new content] Feature Flags in a Microservices Architecture

The original notes end with an open, unanswered question: *"Would you like help setting up feature flags in a microservices architecture?"* Here's the complete senior-level answer.

In a microservices architecture, feature flags introduce cross-service consistency problems that don't exist in a monolith:

- **Centralize flag evaluation.** Don't let each service maintain its own ad-hoc flag logic. Use a shared flag platform (LaunchDarkly, Azure App Configuration + Feature Management, Unleash, Split) so all services agree on flag state, targeting rules, and audit history.
- **Propagate flag context consistently.** If a user-facing request fans out to five downstream services, the *same* targeting context (user ID, tenant, region) must be passed along (e.g., via a header or the distributed trace context) so all services evaluate the flag the same way for that request. Inconsistent evaluation across services in the same request is a classic bug — e.g., Service A shows the new checkout UI but Service B's API still validates against the old contract.
- **Version/contract compatibility.** A flag toggling a new feature in Service A often requires Service B to already support the new contract. This means the flag's "on" state has a **dependency graph** — document and, where possible, enforce flag dependencies (some platforms support prerequisite flags).
- **Local caching + streaming updates.** Each service's SDK should cache flag values locally (in-memory) and get push updates via streaming/polling, not call the flag service synchronously per request — that would add a network hop and single point of failure to every request path.
- **Kill switches vs. release flags vs. experiment flags** — in a microservices setup, kill switches (see below) are especially important because a bad release in one service can cascade failures through the call graph; being able to instantly disable one feature without a coordinated multi-service rollback is a major operational win.
- **Flag lifecycle/cleanup governance matters more** at scale — with dozens of services each accumulating flags, stale flags become a distributed technical-debt and security-audit problem (old flags gating deprecated code paths that nobody remembers exist).

### [new content] Feature Flag Types and Anti-Patterns

Interviewers often probe whether you understand that "feature flag" isn't one thing:

| Flag Type | Lifespan | Purpose |
|---|---|---|
| **Release flag** | Short (days–weeks) | Decouple deploy from release; removed after full rollout |
| **Experiment flag** (A/B) | Medium | Drive experimentation/analytics; removed after experiment concludes |
| **Ops / kill switch** | Long-lived | Circuit-breaker for a feature/dependency; kept permanently for operability |
| **Permission / entitlement flag** | Permanent | Gate features by plan/tier (e.g., Prime-only discounts) — business logic, not a deploy mechanism |

**Common anti-patterns:**
- **Flag debt** — never removing release flags after 100% rollout, leading to a codebase littered with dead `if` branches and combinatorial testing explosion.
- **Flags wrapping non-idempotent side effects** (e.g., toggling a flag mid-transaction that has already written half its state) — flags should gate *behavior*, not be flipped inside a unit of work.
- **Testing only the "on" and "off" paths, never both in combination** with other active flags — flag interaction bugs are a real production risk at scale.

---

## Intermediate Topics

### [new content] Zero-Downtime Database Migrations

This is arguably the single most commonly-missed topic in notes that only cover app-tier deployment strategies — and a favorite senior-level interview question, because blue-green/canary/rolling all assume the **database is shared** across old and new versions during the transition window.

**The core rule: expand/contract (a.k.a. parallel change).**

1. **Expand** — add the new schema element (column, table) without removing or renaming anything old. Both old and new app code must work against this schema.
2. **Migrate/backfill** — backfill data into the new structure, dual-write if needed (old code writes old+new, or a background job syncs).
3. **Contract** — once all instances run the new code and are verified, remove the old schema element in a later, separate deployment.

```mermaid
flowchart LR
    A[Deploy N: add new column, keep old] --> B[Deploy N: app writes to both old+new]
    B --> C[Backfill historical data]
    C --> D[Deploy N+1: app reads/writes new column only]
    D --> E[Deploy N+2: drop old column]
```

**Concrete rules of thumb:**
- Never do a breaking rename in one step — it's always "add new" → "migrate" → "remove old" across multiple deploys.
- Additive changes (`ADD COLUMN` nullable, new table) are generally safe mid-rollout.
- Destructive changes (`DROP COLUMN`, `NOT NULL` without a default, renaming) must wait until **all** instances (100% of the fleet) run code that no longer references the old shape.
- For EF Core specifically: avoid `Database.Migrate()` running automatically on app startup in a multi-instance rolling deployment — multiple instances racing to apply migrations concurrently can deadlock or corrupt state. Run migrations as a **separate, single pre-deploy step** (CI/CD migration job) before the new app version starts receiving traffic.
- Large table migrations (adding indexes, backfills) should be done **online/incrementally** (batched updates, `CREATE INDEX ONLINE`/concurrently) to avoid locking production tables.
- For blue-green specifically: since Blue and Green typically point at the **same database**, "blue-green for the database" is a much harder, different problem (often solved via read replicas + cutover, or by treating the DB as append-only during transition). Don't conflate app-tier blue-green with DB-tier blue-green in an answer — call out explicitly that they're usually decoupled.

### [new content] Rollback Strategy for Stateful Services

Rollback is trivial to describe for stateless services ("redeploy old image" / "flip traffic back") but the harder senior-level question is: **what happens to state?**

- **Schema compatibility** — per expand/contract above, rollback only works cleanly if the *previous* version of the code can still run against the *current* state of the schema/data. This is why destructive migrations must lag behind code deploys by a full contract cycle — it preserves your ability to roll back.
- **Message queue / event-driven state** — if the new version changed a message schema (e.g., a new required field in a Kafka/Service Bus event), rolling back the consumer without also handling in-flight messages produced by the new version can cause deserialization failures. Use schema versioning/tolerant readers (ignore unknown fields) to keep rollback safe.
- **Cache poisoning** — if the new version wrote data to a shared cache (Redis) in a new shape, rolling back code without invalidating/flushing the affected cache keys can cause the old code to crash on unexpected shapes. Version your cache keys (e.g., `user:v2:{id}`) so old and new code don't collide.
- **Idempotency for replays** — rollback often means replaying/retrying operations; ensure operations are idempotent (idempotency keys on payment/order APIs) so a rollback-triggered retry doesn't double-charge or double-write.
- **Stateful sets in Kubernetes** (databases, brokers running in-cluster) — rolling back a `StatefulSet` is fundamentally different from a `Deployment`: ordered, one-at-a-time rollback, and you must consider whether the persistent volume's data is still compatible with the older container image.
- **Feature-flag-first rollback** — the fastest, safest "rollback" for many production incidents is disabling the feature flag guarding the risky code path, rather than a full deployment rollback. This should be your first lever, with infrastructure rollback as the fallback for issues flags can't cover (e.g., a bad container image, not bad logic).

### [new content] Deployment in Kubernetes / AKS / EKS

Since a large chunk of the source notes' YAML examples target Kubernetes, this section fills in the platform-level context a senior .NET/cloud interview expects:

- **Deployment object** — default strategy is `RollingUpdate` with configurable `maxSurge` (how many extra pods above desired count during rollout) and `maxUnavailable` (how many can be down). Tuning these controls rollout speed vs. resource headroom vs. availability.
- **Readiness vs. liveness probes** — a rolling update only routes traffic to pods that pass **readiness** checks; a pod can be "alive" (liveness passes) but not yet ready to serve (still warming caches/connections). Misconfigured probes are the #1 cause of "rolling deploy caused a brief outage" incidents — traffic gets sent to pods before they're actually ready, or `livenessProbe` kills healthy-but-slow-starting .NET pods (cold JIT/ReadyToRun startup) before they ever become ready.
- **PodDisruptionBudget (PDB)** — guarantees a minimum number of healthy replicas during voluntary disruptions (node drains, cluster upgrades) — separate from, but interacting with, your deployment strategy.
- **Managed cluster specifics (AKS/EKS):**
  - **AKS**: integrates with Azure Load Balancer / Application Gateway Ingress Controller (AGIC) for weighted/canary routing; Azure DevOps and GitHub Actions both have first-class AKS deploy tasks (`kubectl`, Helm, or `azure/k8s-deploy`).
  - **EKS**: typically paired with AWS ALB Ingress Controller or App Mesh/Istio for traffic shaping; often driven via CodeDeploy's native Blue/Green and Canary support for ECS/EKS, or Argo Rollouts.
  - Both support **Flagger** or **Argo Rollouts** as progressive-delivery controllers layered on top of raw Kubernetes primitives — this is what most real canary/blue-green implementations actually use in 2026, rather than hand-rolled Ingress weight annotations.
- **Helm / Kustomize** — templating for environment-specific manifests (dev/stage/prod) — expect a question on how you avoid config drift across environments; answer: single source of templated manifests + environment-specific values files, promoted through environments via CI, not hand-edited per environment.

---

## Advanced Topics

### [new content] Progressive Delivery

"Progressive delivery" is the umbrella term (coined by Weaveworks/James Governor) for canary + feature flags + automated analysis combined — it's the term you should use in a senior interview to show you're current, rather than treating canary/blue-green/flags as three unrelated ideas.

- **Automated analysis/gating** — tools like **Flagger** or **Argo Rollouts** watch Prometheus/Datadog metrics (error rate, latency, custom business metrics) during a canary stage and automatically promote or abort — removing the human-watching-a-dashboard step.
- **Combines with feature flags** — you can canary-deploy the *infrastructure* (new pods) while separately flag-gating the *feature* — two independent risk levers.
- **Traffic mirroring/shadowing** — send a copy of production traffic to the new version without returning its response to the user, to validate behavior/performance under real load with zero user-facing risk (see Dark Launches below).
- Expect an interviewer to ask: "How is progressive delivery different from canary?" — answer: canary is a *technique*; progressive delivery is the *practice* of automating canary analysis, flags, and rollback decisions end-to-end so releases require no manual gate-watching.

### [new content] GitOps

- **Definition**: the desired state of your infrastructure/deployments is declared in Git (manifests, Helm values, Kustomize overlays); a controller (**ArgoCD**, **Flux**) continuously reconciles the live cluster state to match Git, rather than a CI pipeline imperatively running `kubectl apply`.
- **Why it matters for deployment strategy**: Git becomes the single audit trail and rollback mechanism — a bad deploy's rollback is literally `git revert` on the manifest, and the GitOps controller reconciles the cluster back automatically. This is a stronger, more auditable rollback story than "re-run the old pipeline."
- **Push vs. pull model**: traditional CI/CD *pushes* changes to the cluster (CI has cluster credentials — a security surface). GitOps controllers *pull* from Git and reconcile from inside the cluster — no external system needs prod cluster credentials, which is a common security/compliance talking point.
- **Drift detection**: GitOps continuously detects and can auto-correct manual `kubectl` changes made outside of Git ("configuration drift") — valuable in regulated environments where you need guaranteed state.
- Ties directly into canary/progressive delivery: **Argo Rollouts** integrates with ArgoCD to make canary/blue-green a GitOps-native, declarative resource (`Rollout` CRD replacing `Deployment`).

### [new content] Deployment Strategies: Microservices vs Monolith

A senior/lead-level differentiator question:

| Aspect | Monolith | Microservices |
|---|---|---|
| **Unit of deployment** | Whole app, one artifact | Each service independently |
| **Blue-green cost** | One large environment duplicated (simpler, but expensive to double) | Can blue-green per-service (cheaper per-service, but coordinating N services' versions is complex) |
| **Canary granularity** | Coarse — canary the whole app | Fine-grained — canary one service without touching others |
| **Contract/versioning risk** | Low (single deployable, internal calls are in-process) | High — every service boundary needs backward/forward-compatible contracts during independent rollouts (see API versioning, consumer-driven contracts) |
| **Rollback blast radius** | All-or-nothing rollback of the whole app | Can roll back just the offending service |
| **Database migration coordination** | Single DB, single migration path (still needs expand/contract) | Potentially many DBs (database-per-service) — migrations are independent per service, but cross-service data consistency during a partial rollout is a real design problem (sagas/eventual consistency) |
| **Deployment frequency** | Often lower (bigger, riskier releases) | Higher (small, frequent, isolated releases) — this is a primary reason teams adopt microservices |
| **Tooling complexity** | Lower — one pipeline | Higher — orchestration, service mesh, distributed tracing become near-mandatory to safely operate independent deploys |

Key interview point: microservices don't make deployment *strategies* different in kind — blue-green/canary/rolling all still apply — but they multiply the **coordination problem**: independent deploy cadence per service means you must design for N and N+1 version compatibility *continuously*, not just during a deploy window, because at any moment some services are ahead of others.

### [new content] Dark Launches / Shadow Traffic / A-B Testing at Infrastructure Level

- **Dark launch**: deploy and run new code in production, but don't expose its output to real users yet — e.g., run the new recommendation engine in parallel, log its results, compare against the current engine's results offline, without ever showing dark-launch output to a user. This is a validation technique, distinct from canary (which *does* expose real users) and distinct from a feature flag (which is an on/off switch, not a shadow comparison).
- **Traffic mirroring/shadowing**: infrastructure-level dark launch — the load balancer/service mesh duplicates a request to both old and new versions; only the old version's response is returned to the client. Istio and some API gateways support this natively (`mirror` in a VirtualService).
- **A/B testing vs. canary**: canary is a *risk-mitigation* rollout mechanism (temporary, converges to 100% or 0%); A/B testing is an *experimentation* mechanism (can run indefinitely, both variants are "correct," you're measuring a business metric, not safety). Don't conflate them in an answer — interviewers listen for this distinction.

### [new content] CI/CD Pipeline Design for .NET (Azure DevOps / GitHub Actions)

A concrete, current pipeline shape senior .NET candidates should be able to sketch:

```mermaid
flowchart LR
    A[Commit / PR] --> B[Build + Unit Tests]
    B --> C[Static Analysis / SCA / SAST]
    C --> D[Package: dotnet publish -> container image]
    D --> E[Push to ACR/ECR]
    E --> F[Deploy to Dev - auto]
    F --> G[Integration/E2E tests]
    G --> H[Deploy to Staging - auto]
    H --> I[Manual approval gate]
    I --> J[Progressive deploy to Prod: canary/blue-green]
    J --> K[Automated metric analysis]
    K -->|pass| L[100% rollout]
    K -->|fail| M[Auto rollback / abort]
```

- **Immutable artifacts**: build once, promote the *same* container image/artifact through Dev → Staging → Prod — never rebuild per environment (eliminates "works in staging, different bits in prod" class of bugs).
- **Environment-specific config**, not environment-specific builds: use `appsettings.{Environment}.json`, Azure App Configuration, or environment variables/K8s ConfigMaps/Secrets injected at deploy time.
- **Approval gates**: Azure DevOps environments / GitHub Actions environments support required reviewers before a prod deploy job runs — standard for regulated .NET shops (banking, healthcare).
- **Migrations as a pipeline stage**, run once, before the new version receives traffic (ties back to the zero-downtime migration section above).

### [gaps] AWS-Specific Deployment Mechanics

Everything above (blue-green, canary, rolling, progressive delivery, GitOps) is framework-agnostic in principle, but the concrete implementation the notes lean on is almost entirely Kubernetes-flavored (Ingress annotations, Flagger, Argo Rollouts). Since the candidate's target cloud is AWS, this section grounds the same strategies in AWS-native mechanics — and closes the loop on how Terraform/CDKTF (the candidate's actual IaC tool) drives them, rather than hand-run CLI commands.

#### `[gaps]` AWS CodeDeploy — Blue/Green for EC2, ECS, and Lambda

CodeDeploy is AWS's managed deployment orchestrator, and it natively understands blue/green for three different compute targets — a senior AWS answer should distinguish how the mechanics differ per target rather than treating "CodeDeploy blue/green" as one uniform thing:

| Target | What "Blue" and "Green" mean | Traffic-shift mechanism | Rollback |
|---|---|---|---|
| **EC2 (in-place or blue/green)** | Blue = existing ASG/instance fleet; Green = a freshly provisioned, fully-provisioned replacement fleet | CodeDeploy re-registers instances behind the existing ELB/Target Group, or swaps Target Groups entirely | Terminate Green fleet, Blue fleet (still running until cutover completes) keeps serving |
| **ECS** | Blue = current running task set (existing task definition revision); Green = new task set (new task definition revision) on the same service | ALB listener rule shifts traffic between two Target Groups (one per task set) — can be all-at-once, linear, or canary (see below) | CodeDeploy automatically reroutes the ALB listener back to the original (Blue) Target Group |
| **Lambda** | Blue = currently aliased function version; Green = newly published function version | Lambda **alias traffic shifting** — the alias's weighted routing config shifts a percentage of invocations to the new version | Alias weight reverts to 100% on the original version |

**CloudWatch Alarms as the automated abort trigger** — for all three targets, CodeDeploy can be wired to CloudWatch Alarms (error rate, latency) so a bad deployment automatically halts and rolls back mid-shift, without a human watching a dashboard — this is the direct AWS-native analog to Flagger/Argo Rollouts' automated canary analysis covered in the Progressive Delivery section above.

#### `[gaps]` ECS Blue/Green Deployments in Detail

Because ECS is the most likely .NET-container deployment target on AWS, it's worth knowing the mechanics one level deeper than the table above:

```mermaid
sequenceDiagram
    participant CD as CodeDeploy
    participant ALB as ALB Listener
    participant TG1 as Target Group (Blue - v1 tasks)
    participant TG2 as Target Group (Green - v2 tasks)
    CD->>TG2: Launch new task set (v2) - not yet receiving traffic
    CD->>CD: Run pre-traffic hook (Lambda) - smoke test v2
    CD->>ALB: Shift traffic to TG2 (all-at-once, linear, or canary %)
    ALB->>TG2: Production traffic now hitting v2
    CD->>CD: Monitor CloudWatch Alarms during bake time
    alt Healthy
        CD->>TG1: Drain and terminate old (v1) task set
    else Alarm triggered
        CD->>ALB: Revert traffic to TG1 (v1)
        CD->>TG2: Terminate v2 task set
    end
```

- ECS supports this natively via the **`CODE_DEPLOY` deployment controller** on the service (as opposed to the default **`ECS` rolling-update controller**, which is ECS's own equivalent of a Kubernetes `RollingUpdate` — no second Target Group, no traffic-shift granularity, just incremental task replacement).
- Traffic-shift options mirror what Flagger/Argo Rollouts offer for Kubernetes: **Canary** (e.g., 10% for N minutes, then 100%), **Linear** (fixed percentage increments on a timer), or **AllAtOnce** (full cutover, relying purely on the pre/post-traffic hooks and alarms for safety).
- **Pre-traffic and post-traffic Lambda hooks** let you run validation (smoke tests, synthetic checks) before real traffic ever reaches Green, and again after the shift completes — the AWS-native equivalent of the "automated analysis gate" concept from Progressive Delivery.

#### `[gaps]` Route 53 Weighted Routing for Gradual Traffic Shifting

Where CodeDeploy operates at the compute/target-group layer, **Route 53 weighted routing policies** are the DNS-layer alternative for gradual traffic shifting — most relevant when the two environments are more coarsely separated (e.g., two entirely separate stacks/regions, or a blue-green cutover above the load-balancer level rather than within one ALB):

```
Record: api.example.com
  → Weighted record A (Blue stack ALB)   weight = 90
  → Weighted record B (Green stack ALB)  weight = 10
```

- Route 53 resolves each DNS query probabilistically according to the weight ratio — over enough queries, ~10% of *new* connections land on Green.
- **The same DNS TTL/caching gotcha flagged elsewhere in this guide applies here too**: clients (and resolver caches) that already resolved and cached the Blue IP won't re-resolve until their TTL expires, so a weighted Route 53 shift is gradual and "fuzzy" at the edges, not a precise, instant percentage — a materially different characteristic from an ALB-layer or service-mesh-layer traffic split, which shifts existing *connections* more precisely and immediately.
- Often paired with **Route 53 health checks** so a weighted record is automatically pulled out of rotation if the target ALB/endpoint starts failing health checks — a coarser, DNS-layer safety net compared to CodeDeploy's CloudWatch-Alarm-driven abort.
- Typical use case: cross-region blue-green (e.g., shifting traffic from an `us-east-1` stack to a newly-deployed `us-west-2` stack) where there's no single shared load balancer to place a canary/blue-green controller in front of — DNS-weighted routing is the natural mechanism at that scope.

#### `[gaps]` Terraform/CDKTF Driving Deployment Orchestration

The candidate's actual IaC tooling is Terraform/CDKTF, and while Terraform is fundamentally a *provisioning* tool (declarative infrastructure state), not a deployment *orchestrator* in the CodeDeploy/Argo Rollouts sense, it plays a specific, important role in this picture that's worth being able to articulate precisely rather than conflating the two:

- **Terraform provisions the deployment machinery itself** — the CodeDeploy Application/DeploymentGroup resources, the ECS service's `deployment_controller` block, the ALB listener rules and both Target Groups, the Lambda alias and its `routing_config` weighted-alias block, and the Route 53 weighted record sets. In other words: Terraform/CDKTF sets up *the rails*; CodeDeploy (or a CI/CD pipeline step) *drives the train* along them on each release.
  ```hcl
  resource "aws_codedeploy_deployment_group" "ecs_bluegreen" {
    app_name               = aws_codedeploy_app.order_api.name
    deployment_group_name  = "order-api-dg"
    service_role_arn       = aws_iam_role.codedeploy.arn
    deployment_config_name = "CodeDeployDefault.ECSLinear10PercentEvery1Minute"

    deployment_style {
      deployment_type   = "BLUE_GREEN"
      deployment_option  = "WITH_TRAFFIC_CONTROL"
    }

    blue_green_deployment_config {
      terminate_blue_instances_on_deployment_success {
        action                           = "TERMINATE"
        termination_wait_time_in_minutes = 5
      }
    }

    ecs_service {
      cluster_name = aws_ecs_cluster.main.name
      service_name = aws_ecs_service.order_api.name
    }

    load_balancer_info {
      target_group_pair_info {
        target_group {
          name = aws_lb_target_group.blue.name
        }
        target_group {
          name = aws_lb_target_group.green.name
        }
        prod_traffic_route {
          listener_arns = [aws_lb_listener.https.arn]
        }
      }
    }
  }
  ```
- **What Terraform does *not* do**: it doesn't itself perform the live traffic shift or watch CloudWatch Alarms mid-deployment — that's CodeDeploy's runtime job once a new task definition/Lambda version triggers a deployment. Running `terraform apply` on every code release (re-applying the whole stack per deploy) is the wrong mental model and a common junior mistake; the correct split is: Terraform/CDKTF manages the *infrastructure* (rarely changes), while the CI/CD pipeline (GitHub Actions, in this candidate's case) triggers a CodeDeploy deployment or updates an ECS task definition/Lambda alias on each release — infrastructure changes and application deployments are two different change cadences using two different mechanisms, even though both are "IaC-adjacent."
- **CDKTF specifically** — since CDKTF lets you express the same HCL-equivalent resources in TypeScript/C#, this is also where a .NET-background candidate can note that CDKTF's typed constructs make it straightforward to build a reusable "blue-green ECS service" construct/module shared across services, rather than copy-pasting the CodeDeploy/Target-Group/Listener HCL block per repo — the same DRY argument the GitHub Actions guide makes for reusable workflows applies structurally to CDKTF constructs.
- **Interviewer angle**: expect a direct question like 