# Open Items vs Mandatory/Suggested Items - Commitment Cancellation Analysis

**Document Status**: Architecture Analysis
**Date**: 2025-12-18
**Phase**: 6A.28 Issue Investigation
**Critical Finding**: ROOT CAUSE IDENTIFIED

---

## Executive Summary

**THE SMOKING GUN**: Open Items use a **COMPLETELY DIFFERENT** cancellation mechanism than Mandatory/Suggested items.

**Working Path (Mandatory/Suggested)**:
- Frontend calls `handleCancelSignUp()` → Sends `quantity: 0` to `commitToSignUpItem` API
- Backend `CommitToSignUpItemCommandHandler` → Calls `SignUpItem.UpdateCommitment(userId, 0)`
- Domain `SignUpItem.UpdateCommitment()` → Detects `quantity == 0` → Removes commitment → Restores `RemainingQuantity`
- **Result**: Commitment deleted ✅

**Broken Path (Open Items)**:
- Frontend calls `handleCancelOpenItem()` → Calls `cancelOpenSignUpItem` API
- Backend `CancelOpenSignUpItemCommandHandler` → Calls `SignUpItem.CancelCommitment()` → Then `SignUpList.RemoveItem()`
- Domain `SignUpItem.CancelCommitment()` → Removes commitment → Raises `CommitmentCancelledEvent`
- Domain `SignUpList.RemoveItem()` → **DELETES THE ENTIRE ITEM** (not just commitment!)
- **Result**: Commitment AND item deleted ✅

**THE BUG**: When user cancels registration with "delete commitments" checkbox:
- `Event.CancelAllUserCommitments()` iterates through ALL items
- For Mandatory/Suggested: Calls `SignUpItem.CancelCommitment()` → Commitment deleted ✅
- For Open Items: Calls `SignUpItem.CancelCommitment()` → Commitment deleted ✅ → **BUT ITEM STILL EXISTS** ❌
- **Frontend re-renders** → Open item has NO commitments but item still visible → "Sign Up" button appears instead of being hidden

---

## Side-by-Side Code Comparison

### 1. Entity Model - IDENTICAL

Both Mandatory/Suggested and Open Items use the **SAME** `SignUpItem` entity:

```csharp
// SignUpItem.cs - Lines 12-27
public class SignUpItem : BaseEntity
{
    private readonly List<SignUpCommitment> _commitments = new();

    public Guid SignUpListId { get; private set; }
    public string ItemDescription { get; private set; }
    public int Quantity { get; private set; }
    public SignUpItemCategory ItemCategory { get; private set; }  // Mandatory, Suggested, OR Open
    public int RemainingQuantity { get; private set; }
    public Guid? CreatedByUserId { get; private set; }  // NULL for Mandatory/Suggested, SET for Open

    public IReadOnlyList<SignUpCommitment> Commitments => _commitments.AsReadOnly();
}
```

**Key Difference**:
- `CreatedByUserId == null` → Organizer-created (Mandatory/Suggested)
- `CreatedByUserId != null` → User-created (Open)
- `ItemCategory == SignUpItemCategory.Open` → Open item

### 2. Commitment Creation - IDENTICAL

Both use the **SAME** domain method:

```csharp
// SignUpItem.cs - Lines 127-167
public Result AddCommitment(
    Guid userId,
    int commitQuantity,
    string? commitNotes = null,
    string? contactName = null,
    string? contactEmail = null,
    string? contactPhone = null)
{
    // SAME validation for all categories
    if (userId == Guid.Empty)
        return Result.Failure("User ID is required");

    if (commitQuantity <= 0)
        return Result.Failure("Commit quantity must be greater than 0");

    if (commitQuantity > RemainingQuantity)
        return Result.Failure($"Cannot commit {commitQuantity}. Only {RemainingQuantity} remaining");

    // SAME commitment check
    if (_commitments.Any(c => c.UserId == userId))
        return Result.Failure("User has already committed to this item");

    // SAME commitment creation
    var commitmentResult = SignUpCommitment.CreateForItem(
        Id, userId, ItemDescription, commitQuantity, commitNotes,
        contactName, contactEmail, contactPhone);

    if (commitmentResult.IsFailure)
        return Result.Failure(commitmentResult.Error);

    _commitments.Add(commitmentResult.Value);
    RemainingQuantity -= commitQuantity;
    MarkAsUpdated();

    return Result.Success();
}
```

