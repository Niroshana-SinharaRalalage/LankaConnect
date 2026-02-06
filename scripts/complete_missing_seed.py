#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Phase 6A.47: Complete missing seed data
Identifies and inserts missing reference values to reach 402 total rows.
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

# Expected counts for each enum type
EXPECTED_COUNTS = {
    'EventCategory': 8,
    'EventStatus': 8,
    'UserRole': 6,
    'EmailStatus': 11,
    'EmailType': 9,
    'EmailDeliveryStatus': 8,
    'EmailPriority': 4,
    'Currency': 6,
    'NotificationType': 8,
    'IdentityProvider': 2,
    'SignUpItemCategory': 4,
    'SignUpType': 2,
    'AgeCategory': 2,
    'Gender': 3,
    'EventType': 10,
    'SriLankanLanguage': 3,
    'CulturalBackground': 8,
    'ReligiousContext': 10,
    'GeographicRegion': 35,
    'BuddhistFestival': 11,
    'HinduFestival': 10,
    'RegistrationStatus': 4,
    'PaymentStatus': 4,
    'PricingType': 3,
    'SubscriptionStatus': 5,
    'BadgePosition': 4,
    'CalendarSystem': 4,
    'FederatedProvider': 3,
    'ProficiencyLevel': 5,
    'BusinessCategory': 9,
    'BusinessStatus': 4,
    'ReviewStatus': 4,
    'ServiceType': 4,
    'ForumCategory': 5,
    'TopicStatus': 4,
    'WhatsAppMessageStatus': 5,
    'WhatsAppMessageType': 4,
    'CulturalCommunity': 5,
    'PassPurchaseStatus': 5,
    'CulturalConflictLevel': 5,
    'PoyadayType': 3
}

def main():
    try:
        print("Connecting to database...")
        conn = psycopg2.connect(**conn_params)
        cur = conn.cursor()

        # Check current counts
        print("\nAnalyzing current data...")
        cur.execute("""
            SELECT enum_type, COUNT(*) as count
            FROM reference_data.reference_values
            GROUP BY enum_type
            ORDER BY enum_type;
        """)

        current_counts = {row[0]: row[1] for row in cur.fetchall()}

        print("\nComparison (Current vs Expected):")
        missing_total = 0
        for enum_type, expected in sorted(EXPECTED_COUNTS.items()):
            current = current_counts.get(enum_type, 0)
            status = "[OK]" if current == expected else "[MISSING]"
            if current != expected:
                missing_total += (expected - current)
            print(f"   {status} {enum_type}: {current}/{expected}")

        print(f"\nTotal missing rows: {missing_total}")

        if missing_total == 0:
            print("\n[OK] All data is already complete!")
            return

        # Delete all data and re-seed
        print(f"\nDeleting existing {sum(current_counts.values())} rows...")
        cur.execute("DELETE FROM reference_data.reference_values;")
        conn.commit()
        print("[OK] Deleted")

        # Read and execute the full seed script
        print("\nReading seed script...")
        with open(r'c:\Work\LankaConnect\scripts\seed_reference_data_hotfix.sql', 'r', encoding='utf-8') as f:
            seed_sql = f.read()

        print("Executing full seed script...")
        cur.execute(seed_sql)
        conn.commit()
        print("[OK] Seed script executed")

        # Verify final counts
        print("\nVerifying final data...")
        cur.execute("SELECT COUNT(*) FROM reference_data.reference_values;")
        total_rows = cur.fetchone()[0]

        cur.execute("SELECT COUNT(DISTINCT enum_type) FROM reference_data.reference_values;")
        distinct_types = cur.fetchone()[0]

        print(f"   Total rows: {total_rows}")
        print(f"   Distinct enum types: {distinct_types}")

        if total_rows == 402 and distinct_types == 41:
            print("\n[SUCCESS] All 402 reference values seeded across 41 enum types!")
        else:
            print(f"\n[WARNING] Expected 402 rows and 41 types, got {total_rows} rows and {distinct_types} types")

            # Show what's still missing
            cur.execute("""
                SELECT enum_type, COUNT(*) as count
                FROM reference_data.reference_values
                GROUP BY enum_type
                ORDER BY enum_type;
            """)

            final_counts = {row[0]: row[1] for row in cur.fetchall()}
            print("\nFinal breakdown:")
            for enum_type, expected in sorted(EXPECTED_COUNTS.items()):
                current = final_counts.get(enum_type, 0)
                status = "[OK]" if current == expected else "[MISSING]"
                print(f"   {status} {enum_type}: {current}/{expected}")

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
