# Phase 6A.34 Verification Plan

**Created**: 2025-12-18
**Issue**: User registered for event, but no POST /rsvp logs found in Azure Container App logs
**Deployment**: Phase 6A.34 (revision 0000313) deployed at 04:38:00 UTC with 100% traffic

## Executive Summary

**Most Likely Scenario**: Registration occurred BEFORE Phase 6A.34 deployment (before 04:38 UTC), and POST request logs have rolled out of the 300-line log retention window.

**Evidence Supporting This**:
1. Registration EXISTS in database (GET returns 200 OK)
2. NO POST logs in recent 300 lines (logs rolled out)
3. Phase 6A.34 shows 100% traffic (no routing issues)
4. GET requests ARE being logged (rules out logging configuration issues)
5. No legacy registration code paths exist

**Action Required**: Query database for registration timestamp to confirm, then perform fresh registration test to verify Phase 6A.34 fix is working.

---

## Evidence Analysis

### What We Found

1. **Registration Confirmed**
   - User registered for event: `c1f182a9-c957-4a78-a0b2-085917a88900`
   - Page refreshed after registration
   - UI now shows "You're Registered!"
   - GET `/api/events/c1f182a9.../my-registration` returns 200 OK

2. **Missing Logs**
   - NO `POST /api/events/.../rsvp` in last 300 log lines
   - NO `RsvpToEventCommand` processing logs
   - NO `RegistrationConfirmedEvent` domain event logs
   - NO `[Phase 6A.24] Found X domain events` logs

3. **Logs That Exist**
   - GET requests for registration status (200 OK)
   - Email queue processor running normally
   - UserLoggedInEvent domain events dispatching successfully

4. **Deployment Status**
   - Phase 6A.34 deployed at 04:38:00 UTC
   - Revision 0000313 active with 100% traffic
   - DetectChanges() added to AppDbContext.CommitAsync
   - _eventRepository.Update() calls added

### Scenario Analysis

| Scenario | Evidence For | Evidence Against | Likelihood |
|----------|--------------|------------------|------------|
| **1. Registration before deployment** | Registration exists, no POST logs in recent 300 lines | - | **HIGH** |
| **2. Logging configuration issue** | No POST logs found | GET requests ARE logged | **LOW** |
| **3. Different code path** | - | No legacy paths exist, UI uses standard endpoint | **LOW** |
| **4. Traffic routing issue** | - | 100% traffic to revision 0000313 | **LOW** |

---

## Verification Steps

### Step 1: Query Registration Timestamp (IMMEDIATE)

**Script**: `c:\Work\LankaConnect\scripts\query-registration-timestamp.sql`

```sql
SELECT
    r."Id",
    r."UserId",
    r."EventId",
    r."CreatedAt",
    r."UpdatedAt",
    CASE
        WHEN r."CreatedAt" < '2025-12-18 04:38:00+00' THEN 'BEFORE Phase 6A.34'
        ELSE 'AFTER Phase 6A.34'
    END AS "DeploymentRelation"
FROM "Registrations" r
WHERE r."EventId" = 'c1f182a9-c957-4a78-a0b2-085917a88900'
ORDER BY r."CreatedAt" DESC;
```

**Expected Result**:
- If `CreatedAt < 2025-12-18 04:38:00 UTC`: Registration happened BEFORE fix (explains missing logs)
- If `CreatedAt > 2025-12-18 04:38:00 UTC`: We have a logging problem to investigate

### Step 2: Test Fresh Registration (AFTER TIMESTAMP CHECK)

**Script**: `c:\Work\LankaConnect\scripts\test-phase6a34-fix.sh`

**Prerequisites**:
```bash
export AUTH_TOKEN="Bearer YOUR_TOKEN_HERE"
export API_BASE_URL="https://lankaconnect.azurewebsites.net"
```

**Test Flow**:
1. Check current registration status (GET)
2. Cancel existing registration if needed (DELETE)
3. Register for event (POST)
4. Verify registration exists (GET)
5. Check Azure logs for critical indicators

**Expected Log Sequence** (if fix is working):
```
1. POST /api/events/c1f182a9.../rsvp (200 OK)
2. Processing RsvpToEventCommand for event c1f182a9...
3. [Phase 6A.24] Found 1 domain events to dispatch
4. Dispatching domain event: RegistrationConfirmedEvent
5. Handling RegistrationConfirmedEvent for user ...
6. Queuing confirmation email for registration ...
7. OutboxMessage created with Status=Pending
```

