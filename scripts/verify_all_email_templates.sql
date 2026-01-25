-- Comprehensive Email Template Parameter Verification Script
-- Purpose: Check ALL 19 templates for literal Handlebars parameters
-- Author: Phase 6A.82 Extended Verification
-- Date: 2026-01-24

-- =====================================================
-- PART 1: List All Templates with Parameter Extraction
-- =====================================================

SELECT
    name,
    -- Extract all unique Handlebars parameters from HTML template
    (
        SELECT string_agg(DISTINCT matches[1], ', ' ORDER BY matches[1])
        FROM regexp_matches(html_template, '\{\{#?/?([A-Za-z]+)\}\}', 'g') AS matches
        WHERE matches[1] NOT IN ('if', 'unless', 'each', 'with')  -- Exclude Handlebars helpers
    ) as html_parameters,
    -- Extract all unique Handlebars parameters from text template
    (
        SELECT string_agg(DISTINCT matches[1], ', ' ORDER BY matches[1])
        FROM regexp_matches(text_template, '\{\{#?/?([A-Za-z]+)\}\}', 'g') AS matches
        WHERE matches[1] NOT IN ('if', 'unless', 'each', 'with')  -- Exclude Handlebars helpers
    ) as text_parameters,
    is_active,
    updated_at
FROM communications.email_templates
ORDER BY name;

-- =====================================================
-- PART 2: Check Recent Emails for Literal Handlebars
-- =====================================================

-- Find ANY emails sent in last 7 days with literal Handlebars in rendered content
SELECT
    em."Id" as email_id,
    em.template_name,
    em.to_emails,
    em.status,
    em."CreatedAt" as queued_at,
    em.sent_at,
    -- Check subject for literals
    CASE
        WHEN em.subject LIKE '%{{%' THEN 'FAIL - Subject has literal Handlebars'
        ELSE 'OK'
    END as subject_check,
    -- Extract literal parameters from HTML body
    (
        SELECT string_agg(DISTINCT matches[1], ', ')
        FROM regexp_matches(em.html_body, '\{\{([A-Za-z]+)\}\}', 'g') AS matches
        LIMIT 10
    ) as html_literal_params,
    -- Extract literal parameters from text body
    (
        SELECT string_agg(DISTINCT matches[1], ', ')
        FROM regexp_matches(em.text_body, '\{\{([A-Za-z]+)\}\}', 'g') AS matches
        LIMIT 10
    ) as text_literal_params
FROM communications.email_messages em
WHERE em."CreatedAt" >= NOW() - INTERVAL '7 days'
    AND (
        em.html_body LIKE '%{{%'
        OR em.text_body LIKE '%{{%'
        OR em.subject LIKE '%{{%'
    )
ORDER BY em."CreatedAt" DESC;

-- =====================================================
-- PART 3: Template-by-Template Parameter Analysis
-- =====================================================

-- Template 1: template-free-event-registration-confirmation
SELECT
    'template-free-event-registration-confirmation' as template_name,
    'Handler: RegistrationConfirmedEventHandler, AnonymousRegistrationConfirmedEventHandler' as used_by,
    (
        SELECT string_agg(DISTINCT matches[1], ', ' ORDER BY matches[1])
        FROM regexp_matches(html_template, '\{\{#?/?([A-Za-z]+)\}\}', 'g') AS matches
        WHERE matches[1] NOT IN ('if', 'unless', 'each', 'with')
    ) as expected_parameters
FROM communications.email_templates
WHERE name = 'template-free-event-registration-confirmation';

-- Template 2: template-paid-event-registration-confirmation-with-ticket
SELECT
    'template-paid-event-registration-confirmation-with-ticket' as template_name,
    'Handler: PaymentCompletedEventHandler, ResendTicketEmailCommandHandler' as used_by,
    (
        SELECT string_agg(DISTINCT matches[1], ', ' ORDER BY matches[1])
        FROM regexp_matches(html_template, '\{\{#?/?([A-Za-z]+)\}\}', 'g') AS matches
        WHERE matches[1] NOT IN ('if', 'unless', 'each', 'with')
    ) as expected_parameters
FROM communications.email_templates
WHERE name = 'template-paid-event-registration-confirmation-with-ticket';

-- Template 3: template-event-reminder
SELECT
    'template-event-reminder' as template_name,
    'Job: EventReminderJob' as used_by,
    (
        SELECT string_agg(DISTINCT matches[1], ', ' ORDER BY matches[1])
        FROM regexp_matches(html_template, '\{\{#?/?([A-Za-z]+)\}\}', 'g') AS matches
        WHERE matches[1] NOT IN ('if', 'unless', 'each', 'with')
    ) as expected_parameters
FROM communications.email_templates
WHERE name = 'template-event-reminder';

-- Template 4: template-membership-email-verification
SELECT
    'template-membership-email-verification' as template_name,
    'Handler: MemberVerificationRequestedEventHandler' as used_by,
    (
        SELECT string_agg(DISTINCT matches[1], ', ' ORDER BY matches[1])
        FROM regexp_matches(html_template, '\{\{#?/?([A-Za-z]+)\}\}', 'g') AS matches
        WHERE matches[1] NOT IN ('if', 'unless', 'each', 'with')
    ) as expected_parameters
