# LankaConnect — Enterprise Modular Monolith Architecture Blueprint

**Full Picture, Founder-Approved, Zero Carried Debt**

| | |
|---|---|
| **Author** | System Architect (Opus 4.7) |
| **Date** | 2026-06-04 |
| **Status** | Founder-Approved 2026-06-04 |
| **Scope** | Replaces "Hybrid C → B-lite" pragmatic path |
| **Audience** | Founder, future contributors, future-you who needs to add LankaSeyla in 6 months without architectural regret |
| **Related ADRs** | ADR-006 (Layer Topology), ADR-007 (IAuditable + Interceptor), ADR-008 (Cultural in SharedKernel), ADR-009 (Outbox-Everything), ADR-010 (Repository-per-Aggregate) |

---

## EXECUTIVE SUMMARY

The founder's instinct to reject the pragmatic Hybrid path was **architecturally correct**. The pragmatic path was a tactical compromise that bought 2-3 weeks at the cost of:

1. **Permanent ambiguity** about where new code goes (legacy `LankaConnect.Domain` vs new modules)
2. **Repeated rediscovery** of the same cross-cutting types every time a new module extracts (Cultural blocked Communications; Money blocked Payments; Identity will block every module)
3. **Deferred debt that compounds** — the W4.1.2 Communications failure (BLOCKED on cross-cutting Cultural types) is the textbook outcome of the pragmatic path. It will repeat at W7 Events, W10 Identity, and EVERY Phase 2+ module.

The plan below buys you **+3 to +5 weeks of explicit untangling work today** in exchange for **NEVER paying a re-architecture tax again** when adding LankaSeyla, LankaMart, LankaHomes, LankaTemples, LankaBusiness, LankaNivasa.

**Bottom line**: 25 weeks total Phase A (vs current 20-week plan), but the curve goes asymptotic at 0 marginal cost per future product module — exactly what an enterprise architecture should do.

---

## 1. TARGET ARCHITECTURE — THE FINAL TOPOLOGY

### 1.1 The 5-Layer Model

LankaConnect uses 5 architectural layers — `BuildingBlocks`, `SharedKernel`, `Capabilities`, `Products`, `Hosts` — each with clear ownership and explicit dependency rules.

```
src/
├── BuildingBlocks/                           # LAYER 1: Framework primitives (ZERO domain knowledge)
│   ├── BuildingBlocks.Abstractions/          # NEW: pure interfaces, no impl. Breaks the BBs.Domain↔BBs.Application cycle
│   ├── BuildingBlocks.Domain/                # Entity<TId>, ValueObject, IAuditable, ISoftDeletable, IDomainEvent, Guard, Result/Maybe/Error
│   ├── BuildingBlocks.Application/           # MediatR pipeline behaviors, ICommand/IQuery, IUnitOfWork, IIdempotencyStore, IOutbox, IAuditLogger
│   ├── BuildingBlocks.Infrastructure/        # BaseDbContext, OutboxProcessor, AuditableInterceptor, JSONB helpers, generic Repository<T>
│   ├── BuildingBlocks.Web/                   # ApiController base, ProblemDetails, Telemetry, FeatureFlags, JWT, RateLimit, HealthChecks
│   ├── BuildingBlocks.Contracts/             # IntegrationEventBase, IIntegrationEventV1, IIntegrationEventDispatcher (cross-module wire format)
│   └── BuildingBlocks.Testing/               # NEW: shared test fixtures (TestcontainersFixture, FakeCurrentActor, FakeOutbox)
│
├── SharedKernel/                             # LAYER 2: LankaConnect-specific cross-domain primitives (NO behavior, just types)
│   ├── SharedKernel.Cultural/                # CulturalContext, SriLankanLanguage, GeographicRegion, ReligiousContext, CulturalBackground, CulturalProfile, ICulturalCalendar
│   ├── SharedKernel.Money/                   # Money, Currency (graduated from BBs.Domain — see D6)
│   ├── SharedKernel.Locale/                  # Locale, Country, LocalizedString<T>
│   ├── SharedKernel.Identity/                # UserId, TenantId, StorefrontId, OrganizationId (TYPED IDS only, NOT the User aggregate)
│   ├── SharedKernel.Geo/                     # Address, GeoCoordinate, MetroAreaId, TimeZoneRegion
│   ├── SharedKernel.Time/                    # DateRange, IClock, ISystemClock, ITimeZoneService
│   └── SharedKernel.Contracts/               # SharedKernel-level integration events (e.g., LocalizationChangedV1)
│
├── Capabilities/                             # LAYER 3: Reusable infrastructure modules (the "8 capabilities")
│   ├── Identity/                             # User aggregate, auth, RBAC, JWT issuance
│   ├── Notifications/                        # In-app notification surface
│   ├── Communications/                       # Email, SMS, WhatsApp, Newsletter (delivery channel)
│   ├── Media/                                # Photos, videos, albums, thumbnails (storage + delivery)
│   ├── Forms/                                # Form definition + submission (owner-agnostic)
│   ├── Payments/                             # Stripe abstraction, refunds, invoices, webhooks
│   ├── Scheduling/                           # NEW (was "Events"): generic scheduling — start/end, recurrence, RSVP, capacity
│   └── CulturalIntelligence/                 # NEW: impls of ICulturalCalendar etc.; external integrations (Google Calendar, religious feeds)
│
├── Products/                                 # LAYER 4: Business products (compose Capabilities)
│   ├── LankaEvents/                          # Events aggregate, ticket tiers, sponsors, venue layouts (Events-specific domain)
│   ├── LankaSeyla/                           # Phase 3 — Commerce-clothing
│   ├── LankaMart/                            # Phase 3
│   ├── LankaNivasa/                          # Phase 3
│   ├── LankaBusiness/                        # Phase 2 — Directory
│   ├── LankaHomes/                           # Phase 2 — Real estate
│   ├── LankaTemples/                         # Phase 2 — Community/religious
│   └── _Shared/                              # Cross-product types ONLY (e.g., StorefrontKind enum) — rarely populated
│
├── Hosts/                                    # LAYER 5: Composition roots
│   ├── Host.AllInOne/                        # Phase A target — single container, all Capabilities + Products
│   ├── Host.Api/                             # Future split: API-only worker
│   ├── Host.Worker/                          # Future split: OutboxProcessor + background jobs
│   └── Host.PerCapability/                   # Future microservice extraction (Identity-only host, etc.)
│
└── (legacy LankaConnect.{Domain,Application,Infrastructure,API,Shared} — progressively emptied to ZERO files, then deleted)
```

### 1.2 Why 5 layers, not 4 — Capabilities vs Products distinction

