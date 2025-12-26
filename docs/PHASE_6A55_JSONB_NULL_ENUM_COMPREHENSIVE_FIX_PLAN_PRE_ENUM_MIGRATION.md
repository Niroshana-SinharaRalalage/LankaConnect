# Phase 6A.50: Comprehensive JSONB Nullable Enum Fix - Implementation Plan

**Date**: 2025-12-26
**Priority**: P0 - CRITICAL
**Issue**: `.Include(r => r.Attendees)` materializes JSONB data with null `AgeCategory` into non-nullable domain enums, causing HTTP 500 errors across multiple handlers
**Status**: 📋 PLANNING COMPLETE - READY FOR IMPLEMENTATION

---

## Executive Summary

This document provides a comprehensive, systematic plan to fix ALL instances of the JSONB nullable enum materialization bug across the LankaConnect codebase. The bug occurs when Entity Framework materializes JSONB `Attendees` data containing null `AgeCategory` values into domain objects with non-nullable `AgeCategory` enum properties, causing runtime exceptions.

**Scope**: 2 handlers confirmed broken, 3 handlers delegating to broken handlers (indirect impact), 2 handlers using correct pattern already.

**Estimated Time**: 6-8 hours total (incremental deployment over 3-4 phases)

**Risk Level**: MEDIUM (changes isolated to query handlers, no database migrations, easy rollback)

---

## Section 1: Comprehensive Audit Results

### 1.1 Handler Classification Matrix

| Handler | Risk Level | Current Pattern | Issue Type | Priority | API Endpoint |
|---------|------------|-----------------|------------|----------|--------------|
| **GetEventAttendeesQueryHandler** | 🔴 CRITICAL | `.Include(r => r.Attendees)` | Direct materialization | P0 | `GET /api/events/{id}/attendees` |
| **GetRegistrationByIdQueryHandler** | 🔴 CRITICAL | `.Include()` via projection | Direct LINQ projection with AgeCategory | P0 | `GET /api/events/registrations/{id}` |
| **ExportEventAttendeesQueryHandler** | 🟡 HIGH | Delegates to GetEventAttendeesQueryHandler | Indirect failure | P1 | `GET /api/events/{id}/export` |
| **GetEventsByOrganizerQueryHandler** | 🟢 LOW | Delegates to GetEventsQuery | No Attendees access | P3 | `GET /api/events/by-organizer/{id}` |
| **GetMyRegisteredEventsQueryHandler** | 🟢 LOW | Delegates to GetEventsQuery | No Attendees access | P3 | `GET /api/my-registered-events` |
| **GetEventsQueryHandler** | 🟢 SAFE | No Attendees access | Returns EventDto only | N/A | `GET /api/events` |
| **GetUserRegistrationForEventQueryHandler** | ✅ FIXED | Direct LINQ projection | Phase 6A.48 fix applied | N/A | `GET /api/events/{id}/my-registration` |
| **CheckEventRegistrationQueryHandler** | ✅ SAFE | No Attendees access | Simple exists check | N/A | `POST /api/events/{id}/check-registration` |
| **GetEventRegistrationByEmailQueryHandler** | ✅ SAFE | No Attendees access | Simple exists check | N/A | Internal |

### 1.2 Detailed Handler Analysis

#### 🔴 Handler 1: GetEventAttendeesQueryHandler (CRITICAL)

**File**: `src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs`

**Current Implementation** (Lines 37-46):
```csharp
var registrations = await _context.Registrations
    .AsNoTracking()
    .Include(r => r.Attendees)  // ❌ MATERIALIZES JSONB TO DOMAIN OBJECT
    .Include(r => r.Contact)
    .Include(r => r.TotalPrice)
    .Where(r => r.EventId == request.EventId)
    .Where(r => r.Status != RegistrationStatus.Cancelled &&
               r.Status != RegistrationStatus.Refunded)
    .OrderBy(r => r.CreatedAt)
    .ToListAsync(cancellationToken);
```

**Bug Location** (Lines 66-67):
```csharp
var adultCount = registration.Attendees.Count(a => a.AgeCategory == AgeCategory.Adult);
var childCount = registration.Attendees.Count(a => a.AgeCategory == AgeCategory.Child);
```
**Problem**: When JSONB has `{"ageCategory": null}`, EF cannot materialize into non-nullable `AgeCategory` enum, causing exception.

**Affected Users**: Event organizers viewing attendee lists

**API Endpoint**: `GET /api/events/{eventId}/attendees`

**Failure Scenario**:
1. User registers with corrupted JSONB data (null AgeCategory)
2. Organizer clicks "Attendees" tab
3. Handler tries to materialize JSONB → HTTP 500 error
4. Organizer cannot view attendee list

---

#### 🔴 Handler 2: GetRegistrationByIdQueryHandler (CRITICAL)

