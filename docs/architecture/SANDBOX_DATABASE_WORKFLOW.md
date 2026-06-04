# Sandbox Database Workflow

**Purpose**: dry-run destructive EF Core migrations against a sibling Postgres database BEFORE applying them to the live staging database. Pays off starting Phase A W4.2 (Media's column rename) and is essential for W7-W9.5 (Events) + W9 (Money refactor).

**Cost**: **$0 marginal Azure spend.** The sandbox is a second database on the EXISTING `lankaconnect-staging-db` Postgres flexible server (not a separate Azure resource).

## What's destructive vs additive

Phase A migrations fall into two buckets:

| Bucket | What it does | Sandbox needed? |
|---|---|---|
| **Additive** | `CREATE TABLE`, `CREATE INDEX`, baseline empty-Up | No — safe to apply directly to staging |
| **Destructive** | `RENAME COLUMN`, `ALTER COLUMN ... SET NOT NULL`, `DROP TABLE`, schema-shape reshape, NOT-NULL backfill scripts | **Yes — dry-run on sandbox first** |

Examples of destructive migrations ahead:
- **W4.2 Media**: `EventId → OwnerEntityId` rename + `OwnerEntityType` column add + UPDATE backfill
- **W5 Forms**: ownership-generalization shape change
- **W7-W9.5 Events**: 60+ entity moves with potential snapshot drift
- **W9 Money refactor**: per ADR-005, 3-migration sequence (nullable → backfill → tighten NOT NULL) across many tables

## One-time setup

```bash
# 1. Start Docker Desktop (the scripts use postgres:15-alpine for portable psql)
# 2. Provision the sandbox database
./tools/sandbox-create.sh
```

This:
- Creates `lankaconnect_sandbox` as a sibling database on `lankaconnect-staging-db`
- Copies the schema-only (no data) from `LankaConnectDB`
- Stores the connection string in Key Vault as `STAGING-SANDBOX-DATABASE-CONNECTION-STRING`

Idempotent — safe to re-run. Use `--recreate` to drop and start fresh.

## Per-migration workflow

When you're about to ship a destructive migration:

```bash
# 1. Refresh sandbox schema from current staging (kills sandbox state)
./tools/sandbox-refresh.sh

# 2. Apply the new module's pending migrations against sandbox
./tools/sandbox-test-migration.sh <DbContextName> <InfraProjectPath>
```

Example:
```bash
./tools/sandbox-test-migration.sh NotificationsDbContext src/Modules/Notifications/Notifications.Infrastructure
```

The script prints two schema diffs:
1. **Sandbox BEFORE → AFTER**: shows exactly what the migration changed
2. **Primary current vs Sandbox AFTER**: should be IDENTICAL except for the new module's tables. Any other diff is unintended drift.

If diff #2 is clean → safe to deploy the migration to real staging.
If diff #2 shows unexpected changes → fix the migration BEFORE the live staging deploy.

## When to skip the sandbox

For purely additive migrations (the Notifications W3.5b operational tables pattern: `CREATE TABLE notifications.outbox` etc), the sandbox check is belt-and-braces only. You can run those directly against staging.

The discipline: use the sandbox **whenever the migration has at least one of**:
- `RenameColumn`
- `AlterColumn` (especially `SET NOT NULL`)
- `DropTable`
- `DropColumn`
- Any raw `migrationBuilder.Sql("UPDATE ...")` or `migrationBuilder.Sql("ALTER ...")` block

## How this fits the Phase A plan

Per [Master TODO §W3.5](../MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md) the original architect-approved plan called for a `migra` schema-diff against a "staging clone". That's exactly this sandbox — same instance, different database, same effect, $0 cost. The W3.5c "deferred to a real staging cycle" item lands here via `./tools/sandbox-test-migration.sh NotificationsDbContext ...` against the sandbox.

## Why same-instance vs separate Postgres server

Considered + rejected: a separate `lankaconnect-staging-sandbox-db` Postgres flexible server (~$20/mo).

Why same-instance won:
- **$0 marginal cost** vs ~$20/mo
- **Same SKU** = identical query-planner behaviour (sandbox dry-run truly representative)
- **Backups already configured** on the parent server
- **No DNS / Key Vault / Container Apps secret routing** complexity

Trade-off accepted: a CPU-bound migration on sandbox briefly slows the primary database. Staging primary serves dev-only traffic so this is acceptable. If staging ever becomes user-facing or carries high test traffic, revisit.
