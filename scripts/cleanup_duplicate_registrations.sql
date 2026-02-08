-- Data Cleanup: Find and resolve duplicate registrations before adding unique constraints
-- This script must be run BEFORE the migration that adds unique indexes.
-- Run on: Production database
-- Date: 2026-02-08

-- Step 1: Find duplicate registrations by email per event (all paths)
SELECT r."EventId", r.contact->>'email' AS email, COUNT(*) AS count,
       string_agg(r."Id"::text || ' (' || r."Status" || ', UserId=' || COALESCE(r."UserId"::text, 'null') || ')', ', ') AS registrations
FROM events.registrations r
WHERE r."Status" NOT IN ('Cancelled', 'Refunded', 'RefundRequested', 'Abandoned', 'Preliminary', 'Pending')
  AND r.contact IS NOT NULL
  AND r.contact->>'email' IS NOT NULL
GROUP BY r."EventId", r.contact->>'email'
HAVING COUNT(*) > 1;

-- Step 2: Find duplicate registrations by UserId per event
SELECT r."EventId", r."UserId", COUNT(*) AS count,
       string_agg(r."Id"::text || ' (' || r."Status" || ')', ', ') AS registrations
FROM events.registrations r
WHERE r."UserId" IS NOT NULL
  AND r."Status" NOT IN ('Cancelled', 'Refunded', 'RefundRequested', 'Abandoned', 'Preliminary', 'Pending')
GROUP BY r."EventId", r."UserId"
HAVING COUNT(*) > 1;

-- Step 3: For each duplicate found above, cancel the newer one (keep the oldest).
-- Replace <registration_id> with the actual ID of the duplicate to cancel.
-- DO NOT run this blindly - review each duplicate first.

-- Example:
-- UPDATE events.registrations
-- SET "Status" = 'Cancelled', "UpdatedAt" = NOW()
-- WHERE "Id" = '<registration_id_to_cancel>';
