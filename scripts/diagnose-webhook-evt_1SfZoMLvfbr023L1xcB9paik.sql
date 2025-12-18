-- ============================================================================
-- Phase 6A.24 Webhook Diagnostic Script
-- Purpose: Diagnose webhook processing state for evt_1SfZoMLvfbr023L1xcB9paik
-- Date: 2025-12-18
-- ============================================================================

-- ============================================================================
-- QUERY 1: Webhook Event Processing Status
-- ============================================================================
-- Purpose: Check if webhook was received and marked as processed
-- Expected: 1 row with processed = true, processed_at around 5:31 AM UTC
-- ============================================================================

SELECT
    'WEBHOOK EVENT STATUS' as check_type,
    event_id,
    event_type,
    processed,
    processed_at,
    created_at,
    CASE
        WHEN processed = true THEN '✅ Processed'
        ELSE '❌ Not Processed'
    END as status,
    EXTRACT(EPOCH FROM (processed_at - created_at)) as processing_duration_seconds
FROM payments.stripe_webhook_events
WHERE event_id = 'evt_1SfZoMLvfbr023L1xcB9paik';

-- ============================================================================
-- QUERY 2: Find Associated Registration
-- ============================================================================
-- Purpose: Find the registration linked to this webhook's checkout session
-- Expected: 1 registration with status = Confirmed, payment_status = Completed
-- ============================================================================

SELECT
    'REGISTRATION STATUS' as check_type,
    r.id as registration_id,
    r.event_id,
    r.user_id,
    r.status,
    r.payment_status,
    r.stripe_checkout_session_id,
    r.stripe_payment_intent_id,
    r.total_price,
    r.created_at,
    r.updated_at,
    CASE
        WHEN r.status = 'Confirmed' AND r.payment_status = 'Completed'
        THEN '✅ Payment Complete'
        ELSE '❌ Payment Incomplete'
    END as payment_check
FROM events.registrations r
WHERE r.stripe_checkout_session_id = 'cs_test_a1GJDLaIOJeGp1IUiA54Yv2uXmBZrou3bor1iANb25zissulk'
   OR r.stripe_payment_intent_id IN (
       SELECT data->>'payment_intent'
       FROM payments.stripe_webhook_events
       WHERE event_id = 'evt_1SfZoMLvfbr023L1xcB9paik'
   )
ORDER BY r.created_at DESC
LIMIT 5;

-- ============================================================================
-- QUERY 3: Check Ticket Generation
-- ============================================================================
-- Purpose: Verify if ticket was generated for this registration
-- Expected: 1 ticket with status = Active OR 0 tickets (if handler failed)
-- ============================================================================

SELECT
    'TICKET STATUS' as check_type,
    t.id as ticket_id,
    t.registration_id,
    t.event_id,
    t.ticket_code,
    t.status,
    t.qr_code_data,
    t.created_at,
    CASE
        WHEN t.id IS NOT NULL THEN '✅ Ticket Generated'
        ELSE '❌ No Ticket Found'
    END as ticket_check
FROM events.registrations r
LEFT JOIN tickets.tickets t ON t.registration_id = r.id
WHERE r.stripe_checkout_session_id = 'cs_test_a1GJDLaIOJeGp1IUiA54Yv2uXmBZrou3bor1iANb25zissulk'
   OR r.stripe_payment_intent_id IN (
       SELECT data->>'payment_intent'
       FROM payments.stripe_webhook_events
       WHERE event_id = 'evt_1SfZoMLvfbr023L1xcB9paik'
   );

-- ============================================================================
-- QUERY 4: Check Email Queue Status
-- ============================================================================
-- Purpose: Verify if confirmation email was queued/sent
-- Expected: 1 email with status = Sent OR Failed (if email service failed)
-- ============================================================================

SELECT
    'EMAIL STATUS' as check_type,
    em.id as email_id,
    em.to_emails,
    em.subject,
    em.status,
    em.template_name,
    em.created_at,
    em.sent_at,
    em.failed_at,
    em.error_message,
    CASE
        WHEN em.status = 'Sent' THEN '✅ Email Sent'
        WHEN em.status = 'Failed' THEN '❌ Email Failed'
        WHEN em.status = 'Queued' THEN '⏳ Email Queued'
        ELSE '❌ No Email Found'
    END as email_check
