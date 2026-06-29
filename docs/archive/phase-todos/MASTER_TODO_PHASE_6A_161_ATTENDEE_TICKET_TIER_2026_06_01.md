# Phase 6A.161 — Ticket Tier on Attendees tab + CSV/Excel exports

**Date opened:** 2026-06-01
**Branch:** `feat/phase-6a-161-attendee-ticket-tier` off `main`
**Status:** 📋 Master TODO ready — awaiting product-owner approval before code changes

## Goal in one sentence

Surface the **Ticket Tier** each attendee was registered under in the event-manage **Attendees** tab (collapsed-row summary + per-attendee detail on expand) and append a **"Ticket Tier(s)"** column to the CSV and Excel attendee exports — by wiring through tier data the domain already persists but the read path drops.

## Classification (product-owner question)

| Layer | Verdict |
|-------|---------|
| Database | ✅ Healthy — `ticket_tier_id` + `ticket_tier_name` already persisted in `registrations.attendees` JSONB |
| Auth | ✅ Not involved |
| UI | ⚠️ Symptom only |
| Backend API | ⚠️ Symptom only |
| **Feature-missing (incomplete read-path projection)** | ✅ **Root cause** |

## Root-cause analysis (architect-validated, 2026-06-01)

The denormalized tier name is **already stored** per attendee — no join to `ticket_tiers` is needed to display it ([AttendeeDetails.cs:21,27](../src/LankaConnect.Domain/Events/ValueObjects/AttendeeDetails.cs#L21); [RegistrationConfiguration.cs:139-145](../src/LankaConnect.Infrastructure/Data/Configurations/RegistrationConfiguration.cs#L139)). The data is dropped at three points:

1. **Projection drops it** — [GetEventAttendeesQueryHandler.cs:129-134](../src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs#L129) maps only `Name, AgeCategory, Gender`.
2. **DTOs have no tier field** — shared `AttendeeDetailsDto` ([RegistrationDetailsDto.cs:96-101](../src/LankaConnect.Application/Events/Common/RegistrationDetailsDto.cs#L96)) and `EventAttendeeDto` carry no tier.
3. **UI + export never render it** — FE `AttendeeDto` lacks `ticketTierName`; table renders no tier column; backend-driven CSV/Excel emit no tier column.

Tier is **per-attendee** (a registration may mix tiers), and **nullable** — null = single-tier event, free event, or legacy registration before migration `20260415203751_AddTicketTiers` (2026-04-15). Every surface must degrade null → `—`, never blank/throw.

## Decisions locked-in (architect + product owner, 2026-06-01)

| # | Decision | Locked |
|---|---|---|
| D1 | **Display model: summary + per-attendee detail.** Collapsed row shows registration-level summary (uniform → single name; mixed → distinct names joined e.g. `VIP, General`; none → `—`); expanded row shows each attendee's own tier. Mirrors existing `MainAttendeeName`/`AdditionalAttendees` idiom. | ✅ (PO) |
| D2 | **DTO shape: both levels.** Add nullable `TicketTierId` + `TicketTierName` to shared `AttendeeDetailsDto` (source of truth); add **computed** `TicketTierSummary` getter on `EventAttendeeDto` (no stored field → zero summary/detail drift). | ✅ |
| D3 | **Export model: single appended summary column.** Add one `Ticket Tier(s)` column populated from the same computed summary, **appended at the end** of the existing column block in BOTH CSV and Excel. No restructure to per-attendee rows (would break TOTAL row + saved templates + desync from table). | ✅ |
| D4 | **No DB migration, no schema change.** Read-path only; data already persisted. | ✅ |
| D5 | **Append-only column order.** Never insert mid-sequence — consumers parse CSV positionally. TOTAL summary row emits empty cell for the new column. | ✅ |
| D6 | **Null/edge safety.** Single-tier, free, legacy, and Mode B / head-count registrations (empty `Attendees` list) → `—`, never throw. | ✅ |

## Scope of changes (5 edits, read-path only)

### Backend — `LankaConnect.Application`
1. **`src/LankaConnect.Application/Events/Common/RegistrationDetailsDto.cs`** — add nullable `Guid? TicketTierId` + `string? TicketTierName` to shared `AttendeeDetailsDto`. Additive; the 3 handlers sharing it stay null until opted in (only `GetEventAttendees` opts in now).
2. **`src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs:129-134`** — map `a.TicketTierId`, `a.TicketTierName` in the projection. **No new join.** Wrap in existing try/catch; add structured log of tier-summary computation.
3. **`src/LankaConnect.Application/Events/Common/EventAttendeeDto.cs`** — add computed `string TicketTierSummary` getter (distinct/uniform/`—` from `Attendees`), modelled on `AdditionalAttendees`.

### Backend — `LankaConnect.Infrastructure` (export, atomic pair)
4. **`src/LankaConnect.Infrastructure/Services/Export/CsvExportService.cs`** AND **`ExcelExportService.cs`** — append one `Ticket Tier(s)` column header + data cell (from `TicketTierSummary`) + empty cell in the TOTAL summary row, in BOTH writers in lockstep. Defensive try/catch around the new cell emission.

### Frontend — `web`
5. **`web/src/infrastructure/api/types/events.types.ts`** — add `ticketTierName?: string \| null` to `AttendeeDto`; add `ticketTierSummary?: string \| null` to `EventAttendeeDto`.
   **`web/src/presentation/components/features/events/AttendeeManagementTab.tsx`** — add a single "Ticket Tier" column: collapsed row renders `attendee.ticketTierSummary || '—'`; expanded detail renders each `attendees[].ticketTierName || '—'`. Confined to one new column — no surrounding-column refactor.

## Tests (TDD — RED first)

**Backend — Application.Tests**
- `GetEventAttendeesQueryHandlerTests` — projection now carries `TicketTierId`/`TicketTierName`.
- `EventAttendeeDto` `TicketTierSummary` unit cases: (a) uniform tier → single name; (b) mixed tiers → distinct joined; (c) all null (legacy/free/single-tier) → `—`; (d) empty `Attendees` (Mode B head-count) → `—`; (e) duplicate tier names dedup correctly.

**Backend — Infrastructure.Tests (export)**
- `CsvExportServiceLineEndingTests.cs` (and Excel column-count test if present) — update expected header count + assert CSV and Excel emit an identical header set including `Ticket Tier(s)`.
- New: export row carries summary; TOTAL row carries empty cell for the new column; null tier → `—`.

**Frontend**
- `AttendeeManagementTab` test: tier column header renders; collapsed summary renders for uniform/mixed/null; expanded per-attendee tier renders; null → `—`.

**Zero-tolerance:** no compilation errors at any step; full local backend + frontend suites green before commit.

## API testing plan (post-deploy verification — MANDATORY)

> Get a token first:
> ```bash
> curl -X POST 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' \
>   -H 'accept: application/json' -H 'Content-Type: application/json' \
>   -d '{"email":"niroshhh@gmail.com","password":"1qaz!QAZ","rememberMe":true,"ipAddress":"string"}'
> ```

| # | Check | Expected |
|---|---|---|
| T1 | `GET /api/events/{multiTierEventId}/attendees` (organizer token) | Each `attendees[]` element includes `ticketTierId` + `ticketTierName`; registration object includes computed `ticketTierSummary` |
| T2 | Inspect a **mixed-tier** registration in the T1 response | `ticketTierSummary` = distinct names joined (e.g. `VIP, General`) |
| T3 | Inspect a **legacy / single-tier / free** registration | `ticketTierName` null per attendee; `ticketTierSummary` = `—` |
| T4 | `GET /api/events/{id}/export?format=csv` | Response CSV header contains `Ticket Tier(s)` as the LAST column; data rows populated; TOTAL row has empty cell |
| T5 | `GET /api/events/{id}/export?format=excel` | XLSX opens; same `Ticket Tier(s)` column present, identical position/semantics to CSV |
| T6 | UI smoke on staging `/events/{id}/manage` Attendees tab | Tier column visible; collapsed summary + expanded per-attendee tier render; null rows show `—`; no console errors |
| T7 | Azure container logs after T1–T6 | No errors/exceptions from the projection or export services |

## Risks (all low)

- **Export column-order regression** — mitigated: append-only + update column-count test in the same commit.
- **CSV/Excel parity drift** — mitigated: both writers changed atomically + test asserts identical header sets.
- **Fragile table component** — mitigated: one added column only, no surrounding refactor; verified across uniform/mixed/null/empty states.
- **No data mutation, no migration, no auth surface.**

## Phase reservation (4-source check, 2026-06-01)

- Master index `PHASE_6A_MASTER_INDEX.md`: highest 6A row is 6A.160 (reserved, sponsorship-wall polish — different chain); **6A.161 absent** ✅
- `git log --oneline --all | grep '6a.161'`: **no matches** ✅
- `git branch -a | grep '6a-161'`: **no matches** ✅
- `docs/MASTER_TODO_PHASE_6A_161*.md`: **this file is the first** ✅
- (6A.160 deliberately NOT taken — reserved for sponsorship tier grouping, depends on 6A.157.)

## Deploy plan

1. Backend tests RED → GREEN; full local backend suite passes.
2. Frontend tests RED → GREEN; full local frontend suite passes.
3. Commit + push branch (descriptive `feat(events 6A.161)` messages).
4. Trigger `deploy-staging.yml` (backend) **and** `deploy-ui-staging.yml` (UI) in the same chain — feature spans both.
5. Run API testing plan T1–T7 on staging.
6. Browser smoke the Attendees tab + download both export formats on staging.
7. Update `PROGRESS_TRACKER.md` + `STREAMLINED_ACTION_PLAN.md` per `TASK_SYNCHRONIZATION_STRATEGY.md`; flip this row + master-index row.
8. Open PR to main; operator UAT.
