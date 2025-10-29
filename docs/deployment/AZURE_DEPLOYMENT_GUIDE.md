# Azure Deployment Guide - Staging First Approach

**Last Updated:** 2025-10-28
**Version:** 1.0
**Target:** Staging Environment First, Production Later

---

## Overview

This guide provides step-by-step instructions for deploying LankaConnect to Azure using the staging-first approach. Follow each section in order.

**Timeline:**
- Day 1-2: Azure resource provisioning
- Day 3-4: CI/CD pipeline setup
- Day 5: Staging deployment and validation
- Day 6-7: Production deployment (after staging validation)

---

## Prerequisites

Before starting, ensure you have:

1. Azure subscription with Owner or Contributor role
2. Azure CLI installed (v2.50+)
3. Docker Desktop installed
4. GitHub repository access
5. .NET 8 SDK installed
6. Access to Microsoft Entra External ID tenant

### Install Azure CLI

```bash
# Windows (PowerShell)
winget install -e --id Microsoft.AzureCLI

# macOS
brew install azure-cli

# Linux (Ubuntu/Debian)
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Verify installation
az --version
```

### Login to Azure

```bash
# Interactive login
az login

# List subscriptions
az account list --output table

# Set active subscription
az account set --subscription "YOUR_SUBSCRIPTION_ID"

# Verify current subscription
az account show --output table
```

---

## Phase 1: Staging Environment Provisioning

### Step 1.1: Create Resource Group

```bash
# Define variables
LOCATION="eastus"
STAGING_RG="lankaconnect-staging"

# Create resource group
az group create \
  --name $STAGING_RG \
  --location $LOCATION \
  --tags Environment=Staging Project=LankaConnect Owner=DevTeam

# Verify creation
az group show --name $STAGING_RG --output table
```

**Expected Output:**
```
Name                   Location    Status
---------------------  ----------  ---------
lankaconnect-staging   eastus      Succeeded
```

### Step 1.2: Create Container Registry

```bash
# Define variables
ACR_NAME="lankaconnectstaging"  # Must be globally unique, lowercase, no hyphens

# Create container registry (Basic tier for staging)
az acr create \
  --name $ACR_NAME \
  --resource-group $STAGING_RG \
  --sku Basic \
  --admin-enabled true \
  --location $LOCATION

# Get registry credentials (save these for GitHub Secrets)
az acr credential show --name $ACR_NAME --resource-group $STAGING_RG

# Expected output:
# {
#   "passwords": [
#     {
#       "name": "password",
#       "value": "YOUR_ACR_PASSWORD_1"
#     },
#     {
#       "name": "password2",
#       "value": "YOUR_ACR_PASSWORD_2"
#     }
#   ],
#   "username": "lankaconnectstaging"
# }
```

**Save Credentials:**
- ACR Username: `lankaconnectstaging`
- ACR Password: (from output above)

### Step 1.3: Create PostgreSQL Database

```bash
# Define variables
POSTGRES_SERVER="lankaconnect-staging-db"
POSTGRES_ADMIN="adminuser"
POSTGRES_PASSWORD="P@ssw0rd123!ChangeMe"  # CHANGE THIS!

# Create PostgreSQL Flexible Server (Burstable tier)
az postgres flexible-server create \
  --name $POSTGRES_SERVER \
  --resource-group $STAGING_RG \
  --location $LOCATION \
  --admin-user $POSTGRES_ADMIN \
  --admin-password "$POSTGRES_PASSWORD" \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --storage-size 32 \
  --version 15 \
  --backup-retention 7 \
  --high-availability Disabled \
  --public-access 0.0.0.0 \
  --tags Environment=Staging

# Create database
az postgres flexible-server db create \
  --resource-group $STAGING_RG \
  --server-name $POSTGRES_SERVER \
  --database-name LankaConnectDB

# Enable PgBouncer connection pooling
az postgres flexible-server parameter set \
  --resource-group $STAGING_RG \
  --server-name $POSTGRES_SERVER \
  --name pgbouncer.enabled \
  --value on

# Configure firewall (allow Azure services)
az postgres flexible-server firewall-rule create \
  --resource-group $STAGING_RG \
  --name $POSTGRES_SERVER \
  --rule-name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Get connection string
POSTGRES_HOST="${POSTGRES_SERVER}.postgres.database.azure.com"
POSTGRES_CONNECTION_STRING="Host=${POSTGRES_HOST};Database=LankaConnectDB;Username=${POSTGRES_ADMIN};Password=${POSTGRES_PASSWORD};SslMode=Require;Pooling=true;MinPoolSize=2;MaxPoolSize=20"

echo "Connection String: $POSTGRES_CONNECTION_STRING"
```

