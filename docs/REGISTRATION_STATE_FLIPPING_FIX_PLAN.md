# Fix Plan: Registration State Flipping Issue (Phase 6A.50)

**Phase**: 6A.50
**Date**: 2025-12-26
**Priority**: P1 - HIGH
**Related RCA**: [REGISTRATION_STATE_FLIPPING_RCA.md](./REGISTRATION_STATE_FLIPPING_RCA.md)

---

## Executive Summary

This document provides the step-by-step implementation plan to fix the registration state flipping issue caused by `null` AgeCategory values in JSONB data.

**Fix Strategy**: 5-Phase Approach
1. ✅ Deploy existing fixes (commit 0daa9168)
2. ⚠️ Add missing null checks to query handlers
3. 🔧 Clean up corrupt database records
4. 🛡️ Add database constraints to prevent recurrence
5. 📊 Add monitoring and observability

**Estimated Time**: 4-6 hours total
**Risk Level**: MEDIUM (data modification required)
**Rollback Plan**: Included for each phase

---

## Phase 1: Deploy Existing Fixes (IMMEDIATE)

### Status Check

**Step 1.1: Verify Current Deployment State**

```bash
# Check latest deployed commit
az containerapp revision list \
  --name lankaconnect-api-staging \
  --resource-group rg-lankaconnect-staging \
  --query "sort_by([].{Name:name, CreatedAt:properties.createdTime, Active:properties.active}, &CreatedAt)" \
  --output table

# Expected: Should show commit BEFORE 0daa9168
```

**Step 1.2: Verify Commits Exist Locally**

```bash
# Check Phase 6A.48 commits exist
git log --oneline | grep "6a48"

# Expected output:
# 0daa9168 fix(phase-6a48): Make AgeCategory nullable in AttendeeDetailsDto
# 6ad41292 fix(phase-6a48): Fix CSV export issues for Signups and Attendees
```

**Step 1.3: Verify Code Changes**

```bash
# Review what's in commit 0daa9168
git show 0daa9168 --name-only

# Expected files:
# src/LankaConnect.Application/Events/Common/RegistrationDetailsDto.cs
# src/LankaConnect.Application/Events/Common/TicketDto.cs
# src/LankaConnect.Application/Events/Queries/GetUserRegistrationForEvent/GetUserRegistrationForEventQueryHandler.cs
```

### Deployment Actions

**Step 1.4: Merge to Develop (if needed)**

```bash
# Check if commits are on develop
git branch --contains 0daa9168

# If NOT on develop, cherry-pick
git checkout develop
git cherry-pick 0daa9168
git cherry-pick 6ad41292

# Push to remote
git push origin develop
```

**Step 1.5: Trigger Azure Deployment**

```bash
# Option A: Trigger via GitHub Actions (if configured)
# Push to develop triggers automatic deployment

# Option B: Manual Docker build and push
cd src/LankaConnect.Web
docker build -t lankaconnect-api:phase6a50-fix1 .
docker tag lankaconnect-api:phase6a50-fix1 <acr-name>.azurecr.io/lankaconnect-api:phase6a50-fix1
docker push <acr-name>.azurecr.io/lankaconnect-api:phase6a50-fix1

# Update Container App
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group rg-lankaconnect-staging \
  --image <acr-name>.azurecr.io/lankaconnect-api:phase6a50-fix1
```

**Step 1.6: Verify Deployment**

```bash
# Check deployment succeeded
az containerapp revision show \
  --name lankaconnect-api-staging \
  --resource-group rg-lankaconnect-staging \
  --revision <revision-name> \
  --query "properties.provisioningState"

# Expected: "Succeeded"

# Check application logs
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group rg-lankaconnect-staging \
  --follow \
  --tail 50
```

### Validation Tests

**Test 1.1: My Registration Endpoint**

```bash
# Test with user who has registration
curl -X GET "https://lankaconnect-staging.azurewebsites.net/api/Events/{eventId}/my-registration" \
  -H "Authorization: Bearer {token}" \
  -v

# Expected: 200 OK (not 500)
```