**Conclusion**: Commitment creation is 100% identical across all categories.

### 3. Frontend Cancellation - COMPLETELY DIFFERENT

**Mandatory/Suggested Items** (Working):

```typescript
// SignUpManagementSection.tsx - Lines 213-248
const handleCancelSignUp = async (signUpListId: string, itemId: string) => {
  if (!userId) {
    alert('Please log in to cancel sign-ups');
    return;
  }

  if (!confirm('Are you sure you want to cancel your sign-up for this item?')) {
    return;
  }

  try {
    setIsCancelling(true);
    setCancelConfirmId(itemId);

    // Call API with quantity = 0 to signal cancellation
    await commitToSignUpItem.mutateAsync({
      eventId,
      signupId: signUpListId,
      itemId: itemId,
      userId: userId,
      quantity: 0,  // ← MAGIC: quantity = 0 signals full cancellation
      notes: '',
      contactName: '',
      contactEmail: '',
      contactPhone: '',
    });

    setCancelConfirmId(null);
  } catch (error) {
    console.error('Failed to cancel sign-up:', error);
    alert('Failed to cancel sign-up. Please try again.');
    setCancelConfirmId(null);
  } finally {
    setIsCancelling(false);
  }
};
```

**Open Items** (Broken):

```typescript
// SignUpManagementSection.tsx - Lines 372-399
const handleCancelOpenItem = async (signUpListId: string, itemId: string) => {
  if (!userId) {
    alert('Please log in to cancel sign-ups');
    return;
  }

  if (!confirm('Are you sure you want to cancel your sign-up for this item?')) {
    return;
  }

  try {
    setIsCancelling(true);
    setCancelConfirmId(itemId);

    // ❌ DIFFERENT API: Uses dedicated cancelOpenSignUpItem
    await cancelOpenSignUpItem.mutateAsync({
      eventId,
      signupId: signUpListId,
      itemId: itemId,
      // ❌ NO quantity parameter - completely different contract
    });

    setCancelConfirmId(null);
  } catch (error) {
    console.error('Failed to cancel open item sign-up:', error);
    alert('Failed to cancel sign-up. Please try again.');
    setCancelConfirmId(null);
  } finally {
    setIsCancelling(false);
  }
};
```

**Key Differences**:
1. **Different API endpoints**: `commitToSignUpItem` vs `cancelOpenSignUpItem`
2. **Different parameters**: `quantity: 0` vs no quantity
3. **Different command**: `CommitToSignUpItemCommand` vs `CancelOpenSignUpItemCommand`

### 4. Backend Command Handlers - DIFFERENT LOGIC

**Mandatory/Suggested** (Working):

```csharp
// CommitToSignUpItemCommandHandler.cs - Lines 20-72
public async Task<Result> Handle(CommitToSignUpItemCommand request, CancellationToken cancellationToken)
{
    // Get event, sign-up list, and item
    var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
    var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
    var signUpItem = signUpList.GetItem(request.SignUpItemId);

    // Check if user already has a commitment
    var existingCommitment = signUpItem.Commitments.FirstOrDefault(c => c.UserId == request.UserId);

    Result commitResult;
    if (existingCommitment != null)
    {
        // ✅ Update existing commitment (supports quantity = 0 for cancellation)
        commitResult = signUpItem.UpdateCommitment(
            request.UserId,
            request.Quantity,  // ← Can be 0 to cancel
            request.Notes,
            request.ContactName,
            request.ContactEmail,
            request.ContactPhone);
    }
    else
    {
        // New commitment
        commitResult = signUpItem.AddCommitment(
            request.UserId,
            request.Quantity,
            request.Notes,
            request.ContactName,
            request.ContactEmail,
            request.ContactPhone);
    }

    if (commitResult.IsFailure)
        return Result.Failure(commitResult.Error);

    await _unitOfWork.CommitAsync(cancellationToken);

    return Result.Success();
}
```

