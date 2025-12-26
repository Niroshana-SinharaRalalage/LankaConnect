# Phase 6A.49: JSONB Data Integrity Permanent Fix Plan

**Date**: 2025-12-25
**Status**: ARCHITECT APPROVED
**Priority**: P1 - HIGH (Prevents data corruption, permanent solution)
**Phase Number**: Phase 6A.49

---

## Executive Summary

This document provides a **comprehensive, permanent fix** for JSONB data integrity issues identified in Phase 6A.48. The current fix (making DTOs nullable) is a **defensive workaround** that prevents crashes but doesn't address the **root cause**: corrupt data in the database and lack of data integrity constraints.

**Problem Statement**:
- Database contains corrupt JSONB records with null/invalid AgeCategory values
- Current fix (nullable DTOs) prevents crashes but allows corrupt data to persist
- No database constraints prevent future corruption
- No validation ensures data integrity at write time

**Solution Strategy**:
This plan implements a **5-phase systematic approach** following TDD methodology:
1. **Detection & Analysis** - Identify all corrupt records and affected endpoints
2. **Defensive Code Changes** - Add null checks to ALL query handlers (TDD)
3. **Database Cleanup** - Remove corrupt data with validation
4. **Database Constraints** - Prevent future corruption with CHECK constraints
5. **Monitoring & Documentation** - Track data quality metrics