**Save Connection String:** You'll need this for Key Vault.

### Step 1.4: Apply Database Migration

```bash
# Download migration script
# Assuming you're in the project root directory

# Install PostgreSQL client (if not already installed)
# Windows: Download from https://www.postgresql.org/download/windows/
# macOS: brew install postgresql
# Linux: sudo apt-get install postgresql-client

# Apply migration
psql "$POSTGRES_CONNECTION_STRING" -f docs/deployment/migrations/20251028_AddEntraExternalIdSupport.sql

# Verify migration
psql "$POSTGRES_CONNECTION_STRING" -c "SELECT * FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 5;"

# Expected output should show:
# 20251028184528_AddEntraExternalIdSupport | 8.0.19
```

### Step 1.5: Create Key Vault

```bash
# Define variables
KEY_VAULT_NAME="lankaconnect-staging-kv"  # Must be globally unique

# Create Key Vault
az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $STAGING_RG \
  --location $LOCATION \
  --sku standard \
  --enable-soft-delete true \
  --soft-delete-retention-days 90 \
  --enable-rbac-authorization false

# Verify creation
az keyvault show --name $KEY_VAULT_NAME --resource-group $STAGING_RG --output table
```

### Step 1.6: Populate Key Vault Secrets

```bash
# Generate JWT secret (256-bit)
JWT_SECRET=$(openssl rand -base64 32)

# Set secrets (replace placeholders with your values)
az keyvault secret set --vault-name $KEY_VAULT_NAME --name DATABASE-CONNECTION-STRING --value "$POSTGRES_CONNECTION_STRING"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name JWT-SECRET-KEY --value "$JWT_SECRET"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name JWT-ISSUER --value "https://lankaconnect-api-staging.azurewebsites.net"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name JWT-AUDIENCE --value "https://lankaconnect-staging.azurewebsites.net"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name ENTRA-ENABLED --value "true"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name ENTRA-TENANT-ID --value "369a3c47-33b7-4baa-98b8-6ddf16a51a31"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name ENTRA-CLIENT-ID --value "957e9865-fca0-4236-9276-a8643a7193b5"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name ENTRA-AUDIENCE --value "api://957e9865-fca0-4236-9276-a8643a7193b5"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name SMTP-HOST --value "smtp.sendgrid.net"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name SMTP-PORT --value "587"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name SMTP-USERNAME --value "apikey"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name SMTP-PASSWORD --value "YOUR_SENDGRID_API_KEY"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name EMAIL-FROM-ADDRESS --value "noreply-staging@lankaconnect.com"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name AZURE-STORAGE-CONNECTION-STRING --value "UseDevelopmentStorage=true"

# List secrets to verify
az keyvault secret list --vault-name $KEY_VAULT_NAME --output table

# Expected output:
# Name                            Enabled    Expires    Not Before    Created
# ------------------------------  ---------  ---------  ------------  -------------------
# DATABASE-CONNECTION-STRING      True       None       None          2025-10-28...
# JWT-SECRET-KEY                  True       None       None          2025-10-28...
# ... (14 total secrets)
```

### Step 1.7: Create Container Apps Environment

