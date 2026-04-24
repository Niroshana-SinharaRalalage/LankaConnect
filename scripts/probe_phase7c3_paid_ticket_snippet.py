#!/usr/bin/env python3
"""Extract the context around the DecomposedBlock in the post-migration
paid-ticket template body so we can see what HTML wraps the location keys.
Issue 1: rendered email shows LOCATION header with empty value — need to
verify the template actually has the keys and the surrounding markup."""
import psycopg2

conn_params = {
    "host": "lankaconnect-staging-db.postgres.database.azure.com",
    "database": "LankaConnectDB",
    "user": "adminuser",
    "password": "1qaz!QAZ",
    "sslmode": "require",
}


def main():
    conn = psycopg2.connect(**conn_params)
    cur = conn.cursor()
    for template in (
        "template-paid-event-registration-confirmation-with-ticket",
        "template-free-event-registration-confirmation",
        "template-event-reminder",
    ):
        cur.execute(
            "SELECT html_template FROM communications.email_templates WHERE name = %s;",
            (template,),
        )
        row = cur.fetchone()
        if not row:
            print(f"!! {template} not found")
            continue
        body = row[0]
        idx = body.find("{{LocationName}}")
        if idx == -1:
            idx = body.find("{{LocationAddress}}")
        if idx == -1:
            print(f"\n=== {template}: no decomposed keys found ===")
            continue
        start = max(0, idx - 250)
        end = min(len(body), idx + 900)
        snippet = body[start:end]
        print(f"\n{'=' * 100}")
        print(f"{template}  — DecomposedBlock context (chars {start}..{end})")
        print("=" * 100)
        print(snippet)
    cur.close()
    conn.close()


if __name__ == "__main__":
    main()