**Test 1.2: Check Logs for Errors**

```bash
# Monitor for 5 minutes
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group rg-lankaconnect-staging \
  --follow \
  --filter "error" \
  --since 5m

# Expected: No "Nullable object must have a value" errors
```

### Success Criteria

- [ ] Commit 0daa9168 deployed to staging
- [ ] `/my-registration` endpoint returns 200 OK
- [ ] No new exceptions in logs
- [ ] At least 1 affected endpoint now working

### Rollback Plan

If deployment fails or causes new issues:

```bash
# Revert to previous revision
az containerapp revision list \
  --name lankaconnect-api-staging \
  --resource-group rg-lankaconnect-staging \
  --output table

az containerapp revision activate \
  --name lankaconnect-api-staging \
  --resource-group rg-lankaconnect-staging \
  --revision <previous-revision-name>
```

---

## Phase 2: Add Missing Null Checks (SAME DAY)

### Code Changes Required

**Change 2.1: GetTicketQueryHandler.cs**

**File**: `src/LankaConnect.Application/Events/Queries/GetTicket/GetTicketQuery.cs`

**Location**: Lines 115-122

**Current Code**:
```csharp
var attendees = registration.HasDetailedAttendees()
    ? registration.Attendees.Select(a => new TicketAttendeeDto
    {
        Name = a.Name,
        AgeCategory = a.AgeCategory,
        Gender = a.Gender
    }).ToList()
    : null;
```

**Fixed Code**:
```csharp
var attendees = registration.HasDetailedAttendees() && registration.Attendees != null
    ? registration.Attendees.Select(a => new TicketAttendeeDto
    {
        Name = a.Name,
        AgeCategory = a.AgeCategory,
        Gender = a.Gender
    }).ToList()
    : null;
```

**Rationale**: Adds explicit null check for Attendees collection

---

**Change 2.2: GetRegistrationByIdQueryHandler.cs**

**File**: `src/LankaConnect.Application/Events/Queries/GetRegistrationById/GetRegistrationByIdQueryHandler.cs`

**Location**: Lines 39-44

**Current Code**:
```csharp
Attendees = r.Attendees.Select(a => new AttendeeDetailsDto
{
    Name = a.Name,
    AgeCategory = a.AgeCategory,
    Gender = a.Gender
}).ToList(),
```

**Fixed Code**:
```csharp
Attendees = r.Attendees != null
    ? r.Attendees.Select(a => new AttendeeDetailsDto
    {
        Name = a.Name,
        AgeCategory = a.AgeCategory,
        Gender = a.Gender
    }).ToList()
    : new List<AttendeeDetailsDto>(),
```

**Rationale**: Adds null check and returns empty list instead of null

---

**Change 2.3: GetEventAttendeesQueryHandler.cs**

**File**: `src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs`

**Location**: Lines 36-46 (wrap with try-catch)

