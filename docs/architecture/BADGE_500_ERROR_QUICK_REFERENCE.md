# Badge 500 Error - Quick Reference Guide

## TL;DR

**Problem**: Badge Management page returns 500 error
**Root Cause**: Migration with default values committed but NOT deployed to staging
**Fix**: Wait for GitHub Actions to deploy migration `20251216150703`
**Status**: Code is ready ✅ | Deployment pending ⏳

---

## Your Questions Answered

### 1. Is this definitely a database migration issue?

**YES - 100% confirmed.**

- Phase 6A.31a added 15 new columns for BadgeLocationConfig (owned entity)
- Migration 1: Added columns WITHOUT defaults → Existing badges have NULL
- Migration 2: Adds UPDATE + defaults → **Committed but NOT deployed**
- EF Core owned entities require ALL properties to be non-NULL
- When API tries to load badges, it finds NULL values → throws exception → 500 error

**File**: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Migrations\20251216150703_UpdateBadgeLocationConfigsWithDefaults.cs`

### 2. How to verify migration deployment status?

**Method 1: Test the API (Fastest)**
```powershell
# Windows
.\scripts\verify-badge-migration.ps1

# Linux/Mac
./scripts/verify-badge-migration.sh
```

**Method 2: Check GitHub Actions**
1. Go to: https://github.com/[org]/LankaConnect/actions
2. Find workflow for commit `a359fea` or later
3. Check step "Run EF Migrations"
4. Look for: "✅ Migrations completed successfully"

**Method 3: Query Database (if you have access)**
```sql
SELECT COUNT(*)
FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20251216150703_UpdateBadgeLocationConfigsWithDefaults';
```
- Result = 0: NOT deployed
- Result = 1: Deployed

### 3. Testing strategy with no local database?

**Option A: Staging API Testing (Recommended)**
```powershell
# Create token.txt with your auth token
# Run verification script
.\scripts\verify-badge-migration.ps1

# Or test manually
$token = Get-Content token.txt
$headers = @{ "Authorization" = "Bearer $token" }
Invoke-WebRequest -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/badges" -Headers $headers
```

**Option B: Monitor Container Logs**
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 100
```

**Option C: UI Testing**
1. Start local UI: `cd web && npm run dev`
2. UI proxies to staging API automatically
3. Visit: http://localhost:3000/dashboard
4. Click Badge Management
5. If loads → Migration deployed ✅
6. If 500 error → Migration NOT deployed ❌

### 4. Immediate fix - what should I do?

**RECOMMENDED: Option A - Wait for GitHub Actions**

1. Check if workflow is running:
   ```bash
   # Check GitHub Actions page
   # https://github.com/[org]/LankaConnect/actions?query=branch:develop
   ```

2. If workflow hasn't triggered:
   ```bash
   git commit --allow-empty -m "chore: trigger staging deployment for badge migration"
   git push origin develop
   ```

3. Wait 2-3 minutes for deployment

4. Verify:
   ```powershell
   .\scripts\verify-badge-migration.ps1
   ```

**IF URGENT: Option B - Manual Migration**
```bash
# Install EF Core tools
dotnet tool install -g dotnet-ef --version 8.0.0

# Get connection string
az login
DB_CONNECTION=$(az keyvault secret show \
  --vault-name lankaconnect-staging-kv \
  --name DATABASE-CONNECTION-STRING \
  --query value -o tsv)

# Apply migration
cd src/LankaConnect.API
dotnet ef database update \
  --connection "$DB_CONNECTION" \
  --project ../LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj \
  --verbose
```

**NOT RECOMMENDED: Option C - Code Fallback**

Don't add nullable properties to BadgeLocationConfig - this violates Value Object pattern and creates technical debt for a temporary deployment issue.

### 5. Better pattern for EF Core owned entities?

**YES - Use single atomic migration with defaults:**

**❌ Current Approach (Two Migrations)**
```csharp
// Migration 1: Add columns (creates broken state)
migrationBuilder.AddColumn<decimal>("position_x", nullable: false);
// → Existing rows have NULL, violates EF Core contract

// Migration 2: Fix broken state
migrationBuilder.Sql("UPDATE ... SET position_x = 1.0 WHERE position_x IS NULL");
migrationBuilder.AlterColumn("position_x", defaultValue: 1.0m);
```

**✅ Better Approach (Single Atomic Migration)**
```csharp
// Single migration: Add columns with defaults
migrationBuilder.AddColumn<decimal>(
    name: "position_x_listing",
    nullable: false,
    defaultValue: 1.0m);  // ✅ Existing rows get default immediately

migrationBuilder.AddColumn<decimal>(
    name: "position_y_listing",
    nullable: false,
    defaultValue: 0.0m);

// No UPDATE needed - database handles it
// No broken state between deployments
```

**Why Single Migration is Better:**
1. No temporary broken state
2. Atomic operation - all or nothing
3. Works if code deploys before migration
4. Works if migration deploys before code
5. Simpler rollback

**When to Use Two Migrations:**
- Complex data transformations
- Need to validate data before adding constraints
- Backfilling from other tables

**Alternative Patterns (NOT for this case):**
- Nullable backing fields → Violates Value Object immutability
- Sentinel values → Unnecessary complexity
- HasConversion with null handling → Only if schema can't change

---

## Verification Checklist

Run through this after deployment:

```powershell
# 1. Test API
.\scripts\verify-badge-migration.ps1

# 2. Test UI
cd web
npm run dev
# Visit: http://localhost:3000/dashboard
# Click: Badge Management
# Expected: Badge list loads (not 500 error)

# 3. Test Badge Creation
# Click "Create Badge"
# Upload image
# Save
# Expected: Badge created with default location configs

# 4. Check Database (if access)
# Verify no NULL values in badge location columns
```

---

## Timeline

| Time | Event | Status |
|------|-------|--------|
| 2025-12-15 23:59 | Phase 6A.31a committed (first migration) | ✅ Done |
| 2025-12-16 05:13 | Second migration committed (fix) | ✅ Done |
| 2025-12-16 [NOW] | Code ready, awaiting deployment | ⏳ Pending |
| 2025-12-16 +2min | GitHub Actions completes | ⏳ Pending |
| 2025-12-16 +3min | Badge API working | ⏳ Pending |

---

## Related Files

- **Root Cause Analysis**: `docs/architecture/BADGE_500_ERROR_ROOT_CAUSE_ANALYSIS.md`
- **Migration File**: `src/LankaConnect.Infrastructure/Data/Migrations/20251216150703_UpdateBadgeLocationConfigsWithDefaults.cs`
- **Domain Model**: `src/LankaConnect.Domain/Badges/Badge.cs`
- **Value Object**: `src/LankaConnect.Domain/Badges/BadgeLocationConfig.cs`
- **EF Configuration**: `src/LankaConnect.Infrastructure/Data/Configurations/BadgeConfiguration.cs`

---

## Next Steps

1. Run verification script: `.\scripts\verify-badge-migration.ps1`
2. If NOT deployed: Trigger GitHub Actions
3. Wait 2-3 minutes
4. Test Badge Management page in UI
5. Mark issue as resolved
