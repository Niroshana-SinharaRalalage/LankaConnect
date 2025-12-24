-- Phase 6A.46: Backfill PublishedAt column for existing events
-- This script sets PublishedAt to a reasonable timestamp for events that are already published
-- Run this on the staging database after the migration has been applied

-- Backfill PublishedAt for all published events (excluding Draft status)
-- Use COALESCE to prefer UpdatedAt, fallback to CreatedAt
UPDATE events.events
SET "PublishedAt" = COALESCE("UpdatedAt", "CreatedAt")
WHERE "Status" IN ('Published', 'Active', 'Completed', 'Cancelled', 'Postponed', 'Archived', 'UnderReview')
  AND "PublishedAt" IS NULL;

-- Verify the update
SELECT
    "Status",
    COUNT(*) as EventCount,
    COUNT("PublishedAt") as WithPublishedAt,
    COUNT(*) - COUNT("PublishedAt") as NullPublishedAt
FROM events.events
GROUP BY "Status"
ORDER BY "Status";
