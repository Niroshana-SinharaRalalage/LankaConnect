# Production Release — Slim Path — 2026-04-25

**Status**: IN PROGRESS
**Operator**: single
**Architect-approved**: yes (slim path reviewed and tightened 2026-04-25)
**Prior plan**: superseded — 8-phase ceremonial plan replaced by this slim path after user pushback

## Scope

Ship 158 commits and 18 EF migrations from `origin/develop` (HEAD `00ff9ad4`) to
`origin/main`, covering:

- Phase 7B Twilio WhatsApp BSP integration (replacing dormant ACS)
- Phase 7C decomposed event location email templates (12-migration REGEXP_REPLACE chain)
- Phase 7D volunteer email seeds + WhatsApp Fix 4 unverified-grace auto-disable
- Slice 8 seating canvas editor (S8.1 modal shell → S8.7 per-shape ticket tier assignment)
- Ticketing tiers + tier-aware seat picker

~1 hour wall clock total. Rollback: 30-sec Container App revision activation;
30-min Postgres PITR for data corruption.

## Risk register (residual after slim cuts)

1. Phase 7C template-rewrite chain renders broken in prod though staging looked OK — mitigated by Step 4, residual differences across 14 templates accepted because rollback is 30 sec
2. ContentSid copy-paste typo — mitigated by Step 1 diff verification
3. Migration ordering bug only at prod data volume — low; same 18 migrations applied cleanly on staging
4. Twilio account-sid/auth-token in KV are wrong values — caught by Step 7.5 WhatsApp test send

---

## Pre-flight (on develop branch, local)

### Step 1 — Edit `.github/workflows/deploy-production.yml`

Copy the WhatsApp env-var block from `deploy-staging.yml:261-290` verbatim,
append to `--replace-env-vars` block at line 186-190. Also add
`WhatsAppSettings__SandboxMode=false` (redundant with code default but keeps
prod/staging parity explicit).

- **Pass**: `diff <(grep WhatsApp deploy-staging.yml) <(grep WhatsApp deploy-production.yml)`
  shows only line-number/comment differences.
- **Duration**: 5 min.
- **On failure**: abort; do not push.

### Step 2 — Verify staging UI test history is unchanged-red

Open the latest `deploy-ui-staging.yml` run. Scan failed test names.

- **Pass**: failed names match the known-broken set from prior session (no new names).
- **Fail**: new test names appear → stop, investigate before ship.
- **Duration**: 2 min.

### Step 3 — Commit and push develop

Single commit message describing the prod workflow Twilio additions. Push.

- **Pass**: `gh run watch` on triggered `deploy-staging.yml` → green.
- **Duration**: 8–12 min for staging deploy.
- **On failure**: read logs, fix, re-push. Do not proceed.

## Staging render check

### Step 4 — One multi-venue event registration on staging

Register on a real multi-venue event using `niroshhh@gmail.com`. Open the
confirmation email in inbox.

- **Pass**: decomposed location block renders — LocationName line, primary
  address, SecondaryLocationName line, secondary address — no `{{ }}`
  placeholders, no duplicate locations, no broken HTML.
- **Fail**: any placeholder or layout corruption → stop; the 12-migration
  chain has a render bug; do not ship.
- **Duration**: 5 min.

---

## GO / NO-GO DECISION POINT

Ship if and only if all four are true:

- [ ] Step 1 deploy-production.yml diff is clean (only Twilio block + SandboxMode)
- [ ] Step 2 staging UI failures are known-broken set
- [ ] Step 3 staging deploy ran fully green
- [ ] Step 4 multi-venue email rendered correctly

**If any is false: abort. Do not merge to main.**

---

## Merge and deploy

### Step 5 — Capture previous revision name (rollback anchor)

```bash
az containerapp revision list \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --query "[?properties.active].name | [0]" -o tsv
```

Record the value. This is the rollback target.

### Step 6 — Open PR develop → main, merge, watch deploy

Merge with merge-commit (preserve history). Watch `gh run watch` on triggered
`deploy-production.yml`.

- **Pass**: all phases green; final smoke step reports `/health` 200.
- **Fail in PHASE 3 (migrations)**: the 3-retry loop will mask root cause —
  **cancel the run manually** before retry 2 starts; read PHASE 3 logs;
  decide PITR rollback vs forward-fix.
- **Fail in PHASE 4 (Container App update)**: previous revision still active
  and serving traffic; activate revision rollback (see matrix below).
- **Duration**: 10–15 min.

## Production smoke

### Step 7 — Five spot checks, in order

1. `curl /health` → 200
2. `POST /api/Auth/login` with test creds (`niroshhh@gmail.com` / `1qaz!QAZ`) → 200 + JWT
3. Register on a real single-venue event via UI → confirmation email arrives
4. Register on multi-venue event → confirmation email arrives, decomposed location correct
5. Trigger one WhatsApp template send via admin endpoint → Twilio dashboard shows
   `delivered` (not `failed` with `61135` template-not-found)

- **Pass**: all five succeed.
- **Fail on 1 or 2**: revision rollback immediately.
- **Fail on 3 or 4**: investigate; revision rollback if email broken; PITR if
  migration data corrupted.
- **Fail on 5 only**: fix Twilio env vars in next deploy; do not rollback
  (WhatsApp is fire-and-forget; users not impacted).
- **Duration**: 10 min.

## Rollback decision matrix

| Symptom | Action | Time |
|---|---|---|
| `/health` 500 or 5xx on `/api/Auth/login` | `az containerapp revision activate --revision <previous>` | 30 sec |
| Email renders broken (placeholders, layout) | revision rollback first; investigate template state separately | 30 sec |
| Migration data corruption (column missing, wrong row value) | PITR restore to pre-deploy timestamp | 30 min |
| WhatsApp send fails with `61135` template-not-found | fix env vars in next deploy; no rollback | n/a |
| Container App update never reaches Healthy | previous revision still serving; investigate without rollback | n/a |

## Post-deploy monitoring (first hour)

### Step 8 — Watch 60 minutes

```bash
az containerapp logs show \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod --follow
```

Look for:
- `[Phase 7B.4] Twilio WhatsApp send REJECTED` (means Twilio env vars wrong)
- migration retry messages
- uncaught exceptions
- any 5xx in request logs

**Declare green at T+60min** if:
- no 5xx spike
- no uncaught exceptions
- no Twilio rejection logs
- at least one organic registration succeeded

---

## Deferred follow-ups (post-green, separate commits)

- **Three-doc sync** (CLAUDE.md §7): `PROGRESS_TRACKER.md`, `STREAMLINED_ACTION_PLAN.md`,
  `TASK_SYNCHRONIZATION_STRATEGY.md`
- **Delete orphan migration**
  `20260214230204_Phase6A113_UpdateEmailTemplatesWithSignupFormsButton.cs` +
  matching `__EFMigrationsHistory` row removal (low-risk housekeeping; pair them)
- **UI test red-suite triage** (full classification (a) test bug / (b) impl bug
  / (c) flake per prior session)

---

## Execution log

(append timestamps + outcome per step as we go)
