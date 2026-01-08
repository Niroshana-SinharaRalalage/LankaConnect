-- Phase 6A.64: Newsletter Subscriber Metro Areas Junction Table
-- MANUAL MIGRATION SCRIPT
-- Use this ONLY if automated migration hasn't run

-- ============================================
-- SAFETY CHECKS
-- ============================================

-- Check 1: Verify junction table does NOT already exist
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'communications'
        AND table_name = 'newsletter_subscriber_metro_areas'
    ) THEN
        RAISE EXCEPTION 'Junction table already exists! Migration may have already run.';
    END IF;
    RAISE NOTICE 'Safety check passed: Junction table does not exist yet.';
END $$;

-- Check 2: Verify old column still exists
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'communications'
        AND table_name = 'newsletter_subscribers'
        AND column_name = 'metro_area_id'
    ) THEN
        RAISE EXCEPTION 'Old metro_area_id column is missing! Migration may have already run.';
    END IF;
    RAISE NOTICE 'Safety check passed: Old metro_area_id column exists.';
END $$;

-- ============================================
-- MIGRATION STEPS
-- ============================================

-- Step 1: Create junction table
CREATE TABLE communications.newsletter_subscriber_metro_areas (
    subscriber_id uuid NOT NULL,
    metro_area_id uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_newsletter_subscriber_metro_areas PRIMARY KEY (subscriber_id, metro_area_id),
    CONSTRAINT fk_newsletter_subscriber_metro_areas_newsletter_subscribers
        FOREIGN KEY (subscriber_id)
        REFERENCES communications.newsletter_subscribers(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_newsletter_subscriber_metro_areas_metro_areas
        FOREIGN KEY (metro_area_id)
        REFERENCES events.metro_areas(id)
        ON DELETE CASCADE
);

RAISE NOTICE 'Step 1 complete: Junction table created.';

-- Step 2: Create indexes for query performance
CREATE INDEX ix_newsletter_subscriber_metro_areas_metro_area_id
    ON communications.newsletter_subscriber_metro_areas(metro_area_id);

CREATE INDEX ix_newsletter_subscriber_metro_areas_subscriber_id
    ON communications.newsletter_subscriber_metro_areas(subscriber_id);

RAISE NOTICE 'Step 2 complete: Indexes created.';

-- Step 3: Migrate existing data
INSERT INTO communications.newsletter_subscriber_metro_areas (subscriber_id, metro_area_id, created_at)
SELECT id, metro_area_id, created_at
FROM communications.newsletter_subscribers
WHERE metro_area_id IS NOT NULL;

-- Get count of migrated rows
DO $$
DECLARE
    migrated_count INT;
BEGIN
    SELECT COUNT(*) INTO migrated_count
    FROM communications.newsletter_subscriber_metro_areas;

    RAISE NOTICE 'Step 3 complete: % rows migrated to junction table.', migrated_count;
END $$;

-- Step 4: Drop old metro_area_id column
ALTER TABLE communications.newsletter_subscribers
DROP COLUMN metro_area_id;

RAISE NOTICE 'Step 4 complete: Old metro_area_id column dropped.';

-- ============================================
-- VERIFICATION
-- ============================================

-- Verify junction table structure
SELECT
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'communications'
AND table_name = 'newsletter_subscriber_metro_areas'
ORDER BY ordinal_position;

-- Verify indexes
SELECT
    indexname,
    tablename
FROM pg_indexes
WHERE schemaname = 'communications'
AND tablename = 'newsletter_subscriber_metro_areas';

-- Verify old column is gone
SELECT
    COUNT(*) as old_column_exists
FROM information_schema.columns
WHERE table_schema = 'communications'
AND table_name = 'newsletter_subscribers'
AND column_name = 'metro_area_id';
-- Should return: 0

-- Verify data migrated
SELECT
    COUNT(*) as total_junction_rows
FROM communications.newsletter_subscriber_metro_areas;

-- ============================================
-- SUCCESS
-- ============================================

DO $$
BEGIN
    RAISE NOTICE '';
    RAISE NOTICE '✅ Phase 6A.64 migration completed successfully!';
    RAISE NOTICE '';
    RAISE NOTICE 'Next steps:';
    RAISE NOTICE '1. Test newsletter subscription in UI';
    RAISE NOTICE '2. Verify multiple metro areas are stored';
    RAISE NOTICE '3. Test event cancellation emails';
    RAISE NOTICE '';
END $$;
