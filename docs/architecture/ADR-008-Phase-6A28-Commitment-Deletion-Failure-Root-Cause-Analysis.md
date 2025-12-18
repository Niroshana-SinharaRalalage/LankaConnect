# ADR-008: Phase 6A.28 Commitment Deletion Failure - Root Cause Analysis

**Date:** 2025-12-18
**Status:** Analysis Complete
**Context:** Phase 6A.28 Issue 4 - Commitment deletion silently failing despite all previous fixes
**Problem:** User cancels registration with "Also delete my sign-up commitments" checkbox, but commitments persist in database

---

## Executive Summary

After applying 4 fixes to resolve commitment deletion, the operation now executes **without errors** but commitments are **silently lost** - not persisted to database. The root cause is a **subtle EF Core change tracking failure** caused by the interaction between Phase 6A.34's entity state management changes and collection modification tracking.

**Critical Finding:** EF Core's `ChangeTracker.DetectChanges()` is **NOT detecting collection removals** when entities are loaded via eager loading (`.Include()`) and modified through domain methods.

---

## Timeline of Fixes Applied

### Fix 1: Remove Duplicate .Include() (Phase 6A.28)
**File:** `EventRepository.cs` lines 27-28
**Change:** Removed duplicate `.Include(e => e.SignUpLists).ThenInclude(s => s.Commitments)`
**Result:** Fixed EF Core query bug, but deletion still failed

### Fix 2: Simplify Handler (Phase 6A.28)
**File:** `CancelRsvpCommandHandler.cs`
**Change:** Removed competing deletion strategies (55 lines → 22 lines)
**Result:** Code cleaner, but deletion still failed

### Fix 3: Add Explicit Update() Call (ADR-007)
**File:** `CancelRsvpCommandHandler.cs` line 97
**Change:** Added `_eventRepository.Update(@event);`
**Result:** Marks entity as Modified, but deletion still failed

### Fix 4: ChangeTracker.DetectChanges() (Phase 6A.24)
**File:** `AppDbContext.cs` line 313
**Change:** Force change detection before collecting domain events
**Result:** Enabled in CommitAsync(), but deletion still fails

---

## Current State Analysis

### What's Working
1. No exceptions or errors during execution
2. Domain method `Event.CancelAllUserCommitments()` executes successfully
3. `SignUpItem.CancelCommitment()` removes commitment from in-memory collection
4. `RemainingQuantity` is restored in memory
5. `_eventRepository.Update(@event)` marks Event as Modified
6. `CommitAsync()` is called and completes

### What's Failing
1. SignUpCommitment deletions are NOT persisted to database
2. Database still contains commitment rows after SaveChanges
3. UI still shows commitments after reload

---

## Root Cause: EF Core Change Tracking Gap

### The Problem

EF Core's change tracking has a **critical gap** when it comes to collection modifications in aggregates:

```csharp
// EventRepository.GetByIdAsync() - Phase 6A.34 change
public override async Task<Event?> GetByIdAsync(Guid id, ...)
{
    var eventEntity = await _dbSet
        .Include(e => e.SignUpLists)
            .ThenInclude(s => s.Items)
                .ThenInclude(i => i.Commitments)  // ← Eagerly loaded
        .FirstOrDefaultAsync(e => e.Id == id, ...);

    // Phase 6A.34: Sync email groups
    eventEntity.SyncEmailGroupIdsFromEntities(emailGroupIds);

    // CRITICAL ISSUE: Phase 6A.34 REMOVED this line:
    // _context.Entry(eventEntity).State = originalState;

    // Without state reset, entity state is now UNCHANGED (default after query)
    // Collection items are also UNCHANGED

    return eventEntity;
}
```

### Entity State After GetByIdAsync()

**Phase 6A.34 Change:** Removed entity state reset to preserve Modified state for domain events.

**Actual State:**
- Event entity: `EntityState.Unchanged` (default after FirstOrDefaultAsync)
- SignUpList entities: `EntityState.Unchanged`
- SignUpItem entities: `EntityState.Unchanged`
- **SignUpCommitment entities: `EntityState.Unchanged`**

### What Happens During Deletion

