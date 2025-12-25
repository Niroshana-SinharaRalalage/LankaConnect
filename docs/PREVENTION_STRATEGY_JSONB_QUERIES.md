# Prevention Strategy: JSONB Query and Projection Issues

**Date**: 2025-12-24
**Purpose**: Prevent future 500 errors related to EF Core JSONB queries
**Scope**: All queries involving JSONB-backed owned collections (Attendees, Contact, etc.)

---

## Overview

This document provides strategies to prevent issues similar to the `/my-registration` 500 error, which was caused by querying JSONB-backed collections in EF Core projections.

---

## Root Causes to Prevent

1. **Null Handling in JSONB Projections** - LINQ operations on potentially null JSONB collections
2. **EF Core Translation Failures** - Queries that cannot translate to PostgreSQL SQL
3. **Schema Mismatches** - JSONB structure in database differs from EF Core expectations
4. **Deployment Gaps** - Code fixes not deployed to production environments
5. **Insufficient Testing** - Integration tests don't cover JSONB query edge cases

---

## Strategy 1: Defensive JSONB Query Patterns

### Pattern 1A: Always Check for Null Before LINQ Operations

**WRONG**:
```csharp
// This will throw NullReferenceException if Attendees is null
Attendees = r.Attendees.Select(a => new AttendeeDetailsDto { ... }).ToList()
```

**RIGHT**:
```csharp
// Always use null-conditional operator or ternary
Attendees = r.Attendees?.Select(a => new AttendeeDetailsDto { ... }).ToList()
            ?? new List<AttendeeDetailsDto>()

// Or explicit null check
Attendees = r.Attendees != null
    ? r.Attendees.Select(a => new AttendeeDetailsDto { ... }).ToList()
    : new List<AttendeeDetailsDto>()
```

### Pattern 1B: Materialize Before Complex Projections

**WRONG** (Complex projection in SQL):
```csharp
var dto = await _context.Registrations
    .Where(r => r.EventId == eventId)
    .Select(r => new RegistrationDetailsDto
    {
        Id = r.Id,
        Attendees = r.Attendees?.Select(a => new AttendeeDetailsDto
        {
            Name = a.Name,
            AgeCategory = a.AgeCategory,
            Gender = a.Gender
        }).ToList() ?? new List<AttendeeDetailsDto>()
    })
    .FirstOrDefaultAsync();
```

**RIGHT** (Materialize then project):
```csharp
// Step 1: Fetch entity (EF Core handles JSONB deserialization)
var registration = await _context.Registrations
    .Where(r => r.EventId == eventId)
    .FirstOrDefaultAsync();

if (registration == null)
    return null;

// Step 2: Project in memory (no SQL translation needed)
var dto = new RegistrationDetailsDto
{
    Id = registration.Id,
    Attendees = registration.Attendees?.Select(a => new AttendeeDetailsDto
    {
        Name = a.Name,
        AgeCategory = a.AgeCategory,
        Gender = a.Gender
    }).ToList() ?? new List<AttendeeDetailsDto>()
};

return dto;
```

**Benefits**:
- EF Core only needs to deserialize JSONB (well-tested path)
- Projection happens in C# (no SQL translation failures)
- Easier to debug
- More explicit control flow

**When to Use**:
- JSONB collections with nested projections
- Complex LINQ operations on JSONB data
- When debugging SQL translation issues

**When NOT to Use**:
- Simple scalar JSONB properties (Contact.Email)
- When fetching large result sets (projection in SQL reduces data transfer)

### Pattern 1C: Use Defensive Column Mappings

**EF Core Configuration**:
```csharp
// ALWAYS explicitly map JSONB properties
builder.OwnsMany(r => r.Attendees, attendeesBuilder =>
{
    attendeesBuilder.ToJson("attendees");

    // Explicit column mappings prevent SQL generation errors
    attendeesBuilder.Property(a => a.Name)
        .HasColumnName("name");

    attendeesBuilder.Property(a => a.AgeCategory)
        .HasColumnName("age_category")
        .HasConversion<string>(); // Explicit conversion for enums

    attendeesBuilder.Property(a => a.Gender)
        .HasColumnName("gender")
        .HasConversion<string?>()
        .IsRequired(false); // Explicit nullable
});
```

---

## Strategy 2: Comprehensive Testing for JSONB Queries

### Test Coverage Requirements

**For every query handler that uses JSONB**:

1. **Null Collection Test** - Test with null JSONB collection (legacy data)
2. **Empty Collection Test** - Test with empty JSONB array `[]`
3. **Single Item Test** - Test with one item in JSONB array
4. **Multiple Items Test** - Test with 5-10 items
5. **Null Properties Test** - Test JSONB objects with null properties (e.g., Gender)
6. **Anonymous vs Authenticated Test** - Test both user types
7. **Integration Test** - Test against real PostgreSQL database

