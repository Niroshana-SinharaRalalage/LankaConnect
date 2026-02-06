-- ==========================================
-- Payment Flow Diagnosis SQL Queries
-- ==========================================
-- Run these queries against your PostgreSQL database to diagnose payment flow issues
-- Usage: psql -h <host> -U <user> -d <database> -f diagnose-payment-flow.sql

\echo '=========================================='
\echo 'STEP 1: Check Recent Registrations'
\echo '=========================================='

-- Find recent registrations with payment details
SELECT
    r.id AS registration_id,
    r.event_id,
    r.user_id,
    r.status,
    r.payment_status,
    r.stripe_checkout_session_id,
    r.stripe_payment_intent_id,
    r.quantity,
    r.total_price_amount,
    r.total_price_currency,
    r.created_at,
    r.updated_at
FROM events.registrations r
WHERE r.created_at > NOW() - INTERVAL '24 hours'
ORDER BY r.created_at DESC
LIMIT 20;

\echo ''
\echo '=========================================='
\echo 'STEP 2: Check Stripe Webhook Events'
\echo '=========================================='

-- Find recent webhook events
SELECT
    swe.id,
    swe.stripe_event_id,
    swe.event_type,
    swe.is_processed,
    swe.created_at,
    swe.processed_at
FROM stripe_webhook_events swe
WHERE swe.created_at > NOW() - INTERVAL '24 hours'
ORDER BY swe.created_at DESC
LIMIT 20;

\echo ''
\echo '=========================================='
\echo 'STEP 3: Check Tickets Generated'
\echo '=========================================='

-- Find recent tickets
SELECT
    t.id AS ticket_id,
    t.registration_id,
    t.event_id,
    t.ticket_code,
    t.status,
    t.created_at
FROM events.tickets t
WHERE t.created_at > NOW() - INTERVAL '24 hours'
ORDER BY t.created_at DESC
LIMIT 20;

\echo ''
\echo '=========================================='
\echo 'STEP 4: Check Email Messages Queued'
\echo '=========================================='

-- Find recent email messages
SELECT
    em.id AS email_id,
    em.to_email,
    em.to_name,
    em.subject,
    em.status,
    em.created_at,
    em.sent_at,
    em.failed_at,
    em.last_error_message
FROM communications.email_messages em
WHERE em.created_at > NOW() - INTERVAL '24 hours'
ORDER BY em.created_at DESC
LIMIT 20;

\echo ''
\echo '=========================================='
\echo 'STEP 5: Registrations Stuck in Pending'
\echo '=========================================='

-- Find registrations stuck in pending payment status
SELECT
    r.id AS registration_id,
    r.event_id,
    r.stripe_checkout_session_id,
    r.payment_status,
    r.status,
    r.created_at,
    EXTRACT(EPOCH FROM (NOW() - r.created_at))/60 AS minutes_ago
FROM events.registrations r
WHERE r.payment_status = 'Pending'
  AND r.created_at > NOW() - INTERVAL '24 hours'
ORDER BY r.created_at DESC;

\echo ''
\echo '=========================================='
\echo 'STEP 6: Complete Payment Flow Trace'
\echo '=========================================='

-- Join registrations, webhooks, tickets, and emails for complete picture
SELECT
    r.id AS registration_id,
    r.stripe_checkout_session_id,
    r.payment_status AS reg_payment_status,
    r.status AS reg_status,
    r.created_at AS reg_created,
    r.updated_at AS reg_updated,
    -- Webhook data
    swe.stripe_event_id AS webhook_event_id,
    swe.event_type AS webhook_type,
    swe.is_processed AS webhook_processed,
    swe.created_at AS webhook_received,
    swe.processed_at AS webhook_processed_at,
    -- Ticket data
    t.ticket_code,
    t.status AS ticket_status,
    t.created_at AS ticket_created,
    -- Email data
    em.to_email,
    em.status AS email_status,
    em.created_at AS email_created,
    em.sent_at AS email_sent,
    em.last_error_message AS email_error
FROM events.registrations r
LEFT JOIN stripe_webhook_events swe
    ON swe.stripe_event_id LIKE '%' || r.stripe_checkout_session_id || '%'
    OR swe.stripe_event_id LIKE '%' || r.stripe_payment_intent_id || '%'
LEFT JOIN events.tickets t
    ON t.registration_id = r.id
LEFT JOIN communications.email_messages em
    ON em.to_email LIKE '%' -- You might need to adjust this join based on your data
    AND em.created_at > r.updated_at
    AND em.created_at < r.updated_at + INTERVAL '5 minutes'
WHERE r.created_at > NOW() - INTERVAL '24 hours'
  AND r.payment_status IN ('Pending', 'Completed')
ORDER BY r.created_at DESC
LIMIT 10;

\echo ''
\echo '=========================================='
\echo 'STEP 7: Check for Orphaned Webhooks'
\echo '=========================================='

-- Find webhook events that didn't match any registration
SELECT
    swe.stripe_event_id,
    swe.event_type,
    swe.is_processed,
    swe.created_at,
    COUNT(r.id) AS matching_registrations
FROM stripe_webhook_events swe
LEFT JOIN events.registrations r
    ON r.stripe_checkout_session_id IS NOT NULL
    AND swe.stripe_event_id LIKE '%' || r.stripe_checkout_session_id || '%'
WHERE swe.event_type = 'checkout.session.completed'
  AND swe.created_at > NOW() - INTERVAL '24 hours'
GROUP BY swe.stripe_event_id, swe.event_type, swe.is_processed, swe.created_at
HAVING COUNT(r.id) = 0
ORDER BY swe.created_at DESC;

\echo ''
\echo '=========================================='
\echo 'Diagnosis Complete'
\echo '=========================================='
