# Monolith-to-Microservices Migration Interview Guide --- Hinglish

## Phase 7: Deployment, CI/CD, Containers & ECS/Kubernetes Operations

**Audience:** Senior Software Engineer, Staff Engineer, Tech Lead,
Solution Architect\
**Technology context:** .NET 8, C#, Docker, GitHub Actions,
Terraform/OpenTofu (or AWS CDK), AWS ECS/Fargate, Kubernetes\
**Style:** Hinglish (Roman Hindi + English technical terminology)

------------------------------------------------------------------------

## Phase objective

Microservices ka benefit tabhi milta hai jab services independently
build, test, deploy, observe aur rollback ho sakein. Agar 20 services ko
ek saath release karna pade, to operationally humne distributed monolith
create kiya ho sakta hai.

Is phase mein aap cover karenge:

-   Containerization aur .NET 8 Docker best practices.
-   CI/CD pipeline design and release gates.
-   Rolling, blue/green and canary deployments.
-   AWS ECS/Fargate vs Kubernetes.
-   Configuration, secrets, feature flags and environment separation.
-   Database migration compatibility during releases.
-   Infrastructure as Code and deployment safety.
-   Rollback, roll-forward and production release scenarios.

------------------------------------------------------------------------

# 1. Independent deployability: Microservices ka practical test

A service independently deployable tab hai jab usko release karne ke
liye unrelated services ko same time deploy karna required na ho.

Iske liye:

-   API/event contracts backward-compatible hon.
-   Service apne data writes own kare.
-   Configuration and secrets independently manage hon.
-   Build/test/deploy pipeline per service ya clearly bounded deployable
    unit ho.
-   Health checks and observability release validation support karein.
-   Rollback/roll-forward strategy defined ho.

**Interview point:** Independent deployment ka matlab "har service ko
har waqt deploy karna" nahi. Matlab service ko unrelated service
releases ke bina safely deploy karne ki capability.

------------------------------------------------------------------------

# 2. Typical CI/CD pipeline

``` mermaid
flowchart TD
    A[Commit / Pull Request] --> B[Restore and build]
    B --> C[Unit tests]
    C --> D[Static analysis and dependency scan]
    D --> E[Integration / contract tests]
    E --> F[Build immutable container image]
    F --> G[Generate SBOM and scan image]
    G --> H[Publish image to registry]
    H --> I[Deploy to test environment]
    I --> J[Smoke tests and health checks]
    J --> K{Release gate passes?}
    K -- No --> L[Stop / notify / investigate]
    K -- Yes --> M[Canary or blue-green production release]
    M --> N[Observe metrics and business outcomes]
    N --> O{Healthy?}
    O -- Yes --> P[Complete release]
    O -- No --> Q[Rollback / halt rollout]
```

## Recommended pipeline stages

1.  **Fast checks:** formatting, build, unit tests.
2.  **Quality and security:** static analysis, dependency/container
    scan, secret scan.
3.  **Contract/integration tests:** API schemas, event compatibility,
    database integration.
4.  **Artifact build:** build once, tag immutable image by commit/build
    ID.
5.  **Artifact publish:** push to approved registry.
6.  **Deploy lower environment:** integration, smoke and migration
    checks.
7.  **Release approval/gates:** risk-based; avoid unnecessary manual
    bottlenecks.
8.  **Production rollout:** rolling, canary or blue/green depending on
    risk.
9.  **Post-deploy verification:** error rate, latency, business metrics,
    queue health.
10. **Automatic halt/rollback:** if agreed health gates fail.

Avoid rebuilding a different artifact for every environment. Promote the
same immutable image through environments; inject environment-specific
configuration at deployment time.

------------------------------------------------------------------------

# 3. Dockerizing a .NET 8 service

## 3.1 Multi-stage Dockerfile

Multi-stage build se build tools final runtime image se separate rehte
hain.

