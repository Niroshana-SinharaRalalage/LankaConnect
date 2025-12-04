-- Cleanup script for orphaned EventImages with duplicate DisplayOrder
-- This script deletes EventImages that were created due to the GetByIdAsync bug
-- where Images collection was not loaded, causing all images to get DisplayOrder = 1

-- Check current state
SELECT
    "EventId",
    "DisplayOrder",
    COUNT(*) as count,
    STRING_AGG("Id"::text, ', ') as image_ids
FROM events."EventImages"
GROUP BY "EventId", "DisplayOrder"
HAVING COUNT(*) > 1
ORDER BY "EventId", "DisplayOrder";

-- Delete duplicate EventImages, keeping only the oldest one for each EventId/DisplayOrder pair
-- This preserves the first successfully uploaded image
WITH duplicates AS (
    SELECT
        "Id",
        "EventId",
        "DisplayOrder",
        ROW_NUMBER() OVER (
            PARTITION BY "EventId", "DisplayOrder"
            ORDER BY "CreatedAt" ASC
        ) as rn
    FROM events."EventImages"
)
DELETE FROM events."EventImages"
WHERE "Id" IN (
    SELECT "Id"
    FROM duplicates
    WHERE rn > 1
);

-- Verify cleanup
SELECT
    e."Id" as event_id,
    e."Title" as event_title,
    COUNT(i."Id") as image_count,
    STRING_AGG(i."DisplayOrder"::text, ', ' ORDER BY i."DisplayOrder") as display_orders
FROM events.events e
LEFT JOIN events."EventImages" i ON e."Id" = i."EventId"
GROUP BY e."Id", e."Title"
HAVING COUNT(i."Id") > 0
ORDER BY e."Title";