**The problem with a 4-layer model**: it forces you to choose between "Events is a Module" (then where does LankaEvents-specific code like ticket tiers live?) OR "Events is a Product" (then where does the reusable scheduling primitive go that LankaTemples will need for puja schedules and LankaSeyla will need for flash-sale windows?).

**The 5-layer solution**:

| Layer | Question it answers | Example |
|---|---|---|
| **Capabilities** | What infrastructure does ANY product need? | Scheduling, Payments, Identity |
| **Products** | What business outcome does THIS product deliver? | LankaEvents tickets, LankaSeyla cart |

**Concrete decomposition of the current "Events" module**:

- `Capabilities/Scheduling` — `ScheduledOccurrence`, `RecurrenceRule`, `RSVP`, `CapacityRule`, `WaitlistPolicy` (LankaEvents uses for events, LankaTemples uses for puja schedules, LankaSeyla uses for flash-sale slots)
- `Capabilities/Forms` — `FormDefinition`, `FormResponse` (LankaEvents uses for signup commitments, LankaHomes uses for inquiry forms, LankaBusiness uses for business profile forms)
- `Products/LankaEvents` — `Event` aggregate, `EventPass`, `TicketTier`, `Sponsor`, `VenueLayout`, `SignUpList` (Events-SPECIFIC; never reused by another product)

This decomposition unblocks the founder's vision: LankaTemples needs scheduling (poya days, religious events) but it does NOT need EventPass or TicketTier. The pragmatic plan jams them together into one "Events" module, then LankaTemples has to either (a) take the whole Events module as a dep (architectural malpractice) or (b) duplicate scheduling primitives (the very anti-pattern explicitly forbidden by the founder).

### 1.3 Dependency direction (THE NON-NEGOTIABLE RULE)

```
                    BuildingBlocks ◄────────── SharedKernel
                          ▲                          ▲
                          │                          │
                          ├──────────┬───────────────┤
                          │          │               │
                     Capabilities ◄────── Products
                          │          │               │
                          └──────────┴───────────────┘
                                     ▲
                                     │
                                   Hosts
```

**Rules** (enforced by NetArchTest in Wave 6):

