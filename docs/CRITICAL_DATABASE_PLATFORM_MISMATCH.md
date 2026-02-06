# CRITICAL: Database Platform Mismatch Discovered

**Date**: 2026-01-22
**Status**: BLOCKING PRODUCTION DEPLOYMENT ⛔
**Severity**: CRITICAL

---

## Issue Summary

Production infrastructure was created with **Azure SQL Server** (Microsoft SQL Server), but the application and all 182 EF Core migrations are built for **PostgreSQL**.

This is a **complete incompatibility** that blocks production deployment.

---

## Evidence

### 1. Production Database (Current State)
```bash
# Resource created on Day 3
az sql server show --name lankaconnect-prod-sql --resource-group lankaconnect-prod
# Result: Microsoft.Sql/servers (SQL Server, NOT PostgreSQL)

az sql db show --name lankaconnect-db --server lankaconnect-prod-sql
# Result: {
#   "collation": "SQL_Latin1_General_CP1_CI_AS",  ← SQL Server collation
#   "status": "Paused",
#   "edition": "GeneralPurpose",
#   "computeModel": "Serverless"
# }
```

### 2. Staging Database (Working)
```bash
# PostgreSQL Flexible Server
Host: lankaconnect-staging-db.postgres.database.azure.com
Database: LankaConnectDB
Type: PostgreSQL 15
```

### 3. Application Requirements
```csharp
// All 182 migrations use PostgreSQL syntax
// Example: Migration 20260114000000_SeedEventReminderTemplate.cs
migrationBuilder.Sql(@"
    INSERT INTO communications.email_templates
    (""Id"", ""name"", ...)
    SELECT
        gen_random_uuid(),  ← PostgreSQL function
        'event-reminder',
        ...
    WHERE NOT EXISTS (...)
    ON CONFLICT DO NOTHING;  ← PostgreSQL syntax
");
```

### 4. Infrastructure Setup Script
From `scripts/setup-production-infrastructure-cost-optimized.sh`:

```bash
# Lines 200-206: Creates SQL Server (NOT PostgreSQL)
az sql server create \
    --name $SQL_SERVER_NAME \
    --resource-group $RESOURCE_GROUP \
    ...

# Lines 212-223: Creates SQL Database
az sql db create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --edition GeneralPurpose \
    --compute-model Serverless
```

**No PostgreSQL creation commands exist in the script.**

---

## Why SQL Server Was Chosen

From the setup script comments and configuration:

1. **Cost Optimization**: Serverless auto-pause saves money
   - Database pauses after 60 minutes of inactivity
   - Estimated cost: $50-60/month

2. **Azure SQL Serverless Features**:
   - Auto-scaling (0.5-1 vCore)
   - Pay only when active
   - Automatic backups

**However**: This cost optimization created an incompatibility with the application.

---

## PostgreSQL Features Used by Application

The application extensively uses PostgreSQL-specific features:

### 1. Data Types
- `JSONB` - Binary JSON storage
- `UUID` - Native UUID type
- Arrays - PostgreSQL array types

### 2. Functions
- `gen_random_uuid()` - UUID generation
- PostgreSQL string functions
- Date/time functions specific to PostgreSQL

### 3. Syntax
- `ON CONFLICT DO NOTHING` - Upsert syntax
- `RETURNING` clause
- PostgreSQL-specific operators

### 4. Extensions (if used)
- `uuid-ossp` extension
- Full-text search
- PostGIS (if used for location features)

---

## Impact Analysis

### What Breaks if We Try SQL Server:

1. **All 182 migrations will fail** with syntax errors
2. **Application code will fail** expecting PostgreSQL behavior
3. **Connection strings incompatible** (different format)
4. **Data types incompatible** (JSONB, UUID, arrays)
5. **Queries will fail** (PostgreSQL-specific syntax)

### What Works:
- Nothing - complete incompatibility

---

## Solution: Migrate to PostgreSQL Flexible Server

### Step 1: Delete SQL Server Resources
```bash
# Delete SQL Database
az sql db delete \
  --name lankaconnect-db \
  --server lankaconnect-prod-sql \
  --resource-group lankaconnect-prod \
  --yes

# Delete SQL Server
az sql server delete \
  --name lankaconnect-prod-sql \
  --resource-group lankaconnect-prod \
  --yes
```

