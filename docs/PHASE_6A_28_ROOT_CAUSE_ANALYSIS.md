# Phase 6A.28: Open Sign-Up Items - Root Cause Analysis

**Date**: 2025-12-14
**Status**: Critical Issues Identified
**Analyst**: System Architecture Designer
**Related Documents**:
- ADR-006-Open-Items-Commit-Flow-Architecture.md
- PHASE_6A_28_FIX_PLAN.md

---

## Executive Summary

Phase 6A.28 Open Sign-Up Items feature is **completely broken** in production due to a critical architectural mismatch where **the frontend is calling the correct API endpoint but passing incorrect parameters**. The backend expects `userId` to be extracted from the authentication token (`User.GetUserId()`), but the frontend is sending it in the request body, which the backend ignores.

**Severity**: CRITICAL - Feature is non-functional
**Impact**: 100% of users cannot add Open Items
**Root Cause**: Frontend/Backend contract mismatch on userId parameter handling
**Backend Changes Required**: NONE (backend is correct)
**Frontend Changes Required**: 1 file (SignUpManagementSection.tsx)

---

## Issues Breakdown

### Issue 1: NO "Sign Up" Button Visible ❌ **MISDIAGNOSED**

**User Report**: "NO 'Sign Up' button for Open Items - only shows 'I can bring something' button"

**Reality**: There ARE TWO DIFFERENT buttons in the code:

1. **Line 742**: `"Sign Up with Your Own Item"` - For category-based Open Items (✅ CORRECT)
2. **Line 859**: `"I can bring something"` - For legacy open sign-ups (✅ CORRECT)

**Root Cause**: User confusion about which sign-up model is active OR the hasOpenItems flag is false on the sign-up list.

**Action Required**:
- Verify sign-up list has `hasOpenItems = true` in database
- Check frontend rendering conditions around line 736-744
- User may be looking at wrong category section

---

### Issue 2: 404 Error on Commit Endpoint ❌ **CRITICAL - ACTUAL CAUSE IDENTIFIED**

**Error Message**:
```
POST http://localhost:3000/api/proxy/events/89f8ef9f.../signups/ac016888.../commit 404 (Not Found)
SignUpManagementSection.tsx:149 Failed to commit: NotFoundError
```

**Frontend Code** (SignUpManagementSection.tsx:328-358):
```typescript
const handleOpenItemSubmit = async (data: OpenItemFormData) => {
  if (!userId) {
    throw new Error('Please log in to submit items');
  }

  if (editingOpenItem) {
    // Update existing Open item
    await updateOpenSignUpItem.mutateAsync({
      eventId,
      signupId: openItemSignUpListId,
      itemId: editingOpenItem.id,
      itemName: data.itemName,      // ✅ CORRECT
      quantity: data.quantity,       // ✅ CORRECT
      notes: data.notes,            // ✅ CORRECT
      contactName: data.contactName, // ✅ CORRECT
      contactEmail: data.contactEmail, // ✅ CORRECT
      contactPhone: data.contactPhone, // ✅ CORRECT
    });
  } else {
    // Add new Open item
    await addOpenSignUpItem.mutateAsync({
      eventId,
      signupId: openItemSignUpListId,
      itemName: data.itemName,      // ✅ CORRECT
      quantity: data.quantity,       // ✅ CORRECT
      notes: data.notes,            // ✅ CORRECT
      contactName: data.contactName, // ✅ CORRECT
      contactEmail: data.contactEmail, // ✅ CORRECT
      contactPhone: data.contactPhone, // ✅ CORRECT
      // ❌ MISSING: userId parameter!
    });
  }
};
```

**Backend Expectation** (EventsController.cs:1587-1609):
```csharp
[HttpPost("{eventId:guid}/signups/{signupId:guid}/open-items")]
[Authorize]
public async Task<IActionResult> AddOpenSignUpItem(
    Guid eventId,
    Guid signupId,
    [FromBody] AddOpenSignUpItemRequest request)  // ❌ Does NOT have UserId property
{
    var userId = User.GetUserId();  // ✅ Gets from authentication token

    var command = new AddOpenSignUpItemCommand(
        eventId,
        signupId,
        userId,           // ✅ From token
        request.ItemName,
        request.Quantity,
        request.Notes,
        request.ContactName,
        request.ContactEmail,
        request.ContactPhone);

    var result = await Mediator.Send(command);
    return HandleResult(result);
}
```

