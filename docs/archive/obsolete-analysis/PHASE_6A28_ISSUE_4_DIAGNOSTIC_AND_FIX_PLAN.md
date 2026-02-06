# Phase 6A.28 Issue 4 - Domain Event Handler Not Triggered: Diagnostic & Fix Plan

**Status:** Ready for Implementation
**Date:** 2025-12-18
**Priority:** CRITICAL
**Impact:** Commitment deletion not working in production

---

## Executive Summary

The `CommitmentCancelledEventHandler` is not being triggered in Azure staging, despite correct code implementation. This document provides:

1. **Diagnostic code** to identify the exact failure point
2. **Three ranked solution options** with complete code samples
3. **Step-by-step deployment instructions**

**Root Cause Hypothesis:** Navigation properties not loaded at runtime, causing `Event.CancelAllUserCommitments()` to execute on an empty collection.

**Recommended Solution:** Solution B (Bubble Events to Aggregate Root) - most reliable and DDD-compliant.

---

## Part 1: Diagnostic Code (Deploy First)

### File 1: EventRepository.cs

**Location:** `src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs`

**Change:** Add diagnostic logging after line 63

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

    // ADD THIS: Verify child entities loaded
    if (eventEntity != null)
    {
        var signUpListCount = eventEntity.SignUpLists.Count;
        var totalItems = eventEntity.SignUpLists.Sum(s => s.Items.Count);
        var totalCommitments = eventEntity.SignUpLists.SelectMany(s => s.Items).Sum(i => i.Commitments.Count);

        _logger.LogInformation(
            "[EventRepository] Loaded Event {EventId} with {SignUpListCount} sign-up lists, " +
            "{TotalItems} items, {TotalCommitments} commitments",
            eventEntity.Id,
            signUpListCount,
            totalItems,
            totalCommitments);
    }

    if (eventEntity != null)
    {
        // ... (existing email group sync code)
    }

    return eventEntity;
}
```

### File 2: Event.cs

**Location:** `src/LankaConnect.Domain/Events/Event.cs`

**Change:** Add diagnostic Console.WriteLine statements in `CancelAllUserCommitments()` method (line 1337)

```csharp
public Result CancelAllUserCommitments(Guid userId)
{
    if (userId == Guid.Empty)
        return Result.Failure("User ID is required");

    // ADD THESE: Diagnostic logging
    Console.WriteLine($"[CancelAllUserCommitments] STARTED for userId={userId}");
    Console.WriteLine($"[CancelAllUserCommitments] _signUpLists count: {_signUpLists.Count}");

    var cancelledCount = 0;
    var errors = new List<string>();

    foreach (var signUpList in _signUpLists)
    {
        Console.WriteLine($"[CancelAllUserCommitments] Processing list: {signUpList.Category}, Items: {signUpList.Items.Count}");

        foreach (var item in signUpList.Items)
        {
            Console.WriteLine($"[CancelAllUserCommitments] Item: {item.ItemDescription}, Commitments: {item.Commitments.Count}");

            if (item.Commitments.Any(c => c.UserId == userId))
            {
                Console.WriteLine($"[CancelAllUserCommitments] Found commitment, cancelling...");

                var result = item.CancelCommitment(userId);

                if (result.IsSuccess)
                {
                    cancelledCount++;
                    Console.WriteLine($"[CancelAllUserCommitments] SUCCESS. Domain events on item: {item.DomainEvents.Count}");
                }
                else
                {
                    errors.Add($"Failed to cancel commitment for item '{item.ItemDescription}': {result.Error}");
                    Console.WriteLine($"[CancelAllUserCommitments] FAILED: {result.Error}");
                }
            }
        }
    }

    Console.WriteLine($"[CancelAllUserCommitments] COMPLETED. Cancelled count: {cancelledCount}");

    if (cancelledCount > 0)
    {
        MarkAsUpdated();
    }

    return cancelledCount > 0 || !errors.Any()
        ? Result.Success()
        : Result.Failure($"Failed to cancel commitments: {string.Join("; ", errors)}");
}
```

### File 3: AppDbContext.cs

**Location:** `src/LankaConnect.Infrastructure/Data/AppDbContext.cs`

**Change:** Add diagnostic logging in `CommitAsync()` method before event collection (line 315)

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

    // ADD THIS: Diagnostic logging for ALL tracked entities
    var allTracked = ChangeTracker.Entries<BaseEntity>().ToList();
    _logger.LogInformation(
        "[CommitAsync] DIAGNOSTIC: Total tracked BaseEntities: {Count}",
        allTracked.Count);

    foreach (var entry in allTracked)
    {
        var entityType = entry.Entity.GetType().Name;
        var eventCount = entry.Entity.DomainEvents.Count;

        _logger.LogInformation(
            "[CommitAsync] DIAGNOSTIC: Tracked={EntityType}, Id={EntityId}, State={State}, Events={EventCount}",
            entityType,
            entry.Entity.Id,
            entry.State,
            eventCount);

        if (eventCount > 0)
        {
            var eventTypes = string.Join(", ", entry.Entity.DomainEvents.Select(e => e.GetType().Name));
            _logger.LogInformation(
                "[CommitAsync] DIAGNOSTIC: Events on {EntityType}: {EventTypes}",
                entityType,
                eventTypes);
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
        _logger.LogWarning(
            "[CommitAsync] DIAGNOSTIC: NO DOMAIN EVENTS FOUND in {TrackedCount} tracked entities",
            allTracked.Count);
    }

    // ... (rest of existing method)
}
```

