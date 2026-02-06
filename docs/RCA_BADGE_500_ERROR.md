# Root Cause Analysis: Badge API 500 Error
**Date**: 2025-12-17
**Status**: DIAGNOSTIC PHASE
**Severity**: CRITICAL (Production blocker)

---

## Executive Summary

Badge API endpoint returns HTTP 500 on staging. **Root cause is NOT yet confirmed**. Five migrations deployed based on NULL hypothesis without diagnostic evidence collection. This RCA provides systematic diagnostic plan before further remediation attempts.

---

## Problem Statement

### Symptoms
- **Endpoint**: `GET /api/badges`
- **Response**: HTTP 500 Internal Server Error
- **Impact**: Badge Management page cannot load
- **Environment**: Staging (production-like)
- **Frequency**: 100% reproducible

### Known Error Location
```csharp
// GetBadgesQueryHandler.cs:105
return b.ToBadgeDto(creatorName);  // FAILS HERE

// Called from BadgeMappingExtensions.cs:32-34
ListingConfig = badge.ListingConfig.ToDto(),    // Line 32
FeaturedConfig = badge.FeaturedConfig.ToDto(),  // Line 33
DetailConfig = badge.DetailConfig.ToDto(),      // Line 34
```

---

## Current Hypothesis (UNCONFIRMED)

### Working Theory
Owned entity properties (`ListingConfig`, `FeaturedConfig`, `DetailConfig`) contain NULL values, causing `NullReferenceException` when EF Core materializes Badge entities.

### Why This Theory Emerged
1. EF Core `OwnsOne()` behavior: If ALL owned entity columns are NULL, EF Core materializes the navigation property as `null` instead of an instance
2. Database may have been created before migration that added owned entity columns
3. Existing Badge rows may have NULL values for position columns

### Critical Gap: THEORY NOT VALIDATED
- No actual exception message captured from Azure logs
- No database query executed to check for NULL values
- No schema verification performed
- No local reproduction attempted

**This is assumption-driven debugging without evidence.**

---

## Architecture Analysis

### EF Core Owned Entity Behavior (The Trap)

```csharp
// BadgeConfiguration.cs:50-81
builder.OwnsOne(b => b.ListingConfig, cfg =>
{
    cfg.Property(c => c.PositionX)
        .HasColumnName("position_x_listing")
        .IsRequired()
        .HasDefaultValue(1.0m);  // ⚠️ HasDefaultValue() only applies to NEW rows
    // ... other properties
});
```

**Key Insight**: `HasDefaultValue()` in EF Core does TWO things:
1. Tells EF Core what default to use when creating NEW entities in memory
2. Generates SQL `DEFAULT` constraint for INSERT operations

**It does NOT**:
- Update existing NULL values in database
- Apply defaults during SELECT queries
- Retroactively fix historical data

### The Null Coalescing Problem

EF Core owned entity rule:
> If **ALL** columns of an owned entity are NULL, EF Core materializes the navigation property as `null` instead of creating an instance.

Example:
```sql
-- If this row exists:
SELECT position_x_listing, position_y_listing, size_width_listing, size_height_listing, rotation_listing
FROM badges.badges
WHERE id = 'some-id';

-- Result:
-- NULL, NULL, NULL, NULL, NULL

-- Then EF Core will set:
badge.ListingConfig = null;  // Instead of new BadgeLocationConfig(...)
```

Then when mapping:
```csharp
// BadgeMappingExtensions.cs:32
ListingConfig = badge.ListingConfig.ToDto(),  // NullReferenceException if badge.ListingConfig is null
```

### Defensive Null Handling Already Added (Phase 6A.31b)

```csharp
// BadgeMappingExtensions.cs:51-65
public static BadgeLocationConfigDto ToDto(this BadgeLocationConfig? config)
{
    if (config == null)  // ✅ Defensive null check
    {
        return new BadgeLocationConfigDto
        {
            PositionX = 1.0m,
            PositionY = 0.0m,
            SizeWidth = 0.26m,
            SizeHeight = 0.26m,
            Rotation = 0.0m
        };
    }
    // ...
}
```

**Critical Question**: If defensive null handling exists (Phase 6A.31b), why is 500 error still occurring?

**Possible Explanations**:
1. Code not deployed to staging (deployment issue)
2. Different error entirely (not related to owned entities)
3. NullReferenceException occurring at different line (not in ToDto())
4. Exception during EF Core materialization itself (before mapping)

---

## Migration History Analysis

### Migrations Deployed (All Based on Unconfirmed Hypothesis)

#### 1. 20251217053649_ApplyBadgeLocationConfigDefaults
**Approach**: UPDATE with WHERE clause
```sql
UPDATE badges.badges
SET position_x_listing = COALESCE(position_x_listing, 1.0)
WHERE position_x_listing IS NULL;
```

