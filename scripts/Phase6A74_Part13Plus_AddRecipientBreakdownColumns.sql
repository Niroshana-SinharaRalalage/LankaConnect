-- Phase 6A.74 Part 13+: Add detailed recipient breakdown columns to newsletter_email_history
-- These columns track the 4 recipient sources separately plus success/failed counts

-- Add new columns for detailed breakdown
ALTER TABLE communications.newsletter_email_history
ADD COLUMN IF NOT EXISTS newsletter_email_group_count INTEGER NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS event_email_group_count INTEGER NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS subscriber_count INTEGER NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS event_registration_count INTEGER NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS successful_sends INTEGER NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS failed_sends INTEGER NOT NULL DEFAULT 0;

-- Backfill existing records: Use legacy columns to populate new columns for historical data
-- Since we don't have the detailed breakdown for existing records,
-- we'll set newsletter_email_group_count = email_group_recipient_count (legacy)
-- and subscriber_count = subscriber_recipient_count (legacy)
-- event_email_group_count and event_registration_count will be 0 (unknown for legacy data)
UPDATE communications.newsletter_email_history
SET
    newsletter_email_group_count = COALESCE(email_group_recipient_count, 0),
    subscriber_count = COALESCE(subscriber_recipient_count, 0),
    successful_sends = total_recipient_count,
    failed_sends = 0
WHERE newsletter_email_group_count = 0
  AND subscriber_count = 0
  AND successful_sends = 0;

-- Verify the migration
SELECT
    id,
    newsletter_id,
    total_recipient_count,
    newsletter_email_group_count,
    event_email_group_count,
    subscriber_count,
    event_registration_count,
    successful_sends,
    failed_sends,
    email_group_recipient_count as legacy_email_group,
    subscriber_recipient_count as legacy_subscriber
FROM communications.newsletter_email_history
ORDER BY sent_at DESC
LIMIT 5;