### Step 3: Verify Domain Event Dispatching

**What to Check**:
```bash
# Azure CLI command to monitor logs
az containerapp logs show \
  --name lankaconnect \
  --resource-group DefaultResourceGroup-EUS \
  --follow \
  --tail 100 | grep -i "Phase 6A.24"
```

**Critical Patterns**:
1. `[Phase 6A.24] Found 1 domain events to dispatch`
2. `RegistrationConfirmedEvent`
3. `Queuing confirmation email`
4. `OutboxMessage.*Pending`

### Step 4: Verify Email Queued

**Database Query**:
```sql
SELECT TOP 10
    om."Id",
    om."Type",
    om."Status",
    om."CreatedAt",
    om."ProcessedAt",
    om."ErrorMessage"
FROM "OutboxMessages" om
WHERE om."Type" LIKE '%RegistrationConfirmedEvent%'
AND om."CreatedAt" > '2025-12-18 04:38:00+00'
ORDER BY om."CreatedAt" DESC;
```

**Expected Result**:
- New OutboxMessage with Status=Pending (0)
- Type contains "RegistrationConfirmedEvent"
- CreatedAt after deployment timestamp

---

## Technical Architecture

### Registration Flow

```
1. User clicks "Register" in UI
   ↓
2. POST /api/events/{id}/rsvp
   ↓
3. EventsController.RsvpToEvent()
   ↓
4. RsvpToEventCommandHandler.Handle()
   ↓
5. Event.AddRegistration() (raises RegistrationConfirmedEvent)
   ↓
6. AppDbContext.CommitAsync()
   - DetectChanges() [Phase 6A.34]
   - DispatchDomainEvents() [Phase 6A.24]
   - SaveChangesAsync()
   ↓
7. RegistrationConfirmedEventHandler.Handle()
   - Create OutboxMessage with confirmation email
   ↓
8. EmailQueueProcessor (background job)
   - Process OutboxMessages
   - Send confirmation email via SendGrid
```

### Phase 6A.34 Fix Details

**Problem**: DetectChanges() not called before domain event dispatching, causing EF Core to miss entity changes.

**Solution**: Added `ChangeTracker.DetectChanges()` to `AppDbContext.CommitAsync()`:

```csharp
public async Task<bool> CommitAsync(CancellationToken cancellationToken = default)
{
    // Phase 6A.34: CRITICAL FIX - Call DetectChanges() BEFORE domain event dispatching
    // This ensures EF Core tracks all entity changes made in current context
    ChangeTracker.DetectChanges();

    // Phase 6A.24: Dispatch domain events BEFORE SaveChanges
    await _domainEventDispatcher.DispatchEventsAsync(this, cancellationToken);

    // Now save all changes (including those triggered by domain event handlers)
    return await SaveChangesAsync(cancellationToken) > 0;
}
```

**Also Fixed**: Added `_eventRepository.Update()` calls in EventService to explicitly mark entities as modified.

### Logging Architecture

**Serilog Configuration**:
- Console sink for Azure Container App logs
- Structured logging with enrichment
- Request logging middleware
- Correlation ID tracking

**Current Limitation**: Azure Container App log retention appears limited to ~300 lines, causing older logs to roll out quickly.

**Enhancement Needed**: Add UTC timestamps to all log entries for better debugging.

---

## Decision Matrix

### If Registration Was BEFORE Deployment (Most Likely)

**Conclusion**: Phase 6A.34 fix was deployed correctly, but registration happened earlier.

**Next Actions**:
1. ✅ Mark verification step as complete
2. ✅ Run fresh registration test to verify fix
3. ✅ Confirm domain events dispatch correctly
4. ✅ Confirm confirmation email is queued
5. ✅ Update Phase 6A.34 status to "Verified"

### If Registration Was AFTER Deployment (Unlikely)

**Conclusion**: We have a logging or domain event dispatching issue.

**Next Actions**:
1. ❌ Check Serilog configuration for POST request filtering
2. ❌ Check if domain events are being cleared prematurely
3. ❌ Add explicit logging to RsvpToEventCommandHandler
4. ❌ Add explicit logging to AppDbContext.CommitAsync
5. ❌ Investigate if DetectChanges() is actually being called

---

## Test Checklist

