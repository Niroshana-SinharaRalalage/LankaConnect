-- Verify newsletter_subscribers table and indexes in staging database

-- 1. Check if table exists
SELECT
    table_schema,
    table_name,
    table_type
FROM information_schema.tables
WHERE table_schema = 'communications'
  AND table_name = 'newsletter_subscribers';

-- 2. Get all columns for newsletter_subscribers
SELECT
    column_name,
    data_type,
    character_maximum_length,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'communications'
  AND table_name = 'newsletter_subscribers'
ORDER BY ordinal_position;

-- 3. Check all indexes on newsletter_subscribers table
SELECT
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'communications'
  AND tablename = 'newsletter_subscribers'
ORDER BY indexname;

-- 4. Check if the 5 strategic indexes exist
SELECT
    CASE
        WHEN EXISTS (
            SELECT 1 FROM pg_indexes
            WHERE schemaname = 'communications'
              AND tablename = 'newsletter_subscribers'
              AND indexname = 'idx_newsletter_subscribers_email'
        ) THEN '✓ idx_newsletter_subscribers_email'
        ELSE '✗ idx_newsletter_subscribers_email MISSING'
    END AS email_index,
    CASE
        WHEN EXISTS (
            SELECT 1 FROM pg_indexes
            WHERE schemaname = 'communications'
              AND tablename = 'newsletter_subscribers'
              AND indexname = 'idx_newsletter_subscribers_confirmation_token'
        ) THEN '✓ idx_newsletter_subscribers_confirmation_token'
        ELSE '✗ idx_newsletter_subscribers_confirmation_token MISSING'
    END AS confirmation_token_index,
    CASE
        WHEN EXISTS (
            SELECT 1 FROM pg_indexes
            WHERE schemaname = 'communications'
              AND tablename = 'newsletter_subscribers'
              AND indexname = 'idx_newsletter_subscribers_unsubscribe_token'
        ) THEN '✓ idx_newsletter_subscribers_unsubscribe_token'
        ELSE '✗ idx_newsletter_subscribers_unsubscribe_token MISSING'
    END AS unsubscribe_token_index,
    CASE
        WHEN EXISTS (
            SELECT 1 FROM pg_indexes
            WHERE schemaname = 'communications'
              AND tablename = 'newsletter_subscribers'
              AND indexname = 'idx_newsletter_subscribers_metro_area_id'
        ) THEN '✓ idx_newsletter_subscribers_metro_area_id'
        ELSE '✗ idx_newsletter_subscribers_metro_area_id MISSING'
    END AS metro_area_index,
    CASE
        WHEN EXISTS (
            SELECT 1 FROM pg_indexes
            WHERE schemaname = 'communications'
              AND tablename = 'newsletter_subscribers'
              AND indexname = 'idx_newsletter_subscribers_active_confirmed'
        ) THEN '✓ idx_newsletter_subscribers_active_confirmed'
        ELSE '✗ idx_newsletter_subscribers_active_confirmed MISSING'
    END AS active_confirmed_index;

-- 5. Get row count (should be 0 initially)
SELECT
    'newsletter_subscribers' as table_name,
    COUNT(*) as row_count
FROM communications.newsletter_subscribers;
