-- Diagnostic SQL Query for Phase 6A.28 Issue 4
-- Check signup commitments state after registration cancellation

-- Replace these with actual values from your test:
-- @event_id: The event ID you're testing with
-- @user_id: The user ID who cancelled registration

-- Check if commitments still exist for the user
SELECT
    sc.id,
    sc.user_id,
    sc.quantity,
    sc.created_at,
    sc.updated_at,
    si.item_description,
    si.remaining_quantity,
    sl.category
FROM signup_commitments sc
INNER JOIN signup_items si ON sc.signup_item_id = si.id
INNER JOIN signup_lists sl ON si.signup_list_id = sl.id
WHERE sl.event_id = @event_id
  AND sc.user_id = @user_id
ORDER BY sc.created_at DESC;

-- Check remaining quantities for all items in the event
SELECT
    sl.category,
    si.item_description,
    si.required_quantity,
    si.remaining_quantity,
    COUNT(sc.id) as commitment_count,
    COALESCE(SUM(sc.quantity), 0) as total_committed
FROM signup_lists sl
INNER JOIN signup_items si ON si.signup_list_id = sl.id
LEFT JOIN signup_commitments sc ON sc.signup_item_id = si.id
WHERE sl.event_id = @event_id
GROUP BY sl.category, si.item_description, si.required_quantity, si.remaining_quantity
ORDER BY sl.category, si.item_description;

-- Check registration status
SELECT
    r.id,
    r.user_id,
    r.event_id,
    r.status,
    r.updated_at
FROM registrations r
WHERE r.event_id = @event_id
  AND r.user_id = @user_id
ORDER BY r.updated_at DESC;

-- Expected Results After Successful Deletion:
-- 1. First query should return 0 rows (no commitments for user)
-- 2. Second query should show increased remaining_quantity values
-- 3. Third query should show status = 'Cancelled'
