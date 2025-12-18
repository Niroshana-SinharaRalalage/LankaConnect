-- ============================================================================
-- QUICK DIAGNOSTIC - Phase 6A.24 Webhook Recovery
-- Run these 3 queries to diagnose webhook evt_1SfZoMLvfbr023L1xcB9paik
-- ============================================================================

-- QUERY 1: Find the registration ID
-- Copy the registration_id from results - you'll need it for recovery
SELECT
    r.id as registration_id,
    r.event_id,
    r.user_id,
    r.contact_email,
    r.status,
    r.payment_status,
    r.total_price,
    r.created_at
FROM events.registrations r
WHERE r.stripe_checkout_session_id LIKE '%cs_test%'
  AND r.created_at > '2025-12-17 00:00:00'::timestamptz
ORDER BY r.created_at DESC
LIMIT 5;

-- QUERY 2: Check if ticket exists
SELECT
    t.id as ticket_id,
    t.ticket_code,
    t.status,
    t.created_at,
    CASE WHEN t.id IS NOT NULL THEN '✅ TICKET EXISTS' ELSE '❌ NO TICKET' END as result
FROM events.registrations r
LEFT JOIN tickets.tickets t ON t.registration_id = r.id
WHERE r.stripe_checkout_session_id LIKE '%cs_test%'
  AND r.created_at > '2025-12-17 00:00:00'::timestamptz
ORDER BY r.created_at DESC
LIMIT 5;

-- QUERY 3: Check if email was sent
SELECT
    em.id as email_id,
    em.to_emails,
    em.subject,
    em.status,
    em.created_at,
    em.sent_at,
    em.error_message,
    CASE
        WHEN em.status = 'Sent' THEN '✅ EMAIL SENT'
        WHEN em.status = 'Failed' THEN '❌ EMAIL FAILED'
        WHEN em.status = 'Queued' THEN '⏳ EMAIL QUEUED'
        WHEN em.id IS NULL THEN '❌ NO EMAIL'
        ELSE em.status
    END as result
FROM events.registrations r
LEFT JOIN communications.email_messages em
    ON em.to_emails::text LIKE '%' || r.contact_email || '%'
WHERE r.stripe_checkout_session_id LIKE '%cs_test%'
  AND r.created_at > '2025-12-17 00:00:00'::timestamptz
  AND (em.created_at > '2025-12-17 00:00:00'::timestamptz OR em.id IS NULL)
ORDER BY r.created_at DESC, em.created_at DESC
LIMIT 10;

-- ============================================================================
-- WHAT TO LOOK FOR:
--
-- QUERY 1: Copy the registration_id (you need this for recovery curl command)
-- QUERY 2: If result = "❌ NO TICKET" → RECOVERY NEEDED
-- QUERY 3: If result = "❌ NO EMAIL" or "❌ EMAIL FAILED" → RECOVERY NEEDED
--
-- If both ticket and email are missing, run the recovery curl command below
-- ============================================================================
