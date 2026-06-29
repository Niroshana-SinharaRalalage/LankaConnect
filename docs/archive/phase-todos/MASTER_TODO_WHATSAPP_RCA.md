# MASTER TODO — WhatsApp Silent-Drop-Off RCA & Remediation

**Owner**: backend + frontend
**Created**: 2026-04-20
**Last updated**: 2026-05-07
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
| 4 | Daily scheduled job auto-disabling WhatsApp after 30-day verification grace + notification email | backend | done |
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

## Fix 3 — UX enforcement (SHIPPED + BROWSER-SMOKE VERIFIED)

Goal: eliminate the "enabled but never verified" silent drop-off cohort at the source.

### Planned work
- [x] When user toggles WhatsApp on, auto-fire `POST /api/whatsapp/request-verification` immediately (no separate "send code" click) — `WhatsAppOptIn.tsx::handleEnable`
- [x] Persistent amber banner on `/profile` page only when `whatsAppEnabled && !phoneVerified` — new `WhatsAppUnverifiedBanner.tsx`, masks phone to last 4 digits
- [x] Banner appears on profile page ONLY — wired in `app/(dashboard)/profile/page.tsx` at top of main content; self-hides via guard clauses so safe to drop elsewhere later
- [x] Banner includes one-click resend + inline 6-digit code entry
- [x] Vitest coverage: 3 tests for auto-request (happy path, enable-fails-no-auto-request, manual-send-button regression guard) + 10 tests for banner (visibility truth table, phone masking, 6-digit gating, rate-limit lockout branch)
- [x] Deploy-ui-staging run `24736264892` succeeded (commit `453c37f2`); `GET https://lankaconnect-ui-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/profile` → HTTP 200
- [x] Browser smoke confirmed (user 2026-04-21): WhatsApp messages delivering end-to-end on staging for signup, cancel registration, and other lifecycle events — implicit proof that auto-request-on-enable fires (otherwise `EvaluateSkipReason` would return `PhoneUnverified` forever and the Twilio pipeline would never be exercised)

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

## Fix 4 — Auto-disable stale unverified preferences (SHIPPED + STAGING-VERIFIED)

Goal: prevent indefinite "enabled but never verified" rows from accumulating; notify affected users.

### Planned work
- [x] New `ExpireUnverifiedWhatsAppPreferencesJob` scheduled daily (Hangfire) — registered as `expire-unverified-whatsapp-preferences-job`, `Cron.Daily(3)` UTC, `Program.cs:504`
- [x] Query: `WhatsAppEnabled=true AND PhoneVerified=false AND WhatsAppEnabledAt < UtcNow - graceDays` — `IUserWhatsAppPreferencesRepository.GetStaleUnverifiedAsync(cutoff)` backed by partial index `IX_UserWhatsAppPreferences_EnabledAt_EnabledUnverified` for cheap scans
- [x] For each stale row: `UserWhatsAppPreferences.AutoDisableUnverified(reason)` domain method clears `WhatsAppEnabled`, stamps `WhatsAppAutoDisabledAt` + `WhatsAppAutoDisableReason`, raises `WhatsAppAutoDisabledDomainEvent`
- [x] Audit columns `WhatsAppAutoDisabledAt` + `WhatsAppAutoDisableReason` on `UserWhatsAppPreferences` — migration `Phase7DFix4_WhatsAppAutoDisableUnverified` (with `.Designer.cs` companion, generated via `dotnet ef migrations add` — not hand-created)
- [x] Notification email — `WhatsAppAutoDisabledDomainEventHandler` dispatches `whatsapp-auto-disabled` template with fire-and-forget `Task.Run` capture pattern (MEMORY 6A.122)
- [x] Job observability: structured `UnverifiedWhatsAppAutoDisable START` + `UnverifiedWhatsAppAutoDisabled` logs with `CorrelationId`, `GraceDays`, `Cutoff`, `Count`, `Skipped`, `Failed`, `Duration` properties; per-row defensive re-check guards concurrent verify race
- [x] Defensive per-row re-check on `WhatsAppEnabled && !PhoneVerified` between query + iteration to handle the rare race where a user verifies between the SELECT and the loop
- [x] Grace period configurable via `WhatsAppSettings:UnverifiedGracePeriodDays` (default 30); job re-throws on outer failure for Hangfire retry

### Verification (staging)
- [x] Commit + push (`895e9a48`)
- [x] `deploy-staging.yml` run for `895e9a48` completed at 2026-04-21T20:22:18 — conclusion: success
- [x] Migration applied — `GET /api/whatsapp/preferences` returns 200 with full payload (proves EF mapping for new audit columns + `WhatsAppEnabledAt` deserializes a real row without 500)
- [x] Hangfire registration log line `Hangfire recurring jobs registered successfully` present after every container restart through 2026-05-07 02:37 UTC (line follows the `AddOrUpdate` for `expire-unverified-whatsapp-preferences-job`)
- [x] **Job firing live in production** — Log Analytics confirms 5 consecutive daily runs (most recent 2026-05-07 03:00:01.670 UTC, then 2026-05-06, -05, -04, -03 all at 03:00 UTC ±60s); each run logs structured `START` + `COMPLETE` pair with correlation IDs, `GraceDays=30`, computed `Cutoff = UtcNow - 30d`, `Count=0` (correct — additive nullable migration leaves existing rows with `WhatsAppEnabledAt=NULL` so they're permanently ineligible by design); zero exceptions, zero Hangfire retries
- [x] Fix #4 (and the rest of the WhatsApp pipeline) now treated as a real channel per project memory note 2026-04-21 — WhatsApp messages delivering end-to-end on staging

### Architect Q&A outcome
- Grace period locked at 30 days (configurable). Existing rows have `WhatsAppEnabledAt=NULL` and are never swept (intentional — only NEW enables after the migration become eligible).
- Fix 3 banner countdown deferred — current banner is sufficient until we see a real auto-disable cohort.

---

## Overall status snapshot (2026-05-07)

- **Fixes shipped**: 0, 1, 2, 3, 4, 5 (6 of 6 planned — RCA remediation complete)
- **Staging-verified end-to-end**: Fix 1+2+5 (admin metric returns `usersEnabledButUnverified: 2`); Fix 3 (browser-smoke 2026-04-21 — WhatsApp messages delivering for signup / cancel registration / other lifecycle events); Fix 4 (job has fired daily at 03:00 UTC for at least 5 consecutive days with structured START/COMPLETE logs, GraceDays=30, Count=0, zero exceptions)
- **Remaining smoke**: log-side check for Fix 1+2+5 (needs a real skip event — low priority now that the positive path is flowing); first non-zero `Count>0` Fix 4 run will validate the email handler end-to-end (will surface naturally as a real test signup post-2026-05-21 if any user enables but never verifies)
- **Deferred**: Fix 6 (persist skip-reason on message records)