```bash
# Define variables
CONTAINERAPPS_ENV="lankaconnect-staging"
LOG_ANALYTICS_WORKSPACE="lankaconnect-staging-logs"

# Create Log Analytics Workspace
az monitor log-analytics workspace create \
  --resource-group $STAGING_RG \
  --workspace-name $LOG_ANALYTICS_WORKSPACE \
  --location $LOCATION

# Get workspace ID and key
LOG_ANALYTICS_WORKSPACE_ID=$(az monitor log-analytics workspace show \
  --resource-group $STAGING_RG \
  --workspace-name $LOG_ANALYTICS_WORKSPACE \
  --query customerId -o tsv)

LOG_ANALYTICS_WORKSPACE_KEY=$(az monitor log-analytics workspace get-shared-keys \
  --resource-group $STAGING_RG \
  --workspace-name $LOG_ANALYTICS_WORKSPACE \
  --query primarySharedKey -o tsv)

# Create Container Apps environment
az containerapp env create \
  --name $CONTAINERAPPS_ENV \
  --resource-group $STAGING_RG \
  --location $LOCATION \
  --logs-workspace-id $LOG_ANALYTICS_WORKSPACE_ID \
  --logs-workspace-key $LOG_ANALYTICS_WORKSPACE_KEY

# Verify creation
az containerapp env show --name $CONTAINERAPPS_ENV --resource-group $STAGING_RG --output table
```

### Step 1.8: Create Container App with Managed Identity

```bash
# Define variables
CONTAINER_APP_NAME="lankaconnect-api-staging"

# Create Container App with system-assigned managed identity
az containerapp create \
  --name $CONTAINER_APP_NAME \
  --resource-group $STAGING_RG \
  --environment $CONTAINERAPPS_ENV \
  --image mcr.microsoft.com/azuredocs/containerapps-helloworld:latest \
  --target-port 80 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 3 \
  --cpu 0.25 \
  --memory 0.5Gi \
  --system-assigned

# Get managed identity principal ID
MANAGED_IDENTITY_ID=$(az containerapp show \
  --name $CONTAINER_APP_NAME \
  --resource-group $STAGING_RG \
  --query identity.principalId -o tsv)

echo "Managed Identity Principal ID: $MANAGED_IDENTITY_ID"

# Grant Container App access to Key Vault
az keyvault set-policy \
  --name $KEY_VAULT_NAME \
  --object-id $MANAGED_IDENTITY_ID \
  --secret-permissions get list

# Grant Container App access to Container Registry
az acr update --name $ACR_NAME --admin-enabled true

# Get Container App URL
CONTAINER_APP_URL=$(az containerapp show \
  --name $CONTAINER_APP_NAME \
  --resource-group $STAGING_RG \
  --query properties.configuration.ingress.fqdn -o tsv)

echo "Container App URL: https://$CONTAINER_APP_URL"
```

**Save Container App URL:** You'll use this for testing.

---

## Phase 2: CI/CD Pipeline Setup

### Step 2.1: Create GitHub Secrets

Navigate to your GitHub repository: `Settings` → `Secrets and variables` → `Actions` → `New repository secret`

Create the following secrets:

```
1. AZURE_CREDENTIALS_STAGING
   Value: (See Step 2.2 for how to generate)

2. ACR_USERNAME_STAGING
   Value: lankaconnectstaging

3. ACR_PASSWORD_STAGING
   Value: (From Step 1.2)

4. AZURE_SUBSCRIPTION_ID
   Value: (Your Azure subscription ID)
```

### Step 2.2: Create Service Principal for GitHub Actions

```bash
# Get subscription ID
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

# Create service principal with Contributor role
az ad sp create-for-rbac \
  --name "lankaconnect-github-actions-staging" \
  --role Contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$STAGING_RG \
  --sdk-auth

# Expected output (save entire JSON):
# {
#   "clientId": "YOUR_CLIENT_ID",
#   "clientSecret": "YOUR_CLIENT_SECRET",
#   "subscriptionId": "YOUR_SUBSCRIPTION_ID",
#   "tenantId": "YOUR_TENANT_ID",
#   "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
#   "resourceManagerEndpointUrl": "https://management.azure.com/",
#   "activeDirectoryGraphResourceId": "https://graph.windows.net/",
#   "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
#   "galleryEndpointUrl": "https://gallery.azure.com/",
#   "managementEndpointUrl": "https://management.core.windows.net/"
# }
```

