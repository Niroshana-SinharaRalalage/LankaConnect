# LankaConnect - Azure Staging Deployment Quick Start

**Last Updated:** 2025-10-28
**Estimated Time:** 90 minutes (70 minutes automated)
**Target:** Staging environment deployment

---

## 🚀 Quick Start (4 Simple Steps)

### Prerequisites Checklist

- [ ] **Azure subscription** with Contributor access
- [ ] **Azure CLI** installed (`az --version` shows 2.50.0+)
- [ ] **Git repository** cloned locally
- [ ] **PostgreSQL client** installed (for migration)
- [ ] **Bash shell** (Git Bash on Windows, native on Mac/Linux)

---

## Step 1: Azure Login (2 minutes)

```bash
# Login to Azure
az login

# List subscriptions
az account list --output table

# Set the subscription you want to use
az account set --subscription "YOUR_SUBSCRIPTION_NAME_OR_ID"

# Verify current subscription
az account show --output table
```

**Expected Output:**
```
Name                         SubscriptionId                        State
---------------------------  ------------------------------------  -------
Your Subscription Name       12345678-1234-1234-1234-123456789012  Enabled
```

**✅ Checkpoint:** You should see your subscription marked as current.

---

## Step 2: Run Provisioning Script (45 minutes automated)

```bash
# Navigate to project directory
cd C:\Work\LankaConnect

# Make script executable (if on Mac/Linux)
chmod +x scripts/azure/provision-staging.sh

# Run provisioning script
./scripts/azure/provision-staging.sh
```

### What Happens During Provisioning?

**Minute 0-2:** Pre-flight checks
- ✅ Verify Azure CLI installed
- ✅ Verify logged in to Azure
- ✅ Show current subscription
- ✅ Prompt for confirmation

**Minute 2-3:** Password setup
- 📝 Enter PostgreSQL admin password (min 8 chars, strong)
- 📝 Confirm password
- **IMPORTANT:** Save this password securely!

**Minute 3-5:** Resource Group & Container Registry
- 🔨 Create resource group: `lankaconnect-staging`
- 🔨 Create container registry: `lankaconnectstaging.azurecr.io`
- 🔨 Enable admin user for GitHub Actions

**Minute 5-15:** PostgreSQL Database
- 🔨 Create PostgreSQL Flexible Server (Burstable B1ms)
- 🔨 Create database: `lankaconnectdb`
- 🔨 Configure firewall rules (allow Azure services)
- ⏳ This is the longest step (10 minutes)

**Minute 15-20:** Key Vault & Secrets
- 🔨 Create Key Vault: `lankaconnect-staging-kv`
- 🔨 Store 14 secrets (connection strings, JWT keys, Entra config)
- 🔨 Enable soft-delete (30-day recovery)

**Minute 20-30:** Container Apps Environment
- 🔨 Create Log Analytics workspace
- 🔨 Create Container Apps environment
- 🔨 Configure monitoring and logging

**Minute 30-45:** Container App Deployment
- 🔨 Create Container App: `lankaconnect-api-staging`
- 🔨 Configure auto-scaling (1-3 replicas)
- 🔨 Setup ingress (HTTPS on port 5000)
- 🔨 Grant Managed Identity access to Key Vault
- 🔨 Configure environment variables from Key Vault

**Minute 45:** Summary & Next Steps
```
════════════════════════════════════════════════════════════════
✅ Azure Staging Environment Provisioning Complete!
════════════════════════════════════════════════════════════════
Resource Group:     lankaconnect-staging
Container Registry: lankaconnectstaging.azurecr.io
PostgreSQL Server:  lankaconnect-staging-db.postgres.database.azure.com
Key Vault:          lankaconnect-staging-kv
Container App URL:  https://lankaconnect-api-staging.blueriver-a1b2c3d4.eastus.azurecontainerapps.io

Next Steps:
1. Apply database migration
2. Configure GitHub Secrets
3. Push to develop branch to deploy
```

### 📝 Save These Values!

**Copy and save somewhere secure:**

```
Container App URL: _________________________________________________
PostgreSQL Server: _________________________________________________
PostgreSQL Password: _______________________________________________
ACR Username: ______________________________________________________
ACR Password: ______________________________________________________
Azure Service Principal: ___________________________________________
```

**✅ Checkpoint:** Provisioning script completed with success message.

---

## Step 3: Apply Database Migration (5 minutes)

### 3.1 Get PostgreSQL Connection String

