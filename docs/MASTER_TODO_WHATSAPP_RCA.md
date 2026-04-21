# MASTER TODO — WhatsApp Silent-Drop-Off RCA & Remediation

**Owner**: backend + frontend
**Created**: 2026-04-20
**Last updated**: 2026-04-21
**Plan reference**: architect-approved 7-step remediation plan (derived from the silent-drop-off RCA — users who turned WhatsApp on but never verified their phone silently never received any messages because `UserWhatsAppPreferences.ShouldNotify()` returned `bool` and every skip logged as "opted out", masking four different failure modes)

---

## Summary of fixes

| # | Fix | Scope | Priority |
|---|-----|-------|----------|
| 0 | Unblock `PUT /api/whatsapp/preferences` (Zod boundary `"" → null`) | frontend | done |
| 1 | `WhatsAppSkipReason` enum taxonomy replacing "opted out" log | backend domain + application | done |
| 2 | `EvaluateSkipReason()` discriminator on `UserWhatsAppPreferences` | backend domain | done |
| 5 | Admin metric `usersEnabledButUnverified` on `WhatsAppMetricsDto` | backend application | done |
| 3 | UX enforcement — auto-request verification code on enable + persistent unverified banner | frontend | pending |
| 4 | Daily scheduled job auto-disabling WhatsApp after 30-day verification grace + notification email | backend | pending |
| 6 | (Deferred) persist skip-reason on `WhatsAppMessageRecord` for historical analytics | backend + migration | deferred |

---

## Fix #0 — Boundary normalisation for empty time pickers

- [x] Root-cause empty `<input type="time">` submitting `""` → .NET `TimeOnly?` rejects → 400
- [x] Add `nullableTrimmedString` Zod transform in `web/src/presentation/lib/validators/whatsapp.schemas.ts`
- [x] Split types: `UpdatePreferencesFormInput` (pre-transform) vs `UpdatePreferencesFormData` (post-transform)
- [x] 3-generic `useForm<Input, unknown, Data>` on `WhatsAppPreferences.tsx`
- [x] 7 Vitest cases covering `""`→null, combined, populated passthrough, explicit null, omitted undefined
- [x] `npx tsc --noEmit` clean
- [x] Commit + push (`33ccc542`)
- [x] Deploy via `deploy-ui-staging.yml` (run `24696324247`)
- [ ] Browser smoke on staging (enable → clear time pickers → Save → 200)
- [ ] Curl smoke: direct `"quietHoursStart":null` → 200

**Status**: code shipped; browser + curl smoke deferred to Fix 3 session (will exercise the same form).

---

## Fix 1+2+5 — Skip-reason enum taxonomy + unverified-cohort metric

### Domain
- [x] New `WhatsAppSkipReason.cs` enum with explicit numbering (`GloballyDisabled=1` … `Deduplicated=7`)
- [x] `UserWhatsAppPreferences.EvaluateSkipReason(type)` discriminator with root-cause-over-symptom ordering (`WhatsAppDisabled` > `PhoneUnverified` > `TypeDisabled`)
- [x] `ShouldNotify(type)` collapsed to thin facade `=> EvaluateSkipReason(type) is null`
- [x] Reuse existing `IsFullyVerified` (rejected architect suggestion of duplicate `EffectivelyEnabled`)
- [x] Facade-invariant test iterating all `WhatsAppNotificationType` values to prevent future drift

### Application
- [x] `WhatsAppSendResult.SkipReasonCode` (optional enum) + overloaded `Skipped(WhatsAppSkipReason, string)` factory; legacy `Skipped(reason)` preserved
- [x] `WhatsAppService.cs` all 5 skip branches call `EvaluateSkipReason` + log `SkipReason={enum}` structured property
- [x] Helper `BuildSkipMessage(WhatsAppSkipReason, WhatsAppNotificationType)` for human-readable response strings

### Repository + metric
- [x] `IUserWhatsAppPreferencesRepository.GetUsersEnabledButUnverifiedCountAsync(ct)`
- [x] `UserWhatsAppPreferencesRepository` implementation — `AsNoTracking().CountAsync(p => p.WhatsAppEnabled && !p.PhoneVerified, ct)` with LogContext + Stopwatch + Npgsql `SqlState` logging
- [x] `WhatsAppMetricsDto.UsersEnabledButUnverified : int`
- [x] `GetWhatsAppMetricsQueryHandler` constructor injects repo, awaits count, forwards to DTO
- [x] Handler unit test `Handle_Includes_UsersEnabledButUnverified_From_Preferences_Repository`

### Verification
- [x] `dotnet build` — 0 errors
- [x] WhatsApp test filter — 256/256 green (146 Application + 87 Domain + 23 Infrastructure)
- [x] Commit + push (`4428236b`)
- [x] `deploy-staging.yml` run `24699949763` — conclusion: success
- [x] App health: `POST /api/Auth/login` → 200 (proves new DI injection resolved)
- [x] Admin smoke: `GET /api/whatsapp-admin/metrics?from=...&to=...` → HTTP 200 with `"usersEnabledButUnverified":2` in body (admin@lankaconnect.com token)
- [ ] Log smoke: trigger one send to an unverified user, confirm `SkipReason=PhoneUnverified` appears in container logs (instead of the old "opted out" string)
- [x] Docs sync — `PROGRESS_TRACKER.md`, `STREAMLINED_ACTION_PLAN.md`, `TASK_SYNCHRONIZATION_STRATEGY.md` (commit `c9071696`)

