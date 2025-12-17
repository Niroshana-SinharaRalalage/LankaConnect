-- Phase 6A.31c: Manual Badge NULL Fix Script
-- Purpose: Direct SQL fix for Badge location config NULL values
-- Use this if migration didn't execute or failed silently
--
-- IMPORTANT: Run this ONLY AFTER confirming NULL values exist via diagnostic script
--
-- How to execute:
-- Option 1: Azure Query Editor (recommended for staging)
-- Option 2: psql $LANKACONNECT_CONNECTION_STRING -f scripts/fix-badge-nulls-manual.sql
-- Option 3: pgAdmin or any PostgreSQL client

BEGIN;

-- Display current state BEFORE fix
SELECT
    'BEFORE FIX' as status,
    COUNT(*) as total_badges,
    COUNT(CASE WHEN position_x_listing IS NULL OR position_y_listing IS NULL OR
                     size_width_listing IS NULL OR size_height_listing IS NULL OR
                     rotation_listing IS NULL THEN 1 END) as listing_nulls,
    COUNT(CASE WHEN position_x_featured IS NULL OR position_y_featured IS NULL OR
                     size_width_featured IS NULL OR size_height_featured IS NULL OR
                     rotation_featured IS NULL THEN 1 END) as featured_nulls,
    COUNT(CASE WHEN position_x_detail IS NULL OR position_y_detail IS NULL OR
                     size_width_detail IS NULL OR size_height_detail IS NULL OR
                     rotation_detail IS NULL THEN 1 END) as detail_nulls
FROM badges.badges;

-- Update ALL badges with COALESCE to set default values
-- This is the EXACT same logic as migration 20251217205258
UPDATE badges.badges
SET
    -- Listing config (Events Listing page - 26% size)
    position_x_listing = COALESCE(position_x_listing, 1.0),
    position_y_listing = COALESCE(position_y_listing, 0.0),
    size_width_listing = COALESCE(size_width_listing, 0.26),
    size_height_listing = COALESCE(size_height_listing, 0.26),
    rotation_listing = COALESCE(rotation_listing, 0.0),

    -- Featured config (Home page banner - 26% size)
    position_x_featured = COALESCE(position_x_featured, 1.0),
    position_y_featured = COALESCE(position_y_featured, 0.0),
    size_width_featured = COALESCE(size_width_featured, 0.26),
    size_height_featured = COALESCE(size_height_featured, 0.26),
    rotation_featured = COALESCE(rotation_featured, 0.0),

    -- Detail config (Event Detail Hero - 21% size for larger images)
    position_x_detail = COALESCE(position_x_detail, 1.0),
    position_y_detail = COALESCE(position_y_detail, 0.0),
    size_width_detail = COALESCE(size_width_detail, 0.21),
    size_height_detail = COALESCE(size_height_detail, 0.21),
    rotation_detail = COALESCE(rotation_detail, 0.0);

-- Display number of rows updated
SELECT 'Rows updated: ' || COUNT(*) as update_result
FROM badges.badges;

-- Display current state AFTER fix
SELECT
    'AFTER FIX' as status,
    COUNT(*) as total_badges,
    COUNT(CASE WHEN position_x_listing IS NULL OR position_y_listing IS NULL OR
                     size_width_listing IS NULL OR size_height_listing IS NULL OR
                     rotation_listing IS NULL THEN 1 END) as listing_nulls,
    COUNT(CASE WHEN position_x_featured IS NULL OR position_y_featured IS NULL OR
                     size_width_featured IS NULL OR size_height_featured IS NULL OR
                     rotation_featured IS NULL THEN 1 END) as featured_nulls,
    COUNT(CASE WHEN position_x_detail IS NULL OR position_y_detail IS NULL OR
                     size_width_detail IS NULL OR size_height_detail IS NULL OR
                     rotation_detail IS NULL THEN 1 END) as detail_nulls
FROM badges.badges;

-- Verify NO NULL values remain
SELECT
    id,
    name,
    CASE
        WHEN position_x_listing IS NULL THEN 'position_x_listing IS NULL'
        WHEN position_y_listing IS NULL THEN 'position_y_listing IS NULL'
        WHEN size_width_listing IS NULL THEN 'size_width_listing IS NULL'
        WHEN size_height_listing IS NULL THEN 'size_height_listing IS NULL'
        WHEN rotation_listing IS NULL THEN 'rotation_listing IS NULL'
        WHEN position_x_featured IS NULL THEN 'position_x_featured IS NULL'
        WHEN position_y_featured IS NULL THEN 'position_y_featured IS NULL'
        WHEN size_width_featured IS NULL THEN 'size_width_featured IS NULL'
        WHEN size_height_featured IS NULL THEN 'size_height_featured IS NULL'
        WHEN rotation_featured IS NULL THEN 'rotation_featured IS NULL'
        WHEN position_x_detail IS NULL THEN 'position_x_detail IS NULL'
        WHEN position_y_detail IS NULL THEN 'position_y_detail IS NULL'
        WHEN size_width_detail IS NULL THEN 'size_width_detail IS NULL'
        WHEN size_height_detail IS NULL THEN 'size_height_detail IS NULL'
        WHEN rotation_detail IS NULL THEN 'rotation_detail IS NULL'
        ELSE 'ALL_VALID'
    END as validation_status
FROM badges.badges
WHERE
    position_x_listing IS NULL OR
    position_y_listing IS NULL OR
    size_width_listing IS NULL OR
    size_height_listing IS NULL OR
    rotation_listing IS NULL OR
    position_x_featured IS NULL OR
    position_y_featured IS NULL OR
    size_width_featured IS NULL OR
    size_height_featured IS NULL OR
    rotation_featured IS NULL OR
    position_x_detail IS NULL OR
    position_y_detail IS NULL OR
    size_width_detail IS NULL OR
    size_height_detail IS NULL OR
    rotation_detail IS NULL;

-- If no rows returned above, all NULLs are fixed
-- If any rows returned, something is wrong - DO NOT COMMIT

-- IMPORTANT: Review results above before committing
-- If validation_status shows ALL_VALID (no rows), run COMMIT
-- If any NULL values remain, run ROLLBACK and investigate

-- Uncomment ONE of these after reviewing results:
-- COMMIT;
-- ROLLBACK;
