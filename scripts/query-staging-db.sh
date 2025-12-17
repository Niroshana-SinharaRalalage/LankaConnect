#!/bin/bash

# Query staging database via psql
# Connection details from Azure PostgreSQL Flexible Server

HOST="lankaconnect-postgres-staging.postgres.database.azure.com"
USER="lankaconnect_admin"
DATABASE="lankaconnect"
# Password needs to be provided

export PGPASSWORD="$1"

psql -h "$HOST" -U "$USER" -d "$DATABASE" -c "
SELECT
    id,
    category,
    description,
    has_mandatory_items,
    has_preferred_items,
    has_suggested_items,
    has_open_items,
    created_at
FROM events.sign_up_lists
WHERE event_id = '89f8ef9f-af11-4b1a-8dec-b440faef9ad0'
ORDER BY created_at;
"