**File**: `src/LankaConnect.Application/Events/Queries/GetRegistrationById/GetRegistrationByIdQueryHandler.cs`

**Current Implementation** (Lines 26-56):
```csharp
var registration = await _context.Registrations
    .Where(r => r.Id == request.RegistrationId)
    .Select(r => new RegistrationDetailsDto
    {
        Id = r.Id,
        EventId = r.EventId,
        UserId = r.UserId,
        Quantity = r.Quantity,
        Status = r.Status,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,

        // Map attendees
        Attendees = r.Attendees.Select(a => new AttendeeDetailsDto
        {
            Name = a.Name,
            AgeCategory = a.AgeCategory,  // ❌ Non-nullable assignment from JSONB
            Gender = a.Gender
        }).ToList(),
        // ... rest of mapping
    })
    .FirstOrDefaultAsync(cancellationToken);
```

**Problem**: Direct LINQ projection assumes `AgeCategory` is always non-null in JSONB data.

**Affected Users**: Anyone viewing registration details (anonymous users after payment, authenticated users)

**API Endpoint**: `GET /api/events/registrations/{registrationId}`

**Failure Scenario**:
1. Anonymous user completes paid registration
2. Payment success page tries to fetch registration details
3. JSONB has null AgeCategory → projection fails → HTTP 500
4. User sees error page instead of confirmation

**Note**: This is the **SAME pattern** as Phase 6A.48 fix but in a different handler.

---

#### 🟡 Handler 3: ExportEventAttendeesQueryHandler (HIGH - Indirect)

**File**: `src/LankaConnect.Application/Events/Queries/ExportEventAttendees/ExportEventAttendeesQueryHandler.cs`

**Current Implementation** (Lines 36-38):
```csharp
var attendeesQuery = new GetEventAttendeesQuery(request.EventId);
var attendeesHandler = new GetEventAttendeesQueryHandler(_context, _eventRepository);
var attendeesResult = await attendeesHandler.Handle(attendeesQuery, cancellationToken);
```

**Problem**: Delegates to `GetEventAttendeesQueryHandler` which is broken (Handler 1).

**Impact**: CSV/Excel export fails if any registration has corrupt JSONB data.

**API Endpoint**: `GET /api/events/{eventId}/export?format=csv|excel`

**Fix Strategy**: Fix Handler 1 → this handler automatically fixed.

---

#### 🟢 Handlers 4 & 5: GetEventsByOrganizerQueryHandler & GetMyRegisteredEventsQueryHandler (LOW)

**File**:
- `src/LankaConnect.Application/Events/Queries/GetEventsByOrganizer/GetEventsByOrganizerQueryHandler.cs`
- `src/LankaConnect.Application/Events/Queries/GetMyRegisteredEvents/GetMyRegisteredEventsQueryHandler.cs`

**Implementation Pattern**:
```csharp
// Both delegate to GetEventsQuery which returns EventDto (no Attendees access)
var getEventsQuery = new GetEventsQuery(...);
var eventsResult = await _mediator.Send(getEventsQuery, cancellationToken);
```

**Risk Assessment**: LOW - These handlers never access Attendees JSONB data.

**Recommendation**: No changes required, but monitor for future modifications.

---

### 1.3 Data Structure Analysis

**Domain Model** (Rich entity with non-nullable enum):
```csharp
// src/LankaConnect.Domain/Events/Attendee.cs
public class Attendee
{
    public string Name { get; private set; }
    public AgeCategory AgeCategory { get; private set; }  // Non-nullable
    public Gender? Gender { get; private set; }           // Nullable
}
```

**JSONB Storage** (PostgreSQL):
```json
{
  "name": "John Doe",
  "ageCategory": null,  // ⚠️ CORRUPT DATA
  "gender": "Male"
}
```

**DTO Model** (Fixed in Phase 6A.48):
```csharp
// src/LankaConnect.Application/Events/Common/RegistrationDetailsDto.cs
public record AttendeeDetailsDto
{
    public string Name { get; init; } = string.Empty;
    public AgeCategory? AgeCategory { get; init; }  // ✅ Nullable
    public Gender? Gender { get; init; }
}
```

**Mismatch**: Domain model is non-nullable, JSONB data has nulls, DTO is nullable.

---

## Section 2: Root Cause Analysis

### 2.1 Why This Bug Exists

**Historical Context**:
1. **Original Design**: `AgeCategory` was required field in domain model
2. **Data Evolution**: JSONB schema changed or data migrated incorrectly
3. **Legacy Data**: Old registrations may have null `AgeCategory` values
4. **EF Core Behavior**: Cannot materialize JSONB null into non-nullable C# enum

**Why Phase 6A.48 Only Fixed One Handler**:
- Phase 6A.48 focused on `GetUserRegistrationForEventQueryHandler` only
- RCA document identified the bug but didn't audit entire codebase
- User question "why not fix all handlers?" is absolutely valid

