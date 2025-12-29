# Phase 6A.55: Revised Implementation Plan - JSONB Nullable Enum Fix

**Date**: 2025-12-29
**Status**: ✅ **READY TO IMPLEMENT** - No revision needed, original enum-based plan is valid
**Priority**: P1 - HIGH (User-facing HTTP 500 errors)
**Estimated Time**: 6-8 hours (reduced from 9 hours due to Phase 6A.48 defensive fixes)
**Dependencies**: ✅ Phase 6A.47 complete (confirmed enums still used)

---

## Executive Summary

After Phase 6A.47 review, **the original fix plan is still valid**. Phase 6A.47 did NOT migrate AgeCategory/Gender to foreign keys - they remain enums stored in JSONB. The nullable enum bug persists exactly as before.

### Key Changes from Original Plan

1. ✅ **Phase 1 (Detection)**: Reduced to 30 min (Phase 6A.48 already added defensive DTOs)
2. ✅ **Phase 2 (Defensive Code)**: Reduced to 2 hours (DTOs already nullable, focus on query handlers)
3. ✅ **Phase 3 (Database Cleanup)**: Same (2 hours) - still needed
4. ✅ **Phase 4 (Database Constraints)**: Enhanced (1.5 hours) - add JSONB validation functions
5. ✅ **Phase 5 (Monitoring)**: Same (1 hour) - still needed

**Total Time**: 6-8 hours (reduced from 9 hours)

---

## Plan Overview

### 5 Phases

| Phase | Description | Time | Status |
|-------|-------------|------|--------|
| 1 | Detection & Analysis | 30 min | ⏳ Ready |
| 2 | Defensive Code Changes - TDD | 2 hours | ⏳ Ready |
| 3 | Database Cleanup | 2 hours | ⏳ Ready |
| 4 | Database Constraints | 1.5 hours | ⏳ Ready |
| 5 | Monitoring & Documentation | 1 hour | ⏳ Ready |

---

## Phase 1: Detection & Analysis (30 minutes)

**Reduced from 1 hour** - Phase 6A.48 already confirmed the issue exists.

### Goals
1. Quantify corrupt records in database
2. Identify which events/registrations affected
3. Determine if any new registrations still creating null values

### Tasks

#### Task 1.1: Run Database Detection Queries (10 min)

**Query 1: Find all affected registrations**
```sql
-- Find registrations with null age categories
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

**Query 2: Count by event**
```sql
-- Count affected registrations per event
SELECT
    e.id as event_id,
    e.title,
    COUNT(r.id) as affected_registrations,
    SUM(jsonb_array_length(r.attendees)) as total_attendees,
    SUM((
        SELECT COUNT(*)
        FROM jsonb_array_elements(r.attendees) elem
        WHERE elem->>'age_category' IS NULL
    )) as null_age_category_count
FROM events e
INNER JOIN registrations r ON r.event_id = e.id
WHERE r.attendees IS NOT NULL
  AND (
      r.attendees::text LIKE '%"age_category":null%'
      OR r.attendees::text LIKE '%"ageCategory":null%'
  )
GROUP BY e.id, e.title
ORDER BY affected_registrations DESC;
```

**Query 3: Check for recent null creations**
```sql
-- Check if null values created in last 7 days
SELECT
    COUNT(*) as recent_null_count
FROM registrations
WHERE
    attendees IS NOT NULL
    AND (
        attendees::text LIKE '%"age_category":null%'
        OR attendees::text LIKE '%"ageCategory":null%'
    )
    AND created_at > NOW() - INTERVAL '7 days';
