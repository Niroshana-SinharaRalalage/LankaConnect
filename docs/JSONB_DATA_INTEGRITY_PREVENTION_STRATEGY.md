# Prevention Strategy: JSONB Data Integrity Issues

**Date**: 2025-12-26
**Related**: Phase 6A.50 - Registration State Flipping Fix
**Purpose**: Prevent future JSONB data corruption and schema mismatch issues

---

## Executive Summary

This document establishes patterns, practices, and safeguards to prevent JSONB data integrity issues like the Phase 6A.50 registration state flipping bug from recurring.

**Problem Prevented**:
- JSONB schema changes without complete data migration
- Null values in JSONB when domain models expect non-null
- EF Core deserialization failures
- Intermittent 500 errors from corrupt data

**5-Layer Defense Strategy**:
1. Domain Layer: Validation at entity creation
2. Application Layer: Defensive DTO mapping
3. Infrastructure Layer: EF Core configuration safeguards
4. Database Layer: Constraints and triggers
5. Operations Layer: Monitoring and alerts

---

## Layer 1: Domain Layer - Entity Validation

### Pattern 1.1: Value Object Validation

**RULE**: All Value Objects MUST validate invariants in factory methods

**Example**: `AttendeeDetails.cs`

```csharp
public class AttendeeDetails : ValueObject
{
    public string Name { get; }
    public AgeCategory AgeCategory { get; }  // Non-nullable
    public Gender? Gender { get; }

    // ❌ BAD: No validation
    private AttendeeDetails(string name, AgeCategory ageCategory, Gender? gender)
    {
        Name = name;
        AgeCategory = ageCategory;
        Gender = gender;
    }

    // ✅ GOOD: Validation in factory method
    public static Result<AttendeeDetails> Create(string? name, AgeCategory ageCategory, Gender? gender = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<AttendeeDetails>.Failure("Name is required");

        if (!Enum.IsDefined(typeof(AgeCategory), ageCategory))
            return Result<AttendeeDetails>.Failure("Invalid age category");

        if (gender.HasValue && !Enum.IsDefined(typeof(Gender), gender.Value))
            return Result<AttendeeDetails>.Failure("Invalid gender value");

        return Result<AttendeeDetails>.Success(new AttendeeDetails(name.Trim(), ageCategory, gender));
    }
}
```

**Checklist**:
- [ ] All value objects have private constructors
- [ ] All value objects expose `Create()` factory method
- [ ] Factory method validates ALL properties
- [ ] Factory method returns `Result<T>` for error handling
- [ ] Enum properties are validated with `Enum.IsDefined()`

---

### Pattern 1.2: Aggregate Root Validation

**RULE**: Aggregate roots MUST validate all owned entities before creation

**Example**: `Registration.cs`

```csharp
public class Registration : AggregateRoot
{
    private readonly List<AttendeeDetails> _attendees = new();
    public IReadOnlyList<AttendeeDetails> Attendees => _attendees.AsReadOnly();

    // ❌ BAD: No validation of attendees
    public static Result<Registration> CreateWithAttendees(
        Guid eventId,
        Guid? userId,
        List<AttendeeDetails> attendees,
        ContactInfo? contact,
        Money? totalPrice,
        bool isPaidEvent)
    {
        var registration = new Registration(eventId, userId, isPaidEvent);
        registration._attendees.AddRange(attendees);  // ❌ No validation!
        return Result<Registration>.Success(registration);
    }

    // ✅ GOOD: Validate attendees before adding
    public static Result<Registration> CreateWithAttendees(
        Guid eventId,
        Guid? userId,
        List<AttendeeDetails> attendees,
        ContactInfo? contact,
        Money? totalPrice,
        bool isPaidEvent)
    {
        // Validation: Attendees list is required for multi-attendee registration
        if (attendees == null || attendees.Count == 0)
            return Result<Registration>.Failure("At least one attendee is required");

        // Validation: All attendees must have valid data
        foreach (var attendee in attendees)
        {
            if (!Enum.IsDefined(typeof(AgeCategory), attendee.AgeCategory))
                return Result<Registration>.Failure($"Attendee '{attendee.Name}' has invalid age category");

            if (attendee.Gender.HasValue && !Enum.IsDefined(typeof(Gender), attendee.Gender.Value))
                return Result<Registration>.Failure($"Attendee '{attendee.Name}' has invalid gender");
        }

        var registration = new Registration(eventId, userId, isPaidEvent);
        registration._attendees.AddRange(attendees);
        return Result<Registration>.Success(registration);
    }
}
```