### 2.2 Why .Include() Fails

**Entity Framework Behavior**:
```csharp
// ❌ This FAILS when JSONB has null AgeCategory
.Include(r => r.Attendees)
.ToListAsync()
// EF tries to materialize JSONB → Domain object
// JSONB: {"ageCategory": null}
// Domain: public AgeCategory AgeCategory { get; set; } // Non-nullable
// Result: InvalidCastException or JsonException

// ✅ This SUCCEEDS with direct projection
.Select(r => new RegistrationDetailsDto
{
    Attendees = r.Attendees.Select(a => new AttendeeDetailsDto
    {
        AgeCategory = a.AgeCategory  // EF translates to nullable DTO
    }).ToList()
})
```

**Key Insight**: Direct LINQ projection with `Select()` allows EF to translate JSONB nulls to DTO nulls, bypassing domain model constraints.

---

## Section 3: Fix Strategy & Architecture Decision

### 3.1 The Correct Pattern (ADR)

**Decision**: Use direct LINQ projection with `AsNoTracking()` for all JSONB queries.

**Pattern Template**:
```csharp
// ❌ WRONG: Materialization with Include
var registrations = await _context.Registrations
    .AsNoTracking()
    .Include(r => r.Attendees)  // Don't do this
    .ToListAsync();

// ✅ CORRECT: Direct LINQ projection
var registrations = await _context.Registrations
    .AsNoTracking()
    .Where(/* filters */)
    .Select(r => new
    {
        // Flatten all needed data
        RegistrationId = r.Id,
        EventId = r.EventId,
        Status = r.Status,

        // JSONB fields - project to nullable DTOs
        Attendees = r.Attendees.Select(a => new AttendeeDetailsDto
        {
            Name = a.Name,
            AgeCategory = a.AgeCategory,  // Implicit nullable conversion
            Gender = a.Gender
        }).ToList(),

        ContactEmail = r.Contact != null ? r.Contact.Email : null,
        ContactPhone = r.Contact != null ? r.Contact.PhoneNumber : null,
        TotalAmount = r.TotalPrice != null ? r.TotalPrice.Amount : null
    })
    .ToListAsync();
```

**Rationale**:
1. **Safety**: Nullable DTO properties handle corrupt JSONB data gracefully
2. **Performance**: Single SQL query with JSON projection (no N+1 queries)
3. **AsNoTracking**: No change tracking overhead for read-only queries
4. **Testability**: Easy to test with in-memory data

**Trade-offs**:
- ❌ More verbose code (explicit mapping instead of Include)
- ✅ Explicit contract (clearer what data is needed)
- ✅ Defensive against corrupt data
- ✅ No runtime exceptions

### 3.2 Why Not Fix Domain Model?

**Option Considered**: Make `Attendee.AgeCategory` nullable in domain model.

**Rejected Because**:
1. **Domain Integrity**: Business rule states AgeCategory should be required
2. **Breaking Change**: Would affect all code using Attendee entity
3. **Aggregate Consistency**: Domain model should enforce invariants
4. **Better Solution**: Keep domain strict, use projections for queries

**Conclusion**: Domain model stays non-nullable, query handlers use projection pattern.

---

## Section 4: Phased Implementation Plan

### Phase 1: Fix Critical Handlers (2-3 hours)

**Handlers**: GetEventAttendeesQueryHandler, GetRegistrationByIdQueryHandler

**Deliverables**:
1. Update both handlers to use direct LINQ projection
2. Write unit tests with null AgeCategory data
3. Build backend (0 errors required)
4. Test API endpoints with PowerShell
5. Deploy to staging
6. Verify with production-like data

**Success Criteria**:
- ✅ Both handlers return 200 OK with corrupt data
- ✅ Null AgeCategory handled gracefully
- ✅ Adult/child counts accurate (excluding nulls)
- ✅ No breaking changes to API responses

---

### Phase 2: Verify Indirect Handlers (30 minutes)

**Handler**: ExportEventAttendeesQueryHandler

**Tasks**:
1. Test CSV export with corrupt data
2. Test Excel export with corrupt data
3. Verify multi-sheet export works

**Success Criteria**:
- ✅ Export succeeds with corrupt data
- ✅ CSV shows empty for null AgeCategory
- ✅ Excel formats correctly

---

### Phase 3: Add Defensive Logging & Monitoring (1 hour)

**Tasks**:
1. Add diagnostic logging for null AgeCategory detection
2. Add Application Insights metrics
3. Create alert for corrupt JSONB data

**Example**:
```csharp
var nullAgeCategoryCount = attendees.Count(a => a.AgeCategory == null);
if (nullAgeCategoryCount > 0)
{
    _logger.LogWarning(
        "Event {EventId} has {Count} attendees with null AgeCategory",
        eventId, nullAgeCategoryCount);

    _telemetry.TrackMetric("CorruptJSONB.NullAgeCategory", nullAgeCategoryCount);
}
```

