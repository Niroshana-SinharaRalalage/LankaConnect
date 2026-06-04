#!/usr/bin/env bash
# Apply a `dotnet ef database update` against the sandbox database and emit
# a schema-diff vs the primary staging database. Use BEFORE deploying a
# destructive migration to real staging.
#
# Workflow:
#   1. (Optional) Refresh sandbox schema from current staging
#      ./tools/sandbox-refresh.sh
#   2. Apply the new module's pending migrations against sandbox
#      ./tools/sandbox-test-migration.sh <DbContextName> <InfraProjectPath>
#   3. Inspect the schema-diff this script prints
#
# Example:
#   ./tools/sandbox-test-migration.sh NotificationsDbContext src/Modules/Notifications/Notifications.Infrastructure
#
# Dependencies:
# - Azure CLI (Key Vault GET access)
# - dotnet ef tool (installed globally)
# - Docker (postgres:15-alpine image — used as portable psql/pg_dump)
set -euo pipefail

if [[ $# -lt 2 ]]; then
    sed -n '2,/^set/p' "$0" | sed 's/^# \{0,1\}//'
    exit 2
fi

CONTEXT="$1"
PROJECT="$2"
KEY_VAULT="lankaconnect-staging-kv"
PRIMARY_SECRET="DATABASE-CONNECTION-STRING"
SANDBOX_SECRET="STAGING-SANDBOX-DATABASE-CONNECTION-STRING"

PRIMARY_CONN=$(az keyvault secret show --vault-name "$KEY_VAULT" --name "$PRIMARY_SECRET" --query value -o tsv)
SANDBOX_CONN=$(az keyvault secret show --vault-name "$KEY_VAULT" --name "$SANDBOX_SECRET" --query value -o tsv)

PG_HOST=$(python -c "import re; m=re.search(r'Host=([^;]+)', '''$PRIMARY_CONN'''); print(m.group(1))")
PG_PORT=$(python -c "import re; m=re.search(r'Port=([^;]+)', '''$PRIMARY_CONN'''); print(m.group(1) if m else '5432')")
PG_USER=$(python -c "import re; m=re.search(r'Username=([^;]+)', '''$PRIMARY_CONN'''); print(m.group(1))")
PG_PASS=$(python -c "import re; m=re.search(r'Password=([^;]+)', '''$PRIMARY_CONN'''); print(m.group(1))")
PG_DB_PRIMARY=$(python -c "import re; m=re.search(r'Database=([^;]+)', '''$PRIMARY_CONN'''); print(m.group(1))")
PG_DB_SANDBOX=$(python -c "import re; m=re.search(r'Database=([^;]+)', '''$SANDBOX_CONN'''); print(m.group(1))")

if ! docker info >/dev/null 2>&1; then
    echo "ERROR: Docker daemon is not running. Start Docker Desktop and re-run." >&2
    exit 2
fi

PG_DUMP_CMD="docker run --rm -e PGPASSWORD=$PG_PASS postgres:15-alpine pg_dump"

echo "==> Snapshotting BEFORE-state schema from sandbox..."
$PG_DUMP_CMD -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -d "$PG_DB_SANDBOX" \
    --schema-only --no-owner --no-privileges \
    --schema-only > "/tmp/sandbox-before-$CONTEXT.sql"

echo "==> Applying migrations: dotnet ef database update --context $CONTEXT..."
dotnet ef database update \
    --context "$CONTEXT" \
    --project "$PROJECT" \
    --startup-project "$PROJECT" \
    --connection "$SANDBOX_CONN" \
    --verbose 2>&1 | tail -20

echo "==> Snapshotting AFTER-state schema from sandbox..."
$PG_DUMP_CMD -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -d "$PG_DB_SANDBOX" \
    --schema-only --no-owner --no-privileges \
    > "/tmp/sandbox-after-$CONTEXT.sql"

echo "==> Snapshotting current primary schema..."
$PG_DUMP_CMD -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -d "$PG_DB_PRIMARY" \
    --schema-only --no-owner --no-privileges \
    > "/tmp/primary-current-$CONTEXT.sql"

echo ""
echo "==> Diff (sandbox before → after the migration):"
diff -u "/tmp/sandbox-before-$CONTEXT.sql" "/tmp/sandbox-after-$CONTEXT.sql" | head -100 || true
echo ""
echo "==> Diff (sandbox after vs primary current) — should be IDENTICAL except for the new module's tables:"
diff -u "/tmp/primary-current-$CONTEXT.sql" "/tmp/sandbox-after-$CONTEXT.sql" | head -100 || true
echo ""
echo "==> Full diffs saved:"
echo "    /tmp/sandbox-before-$CONTEXT.sql"
echo "    /tmp/sandbox-after-$CONTEXT.sql"
echo "    /tmp/primary-current-$CONTEXT.sql"
echo ""
echo "==> Done. If the second diff shows ONLY the expected new tables, you're safe to deploy."
