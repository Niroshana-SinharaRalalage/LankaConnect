# Badge 500 Error - Immediate Action Plan

**Critical**: DO NOT deploy more migrations until evidence is collected.

---

## Step 1: Get Actual Exception (15 minutes)

### Option A: Azure Portal (Easiest)
1. Open Azure Portal
2. Navigate to your Container App or App Service
3. Go to "Log stream" or "Logs" section
4. In another tab, trigger the error: `GET https://your-staging-url/api/badges`
5. Watch logs for exception
6. Copy FULL exception including stack trace

### Option B: Azure CLI
```bash
# For Container Apps
az containerapp logs show \
  --name <your-app-name> \
  --resource-group <your-resource-group> \
  --follow \
  --tail 100

# For App Service
az webapp log tail \
  --name <your-app-name> \
  --resource-group <your-resource-group>
```

### Option C: Application Insights
```kusto
exceptions
| where timestamp > ago(1h)
| where operation_Name contains "badges"
| project timestamp, type, outerMessage, details
| order by timestamp desc
```

**Save output to**: `logs/badge-500-exception.txt`

---

## Step 2: Check Database (10 minutes)

### Connect to Staging Database
Use psql, Azure Data Studio, or pgAdmin with staging connection string.

### Query 1: Check for NULL Values
```sql
-- Check if NULL values exist
SELECT
    id,
    name,
    position_x_listing,
    position_y_listing,
    position_x_featured,
    position_y_featured,
    position_x_detail,
    position_y_detail,
    CASE
        WHEN position_x_listing IS NULL OR position_y_listing IS NULL THEN 'LISTING_NULLS'
        WHEN position_x_featured IS NULL OR position_y_featured IS NULL THEN 'FEATURED_NULLS'
        WHEN position_x_detail IS NULL OR position_y_detail IS NULL THEN 'DETAIL_NULLS'
        ELSE 'OK'
    END as status
FROM badges.badges
WHERE is_active = true
ORDER BY status, name;

-- Summary count
SELECT
    COUNT(*) as total_active_badges,
    COUNT(CASE WHEN position_x_listing IS NULL OR position_y_listing IS NULL THEN 1 END) as listing_nulls,
    COUNT(CASE WHEN position_x_featured IS NULL OR position_y_featured IS NULL THEN 1 END) as featured_nulls,
    COUNT(CASE WHEN position_x_detail IS NULL OR position_y_detail IS NULL THEN 1 END) as detail_nulls
FROM badges.badges
WHERE is_active = true;
```

**Save output to**: `logs/badge-database-state.txt`

### Query 2: Check Schema
```sql
-- Check column nullability
SELECT
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'badges'
  AND table_name = 'badges'
  AND column_name IN (
    'position_x_listing', 'position_y_listing',
    'position_x_featured', 'position_y_featured',
    'position_x_detail', 'position_y_detail'
  )
ORDER BY column_name;
```

**Save output to**: `logs/badge-schema-state.txt`

### Query 3: Check Migration History
```sql
-- Verify which migrations ran
SELECT migration_id, product_version
FROM public.__efmigrationshistory
WHERE migration_id LIKE '%Badge%'
   OR migration_id LIKE '%20251217%'
ORDER BY migration_id DESC;
```

**Save output to**: `logs/badge-migration-history.txt`

---

## Step 3: Analyze Results (10 minutes)

### Decision Tree

#### If Exception Shows NullReferenceException at BadgeMappingExtensions.cs:32-34
→ **Issue**: Owned entity is null
→ **Check**: Did defensive null handling deploy to staging?
→ **Action**: See "Scenario Analysis" below

#### If Exception Shows Different Error
→ **Issue**: Not related to NULL values
→ **Action**: Analyze actual exception, create new hypothesis

#### If Database Query Shows NULL Values
→ **Issue**: Data needs fixing
→ **Action**: See "Data Fix" below

#### If Database Query Shows NO NULL Values
→ **Issue**: Not a data problem
→ **Action**: Review actual exception for real cause

---

## Scenario Analysis

### Scenario A: NULLs Exist + Defensive Code NOT Deployed
**Evidence**:
- Database query shows NULL values
- Exception is `NullReferenceException`
- Staging doesn't have Phase 6A.31b changes

**Fix**:
1. Deploy latest code (includes defensive null handling)
2. Then fix database (migration 20251217205258 if successful)

**Commands**:
```bash
# Check if code deployed
git log --oneline -10 | grep "6A.31b"

# Deploy if needed
./deploy-to-staging.sh  # or your deployment command
```

---

### Scenario B: NULLs Exist + Defensive Code IS Deployed
**Evidence**:
- Database query shows NULL values
- Exception is NOT `NullReferenceException` but EF Core error
- Staging has Phase 6A.31b defensive code

**Fix**: Database needs immediate update
```sql
-- Run this UPDATE statement directly on staging database
UPDATE badges.badges
SET
    position_x_listing = COALESCE(position_x_listing, 1.0),
    position_y_listing = COALESCE(position_y_listing, 0.0),
    size_width_listing = COALESCE(size_width_listing, 0.26),
    size_height_listing = COALESCE(size_height_listing, 0.26),
    rotation_listing = COALESCE(rotation_listing, 0.0),

    position_x_featured = COALESCE(position_x_featured, 1.0),
    position_y_featured = COALESCE(position_y_featured, 0.0),
    size_width_featured = COALESCE(size_width_featured, 0.26),
    size_height_featured = COALESCE(size_height_featured, 0.26),
    rotation_featured = COALESCE(rotation_featured, 0.0),

    position_x_detail = COALESCE(position_x_detail, 1.0),
    position_y_detail = COALESCE(position_y_detail, 0.0),
    size_width_detail = COALESCE(size_width_detail, 0.21),
    size_height_detail = COALESCE(size_height_detail, 0.21),
    rotation_detail = COALESCE(rotation_detail, 0.0);

-- Verify fix
SELECT COUNT(*) FROM badges.badges
WHERE position_x_listing IS NULL
   OR position_y_listing IS NULL;  -- Should return 0
```

