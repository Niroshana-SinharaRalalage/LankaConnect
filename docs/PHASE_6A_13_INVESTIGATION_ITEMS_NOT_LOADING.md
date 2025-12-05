# Phase 6A.13: Investigation - Items Not Loading in Edit Page

**Date**: 2025-12-04
**Status**: Investigation in progress
**Issue**: Items showing "No items added yet" in edit page tables despite items existing

## Problem Statement

User reported that the Edit Sign-Up List page at `/events/[id]/manage-signups/[signupId]/page.tsx` shows "No items added yet" in all three category tables (Mandatory, Preferred, Suggested), even though items should exist in the database for this sign-up list.

## Investigation Summary

### ✅ Backend Structure - VERIFIED CORRECT

1. **EventRepository.cs (lines 24-28)**: Query correctly includes Items collection:
```csharp
.Include(e => e.SignUpLists)
    .ThenInclude(s => s.Items)
        .ThenInclude(i => i.Commitments)
```

2. **SignUpItem.cs (line 18)**: `ItemCategory` property exists with correct enum type `SignUpItemCategory`

3. **AppDbContext.cs**:
   - SignUpItem entity is configured (line 65)
   - Mapped to `events.sign_up_items` table (line 112)
   - Has proper ItemCategory column

4. **API Endpoint**: `/api/events/{id}/signups` correctly calls `GetEventSignUpListsQuery`

5. **Query Handler** (GetEventSignUpListsQueryHandler.cs lines 23-73): Properly maps Items array with all properties including `ItemCategory`

### ✅ Frontend Structure - VERIFIED CORRECT

1. **useEventSignUps hook** (useEventSignUps.ts lines 67-76): Correctly calls `eventsRepository.getEventSignUpLists(eventId)`

2. **Type definitions** (events.types.ts): SignUpListDto has `items: SignUpItemDto[]` array with `itemCategory: SignUpItemCategory` property

3. **Edit page data fetching** (page.tsx lines 83-98): Debug logging added to track what data is loaded:
```typescript
console.log('[EditSignUpList] Sign-up list loaded:', signUpList);
console.log('[EditSignUpList] Items array:', signUpList.items);
console.log('[EditSignUpList] Items count:', signUpList.items?.length || 0);
```

4. **Item filtering** (page.tsx lines 321-330): Correctly filters items by category:
```typescript
const mandatoryItems = signUpList.items?.filter(item => item.itemCategory === SignUpItemCategory.Mandatory) || [];
```

### ❓ UNKNOWN: Database State

The investigation has confirmed that the code structure is correct. The issue is either:
1. **No items exist in database** for this specific sign-up list
2. **API is not returning items** for some reason (but the query includes them)

## Next Steps for User

### 1. Check Browser Console Logs

Open the edit page in the browser and check the console for these logs:
```
[EditSignUpList] Sign-up list loaded: { ... }
[EditSignUpList] Items array: [...]
[EditSignUpList] Items count: X
[EditSignUpList] Filtered items: { mandatoryItems: X, preferredItems: Y, suggestedItems: Z }
```

**What to look for:**
- Is `items` array present in the sign-up list object?
- Is `items.length` 0 or > 0?
- What do the filtered counts show?

### 2. Check Database Using SQL Script

Run the SQL script located at `scripts/check_signup_items.sql` in Azure Portal Query Editor:

```sql
-- This will show all sign-up lists and their items
SELECT
    sl.id as signup_list_id,
    sl.category as signup_list_name,
    sl.has_mandatory_items,
    sl.has_preferred_items,
    sl.has_suggested_items,
    si.id as item_id,
    si.item_description,
    si.quantity,
    si.remaining_quantity,
    si.item_category,
    CASE si.item_category
        WHEN 0 THEN 'Mandatory'
        WHEN 1 THEN 'Preferred'
        WHEN 2 THEN 'Suggested'
        ELSE 'Unknown'
    END as category_name
FROM events.sign_up_lists sl
LEFT JOIN events.sign_up_items si ON si.sign_up_list_id = sl.id
ORDER BY sl.created_at DESC, si.item_category ASC, si.created_at DESC;
```

**What to look for:**
- Do items exist in `events.sign_up_items` table?
- Are they linked to the correct `sign_up_list_id`?
- Do they have correct `item_category` values (0, 1, or 2)?

### 3. Check Network Tab

In browser DevTools > Network tab:
1. Filter for API calls
2. Look for the call to `/api/events/{eventId}/signups`
3. Check the response body
4. Does it include an `items` array for the sign-up list?

## Possible Issues and Fixes

### Issue 1: No Items in Database
**Symptom**: SQL query returns no rows for sign_up_items table
**Fix**: Items were never created. User needs to use the CREATE page to add items, not just the edit page.

### Issue 2: Items Exist But Not Linked
**Symptom**: Items exist but `sign_up_list_id` doesn't match
**Fix**: Database integrity issue - need to investigate how items were created.

### Issue 3: API Not Including Items
**Symptom**: Database has items, but API response doesn't include them
**Fix**: Check if EventRepository.GetByIdAsync is being called correctly and includes SignUpLists.Items

### Issue 4: Frontend Filtering Wrong
**Symptom**: API returns items, but frontend shows "No items added yet"
**Fix**: Check if `SignUpItemCategory` enum values match (0, 1, 2) in both frontend and backend.

## Files Investigated

- ✅ `src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs`
- ✅ `src/LankaConnect.Domain/Events/Entities/SignUpItem.cs`
- ✅ `src/LankaConnect.Infrastructure/Data/AppDbContext.cs`
- ✅ `src/LankaConnect.API/Controllers/EventsController.cs`
- ✅ `src/LankaConnect.Application/Events/Queries/GetEventSignUpLists/GetEventSignUpListsQueryHandler.cs`
- ✅ `web/src/presentation/hooks/useEventSignUps.ts`
- ✅ `web/src/infrastructure/api/types/events.types.ts`
- ✅ `web/src/app/events/[id]/manage-signups/[signupId]/page.tsx`

## Debug Logging Added

Added comprehensive logging in edit page (lines 83-98, 326-330):
```typescript
// When sign-up list loads
console.log('[EditSignUpList] Sign-up list loaded:', signUpList);
console.log('[EditSignUpList] Items array:', signUpList.items);
console.log('[EditSignUpList] Items count:', signUpList.items?.length || 0);
console.log('[EditSignUpList] Category flags:', { ... });

// After filtering
console.log('[EditSignUpList] Filtered items:', {
  mandatoryItems: mandatoryItems.length,
  preferredItems: preferredItems.length,
  suggestedItems: suggestedItems.length
});
```

## Conclusion

The code structure is correct on both backend and frontend. The issue is most likely that **no items exist in the database** for this specific sign-up list. User needs to check:
1. Browser console logs to see what data is actually being loaded
2. Database directly using the provided SQL script
3. Network tab to see API response

Once we know which of these is the issue, we can implement the appropriate fix.
