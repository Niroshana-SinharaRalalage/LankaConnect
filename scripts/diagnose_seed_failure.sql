-- PHASE 6A.47 SEED DATA FAILURE - DIAGNOSTIC SCRIPT
-- Execute this against Azure PostgreSQL staging database
-- Connection: Host=lankaconnect-staging-db.postgres.database.azure.com;Database=LankaConnectDB;Username=adminuser;SslMode=Require

-- ====================================================================
-- SECTION 1: VERIFY MIGRATION STATUS
-- ====================================================================

SELECT 'Migration Status Check' AS test_name;

-- Check if Phase6A47 migration is applied
SELECT
    "MigrationId",
    "ProductVersion",
    CASE
        WHEN "MigrationId" = '20251227034100_Phase6A47_Refactor_To_Unified_ReferenceValues'
        THEN 'APPLIED ✓'
        ELSE 'Checking...'
    END AS status
FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%Phase6A47%'
ORDER BY "MigrationId";

-- Count total migrations applied
SELECT
    'Total migrations applied: ' || COUNT(*) AS summary
FROM "__EFMigrationsHistory";

-- ====================================================================
-- SECTION 2: VERIFY TABLE STRUCTURE
-- ====================================================================

SELECT '' AS separator, 'Table Structure Check' AS test_name;

-- Check if reference_values table exists
SELECT
    table_schema,
    table_name,
    CASE
        WHEN table_name = 'reference_values' THEN 'EXISTS ✓'
        ELSE 'Unexpected table'
    END AS status
FROM information_schema.tables
WHERE table_schema = 'reference_data'
ORDER BY table_name;

-- Check reference_values table columns
SELECT
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'reference_data'
  AND table_name = 'reference_values'
ORDER BY ordinal_position;

-- Check indexes
SELECT
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'reference_data'
  AND tablename = 'reference_values'
ORDER BY indexname;

-- ====================================================================
-- SECTION 3: CHECK DATA COUNT (PRIMARY DIAGNOSTIC)
-- ====================================================================

SELECT '' AS separator, 'Data Count Check (CRITICAL)' AS test_name;

-- Total row count (EXPECTED: 402, ACTUAL: likely 0)
SELECT
    COUNT(*) AS total_rows,
    CASE
        WHEN COUNT(*) = 0 THEN 'FAILED - NO DATA ✗'
        WHEN COUNT(*) = 402 THEN 'SUCCESS - FULL SEED ✓'
        WHEN COUNT(*) < 402 THEN 'PARTIAL - ONLY ' || COUNT(*) || ' ROWS ⚠'
        WHEN COUNT(*) > 402 THEN 'DUPLICATE - ' || COUNT(*) || ' ROWS (EXPECTED 402) ⚠'
        ELSE 'UNKNOWN STATE'
    END AS status
FROM reference_data.reference_values;

-- Count by enum type (EXPECTED: 41 distinct types)
SELECT
    COUNT(DISTINCT enum_type) AS distinct_enum_types,
    CASE
        WHEN COUNT(DISTINCT enum_type) = 0 THEN 'FAILED - NO ENUM TYPES ✗'
        WHEN COUNT(DISTINCT enum_type) = 41 THEN 'SUCCESS - ALL 41 TYPES ✓'
        WHEN COUNT(DISTINCT enum_type) < 41 THEN 'PARTIAL - ONLY ' || COUNT(DISTINCT enum_type) || ' TYPES ⚠'
        ELSE 'UNEXPECTED COUNT'
    END AS status
FROM reference_data.reference_values;

-- ====================================================================
-- SECTION 4: DETAILED ENUM TYPE BREAKDOWN
-- ====================================================================

SELECT '' AS separator, 'Enum Type Breakdown' AS test_name;

