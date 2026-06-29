# Phase 8YB.5 — TBD-Publish Recovery Slice

**Date:** 2026-05-09
**Status:** ✅ SHIPPED + STAGING-VERIFIED 2026-05-09 — Niroshana's screenshot confirmed search "Sam" with default Active+Upcoming filters returns `541876b8` (TBD ExternalPaid event), validating the headline rule overturn end-to-end.
**Architect-approved.** Single slice; 8 code edits across 7 files; 22-cell smoke matrix; 8-cell operator UAT.

**Commit:** `e9e8ce31` (`develop`) — 11 files, +521 / -26.
**Deploys:** BE run `25610497852` GREEN. UI run `25610497854` GREEN.
**API smoke:** **17 / 17 PASS** on staging via `scripts/phase8YB5_smoke.py` (3 setup + 3 headline publish + 3 listing/filter + 2 detail/iCal + 2 search/featured + 1 SetDates + 1 cancel + 1 unpublish + 1 RSVP-blocked).
**Tests:** 4 new domain tests + 2 new application tests (TDD red→green). Domain 703/705 + Application 2646/2652 PASS (2 unrelated pre-existing fails). Frontend typecheck + Next.js build clean.

---

## Product rule overturn (user-locked 2026-05-09)

> "TBD events should be able to publish, otherwise no point of creating them. We publish and show the public that the event is coming soon."

Applies to **all payment modes** (Free / OnPlatformPaid / ExternalPaid).

## Decisions (user-locked 2026-05-09 = architect recommendations)

| # | Decision | Choice |
|---|---|---|
| D1 | Publish button on Planning events | **A** — enable directly, identical UX to Draft → Published |
| D2 | TS `EventStatus` enum format | **B** — convert to string-valued to match `JsonStringEnumConverter` |
| D3 | "Notify me when dates set" intent capture | **A** — out of scope; passive "Coming soon" copy only |
| D4 | Planning → UnderReview admin path | **A** — bypass review (matches Draft today) |
| D5 | "Coming Soon" pill on event card | **A** — add small orange pill |
| D5b | Date-range filter behaviour with TBD events | **A** — TBD in "Upcoming" + "All"; excluded from "This Week / Next Week / Next Month" |
| D6 | `Postpone()` on TBD-Published event | **A** — tighten domain to require `StartDate.HasValue` |
| D7 | TBD-publish across payment modes | **A** — allow uniformly. Domain `Register()` blocks RSVPs while StartDate null |

## Discipline (carry-forward + new)

1. Master TODO before code (this file).
2. Both deploy workflows GREEN before "DEPLOYED".
3. Per-file `git add` only — never whole-file staging.
4. API smoke matrix BEFORE flipping status (`feedback_cross_surface_matrix_smoke.md`).
5. Operator UAT gate BEFORE "Shipped" (`feedback_operator_uat_gate.md`).
6. No author-laundering — never `git add` parallel author's untracked files.
7. Honest end-of-turn — plain English, no commit-hash dumps.
8. **NEW: TDD — write failing tests first, implement to make them pass.**
9. **NEW: HS-8YB.5 hard-stop — if implementation reveals >5 additional structural assumption sites, stop and re-architect.**

---

## Sites + classification

**Primary: UI issue.** **Secondary: 1 backend filter bug + spec gap.** NOT auth / DB / feature missing.

