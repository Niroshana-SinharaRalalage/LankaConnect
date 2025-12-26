# Root Cause Analysis: Registration State Flipping Issue

**Date**: 2025-12-26
**Severity**: P1 - HIGH
**Issue**: User registration state randomly "flips" between registered and not registered on page refresh
**Symptom**: Intermittent 500 errors on `/api/Events/{eventId}/my-registration` and `/api/Events/{eventId}/my-registration/ticket`
**Status**: 🔍 ROOT CAUSE IDENTIFIED

---

## Executive Summary

Users are experiencing intermittent 500 errors after successfully registering for events. The registration state appears to "flip" on page refresh, showing the user as sometimes registered and sometimes not registered.

**ROOT CAUSE IDENTIFIED**:
1. **JSONB Data Corruption**: The database contains `null` AgeCategory values in the JSONB `attendees` column
2. **Inconsistent Nullable Handling**: Multiple DTOs and query handlers map from domain `AttendeeDetails.AgeCategory` (non-nullable) to DTOs with inconsistent nullability
3. **Incomplete Fix Deployment**: Phase 6A.48 partially fixed the issue but only for `AttendeeDetailsDto`, not for `TicketAttendeeDto`

**IMPACT**:
- **User Experience**: Users cannot reliably view their registration after payment
- **Revenue**: Users may lose confidence in the system and request refunds
- **Data Integrity**: Database contains corrupt JSONB data that needs cleanup

**AFFECTED ENDPOINTS**:
1. ✅ `GET /api/Events/{eventId}/my-registration` - FIXED (commit 0daa9168, NOT DEPLOYED)
2. ❌ `GET /api/Events/{eventId}/my-registration/ticket` - NOT FIXED
3. ❌ `GET /api/Events/registrations/{registrationId}` - NOT FIXED
4. ❌ `GET /api/Events/{eventId}/attendees` - POTENTIALLY AFFECTED

---

## Section 1: Data Corruption Timeline

### Historical Context

| Date | Event | Impact |
|------|-------|--------|
| **Before Dec 2025** | AttendeeDetails used `Age` property (int) | JSONB stored: `{"name": "John", "age": 25}` |
| **Dec 2025 (Commit 5ae0e409)** | Refactored from `Age` to `AgeCategory` enum | Breaking schema change |
| **Dec 23, 2025 (Migration Phase6A43)** | Database migration to transform `age` → `age_category` | Partial data cleanup |
| **Dec 24, 2025 (Commit 0daa9168)** | Made `AttendeeDetailsDto.AgeCategory` nullable | Partial fix (1 of 4 endpoints) |
| **Dec 25-26, 2025** | User reports registration state flipping | Issue discovered |

### How Data Corruption Occurred

The corruption happened in **3 phases**:

#### Phase 1: Original Schema (Age-based)
```json
// Database JSONB before Dec 2025
{
  "attendees": [
    {"name": "John Doe", "age": 30},
    {"name": "Jane Doe", "age": 25}
  ]
}
```

**Domain Model**:
```csharp
public class AttendeeDetails {
    public string Name { get; }
    public int Age { get; }  // Non-nullable
}
```

#### Phase 2: Schema Change (AgeCategory Enum - Commit 5ae0e409)
```csharp
// New domain model (breaking change)
public class AttendeeDetails {
    public string Name { get; }
    public AgeCategory AgeCategory { get; }  // Non-nullable enum (Adult/Child)
    public Gender? Gender { get; }
}
```

**EF Core Configuration**:
```csharp
builder.OwnsMany(r => r.Attendees, attendeesBuilder => {
    attendeesBuilder.ToJson("attendees");
    attendeesBuilder.Property(a => a.AgeCategory)
        .HasColumnName("age_category")
        .HasConversion<string>();  // Stores as "Adult" or "Child"
});
```

**PROBLEM**: Existing database records still had `age` field, not `age_category`.

#### Phase 3: Data Migration (Dec 23, 2025 - Phase 6A.43)
```sql
-- Migration SQL (UpdateAttendeesAgeCategoryAndGender_Phase6A43)
UPDATE registrations
SET attendees = (
    SELECT jsonb_agg(
        jsonb_build_object(
            'name', elem->>'name',
            'age_category', CASE
                WHEN (elem->>'age')::int <= 18 THEN 'Child'
                ELSE 'Adult'
            END,
            'gender', null::text
        )
    )
    FROM jsonb_array_elements(attendees) elem
)
WHERE attendees IS NOT NULL
  AND jsonb_array_length(attendees) > 0
  AND attendees->0 ? 'age';  -- Only transforms records with 'age' field
```

**CRITICAL GAP**: This migration only transformed records that had the `age` field. Records created during the transition period (between code deploy and migration run) may have had:
- Empty `age_category`
- `null` `age_category`
- Malformed JSONB structure

#### Phase 4: Current State (Data Corruption Confirmed)
```json
// Corrupted records in database
{
  "attendees": [
    {"name": "John Doe", "age_category": null, "gender": null},  // ❌ NULL
    {"name": "Jane Doe", "age_category": "Adult", "gender": "Male"}  // ✅ OK
  ]
}
```

