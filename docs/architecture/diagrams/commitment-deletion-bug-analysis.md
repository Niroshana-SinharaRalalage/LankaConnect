# Registration Cancellation Commitment Deletion Bug - Visual Analysis

## Problem Visualization

### Current Broken Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                    REGISTRATION CANCELLATION REQUEST                 │
│              DeleteSignUpCommitments = true                          │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        LOAD EVENT FROM DATABASE                      │
│                                                                       │
│   EventRepository.GetByIdAsync() with DUPLICATE includes:            │
│                                                                       │
│   .Include(e => e.SignUpLists)                                       │
│       .ThenInclude(s => s.Commitments)        ◄─ PATH 1 (Legacy)    │
│   .Include(e => e.SignUpLists)                ◄─ DUPLICATE!          │
│       .ThenInclude(s => s.Items)                                     │
│           .ThenInclude(i => i.Commitments)    ◄─ PATH 2 (Current)   │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    EF CORE CHANGE TRACKER STATE                      │
│                                                                       │
│  Event entity: Tracked                                               │
│  ├─ SignUpLists: Tracked (loaded TWICE)                             │
│  │   ├─ Commitments: Tracked (legacy collection)                    │
│  │   └─ Items: Tracked                                              │
│  │       └─ Commitments: Tracked (current collection)               │
│  │                                                                    │
│  │  ⚠️  SAME SignUpCommitment entities in TWO navigation paths!      │
│  │  ⚠️  Change tracker confused about which path is authoritative    │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
┌───────────────────────────────┐   ┌────────────────────────────────┐
│   STRATEGY 1: SEPARATE QUERY  │   │   STRATEGY 2: DOMAIN METHOD    │
│                               │   │                                │
│  Query commitments directly:  │   │  @event.CancelAllUserCommits() │
│                               │   │                                │
│  var commitments = await      │   │  foreach item in signUpLists:  │
│    _dbContext.SignUpCommitments│   │    foreach commitment:        │
│    .Where(...)                │   │      _commitments.Remove()     │
│    .Join(...)                 │   │      RemainingQty += qty       │
│    .ToListAsync()             │   │                                │
│                               │   │  ✓ Removes from collection     │
│  ⚠️ Creates NEW tracked       │   │  ✓ Restores quantities         │
│     instances OR returns      │   │  ✓ Marks entity as Updated     │
│     same instances with       │   │                                │
│     conflicted state          │   │  BUT: Change tracker confused  │
│                               │   │       due to duplicate include │
└───────────────────────────────┘   └────────────────────────────────┘
                    │                               │
                    │               ┌───────────────┘
                    │               │
                    ▼               ▼
