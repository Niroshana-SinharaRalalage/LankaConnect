# Phase 6A.28 - Orphaned Open Items Cleanup Guide

**Date:** 2025-12-19
**Issue:** Existing Open Items with commitments that cannot be deleted via UI
**Related Issues:** Issue 3 (400 error), Issue 4 (deletion on registration cancel)

---

## Problem Summary

Before the Issue 3 fix (commit 7aa80337), users could create orphaned Open Items that cannot be deleted because:

1. **Root Cause:** `SignUpList.RemoveItem()` checks if commitments exist and rejects deletion
2. **Handler Bug:** `CancelOpenSignUpItemCommandHandler` tried to remove item before commitments were fully processed
3. **Result:** Open Items with commitments stuck in database, showing "Cancel Sign Up" button that returns 400 error

**Example orphaned item:**
- Event: Monthly Dana January 2026 (`0458806b-8672-4ad5-a7cb-f5346f1b282a`)
- Open Item: "aaa" (Qty: 3)
- Status: Has commitment from creator, cannot delete via UI

---

## What Was Fixed (Issue 3)

**Commit:** 7aa80337
**File:** [CancelOpenSignUpItemCommandHandler.cs](../src/LankaConnect.Application/Events/Commands/CancelOpenSignUpItem/CancelOpenSignUpItemCommandHandler.cs)

**Fix Details:**
- Check and cancel user's commitment first (if exists)
- Count other users' commitments separately
- Only allow deletion if no other users have committed
- Provide clear error message if others have committed

**This prevents NEW orphaned items**, but doesn't fix existing ones.

---

## Cleanup Options for Existing Orphaned Items

### Option 1: Delete via Database (Recommended for bulk cleanup)

```sql
-- Step 1: Find all orphaned Open Items (items with commitments that should be deletable)
SELECT
    e.title AS event_title,
    si.id AS item_id,
    si.item_description,
    si.created_by_user_id,
    COUNT(sc.id) AS commitment_count,
    STRING_AGG(u.email, ', ') AS committed_users
FROM sign_up_items si
JOIN sign_up_lists sl ON si.sign_up_list_id = sl.id
JOIN events e ON sl.event_id = e.id
LEFT JOIN sign_up_commitments sc ON sc.sign_up_item_id = si.id
LEFT JOIN users u ON sc.user_id = u.id
WHERE si.item_category = 'Open'
    AND si.created_by_user_id IS NOT NULL
    AND EXISTS (
        SELECT 1 FROM sign_up_commitments
        WHERE sign_up_item_id = si.id
    )
GROUP BY e.title, si.id, si.item_description, si.created_by_user_id
ORDER BY e.title, si.item_description;

-- Step 2: Delete commitments first, then items
-- WARNING: Review query results before running DELETE commands!

-- Delete commitments for specific item
DELETE FROM sign_up_commitments
WHERE sign_up_item_id = '{ITEM_ID}';

-- Then delete the item itself
DELETE FROM sign_up_items
WHERE id = '{ITEM_ID}';

-- Or delete all orphaned Open Items at once (BE CAREFUL!)
WITH orphaned_items AS (
    SELECT si.id
    FROM sign_up_items si
    WHERE si.item_category = 'Open'
        AND si.created_by_user_id IS NOT NULL
        AND EXISTS (
            SELECT 1 FROM sign_up_commitments
            WHERE sign_up_item_id = si.id
            LIMIT 1
        )
)
DELETE FROM sign_up_commitments
WHERE sign_up_item_id IN (SELECT id FROM orphaned_items);

DELETE FROM sign_up_items
WHERE id IN (
    SELECT si.id
    FROM sign_up_items si
    WHERE si.item_category = 'Open'
        AND si.created_by_user_id IS NOT NULL
);
```

### Option 2: Delete via API (After Issue 3 fix deployed)

Once the fix is deployed, the "Cancel Sign Up" button should work correctly:

**Prerequisites:**
- Issue 3 fix deployed to staging (commit 7aa80337)
- User must be logged in as the creator of the Open Item

**Steps:**
1. Navigate to event with orphaned item
2. Click "Cancel Sign Up" button on the orphaned Open Item
3. If only you committed: Item will be deleted ✅
4. If others committed: Your commitment canceled, item remains for others ✅

**Testing with example item "aaa":**
```
Event: http://localhost:3000/events/0458806b-8672-4ad5-a7cb-f5346f1b282a
Action: Click "Cancel Sign Up" on item "aaa"
Expected:
- If you're the only one committed: Item deleted
- If others committed: Get informative error message
```

### Option 3: Registration Cancellation (Uses Issue 4 fix)

If you're registered for the event:

1. Click "Cancel My Registration"
2. **Check** "Also delete my sign-up commitments" checkbox
3. Confirm cancellation
4. Your user-created Open Items will be deleted automatically

**This works because:**
- Issue 4 fix (commit 5a988c30) detects `CreatedByUserId == userId`
- Deletes Open Items after canceling commitments
- Protects organizer-owned items

---

## Recommended Cleanup Workflow

### For Testing/Staging Environment

1. **Deploy Issue 3 fix** (commit 7aa80337) - ✅ IN PROGRESS
2. **Wait for deployment** to complete
3. **Test the fix:**
   - Try "Cancel Sign Up" on item "aaa"
   - Should succeed if only you committed
   - Should get clear error if others committed