-- Show count per enum type
SELECT
    enum_type,
    COUNT(*) AS count,
    CASE enum_type
        WHEN 'EventCategory' THEN 8
        WHEN 'EventStatus' THEN 8
        WHEN 'UserRole' THEN 6
        WHEN 'EmailStatus' THEN 11
        WHEN 'EmailType' THEN 9
        WHEN 'EmailDeliveryStatus' THEN 8
        WHEN 'EmailPriority' THEN 4
        WHEN 'Currency' THEN 6
        WHEN 'NotificationType' THEN 8
        WHEN 'IdentityProvider' THEN 2
        WHEN 'SignUpItemCategory' THEN 4
        WHEN 'SignUpType' THEN 2
        WHEN 'AgeCategory' THEN 2
        WHEN 'Gender' THEN 3
        WHEN 'EventType' THEN 10
        WHEN 'SriLankanLanguage' THEN 3
        WHEN 'CulturalBackground' THEN 8
        WHEN 'ReligiousContext' THEN 10
        WHEN 'GeographicRegion' THEN 35
        WHEN 'BuddhistFestival' THEN 11
        WHEN 'HinduFestival' THEN 10
        WHEN 'RegistrationStatus' THEN 4
        WHEN 'PaymentStatus' THEN 4
        WHEN 'PricingType' THEN 3
        WHEN 'SubscriptionStatus' THEN 5
        WHEN 'BadgePosition' THEN 4
        WHEN 'CalendarSystem' THEN 4
        WHEN 'FederatedProvider' THEN 3
        WHEN 'ProficiencyLevel' THEN 5
        WHEN 'BusinessCategory' THEN 9
        WHEN 'BusinessStatus' THEN 4
        WHEN 'ReviewStatus' THEN 4
        WHEN 'ServiceType' THEN 4
        WHEN 'ForumCategory' THEN 5
        WHEN 'TopicStatus' THEN 4
        WHEN 'WhatsAppMessageStatus' THEN 5
        WHEN 'WhatsAppMessageType' THEN 4
        WHEN 'CulturalCommunity' THEN 5
        WHEN 'PassPurchaseStatus' THEN 5
        WHEN 'CulturalConflictLevel' THEN 5
        WHEN 'PoyadayType' THEN 3
        ELSE 0
    END AS expected_count,
    CASE
        WHEN COUNT(*) = CASE enum_type
            WHEN 'EventCategory' THEN 8
            WHEN 'EventStatus' THEN 8
            WHEN 'UserRole' THEN 6
            WHEN 'EmailStatus' THEN 11
            WHEN 'EmailType' THEN 9
            WHEN 'EmailDeliveryStatus' THEN 8
            WHEN 'EmailPriority' THEN 4
            WHEN 'Currency' THEN 6
            WHEN 'NotificationType' THEN 8
            WHEN 'IdentityProvider' THEN 2
            WHEN 'SignUpItemCategory' THEN 4
            WHEN 'SignUpType' THEN 2
            WHEN 'AgeCategory' THEN 2
            WHEN 'Gender' THEN 3
            WHEN 'EventType' THEN 10
            WHEN 'SriLankanLanguage' THEN 3
            WHEN 'CulturalBackground' THEN 8
            WHEN 'ReligiousContext' THEN 10
            WHEN 'GeographicRegion' THEN 35
            WHEN 'BuddhistFestival' THEN 11
            WHEN 'HinduFestival' THEN 10
            WHEN 'RegistrationStatus' THEN 4
            WHEN 'PaymentStatus' THEN 4
            WHEN 'PricingType' THEN 3
            WHEN 'SubscriptionStatus' THEN 5
            WHEN 'BadgePosition' THEN 4
            WHEN 'CalendarSystem' THEN 4
            WHEN 'FederatedProvider' THEN 3
            WHEN 'ProficiencyLevel' THEN 5
            WHEN 'BusinessCategory' THEN 9
            WHEN 'BusinessStatus' THEN 4
            WHEN 'ReviewStatus' THEN 4
            WHEN 'ServiceType' THEN 4
            WHEN 'ForumCategory' THEN 5
            WHEN 'TopicStatus' THEN 4
            WHEN 'WhatsAppMessageStatus' THEN 5
            WHEN 'WhatsAppMessageType' THEN 4
            WHEN 'CulturalCommunity' THEN 5
            WHEN 'PassPurchaseStatus' THEN 5
            WHEN 'CulturalConflictLevel' THEN 5
            WHEN 'PoyadayType' THEN 3
            ELSE 0
        END THEN '✓'
        ELSE '✗ MISMATCH'
    END AS status
FROM reference_data.reference_values
GROUP BY enum_type
ORDER BY enum_type;

-- ====================================================================
-- SECTION 5: CHECK FOR OLD TABLES
-- ====================================================================

SELECT '' AS separator, 'Old Table Existence Check' AS test_name;

