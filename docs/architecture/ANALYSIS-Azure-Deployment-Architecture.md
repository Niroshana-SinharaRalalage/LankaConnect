# ADR-002: Azure Deployment Architecture (Staging-First Approach)

**Status:** Proposed
**Date:** 2025-10-28
**Decision Makers:** Architecture Team
**Context:** Phase 1 Entra External ID complete, 318/319 tests passing, ready for cloud deployment

---

## Executive Summary

This ADR defines the Azure deployment architecture for LankaConnect using a **staging-first approach** with Azure Container Apps, following Clean Architecture and Zero Tolerance for Compilation Errors principles.

## Decision

Deploy LankaConnect to Azure using:
1. **Azure Container Apps** (not AKS) for .NET 8 API hosting
2. **Azure Database for PostgreSQL Flexible Server** with connection pooling
3. **Azure Key Vault** for secrets management with Managed Identity
4. **GitHub Actions** for CI/CD automation
5. **Staging environment first**, validate, then promote to production

---

## Architecture Topology

### Staging Environment

```
┌─────────────────────────────────────────────────────────────────┐
│                        Azure Subscription                        │
│                    Resource Group: lankaconnect-staging          │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Azure Container Apps Environment: lankaconnect-staging  │  │
│  │  ┌────────────────────────────────────────────────────┐  │  │
│  │  │  Container App: lankaconnect-api-staging           │  │  │
│  │  │  - Image: ghcr.io/yourorg/lankaconnect-api:staging│  │  │
│  │  │  - Min Replicas: 1                                 │  │  │
│  │  │  - Max Replicas: 3                                 │  │  │
│  │  │  - CPU: 0.25 vCPU, Memory: 0.5 GB                 │  │  │
│  │  │  - Ingress: HTTPS only (auto TLS)                 │  │  │
│  │  │  - Managed Identity: Enabled                       │  │  │
│  │  └────────────────────────────────────────────────────┘  │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Azure Database for PostgreSQL Flexible Server          │  │
│  │  - Name: lankaconnect-staging-db                         │  │
│  │  - Tier: Burstable (B1ms) - 1 vCore, 2 GB RAM          │  │
│  │  - Storage: 32 GB (autogrow enabled)                    │  │
│  │  - High Availability: Disabled (staging only)            │  │
│  │  - Backup Retention: 7 days                              │  │
│  │  - Connection Pooling: Enabled (PgBouncer)               │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Azure Key Vault: lankaconnect-staging-kv                │  │
│  │  - Tier: Standard                                         │  │
│  │  - Access Policy: Managed Identity (Container App)       │  │
│  │  - Secrets: 14 environment variables                     │  │
│  │  - Soft Delete: 90 days                                  │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Azure Container Registry: lankaconnectstaging           │  │
│  │  - Tier: Basic ($5/month)                                │  │
│  │  - Images: 1 repository (api)                            │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Azure Monitor / Log Analytics                           │  │
│  │  - Container App Logs                                    │  │
│  │  - Database Metrics                                      │  │
│  │  - Retention: 30 days                                    │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘

Cost Estimate: $50-70/month
```

### Production Environment

