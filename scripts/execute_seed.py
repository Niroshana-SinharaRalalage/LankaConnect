#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Phase 6A.47: Execute seed data for reference_values table
This script drops the blocking constraint and inserts all 402 reference values.
"""

import psycopg2
import sys
import io

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
        print("Connecting to database...")
        conn = psycopg2.connect(**conn_params)
        cur = conn.cursor()

        # Step 1: Drop the blocking constraint
        print("\n1. Dropping check constraint...")
        cur.execute("""
            ALTER TABLE reference_data.reference_values
            DROP CONSTRAINT IF EXISTS ck_reference_values_enum_type;
        """)
        conn.commit()
        print("[OK] Constraint dropped successfully")

        # Step 2: Read and execute the seed script
        print("\n2. Reading seed script...")
        with open(r'c:\Work\LankaConnect\scripts\seed_reference_data_hotfix.sql', 'r', encoding='utf-8') as f:
            seed_sql = f.read()

        print("3. Executing seed script...")
        cur.execute(seed_sql)
        conn.commit()
        print("[OK] Seed script executed successfully")

        # Step 3: Verify the data
        print("\n4. Verifying data...")
        cur.execute("SELECT COUNT(*) FROM reference_data.reference_values;")
        total_rows = cur.fetchone()[0]
        print(f"   Total rows: {total_rows}")

        cur.execute("SELECT COUNT(DISTINCT enum_type) FROM reference_data.reference_values;")
        distinct_types = cur.fetchone()[0]
        print(f"   Distinct enum types: {distinct_types}")

        if total_rows == 402 and distinct_types == 41:
            print("\n[SUCCESS] All 402 reference values seeded across 41 enum types!")
        else:
            print(f"\n[WARNING] Expected 402 rows and 41 types, got {total_rows} rows and {distinct_types} types")

        # Show breakdown by type
        print("\n5. Breakdown by enum type:")
        cur.execute("""
            SELECT enum_type, COUNT(*) as count
            FROM reference_data.reference_values
            GROUP BY enum_type
            ORDER BY enum_type;
        """)
        for row in cur.fetchall():
            print(f"   {row[0]}: {row[1]}")

        cur.close()
        conn.close()
        print("\n[OK] Database connection closed")

    except psycopg2.Error as e:
        print(f"\n[ERROR] Database error: {e}")
        sys.exit(1)
    except FileNotFoundError as e:
        print(f"\n[ERROR] File not found: {e}")
        sys.exit(1)
    except Exception as e:
        print(f"\n[ERROR] Unexpected error: {e}")
        sys.exit(1)

if __name__ == '__main__':
    main()
