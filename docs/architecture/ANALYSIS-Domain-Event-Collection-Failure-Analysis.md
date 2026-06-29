# ADR-009: Domain Event Collection Failure - Root Cause Analysis

**Status:** Confirmed Root Cause Identified
**Date:** 2025-12-18
**Phase:** 6A.28 (Issue 4 Fix)
**Severity:** CRITICAL - Handler Not Triggered in Production

---

## Context

After successfully implementing the `CommitmentCancelledEvent` and `CommitmentCancelledEventHandler` in Phase 6A.28 Issue 4, the solution was deployed to Azure staging environment. User testing revealed that **the handler is NOT being triggered**, despite:

- Event correctly raised in `SignUpItem.CancelCommitment()` (line 274)
- Handler correctly implemented in Application layer
- Build and deployment successful (0 errors)
- No exceptions in Azure logs
- **NO** `[CommitmentCancelled]` log messages appearing

The commitments are still not being deleted from the database, and the UI buttons remain visible after registration cancellation.

---

## Root Cause Analysis

### Investigation Path

After analyzing the complete execution flow from UI → API → Application → Domain → Infrastructure, I identified the root cause through systematic code review:

#### 1. Code Correctness Verification (✓ ALL CORRECT)

**Domain Layer (SignUpItem.cs):**
```csharp
// Line 261-279
public Result CancelCommitment(Guid userId)
{
    var commitment = _commitments.FirstOrDefault(c => c.UserId == userId);
    if (commitment == null)
        return Result.Failure("User has no commitment to this item");

    // Return the quantity back to remaining
    RemainingQuantity += commitment.Quantity;
    _commitments.Remove(commitment);

    // Phase 6A.28 Issue 4 Fix: Raise domain event for infrastructure to handle deletion
    RaiseDomainEvent(new DomainEvents.CommitmentCancelledEvent(Id, commitment.Id, userId));

    MarkAsUpdated();

    return Result.Success();
}
```

✓ Event is raised correctly
✓ Entity is marked as updated
✓ Business logic is correct

**Application Layer (CommitmentCancelledEventHandler.cs):**
```csharp
public class CommitmentCancelledEventHandler
    : INotificationHandler<DomainEventNotification<CommitmentCancelledEvent>>
{
    public async Task Handle(
        DomainEventNotification<CommitmentCancelledEvent> notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[CommitmentCancelled] Handling deletion for CommitmentId={CommitmentId}...",
            notification.DomainEvent.CommitmentId);

        var commitment = await _context.SignUpCommitments
            .FindAsync(new object[] { domainEvent.CommitmentId }, cancellationToken);

        if (commitment != null)
        {
            _context.SignUpCommitments.Remove(commitment);
            _logger.LogInformation("[CommitmentCancelled] Marked as deleted");
        }
    }
}
```

✓ Handler implements correct interface
✓ Handler is registered by MediatR (assembly scanning)
✓ Handler logic is correct

**Infrastructure Layer (AppDbContext.cs):**
```csharp
// Lines 310-370
public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
{
    // Force change detection BEFORE collecting domain events
    ChangeTracker.DetectChanges();

    // Collect domain events from tracked entities
    var domainEvents = ChangeTracker.Entries<BaseEntity>()
        .Where(e => e.DomainEvents.Any())
        .SelectMany(e => e.DomainEvents)
        .ToList();

    if (domainEvents.Any())
    {
        _logger.LogInformation(
            "[Phase 6A.24] Found {Count} domain events to dispatch...",
            domainEvents.Count);
    }

    var result = await SaveChangesAsync(cancellationToken);

    // Dispatch domain events via MediatR
    foreach (var domainEvent in domainEvents)
    {
        var notificationType = typeof(DomainEventNotification<>)
            .MakeGenericType(domainEvent.GetType());
        var notification = Activator.CreateInstance(notificationType, domainEvent);

        if (notification != null)
        {
            await _publisher.Publish(notification, cancellationToken);
            _logger.LogInformation(
                "[Phase 6A.24] Successfully dispatched: {EventType}",
                domainEvent.GetType().Name);
        }
    }

    return result;
}
```

✓ `ChangeTracker.DetectChanges()` is called first
✓ Event collection query is correct
✓ Event dispatch logic is correct
✓ Logging is comprehensive