```
┌─────────────────────────────────────────────────────────────────┐
│                        Azure Subscription                        │
│                    Resource Group: lankaconnect-prod             │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Azure Container Apps Environment: lankaconnect-prod     │  │
│  │  ┌────────────────────────────────────────────────────┐  │  │
│  │  │  Container App: lankaconnect-api-prod              │  │  │
│  │  │  - Image: ghcr.io/yourorg/lankaconnect-api:v1.0.0 │  │  │
│  │  │  - Min Replicas: 2 (high availability)             │  │  │
│  │  │  - Max Replicas: 10 (auto-scale)                   │  │  │
│  │  │  - CPU: 0.5 vCPU, Memory: 1 GB                     │  │  │
│  │  │  - Ingress: HTTPS only + Custom Domain             │  │  │
│  │  │  - Managed Identity: Enabled                        │  │  │
│  │  └────────────────────────────────────────────────────┘  │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Azure Database for PostgreSQL Flexible Server          │  │
│  │  - Name: lankaconnect-prod-db                            │  │
│  │  - Tier: General Purpose (D2ds_v5) - 2 vCores, 8 GB    │  │
│  │  - Storage: 64 GB (autogrow enabled)                    │  │
│  │  - High Availability: Zone-redundant (HA)               │  │
│  │  - Backup Retention: 35 days                             │  │
│  │  - Connection Pooling: Enabled (PgBouncer)               │  │
│  │  - Read Replicas: 1 (optional for scaling)              │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Azure Cache for Redis: lankaconnect-prod-redis          │  │
│  │  - Tier: Basic C1 (1 GB cache)                           │  │
│  │  - TLS 1.2 required                                      │  │
│  │  - Backup: Daily snapshot                                │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Azure Blob Storage: lankaconnectprodst                  │  │
│  │  - Tier: Standard LRS                                    │  │
│  │  - Container: business-images                            │  │
│  │  - CDN: Azure CDN Standard (optional)                    │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Azure Key Vault: lankaconnect-prod-kv                   │  │
│  │  - Tier: Standard                                         │  │
│  │  - Access Policy: Managed Identity                       │  │
│  │  - Secrets: 14 environment variables                     │  │
│  │  - RBAC Enabled                                           │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Azure Container Registry: lankaconnectprod              │  │
│  │  - Tier: Standard ($20/month)                            │  │
│  │  - Geo-Replication: Optional                             │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  SendGrid Email Service (3rd Party)                      │  │
│  │  - Plan: Essentials ($15/month for 50K emails)           │  │
│  │  - API Key stored in Key Vault                           │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Azure Monitor / Application Insights                    │  │
│  │  - APM (Application Performance Monitoring)              │  │
│  │  - Alerts (CPU, Memory, Response Time)                   │  │
│  │  - Retention: 90 days                                    │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘

Cost Estimate: $250-350/month (before scaling)
```

---

## Key Architectural Decisions

### 1. Container Apps vs App Service vs AKS

**Decision:** Azure Container Apps

**Rationale:**
- **Clean Architecture Fit:** Container Apps align with our stateless API design
- **Cost-Effective:** Pay only for active requests (consumption model)
- **No Kubernetes Overhead:** Managed platform, no cluster management
- **Built-in Scaling:** Auto-scale from 0-10 replicas based on HTTP requests
- **Managed Identity:** Native integration with Key Vault, no connection strings

**Rejected Alternatives:**
- **AKS (Azure Kubernetes Service):** Overkill for MVP, $70+ base cost, requires DevOps expertise
- **App Service:** Less flexible scaling, tied to specific SKUs

### 2. Database Strategy

**Decision:** Azure Database for PostgreSQL Flexible Server with PgBouncer

**Rationale:**
- **Compatibility:** Drop-in replacement for local Docker PostgreSQL
- **Connection Pooling:** PgBouncer built-in, handles .NET Entity Framework pooling
- **Burstable Tier:** Cost-effective for staging (B1ms = $12/month)
- **Managed Backups:** 7-35 days automated backups
- **High Availability:** Zone-redundant HA for production only

**Configuration:**
```
Staging: Burstable B1ms (1 vCore, 2 GB RAM) = $12/month
Production: General Purpose D2ds_v5 (2 vCores, 8 GB RAM) = $120/month
Production HA: Add $120/month for zone-redundant replica
```

**Migration Strategy:**
- Use idempotent SQL migrations (`20251028_AddEntraExternalIdSupport.sql`)
- Run migrations manually via Azure Cloud Shell for staging
- Automate migrations in CI/CD for production (with approval gate)

### 3. Secrets Management

**Decision:** Azure Key Vault with Managed Identity