**Checklist**:
- [ ] Aggregate validates all owned entities
- [ ] Validation happens before adding to collections
- [ ] Validation checks domain invariants (e.g., enum values)
- [ ] Clear error messages for validation failures

---

## Layer 2: Application Layer - Defensive DTO Mapping

### Pattern 2.1: Nullable DTOs for JSONB-Backed Properties

**RULE**: DTOs mapping from JSONB MUST use nullable types for non-nullable domain properties

**Rationale**: JSONB is schema-less and may contain null values due to data corruption or migration gaps

**Example**:

```csharp
// ❌ BAD: DTO has non-nullable enum matching domain
public record AttendeeDetailsDto
{
    public string Name { get; init; } = string.Empty;
    public AgeCategory AgeCategory { get; init; }  // ❌ Non-nullable
    public Gender? Gender { get; init; }
}

// ✅ GOOD: DTO has nullable enum for defensive mapping
public record AttendeeDetailsDto
{
    public string Name { get; init; } = string.Empty;
    public AgeCategory? AgeCategory { get; init; }  // ✅ Nullable to handle corrupt data
    public Gender? Gender { get; init; }
}
```

**Domain vs DTO Nullability Matrix**:

| Property | Domain Model | DTO | Rationale |
|----------|--------------|-----|-----------|
| `Name` | Non-null | Non-null | Required by business logic |
| `AgeCategory` | Non-null | **Nullable** | JSONB may have nulls |
| `Gender` | Nullable | Nullable | Optional property |
| `Email` | Non-null | Non-null | Required by business logic |
| `PhoneNumber` | Nullable | Nullable | Optional property |

**Checklist**:
- [ ] DTOs for JSONB-backed properties use nullable types
- [ ] Comments explain why DTO is nullable when domain is not
- [ ] Frontend handles null values gracefully

---

### Pattern 2.2: Null-Safe LINQ Projections

**RULE**: All LINQ projections mapping from JSONB collections MUST check for null

**Example**:

```csharp
// ❌ BAD: No null check on collection
var registration = await _context.Registrations
    .Select(r => new RegistrationDetailsDto
    {
        Id = r.Id,
        Attendees = r.Attendees.Select(a => new AttendeeDetailsDto  // ❌ NullReferenceException if r.Attendees is null
        {
            Name = a.Name,
            AgeCategory = a.AgeCategory,
            Gender = a.Gender
        }).ToList()
    })
    .FirstOrDefaultAsync();

// ✅ GOOD: Null check with fallback
var registration = await _context.Registrations
    .Select(r => new RegistrationDetailsDto
    {
        Id = r.Id,
        Attendees = r.Attendees != null
            ? r.Attendees.Select(a => new AttendeeDetailsDto
            {
                Name = a.Name,
                AgeCategory = a.AgeCategory,
                Gender = a.Gender
            }).ToList()
            : new List<AttendeeDetailsDto>()  // ✅ Return empty list instead of null
    })
    .FirstOrDefaultAsync();
```

**Checklist**:
- [ ] All JSONB collection projections have null checks
- [ ] Null collections return empty lists, not null
- [ ] Nested JSONB objects checked for null before property access

---

### Pattern 2.3: Try-Catch for Materialization Failures

**RULE**: Query handlers using `.Include()` on JSONB entities MUST catch deserialization exceptions

**Example**:

```csharp
// ❌ BAD: No exception handling for materialization
public async Task<Result<EventAttendeesResponse>> Handle(GetEventAttendeesQuery request, CancellationToken cancellationToken)
{
    var registrations = await _context.Registrations
        .AsNoTracking()
        .Include(r => r.Attendees)  // ❌ Fails if JSONB has null enum values
        .Where(r => r.EventId == request.EventId)
        .ToListAsync(cancellationToken);

    // ... map to DTO
}

// ✅ GOOD: Catch deserialization errors
public async Task<Result<EventAttendeesResponse>> Handle(GetEventAttendeesQuery request, CancellationToken cancellationToken)
{
    List<Registration> registrations;
    try
    {
        registrations = await _context.Registrations
            .AsNoTracking()
            .Include(r => r.Attendees)
            .Where(r => r.EventId == request.EventId)
            .ToListAsync(cancellationToken);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("Nullable object must have a value"))
    {
        _logger.LogError(ex,
            "JSONB deserialization failed for event {EventId} - corrupt data detected. " +
            "Run scripts/identify-corrupt-attendees.sql to find and fix corrupt records.",
            request.EventId);

        return Result<EventAttendeesResponse>.Failure(
            "Unable to load attendees due to data corruption. Please contact support.");
    }

    // ... map to DTO
}
```