**Action:** Copy the entire JSON output and paste it as `AZURE_CREDENTIALS_STAGING` secret in GitHub.

### Step 2.3: Create GitHub Actions Workflow Files

Create `.github/workflows/deploy-staging.yml`:

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
  KEY_VAULT_NAME: lankaconnect-staging-kv

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
        run: dotnet restore

      - name: Run unit tests
        run: dotnet test tests/LankaConnect.Application.Tests/LankaConnect.Application.Tests.csproj --no-restore --verbosity normal

      - name: Run integration tests
        run: dotnet test tests/LankaConnect.IntegrationTests/LankaConnect.IntegrationTests.csproj --no-restore --verbosity normal

      - name: Build application
        run: dotnet build src/LankaConnect.API/LankaConnect.API.csproj -c Release --no-restore

      - name: Azure Login
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS_STAGING }}

      - name: Login to Azure Container Registry
        uses: azure/docker-login@v1
        with:
          login-server: ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io
          username: ${{ secrets.ACR_USERNAME_STAGING }}
          password: ${{ secrets.ACR_PASSWORD_STAGING }}

      - name: Build and push Docker image
        run: |
          docker build -t ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/lankaconnect-api:${{ github.sha }} \
                       -t ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/lankaconnect-api:staging \
                       -f src/LankaConnect.API/Dockerfile .
          docker push ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/lankaconnect-api:${{ github.sha }}
          docker push ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/lankaconnect-api:staging

      - name: Update Container App with Key Vault references
        run: |
          az containerapp update \
            --name ${{ env.CONTAINER_APP_NAME }} \
            --resource-group ${{ env.RESOURCE_GROUP }} \
            --image ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/lankaconnect-api:${{ github.sha }} \
            --set-env-vars \
              ASPNETCORE_ENVIRONMENT=Staging \
              DATABASE_CONNECTION_STRING=secretref:DATABASE-CONNECTION-STRING \
              JWT_SECRET_KEY=secretref:JWT-SECRET-KEY \
              JWT_ISSUER=secretref:JWT-ISSUER \
              JWT_AUDIENCE=secretref:JWT-AUDIENCE \
              ENTRA_ENABLED=secretref:ENTRA-ENABLED \
              ENTRA_TENANT_ID=secretref:ENTRA-TENANT-ID \
              ENTRA_CLIENT_ID=secretref:ENTRA-CLIENT-ID \
              ENTRA_AUDIENCE=secretref:ENTRA-AUDIENCE \
              SMTP_HOST=secretref:SMTP-HOST \
              SMTP_PORT=secretref:SMTP-PORT \
              SMTP_USERNAME=secretref:SMTP-USERNAME \
              SMTP_PASSWORD=secretref:SMTP-PASSWORD \
              EMAIL_FROM_ADDRESS=secretref:EMAIL-FROM-ADDRESS \
              AZURE_STORAGE_CONNECTION_STRING=secretref:AZURE-STORAGE-CONNECTION-STRING

      - name: Wait for deployment
        run: sleep 30

      - name: Get Container App URL
        id: get-url
        run: |
          URL=$(az containerapp show \
            --name ${{ env.CONTAINER_APP_NAME }} \
            --resource-group ${{ env.RESOURCE_GROUP }} \
            --query 'properties.configuration.ingress.fqdn' -o tsv)
          echo "STAGING_URL=https://$URL" >> $GITHUB_OUTPUT

      - name: Health check
        run: |
          curl --fail ${{ steps.get-url.outputs.STAGING_URL }}/health || exit 1

      - name: Smoke test - Entra endpoint
        run: |
          HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
            -X POST ${{ steps.get-url.outputs.STAGING_URL }}/api/auth/login/entra \
            -H "Content-Type: application/json" \
            -d '{"accessToken":"invalid","ipAddress":"127.0.0.1"}')

          if [ "$HTTP_CODE" -eq 400 ] || [ "$HTTP_CODE" -eq 401 ]; then
            echo "Entra endpoint responding correctly (HTTP $HTTP_CODE)"
          else
            echo "Unexpected response code: $HTTP_CODE"
            exit 1
          fi

      - name: Deployment summary
        run: |
          echo "Staging deployment successful!"
          echo "URL: ${{ steps.get-url.outputs.STAGING_URL }}"
          echo "Image: ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/lankaconnect-api:${{ github.sha }}"
