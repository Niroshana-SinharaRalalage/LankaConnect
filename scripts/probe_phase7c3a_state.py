#!/usr/bin/env python3
"""Check whether the failed Phase7C3a migration left any state behind:
backup table, __EFMigrationsHistory row, and current-body snapshots so
we can safely amend the migration file."""
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

    cur.execute(
        "SELECT table_schema, table_name FROM information_schema.tables "
        "WHERE table_schema = 'communications' "
        "  AND table_name LIKE 'email_templates_backup%' ORDER BY table_name;"
    )
    print("backup tables:", cur.fetchall())

    cur.execute(
        "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" "
        "WHERE \"MigrationId\" LIKE '%Phase7C3a%';"
    )
    print("Phase7C3a in history:", cur.fetchall())

    cur.execute(
        "SELECT name, length(html_template), "
        "  (html_template LIKE '%{{EventLocation}}%') AS has_standard, "
        "  (html_template LIKE '%{{LocationName}}%') AS has_decomposed "
        "FROM communications.email_templates "
        "WHERE name IN ("
        "  'template-paid-event-registration-confirmation-with-ticket',"
        "  'template-event-registration-cancellation',"
        "  'template-event-cancellation-notifications',"
        "  'template-event-approval',"
        "  'template-event-reminder',"
        "  'template-attendees-added-confirmation',"
        "  'template-preliminary-registration-payment-pending'"
        ") ORDER BY name;"
    )
    print()
    print("current template state:")
    for row in cur.fetchall():
        print(f"  {row[0]:65} length={row[1]:>6}  has_standard={row[2]}  has_decomposed={row[3]}")
    cur.close()
    conn.close()


if __name__ == "__main__":
    main()