```bash
# Get connection string from Key Vault
az keyvault secret show \
  --vault-name lankaconnect-staging-kv \
  --name DATABASE-CONNECTION-STRING \
  --query value -o tsv
```

**Expected Output:**
```
Host=lankaconnect-staging-db.postgres.database.azure.com;Port=5432;Database=lankaconnectdb;Username=adminuser;Password=YOUR_PASSWORD;SSL Mode=Require
```

### 3.2 Apply Migration

```bash
# Navigate to project directory
cd C:\Work\LankaConnect

# Apply migration using psql
psql "Host=lankaconnect-staging-db.postgres.database.azure.com;Port=5432;Database=lankaconnectdb;Username=adminuser;Password=YOUR_PASSWORD;SSL Mode=Require" \
  -f docs/deployment/migrations/20251028_AddEntraExternalIdSupport.sql
```

**Expected Output:**
```
ALTER TABLE
CREATE INDEX
CREATE INDEX
CREATE INDEX
```

### 3.3 Verify Migration

```sql
-- Connect to database
psql "CONNECTION_STRING"

-- Check migration applied
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'Users'
  AND column_name IN ('IdentityProvider', 'ExternalProviderId');

-- Expected output:
--  column_name       | data_type
-- -------------------+-----------
--  IdentityProvider  | integer
--  ExternalProviderId| character varying
```

**✅ Checkpoint:** Migration applied, columns exist in Users table.

---

## Step 4: Configure GitHub & Deploy (20 minutes)

### 4.1 Get Azure Credentials for GitHub Actions

```bash
# Create service principal for GitHub Actions
az ad sp create-for-rbac \
  --name "LankaConnect-Staging-GitHub" \
  --role contributor \
  --scopes /subscriptions/$(az account show --query id -o tsv)/resourceGroups/lankaconnect-staging \
  --sdk-auth

# Copy the ENTIRE JSON output (starts with {, ends with })
```

**Expected Output:**
```json
{
  "clientId": "12345678-1234-1234-1234-123456789012",
  "clientSecret": "your-secret-here",
  "subscriptionId": "87654321-4321-4321-4321-210987654321",
  "tenantId": "abcdefab-abcd-abcd-abcd-abcdefabcdef",
  ...
}
```

**⚠️ COPY THIS ENTIRE JSON - YOU'LL NEED IT FOR GITHUB!**

### 4.2 Get ACR Credentials

```bash
# Get ACR username
az acr credential show \
  --name lankaconnectstaging \
  --query username -o tsv

# Get ACR password
az acr credential show \
  --name lankaconnectstaging \
  --query "passwords[0].value" -o tsv
```

### 4.3 Add GitHub Secrets

**Navigate to:** https://github.com/Niroshana-SinharaRalalage/LankaConnect/settings/secrets/actions

**Click "New repository secret" and add each:**

1. **Name:** `AZURE_CREDENTIALS_STAGING`
   - **Value:** (paste entire JSON from Step 4.1)

2. **Name:** `ACR_USERNAME_STAGING`
   - **Value:** (paste ACR username from Step 4.2)

3. **Name:** `ACR_PASSWORD_STAGING`
   - **Value:** (paste ACR password from Step 4.2)

**✅ Checkpoint:** All 3 secrets added to GitHub.

### 4.4 Trigger Deployment

**Option A: Push to develop branch**
```bash
# Push current master to develop (triggers deployment)
git push origin master:develop
```

**Option B: Manual trigger via GitHub UI**
1. Navigate to: https://github.com/Niroshana-SinharaRalalage/LankaConnect/actions
2. Click "Deploy to Azure Staging"
3. Click "Run workflow"
4. Select `develop` branch
5. Click "Run workflow" button

**Monitor Progress:**
- URL: https://github.com/Niroshana-SinharaRalalage/LankaConnect/actions
- Watch for: Build → Test → Docker → Deploy → Smoke Test
- Duration: 10-15 minutes

**✅ Checkpoint:** GitHub Actions workflow completes with green checkmark.

---

## 🎯 Validation (5 minutes)

### Test 1: Health Check

```bash
# Get Container App URL
STAGING_URL=$(az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.configuration.ingress.fqdn' -o tsv)

# Test health endpoint
curl https://$STAGING_URL/health
```

**Expected Response:**
```json
{
  "status": "Healthy",
  "service": "Authentication",
  "timestamp": "2025-10-28T22:30:00Z"
}
```

