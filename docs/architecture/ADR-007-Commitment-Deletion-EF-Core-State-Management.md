# ADR-007: Commitment Deletion Failure - EF Core Entity State Management

**Status**: ROOT CAUSE IDENTIFIED
**Date**: 2025-12-17
**Context**: Phase 6A.28 Open Items Sign-Ups - Commitment Deletion Bug
**Severity**: HIGH - Data integrity issue affecting user sign-up cancellations

---

## Executive Summary

**PROBLEM**: Open Items commitments are NOT being deleted when users cancel their RSVP, despite:
- Fix 1 (Phase 6A.28, commit ffb8c26): Simplified handler, removed duplicate `.Include()`
- Fix 2 (Phase 6A.24, commit 0ee6300): Added `ChangeTracker.DetectChanges()` in AppDbContext

**ROOT CAUSE**: Missing `_eventRepository.Update(@event)` call in CancelRsvpCommandHandler

**VALIDATION**: RsvpToEventCommandHandler has this fix (Phase 6A.24, lines 101 & 167) and WORKS correctly

---

## Technical Analysis

### 1. The Critical Difference

**Working Code** (RsvpToEventCommandHandler):
```csharp
// Lines 90-101
var registerResult = @event.RegisterWithAttendees(...);
if (registerResult.IsFailure)
    return Result<string?>.Failure(registerResult.Error);

// DEFENSIVE FIX Phase 6A.24: Explicitly mark event as modified for change tracking
_eventRepository.Update(@event);

await _unitOfWork.CommitAsync(cancellationToken);
```

**Broken Code** (CancelRsvpCommandHandler):
```csharp
// Lines 83-101
var cancelResult = @event.CancelAllUserCommitments(request.UserId);

if (cancelResult.IsFailure)
{
    _logger.LogWarning("[CancelRsvp] Failed to delete commitments: {Error}", cancelResult.Error);
}
else
{
    _logger.LogInformation("[CancelRsvp] Commitments cancelled successfully");
}

// MISSING: _eventRepository.Update(@event);

// Save changes
await _unitOfWork.CommitAsync(cancellationToken);
```

### 2. Why MarkAsUpdated() Alone Is Insufficient

**BaseEntity.MarkAsUpdated()** (BaseEntity.cs:29-32):
```csharp
public void MarkAsUpdated()
{
    UpdatedAt = DateTime.UtcNow;
}
```

**What This Does**:
- Updates the `UpdatedAt` timestamp (a property value change)
- DOES NOT change EF Core's EntityState tracking

**Why It Fails for Deletions**:
1. Domain method removes items from `_commitments` collection
2. EF Core does NOT automatically track collection changes unless:
   - Entity is in `EntityState.Modified` state, OR
   - ChangeTracker explicitly runs change detection
3. `MarkAsUpdated()` only changes a property, doesn't set EntityState
4. Therefore, removed commitments are never tracked as deletions

### 3. How Repository.Update() Fixes This

**Repository.Update()** (Repository.cs:130-139):
```csharp
public virtual void Update(T entity)
{
    using (LogContext.PushProperty("Operation", "Update"))
    using (LogContext.PushProperty("EntityType", typeof(T).Name))
    using (LogContext.PushProperty("EntityId", entity.Id))
    {
        _logger.Information("Updating entity {EntityType} with ID {EntityId}", typeof(T).Name, entity.Id);
        _dbSet.Update(entity);  // <-- THIS IS THE KEY
    }
}
```

**What DbSet.Update() Does** (EF Core behavior):
1. Sets the entity's EntityState to `EntityState.Modified`
2. Marks ALL navigation properties for change detection
3. Forces EF Core to track collection changes (additions/deletions)
4. Ensures child entities (Commitments) are properly tracked for deletion

### 4. The EventRepository.GetByIdAsync() Complication

**Critical Code** (EventRepository.cs:89):
```csharp
// CRITICAL FIX Phase 6A.33: Reset entity state to prevent UPDATE conflicts
// The sync modifies _emailGroupIds which EF Core detects as a change
// But this is infrastructure hydration, not a business modification
// Reset state to what it was before sync (Unchanged for tracked entities)
_context.Entry(eventEntity).State = originalState;
```

**Why This Matters**:
1. EventRepository loads entity as TRACKED (not AsNoTracking)
2. After loading, it explicitly resets EntityState to `Unchanged`
3. This OVERWRITES any automatic tracking that would have occurred
4. Therefore, subsequent domain method calls don't trigger automatic change tracking
5. **Solution**: Must EXPLICITLY call `_eventRepository.Update()` before CommitAsync()