**Problem**: WHERE clause may have matched 0 rows if:
- Columns didn't exist yet
- Rows already had values
- Table was empty

**Result**: Deployed, error persists

---

#### 2. 20251217175941_EnforceBadgeLocationConfigNotNull
**Approach**: Unconditional UPDATE + AlterColumn
```csharp
// Intended SQL
migrationBuilder.Sql("UPDATE badges.badges SET position_x_listing = COALESCE(position_x_listing, 1.0);");

// But AlterColumn() triggered PostgreSQL to inject WHERE clauses
migrationBuilder.AlterColumn<decimal>(
    name: "position_x_listing",
    nullable: false  // ⚠️ This caused PostgreSQL to add WHERE clause
);
```

**Critical Finding**: PostgreSQL deployment logs show:
```
Expected: UPDATE badges.badges SET position_x_listing = 1.0;
Actual:   UPDATE badges.badges SET position_x_listing = 1.0 WHERE position_x_listing IS NULL;
```

**Root Cause of Failure**: When `AlterColumn` changes `nullable: true` → `nullable: false`, PostgreSQL EF Core provider automatically generates conditional UPDATE statements, overriding our unconditional SQL.

**Result**: Deployed, error persists

---

#### 3. 20251217205258_FixBadgeNullsDataOnly
**Approach**: Pure data migration (no AlterColumn)
```csharp
// Only data updates, no schema changes
migrationBuilder.Sql("UPDATE badges.badges SET position_x_listing = COALESCE(position_x_listing, 1.0);");
```

**Status**: Deployment failed (NuGet 503 errors), retry in progress
**Expected Outcome**: Unknown

---

## Critical Unknown Factors

### 1. Actual Exception Details (HIGHEST PRIORITY)
**What We Need**:
- Full exception type (NullReferenceException? InvalidOperationException? EF Core exception?)
- Stack trace (which exact line is failing?)
- Inner exception details
- Request/response context

**How to Get It**:
- Azure Container App logs
- Azure App Service log stream
- Application Insights traces

### 2. Database State
**What We Need**:
```sql
-- Check for NULL values
SELECT id, name,
       position_x_listing, position_y_listing,
       position_x_featured, position_y_featured,
       position_x_detail, position_y_detail
FROM badges.badges
WHERE position_x_listing IS NULL
   OR position_y_listing IS NULL
   OR position_x_featured IS NULL
   OR position_y_featured IS NULL
   OR position_x_detail IS NULL
   OR position_y_detail IS NULL;

-- Count summary
SELECT
    COUNT(*) as total_badges,
    COUNT(CASE WHEN position_x_listing IS NULL THEN 1 END) as listing_nulls,
    COUNT(CASE WHEN position_x_featured IS NULL THEN 1 END) as featured_nulls,
    COUNT(CASE WHEN position_x_detail IS NULL THEN 1 END) as detail_nulls
FROM badges.badges;
```

### 3. Schema State
**What We Need**:
```sql
-- Check actual column nullability
SELECT column_name, is_nullable, column_default
FROM information_schema.columns
WHERE table_schema = 'badges'
  AND table_name = 'badges'
  AND column_name LIKE 'position_%'
ORDER BY column_name;
```

### 4. Migration History
**What We Need**:
```sql
-- Verify which migrations actually ran
SELECT migration_id, product_version
FROM public.__efmigrationshistory
WHERE migration_id LIKE '%Badge%'
ORDER BY migration_id DESC;
```

### 5. Deployment State
**What We Need**:
- Verify which code version is running in staging
- Check if Phase 6A.31b changes (defensive null handling) are deployed
- Confirm migration execution order

---

## Systematic Diagnostic Plan

### Phase 1: Evidence Collection (BEFORE ANY MORE CHANGES)

#### Step 1.1: Capture Actual Exception
**Priority**: CRITICAL
**Tools**:
- Azure Portal → Container App/App Service → Log Stream
- Azure CLI: `az containerapp logs show --name <app> --resource-group <rg> --tail 100`
- Application Insights query

**Action**: Trigger Badge API endpoint, capture FULL exception with stack trace

**Deliverable**: Text file with complete exception details

---

#### Step 1.2: Query Database State
**Priority**: CRITICAL
**Tool**: psql, Azure Data Studio, or pgAdmin

**Queries**:
1. NULL value check (see "Database State" section above)
2. Schema check (see "Schema State" section above)
3. Migration history (see "Migration History" section above)
4. Sample data inspection:
```sql
SELECT * FROM badges.badges LIMIT 5;
```

**Deliverable**: Query results showing actual data state

---

#### Step 1.3: Verify Code Deployment
**Priority**: HIGH
**Actions**:
1. Check deployment logs for Phase 6A.31b deployment
2. Verify git commit hash running in staging
3. Confirm BadgeMappingExtensions.cs contains defensive null checks (lines 51-65)

