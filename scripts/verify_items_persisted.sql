-- Quick verification query to check if items are being persisted
-- Run this after creating a new sign-up list

-- Step 1: Check the most recent sign-up list
SELECT
    sl.id,
    sl.category as signup_list_name,
    sl.has_mandatory_items,
    sl.has_preferred_items,
    sl.has_suggested_items,
    sl.created_at,
    COUNT(si.id) as total_items_count
FROM events.sign_up_lists sl
LEFT JOIN events.sign_up_items si ON si.sign_up_list_id = sl.id
GROUP BY sl.id, sl.category, sl.has_mandatory_items, sl.has_preferred_items, sl.has_suggested_items, sl.created_at
ORDER BY sl.created_at DESC
LIMIT 5;

-- Step 2: Check all items for the most recent sign-up list
SELECT
    si.id as item_id,
    si.sign_up_list_id,
    si.item_description,
    si.quantity,
    si.remaining_quantity,
    si.item_category, -- 0=Mandatory, 1=Preferred, 2=Suggested
    CASE si.item_category
        WHEN 0 THEN 'Mandatory'
        WHEN 1 THEN 'Preferred'
        WHEN 2 THEN 'Suggested'
        ELSE 'Unknown'
    END as category_name,
    si.notes,
    si.created_at
FROM events.sign_up_items si
WHERE si.sign_up_list_id IN (
    SELECT sl.id
    FROM events.sign_up_lists sl
    ORDER BY sl.created_at DESC
    LIMIT 5
)
ORDER BY si.sign_up_list_id, si.item_category, si.created_at;
