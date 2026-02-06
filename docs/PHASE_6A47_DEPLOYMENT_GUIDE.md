# Phase 6A.47 - Deployment Guide

**Date**: 2025-12-29
**Phase**: 6A.47 - Enum to Reference Data Migration (Deterministic GUID Fix)
**Status**: Ready for Azure Staging Deployment

---

## Overview

This deployment updates reference data seed GUIDs from random to deterministic values. This is a **critical infrastructure fix** that prevents future migration issues.

**What Changed**:
- Fixed seed data configuration to use MD5-based deterministic GUIDs
- Created EF Core migration to update existing database records
- 22 reference data records will have new GUIDs (EventCategory, EventStatus, UserRole)
- **IMPORTANT**: `(enum_type, int_value)` pairs remain unchanged - no data loss

---

## Pre-Deployment Checklist

### 1. Code Review ✅
- [x] ReferenceValueConfiguration.cs uses deterministic GUIDs
- [x] Build succeeds (0 errors, 0 warnings)
- [x] Migration generated successfully
- [x] Commits pushed to develop branch

### 2. Environment Preparation
**Before deploying to Azure staging**:

```bash
# 1. Ensure you're on develop branch
git checkout develop
git pull origin develop

# 2. Verify latest commits present
git log --oneline -3
# Should see:
# 537e5bb9 feat(phase-6a47): Add migration to update seed GUIDs
# 22c6199d fix(phase-6a47): Use deterministic GUIDs for reference data seed
```

### 3. Database Backup
**CRITICAL**: Backup reference_values table before migration

```sql
-- Connect to Azure staging database
-- Create backup table
CREATE TABLE reference_data.reference_values_backup_phase6a47 AS
SELECT * FROM reference_data.reference_values;

-- Verify backup
SELECT COUNT(*) FROM reference_data.reference_values_backup_phase6a47;
-- Expected: Should match current reference_values count
```

---

## Deployment Steps

### Step 1: Deploy Code to Azure Staging

**Option A: Azure App Service Deployment**
```bash
# From project root
cd c:/Work/LankaConnect

# Build and publish
dotnet publish src/LankaConnect.API/LankaConnect.API.csproj -c Release -o ./publish

# Deploy to Azure App Service (use your deployment method)
# - Azure DevOps pipeline
# - GitHub Actions
# - Manual ZIP deploy
# - FTP upload
```

**Option B: Manual Verification (Local)**
```bash
# Test migration locally first
cd src/LankaConnect.Infrastructure

# Ensure connection string points to LOCAL test database
# Run migration
dotnet ef database update --context AppDbContext

# Verify reference_values updated
psql $LOCAL_DATABASE_URL -c "SELECT enum_type, COUNT(*) FROM reference_data.reference_values GROUP BY enum_type"
```

### Step 2: Run Migration on Azure Staging Database

**Connect to Azure Staging**:
```bash
# Get connection string from Azure Portal:
# App Service → Configuration → Connection Strings → DefaultConnection

# Set environment variable
export AZURE_STAGING_DB="Host=your-server.postgres.database.azure.com;Database=lankaconnect;Username=admin;Password=xxx"
```

**Run Migration**:
```bash
cd src/LankaConnect.Infrastructure

# Apply migration to Azure staging
dotnet ef database update --context AppDbContext --connection "$AZURE_STAGING_DB"
```

**Expected Output**:
```
Build started...
Build succeeded.
Applying migration '20251229183353_Phase6A47_Fix_DeterministicSeedGUIDs'.
Done.
```

### Step 3: Verification

**Verify Migration Applied**:
```sql
-- Connect to Azure staging database
SELECT migration_id, product_version
FROM "__EFMigrationsHistory"
WHERE migration_id LIKE '%Phase6A47_Fix_DeterministicSeedGUIDs%';
-- Expected: 1 row with migration_id = '20251229183353_Phase6A47_Fix_DeterministicSeedGUIDs'
```

**Verify Reference Data Unchanged**:
```sql
-- Verify counts (should be same as before)
SELECT enum_type, COUNT(*) as value_count
FROM reference_data.reference_values
GROUP BY enum_type
ORDER BY enum_type;

-- Expected result:
-- EventCategory  | 8
-- EventStatus    | 8
-- UserRole       | 6
-- (plus any other enum types you have)

-- Verify specific values unchanged
SELECT code, int_value, name
FROM reference_data.reference_values
WHERE enum_type = 'EventCategory'
ORDER BY int_value;

-- Expected: Religious(0), Cultural(1), Community(2), Educational(3),
--           Social(4), Business(5), Charity(6), Entertainment(7)
```

**Verify Deterministic GUIDs**:
```sql
-- Check that GUIDs are now deterministic
-- Religious should always have same GUID: MD5("LankaConnect.ReferenceData.EventCategory.Religious")
SELECT id, code
FROM reference_data.reference_values
WHERE enum_type = 'EventCategory' AND code = 'Religious';

-- Expected: id = '31f73d61-6c12-1252-f5ab-10d9d47eba46' (deterministic)
```