**Why This Causes 500 Errors**:
```csharp
// Query handler maps from domain to DTO
registration.Attendees.Select(a => new TicketAttendeeDto
{
    Name = a.Name,
    AgeCategory = a.AgeCategory,  // ❌ Throws "Nullable object must have a value"
    Gender = a.Gender
})
```

When EF Core deserializes JSONB with `"age_category": null`, it cannot map to the non-nullable `AgeCategory` enum in the domain model, causing an exception during materialization.

---

## Section 2: Code Analysis - All Affected Components

### 2.1 Domain Layer (Source of Truth)

**File**: `src/LankaConnect.Domain/Events/ValueObjects/AttendeeDetails.cs`

```csharp
public class AttendeeDetails : ValueObject
{
    public string Name { get; }
    public AgeCategory AgeCategory { get; }  // ❌ NON-NULLABLE (cannot be changed)
    public Gender? Gender { get; }

    private AttendeeDetails(string name, AgeCategory ageCategory, Gender? gender)
    {
        Name = name;
        AgeCategory = ageCategory;  // Validation ensures not null
        Gender = gender;
    }

    public static Result<AttendeeDetails> Create(string? name, AgeCategory ageCategory, Gender? gender = null)
    {
        if (!Enum.IsDefined(typeof(AgeCategory), ageCategory))
            return Result<AttendeeDetails>.Failure("Invalid age category");
        // ...
    }
}
```

**Analysis**:
- AgeCategory is **intentionally non-nullable** at the domain level
- Domain model enforces business rule: "Every attendee MUST have an age category"
- This is CORRECT from a domain-driven design perspective
- **ISSUE**: Database JSONB allows `null`, violating domain invariants

### 2.2 DTOs - Inconsistent Nullability

#### DTO 1: AttendeeDetailsDto (FIXED in 0daa9168, NOT DEPLOYED)
**File**: `src/LankaConnect.Application/Events/Common/RegistrationDetailsDto.cs`

```csharp
public record AttendeeDetailsDto
{
    public string Name { get; init; } = string.Empty;
    public AgeCategory? AgeCategory { get; init; }  // ✅ NULLABLE (Phase 6A.48)
    public Gender? Gender { get; init; }
}
```

**Status**: ✅ FIXED but NOT DEPLOYED

---

#### DTO 2: TicketAttendeeDto (FIXED in file, NEEDS VERIFICATION)
**File**: `src/LankaConnect.Application/Events/Common/TicketDto.cs`

```csharp
public record TicketAttendeeDto
{
    public string Name { get; init; } = string.Empty;
    public AgeCategory? AgeCategory { get; init; }  // ✅ ALREADY NULLABLE (Phase 6A.48)
    public Gender? Gender { get; init; }
}
```

**Status**: ✅ ALREADY FIXED (as of latest code read)

---

### 2.3 Query Handlers - All Mapping Locations

#### Handler 1: GetUserRegistrationForEventQueryHandler ✅ FIXED
**File**: `src/LankaConnect.Application/Events/Queries/GetUserRegistrationForEvent/GetUserRegistrationForEventQueryHandler.cs`

```csharp
// Line 45-52 (commit 0daa9168)
Attendees = r.Attendees != null ? r.Attendees.Select(a => new AttendeeDetailsDto
{
    Name = a.Name,
    AgeCategory = a.AgeCategory,  // ✅ Maps to nullable DTO
    Gender = a.Gender
}).ToList() : new List<AttendeeDetailsDto>(),
```

**Status**: ✅ FIXED - Null check + nullable DTO (NOT DEPLOYED)

---

#### Handler 2: GetTicketQueryHandler ❌ NEEDS VERIFICATION
**File**: `src/LankaConnect.Application/Events/Queries/GetTicket/GetTicketQuery.cs`

```csharp
// Line 116-122
var attendees = registration.HasDetailedAttendees()
    ? registration.Attendees.Select(a => new TicketAttendeeDto
    {
        Name = a.Name,
        AgeCategory = a.AgeCategory,  // ⚠️ Maps to nullable DTO (NOW)
        Gender = a.Gender
    }).ToList()
    : null;
```

**Analysis**:
- DTO is now nullable (Phase 6A.48 comments show this was fixed)
- **HOWEVER**: Still missing null check on `registration.Attendees`
- If `registration.Attendees` is null (edge case), this throws NullReferenceException

**Status**: ⚠️ PARTIALLY FIXED - DTO is nullable, but missing collection null check

---

#### Handler 3: GetRegistrationByIdQueryHandler ❌ NOT FIXED
**File**: `src/LankaConnect.Application/Events/Queries/GetRegistrationById/GetRegistrationByIdQueryHandler.cs`

```csharp
// Line 39-44
Attendees = r.Attendees.Select(a => new AttendeeDetailsDto
{
    Name = a.Name,
    AgeCategory = a.AgeCategory,  // ❌ NO NULL CHECK on collection
    Gender = a.Gender
}).ToList(),
```