```

**Expected Output**:
- Total affected registrations
- Number of attendees with null age category
- List of events affected
- Whether nulls are still being created

#### Task 1.2: Document Findings (10 min)

**Create**: `docs/PHASE_6A55_DETECTION_RESULTS.md`

**Contents**:
- Total corrupt records
- Events impacted
- Date range of affected registrations
- Whether nulls are actively being created
- Risk assessment for cleanup

#### Task 1.3: Review Phase 6A.48 Workaround (10 min)

**Check**:
- Verify DTOs are nullable (already done)
- Identify any query handlers NOT using defensive DTOs
- Confirm which endpoints still crash vs display blanks

**Files to review**:
- `src/LankaConnect.Application/Events/Common/RegistrationDetailsDto.cs`
- `src/LankaConnect.Application/Events/Common/TicketDto.cs`
- `src/LankaConnect.Application/Events/Common/EventAttendeeDto.cs`

---

## Phase 2: Defensive Code Changes - TDD (2 hours)

**Reduced from 4 hours** - DTOs already nullable (Phase 6A.48), focus on query handlers.

### Goals
1. Prevent HTTP 500 errors in ALL query handlers
2. Handle null age categories gracefully
3. Add tests to verify defensive handling

### Approach: Null-Coalescing with Default Values

**Strategy**: Map `null` → `AgeCategory.Adult` (default) at query projection layer.

**Why Adult as default**:
- Most registrations are adults (based on typical demographics)
- Better UX than displaying "Unknown" or blank
- Can be corrected later by user if wrong

### Task 2.1: Fix GetEventAttendeesQueryHandler (30 min)

**File**: `src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs`

**Current Code** (Lines 66-67):
```csharp
var adultCount = registration.Attendees.Count(a => a.AgeCategory == AgeCategory.Adult);
var childCount = registration.Attendees.Count(a => a.AgeCategory == AgeCategory.Child);
```

**Issue**: Direct enum comparison throws if materialization fails.

**Fix**:
```csharp
// Phase 6A.55: Handle null age categories defensively
// Map null → Adult (default) to prevent HTTP 500 errors
var attendeesWithDefaults = registration.Attendees.Select(a => new
{
    Name = a.Name,
    AgeCategory = a.AgeCategory,  // Will be null if JSONB has null
    Gender = a.Gender
}).ToList();

var adultCount = attendeesWithDefaults.Count(a =>
    a.AgeCategory == AgeCategory.Adult || a.AgeCategory == null);  // Null counts as Adult
var childCount = attendeesWithDefaults.Count(a =>
    a.AgeCategory == AgeCategory.Child);

// Map to DTOs with default for nulls
var attendeeDtos = attendeesWithDefaults.Select(a => new AttendeeDetailsDto
{
    Name = a.Name,
    AgeCategory = a.AgeCategory ?? AgeCategory.Adult,  // Default to Adult
    Gender = a.Gender
}).ToList();
```

**Write Test** (TDD):
```csharp
// File: tests/LankaConnect.Application.Tests/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandlerTests.cs

