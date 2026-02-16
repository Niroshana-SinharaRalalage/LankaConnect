# Phase 6A.114: Debug Logging Findings - Signup List Emails

**Date**: 2026-02-15
**Status**: 🔍 **ROOT CAUSE IDENTIFIED - NOT WHAT WE EXPECTED**
**Issue**: Signup commitments save to database but NO domain events dispatched, NO emails sent

---

## Summary

After deploying comprehensive debug logging and testing signup commitments, I discovered the **actual root cause is completely different** from initial hypothesis.

### Initial Hypothesis ❌
- EF Core change tracking failure
- Entities loaded with AsNoTracking()
- SaveChangesAsync bypassing CommitAsync

### Actual Root Cause ✅
**The CommitToSignUpItemCommandHandler is NEVER being called!**

---

## Evidence from Debug Logs

### Test Executed
```bash
POST /api/Events/d543629f-a5ba-4475-b124-3d0fc5200f2f/signups/8567645d-7c71-4965-bec3-f696d266b597/items/72639fc5-005f-415f-aa94-6326965e1590/commit

User: Admin Manager (admin@lankaconnect.com)
Quantity: 3
Notes: "Phase 6A.114 DEBUG TEST"
Response: HTTP 200 OK
```

### Azure Logs Analysis

#### ❌ **MISSING LOGS** (Expected but NOT present):
```
[DEBUG-HANDLER] CommitToSignUpItemCommandHandler executing
[DIAG-R1] EventRepository.GetByIdAsync START
[DIAG-R2] Loading entity WITH/WITHOUT change tracking
[DIAG-R3] Event not found / Event loaded
[DIAG-R4] Event loaded - Enhanced diagnostics
[DIAG-R5] Synced email group IDs
[DIAG-R6] EventRepository.GetByIdAsync COMPLETE
CommitToSignUpItem START/COMPLETE/FAILED
```

**NONE of these logs appear!** This proves the CommandHandler was NEVER executed.

#### ✅ **PRESENT LOGS** (AppDbContext from different source):
```
15:46:24.683 [INF] [DIAG-10] AppDbContext.CommitAsync START
15:46:24.683 [WRN] [DEBUG-STACK] CommitAsync called from:
    <CommitAsync>d__95.MoveNext <-
    AsyncMethodBuilderCore.Start <-
    AsyncTaskMethodBuilder`1.Start <-
    AppDbContext.CommitAsync <-
    <FlushToDatabaseAsync>d__31.MoveNext  ← THIS IS THE KEY!

15:46:24.683 [INF] [DIAG-11] Tracked BaseEntity count BEFORE DetectChanges: 0
15:46:24.683 [INF] [DIAG-13] Tracked BaseEntity count AFTER DetectChanges: 0
15:46:24.683 [INF] [DIAG-15] Domain events collected: 0, Types: []
15:46:24.692 [INF] [DEBUG-SAVECHANGES] SaveChangesAsync called from CommitAsync (correct flow)
15:46:24.692 [INF] [DIAG-16] SaveChangesAsync completed, 0 entities saved
15:46:24.693 [INF] [DIAG-19] No domain events to dispatch - this may indicate an issue!
15:46:24.693 [INF] [DIAG-20] AppDbContext.CommitAsync COMPLETE
```

**Source**: `DatabaseEmailMetrics.FlushToDatabaseAsync()` - NOT from signup commitment!

---

## Root Cause Analysis

### What's Happening

1. **User makes signup commitment via API** → HTTP 200 OK
2. **API endpoint returns success** (somehow)
3. **CommandHandler NEVER executes** (no logs from handler or repository)
4. **Commitment appears in database** (confirmed in earlier tests)
5. **NO domain events raised** (0 tracked entities)
6. **NO emails sent** (handlers never triggered)

### Possible Explanations

#### Theory 1: Caching Layer Returning Cached Response ⚠️
- API endpoint has response caching
- Returns HTTP 200 from cache without executing handler
- Previous successful commitment cached
- Explains: HTTP 200, no logs, data in DB (from previous call)

#### Theory 2: API Route Not Mapped to Handler ⚠️
- Endpoint route exists but not properly wired to MediatR
- Returns success without calling Send(command)
- Controller manually handles request somehow
- Explains: HTTP 200, no handler logs

#### Theory 3: Background Job Processing ⚠️
- API queues commitment to background job
- Returns HTTP 200 immediately
- Job processes later (or fails silently)
- Explains: HTTP 200, delayed/missing processing

#### Theory 4: Direct Database Write in Controller ⚠️
- Controller bypasses MediatR entirely
- Writes directly to DbContext
- Returns success
- Explains: Data in DB, no domain events

---

## Next Investigation Steps

### Priority 1: Check EventsController Endpoint Implementation 🚨

**File**: `src/LankaConnect.API/Controllers/EventsController.cs` (Line ~1737)

**Check**:
1. Does endpoint call `await Mediator.Send(command)`?
2. Or does it write directly to repository/db context?
3. Is there response caching middleware?
4. Any background job enqueueing?

**Expected Code**:
```csharp
[HttpPost("{eventId:guid}/signups/{signupId:guid}/items/{itemId:guid}/commit")]
[Authorize]
public async Task<IActionResult> CommitToSignUpItem(...)
{
    var command = new CommitToSignUpItemCommand(...);
    var result = await Mediator.Send(command);  ← MUST be here!
    return HandleResult(result);
}
```

### Priority 2: Add Controller-Level Logging 🚨

**Add to controller method**:
```csharp
_logger.LogWarning("[DEBUG-CONTROLLER] CommitToSignUpItem endpoint HIT - EventId: {EventId}, ItemId: {ItemId}",
    eventId, itemId);