### Example Test Suite

**File**: `tests/LankaConnect.Application.Tests/Events/Queries/GetUserRegistrationForEventQueryHandlerTests.cs`

```csharp
public class GetUserRegistrationForEventQueryHandlerTests : IClassFixture<PostgresTestFixture>
{
    private readonly IApplicationDbContext _context;

    [Fact]
    public async Task Handle_WithNullAttendees_ReturnsEmptyList()
    {
        // Arrange: Create legacy registration (before multi-attendee feature)
        var @event = CreateTestEvent();
        var registration = Registration.Create(
            @event.Id,
            Guid.NewGuid(),
            quantity: 5
        ).Value;

        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        // Act
        var handler = new GetUserRegistrationForEventQueryHandler(_context);
        var result = await handler.Handle(
            new GetUserRegistrationForEventQuery(@event.Id, registration.UserId.Value),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Attendees.Should().NotBeNull();
        result.Value.Attendees.Should().BeEmpty(); // Null JSONB → empty list
    }

    [Fact]
    public async Task Handle_WithMultipleAttendees_ReturnsAllAttendees()
    {
        // Arrange: Create multi-attendee registration
        var @event = CreateTestEvent();
        var attendees = new List<AttendeeDetails>
        {
            AttendeeDetails.Create("John Doe", AgeCategory.Adult, Gender.Male).Value,
            AttendeeDetails.Create("Jane Doe", AgeCategory.Child, null).Value,
            AttendeeDetails.Create("Bob Smith", AgeCategory.Senior, Gender.Male).Value
        };

        var contact = RegistrationContact.Create(
            "test@example.com",
            "+1234567890",
            "123 Main St").Value;

        var registration = Registration.CreateWithAttendees(
            @event.Id,
            Guid.NewGuid(),
            attendees,
            contact,
            Money.Create(100, Currency.USD).Value,
            isPaidEvent: false
        ).Value;

        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        // Act
        var handler = new GetUserRegistrationForEventQueryHandler(_context);
        var result = await handler.Handle(
            new GetUserRegistrationForEventQuery(@event.Id, registration.UserId.Value),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Attendees.Should().HaveCount(3);
        result.Value.Attendees[0].Name.Should().Be("John Doe");
        result.Value.Attendees[0].AgeCategory.Should().Be(AgeCategory.Adult);
        result.Value.Attendees[1].Gender.Should().BeNull(); // Test null Gender
    }

    [Fact]
    public async Task Handle_WithAnonymousRegistration_LoadsContactInfo()
    {
        // Arrange: Anonymous registration
        var @event = CreateTestEvent();
        var attendees = new List<AttendeeDetails>
        {
            AttendeeDetails.Create("Anonymous User", AgeCategory.Adult, null).Value
        };

        var contact = RegistrationContact.Create(
            "anonymous@example.com",
            "+9876543210",
            "456 Oak Ave").Value;

        var registration = Registration.CreateWithAttendees(
            @event.Id,
            userId: null, // Anonymous
            attendees,
            contact,
            Money.Create(50, Currency.USD).Value,
            isPaidEvent: true
        ).Value;

        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        // Act
        var handler = new GetUserRegistrationForEventQueryHandler(_context);
        var result = await handler.Handle(
            new GetUserRegistrationForEventQuery(@event.Id, null), // Anonymous query
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ContactEmail.Should().Be("anonymous@example.com");
        result.Value.ContactPhone.Should().Be("+9876543210");
        result.Value.UserId.Should().BeNull();
    }
}
```

### Integration Test Requirements

**Minimum Coverage**:
- [ ] All query handlers that use JSONB collections
- [ ] All scenarios: null, empty, single, multiple items
- [ ] All user types: authenticated, anonymous
- [ ] All payment types: free, paid, pending
- [ ] All registration statuses: Confirmed, Pending, Cancelled

---

## Strategy 3: Database Schema Management

### 3.1: Migration Hygiene

**Rule 1**: NEVER delete JSONB columns in migrations (breaks backward compatibility)

**Rule 2**: When adding new JSONB properties, make them nullable initially

**Example**:
```csharp
// Phase 1: Add nullable property
attendeesBuilder.Property(a => a.NewProperty)
    .HasColumnName("new_property")
    .IsRequired(false); // Start as nullable

// Phase 2 (later): Backfill data with migration script

// Phase 3 (after backfill): Make required
attendeesBuilder.Property(a => a.NewProperty)
    .HasColumnName("new_property")
    .IsRequired(true);
```

