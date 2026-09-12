# GitHub Actions — Quick Revision Notes

> Quick-revision notes derived from the GitHub Actions Interview Guide. Covers every section of the guide, in the same order, in condensed Q&A / bullet form for fast brush-up.

---

## Core Concepts

GitHub Actions = GitHub's native CI/CD + automation platform, driven by YAML in `.github/workflows/`.

**Building blocks:**

| Concept | Description |
|---|---|
| **Workflow** | A YAML file defining an automated process, triggered by events. |
| **Job** | Group of steps on a single runner. Parallel by default; sequence via `needs`. |
| **Step** | One task in a job — a shell command (`run`) or a reusable action (`uses`). |
| **Action** | Reusable automation unit (JS, Docker, or composite). First-party/community/custom. |
| **Runner** | The compute (VM/container) executing a job. GitHub-hosted or self-hosted. |

**Q: Job vs step — the core distinction?**
A: A **job** is the unit of isolation (own VM/container, own filesystem). A **step** is the unit of sequencing within that isolation. Common trip-up for Jenkins/Azure DevOps folks (stage-vs-task models).

**Workflow lifecycle:** Trigger event → workflow selected → jobs (parallel, or sequenced via `needs` forming a DAG) → each job runs steps (checkout, then run/uses) → artifacts / deployment / status check.

```yaml
name: CI Workflow
on: [push, pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - run: npm test
```

**Version note:** Pin to `@v4` for checkout / upload-artifact / download-artifact. `@v3` of upload/download-artifact is **deprecated and disabled** on GitHub-hosted runners. Treat any `@v3` in old notes as stale.

---

## Workflow Triggers

| Trigger | Use case |
|---|---|
| `push` | Commits to matching branches |
| `pull_request` | PR open/sync/reopen — runs against the **merge commit**, not head |
| `schedule` (cron) | Nightly / scheduled |
| `workflow_dispatch` | Manual trigger (UI/API) with optional typed inputs |
| `workflow_call` | Makes workflow reusable/callable |
| `repository_dispatch` | External system triggers via API |
| `issues`, `release`, etc. | Repo/object lifecycle events |

```yaml
on:
  push:
    branches: [main, develop]
    paths-ignore: ['**.md', 'docs/**']
  pull_request:
  workflow_dispatch:
  schedule:
    - cron: '0 0 * * *'   # daily midnight UTC
```

**Manual dispatch with typed inputs:**
```yaml
on:
  workflow_dispatch:
    inputs:
      environment:
        description: 'Target environment'
        required: true
        default: 'staging'
        type: choice
        options: [staging, production]
```
Trigger from Actions tab, `gh workflow run`, or REST API.

**Cross-repo triggering:** Use `repository_dispatch` or `gh workflow run workflow.yml --repo other/repo`. Needs a PAT or GitHub App token — the default `GITHUB_TOKEN` **cannot** trigger workflows in other repos, and by design cannot trigger another workflow run in the *same* repo (prevents infinite recursion).

**Gotcha — fork PRs:** `pull_request` from a fork runs with a **read-only, restricted `GITHUB_TOKEN`** and **no repo secrets** (security boundary). `pull_request_target` runs in the base repo context (full token/secrets) but checks out the *base* ref by default — a classic **pwn request** vector if misused.

---

## Runners

A runner is the VM/container that executes a job.

**GitHub-Hosted vs Self-Hosted:**

| Aspect | GitHub-Hosted | Self-Hosted |
|---|---|---|
| Maintenance | Zero — provisioned/torn down per job | You patch OS, install SDKs, manage capacity |
| Cost | Per-minute (Linux 1x, Windows 2x, macOS 10x) | Infra cost only; free from Actions minutes billing |
| Networking | Public internet, ephemeral IP, no VPN by default | Inside your VPC/on-prem; access private feeds/resources |
| State | Fully ephemeral — clean VM per run | Persistent by default (danger: leftover caches/secrets/malware unless cleaned or ephemeral) |
| Security | Isolated per-job VM, GitHub-patched | You own posture; malicious PR code could hit internal network — bigger blast radius |
| Hardware | Fixed SKUs (some GPU/large at cost) | Anything you provision (bare metal, GPU, ARM, custom images) |
| Best for | Most OSS/standard CI | Compliance/data-residency, large monorepos, licensed software, private network, cost at scale |

**Current hosted images:** Ubuntu (`ubuntu-latest/24.04/22.04`), Windows (`windows-latest/2025/2022`), macOS (`macos-latest/15/14`). Old notes citing `ubuntu-20.04`/`windows-2019`/`macos-12` are retired — verify against `actions/runner-images` since `-latest` rotates.

**Self-hosted setup:** `./config.sh --url <repo-url> --token <runner-token>` then `./run.sh`.

**Q: How do you scale self-hosted runners?**
A: **Actions Runner Controller (ARC)** on Kubernetes for autoscaling ephemeral runners, or cloud autoscaling groups. **Never** run long-lived self-hosted runners on a *public* repo — anyone opening a PR could execute arbitrary code on your infra.

---

## Variables, Secrets & Environments

**Env vars:**
```yaml
env:
  NODE_ENV: production
steps:
  - run: echo "Running in $NODE_ENV mode"
```

