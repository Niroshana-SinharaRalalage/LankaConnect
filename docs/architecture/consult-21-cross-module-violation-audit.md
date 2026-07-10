# Consult #21 — Cross-Module Violation Audit

**Date**: 2026-07-10 EOD
**Trigger**: Consult #21 Q3 mandatory pre-code-change audit
**Consult ref**: Follows Consult #20 (ownership-boundary hardening) execution attempt
**Blocks**: Day 7 slot A staging deploy until scope closed

## Scope discovered (larger than Consult #20/21 initial estimate)

### Category A — Module Infrastructure repos directly injecting AppDbContext

13 files across 4 modules currently inject `AppDbContext` in their constructors — a Rule 5b violation. Each must migrate to inject the corresponding module DbContext.

**Communications.Infrastructure (5 files):**
- `Data/Repositories/EmailMessageRepository.cs` — should inject `CommunicationsDbContext`
- `Data/Repositories/EmailTemplateRepository.cs` — should inject `CommunicationsDbContext`
- `Data/Repositories/UserEmailPreferencesRepository.cs` — should inject `CommunicationsDbContext`
- `Email/Services/InfrastructureTypedEmailService.cs` — should inject `CommunicationsDbContext`
- `Email/Services/RefundDispatchAuditService.cs` — decide: Communications or LankaEvents context
- `Repositories/EmailGroupRepository.cs` — should inject `CommunicationsDbContext`

**Identity.Infrastructure (1 file):**
- `Repositories/UserRepository.cs` — should inject `IdentityDbContext` (4C.e migration incomplete)

**Payments.Infrastructure (3 files):**
- `Repositories/StripeCustomerRepository.cs` — decide: PaymentsDbContext (not yet extracted) or AppDbContext
- `Repositories/StripeWebhookEventRepository.cs` — same
- `Repositories/RefundRequestRepository.cs` — **CROSS-MODULE VIOLATION (see Category B)**

**LankaEvents.Infrastructure (4 files, marked `[Wave6_5TransitionalException]`):**
- `Repositories/AddOnPurchaseRepository.cs`
- `Repositories/RegistrationAdditionRepository.cs`
- `Repositories/RegistrationPaymentRepository.cs`
- `Repositories/TicketRepository.cs`
- All should inject `LankaEventsDbContext`. Existing `[Wave6_5TransitionalException]` attribute documents debt intent.

### Category B — Cross-module boundary violation

**`src/Modules/Payments/Payments.Infrastructure/Repositories/RefundRequestRepository.cs`** operates on LankaEvents-owned aggregates:
- `_context.RefundRequests` (LankaEvents-owned)
- `_context.Registrations` (LankaEvents-owned)
- `_context.RefundRequestLineItems` (LankaEvents-owned)

Was masked by AppDbContext dual-mapping via `ApplyConfigurationsFromAssembly(LankaEvents.Infrastructure)`. Fix per Consult #21 Q2 ruling (2a with Consult #15 PASS C placement):
- Move `IRefundRequestRepository` interface + DTOs → `LankaEvents.Contracts` (NOT Domain)
- Move impl → `LankaEvents.Infrastructure/Repositories/`
- Payments consumers inject via Contracts surface
- Fall back to `LankaEvents.Contracts/LegacyPromotions/` per Consult #17 if cycles surface
- Rule 5j config-relocation audit mandatory in commit body

### Category C — LankaEvents.Application handlers using `_context` (AppDbContext) for LankaEvents-owned entities

Wave 6.5.f handler migration to `IMultiContextUnitOfWork` was INCOMPLETE — the following handlers still use `_context.<LankaEventsEntity>` on AppDbContext:

- `Commands/InitiateAddAttendees/InitiateAddAttendeesCommandHandler.cs` (Registrations)
- `Commands/InitiateAddHeadCount/InitiateAddHeadCountCommandHandler.cs` (Registrations)
- `EventHandlers/CommitmentCancelledEventHandler.cs` (SignUpCommitments)
- `MetroAreas/Queries/GetMetroAreas/GetMetroAreasQueryHandler.cs` (MetroAreas)
- `Queries/CalculateAdditionPrice/CalculateAdditionPriceQueryHandler.cs` (Registrations)
- `Queries/CheckEventRegistration/CheckEventRegistrationQueryHandler.cs` (Registrations)
- `Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs` (Registrations, Tickets)
- `Queries/GetEventRegistrationByEmail/GetEventRegistrationByEmailQueryHandler.cs` (Registrations)
- `Queries/GetEventTemplates/GetEventTemplatesQueryHandler.cs` (EventTemplates)
- `Queries/GetRegistrationById/GetRegistrationByIdQueryHandler.cs` (Registrations)
- `Queries/GetTicket/GetTicketQuery.cs` (Registrations)