---

### Phase 4: Create ADR & Prevention Measures (2 hours)

**Deliverables**:
1. Architecture Decision Record (ADR)
2. Code review checklist
3. Unit test template
4. Developer guidelines

---

## Section 5: Detailed Implementation - Handler by Handler

### 5.1 Fix GetEventAttendeesQueryHandler

**File**: `src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs`

**Current Code** (Lines 37-60):
```csharp
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

var attendeeDtos = registrations.Select(r => MapToDto(r)).ToList();
```

**Proposed Fix**:
```csharp
// Phase 6A.50: Use direct LINQ projection to handle null AgeCategory in JSONB
var attendeeDtos = await _context.Registrations
    .AsNoTracking()
    .Where(r => r.EventId == request.EventId)
    .Where(r => r.Status != RegistrationStatus.Cancelled &&
               r.Status != RegistrationStatus.Refunded)
    .OrderBy(r => r.CreatedAt)
    .Select(r => new EventAttendeeDto
    {
        RegistrationId = r.Id,
        UserId = r.UserId,
        Status = r.Status,
        PaymentStatus = r.PaymentStatus,
        CreatedAt = r.CreatedAt,

        // Contact info (nullable)
        ContactEmail = r.Contact != null ? r.Contact.Email : string.Empty,
        ContactPhone = r.Contact != null ? r.Contact.PhoneNumber : string.Empty,
        ContactAddress = r.Contact != null ? r.Contact.Address : null,

        // Attendee details (JSONB projection)
        Attendees = r.Attendees.Select(a => new AttendeeDetailsDto
        {
            Name = a.Name,
            AgeCategory = a.AgeCategory,  // Nullable DTO handles null JSONB
            Gender = a.Gender
        }).ToList(),

        TotalAttendees = r.Attendees.Count(),

        // Count only non-null AgeCategory values
        AdultCount = r.Attendees.Count(a => a.AgeCategory == AgeCategory.Adult),
        ChildCount = r.Attendees.Count(a => a.AgeCategory == AgeCategory.Child),

        // Gender distribution (full names for Excel compatibility)
        GenderDistribution = string.Join(", ",
            r.Attendees
                .Where(a => a.Gender.HasValue)
                .GroupBy(a => a.Gender!.Value)
                .Select(g => g.Count() + " " + g.Key.ToString())),

        // Payment info (nullable)
        TotalAmount = r.TotalPrice != null ? r.TotalPrice.Amount : null,
        Currency = r.TotalPrice != null ? r.TotalPrice.Currency.ToString() : null,

        // Ticket info (placeholder until ticket integration)
        TicketCode = null,
        QrCodeData = null,
        HasTicket = false
    })
    .ToListAsync(cancellationToken);

return Result<EventAttendeesResponse>.Success(new EventAttendeesResponse
{
    EventId = request.EventId,
    EventTitle = @event.Title.Value,
    Attendees = attendeeDtos,
    TotalRegistrations = attendeeDtos.Count,
    TotalAttendees = attendeeDtos.Sum(a => a.TotalAttendees),
    TotalRevenue = attendeeDtos.Sum(a => a.TotalAmount ?? 0)
});
```

**Changes Summary**:
1. ✅ Removed `.Include()` calls (no materialization)
2. ✅ Replaced in-memory `MapToDto()` with direct LINQ projection
3. ✅ All JSONB fields projected through nullable DTOs
4. ✅ `AdultCount`/`ChildCount` count only non-null values
5. ✅ Single database query (performance maintained)

**Method Removal**:
Delete `MapToDto()` method (lines 63-115) - no longer needed.

Delete `GetGenderShortCode()` method (lines 117-126) - no longer needed.

---

### 5.2 Fix GetRegistrationByIdQueryHandler

**File**: `src/LankaConnect.Application/Events/Queries/GetRegistrationById/GetRegistrationByIdQueryHandler.cs`

**Current Code** (Lines 26-56):
```csharp
var registration = await _context.Registrations
    .Where(r => r.Id == request.RegistrationId)
    .Select(r => new RegistrationDetailsDto
    {
        Id = r.Id,
        EventId = r.EventId,
        UserId = r.UserId,
        Quantity = r.Quantity,
        Status = r.Status,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,

        // Map attendees
        Attendees = r.Attendees.Select(a => new AttendeeDetailsDto
        {
            Name = a.Name,
            AgeCategory = a.AgeCategory,  // ❌ ISSUE: Non-nullable assignment
            Gender = a.Gender
        }).ToList(),

        // Contact information
        ContactEmail = r.Contact != null ? r.Contact.Email : null,
        ContactPhone = r.Contact != null ? r.Contact.PhoneNumber : null,
        ContactAddress = r.Contact != null ? r.Contact.Address : null,

        // Payment information
        PaymentStatus = r.PaymentStatus,
        TotalPriceAmount = r.TotalPrice != null ? r.TotalPrice.Amount : null,
        TotalPriceCurrency = r.TotalPrice != null ? r.TotalPrice.Currency.ToString() : null
    })
    .FirstOrDefaultAsync(cancellationToken);
```

