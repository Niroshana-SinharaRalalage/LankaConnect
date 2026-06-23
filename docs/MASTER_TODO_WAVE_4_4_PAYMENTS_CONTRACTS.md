# Wave 4.4 — Payments.Contracts (IPaymentQueries + IPaymentCommands) — Implementation Plan

**Status**: PLANNED — drafted 2026-06-23, awaits architect consult before 4.4.a kicks off.

**Predecessor**: Wave 4.1 Communications.Contracts (the just-shipped commits labeled `Wave5.4.*` — see [[wave-numbering-correction]] memory; those commits ARE the Wave 4.1 master-plan deliverable, just mis-labeled at the per-wave-doc level).

**Memory pins to anchor the plan**:
- `feedback_module_extraction_cross_aggregate_nav.md` — Wave 4.1.2 typed-nav blocker; the Risk #1 issue below is the SAME class of problem with a different resolution.
- `feedback_read_side_bypass_audit.md` — Wave 5.4.d.4 hotfix lesson; the smoke matrix below explicitly includes the dbContext.Set bypass grep.
- `feedback_empty_up_snapshot_rebaseline.md` — applies only if Risk #1 resolves to "move RefundRequest"; if it resolves to "keep in legacy Events", no migration is needed.

---

## TL;DR

Wave 4.4 splits into **8 atomic commits (4.4.a → 4.4.d.3)**. Same shape as Wave 4.1 (Communications) and Wave 4.3 (Forms) with TWO key shape differences:

1. **No aggregate physical move** (Risk #1 resolution path A). `RefundRequest` is a CHILD entity of the `Registration` aggregate (line 104 of `Registration.cs`), not a standalone aggregate root. Forms (5.3) and Communications (5.4) moved standalone aggregates; this one doesn't. The plan recommends Option A (keep RefundRequest where it is, only extract handlers + define Contracts) — see Risk #1 for the architect's call.
2. **No EF migration in any sub-phase** (consequence of #1). Wave 4.1 had two junction-table rebaselines (5.4.c.0 + 5.4.d.1b); Wave 4.4 should have zero schema deltas if Risk #1 resolves to Option A.

Final state:
- `IPaymentQueries` + `IPaymentCommands` live in `src/Modules/Payments/Payments.Contracts/`.
- `LankaConnect.Application` consumes the Contracts surface for all read-side + mutator access to refund/payment data (no Payments.Domain edge).
- 7 command handlers + 2 query handlers + 12 event handlers + 6 service implementations physically relocated to `Payments.Application`.
- Stripe repository implementations (`StripeCustomerRepository`, `StripeWebhookEventRepository`) relocated to `Payments.Infrastructure`.
- ArchTest Rule 5 `LegacyApplication_DoesNotDependOnPaymentsDomain` pins the Application-layer cut. **No Rule 6** for `LegacyDomain_DoesNotDependOnPaymentsDomain` UNLESS Risk #1 resolves to Option B.

---

## Pre-flight survey snapshot (2026-06-23)

| Concern | State |
|---|---|
| `src/Modules/Payments/` skeleton | **Does NOT exist** — fresh extraction |
| Legacy Payments domain code | `src/LankaConnect.Domain/Payments/` (2 Stripe repo interfaces, 74 LOC) + `src/LankaConnect.Domain/Billing/` (4 files, 1,776 LOC, **out of scope**) |
| `RefundRequest` aggregate | `src/LankaConnect.Domain/Events/Entities/RefundRequest.cs` (400 LOC) — **child of Registration, not standalone** |
| `RefundRequestLineItem` | `src/LankaConnect.Domain/Events/Entities/RefundRequestLineItem.cs` (~150 LOC) — child of RefundRequest |
| `RegistrationPayment` | `src/LankaConnect.Domain/Events/RegistrationPayment.cs` (208 LOC) — child of Registration |
| Cross-aggregate typed navs | `Registration.cs:104 private readonly List<RefundRequest> _refundRequests` — **Risk #1** |
| Repository interfaces | `IStripeCustomerRepository` (3 methods) + `IStripeWebhookEventRepository` (5 methods) + `IRefundRequestRepository` (10 methods) — **drives the IPaymentQueries surface** |
| Command handlers in scope | 7 (5 RefundRequests + 1 ForceCancelStuckRefund + 1 WithdrawRefundRequest v1) |
| Query handlers in scope | 2 (GetEventRefundRequestsQH + GetMyRefundRequestQH) |
| Event handlers in scope | 12 (5 under RefundRequests/ + 7 Payment/Refund event handlers in Events/EventHandlers/) |
| Service implementations in scope | 6 (RefundExecutionService + RefundLineDispatcher + RefundReconciliationService + RefundTotalCalculator + AddOnRefundService + RegistrationRefundService) |
| `IStripePaymentService` | 817 LOC interface — declared in `LankaConnect.Application.Common.Interfaces/`. **Architect call**: stays in legacy Application as a port, or moves to Payments.Application? (sub-question of Risk #2 below) |
| `CulturalIntelligenceBilling*` | 1,776 LOC — **out of scope** for Wave 4.4 (SaaS-tier billing, separate concern). Future "Wave 4.X Billing.Contracts" or fold into Wave 5 Products carve-out. |
| Cross-module consumers | 8+ Events command handlers call `IStripePaymentService` for checkout/refund dispatch; CancelRsvpCommandHandler, RegisterAnonymousAttendee, RsvpToEvent, CreateCollection, CreateDonation, CreateSponsor, PurchaseAddOn, InitiateAddAttendees/HeadCount, EventCancellationEmailJob |
| Existing Payments ArchTests | None — expected, module doesn't exist yet |

---

## Sub-phase decomposition

### 4.4.a — Define `Payments.Contracts` surface (additive)

New files in `src/Modules/Payments/Payments.Contracts/`:
- `IPaymentQueries.cs` — read-side surface for cross-module consumers
- `IPaymentCommands.cs` — mutator surface (mirrors 5.3 ICommunicationsCommands shape — empty marker for now, populated in 4.4.d.1 once consumer audit lands)
- DTOs: `RefundRequestSummaryDto`, `RefundRequestDetailDto` (with line items), `RefundLineItemDto`, `StripeCustomerSummaryDto`
- Enums: `RefundRequestStatusDto`, `RefundLineItemTypeDto` (deliberately duplicated from Domain — Contracts must not pull Payments.Domain per the Forms/Communications precedent)

T-triggers: T1 (new public interfaces). Tests: `Payments.Contracts.Tests/` — interface-shape pinning (~5 tests).

**ArchTest rule lands HERE** (added to `LayeringRules.cs`): `Modules_Payments_Contracts_DependsOnlyOnBuildingBlocksContracts`.

### 4.4.b — Implement `PaymentQueries` + `PaymentsModule` DI (additive)

- `Payments.Application/Queries/PaymentQueries.cs` — wraps the legacy `IRefundRequestRepository` + `IStripeCustomerRepository` + `IStripeWebhookEventRepository` (transitional; delegates via DI seam until 4.4.d.2)
- `Payments.Application/Mappings/PaymentContractMappings.cs` — `ToSummaryDto()` / `ToDetailDto()` extensions
- `Payments.Api/PaymentsModule.cs` — DI: `services.AddScoped<IPaymentQueries, PaymentQueries>()` + MediatR/FluentValidation scan placeholder (handlers move in later sub-phases)
- `Payments.Application.csproj` adds `<ProjectReference Payments.Contracts>` + transitional edge to `LankaConnect.Application` (mirrors Forms/Communications 5.3c.0/5.4.b pattern)

T-triggers: T3 (new handlers) + T6 (DI registration). Tests: `Payments.Application.Tests/Queries/PaymentQueriesTests.cs` (~7 tests).

### 4.4.c.0 — Risk #1 resolution checkpoint (NOT a code commit)

**Before any handler moves**, architect must rule on Risk #1 (Registration.List<RefundRequest> typed nav). See Risk #1 below for the three options. The chosen path determines whether 4.4.c.1+ relocates RefundRequest.cs as part of the physical move or leaves it in Registration.

**If Option A (recommended)**: skip 4.4.c.0 entirely — RefundRequest stays in `LankaConnect.Domain.Events.Entities`. The Registration aggregate continues to own its typed `List<RefundRequest>` nav. The Payments module reads RefundRequest data via Repository abstraction injected as `IRefundRequestRepository` (which moves to Payments.Domain in 4.4.d.2 BUT continues to return the legacy `LankaConnect.Domain.Events.Entities.RefundRequest` type — exactly mirrors the W5.4.b transitional state where `IEmailGroupRepository` lived in `LankaConnect.Domain.Communications` and the Payments module's Application layer takes a transitional `LankaConnect.Domain` edge).

**If Option B**: insert a typed-nav-surgery sub-phase here mirroring W5.4.c.0:
- New CLR junction type `Registration._refundRequestLinks: List<RegistrationRefundRequestLink>` (raw Guids, no nav).
- Move RefundRequest + RefundRequestLineItem out to Payments.Domain.
- EF snapshot rebaseline migration (empty Up()/Down() per `feedback_empty_up_snapshot_rebaseline.md`).
- ~5 unit tests pinning Registration._refundRequestIds ↔ _refundRequestLinks sync.

**If Option C**: defer Wave 4.4 until after Wave 4.6 Identity + the broader Phase-A re-think the architect noted in [[project_phase_a_v5_wave_plan]].

### 4.4.c.1 — Move 7 command handlers into Payments.Application

Files moved from `src/LankaConnect.Application/Events/Commands/RefundRequests/` (+ 2 sibling folders) to `src/Modules/Payments/Payments.Application/Commands/`:

- `ApproveRefundRequest/ApproveRefundRequestCommandHandler.cs`
- `CreateRefundRequest/CreateRefundRequestCommandHandler.cs`
- `CreateOrganizerInitiatedRefund/CreateOrganizerInitiatedRefundCommandHandler.cs`
- `RejectRefundRequest/RejectRefundRequestCommandHandler.cs`
- `WithdrawRefundRequestV2/WithdrawRefundRequestV2CommandHandler.cs`
- `ForceCancelStuckRefund/ForceCancelStuckRefundCommandHandler.cs`
- `WithdrawRefundRequest/WithdrawRefundRequestCommandHandler.cs` (legacy v1)

Plus the command DTOs + validators that travel with each handler. API controllers DO NOT MOVE.

T-triggers: T1+T3+T7. S-class: S2 (mutator round-trip — POST /api/Refunds → re-fetch → assert state).

### 4.4.c.2 — Move 2 query handlers into Payments.Application

Files: `GetEventRefundRequestsQueryHandler.cs` + `GetMyRefundRequestQueryHandler.cs`. Includes the `AttendeeRefundRequestDto.cs` + `EventRefundRequestsDto.cs` (mirrors `EventFormDtos.cs` treatment in W5.3c.2 and `EmailGroupDto.cs` in W5.4.c.2).

T-triggers: T1+T3+T7. S-class: S1 (read-only GET shape parity).

### 4.4.c.3 — Move 12 event handlers into Payments.Application

Files moved from `src/LankaConnect.Application/Events/EventHandlers/RefundRequests/` (+ 7 from sibling `EventHandlers/`):

RefundRequests sub-folder:
- `RefundRequestCreatedEventHandler.cs`
- `RefundRequestApprovedEventHandler.cs`
- `RefundRequestRejectedEventHandler.cs`
- `RefundRequestWithdrawnEventHandler.cs`
- `OrganizerInitiatedRefundCreatedEventHandler.cs`

Sibling Events/EventHandlers/:
- `PaymentCompletedEventHandler.cs`
- `PaymentCompletedWhatsAppHandler.cs`
- `PaymentPendingWhatsAppHandler.cs`
- `RefundCompletedEventHandler.cs`
- `RefundCompletedWhatsAppHandler.cs`
- `RefundRequestedEventHandler.cs`
- `RefundRequestedWhatsAppHandler.cs`

Risk to verify before the move: any of these handlers that publish to `INotificationService` / `IEmailService` / `IWhatsAppMessagingService` need to continue resolving those legacy ports across the new module boundary. Mirrors the 5.4.c.1 hotfix pattern (MediatR + FluentValidation scan added to PaymentsModule.cs).

T-triggers: T1+T3+T7. S-class: S3 (log silence — ensure published domain events are still handled exactly once after the move; missing scan would silently drop handlers).

### 4.4.c.4 — Move 6 service implementations into Payments.Application + Payments.Infrastructure

Service interfaces (port) + implementations (adapter) move together to maintain DI cohesion:

- `IRefundExecutionService` + `RefundExecutionService` — post-approval Stripe dispatch
- `IRefundLineDispatcher` + `RefundLineDispatcher` — per-line Stripe calls (Phase 6A.148.W5.D2)
- `IRefundReconciliationService` + `RefundReconciliationService` — webhook safety net (Phase 7G)
- `IRefundTotalCalculator` + `RefundTotalCalculator` — refund completion email aggregation
- `IAddOnRefundService` + `AddOnRefundService`
- `IRegistrationRefundService` + `RegistrationRefundService`

Architect ruling needed: do the interfaces (the port) live in `Payments.Application` or `Payments.Contracts`? Forms precedent says ports stay in `*.Application`; only cross-module-consumable surfaces go in `*.Contracts`. These 6 services are NOT consumed across module boundaries (verified by survey — they're called from Payments-internal commands + handlers only), so `Payments.Application` is correct.

T-triggers: T6 (DI registration relocation from legacy DependencyInjection.cs to PaymentsModule.cs). S-class: S2 (mutator round-trip for refund approval flow).

### 4.4.c.5 — Gap verification

Grep `src/LankaConnect.Application/` (excluding the now-moved subtrees) for any remaining handler/service that imports:
- `LankaConnect.Domain.Events.Entities.RefundRequest`
- `LankaConnect.Domain.Events.Entities.RefundRequestLineItem`
- `LankaConnect.Domain.Events.RegistrationPayment`
- `LankaConnect.Domain.Payments.IStripeCustomerRepository`
- `LankaConnect.Domain.Payments.IStripeWebhookEventRepository`
- Any of the 6 service interfaces moved in 4.4.c.4

Plus the dbContext.Set bypass grep per `[[feedback_read_side_bypass_audit]]`:
- Grep `dbContext.Set<` for table names: `refund_requests`, `refund_request_line_items`, `registration_payments`, `stripe_customers`, `stripe_webhook_events`.

If zero non-Payments hits: document in commit message as "no remaining cross-module Payments consumers in legacy Application".

If non-zero: each hit is either (a) a legitimate use-case that needs an IPaymentQueries/IPaymentCommands method added to Contracts, OR (b) dead code to delete. Split into 4.4.c.5a (add Contracts method) + 4.4.c.5b (swap consumer).

### 4.4.d.1 — Swap read-side cross-module consumers in LankaConnect.Application to IPaymentQueries

Replace direct `IRefundRequestRepository` / Stripe repo injection with `IPaymentQueries` in consumers that only READ payment data. List determined by grep at execution time (parallel to 4-consumer list in W5.4.d.1 + 12-handler list in W5.3d.1).

Likely consumers based on survey:
- `EventCancellationEmailJob.cs` (reads refund state for cancellation emails)
- `CancelRsvpCommandHandler.cs` (reads RefundRequest history before approving cancellation)
- `RegistrationPendingPaymentEventHandler.cs` (reads RegistrationPayment state)
- API controllers that compose refund detail responses

T-triggers: T3. S-class: S1 (read-only routing change) + S3 (log silence — same `[[feedback_read_side_bypass_audit]]` pattern).

### 4.4.d.2 — Physical move of Payments code

Two atomic moves combined:

**Move 1 — physical relocation** (`git mv` for blame preservation):
- `src/LankaConnect.Domain/Payments/IStripeCustomerRepository.cs` → `src/Modules/Payments/Payments.Domain/Repositories/IStripeCustomerRepository.cs`
- `src/LankaConnect.Domain/Payments/IStripeWebhookEventRepository.cs` → `src/Modules/Payments/Payments.Domain/Repositories/IStripeWebhookEventRepository.cs`
- `src/LankaConnect.Domain/Events/Repositories/IRefundRequestRepository.cs` → `src/Modules/Payments/Payments.Domain/Repositories/IRefundRequestRepository.cs`
- `src/LankaConnect.Infrastructure/Data/Repositories/StripeCustomerRepository.cs` → `src/Modules/Payments/Payments.Infrastructure/Repositories/StripeCustomerRepository.cs`
- `src/LankaConnect.Infrastructure/Data/Repositories/StripeWebhookEventRepository.cs` → `src/Modules/Payments/Payments.Infrastructure/Repositories/StripeWebhookEventRepository.cs`
- `src/LankaConnect.Infrastructure/Data/Repositories/RefundRequestRepository.cs` → `src/Modules/Payments/Payments.Infrastructure/Repositories/RefundRequestRepository.cs`
- `src/LankaConnect.Application/Billing/StripeWebhookHandler.cs` → `src/Modules/Payments/Payments.Application/Webhooks/StripeWebhookHandler.cs`

**EF configurations stay in `LankaConnect.Infrastructure/Data/Configurations/`** — same reason as W5.4.d.2 (`EmailGroupConfiguration` stayed there to avoid a circular ref between `LankaConnect.Infrastructure` ↔ `Payments.Infrastructure`).

**`RefundRequest.cs` + `RefundRequestLineItem.cs` + `RegistrationPayment.cs` DO NOT MOVE** under Option A — they remain Registration aggregate children in `LankaConnect.Domain.Events.Entities/`. The IRefundRequestRepository interface in Payments.Domain returns the legacy `LankaConnect.Domain.Events.Entities.RefundRequest` type via a transitional Payments.Domain → LankaConnect.Domain ProjectReference (mirrors W5.4.d.2 Communications.Domain → LankaConnect.Domain edge).

**Move 2 — namespace patches** + DI relocation in PaymentsModule.cs (similar to W5.4.d.2 CommunicationsModule.cs).

T-triggers: T1+T3+T6+T7. S-class: S2 (mutator round-trip) + S3 (log silence) + S1 (list query).

### 4.4.d.3 — Cut edges + ArchTest Rules

1. REMOVE `<ProjectReference LankaConnect.Domain>` from `LankaConnect.Application.csproj` if the only edge was the legacy Payments code. (Likely still needed for non-Payments code; verify with build.)
2. ADD ArchTest Rule 5 `LegacyApplication_DoesNotDependOnPaymentsDomain` in `LayeringRules.cs`.
3. **NO Rule 6** `LegacyDomain_DoesNotDependOnPaymentsDomain` under Option A — Payments.Domain still depends on LankaConnect.Domain (the inverse direction) for the legacy RefundRequest type. If Option B was chosen, Rule 6 lands here.
4. The 4 Rule docstrings should explain the Option A scope decision so future-me doesn't re-litigate it.

T-triggers: T6. Tests: the new ArchTest fact(s) ARE the tests. S-class: none.

---

## ProjectReference graph (final state — Option A)

```
Payments.Contracts.csproj
  -> BuildingBlocks.Contracts                              (existing)

Payments.Domain.csproj
  -> LankaConnect.Domain                                   (NEW edge — transitional, IRefundRequestRepository returns LankaConnect.Domain.Events.Entities.RefundRequest)
  -> BuildingBlocks.Abstractions                           (existing)

Payments.Application.csproj
  -> Payments.Contracts                                    (NEW edge — 4.4.b)
  -> Payments.Domain                                       (NEW edge — 4.4.b)
  -> BuildingBlocks.Application                            (existing)
  -> LankaConnect.Application                              (transitional, 4.4.b — ICommandHandler/IQueryHandler/IUnitOfWork/ICurrentUserService)

Payments.Infrastructure.csproj
  -> Payments.Application                                  (NEW edge — 4.4.d.2)
  -> Payments.Domain                                       (NEW edge — 4.4.d.2)
  -> LankaConnect.Infrastructure                           (transitional — Repository<T> base + AppDbContext share, mirrors 5.4.d.2)

LankaConnect.Application.csproj
  -> Payments.Contracts                                    (NEW edge — 4.4.d.1)
  -> Payments.Domain                                       NEVER ADDED — IPaymentQueries / IPaymentCommands cover all read+mutator needs

LankaConnect.Infrastructure.csproj
  -> Payments.Domain                                       (NEW direct edge — 4.4.d.2, mirror of 5.4.d.2)
```

---

## Contract surface (concrete — pending architect ratification at 4.4.a)

```csharp
public interface IPaymentQueries
{
    // Refund request reads
    Task<RefundRequestSummaryDto?> GetRefundRequestByIdAsync(Guid id, CancellationToken ct = default);
    Task<RefundRequestDetailDto?> GetRefundRequestWithLineItemsAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<RefundRequestSummaryDto>> GetByRegistrationAsync(Guid registrationId, CancellationToken ct = default);
    Task<RefundRequestSummaryDto?> GetMyMostRecentForEventAsync(Guid eventId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<RefundRequestSummaryDto>> ListByEventAsync(Guid eventId, RefundRequestStatusDto? statusFilter, CancellationToken ct = default);
    Task<IReadOnlyList<RefundRequestSummaryDto>> ListStuckApprovedAsync(DateTime olderThanUtc, CancellationToken ct = default);

    // Stripe customer reads (cross-module — User aggregate consumes these)
    Task<string?> GetStripeCustomerIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> HasStripeCustomerAsync(Guid userId, CancellationToken ct = default);

    // Webhook idempotency probe (cross-module — webhook plumbing in legacy API layer)
    Task<bool> IsWebhookEventProcessedAsync(string stripeEventId, CancellationToken ct = default);
}

public interface IPaymentCommands
{
    // Populated at 4.4.d.1 based on actual consumer audit. Likely thin —
    // most refund mutations stay inside Payments.Application via MediatR.
    // The cross-module mutator surface here is for OOS callers like
    // EventCancellationEmailJob (which currently calls RefundExecutionService
    // directly across the module boundary).
}
```

**Deliberately omitted**: `GetByLineItemReferenceId` family of methods. Those are internal-to-Payments lookup helpers used only inside `RefundLineDispatcher.cs`. Keep them on `IRefundRequestRepository` (which is internal-to-Payments after 4.4.d.2) rather than exposing them through the cross-module Contracts surface.

---

## Risks (architect-flagged)

### Risk #1 (LOAD-BEARING) — Registration.List&lt;RefundRequest&gt; typed nav

**Found**: `src/LankaConnect.Domain/Events/Registration.cs:104` declares `private readonly List<RefundRequest> _refundRequests = new();` with `IReadOnlyList<RefundRequest> RefundRequests` accessor on line 105. This is the SAME class of cross-aggregate typed-nav pattern that blocked Wave 4.1.2 (Event.List&lt;EmailGroup&gt;) and forced the 5.4.c.0 + 5.4.d.1b junction CLR-type surgery.

**Key difference**: RefundRequest is a CHILD entity within the Registration aggregate, not a sibling aggregate root. The typed nav is the CORRECT DDD pattern for a parent owning its children. The problem only arises if RefundRequest physically moves OUT of `LankaConnect.Domain` — at which point either (a) Registration would still need to type-reference Payments.Domain.RefundRequest (a backwards module dependency), or (b) the relationship has to be re-modeled.

**Three options**:

- **Option A — keep RefundRequest in LankaConnect.Domain.Events (RECOMMENDED)**. RefundRequest + RefundRequestLineItem + RegistrationPayment STAY in `src/LankaConnect.Domain/Events/Entities/` as Registration aggregate children. `Payments.Domain.Repositories.IRefundRequestRepository` returns the legacy `LankaConnect.Domain.Events.Entities.RefundRequest` type via a transitional Payments.Domain → LankaConnect.Domain ProjectReference (mirrors W5.4.d.2 Communications.Domain → LankaConnect.Domain transitional edge). Cross-module Payments access goes through `IPaymentQueries`. Zero EF migrations needed. Zero risk of the W5.4.c.0/5.4.d.1b runtime InvalidOperationException class of bug. **The Application + Infrastructure layer cuts still happen** (ArchTest Rule 5), so we still get the structural improvement. The architectural compromise: Payments.Domain depends on LankaConnect.Domain (inverse of the usual capability-references-shared direction). This is the same compromise W5.4 made, and it works.

- **Option B — move RefundRequest + RefundRequestLineItem + RegistrationPayment to Payments.Domain**. Requires 4.4.c.0 surgery: replace Registration.`_refundRequests: List<RefundRequest>` with `_refundRequestLinks: List<RegistrationRefundRequestLink>` (junction CLR with raw Guid). EF snapshot rebaseline migration. ~12 new unit tests. Net wall-clock +1 day vs Option A. The win: ArchTest Rule 6 `LegacyDomain_DoesNotDependOnPaymentsDomain` lands (mirrors W5.4.d.3 Rule 4). The risk: Registration.cs has 10+ methods that operate on the typed nav (`AddRefundRequest`, `WithdrawRefundRequest`, etc., lines 1138-1309). Refactoring them to the junction pattern is heavier than W5.4.d.1b Newsletter surgery was, AND there's a behavioral semantics question — the Registration aggregate currently enforces invariants like "max one active refund per registration" via the typed collection. The junction pattern would force those invariants into a query against `IRefundRequestRepository` from inside the Registration aggregate (which it must not do — aggregates don't query repositories). This may be a hard architectural blocker.

- **Option C — defer Wave 4.4 until after the Phase A architect re-think**. The [[project_phase_a_v5_wave_plan]] memory notes that "Plan v5 Amendment" is the current authoritative ordering. If the architect determines that Refund/Registration is one of the cases where the Capability/Product split needs additional design work (the `Products/LankaEvents` vs `Capabilities/Payments` boundary is unclear when one aggregate root owns children that belong to two different capability domains), then 4.4 should pause until Wave 5 Products carve-out lands and the boundary is firm.

**Recommended path: Option A.** Reasons: (1) the structural win (ArchTest Rule 5 + handler cohesion in Payments.Application) is mostly achieved without the entity move; (2) Option B's aggregate-invariant problem looks load-bearing and may not have a clean resolution; (3) Option C delays by ~3-4 weeks, which is excessive for a ruling that may swing either way.

**ARCHITECT INPUT NEEDED before 4.4.a kicks off.**

### Risk #2 — `IStripePaymentService` interface scope

The 817-LOC `IStripePaymentService` interface in `src/LankaConnect.Application/Common/Interfaces/` is consumed by 8+ Events command handlers (checkout flows for collection/donation/sponsor/add-on/RSVP). It's a true cross-module port.

**Question**: does it stay in `LankaConnect.Application.Common.Interfaces` (the port stays in the legacy layer; only the adapter `StripePaymentService` moves to Payments.Infrastructure), or does it physically relocate to `Payments.Contracts` (cross-module-consumable surface) or `Payments.Application` (Payments-internal port)?

**Wave precedent**:
- W5.3 (Forms) kept `IFormQueries` / `IFormCommands` in `Forms.Contracts`. No equivalent of "big interface used everywhere" existed.
- W5.4 (Communications) ditto — `IEmailGroupQueries` in `Communications.Contracts`, `IEmailService` (the SMTP port equivalent) stayed in `LankaConnect.Application` because it's consumed by many capability modules.

**Recommended path**: `IStripePaymentService` is the Payments equivalent of `IEmailService` — a fundamental port consumed across many capabilities. It STAYS in `LankaConnect.Application.Common.Interfaces`. The implementation `StripePaymentService.cs` moves to `Payments.Infrastructure` in 4.4.d.2. ArchTest Rule 5 explicitly excludes `LankaConnect.Application.Common.Interfaces` from the ban (similar to how W5.4.d.3 didn't ban LankaConnect.Application from depending on Communications types that stayed in shared interfaces).

**ARCHITECT INPUT NEEDED before 4.4.c.4 (services move).**

### Risk #3 — Billing aggregate (`CulturalIntelligenceBilling.cs` + supporting types) is OUT OF SCOPE

The 1,776 LOC under `src/LankaConnect.Domain/Billing/` is SaaS-tier billing (the organizer pays LankaConnect for cultural intelligence features), NOT event-checkout payment. Different domain, different lifecycle, different stakeholders. Including it in Wave 4.4 would balloon the scope by 2-3 days and introduce a second aggregate move (CulturalIntelligenceBilling is its own aggregate root).

**Recommendation**: defer to a future `Wave 4.X Billing.Contracts` sub-wave (architect-numbered) OR fold into the Wave 5 Products carve-out (since "who pays whom for what" is partly a Products-layer question).

**No architect input needed** — flag for future planning.

### Risk #4 — `StripeWebhookHandler.cs` lives in `src/LankaConnect.Application/Billing/` despite being NOT-billing

The file path is wrong: `StripeWebhookHandler.cs` handles all Stripe webhook events (payment_intent.succeeded, charge.refunded, etc.), not billing-tier webhooks. The current path is a legacy historical artifact. 4.4.d.2 fixes this by moving it to `src/Modules/Payments/Payments.Application/Webhooks/`.

**No architect input needed** — silent fix during 4.4.d.2.

### Risk #5 — same `dbContext.Set<>` bypass class of bug as W5.4.d.4

Per `[[feedback_read_side_bypass_audit]]`, the 4.4.c.5 gap-verification grep MUST include all 5 Payments-related table names (refund_requests, refund_request_line_items, registration_payments, stripe_customers, stripe_webhook_events) across BOTH `src/LankaConnect.Application/` AND `src/Modules/`. Any read-side handler that bypasses the aggregate and queries the junction table directly would survive unit tests but throw at runtime.

**No architect input needed** — encoded in the 4.4.c.5 sub-phase definition.

---

## Implementation checklist (next-session resumption)

- [ ] **Pre-flight (NEW)**: architect consult on Risk #1 (Option A vs B vs C) and Risk #2 (IStripePaymentService location). Cannot start 4.4.a without these two rulings.
- [ ] **4.4.a**: define Payments.Contracts surface + Contracts.Tests + ArchTest rule (`Modules_Payments_Contracts_DependsOnlyOnBuildingBlocksContracts`)
- [ ] **4.4.b**: implement PaymentQueries in Payments.Application + DI + ~7 query tests
- [ ] **4.4.c.0**: SKIPPED under Option A. Under Option B: Registration._refundRequests typed-nav surgery + RegistrationRefundRequestLink + EF mapping flip + EF-snapshot rebaseline migration + unit tests + S3+S5 smoke.
- [ ] **4.4.c.1**: move 7 command handlers into Payments.Application
- [ ] **4.4.c.2**: move 2 query handlers + RefundRequest DTOs into Payments.Application
- [ ] **4.4.c.3**: move 12 event handlers into Payments.Application
- [ ] **4.4.c.4**: move 6 service implementations into Payments.Application + DI relocation
- [ ] **4.4.c.5**: gap-verification grep (including dbContext.Set bypass per `[[feedback_read_side_bypass_audit]]`), document or split
- [ ] **4.4.d.1**: swap read-side cross-module consumers to IPaymentQueries; populate IPaymentCommands surface based on consumer audit
- [ ] **4.4.d.2**: physical move of Stripe repos + RefundRequestRepository + StripeWebhookHandler (NOT the RefundRequest entity under Option A); namespace patch; Payments DI wire-up
- [ ] **4.4.d.3**: cut LankaConnect.Application → LankaConnect.Domain.Payments edge + ArchTest Rule 5 (LegacyApplication_DoesNotDependOnPaymentsDomain). Rule 6 only if Option B chosen.

**Pre-flight before 4.4.a**:
1. Confirm Risk #1 + Risk #2 resolutions with architect.
2. Grep `src/LankaConnect.Application/` for ALL imports of `RefundRequest`, `StripeCustomer`, `StripeWebhookEvent`, `RegistrationPayment` types to cross-check the 4.4.c.x scope numbers in this plan.
3. Grep `dbContext.Set<` for the 5 payment-related table names (per `[[feedback_read_side_bypass_audit]]`) so the 4.4.c.5 gap audit knows what to look for upfront.

---

## Why labeled `Wave 4.4` (not `Wave 5.5`)

Per `[[feedback_wave_numbering_correction]]` (2026-06-23): the shipped commits labeled `Wave5.3.*` (Forms) and `Wave5.4.*` (Communications) are mis-labeled relative to the authoritative Phase A master plan. They ARE the master plan's Wave 4.3 (Forms) and Wave 4.1 (Communications) capability extractions, just numbered wrong at the per-wave-doc level. This doc adopts the master-plan numbering — `Wave 4.4` for Payments — so commit messages, ArchTest docstrings, and per-wave files stay aligned with the source of truth going forward.