### Scope deliberately excluded
- [ ] Persist `SkipReasonCode` on `WhatsAppMessageRecord` (Fix #6) — would need migration + schema change; skipped messages aren't written to DB today, so enum is log-and-response-only for this slice

---

## Fix 3 — UX enforcement (SHIPPED — awaiting staging browser smoke)

Goal: eliminate the "enabled but never verified" silent drop-off cohort at the source.

### Planned work
- [x] When user toggles WhatsApp on, auto-fire `POST /api/whatsapp/request-verification` immediately (no separate "send code" click) — `WhatsAppOptIn.tsx::handleEnable`
- [x] Persistent amber banner on `/profile` page only when `whatsAppEnabled && !phoneVerified` — new `WhatsAppUnverifiedBanner.tsx`, masks phone to last 4 digits
- [x] Banner appears on profile page ONLY — wired in `app/(dashboard)/profile/page.tsx` at top of main content; self-hides via guard clauses so safe to drop elsewhere later
- [x] Banner includes one-click resend + inline 6-digit code entry
- [x] Vitest coverage: 3 tests for auto-request (happy path, enable-fails-no-auto-request, manual-send-button regression guard) + 10 tests for banner (visibility truth table, phone masking, 6-digit gating, rate-limit lockout branch)
- [ ] Browser smoke on staging (after deploy-ui-staging.yml completes)

### Non-goals
- No banner on other pages
- No modal / blocking UX
- No changes to the existing 5-attempts/1h-lockout on `UserWhatsAppPreferences` (already correct)

### Files shipped
- **MODIFIED** `web/src/presentation/components/features/whatsapp/WhatsAppOptIn.tsx` — `handleEnable` now chains auto-request after enable, with inner try/catch so auto-request failure falls back to manual button
- **NEW** `web/src/presentation/components/features/whatsapp/WhatsAppUnverifiedBanner.tsx` — self-hiding amber banner, `role="alert" aria-live="polite"`, `maskPhone()` helper, rate-limit lockout branch
- **MODIFIED** `web/src/app/(dashboard)/profile/page.tsx` — import + render `<WhatsAppUnverifiedBanner />` at top of main content
- **NEW** `web/tests/unit/presentation/components/features/whatsapp/WhatsAppOptIn.autoRequest.test.tsx` (3 tests)
- **NEW** `web/tests/unit/presentation/components/features/whatsapp/WhatsAppUnverifiedBanner.test.tsx` (10 tests)

### Regression verification
- 13/13 Fix 3 tests green; `npx tsc --noEmit` clean
- Broader profile-component batch shows 26 pre-existing failures in `CulturalInterestsSection.test.tsx` + `PreferredMetroAreasSection.test.tsx` (missing `QueryClientProvider` wrapper in test harness) — reproduced with Fix 3 stashed, NOT caused by this slice

---

## Fix 4 — Auto-disable stale unverified preferences (UNSTARTED)

Goal: prevent indefinite "enabled but never verified" rows from accumulating; notify affected users.

### Planned work
- [ ] New `ExpireUnverifiedWhatsAppPreferencesJob` scheduled daily (Hangfire)
- [ ] Query: `WhatsAppEnabled=true AND PhoneVerified=false AND WhatsAppEnabledAt < UtcNow - 30 days`
- [ ] For each stale row: set `WhatsAppEnabled=false` via a new domain method `AutoDisableUnverified(reason)` that records the reason in an audit column
- [ ] New audit column `WhatsAppAutoDisabledAt` + `WhatsAppAutoDisableReason` on `UserWhatsAppPreferences` (EF migration with `.Designer.cs` companion — never hand-create)
- [ ] Notification email via existing `EmailTemplateContract` mechanism: "We turned WhatsApp off because you never verified. Re-enable anytime."
- [ ] Job observability: Hangfire dashboard + structured log `UnverifiedWhatsAppAutoDisabled: Count={count}, Duration={ms}`
- [ ] Integration test covering: stale row auto-disabled, fresh row untouched, already-verified row untouched, email dispatched fire-and-forget (MEMORY 6A.122)

### Open questions for architect
- Grace period duration: 30 days is a first-pass pick. Worth instrumenting first (via the Fix-5 metric now live) for a week to size it on real data before committing?
- Should Fix 3 banner show countdown ("WhatsApp will auto-disable in N days if unverified")?

---

## Overall status snapshot (2026-04-20)

- **Fixes shipped**: 0, 1, 2, 3, 5 (5 of 6 planned)
- **Staging-verified end-to-end**: Fix 1+2+5 (app healthy + admin metric returns new field, `usersEnabledButUnverified: 2`)
- **Remaining smoke**: browser smoke for Fix 3 (auto-request + banner visibility) on staging after deploy-ui; log-side check for Fix 1+2+5 (needs a real skip event)
- **Remaining work**: Fix 4 (auto-disable job + 30-day grace + notification email + EF migration)
- **Deferred**: Fix 6 (persist skip-reason on message records)
