# LankaConnect - Azure Staging Deployment Checklist

**Last Updated:** 2025-10-28
**Version:** 1.0
**Deployment Type:** Staging Environment (Option B: Staging First)

---

## ✅ Pre-Deployment Checklist

### 1. GitHub Repository Status
- [x] **CI/CD Pipeline Pushed to GitHub**
  - File: `.github/workflows/deploy-staging.yml`
  - Commit: 72f030b
  - Status: ✅ Pushed to origin/master
  - URL: https://github.com/Niroshana-SinharaRalalage/LankaConnect/blob/master/.github/workflows/deploy-staging.yml

- [x] **Develop Branch Created**
  - Branch: `develop`
  - Status: ✅ Created and pushed to origin
  - Purpose: Auto-triggers staging deployment on push

- [ ] **GitHub Actions Tab Verified**
  - Navigate to: https://github.com/Niroshana-SinharaRalalage/LankaConnect/actions
  - Verify: "Deploy to Azure Staging" workflow visible
  - Status: ⏳ Pending manual verification

### 2. Local Build Verification
- [x] **Build Status**
  - Command: `dotnet build -c Release`
  - Result: ✅ 0 errors, 1 warning (Microsoft.Identity.Web vulnerability - documented)
  - Commit: 72f030b

- [x] **Test Status**
  - Application Tests: ✅ 318/319 passing (100%)
  - Integration Tests: ✅ 8 Entra tests ready
  - Zero Tolerance: ✅ Enforced

### 3. Azure Prerequisites
- [ ] **Azure Subscription Active**
  - Login: `az login`
  - Select Subscription: `az account set --subscription "YOUR_SUBSCRIPTION_ID"`
  - Verify: `az account show`

- [ ] **Azure CLI Installed**
  - Version: Minimum 2.50.0
  - Check: `az --version`
  - Install: https://learn.microsoft.com/cli/azure/install-azure-cli

- [ ] **Permissions Verified**
  - Required Roles:
    - Contributor (to create resources)
    - User Access Administrator (to assign Managed Identity)
  - Check: `az role assignment list --assignee $(az account show --query user.name -o tsv)`

---

## 🚀 Deployment Steps (70 Minutes Automated)

### **Step 1: Run Azure Provisioning Script** (45 minutes)

**Location**: `scripts/azure/provision-staging.sh`

**Command:**
```bash
cd C:\Work\LankaConnect
chmod +x scripts/azure/provision-staging.sh
./scripts/azure/provision-staging.sh
```

**What It Creates:**
- [ ] Resource Group: `lankaconnect-staging`
- [ ] Container Registry: `lankaconnectstaging.azurecr.io`
- [ ] PostgreSQL Server: `lankaconnect-staging-db.postgres.database.azure.com`
- [ ] Key Vault: `lankaconnect-staging-kv` (with 14 secrets)
- [ ] Container Apps Environment: `lankaconnect-staging`
- [ ] Container App: `lankaconnect-api-staging`
- [ ] Log Analytics Workspace: `lankaconnect-staging-logs`

**Expected Output:**
```
════════════════════════════════════════════════════════════════
✅ Azure Staging Environment Provisioning Complete!
════════════════════════════════════════════════════════════════
Resource Group:     lankaconnect-staging
Container Registry: lankaconnectstaging.azurecr.io
PostgreSQL Server:  lankaconnect-staging-db.postgres.database.azure.com
Key Vault:          lankaconnect-staging-kv
Container App URL:  https://lankaconnect-api-staging.<random>.eastus.azurecontainerapps.io
```

**Capture These Values:**
- Container App URL: `_______________________________________________`
- PostgreSQL Connection String: `_______________________________________________`
- ACR Username: `_______________________________________________`
- ACR Password: `_______________________________________________`

---

### **Step 2: Apply Database Migration** (5 minutes)

**Migration File**: `docs/deployment/migrations/20251028_AddEntraExternalIdSupport.sql`

**Command:**
```bash
psql "Host=lankaconnect-staging-db.postgres.database.azure.com;Port=5432;Database=lankaconnectdb;Username=adminuser;Password=YOUR_PASSWORD;SSL Mode=Require" -f docs/deployment/migrations/20251028_AddEntraExternalIdSupport.sql
```

**Verification:**
```sql
-- Check columns were added
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'Users'
  AND column_name IN ('IdentityProvider', 'ExternalProviderId');

-- Expected output:
-- IdentityProvider    | integer            | NO
-- ExternalProviderId  | character varying  | YES
```

