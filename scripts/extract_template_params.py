import psycopg2
import json
import re
from collections import defaultdict

# Connection string
conn_string = "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser password=1qaz!QAZ sslmode=require"

try:
    # Connect to database
    conn = psycopg2.connect(conn_string)
    cursor = conn.cursor()

    # First, check column names
    cursor.execute("""
        SELECT column_name
        FROM information_schema.columns
        WHERE table_schema = 'communications'
        AND table_name = 'email_templates'
        ORDER BY ordinal_position
    """)
    columns = cursor.fetchall()
    print("Available columns:")
    for col in columns:
        print(f"  - {col[0]}")

    # Query to get all templates (correct column names)
    cursor.execute("""
        SELECT
            name,
            html_template,
            text_template,
            subject_template
        FROM communications.email_templates
        WHERE is_active = true
        ORDER BY name
    """)

    templates = cursor.fetchall()

    # Extract parameters from each template
    results = []

    for template_name, html_body, text_body, subject in templates:
        # Find all {{ParameterName}} patterns
        html_params = set(re.findall(r'\{\{([A-Za-z0-9_]+)\}\}', html_body or ''))
        text_params = set(re.findall(r'\{\{([A-Za-z0-9_]+)\}\}', text_body or ''))
        subject_params = set(re.findall(r'\{\{([A-Za-z0-9_]+)\}\}', subject or ''))

        # Combine all parameters
        all_params = sorted(html_params | text_params | subject_params)

        results.append({
            'template_name': template_name,
            'parameters': all_params,
            'param_count': len(all_params)
        })

        print(f"\n{'='*80}")
        print(f"Template: {template_name}")
        print(f"Parameters ({len(all_params)}):")
        for param in all_params:
            print(f"  - {param}")

    # Save to JSON file
    with open('template_parameters.json', 'w') as f:
        json.dump(results, f, indent=2)

    print(f"\n{'='*80}")
    print(f"Total templates processed: {len(results)}")
    print(f"Results saved to template_parameters.json")

    cursor.close()
    conn.close()

except Exception as e:
    print(f"ERROR: {e}")
