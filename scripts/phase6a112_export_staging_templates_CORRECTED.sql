-- Phase 6A.112 CORRECTED: Export email templates from staging to production
-- Root Cause Fix: Use ACTUAL template names from EmailParams classes (not EmailTemplateContract constants)
--
-- Date: 2026-02-14
-- Purpose: Export email templates that exist in staging but missing in production
--
-- Verification:
-- User confirmed staging has 32 templates. This script exports templates
-- using the ACTUAL database names from EmailParams.TemplateName properties.

-- ========================================
-- STEP 1: Verify templates exist in staging
-- ========================================

-- Run this query in STAGING database to verify all templates exist
SELECT
    name AS template_name,
    template_type,
    subject,
    CASE
        WHEN html_body IS NOT NULL THEN 'HTML: ' || LENGTH(html_body) || ' chars'
        ELSE 'No HTML body'
    END AS html_status,
    CASE
        WHEN text_body IS NOT NULL THEN 'Text: ' || LENGTH(text_body) || ' chars'
        ELSE 'No text body'
    END AS text_status,
    description
FROM communications.email_templates
WHERE name IN (
    -- REGISTRATION TEMPLATES
    'template-free-event-registration-confirmation',
    'template-paid-event-registration-confirmation-with-ticket',
    'template-event-registration-cancellation',
    'template-preliminary-registration-payment-pending',

    -- EVENT MANAGEMENT TEMPLATES
    'template-event-reminder',
    'template-event-cancellation-notifications',
    'template-attendees-added-confirmation',
    'template-new-event-publication',
    'template-event-details-publication',
    'template-event-approval',

    -- REFUND TEMPLATES
    'template-refund-requested',
    'template-refund-completed',

    -- SIGNUP COMMITMENT TEMPLATES
    'template-signup-list-commitment-confirmation',
    'template-signup-list-commitment-update',
    'template-signup-list-commitment-cancellation',

    -- NEWSLETTER TEMPLATES
    'template-newsletter-notification',
    'template-newsletter-subscription-confirmation',

    -- FORM RESPONSE TEMPLATES (Phase 6A.107)
    'template-form-response-confirmation',
    'template-form-response-update',
    'template-form-response-cancellation',

    -- ADMIN/USER MANAGEMENT TEMPLATES
    'template-password-reset',
    'template-password-change-confirmation',
    'template-welcome',
    'template-account-activated-by-admin',
    'template-account-deactivated-by-admin',
    'template-account-locked-by-admin',
    'template-account-unlocked-by-admin',
    'template-organizer-role-approval',

    -- SUPPORT TEMPLATES
    'template-support-ticket-confirmation',
    'template-support-ticket-reply',

    -- CUSTOM TEMPLATES
    'OrganizerCustomEmail'
)
ORDER BY
    CASE
        WHEN name LIKE 'template-free-event%' THEN 1
        WHEN name LIKE 'template-paid-event%' THEN 2
        WHEN name LIKE 'template-event-r%' THEN 3
        WHEN name LIKE 'template-event-c%' THEN 4
        WHEN name LIKE 'template-event-a%' THEN 5
        WHEN name LIKE 'template-refund%' THEN 6
        WHEN name LIKE 'template-signup%' THEN 7
        WHEN name LIKE 'template-newsletter%' THEN 8
        WHEN name LIKE 'template-form%' THEN 9
        WHEN name LIKE 'template-password%' THEN 10
        WHEN name LIKE 'template-account%' THEN 11
        WHEN name LIKE 'template-support%' THEN 12
        ELSE 99
    END,
    name;

-- Expected result: 32 rows (all templates in staging)

-- ========================================
-- STEP 2: Check which templates are missing in production
-- ========================================

-- Run this query in PRODUCTION database to see what's missing
SELECT
    t.template_name
FROM (
    VALUES
        -- REGISTRATION TEMPLATES
        ('template-free-event-registration-confirmation'),
        ('template-paid-event-registration-confirmation-with-ticket'),
        ('template-event-registration-cancellation'),
        ('template-preliminary-registration-payment-pending'),

        -- EVENT MANAGEMENT TEMPLATES
        ('template-event-reminder'),
        ('template-event-cancellation-notifications'),
        ('template-attendees-added-confirmation'),
        ('template-new-event-publication'),
        ('template-event-details-publication'),
        ('template-event-approval'),

        -- REFUND TEMPLATES
        ('template-refund-requested'),
        ('template-refund-completed'),

        -- SIGNUP COMMITMENT TEMPLATES
        ('template-signup-list-commitment-confirmation'),
        ('template-signup-list-commitment-update'),
        ('template-signup-list-commitment-cancellation'),

        -- NEWSLETTER TEMPLATES
        ('template-newsletter-notification'),
        ('template-newsletter-subscription-confirmation'),

        -- FORM RESPONSE TEMPLATES
        ('template-form-response-confirmation'),
        ('template-form-response-update'),
        ('template-form-response-cancellation'),

        -- ADMIN/USER MANAGEMENT TEMPLATES
        ('template-password-reset'),
        ('template-password-change-confirmation'),
        ('template-welcome'),
        ('template-account-activated-by-admin'),
        ('template-account-deactivated-by-admin'),
        ('template-account-locked-by-admin'),
        ('template-account-unlocked-by-admin'),
        ('template-organizer-role-approval'),

        -- SUPPORT TEMPLATES
        ('template-support-ticket-confirmation'),
        ('template-support-ticket-reply'),

        -- CUSTOM TEMPLATES
        ('OrganizerCustomEmail')
) AS t(template_name)
LEFT JOIN communications.email_templates et ON et.name = t.template_name
WHERE et.name IS NULL
ORDER BY t.template_name;