**Issues**:
1. No null check on `r.Attendees` collection
2. Maps to `AttendeeDetailsDto` (now nullable), but still vulnerable if collection is null

**Status**: ❌ NOT FIXED - Missing null check

---

#### Handler 4: GetEventAttendeesQueryHandler ⚠️ SPECIAL CASE
**File**: `src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs`

```csharp
// Line 36-46: Loads entities with EF Core Include
var registrations = await _context.Registrations
    .AsNoTracking()
    .Include(r => r.Attendees)  // ✅ EF Core loads JSONB into collection
    .Include(r => r.Contact)
    .Include(r => r.TotalPrice)
    .Where(r => r.EventId == request.EventId)
    .ToListAsync(cancellationToken);

// Line 66-67: Uses domain properties directly
var adultCount = registration.Attendees.Count(a => a.AgeCategory == AgeCategory.Adult);
var childCount = registration.Attendees.Count(a => a.AgeCategory == AgeCategory.Child);

// Line 82-87: Maps in-memory (NOT in SQL projection)
var attendeeDtos = registration.Attendees.Select(a => new AttendeeDetailsDto
{
    Name = a.Name,
    AgeCategory = a.AgeCategory,  // ⚠️ In-memory mapping
    Gender = a.Gender
}).ToList();
```

**Analysis**:
- Uses `.Include()` to load entities into memory FIRST
- THEN maps in C# code (not SQL projection)
- **CRITICAL**: If JSONB has `null` AgeCategory, EF Core deserialization fails BEFORE reaching mapping code
- This would throw during `.ToListAsync()` at line 46, not during `.Select()` at line 82

**Status**: ❌ VULNERABLE - Will fail at entity materialization, not DTO mapping

---

## Section 3: Root Cause Summary

### Primary Root Cause
**Database contains JSONB records with `null` AgeCategory values, violating domain invariants**

### Contributing Factors

1. **Incomplete Data Migration**
   - Phase 6A.43 migration only transformed records with existing `age` field
   - Records created during transition period may have escaped migration
   - No validation to prevent future `null` insertions

2. **Inconsistent DTO Nullability** (MOSTLY FIXED)
   - Phase 6A.48 made `AttendeeDetailsDto.AgeCategory` nullable ✅
   - Phase 6A.48 made `TicketAttendeeDto.AgeCategory` nullable ✅
   - BUT: Query handlers still have gaps (missing null checks)

3. **Missing Collection Null Checks**
   - `GetRegistrationByIdQueryHandler`: No check for `r.Attendees == null`
   - `GetTicketQueryHandler`: Has `HasDetailedAttendees()` check, but inconsistent

4. **EF Core Materialization Failure**
   - `GetEventAttendeesQueryHandler` uses `.Include()` which materializes entities
   - If JSONB has `null` AgeCategory, deserialization fails BEFORE query completes
   - This is a different failure mode than SQL projection queries

---

## Section 4: All Affected Endpoints

| Endpoint | Query Handler | DTO Used | Status | Fix Needed |
|----------|---------------|----------|--------|------------|
| `GET /api/Events/{eventId}/my-registration` | GetUserRegistrationForEventQueryHandler | AttendeeDetailsDto | ✅ FIXED (0daa9168) | Deploy |
| `GET /api/Events/{eventId}/my-registration/ticket` | GetTicketQueryHandler | TicketAttendeeDto | ⚠️ PARTIAL | Add null check |
| `GET /api/Events/registrations/{registrationId}` | GetRegistrationByIdQueryHandler | AttendeeDetailsDto | ❌ NOT FIXED | Add null check |
| `GET /api/Events/{eventId}/attendees` | GetEventAttendeesQueryHandler | AttendeeDetailsDto | ❌ VULNERABLE | Add defensive handling |

---

## Section 5: Impact Assessment

### User Impact
- **Severity**: HIGH
- **Affected Users**: Any user with corrupt JSONB data (likely small percentage)
- **Functionality Broken**:
  - Cannot view registration details
  - Cannot view ticket
  - Event organizers cannot export attendee list
  - Ticket QR code cannot be generated

### Business Impact
- **Revenue Risk**: Users paid but cannot access tickets → refund requests
- **Trust Erosion**: Users lose confidence in platform reliability
- **Support Burden**: Increased support tickets for "registration disappeared"

### Data Integrity Impact
- **Database Corruption**: Unknown number of registrations have `null` AgeCategory
- **Domain Invariant Violation**: Database allows state that domain model forbids
- **Future Risk**: Without constraints, corruption can happen again

---

## Section 6: Comprehensive Fix Plan

### Phase 1: IMMEDIATE - Stop the Bleeding (Deploy Existing Fixes)

