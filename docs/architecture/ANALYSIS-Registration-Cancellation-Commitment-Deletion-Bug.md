# ADR-007: Registration Cancellation Commitment Deletion Bug - Root Cause Analysis

**Status**: Analysis Complete
**Date**: 2025-12-17
**Category**: Bug Analysis - Critical
**Context**: Phase 6A.28 Registration Cancellation with Signup Commitment Deletion

## Executive Summary

A critical bug prevents signup commitments from being deleted when users cancel their event registration with the "delete commitments" checkbox enabled. The root cause is a **fundamental EF Core change tracking conflict** caused by attempting to track and delete the same entities through multiple access paths simultaneously.

## Problem Statement

When users cancel their event registration and choose to delete their signup commitments:
1. The domain model correctly removes commitments from in-memory collections
2. The application handler explicitly calls `DbContext.Remove()` on queried entities
3. However, **NO deletions are persisted to the database**

The issue affects user experience by leaving orphaned commitments in the database after registration cancellation.

## Root Cause Analysis

### 1. THE CRITICAL FLAW: Duplicate Include Paths (EventRepository.cs:27-31)

```csharp
public override async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    return await _dbSet
        .Include(e => e.Images)
        .Include(e => e.Videos)
        .Include(e => e.Registrations)
        .Include(e => e.SignUpLists)
            .ThenInclude(s => s.Commitments)        // Path 1: Via SignUpList
        .Include(e => e.SignUpLists)                 // DUPLICATE INCLUDE
            .ThenInclude(s => s.Items)
                .ThenInclude(i => i.Commitments)     // Path 2: Via SignUpItem
        .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
}
```

**Why This Causes the Bug**:

1. **Two Separate Collection Instances**: EF Core processes these as TWO separate navigation chains:
   - `SignUpList._commitments` (legacy collection, line 28)
   - `SignUpItem._commitments` (actual collection used, line 31)

2. **Change Tracking Confusion**: When the same `SignUpCommitment` entity exists in BOTH navigation paths, EF Core's change tracker may:
   - Create duplicate tracking entries
   - Lose deletion markers when the same entity is tracked multiple times
   - Prioritize one navigation path over the other during SaveChanges

3. **The Actual Bug**: The commitments in `SignUpList.Commitments` (legacy) are NOT the same collection references as `SignUpItem.Commitments` (current). The domain model operates on `SignUpItem._commitments`, but the duplicate include may cause EF Core to lose track of deletions.

### 2. COMPOUNDING ISSUE: Competing Deletion Strategies

The application handler uses **THREE different deletion approaches simultaneously**:

```csharp
// APPROACH 1: Query separate instances from DbContext (lines 88-100)
var commitmentsToDelete = await _dbContext.SignUpCommitments
    .Where(c => c.UserId == request.UserId && c.SignUpItemId != null)
    // ... complex join logic ...
    .ToListAsync(cancellationToken);

// APPROACH 2: Domain model removes from collection (line 107, Event.cs:1352)
var cancelResult = @event.CancelAllUserCommitments(request.UserId);
// Inside: _commitments.Remove(commitment)  // SignUpItem.cs:266

// APPROACH 3: Explicit DbContext.Remove on queried entities (line 121)
_dbContext.SignUpCommitments.Remove(item.Commitment);
```

**The Conflict**:
- `commitmentsToDelete` contains entities loaded from a SEPARATE query (lines 88-100)
- `@event` was loaded earlier with `.Include()` navigation properties (tracked entities)
- These are **DIFFERENT instances of the same entity in the change tracker**
- When you call `DbContext.Remove()` on the separately-queried entities, EF Core doesn't know they're the same as the ones removed from the domain collection

### 3. EF Core Change Tracking Behavior

From EF Core's perspective:
```
Change Tracker State:

1. Event entity loaded with tracked navigation properties:
   - SignUpItem entities tracked
   - SignUpCommitment entities tracked via SignUpItem.Commitments

2. Domain method removes from collection:
   - _commitments.Remove(commitment)
   - EF Core SHOULD mark as Deleted when collection is configured with cascade delete
   - BUT: Duplicate Include may interfere

3. Separate query loads commitments again:
   - New query creates DIFFERENT tracked instances OR
   - EF Core returns same instances but with confused state

4. Explicit Remove() called on separately-queried entities:
   - EF Core tries to mark as Deleted
   - CONFLICT: Entity state already modified by domain operation
   - Result: Deletion state is LOST or OVERWRITTEN
```