| # | File | Layer | Today | Fix |
|---|---|---|---|---|
| 1 | `web/src/app/events/[id]/manage/page.tsx:241,379` | UI | Publish gated on `isDraft` only | Gate on `isDraft \|\| isPlanning` |
| 2 | same `:84-93` | UI | `statusLabels[Planning]` undefined → blank badge | Add `[EventStatus.Planning]: 'Planning'` |
| 3 | same `:256-257` | UI | `canCancel` / `canDelete` exclude Planning | Extend to Planning |
| 4 | `web/src/infrastructure/api/types/events.types.ts:13-22` | TS types | Numeric enum stops at `UnderReview = 7`; `Planning` missing → test passes by accident | Convert to string-valued + add `Planning` |
| 5 | `src/LankaConnect.Application/Events/Queries/GetEvents/GetEventsQueryHandler.cs:710-718` | Backend | `StartDateFrom` filter silently drops TBD | Pass through TBD when only `StartDateFrom` set; exclude when `StartDateTo` also set (D5b) |
| 6 | `Event.cs Unpublish()` (E16) | Domain | TBD-Published → Unpublish creates `Draft × null` impossible state | Revert to Planning when StartDate null |
| 7 | `web/src/app/events/[id]/page.tsx` ~773,827 | UI | No TBD banner; no Register CTA replacement; no Add-to-Calendar gate | Add banner; replace Register CTA; hide iCal button |
| 8 | `EventPublishedEvent` handlers (Phase 7B WhatsApp + email) | Backend | Risk of formatting `1/1/0001` for null StartDate | Guard date formatting; substitute "Date TBD" |
| 9 | `Event.cs Postpone()` (D6) | Domain | Allows postponing TBD event | Tighten to require `StartDate.HasValue` |
| 10 | `web/src/app/events/page.tsx` (EventCard) (D5) | UI | Date TBD text only | Add "Coming Soon" pill |

---

## Pre-flight diagnostics (PF.1–PF.5)

- [ ] PF.1 — Confirm `Event.Publish()` accepts `Planning` (architect found yes; verify line 277-295)
- [ ] PF.2 — Confirm `Event.Unpublish()` (line 305-318) currently sets Draft regardless of dates → needs E16 fix
- [ ] PF.3 — Audit ALL `EventPublishedEvent` handlers (Phase 7B WhatsApp + Phase 6A.99 email) for null-StartDate handling
- [ ] PF.4 — Audit `GetEventsQueryHandler.cs` filter logic at line 710-718
- [ ] PF.5 — Audit TS `EventStatus` consumers for numeric-arithmetic / reverse-lookup usage

---

## TDD plan (tests written FIRST)

### Domain (xUnit)

- [ ] T1 — `Event_PublishFromPlanning_Succeeds()` — regression guard
- [ ] T2 — `Event_UnpublishFromPlanning_Stays_Planning()` — TBD-Published → Unpublish reverts to Planning, not Draft (E16)
- [ ] T3 — `Event_UnpublishFromDraft_With_Dates_Reverts_To_Draft()` — regression guard for non-TBD path
- [ ] T4 — `Event_UnpublishFromPublished_With_Dates_Reverts_To_Draft()` — regression guard for normal path
- [ ] T5 — `Event_Postpone_Fails_When_StartDate_Null()` — D6 tighten
- [ ] T6 — `Event_Postpone_Succeeds_When_StartDate_Set()` — regression guard

### Application

- [ ] T7 — `GetEventsQueryHandler_With_StartDateFrom_Only_Includes_Tbd_Published_Events()` — fix #5
- [ ] T8 — `GetEventsQueryHandler_With_StartDateFrom_AND_StartDateTo_Excludes_Tbd_Events()` — D5b=A regression guard
- [ ] T9 — `PublishEventCommandHandler_Planning_Path_Raises_EventPublishedEvent()` — domain event fires for TBD path
- [ ] T10 — `EventPublishedEvent` handlers handle null StartDate (per E18 audit findings)

### Frontend (vitest, optional but recommended)

- [ ] T11 — `manage/page.test.tsx` — Publish button visible when status = Planning
- [ ] T12 — `eventMapper.tbd-dates.test.ts` strengthened — assert `EventStatus.Planning === 'Planning'`

---

## Implementation order

1. **PF audit** — confirm sites, surface E18 latent bugs.
2. **TDD red** — write failing tests T1-T9.
3. **Backend fix #5** — GetEventsQueryHandler filter.
4. **Domain fix E16** — Unpublish revert to Planning when null.
5. **Domain fix D6** — Postpone requires HasValue.
6. **Lifecycle handler audit** — guard against null StartDate (E18/E19).
7. **TDD green** — verify all tests pass.
8. **TS enum conversion (D2 = B)** — string-valued + add Planning.
9. **Manage page** — Publish gate + status label + canCancel/canDelete.
10. **EventCard** — Coming Soon pill (D5 = A).
11. **Public detail** — TBD banner + Register CTA replacement + hide iCal button.
12. **Build / test / commit / push.**
13. **Both deploys GREEN.**
14. **22-cell API smoke.**
15. **8-cell operator UAT (user-driven).**
16. **Doc updates + commit.**

