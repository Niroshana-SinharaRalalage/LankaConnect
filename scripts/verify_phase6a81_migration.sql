-- Phase 6A.81: Verify Three-State Registration Lifecycle Migration
-- This script checks if the Phase 6A.81 migration was applied successfully

\echo '================================================================'
\echo 'Phase 6A.81 Migration Verification'
\echo '================================================================'
\echo ''

-- 1. Check if new columns exist
\echo '1. Checking for new columns in registrations table...'
SELECT
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_name = 'registrations'
  AND column_name IN ('checkout_session_expires_at', 'abandoned_at')
ORDER BY column_name;

\echo ''
\echo '2. Checking registration status enum values...'
SELECT DISTINCT status
FROM registrations
ORDER BY status;

\echo ''
\echo '3. Checking for Phase 6A.81 indexes...'
SELECT
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'registrations'
  AND (indexname LIKE '%preliminary%' OR
       indexname LIKE '%abandoned%' OR
       indexname LIKE '%active%')
ORDER BY indexname;

\echo ''
\echo '4. Checking migration history for Phase 6A.81...'
SELECT
    "MigrationId",
    "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%Phase6A81%'
ORDER BY "MigrationId" DESC;

\echo ''
\echo '5. Sample registrations with new columns...'
SELECT
    id,
    status,
    payment_status,
    checkout_session_expires_at,
    abandoned_at,
    created_at
FROM registrations
ORDER BY created_at DESC
LIMIT 5;

\echo ''
\echo '6. Count by status (including Preliminary=0 and Abandoned=8)...'
SELECT
    status,
    COUNT(*) as count
FROM registrations
GROUP BY status
ORDER BY status;

\echo ''
\echo '================================================================'
\echo 'Verification Complete'
\echo '================================================================'
