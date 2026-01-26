-- Email Template Parameter Validation Script
-- Phase 6A.83 - Verify handler fixes deployed correctly
-- Run against: lankaconnect-staging-db (Azure PostgreSQL)

-- ============================================
-- 1. CHECK FOR LITERAL {{}} IN RECENT EMAILS
-- ============================================

SELECT
    'BROKEN EMAILS' as check_type,
    COUNT(*) as total_broken,
    ARRAY_AGG(DISTINCT template_name) as affected_templates
FROM communications.email_messages
WHERE body_html LIKE '%{{%'
  AND created_at > NOW() - INTERVAL '24 hours'
HAVING COUNT(*) > 0;

-- ============================================
-- 2. DETAILED BROKEN EMAIL ANALYSIS
-- ============================================

SELECT
    template_name,
    recipient,
    subject,
    created_at,
    -- Extract literal parameters (basic regex)
    SUBSTRING(body_html FROM '\{\{[a-zA-Z]+\}\}') as first_literal_param,
    LENGTH(body_html) as html_length
FROM communications.email_messages
WHERE body_html LIKE '%{{%'
  AND created_at > NOW() - INTERVAL '24 hours'
ORDER BY created_at DESC
LIMIT 20;

-- ============================================
-- 3. VERIFY HIGH PRIORITY HANDLERS
-- ============================================

-- Fix #1: EventReminderJob - Check organizer and ticket parameters
SELECT
    'EventReminderJob' as handler,
    recipient,
    parameters::json->>'OrganizerContactName' as organizer_name,
    parameters::json->>'OrganizerContactEmail' as organizer_email,
    parameters::json->>'OrganizerContactPhone' as organizer_phone,
    parameters::json->>'TicketCode' as ticket_code,
    parameters::json->>'TicketExpiryDate' as ticket_expiry,
    CASE
        WHEN parameters::json->>'OrganizerContactName' IS NULL THEN 'MISSING OrganizerContactName'
        WHEN parameters::json->>'TicketCode' IS NULL AND body_html LIKE '%ticket%' THEN 'MISSING TicketCode'
        ELSE 'OK'
    END as status,
    created_at
FROM communications.email_messages
WHERE template_name = 'template-event-reminder'
  AND created_at > NOW() - INTERVAL '24 hours'
ORDER BY created_at DESC
LIMIT 10;

-- Fix #2: PaymentCompletedEventHandler - Check ticket, amount, organizer
SELECT
    'PaymentCompletedEventHandler' as handler,
    recipient,
    parameters::json->>'TicketCode' as ticket_code,
    parameters::json->>'AmountPaid' as amount_paid,
    parameters::json->>'TotalAmount' as total_amount,
    parameters::json->>'OrganizerContactName' as organizer_name,
    parameters::json->>'OrganizerContactEmail' as organizer_email,
    CASE
        WHEN parameters::json->>'TicketCode' IS NULL THEN 'MISSING TicketCode'
        WHEN parameters::json->>'AmountPaid' IS NULL THEN 'MISSING AmountPaid'
        WHEN parameters::json->>'OrganizerContactName' IS NULL THEN 'MISSING OrganizerContactName'
        ELSE 'OK'
    END as status,
    created_at
FROM communications.email_messages
WHERE template_name = 'template-paid-event-registration-confirmation-with-ticket'
  AND created_at > NOW() - INTERVAL '24 hours'
ORDER BY created_at DESC
LIMIT 10;

-- Fix #3: EventCancellationEmailJob - Check organizer contact
SELECT
    'EventCancellationEmailJob' as handler,
    recipient,
    parameters::json->>'OrganizerContactName' as organizer_name,
    parameters::json->>'OrganizerContactEmail' as organizer_email,
    parameters::json->>'OrganizerContactPhone' as organizer_phone,
    CASE
        WHEN parameters::json->>'OrganizerContactName' IS NULL THEN 'MISSING OrganizerContactName'
        WHEN parameters::json->>'OrganizerContactEmail' IS NULL THEN 'MISSING OrganizerContactEmail'
        ELSE 'OK'
    END as status,
    created_at
FROM communications.email_messages
WHERE template_name = 'template-event-cancellation-notifications'
  AND created_at > NOW() - INTERVAL '24 hours'
ORDER BY created_at DESC
LIMIT 10;

-- Fix #4: EventPublishedEventHandler - Check organizer contact
SELECT
    'EventPublishedEventHandler' as handler,
    recipient,
    parameters::json->>'OrganizerContactName' as organizer_name,
    parameters::json->>'OrganizerContactEmail' as organizer_email,
    parameters::json->>'OrganizerContactPhone' as organizer_phone,
    parameters::json->>'OrganizerName' as old_organizer_name, -- Should NOT be used
    CASE
        WHEN parameters::json->>'OrganizerContactName' IS NULL THEN 'MISSING OrganizerContactName'
        WHEN parameters::json->>'OrganizerName' IS NOT NULL THEN 'USING OLD PARAMETER (OrganizerName)'
        ELSE 'OK'
    END as status,
    created_at
