# Phase 6A.28: Open Items Fix Plan

**Date**: 2025-12-14
**Status**: Ready for Implementation
**Related ADR**: ADR-006-Open-Items-Commit-Flow-Architecture.md

## Executive Summary

Phase 6A.28 Open Sign-Up Items feature has **architectural mismatch** between frontend and backend. Backend is correctly implemented using DDD principles, but frontend incorrectly calls a non-existent legacy endpoint.

**Root Cause**: Frontend confused legacy "commit to list" pattern with category-based "create item" pattern.

**Solution**: Fix frontend to use existing `/open-items` endpoint (which auto-commits user).

**Backend Changes**: NONE (already correct)
**Frontend Changes**: 4 files
**Risk**: Low (backend unchanged, frontend follows existing pattern)

---

## Issues Identified During User Testing

### Issue 1: ✅ Sign-Up List Creation Works
**Status**: NO ACTION NEEDED
- Sign-up list with `hasOpenItems` checkbox creates successfully
- Database persists `has_open_items` column correctly

### Issue 2: ❌ 404 Error on Commit Endpoint (CRITICAL)
**Error**: `POST /events/{id}/signups/{signupId}/commit` returns 404

**Root Cause**:
- Frontend calls: `eventsRepository.commitToSignUp(eventId, signupId, data)`
- Which calls: `POST /events/${eventId}/signups/${signupId}/commit`
- This endpoint **does not exist** for Open Items

**Correct Flow**:
- Frontend should call: `eventsRepository.addOpenSignUpItem(eventId, signupId, data)`
- Which calls: `POST /events/${eventId}/signups/${signupId}/open-items`
- Domain automatically commits user to their item

**Fix**: Update frontend to use correct endpoint (see Implementation section)

### Issue 3: ❌ Open Items Checkbox Unchecked After Edit
**Symptom**: Edit sign-up list, `hasOpenItems` checkbox becomes unchecked

**Investigation Needed**:
1. ✅ Backend query returns value: `HasOpenItems = signUpList.HasOpenItems`
2. ❓ Is UpdateSignUpList command receiving the value?
3. ❓ Is frontend edit page sending the value?
4. ❓ Is there a form state issue?

**Action**: Check EventEditForm and UpdateSignUpListCommand handling

### Issue 4: ❌ Save Button Not Visible
**Symptom**: Save button scrolled out of view on edit page

**Cause**: Layout issue with scrollable container

**Fix**: Adjust modal/form layout CSS

---

## Architecture Analysis

### Backend Domain Model (CORRECT ✅)

```
Event (Aggregate Root)
  └─ SignUpList (Entity)
       ├─ HasOpenItems: bool
       └─ Items: List<SignUpItem> (Value Objects)
            ├─ SignUpItem (Category: Mandatory/Suggested/Open)
            │    ├─ CreatedByUserId: Guid? (for Open items)
            │    └─ Commitments: List<SignUpCommitment>
            └─ AddOpenItem() method
                 → Creates SignUpItem
                 → Auto-commits creator
```

**Key Insight**: Open Items are `SignUpItem` entities, NOT list-level commitments.

### API Endpoints (Backend)

#### ✅ Correctly Implemented
```
POST   /api/events/{eventId}/signups/{signupId}/open-items
PUT    /api/events/{eventId}/signups/{signupId}/open-items/{itemId}
DELETE /api/events/{eventId}/signups/{signupId}/open-items/{itemId}
```

#### ❌ Does Not Exist (Frontend Expectation)
```
POST /api/events/{eventId}/signups/{signupId}/commit
```

**Why it doesn't exist**: This endpoint is for **legacy Open Sign-Ups** (old model), not for **Category-Based Open Items** (new model).

### Two Sign-Up Models in Codebase

| Model | Use Case | Commitment Target | Endpoint |
|-------|----------|-------------------|----------|
| **Legacy Open** | User brings custom item, no predefined items | SignUpList (direct) | `/signups/{id}/commit` |
| **Category-Based** | Organizer defines categories (Mandatory/Suggested/Open) | SignUpItem (via category) | `/signups/{id}/items/{itemId}/commit` OR `/signups/{id}/open-items` |

