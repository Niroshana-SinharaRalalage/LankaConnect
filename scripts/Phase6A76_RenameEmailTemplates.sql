-- =====================================================
-- Phase 6A.76: Rename Email Templates + Add Missing Templates
-- Run this script manually against the staging database
-- IMPORTANT: Deploy code changes at the same time!
--
-- Part 1: Renames 14 existing templates to template-* convention
-- Part 2: Adds 5 missing templates that were causing silent failures
--
-- This script should be run INSTEAD of the EF migration if you prefer
-- manual database changes. Both achieve the same result.
-- =====================================================

BEGIN;

-- Show current state before changes
SELECT 'BEFORE CHANGES:' as status;
SELECT name FROM communications.email_templates ORDER BY name;

-- =====================================================
-- PART 1: RENAME ALL 14 EXISTING TEMPLATES
-- =====================================================

-- Rename registration-confirmation to template-free-event-registration-confirmation
UPDATE communications.email_templates
SET name = 'template-free-event-registration-confirmation', updated_at = NOW()
WHERE name = 'registration-confirmation';

-- Rename event-published to template-new-event-publication
UPDATE communications.email_templates
SET name = 'template-new-event-publication', updated_at = NOW()
WHERE name = 'event-published';

-- Rename member-email-verification to template-membership-email-verification
UPDATE communications.email_templates
SET name = 'template-membership-email-verification', updated_at = NOW()
WHERE name = 'member-email-verification';

-- Rename event-cancelled-notification to template-event-cancellation-notifications
UPDATE communications.email_templates
SET name = 'template-event-cancellation-notifications', updated_at = NOW()
WHERE name = 'event-cancelled-notification';

-- Rename registration-cancellation to template-event-registration-cancellation
UPDATE communications.email_templates
SET name = 'template-event-registration-cancellation', updated_at = NOW()
WHERE name = 'registration-cancellation';

-- Rename newsletter-confirmation to template-newsletter-subscription-confirmation
UPDATE communications.email_templates
SET name = 'template-newsletter-subscription-confirmation', updated_at = NOW()
WHERE name = 'newsletter-confirmation';

-- Rename newsletter to template-newsletter-notification
UPDATE communications.email_templates
SET name = 'template-newsletter-notification', updated_at = NOW()
WHERE name = 'newsletter';

-- Rename event-details to template-event-details-publication
UPDATE communications.email_templates
SET name = 'template-event-details-publication', updated_at = NOW()
WHERE name = 'event-details';

-- Rename signup-commitment-confirmation to template-signup-list-commitment-confirmation
UPDATE communications.email_templates
SET name = 'template-signup-list-commitment-confirmation', updated_at = NOW()
WHERE name = 'signup-commitment-confirmation';

-- Rename signup-commitment-updated to template-signup-list-commitment-update
UPDATE communications.email_templates
SET name = 'template-signup-list-commitment-update', updated_at = NOW()
WHERE name = 'signup-commitment-updated';

-- Rename signup-commitment-cancelled to template-signup-list-commitment-cancellation
UPDATE communications.email_templates
SET name = 'template-signup-list-commitment-cancellation', updated_at = NOW()
WHERE name = 'signup-commitment-cancelled';

-- Rename event-approved to template-event-approval
UPDATE communications.email_templates
SET name = 'template-event-approval', updated_at = NOW()
WHERE name = 'event-approved';

-- Rename event-reminder to template-event-reminder
UPDATE communications.email_templates
SET name = 'template-event-reminder', updated_at = NOW()
WHERE name = 'event-reminder';

-- Rename ticket-confirmation to template-paid-event-registration-confirmation-with-ticket
UPDATE communications.email_templates
SET name = 'template-paid-event-registration-confirmation-with-ticket', updated_at = NOW()
WHERE name = 'ticket-confirmation';

-- Also rename organizer-role-approved if it exists from old migration
UPDATE communications.email_templates
SET name = 'template-organizer-role-approval', updated_at = NOW()
WHERE name = 'organizer-role-approved';

-- Show state after renames
SELECT 'AFTER RENAMES:' as status;
SELECT name FROM communications.email_templates ORDER BY name;

-- Verify renamed templates
SELECT 'VERIFICATION - Renamed Templates:' as status;
SELECT
    CASE
        WHEN COUNT(*) >= 14 THEN 'SUCCESS: At least 14 templates renamed'
        ELSE 'WARNING: Expected 14+ templates, found ' || COUNT(*)::text
    END as result
FROM communications.email_templates
WHERE name LIKE 'template-%';

COMMIT;

-- =====================================================
-- PART 2: ADD 5 MISSING TEMPLATES
-- Run the EF migration for this part:
-- dotnet ef database update --project src/LankaConnect.Infrastructure
--
-- The migration file is:
-- 20260122200000_Phase6A76_RenameAndAddEmailTemplates.cs
--
-- Templates being added:
-- - template-password-reset
-- - template-password-change-confirmation
-- - template-welcome
-- - template-anonymous-rsvp-confirmation
-- - template-organizer-role-approval
-- =====================================================

-- =====================================================
-- ROLLBACK SCRIPT (if needed, run this separately)
-- =====================================================
/*
BEGIN;

-- Revert all template renames
UPDATE communications.email_templates
SET name = 'registration-confirmation', updated_at = NOW()
WHERE name = 'template-free-event-registration-confirmation';

UPDATE communications.email_templates
SET name = 'event-published', updated_at = NOW()
WHERE name = 'template-new-event-publication';

UPDATE communications.email_templates
SET name = 'member-email-verification', updated_at = NOW()
WHERE name = 'template-membership-email-verification';

UPDATE communications.email_templates
SET name = 'event-cancelled-notification', updated_at = NOW()
WHERE name = 'template-event-cancellation-notifications';

UPDATE communications.email_templates
SET name = 'registration-cancellation', updated_at = NOW()
WHERE name = 'template-event-registration-cancellation';

UPDATE communications.email_templates
SET name = 'newsletter-confirmation', updated_at = NOW()
WHERE name = 'template-newsletter-subscription-confirmation';

UPDATE communications.email_templates
SET name = 'newsletter', updated_at = NOW()
WHERE name = 'template-newsletter-notification';

UPDATE communications.email_templates
SET name = 'event-details', updated_at = NOW()
WHERE name = 'template-event-details-publication';

UPDATE communications.email_templates
SET name = 'signup-commitment-confirmation', updated_at = NOW()
WHERE name = 'template-signup-list-commitment-confirmation';

UPDATE communications.email_templates
SET name = 'signup-commitment-updated', updated_at = NOW()
WHERE name = 'template-signup-list-commitment-update';

UPDATE communications.email_templates
SET name = 'signup-commitment-cancelled', updated_at = NOW()
WHERE name = 'template-signup-list-commitment-cancellation';

UPDATE communications.email_templates
SET name = 'event-approved', updated_at = NOW()
WHERE name = 'template-event-approval';

UPDATE communications.email_templates
SET name = 'event-reminder', updated_at = NOW()
WHERE name = 'template-event-reminder';

UPDATE communications.email_templates
SET name = 'ticket-confirmation', updated_at = NOW()
WHERE name = 'template-paid-event-registration-confirmation-with-ticket';

-- Delete the 5 new templates
DELETE FROM communications.email_templates
WHERE name IN (
    'template-password-reset',
    'template-password-change-confirmation',
    'template-welcome',
    'template-anonymous-rsvp-confirmation',
    'template-organizer-role-approval'
);

COMMIT;
*/