var command = new CommitToSignUpItemCommand(...);

_logger.LogWarning("[DEBUG-CONTROLLER] About to send command to MediatR");
var result = await Mediator.Send(command);
_logger.LogWarning("[DEBUG-CONTROLLER] MediatR returned: {IsSuccess}", result.IsSuccess);
```

### Priority 3: Test Other MediatR Commands

**Action**: Test another command handler to verify MediatR is working

**Test Cases**:
- UpdateEventCommand
- PublishEventCommand
- CreateSignUpListCommand

**Check**: Do these commands show handler logs?

### Priority 4: Check for Middleware Interference

**Files to Review**:
- Response caching middleware
- API throttling/rate limiting
- Request deduplication middleware

---

## Code Changes Made (Phase 6A.114)

### 1. AppDbContext.cs
- Added stack trace logging to `CommitAsync()`
- Added `SaveChangesAsync()` override to detect bypass calls
- Successfully capturing call stacks

### 2. EventRepository.cs
- Enhanced `GetByIdAsync()` with entity state diagnostics
- Tracks: TrackChangesParam, ActuallyTracked, EntityState, SignUpListsCount
- Alerts if entity requested WITH tracking but loaded DETACHED

### 3. CommitToSignUpItemCommandHandler.cs
- Added `[DEBUG-HANDLER]` logs at entry point
- Logs when event is loaded
- **BUT: These logs NEVER appear in production!**

---

## Status Update

### ✅ Completed
1. Comprehensive debug logging deployed to staging
2. Test signup commitment executed successfully (HTTP 200)
3. Azure logs analyzed - found critical gap
4. Root cause narrowed to controller/routing level

### ❌ NOT Completed
1. Handler execution confirmed (logs missing!)
2. Email delivery verified
3. Root cause fully identified
4. Fix implemented

### ⏭️ Next Actions
1. **Read EventsController.cs line 1737** - Check endpoint implementation
2. **Add controller-level logging** - Track if endpoint is even hit
3. **Test another MediatR command** - Verify MediatR works for other features
4. **Deploy and retest** - Confirm controller logs appear

---

## Recommendation

**Do NOT proceed with EF Core tracking fixes** - that's not the issue!

The real problem is **architectural/routing level**, not data access level.

**Focus on**:
1. Why is CommitAsync being called from `FlushToDatabaseAsync` (email metrics)?
2. Why is CommitToSignUpItemCommandHandler NEVER executing?
3. Is the API endpoint even being hit by our test call?

**Hypothesis**: The HTTP 200 response might be from:
- A different endpoint
- Cached response
- Mock/stub implementation
- Background job acknowledgment

---

## Files to Review Next

1. `src/LankaConnect.API/Controllers/EventsController.cs` (line 1737+)
2. `src/LankaConnect.API/Program.cs` (middleware configuration)
3. `src/LankaConnect.Infrastructure/Email/Services/DatabaseEmailMetrics.cs` (FlushToDatabaseAsync)

---

**Status**: Investigation ongoing - root cause partially identified
**Confidence**: 90% - Handler definitely not executing, need to find why
**Next Update**: After controller code review and additional logging deployment