**Success Criteria**:
- ✅ Zero build errors throughout implementation
- ✅ All 4 affected endpoints handle null AgeCategory gracefully
- ✅ Database cleanup verified: 0 corrupt records remaining
- ✅ CHECK constraints deployed and tested
- ✅ 100% test coverage for null AgeCategory scenarios
- ✅ All 3 PRIMARY docs updated

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Phase-by-Phase Implementation](#phase-by-phase-implementation)
3. [Deployment Strategy](#deployment-strategy)
4. [Validation Checklist](#validation-checklist)
5. [Success Criteria](#success-criteria-1)
6. [Risk Assessment](#risk-assessment)
7. [Rollback Procedures](#rollback-procedures)
8. [Documentation Updates](#documentation-updates)

---

## Prerequisites

### Before Starting Implementation

**✅ Checklist**:
- [ ] Verify commit 0daa9168 is deployed to Azure staging
- [ ] Verify commit fa30668d (CSV export fixes) is deployed
- [ ] Confirm no other developers working on Registration/Attendee code
- [ ] Backup production database (if deploying to production)
- [ ] Review [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md) - Phase 6A.49 available
- [ ] Review similar implementations in codebase for patterns

**Environment Verification**:
```bash
# 1. Check current branch
git status

# 2. Verify build succeeds
dotnet build

# 3. Verify tests pass
dotnet test

# 4. Check deployed version
az containerapp show --name lankaconnect-staging-api --resource-group <rg> \
  --query "properties.template.containers[0].image"
```

**Expected Output**:
- Branch: `develop` (or feature branch)
- Build: 0 errors, 0 warnings
- Tests: All passing
- Deployed image: Contains commit 0daa9168 or later

---

## Phase-by-Phase Implementation

### Phase 1: Detection & Analysis (1 hour)

**Objective**: Identify ALL corrupt records and ALL affected code paths.

#### 1.1 Database Forensics

**SQL Query 1: Find Corrupt Records**
```sql
-- Find all registrations with null AgeCategory in JSONB
WITH corrupt_records AS (
  SELECT
    r.id as registration_id,
    r.event_id,
    r.user_id,
    r.created_at,
    r.attendees,
    -- Check each attendee for null AgeCategory
    jsonb_array_length(r.attendees) as attendee_count,
    (
      SELECT COUNT(*)
      FROM jsonb_array_elements(r.attendees) elem
      WHERE elem->>'AgeCategory' IS NULL
        OR elem->>'AgeCategory' = ''
        OR elem->>'AgeCategory' = '0'
    ) as corrupt_attendee_count
  FROM events.event_registrations r
  WHERE r.attendees IS NOT NULL
    AND jsonb_array_length(r.attendees) > 0
)
SELECT
  registration_id,
  event_id,
  user_id,
  created_at,
  attendee_count,
  corrupt_attendee_count,
  attendees
FROM corrupt_records
WHERE corrupt_attendee_count > 0
ORDER BY created_at DESC;

-- Save results to CSV for analysis
\copy (SELECT registration_id, event_id, user_id, created_at, attendee_count, corrupt_attendee_count FROM corrupt_records WHERE corrupt_attendee_count > 0) TO 'corrupt_records_analysis.csv' CSV HEADER;
```

**SQL Query 2: Analyze Corruption Patterns**
```sql
-- When did corruption start?
SELECT
  DATE(created_at) as corruption_date,
  COUNT(*) as corrupt_registration_count
FROM events.event_registrations r
WHERE EXISTS (
  SELECT 1
  FROM jsonb_array_elements(r.attendees) elem
  WHERE elem->>'AgeCategory' IS NULL
    OR elem->>'AgeCategory' = ''
    OR elem->>'AgeCategory' = '0'
)
GROUP BY DATE(created_at)
ORDER BY corruption_date DESC;

-- Which events are affected?
SELECT
  e.id,
  e.title,
  COUNT(DISTINCT r.id) as corrupt_registration_count
FROM events.events e
JOIN events.event_registrations r ON r.event_id = e.id
WHERE EXISTS (
  SELECT 1
  FROM jsonb_array_elements(r.attendees) elem
  WHERE elem->>'AgeCategory' IS NULL
    OR elem->>'AgeCategory' = ''
    OR elem->>'AgeCategory' = '0'
)
GROUP BY e.id, e.title
ORDER BY corrupt_registration_count DESC;
```

**Expected Findings**:
- X registrations with corrupt data
- Corruption pattern (when it started)
- Affected events
- Total corrupt attendee records

**Document Results**: Save to `scripts/corrupt_records_analysis.csv`

#### 1.2 Code Analysis - Find All AgeCategory Usages

**Search 1: All Query Handlers**
```bash
# Find all query handlers that map AgeCategory
cd c:/Work/LankaConnect/src/LankaConnect.Application/Events/Queries
grep -r "AgeCategory" --include="*QueryHandler.cs" -n
```

**Search 2: All DTO Projections**
```bash
# Find all Select() statements that map attendees
cd c:/Work/LankaConnect/src/LankaConnect.Application/Events
grep -r "Select.*AttendeeDetailsDto" --include="*.cs" -n -A 5
```

**Known Affected Files** (from prior analysis):
1. `GetUserRegistrationForEventQueryHandler.cs` - ✅ Fixed in 0daa9168
2. `GetTicketQueryHandler.cs` - ❌ NOT FIXED (lines 116-122)
3. `GetEventAttendeesQueryHandler.cs` - ❌ NOT FIXED (lines 66, 82-87)
4. `GetRegistrationByIdQueryHandler.cs` - ❌ NOT FIXED (lines 39-44)

**Expected Output**:
- List of ALL files that need defensive null checks
- Line numbers for each AgeCategory mapping
- Patterns to follow for null handling

#### 1.3 Search for Similar Null-Handling Patterns

**Pattern Search**: Find how other nullable enums are handled in codebase
```bash
# Search for Gender? handling (it's already nullable)
cd c:/Work/LankaConnect/src/LankaConnect.Application
grep -r "Gender\?" --include="*.cs" -n -B 2 -A 2

# Search for HasValue checks
grep -r "\.HasValue" --include="*QueryHandler.cs" -n -B 1 -A 1
```

**Document Reusable Patterns**: Identify consistent approach used in codebase.

**✅ Validation**:
- [ ] Corrupt records count documented
- [ ] All 4+ affected files identified
- [ ] Reusable patterns found and documented

---

### Phase 2: Defensive Code Changes (TDD) (4 hours)

**Objective**: Add comprehensive null checks to ALL query handlers using TDD methodology.

#### 2.1 Write Integration Tests FIRST (RED)

**Test File**: `tests/LankaConnect.IntegrationTests/Events/CorruptAttendeesIntegrationTests.cs`

```csharp
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Queries.GetUserRegistrationForEvent;
using LankaConnect.Application.Events.Queries.GetTicket;
using LankaConnect.Application.Events.Queries.GetEventAttendees;
using LankaConnect.Application.Events.Queries.GetRegistrationById;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LankaConnect.IntegrationTests.Events;

/// <summary>
/// Phase 6A.49: Integration tests for handling corrupt JSONB data with null AgeCategory
/// These tests verify that all endpoints gracefully handle corrupt data without crashing
/// </summary>
public class CorruptAttendeesIntegrationTests : BaseIntegrationTest
{
    [Fact]
    public async Task GetUserRegistrationForEvent_WithNullAgeCategory_ReturnsSuccess()
    {
        // Arrange: Create registration with corrupt JSONB data (null AgeCategory)
        var registrationId = await CreateRegistrationWithCorruptAttendees();

        var query = new GetUserRegistrationForEventQuery(
            EventId: TestEventId,
            UserId: TestUserId
        );

        // Act: Execute query
        var handler = ServiceProvider.GetRequiredService<IQueryHandler<GetUserRegistrationForEventQuery, RegistrationDetailsDto?>>();
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert: Should succeed and return registration with null AgeCategory
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value.Attendees);
        Assert.Null(result.Value.Attendees[0].AgeCategory); // Corrupt data returns null
    }

    [Fact]
    public async Task GetTicket_WithNullAgeCategory_ReturnsSuccess()
    {
        // Arrange: Create registration and ticket with corrupt attendees
        var registrationId = await CreateRegistrationWithCorruptAttendees();
        await CreateTicketForRegistration(registrationId);

        var query = new GetTicketQuery(
            EventId: TestEventId,
            RegistrationId: registrationId,
            UserId: TestUserId
        );

        // Act
        var handler = ServiceProvider.GetRequiredService<IRequestHandler<GetTicketQuery, Result<TicketDto>>>();
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert: Should succeed even with corrupt attendee data
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Attendees);
        Assert.Null(result.Value.Attendees[0].AgeCategory); // Handles null gracefully
    }

    [Fact]
    public async Task GetEventAttendees_WithMixedCorruptData_ReturnsAllRegistrations()
    {
        // Arrange: Create mix of valid and corrupt registrations
        await CreateRegistrationWithValidAttendees();
        await CreateRegistrationWithCorruptAttendees();

        var query = new GetEventAttendeesQuery(TestEventId);

        // Act
        var handler = ServiceProvider.GetRequiredService<IQueryHandler<GetEventAttendeesQuery, EventAttendeesResponse>>();
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert: Should return all registrations including corrupt ones
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Attendees.Count);

        // Verify count calculation handles null AgeCategory (doesn't crash)
        Assert.True(result.Value.TotalAttendees > 0);
    }

    [Fact]
    public async Task GetRegistrationById_WithNullAgeCategory_ReturnsSuccess()
    {
        // Arrange
        var registrationId = await CreateRegistrationWithCorruptAttendees();
        var query = new GetRegistrationByIdQuery(registrationId);

        // Act
        var handler = ServiceProvider.GetRequiredService<IQueryHandler<GetRegistrationByIdQuery, RegistrationDetailsDto?>>();
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Null(result.Value.Attendees[0].AgeCategory);
    }

    // Helper methods to create test data
    private Guid TestEventId { get; set; }
    private Guid TestUserId { get; set; }

    private async Task<Guid> CreateRegistrationWithCorruptAttendees()
    {
        // Create registration with JSONB containing null AgeCategory
        // This simulates the corrupt data found in production
        var registration = new Registration(
            eventId: TestEventId,
            userId: TestUserId,
            quantity: 1,
            ticketPrice: null
        );

        // Use raw SQL to insert corrupt JSONB data
        await ExecuteDbContextAsync(async context =>
        {
            await context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO events.event_registrations (id, event_id, user_id, quantity, status, payment_status, attendees, created_at)
                VALUES (
                    @p0,
                    @p1,
                    @p2,
                    1,
                    0, -- RegistrationStatus.Pending
                    0, -- PaymentStatus.Pending
                    '[{""Name"": ""Test Attendee"", ""AgeCategory"": null, ""Gender"": null}]'::jsonb,
                    NOW()
                )",
                registration.Id, TestEventId, TestUserId
            );
        });

        return registration.Id;
    }

    private async Task<Guid> CreateRegistrationWithValidAttendees()
    {
        // Create registration with valid attendee data for comparison
        // Implementation details...
        return Guid.NewGuid();
    }

    private async Task CreateTicketForRegistration(Guid registrationId)
    {
        // Create ticket for testing GetTicket endpoint
        // Implementation details...
    }
}
```

**Run Tests** (should FAIL):
```bash
cd c:/Work/LankaConnect
dotnet test --filter "FullyQualifiedName~CorruptAttendeesIntegrationTests"
```

**Expected Result**: RED - Tests fail because:
1. `GetTicketQueryHandler` crashes on null AgeCategory at line 119
2. `GetEventAttendeesQueryHandler` crashes at lines 66-67
3. Other handlers may crash or return unexpected results

**✅ Validation**:
- [ ] All 4 test methods written
- [ ] Tests compile without errors
- [ ] Tests fail as expected (RED phase)

#### 2.2 Implement Defensive Code Changes (GREEN)

**Change 1: TicketDto.cs** (Already has nullable DTO, but verify)
```csharp
// File: src/LankaConnect.Application/Events/Common/TicketDto.cs
// Lines 32-41

/// <summary>
/// Phase 6A.24: Attendee information for ticket display with age category and gender
/// Phase 6A.48: Made AgeCategory nullable to handle legacy/corrupted JSONB data
/// </summary>
public record TicketAttendeeDto
{
    public string Name { get; init; } = string.Empty;
    public AgeCategory? AgeCategory { get; init; } // ✅ Already nullable (verify local changes committed)
    public Gender? Gender { get; init; }
}
```

**Change 2: GetTicketQueryHandler.cs** (Add null-safe mapping)
```csharp
// File: src/LankaConnect.Application/Events/Queries/GetTicket/GetTicketQueryHandler.cs
// Lines 114-122

// BEFORE (CRASHES on null):
var attendees = registration.HasDetailedAttendees()
    ? registration.Attendees.Select(a => new TicketAttendeeDto
    {
        Name = a.Name,
        AgeCategory = a.AgeCategory, // ❌ CRASHES if a.AgeCategory is null
        Gender = a.Gender
    }).ToList()
    : null;

// AFTER (SAFE):
var attendees = registration.HasDetailedAttendees()
    ? registration.Attendees
        .Where(a => a != null) // ✅ Filter out null attendees
        .Select(a => new TicketAttendeeDto
        {
            Name = a?.Name ?? "Unknown", // ✅ Handle null attendee
            AgeCategory = a?.AgeCategory, // ✅ Already nullable, safe access
            Gender = a?.Gender
        }).ToList()
    : null;
```

**Change 3: GetEventAttendeesQueryHandler.cs** (Handle null in counts)
```csharp
// File: src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs
// Lines 63-87

private EventAttendeeDto MapToDto(Registration registration)
{
    // BEFORE (CRASHES on null AgeCategory):
    var adultCount = registration.Attendees.Count(a => a.AgeCategory == AgeCategory.Adult);
    var childCount = registration.Attendees.Count(a => a.AgeCategory == AgeCategory.Child);

    // AFTER (SAFE with null checks):
    var adultCount = registration.Attendees
        .Count(a => a?.AgeCategory == AgeCategory.Adult); // ✅ Null-safe comparison
    var childCount = registration.Attendees
        .Count(a => a?.AgeCategory == AgeCategory.Child); // ✅ Null-safe comparison

    // Gender distribution already handles null Gender, verify it handles null attendees
    var genderCounts = registration.Attendees
        .Where(a => a != null && a.Gender.HasValue) // ✅ Filter nulls
        .GroupBy(a => a.Gender!.Value)
        .Select(g => $"{g.Count()} {g.Key}")
        .ToList();

    var genderDistribution = genderCounts.Any()
        ? string.Join(", ", genderCounts)
        : string.Empty;

    // Map attendees with null safety
    var attendeeDtos = registration.Attendees
        .Where(a => a != null) // ✅ Filter null attendees
        .Select(a => new AttendeeDetailsDto
        {
            Name = a.Name,
            AgeCategory = a.AgeCategory, // Safe: DTO is nullable
            Gender = a.Gender
        }).ToList();

    return new EventAttendeeDto
    {
        // ... rest of mapping ...
        Attendees = attendeeDtos,
        TotalAttendees = attendeeDtos.Count, // ✅ Count after filtering nulls
        AdultCount = adultCount,
        ChildCount = childCount,
        GenderDistribution = genderDistribution,
        // ...
    };
}
```

**Change 4: GetRegistrationByIdQueryHandler.cs** (Already uses nullable DTO projection)
```csharp
// File: src/LankaConnect.Application/Events/Queries/GetRegistrationById/GetRegistrationByIdQueryHandler.cs
// Lines 38-44

// Current code (verify it's safe):
Attendees = r.Attendees.Select(a => new AttendeeDetailsDto
{
    Name = a.Name,
    AgeCategory = a.AgeCategory, // Safe: DTO is nullable
    Gender = a.Gender
}).ToList(),

// Potential issue: If r.Attendees collection contains null elements
// FIX: Add null check in projection
Attendees = r.Attendees
    .Where(a => a != null) // ✅ Filter null elements
    .Select(a => new AttendeeDetailsDto
    {
        Name = a.Name ?? "Unknown", // ✅ Handle null Name
        AgeCategory = a.AgeCategory,
        Gender = a.Gender
    }).ToList(),
```

**✅ Validation**:
- [ ] All 4 files modified with defensive null checks
- [ ] Code compiles with 0 errors
- [ ] No warnings introduced

#### 2.3 Run Tests (GREEN)

```bash
# Run all tests
dotnet test

# Run specific integration tests
dotnet test --filter "FullyQualifiedName~CorruptAttendeesIntegrationTests"

# Run all event query tests
dotnet test --filter "FullyQualifiedName~Events.Queries"
```

**Expected Result**: GREEN - All tests pass

**✅ Validation**:
- [ ] All 4 integration tests pass
- [ ] No regressions in existing tests
- [ ] Build succeeds with 0 errors/warnings

#### 2.4 Commit Changes

**Commit 1: Integration Tests**
```bash
git add tests/LankaConnect.IntegrationTests/Events/CorruptAttendeesIntegrationTests.cs
git commit -m "$(cat <<'EOF'
test(phase-6a49): Add integration tests for corrupt JSONB attendee data

Add comprehensive integration tests to verify all 4 affected endpoints
handle null AgeCategory values gracefully:
- GetUserRegistrationForEvent
- GetTicket
- GetEventAttendees
- GetRegistrationById

Tests simulate production corrupt data scenarios and verify:
- Endpoints return 200 OK instead of 500 error
- Null AgeCategory values are handled without crashes
- Mixed valid/corrupt data is processed correctly
- Adult/child counts handle null values

Part of Phase 6A.49 TDD permanent fix for JSONB data integrity.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
EOF
)"
```

**Commit 2: TicketDto Nullable Change** (if not already committed)
```bash
git add src/LankaConnect.Application/Events/Common/TicketDto.cs
git commit -m "$(cat <<'EOF'
fix(phase-6a49): Make TicketAttendeeDto.AgeCategory nullable

Make AgeCategory nullable in TicketAttendeeDto to match AttendeeDetailsDto
and handle corrupt JSONB data gracefully.

This prevents crashes in GetTicket endpoint when JSONB contains null values.

Part of Phase 6A.49 permanent fix for JSONB data integrity.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
EOF
)"
```

**Commit 3: Query Handler Defensive Code**
```bash
git add src/LankaConnect.Application/Events/Queries/GetTicket/GetTicketQueryHandler.cs
git add src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs
git add src/LankaConnect.Application/Events/Queries/GetRegistrationById/GetRegistrationByIdQueryHandler.cs

git commit -m "$(cat <<'EOF'
fix(phase-6a49): Add defensive null checks for corrupt JSONB attendee data

Add comprehensive null safety to all query handlers that map attendee data:

GetTicketQueryHandler:
- Add null checks when mapping Attendees to TicketAttendeeDto
- Handle null attendee objects and null properties gracefully

GetEventAttendeesQueryHandler:
- Add null-safe comparisons in adult/child count calculations
- Filter null attendees before mapping to DTOs
- Handle null attendees in gender distribution

GetRegistrationByIdQueryHandler:
- Filter null attendees in LINQ projection
- Add null coalescing for Name property

These changes prevent 500 errors when JSONB contains:
- Null attendee objects
- Null AgeCategory values
- Null Name values

All endpoints now return 200 OK with partial data instead of crashing.

Part of Phase 6A.49 permanent fix for JSONB data integrity.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
EOF
)"
```

**✅ Validation**:
- [ ] 3 separate commits created
- [ ] Commits follow project convention
- [ ] Git history is clean

---

### Phase 3: Database Cleanup (2 hours)

**Objective**: Remove ALL corrupt JSONB data with validation and backup.

#### 3.1 Create Backup Table

**SQL Script**: `scripts/phase-6a49-backup-corrupt-records.sql`
```sql
-- Phase 6A.49: Backup corrupt registrations before cleanup
-- Date: 2025-12-25

-- Create backup schema if not exists
CREATE SCHEMA IF NOT EXISTS backups;

-- Create backup table with timestamp
CREATE TABLE backups.corrupt_registrations_20251225 AS
SELECT
  r.id,
  r.event_id,
  r.user_id,
  r.quantity,
  r.status,
  r.payment_status,
  r.attendees,
  r.contact_email,
  r.contact_phone,
  r.contact_address,
  r.created_at,
  r.updated_at,
  NOW() as backed_up_at
FROM events.event_registrations r
WHERE EXISTS (
  SELECT 1
  FROM jsonb_array_elements(r.attendees) elem
  WHERE elem->>'AgeCategory' IS NULL
    OR elem->>'AgeCategory' = ''
    OR elem->>'AgeCategory' = '0'
);

-- Verify backup
SELECT
  COUNT(*) as total_backed_up,
  MIN(created_at) as oldest_record,
  MAX(created_at) as newest_record
FROM backups.corrupt_registrations_20251225;

-- Grant read access to backup table
GRANT SELECT ON backups.corrupt_registrations_20251225 TO PUBLIC;
```

**Run Backup**:
```bash
# Execute backup script
psql $DATABASE_URL -f scripts/phase-6a49-backup-corrupt-records.sql > backup_results.log

# Verify backup count
psql $DATABASE_URL -c "SELECT COUNT(*) FROM backups.corrupt_registrations_20251225;"
```

**✅ Validation**:
- [ ] Backup table created successfully
- [ ] Record count matches analysis from Phase 1
- [ ] Backup contains attendees JSONB data

#### 3.2 Create Cleanup Script

**SQL Script**: `scripts/phase-6a49-cleanup-corrupt-attendees.sql`
```sql
-- Phase 6A.49: Clean up corrupt JSONB attendee data
-- Date: 2025-12-25
-- CRITICAL: This script REMOVES attendee records with null AgeCategory
-- Strategy: Remove individual corrupt attendees from JSONB array

-- Option A: Remove entire registration if ALL attendees are corrupt
-- (Use if registration is unusable)
BEGIN;

-- Report what will be deleted
SELECT
  r.id,
  r.event_id,
  jsonb_array_length(r.attendees) as total_attendees,
  (
    SELECT COUNT(*)
    FROM jsonb_array_elements(r.attendees) elem
    WHERE elem->>'AgeCategory' IS NULL
      OR elem->>'AgeCategory' = ''
      OR elem->>'AgeCategory' = '0'
  ) as corrupt_attendees,
  r.attendees
FROM events.event_registrations r
WHERE EXISTS (
  SELECT 1
  FROM jsonb_array_elements(r.attendees) elem
  WHERE elem->>'AgeCategory' IS NULL
    OR elem->>'AgeCategory' = ''
    OR elem->>'AgeCategory' = '0'
)
AND (
  -- All attendees are corrupt
  SELECT COUNT(*)
  FROM jsonb_array_elements(r.attendees) elem
  WHERE elem->>'AgeCategory' IS NULL
    OR elem->>'AgeCategory' = ''
    OR elem->>'AgeCategory' = '0'
) = jsonb_array_length(r.attendees);

-- DELETE registrations where ALL attendees are corrupt
-- Uncomment to execute:
-- DELETE FROM events.event_registrations r
-- WHERE EXISTS (
--   SELECT 1
--   FROM jsonb_array_elements(r.attendees) elem
--   WHERE elem->>'AgeCategory' IS NULL
--     OR elem->>'AgeCategory' = ''
--     OR elem->>'AgeCategory' = '0'
-- )
-- AND (
--   SELECT COUNT(*)
--   FROM jsonb_array_elements(r.attendees) elem
--   WHERE elem->>'AgeCategory' IS NULL
--     OR elem->>'AgeCategory' = ''
--     OR elem->>'AgeCategory' = '0'
-- ) = jsonb_array_length(r.attendees);

ROLLBACK; -- Don't commit yet, verify first

-- Option B: Fix corrupt attendees by setting default AgeCategory
-- (Use if registration should be preserved)
BEGIN;

-- Update corrupt attendees to Adult (default)
UPDATE events.event_registrations
SET attendees = (
  SELECT jsonb_agg(
    CASE
      WHEN elem->>'AgeCategory' IS NULL
        OR elem->>'AgeCategory' = ''
        OR elem->>'AgeCategory' = '0'
      THEN jsonb_set(elem, '{AgeCategory}', '1') -- 1 = Adult
      ELSE elem
    END
  )
  FROM jsonb_array_elements(attendees) elem
)
WHERE EXISTS (
  SELECT 1
  FROM jsonb_array_elements(attendees) elem
  WHERE elem->>'AgeCategory' IS NULL
    OR elem->>'AgeCategory' = ''
    OR elem->>'AgeCategory' = '0'
);

-- Verify fix
SELECT
  COUNT(*) as registrations_fixed,
  SUM(jsonb_array_length(attendees)) as total_attendees_after_fix
FROM events.event_registrations
WHERE id IN (SELECT id FROM backups.corrupt_registrations_20251225);

COMMIT; -- Only commit if verification passes
```

**Decision Point**: Choose Option A or B based on business requirements.

**Recommended Approach**: Option B (set default AgeCategory to Adult)
- Preserves user registrations
- Payment records remain intact
- Tickets remain valid
- Default to Adult is safer than Child for pricing

**Run Cleanup** (staging first):
```bash
# DRY RUN: See what will be changed
psql $STAGING_DATABASE_URL -f scripts/phase-6a49-cleanup-corrupt-attendees.sql

# ACTUAL RUN: Execute cleanup
psql $STAGING_DATABASE_URL -c "
UPDATE events.event_registrations
SET attendees = (
  SELECT jsonb_agg(
    CASE
      WHEN elem->>'AgeCategory' IS NULL
        OR elem->>'AgeCategory' = ''
        OR elem->>'AgeCategory' = '0'
      THEN jsonb_set(elem, '{AgeCategory}', '1')
      ELSE elem
    END
  )
  FROM jsonb_array_elements(attendees) elem
)
WHERE EXISTS (
  SELECT 1
  FROM jsonb_array_elements(attendees) elem
  WHERE elem->>'AgeCategory' IS NULL
    OR elem->>'AgeCategory' = ''
    OR elem->>'AgeCategory' = '0'
);
"
```

**✅ Validation**:
- [ ] Cleanup script created
- [ ] Dry run shows expected changes
- [ ] Cleanup executed successfully
- [ ] Verification query shows 0 corrupt records remaining

#### 3.3 Verify Cleanup

**Verification Query**:
```sql
-- Verify NO corrupt records remain
SELECT COUNT(*) as remaining_corrupt_records
FROM events.event_registrations r
WHERE EXISTS (
  SELECT 1
  FROM jsonb_array_elements(r.attendees) elem
  WHERE elem->>'AgeCategory' IS NULL
    OR elem->>'AgeCategory' = ''
    OR elem->>'AgeCategory' = '0'
);
-- Expected: 0

-- Verify all fixed records now have valid AgeCategory
SELECT
  id,
  attendees
FROM events.event_registrations
WHERE id IN (SELECT id FROM backups.corrupt_registrations_20251225)
LIMIT 5;
-- Expected: All attendees have AgeCategory = 1 (Adult) or 2 (Child)
```

**✅ Validation**:
- [ ] 0 corrupt records remaining
- [ ] Sample inspection shows valid AgeCategory values
- [ ] No registration data lost

---

### Phase 4: Database Constraints (1 hour)

**Objective**: Add CHECK constraints to prevent future corruption.

#### 4.1 Create EF Core Migration

**Migration File**: `src/LankaConnect.Infrastructure/Data/Migrations/YYYYMMDDHHMMSS_AddAttendeeDataIntegrityConstraints_Phase6A49.cs`

```bash
# Create migration
cd c:/Work/LankaConnect/src/LankaConnect.Infrastructure
dotnet ef migrations add AddAttendeeDataIntegrityConstraints_Phase6A49 --context AppDbContext
```

**Edit Migration** (add CHECK constraints):
```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.49: Add CHECK constraints to prevent corrupt JSONB attendee data
    /// Ensures AgeCategory is never null in attendees JSONB array
    /// </summary>
    public partial class AddAttendeeDataIntegrityConstraints_Phase6A49 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add CHECK constraint: All attendees must have non-null AgeCategory
            migrationBuilder.Sql(@"
                ALTER TABLE events.event_registrations
                ADD CONSTRAINT chk_attendees_age_category_not_null
                CHECK (
                    attendees IS NULL
                    OR NOT EXISTS (
                        SELECT 1
                        FROM jsonb_array_elements(attendees) elem
                        WHERE elem->>'AgeCategory' IS NULL
                          OR elem->>'AgeCategory' = ''
                          OR elem->>'AgeCategory' = '0'
                    )
                );
            ");

            // Add CHECK constraint: AgeCategory must be valid enum value (1 or 2)
            migrationBuilder.Sql(@"
                ALTER TABLE events.event_registrations
                ADD CONSTRAINT chk_attendees_age_category_valid
                CHECK (
                    attendees IS NULL
                    OR NOT EXISTS (
                        SELECT 1
                        FROM jsonb_array_elements(attendees) elem
                        WHERE (elem->>'AgeCategory')::int NOT IN (1, 2)
                    )
                );
            ");

            // Add CHECK constraint: Attendee name must not be null or empty
            migrationBuilder.Sql(@"
                ALTER TABLE events.event_registrations
                ADD CONSTRAINT chk_attendees_name_not_empty
                CHECK (
                    attendees IS NULL
                    OR NOT EXISTS (
                        SELECT 1
                        FROM jsonb_array_elements(attendees) elem
                        WHERE elem->>'Name' IS NULL
                          OR TRIM(elem->>'Name') = ''
                    )
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE events.event_registrations
                DROP CONSTRAINT IF EXISTS chk_attendees_age_category_not_null;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE events.event_registrations
                DROP CONSTRAINT IF EXISTS chk_attendees_age_category_valid;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE events.event_registrations
                DROP CONSTRAINT IF EXISTS chk_attendees_name_not_empty;
            ");
        }
    }
}
```

**✅ Validation**:
- [ ] Migration file created
- [ ] Up() method adds 3 CHECK constraints
- [ ] Down() method removes constraints
- [ ] Migration compiles without errors

#### 4.2 Test Migration on Staging

```bash
# Apply migration to staging database
cd c:/Work/LankaConnect/src/LankaConnect.Infrastructure
dotnet ef database update --connection "$STAGING_DATABASE_URL"

# Verify constraints exist
psql $STAGING_DATABASE_URL -c "
SELECT
  conname as constraint_name,
  pg_get_constraintdef(oid) as definition
FROM pg_constraint
WHERE conrelid = 'events.event_registrations'::regclass
  AND conname LIKE 'chk_attendees%';
"
```

**Expected Output**: 3 constraints listed

**✅ Validation**:
- [ ] Migration applied successfully
- [ ] 3 CHECK constraints exist in database
- [ ] Constraints match migration definition

#### 4.3 Test Constraint Enforcement

**Test Script**: `scripts/test-phase-6a49-constraints.sql`
```sql
-- Test 1: Try to insert registration with null AgeCategory (should FAIL)
BEGIN;

INSERT INTO events.event_registrations (
  id, event_id, user_id, quantity, status, payment_status, attendees, created_at
) VALUES (
  gen_random_uuid(),
  (SELECT id FROM events.events LIMIT 1),
  (SELECT id FROM users.users LIMIT 1),
  1,
  0, -- Pending
  0, -- Pending
  '[{"Name": "Test", "AgeCategory": null}]'::jsonb,
  NOW()
);
-- Expected: ERROR - violates check constraint "chk_attendees_age_category_not_null"

ROLLBACK;

-- Test 2: Try to insert with invalid AgeCategory value (should FAIL)
BEGIN;

INSERT INTO events.event_registrations (
  id, event_id, user_id, quantity, status, payment_status, attendees, created_at
) VALUES (
  gen_random_uuid(),
  (SELECT id FROM events.events LIMIT 1),
  (SELECT id FROM users.users LIMIT 1),
  1,
  0,
  0,
  '[{"Name": "Test", "AgeCategory": 99}]'::jsonb, -- Invalid enum
  NOW()
);
-- Expected: ERROR - violates check constraint "chk_attendees_age_category_valid"

ROLLBACK;

-- Test 3: Insert valid data (should SUCCEED)
BEGIN;

INSERT INTO events.event_registrations (
  id, event_id, user_id, quantity, status, payment_status, attendees, created_at
) VALUES (
  gen_random_uuid(),
  (SELECT id FROM events.events LIMIT 1),
  (SELECT id FROM users.users LIMIT 1),
  1,
  0,
  0,
  '[{"Name": "Valid Attendee", "AgeCategory": 1, "Gender": 1}]'::jsonb,
  NOW()
);
-- Expected: SUCCESS

ROLLBACK;
```

**Run Tests**:
```bash
psql $STAGING_DATABASE_URL -f scripts/test-phase-6a49-constraints.sql
```

**Expected Output**:
- Test 1: `ERROR:  new row violates check constraint "chk_attendees_age_category_not_null"`
- Test 2: `ERROR:  new row violates check constraint "chk_attendees_age_category_valid"`
- Test 3: `INSERT 0 1` (success)

**✅ Validation**:
- [ ] Test 1 fails with correct constraint error
- [ ] Test 2 fails with correct constraint error
- [ ] Test 3 succeeds
- [ ] Constraints working as expected

#### 4.4 Commit Migration

```bash
git add src/LankaConnect.Infrastructure/Data/Migrations/*Phase6A49*
git add scripts/test-phase-6a49-constraints.sql
git add scripts/phase-6a49-backup-corrupt-records.sql
git add scripts/phase-6a49-cleanup-corrupt-attendees.sql

git commit -m "$(cat <<'EOF'
feat(phase-6a49): Add database constraints for JSONB attendee data integrity

Add CHECK constraints to prevent corrupt attendee data in JSONB:

Constraints Added:
1. chk_attendees_age_category_not_null
   - Ensures AgeCategory is never null, empty, or 0
   - Prevents future JSONB corruption

2. chk_attendees_age_category_valid
   - Ensures AgeCategory is valid enum (1=Adult, 2=Child)
   - Prevents invalid enum values

3. chk_attendees_name_not_empty
   - Ensures Name is not null or empty
   - Maintains data quality

Migration includes:
- Up: Add all 3 constraints
- Down: Remove constraints (rollback support)
- Test scripts to verify constraint enforcement

Database cleanup scripts:
- Backup corrupt records before cleanup
- Fix corrupt data by setting default AgeCategory
- Verification queries

Phase 6A.49 permanent fix for JSONB data integrity.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
EOF
)"
```

**✅ Validation**:
- [ ] Migration committed
- [ ] Scripts committed
- [ ] Commit message follows convention

---

### Phase 5: Monitoring & Documentation (1 hour)

**Objective**: Add observability and update all tracking documents.

#### 5.1 Add Data Quality Metrics

**Option A: Application-Level Logging** (Quick)
```csharp
// In GetUserRegistrationForEventQueryHandler.cs
public async Task<Result<RegistrationDetailsDto?>> Handle(...)
{
    var registration = await _context.Registrations
        .AsNoTracking()
        .Where(...)
        .Select(r => new RegistrationDetailsDto { ... })
        .FirstOrDefaultAsync(cancellationToken);

    // Log data quality metric
    if (registration?.Attendees.Any(a => a.AgeCategory == null) == true)
    {
        _logger.LogWarning(
            "[Phase 6A.49] Data Quality Issue: Registration {RegistrationId} has {Count} attendees with null AgeCategory",
            registration.Id,
            registration.Attendees.Count(a => a.AgeCategory == null)
        );
    }

    return Result<RegistrationDetailsDto?>.Success(registration);
}
```

**Option B: Database View** (Better for analytics)
```sql
-- Create view for data quality monitoring
CREATE OR REPLACE VIEW analytics.attendee_data_quality AS
SELECT
  DATE(r.created_at) as date,
  COUNT(DISTINCT r.id) as total_registrations,
  COUNT(DISTINCT CASE
    WHEN EXISTS (
      SELECT 1 FROM jsonb_array_elements(r.attendees) elem
      WHERE elem->>'AgeCategory' IS NULL
    )
    THEN r.id
  END) as registrations_with_null_age,
  SUM(jsonb_array_length(r.attendees)) as total_attendees,
  SUM((
    SELECT COUNT(*)
    FROM jsonb_array_elements(r.attendees) elem
    WHERE elem->>'AgeCategory' IS NULL
  )) as attendees_with_null_age,
  ROUND(
    100.0 * SUM((
      SELECT COUNT(*)
      FROM jsonb_array_elements(r.attendees) elem
      WHERE elem->>'AgeCategory' IS NULL
    )) / NULLIF(SUM(jsonb_array_length(r.attendees)), 0),
    2
  ) as null_age_percentage
FROM events.event_registrations r
WHERE r.created_at >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY DATE(r.created_at)
ORDER BY date DESC;

-- Query to monitor data quality
SELECT * FROM analytics.attendee_data_quality
WHERE null_age_percentage > 0;
```

**✅ Validation**:
- [ ] Logging added to critical query handlers
- [ ] Data quality view created
- [ ] Metrics queryable

#### 5.2 Update Documentation

**Document 1: PHASE_6A_MASTER_INDEX.md**
```markdown
| Phase | Feature | Status | Document | Implemented |
|-------|---------|--------|----------|-------------|
| 6A.48 | CSV Export Fixes | ✅ Complete | [PHASE_6A48_CSV_EXPORT_FIX_SUMMARY.md](./PHASE_6A48_CSV_EXPORT_FIX_SUMMARY.md) | 2025-12-25 |
| 6A.49 | JSONB Data Integrity Permanent Fix | ✅ Complete | [PHASE_6A49_JSONB_INTEGRITY_SUMMARY.md](./PHASE_6A49_JSONB_INTEGRITY_SUMMARY.md) | 2025-12-25 |
```

**Document 2: PROGRESS_TRACKER.md**
```markdown
### Continuation Session: Phase 6A.49 JSONB Data Integrity Permanent Fix - COMPLETE - 2025-12-25

**Status**: ✅ **COMPLETE** (Permanent fix deployed, constraints active)

**Summary**: Implemented comprehensive permanent fix for JSONB data integrity issues identified in Phase 6A.48. Added defensive code, database cleanup, CHECK constraints, and monitoring.

**What Was Done**:
1. ✅ Integration tests for corrupt JSONB data handling (TDD)
2. ✅ Defensive null checks in all 4 affected query handlers
3. ✅ Database cleanup: Fixed X corrupt registrations
4. ✅ CHECK constraints to prevent future corruption
5. ✅ Data quality monitoring view

**Files Modified**:
- Query Handlers: GetTicketQueryHandler, GetEventAttendeesQueryHandler, GetRegistrationByIdQueryHandler
- DTOs: TicketDto.cs (made TicketAttendeeDto.AgeCategory nullable)
- Tests: Added CorruptAttendeesIntegrationTests.cs
- Migration: AddAttendeeDataIntegrityConstraints_Phase6A49
- Scripts: Backup, cleanup, and constraint test scripts

**Commits**:
- [HASH1] test(phase-6a49): Add integration tests for corrupt JSONB attendee data
- [HASH2] fix(phase-6a49): Make TicketAttendeeDto.AgeCategory nullable
- [HASH3] fix(phase-6a49): Add defensive null checks for corrupt JSONB attendee data
- [HASH4] feat(phase-6a49): Add database constraints for JSONB attendee data integrity

**Deployment**: ✅ Azure Staging + Production (constraints active)

**Success Criteria Met**:
- ✅ All 4 endpoints return 200 OK with corrupt data
- ✅ Database cleanup: 0 corrupt records remaining
- ✅ CHECK constraints prevent new corruption
- ✅ 100% integration test coverage
- ✅ Zero build errors throughout implementation
```

**Document 3: STREAMLINED_ACTION_PLAN.md**
```markdown
## Recently Completed Actions

### Phase 6A.49: JSONB Data Integrity Permanent Fix ✅
- **Date**: 2025-12-25
- **Type**: Permanent Fix + Prevention
- **Approach**: TDD with database constraints
- **Result**: Corrupt data cleaned, future corruption prevented
- **Files**: 4 query handlers, 1 migration, 4 tests, 3 scripts
- **Status**: Complete and deployed
```

**Document 4: TASK_SYNCHRONIZATION_STRATEGY.md** (if needed)

**Create Summary**: `docs/PHASE_6A49_JSONB_INTEGRITY_SUMMARY.md`
```markdown
# Phase 6A.49: JSONB Data Integrity Permanent Fix - Summary

**Date**: 2025-12-25
**Status**: ✅ COMPLETE
**Category**: Data Integrity + Prevention

## Overview

Comprehensive permanent fix for JSONB data integrity issues where null AgeCategory values caused intermittent 500 errors. Implemented using TDD methodology with defensive code, database cleanup, CHECK constraints, and monitoring.

## Problem Statement

Phase 6A.48 identified that making DTOs nullable was a defensive workaround but didn't address root cause:
- Database contained corrupt JSONB with null AgeCategory
- No constraints prevented future corruption
- 4 endpoints affected, only 1 fixed in Phase 6A.48

## Solution Implemented

### 1. Detection & Analysis
- Identified X corrupt registrations
- Found 4 affected query handlers
- Documented corruption patterns

### 2. Defensive Code (TDD)
- Wrote 4 integration tests FIRST (RED)
- Implemented defensive null checks (GREEN)
- All tests pass (REFACTOR)

Query handlers fixed:
- GetUserRegistrationForEventQueryHandler ✅ (already done in 6A.48)
- GetTicketQueryHandler ✅
- GetEventAttendeesQueryHandler ✅
- GetRegistrationByIdQueryHandler ✅

### 3. Database Cleanup
- Backed up X corrupt registrations
- Fixed corrupt data: Set AgeCategory = Adult (default)
- Verified: 0 corrupt records remaining

### 4. Database Constraints
- chk_attendees_age_category_not_null: Prevents null values
- chk_attendees_age_category_valid: Ensures valid enum (1 or 2)
- chk_attendees_name_not_empty: Prevents empty names

### 5. Monitoring
- Added data quality logging
- Created analytics.attendee_data_quality view
- Tracks null percentage over time

## Technical Details

**DTOs Made Nullable**:
- AttendeeDetailsDto.AgeCategory (Phase 6A.48)
- TicketAttendeeDto.AgeCategory (Phase 6A.49)

**Defensive Patterns**:
```csharp
// Pattern: Filter null attendees before mapping
.Where(a => a != null)
.Select(a => new AttendeeDetailsDto {
    Name = a.Name ?? "Unknown",
    AgeCategory = a.AgeCategory, // Safe: DTO is nullable
    Gender = a.Gender
})

// Pattern: Null-safe counting
var adultCount = registration.Attendees
    .Count(a => a?.AgeCategory == AgeCategory.Adult);
```

**Database Constraints**:
```sql
ALTER TABLE events.event_registrations
ADD CONSTRAINT chk_attendees_age_category_not_null
CHECK (
    attendees IS NULL
    OR NOT EXISTS (
        SELECT 1 FROM jsonb_array_elements(attendees) elem
        WHERE elem->>'AgeCategory' IS NULL
    )
);
```

## Files Changed

**Application Layer**:
- TicketDto.cs
- GetTicketQueryHandler.cs
- GetEventAttendeesQueryHandler.cs
- GetRegistrationByIdQueryHandler.cs

**Tests**:
- CorruptAttendeesIntegrationTests.cs (new)

**Infrastructure**:
- Migration: AddAttendeeDataIntegrityConstraints_Phase6A49

**Scripts**:
- phase-6a49-backup-corrupt-records.sql
- phase-6a49-cleanup-corrupt-attendees.sql
- test-phase-6a49-constraints.sql

## Deployment

**Staging**:
- Database cleanup: YYYY-MM-DD HH:MM
- Code deployment: GitHub Actions Run XXXXXXX
- Constraints applied: YYYY-MM-DD HH:MM

**Production**:
- Scheduled for: TBD
- Approval: Required
- Downtime: None (migrations are non-blocking)

## Verification

**Test Results**:
- ✅ All 4 integration tests pass
- ✅ Constraint tests pass
- ✅ Manual API testing: 5/5 successful
- ✅ No regressions in existing tests

**Database Verification**:
```sql
-- Should return 0
SELECT COUNT(*) FROM events.event_registrations r
WHERE EXISTS (
  SELECT 1 FROM jsonb_array_elements(r.attendees) elem
  WHERE elem->>'AgeCategory' IS NULL
);
```

**Monitoring Query**:
```sql
SELECT * FROM analytics.attendee_data_quality
WHERE null_age_percentage > 0;
-- Should return 0 rows after cleanup
```

## Impact

**Before**:
- Intermittent 500 errors on 4 endpoints
- X registrations with corrupt data
- No prevention mechanism

**After**:
- All endpoints return 200 OK
- 0 corrupt registrations
- Database constraints prevent future issues
- 100% test coverage

## Lessons Learned

1. **Defensive DTOs alone are insufficient** - Need database-level enforcement
2. **TDD catches regressions** - Tests found issues in GetTicket we missed
3. **Database constraints are essential** - Application validation isn't enough
4. **Monitoring enables detection** - Data quality view helps catch future issues

## Next Steps

**Phase 6A.50** (if needed):
- Monitor data quality metrics for 1 week
- Investigate root cause of original corruption
- Consider additional JSONB validation rules

**Related Issues**:
- None currently open

**Dependencies**:
- Phase 6A.48 (prerequisite)
- Phase 6A.47 (related: AsNoTracking pattern)

---

**Related Documents**:
- [RCA: Phase 6A.48](./REGISTRATION_STATE_FLIPPING_RCA.md)
- [Master Index](./PHASE_6A_MASTER_INDEX.md)
- [Progress Tracker](./PROGRESS_TRACKER.md)
```

**✅ Validation**:
- [ ] All 4 documentation files updated
- [ ] Summary document created
- [ ] Links verified
- [ ] Status accurate

---

## Deployment Strategy

### Deployment Sequence

**Goal**: Zero downtime, safe rollout with rollback capability.

**Sequence**:
```
1. Deploy Code Changes (Phase 2)
   ↓
2. Database Cleanup (Phase 3) - STAGING ONLY
   ↓
3. Deploy Database Constraints (Phase 4) - STAGING ONLY
   ↓
4. Verify Staging for 24 hours
   ↓
5. Deploy to Production (Code → Cleanup → Constraints)
```

### Staging Deployment

**Step 1: Deploy Code Changes**
```bash
# Merge to develop branch
git checkout develop
git merge feature/phase-6a49-jsonb-integrity
git push origin develop

# GitHub Actions will deploy to staging automatically
# Monitor: https://github.com/your-org/your-repo/actions
```

**Wait for deployment**: ~5 minutes

**Step 2: Database Cleanup**
```bash
# Connect to staging database
psql $STAGING_DATABASE_URL

# Run backup
\i scripts/phase-6a49-backup-corrupt-records.sql

# Verify backup
SELECT COUNT(*) FROM backups.corrupt_registrations_20251225;

# Run cleanup (Option B: Fix with default)
\i scripts/phase-6a49-cleanup-corrupt-attendees.sql

# Verify cleanup
SELECT COUNT(*) FROM events.event_registrations r
WHERE EXISTS (
  SELECT 1 FROM jsonb_array_elements(r.attendees) elem
  WHERE elem->>'AgeCategory' IS NULL
);
-- Expected: 0
```

**Step 3: Deploy Constraints**
```bash
# Apply migration
cd c:/Work/LankaConnect/src/LankaConnect.Infrastructure
dotnet ef database update --connection "$STAGING_DATABASE_URL"

# Verify constraints
psql $STAGING_DATABASE_URL -c "
SELECT conname, pg_get_constraintdef(oid)
FROM pg_constraint
WHERE conrelid = 'events.event_registrations'::regclass
  AND conname LIKE 'chk_attendees%';
"
```

**Step 4: Test Staging**
```bash
# Test all 4 endpoints
./scripts/test-phase-6a47-apis.ps1

# Test resend email (previously failed)
# Go to staging UI → My Registrations → Resend Email

# Monitor logs for errors
az containerapp logs show --name lankaconnect-staging-api --follow
```

**✅ Staging Validation**:
- [ ] Code deployed successfully
- [ ] Database cleanup: 0 corrupt records
- [ ] Constraints active and working
- [ ] All 4 endpoints return 200 OK
- [ ] No errors in logs
- [ ] Wait 24 hours for monitoring

### Production Deployment

**Prerequisites**:
- [ ] Staging tested for 24 hours minimum
- [ ] No new issues reported
- [ ] Approval from product owner
- [ ] Backup of production database created
- [ ] Rollback plan reviewed

**Step 1: Merge to Master**
```bash
git checkout master
git merge develop
git push origin master
```

**Step 2: Deploy Code** (GitHub Actions auto-deploys)

**Step 3: Database Cleanup** (During low-traffic window)
```bash
# Schedule for 2 AM UTC (low traffic)
psql $PRODUCTION_DATABASE_URL -f scripts/phase-6a49-backup-corrupt-records.sql
psql $PRODUCTION_DATABASE_URL -f scripts/phase-6a49-cleanup-corrupt-attendees.sql
```

**Step 4: Deploy Constraints**
```bash
dotnet ef database update --connection "$PRODUCTION_DATABASE_URL"
```

**Step 5: Monitor**
```bash
# Watch for constraint violations
az containerapp logs show --name lankaconnect-production-api --follow \
  --filter "violates check constraint"

# Monitor error rate
# Check Application Insights dashboard
```

**✅ Production Validation**:
- [ ] Code deployed successfully
- [ ] Database cleanup completed
- [ ] Constraints active
- [ ] No increase in error rate
- [ ] User verification: Registration flipping fixed

### Timing Considerations

**Staging**:
- Deploy anytime (non-production)
- Cleanup can run during business hours
- Monitor for 24 hours minimum

**Production**:
- Code deploy: Anytime (backwards compatible)
- Database cleanup: 2 AM UTC (low traffic)
- Constraint deploy: Immediately after cleanup
- Total downtime: 0 (all operations are non-blocking)

**Why This Order**:
1. Code changes are defensive → Safe to deploy first
2. Cleanup must happen before constraints → Constraints will fail if corrupt data exists
3. Constraints prevent new corruption → Deploy immediately after cleanup

---

## Validation Checklist

### Phase 1 Validation
- [ ] Database forensics queries executed
- [ ] Corrupt record count documented
- [ ] All 4 affected files identified
- [ ] Reusable patterns found

### Phase 2 Validation
- [ ] All 4 integration tests written
- [ ] Tests compile without errors
- [ ] Tests FAIL initially (RED phase)
- [ ] All 4 query handlers updated with null checks
- [ ] Code compiles with 0 errors/warnings
- [ ] Tests PASS after changes (GREEN phase)
- [ ] No regressions in existing tests
- [ ] 3 commits created with proper messages

### Phase 3 Validation
- [ ] Backup table created successfully
- [ ] Backup contains all corrupt records
- [ ] Cleanup script tested (dry run)
- [ ] Cleanup executed successfully
- [ ] Verification shows 0 corrupt records
- [ ] No data loss confirmed

### Phase 4 Validation
- [ ] Migration created successfully
- [ ] Migration compiles without errors
- [ ] Migration tested on staging
- [ ] 3 CHECK constraints exist
- [ ] Constraint tests PASS
- [ ] Constraints prevent null values
- [ ] Constraints allow valid values

### Phase 5 Validation
- [ ] Data quality logging added
- [ ] Monitoring view created
- [ ] PHASE_6A_MASTER_INDEX.md updated
- [ ] PROGRESS_TRACKER.md updated
- [ ] STREAMLINED_ACTION_PLAN.md updated
- [ ] Summary document created
- [ ] All links verified

### Deployment Validation
- [ ] Staging: Code deployed
- [ ] Staging: Cleanup completed
- [ ] Staging: Constraints active
- [ ] Staging: 24 hour monitoring clean
- [ ] Production: Code deployed
- [ ] Production: Cleanup completed
- [ ] Production: Constraints active
- [ ] Production: User verification successful

---

## Success Criteria

### Functional Success
1. ✅ All 4 endpoints return 200 OK even with historically corrupt data
2. ✅ GetUserRegistrationForEvent handles null AgeCategory
3. ✅ GetTicket handles null AgeCategory
4. ✅ GetEventAttendees calculates counts correctly with nulls
5. ✅ GetRegistrationById returns data with null AgeCategory

### Data Integrity Success
1. ✅ 0 corrupt records remain after cleanup
2. ✅ Database constraints prevent new null values
3. ✅ Database constraints prevent invalid enum values
4. ✅ Backup of corrupt data preserved
5. ✅ No legitimate registrations deleted

### Technical Success
1. ✅ Zero build errors throughout implementation
2. ✅ 100% test coverage for null scenarios
3. ✅ All integration tests pass
4. ✅ No regressions in existing functionality
5. ✅ Code follows defensive programming patterns

### Process Success
1. ✅ TDD methodology followed (RED → GREEN → REFACTOR)
2. ✅ All commits follow project conventions
3. ✅ All 3 PRIMARY docs updated
4. ✅ Summary document created
5. ✅ Phase number properly tracked in master index

### Deployment Success
1. ✅ Staging deployment successful
2. ✅ 24 hour monitoring shows no issues
3. ✅ Production deployment successful
4. ✅ Zero downtime achieved
5. ✅ User confirms registration flipping fixed

---

## Risk Assessment

### High Risks

**Risk 1: Database Cleanup Deletes Valid Data**
- **Likelihood**: Low
- **Impact**: High
- **Mitigation**:
  - Backup ALL corrupt records before cleanup
  - Use UPDATE instead of DELETE (Option B)
  - Test on staging first
  - Manual review of sample records before production
- **Rollback**: Restore from backup table

**Risk 2: CHECK Constraints Block Legitimate Registrations**
- **Likelihood**: Low
- **Impact**: High
- **Mitigation**:
  - Deploy constraints AFTER cleanup verified
  - Test constraints on staging for 24 hours
  - Monitor staging for constraint violations
  - Application code already validates before INSERT
- **Rollback**: Remove constraints (migration Down())

**Risk 3: Performance Degradation from JSONB Constraint Checks**
- **Likelihood**: Medium
- **Impact**: Low
- **Mitigation**:
  - Constraints only checked on INSERT/UPDATE
  - Modern PostgreSQL handles JSONB checks efficiently
  - Monitor query performance after deployment
- **Rollback**: Remove constraints if performance issue confirmed

### Medium Risks

**Risk 4: Null Checks Miss Edge Cases**
- **Likelihood**: Medium
- **Impact**: Medium
- **Mitigation**:
  - Comprehensive integration tests
  - TDD approach catches edge cases
  - 24 hour staging monitoring
- **Rollback**: Revert code changes

**Risk 5: Migration Fails on Production**
- **Likelihood**: Low
- **Impact**: Medium
- **Mitigation**:
  - Test migration on staging first
  - Migrations are idempotent (can re-run)
  - Use transaction for migration
- **Rollback**: Run Down() migration

### Low Risks

**Risk 6: Documentation Out of Sync**
- **Likelihood**: Low
- **Impact**: Low
- **Mitigation**:
  - Update all 3 PRIMARY docs in single commit
  - Use checklist to verify updates
- **Rollback**: Update docs post-deployment

---

## Rollback Procedures

### Rollback Phase 2 (Code Changes)

**When**: If code changes cause new errors in production

**Procedure**:
```bash
# Revert to previous commit
git revert <commit-hash-phase-2>
git push origin master

# GitHub Actions will auto-deploy revert
# Monitor deployment
```

**Validation**:
- [ ] Previous version deployed
- [ ] Error rate returns to normal
- [ ] No new issues introduced

### Rollback Phase 3 (Database Cleanup)

**When**: If cleanup deleted wrong data or corrupted records

**Procedure**:
```sql
-- Restore from backup table
BEGIN;

-- Delete incorrectly cleaned records (if any)
DELETE FROM events.event_registrations
WHERE id IN (SELECT id FROM backups.corrupt_registrations_20251225);

-- Restore original corrupt data
INSERT INTO events.event_registrations (
  id, event_id, user_id, quantity, status, payment_status,
  attendees, contact_email, contact_phone, contact_address,
  created_at, updated_at
)
SELECT
  id, event_id, user_id, quantity, status, payment_status,
  attendees, contact_email, contact_phone, contact_address,
  created_at, updated_at
FROM backups.corrupt_registrations_20251225;

COMMIT;
```

**Validation**:
- [ ] Backup count matches restored count
- [ ] User data restored
- [ ] No data loss

### Rollback Phase 4 (Database Constraints)

**When**: If constraints block legitimate registrations

**Procedure**:
```bash
# Run Down() migration
cd c:/Work/LankaConnect/src/LankaConnect.Infrastructure
dotnet ef migrations remove --force

# Or manually remove constraints
psql $DATABASE_URL -c "
ALTER TABLE events.event_registrations
DROP CONSTRAINT IF EXISTS chk_attendees_age_category_not_null;

ALTER TABLE events.event_registrations
DROP CONSTRAINT IF EXISTS chk_attendees_age_category_valid;

ALTER TABLE events.event_registrations
DROP CONSTRAINT IF EXISTS chk_attendees_name_not_empty;
"
```

**Validation**:
- [ ] Constraints removed
- [ ] Registrations working normally
- [ ] No lingering constraint errors

### Full Rollback

**When**: Critical issue requires complete rollback

**Procedure**:
1. Rollback code (Phase 2)
2. Rollback constraints (Phase 4)
3. Rollback cleanup (Phase 3)
4. Verify system restored to pre-implementation state

**Validation**:
- [ ] All changes reverted
- [ ] System functioning as before
- [ ] Incident report created

---

## Documentation Updates

### Files to Update

**PRIMARY Tracking Documents**:
1. `docs/PHASE_6A_MASTER_INDEX.md`
   - Add Phase 6A.49 entry
   - Link to summary document
   - Mark status as Complete

2. `docs/PROGRESS_TRACKER.md`
   - Add continuation session for Phase 6A.49
   - List all commits
   - Document deployment status
   - Add "Next Steps" section

3. `docs/STREAMLINED_ACTION_PLAN.md`
   - Add Phase 6A.49 to completed actions
   - Update current priorities if needed

**Summary Document**:
4. `docs/PHASE_6A49_JSONB_INTEGRITY_SUMMARY.md`
   - Comprehensive summary (created above)
   - Technical details
   - Files changed
   - Verification steps

**Optional Documentation**:
5. `docs/JSONB_DATA_INTEGRITY_BEST_PRACTICES.md`
   - Guidelines for future JSONB usage
   - Defensive patterns from this implementation
   - Constraint recommendations

### Update Checklist

- [ ] PHASE_6A_MASTER_INDEX.md updated with Phase 6A.49
- [ ] PROGRESS_TRACKER.md has continuation session
- [ ] STREAMLINED_ACTION_PLAN.md shows Phase 6A.49 complete
- [ ] Summary document created
- [ ] All cross-references verified
- [ ] Commit hashes added after implementation
- [ ] Deployment dates/times added
- [ ] Build status verified

---

## Next Steps After Completion

### Immediate (Within 24 hours)
1. ✅ Monitor staging for constraint violations
2. ✅ Verify all 4 endpoints respond correctly
3. ✅ Check data quality view shows 0% null rate
4. ✅ User confirmation: Registration flipping fixed

### Short-term (Within 1 week)
1. Deploy to production (following deployment strategy)
2. Monitor production data quality metrics
3. Investigate root cause of original corruption (separate task)
4. Document best practices for JSONB usage

### Long-term (Within 1 month)
1. Review all other JSONB columns for similar issues
2. Add CHECK constraints to other JSONB columns if needed
3. Create automated data quality tests
4. Consider adding JSON Schema validation at application level

### Phase 6A.50 (If Needed)
If monitoring reveals additional issues:
- Investigate root cause of original corruption
- Add JSON Schema validation to prevent client-side corruption
- Enhanced logging for JSONB operations
- Automated data quality alerts

---

## Appendix A: Code Patterns

### Pattern 1: Defensive DTO Mapping
```csharp
// Before (crashes on null):
Attendees = r.Attendees.Select(a => new AttendeeDetailsDto
{
    Name = a.Name,
    AgeCategory = a.AgeCategory,
    Gender = a.Gender
}).ToList()

// After (null-safe):
Attendees = r.Attendees
    .Where(a => a != null)
    .Select(a => new AttendeeDetailsDto
    {
        Name = a.Name ?? "Unknown",
        AgeCategory = a.AgeCategory, // Safe: DTO is nullable
        Gender = a.Gender
    }).ToList()
```

### Pattern 2: Null-Safe Counting
```csharp
// Before (crashes on null):
var adultCount = registration.Attendees.Count(a => a.AgeCategory == AgeCategory.Adult);

// After (null-safe):
var adultCount = registration.Attendees.Count(a => a?.AgeCategory == AgeCategory.Adult);
```

### Pattern 3: JSONB CHECK Constraint
```sql
-- Prevent null values in JSONB
ALTER TABLE events.event_registrations
ADD CONSTRAINT chk_attendees_age_category_not_null
CHECK (
    attendees IS NULL
    OR NOT EXISTS (
        SELECT 1 FROM jsonb_array_elements(attendees) elem
        WHERE elem->>'AgeCategory' IS NULL
    )
);
```

### Pattern 4: Data Quality Monitoring
```sql
-- Create view for tracking null percentages
CREATE VIEW analytics.attendee_data_quality AS
SELECT
  DATE(created_at) as date,
  COUNT(*) as total_registrations,
  ROUND(
    100.0 * SUM(CASE WHEN ... THEN 1 END) / COUNT(*),
    2
  ) as null_percentage
FROM events.event_registrations
GROUP BY DATE(created_at);
```

---

## Appendix B: Testing Scenarios

### Integration Test Scenarios

**Scenario 1: Null AgeCategory**
- Registration with single attendee, AgeCategory = null
- Expected: DTO.AgeCategory = null, no crash

**Scenario 2: Mixed Valid/Null**
- Registration with 2 attendees: One valid, one null AgeCategory
- Expected: Adult count = 1 (or 0), no crash

**Scenario 3: All Null**
- Registration with all attendees having null AgeCategory
- Expected: Adult count = 0, Child count = 0, no crash

**Scenario 4: Null Attendee Object**
- JSONB array contains null element: `[{...}, null, {...}]`
- Expected: Filtered out, no crash

**Scenario 5: Empty JSONB Array**
- `attendees = []`
- Expected: Empty Attendees list, no crash

### Manual Test Scenarios

**Scenario A: My Registration Page**
- User with corrupt registration
- Load `/my-registration`
- Expected: 200 OK, data displayed with null age

**Scenario B: Resend Email Button**
- Click resend on corrupt registration
- Expected: Email sent successfully

**Scenario C: Event Attendees Export**
- Export attendees for event with corrupt data
- Expected: CSV/Excel downloaded, corrupt rows included

**Scenario D: Ticket Download**
- Download ticket for registration with null AgeCategory
- Expected: PDF generated successfully

---

## Appendix C: SQL Queries

### Query 1: Find Corrupt Records
```sql
SELECT
  r.id,
  r.event_id,
  r.created_at,
  jsonb_array_length(r.attendees) as total,
  (
    SELECT COUNT(*)
    FROM jsonb_array_elements(r.attendees) elem
    WHERE elem->>'AgeCategory' IS NULL
  ) as corrupt
FROM events.event_registrations r
WHERE EXISTS (
  SELECT 1 FROM jsonb_array_elements(r.attendees) elem
  WHERE elem->>'AgeCategory' IS NULL
);
```

### Query 2: Verify Cleanup
```sql
SELECT COUNT(*) as corrupt_remaining
FROM events.event_registrations r
WHERE EXISTS (
  SELECT 1 FROM jsonb_array_elements(r.attendees) elem
  WHERE elem->>'AgeCategory' IS NULL
);
-- Expected: 0
```

### Query 3: Check Constraints
```sql
SELECT
  conname as constraint_name,
  pg_get_constraintdef(oid) as definition
FROM pg_constraint
WHERE conrelid = 'events.event_registrations'::regclass
  AND contype = 'c'
  AND conname LIKE 'chk_attendees%';
```

### Query 4: Data Quality Over Time
```sql
SELECT * FROM analytics.attendee_data_quality
WHERE date >= CURRENT_DATE - INTERVAL '7 days'
ORDER BY date DESC;
```

---

**Document Status**: ✅ ARCHITECT APPROVED
**Next Action**: Begin Phase 1 (Detection & Analysis)
**Estimated Time**: 9 hours total (1 + 4 + 2 + 1 + 1)
**Dependencies**: None (Phase 6A.48 already deployed)
**Approval Required**: Database cleanup and production deployment

---

**References**:
- [Phase 6A.48 Fix](./REGISTRATION_STATE_FLIPPING_RCA.md)
- [Phase 6A.47 AsNoTracking Fix](./MY_REGISTRATION_500_ERROR_RCA.md)
- [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md)
- [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md)
