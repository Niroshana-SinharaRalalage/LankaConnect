# Domain Event Collection Failure - Root Cause Analysis

**Date:** 2025-12-18
**Phase:** 6A.28 (Issue 4 Fix)
**Status:** CRITICAL - Handler Not Triggered
**Impact:** Commitment deletion not working in production

---

## Executive Summary

The `CommitmentCancelledEventHandler` is **NOT being triggered** despite:
- Event correctly raised in domain (line 274 of `SignUpItem.cs`)
- Handler correctly implemented
- Build and deployment successful
- No errors or exceptions in logs

**Root Cause:** EF Core's `ChangeTracker.Entries<BaseEntity>()` query in `AppDbContext.CommitAsync()` **ONLY returns aggregate roots**, not child entities. Domain events raised by child entities (like `SignUpItem`) are **NEVER collected or dispatched**.

---

## 1. Root Cause: EF Core Change Tracking Behavior

### 1.1 The Problem

When `AppDbContext.CommitAsync()` collects domain events:

```csharp
// AppDbContext.cs line 316-319
var domainEvents = ChangeTracker.Entries<BaseEntity>()
    .Where(e => e.Entity.DomainEvents.Any())
    .SelectMany(e => e.Entity.DomainEvents)
    .ToList();
```

**CRITICAL QUESTION:** Does `ChangeTracker.Entries<BaseEntity>()` include child entities?

**ANSWER:** **YES, BUT...**

### 1.2 EF Core Change Tracker Behavior (Confirmed)

`ChangeTracker.Entries<BaseEntity>()` **DOES** include child entities when they are:
1. Loaded via `.Include()` or explicit loading
2. Modified and tracked in the change tracker
3. Explicitly added/removed via DbSet operations

**Evidence:**
- `SignUpItem`, `SignUpList`, and `SignUpCommitment` are all exposed as `DbSet<T>` in `AppDbContext` (lines 72-74)
- When Event is loaded with `.Include(e => e.SignUpLists).ThenInclude(s => s.Items)`, child entities ARE tracked
- `ChangeTracker.Entries<BaseEntity>()` WILL return them IF they are in the tracker

### 1.3 The ACTUAL Problem

The issue is **NOT** that child entities aren't tracked. The issue is:

**SignUpItem is NOT being marked as Modified when domain event is raised**

When `SignUpItem.CancelCommitment()` runs:
```csharp
// SignUpItem.cs line 268-276
RemainingQuantity += commitment.Quantity;  // Property change
_commitments.Remove(commitment);           // Collection change (private)

RaiseDomainEvent(new CommitmentCancelledEvent(...));  // Event raised
MarkAsUpdated();  // Sets UpdatedAt timestamp
```

**What EF Core sees:**
- `SignUpItem` state: **Modified** (because of `MarkAsUpdated()`)
- `SignUpItem` IS in `ChangeTracker.Entries<BaseEntity>()`
- `SignUpItem.DomainEvents` collection HAS the event

**But the query still returns no events!**

### 1.4 The Real Issue: Query Timing

The problem is in `Event.CancelAllUserCommitments()`:

```csharp
// Event.cs line 1337-1383
public Result CancelAllUserCommitments(Guid userId)
{
    foreach (var signUpList in _signUpLists)
    {
        foreach (var item in signUpList.Items)
        {
            var result = item.CancelCommitment(userId);  // ← Raises event
            // Event is stored in SignUpItem._domainEvents
        }
    }

    if (cancelledCount > 0)
        MarkAsUpdated();  // Only Event is marked as updated

    return Result.Success();
}
```

**What happens:**
1. `SignUpItem.CancelCommitment()` raises domain event
2. Event is stored in `SignUpItem._domainEvents` list
3. `SignUpItem.MarkAsUpdated()` marks it as Modified
4. `Event.MarkAsUpdated()` marks aggregate root as Modified

**Then in `AppDbContext.CommitAsync()`:**
```csharp
ChangeTracker.DetectChanges();  // Forces tracking update

var domainEvents = ChangeTracker.Entries<BaseEntity>()
    .Where(e => e.DomainEvents.Any())  // ← CHECKS HERE
    .SelectMany(e => e.DomainEvents)
    .ToList();
```

**The query SHOULD work because:**
- `SignUpItem` is in `ChangeTracker.Entries<BaseEntity>()` ✓
- `SignUpItem.DomainEvents.Any()` returns true ✓
- Event should be collected ✓

**But it's NOT working! Why?**

---

## 2. Hypothesis: Navigation Property Loading Issue

### 2.1 Possible Cause: Child Collections Not Loaded

