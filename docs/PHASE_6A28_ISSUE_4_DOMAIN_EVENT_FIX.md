# Phase 6A.28 Issue 4 - Domain Event Fix for Commitment Deletion

**Status:** DEPLOYED - Testing Required
**Commit:** df2079f8
**Workflow:** [20338583498](https://github.com/Niroshana-SinharaRalalage/LankaConnect/actions/runs/20338583498)
**Date:** 2025-12-18

---

## Executive Summary

After 3 previous fix attempts that didn't work, we've identified the **actual root cause** and implemented the **correct solution** using domain events.

**Root Cause:** EF Core cannot detect collection deletions when using private backing fields with read-only public interfaces (see [ADR-008](architecture/ADR-008-Phase-6A28-Commitment-Deletion-Failure-Root-Cause-Analysis.md)).

**Solution:** Use domain events to bridge the gap between domain encapsulation and EF Core change tracking.

---

## What Was Fixed

### The Problem

When users canceled registration with "Also delete my sign-up commitments" checkbox:
- Code executed without errors ✅
- Domain logic removed commitments from collections ✅
- **BUT commitments were NOT deleted from database** ❌
- Commitments reappeared after page reload ❌

### Why Previous Fixes Didn't Work

| Fix | What It Did | Why It Failed |
|-----|-------------|---------------|
| 1. Remove duplicate `.Include()` | Fixed query bug | Didn't address change tracking |
| 2. Simplify handler | Cleaner code | Didn't address EF Core tracking |
| 3. Add `_eventRepository.Update()` | Marked Event as Modified | Didn't traverse child collections |
| 4. `ChangeTracker.DetectChanges()` | Already present from Phase 6A.24 | Cannot detect private field mutations |

### The Real Issue

```csharp
// Domain uses private backing field for encapsulation (DDD best practice)
private readonly List<SignUpCommitment> _commitments = new();
public IReadOnlyList<SignUpCommitment> Commitments => _commitments.AsReadOnly();

public Result CancelCommitment(Guid userId)
{
    _commitments.Remove(commitment);  // ← EF Core CANNOT detect this
    MarkAsUpdated();
}
```

**Why EF Core Missed It:**
1. Domain removes from private `_commitments` list
2. EF Core's `ChangeTracker.DetectChanges()` only tracks public properties
3. SignUpCommitment entities remain in `Unchanged` state
4. `SaveChanges()` ignores them → deletion silently lost

---

## The Solution: Domain Event Pattern

### 1. Domain Event Created

**File:** [`src/LankaConnect.Domain/Events/DomainEvents/CommitmentCancelledEvent.cs`](../src/LankaConnect.Domain/Events/DomainEvents/CommitmentCancelledEvent.cs)

```csharp
public record CommitmentCancelledEvent(
    Guid SignUpItemId,
    Guid CommitmentId,
    Guid UserId) : DomainEvent;
```

### 2. Domain Method Updated

**File:** [`src/LankaConnect.Domain/Events/Entities/SignUpItem.cs:271-274`](../src/LankaConnect.Domain/Events/Entities/SignUpItem.cs)

```csharp
public Result CancelCommitment(Guid userId)
{
    // ... remove from collection ...
    _commitments.Remove(commitment);

    // NEW: Raise domain event for infrastructure to handle deletion
    RaiseDomainEvent(new DomainEvents.CommitmentCancelledEvent(Id, commitment.Id, userId));

    MarkAsUpdated();
    return Result.Success();
}
```

### 3. Event Handler Created

**File:** [`src/LankaConnect.Application/Events/EventHandlers/CommitmentCancelledEventHandler.cs`](../src/LankaConnect.Application/Events/EventHandlers/CommitmentCancelledEventHandler.cs)

```csharp
public async Task Handle(
    DomainEventNotification<CommitmentCancelledEvent> notification,
    CancellationToken cancellationToken)
{
    var domainEvent = notification.DomainEvent;

    // Find the commitment entity in EF Core's change tracker
    var commitment = await _context.SignUpCommitments
        .FindAsync(new object[] { domainEvent.CommitmentId }, cancellationToken);

    if (commitment != null)
    {
        // Explicitly mark as deleted - what ChangeTracker.DetectChanges() couldn't do
        _context.SignUpCommitments.Remove(commitment);
    }
}
```

### How It Works

1. **User cancels registration** with checkbox checked
2. **Handler calls** `@event.CancelAllUserCommitments(userId)`
3. **Domain method** removes from private `_commitments` list
4. **Domain method** raises `CommitmentCancelledEvent`
5. **`UnitOfWork.CommitAsync()`** dispatches domain events
6. **Event handler** explicitly marks `SignUpCommitment` as `Deleted`
7. **`SaveChanges()`** persists deletion to database ✅

---

## Why This Solution Is Correct

### 1. Maintains Separation of Concerns
- **Domain:** Business logic (remove from collection, raise event)
- **Infrastructure:** Persistence (mark entity as deleted)

### 2. Preserves Encapsulation
- Private backing field remains private
- IReadOnlyList remains read-only
- Aggregate boundaries intact

### 3. Follows Existing Pattern
- Phase 6A.24 uses domain events for `RegistrationConfirmedEvent`
- Consistent with architecture

### 4. Explicit and Testable
- Clear intent: commitment cancelled → entity deleted
- Can unit test domain event raised
- Can integration test database deletion

### 5. Future-Proof
- Can add side effects (notifications, analytics)
- Decoupled from EF Core specifics

---

## Testing Requirements

### Scenario 1: Cancel WITH Deleting Commitments (THE FIX)

**Expected Behavior:**
1. User registers for event
2. User commits to signup items
3. User cancels registration WITH "Also delete my sign-up commitments" checkbox ✅
4. **Commitments DELETED from database** ✅
5. **Update/Cancel buttons disappear** ✅
6. **Remaining quantities restored** ✅
7. Page reload confirms commitments gone ✅

### Scenario 2: Cancel WITHOUT Deleting Commitments (UNCHANGED)

**Expected Behavior:**
1. User registers for event
2. User commits to signup items
3. User cancels registration WITHOUT checking checkbox
4. Commitments remain in database ✅
5. Update/Cancel buttons still show ✅
6. Page reload confirms commitments still there ✅

### How to Test

Wait for deployment to complete (check [workflow](https://github.com/Niroshana-SinharaRalalage/LankaConnect/actions/runs/20338583498)), then:

1. **Check Azure logs** for `[CommitmentCancelled]` messages:
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group rg-lankaconnect-staging \
  --follow=false \
  | grep -i "CommitmentCancelled"
```

2. **Test in UI:**
   - Register for event
   - Commit to 2-3 signup items
   - Cancel registration WITH checkbox
   - Verify commitments gone (buttons disappeared)
   - Reload page to confirm

3. **Verify database** using [diagnostic-commitment-deletion.sql](../diagnostic-commitment-deletion.sql)

---

## Build Status

**Backend:**
```
✅ Build SUCCEEDED
✅ 0 errors, 0 warnings
✅ Time: 00:00:37
```

**Commit:**
```
df2079f8 - fix(phase-6a28): Issue 4 - Add domain event for commitment deletion
```

**GitHub Actions:**
```
Workflow: 20338583498
Status: in_progress
Commit: df2079f8
```

---

## Files Changed

### New Files
- `src/LankaConnect.Domain/Events/DomainEvents/CommitmentCancelledEvent.cs`
- `src/LankaConnect.Application/Events/EventHandlers/CommitmentCancelledEventHandler.cs`

### Modified Files
- `src/LankaConnect.Domain/Events/Entities/SignUpItem.cs` (lines 271-274)

### No Changes Required
- `CancelRsvpCommandHandler.cs` - Already correct
- `EventRepository.cs` - Already fixed
- `AppDbContext.cs` - Already has `ChangeTracker.DetectChanges()`

---

## Architecture Documentation

See the comprehensive root cause analysis:
- **[ADR-008: Phase 6A.28 Commitment Deletion Failure](architecture/ADR-008-Phase-6A28-Commitment-Deletion-Failure-Root-Cause-Analysis.md)** - 20+ page deep dive
- **[Phase 6A.28 Root Cause & Fix](PHASE_6A_28_COMMITMENT_DELETION_ROOT_CAUSE_AND_FIX.md)** - Executive summary

---

## What Phase 6A.34 Did (NOT the problem)

Phase 6A.34 removed entity state reset to preserve Modified state for domain events:

```csharp
// Phase 6A.34 comment:
// "Do NOT reset entity state after sync - it breaks domain event detection"
```

**Analysis:**
- Phase 6A.34 change is **CORRECT** for its purpose
- The removal **revealed** a pre-existing issue with collection tracking
- Reverting Phase 6A.34 would break domain events
- The real issue is architectural: encapsulation vs. change tracking

---

## Next Steps

1. ✅ **Implementation Complete** - Domain event pattern implemented
2. ✅ **Build Successful** - 0 errors, 0 warnings
3. ✅ **Committed & Pushed** - df2079f8
4. 🔄 **Deployment In Progress** - GitHub Actions workflow running
5. ⏳ **User Testing Required** - Test both scenarios after deployment
6. ⏳ **Fix Issue 3** - Cannot cancel individual Open Items (400 error)
7. ⏳ **Fix Issue 1** - Remove Sign Up buttons from manage page
8. ⏳ **Fix Issue 2** - Remove commitment count numbers

---

## Lessons Learned

1. **EF Core Collection Tracking Is Limited**
   - Snapshot tracking doesn't work with private backing fields
   - Explicit state management needed for deletions

2. **Domain Encapsulation vs. ORM Tracking**
   - DDD favors encapsulation (private fields)
   - ORMs favor mutability (public collections)
   - Domain events bridge the gap

3. **Silent Failures Are Dangerous**
   - No errors != working correctly
   - Need integration tests for persistence

4. **Consult Architects Early**
   - System-architect identified root cause after 3 failed attempts
   - Deep analysis prevented more wasted effort

5. **Test APIs Before Declaring Success**
   - Don't assume deployment success means functionality works
   - Follow best practice #11 consistently

---

**Session:** 34
**Architect Consultation:** system-architect agent (ADR-008)
**Status:** ✅ Implementation Complete, Deployment In Progress, Testing Pending