FROM events.registrations r
LEFT JOIN communications.email_messages em ON em.to_emails::jsonb @> to_jsonb(ARRAY[r.contact_email])
WHERE r.stripe_checkout_session_id = 'cs_test_a1GJDLaIOJeGp1IUiA54Yv2uXmBZrou3bor1iANb25zissulk'
   OR r.stripe_payment_intent_id IN (
       SELECT data->>'payment_intent'
       FROM payments.stripe_webhook_events
       WHERE event_id = 'evt_1SfZoMLvfbr023L1xcB9paik'
   )
   AND em.created_at > '2025-12-18 05:00:00'::timestamptz
   AND (em.template_name LIKE '%ticket%' OR em.subject LIKE '%ticket%')
ORDER BY em.created_at DESC
LIMIT 5;

-- ============================================================================
-- QUERY 5: Complete Payment Flow Trace
-- ============================================================================
-- Purpose: Get a timeline of all events in the payment flow
-- Expected: Chronological list showing webhook → registration → ticket → email
-- ============================================================================

WITH webhook_data AS (
    SELECT
        event_id,
        created_at as webhook_received_at,
        processed_at as webhook_processed_at,
        data->>'payment_intent' as payment_intent_id,
        (data->'data'->'object'->>'id')::text as checkout_session_id
    FROM payments.stripe_webhook_events
    WHERE event_id = 'evt_1SfZoMLvfbr023L1xcB9paik'
),
registration_data AS (
    SELECT
        r.id as registration_id,
        r.event_id,
        r.user_id,
        r.contact_email,
        r.status,
        r.payment_status,
        r.created_at as registration_created_at,
        r.updated_at as registration_updated_at
    FROM events.registrations r, webhook_data w
    WHERE r.stripe_checkout_session_id = w.checkout_session_id
       OR r.stripe_payment_intent_id = w.payment_intent_id
)
SELECT
    '1. WEBHOOK' as step,
    w.event_id as identifier,
    'evt_1SfZoMLvfbr023L1xcB9paik' as event_id,
    w.webhook_received_at as timestamp,
    'Webhook received' as action,
    NULL::text as status_detail
FROM webhook_data w

UNION ALL

SELECT
    '2. REGISTRATION' as step,
    r.registration_id::text as identifier,
    r.event_id::text,
    r.registration_updated_at as timestamp,
    'Payment completed' as action,
    CONCAT('Status: ', r.status, ', Payment: ', r.payment_status) as status_detail
FROM registration_data r

UNION ALL

SELECT
    '3. TICKET' as step,
    t.ticket_code as identifier,
    t.event_id::text,
    t.created_at as timestamp,
    'Ticket generated' as action,
    CONCAT('Status: ', t.status) as status_detail
FROM registration_data r
LEFT JOIN tickets.tickets t ON t.registration_id = r.registration_id

UNION ALL

SELECT
    '4. EMAIL' as step,
    em.id::text as identifier,
    r.event_id::text,
    em.created_at as timestamp,
    'Email queued' as action,
    CONCAT('Status: ', em.status, COALESCE(', Error: ' || em.error_message, '')) as status_detail
FROM registration_data r
LEFT JOIN communications.email_messages em ON em.to_emails::jsonb @> to_jsonb(ARRAY[r.contact_email])
WHERE em.created_at > '2025-12-18 05:00:00'::timestamptz

ORDER BY timestamp;

-- ============================================================================
-- QUERY 6: Check Contact Information
-- ============================================================================
-- Purpose: Get registration contact details for recovery endpoint
-- Expected: Contact email and user details needed for manual retry
-- ============================================================================

SELECT
    'CONTACT INFORMATION' as check_type,
    r.id as registration_id,
    r.event_id,
    r.user_id,
    r.contact_email,
    r.contact_first_name,
    r.contact_last_name,
    r.contact_phone,
    CASE
        WHEN r.contact_email IS NOT NULL THEN '✅ Email Available'
        ELSE '❌ Email Missing'
    END as email_check
FROM events.registrations r
WHERE r.stripe_checkout_session_id = 'cs_test_a1GJDLaIOJeGp1IUiA54Yv2uXmBZrou3bor1iANb25zissulk'
   OR r.stripe_payment_intent_id IN (
       SELECT data->>'payment_intent'
       FROM payments.stripe_webhook_events
       WHERE event_id = 'evt_1SfZoMLvfbr023L1xcB9paik'
   );