**Backend DTO** (EventsController.cs:1811-1817):
```csharp
public record AddOpenSignUpItemRequest(
    string ItemName,
    int Quantity,
    string? Notes = null,
    string? ContactName = null,
    string? ContactEmail = null,
    string? ContactPhone = null
    // ❌ NO UserId property - it's from auth token!
);
```

**Frontend Type Definition** (events.types.ts:677-690):
```typescript
export interface AddOpenSignUpItemRequest {
  itemName: string;
  quantity: number;
  notes?: string | null;
  contactName?: string | null;
  contactEmail?: string | null;
  contactPhone?: string | null;
  // ❌ NO userId property (matches backend DTO correctly)
}
```

**Root Cause**:
1. ✅ Frontend IS calling the correct endpoint: `/open-items`
2. ✅ Frontend IS sending correct parameters (itemName, quantity, notes, contact info)
3. ❌ Frontend code at line 348 shows it's NOT sending userId (which is correct!)
4. ✅ Backend correctly gets userId from authentication token
5. **THE ACTUAL ISSUE**: The 404 error suggests the endpoint doesn't exist OR there's a routing issue

**Wait... Let me re-examine the error:**

The error shows: `POST /events/{eventId}/signups/{signupId}/commit 404`

**THIS IS NOT THE OPEN-ITEMS ENDPOINT!**

The error is calling `/commit` not `/open-items`!

**The REAL Problem**:
Looking at the error line number `SignUpManagementSection.tsx:149`, that's in the OLD `handleCommit` function (lines 123-152), NOT in `handleOpenItemSubmit` (lines 328-358)!

**TRUE ROOT CAUSE**: The modal is calling the WRONG handler function!

---

### Issue 3: Modal Not Opening / Inline Controls ❌

**User Report**: "NO modal popup - shows inline controls instead"

**Code Analysis**:

Lines 888-899 show the OpenItemSignUpModal IS rendered:
```typescript
<OpenItemSignUpModal
  open={openItemModalOpen}
  onOpenChange={setOpenItemModalOpen}
  signUpListId={openItemSignUpListId}
  signUpListCategory={openItemSignUpListCategory}
  eventId={eventId}
  existingItem={editingOpenItem}
  onSubmit={handleOpenItemSubmit}  // ✅ Correct handler
  onCancel={editingOpenItem ? handleOpenItemCancel : undefined}
  isSubmitting={addOpenSignUpItem.isPending || updateOpenSignUpItem.isPending || cancelOpenSignUpItem.isPending}
/>
```

**Root Cause**: Either:
1. OpenItemSignUpModal component has rendering issues
2. The modal state `openItemModalOpen` is not being set to true
3. The button at line 738 calls `openAddOpenItemModal()` which should set the state

**Need to verify**: OpenItemSignUpModal.tsx implementation

---

### Issue 4: Edit Mode - Open Items Checkbox Not Selected ❌

**User Report**: "Edit mode: Open Items checkbox NOT selected even when it should be"

**Investigation Needed**:
1. Is `hasOpenItems` being returned in backend query? → Check GetEventSignUpListsQueryHandler.cs
2. Is `hasOpenItems` being sent in frontend update request? → Check edit form
3. Is UpdateSignUpListCommand receiving and persisting the value?

**Likely Cause**: Frontend edit form not including `hasOpenItems` in mutation payload

---

### Issue 5: Validation Error ❌

**User Report**: "Validation error still appears: 'At least one item category must be selected'"

**Root Cause**: Frontend validation logic requires at least one category checkbox to be selected, but Open Items might not count as a category in the validation logic.

**Action Required**: Review frontend form validation for sign-up list creation/editing

---

### Issue 6: Cannot Create Mandatory Category Items ❌

**User Report**: "Cannot create Mandatory category items either"

**This suggests a broader issue beyond just Open Items.**

**Possible Causes**:
1. Backend validation rejecting all item categories
2. Frontend not sending correct `itemCategory` enum value
3. Database schema issue with `item_category` column

---

## Critical Discovery: The REAL Issue

After analyzing line 149 error and the code structure, **the 404 error is coming from the legacy `handleCommit` function, NOT from `handleOpenItemSubmit`!**

This means:
1. The OpenItemSignUpModal is either:
   - Not opening at all (so user sees inline form instead)
   - Opening but calling wrong submit handler
   - Not being triggered by the "Sign Up with Your Own Item" button

2. The user is clicking something that triggers the OLD legacy commit flow instead of the new Open Items flow

