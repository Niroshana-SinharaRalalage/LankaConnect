# Phase 6A.28 - Commitment Deletion Root Cause & Fix

**Status:** Root cause identified, solution designed
**Date:** 2025-12-18
**Priority:** HIGH - User-facing data integrity issue

---

## Problem Statement

When users cancel event registration with "Also delete my sign-up commitments" checkbox enabled:
- Code executes without errors
- Domain logic removes commitments from collections
- **BUT: Deletions are NOT persisted to database**
- Commitments reappear after page reload

---

## Root Cause

**EF Core cannot detect collection deletions when using private backing fields with read-only public interfaces.**

### The Architecture Conflict

**Domain Layer (DDD Best Practice):**
```csharp
// SignUpItem.cs - Encapsulated collection
private readonly List<SignUpCommitment> _commitments = new();
public IReadOnlyList<SignUpCommitment> Commitments => _commitments.AsReadOnly();

public Result CancelCommitment(Guid userId)
{
    _commitments.Remove(commitment);  // ← In-memory removal
    MarkAsUpdated();
}
```

**EF Core Change Tracking (Requirement):**
- Needs public mutable collections for snapshot tracking
- `ChangeTracker.DetectChanges()` compares public property snapshots
- Cannot see mutations to private backing field
- Removed items remain in `Unchanged` state → NOT deleted

### Why Silent Failure Occurs

1. Event loaded via `GetByIdAsync()` with eager loading
2. SignUpCommitment entities tracked with state = `Unchanged`
3. Domain method removes from `_commitments` (private field)
4. `_eventRepository.Update(@event)` marks Event as `Modified`
5. **BUT SignUpCommitment state remains `Unchanged`**
6. `ChangeTracker.DetectChanges()` cannot detect private field mutation
7. `SaveChanges()` updates Event but **ignores SignUpCommitment** (still Unchanged)
8. No errors, no exceptions - deletion silently lost

---

## Why Previous Fixes Didn't Work

### Fix 1: Remove Duplicate .Include() ✅
- Fixed query bug, but not change tracking issue

### Fix 2: Simplify Handler ✅
- Cleaned code, but didn't address EF Core tracking

### Fix 3: Add _eventRepository.Update() ✅
- Marks **Event** as Modified
- Does **NOT** traverse child collections
- SignUpCommitment state unchanged

### Fix 4: ChangeTracker.DetectChanges() ✅
- Already present in Phase 6A.24
- Works for property changes
- **Cannot detect private collection mutations**

---

## Solution: Domain Event Pattern

Use domain events to bridge the gap between domain encapsulation and infrastructure persistence.

### Implementation

#### 1. Domain Event
```csharp
// Domain/Events/DomainEvents/CommitmentCancelledEvent.cs
public record CommitmentCancelledEvent(
    Guid SignUpItemId,
    Guid CommitmentId,
    Guid UserId) : DomainEvent;
```

#### 2. Raise Event in Domain
```csharp
// Domain/Events/Entities/SignUpItem.cs
public Result CancelCommitment(Guid userId)
{
    var commitment = _commitments.FirstOrDefault(c => c.UserId == userId);
    if (commitment == null)
        return Result.Failure("User has no commitment to this item");

    RemainingQuantity += commitment.Quantity;
    _commitments.Remove(commitment);

    // NEW: Raise domain event for infrastructure to handle deletion
    RaiseDomainEvent(new CommitmentCancelledEvent(Id, commitment.Id, userId));

    MarkAsUpdated();
    return Result.Success();
}
```

#### 3. Handle Event in Infrastructure
```csharp
// Infrastructure/Events/CommitmentCancelledEventHandler.cs
public class CommitmentCancelledEventHandler
    : INotificationHandler<DomainEventNotification<CommitmentCancelledEvent>>
{
    private readonly AppDbContext _context;
    private readonly ILogger<CommitmentCancelledEventHandler> _logger;

    public CommitmentCancelledEventHandler(
        AppDbContext context,
        ILogger<CommitmentCancelledEventHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<CommitmentCancelledEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "[CommitmentCancelled] Deleting commitment {CommitmentId} for user {UserId}",
            domainEvent.CommitmentId, domainEvent.UserId);

        // Find the commitment entity in change tracker or database
        var commitment = await _context.SignUpCommitments
            .FindAsync(new object[] { domainEvent.CommitmentId }, cancellationToken);

        if (commitment != null)
        {
            // Explicitly mark as deleted
            _context.SignUpCommitments.Remove(commitment);
            _logger.LogInformation(
                "[CommitmentCancelled] Marked commitment {CommitmentId} as deleted",
                domainEvent.CommitmentId);
        }
        else
        {
            _logger.LogWarning(
                "[CommitmentCancelled] Commitment {CommitmentId} not found in database",
                domainEvent.CommitmentId);
        }
    }
}
```

### No Handler Changes Required

The existing `CancelRsvpCommandHandler` remains unchanged:

```csharp
// Already correct - domain event dispatched by CommitAsync()
if (request.DeleteSignUpCommitments)
{
    var cancelResult = @event.CancelAllUserCommitments(request.UserId);
    _eventRepository.Update(@event);
}

await _unitOfWork.CommitAsync(cancellationToken);  // ← Dispatches domain events
```

---

## Why This Solution Works

### 1. Maintains Separation of Concerns
- **Domain:** Business logic (remove from collection, raise event)
- **Infrastructure:** Persistence (mark entity as deleted in EF Core)

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

### Unit Tests
```csharp
[Fact]
public void CancelCommitment_ShouldRaiseDomainEvent()
{
    // Arrange
    var item = CreateSignUpItemWithCommitment(userId);

    // Act
    var result = item.CancelCommitment(userId);

    // Assert
    Assert.True(result.IsSuccess);
    var domainEvent = item.DomainEvents
        .OfType<CommitmentCancelledEvent>()
        .FirstOrDefault();
    Assert.NotNull(domainEvent);
    Assert.Equal(userId, domainEvent.UserId);
}
```

### Integration Tests
```csharp
[Fact]
public async Task CancelRsvp_WithDeleteCommitments_ShouldRemoveFromDatabase()
{
    // Arrange
    var @event = await CreateEventWithUserCommitment(userId);
    var commitmentId = @event.SignUpLists[0].Items[0].Commitments[0].Id;

    // Act
    var command = new CancelRsvpCommand(eventId, userId, DeleteSignUpCommitments: true);
    await _handler.Handle(command, CancellationToken.None);

    // Assert
    var commitment = await _context.SignUpCommitments
        .FirstOrDefaultAsync(c => c.Id == commitmentId);
    Assert.Null(commitment);  // Should be deleted from database
}
```

---

## Implementation Checklist

- [ ] Create `CommitmentCancelledEvent.cs` in Domain/Events/DomainEvents
- [ ] Update `SignUpItem.CancelCommitment()` to raise domain event
- [ ] Create `CommitmentCancelledEventHandler.cs` in Infrastructure/Events
- [ ] Register handler in DI container (should auto-register via MediatR)
- [ ] Add unit test for domain event
- [ ] Add integration test for database deletion
- [ ] Test with staging deployment
- [ ] Verify commitments actually deleted in database

---

## Files to Modify

### 1. Domain Layer
**File:** `src/LankaConnect.Domain/Events/DomainEvents/CommitmentCancelledEvent.cs` (NEW)
```csharp
namespace LankaConnect.Domain.Events.DomainEvents;

public record CommitmentCancelledEvent(
    Guid SignUpItemId,
    Guid CommitmentId,
    Guid UserId) : DomainEvent;
```

**File:** `src/LankaConnect.Domain/Events/Entities/SignUpItem.cs`
- **Line 267:** Add `RaiseDomainEvent(new CommitmentCancelledEvent(Id, commitment.Id, userId));`

### 2. Infrastructure Layer
**File:** `src/LankaConnect.Infrastructure/Events/CommitmentCancelledEventHandler.cs` (NEW)
- Create handler as shown above

### 3. Tests
**File:** `tests/LankaConnect.Application.Tests/Events/Commands/CancelRsvp/CancelRsvpCommandHandlerTests.cs`
- Add integration test for deletion

**File:** `tests/LankaConnect.Domain.Tests/Events/Entities/SignUpItemTests.cs`
- Add unit test for domain event

---

## Why Phase 6A.34 Is NOT the Problem

Phase 6A.34 removed entity state reset to preserve Modified state for domain events:

```csharp
// Phase 6A.34 comment:
// "Do NOT reset entity state after sync - it breaks domain event detection.
//  The state reset was preventing ChangeTracker.DetectChanges() from finding
//  modified Event entities in AppDbContext.CommitAsync()"
```

**Analysis:**
- Phase 6A.34 change is **CORRECT** for its purpose
- The removal **revealed** a pre-existing issue with collection tracking
- Reverting Phase 6A.34 would break domain events
- The real issue is architectural: encapsulation vs. change tracking

---

## Long-Term Pattern

This solution establishes a pattern for **all collection modifications in aggregates:**

1. **Domain layer:** Modify private collection + raise domain event
2. **Infrastructure layer:** Handle event + mark entity state appropriately

Apply to:
- SignUpItem adding/removing commitments
- Event adding/removing images/videos
- Any aggregate with child collections

---

## References

- **Full Analysis:** `docs/architecture/ADR-008-Phase-6A28-Commitment-Deletion-Failure-Root-Cause-Analysis.md`
- **EF Core Change Tracking:** https://learn.microsoft.com/en-us/ef/core/change-tracking/
- **Phase 6A.24:** Domain Event Infrastructure
- **Phase 6A.34:** Email Group Sync State Management
- **ADR-007:** CancelRsvp Fix Attempt

---

## Next Steps

1. Review this analysis with team
2. Approve domain event approach
3. Implement changes (estimated: 2-3 hours)
4. Test locally
5. Deploy to staging
6. Verify database deletions
7. Deploy to production
8. Mark Phase 6A.28 Issue 4 as resolved
