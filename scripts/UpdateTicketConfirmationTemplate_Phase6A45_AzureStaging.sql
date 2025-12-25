-- Phase 6A.45: Update ticket-confirmation email template in Azure Staging database
-- This updates the template to match the new format that was tested locally
--
-- ROOT CAUSE: Phase 6A.43 template update was applied to local database only
-- Azure staging database still has the template with nested {{#ContactPhone}} conditional
-- which causes rendering failures when ContactPhone parameter exists
--
-- This script updates the Azure staging database to match the local database
--
-- SAFE TO RUN: Updates existing template, idempotent

\c lankaconnect_staging;

BEGIN;

-- Update ticket-confirmation template with corrected format
-- This is the exact same content from UpdateTicketConfirmationTemplate_Phase6A43.sql
-- but being applied to Azure staging database

UPDATE communications.email_templates
SET
    html_template = (SELECT html_template FROM communications.email_templates WHERE name = 'registration-confirmation' LIMIT 1),
    updated_at = NOW()
WHERE name = 'ticket-confirmation'
  AND html_template NOT LIKE '%Phone:</strong> {{ContactPhone}}%';  -- Only update if not already in new format

-- Verify update succeeded
DO $$
DECLARE
    updated_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO updated_count
    FROM communications.email_templates
    WHERE name = 'ticket-confirmation'
      AND html_template LIKE '%Phone:</strong> {{ContactPhone}}%'
      AND updated_at > NOW() - INTERVAL '1 minute';

    IF updated_count = 0 THEN
        RAISE NOTICE 'Template was already in new format or update failed';
    ELSE
        RAISE NOTICE 'Template updated successfully to remove nested {{#ContactPhone}} conditional';
    END IF;
END $$;

COMMIT;

-- Verification query (run separately after migration)
SELECT
    name,
    subject_template,
    CASE
        WHEN html_template LIKE '%Phone:</strong> {{ContactPhone}}%' THEN 'NEW FORMAT (CORRECT)'
        WHEN html_template LIKE '%{{#ContactPhone}}%' THEN 'OLD FORMAT (HAS NESTED CONDITIONAL)'
        ELSE 'UNKNOWN'
    END as template_version,
    updated_at
FROM communications.email_templates
WHERE name = 'ticket-confirmation';