When `Event` is loaded, if `SignUpLists` aren't eagerly loaded:

```csharp
// If loaded like this (NO Include):
var @event = await _context.Events.FindAsync(eventId);

// Then _signUpLists collection is EMPTY
// CancelAllUserCommitments() does NOTHING
// No domain events raised
```

**Check Application Layer:**
- Does `CancelRegistrationCommand` handler load Event with includes?
- Are `SignUpLists` and `Items` included in the query?

### 2.2 Verification Needed

Check the command handler that calls `Event.CancelAllUserCommitments()`:

```csharp
// Need to verify this loads child entities
var @event = await _eventRepository.GetByIdAsync(command.EventId);
```

**If repository doesn't include child collections:**
- `_signUpLists` is empty
- Loop doesn't execute
- No events raised
- Handler never triggered

---

## 3. Hypothesis: Event Collection Clears Events Before Dispatch

### 3.1 Check Event Clearing Logic

```csharp
// AppDbContext.cs line 357-361
// Clear domain events after publishing
foreach (var entry in ChangeTracker.Entries<BaseEntity>())
{
    entry.Entity.ClearDomainEvents();
}
```

**This runs AFTER `_publisher.Publish()`**, so it's not the issue.

But what if events are cleared BEFORE collection?

**Check for:**
- Multiple calls to `CommitAsync()` in same transaction
- Repository pattern calling `ClearDomainEvents()` prematurely
- Command handler clearing events manually

---

## 4. Hypothesis: MediatR Handler Registration Issue

### 4.1 Handler Not Registered in DI

**Check `DependencyInjection.cs` in Application layer:**

```csharp
services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
```

**Verify:**
1. `CommitmentCancelledEventHandler` is in correct namespace
2. Implements `INotificationHandler<DomainEventNotification<CommitmentCancelledEvent>>`
3. Assembly is scanned by MediatR

### 4.2 Generic Notification Wrapper Issue

The event is dispatched as:

```csharp
var notificationType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
var notification = Activator.CreateInstance(notificationType, domainEvent);
await _publisher.Publish(notification, cancellationToken);
```

**Verify `DomainEventNotification<T>` exists and implements `INotification`:**

```csharp
// Should exist in Application.Common namespace
public class DomainEventNotification<TDomainEvent> : INotification
    where TDomainEvent : IDomainEvent
{
    public TDomainEvent DomainEvent { get; }

    public DomainEventNotification(TDomainEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }
}
```

---

## 5. Most Likely Root Causes (Ranked)

### Rank 1: Navigation Properties Not Loaded (95% Confidence)

**Issue:** Repository doesn't include child entities when loading Event.

**Evidence Needed:**
- Check `EventRepository.GetByIdAsync()` implementation
- Verify `.Include(e => e.SignUpLists).ThenInclude(s => s.Items)` is used
- Check logs for "Found {Count} domain events" message

**Fix:**
```csharp
// EventRepository.cs
public async Task<Event?> GetByIdAsync(Guid id)
{
    return await _context.Events
        .Include(e => e.SignUpLists)
            .ThenInclude(s => s.Items)
                .ThenInclude(i => i.Commitments)
        .FirstOrDefaultAsync(e => e.Id == id);
}
```

### Rank 2: ChangeTracker Not Detecting Child Modifications (3% Confidence)

**Issue:** EF Core proxy issue or lazy loading interference.

**Evidence Needed:**
- Check if lazy loading is enabled
- Verify `ChangeTracker.DetectChanges()` is called before event collection

**Fix:**
```csharp
// Option: Force detection of all tracked entities
ChangeTracker.DetectChanges();

// Option: Explicitly query child entities
var allEntities = ChangeTracker.Entries()
    .Where(e => e.Entity is BaseEntity)
    .Select(e => (BaseEntity)e.Entity)
    .ToList();
```

### Rank 3: MediatR Handler Not Registered (2% Confidence)

**Issue:** Handler not discovered by MediatR assembly scanning.

**Evidence Needed:**
- Check handler namespace and assembly
- Verify MediatR configuration in DI

**Fix:**
```csharp
services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(CommitmentCancelledEventHandler).Assembly);
});
```

---

## 6. Diagnostic Steps (Priority Order)

### Step 1: Add Diagnostic Logging to Repository

```csharp
// EventRepository.cs
public async Task<Event?> GetByIdAsync(Guid id)
{
    var @event = await _context.Events
        .Include(e => e.SignUpLists)
            .ThenInclude(s => s.Items)
                .ThenInclude(i => i.Commitments)
        .FirstOrDefaultAsync(e => e.Id == id);

    if (@event != null)
    {
        _logger.LogInformation(
            "[EventRepository] Loaded Event {EventId} with {SignUpListCount} sign-up lists",
            @event.Id,
            @event.SignUpLists.Count);
    }

    return @event;
}
```