### Deployment Steps for Diagnostics

1. **Apply diagnostic code changes** to all 3 files above
2. **Build:**
   ```bash
   dotnet build src/LankaConnect.API --configuration Release
   ```
3. **Deploy to Azure staging**
4. **Test commitment cancellation** via UI
5. **Check Azure logs** for diagnostic output patterns

### Expected Log Patterns

**Pattern A: Collections Not Loaded (Most Likely)**
```
[EventRepository] Loaded Event {Id} with 0 sign-up lists, 0 items, 0 commitments
[CancelAllUserCommitments] STARTED for userId={UserId}
[CancelAllUserCommitments] _signUpLists count: 0
[CancelAllUserCommitments] COMPLETED. Cancelled count: 0
[CommitAsync] DIAGNOSTIC: Total tracked BaseEntities: 2
[CommitAsync] DIAGNOSTIC: Tracked=Event, State=Modified, Events=0
[CommitAsync] DIAGNOSTIC: Tracked=Registration, State=Modified, Events=0
[CommitAsync] DIAGNOSTIC: NO DOMAIN EVENTS FOUND
```

**Action:** Implement Solution A or B below

**Pattern B: Collections Loaded but SignUpItem Not Tracked**
```
[EventRepository] Loaded Event {Id} with 1 sign-up lists, 2 items, 1 commitments
[CancelAllUserCommitments] STARTED
[CancelAllUserCommitments] _signUpLists count: 1
[CancelAllUserCommitments] Found commitment, cancelling...
[CancelAllUserCommitments] SUCCESS. Domain events on item: 1
[CancelAllUserCommitments] COMPLETED. Cancelled count: 1
[CommitAsync] DIAGNOSTIC: Total tracked BaseEntities: 2
[CommitAsync] DIAGNOSTIC: Tracked=Event, State=Modified, Events=0
[CommitAsync] DIAGNOSTIC: Tracked=Registration, State=Modified, Events=0
[CommitAsync] DIAGNOSTIC: NO DOMAIN EVENTS FOUND  ← SignUpItem missing!
```

**Action:** Implement Solution B or C below

---

## Part 2: Solution Options (Choose One)

### Solution A: Explicit Loading (Defensive Fix)

**Use when:** Navigation properties fail to load via `.Include()`

**File:** `src/LankaConnect.Application/Events/Commands/CancelRsvp/CancelRsvpCommandHandler.cs`

**Change:** Add explicit loading after line 34

```csharp
public async Task<Result> Handle(CancelRsvpCommand request, CancellationToken cancellationToken)
{
    _logger.LogInformation("[CancelRsvp] Starting cancellation...");

    var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
    if (@event == null)
    {
        return Result.Failure("Event not found");
    }

    // ADD THIS: Force explicit loading of sign-up lists
    // This ensures navigation properties are loaded even if .Include() failed
    await _context.Entry(@event)
        .Collection(e => e.SignUpLists)
        .Query()
        .Include(s => s.Items)
            .ThenInclude(i => i.Commitments)
        .LoadAsync(cancellationToken);

    _logger.LogInformation(
        "[CancelRsvp] Explicitly loaded {SignUpListCount} sign-up lists with {ItemCount} items",
        @event.SignUpLists.Count,
        @event.SignUpLists.Sum(s => s.Items.Count));

    // ... (rest of existing method)
}
```

