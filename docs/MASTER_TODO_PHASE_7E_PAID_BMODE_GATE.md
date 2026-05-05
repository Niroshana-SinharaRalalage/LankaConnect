# Master TODO — Phase 7E follow-up: Paid Mode B Gate (deferred-feature handling)

**Status**: ✅ **SHIPPED + STAGING-VERIFIED** (2026-04-29).
**Architect review iteration**: 1 (6 edits applied — all incorporated).
**Commits**: `ca5314d6` (Slice 1 validator), `d4bac3ed` (Slice 2 DTO + mapper + handler integration), `84ca2d82` (Slice 3 FE coming-soon panel).
**Deploys**: backend `25115122343`, `25121840037`, `25123122840` — all `success`. UI `25121840030`, `25123122751` — all `success`.
**Legacy rollback**: `d543629f-…` reverted to `DetailedAttendees` via PUT (start date bumped to T+7 per architect edit #3).
**Prod scan @ 2026-04-29T18:03:48Z**: 3 events surveyed, 0 paid+B-mode events. Phase 7E not deployed to prod yet — registrationMode field absent.
**Staging scan @ 2026-04-29T18:05:24Z**: 59 events surveyed, 1 paid+B-mode event found (`d543629f-…`) and rolled back.
**Container log scan**: 1000-line window post-Slice-1 — zero `PaidHeadCountDeferred` failures from real (non-smoke) traffic.
**Origin**: Architect RCA on `Christmas Dinner Dance 2025` event showing a Mode-B form on a paid event with a dead-end "Coming soon (Phase 7E.3b)" submit error. Validator was target-state (paid+B = OK per plan §2); only free B-mode is implemented today (slice 7E.3a). UI/validator/domain disagreed → fillable-but-broken form.
**Architect RCA conclusion**: Tighten the validator (single source of truth) + carry a server-side `registrationModeStatus` so the frontend renders a clean "coming soon" panel instead of a fillable form for not-yet-implemented combinations.
**Out of scope for this slice**: Phase 7E.3b (paid B-mode + Stripe checkout) is queued separately and not started here.
**Related minor ticket (separate)**: copy bug in `HeadCountRsvpForm.tsx:178` — `{derivedTotal} of {spotsLeft}` mis-reads as "3 of 75 spots left" when only 3 are being requested. P3 polish; **not in this slice**.

---

## 1. Failure modes the fix must remove

| # | Symptom | Root cause |
|---|---|---|
| F1 | Paid event flipped to HeadCountByAge accepted by API | Validator allows paid+B per target-state plan |
| F2 | Mode picker on a paid event still offers B options | Allowed-modes query inherits validator → exposes B for paid |
| F3 | RsvpFormSection renders fillable HeadCountRsvpForm, RSVP click errors | Dispatcher branches on mode only, not on paid×mode×implementation status |
| F4 | Existing event `d543629f-…` already in paid+B state | One-off smoke artefact; needs rollback |
| F5 | No defensive evidence about prod | We haven't queried prod for paid+B events yet |

---

## 2. Slices — each slice ships its own deploy + API smoke

### Slice 1 — Validator gate (single source of truth)

**Goal**: `RegistrationModeCompatibility.Check(B-mode, ctx)` returns `Failure(RegistrationModeErrorCodes.PaidHeadCountDeferred)` when `!ctx.IsFreeAttendance`. Free + B-mode untouched.

**Files**:
- **NEW** [`src/LankaConnect.Domain/Events/Services/RegistrationModeErrorCodes.cs`](../src/LankaConnect.Domain/Events/Services/RegistrationModeErrorCodes.cs) — `public static class RegistrationModeErrorCodes { public const string PaidHeadCountDeferred = "PaidHeadCountDeferred"; }`. Architect-required (edit #2): real constant, not English-in-message, so frontend pattern-matching is stable across copy edits.
- [`src/LankaConnect.Domain/Events/Services/RegistrationModeCompatibility.cs`](../src/LankaConnect.Domain/Events/Services/RegistrationModeCompatibility.cs) — add `IsFreeAttendance` gate inside `CheckCommonHeadCountConstraints` (or new helper `CheckPaidImplementationGate`). Use the new constant. **Architect-required breadcrumb (edit #6)**: inline `// PHASE_7E_3B: remove this gate when paid B-mode + Stripe ships. See docs/MASTER_TODO_PHASE_7E_FLEXIBLE_REGISTRATION.md` directly above the `IsFreeAttendance` check so it's grep-discoverable.
- [`tests/.../Events/Domain/Phase7E2RegistrationModeCompatibilityTests.cs`](../tests/LankaConnect.Application.Tests/Events/Domain/Phase7E2RegistrationModeCompatibilityTests.cs) — extend `[Theory]` with paid + B-mode rows that expect Failure with the constant.

**TDD**:
1. RED `Check_Fails_WhenPaidAndBMode` — paid + HeadCountByAge → `Failure`, error equals `RegistrationModeErrorCodes.PaidHeadCountDeferred`.
2. RED parametrised over B1/B2/B3/B4.
3. GREEN: add the gate. Free + B-mode still passes (regression test).
4. RED `AllowedModes_ExcludesBModes_ForPaidEvents` — paid context returns `[DetailedAttendees]` (and not B*).
5. GREEN: cascades automatically because `AllowedModes` calls `Check`.

**Acceptance**: 4-7 new test rows, all green. No regression on the existing 27 [Theory] rows.

**Deploy**: `deploy-staging.yml` once tests + build green.

**API smoke**:
```bash
TOKEN=$(curl -sS -X POST 'https://lankaconnect-api-staging.../api/Auth/login' \
  -H 'Content-Type: application/json' \
  -d '{"email":"niroshhh@gmail.com","password":"1qaz!QAZ","rememberMe":true,"ipAddress":"x"}' \
  | python -c "import sys,json; print(json.load(sys.stdin)['accessToken'])")

# 1. Allowed modes for a PAID context — should now exclude B*
curl -sS 'https://...../api/Events/allowed-registration-modes?isFreeAttendance=false&hasDualPricing=true' \
  -H "Authorization: Bearer $TOKEN"
# Expected: ["DetailedAttendees"] (NOT B*)

# 2. Allowed modes for a FREE context — should still include B*
curl -sS 'https://...../api/Events/allowed-registration-modes?isFreeAttendance=true' \
  -H "Authorization: Bearer $TOKEN"
# Expected: ["DetailedAttendees", "HeadCountOnly", "HeadCountByAge", "HeadCountByGender", "HeadCountByAgeAndGender", "NoRegistration"]

# 3. Try to update a NEW paid event's mode to B — must 400 with PaidHeadCountDeferred
curl -sS -X PUT "https://...../api/Events/{newPaidEventId}" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  --data-binary @/c/tmp/paid-event-update-with-b.json
# Expected: HTTP 400 + error containing "PaidHeadCountDeferred"

# 4. Free Mode B regression — register against existing free B event 16eeb15c-…
curl -sS -X POST "https://...../api/events/16eeb15c-.../rsvp" -H "Authorization: Bearer $TOKEN" \
  -d '{...HeadCountByAge payload...}'
# Expected: HTTP 204
```

---

### Slice 2 — `EventDto.registrationModeStatus`

**Goal**: Server-side flag tells the frontend whether the event's *current* `registrationMode` is implementable. Two values: `'active'` and `'deferred'`. Frontend never re-implements policy.

**Why**: covers legacy events already flipped (like `d543629f-…`) without forcing rollback; future-proofs against any other slice that ships a target-state validator ahead of implementation.

**Files**:
- [`src/LankaConnect.Application/Events/Common/EventDto.cs`](../src/LankaConnect.Application/Events/Common/EventDto.cs) — add `public string RegistrationModeStatus { get; init; } = "deferred";`. **Architect-required (edit #1)**: default is `"deferred"` (fail-safe). Any code path that forgets to populate falls into the "coming soon" panel rather than rendering a fillable dead-end form. The mapper (the only producer) explicitly sets it to `"active"` when compatibility passes.
- [`src/LankaConnect.Application/Common/Mappings/EventMappingProfile.cs`](../src/LankaConnect.Application/Common/Mappings/EventMappingProfile.cs) — `.ForMember(dst => dst.RegistrationModeStatus, opt => opt.MapFrom(src => ComputeStatus(src)))` where `ComputeStatus` builds a `RegistrationModeContext` from `src` and runs `RegistrationModeCompatibility.Check(src.RegistrationMode, ctx)` — `"active"` if Success, `"deferred"` if Failure (using `RegistrationModeErrorCodes.PaidHeadCountDeferred` as the deciding signal).
- [`web/src/infrastructure/api/types/events.types.ts`](../web/src/infrastructure/api/types/events.types.ts) — add `registrationModeStatus: 'active' | 'deferred'` to `EventDto` interface.

**TDD**:
1. RED `EventMapper_Returns_Deferred_ForPaidBModeEvent` (mapper unit) — Event with `IsFree=false` + `RegistrationMode=HeadCountByAge` → mapper produces `RegistrationModeStatus = "deferred"`.
2. RED `EventMapper_Returns_Active_ForFreeBModeEvent` — same shape but free → `"active"`.
3. RED `EventMapper_Returns_Active_ForLegacyDetailedAttendeesEvent` — Mode A always active.
4. GREEN: add the mapping rule.
5. **Architect-required (edit #5)**: RED `GetEventByIdQueryHandler_PopulatesRegistrationModeStatus_EndToEnd` — handler-level integration test asserting the field round-trips through the actual handler (not just the mapper unit). Catches DI/profile-registration breaks that mapper-only tests miss.

**Acceptance**: 3 mapper unit tests + 1 handler integration test green. Existing event-mapping suite still green.

**Deploy**: `deploy-staging.yml`.

**API smoke**:
```bash
# Existing paid+B legacy event → registrationModeStatus = "deferred"
curl -sS "https://...../api/Events/d543629f-..." -H "Authorization: Bearer $TOKEN" \
  | python -c "import sys,json; print(json.load(sys.stdin).get('registrationModeStatus'))"
# Expected: deferred

# Free Mode B event → "active"
curl -sS "https://...../api/Events/16eeb15c-..." -H "Authorization: Bearer $TOKEN" \
  | python -c "import sys,json; print(json.load(sys.stdin).get('registrationModeStatus'))"
# Expected: active

# Free Mode A event (legacy) → "active"
curl -sS "https://...../api/Events/c0cd6cfd-..." -H "Authorization: Bearer $TOKEN"
# Expected: active
```

---

### Slice 3 — Frontend: "Coming soon" panel for deferred mode

**Goal**: `RsvpFormSection` reads `event.registrationModeStatus`. If `'deferred'`, render a read-only panel ("Paid head-count registration is not yet supported. Please contact the organiser directly.") with the existing organiser-contact card. Don't render the fillable `HeadCountRsvpForm`.

**Why**: kills the dead-end UX immediately, even for events stuck in legacy state until Phase 7E.3b ships.

**Files**:
- [`web/src/presentation/components/features/events/RsvpFormSection.tsx`](../web/src/presentation/components/features/events/RsvpFormSection.tsx) — branch on `event.registrationModeStatus === 'deferred'` BEFORE the existing mode dispatch. Use the same `Info`/blue card style as the Mode C "No registration required" notice for visual consistency.
- [`web/src/presentation/components/features/events/__tests__/RsvpFormSection.test.tsx`](../web/src/presentation/components/features/events/__tests__/RsvpFormSection.test.tsx) — new test file (none exists today) covering: deferred → renders panel + organiser contact link, not the form; active + B mode → renders HeadCountRsvpForm; active + Mode C → renders no-registration notice; active + Mode A → renders EventRegistrationForm.
- Reuse the existing `Info` icon + blue card pattern from RsvpFormSection's Mode C branch — no new design tokens.

**TDD**:
1. RED `renders_coming_soon_panel_when_status_is_deferred`.
2. RED `renders_HeadCountRsvpForm_when_active_and_BMode`.
3. RED `renders_NoRegistration_notice_when_NoRegistration_mode`.
4. RED `renders_EventRegistrationForm_when_active_and_DetailedAttendees`.
5. GREEN: add the conditional branch.

**Acceptance**: 4 new RsvpFormSection tests green. `npx tsc --noEmit` clean.

**Deploy**: `deploy-ui-staging.yml`.

**Manual UI smoke**:
- Open `d543629f-…` (paid+B legacy) → see "coming soon" panel, not the form.
- Open `16eeb15c-…` (free+B) → still see HeadCountRsvpForm with Adults/Children spinners.
- Open Mode C event → still see "No registration required" notice.
- Open any Mode A event → still see EventRegistrationForm.

---

### Slice 4 — Legacy event recovery

**Goal**: Roll back `d543629f-…` to `DetailedAttendees`. Confirm no other paid+B events on staging or prod.

**Why this slice exists**: even with Slices 1-3, `d543629f-…` will display the "coming soon" panel — which is correct UX, but the user may want their Christmas event back to its original A mode for normal operation.

**Steps**:
1. Query staging: list events where `registration_mode IN (1,2,3,4) AND is_free = false`. Two paths:
   - Preferred: API endpoint `GET /api/admin/events?paidBModeOnly=true` if it exists (probably doesn't — check `AdminController`).
   - Fallback: write a one-off script that pages through `GET /api/Events?pageSize=200` and filters in memory by `registrationMode + isFree` (only requires existing endpoints).
2. Repeat for prod.
3. For each match (expected: `d543629f-…` only on staging, zero on prod):
   ```bash
   # Pull the current event detail
   curl -sS "https://...../api/Events/{id}" -H "Authorization: Bearer $TOKEN" > /c/tmp/rollback.json

   # Build a PUT body that:
   #  (a) flips RegistrationMode back to "DetailedAttendees"
   #  (b) bumps startDate to (now + 7 days) and endDate to (now + 7 days + originalDuration).
   #      Architect-required (edit #3): the existing UpdateEvent guard rejects past start dates,
   #      so a no-op PUT on a Dec-2025 event would 400. Bumping to T+7 keeps the rollback in
   #      the audit log as a single PUT (no domain back-door, no SQL).
   python -c "<...>" > /c/tmp/rollback-update.json

   # PUT
   curl -sS -X PUT "https://...../api/Events/{id}" -H "Authorization: Bearer $TOKEN" \
     -H 'Content-Type: application/json' --data-binary @/c/tmp/rollback-update.json
   # Expected: HTTP 200
   ```
4. Verify: re-fetch the event, check `registrationMode == "DetailedAttendees"` AND `registrationModeStatus == "active"`.

**Acceptance**: Zero paid+B events remaining on staging or prod. Each rollback PUT returns HTTP 200. Each post-rollback fetch shows the active form path.

**No deploy** — pure state cleanup.

---

### Slice 5 — Tracking docs

**Files**:
- [`docs/PROGRESS_TRACKER.md`](./PROGRESS_TRACKER.md) — new entry summarising the slice + commits + deploy run IDs + smoke evidence.
- [`docs/STREAMLINED_ACTION_PLAN.md`](./STREAMLINED_ACTION_PLAN.md) — full slice entry following existing format.
- [`docs/MASTER_TODO_PHASE_7E_FLEXIBLE_REGISTRATION.md`](./MASTER_TODO_PHASE_7E_FLEXIBLE_REGISTRATION.md) — note this fix in the "Deferred to Phase 7F / 7E.3b" section so the gate's removal is on the 7E.3b checklist.
- This master TODO file (`MASTER_TODO_PHASE_7E_PAID_BMODE_GATE.md`) — close out, link to the commits.

---

## 3. Risk register + mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Slice 1 breaks free Mode B (regression) | Low | High | Existing 27 [Theory] rows + new free-B-mode regression test in Slice 1. Smoke `16eeb15c-…` after deploy. |
| Slice 2 mapper rule too aggressive (marks active as deferred) | Low | Medium | TDD with explicit cases for Mode A always active + free + Mode B always active. |
| Slice 3 panel hides organiser contact | Low | Low | Test asserts contact block is visible. |
| Slice 4 rollback PUT hits "Start date in the past" guard | Medium (for old events) | Low | Update body should send the existing start date as-is; the date guard fires only if you try to change start date — verify by reading the current code path. If it does fire, set start date to a future-near-past combo or use admin override. |
| Prod has unknown paid+B events | Low | Medium | Slice 4 step 2 explicitly queries prod before declaring done. |
| Phase 7E.3b implementation forgets to remove the gate | Low | Low | Note in `MASTER_TODO_PHASE_7E_FLEXIBLE_REGISTRATION.md` that gate removal is part of the 7E.3b ship checklist. |

---

## 4. Order of execution + commit boundaries

```
Slice 1 (validator)                   → 1 commit, deploy-staging, API smoke
  └── Slice 2 (DTO)                   → 1 commit, deploy-staging, API smoke
        └── Slice 3 (frontend)        → 1 commit, deploy-ui-staging, manual smoke
              └── Slice 4 (rollback)  → 0 commits, just curl
                    └── Slice 5 (docs)→ 1 commit
```

Backend (Slices 1+2) lands first because Slice 3 reads the new field. Could combine 1+2 into a single commit/deploy if the architect prefers — flagged as a question.

---

## 5. Definition of Done (whole slice)

- [ ] Slice 1 validator tests green (≥4 new theory rows + regression). Build clean. Backend deploy success.
- [ ] Slice 2 mapper tests green (3 new) + handler integration test green (1 new). Build clean. Backend deploy success.
- [ ] Slice 1+2 API smoke: paid context excludes B from allowed modes; legacy paid+B event returns `registrationModeStatus: deferred`; free B event returns `active`.
- [ ] Slice 3 RsvpFormSection tests green (4 new). `npx tsc --noEmit` clean. UI deploy success.
- [ ] Slice 3 manual UI smoke: deferred event shows panel; free B shows form; Mode A unchanged; Mode C unchanged.
- [ ] Slice 4: zero paid+B events remaining on staging + prod. Rollback PUT(s) success.
- [ ] **Architect-required (edit #4a)**: container-log scan post-Slice-1 deploy — zero `PaidHeadCountDeferred` failures triggered by real (non-smoke) traffic. Free traffic must NOT hit the gate.
- [ ] **Architect-required (edit #4b)**: prod paid+B scan results documented in this file before close — row count + UTC timestamp, even if zero. Audit-trail evidence.
- [ ] Slice 5: PROGRESS_TRACKER + STREAMLINED_ACTION_PLAN updated. 7E.3b ship checklist links the gate-removal task.
- [ ] Container logs: zero unexpected exceptions during smoke.

---

## 6. Architect notes

**Review iteration 1** (architect-approved with edits, applied above):

| # | Edit | Where applied |
|---|---|---|
| 1 | Default `RegistrationModeStatus` to `"deferred"` (fail-safe, not `"active"`) | Slice 2 |
| 2 | Promote error code to `RegistrationModeErrorCodes.PaidHeadCountDeferred` constant | Slice 1 (new file) |
| 3 | Slice 4 rollback: bump start date to T+7 days as part of the PUT (no SQL, no back-door) | Slice 4 |
| 4 | DoD additions: container-log scan + prod-scan-evidence with row count + timestamp | Slice 5 / DoD |
| 5 | Slice 2: add handler-level integration test (catches DI/profile breaks the mapper unit misses) | Slice 2 TDD |
| 6 | Inline `// PHASE_7E_3B: remove this gate when paid B-mode + Stripe ships` breadcrumb above the validator gate | Slice 1 |

**Slice ordering** (Slices 1, 2, 3 as separate commits) confirmed correct — different blast radii, clean bisect. Two deploys safer than one.
