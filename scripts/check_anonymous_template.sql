-- Phase 6A.80: Anonymous Email Template Verification Script
-- Purpose: Verify anonymous registration emails are using correct template and parameters
-- Author: Phase 6A.80 Implementation
-- Date: 2026-01-24

-- =====================================================
-- PART 1: Verify Template Configuration
-- =====================================================

-- Check if anonymous RSVP template was removed (should return 0 rows)
SELECT
    name,
    description,
    is_active,
    created_at
FROM communications.email_templates
WHERE name = 'template-anonymous-rsvp-confirmation';
-- Expected: 0 rows (template removed in Phase 6A.80)

-- Check if FreeEventRegistration template supports anonymous users (should return 1 row)
SELECT
    name,
    description,
    is_active,
    created_at,
    updated_at
FROM communications.email_templates
WHERE name = 'template-free-event-registration-confirmation';
-- Expected: 1 row with description containing "member and anonymous"

-- =====================================================
-- PART 2: Check Recent Anonymous Registration Emails
-- =====================================================

-- Find emails sent for anonymous registrations (last 24 hours)
SELECT
    em."Id" as email_id,
    em.to_emails,
    em.template_name,
    em.status,
    em."CreatedAt" as queued_at,
    em.sent_at,
    EXTRACT(EPOCH FROM (em.sent_at - em."CreatedAt")) as seconds_to_send,
    em.retry_count,
    em.error_message,
    -- Extract registration ID from template_data
    (em.template_data::json->>'RegistrationDate')::text as registration_date,
    (em.template_data::json->>'EventTitle')::text as event_title
FROM communications.email_messages em
WHERE em."CreatedAt" >= NOW() - INTERVAL '24 hours'
    AND em.template_name = 'template-free-event-registration-confirmation'
    AND em.template_data::text LIKE '%"ContactEmail"%'  -- Anonymous registrations have ContactEmail
ORDER BY em."CreatedAt" DESC;

-- =====================================================
-- PART 3: Verify Template Parameters
-- =====================================================

-- Check if emails have all required parameters (no missing/null values)
SELECT
    em."Id" as email_id,
    em.to_emails,
    em.status,
    -- Extract key parameters
    (em.template_data::json->>'UserName')::text as user_name,
    (em.template_data::json->>'EventTitle')::text as event_title,
    (em.template_data::json->>'EventDateTime')::text as event_datetime,
    (em.template_data::json->>'EventLocation')::text as event_location,
    (em.template_data::json->>'RegistrationDate')::text as registration_date,
    (em.template_data::json->>'HasAttendeeDetails')::text as has_attendee_details,
    (em.template_data::json->>'Attendees')::text as attendees_html,
    (em.template_data::json->>'ContactEmail')::text as contact_email,
    (em.template_data::json->>'EventImageUrl')::text as event_image_url,
    (em.template_data::json->>'HasEventImage')::text as has_event_image
FROM communications.email_messages em
WHERE em.template_name = 'template-free-event-registration-confirmation'
    AND em."CreatedAt" >= NOW() - INTERVAL '24 hours'
ORDER BY em."CreatedAt" DESC
LIMIT 5;

-- =====================================================
-- PART 4: Verify Email Content (No Literal Handlebars)
-- =====================================================

-- Check if any emails have literal Handlebars syntax in subject/body
-- This would indicate template rendering failure
SELECT
    em."Id" as email_id,
    em.to_emails,
    em.status,
    CASE
        WHEN em.subject LIKE '%{{%' THEN 'FAIL - Subject has literal Handlebars'
        ELSE 'OK - Subject rendered correctly'
    END as subject_check,
    CASE
        WHEN em.html_body LIKE '%{{UserName}}%' THEN 'FAIL - HTML has {{UserName}}'
        WHEN em.html_body LIKE '%{{EventTitle}}%' THEN 'FAIL - HTML has {{EventTitle}}'
        WHEN em.html_body LIKE '%{{EventDateTime}}%' THEN 'FAIL - HTML has {{EventDateTime}}'
        WHEN em.html_body LIKE '%{{%' THEN 'WARN - HTML has other Handlebars'
        ELSE 'OK - HTML rendered correctly'
    END as html_check,
    CASE
        WHEN em.text_body LIKE '%{{UserName}}%' THEN 'FAIL - Text has {{UserName}}'
        WHEN em.text_body LIKE '%{{EventTitle}}%' THEN 'FAIL - Text has {{EventTitle}}'
        WHEN em.text_body LIKE '%{{EventDateTime}}%' THEN 'FAIL - Text has {{EventDateTime}}'
        WHEN em.text_body LIKE '%{{%' THEN 'WARN - Text has other Handlebars'
        ELSE 'OK - Text rendered correctly'
    END as text_check
