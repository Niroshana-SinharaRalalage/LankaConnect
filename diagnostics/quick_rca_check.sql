-- ================================================
-- QUICK RCA DIAGNOSTICS
-- Execute these 5 queries in order
-- ================================================

-- QUERY 1: Migration Status (30 seconds)
-- Expected: Should see Phase6A53Fix3 migration
SELECT
    "MigrationId",
    "ProductVersion",
    CASE
        WHEN "MigrationId" LIKE '%Phase6A53%' THEN '✅ PHASE 6A.53 MIGRATION'
        ELSE 'Other migration'
    END as migration_status
FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%Phase6A53%' OR "MigrationId" LIKE '%Email%'
ORDER BY "MigrationId" DESC
LIMIT 10;

-- QUERY 2: Email Template Version (30 seconds)
-- Expected: NEW_TEMPLATE without decorative stars
SELECT
    "Id",
    "Type" as template_type,
    CASE
        WHEN "HtmlTemplate" LIKE '%✦%' THEN '❌ OLD_TEMPLATE_WITH_STARS'
        WHEN "HtmlTemplate" LIKE '%UserName%' THEN '✅ NEW_TEMPLATE_WITH_USERNAME'
        ELSE '⚠️ UNKNOWN_TEMPLATE'
    END as template_version,
    LENGTH("HtmlTemplate") as template_size_bytes,
    "UpdatedAt",
    "CreatedAt"
FROM "EmailTemplates"
WHERE "Type" = 1  -- EmailVerification
ORDER BY "UpdatedAt" DESC;

-- QUERY 3: Recent Registration Attempts (30 seconds)
-- Expected: Multiple users created in last 24 hours with tokens
SELECT
    "Id",
    "Email",
    CONCAT("FirstName", ' ', "LastName") as full_name,
    "IsEmailVerified",
    CASE
        WHEN "EmailVerificationToken" IS NOT NULL THEN '✅ Has Token'
        ELSE '❌ No Token'
    END as token_status,
    "EmailVerificationTokenExpiresAt",
    "CreatedAt",
    ROUND(EXTRACT(EPOCH FROM (NOW() - "CreatedAt"))/60, 1) as minutes_ago
FROM "Users"
WHERE "CreatedAt" >= NOW() - INTERVAL '24 hours'
ORDER BY "CreatedAt" DESC
LIMIT 10;

-- QUERY 4: Token Timing Analysis (30 seconds)
-- Expected: Token should be generated within 1 second of user creation
SELECT
    "Email",
    "CreatedAt",
    "EmailVerificationTokenExpiresAt",
    ROUND(
        EXTRACT(EPOCH FROM (
            ("EmailVerificationTokenExpiresAt" - INTERVAL '24 hours') - "CreatedAt"
        )),
        2
    ) as token_delay_seconds,
    CASE
        WHEN ("EmailVerificationTokenExpiresAt" - INTERVAL '24 hours') - "CreatedAt" < INTERVAL '5 seconds'
            THEN '✅ Immediate Token Generation'
        WHEN ("EmailVerificationTokenExpiresAt" - INTERVAL '24 hours') - "CreatedAt" < INTERVAL '1 minute'
            THEN '⚠️ Delayed Token Generation'
        ELSE '❌ Significant Delay'
    END as timing_status
FROM "Users"
WHERE "CreatedAt" >= NOW() - INTERVAL '24 hours'
    AND "EmailVerificationTokenExpiresAt" IS NOT NULL
ORDER BY "CreatedAt" DESC
LIMIT 10;

-- QUERY 5: User Creation Success Rate (30 seconds)
-- Expected: High creation rate, low verification rate
SELECT
    DATE_TRUNC('hour', "CreatedAt") as hour,
    COUNT(*) as users_created,
    SUM(CASE WHEN "EmailVerificationToken" IS NOT NULL THEN 1 ELSE 0 END) as users_with_tokens,
    SUM(CASE WHEN "IsEmailVerified" THEN 1 ELSE 0 END) as users_verified,
    ROUND(
        100.0 * SUM(CASE WHEN "EmailVerificationToken" IS NOT NULL THEN 1 ELSE 0 END) / COUNT(*),
        1
    ) as token_generation_rate_pct,
    ROUND(
        100.0 * SUM(CASE WHEN "IsEmailVerified" THEN 1 ELSE 0 END) / COUNT(*),
        1
    ) as verification_rate_pct
FROM "Users"
WHERE "CreatedAt" >= NOW() - INTERVAL '48 hours'
GROUP BY DATE_TRUNC('hour', "CreatedAt")
ORDER BY hour DESC
LIMIT 24;

-- ================================================
-- INTERPRETATION GUIDE
-- ================================================
-- QUERY 1: Should show Phase6A53Fix3 migration
--   ❌ If missing → Migration not applied, email template may be old
--
-- QUERY 2: Should show NEW_TEMPLATE_WITH_USERNAME
--   ❌ If OLD_TEMPLATE_WITH_STARS → Email rendering may fail
--
-- QUERY 3: Should show multiple users with tokens created recently
--   ✅ If users have tokens → User creation working
--   ❌ If IsEmailVerified=false → Email delivery failing
--
-- QUERY 4: Should show "Immediate Token Generation" (<5 seconds)
--   ⚠️ If delayed → Indicates token regeneration issue
--
-- QUERY 5: Should show high token_generation_rate (near 100%)
--   ❌ If verification_rate_pct is 0% → Email system broken
--   ⚠️ If verification_rate_pct is low → Emails being sent but users not verifying
-- ================================================