FROM communications.email_templates
WHERE name = 'template-membership-email-verification';

-- Template 5: template-signup-list-commitment-confirmation
SELECT
    'template-signup-list-commitment-confirmation' as template_name,
    'Handler: UserCommittedToSignUpEventHandler' as used_by,
    (
        SELECT string_agg(DISTINCT matches[1], ', ' ORDER BY matches[1])
        FROM regexp_matches(html_template, '\{\{#?/?([A-Za-z]+)\}\}', 'g') AS matches
        WHERE matches[1] NOT IN ('if', 'unless', 'each', 'with')
    ) as expected_parameters
FROM communications.email_templates
WHERE name = 'template-signup-list-commitment-confirmation';

-- Template 6: template-signup-list-commitment-update
SELECT
    'template-signup-list-commitment-update' as template_name,
    'Handler: CommitmentUpdatedEventHandler' as used_by,
    (
        SELECT string_agg(DISTINCT matches[1], ', ' ORDER BY matches[1])
        FROM regexp_matches(html_template, '\{\{#?/?([A-Za-z]+)\}\}', 'g') AS matches
        WHERE matches[1] NOT IN ('if', 'unless', 'each', 'with')
    ) as expected_parameters
FROM communications.email_templates
WHERE name = 'template-signup-list-commitment-update';

-- Template 7: template-signup-list-commitment-cancellation
SELECT
    'template-signup-list-commitment-cancellation' as template_name,
    'Handler: CommitmentCancelledEmailHandler' as used_by,
    (
        SELECT string_agg(DISTINCT matches[1], ', ' ORDER BY matches[1])
        FROM regexp_matches(html_template, '\{\{#?/?([A-Za-z]+)\}\}', 'g') AS matches
        WHERE matches[1] NOT IN ('if', 'unless', 'each', 'with')
    ) as expected_parameters
FROM communications.email_templates
WHERE name = 'template-signup-list-commitment-cancellation';

-- Template 8: template-event-registration-cancellation
SELECT
    'template-event-registration-cancellation' as template_name,
    'Handler: RegistrationCancelledEventHandler' as used_by,
    (
        SELECT string_agg(DISTINCT matches[1], ', ' ORDER BY matches[1])
        FROM regexp_matches(html_template, '\{\{#?/?([A-Za-z]+)\}\}', 'g') AS matches
        WHERE matches[1] NOT IN ('if', 'unless', 'each', 'with')
    ) as expected_parameters
FROM communications.email_templates
WHERE name = 'template-event-registration-cancellation';

-- Template 9: template-new-event-publication
SELECT
    'template-new-event-publication' as template_name,
    'Handler: EventPublishedEventHandler (FIXED in Phase 6A.82)' as used_by,
    (
        SELECT string_agg(DISTINCT matches[1], ', ' ORDER BY matches[1])
        FROM regexp_matches(html_template, '\{\{#?/?([A-Za-z]+)\}\}', 'g') AS matches
        WHERE matches[1] NOT IN ('if', 'unless', 'each', 'with')
    ) as expected_parameters
FROM communications.email_templates
WHERE name = 'template-new-event-publication';

-- Template 10: template-event-details-publication
SELECT
    'template-event-details-publication' as template_name,
    'Job: EventNotificationEmailJob (manual Send Notification button)' as used_by,
    (
        SELECT string_agg(DISTINCT matches[1], ', ' ORDER BY matches[1])
        FROM regexp_matches(html_template, '\{\{#?/?([A-Za-z]+)\}\}', 'g') AS matches
        WHERE matches[1] NOT IN ('if', 'unless', 'each', 'with')
    ) as expected_parameters
FROM communications.email_templates
WHERE name = 'template-event-details-publication';

-- Template 11: template-event-cancellation-notifications
SELECT
    'template-event-cancellation-notifications' as template_name,
    'Job: EventCancellationEmailJob' as used_by,
    (
        SELECT string_agg(DISTINCT matches[1], ', ' ORDER BY matches[1])
        FROM regexp_matches(html_template, '\{\{#?/?([A-Za-z]+)\}\}', 'g') AS matches
        WHERE matches[1] NOT IN ('if', 'unless', 'each', 'with')
    ) as expected_parameters
FROM communications.email_templates
WHERE name = 'template-event-cancellation-notifications';

-- Template 12: template-event-approval
SELECT
    'template-event-approval' as template_name,
    'Handler: EventApprovedEventHandler' as used_by,
    (
        SELECT string_agg(DISTINCT matches[1], ', ' ORDER BY matches[1])
        FROM regexp_matches(html_template, '\{\{#?/?([A-Za-z]+)\}\}', 'g') AS matches
        WHERE matches[1] NOT IN ('if', 'unless', 'each', 'with')
    ) as expected_parameters