**Current Code**:
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
```

**Fixed Code**:
```csharp
List<Registration> registrations;
try
{
    registrations = await _context.Registrations
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
    _logger.LogError(ex,
        "JSONB deserialization failed for event {EventId} - corrupt attendee data detected. " +
        "This indicates null AgeCategory values in database. Run data cleanup script.",
        request.EventId);

    // Return empty result with error message
    return Result<EventAttendeesResponse>.Failure(
        "Unable to load attendees due to data corruption. Please contact support.");
}
```

**Rationale**: Catches materialization failure and provides user-friendly error

---

**Change 2.4: Add Logging to GetUserRegistrationForEventQueryHandler**

**File**: `src/LankaConnect.Application/Events/Queries/GetUserRegistrationForEvent/GetUserRegistrationForEventQueryHandler.cs`

**Location**: After line 55 (in Handle method)

**Add Code**:
```csharp
// Add defensive logging
try
{
    var registration = await _context.Registrations
        // ... existing query code ...

    // Log warning if Attendees is unexpectedly null
    if (registration != null && registration.Attendees == null)
    {
        _logger.LogWarning(
            "Registration {RegistrationId} for Event {EventId} has null Attendees collection. " +
            "This may indicate data corruption.",
            registration.Id, request.EventId);
    }

    return Result<RegistrationDetailsDto?>.Success(registration);
}
catch (InvalidOperationException ex) when (ex.Message.Contains("Nullable object"))
{
    _logger.LogError(ex,
        "JSONB deserialization failed for EventId={EventId}, UserId={UserId}. " +
        "Corrupt AgeCategory data detected.",
        request.EventId, request.UserId);

    return Result<RegistrationDetailsDto?>.Failure(
        "Unable to load registration details. Please contact support.");
}
```

**Rationale**: Adds observability and user-friendly error messages

---

### Implementation Steps

**Step 2.1: Create Feature Branch**

```bash
git checkout develop
git pull origin develop
git checkout -b fix/phase-6a50-registration-null-checks
```

**Step 2.2: Apply Changes**

```bash
# Open files in editor and apply changes listed above
# Files to edit:
# 1. src/LankaConnect.Application/Events/Queries/GetTicket/GetTicketQuery.cs
# 2. src/LankaConnect.Application/Events/Queries/GetRegistrationById/GetRegistrationByIdQueryHandler.cs
# 3. src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs
# 4. src/LankaConnect.Application/Events/Queries/GetUserRegistrationForEvent/GetUserRegistrationForEventQueryHandler.cs
```

**Step 2.3: Build and Test Locally**

```bash
# Build solution
dotnet build src/LankaConnect.sln

# Expected: 0 errors

# Run unit tests
dotnet test src/LankaConnect.Application.Tests/LankaConnect.Application.Tests.csproj

# Expected: All tests pass
```

**Step 2.4: Commit Changes**

```bash
git add .
git commit -m "fix(phase-6a50): Add null checks to all AgeCategory mappings

- Add null check to GetTicketQueryHandler (ticket endpoint)
- Add null check to GetRegistrationByIdQueryHandler (registration details)
- Add try-catch to GetEventAttendeesQueryHandler (attendee export)
- Add defensive logging to GetUserRegistrationForEventQueryHandler
- Prevents 500 errors when JSONB contains null AgeCategory values

Fixes registration state flipping issue caused by corrupt JSONB data.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

**Step 2.5: Push and Create PR**

```bash
git push origin fix/phase-6a50-registration-null-checks

# Create PR via GitHub CLI
gh pr create \
  --title "fix(phase-6a50): Add null checks to all AgeCategory mappings" \
  --body "$(cat <<'EOF'
## Summary
- Add null checks to 4 query handlers that map AttendeeDetails to DTOs
- Prevents 500 errors when JSONB contains null AgeCategory values
- Adds defensive logging and user-friendly error messages

## Affected Endpoints
- ✅ GET /api/Events/{eventId}/my-registration/ticket
- ✅ GET /api/Events/registrations/{registrationId}
- ✅ GET /api/Events/{eventId}/attendees
- ✅ GET /api/Events/{eventId}/my-registration (logging only)

## Root Cause
Database contains JSONB records with null age_category due to incomplete data migration in Phase 6A.43.

## Test Plan
- [x] Build succeeds with 0 errors
- [x] All unit tests pass
- [ ] Manual test with corrupt data
- [ ] Deploy to staging
- [ ] Verify all 4 endpoints return 200 OK

## Related
- RCA: docs/REGISTRATION_STATE_FLIPPING_RCA.md
- Fix Plan: docs/REGISTRATION_STATE_FLIPPING_FIX_PLAN.md

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)" \
  --base develop
```

**Step 2.6: Review and Merge**

- Request code review from team
- Address review comments
- Merge to develop after approval

**Step 2.7: Deploy to Staging**

```bash
# Same deployment process as Phase 1
# Build Docker image, push to ACR, update Container App
```

### Validation Tests

**Test 2.1: All 4 Endpoints**

