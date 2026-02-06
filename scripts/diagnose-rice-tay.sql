-- Diagnostic Query for Rice Tay Commitment Issue
-- Purpose: Determine if commitments exist in database (Scenario B) or were deleted (Scenario A)

SELECT
    si.id AS item_id,
    si.item_description,
    si.quantity,
    si.remaining_quantity,
    COUNT(sc.id) AS actual_commitment_count,
    STRING_AGG(COALESCE(sc.contact_name, 'NULL') || ' (Qty: ' || sc.quantity || ', User: ' || SUBSTRING(sc.user_id::text, 1, 8) || ')', ', ') AS commitments_detail
FROM sign_up_items si
LEFT JOIN sign_up_commitments sc ON sc.sign_up_item_id = si.id
WHERE si.item_description = 'Rice Tay'
GROUP BY si.id, si.item_description, si.quantity, si.remaining_quantity;

-- Expected Results:
-- If actual_commitment_count = 2 → Scenario B (EF Core bug - commitments exist but not loaded)
-- If actual_commitment_count = 0 → Scenario A (Data corruption - commitments were deleted)