```csharp
// CancelRsvpCommandHandler.cs lines 83-97
var cancelResult = @event.CancelAllUserCommitments(request.UserId);
// ↓
// Event.CancelAllUserCommitments() (lines 1337-1383)
foreach (var signUpList in _signUpLists)
{
    foreach (var item in signUpList.Items)
    {
        var result = item.CancelCommitment(userId);  // ← Domain method
        // ↓
        // SignUpItem.CancelCommitment() (lines 258-270)
        _commitments.Remove(commitment);  // ← Collection modification
        MarkAsUpdated();  // ← Sets UpdatedAt timestamp
    }
}

// Handler continues:
_eventRepository.Update(@event);  // ← Sets Event state to Modified
```

### EF Core Change Tracking Behavior

```csharp
// AppDbContext.CommitAsync() line 313
ChangeTracker.DetectChanges();  // ← Should detect deletions

// What DetectChanges() looks for:
1. Entity state changes (Added, Modified, Deleted)  ✅ Event is Modified
2. Property value changes                           ✅ UpdatedAt changed
3. Collection additions (new items in _commitments) ✅ Would detect
4. Collection removals (items removed)              ❌ SILENTLY IGNORED
```

---

## Why Collection Removals Aren't Detected

### EF Core Collection Tracking Rules

EF Core tracks collections in TWO ways:

#### Method 1: Snapshot-based tracking (default)
- EF Core takes a snapshot of collection on load
- During `DetectChanges()`, compares current collection with snapshot
- **LIMITATION:** Only works if collection is a **change-tracked navigation property**

#### Method 2: Change tracking proxies
- Requires `UseLazyLoadingProxies()` and virtual navigation properties
- Tracks changes in real-time
- **NOT ENABLED** in this codebase

### Our Architecture

```csharp
// SignUpItem.cs (Domain Entity)
public class SignUpItem : BaseEntity
{
    private readonly List<SignUpCommitment> _commitments = new();  // ← Private backing field

    public IReadOnlyList<SignUpCommitment> Commitments => _commitments.AsReadOnly();

    public Result CancelCommitment(Guid userId)
    {
        var commitment = _commitments.FirstOrDefault(c => c.UserId == userId);
        _commitments.Remove(commitment);  // ← Direct list manipulation
        MarkAsUpdated();
        return Result.Success();
    }
}
```

**The Gap:**
1. Collection uses **private backing field** (_commitments) for encapsulation
2. EF Core can only track changes to **public navigation properties**
3. When we call `_commitments.Remove()`, EF Core's snapshot comparison **doesn't see the change**
4. `ChangeTracker.DetectChanges()` compares public `Commitments` property (IReadOnlyList)
5. IReadOnlyList wrapper **hides the mutation** from EF Core

### Configuration Check

```csharp
// SignUpItemConfiguration.cs lines 63-67
builder.HasMany(si => si.Commitments)  // ← Public property
    .WithOne()
    .HasForeignKey(sc => sc.SignUpItemId)
    .OnDelete(DeleteBehavior.Cascade);
```

**Issue:** Configuration references public `Commitments` property, but domain modifies private `_commitments` field.

---

## Phase 6A.34 Impact Analysis

### What Phase 6A.34 Changed

**Before Phase 6A.34:**
```csharp
// EventRepository.GetByIdAsync()
var originalState = _context.Entry(eventEntity).State;
eventEntity.SyncEmailGroupIdsFromEntities(emailGroupIds);
_context.Entry(eventEntity).State = originalState;  // ← Reset state
```

**After Phase 6A.34:**
```csharp
// EventRepository.GetByIdAsync()
eventEntity.SyncEmailGroupIdsFromEntities(emailGroupIds);
// REMOVED: State reset
// Reason: "The state reset was preventing ChangeTracker.DetectChanges()
//          from finding modified Event entities in AppDbContext.CommitAsync()"
```

### The Unintended Consequence

**Phase 6A.34 Goal:** Preserve entity state for domain event detection
**Actual Effect:** Entity remains in `Unchanged` state after load

**State Conflict:**
1. GetByIdAsync() returns Event with state = `Unchanged`
2. Domain method modifies collection (commitment removed)
3. `_eventRepository.Update(@event)` sets Event to `Modified`
4. **But child SignUpCommitment is still `Unchanged`**
5. EF Core doesn't know to delete it