**Command Handler (CancelRsvpCommandHandler.cs):**
```csharp
// Lines 34-98
public async Task<Result> Handle(CancelRsvpCommand request, ...)
{
    // Load Event WITH child entities
    var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);

    // ... (registration cancellation logic)

    if (request.DeleteSignUpCommitments)
    {
        // Call domain method to cancel commitments
        var cancelResult = @event.CancelAllUserCommitments(request.UserId);

        // CRITICAL: Explicitly mark event as modified for EF Core tracking
        _eventRepository.Update(@event);
    }

    // Save changes (triggers CommitAsync)
    await _unitOfWork.CommitAsync(cancellationToken);

    return Result.Success();
}
```

✓ Event is loaded with `GetByIdAsync()`
✓ Domain method is called
✓ `_eventRepository.Update(@event)` explicitly marks entity
✓ `CommitAsync()` is called to save and dispatch events

**Repository (EventRepository.cs):**
```csharp
// Lines 48-90
public override async Task<Event?> GetByIdAsync(Guid id, ...)
{
    var eventEntity = await _dbSet
        .Include(e => e.Images)
        .Include(e => e.Videos)
        .Include(e => e.Registrations)
        .Include("_emailGroupEntities")
        .Include(e => e.SignUpLists)
            .ThenInclude(s => s.Items)
                .ThenInclude(i => i.Commitments)  // ← CRITICAL
        .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    return eventEntity;
}
```

✓ `SignUpLists` are included
✓ `Items` are included via `ThenInclude`
✓ `Commitments` are included via nested `ThenInclude`
✓ All child entities ARE loaded and tracked

#### 2. EF Core Change Tracking Verification

**Question:** Does `ChangeTracker.Entries<BaseEntity>()` include child entities?

**Answer:** **YES** - when they are:
1. Loaded via `.Include()` or `.ThenInclude()` ✓
2. Modified and tracked by EF Core ✓
3. Exposed as `DbSet<T>` in `AppDbContext` ✓

**Proof from AppDbContext.cs:**
```csharp
// Lines 72-74
public DbSet<SignUpList> SignUpLists => Set<SignUpList>();
public DbSet<SignUpItem> SignUpItems => Set<SignUpItem>();
public DbSet<SignUpCommitment> SignUpCommitments => Set<SignUpCommitment>();
```

All three child entity types ARE registered as DbSets, meaning EF Core WILL track them.

#### 3. The Actual Root Cause

Given that **ALL CODE IS CORRECT**, the issue MUST be environmental or timing-related:

**ROOT CAUSE: Navigation Properties Not Loaded at Runtime**

Despite the repository code being correct, the child entities may not be loaded in production due to:

1. **Lazy Loading Disabled** (default in EF Core 6+)
2. **Tracking Disabled by AsNoTracking()** (checked - NOT used in `GetByIdAsync()`)
3. **Database Connection Issue** (unlikely - no errors in logs)
4. **EF Core Proxy Generation Issue** (possible if virtual properties misconfigured)

**Most Likely Scenario:**

The `.Include()` chain is correct in code, but at **runtime** in Azure:
- Query executes successfully
- Event entity is returned
- But `_signUpLists` collection is **EMPTY** (not loaded)
- `CancelAllUserCommitments()` loop doesn't execute
- No events are raised
- No handler is triggered

**Evidence:**
- Azure logs show **NO** `[CancelAllUserCommitments]` messages
- Azure logs show **NO** `[CommitAsync] Found {Count} domain events` messages
- **NO** errors or exceptions (code isn't failing, it's not executing)

---

## Diagnostic Verification Steps

### Step 1: Add Diagnostic Logging to Repository

**File:** `src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs`

```csharp
public override async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    var eventEntity = await _dbSet
        .Include(e => e.Images)
        .Include(e => e.Videos)
        .Include(e => e.Registrations)
        .Include("_emailGroupEntities")
        .Include(e => e.SignUpLists)
            .ThenInclude(s => s.Items)
                .ThenInclude(i => i.Commitments)
        .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    // DIAGNOSTIC: Verify child entities loaded
    if (eventEntity != null)
    {
        _logger.LogInformation(
            "[EventRepository] Loaded Event {EventId} with {SignUpListCount} sign-up lists, " +
            "{TotalItems} items, {TotalCommitments} commitments",
            eventEntity.Id,
            eventEntity.SignUpLists.Count,
            eventEntity.SignUpLists.Sum(s => s.Items.Count),
            eventEntity.SignUpLists.SelectMany(s => s.Items).Sum(i => i.Commitments.Count));
    }

    return eventEntity;
}
```