**Pros:**
- Minimal code change
- Fixes loading issue directly
- No domain model changes

**Cons:**
- Requires `_context` injection into command handler
- Doesn't fix underlying `.Include()` issue
- Performance impact (double query)

---

### Solution B: Bubble Events to Aggregate Root (RECOMMENDED)

**Use when:** You want a reliable, DDD-compliant solution

**Why Recommended:**
- Events always collected (aggregate root always tracked)
- Follows DDD aggregate consistency boundary
- No infrastructure dependencies
- Most reliable pattern

**File:** `src/LankaConnect.Domain/Events/Event.cs`

**Change:** Update `CancelAllUserCommitments()` method (line 1337)

```csharp
public Result CancelAllUserCommitments(Guid userId)
{
    if (userId == Guid.Empty)
        return Result.Failure("User ID is required");

    var cancelledCount = 0;
    var errors = new List<string>();

    foreach (var signUpList in _signUpLists)
    {
        foreach (var item in signUpList.Items)
        {
            if (item.Commitments.Any(c => c.UserId == userId))
            {
                var result = item.CancelCommitment(userId);

                if (result.IsSuccess)
                {
                    // SOLUTION B: Bubble child events to aggregate root
                    // This ensures events are always collected because Event is always tracked
                    foreach (var childEvent in item.DomainEvents)
                    {
                        RaiseDomainEvent(childEvent);
                    }
                    item.ClearDomainEvents();

                    cancelledCount++;
                }
                else
                {
                    errors.Add($"Failed to cancel commitment for item '{item.ItemDescription}': {result.Error}");
                }
            }
        }
    }

    if (cancelledCount > 0)
    {
        MarkAsUpdated();
    }

    return cancelledCount > 0 || !errors.Any()
        ? Result.Success()
        : Result.Failure($"Failed to cancel commitments: {string.Join("; ", errors)}");
}
```

**Pros:**
- Guaranteed event collection
- DDD-compliant (aggregate manages child events)
- Simple and testable
- No infrastructure changes needed

**Cons:**
- Aggregate knows about child event types (tighter coupling)
- Must repeat pattern for other child operations

**Test After Implementation:**
```bash
dotnet test tests/LankaConnect.Domain.Tests --filter "Event.CancelAllUserCommitments"
```

---

### Solution C: Explicit Child Entity Traversal (Infrastructure Fix)

**Use when:** You want a general solution for all child entity events

**File:** `src/LankaConnect.Infrastructure/Data/AppDbContext.cs`

**Change:** Replace event collection logic in `CommitAsync()` (line 316-319)

```csharp
public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
{
    // ... (timestamp updates)

    ChangeTracker.DetectChanges();

    // SOLUTION C: Explicitly traverse ALL tracked entities for domain events
    var domainEvents = new List<IDomainEvent>();

    // Get all tracked BaseEntity entries
    var baseEntityEntries = ChangeTracker.Entries<BaseEntity>().ToList();

    foreach (var entry in baseEntityEntries)
    {
        // Collect events from this entity
        domainEvents.AddRange(entry.Entity.DomainEvents);

        // Traverse navigation properties to find child entities
        foreach (var navigation in entry.Navigations)
        {
            // Check if navigation is a collection of BaseEntity
            if (navigation.CurrentValue is IEnumerable<BaseEntity> children)
            {
                foreach (var child in children)
                {
                    // Collect events from child entity
                    domainEvents.AddRange(child.DomainEvents);

                    // RECURSIVE: Check grandchildren (e.g., SignUpItem -> SignUpCommitment)
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

    if (domainEvents.Any())
    {
        _logger.LogInformation(
            "[Phase 6A.24] Found {Count} domain events to dispatch via explicit traversal: {EventTypes}",
            domainEvents.Count,
            string.Join(", ", domainEvents.Select(e => e.GetType().Name)));
    }

    // Save changes
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

    // Clear events from ALL entities
    foreach (var entry in baseEntityEntries)
    {
        entry.Entity.ClearDomainEvents();

        // Clear child events
        foreach (var navigation in entry.Navigations)
        {
            if (navigation.CurrentValue is IEnumerable<BaseEntity> children)
            {
                foreach (var child in children)
                {
                    child.ClearDomainEvents();

                    var childEntry = ChangeTracker.Entry(child);
                    foreach (var childNav in childEntry.Navigations)
                    {
                        if (childNav.CurrentValue is IEnumerable<BaseEntity> grandchildren)
                        {
                            foreach (var grandchild in grandchildren)
                            {
                                grandchild.ClearDomainEvents();
                            }
                        }
                    }
                }
            }
        }
    }

    return result;
}
```

