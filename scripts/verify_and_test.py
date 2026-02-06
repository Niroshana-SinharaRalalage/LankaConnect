#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Phase 6A.47: Final verification and API test
"""

import psycopg2
import sys
import io
import urllib.request
import json

# Set UTF-8 encoding for output
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

# Connection parameters
conn_params = {
    'host': 'lankaconnect-staging-db.postgres.database.azure.com',
    'database': 'LankaConnectDB',
    'user': 'adminuser',
    'password': '1qaz!QAZ',
    'sslmode': 'require'
}

def main():
    try:
        print("=" * 60)
        print("Phase 6A.47 Verification Report")
        print("=" * 60)

        # Connect to database
        print("\n1. Database Connection")
        conn = psycopg2.connect(**conn_params)
        cur = conn.cursor()
        print("   [OK] Connected to staging database")

        # Total row count
        print("\n2. Total Row Count")
        cur.execute("SELECT COUNT(*) FROM reference_data.reference_values;")
        total_rows = cur.fetchone()[0]
        print(f"   Total rows: {total_rows}")

        # Distinct enum types
        print("\n3. Enum Types")
        cur.execute("SELECT COUNT(DISTINCT enum_type) FROM reference_data.reference_values;")
        distinct_types = cur.fetchone()[0]
        print(f"   Distinct enum types: {distinct_types}")

        # Check for duplicates
        print("\n4. Duplicate Check")
        cur.execute("""
            SELECT enum_type, code, COUNT(*)
            FROM reference_data.reference_values
            GROUP BY enum_type, code
            HAVING COUNT(*) > 1;
        """)
        duplicates = cur.fetchall()
        if duplicates:
            print(f"   [ERROR] Found {len(duplicates)} duplicates:")
            for dup in duplicates:
                print(f"      {dup[0]}.{dup[1]}: {dup[2]} copies")
        else:
            print("   [OK] No duplicates found")

        # Breakdown by enum type
        print("\n5. Breakdown by Enum Type")
        cur.execute("""
            SELECT enum_type, COUNT(*) as count
            FROM reference_data.reference_values
            GROUP BY enum_type
            ORDER BY enum_type;
        """)

        total_check = 0
        for row in cur.fetchall():
            total_check += row[1]
            print(f"   {row[0]}: {row[1]}")

        print(f"\n   Sum check: {total_check} (should match total: {total_rows})")

        # Check constraint
        print("\n6. Check Constraint Status")
        cur.execute("""
            SELECT COUNT(*)
            FROM information_schema.constraint_column_usage
            WHERE table_schema = 'reference_data'
              AND table_name = 'reference_values'
              AND constraint_name = 'ck_reference_values_enum_type';
        """)
        constraint_exists = cur.fetchone()[0]
        if constraint_exists > 0:
            print("   [WARNING] Check constraint still exists (may block future inserts)")
        else:
            print("   [OK] Blocking check constraint has been dropped")

        cur.close()
        conn.close()
        print("\n   [OK] Database connection closed")

        # Test API endpoint
        print("\n7. API Endpoint Test")
        api_url = "https://lankaconnect-api-staging.azurewebsites.net/api/reference-data?types=EmailStatus"
        print(f"   Testing: {api_url}")

        try:
            req = urllib.request.Request(api_url)
            with urllib.request.urlopen(req, timeout=10) as response:
                status_code = response.getcode()
                data = json.loads(response.read().decode('utf-8'))

                print(f"   Status: {status_code}")
                print(f"   Response: {len(data)} items")

                if len(data) == 11:
                    print("   [OK] EmailStatus has correct count (11)")
                    print("\n   Sample items:")
                    for i, item in enumerate(data[:3]):
                        print(f"      {i+1}. {item.get('code')}: {item.get('name')}")
                else:
                    print(f"   [WARNING] Expected 11 EmailStatus items, got {len(data)}")

        except urllib.error.URLError as e:
            print(f"   [ERROR] API request failed: {e}")
        except Exception as e:
            print(f"   [ERROR] Unexpected error testing API: {e}")

        # Final status
        print("\n" + "=" * 60)
        print("FINAL STATUS")
        print("=" * 60)
        print(f"Database: {total_rows} rows across {distinct_types} enum types")
        print(f"Duplicates: {'None' if not duplicates else len(duplicates)}")
        print(f"Status: {'[SUCCESS]' if total_rows > 0 and not duplicates else '[NEEDS REVIEW]'}")
        print("=" * 60)

    except psycopg2.Error as e:
        print(f"\n[ERROR] Database error: {e}")
        sys.exit(1)
    except Exception as e:
        print(f"\n[ERROR] Unexpected error: {e}")
        sys.exit(1)

if __name__ == '__main__':
    main()
