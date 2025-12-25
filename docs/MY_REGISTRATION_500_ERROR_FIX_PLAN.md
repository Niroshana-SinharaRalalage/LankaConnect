# Fix Plan: 500 Internal Server Error on /my-registration Endpoint

**Date**: 2025-12-24
**Related RCA**: [MY_REGISTRATION_500_ERROR_RCA.md](./MY_REGISTRATION_500_ERROR_RCA.md)
**Severity**: High
**Estimated Time**: 30-60 minutes

---

## Overview

This fix plan provides step-by-step instructions to diagnose and resolve the 500 error on the `/my-registration` endpoint. The plan is structured to identify the actual root cause first, then apply the appropriate fix.

---

## Phase 1: Diagnosis (15 minutes)

### Step 1.1: Verify Deployment Status

**Objective**: Confirm whether the fix (commit 9b4142fc) is deployed to Azure staging.

**Commands**:
```powershell
# Check Azure Container App revision history
az containerapp revision list `
  --name lankaconnect-api `
  --resource-group <resource-group-name> `
  --output table

# Get current deployed image/tag
az containerapp show `
  --name lankaconnect-api `
  --resource-group <resource-group-name> `
  --query 'properties.template.containers[0].image' `
  --output tsv

# Compare with local git commit
git log origin/develop --oneline -5
git log --oneline -5
```

**Expected Outcome**:
- If deployed image matches commit 9b4142fc or later → Deployment is current
- If deployed image is older than 9b4142fc → **FIX: Deploy latest code**

### Step 1.2: Check Database Migration State

**Objective**: Verify all migrations are applied to staging database.

**SQL Query**:
```sql
-- Connect to Azure PostgreSQL staging database
-- Connection string from Azure Portal > PostgreSQL > Connection strings

SELECT
  migration_id,
  product_version,
  applied_on
FROM __efmigrationshistory
ORDER BY migration_id DESC
LIMIT 10;
```

**Expected Migrations** (recent ones):
- `20XXXXXX_UpdateTicketConfirmationTemplate_Phase6A45`
- `20XXXXXX_AddPublishedAtToEvent_Phase6A46`
- Migrations containing `AgeCategory`, `Gender` changes

**Expected Outcome**:
- All recent migrations applied → Migration state is current
- Missing migrations → **FIX: Apply missing migrations**

### Step 1.3: Examine Actual Registration Data

**Objective**: Inspect the JSONB structure of the failing registration.

**SQL Query**:
```sql
-- Replace <event-id> and <user-email> with actual values
SELECT
  id AS registration_id,
  event_id,
  user_id,
  quantity,
  attendees::text AS attendees_jsonb,
  contact::text AS contact_jsonb,
  attendee_info::text AS legacy_attendee_info,
  status,
  payment_status,
  created_at
FROM registrations
WHERE
  event_id = '<event-id>'
  AND (
    user_id = '<user-id>' -- If authenticated user
    OR contact->>'email' = '<user-email>' -- If anonymous user
  )
  AND status NOT IN ('Cancelled', 'Refunded')
ORDER BY created_at DESC
LIMIT 1;
```

**Expected Outcome**:
- `attendees` is NOT NULL and contains valid JSON array
- `attendees` structure matches EF Core expectations:
  ```json
  [
    {"name": "...", "age_category": "Adult", "gender": "Male"},
    {"name": "...", "age_category": "Child", "gender": null}
  ]
  ```
- If `attendees` is NULL → Legacy registration (shouldn't happen for new registrations)
- If `attendees` has wrong structure → **FIX: Data migration needed**

### Step 1.4: Test Query Locally Against Staging Database

**Objective**: Reproduce the error locally to see exact exception details.

**Steps**:
1. Update `appsettings.Development.json` with staging database connection string
2. Add logging to GetUserRegistrationForEventQueryHandler:
   ```csharp
   try {
       var registration = await _context.Registrations
           .Where(r => r.EventId == request.EventId && ...)
           .OrderByDescending(r => r.CreatedAt)
           .Select(r => new RegistrationDetailsDto { ... })
           .FirstOrDefaultAsync(cancellationToken);

       _logger.LogInformation(
           "Successfully fetched registration for EventId={EventId}, UserId={UserId}",
           request.EventId, request.UserId);

       return Result<RegistrationDetailsDto?>.Success(registration);
   }
   catch (Exception ex)
   {
       _logger.LogError(ex,
           "Failed to fetch registration for EventId={EventId}, UserId={UserId}. Exception: {ExceptionType}",
           request.EventId, request.UserId, ex.GetType().Name);
       throw;
   }
   ```
3. Run the query handler locally
4. Examine exception details and stack trace

**Expected Outcome**:
- If exception is `NullReferenceException` → Code doesn't have null check (deployment issue)
- If exception is `InvalidOperationException` → EF Core translation failed
- If exception is `PostgresException` → Database/schema issue
- If no exception → Deployment issue (local has fix, staging doesn't)

---

## Phase 2: Apply Fix (15-30 minutes)

Based on diagnosis results, follow the appropriate fix path:

### Fix Path A: Deployment Issue (Most Likely)

**Symptoms**:
- Deployed code is older than commit 9b4142fc
- Local test against staging DB succeeds
- Exception is NullReferenceException

**Solution**: Deploy latest code

**Steps**:
```bash
# 1. Ensure all changes committed
git status