```bash
# 1. My Registration
curl -X GET "https://lankaconnect-staging.azurewebsites.net/api/Events/{eventId}/my-registration" \
  -H "Authorization: Bearer {token}" -v

# 2. Ticket
curl -X GET "https://lankaconnect-staging.azurewebsites.net/api/Events/{eventId}/my-registration/ticket" \
  -H "Authorization: Bearer {token}" -v

# 3. Registration by ID
curl -X GET "https://lankaconnect-staging.azurewebsites.net/api/Events/registrations/{registrationId}" \
  -H "Authorization: Bearer {token}" -v

# 4. Attendee Export
curl -X GET "https://lankaconnect-staging.azurewebsites.net/api/Events/{eventId}/attendees" \
  -H "Authorization: Bearer {token}" -v

# Expected: ALL return 200 OK
```

**Test 2.2: Verify Logging**

```bash
# Check Application Insights for new log entries
az monitor app-insights query \
  --app <app-insights-name> \
  --resource-group rg-lankaconnect-staging \
  --analytics-query "traces | where message contains 'Attendees collection' | top 10 by timestamp desc"
```

### Success Criteria

- [ ] All 4 code changes applied
- [ ] Build succeeds with 0 errors
- [ ] Unit tests pass
- [ ] PR merged to develop
- [ ] Deployed to staging
- [ ] All 4 endpoints return 200 OK
- [ ] Logging captures null Attendees warnings

### Rollback Plan

If issues arise:
1. Revert commits on develop branch
2. Redeploy previous Container App revision
3. Investigate failures and fix before re-attempting

---

## Phase 3: Database Cleanup (SCHEDULED MAINTENANCE)

### Pre-Cleanup Analysis

**Step 3.1: Identify Corrupt Records**

Create diagnostic script: `scripts/identify-corrupt-attendees.sql`

```sql
-- File: scripts/identify-corrupt-attendees.sql

-- Count total corrupt records
SELECT COUNT(*) as corrupt_registration_count
FROM registrations r
WHERE r.attendees IS NOT NULL
  AND jsonb_array_length(r.attendees) > 0
  AND EXISTS (
      SELECT 1
      FROM jsonb_array_elements(r.attendees) elem
      WHERE elem->>'age_category' IS NULL
  );

-- Sample corrupt records for review
SELECT
    r.id as registration_id,
    r.event_id,
    e.title as event_name,
    r.user_id,
    u.email as user_email,
    r.quantity,
    r.attendees::text,
    r.status,
    r.payment_status,
    r.created_at
FROM registrations r
LEFT JOIN events e ON r.event_id = e.id
LEFT JOIN users u ON r.user_id = u.id
WHERE r.attendees IS NOT NULL
  AND jsonb_array_length(r.attendees) > 0
  AND EXISTS (
      SELECT 1
      FROM jsonb_array_elements(r.attendees) elem
      WHERE elem->>'age_category' IS NULL
  )
ORDER BY r.created_at DESC
LIMIT 20;

-- Corruption by date
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

**Step 3.2: Execute Diagnostic Query**

```bash
# Connect to Azure PostgreSQL
az postgres flexible-server connect \
  --name <postgres-server-name> \
  --database lankaconnect-db \
  --admin-user <admin-user>

# Run diagnostic script
\i scripts/identify-corrupt-attendees.sql

# Review output
# Expected: Count and sample of corrupt records
```

**Step 3.3: Review Results with Team**

- Review sample records to understand pattern
- Decide on default value (recommend: 'Adult')
- Get approval from product owner if changing user data
- Document decision in this plan

**Decision**: Default value for null age_category: **Adult**

**Justification**:
- Most events are adult-focused
- Conservative default (higher pricing tier if applicable)
- Unlikely to cause issues with event capacity

---

### Cleanup Script

**Step 3.4: Create Cleanup Script**

Create file: `scripts/fix-corrupt-attendees-data.sql`

```sql
-- File: scripts/fix-corrupt-attendees-data.sql

-- Phase 6A.50: Fix corrupt JSONB data with null age_category
-- Default value: 'Adult'
-- Backup: Create backup before running this script

BEGIN;