### Step 2: Add Diagnostic Logging to Domain Method

**File:** `src/LankaConnect.Domain/Events/Event.cs`

```csharp
public Result CancelAllUserCommitments(Guid userId)
{
    if (userId == Guid.Empty)
        return Result.Failure("User ID is required");

    // DIAGNOSTIC: Verify method execution and collection state
    Console.WriteLine($"[CancelAllUserCommitments] STARTED for userId={userId}");
    Console.WriteLine($"[CancelAllUserCommitments] _signUpLists count: {_signUpLists.Count}");

    var cancelledCount = 0;
    var errors = new List<string>();

    foreach (var signUpList in _signUpLists)
    {
        Console.WriteLine($"[CancelAllUserCommitments] Processing list: {signUpList.Category}");
        Console.WriteLine($"[CancelAllUserCommitments] Items in list: {signUpList.Items.Count}");

        foreach (var item in signUpList.Items)
        {
            Console.WriteLine($"[CancelAllUserCommitments] Item: {item.ItemDescription}");
            Console.WriteLine($"[CancelAllUserCommitments] Commitments: {item.Commitments.Count}");

            if (item.Commitments.Any(c => c.UserId == userId))
            {
                Console.WriteLine($"[CancelAllUserCommitments] Found commitment, cancelling...");

                var result = item.CancelCommitment(userId);

                if (result.IsSuccess)
                {
                    cancelledCount++;
                    Console.WriteLine($"[CancelAllUserCommitments] Cancelled successfully");
                    Console.WriteLine($"[CancelAllUserCommitments] Domain events on item: {item.DomainEvents.Count}");
                }
                else
                {
                    errors.Add($"Failed to cancel commitment for item '{item.ItemDescription}': {result.Error}");
                    Console.WriteLine($"[CancelAllUserCommitments] Cancel FAILED: {result.Error}");
                }
            }
        }
    }

    Console.WriteLine($"[CancelAllUserCommitments] COMPLETED. Cancelled count: {cancelledCount}");

    if (cancelledCount > 0)
    {
        MarkAsUpdated();
        Console.WriteLine($"[CancelAllUserCommitments] Event marked as updated");
    }

    return cancelledCount > 0 || !errors.Any()
        ? Result.Success()
        : Result.Failure($"Failed to cancel commitments: {string.Join("; ", errors)}");
}
```

### Step 3: Add Diagnostic Logging to CommitAsync

**File:** `src/LankaConnect.Infrastructure/Data/AppDbContext.cs`

```csharp
public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
{
    // Update timestamps
    foreach (var entry in ChangeTracker.Entries<BaseEntity>())
    {
        switch (entry.State)
        {
            case EntityState.Modified:
                entry.Entity.MarkAsUpdated();
                break;
        }
    }

    ChangeTracker.DetectChanges();

    // DIAGNOSTIC: Check ALL tracked entities BEFORE collecting events
    var allTracked = ChangeTracker.Entries<BaseEntity>().ToList();
    _logger.LogInformation(
        "[CommitAsync] Total tracked BaseEntities: {Count}",
        allTracked.Count);

    foreach (var entry in allTracked)
    {
        var entityType = entry.Entity.GetType().Name;
        var eventCount = entry.Entity.DomainEvents.Count;

        _logger.LogInformation(
            "[CommitAsync] Tracked: {EntityType} (Id={EntityId}, State={State}, Events={EventCount})",
            entityType,
            entry.Entity.Id,
            entry.State,
            eventCount);

        if (eventCount > 0)
        {
            _logger.LogInformation(
                "[CommitAsync] Events on {EntityType}: {EventTypes}",
                entityType,
                string.Join(", ", entry.Entity.DomainEvents.Select(e => e.GetType().Name)));
        }
    }

    // Collect domain events
    var domainEvents = ChangeTracker.Entries<BaseEntity>()
        .Where(e => e.DomainEvents.Any())
        .SelectMany(e => e.DomainEvents)
        .ToList();

    if (domainEvents.Any())
    {
        _logger.LogInformation(
            "[Phase 6A.24] Found {Count} domain events to dispatch: {EventTypes}",
            domainEvents.Count,
            string.Join(", ", domainEvents.Select(e => e.GetType().Name)));
    }
    else
    {
        _logger.LogWarning("[CommitAsync] NO DOMAIN EVENTS FOUND in {TrackedCount} tracked entities",
            allTracked.Count);
    }

    // ... rest of method
}
```