**Open Items belong to Category-Based model**, not Legacy model.

---

## Implementation Plan

### Phase 1: Frontend Fixes (Required)

#### File 1: `web/src/presentation/components/features/events/SignUpManagementSection.tsx`

**Current Code** (INCORRECT):
```typescript
const handleOpenItemSubmit = async (data: OpenItemFormData) => {
    // WRONG: Calling legacy commit endpoint
    await eventsRepository.commitToSignUp(eventId, signupId, data);
};
```

**Fix**:
```typescript
const handleOpenItemSubmit = async (data: OpenItemFormData) => {
    const userId = useAuthStore.getState().user?.id;
    if (!userId) {
        toast.error('You must be logged in to add items');
        return;
    }

    if (editingItem) {
        // Update existing Open item
        await updateOpenSignUpItem.mutateAsync({
            eventId,
            signupId: signUpList.id,
            itemId: editingItem.id,
            userId,
            itemName: data.itemName,
            quantity: data.quantity,
            notes: data.notes,
            contactName: data.contactName,
            contactEmail: data.contactEmail,
            contactPhone: data.contactPhone,
        });
    } else {
        // Add new Open item (auto-commits user)
        await addOpenSignUpItem.mutateAsync({
            eventId,
            signupId: signUpList.id,
            userId,
            itemName: data.itemName,
            quantity: data.quantity,
            notes: data.notes,
            contactName: data.contactName,
            contactEmail: data.contactEmail,
            contactPhone: data.contactPhone,
        });
    }
    setOpenItemModalOpen(false);
    setEditingItem(null);
};
```

#### File 2: `web/src/infrastructure/api/repositories/events.repository.ts`

**Action**: Add comments to clarify usage

```typescript
/**
 * Commit to bringing an item to event (LEGACY OPEN SIGN-UPS ONLY)
 * DO NOT USE for Category-Based Open Items - use addOpenSignUpItem() instead
 *
 * @deprecated for Open Items - use addOpenSignUpItem()
 * Maps to backend POST /api/events/{eventId}/signups/{signupId}/commit
 */
async commitToSignUp(
    eventId: string,
    signupId: string,
    request: CommitToSignUpRequest
): Promise<void> {
    await apiClient.post<void>(`${this.basePath}/${eventId}/signups/${signupId}/commit`, request);
}
```

#### File 3: `web/src/presentation/hooks/useEventSignUps.ts`

**Action**: Update hook documentation

```typescript
/**
 * useCommitToSignUp Hook
 *
 * LEGACY MODEL ONLY - For open sign-up lists without categories
 * DO NOT USE for Category-Based Open Items
 *
 * For Open Items (Category-Based), use:
 * - useAddOpenSignUpItem() - Add Open item (auto-commits user)
 * - useUpdateOpenSignUpItem() - Update Open item
 * - useCancelOpenSignUpItem() - Cancel Open item
 *
 * @deprecated for Open Items
 */
export function useCommitToSignUp() {
    // ... existing code
}
```

#### File 4: `web/src/app/events/[id]/manage/page.tsx` (or edit form)

**Investigation**: Check if `hasOpenItems` is being sent in UpdateSignUpListCommand

**Expected Fix**:
```typescript
const handleUpdateSignUpList = async (data: SignUpListFormData) => {
    await updateSignUpList.mutateAsync({
        eventId,
        signupId,
        category: data.category,
        description: data.description,
        hasMandatoryItems: data.hasMandatoryItems,
        hasPreferredItems: data.hasPreferredItems,
        hasSuggestedItems: data.hasSuggestedItems,
        hasOpenItems: data.hasOpenItems, // ← ENSURE THIS IS INCLUDED
    });
};
```

### Phase 2: Layout Fixes (Save Button Visibility)

**File**: Modal/form container CSS

**Investigation**: Check if save button is inside scrollable container without sticky positioning

**Potential Fix**:
```css
.sign-up-form-footer {
    position: sticky;
    bottom: 0;
    background: white;
    z-index: 10;
    padding: 1rem;
    border-top: 1px solid #e5e7eb;
}
```

