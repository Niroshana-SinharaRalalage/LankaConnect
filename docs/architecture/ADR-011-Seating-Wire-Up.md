# ADR-011 — Seating Wire-Up (Slice S8)

**Status:** Approved 2026-05-04 (architect + user sign-off on default Q1–Q5 answers)
**Author:** system-architect agent + implementer
**Supersedes:** part of `MASTER_TODO_SLICE9_SEATING_FIX.md` (the seating-foundation-and-fix planning thread)

---

## Context

Phase 2A foundation commit `b9bbfc3a` (2026-04-17) introduced the seating *foundation* — entities (`SeatHold`, `SeatReservation`, `VenueLayout`, `Seat`), repositories, EF migrations for the tables, the `SeatsReservedEvent` domain event, and added vestigial `SeatId`/`SeatLabel` fields to `AttendeeDetails`. The work was scoped explicitly to "domain layer foundation"; the application/infrastructure write-side wiring was deferred. Subsequent slices (S2 through S6 plus Phase 7H observability) built layout creation tools, the SeatPicker UI, hold/release endpoints, the cleanup background service, the structural-edit guard's *read* path, and metrics — every one of those slices added consumers of `SeatReservation`-data, but **none added a producer**.

While wiring `seat_hold.converted_to_reservation` for Phase 7H, we discovered the gap is comprehensive end-to-end:

- Frontend already sends `seatIds` + `seatSessionId` in the RSVP request body.
- Backend `RsvpToEventCommand` and `RegisterAnonymousAttendeeCommand` have no fields for those values — System.Text.Json silently drops them.
- Handlers call `AttendeeDetails.Create(name, age, gender, tierId, tierName)` with no `seatId`/`seatLabel`.
- `RegistrationConfiguration.OwnsMany(r => r.Attendees, b => b.ToJson("attendees"))` doesn't map `SeatId`/`SeatLabel` to JSONB columns.
- `RegistrationWebhookHandler.HandleCheckoutCompletedAsync` doesn't convert holds → reservations or bind seat-ids to attendees.
- Read-side handlers (email, ticket PDF) read `attendee.SeatLabel` correctly — but the value is always null.

**Effect on staging:** a buyer who picks seats, holds them, pays via Stripe, gets `Confirmed/PaymentCompleted` — and the seat assignment is silently dropped. Hold expires 10 min later; another buyer can claim the same seat. Email + ticket PDF show no seat label. Organiser can structurally delete the seat 10 min later because the structural-edit guard sees 0 reservations (it queries an empty `seat_reservations` table).

## Decision

Implement **Slice S8 — Seating Wire-Up** as 4 sequential chunks:

```
S8.1  ──────►  S8.2  ──────►  S8.3
  │                              │
  └──────►  S8.4  ◄──────────────┘
```

| Chunk | Acceptance | Effort |
|---|---|---|
| **S8.1** | Domain shape: `Registration.ConfirmSeatAssignments`, `AttendeeDetails.WithSeat`, EF mapping for `seat_id`/`seat_label` JSONB fields, snapshot-only migration. No behaviour change. | 5–6 h |
| **S8.2** | Add `SeatIds`/`SeatSessionId` to RSVP commands; pre-checkout validation; pending-seat-assignments JSONB on registration; webhook converts holds → `SeatReservation` rows + binds seat-ids to attendees with C2-guarded race handling. **End-to-end bug fixed.** | 13–14 h |
| **S8.3** | New `SeatReservationsReleasedEvent` raised from `CompleteRefund` / `MarkAbandoned` / cancel paths → handler hard-deletes `seat_reservations` rows. | 4–5 h |
| **S8.4** | Audit existing broken rows on staging → refund + comp; staging API smoke; close `seat_hold.converted_to_reservation` metric gap. | 3–4 h |

**Total estimate: 25–30 h, ~3–4 working days.**

## Q1 — Cancel-with-refund unlock semantics

**Decision:** **Hard-delete** the `SeatReservation` row when a registration transitions to Refunded / Cancelled / Abandoned. The seat returns to the available pool.

**Rationale:**
- The repository contract already documents this (`ISeatReservationRepository.cs:8` "On cancellation/refund, the reservation is hard-deleted (V1 — no soft delete)").
- A "forever-locked" seat creates organiser pain.
- Industry standard (Ticketmaster, Eventbrite all unlock).
- Audit trail: `registrations` row stays; only `seat_reservations` row goes.

## Q2 — Hold-vs-reservation race during long Stripe sessions

**Decision:** **Try-insert at webhook with optimistic-fail.** Payment confirms regardless; on rare race-loss, log `seat_conversion.race_lost` metric and let support handle it.