**Rationale:**
- **Zero Secrets in Code:** All 14 environment variables stored in Key Vault
- **Managed Identity:** Container App authenticates without credentials
- **Audit Logging:** Track every secret access
- **Rotation Support:** Rotate secrets without redeploying app

**Secrets Stored (14 total):**
```
1. DATABASE-CONNECTION-STRING
2. JWT-SECRET-KEY
3. JWT-ISSUER
4. JWT-AUDIENCE
5. ENTRA-ENABLED
6. ENTRA-TENANT-ID
7. ENTRA-CLIENT-ID
8. ENTRA-AUDIENCE
9. AZURE-STORAGE-CONNECTION-STRING
10. SMTP-HOST
11. SMTP-PORT
12. SMTP-USERNAME
13. SMTP-PASSWORD
14. EMAIL-FROM-ADDRESS
15. REDIS-CONNECTION-STRING (production only)
```

**Access Pattern:**
```csharp
// Container App environment variables reference Key Vault
DATABASE_CONNECTION_STRING=@Microsoft.KeyVault(SecretUri=https://lankaconnect-staging-kv.vault.azure.net/secrets/DATABASE-CONNECTION-STRING/)
```

### 4. CI/CD Strategy

**Decision:** GitHub Actions with Environment Separation

**Rationale:**
- **Native Integration:** Repository already on GitHub
- **Environment Protection:** Require approvals for production deployments
- **Secrets Management:** GitHub Secrets for Azure credentials
- **Zero Downtime:** Rolling updates in Container Apps

**Pipeline Flow:**
```
1. Push to 'develop' → Deploy to Staging (automatic)
2. Manual approval → Deploy to Production
3. Failed deployment → Automatic rollback to previous revision
```

### 5. Cost Optimization Strategies

#### Staging Environment Optimizations

**Decision:** Minimal viable infrastructure

**Strategies:**
1. **Skip Redis:** Use in-memory caching for staging
2. **Skip Blob Storage:** Use Azurite locally, validate logic only
3. **Skip SendGrid:** Use MailHog (local dev) or free tier (25K emails/month)
4. **Burstable Database:** B1ms tier sufficient for testing
5. **Single Replica:** Min=1, Max=3 for load testing only
6. **Basic Container Registry:** $5/month vs $20/month Standard

**Staging Monthly Cost Breakdown:**
```
- Container Apps Environment:        $0 (no charge)
- Container App (1 replica):         $15-20
- PostgreSQL Flexible (B1ms):        $12
- Container Registry (Basic):        $5
- Key Vault (Standard):              $3
- Log Analytics (30-day retention):  $5-10
- Bandwidth:                          $5
────────────────────────────────────────
Total Staging:                        $45-55/month
```

#### Production Environment

**Production Monthly Cost Breakdown:**
```
- Container Apps (2-10 replicas):    $80-120
- PostgreSQL Flexible (D2ds_v5):     $120
- PostgreSQL HA (zone-redundant):    $120
- Redis Cache (C1):                  $30
- Blob Storage (50 GB):              $10
- SendGrid (50K emails/month):       $15
- Container Registry (Standard):     $20
- Key Vault (Standard):              $3
- Application Insights:              $20-30
- Bandwidth:                          $20-30
────────────────────────────────────────
Total Production:                     $438-498/month

Without HA Database:                  $318-378/month
```

**Cost Reduction Options:**
1. **Defer Redis:** Use distributed caching later (save $30/month)
2. **Defer HA Database:** Enable after 1,000+ users (save $120/month)
3. **Use Azure CDN:** Reduce bandwidth costs by 50%
4. **Reserved Instances:** 30% savings with 1-year commitment

---

## Configuration Files

### 1. appsettings.Staging.json

**Decision:** Create new file for staging-specific overrides

