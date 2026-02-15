-- Phase 6A.113: Export ALL 8 Templates for Comprehensive "Feel Free" Text Removal
-- Purpose: Export all templates containing "feel free" for comprehensive cleanup
-- Usage: Run against staging database, modify HTML, then import to production

-- Expected Output: 8 templates
SELECT
    name,
    subject,
    html_content
FROM communications.email_templates
WHERE name IN (
    'template-account-activated-by-admin',
    'template-account-unlocked-by-admin',
    'template-free-event-registration-confirmation',
    'template-membership-email-verification',
    'template-paid-event-registration-confirmation-with-ticket',
    'template-password-change-confirmation',
    'template-password-reset',
    'template-signup-list-commitment-confirmation'
)
ORDER BY name;

-- Modification Instructions:
-- 1. Replace "feel free to reach out" → "please reach out"
-- 2. Replace "feel free to contact" → "please contact"
-- 3. Replace "Feel free to" → "Please"
-- 4. Keep tone professional and welcoming
-- 5. Maintain existing structure and formatting
