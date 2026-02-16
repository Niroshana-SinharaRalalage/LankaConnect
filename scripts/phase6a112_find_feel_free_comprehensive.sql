-- Phase 6A.112: Comprehensive "feel free" text analysis
-- Find ALL templates containing "feel free" variations
-- Run on BOTH staging and production to identify complete scope

-- ========================================
-- STEP 1: Find templates with "feel free" text
-- ========================================

SELECT
    name AS template_name,
    CASE
        WHEN html_template ILIKE '%feel free to reply%' THEN 'reply'
        WHEN html_template ILIKE '%feel free to contact%' THEN 'contact'
        WHEN html_template ILIKE '%feel free to reach%' THEN 'reach'
        WHEN html_template ILIKE '%feel free to%' THEN 'other'
        ELSE 'none'
    END AS variation_type,
    -- Extract the actual text for review
    substring(
        html_template,
        POSITION('feel free' IN LOWER(html_template)) - 50,
        150
    ) AS surrounding_text,
    LENGTH(html_template) AS template_size,
    updated_at
FROM communications.email_templates
WHERE LOWER(html_template) LIKE '%feel free%'
ORDER BY name;

-- ========================================
-- STEP 2: Count by variation
-- ========================================

SELECT
    CASE
        WHEN html_template ILIKE '%feel free to reply%' THEN 'feel free to reply'
        WHEN html_template ILIKE '%feel free to contact%' THEN 'feel free to contact'
        WHEN html_template ILIKE '%feel free to reach%' THEN 'feel free to reach'
        ELSE 'other feel free variations'
    END AS text_variation,
    COUNT(*) AS template_count,
    array_agg(name ORDER BY name) AS affected_templates
FROM communications.email_templates
WHERE LOWER(html_template) LIKE '%feel free%'
GROUP BY
    CASE
        WHEN html_template ILIKE '%feel free to reply%' THEN 'feel free to reply'
        WHEN html_template ILIKE '%feel free to contact%' THEN 'feel free to contact'
        WHEN html_template ILIKE '%feel free to reach%' THEN 'feel free to reach'
        ELSE 'other feel free variations'
    END
ORDER BY template_count DESC;

-- ========================================
-- STEP 3: Check templates in Phase 6A.112 scope
-- ========================================

-- Check which Phase 6A.112 templates contain "feel free" text
SELECT
    t.template_category,
    t.template_name,
    CASE
        WHEN et.html_template ILIKE '%feel free%' THEN 'YES - NEEDS CLEANUP'
        ELSE 'NO - Already clean'
    END AS needs_cleanup,
    CASE
        WHEN et.name IS NOT NULL THEN 'EXISTS'
        ELSE 'MISSING'
    END AS db_status
FROM (
    VALUES
        -- Event templates (Phase 6A.112 scope)
        ('event', 'template-free-event-registration-confirmation'),
        ('event', 'template-paid-event-registration-confirmation-with-ticket'),
        ('event', 'template-attendees-added-confirmation'),
        ('event', 'template-new-event-publication'),
        ('event', 'template-event-reminder'),
        ('event', 'template-event-cancellation-notifications'),
        ('event', 'template-refund-completed'),
        ('event', 'template-refund-requested'),
        ('event', 'template-event-registration-cancellation'),
        ('event', 'template-signup-list-commitment-confirmation'),
        ('event', 'template-newsletter-notification'),

        -- Form response templates (Phase 6A.112 scope)
        ('form', 'template-form-response-confirmation'),
        ('form', 'template-form-response-update'),
        ('form', 'template-form-response-cancellation')
) AS t(template_category, template_name)
LEFT JOIN communications.email_templates et ON et.name = t.template_name
ORDER BY t.template_category, t.template_name;

-- ========================================
-- STEP 4: Summary statistics
-- ========================================

SELECT
    COUNT(*) AS total_templates_in_db,
    COUNT(CASE WHEN LOWER(html_template) LIKE '%feel free%' THEN 1 END) AS templates_with_feel_free,
    COUNT(CASE WHEN LOWER(html_template) LIKE '%feel free%' THEN 1 END)::FLOAT /
        NULLIF(COUNT(*), 0) * 100 AS percentage_with_feel_free
FROM communications.email_templates;

-- ========================================
-- STEP 5: Generate list for Phase 6A.112
-- ========================================

-- This query shows EXACTLY which templates need modification in Phase 6A.112
WITH phase6a112_templates AS (
    SELECT unnest(ARRAY[
        'template-free-event-registration-confirmation',
        'template-paid-event-registration-confirmation-with-ticket',
        'template-attendees-added-confirmation',
        'template-new-event-publication',
        'template-event-reminder',
        'template-event-cancellation-notifications',
        'template-refund-completed',
        'template-refund-requested',
        'template-event-registration-cancellation',
        'template-signup-list-commitment-confirmation',
        'template-newsletter-notification',
        'template-form-response-confirmation',
        'template-form-response-update',
        'template-form-response-cancellation'
    ]) AS template_name
)
SELECT
    p.template_name,
    CASE WHEN et.name IS NOT NULL THEN 'EXISTS' ELSE 'MISSING' END AS db_status,
    CASE WHEN et.html_template ILIKE '%feel free%' THEN 'CLEANUP REQUIRED' ELSE 'CLEAN' END AS cleanup_status
FROM phase6a112_templates p
LEFT JOIN communications.email_templates et ON et.name = p.template_name
ORDER BY p.template_name;

-- SUMMARY: This will tell you EXACTLY:
-- 1. How many templates total have "feel free" text
-- 2. Which specific Phase 6A.112 templates need cleanup
-- 3. Which text variations exist ("feel free to reply" vs "feel free to contact")
