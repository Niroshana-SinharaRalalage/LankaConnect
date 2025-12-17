#!/bin/bash

echo "=== Checking Webhook Processing Status ==="
echo ""

# Get the database connection string from Key Vault
echo "1. Fetching database connection string..."
DB_CONN=$(az keyvault secret show --vault-name lankaconnect-staging-kv --name database-connection-string --query value -o tsv 2>/dev/null)

if [ -z "$DB_CONN" ]; then
    echo "Failed to fetch connection string"
    exit 1
fi

# Extract connection parameters
DB_SERVER=$(echo "$DB_CONN" | grep -oP 'Server=\K[^;]+')
DB_NAME=$(echo "$DB_CONN" | grep -oP 'Database=\K[^;]+')
DB_USER=$(echo "$DB_CONN" | grep -oP 'User Id=\K[^;]+')
DB_PASS=$(echo "$DB_CONN" | grep -oP 'Password=\K[^;]+')

echo "Database: $DB_NAME on $DB_SERVER"
echo ""

# Check recent webhook events
echo "2. Checking recent webhook events received..."
PGPASSWORD="$DB_PASS" psql -h "$DB_SERVER" -U "$DB_USER" -d "$DB_NAME" -c "
SELECT 
    event_id,
    event_type,
    created_at,
    processed
FROM stripe_webhook_events
ORDER BY created_at DESC
LIMIT 10;
" 2>/dev/null

echo ""
echo "3. Checking recent event registrations..."
PGPASSWORD="$DB_PASS" psql -h "$DB_SERVER" -U "$DB_USER" -d "$DB_NAME" -c "
SELECT 
    er.id,
    er.status,
    er.payment_status,
    er.created_at,
    er.updated_at,
    e.title as event_title
FROM event_registrations er
JOIN events e ON er.event_id = e.id
ORDER BY er.updated_at DESC
LIMIT 5;
" 2>/dev/null

echo ""
echo "4. Checking if tickets were generated..."
PGPASSWORD="$DB_PASS" psql -h "$DB_SERVER" -U "$DB_USER" -d "$DB_NAME" -c "
SELECT 
    t.id,
    t.qr_code,
    t.issued_at,
    er.status as registration_status,
    e.title as event_title
FROM tickets t
JOIN event_registrations er ON t.registration_id = er.id
JOIN events e ON er.event_id = e.id
ORDER BY t.issued_at DESC
LIMIT 5;
" 2>/dev/null