**Secrets:** Stored in Settings → Secrets and variables → Actions, scoped at repo/environment/org level. `${{ secrets.MY_SECRET_KEY }}`.

| Feature | `env` variables | `secrets` |
|---|---|---|
| Visibility | Visible in logs | Auto-masked in logs |
| Encryption | Not encrypted | Encrypted at rest, decrypted only at runtime |
| Scope | Public to repo/workflow | Access-controlled (repo/env/org) |
| Use | Build flags, feature toggles | Keys, connection strings, tokens |

**Gotcha:** Secret masking is naive substring matching on the printed value. It does **not** protect against base64-encoding, line-splitting, or transforming a secret before printing. Assume anything touching a secret is exfiltratable in a compromised workflow.

**Environments & approval gates:**
```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: production
    steps:
      - run: ./deploy.sh
```
Environments let you: scope secrets to that environment only; require **manual approval** from reviewers; restrict which branches/tags can deploy (**deployment branch policies**); add a wait timer. Direct equivalent of Azure DevOps environments + approvals/checks — the standard senior answer to "how do you gate a prod deploy?"

---

## Jobs, Dependencies & Control Flow

**`needs` — job dependencies:**
```yaml
jobs:
  build: { runs-on: ubuntu-latest }
  test:  { runs-on: ubuntu-latest, needs: build }
  deploy: { runs-on: ubuntu-latest, needs: [build, test] }
```
No `needs` → parallel. `needs` creates a DAG; downstream jobs consume upstream **outputs** (`needs.build.outputs.version`).

**`if` conditionals:** Apply at step or job level.
```yaml
- if: github.ref == 'refs/heads/main'
  run: echo "on main"
```
Common: `github.event_name == 'pull_request'`, `success()`, `failure()`, `always()`, `cancelled()`.

**Gotcha:** If a prior step fails, later steps are **skipped by default** unless `if: always()` or `if: failure()`. Frequent cause of "why didn't my cleanup/notification run?"

**Build/Deploy Notifications (Slack & Email):** Not a special primitive — ordinary steps gated by the same `if:` logic, driven by a webhook/SMTP secret (not `GITHUB_TOKEN`).
```yaml
- name: Notify Slack on success
  if: success()
  uses: rtCamp/action-slack-notify@v2
  env:
    SLACK_WEBHOOK: ${{ secrets.SLACK_WEBHOOK }}
    SLACK_MESSAGE: 'Build succeeded! :white_check_mark:'
    SLACK_COLOR: good
- name: Notify Slack on failure
  if: failure()
  uses: rtCamp/action-slack-notify@v2
  env:
    SLACK_WEBHOOK: ${{ secrets.SLACK_WEBHOOK }}
    SLACK_MESSAGE: 'Build failed on ${{ github.ref_name }}.'
    SLACK_COLOR: danger
```
- Posts to a Slack **Incoming Webhook URL** stored as a secret — no OAuth app needed for the simple case.
- Split success/failure into separate steps for different messages; use `if: always()` + branch on `${{ job.status }}` for a single step.
- **Email:** same pattern with an SMTP action (`dawidd6/action-send-mail@v3`), creds via secrets, gated the same way.
- **Reverse (Slack → workflow):** a Slack bot/slash-command calls the REST API / `gh workflow run` to fire `workflow_dispatch` (ChatOps). Needs a PAT/App token with `actions: write` — `GITHUB_TOKEN` can't be used (call originates outside a run).

**Concurrency control:**
```yaml
concurrency:
  group: ci-${{ github.ref }}
  cancel-in-progress: true
```
Prevents redundant/overlapping runs. **Without `cancel-in-progress: true`, `concurrency` only queues serially** — it does not cancel stale runs.

**Timeouts & retries:**
```yaml
jobs:
  build:
    timeout-minutes: 10
```
**No built-in step-level retry.** Options: shell retry loop (`for i in 1 2 3; do cmd && break || sleep 5; done`) or `nick-fields/retry@v3` (max attempts/backoff/timeout).

