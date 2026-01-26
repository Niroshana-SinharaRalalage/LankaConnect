#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Phase 6A.84 - Newsletter Email History Investigation Script

Purpose: Investigate invalid newsletter history records where total_recipient_count = 0
but successful_sends or failed_sends > 0 (impossible state).

This script queries the Azure staging database to find and analyze invalid records.
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

def print_header(title):
    """Print a formatted header"""
    print("\n" + "=" * 100)
    print(f"  {title}")
    print("=" * 100)

try:
    print_header("PHASE 6A.84: NEWSLETTER EMAIL HISTORY INVESTIGATION")
    print(f"Investigation Time: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")

    conn = psycopg2.connect(conn_string)
    cur = conn.cursor()

    # ========================================================================
    # QUERY 1: Find Invalid History Records
    # ========================================================================
    print_header("QUERY 1: Invalid Newsletter Email History Records")
    print("Finding records with total_recipient_count = 0 BUT (successful_sends > 0 OR failed_sends > 0)")
    print("This is an IMPOSSIBLE STATE indicating a bug in the background job.")

    cur.execute("""
        SELECT
            nh.id,
            nh.newsletter_id,
            nh.sent_at,
            nh.total_recipient_count,
            nh.newsletter_email_group_count,
            nh.event_email_group_count,
            nh.subscriber_count,
            nh.event_registration_count,
            nh.successful_sends,
            nh.failed_sends,
            n.title as newsletter_title,
            n.status as newsletter_status,
            n.sent_at as newsletter_sent_at,
            n.created_by_user_id
        FROM communications.newsletter_email_history nh
        INNER JOIN communications.newsletters n ON nh.newsletter_id = n.id
        WHERE nh.total_recipient_count = 0
          AND (nh.successful_sends > 0 OR nh.failed_sends > 0)
        ORDER BY nh.sent_at DESC
    """)

    invalid_records = cur.fetchall()

    if invalid_records:
        print(f"\n🔴 FOUND {len(invalid_records)} INVALID RECORDS!\n")

        for idx, row in enumerate(invalid_records, 1):
            (hist_id, newsletter_id, sent_at, total_recipients,
             newsletter_groups, event_groups, subscribers, event_regs,
             successful, failed, title, status, newsletter_sent_at, owner_id) = row

            print(f"\nRECORD {idx}:")
            print(f"  History ID: {hist_id}")
            print(f"  Newsletter ID: {newsletter_id}")
            print(f"  Newsletter Title: {title}")
            print(f"  Newsletter Status: {status}")
            print(f"  Owner User ID: {owner_id}")
            print(f"  Sent At: {sent_at}")
            print(f"  Newsletter.SentAt: {newsletter_sent_at}")
            print(f"\n  📊 INVALID DATA:")
            print(f"    🔴 Total Recipients: {total_recipients} (ZERO!)")
            print(f"    🔴 Successful Sends: {successful}")
            print(f"    🔴 Failed Sends: {failed}")
            print(f"    Total Sends: {successful + failed}")
            print(f"\n  Breakdown:")
            print(f"    Newsletter Email Groups: {newsletter_groups}")
            print(f"    Event Email Groups: {event_groups}")
            print(f"    Subscribers: {subscribers}")
            print(f"    Event Registrations: {event_regs}")
            print(f"    Breakdown Total: {newsletter_groups + event_groups + subscribers + event_regs}")
    else:
        print("\n✅ NO INVALID RECORDS FOUND! Database is clean.")

    # ========================================================================
    # QUERY 2: Find Records with Send Count > Recipients
    # ========================================================================
    print_header("QUERY 2: Records Where Sends Exceed Recipients")
    print("Finding records where (successful_sends + failed_sends) > total_recipient_count")

    cur.execute("""
        SELECT
            nh.id,
            nh.newsletter_id,
            nh.sent_at,
            nh.total_recipient_count,
            nh.successful_sends,
            nh.failed_sends,
            (nh.successful_sends + nh.failed_sends) as total_sends,
            n.title
        FROM communications.newsletter_email_history nh
        INNER JOIN communications.newsletters n ON nh.newsletter_id = n.id
        WHERE (nh.successful_sends + nh.failed_sends) > nh.total_recipient_count
        ORDER BY nh.sent_at DESC
    """)

    overflow_records = cur.fetchall()

    if overflow_records:
        print(f"\n🔴 FOUND {len(overflow_records)} OVERFLOW RECORDS!\n")

        for idx, row in enumerate(overflow_records, 1):
            (hist_id, newsletter_id, sent_at, total_recipients, successful, failed, total_sends, title) = row

            print(f"\nRECORD {idx}:")
            print(f"  History ID: {hist_id}")
            print(f"  Newsletter: {title}")
            print(f"  Sent At: {sent_at}")
            print(f"  🔴 Total Recipients: {total_recipients}")
            print(f"  🔴 Total Sends: {total_sends} (successful: {successful}, failed: {failed})")
            print(f"  ❌ OVERFLOW: {total_sends - total_recipients} sends beyond recipient count!")
    else:
        print("\n✅ NO OVERFLOW RECORDS FOUND!")

    # ========================================================================
    # QUERY 3: Find Records with Negative Counts
    # ========================================================================
    print_header("QUERY 3: Records With Negative Counts")
    print("Finding records with negative recipient or send counts")

    cur.execute("""
        SELECT
            nh.id,
            nh.newsletter_id,
            nh.sent_at,
            nh.total_recipient_count,
            nh.successful_sends,
            nh.failed_sends,
            n.title
        FROM communications.newsletter_email_history nh
        INNER JOIN communications.newsletters n ON nh.newsletter_id = n.id
        WHERE nh.total_recipient_count < 0
           OR nh.successful_sends < 0
           OR nh.failed_sends < 0
        ORDER BY nh.sent_at DESC
    """)

    negative_records = cur.fetchall()

    if negative_records:
        print(f"\n🔴 FOUND {len(negative_records)} RECORDS WITH NEGATIVE COUNTS!\n")

        for idx, row in enumerate(negative_records, 1):
            print(f"\nRECORD {idx}:")
            print(f"  History ID: {row[0]}")
            print(f"  Newsletter: {row[6]}")
            print(f"  Total Recipients: {row[3]}")
            print(f"  Successful Sends: {row[4]}")
            print(f"  Failed Sends: {row[5]}")
    else:
        print("\n✅ NO NEGATIVE COUNT RECORDS FOUND!")

    # ========================================================================
    # QUERY 4: Summary Statistics
    # ========================================================================
    print_header("QUERY 4: Newsletter Email History Summary Statistics")

    cur.execute("""
        SELECT
            COUNT(*) as total_history_records,
            COUNT(DISTINCT newsletter_id) as unique_newsletters_sent,
            SUM(total_recipient_count) as total_recipients_all_sends,
            SUM(successful_sends) as total_successful_all_sends,
            SUM(failed_sends) as total_failed_all_sends,
            AVG(total_recipient_count) as avg_recipients_per_send,
            MAX(total_recipient_count) as max_recipients_single_send,
            MIN(sent_at) as first_send_date,
            MAX(sent_at) as last_send_date
        FROM communications.newsletter_email_history
    """)

    stats = cur.fetchone()

    if stats:
        (total_records, unique_newsletters, total_recipients, total_successful,
         total_failed, avg_recipients, max_recipients, first_send, last_send) = stats

        print(f"\n  Total History Records: {total_records}")
        print(f"  Unique Newsletters Sent: {unique_newsletters}")
        print(f"  Total Recipients (all sends): {total_recipients}")
        print(f"  Total Successful Sends: {total_successful}")
        print(f"  Total Failed Sends: {total_failed}")
        print(f"  Success Rate: {(total_successful / (total_successful + total_failed) * 100) if (total_successful + total_failed) > 0 else 0:.2f}%")
        print(f"  Average Recipients per Send: {avg_recipients:.2f}")
        print(f"  Max Recipients in Single Send: {max_recipients}")
        print(f"  First Send Date: {first_send}")
        print(f"  Last Send Date: {last_send}")

    # ========================================================================
    # QUERY 5: Find "Christmas Dinner Dance 2025" Newsletter
    # ========================================================================
    print_header("QUERY 5: 'Christmas Dinner Dance 2025' Newsletter Investigation")
    print("Finding the specific newsletter mentioned in the bug report...")

    cur.execute("""
        SELECT
            n.id,
            n.title,
            n.status,
            n.sent_at,
            n.published_at,
            nh.total_recipient_count,
            nh.successful_sends,
            nh.failed_sends,
            nh.sent_at as history_sent_at
        FROM communications.newsletters n
        LEFT JOIN communications.newsletter_email_history nh ON n.id = nh.newsletter_id
        WHERE LOWER(n.title) LIKE '%christmas%dinner%dance%'
           OR LOWER(n.title) LIKE '%christmas%2025%'
           OR LOWER(n.title) LIKE '%dinner%dance%2025%'
        ORDER BY n.created_at DESC
    """)

    christmas_newsletters = cur.fetchall()

    if christmas_newsletters:
        print(f"\n✅ FOUND {len(christmas_newsletters)} MATCHING NEWSLETTER(S)\n")

        for idx, row in enumerate(christmas_newsletters, 1):
            (newsletter_id, title, status, sent_at, published_at,
             total_recipients, successful, failed, history_sent_at) = row

            print(f"\nNEWSLETTER {idx}:")
            print(f"  ID: {newsletter_id}")
            print(f"  Title: {title}")
            print(f"  Status: {status}")
            print(f"  Sent At: {sent_at}")
            print(f"  Published At: {published_at}")

            if total_recipients is not None:
                print(f"\n  Email History:")
                print(f"    Sent At: {history_sent_at}")
                print(f"    Total Recipients: {total_recipients}")
                print(f"    Successful Sends: {successful}")
                print(f"    Failed Sends: {failed}")

                if total_recipients == 0 and (successful > 0 or failed > 0):
                    print(f"\n    🔴 INVALID STATE DETECTED!")
                    print(f"    This is the bug we're investigating.")
            else:
                print(f"\n  ℹ️  No email history record found (newsletter never sent)")
    else:
        print("\n⚠️  Newsletter not found. May have been deleted or title is different.")

    # ========================================================================
    # FINAL RECOMMENDATIONS
    # ========================================================================
    print_header("INVESTIGATION SUMMARY & RECOMMENDATIONS")

    total_issues = len(invalid_records) + len(overflow_records) + len(negative_records)

    if total_issues > 0:
        print(f"\n🔴 TOTAL ISSUES FOUND: {total_issues}")
        print(f"   - Invalid records (0 recipients + sends): {len(invalid_records)}")
        print(f"   - Overflow records (sends > recipients): {len(overflow_records)}")
        print(f"   - Negative count records: {len(negative_records)}")

        print("\n📋 RECOMMENDED CLEANUP STRATEGY:")
        print("   1. Add 'is_valid' BOOLEAN column (default TRUE)")
        print("   2. Mark invalid records as is_valid = FALSE")
        print("   3. Update queries to filter WHERE is_valid = TRUE")
        print("   4. Add database constraint to prevent future invalid records")
        print("   5. Add domain validation in NewsletterEmailHistory.Create()")
        print("   6. Add idempotency check in NewsletterEmailJob (match EventNotificationEmailJob)")

        print("\n🔧 SQL CLEANUP SCRIPT:")
        print("""
        -- Step 1: Add is_valid column
        ALTER TABLE communications.newsletter_email_history
        ADD COLUMN is_valid BOOLEAN DEFAULT TRUE;

        -- Step 2: Mark invalid records
        UPDATE communications.newsletter_email_history
        SET is_valid = FALSE
        WHERE total_recipient_count = 0
          AND (successful_sends > 0 OR failed_sends > 0);

        -- Step 3: Mark overflow records
        UPDATE communications.newsletter_email_history
        SET is_valid = FALSE
        WHERE (successful_sends + failed_sends) > total_recipient_count;

        -- Step 4: Mark negative count records
        UPDATE communications.newsletter_email_history
        SET is_valid = FALSE
        WHERE total_recipient_count < 0
           OR successful_sends < 0
           OR failed_sends < 0;

        -- Step 5: Add constraint (optional - only after cleanup)
        ALTER TABLE communications.newsletter_email_history
        ADD CONSTRAINT chk_valid_send_counts
        CHECK (
            (total_recipient_count > 0 OR (successful_sends = 0 AND failed_sends = 0))
            AND (successful_sends + failed_sends <= total_recipient_count)
            AND successful_sends >= 0
            AND failed_sends >= 0
        );
        """)
    else:
        print("\n✅ DATABASE IS CLEAN! No invalid records found.")
        print("   Proceed with implementing validation to prevent future issues.")

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
