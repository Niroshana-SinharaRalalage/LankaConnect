# Root Cause Analysis: Badge Location Config Migration Failures

**Date**: 2025-12-18
**Migration File**: `20251218044022_FixBadgeLocationConfigZeroValues.cs`
**Failures**: 2 consecutive failures with different errors
**Status**: BLOCKED - Requires corrective action

---

## Executive Summary

The migration `FixBadgeLocationConfigZeroValues` has failed twice due to **incorrect PostgreSQL column naming** in raw SQL UPDATE statements. The root cause is a **naming convention mismatch** between EF Core's PascalCase property names and PostgreSQL's actual column names.

**ROOT CAUSE**: EF Core does NOT use snake_case naming convention by default. Column names in PostgreSQL are stored as **PascalCase** (e.g., `UpdatedAt`, not `updated_at`).

**CRITICAL FINDING**: The `UpdatedAt` column should NOT be included in this data-fix migration at all.

---

## Evidence Analysis

### 1. EF Core Configuration Analysis

**File**: `src\LankaConnect.Infrastructure\Data\AppDbContext.cs`

- **NO snake_case naming convention configured**
- No `UseSnakeCaseNamingConvention()` call found
- No custom naming convention in `OnModelCreating()`
- EF Core default behavior: **PascalCase column names**

**File**: `src\LankaConnect.Infrastructure\Data\Configurations\BadgeConfiguration.cs`

```csharp
// Line 171: Audit field configuration
builder.Property(b => b.UpdatedAt);  // ← No explicit HasColumnName()
```

**Conclusion**: When `HasColumnName()` is NOT specified, EF Core uses the **property name as-is**: `UpdatedAt` (PascalCase).

---

### 2. Database Schema Evidence

**File**: `src\LankaConnect.Infrastructure\Migrations\AppDbContextModelSnapshot.cs` (Line 194-195)

```csharp
b.Property<DateTime?>("UpdatedAt")
    .HasColumnType("timestamp with time zone");
```

**Conclusion**: The actual PostgreSQL column name is `UpdatedAt` (PascalCase), not `updated_at`.

---

### 3. Migration Column Name Pattern Analysis

**Badge location config columns** (Lines 53-147 in BadgeConfiguration.cs):
- Explicitly use snake_case via `HasColumnName()`: `position_x_listing`, `size_width_listing`, etc.

**Audit columns** (Lines 167-171 in BadgeConfiguration.cs):
- NO explicit `HasColumnName()` → Default to PascalCase: `CreatedAt`, `UpdatedAt`

**Pattern**:
- Domain-specific columns: **Explicit snake_case via HasColumnName()**
- BaseEntity audit fields: **PascalCase (EF Core default)**

---

## Failure Timeline

### Failure 1: PostgreSQL Error 42703 (Run ID 20351908653)

**Error**: `column "updated_at" of relation "badges" does not exist`

**SQL Executed** (Lines 22-49 in migration):
```sql
UPDATE badges.badges
SET
    position_x_listing = 1.0,
    ...
    rotation_detail = 0.0,
    updated_at = NOW()  -- ❌ WRONG: Column doesn't exist
WHERE
    position_x_listing = 0 OR size_width_listing = 0;
```

**Analysis**:
- Developer assumed snake_case naming convention
- Actual column name is `UpdatedAt` (PascalCase)
- PostgreSQL case-sensitivity: `updated_at` ≠ `UpdatedAt`

---

### Failure 2: C# Compilation Error (Run ID 20353273583)

**Errors**:
- CS1003: Syntax error, ',' expected [Line 46]
- CS1010: Newline in constant

**SQL Attempted** (Line 46):
```sql
"UpdatedAt" = NOW()  -- ❌ WRONG: Quotes break C# verbatim string
```

**Analysis**:
- Developer correctly identified column name as `UpdatedAt`
- Attempted to use PostgreSQL quoted identifiers `"UpdatedAt"`
- C# verbatim string `@"..."` doesn't escape double quotes properly
- Double quotes inside verbatim string must be escaped as `""`

**Correct C# syntax** would be:
```csharp
migrationBuilder.Sql(@"
    UPDATE badges.badges
    SET
        ...
        ""UpdatedAt"" = NOW()  -- ✅ Escaped quotes in C# verbatim string
");
```

---

## Questions Answered

### Q1: What is the CORRECT column name in PostgreSQL for the UpdatedAt property?

**Answer**: `UpdatedAt` (PascalCase, no quotes needed in SQL)

**Evidence**: AppDbContextModelSnapshot.cs Line 194, BadgeConfiguration.cs Line 171

---

### Q2: How should it be referenced in `migrationBuilder.Sql()` statements?

**Answer (Option A - Recommended)**: Use column name without quotes:
```sql
UPDATE badges.badges
SET
    position_x_listing = 1.0,
    "UpdatedAt" = NOW()  -- PostgreSQL preserves case in quotes
```