### Step 2: Create PostgreSQL Flexible Server
```bash
# Create PostgreSQL Flexible Server
az postgres flexible-server create \
  --name lankaconnect-prod-db \
  --resource-group lankaconnect-prod \
  --location eastus2 \
  --admin-user pgadmin \
  --admin-password '[SECURE_PASSWORD]' \
  --version 15 \
  --tier Burstable \
  --sku-name Standard_B1ms \
  --storage-size 32 \
  --backup-retention 7 \
  --public-access 0.0.0.0 \
  --high-availability Disabled
```

**Cost Estimate**:
- Burstable B1ms: ~$13/month (1 vCPU, 2 GB RAM)
- Storage 32 GB: ~$5/month
- **Total**: ~$18-20/month (CHEAPER than SQL Server Serverless!)

### Step 3: Create Database
```bash
az postgres flexible-server db create \
  --resource-group lankaconnect-prod \
  --server-name lankaconnect-prod-db \
  --database-name LankaConnectDB
```

### Step 4: Configure Firewall
```bash
# Allow Azure services
az postgres flexible-server firewall-rule create \
  --resource-group lankaconnect-prod \
  --name lankaconnect-prod-db \
  --rule-name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

### Step 5: Update Key Vault
```bash
# Get new connection string
POSTGRES_CONNECTION_STRING="Host=lankaconnect-prod-db.postgres.database.azure.com;Database=LankaConnectDB;Username=pgadmin;Password=[PASSWORD];SslMode=Require"

# Update Key Vault
az keyvault secret set \
  --vault-name lankaconnect-prod-kv \
  --name DatabaseConnectionString \
  --value "$POSTGRES_CONNECTION_STRING"
```

---

## Cost Comparison

### Original Plan (SQL Server Serverless):
- Cost: $50-60/month
- Auto-pause after 60 minutes
- 5-10s cold start delay
- **Incompatible with application** ❌

### Corrected Plan (PostgreSQL Flexible Server):
- Cost: $18-20/month (Burstable B1ms)
- Always-on (no cold start)
- **Compatible with application** ✅
- **CHEAPER!** 💰

### Total Infrastructure Cost (Corrected):
- Container Apps: $30-40/month
- **PostgreSQL Flexible**: $18-20/month (was $50-60)
- Storage: $15-20/month
- Key Vault: $5/month
- Application Insights: $20-30/month
- Container Registry: $5/month
- Bandwidth: $20-30/month
- **NEW TOTAL**: **$113-165/month** (was $150-180)

**Result**: PostgreSQL is both compatible AND cheaper! 🎉

---

## Why This Happened

1. **Day 3 Infrastructure Setup**: Cost optimization focused on serverless features
2. **Azure SQL Serverless**: Appeared cost-effective with auto-pause
3. **PostgreSQL Not Considered**: Script assumed SQL Server compatibility
4. **No Testing**: Migrations never run against production database
5. **Late Discovery**: Only caught when preparing for database migration (Day 6)

---

## Lessons Learned

1. **Database platform must match staging** - consistency is critical
2. **Migration compatibility** - check migrations before infrastructure creation
3. **Test early** - run at least one migration against production to verify
4. **Cost analysis** - PostgreSQL Burstable is actually cheaper than SQL Server Serverless
5. **Documentation** - infrastructure scripts should document database choice reasoning

---

## Next Steps (IMMEDIATE)

1. ✅ User approval to proceed with PostgreSQL migration
2. Delete SQL Server resources
3. Create PostgreSQL Flexible Server (Burstable B1ms)
4. Update Key Vault with PostgreSQL connection string
5. Run EF Core migrations (now compatible!)
6. Verify reference data seeded
7. Continue with Day 6 tasks

---

## Status: AWAITING USER APPROVAL

**Question for User**:
> "Should we proceed with deleting the SQL Server and creating PostgreSQL Flexible Server instead? This will make the database compatible with our application AND save money ($18-20/month vs $50-60/month)."

**Recommendation**:
✅ **YES - Proceed immediately**. PostgreSQL is the only compatible option and is also cheaper.
