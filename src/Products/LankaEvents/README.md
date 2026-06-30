# LankaEvents — Product Module

> First product in the LankaConnect platform. Lives in
> `src/Products/LankaEvents/` per the 5-layer enterprise architecture
> (`BuildingBlocks → SharedKernel → Capabilities → Products → Hosts`).

## Carve-out status (Wave 5 SHIPPED 2026-06-29)

| Sub-wave | Scope | Status |
|---|---|---|
| **Wave 5.0** | 5-csproj skeleton: `Domain` / `Application` / `Infrastructure` / `Api` / `AssemblyMarker` | ✅ SHIPPED `916aab0b` (2026-06-26) |
| **Wave 5.1** | Event-family Domain move (Event aggregate + 30+ sub-aggregates + value objects + cultural files purge) | ✅ SHIPPED `47e14ef9` + `59ed4483` |
| **Wave 5.2** | Application layer carve-out (~458 files: 225 Commands + 101 Queries + handlers + services + cross-cutting stragglers) | ✅ SHIPPED across 10 sub-commits |
| **Wave 5.3** | Infrastructure Repositories carve-out (18 of 20 Event-family repos) | ✅ SHIPPED 5 sub-slices (a1/a2/b/c1/c2); STAGING-VERIFIED 2026-06-29 |
| **Wave 5.4** | Final Repository moves (EventAnalytics + EventViewRecord interfaces + impls) — completes the 20-of-20 repo carve-out | ✅ SHIPPED `ae50fb27` + `c82ddce1` |
| **Wave 5.5.a** | ArchTest rules formalizing the Products dependency boundary | ✅ SHIPPED `d64fb3a5` |
| **Wave 5.5.b** | Stale `LankaConnect.Domain.Events` comment cleanup | ✅ SHIPPED `9e7a37ce` |
| **Wave 5.5.c** | Module docs (THIS file) + MEMORY entry | ⏳ IN-PROGRESS |
| **Wave 5.5.d** | Wave 6.5 deferral annotation (architect-consult-gated) | ⏳ PENDING |

After Wave 5.5.d ships: **Wave 5 CLOSED.**

## Project layout

```
src/Products/LankaEvents/
├── LankaEvents.Domain/                 # Event aggregate + 30+ sub-aggregates + value objects
│   ├── AssemblyMarker.cs
│   ├── Event.cs                        # Event aggregate root (953 LOC; moved Wave 5.1)
│   ├── Registration.cs                 # Registration aggregate (moved Wave 5.1)
│   ├── ... (Sponsor / Ticket / TicketTier / Donation / etc.)
│   ├── Repositories/                   # Domain-side repository INTERFACES
│   │   ├── IEventAnalyticsRepository.cs    # moved Wave 5.4.a
│   │   ├── IEventViewRecordRepository.cs   # moved Wave 5.4.a
│   │   ├── IAddOnDefinitionRepository.cs
│   │   ├── ... (~20 interfaces)
│   └── Services/                       # Domain services (IGeoLocationService etc.)
│
├── LankaEvents.Application/            # CQRS handlers + queries + services
│   ├── AssemblyMarker.cs
│   ├── Commands/                       # 225 commands (moved Wave 5.2)
│   ├── Queries/                        # 101 queries (moved Wave 5.2)
│   ├── EventHandlers/                  # MediatR INotificationHandler implementations
│   ├── BackgroundJobs/                 # Quartz/Hangfire job classes
│   └── Services/                       # ILayoutAuthorizationService, IStructuralEditGuard, etc.
│
├── LankaEvents.Infrastructure/         # EF Core repositories + integrations
│   ├── AssemblyMarker.cs
│   └── Repositories/                   # 20 repository implementations (Wave 5.3 + 5.4.b)
│       ├── EventRepository.cs          # 953 LOC; canonical AsSplitQuery + Include patterns
│       ├── RegistrationRepository.cs   # canonical S2 mutator-pattern lifecycle (RSVP)
│       ├── ... (18 more)
│
└── LankaEvents.Api/                    # ASP.NET Core composition / DI
    ├── AssemblyMarker.cs
    └── LankaEventsModule.cs            # AddLankaEventsModule() extension method
```

## Dependency boundary (enforced by ArchTest)

`tests/architecture/LankaConnect.ArchitectureTests/ProductsLayerRules.cs` defines 8 rules
(see [Wave 5.5.a architect consult](../../../docs/architect-consults/2026-06-29-platform-plan-hierarchy.md)
for full breakdown):

- **Rule 1** — `Products.LankaEvents.Domain` depends only on `BuildingBlocks.*` +
  `SharedKernel.*` + `Scheduling.Domain` + transitional `LankaConnect.Domain`.
  No MediatR, no EF Core, no ASP.NET, no capability internals.
- **Rule 2** — `Products.LankaEvents.Infrastructure` transitional dep on legacy is
  namespace-scoped to `LankaConnect.Infrastructure.Data` + `.Data.Repositories` only.
- **Rule 3-5** — legacy `LankaConnect.*` does not back-reference Products (one
  exception: `DependencyInjection.cs` composition root).
- **Rule 6** — every Products class with a legacy transitional dep carries
  `[Wave6_5TransitionalException(...)]`. 20 classes decorated.
- **Rule 8** — Clean Architecture invariant (`Application` does not reference `Infrastructure`).
- **Rule 9** — other capability modules reach Products only via Domain interfaces
  (SKIPPED with debt tracking; Wave 6 cleanup).

## Wave 6.5 carryover (scoped deferral — NOT technical debt)

**Scoped to Wave 6.5 per architect ruling 2026-06-29 alongside Outbox cutover. Not a
pre-Phase-B blocker.** Four items were intentionally left out of Wave 5's scope:

The W5.0 transitional ProjectReference from `Products.LankaEvents.Infrastructure` to
`LankaConnect.Infrastructure` remains in place; 20 repository classes still depend on
legacy `AppDbContext` + `Repository<T>` base. Each carries
`[Wave6_5TransitionalException("...")]` for grep-able audit. Wave 6.5 cuts the
dependency alongside Outbox cutover work:
- `LankaEventsDbContext` extracted into `Products.LankaEvents.Infrastructure`
- All 20 repos rewired against the new context
- EF Configurations moved alongside (currently still in `LankaConnect.Infrastructure/Data/Configurations/`
  because the project-reference cycle prevented earlier movement — see
  [Wave 5.4 architect consult](../../../docs/architect-consults/2026-06-29-platform-plan-hierarchy.md))
- Cross-schema FKs (`events.registrations.user_id → identity.users.id`) policy decision

Wave 6 also resolves the two Skip-fact ArchTest rules (5 + 9) per
[MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md §"Wave 6 debt-tracking entries"](../../../docs/MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md).

## Testing

Per-controller smoke for EventsController lives at
`scripts/smoke/Smoke-EventsController.ps1`. Run via
`pwsh ./scripts/smoke/Run-Wave9a.ps1` (orchestrator). Wave 9.a (foundation + Events
smoke) gates W5.3 STAGING-VERIFIED + Wave 5 resumption per Wave 5 closeout discipline.

Application-layer unit tests for moved handlers live alongside the relocated code in
`tests/LankaConnect.Application.Tests/Analytics/` etc. (still under the legacy test
project; per-Capability test split is a Wave 6+ cleanup).