**Rationale:**
- Stripe's own data: 99.9% of buyers complete payment within 2 min (well within 10-min hold TTL).
- Auto-extending holds across Stripe Checkout state-machine adds significant operational complexity for a 0.1% case.
- Hard-failing the webhook after Stripe charged the card is worse than a one-in-a-million orphan that support can fix.
- Hold TTL stays at **10 minutes** for now. If `seat_conversion.race_lost` events appear in production telemetry, revisit with a 30-min-TTL slice for `AssignedSeating` events.

## Q3 — In-flight migration for already-broken staging registrations

**Decision:** **Refund + comp + courtesy email.** Acceptable on staging because there are no real users.

**Rationale:**
- Audit query identifies rows where `event.seating_mode = AssignedSeating AND registration.status = Confirmed AND registration.payment_status = Completed AND no attendee has seat_id`.
- For each, refund via the existing organiser API (Phase 7E), mark cancelled with reason `"S8 data fixup — pre-seat-binding registration"`, send a courtesy email.
- Document affected registration IDs in `docs/PROGRESS_TRACKER.md`.
- Cleanup script: `scripts/sql/2026-05-S8-data-fixup.sql` (NOT an EF migration — manual one-off).

## Q4 — Add-attendees-with-seats deferral

**Decision:** **Defer to Slice S9.** S8.2 will explicitly reject `InitiateAddAttendees` for `AssignedSeating` events with a clear error message: `"Add-attendees not yet supported for seated events — coming in Slice S9."`

## Q5 — Hold TTL

**Decision:** **Stay at 10 minutes** for S8. Revisit only if `seat_conversion.race_lost` telemetry shows real impact.

---

## Implementation plan (chunk-by-chunk)

### S8.1 — Domain + EF persistence shape

**Domain changes:**

- `src/LankaConnect.Domain/Events/ValueObjects/AttendeeDetails.cs` — already has `SeatId`/`SeatLabel`; add a method `AttendeeDetails WithSeat(Guid seatId, string seatLabel)` that returns a new immutable instance with the seat fields populated. Trim `seatLabel`. Reject empty `seatId` / empty `seatLabel`.
- `src/LankaConnect.Domain/Events/Registration.cs` — new method `Result ConfirmSeatAssignments(IReadOnlyList<(int AttendeeIndex, Guid SeatId, string SeatLabel)> assignments)`:
  - Status must be `Confirmed`.
  - `assignments.Count == _attendees.Count` (one seat per attendee — Mode A only).
  - Each `AttendeeIndex` is unique and within `[0, _attendees.Count)`.
  - Each `SeatId` is non-empty.
  - Replace `_attendees[index]` with `attendees[index].WithSeat(seatId, seatLabel)`.
  - Idempotent: if attendee already has `(seatId, seatLabel)`, return Success without raising the event.
  - Otherwise raise `SeatsReservedEvent(EventId, Id, assignments)`.
- `src/LankaConnect.Domain/Events/DomainEvents/SeatsReservedEvent.cs` — already exists, no change.

**Infrastructure changes:**

- `src/LankaConnect.Infrastructure/Data/Configurations/RegistrationConfiguration.cs:116` — extend the `attendeesBuilder` block:
  ```csharp
  attendeesBuilder.Property(a => a.SeatId)
      .HasColumnName("seat_id")
      .IsRequired(false);
  attendeesBuilder.Property(a => a.SeatLabel)
      .HasColumnName("seat_label")
      .HasMaxLength(50)
      .IsRequired(false);
  ```
- `dotnet ef migrations add Phase8S81_AddSeatFieldsToAttendeeJsonb` — generate the migration. The `Up()`/`Down()` SQL will be empty because JSONB columns are schema-less. **Do NOT delete the file** — EF Core needs the snapshot updated so subsequent migrations diff correctly. Add a comment in the migration body: `// Snapshot-only migration: attendees JSONB is schema-less; new SeatId/SeatLabel fields require no schema change.`

**Tests (TDD order):**

1. `tests/LankaConnect.Domain.Tests/Events/ValueObjects/AttendeeDetailsTests.cs` (extend) — `WithSeat_ReturnsNewInstance_PreservesOriginal`, `WithSeat_TrimsSeatLabel`, `WithSeat_RejectsEmptySeatId`, `WithSeat_RejectsEmptySeatLabel`.
2. `tests/LankaConnect.Domain.Tests/Events/RegistrationTests.cs` (extend) — `ConfirmSeatAssignments_HappyPath_BindsSeatsAndRaisesEvent`, `ConfirmSeatAssignments_RejectsWhenStatusNotConfirmed`, `ConfirmSeatAssignments_RejectsCountMismatch`, `ConfirmSeatAssignments_RejectsDuplicateAttendeeIndex`, `ConfirmSeatAssignments_IsIdempotentWhenAttendeeAlreadyHasSameSeat`.
3. `tests/LankaConnect.Infrastructure.Tests/Data/Configurations/RegistrationJsonbRoundTripTests.cs` — integration test using Testcontainers Postgres: persist a `Registration` with seated attendees, fetch, assert `SeatId`/`SeatLabel` round-trip correctly.