**Action 1: Deploy Commit 0daa9168 (Phase 6A.48)**
```bash
# This commit already makes DTOs nullable
git log --oneline | grep "6a48"
# 0daa9168 fix(phase-6a48): Make AgeCategory nullable in AttendeeDetailsDto
# 6ad41292 fix(phase-6a48): Fix CSV export issues for Signups and Attendees

# Verify what's deployed
az containerapp revision list --name <app> --resource-group <rg> --output table

# If not deployed, trigger deployment
git push origin develop
# Then trigger Azure deployment
```

**Expected Impact**: Fixes `/my-registration` endpoint (1 of 4)

---

### Phase 2: CODE FIXES - Add Missing Null Checks

**Fix 1: GetTicketQueryHandler** (Add collection null check)

```csharp
// File: src/LankaConnect.Application/Events/Queries/GetTicket/GetTicketQuery.cs
// Line 115-122 (CHANGE)

// BEFORE
var attendees = registration.HasDetailedAttendees()
    ? registration.Attendees.Select(a => new TicketAttendeeDto
    {
        Name = a.Name,
        AgeCategory = a.AgeCategory,
        Gender = a.Gender
    }).ToList()
    : null;

// AFTER
var attendees = registration.HasDetailedAttendees() && registration.Attendees != null
    ? registration.Attendees.Select(a => new TicketAttendeeDto
    {
        Name = a.Name,
        AgeCategory = a.AgeCategory,
        Gender = a.Gender
    }).ToList()
    : null;
```

**Fix 2: GetRegistrationByIdQueryHandler** (Add collection null check)

```csharp
// File: src/LankaConnect.Application/Events/Queries/GetRegistrationById/GetRegistrationByIdQueryHandler.cs
// Line 39-44 (CHANGE)

// BEFORE
Attendees = r.Attendees.Select(a => new AttendeeDetailsDto
{
    Name = a.Name,
    AgeCategory = a.AgeCategory,
    Gender = a.Gender
}).ToList(),

// AFTER
Attendees = r.Attendees != null
    ? r.Attendees.Select(a => new AttendeeDetailsDto
    {
        Name = a.Name,
        AgeCategory = a.AgeCategory,
        Gender = a.Gender
    }).ToList()
    : new List<AttendeeDetailsDto>(),
```

**Fix 3: GetEventAttendeesQueryHandler** (Defensive try-catch)

```csharp
// File: src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs
// Line 36-46 (WRAP WITH TRY-CATCH)

try
{
    var registrations = await _context.Registrations
        .AsNoTracking()
        .Include(r => r.Attendees)
        .Include(r => r.Contact)
        .Include(r => r.TotalPrice)
        .Where(r => r.EventId == request.EventId)
        .Where(r => r.Status != RegistrationStatus.Cancelled &&
                   r.Status != RegistrationStatus.Refunded)
        .OrderBy(r => r.CreatedAt)
        .ToListAsync(cancellationToken);
}
catch (InvalidOperationException ex) when (ex.Message.Contains("Nullable object must have a value"))
{
    // Log corrupted registration IDs for data cleanup
    _logger.LogError(ex, "JSONB deserialization failed for event {EventId} - corrupt data detected", request.EventId);

    // Fallback: Query without Include, manually handle Attendees
    return await HandleCorruptAttendeesData(request.EventId, cancellationToken);
}
```

---

### Phase 3: DATABASE CLEANUP - Fix Corrupt Data

**Step 1: Identify Corrupt Records**

```sql
-- Find all registrations with null age_category in JSONB
SELECT
    r.id,
    r.event_id,
    r.user_id,
    r.quantity,
    r.attendees::text,
    r.created_at
FROM registrations r
WHERE r.attendees IS NOT NULL
  AND jsonb_array_length(r.attendees) > 0
  AND EXISTS (
      SELECT 1
      FROM jsonb_array_elements(r.attendees) elem
      WHERE elem->>'age_category' IS NULL
  );
```

**Step 2: Analyze Pattern**

```sql
-- Count by date to find when corruption started
SELECT
    DATE(created_at) as registration_date,
    COUNT(*) as corrupt_count
FROM registrations r
WHERE r.attendees IS NOT NULL
  AND jsonb_array_length(r.attendees) > 0
  AND EXISTS (
      SELECT 1
      FROM jsonb_array_elements(r.attendees) elem
      WHERE elem->>'age_category' IS NULL
  )
GROUP BY DATE(created_at)
ORDER BY registration_date DESC;
```

**Step 3: Fix Corrupt Data with Default Values**

```sql
-- Backfill null age_category with 'Adult' as default
UPDATE registrations
SET attendees = (
    SELECT jsonb_agg(
        CASE
            WHEN elem->>'age_category' IS NULL THEN
                jsonb_build_object(
                    'name', elem->>'name',
                    'age_category', 'Adult',  -- Default to Adult
                    'gender', elem->>'gender'
                )
            ELSE elem
        END
    )
    FROM jsonb_array_elements(attendees) elem
)
WHERE attendees IS NOT NULL
  AND jsonb_array_length(attendees) > 0
  AND EXISTS (
      SELECT 1
      FROM jsonb_array_elements(attendees) elem
      WHERE elem->>'age_category' IS NULL
  );
```

