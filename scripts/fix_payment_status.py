#!/usr/bin/env python3
"""
Phase 6A.X: Fix PaymentStatus for testing resend confirmation feature
Quick script to update test registration's PaymentStatus from Pending to Completed
"""

import sys

try:
    import psycopg2
except ImportError:
    print("psycopg2 not installed. Installing psycopg2-binary...")
    import subprocess
    subprocess.check_call([sys.executable, "-m", "pip", "install", "psycopg2-binary"])
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
    # Connect to database
    print("Connecting to Azure PostgreSQL...")
    conn = psycopg2.connect(conn_string)
    cur = conn.cursor()

    # Check current state
    print(f"\n1. Checking current state of registration {registration_id}...")
    cur.execute("""
        SELECT "Id", "Status", "PaymentStatus", "StripePaymentIntentId", "UpdatedAt"
        FROM events.registrations
        WHERE "Id" = %s
    """, (registration_id,))

    row = cur.fetchone()
    if not row:
        print(f"ERROR: Registration {registration_id} not found!")
        sys.exit(1)

    print(f"   ID: {row[0]}")
    print(f"   Status: {row[1]} (1 = Confirmed)")
    print(f"   PaymentStatus: {row[2]} (0 = Pending, 1 = Completed, 2 = Failed)")
    print(f"   StripePaymentIntentId: {row[3]}")
    print(f"   UpdatedAt: {row[4]}")

    if row[2] == 1:
        print("\n[OK] PaymentStatus is already Completed (1). No update needed.")
        cur.close()
        conn.close()
        sys.exit(0)

    # Update PaymentStatus
    print(f"\n2. Updating PaymentStatus to Completed (1)...")
    cur.execute("""
        UPDATE events.registrations
        SET
            "PaymentStatus" = 1,
            "UpdatedAt" = CURRENT_TIMESTAMP
        WHERE "Id" = %s
          AND "EventId" = %s
    """, (registration_id, event_id))

    rows_updated = cur.rowcount
    conn.commit()

    if rows_updated == 0:
        print("   WARNING: No rows updated. Check conditions.")
    else:
        print(f"   [OK] Updated {rows_updated} row(s)")

    # Verify update
    print(f"\n3. Verifying update...")
    cur.execute("""
        SELECT "Id", "Status", "PaymentStatus", "StripePaymentIntentId", "UpdatedAt"
        FROM events.registrations
        WHERE "Id" = %s
    """, (registration_id,))

    row = cur.fetchone()
    print(f"   ID: {row[0]}")
    print(f"   Status: {row[1]} (1 = Confirmed)")
    print(f"   PaymentStatus: {row[2]} (0 = Pending, 1 = Completed, 2 = Failed)")
    print(f"   StripePaymentIntentId: {row[3]}")
    print(f"   UpdatedAt: {row[4]}")

    if row[2] == 1:
        print("\n[SUCCESS] PaymentStatus successfully updated to Completed (1)!")
        print("\nYou can now test the resend confirmation endpoint.")
    else:
        print(f"\n[ERROR] PaymentStatus is still {row[2]} instead of 1")

    # Close connection
    cur.close()
    conn.close()

except psycopg2.Error as e:
    print(f"\n[ERROR] Database error: {e}")
    sys.exit(1)
except Exception as e:
    print(f"\n[ERROR] Unexpected error: {e}")
    sys.exit(1)