**Checklist**:
- [ ] All `.Include()` calls on JSONB entities wrapped in try-catch
- [ ] Specific exception filter for deserialization errors
- [ ] Clear log message with troubleshooting steps
- [ ] User-friendly error message returned

---

## Layer 3: Infrastructure Layer - EF Core Configuration

### Pattern 3.1: Required JSONB Properties

**RULE**: JSONB properties that are non-nullable in domain SHOULD be configured as required in EF Core

**Example**:

```csharp
// File: RegistrationConfiguration.cs

// ❌ BAD: No IsRequired() for non-nullable enum
builder.OwnsMany(r => r.Attendees, attendeesBuilder =>
{
    attendeesBuilder.ToJson("attendees");

    attendeesBuilder.Property(a => a.Name)
        .HasColumnName("name");

    attendeesBuilder.Property(a => a.AgeCategory)
        .HasColumnName("age_category")
        .HasConversion<string>();  // ❌ Allows null

    attendeesBuilder.Property(a => a.Gender)
        .HasColumnName("gender")
        .HasConversion<string?>()
        .IsRequired(false);
});

// ✅ GOOD: IsRequired() for non-nullable properties
builder.OwnsMany(r => r.Attendees, attendeesBuilder =>
{
    attendeesBuilder.ToJson("attendees");

    attendeesBuilder.Property(a => a.Name)
        .HasColumnName("name")
        .IsRequired();  // ✅ Required

    attendeesBuilder.Property(a => a.AgeCategory)
        .HasColumnName("age_category")
        .HasConversion<string>()
        .IsRequired();  // ✅ Required - prevents null

    attendeesBuilder.Property(a => a.Gender)
        .HasColumnName("gender")
        .HasConversion<string?>()
        .IsRequired(false);  // ✅ Explicitly optional
});
```

**CAVEAT**: `.IsRequired()` on JSONB properties may not enforce at database level for PostgreSQL JSONB. Use database constraints (Layer 4) for true enforcement.

**Checklist**:
- [ ] All non-nullable domain properties configured with `.IsRequired()`
- [ ] Nullable domain properties configured with `.IsRequired(false)`
- [ ] Comments explain which properties are required vs optional

---

### Pattern 3.2: Default Values for JSONB Enums

**RULE**: Consider adding default values for enum properties in JSONB

**Example**:

```csharp
builder.OwnsMany(r => r.Attendees, attendeesBuilder =>
{
    attendeesBuilder.ToJson("attendees");

    attendeesBuilder.Property(a => a.AgeCategory)
        .HasColumnName("age_category")
        .HasConversion<string>()
        .IsRequired()
        .HasDefaultValue("Adult");  // ✅ Default to Adult if somehow null
});
```

**CAVEAT**: Default values in EF Core may not apply to JSONB properties. This is a safeguard but not foolproof.

**Checklist**:
- [ ] Consider defaults for critical enum properties
- [ ] Document default value choice in comments
- [ ] Don't rely on defaults alone - use validation

---

## Layer 4: Database Layer - Constraints and Triggers

### Pattern 4.1: CHECK Constraints for JSONB Structure

**RULE**: Add PostgreSQL CHECK constraints to validate JSONB structure for critical properties

**Example**:

```sql
-- Constraint: Prevent null age_category in attendees JSONB
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

**When to Use**:
- Critical business properties that MUST NOT be null
- Enum values that MUST be valid
- JSONB structure that MUST follow a specific schema

**Checklist**:
- [ ] Constraint added via EF Core migration
- [ ] Constraint tested with invalid data
- [ ] Constraint doesn't block legitimate use cases
- [ ] Constraint error message is clear

---

### Pattern 4.2: Validation Triggers (Alternative to Constraints)

**RULE**: For complex validation, use PostgreSQL triggers instead of CHECK constraints

**Example**:

```sql
-- Trigger function: Validate attendees JSONB structure
CREATE OR REPLACE FUNCTION validate_attendees_jsonb()
RETURNS TRIGGER AS $$
DECLARE
    attendee_elem jsonb;
    age_category text;