### Phase 3: Backend Validation (Optional Hardening)

**File**: `src/LankaConnect.API/Controllers/EventsController.cs`

**Action**: Add clear error message if someone tries to use wrong endpoint

```csharp
/// <summary>
/// Legacy endpoint for Open Sign-Ups (old model)
/// DO NOT USE for Category-Based Open Items
/// </summary>
[HttpPost("{eventId:guid}/signups/{signupId:guid}/commit")]
[Authorize]
public async Task<IActionResult> CommitToSignUpLegacy(Guid eventId, Guid signupId, [FromBody] CommitToSignUpRequest request)
{
    Logger.LogWarning("Legacy commit endpoint called for sign-up {SignUpId}. Consider using /open-items for Category-Based model.", signupId);

    // Check if this is a category-based sign-up list
    var signUpList = await GetSignUpListAsync(eventId, signupId);
    if (signUpList != null && signUpList.IsCategoryBased())
    {
        return BadRequest(new ProblemDetails
        {
            Title = "Invalid Endpoint",
            Detail = "This is a category-based sign-up list. Use POST /signups/{id}/open-items to add Open items.",
            Status = 400
        });
    }

    // Handle legacy open sign-ups
    // ... existing code
}
```

---

## Testing Plan

### 1. Unit Tests (Frontend)

**File**: `web/src/presentation/components/features/events/__tests__/SignUpManagementSection.test.tsx`

```typescript
describe('SignUpManagementSection - Open Items', () => {
    it('should call addOpenSignUpItem when adding new Open item', async () => {
        const addOpenSignUpItemMock = jest.fn();
        // ... test setup

        await userEvent.click(screen.getByText('Sign Up'));
        await userEvent.type(screen.getByLabelText('Item Name'), 'Cookies');
        await userEvent.type(screen.getByLabelText('Quantity'), '24');
        await userEvent.click(screen.getByText('Submit'));

        expect(addOpenSignUpItemMock).toHaveBeenCalledWith(
            expect.objectContaining({
                itemName: 'Cookies',
                quantity: 24
            })
        );
        expect(commitToSignUpMock).not.toHaveBeenCalled(); // Should NOT call legacy endpoint
    });

    it('should call updateOpenSignUpItem when editing Open item', async () => {
        // ... test update flow
    });

    it('should call cancelOpenSignUpItem when canceling Open item', async () => {
        // ... test cancel flow
    });
});
```

### 2. Integration Tests (API)

**File**: `tests/LankaConnect.IntegrationTests/Events/OpenSignUpItemsTests.cs`

```csharp
[Fact]
public async Task AddOpenSignUpItem_ShouldAutoCommitUser()
{
    // Arrange
    var eventId = await CreateTestEventAsync();
    var signUpListId = await CreateSignUpListWithOpenItemsAsync(eventId);
    var userId = TestUserId;

    var request = new AddOpenSignUpItemRequest
    {
        UserId = userId,
        ItemName = "Homemade Cookies",
        Quantity = 24,
        Notes = "Chocolate chip",
        ContactName = "Test User",
        ContactEmail = "test@example.com"
    };

    // Act
    var response = await Client.PostAsJsonAsync(
        $"/api/events/{eventId}/signups/{signUpListId}/open-items",
        request
    );

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var itemId = await response.Content.ReadFromJsonAsync<Guid>();

    // Verify item created
    var item = await GetSignUpItemAsync(itemId);
    item.Should().NotBeNull();
    item.ItemDescription.Should().Be("Homemade Cookies");
    item.CreatedByUserId.Should().Be(userId);
    item.Commitments.Should().HaveCount(1); // Auto-committed
    item.Commitments[0].UserId.Should().Be(userId);
}

[Fact]
public async Task LegacyCommitEndpoint_ShouldRejectCategoryBasedLists()
{
    // Arrange: Create category-based sign-up list
    var eventId = await CreateTestEventAsync();
    var signUpListId = await CreateCategoryBasedSignUpListAsync(eventId);

    var request = new CommitToSignUpRequest
    {
        UserId = TestUserId,
        ItemDescription = "Test Item",
        Quantity = 1
    };

    // Act: Try to use legacy endpoint
    var response = await Client.PostAsJsonAsync(
        $"/api/events/{eventId}/signups/{signUpListId}/commit",
        request
    );

    // Assert: Should reject or redirect
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
    problem.Detail.Should().Contain("Use POST /signups/{id}/open-items");
}
```