### 5. Why Phase 6A.24 Fix Didn't Work

**Phase 6A.24 Added** (AppDbContext.cs:313):
```csharp
// CRITICAL FIX Phase 6A.24: Force change detection BEFORE collecting domain events
ChangeTracker.DetectChanges();
```

**What This Fixed**:
- Domain event collection (RegistrationConfirmedEvent, PaymentCompletedEvent)
- Event state detection for email dispatching
- Property-level changes (like UpdatedAt timestamp)

**What This DIDN'T Fix**:
- Collection deletions when EntityState is `Unchanged`
- Child entity removals without explicit Update() call
- Navigation property modifications in aggregates

**Why**:
- `DetectChanges()` only scans entities ALREADY in the ChangeTracker
- If parent entity is in `Unchanged` state, DetectChanges won't mark children as Deleted
- Need `Update()` to change state to `Modified` FIRST, then DetectChanges can work

---

## Evidence Summary

### Proof Points

1. **Pattern Validation**: 25 other command handlers use `Repository.Update()` pattern (see grep results)
2. **Working Example**: RsvpToEventCommandHandler has `_eventRepository.Update(@event)` at lines 101 & 167
3. **Missing Call**: CancelRsvpCommandHandler lacks this call
4. **EntityState Reset**: EventRepository.GetByIdAsync() resets state to `Unchanged` at line 89
5. **MarkAsUpdated() Limitation**: Only sets timestamp, doesn't change EntityState

### User Testing Evidence

- User confirmed: Commitments NOT deleted after cancellation
- Expected: Commitment row removed from database
- Actual: Commitment row persists
- Logs show: Domain method executes successfully
- Conclusion: Changes not persisted to database

---

## Solution

### Code Change Required

**File**: `src\LankaConnect.Application\Events\Commands\CancelRsvp\CancelRsvpCommandHandler.cs`

**Location**: After line 93 (after domain method call, before CommitAsync)

**Change**:
```csharp
// Phase 6A.28: Handle sign-up commitments based on user choice
if (request.DeleteSignUpCommitments)
{
    _logger.LogInformation("[CancelRsvp] Deleting commitments via domain model for EventId={EventId}, UserId={UserId}",
        request.EventId, request.UserId);

    var cancelResult = @event.CancelAllUserCommitments(request.UserId);

    if (cancelResult.IsFailure)
    {
        _logger.LogWarning("[CancelRsvp] Failed to delete commitments: {Error}", cancelResult.Error);
    }
    else
    {
        _logger.LogInformation("[CancelRsvp] Commitments cancelled successfully");
    }

    // ADD THIS LINE (matching RsvpToEventCommandHandler pattern):
    _eventRepository.Update(@event);
}
else
{
    _logger.LogInformation("[CancelRsvp] User chose to keep sign-up commitments for EventId={EventId}, UserId={UserId}",
        request.EventId, request.UserId);
}

// Save changes
await _unitOfWork.CommitAsync(cancellationToken);
```

### Why This Will Work

1. **Explicit State Change**: `Update()` sets EntityState to Modified
2. **Collection Tracking**: EF Core will detect commitment removals
3. **Proven Pattern**: Same fix worked in RsvpToEventCommandHandler (Phase 6A.24)
4. **No Side Effects**: Update() is idempotent, safe to call multiple times
5. **ChangeTracker Integration**: Works with Phase 6A.24's DetectChanges() fix

---

## Verification Plan

### 1. Code Verification
```bash
# Verify the fix is applied
grep -n "_eventRepository.Update" src/LankaConnect.Application/Events/Commands/CancelRsvp/CancelRsvpCommandHandler.cs
```

Expected output: Line number showing the Update() call

### 2. Build Verification
```bash
cd src/LankaConnect.API
dotnet build
```

Expected: 0 errors, 0 warnings

### 3. API Testing (After Deployment)

**Step 1: Create Test Event with Open Items**
```bash
# Create event via API with sign-up list
POST /api/events
```

**Step 2: Register for Event**
```bash
# RSVP to the event
POST /api/events/{eventId}/rsvp
```

**Step 3: Commit to Open Items**
```bash
# Sign up for items
POST /api/events/{eventId}/signup-lists/{listId}/items/{itemId}/commit
```