FROM communications.email_templates
WHERE name = 'template-event-approval';

-- Template 13: template-newsletter-notification
SELECT
    'template-newsletter-notification' as template_name,
    'Job: NewsletterEmailJob' as used_by,
    (
        SELECT string_agg(DISTINCT matches[1], ', ' ORDER BY matches[1])
        FROM regexp_matches(html_template, '\{\{#?/?([A-Za-z]+)\}\}', 'g') AS matches
        WHERE matches[1] NOT IN ('if', 'unless', 'each', 'with')
    ) as expected_parameters
FROM communications.email_templates
WHERE name = 'template-newsletter-notification';

-- Template 14: template-newsletter-subscription-confirmation
SELECT
    'template-newsletter-subscription-confirmation' as template_name,
    'Handler: SubscribeToNewsletterCommandHandler' as used_by,
    (
        SELECT string_agg(DISTINCT matches[1], ', ' ORDER BY matches[1])
        FROM regexp_matches(html_template, '\{\{#?/?([A-Za-z]+)\}\}', 'g') AS matches
        WHERE matches[1] NOT IN ('if', 'unless', 'each', 'with')
    ) as expected_parameters
FROM communications.email_templates
WHERE name = 'template-newsletter-subscription-confirmation';

-- Template 15: template-password-reset
SELECT
    'template-password-reset' as template_name,
    'Handler: PasswordResetRequestedEventHandler' as used_by,
    (
        SELECT string_agg(DISTINCT matches[1], ', ' ORDER BY matches[1])
        FROM regexp_matches(html_template, '\{\{#?/?([A-Za-z]+)\}\}', 'g') AS matches
        WHERE matches[1] NOT IN ('if', 'unless', 'each', 'with')
    ) as expected_parameters
FROM communications.email_templates
WHERE name = 'template-password-reset';

-- Template 16: template-password-change-confirmation
SELECT
    'template-password-change-confirmation' as template_name,
    'Handler: PasswordChangedEventHandler' as used_by,
    (
        SELECT string_agg(DISTINCT matches[1], ', ' ORDER BY matches[1])
        FROM regexp_matches(html_template, '\{\{#?/?([A-Za-z]+)\}\}', 'g') AS matches
        WHERE matches[1] NOT IN ('if', 'unless', 'each', 'with')
    ) as expected_parameters
FROM communications.email_templates
WHERE name = 'template-password-change-confirmation';

-- Template 17: template-welcome
SELECT
    'template-welcome' as template_name,
    'Handler: UserRegisteredEventHandler (if exists)' as used_by,
    (
        SELECT string_agg(DISTINCT matches[1], ', ' ORDER BY matches[1])
        FROM regexp_matches(html_template, '\{\{#?/?([A-Za-z]+)\}\}', 'g') AS matches
        WHERE matches[1] NOT IN ('if', 'unless', 'each', 'with')
    ) as expected_parameters
FROM communications.email_templates
WHERE name = 'template-welcome';

-- Template 18: template-organizer-role-approval
SELECT
    'template-organizer-role-approval' as template_name,
    'Handler: OrganizerRoleApprovedEventHandler' as used_by,
    (
        SELECT string_agg(DISTINCT matches[1], ', ' ORDER BY matches[1])
        FROM regexp_matches(html_template, '\{\{#?/?([A-Za-z]+)\}\}', 'g') AS matches
        WHERE matches[1] NOT IN ('if', 'unless', 'each', 'with')
    ) as expected_parameters
FROM communications.email_templates
WHERE name = 'template-organizer-role-approval';

-- Template 19: OrganizerCustomEmail
SELECT
    'OrganizerCustomEmail' as template_name,
    'Custom organizer emails (if implemented)' as used_by,
    (
        SELECT string_agg(DISTINCT matches[1], ', ' ORDER BY matches[1])
        FROM regexp_matches(html_template, '\{\{#?/?([A-Za-z]+)\}\}', 'g') AS matches
        WHERE matches[1] NOT IN ('if', 'unless', 'each', 'with')
    ) as expected_parameters
FROM communications.email_templates
WHERE name = 'OrganizerCustomEmail';

-- =====================================================
-- PART 4: Summary - Templates by Status
-- =====================================================

SELECT
    COUNT(*) as total_templates,
    COUNT(CASE WHEN is_active = true THEN 1 END) as active_templates,
    COUNT(CASE WHEN is_active = false THEN 1 END) as inactive_templates
FROM communications.email_templates;

-- =====================================================
-- INSTRUCTIONS
-- =====================================================

-- Run this script and review:
-- 1. PART 1: All templates with their expected parameters
-- 2. PART 2: Any recent emails showing literal Handlebars (CRITICAL!)
-- 3. PART 3: Template-by-template breakdown with handler info
-- 4. PART 4: Template count summary

-- For each template in PART 3:
-- - Note the "expected_parameters" column
-- - Find the handler in codebase (listed in "used_by" column)
-- - Verify handler sends ALL expected parameters
-- - If mismatch found, add to fix list
