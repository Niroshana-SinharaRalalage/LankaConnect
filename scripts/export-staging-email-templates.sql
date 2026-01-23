-- Export all email templates from staging database
-- This helps identify which templates were manually modified

-- Show all templates with metadata
SELECT
    name,
    type,
    category,
    is_active,
    created_at,
    updated_at,
    CASE WHEN updated_at IS NOT NULL THEN 'MANUALLY UPDATED' ELSE 'Original' END as status,
    LENGTH(subject_template) as subject_length,
    LENGTH(html_template) as html_length,
    LENGTH(text_template) as text_length
FROM communications.email_templates
ORDER BY updated_at DESC NULLS LAST, name;

-- Show recently modified templates (likely manual changes)
\echo '\n=== RECENTLY MODIFIED TEMPLATES (Likely Manual Changes) ==='
SELECT
    name,
    updated_at,
    LENGTH(html_template) as html_length
FROM communications.email_templates
WHERE updated_at IS NOT NULL
  AND updated_at > created_at
ORDER BY updated_at DESC;

-- Show templates that might have been manually edited
\echo '\n=== TEMPLATES WITH CUSTOM HTML (Potentially Modified) ==='
SELECT
    name,
    CASE
        WHEN html_template LIKE '%<!-- Custom%' THEN 'Has custom HTML comment'
        WHEN html_template LIKE '%style=%' THEN 'Has inline styles'
        WHEN LENGTH(html_template) > 5000 THEN 'Large HTML (likely customized)'
        ELSE 'Standard'
    END as html_status,
    LENGTH(html_template) as html_length
FROM communications.email_templates
ORDER BY html_length DESC;

-- Export full content of all templates for backup
\echo '\n=== FULL TEMPLATE EXPORT (for creating migration) ==='
\echo 'Copy this output to create your migration SQL...\n'

SELECT
    '-- Template: ' || name || E'\n' ||
    'UPDATE communications.email_templates SET' || E'\n' ||
    '  subject_template = ' || quote_literal(subject_template) || ',' || E'\n' ||
    '  html_template = ' || quote_literal(html_template) || ',' || E'\n' ||
    '  text_template = ' || quote_literal(text_template) || ',' || E'\n' ||
    '  updated_at = NOW()' || E'\n' ||
    'WHERE name = ' || quote_literal(name) || ';' || E'\n'
FROM communications.email_templates
ORDER BY name;