**Step 4: Cancel RSVP with Deletion**
```bash
# Cancel RSVP with DeleteSignUpCommitments = true
POST /api/events/{eventId}/cancel-rsvp
{
  "deleteSignUpCommitments": true
}
```

**Step 5: Verify Database**
```sql
-- Should return 0 rows (commitment deleted)
SELECT * FROM signup_item_commitments
WHERE user_id = '{userId}';

-- Should show increased remaining_quantity
SELECT remaining_quantity
FROM signup_items
WHERE id = '{itemId}';
```

### 4. Log Verification

Check Azure container logs for:
```
[CancelRsvp] Deleting commitments via domain model...
[CancelRsvp] Commitments cancelled successfully
Updating entity Event with ID {EventId}
Successfully committed {N} changes to database
```

---

## Architectural Implications

### Design Pattern Issue Identified

**Problem**: Inconsistent entity state management across command handlers

**Current State**:
- Some handlers call `Repository.Update()` (25 handlers)
- Some handlers rely on automatic change tracking (assumed to work)
- EventRepository.GetByIdAsync() resets state to Unchanged (Phase 6A.33)

**Recommendation**: Standardize the pattern

### Proposed ADR: Explicit Update() Before CommitAsync()

**Rule**: ALL command handlers that modify aggregates MUST call `Repository.Update()` before `CommitAsync()`

**Rationale**:
1. EventRepository resets EntityState to Unchanged after loading
2. Domain methods may modify collections (not just properties)
3. EF Core doesn't automatically track collection changes in Unchanged state
4. Explicit Update() ensures all changes are tracked

**Implementation Checklist**:
- [ ] Create linting rule or analyzer to enforce pattern
- [ ] Document in architecture guidelines
- [ ] Add to code review checklist
- [ ] Audit existing handlers for compliance

---

## Lessons Learned

### What Went Wrong

1. **Incomplete Fix**: Phase 6A.24 added DetectChanges() but missed the Update() requirement
2. **Testing Gap**: Changes deployed without actual API testing
3. **Pattern Inconsistency**: No clear rule about when to call Update()
4. **Documentation Gap**: EntityState management not documented in guidelines

### What Worked

1. **User Validation**: User tested and caught the issue
2. **Code Comparison**: Comparing working vs broken handlers revealed the pattern
3. **Architectural Review**: Deep analysis found the root cause
4. **Evidence-Based**: Validated hypothesis with code examination

### Preventive Measures

1. **Testing Protocol**: ALWAYS test API endpoints after deployment
2. **Pattern Library**: Document common patterns with examples
3. **Consistency Checks**: Audit similar handlers when fixing bugs
4. **State Management Docs**: Add EF Core state management guide

---

## References

### Related Documents
- [PHASE_6A_28_OPEN_SIGNUP_ITEMS_SUMMARY.md](../PHASE_6A_28_OPEN_SIGNUP_ITEMS_SUMMARY.md)
- [ADR-005: Group Pricing JSONB Update Failure Analysis](./ADR-005-Group-Pricing-JSONB-Update-Failure-Analysis.md)
- [SESSION_33_DEPLOYMENT_FAILURE_ROOT_CAUSE_ANALYSIS.md](../SESSION_33_DEPLOYMENT_FAILURE_ROOT_CAUSE_ANALYSIS.md)

### Code References
- EventRepository.GetByIdAsync(): `src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs:53-94`
- CancelRsvpCommandHandler: `src/LankaConnect.Application/Events/Commands/CancelRsvp/CancelRsvpCommandHandler.cs:75-108`
- RsvpToEventCommandHandler: `src/LankaConnect.Application/Events/Commands/RsvpToEvent/RsvpToEventCommandHandler.cs:90-174`
- Repository.Update(): `src/LankaConnect.Infrastructure/Data/Repositories/Repository.cs:130-139`
- BaseEntity.MarkAsUpdated(): `src/LankaConnect.Domain/Common/BaseEntity.cs:29-32`

### Commits
- Phase 6A.28 Fix 1: `ffb8c26` - Simplified handler, removed duplicate Include
- Phase 6A.24 Fix: `0ee6300` - Added ChangeTracker.DetectChanges() for domain events
- Phase 6A.33 Fix: EventRepository state reset for email group sync

---

**Architect**: Claude Sonnet 4.5
**Validation**: Code analysis + pattern comparison
**Status**: Ready for implementation
