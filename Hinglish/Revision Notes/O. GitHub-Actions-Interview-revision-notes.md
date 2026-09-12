# GitHub Actions — Interview Quick-Revision Notes

Yeh guide (`../Detailed Notes/O. GitHub-Actions-Interview-Guide.md`) se derived quick-revision notes hain — **har section** cover kiya gaya hai, same order mein, **Q/A + tight bullets** format mein taaki bina guide khole brush-up ho jaaye. Explanations Hinglish mein, code/keywords English mein.

---

## 1. Core Concepts

**Q: GitHub Actions kya hai?**
A: GitHub ka native CI/CD + automation platform, `.github/workflows/` mein stored YAML files se driven, events par trigger hota hai.

**Building blocks:**

| Concept | Kya hai |
|---|---|
| **Workflow** | Ek YAML file, event se triggered automated process |
| **Job** | Steps ka group jo ek single runner par chalta hai. Default **parallel**; sequencing `needs` se |
| **Step** | Job ke andar single task — `run` (shell) ya `uses` (action) |
| **Action** | Reusable unit (JS, Docker, ya composite) — first-party/community/custom |
| **Runner** | Compute (VM/container) jo job chalata hai — GitHub-hosted ya self-hosted |

**Lifecycle:** Trigger event → workflow selected → jobs (independent = parallel, `needs` = DAG) → steps (checkout → run/uses) → artifacts/deployment/status check.

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

- **Version note:** koi bhi `@v3` reference (checkout, cache, upload/download-artifact) **stale** hai — 2026 mein `@v4+` use karo. `upload/download-artifact@v3` deprecated aur hosted runners par disabled.
- **Interviewer angle:** *job* = isolation ki unit (apna VM/FS), *step* = us isolation ke andar sequencing ki unit. Jenkins/Azure DevOps stage-vs-task se aane walon ke liye common trip-up.

---

## 2. Workflow Triggers

| Trigger | Use case |
|---|---|
| `push` | Matching branches par commits |
| `pull_request` | PR open/sync/reopen — **merge commit** ke against chalta hai, head ke against nahi |
| `schedule` (cron) | Nightly/scheduled jobs |
| `workflow_dispatch` | Manual (UI/API), optional typed inputs |
| `workflow_call` | Workflow ko reusable/callable banata hai |
| `repository_dispatch` | External API se trigger |
| `issues`, `release` | Repo/object lifecycle events |

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

**Manual dispatch with typed input:**
```yaml
on:
  workflow_dispatch:
    inputs:
      environment:
        required: true
        default: 'staging'
        type: choice
        options: [staging, production]
```
Trigger: Actions tab, ya `gh workflow run` / REST API.

**Cross-repo trigger:** `repository_dispatch` ya `gh workflow run workflow.yml --repo other/repo`. Token ke paas permissions chahiye (PAT/GitHub App). Default `GITHUB_TOKEN` doosre repo mein — aur infinite recursion rokne ke liye same repo mein doosra workflow run bhi — trigger nahi kar sakta.

**Gotcha (security boundary):** fork se aaya `pull_request` **read-only restricted `GITHUB_TOKEN`** ke saath, **koi repo secrets nahi**. `pull_request_target` base repo context mein (full token/secrets) chalta hai lekin default se *base* ref checkout karta hai — misuse par classic **pwn request** vector (dekho §13).

---

## 3. Runners

**Runner** = VM/container jo job chalata hai.

### GitHub-Hosted vs Self-Hosted

| Aspect | GitHub-Hosted | Self-Hosted |
|---|---|---|
| Maintenance | Zero (auto provision/teardown) | Aap OS patch/SDK/capacity manage karo |
| Cost | Per-minute + OS multiplier (Linux 1x, Windows 2x, macOS 10x) | Sirf infra cost, Actions minutes free |
| Networking | Public internet only, ephemeral IP | VPC/on-prem, private feeds (internal NuGet/npm) access |
| State | Fully ephemeral (clean VM har run) | Persistent by default (danger: leftover secrets/caches/malware) |
| Security | Per-job isolated VM, GitHub patches | Aapki zimmedari; malicious PR code internal infra par chal sakta hai — bada blast radius |
| Hardware | Fixed SKUs | Kuch bhi — bare metal, GPU, ARM, custom |
| Best for | OSS/standard CI, quick start | Compliance, big monorepos, licensed SW, private network, scale par cost control |

