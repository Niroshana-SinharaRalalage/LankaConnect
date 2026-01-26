#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Phase 6A.85: End-to-end test - Create new newsletter and send
Verify complete flow: Create → Verify metros → Send → Check history
"""

import requests
import psycopg2
import sys
import io
from datetime import datetime
import time

if sys.stdout.encoding != 'utf-8':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

API_BASE = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
DB_CONN = "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser password=1qaz!QAZ sslmode=require"

print("=" * 100)
print("PHASE 6A.85 - END-TO-END EMAIL SENDING TEST")
print("=" * 100)
print(f"Test Time: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n")

# Step 1: Login
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
    print(f"✅ Authenticated\n")
except Exception as e:
    print(f"❌ Login failed: {e}")
    sys.exit(1)

# Step 2: Create newsletter with targetAllLocations=true
print("Step 2: Creating new newsletter with targetAllLocations=true...")
newsletter_payload = {
    "title": f"E2E Test - {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}",
    "description": "End-to-end test of Phase 6A.85 fix. Testing: Create → Send → Verify email delivery.",
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
    print(f"✅ Newsletter created: {newsletter_id}\n")
except Exception as e:
    print(f"❌ Newsletter creation failed: {e}")
    sys.exit(1)

# Step 3: Verify metro areas populated
print("Step 3: Verifying metro areas populated...")
try:
    conn = psycopg2.connect(DB_CONN)
    cur = conn.cursor()

    cur.execute("""
        SELECT COUNT(*)
        FROM communications.newsletter_metro_areas
        WHERE newsletter_id = %s::uuid
    """, (newsletter_id,))

    metro_count = cur.fetchone()[0]
    print(f"   Metro areas in junction table: {metro_count}")

    if metro_count != 84:
        print(f"   ❌ Expected 84, got {metro_count} - Fix NOT working!")
        sys.exit(1)
    else:
        print(f"   ✅ All 84 metros populated correctly\n")

except Exception as e:
    print(f"❌ Database verification failed: {e}")
    sys.exit(1)

# Step 4: Activate newsletter (change status from Draft to Active)
print("Step 4: Activating newsletter...")
try:
    activate_response = requests.post(
        f"{API_BASE}/api/newsletters/{newsletter_id}/publish",
        headers={
            "Authorization": f"Bearer {token}",
            "Content-Type": "application/json"
        }
    )

    if activate_response.status_code in [200, 204]:
        print(f"   ✅ Newsletter activated\n")
    else:
        print(f"   Note: Activation returned {activate_response.status_code}")
        print(f"   Attempting to send anyway...\n")

except Exception as e:
    print(f"   Note: Activation endpoint may not exist: {e}")
    print(f"   Attempting to send anyway...\n")

# Step 5: Send newsletter
print("Step 5: Sending newsletter...")
try:
    send_response = requests.post(
        f"{API_BASE}/api/newsletters/{newsletter_id}/send",
        headers={
            "Authorization": f"Bearer {token}",
            "Content-Type": "application/json"
        }
    )

    print(f"   API Response: {send_response.status_code}")

    if send_response.status_code == 202:
        print(f"   ✅ Newsletter queued for background sending\n")
    elif send_response.status_code == 400:
        print(f"   ❌ Bad Request: {send_response.text}")
        sys.exit(1)
    else:
        print(f"   Response: {send_response.text}\n")

except Exception as e:
    print(f"❌ Send failed: {e}")
    sys.exit(1)

# Step 6: Wait and check email history
print("Step 6: Waiting for background job (30 seconds)...")
for i in range(6):
    time.sleep(5)
    print(f"   ... {(i+1)*5} seconds elapsed")

print("\nStep 7: Checking email history...")
try:
    cur.execute("""
        SELECT
            eh.total_recipient_count,
            eh.successful_sends,
            eh.failed_sends,
            eh.subscriber_count,
            eh.sent_at
        FROM communications.newsletter_email_history eh
        WHERE eh.newsletter_id = %s::uuid
        ORDER BY eh.sent_at DESC
        LIMIT 1
    """, (newsletter_id,))

    history = cur.fetchone()

    if history:
        print(f"   ✅ Email history found!")
        print(f"   Total Recipients: {history[0]}")
        print(f"   Successful: {history[1]}")
        print(f"   Failed: {history[2]}")
        print(f"   Subscribers: {history[3]}")
        print(f"   Sent At: {history[4]}\n")

        success = history[0] > 0 and history[2] == 0
    else:
        print(f"   ❌ No email history - job may have failed")
        print(f"   Check Azure container logs for NewsletterEmailJob")
        success = False

    # Check newsletter sent_at
    cur.execute("""
        SELECT sent_at, status
        FROM communications.newsletters
        WHERE id = %s::uuid
    """, (newsletter_id,))

    nl = cur.fetchone()
    print(f"   Newsletter sent_at: {nl[0]}")
    print(f"   Newsletter status: {nl[1]}")

    cur.close()
    conn.close()

except Exception as e:
    print(f"❌ Failed to check history: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)

# Final result
print("\n" + "=" * 100)
print("TEST RESULT")
print("=" * 100)

if success:
    print("\n✅ PHASE 6A.85 END-TO-END TEST PASSED!")
    print(f"   - Newsletter created with 84 metros")
    print(f"   - Email sent successfully to {history[0]} recipients")
    print(f"   - All components working correctly")
    print(f"\n🎯 Phase 6A.85 fix is COMPLETE and WORKING!")
    sys.exit(0)
else:
    print("\n❌ TEST FAILED")
    print(f"   - Newsletter created and metros populated correctly")
    print(f"   - But email sending failed or no recipients resolved")
    print(f"   - Check Azure logs for job execution details")
    sys.exit(1)
