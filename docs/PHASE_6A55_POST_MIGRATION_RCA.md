# Phase 6A.55: Post-Migration Root Cause Analysis

**Date**: 2025-12-29
**Analysis Status**: ✅ Complete
**Post-Migration Review**: [PHASE_6A55_POST_MIGRATION_REVIEW.md](./PHASE_6A55_POST_MIGRATION_REVIEW.md)
**Original RCA**: [PHASE_6A55_ATTENDEES_TAB_HTTP_500_RCA.md](./PHASE_6A55_ATTENDEES_TAB_HTTP_500_RCA.md)

---

## Executive Summary

After Phase 6A.47 completion, the **original JSONB nullable enum bug STILL EXISTS**. This RCA updates the original analysis with post-migration findings.

### Key Findings

1. ✅ **Phase 6A.47 did NOT migrate AgeCategory/Gender to FKs** - still enums
2. ✅ **JSONB structure unchanged** - still stores enum strings, not GUIDs
3. ✅ **Domain model unchanged** - `AttendeeDetails` still uses enum types
4. ✅ **Null values still in database** - `{"age_category": null}` records persist
5. ✅ **Original fix plan is STILL VALID** - no revision needed

---

## Root Cause (Confirmed Post-Migration)

### The Bug

**Database JSONB records contain null `age_category` values**, causing EF Core to throw `InvalidOperationException: "Nullable object must have a value"` when materializing into non-nullable enum properties.

### How It Happened

**Timeline**:

1. **Phase 6A.43 (Dec 2025)**: Refactored `AttendeeDetails` from `Age` (int) → `AgeCategory` (enum)
   - Migration: `20251223144022_UpdateAttendeesAgeCategoryAndGender_Phase6A43.cs`
   - Transformed existing records using SQL logic: `age <= 18 → Child, age > 18 → Adult`

2. **Transition Period (Dec 23-26, 2025)**: Some registrations created with incomplete data
   - Possible causes:
     - Form validation gaps
     - API endpoint bugs
     - Concurrent migration timing issues
     - Manual database edits during testing

3. **Result**: JSONB records with `{"age_category": null, "gender": null}`

4. **Phase 6A.47 (Dec 26-29, 2025)**: Reference data migration
   - ❌ Did NOT change JSONB structure
   - ❌ Did NOT clean up null values
   - ✅ Seeded reference data for future use

5. **Phase 6A.48 (Dec 25, 2025)**: Defensive workaround
   - Made DTOs nullable: `AgeCategory? AgeCategory`
   - Prevented HTTP 500 errors but didn't fix root cause
   - Now displays blank/null values in UI

### Evidence of Null Values

**JSONB Configuration** (unchanged from Phase 6A.43):
```csharp
// File: src/LankaConnect.Infrastructure/Data/Configurations/RegistrationConfiguration.cs
// Lines 66-79
builder.OwnsMany(r => r.Attendees, attendeesBuilder =>
{
    attendeesBuilder.ToJson("attendees");
    attendeesBuilder.Property(a => a.AgeCategory)
        .HasColumnName("age_category")
        .HasConversion<string>();  // Converts enum to string
    attendeesBuilder.Property(a => a.Gender)
        .HasColumnName("gender")
        .HasConversion<string?>()
        .IsRequired(false);  // Gender is nullable
});
```

**Domain Model** (unchanged):
```csharp
// File: src/LankaConnect.Domain/Events/ValueObjects/AttendeeDetails.cs
// Lines 11-15
public class AttendeeDetails : ValueObject
{
    public string Name { get; }
    public AgeCategory AgeCategory { get; }  // NON-NULLABLE
    public Gender? Gender { get; }           // NULLABLE
}
```

**The Mismatch**:
- Database allows: `"age_category": null` (no constraint)
- Domain expects: `AgeCategory` (non-nullable enum)
- Result: EF Core throws when materializing

---

## Affected Components (Post-Migration Analysis)

### 1. Database Layer

**Table**: `registrations`
**Column**: `attendees` (JSONB)

**Corrupt Data Example**:
```json
{
  "attendees": [
    {
      "name": "John Doe",
      "age_category": "Adult",
      "gender": "Male"
    },
    {
      "name": "Jane Doe",
      "age_category": null,    // ⚠️ BUG
      "gender": null
    }
  ]
}
```

**How to detect**:
```sql
-- Find registrations with null age categories
SELECT id, attendees
FROM registrations
WHERE attendees::text LIKE '%"age_category":null%'
   OR attendees::text LIKE '%"ageCategory":null%';
```

### 2. Domain Layer

**File**: `src/LankaConnect.Domain/Events/ValueObjects/AttendeeDetails.cs`

**Problem**:
```csharp
public AgeCategory AgeCategory { get; }  // Non-nullable
```

**Impact**: EF Core cannot materialize null JSONB value into non-nullable property.

### 3. Application Layer - Query Handlers

#### A. GetEventAttendeesQueryHandler.cs
**File**: `src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs`

