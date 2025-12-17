-- Query signup lists for event 89f8ef9f-af11-4b1a-8dec-b440faef9ad0
SELECT
    id,
    category,
    description,
    has_mandatory_items,
    has_preferred_items,
    has_suggested_items,
    has_open_items,
    created_at
FROM events.sign_up_lists
WHERE event_id = '89f8ef9f-af11-4b1a-8dec-b440faef9ad0'
ORDER BY created_at;
