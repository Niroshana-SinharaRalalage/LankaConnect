# Phase 6A.153 — Organizer-controlled Registration Window (open/close gating)

**Date opened:** 2026-05-25
**Branch:** `feat/phase-6a-153-registration-window` off `main`
**Status:** 🔧 In progress — TDD, no commits yet

## Goal in one sentence

Let organizers publish an event in advance with registration held closed until a date they choose; the Register/RSVP button still renders, and clicking it before the window opens shows a "Registration opens [LOCAL DATE]" modal instead of the RSVP form.

## Decisions locked-in (architect-approved 2026-05-25)

| # | Decision | Locked |
|---|---|---|
| D1 | **Two fields** on `Event` aggregate: `RegistrationOpensAt`, `RegistrationClosesAt`. Both `DateTime?` UTC. ClosesAt hidden behind organizer-form "Advanced" disclosure. | ✅ |
| D2 | **Nullable = "always open"** (backward-compatible). `null` `OpensAt` → open now; `null` `ClosesAt` → open until StartDate (or indefinitely on TBD events). | ✅ |
| D3 | **Validation invariants** in domain mutator: `opensAt < closesAt`; `closesAt <= StartDate` (when StartDate set); `opensAt < StartDate` (when StartDate set); TBD events allowed any window. | ✅ |
| D4 | **Editable in Planning/Draft/Published**, locked in Cancelled/Completed. No "first-registration-locks-it" rule (window only gates future register attempts). | ✅ |
| D5 | **Date window is the only mechanism** — no separate "pause now" toggle. Pause = `ClosesAt = now`. (User-confirmed.) | ✅ |
| D6 | **New dedicated DTO field** `RegistrationAvailability` (string union: `"open" \| "not-yet-open" \| "closed-by-organizer" \| "closed-event-started"`). Do NOT overload `RegistrationModeStatus`. | ✅ |
| D7 | DTO surfaces raw `RegistrationOpensAt` + `RegistrationClosesAt` UTC timestamps; FE formats in event-local timezone (same plumbing as `StartDate`). | ✅ |
| D8 | **Domain owns the guard** (defense-in-depth, single source of truth). Handlers translate `Result.Failure`, do not duplicate the check. | ✅ |
| D9 | Error wording: `"Registration for this event opens at {opensAt:o}"` and `"Registration for this event has closed"`. Strings are API fallback; FE reads DTO state. | ✅ |
| D10 | **Click-through modal pattern** for `not-yet-open` state (user verbatim ask). Inline microcopy for `closed-by-organizer` state (user-confirmed). | ✅ |
| D11 | Organizer form: 2 datetime pickers under "Registration Window (Optional)" heading. `OpensAt` prominent; `ClosesAt` behind "Advanced" disclosure. Pickers operate in event-local timezone, post ISO-8601 UTC. | ✅ |

## Scope of changes

### Domain (`LankaConnect.Domain`)
**File:** `src/LankaConnect.Domain/Events/Event.cs`
- Add `RegistrationOpensAt: DateTime?` and `RegistrationClosesAt: DateTime?` private-set properties
- Add `SetRegistrationWindow(opensAt: DateTime?, closesAt: DateTime?)` mutator with D3 invariants + D4 status gate
- Add window guards to `Register`, `RegisterAnonymous`, `RegisterWithAttendees` after Status/StartDate guards

### Application (`LankaConnect.Application`)
**Files:**
- `src/LankaConnect.Application/Events/Common/EventDto.cs` — add 3 new fields
- `src/LankaConnect.Application/Common/Mappings/EventMappingProfile.cs` — add `ComputeRegistrationAvailability(src)` helper + map `RegistrationOpensAt` / `RegistrationClosesAt`
- `src/LankaConnect.Application/Events/Commands/CreateEvent/CreateEventCommand.cs` — add `RegistrationOpensAt`, `RegistrationClosesAt` optional fields
- `src/LankaConnect.Application/Events/Commands/UpdateEvent/UpdateEventCommand.cs` — same
- Both handlers call `SetRegistrationWindow` when fields present

### Infrastructure (`LankaConnect.Infrastructure`)
**Migration:** `Phase6A153_AddEventRegistrationWindow` — 2 nullable `timestamp with time zone` columns + 2 indexes on `(registration_opens_at)` and `(registration_closes_at)`. Verify `[Migration("...")]` attribute on Designer.cs.

### API
No new endpoints. Existing `POST /api/events/{id}/register-anonymous-attendee` and `POST /api/events/{id}/rsvp` will fail with 400 + ISO-8601 timestamp in the error message when window closed/not-open. Existing `POST /api/events` (create) and `PATCH /api/events/{id}` (update) take the new optional fields.

### Frontend (`web/`)
**Files:**
- `web/src/app/events/[id]/page.tsx` — new conditional branch in the registration cascade (lines ~1245-1347): `not-yet-open` → modal on click; `closed-by-organizer` → inline microcopy. Ordering: Cancelled → ExternalPaid → already-registered → past-started → **not-yet-open** → **closed-by-organizer** → full-waitlist → RSVP form
- `web/src/presentation/components/features/events/RegistrationOpensSoonModal.tsx` — new modal showing formatted local time
- `web/src/presentation/components/features/events/EventCreateForm.tsx` and `EventEditForm.tsx` (or whichever organizer create/edit form lives in the tree) — add 2 datetime pickers under "Registration Window (Optional)" with `ClosesAt` behind an "Advanced" disclosure
- `web/src/infrastructure/api/types/events.types.ts` — add `RegistrationOpensAt`, `RegistrationClosesAt`, `RegistrationAvailability` types

## Test matrix (27 cases)

Locked in architect report; covers domain mutator (1-18), application/mapper (19-22), FE (23-25), API integration (26-27). 90%+ coverage target per CLAUDE.md §2.

## Risks (all low)

- **Cache staleness**: DTO default `"open"` → stale-cache users get legacy behavior, no crash.
- **ExternalPaid + window**: ExternalPaid CTA wins (test pinned).
- **TBD events**: window allowed independently of StartDate.
- **Email triggers / Hangfire**: untouched. No coupling. Pinned in test.

## Phase reservation (4-source check, 2026-05-25)

- Master index `PHASE_6A_MASTER_INDEX.md`: highest row 6A.152; **6A.153 absent** ✅
- `git log --all`: **no matches** for 6A.153 ✅
- `git branch -a`: **no matches** ✅
- `docs/MASTER_TODO_PHASE_6A_153*.md`: this file is the first ✅

## Deploy plan

1. Backend domain + app + migration → unit tests RED → GREEN → full app test suite passes
2. Frontend organizer form + modal + types → unit tests RED → GREEN
3. Push branch + trigger `deploy-staging.yml` (backend) + `deploy-ui-staging.yml` (UI) together
4. Verify EF migration applied on staging DB (`SELECT column_name FROM information_schema.columns WHERE table_name='events' AND column_name LIKE 'registration_%'`)
5. API smoke: 3 curls — (a) anon register against future-Opens event → 400; (b) authed RSVP against past-Closes event → 400; (c) normal register against null-window event → 200
6. Browser UAT on staging: create event with future OpensAt → button click shows modal with formatted local time → wait/edit to bring window into past → form renders normally
7. Update `PROGRESS_TRACKER.md` + `STREAMLINED_ACTION_PLAN.md`
8. PR to main
