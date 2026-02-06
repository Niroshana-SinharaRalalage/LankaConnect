# ADR-006: Open Items Commit Flow Architecture

**Status**: Accepted
**Date**: 2025-12-14
**Phase**: 6A.28 - Open Sign-Up Items
**Deciders**: System Architect

## Context and Problem Statement

Phase 6A.28 implemented "Open Sign-Up Items" feature that allows users to submit custom items to sign-up lists. During user testing, a critical architectural mismatch was discovered between frontend and backend implementation:

**Frontend expectation** (events.repository.ts:435):
```typescript
await apiClient.post(`/events/${eventId}/signups/${signupId}/commit`, request);
```

**Backend reality** (EventsController.cs:1452):
```csharp
[HttpPost("{eventId:guid}/signups/{signupId:guid}/items/{itemId:guid}/commit")]
```

The frontend is calling a non-existent endpoint `/signups/{signupId}/commit`, while the backend only provides `/signups/{signupId}/items/{itemId}/commit`.

## Root Cause Analysis

### What Was Supposed to Happen

Based on Phase 6A.28 documentation and domain model analysis:

1. User enables "Open Items" checkbox on sign-up list
2. Frontend shows "Sign Up" button in Open category section
3. User clicks button, modal opens for custom item entry
4. User submits: `AddOpenSignUpItem` command creates SignUpItem + auto-commits user
5. Item appears in Open category with user's commitment

### What Actually Happened

The implementation created the correct domain behavior but **incorrect API routing**:

**Backend Domain Layer** (SignUpList.cs:447):
```csharp
public Result<SignUpItem> AddOpenItem(
    Guid userId,
    string itemName,
    int quantity,
    string? notes = null,
    string? contactName = null,
    string? contactEmail = null,
    string? contactPhone = null)
{
    // Creates SignUpItem
    var itemResult = SignUpItem.CreateOpenItem(Id, userId, itemName, quantity, notes);

    // Auto-commits user to their own item
    var commitResult = item.AddCommitment(userId, quantity, notes, contactName, contactEmail, contactPhone);

    _items.Add(item);
    return Result<SignUpItem>.Success(item);
}
```

**Backend API** (EventsController.cs):
```csharp
// MISSING: POST /events/{id}/signups/{signupId}/commit
// EXISTS:  POST /events/{id}/signups/{signupId}/open-items ✅
// EXISTS:  POST /events/{id}/signups/{signupId}/items/{itemId}/commit ✅
```

**Frontend** (SignUpManagementSection.tsx):
```typescript
// Phase 6A.27: User clicks "Sign Up" for Open items
// Opens OpenItemSignUpModal
// Modal calls: eventsRepository.commitToSignUp(eventId, signupId, data)
// Which calls: POST /events/${eventId}/signups/${signupId}/commit ❌ DOES NOT EXIST
```

### The Confusion: Legacy vs Category-Based Models

The codebase has **two concurrent sign-up models**:

#### Legacy Model (Open Sign-Ups)
```csharp
// SignUpList.cs:274 - Legacy method
public Result AddCommitment(Guid userId, string itemDescription, int quantity)
{
    // User commits to SignUpList directly, no items
    _commitments.Add(commitmentResult.Value);
}
```

#### Category-Based Model (Mandatory/Suggested/Open Items)
```csharp
// SignUpList.cs:192 - New model
public Result<SignUpItem> AddItem(string itemDescription, int quantity, SignUpItemCategory itemCategory)
{
    // Creates SignUpItem entity
    _items.Add(itemResult.Value);
}

// SignUpItem.cs:127 - Commitments on items
public Result AddCommitment(Guid userId, int commitQuantity, ...)
{
    // User commits to specific SignUpItem
    _commitments.Add(commitmentResult.Value);
}
```

**The Problem**: Frontend confused these models, calling legacy `/signups/{id}/commit` endpoint for category-based Open Items.

## Decision Drivers

1. **Domain-Driven Design Principles**: Maintain aggregate boundaries and consistency
2. **RESTful API Design**: Clear resource hierarchy and intent
3. **Backward Compatibility**: Don't break legacy open sign-ups
4. **User Experience**: Single-step submission for Open Items
5. **Code Clarity**: Remove ambiguity between legacy and new models

## Considered Options

### Option A: Create Legacy Endpoint (Frontend Expectation)

Create `POST /events/{id}/signups/{signupId}/commit` endpoint that:
- Creates Open item + commitment in single call
- Matches frontend's current implementation
- Quick fix, minimal frontend changes

**Pros**:
- Fast implementation
- Minimal frontend changes
- Works with current UI flow

**Cons**:
- Perpetuates model confusion
- Violates DDD aggregate boundaries
- Two different ways to commit (legacy vs category-based)
- Unclear semantics: "committing to a sign-up list" vs "committing to an item"

