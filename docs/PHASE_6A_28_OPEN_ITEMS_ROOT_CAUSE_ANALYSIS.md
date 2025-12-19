# Phase 6A.28 - Open Items Root Cause Analysis

**Date**: 2025-12-18
**Issue**: Open Items not deleted when user cancels registration with "delete commitments" checkbox
**Status**: ROOT CAUSE IDENTIFIED
**Priority**: High (User-facing bug)

---

## TL;DR - The One Thing You Need to Know

**THE BUG**: When a user cancels their registration and checks "Also delete my sign-up commitments":
- ✅ Mandatory item commitments are deleted (item remains for others)
- ✅ Suggested item commitments are deleted (item remains for others)
- ❌ **Open item commitments are deleted BUT THE ITEM REMAINS** (should be deleted entirely)

**WHY**: Open Items use a **different cancellation mechanism** than Mandatory/Suggested items.

**THE FIX**: Update `Event.CancelAllUserCommitments()` to detect and delete Open items created by the user.

---

## What User Experiences

### Before Cancellation
```
User Alice creates Open item: "Homemade Lasagna (qty: 1)"
Alice is auto-committed to bring 1 Lasagna
Alice is registered for event: CONFIRMED ✓
```

### User Action
```
Alice clicks "Cancel Registration"
Checks checkbox: "Also delete my sign-up commitments"
Expects: Registration cancelled, Lasagna item deleted
```

### What Actually Happens (BROKEN)
```
Registration: CANCELLED ✓
Mandatory items: Commitment deleted, item remains ✅
Suggested items: Commitment deleted, item remains ✅
Open items: Commitment deleted, BUT ITEM STILL VISIBLE ❌

Frontend shows:
- "Homemade Lasagna" item still listed
- "Your item" badge still showing
- "Sign Up" button visible (instead of item being gone)
- User confused: "I cancelled, why is my Lasagna still there?"
```

### What Should Happen (EXPECTED)
```
Registration: CANCELLED ✓
Mandatory items: Commitment deleted, item remains ✅
Suggested items: Commitment deleted, item remains ✅
Open items: Commitment deleted, ITEM ALSO DELETED ✅

Frontend shows:
- "Homemade Lasagna" item completely removed
- "Sign Up" button for Open category visible
- User happy: "Perfect! Everything is cancelled."
```

---

## Technical Root Cause

### The Key Insight

**Mandatory/Suggested items** have ONE cancellation path:
```
handleCancelSignUp() → commitToSignUpItem({ quantity: 0 }) → UpdateCommitment(0) → Cancel
```

**Open items** have TWO DIFFERENT cancellation paths:

**Path 1: Direct "Cancel Sign Up" button** (WORKS ✅)
```
handleCancelOpenItem() → cancelOpenSignUpItem API → CancelCommitment() + RemoveItem()
```

**Path 2: Registration cancellation** (BROKEN ❌)
```
CancelRegistration → CancelAllUserCommitments() → CancelCommitment() ONLY
                                                    ↑
                                                    Missing: RemoveItem()
```

### Why Different Paths Matter

**Organizer-created items** (Mandatory/Suggested):
- Created by event organizer
- Permanent (survive commitment cancellation)
- Example: "Rice (qty: 5 kg)" - stays for other users to sign up

**User-created items** (Open):
- Created by individual user
- User-owned (should be deleted when user cancels)
- Example: Alice's "Homemade Lasagna" - should disappear when Alice cancels

### The Code Issue

`Event.CancelAllUserCommitments()` was designed before Open items existed:

```csharp
// Event.cs - Lines 1337-1383
public Result CancelAllUserCommitments(Guid userId)
{
    foreach (var signUpList in _signUpLists)
    {
        foreach (var item in signUpList.Items)
        {
            if (item.Commitments.Any(c => c.UserId == userId))
            {
                item.CancelCommitment(userId);  // ✅ Cancels commitment

                // ❌ MISSING: Check if Open item created by userId
                //             If yes, also delete the item
            }
        }
    }
}
```

**What's missing**: After cancelling the commitment, it should check:
1. Is this an Open item? (`item.ItemCategory == SignUpItemCategory.Open`)
2. Did this user create it? (`item.CreatedByUserId == userId`)
3. If yes → Delete the entire item

---

## Side-by-Side Comparison

| Feature | Mandatory/Suggested (Working) | Open Items (Broken) | Open Items (Fixed) |
|---------|-------------------------------|---------------------|-------------------|
| **Cancel via Button** | ✅ Deletes commitment only | ✅ Deletes commitment + item | ✅ Deletes commitment + item |
| **Cancel via Registration** | ✅ Deletes commitment only | ❌ Deletes commitment only (WRONG!) | ✅ Deletes commitment + item |
| **Item Lifecycle** | Permanent | User-owned | User-owned |
| **Frontend API** | `commitToSignUpItem` | `cancelOpenSignUpItem` | `cancelOpenSignUpItem` |
| **Backend Command** | `CommitToSignUpItemCommand` | `CancelOpenSignUpItemCommand` | `CancelOpenSignUpItemCommand` |
| **Domain Methods** | `UpdateCommitment(0)` | `CancelCommitment()` + `RemoveItem()` | `CancelCommitment()` + `RemoveItem()` |

