-- Check sign-up lists and their items
-- Run this in Azure Portal Query Editor or psql

-- 1. Check all sign-up lists
SELECT
    sl.id as signup_list_id,
    sl.category,
    sl.description,
    sl.has_mandatory_items,
    sl.has_preferred_items,
    sl.has_suggested_items,
    sl.created_at
FROM events.sign_up_lists sl
ORDER BY sl.created_at DESC
LIMIT 10;

-- 2. Check all sign-up items and their categories
SELECT
    si.id as item_id,
    si.sign_up_list_id,
    si.item_description,
    si.quantity,
    si.remaining_quantity,
    si.item_category, -- 0=Mandatory, 1=Preferred, 2=Suggested
    si.notes,
    si.created_at
FROM events.sign_up_items si
ORDER BY si.created_at DESC
LIMIT 20;

-- 3. Join to see items with their sign-up list names
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

-- 4. Check for specific sign-up list (replace with actual ID from screenshot)
-- SELECT
--     sl.*,
--     json_agg(
--         json_build_object(
--             'id', si.id,
--             'itemDescription', si.item_description,
--             'quantity', si.quantity,
--             'remainingQuantity', si.remaining_quantity,
--             'itemCategory', si.item_category,
--             'notes', si.notes
--         ) ORDER BY si.item_category, si.created_at
--     ) as items
-- FROM events.sign_up_lists sl
-- LEFT JOIN events.sign_up_items si ON si.sign_up_list_id = sl.id
-- WHERE sl.id = 'YOUR-SIGNUP-LIST-ID-HERE'
-- GROUP BY sl.id;