# 2. Push to develop branch
git push origin develop

# 3. Wait for Azure DevOps pipeline to deploy
# OR manually trigger deployment

# 4. Verify deployment
az containerapp revision list \
  --name lankaconnect-api \
  --resource-group <rg> \
  --output table

# 5. Check new revision is active
az containerapp show \
  --name lankaconnect-api \
  --resource-group <rg> \
  --query 'properties.latestRevisionName'
```

**Timeline**: 10-15 minutes (deployment time)

### Fix Path B: EF Core Translation Issue (Less Likely)

**Symptoms**:
- Deployed code is current
- Exception is `InvalidOperationException` with message about SQL translation
- Error occurs even with null check in place

**Root Cause**: EF Core cannot translate nested JSONB projection to SQL

**Solution**: Change query to materialize entity first, then project

**Code Change** (GetUserRegistrationForEventQueryHandler.cs):
```csharp
// BEFORE (current - fails to translate)
var registration = await _context.Registrations
    .Where(r => r.EventId == request.EventId && ...)
    .OrderByDescending(r => r.CreatedAt)
    .Select(r => new RegistrationDetailsDto
    {
        Id = r.Id,
        // ... other fields
        Attendees = r.Attendees != null
            ? r.Attendees.Select(a => new AttendeeDetailsDto { ... }).ToList()
            : new List<AttendeeDetailsDto>()
    })
    .FirstOrDefaultAsync(cancellationToken);

// AFTER (fix - materialize then project)
var registration = await _context.Registrations
    .Where(r => r.EventId == request.EventId &&
               r.UserId == request.UserId &&
               r.Status != RegistrationStatus.Cancelled &&
               r.Status != RegistrationStatus.Refunded)
    .OrderByDescending(r => r.CreatedAt)
    .FirstOrDefaultAsync(cancellationToken);

if (registration == null)
    return Result<RegistrationDetailsDto?>.Success(null);

// Project to DTO in memory (not in SQL)
var dto = new RegistrationDetailsDto
{
    Id = registration.Id,
    EventId = registration.EventId,
    UserId = registration.UserId,
    Quantity = registration.Quantity,
    Status = registration.Status,
    CreatedAt = registration.CreatedAt,
    UpdatedAt = registration.UpdatedAt,

    // Map attendees in memory (JSONB already deserialized by EF Core)
    Attendees = registration.Attendees?.Select(a => new AttendeeDetailsDto
    {
        Name = a.Name,
        AgeCategory = a.AgeCategory,
        Gender = a.Gender
    }).ToList() ?? new List<AttendeeDetailsDto>(),

    // Contact information
    ContactEmail = registration.Contact?.Email,
    ContactPhone = registration.Contact?.PhoneNumber,
    ContactAddress = registration.Contact?.Address,

    // Payment information
    PaymentStatus = registration.PaymentStatus,
    TotalPriceAmount = registration.TotalPrice?.Amount,
    TotalPriceCurrency = registration.TotalPrice?.Currency.ToString()
};

return Result<RegistrationDetailsDto?>.Success(dto);
```

**Benefits of This Approach**:
1. EF Core only needs to deserialize JSONB (which it handles well)
2. Projection happens in C# code (no SQL translation needed)
3. More explicit and easier to debug
4. Better performance (smaller query, less SQL complexity)

**Tradeoffs**:
- Fetches full entity instead of projecting in SQL
- Minimal overhead (Attendees JSONB is small, max 10 attendees)

**Timeline**: 15 minutes (code + test + deploy)

### Fix Path C: Database Schema Issue (Least Likely)

**Symptoms**:
- Deployed code is current
- Exception is `InvalidOperationException` or `PostgresException`
- Error message mentions column names or JSONB structure

**Root Cause**: Database JSONB structure doesn't match EF Core expectations

**Diagnosis Query**:
```sql
-- Check actual JSONB structure
SELECT
  id,
  attendees->>0 AS first_attendee_raw,
  jsonb_typeof(attendees) AS attendees_type,
  jsonb_array_length(attendees) AS attendees_count,
  attendees->0->>'age_category' AS age_category_value,
  attendees->0->>'age' AS age_value_legacy
