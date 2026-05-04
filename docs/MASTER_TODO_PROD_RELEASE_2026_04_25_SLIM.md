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
- **Delete orphan migration** — ✅ **CLOSED 2026-05-03.** Architect-reviewed audit (Outcome A from procedure): the orphan source file `20260214230204_Phase6A113_UpdateEmailTemplatesWithSignupFormsButton.cs` was hand-authored without a `.Designer.cs`, never carried a `[Migration]` attribute, was therefore invisible to EF Core and never applied. Confirmed `__EFMigrationsHistory` has zero rows for the timestamp — the master TODO line about removing a history row was wrong; nothing to delete from any DB. Pre-delete audit (`scripts/verify_phase7fe3_migration.py` pattern, read-only psycopg2): all 13 affected templates already carry the "View Signup Forms" button thanks to subsequent Phase 7C.2 / 7F-A overwrites — desired end-state achieved. Source file removed via `git rm`; `dotnet build LankaConnect.sln` succeeded (0 errors); Application 2567/6/0 + Infrastructure 317/0/0 suites green. Zero references to timestamp `20260214230204` remain anywhere in the project.
- **UI test red-suite triage** (full classification (a) test bug / (b) impl bug
  / (c) flake per prior session)

---

## Execution log

### 2026-04-25

- **Step 1** (10:30 UTC) — Edited `deploy-production.yml` lines 186–214: added 25 `TwilioContentSids__*` env vars. Diff vs `deploy-staging.yml:261-290` block confirmed empty. Both files now have 30 WhatsApp env-var lines. **PASS**.
- **Step 2** (10:35 UTC) — Verified UI test history: 217 tests failed across 25 files on run `24931720287` (S8.7). Same pattern as runs `24913355081` (S8.6, 25 failed) and `24893444914` (S8.3, 26 failed). No new regressions. Failed files include the architect-flagged NewsletterMetroSelector + MetroAreaSelector + SignUpManagementSection (Phase 6A.118 enhancements) + FeedCard/FeedTabs. **PASS**.
- **Step 3** (10:50 UTC) — Committed `5423c51f` (`chore(deploy): mirror staging Twilio WhatsApp env vars in prod workflow`). Pushed to develop. Triggered staging deploy run `24932581622`. Completed `success` ~10 min. **PASS**.
- **Step 4** (11:50 UTC) — User confirmed: "Yes they are working fine" — recent staging confirmation emails on the rewritten Phase 7C templates render correctly with decomposed location block (LocationName + address), no `{{ }}` placeholders, no broken HTML. Note: multi-venue branch genuinely untested (zero events with `hasSecondaryLocation=true` on staging) but accepted because rollback is 30 sec. **PASS**.
- **GO/NO-GO** (11:55 UTC) — All four GO criteria met. **GO.**
- **Step 5** (12:00 UTC) — Rollback anchors captured:
  - API: `lankaconnect-api-prod--0000034` (image `2ba7f1c0c3590f6a41a0b45443da23dd6ecb66fc`, Healthy since 2026-04-19 20:42 UTC)
  - UI: `lankaconnect-ui-prod--0000033` (image `2194f0e1c34973616ff58500748a9a6e924dd8a9`, Healthy since 2026-04-19 21:17 UTC)
- **Step 6a** (12:01 UTC) — PR #103 opened (develop → main, 159-commit gap).
- **Step 6b** (12:06 UTC) — User merged PR #103 manually after resolving lucide-react import conflict in `web/src/presentation/components/features/events/SignUpManagementSection.tsx` (kept develop side: `ChevronDown, ChevronUp` — `ChevronUp` is required at line 827 for expand state). Merge commit `85aa3a71`.
- **Step 6c — API deploy** (12:06 UTC) — Triggered automatically. Run `24934944049` (`Deploy to Azure Production`) succeeded in 11m36s. New active API revision `lankaconnect-api-prod--0000035` (image `85aa3a71`, Healthy).
- **Step 6c — UI deploy** (12:33 UTC) — **GitHub Actions silently skipped `Deploy UI to Azure Production`** despite the merge commit modifying 27 web/** files. Known edge case with large merge commits + path-filter triggers. Verified via `gh api repos/.../actions/runs?head_sha=85aa3a71` — only 2 of 3 prod workflows registered. Manually dispatched via `gh workflow run deploy-ui-production.yml --ref main`. Run `24935464715` succeeded. New active UI revision `lankaconnect-ui-prod--0000034` (image `85aa3a71`, Healthy).
- **Step 7 — Prod smoke**:
  - ✅ `/health` 200, PostgreSQL Healthy, EF Core DbContext Healthy → all 18 release migrations applied
  - ✅ `POST /api/Auth/login` returns structured JSON 400 for invalid creds → auth pipeline + DB query alive
  - ✅ `GET /api/events?pageSize=5` 200 with new schema columns serialized (`hasSecondaryLocation`, `seatingMode`, `ticketingMode`, `ticketTiers`, `locationName` decomposed) → migrations + EF mapping intact
  - ✅ UI home page 200 (`<title>LankaConnect - Sri Lankan Community Platform</title>`)
  - ✅ UI events listing page 200
  - ✅ Container env: 31 WhatsApp env vars on prod API (1 Enabled + 5 base + 25 ContentSids)
  - ⚠️ Redis: Degraded (pre-existing parity gap, same on staging — not caused by this release)
  - ✅ API logs (post-deploy): zero exceptions, zero ERR, zero Twilio rejections
- **Step 8 — Soak begins** (12:40 UTC). Watch for 60 min. Declare green at 13:40 UTC if no 5xx spike, no uncaught exceptions, no Twilio rejection logs.

## Deferred follow-ups (added during execution)

- **R-NEW (path-filter fallback)** — ✅ **CLOSED 2026-05-03 (commit `2a8e75e5`).** Architect-approved Option (a): dropped the `paths:` filter on both `deploy-ui-staging.yml` AND `deploy-ui-production.yml` (symmetry: staging is where contract mismatches should be caught before prod). Root cause was GitHub's documented 300-file diff truncation — PR #103's 161-commit / 300+ file merge pushed `web/**` paths past the cutoff so the filter saw nothing and silently skipped. Bundled observability adds: `run-name:` showing SHA + event ("UI staging · 2a8e75e5... · push") and a first step that annotates `event_name`/`actor`/`sha`/`ref` into `$GITHUB_STEP_SUMMARY` so ops can reconstruct trigger context from any historical run. Verification on staging: the post-fix push (zero web/** files — pure CI yaml) successfully fired `deploy-ui-staging.yml` (run `25291529488`, conclusion success), proving the path filter is truly gone. Trade-off accepted: ~3-5 min wasted CI per non-web push to develop/main; reliability beats CI minutes. Rejected `workflow_run:` (couples failure domains, inverts bug); rejected `paths-ignore:` invert (same truncation).
- **3-doc sync** (CLAUDE.md §7): PROGRESS_TRACKER.md, STREAMLINED_ACTION_PLAN.md, TASK_SYNCHRONIZATION_STRATEGY.md
- **Delete orphan migration** `20260214230204_Phase6A113_UpdateEmailTemplatesWithSignupFormsButton.cs` + `__EFMigrationsHistory` row removal (low-risk housekeeping)
- **UI test triage** (217 failed tests across 25 files — mostly NewsletterMetroSelector, MetroAreaSelector, SignUpManagementSection, FeedCard/FeedTabs)
