-- Check if event 23af624e-7f14-4a0f-8223-06026b547a28 has coordinates
SELECT 
    id,
    title,
    status,
    location_city,
    location_state,
    location_latitude,
    location_longitude,
    is_cancelled,
    cancellation_reason
FROM events.events
WHERE id = '23af624e-7f14-4a0f-8223-06026b547a28';