-- Create backup table
CREATE TABLE IF NOT EXISTS registrations_backup_phase6a50 AS
SELECT * FROM registrations
WHERE attendees IS NOT NULL
  AND jsonb_array_length(attendees) > 0
  AND EXISTS (
      SELECT 1
      FROM jsonb_array_elements(attendees) elem
      WHERE elem->>'age_category' IS NULL
  );

-- Log backup count
DO $$
DECLARE
    backup_count INT;
BEGIN
    SELECT COUNT(*) INTO backup_count FROM registrations_backup_phase6a50;
    RAISE NOTICE 'Backed up % corrupt registrations to registrations_backup_phase6a50', backup_count;
END $$;

-- Fix corrupt data
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

-- Verify fix
DO $$
DECLARE
    remaining_corrupt INT;
BEGIN
    SELECT COUNT(*) INTO remaining_corrupt
    FROM registrations r
    WHERE r.attendees IS NOT NULL
      AND jsonb_array_length(r.attendees) > 0
      AND EXISTS (
          SELECT 1
          FROM jsonb_array_elements(r.attendees) elem
          WHERE elem->>'age_category' IS NULL
      );

    IF remaining_corrupt > 0 THEN
        RAISE EXCEPTION 'Data cleanup FAILED - % corrupt records remain', remaining_corrupt;
    ELSE
        RAISE NOTICE 'Data cleanup SUCCESSFUL - 0 corrupt records remain';
    END IF;
END $$;

COMMIT;
```

---

### Execution Plan

**Step 3.5: Test on Staging Database**

```bash
# Connect to STAGING database FIRST
az postgres flexible-server connect \
  --name <postgres-server-name-staging> \
  --database lankaconnect-db-staging \
  --admin-user <admin-user>

# Run cleanup script
\i scripts/fix-corrupt-attendees-data.sql

# Expected output:
# NOTICE: Backed up X corrupt registrations...
# NOTICE: Data cleanup SUCCESSFUL - 0 corrupt records remain
# COMMIT
```

**Step 3.6: Validate Staging Cleanup**

```sql
-- Verify 0 corrupt records
SELECT COUNT(*) as corrupt_count
FROM registrations r
WHERE r.attendees IS NOT NULL
  AND jsonb_array_length(r.attendees) > 0
  AND EXISTS (
      SELECT 1
      FROM jsonb_array_elements(r.attendees) elem
      WHERE elem->>'age_category' IS NULL
  );

-- Expected: 0

-- Sample fixed records
SELECT
    r.id,
    r.attendees::text
FROM registrations r
WHERE r.id IN (SELECT id FROM registrations_backup_phase6a50 LIMIT 5);

-- Expected: All have 'age_category': 'Adult'
```

**Step 3.7: Test Application After Cleanup**

```bash
# Test all 4 endpoints again
# Should now work WITHOUT code fixes (but safer to keep them)

# My Registration
curl -X GET "https://lankaconnect-staging.azurewebsites.net/api/Events/{eventId}/my-registration" \
  -H "Authorization: Bearer {token}" -v

# Ticket
curl -X GET "https://lankaconnect-staging.azurewebsites.net/api/Events/{eventId}/my-registration/ticket" \
  -H "Authorization: Bearer {token}" -v

# Expected: 200 OK for all
```

---

**Step 3.8: Execute on Production (AFTER Staging Success)**

```bash
# Schedule during low-traffic period (e.g., 2am UTC)

# Connect to PRODUCTION database
az postgres flexible-server connect \
  --name <postgres-server-name-prod> \
  --database lankaconnect-db-prod \
  --admin-user <admin-user>

# Run cleanup script
\i scripts/fix-corrupt-attendees-data.sql

# Monitor execution time
# Expected: < 1 second (UPDATE is fast for JSONB)

# Validate
SELECT COUNT(*) FROM registrations WHERE attendees IS NOT NULL;
-- Should match previous count