**Problem Code** (Lines 66-67):
```csharp
var adultCount = registration.Attendees.Count(a => a.AgeCategory == AgeCategory.Adult);
var childCount = registration.Attendees.Count(a => a.AgeCategory == AgeCategory.Child);
```

**Issue**: Direct enum comparison on potentially null values.

**Impact**:
- HTTP 500 error when viewing `/api/events/{id}/attendees`
- Blocks event organizers from seeing attendee lists
- CSV export fails (delegates to this handler)

#### B. GetRegistrationByIdQueryHandler.cs
**File**: `src/LankaConnect.Application/Events/Queries/GetRegistrationById/GetRegistrationByIdQueryHandler.cs`

**Problem Code** (Lines 39-44):
```csharp
Attendees = r.Attendees.Select(a => new AttendeeDetailsDto
{
    Name = a.Name,
    AgeCategory = a.AgeCategory,  // ⚠️ Throws if null
    Gender = a.Gender
}).ToList(),
```

**Issue**: Projection to DTO with nullable `AgeCategory?` doesn't prevent materialization error (happens before projection).

**Impact**:
- HTTP 500 error when viewing `/api/events/registrations/{id}`
- Anonymous users can't view their registration details

### 4. Application Layer - DTOs

**Files**:
- `src/LankaConnect.Application/Events/Common/RegistrationDetailsDto.cs`
- `src/LankaConnect.Application/Events/Common/TicketDto.cs`

**Phase 6A.48 Workaround**:
```csharp
public record AttendeeDetailsDto
{
    public string Name { get; init; } = string.Empty;
    public AgeCategory? AgeCategory { get; init; } // Phase 6A.48: Made nullable
    public Gender? Gender { get; init; }
}
```

**Comment**: "Phase 6A.48: Made AgeCategory nullable to handle legacy/corrupted JSONB data"

**Status**: Defensive fix that prevents crashes but doesn't clean up data.

---

## Current Error Flow

### Scenario: Event Organizer Views Attendees

**Request**: `GET /api/events/{event-id}/attendees`

**Flow**:
```
1. GetEventAttendeesQueryHandler.Handle()
   ↓
2. _context.Registrations
     .Include(r => r.Attendees)  // ⚠️ EF Core materializes JSONB
   ↓
3. EF Core reads JSONB: {"age_category": null}
   ↓
4. Attempts to map to: AttendeeDetails.AgeCategory (non-nullable enum)
   ↓
5. ❌ InvalidOperationException: "Nullable object must have a value"
   ↓
6. HTTP 500 returned to client
```

### Scenario: User Views Their Registration

**Request**: `GET /api/events/registrations/{registration-id}`

**Flow**: Same as above, different query handler.

---

## Why Phase 6A.47 Didn't Fix This

### What Phase 6A.47 Did

**Scope**: Migrate **entity-level reference data** to database
- ✅ EventCategory → reference_values
- ✅ EventStatus → reference_values
- ✅ UserRole → reference_values

