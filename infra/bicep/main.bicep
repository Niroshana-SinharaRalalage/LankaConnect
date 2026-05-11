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

@description('Postgres admin login. Staging uses adminuser.')
param postgresAdminLogin string = 'adminuser'

@description('Postgres admin password. Pass at deploy time — never commit. Will be moved to a Key Vault reference once modules/key-vault.bicep lands.')
@secure()
@minLength(8)
param postgresAdminPassword string

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

module postgres 'modules/postgres.bicep' = {
  name: 'postgres-${environment}'
  params: {
    name: 'lankaconnect-${environment}-db'
    location: location
    administratorLogin: postgresAdminLogin
    administratorLoginPassword: postgresAdminPassword
    tags: commonTags
  }
}

module keyVault 'modules/key-vault.bicep' = {
  name: 'keyVault-${environment}'
  params: {
    name: 'lankaconnect-${environment}-kv'
    location: location
    // Production will pass enablePurgeProtection: true here.
    enablePurgeProtection: environment == 'production'
    tags: commonTags
  }
}

module acr 'modules/acr.bicep' = {
  name: 'acr-${environment}'
  params: {
    // ACR naming rule: alphanumeric only, no hyphens
    name: 'lankaconnect${environment}'
    location: location
    // Staging: Basic. Production should override to Premium in production.parameters.json.
    sku: 'Basic'
    tags: commonTags
  }
}

// Pending follow-up modules (NOT YET LANDED — placeholders for review):
// - modules/application-insights.bicep -> lankaconnect-${environment}-ai
// - modules/app-configuration.bicep   -> lankaconnect-${environment}-appcfg (for FeatureManagement W1.5)

// ---------- Outputs ----------

@description('Container Apps Environment resource ID — consumed by Container App modules.')
output containerAppsEnvId string = containerAppsEnv.outputs.id

@description('Default DNS suffix for Container Apps in this environment.')
output containerAppsDefaultDomain string = containerAppsEnv.outputs.defaultDomain

@description('Log Analytics workspace ID — wired into containerAppsEnv for log shipping.')
output logAnalyticsWorkspaceId string = containerAppsEnv.outputs.logAnalyticsWorkspaceId

@description('Postgres FQDN — consumed by Container App env vars for connection strings.')
output postgresFqdn string = postgres.outputs.fullyQualifiedDomainName

@description('Postgres application database name.')
output postgresDatabaseName string = postgres.outputs.databaseName

@description('Key Vault URI — passed to AddAzureKeyVault in W1.1b application wiring.')
output keyVaultUri string = keyVault.outputs.vaultUri

@description('Key Vault name — useful for `az keyvault secret` commands during W1.1b.')
output keyVaultName string = keyVault.outputs.name

@description('ACR login server (e.g. lankaconnectstaging.azurecr.io) — Container Apps reference this for image pulls.')
output acrLoginServer string = acr.outputs.loginServer

@description('ACR name (informational).')
output acrName string = acr.outputs.name