**Current images:** Ubuntu (`ubuntu-latest/24.04/22.04`), Windows (`windows-latest/2025/2022`), macOS (`macos-latest/15/14`).
- Stale (retired): `ubuntu-20.04`, `windows-2019`, `macos-12`. Live matrix ke liye `actions/runner-images` check karo; `-latest` rotate hota hai.

```bash
./config.sh --url <repo-url> --token <runner-token>
./run.sh
```

**Follow-up "scale kaise?"** → Kubernetes par **Actions Runner Controller (ARC)** (ephemeral autoscaling), ya cloud autoscaling groups. **Public repo par kabhi long-lived self-hosted runner mat chalao** — koi bhi PR se arbitrary code chala sakta hai.

---

## 4. Variables, Secrets & Environments

```yaml
env:
  NODE_ENV: production
steps:
  - run: echo "Running in $NODE_ENV mode"
```

**Secrets:** Settings → Secrets and variables → Actions. Repo/environment/org level scoped.
```yaml
env:
  API_KEY: ${{ secrets.MY_SECRET_KEY }}
```

### `env` vs `secrets`

| | `env` | `secrets` |
|---|---|---|
| Visibility | Logs mein visible | Logs mein masked/redacted |
| Encryption | No | Rest mein encrypted, runtime par decrypt |
| Scope | Public to repo/workflow | Private, access-controlled |
| Use | Non-sensitive config, flags | API keys, conn strings, tokens |

**Gotcha:** secret masking = naive substring match on printed value. Base64-encode / split / transform karke print karne se protect **nahi** hota. Secret ko "clever" workaround se echo mat karo — assume exfiltratable.

### Environments & approval gates
```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: production
    steps:
      - run: ./deploy.sh
```
Environments (Settings → Environments) dete hain:
- Secrets ko us environment tak scope karna (prod DB string staging job ko invisible).
- Proceed se pehle **manual approval** (designated reviewers) — prod gates ke liye critical.
- **Deployment branch policies** (kaunse branches/tags deploy kar sakte hain).
- Wait timer.

Azure DevOps ke **environments + approvals/checks** ka direct equivalent; "prod deployment gate kaise?" ka standard senior jawab.

---

## 5. Jobs, Dependencies & Control Flow

### `needs` — job dependencies
```yaml
jobs:
  build:
    runs-on: ubuntu-latest
  test:
    runs-on: ubuntu-latest
    needs: build
  deploy:
    runs-on: ubuntu-latest
    needs: [build, test]
```
`needs` ke bina = parallel; `needs` DAG banata hai. Downstream jobs upstream **outputs** consume karte hain (`needs.build.outputs.version`).

### `if` conditionals
```yaml
- if: github.ref == 'refs/heads/main'
  run: echo "on main"
```
Step **ya** job level. Common: `github.event_name == 'pull_request'`, `success()`, `failure()`, `always()`, `cancelled()`.

**Gotcha:** prior step fail hone par subsequent steps default se **skip** — cleanup/notification chahiye to `if: always()` ya `if: failure()` lagao.

### Notifications (Slack & Email)
Notifications koi special primitive nahi — ordinary steps hain, same `if:` logic se gated, `GITHUB_TOKEN` ke bajaye webhook/SMTP secret se driven.
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
    SLACK_MESSAGE: 'Build failed on ${{ github.ref_name }}'
    SLACK_COLOR: danger
```
- Webhook = **Slack Incoming Webhook URL** (secret), koi OAuth app nahi.
- Different messages/colors chahiye to success/failure **alag steps**; single step + `${{ job.status }}` par branch chahiye to `if: always()`.
- **Email:** same pattern, SMTP action (`dawidd6/action-send-mail@v3`), same gating.
- **Reverse (Slack → workflow):** Slack bot/slash-command REST API/`gh workflow run` se `workflow_dispatch` fire kare = ChatOps. Isko `actions: write` PAT/App token chahiye — `GITHUB_TOKEN` nahi chalega (call workflow run ke bahar se aati hai).

### Concurrency control
```yaml
concurrency:
  group: ci-${{ github.ref }}
  cancel-in-progress: true
```
Redundant runs prevent. **`cancel-in-progress: true` ke bina, `concurrency` sirf runs ko serially queue karta hai, cancel nahi** — key subtlety.

### Timeouts & retries
```yaml
jobs:
  build:
    timeout-minutes: 10
