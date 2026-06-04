#!/usr/bin/env bash
# Provision the `lankaconnect_sandbox` database on the EXISTING
# `lankaconnect-staging-db` Postgres flexible server. $0 marginal Azure cost —
# this is a second database on the same instance, fully schema-isolated from
# the live staging app's `LankaConnectDB` database.
#
# Use case: dry-run destructive EF migrations (column renames, NOT-NULL adds,
# table drops, schema reshapes) against a sandbox copy before applying them to
# the live staging database. Pays off starting W4.2 (Media's
# EventId→OwnerEntityId rename) and is essential for W7-W9.5 (Events) + W9
# (Money refactor).
#
# Dependencies:
# - Azure CLI (logged in to a subscription with Key Vault GET access to
#   lankaconnect-staging-kv)
# - Docker (for the portable postgres:15-alpine client image — no local psql install required)
#
# Usage:
#   ./tools/sandbox-create.sh                 # idempotent; safe to re-run
#   ./tools/sandbox-create.sh --recreate      # drops + re-creates the sandbox DB (loses sandbox state)
#
# Side effects:
# - Creates database `lankaconnect_sandbox` on the staging Postgres server
# - Copies schema-only from `LankaConnectDB` into the sandbox (no data)
# - Stores connection string in Key Vault as STAGING-SANDBOX-DATABASE-CONNECTION-STRING
set -euo pipefail

KEY_VAULT="lankaconnect-staging-kv"
PRIMARY_SECRET="DATABASE-CONNECTION-STRING"
SANDBOX_SECRET="STAGING-SANDBOX-DATABASE-CONNECTION-STRING"
SANDBOX_DB="lankaconnect_sandbox"
RECREATE=0

for arg in "$@"; do
    case "$arg" in
        --recreate) RECREATE=1 ;;
        -h|--help)
            sed -n '2,/^set/p' "$0" | sed 's/^# \{0,1\}//'
            exit 0
            ;;
    esac
done

echo "==> Fetching primary connection string from Key Vault..."
PRIMARY_CONN=$(az keyvault secret show --vault-name "$KEY_VAULT" --name "$PRIMARY_SECRET" --query value -o tsv)

# Parse the Npgsql connection string into individual components.
PG_HOST=$(python -c "import re; m=re.search(r'Host=([^;]+)', '''$PRIMARY_CONN'''); print(m.group(1))")
PG_PORT=$(python -c "import re; m=re.search(r'Port=([^;]+)', '''$PRIMARY_CONN'''); print(m.group(1) if m else '5432')")
PG_USER=$(python -c "import re; m=re.search(r'Username=([^;]+)', '''$PRIMARY_CONN'''); print(m.group(1))")
PG_PASS=$(python -c "import re; m=re.search(r'Password=([^;]+)', '''$PRIMARY_CONN'''); print(m.group(1))")
PG_DB=$(python -c "import re; m=re.search(r'Database=([^;]+)', '''$PRIMARY_CONN'''); print(m.group(1))")

echo "    host=$PG_HOST port=$PG_PORT user=$PG_USER primary=$PG_DB"

# Use Docker-hosted psql so this script needs zero local Postgres install.
PSQL_CMD="docker run --rm -e PGPASSWORD=$PG_PASS postgres:15-alpine psql"
PG_DUMP_CMD="docker run --rm -e PGPASSWORD=$PG_PASS postgres:15-alpine pg_dump"

echo "==> Checking Docker daemon..."
if ! docker info >/dev/null 2>&1; then
    echo "ERROR: Docker daemon is not running. Start Docker Desktop and re-run." >&2
    exit 2
fi

if [[ $RECREATE -eq 1 ]]; then
    echo "==> --recreate flag: dropping $SANDBOX_DB if it exists..."
    $PSQL_CMD -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -d postgres \
        -c "DROP DATABASE IF EXISTS $SANDBOX_DB;"
fi

echo "==> Ensuring $SANDBOX_DB exists..."
$PSQL_CMD -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -d postgres \
    -tAc "SELECT 1 FROM pg_database WHERE datname = '$SANDBOX_DB';" \
    | grep -q '^1$' \
    || $PSQL_CMD -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -d postgres \
        -c "CREATE DATABASE $SANDBOX_DB;"

echo "==> Schema-only dump from $PG_DB → $SANDBOX_DB..."
# --schema-only: no data copy; this is a fast structural mirror.
# --no-owner --no-privileges: avoid GRANTs that reference users not in sandbox.
$PG_DUMP_CMD -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -d "$PG_DB" \
    --schema-only --no-owner --no-privileges \
    | $PSQL_CMD -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -d "$SANDBOX_DB" \
        --quiet \
        --set ON_ERROR_STOP=on 2>&1 \
    | tail -5

echo "==> Storing sandbox connection string in Key Vault as $SANDBOX_SECRET..."
SANDBOX_CONN="Host=$PG_HOST;Port=$PG_PORT;Database=$SANDBOX_DB;Username=$PG_USER;Password=$PG_PASS;Ssl Mode=Require;Trust Server Certificate=true"
az keyvault secret set --vault-name "$KEY_VAULT" --name "$SANDBOX_SECRET" \
    --value "$SANDBOX_CONN" --query "{name:name, version:attributes.version}" -o json | tail -5

echo ""
echo "==> Done."
echo "    Sandbox DB: $SANDBOX_DB on $PG_HOST"
echo "    Conn string: Key Vault secret '$SANDBOX_SECRET'"
echo "    To refresh sandbox schema from primary: ./tools/sandbox-refresh.sh"
echo "    To test a migration: ./tools/sandbox-test-migration.sh <Context> <ProjectPath>"
