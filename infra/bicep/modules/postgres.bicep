// =============================================================================
// PostgreSQL Flexible Server + LankaConnectDB database + firewall rule
// =============================================================================
//
// Module 2 of N for Phase A W1.4. Describes the existing
// `lankaconnect-staging-db` Postgres Flexible Server so `what-if` should
// report zero changes once authenticated.
//
// Source-of-truth for current staging config: scripts/azure/provision-staging.sh
//   - SKU: Standard_B1ms (Burstable tier)
//   - Storage: 32GB
//   - Version: 15
//   - Backup retention: 7 days
//   - High availability: Disabled
//   - PgBouncer: configured `on` BUT not available on Burstable tier
//     (the bash script tries and tolerates failure). Bicep mirrors this
//     intent — sets `pgbouncer.enabled` to `on`; Azure ignores on Burstable.
//
// Sensitive: admin password is a @secure parameter; never committed.
// Provide it at deployment time via --parameters administratorLoginPassword=$(...)
// or via Key Vault reference once the key-vault module lands.
// =============================================================================

@description('Name of the Postgres Flexible Server.')
param name string

@description('Azure region.')
param location string

@description('Postgres major version. Staging is on 15.')
@allowed([
  '14'
  '15'
  '16'
])
param version string = '15'

@description('Server admin login.')
param administratorLogin string

@description('Server admin password. Pass at deploy time — never commit.')
@secure()
@minLength(8)
param administratorLoginPassword string

@description('Compute SKU name. Staging is Standard_B1ms (Burstable).')
param skuName string = 'Standard_B1ms'

@description('SKU tier. Staging is Burstable; production will likely be GeneralPurpose.')
@allowed([
  'Burstable'
  'GeneralPurpose'
  'MemoryOptimized'
])
param skuTier string = 'Burstable'

@description('Storage size in GB. Staging is 32GB.')
@minValue(32)
@maxValue(16384)
param storageSizeGB int = 32

@description('Daily backup retention in days. Staging is 7.')
@minValue(7)
@maxValue(35)
param backupRetentionDays int = 7

@description('Application database to create inside the server.')
param databaseName string = 'LankaConnectDB'

@description('Common tags propagated from main.bicep.')
param tags object = {}

// ---------- Flexible Server ----------

resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    version: version
    storage: {
      storageSizeGB: storageSizeGB
      // autoGrow + tier left at API defaults to match existing staging.
    }
    backup: {
      backupRetentionDays: backupRetentionDays
      geoRedundantBackup: 'Disabled' // matches Burstable staging; flip post-cutover.
    }
    highAvailability: {
      mode: 'Disabled' // staging is single-zone; production may flip to ZoneRedundant.
    }
    network: {
      // Public-access flexible server matches current staging shape
      // (firewall rule below allows Azure services).
      publicNetworkAccess: 'Enabled'
    }
  }
}

// ---------- Application database ----------

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: postgres
  name: databaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// ---------- Firewall rule: allow Azure services ----------
// 0.0.0.0 -> 0.0.0.0 is the Azure-services convention (NOT a public open rule).

resource allowAzureServices 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: postgres
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ---------- pgBouncer hint ----------
// The bash provisioner sets `pgbouncer.enabled = on` but Burstable rejects it.
// Bicep `configurations` resource would do the same; intentionally omitted
// in this skeleton so `what-if` doesn't claim a delta on a no-op setting.
// Add when staging moves off Burstable.

// ---------- Outputs ----------

@description('Fully qualified Postgres server hostname (e.g. lankaconnect-staging-db.postgres.database.azure.com).')
output fullyQualifiedDomainName string = postgres.properties.fullyQualifiedDomainName

@description('Server resource ID.')
output serverId string = postgres.id

@description('Application database name (informational).')
output databaseName string = database.name