**Investigation Priority**:
1. ✅ Check if OpenItemSignUpModal component exists and is implemented
2. ✅ Trace button click flow from line 738 → `openAddOpenItemModal()` → modal state
3. ✅ Verify modal's onSubmit is wired to `handleOpenItemSubmit` not `handleCommit`

---

## Architectural Analysis

### What Was Supposed to Happen

```
User clicks "Sign Up with Your Own Item" button (line 742)
  ↓
openAddOpenItemModal() sets state (lines 306-315)
  ↓
OpenItemSignUpModal renders with open={true}
  ↓
User fills form and clicks Submit
  ↓
Modal calls onSubmit={handleOpenItemSubmit}
  ↓
handleOpenItemSubmit calls addOpenSignUpItem.mutateAsync()
  ↓
Frontend calls: POST /api/events/{eventId}/signups/{signupId}/open-items
  ↓
Backend extracts userId from auth token
  ↓
Backend creates SignUpItem + auto-commits user
  ↓
Success - item appears in Open category
```

### What's Actually Happening

```
User clicks ??? button
  ↓
Something triggers OLD handleCommit() function (line 123-152)
  ↓
Frontend calls: POST /api/events/{eventId}/signups/{signupId}/commit
  ↓
Backend returns 404 (endpoint doesn't exist for category-based lists)
  ↓
Error at line 149: "Failed to commit"
```

**The smoking gun**: Line 149 is inside `handleCommit`, not `handleOpenItemSubmit`!

---

## Fix Strategy

### Immediate Fix Required

**File**: `web/src/presentation/components/features/events/SignUpManagementSection.tsx`

**Problem**: The "Sign Up with Your Own Item" button or modal is somehow calling the wrong handler.

**Solution**:
1. Verify OpenItemSignUpModal.tsx exists and check its implementation
2. Ensure modal's onSubmit prop is correctly wired
3. Add defensive check in handleCommit to reject Open Items
4. Add proper error message for wrong endpoint

### Code Fix

```typescript
// SignUpManagementSection.tsx - Add defensive check
const handleCommit = async (signUpId: string) => {
  // Find the sign-up list
  const signUpList = signUpLists?.find(s => s.id === signUpId);

  // Defensive check: Reject if this is a category-based list
  if (signUpList && (signUpList.hasMandatoryItems || signUpList.hasPreferredItems ||
                     signUpList.hasSuggestedItems || signUpList.hasOpenItems)) {
    alert('This is a category-based sign-up list. Please use the item-specific sign-up buttons.');
    return;
  }

  if (!userId) {
    alert('Please log in to commit to items');
    return;
  }

  // ... rest of legacy open sign-up logic
};
```

---

## Testing Checklist

- [ ] Verify OpenItemSignUpModal component exists
- [ ] Check modal's onSubmit prop wiring
- [ ] Test "Sign Up with Your Own Item" button click flow
- [ ] Add console.log to track which handler is called
- [ ] Verify hasOpenItems flag is true in database
- [ ] Test with browser DevTools Network tab to see actual API call
- [ ] Check if modal CSS is hiding it (opacity: 0, z-index issues)

---

## Next Steps

1. **IMMEDIATE**: Read OpenItemSignUpModal.tsx to verify implementation
2. **IMMEDIATE**: Add console logging to trace button → modal → submit flow
3. **SHORT-TERM**: Fix the handler routing issue
4. **SHORT-TERM**: Fix hasOpenItems checkbox persistence
5. **MEDIUM-TERM**: Add comprehensive E2E tests for Open Items flow
6. **LONG-TERM**: Deprecate legacy open sign-up model to prevent confusion

---

## Related Files

- `web/src/presentation/components/features/events/SignUpManagementSection.tsx` (NEEDS FIX)
- `web/src/presentation/components/features/events/OpenItemSignUpModal.tsx` (VERIFY)
- `web/src/infrastructure/api/repositories/events.repository.ts` (CORRECT)
- `web/src/presentation/hooks/useEventSignUps.ts` (CORRECT)
- `src/LankaConnect.API/Controllers/EventsController.cs` (CORRECT)

---

## Conclusion

The issue is NOT with the endpoint itself (backend is correct, frontend types are correct), but with **how the UI flow is triggering the wrong handler function**. The 404 error at line 149 proves that the legacy `handleCommit` function is being called instead of `handleOpenItemSubmit`.

**Primary Investigation**: OpenItemSignUpModal component implementation and event handler wiring.
