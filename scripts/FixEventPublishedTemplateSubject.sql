-- Phase 6A.41 Hotfix: Fix event-published template missing subject
-- Root Cause: Migration inserted template but subject_template column is NULL
-- This causes EmailSubject value object creation to fail when sending emails

UPDATE communications.email_templates
SET subject_template = 'New Event: {{EventTitle}} in {{EventCity}}, {{EventState}}',
    updated_at = NOW()
WHERE name = 'event-published'
  AND (subject_template IS NULL OR subject_template = '');

-- Verify the fix
SELECT 
    name,
    subject_template,
    is_active,
    LENGTH(subject_template) as subject_len
FROM communications.email_templates
WHERE name = 'event-published';
