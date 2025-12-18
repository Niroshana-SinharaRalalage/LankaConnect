# Phase 6A.32: Badge Zero Values - Step-by-Step Fix Guide

## Quick Summary

**Problem**: All badges showing 0x0 size (invisible) instead of proper defaults

**Root Cause**: PostgreSQL converted NULL values to 0 during NULL→NOT NULL migration, COALESCE didn't fix zeros

**Fix**: New migration that directly sets correct values without COALESCE

**Time to Fix**: ~5 minutes (apply migration + verify)

---

## Step 1: Verify the Problem

Run diagnostic query to confirm zero values:

```bash
# Connect to staging database
psql -U [user] -d lankaconnect_staging

# Run diagnostic
\i verify-badge-values.sql
```

**Expected Output** (BEFORE FIX):
```
 total_badges | zero_x_listing | zero_width_listing | correct_listing
--------------+----------------+--------------------+-----------------
           13 |             13 |                 13 |               0
```

**Translation**: All 13 badges have zero values (broken)

---

## Step 2: Apply the Fix Migration

### Option A: Using EF Core Migrations (Recommended)

```bash
cd /c/Work/LankaConnect

# Build the project
dotnet build src/LankaConnect.Infrastructure

# Preview the migration (dry-run)
dotnet ef migrations script \
  --project src/LankaConnect.Infrastructure \
  --startup-project src/LankaConnect.Api \
  --idempotent > migration-preview.sql

# Review the SQL
cat migration-preview.sql

# Apply to staging database
dotnet ef database update \
  --project src/LankaConnect.Infrastructure \
  --startup-project src/LankaConnect.Api \
  --connection "Host=staging-db;Database=lankaconnect_staging;..."
```

### Option B: Direct SQL Execution (Faster)

```bash
# Run the fix script directly
psql -U [user] -d lankaconnect_staging -f fix-badge-zeros-final.sql
```

**Expected Output**:
```
UPDATE 13

 total_updated | correct_x | correct_width_listing | correct_width_detail
---------------+-----------+-----------------------+----------------------
            13 |        13 |                    13 |                   13

COMMIT
```

---

## Step 3: Verify the Fix

### 3.1 Database Verification

```bash
# Re-run diagnostic query
psql -U [user] -d lankaconnect_staging -f verify-badge-values.sql
```

**Expected Output** (AFTER FIX):
```
 total_badges | zero_x_listing | zero_width_listing | correct_listing
--------------+----------------+--------------------+-----------------
           13 |              0 |                  0 |              13
```

**Translation**: All 13 badges now have correct default values

### 3.2 API Verification

```bash
# Test the API endpoint
curl https://staging.lankaconnect.com/api/badges | jq '.[0].listingConfig'
```

**Expected Output**:
```json
{
  "positionX": 1.0,
  "positionY": 0.0,
  "sizeWidth": 0.26,
  "sizeHeight": 0.26,
  "rotation": 0.0
}
```

**NOT** (what we had before):
```json
{
  "positionX": 0.0,  // ❌ WRONG
  "positionY": 0.0,
  "sizeWidth": 0.0,   // ❌ WRONG (invisible!)
  "sizeHeight": 0.0,  // ❌ WRONG
  "rotation": 0.0
}
```

### 3.3 UI Verification

1. Navigate to Badge Management UI: `https://staging.lankaconnect.com/admin/badges`
2. Check that badges are visible in the preview
3. Verify badge size is ~26% of container (not 0%)
4. Check positioning is TopRight corner

**Before Fix**: Nothing visible (0x0 badges)
**After Fix**: Badges visible in top-right corner at 26% size

---

## Step 4: Smoke Test

Create a test event with a badge:

```bash
# Via API or UI:
# 1. Create test event
# 2. Assign a badge (e.g., "New Event")
# 3. View event in listing page
# 4. Verify badge visible in top-right corner
# 5. Check Featured Banner (if applicable)
# 6. Check Event Detail Hero page
```

**All three locations should show badge correctly positioned and sized.**

---

## Step 5: Production Deployment

### Pre-Deployment Checklist
- [ ] Staging verification complete
- [ ] All 13 badges show correct values
- [ ] API returns proper defaults
- [ ] UI renders badges correctly
- [ ] Smoke test passed

