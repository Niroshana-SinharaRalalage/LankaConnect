-- Check varunipw@gmail.com's newsletter subscriptions
SELECT 
    ns.id,
    ns.email,
    ns.is_confirmed,
    ns.subscribes_to_all_ohio_metros,
    ns.subscribes_to_all_locations,
    ma.name as metro_name,
    ma.state as metro_state,
    ma.center_latitude,
    ma.center_longitude,
    ma.radius_miles
FROM communications.newsletter_subscribers ns
LEFT JOIN communications.newsletter_subscriber_metro_areas nsma ON ns.id = nsma.subscriber_id
LEFT JOIN events.metro_areas ma ON nsma.metro_area_id = ma.id
WHERE LOWER(ns.email) = 'varunipw@gmail.com'
ORDER BY ma.name;
