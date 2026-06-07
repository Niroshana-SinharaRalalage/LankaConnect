# Migration Rollback Runbook (Staging + Production)

> **When to use this**: a migration was deployed to staging (or production), it
> caused a broken state (schema corruption, silent FK breakage, table not where
> expected, app throwing "relation does not exist"), and you need to restore the
> database to its pre-migration shape.
>
> **When NOT to use this**: small recoverable issues that can be fixed forward
> with a follow-up migration. Use PITR only when forward-fix is materially riskier
> than restore.

## What's available

Azure Postgres Flexible Server has **Point-in-Time Restore (PITR)** enabled by
default. Retention is 7 days. The restore operation creates a NEW Postgres server
from a backup snapshot at the specified timestamp — it does not mutate the existing
server. Cost: free during the retention window. No additional configuration was
needed.

## Pre-migration ritual (do this BEFORE every schema migration)

Capture the exact UTC timestamp BEFORE you push the migration commit to `develop`.
Drop it into the commit body. Example:

```
Phase 2 PR-1: Media schema rename — events.photo_albums → media.photo_albums

Pre-migration restore point: 2026-06-07T20:15:30Z

Idempotent SQL:
\`\`\`sql
ALTER TABLE events.photo_albums SET SCHEMA media;
ALTER TABLE events.album_photos SET SCHEMA media;
\`\`\`
```

That timestamp is your rollback target if the migration goes sideways.

## Staging rollback procedure

```bash
# 1. Identify the source server name and resource group
az postgres flexible-server list \
    --query "[?contains(name, 'staging')].{Name:name, RG:resourceGroup}" \
    -o table

# 2. Restore to a new server at the pre-migration timestamp
az postgres flexible-server restore \
    --resource-group <rg-from-step-1> \
    --name lankaconnect-staging-rollback \
    --source-server <source-name-from-step-1> \
    --restore-time "2026-06-07T20:15:30Z"

# 3. Wait ~5-10 minutes for the restore to complete.
#    Check status with:
az postgres flexible-server show \
    --resource-group <rg> \
    --name lankaconnect-staging-rollback \
    --query "state"

# 4. Validate the restored server has the pre-migration schema:
psql "host=lankaconnect-staging-rollback.postgres.database.azure.com \
    user=<admin> dbname=lankaconnect sslmode=require" \
    -c "\dt events.photo_albums"   # or whatever was affected
```

**Two options for finishing the rollback**:

### Option A — point staging API at the restored server (preferred — preserves
audit trail of what went wrong on the original).

```bash
# Get the connection string for the restored server
NEW_HOST="lankaconnect-staging-rollback.postgres.database.azure.com"

# Update the staging Container Apps environment variable
az containerapp update \
    --name lankaconnect-api-staging \
    --resource-group <rg> \
    --set-env-vars "ConnectionStrings__DefaultConnection=Host=$NEW_HOST;..."

# Container Apps will roll a new revision. Verify health:
curl https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/health

# After post-mortem: rename the original "broken" server to *-quarantine for
# inspection, rename the restored *-rollback to the original production name,
# update env var back to the original name.
```

### Option B — restore the original server in place (faster but destroys the
broken state — only choose if root cause is already understood).

```bash
# Stop the original server first to prevent writes during the swap
az postgres flexible-server stop \
    --name <source-name> \
    --resource-group <rg>

# Use Azure portal "Restore" action on the source server, pointing to the
# pre-migration timestamp. Original server is overwritten.
# (No CLI equivalent for in-place restore as of 2026; use portal.)
```

## Production rollback procedure

Identical to staging, but:

1. **Always Option A** (restore alongside, not in-place). Production traffic
   must keep flowing during the swap.
2. Use Azure Container Apps revision traffic-weight shift to move 100% of traffic
   to a new revision pointing at the restored server:

   ```bash
   az containerapp revision copy \
       --name lankaconnect-api \
       --resource-group <prod-rg> \
       --set-env-vars "ConnectionStrings__DefaultConnection=Host=$RESTORED_HOST;..."

   # Get the new revision name
   NEW_REV=$(az containerapp revision list \
       --name lankaconnect-api \
       --resource-group <prod-rg> \
       --query "[0].name" -o tsv)

   # Shift 100% of traffic to the new revision
   az containerapp ingress traffic set \
       --name lankaconnect-api \
       --resource-group <prod-rg> \
       --revision-weight latest=0 $NEW_REV=100
   ```

3. **Post-incident write-up is mandatory** for production rollbacks. Even if
   it's "the schema rename clipped a wrong column," the root-cause + the
   discipline-gap that allowed it past CI lint must be captured in
   `docs/operations/incidents/YYYY-MM-DD-<slug>.md`.

## Rollback decision tree (single-page summary)

```
Was the migration destructive (DropTable/RenameTable/etc.)?
├── YES — PITR is the only safe rollback. Use it.
└── NO  — Was the issue purely additive (extra column ignored)?
         ├── YES — Forward-fix with another additive migration. Skip PITR.
         └── NO  — PITR.

Is the broken environment STAGING?
├── YES — Use Option A or Option B depending on whether root cause is known.
└── PROD — Always Option A. Always post-mortem.
```

## What this runbook does NOT cover

- Data-only corruption (bad UPDATE that mutates rows but doesn't change schema)
  — Standard `pg_restore` from the most recent automated backup is appropriate
  there, not PITR.
- Connection-string leakage / credential rotation — separate runbook.
- Container Apps revision rollback for purely-code issues (no DB involvement) —
  see `docs/operations/code-rollback.md` (not yet written).

## Last-known restore points

Maintain a brief running log here of the most recent rollback events so the
on-call has a fresh example:

| Date (UTC) | Env | Trigger | Restore point used | Outcome |
|---|---|---|---|---|
| _(no events yet — this runbook was created 2026-06-07 alongside the Phase 0 migration discipline rollout)_ | | | | |
