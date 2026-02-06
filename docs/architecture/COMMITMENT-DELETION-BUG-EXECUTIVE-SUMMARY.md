# Registration Cancellation Commitment Deletion Bug - Executive Summary

**Status**: Root Cause Identified - Fix Ready
**Severity**: HIGH (User-facing data integrity bug)
**Estimated Fix Time**: 30 minutes
**Risk Level**: LOW (simplifies code)

## The Problem

When users cancel event registration with the "delete commitments" checkbox enabled, their signup commitments are **NOT deleted from the database** despite appearing to be removed in the UI.

## Root Cause

**Three EF Core anti-patterns working together**:

1. **Duplicate Navigation Property Includes** (EventRepository.cs:27-31)
   ```csharp
   .Include(e => e.SignUpLists).ThenInclude(s => s.Commitments)        // Path 1
   .Include(e => e.SignUpLists).ThenInclude(s => s.Items)              // Duplicate!
       .ThenInclude(i => i.Commitments)                                 // Path 2
   ```
   - Same `SignUpCommitment` entities tracked via TWO different navigation paths
   - EF Core change tracker gets confused about which path is authoritative

2. **Three Competing Deletion Strategies** (CancelRsvpCommandHandler.cs:88-124)
   - Strategy 1: Separate database query loads commitments
   - Strategy 2: Domain model removes from collection
   - Strategy 3: Explicit `DbContext.Remove()` on queried entities
   - These strategies CONFLICT with each other in the change tracker

3. **Change Tracker State Conflict**
   - Domain method removes from collection
   - Explicit Remove() tries to delete separately-queried entities
   - EF Core loses track of deletions due to competing state changes

## The Fix

**Simplify and trust the domain model:**

### Change 1: Fix Repository Include (EventRepository.cs)

```csharp
// REMOVE the duplicate include, keep only ONE path:
.Include(e => e.SignUpLists)
    .ThenInclude(s => s.Items)
        .ThenInclude(i => i.Commitments)  // Single navigation path
```

### Change 2: Simplify Handler (CancelRsvpCommandHandler.cs)

```csharp
if (request.DeleteSignUpCommitments)
{
    // ONLY use domain method - remove complex query and explicit DbContext.Remove
    var cancelResult = @event.CancelAllUserCommitments(request.UserId);

    if (cancelResult.IsFailure)
        return Result.Failure(cancelResult.Error);
}
```

## Why This Works

1. **Single Navigation Path**: No duplicate tracking, clear ownership
2. **Domain Model Authority**: Single source of truth for business logic
3. **EF Core Cascade Delete**: Automatically tracks collection removals
4. **No Competing Strategies**: One clear deletion approach

## Files to Change

| File | Lines | Change |
|------|-------|--------|
| `EventRepository.cs` | 27-31 | Remove duplicate `.Include(e => e.SignUpLists)` |
| `CancelRsvpCommandHandler.cs` | 88-124 | Remove separate query and explicit DbContext.Remove loop |

## Benefits

- **Fixes bug**: Commitments properly deleted from database
- **Simplifies code**: Removes 30+ lines of complex query logic
- **Improves performance**: 50% fewer database queries
- **Follows DDD**: Domain model controls entity lifecycle
- **Reduces maintenance**: Less code = fewer bugs

## Testing Checklist

- [ ] User commits to signup items
- [ ] User cancels registration with "delete commitments" checked
- [ ] Verify: Commitments removed from database
- [ ] Verify: RemainingQuantity restored correctly
- [ ] Verify: User can re-register and commit again
- [ ] Test: Cancel WITHOUT deleting commitments still works

## Risk Assessment

**LOW RISK** because:
- Simplifies code (removes complexity)
- Domain model already correct
- Well-defined test cases
- No database migration required
- Easy rollback if needed

## Implementation Steps

1. Backup current code
2. Modify `EventRepository.cs` (remove duplicate include)
3. Modify `CancelRsvpCommandHandler.cs` (remove query and explicit removes)
4. Run integration tests
5. Manual testing with real data
6. Deploy to staging
7. Monitor logs
8. Deploy to production

## Related Documents

- **Detailed Analysis**: [ADR-007-Registration-Cancellation-Commitment-Deletion-Bug.md](./ADR-007-Registration-Cancellation-Commitment-Deletion-Bug.md)
- **Visual Diagrams**: [diagrams/commitment-deletion-bug-analysis.md](./diagrams/commitment-deletion-bug-analysis.md)

## Key Architectural Lesson

**Never duplicate navigation property includes in EF Core:**
- Creates tracking confusion
- Leads to state conflicts
- Difficult to debug
- Always use single, clear navigation paths

**Trust your domain model:**
- Encapsulates business logic
- Maintains invariants
- Single source of truth
- Infrastructure should respect domain decisions

---

**Decision**: Implement fix immediately (30 min effort, high impact)

**Next Action**: Review with team, then implement and test