FROM communications.email_messages
WHERE template_name = 'template-new-event-publication'
  AND created_at > NOW() - INTERVAL '24 hours'
ORDER BY created_at DESC
LIMIT 10;

-- Fix #5: EventNotificationEmailJob - Check organizer contact
SELECT
    'EventNotificationEmailJob' as handler,
    recipient,
    parameters::json->>'OrganizerContactName' as organizer_name,
    parameters::json->>'OrganizerContactEmail' as organizer_email,
    parameters::json->>'OrganizerContactPhone' as organizer_phone,
    CASE
        WHEN parameters::json->>'OrganizerContactName' IS NULL THEN 'MISSING OrganizerContactName'
        ELSE 'OK'
    END as status,
    created_at
FROM communications.email_messages
WHERE template_name = 'template-event-details-publication'
  AND created_at > NOW() - INTERVAL '24 hours'
ORDER BY created_at DESC
LIMIT 10;

-- ============================================
-- 4. VERIFY MEDIUM PRIORITY HANDLERS
-- ============================================

-- Fix #6: RegistrationConfirmedEventHandler - Check organizer contact
SELECT
    'RegistrationConfirmedEventHandler' as handler,
    recipient,
    parameters::json->>'OrganizerContactName' as organizer_name,
    parameters::json->>'OrganizerContactEmail' as organizer_email,
    CASE
        WHEN parameters::json->>'OrganizerContactName' IS NULL THEN 'MISSING OrganizerContactName'
        ELSE 'OK'
    END as status,
    created_at
FROM communications.email_messages
WHERE template_name = 'template-free-event-registration-confirmation'
  AND created_at > NOW() - INTERVAL '24 hours'
ORDER BY created_at DESC
LIMIT 10;

-- Fix #7: Signup List Handlers - Check ItemDescription and organizer
SELECT
    'SignupListHandlers' as handler,
    template_name,
    recipient,
    parameters::json->>'ItemName' as item_name,
    parameters::json->>'ItemDescription' as item_description,
    parameters::json->>'OrganizerContactName' as organizer_name,
    CASE
        WHEN parameters::json->>'ItemDescription' IS NULL THEN 'MISSING ItemDescription'
        WHEN parameters::json->>'OrganizerContactName' IS NULL THEN 'MISSING OrganizerContactName'
        ELSE 'OK'
    END as status,
    created_at
FROM communications.email_messages
WHERE template_name IN (
    'template-signup-list-commitment-confirmation',
    'template-signup-list-commitment-update',
    'template-signup-list-commitment-cancellation'
)
  AND created_at > NOW() - INTERVAL '24 hours'
ORDER BY created_at DESC
LIMIT 10;

-- Fix #9: SubscribeToNewsletterCommandHandler - Check UnsubscribeLink
SELECT
    'SubscribeToNewsletterCommandHandler' as handler,
    recipient,
    parameters::json->>'UnsubscribeUrl' as unsubscribe_url,
    parameters::json->>'UnsubscribeLink' as unsubscribe_link,
    CASE
        WHEN parameters::json->>'UnsubscribeLink' IS NULL THEN 'MISSING UnsubscribeLink'
        WHEN parameters::json->>'UnsubscribeUrl' IS NULL THEN 'MISSING UnsubscribeUrl'
        ELSE 'OK'
    END as status,
    created_at
FROM communications.email_messages
WHERE template_name = 'template-newsletter-subscription-confirmation'
  AND created_at > NOW() - INTERVAL '24 hours'
ORDER BY created_at DESC
LIMIT 10;

-- ============================================
-- 5. OVERALL TEMPLATE HEALTH CHECK
-- ============================================

SELECT
    template_name,
    COUNT(*) as total_sent,
    SUM(CASE WHEN body_html LIKE '%{{%' THEN 1 ELSE 0 END) as broken_count,
    ROUND(
        100.0 * SUM(CASE WHEN body_html LIKE '%{{%' THEN 1 ELSE 0 END) / COUNT(*),
        2
    ) as broken_percentage,
    MAX(created_at) as last_sent
FROM communications.email_messages
WHERE created_at > NOW() - INTERVAL '24 hours'
GROUP BY template_name
ORDER BY broken_percentage DESC, total_sent DESC;

-- ============================================
-- 6. PARAMETER COMPLETENESS BY TEMPLATE
-- ============================================

