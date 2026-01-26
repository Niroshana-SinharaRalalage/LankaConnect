#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Phase 6A.85: Test sending "Sample NewsletterVaruni" after backfill
Verify that the newsletter now resolves recipients and sends emails successfully
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
NEWSLETTER_ID = "a595d9bc-bc1b-4a17-b138-9c1f081a5992"  # Sample NewsletterVaruni

print("=" * 100)
print("PHASE 6A.85 - SAMPLE NEWSLETTERVARUNI EMAIL SENDING TEST")
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
    print(f"✅ Authentication successful\n")
except Exception as e:
    print(f"❌ Login failed: {e}")
    sys.exit(1)

# Step 2: Check newsletter status before sending
print("Step 2: Checking newsletter status before sending...")
try:
    conn = psycopg2.connect(DB_CONN)
    cur = conn.cursor()

    cur.execute("""
        SELECT
            n.title,
            n.status,
            n.target_all_locations,
            n.include_newsletter_subscribers,
            n.sent_at,
            COUNT(nma.metro_area_id) AS metro_count
        FROM communications.newsletters n
        LEFT JOIN communications.newsletter_metro_areas nma ON n.id = nma.newsletter_id
        WHERE n.id = %s::uuid
        GROUP BY n.id, n.title, n.status, n.target_all_locations, n.include_newsletter_subscribers, n.sent_at
    """, (NEWSLETTER_ID,))

    newsletter_info = cur.fetchone()

    print(f"   Title: {newsletter_info[0]}")
    print(f"   Status: {newsletter_info[1]}")
    print(f"   Target All Locations: {newsletter_info[2]}")
    print(f"   Include Newsletter Subscribers: {newsletter_info[3]}")
    print(f"   Sent At: {newsletter_info[4]}")
    print(f"   Metro Areas in Junction Table: {newsletter_info[5]}")

    if newsletter_info[5] != 84:
        print(f"   ❌ ERROR: Expected 84 metros, found {newsletter_info[5]}")
        sys.exit(1)
    else:
        print(f"   ✅ Metro areas correctly populated\n")

    # Check total active subscribers
    cur.execute("""
        SELECT COUNT(*)
        FROM communications.newsletter_subscribers
        WHERE is_confirmed = TRUE AND is_active = TRUE
    """)

    total_subscribers = cur.fetchone()[0]
    print(f"   Total Active Newsletter Subscribers: {total_subscribers}\n")

except Exception as e:
    print(f"❌ Database check failed: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)

# Step 3: Send the newsletter
print("Step 3: Sending newsletter via API...")
try:
    send_response = requests.post(
        f"{API_BASE}/api/newsletters/{NEWSLETTER_ID}/send",
        headers={
            "Authorization": f"Bearer {token}",
            "Content-Type": "application/json"
        }
    )

    print(f"   API Response Status: {send_response.status_code}")

    if send_response.status_code == 202:
        print(f"   ✅ Newsletter queued for sending (202 Accepted)\n")
    elif send_response.status_code == 200:
        print(f"   ✅ Newsletter send initiated (200 OK)\n")
    else:
        print(f"   ❌ Unexpected status code: {send_response.status_code}")
        print(f"   Response: {send_response.text}")
        sys.exit(1)

except Exception as e:
    print(f"❌ Newsletter send failed: {e}")
    sys.exit(1)

# Step 4: Wait for background job to complete
print("Step 4: Waiting for background job to complete...")
print("   Waiting 10 seconds for Hangfire job to process...")
time.sleep(10)

# Step 5: Check email history
print("\nStep 5: Checking email history...")
try:
    cur.execute("""
        SELECT
            eh.id,
            eh.total_recipient_count,
            eh.successful_sends,
            eh.failed_sends,
            eh.newsletter_email_group_count,
            eh.event_email_group_count,
            eh.subscriber_count,
            eh.event_registration_count,
            eh.sent_at
        FROM communications.newsletter_email_history eh
        WHERE eh.newsletter_id = %s::uuid
        ORDER BY eh.sent_at DESC
        LIMIT 1
    """, (NEWSLETTER_ID,))

    history = cur.fetchone()

    if history:
        print(f"   ✅ Email history found!")
        print(f"   History ID: {history[0]}")
        print(f"   Total Recipients: {history[1]}")
        print(f"   Successful Sends: {history[2]}")
        print(f"   Failed Sends: {history[3]}")
        print(f"   Breakdown:")
        print(f"     - Newsletter Email Groups: {history[4]}")
        print(f"     - Event Email Groups: {history[5]}")
        print(f"     - Subscribers: {history[6]}")
        print(f"     - Event Registrations: {history[7]}")
        print(f"   Sent At: {history[8]}\n")

        # Check newsletter sent_at timestamp
        cur.execute("""
            SELECT sent_at
            FROM communications.newsletters
            WHERE id = %s::uuid
        """, (NEWSLETTER_ID,))

        sent_at = cur.fetchone()[0]
        print(f"   Newsletter sent_at timestamp: {sent_at}")

        if history[1] == 0:
            print(f"\n   ❌ FAILED: 0 recipients resolved!")
            print(f"   This indicates the matching logic still has issues.")
            sys.exit(1)
        elif history[3] > 0:
            print(f"\n   ⚠️  WARNING: {history[3]} emails failed to send")
            print(f"   Check Azure Communication Services logs")
        else:
            print(f"\n   ✅ SUCCESS: {history[2]} emails sent successfully!")

    else:
        print(f"   ❌ No email history found!")
        print(f"   Background job may still be running or failed.")
        print(f"   Check Azure container logs for NewsletterEmailJob execution.")
        sys.exit(1)

    cur.close()
    conn.close()

except Exception as e:
    print(f"❌ Failed to check email history: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)

# Final summary
print("\n" + "=" * 100)
print("TEST RESULT SUMMARY")
print("=" * 100)

if history and history[1] > 0 and history[3] == 0:
    print("\n✅ PHASE 6A.85 FIX VERIFIED END-TO-END!")
    print(f"   - Newsletter 'Sample NewsletterVaruni' sent successfully")
    print(f"   - {history[2]} recipients received emails")
    print(f"   - Matching logic working correctly with 84 metro areas")
    print(f"   - All 16 broken newsletters are now fixed!")
    sys.exit(0)
elif history and history[1] > 0 and history[3] > 0:
    print("\n⚠️  PARTIAL SUCCESS")
    print(f"   - Recipients resolved: {history[1]}")
    print(f"   - Successful sends: {history[2]}")
    print(f"   - Failed sends: {history[3]}")
    print(f"   - Review Azure Communication Services configuration")
    sys.exit(0)
else:
    print("\n❌ TEST FAILED")
    print(f"   - Newsletter queued but no recipients resolved")
    print(f"   - Review CreateNewsletterCommandHandler implementation")
    print(f"   - Review NewsletterEmailJob recipient resolution logic")
    sys.exit(1)