### 4. Why Domain Deletion Alone Should Work (But Doesn't)

The EF Core configuration shows cascade delete is properly configured:

```csharp
// SignUpItemConfiguration.cs:64-67
builder.HasMany(si => si.Commitments)
    .WithOne()
    .HasForeignKey(sc => sc.SignUpItemId)
    .OnDelete(DeleteBehavior.Cascade);
```

With this configuration:
- Removing from `_commitments` collection SHOULD mark entities as `Deleted`
- SaveChanges SHOULD persist the deletions
- **BUT**: The duplicate include paths interfere with this mechanism

## Architecture Decision Records

### What We Learned

1. **Never Include the Same Navigation Property Twice**: EF Core can create duplicate collection instances or confused tracking state

2. **Choose ONE Deletion Strategy**: Either:
   - Trust the domain model (remove from collections, let EF cascade)
   - OR manually track deletions in DbContext
   - NEVER mix both strategies

3. **Legacy Collections Are Dangerous**: `SignUpList._commitments` (legacy) coexists with `SignUpItem._commitments` (current), creating dual navigation paths

4. **Complex Queries vs Navigation Properties**: Separately-queried entities may conflict with navigation-loaded entities in the change tracker

## Architectural Recommendations

### Recommendation 1: Fix the Repository Include Path (HIGHEST PRIORITY)

**Problem**: Duplicate `.Include(e => e.SignUpLists)` calls create tracking conflicts

**Solution**: Consolidate into a single include chain

```csharp
public override async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    return await _dbSet
        .Include(e => e.Images)
        .Include(e => e.Videos)
        .Include(e => e.Registrations)
        .Include(e => e.SignUpLists)
            .ThenInclude(s => s.Items)
                .ThenInclude(i => i.Commitments)  // Single path to commitments
        .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
}
```

**Why This Fixes It**:
- Single navigation path to SignUpCommitment entities
- No duplicate tracking
- EF Core can properly track collection removals

**Note**: Remove the legacy `SignUpList.Commitments` include entirely since:
- `SignUpList._commitments` is legacy (line 15 in SignUpList.cs)
- All current commitments use `SignUpItem._commitments` (line 14 in SignUpItem.cs)
- No business logic uses `SignUpList.Commitments` in cancellation flow

### Recommendation 2: Use Domain-Driven Deletion ONLY

**Problem**: Three competing deletion strategies create conflicts

**Solution**: Trust the domain model exclusively

```csharp
// Phase 6A.28: Handle sign-up commitments based on user choice
if (request.DeleteSignUpCommitments)
{
    _logger.LogInformation("[CancelRsvp] User chose to delete sign-up commitments");

    // Use ONLY the domain method - let EF Core cascade handle deletions
    var cancelResult = @event.CancelAllUserCommitments(request.UserId);

    if (cancelResult.IsFailure)
    {
        _logger.LogWarning("[CancelRsvp] Failed to cancel commitments: {Error}",
            cancelResult.Error);
        return Result.Failure(cancelResult.Error);
    }

    _logger.LogInformation("[CancelRsvp] Commitments successfully cancelled via domain model");

    // NO separate query
    // NO explicit DbContext.Remove()
    // Let EF Core handle it through navigation property changes
}
```

**Why This Works**:
- Single source of truth: domain model
- EF Core tracks collection removals automatically
- No competing change tracker states
- Follows DDD principles

### Recommendation 3: Verify EF Core Tracks Collection Removals

**Add Diagnostic Logging** (temporary, for verification):

```csharp
var cancelResult = @event.CancelAllUserCommitments(request.UserId);

// Diagnostic: Check change tracker state
var deletedCommitments = _dbContext.ChangeTracker
    .Entries<SignUpCommitment>()
    .Where(e => e.State == EntityState.Deleted)
    .ToList();

_logger.LogInformation("[CancelRsvp] Change tracker shows {Count} commitments marked for deletion",
    deletedCommitments.Count);
```

**Expected Behavior**:
- After domain method call, change tracker should show commitments in `Deleted` state
- If count is 0, cascade delete configuration may be incorrect
- If count matches expected, SaveChanges should persist

## Step-by-Step Fix Plan

### Phase 1: Fix Repository Include (5 minutes)