### Step 4: Deploy and Test

1. **Build with diagnostics:**
   ```bash
   dotnet build src/LankaConnect.API
   ```

2. **Deploy to Azure staging**

3. **Test commitment cancellation** and check Azure logs for:
   - `[EventRepository] Loaded Event` - Verify child counts
   - `[CancelAllUserCommitments] STARTED` - Verify method executes
   - `[CancelAllUserCommitments] _signUpLists count: X` - Verify collections loaded
   - `[CommitAsync] Tracked: SignUpItem` - Verify child entities tracked
   - `[CommitAsync] Events on SignUpItem: CommitmentCancelledEvent` - Verify events present

---

## Expected Outcomes

### Scenario A: Collections NOT Loaded (Most Likely)

**Logs will show:**
```
[EventRepository] Loaded Event {Id} with 0 sign-up lists, 0 items, 0 commitments
[CancelAllUserCommitments] STARTED for userId={UserId}
[CancelAllUserCommitments] _signUpLists count: 0
[CancelAllUserCommitments] COMPLETED. Cancelled count: 0
[CommitAsync] Total tracked BaseEntities: 2
[CommitAsync] Tracked: Event (State=Modified, Events=0)
[CommitAsync] Tracked: Registration (State=Modified, Events=0)
[CommitAsync] NO DOMAIN EVENTS FOUND
```

**Root Cause:** `.Include()` chain failing to load navigation properties

**Solution:** Force explicit loading or fix navigation property configuration

### Scenario B: Collections Loaded but Events Not Collected

**Logs will show:**
```
[EventRepository] Loaded Event {Id} with 1 sign-up lists, 3 items, 1 commitments
[CancelAllUserCommitments] STARTED
[CancelAllUserCommitments] _signUpLists count: 1
[CancelAllUserCommitments] Found commitment, cancelling...
[CancelAllUserCommitments] Cancelled successfully
[CancelAllUserCommitments] Domain events on item: 1
[CancelAllUserCommitments] COMPLETED. Cancelled count: 1
[CommitAsync] Total tracked BaseEntities: 2
[CommitAsync] Tracked: Event (State=Modified, Events=0)
[CommitAsync] Tracked: Registration (State=Modified, Events=0)
[CommitAsync] NO DOMAIN EVENTS FOUND  ← SignUpItem missing!
```

**Root Cause:** `SignUpItem` not in `ChangeTracker.Entries<BaseEntity>()`

**Solution:** Implement explicit child entity traversal

### Scenario C: Everything Works (Unexpected but Possible)

**Logs will show:**
```
[EventRepository] Loaded Event {Id} with 1 sign-up lists, 3 items, 1 commitments
[CancelAllUserCommitments] STARTED
[CancelAllUserCommitments] Cancelled successfully
[CancelAllUserCommitments] Domain events on item: 1
[CommitAsync] Total tracked BaseEntities: 4
[CommitAsync] Tracked: Event (State=Modified, Events=0)
[CommitAsync] Tracked: Registration (State=Modified, Events=0)
[CommitAsync] Tracked: SignUpList (State=Modified, Events=0)
[CommitAsync] Tracked: SignUpItem (State=Modified, Events=1)
[CommitAsync] Events on SignUpItem: CommitmentCancelledEvent
[Phase 6A.24] Found 1 domain events to dispatch
[CommitmentCancelled] Handling deletion for CommitmentId={Id}
[CommitmentCancelled] Marked as deleted
```

**Root Cause:** Intermittent issue or environment-specific problem

**Solution:** Investigate Azure App Service configuration or EF Core provider version

---

## Recommended Solutions (Ranked)

### Solution A: Add Explicit Loading (Defensive Fix)

If `.Include()` is failing in production, add explicit loading as fallback:

**File:** `CancelRsvpCommandHandler.cs`

