#!/usr/bin/env bash
# Refresh the `lankaconnect_sandbox` database SCHEMA from the current
# `LankaConnectDB` on the staging server. Existing sandbox data is wiped
# (this is a structural mirror, not a data backup).
#
# Run before a migration dry-run to ensure the sandbox starts from the
# current production-equivalent schema state.
#
# Dependencies:
# - Azure CLI (Key Vault GET access to lankaconnect-staging-kv)
# - Docker (postgres:15-alpine image)
#
# Usage:
#   ./tools/sandbox-refresh.sh
set -euo pipefail

KEY_VAULT="lankaconnect-staging-kv"
PRIMARY_SECRET="DATABASE-CONNECTION-STRING"
SANDBOX_DB="lankaconnect_sandbox"

PRIMARY_CONN=$(az keyvault secret show --vault-name "$KEY_VAULT" --name "$PRIMARY_SECRET" --query value -o tsv)
PG_HOST=$(python -c "import re; m=re.search(r'Host=([^;]+)', '''$PRIMARY_CONN'''); print(m.group(1))")
PG_PORT=$(python -c "import re; m=re.search(r'Port=([^;]+)', '''$PRIMARY_CONN'''); print(m.group(1) if m else '5432')")
PG_USER=$(python -c "import re; m=re.search(r'Username=([^;]+)', '''$PRIMARY_CONN'''); print(m.group(1))")
PG_PASS=$(python -c "import re; m=re.search(r'Password=([^;]+)', '''$PRIMARY_CONN'''); print(m.group(1))")
PG_DB=$(python -c "import re; m=re.search(r'Database=([^;]+)', '''$PRIMARY_CONN'''); print(m.group(1))")

PSQL_CMD="docker run --rm -e PGPASSWORD=$PG_PASS postgres:15-alpine psql"
PG_DUMP_CMD="docker run --rm -e PGPASSWORD=$PG_PASS postgres:15-alpine pg_dump"

if ! docker info >/dev/null 2>&1; then
    echo "ERROR: Docker daemon is not running. Start Docker Desktop and re-run." >&2
    exit 2
fi

echo "==> Wiping sandbox schemas (DROP SCHEMA ... CASCADE for every non-system schema)..."
$PSQL_CMD -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -d "$SANDBOX_DB" -tA -c "
SELECT 'DROP SCHEMA IF EXISTS \"' || nspname || '\" CASCADE;'
FROM pg_namespace
WHERE nspname NOT IN ('pg_catalog', 'information_schema', 'public')
  AND nspname NOT LIKE 'pg_%';
SELECT 'DROP TABLE IF EXISTS public.\"' || tablename || '\" CASCADE;'
FROM pg_tables WHERE schemaname = 'public';
" | grep -v '^$' | $PSQL_CMD -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -d "$SANDBOX_DB" --quiet --set ON_ERROR_STOP=on > /dev/null

echo "==> Schema-only dump from $PG_DB → $SANDBOX_DB..."
$PG_DUMP_CMD -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -d "$PG_DB" \
    --schema-only --no-owner --no-privileges \
    | $PSQL_CMD -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -d "$SANDBOX_DB" \
        --quiet --set ON_ERROR_STOP=on 2>&1 \
    | tail -5

TABLE_COUNT=$($PSQL_CMD -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -d "$SANDBOX_DB" \
    -tAc "SELECT count(*) FROM pg_tables WHERE schemaname NOT IN ('pg_catalog', 'information_schema');")
echo "==> Refresh complete. Sandbox has $TABLE_COUNT tables."