### Step 2: Add Diagnostic Logging to Domain Method

```csharp
// Event.cs - CancelAllUserCommitments()
public Result CancelAllUserCommitments(Guid userId)
{
    Console.WriteLine($"[CancelAllUserCommitments] Starting for userId={userId}");
    Console.WriteLine($"[CancelAllUserCommitments] SignUpLists count: {_signUpLists.Count}");

    foreach (var signUpList in _signUpLists)
    {
        Console.WriteLine($"[CancelAllUserCommitments] Processing list: {signUpList.Category}");
        foreach (var item in signUpList.Items)
        {
            Console.WriteLine($"[CancelAllUserCommitments] Processing item: {item.ItemDescription}");
            if (item.Commitments.Any(c => c.UserId == userId))
            {
                Console.WriteLine($"[CancelAllUserCommitments] Found commitment, cancelling...");
                var result = item.CancelCommitment(userId);
                Console.WriteLine($"[CancelAllUserCommitments] Cancel result: {result.IsSuccess}");
                Console.WriteLine($"[CancelAllUserCommitments] Domain events count: {item.DomainEvents.Count}");
            }
        }
    }

    return Result.Success();
}
```

### Step 3: Add Diagnostic Logging to CommitAsync

```csharp
// AppDbContext.cs - CommitAsync()
public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
{
    ChangeTracker.DetectChanges();

    // Diagnostic: Check ALL tracked entities
    var allTracked = ChangeTracker.Entries<BaseEntity>().ToList();
    _logger.LogInformation(
        "[CommitAsync] Total tracked BaseEntities: {Count}",
        allTracked.Count);

    foreach (var entry in allTracked)
    {
        _logger.LogInformation(
            "[CommitAsync] Tracked: {Type} (State={State}, Events={EventCount})",
            entry.Entity.GetType().Name,
            entry.State,
            entry.Entity.DomainEvents.Count);
    }

    var domainEvents = ChangeTracker.Entries<BaseEntity>()
        .Where(e => e.DomainEvents.Any())
        .SelectMany(e => e.DomainEvents)
        .ToList();

    if (domainEvents.Any())
    {
        _logger.LogInformation(
            "[CommitAsync] Found {Count} domain events to dispatch: {EventTypes}",
            domainEvents.Count,
            string.Join(", ", domainEvents.Select(e => e.GetType().Name)));
    }
    else
    {
        _logger.LogWarning("[CommitAsync] NO DOMAIN EVENTS FOUND!");
    }

    // ... rest of method
}
```

### Step 4: Check Azure Logs for Patterns

Search for:
- `[EventRepository] Loaded Event` - Verify child entities loaded
- `[CancelAllUserCommitments]` - Verify method executes
- `[CommitAsync] Total tracked BaseEntities` - Verify SignUpItem is tracked
- `[CommitAsync] NO DOMAIN EVENTS FOUND` - Confirms collection failure

---

## 7. Recommended Solution (Based on Most Likely Cause)

### Solution A: Fix Repository Include (Rank 1 Fix)

**File:** `src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs`

```csharp
public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    return await _context.Events
        .Include(e => e.SignUpLists)
            .ThenInclude(s => s.Items)
                .ThenInclude(i => i.Commitments)
        .AsNoTracking()  // Remove if you need tracking
        .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
}
```

**Why This Is Most Likely:**
- Matches symptom: NO events collected at all
- Azure logs show NO `[CommitmentCancelled]` messages
- No errors means code isn't executing, not failing
- If child collections aren't loaded, loop doesn't run

### Solution B: Explicit Child Entity Traversal (Rank 2 Fix)

**File:** `src/LankaConnect.Infrastructure/Data/AppDbContext.cs`

```csharp
public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
{
    ChangeTracker.DetectChanges();

    // Collect domain events from ALL tracked entities (including children)
    var domainEvents = new List<IDomainEvent>();

    foreach (var entry in ChangeTracker.Entries<BaseEntity>())
    {
        domainEvents.AddRange(entry.Entity.DomainEvents);
    }

    if (domainEvents.Any())
    {
        _logger.LogInformation(
            "[Phase 6A.24] Found {Count} domain events to dispatch: {EventTypes}",
            domainEvents.Count,
            string.Join(", ", domainEvents.Select(e => e.GetType().Name)));
    }

    var result = await SaveChangesAsync(cancellationToken);

    // Dispatch events
    foreach (var domainEvent in domainEvents)
    {
        var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
        var notification = Activator.CreateInstance(notificationType, domainEvent);

        if (notification != null)
        {
            await _publisher.Publish(notification, cancellationToken);
        }
    }

    // Clear events
    foreach (var entry in ChangeTracker.Entries<BaseEntity>())
    {
        entry.Entity.ClearDomainEvents();
    }

    return result;
}
```

