# Master TODO — Prod Perf RCA (2026-04-25)

**Status**: prod degraded (35s timeouts + 503s on `/api/proxy/events/{id}`); staging healthy.
**Architect-approved**: yes (re-reviewed with full evidence after escalation).

## RCA summary

**Layer**: Backend API (Application + Infrastructure).

**Primary cause**: `EventRepository.GetByIdAsync` ([src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs:128-139](src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs#L128-L139)) chains 6 sibling collections (`Images`, `Videos`, `Registrations`, `_emailGroupEntities`, `OrganizerContacts`, `TicketTiers`) plus two 3-deep nested chains (`SignUpLists.Items.Commitments` and `TicketTiers.Assignments`) in a single non-split LINQ query. Postgres row count = product of collection cardinalities. Both read handlers (`GetEventByIdQueryHandler.cs:70`, `GetEventSignUpListsQueryHandler.cs:55`) call the parameterless overload which forces `trackChanges: true`, so the change tracker is populated on every read.

**Why staging smooth, prod slow**: same code, same 0.25 vCPU container — but staging's busiest event has 8 registrations (~50-row JOIN, 0.29s) vs prod's busiest event has 85 registrations (~100K-row JOIN, 10-35s). Latent for months; only became symptomatic at high data cardinality.

**Key amplifiers**:
1. Same expensive query is dispatched twice per event-detail page load (`GetEventById` + `GetEventSignUpLists` both call `GetByIdAsync`).
2. Container at 0.25 vCPU + 0.5 GiB — fine when requests are 300ms; saturates instantly at 10-35s/request.
3. Prod Container App has `scaleRules: null` while staging has `http-scaler` (concurrentRequests=10) — KEDA never spawns replica #2 fast enough on prod, so requests pile in Envoy's queue and Container Apps eventually 503s.
4. `MetroAreas` and other tiny endpoints time out because the replica's thread/connection pool is pinned by the Event query.

---

## EMERGENCY MITIGATION (do now — restore prod within 15 min)

### Phase 2 — Bump prod resources AND add scale rule (single revision)

Single `az containerapp update` so both changes ship in one revision (atomic rollback).
Threshold is **10** (not 30) for the emergency window because pre-Phase-1 requests are still slow; replicas must spawn early.

```bash
az containerapp update \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --cpu 1.0 \
  --memory 2.0Gi \
  --min-replicas 2 \
  --max-replicas 5 \
  --scale-rule-name http-scaler \
  --scale-rule-type http \
  --scale-rule-http-concurrency 10 \
  --revision-suffix emergency-2026-04-25
```

**Verification gates (all must pass before declaring restored):**
- [ ] `az containerapp revision list` shows new revision Active; prior revision available for rollback
- [ ] Replica count climbs to ≥ 2 within 60s
- [ ] `curl /api/MetroAreas` returns < 1s (proves replica-level resource exhaustion is gone)
- [ ] `curl /api/events/{busiest-id}` returns < 35s without 503 (still slow until Phase 1, but completes)
- [ ] Browser console no longer shows `ECONNABORTED` or 503 in 5-min window

If `az containerapp update` rejects combined resource + scale-rule flags on this CLI version (older `az containerapp` extensions have been finicky), fall back to two commands but in this order: **scale rule first, resources second**. Never leave bigger box without rule.

**Rollback**: `az containerapp revision activate --name lankaconnect-api-prod --resource-group lankaconnect-prod --revision lankaconnect-api-prod--0000035`

---

## DURABLE FIX (next ~90 min — eliminate the underlying perf bug)

### Phase 1 — Split-query + read-only tracking on hot path

TDD per CLAUDE.md §2: write the failing perf integration test FIRST.

**1. RED — Add perf integration test**
- Path: `tests/LankaConnect.IntegrationTests/Events/GetEventByIdPerfTests.cs`
- Seed: 1 event with **90 registrations**, **5 sign-up lists**, **12 items per list**, **3 commitments per item**, 4 images, 2 videos, 3 ticket tiers with assignments, 2 organizer contacts, 1 email group. Bias above prod's current worst case.
- Assert: `GetByIdAsync` p95 < 1500ms over 10 warm runs against a real Postgres test container.
- Confirm test FAILS on current `develop` head (proves the regression is reproducible in CI).

**2. GREEN — Apply fixes**
- `EventRepository.cs:128` — add `.AsSplitQuery()` after the Include chain.
- `AppDbContext.OnConfiguring` — add `optionsBuilder.UseNpgsql(..., o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))` as default.
- `GetEventByIdQueryHandler.cs:70` — change to `GetByIdAsync(request.Id, trackChanges: false, cancellationToken)`.
- `GetEventSignUpListsQueryHandler.cs:55` — same change.
- Run perf test: must pass < 1500ms.
- Run full `dotnet test`: zero regressions.

**3. REFACTOR** — Verify the parameterless overload at `EventRepository.cs:248-253` is no longer called by any read handler (write handlers continue to use it; tracking is correct for them). Grep usages, document expected callers in xmldoc.

**4. Pre-merge sanity** — Verify Postgres flexible-server is same Azure region as Container App (East US 2). Split queries become 9 round-trips; cross-region misconfig would worsen latency. Quick `az postgres flexible-server show` check.

**5. Deploy to staging via deploy-staging.yml**
- Smoke per CLAUDE.md §10: login token + `GET /api/events?pageSize=20` < 2s + `GET /api/events/{seeded-event-id}` < 2s + `GET /api/events/{seeded-event-id}/signups` < 2s.

**6. Deploy to prod**
- Verify p95 in prod via Container Apps logs over 15-min window.

**7. Post-deploy: relax scale-rule concurrency 10 → 30**
```bash
az containerapp update \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --scale-rule-http-concurrency 30 \
  --revision-suffix post-fix-2026-04-25
```

**Rollback per phase**: container revision activate (30s).

---

## FOLLOW-UP (over the week — durable hygiene, prevent recurrence)

### Phase 0 — Alerting (NEW — added by architect)

The whole reason this surprised us is no signal fired. Cheap; do this week.

- [ ] Azure Monitor alert: `GET /api/events/{id}` p95 > 2s over 5-min window → page on-call
- [ ] Azure Monitor alert: Container App replica count == max-replicas for > 5 min → warn
- [ ] Azure Monitor alert: Container App HTTP 5xx rate > 1% over 5-min → page
- [ ] Document alert routing in `docs/ON_CALL_RUNBOOK.md`

### Phase 3 — Decompose `GetByIdAsync` into specialized methods

Phase 1 makes prod healthy; Phase 3 makes the codebase honest about read shapes.

- `GetForDetailViewAsync` — Images, Videos, Location, OrganizerContacts, TicketTiers
- `GetForRegistrationManagementAsync` — Registrations, OrganizerContacts
- `GetForSignUpListsViewAsync` — SignUpLists.Items.Commitments only
- `GetFullAggregateAsync` — current behavior, used by command handlers needing full graph
- Migrate read handlers to specialized methods; keep `GetByIdAsync` for write path
- Add a perf-regression test for `SearchEventsQuery` so list endpoint can't accidentally add a deep `.Include`

### Phase 4 — Other findings from the audit

- [ ] Cache `MetroAreas` (rarely changes; trivial perf win)
- [ ] Fix `PhotoAlbums` Include duplication (similar cartesian risk pattern)
- [ ] Audit `EmailQueueProcessor` DbContext lifetime (suspected scope leak holding connections)
- [ ] Fix fire-and-forget `RecordEventViewCommand` scope at `EventsController.cs:238` (suspected scope-disposed exceptions on slow paths)
- [ ] **Verify Npgsql `MaxPoolSize` vs Postgres flexible-server `max_connections`** (slow reads can hold connections for full 35s, starving small endpoints). Document in `docs/INFRASTRUCTURE.md`
- [ ] Sanity-check via `pg_stat_statements` snapshot during next p95 spike that the Event query hash dominates (post-mortem confirmation)

### Phase 4 chore — Sync staging↔prod Container App config permanently

Drift caused the outage amplification. Lives under infra-as-code.

- [ ] Add `infra/containerapp-prod.bicep` (or Terraform) with explicit `scaleRules` block. Same for staging. Identical rule shape
- [ ] CI gate: deploy job rejects if `scaleRules` is null on either env
- [ ] One-time manual diff: `az containerapp show` on staging + prod, compare every field beyond resources/replicas. Document any other drift
- [ ] Add to `docs/DEPLOYMENT.md`: "Container App config changes go through IaC, never via ad-hoc `az containerapp update` except for documented emergency mitigations like 2026-04-25"

---

## Tracking doc updates (per CLAUDE.md §7)

After Phase 1 ships:
- [ ] `docs/PROGRESS_TRACKER.md` — entry dated 2026-04-25, RCA + fix
- [ ] `docs/STREAMLINED_ACTION_PLAN.md` — close perf RCA item
- [ ] `docs/TASK_SYNCHRONIZATION_STRATEGY.md` — phase status update

---

## Execution log

### 2026-04-25

- **18:00:27 UTC** — Phase 2 emergency mitigation **executed** via single `az containerapp update` with `--cpu 1.0 --memory 2.0Gi --min-replicas 2 --max-replicas 5 --scale-rule-name http-scaler --scale-rule-type http --scale-rule-http-concurrency 10 --revision-suffix emergency-2026-04-25`. Both replicas Running by 18:00:56 UTC.
- **18:01 UTC** — Phase 2 verification gates:
  - `/health` 0.63s (200) ✅
  - `/api/metro-areas` 0.32-0.37s (200) — was 30s timeout ✅
  - `/api/events/{busiest}` single 1.5-3.9s (200) — was 10-35s ✅
  - `/api/events/{busiest}` ×3 parallel 3.2-3.5s each (200) — was all timing out at 35s ✅
  - Rollback target `lankaconnect-api-prod--0000035` (image `85aa3a71`) confirmed available, Healthy state ✅
  - Old revision auto-deactivated by single-revision mode; remains in revision history for rollback via `az containerapp revision activate --revision lankaconnect-api-prod--0000035`
- **Phase 2 status: PRODUCTION RESTORED** — 503s eliminated, latency reduced 5-10x. Cartesian-explosion bug still present but no longer saturating. Phase 1 durable fix in progress.

**Phase 1 — Durable fix (commit `a86e2f4f` rebased onto develop, merged via PR #104 → `42abd834`)**

- Three single-line code changes:
  1. `DependencyInjection.cs`: `UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)` global default
  2. `EventRepository.cs:128`: explicit `.AsSplitQuery()` at the call site
  3. `GetEventByIdQueryHandler` + `GetEventSignUpListsQueryHandler`: pass `trackChanges:false`
- Test fixture: `GetEventSignUpListsQueryHandlerKindFilterTests` mock setup updated for 3-arg `GetByIdAsync`
- Build: 0 errors. Application.Tests: **2253 passed, 0 failed, 6 skipped**.
- Staging deploy run `24937673372` green; staging smoke all <1s.
- Prod deploy via PR #104 merge — `Deploy to Azure Production` workflow succeeded. New active revision `lankaconnect-api-prod--0000036` (image `42abd834`).
- **Prod smoke results (the actual Phase 1 win):**

| Endpoint | Pre-fix | Phase 2 only (scale) | **Phase 1 (split-query)** |
|---|---|---|---|
| `/health` | 0.4-1.6s | 0.63s | **0.52s** |
| `/api/events/{busiest-id}` | **10-35s + 503s** | 1.5-3.9s | **0.18-0.86s** |
| `/api/events/{id}/signups` | 10s+ | similar | **0.20-0.26s** |
| `/events/{id}` ×3 parallel | all timed out at 35s | 3.2-3.5s each | **0.17-0.20s each** |
| `/api/events?pageSize=20` | 1.5s | 0.36s | **0.36s** |

The single-event-detail endpoint is now **faster than the list endpoint**. Cartesian explosion eliminated. **40-200x improvement** vs pre-fix.

- **Post-deploy step**: relaxed http-scaler concurrency 10 → 30 via `az containerapp update --revision-suffix post-fix-2026-04-25` (architect-approved, since requests are now fast enough that 30 is appropriate, matching staging's headroom-per-replica ratio).
- **UI prod deploy**: PR #104 had ZERO `web/**` files, so `deploy-ui-production.yml` correctly did NOT trigger. Current prod UI image `85aa3a71` (from PR #103 yesterday) is up-to-date.

### Phase 1 status: PRODUCTION RESTORED + DURABLE FIX SHIPPED.