### Test 2: Entra Login Endpoint

```bash
# Test Entra endpoint (should return 400/401 for invalid token)
curl -X POST https://$STAGING_URL/api/auth/login/entra \
  -H "Content-Type: application/json" \
  -d '{"accessToken":"invalid","ipAddress":"127.0.0.1"}'
```

**Expected Response (400 or 401):**
```json
{
  "error": "Invalid access token"
}
```

### Test 3: Swagger UI

```bash
# Open Swagger UI in browser
echo "https://$STAGING_URL/swagger"
```

**Navigate to the URL and verify:**
- ✅ Swagger UI loads
- ✅ All endpoints documented
- ✅ Can execute test requests

**✅ Checkpoint:** All endpoints responding correctly.

---

## 🎉 Success!

**You now have a live staging environment at:**
```
https://lankaconnect-api-staging.<random>.eastus.azurecontainerapps.io
```

**Available Endpoints:**
- `/health` - Health check
- `/api/auth/login/entra` - Entra External ID login
- `/api/auth/register` - User registration
- `/api/auth/login` - Local login
- `/swagger` - API documentation
- All other API endpoints from your application

---

## 📊 Verify Costs

```bash
# View current costs
az consumption usage list \
  --resource-group lankaconnect-staging \
  --start-date $(date -d '7 days ago' +%Y-%m-%d) \
  --end-date $(date +%Y-%m-%d)

# Set up budget alert
az consumption budget create \
  --budget-name "LankaConnect-Staging-Budget" \
  --resource-group lankaconnect-staging \
  --amount 60 \
  --time-grain Monthly
```

**Expected Monthly Cost:** $45-55

---

## 🚨 Troubleshooting

### Issue: "PostgreSQL password too weak"
```bash
# Password must have:
# - At least 8 characters
# - Uppercase letters (A-Z)
# - Lowercase letters (a-z)
# - Numbers (0-9)
# - Special characters (!@#$%^&*)

# Example strong password: MyP@ssw0rd123!
```

### Issue: "Resource group already exists"
```bash
# Delete existing resource group
az group delete --name lankaconnect-staging --yes --no-wait

# Wait 5 minutes, then re-run provisioning script
./scripts/azure/provision-staging.sh
```

### Issue: "GitHub Actions fails on tests"
```bash
# Run tests locally to identify failures
dotnet test tests/LankaConnect.Application.Tests/

# Fix failures, commit, and push again
git add .
git commit -m "fix: Resolve test failures"
git push origin master:develop
```

### Issue: "Container App not starting"
```bash
# Check logs
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 100 --follow

# Common causes:
# - Missing environment variable (check Key Vault secrets)
# - Database migration not applied
# - Invalid connection string
```

---

## 📝 Next Steps

**After successful staging deployment:**

1. **Load Testing** (Week 2)
   - Test with 100+ concurrent users
   - Monitor performance and auto-scaling

2. **Security Audit** (Week 2)
   - Verify Key Vault access policies
   - Check network security groups
   - Review HTTPS configuration

3. **Cost Monitoring** (Ongoing)
   - Verify staying within $50/month budget
   - Set up daily cost alerts

4. **Production Deployment** (Week 4+)
   - Run `provision-production.sh` (to be created)
   - Apply same process with production configuration
   - Enable zone-redundant HA for database

---

## 🎯 Summary Checklist

- [ ] **Azure CLI** logged in and subscription selected
- [ ] **Provisioning script** ran successfully (45 minutes)
- [ ] **PostgreSQL password** saved securely
- [ ] **Database migration** applied successfully
- [ ] **GitHub Secrets** configured (3 secrets)
- [ ] **Deployment** triggered and completed (green checkmark)
- [ ] **Health endpoint** returns 200 OK
- [ ] **Entra endpoint** responds correctly (400/401)
- [ ] **Swagger UI** accessible
- [ ] **Costs** within budget ($45-55/month)
- [ ] **Staging URL** saved and accessible

**Total Time:** ~90 minutes from start to finish

---

## 📞 Support

**Issue Tracker:** https://github.com/Niroshana-SinharaRalalage/LankaConnect/issues
**Azure Support:** https://azure.microsoft.com/support
**Documentation:** `docs/deployment/AZURE_DEPLOYMENT_GUIDE.md`

---

**Deployment completed by:** _______________________
**Date:** _______________________
**Staging URL:** _______________________