┌─────────────────────────────────────────────────────────────────────┐
│             STRATEGY 3: EXPLICIT DBCONTEXT.REMOVE()                  │
│                                                                       │
│  foreach (var item in commitmentsToDelete)                           │
│  {                                                                    │
│      _dbContext.SignUpCommitments.Remove(item.Commitment);           │
│  }                                                                    │
│                                                                       │
│  ⚠️ Trying to remove entities that were:                             │
│     1. Already removed from domain collection (Strategy 2)           │
│     2. Queried separately (Strategy 1)                               │
│     3. Tracked through duplicate navigation paths                    │
│                                                                       │
│  RESULT: Change tracker state conflict!                              │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     CHANGE TRACKER FINAL STATE                       │
│                                                                       │
│  SignUpCommitment entities:                                          │
│    State: ??? (Deleted? Modified? Unchanged?)                        │
│                                                                       │
│  EF Core confused by:                                                │
│    ❌ Same entity in multiple navigation paths                       │
│    ❌ Collection removal + Explicit Remove() conflict                │
│    ❌ Separate query entities vs navigation entities                 │
│                                                                       │
│  RESULT: Deletions NOT tracked properly                              │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        SAVECHANGES() CALLED                          │
│                                                                       │
│  EF Core generates SQL:                                              │
│    ✓ UPDATE registrations SET status = 'Cancelled'                  │
│    ✓ UPDATE events SET updated_at = NOW()                           │
│    ❌ DELETE FROM sign_up_commitments... ◄─ NOT GENERATED!          │
│                                                                       │
│  BUG: Commitments remain in database!                                │
└─────────────────────────────────────────────────────────────────────┘
```

## Solution Visualization

### Fixed Flow (Simplified, DDD-Compliant)

```
┌─────────────────────────────────────────────────────────────────────┐
│                    REGISTRATION CANCELLATION REQUEST                 │
│              DeleteSignUpCommitments = true                          │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        LOAD EVENT FROM DATABASE                      │
│                                                                       │
│   EventRepository.GetByIdAsync() with SINGLE include chain:          │
│                                                                       │
│   .Include(e => e.SignUpLists)                                       │
│       .ThenInclude(s => s.Items)                                     │
│           .ThenInclude(i => i.Commitments)    ◄─ ONE PATH ONLY      │
│                                                                       │
│   ✓ No duplicate navigation paths                                    │
│   ✓ Legacy SignUpList.Commitments NOT loaded                        │
│   ✓ Clean change tracking                                            │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    EF CORE CHANGE TRACKER STATE                      │
│                                                                       │
│  Event entity: Tracked                                               │
│  ├─ SignUpLists: Tracked                                             │
│  │   └─ Items: Tracked                                              │
│  │       └─ Commitments: Tracked (SINGLE navigation path)           │
│  │                                                                    │
│  ✓ Each SignUpCommitment in ONE path only                           │
│  ✓ Change tracker has clear ownership                               │
│  ✓ Collection removals tracked properly                             │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│             SINGLE STRATEGY: DOMAIN-DRIVEN DELETION                  │
│                                                                       │
│  var result = @event.CancelAllUserCommitments(userId);               │
│                                                                       │
│  Domain method:                                                      │
│    foreach (var signUpList in _signUpLists)                          │
│      foreach (var item in signUpList.Items)                          │
│        if (item.Commitments.Any(c => c.UserId == userId))            │
│          item.CancelCommitment(userId)                               │
│            // Removes from _commitments collection                   │
│            // Restores RemainingQuantity                             │
│            // Marks SignUpItem as Updated                            │
│                                                                       │
│  ✓ Single source of truth: domain model                             │
│  ✓ Business logic encapsulated                                       │
│  ✓ Invariants maintained                                             │
│  ✓ No competing strategies                                           │
│                                                                       │
│  ❌ NO separate query                                                │
│  ❌ NO explicit DbContext.Remove()                                   │
│  ❌ NO manual tracking                                               │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     CHANGE TRACKER FINAL STATE                       │
│                                                                       │
│  SignUpCommitment entities removed from collection:                  │
│    State: Deleted (automatically tracked by EF Core)                 │
│                                                                       │
│  SignUpItem entities:                                                │
│    State: Modified (RemainingQuantity changed)                       │
│                                                                       │
│  ✓ Clear, unambiguous state                                          │
│  ✓ Cascade delete configuration active                               │
│  ✓ All changes tracked correctly                                     │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        SAVECHANGES() CALLED                          │
│                                                                       │
│  EF Core generates SQL:                                              │
│    ✓ UPDATE registrations SET status = 'Cancelled'                  │
│    ✓ UPDATE sign_up_items SET remaining_quantity = X                │
│    ✓ DELETE FROM sign_up_commitments WHERE id IN (...)              │
│    ✓ UPDATE events SET updated_at = NOW()                           │
│                                                                       │
│  SUCCESS: All changes persisted correctly!                           │
└─────────────────────────────────────────────────────────────────────┘
```

## Entity Relationship and Navigation Paths

### Current (Broken) - Dual Navigation Paths

```
┌─────────────┐
│    Event    │
└──────┬──────┘
       │ 1
       │
       │ *
┌──────▼─────────┐
│  SignUpList    │
│                │
│  Properties:   │
│  - Category    │
│  - Description │
└────┬───────┬───┘
     │       │
     │*      │*
     │       │
     │   ┌───▼─────────────┐
     │   │   SignUpItem    │
     │   │                 │
     │   │  Properties:    │
     │   │  - Description  │
     │   │  - Quantity     │
     │   │  - Remaining    │
     │   └───┬─────────────┘
     │       │
     │       │ *
     │       │
     │   ┌───▼──────────────────┐
     │   │  SignUpCommitment    │ ◄─────── PATH 2 (Current, Active)
     │   │                      │
     │   │  Properties:         │
     │   │  - UserId            │
     │   │  - Quantity          │
     │   │  - SignUpItemId      │
     │   └──────────────────────┘
     │              ▲
     │              │
     │ *            │ SAME
     │              │ ENTITY!
     │              │
 ┌───▼──────────────┴───────┐
 │  SignUpCommitment        │ ◄─────── PATH 1 (Legacy, Deprecated)
 │                          │
 │  (Legacy Collection)     │
 │  - SignUpItemId = NULL   │
 └──────────────────────────┘

⚠️  PROBLEM: Same SignUpCommitment accessible via TWO paths!
    - Path 1: SignUpList._commitments (legacy)
    - Path 2: SignUpItem._commitments (current)

    When EF Core loads both:
    1. Duplicate tracking entries possible
    2. Collection removal on one path may not affect other
    3. Change tracker state conflicts
```

### Fixed - Single Navigation Path

```
┌─────────────┐
│    Event    │
└──────┬──────┘
       │ 1
       │
       │ *
┌──────▼─────────┐
│  SignUpList    │
│                │
│  Properties:   │
│  - Category    │
│  - Description │
│                │
│  ❌ Legacy     │
│  Commitments   │
│  NOT LOADED    │
└────────┬───────┘
         │
         │ *
         │
     ┌───▼─────────────┐
     │   SignUpItem    │
     │                 │
     │  Properties:    │
     │  - Description  │
     │  - Quantity     │
     │  - Remaining    │
     └───┬─────────────┘
         │
         │ *
         │
     ┌───▼──────────────────┐
     │  SignUpCommitment    │ ◄─────── SINGLE PATH ONLY
     │                      │
     │  Properties:         │
     │  - UserId            │
     │  - Quantity          │
     │  - SignUpItemId      │
     └──────────────────────┘

