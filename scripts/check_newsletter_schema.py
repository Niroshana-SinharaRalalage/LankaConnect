#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Check actual column names in communications.newsletters table
"""

import psycopg2
import sys
import io

# Set stdout to UTF-8 encoding
if sys.stdout.encoding != 'utf-8':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

conn_string = "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser password=1qaz!QAZ sslmode=require"

try:
    conn = psycopg2.connect(conn_string)
    cur = conn.cursor()

    # Get column names from information_schema
    cur.execute("""
        SELECT column_name, data_type
        FROM information_schema.columns
        WHERE table_schema = 'communications'
          AND table_name = 'newsletters'
        ORDER BY ordinal_position
    """)

    columns = cur.fetchall()

    print("=" * 80)
    print("COMMUNICATIONS.NEWSLETTERS TABLE SCHEMA")
    print("=" * 80)
    print(f"\nTotal Columns: {len(columns)}\n")

    for col_name, data_type in columns:
        print(f"  {col_name:<40} {data_type}")

    print("\n" + "=" * 80)

    cur.close()
    conn.close()

except Exception as e:
    print(f"ERROR: {e}", file=sys.stderr)
    import traceback
    traceback.print_exc()
    sys.exit(1)