### Option B: Use Existing Flow (Backend Pattern)

Fix frontend to use correct two-step flow:
1. `POST /events/{id}/signups/{signupId}/open-items` → Creates Open item (already auto-commits)
2. No second call needed (item creation = commitment for Open items)

**Pros**:
- Matches domain model correctly
- No backend changes needed
- Clear separation: create item = auto-commit
- Follows existing pattern

**Cons**:
- Frontend needs refactoring
- Must update OpenItemSignUpModal and hooks
- Different UX flow than expected

### Option C: Dedicated Endpoint for Open Items (Hybrid)

Create `POST /events/{id}/signups/{signupId}/open-items/commit` endpoint that:
- Single call for create + commit
- Clear intent: "commit to Open item"
- Separate from legacy model

**Pros**:
- Clear API semantics
- Maintains DDD principles
- One-step UX for users
- Doesn't conflict with legacy

**Cons**:
- Redundant with existing `AddOpenSignUpItem` command
- Additional endpoint to maintain
- Still creates confusion

## Decision Outcome

**Chosen Option: Option B - Use Existing Flow**

**Rationale**:

1. **Already Implemented Correctly**: The backend domain model (`SignUpList.AddOpenItem()`) already implements the correct behavior - creating an Open item automatically commits the user.

2. **DDD Aggregate Consistency**: Open Items are `SignUpItem` entities within the `SignUpList` aggregate. The commitment is a child entity of the item. The architecture should reflect this hierarchy.

3. **API Clarity**: The existing endpoint semantics are correct:
   - `POST /signups/{id}/open-items` = "Add my custom item to this list"
   - Auto-commitment is a domain rule, not a separate user action

4. **Frontend Misunderstanding**: The frontend incorrectly assumed Open Items followed the legacy "commit to list" pattern. The fix is frontend education, not backend accommodation.

5. **No Redundancy**: Adding a `/commit` endpoint would duplicate `AddOpenSignUpItem` command functionality.

## Implementation Plan

### Backend Changes: NONE REQUIRED

Backend is correctly implemented:
- ✅ Domain model: `SignUpList.AddOpenItem()` creates item + auto-commits
- ✅ Command: `AddOpenSignUpItemCommand` exists
- ✅ Handler: `AddOpenSignUpItemCommandHandler` works correctly
- ✅ Endpoint: `POST /events/{id}/signups/{signupId}/open-items` exists
- ✅ Update: `PUT /events/{id}/signups/{signupId}/open-items/{itemId}` exists
- ✅ Cancel: `DELETE /events/{id}/signups/{signupId}/open-items/{itemId}` exists

### Frontend Changes: REQUIRED

#### 1. Update events.repository.ts

**Remove incorrect method**:
```typescript
// REMOVE THIS (incorrect for Open Items)
async commitToSignUp(eventId: string, signupId: string, request: CommitToSignUpRequest): Promise<void> {
    await apiClient.post<void>(`${this.basePath}/${eventId}/signups/${signupId}/commit`, request);
}
```

**Note**: Keep for legacy open sign-ups (if still used), but don't call for Open Items.

#### 2. Update SignUpManagementSection.tsx

**Current incorrect usage**:
```typescript
// WRONG: Calling commitToSignUp for Open Items
const handleOpenItemSubmit = async (data: OpenItemFormData) => {
    await eventsRepository.commitToSignUp(eventId, signupId, data);
};
```

**Correct usage**:
```typescript
// CORRECT: Call addOpenSignUpItem (which auto-commits)
const handleOpenItemSubmit = async (data: OpenItemFormData) => {
    if (editingItem) {
        // Update existing Open item
        await updateOpenSignUpItem.mutateAsync({
            eventId,
            signupId: signUpList.id,
            itemId: editingItem.id,
            ...data
        });
    } else {
        // Add new Open item (auto-commits user)
        await addOpenSignUpItem.mutateAsync({
            eventId,
            signupId: signUpList.id,
            userId: user.id,
            ...data
        });
    }
};
```

#### 3. Update useEventSignUps.ts Hook

**Issue**: `useCommitToSignUp` hook should NOT be used for Open Items.

**Solution**: Update documentation and ensure Open Items use dedicated hooks:
- `useAddOpenSignUpItem` - Add Open item (includes auto-commit)
- `useUpdateOpenSignUpItem` - Update Open item
- `useCancelOpenSignUpItem` - Cancel/delete Open item

#### 4. Fix HasOpenItems Persistence Issue

**Investigation needed**:
```typescript
// Backend query (GetEventSignUpListsQueryHandler.cs:51)
HasOpenItems = signUpList.HasOpenItems, // ✅ Returns value

// Frontend edit page: Checkbox unchecked after edit
// Possible causes:
// 1. UpdateSignUpList command not receiving hasOpenItems
// 2. Frontend edit form not sending value
// 3. Caching issue
```