```
**Koi built-in step-level retry nahi.** Options: (1) shell loop `for i in 1 2 3; do cmd && break || sleep 5; done`; (2) `nick-fields/retry@v3` (attempts/backoff/timeout).

### Step reuse within repo
True cross-job/workflow reuse ke liye **composite action** prefer karo (YAML anchors officially unsupported, fragile hack).

---

## 6. Matrix Builds

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
- **Cross-product** = separate parallel jobs (2×2 = 4 jobs).
- `fail-fast: true` (default) ek fail hote hi baaki cancel; CI mein aksar `false` chahiye (full signal). `true` sirf minutes bachane ke liye.
- `max-parallel` = concurrent matrix jobs throttle (limited self-hosted pool ke liye).
- `include`/`exclude` = one-off combos add/exclude bina full blow-up.

**Interviewer angle:** matrix kyun? DRY definition + Checks UI ek job name ke andar per-combination status legibly group karta hai.

---

## 7. Caching & Artifacts

### Caching (speed, NOT persistence guarantee)
```yaml
- uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: nuget-${{ runner.os }}-${{ hashFiles('**/packages.lock.json') }}
    restore-keys: |
      nuget-${{ runner.os }}-
```
- Exact key hit = restore; `restore-keys` = **partial** fallback.
- Ek key ki cache **write ke baad immutable** — update nahi hoti, nayi key chahiye ("cache update kyun nahi ho raha?").
- Per repo/branch scoped, ~10 GB soft cap, LRU eviction (exact limits verify karo).
- **Best-effort** — correctness ke liye kabhi rely mat karo, sirf speed. Full cache miss par bhi pipeline correct chale.

### Artifacts (durable, cross-job)
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
**v4 breaking change (gotcha):** *same* name par multiple uploads ab allowed nahi (har upload immutable naya artifact); v3 se 10x faster; matrix legs ke liye distinct names chahiye — `name: artifact-${{ matrix.os }}-${{ matrix.dotnet-version }}`. v3 deprecated/EOL.

| | Cache | Artifact |
|---|---|---|
| Purpose | Dependency restore speed up | Output jobs ke beech pass / humans ke liye preserve |
| Lifetime | Best-effort, evictable | `retention-days` guaranteed, downloadable |
| Content | node_modules, NuGet cache, Docker layers | Binaries, test results, logs, coverage |

**Long-term storage:** upload-artifact retention capped (default 90 days). Genuinely long-term = package feed (GitHub Packages/NuGet.org/Azure Artifacts) ya blob (S3/Azure Blob).

---

## 8. Reusable Workflows & Composite Actions

Commonly confused — distinction clear rakho.

| | Reusable Workflow | Composite Action |
|---|---|---|
| File | `.github/workflows/*.yml` with `on: workflow_call` | `action.yml` (+ scripts) |
| Granularity | Ek ya zyada **jobs** | Ek job ke andar **steps** ki sequence |
| Runner | Apna `runs-on` | Caller ke runner par |
| Secrets | Explicitly pass (ya `secrets: inherit`) | Caller ka context auto-inherit |
| Nesting | Nested workflows (4 levels deep) | Nested actions |
| Best for | Entire pipeline stage standardize (20 repos ka "build-test-scan") | Kuch steps standardize (checkout+setup+restore) |

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
Call (cross-repo bhi):
```yaml
jobs:
  call-workflow:
    uses: my-org/my-repo/.github/workflows/deploy.yml@main
    with:
      environment: production
    secrets:
      AWS_ACCESS_KEY_ID: ${{ secrets.AWS_ACCESS_KEY_ID }}
      # ya: secrets: inherit
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
**Gotcha:** composite ke har `run` step par **`shell:` explicitly** dena zaruri — inherit nahi hota.

**Kab kaunsa?** Composite = chhota reusable step-sequence bade job ke andar embedded (shared setup). Reusable workflow = many repos ke across entire deploy/release process centralized governance ke saath (security team owns, teams call — drift kam, auditing/patching single choke point).

---

## 9. Containers & Custom Actions

### Job inside a container
```yaml
jobs:
  docker-job:
    runs-on: ubuntu-latest
    container:
      image: mcr.microsoft.com/dotnet/sdk:8.0
    steps:
      - run: dotnet --version
```
Exact toolchain pin karne / production-matching hermetic build ke liye.

### Custom actions — teen flavors

| Type | Runtime | Use case |
|---|---|---|
| **Docker container** | Koi bhi language, container | Full control, heavier/slower start, mostly Linux only |
| **JavaScript/TS** | Node (`node20`) | Fastest, cross-platform, most Marketplace actions |
| **Composite** | Existing actions/shell orchestrate | No compile, easiest to maintain |

```yaml
# action.yml (JS skeleton)
name: 'My Custom Action'
inputs:
  who-to-greet: { required: true }
runs:
  using: 'node20'
  main: 'dist/index.js'
```

---

## 10. Deployment Patterns

### Multi-stage (dev → staging → prod)
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
    environment: production   # required reviewers se gated
    runs-on: ubuntu-latest
    steps: [ ... ]
```

### Cloud (AWS) — legacy static keys
```yaml
- uses: aws-actions/configure-aws-credentials@v4
  with:
    aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
    aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
    aws-region: us-east-1
- run: aws s3 sync ./build s3://my-bucket
```
**Senior flag:** long-lived AWS keys as secrets = *legacy*. Current best practice = **OIDC federation** (koi static creds nahi, §14). Interview mein proactively mention karo.

### IaC
```yaml
- run: terraform apply -auto-approve
```
Same OIDC principle Azure (`azure/login@v2`) / GCP (`google-github-actions/auth`) ke liye — static SP secrets avoid.

### Status badge
```markdown
![CI](https://github.com/your-org/your-repo/actions/workflows/ci.yml/badge.svg)
```

---

## 11. Debugging & Local Testing

**Failed run debug:**
- Repo secrets `ACTIONS_STEP_DEBUG=true` + `ACTIONS_RUNNER_DEBUG=true`, ya UI "Re-run with debug logging".
- `tmate` (`mxschmitt/action-tmate`) = mid-failure live runner mein SSH (flaky/self-hosted issues).
- `${{ toJSON(github) }}` / `${{ toJSON(steps) }}` dump karke context inspect karo.
- **Note:** `actions/setup-debugging` **exist nahi karta** (source mein likely hallucinated) — sahi jawab upar wale debug secrets / `tmate`.

**Local run:**
```bash
act -j build
```
`nektos/act` = workflows locally Docker mein (push/wait bina). Limits: hosted images perfectly emulate nahi, kuch contexts (`secrets`, kuch `github.*`) manual stub, environments/approvals simulate nahi.

---

## 12. .NET-Specific CI/CD

### Idiomatic build/test/publish pipeline
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
          fetch-depth: 0   # GitVersion/nbgv ke liye
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
Key points (jo interviewer sunna chahta hai):
- **`packages.lock.json` + `--locked-mode`** = deterministic restore, cache key ek exact reproducible hash par. Lock file bina `hashFiles('**/*.csproj')` weaker proxy hai, transitive drift miss karta hai.
- **`fetch-depth: 0`** = shallow clone (default 1) GitVersion/nbgv/`git describe` versioning ko break karta hai.
- **`--no-restore`/`--no-build`** = redundant work avoid.
- **Test publishing:** `.trx` natively render nahi hoti — `dorny/test-reporter` / `EnricoMi/publish-unit-test-result-action` / artifact + external tool.
- **Coverage:** `--collect:"XPlat Code Coverage"` = Cobertura XML; `danielpalme/ReportGenerator-GitHub-Action` ya Codecov/SonarCloud se visualize/gate.

