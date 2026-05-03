#!/usr/bin/env python3
"""
Phase 7F-E.3 staging verification (per architect playbook + memory
feedback_template_body_is_authoritative.md).

Three checks:
  1. Each of the 5 migrated templates contains the new {{{RegistrationBreakdownHtml}}} token
     and exactly one anchor pair <!-- attendee-block-7e --> ... <!-- /attendee-block-7e -->
  2. None of the 5 templates still contain the legacy flat tokens
     ({{HeadCountTotal}}, {{HeadCountBreakdownLine}}, {{TierBreakdownLine}})
  3. communications.email_template_backups has 5 rows with migration_tag='Phase7F_E_3'
"""

import sys
import psycopg2

conn_params = {
    "host": "lankaconnect-staging-db.postgres.database.azure.com",
    "database": "LankaConnectDB",
    "user": "adminuser",
    "password": "1qaz!QAZ",
    "sslmode": "require",
}

TEMPLATE_NAMES = [
    "template-free-event-registration-confirmation",
    "template-event-cancellation-notifications",
    "template-event-reminder",
    "template-attendees-added-confirmation",
    "template-paid-event-registration-confirmation-with-ticket",
]

NEW_TOKEN = "{{{RegistrationBreakdownHtml}}}"
ANCHOR_OPEN = "<!-- attendee-block-7e -->"
ANCHOR_CLOSE = "<!-- /attendee-block-7e -->"

# Legacy flat tokens that MUST be gone from the post-migration body
LEGACY_TOKENS = ["{{HeadCountTotal}}", "{{HeadCountBreakdownLine}}", "{{TierBreakdownLine}}"]


def main() -> int:
    conn = psycopg2.connect(**conn_params)
    cur = conn.cursor()

    failures: list[str] = []

    print("=" * 100)
    print("Phase 7F-E.3 staging migration verification")
    print("=" * 100)

    # Check 1 + 2: per-template body checks
    print()
    print("[1] Template body invariants")
    print("-" * 100)
    cur.execute(
        """
        SELECT name, html_template, length(html_template)
        FROM communications.email_templates
        WHERE name = ANY(%s)
        ORDER BY name
        """,
        (TEMPLATE_NAMES,),
    )
    rows = cur.fetchall()
    found_names = {r[0] for r in rows}
    missing = set(TEMPLATE_NAMES) - found_names
    if missing:
        failures.append(f"Templates missing entirely from DB: {sorted(missing)}")

    for name, body, length in rows:
        open_count = body.count(ANCHOR_OPEN)
        close_count = body.count(ANCHOR_CLOSE)
        has_token = NEW_TOKEN in body
        legacy_hits = [t for t in LEGACY_TOKENS if t in body]

        ok = (
            open_count == 1
            and close_count == 1
            and has_token
            and not legacy_hits
        )
        marker = "OK   " if ok else "FAIL "
        print(
            f"  {marker} {name:<60} len={length:>6}  open={open_count} "
            f"close={close_count} token={has_token} legacy={legacy_hits or 'none'}"
        )

        if not ok:
            failures.append(
                f"{name}: open={open_count}, close={close_count}, has_token={has_token}, "
                f"legacy_hits={legacy_hits}"
            )

    # Check 3: backup row count
    print()
    print("[2] Backup table rows for migration_tag='Phase7F_E_3'")
    print("-" * 100)
    cur.execute(
        """
        SELECT template_name, length(html_template_before)
        FROM communications.email_template_backups
        WHERE migration_tag = 'Phase7F_E_3'
        ORDER BY template_name
        """,
    )
    backups = cur.fetchall()
    backup_names = {r[0] for r in backups}
    for n, l in backups:
        print(f"  {n:<60} backup_len={l}")

    missing_backups = set(TEMPLATE_NAMES) - backup_names
    if missing_backups:
        failures.append(f"Missing backup rows for: {sorted(missing_backups)}")
    elif len(backups) != 5:
        failures.append(f"Expected 5 backup rows, found {len(backups)}")

    # Final
    print()
    print("=" * 100)
    if failures:
        print("RESULT: FAIL")
        for f in failures:
            print(f"  - {f}")
        return 1

    print("RESULT: PASS  (5/5 templates migrated; 5/5 backup rows present)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
