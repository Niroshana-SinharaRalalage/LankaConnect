# LankaConnect On-Call Runbook

Living document. Update after every incident.

## Alert routing (target state — alerts to be configured in Azure Monitor)

| Alert | Severity | Window | Action |
|---|---|---|---|
| `GET /api/events/{id}` p95 > 2s | **PAGE** (P1) | 5-min rolling | Check Container App replica count + Postgres `pg_stat_activity` for long-running queries. Compare to baseline 0.18-0.86s (post-Phase-1 fix 2026-04-25). |
| Container App replica count == max-replicas for > 5 min | **WARN** | 5-min rolling | Check incoming request rate. If burst-legitimate, raise `maxReplicas` (after verifying Postgres `max_connections` headroom — see [INFRASTRUCTURE.md](INFRASTRUCTURE.md)). If queue-driven, find the slow path. |
| Container App HTTP 5xx rate > 1% | **PAGE** (P1) | 5-min rolling | Check container logs for unhandled exceptions, `ObjectDisposedException`, `NpgsqlException`, `FATAL: too many clients already`. |

> **Status**: alerts NOT YET CONFIGURED in Azure Monitor as of 2026-05-06. The
> ConnectionPoolValidator boot log (`docs/INFRASTRUCTURE.md`) covers the
> pool-overflow case observability-only; the three alerts above need portal
> setup (or `az monitor metrics alert create` + `az monitor scheduled-query
> create` automation).

## Standard incident response checklist

1. **Confirm scope**: hit `/health` + `/api/MetroAreas` + `/api/events?pageSize=20` from your laptop. If all 200 < 1s, the issue is path-specific (e.g. `/api/events/{id}` only); if all timing out, it's container-level.
2. **Check Container App revisions**: `az containerapp revision list --name lankaconnect-api-prod --resource-group lankaconnect-prod`. Confirm latest revision Active + replicas Running.
3. **Check Postgres connections**:
   ```sql
   SELECT count(*) FROM pg_stat_activity;
   SELECT pid, state, query_start, left(query, 80) FROM pg_stat_activity ORDER BY query_start ASC LIMIT 10;
   ```
   If count near `max_connections` (50 on Burstable SKU), look for long-running queries holding pool slots. The 2026-04-25 incident had `EventRepository.GetByIdAsync` queries running 10-35s; post-Phase-1 they're sub-second.
4. **Check container logs for known patterns**:
   - `[ConnectionPoolValidator] [POOL-OVERFLOW-RISK]` — pool too big for server `max_connections`. Action: lower `MaxPoolSize` in KV or raise server `max_connections`.
   - `Metric seat_conversion.race_lost` — concurrent buyer beat us to a seat (Phase 8 S8.2.C). Manual reseat or refund per S8.4 audit.
   - `Metric seat_hold.expired` Count > 100/min — unusual hold abandonment burst. Investigate frontend.
5. **Roll back if needed**: 30-sec revision activate to last-known-good:
   ```bash
   az containerapp revision activate \
     --name lankaconnect-api-prod \
     --resource-group lankaconnect-prod \
     --revision <prev-revision-name>
   ```

## Known operational ceilings

- Postgres flexible-server (Burstable): `max_connections=50` — see [INFRASTRUCTURE.md](INFRASTRUCTURE.md) connection-pool-sizing section.
- Container App scale rule: `http-scaler` concurrency 30 (prod) / 10 (staging). Beyond max-replicas, requests queue in Envoy.
- Stripe webhook retry: ~24 hours of exponential backoff. Missed webhooks self-heal via `RefundReconciliationBackgroundService` every 5 min (Phase 7G) for refunds + via `Registration.ConfirmSeatAssignments` retry on next webhook call (Phase 8 S8.2.C) for seat reservations.

## Key dashboards / queries

- **Log Analytics**: `ContainerAppConsoleLogs_CL | where ContainerAppName_s == 'lankaconnect-api-prod' | where TimeGenerated > ago(30m)`
- **`Metric` log filter**: `Log_s contains 'Metric '` — surfaces all named metrics (seat_hold.*, layout.*, seat_conversion.*, seat_reservation.*, app health/ready).

## History

- **2026-04-25 18:00 UTC** — Prod degraded with 35s timeouts + 503s on `/api/proxy/events/{id}`. Cartesian-explosion EF query under high registration count. Phase 1 split-query fix + Phase 2 emergency scale-up. Restored same day. Full RCA: [MASTER_TODO_PROD_PERF_RCA_2026_04_25.md](MASTER_TODO_PROD_PERF_RCA_2026_04_25.md).