### Deployment Steps

1. **Backup Production Database**:
   ```bash
   pg_dump -U [user] -d lankaconnect_production > backup_before_6a32_fix.sql
   ```

2. **Deploy Code** (if not already deployed):
   ```bash
   git pull origin develop
   # ... your deployment process ...
   ```

3. **Apply Migration**:
   ```bash
   dotnet ef database update \
     --project src/LankaConnect.Infrastructure \
     --startup-project src/LankaConnect.Api \
     --connection "[production-connection-string]"
   ```

4. **Verify Production**:
   ```bash
   psql -U [user] -d lankaconnect_production -f verify-badge-values.sql
   curl https://api.lankaconnect.com/api/badges | jq '.[0].listingConfig'
   ```

5. **Monitor**:
   - Check application logs for errors
   - Verify user-facing pages load correctly
   - Test badge assignment workflow

---

## Rollback Plan

If something goes wrong:

### Option 1: Restore from Backup
```bash
psql -U [user] -d lankaconnect_production < backup_before_6a32_fix.sql
```

### Option 2: Revert Migration
```bash
# Get the migration before the fix
dotnet ef migrations list --project src/LankaConnect.Infrastructure

# Revert to previous migration
dotnet ef database update [previous-migration-name] \
  --project src/LankaConnect.Infrastructure \
  --startup-project src/LankaConnect.Api
```

**NOTE**: Reverting the migration will restore the BROKEN state (zero values). Only do this if the migration itself causes errors.

---

## Troubleshooting

### Issue: Migration shows "already applied"

**Cause**: Migration already ran on this database

**Solution**:
```bash
# Check migration history
dotnet ef migrations list --project src/LankaConnect.Infrastructure

# If migration is listed but data is still wrong, run SQL directly:
psql -U [user] -d [database] -f fix-badge-zeros-final.sql
```

### Issue: Still seeing zeros after migration

**Cause**: Migration WHERE clause didn't match your data

**Solution**:
```sql
-- Force update ALL badges regardless of current values
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
    rotation_detail = 0.0,
    updated_at = NOW();
-- No WHERE clause = update ALL
```

### Issue: API still returns zeros

**Cause**: Application cache or connection pooling

**Solution**:
```bash
# Restart the API application
systemctl restart lankaconnect-api  # or your restart method

# Clear Redis cache if using
redis-cli FLUSHALL
```

### Issue: Build errors when creating migration

**Cause**: Compilation errors in codebase

**Solution**:
```bash
# Check for build errors
dotnet build src/LankaConnect.Infrastructure

# Fix any compilation errors first
# Then retry migration creation
```

---

## Success Criteria

Fix is complete when ALL of these are true:

- [ ] Database query shows 0 badges with zero values
- [ ] Database query shows 13 badges with correct defaults
- [ ] API endpoint returns positionX=1.0, sizeWidth=0.26
- [ ] UI shows badges in top-right corner
- [ ] Badges are visible (not 0x0 size)
- [ ] All three locations (Listing/Featured/Detail) work correctly
- [ ] No errors in application logs

---

## Files Reference

- `verify-badge-values.sql` - Diagnostic queries
- `fix-badge-zeros-final.sql` - Manual fix script
- `20251218044022_FixBadgeLocationConfigZeroValues.cs` - EF Core migration
- `PHASE_6A32_BADGE_ZERO_VALUES_ROOT_CAUSE_ANALYSIS.md` - Detailed analysis

---

## Timeline

- **Discovery**: 2025-12-17 (User reported "badges not working")
- **Root Cause Analysis**: 2025-12-17 (PostgreSQL NULL→0 conversion)
- **Fix Created**: 2025-12-17 (Migration 20251218044022)
- **Staging Deployment**: [TBD]
- **Production Deployment**: [TBD]

---

## Support

If issues persist after following this guide:

1. Check `PHASE_6A32_BADGE_ZERO_VALUES_ROOT_CAUSE_ANALYSIS.md` for technical details
2. Review database logs for migration errors
3. Verify connection string points to correct database
4. Check PostgreSQL version compatibility (12+)
5. Review EF Core migration history for conflicts
