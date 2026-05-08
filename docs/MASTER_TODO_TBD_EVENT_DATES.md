# Master TODO — TBD Event Dates (Phase 8YA)

**Goal:** Allow event organizers to create events for "upcoming" dates without committing to a specific start/end date yet ("dates TBD").

**Classification:** Feature-missing — dates are required end-to-end (DB NOT NULL → Domain non-nullable → Command non-nullable → DTO non-nullable → zod `min(1)` → form HTML5 input).

**Architect verdict (2026-05-08):** Use **Option 3 — `EventStatus.Planning` lifecycle state + nullable `DateTime?`**. Hybrid that keeps schema migration trivial (`DROP NOT NULL`) while reducing real null-checks from ~30 sites to ~10 by routing most date reads through a centralised `EmailDateTimeHelper.FormatEventDate(DateTime?)` and `EventExtensions.GetDisplayLabel`.

**Rejected alternatives:**
- **Option 1 (full nullable, no state)** — every new feature in 6 months will forget the null-guard.
- **Option 2 (sentinel + `IsDatesTbd` flag)** — exactly the silent-failure class of MEMORY.md Phase 6A.122. Reminder job `StartDate <= UtcNow.AddHours(24)` against a `9999-12-31` sentinel silently never fires.

---

## User answers (Q1–Q4) — locked 2026-05-08

| Q | Answer | Implication |
|---|---|---|
| Q1 — Public listing of TBD events | **A** — Yes, listed | TBD events appear in `/events`, sorted to bottom, with "Date TBD" badge. `Publish()` is allowed on a `Planning` event. |
| Q2 — Registration on TBD events | **A** — Blocked | `Event.Register*` rejects when `StartDate == null`. |
| Q3 — Featured / Nearby / Upcoming carousels | **A** — Excluded | Those query handlers add explicit `WHERE StartDate.HasValue`. |
| Q4 — Email when dates added | **A** — No | Silent state change Planning → Draft (or stays Published if already published). |

---

## Phased plan

### Phase 1 — Domain + DB foundation (TDD)
**Status:** ✅ COMPLETE (2026-05-08)

**Scope:**
- Add `EventStatus.Planning = 8`.
- Make `Event.StartDate` and `Event.EndDate` `DateTime?`.
- New `Event.SetDates(DateTime, DateTime)` instance method (transitions Planning → Draft when in Planning; idempotent in any other status).
- `Event.Create(...)` accepts `DateTime?` for dates; both-null → `Status = Planning`, both-set → existing Draft behaviour, mixed → `Result.Failure`.
- Null-safe guards in `Register`, `RegisterAnonymous`, `RegisterWithAttendees`, `Complete`, `ActivateEvent`, `HasSchedulingConflict`.
- `EventConfiguration.cs` drops `IsRequired()` for the two date columns.
- Migration `Phase8YA1_AllowNullEventDates` generated via `dotnet ef migrations add` (never hand-created — MEMORY.md scar).

**Tests FIRST (Red → Green):**
- `Event.Create_WithBothDatesNull_StartsInPlanning`
- `Event.Create_WithBothDates_StartsInDraft`
- `Event.Create_WithMixedDates_Fails` (one null, one set)
- `Event.SetDates_FromPlanning_TransitionsToDraft`
- `Event.SetDates_EndBeforeStart_Fails`
- `Event.SetDates_StartInPast_Fails`
- `Event.Register_OnPlanningEvent_Fails`
- `Event.Register_OnPublishedTbdEvent_Fails` (Q1=A: TBD can be published, but registration still blocked Q2=A)
- `Event.HasSchedulingConflict_OnTbdEvent_ReturnsFailure`
- `Event.ActivateEvent_OnTbdEvent_Fails`
- `Event.Complete_OnTbdEvent_Idempotent` (no-op since EndDate is null)

**Acceptance:** `dotnet test` green. Migration applied locally and rollback (`Down()`) re-adds NOT NULL cleanly.

**Rollback:** Migration `Down()` re-adds NOT NULL; safe until Phase 3 ships UI (no `Planning` rows yet).

**Out of Phase 1:** Application command/DTO changes, frontend, jobs. Phase 1 only adjusts immediate compile fallout in callers via defensive `.Value` with explicit `// Phase 8YA-2` TODO comments.

---

### Phase 2 — Application + DTO + email pipeline
**Status:** ⚪ NOT STARTED

**Scope:**
- `CreateEventCommand.StartDate` / `EndDate` → `DateTime?`.
- `UpdateEventCommand.StartDate` / `EndDate` → `DateTime?`.
- `EventDto` → `DateTime?`.
- Centralise null-handling in `EmailDateTimeHelper.FormatEventDate(DateTime?, ...)` → `"Date TBD"`; `FormatEventTime(DateTime?, ...)` → `"Time TBD"`.
- Update `EventExtensions.GetDisplayLabel` early-return: `if (StartDate == null) return "Date TBD";`.
- `EventStatusUpdateJob`: filter `WHERE e.StartDate.HasValue && e.EndDate.HasValue` defensively. Q1=A means Published-TBD events exist, jobs must skip them until dates are set.
- `EventReminderJob`: same filter.
- `EventNotificationEmailJob`: same filter.
- `EventCancellationEmailJob`: pass null through and let helper render "Date TBD".
- `GetEventIcsQueryHandler`: return 422 when dates are TBD.
- WhatsApp handlers: skip TBD events (Twilio approved templates require date params).
- New `CreateEventCommandValidator` rule: when both dates present, end > start; when one set and one null, fail.