✓  SOLUTION: One navigation path to SignUpCommitment
   - Only via SignUpItem._commitments
   - Legacy path not loaded
   - Clean change tracking
   - Collection removals tracked properly
```

## Change Tracker State Comparison

### Before Fix: Confused State

```
EF Core Change Tracker Contents:

SignUpCommitment {Id: abc-123, UserId: xyz, Quantity: 2}
├─ Tracked via: Event.SignUpLists[0].Commitments (legacy)
├─ ALSO tracked via: Event.SignUpLists[0].Items[0].Commitments
├─ State: ???
└─ Confusion: Which navigation path is authoritative?

When domain removes from Items[0].Commitments:
  ❌ Change tracker may not detect (watching wrong path?)
  ❌ Other navigation path still has reference
  ❌ State conflict: Modified? Deleted? Unchanged?

When explicit DbContext.Remove() called:
  ❌ Trying to remove entity already in confused state
  ❌ May overwrite domain model's state change
  ❌ Final state: Lost deletion!
```

### After Fix: Clear State

```
EF Core Change Tracker Contents:

SignUpCommitment {Id: abc-123, UserId: xyz, Quantity: 2}
├─ Tracked via: Event.SignUpLists[0].Items[0].Commitments (ONLY PATH)
├─ State: Unchanged (initially)
└─ Clear ownership: Item[0] aggregate owns this commitment

When domain removes from Items[0].Commitments:
  ✓ Change tracker detects collection removal
  ✓ State changed to: Deleted (via cascade delete config)
  ✓ No competing navigation paths
  ✓ Clear, unambiguous state

On SaveChanges():
  ✓ DELETE FROM sign_up_commitments WHERE id = 'abc-123'
  ✓ Successfully removed from database
```

## Code Change Comparison

### EventRepository.cs

```diff
public override async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    return await _dbSet
        .Include(e => e.Images)
        .Include(e => e.Videos)
        .Include(e => e.Registrations)
-       .Include(e => e.SignUpLists)
-           .ThenInclude(s => s.Commitments)        // REMOVE: Legacy path
        .Include(e => e.SignUpLists)
            .ThenInclude(s => s.Items)
                .ThenInclude(i => i.Commitments)     // KEEP: Only path
        .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
}
```

### CancelRsvpCommandHandler.cs

```diff
if (request.DeleteSignUpCommitments)
{
    _logger.LogInformation("[CancelRsvp] User chose to delete sign-up commitments");

-   // REMOVE: Separate query approach
-   var commitmentsToDelete = await _dbContext.SignUpCommitments
-       .Where(c => c.UserId == request.UserId && c.SignUpItemId != null)
-       .Join(_dbContext.SignUpItems, ...)
-       .ToListAsync(cancellationToken);
-
-   if (commitmentsToDelete.Any())
-   {
        // KEEP: Domain method only
        var cancelResult = @event.CancelAllUserCommitments(request.UserId);

        if (cancelResult.IsFailure)
        {
            _logger.LogWarning("[CancelRsvp] Failed: {Error}", cancelResult.Error);
+           return Result.Failure(cancelResult.Error);
        }
-
-       // REMOVE: Explicit DbContext.Remove() loop
-       foreach (var item in commitmentsToDelete)
-       {
-           _dbContext.SignUpCommitments.Remove(item.Commitment);
-       }
-   }

    _logger.LogInformation("[CancelRsvp] Commitments cancelled successfully");
}
```

## DDD Principles Applied

```
┌──────────────────────────────────────────────────────────────┐
│                    DOMAIN-DRIVEN DESIGN                       │
│                                                               │
│  Event Aggregate Root                                         │
│  ├─ SignUpList Entity                                         │
│  │   └─ SignUpItem Entity                                    │
│  │       └─ SignUpCommitment Entity                          │
│  │                                                             │
│  Domain Method: CancelAllUserCommitments()                    │
│  ├─ Encapsulates business logic                              │
│  ├─ Maintains invariants (RemainingQuantity)                 │
│  ├─ Controls entity lifecycle                                │
│  └─ Single source of truth                                   │
│                                                               │
│  Application Layer:                                           │
│  ├─ Orchestrates use case                                    │
│  ├─ TRUSTS domain model                                      │
│  ├─ Does NOT duplicate business logic                        │
│  └─ Lets infrastructure handle persistence                   │
│                                                               │
│  Infrastructure Layer:                                        │
│  ├─ Configures EF Core relationships                         │
│  ├─ Tracks entity changes automatically                      │
│  ├─ Respects cascade delete configuration                    │
│  └─ Translates domain changes to SQL                         │
└──────────────────────────────────────────────────────────────┘
```

## Summary

### Root Cause
Duplicate navigation property includes + Competing deletion strategies = Change tracker confusion

### Solution
Single navigation path + Domain-driven deletion = Clear, correct behavior

### Key Takeaways
1. Never include the same navigation property twice
2. Trust the domain model
3. Use one deletion strategy, not three
4. EF Core cascade delete works when configured properly
5. Simpler code is more maintainable

### Result
- Bug fixed
- Code simplified
- Performance improved
- DDD principles enforced
- Maintainability increased