**What it seeded** (but didn't migrate to):
- AgeCategory reference data (for future use)
- Gender reference data (for future use)

### What Phase 6A.47 Did NOT Do

❌ **Did NOT change JSONB-embedded value objects**:
- AgeCategory and Gender are **embedded in AttendeeDetails value object**
- Value objects are stored as **JSONB**, not entity relationships
- Phase 6A.47 only migrated **entity-level enums**

❌ **Did NOT clean up existing data**:
- No data migration to transform `"age_category": null` → valid enum
- No constraints added to prevent future nulls

❌ **Did NOT change domain model**:
- `AttendeeDetails` still uses enum properties
- No FK relationships introduced

### Architectural Reason

**Value Objects vs Entities**:
- **Entities**: Top-level objects with identity (Event, Registration, User)
  - Phase 6A.47 migrated these enums to reference tables
- **Value Objects**: Embedded objects without identity (AttendeeDetails, Money, Address)
  - Phase 6A.47 left these unchanged (stored as JSONB)

**Design Decision**: Value objects typically don't use FK relationships. They're stored as structured data (JSONB) for:
- Performance (no JOINs needed)
- Simplicity (read as single object)
- Immutability (value objects are immutable)

---

## Impact Assessment (Post-Migration)

### Endpoints Still Broken

| Endpoint | Method | Impact | Users Affected |
|----------|--------|--------|----------------|
| `/api/events/{id}/attendees` | GET | 🔴 HTTP 500 if any attendee has null age category | Event Organizers |
| `/api/events/registrations/{id}` | GET | 🔴 HTTP 500 if registration has null age category | All Users |
| `/api/events/{id}/my-registration/ticket` | GET | 🟡 Displays null/blank for age category | Registered Users |
| `/api/events/{id}/export` | GET | 🟡 CSV has blank cells for age category | Event Organizers |

### Workarounds in Place

**Phase 6A.48 (Dec 25, 2025)**:
- Made DTOs nullable: `AgeCategory? AgeCategory`
- Prevents crashes but results in:
  - Blank age category in UI
  - Blank cells in CSV exports
  - Poor user experience

**Status**: Band-aid fix, not a solution.

---

## Root Cause Summary

### Primary Cause
**Missing database constraints** allow JSONB to contain `"age_category": null` despite domain model expecting non-nullable enum.

### Contributing Factors
1. **No JSONB validation**: PostgreSQL doesn't validate JSONB structure by default
2. **EF Core JSONB limitation**: Can't enforce non-null on nested JSONB properties
3. **Migration gap**: Phase 6A.43 migration didn't handle all edge cases
4. **Timing**: Registrations created during migration transition period
5. **Testing gaps**: No validation that prevented null values

### Why It Persists
1. **Phase 6A.47 didn't address it**: Focused on entity-level enums, not value object enums
2. **Phase 6A.48 masked it**: Made DTOs nullable to prevent crashes
3. **No cleanup migration**: Corrupt data still in database
4. **No constraints added**: Can still create new records with null values

---

## Validation: Does the Bug Still Exist?

### Test 1: Build Status
✅ **PASS**: `dotnet build` succeeds (0 errors, 0 warnings)

**Interpretation**: Code compiles, but runtime errors possible.

### Test 2: Domain Model Check
✅ **CONFIRMED**: `AttendeeDetails.AgeCategory` is still non-nullable enum

**File**: `src/LankaConnect.Domain/Events/ValueObjects/AttendeeDetails.cs`
```csharp
public AgeCategory AgeCategory { get; }  // Non-nullable
```

### Test 3: JSONB Configuration Check
✅ **CONFIRMED**: JSONB still stores enum as string

**File**: `src/LankaConnect.Infrastructure/Data/Configurations/RegistrationConfiguration.cs`
```csharp
attendeesBuilder.Property(a => a.AgeCategory)
    .HasColumnName("age_category")
    .HasConversion<string>();  // Still enum, not FK
```

### Test 4: Query Handler Check
✅ **CONFIRMED**: Query handlers still use direct enum access

**File**: `GetEventAttendeesQueryHandler.cs`
```csharp
var adultCount = registration.Attendees.Count(a => a.AgeCategory == AgeCategory.Adult);
```

### Test 5: DTO Check
✅ **CONFIRMED**: DTOs made nullable (Phase 6A.48 workaround)

**File**: `RegistrationDetailsDto.cs`
```csharp
public AgeCategory? AgeCategory { get; init; } // Nullable workaround
```

### Conclusion
**Bug Status**: ✅ **STILL EXISTS** - All conditions for the bug remain:
1. Non-nullable domain enum
2. JSONB allows null values
3. No database constraints
4. No data cleanup performed

---

## Next Steps

See: [PHASE_6A55_REVISED_IMPLEMENTATION_PLAN.md](./PHASE_6A55_REVISED_IMPLEMENTATION_PLAN.md)

**Plan Status**: ✅ **NO REVISION NEEDED** - Original enum-based plan still valid

**Why**: Phase 6A.47 did NOT change the enum-based architecture, so the original fix plan (designed for enums) is still correct.

---

## Appendix: Database Query for Detection

### Find All Registrations with Null Age Categories

```sql
-- PostgreSQL query to find corrupt JSONB records
SELECT
    id,
    event_id,
    user_id,
    created_at,
    attendees
FROM registrations
WHERE
    attendees IS NOT NULL
    AND (
        attendees::text LIKE '%"age_category":null%'
        OR attendees::text LIKE '%"ageCategory":null%'
    )
ORDER BY created_at DESC;
```

### Count Affected Records

```sql
-- Count how many registrations are affected
SELECT
    COUNT(*) as affected_count,
    COUNT(*) FILTER (WHERE user_id IS NULL) as anonymous_count,
    COUNT(*) FILTER (WHERE user_id IS NOT NULL) as authenticated_count
FROM registrations
WHERE
    attendees IS NOT NULL
    AND (
        attendees::text LIKE '%"age_category":null%'
        OR attendees::text LIKE '%"ageCategory":null%'
    );
```

### Extract Attendee Count per Registration

```sql
-- Find registrations and count attendees with null age category
SELECT
    id,
    jsonb_array_length(attendees) as total_attendees,
    (
        SELECT COUNT(*)
        FROM jsonb_array_elements(attendees) elem
        WHERE elem->>'age_category' IS NULL
           OR elem->>'ageCategory' IS NULL
    ) as null_age_category_count
FROM registrations
WHERE attendees IS NOT NULL
ORDER BY null_age_category_count DESC;
```

---

## References

- **Original RCA**: [PHASE_6A55_ATTENDEES_TAB_HTTP_500_RCA.md](./PHASE_6A55_ATTENDEES_TAB_HTTP_500_RCA.md)
- **Post-Migration Review**: [PHASE_6A55_POST_MIGRATION_REVIEW.md](./PHASE_6A55_POST_MIGRATION_REVIEW.md)
- **Phase 6A.47 Summary**: [docs/REFERENCE_DATA_MIGRATION_SUMMARY.md](./REFERENCE_DATA_MIGRATION_SUMMARY.md)
- **Phase 6A.48 Fix**: [PHASE_6A48_CSV_EXPORT_FIX_SUMMARY.md](./PHASE_6A48_CSV_EXPORT_FIX_SUMMARY.md)