-- ============================================================================
-- DIAGNOSTIC SUMMARY
-- ============================================================================
-- Purpose: Single-row summary of diagnostic results
-- Expected: Shows which steps succeeded/failed
-- ============================================================================

WITH diagnostics AS (
    SELECT
        EXISTS(
            SELECT 1 FROM payments.stripe_webhook_events
            WHERE event_id = 'evt_1SfZoMLvfbr023L1xcB9paik' AND processed = true
        ) as webhook_processed,
        EXISTS(
            SELECT 1 FROM events.registrations r
            WHERE (r.stripe_checkout_session_id = 'cs_test_a1GJDLaIOJeGp1IUiA54Yv2uXmBZrou3bor1iANb25zissulk'
                OR r.stripe_payment_intent_id IN (
                    SELECT data->>'payment_intent'
                    FROM payments.stripe_webhook_events
                    WHERE event_id = 'evt_1SfZoMLvfbr023L1xcB9paik'
                ))
                AND r.status = 'Confirmed'
                AND r.payment_status = 'Completed'
        ) as registration_completed,
        EXISTS(
            SELECT 1 FROM tickets.tickets t
            JOIN events.registrations r ON r.id = t.registration_id
            WHERE r.stripe_checkout_session_id = 'cs_test_a1GJDLaIOJeGp1IUiA54Yv2uXmBZrou3bor1iANb25zissulk'
        ) as ticket_generated,
        EXISTS(
            SELECT 1 FROM communications.email_messages em
            JOIN events.registrations r ON em.to_emails::jsonb @> to_jsonb(ARRAY[r.contact_email])
            WHERE r.stripe_checkout_session_id = 'cs_test_a1GJDLaIOJeGp1IUiA54Yv2uXmBZrou3bor1iANb25zissulk'
                AND em.created_at > '2025-12-18 05:00:00'::timestamptz
                AND em.status IN ('Sent', 'Queued', 'Sending')
        ) as email_sent
)
SELECT
    'DIAGNOSTIC SUMMARY' as summary_type,
    CASE WHEN webhook_processed THEN '✅' ELSE '❌' END || ' Webhook' as step_1_webhook,
    CASE WHEN registration_completed THEN '✅' ELSE '❌' END || ' Registration' as step_2_registration,
    CASE WHEN ticket_generated THEN '✅' ELSE '❌' END || ' Ticket' as step_3_ticket,
    CASE WHEN email_sent THEN '✅' ELSE '❌' END || ' Email' as step_4_email,
    CASE
        WHEN webhook_processed AND registration_completed AND ticket_generated AND email_sent
        THEN '✅ COMPLETE - All steps succeeded'
        WHEN webhook_processed AND registration_completed AND NOT ticket_generated
        THEN '⚠️ PARTIAL - Handler failed at ticket generation'
        WHEN webhook_processed AND registration_completed AND ticket_generated AND NOT email_sent
        THEN '⚠️ PARTIAL - Handler failed at email sending'
        WHEN webhook_processed AND NOT registration_completed
        THEN '❌ FAILED - Payment not completed'
        ELSE '❌ UNKNOWN - Unexpected state'
    END as overall_status,
    CASE
        WHEN NOT ticket_generated OR NOT email_sent
        THEN 'RECOVERY NEEDED - Use AdminRecoveryController'
        ELSE 'NO ACTION NEEDED'
    END as recommended_action
FROM diagnostics;

-- ============================================================================
-- INSTRUCTIONS FOR USE
-- ============================================================================
/*
1. Run this entire script in Azure Data Studio or pgAdmin
2. Review each query result set
3. Check the DIAGNOSTIC SUMMARY (last query) for overall status
4. Share all results with the development team

INTERPRETATION:
- ✅ COMPLETE: No action needed, everything worked
- ⚠️ PARTIAL: Recovery endpoint needed (ticket or email failed)
- ❌ FAILED: Investigate payment processing issue

NEXT STEPS BASED ON RESULTS:
- If ticket_generated = false: Handler failed, use recovery endpoint
- If email_sent = false: Email service issue, use recovery endpoint
- If registration_completed = false: Payment issue, investigate webhook handler
*/