-- This will show which templates need to be exported from staging

-- ========================================
-- STEP 3: Export templates as JSON from staging
-- ========================================

-- Run this query in STAGING database and save the output
SELECT json_agg(
    json_build_object(
        'name', name,
        'template_type', template_type,
        'subject', subject,
        'html_body', html_body,
        'text_body', text_body,
        'description', description
    ) ORDER BY name
) AS templates_json
FROM communications.email_templates
WHERE name IN (
    -- REGISTRATION TEMPLATES
    'template-free-event-registration-confirmation',
    'template-paid-event-registration-confirmation-with-ticket',
    'template-event-registration-cancellation',
    'template-preliminary-registration-payment-pending',

    -- EVENT MANAGEMENT TEMPLATES
    'template-event-reminder',
    'template-event-cancellation-notifications',
    'template-attendees-added-confirmation',
    'template-new-event-publication',
    'template-event-details-publication',
    'template-event-approval',

    -- REFUND TEMPLATES
    'template-refund-requested',
    'template-refund-completed',

    -- SIGNUP COMMITMENT TEMPLATES
    'template-signup-list-commitment-confirmation',
    'template-signup-list-commitment-update',
    'template-signup-list-commitment-cancellation',

    -- NEWSLETTER TEMPLATES
    'template-newsletter-notification',
    'template-newsletter-subscription-confirmation',

    -- FORM RESPONSE TEMPLATES
    'template-form-response-confirmation',
    'template-form-response-update',
    'template-form-response-cancellation',

    -- ADMIN/USER MANAGEMENT TEMPLATES
    'template-password-reset',
    'template-password-change-confirmation',
    'template-welcome',
    'template-account-activated-by-admin',
    'template-account-deactivated-by-admin',
    'template-account-locked-by-admin',
    'template-account-unlocked-by-admin',
    'template-organizer-role-approval',

    -- SUPPORT TEMPLATES
    'template-support-ticket-confirmation',
    'template-support-ticket-reply',

    -- CUSTOM TEMPLATES
    'OrganizerCustomEmail'
);

-- ========================================
-- STEP 4: Generate INSERT statements for production
-- ========================================

-- Run this query in STAGING database to generate INSERT statements
-- Copy the output and run in PRODUCTION database

SELECT
    'INSERT INTO communications.email_templates (name, template_type, subject, html_body, text_body, description, created_at, updated_at)' || E'\n' ||
    'VALUES' || E'\n' ||
    string_agg(
        format(
            '(%L, %L, %L, %L, %L, %L, NOW(), NOW())',
            name,
            template_type,
            subject,
            html_body,
            text_body,
            description
        ),
        ',' || E'\n'
    ) || E'\n' ||
    'ON CONFLICT (name) DO UPDATE SET' || E'\n' ||
    '    template_type = EXCLUDED.template_type,' || E'\n' ||
    '    subject = EXCLUDED.subject,' || E'\n' ||
    '    html_body = EXCLUDED.html_body,' || E'\n' ||
    '    text_body = EXCLUDED.text_body,' || E'\n' ||
    '    description = EXCLUDED.description,' || E'\n' ||
    '    updated_at = NOW();' AS insert_statements
FROM communications.email_templates
WHERE name IN (
    -- REGISTRATION TEMPLATES
    'template-free-event-registration-confirmation',
    'template-paid-event-registration-confirmation-with-ticket',
    'template-event-registration-cancellation',
    'template-preliminary-registration-payment-pending',

    -- EVENT MANAGEMENT TEMPLATES
    'template-event-reminder',
    'template-event-cancellation-notifications',
    'template-attendees-added-confirmation',
    'template-new-event-publication',
    'template-event-details-publication',
    'template-event-approval',

    -- REFUND TEMPLATES
    'template-refund-requested',
    'template-refund-completed',

    -- SIGNUP COMMITMENT TEMPLATES
    'template-signup-list-commitment-confirmation',
    'template-signup-list-commitment-update',
    'template-signup-list-commitment-cancellation',

    -- NEWSLETTER TEMPLATES
    'template-newsletter-notification',
    'template-newsletter-subscription-confirmation',

    -- FORM RESPONSE TEMPLATES
    'template-form-response-confirmation',
    'template-form-response-update',
    'template-form-response-cancellation',

    -- ADMIN/USER MANAGEMENT TEMPLATES
    'template-password-reset',
    'template-password-change-confirmation',
    'template-welcome',
    'template-account-activated-by-admin',
    'template-account-deactivated-by-admin',
    'template-account-locked-by-admin',
    'template-account-unlocked-by-admin',
    'template-organizer-role-approval',

    -- SUPPORT TEMPLATES
    'template-support-ticket-confirmation',
    'template-support-ticket-reply',

    -- CUSTOM TEMPLATES
    'OrganizerCustomEmail'
);

