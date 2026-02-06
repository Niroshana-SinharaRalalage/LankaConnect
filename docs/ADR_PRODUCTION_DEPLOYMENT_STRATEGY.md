# ADR: Production Deployment Strategy for LankaConnect

**Status:** PROPOSED
**Date:** 2026-01-08
**Decision Makers:** Technical Lead, Operations Team
**Consulted:** Development Team, Security Team
**Informed:** Stakeholders

---

## Executive Summary

This ADR defines the production deployment strategy for LankaConnect, addressing the user's questions about deploying to production from the current staging environment. The recommended approach is **Option 3: Separate Production Environment with Blue-Green Deployment**, which provides the best balance of safety, scalability, and operational excellence.

**Key Recommendation:** Do NOT convert staging to production. Create a dedicated production environment with proper separation, security hardening, and blue-green deployment capability.

---

## Table of Contents

1. [Context and Problem Statement](#1-context-and-problem-statement)
2. [User Questions Analysis](#2-user-questions-analysis)
3. [Decision Drivers](#3-decision-drivers)
4. [Considered Options](#4-considered-options)
5. [Recommended Solution](#5-recommended-solution)
6. [Architecture Design](#6-architecture-design)
7. [Production Resources](#7-production-resources)
8. [Security Considerations](#8-security-considerations)
9. [Deployment Strategy](#9-deployment-strategy)
10. [Configuration Management](#10-configuration-management)
11. [Monitoring and Observability](#11-monitoring-and-observability)
12. [Rollback Strategy](#12-rollback-strategy)
13. [Implementation Plan](#13-implementation-plan)
14. [Pre-Production Checklist](#14-pre-production-checklist)
15. [Cost Analysis](#15-cost-analysis)
16. [Consequences](#16-consequences)

---

## 1. Context and Problem Statement

### Current State

**Staging Environment (Successfully Deployed)**:
- **Frontend**: `lankaconnect-ui-staging` (Next.js on Azure Container Apps)
- **Backend**: `lankaconnect-api-staging` (.NET 8 on Azure Container Apps)
- **Database**: Azure SQL Database (staging data)
- **Container Registry**: `lankaconnectstaging.azurecr.io`
- **Resource Group**: `lankaconnect-staging`
- **Location**: `eastus2`
- **Key Vault**: `lankaconnect-staging-kv`
- **GitHub Workflows**: `deploy-staging.yml`, `deploy-ui-staging.yml` (triggered on `develop` branch)

**Staging URLs**:
- Frontend: `https://lankaconnect-ui-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io`
- Backend: `https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io`

### Problem Statement

The user needs to deploy LankaConnect to production and has two questions:

1. **Can we simply push staging changes to production after creating relevant prod resources?**
2. **Can we convert current staging to production if everything is working fine?**

Both approaches have significant trade-offs that must be evaluated against Azure best practices and operational requirements.

---

## 2. User Questions Analysis

### Question 1: Push Staging to Production (New Resources)

**What This Means:**
- Create new production resources (container apps, database, key vault, etc.)
- Push the same Docker images from staging to production
- Use identical configuration with environment-specific secrets

**Pros:**
- Clean separation of environments
- Staging remains intact for testing
- Follows Azure best practices
- Easy rollback to previous versions
- Proper audit trail

**Cons:**
- Requires creating duplicate infrastructure
- Initial setup complexity
- Higher ongoing costs (2 full environments)

**Verdict:** ✅ **This is the recommended approach** (with enhancements for blue-green deployment)

---

### Question 2: Convert Staging to Production

**What This Means:**
- Rename staging resources to production names
- Change environment variables from "Staging" to "Production"
- Promote staging data to production
- Update DNS/URLs to production domains

**Pros:**
- Minimal initial setup
- Lower costs (single environment)
- Faster time to production

**Cons:**
- ❌ Loss of staging environment for future development
- ❌ No testing environment for changes before production
- ❌ Risk of staging data contamination in production
- ❌ Violates Azure best practices for environment separation
- ❌ Makes rollbacks difficult
- ❌ Breaks CI/CD pipeline (develop branch would deploy to production)
- ❌ Security and compliance risks
- ❌ No isolation for testing breaking changes

**Verdict:** ❌ **NOT RECOMMENDED**

---

## 3. Decision Drivers

### Must-Have Requirements

1. **Zero-Downtime Deployments**: Production must remain available during updates
2. **Environment Separation**: Staging and production must remain isolated
3. **Quick Rollback**: Ability to revert to previous version within 5 minutes
4. **Security Hardening**: Production must have enhanced security controls
5. **Data Integrity**: Production data must never be contaminated by test data
6. **Monitoring**: Comprehensive observability before going live
7. **Cost Efficiency**: Optimize for production workloads without over-provisioning

### Nice-to-Have

1. Blue-Green deployment capability
2. Canary deployments for gradual rollout
3. Automated smoke tests post-deployment
4. Infrastructure as Code (IaC) for reproducibility
5. Multi-region deployment for disaster recovery

---

## 4. Considered Options

### Option 1: Direct Push to New Production Resources

**Description:** Create production resources, push staging images directly.

**Pros:**
- Simple and fast
- Minimal changes to existing workflows
- Clear environment separation

**Cons:**
- No blue-green deployment
- Downtime during initial deployment
- No gradual rollout capability

**Verdict:** Good starting point, but lacks production-grade deployment features.

---

### Option 2: Convert Staging to Production

**Description:** Rename staging to production.

**Pros:**
- Fastest path to production
- No new resources needed

**Cons:**
- Loss of staging environment
- High risk of data contamination
- Violates Azure best practices
- No rollback capability

**Verdict:** ❌ **Rejected** due to operational and security risks.

---

### Option 3: Separate Production with Blue-Green Deployment (RECOMMENDED)

**Description:** Create dedicated production environment with Azure Container Apps revisions for zero-downtime deployment.

**Pros:**
- Zero-downtime deployments
- Instant rollback capability (traffic split)
- Staging remains intact
- Production-grade reliability
- Follows Azure best practices
- Supports gradual rollout (canary)

**Cons:**
- Higher initial complexity
- Requires learning Azure Container Apps revisions
- Slightly higher costs (multiple revisions during deployment)

**Verdict:** ✅ **RECOMMENDED** - Best balance of safety, reliability, and operational excellence.

---

## 5. Recommended Solution

### Option 3: Separate Production Environment with Blue-Green Deployment

**Architecture:**
- **Dedicated Production Resources**: Separate resource group, container apps, database, key vault
- **Blue-Green Deployment**: Use Azure Container Apps revisions and traffic splitting
- **Staging Preservation**: Keep staging environment intact for pre-production testing
- **Gradual Rollout**: Support canary deployments with traffic percentage control
- **Quick Rollback**: Instant traffic switch back to previous revision

**Rationale:**
1. **Safety**: Production changes are tested in staging first, then rolled out gradually
2. **Reliability**: Zero-downtime deployments with instant rollback
3. **Compliance**: Proper environment separation for audit and security
4. **Scalability**: Production can scale independently of staging
5. **Best Practices**: Aligns with Azure Well-Architected Framework

---

## 6. Architecture Design

### 6.1 Environment Topology

```
┌─────────────────────────────────────────────────────────────────┐
│                         GitHub Repository                        │
│  ┌─────────────────┐              ┌─────────────────┐          │
│  │ develop branch  │              │  main branch    │          │
│  │  (feature dev)  │              │  (production)   │          │
│  └────────┬────────┘              └────────┬────────┘          │
│           │                                 │                    │
└───────────┼─────────────────────────────────┼────────────────────┘
            │                                 │
            ▼                                 ▼
   ┌────────────────────┐          ┌────────────────────┐
   │  Staging (eastus2) │          │ Production (eastus)│
   ├────────────────────┤          ├────────────────────┤
   │ lankaconnectstaging│          │ lankaconnectprod   │
   │     .azurecr.io    │          │     .azurecr.io    │
   ├────────────────────┤          ├────────────────────┤
   │ API: develop:tag   │          │ API: main:tag      │
   │ UI:  develop:tag   │          │ UI:  main:tag      │
   ├────────────────────┤          ├────────────────────┤
   │ DB: Test data      │          │ DB: Production data│
   ├────────────────────┤          ├────────────────────┤
   │ Scale: 0.5 CPU     │          │ Scale: 1-10 replicas│
   │        1 GB RAM    │          │        2-4 CPU     │
   └────────────────────┘          └────────────────────┘
```

### 6.2 Blue-Green Deployment Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    Blue-Green Deployment                         │
└─────────────────────────────────────────────────────────────────┘

1. CURRENT STATE (Blue Active)
   ┌─────────────────────────────────────────────┐
   │  Container App: lankaconnect-api-prod       │
   ├─────────────────────────────────────────────┤
   │  Revision: api-prod-rev-001 (100% traffic)  │ ← BLUE (Active)
   │  Image: api:abc123                          │
   └─────────────────────────────────────────────┘

2. DEPLOY NEW VERSION (Green Inactive)
   ┌─────────────────────────────────────────────┐
   │  Container App: lankaconnect-api-prod       │
   ├─────────────────────────────────────────────┤
   │  Revision: api-prod-rev-001 (100% traffic)  │ ← BLUE (Active)
   │  Image: api:abc123                          │
   ├─────────────────────────────────────────────┤
   │  Revision: api-prod-rev-002 (0% traffic)    │ ← GREEN (Testing)
   │  Image: api:def456                          │
   └─────────────────────────────────────────────┘

3. CANARY ROLLOUT (10% Traffic)
   ┌─────────────────────────────────────────────┐
   │  Container App: lankaconnect-api-prod       │
   ├─────────────────────────────────────────────┤
   │  Revision: api-prod-rev-001 (90% traffic)   │ ← BLUE (Active)
   │  Image: api:abc123                          │
   ├─────────────────────────────────────────────┤
   │  Revision: api-prod-rev-002 (10% traffic)   │ ← GREEN (Canary)
   │  Image: api:def456                          │
   └─────────────────────────────────────────────┘

4. FULL CUTOVER (100% Green)
   ┌─────────────────────────────────────────────┐
   │  Container App: lankaconnect-api-prod       │
   ├─────────────────────────────────────────────┤
   │  Revision: api-prod-rev-001 (0% traffic)    │ ← BLUE (Standby)
   │  Image: api:abc123                          │
   ├─────────────────────────────────────────────┤
   │  Revision: api-prod-rev-002 (100% traffic)  │ ← GREEN (Active)
   │  Image: api:def456                          │
   └─────────────────────────────────────────────┘

5. ROLLBACK (If Issues Detected)
   ┌─────────────────────────────────────────────┐
   │  Container App: lankaconnect-api-prod       │
   ├─────────────────────────────────────────────┤
   │  Revision: api-prod-rev-001 (100% traffic)  │ ← BLUE (Restored)
   │  Image: api:abc123                          │
   ├─────────────────────────────────────────────┤
   │  Revision: api-prod-rev-002 (0% traffic)    │ ← GREEN (Failed)
   │  Image: api:def456                          │
   └─────────────────────────────────────────────┘
```

---

## 7. Production Resources

### 7.1 Azure Resources Inventory

| Resource Type | Staging | Production | Differences |
|--------------|---------|------------|-------------|
| **Resource Group** | `lankaconnect-staging` | `lankaconnect-prod` | Separate subscriptions recommended |
| **Container Registry** | `lankaconnectstaging.azurecr.io` | `lankaconnectprod.azurecr.io` | Separate for security isolation |
| **Container App (API)** | `lankaconnect-api-staging` | `lankaconnect-api-prod` | Production: 2-10 replicas, 2-4 CPU |
| **Container App (UI)** | `lankaconnect-ui-staging` | `lankaconnect-ui-prod` | Production: 2-10 replicas, 2-4 CPU |
| **SQL Database** | `lankaconnect-staging-db` | `lankaconnect-prod-db` | Production: P2 tier, geo-replication |
| **Key Vault** | `lankaconnect-staging-kv` | `lankaconnect-prod-kv` | Production: Premium SKU, HSM |
| **Application Insights** | `lankaconnect-staging-insights` | `lankaconnect-prod-insights` | Separate telemetry |
| **Log Analytics** | `lankaconnect-staging-logs` | `lankaconnect-prod-logs` | 90-day retention |
| **Storage Account** | `lankaconnectstaging` | `lankaconnectprod` | Production: GRS, immutable blobs |
| **Azure Email Service** | Shared staging | Production domain | Dedicated sender domain |
| **CDN Profile** | None | `lankaconnect-cdn` | Cloudflare or Azure CDN |
| **Front Door** | None | `lankaconnect-fd` | WAF, SSL termination |
| **Service Principal** | `lankaconnect-staging-sp` | `lankaconnect-prod-sp` | Separate RBAC |

### 7.2 Naming Conventions

**Pattern**: `lankaconnect-{service}-{environment}-{region}`

**Examples**:
- Container App API: `lankaconnect-api-prod-eastus`
- Container App UI: `lankaconnect-ui-prod-eastus`
- Database: `lankaconnect-sqldb-prod-eastus`
- Key Vault: `lankaconnect-kv-prod-eastus` (15-char limit)
- Storage: `lankaconnectprodeus` (24-char limit, no hyphens)

---

## 8. Security Considerations

### 8.1 Production-Specific Security Enhancements

| Security Control | Staging | Production |
|-----------------|---------|------------|
| **Key Vault SKU** | Standard | Premium (HSM-backed) |
| **Network Access** | Public (IP restrictions) | Private endpoints + VNet |
| **SQL Firewall** | Allow Azure services | Private endpoint only |
| **TLS Version** | TLS 1.2 | TLS 1.3 |
| **Managed Identity** | System-assigned | User-assigned (auditable) |
| **RBAC** | Contributor | Least privilege (Reader + specific actions) |
| **Secrets Rotation** | Manual | Automated (30-day rotation) |
| **DDoS Protection** | Basic | Standard |
| **WAF** | None | Azure Front Door WAF |
| **Certificate Management** | Let's Encrypt | Azure managed certificates |
| **Audit Logging** | 7 days | 90 days (compliance requirement) |
| **Backup Retention** | 7 days | 35 days (weekly backups retained 52 weeks) |

### 8.2 Service Principal Configuration

**Staging Service Principal** (Current):
```json
{
  "clientId": "AZURE_CLIENT_ID",
  "clientSecret": "AZURE_CLIENT_SECRET",
  "subscriptionId": "AZURE_SUBSCRIPTION_ID",
  "tenantId": "AZURE_TENANT_ID",
  "permissions": ["Contributor"]
}
```

**Production Service Principal** (Recommended):
```json
{
  "clientId": "AZURE_PROD_CLIENT_ID",
  "clientSecret": "AZURE_PROD_CLIENT_SECRET",
  "subscriptionId": "AZURE_PROD_SUBSCRIPTION_ID",
  "tenantId": "AZURE_TENANT_ID",
  "permissions": [
    "AcrPush",
    "ContainerAppContributor",
    "KeyVaultSecretsUser",
    "SqlDbContributor",
    "StorageBlobDataContributor"
  ]
}
```

**Rationale**: Least privilege principle - production service principal only has permissions for deployment, not full Contributor access.

### 8.3 GitHub Secrets for Production

**New Secrets Required**:
- `AZURE_CREDENTIALS_PROD`
- `ACR_USERNAME_PROD`
- `ACR_PASSWORD_PROD`
- `AZURE_PROD_SUBSCRIPTION_ID`
- `PROD_DATABASE_CONNECTION_STRING` (for migration verification only)

---

## 9. Deployment Strategy

### 9.1 Blue-Green Deployment with Azure Container Apps

**How It Works:**

Azure Container Apps natively supports multiple **revisions** of the same app. Each deployment creates a new revision, and traffic can be split between revisions.

**Advantages:**
1. **Zero Downtime**: New version is deployed alongside old version
2. **Instant Rollback**: Change traffic split from 100% new → 100% old in seconds
3. **Canary Testing**: Gradually increase traffic to new version (10% → 50% → 100%)
4. **A/B Testing**: Run two versions simultaneously for experimentation

**Disadvantages:**
1. **Database Migrations**: Requires backward-compatible schema changes
2. **Cost**: Both revisions run simultaneously during transition (temporary)
3. **Complexity**: Need to manage revision lifecycle

### 9.2 Deployment Steps (Blue-Green)

**Phase 1: Deploy New Revision (Green)**
```bash
# Deploy new revision with 0% traffic
az containerapp update \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --image lankaconnectprod.azurecr.io/lankaconnect-api:${GITHUB_SHA} \
  --revision-suffix ${GITHUB_SHA:0:7} \
  --set-env-vars ASPNETCORE_ENVIRONMENT=Production

# New revision gets 0% traffic automatically
```

**Phase 2: Smoke Test (Green Revision)**
```bash
# Get the new revision URL (each revision has a unique URL)
GREEN_URL=$(az containerapp revision show \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --revision lankaconnect-api-prod--${GITHUB_SHA:0:7} \
  --query properties.fqdn -o tsv)

# Test health endpoint
curl --fail https://${GREEN_URL}/health
```

**Phase 3: Canary Rollout (10% Traffic)**
```bash
# Send 10% of traffic to new revision
az containerapp ingress traffic set \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --revision-weight \
    lankaconnect-api-prod--abc123=90 \
    lankaconnect-api-prod--${GITHUB_SHA:0:7}=10
```

**Phase 4: Monitor Metrics (5 minutes)**
```bash
# Check error rates, latency, and health
# If metrics look good, proceed to full cutover
# If issues detected, rollback immediately
```

**Phase 5: Full Cutover (100% Traffic)**
```bash
# Send 100% traffic to new revision
az containerapp ingress traffic set \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --revision-weight \
    lankaconnect-api-prod--${GITHUB_SHA:0:7}=100

# Deactivate old revision (keeps it for potential rollback)
az containerapp revision deactivate \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --revision lankaconnect-api-prod--abc123
```

**Phase 6: Cleanup Old Revisions (After 24 hours)**
```bash
# Delete old revisions after confirming stability
az containerapp revision list \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --query "[?properties.trafficWeight==0]" \
  --output table

# Delete specific revision
az containerapp revision delete \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --revision lankaconnect-api-prod--abc123
```

### 9.3 Rollback Strategy

**Instant Rollback (Within 30 seconds)**:
```bash
# Immediately shift 100% traffic back to previous revision
az containerapp ingress traffic set \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --revision-weight \
    lankaconnect-api-prod--abc123=100 \
    lankaconnect-api-prod--${GITHUB_SHA:0:7}=0

# Deactivate failed revision
az containerapp revision deactivate \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --revision lankaconnect-api-prod--${GITHUB_SHA:0:7}
```

**Database Rollback (If Migration Issues)**:
```bash
# Restore database from automated backup
az sql db restore \
  --resource-group lankaconnect-prod \
  --server lankaconnect-sqldb-prod \
  --name LankaConnectDB \
  --dest-name LankaConnectDB-restored \
  --time "2026-01-08T10:30:00Z"
```

---

## 10. Configuration Management

### 10.1 Environment-Specific Configuration

**Staging Configuration** (`appsettings.Staging.json`):
```json
{
  "Serilog": {
    "MinimumLevel": { "Default": "Information" }
  },
  "ApplicationUrls": {
    "FrontendBaseUrl": "http://localhost:3000"
  },
  "EmailSettings": {
    "SenderName": "LankaConnect Staging"
  },
  "Kestrel": {
    "Endpoints": { "Http": { "Url": "http://+:5000" } }
  }
}
```

**Production Configuration** (`appsettings.Production.json`):
```json
{
  "Serilog": {
    "MinimumLevel": { "Default": "Warning" },
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "/var/log/lankaconnect/app-.log" } }
    ]
  },
  "ApplicationUrls": {
    "FrontendBaseUrl": "https://lankaconnect.com"
  },
  "EmailSettings": {
    "SenderName": "LankaConnect"
  },
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://+:5000" },
      "Https": { "Url": "https://+:5001" }
    }
  }
}
```

### 10.2 Key Vault Secrets (Production)

**Secrets to Create in Production Key Vault**:

| Secret Name | Example Value | Description |
|------------|---------------|-------------|
| `database-connection-string` | `Server=tcp:lankaconnect-prod.database.windows.net;Database=LankaConnectDB;...` | Production SQL connection |
| `jwt-secret-key` | `<64-char random string>` | Different from staging |
| `jwt-issuer` | `https://lankaconnect.com` | Production domain |
| `jwt-audience` | `https://lankaconnect.com` | Production domain |
| `entra-enabled` | `true` | Microsoft Entra authentication |
| `entra-tenant-id` | `<Azure AD tenant ID>` | Production tenant |
| `entra-client-id` | `<Azure AD app ID>` | Production app registration |
| `entra-audience` | `api://lankaconnect-prod` | Production API audience |
| `azure-email-connection-string` | `endpoint=https://...;accesskey=...` | Production email service |
| `azure-email-sender-address` | `noreply@lankaconnect.com` | Production sender |
| `azure-storage-connection-string` | `DefaultEndpointsProtocol=https;...` | Production blob storage |
| `stripe-secret-key` | `sk_live_...` | **LIVE** Stripe key (not test!) |
| `stripe-publishable-key` | `pk_live_...` | **LIVE** Stripe key |
| `stripe-webhook-secret` | `whsec_...` | Production webhook secret |

**⚠️ CRITICAL: Production Stripe Keys**

- Staging uses `sk_test_...` (test mode)
- Production MUST use `sk_live_...` (live mode)
- Test keys will NOT charge real credit cards
- Live keys WILL charge real credit cards
- Webhook secrets differ between test and live mode

---

## 11. Monitoring and Observability

### 11.1 Required Monitoring Before Production

**Application Insights Metrics**:
- Request rate (requests/sec)
- Response time (95th percentile < 500ms)
- Error rate (< 0.1%)
- Dependency failures (database, storage, email)
- Custom events (event creation, registration, payment)

**Container Apps Metrics**:
- CPU usage (< 70% average)
- Memory usage (< 80% average)
- Replica count (auto-scaling 2-10)
- Request queue depth (< 10)
- Revision health (all revisions healthy)

**Database Metrics**:
- DTU usage (< 80% average)
- Connection count (< 100 concurrent)
- Query performance (slow query alerts > 5 seconds)
- Deadlock detection
- Failed login attempts

**Availability Tests**:
- Health endpoint (`/health`) - every 5 minutes from 5 regions
- Login endpoint (`/api/auth/login/entra`) - every 15 minutes
- Event creation endpoint - every 30 minutes

### 11.2 Alert Configuration

**Critical Alerts (PagerDuty/Email)**:
- API health check failure (3 consecutive failures)
- Database connectivity failure
- Error rate > 1% for 5 minutes
- Response time > 2 seconds (95th percentile)
- No successful deployments in 1 hour

**Warning Alerts (Email)**:
- CPU > 80% for 10 minutes
- Memory > 90% for 10 minutes
- Slow query detected (> 5 seconds)
- Stripe webhook failure
- Email send failure rate > 5%

### 11.3 Dashboard Requirements

**Production Dashboard** (Azure Portal or Grafana):
1. **Overview Panel**: Health status, request rate, error rate
2. **Performance Panel**: Response time, CPU, memory
3. **Business Metrics**: Events created, registrations, payments
4. **Error Panel**: Top errors, failed requests, exception traces
5. **Database Panel**: Connection pool, query performance, deadlocks

---

## 12. Rollback Strategy

### 12.1 Rollback Scenarios and Procedures

**Scenario 1: Application Bug Detected in Production**

**Detection Time**: 0-5 minutes after deployment
**Rollback Time**: 30 seconds

```bash
# Instant traffic switch to previous revision
az containerapp ingress traffic set \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --revision-weight <previous-revision>=100 <new-revision>=0
```

**Scenario 2: Database Migration Breaks Application**

**Detection Time**: 1-10 minutes
**Rollback Time**: 5-15 minutes (depending on database size)

```bash
# 1. Stop all traffic to new revision
az containerapp ingress traffic set \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --revision-weight <previous-revision>=100

# 2. Restore database from backup
az sql db restore \
  --resource-group lankaconnect-prod \
  --server lankaconnect-sqldb-prod \
  --name LankaConnectDB \
  --dest-name LankaConnectDB-restored \
  --time "<timestamp-before-migration>"

# 3. Update connection string to restored database
az keyvault secret set \
  --vault-name lankaconnect-prod-kv \
  --name database-connection-string \
  --value "<restored-db-connection-string>"

# 4. Restart container app to pick up new connection string
az containerapp revision restart \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --revision <previous-revision>
```

**Scenario 3: Third-Party Service Failure (Stripe, Email)**

**Detection Time**: Immediate (alert triggered)
**Rollback Time**: N/A (graceful degradation)

```bash
# Option 1: Disable feature flag
az containerapp update \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --set-env-vars STRIPE_ENABLED=false

# Option 2: Fallback to backup service
az containerapp update \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --set-env-vars EMAIL_PROVIDER=SMTP
```

---

## 13. Implementation Plan

### 13.1 Phase 1: Pre-Production Setup (2-3 days)

**Day 1: Infrastructure Provisioning**

```bash
# Step 1: Create production resource group
az group create \
  --name lankaconnect-prod \
  --location eastus \
  --tags Environment=Production Project=LankaConnect

# Step 2: Create production container registry
az acr create \
  --resource-group lankaconnect-prod \
  --name lankaconnectprod \
  --sku Standard \
  --admin-enabled true

# Step 3: Create production Key Vault
az keyvault create \
  --resource-group lankaconnect-prod \
  --name lankaconnect-prod-kv \
  --location eastus \
  --sku premium \
  --enable-rbac-authorization true \
  --enabled-for-deployment true

# Step 4: Create production SQL Database
az sql server create \
  --resource-group lankaconnect-prod \
  --name lankaconnect-sqldb-prod \
  --location eastus \
  --admin-user sqladmin \
  --admin-password '<strong-password>'

az sql db create \
  --resource-group lankaconnect-prod \
  --server lankaconnect-sqldb-prod \
  --name LankaConnectDB \
  --service-objective P2 \
  --backup-storage-redundancy Geo \
  --zone-redundant true

# Step 5: Create production Storage Account
az storage account create \
  --resource-group lankaconnect-prod \
  --name lankaconnectprodeus \
  --location eastus \
  --sku Standard_GRS \
  --kind StorageV2 \
  --access-tier Hot \
  --enable-hierarchical-namespace false

# Step 6: Create production Application Insights
az monitor app-insights component create \
  --resource-group lankaconnect-prod \
  --app lankaconnect-prod-insights \
  --location eastus \
  --application-type web

# Step 7: Create production Log Analytics workspace
az monitor log-analytics workspace create \
  --resource-group lankaconnect-prod \
  --workspace-name lankaconnect-prod-logs \
  --location eastus \
  --retention-time 90
```

**Day 2: Container Apps and Networking**

```bash
# Step 8: Create Container Apps environment
az containerapp env create \
  --resource-group lankaconnect-prod \
  --name lankaconnect-prod-env \
  --location eastus \
  --logs-workspace-id <log-analytics-workspace-id> \
  --logs-workspace-key <log-analytics-workspace-key>

# Step 9: Create API Container App
az containerapp create \
  --resource-group lankaconnect-prod \
  --name lankaconnect-api-prod \
  --environment lankaconnect-prod-env \
  --image lankaconnectprod.azurecr.io/lankaconnect-api:latest \
  --target-port 5000 \
  --ingress external \
  --min-replicas 2 \
  --max-replicas 10 \
  --cpu 2.0 \
  --memory 4.0Gi \
  --registry-server lankaconnectprod.azurecr.io \
  --registry-username <acr-username> \
  --registry-password <acr-password> \
  --secrets \
    database-connection-string=<value> \
    jwt-secret-key=<value> \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection=secretref:database-connection-string \
    Jwt__Key=secretref:jwt-secret-key

# Step 10: Create UI Container App
az containerapp create \
  --resource-group lankaconnect-prod \
  --name lankaconnect-ui-prod \
  --environment lankaconnect-prod-env \
  --image lankaconnectprod.azurecr.io/lankaconnect-ui:latest \
  --target-port 3000 \
  --ingress external \
  --min-replicas 2 \
  --max-replicas 10 \
  --cpu 1.0 \
  --memory 2.0Gi \
  --env-vars \
    BACKEND_API_URL=https://<api-fqdn>/api \
    NEXT_PUBLIC_API_URL=/api/proxy \
    NEXT_PUBLIC_ENV=production \
    NODE_ENV=production
```

**Day 3: Security and Secrets**

```bash
# Step 11: Configure Key Vault secrets (repeat for all secrets)
az keyvault secret set \
  --vault-name lankaconnect-prod-kv \
  --name database-connection-string \
  --value "<connection-string>"

# Step 12: Configure managed identity for Container Apps
az containerapp identity assign \
  --resource-group lankaconnect-prod \
  --name lankaconnect-api-prod \
  --system-assigned

# Grant Key Vault access to managed identity
IDENTITY_ID=$(az containerapp identity show \
  --resource-group lankaconnect-prod \
  --name lankaconnect-api-prod \
  --query principalId -o tsv)

az keyvault set-policy \
  --name lankaconnect-prod-kv \
  --object-id $IDENTITY_ID \
  --secret-permissions get list

# Step 13: Configure service principal for GitHub Actions
az ad sp create-for-rbac \
  --name lankaconnect-prod-github \
  --role "AcrPush" \
  --scopes /subscriptions/<subscription-id>/resourceGroups/lankaconnect-prod \
  --sdk-auth

# Output: Copy this JSON to GitHub Secrets as AZURE_CREDENTIALS_PROD
```

### 13.2 Phase 2: Database Migration (1 day)

```bash
# Step 14: Export staging database (optional - for initial production data)
# Only do this if you want to seed production with staging data
# Otherwise, start with empty production database

# Export staging database
az sql db export \
  --resource-group lankaconnect-staging \
  --server lankaconnect-staging-db \
  --name LankaConnectDB \
  --admin-user sqladmin \
  --admin-password '<password>' \
  --storage-key-type StorageAccessKey \
  --storage-key '<storage-account-key>' \
  --storage-uri 'https://lankaconnectstaging.blob.core.windows.net/backups/staging-db-export.bacpac'

# Import to production (if desired)
az sql db import \
  --resource-group lankaconnect-prod \
  --server lankaconnect-sqldb-prod \
  --name LankaConnectDB \
  --admin-user sqladmin \
  --admin-password '<password>' \
  --storage-key-type StorageAccessKey \
  --storage-key '<storage-account-key>' \
  --storage-uri 'https://lankaconnectstaging.blob.core.windows.net/backups/staging-db-export.bacpac'

# Step 15: Run EF Core migrations on production database
dotnet ef database update \
  --connection "<production-connection-string>" \
  --project src/LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj \
  --context AppDbContext \
  --verbose
```

**⚠️ WARNING: Database Migration Strategy**

**Option A: Fresh Production Database (RECOMMENDED)**
- Start with empty database
- Run EF Core migrations to create schema
- No staging data contamination
- Users start from scratch (post-launch signups)

**Option B: Copy Staging to Production (NOT RECOMMENDED)**
- Only if staging contains critical reference data
- Must scrub all PII/test data first
- Risk of test data in production
- Requires data validation scripts

### 13.3 Phase 3: CI/CD Pipeline Setup (1 day)

**Step 16: Create production deployment workflows**

Create `.github/workflows/deploy-production.yml`:

```yaml
name: Deploy to Azure Production

on:
  push:
    branches:
      - main  # Production deploys from main branch only
  workflow_dispatch:

env:
  AZURE_CONTAINER_REGISTRY: lankaconnectprod
  CONTAINER_APP_NAME: lankaconnect-api-prod
  RESOURCE_GROUP: lankaconnect-prod
  KEY_VAULT_NAME: lankaconnect-prod-kv
  DOTNET_VERSION: '8.0.x'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore and build
        run: |
          dotnet restore
          dotnet build -c Release --no-restore

      - name: Run unit tests (MANDATORY - must pass)
        run: |
          dotnet test tests/LankaConnect.Application.Tests/LankaConnect.Application.Tests.csproj \
            --no-restore \
            --verbosity normal
          echo "✅ All tests passed - Zero Tolerance enforced"

      - name: Azure Login
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS_PROD }}

      - name: Login to Azure Container Registry
        uses: azure/docker-login@v1
        with:
          login-server: ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io
          username: ${{ secrets.ACR_USERNAME_PROD }}
          password: ${{ secrets.ACR_PASSWORD_PROD }}

      - name: Publish application
        run: |
          dotnet publish src/LankaConnect.API/LankaConnect.API.csproj \
            -c Release \
            -o src/LankaConnect.API/publish \
            --no-restore

      - name: Build and push Docker image
        run: |
          docker build \
            -t ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/lankaconnect-api:${{ github.sha }} \
            -t ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/lankaconnect-api:latest \
            -f src/LankaConnect.API/Dockerfile.simple .
          docker push ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/lankaconnect-api:${{ github.sha }}
          docker push ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/lankaconnect-api:latest

      - name: Run database migrations
        run: |
          dotnet tool install -g dotnet-ef --version 8.0.0 2>/dev/null || dotnet tool update -g dotnet-ef --version 8.0.0

          DB_CONNECTION=$(az keyvault secret show \
            --vault-name ${{ env.KEY_VAULT_NAME }} \
            --name database-connection-string \
            --query value -o tsv)

          cd src/LankaConnect.API
          dotnet ef database update \
            --connection "$DB_CONNECTION" \
            --project ../LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj \
            --context AppDbContext \
            --verbose

      # BLUE-GREEN DEPLOYMENT STARTS HERE
      - name: Deploy new revision (0% traffic)
        run: |
          REVISION_SUFFIX=$(echo "${{ github.sha }}" | cut -c1-7)

          az containerapp update \
            --name ${{ env.CONTAINER_APP_NAME }} \
            --resource-group ${{ env.RESOURCE_GROUP }} \
            --image ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/lankaconnect-api:${{ github.sha }} \
            --revision-suffix $REVISION_SUFFIX \
            --replace-env-vars \
              ASPNETCORE_ENVIRONMENT=Production \
              ConnectionStrings__DefaultConnection=secretref:database-connection-string \
              Jwt__Key=secretref:jwt-secret-key \
              # ... (all other environment variables)

          echo "REVISION_SUFFIX=$REVISION_SUFFIX" >> $GITHUB_ENV

      - name: Wait for new revision to become healthy
        run: |
          echo "Waiting for revision to become healthy..."
          sleep 30

          REVISION_NAME="${{ env.CONTAINER_APP_NAME }}--${{ env.REVISION_SUFFIX }}"

          for i in {1..10}; do
            HEALTH_STATE=$(az containerapp revision show \
              --name ${{ env.CONTAINER_APP_NAME }} \
              --resource-group ${{ env.RESOURCE_GROUP }} \
              --revision $REVISION_NAME \
              --query properties.healthState -o tsv)

            if [ "$HEALTH_STATE" == "Healthy" ]; then
              echo "✅ Revision is healthy"
              break
            fi

            if [ $i -eq 10 ]; then
              echo "❌ Revision did not become healthy"
              exit 1
            fi

            echo "⏳ Waiting for healthy state... ($i/10)"
            sleep 10
          done

      - name: Smoke test new revision
        run: |
          REVISION_NAME="${{ env.CONTAINER_APP_NAME }}--${{ env.REVISION_SUFFIX }}"

          REVISION_URL=$(az containerapp revision show \
            --name ${{ env.CONTAINER_APP_NAME }} \
            --resource-group ${{ env.RESOURCE_GROUP }} \
            --revision $REVISION_NAME \
            --query properties.fqdn -o tsv)

          echo "Testing new revision at: https://${REVISION_URL}"

          curl --fail --retry 3 --retry-delay 5 https://${REVISION_URL}/health || exit 1
          echo "✅ Smoke test passed"

      - name: Canary deployment (10% traffic)
        run: |
          REVISION_NAME="${{ env.CONTAINER_APP_NAME }}--${{ env.REVISION_SUFFIX }}"

          # Get current active revision
          OLD_REVISION=$(az containerapp revision list \
            --name ${{ env.CONTAINER_APP_NAME }} \
            --resource-group ${{ env.RESOURCE_GROUP }} \
            --query "[?properties.trafficWeight>0].name" -o tsv)

          echo "Shifting 10% traffic to new revision"
          az containerapp ingress traffic set \
            --name ${{ env.CONTAINER_APP_NAME }} \
            --resource-group ${{ env.RESOURCE_GROUP }} \
            --revision-weight \
              $OLD_REVISION=90 \
              $REVISION_NAME=10

          echo "OLD_REVISION=$OLD_REVISION" >> $GITHUB_ENV

      - name: Monitor canary (5 minutes)
        run: |
          echo "⏳ Monitoring canary deployment for 5 minutes..."
          echo "Check Application Insights for errors"
          sleep 300

      - name: Full cutover (100% traffic)
        run: |
          REVISION_NAME="${{ env.CONTAINER_APP_NAME }}--${{ env.REVISION_SUFFIX }}"

          echo "Shifting 100% traffic to new revision"
          az containerapp ingress traffic set \
            --name ${{ env.CONTAINER_APP_NAME }} \
            --resource-group ${{ env.RESOURCE_GROUP }} \
            --revision-weight $REVISION_NAME=100

      - name: Deactivate old revision
        run: |
          echo "Deactivating old revision (kept for rollback)"
          az containerapp revision deactivate \
            --name ${{ env.CONTAINER_APP_NAME }} \
            --resource-group ${{ env.RESOURCE_GROUP }} \
            --revision ${{ env.OLD_REVISION }}

      - name: Get production URL
        id: get-url
        run: |
          URL=$(az containerapp show \
            --name ${{ env.CONTAINER_APP_NAME }} \
            --resource-group ${{ env.RESOURCE_GROUP }} \
            --query properties.configuration.ingress.fqdn -o tsv)
          echo "PROD_URL=https://$URL" >> $GITHUB_OUTPUT

      - name: Final smoke test
        run: |
          curl --fail --retry 3 --retry-delay 5 ${{ steps.get-url.outputs.PROD_URL }}/health || exit 1
          echo "✅ Production deployment successful"

      - name: Deployment summary
        run: |
          echo "════════════════════════════════════════"
          echo "✅ PRODUCTION DEPLOYMENT SUCCESSFUL"
          echo "════════════════════════════════════════"
          echo "URL: ${{ steps.get-url.outputs.PROD_URL }}"
          echo "Revision: ${{ env.REVISION_SUFFIX }}"
          echo "Commit: ${{ github.sha }}"
          echo "Deployed by: ${{ github.actor }}"

      - name: Rollback on failure
        if: failure()
        run: |
          echo "❌ Deployment failed - rolling back"
          az containerapp ingress traffic set \
            --name ${{ env.CONTAINER_APP_NAME }} \
            --resource-group ${{ env.RESOURCE_GROUP }} \
            --revision-weight ${{ env.OLD_REVISION }}=100
```

Create `.github/workflows/deploy-ui-production.yml` (similar structure for UI)

### 13.4 Phase 4: Testing and Validation (2-3 days)

**Day 1: Functional Testing**

```bash
# Test 1: Health endpoints
curl https://lankaconnect-api-prod.<region>.azurecontainerapps.io/health
curl https://lankaconnect-ui-prod.<region>.azurecontainerapps.io/api/health

# Test 2: Authentication flow
curl -X POST https://lankaconnect-api-prod.<region>.azurecontainerapps.io/api/auth/login/entra \
  -H "Content-Type: application/json" \
  -d '{"accessToken":"<valid-token>","ipAddress":"127.0.0.1"}'

# Test 3: Event creation (requires auth token)
curl -X POST https://lankaconnect-api-prod.<region>.azurecontainerapps.io/api/events \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d @test-event.json

# Test 4: Stripe payment flow (LIVE MODE - use test card)
# Visit UI and complete payment with test card 4242 4242 4242 4242
```

**Day 2: Performance Testing**

```bash
# Load test with Apache Bench
ab -n 1000 -c 10 https://lankaconnect-api-prod.<region>.azurecontainerapps.io/health

# Expected results:
# - Requests per second: > 100
# - Mean response time: < 100ms
# - 99th percentile: < 500ms
```

**Day 3: Security Testing**

```bash
# Test 1: SQL injection protection
curl "https://lankaconnect-api-prod.<region>.azurecontainerapps.io/api/events?search='; DROP TABLE Events;--"
# Expected: 400 Bad Request or sanitized query

# Test 2: XSS protection
curl "https://lankaconnect-api-prod.<region>.azurecontainerapps.io/api/events?search=<script>alert('xss')</script>"
# Expected: Escaped or sanitized

# Test 3: Rate limiting
for i in {1..100}; do
  curl https://lankaconnect-api-prod.<region>.azurecontainerapps.io/api/events
done
# Expected: 429 Too Many Requests after threshold
```

### 13.5 Phase 5: Go-Live (1 day)

**Pre-Go-Live Checklist** (See Section 14)

**Go-Live Steps**:

1. **DNS Cutover**:
```bash
# Update DNS records to point to production
# A record: lankaconnect.com → <Container App IP>
# CNAME: www.lankaconnect.com → lankaconnect-ui-prod.<region>.azurecontainerapps.io
```

2. **SSL Certificate**:
```bash
# Configure custom domain and SSL
az containerapp hostname add \
  --resource-group lankaconnect-prod \
  --name lankaconnect-ui-prod \
  --hostname lankaconnect.com

az containerapp hostname bind \
  --resource-group lankaconnect-prod \
  --name lankaconnect-ui-prod \
  --hostname lankaconnect.com \
  --environment lankaconnect-prod-env \
  --validation-method HTTP
```

3. **Monitoring Alerts**:
```bash
# Enable all critical alerts
az monitor metrics alert create \
  --resource-group lankaconnect-prod \
  --name prod-api-health-alert \
  --scopes /subscriptions/<sub-id>/resourceGroups/lankaconnect-prod/providers/Microsoft.App/containerApps/lankaconnect-api-prod \
  --condition "avg Percentage CPU > 80" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action-group <action-group-id>
```

4. **Communication**:
- Notify stakeholders of go-live
- Post announcement on social media
- Enable customer support channels

---

## 14. Pre-Production Checklist

### 14.1 Infrastructure Readiness

- [ ] Production resource group created
- [ ] Container registry configured with geo-replication
- [ ] SQL Database created with P2 tier and geo-redundancy
- [ ] Key Vault created with Premium SKU
- [ ] Storage Account created with GRS
- [ ] Application Insights configured
- [ ] Log Analytics workspace created (90-day retention)
- [ ] Container Apps environment created
- [ ] API Container App created with 2-10 replicas
- [ ] UI Container App created with 2-10 replicas

### 14.2 Security Checklist

- [ ] All secrets stored in Key Vault (no hardcoded secrets)
- [ ] Managed identity configured for Container Apps
- [ ] Service principal configured with least privilege
- [ ] SQL firewall configured (private endpoint only)
- [ ] Storage account firewall configured
- [ ] TLS 1.3 enforced on all endpoints
- [ ] CORS configured correctly for production domain
- [ ] Stripe webhook signature verification enabled
- [ ] JWT secret key different from staging
- [ ] Database connection string uses managed identity (if possible)

### 14.3 Database Checklist

- [ ] Production database created
- [ ] All EF Core migrations applied successfully
- [ ] Database firewall rules configured
- [ ] Automated backups enabled (35-day retention)
- [ ] Geo-replication configured
- [ ] Read replica created (optional, for reporting)
- [ ] Connection pooling configured in connection string
- [ ] Slow query alerts configured

### 14.4 Application Checklist

- [ ] Production Docker images built and pushed to ACR
- [ ] Environment variables configured correctly
- [ ] `appsettings.Production.json` validated
- [ ] Logging level set to Warning/Error (not Debug)
- [ ] Health check endpoint responding
- [ ] Authentication flow tested with live Entra ID
- [ ] Stripe integration tested in LIVE mode (with test card)
- [ ] Email sending tested with production SMTP/Azure Email
- [ ] File upload tested with production blob storage

### 14.5 Monitoring Checklist

- [ ] Application Insights instrumentation key configured
- [ ] Custom metrics and events instrumented
- [ ] Availability tests configured (5 regions)
- [ ] Critical alerts configured (health, error rate, latency)
- [ ] Warning alerts configured (CPU, memory, slow queries)
- [ ] Dashboard created for production monitoring
- [ ] Log queries saved for common troubleshooting scenarios
- [ ] PagerDuty/notification channels configured

### 14.6 CI/CD Checklist

- [ ] Production GitHub workflow created
- [ ] GitHub secrets configured (AZURE_CREDENTIALS_PROD, ACR credentials)
- [ ] Workflow triggers on `main` branch only
- [ ] Unit tests must pass before deployment
- [ ] Database migrations run automatically
- [ ] Blue-green deployment steps validated
- [ ] Smoke tests configured
- [ ] Rollback procedure tested
- [ ] Manual approval step for production (optional)

### 14.7 Domain and SSL Checklist

- [ ] Custom domain purchased (lankaconnect.com)
- [ ] DNS records configured
- [ ] SSL certificate provisioned (Azure managed or Let's Encrypt)
- [ ] Custom domain bound to Container Apps
- [ ] HTTPS redirect enabled
- [ ] HSTS header configured

### 14.8 Compliance and Legal Checklist

- [ ] Privacy policy published
- [ ] Terms of service published
- [ ] GDPR compliance verified (if EU users)
- [ ] Data retention policy documented
- [ ] User consent flows implemented (cookies, emails)
- [ ] DMCA/content moderation policy documented

### 14.9 Business Readiness Checklist

- [ ] Stripe account activated in LIVE mode
- [ ] Payment webhooks tested in production
- [ ] Email templates reviewed and approved
- [ ] Customer support channels established
- [ ] Incident response plan documented
- [ ] Disaster recovery plan documented
- [ ] Go-live communication plan finalized

---

## 15. Cost Analysis

### 15.1 Staging vs Production Cost Comparison

| Resource | Staging Monthly Cost | Production Monthly Cost | Notes |
|----------|---------------------|------------------------|-------|
| **Container Registry** | $5 (Standard) | $20 (Premium) | Geo-replication, 500 GB storage |
| **Container App (API)** | $30 (0.5 vCPU, 1 GB, 1 replica) | $300 (2 vCPU, 4 GB, 2-10 replicas) | Auto-scaling based on load |
| **Container App (UI)** | $15 (0.25 vCPU, 0.5 GB, 1 replica) | $150 (1 vCPU, 2 GB, 2-10 replicas) | Auto-scaling based on load |
| **SQL Database** | $50 (S3 tier, 100 DTU) | $300 (P2 tier, 250 DTU, geo-redundant) | Includes automated backups |
| **Key Vault** | $5 (Standard) | $25 (Premium, HSM-backed) | 10,000 transactions included |
| **Storage Account** | $5 (LRS, 100 GB) | $20 (GRS, 500 GB) | Includes blob storage for media |
| **Application Insights** | $10 (5 GB ingestion) | $100 (50 GB ingestion) | Includes 90-day retention |
| **Log Analytics** | $5 (1 GB ingestion) | $50 (10 GB ingestion) | 90-day retention |
| **Azure Email Service** | $5 (1,000 emails) | $50 (10,000 emails) | Pay-as-you-go pricing |
| **CDN (Cloudflare)** | $0 (not used) | $20 (Pro plan) | Optional, for static assets |
| **Azure Front Door** | $0 (not used) | $100 (Standard tier, WAF) | Optional, for DDoS protection |
| **Backup Storage** | $5 (7-day retention) | $30 (35-day retention) | Database backups |
| **Total** | **$135/month** | **$1,165/month** | Without CDN/Front Door: $1,045 |

### 15.2 Cost Optimization Strategies

**Development Phase (Months 1-3)**:
- Use staging environment only
- Scale down Container Apps to minimum (0.5 vCPU)
- Use S3 SQL tier instead of P2
- No geo-replication needed
- **Estimated Cost**: $135/month

**Launch Phase (Months 4-6)**:
- Provision production with moderate scaling (2-4 replicas)
- Use P1 SQL tier (can upgrade later)
- Enable geo-replication
- **Estimated Cost**: $700/month

**Growth Phase (Months 7-12)**:
- Scale up to P2 SQL tier
- Increase Container App replicas (5-10)
- Add CDN and Front Door
- **Estimated Cost**: $1,165/month

**Cost Saving Tips**:
1. Use **Azure Reservations** for Container Apps (1-year commitment: 30% savings)
2. Use **Spot Containers** for non-critical workloads (up to 70% savings)
3. Configure **auto-scaling** to scale down during low traffic (nights/weekends)
4. Use **Azure Dev/Test pricing** for staging environment (40% savings)
5. Leverage **Azure credits** for startups (if applicable)

---

## 16. Consequences

### 16.1 Positive Consequences

**Technical Benefits**:
- ✅ Zero-downtime deployments with blue-green strategy
- ✅ Instant rollback capability (< 30 seconds)
- ✅ Production and staging environments isolated
- ✅ Scalable architecture for growth
- ✅ Comprehensive monitoring and observability

**Operational Benefits**:
- ✅ Reduced deployment risk
- ✅ Faster incident response
- ✅ Clear environment separation for testing
- ✅ Compliance-ready architecture
- ✅ Predictable deployment process

**Business Benefits**:
- ✅ Higher uptime and reliability
- ✅ Better customer experience
- ✅ Reduced revenue loss from outages
- ✅ Competitive advantage (fast feature releases)

### 16.2 Negative Consequences

**Cost Impact**:
- ❌ Higher infrastructure costs ($1,165/month vs $135/month for staging only)
- ❌ Need for cost optimization and monitoring

**Complexity**:
- ❌ More complex deployment pipeline
- ❌ Need to manage multiple environments
- ❌ Learning curve for blue-green deployments

**Operational Overhead**:
- ❌ Need for 24/7 monitoring and alerts
- ❌ Database migration backward compatibility requirements
- ❌ More stringent testing requirements

### 16.3 Mitigation Strategies

**Cost Mitigation**:
- Use Azure Reservations for predictable workloads
- Implement aggressive auto-scaling policies
- Monitor and optimize unnecessary resources
- Use Dev/Test pricing for non-production

**Complexity Mitigation**:
- Document all deployment procedures
- Create runbooks for common scenarios
- Automate everything possible
- Use Infrastructure as Code (IaC) for reproducibility

**Operational Mitigation**:
- Implement comprehensive monitoring and alerting
- Create on-call rotation for incident response
- Document rollback procedures
- Conduct regular disaster recovery drills

---

## 17. Related Decisions

- ADR-001: Choice of Azure as cloud provider
- ADR-002: Modular monolith architecture
- ADR-003: .NET 8 and Next.js technology stack
- ADR-004: Azure Container Apps for hosting
- ADR-005: Azure SQL Database for data persistence
- ADR-006: Stripe for payment processing
- ADR-007: Microsoft Entra ID for authentication

---

## 18. References

### Azure Documentation

- [Azure Container Apps Blue-Green Deployment](https://learn.microsoft.com/en-us/azure/container-apps/blue-green-deployment)
- [Azure Container Apps Revisions](https://learn.microsoft.com/en-us/azure/container-apps/revisions)
- [Azure Well-Architected Framework](https://learn.microsoft.com/en-us/azure/well-architected/)
- [Azure SQL Database Best Practices](https://learn.microsoft.com/en-us/azure/azure-sql/database/best-practices)
- [Azure Key Vault Best Practices](https://learn.microsoft.com/en-us/azure/key-vault/general/best-practices)

### Industry Best Practices

- Martin Fowler - [BlueGreenDeployment](https://martinfowler.com/bliki/BlueGreenDeployment.html)
- AWS - [Blue/Green Deployments](https://docs.aws.amazon.com/whitepapers/latest/overview-deployment-options/bluegreen-deployments.html)
- Google - [SRE Book: Eliminating Toil](https://sre.google/sre-book/eliminating-toil/)

---

## 19. Appendix: Quick Start Commands

### 19.1 One-Command Production Setup

```bash
# Download and run production setup script
curl -O https://raw.githubusercontent.com/your-org/lankaconnect/main/scripts/setup-production.sh
chmod +x setup-production.sh
./setup-production.sh
```

### 19.2 Manual Rollback

```bash
# Emergency rollback to previous revision
az containerapp ingress traffic set \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --revision-weight <previous-revision>=100

az containerapp ingress traffic set \
  --name lankaconnect-ui-prod \
  --resource-group lankaconnect-prod \
  --revision-weight <previous-revision>=100
```

### 19.3 Monitoring Dashboard URL

```bash
# Get Application Insights portal URL
az monitor app-insights component show \
  --resource-group lankaconnect-prod \
  --app lankaconnect-prod-insights \
  --query appId -o tsv

# Open: https://portal.azure.com/#@<tenant>/resource/subscriptions/<sub-id>/resourceGroups/lankaconnect-prod/providers/microsoft.insights/components/lankaconnect-prod-insights/overview
```

---

## Conclusion

**Final Recommendation**: Implement **Option 3 - Separate Production Environment with Blue-Green Deployment**.

This approach provides the best balance of safety, reliability, and operational excellence. While it requires higher upfront investment in infrastructure and complexity, the benefits of zero-downtime deployments, instant rollback, and proper environment separation far outweigh the costs.

**Next Steps**:
1. Review and approve this ADR
2. Provision production infrastructure (Phase 1)
3. Configure CI/CD pipelines (Phase 3)
4. Conduct testing and validation (Phase 4)
5. Execute go-live plan (Phase 5)

**Estimated Timeline**: 7-10 days from approval to production launch.
