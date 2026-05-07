# LankaConnect Deployment Reference

Living document for deploy-time conventions, gates, and runbook entries.

---

## Container App configuration changes

**Rule (post 2026-04-25 prod incident)**: Container App configuration changes
go through Infrastructure-as-Code (`deploy-staging.yml` / `deploy-production.yml`
workflows + future `infra/containerapp-*.bicep`), **never via ad-hoc
`az containerapp update`** — except for documented emergency mitigations.

### Why this rule exists

On 2026-04-25 a prod-only `az containerapp update` had set
`scaleRules: null` while staging had `http-scaler`. The drift was invisible
in the workflow YAML because the field was never set there. Under load,
prod failed to scale (KEDA never spawned replica #2) and went into a
queue-then-503 cascade. Staging stayed healthy because its scale rule was
still active.

The fix from RCA: every config change is in version-controlled IaC, plus a
CI gate that rejects deploy jobs where `scaleRules` is null.

### Documented emergency mitigations

These are the only times `az containerapp update` ad-hoc is acceptable:

- **2026-04-25 18:00 UTC** — Phase 2 emergency mitigation: bumped CPU 0.25 → 1.0,
  memory 0.5Gi → 2Gi, min/max replicas 1/3 → 2/5, added `http-scaler`
  concurrency 10 (later relaxed to 30 post-Phase-1). Single revision
  `lankaconnect-api-prod--emergency-2026-04-25`. Rollback target preserved.
  Documented in `docs/MASTER_TODO_PROD_PERF_RCA_2026_04_25.md`.

When you do an emergency update, immediately mirror the change into the
deploy workflow YAML (in the same PR or directly after) so the next deploy
doesn't silently revert it.

---

## Pre-deploy gates

Per architect §2026-04-25 retrospective:

- [ ] **Deploy workflow YAML diff** matches the manual mitigation (no silent revert)
- [ ] **Connection pool validator** boot log shows `[OK]` (no `[POOL-OVERFLOW-RISK]`)
- [ ] **EF migrations** applied cleanly to staging before promoting to prod
- [ ] **Smoke** `/api/events` and `/api/MetroAreas` < 1s on staging post-deploy
- [ ] **Container Apps logs** clean of `ObjectDisposedException`,
  `NpgsqlException: pool exhausted`, `FATAL: too many clients` for 5 min
  post-deploy

---

## Rollback

- **Container App revision** rollback is 30 sec:
  `az containerapp revision activate --name <app> --resource-group <rg> --revision <prev>`
- **Postgres** PITR window is 7 days; rollback is 30 min (full server restore).

Always preserve the prior revision name in the deploy log before running
`az containerapp update`.
