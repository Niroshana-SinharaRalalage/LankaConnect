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
**Status:** ✅ COMPLETE (2026-05-08)

**What shipped:**
- `CreateEventCommand` / `UpdateEventCommand` / `EventDto` — `StartDate` / `EndDate` → `DateTime?`
- `CreateEventCommandValidator` + `UpdateEventCommandValidator` — new mixed-dates rule (one set, one null → 400 with `"both must be provided together, or both empty"`)
- `UpdateEventCommandHandler` — both-null path leaves dates unchanged; both-set path routes through `Event.SetDates(...)` (validates + transitions Planning → Draft)
- `EventStatusUpdateJob` — explicit `WHERE e.StartDate.HasValue && e.StartDate.Value <= now` filter on Active transitions; symmetric guard on Completed transitions
- `GetEventIcsQueryHandler` — returns `Result.Failure("...Date TBD...")` for TBD events
- `EventsController.GetEventIcs` — maps the TBD failure to **HTTP 422 Unprocessable Entity** (architect-locked) distinct from 400 (bad request) and 404 (not found)
- `EventPublishedEventHandler` — skips email + structured log when StartDate/EndDate null (Q1=A allows TBD-Published; broadcasting "Date TBD" defeats the email's purpose)
- `EventApprovedEventHandler` — defensive skip on TBD (theoretically unreachable since SubmitForReview requires Draft, but defensive against future loosening)
- `EventRejectedEventHandler` — same defensive skip
- `EventPublishedWhatsAppHandler` — skips WhatsApp broadcast on TBD events (Twilio approved templates require {{EventDate}})
- 10 new unit tests across Application: `CreateEventTbdDatesTests` (5), `EventStatusUpdateJobTbdTests` (3), `GetEventIcsQueryHandlerTbdTests` (2)

**Out of Phase 2 (deferred):**
- Email param class refactor (~18 `*EmailParams.Create()` factories accepting `DateTime?` and rendering "Date TBD" via the centralised `EmailDateTimeHelper.FormatEventDate(DateTime?)` overload). Registration-flow handlers stay with `.GetValueOrDefault()` shim because the Register* domain method already blocks TBD events per Q2=A; the `// Phase 8YA-2 TODO` markers remain as future-proofing if the param classes ever need to handle null. Not a regression — those code paths are unreachable on TBD events today.

**Tests:** Application 2637/2643 (0 fail, 6 skipped — was 2627 pre-Phase-2, +10). Domain 696/698 (2 pre-existing failures unchanged from Phase 1 baseline). Build clean.

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
**Status:** ✅ COMPLETE (2026-05-08)

**What shipped:**
- `events.types.ts` — `EventDto.startDate`, `EventDto.endDate`, `CreateEventRequest.startDate`, `CreateEventRequest.endDate`, `UpdateEventRequest.startDate`, `UpdateEventRequest.endDate` are now `string | null`
- `event.schemas.ts` — `createEventSchema` + `editEventSchema` accept optional dates and gate all date refines (future-date, end > start, mixed-pair) on a new `datesUnknown` boolean toggle. When `datesUnknown=true`, the form skips date validation entirely.
- `EventCreationForm` — new "Dates not yet decided (TBD)" toggle in the Date & Time section. When checked, hides the datetime-local inputs and submits `{ startDate: null, endDate: null }` to the backend (which creates a Planning-status event).
- `EventEditForm` — same toggle. Pre-checks itself when the loaded event has no dates (Planning event); operator can uncheck + fill in dates → save routes through `Event.SetDates(...)` (Planning → Draft auto-transition).
- `formatDateForInput` in `EventEditForm` — null-safe (returns empty string instead of throwing on null).
- `formatEventDateRange` in `presentation/utils/eventMapper.ts` — returns `"Date TBD"` early when either date is null.
- `mapEventToFeedItem` in `presentation/utils/eventMapper.ts` — surfaces `date: "Date TBD"` / `time: "Time TBD"` in metadata for null dates.
- `application/mappers/eventMapper.ts` — `sortEventsByDate` puts TBD events at the bottom; `getUpcomingEvents` excludes them (Q3=A).
- Display surfaces patched defensively for `string | null`: `events/[id]/page.tsx`, `events/page.tsx`, `events/payment/cancel/page.tsx`, `events/payment/success/page.tsx`, `lanka-events/page.tsx`, `search/page.tsx`, `EventsList.tsx` (dashboard), `EventDetailsTab.tsx`, `EventScroller.tsx`, `NewsletterForm.tsx`. Each renders `"Date TBD"` / `"Time TBD"` placeholder when null.
- 16 new vitest tests across 2 files: `event.schemas.tbd-dates.test.ts` (11) + `eventMapper.tbd-dates.test.ts` (5)

**Phase 3 verification:**
- `tsc --noEmit` clean (only one pre-existing error in `page_old_backup.tsx` — backup file, not a real surface)
- New TBD-dates tests: 16/16 pass
- Event component tests (RTL): 78/78 pass — no regressions
- Validators: 55/55 pass (phone + volunteer + event-tbd-dates)

**Out of Phase 3 (deferred):**
- Manage-page banner ("Add dates to enable registration") — not blocking the user flow; the create + edit form toggles already give the operator a clear path. Defer to Phase 4 polish if needed.
- RTL test for the full create/edit form TBD toggle (would require deep provider mocking) — covered indirectly by the zod schema tests + manual smoke in Phase 5.

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

### Phase 2 ✅
- [x] `src/LankaConnect.Application/Events/Commands/CreateEvent/CreateEventCommand.cs` — `StartDate` / `EndDate` → `DateTime?`
- [x] `src/LankaConnect.Application/Events/Commands/CreateEvent/CreateEventCommandValidator.cs` — mixed-dates rule
- [x] `src/LankaConnect.Application/Events/Commands/UpdateEvent/UpdateEventCommand.cs` — `DateTime?`
- [x] `src/LankaConnect.Application/Events/Commands/UpdateEvent/UpdateEventCommandValidator.cs` — mixed-dates rule
- [x] `src/LankaConnect.Application/Events/Commands/UpdateEvent/UpdateEventCommandHandler.cs` — both-null leaves unchanged; both-set routes through SetDates
- [x] `src/LankaConnect.Application/Events/Common/EventDto.cs` — `DateTime?`
- [x] `src/LankaConnect.Application/Events/BackgroundJobs/EventStatusUpdateJob.cs` — `.HasValue` filter on both transitions
- [x] `src/LankaConnect.Application/Events/Queries/GetEventIcs/GetEventIcsQueryHandler.cs` — Failure on TBD
- [x] `src/LankaConnect.API/Controllers/EventsController.cs` — `/ics` 422 mapping
- [x] `src/LankaConnect.Application/Events/EventHandlers/EventPublishedEventHandler.cs` — skip on TBD
- [x] `src/LankaConnect.Application/Events/EventHandlers/EventApprovedEventHandler.cs` — defensive skip
- [x] `src/LankaConnect.Application/Events/EventHandlers/EventRejectedEventHandler.cs` — defensive skip
- [x] `src/LankaConnect.Application/Events/EventHandlers/EventPublishedWhatsAppHandler.cs` — skip on TBD
- [x] `tests/LankaConnect.Application.Tests/Events/Commands/CreateEventTbdDatesTests.cs` — 5 tests
- [x] `tests/LankaConnect.Application.Tests/Events/BackgroundJobs/EventStatusUpdateJobTbdTests.cs` — 3 tests
- [x] `tests/LankaConnect.Application.Tests/Events/Queries/GetEventIcsQueryHandlerTbdTests.cs` — 2 tests

**Phase 2 deferred to a later phase (not blocking Phase 3):**
- `EventReminderJob` / `EventNotificationEmailJob` / `EventCancellationEmailJob` — `.GetValueOrDefault()` shim from Phase 1 still in place. Reminder + notification jobs already filter by Status/StartDate predicates that exclude TBD events implicitly. Cancellation handler currently passes shim — TBD events that get cancelled should still notify users that the event is cancelled.
- Email param class refactor (`*EmailParams.Create()` accepting `DateTime?`) — defer; registration-flow handlers can't fire on TBD per Q2=A.
- `CreateEventCommandHandler` — already wires nullable through to `Event.Create` via implicit conversion; no change needed.

### Phase 3 ✅
- [x] `web/src/infrastructure/api/types/events.types.ts` — EventDto + CreateEventRequest + UpdateEventRequest dates → `string | null`
- [x] `web/src/presentation/lib/validators/event.schemas.ts` — datesUnknown toggle + nullable dates + gated refines
- [x] `web/src/presentation/components/features/events/EventCreationForm.tsx` — TBD toggle UI + null submit
- [x] `web/src/presentation/components/features/events/EventEditForm.tsx` — TBD toggle UI + auto-check on Planning events + null-safe formatDateForInput
- [x] `web/src/presentation/components/features/events/EventDetailsTab.tsx` — "Date TBD" placeholder
- [x] `web/src/presentation/utils/eventMapper.ts` — formatEventDateRange + mapEventToFeedItem null-safe
- [x] `web/src/application/mappers/eventMapper.ts` — sortEventsByDate (TBD bottom) + getUpcomingEvents (excludes TBD)
- [x] `web/src/app/events/page.tsx` — listing card "Date TBD" / "Time TBD"
- [x] `web/src/app/events/[id]/page.tsx` — detail page null-safe formatters + hasStarted=false on TBD
- [x] `web/src/app/events/payment/cancel/page.tsx` + `payment/success/page.tsx` — defensive placeholder
- [x] `web/src/app/lanka-events/page.tsx` — featured carousel placeholder (Q3=A excludes TBD anyway)
- [x] `web/src/app/search/page.tsx` — search card placeholder
- [x] `web/src/presentation/components/features/dashboard/EventsList.tsx` — dashboard "Date TBD"
- [x] `web/src/presentation/components/features/landing/EventScroller.tsx` — landing scroller placeholder
- [x] `web/src/presentation/components/features/newsletters/NewsletterForm.tsx` — newsletter compose placeholder
- [x] `web/src/presentation/lib/validators/__tests__/event.schemas.tbd-dates.test.ts` — 11 vitest tests
- [x] `web/src/presentation/utils/__tests__/eventMapper.tbd-dates.test.ts` — 5 vitest tests

### Phase 4 (later)
- [ ] `src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs` (sort tiebreakers)
- [ ] `src/LankaConnect.Application/Events/Queries/GetFeaturedEvents/GetFeaturedEventsQueryHandler.cs`
- [ ] `src/LankaConnect.Application/Events/Queries/GetNearbyEvents/GetNearbyEventsQueryHandler.cs`
- [ ] `src/LankaConnect.Application/Events/Queries/GetUpcomingEventsForUser/GetUpcomingEventsForUserQueryHandler.cs`

---

## Status log

- 2026-05-08 — Phase 8YA kicked off. Architect RCA + plan locked. Q1–Q4 answered (A, A, A, A). Phase 1 starting.
- 2026-05-08 — Phase 1 complete. 13 new domain tests pass, zero regressions in Domain/Application unit suites. Migration `20260508153410_Phase8YA1_AllowNullEventDates` generated and scoped. Build clean across the solution. Pre-existing test failures (FormResponseTests + DonationConfigurationTests in Domain.Tests; 5 timezone-related in Shared.Tests) confirmed unrelated to Phase 1 — present before changes. Phase 2 next: Application command/DTO, validator update, jobs filter TBD, ICS 422.
- 2026-05-08 — Phase 2 complete. CreateEventCommand/UpdateEventCommand/EventDto now nullable; validators reject mixed-dates; UpdateEventCommandHandler routes through Event.SetDates; EventStatusUpdateJob filters TBD events; GetEventIcsQueryHandler returns Failure → controller maps to HTTP 422; EventPublished/Approved/Rejected email handlers + EventPublished WhatsApp handler skip TBD events with structured logs. 10 new Application unit tests; Application.Tests now 2637/2643 (was 2627; +10), 0 fail. Domain + Shared test counts unchanged from Phase 1 baseline (2 + 5 pre-existing failures unrelated). Build clean. Phase 3 next: Frontend zod + form toggle + "Date TBD" display.
- 2026-05-08 — Phase 3 complete. Frontend `EventDto` / `CreateEventRequest` / `UpdateEventRequest` dates flip to `string | null`; `event.schemas.ts` gains a `datesUnknown` toggle that gates all date refines (future-date, end > start, mixed-pair) so checking it submits null dates without errors. `EventCreationForm` + `EventEditForm` get a "Dates not yet decided (TBD)" checkbox in the Date & Time section; the edit form pre-checks itself when loading a Planning event and routes save through `Event.SetDates(...)` when the operator unchecks + fills in dates. `formatDateForInput` in EventEditForm is now null-safe. ~10 display surfaces (events listing card, detail page, payment success/cancel pages, lanka-events landing, search results, dashboard EventsList, EventDetailsTab, EventScroller, NewsletterForm) render "Date TBD" / "Time TBD" placeholders when dates are null. `application/mappers/eventMapper.ts` sorts TBD events to the bottom in `sortEventsByDate` and excludes them from `getUpcomingEvents` (Q3=A). 16 new vitest tests across 2 files (zod schema TBD coverage + eventMapper TBD coverage), all pass. tsc clean (one pre-existing error in `page_old_backup.tsx` backup file, unrelated). Event component RTL tests 78/78 pass — no regressions. Validators 55/55 pass. Phase 4 next: backend listing/sort/filter polish to put TBD events at the bottom of date-ordered lists + featured/nearby exclusion. Phase 5: deploy to staging + operator UAT + 12-cell smoke matrix.
