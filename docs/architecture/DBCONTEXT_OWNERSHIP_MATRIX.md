# DbContext Ownership Matrix — Canonical

**Authored:** 2026-07-16 by Tech Lead (Claude) per architect Consult #28 R5 doc-drift mitigation.
**Status:** CANONICAL — supersedes Consult #7 Delta §2.4 "corrected count: 5 DbContexts" claim (that count was accurate at 2026-07-04; reality on head at 2026-07-16 = **7 operational DbContexts** plus 1 Phase B scaffold, per Consults #14 + #16 that pushed extraction further than #7 anticipated).
**Consult chain trail:** [#7 Delta](../architect-consults/2026-07-04-multi-dbcontext-implementation-comparison-ruling.md) → [#14 PASS B](../architect-consults/2026-07-05-day-4-compile-fix-strategy-ruling.md) → [#16 (4C.e User caller pattern; landed at `8465d219`)](#) → [#19 (AppDbContext Ignore sweep blanket)](#) → [#20 (AppDbContext Ownership Boundary)](appdbcontext-ownership-boundary.md) → [#25 (direct-SaveChanges blanket)](../architect-consults/2026-07-13-consult-25-day7-attack-order.md) → [#28 (Phase A completion review)](../architect-consults/2026-07-16-consult-28-phase-a-completion-review.md).

---

## 1. Ownership philosophy

Every persisted type falls in exactly one of three categories (unchanged from Consult #7 Delta §2). Only the DbContext plurality and the specific per-category assignments have moved.

- **Category VO** — SharedKernel value objects + typed IDs. No DbContext ownership; embedded via Owned-types / value converters.
- **Category PLAT** — Platform-cross-cutting entities. Live on `AppDbContext` permanently (moving them creates cross-context FK-to-Ignored-principal problems in every module).
- **Category MOD** — Module-owned aggregates. Live on the module's own DbContext.

---

## 2. The 7 operational DbContexts (Phase A head, 2026-07-16)

| # | DbContext | Physical location | Default schema | Extraction wave | Domain event dispatch |
|---:|---|---|---|---|---|
| 1 | `AppDbContext` | `src/LankaConnect.Infrastructure/Data/` | (mixed — per-entity `ToTable(name, schema)`) | Legacy (host of Category PLAT) | `DomainEventDispatcher` (legacy) — Wave 8.5.f interceptor pending |
| 2 | `LankaEventsDbContext` | `src/Products/LankaEvents/LankaEvents.Infrastructure/Data/` | `events` (per-entity two-arg `ToTable`, no `HasDefaultSchema` — see Rule 5i.1) | 6.5.e (2026-07-08) | **NOT yet wired** — Wave 8.5.f (LIVE risk R1 per Consult #28) |
| 3 | `IdentityDbContext` | `src/Modules/Identity/Identity.Infrastructure/Data/` | `identity` | 4C.e (2026-07-08 at commit `8465d219`) | **NOT yet wired** — Wave 8.5.f |
| 4 | `CommunicationsDbContext` | `src/Modules/Communications/Communications.Infrastructure/Data/` | `communications` | 4C.c (2026-07-07) | **NOT yet wired** — Wave 8.5.f |
| 5 | `NotificationsDbContext` | `src/Modules/Notifications/Notifications.Infrastructure/Data/` | `notifications` | 4.0b (early Wave 4 extraction) | ✅ Wired at commit `1212d994` (2026-07-16) |
| 6 | `MediaDbContext` | `src/Modules/Media/Media.Infrastructure/Data/` | `media` | 4.2 (2026-06 sub-slice) | ✅ Wired at commit `1212d994` (2026-07-16) |
| 7 | `FormsDbContext` | `src/Modules/Forms/Forms.Infrastructure/Data/` | `forms` | 4.3 (2026-06 sub-slice) | ✅ Wired at commit `1212d994` (2026-07-16) |

**Phase B scaffold (not counted in operational total):**

| # | DbContext | Physical location | Default schema | Status |
|---:|---|---|---|---|
| 8 | `LankaTemplesDbContext` | `src/Products/LankaTemples/LankaTemples.Infrastructure/Data/` | `temples` | Scaffold at commit `36d1fce2` (Consult #27 Q5 GREEN); empty `OnModelCreating`, no runtime footprint. FROZEN per Tech Lead D-02 until founder ratifies Phase B first-slice implementation. |

**Retired counts:** Consult #7 Delta §2.4 said "5 today (not 6)" — that was correct on 2026-07-04 (`AppDbContext + LankaEvents + Notifications + Media + Forms`). Consult #14 PASS B (2026-07-06) added `CommunicationsDbContext` at 4C.c and `IdentityDbContext` at 4C.e per the `IApplicationDbContext` teardown plan — pushing the count to 7 by 2026-07-08. Consult #7 Delta was not re-stamped at the time; hence the drift observed at Consult #28.

---

## 3. Category PLAT — AppDbContext permanent ownership

These entities are FK'd from every module. Moving them creates `Ignore<T>()` ceremony in every module context AND cross-context FK-to-Ignored-principal problems on any join. **AppDbContext is the permanent owner.** If microservice extraction ever lands, they extract to a separate SERVICE not a separate context.

| Entity | Schema | Rationale |
|---|---|---|
| `ReferenceValue` | `reference_data` | Unified reference-data lookup; every module reads. Cutover to AppDbContext direct completed at 4C.g. |
| `StateTaxRate` (`Modules.Payments.Domain.Tax`) | `reference_data` | Cross-module US-state sales-tax lookup. |
| `Badge`, `EventBadge` | `badges` | `EventBadge` is a junction crossing LankaEventsDbContext (Event) + AppDbContext (Badge); AppDbContext is the only context where BOTH principals are mapped. Consult #21 backlog will relocate Badge to LankaEventsDbContext once BadgeSeeder can route via LankaEventsDbContext with Badge DbSet added there. |
| `AdminAuditLog`, `SupportTicket` | `platform` | Cross-module operational primitives. |
| Stripe primitives (StripeCustomer, StripeWebhookEvent, PaymentIntent surface) | `payments` | Payments module NOT extracted (Consult #7 Delta §2.4). Payment intent / refund workflow tightly coupled to Registration + User; extraction cost > isolation gain. **Payments stays on AppDbContext PERMANENTLY.** |
| Newsletter family (`Newsletter`, `NewsletterEmailHistory`, `NewsletterSubscriber`, `EmailMetricRecord`, `EmailFailureDetail`, `EmailDispatchLog`, `EmailGroup`) | `communications` | Consult #20 flagged as "out of scope; Consult #21 to decide disposition." Currently on AppDbContext; may relocate to CommunicationsDbContext in Wave 8.5 tail. Category PLAT until then. |
| WhatsApp family (`WhatsAppMessageRecord`, `WhatsAppTemplate`, `UserWhatsAppPreferences`, `WhatsAppWebhookEvent`) | `communications` | Same disposition as Newsletter family. |
| `ForumTopic`, `Reply` | `community` | Community-forum aggregate. LankaConnect does not have a dedicated Community module yet; lives on AppDbContext until such a module surfaces. |

**Note on `User`:** Consult #7 Delta §2.2 originally placed `User` in Category PLAT on AppDbContext. Consult #16 (2026-07-08) split that: `User` moved to `IdentityDbContext` (per 4C.e) via the Option C mixed pattern — cross-boundary reads route through `IIdentityQueries` (Contracts), owned-writes stay on `IdentityDbContext`. AppDbContext calls `Ignore<User>()` and every module carries a `UserId` scalar FK column with no navigation. This is the single largest boundary shift beyond Consult #7 Delta and is the primary reason the DbContext count went from 5 to 7.

---

## 4. Category MOD — Module-owned aggregates per DbContext

### 4.1 `LankaEventsDbContext` (Product)

Physical schema: `events`. Aggregate + child inventory (from head, non-exhaustive):

- `Event`, `EventTemplate`, `EventNotificationHistory`, `EventOrganizerContact`, `EventSlugAlias`, `EventImage`, `EventVideo`, `EventEmailGroupLink`
- `Registration`, `RegistrationAddition`, `RegistrationPayment`, `RegistrationModeConversion`, `RegistrationModeConversionRow`
- `MetroArea`
- `SignUpList`, `SignUpItem`, `SignUpCommitment`
- `Ticket`, `TicketTier`, `TicketScanLog`, `TierAssignment`
- `VenueLayout`, `VenueZone`, `VenueTable`, `VenueDecoration`
- `Seat`, `SeatHold`, `SeatReservation`
- `Donation`, `Collection`, `Sponsor`, `SponsorshipPackage`
- `AddOnDefinition`, `AddOnPurchase`
- `RefundRequest`, `RefundRequestLineItem`
- `EventAnalytics`, `EventViewRecord`

Plus infrastructure primitives: `OutboxMessage` (`events.outbox`), `DeadLetterMessage` (`events.outbox_dead_letter`), `IdempotencyKey` (`events.idempotency_keys`).

### 4.2 `IdentityDbContext` (Module)

Physical schema: `identity`. Aggregate:
- `User` + Identity child entities (refresh tokens, external-provider linkages).

Plus infrastructure primitives: `OutboxMessage`, `DeadLetterMessage`, `IdempotencyKey` (all `identity.*`).

### 4.3 `CommunicationsDbContext` (Module)

Physical schema: `communications`. Aggregates (migrated at 4C.c):
- `EmailMessage`, `EmailTemplate`, `UserEmailPreferences`

Also mapped (though Consult #20 flagged the Newsletter+WhatsApp+Forum families as "still on AppDbContext / disposition pending"):
- `Newsletter`, `NewsletterEmailHistory`, `NewsletterSubscriber`, `EmailGroup`

Plus infrastructure primitives: `OutboxMessage`, `DeadLetterMessage`, `IdempotencyKey`.

**Consult #20 boundary note:** the WhatsApp + Newsletter + Community + Support entities were flagged OUT OF SCOPE for Consult #20 relocation. As of 2026-07-16 they retain their AppDbContext registrations (see §3 Category PLAT table). Consult #21 will decide final disposition; this doc reflects that pending state, not a final ruling.

### 4.4 `NotificationsDbContext` (Module)

Physical schema: `notifications`. Aggregate:
- `Notification`

Plus infrastructure primitives: `OutboxMessage`, `DeadLetterMessage`, `IdempotencyKey`.

### 4.5 `MediaDbContext` (Module)

Physical schema: `media`. Aggregates (migrated at Wave 4.2):
- `PhotoAlbum`, `AlbumPhoto`

Plus infrastructure primitives: `OutboxMessage`, `DeadLetterMessage`, `IdempotencyKey`.

### 4.6 `FormsDbContext` (Module)

Physical schema: `forms`. Aggregates (migrated at Wave 4.3):
- `Form`, `FormQuestion`, `FormResponse`, `FormAnswer`

Plus infrastructure primitives: `OutboxMessage`, `DeadLetterMessage`, `IdempotencyKey`.

---

## 5. Cross-context write pattern (binding — post Tech Lead D-01)

**RETIRED:** `IMultiContextUnitOfWork.CommitAsync(DbContext[])`. Per Wave 8.5.h + Tech Lead decision D-01 (`docs/coordination/DECISIONS_LOG.md`), the multi-context UoW method that shared a connection across multiple DbContexts is being **removed** — not fixed. Rationale: (a) architect Consult #25 blanket-approved single-context direct-SaveChanges as the pattern going forward; (b) the multi-context method had a known shared-connection bug whose fix cost 1-2 days; (c) direct-SaveChanges + integration events achieves the same outcome without atomicity that we do not, in practice, need.

**Current pattern (binding for all new code + all Wave 8.5.g handler migrations):**

1. **Single-context handler.** Inject the specific module DbContext directly (`LankaEventsDbContext`, `IdentityDbContext`, `CommunicationsDbContext`, `AppDbContext` for Category PLAT). Call `_dbContext.SaveChangesAsync(ct)`. Domain events dispatch via the module DbContext's `DomainEventSaveChangesInterceptor` (Wave 8.5.f wiring; complete for Notifications+Media+Forms at commit `1212d994`; LankaEvents+Identity+Communications pending).

2. **Cross-context write.** Use a **saga / compensation pattern**, NOT a shared transaction:
   - Handler A writes to its owned DbContext, raises a `<Something>IntegrationEvent`.
   - The integration event lands in the module's Outbox (`<module>.outbox` table).
   - A background dispatcher publishes the event; a subscriber in the second module writes to its DbContext.
   - Compensation: if the second write fails, subscriber emits a compensating integration event that the first module handles to roll back its write.
   - Idempotency: subscribers use the module's `idempotency_keys` table to dedupe on retries.

3. **Cross-context read.** Use the `.Contracts` query interface pattern (Consult #7 Delta §2.5, Consult #16 Option C):
   - Consumer module declares `I<Producer>Queries` in `<Producer>.Contracts`.
   - Producer implements against its DbContext.
   - **No cross-context navigation properties. No `Include()` across contexts.**

**Escalation:** if any handler surfaces during Wave 8.5.g migration that CANNOT be split into saga + integration event without unacceptable business-level atomicity loss, escalate to architect (Tech Lead D-01 explicitly names this as the one escalation trigger). Expected count of such handlers: zero based on Wave 8.5.g audit so far (5 of ~95 handlers migrated, all clean).

---

## 6. Schema declaration policy (Rule 5i.1 recap)

Per architect ruling in `feedback_hasdefaultschema_requires_explicit_schema.md`:

- **Module DbContext owning ONLY module-schema entities:** use `modelBuilder.HasDefaultSchema(SchemaName)` in `OnModelCreating`. `IdentityDbContext`, `CommunicationsDbContext`, `NotificationsDbContext`, `MediaDbContext`, `FormsDbContext` all use this pattern.
- **DbContext owning entities across multiple schemas:** MUST NOT declare `HasDefaultSchema`. Use per-entity two-arg `ToTable("<name>", "<schema>")` calls. `AppDbContext` follows this pattern (mixed schemas: `identity`, `reference_data`, `badges`, `communications`, `community`, `platform`, `payments`). `LankaEventsDbContext` follows this pattern historically because the extraction inherited pre-existing table-schema pinning (per-entity `ToTable(name, "events")`).
- **Shared BuildingBlocks configs (e.g. `OutboxMessage`, `DeadLetterMessage`, `IdempotencyKey`):** use single-arg `ToTable("<name>")` in the config class; the OWNING DbContext pins schema after `ApplyConfiguration` via `modelBuilder.Entity<OutboxMessage>().ToTable("outbox", SchemaName)`.

---

## 7. Parity-test coverage per DbContext

Rule 5e mandates a `<Ctx>ModelParityTests.cs` for every DbContext asserting mapping presence + schema + table name + `Ignore<T>` compliance. As of 2026-07-16:

| DbContext | Parity test file | Status |
|---|---|---|
| `AppDbContext` | `tests/LankaConnect.Infrastructure.Tests/Data/AppDbContextModelParityTests.cs` | ✅ (Consult #20 Ignore-sweep coverage) |
| `LankaEventsDbContext` | `tests/Products/LankaEvents/LankaEvents.Infrastructure.Tests/Data/LankaEventsDbContextModelParityTests.cs` | ✅ |
| `IdentityDbContext` | `tests/Modules/Identity/Identity.Infrastructure.Tests/Data/IdentityDbContextModelParityTests.cs` | ✅ (added at 4C.e.2) |
| `CommunicationsDbContext` | `tests/Modules/Communications/Communications.Infrastructure.Tests/Data/CommunicationsDbContextModelParityTests.cs` | ✅ |
| `NotificationsDbContext` | `tests/Modules/Notifications/Notifications.Infrastructure.Tests/Data/NotificationsDbContextModelParityTests.cs` | ✅ |
| `MediaDbContext` | `tests/Modules/Media/Media.Infrastructure.Tests/Data/MediaDbContextModelParityTests.cs` | ✅ |
| `FormsDbContext` | `tests/Modules/Forms/Forms.Infrastructure.Tests/Data/FormsDbContextModelParityTests.cs` | ✅ |
| `LankaTemplesDbContext` | — | Deferred until Phase B first slice lands (no aggregates yet). |

---

## 8. Doc drift audit trail (why this doc exists)

Consult #28 R5 (2026-07-16) named three pieces of doc drift; this doc closes one:

- **Consult #7 Delta §2.4** — "Corrected DbContext count: 5 today (not 6)." Accurate at 2026-07-04; drifted to 7 as Consult #14 (Communications) + #16 (Identity) landed. This doc IS the reconciliation. **Do not delete Consult #7 Delta §2.4** — it is a historical ruling. This doc supersedes it going forward and cross-links back for provenance.
- **CLAUDE.md §-1 + §0.6** — refreshed in the same Wave-1 doc-refresh commit as this file.
- **`docs/PLATFORM_MASTER_PLAN.md` status header** — refreshed in the same commit.

If any future consult ruling changes any category assignment, schema policy, or DbContext count, update this document in the same commit as the consult ruling. Do not let the doc-drift shape recur.

---

## 9. Related canonical documents

- [`docs/architecture/ENTERPRISE_ARCHITECTURE_BLUEPRINT.md`](ENTERPRISE_ARCHITECTURE_BLUEPRINT.md) — 5-layer topology + D1-D10 decisions.
- [`docs/architecture/appdbcontext-ownership-boundary.md`](appdbcontext-ownership-boundary.md) — Consult #20 AppDbContext ownership (still valid; complements this matrix).
- [`docs/architect-consults/2026-07-04-multi-dbcontext-implementation-comparison-ruling.md`](../architect-consults/2026-07-04-multi-dbcontext-implementation-comparison-ruling.md) — Consult #7 Delta (superseded on the 5-vs-7 count only; all other rulings remain in force).
- [`docs/architect-consults/2026-07-13-consult-25-day7-attack-order.md`](../architect-consults/2026-07-13-consult-25-day7-attack-order.md) — Consult #25 direct-SaveChanges blanket approval.
- [`docs/architect-consults/2026-07-14-consult-27-phase-a-close.md`](../architect-consults/2026-07-14-consult-27-phase-a-close.md) — Phase A close-out ratification.
- [`docs/architect-consults/2026-07-16-consult-28-phase-a-completion-review.md`](../architect-consults/2026-07-16-consult-28-phase-a-completion-review.md) — Phase A completion review (doc-drift observation origin).
- [`docs/coordination/DECISIONS_LOG.md`](../coordination/DECISIONS_LOG.md) — Tech Lead D-01 (retire `IMultiContextUnitOfWork.CommitAsync(DbContext[])`).