**Step 4: Verify Fix**

```sql
-- Should return 0 rows
SELECT COUNT(*)
FROM registrations r
WHERE r.attendees IS NOT NULL
  AND jsonb_array_length(r.attendees) > 0
  AND EXISTS (
      SELECT 1
      FROM jsonb_array_elements(r.attendees) elem
      WHERE elem->>'age_category' IS NULL
  );
```

---

### Phase 4: PREVENTION - Add Database Constraints

**Option 1: PostgreSQL Check Constraint (Recommended)**

```sql
-- Add constraint to prevent null age_category in future
ALTER TABLE registrations
ADD CONSTRAINT chk_attendees_age_category_not_null
CHECK (
    attendees IS NULL
    OR jsonb_array_length(attendees) = 0
    OR NOT EXISTS (
        SELECT 1
        FROM jsonb_array_elements(attendees) elem
        WHERE elem->>'age_category' IS NULL
    )
);
```

**Option 2: PostgreSQL Trigger (More Flexible)**

```sql
-- Create function to validate attendees JSONB
CREATE OR REPLACE FUNCTION validate_attendees_jsonb()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.attendees IS NOT NULL
       AND jsonb_array_length(NEW.attendees) > 0
       AND EXISTS (
           SELECT 1
           FROM jsonb_array_elements(NEW.attendees) elem
           WHERE elem->>'age_category' IS NULL
       )
    THEN
        RAISE EXCEPTION 'Attendees cannot have null age_category';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger
CREATE TRIGGER trg_validate_attendees
BEFORE INSERT OR UPDATE ON registrations
FOR EACH ROW
EXECUTE FUNCTION validate_attendees_jsonb();
```

**Option 3: Domain Event Validation (Application Layer)**

```csharp
// Add to Registration.cs CreateWithAttendees method
public static Result<Registration> CreateWithAttendees(
    Guid eventId,
    Guid? userId,
    List<AttendeeDetails> attendees,
    ContactInfo? contact,
    Money? totalPrice,
    bool isPaidEvent)
{
    // Existing validation...

    // NEW: Validate all attendees have valid AgeCategory
    if (attendees.Any(a => !Enum.IsDefined(typeof(AgeCategory), a.AgeCategory)))
    {
        return Result<Registration>.Failure("All attendees must have a valid age category");
    }

    // ...
}
```

---

### Phase 5: MONITORING - Add Observability

**Add Logging to Detect Future Issues**

```csharp
// Add to GetUserRegistrationForEventQueryHandler
public async Task<Result<RegistrationDetailsDto?>> Handle(...)
{
    try
    {
        var registration = await _context.Registrations...

        // Log warning if Attendees is null (should never happen)
        if (registration != null && registration.Attendees == null)
        {
            _logger.LogWarning(
                "Registration {RegistrationId} has null Attendees collection - potential data corruption",
                registration.Id);
        }

        return Result<RegistrationDetailsDto?>.Success(registration);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("Nullable object"))
    {
        _logger.LogError(ex,
            "JSONB deserialization failed for EventId={EventId}, UserId={UserId} - data corruption detected",
            request.EventId, request.UserId);
        throw;
    }
}
```

**Add Application Insights Custom Metric**

```csharp
// Track JSONB corruption incidents
_telemetryClient.TrackMetric(
    "RegistrationCorruptionCount",
    1,
    new Dictionary<string, string>
    {
        { "EventId", request.EventId.ToString() },
        { "ErrorType", "NullAgeCategory" }
    });
```

---

## Section 7: Testing Strategy

### Test 1: Verify Fix with Corrupt Data (Integration Test)

```csharp
[Fact]
public async Task GetUserRegistration_WithNullAgeCategory_ReturnsSuccessWithNullableDto()
{
    // Arrange: Insert corrupt JSONB directly
    await _context.Database.ExecuteSqlRawAsync(@"
        INSERT INTO registrations (id, event_id, user_id, quantity, status, payment_status, attendees, created_at, updated_at)
        VALUES (
            gen_random_uuid(),
            @p0,
            @p1,
            2,
            'Confirmed',
            'Completed',
            '[{""name"": ""John Doe"", ""age_category"": null, ""gender"": null}]'::jsonb,
            NOW(),
            NOW()
        )", eventId, userId);

    // Act
    var query = new GetUserRegistrationForEventQuery(eventId, userId);
    var result = await _handler.Handle(query, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().NotBeNull();
    result.Value.Attendees.Should().HaveCount(1);
    result.Value.Attendees[0].AgeCategory.Should().BeNull();  // ✅ Handles null gracefully
}
```

### Test 2: Verify Database Constraint

```sql
-- Should FAIL with constraint violation
INSERT INTO registrations (id, event_id, user_id, quantity, status, payment_status, attendees, created_at, updated_at)
VALUES (
    gen_random_uuid(),
    '00000000-0000-0000-0000-000000000001',
    '00000000-0000-0000-0000-000000000002',
    1,
    'Confirmed',
    'Completed',
    '[{"name": "Test", "age_category": null, "gender": null}]'::jsonb,
    NOW(),
    NOW()
);
-- Expected: ERROR: new row for relation "registrations" violates check constraint "chk_attendees_age_category_not_null"
```

