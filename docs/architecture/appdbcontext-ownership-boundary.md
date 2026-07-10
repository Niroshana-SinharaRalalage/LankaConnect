# AppDbContext Ownership Boundary

**Consult #20 (2026-07-10)** — architect-authoritative ownership matrix.
**Rule 5b consult artifact** — per Consult #19 blanket pre-approval, this document is the consult artifact for the bulk `modelBuilder.Ignore<T>()` sweep applied at the same commit.
**Follow-up to Consult #7 Delta** (multi-DbContext done right) + **Consult #19** (stop hotfix loop).

---

## Vision context

The 5-layer topology (BuildingBlocks → SharedKernel → Capabilities → Products → Hosts) demands each **product** and each **capability module** owns its own bounded context — including its EF Core DbContext. AppDbContext is the LEGACY god-context; it exists ONLY for transitional cross-cutting types until every domain graduates to its module DbContext. Once all extractions complete (Phase B), AppDbContext is DELETED.

Consult #7 Delta (2026-07-04) ratified: `LankaEventsDbContext`, `IdentityDbContext`, `CommunicationsDbContext`, `FormsDbContext`, `MediaDbContext`, `NotificationsDbContext` own their aggregates. Wave 6.5.f (2026-07-08 handler migration + LegacyPromotions cycle-break) removed the last runtime path that read/wrote LankaEvents aggregates through AppDbContext.

The dual-mapping pattern (`ApplyConfigurationsFromAssembly` for both LankaEvents.Infrastructure AND Identity.Infrastructure INSIDE AppDbContext.OnModelCreating) was a Wave 6.5.d transitional. Its justification — "20 not-yet-cutover LankaEvents repositories" — **expired at commit `f8ce2ee4` (Wave 6.5.f handler migration)**. Continuing to dual-map is now a landmine field: every VO with a VO-typed ctor parameter (Money(decimal, Currency), EmailSubject(string), etc.) fails EF's constructor binding when discovered by AppDbContext.

## Ownership matrix

**Module/Product-owned (Ignore<T>() on AppDbContext):**

| DbContext | Aggregate roots + child entities |
|---|---|
| `LankaEventsDbContext` | `Event`, `Registration`, `Sponsor`, `SponsorshipPackage`, `AddOnDefinition`, `AddOnPurchase`, `Collection`, `Donation`, `EventImage`, `EventVideo`, `EventTemplate`, `MetroArea`, `RegistrationAddition`, `RegistrationPayment`, `EventAnalytics`, `EventViewRecord`, `EventBadge`, `EventEmailGroupLink`, `EventNotificationHistory`, `EventOrganizerContact`, `EventSlugAlias`, `RefundRequest`, `RefundRequestLineItem`, `RegistrationModeConversion`, `Seat`, `SeatHold`, `SeatReservation`, `SignUpCommitment`, `SignUpItem`, `SignUpList`, `Ticket`, `TicketScanLog`, `TicketTier`, `VenueDecoration`, `VenueLayout`, `VenueTable`, `VenueZone`, `Badge` |
| `IdentityDbContext` | `User` |
| `CommunicationsDbContext` | `EmailMessage`, `EmailTemplate`, `UserEmailPreferences` (migrated at 4C.c) |
| `FormsDbContext` | `Form`, `FormResponse`, `FormQuestion`, `FormAnswer` (migrated at Wave 4.3) |
| `MediaDbContext` | `PhotoAlbum`, `AlbumPhoto` (migrated at Wave 4.2) |
| `NotificationsDbContext` | `Notification` (migrated at Wave 4.0b) |

**AppDbContext-owned (cross-cutting; NO Ignore<T>()):**

| Type | Schema | Rationale |
|---|---|---|
| `ReferenceValue` | `reference_data.*` | Cross-module lookup values; AppDbContext-exclusive per Consult #7 Delta |
| `StateTaxRate` (Payments.Domain.Tax) | `reference_data.state_tax_rates` | Cross-module tax lookup; per Consult #7 Delta stays on AppDbContext |

**OUT OF SCOPE for Consult #20 (open Consult #21 to relocate):**

The following Communications-domain entities still live on AppDbContext because they never migrated to CommunicationsDbContext at Wave 6.5.f. They are NOT touched by Consult #20; Consult #21 will decide their disposition (move to CommunicationsDbContext or stay on AppDbContext with proper explicit configs).

- `Newsletter`, `NewsletterEmailHistory`, `NewsletterSubscriber`
- `EmailMetricRecord`, `EmailFailureDetail`, `EmailDispatchLog`, `EmailGroup`
- `WhatsAppMessage`, `WhatsAppMessageRecord`, `WhatsAppTemplate`, `UserWhatsAppPreferences`, `WhatsAppWebhookEvent`
- `ForumTopic`, `Reply` (Community)
- `AdminAuditLog`, `SupportTicket` (Support)

## Empty-Up neutralization

`dotnet ef migrations add AppDbContextOwnershipBoundary --context AppDbContext` will emit a migration reflecting the model diff — EF sees "these tables were in the AppDbContext model; now they're not." The generated `Up()` will attempt to `DropTable` on physical tables that ARE the runtime source of truth for LankaEvents/Identity/Communications. That is CATASTROPHIC.

**Neutralization**: after generating the migration:
1. Keep the EF-regenerated `AppDbContextModelSnapshot.cs`.
2. Blank the migration `.cs` file's `Up()` and `Down()` method bodies (empty braces).
3. Retain the class + attribute so the `__EFMigrationsHistory` gets a row (marks the model shift as "applied").

Pattern reference: memory `feedback_empty_up_snapshot_rebaseline.md`.

Sibling DbContext snapshots (`LankaEventsDbContextModelSnapshot`, `IdentityDbContextModelSnapshot`, `CommunicationsDbContextModelSnapshot`) MUST NOT change — they already own these entities. Any snapshot diff on a sibling context = HALT + re-consult.

## Parity test

`tests/LankaConnect.Infrastructure.Tests/Data/AppDbContextModelParityTests.cs` asserts:

1. **Ignore assertions** — `AppDbContext.Model.FindEntityType(typeof(Event))` returns null (and every other module-owned type per the ownership matrix above).
2. **Whitelist assertion** — `AppDbContext.Model.GetEntityTypes().Select(e => e.ClrType).ToHashSet()` EQUALS the expected AppDbContext-owned set exactly (drift-detection both ways).
3. **Schema assertion** — no `AppDbContext` entity maps to `events` / `identity` / `communications` schemas.

Mirrors Rule 5e (`LankaEventsDbContextModelParityTests` pattern) and satisfies Consult #19 guardrail #1 + T4/T-9 triggers.

## Post-sprint ArchTest

Consult #19 guardrail #2: post-sprint, a NetArchTest rule enforces "AppDbContext MUST NOT reach module-owned aggregate types except via Ignore<T>()." Deferred to Wave 7.X.R Roslyn analyzer sprint.
