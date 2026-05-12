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

// Tagging intentionally NOT applied uniformly via Bicep — staging resources
// have heterogeneous tag state. Tag policy is a separate later task (Azure
// Policy or a one-off `az tag update` pass).

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
    // NOTE: actual staging resource is `lankaconnect-staging-env2` — the
    // original `lankaconnect-staging-env` was created and replaced; the
    // "2" suffix is the canonical live name. Verified via `az resource
    // list` 2026-05-11. provision-staging.sh §line 53 has stale name.
    name: 'lankaconnect-${environment}-env2'
    location: location
    logAnalyticsWorkspaceName: 'lankaconnect-${environment}-logs'
  }
}

module postgres 'modules/postgres.bicep' = {
  name: 'postgres-${environment}'
  params: {
    name: 'lankaconnect-${environment}-db'
    location: location
    administratorLogin: postgresAdminLogin
    administratorLoginPassword: postgresAdminPassword
  }
}

module keyVault 'modules/key-vault.bicep' = {
  name: 'keyVault-${environment}'
  params: {
    name: 'lankaconnect-${environment}-kv'
    location: location
    // Production will pass enablePurgeProtection: true here.
    enablePurgeProtection: environment == 'production'
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
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage-${environment}'
  params: {
    // Storage naming rule: lowercase alphanumeric, 3-24 chars, no hyphens
    name: 'lankaconnectstrgaccount'
    // Existing staging storage is in eastus (NOT eastus2 like other resources).
    // Cross-region rebuild is out of W1.4 scope.
    location: 'eastus'
  }
}

module managedIdentity 'modules/managed-identity.bicep' = {
  name: 'identity-${environment}'
  params: {
    name: 'lankaconnect-${environment}-identity'
    location: location
  }
}

module communicationServices 'modules/acs.bicep' = {
  name: 'acs-${environment}'
  params: {
    communicationServiceName: 'lankaconnect-communication'
    emailServiceName: 'lankaconnect-email'
    customDomainName: 'lankaconnect.app'
  }
}

// Container Apps (lankaconnect-api-staging, lankaconnect-ui-staging) are
// intentionally NOT in Bicep. They are managed by CI workflows
// (.github/workflows/deploy-staging.yml + deploy-ui-staging.yml) which push
// new image tags on every commit. Modeling them here would create dual-
// ownership: Bicep would try to set the image to a literal tag while CI
// sets it to the latest commit SHA, causing perpetual drift.
//
// Future state (post W1.4 if desired): move Container App orchestration
// fully into Bicep with the image tag as a deploy-time parameter passed
// from CI. Out of W1.4 scope.

// Application Insights + Azure App Configuration: planned by master TODO
// §W1.4 but DO NOT EXIST in staging today. Will be added as create-new
// modules when those resources are provisioned (e.g. App Config arrives
// with W1.5 Microsoft.FeatureManagement work).

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

@description('Storage account primary blob endpoint — used in EmailBrandingService blob URLs.')
output storagePrimaryBlobEndpoint string = storage.outputs.primaryBlobEndpoint

@description('Managed identity principal ID — used for RBAC role assignments in downstream modules.')
output managedIdentityPrincipalId string = managedIdentity.outputs.principalId

@description('Managed identity resource ID — assigned to Container Apps for ACR pull + KV access.')
output managedIdentityId string = managedIdentity.outputs.id

@description('ACS hostname — used in connection strings.')
output communicationServiceHost string = communicationServices.outputs.communicationServiceHost

@description('Custom email domain From sender domain — used in EmailBrandingService.')
output emailFromSenderDomain string = communicationServices.outputs.customDomainSenderDomain