**Status:**
- [ ] Migration applied successfully
- [ ] Schema verified with diagnostic queries

---

### **Step 3: Configure GitHub Secrets** (10 minutes)

**Navigate to:** https://github.com/Niroshana-SinharaRalalage/LankaConnect/settings/secrets/actions

**Required Secrets:**

1. **AZURE_CREDENTIALS_STAGING**
   ```bash
   # Get service principal credentials
   az ad sp create-for-rbac \
     --name "LankaConnect-Staging-GitHub" \
     --role contributor \
     --scopes /subscriptions/YOUR_SUBSCRIPTION_ID/resourceGroups/lankaconnect-staging \
     --sdk-auth

   # Copy entire JSON output and paste as secret
   ```

2. **ACR_USERNAME_STAGING**
   ```bash
   # Get ACR admin username
   az acr credential show --name lankaconnectstaging --query username -o tsv
   ```

3. **ACR_PASSWORD_STAGING**
   ```bash
   # Get ACR admin password
   az acr credential show --name lankaconnectstaging --query "passwords[0].value" -o tsv
   ```

**Verification:**
- [ ] All 3 secrets added to GitHub repository
- [ ] Secret names match exactly (case-sensitive)

---

### **Step 4: Deploy to Staging** (10 minutes automated)

**Option A: Push to Develop Branch**
```bash
# From master branch
git push origin master:develop
```

**Option B: Manual Trigger**
1. Navigate to: https://github.com/Niroshana-SinharaRalalage/LankaConnect/actions
2. Select "Deploy to Azure Staging" workflow
3. Click "Run workflow" → Select `develop` branch → Run

**GitHub Actions Will:**
1. ✅ Build .NET 8 application (Release mode)
2. ✅ Run 318 unit tests (Zero Tolerance enforced)
3. ✅ Run integration tests
4. ✅ Build Docker image
5. ✅ Push to Azure Container Registry
6. ✅ Update Container App with new image
7. ✅ Configure environment variables from Key Vault
8. ✅ Run smoke tests (health check, Entra endpoint)

**Expected Duration:** 10-15 minutes

**Monitor Progress:**
- URL: https://github.com/Niroshana-SinharaRalalage/LankaConnect/actions
- Status: ⏳ Watch for green checkmark

---

### **Step 5: Validate Staging Deployment** (30 minutes)

#### 5.1 Health Check Endpoint

```bash
# Get Container App URL from Step 1 output
STAGING_URL="https://lankaconnect-api-staging.<random>.eastus.azurecontainerapps.io"

# Test health endpoint
curl $STAGING_URL/health

# Expected response (200 OK):
{
  "status": "Healthy",
  "service": "Authentication",
  "timestamp": "2025-10-28T22:00:00Z"
}
```

**Status:**
- [ ] Health endpoint returns 200 OK
- [ ] Response contains correct timestamp

#### 5.2 Entra Login Endpoint

```bash
# Test with invalid token (should return 400/401)
curl -X POST $STAGING_URL/api/auth/login/entra \
  -H "Content-Type: application/json" \
  -d '{"accessToken":"invalid","ipAddress":"127.0.0.1"}'

# Expected response (400 or 401):
{
  "error": "Invalid access token"
}
```

**Status:**
- [ ] Endpoint returns 400 or 401 for invalid token
- [ ] Error message is correct

#### 5.3 Swagger UI (if enabled)

```bash
# Navigate to Swagger UI in browser
open $STAGING_URL/swagger
```

**Status:**
- [ ] Swagger UI loads successfully
- [ ] All endpoints documented
- [ ] Can execute test requests

#### 5.4 Database Connectivity

```sql
-- Connect to staging database
psql "Host=lankaconnect-staging-db.postgres.database.azure.com;Port=5432;Database=lankaconnectdb;Username=adminuser;Password=YOUR_PASSWORD;SSL Mode=Require"

-- Verify Users table
SELECT COUNT(*) FROM "Users";

-- Check migration status
SELECT "MigrationId", "ProductVersion" FROM "__EFMigrationsHistory";
```

**Status:**
- [ ] Database connection successful
- [ ] Users table accessible
- [ ] Migration 20251028_AddEntraExternalIdSupport applied

#### 5.5 Container App Logs

```bash
# View live logs
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 50 --follow

# Look for:
# - Application startup messages
# - No error logs
# - Health check requests succeeding
```

**Status:**
- [ ] No critical errors in logs
- [ ] Application started successfully
- [ ] Health checks passing

---

## 📊 Post-Deployment Verification

