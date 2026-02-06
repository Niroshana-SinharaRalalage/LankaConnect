import psycopg2
import sys

conn_string = "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser password=1qaz!QAZ sslmode=require"

try:
    conn = psycopg2.connect(conn_string)
    cur = conn.cursor()
    
    print("=" * 80)
    print("QUERY 1: Event 23af624e-7f14-4a0f-8223-06026b547a28 Details")
    print("=" * 80)
    
    cur.execute("""
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
        WHERE id = '23af624e-7f14-4a0f-8223-06026b547a28'::uuid
    """)
    
    row = cur.fetchone()
    if row:
        print(f"Event ID: {row[0]}")
        print(f"Title: {row[1]}")
        print(f"Status: {row[2]}")
        print(f"Address Line 1: {row[3]}")
        print(f"City: {row[4]}")
        print(f"State: {row[5]}")
        print(f"✓✓✓ Latitude: {row[6]}")
        print(f"✓✓✓ Longitude: {row[7]}")
        print(f"Is Cancelled: {row[8]}")
        print(f"Cancellation Reason: {row[9]}")
    else:
        print("Event not found!")
    
    print("\n" + "=" * 80)
    print("QUERY 2: varunipw@gmail.com Newsletter Subscriptions")
    print("=" * 80)
    
    cur.execute("""
        SELECT 
            ns.id,
            ns.email,
            ns.is_confirmed,
            ns.subscribes_to_all_locations,
            ma.name as metro_name,
            ma.state as metro_state,
            ma.center_latitude,
            ma.center_longitude,
            ma.radius_miles,
            ma.is_active
        FROM communications.newsletter_subscribers ns
        LEFT JOIN communications.newsletter_subscriber_metro_areas nsma ON ns.id = nsma.subscriber_id
        LEFT JOIN events.metro_areas ma ON nsma.metro_area_id = ma.id
        WHERE LOWER(ns.email) = 'varunipw@gmail.com'
        ORDER BY ma.name
    """)
    
    rows = cur.fetchall()
    if rows:
        for row in rows:
            print(f"\n✓ Subscriber ID: {row[0]}")
            print(f"  Email: {row[1]}")
            print(f"  Is Confirmed: {row[2]}")
            print(f"  Subscribes to All Locations: {row[3]}")
            print(f"  Metro Name: {row[4]}")
            print(f"  Metro State: {row[5]}")
            print(f"  Metro Latitude: {row[6]}")
            print(f"  Metro Longitude: {row[7]}")
            print(f"  Metro Radius (miles): {row[8]}")
            print(f"  Metro Is Active: {row[9]}")
    else:
        print("No subscriptions found!")
    
    print("\n" + "=" * 80)
    print("QUERY 3: All Ohio Metro Areas")
    print("=" * 80)
    
    cur.execute("""
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
        ORDER BY name
    """)
    
    rows = cur.fetchall()
    print(f"\nFound {len(rows)} Ohio metro areas:")
    for row in rows:
        print(f"\n  ✓ {row[1]}, {row[2]}")
        print(f"      ID: {row[0]}")
        print(f"      Center: ({row[3]}, {row[4]})")
        print(f"      Radius: {row[5]} miles")
        print(f"      Active: {row[6]}")
    
    # Calculate distance from Aurora to Cleveland
    print("\n" + "=" * 80)
    print("CALCULATION: Distance from Aurora to Cleveland Metro")
    print("=" * 80)
    
    cur.execute("""
        SELECT 
            id,
            name,
            center_latitude,
            center_longitude,
            radius_miles
        FROM events.metro_areas
        WHERE LOWER(name) = 'cleveland' AND LOWER(state) = 'oh'
    """)
    
    cleveland = cur.fetchone()
    if cleveland and row[6] and row[7]:
        # Haversine formula
        import math
        
        aurora_lat = float(row[6]) if row[6] else 41.3173
        aurora_lon = float(row[7]) if row[7] else -81.3460
        cleve_lat = float(cleveland[2])
        cleve_lon = float(cleveland[3])
        
        lat1_rad = math.radians(aurora_lat)
        lon1_rad = math.radians(aurora_lon)
        lat2_rad = math.radians(cleve_lat)
        lon2_rad = math.radians(cleve_lon)
        
        dlat = lat2_rad - lat1_rad
        dlon = lon2_rad - lon1_rad
        
        a = math.sin(dlat/2)**2 + math.cos(lat1_rad) * math.cos(lat2_rad) * math.sin(dlon/2)**2
        c = 2 * math.atan2(math.sqrt(a), math.sqrt(1-a))
        
        distance_km = 6371.0 * c
        distance_miles = distance_km / 1.60934
        
        print(f"Aurora coordinates: ({aurora_lat}, {aurora_lon})")
        print(f"Cleveland metro center: ({cleve_lat}, {cleve_lon})")
        print(f"Cleveland radius: {cleveland[4]} miles")
        print(f"✓✓✓ Calculated distance: {distance_km:.2f} km ({distance_miles:.2f} miles)")
        print(f"✓✓✓ Within radius? {distance_miles <= cleveland[4]}")
    
    cur.close()
    conn.close()
    
except Exception as e:
    print(f"ERROR: {e}", file=sys.stderr)
    import traceback
    traceback.print_exc()
    sys.exit(1)
