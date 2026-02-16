-- Phase 6A.113: Export 3 Event Templates for "Feel Free" Text Removal
-- Purpose: Export templates needing "feel free" text cleanup (event-related only)
-- Usage: Run against staging database, modify HTML, then import to production

-- Expected Output: 3 templates
SELECT
    name,
    subject,
    html_content
FROM communications.email_templates
WHERE name IN (
    'template-free-event-registration-confirmation',
    'template-paid-event-registration-confirmation-with-ticket',
    'template-signup-list-commitment-confirmation'
)
ORDER BY name;

-- Modification Instructions:
-- 1. Replace "feel free to reach out" → "please reach out"
-- 2. Replace "feel free to contact" → "please contact"
-- 3. Replace "Feel free to" → "Please"
-- 4. Keep tone professional and welcoming