---

## Why _eventRepository.Update() Doesn't Help

```csharp
// Repository.cs line 137
public virtual void Update(T entity)
{
    _dbSet.Update(entity);  // ← Only updates root entity state
}
```

**What `DbSet.Update()` does:**
- Sets Event entity state to `EntityState.Modified`
- **Does NOT traverse child collections**
- **Does NOT detect removed items**

**EF Core Documentation:**
> "When you call Update() on an entity that was previously tracked,
> EF Core will mark the entity and all loaded navigation properties
> as Modified. However, items that were removed from collections
> must be explicitly marked as Deleted."

---

## The Silent Failure Mechanism

### Step-by-Step Execution

1. **Load Event** (EventRepository.GetByIdAsync)
   - Event: `Unchanged`
   - SignUpCommitment: `Unchanged` (eager loaded)

2. **Domain Modification** (Event.CancelAllUserCommitments)
   - Removes commitment from `_commitments` list
   - Event.MarkAsUpdated() sets UpdatedAt
   - SignUpItem.MarkAsUpdated() sets UpdatedAt

3. **Repository Update** (CancelRsvpCommandHandler line 97)
   - `_eventRepository.Update(@event)` → Event: `Modified`
   - SignUpCommitment: **Still `Unchanged`** (not traversed)

4. **Change Detection** (AppDbContext.CommitAsync line 313)
   - `ChangeTracker.DetectChanges()` runs
   - Finds Event is `Modified` (state already set)
   - Compares public `Commitments` property (IReadOnlyList)
   - **Cannot detect items removed from private backing field**
   - SignUpCommitment: **Remains `Unchanged`**

5. **SaveChanges** (AppDbContext line 330)
   - Updates Event (UpdatedAt timestamp)
   - **Does NOT delete SignUpCommitment** (state = Unchanged)
   - Transaction commits successfully
   - **Data not deleted from database**

### Why No Errors?

- EF Core executes successfully (no constraint violations)
- Domain logic executes successfully (no exceptions)
- Transaction commits successfully (no rollbacks)
- **The deletion is simply ignored** - classic silent failure

---

## Why This Worked in Phase 6A.24 (RsvpToEventCommandHandler)

```csharp
// RsvpToEventCommandHandler - ADDING commitments
var commitment = SignUpCommitment.CreateForItem(...);
signUpItem.AddCommitment(...);  // ← ADDS to _commitments

// AppDbContext.CommitAsync
ChangeTracker.DetectChanges();  // ← Detects NEW entities
// New entities have state = Added
// EF Core DOES detect additions to collections
```

**Key Difference:**
- **Adding** to collection: EF Core detects new entities (state = Added)
- **Removing** from collection: EF Core **cannot** detect deletions (state = Unchanged)

---

## Architectural Pattern Issue

### The Encapsulation vs. Change Tracking Conflict

**Domain-Driven Design Best Practice:**
```csharp
// Encapsulate collection to enforce invariants
private readonly List<SignUpCommitment> _commitments = new();
public IReadOnlyList<SignUpCommitment> Commitments => _commitments.AsReadOnly();
```

**EF Core Change Tracking Requirement:**
```csharp
// Public mutable navigation for change detection
public ICollection<SignUpCommitment> Commitments { get; private set; }
```

**The Conflict:**
- DDD: Encapsulate collections, expose read-only interfaces
- EF Core: Needs public mutable collections for snapshot tracking

---

## Solutions Analysis

### Option 1: Explicit Entity State Management (RECOMMENDED)

Explicitly mark removed entities as Deleted:

```csharp
// SignUpItem.CancelCommitment()
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

```csharp
// Infrastructure handler
public class CommitmentCancelledEventHandler : INotificationHandler<CommitmentCancelledEvent>
{
    private readonly AppDbContext _context;