**Tests FIRST:**
- `EmailDateTimeHelper.FormatEventDate(null) == "Date TBD"`
- `EmailDateTimeHelper.FormatEventTime(null) == "Time TBD"`
- `GetDisplayLabel_TbdEvent_ReturnsDateTbd`
- `EventStatusUpdateJob_TbdEvent_NotTransitioned` (integration)
- `GetEventIcsQuery_TbdEvent_Returns422`
- `EventReminderJob_TbdEvent_Skipped`
- WhatsApp handler unit test: `TbdEvent_Skipped_LogsReason`
- `CreateEventCommandValidator_OneDateNullOtherSet_Fails`

**Acceptance:** All email contract tests green; no template surfaces null/empty date.

---

### Phase 3 — Frontend (zod, types, forms, display)
**Status:** ⚪ NOT STARTED

**Scope:**
- `events.types.ts`: `startDate?: string | null`, `endDate?: string | null`.
- `event.schemas.ts`: optional dates; future-date refine only when present; `end > start` refine only when both present.
- New `datesUnknown` boolean on `EventCreationForm` and `EventEditForm`. Toggle hides date inputs and submits null dates.
- Detail/listing/payment-result pages render "Date TBD" when null. Add "Date TBD" badge component.
- Manage page banner: "Add dates to lock in registration" when event is in Planning.

**Tests FIRST (Vitest/RTL):**
- `EventCreationForm_TbdToggle_HidesDateInputs`
- `EventCreationForm_TbdToggle_SubmitsNullDates`
- `EventDetailPage_TbdEvent_RendersDateTbd`
- `EventListingCard_TbdEvent_RendersDateTbd`
- zod: `createSchema_DatesUnknownTrue_DatesOptional`
- zod: `createSchema_OnlyOneDateSet_Fails`

**Acceptance:** All FE tests green; manual click-through verifies create-with-TBD and add-dates-later flows.

---

### Phase 4 — Listing/sort/filter polish
**Status:** ⚪ NOT STARTED

**Scope:**
- TBD events sort to bottom of date-ordered lists everywhere: `OrderBy(e => e.StartDate.HasValue).ThenBy(e => e.StartDate)`.
- Featured / Nearby / Upcoming queries: explicit `WHERE e.StartDate != null` (Q3=A).
- Date-range filters naturally exclude TBD events (no UI work needed).
- Search SQL (`EventRepository.cs` raw SQL sites) — TBD events fall out of date-range; verify and document.

**Tests:** `Search_TbdEvent_AppearsAfterDatedEvents`; `FeaturedEvents_TbdEvent_Excluded`; `NearbyEvents_TbdEvent_Excluded`.

---

### Phase 5 — Operator UAT + smoke matrix
**Status:** ⚪ NOT STARTED

**Smoke matrix (12 cells, all asserted on staging — per MEMORY.md cross-surface matrix-smoke rule):**

| # | Path | Expected |
|---|---|---|
| 1 | Create with dates | 201, status=Draft |
| 2 | Create TBD | 201, status=Planning |
| 3 | Edit TBD → set dates | 200, status auto-Draft |
| 4 | Publish TBD | 200, status=Published (Q1=A) |
| 5 | Register on TBD | 400 "no confirmed dates" (Q2=A) |
| 6 | Listing card shows "Date TBD" | GET `/events` includes TBD with badge |
| 7 | Featured carousel excludes TBD | GET `/events/featured` skips TBD (Q3=A) |
| 8 | Detail page renders | "Date TBD" rendered, no JS error |
| 9 | Reminder job skips TBD | log: "skipped TBD event {id}" |
| 10 | Status job skips TBD | TBD-Published event not transitioned to Active |
| 11 | ICS export 422 on TBD | GET `/events/{id}/ics` → 422 |
| 12 | Add dates → register → email | confirmation email shows real dates |

**Operator UAT (per MEMORY.md operator-UAT gate before "Shipped"):**
- Operator on manage page of Planning event → sees "Add dates to enable registration" banner.
- Operator on `/events` → sees TBD card with "Date TBD" badge.
- Operator clicks TBD card → detail page renders cleanly.
- Operator attempts register → blocked with clear message.
- Operator adds dates via Edit → status flips to Draft (or stays Published if was Published) → registration allowed.

---

## Non-goals (first cut)