-- Check if old tables still exist (they shouldn't)
SELECT
    table_name,
    CASE
        WHEN table_name IN ('event_categories', 'event_statuses', 'user_roles')
        THEN 'OLD TABLE STILL EXISTS ⚠'
        ELSE 'Other table'
    END AS status
FROM information_schema.tables
WHERE table_schema = 'reference_data'
  AND table_name IN ('event_categories', 'event_statuses', 'user_roles')
ORDER BY table_name;

-- If no rows returned, old tables were successfully dropped ✓

-- ====================================================================
-- SECTION 6: DATA QUALITY CHECKS
-- ====================================================================

SELECT '' AS separator, 'Data Quality Checks' AS test_name;

-- Check for duplicate enum_type + int_value combinations (should be 0)
SELECT
    'Duplicate enum_type + int_value: ' || COUNT(*) AS test_result,
    CASE
        WHEN COUNT(*) = 0 THEN '✓ No duplicates'
        ELSE '✗ DUPLICATES FOUND'
    END AS status
FROM (
    SELECT enum_type, int_value, COUNT(*)
    FROM reference_data.reference_values
    GROUP BY enum_type, int_value
    HAVING COUNT(*) > 1
) AS duplicates;

-- Check for duplicate enum_type + code combinations (should be 0)
SELECT
    'Duplicate enum_type + code: ' || COUNT(*) AS test_result,
    CASE
        WHEN COUNT(*) = 0 THEN '✓ No duplicates'
        ELSE '✗ DUPLICATES FOUND'
    END AS status
FROM (
    SELECT enum_type, code, COUNT(*)
    FROM reference_data.reference_values
    GROUP BY enum_type, code
    HAVING COUNT(*) > 1
) AS duplicates;

-- Check for NULL required fields (should be 0)
SELECT
    'NULL required fields: ' || COUNT(*) AS test_result,
    CASE
        WHEN COUNT(*) = 0 THEN '✓ All required fields populated'
        ELSE '✗ NULL VALUES FOUND'
    END AS status
FROM reference_data.reference_values
WHERE enum_type IS NULL
   OR code IS NULL
   OR int_value IS NULL
   OR name IS NULL;

-- Check for inactive records (informational)
SELECT
    'Inactive records: ' || COUNT(*) AS test_result,
    'ℹ Informational (is_active = false)'  AS status
FROM reference_data.reference_values
WHERE is_active = false;

-- ====================================================================
-- SECTION 7: SAMPLE DATA VERIFICATION
-- ====================================================================

SELECT '' AS separator, 'Sample Data Verification' AS test_name;

-- Show sample of each critical enum type
SELECT enum_type, code, int_value, name
FROM reference_data.reference_values
WHERE enum_type IN ('EventCategory', 'EventStatus', 'UserRole', 'EmailStatus', 'Currency')
ORDER BY enum_type, int_value
LIMIT 30;

-- ====================================================================
-- SECTION 8: METADATA VALIDATION
-- ====================================================================

SELECT '' AS separator, 'Metadata JSON Validation' AS test_name;

-- Check for valid JSONB metadata (should not error)
SELECT
    enum_type,
    COUNT(*) AS records_with_metadata
FROM reference_data.reference_values
WHERE metadata IS NOT NULL
  AND metadata != '{}'::jsonb
GROUP BY enum_type
ORDER BY enum_type;

-- Show sample metadata structures
SELECT
    enum_type,
    code,
    metadata
FROM reference_data.reference_values
WHERE metadata IS NOT NULL
  AND metadata != '{}'::jsonb
LIMIT 10;

-- ====================================================================
-- SECTION 9: DATABASE PERMISSIONS CHECK
-- ====================================================================

SELECT '' AS separator, 'Permissions Check' AS test_name;

-- Check if current user has necessary permissions
SELECT
    grantee,
    privilege_type,
    CASE
        WHEN privilege_type IN ('INSERT', 'SELECT', 'UPDATE', 'DELETE') THEN '✓'
        ELSE 'ℹ'
    END AS status
FROM information_schema.role_table_grants
WHERE table_schema = 'reference_data'
  AND table_name = 'reference_values'
  AND grantee = CURRENT_USER
ORDER BY privilege_type;

-- ====================================================================
-- SECTION 10: SUMMARY REPORT
-- ====================================================================

SELECT '' AS separator, 'SUMMARY REPORT' AS test_name;

WITH counts AS (
    SELECT
        COUNT(*) AS total_rows,
        COUNT(DISTINCT enum_type) AS total_enum_types
    FROM reference_data.reference_values
)
SELECT
    'Total rows: ' || total_rows || ' (expected 402)' AS metric_1,
    'Total enum types: ' || total_enum_types || ' (expected 41)' AS metric_2,
    CASE
        WHEN total_rows = 0 AND total_enum_types = 0 THEN
            '🔴 CRITICAL: No seed data - Migration Steps 5b-5g failed completely'
        WHEN total_rows > 0 AND total_rows < 402 THEN
            '🟡 WARNING: Partial seed data - Some migration steps failed'
        WHEN total_rows = 402 AND total_enum_types = 41 THEN
            '🟢 SUCCESS: All seed data present'
        WHEN total_rows > 402 THEN
            '🟡 WARNING: Duplicate data detected - Migration may have run twice'
        ELSE
            '🔴 UNKNOWN: Unexpected state'
    END AS diagnosis,
    CASE
        WHEN total_rows = 0 THEN
            'RECOMMENDED FIX: Execute hotfix migration or manual SQL seed'
        WHEN total_rows > 0 AND total_rows < 402 THEN
            'RECOMMENDED FIX: Delete partial data, then execute hotfix migration'
        WHEN total_rows = 402 THEN
            'NO ACTION NEEDED: System is healthy'
        WHEN total_rows > 402 THEN
            'RECOMMENDED FIX: Identify and remove duplicates'
        ELSE
            'ESCALATE: Investigate with DBA'
    END AS recommended_action
FROM counts;

-- ====================================================================
-- END OF DIAGNOSTIC SCRIPT
-- ====================================================================

-- USAGE INSTRUCTIONS:
-- 1. Copy this entire script
-- 2. Connect to Azure PostgreSQL staging database using psql or pgAdmin
-- 3. Execute the script
-- 4. Review output sections 1-10
-- 5. Pay special attention to Section 10 (Summary Report)
-- 6. Follow recommended action from diagnosis
--
-- CONNECTION STRING:
-- psql "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser sslmode=require"
-- Password: 1qaz!QAZ
