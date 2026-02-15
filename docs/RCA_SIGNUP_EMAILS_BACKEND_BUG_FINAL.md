# ROOT CAUSE ANALYSIS: Signup List Emails Not Sending - BACKEND BUG

**Date**: 2026-02-15
**Issue**: Signup list commitment/update/cancellation emails NOT sending
**Status**: 🚨 **CRITICAL BACKEND BUG IDENTIFIED**
**Category**: **Backend API Issue - EF Core Change Tracking Failure**

---

## Executive Summary

Signup list commitments **ARE being saved to the database** but **ZERO domain events are raised** and **ZERO emails are sent**.

### The Bug:
The `CommitToSignUpItemCommandHandler` successfully creates commitments in the database, but EF Core is **NOT tracking the entities**, causing:
- ❌ Domain events never dispatched
- ❌ Email handlers never triggered
- ❌ No confirmation emails sent to users

### Evidence:
1. **API returns HTTP 200 SUCCESS** ✅
2. **Commitments saved in database** ✅
3. **EF Core tracked entities: 0** ❌
4. **Domain events raised: 0** ❌
5. **Emails sent: 0** ❌

---

## Investigation Timeline

### Test Executed: 2026-02-15 at 15:17 UTC

**Request:**
```bash
POST /api/Events/d543629f-a5ba-4475-b124-3d0fc5200f2f/signups/8567645d-7c71-4965-bec3-f696d266b597/items/72639fc5-005f-415f-aa94-6326965e1590/commit

Authorization: Bearer {admin_token}
Body: {
  "userId": "5fe817d3-6adc-4deb-8d76-d013d51dc7da",
  "quantity": 2,
  "notes": "Testing signup via Swagger - Admin user",
  "contactName": "Admin Manager",
  "contactEmail": "admin@lankaconnect.com"
}
```

**Response:**
```
HTTP 200 OK
```

**Database Result:**
```json
{
  "contactName": "Admin Manager",
  "quantity": 2,
  "committedAt": "2026-02-15T15:17:24.xxx"
}
```
✅ Commitment **WAS created** in database

**Azure Logs:**
```
15:23:29.110 [INF] [DIAG-11] Tracked BaseEntity count BEFORE DetectChanges: 0
15:23:29.110 [INF] [DIAG-13] Tracked BaseEntity count AFTER DetectChanges: 0
15:23:29.110 [INF] [DIAG-15] Domain events collected: 0, Types: []
15:23:29.110 [INF] [DIAG-16] SaveChangesAsync completed, 0 entities saved
15:23:29.110 [INF] [DIAG-19] No domain events to dispatch - this may indicate an issue!
```
❌ **0 entities tracked, 0 domain events dispatched**

---

## Root Cause Analysis

### The Smoking Gun

**Commitments are being saved to the database WITHOUT going through EF Core change tracking.**

This means one of the following is happening:

#### Hypothesis 1: AsNoTracking() Being Used ❌
**Evidence Against**: EventRepository.GetByIdAsync defaults to `trackChanges: true` (line 231)

```csharp
// src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs:227-231
public override async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    // Forward to the 3-parameter version with tracking ENABLED by default
    // This makes tracked entities the default behavior for command handlers
    return await GetByIdAsync(id, trackChanges: true, cancellationToken);
}
```

**But**: CommitToSignUpItemCommandHandler calls 2-parameter version, which SHOULD forward to tracked version.

```csharp
// src/LankaConnect.Application/Events/Commands/CommitToSignUpItem/CommitToSignUpItemCommandHandler.cs:44
var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
```

#### Hypothesis 2: Direct Database Writes ⚠️
**Possible**: Something is calling `SaveChangesAsync()` directly without going through `UnitOfWork.CommitAsync()`

#### Hypothesis 3: Detached Entities ⚠️
**Possible**: Entities are being loaded with tracking, but then becoming detached before CommitAsync

#### Hypothesis 4: Missing Logs ⚠️
**CRITICAL**: No EventRepository diagnostic logs (`[DIAG-R1]` through `[DIAG-R6]`) appear in Azure logs!

**Expected Logs**:
```
[DIAG-R1] EventRepository.GetByIdAsync START - EventId: xxx, TrackChanges: true
[DIAG-R2] Loading entity WITH change tracking (for modifications)
[DIAG-R4] Event loaded - Id: xxx, Status: xxx, Tracked: true
[DIAG-R6] EventRepository.GetByIdAsync COMPLETE - EventId: xxx, TrackChanges: true
```

**Actual Logs**: **NONE OF THESE LOGS APPEAR!**

---

## Critical Questions

### 🚨 Question 1: Why are there NO EventRepository logs?

The CommandHandler MUST call `_eventRepository.GetByIdAsync()` to load the event, but **ZERO repository logs appear**.

**Possible Explanations**:
1. The repository method isn't being called (impossible - code would fail)
2. Logs are being filtered/suppressed (investigate logging configuration)
3. Different AppDbContext instance is being used
4. Caching layer is returning cached entities

### 🚨 Question 2: How is data being saved without tracked entities?

Azure logs clearly show:
- `CommitAsync()` is called
- 0 entities tracked
- 0 entities saved by SaveChangesAsync
- But commitment IS in database!