**Observability:** none for this chunk.

### S8.2 — Application + API + Webhook conversion

(Full plan in commit description; see also `docs/PROGRESS_TRACKER.md` 2026-05-04 entries.)

**Highlights:**

- Add `List<Guid>? SeatIds = null` and `string? SeatSessionId = null` to `RsvpToEventCommand` + `RegisterAnonymousAttendeeCommand` (additive — preserves source-compatibility).
- Pre-checkout validation: when `event.SeatingMode == AssignedSeating`, require `SeatIds.Count == Attendees.Count` AND every `SeatId` in the request is held in this `SessionId` AND none are reserved. Persist intended `SeatId`/`SeatLabel` onto each `AttendeeDetails`.
- New `Registration.SetPendingSeatAssignments(...)` method + `pending_seat_assignments` JSONB column (real EF migration `Phase8S82_AddPendingSeatAssignmentsToRegistration`).
- `RegistrationWebhookHandler.HandleCheckoutCompletedAsync` — new section after `CompletePayment`: read `PendingSeatAssignments`, insert `SeatReservation` rows (C2-guarded), call `SeatHold.Confirm()` on the matching holds, call `Registration.ConfirmSeatAssignments(...)`, clear pending state, emit `seat_hold.converted_to_reservation` metric. On race-loss (`DbUpdateException` 23505): log `seat_conversion.race_lost`, leave registration confirmed-but-unseated, do NOT fail the webhook.
- `HandleCheckoutExpiredAsync` — release pending holds eagerly (symmetric C5 guard).
- `InitiateAddAttendees` rejects `AssignedSeating` events with clear S9-deferral message.

### S8.3 — Cancel/refund unlock

- `src/LankaConnect.Domain/Events/DomainEvents/SeatReservationsReleasedEvent.cs` — new event.
- Raise from `Registration.CompleteRefund`, `MarkAbandoned`, `CancelRegistration`, `FailPayment`.
- `src/LankaConnect.Application/Events/EventHandlers/SeatReservationsReleasedEventHandler.cs` — new handler calling `_seatReservationRepository.DeleteByRegistrationIdAsync(registrationId)`.
- Tests cover the four trigger paths + idempotent no-op when no rows.
- Emit `seat_reservation.released` metric with reason tag.

### S8.4 — Data fixup + observability close-out

- Audit query to identify broken in-flight rows.
- `scripts/sql/2026-05-S8-data-fixup.sql` — manual psql script for staging-only cleanup.
- Final staging API smoke: hold seats → RSVP → fire `checkout.session.completed` via Stripe CLI → assert `attendees[0].seatLabel` non-null → assert `seat_reservations` row exists → wait 11 min → POST structural-edit attempt → assert 422 with reservation-blocking message.
- Verify `seat_hold.converted_to_reservation` metric appears in container logs.
- Update `docs/STREAMLINED_ACTION_PLAN.md` and `docs/MASTER_TODO_SEATING_MVP.md` to mark `seat_hold.converted_to_reservation` as ✅ shipped (closes the 9/11 → 10/11 metric coverage; only `canvas_editor.session_abandoned` remains deferred).

---

## Risk register

| Risk | Likelihood | Impact | Mitigation | Rollback |
|---|---|---|---|---|
| **R1** JSONB attendees rehydration loses `SeatId`/`SeatLabel` | Low | High | S8.1 integration test with real Postgres; owned-entity scalars work in `ToJson()`. | Revert deploy. |
| **R2** Webhook race-loss on duplicate seat insert | Very low | Medium | C2-guarded try-catch; payment confirms regardless; `seat_conversion.race_lost` metric. | Manual reseat or refund. |
| **R3** Pending-seat-assignments stash out-of-sync with `seat_holds` | Low | Medium | Look up by `SessionId`; fall back to R2 path if hold expired. | Same as R2. |
| **R4** New `ConfirmSeatAssignments` invariants reject a valid case | Medium | Medium | Webhook treats failure as logged warning, not fatal. | Forward fix. |
| **R5** `SeatReservationsReleasedEvent` fires for free events | High | Low | `DeleteByRegistrationIdAsync` is idempotent. | None needed. |
| **R6** Frontend stale cache sends `seatIds` for GA events | Low | Low | New 400 with friendly message. | Frontend retry clears state. |

---

## Sequencing

```
PR-1 (S8.1)  ──►  PR-2 (S8.2)  ──►  PR-3 (S8.3)  ──►  PR-4 (S8.4)
   docs       │     bug fix       │   unlock      │   close-out
              │                   │               │
              └─ end-to-end       └─ refunds      └─ metric coverage
                 functional         unlock seats     9/11 → 10/11
                 from this point
```

Each PR ships independently to staging via the standard `deploy-staging.yml` flow with TDD red→green discipline, structured logs on every new code path, try-catch on every external call, and per-PR API smoke per CLAUDE.md §6.
