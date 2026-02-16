-- Phase 6A.112: Export 11 event email templates from staging database
-- Run this script on the STAGING database to download templates for modification
-- Output: JSON file with all templates for easy modification

-- List of 11 event email templates to export:
-- 1. template-free-event-registration (FreeEventRegistrationEmailParams)
-- 2. template-ticket-confirmation (TicketConfirmationEmailParams)
-- 3. template-attendees-added (AttendeesAddedEmailParams)
-- 4. template-event-published (EventPublishedEmailParams)
-- 5. template-event-reminder (EventReminderEmailParams)
-- 6. template-event-cancellation (EventCancellationEmailParams)
-- 7. template-refund-completed (RefundEmailParams - completed)
-- 8. template-refund-requested (RefundEmailParams - requested)
-- 9. template-registration-cancellation (RegistrationCancellationEmailParams)
-- 10. template-signup-commitment-confirmation (SignupCommitmentEmailParams)
-- 11. template-newsletter (NewsletterEmailParams)

SELECT
    name AS template_name,
    subject_template,
    html_template,
    text_template,
    is_active,
    created_at,
    updated_at
FROM communications.email_templates
WHERE name IN (
    'template-free-event-registration',
    'template-ticket-confirmation',
    'template-attendees-added',
    'template-event-published',
    'template-event-reminder',
    'template-event-cancellation',
    'template-refund-completed',
    'template-refund-requested',
    'template-registration-cancellation',
    'template-signup-commitment-confirmation',
    'template-newsletter'
)
ORDER BY name;

-- INSTRUCTIONS FOR USER:
-- 1. Connect to staging database using Azure Data Studio or psql
-- 2. Run this query
-- 3. Export results to JSON format
-- 4. Save each template's html_template to separate files:
--    - Template_Correction/event_templates/template-free-event-registration.html
--    - Template_Correction/event_templates/template-ticket-confirmation.html
--    - ... (repeat for all 11)
-- 5. Then modify each HTML file:
--    a. Add "View Signup Forms" button (copy from cancellation template)
--    b. Remove "feel free to reply" text variations
-- 6. Update Phase6A112 migration to use these modified templates