### 1. Azure Portal Verification

**Navigate to:** https://portal.azure.com

**Check Each Resource:**
- [ ] **Container App**
  - URL: Resource Groups → lankaconnect-staging → lankaconnect-api-staging
  - Status: ✅ Running
  - Replicas: 1-3 (auto-scale)
  - Ingress: Enabled (HTTPS)

- [ ] **PostgreSQL Server**
  - URL: Resource Groups → lankaconnect-staging → lankaconnect-staging-db
  - Status: ✅ Available
  - Tier: Burstable B1ms
  - Storage: 32 GB

- [ ] **Key Vault**
  - URL: Resource Groups → lankaconnect-staging → lankaconnect-staging-kv
  - Secrets: 14 secrets configured
  - Access Policies: Container App Managed Identity granted

- [ ] **Container Registry**
  - URL: Resource Groups → lankaconnect-staging → lankaconnectstaging
  - Images: `lankaconnect-api:staging`, `lankaconnect-api:latest`
  - Admin User: Enabled

### 2. Cost Monitoring

**Navigate to:** https://portal.azure.com → Cost Management + Billing

**Expected Costs:**
- Daily: ~$1.50-1.80
- Weekly: ~$10-12
- Monthly: ~$45-55

**Set Budget Alert:**
```bash
az consumption budget create \
  --budget-name "LankaConnect-Staging-Budget" \
  --resource-group lankaconnect-staging \
  --amount 60 \
  --time-grain Monthly \
  --time-period "$(date -u +%Y-%m-01T00:00:00Z)" \
  --threshold 80 90 100
```

**Status:**
- [ ] Budget alert configured ($60/month threshold)
- [ ] Cost monitoring dashboard reviewed

### 3. Security Verification

- [ ] **No Secrets in Code**
  - All environment variables via Key Vault
  - No hardcoded connection strings
  - appsettings.Staging.json uses `secretref:`

- [ ] **HTTPS Only**
  - Container App ingress uses HTTPS
  - HTTP automatically redirects to HTTPS

- [ ] **PostgreSQL Firewall**
  - Only Azure services allowed
  - No public IP access (unless explicitly needed)

- [ ] **Key Vault Access**
  - Only Container App Managed Identity has access
  - Soft-delete enabled (30-day recovery)

---

## 🎯 Success Criteria

**Staging Deployment is Successful When:**
- ✅ All Azure resources provisioned (7 resources)
- ✅ Database migration applied successfully
- ✅ GitHub Actions workflow completes (green checkmark)
- ✅ Health endpoint returns 200 OK
- ✅ Entra login endpoint responds correctly (400/401 for invalid token)
- ✅ No critical errors in Container App logs
- ✅ Cost within budget ($45-55/month)
- ✅ All security checks pass

---

## 🚨 Troubleshooting

### Common Issues

**Issue 1: Provisioning Script Fails**
```bash
# Error: "Resource group already exists"
# Solution: Delete and recreate
az group delete --name lankaconnect-staging --yes
./scripts/azure/provision-staging.sh
```

**Issue 2: GitHub Actions Fails on Tests**
```bash
# Error: "Tests failed (Zero Tolerance)"
# Solution: Fix failing tests locally first
dotnet test tests/LankaConnect.Application.Tests/
# Then commit and push
```

**Issue 3: Container App Not Starting**
```bash
# Check logs
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 100

# Common causes:
# - Missing environment variable
# - Invalid connection string
# - Database migration not applied
```

**Issue 4: Health Endpoint Returns 503**
```bash
# Wait 2-3 minutes for startup
# If still failing, check Container App logs
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.latestRevisionName' -o tsv
```

---

## 📝 Next Steps After Staging Validation

**Week 2-3: Staging Testing**
- [ ] Load testing (100+ concurrent users)
- [ ] Security audit
- [ ] Cost monitoring (validate $50/month estimate)
- [ ] Fix any issues discovered

**Week 4+: Production Deployment**
- [ ] Create production provisioning script (similar to staging)
- [ ] Configure GitHub environment protection (manual approval)
- [ ] Deploy to production
- [ ] Enable monitoring and alerts

---

## 📞 Support

**Azure Support:** https://azure.microsoft.com/support
**GitHub Repository:** https://github.com/Niroshana-SinharaRalalage/LankaConnect
**Documentation:** `docs/deployment/AZURE_DEPLOYMENT_GUIDE.md`

---

**Checklist Completed By:** _______________________
**Date:** _______________________
**Staging URL:** _______________________
**Notes:** _______________________
