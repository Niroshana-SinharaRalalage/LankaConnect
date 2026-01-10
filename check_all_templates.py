#!/usr/bin/env python3
import psycopg2

conn = psycopg2.connect('host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser password=1qaz!QAZ sslmode=require')
cur = conn.cursor()

# Query ALL templates
cur.execute("""
    SELECT name, category, type, is_active, subject_template
    FROM communications.email_templates
    ORDER BY name
""")
results = cur.fetchall()

print('=== ALL Email Templates in Database ===\n')
for r in results:
    print(f'{r[0]}')
    print(f'  Category: {r[1]}, Type: {r[2]}, Active: {r[3]}')
    print(f'  Subject: {r[4][:60]}...')
    print()

print('\n=== Checking Required Templates ===\n')
templates_needed = ['newsletter-confirmation', 'registration-cancellation', 'registration-confirmation']
found_names = {r[0] for r in results}

for t in templates_needed:
    if t in found_names:
        print(f'OK {t}: EXISTS')
    else:
        print(f'MISSING {t}')

cur.close()
conn.close()