| Layer | MAY reference | MUST NOT reference |
|---|---|---|
| BuildingBlocks.* | (nothing within LankaConnect) | Anything |
| SharedKernel.* | BuildingBlocks.* | Capabilities.*, Products.*, Hosts.*, LankaConnect.* |
| Capabilities.X.* | BuildingBlocks.*, SharedKernel.*, **Capabilities.Y.Contracts** (other capability's CONTRACTS ONLY) | Capabilities.Y.Domain/Application/Infrastructure (only Contracts), Products.*, Hosts.*, LankaConnect.* |
| Products.X.* | BuildingBlocks.*, SharedKernel.*, **Capabilities.*.Contracts** | Products.Y.*, Hosts.*, LankaConnect.* |
| Hosts.* | Everything below | (no constraint — composition root) |

**The critical rule**: Capabilities reference each other ONLY via Contracts, never Domain/Application/Infrastructure. This is how LankaSeyla can ship without breaking LankaEvents, and how Identity can swap auth providers without coupling every other capability.

---

## 2. DESIGN DECISIONS — D1 through D10

### D1: BaseEntity → Entity<TId> reconciliation — VALIDATED with refinements

**Pattern**: Add `IAuditable` + `ISoftDeletable` interfaces. Interceptor in `BuildingBlocks.Infrastructure`. `MarkAsUpdated()` is DELETED entirely (not a no-op — a no-op is a footgun).

**Already shipped (W2.5)**: the interfaces are in place; `BaseDbContext` already does the right thing.

**Refinements / additions**:

1. **CreatedBy/UpdatedBy is `string?`, NOT `Guid?`** — system actors are `"system"`, `"migration:Phase6A.148"`, not GUIDs. Already correct in current `IAuditable.cs`.

2. **Soft-delete is OPT-IN per aggregate root**, not blanket. Reason: `Notification.IsRead` and `OutboxMessage.ProcessedAt` are NOT soft-delete; they're status. Forcing soft-delete on `OutboxMessage` would corrupt outbox semantics. ✅ Current `ISoftDeletable` is opt-in. Keep it.

3. **NEW interface: `IConcurrencyToken`** for entities needing optimistic concurrency:
   ```csharp
   public interface IConcurrencyToken { byte[] RowVersion { get; set; } }
   ```
   Required for: Payments (charge state transitions), SeatHold (concurrent seat reservation), Inventory (Phase 3 Commerce). Without this, phantom double-charges under load.

4. **NEW interface: `IMultiTenant<TTenantId>`** for entities that carry `StorefrontId` (per ADR-002):
   ```csharp
   public interface IMultiTenant<TTenantId> { TTenantId TenantId { get; } }
   ```
   `BaseDbContext` auto-applies query filter for `IMultiTenant<StorefrontId>`. This is the enforcement mechanism for ADR-002 that the ADR currently leaves as "ArchTest enforces" hand-waving. Without it, every new Commerce entity is a potential cross-tenant data leak.

5. **DELETE `MarkAsUpdated()` entirely** — Wave 3 sweeps 64 call sites and removes the method. No no-op preserved.

6. **Migration path for 79 entities**: current `BaseEntity.Id` is `protected set`; `Entity<TId>` is `protected init`. Subclasses use the parameterless `protected Entity()` ctor and set Id in their public factory. Mechanical but tested per batch.

### D2: Cultural in SharedKernel — VALIDATED, scope BIGGER than initially estimated

**Pattern**: Cultural is cross-cutting domain. Promote to `SharedKernel.Cultural`. All consumers reference it.

**True scope from grep**: **410 cross-module references** including migrations; **54 production code references** outside Communications. The `SharedKernel.Cultural` package contains:

**Value Objects** (move from `LankaConnect.Domain.Communications.ValueObjects/`):
- CulturalContext, CulturalEvent, CulturalAppropriateness, CulturalConflict, CulturalProfile, CulturalCalendarSync, CulturalTimingPreference, CrossCulturalEvent, DiasporaCommunityProfile, DiasporaRelevance, MultilingualContent, MultilingualDescription, RecipientCulturalProfile, MultiCulturalCommunity, MultiCulturalSupporting, GoogleCalendarCulturalEvent, TempleScheduleIntegration

**Enums** (move from `LankaConnect.Domain.Common.Enums/`):
- SriLankanLanguage (rename from SouthAsianLanguage), GeographicRegion, CulturalDataType, CulturalEventType, CulturalIntelligenceBackupStatus, DiasporaEngagementType

**Enums** (from `LankaConnect.Domain.Shared.Enums/Types/`):
- CulturalBackground, ReligiousContext, CulturalPriority

**Services** (interface only): `ICulturalCalendarService`, `ICulturalAppropriatenessChecker`. **Implementations live in `Capabilities/CulturalIntelligence`** (NEW capability). Reason: implementations need external API queries (Google Calendar, religious calendar feeds) — that's behavior, not a primitive.

**Namespace**: `LankaConnect.SharedKernel.Cultural`, NOT `LankaConnect.BuildingBlocks.Cultural`. Cultural is LankaConnect-business-specific (it knows about SriLankanLanguage); BuildingBlocks is framework-agnostic.

### D3: Enum partition by audience — VALIDATED with concrete partition map

| Current location | Enum | New home | Rationale |
|---|---|---|---|
| `Domain/Common/Enums/SouthAsianLanguage.cs` | SriLankanLanguage (renamed) | `SharedKernel.Cultural` | Cultural primitive |
| `Domain/Common/Enums/GeographicRegion.cs` | GeographicRegion | `SharedKernel.Cultural` | Cultural primitive |
| `Domain/Common/Enums/ContentType.cs` | ContentType | `Capabilities/Media.Domain` | Module-internal |
| `Domain/Common/Enums/CulturalDataType.cs` | CulturalDataType | `SharedKernel.Cultural` | Cultural primitive |
| `Domain/Common/Enums/CulturalEventType.cs` | CulturalEventType | `SharedKernel.Cultural` | Cultural primitive |
| `Domain/Common/Enums/CulturalIntelligenceBackupStatus.cs` | (status of backup) | `Capabilities/CulturalIntelligence` | Service-internal |
| `Domain/Common/Enums/DiasporaEngagementType.cs` | DiasporaEngagementType | `SharedKernel.Cultural` | Cultural primitive |
| `Domain/Common/Enums/EnterpriseContractTier.cs` | EnterpriseContractTier | `Capabilities/Payments.Domain` | Billing-specific |
| `Domain/Common/Enums/ClientSegment.cs` | ClientSegment | `Capabilities/Identity.Domain` | User-classification |
| `Domain/Common/Enums/SubscriptionTier.cs` | SubscriptionTier | `Capabilities/Payments.Domain` | Billing |
| `Domain/Common/Enums/PerformanceObjective.cs` | (unused) | **DELETE** — Phase 6A monitoring artifact, dead code | Audit + remove |
| `Domain/Common/Enums/SynchronizationPriority.cs` | SynchronizationPriority | `Capabilities/CulturalIntelligence` | Service-internal |
| `Domain/Common/Enums/ReportFormat.cs` (DUPE!) | ReportFormat | **DELETE** — duplicate with `Domain/Common/ValueObjects/ReportFormat.cs` | Resolve duplicate first |
| `Domain/Shared/Enums/Currency.cs` | Currency (enum) | **DELETE** — replaced by `SharedKernel.Money.Currency` value object | Already partial in BB.Domain |

**Note on ReportFormat duplicate**: exists as BOTH enum and class with the same name in different folders. Existing bug that Wave 3 must fix.

### D4 (NEW): The Repository pattern decision

**Problem**: Every existing entity has `IXxxRepository` in `Domain` and `XxxRepository : Repository<Xxx>` in `Infrastructure`. The base `Repository<T>` lives in `LankaConnect.Infrastructure` and is THE blocking dep for every module extraction (W3.4 transitional debt).

**Decision: Repository-per-aggregate with surgical exceptions** (see ADR-010).

- Wave 1 introduces `IAggregateRepository<TAggregate, TId>` marker interface (NO methods) in `BuildingBlocks.Application`.
- Wave 4 module extractions write hand-rolled per-aggregate repositories with NAMED query methods (`FindByEventIdAsync`, `FindPendingByStatusAsync` — NOT generic `FindAsync(predicate)`).
- Legacy `Repository<T>` continues to work during Waves 2-3 and is deleted in Wave 4 alongside module migrations.
- For cross-cutting concerns where operations are TRULY generic (Outbox, Idempotency), expose as services (`IOutbox`, `IIdempotencyStore`) — already done.

**Rationale**: `IRepository<T>` with generic `FindAsync(predicate)` violates aggregate boundaries (callers query across aggregates). Current `Repository<T>.GetAll()` is used in 60+ places — many should be specific queries. DDD canonical guidance: one repository per aggregate root, hand-written, returns aggregates ONLY.

### D5 (NEW — CRITICAL PRODUCT IMPACT): DbContext-per-module + Outbox-everything

**The problem**: A user registers for an event today, which atomically (a) inserts into `events.registrations`, (b) creates a `payments.payment_intents`, (c) enqueues a `communications.outbox` entry, (d) creates a `notifications.notifications` row. All in ONE `AppDbContext.SaveChangesAsync`. After modular extraction, this is FOUR DbContexts.

**Decision: Outbox-everything pattern** (see ADR-009).

- A handler in `LankaEvents.Application.RegisterForEventCommandHandler` writes to `events.registrations` AND `events.outbox` in ONE transaction.
- The Events outbox processor publishes `RegistrationCreatedIntegrationEventV1`.
- Communications, Payments, Notifications modules subscribe and handle the event in their OWN transactions.
- If Communications fails to send the welcome email, it retries; if dead-lettered, alert.
- The event is the source of truth; cross-module consistency is **eventual** (typically <1 second).

**Critical implication — UI/UX**: Wording changes from "your registration is confirmed and email is sent" to "your registration is confirmed; you'll receive a confirmation email shortly."

**Founder accepts**: eventual consistency across modules. This is industry-standard for modular monoliths (Vaughn Vernon, Microservices Patterns) and required if microservice extraction is ever needed.

### D6 (NEW): Money in SharedKernel vs BuildingBlocks

**Current state**: `Money` and `Currency` are in `BuildingBlocks.Domain` (per W2.3b).

**Problem**: BuildingBlocks is supposed to be ZERO business knowledge, but `Money` enforces the registry of LankaConnect's 7 supported currencies (USD, LKR, INR, GBP, EUR, AUD, CAD). That's a business decision.

**Decision**: **Move Money + Currency from BuildingBlocks.Domain to SharedKernel.Money** in Wave 1E. The Money TYPE is generic; the SUPPORTED CURRENCIES are business-specific. Splitting is the right enterprise pattern.

Small move: 3 files + ~28 namespace updates.

### D7 (NEW — most important): The Identity-everywhere problem

**Every module needs `UserId`. If Identity owns User aggregate, every other module ends up referencing Identity.** Fix:

| Project | Contents |
|---|---|
| `SharedKernel.Identity` | `UserId`, `TenantId`, `StorefrontId`, `OrganizationId` (TYPED IDS only); `ClaimsPrincipal` extensions; `IUserContext` (returns current UserId) |
| `Capabilities/Identity` | `User` aggregate, `Role`, `Permission`, `AuthorizationPolicy`, JWT issuance, OAuth providers, password reset workflows |
| `Capabilities/Identity.Contracts` | `IUserDirectory` (read-only: get user name/email/avatar by UserId), `UserRegisteredIntegrationEventV1`, `UserDeletedIntegrationEventV1` |

**Result**: Every other Capability references `SharedKernel.Identity` for typed `UserId` and uses `IUserContext.CurrentUserId`. NO other Capability references `Capabilities/Identity` directly — they go through `Capabilities/Identity.Contracts.IUserDirectory` for "give me user display name" queries.

**This is how Identity ships LAST (Wave 4 final canary) without blocking anyone.**

### D8 (NEW): Frontend mirrors backend layering

Mirror the backend layering on the frontend:

```
web/
├── packages/
│   ├── @lankaconnect/ui/                     # BuildingBlocks-equivalent: design system primitives
│   ├── @lankaconnect/shared-kernel/          # Types: Money, Locale, UserId mirrors of backend
│   ├── @lankaconnect/auth/                   # Capability: authStore, ProtectedRoute, JWT refresh
│   ├── @lankaconnect/api-client-core/        # Capability: axios + interceptors
│   ├── @lankaconnect/api-client-events/      # Capability-specific generated client
│   ├── @lankaconnect/api-client-payments/    # ditto
│   ├── @lankaconnect/feature-flags/          # Capability: useFlag hook
│   ├── @lankaconnect/formatters/             # SharedKernel-equivalent: formatMoney, formatDate, formatPhone
│   └── @lankaconnect/feature-events/         # Product: event-specific components
├── apps/
│   ├── lankaconnect-web/                     # Current Next.js app (Product composition root)
│   └── (future) lankaseyla-web/              # Phase 3 product app
```

ESLint rule: feature packages can NEVER import from other feature packages (use API contracts or events). Mirrors the backend `Capabilities.X.Domain` isolation rule.

### D9 (NEW): Delete the Specification pattern

Current code uses `ISpecification<T>` (located at `Domain/Common/ISpecification.cs`). This is a code-smell in modular monoliths because specifications encode QUERY LOGIC that should be in the module's `XxxRepository.FindByXxxAsync` methods.

**Decision**: Delete `ISpecification<T>` in Wave 4 as part of repository rewrite. Specifications get inlined as named repository methods. Cleaner, faster, easier to optimize.

### D10 (NEW): Domain Event vs Integration Event boundary

`IDomainEvent` (in-process, same module, raised by aggregate) vs `IIntegrationEventV1` (cross-module, via outbox). Current code has 60+ `IDomainEvent` implementations that are ACTUALLY used for cross-module signaling (e.g., `UserRegistered` triggers email send across module boundary). These need conversion to integration events in Wave 4.

**Rule** enforced in ArchTest:
- `IDomainEvent` handlers may ONLY live in the same module that raises them
- Cross-module signaling MUST use `IIntegrationEventV1` + outbox

---

## 3. EXECUTION WAVES — Zero-Deferred-Cleanup Sequence

All waves complete the cleanup they introduce within the wave — no "we'll fix it later."

### Wave 0 — Architecture Ratification (1 week, 5 sessions)

| Day | Output |
|---|---|
| W0.1 | This document committed as `docs/architecture/ENTERPRISE_ARCHITECTURE_BLUEPRINT.md` |
| W0.2 | ADR-006 (Layer Topology) + ADR-007 (IAuditable + Interceptor) + ADR-008 (Cultural in SharedKernel) + ADR-009 (Outbox-everything) + ADR-010 (Repository-per-aggregate) drafted |
| W0.3 | Founder review session; resolutions logged in each ADR (DONE 2026-06-04 for D1-D10 approval) |
| W0.4 | Master TODO rewritten with new wave numbering; existing W4-W19 reorganized |
| W0.5 | Communications W4.1 BLOCKED status formally moved to Wave 4 (after the untangling waves complete) |

**Gate to exit Wave 0**: founder signs every ADR. **DO NOT START WAVE 1 until this is done.**

### Wave 1 — BuildingBlocks Completion + SharedKernel Skeleton (2 weeks, 10 sessions)

Goal: every primitive a module COULD need is in BuildingBlocks or SharedKernel, with tests, before ANY module is touched.

| Sub-wave | Days | Output |
|---|---|---|
| W1A — BuildingBlocks.Abstractions (NEW) | 2 | New csproj. Move `IUnitOfWork`, `ICommand`, `IQuery`, `IIdempotentCommand`, `IIdempotencyStore`, `IOutbox`, `ICurrentActor`, `IAuditLogger` + extract `IIntegrationEventBuffer` from OutboxBehavior here. `BuildingBlocks.Application` references it (impls stay where they are). **Rationale (corrected from "breaks the cycle" — no cycle exists)**: any consumer (module, Infrastructure adapter, ArchTest fixture, mock, lightweight worker like OutboxProcessor host, future Phase B product) can depend on the *contract surface* without inheriting MediatR + FluentValidation + Microsoft.Extensions.Logging.Abstractions purely to type-reference `IUnitOfWork` or `ICurrentActor`. Cheap now (8 files, namespace preserved); expensive to retrofit once 4+ modules and 3+ Phase B products transitively bind to the fat package set. Namespace stays `LankaConnect.BuildingBlocks.Application.Abstractions` (deliberate assembly-vs-namespace mismatch — zero source churn in Behaviors, modules, and W3.4 cutover). |
| W1B — IConcurrencyToken + IMultiTenant<T> | 1 | New interfaces in BuildingBlocks.Domain. `BaseDbContext` handles ConcurrencyToken via standard EF; MultiTenant via auto-applied query filter. Unit tests. |
| W1C — IAggregateRepository<TAggregate, TId> marker | 1 | New empty marker in BuildingBlocks.Application. ArchTest: every concrete repository in modules must implement this. |
| W1D — SharedKernel skeleton projects | 2 | All 7 SharedKernel csprojs created with `AssemblyMarker.cs` placeholders. ArchTest rules added. Build green. |
| W1E — Move Money + Currency from BB.Domain to SharedKernel.Money | 1 | Per D6. Update namespaces (28 callers in current code). |
| W1F — Move Locale + Country from BB.Domain to SharedKernel.Locale | 1 | Same pattern. |
| W1G — IClock + IUserContext + cleanup | 1 | `IClock` in BuildingBlocks.Abstractions (replaces `DateTime.UtcNow` direct calls in BaseDbContext); `IUserContext` in SharedKernel.Identity (replaces `ICurrentActor`-but-typed-as-UserId). Composition root impl in Hosts.AllInOne. |
| W1H — ArchTest expansion | 1 | All new rules wired. Currently 9 rules; will be ~20 by end of W1. |

**Gate to exit Wave 1**: zero new code in `LankaConnect.{Domain,Application,Infrastructure}`. Notifications module's transitional debt to `LankaConnect.Domain` (per W3.2) is CUT. ArchTest for Notifications passes WITHOUT the relaxation rule.

### Wave 2 — SharedKernel.Cultural Untangling (2 weeks, 10 sessions)

This is the BIG one. The W4.1 Communications BLOCKED status is the empirical proof this must happen FIRST.

| Sub-wave | Days | Output |
|---|---|---|
| W2A — Cultural type inventory | 1 | Per the playbook command. Produce `docs/architecture/cultural-type-inventory.md` listing all 54 cross-cutting types + their 410 reference sites. |
| W2B — SharedKernel.Cultural project + skeleton | 1 | Csproj + namespace + AssemblyMarker. ArchTest rule added: cannot reference `LankaConnect.*` or `Capabilities.*`. |
| W2C — Move Cultural Enums | 2 | SriLankanLanguage, GeographicRegion, CulturalDataType, CulturalEventType, DiasporaEngagementType, CulturalBackground, ReligiousContext, CulturalPriority. ~120 caller updates. |
| W2D — Move Cultural ValueObjects | 3 | 17 VOs from `LankaConnect.Domain.Communications.ValueObjects` to `SharedKernel.Cultural`. Update 290+ caller files via sed. |
| W2E — Move Cultural Services interfaces | 1 | `ICulturalCalendarService`, `ICulturalAppropriatenessChecker` (interfaces only). Implementations remain in legacy `LankaConnect.Infrastructure` for now — extracted to `Capabilities/CulturalIntelligence` in Wave 4. |
| W2F — Delete duplicate types | 1 | `ReportFormat` (enum + class duplicate); dead `PerformanceObjective`; the `Domain/Shared/Currency.cs` enum (replaced by SharedKernel.Money.Currency). Tighten the gap. |
| W2G — Verify Communications can extract | 1 | Re-run the playbook diagnostic: count cross-module references from `LankaConnect.Domain.Communications/*` (excluding SharedKernel types). Must be ZERO. If non-zero, additional untangling required. |

**Gate to exit Wave 2**: re-run W4.1.2 dry-run. Must succeed (Communications domain moveable with zero cycles). If not, identify the new tangle and untangle BEFORE proceeding.

### Wave 3 — Entity Migration to BuildingBlocks.Entity<TId> (2 weeks, 10 sessions)

| Sub-wave | Days | Output |
|---|---|---|
| W3A — Migration script + Notification pilot | 1 | Pilot Notification (only 1 entity in module). Script converts `: BaseEntity` to `: Entity<Guid>, IAuditable` + removes constructor `Id = Guid.NewGuid()` + removes `MarkAsUpdated()` calls. Validate end-to-end. |
| W3B — User aggregate migration | 1 | User is high-risk (auth depends on it). Migrate; full auth test suite must pass. |
| W3C — Events aggregate batch 1 (Event, EventPass, TicketTier) | 2 | Highest-traffic entities. Carefully. |
| W3D — Events aggregate batch 2 (29 entities — SignUpList, Seat, Form, AlbumPhoto, etc.) | 2 | Mechanical. Per-aggregate-root unit test after each. |
| W3E — Communications entities (15) | 1 | Per playbook pattern. |
| W3F — All remaining entities (Business 22, Enterprise 25, etc.) | 2 | Mechanical sweep. Per-aggregate testing. |
| W3G — Delete `LankaConnect.Domain.Common.BaseEntity` | 1 | Final cleanup. Zero compilation errors. Full test suite green. |

**Reality check**: 79 entities × ~30 minutes per (including testing) = ~40 hours = 5 days. Plus 5 days for the high-risk entities (User, Event, Payments) that need extra care. **10 sessions is realistic.** The mechanical work isn't the problem; the EF Core configuration adjustments are.

**Specific risk**: EF Core configurations currently use `entity.Property(e => e.CreatedAt)` — when CreatedAt moves to interface, the configuration STILL works but with subtle conversion issues. Test EF migration generation per batch.

### Wave 4 — Capability Extractions (5 weeks, 25 sessions)

NOW the playbook actually works end-to-end with no transitional debt. Per-capability:

| Capability | Sessions | Notes |
|---|---|---|
| Notifications cleanup (retire transitional edges from W3.4) | 1 | Already 90% done. Cut `LankaConnect.Application` and `LankaConnect.Infrastructure` edges. |
| Communications | 4 | Big. Per playbook. Now unblocked (SharedKernel.Cultural exists). |
| Media | 2 | Small. |
| Forms | 2 | Generalize from Events-coupled to owner-agnostic. |
| Payments | 3 | Real money + dual-context (legacy Stripe wrapper + new abstraction). |
| Scheduling (extracted from Events) | 3 | NEW capability — pull reusable schedule/recurrence/RSVP/capacity primitives out of current Events module. |
| Identity | 3 | LAST. User aggregate + auth. Most callers. |
| CulturalIntelligence (NEW) | 2 | Move impls from `LankaConnect.Infrastructure` of `ICulturalCalendarService` etc. |
| Cross-cutting cleanup | 5 | DELETE `LankaConnect.Domain.Common.*` (now empty); migrate `LankaConnect.Application.*` to per-capability Application; same for Infrastructure. |

**Risks to watch in Wave 4**:
- **Cross-aggregate transaction expectations**: existing handlers assume single-DbContext atomicity. The D5 Outbox-everything decision forces refactor of multi-aggregate handlers (e.g., `RegisterForEventCommandHandler` currently saves Registration + Payment + Email + Notification atomically; post-extraction saves Registration + outbox events).
- **Test data setup**: integration tests currently build a full `AppDbContext` state; post-extraction need multi-DbContext setup. New BuildingBlocks.Testing helper required.
- **EF Migration history coexistence**: each module has its own `__EFMigrationsHistory` in its own schema. Cross-module data integrity (e.g., FK from `events.registrations.user_id` to `identity.users.id`) requires careful migration ordering — the FK is a CROSS-SCHEMA FK that's only legal because they're in the same Postgres database during the modular monolith phase. Phase B microservice extraction loses this FK and replaces it with eventual consistency via outbox.

### Wave 5 — Product Layer Carve-Out (1 week, 5 sessions)

| Session | Output |
|---|---|
| W5A | Create `src/Products/LankaEvents/` skeleton |
| W5B | Move Events-SPECIFIC types (Event aggregate, EventPass, TicketTier, Sponsor, VenueLayout) from `Capabilities/Scheduling` to `Products/LankaEvents` |
| W5C | Move LankaEvents-specific application handlers/controllers |
| W5D | Wire LankaEvents to depend on `Capabilities/{Scheduling, Communications, Media, Payments, Identity}.Contracts` |
| W5E | ArchTest rules for Products layer enforced |

### Wave 6 — ArchTest Hardening + Documentation (1 week, 5 sessions)

| Session | Output |
|---|---|
| W6A | All ArchTest rules consolidated into 4 classes: `LayeringRules.cs`, `ModuleBoundaryRules.cs`, `MoneyDecimalBanRules.cs`, `MultiTenantQueryFilterRules.cs` |
| W6B | Money-decimal-ban rule (per ADR-001) implemented |
| W6C | Per-capability HasQueryFilter assertion test (per ADR-002) |
| W6D | Domain-event-cross-module rule (per D10) |
| W6E | Final `ENTERPRISE_ARCHITECTURE_BLUEPRINT.md` updated with empirical lessons from Waves 1-5 |

### Wave 7 — Frontend Mirror (3 weeks)

The 73-component frontend events page split + Turborepo + feature packages — existing W11 plan, EXTENDED to mirror the backend layering per D8.

### Wave 8 — Production Cutover + Stabilization (4 weeks)

Existing W12-W19 plan applies.

---

## 4. TOTAL TIMELINE — HONEST + CONSERVATIVE

| Phase | Wave | Duration |
|---|---|---|
| Pre-flight | Done (W0-W2 + W3 + W4.1.1 + W4.2.1) | 4 weeks DONE |
| Architecture ratification | Wave 0 | 1 week |
| BuildingBlocks completion | Wave 1 | 2 weeks |
| SharedKernel.Cultural untangling | Wave 2 | 2 weeks |
| Entity migration sweep | Wave 3 | 2 weeks |
| Capability extractions | Wave 4 | 5 weeks |
| Products layer | Wave 5 | 1 week |
| ArchTest hardening | Wave 6 | 1 week |
| Frontend mirror | Wave 7 | 3 weeks |
| Production cutover + stabilization | Wave 8 | 4 weeks |
| **Total Phase A** | | **25 weeks** (vs 20-week current plan) |
| **Remaining from today** | | **21 weeks** (4 done) |

**Risk-adjusted with 20% buffer**: **30 weeks total** = 26 weeks remaining = ~6 months.

**This is the honest number.** The "pragmatic" Hybrid C path was 20 weeks **plus** the certain cost of mid-Phase-2 refactoring when adding LankaSeyla. That cost was estimated at 6-8 weeks (per the W4.1 Communications experience extrapolated to 6 more products). Net: 28-30 weeks for the pragmatic path **with debt**.

**Enterprise path is the SAME total time, with ZERO debt.**

---

## 5. ARCHTEST RULES — Complete Enforcement Matrix

These rules go into `tests/architecture/`. Each is a `[Fact]` with `[Trait("Category", "ArchTest")]`.

### Layer dependency rules (15 rules)

| Rule | What it enforces |
|---|---|
| `BuildingBlocks_Domain_DependsOnNothing` | BB.Domain refs zero LankaConnect.* |
| `BuildingBlocks_Abstractions_DependsOnNothing` | BB.Abstractions refs zero LankaConnect.* (NEW per W1A) |
| `BuildingBlocks_Application_DependsOnDomainAndAbstractionsOnly` | BB.App refs only BB.Domain + BB.Abstractions |
| `BuildingBlocks_Infrastructure_DoesNotReferenceWeb` | BB.Infra cannot ref BB.Web |
| `BuildingBlocks_Contracts_DependsOnNothing` | Cross-module ABI is leaf |
| `SharedKernel_X_DependsOnlyOnBuildingBlocks` | Each SharedKernel.* (7 packages) cannot ref Capabilities/Products/LankaConnect/other SharedKernel impls (except SharedKernel.Contracts) |
| `Capabilities_X_Domain_DependsOnlyOnBuildingBlocksAndSharedKernel` | Capability Domain layer leaf |
| `Capabilities_X_Application_DoesNotReferenceInfraOrWeb` | Standard Clean Arch |
| `Capabilities_X_Infrastructure_DoesNotReferenceWeb` | Standard Clean Arch |
| `Capabilities_X_DoesNotReferenceCapabilities_Y_Internals` | Cross-capability refs ONLY via `.Contracts` |
| `Products_X_DependsOnlyOnBuildingBlocksSharedKernelAndCapabilityContracts` | Products never reach into Capability internals |
| `Products_X_DoesNotReferenceProducts_Y` | Products are siblings, never cross-coupled |
| `Hosts_DoNotContainBusinessLogic` | Hosts are pure composition (only `AddXxx()` extension calls) |
| `Legacy_LankaConnect_X_IsEmpty` | After Wave 4, file count in legacy folders MUST be zero (CI gate) |
| `No_Cycle_Allowed` | Type cycles across layer boundaries forbidden |

### Domain-specific rules (8 rules)

| Rule | What it enforces |
|---|---|
| `Money_Decimal_Ban` | No property of type decimal/double/float matches money-name regex (per ADR-001) |
| `Aggregates_InheritFromEntity` | Every aggregate root inherits Entity<TId> + IAggregateRoot |
| `Auditable_Entities_DeclareInterface` | If entity has CreatedAt property, must implement IAuditable |
| `Soft_Delete_Is_Opt_In` | If entity has IsDeleted property, must implement ISoftDeletable |
| `MultiTenant_Entities_HaveQueryFilter` | Per-Capability DbContext: all IMultiTenant entities have HasQueryFilter (the ADR-002 enforcement) |
| `Domain_Events_Stay_In_Module` | IDomainEvent handlers and raisers must be in same Capability/Product (per D10) |
| `Integration_Events_Are_Versioned` | All IIntegrationEventV1 are sealed records named `*IntegrationEventV1` |
| `Concurrency_Token_For_Mutable_State` | Entities with `Status` enum mutations or balance updates implement IConcurrencyToken |

### Operational rules (5 rules)

| Rule | What it enforces |
|---|---|
| `Every_Capability_Has_Outbox_Table` | Per-Capability DbContext exposes DbSet<OutboxMessage> |
| `Every_Capability_Has_Idempotency_Table` | Per-Capability DbContext exposes DbSet<IdempotencyKey> |
| `Every_Capability_Has_DeadLetter_Table` | Per-Capability DbContext exposes DbSet<DeadLetterMessage> |
| `Every_Capability_DbContext_DerivesFromBaseDbContext` | All entities get audit + soft-delete + tenant filter |
| `Stripe_CheckoutRequest_HasRequiredMetadata` | Per ADR-003 — storefront_id + originating_module + customer_country present |

**Total: 28 ArchTest rules** (up from current 9). Each blocks merge in CI.

---

## 6. TRADE-OFFS — What the Founder Accepts

| Trade-off | What you give up | What you get |
|---|---|---|
| **+5 weeks of upfront work** | 5 calendar weeks of revenue-impacting feature pause | Zero re-architecture cost when adding 6 future products |
| **Outbox-everything (eventual consistency)** | "Email sent" UX wording becomes "Email queued for sending" | Module independence; future microservice extraction without code changes |
| **No `IRepository<T>` generic base** | Less code reuse in repository plumbing | Aggregate boundaries respected; queries become explicit and optimizable |
| **5 architectural layers** | Steeper learning curve for new contributors | Clear "where does this go?" answer for every type |
| **Capability ≠ Product distinction** | Initial cognitive overhead | LankaTemples can use Scheduling without taking LankaEvents as a dep |
| **ArchTest CI gate** | PRs blocked when rules violated | Architecture decay impossible without explicit override |
| **Per-Capability DbContext** | Cross-module FKs become cross-schema (less elegant SQL) | Each module independently deployable; per-module migration history |
| **SharedKernel layer** | More namespaces to navigate | Single source of truth for Cultural, Money, Locale, Identity types |
| **No `MarkAsUpdated()` no-op preservation** | One-time disruption to 64 call sites | Cleaner domain code; no footguns |
| **NetArchTest dependency** | One more package | Boundary enforcement that scales |
| **Delete dead code aggressively** (Wave 2F) | Risk of removing something needed | Smaller surface area; less ambiguity |
| **Capabilities.Identity ships LAST** | Auth changes deferred | Confidence that infrastructure modules don't break auth |

---

## 7. ADDITIONAL ENTERPRISE CONCERNS

15 aspects that will hit Phase 2 if not addressed now.

### 7.1 Domain Event vs Integration Event semantic boundary (D10 above)

Both `IDomainEvent` and `IIntegrationEventV1` exist but no rule enforces the boundary. Fix: ArchTest rule per D10.

### 7.2 Idempotency at the API boundary

W2.4 IdempotencyBehavior + W2.5b idempotency_keys tables exist, but no rule enforces that mutation endpoints REQUIRE `Idempotency-Key` HTTP header. Without this, the infrastructure exists but isn't used. Fix: middleware in `BuildingBlocks.Web` rejects POST/PUT/PATCH without the header for endpoints decorated with `[RequireIdempotency]`.

### 7.3 Telemetry conventions

W2.6b shipped Azure Monitor distro but didn't establish trace-id propagation conventions across the outbox. When `RegisterForEventCommandHandler` publishes an integration event consumed asynchronously, the consumer's logs must include the original request's correlation ID. Fix: add `TraceContext` to `IntegrationEventBase` carrying `traceparent` (W3C standard); `OutboxProcessor` injects it into Activity.Current before dispatching to handlers.

### 7.4 Schema migration ordering across capabilities

Per-capability migrations are independent, BUT cross-schema FKs (e.g., `events.registrations.user_id → identity.users.id`) require Identity's table to exist before Events' migration runs. Need an explicit `migration_ordering.md` doc and a CI check that fails if Capability X's migration references Capability Y but Y's migration isn't tagged as prerequisite. Industry pattern: each migration declares `[RequiresCapability(typeof(IdentityModule), MinVersion = "1.2.0")]`.

### 7.5 Read model / CQRS readiness

Several Capabilities have read-heavy queries that don't fit aggregate-shaped data (e.g., admin dashboards counting users-per-region). Currently these go through `AppDbContext` directly with raw SQL. After modular extraction, they have no single DbContext to query.

Fix: per-Product (NOT per-Capability) "ReadModel" projections — denormalized views built from integration events, owned by the Product. LankaEvents has a `LankaEventsReadModelContext` that subscribes to UserRegistered + RegistrationCreated + PaymentSettled events and maintains a flat view. Phase 2 work, but design the contract NOW.

### 7.6 API versioning at the Capability boundary

Currently every capability's API is `/api/{capability}/...` with no version. When Phase 2 LankaSeyla launches needing a Capabilities/Payments change, you'll break LankaEvents. Fix: every capability API exposes `/api/v1/{capability}/...` from Day 1. `BuildingBlocks.Web.ApiVersioning` already in place; just enforce its usage.

### 7.7 Per-Capability logging context

Each capability should add its capability name to every log entry: `Logger.WithProperty("capability", "Communications")`. Enables per-capability App Insights filtering. Currently this is implicit (logger category name). Make it explicit via Serilog enricher in BuildingBlocks.Web.

### 7.8 Failure semantics for cross-capability calls

Today: a synchronous in-process call from EventsHandler → EmailService either succeeds or throws. Tomorrow: an outbox-published integration event might be dead-lettered. Founder needs an explicit policy:

- Dead-letter retention: 90 days
- Dead-letter alert SLA: <15 minutes from dead-letter event
- Manual replay tooling: `POST /api/admin/dead-letter/{id}/replay`

Build the tooling in Wave 4 alongside the first real cross-capability outbox use.

### 7.9 Secret rotation surface per capability

Each Capability has its own secrets (Stripe key for Payments; ACS key for Communications; ML.NET model storage for CulturalIntelligence). Today these are global in Key Vault. Per-capability rotation policy needed; ownership matrix in `docs/operations/secret-ownership.md`. Wave 4 work.

### 7.10 Data residency / GDPR considerations

Phase 2+ products (LankaHomes, LankaTemples) may launch in regions with data residency requirements. Per-Capability database is the foundation; Phase B microservice extraction with per-region deployment is the realization. Today: ensure all PII goes through `SharedKernel.Identity.PiiRedactor` (NEW); ensure all per-Capability DbContexts can be configured for per-region connection string (already true).

### 7.11 Observability of the boundaries themselves

Add OpenTelemetry metrics for: outbox queue depth, dead-letter rate per Capability, idempotency cache hit rate, ArchTest rule violation count (should always be 0). Wave 6 work.

### 7.12 Documentation for future contributors

A `CAPABILITY_CONTRACT.md` template that every new Capability must complete:

- Aggregates owned
- Integration events published (with V1 schema)
- Integration events consumed
- Cross-capability dependencies (Contracts only)
- Schema migrations dependency order

Without this, Phase 2 contributors will make mistakes. Wave 5 work.

### 7.13 Testing strategy

**Per-Capability test projects**:

- `tests/Capabilities/X/X.Domain.Tests` — pure unit tests, no DB
- `tests/Capabilities/X/X.Application.Tests` — handler tests with mocked infra
- `tests/Capabilities/X/X.Infrastructure.Tests` — Testcontainers-backed
- `tests/Capabilities/X/X.Api.Tests` — WebApplicationFactory with module DI only

**Cross-capability test projects**:

- `tests/Integration/CrossCapability.Tests` — boots multiple capabilities; tests outbox flow
- `tests/Integration/EndToEnd.Tests` — boots all of Host.AllInOne; happy-path scenarios

**Trade-off**: more projects = slower full-build but per-capability iteration is FAST. Industry standard for modular monoliths. The per-Capability sln filter (`Capability.Notifications.slnf`) makes incremental builds <30s.

### 7.14 Microservice readiness

The topology IS microservice-ready post-Wave 5 with these caveats:

- **Identity** is the easiest to extract (already a clear capability)
- **Payments** is next-easiest (Stripe-bounded; clear contracts)
- **Events / LankaEvents** is hardest (large aggregate; many integration points)
- **Cross-schema FKs become cross-service**. Need to convert them to UserId-typed references with no FK constraint, plus a read-model in each capability that needs user display data.
- **Distributed transactions impossible across services**. Outbox-everything already addresses this for Phase A; same mechanism works post-extraction.

### 7.15 Phase A framing

Phase A is "Build the modular foundation"; Phase A.5 is "Carve LankaEvents into a Product module"; Phase B is "First new Product (e.g., LankaTemples)". This three-phase framing makes the value proposition clearer:

- Phase A delivers ZERO new customer value but enables EVERY future product
- Phase A.5 delivers refactoring LankaEvents — also no new customer value
- Phase B delivers the FIRST new product — and proves the foundation works

The current "Phase A" jams the first two together. The proposed Wave 5 (Products carve-out) is what should be Phase A.5.

**Decision**: keep them merged in this round (we've already started), but document the Wave 5 boundary explicitly. Future products are clearly "Phase B forward."

---

### 7.16 Wave 5 Outcome and Wave 6.5 Carryover

**Wave 5 SHIPPED 2026-06-29.** The Event family (1 aggregate root, 30+ sub-aggregates, ~458 Application files, 20 repository classes) now physically lives under `src/Products/LankaEvents/` across four assemblies (`Domain`, `Application`, `Infrastructure`, `Api`). DI composition runs through `LankaEventsModule.AddLankaEventsModule()`. The Products dependency boundary is ArchTest-enforced via 8 rules in `tests/architecture/LankaConnect.ArchitectureTests/ProductsLayerRules.cs` (6 hard rules + 2 architect-deferred Skip-fact entries tracked under Wave 6.X.Y / Wave 6.X.Z in the Phase A plan).

**Wave 6.5 carryover (scoped deferral, not technical debt):** four items were intentionally left out of Wave 5's scope and assigned to Wave 6.5 (Outbox cutover) by architect ruling 2026-06-29 — (a) `LankaEventsDbContext` extraction, (b) EF Configurations move from `LankaConnect.Infrastructure/Data/Configurations/` to `Products/LankaEvents.Infrastructure/Configurations/` (blocked by project-reference cycle until the DbContext is extracted first), (c) cross-schema FK policy decision (`events.registrations.user_id → identity.users.id` per §7.4 D5), and (d) the 14 + 14 boundary violators surfaced by ArchTest Rules 5 + 9. Per §7.14, this scoping aligns with microservice-readiness work, which Wave 6.5 owns. 20 transitional-dep repository classes carry the `[Wave6_5TransitionalException(reason)]` attribute (defined in `BuildingBlocks.Abstractions`) for grep-able audit. **Phase B product launches do NOT require Wave 6.5 completion** — products can launch on the transitional W5.0 setup; Outbox cutover is operational maturation, not a precondition.

**Pattern banked for Phase B:** Domain-first → Application → Infrastructure repositories (per-cluster sub-slices, leaf-most first) → final repos + interface promotion → ArchTest rules closeout. Documented in `src/Products/LankaEvents/README.md` (status table + project layout + boundary summary) and the `[[wave-5-products-carveout-complete]]` MEMORY entry. ~3 days founder-pace solo; ~2 weeks with operator UAT cadence and parallel architect-consult gates per the Wave 5 testing-discipline overlay.

---

## 8. WHAT FIRES FIRST — Immediate Next Actions

1. ✅ **Founder approval received 2026-06-04** for full enterprise plan (5 layers, 8 waves, 21 weeks remaining).
2. ⏭ **Draft ADR-006 through ADR-010** based on approved decisions.
3. ⏭ **Update Master TODO** with new wave numbering. Existing W4-W19 work survives but is renumbered into Waves 2-8.
4. ⏭ **Mark W4.1 Communications BLOCKED status as "scheduled for Wave 4"** — un-block becomes a Wave 2 deliverable.
5. ⏭ **Begin Wave 1 (BuildingBlocks completion)** in next session.

---

## 9. WHAT THIS DOCUMENT IS NOT

- A line-by-line code spec — that's the per-Wave Master TODO update
- A guarantee of zero pain — Wave 2 (Cultural untangling) WILL surface unknown couplings, just like W4.1 did
- A perfect map — Wave 4 will produce empirical lessons that revise Wave 5+ plans

This document IS the contract: **what the architecture looks like at end of Phase A, why, and what trade-offs that demands of the team today.**

---

## Appendix A — Critical Files for Implementation

The 5 files that will carry the most weight when executing this plan:

- `c:\Work\LankaConnect\docs\MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md` — re-sequence into Waves 0-8
- `c:\Work\LankaConnect\docs\architecture\MODULE_EXTRACTION_PLAYBOOK.md` — update with Capability/Product distinction; add Cultural-untangling-first as Step 0
- `c:\Work\LankaConnect\tests\architecture\LankaConnect.ArchitectureTests\LayeringRules.cs` — expand from 9 to 28 rules per §5
- `c:\Work\LankaConnect\src\BuildingBlocks\BuildingBlocks.Infrastructure\Persistence\BaseDbContext.cs` — extend for IConcurrencyToken + IMultiTenant<T> per D1/W1B
- `c:\Work\LankaConnect\src\LankaConnect.Domain\Communications\ValueObjects\CulturalContext.cs` — Wave 2's anchor type; first to move to `c:\Work\LankaConnect\src\SharedKernel\SharedKernel.Cultural\` (to be created)
