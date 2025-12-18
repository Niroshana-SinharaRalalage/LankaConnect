-- ADR-007 Verification Script: Commitment Deletion After RSVP Cancellation
-- Purpose: Verify that Open Items commitments are properly deleted when users cancel RSVP
-- Expected: 0 commitments for the test user after cancellation

-- STEP 1: Check if user has any commitments BEFORE cancellation
-- Run this BEFORE calling the cancel-rsvp API endpoint
SELECT
    c.id AS commitment_id,
    c.user_id,
    c.signup_item_id,
    c.quantity AS committed_quantity,
    i.description AS item_description,
    i.remaining_quantity,
    e.title AS event_title
FROM signup_item_commitments c
JOIN signup_items i ON c.signup_item_id = i.id
JOIN signup_lists sl ON i.signup_list_id = sl.id
JOIN events e ON sl.event_id = e.id
WHERE c.user_id = 'REPLACE_WITH_USER_ID'
  AND e.id = 'REPLACE_WITH_EVENT_ID'
ORDER BY c.created_at DESC;

-- Expected output: One or more rows showing user's commitments
-- Note the commitment_id and remaining_quantity values for verification


-- STEP 2: Cancel RSVP via API
-- POST /api/events/{eventId}/cancel-rsvp
-- Body: { "deleteSignUpCommitments": true }


-- STEP 3: Verify commitments are DELETED after cancellation
-- Run this AFTER calling the cancel-rsvp API endpoint
SELECT
    c.id AS commitment_id,
    c.user_id,
    c.signup_item_id,
    c.quantity AS committed_quantity,
    i.description AS item_description,
    i.remaining_quantity,
    e.title AS event_title
FROM signup_item_commitments c
JOIN signup_items i ON c.signup_item_id = i.id
JOIN signup_lists sl ON i.signup_list_id = sl.id
JOIN events e ON sl.event_id = e.id
WHERE c.user_id = 'REPLACE_WITH_USER_ID'
  AND e.id = 'REPLACE_WITH_EVENT_ID'
ORDER BY c.created_at DESC;

-- Expected output: 0 rows (commitments deleted)
-- If this returns rows, the fix FAILED


-- STEP 4: Verify remaining_quantity was restored
-- Check that the item's remaining_quantity increased by committed_quantity
SELECT
    i.id AS item_id,
    i.description,
    i.needed_quantity,
    i.remaining_quantity,
    (i.needed_quantity - i.remaining_quantity) AS total_committed,
    sl.title AS signup_list_title
FROM signup_items i
JOIN signup_lists sl ON i.signup_list_id = sl.id
WHERE sl.event_id = 'REPLACE_WITH_EVENT_ID'
  AND i.id = 'REPLACE_WITH_ITEM_ID'
ORDER BY i.description;

-- Expected: remaining_quantity should equal (previous_remaining_quantity + committed_quantity)
-- Example: If previous remaining_quantity was 5, committed_quantity was 3,
--          new remaining_quantity should be 8


-- STEP 5: Verify registration status
-- Check that the registration status is 'Cancelled'
SELECT
    r.id AS registration_id,
    r.user_id,
    r.event_id,
    r.status,
    r.updated_at,
    e.title AS event_title
FROM registrations r
JOIN events e ON r.event_id = e.id
WHERE r.user_id = 'REPLACE_WITH_USER_ID'
  AND r.event_id = 'REPLACE_WITH_EVENT_ID'
ORDER BY r.updated_at DESC
LIMIT 1;

-- Expected: status = 'Cancelled'
-- Expected: updated_at shows recent timestamp (when cancel-rsvp was called)


-- DIAGNOSTIC QUERY: If commitments NOT deleted, check entity state
-- This helps identify if the issue is with EF Core tracking or database persistence
SELECT
    e.id AS event_id,
    e.title,
    e.updated_at AS event_updated_at,
    COUNT(DISTINCT sl.id) AS signup_lists_count,
    COUNT(DISTINCT i.id) AS items_count,
    COUNT(c.id) AS total_commitments,
    SUM(CASE WHEN c.user_id = 'REPLACE_WITH_USER_ID' THEN 1 ELSE 0 END) AS user_commitments
FROM events e
LEFT JOIN signup_lists sl ON sl.event_id = e.id
LEFT JOIN signup_items i ON i.signup_list_id = sl.id
LEFT JOIN signup_item_commitments c ON c.signup_item_id = i.id
WHERE e.id = 'REPLACE_WITH_EVENT_ID'
GROUP BY e.id, e.title, e.updated_at;

-- This shows overall event state and whether commitments still exist in database