**Deliverable**: Confirmation of code version in staging

---

#### Step 1.4: Local Reproduction
**Priority**: HIGH
**Actions**:
1. Update local appsettings to use staging database connection string
2. Run API locally: `dotnet run --project src/LankaConnect.API`
3. Test endpoint: `curl http://localhost:5000/api/badges`
4. Capture exception with full stack trace (better details than Azure logs)

**Deliverable**: Local reproduction with detailed exception

---

### Phase 2: Root Cause Confirmation (AFTER EVIDENCE COLLECTED)

Based on evidence from Phase 1, determine actual root cause:

#### Scenario A: NULL Values Exist + Defensive Code NOT Deployed
**Evidence**:
- Database query shows NULL values
- Staging code doesn't have defensive null checks

**Root Cause**: Phase 6A.31b not deployed to staging
**Fix**: Deploy Phase 6A.31b code changes

---

#### Scenario B: NULL Values Exist + Defensive Code IS Deployed
**Evidence**:
- Database query shows NULL values
- Staging code HAS defensive null checks
- Exception still occurs

**Root Cause**: Exception happening during EF Core materialization, not during mapping
**Fix**: Need to update owned entity configuration OR fix database values

---

#### Scenario C: NO NULL Values in Database
**Evidence**:
- Database query shows NO NULL values
- All columns have data

**Root Cause**: Different issue entirely (not related to owned entities)
**Next Steps**: Analyze actual exception from logs to determine real cause

---

#### Scenario D: Schema Issues
**Evidence**:
- Columns don't exist
- Columns have wrong types
- Constraints prevent updates

**Root Cause**: Migration not executed or partially executed
**Fix**: Rollback and re-apply migrations in correct order

---

### Phase 3: Remediation (ONLY AFTER ROOT CAUSE CONFIRMED)

**DO NOT PROCEED TO THIS PHASE WITHOUT COMPLETING PHASE 1 AND 2**

Remediation steps will be determined based on confirmed root cause from Phase 2.

---

## Lessons Learned & Prevention Strategy

### What Went Wrong

#### 1. Assumption-Driven Debugging
**Problem**: Created 5 migrations based on hypothesis without validating it
**Cost**: Hours of wasted effort, multiple failed deployments

**Prevention**:
- ✅ ALWAYS capture actual exception first
- ✅ ALWAYS query database state before assuming data issues
- ✅ ALWAYS verify schema state before schema migrations
- ✅ Test migrations locally before deploying to staging

---

#### 2. Missing Diagnostic Steps
**Problem**: Skipped evidence collection phase
**Cost**: Operating blind without knowing actual problem

**Prevention**:
- ✅ Create diagnostic playbook (see `scripts/diagnose-badge-500-error.ps1`)
- ✅ Use checklist for production issues
- ✅ Require evidence before hypothesis formation

---

#### 3. PostgreSQL EF Core Provider Behavior
**Problem**: Didn't understand how `AlterColumn` triggers automatic WHERE clauses
**Learning**: PostgreSQL provider adds safety checks when changing nullable columns

**Prevention**:
- ✅ Separate data migrations from schema migrations
- ✅ Use pure SQL for data fixes (avoid AlterColumn in same migration)
- ✅ Test migrations against PostgreSQL locally
- ✅ Review generated SQL in deployment logs

---

#### 4. Incomplete Understanding of EF Core Owned Entities
**Problem**: Didn't understand NULL materialization behavior
**Learning**: EF Core sets navigation property to null if ALL owned entity columns are NULL

**Prevention**:
- ✅ Add comprehensive comments in entity configuration
- ✅ Document EF Core gotchas in architecture docs
- ✅ Include defensive null checks as standard practice (already done in Phase 6A.31b)

---

## Production Issue Response Checklist

Use this checklist for ALL future production issues:

### Before Writing ANY Code
- [ ] Capture actual exception with full stack trace
- [ ] Query database state (if data-related)
- [ ] Verify schema state (if schema-related)
- [ ] Check deployment state (confirm code version)
- [ ] Attempt local reproduction
- [ ] Review recent changes (git log, deployment history)
- [ ] Consult architecture documentation

### After Evidence Collection
- [ ] Form hypothesis based on evidence
- [ ] Validate hypothesis with test case
- [ ] Design fix with minimal scope
- [ ] Test fix locally
- [ ] Review fix with team/architect
- [ ] Deploy fix to staging
- [ ] Verify fix resolves issue
- [ ] Update documentation

### After Resolution
- [ ] Document root cause analysis
- [ ] Update prevention strategies
- [ ] Create regression test
- [ ] Share learnings with team

---

## Recommended Next Steps