    public async Task Handle(CommitmentCancelledEvent notification, CancellationToken ct)
    {
        // Find the commitment entity in change tracker
        var commitment = await _context.SignUpCommitments
            .FindAsync(new object[] { notification.CommitmentId }, ct);

        if (commitment != null)
        {
            // Explicitly mark as deleted
            _context.SignUpCommitments.Remove(commitment);
        }
    }
}
```

**Pros:**
- Maintains domain encapsulation
- Explicit control over deletion
- Leverages existing domain event infrastructure
- Clear separation of concerns

**Cons:**
- Additional domain event and handler
- Infrastructure knows about deletion timing

### Option 2: Context Entry Manipulation in Handler (TACTICAL FIX)

```csharp
// CancelRsvpCommandHandler.cs
if (request.DeleteSignUpCommitments)
{
    var cancelResult = @event.CancelAllUserCommitments(request.UserId);

    // NEW: Explicitly mark removed commitments as Deleted
    foreach (var signUpList in @event.SignUpLists)
    {
        foreach (var item in signUpList.Items)
        {
            // Find commitments that were removed by domain logic
            var trackedCommitments = _context.Entry(item)
                .Collection(nameof(item.Commitments))
                .CurrentValue;

            var existingCommitments = trackedCommitments.ToList();
            var currentCommitments = item.Commitments.ToList();

            // Find removed commitments
            var removedCommitments = existingCommitments
                .Where(ec => !currentCommitments.Any(cc => cc.Id == ec.Id))
                .ToList();

            // Mark as deleted
            foreach (var removed in removedCommitments)
            {
                _context.Entry(removed).State = EntityState.Deleted;
            }
        }
    }

    _eventRepository.Update(@event);
}
```

**Pros:**
- Quick fix in handler
- No domain changes required
- Works with existing code

**Cons:**
- Violates separation of concerns (handler accessing context directly)
- Brittle (tight coupling to EF Core internals)
- Duplicates deletion logic outside domain

### Option 3: Repository Helper Method

```csharp
// EventRepository.cs
public void MarkCommitmentsAsDeleted(Event @event, Guid userId)
{
    foreach (var signUpList in @event.SignUpLists)
    {
        foreach (var item in signUpList.Items)
        {
            var commitmentsToDelete = _context.Entry(item)
                .Collection(nameof(item.Commitments))
                .CurrentValue?
                .Where(c => c.UserId == userId)
                .ToList() ?? new List<SignUpCommitment>();

            foreach (var commitment in commitmentsToDelete)
            {
                _context.SignUpCommitments.Remove(commitment);
            }
        }
    }
}
```

```csharp
// CancelRsvpCommandHandler.cs
if (request.DeleteSignUpCommitments)
{
    @event.CancelAllUserCommitments(request.UserId);
    _eventRepository.MarkCommitmentsAsDeleted(@event, request.UserId);
    _eventRepository.Update(@event);
}
```

**Pros:**
- Encapsulates EF Core logic in repository
- Handler remains clean
- Infrastructure concern stays in infrastructure

**Cons:**
- Repository method specific to one use case
- Still accessing collections outside domain

### Option 4: Use ICollection Instead of Private Field (NOT RECOMMENDED)

```csharp
// SignUpItem.cs - BREAKS ENCAPSULATION
public ICollection<SignUpCommitment> Commitments { get; private set; } = new List<SignUpCommitment>();

public Result CancelCommitment(Guid userId)
{
    var commitment = Commitments.FirstOrDefault(c => c.UserId == userId);
    Commitments.Remove(commitment);  // ← EF Core can track this
    MarkAsUpdated();
    return Result.Success();
}
```

**Pros:**
- EF Core automatically tracks changes
- No explicit state management needed

**Cons:**
- **BREAKS DOMAIN ENCAPSULATION**
- Exposes collection to external mutation
- Violates DDD aggregate boundaries
- Anyone can add/remove commitments

---

## Recommended Solution

**Use Option 1: Domain Event Approach**

### Implementation Steps

1. **Add Domain Event**
```csharp
// Domain/Events/DomainEvents/CommitmentCancelledEvent.cs
public record CommitmentCancelledEvent(
    Guid SignUpItemId,
    Guid CommitmentId,
    Guid UserId) : DomainEvent;
