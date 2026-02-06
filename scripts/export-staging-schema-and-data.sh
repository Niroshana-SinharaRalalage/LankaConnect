#!/bin/bash
# Export complete staging database schema and reference data for production

# Connection details
STAGING_HOST="lankaconnect-staging-db.postgres.database.azure.com"
STAGING_DB="LankaConnectDB"
STAGING_USER="adminuser"
export PGPASSWORD="1qaz!QAZ"

OUTPUT_DIR="./production-migration-scripts"
mkdir -p "$OUTPUT_DIR"

echo "=== Exporting Staging Database for Production Migration ==="
echo ""

# Option 1: Full database dump (NOT RECOMMENDED - includes test data)
echo "1. Creating full database dump..."
pg_dump \
  --host="$STAGING_HOST" \
  --username="$STAGING_USER" \
  --dbname="$STAGING_DB" \
  --no-password \
  --verbose \
  --file="$OUTPUT_DIR/full_database_dump.sql" \
  2>&1 | grep -v "^pg_dump:"

# Option 2: Schema only (structure without data)
echo ""
echo "2. Creating schema-only dump..."
pg_dump \
  --host="$STAGING_HOST" \
  --username="$STAGING_USER" \
  --dbname="$STAGING_DB" \
  --schema-only \
  --no-password \
  --file="$OUTPUT_DIR/schema_only.sql" \
  2>&1 | grep -v "^pg_dump:"

# Option 3: Reference data only (RECOMMENDED)
echo ""
echo "3. Creating reference data dump..."
pg_dump \
  --host="$STAGING_HOST" \
  --username="$STAGING_USER" \
  --dbname="$STAGING_DB" \
  --no-password \
  --data-only \
  --table='events.metro_areas' \
  --table='communications.email_templates' \
  --table='reference_data.reference_values' \
  --table='reference_data.state_tax_rates' \
  --file="$OUTPUT_DIR/reference_data_only.sql" \
  2>&1 | grep -v "^pg_dump:"

# Option 4: Email templates only
echo ""
echo "4. Creating email templates dump..."
pg_dump \
  --host="$STAGING_HOST" \
  --username="$STAGING_USER" \
  --dbname="$STAGING_DB" \
  --no-password \
  --data-only \
  --table='communications.email_templates' \
  --file="$OUTPUT_DIR/email_templates_only.sql" \
  2>&1 | grep -v "^pg_dump:"

# Option 5: Custom SELECT export for comparison
echo ""
echo "5. Creating CSV export of email templates..."
psql \
  --host="$STAGING_HOST" \
  --username="$STAGING_USER" \
  --dbname="$STAGING_DB" \
  --no-password \
  --command="COPY (SELECT name, type, category, subject_template, html_template, text_template, is_active, created_at, updated_at FROM communications.email_templates ORDER BY name) TO STDOUT WITH CSV HEADER;" \
  > "$OUTPUT_DIR/email_templates.csv"

echo ""
echo "=== Export Complete ==="
echo ""
echo "Files created in $OUTPUT_DIR:"
ls -lh "$OUTPUT_DIR"
echo ""
echo "RECOMMENDED: Use 'reference_data_only.sql' for production migration"
echo "This includes all lookup tables but NO user data"
