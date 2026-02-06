"""
Phase 6A.85: Backfill newsletter_subscriber_metro_areas junction table for broken subscribers

PROBLEM:
- Subscribers have receive_all_locations = TRUE
- But newsletter_subscriber_metro_areas junction table has 0 rows
- Recipient matching fails: [84 metros] ∩ [] = NO MATCH
- Result: Subscribers don't receive newsletters

SOLUTION:
- Query all subscribers with receive_all_locations = TRUE and empty junction tables
- Insert all 84 active metro area IDs into junction table for each subscriber
- Result: [84 metros] ∩ [84 metros] = MATCH ✓

SAFETY:
- Transaction-based (rollback on error)
- Idempotent (ON CONFLICT DO NOTHING)
- Validation before and after
- Dry-run mode available

USAGE:
    python scripts/backfill_subscriber_metro_areas_phase6a85.py --dry-run
    python scripts/backfill_subscriber_metro_areas_phase6a85.py --execute
"""

import psycopg2
from psycopg2 import sql
import argparse
from datetime import datetime
from typing import List, Tuple

# DATABASE CONNECTION
# Replace with your actual connection string
# For production: Use environment variable or secrets manager
DATABASE_URL = "postgresql://user:password@host:5432/lankaconnect"


def get_connection():
    """Establish database connection"""
    try:
        conn = psycopg2.connect(DATABASE_URL)
        print(f"✓ Connected to database")
        return conn
    except Exception as e:
        print(f"✗ Failed to connect to database: {e}")
        raise


def find_broken_subscribers(cursor) -> List[Tuple[str, str, bool, bool]]:
    """
    Find subscribers with receive_all_locations = TRUE but 0 junction rows
    Returns: List of (subscriber_id, email, is_active, is_confirmed)
    """
    print("\n=== Step 1: Finding Broken Subscribers ===")

    cursor.execute("""
        SELECT
            s.id,
            s.email,
            s.is_active,
            s.is_confirmed,
            s.created_at
        FROM events.newsletter_subscribers s
        WHERE s.receive_all_locations = TRUE
          AND NOT EXISTS (
              SELECT 1
              FROM events.newsletter_subscriber_metro_areas sma
              WHERE sma.subscriber_id = s.id
          )
        ORDER BY s.created_at DESC
    """)

    broken_subscribers = cursor.fetchall()

    print(f"Found {len(broken_subscribers)} broken subscribers:")
    for subscriber_id, email, is_active, is_confirmed, created_at in broken_subscribers:
        status = []
        if is_active:
            status.append("Active")
        if is_confirmed:
            status.append("Confirmed")
        status_str = ", ".join(status) if status else "Inactive"

        print(f"  - {email}")
        print(f"    ID: {subscriber_id}")
        print(f"    Status: {status_str}")
        print(f"    Created: {created_at}")
        print()

    return broken_subscribers


def get_active_metro_areas(cursor) -> List[str]:
    """
    Get all active metro area IDs from database
    Returns: List of metro area UUIDs
    """
    print("\n=== Step 2: Loading Active Metro Areas ===")

    cursor.execute("""
        SELECT id, name, state
        FROM events.metro_areas
        WHERE is_active = TRUE
        ORDER BY state, name
    """)

    metro_areas = cursor.fetchall()

    print(f"Found {len(metro_areas)} active metro areas")
    print(f"Sample metros:")
    for metro_id, name, state in metro_areas[:5]:
        print(f"  - {name}, {state} ({metro_id})")
    print(f"  ... and {len(metro_areas) - 5} more\n")

    return [metro_id for metro_id, _, _ in metro_areas]


def backfill_subscriber(
    cursor,
    subscriber_id: str,
    subscriber_email: str,
    metro_area_ids: List[str],
    dry_run: bool = True
) -> int:
    """
    Backfill newsletter_subscriber_metro_areas junction table for a single subscriber
    Returns: Number of rows inserted
    """
    if dry_run:
        print(f"[DRY-RUN] Would insert {len(metro_area_ids)} metro areas for: {subscriber_email}")
        return len(metro_area_ids)

    print(f"Backfilling: {subscriber_email}")

    # Prepare bulk insert values
    values = [(subscriber_id, metro_id) for metro_id in metro_area_ids]

    # Execute bulk insert with conflict handling
    cursor.executemany("""
        INSERT INTO events.newsletter_subscriber_metro_areas (subscriber_id, metro_area_id)
        VALUES (%s, %s)
        ON CONFLICT (subscriber_id, metro_area_id) DO NOTHING
    """, values)

    inserted_count = cursor.rowcount
    print(f"  ✓ Inserted {inserted_count} junction rows\n")

    return inserted_count