BEGIN
    -- Only validate if attendees is not null and not empty
    IF NEW.attendees IS NOT NULL AND jsonb_array_length(NEW.attendees) > 0 THEN
        FOR attendee_elem IN SELECT * FROM jsonb_array_elements(NEW.attendees)
        LOOP
            -- Check age_category is not null
            age_category := attendee_elem->>'age_category';
            IF age_category IS NULL THEN
                RAISE EXCEPTION 'Attendee % has null age_category', attendee_elem->>'name';
            END IF;

            -- Check age_category is valid enum value
            IF age_category NOT IN ('Adult', 'Child') THEN
                RAISE EXCEPTION 'Attendee % has invalid age_category: %', attendee_elem->>'name', age_category;
            END IF;

            -- Check gender is valid enum value (if not null)
            IF attendee_elem->>'gender' IS NOT NULL
               AND attendee_elem->>'gender' NOT IN ('Male', 'Female', 'Other') THEN
                RAISE EXCEPTION 'Attendee % has invalid gender: %', attendee_elem->>'name', attendee_elem->>'gender';
            END IF;
        END LOOP;
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

**Pros**:
- More flexible than CHECK constraints
- Can provide detailed error messages
- Can validate complex logic

**Cons**:
- Slower than CHECK constraints
- More complex to maintain
- Harder to disable temporarily

**Checklist**:
- [ ] Trigger function has clear error messages
- [ ] Trigger only validates what's necessary
- [ ] Trigger doesn't cause performance issues
- [ ] Trigger can be disabled if needed

---

## Layer 5: Operations Layer - Monitoring and Alerting

### Pattern 5.1: Application Logging

**RULE**: Log warnings when defensive code paths are triggered

**Example**:

```csharp
public async Task<Result<RegistrationDetailsDto?>> Handle(GetUserRegistrationForEventQuery request, CancellationToken cancellationToken)
{
    var registration = await _context.Registrations
        .Select(r => new RegistrationDetailsDto
        {
            Attendees = r.Attendees != null
                ? r.Attendees.Select(a => new AttendeeDetailsDto { ... }).ToList()
                : new List<AttendeeDetailsDto>()
        })
        .FirstOrDefaultAsync(cancellationToken);

    // ✅ Log warning if defensive null check was needed
    if (registration != null && registration.Attendees.Count == 0)
    {
        var actualAttendees = await _context.Registrations
            .Where(r => r.Id == registration.Id)
            .Select(r => r.Attendees)
            .FirstOrDefaultAsync(cancellationToken);

        if (actualAttendees == null)
        {
            _logger.LogWarning(
                "Registration {RegistrationId} for Event {EventId} has null Attendees collection. " +
                "This may indicate data corruption. Run scripts/identify-corrupt-attendees.sql",
                registration.Id, request.EventId);
        }
    }

    return Result<RegistrationDetailsDto?>.Success(registration);
}
```

**Checklist**:
- [ ] Log warnings when null checks trigger
- [ ] Log errors when deserialization fails
- [ ] Include troubleshooting steps in log messages
- [ ] Don't log sensitive user data

---

### Pattern 5.2: Custom Application Insights Metrics

**RULE**: Track JSONB deserialization failures as custom metrics

**Example**:

```csharp
catch (InvalidOperationException ex) when (ex.Message.Contains("Nullable object must have a value"))
{
    _logger.LogError(ex,
        "JSONB deserialization failed for EventId={EventId}, UserId={UserId}",
        request.EventId, request.UserId);

    // ✅ Track custom metric
    _telemetryClient.TrackMetric(
        "JsonbDeserializationFailure",
        1,
        new Dictionary<string, string>
        {
            { "Entity", "Registration" },
            { "Property", "Attendees" },
            { "EventId", request.EventId.ToString() },
            { "Operation", "GetUserRegistration" }
        });

    return Result<RegistrationDetailsDto?>.Failure("Unable to load registration details.");
}
```

**Checklist**:
- [ ] Custom metric for each type of JSONB failure
- [ ] Dimensions for entity type, property, operation
- [ ] Dashboard visualizing metrics over time
- [ ] Alerts trigger when metric exceeds threshold

---

### Pattern 5.3: Azure Monitor Alerts

**RULE**: Create alerts for JSONB data integrity issues

**Example Alert 1: Deserialization Failure Count**

```bash
az monitor metrics alert create \
  --name "JSONB Deserialization Failures" \
  --resource-group rg-lankaconnect-prod \
  --scopes "/subscriptions/{subscription-id}/resourceGroups/rg-lankaconnect-prod/providers/Microsoft.Insights/components/{app-insights-name}" \
  --condition "count JsonbDeserializationFailure > 10" \
  --window-size 15m \
  --evaluation-frequency 5m \
  --severity 2 \
  --description "Alert when JSONB deserialization failures exceed 10 in 15 minutes"
```