### Versioning & semantic release
- **GitVersion** / **Nerdbank.GitVersioning (nbgv)** = git history/tags se auto SemVer, manual `.csproj` bumps avoid. Early step, computed version = job output jo `dotnet build -p:Version=...` + container tags consume.
- **MinVer** = lighter, purely tag-based.
- Container images + NuGet packages *same* computed value se tag karo — deployed artifact ↔ commit traceability.

### Containerized .NET — build & push
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
`type=gha` = Docker layer caching Actions cache backend par — har layer rebuild se meaningfully faster, `/var/lib/docker` cache ka modern replacement.

---

## 13. Security & Supply-Chain Hardening

### Pwn requests & `pull_request_target`
Sabse zyada tested current security topic.
- Fork `pull_request`: **read-only token, koi secrets nahi** — default safe (malicious code bhi kuch exfiltrate nahi kar sakta).
- `pull_request_target`: **base repo token + full secrets**, lekin default **base** branch checkout — safe *jab tak* PR head (`github.event.pull_request.head.sha`) checkout/execute na ho, ya attacker code na chale (malicious `postinstall` via `npm install`, checked-out Makefile/script). Privileged token + untrusted code = **"pwn request"** class.
- **Mitigation:** `pull_request_target`/`workflow_run` ko untrusted head checkout ke saath combine mat karo. Fork PR ko elevated capability ke saath build karna ho to **do workflows**: (1) unprivileged `pull_request` build/test; (2) separate manually-gated/`workflow_run` job jo sirf vetted **artifacts** (source nahi) re-use kare.

