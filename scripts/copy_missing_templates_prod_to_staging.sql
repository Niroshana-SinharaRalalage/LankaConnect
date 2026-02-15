-- Phase 6A.112: Copy 8 missing templates from PRODUCTION to STAGING
-- INSTRUCTIONS:
-- 1. Run this query on PRODUCTION database to export missing templates
-- 2. Then insert into STAGING database

-- Step 1: Run on PRODUCTION to get templates
SELECT
    name,
    subject_template,
    html_template,
    text_template,
    is_active,
    NOW() as created_at,
    NOW() as updated_at
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

-- Step 2: After exporting, use this to insert into STAGING
-- (You'll need to replace {{exported_data}} with actual values from step 1)
/*
INSERT INTO communications.email_templates (name, subject_template, html_template, text_template, is_active, created_at, updated_at)
VALUES
    ('template-free-event-registration', '...', '...', '...', true, NOW(), NOW()),
    -- ... repeat for all 8 templates
ON CONFLICT (name) DO UPDATE SET
    subject_template = EXCLUDED.subject_template,
    html_template = EXCLUDED.html_template,
    text_template = EXCLUDED.text_template,
    updated_at = NOW();
*/