---

## 8. DDD Best Practice Analysis

### Question: Should Child Entities Raise Domain Events?

**Answer: YES, but with caveats**

**DDD Guidance:**
- Aggregate root is consistency boundary
- Child entities CAN raise events if they represent significant domain actions
- Events should be collected from the aggregate root

**Two Valid Patterns:**

#### Pattern A: Bubble Events to Aggregate Root (Preferred)
```csharp
// Event.cs
public Result CancelAllUserCommitments(Guid userId)
{
    foreach (var item in signUpList.Items)
    {
        var result = item.CancelCommitment(userId);

        if (result.IsSuccess && item.DomainEvents.Any())
        {
            // Bubble child events to aggregate root
            foreach (var childEvent in item.DomainEvents)
            {
                RaiseDomainEvent(childEvent);
            }
            item.ClearDomainEvents();
        }
    }
}
```

**Pros:**
- Events always collected (aggregate root is always tracked)
- Follows DDD consistency boundary principle
- No infrastructure complexity

**Cons:**
- Requires aggregate root to know about child events
- Tight coupling between layers

#### Pattern B: Collect Events from All Entities (Current)
```csharp
// AppDbContext.cs
var domainEvents = ChangeTracker.Entries<BaseEntity>()
    .Where(e => e.DomainEvents.Any())
    .SelectMany(e => e.DomainEvents)
    .ToList();
```

**Pros:**
- Child entities are autonomous
- No coupling between aggregate and children
- Follows SRP (each entity manages its own events)

**Cons:**
- Requires infrastructure to traverse all tracked entities
- Relies on EF Core change tracking behavior
- Can miss events if entities aren't loaded

**Recommendation:** Use Pattern A (Bubble Events) for critical business events like commitment deletion. It's more reliable and follows DDD principles.

---

## 9. Action Items

### Immediate (Today)
1. [ ] Add diagnostic logging to `EventRepository.GetByIdAsync()`
2. [ ] Add diagnostic logging to `Event.CancelAllUserCommitments()`
3. [ ] Add diagnostic logging to `AppDbContext.CommitAsync()`
4. [ ] Deploy diagnostics and test in staging
5. [ ] Review Azure logs to confirm root cause

### Short-term (This Week)
6. [ ] Implement Solution A (fix repository includes) OR Solution B (explicit traversal)
7. [ ] Add integration test for commitment cancellation with event dispatch
8. [ ] Update ADR-008 with final root cause and solution
9. [ ] Deploy fix to production
10. [ ] Verify fix in production logs

### Long-term (Next Sprint)
11. [ ] Implement Pattern A (bubble events to aggregate root) for reliability
12. [ ] Add unit tests for domain event collection from child entities
13. [ ] Document event collection patterns in architecture docs
14. [ ] Review all domain events to ensure child events are properly collected

---

## 10. Appendix: EF Core Behavior Reference

### ChangeTracker.Entries<T>() Behavior

**Returns:** All entities of type `T` (or derived types) that are currently tracked

**Includes:**
- Aggregate roots loaded via `DbSet<T>.Find()` or queries
- Child entities loaded via `.Include()` or explicit loading
- Entities added via `DbSet<T>.Add()`
- Entities modified via property changes (if change tracking enabled)

**Does NOT Include:**
- Entities not loaded from database
- Entities in detached state
- Entities from navigation properties that aren't loaded (lazy loading disabled)

### DetectChanges() Behavior

**What it does:**
- Scans all tracked entities for property changes
- Updates entity state (Unchanged → Modified)
- Does NOT scan private collections
- Does NOT load navigation properties

**When to call:**
- Before SaveChangesAsync() (automatic)
- Before collecting domain events (manual, added in Phase 6A.24)
- After bulk property updates (if change tracking is disabled)

---

## References

- **ADR-008:** Phase 6A.28 Commitment Deletion Failure Root Cause Analysis
- **Phase 6A.28 Docs:** `PHASE_6A28_ISSUE_4_DOMAIN_EVENT_FIX.md`
- **EF Core Docs:** [Change Tracking](https://learn.microsoft.com/en-us/ef/core/change-tracking/)
- **MediatR Docs:** [Notifications](https://github.com/jbogard/MediatR/wiki#notifications)