**Proposed Fix**:
```csharp
// Phase 6A.50: Added null check for Attendees JSONB list
var registration = await _context.Registrations
    .AsNoTracking()  // ✅ Added AsNoTracking
    .Where(r => r.Id == request.RegistrationId)
    .Select(r => new RegistrationDetailsDto
    {
        Id = r.Id,
        EventId = r.EventId,
        UserId = r.UserId,
        Quantity = r.Quantity,
        Status = r.Status,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,

        // Phase 6A.50: Handle null Attendees list + null AgeCategory
        Attendees = r.Attendees != null ? r.Attendees.Select(a => new AttendeeDetailsDto
        {
            Name = a.Name,
            AgeCategory = a.AgeCategory,  // ✅ FIXED: DTO is nullable, handles null JSONB
            Gender = a.Gender
        }).ToList() : new List<AttendeeDetailsDto>(),

        // Contact information
        ContactEmail = r.Contact != null ? r.Contact.Email : null,
        ContactPhone = r.Contact != null ? r.Contact.PhoneNumber : null,
        ContactAddress = r.Contact != null ? r.Contact.Address : null,

        // Payment information
        PaymentStatus = r.PaymentStatus,
        TotalPriceAmount = r.TotalPrice != null ? r.TotalPrice.Amount : null,
        TotalPriceCurrency = r.TotalPrice != null ? r.TotalPrice.Currency.ToString() : null
    })
    .FirstOrDefaultAsync(cancellationToken);
```

**Changes Summary**:
1. ✅ Added `AsNoTracking()` for consistency with Phase 6A.47 fix
2. ✅ Added null check for `r.Attendees` collection
3. ✅ `AgeCategory` assignment works because DTO is nullable (Phase 6A.48)

**Note**: This is minimal change - code already uses projection pattern correctly.

---

## Section 6: Testing Strategy

### 6.1 Unit Tests

**Test File**: `tests/LankaConnect.Application.Tests/Events/Queries/GetEventAttendeesQueryHandlerTests.cs`

**Test Cases Required**:

```csharp
public class GetEventAttendeesQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithNullAgeCategory_ReturnsSuccess()
    {
        // Arrange: Create registration with null AgeCategory in JSONB
        var eventId = Guid.NewGuid();
        var registration = CreateRegistrationWithCorruptJSONB(eventId);

        // Act
        var result = await _handler.Handle(new GetEventAttendeesQuery(eventId));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.Attendees.Count);
        Assert.Null(result.Value.Attendees[0].Attendees[0].AgeCategory);
    }

    [Fact]
    public async Task Handle_WithMixedValidAndNullAgeCategory_CountsOnlyValid()
    {
        // Arrange: 2 adults, 1 child, 1 null
        var eventId = Guid.NewGuid();
        var registration = CreateMixedAttendees(eventId);

        // Act
        var result = await _handler.Handle(new GetEventAttendeesQuery(eventId));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Attendees[0].AdultCount);
        Assert.Equal(1, result.Value.Attendees[0].ChildCount);
        Assert.Equal(4, result.Value.Attendees[0].TotalAttendees);
    }

    [Theory]
    [InlineData(null)]  // Null AgeCategory
    [InlineData(0)]     // Invalid enum value
    public async Task Handle_WithInvalidAgeCategory_DoesNotThrow(int? ageCategory)
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var registration = CreateRegistrationWithAgeCategory(eventId, ageCategory);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() =>
            _handler.Handle(new GetEventAttendeesQuery(eventId)));

        Assert.Null(exception);
    }
}
```

**Test Data Setup**:
```csharp
private Registration CreateRegistrationWithCorruptJSONB(Guid eventId)
{
    // Use Newtonsoft.Json to create JSONB with null AgeCategory
    var jsonbData = JsonConvert.SerializeObject(new[]
    {
        new { name = "John Doe", ageCategory = (int?)null, gender = "Male" }
    });

    // Insert into test database with raw JSONB
    return CreateRegistrationWithRawJSONB(eventId, jsonbData);
}
```

---

### 6.2 Integration Tests

**Test File**: `tests/LankaConnect.API.Tests/Controllers/EventsControllerTests.cs`

**Test Case**:
```csharp
[Fact]
public async Task GetEventAttendees_WithCorruptJSONB_Returns200()
{
    // Arrange
    var eventId = await CreateEventWithCorruptAttendeeData();

    // Act
    var response = await _client.GetAsync($"/api/events/{eventId}/attendees");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var content = await response.Content.ReadAsStringAsync();
    var result = JsonConvert.DeserializeObject<EventAttendeesResponse>(content);

    Assert.NotNull(result);
    Assert.True(result.Attendees.Count > 0);
}
```