**Rule 3**: Always include data migration scripts for JSONB structure changes

**Example Data Migration**:
```sql
-- Update all registrations to add Gender field (set to null)
UPDATE registrations
SET attendees = (
  SELECT jsonb_agg(
    jsonb_set(elem, '{gender}', 'null'::jsonb)
  )
  FROM jsonb_array_elements(attendees) elem
)
WHERE attendees IS NOT NULL
  AND attendees->0->>'gender' IS NULL;
```

### 3.2: Schema Verification in Deployment Pipeline

**Add to CI/CD**:
```bash
# After deployment, verify schema matches code
dotnet ef migrations has-pending-model-changes --project Infrastructure

# If migrations pending, BLOCK deployment
if [ $? -ne 0 ]; then
  echo "ERROR: Database schema out of sync with code"
  exit 1
fi
```

---

## Strategy 4: Deployment Safety Checks

### 4.1: Pre-Deployment Checklist

Before deploying code that changes JSONB queries:

- [ ] Run all integration tests locally against staging database
- [ ] Verify migrations applied to staging database
- [ ] Check staging database has same JSONB schema as production
- [ ] Test query handler manually against staging database
- [ ] Review EF Core generated SQL (enable logging)
- [ ] Check for null handling in all JSONB projections

### 4.2: Post-Deployment Monitoring

**Monitor these metrics for 30 minutes after deployment**:

1. **Error Rate** - Watch for 500 errors
   ```kusto
   ContainerAppConsoleLogs_CL
   | where TimeGenerated > ago(30m)
   | where Log_s contains "500" or Log_s contains "Exception"
   | summarize count() by bin(TimeGenerated, 1m)
   ```

2. **Query Handler Exceptions**
   ```kusto
   ContainerAppConsoleLogs_CL
   | where TimeGenerated > ago(30m)
   | where Log_s contains "GetUserRegistrationForEventQueryHandler"
   | where Log_s contains "Exception" or Log_s contains "Error"
   ```

3. **Response Times**
   ```kusto
   AppMetrics
   | where TimeGenerated > ago(30m)
   | where Name == "GET /api/proxy/events/{id}/my-registration"
   | summarize avg(Value), max(Value) by bin(TimeGenerated, 1m)
   ```

### 4.3: Canary Deployments

For high-risk JSONB query changes:

1. Deploy to 10% of traffic first
2. Monitor error rates for 15 minutes
3. If error rate < 0.1%, deploy to 50%
4. Monitor for another 15 minutes
5. Deploy to 100%

**Rollback Trigger**: Error rate > 1% or response time > 2 seconds

---

## Strategy 5: Documentation and Code Review

### 5.1: Code Review Checklist for JSONB Queries

When reviewing code that queries JSONB collections:

- [ ] Null check before any LINQ operations on JSONB collections
- [ ] No complex projections in SQL (materialize first if needed)
- [ ] Explicit column mappings in EF Core configuration
- [ ] Integration tests cover null/empty/multiple scenarios
- [ ] Migration script included if JSONB structure changes
- [ ] Logging added for debugging query failures

### 5.2: Development Guidelines Document

**Create**: `docs/DEVELOPMENT_GUIDELINES_JSONB.md`

**Contents**:
1. When to use JSONB vs. separate tables
2. How to configure OwnsOne/OwnsMany for JSONB
3. Query patterns that work vs. fail with JSONB
4. Testing requirements for JSONB queries
5. Migration patterns for JSONB schema changes
6. Debugging tips for EF Core JSONB issues

### 5.3: Architecture Decision Record (ADR)

**Create**: `docs/ADR_JSONB_QUERY_PATTERNS.md`

**Purpose**: Document architectural decisions around JSONB usage

**Key Decisions**:
1. **When to materialize vs. project** - Guideline on query complexity threshold
2. **Null handling strategy** - Always use null-conditional or ternary
3. **Testing requirements** - Minimum 5 test scenarios for JSONB queries
4. **Migration strategy** - 3-phase approach for schema changes

---

## Strategy 6: Monitoring and Alerting

### 6.1: Application Insights Custom Metrics