All should inject `LankaEventsDbContext` (not AppDbContext) and use its DbSets.

### Category D — `IApplicationDbContext` post-4C.h residue

- `src/Modules/Communications/Communications.Application/BackgroundJobs/NewsletterEmailJob.cs:332` — comment-only reference (grep hit inside a log string), verify not live code
- `src/Modules/Media/Media.Infrastructure/BackgroundServices/AlbumPhotoCleanupService.cs:112` — comment
- `src/Products/LankaEvents/LankaEvents.Application/Queries/CheckEventRegistration/CheckEventRegistrationQueryHandler.cs:90` — comment
- `src/Modules/Communications/Communications.Infrastructure/Data/CommunicationsDbContext.cs:18` — XML doc comment

Category D is mostly noise — 4C.h delete was substantively complete.

## Consult #21 execution plan (3-day per Q4 ruling)

**Day 7 (2026-07-11)** — audit landed (this document) + start Category B (cross-module RefundRequestRepository relocation) as the highest-impact change.
- Move `IRefundRequestRepository` interface → `LankaEvents.Contracts` (per Consult #15 PASS C)
- Move `RefundRequestRepository` impl → `LankaEvents.Infrastructure/Repositories/`
- Update Payments consumers to inject Contracts surface
- Rule 5j audit in commit body

**Day 8 (2026-07-12)** — Category A + Category C bulk migration.
- Category A: 13 module Infrastructure repos → inject module DbContext
- Category C: 11 LankaEvents.Application handlers → inject LankaEventsDbContext
- Cluster by module to keep each commit reviewable per Section 0.5 guardrail (one commit per sub-slice cap)

**Day 9 (2026-07-13)** — Consult #20 sweep completion + deploy.
- Delete Assembly.Load blocks on AppDbContext
- Add Ignore<T>() for module-owned aggregates (from ownership matrix)
- Delete module-owned DbSet<T> declarations from AppDbContext (now all callers migrated)
- Empty-Up migration + `AppDbContextModelSnapshot` regen
- `AppDbContextModelParityTests.cs` GREEN
- Local `dotnet build` + cold `dotnet restore` clean
- Push develop → CI deploy → `Run-Wave9.ps1` API smoke

**Day 10 (2026-07-14)** — buffer + Category D residue cleanup + docs reconciliation.

## STOP conditions (per Consult #21)

1. Audit reveals >3 cross-module repo violations → re-consult on scope (**NOT TRIGGERED** — 1 cross-module violation: RefundRequestRepository)
2. Contracts DTO promotion cascades to new cycles → Consult #17 LegacyPromotions bucket
3. Smoke <100/261 by Day 10 EOD → Consult #10 sprint rescope
4. Day 5A URGENT slips → Consult #21 pauses (**NOT TRIGGERED** — 4C.h landed at `d7fdfa44`, all 4 deploy attempts had cold `dotnet restore` PASS)

## Sprint bible impact per Consult #21 Q5

Recomputed targets:
- Day 7: audit + Category B (no smoke)
- Day 8: Category A + C bulk (no smoke)
- Day 9: Consult #20 sweep + deploy + smoke — target **50/261** (revised down from 100/261)
- Day 10: Category D cleanup + fix-forward — target **100/261**
- Day 11-14 (Mon-Thu next week): 150/261 → 200/261 fix-forward regression

Original Day 7 target of 100/261 SLIPS. Recommend invoking sprint bible Consult #10 for rescope conversation Day 7 morning if founder concurs.

## Rule 5j config-relocation audit — this commit

Files touched: `docs/architecture/consult-21-cross-module-violation-audit.md` (new — documentation only).
No configs / interfaces / DTOs relocated.