``` dockerfile
# Illustrative baseline. Pin approved base-image versions/digests in production.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore layer cache ke liye saare referenced .csproj (aur
# Directory.Build.props / Directory.Packages.props / nuget.config) pehle copy karein.
COPY ["Directory.Build.props", "Directory.Packages.props", "nuget.config", "./"]
COPY ["Sales.Api/Sales.Api.csproj", "Sales.Api/"]
COPY ["Sales.Domain/Sales.Domain.csproj", "Sales.Domain/"]
COPY ["Sales.Infrastructure/Sales.Infrastructure.csproj", "Sales.Infrastructure/"]
RUN dotnet restore "Sales.Api/Sales.Api.csproj"

COPY . .
WORKDIR /src/Sales.Api
RUN dotnet publish "Sales.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# Non-root user (.NET 8 images mein APP_UID defined hai); port 8080 non-root ke liye theek hai.
USER $APP_UID
ENTRYPOINT ["dotnet", "Sales.Api.dll"]
```

Adjust paths and project names for the repository layout. Production
pipelines should pin approved base image versions or digests and rebuild
regularly for security updates.

## 3.2 Best practices

-   Use multi-stage builds.
-   Use `.dockerignore` to exclude `.git`, `bin`, `obj`, local secrets
    and irrelevant files.
-   Do not bake credentials into image layers or build arguments.
-   Run as non-root where the base image/platform supports it.
-   Prefer minimal runtime images while ensuring required native
    dependencies are present.
-   Pin and regularly update base images.
-   Scan images and dependencies.
-   Use immutable image tags/digests.
-   Configure graceful shutdown and cancellation.
-   Keep logs on stdout/stderr for container log collection.
-   Keep application state outside the container; do not rely on local
    filesystem persistence.
-   Set CPU/memory requests and limits appropriately for the chosen
    runtime.
-   Use explicit health checks at the orchestrator/platform layer.

## 3.3 `.dockerignore` example

``` text
.git
.github
.vs
.vscode
**/bin/
**/obj/
**/TestResults/
.env
appsettings.Development.json
secrets/
```

Do not exclude files required for the build. Secret/config exclusions
should match your actual repo practices.

## 3.4 Configuration

Environment-specific values should be provided through approved
configuration mechanisms, not hard-coded into image.

In ASP.NET Core, environment variables can override configuration
values. Nested keys commonly use double underscores,
e.g. `ConnectionStrings__SalesDb`.

Secrets should come from an approved secret store or platform
integration. Do not commit production credentials or copy them into
Docker images.

------------------------------------------------------------------------

# 4. Container lifecycle and graceful shutdown

When ECS/Kubernetes stops a task/pod, the process may receive a
termination signal and have a limited grace period before forceful
termination.

The service should:

-   Stop accepting new work when appropriate.
-   Finish or safely cancel in-flight requests.
-   Stop queue polling and finish/abandon messages according to
    visibility/lease semantics.
-   Flush telemetry/logging where supported.
-   Avoid acknowledging a message before durable processing succeeds.
-   Respect cancellation tokens.
-   Use idempotency so interrupted work can be retried safely.

For ASP.NET Core, host shutdown tokens and request cancellation should
be propagated to I/O operations. A `CancellationToken` is not a
guarantee that every database/network operation can be cancelled
instantly; provider behaviour and timeout configuration matter.

------------------------------------------------------------------------

# 5. Rolling vs blue/green vs canary deployment

## 5.1 Rolling deployment

Instances are replaced in batches.

**Advantages** - Resource-efficient. - Often simple to operate. - No
full duplicate environment required.

**Risks** - Old and new versions coexist. - Schema/API/event contracts
must be compatible. - A bad version can affect some traffic before
detection. - Rollback may require another rollout and compatible
data/schema.

## 5.2 Blue/green deployment

Two environments/target groups are maintained:

-   Blue = current version.
-   Green = candidate version.

Test Green, then shift routing when gates pass. Keep Blue available
during the agreed rollback window.

**Advantages:** Fast traffic switch and clear environment separation.\
**Risks:** Extra capacity/cost; data and background workers may be
shared; switching traffic back does not reverse writes already made by
Green.

## 5.3 Canary deployment

Small traffic cohort first, then increase gradually.

Example rollout: - 1--5% canary - 10--25% - 50% - 100%

