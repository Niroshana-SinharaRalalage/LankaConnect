-- =====================================================
-- Phase 6A.78 Verification Query
-- Check that all 19 email templates have plain text
-- (not HTML) in their text_template column
-- =====================================================

-- Main verification: Check for ANY templates with HTML in text_template
SELECT
    name,
    CASE
        WHEN text_template LIKE '<!DOCTYPE%' THEN 'FAIL ❌'
        WHEN text_template LIKE '%<html%' THEN 'FAIL ❌'
        ELSE 'OK ✅'
    END as status,
    LEFT(text_template, 80) as text_preview,
    LENGTH(text_template) as text_length,
    updated_at
FROM communications.email_templates
WHERE name LIKE 'template-%'
ORDER BY name;

-- Summary statistics
SELECT
    COUNT(*) as total_templates,
    COUNT(CASE WHEN text_template NOT LIKE '<!DOCTYPE%' AND text_template NOT LIKE '%<html%' THEN 1 END) as correct_templates,
    COUNT(CASE WHEN text_template LIKE '<!DOCTYPE%' OR text_template LIKE '%<html%' THEN 1 END) as failed_templates
FROM communications.email_templates
WHERE name LIKE 'template-%';

-- Expected: All 19 templates should be in 'correct_templates', 0 in 'failed_templates'
