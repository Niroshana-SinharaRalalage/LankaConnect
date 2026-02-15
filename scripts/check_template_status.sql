-- Check Email Template Status in Staging Database
-- Query all templates to see which are active/inactive

-- All templates overview
SELECT
    name,
    is_active,
    created_at,
    updated_at,
    LENGTH(html_template) as html_length,
    LENGTH(subject_template) as subject_length,
    category
FROM communications.email_templates
ORDER BY is_active DESC, name;

-- Specifically check signup list templates
SELECT
    name,
    is_active,
    created_at,
    updated_at
FROM communications.email_templates
WHERE name LIKE '%signup-list-commitment%'
ORDER BY name;

-- Summary count by status
SELECT
    is_active,
    COUNT(*) as template_count
FROM communications.email_templates
GROUP BY is_active
ORDER BY is_active DESC;

-- List inactive templates
SELECT
    name,
    category,
    updated_at
FROM communications.email_templates
WHERE is_active = false
ORDER BY category, name;
