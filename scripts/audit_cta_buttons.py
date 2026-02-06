#!/usr/bin/env python3
"""
Audit CTA buttons in email templates to identify duplicates.
"""

import psycopg2

# Connection details for Azure PostgreSQL staging
conn_params = {
    "host": "lankaconnect-staging-db.postgres.database.azure.com",
    "database": "LankaConnectDB",
    "user": "adminuser",
    "password": "1qaz!QAZ",
    "sslmode": "require"
}

def main():
    conn = psycopg2.connect(**conn_params)
    cur = conn.cursor()

    print("=" * 100)
    print("AUDIT: CTA Buttons in Email Templates")
    print("=" * 100)

    # Query to check button presence
    query = """
        SELECT
            name,
            CASE WHEN html_template LIKE '%View Event &amp; Register%'
                 OR html_template LIKE '%View Event & Register%'
                 THEN 'YES' ELSE 'NO' END as has_view_register,
            CASE WHEN html_template LIKE '%View Event Details%' THEN 'YES' ELSE 'NO' END as has_view_details,
            CASE WHEN html_template LIKE '%View Sign-Up Lists%' THEN 'YES' ELSE 'NO' END as has_signup_lists,
            CASE WHEN html_template LIKE '%EventDetailsUrl%' THEN 'YES' ELSE 'NO' END as has_url_param
        FROM communications.email_templates
        WHERE is_active = true
        ORDER BY name;
    """

    cur.execute(query)
    rows = cur.fetchall()

    print(f"\n{'Template Name':<60} | Register | Details | SignUp | URL")
    print("-" * 100)

    duplicate_templates = []

    for row in rows:
        name, has_register, has_details, has_signup, has_url = row

        # Check for duplicates (has both "View Event & Register" AND "View Event Details")
        is_duplicate = has_register == 'YES' and has_details == 'YES'
        if is_duplicate:
            duplicate_templates.append(name)
            marker = " ** DUPLICATE **"
        else:
            marker = ""

        print(f"{name:<60} | {has_register:^8} | {has_details:^7} | {has_signup:^6} | {has_url:^3}{marker}")

    print("\n" + "=" * 100)

    if duplicate_templates:
        print("\nTEMPLATES WITH DUPLICATE BUTTONS (both 'View Event & Register' AND 'View Event Details'):")
        for t in duplicate_templates:
            print(f"  - {t}")
    else:
        print("\nNo templates found with duplicate buttons.")

    # Also check templates that ONLY have "View Event & Register"
    print("\n" + "-" * 100)
    print("\nTEMPLATES WITH ONLY 'View Event & Register' (no 'View Event Details'):")

    query2 = """
        SELECT name
        FROM communications.email_templates
        WHERE is_active = true
          AND (html_template LIKE '%View Event &amp; Register%'
               OR html_template LIKE '%View Event & Register%')
          AND html_template NOT LIKE '%View Event Details%'
        ORDER BY name;
    """
    cur.execute(query2)
    rows2 = cur.fetchall()

    if rows2:
        for row in rows2:
            print(f"  - {row[0]}")
    else:
        print("  None found.")

    cur.close()
    conn.close()

    print("\n" + "=" * 100)
    print("AUDIT COMPLETE")
    print("=" * 100)

if __name__ == "__main__":
    main()