SELECT COUNT(*) FROM registrations_backup_phase6a50;
-- Should show number of fixed records
```

**Step 3.9: Monitor Application After Production Cleanup**

```bash
# Monitor logs for 15 minutes
az containerapp logs show \
  --name lankaconnect-api-prod \
  --resource-group rg-lankaconnect-prod \
  --follow \
  --tail 100

# Check Application Insights
az monitor app-insights query \
  --app <app-insights-name-prod> \
  --resource-group rg-lankaconnect-prod \
  --analytics-query "requests
    | where timestamp > ago(15m)
    | where url contains '/my-registration'
    | summarize count() by resultCode
    | order by resultCode"

# Expected: 200 OK responses, no 500 errors
```

### Success Criteria

- [ ] Diagnostic query identifies all corrupt records
- [ ] Cleanup script tested on staging successfully
- [ ] 0 corrupt records remain in staging
- [ ] All endpoints work after staging cleanup
- [ ] Cleanup script executed on production
- [ ] 0 corrupt records remain in production
- [ ] Backup table created with original data
- [ ] No new errors in production logs

### Rollback Plan

If cleanup causes issues:

```sql
-- Rollback: Restore from backup
BEGIN;

UPDATE registrations r
SET attendees = b.attendees
FROM registrations_backup_phase6a50 b
WHERE r.id = b.id;

COMMIT;

-- Verify restoration
SELECT COUNT(*) FROM registrations_backup_phase6a50;
-- Should match number of restored records
```

---

## Phase 4: Add Database Constraints (AFTER Phase 3 Success)

### Constraint Strategy

**Decision**: Use PostgreSQL CHECK constraint

**Rationale**:
- Prevents future corruption at database level
- Fails fast during INSERT/UPDATE
- Can be added with zero downtime
- Easier to troubleshoot than triggers

---

### Implementation

**Step 4.1: Create Migration**

```bash
# Create new EF Core migration
cd src/LankaConnect.Infrastructure
dotnet ef migrations add AddAttendeesValidationConstraint_Phase6A50 \
  --context AppDbContext \
  --output-dir Data/Migrations
```

**Step 4.2: Edit Migration File**

File: `src/LankaConnect.Infrastructure/Data/Migrations/YYYYMMDD_AddAttendeesValidationConstraint_Phase6A50.cs`

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 6A.50: Add CHECK constraint to prevent null age_category in attendees JSONB
    /// This constraint ensures data integrity at the database level
    /// </summary>
    public partial class AddAttendeesValidationConstraint_Phase6A50 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add CHECK constraint to validate attendees JSONB structure
            // Allows:
            // - NULL attendees
            // - Empty attendees array
            // - Attendees with valid age_category values
            // Rejects:
            // - Attendees with null age_category
            migrationBuilder.Sql(@"
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
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove CHECK constraint
            migrationBuilder.Sql(@"
                ALTER TABLE registrations
                DROP CONSTRAINT IF EXISTS chk_attendees_age_category_not_null;
            ");
        }
    }
}
```

**Step 4.3: Test Migration Locally**

```bash
# Update local database with migration
dotnet ef database update --context AppDbContext

# Expected: Migration applies successfully

# Test constraint works
psql -d lankaconnect-local -c "
INSERT INTO registrations (id, event_id, user_id, quantity, status, payment_status, attendees, created_at, updated_at)
VALUES (
    gen_random_uuid(),
    '00000000-0000-0000-0000-000000000001',
    '00000000-0000-0000-0000-000000000002',
    1,
    'Confirmed',
    'Completed',
    '[{\"name\": \"Test\", \"age_category\": null, \"gender\": null}]'::jsonb,
    NOW(),
    NOW()
);"

# Expected: ERROR: new row violates check constraint "chk_attendees_age_category_not_null"
```

**Step 4.4: Deploy Migration to Staging**

```bash
# Include migration in Docker image
# Deploy to staging (same process as Phase 1-2)

# Migration runs automatically on application startup
# Monitor logs for migration success
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group rg-lankaconnect-staging \
  --follow \
  --filter "migration" \
  --tail 50

# Expected: "Applied migration 'YYYYMMDD_AddAttendeesValidationConstraint_Phase6A50'"
```

