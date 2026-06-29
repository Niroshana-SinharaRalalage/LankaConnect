# Phase 6A.152 — `/events` Upcoming / Completed split: date-based, not status-based

**Date opened:** 2026-05-24
**Branch:** `feat/phase-6a-152-events-date-based-upcoming-completed` off `main`
**Status:** 🔧 In progress — TDD, awaiting first commit

## Goal in one sentence

Make the public `/events` page show **every event that already happened** in the "Completed Events" section, by deriving the bucket from `StartDate` instead of `Status` — undoing the status-based split shipped in 6A.149 which assumed events would auto-flip to `Status=Completed` (they don't, because the hourly Hangfire job only handles `Published → Active → Completed` and silently strands `Published` events that miss the `Active` hop).

## Decisions locked-in by product owner (2026-05-24)

| # | Decision | Locked |
|---|---|---|
| D1 | **Bucket by `StartDate`, not by `Status`.** Upcoming = `StartDate >= now OR StartDate IS NULL` (TBD). Completed = `StartDate < now`. | ✅ |
| D2 | **Exclude `Cancelled` from both buckets.** Cancelled events do not appear on the public listing. | ✅ |
| D3 | **`Draft` and `UnderReview` continue to be excluded** (existing public-visibility rule, unchanged from today). | ✅ |
| D4 | **Postponed events follow the date rule like everything else.** Future date → Upcoming. Past date → Completed. No special handling. | ✅ |
| D5 | **No background-job change, no migration, no data backfill.** Pure query / display refactor. | ✅ |
| D6 | **Completed section heading always renders** (with an empty-state card if zero), so users can discover the feature. | ✅ |

## Live production data (proves the problem, 2026-05-24)

```
GET /api/events?statusFilter=1 (Active)   → 6 events, all Status=Published
                                            4 future-dated → currently visible
                                            2 past-dated   → stranded, invisible everywhere
GET /api/events?statusFilter=2 (Inactive) → []  (zero Completed/Archived/Postponed)
```

That's why production shows only 3-4 cards with no Completed section. The 2 stranded past-Published events (`Sri Lankan New Year Celebration 2026` 2026-05-02; `NorthEast Ohio Community Drive` 2026-05-16) become visible immediately after this fix lands.

## Scope of changes

### Backend (`LankaConnect.Application`)

**File:** `src/LankaConnect.Application/Events/Queries/GetEvents/GetEventsQueryHandler.cs`

Replace status-array semantics for `EventStatusFilter.Active` / `EventStatusFilter.Inactive` with date-based semantics. New behaviour:

| `EventStatusFilter` value | New filter applied |
|---|---|
| `Active` (1) | `Status != Cancelled AND Status != Draft AND Status != UnderReview AND (StartDate >= now OR StartDate IS NULL)` |
| `Inactive` (2) | `Status != Cancelled AND Status != Draft AND Status != UnderReview AND StartDate.HasValue AND StartDate < now` |
| `Cancelled` (3) | unchanged (`Status = Cancelled`) |
| `Unpublished` (4) | unchanged (Draft + UnderReview, organizers only) |
| `All` (0) | unchanged |

`startDateFrom` / `startDateTo` query parameters keep their existing meaning — they layer ON TOP of the bucket filter. (Upcoming page sends `startDateFrom=now` already; that becomes redundant but harmless. Completed page will additionally send `startDateTo=now`.)

### Frontend (`web`)

**File:** `web/src/app/events/page.tsx`

1. Drop the client-side `e.status === EventStatus.Completed` filter (lines ~131-137). The backend now returns the correct set.
2. Drop the `hasCompletedEvents &&` gate on the Completed section heading (line ~444). Always render the heading; show an empty-state card inside when the list is empty.
3. (Optional, defensive) Add `startDateTo: new Date().toISOString()` to the Completed filter so client and backend agree on the cutoff.

### Tests

**Backend:** `tests/LankaConnect.Application.Tests/Events/Queries/GetEvents/GetEventsQueryHandlerTests.cs`
Add a new region with 8 cases:
- Active+future → present in Active bucket; absent from Inactive
- Active+past → absent from Active; present in Inactive
- Published+future → present in Active; absent from Inactive
- Published+past → absent from Active; present in Inactive ← the stranded case, regression-guard for this fix
- Postponed+future → present in Active; absent from Inactive
- Postponed+past → absent from Active; present in Inactive
- Cancelled+past → absent from both
- TBD (null StartDate) → present in Active; absent from Inactive

**Frontend:** existing `web/src/app/events/__tests__/events-page-6a-149.test.tsx`
Update tests that asserted the old client-side `status === Completed` filter; add a new test that the Completed heading renders with an empty-state when the API returns `[]`.

## Risks (all low)

- **Active bucket may grow** if there are Published events with past StartDate that the existing in-memory `startDateFrom` filter had been masking — they'll now correctly move to Completed. Net: visible events go up, no events disappear.
- **Postponed events flip between buckets** as their date changes. This is what the user asked for; intentional.
- **No data mutation**, no migration, no Hangfire interaction.

## Phase reservation (4-source check, 2026-05-24)

- Master index `PHASE_6A_MASTER_INDEX.md`: highest row is 6A.151 (Sponsor Edit); **6A.152 absent** ✅
- `git log --oneline --all -500 | grep '6a.152'`: **no matches** ✅
- `git branch -a | grep '6a-152'`: **no matches** ✅
- `docs/MASTER_TODO_PHASE_6A_152*.md`: **this file is the first** ✅

## Deploy plan

1. Backend tests RED → GREEN; full local backend test suite passes
2. Frontend tests RED → GREEN; full local frontend test suite passes
3. Commit + push branch
4. Trigger `deploy-staging.yml` (backend) **and** `deploy-ui-staging.yml` (UI) in the same chain
5. Curl staging API: confirm `statusFilter=2` now returns past-dated events (including stranded Published ones)
6. Browser smoke `/events` on staging UI: confirm Completed section renders with cards
7. Update `PROGRESS_TRACKER.md` + `STREAMLINED_ACTION_PLAN.md`
8. Open PR to main