**Location:** `src/LankaConnect.API/appsettings.Staging.json`

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  },
  "ConnectionStrings": {
    "DefaultConnection": "${DATABASE_CONNECTION_STRING}",
    "Redis": "localhost:6379"
  },
  "JwtSettings": {
    "SecretKey": "${JWT_SECRET_KEY}",
    "Issuer": "${JWT_ISSUER}",
    "Audience": "${JWT_AUDIENCE}",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "EntraExternalId": {
    "IsEnabled": "${ENTRA_ENABLED:true}",
    "TenantId": "${ENTRA_TENANT_ID}",
    "ClientId": "${ENTRA_CLIENT_ID}",
    "Instance": "https://login.microsoftonline.com/",
    "Audience": "${ENTRA_AUDIENCE}",
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true,
    "ValidateIssuerSigningKey": true
  },
  "Email": {
    "SmtpHost": "smtp.sendgrid.net",
    "SmtpPort": "587",
    "SmtpUsername": "apikey",
    "SmtpPassword": "${SMTP_PASSWORD}",
    "FromEmail": "noreply-staging@lankaconnect.com",
    "FromName": "LankaConnect Staging",
    "EnableSsl": true
  },
  "ASPNETCORE_ENVIRONMENT": "Staging"
}
```

### 2. Dockerfile (Production-Ready)

**Decision:** Multi-stage build with health checks

**Location:** `src/LankaConnect.API/Dockerfile`

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["src/LankaConnect.API/LankaConnect.API.csproj", "LankaConnect.API/"]
COPY ["src/LankaConnect.Application/LankaConnect.Application.csproj", "LankaConnect.Application/"]
COPY ["src/LankaConnect.Domain/LankaConnect.Domain.csproj", "LankaConnect.Domain/"]
COPY ["src/LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj", "LankaConnect.Infrastructure/"]

RUN dotnet restore "LankaConnect.API/LankaConnect.API.csproj"

# Copy source code
COPY ["src/", "."]

# Build
WORKDIR "/src/LankaConnect.API"
RUN dotnet build "LankaConnect.API.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "LankaConnect.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5000

# Copy published output
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl --fail http://localhost:5000/health || exit 1

# Run as non-root user
USER $APP_UID

ENTRYPOINT ["dotnet", "LankaConnect.API.dll"]
```

### 3. GitHub Actions Workflow

**Decision:** Separate workflows for staging and production

**Location:** `.github/workflows/deploy-staging.yml`

```yaml
name: Deploy to Azure Staging

on:
  push:
    branches:
      - develop
  workflow_dispatch:

env:
  AZURE_CONTAINER_REGISTRY: lankaconnectstaging
  CONTAINER_APP_NAME: lankaconnect-api-staging
  RESOURCE_GROUP: lankaconnect-staging

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET 8
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore src/LankaConnect.API/LankaConnect.API.csproj

      - name: Run tests (Zero Tolerance)
        run: |
          dotnet test tests/LankaConnect.Application.Tests/LankaConnect.Application.Tests.csproj --no-restore
          dotnet test tests/LankaConnect.IntegrationTests/LankaConnect.IntegrationTests.csproj --no-restore

      - name: Build application
        run: dotnet build src/LankaConnect.API/LankaConnect.API.csproj -c Release --no-restore

      - name: Azure Login
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS_STAGING }}

      - name: Build and push Docker image
        uses: azure/docker-login@v1
        with:
          login-server: ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io
          username: ${{ secrets.ACR_USERNAME_STAGING }}
          password: ${{ secrets.ACR_PASSWORD_STAGING }}

      - name: Build Docker image
        run: |
          docker build -t ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/lankaconnect-api:${{ github.sha }} \
                       -t ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/lankaconnect-api:staging \
                       -f src/LankaConnect.API/Dockerfile .
          docker push ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/lankaconnect-api:${{ github.sha }}
          docker push ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/lankaconnect-api:staging

      - name: Deploy to Container App
        uses: azure/container-apps-deploy-action@v1
        with:
          acrName: ${{ env.AZURE_CONTAINER_REGISTRY }}
          containerAppName: ${{ env.CONTAINER_APP_NAME }}
          resourceGroup: ${{ env.RESOURCE_GROUP }}
          imageToDeploy: ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/lankaconnect-api:${{ github.sha }}

      - name: Run smoke tests
        run: |
          sleep 30  # Wait for deployment
          STAGING_URL=$(az containerapp show \
            --name ${{ env.CONTAINER_APP_NAME }} \
            --resource-group ${{ env.RESOURCE_GROUP }} \
            --query 'properties.configuration.ingress.fqdn' -o tsv)

          # Health check
          curl --fail https://$STAGING_URL/health || exit 1

          # Entra endpoint check
          curl -X POST https://$STAGING_URL/api/auth/login/entra \
            -H "Content-Type: application/json" \
            -d '{"accessToken":"invalid","ipAddress":"127.0.0.1"}' | grep -q '401\|400'

      - name: Notify deployment
        if: always()
        run: |
          echo "Staging deployment completed: ${{ job.status }}"
```