Percentages are illustrative; select cohort size and observation period
based on traffic volume, risk and SLOs.

``` mermaid
flowchart TD
    A[Build candidate release] --> B[Deploy candidate]
    B --> C[Smoke and compatibility tests]
    C --> D[Route small canary cohort]
    D --> E[Compare error rate, latency and business KPIs]
    E --> F{Gates pass?}
    F -- No --> G[Stop rollout and assess rollback]
    F -- Yes --> H[Increase traffic gradually]
    H --> I{Full rollout healthy?}
    I -- No --> G
    I -- Yes --> J[Complete deployment]
```

## 5.4 Strategy comparison

| Strategy | Resource cost | Detection speed | Rollback characteristics | Good fit |
|---|---|---|---|---|
| Rolling | Usually lower | Depends on batch/gates | Rollout/redeploy; mixed versions | Routine compatible releases |
| Blue/green | Higher during overlap | Can validate full candidate environment | Traffic switch may be quick; data rollback still complex | High-risk releases needing parallel environment |
| Canary | Variable | Good if metrics and traffic are sufficient | Halt/shift traffic back if data compatibility allows | Gradual exposure and SLO-based gates |

No strategy guarantees zero downtime automatically. Health checks, load
balancing, compatible contracts, capacity, connection draining and
database changes all matter.

------------------------------------------------------------------------

# 6. AWS ECS/Fargate vs Kubernetes

## 6.1 ECS/Fargate

-   AWS-native container orchestration.
-   Fargate manages much of the underlying compute infrastructure.
-   Integrates with IAM, CloudWatch, load balancers, service discovery
    and deployment controls.
-   Often lower operational overhead for teams committed to AWS.

## 6.2 Kubernetes

-   Broad ecosystem and portability across supported environments.
-   Rich primitives for scheduling, rollout, service discovery,
    configuration and extensibility.
-   Requires cluster/platform expertise unless using a managed offering.
-   Adds operational complexity: upgrades, policies, networking,
    observability and add-ons still need ownership.

## 6.3 Decision matrix

  -----------------------------------------------------------------------
  Consideration           ECS/Fargate             Kubernetes
  ----------------------- ----------------------- -----------------------
  AWS integration         Strong native           Available, but often
                          integration             requires platform
                                                  integration

  Cluster management      Fargate reduces         Managed control plane
                          infrastructure          helps, but
                          management              workload/platform
                                                  operations remain

  Portability             More AWS-oriented       Broader
                                                  ecosystem/portability

  Learning/operations     Often simpler for       More concepts and
                          AWS-focused teams       platform
                                                  responsibilities

  Ecosystem extensibility AWS service model       Extensive Kubernetes
                                                  ecosystem

  Best choice             Team wants AWS-native   Existing Kubernetes
                          operations with less    platform/skills or
                          cluster management      specific ecosystem
                                                  requirements
  -----------------------------------------------------------------------

**Interview answer:** "I would not choose Kubernetes just because the
system has microservices. I'd evaluate operational capability,
portability needs, deployment requirements, policy/networking
complexity, AWS integration and total cost. For an AWS-centric team
without Kubernetes platform expertise, ECS/Fargate can be a pragmatic
choice."

------------------------------------------------------------------------

# 7. Configuration, secrets and feature flags

## 7.1 Configuration principles

-   Separate deployable artifact from environment configuration.
-   Validate required configuration at startup.
-   Use typed options and validation for important settings.
-   Avoid secrets in source control, image layers, logs or plain-text
    deployment files.
-   Rotate credentials without rebuilding the application where the
    platform supports it.
-   Give each service least-privilege permissions.
-   Audit access to secrets and production configuration.

## 7.2 Feature flags

Feature flags separate deployment from feature activation.

Use cases: - Enable a new read path for a small dealer cohort. - Switch
from legacy inventory lookup to new service. - Disable an optional
non-critical feature during an incident. - Gradually roll out a
workflow.

Flags need ownership, expiry/review dates, auditability and safe
defaults. Old flags should be removed after rollout. A feature flag is
not a substitute for authorization or a security boundary.

