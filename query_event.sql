-- Check if event 23af624e-7f14-4a0f-8223-06026b547a28 has coordinates
SELECT 
    id,
    title,
    status,
    location_address_line1,
    location_address_city,
    location_address_state,
    location_coordinates_latitude,
    location_coordinates_longitude,
    is_cancelled,
    cancellation_reason
FROM events.events
WHERE id = '23af624e-7f14-4a0f-8223-06026b547a28';
