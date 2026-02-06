#!/usr/bin/env python3
import psycopg2

conn = psycopg2.connect('host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser password=1qaz!QAZ sslmode=require')
cur = conn.cursor()

# Query communications schema
cur.execute("""
    SELECT name, category, type, is_active
    FROM communications.email_templates
    WHERE name IN ('newsletter-confirmation', 'registration-cancellation', 'event-cancelled-notification')
    ORDER BY name
""")
results = cur.fetchall()

print('=== Email Template Status ===\n')
found = {r[0] for r in results}

templates = ['newsletter-confirmation', 'registration-cancellation', 'event-cancelled-notification']
for t in templates:
    if t in found:
        r = [r for r in results if r[0] == t][0]
        print(f'OK {t}: EXISTS')
        print(f'   Category: {r[1]}, Type: {r[2]}, Active: {r[3]}')
    else:
        print(f'MISSING {t}: NEEDS MIGRATION')

cur.close()
conn.close()
