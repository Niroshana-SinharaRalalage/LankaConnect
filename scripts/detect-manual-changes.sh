#!/bin/bash
# Detect manual database changes by comparing staging DB with migrations

STAGING_HOST="lankaconnect-staging-db.postgres.database.azure.com"
STAGING_DB="LankaConnectDB"
STAGING_USER="adminuser"
export PGPASSWORD="1qaz!QAZ"

echo "=== DETECTING MANUAL DATABASE CHANGES ==="
echo ""

# Check if psql is available
if ! command -v psql &> /dev/null; then
    echo "ERROR: psql not found. Install PostgreSQL client tools."
    echo ""
    echo "Alternative: Use Azure Data Studio or pgAdmin to run these queries manually:"
    echo ""
    echo "Query 1 - Find modified templates:"
    echo "SELECT name, created_at, updated_at FROM communications.email_templates WHERE updated_at > created_at + interval '1 minute';"
    echo ""
    echo "Query 2 - Count templates:"
    echo "SELECT COUNT(*) FROM communications.email_templates;"
    echo ""
    exit 1
fi

echo "Step 1: Checking email templates..."
echo ""

# Find templates that were manually updated
MODIFIED=$(psql -h "$STAGING_HOST" -U "$STAGING_USER" -d "$STAGING_DB" -t -c "
SELECT name, updated_at
FROM communications.email_templates
WHERE updated_at IS NOT NULL
  AND updated_at > created_at + interval '1 minute'
ORDER BY updated_at DESC;
" 2>&1)

if [ $? -eq 0 ]; then
    if [ -z "$MODIFIED" ] || [ "$MODIFIED" == " " ]; then
        echo "✅ NO manually modified templates detected!"
        echo "   Database matches migrations - safe to deploy."
    else
        echo "⚠️  MANUALLY MODIFIED templates found:"
        echo "$MODIFIED"
        echo ""
        echo "Exporting manual changes..."

        # Export the SQL to update these templates
        psql -h "$STAGING_HOST" -U "$STAGING_USER" -d "$STAGING_DB" -t -c "
SELECT
    '-- Template: ' || name || E'\n' ||
    'UPDATE communications.email_templates SET' || E'\n' ||
    '  subject_template = ' || quote_literal(subject_template) || ',' || E'\n' ||
    '  html_template = ' || quote_literal(html_template) || ',' || E'\n' ||
    '  text_template = ' || quote_literal(text_template) || ',' || E'\n' ||
    '  updated_at = NOW()' || E'\n' ||
    'WHERE name = ' || quote_literal(name) || ';' || E'\n\n'
FROM communications.email_templates
WHERE updated_at IS NOT NULL
  AND updated_at > created_at + interval '1 minute'
ORDER BY name;
" > scripts/manual_changes_detected.sql 2>&1

        echo "✅ SQL exported to: scripts/manual_changes_detected.sql"
    fi
else
    echo "❌ Could not connect to staging database"
    echo "$MODIFIED"
fi

echo ""
echo "Step 2: Checking reference data counts..."
echo ""

# Check counts
COUNTS=$(psql -h "$STAGING_HOST" -U "$STAGING_USER" -d "$STAGING_DB" -t -c "
SELECT
    'Metro Areas: ' || (SELECT COUNT(*) FROM events.metro_areas) || ' (expected: 22+)' ||
    E'\nEmail Templates: ' || (SELECT COUNT(*) FROM communications.email_templates) || ' (expected: 15+)' ||
    E'\nReference Values: ' || (SELECT COUNT(*) FROM reference_data.reference_values) || ' (expected: 50+)' ||
    E'\nState Tax Rates: ' || (SELECT COUNT(*) FROM reference_data.state_tax_rates) || ' (expected: 51)';
" 2>&1)

echo "$COUNTS"

echo ""
echo "=== ANALYSIS COMPLETE ==="