```

### Step 2.4: Create Production Workflow (for later)

Create `.github/workflows/deploy-production.yml`:

```yaml
name: Deploy to Azure Production

on:
  workflow_dispatch:
    inputs:
      version:
        description: 'Version tag to deploy (e.g., v1.0.0)'
        required: true
        type: string

env:
  AZURE_CONTAINER_REGISTRY: lankaconnectprod
  CONTAINER_APP_NAME: lankaconnect-api-prod
  RESOURCE_GROUP: lankaconnect-prod

jobs:
  deploy-production:
    runs-on: ubuntu-latest
    environment:
      name: production
      url: https://api.lankaconnect.com

    steps:
      # Similar to staging workflow but with production resources
      # Requires manual approval (configured in GitHub environment settings)

      - name: Checkout code
        uses: actions/checkout@v4
        with:
          ref: ${{ inputs.version }}

      # ... (similar steps to staging)
```

---

## Phase 3: Deploy to Staging

### Step 3.1: Trigger Initial Deployment

```bash
# Option 1: Push to develop branch
git checkout develop
git push origin develop

# Option 2: Trigger manual workflow
# Go to GitHub Actions → Deploy to Azure Staging → Run workflow
```

### Step 3.2: Monitor Deployment

```bash
# Watch Container App logs
az containerapp logs show \
  --name $CONTAINER_APP_NAME \
  --resource-group $STAGING_RG \
  --follow

# Check deployment status
az containerapp revision list \
  --name $CONTAINER_APP_NAME \
  --resource-group $STAGING_RG \
  --output table

# Expected output:
# Name                                    Active    Created              Traffic Weight
# --------------------------------------  --------  -------------------  ---------------
# lankaconnect-api-staging--abc123        True      2025-10-28 10:00:00  100%
```

### Step 3.3: Manual Smoke Tests

```bash
# Get Container App URL
STAGING_URL=$(az containerapp show \
  --name $CONTAINER_APP_NAME \
  --resource-group $STAGING_RG \
  --query 'properties.configuration.ingress.fqdn' -o tsv)

echo "Staging URL: https://$STAGING_URL"

# Test 1: Health check
curl https://$STAGING_URL/health

# Expected response:
# {
#   "status": "Healthy",
#   "service": "Authentication",
#   "timestamp": "2025-10-28T10:00:00Z"
# }

# Test 2: Entra login endpoint (should return 400 for invalid token)
curl -X POST https://$STAGING_URL/api/auth/login/entra \
  -H "Content-Type: application/json" \
  -d '{"accessToken":"invalid","ipAddress":"127.0.0.1"}'

# Expected response (400 Bad Request):
# {
#   "error": "Invalid access token"
# }

# Test 3: Check database connection
psql "$POSTGRES_CONNECTION_STRING" -c "SELECT COUNT(*) FROM identity.users;"

# Expected: Query should execute successfully
```

### Step 3.4: Validate Entra External ID Integration

**Prerequisites:**
- Valid Entra External ID access token from frontend

```bash
# Get access token from your Entra External ID tenant
# Use Postman or your frontend application to authenticate

# Test with valid token
curl -X POST https://$STAGING_URL/api/auth/login/entra \
  -H "Content-Type: application/json" \
  -d '{
    "accessToken": "YOUR_VALID_ENTRA_TOKEN",
    "ipAddress": "203.0.113.1"
  }'