FROM registrations
WHERE event_id = '<event-id>'
  AND attendees IS NOT NULL
LIMIT 1;
```

**Expected**:
- `attendees_type` = `array`
- `age_category_value` = `Adult` or `Child` or `Senior`
- `age_value_legacy` = NULL (old field should not exist)

**If age_category is missing**:
- Need to run data migration to convert old JSONB structure

**Solution**: Apply missing migrations or run data migration script

```sql
-- Verify migrations applied
SELECT * FROM __efmigrationshistory
WHERE migration_id LIKE '%AgeCategory%' OR migration_id LIKE '%Phase6A43%';

-- If missing, apply from migration files
-- Or run data migration script to update JSONB structure
```

**Timeline**: 20-30 minutes (migration + verification)

---

## Phase 3: Verification (15 minutes)

### Step 3.1: Test All Registration Scenarios

**Test Cases**:

1. **Free Event - Anonymous User**
   - Register as anonymous user
   - Complete registration (no payment)
   - Navigate to event detail page
   - Verify "You're Registered!" shows
   - Verify attendee details load without 500 error
   - Verify attendee count matches

2. **Free Event - Authenticated User**
   - Login as authenticated user
   - Register for free event
   - Navigate to event detail page
   - Verify registration details load

3. **Paid Event - Anonymous User**
   - Register as anonymous user
   - Complete Stripe payment
   - Navigate to event detail page
   - Verify registration details load
   - Verify payment status shows "Completed"

4. **Paid Event - Authenticated User**
   - Login as authenticated user
   - Register for paid event
   - Complete Stripe payment
   - Navigate to event detail page
   - Verify registration details load

5. **Legacy Registration (if exists)**
   - Find event with legacy registrations (pre-Phase 6A.43)
   - Navigate to event detail page
   - Verify attendee details load (should show empty list)

**Expected Results**: All test cases pass with 200 OK response

### Step 3.2: Verify Database Data

```sql
-- Check that registration data is correct
SELECT
  r.id,
  r.quantity,
  jsonb_array_length(r.attendees) AS attendees_count,
  r.status,
  r.payment_status,
  e.title
FROM registrations r
  JOIN events e ON r.event_id = e.id
WHERE r.created_at > NOW() - INTERVAL '1 day'
  AND r.status != 'Cancelled'
ORDER BY r.created_at DESC
LIMIT 10;
```

**Expected**: Quantity matches attendees_count for new registrations

### Step 3.3: Monitor Application Logs

**Check for**:
- No 500 errors in application logs
- No exceptions in GetUserRegistrationForEventQueryHandler
- Successful responses for `/my-registration` endpoint

**Azure Log Query**:
```kusto
ContainerAppConsoleLogs_CL
| where ContainerAppName_s == "lankaconnect-api"
| where TimeGenerated > ago(1h)
| where Log_s contains "GetUserRegistrationForEvent" or Log_s contains "my-registration"
| order by TimeGenerated desc
| take 50
```

---

## Phase 4: Prevention (Ongoing)

### 4.1: Add Integration Tests

**Test File**: `tests/LankaConnect.Application.Tests/Events/Queries/GetUserRegistrationForEventQueryHandlerTests.cs`

**Test Cases to Add**:
```csharp
[Fact]
public async Task Handle_WithMultipleAttendees_ReturnsAllAttendees()
{
    // Arrange: Create registration with 9 attendees
    // Act: Call GetUserRegistrationForEventQueryHandler
    // Assert: Response contains all 9 attendees
}

[Fact]
public async Task Handle_WithNullAttendees_ReturnsEmptyList()
{
    // Arrange: Create legacy registration (null Attendees)
    // Act: Call handler
    // Assert: Returns empty attendee list, no exception
}