**Step 4.5: Validate Constraint in Staging**

```sql
-- Verify constraint exists
SELECT
    tc.constraint_name,
    tc.constraint_type,
    cc.check_clause
FROM information_schema.table_constraints tc
JOIN information_schema.check_constraints cc ON tc.constraint_name = cc.constraint_name
WHERE tc.table_name = 'registrations'
  AND tc.constraint_name = 'chk_attendees_age_category_not_null';

-- Expected: 1 row returned with constraint details
```

**Step 4.6: Test Constraint Prevents Bad Data**

```bash
# Try to insert corrupt data via API
curl -X POST "https://lankaconnect-staging.azurewebsites.net/api/Events/{eventId}/register" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "attendees": [
      {"name": "Test User", "ageCategory": null, "gender": "Male"}
    ]
  }'

# Expected: 400 Bad Request (validation error)
# OR: 500 Internal Server Error with "violates check constraint" in logs
```

**Step 4.7: Deploy to Production**

```bash
# Same deployment process
# Migration runs automatically
# Monitor for successful application
```

### Success Criteria

- [ ] Migration created and tested locally
- [ ] Migration deployed to staging successfully
- [ ] Constraint exists in staging database
- [ ] Constraint prevents null age_category insertions
- [ ] Migration deployed to production
- [ ] Constraint exists in production database
- [ ] No impact on normal registration flow

### Rollback Plan

```bash
# Rollback migration
dotnet ef database update <PreviousMigrationName> --context AppDbContext

# OR manually drop constraint
az postgres flexible-server execute \
  --name <postgres-server-name> \
  --database-name lankaconnect-db \
  --admin-user <admin-user> \
  --admin-password <password> \
  --query-string "ALTER TABLE registrations DROP CONSTRAINT chk_attendees_age_category_not_null;"
```

---

## Phase 5: Monitoring and Observability (ONGOING)

### Logging Enhancements

**Already Added in Phase 2**:
- Null Attendees collection warnings
- JSONB deserialization failure errors
- User-friendly error messages

**Additional Monitoring**:

**Step 5.1: Add Application Insights Custom Metric**

File: `src/LankaConnect.Application/Events/Queries/GetUserRegistrationForEvent/GetUserRegistrationForEventQueryHandler.cs`

Add after exception catch (around line 70):

```csharp
catch (InvalidOperationException ex) when (ex.Message.Contains("Nullable object"))
{
    _logger.LogError(ex,
        "JSONB deserialization failed for EventId={EventId}, UserId={UserId}. " +
        "Corrupt AgeCategory data detected.",
        request.EventId, request.UserId);

    // Track custom metric in Application Insights
    _telemetryClient.TrackMetric(
        "RegistrationCorruptionCount",
        1,
        new Dictionary<string, string>
        {
            { "EventId", request.EventId.ToString() },
            { "UserId", request.UserId.ToString() },
            { "ErrorType", "NullAgeCategory" }
        });

    return Result<RegistrationDetailsDto?>.Failure(
        "Unable to load registration details. Please contact support.");
}
```

**Step 5.2: Create Azure Monitor Alert**

```bash
# Create alert rule for JSONB deserialization failures
az monitor metrics alert create \
  --name "Registration Corruption Alert" \
  --resource-group rg-lankaconnect-prod \
  --scopes "/subscriptions/{subscription-id}/resourceGroups/rg-lankaconnect-prod/providers/Microsoft.Insights/components/{app-insights-name}" \
  --condition "count RegistrationCorruptionCount > 5" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action-group <action-group-id> \
  --description "Alert when more than 5 JSONB deserialization failures occur in 5 minutes"
```

**Step 5.3: Create Application Insights Dashboard**

```bash
# Create dashboard for registration health monitoring
# Include:
# - Request count by endpoint
# - Error rate for /my-registration endpoints
# - Custom metric: RegistrationCorruptionCount
# - Exceptions by type (filter: "Nullable object")
```

---

### Health Check Queries

**Step 5.4: Daily Database Health Check**