# Expected response (200 OK):
# {
#   "user": {
#     "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
#     "email": "user@example.com",
#     "fullName": "John Doe",
#     "role": "User",
#     "isNewUser": true
#   },
#   "accessToken": "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9...",
#   "tokenExpiresAt": "2025-10-28T10:15:00Z"
# }

# Verify user was created in database
psql "$POSTGRES_CONNECTION_STRING" -c "
SELECT \"Id\", \"Email\", \"IdentityProvider\", \"ExternalProviderId\", \"CreatedAt\"
FROM identity.users
WHERE \"IdentityProvider\" = 1
ORDER BY \"CreatedAt\" DESC
LIMIT 5;
"
```

---

## Phase 4: Production Deployment (After Staging Validation)

### Step 4.1: Provision Production Resources

**Repeat Phase 1 with production-specific values:**

```bash
# Production variables
LOCATION="eastus"
PROD_RG="lankaconnect-prod"
ACR_NAME_PROD="lankaconnectprod"
POSTGRES_SERVER_PROD="lankaconnect-prod-db"
KEY_VAULT_NAME_PROD="lankaconnect-prod-kv"
CONTAINERAPPS_ENV_PROD="lankaconnect-prod"
CONTAINER_APP_NAME_PROD="lankaconnect-api-prod"

# Create resource group
az group create --name $PROD_RG --location $LOCATION --tags Environment=Production

# Create Container Registry (Standard tier for production)
az acr create --name $ACR_NAME_PROD --resource-group $PROD_RG --sku Standard --admin-enabled true

# Create PostgreSQL (General Purpose tier with HA)
az postgres flexible-server create \
  --name $POSTGRES_SERVER_PROD \
  --resource-group $PROD_RG \
  --location $LOCATION \
  --admin-user adminuser \
  --admin-password "STRONG_PASSWORD_HERE" \
  --sku-name Standard_D2ds_v5 \
  --tier GeneralPurpose \
  --storage-size 64 \
  --version 15 \
  --backup-retention 35 \
  --high-availability ZoneRedundant

# Create Redis Cache
az redis create \
  --name lankaconnect-prod-redis \
  --resource-group $PROD_RG \
  --location $LOCATION \
  --sku Basic \
  --vm-size c1 \
  --enable-non-ssl-port false

# Create Blob Storage
az storage account create \
  --name lankaconnectprodst \
  --resource-group $PROD_RG \
  --location $LOCATION \
  --sku Standard_LRS \
  --kind StorageV2

az storage container create \
  --name business-images \
  --account-name lankaconnectprodst \
  --public-access off

# ... (continue with Key Vault, Container Apps environment, etc.)
```

### Step 4.2: Configure GitHub Environment Protection

1. Go to GitHub repository → `Settings` → `Environments`
2. Click `New environment` → Name: `production`
3. Enable `Required reviewers` → Add team members
4. Enable `Wait timer` → 5 minutes (optional)
5. Add environment secrets (same as staging but with production values)

### Step 4.3: Deploy to Production

```bash
# Trigger production workflow manually
# Go to GitHub Actions → Deploy to Azure Production → Run workflow
# Enter version tag (e.g., v1.0.0)
# Approve deployment when prompted
```

---

## Phase 5: Monitoring & Operations

### Step 5.1: Configure Application Insights (Production)

```bash
# Create Application Insights
az monitor app-insights component create \
  --app lankaconnect-prod-insights \
  --location $LOCATION \
  --resource-group $PROD_RG \
  --application-type web

# Get instrumentation key
INSTRUMENTATION_KEY=$(az monitor app-insights component show \
  --app lankaconnect-prod-insights \
  --resource-group $PROD_RG \
  --query instrumentationKey -o tsv)

# Add to Key Vault
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME_PROD \
  --name APPLICATIONINSIGHTS-CONNECTION-STRING \
  --value "InstrumentationKey=$INSTRUMENTATION_KEY"
```

### Step 5.2: Configure Alerts

```bash
# Create action group for email notifications
az monitor action-group create \
  --name lankaconnect-alerts \
  --resource-group $PROD_RG \
  --short-name lcalerts \
  --email-receiver name=DevTeam email=devteam@lankaconnect.com