**Open Items** (Different):

```csharp
// CancelOpenSignUpItemCommandHandler.cs - Lines 24-62
public async Task<Result> Handle(CancelOpenSignUpItemCommand request, CancellationToken cancellationToken)
{
    // Get event, sign-up list, and item
    var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
    var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
    var item = signUpList.GetItem(request.ItemId);

    // ❌ Verify this is an Open item created by this user
    if (item.ItemCategory != SignUpItemCategory.Open)
        return Result.Failure("Only Open items can be canceled using this endpoint");

    if (!item.IsCreatedByUser(request.UserId))
        return Result.Failure("You can only cancel Open items that you created");

    // ❌ Step 1: Cancel the commitment
    var cancelCommitResult = item.CancelCommitment(request.UserId);
    if (cancelCommitResult.IsFailure)
        return cancelCommitResult;

    // ❌ Step 2: ALSO DELETE THE ENTIRE ITEM
    var removeResult = signUpList.RemoveItem(request.ItemId);
    if (removeResult.IsFailure)
        return removeResult;

    await _unitOfWork.CommitAsync(cancellationToken);

    return Result.Success();
}
```

**Critical Difference**: Open item cancellation has **TWO STEPS**:
1. Cancel commitment (`item.CancelCommitment()`)
2. Delete the entire item (`signUpList.RemoveItem()`)

Why? Because Open items are **user-created**, so when the user cancels, the item should be removed entirely.

### 5. Domain Method - CancelCommitment() - IDENTICAL

```csharp
// SignUpItem.cs - Lines 261-279
public Result CancelCommitment(Guid userId)
{
    var commitment = _commitments.FirstOrDefault(c => c.UserId == userId);
    if (commitment == null)
        return Result.Failure("User has no commitment to this item");

    // Return the quantity back to remaining
    RemainingQuantity += commitment.Quantity;
    _commitments.Remove(commitment);

    // Raise domain event for infrastructure to handle deletion
    // EF Core cannot detect collection removals from private backing fields
    RaiseDomainEvent(new DomainEvents.CommitmentCancelledEvent(Id, commitment.Id, userId));

    MarkAsUpdated();

    return Result.Success();
}
```

**This method is identical for all categories** - it properly:
1. Finds the commitment
2. Restores `RemainingQuantity`
3. Removes commitment from collection
4. Raises domain event
5. Marks entity as updated

### 6. Domain Method - UpdateCommitment() with quantity=0

```csharp
// SignUpItem.cs - Lines 175-253
public Result UpdateCommitment(
    Guid userId,
    int newQuantity,
    string? commitNotes = null,
    string? contactName = null,
    string? contactEmail = null,
    string? contactPhone = null)
{
    if (userId == Guid.Empty)
        return Result.Failure("User ID is required");

    // Find existing commitment
    var existingCommitment = _commitments.FirstOrDefault(c => c.UserId == userId);
    if (existingCommitment == null)
        return Result.Failure("User has no commitment to this item");

    // ✅ SPECIAL CASE: quantity = 0 means cancel the commitment entirely
    if (newQuantity == 0)
    {
        RemainingQuantity += existingCommitment.Quantity;
        _commitments.Remove(existingCommitment);
        MarkAsUpdated();
        return Result.Success();
    }

    // ... rest of update logic
}
```

**This is the magic**: `UpdateCommitment(userId, 0)` is effectively the same as `CancelCommitment(userId)`.

---

## Registration Cancellation Flow

When user cancels registration with "delete commitments" checkbox:

```csharp
// CancelRsvpCommandHandler.cs - Lines 76-98
if (request.DeleteSignUpCommitments)
{
    _logger.LogInformation("[CancelRsvp] Deleting commitments via domain model...");

    var cancelResult = @event.CancelAllUserCommitments(request.UserId);

    if (cancelResult.IsFailure)
    {
        _logger.LogWarning("[CancelRsvp] Failed to delete commitments: {Error}", cancelResult.Error);
    }

    // CRITICAL: Mark event as modified for EF Core change tracking
    _eventRepository.Update(@event);
}
```

This calls:

```csharp
// Event.cs - Lines 1337-1383
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
            // Check if this item has a commitment from the user
            if (item.Commitments.Any(c => c.UserId == userId))
            {
                // ✅ Use domain method which properly restores remaining_quantity
                var result = item.CancelCommitment(userId);

                if (result.IsSuccess)
                {
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

    return cancelledCount > 0 ? Result.Success() : Result.Success();
}
```

**What happens**:
1. ✅ Iterates through ALL sign-up lists (including Open items)
2. ✅ Finds items with user's commitments
3. ✅ Calls `item.CancelCommitment(userId)` - SAME for all categories
4. ✅ Commitment is removed
5. ✅ `RemainingQuantity` is restored
6. ✅ Domain event raised
7. ✅ EF Core change tracking updated via `_eventRepository.Update()`

**THE PROBLEM**:
- For Mandatory/Suggested: Item remains in list (expected) ✅
- For Open Items: Item remains in list (NOT expected) ❌

**Why is this a problem for Open Items?**
- Open items are **user-created** - they should be deleted when the creator cancels
- The `CancelOpenSignUpItemCommandHandler` does this correctly: cancels commitment + deletes item
- But `Event.CancelAllUserCommitments()` ONLY cancels the commitment, doesn't delete the item

---

## The Root Cause

**Design Inconsistency**: Open Items have **TWO** lifecycle rules:

1. **User-initiated cancellation** (via "Cancel Sign Up" button):
   - Frontend → `handleCancelOpenItem()` → `cancelOpenSignUpItem` API
   - Backend → `CancelOpenSignUpItemCommandHandler` → Cancels commitment + Deletes item ✅

2. **Registration cancellation** (via "Also delete my sign-up commitments" checkbox):
   - Frontend → Registration cancellation flow
   - Backend → `CancelRsvpCommandHandler` → `Event.CancelAllUserCommitments()`
   - Domain → Iterates all items → Cancels commitments only ❌
   - **Missing**: Delete the Open items created by this user

**The Fix**: `Event.CancelAllUserCommitments()` needs to detect Open items created by the user and delete them.

---

## Data Flow Diagrams

### Mandatory/Suggested Cancellation (Working)

```
┌──────────────────────────────────────────────────────────────────┐
│ USER CLICKS "Cancel Sign Up" on Mandatory/Suggested Item        │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│ Frontend: handleCancelSignUp()                                   │
│   - Calls commitToSignUpItem.mutateAsync()                      │
│   - Passes quantity: 0 (special signal)                         │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│ Backend: CommitToSignUpItemCommandHandler                        │
│   - Finds existing commitment                                    │
│   - Calls SignUpItem.UpdateCommitment(userId, 0)                │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│ Domain: SignUpItem.UpdateCommitment()                            │
│   - Detects newQuantity == 0                                    │
│   - RemainingQuantity += commitment.Quantity                    │
│   - _commitments.Remove(commitment)                             │
│   - MarkAsUpdated()                                             │
│   - Returns Result.Success()                                    │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│ EF Core: Saves changes                                           │
│   - DELETE FROM signup_commitments WHERE id = ...               │
│   - UPDATE signup_items SET remaining_quantity = ...            │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│ Frontend: Re-renders                                             │
│   - Item still visible ✅                                        │
│   - userItemCommitment = null                                   │
│   - Shows "Sign Up" button ✅                                   │
│   - NO "Cancel Sign Up" button ✅                               │
└──────────────────────────────────────────────────────────────────┘
```

### Open Items Cancellation via Button (Working)

