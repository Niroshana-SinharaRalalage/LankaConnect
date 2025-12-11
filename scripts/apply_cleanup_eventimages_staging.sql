-- Apply cleanup to staging database
-- Step 1: Check for duplicates first
\echo '=== Checking for duplicate EventImages ==='
SELECT
    "EventId",
    "DisplayOrder",
    COUNT(*) as count,
    STRING_AGG("Id"::text, ', ') as image_ids
FROM events."EventImages"
GROUP BY "EventId", "DisplayOrder"
HAVING COUNT(*) > 1
ORDER BY "EventId", "DisplayOrder";

-- Step 2: Delete duplicates, keeping only the oldest one
\echo '=== Deleting duplicate EventImages (keeping oldest) ==='
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

-- Step 3: Verify cleanup
\echo '=== Verifying cleanup - current EventImages state ==='
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
