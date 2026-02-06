-- Phase 6A.32: Diagnostic query to verify actual database values vs API output
-- This query shows both the raw database values and what should be returned

SELECT
    b.id,
    b.name,

    -- Listing config (expected: x=1.0, y=0.0, size=0.26)
    b.position_x_listing,
    b.position_y_listing,
    b.size_width_listing,
    b.size_height_listing,
    b.rotation_listing,

    -- Featured config (expected: x=1.0, y=0.0, size=0.26)
    b.position_x_featured,
    b.position_y_featured,
    b.size_width_featured,
    b.size_height_featured,
    b.rotation_featured,

    -- Detail config (expected: x=1.0, y=0.0, size=0.21)
    b.position_x_detail,
    b.position_y_detail,
    b.size_width_detail,
    b.size_height_detail,
    b.rotation_detail,

    -- Check if values are default or zero
    CASE
        WHEN b.position_x_listing = 0 AND b.size_width_listing = 0 THEN 'ZERO (BROKEN)'
        WHEN b.position_x_listing = 1.0 AND b.size_width_listing = 0.26 THEN 'CORRECT DEFAULTS'
        ELSE 'CUSTOM VALUES'
    END as listing_status,

    b.is_active,
    b.is_system,
    b.created_at
FROM badges.badges b
ORDER BY b.display_order, b.name;

-- Count of badges with zero values
SELECT
    COUNT(*) as total_badges,
    SUM(CASE WHEN position_x_listing = 0 THEN 1 ELSE 0 END) as zero_x_listing,
    SUM(CASE WHEN size_width_listing = 0 THEN 1 ELSE 0 END) as zero_width_listing,
    SUM(CASE WHEN position_x_listing = 1.0 AND size_width_listing = 0.26 THEN 1 ELSE 0 END) as correct_listing
FROM badges.badges;