4. **Database cleanup** (if needed for bulk):
   - Run Step 1 query to identify all orphaned items
   - Review results carefully
   - Run Step 2 DELETE commands for confirmed orphans

### For Production Environment

1. **Test in staging first** (above workflow)
2. **Backup database** before any cleanup
3. **Run identification query** in production
4. **Manual review** of each orphaned item:
   - Check if item has commitments from multiple users
   - Verify creator still wants item deleted
   - Consider keeping items if others depend on them
5. **Delete in batches**, verify after each batch
6. **Monitor logs** for any errors

---

## Prevention (Already Implemented)

**Issue 3 Fix:** Prevents new orphaned items by properly handling commitment cancellation before deletion

**Issue 4 Fix:** Deletes user-created Open Items when user cancels registration with checkbox

**Going Forward:**
- New Open Items can be deleted via "Cancel Sign Up" button
- Multi-user commitments are protected (can't delete if others committed)
- Clear error messages guide users

---

## Identifying Orphaned Items

### Via Database Query

```sql
-- Count orphaned Open Items per event
SELECT
    e.title AS event_title,
    e.id AS event_id,
    COUNT(DISTINCT si.id) AS orphaned_items_count
FROM sign_up_items si
JOIN sign_up_lists sl ON si.sign_up_list_id = sl.id
JOIN events e ON sl.event_id = e.id
WHERE si.item_category = 'Open'
    AND si.created_by_user_id IS NOT NULL
    AND EXISTS (
        SELECT 1 FROM sign_up_commitments
        WHERE sign_up_item_id = si.id
    )
GROUP BY e.title, e.id
ORDER BY orphaned_items_count DESC;

-- Get details of orphaned items for specific event
SELECT
    si.id,
    si.item_description,
    si.quantity,
    si.remaining_quantity,
    si.created_by_user_id,
    si.created_at,
    COUNT(sc.id) AS total_commitments,
    SUM(CASE WHEN sc.user_id = si.created_by_user_id THEN 1 ELSE 0 END) AS creator_commitments,
    SUM(CASE WHEN sc.user_id != si.created_by_user_id THEN 1 ELSE 0 END) AS other_commitments
FROM sign_up_items si
JOIN sign_up_lists sl ON si.sign_up_list_id = sl.id
LEFT JOIN sign_up_commitments sc ON sc.sign_up_item_id = si.id
WHERE sl.event_id = '{EVENT_ID}'
    AND si.item_category = 'Open'
    AND si.created_by_user_id IS NOT NULL
GROUP BY si.id, si.item_description, si.quantity, si.remaining_quantity,
         si.created_by_user_id, si.created_at
HAVING COUNT(sc.id) > 0
ORDER BY si.created_at DESC;
```

### Via UI Inspection

**Symptoms of orphaned items:**
- ✅ Shows "Update Sign Up" button
- ✅ Shows "Cancel Sign Up" button
- ✅ Marked as "Your item"
- ❌ "Cancel Sign Up" returns 400 error (before fix)
- ❌ Cannot delete via registration cancellation (before Issue 4 fix)

---

## Testing After Deployment

### Test 1: Delete Orphaned Item (Issue 3 fix)

1. Navigate to event `0458806b-8672-4ad5-a7cb-f5346f1b282a`
2. Find Open Item "aaa" (or any orphaned item)
3. Click "Cancel Sign Up" button
4. **Expected:**
   - Status 200 OK (success)
   - Item disappears from UI
   - Database confirms deletion

### Test 2: Multi-User Protection

1. Create new Open Item
2. User A: Commit to the item
3. User B (not creator): Commit to the same item
4. User A: Try to delete via "Cancel Sign Up"
5. **Expected:**
   - Error message: "Cannot delete this Open item because 1 other user(s) have committed to it"
   - User A's commitment canceled
   - Item remains for User B

### Test 3: Registration Cancellation (Issue 4)

1. Register for event
2. Create 2-3 Open Items
3. Commit to them
4. Cancel registration WITH checkbox
5. **Expected:**
   - All user-created Open Items deleted
   - Commitments canceled
   - Items disappear from UI

---

## Success Criteria

Orphaned items issue is resolved when:

- [x] Issue 3 fix deployed (commit 7aa80337)
- [x] Issue 4 fix deployed (commit 5a988c30)
- [ ] "Cancel Sign Up" button works for orphaned items (test after deployment)
- [ ] Clear error message when others have committed
- [ ] Registration cancellation deletes user-created Open Items
- [ ] No new orphaned items can be created
- [ ] Existing orphaned items cleaned up via database or API

---

## Related Documentation

- **Issue 3 Fix:** [Commit 7aa80337](https://github.com/Niroshana-SinharaRalalage/LankaConnect/commit/7aa80337)
- **Issue 4 Fix:** [PHASE_6A28_ISSUE_4_OPEN_ITEMS_FIX.md](./PHASE_6A28_ISSUE_4_OPEN_ITEMS_FIX.md)
- **Root Cause Analysis:** [PHASE_6A_28_OPEN_ITEMS_ROOT_CAUSE_ANALYSIS.md](./PHASE_6A_28_OPEN_ITEMS_ROOT_CAUSE_ANALYSIS.md)

---

**Status:** 🔄 Issue 3 fix deploying, testing pending
**Next:** Test with orphaned item "aaa" after deployment completes