---

### 6.3 API Testing (PowerShell Scripts)

**Script**: `scripts/test-event-attendees-api.ps1`

```powershell
# Phase 6A.50: Test event attendees API with corrupt JSONB data

$baseUrl = "https://lankaconnect-staging-api.azurewebsites.net"
$eventId = "9e3722f5-c255-4dcc-b167-afef56bc5592"  # Event known to have corrupt data
$token = "Bearer ..."  # Get from login

# Test 1: Get Attendees
Write-Host "Testing GET /api/events/$eventId/attendees"
$response = Invoke-RestMethod `
    -Uri "$baseUrl/api/events/$eventId/attendees" `
    -Method Get `
    -Headers @{ Authorization = $token }

Write-Host "Status: $($response.StatusCode)"
Write-Host "Total Attendees: $($response.totalAttendees)"
Write-Host "Attendees with null AgeCategory: $(($response.attendees | Where-Object { $_.attendees | Where-Object { $_.ageCategory -eq $null } }).Count)"

# Test 2: Export CSV
Write-Host "`nTesting GET /api/events/$eventId/export?format=csv"
$csvResponse = Invoke-RestMethod `
    -Uri "$baseUrl/api/events/$eventId/export?format=csv" `
    -Method Get `
    -Headers @{ Authorization = $token } `
    -OutFile "test-export.csv"

Write-Host "CSV exported successfully: test-export.csv"

# Test 3: Get Registration by ID
$registrationId = $response.attendees[0].registrationId
Write-Host "`nTesting GET /api/events/registrations/$registrationId"
$regResponse = Invoke-RestMethod `
    -Uri "$baseUrl/api/events/registrations/$registrationId" `
    -Method Get

Write-Host "Status: OK"
Write-Host "Attendees: $(($regResponse.attendees | ConvertTo-Json -Depth 3))"
```

---

## Section 7: Deployment Strategy

### 7.1 Incremental Rollout

**Phase 1 Deployment** (GetEventAttendeesQueryHandler + GetRegistrationByIdQueryHandler):
1. Create feature branch: `fix/phase-6a50-jsonb-null-enum-part1`
2. Implement both handlers
3. Write unit tests
4. Build backend (0 errors)
5. Run local tests
6. Commit with message: `fix(phase-6a50): Fix JSONB null AgeCategory in GetEventAttendees and GetRegistrationById handlers`
7. Deploy to staging
8. Run PowerShell API tests
9. Monitor Application Insights for 1 hour
10. Deploy to production if successful

**Phase 2 Deployment** (Verification + Monitoring):
1. Test ExportEventAttendees in staging
2. Add diagnostic logging
3. Deploy to production
4. Monitor for 24 hours

---

### 7.2 Rollback Plan

**If Issues Detected**:
1. Revert commit: `git revert <commit-hash>`
2. Redeploy previous version
3. No database changes = instant rollback

**Rollback Criteria**:
- ❌ HTTP 500 errors increased
- ❌ API response time degraded >20%
- ❌ Breaking changes to API contracts
- ❌ Data accuracy issues in exports

---

## Section 8: Success Criteria

### 8.1 Handler-Specific Criteria

**GetEventAttendeesQueryHandler**:
- ✅ Returns 200 OK with corrupt JSONB data
- ✅ Adult/child counts exclude null AgeCategory
- ✅ No breaking changes to `EventAttendeesResponse` schema
- ✅ Performance: <500ms for 100 attendees

**GetRegistrationByIdQueryHandler**:
- ✅ Returns 200 OK with null AgeCategory
- ✅ Attendees array includes entries with null AgeCategory
- ✅ Payment success page renders correctly

**ExportEventAttendeesQueryHandler**:
- ✅ CSV export succeeds with corrupt data
- ✅ Excel export succeeds with corrupt data
- ✅ Null AgeCategory shown as empty cell

---

### 8.2 Overall Success Criteria

**Technical**:
- ✅ Zero build errors
- ✅ Zero test failures
- ✅ Zero HTTP 500 errors in Application Insights
- ✅ Code coverage >80% for changed handlers

**User Experience**:
- ✅ Organizers can view attendee lists
- ✅ CSV/Excel exports work
- ✅ Anonymous users see registration confirmation

**Production Verification**:
- ✅ 100+ successful API calls with corrupt data
- ✅ No increase in error rate
- ✅ No user-reported issues

---

## Section 9: Architecture Decision Record (ADR)

**Title**: ADR-006: Use Direct LINQ Projection for JSONB Queries to Handle Nullable Enums

**Context**:
Entity Framework Core cannot materialize JSONB data with null enum values into domain objects with non-nullable enum properties. This causes runtime exceptions when using `.Include()` on JSONB columns.

**Decision**:
For all query handlers that access JSONB-stored complex types (e.g., `Attendees`, `Contact`), use direct LINQ projection with `Select()` to map to nullable DTO properties.

**Consequences**:

**Positive**:
- ✅ Handles corrupt/legacy data gracefully
- ✅ No runtime exceptions
- ✅ Clear query intent (explicit projections)
- ✅ Performance: single SQL query with JSON projection
- ✅ AsNoTracking reduces memory overhead

**Negative**:
- ❌ More verbose code (no automatic mapping)
- ❌ Duplicate projection logic across handlers (mitigated by DTOs)

**Alternatives Considered**:
1. Make domain model properties nullable → Rejected (breaks domain invariants)
2. Custom EF value converter → Rejected (complex, fragile)
3. Data migration to clean JSONB → Rejected (risky, doesn't prevent future issues)

**Implementation Pattern**:
```csharp
// ❌ DON'T: Materialization with Include
.Include(r => r.Attendees).ToListAsync()