```csharp
File: EventRepository.cs (line 20-32)

BEFORE:
.Include(e => e.SignUpLists)
    .ThenInclude(s => s.Commitments)        // REMOVE THIS
.Include(e => e.SignUpLists)                 // DUPLICATE
    .ThenInclude(s => s.Items)
        .ThenInclude(i => i.Commitments)

AFTER:
.Include(e => e.SignUpLists)
    .ThenInclude(s => s.Items)
        .ThenInclude(i => i.Commitments)     // Single path only
```

### Phase 2: Simplify Handler (10 minutes)

```csharp
File: CancelRsvpCommandHandler.cs (lines 81-135)

REMOVE:
- Lines 88-100: Separate query for commitments
- Lines 102-125: Conditional check and explicit DbContext.Remove loop

KEEP:
- Line 107: Domain method call
- Lines 109-116: Error handling

RESULT:
if (request.DeleteSignUpCommitments)
{
    _logger.LogInformation("[CancelRsvp] Deleting commitments via domain model");

    var cancelResult = @event.CancelAllUserCommitments(request.UserId);

    if (cancelResult.IsFailure)
    {
        _logger.LogWarning("[CancelRsvp] Failed: {Error}", cancelResult.Error);
        return Result.Failure(cancelResult.Error);
    }

    _logger.LogInformation("[CancelRsvp] Commitments cancelled successfully");
}
```

### Phase 3: Add Diagnostic Logging (Optional, 5 minutes)

```csharp
// After domain method, before SaveChanges
var trackedDeletions = _dbContext.ChangeTracker
    .Entries<SignUpCommitment>()
    .Where(e => e.State == EntityState.Deleted)
    .Count();

_logger.LogInformation("[CancelRsvp] Change tracker: {Count} commitments marked for deletion",
    trackedDeletions);
```

### Phase 4: Test (15 minutes)

1. Create event with signup items
2. User commits to items
3. User cancels registration with "delete commitments" checked
4. Verify: Commitments removed from database
5. Verify: RemainingQuantity restored correctly
6. Check logs for change tracker diagnostics

### Phase 5: Remove Diagnostic Logging (2 minutes)

Once verified working, remove temporary diagnostic code.

## Alternative Approaches Considered

### Alternative 1: Keep Separate Query, Remove Domain Method

```csharp
// Query commitments
var commitments = await _dbContext.SignUpCommitments
    .Where(c => c.UserId == userId)
    .ToListAsync();

// Restore quantities manually
foreach (var commitment in commitments)
{
    var item = await _dbContext.SignUpItems.FindAsync(commitment.SignUpItemId);
    item.RemainingQuantity += commitment.Quantity;
}

// Delete commitments
_dbContext.SignUpCommitments.RemoveRange(commitments);
```

**Rejected Because**:
- Violates DDD principles (domain logic in application layer)
- Duplicates business logic (quantity restoration)
- Bypasses domain invariants
- Makes domain model anemic
- No domain event raising

### Alternative 2: Use Database Triggers

```sql
CREATE TRIGGER restore_quantity_on_commitment_delete
AFTER DELETE ON sign_up_commitments
FOR EACH ROW
BEGIN
    UPDATE sign_up_items
    SET remaining_quantity = remaining_quantity + OLD.quantity
    WHERE id = OLD.sign_up_item_id;
END;
```

**Rejected Because**:
- Business logic in database layer
- Harder to test
- Domain model becomes inconsistent with database
- Violates Clean Architecture
- No domain event raising

## Testing Strategy

### Unit Tests (Domain Layer)

```csharp
[Fact]
public void CancelAllUserCommitments_RemovesCommitmentsFromCollection()
{
    // Arrange
    var @event = CreateEventWithSignUpItems();
    var userId = Guid.NewGuid();
    @event.AddSignUpCommitment(userId, itemId: signUpItem.Id, quantity: 2);

    // Act
    var result = @event.CancelAllUserCommitments(userId);

    // Assert
    result.IsSuccess.Should().BeTrue();
    signUpItem.Commitments.Should().BeEmpty();
    signUpItem.RemainingQuantity.Should().Be(originalQuantity);
}
```

### Integration Tests (Infrastructure Layer)

```csharp
[Fact]
public async Task CancelRsvp_WithDeleteCommitments_RemovesFromDatabase()
{
    // Arrange
    var @event = await CreateEventWithCommitmentsInDatabase();
    var command = new CancelRsvpCommand
    {
        EventId = @event.Id,
        UserId = userId,
        DeleteSignUpCommitments = true
    };

    // Act
    await _handler.Handle(command, CancellationToken.None);

    // Assert
    var commitmentsInDb = await _dbContext.SignUpCommitments
        .Where(c => c.UserId == userId)
        .ToListAsync();
    commitmentsInDb.Should().BeEmpty();

    // Verify quantities restored
    var item = await _dbContext.SignUpItems.FindAsync(itemId);
    item.RemainingQuantity.Should().Be(originalQuantity);
}
```

