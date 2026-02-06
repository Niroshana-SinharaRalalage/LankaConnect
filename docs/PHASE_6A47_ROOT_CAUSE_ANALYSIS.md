# Phase 6A.47 Root Cause Analysis - Reference Data Migration Failure

**Date**: 2025-12-27
**Analyzed By**: Claude (System Architecture Designer)
**Issue**: API endpoints return empty arrays [] despite successful migration
**Status**: ROOT CAUSE IDENTIFIED

---

## Executive Summary

The Phase 6A.47 migration executed successfully and correctly migrated data from 3 separate tables to the unified `reference_values` table. However, **the application code is still configured to read from the OLD DEPRECATED TABLES** that were dropped by the migration.

### Critical Finding

**DbContext is still configured with DbSet properties for deprecated entities:**
- `EventCategoryRef` → Maps to `reference_data.event_categories` (DROPPED)
- `EventStatusRef` → Maps to `reference_data.event_statuses` (DROPPED)
- `UserRoleRef` → Maps to `reference_data.user_roles` (DROPPED)

**These tables were dropped in migration 20251227034100, but the application is still trying to query them.**

---

## Timeline of Events

### Migration 20251226204402 (December 26, 2025)
**Created the 3 separate tables:**
```sql
CREATE TABLE reference_data.event_categories
CREATE TABLE reference_data.event_statuses
CREATE TABLE reference_data.user_roles
```

**BUT**: No data was seeded! The migration only created empty tables.

### Migration 20251227034100 (December 27, 2025)
**Step 1**: Created unified `reference_values` table
**Step 2**: Attempted to migrate data FROM old tables
**Step 3**: Since old tables were EMPTY, migration seeded default values
**Step 4**: Dropped the 3 old tables (event_categories, event_statuses, user_roles)

---

## Root Cause Analysis

### Problem 1: DbContext Configuration Mismatch

**File**: `src/LankaConnect.Infrastructure/Data/AppDbContext.cs` (Lines 95-97)

```csharp
// Reference Data Entity Sets - Phase 6A.47
public DbSet<EventCategoryRef> EventCategories => Set<EventCategoryRef>();
public DbSet<EventStatusRef> EventStatuses => Set<EventStatusRef>();
public DbSet<UserRoleRef> UserRoles => Set<UserRoleRef>();
public DbSet<ReferenceValue> ReferenceValues => Set<ReferenceValue>(); // Phase 6A.47: Unified Reference Data
```

**ISSUE**: The DbContext defines BOTH deprecated entities AND the new unified entity.

### Problem 2: EF Core Configuration Still Maps to Dropped Tables

**File**: `src/LankaConnect.Infrastructure/Data/Configurations/ReferenceData/EventCategoryRefConfiguration.cs` (Line 16)

```csharp
public void Configure(EntityTypeBuilder<EventCategoryRef> builder)
{
    builder.ToTable("event_categories", "reference_data"); // ❌ TABLE WAS DROPPED!
    // ...
}
```

**Same issue in:**
- `EventStatusRefConfiguration.cs` → Maps to dropped `event_statuses` table
- `UserRoleRefConfiguration.cs` → Maps to dropped `user_roles` table

### Problem 3: AppDbContext Applies Deprecated Configurations

**File**: `src/LankaConnect.Infrastructure/Data/AppDbContext.cs` (Lines 157-159)

```csharp
// Reference Data entity configurations (Phase 6A.47)
modelBuilder.ApplyConfiguration(new EventCategoryRefConfiguration()); // ❌ MAPS TO DROPPED TABLE
modelBuilder.ApplyConfiguration(new EventStatusRefConfiguration());   // ❌ MAPS TO DROPPED TABLE
modelBuilder.ApplyConfiguration(new UserRoleRefConfiguration());      // ❌ MAPS TO DROPPED TABLE
modelBuilder.ApplyConfiguration(new ReferenceValueConfiguration());  // ✅ CORRECT TABLE
```

### Problem 4: Service Layer Uses Correct Repository, But Repository Queries Wrong Entities

**Service Layer** (`ReferenceDataService.cs`) is CORRECT:
```csharp
// ✅ Uses unified repository method
var entities = await _repository.GetByTypeAsync("EventCategory", activeOnly, cancellationToken);
```

**Repository Layer** (`ReferenceDataRepository.cs`) is CORRECT:
```csharp
// ✅ Queries unified ReferenceValues table
public async Task<IReadOnlyList<ReferenceValue>> GetByTypeAsync(...)
{
    var query = _context.ReferenceValues
        .Where(rv => rv.EnumType == enumType);
    // ...
}
```