[Fact]
public async Task Handle_WithAnonymousRegistration_ReturnsRegistrationDetails()
{
    // Arrange: Create anonymous registration
    // Act: Call handler
    // Assert: Returns registration with contact info
}
```

### 4.2: Add Defensive Logging

**Update GetUserRegistrationForEventQueryHandler.cs**:
```csharp
public async Task<Result<RegistrationDetailsDto?>> Handle(
    GetUserRegistrationForEventQuery request,
    CancellationToken cancellationToken)
{
    try
    {
        _logger.LogInformation(
            "Fetching registration for EventId={EventId}, UserId={UserId}",
            request.EventId, request.UserId);

        var registration = await _context.Registrations
            .Where(r => r.EventId == request.EventId && ...)
            .FirstOrDefaultAsync(cancellationToken);

        if (registration == null)
        {
            _logger.LogInformation(
                "No registration found for EventId={EventId}, UserId={UserId}",
                request.EventId, request.UserId);
            return Result<RegistrationDetailsDto?>.Success(null);
        }

        _logger.LogDebug(
            "Registration found: Id={RegistrationId}, Quantity={Quantity}, HasAttendees={HasAttendees}",
            registration.Id, registration.Quantity, registration.Attendees != null);

        // Project to DTO...

        return Result<RegistrationDetailsDto?>.Success(dto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "Failed to fetch registration for EventId={EventId}, UserId={UserId}",
            request.EventId, request.UserId);
        return Result<RegistrationDetailsDto?>.Failure(
            "Failed to load registration details. Please try again later.");
    }
}
```

### 4.3: Add Health Check for JSONB Queries

**Create**: `src/LankaConnect.Infrastructure/HealthChecks/JsonbQueryHealthCheck.cs`

```csharp
public class JsonbQueryHealthCheck : IHealthCheck
{
    private readonly IApplicationDbContext _context;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test JSONB query with projection
            var test = await _context.Registrations
                .Where(r => r.Attendees != null)
                .Select(r => new
                {
                    r.Id,
                    AttendeeCount = r.Attendees.Count()
                })
                .FirstOrDefaultAsync(cancellationToken);

            return HealthCheckResult.Healthy("JSONB queries working");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "JSONB query failed",
                ex);
        }
    }
}
```

### 4.4: Document JSONB Query Patterns

**Create**: `docs/DEVELOPMENT_GUIDELINES_JSONB.md`

**Content**:
- How to query JSONB collections with EF Core
- Common pitfalls with projections
- When to materialize vs. project in SQL
- Testing strategies for JSONB queries

---

## Rollback Plan

If the fix causes new issues:

### Rollback Option 1: Revert to Previous Revision

```bash
# List revisions
az containerapp revision list \
  --name lankaconnect-api \
  --resource-group <rg> \
  --output table

# Activate previous revision
az containerapp revision activate \
  --name lankaconnect-api \
  --resource-group <rg> \
  --revision <previous-revision-name>
```

### Rollback Option 2: Revert Code Changes

```bash
# Revert the fix commit
git revert <fix-commit-sha>

# Push and deploy
git push origin develop
```

---

## Success Criteria

The fix is considered successful when:

- [ ] `/my-registration` endpoint returns 200 OK
- [ ] Attendee details display correctly on event detail page
- [ ] No 500 errors in application logs
- [ ] All 5 test scenarios pass
- [ ] Database queries return expected data
- [ ] No performance degradation (response time < 500ms)
- [ ] Fix verified in Azure staging environment
- [ ] Integration tests added and passing
- [ ] Documentation updated

---

## Timeline Summary

| Phase | Duration | Critical Path |
|-------|----------|---------------|
| Phase 1: Diagnosis | 15 minutes | YES |
| Phase 2: Apply Fix | 15-30 minutes | YES |
| Phase 3: Verification | 15 minutes | YES |
| Phase 4: Prevention | Ongoing | NO |

**Total Time to Resolution**: 45-60 minutes

---

## Contact Information

**Incident Owner**: Development Team
**Escalation**: Backend Lead
**Database Support**: DBA Team

---

## Appendix: Useful Commands

### Azure Container Apps

```bash
# View logs
az containerapp logs show \
  --name lankaconnect-api \
  --resource-group <rg> \
  --follow

# Restart app
az containerapp revision restart \
  --name lankaconnect-api \
  --resource-group <rg> \
  --revision <revision-name>
```

### PostgreSQL

```bash
# Connect to database
psql -h <hostname>.postgres.database.azure.com \
     -U <username> \
     -d <database> \
     -p 5432

# Run query from file
psql -h <hostname> -U <username> -d <database> -f query.sql
```

### Git

```bash
# Check deployment status
git log origin/develop --oneline -10
git diff origin/develop develop

# Check file history
git log --oneline -- GetUserRegistrationForEventQueryHandler.cs
```

---

**End of Fix Plan**
