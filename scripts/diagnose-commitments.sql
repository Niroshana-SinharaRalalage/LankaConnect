-- Root Cause Analysis: Diagnose Missing Commitments for Rice Tay Item
-- Event: Monthly Dana January 2026 (0458806b-8672-4ad5-a7cb-f5346f1b282a)
-- Item: Rice Tay (9dbce508-743a-4cfd-a222-0c3acafd8bbd)
-- Date: 2025-12-19

-- ============================================================================
-- QUERY 1: Event and SignUpList Overview
-- ============================================================================
SELECT
    e.id AS event_id,
    e.title AS event_title,
    e.status,
    sl.id AS signup_list_id,
    sl.category AS signup_category,
    sl.sign_up_type,
    sl.has_mandatory_items,
    sl.has_open_items
FROM events e
JOIN sign_up_lists sl ON sl.event_id = e.id
WHERE e.id = '0458806b-8672-4ad5-a7cb-f5346f1b282a';

-- ============================================================================
-- QUERY 2: Rice Tay Item State Analysis (CRITICAL)
-- ============================================================================
-- This query checks if commitments exist in DB and if quantities match
SELECT
    si.id AS item_id,
    si.item_description,
    si.item_category,
    si.quantity AS total_quantity,
    si.remaining_quantity AS db_remaining_quantity,
    (si.quantity - si.remaining_quantity) AS calculated_committed_qty,
    COUNT(sc.id) AS actual_commitments_in_db,
    COALESCE(SUM(sc.quantity), 0) AS actual_committed_quantity_sum,
    si.created_at AS item_created_at,
    si.updated_at AS item_updated_at
FROM sign_up_items si
LEFT JOIN sign_up_commitments sc ON sc.sign_up_item_id = si.id
WHERE si.id = '9dbce508-743a-4cfd-a222-0c3acafd8bbd'
GROUP BY si.id, si.item_description, si.item_category, si.quantity, si.remaining_quantity, si.created_at, si.updated_at;

-- EXPECTED RESULTS INTERPRETATION:
-- - If actual_commitments_in_db = 0 but calculated_committed_qty = 2:
--   → SCENARIO A: Orphaned quantity (commitments deleted, quantity not recalculated)
-- - If actual_commitments_in_db > 0 but API returns empty array:
--   → SCENARIO B: EF Core hydration failure (data exists, not loaded)

-- ============================================================================
-- QUERY 3: All Commitments for Rice Tay (If Any Exist)
-- ============================================================================
SELECT
    sc.id AS commitment_id,
    sc.sign_up_item_id,
    sc.user_id,
    sc.item_description,
    sc.quantity AS commitment_quantity,
    sc.contact_name,
    sc.contact_email,
    sc.contact_phone,
    sc.notes,
    sc.committed_at,
    sc.created_at,
    sc.updated_at,
    u.name AS user_name,
    u.email AS user_email
FROM sign_up_commitments sc
LEFT JOIN users u ON u.id = sc.user_id
WHERE sc.sign_up_item_id = '9dbce508-743a-4cfd-a222-0c3acafd8bbd'
ORDER BY sc.committed_at DESC;

-- ============================================================================
-- QUERY 4: All Items in the Same SignUpList (Context)
-- ============================================================================
SELECT
    si.id AS item_id,
    si.item_description,
    si.item_category,
    si.quantity AS total_qty,
    si.remaining_quantity,
    (si.quantity - si.remaining_quantity) AS calculated_committed,
    COUNT(sc.id) AS actual_commitments,
    COALESCE(SUM(sc.quantity), 0) AS actual_committed_sum
FROM sign_up_items si
LEFT JOIN sign_up_commitments sc ON sc.sign_up_item_id = si.id
WHERE si.sign_up_list_id IN (
    SELECT sign_up_list_id
    FROM sign_up_items
    WHERE id = '9dbce508-743a-4cfd-a222-0c3acafd8bbd'
)
GROUP BY si.id, si.item_description, si.item_category, si.quantity, si.remaining_quantity
ORDER BY si.item_category, si.item_description;

-- REFERENCE: Working Item (Boiled Eggs) for comparison
-- Item ID: 17f4d475-90ce-479a-bffe-63044e634cdc
-- Expected: commitments array NOT empty, quantities match

-- ============================================================================
-- QUERY 5: Check for Soft-Delete Columns (Potential Hidden Data)
-- ============================================================================
SELECT
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name = 'sign_up_commitments'
  AND column_name IN ('is_deleted', 'deleted_at', 'deleted', 'status', 'is_active')
ORDER BY ordinal_position;