## 7.3 Migration-specific feature flags

Example:

``` csharp
if (featureFlags.UseNewInventoryRead)
{
    return await newInventoryClient.GetVehicleAsync(vehicleId, cancellationToken);
}

return await legacyInventoryRepository.GetVehicleAsync(vehicleId, cancellationToken);
```

This is illustrative; actual flag provider and fallback rules depend on
the app. Ensure the two paths have compatible semantics, and do not
silently fall back to stale data for a business-critical operation
without a defined policy.

------------------------------------------------------------------------

# 8. Database changes during deployment

A rolling release can run old and new application versions
simultaneously. Therefore, database changes must preserve compatibility.

Safer order:

1.  Add new nullable column/table/index or other compatible structure.
2.  Deploy code that can tolerate both old and new structure.
3.  Backfill data separately.
4.  Switch reads/writes after validation.
5.  Observe and verify old version is retired.
6.  Remove obsolete structure in a later release.

Risky pattern:

1.  Rename/drop a column immediately.
2.  Deploy code expecting only new schema.
3.  Old instances continue to run and fail.

For large tables, inspect locks, transaction log growth, index
operations, deployment timeout and migration duration. Do not
automatically run EF Core migrations concurrently from every replica at
application startup.

------------------------------------------------------------------------

# 9. Deployment safety gates

A release should have measurable go/no-go criteria.

Potential gates:

-   Unit/integration/contract tests pass.
-   Image vulnerability policy passes or approved exceptions exist.
-   Service becomes ready.
-   Smoke tests pass.
-   Error rate remains within agreed threshold.
-   p95/p99 latency remains within target.
-   Business completion rate is healthy.
-   Queue lag and DLQ do not regress.
-   Database error/lock metrics remain healthy.
-   No significant increase in duplicate operations or data mismatches.

Use a suitable observation window. Very low traffic can make percentage
metrics noisy, so combine rate, counts, absolute error budgets and
business signals.

**Automatic rollback is not always safe.** If the candidate release has
made irreversible schema changes or business side effects, a blind
rollback may worsen data inconsistency. In those cases, halt rollout,
preserve evidence and follow the rehearsed recovery strategy.

------------------------------------------------------------------------

# 10. Infrastructure as Code and release governance

Terraform/OpenTofu, AWS CDK ya Pulumi jaise IaC tool infrastructure
changes ko application delivery ke saath version kar sakte hain.
Note: CDK for Terraform (CDKTF) ko HashiCorp ne Dec 2025 mein deprecate/
archive kar diya; naye projects mein use na karein. Existing CDKTF code
ko `cdktf synth --hcl` se plain Terraform HCL mein migrate kiya ja sakta
hai, ya OpenTofu/Pulumi/AWS CDK ki taraf move karein.

Good practices:

-   Review plans/diffs before apply.
-   Separate environments and permissions.
-   Use remote state with locking and restricted access where
    applicable.
-   Avoid secrets in state or outputs; state itself may contain
    sensitive values.
-   Apply least privilege to CI identities.
-   Pin provider/module versions under a controlled update process.
-   Detect drift and define whether/how it is reconciled.
-   Use separate approvals for high-risk production infrastructure
    changes.
-   Keep infrastructure and application rollout dependencies explicit.

CI/CD identity should receive only required deployment permissions.
Avoid long-lived static AWS access keys when an appropriate short-lived
role/OIDC integration is available.

------------------------------------------------------------------------

# 11. Deployment architecture for a .NET microservice on AWS

``` mermaid
flowchart TD
    DEV[Developer / Pull Request] --> CI[CI: Build, tests, scans]
    CI --> IMG[Immutable container image]
    IMG --> REG[Container registry]
    REG --> CD[Deployment workflow]
    CD --> ECS[ECS service / Fargate tasks]
    ECS --> ALB[Application Load Balancer]
    ALB --> U[Users / API Gateway]
    ECS --> SM[Secrets/config integration]
    ECS --> DB[(Service-owned database)]
    ECS --> Q[SQS / event broker]
    ECS --> TEL[Logs, metrics, traces]
    TEL --> CW[CloudWatch / trace backend]
    CW --> G[Release gates and alerts]
    G -->|Healthy| DONE[Complete rollout]
    G -->|Unhealthy| STOP[Halt / rollback or roll forward]
```

