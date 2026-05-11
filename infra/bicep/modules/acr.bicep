// =============================================================================
// Azure Container Registry — hosts API + UI container images
// =============================================================================
//
// Module 4 of N for Phase A W1.4. Describes existing `lankaconnectstaging`
// ACR (no hyphens — ACR naming rule: alphanumeric, lowercase, 5-50 chars).
//
// Source-of-truth for parity: scripts/azure/provision-staging.sh § Step 2
//   - SKU: Basic
//   - Admin user: enabled
//
// Note: admin user is enabled to match existing staging shape. Long-term
// the API + UI Container Apps should pull via Managed Identity instead
// (ACR Pull role on the env's user-assigned identity). That migration
// is out of scope for this skeleton; track as a separate hardening task.
// =============================================================================

@description('ACR name. Must be globally unique, lowercase, alphanumeric only (no hyphens).')
@minLength(5)
@maxLength(50)
param name string

@description('Azure region.')
param location string

@description('ACR SKU. Staging is Basic; production should be Premium for geo-replication + private endpoints.')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param sku string = 'Basic'

@description('Enable admin user (username/password auth). Existing staging is true; future hardening will flip to false and use Managed Identity.')
param adminUserEnabled bool = true

@description('Common tags propagated from main.bicep.')
param tags object = {}

// ---------- Container Registry ----------

resource acr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: sku
  }
  properties: {
    adminUserEnabled: adminUserEnabled
    publicNetworkAccess: 'Enabled' // matches existing staging
    zoneRedundancy: 'Disabled'
  }
}

// ---------- Outputs ----------

@description('ACR resource ID.')
output id string = acr.id

@description('Login server hostname (e.g. lankaconnectstaging.azurecr.io). Used by Container App image references.')
output loginServer string = acr.properties.loginServer

@description('ACR name (informational).')
output name string = acr.name