-- ============================================================================
-- QUERY 6: Orphaned Quantities Across ALL Items (System-Wide Audit)
-- ============================================================================
-- Find all items where calculated committed quantity doesn't match actual commitments
SELECT
    e.title AS event_title,
    sl.category AS signup_category,
    si.id AS item_id,
    si.item_description,
    si.quantity AS total_qty,
    si.remaining_quantity,
    (si.quantity - si.remaining_quantity) AS calculated_committed,
    COUNT(sc.id) AS actual_commitments_count,
    COALESCE(SUM(sc.quantity), 0) AS actual_committed_sum,
    ABS((si.quantity - si.remaining_quantity) - COALESCE(SUM(sc.quantity), 0)) AS discrepancy
FROM events e
JOIN sign_up_lists sl ON sl.event_id = e.id
JOIN sign_up_items si ON si.sign_up_list_id = sl.id
LEFT JOIN sign_up_commitments sc ON sc.sign_up_item_id = si.id
GROUP BY e.title, sl.category, si.id, si.item_description, si.quantity, si.remaining_quantity
HAVING (si.quantity - si.remaining_quantity) != COALESCE(SUM(sc.quantity), 0)
ORDER BY discrepancy DESC;

-- ============================================================================
-- QUERY 7: Recent Commitment Activity (Audit Trail)
-- ============================================================================
-- Check for recently deleted or modified commitments
SELECT
    sc.id AS commitment_id,
    sc.sign_up_item_id,
    sc.item_description,
    sc.quantity,
    sc.user_id,
    sc.committed_at,
    sc.created_at,
    sc.updated_at,
    CASE
        WHEN sc.updated_at IS NULL THEN 'never modified'
        WHEN sc.updated_at > sc.created_at + INTERVAL '1 minute' THEN 'modified'
        ELSE 'created recently'
    END AS status
FROM sign_up_commitments sc
WHERE sc.sign_up_item_id IN (
    SELECT id FROM sign_up_items
    WHERE sign_up_list_id IN (
        SELECT id FROM sign_up_lists
        WHERE event_id = '0458806b-8672-4ad5-a7cb-f5346f1b282a'
    )
)
ORDER BY sc.created_at DESC
LIMIT 50;

-- ============================================================================
-- QUERY 8: Foreign Key Integrity Check
-- ============================================================================
-- Verify all commitments have valid foreign keys
SELECT
    'Orphaned Commitments' AS check_type,
    COUNT(*) AS count
FROM sign_up_commitments sc
LEFT JOIN sign_up_items si ON si.id = sc.sign_up_item_id
WHERE si.id IS NULL

UNION ALL

SELECT
    'Orphaned Items' AS check_type,
    COUNT(*) AS count
FROM sign_up_items si
LEFT JOIN sign_up_lists sl ON sl.id = si.sign_up_list_id
WHERE sl.id IS NULL;

-- ============================================================================
-- REPAIR QUERY (DO NOT RUN YET - FOR REFERENCE ONLY)
-- ============================================================================
-- Uncomment and run ONLY if Query 2 confirms Scenario A (orphaned quantity)
/*
UPDATE sign_up_items si
SET
    remaining_quantity = si.quantity - COALESCE(
        (SELECT SUM(sc.quantity)
         FROM sign_up_commitments sc
         WHERE sc.sign_up_item_id = si.id),
        0
    ),
    updated_at = NOW()
WHERE si.id = '9dbce508-743a-4cfd-a222-0c3acafd8bbd';

-- Verify the repair
SELECT
    item_description,
    quantity,
    remaining_quantity,
    (quantity - remaining_quantity) AS calculated_committed,
    (SELECT COUNT(*) FROM sign_up_commitments WHERE sign_up_item_id = si.id) AS actual_commitments
FROM sign_up_items si
WHERE id = '9dbce508-743a-4cfd-a222-0c3acafd8bbd';
*/

-- ============================================================================
-- SYSTEM-WIDE REPAIR (DO NOT RUN YET - FOR REFERENCE ONLY)
-- ============================================================================
-- Recalculate ALL items with discrepancies
/*
UPDATE sign_up_items si
SET
    remaining_quantity = si.quantity - COALESCE(
        (SELECT SUM(sc.quantity)
         FROM sign_up_commitments sc
         WHERE sc.sign_up_item_id = si.id),
        0
    ),
    updated_at = NOW()
WHERE (si.quantity - si.remaining_quantity) != COALESCE(
    (SELECT SUM(sc.quantity)
     FROM sign_up_commitments sc
     WHERE sc.sign_up_item_id = si.id),
    0
);

-- Report on repairs
SELECT
    COUNT(*) AS items_repaired
FROM sign_up_items si
WHERE si.updated_at > NOW() - INTERVAL '1 minute';
*/