---

## API smoke matrix (22 cells)

Build BEFORE coding done. Cell-by-cell assertions in `scripts/phase8YB5_smoke.py`.

| Cell | Surface | Action | Expected |
|---|---|---|---|
| C1 | manage UI | Open manage page for Planning event | Publish button visible; status badge "Planning" |
| C2 | manage UI | Free Planning event | Publish button visible |
| C3 | manage UI | OnPlatformPaid Planning event | Publish button visible |
| C4 | API | POST `/events/{id}/publish` on Planning | 200; status="Published" |
| C5 | API | After C4, GET `/api/events?statusFilter=Active` | TBD event present, startDate null |
| C6 | API | After C4, GET `?statusFilter=Active&startDateFrom={now}` | TBD event STILL present (validates fix #5) |
| C7 | API | After C4, GET `?startDateFrom={mon}&startDateTo={sun}` (week window) | TBD event NOT present (D5b=A) |
| C8 | API | After C4, GET `/api/events/{id}` anon | 200, startDate null, status Published |
| C9 | UI | After C4, public detail page Register CTA | "Coming soon" disabled |
| C10 | UI | After C4, public detail page Add-to-Calendar | Hidden |
| C11 | API | After C4, GET `/api/events/{id}/ics` | 422 |
| C12 | API | After C4, search by title | TBD event matches |
| C13 | API | After C4, GET `/api/events/featured` | TBD event NOT present (Q3=A) |
| C14 | API | After C4, GET `/api/events/nearby?lat=...` | TBD event NOT present |
| C15 | observability | Reminder cron run logs | TBD events skipped explicitly or implicitly |
| C16 | observability | EventStatusUpdateJob run logs | TBD events filtered out |
| C17 | API | After C4, PATCH dates → set startDate/endDate | Status STAYS Published; listing card swaps to dated |
| C18 | API | After C4, POST `/events/{id}/cancel` | 200; status="Cancelled"; cancellation email sent |
| C19 | API | After C4, POST `/events/{id}/unpublish` | Status reverts to **Planning** (validates E16) |
| C20 | API | After C4, GET `/events/{id}` to inspect edit-form payload | TBD-relevant fields present |
| C21 | observability | EventPublishedEvent handler logs after C4 | No `1/1/0001` strings; either skip or substitute |
| C22 | API | After C4, anon POST register | 400 "Cannot register without confirmed dates" |

---

## Operator browser UAT (U.1–U.8) — user-driven

Cannot self-attest. Hand-off after API smoke green.

- [ ] U1 — Niroshana's existing event `541876b8` — Publish button now visible; status badge "Planning"
- [ ] U2 — Click Publish → status = "Published"; page renders without errors
- [ ] U3 — Anonymous incognito tab visits `/events` → event in list, "Date TBD" + "Coming Soon" pill
- [ ] U4 — Anonymous incognito opens the event → renders OK; TBD banner shown; no Add-to-Calendar; Register CTA replaced
- [ ] U5 — Operator goes back to manage, edits, sets future dates → save → status STAYS Published; listing card now shows real dates
- [ ] U6 — Operator unpublishes the now-dated event → reverts to Draft (regression guard)
- [ ] U7 — Operator unpublishes a TBD-Published event → reverts to **Planning** (validates E16)
- [ ] U8 — Niroshana confirms `541876b8` Publish-able, public-listing-visible, registration-blocked-with-clear-copy

---

## Status reporting phases

| Phase | Reportable State |
|---|---|
| Implementation done locally | `LOCAL-VERIFIED` |
| `deploy-staging.yml` GREEN | `BACKEND-DEPLOYED` |
| `deploy-ui-staging.yml` GREEN | `UI-DEPLOYED` |
| API smoke 22/22 PASS | `API-VERIFIED` |
| Operator browser UAT 8/8 PASS | `STAGING-VERIFIED` |
| Doc updates + commit pushed | `SHIPPED` |

**Never `SHIPPED` on BE-only evidence. Never claim before user signs off on UAT cells.**