**Reuse within a repo:** For true cross-job/workflow reuse, prefer a **composite action** over copy-pasting steps or YAML anchors (anchors aren't officially supported by the Actions schema — fragile hack).

---

## Matrix Builds

```yaml
strategy:
  matrix:
    os: [ubuntu-latest, windows-latest]
    dotnet-version: ['8.0.x', '9.0.x']
  fail-fast: false
runs-on: ${{ matrix.os }}
steps:
  - uses: actions/setup-dotnet@v4
    with:
      dotnet-version: ${{ matrix.dotnet-version }}
```
- Runs the **cross-product** as parallel jobs (2 OS × 2 versions = 4).
- `fail-fast: true` (default) cancels all matrix jobs on first failure. Usually want `false` in CI for full signal; `true` only to save minutes.
- `max-parallel` throttles concurrent matrix jobs (protects limited self-hosted pools).
- `include`/`exclude` add or drop specific combinations.

**Q: Why matrix instead of N separate jobs?**
A: DRY definition, and Checks UI groups results legibly under one job name with per-combination status.

---

## Caching & Artifacts

**Caching (speed, not persistence):**
```yaml
- uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: nuget-${{ runner.os }}-${{ hashFiles('**/packages.lock.json') }}
    restore-keys: |
      nuget-${{ runner.os }}-
```
- Keyed: exact hit restores; `restore-keys` gives fallback **partial** matches.
- Caches are **immutable per key** — can't update an existing entry; a new key is required ("why isn't my cache updating?").
- Scoped per repo/branch with LRU eviction (~10 GB repo soft cap — verify current).
- **Best-effort** — never rely on it for correctness, only speed. Pipeline must work on a full cache miss.

**Artifacts (durable, cross-job transfer):**
```yaml
- uses: actions/upload-artifact@v4
  with:
    name: build-output
    path: ./bin/Release
    retention-days: 7
- uses: actions/download-artifact@v4
  with:
    name: build-output
```
**v4 breaking change (gotcha):** No multiple uploads to the *same* artifact name (each upload = new immutable artifact); up to 10x faster than v3; needs distinct names per matrix leg → `name: artifact-${{ matrix.os }}-${{ matrix.dotnet-version }}`. v3 is deprecated/EOL.

| | Cache | Artifact |
|---|---|---|
| Purpose | Speed repeated dependency restore | Pass build output between jobs / preserve for humans |
| Lifetime | Best-effort, evicted opportunistically | Guaranteed for `retention-days`, downloadable |
| Content | `node_modules`, NuGet/npm cache, Docker layers | Compiled binaries, test results, logs, coverage |

**Long-term storage:** Artifact retention capped (default 90 days). For genuinely long-term, push to a package feed (GitHub Packages, NuGet.org, Azure Artifacts) or blob storage (S3/Azure Blob).

---

## Reusable Workflows & Composite Actions

Commonly confused — know the distinction cold.

- **Reusable Workflow (`workflow_call`):** own YAML file, own jobs, defines its own `runs-on`, invoked with `uses: org/repo/.github/workflows/x.yml@ref`, has its own secrets/permissions context.
- **Composite Action (`action.yml`):** runs inside the CALLING job, just a sequence of steps (no own `runs-on`), invoked with `uses: ./path` or `org/repo@ref`, inherits the caller's context.

| | Reusable Workflow | Composite Action |
|---|---|---|
| File | `.yml` in `.github/workflows/` with `on: workflow_call` | `action.yml` (+ optional scripts) |
| Granularity | One or more **jobs** | Sequence of **steps** in one job |
| Runner | Own `runs-on` | Caller's runner |
| Secrets | Explicitly passed via `secrets:` (or `secrets: inherit`) | Inherits caller env/context |
| Nesting | Can call other reusable workflows (up to 4 levels) | Can call other actions |
| Best for | Standardizing a whole pipeline stage across many repos | Standardizing a few steps (checkout+setup+restore) |

**Reusable workflow:**
```yaml
# .github/workflows/deploy.yml
on:
  workflow_call:
    inputs:
      environment: { required: true, type: string }
    secrets:
      AWS_ACCESS_KEY_ID: { required: true }
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - run: echo "Deploying to ${{ inputs.environment }}"
```
Calling it (incl. cross-repo):
```yaml
jobs:
  call-workflow:
    uses: my-org/my-repo/.github/workflows/deploy.yml@main
    with:
      environment: production
    secrets:
      AWS_ACCESS_KEY_ID: ${{ secrets.AWS_ACCESS_KEY_ID }}
      # or: secrets: inherit
```

**Composite action:**
```yaml
# action.yml
name: 'Setup and Restore'
runs:
  using: "composite"
  steps:
    - uses: actions/setup-dotnet@v4
      with: { dotnet-version: '8.0.x' }
      shell: bash
    - run: dotnet restore
      shell: bash
```
**Gotcha:** every `run` step in a composite action **must specify `shell:`** — not inherited like in a normal job.

**Q: When pick which?**
A: Composite action for a small reusable step-sequence inside a larger job (shared setup). Reusable workflow to standardize an entire deploy/release process across many repos with central governance (security team owns it, teams just call it — single audit/patch choke point).

---

## Containers & Custom Actions

**Job inside a container:**
```yaml
jobs:
  docker-job:
    runs-on: ubuntu-latest
    container:
      image: mcr.microsoft.com/dotnet/sdk:8.0
    steps:
      - run: dotnet --version
```
Pins exact toolchain independent of runner image; hermetic build env matching prod.

**Custom actions — three flavors:**

| Type | Runtime | Use case |
|---|---|---|
| **Docker container** | Any language, containerized | Full control, heavier/slower startup, mostly Linux runners only |
| **JavaScript/TS** | Node.js (`node20`) | Fastest startup, cross-platform, most Marketplace actions |
| **Composite** | Orchestrates actions/shell steps | No compilation, easiest to author |

```yaml
# action.yml (JS action skeleton)
name: 'My Custom Action'
inputs:
  who-to-greet: { required: true }
runs:
  using: 'node20'
  main: 'dist/index.js'
```

---

## Deployment Patterns

**Multi-stage (dev → staging → prod):**
```yaml
jobs:
  deploy-dev:
    environment: dev
    runs-on: ubuntu-latest
    steps: [ ... ]
  deploy-staging:
    needs: deploy-dev
    environment: staging
    runs-on: ubuntu-latest
    steps: [ ... ]
  deploy-prod:
    needs: deploy-staging
    environment: production   # gated by required reviewers
    runs-on: ubuntu-latest
    steps: [ ... ]
```

**Cloud (AWS) — legacy static-key pattern:**
```yaml
- uses: aws-actions/configure-aws-credentials@v4
  with:
    aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
    aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
    aws-region: us-east-1
- run: aws s3 sync ./build s3://my-bucket
```
**Senior flag:** long-lived AWS keys as secrets is the *legacy* pattern. Current best practice = **OIDC federation** (no static creds). Bring this up proactively.

**IaC:** `terraform apply -auto-approve`. Same OIDC principle for Azure (`azure/login@v2`) and GCP (`google-github-actions/auth`).

**Status badge:**
```markdown
![CI](https://github.com/your-org/your-repo/actions/workflows/ci.yml/badge.svg)
```

---

## Debugging & Local Testing

**Debugging a failed run:**
- Re-run with **debug logging**: set repo secrets `ACTIONS_STEP_DEBUG=true` and `ACTIONS_RUNNER_DEBUG=true`, or use "Re-run with debug logging" in the UI.
- **`tmate`** (`mxschmitt/action-tmate`) to SSH into a live runner mid-failure — great for flaky/self-hosted issues.
- Dump `${{ toJSON(github) }}` / `${{ toJSON(steps) }}` in a debug step to inspect context values.
- **Note:** old notes cite `actions/setup-debugging` — no such official action exists (likely misremembered). Use the debug-logging secrets / `tmate` approach.

**Running Actions locally:**
```bash
act -j build
```
`nektos/act` runs workflows locally in Docker for fast iteration. Limits: imperfect runner-image emulation, `secrets`/some `github.*` need stubbing, environments/approvals not simulated.

---

## .NET-Specific CI/CD

**Idiomatic .NET build/test/publish pipeline:**
```yaml
name: .NET CI
on:
  push: { branches: [main] }
  pull_request:
jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0   # for GitVersion / Nerdbank.GitVersioning
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - name: Cache NuGet
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ hashFiles('**/packages.lock.json') }}
          restore-keys: nuget-${{ runner.os }}-
      - run: dotnet restore --locked-mode
      - run: dotnet build --configuration Release --no-restore
      - name: Test
        run: >
          dotnet test --configuration Release --no-build
          --logger "trx;LogFileName=test-results.trx"
          --collect:"XPlat Code Coverage"
      - name: Publish test results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: Test Results
          path: '**/test-results.trx'
          reporter: dotnet-trx
      - run: dotnet publish ./src/MyApp/MyApp.csproj -c Release -o ./publish
      - uses: actions/upload-artifact@v4
        with: { name: webapp, path: ./publish }
```
Key points:
- **`packages.lock.json` + `--locked-mode`**: deterministic restore + exact reproducible cache-key hash. Without a lock file, `hashFiles('**/*.csproj')` is a weaker proxy that misses transitive drift.
- **`fetch-depth: 0`**: default shallow clone breaks tools needing full history (GitVersion, nbgv, `git describe`).
- **`--no-restore` / `--no-build`**: avoid redundant work across restore→build→test.
- **Test results**: `.trx` isn't natively rendered — use `dorny/test-reporter`, `EnricoMi/publish-unit-test-result-action`, or upload as artifact.
- **Coverage**: `--collect:"XPlat Code Coverage"` emits Cobertura XML; combine with `danielpalme/ReportGenerator-GitHub-Action` or Codecov/SonarCloud for PR coverage-diff gating.

**Versioning & semantic release:**
- **GitVersion** / **Nerdbank.GitVersioning (nbgv)**: derive SemVer from git history/tags; run early, expose as job output for later `dotnet build -p:Version=...` and container tags.
- **MinVer**: lighter, purely tag-based.
- Version container images and NuGet packages from the *same* computed value for traceability.

**Containerized .NET — build & push:**
```yaml
- uses: docker/setup-buildx-action@v3
- uses: docker/login-action@v3
  with:
    registry: ghcr.io
    username: ${{ github.actor }}
    password: ${{ secrets.GITHUB_TOKEN }}
- uses: docker/build-push-action@v6
  with:
    context: .
    push: true
    tags: ghcr.io/my-org/my-app:${{ github.sha }}
    cache-from: type=gha
    cache-to: type=gha,mode=max
```
`type=gha` uses the Actions cache backend for Docker layer caching — much faster than rebuilding every layer, and the modern replacement for manually caching `/var/lib/docker`.

---

## Security & Supply-Chain Hardening

**Pwn requests & `pull_request_target` (most-tested senior security topic):**
- `pull_request` from fork: **read-only token, no secrets** — safe even with malicious code (can't exfiltrate anything).
- `pull_request_target`: **base repo's token + full secrets**, but checks out the **base** branch by default — safe *unless* the workflow explicitly checks out/executes the PR head (`github.event.pull_request.head.sha`) or runs attacker-controlled code (e.g. `npm install` firing a malicious `postinstall`, or a checked-out Makefile/script). Privileged token + untrusted code = **"pwn request"**.
- **Mitigation:** never combine `pull_request_target` (or `workflow_run`) with checkout of untrusted head content. Split into two workflows — an unprivileged `pull_request` build/test job, and a separate gated/`workflow_run`-triggered job that reuses only vetted **artifacts** (not source) for privileged work.

**Pinning actions to commit SHAs:**
- `uses: actions/checkout@v4` = mutable tag — if moved (compromised publisher/force-push), your workflow silently runs different code (real attacks, e.g. 2024 `tj-actions/changed-files`).
- Best practice: pin to **full commit SHA** with a version comment:
  ```yaml
  - uses: actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683 # v4.2.2
  ```
- Dependabot / `pinact` auto-update SHA pins while keeping the pin.

**`GITHUB_TOKEN` least privilege:**
- Old default was broad `write`; new repos default read-only — but never rely on defaults, declare explicitly:
  ```yaml
  permissions:
    contents: read
    pull-requests: write   # only if the job comments/labels PRs
  ```
- Highest-leverage, lowest-effort security control — set least-privilege `permissions:` on every workflow.

**Third-party action vetting:**
- Prefer GitHub / verified creators / trusted vendors (`aws-actions/*`, `docker/*`, `azure/*`).
- Otherwise: review source, pin to SHA, consider mirroring into an internal org action if business-critical.
- Wire in **CodeQL** / **Dependabot** / **Scorecard** (`ossf/scorecard-action`) for continuous supply-chain assessment.

**Environment protection rules as a security control:** Environments also scope *which secrets exist at all* in a job context — a compromised PR workflow targeting `dev` simply cannot read `production` secrets (they're never injected). Stronger boundary than `if` conditions alone.

---

## OIDC Federation for Cloud Auth

**Why OIDC replaces long-lived cloud secrets:**
Old pattern stores static `AWS_ACCESS_KEY_ID`/`AWS_SECRET_ACCESS_KEY` (or Azure SP secret). Problems: leak risk (valid until manually revoked), no natural expiry/scoping, manual rotation burden.

**OIDC:** GitHub Actions requests a short-lived, signed identity token from GitHub's OIDC provider; the cloud provider trusts it (via pre-configured trust relationship) to mint temporary, scoped credentials for just that job run. No static secret stored anywhere.

**Flow:** Workflow requests OIDC token (claims: repo/branch/workflow) → GitHub returns signed short-lived JWT → workflow presents JWT to cloud IAM → cloud validates issuer + claims against trust policy → returns short-lived temp credentials (minutes) → workflow calls cloud APIs.

**AWS via OIDC (no static keys):**
```yaml
permissions:
  id-token: write   # required for OIDC
  contents: read
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: arn:aws:iam::123456789012:role/github-actions-deploy
          aws-region: us-east-1
      - run: aws s3 sync ./build s3://my-bucket
```
IAM role trust policy restricts *which* repo/branch/environment can assume it via claims like `token.actions.githubusercontent.com:sub == "repo:my-org/my-repo:ref:refs/heads/main"` — a compromised workflow can only impersonate the role from that exact context.

**Azure equivalent:**
```yaml
permissions:
  id-token: write
  contents: read
steps:
  - uses: azure/login@v2
    with:
      client-id: ${{ secrets.AZURE_CLIENT_ID }}
      tenant-id: ${{ secrets.AZURE_TENANT_ID }}
      subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
      # no client-secret — federated credential on the App Registration
```

**Q: What's stored as a secret if there's no client secret?**
A: Only non-sensitive identifiers (client ID, tenant ID, subscription ID) — none grant access without the OIDC trust relationship also matching repo/branch claims.

---

## Monorepo & Multi-Project CI Strategies

**Change-detection-based selective builds:**
```yaml
jobs:
  changes:
    runs-on: ubuntu-latest
    outputs:
      api: ${{ steps.filter.outputs.api }}
      frontend: ${{ steps.filter.outputs.frontend }}
    steps:
      - uses: dorny/paths-filter@v3
        id: filter
        with:
          filters: |
            api:
              - 'src/Api/**'
            frontend:
              - 'src/Frontend/**'
  build-api:
    needs: changes
    if: needs.changes.outputs.api == 'true'
    runs-on: ubuntu-latest
    steps: [ ... ]
  build-frontend:
    needs: changes
    if: needs.changes.outputs.frontend == 'true'
    runs-on: ubuntu-latest
    steps: [ ... ]
```
Avoids running the whole pipeline on every push. GitHub Actions equivalent of Azure DevOps `paths` triggers / Bazel/Nx affected detection. Near-guaranteed question for monorepo roles.

**Nx/Turborepo-aware CI:** Tools like **Nx** (`nx affected`) and **Turborepo** (`turbo run build --filter=...[origin/main]`) compute the dependency graph and expose an "affected" command — more accurate than path filters because they understand *code* dependencies, not just directories. Relevant even in .NET shops with a JS frontend.

**Reusable workflows as governance:** Combine path-filtering with a shared reusable workflow per project type (e.g. one `dotnet-build-test.yml` called by every service) so build/test/security logic is centrally maintained while each service triggers independently.

---

## Performance & Cost Optimization

**Where runner minutes go:**
- **OS multiplier**: Windows 2x, macOS 10x vs Linux. Default to Linux unless you need Windows/macOS (full .NET Framework, iOS/macOS builds). Very concrete "reduce our CI bill" answer.
- **Concurrency + `cancel-in-progress`**: stop burning minutes on superseded commits.
- **Caching** (NuGet/npm/Docker layers): often the single biggest lever.
- **Matrix `fail-fast` / `max-parallel`** tuning: avoid wasting parallel slots.
- **Path filters / change detection** in monorepos: skip unrelated pipelines.
- **Self-hosted runners** for high-volume, predictable workloads — a build-vs-buy calculation, not a default.
- **Larger/faster hosted runners** can be cheaper *in aggregate* if they cut wall-clock on CPU-bound builds — benchmark, don't assume default is cheapest.

**Timeout hygiene:** Always set job-level `timeout-minutes` (don't rely on the 6-hour default). A hung process silently burning minutes for hours is a common cost leak; "aggressive timeouts everywhere" is a good concrete practice.

---

## GitHub Actions vs Azure DevOps Pipelines

| Aspect | GitHub Actions | Azure DevOps Pipelines |
|---|---|---|
| Config | YAML in `.github/workflows/` | YAML (`azure-pipelines.yml`) or classic UI |
| Isolation unit | Job (own VM/container) | Stage → Job → Task (extra nesting) |
| Reusability | Reusable workflows + composite actions | Templates (`extends`, `template`) |
| Marketplace | Huge community ecosystem | Smaller, enterprise/ALM-focused |
| Approvals/Gates | Environments + required reviewers | Environments + approvals & checks (similar) |
| Secrets | Repo/Env/Org secrets | Variable groups, Key Vault-linked (more mature KV) |
| Self-hosted | Self-hosted runners | Self-hosted agents |
| Cloud auth | OIDC federation | Workload identity federation |
| Repo integration | Deep (Checks API, PR status, Issues) | Best with Azure Repos; GitHub less native |
| Boards | GitHub Projects/Issues (lighter) | Azure Boards (heavier PM) |
| Pricing | Per-minute + OS multiplier | Per-parallel-job licensing |
| Migration driver | Consolidating on GitHub | Deeper enterprise ALM / Azure lock-in |

**Interviewer angle:** most concepts map directly (stage→job, agent→runner, variable group→environment/secret, template→reusable workflow). Migration is largely mechanical translation plus re-thinking security (OIDC, `permissions:` blocks) since default security postures differ.

---

## AWS-Native CI/CD Integration Points

Fills in AWS-specific mechanics beyond the general OIDC section.

**`aws-actions/configure-aws-credentials` in depth:**
```yaml
permissions:
  id-token: write   # lets the job request an OIDC token
  contents: read
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: arn:aws:iam::123456789012:role/github-actions-ecs-deploy
          aws-region: us-east-1
          role-session-name: gha-${{ github.run_id }}   # traceable in CloudTrail
      - run: aws sts get-caller-identity   # sanity check
```
Under the hood: action gets a signed JWT from GitHub's OIDC provider (scoped to repo/workflow/ref) → calls AWS STS `AssumeRoleWithWebIdentity` with the JWT → AWS validates signature against the registered OIDC provider, checks trust-policy conditions against claims → returns short-lived creds (default 1h) exported as env vars for subsequent steps.

**IAM OIDC provider + role trust policy setup (Terraform):**

1. Register GitHub's OIDC provider (one-time per account):
```hcl
resource "aws_iam_openid_connect_provider" "github_actions" {
  url             = "https://token.actions.githubusercontent.com"
  client_id_list  = ["sts.amazonaws.com"]
  thumbprint_list = []  # AWS validates via its own CA bundle; thumbprint no longer required (verify current guidance)
}
```

2. IAM role with a trust policy scoped to exact repo/branch/environment (limits blast radius):
```hcl
resource "aws_iam_role" "github_actions_ecs_deploy" {
  name = "github-actions-ecs-deploy"
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Federated = aws_iam_openid_connect_provider.github_actions.arn }
      Action    = "sts:AssumeRoleWithWebIdentity"
      Condition = {
        StringEquals = {
          "token.actions.githubusercontent.com:aud" = "sts.amazonaws.com"
        }
        StringLike = {
          "token.actions.githubusercontent.com:sub" = "repo:my-org/my-repo:ref:refs/heads/main"
          # or scoped to an Environment:
          # "token.actions.githubusercontent.com:sub" = "repo:my-org/my-repo:environment:production"
        }
      }
    }]
  })
}
```

**Q: Blast radius if this workflow is compromised?**
A: Only what the attached IAM permissions policy allows, and only from the exact repo/ref/environment in the `sub` claim. A feature branch can't assume a role trust-scoped to `refs/heads/main` even with a stolen `GITHUB_TOKEN` — OIDC claims are generated server-side by GitHub and can't be forged by workflow code.

**Gotcha:** always check the `aud` claim (`sts.amazonaws.com`) alongside `sub` — checking `sub` alone is a real hardening gap.

**Artifacts/caching → AWS deploy targets:**

Containerized .NET service to **ECS** (image-based):
```yaml
jobs:
  build-and-push:
    runs-on: ubuntu-latest
    permissions:
      id-token: write
      contents: read
    steps:
      - uses: actions/checkout@v4
      - uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: arn:aws:iam::123456789012:role/github-actions-ecs-deploy
          aws-region: us-east-1
      - uses: aws-actions/amazon-ecr-login@v2
        id: ecr-login
      - uses: docker/build-push-action@v6
        with:
          context: .
          push: true
          tags: ${{ steps.ecr-login.outputs.registry }}/order-api:${{ github.sha }}
          cache-from: type=gha
          cache-to: type=gha,mode=max
      - name: Render task definition
        id: task-def
        uses: aws-actions/amazon-ecs-render-task-definition@v1
        with:
          task-definition: ecs-task-def.json
          container-name: order-api
          image: ${{ steps.ecr-login.outputs.registry }}/order-api:${{ github.sha }}
      - name: Deploy to ECS
        uses: aws-actions/amazon-ecs-deploy-task-definition@v2
        with:
          task-definition: ${{ steps.task-def.outputs.task-definition }}
          service: order-api-service
          cluster: order-api-cluster
          wait-for-service-stability: true
```
The same `type=gha` layer cache works identically whether the registry is GHCR or ECR — the cache backend is the Actions cache service regardless of push target.

**.NET Lambda** (artifact/zip-based):
```yaml
      - name: Publish Lambda package
        run: dotnet lambda package -pl ./src/MyFunction -o ./publish/function.zip
      - uses: actions/upload-artifact@v4     # durable handoff / audit / rollback
        with: { name: lambda-package, path: ./publish/function.zip }
      - name: Deploy to Lambda
        run: |
          aws lambda update-function-code \
            --function-name order-processor \
            --zip-file fileb://./publish/function.zip
```
Here the **artifact** (not cache) is correct — the zip is the actual deployable unit. NuGet restore caching still applies upstream to speed `dotnet lambda package`.

**Interviewer angle:** ECS deploys are image-based (build once → push ECR → update task def → ECS handles rolling/blue-green); Lambda deploys are artifact-based (zip/container pushed via Lambda API, usually orchestrated by Terraform/CDKTF). Knowing which primitive (image vs zip artifact) fits which compute target signals real AWS fluency.

---

## Best Practices

- Pin action versions (commit SHA for security-sensitive; at minimum a major tag; never `@main`/`@master`).
- Set explicit least-privilege `permissions:` on every workflow.
- Use `environments` with required reviewers for prod deploy gates.
- Prefer OIDC federation over long-lived cloud credentials.
- Use `concurrency` + `cancel-in-progress` to avoid wasted minutes.
- Cache aggressively (NuGet/npm/Docker) but never depend on cache for correctness.
- Centralize shared CI logic in reusable workflows/composite actions.
- Use path filters / change detection in monorepos.
- Fail fast with sensible `timeout-minutes` everywhere.
- Split untrusted PR-code builds (unprivileged) from privileged deploy logic (trusted, gated) to avoid pwn requests.
- Use Dependabot to keep app dependencies and pinned Action SHAs current.

---

## Common Pitfalls

- Assuming `GITHUB_TOKEN` has full permissions — declare explicitly either way.
- Combining `pull_request_target` with a checkout of PR head — classic pwn request.
- Relying on `actions/cache` for correctness rather than speed.
- Forgetting `shell:` in composite action `run` steps.
- Not setting `fail-fast: false` when you want full matrix signal.
- Believing `concurrency` alone cancels stale runs — needs `cancel-in-progress: true`.
- Using deprecated `checkout@v3`, `upload/download-artifact@v3` — use v4+.
- Uploading multiple matrix legs to one artifact name under v4 (disallowed) — use unique names.
- Treating secret masking as a real security boundary (encoding/splitting bypasses it).
- Forgetting `fetch-depth: 0` when a step needs full git history.
- Long-lived, non-ephemeral self-hosted runners on public repos.

---

## Sample Interview Q&A

**Q: End-to-end when a dev opens a PR for a .NET service (well-designed setup)?**
A: PR opened → `pull_request` fires with a restricted, secret-less token → build/test/lint jobs run in parallel (matrix across TFMs/OS if needed) → NuGet cache restored by lockfile hash → `dotnet build`/`test`, results published via a TRX-aware reporter as PR checks → on success, artifacts (build output, coverage) uploaded → branch protection requires green checks before merge → merge to main triggers a separate `push` workflow running the trusted deploy path (often a reusable workflow), gated by an `environment` requiring manual approval for prod, using OIDC to assume a cloud deploy role instead of static secrets.

**Q: CI bill too high — reduce it without hurting velocity?**
A: Instrument first (Actions usage reports). Then: move Windows/macOS runners to Linux where feasible (biggest multiplier lever); add `concurrency`+`cancel-in-progress`; add/verify NuGet + Docker layer caching; add path-based change detection in monorepos; tighten `timeout-minutes`; evaluate self-hosted for highest-volume predictable workloads if infra/ops cost < aggregate hosted-minute cost; consider larger runner SKUs only where benchmarking shows net improvement.

**Q: Securely deploy to AWS/Azure without storing cloud credentials?**
A: OIDC federation — trust relationship between GitHub's OIDC provider and an IAM role (AWS) or federated credential (Azure App Registration), scoped via claims to specific repo/branch/environment. Workflow requests `id-token: write`, exchanges a short-lived signed token for temporary cloud creds scoped to that job run — no static secret anywhere.

**Q: Reusable workflow vs composite action — how decide?**
A: Reusable workflow = one or more full jobs, own runner, `workflow_call`, secrets explicitly passed (or `inherit`); good for standardizing a whole pipeline stage across many repos with central governance. Composite action = a sequence of steps in the *caller's* job/runner, defined in `action.yml`; good for smaller shared step sequences (checkout+setup+restore) embedded in otherwise-different jobs.

**Q: What is a "pwn request" and how prevent it?**
A: A workflow triggered by `pull_request_target` (or `workflow_run`) — which runs with the base repo's full token/secrets — checks out and executes untrusted PR head code, letting a malicious contributor run arbitrary code with privileged access. Prevention: never check out/execute PR head in a `pull_request_target` context; split untrusted build/test (plain `pull_request`, no secrets) from privileged deploy logic (`push`/manual/gated, only trusted merged code or vetted artifacts).

**Q: Migrate an Azure DevOps YAML pipeline to GitHub Actions?**
A: Map stage→job(s), ADO `template`→reusable workflow/composite action, variable groups→repo/environment secrets and variables (re-link Key Vault via OIDC-authenticated steps), self-hosted agent pools→self-hosted runners or ARC, approvals/checks→GitHub environments with required reviewers. Mechanical translation is easy; the real work is re-auditing the security model (default `GITHUB_TOKEN` permissions, secret scoping, OIDC trust policies) — don't assume a lift-and-shift is secure by default.

**Q: Caching vs artifacts, and a wrong-choice bug?**
A: Cache speeds repeated restores (NuGet/npm/Docker), best-effort/evictable/immutable-per-key; artifacts are durable, guaranteed-for-retention outputs passed between jobs or downloaded. Using cache to pass build output between jobs is a bug — a cache miss (eviction/key change) silently produces a stale/missing build instead of a hard failure, whereas a missing artifact download fails loudly. Use artifacts for job-to-job data dependencies, cache only for dependency-restore acceleration.

---

## Summary of Additions

`[new content]` sections added to close gaps vs current (2026) senior .NET interviews (original notes were already Q&A-answered; this pass was gap-filling + reorganization):

1. **.NET-Specific CI/CD** — `dotnet restore --locked-mode`, TRX test reporting, coverage collection, versioning tools (GitVersion/nbgv/MinVer).
2. **Containerized .NET Apps — Build & Push** — Docker Buildx + GHA cache layer.
3. **Pwn Requests & `pull_request_target`** — the most-tested current GHA security topic.
4. **Pinning Actions to Commit SHAs** — supply-chain security (2024-2025 compromised-action incidents).
5. **`GITHUB_TOKEN` Least Privilege** — `permissions:` blocks.
6. **Third-Party Action Vetting** — supply-chain theme.
7. **Environment Protection Rules as a Security Control** — secret-scoping angle.
8. **OIDC Federation** — replaces long-lived static keys.
9. **Change-Detection-Based Selective Builds** (monorepo).
10. **Nx/Turborepo-Aware CI** — mixed-stack monorepos.
11. **Where Runner Minutes Go / Timeout Hygiene** — cost/performance.
12. **GitHub Actions vs Azure DevOps Pipelines** — migration relevance.

**Staleness flagged inline:**
- Action versions (`checkout@v3`, `cache@v3`, `upload/download-artifact@v3`) — outdated/deprecated, updated to v4+.
- Runner OS images (`ubuntu-20.04`, `windows-2019`, `macos-12`) — retired; updated list, verify against live matrix (`-latest` rotates).
- `actions/setup-debugging` — not a real official action; replaced with `ACTIONS_STEP_DEBUG`/`ACTIONS_RUNNER_DEBUG` secrets + `tmate`.

---

## Summary of [gaps] Additions (This Pass)

A follow-up gap analysis flagged that cloud/deployment examples defaulted to generic/Azure-flavored patterns without tying back to AWS (candidate's target cloud) or Terraform/CDKTF (candidate's IaC). One section added:

1. **AWS-Native CI/CD Integration Points** — full `aws-actions/configure-aws-credentials` flow (STS `AssumeRoleWithWebIdentity` under the hood), the exact IAM OIDC provider + trust policy shape (Terraform `aws_iam_openid_connect_provider`/`aws_iam_role`), and how caching/artifacts concretely connect to **ECS** (image-based, `type=gha` layer caching to ECR, task-def rollout) vs **Lambda** (artifact/zip-based, `upload-artifact` as the deployable handoff). Naming the specific AWS action, the trust-policy claim conditions (`aud`/`sub`), and which of cache-vs-artifact fits which AWS compute target is what signals AWS-specific (not generic-cloud) fluency.