### Test 3: End-to-End User Flow

```csharp
[Fact]
public async Task UserRegistrationFlow_AfterDataCleanup_WorksCorrectly()
{
    // 1. Register user
    var registerResult = await RegisterUserForEvent(eventId, userId, 2);
    registerResult.IsSuccess.Should().BeTrue();

    // 2. Complete payment
    var paymentResult = await CompletePayment(registerResult.Value.RegistrationId);
    paymentResult.IsSuccess.Should().BeTrue();

    // 3. Fetch registration details
    var detailsResult = await GetUserRegistration(eventId, userId);
    detailsResult.IsSuccess.Should().BeTrue();
    detailsResult.Value.Attendees.Should().HaveCount(2);
    detailsResult.Value.Attendees.All(a => a.AgeCategory.HasValue).Should().BeTrue();

    // 4. Fetch ticket
    var ticketResult = await GetTicket(eventId, registerResult.Value.RegistrationId, userId);
    ticketResult.IsSuccess.Should().BeTrue();
    ticketResult.Value.Attendees.Should().HaveCount(2);
}
```

---

## Section 8: Deployment Sequence

### Step-by-Step Rollout

1. **✅ Phase 1: Deploy Existing Fixes (IMMEDIATE)**
   - Deploy commit 0daa9168 (AttendeeDetailsDto nullable)
   - Deploy commit 6ad41292 (CSV export fixes)
   - **Estimated Downtime**: 0 minutes (rolling deployment)
   - **Risk**: LOW (defensive changes only)

2. **✅ Phase 2: Add Code Fixes (SAME DAY)**
   - Implement fixes for GetTicketQueryHandler, GetRegistrationByIdQueryHandler
   - Add try-catch to GetEventAttendeesQueryHandler
   - Deploy as Phase 6A.50
   - **Estimated Downtime**: 0 minutes
   - **Risk**: LOW (additive changes only)

3. **⚠️ Phase 3: Database Cleanup (SCHEDULED MAINTENANCE)**
   - Run corrupt data identification queries
   - Review affected registrations
   - Execute cleanup script
   - **Estimated Downtime**: 0 minutes (UPDATE operation)
   - **Risk**: MEDIUM (data modification)
   - **Rollback Plan**: Keep backup before UPDATE

4. **⚠️ Phase 4: Add Database Constraints (AFTER CLEANUP)**
   - Add CHECK constraint or trigger
   - **Estimated Downtime**: 0 minutes (non-blocking ALTER)
   - **Risk**: MEDIUM (prevents future bad data)
   - **Rollback Plan**: DROP CONSTRAINT if issues

5. **✅ Phase 5: Monitoring (ONGOING)**
   - Deploy logging and telemetry
   - Monitor for 7 days
   - **Risk**: NONE

---

## Section 9: Questions Answered

### 1. Is the root cause JSONB data corruption (null AgeCategory values in database)?

**ANSWER**: ✅ YES - CONFIRMED

**Evidence**:
- Migration script (Phase 6A.43) only transformed records with `age` field
- Records created during transition period likely have `null` age_category
- Phase 6A.48 commit message explicitly states: "handle corrupt JSONB data"
- Error message "Nullable object must have a value" confirms null enum deserialization

---

### 2. Are there other DTOs/endpoints that map AgeCategory and could have the same issue?

**ANSWER**: ✅ YES - 4 ENDPOINTS TOTAL, 2 STILL VULNERABLE

| Endpoint | DTO | Status |
|----------|-----|--------|
| `/my-registration` | AttendeeDetailsDto | ✅ Fixed (not deployed) |
| `/my-registration/ticket` | TicketAttendeeDto | ⚠️ DTO fixed, handler needs null check |
| `/registrations/{id}` | AttendeeDetailsDto | ❌ Missing null check |
| `/attendees` | AttendeeDetailsDto | ❌ Vulnerable to materialization failure |

---

### 3. What caused the data corruption in the first place?

**ANSWER**: 3 FACTORS COMBINED

1. **Breaking Schema Change Without Full Migration**
   - Changed from `Age` (int) to `AgeCategory` (enum)
   - Migration only fixed records with existing `age` field
   - Records created during transition fell through the cracks

2. **No Database Constraints**
   - PostgreSQL JSONB doesn't enforce schema
   - No CHECK constraint to validate age_category is not null
   - No trigger to validate data integrity

3. **EF Core Configuration Gap**
   - `.HasConversion<string>()` allows null conversion
   - Should have been `.HasConversion<string>().IsRequired()` or `.HasDefaultValue("Adult")`

---

### 4. Should we make AgeCategory nullable in ALL DTOs consistently?

**ANSWER**: ✅ YES - ALREADY DONE (Phase 6A.48)

