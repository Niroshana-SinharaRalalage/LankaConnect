-- Phase 6A.32: FINAL FIX for badge configuration zeros
-- This directly sets the correct values without relying on COALESCE
-- which doesn't work when values are 0 instead of NULL

BEGIN;

-- Update ALL badges with CORRECT default values
-- NOT using COALESCE because values are 0, not NULL
UPDATE badges.badges
SET
    -- Listing config: TopRight with 26% size
    position_x_listing = 1.0,
    position_y_listing = 0.0,
    size_width_listing = 0.26,
    size_height_listing = 0.26,
    rotation_listing = 0.0,

    -- Featured config: TopRight with 26% size
    position_x_featured = 1.0,
    position_y_featured = 0.0,
    size_width_featured = 0.26,
    size_height_featured = 0.26,
    rotation_featured = 0.0,

    -- Detail config: TopRight with 21% size (smaller for large images)
    position_x_detail = 1.0,
    position_y_detail = 0.0,
    size_width_detail = 0.21,
    size_height_detail = 0.21,
    rotation_detail = 0.0,

    updated_at = NOW()
WHERE
    -- Only update badges with incorrect zero values
    (position_x_listing = 0 OR size_width_listing = 0);

-- Verify the update
SELECT
    COUNT(*) as total_updated,
    COUNT(CASE WHEN position_x_listing = 1.0 THEN 1 END) as correct_x,
    COUNT(CASE WHEN size_width_listing = 0.26 THEN 1 END) as correct_width_listing,
    COUNT(CASE WHEN size_width_detail = 0.21 THEN 1 END) as correct_width_detail
FROM badges.badges;

COMMIT;