Create scheduled query: `scripts/daily-registration-health-check.sql`

```sql
-- File: scripts/daily-registration-health-check.sql

-- Run daily to ensure no new corruption
SELECT
    'Registration Health Check - ' || CURRENT_DATE as report_date,
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
    END) as corrupt_attendees,
    SUM(CASE
        WHEN created_at > CURRENT_DATE - INTERVAL '1 day'
        THEN 1 ELSE 0
    END) as new_registrations_24h
FROM registrations
WHERE created_at > CURRENT_DATE - INTERVAL '7 days';

-- Expected: corrupt_attendees = 0
```

**Step 5.5: Application Insights KQL Query**

```kql
// Track JSONB deserialization failures over time
exceptions
| where timestamp > ago(30d)
| where outerMessage contains "Nullable object must have a value"
| where operation_Name startswith "GET /api/Events"
| summarize count() by bin(timestamp, 1d), operation_Name
| render timechart

// Expected: Count decreases to 0 after Phase 3 deployment
```

---

### Success Criteria

- [ ] Custom metrics tracking JSONB failures
- [ ] Azure Monitor alert configured
- [ ] Application Insights dashboard created
- [ ] Daily health check query scheduled
- [ ] 7-day monitoring period shows 0 failures
- [ ] Team trained on troubleshooting process

---

## Overall Success Metrics

### Technical Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| 500 Error Rate on `/my-registration` | 0% | Application Insights |
| 500 Error Rate on `/ticket` | 0% | Application Insights |
| Corrupt JSONB Records | 0 | Daily health check query |
| Time to Detect Future Issues | < 5 minutes | Azure Monitor alerts |
| Registration Success Rate | > 99.9% | Application Insights |

### Business Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| User Complaints | 0 new | Support tickets |
| Refund Requests | 0 related | Payment logs |
| Time to Resolution | < 6 hours | This fix plan |
| Recurrence Risk | Low | Database constraints + monitoring |

---

## Timeline Summary

| Phase | Duration | Dependency |
|-------|----------|------------|
| **Phase 1**: Deploy Existing Fixes | 30 minutes | None |
| **Phase 2**: Add Code Fixes | 3 hours | Phase 1 complete |
| **Phase 3**: Database Cleanup | 1 hour | Phase 2 deployed to staging |
| **Phase 4**: Add Constraints | 1 hour | Phase 3 successful |
| **Phase 5**: Monitoring | Ongoing | Phase 4 deployed |

**Total Estimated Time**: 5.5 hours (spread over 1-2 days)

---

## Risk Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Deployment breaks existing functionality | LOW | HIGH | Comprehensive testing on staging first |
| Database cleanup corrupts data | LOW | CRITICAL | Backup table created before UPDATE |
| Constraint blocks legitimate registrations | LOW | HIGH | Test all registration scenarios before prod |
| More corrupt data after cleanup | MEDIUM | MEDIUM | Daily health checks + monitoring |
| Performance impact from constraint | LOW | MEDIUM | Constraint uses efficient JSONB operators |

---

## Appendix: Quick Reference Commands

### Check Deployment Status
```bash
az containerapp revision list --name lankaconnect-api-staging -g rg-lankaconnect-staging -o table
```

### Check Application Logs
```bash
az containerapp logs show --name lankaconnect-api-staging -g rg-lankaconnect-staging --follow
```

### Count Corrupt Records
```sql
SELECT COUNT(*) FROM registrations r
WHERE r.attendees IS NOT NULL
  AND jsonb_array_length(r.attendees) > 0
  AND EXISTS (SELECT 1 FROM jsonb_array_elements(r.attendees) elem WHERE elem->>'age_category' IS NULL);
```

### Rollback Deployment
```bash
az containerapp revision activate --name lankaconnect-api-staging -g rg-lankaconnect-staging --revision <previous-revision>
```

---

**Document Status**: READY FOR IMPLEMENTATION
**Owner**: Backend Engineering Team
**Approvers**: System Architect, DevOps Lead, Database Admin
**Next Step**: Begin Phase 1 deployment
