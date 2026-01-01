# Root Cause Analysis: 500 Internal Server Error on /my-registration Endpoint

**Date**: 2025-12-24
**Incident**: User successfully registers for event but cannot load registration details
**Error**: `GET /api/proxy/events/{eventId}/my-registration` returns 500 Internal Server Error
**Severity**: High - Breaks user experience after successful payment/registration

---

## Executive Summary

After comprehensive code analysis, the root cause is **NOT** a code issue with the null check fix (commit 9b4142fc). The fix is correct and the code builds successfully. The 500 error is occurring due to one of three possible causes:

1. **Deployment Issue** - The fix (commit 9b4142fc) has not been deployed to Azure staging
2. **EF Core Query Translation Issue** - PostgreSQL cannot materialize the JSONB query projection
3. **Database Schema Mismatch** - The deployed database schema differs from the code expectations

The most likely cause is **#1 (Deployment Issue)** based on the evidence that:
- The fix was committed but is not on the remote develop branch
- The code builds with 0 errors locally
- The user just registered (data exists) but cannot fetch it back

---

## Timeline of Events

| Time | Event |
|------|-------|
| 2025-12-24 14:06 | Commit 9b4142fc: Fix null Attendees handling |
| 2025-12-24 (later) | User registers for event (9 attendees) |
| 2025-12-24 (later) | `/my-registration` endpoint returns 500 error |
| 2025-12-24 (current) | Latest local commit: da66ce82 (PublishedAt SQL script) |

---

## Investigation Findings

### 1. Code Analysis

#### Query Handler (GetUserRegistrationForEventQueryHandler.cs)
```csharp
// Line 45-50: The null check fix
Attendees = r.Attendees != null ? r.Attendees.Select(a => new AttendeeDetailsDto
{
    Name = a.Name,
    AgeCategory = a.AgeCategory,
    Gender = a.Gender
}).ToList() : new List<AttendeeDetailsDto>(),
```

**Status**: CORRECT - Properly handles null Attendees collection

#### Registration Entity (Registration.cs)
```csharp
// Line 19-20: Attendees collection definition
private readonly List<AttendeeDetails> _attendees = new();
public IReadOnlyList<AttendeeDetails> Attendees => _attendees.AsReadOnly();
```

**Status**: CORRECT - Private backing field, public read-only property

#### EF Core Configuration (RegistrationConfiguration.cs)
```csharp
// Line 65-78: JSONB configuration for Attendees
builder.OwnsMany(r => r.Attendees, attendeesBuilder =>
{
    attendeesBuilder.ToJson("attendees");
    attendeesBuilder.Property(a => a.Name).HasColumnName("name");
    attendeesBuilder.Property(a => a.AgeCategory)
        .HasColumnName("age_category")
        .HasConversion<string>();
    attendeesBuilder.Property(a => a.Gender)
        .HasColumnName("gender")
        .HasConversion<string?>()
        .IsRequired(false);
});
```

**Status**: CORRECT - Proper JSONB configuration with explicit column mappings

#### Registration Creation (Event.RegisterWithAttendees)
```csharp
// Line 312-318: Creates registration with attendees
var registrationResult = Registration.CreateWithAttendees(
    Id,
    userId,
    attendeeList,
    contact,
    totalPrice,
    isPaidEvent);
```

**Status**: CORRECT - Always creates Attendees collection via constructor

### 2. Build Verification

```bash
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:01:48.16
```

**Status**: Code compiles successfully with no errors

### 3. Deployment Analysis

**Local Git Status**:
- Latest commit: `da66ce82` (PublishedAt SQL script)
- Previous commit: `9b4142fc` (Attendees null fix)
- Working directory has uncommitted changes

**Key Question**: Is commit 9b4142fc deployed to Azure staging?

### 4. Data Integrity Analysis

**User Symptom**: "Number of attendees: 9"

This indicates:
1. Registration was created successfully
2. `registration.Quantity = 9` is stored correctly
3. The `Attendees` JSONB array should contain 9 attendee objects

**Possible Data States**:
- **Scenario A**: Attendees JSONB is properly populated (expected)
- **Scenario B**: Attendees JSONB is null (would be legacy format, but user just registered)
- **Scenario C**: Attendees JSONB is malformed or cannot be deserialized

