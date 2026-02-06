#!/usr/bin/env python3
"""
Update a test registration to PaymentStatus=Completed for testing resend feature
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

# Use one of the Confirmed registrations
registration_id = 'c68a9580-0de3-4648-b4d6-69a49b44e826'
event_id = 'd543629f-a5ba-4475-b124-3d0fc5200f2f'

try:
    print(f"Updating registration {registration_id} for testing...\n")

    conn = psycopg2.connect(conn_string)
    cur = conn.cursor()

    # Check current state
    cur.execute("""
        SELECT "Id", "Status", "PaymentStatus", "UpdatedAt"
        FROM events.registrations
        WHERE "Id" = %s
    """, (registration_id,))

    row = cur.fetchone()
    if not row:
        print(f"ERROR: Registration not found!")
        sys.exit(1)

    print(f"Current state:")
    print(f"  Status: {row[1]}")
    print(f"  PaymentStatus: {row[2]}")

    if row[2] == 1:
        print(f"\n✓ Already Completed")
        sys.exit(0)

    # Update to Completed
    cur.execute("""
        UPDATE events.registrations
        SET "PaymentStatus" = 1, "UpdatedAt" = CURRENT_TIMESTAMP
        WHERE "Id" = %s
    """, (registration_id,))

    conn.commit()

    # Verify
    cur.execute("""
        SELECT "PaymentStatus", "UpdatedAt"
        FROM events.registrations
        WHERE "Id" = %s
    """, (registration_id,))

    row = cur.fetchone()
    print(f"\nUpdated state:")
    print(f"  PaymentStatus: {row[0]}")
    print(f"  UpdatedAt: {row[1]}")

    if row[0] == 1:
        print(f"\n✓ SUCCESS - Ready for testing!")
        print(f"\nTest with:")
        print(f"  python scripts/test_resend_confirmation.py")
        print(f"  (Update REGISTRATION_ID to: {registration_id})")

    cur.close()
    conn.close()

except Exception as e:
    print(f"ERROR: {e}")
    sys.exit(1)