[Fact]
public async Task Handle_WithNullAgeCategory_ReturnsDefaultAdult()
{
    // Arrange
    var registration = new Registration(
        eventId: eventId,
        userId: userId,
        attendees: new List<AttendeeDetails>
        {
            AttendeeDetails.Create("John Doe", null, Gender.Male).Value  // null age category
        },
        contact: contactInfo,
        totalPrice: Money.Create(10, Currency.USD).Value
    );

    _context.Registrations.Add(registration);
    await _context.SaveChangesAsync();

    var query = new GetEventAttendeesQuery(eventId);

    // Act
    var result = await _handler.Handle(query, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    var response = result.Value;
    Assert.Equal(1, response.Attendees.Count);
    Assert.Equal(AgeCategory.Adult, response.Attendees[0].Attendees[0].AgeCategory);  // Defaulted to Adult
    Assert.Equal(1, response.Attendees[0].AdultCount);
    Assert.Equal(0, response.Attendees[0].ChildCount);
}
```

### Task 2.2: Fix GetRegistrationByIdQueryHandler (30 min)

**File**: `src/LankaConnect.Application/Events/Queries/GetRegistrationById/GetRegistrationByIdQueryHandler.cs`

**Current Code** (Lines 39-44):
```csharp
Attendees = r.Attendees.Select(a => new AttendeeDetailsDto
{
    Name = a.Name,
    AgeCategory = a.AgeCategory,  // Throws if null
    Gender = a.Gender
}).ToList(),
```

**Fix**:
```csharp
// Phase 6A.55: Handle null age categories defensively
Attendees = r.Attendees.Select(a => new AttendeeDetailsDto
{
    Name = a.Name,
    AgeCategory = a.AgeCategory ?? AgeCategory.Adult,  // Default to Adult
    Gender = a.Gender
}).ToList(),
```

**Note**: Since DTOs are already nullable (Phase 6A.48), we can just add null-coalescing.

**Write Test** (TDD):
```csharp
// File: tests/LankaConnect.Application.Tests/Events/Queries/GetRegistrationById/GetRegistrationByIdQueryHandlerTests.cs

[Fact]
public async Task Handle_WithNullAgeCategory_ReturnsDefaultAdult()
{
    // Arrange
    var registration = new Registration(
        eventId: eventId,
        userId: userId,
        attendees: new List<AttendeeDetails>
        {
            AttendeeDetails.Create("Jane Doe", null, null).Value  // null age category and gender
        },
        contact: contactInfo,
        totalPrice: Money.Create(15, Currency.USD).Value
    );

    _context.Registrations.Add(registration);
    await _context.SaveChangesAsync();

    var query = new GetRegistrationByIdQuery(registration.Id);

    // Act
    var result = await _handler.Handle(query, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    var dto = result.Value;
    Assert.NotNull(dto);
    Assert.Single(dto.Attendees);
    Assert.Equal(AgeCategory.Adult, dto.Attendees[0].AgeCategory);  // Defaulted to Adult
}
```

### Task 2.3: Review Other Query Handlers (30 min)

**Check for similar patterns in**:
- `GetUserRegistrationForEventQueryHandler.cs`
- `GetTicketQueryHandler.cs`
- Any other handlers that project attendees

**Apply same fix**: Null-coalescing to default `AgeCategory.Adult`.

### Task 2.4: Update DTOs Documentation (15 min)

**Files**:
- `src/LankaConnect.Application/Events/Common/RegistrationDetailsDto.cs`
- `src/LankaConnect.Application/Events/Common/TicketDto.cs`

**Update comments**:
```csharp
/// <summary>
/// Individual attendee details with age category and optional gender
/// Phase 6A.48: Made AgeCategory nullable to handle legacy/corrupted JSONB data
/// Phase 6A.55: Query handlers map null → Adult (default) for better UX
/// </summary>
public record AttendeeDetailsDto
{
    public string Name { get; init; } = string.Empty;
    public AgeCategory? AgeCategory { get; init; }  // Nullable for backward compatibility
    public Gender? Gender { get; init; }
}
```

### Task 2.5: Run Tests and Verify Build (15 min)

**Commands**:
```bash
dotnet test
dotnet build
```

**Expected**: All tests pass, 0 build errors.

---

## Phase 3: Database Cleanup (2 hours)

**Same as original plan** - Still needed to clean up corrupt data.

### Goals
1. Identify all JSONB records with null age categories
2. Apply heuristic to determine correct age category
3. Update JSONB records in-place
4. Verify data integrity after update

### Task 3.1: Create Data Cleanup Migration (30 min)

**File**: Create new migration `Phase6A55_CleanupNullAgeCategories.cs`

**Migration Strategy**:
1. Find all registrations with null age categories
2. Apply heuristic:
   - If `total_price_amount > 0`: Check pricing tiers to infer Adult vs Child
   - If no pricing info: Default to Adult (safest assumption)
   - Log all transformations for audit trail

**Migration Code**:
```sql
-- Phase 6A.55: Cleanup null age categories in JSONB attendees
DO $$
DECLARE
    reg RECORD;
    new_attendees jsonb;
    attendee jsonb;
    updated_count int := 0;
BEGIN
    -- Iterate through all registrations with null age categories
    FOR reg IN
        SELECT id, attendees
        FROM registrations
        WHERE attendees IS NOT NULL
          AND (
              attendees::text LIKE '%"age_category":null%'
              OR attendees::text LIKE '%"ageCategory":null%'
          )
    LOOP
        -- Build new attendees array with defaults applied
        new_attendees := '[]'::jsonb;

        FOR attendee IN SELECT * FROM jsonb_array_elements(reg.attendees)
        LOOP
            -- If age_category is null, default to 'Adult'
            IF attendee->>'age_category' IS NULL THEN
                attendee := jsonb_set(
                    attendee,
                    '{age_category}',
                    '"Adult"'::jsonb,
                    true
                );
                RAISE NOTICE 'Registration %: Defaulted null age_category to Adult for attendee %',
                    reg.id, attendee->>'name';
            END IF;

            -- Add updated attendee to new array
            new_attendees := new_attendees || jsonb_build_array(attendee);
        END LOOP;

        -- Update registration with cleaned attendees
        UPDATE registrations
        SET attendees = new_attendees,
            updated_at = NOW()
        WHERE id = reg.id;

        updated_count := updated_count + 1;
    END LOOP;

    RAISE NOTICE 'Phase 6A.55: Updated % registrations with null age categories', updated_count;
END $$;
```

### Task 3.2: Test Migration on Staging (30 min)

**Pre-migration checks**:
```sql
-- Count before cleanup
SELECT COUNT(*) as before_count
FROM registrations
WHERE attendees::text LIKE '%"age_category":null%';
```

**Run migration**:
```bash
dotnet ef migrations add Phase6A55_CleanupNullAgeCategories
dotnet ef database update
```

**Post-migration checks**:
```sql
-- Count after cleanup (should be 0)
SELECT COUNT(*) as after_count
FROM registrations
WHERE attendees::text LIKE '%"age_category":null%';

-- Verify all registrations have valid age categories
SELECT
    id,
    attendees
FROM registrations
WHERE attendees IS NOT NULL
  AND jsonb_array_length(attendees) > 0
LIMIT 10;
```

**Expected**: All null age categories replaced with "Adult".

### Task 3.3: Create Rollback Plan (30 min)

**Migration Down() method**:
```csharp
protected override void Down(MigrationBuilder migrationBuilder)
{
    // Phase 6A.55: Rollback not recommended as it would reintroduce nulls
    // If needed, restore from backup taken before migration
    migrationBuilder.Sql(@"
        -- No automated rollback - restore from backup if needed
        RAISE WARNING 'Phase 6A.55 rollback not automated. Restore from backup if needed.';
    ");
}
```

**Backup Strategy**:
```sql
-- Create backup before migration
CREATE TABLE registrations_backup_phase6a55 AS
SELECT * FROM registrations
WHERE attendees::text LIKE '%"age_category":null%';
```

### Task 3.4: Document Cleanup Results (30 min)

**Create**: `docs/PHASE_6A55_CLEANUP_RESULTS.md`

**Contents**:
- Number of registrations updated
- Number of attendees with null → Adult transformation
- List of events affected
- Any edge cases encountered
- Before/after JSONB examples

---

## Phase 4: Database Constraints (1.5 hours)

**Enhanced from 1 hour** - Add JSONB validation functions for better enforcement.

### Goals
1. Prevent future null age categories in JSONB
2. Add CHECK constraint using custom validation function
3. Ensure all new registrations have valid age categories

### Challenge: PostgreSQL JSONB Constraint Limitations

PostgreSQL cannot directly add CHECK constraints on nested JSONB properties. We need:
1. Create custom validation function
2. Add CHECK constraint calling that function
3. Test constraint enforcement

### Task 4.1: Create JSONB Validation Function (30 min)

**File**: Create migration `Phase6A55_AddAgeCategoryValidation.cs`

**SQL Function**:
```sql
-- Phase 6A.55: Create validation function for attendees JSONB
CREATE OR REPLACE FUNCTION validate_attendees_age_category(attendees jsonb)
RETURNS boolean AS $$
DECLARE
    attendee jsonb;
    age_category text;
BEGIN
    -- Return true if attendees is null (legacy format)
    IF attendees IS NULL THEN
        RETURN true;
    END IF;

    -- Iterate through attendees array
    FOR attendee IN SELECT * FROM jsonb_array_elements(attendees)
    LOOP
        -- Extract age_category
        age_category := attendee->>'age_category';

        -- Check if age_category exists and is valid
        IF age_category IS NULL THEN
            RETURN false;  -- Null age category not allowed
        END IF;

        -- Validate enum value
        IF age_category NOT IN ('Adult', 'Child') THEN
            RETURN false;  -- Invalid enum value
        END IF;
    END LOOP;

    RETURN true;
END;
$$ LANGUAGE plpgsql IMMUTABLE;
```

### Task 4.2: Add CHECK Constraint (30 min)

**Migration**:
```csharp
migrationBuilder.Sql(@"
    -- Phase 6A.55: Add CHECK constraint for attendees age category
    ALTER TABLE registrations
    ADD CONSTRAINT ck_registrations_attendees_age_category
    CHECK (
        attendees IS NULL  -- Legacy format allowed
        OR validate_attendees_age_category(attendees) = true
    );
");
```

### Task 4.3: Test Constraint Enforcement (30 min)

**Test 1: Valid insertion should succeed**
```sql
-- Should succeed: Valid age categories
INSERT INTO registrations (id, event_id, attendees, contact, created_at)
VALUES (
    gen_random_uuid(),
    (SELECT id FROM events LIMIT 1),
    '[{"name": "Test User", "age_category": "Adult", "gender": "Male"}]'::jsonb,
    '{"email": "test@example.com", "phone_number": "1234567890"}'::jsonb,
    NOW()
);
```

**Test 2: Null age category should fail**
```sql
-- Should fail: Null age category
INSERT INTO registrations (id, event_id, attendees, contact, created_at)
VALUES (
    gen_random_uuid(),
    (SELECT id FROM events LIMIT 1),
    '[{"name": "Test User", "age_category": null, "gender": "Male"}]'::jsonb,
    '{"email": "test@example.com", "phone_number": "1234567890"}'::jsonb,
    NOW()
);
-- Expected error: "new row violates check constraint ck_registrations_attendees_age_category"
```

**Test 3: Invalid enum value should fail**
```sql
-- Should fail: Invalid enum value
INSERT INTO registrations (id, event_id, attendees, contact, created_at)
VALUES (
    gen_random_uuid(),
    (SELECT id FROM events LIMIT 1),
    '[{"name": "Test User", "age_category": "InvalidValue", "gender": "Male"}]'::jsonb,
    '{"email": "test@example.com", "phone_number": "1234567890"}'::jsonb,
    NOW()
);
-- Expected error: "new row violates check constraint ck_registrations_attendees_age_category"
```

---

## Phase 5: Monitoring & Documentation (1 hour)

**Same as original plan** - Still needed for long-term maintenance.

### Task 5.1: Create Monitoring Query (15 min)

**File**: `scripts/monitor-jsonb-integrity.sql`

**Query**:
```sql
-- Phase 6A.55: Monitor JSONB data integrity
-- Run this weekly to detect any new null age categories

SELECT
    'Null Age Categories' as metric,
    COUNT(*) as count
FROM registrations
WHERE attendees::text LIKE '%"age_category":null%'

UNION ALL

SELECT
    'Invalid Age Categories' as metric,
    COUNT(*) as count
FROM registrations
WHERE attendees::text LIKE '%"age_category":"Invalid"%'

UNION ALL

SELECT
    'Missing Age Category Key' as metric,
    COUNT(*) as count
FROM registrations
WHERE attendees IS NOT NULL
  AND jsonb_array_length(attendees) > 0
  AND NOT EXISTS (
      SELECT 1
      FROM jsonb_array_elements(attendees) elem
      WHERE elem ? 'age_category'
  );
```

### Task 5.2: Update Documentation (30 min)

**Update files**:

1. **PROGRESS_TRACKER.md**:
   - Mark Phase 6A.55 as Complete
   - Link to summary docs

2. **STREAMLINED_ACTION_PLAN.md**:
   - Update Phase 6A.55 status
   - Add next steps section

3. **Create PHASE_6A55_SUMMARY.md**:
   - Overview of the fix
   - What was broken
   - What was fixed
   - How to prevent recurrence

### Task 5.3: Add Code Comments (15 min)

**Update query handlers with comments**:
```csharp
// Phase 6A.55: Handle null age categories defensively
// Background: JSONB records created during Phase 6A.43 migration may have null age_category
// Fix: Default null → Adult for better UX (most registrations are adults)
// Database cleanup: Migration Phase6A55_CleanupNullAgeCategories applied
// Constraints: CHECK constraint ck_registrations_attendees_age_category prevents future nulls
var attendeeDto = new AttendeeDetailsDto
{
    Name = a.Name,
    AgeCategory = a.AgeCategory ?? AgeCategory.Adult,  // Phase 6A.55: Default null → Adult
    Gender = a.Gender
};
```

---

## Implementation Timeline

### Day 1 (4 hours)
- ✅ Phase 1: Detection & Analysis (30 min)
- ✅ Phase 2: Defensive Code Changes - TDD (2 hours)
- ✅ Phase 3: Database Cleanup (started) (1.5 hours)

### Day 2 (3-4 hours)
- ✅ Phase 3: Database Cleanup (completed) (30 min)
- ✅ Phase 4: Database Constraints (1.5 hours)
- ✅ Phase 5: Monitoring & Documentation (1 hour)
- ✅ Testing and verification (30 min)

**Total**: 6-8 hours over 2 days

---

## Success Criteria

### Code Level
- ✅ All query handlers handle null age categories defensively
- ✅ DTOs use null-coalescing to default values
- ✅ Tests cover null age category scenarios
- ✅ Build succeeds with 0 errors, 0 warnings

### Database Level
- ✅ 0 registrations with `"age_category": null` in JSONB
- ✅ CHECK constraint prevents future null age categories
- ✅ Validation function enforces valid enum values

### Endpoint Level
- ✅ `/api/events/{id}/attendees` - No HTTP 500 errors
- ✅ `/api/events/registrations/{id}` - No HTTP 500 errors
- ✅ `/api/events/{id}/my-registration/ticket` - Displays valid age category
- ✅ `/api/events/{id}/export` - CSV exports with valid age categories

### User Experience
- ✅ Event organizers can view attendee lists
- ✅ Users can view their registration details
- ✅ CSV exports have populated age category column
- ✅ Ticket PDFs show age category

---

## Rollback Plan

### If Issues Arise During Implementation

**Phase 2 Rollback** (Defensive Code):
```bash
git revert <commit-hash>
dotnet build
dotnet test
```

**Phase 3 Rollback** (Database Cleanup):
```sql
-- Restore from backup
INSERT INTO registrations
SELECT * FROM registrations_backup_phase6a55;
```

**Phase 4 Rollback** (Constraints):
```sql
-- Drop constraint
ALTER TABLE registrations
DROP CONSTRAINT IF EXISTS ck_registrations_attendees_age_category;

-- Drop validation function
DROP FUNCTION IF EXISTS validate_attendees_age_category(jsonb);
```

---

## Post-Implementation Monitoring

### Week 1: Daily Checks
```sql
-- Run daily for first week
SELECT COUNT(*) as null_age_category_count
FROM registrations
WHERE attendees::text LIKE '%"age_category":null%';
```

**Expected**: 0 every day.

### Month 1: Weekly Checks
Run same query weekly. Alert if any non-zero counts.

### Month 2+: Monthly Checks
Integrate into monthly database health checks.

---

## Related Documents

- ✅ [PHASE_6A55_POST_MIGRATION_REVIEW.md](./PHASE_6A55_POST_MIGRATION_REVIEW.md) - What Phase 6A.47 changed
- ✅ [PHASE_6A55_POST_MIGRATION_RCA.md](./PHASE_6A55_POST_MIGRATION_RCA.md) - Root cause analysis
- ✅ [PHASE_6A55_ATTENDEES_TAB_HTTP_500_RCA.md](./PHASE_6A55_ATTENDEES_TAB_HTTP_500_RCA.md) - Original RCA
- ✅ [PHASE_6A55_MASTER_PLAN_ON_HOLD.md](./PHASE_6A55_MASTER_PLAN_ON_HOLD.md) - Why this was on hold
- ✅ [PHASE_6A48_CSV_EXPORT_FIX_SUMMARY.md](./PHASE_6A48_CSV_EXPORT_FIX_SUMMARY.md) - Defensive DTO fixes

---

## Conclusion

This revised plan leverages Phase 6A.48's defensive DTO fixes while adding:
1. ✅ Query handler null-coalescing (defensive)
2. ✅ Database cleanup migration (fix existing data)
3. ✅ JSONB validation constraints (prevent future issues)
4. ✅ Monitoring and documentation (long-term maintenance)

**Status**: ✅ **READY TO IMPLEMENT** - No blockers, clear path forward.
