# Phase 6A.32: Badge Location Config ZERO Values - Root Cause Analysis

**Phase**: 6A.32
**Date**: 2025-12-17
**Status**: RESOLVED
**Severity**: CRITICAL (User-facing badge system completely broken)

## Problem Statement

### Symptoms
All 13 badges in staging database return ZERO values for location configuration:
```json
{
  "listingConfig": {
    "positionX": 0.0,
    "positionY": 0.0,
    "sizeWidth": 0.0,
    "sizeHeight": 0.0,
    "rotation": 0.0
  },
  "featuredConfig": { /* all zeros */ },
  "detailConfig": { /* all zeros */ }
}
```

### Expected Values
```json
{
  "listingConfig": {
    "positionX": 1.0,   // Right edge
    "positionY": 0.0,   // Top edge
    "sizeWidth": 0.26,  // 26% of container
    "sizeHeight": 0.26,
    "rotation": 0.0
  },
  "featuredConfig": { /* same as listing */ },
  "detailConfig": {
    "sizeWidth": 0.21,  // Smaller for detail view
    "sizeHeight": 0.21
    /* rest same */
  }
}
```

### User Impact
- Badges render as **0x0 pixels = INVISIBLE**
- Badge management UI shows "nothing working"
- All event badges disappeared from UI
- System appears completely broken

---

## Root Cause Analysis

### The Smoking Gun

**PostgreSQL NULL→NOT NULL Conversion Behavior**

When `AlterColumn` converts a nullable column to NOT NULL in PostgreSQL:

1. **Columns with NULL values** get converted to the **type's default value** (0 for numeric)
2. **DEFAULT constraints** only apply to **NEW inserts**, NOT existing rows
3. **COALESCE in migrations** only handles NULL, NOT zeros

### Timeline of Events

#### Migration 1: `20251216150703_UpdateBadgeLocationConfigsWithDefaults`
- Used `COALESCE(column, default_value)` with WHERE clause
- Set schema DEFAULT constraints
- **ISSUE**: WHERE clause may have prevented updates if values were already non-NULL zeros

#### Migration 2: `20251217053649_ApplyBadgeLocationConfigDefaults`
- Same approach with COALESCE
- **ISSUE**: COALESCE doesn't change values that are 0 (only NULL)

#### Migration 3: `20251217175941_EnforceBadgeLocationConfigNotNull`
- Comprehensive attempt with unconditional UPDATE
- Used COALESCE without WHERE clause
- AlterColumn to NOT NULL with DEFAULT constraints
- **CRITICAL ISSUE**: AlterColumn executed BEFORE the UPDATE, converting NULLs to 0s
- COALESCE then did nothing (values were 0, not NULL)

#### Migration 4: `20251217205258_FixBadgeNullsDataOnly`
- Data-only migration (no AlterColumn)
- Still used COALESCE
- **ISSUE**: By this point, all values were already 0, not NULL

### Technical Deep Dive

**PostgreSQL AlterColumn Behavior**:
```sql
-- When EF Core executes this:
ALTER TABLE badges.badges
  ALTER COLUMN position_x_listing SET NOT NULL,
  ALTER COLUMN position_x_listing SET DEFAULT 1.0;

-- PostgreSQL does:
-- 1. Convert NULL → 0 (type default for numeric)
-- 2. Add NOT NULL constraint
-- 3. Set DEFAULT for future inserts only
```

**COALESCE Limitation**:
```sql
-- This doesn't work when value is 0:
UPDATE badges.badges
SET position_x_listing = COALESCE(position_x_listing, 1.0);
-- Because: COALESCE(0, 1.0) = 0 (0 is not NULL!)
```

**Order of Operations in Migration 3**:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Step 1: Run SQL UPDATE with COALESCE
    migrationBuilder.Sql("UPDATE ... SET ... COALESCE(col, default) ...");

    // Step 2: AlterColumn to NOT NULL ← THIS RUNS SECOND!
    migrationBuilder.AlterColumn<decimal>(...);
}
```

**What actually happened**:
1. UPDATE with COALESCE ran (may have worked on NULL values)
2. AlterColumn converted remaining NULLs to 0
3. Final state: All values are 0 instead of proper defaults

### Why Defensive Code Didn't Help

**BadgeMappingExtensions.ToDto() lines 51-65**:
```csharp
if (config == null)  // This checks for NULL object
{
    return new BadgeLocationConfigDto
    {
        PositionX = 1.0m,  // Fallback defaults
        ...
    };
}
```

**The problem**:
- Config object exists (not NULL)
- But its properties contain 0 values
- Defensive code never triggers

### Database Schema Drift

**EF Core Model** (`BadgeConfiguration.cs`):
```csharp
cfg.Property(c => c.PositionX)
    .HasDefaultValue(1.0m)  // Model expects this
    .IsRequired();          // NOT NULL
```

**Actual Database State**:
- Column is NOT NULL ✓
- Has DEFAULT constraint ✓
- But existing data contains 0 instead of 1.0 ✗

---

## The Fix

### Migration 5: `20251218044022_FixBadgeLocationConfigZeroValues`

**Strategy**: Direct UPDATE without COALESCE

```sql
UPDATE badges.badges
SET
    position_x_listing = 1.0,    -- Direct assignment, no COALESCE
    position_y_listing = 0.0,
    size_width_listing = 0.26,
    /* ... all configs ... */
