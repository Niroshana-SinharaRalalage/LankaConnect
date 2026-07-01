# Wave 6.5 Transitional Baseline

`Wave6_5TransitionalBaseline.json` is an **architecture-normative** allow-list of the
classes that carry the `[Wave6_5TransitionalException(reason)]` attribute at the
Wave 6.b baseline (2026-07-01, exactly the 20 Wave 5.3+5.4 Event-family repository
implementations under `Products.LankaEvents.Infrastructure.Repositories.*`).

## What the rules enforce

- **Rule 12** (`Rule12_Wave6_5TransitionalException_BaselineNotExpanded`) gates the
  SIZE of the transitional set. Any type decorated with
  `[Wave6_5TransitionalException]` whose FQCN is NOT in the baseline JSON fails
  the test. **Adding new transitional decorations requires editing this JSON**,
  which requires architect consult per Wave 6.b ruling.

- **Rule 13**
  (`Rule13_Products_Infrastructure_DoesNotReferenceAppDbContextOrRepositoryBase`)
  gates the SHAPE of what's allowed inside the transitional set. Only two specific
  dependencies are permitted for baseline classes: `AppDbContext` and
  `Repository<T>`. Any Products.Infrastructure class NOT decorated with
  `[Wave6_5TransitionalException]` that references either dependency fails.

Together the two rules enforce: *"The transitional escape hatch exists, is size-capped
at 20 classes, and only opens for two specific dependencies. Any new user of those
dependencies OR any transitional class doing anything beyond the allowed shape is a
rule failure."*

## Removing entries (Wave 6.5 progress)

When Wave 6.5 refactors a repository off `AppDbContext` + `Repository<T>` (typically
by moving it onto the new `LankaEventsDbContext` + a Products-owned Repository base),
the SAME PR that removes the attribute decoration also removes the class name from
this JSON. Atomic change, single review.

Suggested cleanup order (leaf-most repos first, aggregate roots last):

1. Leaf reads: MetroAreaRepository, EventAnalyticsRepository, EventViewRecordRepository
2. Sub-aggregate writes: AddOnDefinition, Sponsor, SponsorshipPackage, Donation,
   Collection, VenueLayout, SeatHold, SeatReservation, TicketScanLog,
   EventNotificationHistory, EventReminder
3. Payment-cluster writes (touches Wave 6.5.X.W integration events too):
   AddOnPurchase, RegistrationPayment, RegistrationAddition, Ticket
4. Aggregate roots last: Registration, Event

## Adding entries

**Additions require architect consult.** Rule 12 intentionally fails when a new
`[Wave6_5TransitionalException]` decoration is added without a corresponding baseline
edit. This is the mechanism that prevents the transitional attribute from silently
growing into permanent debt. Do NOT "fix" the failing test by expanding the baseline
without consult — that defeats the point.

## Related plan entries

- **Wave 6.5.X.Y** — Rule 5 debt (14 legacy `LankaConnect.Infrastructure` services)
- **Wave 6.5.X.W** — Rule 9b debt (11 `Payments.Application` types)
- **Wave 6.5 main** — DbContext extraction + EF Configurations move + cross-schema
  FK policy + Outbox cutover + baseline cleanup (this document's core work)

See `docs/MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md` for full traceability.
