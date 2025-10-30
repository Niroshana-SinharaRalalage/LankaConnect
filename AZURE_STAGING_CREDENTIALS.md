# Azure Staging Environment - Credentials & URLs

**Created**: 2025-10-29
**Subscription**: Azure subscription 1 (ebb8304a-6374-4db0-8de5-e8678afbb5b5)
**Region**: East US 2

---

## 🔐 Container Registry (ACR)

**Registry Name**: `lankaconnectstaging`
**Login Server**: `lankaconnectstaging.azurecr.io`
**Username**: `lankaconnectstaging`
**Password**: `5IdJdlTETmEi4OYLgQ4izVX+jQjTCfbb18+kLVNM7k+ACRCBymn3`

### GitHub Secrets Required:
```
ACR_USERNAME_STAGING=lankaconnectstaging
ACR_PASSWORD_STAGING=5IdJdlTETmEi4OYLgQ4izVX+jQjTCfbb18+kLVNM7k+ACRCBymn3
```

---

## 🗄️ PostgreSQL Database

**Server**: `lankaconnect-staging-db.postgres.database.azure.com`
**Database**: `LankaConnectDB`
**Admin Username**: `adminuser`
**Admin Password**: `1qaz!QAZ` *(save this securely!)*
**Tier**: Standard_B1ms (Burstable, 1 vCPU, 2 GB RAM)
**SSL Mode**: Required

### Connection String:
```
Host=lankaconnect-staging-db.postgres.database.azure.com;Database=LankaConnectDB;Username=adminuser;Password=1qaz!QAZ;SslMode=Require;Pooling=true;MinPoolSize=2;MaxPoolSize=20
```

### Connect via psql:
```bash
psql "host=lankaconnect-staging-db.postgres.database.azure.com port=5432 dbname=LankaConnectDB user=adminuser password=1qaz!QAZ sslmode=require"
```

---

## 🔑 Azure Key Vault

**Key Vault Name**: `lankaconnect-staging-kv`
**Key Vault URL**: `https://lankaconnect-staging-kv.vault.azure.net/`
**Secrets Stored**: 14 secrets (database, JWT, Entra, SMTP, storage)

### Managed Identity Access:
- **Principal ID**: `bf69f3f8-e9c6-464a-9d1f-0038f90e8d03`
- **Permissions**: Get, List secrets

---

## 🚀 Container App

**App Name**: `lankaconnect-api-staging`
**Public URL**: `https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/`
**Environment**: `lankaconnect-staging-env2`
**Status**: Running ✅

### Configuration:
- **CPU**: 0.25 vCPU
- **Memory**: 0.5 GB
- **Replicas**: 1-3 (auto-scaling)
- **Port**: 80
- **Ingress**: External
- **Current Image**: `mcr.microsoft.com/azuredocs/containerapps-helloworld:latest` (placeholder)

### Managed Identity:
- **Type**: System-assigned
- **Principal ID**: `bf69f3f8-e9c6-464a-9d1f-0038f90e8d03`
- **Tenant ID**: `369a3c47-33b7-4baa-98b8-6ddf16a51a31`

---

## 🔐 Microsoft Entra External ID Configuration

**Tenant ID**: `369a3c47-33b7-4baa-98b8-6ddf16a51a31`
**Client ID**: `957e9865-fca0-4236-9276-a8643a7193b5`
**Instance URL**: `https://lankaconnect.ciamlogin.com/`

---

## 📊 Monitoring & Logs

**Log Analytics Workspace**: `workspace-lankaconnectstagingoue8`
**Workspace ID**: `dc92fcf2-7f80-4e1d-b391-fdadac65befe`

### View Logs:
```bash
az containerapp logs show --name lankaconnect-api-staging --resource-group lankaconnect-staging --follow
```

---

## 💰 Cost Estimate

**Monthly Cost**: ~$50/month
- Container Registry (Basic): $5
- PostgreSQL B1ms: $12
- Container App (0.25 vCPU, 0.5 GB, 1-3 replicas): $16
- Key Vault: $3
- Log Analytics: $8
- Bandwidth: $6

---

## 📝 Next Steps

### 1. Apply Database Migration

```bash
# Download connection details from Key Vault or use direct connection
psql "host=lankaconnect-staging-db.postgres.database.azure.com port=5432 dbname=LankaConnectDB user=adminuser password=1qaz!QAZ sslmode=require" -f docs/deployment/migrations/20251028_AddEntraExternalIdSupport.sql
```

### 2. Configure GitHub Secrets

Go to: https://github.com/Niroshana-SinharaRalalage/LankaConnect/settings/secrets/actions

Add these 3 secrets:

```
ACR_USERNAME_STAGING=lankaconnectstaging
ACR_PASSWORD_STAGING=5IdJdlTETmEi4OYLgQ4izVX+jQjTCfbb18+kLVNM7k+ACRCBymn3
AZURE_CREDENTIALS_STAGING={
  "clientId": "<service-principal-client-id>",
  "clientSecret": "<service-principal-secret>",
  "subscriptionId": "ebb8304a-6374-4db0-8de5-e8678afbb5b5",
  "tenantId": "369a3c47-33b7-4baa-98b8-6ddf16a51a31"
}
```

Note: For AZURE_CREDENTIALS_STAGING, you need to create a Service Principal. See: https://docs.microsoft.com/azure/container-apps/github-actions

### 3. Deploy via GitHub Actions

```bash
# Push to develop branch to trigger deployment
git push origin master:develop
```

GitHub Actions will:
1. Build .NET application
2. Run all 318 tests (Zero Tolerance enforced)
3. Build Docker image
4. Push to ACR
5. Update Container App
6. Run smoke tests

---

## 🧪 Test the Deployment

**Current Status**: Hello World placeholder app running
**Test URL**: https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/

Once deployed via GitHub Actions, you'll have:
- `/health` - Health check endpoint
- `/swagger` - API documentation
- `/api/auth/login-entra` - Entra External ID login

---

## 🆘 Troubleshooting

### View Container Logs:
```bash
az containerapp logs show --name lankaconnect-api-staging --resource-group lankaconnect-staging --follow
```

### Check Container Status:
```bash
az containerapp show --name lankaconnect-api-staging --resource-group lankaconnect-staging --query "properties.runningStatus"
```

### Restart Container:
```bash
az containerapp revision restart --name lankaconnect-api-staging --resource-group lankaconnect-staging
```

---

## 🔒 Security Notes

1. **PostgreSQL Password**: Stored in Key Vault as `DATABASE-CONNECTION-STRING`
2. **ACR Password**: Stored as GitHub Secret
3. **Managed Identity**: Container App has automatic access to Key Vault secrets
4. **Firewall**: PostgreSQL allows Azure services only
5. **HTTPS**: All traffic encrypted via Azure Container Apps ingress

---

**Status**: ✅ Infrastructure 100% Ready
**Next Action**: Apply database migration + Configure GitHub Secrets + Deploy!