---

## The Fix

### Code Change Required

Update `Event.CancelAllUserCommitments()` in `src/LankaConnect.Domain/Events/Event.cs`:

```csharp
public Result CancelAllUserCommitments(Guid userId)
{
    if (userId == Guid.Empty)
        return Result.Failure("User ID is required");

    var cancelledCount = 0;
    var errors = new List<string>();

    // NEW: Track Open items to delete
    var itemsToRemove = new List<(Guid signUpListId, Guid itemId)>();

    foreach (var signUpList in _signUpLists)
    {
        foreach (var item in signUpList.Items)
        {
            if (item.Commitments.Any(c => c.UserId == userId))
            {
                // Step 1: Cancel the commitment (SAME AS BEFORE)
                var result = item.CancelCommitment(userId);

                if (result.IsSuccess)
                {
                    cancelledCount++;

                    // Step 2: NEW - If this is an Open item created by user, mark for deletion
                    if (item.IsOpenItem() && item.IsCreatedByUser(userId))
                    {
                        itemsToRemove.Add((signUpList.Id, item.Id));
                    }
                }
                else
                {
                    errors.Add($"Failed to cancel commitment: {result.Error}");
                }
            }
        }
    }

    // Step 3: NEW - Remove Open items created by this user
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

### What This Fixes

1. ✅ Cancels all commitments (same as before)
2. ✅ **NEW**: Detects Open items created by the user
3. ✅ **NEW**: Deletes those Open items from their sign-up lists
4. ✅ Maintains backward compatibility with Mandatory/Suggested items
5. ✅ Matches the behavior of the direct "Cancel Sign Up" button

### Testing Plan

1. **Unit Tests**: Test `Event.CancelAllUserCommitments()` with Open items
2. **Integration Tests**: Test registration cancellation with Open items
3. **Manual Testing**:
   - Create Open item
   - Cancel registration with checkbox
   - Verify item is deleted
   - Verify Mandatory/Suggested items still work

---

## Impact Analysis

### Affected Components

**Domain Layer**:
- ✅ `Event.CancelAllUserCommitments()` - REQUIRES UPDATE
- ✅ `SignUpItem.IsOpenItem()` - Already exists
- ✅ `SignUpItem.IsCreatedByUser()` - Already exists
- ✅ `SignUpList.RemoveItem()` - Already exists

**Application Layer**:
- ✅ `CancelRsvpCommandHandler` - NO CHANGE (already calls domain method)

**Infrastructure Layer**:
- ✅ EF Core configuration - NO CHANGE (already handles cascading deletes)

**Frontend**:
- ✅ `SignUpManagementSection.tsx` - NO CHANGE (will refresh automatically)

### Backward Compatibility

✅ **SAFE**: The fix only affects Open items created by the user
✅ **NO BREAKING CHANGES**: Mandatory/Suggested items behave exactly the same
✅ **CONSISTENT**: Registration cancellation now matches direct button cancellation

### Risks

**Risk**: What if another user has signed up for an Open item created by someone else?

**Analysis**: Current design doesn't support this. Open items are auto-committed to the creator only. Frontend only shows "Cancel Sign Up" button if `isOwnItem === true`.

**Mitigation**: Add validation to ensure we only delete Open items with no other commitments:
```csharp
if (item.IsOpenItem() &&
    item.IsCreatedByUser(userId) &&
    item.GetCommitmentCount() <= 1)  // Only creator's commitment
{
    itemsToRemove.Add((signUpList.Id, item.Id));
}
```

---

## Documentation

**Full Analysis**:
- `c:\Work\LankaConnect\docs\architecture\OPEN_ITEMS_VS_MANDATORY_SUGGESTED_COMPARISON.md`

**Visual Diagrams**:
- `c:\Work\LankaConnect\docs\architecture\diagrams\open-items-cancellation-bug.md`

**Related ADRs**:
- ADR-008: EF Core Collection Change Tracking
- ADR-007: Domain Event Pattern for Commitment Cancellation

---

## Next Steps

1. [ ] Implement fix in `Event.CancelAllUserCommitments()`
2. [ ] Add unit tests for Open item cancellation
3. [ ] Test registration cancellation flow
4. [ ] Update PROGRESS_TRACKER.md
5. [ ] Create ADR documenting the fix
6. [ ] Deploy to staging
7. [ ] Manual QA testing
8. [ ] Deploy to production

---

## Key Takeaways

**For Developers**:
- Open items have different lifecycle than Mandatory/Suggested items
- User-created items should be deleted when user cancels
- Registration cancellation should match direct button cancellation

**For Product**:
- User expectation: "If I cancel my registration and delete commitments, my custom items should also disappear"
- This is a UX bug - user sees their item still listed after cancellation
- Fix is low-risk, high-impact for user experience

**For QA**:
- Test Open items separately from Mandatory/Suggested items
- Verify both cancellation paths: direct button and registration cancellation
- Check that items created by other users are not affected