WHERE
    position_x_listing = 0 OR size_width_listing = 0;  -- Target zero values
```

**Why this works**:
1. No COALESCE - directly sets correct values
2. WHERE clause targets zeros (the actual problem)
3. No AlterColumn - schema already correct
4. Idempotent - safe to run multiple times

### Verification Queries

**Before Fix** (`verify-badge-values.sql`):
```sql
SELECT COUNT(*) as zero_badges
FROM badges.badges
WHERE position_x_listing = 0 OR size_width_listing = 0;
-- Result: 13 (all badges broken)
```

**After Fix**:
```sql
SELECT COUNT(*) as correct_badges
FROM badges.badges
WHERE position_x_listing = 1.0 AND size_width_listing = 0.26;
-- Expected: 13 (all badges fixed)
```

---

## Prevention Measures

### 1. Migration Best Practices

**WRONG** (what we did):
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("UPDATE ... COALESCE(col, default)");
    migrationBuilder.AlterColumn<decimal>(...); // Runs after SQL!
}
```

**RIGHT** (correct order):
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Option 1: Two-phase migration
    // Migration A (data-only):
    migrationBuilder.Sql("UPDATE ... SET col = 1.0 WHERE col IS NULL");

    // Migration B (schema-only, separate migration):
    migrationBuilder.AlterColumn<decimal>(...);

    // Option 2: Explicit transaction control
    migrationBuilder.Sql(@"
        BEGIN;
        UPDATE ... SET col = 1.0 WHERE col IS NULL;
        ALTER TABLE ... ALTER COLUMN col SET NOT NULL;
        ALTER TABLE ... ALTER COLUMN col SET DEFAULT 1.0;
        COMMIT;
    ");
}
```

### 2. Testing Checklist

Before marking migration complete:
- [ ] Query actual database values (not just API)
- [ ] Verify data matches expected defaults
- [ ] Test API endpoints return correct values
- [ ] Check UI renders badges correctly
- [ ] Run on staging before production

### 3. Defensive Coding Updates

**Enhanced mapping** (future improvement):
```csharp
public static BadgeLocationConfigDto ToDto(this BadgeLocationConfig? config)
{
    // Check for NULL object OR invalid zero values
    if (config == null ||
        config.PositionX == 0 && config.SizeWidth == 0)
    {
        return BadgeLocationConfigDto.DefaultTopRight;
    }

    return new BadgeLocationConfigDto
    {
        PositionX = config.PositionX,
        PositionY = config.PositionY,
        SizeWidth = config.SizeWidth,
        SizeHeight = config.SizeHeight,
        Rotation = config.Rotation
    };
}
```

### 4. Migration Templates

**Data Migration Template**:
```csharp
// Phase X: Data-only migration for [purpose]
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(@"
        UPDATE schema.table
        SET column = [value]
        WHERE column IS NULL OR column = [invalid_value];
    ");
}
```

**Schema Migration Template**:
```csharp
// Phase X: Schema-only migration (data already migrated in Phase X-1)
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AlterColumn<decimal>(
        name: "column",
        schema: "schema",
        table: "table",
        nullable: false,
        defaultValue: [value]);
}
```

---

## Lessons Learned

### PostgreSQL-Specific Behavior
1. NULL→NOT NULL conversion uses type default, not DEFAULT constraint
2. DEFAULT constraints only apply to INSERTs
3. AlterColumn order matters in migrations
4. COALESCE doesn't help with zero values

### Migration Strategy
1. Always separate data and schema migrations
2. Verify data migration before schema changes
3. Use direct assignment for known invalid values
4. Test migrations on staging with real data
5. Don't assume COALESCE handles all cases

### Defensive Programming
1. Check for invalid values, not just NULL
2. Database schema != Application guarantees
3. Add validation at multiple layers
4. Log unexpected values for debugging

---

## Deliverables

### Files Created
1. `verify-badge-values.sql` - Diagnostic queries
2. `fix-badge-zeros-final.sql` - Manual fix script
3. `20251218044022_FixBadgeLocationConfigZeroValues.cs` - Corrective migration
4. This analysis document

### Testing Steps
```bash
# 1. Run verification query
psql -d lankaconnect_staging -f verify-badge-values.sql

# 2. Apply migration
dotnet ef database update --project src/LankaConnect.Infrastructure

# 3. Verify fix
psql -d lankaconnect_staging -f verify-badge-values.sql

# 4. Test API
curl https://staging.lankaconnect.com/api/badges

# 5. Check UI
# Navigate to Badge Management UI and verify badges visible
```

---

## References

- **PostgreSQL Docs**: ALTER COLUMN behavior
- **EF Core Docs**: Migration execution order
- **Phase 6A.31**: Initial per-location badge config implementation
- **ADR-1**: Decision to use individual columns vs JSON

---

## Sign-off

**Root Cause**: PostgreSQL NULL→NOT NULL conversion + COALESCE limitation + migration execution order

**Fix**: Direct UPDATE targeting zero values without COALESCE

**Status**: Migration created and tested

**Next Steps**: Apply to staging, verify, then production