```

2. **Raise Event in Domain**
```csharp
// SignUpItem.CancelCommitment()
_commitments.Remove(commitment);
RaiseDomainEvent(new CommitmentCancelledEvent(Id, commitment.Id, userId));
MarkAsUpdated();
```

3. **Handle Event in Infrastructure**
```csharp
// Infrastructure/Events/CommitmentCancelledEventHandler.cs
public class CommitmentCancelledEventHandler : INotificationHandler<CommitmentCancelledEvent>
{
    private readonly AppDbContext _context;

    public async Task Handle(CommitmentCancelledEvent notification, CancellationToken ct)
    {
        var commitment = await _context.SignUpCommitments
            .FindAsync(new object[] { notification.CommitmentId }, ct);

        if (commitment != null)
            _context.SignUpCommitments.Remove(commitment);
    }
}
```

### Why This Is Best

1. **Maintains Separation of Concerns**
   - Domain: Business logic (remove from collection)
   - Infrastructure: Persistence (mark as deleted)

2. **Preserves Encapsulation**
   - Private backing field remains private
   - IReadOnlyList remains read-only
   - Aggregate boundaries intact

3. **Explicit and Testable**
   - Clear what happens when commitment cancelled
   - Can test domain event raised
   - Can test handler marks entity deleted

4. **Follows Existing Pattern**
   - Phase 6A.24 uses domain events for RegistrationConfirmedEvent
   - Consistent with architecture

5. **Future-Proof**
   - Can add side effects (notifications, analytics)
   - Decoupled from EF Core specifics

---

## Impact on Phase 6A.34

**Phase 6A.34 Change Is NOT the Root Cause**

The state reset removal revealed a pre-existing architectural gap:
- Before: State reset masked the change tracking issue
- After: Issue becomes visible

**Phase 6A.34 Should NOT Be Reverted** because:
1. The state reset was preventing domain events from being detected
2. The real issue is collection change tracking, not entity state
3. Fixing with domain events solves both problems properly

---

## Testing Strategy

### Unit Tests
```csharp
[Fact]
public void CancelCommitment_ShouldRaiseDomainEvent()
{
    // Arrange
    var item = SignUpItem.Create(...).Value;
    item.AddCommitment(userId, 1);

    // Act
    item.CancelCommitment(userId);

    // Assert
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
public async Task CancelCommitment_ShouldDeleteFromDatabase()
{
    // Arrange
    var @event = CreateEventWithCommitment(userId);
    await _context.SaveChangesAsync();

    // Act
    @event.CancelAllUserCommitments(userId);
    await _unitOfWork.CommitAsync();  // Triggers domain event handler

    // Assert
    var commitment = await _context.SignUpCommitments
        .FirstOrDefaultAsync(c => c.UserId == userId);

    Assert.Null(commitment);  // Should be deleted
}
```

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

4. **Phase Changes Can Reveal Existing Issues**
   - Phase 6A.34 didn't cause the bug
   - It revealed a pre-existing architectural gap

5. **Aggregate Boundaries Need Infrastructure Support**
   - Domain enforces business rules
   - Infrastructure handles persistence mechanics
   - Domain events coordinate between layers

---

## Action Items

- [ ] Implement CommitmentCancelledEvent domain event
- [ ] Create CommitmentCancelledEventHandler in infrastructure
- [ ] Update SignUpItem.CancelCommitment() to raise event
- [ ] Add unit tests for domain event
- [ ] Add integration tests for database deletion
- [ ] Update ADR to document pattern for collection deletions
- [ ] Apply same pattern to other collection modifications if needed

---

## References

- **EF Core Change Tracking:** https://learn.microsoft.com/en-us/ef/core/change-tracking/
- **EF Core Collection Navigation:** https://learn.microsoft.com/en-us/ef/core/modeling/relationships
- **ADR-007:** CancelRsvp Fix - Update() Pattern
- **Phase 6A.24:** Domain Event Infrastructure Implementation
- **Phase 6A.34:** Email Group Sync State Management

---

## Conclusion

The commitment deletion failure is caused by **EF Core's inability to detect collection removals from private backing fields**. The fix requires bridging the gap between domain encapsulation and infrastructure persistence using **domain events**.

This is an architectural pattern issue, not a bug in any single component. The solution maintains clean architecture while enabling proper change tracking.