**Action**: Check UpdateSignUpListCommand and edit page form submission.

## API Contract Documentation

### Open Items Flow (Category-Based Model)

#### Create Open Item
```http
POST /api/events/{eventId}/signups/{signupId}/open-items
Content-Type: application/json
Authorization: Bearer {token}

{
    "userId": "guid",
    "itemName": "Homemade Cookies",
    "quantity": 24,
    "notes": "Chocolate chip",
    "contactName": "John Doe",
    "contactEmail": "john@example.com",
    "contactPhone": "+1234567890"
}

Response: 200 OK
{
    "value": "item-guid"  // Created item ID
}
```

**Domain Behavior**: Automatically creates commitment for user.

#### Update Open Item
```http
PUT /api/events/{eventId}/signups/{signupId}/open-items/{itemId}
Content-Type: application/json
Authorization: Bearer {token}

{
    "userId": "guid",      // Must match creator
    "itemName": "Updated name",
    "quantity": 30,
    "notes": "Updated notes"
}

Response: 204 No Content
```

**Validation**: Only the creator can update their Open item.

#### Cancel Open Item
```http
DELETE /api/events/{eventId}/signups/{signupId}/open-items/{itemId}?userId={guid}
Authorization: Bearer {token}

Response: 204 No Content
```

**Domain Behavior**: Removes item and associated commitment.

### Legacy Open Sign-Ups (List-Level Commitments)

These endpoints are for the OLD model (SignUpType.Open) where users commit to a list without predefined items.

```http
POST /api/events/{eventId}/signups/{signupId}/commit
DELETE /api/events/{eventId}/signups/{signupId}/commit
```

**Important**: Do NOT use these for Category-Based Open Items.

## Consequences

### Positive

1. **Correct DDD Architecture**: Aggregate boundaries respected
2. **Clear API Semantics**: Endpoint names match intent
3. **No Backend Changes**: Zero risk of regression
4. **Better Documentation**: Clarifies legacy vs new model
5. **Simpler Code**: Remove redundant endpoints

### Negative

1. **Frontend Refactoring Required**: Multiple files need updates
2. **UX Flow Change**: May need UI adjustments
3. **Testing Burden**: Must verify all Open Item scenarios

### Neutral

1. **Learning Curve**: Team must understand two models exist
2. **Migration Path**: Future work to deprecate legacy model

## Testing Strategy

### Unit Tests (Backend - Already Exist)
- ✅ `SignUpList.AddOpenItem()` domain method
- ✅ `AddOpenSignUpItemCommandHandler`
- ✅ Auto-commitment behavior

### Integration Tests (API)
- ✅ POST `/open-items` creates item + commitment
- ✅ PUT `/open-items/{id}` updates owned item
- ✅ DELETE `/open-items/{id}` removes item + commitment
- ❌ Verify 404 on `/signups/{id}/commit` with Open Items

### Frontend Tests
- ❌ OpenItemSignUpModal calls correct endpoint
- ❌ Edit mode updates item correctly
- ❌ Cancel removes item and commitment
- ❌ HasOpenItems checkbox persists on edit

### Manual E2E Tests
1. Create sign-up list with hasOpenItems = true
2. User adds Open item with custom name/quantity
3. Verify item appears in Open category
4. Verify user auto-committed to their item
5. Edit Open item, verify updates persist
6. Cancel Open item, verify removed
7. Edit sign-up list, verify hasOpenItems checkbox state

## Future Considerations

### Deprecate Legacy Model

Consider migrating all legacy `SignUpType.Open` lists to category-based model with `HasOpenItems = true`.

**Migration steps**:
1. Create migration to convert legacy commitments to Open items
2. Update UI to hide legacy model
3. Deprecate legacy endpoints
4. Remove dead code after validation period

### Support for Non-Creators Committing to Open Items

**Current**: Only creator can fulfill their own Open item.

**Future Enhancement**: Allow other users to commit to someone else's Open item (e.g., "I'll help bring some cookies too").

**Impact**: Requires domain model change to allow multiple commitments per Open item.

## References

- Phase 6A.28 Summary: `docs/PHASE_6A_28_OPEN_SIGNUP_ITEMS_SUMMARY.md`
- Domain Model: `src/LankaConnect.Domain/Events/Entities/SignUpList.cs`
- Frontend Repository: `web/src/infrastructure/api/repositories/events.repository.ts`
- API Controller: `src/LankaConnect.API/Controllers/EventsController.cs`

## Glossary

- **Legacy Open Sign-Ups**: Old model where users commit to a SignUpList with custom items (no predefined items)
- **Category-Based Model**: New model with Mandatory/Suggested/Open item categories on SignUpList
- **Open Items**: User-submitted items in the "Open" category (part of category-based model)
- **Auto-Commitment**: Domain rule where creating an Open item automatically commits the creator
