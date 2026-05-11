// =============================================================================
// Azure Key Vault — secret storage for connection strings + JWT keys + ACS / Stripe creds
// =============================================================================
//
// Module 3 of N for Phase A W1.4. Describes the existing
// `lankaconnect-staging-kv` Key Vault.
//
// Scope of this module: the vault ITSELF (SKU, retention, access model).
// Secret VALUES are NOT managed by Bicep — they're operational concerns
// populated imperatively (provision-staging.sh § Step 5 populates 20+
// secrets including DATABASE-CONNECTION-STRING, JWT-SECRET-KEY, ACS-
// CONNECTION-STRING, STRIPE-SECRET-KEY, etc.). Bicep declares the
// container; runtime ops fill it.
//
// This module unblocks W1.1b (the deferred Azure Key Vault wiring task
// from W1.1). Once landed, the API can read secrets via Managed Identity
// instead of appsettings.json placeholders. W1.1b will:
//   1. Add Azure.Extensions.AspNetCore.Configuration.Secrets NuGet
//   2. Wire AddAzureKeyVault(vaultUri, credential) in Program.cs
//   3. Migrate every appsettings*.json secret reference to KV
//   4. Grant the staging Container App's managed identity Key Vault
//      Secrets User role (RBAC) — but existing staging is on access-policy
//      auth model (enableRbacAuthorization = false), so wiring will use
//      access policies for now and migrate to RBAC in a separate task.
//
// Source-of-truth for parity: scripts/azure/provision-staging.sh § Step 4
//   - SKU: standard
//   - Retention: 90 days
//   - RBAC authorization: false (existing vault uses access-policy auth)
// =============================================================================

@description('Name of the Key Vault. Must be globally unique.')
@minLength(3)
@maxLength(24)
param name string

@description('Azure region.')
param location string

@description('Tenant ID for access policies / RBAC. Defaults to deployment subscription tenant.')
param tenantId string = subscription().tenantId

@description('Soft-delete retention in days. Existing staging is 90.')
@minValue(7)
@maxValue(90)
param softDeleteRetentionDays int = 90

@description('Use RBAC instead of access policies. Existing staging is false — flip to true in a future task (RBAC migration) once consumers are ready.')
param enableRbacAuthorization bool = false

@description('Enable purge protection. Strongly recommended for production; defaults off for staging to allow quick teardown. Once enabled cannot be disabled.')
param enablePurgeProtection bool = false

@description('Common tags propagated from main.bicep.')
param tags object = {}

// ---------- Key Vault ----------

resource keyVault 'Microsoft.KeyVault/vaults@2024-04-01-preview' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenantId
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    enableSoftDelete: true
    softDeleteRetentionInDays: softDeleteRetentionDays
    // Conditional: purge protection can't be unset once on; staging stays off
    // unless explicitly enabled. Production module will enable.
    enablePurgeProtection: enablePurgeProtection ? true : null
    enableRbacAuthorization: enableRbacAuthorization
    publicNetworkAccess: 'Enabled' // matches existing staging shape
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
      ipRules: []
      virtualNetworkRules: []
    }
    // accessPolicies intentionally omitted in this skeleton:
    //   - Existing staging vault was created with --enable-rbac-authorization false
    //     and access policies set imperatively (e.g. the developer's principal,
    //     the API Container App's managed identity once it exists)
    //   - Including an empty `accessPolicies: []` in Bicep would WIPE existing
    //     entries on every `what-if`/deploy — dangerous
    //   - W1.1b will add the API's managed identity policy via a SEPARATE
    //     bicep resource (Microsoft.KeyVault/vaults/accessPolicies) with action
    //     `add` semantics, not `replace`
  }
}

// ---------- Outputs ----------

@description('Key Vault resource ID.')
output id string = keyVault.id

@description('Vault URI (https://...vault.azure.net/) — passed to AddAzureKeyVault in W1.1b.')
output vaultUri string = keyVault.properties.vaultUri

@description('Vault name (informational; useful for downstream az keyvault commands).')
output name string = keyVault.name