### Manual Testing Checklist

- [ ] Create event with multiple signup lists
- [ ] Add items to signup lists (Mandatory, Suggested, Open)
- [ ] User registers for event
- [ ] User commits to 3+ items across different lists
- [ ] Note original RemainingQuantity values
- [ ] User cancels registration with "Delete commitments" checked
- [ ] Verify: All commitments deleted from database
- [ ] Verify: RemainingQuantity restored for all items
- [ ] Verify: User can re-register and commit again
- [ ] Test: Cancel WITHOUT deleting commitments
- [ ] Verify: Commitments remain in database
- [ ] Verify: RemainingQuantity unchanged

## Risk Assessment

### Critical Risks Mitigated

1. **Data Integrity**: Orphaned commitments pollute database
2. **Business Logic**: Incorrect remaining quantities block future signups
3. **User Experience**: Cancelled users' names still show in signup lists
4. **Consistency**: Registration cancelled but commitments remain

### Implementation Risks

**Low Risk** because:
- Changes isolated to two files
- Domain model already correct
- Simplifying code (removing complexity)
- Well-defined test cases

**Potential Issues**:
- If legacy `SignUpList.Commitments` is used elsewhere, may break
- Need to verify no other code depends on duplicate include

## Migration Path

### Backward Compatibility

**No migration required** because:
- Database schema unchanged
- Domain model behavior unchanged
- Only fixing repository query and handler logic

**Deployment**:
- Deploy and restart
- No downtime needed
- Works immediately with existing data

### Rollback Plan

If issues occur:
1. Revert repository include change
2. Revert handler simplification
3. Restore original three-strategy approach
4. Investigate further if issue persists

## Performance Implications

### Before Fix

```
Query 1: Load Event with all includes (duplicate navigation)
Query 2: Separate complex join query for commitments
Total: 2 queries, duplicate data loading
```

### After Fix

```
Query 1: Load Event with single include chain
Total: 1 query, no duplicates
```

**Performance Improvement**:
- 50% fewer queries
- Less data loaded from database
- Faster change tracker operations
- Reduced memory footprint

## Compliance and Documentation

### Updated Documents

- [ ] This ADR (ADR-007)
- [ ] CancelRsvpCommandHandler.cs (inline comments)
- [ ] EventRepository.cs (inline comments)
- [ ] Phase 6A.28 completion notes

### Code Comments to Add

```csharp
// EventRepository.cs
// Phase 6A.28 Fix: Single include chain to avoid duplicate tracking of SignUpCommitments
// Previously had duplicate .Include(e => e.SignUpLists) which caused deletion conflicts
.Include(e => e.SignUpLists)
    .ThenInclude(s => s.Items)
        .ThenInclude(i => i.Commitments)

// CancelRsvpCommandHandler.cs
// Phase 6A.28 Fix: Trust domain model for commitment deletion
// EF Core tracks collection removals automatically when properly configured
// No need for separate query or explicit DbContext.Remove()
var cancelResult = @event.CancelAllUserCommitments(request.UserId);
```

## Conclusion

The root cause is a **perfect storm of EF Core anti-patterns**:

1. Duplicate navigation property includes create tracking confusion
2. Legacy collection navigation (`SignUpList.Commitments`) conflicts with current navigation (`SignUpItem.Commitments`)
3. Three competing deletion strategies fight for control
4. Separately-queried entities conflict with navigation-loaded entities

The fix is elegant: **Remove complexity, trust the domain model, consolidate includes**.

This is a textbook example of why DDD principles exist:
- Domain model encapsulates business logic
- Aggregate roots control invariants
- Infrastructure layer should trust domain model
- Single source of truth prevents conflicts

**Estimated Fix Time**: 30 minutes (15 min implementation + 15 min testing)

**Impact**: HIGH (fixes critical user-facing bug)

**Complexity**: LOW (simplifies code, removes anti-patterns)

---

**Next Steps**:
1. Review this analysis with development team
2. Implement Phase 1 (fix repository include)
3. Implement Phase 2 (simplify handler)
4. Test thoroughly
5. Deploy to staging
6. Monitor logs for successful deletions
7. Deploy to production

