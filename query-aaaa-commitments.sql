-- Query to find ALL commitments for the "AAAA" Mandatory item
-- Event ID: 89f8ef9f-af11-4b1a-8dec-b440faef9ad0
-- Item ID from API: 9a01da0e-a801-40fe-8392-36a81031f3f1

SELECT
    c.id as commitment_id,
    c.user_id,
    c.item_description,
    c.quantity as committed_quantity,
    c.contact_name,
    c.contact_email,
    c.contact_phone,
    c.committed_at,
    c.notes,
    u.email as user_email_from_users_table,
    u.first_name,
    u.last_name,
    si.item_description as signup_item_name,
    si.quantity as total_required,
    si.item_category
FROM sign_up_commitments c
INNER JOIN sign_up_items si ON c.sign_up_item_id = si.id
LEFT JOIN users u ON c.user_id = u.id
WHERE si.id = '9a01da0e-a801-40fe-8392-36a81031f3f1'
ORDER BY c.committed_at;

-- Also check if there are any orphaned commitments (sign_up_item_id is NULL)
-- that might be legacy commitments
SELECT
    c.id as commitment_id,
    c.user_id,
    c.sign_up_item_id,
    c.item_description,
    c.quantity,
    c.contact_name,
    c.contact_email,
    c.committed_at,
    u.email as user_email
FROM sign_up_commitments c
LEFT JOIN users u ON c.user_id = u.id
WHERE c.item_description LIKE '%AAAA%'
    AND (c.sign_up_item_id IS NULL OR c.sign_up_item_id = '9a01da0e-a801-40fe-8392-36a81031f3f1')
ORDER BY c.committed_at;

-- Summary: Total commitments and quantities
SELECT
    COUNT(*) as total_commitments,
    SUM(c.quantity) as total_quantity_committed,
    si.quantity as total_required,
    si.quantity - COALESCE(SUM(c.quantity), 0) as remaining
FROM sign_up_items si
LEFT JOIN sign_up_commitments c ON c.sign_up_item_id = si.id
WHERE si.id = '9a01da0e-a801-40fe-8392-36a81031f3f1'
GROUP BY si.id, si.quantity;
