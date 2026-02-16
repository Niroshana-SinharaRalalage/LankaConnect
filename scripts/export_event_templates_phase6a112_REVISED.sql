-- Phase 6A.112: REVISED - Export templates that ACTUALLY EXIST in staging
-- Based on query results showing only 3 templates exist in staging

-- STAGING DATABASE - Only 3 templates exist
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
    'template-event-reminder',
    'template-refund-completed',
    'template-refund-requested'
)
ORDER BY name;

-- NOTE: The following 8 templates do NOT exist in staging:
-- - template-free-event-registration
-- - template-ticket-confirmation
-- - template-attendees-added
-- - template-event-published
-- - template-event-cancellation
-- - template-registration-cancellation
-- - template-signup-commitment-confirmation
-- - template-newsletter

-- You need to run copy_missing_templates_prod_to_staging.sql first to get these 8