**Example Alert 2: Daily Health Check**

```sql
-- File: scripts/daily-jsonb-health-check.sql
-- Run via Azure Automation Account or cron job

SELECT
    COUNT(*) as corrupt_count,
    CASE
        WHEN COUNT(*) > 0 THEN 'ALERT: Corrupt JSONB data detected'
        ELSE 'OK: No corrupt JSONB data'
    END as status
FROM registrations r
WHERE r.attendees IS NOT NULL
  AND jsonb_array_length(r.attendees) > 0
  AND EXISTS (
      SELECT 1
      FROM jsonb_array_elements(r.attendees) elem
      WHERE elem->>'age_category' IS NULL
  );

-- If corrupt_count > 0, trigger alert via Azure Logic App
```

**Checklist**:
- [ ] Alert for real-time failures (5-15 min window)
- [ ] Alert for daily health check results
- [ ] Alert includes runbook link for remediation
- [ ] Alert goes to on-call engineer

---

## Data Migration Best Practices

### Rule M.1: Always Write Idempotent Migrations

**Example**:

```sql
-- ❌ BAD: Not idempotent - fails if run twice
UPDATE registrations
SET attendees = (
    SELECT jsonb_agg(
        jsonb_build_object(
            'name', elem->>'name',
            'age_category', CASE WHEN (elem->>'age')::int <= 18 THEN 'Child' ELSE 'Adult' END
        )
    )
    FROM jsonb_array_elements(attendees) elem
);

-- ✅ GOOD: Idempotent - only transforms records that need it
UPDATE registrations
SET attendees = (
    SELECT jsonb_agg(
        jsonb_build_object(
            'name', elem->>'name',
            'age_category', CASE WHEN (elem->>'age')::int <= 18 THEN 'Child' ELSE 'Adult' END
        )
    )
    FROM jsonb_array_elements(attendees) elem
)
WHERE attendees IS NOT NULL
  AND jsonb_array_length(attendees) > 0
  AND attendees->0 ? 'age';  -- ✅ Only if old schema
```

**Checklist**:
- [ ] Migration has WHERE clause checking for old schema
- [ ] Migration can be run multiple times safely
- [ ] Migration tested on empty database
- [ ] Migration tested on database with old schema
- [ ] Migration tested on database with new schema

---

### Rule M.2: Create Backup Before Data Transformation

**Example**:

```sql
-- ✅ ALWAYS create backup table
BEGIN;

CREATE TABLE IF NOT EXISTS registrations_backup_phase6a43 AS
SELECT * FROM registrations
WHERE attendees IS NOT NULL
  AND jsonb_array_length(attendees) > 0
  AND attendees->0 ? 'age';

-- Log backup count
DO $$
DECLARE backup_count INT;
BEGIN
    SELECT COUNT(*) INTO backup_count FROM registrations_backup_phase6a43;
    RAISE NOTICE 'Backed up % registrations to registrations_backup_phase6a43', backup_count;
END $$;

-- Transform data
UPDATE registrations SET attendees = ...;

-- Verify no data lost
DO $$
DECLARE
    original_count INT;
    transformed_count INT;
BEGIN
    SELECT COUNT(*) INTO original_count FROM registrations_backup_phase6a43;
    SELECT COUNT(*) INTO transformed_count FROM registrations
    WHERE attendees IS NOT NULL AND jsonb_array_length(attendees) > 0;

    IF transformed_count < original_count THEN
        RAISE EXCEPTION 'Data loss detected: % original, % transformed', original_count, transformed_count;
    END IF;
END $$;

COMMIT;
```

**Checklist**:
- [ ] Backup table created before transformation
- [ ] Backup table named with migration identifier
- [ ] Verification checks after transformation
- [ ] Rollback plan documented

---

### Rule M.3: Validate Migration Completeness

**Example**:

```sql
-- ✅ Verification query after migration
DO $$
DECLARE
    old_schema_count INT;
BEGIN
    -- Check for any records still using old schema
    SELECT COUNT(*) INTO old_schema_count
    FROM registrations
    WHERE attendees IS NOT NULL
      AND jsonb_array_length(attendees) > 0
      AND attendees->0 ? 'age';  -- Old schema check

    IF old_schema_count > 0 THEN
        RAISE EXCEPTION 'Migration incomplete: % records still have old schema', old_schema_count;
    ELSE
        RAISE NOTICE 'Migration successful: All records transformed to new schema';
    END IF;
END $$;
```

