# Feature Flag Registry

**Single source of truth for every `Microsoft.FeatureManagement` flag in LankaConnect.**

Governed by [ADR-004 — Feature Flag Strategy](architecture/ADR-004-feature-flag-strategy.md). All flags MUST have an entry in the table below before they can be referenced in code. CI gate (`tools/check-stale-flags.sh`, planned for follow-up) will fail PRs that add flag references not registered here, and warn/fail on overdue sunset dates.

## How flags work in this codebase

- **Source**: `appsettings*.json` `FeatureManagement` section (per environment). Future work layers Azure App Configuration on top for runtime flips without redeploy.
- **Evaluation**: `IFeatureManager.IsEnabledAsync("<flag-name>")` from `Microsoft.FeatureManagement` (registered in `Program.cs` via `AddFeatureManagement()`).
- **Default-closed semantics**: per ADR-004, `Refactor.*` flags default to `false` when configuration is missing — safe for refactor cutovers. Other categories per the matrix in ADR-004 § "Flag Evaluation Outage Default".
- **Frontend distribution**: planned `GET /api/featureflags` endpoint (post-W1.5) exposes per-user flag states; cached client-side per ADR-004 § "Frontend Flag Distribution" (Ops.* TTL ≤5s, others 60s).
- **Cutover mechanism for module extractions** is Container Apps **revision-based traffic split**, NOT FeatureManagement percentage filters (per ADR-004 disambiguation). Flags gate which code path runs; revisions handle the traffic split.

## Naming convention

`<Category>.<Subject>.<Variant>`

| Category | Purpose | Lifetime | Sunset required? |
|---|---|---|---|
| `Refactor.*` | Phase A migration cutover gates | ≤ 4 weeks | YES — CI hard-fail past sunset |
| `Feature.*` | Long-lived business feature gates | Indefinite | No (annual owner re-attestation) |
| `Country.*` | Geo gating | Indefinite | No |
| `Experiment.*` | A/B tests with explicit expiry | ≤ 8 weeks | YES — CI warn past sunset |
| `Ops.*` | Kill-switches for runtime issues | Indefinite | No (annual owner re-attestation) |

## Active flags

| Flag | Category | Owner | Created | Sunset | Default (no config) | Description |
|---|---|---|---|---|---|---|
| `Refactor.Smoke.Enabled` | Refactor | Niroshana | 2026-05-12 | 2026-06-09 (W5) | `false` | Smoke flag wired in W1.5 to prove `Microsoft.FeatureManagement` infrastructure works end-to-end (configured `true` in staging via appsettings.json; evaluated by `GET /api/Health/feature-flags`). Will be removed and deleted by W5 (master TODO §"Plan Delta Amendments" table) once first real `Refactor.<Module>.*` flag exists. |
| `Refactor.Notifications.UseNewModule` | Refactor | Niroshana | 2026-06-03 | 2026-07-01 (W7) | `false` | W3 Notifications module-extraction cutover gate. When `false` (default), `INotificationRepository` resolves to the legacy implementation that injects `AppDbContext`. When `true`, resolves to the module implementation that injects `NotificationsDbContext` (per-module DbContext registered by `Notifications.Api.NotificationsModule.AddNotificationsModule`). Soak target 7 days at staging-ON before production canary ramp (10% → 50% → 100% via Container Apps revision traffic split per ADR-004). Sunsets W7 alongside legacy `NotificationRepository` removal in the cleanup PR. |

## Lifecycle checklist (per ADR-004)

When **adding** a flag:
- [ ] Add entry to the table above with name, category, owner, created date, sunset (if applicable), description
- [ ] Add to `src/LankaConnect.API/appsettings.json` `FeatureManagement` section with the staging default value
- [ ] Reference via `_featureManager.IsEnabledAsync("<flag-name>")` in code
- [ ] PR description references this registry entry

When **flipping** a flag for cutover:
- [ ] Staging flip: edit `appsettings.Staging.json` or set environment variable `FeatureManagement__<flag-name>=true`, deploy via `deploy-staging.yml`, soak 7 days
- [ ] Production flip: Container Apps revision-based traffic split (10% → 50% → 100% over 24h per ADR-004 § "Phase A Migration Pattern")

When **removing** a flag (sunset reached or feature graduated):
- [ ] Single dedicated cleanup PR (do not interleave with feature work)
- [ ] Delete flag from `appsettings*.json` `FeatureManagement` sections
- [ ] Delete the legacy code path the flag gated (the `else` branch)
- [ ] Delete the registry entry from the table above
- [ ] Update related ADRs / runbooks if behavior reference changes

## CI gate behavior (planned)

`tools/check-stale-flags.sh` (W1.5 follow-up, originally W12 in plan; moved up per ADR-004 amendment): scans this registry + greps codebase for flag references; outputs:

| Category | Past-sunset behavior | No-sunset behavior |
|---|---|---|
| `Refactor.*` | Hard fail PR | Hard fail PR (sunset mandatory) |
| `Experiment.*` | Warn PR | Warn PR (sunset mandatory) |
| `Feature.*` | n/a | Warn at annual re-attestation due date |
| `Ops.*` | n/a | Warn at annual re-attestation due date |
| `Country.*` | n/a | Reviewed at country sunset (if country exits) |

Until the script lands, manual sunset audit happens at the end of each Phase A week.

## References

- [ADR-004 — Feature Flag Strategy](architecture/ADR-004-feature-flag-strategy.md) — full design, decision rationale, cutover mechanism disambiguation
- [Master TODO — Phase A](MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md) § W1 — Execution Status — W1.5 lands the install + smoke
- `src/LankaConnect.API/Program.cs` § `AddFeatureManagement()` — DI registration
- `src/LankaConnect.API/Controllers/HealthController.cs` § `FeatureFlags()` — `GET /api/Health/feature-flags` smoke endpoint
