#!/usr/bin/env python3
"""
Apply CTA button fix to email templates.
Removes duplicate buttons per the approved fix plan.
"""

import psycopg2
import re

# Connection details for Azure PostgreSQL staging
conn_params = {
    "host": "lankaconnect-staging-db.postgres.database.azure.com",
    "database": "LankaConnectDB",
    "user": "adminuser",
    "password": "1qaz!QAZ",
    "sslmode": "require"
}

def remove_view_event_details_button(html):
    """Remove <!-- View Event Details CTA Button --> block from HTML."""
    # Pattern to match the View Event Details button block
    pattern = r'<!-- View Event Details CTA Button -->[\s\S]*?</table>\s*'
    return re.sub(pattern, '', html)

def remove_view_event_register_button(html):
    """Remove View Event & Register button block from HTML."""
    # Pattern to match the View Event & Register button (HTML entity encoded)
    pattern = r'<table[^>]*role="presentation"[^>]*>[\s\S]*?View Event &amp; Register[\s\S]*?</table>\s*'
    return re.sub(pattern, '', html)

def main():
    conn = psycopg2.connect(**conn_params)
    cur = conn.cursor()

    print("=" * 100)
    print("APPLYING CTA BUTTON FIX")
    print("=" * 100)

    fixes = [
        {
            "template": "template-new-event-publication",
            "action": "REMOVE 'View Event Details' (keep 'View Event & Register')",
            "remove_func": remove_view_event_details_button
        },
        {
            "template": "template-event-details-publication",
            "action": "REMOVE 'View Event & Register' (keep 'View Event Details')",
            "remove_func": remove_view_event_register_button
        },
        {
            "template": "template-signup-list-commitment-confirmation",
            "action": "REMOVE 'View Event & Register' (keep 'View Event Details')",
            "remove_func": remove_view_event_register_button
        },
        {
            "template": "template-signup-list-commitment-update",
            "action": "REMOVE 'View Event & Register' (keep 'View Event Details')",
            "remove_func": remove_view_event_register_button
        }
    ]

    for fix in fixes:
        template_name = fix["template"]
        action = fix["action"]
        remove_func = fix["remove_func"]

        print(f"\n{'-' * 80}")
        print(f"Template: {template_name}")
        print(f"Action: {action}")

        # Get current HTML
        cur.execute(
            "SELECT html_template FROM communications.email_templates WHERE name = %s",
            (template_name,)
        )
        row = cur.fetchone()

        if not row:
            print(f"  [SKIP] Template not found")
            continue

        old_html = row[0]

        # Check current state
        has_register = "View Event &amp; Register" in old_html or "View Event & Register" in old_html
        has_details = "View Event Details" in old_html

        print(f"  Before: View Event & Register = {has_register}, View Event Details = {has_details}")

        if not has_register and not has_details:
            print(f"  [SKIP] No buttons to fix")
            continue

        # Apply fix
        new_html = remove_func(old_html)

        if new_html == old_html:
            print(f"  [SKIP] No changes made (button may already be removed)")
            continue

        # Update database
        cur.execute(
            """
            UPDATE communications.email_templates
            SET html_template = %s, updated_at = NOW()
            WHERE name = %s
            """,
            (new_html, template_name)
        )

        # Verify
        has_register_after = "View Event &amp; Register" in new_html or "View Event & Register" in new_html
        has_details_after = "View Event Details" in new_html

        print(f"  After:  View Event & Register = {has_register_after}, View Event Details = {has_details_after}")
        print(f"  [OK] Fixed successfully")

    conn.commit()
    print(f"\n{'=' * 100}")
    print("FIX APPLIED - Changes committed to database")
    print("=" * 100)

    cur.close()
    conn.close()

if __name__ == "__main__":
    main()