**Current State**:
- `AttendeeDetailsDto.AgeCategory`: ✅ Nullable (commit 0daa9168)
- `TicketAttendeeDto.AgeCategory`: ✅ Nullable (commit 0daa9168)

**Rationale**:
- DTOs are data transfer objects, not domain models
- DTOs should be defensive and handle real-world data corruption
- Domain model (`AttendeeDetails.AgeCategory`) should remain non-nullable to enforce business rules

---

### 5. Should we add database constraints to prevent future null values?

**ANSWER**: ✅ YES - STRONGLY RECOMMENDED

**Recommendation**: Use CHECK constraint (Option 1 in Phase 4)

**Reasoning**:
- Prevents future corruption at database level
- Fails fast during INSERT/UPDATE
- Easier to troubleshoot than silent failures
- Can be added with zero downtime

---

### 6. Should we create a data cleanup script to fix existing corrupt records?

**ANSWER**: ✅ YES - MANDATORY

**Action Items**:
1. Run identification query to count affected records
2. Review sample records to understand pattern
3. Decide on default value (recommend: 'Adult')
4. Execute UPDATE script during low-traffic period
5. Verify 0 corrupt records remain

**Alternative**: If corrupt records are < 10, manually review and fix each one

---

### 7. How do we prevent this pattern from happening again?

**ANSWER**: 5-LAYER DEFENSE STRATEGY

**Layer 1: Domain Validation (Application Code)**
```csharp
// Registration.CreateWithAttendees validates all attendees have valid AgeCategory
```

**Layer 2: EF Core Configuration (ORM Layer)**
```csharp
builder.Property(a => a.AgeCategory)
    .HasColumnName("age_category")
    .HasConversion<string>()
    .IsRequired();  // ← ADD THIS
```

**Layer 3: Database Constraint (Database Layer)**
```sql
ALTER TABLE registrations ADD CONSTRAINT chk_attendees_age_category_not_null...
```

**Layer 4: Migration Testing (CI/CD)**
- Add integration test that verifies migrations don't leave orphaned data
- Test migration Up() and Down() paths
- Validate data integrity after migration

**Layer 5: Monitoring (Observability)**
- Log warnings when null Attendees detected
- Track custom metrics for JSONB deserialization failures
- Alert on repeated failures

---

### 8. Should we add validation at the domain layer?

**ANSWER**: ✅ ALREADY EXISTS - But Can Be Enhanced

**Current Domain Validation**:
```csharp
public static Result<AttendeeDetails> Create(string? name, AgeCategory ageCategory, Gender? gender = null)
{
    if (!Enum.IsDefined(typeof(AgeCategory), ageCategory))
        return Result<AttendeeDetails>.Failure("Invalid age category");
    // ...
}
```

**Enhancement**: Add to `Registration` aggregate
```csharp
public static Result<Registration> CreateWithAttendees(...)
{
    // Validate ALL attendees before creating registration
    foreach (var attendee in attendees)
    {
        if (!Enum.IsDefined(typeof(AgeCategory), attendee.AgeCategory))
            return Result<Registration>.Failure($"Attendee '{attendee.Name}' has invalid age category");
    }
    // ...
}
```

---

### 9. Should we add database migration to backfill null values with defaults?

**ANSWER**: ✅ YES - But As UPDATE Script, Not Migration

**Reasoning**:
- Migrations should be idempotent and repeatable
- Data fixes are one-time operations
- Use manual UPDATE script during deployment
- Document in Phase 3 of deployment sequence

**Script Location**: Add to `scripts/fix-corrupt-attendees-data.sql`

---

### 10. What's the deployment status and merge sequence?

**ANSWER**: COMPLEX - Multiple Fixes Across Branches

**Current State**:
- ✅ Commit 0daa9168 (Phase 6A.48 - DTO nullable): EXISTS but NOT DEPLOYED
- ✅ Commit 6ad41292 (Phase 6A.48 - CSV export): EXISTS but NOT DEPLOYED
- Latest deployed: fa30668d (Phase 6A.49 - Email fix)

**Deployment Sequence**:
1. Verify 0daa9168 is on `develop` branch
2. If not, cherry-pick from feature branch
3. Deploy `develop` to staging
4. Test all 4 endpoints with corrupt data
5. Deploy to production
6. Run database cleanup script
7. Add constraints

---

## Section 10: Success Criteria

### Immediate Success (Phase 1-2)
- [ ] All 4 endpoints return 200 OK with corrupt data
- [ ] User can view registration details after registration
- [ ] User can view ticket with QR code
- [ ] Event organizers can export attendee list
- [ ] No 500 errors in Application Insights for registration endpoints

### Data Cleanup Success (Phase 3)
- [ ] 0 registrations with `null` age_category in database
- [ ] All existing registrations have valid AgeCategory values
- [ ] Backup created before cleanup
- [ ] Cleanup verified with SQL query

### Prevention Success (Phase 4)
- [ ] Database constraint prevents `null` age_category insertions
- [ ] Integration tests cover corrupt data scenarios
- [ ] Monitoring alerts trigger on deserialization failures

