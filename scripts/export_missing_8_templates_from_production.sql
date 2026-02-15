-- Phase 6A.112: Export 8 missing templates from PRODUCTION database
-- Run this on PRODUCTION, then save results to import into staging

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
    'template-event-cancellation',
    'template-registration-cancellation',
    'template-signup-commitment-confirmation',
    'template-newsletter'
)
ORDER BY name;

-- Export as JSON, then you can import into staging
