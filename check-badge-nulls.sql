-- Check for NULL values in badge location config columns
SELECT
    id,
    name,
    position_x_listing,
    position_y_listing,
    size_width_listing,
    size_height_listing,
    rotation_listing,
    position_x_featured,
    position_y_featured,
    size_width_featured,
    size_height_featured,
    rotation_featured,
    position_x_detail,
    position_y_detail,
    size_width_detail,
    size_height_detail,
    rotation_detail,
    created_at
FROM badges.badges
WHERE
    position_x_listing IS NULL OR position_y_listing IS NULL OR
    size_width_listing IS NULL OR size_height_listing IS NULL OR rotation_listing IS NULL OR
    position_x_featured IS NULL OR position_y_featured IS NULL OR
    size_width_featured IS NULL OR size_height_featured IS NULL OR rotation_featured IS NULL OR
    position_x_detail IS NULL OR position_y_detail IS NULL OR
    size_width_detail IS NULL OR size_height_detail IS NULL OR rotation_detail IS NULL
ORDER BY created_at DESC;
