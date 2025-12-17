-- Test query to verify commitment deletion
-- Event ID: 89f8ef9f-af11-4b1a-8dec-b440faef9ad0
-- User ID: 5e782b4d-29ed-4e1d-9039-6c8f698aeea9

-- Step 1: Check current commitments BEFORE deletion
SELECT
    c.id as commitment_id,
    c.user_id,
    c.sign_up_item_id,
    c.item_description,
    c.quantity,
    c.contact_name,
    si.remaining_quantity as item_remaining_qty,
    si.quantity as item_total_qty
FROM events.sign_up_commitments c
INNER JOIN events.sign_up_items si ON c.sign_up_item_id = si.id
INNER JOIN events.sign_up_lists sl ON si.sign_up_list_id = sl.id
WHERE sl.event_id = '89f8ef9f-af11-4b1a-8dec-b440faef9ad0'
  AND c.user_id = '5e782b4d-29ed-4e1d-9039-6c8f698aeea9'
ORDER BY c.committed_at;

-- Step 2: Check if the commitments are actually getting deleted
-- Run this AFTER cancelling with checkbox checked