// ✅ DO: Direct LINQ projection
.Select(r => new RegistrationDetailsDto
{
    Attendees = r.Attendees.Select(a => new AttendeeDetailsDto
    {
        AgeCategory = a.AgeCategory  // Nullable DTO handles null JSONB
    }).ToList()
}).ToListAsync()
```

**Code Review Checklist**:
- [ ] No `.Include()` calls on JSONB properties (Attendees, Contact)
- [ ] All JSONB projections use nullable DTOs
- [ ] `AsNoTracking()` used for read-only queries
- [ ] Unit tests include null/corrupt JSONB scenarios

---

## Section 10: Prevention Measures

### 10.1 Developer Guidelines

**Document**: `docs/DEVELOPER_GUIDELINES_JSONB_QUERIES.md`

**Content**:
```markdown
# JSONB Query Pattern Guidelines

## ❌ Forbidden Pattern
Never use `.Include()` on JSONB-backed navigation properties.

## ✅ Correct Pattern
Use direct LINQ projection with nullable DTOs.

## Example
// Event Registrations with Attendees (JSONB)
var registrations = await _context.Registrations
    .AsNoTracking()
    .Where(/* filters */)
    .Select(r => new RegistrationDto
    {
        Attendees = r.Attendees.Select(a => new AttendeeDto
        {
            AgeCategory = a.AgeCategory  // Nullable DTO
        }).ToList()
    })
    .ToListAsync();