---

## Testing & Validation Strategy

### Smoke Tests (Post-Deployment)

**Automated Tests (in CI/CD):**
1. Health check endpoint responds 200 OK
2. Entra login endpoint returns proper error for invalid token (400/401)
3. Database connectivity verified
4. Key Vault secrets loaded successfully

**Manual Tests (Staging):**
1. Register new user with Entra External ID
2. Verify auto-provisioning creates user in database
3. Test JWT token generation and refresh
4. Verify email verification flow (if enabled)
5. Test business listing CRUD operations
6. Verify file upload to Azurite (staging) / Blob Storage (prod)

### Validation Checklist

```markdown
Staging Deployment Validation
- [ ] Container App running with 1 replica
- [ ] Health endpoint returns "Healthy"
- [ ] Database migration applied successfully
- [ ] 14 secrets loaded from Key Vault
- [ ] Entra External ID login works
- [ ] Application logs visible in Azure Monitor
- [ ] No compilation errors (Zero Tolerance)
- [ ] 318/319 tests passing
- [ ] Response time <500ms (p95)

Production Promotion Validation
- [ ] All staging validation passed
- [ ] Load testing completed (100+ concurrent users)
- [ ] Security audit passed
- [ ] Backup restoration tested
- [ ] Rollback procedure documented
- [ ] On-call team notified
- [ ] Monitoring alerts configured
```

---

## Rollback Procedures

### Automatic Rollback (Container Apps)

**Decision:** Use Container Apps revision management

**Strategy:**
```bash
# Container Apps automatically creates revisions
# Rollback to previous revision if health checks fail

# Manual rollback command
az containerapp revision list \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query '[].{Name:name, CreatedTime:properties.createdTime, Active:properties.active}' \
  -o table

# Activate previous revision
az containerapp revision activate \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --revision lankaconnect-api-staging--<PREVIOUS-REVISION>
```

### Database Rollback

**Decision:** Manual rollback with backup restoration

**Strategy:**
```bash
# 1. Stop Container App (prevent new connections)
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --min-replicas 0 \
  --max-replicas 0

# 2. Restore from backup (Azure Portal or CLI)
az postgres flexible-server restore \
  --resource-group lankaconnect-staging \
  --name lankaconnect-staging-db-restored \
  --source-server lankaconnect-staging-db \
  --restore-time "2025-10-28T12:00:00Z"

# 3. Update connection string in Key Vault
az keyvault secret set \
  --vault-name lankaconnect-staging-kv \
  --name DATABASE-CONNECTION-STRING \
  --value "Host=lankaconnect-staging-db-restored.postgres.database.azure.com;..."

# 4. Restart Container App
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --min-replicas 1 \
  --max-replicas 3
```

**RTO (Recovery Time Objective):** 15 minutes
**RPO (Recovery Point Objective):** 1 hour (backup frequency)

