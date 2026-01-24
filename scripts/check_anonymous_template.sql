-- Check the anonymous RSVP confirmation template
SELECT
    name,
    subject,
    LEFT(html_template, 500) as html_preview,
    LEFT(text_template, 500) as text_preview,
    is_active,
    updated_at
FROM communications.email_templates
WHERE name = 'template-anonymous-rsvp-confirmation';

-- Also check what parameters are expected
SELECT
    name,
    text_template
FROM communications.email_templates
WHERE name = 'template-anonymous-rsvp-confirmation';