# Alert 1: High CPU usage
az monitor metrics alert create \
  --name "High CPU Usage" \
  --resource-group $PROD_RG \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$PROD_RG/providers/Microsoft.App/containerApps/$CONTAINER_APP_NAME_PROD \
  --condition "avg UsageNanoCores > 80" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action lankaconnect-alerts

# Alert 2: High response time
az monitor metrics alert create \
  --name "High Response Time" \
  --resource-group $PROD_RG \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$PROD_RG/providers/Microsoft.App/containerApps/$CONTAINER_APP_NAME_PROD \
  --condition "avg Requests > 1000" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action lankaconnect-alerts

# Alert 3: Database connection pool exhaustion
az monitor metrics alert create \
  --name "Database Connection Pool Alert" \
  --resource-group $PROD_RG \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$PROD_RG/providers/Microsoft.DBforPostgreSQL/flexibleServers/$POSTGRES_SERVER_PROD \
  --condition "avg active_connections > 80" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action lankaconnect-alerts
```

### Step 5.3: View Logs

```bash
# Stream Container App logs
az containerapp logs show \
  --name $CONTAINER_APP_NAME \
  --resource-group $STAGING_RG \
  --follow

# Query logs using Kusto (Log Analytics)
az monitor log-analytics query \
  --workspace $LOG_ANALYTICS_WORKSPACE_ID \
  --analytics-query "ContainerAppConsoleLogs_CL | where TimeGenerated > ago(1h) | limit 100" \
  --output table

# View database slow queries
az postgres flexible-server execute \
  --name $POSTGRES_SERVER_PROD \
  --resource-group $PROD_RG \
  --admin-user adminuser \
  --admin-password "PASSWORD" \
  --database-name postgres \
  --querytext "SELECT query, calls, total_time, mean_time FROM pg_stat_statements ORDER BY mean_time DESC LIMIT 10;"
```

---

## Troubleshooting

### Issue 1: Container App fails to start

**Symptoms:** Container App stuck in "Provisioning" state

**Solution:**
```bash
# Check container logs
az containerapp logs show --name $CONTAINER_APP_NAME --resource-group $STAGING_RG --tail 100

# Common causes:
# 1. Database connection string incorrect
# 2. Missing Key Vault secrets
# 3. Dockerfile ENTRYPOINT wrong

# Verify Key Vault access
az keyvault secret show --vault-name $KEY_VAULT_NAME --name DATABASE-CONNECTION-STRING

# Verify managed identity has Key Vault permissions
az keyvault show --name $KEY_VAULT_NAME --query properties.accessPolicies
```

### Issue 2: Database connection timeout

**Symptoms:** `Npgsql.NpgsqlException: Timeout during connection attempt`

**Solution:**
```bash
# Check firewall rules
az postgres flexible-server firewall-rule list \
  --resource-group $STAGING_RG \
  --name $POSTGRES_SERVER

# Verify Container Apps can reach database
az containerapp exec \
  --name $CONTAINER_APP_NAME \
  --resource-group $STAGING_RG \
  --command "ping -c 4 ${POSTGRES_SERVER}.postgres.database.azure.com"

# Check connection pool settings
az postgres flexible-server parameter show \
  --resource-group $STAGING_RG \
  --server-name $POSTGRES_SERVER \
  --name max_connections
```

### Issue 3: Entra External ID authentication failing

**Symptoms:** `401 Unauthorized` when calling `/api/auth/login/entra`

**Solution:**
```bash
# Verify Entra configuration in Key Vault
az keyvault secret show --vault-name $KEY_VAULT_NAME --name ENTRA-TENANT-ID
az keyvault secret show --vault-name $KEY_VAULT_NAME --name ENTRA-CLIENT-ID
az keyvault secret show --vault-name $KEY_VAULT_NAME --name ENTRA-AUDIENCE

