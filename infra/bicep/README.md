# `infra/bicep/` — Phase A IaC

Declarative description of LankaConnect's Azure resources. Goal: every resource in `lankaconnect-staging` (and later `lankaconnect-production`) is in source control before W3 (Notifications) module extraction begins.

## Status (2026-05-12) — W1.4 DONE ✅

Verified against actual staging inventory via `az resource list -g lankaconnect-staging` + `az deployment group what-if`. **7 modules cover 12 of 16 staging resources at what-if `NoChange`.** The remaining 4 staging resources are correctly `Ignore`d (Container Apps API + UI managed by CI; 2 auto-generated LAWs managed by Azure).

### Final what-if result (2026-05-12)

```
Resource changes: 1 to modify, 12 no change, 4 to ignore.
```

The 1 "modify" is a documented false-positive on `Microsoft.App/managedEnvironments.appLogsConfiguration.logAnalyticsConfiguration.customerId` — Bicep `reference()` expression resolves to the same GUID at deploy time but what-if can't pre-resolve. See https://aka.ms/WhatIfIssues.

### Modules landed

| File | Purpose | Status |
|---|---|---|
| `main.bicep` | Composition root, resource-group-scoped, 7 modules wired | ✅ done |
| `modules/container-apps-env.bicep` | Container Apps Env (`lankaconnect-staging-env2`) + Log Analytics workspace (`lankaconnect-staging-logs`) | ✅ NoChange |
| `modules/postgres.bicep` | PostgreSQL Flexible Server + `LankaConnectDB` + `AllowAzureServices` firewall | ✅ NoChange |
| `modules/key-vault.bicep` | Key Vault container (`lankaconnect-staging-kv`) — unblocks W1.1b | ✅ NoChange |
| `modules/acr.bicep` | Azure Container Registry (`lankaconnectstaging`) | ✅ NoChange |
| `modules/storage.bicep` | Storage Account (`lankaconnectstrgaccount`, `eastus`) | ✅ NoChange |
| `modules/managed-identity.bicep` | User-Assigned Managed Identity (`lankaconnect-staging-identity`) | ✅ NoChange |
| `modules/acs.bicep` | ACS + Email Service + 2 domains (`AzureManagedDomain`, `lankaconnect.app`) | ✅ NoChange |
| `staging.parameters.json` | Env-specific values | ✅ done |
| `production.parameters.json` | Production env values | ⏳ when production RG is built |
| `.github/workflows/bicep-what-if.yml` | Non-blocking `what-if` CI on push to develop + PR | ✅ wired |

### Resources intentionally NOT in Bicep

| Resource | Reason |
|---|---|
| `lankaconnect-api-staging` Container App | Managed by `.github/workflows/deploy-staging.yml` (image SHA changes every commit); dual-ownership avoided |
| `lankaconnect-ui-staging` Container App | Managed by `.github/workflows/deploy-ui-staging.yml`; same reason |
| `workspace-lankaconnectstagingXKMq`, `workspace-lankaconnectstagingoue8` | Auto-generated LAWs created by Azure when Container Apps connect telemetry |

### Master-TODO §W1.4 acceptance items that don't exist today

- Application Insights — not in staging RG today. When provisioned, add `modules/application-insights.bicep`.
- Azure App Configuration — not in staging RG today. Likely added with W1.5 Microsoft.FeatureManagement work.

### Exit criterion status (architect 2026-05-11)

`scripts/azure/provision-staging.sh` was marked **BICEP PRIMARY** in commit `19a728a2`:
- Top-of-file deprecation header documents Bicep as source of truth
- Per-section markers on Steps 2, 3, 4, 6 point at the corresponding Bicep module + what-if NoChange verification date
- Bash blocks remain operational (idempotent `az X show` gates) for ops continuity but no longer authoritative
- Literal deletion deferred because Steps 3 + 5 have cross-section dependencies (`POSTGRES_CONNECTION_STRING` feeds KV secret population)
- Future hardening: once Container App bootstrap moves to Bicep, retire bash bodies entirely

### Trail of commits

| Commit | Phase | What |
|---|---|---|
| `3df82003` | W1.4 v1 skeleton | main.bicep + container-apps-env + staging params + README + .gitignore |
| `18449e83` | Phase 1 module 2 | postgres.bicep — Flexible Server + DB + firewall |
| `ae7b7302` | Phase 1 module 3 | key-vault.bicep — vault container (unblocks W1.1b) |
| `23e3a2ff` | Phase 1 module 4 | acr.bicep — Basic SKU registry |
| `206e8d13` | Phase 1 critical fix | Container Apps Env name `-env` → `-env2` (matched actual staging) |
| `9ccf3c86` | Phase 1 property-parity | all 4 modules reach NoChange — peerAuthentication, dataEndpointEnabled, postgres storage tier, etc. |
| `d2f6d8e7` | Phase 2 modules | storage + managed-identity + acs (ACS + email service + 2 domains) |
| `f312e86c` | Phase 3 CI | bicep-what-if.yml non-blocking workflow |
| `19a728a2` | Phase 4 cleanup | scripts/azure/provision-staging.sh BICEP PRIMARY markers |

Total: **9 commits direct to develop, no PRs** (per Phase A trunk-based discipline).

## Convention

- **One module per Azure resource type.** Modules live in `modules/`. Keep module API surface small: scalar params in, scalar/object outputs out, no module-to-module direct refs (compose in `main.bicep`).
- **Idempotent describe-what-exists**. Every parameter value must match what's currently in `lankaconnect-staging`. A `what-if` against existing infra MUST show zero changes once a module covers a resource. Drift surfaces here, not via surprise.
- **Resource names from `scripts/azure/provision-staging.sh`**: `lankaconnect-${environment}-{db,kv,env,logs,ai,appcfg}` plus `lankaconnect${environment}` for ACR (ACR naming rule: lowercase, no hyphens).

## How to use

### Validate (no deployment)
```bash
az bicep build infra/bicep/main.bicep
```
Build succeeds = syntax valid + parameter types check.

### Preview a deployment (non-destructive)
```bash
az deployment group what-if \
  --resource-group lankaconnect-staging \
  --template-file infra/bicep/main.bicep \
  --parameters infra/bicep/staging.parameters.json
```

Read the output carefully:
- **`= unchanged`** rows: this Bicep describes existing infra correctly ✅
- **`+ create`** rows: this Bicep would add something new — usually wrong at this stage; investigate parameter mismatch
- **`- delete`** rows: this Bicep would remove something that already exists — usually wrong; check if a resource is missing from the template
- **`~ modify`** rows: this Bicep would change a property — investigate whether the change is intentional

Goal during Phase A: every `what-if` shows ALL `unchanged`.

### Deploy (later — after parity confirmed)
```bash
az deployment group create \
  --resource-group lankaconnect-staging \
  --template-file infra/bicep/main.bicep \
  --parameters infra/bicep/staging.parameters.json
```

Do NOT deploy yet. Wait for all modules + a clean `what-if` + the non-blocking CI workflow.

## Master TODO references

- **W1.4 entry**: `docs/MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md` § "W1 — Execution Status (2026-05-11)" + body section "W1.4 — Bicep skeleton" (line 438+).
- **W1.4 exit criterion** (architect 2026-05-11): as each module covers a resource, the corresponding `scripts/azure/provision-*.sh` lines get deleted in the same commit. Prevents IaC + shell drift.
- **W1.4 architect note**: Bicep skeleton is infrastructure-as-code authoring; **W1.1b** Key Vault wiring is application-config plumbing. They live in different commits even though `modules/key-vault.bicep` is a W1.4 deliverable.