**Answer (Option B - If quotes needed in C#)**: Escape quotes in verbatim string:
```csharp
migrationBuilder.Sql(@"
    UPDATE badges.badges
    SET
        ""UpdatedAt"" = NOW()  -- Two double quotes = escape in C# verbatim string
");
```

**Best Practice**: Since PostgreSQL column names are case-insensitive when unquoted, and EF Core creates them as PascalCase mixed-case identifiers, they were likely created **with quotes** during table creation. Therefore, **quotes are required** in UPDATE statements:

```sql
UPDATE badges.badges
SET "UpdatedAt" = NOW()  -- ✅ CORRECT
```

In C# verbatim strings:
```csharp
@"""UpdatedAt"" = NOW()"  // ✅ CORRECT
```

---

### Q3: Is this column name issue the ROOT cause, or is there a deeper problem?

**Answer**: The column name issue is **superficial**. The ROOT cause is **architectural**:

1. **PRIMARY ROOT CAUSE**: Migration is updating `UpdatedAt` in a **data-fix migration**, violating separation of concerns
2. **SECONDARY CAUSE**: Column naming convention mismatch (snake_case vs PascalCase)
3. **TERTIARY CAUSE**: C# string escaping syntax error

---

### Q4: Should we even be updating UpdatedAt in this migration?

**Answer**: **NO. UpdatedAt should NOT be in this migration.**

**Reasons**:

1. **Audit Trail Integrity**: `UpdatedAt` tracks **application-level changes**, not database maintenance
2. **Migration Purpose**: This is a **data-fix migration** to correct zero values, not a business logic change
3. **Consistency**: Previous similar migrations (`20251217053649_ApplyBadgeLocationConfigDefaults.cs`) do NOT update `UpdatedAt`
4. **BaseEntity Design**: `UpdatedAt` is set by `MarkAsUpdated()` method, which is called by domain entity methods, not SQL scripts

**Evidence**: Previous migration `ApplyBadgeLocationConfigDefaults` (Lines 16-41) does NOT touch `UpdatedAt`:

```sql
UPDATE badges.badges
SET
    position_x_listing = COALESCE(position_x_listing, 1.0),
    ...
    rotation_detail = COALESCE(rotation_detail, 0.0)
    -- ✅ No UpdatedAt column here
WHERE
    position_x_listing IS NULL OR ...;
```

---

### Q5: What are the ROOT CAUSE categories?

**ROOT CAUSE CLASSIFICATION**:

**a) Column name mismatch** ✅ YES
- Developer assumed snake_case convention
- EF Core uses PascalCase by default for audit fields

**b) C# string escaping** ✅ YES (secondary)
- Incorrect use of quotes in verbatim strings

**c) Incorrect migration approach** ✅ YES (primary)
- Including audit columns in data-fix migration violates best practices
- Audit fields should only be updated by application code via EF Core

**d) Something else entirely** ✅ YES (root cause)
- **Lack of migration pattern documentation**
- **Inconsistent column naming conventions** (snake_case for domain, PascalCase for audit)
- **No automated tests for migration SQL syntax**

---

## Correct Fix

### Option 1: Remove UpdatedAt (RECOMMENDED)

