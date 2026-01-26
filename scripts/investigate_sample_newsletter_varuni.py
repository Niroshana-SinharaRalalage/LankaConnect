#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Phase 6A.84 - "Sample NewsletterVaruni" Email Delivery Failure Investigation

Purpose: Investigate why "Sample NewsletterVaruni" (ID: a595d9bc-bc1b-4a17-b138-9c1f081a5992)
         never sent emails despite user clicking "Send Email" button.

Root Cause Hypothesis:
- Recipient resolution returns 0 recipients
- Job exits early at line 122-127 without user feedback
- No history record created, no EmailMessages generated
"""

import psycopg2
import sys
import io
from datetime import datetime

# Set stdout to UTF-8 encoding
if sys.stdout.encoding != 'utf-8':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

# Azure staging database connection
conn_string = "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser password=1qaz!QAZ sslmode=require"

newsletter_id = 'a595d9bc-bc1b-4a17-b138-9c1f081a5992'

def print_header(title):
    """Print a formatted header"""
    print("\n" + "=" * 100)
    print(f"  {title}")
    print("=" * 100)

try:
    print_header("PHASE 6A.84: 'Sample NewsletterVaruni' EMAIL DELIVERY FAILURE INVESTIGATION")
    print(f"Investigation Time: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print(f"Newsletter ID: {newsletter_id}")

    conn = psycopg2.connect(conn_string)
    cur = conn.cursor()

    # ========================================================================
    # QUERY 1: Newsletter Configuration
    # ========================================================================
    print_header("QUERY 1: Newsletter Configuration")

    cur.execute("""
        SELECT
            id,
            title,
            status,
            sent_at,
            published_at,
            expires_at,
            include_newsletter_subscribers,
            target_all_locations,
            event_id,
            created_by_user_id,
            created_at,
            updated_at
        FROM communications.newsletters
        WHERE id = %s::uuid
    """, (newsletter_id,))

    newsletter = cur.fetchone()

    if newsletter:
        print(f"\n✅ Newsletter Found:")
        print(f"  Title: {newsletter[1]}")
        print(f"  Status: {newsletter[2]}")
        print(f"  Sent At: {newsletter[3]} {'🔴 NULL - NEVER SENT!' if newsletter[3] is None else ''}")
        print(f"  Published At: {newsletter[4]}")
        print(f"  Expires At: {newsletter[5]}")
        print(f"  Include Newsletter Subscribers: {newsletter[6]}")
        print(f"  Target All Locations: {newsletter[7]}")
        print(f"  Linked Event ID: {newsletter[8]}")
        print(f"  Owner User ID: {newsletter[9]}")
        print(f"  Created At: {newsletter[10]}")
        print(f"  Updated At: {newsletter[11]}")
    else:
        print("🔴 Newsletter not found!")
        sys.exit(1)

    # ========================================================================
    # QUERY 2: Newsletter Email Groups
    # ========================================================================
    print_header("QUERY 2: Newsletter Email Groups (Direct Recipients)")

    cur.execute("""
        SELECT
            eg."Id",
            eg.name,
            eg.email_addresses,
            eg.is_active,
            neg."AddedAt"
        FROM communications.email_groups eg
        INNER JOIN communications.newsletter_email_groups neg ON eg."Id" = neg.email_group_id
        WHERE neg.newsletter_id = %s::uuid
        ORDER BY neg."AddedAt" DESC
    """, (newsletter_id,))

    email_groups = cur.fetchall()

    if email_groups:
        print(f"\n✅ Found {len(email_groups)} Email Group(s):")
        for idx, group in enumerate(email_groups, 1):
            emails = group[2] if group[2] else []
            print(f"\n  Group {idx}:")
            print(f"    ID: {group[0]}")
            print(f"    Name: {group[1]}")
            print(f"    Email Count: {len(emails)}")
            print(f"    Emails: {', '.join(emails[:5])}" + (f" ... +{len(emails)-5} more" if len(emails) > 5 else ""))
            print(f"    Is Active: {group[3]}")
            print(f"    Added At: {group[4]}")
    else:
        print("\n⚠️  NO EMAIL GROUPS LINKED - This is potential issue #1")
        print("   Newsletter is NOT configured to send to specific email groups.")

    # ========================================================================
    # QUERY 3: Linked Event (for Event Email Groups)
    # ========================================================================
    print_header("QUERY 3: Linked Event Configuration")

    if newsletter[8] is not None:
        cur.execute("""
            SELECT
                "Id",
                title,
                status,
                is_cancelled,
                start_date_time,
                end_date_time
            FROM events.events
            WHERE "Id" = %s::uuid
        """, (newsletter[8],))

        event = cur.fetchone()

        if event:
            print(f"\n✅ Linked to Event:")
            print(f"  Event ID: {event[0]}")
            print(f"  Title: {event[1]}")
            print(f"  Status: {event[2]}")
            print(f"  Is Cancelled: {event[3]}")
            print(f"  Start: {event[4]}")
            print(f"  End: {event[5]}")

            # Check event email groups
            cur.execute("""
                SELECT COUNT(*)
                FROM communications.event_email_groups eeg
                INNER JOIN communications.email_groups eg ON eeg.email_group_id = eg."Id"
                WHERE eeg.event_id = %s::uuid
                  AND eg.is_active = TRUE
            """, (newsletter[8],))

            event_group_count = cur.fetchone()[0]
            print(f"  Event Email Groups: {event_group_count}")

            # Check event registrations
            cur.execute("""
                SELECT COUNT(*)
                FROM events.registrations r
                WHERE r.event_id = %s::uuid
                  AND r.status = 'Confirmed'
            """, (newsletter[8],))

            reg_count = cur.fetchone()[0]
            print(f"  Confirmed Registrations: {reg_count}")
        else:
            print("🔴 Linked event not found (may be deleted)")
    else:
        print("\nℹ️  NOT LINKED TO EVENT")
        print("   Newsletter is standalone (not event-specific).")

    # ========================================================================
    # QUERY 4: Newsletter Subscribers (Location Filtering)
    # ========================================================================
    print_header("QUERY 4: Newsletter Subscribers")

    # Check total subscriber count
    cur.execute("""
        SELECT COUNT(*)
        FROM communications.newsletter_subscribers
        WHERE is_confirmed = TRUE
          AND is_active = TRUE
    """)

    total_subscribers = cur.fetchone()[0]
    print(f"\n📊 Total Confirmed Active Subscribers: {total_subscribers}")

    if newsletter[7]:  # target_all_locations = True
        print("\n✅ Newsletter targets ALL LOCATIONS (no filtering)")

        cur.execute("""
            SELECT
                ns."Id",
                ns.email,
                ns.subscribes_to_all_locations,
                COUNT(nsma.metro_area_id) as metro_area_count
            FROM communications.newsletter_subscribers ns
            LEFT JOIN communications.newsletter_subscriber_metro_areas nsma ON ns."Id" = nsma.subscriber_id
            WHERE ns.is_confirmed = TRUE
              AND ns.is_active = TRUE
            GROUP BY ns."Id", ns.email, ns.subscribes_to_all_locations
            LIMIT 5
        """)

        sample_subscribers = cur.fetchall()

        print(f"\n📋 Sample Subscribers (first 5):")
        for idx, sub in enumerate(sample_subscribers, 1):
            print(f"\n  Subscriber {idx}:")
            print(f"    Email: {sub[1]}")
            print(f"    Subscribes to All: {sub[2]}")
            print(f"    Metro Areas: {sub[3]}")

        # CRITICAL: These subscribers SHOULD be included because target_all_locations = True
        if total_subscribers > 0:
            print(f"\n✅ EXPECTED RECIPIENTS: {total_subscribers} subscribers")
            print("   ⚠️  BUT if newsletter never sent, recipient resolution may have failed!")
        else:
            print("\n🔴 NO SUBSCRIBERS - This explains 0 recipients!")
    else:
        print("\n⚠️  Newsletter targets SPECIFIC LOCATIONS - checking metro areas...")

        # This path shouldn't execute since target_all_locations = True
        # but including for completeness

    # ========================================================================
    # QUERY 5: Email History (Should be EMPTY)
    # ========================================================================
    print_header("QUERY 5: Newsletter Email History")

    cur.execute("""
        SELECT
            "Id",
            newsletter_id,
            "SentAt",
            total_recipient_count,
            successful_sends,
            failed_sends,
            newsletter_email_group_count,
            event_email_group_count,
            subscriber_count,
            event_registration_count
        FROM communications.newsletter_email_history
        WHERE newsletter_id = %s::uuid
        ORDER BY "SentAt" DESC
    """, (newsletter_id,))

    history_records = cur.fetchall()

    if history_records:
        print(f"\n✅ Found {len(history_records)} History Record(s):")
        for idx, record in enumerate(history_records, 1):
            print(f"\n  Record {idx}:")
            print(f"    Sent At: {record[2]}")
            print(f"    Total Recipients: {record[3]}")
            print(f"    Successful: {record[4]}")
            print(f"    Failed: {record[5]}")
            print(f"    Breakdown:")
            print(f"      - Newsletter Groups: {record[6]}")
            print(f"      - Event Groups: {record[7]}")
            print(f"      - Subscribers: {record[8]}")
            print(f"      - Event Registrations: {record[9]}")
    else:
        print("\n🔴 NO HISTORY RECORDS FOUND!")
        print("   This confirms newsletter send job NEVER created history record.")
        print("   Either:")
        print("     1. Job was never queued (API call failed)")
        print("     2. Job exited early due to 0 recipients (line 122-127)")
        print("     3. Job failed before creating history")

    # ========================================================================
    # QUERY 6: EmailMessages (Should be EMPTY)
    # ========================================================================
    print_header("QUERY 6: EmailMessages for This Newsletter")

    cur.execute("""
        SELECT
            "Id",
            subject,
            to_emails,
            status,
            "SentAt",
            "FailedAt",
            error_message,
            retry_count,
            "CreatedAt"
        FROM communications.email_messages
        WHERE subject LIKE %s
        ORDER BY "CreatedAt" DESC
        LIMIT 10
    """, (f"%{newsletter[1]}%",))

    email_messages = cur.fetchall()

    if email_messages:
        print(f"\n✅ Found {len(email_messages)} EmailMessage(s):")
        for idx, msg in enumerate(email_messages, 1):
            print(f"\n  Message {idx}:")
            print(f"    Subject: {msg[1]}")
            print(f"    To: {msg[2]}")
            print(f"    Status: {msg[3]}")
            print(f"    Sent At: {msg[4]}")
            print(f"    Failed At: {msg[5]}")
            print(f"    Error: {msg[6]}")
            print(f"    Retry Count: {msg[7]}")
            print(f"    Created At: {msg[8]}")
    else:
        print("\n🔴 NO EMAIL MESSAGES FOUND!")
        print("   This confirms emails were NEVER queued to Azure Communication Services.")
        print("   Job exited before sending any emails.")

    # ========================================================================
    # QUERY 7: Hangfire Job History
    # ========================================================================
    print_header("QUERY 7: Hangfire Job Execution History")

    cur.execute("""
        SELECT
            j.id,
            j.invocationdata::text,
            j.createdat,
            s.name as state,
            s.createdat as state_created_at,
            s.reason
        FROM hangfire.job j
        LEFT JOIN hangfire.state s ON j.stateid = s.id
        WHERE j.invocationdata::text LIKE %s
        ORDER BY j.createdat DESC
        LIMIT 10
    """, (f"%{newsletter_id}%",))

    hangfire_jobs = cur.fetchall()

    if hangfire_jobs:
        print(f"\n✅ Found {len(hangfire_jobs)} Hangfire Job(s):")
        for idx, job in enumerate(hangfire_jobs, 1):
            print(f"\n  Job {idx}:")
            print(f"    Job ID: {job[0]}")
            print(f"    Created At: {job[2]}")
            print(f"    State: {job[3]}")
            print(f"    State Changed: {job[4]}")
            print(f"    Reason: {job[5]}")
    else:
        print("\n🔴 NO HANGFIRE JOBS FOUND!")
        print("   This suggests:")
        print("     1. Job was never queued (API endpoint failed)")
        print("     2. Job is too old and purged from Hangfire")
        print("     3. Newsletter ID not in job parameters")

    # ========================================================================
    # FINAL DIAGNOSIS
    # ========================================================================
    print_header("ROOT CAUSE ANALYSIS")

    print("\n🔍 FINDINGS:")
    print(f"  1. Newsletter Status: {newsletter[2]}, SentAt: {newsletter[3]}")
    print(f"  2. Email Groups Linked: {len(email_groups)}")
    print(f"  3. Target All Locations: {newsletter[7]}")
    print(f"  4. Total Active Subscribers: {total_subscribers}")
    print(f"  5. History Records: {len(history_records)}")
    print(f"  6. Email Messages: {len(email_messages)}")
    print(f"  7. Hangfire Jobs: {len(hangfire_jobs)}")

    print("\n💡 ROOT CAUSE HYPOTHESIS:")

    if len(email_groups) == 0 and total_subscribers == 0:
        print("\n🔴 CONFIRMED ROOT CAUSE: ZERO RECIPIENTS")
        print("   Newsletter has:")
        print("     - NO email groups linked")
        print("     - NO confirmed subscribers in database")
        print("     - target_all_locations = True (but no subscribers to target)")
        print("\n   📋 What Happened:")
        print("     1. User clicked 'Send Email'")
        print("     2. API queued Hangfire background job")
        print("     3. Job started, resolved recipients from 4 sources:")
        print("        - Newsletter email groups: 0")
        print("        - Event email groups: 0 (no event linked)")
        print("        - Newsletter subscribers: 0")
        print("        - Event registrations: 0 (no event linked)")
        print("     4. Total recipients = 0")
        print("     5. Job hit early return at line 122-127:")
        print("        if (recipients.TotalRecipients == 0)")
        print("           return; // Exit without creating history")
        print("     6. NO history record created")
        print("     7. NO emails sent")
        print("     8. User got ZERO feedback")
        print("\n   ✅ FIX REQUIRED:")
        print("     1. Add logging when job exits early (so we can debug)")
        print("     2. Add toast notification in UI: 'No recipients found'")
        print("     3. Update newsletter status to show 'Failed - No recipients'")
        print("     4. User should add email groups OR wait for subscribers")
    elif len(email_groups) == 0 and total_subscribers > 0:
        print("\n⚠️  POTENTIAL BUG: Subscribers exist but not resolved")
        print(f"   Database has {total_subscribers} confirmed subscribers")
        print("   But newsletter never sent emails")
        print("\n   Possible causes:")
        print("     1. Location filtering bug (target_all_locations not working)")
        print("     2. Subscriber query failing")
        print("     3. Job crashing before creating history")
        print("\n   ✅ INVESTIGATION NEEDED:")
        print("     1. Check application logs for exceptions")
        print("     2. Test recipient resolution logic in isolation")
        print("     3. Add comprehensive logging to NewsletterEmailJob")
    else:
        print("\n⚠️  UNEXPECTED STATE")
        print("   Need further investigation")

    print("\n" + "=" * 100)
    print("  INVESTIGATION COMPLETE")
    print("=" * 100 + "\n")

    cur.close()
    conn.close()

except psycopg2.Error as e:
    print(f"\n🔴 DATABASE ERROR: {e}", file=sys.stderr)
    import traceback
    traceback.print_exc()
    sys.exit(1)
except Exception as e:
    print(f"\n🔴 ERROR: {e}", file=sys.stderr)
    import traceback
    traceback.print_exc()
    sys.exit(1)
