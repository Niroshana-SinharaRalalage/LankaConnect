-- Add Akron metro area to events.metro_areas table
-- This fixes the location filter bug where Akron wasn't in the metro areas list

INSERT INTO events.metro_areas (
    id,
    name,
    state,
    center_latitude,
    center_longitude,
    radius_miles,
    is_state_level_area,
    is_active,
    created_at,
    updated_at
)
VALUES (
    '39111111-1111-1111-1111-111111111005'::uuid,
    'Akron',
    'OH',
    41.0823,
    -81.5178,
    25,
    false,
    true,
    NOW(),
    NOW()
)
ON CONFLICT (id) DO NOTHING;