```
┌──────────────────────────────────────────────────────────────────┐
│ USER CLICKS "Cancel Sign Up" on Open Item                       │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│ Frontend: handleCancelOpenItem()                                 │
│   - Calls cancelOpenSignUpItem.mutateAsync()                    │
│   - Different API endpoint                                       │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│ Backend: CancelOpenSignUpItemCommandHandler                      │
│   - Validates item is Open category                             │
│   - Validates user created this item                            │
│   - Step 1: item.CancelCommitment(userId)                       │
│   - Step 2: signUpList.RemoveItem(itemId)                       │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│ Domain: SignUpItem.CancelCommitment()                            │
│   - RemainingQuantity += commitment.Quantity                    │
│   - _commitments.Remove(commitment)                             │
│   - RaiseDomainEvent(CommitmentCancelledEvent)                  │
│   - MarkAsUpdated()                                             │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│ Domain: SignUpList.RemoveItem()                                  │
│   - Validates no other commitments (already removed)            │
│   - _items.Remove(item)                                         │
│   - MarkAsUpdated()                                             │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│ EF Core: Saves changes                                           │
│   - DELETE FROM signup_commitments WHERE id = ...               │
│   - DELETE FROM signup_items WHERE id = ...                     │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│ Frontend: Re-renders                                             │
│   - Item NOT visible ✅ (deleted)                               │
│   - Shows "Sign Up" button for category ✅                      │
└──────────────────────────────────────────────────────────────────┘
```

### Open Items Registration Cancellation (BROKEN)

```
┌──────────────────────────────────────────────────────────────────┐
│ USER CANCELS REGISTRATION with "Delete Commitments" checked     │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│ Backend: CancelRsvpCommandHandler                                │
│   - Cancels registration                                         │
│   - if (DeleteSignUpCommitments)                                │
│       @event.CancelAllUserCommitments(userId)                   │
│       _eventRepository.Update(@event)                           │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│ Domain: Event.CancelAllUserCommitments()                         │
│   - foreach (signUpList in _signUpLists)                        │
│       foreach (item in signUpList.Items)                        │
│         if (item.Commitments.Any(c => c.UserId == userId))      │
│           item.CancelCommitment(userId) ← WORKS ✅              │
│   - ❌ MISSING: Delete Open items created by userId             │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│ Domain: SignUpItem.CancelCommitment() (called for ALL items)    │
│   - RemainingQuantity += commitment.Quantity ✅                 │
│   - _commitments.Remove(commitment) ✅                          │
│   - RaiseDomainEvent(CommitmentCancelledEvent) ✅               │
│   - MarkAsUpdated() ✅                                          │
│   - ❌ DOESN'T know or care if this is an Open item            │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│ EF Core: Saves changes                                           │
│   - DELETE FROM signup_commitments WHERE userId = ... ✅        │
│   - UPDATE signup_items SET remaining_quantity = ... ✅         │
│   - ❌ NO DELETE for Open items                                │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│ Frontend: Re-renders                                             │
│   - Mandatory/Suggested items still visible ✅                  │
│   - Open items STILL VISIBLE ❌ (should be deleted)            │
│   - userItemCommitment = null for all                           │
│   - Shows "Sign Up" button for Open items ❌                    │
│   - User confused: "I cancelled, why is my item still there?"   │
└──────────────────────────────────────────────────────────────────┘
```

---

## The Fix

Update `Event.CancelAllUserCommitments()` to detect and delete Open items:

```csharp
public Result CancelAllUserCommitments(Guid userId)
{
    if (userId == Guid.Empty)
        return Result.Failure("User ID is required");

    var cancelledCount = 0;
    var errors = new List<string>();
    var itemsToRemove = new List<(Guid signUpListId, Guid itemId)>();  // NEW

    foreach (var signUpList in _signUpLists)
    {
        foreach (var item in signUpList.Items)
        {
            // Check if this item has a commitment from the user
            if (item.Commitments.Any(c => c.UserId == userId))
            {
                // Use domain method which properly restores remaining_quantity
                var result = item.CancelCommitment(userId);

                if (result.IsSuccess)
                {
                    cancelledCount++;

                    // NEW: If this is an Open item created by this user, mark for deletion
                    if (item.IsOpenItem() && item.IsCreatedByUser(userId))
                    {
                        itemsToRemove.Add((signUpList.Id, item.Id));
                    }
                }
                else
                {
                    errors.Add($"Failed to cancel commitment for item '{item.ItemDescription}': {result.Error}");
                }
            }
        }
    }

    // NEW: Remove Open items created by this user
    foreach (var (signUpListId, itemId) in itemsToRemove)
    {
        var signUpList = _signUpLists.FirstOrDefault(s => s.Id == signUpListId);
        if (signUpList != null)
        {
            var removeResult = signUpList.RemoveItem(itemId);
            if (removeResult.IsFailure)
            {
                errors.Add($"Failed to remove Open item: {removeResult.Error}");
            }
        }
    }

    if (cancelledCount > 0)
    {
        MarkAsUpdated();
    }

    return cancelledCount > 0 ? Result.Success() : Result.Success();
}
```