### Pinning actions to commit SHAs
- `@v4` = mutable tag; tag move ho (compromised publisher / force-push) to workflow silently different code chalayega. Real supply-chain surface (2024 `tj-actions/changed-files` incident).
- Best practice: **full commit SHA** + version comment:
  ```yaml
  - uses: actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683 # v4.2.2
  ```
- Dependabot / `pinact` = SHA-pinned actions auto-update rakhte pin.

### `GITHUB_TOKEN` least privilege
- Naye repos default read-only; par defaults par rely mat karo — explicitly declare karo:
  ```yaml
  permissions:
    contents: read
    pull-requests: write   # sirf agar job actually PR comment/label kare
  ```
- Highest-leverage, lowest-effort control — har workflow par least-privilege `permissions:` set karo.

### Third-party action vetting
- Trusted publishers prefer: GitHub, verified creators, vendors (`aws-actions/*`, `docker/*`, `azure/*`).
- Baaki: source review, SHA pin, business-critical ho to internal org action mein mirror. Unaudited third-party script par runtime dependency mat lo.
- **CodeQL / Dependabot / Scorecard** (`ossf/scorecard-action`) wire karo.

### Environment protection as security control
Environments sirf approvals ke liye nahi — scope karte hain ki given job context mein **kaunse secrets exist hi karte hain**. `dev` target karne wala malicious PR workflow `production` secrets read nahi kar sakta (woh inject hi nahi hote). `if` conditions se strong boundary.

---

## 14. OIDC Federation for Cloud Auth

**Q: OIDC long-lived cloud secrets ko kyun replace karta hai?**
A: Static creds (AWS keys / Azure SP secret) as GitHub secret ke problems: leak risk (rotate/revoke tak valid), koi natural expiry/scoping nahi, rotation = manual neglected burden.
OIDC = GitHub Actions GitHub ke OIDC provider se **short-lived signed identity token** maangta hai, jise cloud (pre-configured trust) sirf us job run ke liye temporary scoped creds mint karne ke liye trust karta hai. **Kahin static secret store nahi.**

**Flow:** Job → GitHub OIDC provider (claims: repo/branch/workflow) → signed short-lived JWT → Cloud IAM ko present → issuer+claims validate against trust policy → short-lived temp creds (minutes) → cloud APIs call.

### AWS via OIDC (no static keys)
```yaml
permissions:
  id-token: write   # OIDC ke liye required
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
IAM role ki trust policy restrict karti hai kaun assume kare, claims jaise `...:sub == "repo:my-org/my-repo:ref:refs/heads/main"`. Compromised workflow bhi sirf usi exact repo/branch context se role impersonate kar sakta hai.

### Azure equivalent
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
      # no client-secret — federated credential on App Registration
```
**Follow-up "client secret nahi to secret kya?"** → Sirf non-sensitive identifiers (client/tenant/subscription ID) — koi akela access grant nahi karta jab tak OIDC trust repo/branch claims match na kare.

---

## 15. Monorepo & Multi-Project CI Strategies

### Change-detection selective builds
Monorepo mein har push par entire pipeline = wasted minutes + slow feedback. Standard pattern:
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
Azure DevOps `paths` filter / Bazel/Nx affected-detection ka GHA equivalent. Monorepo role = near-guaranteed question.

### Nx/Turborepo-aware CI
Nx/Turborepo dependency graph compute karte hain, "affected" command (`nx affected`, `turbo run build --filter=...[origin/main]`) expose karte hain — path filters se accurate kyunki *code* dependencies samajhte hain, sirf directory boundaries nahi. Mixed-stack (.NET + JS frontend) shops mein relevant.

### Reusable workflows = monorepo governance
Path-filtering ko har project-type ke shared reusable workflow ke saath combine karo (`dotnet-build-test.yml` jo har service call kare) — build/test/security logic centrally maintained, chahe har service independently trigger ho.

---

## 16. Performance & Cost Optimization

### Runner minutes kahan jaate hain
- **OS multiplier:** Windows 2x, macOS 10x. Linux default rakho jab tak Windows (.NET Framework) / macOS (iOS builds) na chahiye — concrete quantifiable jawab.
- **Concurrency + `cancel-in-progress`:** superseded commits par minutes burn rokta.
- **Caching** (NuGet/npm/Docker layers): often single biggest lever.
- **Matrix `fail-fast` + `max-parallel`** tuning.
- **Path filters / change detection** (monorepo): unrelated pipelines skip.
- **Self-hosted runners:** high-volume predictable loads; crossover = build-vs-buy calc, koi default nahi.
- **Larger/faster hosted runners:** CPU-bound builds par wall-clock kaat dein to *aggregate* mein cheaper — assume mat karo, **benchmark** karo.

