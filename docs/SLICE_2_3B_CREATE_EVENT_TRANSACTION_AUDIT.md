# Slice 2+3B — CreateEventCommand Transaction Boundary Audit (read-only note)

**Status:** DEFERRED — Slice 2+3A ships structural expansion only.
**Created:** 2026-04-19 (during Slice 2+3A implementation)
**Plan reference:** `C:\Users\Niroshana\.claude\plans\stateful-soaring-galaxy.md` — architect decisions #7 and #9.

---

## Why this note exists

The architect-approved plan requires splitting `CreateEventCommand` into three
sequential transactions to avoid a 500+ seat insert timing out inside a single
`SaveChangesAsync` (50 banquet tables × 10 seats = 500 rows before the registration
flow even opens). Slice 2+3A does NOT touch the command handler — it only adds the
domain building blocks (VenueTable, VenueDecoration, CanvasConfig, extended Zone /
Seat) that the Slice 2+3B saga will need.

This note captures what we observed while the code was fresh, so Slice 2+3B does
not need to re-discover the boundaries.

---

## Current handler shape

[CreateEventCommandHandler.cs](../src/LankaConnect.Application/Events/Commands/CreateEvent/CreateEventCommandHandler.cs)

- Single `Handle` method — all persistence runs under one `IUnitOfWork.SaveChangesAsync`.
- Creates `Event` aggregate + `TicketTier` children only. **No seating code is
  invoked here today.**
- Emits `EventCreatedEmailNotification` and updates email groups after the save.
- Inputs include `SeatingMode` (Slice 1) but never a layout payload.

The matching `UpdateEventCommandHandler` follows the same single-transaction pattern.

---

## 3-transaction split required by the plan

| # | Transaction | Writes | Compensating action on failure |
|---|-------------|--------|-------------------------------|
| 1 | `CreateEventTransaction` | `events.events` + `events.ticket_tiers` | Return failure to API — nothing persisted |
| 2 | `CreateLayoutTransaction` | `events.venue_layouts` + `events.venue_zones` + `events.venue_tables` + `events.venue_decorations` + `events.seats` | Event persists without seating. Log + emit metric `layout.structural_edit_rejected` with `reason="persist_failed"`. Leave `SeatingMode=GeneralAdmission` on the event. |
| 3 | `AssignLayoutTransaction` | `events.events.venue_layout_id` + `events.events.seating_mode=AssignedSeating` | Hard-delete the orphaned layout row (cascade wipes zones/tables/seats). Flip event back to `GeneralAdmission`. |

Each transaction is its own `SaveChangesAsync` boundary; they are orchestrated
at the application layer (not the DB). The saga is responsible for the
compensating deletes — there is no outer `TransactionScope`.

---

## Domain entry points already provided by Slice 2+3A

These are the sanctioned seams for the Slice 2+3B handler — no additional
domain surface is needed:

- `VenueLayout.Create(name, type, createdByUserId, eventId: null, isTemplate: false, canvas)`
  — layout is created detached from any event; `eventId` is assigned in Tx 3 via
  `EnableAssignedSeating`.
- `VenueLayout.AddZone / AddTable / AddDecoration` and the
  `GenerateTheaterSeats / GenerateRoundTable / GenerateRectTable` helpers —
  invoked in Tx 2.
- `Event.EnableAssignedSeating(layoutId)` — invoked in Tx 3. Throws
  `InvalidOperationException` if the layoutId is empty, which is what prevents a
  buggy handler from linking a layout that failed to persist in Tx 2.
- `Event.DisableAssignedSeating()` — mirror of the above, for the toggle-off
  flow during `UpdateEvent`.

The `EnableAssignedSeating` throw-on-empty-guid is the architect's line of
defence against step 3 running before step 2 succeeded. Keep this contract
intact in the saga.

---

## Compensating-delete requirement

When Tx 3 fails **after** Tx 2 succeeded, the handler must delete the orphaned
layout. The `FK_seats_venue_tables_venue_table_id` and
`FK_venue_tables_venue_layouts_venue_layout_id` foreign keys cascade, so a
single `DELETE FROM events.venue_layouts WHERE id = @id` wipes zones, tables,
decorations, and seats atomically. No explicit child cleanup needed.

---

## Verification hooks to add in Slice 2+3B

- Integration test: inject throw inside Tx 2 → event persists, no layout row.
- Integration test: inject throw inside Tx 3 → event persists, orphaned layout
  is removed, `SeatingMode = GeneralAdmission` is preserved on the event.
- Integration test: 50-table × 10-seat create completes within p95 < 5s (the
  architect flagged this as the biggest perf risk — worth asserting a soft SLO).

---

## What was NOT changed in Slice 2+3A

- `CreateEventCommandHandler.Handle` — untouched.
- `UpdateEventCommandHandler.Handle` — untouched.
- `CreateEventCommand` / `UpdateEventCommand` DTOs — already accept
  `SeatingMode` from Slice 1; no layout payload yet.

This is intentional. Landing the 3-transaction split alongside schema changes in
the same slice would violate the architect's sequencing guidance (pass 2
decision #9 — "Slice 1 scope reduced…the split moved to Slice 2+3 where the
richer domain model exists"; split specifically to Slice 2+3B so schema changes
ship independently of handler rewrites).