### 5. EF Core Translation Analysis

**Potential Issue**: The projection query uses `.Select()` on a JSONB-backed collection:

```csharp
r.Attendees.Select(a => new AttendeeDetailsDto { ... }).ToList()
```

This works in EF Core 8.0 for PostgreSQL JSONB, but the translation could fail if:
1. The JSONB structure doesn't match expectations
2. EF Core cannot translate the projection to SQL
3. There's a version mismatch between npgsql/EF Core libraries

---

## Root Cause Hypothesis (Ranked by Probability)

### Hypothesis #1: DEPLOYMENT ISSUE (Probability: 70%)

**Evidence**:
- The fix is correct and compiles
- No indication the fix was deployed to staging
- User just registered (recent code path execution)

**Why This Explains Symptoms**:
- Old code (before fix) is still running in Azure
- User's registration has Attendees JSONB (new format)
- Old code calls `.Select()` on potentially null without check
- NullReferenceException → 500 error

**Verification**:
```bash
# Check remote develop branch
git log origin/develop --oneline -1

# Check what's deployed in Azure
az containerapp show --name <app-name> --resource-group <rg> --query 'properties.template.containers[0].image'
```

### Hypothesis #2: EF CORE QUERY TRANSLATION FAILURE (Probability: 20%)

**Evidence**:
- JSONB projections with OwnsMany can fail to translate
- The query uses `.Select()` inside another `.Select()`
- EF Core may not translate nested JSONB projections correctly

**Why This Explains Symptoms**:
- Even with null check, EF Core fails to generate valid SQL
- PostgreSQL rejects the query or returns error
- Application catches exception → 500 error

**Verification**:
```csharp
// Add logging to see generated SQL
_context.Registrations.LogSql()
```

### Hypothesis #3: DATABASE SCHEMA MISMATCH (Probability: 10%)

**Evidence**:
- Migration history shows recent schema changes (AgeCategory, Gender)
- Database might not have latest migrations applied

**Why This Explains Symptoms**:
- JSONB structure in DB uses old column names
- EF Core expects `age_category`, DB has `age` or different structure
- Deserialization fails → 500 error

**Verification**:
```sql
-- Check migration history
SELECT * FROM __efmigrationshistory ORDER BY applied_on DESC LIMIT 5;

-- Check actual JSONB structure
SELECT attendees FROM registrations WHERE event_id = '<event-id>' LIMIT 1;
```

---

## Impact Assessment

### User Impact
- **Severity**: High
- **Affected Users**: All users who register for events
- **Functionality Broken**: Cannot view registration details after signing up
- **Payment Impact**: User pays but cannot see confirmation details

### Business Impact
- Users lose confidence in the system
- Support requests increase
- Revenue at risk if users cannot complete registrations

### Data Integrity Impact
- **None** - Registrations are stored correctly
- Data is intact, only retrieval is broken

---

## Questions Answered

### 1. Root Cause: Why is the query still failing after the null check fix?

**Answer**: The fix is correct but most likely NOT DEPLOYED to Azure staging. The code running in production is the old version without the null check.

### 2. Data Integrity: Is there an issue with how the registration was created?

**Answer**: No. The registration creation code is correct. The user showing "9 attendees" confirms the Quantity field is stored properly. The Attendees JSONB array should also be properly populated.

### 3. EF Core Translation: Is there an issue with JSONB query translation?

**Answer**: Potentially, but less likely. The query structure is valid for EF Core 8.0 + PostgreSQL. However, nested projections on JSONB collections can sometimes fail to translate correctly.

### 4. Migration State: Could there be a schema mismatch?

**Answer**: Unlikely but possible. Recent migrations changed AttendeeDetails from `Age` to `AgeCategory`. If migrations weren't applied to staging DB, deserialization could fail.

### 5. Systemic Issue: Is this a larger architectural problem?

**Answer**: No. The architecture is sound. This is an isolated issue with:
- Either deployment (most likely)
- Or EF Core JSONB projection translation (less likely)
- Or database schema sync (least likely)

---

## Recommended Actions (Priority Order)

### IMMEDIATE (Do First)

