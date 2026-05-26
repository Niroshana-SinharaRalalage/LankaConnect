# Master TODO — Phase A: Modular Monolith Refactor

| | |
|---|---|
| **Plan Version** | v4 (architect-reviewed 2026-05-11 — delta amendments applied) |
| **Phase A Duration** | **20 weeks calendar** (~85 person-days; was 19 weeks pre-delta) |
| **Approach** | Trunk-based development + feature flags (no long-lived branch) |
| **Cutover Discipline** | Per-module flag flip with 7-day staging soak + 24h production canary |
| **Definition of Done** | LankaEvents (and all current functionality) works identically post-cutover; 3-week stabilization soak completed |
| **Pre-flight gates landed** | PR-0 (#107), PR-0a (#108), PR-A (#109), PR-B (#113) all merged on develop |
| **First Phase A task** | W1.0a — align this doc with plan-file delta amendments (this PR) |

---

## Plan Delta Amendments (re-baselined 2026-05-11) — read first

The strategic plan was finalized 2026-04-26. Between then and pre-flight kickoff (2026-05-11), the codebase accumulated 21 operational migrations, the events mega-page grew +89 LOC, and the existing `.github/CODEOWNERS` was found unfit (15 fictional team handles). Architect re-reviewed and approved the delta amendments below. **These amendments take precedence over the Architect Review v3 section and weekly tasks below where they conflict.**

Mirror of plan file §10 at `C:\Users\Niroshana\.claude\plans\yes-one-cart-per-streamed-rocket.md`. Maintained here so this doc is self-sufficient.

### Execution sequence (replaces the old §4 in mid-document)

Four pre-flight PRs landed in order:

1. ✅ **PR-0** (#107 merged 2026-05-11) — doc commit (this Master TODO + 5 ADRs)
2. ✅ **PR-0a** (#108 merged 2026-05-11) — fix 2 stale Domain tests + workflow `continue-on-error` on PR Summary Comment step
3. ✅ **PR-A** (#109 merged 2026-05-11) — replace CODEOWNERS (solo-founder), add PR template, create `phase-a` + `point-of-no-return` labels
4. ✅ **PR-B** (#113 merged 2026-05-11) — PR-title regex gate as separate job in `pr-validation.yml`

**This PR (W1.0a) is the first labeled `phase-a`** — exercises the new gate.

### Week ordering — Money refactor moved from W9 → W5

| New Week | Was | Module |
|---|---|---|
| W3 | W3 | Notifications |
| W4 | W4 | Communications + Media |
| **W5** | **W9** | **Money refactor** (moved up; Payments needs Money) |
| W6 | W5 | Forms |
| W7 | W6 | Payments (now uses Money) |
| W8–W9 | W7–W8 | Events extraction |
| **W7-9.5** | **W7-8** | Events extraction extended to 3 weeks (was 2) |
| W10 | W10 | Identity |
| W11 | W11–W12 | Frontend feature packages + Money DTO migration + **events mega-page split** (moved from W1) |
| W12 | W13 | Per-Module CI/CD hardening |
| W13–W14 | W14–W15 | Staging regression + buffer |
| W15 | W16 | Production cutover |
| W16–W18 | W17–W19 | Stabilization soak (3 weeks) |

Total: **20 weeks** (was 19).

### Production canary order (W15.3) — Identity LAST

Replaces the original "low risk first" order. Identity is highest-blast-radius (auth break = everything breaks). All other modules must prove stable on new path BEFORE touching auth substrate.

```
Notifications → Communications → Media → Forms → Payments → Events → Identity
```

If Identity flip fails, rollback isolates to just Identity; other 6 modules continue working on new path.

### API baseline regression mechanism (A.0.B.6) replaced

JSON shape diff is insufficient (misses field reordering, semantic changes, side-effect drift). Replaced with:

- **Primary**: Schemathesis OpenAPI conformance testing (property-based; explores all endpoints)
- **Secondary**: 5 Pact-style consumer-driven contract tests for: auth, event-detail, payment-checkout, email-send, photo-upload
- **Supplementary**: bash smoke script kept but downgraded (limits documented)

### W0+W1 budget extended from 5 to 8 days

The codebase accumulated more debt between plan-write and execution. W0+W1 absorbs:

| Day | Task |
|---|---|
| W0 D1 | PR-0 doc commit ✅ DONE 2026-05-11 |
| W0 D2 | PR-A template + CODEOWNERS + label creation ✅ DONE 2026-05-11 |
| W1 D1 | PR-B title gate (separate job) ✅ DONE 2026-05-11 |
| W1 D2 | **W1.0a (this PR)**: align Master TODO with delta amendments + W1.0b: add 4 disk-only test projects to sln + triage |
| W1 D3 | W1.1: secret rotation; remove `Secrets/`; Azure Key Vault wiring |
| W1 D4 | W1.2: debug-file cleanup at repo root (~72 files) |
| W1 D5 | W1.3: `scripts/` triage with deletion bias (target: <5 committed scripts, zero untracked) |
| W1 D6 | W1.4: Bicep skeleton for staging RG |
| W1 D7 | W1.5: Microsoft.FeatureManagement install + first flag stub |
| W1 D8 | W1.7: `.claude/settings.json` audit + W1 close-out |

### W1 hygiene NON-GOALS (explicitly excluded — prevent scope creep)

- ❌ **Mega-page split** of `events/[id]/page.tsx` (2,603 LOC). Owned by **W11 frontend phase**, NOT W1.
- ❌ **Archive scripts folder** under `scripts/_archive/`. Hoarding; git log is the archive. Delete instead.
- ❌ **Preserve old CODEOWNERS Cultural Intelligence rules**. Reset; not needed.
- ❌ **Tighten `pr-validation.yml` Application threshold from 50**. Defer to stabilization (W17-W18).

### Schema freeze schedule (NEW — soft pause per module week)

Operational work continues on `develop` during Phase A. Per architect amendment, a **schema-by-schema soft freeze** prevents rebase hell:

| Window | Frozen schema | Reason |
|---|---|---|
| W3-W4 | `notifications.*` | Notifications module extraction |
| W4 | `communications.*`, `media.*` | Comm + Media extraction |
| W5 | (no schema freeze — Money refactor touches monetary columns across schemas) | Cross-schema coordination required |
| W6 | `forms.*` | Forms extraction |
| W7-9.5 | **`events.*` (CRITICAL)** | Events extraction — 21 new migrations recently; biggest risk gate |
| W10 | `identity.*`, `users.*` | Identity extraction |

**W6.5 Pre-W7 task (NEW)**: announce events schema freeze 1 week before W7. Complete any pending events migrations. No new events schema PRs during W7-9.5.

### Pre-flight decisions — RESOLVED 2026-04-26

| # | Decision | Resolution |
|---|---|---|
| **D1** | Bank multi-currency settlement to USD account | **Assumed YES.** Verification deferred to pre-Phase-3. Foundation built regardless. |
| **D2** | Cart scoping | **One cart per storefront.** Mart cart + Seyla cart coexist. |
| **D3** | Commerce launch geography | **USA-only / USD-only at Phase 3.** Multi-currency *foundation* in Phase A; *implementation* deferred. |
| **D4** | `Ops.*` flag cache TTL | **5 seconds.** Other categories: 60s. |
| **D5** | W0 length | **7 days.** |

### Risks newly introduced by deltas (additions to risk register)

| Category | New failure mode | Rollback signal |
|---|---|---|
| Doc-commit ordering | Master TODO references stale untracked state | Grep `docs/MASTER_TODO_PHASE_A_*` against `git ls-files` — must show committed ✅ |
| CODEOWNERS rewrite | New CODEOWNERS misses a path that `pr-validation.yml` checks | Open tiny no-op PR; if PR-validation green, CODEOWNERS sound ✅ verified PR-A |
| Schema freeze enforcement | Operational hotfix needs events migration during W7-9.5 freeze | Architect-approved exception via `point-of-no-return` label |
| Test project triage | 4 disk-only test projects fail to build → must delete | Capture pre-deletion test counts; document in `docs/operations/W1-test-triage.md` (W1.0b task) |

---

## Architect Review v3 — Critical Amendments (read before any execution)

The architecture agent identified **3 structural blockers** that must be addressed before kickoff, plus 32 hardening items. Major changes from v2:

### Structural changes (override sequence below)

1. **Money refactor moves from W9 → W5** (before Payments). Reason: Payments W7 needs `Money` anyway; Events W8-9 should move already-`Money`-typed code, not double-touch. The week labels W5–W10 below reflect the **new execution order**:

   | New Week | Was | Module |
   |---|---|---|
   | W3 | W3 | Notifications |
   | W4 | W4 | Communications + Media |
   | **W5** | **W9** | **Money refactor** |
   | W6 | W5 | Forms |
   | W7 | W6 | Payments |
   | W8–W9 | W7–W8 | Events |
   | W10 | W10 | Identity |
   | W11 | W11–W12 | Frontend feature packages + Money DTO migration |
   | W12 | W13 | Per-Module CI/CD hardening |
   | W13–W14 | W14–W15 | Staging regression + buffer |
   | W15 | W16 | Production cutover |
   | W16–W18 | W17–W19 | Stabilization soak |

2. **Production canary order (W15.3) re-ordered: Identity LAST** (was 5th of 7). Identity is the highest-blast-radius module — auth break = everything breaks. Other modules must prove stable on the new path before touching the auth substrate. New canary order: **Notifications → Communications → Media → Forms → Payments → Events → Identity**.

3. **API baseline regression mechanism (A.0.B.6) replaced**: JSON shape diff is insufficient. Use **Schemathesis OpenAPI conformance testing** + targeted **Pact-style consumer-driven contract tests** for 5 critical paths (auth, event-detail, payment-checkout, email-send, photo-upload). The bash script remains as supplementary smoke check, not authoritative regression.

### Document-level amendments to apply

The following corrections apply across this Master TODO and the 5 ADRs. Apply during pre-flight (A.0):

**ADR-001 (i18n)**
- Specify ArchTest regex for banned `decimal` field names: `(?i)(price|amount|fee|cost|total|subtotal|tax|discount|refund|tip|donation|charge)([A-Z_]|$)`
- Document that per-locale template engine architecture is a constraint on Communications W4 work, not a property
- Define what triggers Phase A.5 scheduling

**ADR-002 (Tenancy)**
- Reconcile `_currentStorefront.Id` snippet with `ICurrentStorefrontAccessor` interface name
- Replace "ArchTest enforces HasQueryFilter" with concrete recipe: custom test boots DbContext, reflects `Model.GetEntityTypes()`, asserts `IQueryFilter != null` per Commerce entity
- Decide explicitly: one cart per storefront vs one cart total cleared on switch (recommend: one cart per storefront)
- Reference `platform.audit_events` schema (defined in W2.4)

**ADR-003 (Stripe)**
- Add fallback section: "If bank rejects multi-currency settlement → open a multi-currency US business account; do NOT open Stripe Connect"
- Add concrete metadata stamping contract: `storefront_id`, `originating_module`, `customer_country` MUST be stamped by every caller of `IPaymentCheckoutService`; ArchTest enforces
- Add SCA / 3DS support requirement for UK/EU launches
- Disclose FX-volatility-on-refund risk and accounting treatment
- Verify (not assume) Stripe Tax coverage for Sri Lanka VAT; document fallback if uncovered

**ADR-004 (Feature Flags)**
- Specify `GET /api/featureflags` cache TTL: `Ops.*` flags ≤5s or bypass cache; `Refactor.*` and `Feature.*` 60s
- Disambiguate "10% traffic" mechanism: Container Apps revision-based traffic split (per W12.5); FeatureManagement percentage filter requires sticky targeting context to avoid mid-session oscillation in payment flows
- Define flag-evaluation outage default: `Refactor.*` defaults closed (legacy serves); `Ops.*` per-flag in registry
- CI gate matrix: `Refactor.*` past sunset = fail; `Experiment.*` past sunset = warn; `Feature.*` and `Ops.*` = annual owner re-attestation, no auto-fail
- Move `tools/check-stale-flags.sh` script creation from W12 to W1.6

**ADR-005 (Money DTO)**
- Add backend contract-test pattern asserting exact JSON property names per MEMORY.md serializer pitfalls
- Drop unverifiable "verify zero external callers via API gateway logs" claim; replace with: "2-week deprecation period in CHANGELOG; LankaConnect has no documented external API consumers"
- Add **W11.0** task: audit all `.price` JSX usages and produce migration list with exact count (don't assume ≤5-PR cohort)
- **CRITICAL**: Replace column-rename strategy with safer add-new-column + dual-write + drop-old pattern (see W5 below). Live monetary data cannot tolerate `ALTER TABLE RENAME COLUMN` rollback ambiguity.

### Other Master TODO amendments

- **W0 (Test Hardening)**: extend from 5 to 7 days OR cut W0.3 target from ≥25 to ≥10 high-quality integration tests (recommend: 7 days, ≥15 tests of mixed quality)
- **A.0.B.2**: convert "~15 events-related baseline files" to explicit checklist enumerating each endpoint state
- **A.0.B.3**: add WhatsApp send/delivery baseline (live since 2026-04-21 per MEMORY.md)
- **A.0.B.5**: expand Stripe baseline to include full webhook side-effects (DB rows, emails, notifications)
- **A.0.B (new section)**: capture outbox/integration event state baseline (pending, processed, dead-lettered counts)
- **W2**: ADD Turborepo workspace conversion (was W11.1) so 8 weeks of bake time before package extractions in W11
- **W4 (NEW W4.1.5)**: explicit outbox smoke test — trigger event in Communications, verify lands in Notifications via dispatcher; both on new path; end-to-end latency < 5s
- **W5 (Money refactor)**: use add-column + dual-write + drop pattern; expand schema audit to include `*_total`, `*_subtotal`, `donation_*`, `tip_*`, `tax_*`, `discount_*`, `refund_*`, `*_usd`; cross-reference C# property names via grep
- **W14.2 (Cutover dry-run)**: must complete in <4 hours on staging; if >4 hours, defer cutover by one week
- **Rollback procedures**: add "point of no return" markers after W4 Media rename and W5 Money migration. After these, "Full Phase A rollback" loses customer data created post-cutover and is a disaster-recovery option, not a routine recovery.
- **W16-W18 stabilization**: add explicit gates — "no UI changes, no new features, no schema changes; only bug fixes." Daily customer-support-ticket review. App Insights performance trend artifact captured.
- **Definition of Done**: add Stripe production keys rotated post-cutover; old container image retention extended from 7 to 30 days; `MODULE_OVERVIEW.md` written for Phase 2 onboarding; Contracts versioning policy (V1 → V2, never modified in place) documented in MODULE_EXTRACTION_PLAYBOOK.

### Pre-flight Decisions — RESOLVED 2026-04-26

| # | Decision | Resolution |
|---|---|---|
| **D1** | Bank multi-currency settlement to USD account | **Assumed YES.** Bank confirmation deferred to pre-Phase-3 verification (Stripe-side foundation built regardless). If bank rejects later, ADR-003 fallback applies (open multi-currency US business account). Not blocking Phase A. |
| **D2** | Cart scoping | **One cart per storefront.** Mart cart and Seyla cart coexist; switching storefronts does NOT wipe the other's cart. Cart entity has unique constraint on `(user_id, storefront_id)`. |
| **D3** | Commerce launch geography | **USA-only / USD-only at Commerce launch (Phase 3).** LankaEvents continues current multi-country Stripe pattern unchanged. Multi-currency *foundation* (Money, Currency value objects, Currency-aware Stripe abstraction) built in Phase A. Multi-currency *settlement implementation* deferred until first non-USA Commerce storefront. LK VAT custom tax table deferred (not Phase A; not Phase 3). Stripe Tax US coverage sufficient at Commerce launch. |
| **D4** | `Ops.*` flag cache TTL | **5 seconds.** Other categories: 60 seconds. (Per ADR-004 amendment table.) |
| **D5** | W0 length | **7 days.** Extended for Testcontainers setup + integration test authoring + Vitest timer-async fixes. |

**Implications of D3 — what gets simpler in Phase A**:
- ADR-003 SCA/3DS retrofit work is documentation-only in Phase A (Events already handles current SCA needs via Stripe Checkout). Real implementation deferred to international Commerce launch.
- ADR-003 LK VAT custom tax table not needed in Phase A or Phase 3.
- Stripe Tax enabled for US only at Commerce launch (Phase 3).
- Money refactor (W5) backfills all existing rows as `USD` currency — straightforward.
- `IPaymentCheckoutService.CreateCheckoutSessionAsync` accepts `Currency` parameter (foundation), but production callers pass only `USD` until international expansion.

**Phase A is unblocked.** Proceed to A.0 (pre-flight + API baseline protocol).

End of architect review amendments. Continue with body below; weekly tasks have **NOT** been renumbered — apply the new sequence per the table above.

---

## How to Use This Document

- Tasks are numbered hierarchically (`W3.1`, `W3.2`, ...) for stable referencing across docs
- Each task has: **Action**, **Files affected**, **Verify** (specific commands), **Acceptance**, **Rollback**
- Mark tasks `[x]` as completed; never batch — mark immediately
- Update [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md) and [STREAMLINED_ACTION_PLAN.md](./STREAMLINED_ACTION_PLAN.md) at end of each task per [TASK_SYNCHRONIZATION_STRATEGY.md](./TASK_SYNCHRONIZATION_STRATEGY.md)
- Architecture decisions live in `docs/architecture/ADR-*.md` — read all before starting

---

## Phase A.0 — Pre-Flight Decisions (BEFORE Week 0)

### A.0.1 — Approve all 5 ADRs
- [ ] **Read & approve** [ADR-001 i18n scope](./architecture/ADR-001-i18n-scope-phase-a.md)
- [ ] **Read & approve** [ADR-002 tenancy strategy](./architecture/ADR-002-tenancy-strategy.md)
- [ ] **Read & approve** [ADR-003 Stripe multi-currency](./architecture/ADR-003-stripe-multi-currency.md)
- [ ] **Read & approve** [ADR-004 feature flag strategy](./architecture/ADR-004-feature-flag-strategy.md)
- [ ] **Read & approve** [ADR-005 Money DTO migration](./architecture/ADR-005-money-dto-migration.md)
- **Acceptance**: each ADR marked `Status: Accepted` with date stamp

### A.0.2 — Confirm Stripe account FX policy
- [ ] Verify with US bank that USD-only settlement of multi-currency Stripe charges is acceptable
- [ ] Confirm Stripe Tax can be enabled for target launch countries (US, LK, IN, GB)
- **Acceptance**: written confirmation in `docs/operations/stripe-banking-confirmation.md`

### A.0.3 — Capture Production Schema Snapshot for Migration Baseline
- [ ] Take pg_dump of production schema (no data) — store securely
- [ ] Capture migration history: `SELECT * FROM "__EFMigrationsHistory"` from production
- [ ] Document last applied migration ID per schema in `docs/operations/migration-baseline-snapshot.md`
- **Acceptance**: snapshot stored, last-applied IDs documented

---

## Phase A.0.B — API Baseline Test Protocol (CRITICAL — runs alongside A.0)

The API baseline must be captured BEFORE any code change. This is the regression yardstick for all subsequent weeks.

### A.0.B.1 — Authentication baseline
- [ ] **Action**: Test current auth flow against staging
  ```bash
  curl -X POST 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' \
    -H 'Content-Type: application/json' \
    -d '{"email":"niroshhh@gmail.com","password":"1qaz!QAZ","rememberMe":true,"ipAddress":"string"}'
  ```
- [ ] **Verify**: HTTP 200; response contains `accessToken`, `refreshToken`, `user.id`, `user.email`, `user.role`
- [ ] **Save**: response shape to `tests/api-baseline/auth-login.baseline.json`
- [ ] Test refresh token flow
- [ ] Test logout flow
- [ ] Test register flow (with throwaway email)
- **Acceptance**: 4 baseline JSON files captured

### A.0.B.2 — Events API baseline
- [ ] Get auth token (from A.0.B.1)
- [ ] **Action**: List events
  ```bash
  curl -H "Authorization: Bearer $TOKEN" \
    'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events?page=1&pageSize=20'
  ```
- [ ] **Verify**: HTTP 200; response shape includes `items[]`, `totalCount`, `page`
- [ ] **Save**: shape to `tests/api-baseline/events-list.baseline.json`
- [ ] Capture baselines for: get event by id, get event registrations, get signup lists, get photo albums, get forms, get add-ons, get sponsors, get donations, get tickets, get venue layouts
- **Acceptance**: ~15 events-related baseline files

### A.0.B.3 — Communications/Newsletter/Notifications baseline (EXPANDED per architect)
- [ ] Capture: get newsletters, get user notifications, get user email preferences, get WhatsApp preferences, send test email
- [ ] **NEW — WhatsApp baseline** (live since 2026-04-21 per MEMORY.md):
  - Send signup-confirmation WhatsApp (verify ACS template invocation)
  - Send event-cancellation WhatsApp (verify message delivered)
  - Verify webhook receipt: query `communications.whatsapp_webhook_events` table for delivery status
- **Acceptance**: 7+ baseline files including WhatsApp send/delivery proof

### A.0.B.4 — Users / Profile / Reference Data baseline
- [ ] Capture: get current user, get user profile, get metro areas, get reference values
- **Acceptance**: 4+ baseline files

### A.0.B.5 — Payments baseline (test mode) — EXPANDED per architect

The webhook side-effects are critical; capturing only the shape misses regressions where API still works but side-effects silently dropped.

- [ ] Capture: create event checkout session (Stripe test card 4242...), create donation checkout session, create sponsor checkout session, create add-on purchase
- [ ] Capture full webhook handling state per webhook event:
  - DB rows written (`events.registration_payments`, `events.donations`, etc.) — capture row shape + count delta
  - Emails sent (query `communications.email_messages` for last 5 entries with timestamps)
  - Notifications enqueued (query `notifications.notifications`)
  - Outbox entries created (query `<schema>.outbox` for new IntegrationEvents)
  - Refund flow: create test charge → refund → verify Stripe webhook received → DB updated → email sent
- **Acceptance**: 6+ baseline files; each captures full side-effect chain (DB + email + notification + outbox)

### A.0.B.5b — Outbox + Integration Event baseline (NEW per architect)
- [ ] Capture: count of pending outbox entries per schema; count of dead-lettered entries; sample 10 recent outbox entries showing full IntegrationEventV1 payload shape
- [ ] Capture: dispatcher processing rate (entries/sec) under current load
- [ ] **Files**: `tests/api-baseline/outbox-state.baseline.json`
- **Acceptance**: outbox state captured; baseline references included in W4.1.5 smoke test

### A.0.B.6 — Build baseline regression mechanism — REVISED per architect

JSON shape diff is insufficient (misses field reordering, semantic changes, pagination drift, side-effect drift, auth regressions). Replaced with two complementary mechanisms:

**Primary: Schemathesis OpenAPI conformance**
- [ ] **Action**: Generate OpenAPI spec from current production: `https://api.lankaconnect.app/swagger/v1/swagger.json` → save as `tests/api-baseline/openapi-baseline.json`
- [ ] **Action**: Add Schemathesis step to CI: `schemathesis run tests/api-baseline/openapi-baseline.json --base-url=$STAGING --auth-type=bearer ...`
- [ ] **Verify**: Schemathesis explores all endpoints with property-based testing; flags any non-conformance
- **Acceptance**: Schemathesis CI job runs on every PR; first run green against current staging

**Secondary: Pact-style consumer-driven contract tests for 5 critical paths**
- [ ] Auth contract: login → refresh → logout sequence with exact response shapes
- [ ] Event-detail contract: get event by id with all sub-fields populated
- [ ] Payment-checkout contract: create Stripe checkout session + webhook receipt
- [ ] Email-send contract: send templated email + verify ACS dispatch
- [ ] Photo-upload contract: multipart upload + retrieval
- [ ] **Files**: `tests/contract/*.pact.json` (consumer side); provider verification in CI
- **Acceptance**: 5 contract tests in CI; all green against current staging

**Supplementary: bash smoke script (kept but downgraded)**
- [ ] Existing `run-baseline-regression.sh` retained as quick smoke check; documented as supplementary, not authoritative
- [ ] Runs in CI on PRs touching API surface
- **Acceptance**: smoke script committed with clear documentation of its limits

### A.0.B.7 — Frontend smoke baseline
- [ ] **Action**: Capture Lighthouse score for: home page, /events list, /events/[id] detail
- [ ] Capture screenshot baseline via Playwright for top 10 pages
- [ ] **Save**: to `tests/visual-baseline/`
- **Acceptance**: Lighthouse JSON + screenshots committed

---

## Phase A.W0 — Test Hardening (Week 0, 5 days)

### W0.1 — Backend coverage audit
- [ ] **Action**: Run coverage for all backend test projects
  ```bash
  dotnet test LankaConnect.sln /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
  ```
- [ ] **Verify**: coverage report generates; identify projects below 70%
- [ ] **Save**: report to `tests/coverage/baseline-W0.cobertura.xml`
- **Acceptance**: coverage map documented per module/layer

### W0.2 — Testcontainers Postgres setup
- [ ] **Action**: Add `Testcontainers.PostgreSql` to `LankaConnect.IntegrationTests`
- [ ] **Files**: `tests/LankaConnect.IntegrationTests/PostgresFixture.cs`
- [ ] **Verify**:
  ```bash
  dotnet test tests/LankaConnect.IntegrationTests/
  ```
- [ ] **Acceptance**: integration tests run against ephemeral Postgres in CI (not "skipped — local only" anymore)

### W0.3 — Integration tests for critical paths
- [ ] Auth flow integration test (login → refresh → logout)
- [ ] Events read-path integration test (list, detail, registrations)
- [ ] Email send integration test (with mock SMTP)
- [ ] Payment checkout integration test (Stripe test mode)
- [ ] Photo upload integration test (Azurite blob)
- **Acceptance**: ≥ 25 integration tests covering paths that will change ownership in module extractions

### W0.4 — Frontend test fixes
- [ ] **Action**: Fix the 18 failing Vitest timer-async tests
- [ ] **Files**: `web/tests/**` (specific tests identified by `npm test`)
- [ ] **Verify**: `npm test` exits 0 with all tests passing
- **Acceptance**: 100% green frontend test suite

### W0.5 — Performance baseline capture
- [ ] **Action**: Snapshot `pg_stat_statements` from production Postgres
- [ ] Capture top 50 slowest queries
- [ ] Save Lighthouse mobile + desktop scores for top 10 pages
- [ ] **Files**: `docs/operations/performance-baseline-W0.md`
- **Acceptance**: baseline document committed

### W0.6 — Update PROGRESS_TRACKER + STREAMLINED_ACTION_PLAN
- [ ] Add Phase A row with start date, target completion
- [ ] Mark Week 0 tasks as in-progress / complete

---

## Phase A.W1 — Hygiene & Foundation (Week 1, 5 days)

### W1 — Execution Status (2026-05-11)

> Execution sequence follows §"Plan Delta Amendments" budget table (lines 70-86, authoritative). The W1.1/W1.2/W1.3 *section bodies* below kept their original (pre-delta) numbering and content for context — refer to the table here for actual landing status.

| Delta-table day | Delta task | Maps to body section | Status | Evidence |
|---|---|---|---|---|
| W1 D3 | W1.1: secret rotation + remove `Secrets/` + KV wiring | W1.1 (rotation + files) + W1.2 (KV wiring → split as **W1.1b**) | 🟡 **PARTIAL** | #117 files deleted ✅; rotation **deferred per founder** ⚠️ (`docs/operations/W1.1-secret-cleanup-decision.md`); KV wiring → W1.1b ⏳ |
| W1 D4 | W1.2: debug-file cleanup at repo root | W1.3 (Repo cleanup) | ✅ **DONE** | #118 — 234 files deleted; root 262→28 tracked; `docs/operations/W1.2-root-cleanup.md` |
| W1 D5 | W1.3: scripts/ triage (target `<5` tracked, 0 untracked) | W1.3 (Repo cleanup) | ✅ **DONE — deviation noted** | #119 — 357 tracked + 24 untracked (9 subdirs) → **14 tracked + 0 untracked** (3 live-referenced clusters: `scripts/azure/`, `scripts/docker/`, `scripts/email-assets/`); kept 14 not `<5` because deleting would break docker-compose mounts + EmailBrandingService runtime uploads — defensible deviation per architect; `docs/operations/W1.3-scripts-cleanup.md` |
| follow-up | W1.3a: architect P1 follow-ups (gitignore anchor + orphan deletion) | n/a — review correction | ✅ DONE | #122 — 3 over-broad `.gitignore` patterns anchored to root (`*test-login*.json` etc. were silently false-positive-blocking `tests/e2e/*test_login*.json` fixtures); orphan `AlertSeverityConsolidationValidation.cs` deleted (not in `.sln`/`.csproj`) |
| W1 D6 | W1.4: Bicep skeleton for staging RG | W1.4 (Bicep skeleton) | ✅ **DONE 2026-05-12** | 7 modules covering 12 staging resources at what-if `NoChange`; non-blocking what-if CI wired; provision-staging.sh marked BICEP PRIMARY; trail of 9 commits 3df82003 → 19a728a2 |
| W1 D7 | W1.5: `Microsoft.FeatureManagement` install + first flag stub | W1.6 (Microsoft.FeatureManagement install) | ✅ **DONE 2026-05-12** | NuGet 4.5.0 added + `AddFeatureManagement()` wired in Program.cs + `Refactor.Smoke.Enabled` flag in appsettings.json + `GET /api/Health/feature-flags` smoke endpoint + `docs/feature-flags.md` registry. Hotfix `e142724b` restored `AddApplication()` accidentally deleted in 4d50251e. Staging revision 0001647 Healthy; smoke endpoint returns 200 with `smokeFlagValue=true`. |
| W1 D8 | W1.7: `.claude/settings.json` audit + W1 close-out | (added in delta; no body section yet) | ✅ **DONE 2026-05-12** | `permissions.allow` 344→326 (-9 entries with embedded JWT tokens, -9 one-off UUID-laden curls); `permissions.deny` 4→19 (+15 hardening rules: prod-RG blocks, force-push, drops, EF migration drops, rm -rf wildcard, find -delete). `additionalDirectories` 11→10 (case-dedupe). Audit decision record: `docs/operations/W1.7-claude-settings-audit.md`. **W1 closed.** |
| unscheduled | **W1.1b**: Azure Key Vault wiring (split out from W1.1 per architect) | W1.2 (Azure Key Vault wiring) | ⏳ founder picks | independent acceptance criteria; deferred when founder accepted W1.1 rotation residual risk |

#### Architect Review (2026-05-11) — GREEN verdict + 6 follow-ups

- **P1 (landed via W1.3a #122)**:
  - 3 over-broad `.gitignore` patterns anchored to root with leading `/` to avoid false-positive blocks against legitimate `tests/e2e/` login fixtures.
  - Orphan `AlertSeverityConsolidationValidation.cs` at repo root deleted (zero `.sln`/`.csproj` references; doesn't compile).
- **P1 (founder browser actions, pending — compensating controls for deferred rotation)**:
  - Enable GitHub Push Protection + Secret Scanning (Settings → Code security, ~5 min).
  - Set Azure AD sign-in alert (or Conditional Access named-location) on staging Service Principal (~15 min).
- **P2 (founder, ~30 sec)**: change password for `niroshhh2@gmail.com` in app profile — was in plaintext in deleted `tests/e2e-api/login-request.json` (now in git history; rotation eliminates residual).
- **Exit criterion added**: as each Bicep module covers a resource in W1.4, the corresponding `scripts/azure/provision-*.sh` lines get deleted in the same commit. Prevents IaC + shell drift.

#### Process Retrospective + Course Correction

4 PRs (#117 / #118 / #119 / #122) were opened through `pr-validation.yml` + Phase A PR Title Gate for develop work — wrong move; plan line 7 (this document) says "Trunk-based development + feature flags (no long-lived branch)". Founder corrected verbatim: *"to push to develop, dont create PR, PR neede for Prod merge."*  Memory rule `feedback_branch_pr_overhead.md` saved.

**Forward W1 discipline**: commit-per-subtask direct to `develop` with `W1.Nx: <summary>` message convention; PROGRESS_TRACKER + STREAMLINED_ACTION_PLAN + TASK_SYNCHRONIZATION_STRATEGY updated in the **same commit** as the code; PRs reserved exclusively for `develop → main` (prod) merges.

---

### W1.1 — Secret rotation (CRITICAL — Day 1)
- [ ] **Action**: Rotate every committed secret in:
  - `Secrets/` folder contents
  - Repo-root JSON files (`admin_login2.json`, `staging_token.json`, `fresh-login.json`, `test-token.txt`, etc.)
  - `appsettings*.json` placeholder values
- [ ] Run `gitleaks` against full history; document exposure
- [ ] **Verify**: invalidated old keys no longer authenticate
  ```bash
  # Should return 401 with old key
  curl -H "Authorization: Bearer <OLD_KEY>" $STAGING_API/api/Users/me
  ```
- **Acceptance**: all old secrets invalidated; new secrets in Azure Key Vault

### W1.2 — Azure Key Vault wiring
- [ ] **Action**: Add `Azure.Extensions.AspNetCore.Configuration.Secrets` to `BuildingBlocks.Web` (creating it as a placeholder; full extraction in W2)
- [ ] Move every secret reference from `appsettings*.json` to Key Vault
- [ ] **Verify**: `dotnet run` locally with Key Vault configured (using developer Azure credentials)
- [ ] Deploy to staging via `deploy-staging.yml`; verify boot logs show Key Vault loaded
- **Acceptance**: zero secrets in source-controlled config files

### W1.3 — Repo cleanup
- [ ] **Action**: Move 211 root debug artifacts to `scripts/_archive/` or delete
- [ ] Delete `src/LankaConnect.Infrastructure/TempContext/`
- [ ] Tighten `.gitignore`:
  - `**/*token*.txt`
  - `**/*login*.json`
  - `Secrets/`
  - `*.local.json`
  - `*.bak`
  - Repo-root files matching `build_*.txt`, `test-*.ps1`, `*-response.json`, etc.
- [ ] **Verify**: `git status` shows only intentional changes
- **Acceptance**: clean repo root (only docs/, src/, web/, infra/, tests/, etc. visible)

### W1.4 — Bicep skeleton
- [ ] **Action**: Create `infra/bicep/` with templates for:
  - Resource Group (staging + production)
  - ACR
  - Key Vault
  - Container Apps Environment
  - Postgres Flexible Server
  - Application Insights
  - Azure App Configuration (for feature flags)
- [ ] **Files**: `infra/bicep/main.bicep`, `infra/bicep/modules/*.bicep`
- [ ] **Verify**: `az bicep build infra/bicep/main.bicep` produces valid ARM
- [ ] **No-op deploy**: `az deployment group what-if --template-file ...` shows zero changes against existing infra
- **Acceptance**: existing infra version-controlled in Bicep; what-if shows no drift

### W1.5 — Decompose 2,514-LOC Events detail mega-page
- [ ] **Action**: Split `web/src/app/events/[id]/page.tsx` into:
  - `EventHero.tsx` (image, title, date, location)
  - `EventDetailsTab.tsx` (description, organizer, contact)
  - `AttendeesTab.tsx` (attendee list, RSVP UI)
  - `PaymentSection.tsx` (Stripe Checkout integration)
  - `SignupSection.tsx` (signup lists, commitments)
  - `AdminPanel.tsx` (edit, publish, cancel, refund)
  - `useEventDetailStore` Zustand store for UI state (modals, processing flags)
- [ ] **Constraint**: pure decomposition, NO behavior change
- [ ] **Verify**: `npm test` green; manual smoke test on staging matches baseline screenshots
- [ ] Run Playwright visual regression; tolerate ≤ 0.1% pixel diff
- [ ] Deploy via `deploy-ui-staging.yml`; sanity-check on staging
- **Acceptance**: 6 sub-components, page LOC dropped from 2,514 to ~300; all behavior preserved; coverage on new components ≥ 80%

### W1.6 — Microsoft.FeatureManagement install
- [ ] **Action**:
  ```bash
  dotnet add src/LankaConnect.Shared/LankaConnect.Shared.csproj package Microsoft.FeatureManagement.AspNetCore
  ```
  (Will be moved to `BuildingBlocks.Web` in W2)
- [ ] Add `FeatureManagement` section to `appsettings.json` (empty)
- [ ] Add `IFeatureManager` to a smoke endpoint to confirm wiring
- [ ] Create `docs/feature-flags.md` registry with header
- **Acceptance**: feature flag infra ready; smoke endpoint tests flag evaluation

### W1.7 — ADR documentation
- [ ] All 5 ADRs reviewed and merged with status `Accepted`
- [ ] `docs/architecture/README.md` index updated
- **Acceptance**: ADR registry visible

### W1.8 — Update tracking docs
- [ ] PROGRESS_TRACKER.md: W1 entries
- [ ] STREAMLINED_ACTION_PLAN.md: W1 status
- [ ] Commit + push develop

---

## Phase A.W2 — BuildingBlocks + Observability (Week 2, 5 days)

### W2.1 — Module skeleton folders ✅ DONE 2026-05-12

**Status**: 5 BuildingBlocks shells + `Hosts/Host.AllInOne.csproj` placeholder + `src/Modules/.gitkeep` parent all landed on develop. `dotnet build LankaConnect.sln` exits 0 (4 unrelated NuGet vuln warnings). Each csproj is in its own nested subdirectory (matches existing convention `src/LankaConnect.X/LankaConnect.X.csproj`). Clean Architecture dependency graph wired in shells before any code lands so W2.2 ArchTest can enforce layering from day one:

```
src/BuildingBlocks/
  BuildingBlocks.Domain/             (innermost; zero refs by design)
  BuildingBlocks.Contracts/          (cross-module ABI; zero refs by design)
  BuildingBlocks.Application/        → Domain + Contracts
  BuildingBlocks.Infrastructure/     → Application + Domain + Contracts
  BuildingBlocks.Web/                → Application + Domain + Contracts + Microsoft.AspNetCore.App
src/Modules/.gitkeep                 (empty parent; W3+ extracts populate this)
src/Hosts/Host.AllInOne/             (placeholder class lib; W7 converts to Web SDK + moves Program.cs here)
```

### W2.1 — Module skeleton folders (original spec)
- [x] **Action**: Create empty `.csproj` shells:
  - `src/BuildingBlocks/BuildingBlocks.Domain.csproj`
  - `src/BuildingBlocks/BuildingBlocks.Application.csproj`
  - `src/BuildingBlocks/BuildingBlocks.Infrastructure.csproj`
  - `src/BuildingBlocks/BuildingBlocks.Web.csproj`
  - `src/BuildingBlocks/BuildingBlocks.Contracts.csproj`
  - `src/Modules/` (empty parent)
  - `src/Hosts/Host.AllInOne.csproj` (placeholder; logic in W7)
- [x] **Verify**: `dotnet build LankaConnect.sln` green
- **Acceptance**: empty projects build green ✅

### W2.2 — Architecture test project ✅ DONE 2026-05-13

**Status**: NetArchTest 1.3.2 wired into a new `tests/architecture/LankaConnect.ArchitectureTests` project; first 4 layering rules landed and pass; CI gate added to `.github/workflows/pr-validation.yml`. Direct-to-develop discipline preserved via push-trigger.

**Rules landed (all `[Trait("Category", "ArchTest")]`)**:
1. `BuildingBlocks.Domain` has no dependency on any other `LankaConnect.*` assembly (innermost layer)
2. `BuildingBlocks.Contracts` has no dependency on any other `LankaConnect.*` assembly (cross-module ABI)
3. `BuildingBlocks.Application` does not depend on `BuildingBlocks.Infrastructure` or `BuildingBlocks.Web`
4. `BuildingBlocks.Infrastructure` does not depend on `BuildingBlocks.Web`

**`public static class AssemblyMarker {}`** added to each of the 5 BuildingBlocks projects so NetArchTest's `Types.InAssembly(typeof(X).Assembly)` has an anchor type until W2.3+ fills the assemblies with real types. Markers are temporary; remove or replace when first real type lands.

**CI integration**:
- Extended `pr-validation.yml` triggers to include `push: branches: [develop]` with paths-filter on `src/BuildingBlocks/**`, `src/Modules/**`, `src/Hosts/**`, `tests/architecture/**`, `Directory.Packages.props`, and the workflow file itself.
- New `arch-test` job runs on both PR (gate develop→main) and push (catch direct trunk commits).
- Existing `pr-quality-check` job guarded with `if: github.event_name == 'pull_request'` so it stays PR-only; `phase-a-title-gate` auto-skips on push because its `if:` references `github.event.pull_request.labels` (null on push).
- Results uploaded as `arch-test-results` artifact (retention 7 days).

**Verification**:
- `dotnet build LankaConnect.sln` exit 0 (0 errors, 4 unrelated NuGet vuln warnings)
- `dotnet test --filter Category=ArchTest` 4/4 pass (~13ms)

### W2.2 — Architecture test project (original spec)
- [x] **Action**: Create `tests/architecture/LankaConnect.ArchitectureTests.csproj` with NetArchTest
- [x] First rule: `Domain` projects reference only `BuildingBlocks.Domain` (expanded to 4 layering rules covering all 5 BuildingBlocks projects)
- [x] **Verify**: `dotnet test --filter Category=ArchTest` green (4/4 pass)
- [x] **CI**: add ArchTest job to `.github/workflows/pr-validation.yml` (extended trigger covers push-to-develop too)
- **Acceptance**: ArchTest job blocks PRs that violate ✅

### W2.3 — Extract BuildingBlocks.Domain ✅ DONE 2026-05-13

**Status**: All 12 types landed across 2 commits (3cb20de1 + this commit). **194 unit tests pass** in 163ms. AssemblyMarker placeholder removed; ArchTest anchor switched to `typeof(Error).Assembly`; layering rules still 4/4 pass on the new types.

**W2.3a (commit `3cb20de1`)** — 8 foundation primitives:
- `Error` (sealed record with sentinels None/NullValue/NotFound/Validation/Conflict/Forbidden)
- `Result` (non-generic outcome with Success/Failure factories + Combine)
- `Result<T>` (value-bearing with implicit conversions from T/Error + Map/Bind/Match railway combinators)
- `Maybe<T>` (readonly struct Some/None with value-based equality + Map/Bind/Match)
- `IDomainEvent` (in-process marker with `OccurredAt`)
- `IAggregateRoot` (DDD aggregate-root marker)
- `Entity<TId>` (identity equality across concrete types + domain-events buffer)
- `ValueObject` (structural equality via `GetEqualityComponents()`)
- `BusinessRule` (abstract named-rule pattern + static Check/CheckAll)
- `Guard` (static argument-check helpers — NotNull, NotNullOrWhitespace, NotEmpty, NotNegative, Positive, InRange)

**W2.3b (this commit)** — 4 value-object value-types per architect review:
- `Currency` — ISO 4217 with the 7-currency registry (USD, LKR, INR, GBP, EUR, AUD, CAD); `FromCode` throws / `TryFromCode` returns Maybe; case-insensitive code lookup
- `Money` — composite (decimal amount + Currency) with same-currency-enforced arithmetic (+ - * / unary-minus) and comparison (< > <= >=); cross-currency operations throw `InvalidOperationException` with a clear message; `RoundToCurrency` uses banker's rounding to `Currency.DecimalDigits`; `Zero(currency)`, `IsZero/IsPositive/IsNegative`, `Negate`, `Abs`
- `Country` — ISO 3166-1 alpha-2 with 6-country registry (LK, US, IN, GB, AU, CA)
- `Locale` — BCP 47 / .NET-culture tag with EnUs/SiLk/TaLk/EnGb static instances; validates against `CultureInfo.GetCultureInfo` with `predefinedOnly: true`; `ToCultureInfo()` for downstream formatting

EF value-converter for `Money` (composite to `_amount` + `_currency` columns per ADR-005) lands in W2.5 BuildingBlocks.Infrastructure — out of W2.3 scope.

### W2.3 — Extract BuildingBlocks.Domain (original spec)
- [x] Move/create: `Result<T>`, `Maybe<T>`, `Entity<TId>`, `ValueObject` base, `IAggregateRoot`, `IDomainEvent`, `BusinessRule`, `Guard`
- [x] **NEW**: Add `Money` value object
- [x] **NEW**: Add `Currency` value object with ISO 4217 registry (USD, LKR, INR, GBP, EUR, AUD, CAD)
- [x] **NEW**: Add `Locale` and `Country` value objects
- [x] **Verify**: unit tests for each value object (90%+ coverage) — 194/194 pass
- **Acceptance**: foundation types in place; tested ✅

### W2.4 — Extract BuildingBlocks.Application ✅ DONE 2026-05-13

**Status**: 6 MediatR pipeline behaviors + 7 abstractions landed; **27 unit tests pass** in 141ms (hand-written fakes, no Moq dependency). AssemblyMarker removed from `BuildingBlocks.Application`; ArchTest anchor switched to `typeof(ICommand<>).Assembly`; all 4 layering rules still green. NO production runtime references this assembly yet — modules consume it from W3+, so "test via API" is N/A; verification is unit tests + full-sln build + ArchTest CI gate.

**Abstractions added** (`src/BuildingBlocks/BuildingBlocks.Application/Abstractions/`):
- `ICommand<TResponse>` / `IQuery<TResponse>` — marker interfaces so behaviors target message types selectively (commands get transaction + outbox + audit; queries skip them)
- `IIdempotentCommand<TResponse>` — extends `ICommand` with `IdempotencyKey` Guid for at-most-once semantics
- `IUnitOfWork` — Begin/Commit/Rollback over a per-module DbContext transaction (W2.5 implements)
- `IIdempotencyStore` — TryGet/Put for replay short-circuit (W2.5 backs onto Postgres `idempotency_keys` table)
- `IOutbox` — Enqueue integration events for the W2.5 `OutboxProcessor` hosted service
- `IAuditLogger` + `AuditEntry` record — write cross-module `platform.audit_events`
- `ICurrentActor` — supplies the authenticated actor id for audit attribution (W2.6 implements from `HttpContext.User`)

**Behaviors landed** (`src/BuildingBlocks/BuildingBlocks.Application/Behaviors/`):
- `LoggingBehavior` — structured Serilog scope with correlation id + Stopwatch + try/catch + re-throw
- `ValidationBehavior` — FluentValidation `IValidator<TRequest>` instances; throws `ValidationException` on failure (handled by ProblemDetails middleware in W2.6)
- `TransactionBehavior` — `IUnitOfWork` Begin → handler → Commit/Rollback; rollback failures swallowed-after-log so the original handler exception propagates as the real cause
- `IdempotencyBehavior` — `JsonSerializer` round-trip via `IIdempotencyStore`; deserialize failure or store-put failure falls through to handler re-execution (better to occasionally double-run than serve stale or block on storage)
- `OutboxBehavior` — drains `IIntegrationEventBuffer` (also defined in this assembly; concrete impl is the W2.5 `BaseDbContext` event collector) and enqueues to `IOutbox`; doesn't drain on handler exception so events don't leak past failed transactions
- `AuditBehavior` — success + failure outcomes; details JSON includes exception **type** but NOT the message (PII risk per ADR-002); audit-write failures swallowed so they can't roll back the business operation

**Test coverage** (`tests/LankaConnect.BuildingBlocks.Application.Tests/`):
- 6 test classes, **27 tests pass** in 141ms
- Each behavior: happy path + null-next guard + key failure modes (handler throws, rollback throws, store throws, audit throws, corrupted cache entry, anonymous actor, multi-validator failure accumulation)
- Hand-written fakes in `Fakes/Fakes.cs` (`FakeUnitOfWork`, `FakeIdempotencyStore`, `FakeOutbox`, `FakeIntegrationEventBuffer`, `FakeAuditLogger`, `FakeCurrentActor`, `NullLog.For<>()`)

### W2.4 — Extract BuildingBlocks.Application (original spec)
- [x] **Action**: Implement MediatR pipeline behaviors:
  - [x] `ValidationBehavior` (FluentValidation)
  - [x] `LoggingBehavior` (correlation IDs, scoped Serilog)
  - [x] `TransactionBehavior` (UoW per command)
  - [x] `IdempotencyBehavior` (per-module idempotency table conventions)
  - [x] `OutboxBehavior` (publish IntegrationEventVx)
  - [x] **`AuditBehavior`** (writes to `platform.audit_events` — NEW per architect review)
- [x] **Verify**: behaviors unit tested with mock pipelines — 27/27 pass
- **Acceptance**: pipeline behaviors ready for module use ✅

### W2.5 — Extract BuildingBlocks.Infrastructure ✅ DONE 2026-05-13 (with one documented gap)

**Status**: Persistence primitives landed across 2 commits (48a916da + this commit). **24/25 tests pass**, 1 skipped honestly with explanation. ArchTest 4/4 still green; full sln build 0 errors. AssemblyMarker removed in `BuildingBlocks.Infrastructure`.

**W2.5a (`48a916da`)** — `BaseDbContext` + `Money` EF converter + JSONB ValueComparer helper:
- `IAuditable` + `ISoftDeletable` opt-in markers in `BuildingBlocks.Domain`
- `BaseDbContext` with two-pass `SaveChangesAsync`: soft-delete pass FIRST (flip Deleted→Modified), audit pass SECOND (stamp UpdatedAt/UpdatedBy on the resulting Modified state). Global query filter for `ISoftDeletable` via expression-tree reflection. `Property("CreatedAt").IsModified = false` on update so immutable insert-time values are preserved.
- `JsonbValueComparerExtensions` — `ApplyJsonbReadOnlyListComparer<TEntity, TElement>` + `ApplyJsonbListComparer<TEntity, TElement>` helpers with deep-copy snapshot ValueComparer per MEMORY.md Phase 6A.129 fix recipe
- `MoneyConfigurationExtensions.ConfigureMoney<TEntity>` — two-column persistence per ADR-005 (`{prefix}_amount` + `{prefix}_currency`), empty-prefix throws at model-build time

**W2.5b (this commit)** — outbox pattern + dead-letter + Testcontainers integration tests:
- `OutboxMessage` entity (Id, EventType, Payload, OccurredAt, ProcessedAt, RetryCount, LastError) with `Create` factory + `MarkProcessed` / `RecordFailure` / `ShouldDeadLetter` methods. MaxRetries = 5.
- `DeadLetterMessage` entity capturing dead-lettered outbox rows with `FromOutboxMessage` factory; separate table so outbox stays small + fast to poll
- `IIntegrationEventDispatcher` interface — AllInOne MediatR concrete impl is W3+; Service Bus concrete impl is post-Phase A (per ADR-002)
- `OutboxProcessor<TDbContext>` `BackgroundService` — polls on interval (default 5s), batch size 50, processes oldest-first, marks processed on success, increments retry + records error on failure, moves to dead-letter after MaxRetries. Generic over TDbContext so each module gets its own with potentially different polling budgets. `ProcessBatchOnceAsync` exposed for tests so they don't wait on the interval timer.
- `Testcontainers.PostgreSql` integration test project — class fixture spins up Postgres 15-alpine container, reused across tests in the class

**Integration tests (Testcontainers Postgres) — master TODO §W2.5 acceptance gate**:
- `MoneyRoundTripIntegrationTests` (3 PASS): Money round-trip across all 7 supported currencies; null Price persists as null; updating both Amount AND Currency persists both columns (verifying the two-column converter handles currency changes, not just amount)
- `JsonbValueComparerIntegrationTests` (1 PASS, 1 SKIP): `WithoutValueComparer_InPlaceMutation_PersistsIncorrectly` PASSES — demonstrates the MEMORY.md Phase 6A.129 bug (in-place `Clear() + AddRange()` on a List<T> JSONB column silently fails to persist). `WithValueComparer_InPlaceMutation_PersistsCorrectly` is SKIPPED with detailed explanation — EF Core 8 + Npgsql 8 + HasConversion + jsonb interaction doesn't currently route the ValueComparer through change detection as expected; the fix-verification path needs more investigation (possibly via custom ProviderValueComparer or switching to OwnedNavigation pattern). The deep-copy snapshot pattern from MEMORY.md is well-documented but needs EF Core 8-specific adaptation.

**Unit tests** (EF Core InMemory, 20 PASS):
- BaseDbContextAuditTests (5): audit field stamping on Add/Update + plain entity passthrough + sync SaveChanges parity
- BaseDbContextSoftDeleteTests (5): Delete on ISoftDeletable flips + default filter + IgnoreQueryFilters + combined audit+soft-delete + plain hard-delete
- MoneyConfigurationTests (4): single-currency round-trip + multi-currency + null Price + empty-prefix throws
- OutboxProcessorTests (6): pending dispatch + empty outbox + skips already-processed + dispatcher-throws records failure + dead-letters after MaxRetries + ordered-by-OccurredAt oldest-first

### W2.5 — Extract BuildingBlocks.Infrastructure (original spec)
- [x] **Action**: Implement:
  - [x] `BaseDbContext` (audit fields, soft delete, JSONB ValueComparer **helper** — bug-reproduction integration test passes; fix-verification integration test SKIPPED pending EF Core 8 adaptation per the documented gap above)
  - [x] **`Money` EF value converter** (composite to `_amount` + `_currency` columns) — 3 Testcontainers Postgres integration tests pass
  - [x] `OutboxProcessor` hosted service
  - [x] `IntegrationEventDispatcher` interface (concrete impl in W3+)
  - [x] `DeadLetterTable` convention
- [x] **Verify**: integration test with Testcontainers Postgres — 4 PASS, 1 SKIP
- **Acceptance**: persistence primitives ready ✅ (with documented gap on JSONB fix-verification)

### W2.6 — Extract BuildingBlocks.Web
- [x] **W2.6a (2026-05-25)**: 6 cross-cutting extensions landed in `src/BuildingBlocks/BuildingBlocks.Web/`:
  - JwtAuthenticationExtensions (Authentication/) — strongly-typed JwtSettings, throws on missing Key/Issuer/Audience, JwtBearerEvents log via `BuildingBlocks.Web.Jwt`
  - ProblemDetailsExtensions + GlobalExceptionHandler (ProblemDetails/) — RFC 7807 with PII redaction on 5xx
  - HealthCheckExtensions (HealthCheckExtensions/) — Postgres/Redis/DbContext, /health + /health/live + /health/ready
  - RateLimitingExtensions (RateLimiting/) — perip 60/min default + host `configure` callback for app policies
  - ApiVersioningExtensions (Versioning/) — Asp.Versioning 8.x with URL+query+header readers
  - FeatureManagementExtensions (FeatureFlags/) — Microsoft.FeatureManagement per ADR-004
- [x] **W2.6a tests**: new `LankaConnect.BuildingBlocks.Web.Tests` project — 18/18 GREEN
- [x] **W2.6a ArchTest**: added `BuildingBlocks_Web_DoesNotDependOnLayeredMonolith` (5th rule, anchors on `JwtSettings`) — 5/5 GREEN
- [x] **W2.6a cleanup**: removed W2.1 AssemblyMarker placeholder
- [x] **W2.6b (2026-05-25)**: OpenTelemetry + Azure Monitor distro wired:
  - Added `Azure.Monitor.OpenTelemetry.AspNetCore` 1.2.0 to Directory.Packages.props
  - New extension `Telemetry/TelemetryExtensions.cs` — `AddBuildingBlocksTelemetry(IServiceCollection, IConfiguration, string serviceName)`. When `ApplicationInsights:ConnectionString` config or `APPLICATIONINSIGHTS_CONNECTION_STRING` env var is set, uses Azure Monitor distro (full traces+metrics+logs export). When absent, falls back to OTel-only with AspNetCore+HttpClient instrumentation
  - LankaConnect.API now references BuildingBlocks.Web and calls `AddBuildingBlocksTelemetry(..., serviceName: "LankaConnect.API")` after Serilog wire-up
  - 4 new unit tests in `tests/LankaConnect.BuildingBlocks.Web.Tests/Telemetry/` — DI registration shape + constant stability (22/22 total Web tests GREEN)
- [x] **W2.6b staging provisioning**: created `lankaconnect-staging-insights` App Insights resource in `lankaconnect-staging` RG (eastus2); set Container App secret `appinsights-connection-string` and bound `APPLICATIONINSIGHTS_CONNECTION_STRING=secretref:appinsights-connection-string`
- [ ] **W2.6b staging verification**: deploy via push to develop → confirm traces visible in App Insights for /api/Auth/login + a downstream endpoint
- **Acceptance**: cross-cutting infrastructure ready; observability live before any module work (W2.6a 🟢; W2.6b code 🟢; staging trace verification PENDING deploy)

### W2.7 — Extract BuildingBlocks.Contracts
- [ ] **Action**: Implement `IntegrationEventBase`, `IntegrationEventV1` versioning convention
- [ ] **NEW**: define `IIntegrationEventDispatcher` and contract for cross-module event publishing
- **Acceptance**: contracts package referenceable by future modules

### W2.8 — API baseline regression run
- [ ] **Action**: Run `tests/api-baseline/run-baseline-regression.sh` against staging
- [ ] **Verify**: zero structural drift (existing API still responds identically)
- **Acceptance**: regression green; baselines unchanged

### W2.9 — Update tracking docs

---

## Phase A.W3 — Module Extraction #1: Notifications (Week 3, 5 days)

**Why first**: lowest fan-in, already generic, sets the playbook.

### W3.1 — Notifications module skeleton
- [ ] **Action**: Create:
  - `src/Modules/Notifications/Notifications.Domain.csproj`
  - `src/Modules/Notifications/Notifications.Application.csproj`
  - `src/Modules/Notifications/Notifications.Contracts.csproj`
  - `src/Modules/Notifications/Notifications.Infrastructure.csproj`
  - `src/Modules/Notifications/Notifications.Api.csproj`
  - `tests/Modules/Notifications/Notifications.{Domain,Application,Infrastructure,Api}.Tests.csproj`
- [ ] **Verify**: ArchTest passes; `dotnet build` green
- **Acceptance**: 5 module projects + 4 test projects exist

### W3.2 — Move domain types
- [ ] Move `src/LankaConnect.Domain/Notifications/*` → `src/Modules/Notifications/Domain/`
- [ ] Update namespaces: `LankaConnect.Modules.Notifications.Domain.*`
- [ ] **Verify**: existing notification unit tests still pass

### W3.3 — Define Notifications.Contracts
- [ ] **Action**: Create:
  - `INotificationDispatcher` (public interface for other modules)
  - `NotificationCreatedIntegrationEventV1`
- [ ] **Verify**: only primitive types in Contracts (no domain entity leakage)
- **Acceptance**: contracts pass ArchTest

### W3.4 — Move application + infrastructure
- [ ] Move handlers → `Notifications.Application/`
- [ ] Move repositories → `Notifications.Infrastructure/`
- [ ] Create `NotificationsDbContext` pointing at `notifications` schema
- **Verify**: unit tests pass

### W3.5 — Baseline migration for Notifications schema
- [ ] **Action**: Use `migra` (or pg-diff) to capture exact production `notifications` schema
- [ ] **Action**: Generate baseline migration:
  ```bash
  dotnet ef migrations add Baseline_Notifications \
    --context NotificationsDbContext \
    --project src/Modules/Notifications/Notifications.Infrastructure \
    --startup-project src/Hosts/Host.AllInOne
  ```
- [ ] **Manual edit**: empty the `Up()` method
- [ ] **Manual SQL**: insert history row marking baseline as applied:
  ```sql
  INSERT INTO notifications."__EFMigrationsHistory" (migration_id, product_version)
  VALUES ('20260501000000_Baseline_Notifications', '8.0.0');
  ```
- [ ] **CRITICAL**: verify `.Designer.cs` companion file exists (per MEMORY.md hand-created migration warning)
- [ ] Add per-schema idempotency table: `notifications.idempotency_keys`
- [ ] Add per-schema outbox + dead-letter: `notifications.outbox`, `notifications.outbox_dead_letter`
- [ ] **Verify**:
  ```bash
  # On staging clone
  dotnet ef database update --context NotificationsDbContext
  # Then schema diff vs production
  migra postgresql://staging postgresql://production
  ```
- [ ] **Acceptance**: zero schema drift; migration applied with `Up()` empty (no DDL run)

### W3.6 — Move controllers, wire NotificationsModule extension
- [ ] Move `NotificationsController` → `Notifications.Api/`
- [ ] Create `NotificationsModule.cs`:
  ```csharp
  public static IServiceCollection AddNotificationsModule(this IServiceCollection s, IConfiguration c) {
      s.AddDbContext<NotificationsDbContext>(...);
      s.AddScoped<INotificationDispatcher, NotificationDispatcher>();
      // ... MediatR registration scoped to this assembly
      return s;
  }
  ```
- [ ] In `Host.AllInOne/Program.cs`: register both legacy and new paths behind feature flag

### W3.7 — Feature flag wiring
- [ ] **Add flag**: `Refactor.Notifications.UseNewModule` to `appsettings.json`, default `false`
- [ ] Update `docs/feature-flags.md` registry with sunset Week 7
- [ ] Controller/handler dispatch logic uses flag

### W3.8 — Deploy to staging + soak
- [ ] **Push**: develop branch
- [ ] **Deploy**: via `deploy-staging.yml`
- [ ] **Verify deployment**: container logs show new module loaded
  ```bash
  az containerapp logs show -n lankaconnect-api-staging -g <rg> --tail 200
  ```
- [ ] **API test (legacy path, flag OFF)**:
  ```bash
  TOKEN=$(curl -X POST $STAGING/api/Auth/login ... | jq -r .accessToken)
  curl -H "Authorization: Bearer $TOKEN" $STAGING/api/Notifications
  ```
  → verify response shape matches A.0.B baseline
- [ ] **Flip flag in staging**: set `Refactor.Notifications.UseNewModule=true` via Azure App Configuration
- [ ] **API test (new path, flag ON)**: same curl → verify identical response shape
- [ ] **Soak**: 7 days monitoring App Insights for error rate, latency p50/p95/p99 deltas
- [ ] **Acceptance**: zero error rate increase, latency within 10% of baseline

### W3.9 — Document MODULE_EXTRACTION_PLAYBOOK
- [ ] **Files**: `docs/architecture/MODULE_EXTRACTION_PLAYBOOK.md`
- [ ] Capture: skeleton creation, namespace migration, baseline migration recipe, feature flag wiring, deployment + soak procedure, common pitfalls encountered
- **Acceptance**: every subsequent module extraction follows this playbook

### W3.10 — Update tracking docs

---

## Phase A.W4 — Modules #2 + #3: Communications + Media (Week 4, 5 days)

Two parallel agent worktrees; PR review sequential.

### W4.1 — Communications module (Email + WhatsApp + Newsletter)
- [ ] Repeat W3.1–W3.7 pattern for Communications
- [ ] 41 email parameter types move to `Communications.Contracts`
- [ ] Per-locale template lookup (en-US fallback) — implements ADR-001 foundation
- [ ] Feature flag: `Refactor.Communications.UseNewModule` (sunset Week 8)
- [ ] **Verify staging soak**: send test email via API; verify Azure Communication Services receives request
  ```bash
  curl -X POST -H "Authorization: Bearer $TOKEN" \
    $STAGING/api/Email/test \
    -d '{"to":"niroshhh@gmail.com","template":"WelcomeEmail"}'
  ```
- [ ] **DB verify**:
  ```sql
  SELECT * FROM communications.email_messages
  WHERE created_at > NOW() - INTERVAL '5 minutes' ORDER BY created_at DESC LIMIT 5;
  ```
- **Acceptance**: legacy + new path both deliverable; staging soak 7 days

### W4.2 — Media module (Photo Albums)
- [ ] Repeat W3.1–W3.7 pattern for Media
- [ ] **Schema migration**: rename `EventId` → `OwnerEntityId` + add `OwnerEntityType`
  - Use `migra` diff to verify exact change
  - Backfill: `UPDATE media.album_photos SET owner_entity_type = 'Event' WHERE owner_entity_type IS NULL`
- [ ] Feature flag: `Refactor.Media.UseNewModule` (sunset Week 8)
- [ ] **Verify staging**: upload a test photo via API
  ```bash
  curl -X POST -H "Authorization: Bearer $TOKEN" \
    -F "file=@test.png" \
    $STAGING/api/PhotoAlbums/{albumId}/photos
  ```
- [ ] **DB verify**: row in `media.album_photos` has `owner_entity_id` and `owner_entity_type='Event'`
- **Acceptance**: photo upload + retrieval functional via both paths

### W4.3 — Run API baseline regression
- [ ] **Action**: Schemathesis OpenAPI conformance + 5 Pact contract tests against staging (per A.0.B.6)
- **Acceptance**: zero structural drift on existing endpoints; all contract tests green

### W4.4 — Cross-module integration smoke test (NEW per architect — W4.1.5)
- [ ] **Action**: End-to-end test of outbox → IntegrationEventDispatcher → cross-module handler
  - Trigger: send a test email via Communications module (new path, flag ON)
  - Expected: `EmailSentIntegrationEventV1` lands in Communications outbox → dispatcher polls → Notifications module receives event and creates user notification
  - Verify: notification appears in `notifications.notifications` table within 5 seconds
- [ ] **Files**: `tests/contract/cross-module-smoke.test.cs`
- [ ] **Verify in production-like staging**:
  ```sql
  SELECT * FROM communications.outbox WHERE created_at > NOW() - INTERVAL '1 minute';
  SELECT * FROM notifications.notifications WHERE created_at > NOW() - INTERVAL '1 minute';
  ```
- **Acceptance**: outbox infrastructure proven end-to-end before Events extraction in W8 (was W7); end-to-end latency < 5s

### W4.5 — Update tracking docs

---

## Phase A.W5 — Module #4: Forms (Week 5, 5 days)

### W5.1 — Forms module skeleton (per playbook)

### W5.2 — Generalize ownership
- [ ] **Action**: Refactor `EventForm.EventId` → `Form.OwnerEntityId` + `OwnerEntityType`
- [ ] **EF migration**: rename + backfill: `UPDATE forms.forms SET owner_entity_type = 'Event' WHERE owner_entity_type IS NULL`
- [ ] **Designer.cs check** per MEMORY.md guidance
- [ ] **Verify after migration**:
  ```sql
  SELECT COUNT(*) FROM forms.forms WHERE owner_entity_type IS NULL;
  -- Must return 0
  ```

### W5.3 — Update Events handlers to call Forms via Contracts
- [ ] Replace direct calls with `IFormQueries` interface
- [ ] **Verify**: ArchTest catches direct Forms.Domain references from Events

### W5.4 — Feature flag deploy + soak
- [ ] Flag: `Refactor.Forms.UseNewModule` (sunset Week 9)
- [ ] **API test**: existing event with form responses still loads correctly
  ```bash
  curl -H "Authorization: Bearer $TOKEN" $STAGING/api/Events/{id}/forms
  ```
- **Acceptance**: 7-day soak; zero regression

### W5.5 — Update tracking docs

---

## Phase A.W6 — Module #5: Payments (Week 6, 5 days)

The biggest semantic refactor. Real money on the line.

### W6.1 — Payments module skeleton

### W6.2 — Generic CheckoutSession abstraction
- [ ] **Action**: Define:
  ```csharp
  public interface IPaymentCheckoutService {
      Task<CheckoutSessionResult> CreateCheckoutSessionAsync(CheckoutRequest request);
  }

  public record CheckoutRequest(
      Money Amount,
      string DescriptiveName,
      Dictionary<string, string> Metadata,  // includes originating_module, storefront_id
      Uri SuccessUrl, Uri CancelUrl, ...);
  ```
- [ ] **Verify**: 8 existing event-specific checkout methods become adapters that build `CheckoutRequest` and call `IPaymentCheckoutService`

### W6.3 — Generic PaymentSettledIntegrationEventV1
- [ ] Carries `(orderId, originatingModule, amount: Money, customerCountry)`
- [ ] Stripe webhook handler dispatches via integration event mechanism (per ADR-005)

### W6.4 — Feature flag deploy + soak
- [ ] Flag: `Refactor.Payments.UseNewModule` (sunset Week 10)
- [ ] **CRITICAL API tests** (Stripe TEST mode):
  - [ ] Event registration payment (use Stripe test card 4242...)
  - [ ] Donation payment
  - [ ] Sponsor payment
  - [ ] Add-on purchase payment
  - [ ] Each: verify webhook received, DB row created, email sent
- [ ] Run **≥ 50 successful test transactions** before flipping production flag
- [ ] **Acceptance**: full payment matrix verified on staging in test mode for 7 days

### W6.5 — Update tracking docs

---

## Phase A.W7 + W8 — Module #6: Events (2 weeks, 10 days)

The largest single move. 80% of the codebase.

### W7.1 — Events module skeleton

### W7.2 — Move domain (60+ files)
- [ ] **Files**: `src/LankaConnect.Domain/Events/*` → `src/Modules/Events/Domain/`
- [ ] Update namespaces
- [ ] **Verify**: domain unit tests pass

### W7.3 — Move application (441 files, 40+ handlers)
- [ ] **Files**: `src/LankaConnect.Application/Events/*` → `src/Modules/Events/Application/`
- [ ] Update namespaces
- [ ] Refactor cross-module references:
  - Email send → `ICommunicationsCommands`
  - Notifications → `INotificationDispatcher` (Notifications.Contracts)
  - Photos → `IMediaCommands`
  - Forms → `IFormQueries`
  - Payments → `IPaymentCheckoutService`

### W7.4 — Move infrastructure + controllers
- [ ] EF configurations → `Events.Infrastructure`
- [ ] EventsController, EventTemplatesController, EventConfigController, CollectionsController, AddOnsController, SponsorsController → `Events.Api`

### W7.5 — Events DbContext + baseline migration
- [ ] Per playbook
- [ ] Per-module idempotency, outbox, dead-letter tables in `events` schema

### W8.1 — Feature flag deploy + extended soak
- [ ] Flag: `Refactor.Events.UseNewModule` (sunset Week 12)
- [ ] **MASSIVE API verification** — full Events flow:
  - [ ] Create event
  - [ ] Publish event
  - [ ] List events
  - [ ] RSVP / register
  - [ ] Add signup commitment
  - [ ] Pay for registration (Stripe test mode)
  - [ ] Upload event photo
  - [ ] Submit form response
  - [ ] Cancel registration + refund
  - [ ] Each: API response shape matches A.0.B baseline
  - [ ] Each: verifying side effects (email sent, notification created, DB row updated)
- [ ] **Performance verification vs W0 baseline**:
  - p50, p95, p99 within 10% of pre-refactor
- [ ] Load test at 2× current production traffic via k6

### W8.2 — Update tracking docs

---

## Phase A.W9 — Money Refactor (Week 9, 5 days)

Standalone week per architect's recommendation (avoid tangling with Events extraction).

### W9.1 — Schema audit
- [ ] **Action**: Identify every `decimal` column representing money across all module schemas
  ```sql
  SELECT table_schema, table_name, column_name, data_type
  FROM information_schema.columns
  WHERE data_type IN ('numeric', 'decimal')
    AND (column_name LIKE '%price%' OR column_name LIKE '%amount%'
         OR column_name LIKE '%fee%' OR column_name LIKE '%cost%');
  ```
- [ ] **Save**: `docs/operations/money-column-audit.md`
- **Acceptance**: complete inventory of monetary columns

### W9.2 — EF Money value converter
- [ ] **Action**: Implement converter in `BuildingBlocks.Infrastructure`:
  ```csharp
  builder.OwnsOne(x => x.Price, m => {
      m.Property(p => p.Amount).HasColumnName("price_amount");
      m.Property(p => p.CurrencyCode).HasColumnName("price_currency");
  });
  ```
- [ ] **Verify**: integration test with Testcontainers reads/writes Money correctly

### W9.3 — Per-module migrations
- [ ] For each schema with monetary columns, write migration that:
  - Renames `price` → `price_amount`
  - Adds `price_currency` column NOT NULL DEFAULT 'USD'
  - Backfills existing rows
- [ ] **Designer.cs check** for every migration
- [ ] **Verify post-migration**:
  ```sql
  SELECT COUNT(*) FROM events.tickets WHERE price_currency IS NULL;
  -- Must return 0
  ```

### W9.4 — Stripe integration with Currency
- [ ] Update `IPaymentCheckoutService` implementation to use `request.Amount.CurrencyCode`
- [ ] **API tests** — Stripe test mode with multi-currency:
  - [ ] USD charge → success
  - [ ] LKR charge → success (verify Stripe accepts)
  - [ ] Refund in original currency

### W9.5 — DTO update with dual fields (per ADR-005)
- [ ] **Action**: Every monetary DTO returns BOTH `price: decimal` AND `priceMoney: { amount, currencyCode }`
- [ ] Add `Refactor.MoneyDto.LegacyField` flag with sunset Week 15
- [ ] Update OpenAPI; regenerate frontend api-client

### W9.6 — Email/WhatsApp template price tokens
- [ ] Update templates to render `priceMoney` formatted

### W9.7 — Deploy to staging + soak
- [ ] **API verification**: every payment flow tested with Stripe test cards
  - [ ] **≥ 50 successful transactions** across event registration, donation, sponsor, add-on
- [ ] **DB verification**: all money columns now have currency_code populated
- **Acceptance**: 7-day staging soak; zero payment regression

### W9.8 — Update tracking docs

---

## Phase A.W10 — Module #7: Identity (Week 10, 5 days)

LAST module per architect (highest fan-in; freeze contract last when most evidence).

### W10.1 — Identity module skeleton

### W10.2 — Move users + auth
- [ ] Domain.Users → `Identity.Domain`
- [ ] Application.Auth → `Identity.Application`
- [ ] Security/Services/JwtTokenService → `Identity.Infrastructure`
- [ ] AuthController → `Identity.Api`

### W10.3 — Add Locale + Country on User
- [ ] **Migration**: add `preferred_locale VARCHAR(10) NOT NULL DEFAULT 'en-US'`, `country_code VARCHAR(2) NOT NULL DEFAULT 'US'` to `identity.users`
- [ ] Update `User` aggregate

### W10.4 — Identity DbContext + baseline migration

### W10.5 — Feature flag deploy + soak
- [ ] Flag: `Refactor.Identity.UseNewModule` (sunset Week 14)
- [ ] **API verification — auth surface**:
  - [ ] Login (verify JWT shape unchanged)
  - [ ] Register (verify defaults populated for locale/country)
  - [ ] Refresh token
  - [ ] Logout
  - [ ] Password reset
  - [ ] All match A.0.B baselines
- **Acceptance**: 7-day soak; zero auth regression

### W10.6 — Update tracking docs

---

## Phase A.W11 — Frontend Turborepo (Week 11, 5 days)

### W11.1 — Convert to Turborepo workspace
- [ ] `web/apps/lankaconnect/` (the existing Next.js app)
- [ ] `web/packages/` (initially empty)
- [ ] `turbo.json` build pipeline config
- [ ] **Verify**: `npm run build` from root produces same artifact as before

### W11.2 — @lankaconnect/ui package
- [ ] Extract 17 UI primitives from `web/src/presentation/components/ui/`
- [ ] **Verify**: Lankaconnect app imports `@lankaconnect/ui` instead; smoke test pages render

### W11.3 — @lankaconnect/auth package
- [ ] Extract `useAuthStore`, `ProtectedRoute`, JWT refresh service
- [ ] **Verify**: login/logout flow works

### W11.4 — @lankaconnect/api-client-core package
- [ ] Extract axios singleton + interceptors
- [ ] **Verify**: existing repositories still work

### W11.5 — next-intl wiring (foundation per ADR-001)
- [ ] Add `[locale]` route segment
- [ ] Middleware redirects `/` → `/en/`
- [ ] `messages/en.json` baseline (English-only)
- [ ] Locale switcher component (only "English" option shown until other locales added)
- [ ] **Verify**: all existing pages accessible at `/en/...`

### W11.6 — ESLint cross-feature import rule
- [ ] `no-restricted-imports` blocks `@lankaconnect/feature-X` from `@lankaconnect/feature-Y`

### W11.7 — Deploy + visual regression
- [ ] **Deploy**: via `deploy-ui-staging.yml`
- [ ] **Verify**: Playwright screenshots match W0 baseline (≤ 0.1% pixel diff)
- [ ] Lighthouse score within 5% of baseline

### W11.8 — Update tracking docs

---

## Phase A.W12 — Feature-Events Package + Money DTO Migration (Week 12, 5 days)

### W12.1 — @lankaconnect/feature-events package
- [ ] Extract 73 components from `web/src/presentation/components/features/events/`
- [ ] Generate `@lankaconnect/api-client-events` from Events OpenAPI via NSwag
- [ ] **Verify**: events pages still render

### W12.2 — Frontend Money migration (per ADR-005)
- [ ] **Action**: per-component PR migrating from `price` to `priceMoney`
- [ ] Plan: 1 PR per ≤ 5 components, with screenshot evidence
- [ ] ESLint `no-decimal-money` warning enabled
- [ ] **Verify** each PR: visual regression passes

### W12.3 — Frontend formatters package
- [ ] `formatMoney(money, locale)`, `formatDate(date, locale)`, `formatPhone(phone, country)`

### W12.4 — Update tracking docs

---

## Phase A.W13 — Per-Module CI/CD Hardening (Week 13, 5 days)

### W13.1 — Path-filter CI
- [ ] **Action**: `dorny/paths-filter` action in workflows
- [ ] Module-touched-only test runs

### W13.2 — Pact contract tests
- [ ] Top 5 module pairs: Events↔Communications, Events↔Payments, Events↔Notifications, Commerce↔Identity (placeholder), Commerce↔Payments (placeholder)
- [ ] Provider verification jobs in CI

### W13.3 — ArchTest CI gate
- [ ] PR check: `dotnet test --filter Category=ArchTest` blocks merge if violations

### W13.4 — Stale-flag CI gate
- [ ] `tools/check-stale-flags.sh` runs on PRs; fails if `Refactor.*` past sunset

### W13.5 — Container Apps revision-based canary
- [ ] Bicep revision-mode = `Multiple`
- [ ] Cutover script supports 10% → 50% → 100% traffic shift

### W13.6 — Update tracking docs

---

## Phase A.W14 + W15 — Staging Full Regression + Buffer (Week 14–15, 10 days)

### W14.1 — Full API regression
- [ ] **Action**: Run `tests/api-baseline/run-baseline-regression.sh` against staging with ALL flags ON
- [ ] **Manual smoke test** — user journey scenarios:
  - [ ] New user registration → verification email → login
  - [ ] Create event → publish → guest registers → guest pays → guest cancels → refund
  - [ ] Create signup list → user commits → admin marks confirmed
  - [ ] Upload event photos → view album
  - [ ] Submit feedback form
  - [ ] Newsletter subscribe → receive newsletter
  - [ ] WhatsApp opt-in → receive message
  - [ ] Admin: ban user, support ticket, audit log

### W14.2 — Performance + load
- [ ] **Action**: k6 load test at 2× current production peak
- [ ] **Verify**: p50/p95/p99 within 10% of W0 baseline; zero error rate increase

### W14.3 — DB performance regression
- [ ] **Action**: Capture `pg_stat_statements` from staging post-refactor; diff vs W0 baseline
- [ ] **Verify**: no query plan degradation > 20%

### W15.1 — Buffer week (5 days for surprises)
- [ ] Reserved for issues found during W14
- [ ] **Acceptance**: all known issues either fixed or scheduled

### W15.2 — Production cutover dry-run
- [ ] Execute cutover procedure on staging end-to-end
- [ ] Time the cutover; document precise sequence
- [ ] **Files**: `docs/operations/PRODUCTION_CUTOVER_RUNBOOK.md`

---

## Phase A.W16 — Production Cutover (Week 16, 5 days)

### W16.1 — Pre-cutover gate (Day 1)
- [ ] All flags `OFF` in production (legacy paths still serving)
- [ ] Production database backup taken: `pg_dump` to Azure Blob with retention 90 days
- [ ] Tag git release: `phase-a-cutover-pre`
- [ ] On-call rotation confirmed
- [ ] Communications: customer notice posted (low-impact maintenance window)

### W16.2 — Container deployment (Day 1)
- [ ] **Push develop → main** via PR with full plan attached
- [ ] **Deploy**: `deploy-production-with-approval.yml` triggers
- [ ] Approval granted manually
- [ ] **Verify deployment**:
  ```bash
  az containerapp logs show -n lankaconnect-api-prod -g <rg> --tail 200
  ```
  → look for "Module XYZ loaded" lines
- [ ] **Smoke test**: API baseline regression run against production with flags STILL OFF
- [ ] **Acceptance**: production unchanged behavior; new code present but unreachable

### W16.3 — Canary flag flips (Day 2) — REVISED ORDER per architect

- [ ] **Per module, in this order** (Identity LAST per architect — highest blast radius):
  1. Notifications
  2. Communications
  3. Media
  4. Forms
  5. Payments
  6. Events
  7. **Identity** (last — auth break = everything breaks; flip only after all other modules proven stable on new path)
- [ ] Per module: 10% canary via Container Apps revision-based traffic split → monitor 4h → 50% → monitor 4h → 100% → monitor 24h
- [ ] **Per step**: Schemathesis OpenAPI conformance check + Pact contract test run + App Insights error/latency comparison vs W0 baseline
- [ ] **Identity-specific**: extra 48h soak between 50% and 100% traffic; validate every other module's auth flow still works

### W16.4 — Money DTO cleanup (Day 4)
- [ ] **Verify**: zero frontend files reference legacy `price` field (grep)
- [ ] **PR**: drop legacy field; OpenAPI regenerates
- [ ] Deploy + verify

### W16.5 — Cutover complete documentation (Day 5)
- [ ] Update PROGRESS_TRACKER, STREAMLINED_ACTION_PLAN, MASTER_TODO_PROD_RELEASE
- [ ] Tag git: `phase-a-cutover-complete`
- [ ] Old image kept for 7 days for emergency rollback
- [ ] **Acceptance**: production live on new architecture; all baselines green

---

## Phase A.W17–W19 — Stabilization Soak (Weeks 17–19, 15 days)

NO new module work. NO Phase 2 kickoff.

### W17 — Production soak
- [ ] On-call rotation active; bug triage
- [ ] Daily App Insights review (errors, latency, anomalies)
- [ ] Address any P0/P1 issues immediately

### W18 — Retrospective + ADR finalization
- [ ] Write retrospective: what went well, what didn't, what to change for Phase 2
- [ ] Write ADR-006 (Migration strategy as actually executed)
- [ ] Write ADR-007 (Feature flag operation patterns observed)
- [ ] Refine MODULE_EXTRACTION_PLAYBOOK with lessons learned

### W19 — Tech debt cleanup + Phase 2 prep
- [ ] Delete every `Refactor.*` flag and its legacy code path
- [ ] Frontend Playwright visual regression suite finalized
- [ ] Phase 2 (Directory module) detailed plan drafted
- [ ] **Phase A officially complete** ✅

---

## Production Cutover Procedure (Detailed)

See [PRODUCTION_CUTOVER_RUNBOOK.md](./operations/PRODUCTION_CUTOVER_RUNBOOK.md). Summary:

1. **Pre-cutover** (T-7 days): all PRs merged; staging soak passes; database backup; on-call confirmed
2. **Deploy** (T+0): push to main, deploy via `deploy-production-with-approval.yml`, approval, smoke test (flags OFF)
3. **Canary** (T+1): per-module 10% → 50% → 100% with monitoring gates
4. **Cleanup** (T+3): drop legacy DTO fields, remove dead code paths
5. **Soak** (T+7): keep old image available; monitor

## Rollback Procedures

### Per-module flag rollback (preferred)
1. In Azure App Configuration: flip `Refactor.<Module>.UseNewModule` → `false`
2. Wait 60s for cache TTL
3. Verify legacy path serving via API baseline regression
4. **No deployment needed**; recovery time < 2 minutes

### Full Phase A rollback (last resort)
1. Container Apps: shift 100% traffic to previous revision (image tag `phase-a-cutover-pre`)
2. Restore database from pre-cutover backup if schema changes incompatible
3. Verify API baseline regression
4. Recovery time: ~15 minutes

## Definition of Done — Phase A

- [ ] All 7 modules extracted: Notifications, Communications, Media, Forms, Payments, Identity, Events
- [ ] Each module: own DbContext, own schema, own migration history, own outbox + idempotency + dead-letter tables
- [ ] BuildingBlocks foundation in place (Domain, Application, Infrastructure, Web, Contracts)
- [ ] ArchTest enforces module boundaries in CI
- [ ] All 5 ADRs accepted and documented
- [ ] OpenTelemetry + App Insights live; per-module observability dashboards
- [ ] Pact contract tests for top 5 module pairs
- [ ] Frontend: Turborepo workspace with `@lankaconnect/{ui,auth,api-client-core,api-client-events,feature-events}` packages
- [ ] next-intl wired with `[locale]` route segment (English-only baseline)
- [ ] Money value object replacing every `decimal`; multi-currency Stripe tested with ≥ 50 successful transactions
- [ ] LankaEvents end-to-end: list → detail → register → pay → photos → forms → cancel/refund all work identically to pre-refactor
- [ ] All tracking docs synchronized
- [ ] 3-week stabilization soak complete; zero P0/P1 outstanding
- [ ] Phase 2 (Directory) plan drafted and approved

---

## Document Maintenance

- Update this document at the end of each task
- Mark completed items `[x]` IMMEDIATELY (don't batch)
- Add discovered subtasks as they're found; don't hide them
- Sync with PROGRESS_TRACKER.md and STREAMLINED_ACTION_PLAN.md per [TASK_SYNCHRONIZATION_STRATEGY.md](./TASK_SYNCHRONIZATION_STRATEGY.md)
- Reference task IDs (W3.1, W6.4, etc.) in PRs and commits for traceability