-- ========================================
-- STEP 5: Verification after import
-- ========================================

-- Run this query in PRODUCTION database after importing templates
SELECT
    COUNT(*) AS total_templates,
    COUNT(CASE WHEN name LIKE 'template-free-event%' THEN 1 END) AS registration_templates,
    COUNT(CASE WHEN name LIKE 'template-event%' THEN 1 END) AS event_templates,
    COUNT(CASE WHEN name LIKE 'template-refund%' THEN 1 END) AS refund_templates,
    COUNT(CASE WHEN name LIKE 'template-signup%' THEN 1 END) AS signup_templates,
    COUNT(CASE WHEN name LIKE 'template-newsletter%' THEN 1 END) AS newsletter_templates,
    COUNT(CASE WHEN name LIKE 'template-form%' THEN 1 END) AS form_templates,
    COUNT(CASE WHEN name LIKE 'template-password%' OR name LIKE 'template-account%' THEN 1 END) AS admin_templates,
    COUNT(CASE WHEN name LIKE 'template-support%' THEN 1 END) AS support_templates
FROM communications.email_templates;

-- Expected result: 32 total templates (or more if production has additional templates)

-- ========================================
-- COMPLETE TEMPLATE NAME REFERENCE
-- ========================================

/*
REGISTRATION TEMPLATES (4):
- template-free-event-registration-confirmation (FreeEventRegistrationEmailParams.cs)
- template-paid-event-registration-confirmation-with-ticket (TicketConfirmationEmailParams.cs)
- template-event-registration-cancellation (RegistrationCancellationEmailParams.cs)
- template-preliminary-registration-payment-pending (PreliminaryRegistrationPaymentEmailParams.cs)

EVENT MANAGEMENT TEMPLATES (6):
- template-event-reminder (EventReminderEmailParams.cs)
- template-event-cancellation-notifications (EventCancellationEmailParams.cs)
- template-attendees-added-confirmation (AttendeesAddedEmailParams.cs)
- template-new-event-publication (EventPublishedEmailParams.cs)
- template-event-details-publication (EventDetailsEmailParams.cs)
- template-event-approval (EventApprovalEmailParams.cs)

REFUND TEMPLATES (2):
- template-refund-requested (RefundEmailParams.cs - AsRequest)
- template-refund-completed (RefundEmailParams.cs - AsCompleted)

SIGNUP COMMITMENT TEMPLATES (3):
- template-signup-list-commitment-confirmation (SignupCommitmentEmailParams.cs - AsConfirmation)
- template-signup-list-commitment-update (SignupCommitmentEmailParams.cs - AsUpdate)
- template-signup-list-commitment-cancellation (SignupCommitmentEmailParams.cs - AsCancellation)

NEWSLETTER TEMPLATES (2):
- template-newsletter-notification (NewsletterEmailParams.cs)
- template-newsletter-subscription-confirmation (NewsletterSubscriptionEmailParams.cs)

FORM RESPONSE TEMPLATES (3):
- template-form-response-confirmation (FormResponseEmailParams.cs - AsConfirmation)
- template-form-response-update (FormResponseEmailParams.cs - AsUpdate)
- template-form-response-cancellation (FormResponseEmailParams.cs - AsCancellation)

ADMIN/USER MANAGEMENT TEMPLATES (8):
- template-password-reset (PasswordResetEmailParams.cs)
- template-password-change-confirmation (PasswordChangedEmailParams.cs)
- template-welcome (WelcomeEmailParams.cs)
- template-account-activated-by-admin (AccountActivatedEmailParams.cs)
- template-account-deactivated-by-admin (AccountDeactivatedEmailParams.cs)
- template-account-locked-by-admin (AdminUserEmailParams.cs)
- template-account-unlocked-by-admin (AdminUserEmailParams.cs)
- template-organizer-role-approval (OrganizerRoleApprovalEmailParams.cs)

SUPPORT TEMPLATES (2):
- template-support-ticket-confirmation (SupportTicketEmailParams.cs)
- template-support-ticket-reply (SupportTicketReplyEmailParams.cs)

CUSTOM TEMPLATES (1):
- OrganizerCustomEmail (Custom organizer email template)

TOTAL: 32 templates (verified in staging database)
*/