### 3. Manual E2E Test Checklist

#### Test Case 1: Create Open Item
- [ ] Navigate to event with sign-up list (hasOpenItems = true)
- [ ] Click "Sign Up" button in Open category
- [ ] Enter: Item Name = "Cookies", Quantity = 24
- [ ] Submit form
- [ ] Verify: No 404 error
- [ ] Verify: Item appears in Open category
- [ ] Verify: User's name shown as committed
- [ ] Verify: Quantity shows "24 committed"

#### Test Case 2: Update Open Item
- [ ] Find user's Open item in list
- [ ] Click "Update" button
- [ ] Change: Quantity to 30
- [ ] Submit
- [ ] Verify: Item updates to 30
- [ ] Verify: Commitment updates

#### Test Case 3: Cancel Open Item
- [ ] Find user's Open item
- [ ] Click "Update" then "Cancel Commitment"
- [ ] Confirm cancellation
- [ ] Verify: Item removed from list
- [ ] Verify: No orphan commitments

#### Test Case 4: Edit Sign-Up List Persistence
- [ ] Create sign-up list with hasOpenItems = true
- [ ] Save
- [ ] Navigate to edit page
- [ ] Verify: hasOpenItems checkbox is CHECKED
- [ ] Change description
- [ ] Save
- [ ] Re-open edit page
- [ ] Verify: hasOpenItems checkbox still CHECKED

#### Test Case 5: Save Button Visibility
- [ ] Open edit sign-up list form
- [ ] Scroll through form fields
- [ ] Verify: Save button visible at all times (sticky footer or always in viewport)

---

## Deployment Strategy

### Step 1: Deploy Frontend Fix (Low Risk)
1. Merge frontend changes (4 files)
2. Build frontend: `npm run build`
3. Deploy to staging
4. Run E2E tests
5. Deploy to production

**Risk**: Low (backend API unchanged)

### Step 2: Monitor for Legacy Endpoint Usage
1. Check backend logs for legacy `/signups/{id}/commit` calls
2. Identify any remaining legacy sign-up lists
3. Plan migration if needed

### Step 3: Add Endpoint Validation (Optional)
1. Deploy backend change to reject category-based lists on legacy endpoint
2. Helps catch future developer mistakes

---

## Rollback Plan

**If issues arise**:

1. **Frontend Rollback**: Revert 4 frontend files to previous commit
2. **No Backend Rollback Needed**: Backend unchanged
3. **Quick Fix**: Add temporary shim endpoint (Option A from ADR) while investigating

---

## Success Metrics

- [ ] Zero 404 errors on `/signups/{id}/commit` for Open Items
- [ ] All Open Items created successfully
- [ ] hasOpenItems checkbox persists correctly
- [ ] Save button always visible
- [ ] Backend logs show no errors related to Open Items

---

## References

- **ADR**: `docs/architecture/ADR-006-Open-Items-Commit-Flow-Architecture.md`
- **Phase Summary**: `docs/PHASE_6A_28_OPEN_SIGNUP_ITEMS_SUMMARY.md`
- **Domain Model**: `src/LankaConnect.Domain/Events/Entities/SignUpList.cs`
- **API Controller**: `src/LankaConnect.API/Controllers/EventsController.cs`
- **Frontend Repo**: `web/src/infrastructure/api/repositories/events.repository.ts`

---

## Next Steps

1. Review this plan with team
2. Implement frontend fixes (Files 1-4)
3. Run unit tests
4. Deploy to staging
5. Manual E2E testing
6. Deploy to production
7. Monitor for issues
8. Update documentation if needed
9. Consider migrating legacy model (future phase)