**Checklist**:
- [ ] Verification query checks for old schema
- [ ] Verification query checks for new schema
- [ ] Verification fails migration if incomplete
- [ ] Verification logged for audit trail

---

## Code Review Checklist

When reviewing changes involving JSONB:

### Domain Layer
- [ ] Value objects validate all properties
- [ ] Aggregate roots validate owned entities
- [ ] Factory methods return Result<T>
- [ ] Enum properties validated with Enum.IsDefined()

### Application Layer
- [ ] DTOs use nullable types for JSONB-backed properties
- [ ] LINQ projections have null checks on collections
- [ ] `.Include()` calls wrapped in try-catch
- [ ] Error messages are user-friendly

### Infrastructure Layer
- [ ] EF Core configuration has .IsRequired() for non-nullable properties
- [ ] EF Core configuration has explicit .IsRequired(false) for nullable properties
- [ ] Comments explain nullability choices

### Database Layer
- [ ] Migration is idempotent
- [ ] Migration creates backup table
- [ ] Migration has verification checks
- [ ] Constraints added for critical properties

### Testing
- [ ] Unit tests for value object validation
- [ ] Integration tests for JSONB deserialization
- [ ] Tests cover null/corrupt data scenarios
- [ ] Tests verify error messages

### Monitoring
- [ ] Logging added for defensive code paths
- [ ] Custom metrics track failures
- [ ] Alerts configured for critical failures

---

## Incident Response Playbook

### When JSONB Deserialization Error Occurs

**Step 1: Identify Scope**

```sql
-- Find all corrupt records
SELECT id, event_id, user_id, attendees::text
FROM registrations
WHERE attendees IS NOT NULL
  AND jsonb_array_length(attendees) > 0
  AND EXISTS (
      SELECT 1 FROM jsonb_array_elements(attendees) elem
      WHERE elem->>'age_category' IS NULL
  );
```

**Step 2: Assess Impact**

```kql
// Check how many users affected
exceptions
| where timestamp > ago(1h)
| where outerMessage contains "Nullable object must have a value"
| summarize AffectedUsers=dcount(user_Id) by bin(timestamp, 5m)
```

**Step 3: Immediate Mitigation**

```sql
-- Quick fix: Set null age_category to 'Adult'
UPDATE registrations
SET attendees = (
    SELECT jsonb_agg(
        CASE
            WHEN elem->>'age_category' IS NULL THEN
                jsonb_build_object('name', elem->>'name', 'age_category', 'Adult', 'gender', elem->>'gender')
            ELSE elem
        END
    )
    FROM jsonb_array_elements(attendees) elem
)
WHERE attendees IS NOT NULL
  AND EXISTS (SELECT 1 FROM jsonb_array_elements(attendees) elem WHERE elem->>'age_category' IS NULL);
```

**Step 4: Root Cause Analysis**

- How did null values get into database?
- Was there a code deployment without migration?
- Was there a migration gap?
- Is there a validation bug?

**Step 5: Prevention**

- Add/update database constraint
- Add/update validation in domain layer
- Add/update defensive code in application layer
- Add/update monitoring and alerts

---

## Summary: Defense in Depth

| Layer | Mechanism | Catches | Example |
|-------|-----------|---------|---------|
| **Domain** | Value Object validation | Invalid data at creation | `AttendeeDetails.Create()` validates enum |
| **Application** | Nullable DTOs + null checks | Corrupt data during read | `AgeCategory? AgeCategory` in DTO |
| **Infrastructure** | EF Core `.IsRequired()` | Schema violations | `.HasConversion<string>().IsRequired()` |
| **Database** | CHECK constraints | Direct database writes | `chk_attendees_age_category_not_null` |
| **Operations** | Monitoring + alerts | Runtime issues | Application Insights custom metrics |

**No single layer is foolproof - defense in depth is critical.**

---

## References

- [Phase 6A.50 RCA](./REGISTRATION_STATE_FLIPPING_RCA.md)
- [Phase 6A.50 Fix Plan](./REGISTRATION_STATE_FLIPPING_FIX_PLAN.md)
- [EF Core JSONB Documentation](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-8.0/whatsnew#json-columns)
- [PostgreSQL JSONB Best Practices](https://www.postgresql.org/docs/current/datatype-json.html)

---

**Document Status**: APPROVED
**Owner**: System Architect
**Reviewers**: Backend Team, Database Admin, DevOps
**Effective Date**: 2025-12-26
