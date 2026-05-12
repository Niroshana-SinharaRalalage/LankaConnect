// =============================================================================
// User-Assigned Managed Identity
// =============================================================================
//
// Describes existing `lankaconnect-staging-identity`. Used by Container Apps
// for ACR pull, Key Vault secret access (once W1.1b lands), and Azure
// Communication Services authentication.
//
// Role assignments (RBAC) are NOT in this module — they live wherever the
// resource being granted access to is declared (e.g. KV access policies
// will be added in W1.1b as a Microsoft.KeyVault/vaults/accessPolicies child).
// =============================================================================

@description('Managed identity name.')
param name string

@description('Azure region.')
param location string

// Tags intentionally NOT applied in Bicep (see container-apps-env.bicep note).

// ---------- User-Assigned Managed Identity ----------

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: name
  location: location
}

// ---------- Outputs ----------

@description('Managed identity resource ID.')
output id string = identity.id

@description('Principal ID (object ID) — used for RBAC role assignments.')
output principalId string = identity.properties.principalId

@description('Client ID — used by applications to authenticate via this identity.')
output clientId string = identity.properties.clientId

@description('Managed identity name.')
output name string = identity.name