```

---

### 10.2 Analyzer Rule (Future Enhancement)

**Rule ID**: LANKA001

**Title**: Avoid Include() on JSONB properties

**Severity**: Error

**Implementation** (Roslyn Analyzer):
```csharp
// Detect: .Include(r => r.Attendees)
// Error: LANKA001: Use direct projection instead of Include for JSONB properties
```

---

## Section 11: Timeline & Milestones

### Estimated Timeline

| Phase | Tasks | Duration | Owner |
|-------|-------|----------|-------|
| **Phase 1: Critical Fixes** | Fix GetEventAttendeesQueryHandler, GetRegistrationByIdQueryHandler | 2-3 hours | Backend Dev |
| **Phase 2: Testing** | Write unit tests, integration tests, API tests | 1-2 hours | QA / Backend Dev |
| **Phase 3: Deployment** | Build, test in staging, deploy to production | 1 hour | DevOps |
| **Phase 4: Monitoring** | Monitor Application Insights, verify metrics | 1 hour | DevOps |
| **Phase 5: Documentation** | Create ADR, update guidelines, phase summary | 1-2 hours | System Architect |

**Total**: 6-9 hours spread over 2 days

---

### Milestones

**Milestone 1**: Both critical handlers fixed and tested (Day 1)
**Milestone 2**: Deployed to staging and verified (Day 1)
**Milestone 3**: Deployed to production and monitored (Day 2)
**Milestone 4**: ADR and prevention measures complete (Day 2)

---

## Section 12: Impact Assessment

### 12.1 User Impact

**Before Fix**:
- ❌ Organizers cannot view attendee lists (HTTP 500)
- ❌ CSV/Excel exports fail
- ❌ Anonymous users see error after payment
- ❌ Admin cannot view registration details

**After Fix**:
- ✅ All features work with corrupt/legacy data
- ✅ Null AgeCategory handled gracefully (shown as empty)
- ✅ Adult/child counts accurate (exclude nulls)
- ✅ No user-facing errors

### 12.2 Risk Assessment

**Risk Level**: LOW-MEDIUM

**Reasons**:
- ✅ Changes isolated to query handlers (no command handlers)
- ✅ No database migrations (no schema changes)
- ✅ No breaking API changes (DTOs already nullable)
- ✅ Easy rollback (single git revert)
- ✅ Incremental deployment (test each phase)

**Mitigation**:
- Comprehensive testing before deployment
- Staged rollout (staging → production)
- Monitoring Application Insights for 24 hours
- Rollback plan ready

---

## Section 13: Lessons Learned & Future Prevention

### 13.1 What Went Well (Phase 6A.48)

1. ✅ Identified root cause correctly (JSONB null enum)
2. ✅ Fixed one handler successfully
3. ✅ DTO made nullable to handle corrupt data

### 13.2 What Could Be Improved

1. ❌ Only fixed ONE handler instead of auditing entire codebase
2. ❌ No ADR created to prevent recurrence
3. ❌ No code review checklist for JSONB queries
4. ❌ No analyzer rule to catch the pattern

### 13.3 Prevention Strategy (Going Forward)

**Immediate**:
1. ✅ Fix all handlers (this phase)
2. ✅ Create ADR for JSONB query pattern
3. ✅ Add to code review checklist
4. ✅ Update developer guidelines

**Long-term**:
1. Create Roslyn analyzer rule (LANKA001)
2. Add JSONB query unit test template
3. Document in architectural decision log
4. Include in onboarding documentation

---

## Appendix A: File Locations

**Handlers to Fix**:
- `src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs`
- `src/LankaConnect.Application/Events/Queries/GetRegistrationById/GetRegistrationByIdQueryHandler.cs`

**Handlers to Verify**:
- `src/LankaConnect.Application/Events/Queries/ExportEventAttendees/ExportEventAttendeesQueryHandler.cs`

**DTOs (Already Fixed)**:
- `src/LankaConnect.Application/Events/Common/RegistrationDetailsDto.cs` (Phase 6A.48)
- `src/LankaConnect.Application/Events/Common/EventAttendeeDto.cs`

**API Endpoints**:
- `src/LankaConnect.API/Controllers/EventsController.cs`
  - Line 1843: `GetEventAttendees()`
  - Line 852: `GetRegistrationById()`
  - Line 1886: `ExportEventAttendees()`

**Tests to Create**:
- `tests/LankaConnect.Application.Tests/Events/Queries/GetEventAttendeesQueryHandlerTests.cs`
- `tests/LankaConnect.Application.Tests/Events/Queries/GetRegistrationByIdQueryHandlerTests.cs`

**Scripts**:
- `scripts/test-event-attendees-api.ps1` (new)

---

## Appendix B: Related Documents

**Phase 6A.48** (Partial Fix):
- [PHASE_6A48_CSV_EXPORT_FIX_SUMMARY.md](./PHASE_6A48_CSV_EXPORT_FIX_SUMMARY.md)

**Phase 6A.49** (Current Investigation):
- [PHASE_6A49_PAID_EVENT_EMAIL_SILENCE_RCA.md](./PHASE_6A49_PAID_EVENT_EMAIL_SILENCE_RCA.md)

**Tracking Documents**:
- [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md)
- [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md)
- [STREAMLINED_ACTION_PLAN.md](./STREAMLINED_ACTION_PLAN.md)

---

## Appendix C: SQL Diagnostic Queries

**Find Registrations with Null AgeCategory**:
```sql
SELECT
  r.id,
  r.event_id,
  e.title as event_title,
  r.created_at,
  jsonb_array_length(r.attendees) as attendee_count,
  (
    SELECT COUNT(*)
    FROM jsonb_array_elements(r.attendees) a
    WHERE a->>'ageCategory' IS NULL
  ) as null_age_category_count
FROM events.event_registrations r
JOIN events.events e ON r.event_id = e.id
WHERE jsonb_array_length(r.attendees) > 0
  AND EXISTS (
    SELECT 1
    FROM jsonb_array_elements(r.attendees) a
    WHERE a->>'ageCategory' IS NULL
  )
ORDER BY r.created_at DESC
LIMIT 20;
```

**Fix Corrupt JSONB Data** (Optional):
```sql
-- Update null AgeCategory to Adult (default)
UPDATE events.event_registrations
SET attendees = (
  SELECT jsonb_agg(
    CASE
      WHEN a->>'ageCategory' IS NULL
      THEN jsonb_set(a, '{ageCategory}', '1')  -- 1 = Adult enum value
      ELSE a
    END
  )
  FROM jsonb_array_elements(attendees) a
)
WHERE EXISTS (
  SELECT 1
  FROM jsonb_array_elements(attendees) a
  WHERE a->>'ageCategory' IS NULL
);
```

---

**Document Status**: ✅ PLANNING COMPLETE - READY FOR IMPLEMENTATION
**Created**: 2025-12-26
**Last Updated**: 2025-12-26
**Next Step**: Implement Phase 1 (Fix Critical Handlers)
**Assigned To**: System Architect → Backend Developer
**Priority**: P0 - CRITICAL
**Estimated Total Time**: 6-9 hours over 2 days