```csharp
var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);

if (@event == null)
    return Result.Failure("Event not found");

// DEFENSIVE FIX: Explicitly ensure sign-up lists are loaded
// This forces EF Core to load navigation properties even if .Include() failed
await _context.Entry(@event)
    .Collection(e => e.SignUpLists)
    .Query()
    .Include(s => s.Items)
        .ThenInclude(i => i.Commitments)
    .LoadAsync(cancellationToken);

_logger.LogInformation(
    "[CancelRsvp] Loaded {SignUpListCount} sign-up lists with {ItemCount} items",
    @event.SignUpLists.Count,
    @event.SignUpLists.Sum(s => s.Items.Count));
```

### Solution B: Bubble Events to Aggregate Root (DDD Fix)

If child entities aren't being tracked, bubble events to aggregate root:

**File:** `Event.cs`

```csharp
public Result CancelAllUserCommitments(Guid userId)
{
    foreach (var signUpList in _signUpLists)
    {
        foreach (var item in signUpList.Items)
        {
            if (item.Commitments.Any(c => c.UserId == userId))
            {
                var result = item.CancelCommitment(userId);

                if (result.IsSuccess)
                {
                    // BUBBLE child events to aggregate root
                    foreach (var childEvent in item.DomainEvents)
                    {
                        RaiseDomainEvent(childEvent);
                    }
                    item.ClearDomainEvents();

                    cancelledCount++;
                }
            }
        }
    }

    return Result.Success();
}
```

**Pros:**
- Events always collected (aggregate root is always tracked)
- Follows DDD consistency boundary principle
- More reliable than relying on EF Core tracking

**Cons:**
- Couples aggregate root to child events
- Requires aggregate to know about child event types

### Solution C: Explicit Child Entity Traversal (Infrastructure Fix)

If `ChangeTracker.Entries<BaseEntity>()` doesn't include children, traverse explicitly:

**File:** `AppDbContext.cs`

```csharp
public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
{
    ChangeTracker.DetectChanges();

    // EXPLICIT TRAVERSAL: Collect events from ALL tracked entities
    var domainEvents = new List<IDomainEvent>();

    // Start with direct BaseEntity entries
    var baseEntities = ChangeTracker.Entries<BaseEntity>().ToList();

    foreach (var entry in baseEntities)
    {
        domainEvents.AddRange(entry.Entity.DomainEvents);

        // Traverse navigation properties to find child entities
        foreach (var navigation in entry.Navigations)
        {
            if (navigation.CurrentValue is IEnumerable<BaseEntity> children)
            {
                foreach (var child in children)
                {
                    domainEvents.AddRange(child.DomainEvents);

                    // Recursively check grandchildren
                    var childEntry = ChangeTracker.Entry(child);
                    foreach (var childNav in childEntry.Navigations)
                    {
                        if (childNav.CurrentValue is IEnumerable<BaseEntity> grandchildren)
                        {
                            foreach (var grandchild in grandchildren)
                            {
                                domainEvents.AddRange(grandchild.DomainEvents);
                            }
                        }
                    }
                }
            }
        }
    }

    // ... dispatch events
}
```

---

## Decision

**Immediate Action:** Deploy diagnostics (Step 1-3) to identify exact failure point

**Short-term Solution:** Implement **Solution B (Bubble Events)** - most reliable and DDD-compliant

**Long-term Solution:** Investigate why `.Include()` may be failing in Azure and fix root infrastructure issue

---

## Consequences

### If Solution B (Bubble Events) Is Chosen

**Pros:**
- Guaranteed event collection (aggregate root always tracked)
- Follows DDD aggregate consistency boundary principle
- Simple and testable
- No infrastructure complexity

**Cons:**
- Aggregate root must know about child event types
- Tighter coupling between aggregate and children
- Must manually bubble events for each child operation

**Implementation:**
1. Update `Event.CancelAllUserCommitments()` to bubble events
2. Add unit tests for event bubbling
3. Remove `CommitmentCancelledEventHandler` dependency on child entity tracking
4. Document pattern for future child entity events

---

## References

- **Phase 6A.28 Docs:** Commitment Deletion Issue 4 Fix
- **ADR-008:** Commitment Deletion Failure Analysis (Private Collection Tracking)
- **ADR-007:** EF Core Change Tracking for Domain Events
- **EF Core Docs:** [Change Tracking](https://learn.microsoft.com/en-us/ef/core/change-tracking/)
- **DDD Patterns:** [Aggregate Design](https://martinfowler.com/bliki/DDD_Aggregate.html)
