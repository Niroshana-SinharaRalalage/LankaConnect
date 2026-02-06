#!/usr/bin/env python3
"""
Find an active Confirmed registration with Completed payment for testing
"""

import sys
import psycopg2

conn_string = (
    "host=lankaconnect-staging-db.postgres.database.azure.com "
    "dbname=LankaConnectDB "
    "user=adminuser "
    "password=1qaz!QAZ "
    "sslmode=require"
)

event_id = 'd543629f-a5ba-4475-b124-3d0fc5200f2f'

try:
    print("Finding active registrations for testing...")
    print(f"Event ID: {event_id}\n")

    conn = psycopg2.connect(conn_string)
    cur = conn.cursor()

    # Find registrations with Status=Confirmed and PaymentStatus=Completed
    cur.execute("""
        SELECT "Id", "Status", "PaymentStatus", "CreatedAt", "UserId"
        FROM events.registrations
        WHERE "EventId" = %s
          AND "Status" = 'Confirmed'
          AND "PaymentStatus" = 1
        ORDER BY "CreatedAt" DESC
        LIMIT 5
    """, (event_id,))

    rows = cur.fetchall()

    if rows:
        print(f"Found {len(rows)} active registration(s):\n")
        for idx, row in enumerate(rows, 1):
            print(f"{idx}. Registration ID: {row[0]}")
            print(f"   Status: {row[1]}")
            print(f"   PaymentStatus: {row[2]}")
            print(f"   Created: {row[3]}")
            print(f"   UserId: {row[4]}")

            # Check if ticket exists
            cur.execute("""
                SELECT "TicketCode", "CreatedAt"
                FROM events.tickets
                WHERE "RegistrationId" = %s
            """, (row[0],))

            ticket = cur.fetchone()
            if ticket:
                print(f"   Ticket: {ticket[0]} (created {ticket[1]})")
            else:
                print(f"   Ticket: None (needs generation)")
            print()
    else:
        print("No active registrations found with Status=Confirmed and PaymentStatus=Completed")
        print("\nLet me check all registrations:")

        cur.execute("""
            SELECT "Id", "Status", "PaymentStatus", "CreatedAt"
            FROM events.registrations
            WHERE "EventId" = %s
            ORDER BY "CreatedAt" DESC
            LIMIT 5
        """, (event_id,))

        all_rows = cur.fetchall()
        if all_rows:
            print(f"\nFound {len(all_rows)} registration(s) in any state:\n")
            for idx, row in enumerate(all_rows, 1):
                print(f"{idx}. ID: {row[0]}, Status: {row[1]}, PaymentStatus: {row[2]}, Created: {row[3]}")

    cur.close()
    conn.close()

except Exception as e:
    print(f"ERROR: {e}")
    sys.exit(1)