### Immediate Actions (Today)
1. **Run diagnostic script**: `./scripts/diagnose-badge-500-error.ps1`
2. **Capture Azure logs**: Get actual exception message
3. **Query staging database**: Verify NULL values exist
4. **Verify code deployment**: Confirm Phase 6A.31b is deployed

### After Evidence Collection (Next 2 Hours)
1. **Analyze evidence**: Determine which scenario (A, B, C, or D)
2. **Confirm root cause**: Update this RCA with findings
3. **Design fix**: Based on confirmed root cause
4. **Review with architect**: Get approval before deployment

### After Fix Deployment (Next Day)
1. **Verify resolution**: Test Badge API endpoint
2. **Monitor for 24 hours**: Ensure no regressions
3. **Update documentation**: Document actual root cause
4. **Create regression test**: Prevent future occurrences

---

## References

### Related Files
- `src/LankaConnect.Domain/Badges/BadgeLocationConfig.cs` - Value object
- `src/LankaConnect.Infrastructure/Data/Configurations/BadgeConfiguration.cs` - EF configuration
- `src/LankaConnect.Application/Badges/DTOs/BadgeMappingExtensions.cs` - Mapping with defensive nulls
- `src/LankaConnect.Application/Badges/Queries/GetBadgesQuery/GetBadgesQueryHandler.cs` - Error location

### Related Migrations
- `20251217053649_ApplyBadgeLocationConfigDefaults.cs`
- `20251217175941_EnforceBadgeLocationConfigNotNull.cs`
- `20251217205258_FixBadgeNullsDataOnly.cs`

### Diagnostic Scripts
- `scripts/diagnose-badge-500-error.ps1` - Comprehensive diagnostic playbook

---

## Document Status

**Current Phase**: Phase 1 - Evidence Collection
**Next Update**: After diagnostic evidence collected
**Owner**: Development Team
**Reviewer**: System Architect

---

## Appendix A: EF Core Owned Entity NULL Behavior

### Test Case
```csharp
// Scenario: All columns NULL in database
// Database state:
// position_x_listing: NULL
// position_y_listing: NULL
// size_width_listing: NULL
// size_height_listing: NULL
// rotation_listing: NULL

var badge = await context.Badges.FirstOrDefaultAsync();

// Result:
badge.ListingConfig == null  // TRUE! Navigation property is null

// This causes NullReferenceException:
badge.ListingConfig.PositionX  // BOOM! 💥
```

### Solution Options

**Option 1: Fix Data (Preferred)**
```sql
UPDATE badges.badges
SET position_x_listing = COALESCE(position_x_listing, 1.0),
    position_y_listing = COALESCE(position_y_listing, 0.0),
    size_width_listing = COALESCE(size_width_listing, 0.26),
    size_height_listing = COALESCE(size_height_listing, 0.26),
    rotation_listing = COALESCE(rotation_listing, 0.0);
```

**Option 2: Defensive Null Handling (Already Implemented)**
```csharp
public static BadgeLocationConfigDto ToDto(this BadgeLocationConfig? config)
{
    if (config == null)  // Handle null navigation property
    {
        return new BadgeLocationConfigDto { /* defaults */ };
    }
    // ...
}
```

**Option 3: Required Navigation Property (Not Recommended)**
```csharp
// In BadgeConfiguration.cs
builder.OwnsOne(b => b.ListingConfig)
    .IsRequired();  // Forces EF Core to throw if NULL

// Problem: This throws exception during query, not helpful
```

---

## Appendix B: PostgreSQL AlterColumn Behavior

### What Happens When Changing Nullable → NOT NULL

```csharp
// Migration code
migrationBuilder.AlterColumn<decimal>(
    name: "position_x_listing",
    table: "badges",
    nullable: false,  // Changed from true
    oldNullable: true
);

// PostgreSQL EF Core provider generates:
ALTER TABLE badges.badges
ALTER COLUMN position_x_listing SET NOT NULL;

// But BEFORE that, it adds safety check:
UPDATE badges.badges
SET position_x_listing = DEFAULT
WHERE position_x_listing IS NULL;  // ⚠️ Automatically added WHERE clause

// This overrides any unconditional UPDATE you placed before AlterColumn()
```

### Solution: Separate Migrations

```csharp
// Migration 1: Data-only (no AlterColumn)
public partial class FixBadgeNullsDataOnly : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Pure SQL, no schema changes
        migrationBuilder.Sql(@"
            UPDATE badges.badges
            SET position_x_listing = COALESCE(position_x_listing, 1.0),
                position_y_listing = COALESCE(position_y_listing, 0.0);
        ");
    }
}

// Migration 2: Schema-only (deployed AFTER data fixed)
public partial class EnforceBadgeNullsSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Now safe to enforce NOT NULL
        migrationBuilder.AlterColumn<decimal>(
            name: "position_x_listing",
            nullable: false
        );
    }
}
```

---

**END OF RCA DOCUMENT**
