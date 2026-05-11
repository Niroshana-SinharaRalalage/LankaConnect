# `infra/bicep/` — Phase A IaC

Declarative description of LankaConnect's Azure resources. Goal: every resource in `lankaconnect-staging` (and later `lankaconnect-production`) is in source control before W3 (Notifications) module extraction begins.

## Status (2026-05-11) — W1.4 v1 SKELETON

Verified against actual staging inventory via `az resource list -g lankaconnect-staging` + `az deployment group what-if`. **4 modules cover 7 of 16 staging resources by name + type.**

### Modules landed

| File | Purpose | Status |
|---|---|---|
| `main.bicep` | Composition root, resource-group-scoped, 4 modules wired | ✅ landed |
| `modules/container-apps-env.bicep` | Container Apps Env (`lankaconnect-staging-env2`) + Log Analytics workspace (`lankaconnect-staging-logs`) | ✅ landed |
| `modules/postgres.bicep` | PostgreSQL Flexible Server (`lankaconnect-staging-db`) + `LankaConnectDB` database + `AllowAzureServices` firewall rule | ✅ landed |
| `modules/key-vault.bicep` | Key Vault (`lankaconnect-staging-kv`) — vault container only; secrets populated by ops; unblocks W1.1b | ✅ landed |
| `modules/acr.bicep` | Azure Container Registry (`lankaconnectstaging`) — Basic SKU, admin user enabled | ✅ landed |
| `staging.parameters.json` | Env-specific values (location, environment) | ✅ landed |
| `production.parameters.json` | Env-specific values | ⏳ after staging at parity |
| `.github/workflows/bicep-what-if.yml` | Non-blocking `what-if` on every `infra/bicep/` change | ⏳ follow-up commit |

### Resources in staging NOT YET covered (sub-tasks for W1.4.x follow-up)

Discovered via `az resource list` 2026-05-11. The master-TODO §W1.4 acceptance list missed these:

| Resource | Type | Sub-task |
|---|---|---|
| `lankaconnect-api-staging` | `Microsoft.App/containerApps` | W1.4.6 — Container App (API) module |
| `lankaconnect-ui-staging` | `Microsoft.App/containerApps` | W1.4.7 — Container App (UI) module |
| `lankaconnectstrgaccount` | `Microsoft.Storage/storageAccounts` (in capitalized RG `LankaConnect-Staging`) | W1.4.8 — Storage Account module |
| `lankaconnect-communication` | `Microsoft.Communication/CommunicationServices` | W1.4.9 — ACS module |
| `lankaconnect-email` + 2 domains | `Microsoft.Communication/EmailServices` + Domains | W1.4.10 — Email Service module |
| `lankaconnect-staging-identity` | `Microsoft.ManagedIdentity/userAssignedIdentities` | W1.4.11 — Managed Identity module |
| `workspace-lankaconnectstagingXKMq`, `workspace-lankaconnectstagingoue8` | auto-generated LAWs | Leave unmanaged (Azure auto-creates) |

### Master-TODO §W1.4 items NOT IN staging today

- ❌ Application Insights — does NOT exist in staging RG; if needed, "create-new" task (not idempotent describe)
- ❌ Azure App Configuration — does NOT exist; create-new task

These are add-not-describe modules; they belong to future sub-tasks when the resources are actually provisioned.

### Property-level parity (sub-task pending)

Latest `what-if` (2026-05-11) shows the 4 modules **target the right 7 resources by name + type** (zero creates of resources that exist, zero deletes), but reports `changeType: Deploy` rather than `NoChange` — meaning property-level delta could exist. A property-parity sub-task per module is required: `az resource show` each existing resource, compare with module params, adjust until `what-if` reports `NoChange`. ETA roughly 30 min per module.

### Exit criterion status (architect 2026-05-11)

`scripts/azure/provision-staging.sh` lines for env/postgres/kv/acr/logs would be deletable IF property-parity were complete. Since `what-if` cannot yet confirm `NoChange`, the bash provisioner lines stay. Will delete in the W1.4.x sub-tasks once each module shows `NoChange`.

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
