-- Query to check registration timestamp for debugging Phase 6A.34 fix
-- Run this against your Azure PostgreSQL database

-- Replace with your user's ID if known, or check by event ID
-- Phase 6A.34 was deployed at: 2025-12-18 04:38:00 UTC

-- Query 1: Check specific event registrations with deployment relation
SELECT
    r."Id",
    r."UserId",
    r."EventId",
    r."Status",
    r."PaymentStatus",
    r."CreatedAt",
    r."UpdatedAt",
    r."Quantity",
    CASE
        WHEN r."CreatedAt" < '2025-12-18 04:38:00+00' THEN 'BEFORE Phase 6A.34 (Explains missing logs)'
        ELSE 'AFTER Phase 6A.34 (Should have logs)'
    END AS "DeploymentRelation",
    EXTRACT(EPOCH FROM (CURRENT_TIMESTAMP - r."CreatedAt")) / 3600 AS "HoursAgo"
FROM "Registrations" r
WHERE r."EventId" = 'c1f182a9-c957-4a78-a0b2-085917a88900'
ORDER BY r."CreatedAt" DESC
LIMIT 20;

-- Query 2: Check recent registrations across all events (last 24 hours)
SELECT
    r."Id",
    r."UserId",
    r."EventId",
    r."Status",
    r."PaymentStatus",
    r."CreatedAt",
    r."UpdatedAt",
    e."Title" AS "EventTitle",
    CASE
        WHEN r."CreatedAt" < '2025-12-18 04:38:00+00' THEN 'BEFORE Phase 6A.34'
        ELSE 'AFTER Phase 6A.34'
    END AS "DeploymentRelation"
FROM "Registrations" r
LEFT JOIN "Events" e ON e."Id" = r."EventId"
WHERE r."CreatedAt" > CURRENT_TIMESTAMP - INTERVAL '24 hours'
ORDER BY r."CreatedAt" DESC;

-- Query 3: Check if confirmation emails were queued (OutboxMessages)
SELECT
    om."Id",
    om."Type",
    om."Status",
    om."CreatedAt",
    om."ProcessedAt",
    om."ErrorMessage",
    CASE
        WHEN om."CreatedAt" < '2025-12-18 04:38:00+00' THEN 'BEFORE Phase 6A.34'
        ELSE 'AFTER Phase 6A.34'
    END AS "DeploymentRelation",
    -- Parse registration ID from content (if available)
    om."Content"::json->>'RegistrationId' AS "RegistrationId"
FROM "OutboxMessages" om
WHERE om."Type" LIKE '%RegistrationConfirmedEvent%'
AND om."CreatedAt" > CURRENT_TIMESTAMP - INTERVAL '48 hours'
ORDER BY om."CreatedAt" DESC
LIMIT 30;

-- Query 4: Check user's recent activity
-- Replace 'USER_ID_HERE' with actual user ID if known
/*
SELECT
    r."Id",
    r."EventId",
    r."Status",
    r."CreatedAt",
    e."Title" AS "EventTitle",
    CASE
        WHEN r."CreatedAt" < '2025-12-18 04:38:00+00' THEN 'BEFORE Phase 6A.34'
        ELSE 'AFTER Phase 6A.34'
    END AS "DeploymentRelation"
FROM "Registrations" r
LEFT JOIN "Events" e ON e."Id" = r."EventId"
WHERE r."UserId" = 'USER_ID_HERE'
ORDER BY r."CreatedAt" DESC
LIMIT 10;
*/

-- Query 5: Count registrations before and after deployment
SELECT
    CASE
        WHEN r."CreatedAt" < '2025-12-18 04:38:00+00' THEN 'Before Phase 6A.34'
        ELSE 'After Phase 6A.34'
    END AS "Period",
    COUNT(*) AS "RegistrationCount",
    COUNT(CASE WHEN r."Status" = 0 THEN 1 END) AS "Confirmed",
    COUNT(CASE WHEN r."Status" = 1 THEN 1 END) AS "Cancelled"
FROM "Registrations" r
WHERE r."CreatedAt" > CURRENT_TIMESTAMP - INTERVAL '48 hours'
GROUP BY "Period"
ORDER BY "Period";

-- Instructions:
-- 1. Connect to your Azure PostgreSQL database
-- 2. Run Query 1 to check the specific event registration timestamp
-- 3. If CreatedAt < 2025-12-18 04:38:00 UTC, registration happened BEFORE fix
-- 4. If CreatedAt > 2025-12-18 04:38:00 UTC, registration happened AFTER fix
-- 5. Run Query 3 to check if confirmation emails were queued
-- 6. Compare results to determine if Phase 6A.34 fix is working