**Add telemetry for JSONB queries**:
```csharp
public async Task<Result<RegistrationDetailsDto?>> Handle(...)
{
    using var operation = _telemetryClient.StartOperation<DependencyTelemetry>("JSONB Query");

    try
    {
        var stopwatch = Stopwatch.StartNew();

        var registration = await _context.Registrations...

        stopwatch.Stop();

        _telemetryClient.TrackMetric(
            "JsonbQueryDuration",
            stopwatch.ElapsedMilliseconds,
            new Dictionary<string, string>
            {
                { "QueryHandler", "GetUserRegistrationForEvent" },
                { "HasAttendees", (registration?.Attendees != null).ToString() }
            });

        return Result<RegistrationDetailsDto?>.Success(dto);
    }
    catch (Exception ex)
    {
        _telemetryClient.TrackException(ex, new Dictionary<string, string>
        {
            { "QueryHandler", "GetUserRegistrationForEvent" },
            { "EventId", request.EventId.ToString() },
            { "UserId", request.UserId.ToString() }
        });

        throw;
    }
}
```

### 6.2: Alert Rules

**Create alerts for**:

1. **JSONB Query Failures**
   - Trigger: > 5 exceptions in 5 minutes
   - Action: Page on-call engineer

2. **Slow JSONB Queries**
   - Trigger: Average duration > 1 second
   - Action: Send Slack notification

3. **Null JSONB Collections**
   - Trigger: > 10% of queries return null Attendees
   - Action: Investigate data migration needed

---

## Strategy 7: Gradual Rollout of JSONB Changes

### Phase-Based Approach

**Phase 1: Add New JSONB Property (Nullable)**
- Add property to value object
- Update EF Core configuration (nullable)
- Deploy code
- No data migration yet

**Phase 2: Backfill Data**
- Run data migration script
- Verify all records updated
- Monitor for errors

**Phase 3: Make Required (if needed)**
- Update EF Core configuration (required)
- Deploy code
- All data already migrated

**Benefits**:
- Reduces deployment risk
- Allows rollback at each phase
- Easier to debug issues

---

## Strategy 8: Health Checks

### JSONB Query Health Check

**Create**: `src/LankaConnect.Infrastructure/HealthChecks/JsonbQueryHealthCheck.cs`

```csharp
public class JsonbQueryHealthCheck : IHealthCheck
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<JsonbQueryHealthCheck> _logger;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test 1: Query with JSONB projection
            var test1 = await _context.Registrations
                .Where(r => r.Attendees != null)
                .Select(r => new
                {
                    r.Id,
                    AttendeeCount = r.Attendees.Count()
                })
                .FirstOrDefaultAsync(cancellationToken);

            // Test 2: Materialize JSONB entity
            var test2 = await _context.Registrations
                .Where(r => r.Attendees != null)
                .FirstOrDefaultAsync(cancellationToken);

            if (test2?.Attendees == null)
            {
                return HealthCheckResult.Degraded(
                    "JSONB query succeeded but Attendees is null");
            }

            return HealthCheckResult.Healthy(
                "JSONB queries working correctly");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JSONB health check failed");
            return HealthCheckResult.Unhealthy(
                "JSONB query failed",
                ex);
        }
    }
}
```

**Register in Startup.cs**:
```csharp
services.AddHealthChecks()
    .AddCheck<JsonbQueryHealthCheck>("jsonb-queries");
```

---

## Implementation Plan

### Immediate Actions (This Week)

1. [ ] Add null checks to all existing JSONB query projections
2. [ ] Add integration tests for GetUserRegistrationForEventQueryHandler
3. [ ] Create JSONB query health check
4. [ ] Document JSONB query patterns in developer guide

### Short-Term Actions (This Month)

1. [ ] Review all query handlers that use JSONB
2. [ ] Add telemetry to JSONB queries
3. [ ] Create alert rules for JSONB query failures
4. [ ] Add JSONB query checklist to PR template

### Long-Term Actions (This Quarter)

1. [ ] Create comprehensive JSONB testing framework
2. [ ] Implement canary deployment for JSONB changes
3. [ ] Develop automated JSONB schema validation
4. [ ] Create ADR for JSONB architecture patterns

---

## Success Metrics

Track these metrics to measure prevention effectiveness:

1. **Zero JSONB-related 500 errors** in production
2. **100% test coverage** for JSONB query handlers
3. **< 500ms average response time** for JSONB queries
4. **Zero deployment rollbacks** due to JSONB issues
5. **All PRs with JSONB changes** pass code review checklist

---

## Conclusion

By following these prevention strategies, we can:

1. Eliminate null reference exceptions in JSONB queries
2. Prevent EF Core translation failures
3. Catch issues before deployment
4. Respond quickly when issues occur
5. Maintain system reliability as JSONB usage grows

The key is **defense in depth**:
- Write defensive code (null checks, materialize before project)
- Test thoroughly (integration tests with real data)
- Monitor actively (telemetry, health checks, alerts)
- Deploy safely (canary, pre-deployment checks)
- Document clearly (guidelines, ADRs, code review checklists)

---

**End of Prevention Strategy Document**