### Timeout hygiene
Job level par hamesha `timeout-minutes` (6-hour default par rely mat karo) — hung test ghanton minutes burn karta hai = common cost leak. "Aggressive timeouts har jagah" cite karne ke liye achhi practice.

---

## 17. GitHub Actions vs Azure DevOps Pipelines

.NET audience ke liye highly likely question (shops migrate/dual-run kar rahe hain).

| Aspect | GitHub Actions | Azure DevOps |
|---|---|---|
| Config | `.github/workflows/` YAML | YAML (`azure-pipelines.yml`) ya classic UI |
| Isolation | Job (VM/container) | Stage → Job → Task (extra nesting) |
| Reusability | Reusable workflows + composite actions | Templates (`extends`, `template`) |
| Marketplace | Bada community ecosystem | Chhota, enterprise/ALM-focused |
| Approvals | Environments + reviewers | Environments + approvals & checks (similar) |
| Secrets | Repo/Env/Org secrets | Variable groups, Key Vault-linked (zyada mature KV) |
| Self-hosted | Runners | Agents (same concept) |
| Cloud auth | OIDC federation | Workload identity federation |
| Repo integration | GitHub deeply (Checks API, PR, Issues) | Azure Repos ke saath best |
| Boards | GitHub Projects/Issues (lighter) | Azure Boards (heavier PM) |
| Pricing | Per-minute + OS multiplier | Per-parallel-job licensing |
| Migration driver | GitHub par consolidate + single tool | Deeper enterprise ALM / Azure lock-in |

**Angle:** har concept ka direct analog — stage→job, agent→runner, variable group→environment/secret, template→reusable workflow. Migration = mostly mechanical translation + **security re-think** (OIDC, `permissions:`) kyunki default security posture meaningfully different.

---

## 18. AWS-Native CI/CD Integration Points