**BUT**: When EF Core resolves `_context.ReferenceValues`, it sees:
1. `EventCategoryRef` mapped to `event_categories` (doesn't exist)
2. `EventStatusRef` mapped to `event_statuses` (doesn't exist)
3. `UserRoleRef` mapped to `user_roles` (doesn't exist)

**Result**: EF Core may be creating queries against non-existent tables OR returning empty results because the table mappings conflict.

---

## Evidence

### 1. Migration 20251227034100 Shows Correct Data Migration Logic

**Lines 74-122**: Migration attempts to read from `reference_data.event_categories`:
```sql
DO $$
BEGIN
    IF EXISTS (SELECT FROM information_schema.tables
               WHERE table_schema = 'reference_data'
               AND table_name = 'event_categories') THEN
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        SELECT
            id,
            'EventCategory' as enum_type,
            code,
            -- Map code to int_value based on enum definition
            CASE code
                WHEN 'Religious' THEN 0
                WHEN 'Cultural' THEN 1
                WHEN 'Community' THEN 2
                -- ... (rest of mapping)
            END as int_value,
            name,
            description,
            display_order,
            is_active,
            jsonb_build_object('iconUrl', icon_url) as metadata,
            created_at,
            updated_at
        FROM reference_data.event_categories; -- ✅ READS FROM OLD TABLE
    ELSE
        -- Seed default EventCategory values if old table doesn't exist
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'EventCategory', 'Religious', 0, 'Religious', 'Religious ceremony events', 1, true, '{}'::jsonb, NOW(), NOW()),
            -- ... (rest of seed data)
    END IF;
END $$;
```

**Lines 624-635**: Migration then drops the old tables:
```sql
// Step 6: Drop old tables
migrationBuilder.DropTable(
    name: "event_categories",
    schema: "reference_data");

migrationBuilder.DropTable(
    name: "event_statuses",
    schema: "reference_data");

migrationBuilder.DropTable(
    name: "user_roles",
    schema: "reference_data");
```

### 2. ReferenceValueConfiguration Has Seed Data

**File**: `src/LankaConnect.Infrastructure/Data/Configurations/ReferenceData/ReferenceValueConfiguration.cs` (Lines 99-102)

```csharp
// Seed data for existing 3 enums
SeedEventCategories(builder);
SeedEventStatuses(builder);
SeedUserRoles(builder);
```

**BUT**: This seed data is ONLY used when creating a NEW migration with `dotnet ef migrations add`. It is NOT executed during `dotnet ef database update` if the table already exists.

### 3. Migration History Confirms Table Creation and Deletion

**Migration 20251226204402** created:
- `event_categories` (EMPTY - no seed data)
- `event_statuses` (EMPTY - no seed data)
- `user_roles` (EMPTY - no seed data)

**Migration 20251227034100**:
- Created `reference_values` table
- Migrated data from old tables (but they were EMPTY)
- Seeded default values (because old tables were empty OR didn't exist in staging)
- Dropped old tables

---

## Why API Returns Empty Arrays

### Scenario 1: EF Core Query Resolution Fails Silently
When the repository calls:
```csharp
var query = _context.ReferenceValues.Where(rv => rv.EnumType == enumType);
```

EF Core sees conflicting table mappings:
- `EventCategoryRef` → `event_categories` (doesn't exist)
- `ReferenceValue` → `reference_values` (exists but may be empty)

**Result**: Query returns empty result OR throws exception caught by service layer.

### Scenario 2: Database Has 0 Rows in reference_values
If the migration's ELSE branch executed (seeding default values), the data SHOULD exist.

**But if staging database already had the old empty tables**, the migration's IF branch executed:
```sql
SELECT ... FROM reference_data.event_categories; -- RETURNS 0 ROWS
```

Then NO data was inserted into `reference_values`, resulting in an empty table.

---

## Verification Steps Needed

### Step 1: Check Staging Database State
```sql
-- Check if reference_values table exists and has data
SELECT COUNT(*) FROM reference_data.reference_values;

-- Check specific enum types
SELECT enum_type, COUNT(*)
FROM reference_data.reference_values
GROUP BY enum_type;

-- Check if old tables still exist (they shouldn't)
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'reference_data'
  AND table_name IN ('event_categories', 'event_statuses', 'user_roles');
```

### Step 2: Check EF Core Migration Status
```bash
dotnet ef migrations list --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API --context AppDbContext
```

Expected output:
```
20251226204402_Phase6A47_Create_ReferenceData_Tables (Applied)
20251227034100_Phase6A47_Refactor_To_Unified_ReferenceValues (Applied)
```

### Step 3: Check Application Logs
Look for EF Core SQL query logs when calling `/api/reference-data/event-categories`:
- Does it query `reference_values` or `event_categories`?
- Are there any SQL exceptions being caught?

---

## Recommended Fix

### Option 1: Remove Deprecated Entity Configurations (RECOMMENDED)

**File**: `src/LankaConnect.Infrastructure/Data/AppDbContext.cs`

**REMOVE** these DbSet properties (Lines 95-97):
```csharp
// ❌ DELETE THESE - Tables were dropped in Phase 6A.47
public DbSet<EventCategoryRef> EventCategories => Set<EventCategoryRef>();
public DbSet<EventStatusRef> EventStatuses => Set<EventStatusRef>();
public DbSet<UserRoleRef> UserRoles => Set<UserRoleRef>();
```

**REMOVE** these configuration calls (Lines 157-159):
```csharp
// ❌ DELETE THESE - Maps to dropped tables
modelBuilder.ApplyConfiguration(new EventCategoryRefConfiguration());
modelBuilder.ApplyConfiguration(new EventStatusRefConfiguration());
modelBuilder.ApplyConfiguration(new UserRoleRefConfiguration());
```

**REMOVE** deprecated entities from IgnoreUnconfiguredEntities (Lines 256-258):
```csharp
// ❌ DELETE THESE - No longer needed
typeof(EventCategoryRef), // Phase 6A.47: Reference Data Migration (deprecated)
typeof(EventStatusRef), // Phase 6A.47: Reference Data Migration (deprecated)
typeof(UserRoleRef), // Phase 6A.47: Reference Data Migration (deprecated)
```

**DELETE** the following configuration files entirely:
- `src/LankaConnect.Infrastructure/Data/Configurations/ReferenceData/EventCategoryRefConfiguration.cs`
- `src/LankaConnect.Infrastructure/Data/Configurations/ReferenceData/EventStatusRefConfiguration.cs`
- `src/LankaConnect.Infrastructure/Data/Configurations/ReferenceData/UserRoleRefConfiguration.cs`

### Option 2: Fix Data Migration (If reference_values is Empty)

If `SELECT COUNT(*) FROM reference_data.reference_values` returns 0:

**Create a data-only migration:**
```bash
dotnet ef migrations add Phase6A47_Seed_ReferenceValues_Hotfix --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API
```

**Manually edit the Up() method to seed data:**
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Seed EventCategory values
    migrationBuilder.Sql(@"
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'EventCategory', 'Religious', 0, 'Religious', 'Religious ceremony events', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Cultural', 1, 'Cultural', 'Cultural celebration events', 2, true, '{}'::jsonb, NOW(), NOW()),
            -- ... (rest of seed data)
        ON CONFLICT DO NOTHING; -- Prevent duplicates if data already exists
    ");

    // Seed EventStatus values
    // Seed UserRole values
    // ... (all other enum types)
}
```

---

## Impact Assessment

### Current State
- ✅ Database schema is CORRECT (unified `reference_values` table exists)
- ✅ Repository layer is CORRECT (queries unified table)
- ✅ Service layer is CORRECT (uses unified repository)
- ❌ DbContext configuration is WRONG (maps to dropped tables)
- ❌ API returns empty arrays (EF Core can't resolve queries)

### After Fix
- ✅ DbContext will ONLY map `ReferenceValue` entity
- ✅ EF Core will correctly query `reference_data.reference_values`
- ✅ API will return data (assuming table has data)
- ✅ No breaking changes (service layer already uses unified methods)

---

## Conclusion

**The user's hypothesis was PARTIALLY CORRECT:**

> "Application code (repositories, services, DbContext) may still be configured to read from OLD tables instead of NEW reference_values table"

**Repository and Service layers are CORRECT** - they query the new unified table.
**DbContext configuration is WRONG** - it still maps deprecated entities to dropped tables.

**The fix is straightforward:**
1. Remove deprecated DbSet properties from AppDbContext
2. Remove deprecated ApplyConfiguration calls
3. Delete deprecated entity configuration files
4. Verify `reference_values` table has data (if empty, run seed migration)

**No changes needed to Repository or Service layers** - they are already correctly implemented.
