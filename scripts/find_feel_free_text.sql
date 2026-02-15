-- Phase 6A.112: Find all templates containing "feel free to reply" text variations
-- Run on both STAGING and PRODUCTION to identify impact

SELECT
    name AS template_name,
    CASE
        WHEN html_template ILIKE '%feel free to reply%' THEN 'YES'
        WHEN html_template ILIKE '%feel free to contact%' THEN 'YES'
        WHEN html_template ILIKE '%feel free to reach%' THEN 'YES'
        ELSE 'NO'
    END AS has_feel_free_text,
    CASE
        WHEN html_template ILIKE '%feel free to reply%' THEN 'reply'
        WHEN html_template ILIKE '%feel free to contact%' THEN 'contact'
        WHEN html_template ILIKE '%feel free to reach%' THEN 'reach'
        ELSE NULL
    END AS variation_type
FROM communications.email_templates
WHERE
    html_template ILIKE '%feel free%'
ORDER BY name;

-- Count by database
SELECT
    COUNT(*) as templates_with_feel_free_text
FROM communications.email_templates
WHERE html_template ILIKE '%feel free%';
