#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Phase 6A.85: Test newsletter creation API with targetAllLocations=true
Verify that all 84 metro areas are populated in the junction table
"""

import requests
import psycopg2
import sys
import io
from datetime import datetime

if sys.stdout.encoding != 'utf-8':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

API_BASE = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
DB_CONN = "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser password=1qaz!QAZ sslmode=require"

print("=" * 100)
print("PHASE 6A.85 API TESTING - Newsletter 'All Locations' Fix Verification")
print("=" * 100)
print(f"Test Time: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n")

# Step 1: Login and get token
print("Step 1: Authenticating...")
try:
    login_response = requests.post(
        f"{API_BASE}/api/Auth/login",
        json={
            "email": "niroshhh@gmail.com",
            "password": "12!@qwASzx",
            "rememberMe": True,
            "ipAddress": "string"
        },
        headers={"Content-Type": "application/json"}
    )
    login_response.raise_for_status()
    token = login_response.json()["accessToken"]
    print(f"✅ Authentication successful")
    print(f"   Token: {token[:50]}...\n")
except Exception as e:
    print(f"❌ Login failed: {e}")
    sys.exit(1)

# Step 2: Create test newsletter with targetAllLocations=true
print("Step 2: Creating test newsletter with targetAllLocations=true...")
newsletter_payload = {
    "title": f"Phase 6A.85 Test - {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}",
    "description": "Testing fix for all locations bug. Should populate 84 metro areas automatically.",
    "emailGroupIds": [],
    "includeNewsletterSubscribers": True,
    "targetAllLocations": True,
    "metroAreaIds": None,
    "eventId": None,
    "isAnnouncementOnly": False
}

try:
    create_response = requests.post(
        f"{API_BASE}/api/newsletters",
        json=newsletter_payload,
        headers={
            "Authorization": f"Bearer {token}",
            "Content-Type": "application/json"
        }
    )
    create_response.raise_for_status()
    newsletter_id = create_response.json()
    print(f"✅ Newsletter created successfully")
    print(f"   Newsletter ID: {newsletter_id}\n")
except Exception as e:
    print(f"❌ Newsletter creation failed: {e}")
    print(f"   Status code: {create_response.status_code}")
    print(f"   Response: {create_response.text}")
    sys.exit(1)

# Step 3: Verify database - check junction table
print("Step 3: Verifying database - checking junction table...")
try:
    conn = psycopg2.connect(DB_CONN)
    cur = conn.cursor()

    # Check newsletter metro areas
    cur.execute("""
        SELECT COUNT(*)
        FROM communications.newsletter_metro_areas
        WHERE newsletter_id = %s::uuid
    """, (newsletter_id,))

    metro_count = cur.fetchone()[0]

    print(f"   Newsletter ID: {newsletter_id}")
    print(f"   Metro areas in junction table: {metro_count}")
    print(f"   Expected: 84 (all active metros)")

    if metro_count == 84:
        print(f"   ✅ SUCCESS - All 84 metros populated correctly!\n")
    elif metro_count == 0:
        print(f"   ❌ FAILED - 0 metros! Bug NOT fixed!\n")
    else:
        print(f"   ⚠️  PARTIAL - Only {metro_count}/84 metros populated\n")

    # Get sample metro IDs
    cur.execute("""
        SELECT ma.name, ma.state
        FROM communications.newsletter_metro_areas nma
        JOIN events.metro_areas ma ON nma.metro_area_id = ma.id
        WHERE nma.newsletter_id = %s::uuid
        LIMIT 5
    """, (newsletter_id,))

    sample_metros = cur.fetchall()
    print("   Sample metros populated:")
    for metro in sample_metros:
        print(f"     - {metro[0]}, {metro[1]}")

    # Verify newsletter settings
    cur.execute("""
        SELECT target_all_locations, include_newsletter_subscribers
        FROM communications.newsletters
        WHERE id = %s::uuid
    """, (newsletter_id,))

    settings = cur.fetchone()
    print(f"\n   Newsletter settings:")
    print(f"     target_all_locations: {settings[0]}")
    print(f"     include_newsletter_subscribers: {settings[1]}")

    cur.close()
    conn.close()

    print("\n" + "=" * 100)
    print("VERIFICATION RESULT")
    print("=" * 100)

    if metro_count == 84:
        print("\n✅ PHASE 6A.85 FIX VERIFIED SUCCESSFULLY!")
        print("   - Newsletter created with targetAllLocations=true")
        print("   - All 84 active metro areas populated in junction table")
        print("   - Matching logic will now work correctly")
        print("   - Recipient resolution will include all subscribers")
        print("\n🎯 NEXT STEP: Run backfill script for 16 broken newsletters")
        sys.exit(0)
    else:
        print("\n❌ PHASE 6A.85 FIX NOT WORKING!")
        print(f"   - Expected 84 metros, got {metro_count}")
        print("   - Need to check Azure container logs")
        print("   - May need to verify deployment completed")
        sys.exit(1)

except Exception as e:
    print(f"❌ Database verification failed: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)