**After SQL fix**:
```bash
# Test endpoint
curl https://your-staging-url/api/badges

# Should return 200 OK
```

---

### Scenario C: NO NULLs in Database
**Evidence**:
- Database query shows NO NULL values
- Exception still occurs

**Next Steps**:
1. Review actual exception details
2. Check if error is in different code path
3. Verify Badge entity loading (check `BadgeRepository.cs`)
4. Check if issue is with specific Badge record

**Diagnostic Query**:
```sql
-- Check specific badge data
SELECT * FROM badges.badges WHERE is_active = true LIMIT 5;

-- Check for data anomalies
SELECT
    id,
    name,
    position_x_listing,
    position_y_listing,
    CASE
        WHEN position_x_listing < 0 OR position_x_listing > 1 THEN 'OUT_OF_RANGE'
        WHEN position_y_listing < 0 OR position_y_listing > 1 THEN 'OUT_OF_RANGE'
        ELSE 'OK'
    END as validation_status
FROM badges.badges
WHERE is_active = true;
```

---

## Step 4: Report Findings (5 minutes)

### Create Summary Document

**Template**:
```
BADGE 500 ERROR - DIAGNOSTIC RESULTS
====================================
Date: 2025-12-17
Time: [current time]

1. EXCEPTION DETAILS
-------------------
[Paste full exception from Step 1]

2. DATABASE STATE
-----------------
Total Active Badges: [number]
Badges with NULL listing config: [number]
Badges with NULL featured config: [number]
Badges with NULL detail config: [number]

3. SCHEMA STATE
---------------
[Paste schema query results]

4. MIGRATION HISTORY
--------------------
[Paste migration history]

5. ROOT CAUSE HYPOTHESIS
------------------------
Based on evidence above:
[Your analysis - which scenario A, B, or C?]

6. RECOMMENDED FIX
------------------
[Specific fix based on scenario]

7. ESTIMATED RESOLUTION TIME
----------------------------
[Your estimate]
```

**Save to**: `logs/badge-500-diagnostic-summary.txt`

---

## Step 5: Execute Fix (Based on Findings)

### DO NOT SKIP STEPS 1-4

Only proceed with fix after:
- ✅ Exception captured
- ✅ Database queried
- ✅ Root cause confirmed
- ✅ Fix validated against evidence

### If Fix is Code Deployment
```bash
# Verify current deployment
git log --oneline -5

# Deploy latest
[your deployment command]

# Verify fix
curl https://your-staging-url/api/badges
```

### If Fix is Database Update
```sql
-- Run UPDATE statement (see Scenario B)
-- Then verify
SELECT COUNT(*) FROM badges.badges
WHERE position_x_listing IS NULL;  -- Should be 0
```

### If Fix is New Migration
**STOP**: Create new RCA document with findings before creating migration.

---

## Emergency Hotfix (If Immediate Resolution Required)

If production is blocked and you need immediate fix:

### Option 1: Quick Database Fix
```sql
-- Direct SQL update (bypasses migration system)
UPDATE badges.badges
SET
    position_x_listing = 1.0,
    position_y_listing = 0.0,
    size_width_listing = 0.26,
    size_height_listing = 0.26,
    rotation_listing = 0.0,
    position_x_featured = 1.0,
    position_y_featured = 0.0,
    size_width_featured = 0.26,
    size_height_featured = 0.26,
    rotation_featured = 0.0,
    position_x_detail = 1.0,
    position_y_detail = 0.0,
    size_width_detail = 0.21,
    size_height_detail = 0.21,
    rotation_detail = 0.0
WHERE position_x_listing IS NULL
   OR position_y_listing IS NULL
   OR position_x_featured IS NULL
   OR position_y_featured IS NULL
   OR position_x_detail IS NULL
   OR position_y_detail IS NULL;
```

**Warning**: This is a hotfix. Still need to:
1. Document what you did
2. Create proper migration for other environments
3. Update RCA with findings

---

## Checklist Before Any Fix

- [ ] Exception captured and analyzed
- [ ] Database state verified
- [ ] Schema state checked
- [ ] Migration history reviewed
- [ ] Root cause confirmed (not assumed)
- [ ] Fix matches confirmed root cause
- [ ] Fix tested locally (if possible)
- [ ] Rollback plan prepared
- [ ] Team/architect notified

---

## Support Contacts

- Architecture Questions: System Architect
- Database Access: DBA Team
- Azure Access: DevOps Team
- Emergency Escalation: [Your escalation path]

---

## Related Documents

- **Full RCA**: `docs/RCA_BADGE_500_ERROR.md`
- **Diagnostic Script**: `scripts/diagnose-badge-500-error.ps1`
- **Migration Files**: `src/LankaConnect.Infrastructure/Data/Migrations/`
- **Configuration**: `src/LankaConnect.Infrastructure/Data/Configurations/BadgeConfiguration.cs`

---

**Remember**: Evidence first, hypothesis second, fix third.