---

## Risk Mitigation Strategies

### Risk 1: Database Connection Pool Exhaustion

**Mitigation:**
- Enable PgBouncer connection pooling (default in Azure PostgreSQL)
- Configure EF Core connection pool limits
- Monitor active connections in Azure Portal
- Alert threshold: >80% of max connections

### Risk 2: Entra External ID Unavailable

**Mitigation:**
- Graceful degradation: Disable Entra via Key Vault (`ENTRA_ENABLED=false`)
- Users can still login with local passwords
- Cache Entra tokens in Redis (production) for 15 minutes
- Monitor Entra token validation latency

### Risk 3: Key Vault Throttling

**Mitigation:**
- Cache secrets in Container App memory for 1 hour
- Use System-Assigned Managed Identity (no credential rotation)
- Monitor Key Vault metrics (requests per second)
- Standard tier supports 2,000 requests/10 seconds

### Risk 4: Cold Start Latency (Container Apps)

**Mitigation:**
- Set min replicas to 1 (staging) or 2 (production)
- Cold start time: ~3-5 seconds for .NET 8 app
- Use HTTP health checks with 10-second grace period
- Monitor p99 response times in Application Insights

### Risk 5: Migration Failure in Production

**Mitigation:**
- Test all migrations in staging first
- Use idempotent SQL scripts (DO IF NOT EXISTS)
- Manual approval gate before production deployment
- Backup database before migration
- Rollback script prepared in advance

---

## Next Steps

### Immediate Actions (Week 1)

1. Create Azure subscription and resource groups
2. Provision staging resources via Azure CLI
3. Configure GitHub Secrets for CI/CD
4. Deploy staging environment
5. Run smoke tests and validate

### Short-Term Actions (Week 2-3)

1. Load test staging (100+ concurrent users)
2. Configure monitoring and alerts
3. Document operational runbooks
4. Provision production resources
5. Deploy to production with approval gate

### Long-Term Actions (Month 2+)

1. Enable Application Insights APM
2. Configure Azure CDN for static assets
3. Implement blue-green deployments
4. Enable auto-scaling based on CPU/memory
5. Setup disaster recovery plan (multi-region)

---

## Consequences

### Positive

- **Fast Time-to-Market:** Staging deployed in 1 week
- **Cost-Effective:** $45-55/month staging vs $500+ for AKS
- **Low Operational Overhead:** Managed services, no cluster management
- **Zero Secrets in Code:** All secrets in Key Vault
- **Auto-Scaling:** Handle traffic spikes automatically

### Negative

- **Vendor Lock-In:** Azure-specific services (Container Apps, Key Vault)
- **Cold Start Latency:** 3-5 seconds for first request (mitigated with min replicas)
- **Limited Customization:** Less control than AKS for advanced scenarios

### Neutral

- **Learning Curve:** Team needs to learn Container Apps and Key Vault
- **Migration Path:** Can migrate to AKS later if needed (Dockerfile already compatible)

---

## Appendix: Useful Commands

### Azure CLI Setup

```bash
# Install Azure CLI
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Login
az login

# Set subscription
az account set --subscription "YOUR_SUBSCRIPTION_ID"
```

### Resource Provisioning

```bash
# Create resource group
az group create \
  --name lankaconnect-staging \
  --location eastus

# Create Container Apps environment
az containerapp env create \
  --name lankaconnect-staging \
  --resource-group lankaconnect-staging \
  --location eastus

# Create PostgreSQL server
az postgres flexible-server create \
  --name lankaconnect-staging-db \
  --resource-group lankaconnect-staging \
  --location eastus \
  --admin-user adminuser \
  --admin-password 'P@ssw0rd123!' \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --storage-size 32 \
  --version 15
```

---

**Document Version:** 1.0
**Last Updated:** 2025-10-28
**Next Review:** 2025-11-11 (after staging deployment)
