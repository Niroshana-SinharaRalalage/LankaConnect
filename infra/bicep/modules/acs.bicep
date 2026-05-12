// =============================================================================
// Azure Communication Services + Email Service + 2 Domains
// =============================================================================
//
// Describes existing ACS + EmailServices setup:
//   - `lankaconnect-communication` (CommunicationServices, global, US data location)
//   - `lankaconnect-email` (EmailServices, global, US data location)
//     - `AzureManagedDomain` (azurecomm.net for testing)
//     - `lankaconnect.app` (custom domain, verified DKIM/SPF/DMARC)
//
// linkedDomains: ACS references both email domains by ID so the API can send
// from either. The reference is bi-directional; both ACS.linkedDomains and
// domain children must coexist for ACS to send mail.
// =============================================================================

@description('Communication Services account name.')
param communicationServiceName string

@description('Email Services name.')
param emailServiceName string

@description('Data location — region where ACS holds customer data. Existing is `unitedstates` (lowercase).')
@allowed([
  'asiapacific'
  'australia'
  'brazil'
  'canada'
  'europe'
  'france'
  'germany'
  'india'
  'japan'
  'korea'
  'norway'
  'switzerland'
  'uae'
  'uk'
  'unitedstates'
])
param dataLocation string = 'unitedstates'

@description('Custom email domain to provision (e.g. lankaconnect.app).')
param customDomainName string

// ---------- Email Service ----------
// Email Services + Domains must exist BEFORE ACS can link them, so declare them first.

resource emailService 'Microsoft.Communication/emailServices@2023-04-01' = {
  name: emailServiceName
  location: 'global'
  properties: {
    dataLocation: dataLocation
  }
}

resource azureManagedDomain 'Microsoft.Communication/emailServices/domains@2023-04-01' = {
  parent: emailService
  name: 'AzureManagedDomain'
  location: 'global'
  properties: {
    domainManagement: 'AzureManaged'
    userEngagementTracking: 'Disabled'
  }
}

resource customDomain 'Microsoft.Communication/emailServices/domains@2023-04-01' = {
  parent: emailService
  name: customDomainName
  location: 'global'
  properties: {
    domainManagement: 'CustomerManaged'
    userEngagementTracking: 'Disabled'
    // DKIM/SPF/DMARC verification records are read-only — Bicep cannot manage
    // verification state. Existing staging shows all 5 records Verified.
  }
}

// ---------- Communication Services (depends on email domains) ----------

resource communicationService 'Microsoft.Communication/communicationServices@2023-04-01' = {
  name: communicationServiceName
  location: 'global'
  properties: {
    dataLocation: dataLocation
    linkedDomains: [
      azureManagedDomain.id
      customDomain.id
    ]
  }
}

// ---------- Outputs ----------

@description('ACS resource ID.')
output communicationServiceId string = communicationService.id

@description('ACS hostname — used in connection strings.')
output communicationServiceHost string = communicationService.properties.hostName

@description('Email Service resource ID.')
output emailServiceId string = emailService.id

@description('Custom domain `fromSenderDomain` — used as the From address suffix.')
output customDomainSenderDomain string = customDomain.properties.fromSenderDomain