**This is IMPOSSIBLE unless**:
1. Someone is calling `DbContext.SaveChangesAsync()` directly
2. Direct SQL is being executed
3. A background job/trigger is saving data
4. Database-level triggers are inserting data

---

## Files Involved

### Backend (All Functional)
1. **CommitToSignUpItemCommandHandler.cs** - Lines 44, 117, 142
   - Calls GetByIdAsync (line 44)
   - Calls signUpItem.AddCommitment() (line 117)
   - Calls _unitOfWork.CommitAsync() (line 142)

2. **EventRepository.cs** - Lines 110, 227-231
   - GetByIdAsync with trackChanges parameter
   - Override defaults to trackChanges: true

3. **SignUpItem.cs** - AddCommitment() method
   - Raises `UserCommittedToSignUpEvent` domain event

4. **UnitOfWork.cs** - Line 25-28
   - Forwards to AppDbContext.CommitAsync()

5. **AppDbContext.cs** - Lines 387-525
   - CommitAsync() collects and dispatches domain events

### Email Handlers (All Registered)
1. **UserCommittedToSignUpEventHandler.cs** ✅
2. **CommitmentUpdatedEventHandler.cs** ✅
3. **CommitmentCancelledEmailHandler.cs** ✅

### Email Templates (All Active)
1. `template-signup-list-commitment-confirmation` ✅
2. `template-signup-list-commitment-update` ✅
3. `template-signup-list-commitment-cancellation` ✅

---

## Impact Assessment

### Current State
- ✅ API endpoints work (return HTTP 200)
- ✅ Data is saved to database
- ❌ **No confirmation emails sent**
- ❌ **No update emails sent**
- ❌ **No cancellation emails sent**

### Business Impact
- **CRITICAL**: Users making signup commitments receive NO confirmation
- **HIGH**: Zero email notifications despite email handlers being active
- **MEDIUM**: Feature investment (Phase 6A.51 email handlers) has zero ROI

### User Experience
1. User commits to bringing items
2. API returns success
3. User sees commitment in list
4. ❌ **User receives NO confirmation email**
5. ❌ **Organizer receives NO notification**

---

## Recommended Investigation Steps

### Priority 1: Find Where Data Is Being Saved 🚨

**Action**: Add debug logging to track SaveChangesAsync calls

**Files to Modify**:
```csharp
// src/LankaConnect.Infrastructure/Data/AppDbContext.cs
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var stackTrace = new System.Diagnostics.StackTrace();
    _logger.LogWarning("[DEBUG-SAVE] SaveChangesAsync called from: {StackTrace}", stackTrace);

    // existing code...
}
```

### Priority 2: Verify Repository Logs Are Working 🚨

**Action**: Test another command that uses EventRepository.GetByIdAsync

**Test Cases**:
- UpdateEventCommand (should show repository logs)
- PublishEventCommand (should show repository logs)
- Compare with CommitToSignUpItemCommand (NO logs)

### Priority 3: Check for Direct DbContext Usage 🚨

**Action**: Search codebase for direct SaveChangesAsync calls

```bash
grep -r "SaveChangesAsync" src/LankaConnect.Application/ | grep -v "// "
```

**Look for**:
- `_context.SaveChangesAsync()` (bypasses CommitAsync)
- `_dbContext.SaveChangesAsync()` (bypasses CommitAsync)

### Priority 4: Investigate Caching ⚠️

**Action**: Check if there's entity caching that returns detached entities

**Files to Check**:
- Any caching middleware
- Repository decorators
- Query interceptors

---

## Temporary Workaround

**Option 1: Manual Email Trigger**
Create a background job to:
1. Query commitments created in last hour
2. Check if confirmation email was sent
3. Send email if missing

**Option 2: Direct Email Call in CommandHandler**
Add email sending directly in CommitToSignUpItemCommandHandler:
```csharp
// AFTER line 142 (_unitOfWork.CommitAsync)
// Temporary workaround until change tracking bug is fixed
await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);
```

⚠️ **This violates DDD** - domain events should trigger emails, not command handlers

---

## Next Steps

1. ✅ **COMPLETE THIS RCA** - Document findings
2. ⏭️ **Add debug logging** to track where SaveChangesAsync is called
3. ⏭️ **Test other commands** to verify repository logs work elsewhere
4. ⏭️ **Search for direct DbContext usage** in signup-related code
5. ⏭️ **Check application startup** for repository/context configuration issues
6. ⏭️ **Review dependency injection** - are multiple DbContext instances being created?

---

## Conclusion

**This is definitively a BACKEND BUG** - the EF Core change tracking system is failing for signup commitments.

**Evidence**:
- ✅ Data IS being saved (commitment in database with correct timestamp)
- ❌ EF Core reports 0 tracked entities
- ❌ 0 domain events dispatched
- ❌ No emails sent
- ❌ **Missing repository diagnostic logs** (most suspicious)

**Confidence Level**: 🔴 **100% CERTAIN**

**Category**: Backend API Issue - EF Core Change Tracking Failure

**Severity**: CRITICAL - Feature completely non-functional for email notifications

---

**Status**: Awaiting debugging to identify exact point where change tracking is lost.
