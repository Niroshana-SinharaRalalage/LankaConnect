"""
Phase 6A.85: Backfill newsletter_metro_areas junction table for broken newsletters

PROBLEM:
- 16 newsletters have target_all_locations = TRUE
- But newsletter_metro_areas junction table has 0 rows
- Recipient matching fails: [] ∩ [user metros] = NO MATCH
- Result: No emails sent

SOLUTION:
- Query all newsletters with target_all_locations = TRUE and empty junction tables
- Insert all 84 active metro area IDs into junction table for each newsletter
- Result: [84 metros] ∩ [user metros] = MATCH ✓

SAFETY:
- Transaction-based (rollback on error)
- Idempotent (ON CONFLICT DO NOTHING)
- Validation before and after
- Dry-run mode available

USAGE:
    python scripts/backfill_newsletter_metro_areas_phase6a85.py --dry-run
    python scripts/backfill_newsletter_metro_areas_phase6a85.py --execute
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


def find_broken_newsletters(cursor) -> List[Tuple[str, str, bool]]:
    """
    Find newsletters with target_all_locations = TRUE but 0 junction rows
    Returns: List of (newsletter_id, title, is_announcement_only)
    """
    print("\n=== Step 1: Finding Broken Newsletters ===")

    cursor.execute("""
        SELECT
            n.id,
            n.title,
            n.is_announcement_only,
            n.status,
            n.created_at
        FROM events.newsletters n
        WHERE n.target_all_locations = TRUE
          AND NOT EXISTS (
              SELECT 1
              FROM events.newsletter_metro_areas nma
              WHERE nma.newsletter_id = n.id
          )
        ORDER BY n.created_at DESC
    """)

    broken_newsletters = cursor.fetchall()

    print(f"Found {len(broken_newsletters)} broken newsletters:")
    for newsletter_id, title, is_announcement, status, created_at in broken_newsletters:
        print(f"  - {title[:50]}")
        print(f"    ID: {newsletter_id}")
        print(f"    Status: {status}")
        print(f"    Announcement-Only: {is_announcement}")
        print(f"    Created: {created_at}")
        print()

    return broken_newsletters


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


def backfill_newsletter(
    cursor,
    newsletter_id: str,
    newsletter_title: str,
    metro_area_ids: List[str],
    dry_run: bool = True
) -> int:
    """
    Backfill newsletter_metro_areas junction table for a single newsletter
    Returns: Number of rows inserted
    """
    if dry_run:
        print(f"[DRY-RUN] Would insert {len(metro_area_ids)} metro areas for: {newsletter_title[:50]}")
        return len(metro_area_ids)

    print(f"Backfilling: {newsletter_title[:50]}")

    # Prepare bulk insert values
    values = [(newsletter_id, metro_id) for metro_id in metro_area_ids]

    # Execute bulk insert with conflict handling
    cursor.executemany("""
        INSERT INTO events.newsletter_metro_areas (newsletter_id, metro_area_id)
        VALUES (%s, %s)
        ON CONFLICT (newsletter_id, metro_area_id) DO NOTHING
    """, values)

    inserted_count = cursor.rowcount
    print(f"  ✓ Inserted {inserted_count} junction rows\n")

    return inserted_count


def validate_fix(cursor) -> bool:
    """
    Validate that all newsletters with target_all_locations now have junction rows
    Returns: True if validation passes, False otherwise
    """
    print("\n=== Step 4: Validation ===")

    # Check 1: No broken newsletters remain
    cursor.execute("""
        SELECT COUNT(*)
        FROM events.newsletters n
        WHERE n.target_all_locations = TRUE
          AND NOT EXISTS (
              SELECT 1
              FROM events.newsletter_metro_areas nma
              WHERE nma.newsletter_id = n.id
          )
    """)

    broken_count = cursor.fetchone()[0]

    if broken_count > 0:
        print(f"✗ VALIDATION FAILED: Still {broken_count} broken newsletters")
        return False
    else:
        print(f"✓ Check 1 PASSED: 0 broken newsletters remain")

    # Check 2: All "All Locations" newsletters have correct metro count
    cursor.execute("""
        SELECT
            n.id,
            n.title,
            COUNT(nma.metro_area_id) AS metro_count
        FROM events.newsletters n
        LEFT JOIN events.newsletter_metro_areas nma ON n.id = nma.newsletter_id
        WHERE n.target_all_locations = TRUE
        GROUP BY n.id, n.title
        HAVING COUNT(nma.metro_area_id) != (
            SELECT COUNT(*) FROM events.metro_areas WHERE is_active = TRUE
        )
    """)

    incorrect_newsletters = cursor.fetchall()

    if incorrect_newsletters:
        print(f"✗ Check 2 FAILED: {len(incorrect_newsletters)} newsletters have incorrect metro counts:")
        for newsletter_id, title, metro_count in incorrect_newsletters:
            print(f"  - {title}: {metro_count} metros (expected 84)")
        return False
    else:
        print(f"✓ Check 2 PASSED: All newsletters have correct metro area counts")

    # Check 3: Show summary of fixed newsletters
    cursor.execute("""
        SELECT
            n.status,
            COUNT(*) AS count,
            COUNT(nma.metro_area_id) / COUNT(*) AS avg_metros_per_newsletter
        FROM events.newsletters n
        LEFT JOIN events.newsletter_metro_areas nma ON n.id = nma.newsletter_id
        WHERE n.target_all_locations = TRUE
        GROUP BY n.status
        ORDER BY n.status
    """)

    summary = cursor.fetchall()

    print(f"\n✓ Check 3: Newsletter Summary")
    print(f"{'Status':<15} {'Count':<10} {'Avg Metros/Newsletter':<25}")
    print(f"{'-'*50}")
    for status, count, avg_metros in summary:
        print(f"{status:<15} {count:<10} {avg_metros:<25.0f}")

    return True


def main():
    """Main backfill execution"""
    parser = argparse.ArgumentParser(
        description="Phase 6A.85: Backfill newsletter_metro_areas for 'All Locations' newsletters"
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
    print("Phase 6A.85: Newsletter Metro Areas Backfill")
    print("=" * 70)
    print(f"Mode: {'DRY-RUN (no changes)' if dry_run else 'EXECUTE (modifying database)'}")
    print(f"Started: {datetime.now()}")
    print()

    conn = get_connection()
    cursor = conn.cursor()

    try:
        # Step 1: Find broken newsletters
        broken_newsletters = find_broken_newsletters(cursor)

        if not broken_newsletters:
            print("\n✓ No broken newsletters found - nothing to backfill!")
            return

        # Step 2: Get all active metro areas
        metro_area_ids = get_active_metro_areas(cursor)

        if not metro_area_ids:
            print("\n✗ ERROR: No active metro areas found in database!")
            return

        # Step 3: Backfill each broken newsletter
        print(f"\n=== Step 3: Backfilling {len(broken_newsletters)} Newsletters ===\n")

        total_inserted = 0
        for newsletter_id, title, is_announcement, status, created_at in broken_newsletters:
            inserted = backfill_newsletter(
                cursor,
                newsletter_id,
                title,
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
                print(f"Fixed {len(broken_newsletters)} newsletters")
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
            print(f"Would fix {len(broken_newsletters)} newsletters")
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