#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Check actual column names in all tables used in investigation
"""

import psycopg2
import sys
import io

# Set stdout to UTF-8 encoding
if sys.stdout.encoding != 'utf-8':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

conn_string = "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser password=1qaz!QAZ sslmode=require"

tables = [
    ('communications', 'newsletters'),
    ('communications', 'email_groups'),
    ('communications', 'newsletter_email_groups'),
    ('communications', 'newsletter_email_history'),
    ('communications', 'email_messages'),
    ('communications', 'newsletter_subscribers'),
    ('communications', 'newsletter_subscriber_metro_areas'),
    ('events', 'events'),
    ('events', 'registrations'),
]

try:
    conn = psycopg2.connect(conn_string)
    cur = conn.cursor()

    for schema, table in tables:
        print("=" * 100)
        print(f"{schema.upper()}.{table.upper()} TABLE SCHEMA")
        print("=" * 100)

        cur.execute("""
            SELECT column_name, data_type
            FROM information_schema.columns
            WHERE table_schema = %s
              AND table_name = %s
            ORDER BY ordinal_position
        """, (schema, table))

        columns = cur.fetchall()

        if columns:
            print(f"\nTotal Columns: {len(columns)}\n")
            for col_name, data_type in columns:
                print(f"  {col_name:<45} {data_type}")
        else:
            print("\n⚠️  TABLE NOT FOUND OR NO COLUMNS")

        print()

    cur.close()
    conn.close()

except Exception as e:
    print(f"ERROR: {e}", file=sys.stderr)
    import traceback
    traceback.print_exc()
    sys.exit(1)