**Why this works**:
1. Cancel all commitments (same as before) ✅
2. **NEW**: Detect Open items created by this user
3. **NEW**: Remove those items from their sign-up lists
4. Maintains backward compatibility with Mandatory/Suggested items ✅

---

## Technology Evaluation

**Pattern Consistency Check**:

| Feature | Mandatory | Suggested | Open (Current) | Open (Fixed) |
|---------|-----------|-----------|----------------|--------------|
| Entity | `SignUpItem` | `SignUpItem` | `SignUpItem` | `SignUpItem` |
| Commitment Entity | `SignUpCommitment` | `SignUpCommitment` | `SignUpCommitment` | `SignUpCommitment` |
| AddCommitment() | ✅ Same | ✅ Same | ✅ Same | ✅ Same |
| CancelCommitment() | ✅ Same | ✅ Same | ✅ Same | ✅ Same |
| UpdateCommitment(0) | ✅ Works | ✅ Works | ✅ Works | ✅ Works |
| Frontend Cancel Button | `handleCancelSignUp()` | `handleCancelSignUp()` | `handleCancelOpenItem()` | `handleCancelOpenItem()` |
| Backend Cancel API | `CommitToSignUpItemCommand` | `CommitToSignUpItemCommand` | `CancelOpenSignUpItemCommand` | `CancelOpenSignUpItemCommand` |
| Item Lifecycle | Permanent | Permanent | User-owned | User-owned |
| Registration Cancel | Keeps item ✅ | Keeps item ✅ | Keeps item ❌ | Deletes item ✅ |

**Conclusion**: The fix maintains architectural consistency while respecting the different lifecycle requirements of Open items.

---

## Risks and Mitigation

**Risk 1**: Deleting Open items might break cascading relationships

**Mitigation**: `SignUpList.RemoveItem()` already validates no commitments exist before deletion. Since we've already cancelled the commitment in the previous step, this check will pass.

**Risk 2**: Other users might have signed up for an Open item created by someone else

**Analysis**: Current design doesn't support this. Open items are auto-committed to the creator. Frontend shows "Update Sign Up" and "Cancel Sign Up" buttons only if `isOwnItem === true` (line 746).

**Mitigation**: Add validation in `Event.CancelAllUserCommitments()`:
```csharp
if (item.IsOpenItem() && item.IsCreatedByUser(userId) && item.GetCommitmentCount() <= 1)
{
    itemsToRemove.Add((signUpList.Id, item.Id));
}
```

This ensures we only delete Open items if:
1. It's an Open item
2. User created it
3. No other users have signed up (commitment count ≤ 1 means only creator)

**Risk 3**: Frontend caching issues

**Mitigation**: Already handled. The `CancelRsvpCommandHandler` explicitly calls `_eventRepository.Update(@event)` (line 97) to ensure EF Core tracks changes.

---

## Next Steps

1. **Update Domain Method**: Modify `Event.CancelAllUserCommitments()` to delete Open items
2. **Add Unit Tests**: Test registration cancellation with Open items
3. **Manual Testing**: Verify UI behavior after registration cancellation
4. **Update Documentation**: Document Open item lifecycle in ADR

---

## References

- `c:\Work\LankaConnect\src\LankaConnect.Domain\Events\Event.cs` (Lines 1337-1383)
- `c:\Work\LankaConnect\src\LankaConnect.Domain\Events\Entities\SignUpItem.cs` (Lines 261-279, 175-253)
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Commands\CancelOpenSignUpItem\CancelOpenSignUpItemCommandHandler.cs`
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Commands\CommitToSignUpItem\CommitToSignUpItemCommandHandler.cs`
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Commands\CancelRsvp\CancelRsvpCommandHandler.cs`
- `c:\Work\LankaConnect\web\src\presentation\components\features\events\SignUpManagementSection.tsx` (Lines 213-399)
