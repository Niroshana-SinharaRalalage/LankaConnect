// =============================================================================
// Container Apps Environment + Log Analytics Workspace
// =============================================================================
//
// Module 1 of N for Phase A W1.4. Illustrative — chosen because it has the
// most outward references from other resource modules (every Container App
// references this env).
//
// Idempotency: existing staging resources should match these names so a
// `what-if` produces zero changes. If `what-if` shows differences, that is
// either drift to investigate OR a parameter to align here.
// =============================================================================

@description('Name of the Container Apps managed environment.')
param name string

@description('Azure region.')
param location string

@description('Name of the Log Analytics workspace backing the env. Created here if it does not exist.')
param logAnalyticsWorkspaceName string

@description('Common tags propagated from main.bicep.')
param tags object = {}

@description('Log Analytics retention in days. 30 = Azure default cheap tier.')
@minValue(30)
@maxValue(730)
param logRetentionInDays int = 30

// ---------- Log Analytics workspace ----------

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: logRetentionInDays
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    workspaceCapping: {
      // dailyQuotaGb: -1 means uncapped; aligns with current staging behavior.
      dailyQuotaGb: -1
    }
  }
}

// ---------- Container Apps managed environment ----------

resource containerAppsEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
    // zoneRedundant: false matches current staging single-zone cheap tier.
    // Flip to true post-cutover (W17+) if needed.
    zoneRedundant: false
  }
}

// ---------- Outputs ----------

@description('Container Apps Environment resource ID.')
output id string = containerAppsEnv.id

@description('Default DNS domain for Container Apps in this env (e.g. politebay-79d6e8a2.eastus2.azurecontainerapps.io).')
output defaultDomain string = containerAppsEnv.properties.defaultDomain

@description('Log Analytics workspace resource ID — wired upstream so other resources can ship logs here.')
output logAnalyticsWorkspaceId string = logAnalytics.id
