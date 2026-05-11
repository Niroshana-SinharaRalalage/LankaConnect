// =============================================================================
// LankaConnect — Phase A W1.4 — Bicep composition root (resource-group scoped)
// =============================================================================
//
// Status: SKELETON. Per architect's W1.4 recommendation (2026-05-11):
//   - Establish file layout, naming convention, module pattern
//   - One illustrative module landed in this commit (container-apps-env)
//   - Remaining modules (postgres, key-vault, acr, app-insights, app-config)
//     land as follow-up commits with their own `what-if` evidence
//
// Convention: one module per Azure resource type. Modules live in
// `infra/bicep/modules/`. Parameter files per environment in `infra/bicep/`.
//
// Usage (validation only — no deployment yet):
//   az bicep build infra/bicep/main.bicep
//
// Future usage (after follow-up modules + CI wiring land):
//   az deployment group what-if \
//     --resource-group lankaconnect-staging \
//     --template-file infra/bicep/main.bicep \
//     --parameters infra/bicep/staging.parameters.json
//
// Exit criterion (master TODO W1.4 amendment 2026-05-11): as each module
// covers a resource, the corresponding `scripts/azure/provision-*.sh` lines
// get deleted in the same commit. Prevents IaC + shell drift.
// =============================================================================

targetScope = 'resourceGroup'

// ---------- Parameters ----------

@description('Environment short name. Used to build resource names + tags.')
@allowed([
  'staging'
  'production'
])
param environment string

@description('Azure region. All Phase A staging + production resources live in eastus2.')
param location string = resourceGroup().location

@description('Common tags applied to every resource for cost attribution + ownership lookup.')
param commonTags object = {
  application: 'LankaConnect'
  environment: environment
  managedBy: 'bicep'
  refactor: 'phase-a'
}

// ---------- Modules ----------

module containerAppsEnv 'modules/container-apps-env.bicep' = {
  name: 'containerAppsEnv-${environment}'
  params: {
    name: 'lankaconnect-${environment}-env'
    location: location
    logAnalyticsWorkspaceName: 'lankaconnect-${environment}-logs'
    tags: commonTags
  }
}

// Pending follow-up modules (NOT YET LANDED — placeholders for review):
// - modules/postgres.bicep            -> lankaconnect-${environment}-db
// - modules/key-vault.bicep           -> lankaconnect-${environment}-kv
// - modules/acr.bicep                 -> lankaconnect${environment} (no hyphens — ACR naming rule)
// - modules/application-insights.bicep -> lankaconnect-${environment}-ai
// - modules/app-configuration.bicep   -> lankaconnect-${environment}-appcfg (for FeatureManagement W1.5)

// ---------- Outputs ----------

@description('Container Apps Environment resource ID — consumed by Container App modules.')
output containerAppsEnvId string = containerAppsEnv.outputs.id

@description('Default DNS suffix for Container Apps in this environment.')
output containerAppsDefaultDomain string = containerAppsEnv.outputs.defaultDomain

@description('Log Analytics workspace ID — wired into containerAppsEnv for log shipping.')
output logAnalyticsWorkspaceId string = containerAppsEnv.outputs.logAnalyticsWorkspaceId
