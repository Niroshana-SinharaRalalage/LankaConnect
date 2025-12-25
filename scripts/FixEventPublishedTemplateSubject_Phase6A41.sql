-- Phase 6A.41: Fix event-published template subject
-- ROOT CAUSE: Template was seeded with NULL subject_template value in Phase 6A.39 migration
-- This script updates the existing template with the correct subject
--
-- ISSUE: Event publication emails fail with "Cannot access value of a failed result"
-- CAUSE: Database template has NULL subject_template despite migration showing valid value
-- FIX: Update template with intended subject from migration specification
--
-- SAFE TO RUN: Updates existing template only if subject is NULL/empty
-- IDEMPOTENT: Can be run multiple times safely

BEGIN;

-- Verify template exists before updating
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM communications.email_templates
        WHERE name = 'event-published'
    ) THEN
        RAISE EXCEPTION 'Template event-published not found. Run Phase 6A.39 migration (20251221160725_SeedEventPublishedTemplate_Phase6A39) first.';
    END IF;

    RAISE NOTICE 'Template event-published found. Proceeding with update...';
END $$;

-- Check current state before update
DO $$
DECLARE
    current_subject TEXT;
BEGIN
    SELECT subject_template INTO current_subject
    FROM communications.email_templates
    WHERE name = 'event-published';

    RAISE NOTICE 'Current subject_template value: %', COALESCE(current_subject, 'NULL');

    IF current_subject IS NULL OR current_subject = '' THEN
        RAISE NOTICE 'Subject is NULL/empty. Will update to correct value.';
    ELSE
        RAISE NOTICE 'Subject already has value. Will update only if needed.';
    END IF;
END $$;

-- Update subject_template to match intended value from Phase 6A.39 migration
-- This is the value that should have been inserted during migration
UPDATE communications.email_templates
SET
    subject_template = 'New Event: {{EventTitle}} in {{EventCity}}, {{EventState}}',
    updated_at = NOW()
WHERE name = 'event-published'
  AND (subject_template IS NULL OR subject_template = '' OR subject_template != 'New Event: {{EventTitle}} in {{EventCity}}, {{EventState}}');

-- Get affected rows count
DO $$
DECLARE
    rows_updated INTEGER;
BEGIN
    GET DIAGNOSTICS rows_updated = ROW_COUNT;
    RAISE NOTICE 'Rows updated: %', rows_updated;
END $$;

-- Verify update succeeded
DO $$
DECLARE
    updated_subject TEXT;
    template_id UUID;
    template_active BOOLEAN;
    template_updated TIMESTAMP;
BEGIN
    SELECT id, subject_template, is_active, updated_at
    INTO template_id, updated_subject, template_active, template_updated
    FROM communications.email_templates
    WHERE name = 'event-published';

    RAISE NOTICE '=== Verification Results ===';
    RAISE NOTICE 'Template ID: %', template_id;
    RAISE NOTICE 'Subject Template: %', COALESCE(updated_subject, 'NULL');
    RAISE NOTICE 'Is Active: %', template_active;
    RAISE NOTICE 'Last Updated: %', template_updated;

    IF updated_subject IS NULL OR updated_subject = '' THEN
        RAISE EXCEPTION 'VERIFICATION FAILED: subject_template is still NULL or empty after update. Current value: %', COALESCE(updated_subject, 'NULL');
    END IF;

    IF updated_subject != 'New Event: {{EventTitle}} in {{EventCity}}, {{EventState}}' THEN
        RAISE EXCEPTION 'VERIFICATION FAILED: subject_template has unexpected value: %', updated_subject;
    END IF;

    IF NOT template_active THEN
        RAISE WARNING 'Template is not active. Emails will not be sent until template is activated.';
    END IF;

    RAISE NOTICE '=== SUCCESS ===';
    RAISE NOTICE 'Template event-published updated successfully with valid subject.';
    RAISE NOTICE 'Expected placeholders verified: {{EventTitle}}, {{EventCity}}, {{EventState}}';
END $$;

COMMIT;

-- Final verification query (can be run separately for manual confirmation)
SELECT
    name,
    subject_template,
    LENGTH(subject_template) as subject_length,
    CASE
        WHEN subject_template IS NULL THEN 'ERROR: NULL'
        WHEN subject_template = '' THEN 'ERROR: EMPTY'
        WHEN subject_template LIKE '%{{EventTitle}}%' AND
             subject_template LIKE '%{{EventCity}}%' AND
             subject_template LIKE '%{{EventState}}%' THEN 'SUCCESS: VALID (All placeholders present)'
        WHEN subject_template LIKE '%{{EventTitle}}%' THEN 'WARNING: PARTIAL (Missing city/state placeholders)'
        ELSE 'ERROR: INVALID (No placeholders)'
    END as subject_status,
    is_active,
    updated_at
FROM communications.email_templates
WHERE name = 'event-published';