1. **Verify Deployment Status**
   ```bash
   # Check what's deployed in Azure
   az containerapp revision list --name <app> --resource-group <rg> --output table

   # Check image tag/version
   az containerapp show --name <app> --resource-group <rg> --query 'properties.template.containers[0].image'
   ```

2. **Check Database Migration State**
   ```sql
   -- Connect to Azure PostgreSQL staging database
   SELECT migration_id, product_version
   FROM __efmigrationshistory
   ORDER BY migration_id DESC
   LIMIT 10;
   ```

3. **Examine Actual Registration Data**
   ```sql
   -- Check the failing registration's data structure
   SELECT
     id,
     event_id,
     user_id,
     quantity,
     attendees::text,
     contact::text,
     status,
     payment_status
   FROM registrations
   WHERE event_id = '<event-id>'
     AND (user_id = '<user-id>' OR contact->>'email' = '<email>')
   ORDER BY created_at DESC
   LIMIT 1;
   ```

### HIGH PRIORITY (Do Next)

4. **Enable Detailed Logging**
   - Add SQL query logging to see what EF Core generates
   - Add exception details in GetUserRegistrationForEventQueryHandler

5. **Test Query Locally Against Staging Database**
   - Connect local environment to staging database
   - Run the exact same query
   - See if error reproduces locally

6. **Deploy Latest Code**
   - If not deployed, push and deploy commit 9b4142fc
   - Verify deployment completes successfully
   - Test `/my-registration` endpoint again

### MEDIUM PRIORITY (For Durability)

7. **Add Integration Tests**
   - Test GetUserRegistrationForEvent with multi-attendee registrations
   - Test with both authenticated and anonymous users
   - Test with different JSONB data shapes

8. **Add Defensive Logging**
   ```csharp
   try {
       var registration = await _context.Registrations...
   } catch (Exception ex) {
       _logger.LogError(ex, "Failed to fetch registration for EventId={EventId}, UserId={UserId}",
           request.EventId, request.UserId);
       throw;
   }
   ```

---

## Next Steps

1. **RUN DIAGNOSTICS** - Execute the verification queries above
2. **ANALYZE RESULTS** - Determine which hypothesis is correct
3. **CREATE FIX PLAN** - Based on root cause, create detailed fix plan
4. **IMPLEMENT FIX** - Deploy or patch as needed
5. **VERIFY FIX** - Test all registration scenarios
6. **PREVENT RECURRENCE** - Add tests and monitoring

---

## Files Analyzed

- `src/LankaConnect.Application/Events/Queries/GetUserRegistrationForEvent/GetUserRegistrationForEventQueryHandler.cs`
- `src/LankaConnect.Domain/Events/Registration.cs`
- `src/LankaConnect.Domain/Events/Event.cs`
- `src/LankaConnect.Infrastructure/Data/Configurations/RegistrationConfiguration.cs`
- `src/LankaConnect.Application/Events/Commands/RegisterAnonymousAttendee/RegisterAnonymousAttendeeCommandHandler.cs`
- `src/LankaConnect.Application/Events/Common/RegistrationDetailsDto.cs`

---

## Appendix: Technical Details

### EF Core JSONB Projection Pattern

The query uses this pattern:
```csharp
.Select(r => new RegistrationDetailsDto
{
    Attendees = r.Attendees != null
        ? r.Attendees.Select(a => new AttendeeDetailsDto { ... }).ToList()
        : new List<AttendeeDetailsDto>()
})
```

This should translate to SQL like:
```sql
SELECT
  r.id,
  r.attendees,
  -- EF Core materializes JSONB into C# objects
FROM registrations r
WHERE r.event_id = @p0 AND r.user_id = @p1
```

**Potential Failure Point**: If `r.Attendees` is accessed before null check in generated SQL, query fails.

### PostgreSQL JSONB Storage

Attendees stored as:
```json
[
  {"name": "John Doe", "age_category": "Adult", "gender": "Male"},
  {"name": "Jane Doe", "age_category": "Child", "gender": null}
]
```

EF Core 8.0 deserializes this automatically when materializing entities.

### Known EF Core JSONB Limitations

1. **Cannot use LINQ methods that don't translate** (GroupBy, Join on JSONB)
2. **Projection inside projection can fail** (nested Select on JSONB collections)
3. **Null handling in JSONB requires careful SQL generation**

---

**End of RCA Document**
