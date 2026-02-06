-- Get all Ohio metro areas with coordinates
SELECT 
    id,
    name,
    state,
    center_latitude,
    center_longitude,
    radius_miles,
    is_active
FROM events.metro_areas
WHERE LOWER(state) = 'oh' OR LOWER(state) = 'ohio'
ORDER BY name;