FROM communications.email_messages em
WHERE em.template_name = 'template-free-event-registration-confirmation'
    AND em."CreatedAt" >= NOW() - INTERVAL '24 hours'
ORDER BY em."CreatedAt" DESC
LIMIT 5;
-- Expected: All checks show "OK"

-- =====================================================
-- PART 5: Email Delivery Status Summary
-- =====================================================

-- Get summary of email statuses for anonymous registrations (last 7 days)
SELECT
    em.status,
    COUNT(*) as count,
    AVG(EXTRACT(EPOCH FROM (em.sent_at - em."CreatedAt"))) as avg_seconds_to_send,
    MAX(EXTRACT(EPOCH FROM (em.sent_at - em."CreatedAt"))) as max_seconds_to_send
FROM communications.email_messages em
WHERE em.template_name = 'template-free-event-registration-confirmation'
    AND em."CreatedAt" >= NOW() - INTERVAL '7 days'
    AND em.template_data::text LIKE '%"ContactEmail"%'
GROUP BY em.status
ORDER BY count DESC;

-- =====================================================
-- PART 6: Find Failed Emails (Troubleshooting)
-- =====================================================

-- Find any failed anonymous registration emails
SELECT
    em."Id" as email_id,
    em.to_emails,
    em."CreatedAt" as queued_at,
    em.retry_count,
    em.error_message,
    (em.template_data::json->>'EventTitle')::text as event_title,
    (em.template_data::json->>'ContactEmail')::text as contact_email
FROM communications.email_messages em
WHERE em.template_name = 'template-free-event-registration-confirmation'
    AND em.status = 'Failed'
    AND em."CreatedAt" >= NOW() - INTERVAL '7 days'
ORDER BY em."CreatedAt" DESC;

-- =====================================================
-- PART 7: Verify Specific Registration Email
-- =====================================================

-- Replace YOUR_EMAIL_HERE with the email address to search for
-- Replace YOUR_EVENT_ID_HERE with the event ID to search for (optional)
SELECT
    em."Id" as email_id,
    em.to_emails,
    em.template_name,
    em.status,
    em."CreatedAt" as queued_at,
    em.sent_at,
    EXTRACT(EPOCH FROM (em.sent_at - em."CreatedAt")) as seconds_to_send,
    em.subject,
    -- Extract key parameters
    (em.template_data::json->>'UserName')::text as user_name,
    (em.template_data::json->>'EventTitle')::text as event_title,
    (em.template_data::json->>'EventDateTime')::text as event_datetime,
    (em.template_data::json->>'RegistrationDate')::text as registration_date
FROM communications.email_messages em
WHERE em.to_emails::text LIKE '%YOUR_EMAIL_HERE%'
    AND em.template_name = 'template-free-event-registration-confirmation'
    -- Optional: Uncomment to filter by event
    -- AND em.template_data::text LIKE '%YOUR_EVENT_ID_HERE%'
ORDER BY em."CreatedAt" DESC
LIMIT 10;

-- =====================================================
-- EXPECTED RESULTS SUMMARY
-- =====================================================

-- Part 1: 0 rows (anonymous template removed)
-- Part 1: 1 row with updated description (FreeEventRegistration now supports anonymous)
-- Part 2: Recent emails using correct template
-- Part 3: All parameters present and non-null
-- Part 4: All checks show "OK" (no literal Handlebars)
-- Part 5: Most emails in "Sent" or "Delivered" status, avg send time 120-360 seconds
-- Part 6: Ideally 0 rows (no failed emails)
-- Part 7: Specific email found with all parameters rendered correctly

-- =====================================================
-- TROUBLESHOOTING
-- =====================================================

-- If Part 4 shows FAIL results:
-- 1. Check if EmailTemplateNames constants are being used in code
-- 2. Verify template name matches exactly: 'template-free-event-registration-confirmation'
-- 3. Check application logs for template rendering errors
-- 4. Verify Phase 6A.78 migration was applied (fixed template content)

-- If Part 5 shows high "Failed" count:
-- 1. Check error_message column in Part 6
-- 2. Verify Azure Email Service configuration
-- 3. Check domain authentication (SPF/DKIM/DMARC)
-- 4. Verify email addresses are valid

-- If Part 2 shows 0 rows for recent registrations:
-- 1. Check if anonymous registrations are being created in database
-- 2. Verify AnonymousRegistrationConfirmedEvent is being raised
-- 3. Check if AnonymousRegistrationConfirmedEventHandler is running
-- 4. Verify Hangfire background job is processing email queue
