// =============================================================================
// Azure Storage Account — blob storage for event images, email assets, business images
// =============================================================================
//
// Describes existing `lankaconnectstrgaccount` storage account.
//
// IMPORTANT: this storage account lives in `eastus` (not `eastus2` like
// the rest of staging). Reason unknown — likely created during an initial
// bootstrap before the rest of staging settled on eastus2. Bicep mirrors
// the existing region; cross-region rebuild would require data migration
// and is out of W1.4 scope.
// =============================================================================

@description('Storage account name. Must be globally unique, lowercase, alphanumeric only, 3-24 chars.')
@minLength(3)
@maxLength(24)
param name string

@description('Azure region. Note: existing staging storage is in eastus (not eastus2).')
param location string = 'eastus'

@description('SKU. Staging is Standard_LRS (locally redundant); production should likely be Standard_ZRS or _GRS.')
@allowed([
  'Standard_LRS'
  'Standard_ZRS'
  'Standard_GRS'
  'Standard_RAGRS'
])
param sku string = 'Standard_LRS'

@description('Storage account kind. Existing is StorageV2.')
param kind string = 'StorageV2'

@description('Default access tier for blob storage.')
@allowed([
  'Hot'
  'Cool'
])
param accessTier string = 'Hot'

// Tags intentionally NOT applied in Bicep (see container-apps-env.bicep note).

// ---------- Storage Account ----------

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: name
  location: location
  sku: {
    name: sku
  }
  kind: kind
  properties: {
    accessTier: accessTier
    allowBlobPublicAccess: true       // matches existing staging — used for email-assets / event-images
    allowSharedKeyAccess: true        // matches existing
    supportsHttpsTrafficOnly: true    // existing has enableHttpsTrafficOnly=true
    minimumTlsVersion: 'TLS1_2'       // matches existing
  }
}

// ---------- Outputs ----------

@description('Storage account resource ID.')
output id string = storageAccount.id

@description('Primary blob endpoint URL — used in EmailBrandingService blob URLs.')
output primaryBlobEndpoint string = storageAccount.properties.primaryEndpoints.blob

@description('Storage account name.')
output name string = storageAccount.name
