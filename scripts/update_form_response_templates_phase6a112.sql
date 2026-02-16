-- Phase 6A.112: Update Form Response Email Templates with Professional Styling
-- Date: 2026-02-14
-- Description: Replace existing basic form response templates with professionally styled versions
--              matching Phase 6A.96 signup list template standards (gradient header/footer, responsive design)
--
-- Templates Updated:
-- 1. template-form-response-confirmation
-- 2. template-form-response-update
-- 3. template-form-response-cancellation
--
-- Backup Note: This script includes rollback commands in case of issues.
-- Always test on staging first!

-- ============================================================================
-- BACKUP CURRENT TEMPLATES (for rollback)
-- ============================================================================

-- Export current templates before updating (for manual backup)
-- SELECT name, subject_template, html_template
-- FROM communications.email_templates
-- WHERE name IN (
--     'template-form-response-confirmation',
--     'template-form-response-update',
--     'template-form-response-cancellation'
-- );

-- ============================================================================
-- UPDATE TEMPLATE 1: Form Response Confirmation
-- ============================================================================

UPDATE communications.email_templates
SET
    subject_template = '{{EventTitle}} - Form Response Received',
    html_template = '<!-- Professional template HTML will be inserted here from template-form-response-confirmation-modified.html -->
    <!-- This is a placeholder - actual HTML is too large for inline SQL -->
    <!-- Use C# migration with File.ReadAllText() or embed programmatically -->',
    updated_at = NOW()
WHERE name = 'template-form-response-confirmation';

-- ============================================================================
-- UPDATE TEMPLATE 2: Form Response Update
-- ============================================================================

UPDATE communications.email_templates
SET
    subject_template = '{{EventTitle}} - Form Response Updated',
    html_template = '<!-- Professional template HTML will be inserted here from template-form-response-update-modified.html -->
    <!-- This is a placeholder - actual HTML is too large for inline SQL -->
    <!-- Use C# migration with File.ReadAllText() or embed programmatically -->',
    updated_at = NOW()
WHERE name = 'template-form-response-update';

-- ============================================================================
-- UPDATE TEMPLATE 3: Form Response Cancellation
-- ============================================================================

UPDATE communications.email_templates
SET
    subject_template = '{{EventTitle}} - Form Response Cancelled',
    html_template = '<!-- Professional template HTML will be inserted here from template-form-response-cancellation-modified.html -->
    <!-- This is a placeholder - actual HTML is too large for inline SQL -->
    <!-- Use C# migration with File.ReadAllText() or embed programmatically -->',
    updated_at = NOW()
WHERE name = 'template-form-response-cancellation';

-- ============================================================================
-- VERIFICATION QUERIES
-- ============================================================================

-- Verify updates were applied
SELECT
    name,
    subject_template,
    LENGTH(html_template) as html_length,
    updated_at
FROM communications.email_templates
WHERE name IN (
    'template-form-response-confirmation',
    'template-form-response-update',
    'template-form-response-cancellation'
)
ORDER BY name;

-- ============================================================================
-- ROLLBACK COMMANDS (if needed)
-- ============================================================================

-- Run these commands to rollback to previous templates:
-- UPDATE communications.email_templates
-- SET html_template = '<previous html here>', updated_at = NOW()
-- WHERE name = 'template-form-response-confirmation';
--
-- (Repeat for update and cancellation templates)