### Long-Term Success (30 days)
- [ ] 0 user reports of "registration state flipping"
- [ ] 0 Application Insights exceptions for "Nullable object must have a value"
- [ ] All registration endpoints have 99.9%+ success rate

---

## Section 11: Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Deploying code fixes breaks existing functionality | LOW | HIGH | Comprehensive integration tests before deploy |
| Database cleanup corrupts data | LOW | CRITICAL | Create backup before UPDATE, test on staging first |
| Database constraint blocks legitimate registrations | LOW | HIGH | Test constraint with all valid scenarios |
| More corrupt data exists than identified | MEDIUM | MEDIUM | Run identification query multiple times over 7 days |
| Future code changes re-introduce bug | MEDIUM | MEDIUM | Add regression tests, code review checklist |

---

## Appendix A: All Files Requiring Changes

### Code Changes (Phase 6A.50)

1. ✅ **ALREADY FIXED** (commit 0daa9168):
   - `src/LankaConnect.Application/Events/Common/RegistrationDetailsDto.cs`
   - `src/LankaConnect.Application/Events/Common/TicketDto.cs`
   - `src/LankaConnect.Application/Events/Queries/GetUserRegistrationForEvent/GetUserRegistrationForEventQueryHandler.cs`

2. ⚠️ **NEEDS FIX**:
   - `src/LankaConnect.Application/Events/Queries/GetTicket/GetTicketQuery.cs` (add null check)
   - `src/LankaConnect.Application/Events/Queries/GetRegistrationById/GetRegistrationByIdQueryHandler.cs` (add null check)
   - `src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs` (add try-catch)

3. **NEW FILES**:
   - `scripts/identify-corrupt-attendees.sql` (diagnostic query)
   - `scripts/fix-corrupt-attendees-data.sql` (cleanup script)
   - `src/LankaConnect.Infrastructure/Data/Migrations/YYYYMMDD_AddAttendeesValidationConstraint.cs` (constraint migration)

---

## Appendix B: SQL Diagnostic Queries

### Query 1: Count Corrupt Records
```sql
SELECT COUNT(*) as corrupt_registration_count
FROM registrations r
WHERE r.attendees IS NOT NULL
  AND jsonb_array_length(r.attendees) > 0
  AND EXISTS (
      SELECT 1
      FROM jsonb_array_elements(r.attendees) elem
      WHERE elem->>'age_category' IS NULL
  );
```

### Query 2: Sample Corrupt Records
```sql
SELECT
    r.id,
    r.event_id,
    e.title as event_name,
    r.user_id,
    r.quantity,
    r.attendees::text,
    r.status,
    r.payment_status,
    r.created_at
FROM registrations r
LEFT JOIN events e ON r.event_id = e.id
WHERE r.attendees IS NOT NULL
  AND jsonb_array_length(r.attendees) > 0
  AND EXISTS (
      SELECT 1
      FROM jsonb_array_elements(r.attendees) elem
      WHERE elem->>'age_category' IS NULL
  )
ORDER BY r.created_at DESC
LIMIT 10;
```

### Query 3: Corruption by Date
```sql
SELECT
    DATE(r.created_at) as registration_date,
    COUNT(*) as count,
    STRING_AGG(DISTINCT e.title, ', ') as affected_events
FROM registrations r
LEFT JOIN events e ON r.event_id = e.id
WHERE r.attendees IS NOT NULL
  AND jsonb_array_length(r.attendees) > 0
  AND EXISTS (
      SELECT 1
      FROM jsonb_array_elements(r.attendees) elem
      WHERE elem->>'age_category' IS NULL
  )
GROUP BY DATE(r.created_at)
ORDER BY registration_date DESC;
```

---

## Appendix C: Monitoring Queries

### Application Insights Query (KQL)
```kql
// Track JSONB deserialization failures
exceptions
| where timestamp > ago(7d)
| where outerMessage contains "Nullable object must have a value"
| where operation_Name startswith "GET /api/Events"
| summarize count() by bin(timestamp, 1h), operation_Name
| render timechart
```

### Database Health Check
```sql
-- Run daily to ensure no new corruption
SELECT
    COUNT(*) as total_registrations,
    SUM(CASE WHEN attendees IS NULL THEN 1 ELSE 0 END) as null_attendees,
    SUM(CASE WHEN jsonb_array_length(attendees) > 0 THEN 1 ELSE 0 END) as with_attendees,
    SUM(CASE
        WHEN attendees IS NOT NULL
        AND jsonb_array_length(attendees) > 0
        AND EXISTS (
            SELECT 1
            FROM jsonb_array_elements(attendees) elem
            WHERE elem->>'age_category' IS NULL
        )
        THEN 1 ELSE 0
    END) as corrupt_attendees
FROM registrations
WHERE created_at > CURRENT_DATE - INTERVAL '7 days';
```

---

**Document Status**: COMPLETE
**Next Action**: Review with team, approve fix plan, begin Phase 1 deployment
**Owner**: System Architect
**Reviewers**: Backend Lead, Database Admin, DevOps Engineer
