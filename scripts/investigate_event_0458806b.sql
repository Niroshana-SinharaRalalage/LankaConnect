-- Investigation SQL for Event 0458806b-8672-4ad5-a7cb-f5346f1b282a
-- Run this in Azure Portal Query Editor

-- 1. Check if event exists
SELECT
    id,
    title,
    created_at
FROM events.events
WHERE id = '0458806b-8672-4ad5-a7cb-f5346f1b282a';

-- 2. Check all sign-up lists for this event
SELECT
    sl.id as signup_list_id,
    sl.category as signup_list_name,
    sl.description,
    sl.has_mandatory_items,
    sl.has_preferred_items,
    sl.has_suggested_items,
    sl.sign_up_type,
    sl.created_at,
    sl.updated_at
FROM events.sign_up_lists sl
WHERE sl.event_id = '0458806b-8672-4ad5-a7cb-f5346f1b282a'
ORDER BY sl.created_at DESC;

-- 3. Check items for ALL sign-up lists of this event
SELECT
    si.id as item_id,
    si.sign_up_list_id,
    si.item_description,
    si.quantity,
    si.remaining_quantity,
    si.item_category,
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
    WHERE sl.event_id = '0458806b-8672-4ad5-a7cb-f5346f1b282a'
)
ORDER BY si.sign_up_list_id, si.item_category, si.created_at;

-- 4. Count items per sign-up list
SELECT
    sl.id as signup_list_id,
    sl.category as signup_list_name,
    COUNT(si.id) as total_items_count,
    COUNT(CASE WHEN si.item_category = 0 THEN 1 END) as mandatory_count,
    COUNT(CASE WHEN si.item_category = 1 THEN 1 END) as preferred_count,
    COUNT(CASE WHEN si.item_category = 2 THEN 1 END) as suggested_count
FROM events.sign_up_lists sl
LEFT JOIN events.sign_up_items si ON si.sign_up_list_id = sl.id
WHERE sl.event_id = '0458806b-8672-4ad5-a7cb-f5346f1b282a'
GROUP BY sl.id, sl.category
ORDER BY sl.created_at DESC;

-- 5. Check commitments for this event's sign-up lists
SELECT
    sc.id as commitment_id,
    sc.sign_up_item_id,
    sc.user_id,
    sc.item_description,
    sc.quantity,
    sc.committed_at
FROM events.sign_up_commitments sc
WHERE sc.sign_up_item_id IN (
    SELECT si.id
    FROM events.sign_up_items si
    WHERE si.sign_up_list_id IN (
        SELECT sl.id
        FROM events.sign_up_lists sl
        WHERE sl.event_id = '0458806b-8672-4ad5-a7cb-f5346f1b282a'
    )
)
ORDER BY sc.committed_at DESC;
