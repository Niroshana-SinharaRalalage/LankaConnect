-- Fix Duplicate CTA Buttons in Email Templates
-- Run this SQL directly on the staging database

-- ============================================================
-- 1. template-new-event-publication: REMOVE "View Event Details" button
--    KEEP "View Event & Register" as this email invites people to register
-- ============================================================
UPDATE communications.email_templates
SET html_template = REGEXP_REPLACE(
    html_template,
    '<!-- View Event Details CTA Button -->[\s\S]*?</table>\s*',
    '',
    'g'
),
updated_at = NOW()
WHERE name = 'template-new-event-publication'
  AND html_template LIKE '%<!-- View Event Details CTA Button -->%';

-- ============================================================
-- 2. template-event-details-publication: REMOVE "View Event & Register" button
--    KEEP "View Event Details" as this is a manual notification
-- ============================================================
UPDATE communications.email_templates
SET html_template = REGEXP_REPLACE(
    html_template,
    '<table[^>]*role="presentation"[^>]*>[\s\S]*?View Event &amp; Register[\s\S]*?</table>\s*',
    '',
    'g'
),
updated_at = NOW()
WHERE name = 'template-event-details-publication'
  AND (html_template LIKE '%View Event &amp; Register%'
       OR html_template LIKE '%View Event & Register%');

-- ============================================================
-- 3. template-signup-list-commitment-confirmation: REMOVE "View Event & Register"
--    KEEP "View Event Details" - user already committed
-- ============================================================
UPDATE communications.email_templates
SET html_template = REGEXP_REPLACE(
    html_template,
    '<table[^>]*role="presentation"[^>]*>[\s\S]*?View Event &amp; Register[\s\S]*?</table>\s*',
    '',
    'g'
),
updated_at = NOW()
WHERE name = 'template-signup-list-commitment-confirmation'
  AND (html_template LIKE '%View Event &amp; Register%'
       OR html_template LIKE '%View Event & Register%');

-- ============================================================
-- 4. template-signup-list-commitment-update: REMOVE "View Event & Register"
--    KEEP "View Event Details" - user already committed
-- ============================================================
UPDATE communications.email_templates
SET html_template = REGEXP_REPLACE(
    html_template,
    '<table[^>]*role="presentation"[^>]*>[\s\S]*?View Event &amp; Register[\s\S]*?</table>\s*',
    '',
    'g'
),
updated_at = NOW()
WHERE name = 'template-signup-list-commitment-update'
  AND (html_template LIKE '%View Event &amp; Register%'
       OR html_template LIKE '%View Event & Register%');
