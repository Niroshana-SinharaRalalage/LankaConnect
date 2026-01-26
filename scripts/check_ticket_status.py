#!/usr/bin/env python3
"""
Phase 6A.X: Check if ticket exists for registration in database
"""

import sys
import psycopg2

# Connection string
conn_string = (
    "host=lankaconnect-staging-db.postgres.database.azure.com "
    "dbname=LankaConnectDB "
    "user=adminuser "
    "password=1qaz!QAZ "
    "sslmode=require"
)

registration_id = '18422a29-61f7-4575-87d2-72ac0b1581d1'
event_id = 'd543629f-a5ba-4475-b124-3d0fc5200f2f'

try:
    print("=" * 60)
    print("Checking Ticket Status in Database - Phase 6A.X")
    print("=" * 60)

    # Connect to database
    print("\n1. Connecting to Azure PostgreSQL...")
    conn = psycopg2.connect(conn_string)
    cur = conn.cursor()

    # Check registration status
    print(f"\n2. Checking registration {registration_id}...")
    cur.execute("""
        SELECT "Id", "Status", "PaymentStatus", "StripePaymentIntentId", "UpdatedAt"
        FROM events.registrations
        WHERE "Id" = %s
    """, (registration_id,))

    row = cur.fetchone()
    if not row:
        print(f"   [ERROR] Registration not found!")
        sys.exit(1)

    print(f"   Registration ID: {row[0]}")
    print(f"   Status: {row[1]}")
    print(f"   PaymentStatus: {row[2]} (0=Pending, 1=Completed, 2=Failed)")
    print(f"   StripePaymentIntentId: {row[3]}")
    print(f"   UpdatedAt: {row[4]}")

    # Check if ticket exists
    print(f"\n3. Checking for ticket in events.tickets table...")
    cur.execute("""
        SELECT "Id", "TicketCode", "EventId", "RegistrationId",
               "QrCodeData", "IsValid", "ValidatedAt", "CreatedAt", "ExpiresAt"
        FROM events.tickets
        WHERE "RegistrationId" = %s
    """, (registration_id,))

    ticket_row = cur.fetchone()

    if ticket_row:
        print(f"   [OK] Ticket found!")
        print(f"   Ticket ID: {ticket_row[0]}")
        print(f"   Ticket Code: {ticket_row[1]}")
        print(f"   Event ID: {ticket_row[2]}")
        print(f"   Registration ID: {ticket_row[3]}")
        print(f"   QR Code Data: {ticket_row[4][:50] if ticket_row[4] else 'None'}...")
        print(f"   Is Valid: {ticket_row[5]}")
        print(f"   Validated At: {ticket_row[6]}")
        print(f"   Created At: {ticket_row[7]}")
        print(f"   Expires At: {ticket_row[8]}")
    else:
        print(f"   [WARNING] No ticket found for this registration")
        print(f"   This explains why the attendees API returns null ticket fields")

    # Check email messages table for confirmation emails
    print(f"\n4. Checking email_messages table...")
    cur.execute("""
        SELECT "Id", template_name, to_emails, status, "CreatedAt", sent_at, error_message
        FROM communications.email_messages
        WHERE template_data::text LIKE %s
        ORDER BY "CreatedAt" DESC
        LIMIT 5
    """, (f'%{registration_id}%',))

    email_rows = cur.fetchall()

    if email_rows:
        print(f"   [OK] Found {len(email_rows)} email(s) related to this registration:")
        for idx, email_row in enumerate(email_rows, 1):
            print(f"\n   Email #{idx}:")
            print(f"   - ID: {email_row[0]}")
            print(f"   - Template: {email_row[1]}")
            print(f"   - To: {email_row[2]}")
            print(f"   - Status: {email_row[3]}")
            print(f"   - Created: {email_row[4]}")
            print(f"   - Sent: {email_row[5]}")
            if email_row[6]:
                print(f"   - Error: {email_row[6]}")
    else:
        print(f"   [WARNING] No email messages found for this registration")

    # Close connection
    cur.close()
    conn.close()

    print("\n" + "=" * 60)
    print("SUMMARY:")
    print(f"- Registration PaymentStatus: {row[2]} (1=Completed expected)")
    print(f"- Ticket exists: {'YES' if ticket_row else 'NO'}")
    print(f"- Emails sent: {len(email_rows) if email_rows else 0}")
    print("=" * 60)

except psycopg2.Error as e:
    print(f"\n[ERROR] Database error: {e}")
    sys.exit(1)
except Exception as e:
    print(f"\n[ERROR] Unexpected error: {e}")
    sys.exit(1)