Architecture details depend on whether API Gateway is placed before ALB,
how the service is networked, and whether workloads use ECS or
Kubernetes. The diagram shows conceptual relationships, not a mandatory
AWS topology.

------------------------------------------------------------------------

# 12. Interview Questions and Answers

## Q1. Microservice independently deployable kab hai?

Jab usko release karne ke liye unrelated services ka synchronized
release required na ho. API/event contracts, schema compatibility,
configuration, tests and deployment pipeline independent release enable
karein.

## Q2. Rolling, blue/green and canary mein difference?

Rolling instances ko batches mein replace karta hai. Blue/green parallel
environments maintain karke routing switch karta hai. Canary candidate
ko small traffic cohort par expose karke metrics validate karta hai
before expanding.

## Q3. Docker multi-stage build kyun?

SDK/build tools ko build stage tak limit kar sakte hain; final image
mein published application and runtime hota hai. Isse image surface/size
reduce ho sakti hai and build/runtime concerns separate hote hain.

## Q4. Container mein application state local disk par kyun avoid karte hain?

Container replacement or rescheduling par local state lost ho sakti hai.
Durable business state database/object store mein aur transient shared
work approved distributed storage/queue mein rakhein.

## Q5. ECS/Fargate ya Kubernetes choose kaise karoge?

Operational maturity, portability, AWS integration, platform skills,
policy/networking needs, deployment model, ecosystem and total cost
evaluate karunga. Microservices hone se Kubernetes automatically
necessary nahi hota.

## Q6. Same image all environments mein promote kyun?

Artifact reproducibility improve hoti hai and environment-to-environment
build differences avoid hote hain. Configuration environment-specific
hoti hai, artifact immutable rehta hai.

## Q7. Deployment ke baad p95 latency badh gayi. Kya karoge?

Release marker ke against metrics compare, new vs old version
traces/logs inspect, business outcomes and dependency metrics validate.
Rollout halt/rollback only if safe; otherwise rehearsed roll-forward/fix
plan use karunga.

## Q8. Feature flag aur deployment mein difference?

Deployment code ko environment mein release karta hai; feature flag
capability ko activate/deactivate kar sakta hai without new binary
deployment. Flags need ownership and cleanup; authorization ke
replacement nahi hain.

## Q9. Database migration rolling deployment ko kaise affect karti hai?

Old/new versions simultaneously run kar sakte hain. Expand-and-contract,
backward-compatible schema, separate backfill and delayed destructive
cleanup use karunga.

## Q10. Graceful shutdown kyun important hai?

In-flight requests and queue messages safely complete/cancel ho sakein,
telemetry flush ho aur duplicate/lost work reduce ho. Shutdown deadline,
cancellation and idempotency design ka part hain.

## Q11. Automatic rollback hamesha safe hai?

Nahi. Data/schema changes and external side effects rollback ko unsafe
bana sakte hain. Release gates rollout halt kar sakte hain, but recovery
decision rehearsed rollback/roll-forward plan se hona chahiye.

## Q12. Pipeline mein kaunse tests include karoge?

Unit tests, integration tests, API/event contract tests, migration
compatibility tests, security/dependency/image scans, smoke tests and
post-deployment health/business checks. Test selection risk and release
time ke hisaab se layer ki jaati hai.

## Q13. Production secret CI pipeline mein kaise handle karoge?

Prefer short-lived federated identity/role, least privilege, secret
manager integration and masked logs. Static credentials or secrets baked
into image avoid karunga.

## Q14. Canary rollout mein 1% traffic enough hai?

Always nahi. Enough traffic and observation time chahiye to meaningful
errors/latency signal mil sake. Critical low-frequency business
operations ke liye synthetic checks and business metrics bhi use
karunga.

## Q15. IaC deployment mein drift detect hua. Kya karoge?