### Pre-Test
- [ ] Run timestamp query to confirm when registration occurred
- [ ] Set AUTH_TOKEN environment variable
- [ ] Verify API_BASE_URL is correct
- [ ] Check Azure Container App logs are accessible

### Test Execution
- [ ] Run test-phase6a34-fix.sh script
- [ ] Monitor Azure logs in real-time during test
- [ ] Capture log output for verification
- [ ] Check database for OutboxMessage creation

### Post-Test Verification
- [ ] POST /rsvp logged with 200 OK status
- [ ] RsvpToEventCommand processing logged
- [ ] "[Phase 6A.24] Found 1 domain events" logged
- [ ] RegistrationConfirmedEvent dispatching logged
- [ ] Confirmation email queued in OutboxMessages
- [ ] Email processed by EmailQueueProcessor

### Documentation
- [ ] Record test timestamp and results
- [ ] Update Phase 6A.34 status
- [ ] Document any issues found
- [ ] Create follow-up tasks if needed

---

## File References

### Scripts Created
- `c:\Work\LankaConnect\scripts\verify-registration-timestamp.sh` - Instructions for timestamp check
- `c:\Work\LankaConnect\scripts\query-registration-timestamp.sql` - Database timestamp queries
- `c:\Work\LankaConnect\scripts\test-phase6a34-fix.sh` - Comprehensive test script

### Documentation
- `c:\Work\LankaConnect\docs\PHASE_6A_34_VERIFICATION_PLAN.md` - This document
- `c:\Work\LankaConnect\docs\PHASE_6A_MASTER_INDEX.md` - Phase tracking index
- `c:\Work\LankaConnect\docs\PROGRESS_TRACKER.md` - Current session status

### Code Files
- `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\AppDbContext.cs` - CommitAsync() with DetectChanges()
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Services\EventService.cs` - Repository.Update() calls
- `c:\Work\LankaConnect\src\LankaConnect.Domain\Events\Registration.cs` - Registration entity
- `c:\Work\LankaConnect\src\LankaConnect.Domain\Common\BaseEntity.cs` - CreatedAt/UpdatedAt tracking

---

## Next Steps

1. **IMMEDIATE** (User action required):
   - Run `query-registration-timestamp.sql` against Azure PostgreSQL database
   - Identify registration timestamp relative to deployment (04:38:00 UTC)

2. **IF BEFORE DEPLOYMENT**:
   - Run `test-phase6a34-fix.sh` to verify fix with fresh registration
   - Monitor Azure logs for expected sequence
   - Verify confirmation email queued
   - Update Phase 6A.34 status to "Verified"

3. **IF AFTER DEPLOYMENT**:
   - Investigate logging configuration
   - Add explicit logging to critical code paths
   - Review domain event dispatching logic
   - Create Phase 6A.35 for logging enhancements

4. **REGARDLESS**:
   - Add UTC timestamps to all log entries
   - Document log retention policy
   - Consider Azure Log Analytics for longer retention
   - Update documentation with test results

---

## Success Criteria

Phase 6A.34 is considered **VERIFIED** when:

1. ✅ Registration timestamp confirms scenario (before or after deployment)
2. ✅ Fresh registration test produces complete log sequence
3. ✅ Domain events dispatch correctly (visible in logs)
4. ✅ Confirmation email queued in OutboxMessages table
5. ✅ Email processed by background job
6. ✅ User receives confirmation email
7. ✅ All verification steps documented

---

## Contact & Support

**Azure Resources**:
- Container App: `lankaconnect`
- Resource Group: `DefaultResourceGroup-EUS`
- Database: Azure PostgreSQL (connection in Azure Portal)

**Log Commands**:
```bash
# Real-time logs
az containerapp logs show --name lankaconnect --resource-group DefaultResourceGroup-EUS --follow --tail 100

# Search logs
az containerapp logs show --name lankaconnect --resource-group DefaultResourceGroup-EUS --tail 500 | grep -i "POST.*rsvp"
```

**Database Access**:
- Via Azure Portal: SQL Database → Query Editor
- Via psql: Connection string in Azure Portal
- Via DBeaver/pgAdmin: Standard PostgreSQL connection

---

## Revision History

| Date | Version | Author | Changes |
|------|---------|--------|---------|
| 2025-12-18 | 1.0 | Claude Sonnet 4.5 | Initial verification plan created |