def validate_fix(cursor) -> bool:
    """
    Validate that all subscribers with receive_all_locations now have junction rows
    Returns: True if validation passes, False otherwise
    """
    print("\n=== Step 4: Validation ===")

    # Check 1: No broken subscribers remain
    cursor.execute("""
        SELECT COUNT(*)
        FROM events.newsletter_subscribers s
        WHERE s.receive_all_locations = TRUE
          AND NOT EXISTS (
              SELECT 1
              FROM events.newsletter_subscriber_metro_areas sma
              WHERE sma.subscriber_id = s.id
          )
    """)

    broken_count = cursor.fetchone()[0]

    if broken_count > 0:
        print(f"✗ VALIDATION FAILED: Still {broken_count} broken subscribers")
        return False
    else:
        print(f"✓ Check 1 PASSED: 0 broken subscribers remain")

    # Check 2: All "Receive All Locations" subscribers have correct metro count
    cursor.execute("""
        SELECT
            s.id,
            s.email,
            COUNT(sma.metro_area_id) AS metro_count
        FROM events.newsletter_subscribers s
        LEFT JOIN events.newsletter_subscriber_metro_areas sma ON s.id = sma.subscriber_id
        WHERE s.receive_all_locations = TRUE
        GROUP BY s.id, s.email
        HAVING COUNT(sma.metro_area_id) != (
            SELECT COUNT(*) FROM events.metro_areas WHERE is_active = TRUE
        )
    """)

    incorrect_subscribers = cursor.fetchall()

    if incorrect_subscribers:
        print(f"✗ Check 2 FAILED: {len(incorrect_subscribers)} subscribers have incorrect metro counts:")
        for subscriber_id, email, metro_count in incorrect_subscribers:
            print(f"  - {email}: {metro_count} metros (expected 84)")
        return False
    else:
        print(f"✓ Check 2 PASSED: All subscribers have correct metro area counts")

    # Check 3: Show summary of fixed subscribers
    cursor.execute("""
        SELECT
            CASE
                WHEN s.is_active AND s.is_confirmed THEN 'Active & Confirmed'
                WHEN s.is_active AND NOT s.is_confirmed THEN 'Active (Unconfirmed)'
                WHEN NOT s.is_active THEN 'Inactive'
            END AS status,
            COUNT(*) AS count,
            COUNT(sma.metro_area_id) / COUNT(*) AS avg_metros_per_subscriber
        FROM events.newsletter_subscribers s
        LEFT JOIN events.newsletter_subscriber_metro_areas sma ON s.id = sma.subscriber_id
        WHERE s.receive_all_locations = TRUE
        GROUP BY
            CASE
                WHEN s.is_active AND s.is_confirmed THEN 'Active & Confirmed'
                WHEN s.is_active AND NOT s.is_confirmed THEN 'Active (Unconfirmed)'
                WHEN NOT s.is_active THEN 'Inactive'
            END
        ORDER BY status
    """)

    summary = cursor.fetchall()

    print(f"\n✓ Check 3: Subscriber Summary")
    print(f"{'Status':<25} {'Count':<10} {'Avg Metros/Subscriber':<25}")
    print(f"{'-'*60}")
    for status, count, avg_metros in summary:
        print(f"{status:<25} {count:<10} {avg_metros:<25.0f}")

    return True


def main():
    """Main backfill execution"""
    parser = argparse.ArgumentParser(
        description="Phase 6A.85: Backfill newsletter_subscriber_metro_areas for 'Receive All Locations' subscribers"
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Simulate backfill without making changes"
    )
    parser.add_argument(
        "--execute",
        action="store_true",
        help="Execute backfill (modifies database)"
    )

    args = parser.parse_args()

    if not args.dry_run and not args.execute:
        print("ERROR: Must specify either --dry-run or --execute")
        parser.print_help()
        return

    dry_run = args.dry_run

    print("=" * 70)
    print("Phase 6A.85: Newsletter Subscriber Metro Areas Backfill")
    print("=" * 70)
    print(f"Mode: {'DRY-RUN (no changes)' if dry_run else 'EXECUTE (modifying database)'}")
    print(f"Started: {datetime.now()}")
    print()

    conn = get_connection()
    cursor = conn.cursor()

    try:
        # Step 1: Find broken subscribers
        broken_subscribers = find_broken_subscribers(cursor)

        if not broken_subscribers:
            print("\n✓ No broken subscribers found - nothing to backfill!")
            return

        # Step 2: Get all active metro areas
        metro_area_ids = get_active_metro_areas(cursor)

        if not metro_area_ids:
            print("\n✗ ERROR: No active metro areas found in database!")
            return

        # Step 3: Backfill each broken subscriber
        print(f"\n=== Step 3: Backfilling {len(broken_subscribers)} Subscribers ===\n")

        total_inserted = 0
        for subscriber_id, email, is_active, is_confirmed, created_at in broken_subscribers:
            inserted = backfill_subscriber(
                cursor,
                subscriber_id,
                email,
                metro_area_ids,
                dry_run=dry_run
            )
            total_inserted += inserted

        # Commit or rollback based on mode
        if dry_run:
            conn.rollback()
            print("\n[DRY-RUN] No changes committed (transaction rolled back)")
        else:
            conn.commit()
            print(f"\n✓ Backfill committed: {total_inserted} junction rows inserted")

        # Step 4: Validate fix (only if executed)
        if not dry_run:
            validation_passed = validate_fix(cursor)

            if validation_passed:
                print("\n" + "=" * 70)
                print("✓✓✓ BACKFILL SUCCESSFUL ✓✓✓")
                print("=" * 70)
                print(f"Fixed {len(broken_subscribers)} subscribers")
                print(f"Inserted {total_inserted} junction table rows")
                print(f"All validation checks passed")
            else:
                print("\n" + "=" * 70)
                print("✗✗✗ BACKFILL INCOMPLETE ✗✗✗")
                print("=" * 70)
                print("Some validation checks failed - review output above")
        else:
            print("\n" + "=" * 70)
            print("[DRY-RUN] Backfill Preview Complete")
            print("=" * 70)
            print(f"Would fix {len(broken_subscribers)} subscribers")
            print(f"Would insert {total_inserted} junction table rows")
            print("\nRun with --execute to apply changes")

    except Exception as e:
        conn.rollback()
        print(f"\n✗ ERROR: Backfill failed - {e}")
        print("Transaction rolled back - no changes made")
        raise

    finally:
        cursor.close()
        conn.close()
        print(f"\nCompleted: {datetime.now()}")


if __name__ == "__main__":
    main()