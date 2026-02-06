-- Phase 6A.83 Part 3: Extract ALL template parameters from database
-- This query extracts EVERY Handlebars parameter from all 19 templates

WITH template_parameters AS (
    SELECT
        template_name,
        html_body,
        text_body,
        -- Extract all {{ParameterName}} from HTML body
        regexp_matches(html_body, '\{\{([A-Za-z0-9_]+)\}\}', 'g') as html_params,
        -- Extract all {{ParameterName}} from text body
        regexp_matches(text_body, '\{\{([A-Za-z0-9_]+)\}\}', 'g') as text_params
    FROM communications.email_templates
    WHERE is_active = true
)
SELECT
    template_name,
    -- Combine HTML and text parameters, deduplicate
    array_agg(DISTINCT param ORDER BY param) as all_parameters
FROM (
    SELECT template_name, html_params[1] as param FROM template_parameters WHERE html_params IS NOT NULL
    UNION
    SELECT template_name, text_params[1] as param FROM template_parameters WHERE text_params IS NOT NULL
) combined
GROUP BY template_name
ORDER BY template_name;

-- Part 2: Get full HTML content for manual inspection
SELECT
    template_name,
    html_body,
    created_at
FROM communications.email_templates
WHERE is_active = true
ORDER BY template_name;
