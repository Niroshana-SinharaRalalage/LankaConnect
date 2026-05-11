# `infra/bicep/` — Phase A IaC

Declarative description of LankaConnect's Azure resources. Goal: every resource in `lankaconnect-staging` (and later `lankaconnect-production`) is in source control before W3 (Notifications) module extraction begins.

## Status (2026-05-11) — SKELETON

| File | Purpose | Status |
|---|---|---|
| `main.bicep` | Composition root, resource-group-scoped | ✅ landed (4 modules wired) |
| `modules/container-apps-env.bicep` | Container Apps Env + Log Analytics workspace | ✅ landed |
| `modules/postgres.bicep` | PostgreSQL Flexible Server (`lankaconnect-staging-db`) + `LankaConnectDB` database + `AllowAzureServices` firewall rule | ✅ landed |
| `modules/key-vault.bicep` | Key Vault (`lankaconnect-staging-kv`) — vault itself only; secrets populated by ops; unblocks W1.1b application wiring | ✅ landed |
| `modules/acr.bicep` | Azure Container Registry (`lankaconnectstaging`) — Basic SKU, admin user enabled | ✅ landed |
| `modules/acr.bicep` | Azure Container Registry (`lankaconnectstaging`) | ⏳ follow-up commit |
| `modules/application-insights.bicep` | App Insights for trace/metric ingestion | ⏳ follow-up commit |
| `modules/app-configuration.bicep` | Azure App Configuration for `Microsoft.FeatureManagement` (W1.5) | ⏳ follow-up commit |
| `staging.parameters.json` | Env-specific values | ✅ landed |
| `production.parameters.json` | Env-specific values | ⏳ after staging is at parity |
| `.github/workflows/bicep-what-if.yml` | Non-blocking `what-if` on every `infra/bicep/` PR | ⏳ follow-up commit |

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