---

## Post-Deployment Testing

### 1. API Testing
**Test reference data endpoint**:
```bash
curl -X GET "https://[staging-url]/api/reference-data?types=EventCategory,EventStatus,UserRole" \
  -H "Accept: application/json"
```

**Expected Response**:
```json
[
  {
    "enumType": "EventCategory",
    "code": "Religious",
    "name": "Religious",
    "intValue": 0,
    "isActive": true
  },
  {
    "enumType": "EventCategory",
    "code": "Cultural",
    "name": "Cultural",
    "intValue": 1,
    "isActive": true
  },
  ...
]
```

### 2. Frontend Testing
**Test dropdowns load correctly**:
1. Navigate to staging frontend: `https://[staging-frontend-url]`
2. Go to Event Creation page
3. Verify:
   - ✅ Category dropdown shows 8 categories
   - ✅ Status labels display correctly
   - ✅ No console errors

### 3. Smoke Tests
**Critical user flows**:
- [ ] Create new event (category dropdown works)
- [ ] Edit existing event (category/status display correctly)
- [ ] Filter events by category (filters work)
- [ ] User registration (role assignment works)

---

## Rollback Procedure

**If migration fails or causes issues**:

### Option 1: EF Core Rollback
```bash
# Rollback to previous migration
cd src/LankaConnect.Infrastructure
dotnet ef database update 20251227034100_Phase6A47_Refactor_To_Unified_ReferenceValues --context AppDbContext --connection "$AZURE_STAGING_DB"
```

### Option 2: Manual Rollback
```sql
BEGIN TRANSACTION;

-- Restore from backup
DELETE FROM reference_data.reference_values
WHERE enum_type IN ('EventCategory', 'EventStatus', 'UserRole');

INSERT INTO reference_data.reference_values
SELECT * FROM reference_data.reference_values_backup_phase6a47
WHERE enum_type IN ('EventCategory', 'EventStatus', 'UserRole');

-- Verify restoration
SELECT enum_type, COUNT(*) FROM reference_data.reference_values
WHERE enum_type IN ('EventCategory', 'EventStatus', 'UserRole')
GROUP BY enum_type;
-- Expected: EventCategory=8, EventStatus=8, UserRole=6

COMMIT;
```

### Option 3: Code Rollback
```bash
# Revert commits
git revert 537e5bb9  # Revert migration commit
git revert 22c6199d  # Revert seed fix commit
git push origin develop

# Redeploy to Azure
```

---

## Production Deployment (After Staging Validation)

**Only proceed after**:
- ✅ Staging migration successful
- ✅ All smoke tests pass
- ✅ Frontend dropdowns work correctly
- ✅ No errors in application logs
- ✅ 24-48 hours stable on staging

**Production Steps**:
1. Backup production reference_values table
2. Merge develop → master
3. Deploy code to production
4. Run migration on production database
5. Verify production (same checks as staging)
6. Monitor for 1 hour post-deployment

---

## Success Criteria

- [ ] Migration applied successfully (no errors)
- [ ] Reference data counts unchanged (EventCategory=8, EventStatus=8, UserRole=6)
- [ ] Reference data API returns correct values
- [ ] Frontend dropdowns populate correctly
- [ ] No new errors in application logs
- [ ] All smoke tests pass
- [ ] GUIDs are deterministic (re-running migration generates same SQL)

---

## Monitoring

**Post-deployment monitoring** (first 24 hours):

1. **Application Logs**:
   ```bash
   # Azure App Service logs
   az webapp log tail --name your-app --resource-group your-rg
   ```

2. **Database Queries**:
   ```sql
   -- Check for errors
   SELECT * FROM reference_data.reference_values
   WHERE enum_type IN ('EventCategory', 'EventStatus', 'UserRole')
   ORDER BY enum_type, int_value;
   ```

3. **API Monitoring**:
   - Watch /api/reference-data endpoint response times
   - Monitor cache hit rates (should be ~100%)
   - Check error rates (should be 0%)

---

## Contact

**Phase Owner**: Phase 6A.47 Agent
**Deployment Date**: 2025-12-29
**Jira Ticket**: (if applicable)
**Slack Channel**: #lankaconnect-deployments

---

## Related Documentation

- [RCA_PHASE_6A47_MIGRATION_GUID_REGENERATION.md](./RCA_PHASE_6A47_MIGRATION_GUID_REGENERATION.md) - Root cause analysis
- [PHASE_6A47_INTERIM_STATUS.md](./PHASE_6A47_INTERIM_STATUS.md) - Phase status
- [Migration File](../src/LankaConnect.Infrastructure/Data/Migrations/20251229183353_Phase6A47_Fix_DeterministicSeedGUIDs.cs)

---

**Document Version**: 1.0
**Last Updated**: 2025-12-29