**Pros:**
- Fixes event collection for ALL child entities
- General solution (works for future child entity events)
- No domain model changes

**Cons:**
- More complex infrastructure code
- Performance overhead (navigation traversal)
- Requires understanding of EF Core navigation API

---

## Part 3: Deployment Plan

### Step 1: Deploy Diagnostics (REQUIRED FIRST)

1. Apply diagnostic code from Part 1 (all 3 files)
2. Build: `dotnet build src/LankaConnect.API --configuration Release`
3. Deploy to Azure staging
4. Test and review logs to confirm root cause

### Step 2: Choose Solution

Based on diagnostic logs:

- **If Pattern A (Collections Not Loaded):** Use Solution A (Explicit Loading)
- **If Pattern B (SignUpItem Not Tracked):** Use Solution B (Bubble Events) - RECOMMENDED
- **For Long-term Fix:** Use Solution C (Explicit Traversal)

### Step 3: Implement Chosen Solution

1. Apply solution code changes
2. **Remove diagnostic Console.WriteLine statements** from Part 1 (keep _logger statements)
3. Build: `dotnet build src/LankaConnect.API --configuration Release`
4. Run tests: `dotnet test`
5. Deploy to Azure staging

### Step 4: Verify Fix

1. Test commitment cancellation via UI
2. Check Azure logs for:
   - `[CommitAsync] Found {Count} domain events to dispatch`
   - `[CommitmentCancelled] Handling deletion for CommitmentId`
   - `[CommitmentCancelled] Marked commitment {Id} as deleted`
3. Verify database: commitment should be DELETED
4. Verify UI: buttons should DISAPPEAR after cancellation

### Step 5: Clean Up Diagnostics

Once fix is confirmed:

1. Remove diagnostic `_logger.LogInformation` statements with "DIAGNOSTIC" prefix
2. Keep meaningful logs (event counts, etc.)
3. Redeploy to Azure staging
4. Deploy to production

---

## Part 4: Testing Checklist

### Before Deployment
- [ ] Code compiles with 0 errors
- [ ] All unit tests pass
- [ ] Diagnostic logging is comprehensive

### After Diagnostic Deployment
- [ ] Azure logs show repository loading child entities
- [ ] Azure logs show `CancelAllUserCommitments` execution
- [ ] Azure logs show tracked entity count and types
- [ ] Root cause confirmed via log patterns

### After Solution Deployment
- [ ] Azure logs show domain events dispatched
- [ ] Azure logs show handler executed
- [ ] Database commitment is deleted
- [ ] UI buttons disappear after cancellation
- [ ] No errors or exceptions in logs

### Production Readiness
- [ ] Diagnostic code removed
- [ ] All tests pass
- [ ] Code reviewed and approved
- [ ] Deployed to staging successfully
- [ ] User acceptance testing complete

---

## Part 5: Rollback Plan

If deployed solution causes issues:

### Quick Rollback (Revert to Previous Version)
```bash
# Via Azure Portal:
# App Service → Deployment Center → Deployment History → Redeploy previous version
```

### Manual Rollback (Revert Code Changes)
```bash
git revert HEAD
git push origin develop
# Redeploy via Azure pipeline
```

---

## References

- **ADR-009:** Domain Event Collection Failure Analysis
- **ADR-008:** Commitment Deletion Failure Root Cause Analysis
- **Phase 6A.28 Docs:** Commitment Deletion Fix Documentation
- **EF Core Docs:** [Change Tracking in EF Core](https://learn.microsoft.com/en-us/ef/core/change-tracking/)
- **DDD Patterns:** [Aggregate Design Best Practices](https://martinfowler.com/bliki/DDD_Aggregate.html)