-- Check if all expected parameters are being sent
WITH template_requirements AS (
    SELECT 'template-event-reminder' as template_name,
           ARRAY['UserName', 'EventTitle', 'OrganizerContactName', 'OrganizerContactEmail', 'TicketCode'] as required_params
    UNION ALL
    SELECT 'template-paid-event-registration-confirmation-with-ticket',
           ARRAY['UserName', 'EventTitle', 'TicketCode', 'AmountPaid', 'OrganizerContactName']
    UNION ALL
    SELECT 'template-event-cancellation-notifications',
           ARRAY['UserName', 'EventTitle', 'OrganizerContactName', 'OrganizerContactEmail']
    UNION ALL
    SELECT 'template-new-event-publication',
           ARRAY['OrganizerContactName', 'OrganizerContactEmail', 'EventTitle']
    UNION ALL
    SELECT 'template-event-details-publication',
           ARRAY['OrganizerContactName', 'OrganizerContactEmail', 'EventTitle', 'UserName']
)
SELECT
    tr.template_name,
    em.recipient,
    tr.required_params,
    ARRAY(
        SELECT param
        FROM UNNEST(tr.required_params) param
        WHERE em.parameters::json->>param IS NULL
    ) as missing_params,
    CASE
        WHEN ARRAY_LENGTH(ARRAY(
            SELECT param
            FROM UNNEST(tr.required_params) param
            WHERE em.parameters::json->>param IS NULL
        ), 1) > 0 THEN 'INCOMPLETE'
        ELSE 'COMPLETE'
    END as status,
    em.created_at
FROM template_requirements tr
JOIN communications.email_messages em ON em.template_name = tr.template_name
WHERE em.created_at > NOW() - INTERVAL '24 hours'
ORDER BY em.created_at DESC
LIMIT 20;

-- ============================================
-- 7. EMAIL SEND SUCCESS RATE (Last 24 Hours)
-- ============================================

SELECT
    template_name,
    COUNT(*) as total_attempts,
    SUM(CASE WHEN status = 'sent' THEN 1 ELSE 0 END) as successful,
    SUM(CASE WHEN status = 'failed' THEN 1 ELSE 0 END) as failed,
    ROUND(100.0 * SUM(CASE WHEN status = 'sent' THEN 1 ELSE 0 END) / COUNT(*), 2) as success_rate
FROM communications.email_messages
WHERE created_at > NOW() - INTERVAL '24 hours'
GROUP BY template_name
ORDER BY success_rate ASC, total_attempts DESC;

-- ============================================
-- 8. RECENT FAILURES (Last 24 Hours)
-- ============================================

SELECT
    template_name,
    recipient,
    status,
    error_message,
    created_at
FROM communications.email_messages
WHERE status = 'failed'
  AND created_at > NOW() - INTERVAL '24 hours'
ORDER BY created_at DESC
LIMIT 20;

-- ============================================
-- 9. QUICK SMOKE TEST - Count Issues
-- ============================================

SELECT
    'Total Emails Last 24h' as metric,
    COUNT(*)::text as value
FROM communications.email_messages
WHERE created_at > NOW() - INTERVAL '24 hours'

UNION ALL

SELECT
    'Emails with Literal {{}}' as metric,
    COUNT(*)::text as value
FROM communications.email_messages
WHERE body_html LIKE '%{{%'
  AND created_at > NOW() - INTERVAL '24 hours'

UNION ALL

SELECT
    'Failed Email Sends' as metric,
    COUNT(*)::text as value
FROM communications.email_messages
WHERE status = 'failed'
  AND created_at > NOW() - INTERVAL '24 hours'

UNION ALL

SELECT
    'EventReminder Missing OrganizerContactName' as metric,
    COUNT(*)::text as value
FROM communications.email_messages
WHERE template_name = 'template-event-reminder'
  AND parameters::json->>'OrganizerContactName' IS NULL
  AND created_at > NOW() - INTERVAL '24 hours'

UNION ALL

SELECT
    'PaymentCompleted Missing TicketCode' as metric,
    COUNT(*)::text as value
FROM communications.email_messages
WHERE template_name = 'template-paid-event-registration-confirmation-with-ticket'
  AND parameters::json->>'TicketCode' IS NULL
  AND created_at > NOW() - INTERVAL '24 hours';

-- ============================================
-- 10. EXPORT SAMPLE BROKEN EMAIL FOR INSPECTION
-- ============================================

-- Run this separately to see actual email content
SELECT
    template_name,
    recipient,
    subject,
    body_html,
    parameters,
    created_at
FROM communications.email_messages
WHERE body_html LIKE '%{{%'
  AND created_at > NOW() - INTERVAL '24 hours'
ORDER BY created_at DESC
LIMIT 1;

-- ============================================
-- USAGE INSTRUCTIONS
-- ============================================

/*
1. Connect to Azure PostgreSQL staging database:
   psql "host=lankaconnect-staging-db.postgres.database.azure.com port=5432 dbname=lankaconnect user=admin_user password=XXX sslmode=require"

2. Run all sections (copy/paste into psql)

3. Interpret results:
   - Section 1: Should return 0 broken emails after fixes
   - Section 3-4: Status should be 'OK' for all recent emails
   - Section 5: Broken percentage should be 0% after fixes
   - Section 9: Quick summary of all issues

4. If issues found:
   - Check section 2 for specific broken emails
   - Check section 6 for missing parameters
   - Check section 8 for send failures

5. Run before deploying fix (baseline) and after deploying fix (verification)
*/