**File**: `src\LankaConnect.Infrastructure\Data\Migrations\20251218044022_FixBadgeLocationConfigZeroValues.cs`

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Phase 6A.32: Fix for badge location config ZERO values issue
    // ROOT CAUSE: When PostgreSQL converts nullable columns to NOT NULL via AlterColumn,
    // existing NULL values are converted to the type's default (0 for numeric) rather than
    // using the DEFAULT constraint value. Previous migrations used COALESCE which only
    // handles NULL, not zeros.
    //
    // SOLUTION: Directly UPDATE all badges with correct default values without COALESCE.
    // This ensures badges with zero values (from NULL->NOT NULL conversion) get proper defaults.

    migrationBuilder.Sql(@"
        UPDATE badges.badges
        SET
            -- Listing config: TopRight (x=1.0, y=0.0) with 26% size
            position_x_listing = 1.0,
            position_y_listing = 0.0,
            size_width_listing = 0.26,
            size_height_listing = 0.26,
            rotation_listing = 0.0,

            -- Featured config: TopRight (x=1.0, y=0.0) with 26% size
            position_x_featured = 1.0,
            position_y_featured = 0.0,
            size_width_featured = 0.26,
            size_height_featured = 0.26,
            rotation_featured = 0.0,

            -- Detail config: TopRight (x=1.0, y=0.0) with 21% size (smaller for large images)
            position_x_detail = 1.0,
            position_y_detail = 0.0,
            size_width_detail = 0.21,
            size_height_detail = 0.21,
            rotation_detail = 0.0
        WHERE
            -- Only update badges with incorrect zero values (result of NULL->NOT NULL conversion)
            position_x_listing = 0 OR size_width_listing = 0;
    ");
}
```

**Changes**:
- ✅ Removed `UpdatedAt` column from UPDATE statement
- ✅ Removed trailing comma after `rotation_detail = 0.0`
- ✅ Migration is now purely a data-fix for location configs
- ✅ Audit trail integrity preserved

---

### Option 2: Include UpdatedAt with Correct Syntax (NOT RECOMMENDED)

If stakeholders insist on updating `UpdatedAt`:

```csharp
migrationBuilder.Sql(@"
    UPDATE badges.badges
    SET
        position_x_listing = 1.0,
        ...
        rotation_detail = 0.0,
        ""UpdatedAt"" = NOW() AT TIME ZONE 'UTC'  -- ✅ Escaped quotes, UTC timestamp
    WHERE
        position_x_listing = 0 OR size_width_listing = 0;
");
```

**Why NOT RECOMMENDED**:
- Breaks audit trail semantics (data fixes are not business updates)
- Inconsistent with existing migration patterns
- Unnecessary timestamp update for maintenance operations

---

## Testing Strategy

### 1. Pre-Migration Verification

**Query to check current data**:
```sql
-- Check badges with zero values
SELECT
    "Id",
    "Name",
    position_x_listing,
    size_width_listing,
    "UpdatedAt"
FROM badges.badges
WHERE position_x_listing = 0 OR size_width_listing = 0;
```

### 2. Post-Migration Verification

**Query to verify fix**:
```sql
-- All badges should have non-zero location configs
SELECT
    "Id",
    "Name",
    position_x_listing,
    size_width_listing,
    "UpdatedAt"
FROM badges.badges
WHERE position_x_listing = 0 OR size_width_listing = 0;
-- Expected: 0 rows
```

**Verify UpdatedAt unchanged**:
```sql
-- UpdatedAt should NOT change (if using Option 1)
SELECT
    "Id",
    "Name",
    "UpdatedAt"
FROM badges.badges
ORDER BY "UpdatedAt" DESC NULLS LAST;
```

### 3. Rollback Test

Since this is data-only fix with no schema changes, rollback is a no-op (correct).

---

## Recommendations

### Immediate Actions

1. ✅ **Update migration file** with Option 1 (remove `UpdatedAt`)
2. ✅ **Run compilation test** locally before pushing
3. ✅ **Execute in development environment** first
4. ✅ **Verify zero badges remain** after migration

### Long-term Improvements

1. **Document column naming conventions**:
   - Create `docs/database-naming-conventions.md`
   - Clarify snake_case vs PascalCase usage
   - Document when to use explicit `HasColumnName()`

2. **Migration SQL validation**:
   - Add pre-commit hook to validate SQL syntax
   - Consider using SQL parser to check column names against schema

3. **Standardize audit field handling**:
   - Document policy: "Data-fix migrations MUST NOT update audit fields"
   - Add code review checklist item

4. **Migration template**:
   - Create template for data-fix migrations
   - Include comments about audit field exclusion

5. **Consider snake_case globally**:
   - Evaluate adding `UseSnakeCaseNamingConvention()` to AppDbContext
   - Would require major migration to rename all existing columns
   - **NOT RECOMMENDED** for existing projects (breaking change)

---

## Conclusion

**ROOT CAUSE**: Incorrect PostgreSQL column naming in raw SQL (`updated_at` instead of `UpdatedAt`)

**FUNDAMENTAL ISSUE**: Including `UpdatedAt` in data-fix migration violates audit trail integrity

**CORRECT FIX**: Remove `UpdatedAt` from migration entirely (Option 1)

**COMPILATION TEST**: ✅ Required before deployment

**RISK LEVEL**: Low (data-only fix, no schema changes)

**ESTIMATED FIX TIME**: 5 minutes (edit migration file)

---

## Appendix: Column Naming Convention Summary

| Context | Naming Convention | Example | Configured Via |
|---------|------------------|---------|----------------|
| Domain Properties (Badge location) | snake_case | `position_x_listing` | `HasColumnName()` in Configuration |
| Audit Properties (BaseEntity) | PascalCase | `UpdatedAt`, `CreatedAt` | EF Core default (no HasColumnName) |
| Table Names | snake_case | `sign_up_lists` | `ToTable()` in AppDbContext |
| Schema Names | lowercase | `badges`, `events` | `ToTable()` in AppDbContext |

**Key Insight**: Mixed naming convention is intentional:
- **Domain-specific columns**: Explicit snake_case for PostgreSQL readability
- **Framework columns**: PascalCase (EF Core default) for consistency with C# properties

**Best Practice**: Always check `HasColumnName()` in entity configuration before writing raw SQL.