# Check application logs for JWT validation errors
az containerapp logs show --name $CONTAINER_APP_NAME --resource-group $STAGING_RG --tail 50 | grep "Entra"

# Common issues:
# 1. Token expired (check clock skew)
# 2. Audience mismatch (should be api://YOUR_CLIENT_ID)
# 3. Token issued by wrong tenant
```

### Issue 4: GitHub Actions deployment fails

**Symptoms:** CI/CD pipeline fails during deployment

**Solution:**
```bash
# Check service principal permissions
az ad sp show --id YOUR_SERVICE_PRINCIPAL_ID

# Verify GitHub secrets are correct
# Settings → Secrets → Actions → Check AZURE_CREDENTIALS_STAGING

# Check ACR credentials
az acr credential show --name $ACR_NAME --resource-group $STAGING_RG

# Manually deploy to test
az containerapp update \
  --name $CONTAINER_APP_NAME \
  --resource-group $STAGING_RG \
  --image ${ACR_NAME}.azurecr.io/lankaconnect-api:staging
```

---

## Rollback Procedures

### Rollback Container App Deployment

```bash
# List revisions
az containerapp revision list \
  --name $CONTAINER_APP_NAME \
  --resource-group $STAGING_RG \
  --output table

# Activate previous revision
az containerapp revision activate \
  --name $CONTAINER_APP_NAME \
  --resource-group $STAGING_RG \
  --revision lankaconnect-api-staging--<PREVIOUS_REVISION_NAME>

# Deactivate current revision (optional)
az containerapp revision deactivate \
  --name $CONTAINER_APP_NAME \
  --resource-group $STAGING_RG \
  --revision lankaconnect-api-staging--<CURRENT_REVISION_NAME>
```

### Rollback Database Migration

```bash
# Stop Container App (prevent new writes)
az containerapp update \
  --name $CONTAINER_APP_NAME \
  --resource-group $STAGING_RG \
  --min-replicas 0 \
  --max-replicas 0

# Restore from backup (Point-in-Time)
az postgres flexible-server restore \
  --resource-group $STAGING_RG \
  --name lankaconnect-staging-db-restored \
  --source-server $POSTGRES_SERVER \
  --restore-time "2025-10-28T09:00:00Z"

# Update connection string in Key Vault
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name DATABASE-CONNECTION-STRING \
  --value "Host=lankaconnect-staging-db-restored.postgres.database.azure.com;..."

# Restart Container App
az containerapp update \
  --name $CONTAINER_APP_NAME \
  --resource-group $STAGING_RG \
  --min-replicas 1 \
  --max-replicas 3
```

---

## Cost Management

### Daily Cost Monitoring

```bash
# Get daily cost estimate
az consumption usage list \
  --start-date 2025-10-01 \
  --end-date 2025-10-28 \
  --output table

# Set budget alerts
az consumption budget create \
  --budget-name lankaconnect-staging-budget \
  --amount 100 \
  --time-grain Monthly \
  --start-date 2025-10-01 \
  --end-date 2026-10-01 \
  --resource-group $STAGING_RG
```

### Cost Optimization Tips

1. Scale down staging during off-hours:
   ```bash
   # Stop staging at night (automated with Azure Automation)
   az containerapp update --name $CONTAINER_APP_NAME --resource-group $STAGING_RG --min-replicas 0
   ```

2. Use Azure Reserved Instances for production (30% savings)
3. Enable autoscaling based on HTTP requests (not CPU)
4. Use Azure CDN for static assets (reduce bandwidth costs)

---

## Next Steps

1. Schedule production deployment after 1 week of staging validation
2. Configure custom domain and SSL certificate
3. Setup monitoring dashboards in Azure Portal
4. Document operational runbooks for on-call team
5. Plan disaster recovery (multi-region deployment)

---

## Support

**Azure Support:** https://azure.microsoft.com/support
**Documentation:** https://learn.microsoft.com/azure/container-apps
**LankaConnect Team:** devops@lankaconnect.com

---

**Document Version:** 1.0
**Last Updated:** 2025-10-28
**Next Review:** After first staging deployment