Current state and desired configuration compare, drift ka cause
identify, plan review and approved reconciliation. Production resources
ko blindly overwrite ya manual changes ignore nahi karunga.

------------------------------------------------------------------------

# 13. Scenario-based practice

## Scenario A: New release passes health check but order completion falls

Health endpoint only process/readiness verify kar sakta hai. Business
metric, queue consumer lag, event contract, DB errors and end-to-end
synthetic transaction check karein. Rollout gate ko business outcomes
include karna chahiye.

## Scenario B: New version and old version fail after schema change

Likely backward compatibility issue. Stop rollout, inspect migration
ordering and application version compatibility. If safe, restore
compatible schema/code state; otherwise roll forward with additive
schema and compatible code. Production recovery should follow tested
runbook.

## Scenario C: ECS tasks restart repeatedly

Inspect task stopped reason, container exit code, memory/CPU, startup
configuration, secret access, health-check grace period, dependency
startup and application logs. Restart count alone root cause nahi
batata.

## Scenario D: Canary error rate rises, but only for one dealer

Check tenant/dealer configuration, data shape, permissions, routing
cohort, feature flag and dealer-specific downstream dependencies. Ensure
dashboards allow safe segmentation without unbounded metric cardinality
or leaking sensitive data.

------------------------------------------------------------------------

# 14. Practical release checklist

### Before release

-   [ ] Tests and contract compatibility pass.
-   [ ] Immutable image built, scanned and published.
-   [ ] Configuration and secrets validated.
-   [ ] Database changes backward-compatible.
-   [ ] Migration/backfill plan and recovery steps reviewed.
-   [ ] Health checks, dashboards and alerts ready.
-   [ ] Rollout strategy and abort thresholds documented.

### During release

-   [ ] Deploy candidate using approved strategy.
-   [ ] Verify readiness and smoke tests.
-   [ ] Observe API RED metrics and resource saturation.
-   [ ] Observe business completion, queue lag and data errors.
-   [ ] Expand canary only when gates pass.
-   [ ] Halt rollout if impact crosses threshold.

### After release

-   [ ] Confirm all expected tasks/instances use the intended version.
-   [ ] Validate business workflow end to end.
-   [ ] Verify no increase in duplicates, DLQ or data mismatches.
-   [ ] Confirm alerts and telemetry are healthy.
-   [ ] Record deployment outcome and follow-up actions.
-   [ ] Remove obsolete feature flags and temporary permissions when
    safe.

------------------------------------------------------------------------

# 15. 60-second interview summary

"Microservices ka benefit independent deployability se aata hai, isliye
main har service ke liye automated build, tests, security scans,
immutable container artifacts and deployment gates design karta hoon.
.NET 8 apps ke liye multi-stage Docker build, secure configuration,
graceful shutdown and health checks use karta hoon. Release risk ke
basis par rolling, blue/green or canary choose karta hoon, and
API/event/database changes ko backward-compatible rakhta hoon.
AWS-centric platform mein ECS/Fargate often practical option ho sakta
hai; Kubernetes tab choose karunga jab portability, existing platform
capability or ecosystem requirements justify karein. Production rollout
ke baad technical metrics ke saath business outcomes monitor karta hoon.
Automatic rollback ko blindly trigger nahi karta---data changes and
irreversible side effects ke context mein rehearsed rollback or
roll-forward plan follow karta hoon."

------------------------------------------------------------------------

# 16. Final revision checklist

-   [ ] Independent deployability.
-   [ ] CI/CD stages and immutable artifact promotion.
-   [ ] Multi-stage Docker build and image security.
-   [ ] Graceful shutdown and stateless containers.
-   [ ] Rolling vs blue/green vs canary.
-   [ ] ECS/Fargate vs Kubernetes trade-offs.
-   [ ] Config, secrets and feature flags.
-   [ ] Expand-and-contract during rolling releases.
-   [ ] Deployment gates and business health signals.
-   [ ] IaC reviews, state protection and least privilege.
-   [ ] Rollback vs roll-forward.
-   [ ] A real release incident with investigation and mitigation.
