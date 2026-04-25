#!/usr/bin/env python3
"""Deep probe for the two surprising templates: which tokens does
template-event-cancellation-notifications use for greeting, and does
template-preliminary-registration-payment-pending render location at all?"""
import re
import psycopg2

conn_params = {
    "host": "lankaconnect-staging-db.postgres.database.azure.com",
    "database": "LankaConnectDB",
    "user": "adminuser",
    "password": "1qaz!QAZ",
    "sslmode": "require",
}

TARGETS = [
    "template-event-cancellation-notifications",
    "template-preliminary-registration-payment-pending",
]


def main():
    conn = psycopg2.connect(**conn_params)
    cur = conn.cursor()
    cur.execute(
        "SELECT name, html_template, length(html_template) "
        "FROM communications.email_templates "
        "WHERE name = ANY(%s);",
        (TARGETS,),
    )
    for name, body, length in cur.fetchall():
        print("=" * 100)
        print(f"{name}  (length={length})")
        # Find every {{...}} token
        tokens = sorted(set(re.findall(r"\{\{\s*[#/]?[A-Za-z_][A-Za-z0-9_.]*\s*\}\}", body)))
        print(f"  total unique tokens: {len(tokens)}")
        for tok in tokens:
            print(f"    {tok}")
        # Find the first 'Hi ' / 'Dear ' / 'Hello ' snippet for greeting context
        for needle in ["Hi ", "Dear ", "Hello "]:
            idx = body.find(needle)
            if idx != -1:
                ctx = body[idx: idx + 200].replace("\n", " ").replace("\r", "")
                print(f"  greeting snippet near '{needle}': {ctx!r}")
                break
    cur.close()
    conn.close()


if __name__ == "__main__":
    main()
