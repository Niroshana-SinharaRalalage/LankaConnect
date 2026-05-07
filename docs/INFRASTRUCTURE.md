# LankaConnect Infrastructure Reference

This document captures non-code operational invariants (connection pool sizing,
SKU choices, scale-rule formulas, KeyVault layout) that aren't visible in the
codebase but matter for production reliability. Living document — append entries
when something changes.

---

## Connection pool sizing — Npgsql ↔ Postgres flexible-server

### Formula

```
MaxPoolSize × peak_replica_count  ≤  max_connections × 0.8
```

Leaves 20% headroom for ad-hoc admin sessions, replication slots, EF Migrations
runner, etc. Crossing 80% utilisation under burst load causes Postgres to reject
new connections (`FATAL: sorry, too many clients already`), which manifests on
the app side as `NpgsqlException: connection lifetime exceeded`/`pool exhausted`.

### Current settings (2026-05-06)

| Environment | Server SKU | `max_connections` | Connection-string `MaxPoolSize` | Container App `min/max replicas` | Peak client conns | 80% threshold | Headroom |
|---|---|---|---|---|---|---|---|
| **staging** | Postgres 15.16 (Burstable) | **50** | 50 (dev appsettings; KV-overridden in staging) | 1 / N | 50 (1 replica) | 40 | **TIGHT** — 1 replica max under current sizing |
| **prod** | Postgres 15.16 (Burstable) | 50 (assumed; verify when next on-call) | 50 (KV) | 2 / 5 | 100 (2 replicas) – 250 (5 replicas) | 40 | **OVERFLOW** if 2+ replicas |

> **Action needed when scaling up replicas**: lower `MaxPoolSize` on the connection
> string in KeyVault (or raise Postgres `max_connections` via the Azure portal /
> server parameter override) BEFORE bumping `min_replicas`.
>
> Recommended sizing for prod 2-replica baseline: `MaxPoolSize=20` (peak
> 2×20 = 40 = 80% of 50). For 5-replica burst: raise `max_connections` to ≥125
> on a larger Postgres SKU first; **don't ship 5 replicas with current SKU**.

### Startup validator

`ConnectionPoolValidator` (Infrastructure/Services/Validation) runs once at boot
as an `IHostedService.StartAsync` and emits a structured log line:

- **Healthy**: `[ConnectionPoolValidator] [OK] Pool size has headroom: peak {Peak} <= threshold {Threshold} (server max_connections={ServerMax})`
- **Risky**: `[ConnectionPoolValidator] [POOL-OVERFLOW-RISK] Peak client connections {Peak} exceeds 80% of server max_connections ({Threshold} of {ServerMax})...`

The validator never throws or blocks startup — it's pure observability so the
log shows up in Container Apps logs every time a replica boots and ops can grep
for `POOL-OVERFLOW-RISK` to catch a misconfig before users do.

Configurable via `ConnectionPool:AssumedMaxReplicas` in appsettings (defaults
to 2 if not set).

### History

- **2026-04-25**: prod incident — `EventRepository.GetByIdAsync` 6+ Includes
  produced 100K-row cartesian JOINs that held DB connections for 10–35s. With
  pool-saturation cascading to small endpoints (MetroAreas timing out), the
  app appeared down. Phase 1 fix (split-query EF) eliminated the cartesian;
  Phase 2 (Container App scale rule + bigger box) gave headroom. **Pool-sizing
  formula was an architect-spec'd hygiene followup** to prevent recurrence as
  replicas scale up. Closed 2026-05-06 with the validator above.

---

## (Future entries: Container App scale rules, KeyVault secrets layout, etc.)