- Recurring events with TBD dates.
- Time-zone derivation while dates are null.
- ICS export for TBD events (explicit 422).
- Reminder/status-transition jobs for TBD events (explicit skip).
- WhatsApp confirmations for TBD events (skip).
- Admin force-publish-without-dates (already covered: Q1=A allows it via the standard publish flow).
- Email confirmation when dates are added (Q4=A).

---

## Files in scope (checklist)

### Phase 1 ✅
- [x] `src/LankaConnect.Domain/Events/Enums/EventStatus.cs` — added `Planning = 8`
- [x] `src/LankaConnect.Domain/Events/Event.cs` — `DateTime?` props, `SetDates(...)`, null-safe `Register*` / `Complete` / `ActivateEvent` / `HasSchedulingConflict`, `Publish` allows Planning (Q1=A)
- [x] `src/LankaConnect.Infrastructure/Data/Configurations/EventConfiguration.cs` — dropped `IsRequired()`
- [x] new migration `src/LankaConnect.Infrastructure/Data/Migrations/20260508153410_Phase8YA1_AllowNullEventDates.cs` — drops NOT NULL on both columns
- [x] `tests/LankaConnect.Domain.Tests/Events/Event_TbdDates_Tests.cs` — 13 tests, all pass
- [x] `src/LankaConnect.Shared/Email/Helpers/EmailDateTimeHelper.cs` — added `DateTime?` overloads returning "Date TBD" / "Time TBD"
- [x] `src/LankaConnect.Application/Common/Helpers/EmailDateTimeHelper.cs` — wrapper overloads
- [x] `src/LankaConnect.Application/Events/Common/EventExtensions.cs` — `GetDisplayLabel` early-returns "Date TBD"
- [x] ~30 callers patched with defensive `.GetValueOrDefault()` + `// Phase 8YA-2 TODO` comments (Application + Infrastructure layers)

### Phase 2 (later)
- [ ] `src/LankaConnect.Application/Events/Commands/CreateEvent/CreateEventCommand.cs`
- [ ] `src/LankaConnect.Application/Events/Commands/CreateEvent/CreateEventCommandValidator.cs`
- [ ] `src/LankaConnect.Application/Events/Commands/CreateEvent/CreateEventCommandHandler.cs`
- [ ] `src/LankaConnect.Application/Events/Commands/UpdateEvent/UpdateEventCommand.cs`
- [ ] `src/LankaConnect.Application/Events/Common/EventDto.cs`
- [ ] `src/LankaConnect.Application/Events/Common/EventExtensions.cs`
- [ ] `src/LankaConnect.Application/Common/Mappings/EventMappingProfile.cs`
- [ ] `src/LankaConnect.Application/Events/BackgroundJobs/EventStatusUpdateJob.cs`
- [ ] `src/LankaConnect.Application/Events/BackgroundJobs/EventReminderJob.cs`
- [ ] `src/LankaConnect.Application/Events/BackgroundJobs/EventNotificationEmailJob.cs`
- [ ] `src/LankaConnect.Application/Events/BackgroundJobs/EventCancellationEmailJob.cs`
- [ ] `src/LankaConnect.Application/Events/Queries/GetEventIcs/GetEventIcsQueryHandler.cs`
- [ ] `src/LankaConnect.Shared/Email/EmailDateTimeHelper.cs`

### Phase 3 (later)
- [ ] `web/src/infrastructure/api/types/events.types.ts`
- [ ] `web/src/presentation/lib/validators/event.schemas.ts`
- [ ] `web/src/presentation/components/features/events/EventCreationForm.tsx`
- [ ] `web/src/presentation/components/features/events/EventEditForm.tsx`
- [ ] `web/src/presentation/components/features/events/EventDetailsTab.tsx`
- [ ] `web/src/presentation/utils/eventMapper.ts`
- [ ] `web/src/app/events/page.tsx`
- [ ] `web/src/app/events/[id]/page.tsx`

### Phase 4 (later)
- [ ] `src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs` (sort tiebreakers)
- [ ] `src/LankaConnect.Application/Events/Queries/GetFeaturedEvents/GetFeaturedEventsQueryHandler.cs`
- [ ] `src/LankaConnect.Application/Events/Queries/GetNearbyEvents/GetNearbyEventsQueryHandler.cs`
- [ ] `src/LankaConnect.Application/Events/Queries/GetUpcomingEventsForUser/GetUpcomingEventsForUserQueryHandler.cs`

---

## Status log

- 2026-05-08 — Phase 8YA kicked off. Architect RCA + plan locked. Q1–Q4 answered (A, A, A, A). Phase 1 starting.
- 2026-05-08 — Phase 1 complete. 13 new domain tests pass, zero regressions in Domain/Application unit suites. Migration `20260508153410_Phase8YA1_AllowNullEventDates` generated and scoped. Build clean across the solution. Pre-existing test failures (FormResponseTests + DonationConfigurationTests in Domain.Tests; 5 timezone-related in Shared.Tests) confirmed unrelated to Phase 1 — present before changes. Phase 2 next: Application command/DTO, validator update, jobs filter TBD, ICS 422.
