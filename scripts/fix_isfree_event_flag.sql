-- IsFreeEvent fix: Backfill events that should be free but are marked as paid
-- This fixes events created after the Phase 6A.86 backfill migration
-- where the frontend "free event" checkbox was checked but backend didn't process it.
--
-- Condition: IsFreeEvent=false AND no pricing data at all (both NULL)
-- These are free events that were incorrectly marked as paid.

-- Preview: See which events will be affected
SELECT "Id", "Title", "IsFreeEvent", ticket_price, pricing, "CreatedAt", "Status"
FROM events.events
WHERE "IsFreeEvent" = false
  AND ticket_price IS NULL
  AND pricing IS NULL;

-- Fix: Set IsFreeEvent=true for these events
UPDATE events.events
SET
    "IsFreeEvent" = true,
    "UpdatedAt" = NOW()
WHERE "IsFreeEvent" = false
  AND ticket_price IS NULL
  AND pricing IS NULL;