### `aws-actions/configure-aws-credentials` in depth
De facto standard action — legacy static-key aur OIDC dono support. Under the hood kya karta hai batana zaruri:
```yaml
permissions:
  id-token: write   # OIDC token request karne ke liye
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
          role-session-name: gha-${{ github.run_id }}   # CloudTrail mein traceable
      - run: aws sts get-caller-identity   # sanity check
```
Flow: action GitHub OIDC provider ko signed JWT (repo/workflow/ref scoped) ke liye call → JWT pass karke STS `AssumeRoleWithWebIdentity` → AWS signature ko registered OIDC provider ke against validate, trust policy conditions ko JWT claims ke against check → match par short-lived temp creds (default 1 hr) → action inhe env vars (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_SESSION_TOKEN`) ki tarah export.

### IAM OIDC provider + role trust policy (Terraform)
Do pieces AWS-side (dono pata hone chahiye):

**1. OIDC provider register (one-time per account):**
```hcl
resource "aws_iam_openid_connect_provider" "github_actions" {
  url            = "https://token.actions.githubusercontent.com"
  client_id_list = ["sts.amazonaws.com"]
  thumbprint_list = []  # GitHub OIDC endpoint ke liye ab required nahi (current AWS docs verify karo)
}
```

**2. Exact repo/branch/environment tak scoped role (blast radius control):**
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
          # ya environment-scoped:
          # "...:sub" = "repo:my-org/my-repo:environment:production"
        }
      }
    }]
  })
}
```
**Follow-up "compromised ho to blast radius?"** → Sirf attached IAM *permissions* policy jo allow kare, aur sirf `sub` claim mein match hue exact repo/ref/environment se. Feature branch par workflow `ref:refs/heads/main`-scoped role assume nahi kar sakta — claims GitHub server-side generate hote hain, forge nahi ho sakte.
**Gotcha:** `aud` (`sts.amazonaws.com`) ko hamesha `sub` ke saath check karo — sirf `sub` = narrower hardening gap.

### Artifacts/caching tying into AWS deploy target

**ECS (image-based) deploy:**
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
      - name: Render task def
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
Same `type=gha` layer cache ECR target ke saath bhi kaam karta hai — cache backend Actions cache service hi hai, image kahin bhi push ho.

**Lambda (artifact/zip-based) deploy:**
```yaml
      - name: Publish Lambda package
        run: dotnet lambda package -pl ./src/MyFunction -o ./publish/function.zip
      - uses: actions/upload-artifact@v4     # durable handoff / audit / rollback
        with:
          name: lambda-package
          path: ./publish/function.zip
      - name: Deploy to Lambda
        run: |
          aws lambda update-function-code \
            --function-name order-processor \
            --zip-file fileb://./publish/function.zip
```
Yahan **artifact** (cache nahi) correct primitive — zip = actual deployable unit (build→deploy handover / rollback). Cache *build* accelerate karti hai, artifact *jo ship karte ho* woh carry karta hai. NuGet restore cache (`packages.lock.json` par keyed) upstream `dotnet lambda package` speed up karti hai.

**Angle:** ECS = image-based (build once → ECR push → task def update → ECS rolling/blue-green via CodeDeploy); Lambda = artifact-based (zip/container via Lambda API/SDK, often Terraform/CDKTF orchestrated). Kaunsa primitive (image vs zip) kaunse compute target ke liye — yahi AWS fluency ka actual signal.

---

## 19. Best Practices

- Action versions pin karo (security-sensitive = commit SHA; minimum major tag; kabhi `@main`/`@master` nahi).
- Har workflow par explicit least-privilege `permissions:`.
- Prod deploy gates par required reviewers ke saath `environments`.
- Jahan support ho OIDC federation prefer (long-lived creds nahi).
- `concurrency` + `cancel-in-progress` superseded runs ke liye.
- Aggressively cache (NuGet/npm/Docker) par correctness ke liye rely nahi.
- Shared CI logic reusable workflows/composite actions mein centralize (copy-paste nahi).
- Monorepos mein path filters / change detection.
- Sensible `timeout-minutes` har jagah — fail fast.
- "Build untrusted PR code" (unprivileged) ko "run privileged deploy" (trusted/gated) se split (pwn-request).
- Dependabot: app dependencies + pinned Action SHAs dono current.

---

## 20. Common Pitfalls

- `GITHUB_TOKEN` default full permissions maan lena — explicitly declare karo.
- `pull_request_target` + PR head checkout = classic pwn request.
- Correctness ke liye `actions/cache` par rely karna (sirf speed hai).
- Composite `run` steps mein `shell:` bhoolna.
- Full matrix signal chahiye tab `fail-fast: false` set na karna.
- `concurrency` akela stale runs cancel karta hai maan lena — sirf `cancel-in-progress: true` se karta hai.
- Deprecated `checkout@v3`, `upload/download-artifact@v3` — v4+ use karo.
- Multiple matrix legs ek artifact name par upload (v4 mein disallowed) — unique names.
- Secret masking ko real exfiltration boundary maan lena (encode/split bypass karta hai).
- `fetch-depth: 0` bhoolna jab full git history chahiye (versioning, changelogs).
- Public repos par long-lived non-ephemeral self-hosted runners = attack surface.

---

## 21. Sample Interview Q&A

**Q: PR open se merge tak end-to-end .NET setup mein kya hota hai?**
A: PR opened → `pull_request` event (restricted, secret-less token) → build/test/lint parallel jobs, zaruri ho to TFM/OS matrix → NuGet cache lockfile hash se restore → `dotnet build`/`test`, results TRX-aware reporter se PR checks ki tarah publish → success par artifacts (build output, coverage) upload → branch protection green checks require → main merge par separate `push` workflow trusted deploy path (reusable workflow), prod ke liye `environment` manual approval se gated, static secrets ke bajaye OIDC role assume.

**Q: CI bill bahut zyada — velocity hurt kiye bina kaise reduce?**
A: Pehle instrument (usage reports se biggest consumers). Phir: Windows/macOS → Linux jahan feasible (biggest multiplier); `concurrency`+`cancel-in-progress`; NuGet+Docker layer caching; monorepo path-based change detection; `timeout-minutes` tighten; self-hosted evaluate agar infra cost < aggregate hosted minutes; larger runner SKU sirf jahan benchmark net improvement dikhaye.

**Q: Cloud creds ko GitHub secret bina securely deploy kaise?**
A: OIDC federation — GitHub OIDC provider aur IAM role (AWS) / federated credential (Azure App Reg) ke beech trust, claims se repo/branch/environment scoped. Workflow `id-token: write` maangta, short-lived signed token ko us single job run tak scoped temp creds se exchange, koi static secret nahi.

**Q: Reusable workflow vs composite action — difference & decision?**
A: Reusable workflow = ek/zyada full jobs, apna runner, `workflow_call` se invoked, secrets explicitly pass (ya inherit); central governance ke saath many repos ka entire pipeline stage standardize. Composite = caller ke job/runner ke andar steps ki sequence, `action.yml`; smaller embedded shared step sequences (checkout+setup+restore).

**Q: "Pwn request" kya hai, prevent kaise?**
A: `pull_request_target`/`workflow_run` se triggered workflow (base repo full token/secrets) untrusted PR head checkout/execute kar leta hai → malicious contributor privileged access se arbitrary code chala leta hai. Prevention: `pull_request_target` context mein PR head kabhi checkout/execute mat karo; untrusted build/test (`pull_request`, no secrets) ko privileged deploy (trusted merged code / vetted artifacts par `push`/manual/gated) se split.

**Q: Existing Azure DevOps YAML pipeline GitHub Actions mein kaise migrate?**
A: stage→job(s), `template`→reusable workflow/composite, variable groups→repo/env secrets+vars (Key Vault ko OIDC steps se re-link), self-hosted agent pools→runners/ARC, approvals/checks→required reviewers ke saath environments. Mechanical translation straightforward; real kaam = security model re-audit (default `GITHUB_TOKEN` permissions, secret scoping, OIDC trust policies Azure DevOps service connection se different) — 1:1 lift-and-shift default se secure mat maano.

**Q: Caching vs artifacts, aur galat use se bug ka scenario?**
A: Cache = repeated restores speed up, best-effort/evictable/immutable-per-key; artifacts = durable, retention tak guaranteed outputs (jobs ke beech pass / humans download). Build output jobs ke beech pass karne ke liye cache use karna = guaranteed bug — cache miss (eviction/key change) hard failure ke bajaye silently stale/missing output deta hai, jabki missing artifact download loudly fail hota hai. Job-to-job dependencies = hamesha artifacts, cache sirf dependency-restore acceleration.

---

## 22. Summary of Additions

Current (2026) senior .NET interviews ke gaps close karne ke liye add kiye gaye topics (original notes Q&A the, koi unanswered question nahi — yeh pass gap-filling + reorg):
1. **.NET-Specific CI/CD** — `dotnet restore --locked-mode`, TRX reporting, coverage, versioning (GitVersion/nbgv/MinVer).
2. **Containerized .NET — Build & Push** — Docker Buildx + GHA cache layer.
3. **Pwn Requests & `pull_request_target`** — single most-tested security topic.
4. **Pinning Actions to Commit SHAs** — supply-chain (2024-25 incidents).
5. **`GITHUB_TOKEN` Least Privilege** — `permissions:` blocks.
6. **Third-Party Action Vetting** — supply-chain extend.
7. **Environment Protection as Security Control** — secret-scoping angle.
8. **OIDC Federation** — long-lived static keys replace.
9. **Change-Detection Selective Builds** (monorepo).
10. **Nx/Turborepo-Aware CI** — mixed-stack.
11. **Runner Minutes / Timeout Hygiene** — cost/performance.
12. **GitHub Actions vs Azure DevOps** — .NET migration relevance.

**Staleness flagged inline** (factual, not conflicting notes):
- Action versions `@v3` (checkout/cache/upload/download-artifact) → v4+ throughout, Core Concepts + Caching mein callout.
- Runner images `ubuntu-20.04`/`windows-2019`/`macos-12` retired → updated list, live matrix verify flag (`-latest` rotate).
- `actions/setup-debugging` real official action nahi → correct approach `ACTIONS_STEP_DEBUG`/`ACTIONS_RUNNER_DEBUG` + `tmate`.

---

## 23. Summary of [gaps] Additions (This Pass)

Follow-up gap-analysis ne flag kiya ki cloud/deployment examples generic/Azure-flavored the, bina AWS (candidate ka target cloud) ya Terraform/CDKTF (candidate ka IaC tool) se tie back. Close karne ke liye:
1. **AWS-Native CI/CD Integration Points** — guide OIDC conceptually + bare `configure-aws-credentials` snippet dikhata tha, lekin deep nahi tha: full flow (STS `AssumeRoleWithWebIdentity` under the hood), exact IAM OIDC provider + trust policy shape (Terraform `aws_iam_openid_connect_provider`/`aws_iam_role`), aur caching/artifacts concretely **ECS** (image-based, ECR tak `type=gha`, task-def rollout) vs **Lambda** (artifact/zip, `upload-artifact` handoff) par kaise connect hote hain. Akela "OIDC federation" general-cloud talking point hai; specific AWS action + trust-policy claims (`aud`/`sub`) + cache-vs-artifact kis compute target ke liye — yeh name karna hi AWS-specific fluency signal karta hai.
