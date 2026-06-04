# ADR-006: 5-Layer Architecture Topology

| | |
|---|---|
| **Status** | Accepted (2026-06-04) |
| **Date** | 2026-06-04 |
| **Decision Owner** | Niroshana (Founder/Architect) |
| **Reviewers** | system-architect agent |
| **Supersedes** | Implicit 3-layer model (Modules / Hosts / Tests) used in v4 Master TODO |
| **Related** | [ENTERPRISE_ARCHITECTURE_BLUEPRINT.md](./ENTERPRISE_ARCHITECTURE_BLUEPRINT.md) §1; ADR-007 (IAuditable); ADR-008 (Cultural in SharedKernel); ADR-009 (Outbox-everything); ADR-010 (Repository-per-aggregate) |

## Context

The v4 Phase A plan extracted modules under `src/Modules/` but did not formalize what a "module" was. The W4.1.2 Communications extraction failed twice on cross-domain cultural type coupling — symptom of an under-specified topology. Future products (LankaSeyla, LankaMart, LankaHomes, LankaTemples, LankaBusiness, LankaNivasa) need a topology that distinguishes reusable infrastructure from product-specific code, or every new product re-discovers the same coupling problems.

## Decision

LankaConnect adopts a **5-layer architecture**:

```
src/
├── BuildingBlocks/        Layer 1 — Framework primitives (zero domain knowledge)
├── SharedKernel/          Layer 2 — LankaConnect-specific cross-domain primitives (no behavior)
├── Capabilities/          Layer 3 — Reusable infrastructure modules consumed by ALL products
├── Products/              Layer 4 — Business products that compose capabilities
└── Hosts/                 Layer 5 — Composition roots (one container for AllInOne deployment)
```

**Capability vs Product distinction (the critical addition vs simpler 4-layer models)**:

- A **Capability** is reusable infrastructure that ANY product could need: Identity, Notifications, Communications, Media, Forms, Payments, Scheduling, CulturalIntelligence.
- A **Product** is a specific business offering composed of capabilities: LankaEvents, LankaSeyla, etc.
- `Capabilities/Scheduling` owns generic `ScheduledOccurrence` / `RecurrenceRule` / `RSVP` (used by LankaEvents, LankaTemples, LankaSeyla flash sales).
- `Products/LankaEvents` owns `Event`, `EventPass`, `TicketTier`, `Sponsor` (never reused by another product).

## Dependency Rules (enforced by ArchTest in Wave 6)

| Layer | MAY reference | MUST NOT reference |
|---|---|---|
| BuildingBlocks.* | (nothing within LankaConnect) | Anything |
| SharedKernel.* | BuildingBlocks.* | Capabilities.*, Products.*, Hosts.*, LankaConnect.* (legacy) |
| Capabilities.X.* | BuildingBlocks.*, SharedKernel.*, **Capabilities.Y.Contracts** only | Other Capability Domain/Application/Infrastructure, Products.*, Hosts.* |
| Products.X.* | BuildingBlocks.*, SharedKernel.*, **Capabilities.*.Contracts** | Other Products, Hosts.*, Capability internals |
| Hosts.* | Everything below | (no constraint) |

**The critical rule**: Capabilities reference each other ONLY via Contracts. This is how LankaSeyla ships without breaking LankaEvents.

## ArchTest Rule Specification

Implemented in `tests/architecture/LankaConnect.ArchitectureTests/LayeringRules.cs`:

```csharp
[Fact]
public void Capabilities_X_DoesNotReferenceCapabilities_Y_Internals()
{
    var result = Types.InAssembly(typeof(CapabilityXMarker).Assembly)
        .Should()
        .NotHaveDependencyOnAny("LankaConnect.Capabilities.Y.Domain",
                                "LankaConnect.Capabilities.Y.Application",
                                "LankaConnect.Capabilities.Y.Infrastructure")
        .GetResult();
    Assert.True(result.IsSuccessful, string.Join(",", result.FailingTypeNames ?? Array.Empty<string>()));
}
```

15 layer-dependency rules total (see blueprint §5). Each rule is a `[Fact]` with `[Trait("Category", "ArchTest")]` and blocks PR merge in CI.

## Consequences

### Positive

- Single answer to "where does this type go?" — any contributor reading the topology can place new code correctly
- Future products (Phase B) onboarded by following the Capability/Product template without re-architecture
- Cross-capability coupling literally cannot compile (Contracts-only reference rule)
- Microservice extraction path clean: lift any Capability to its own service by replacing Contracts assembly with HTTP/gRPC client

### Negative / Trade-offs

- Steeper learning curve (5 layers vs 3)
- More projects in solution (≈40 csprojs total at end of Phase A vs ~20 today)
- Per-Capability sln filters (`Capability.Notifications.slnf`) required for fast incremental builds
- Cross-module FKs become cross-schema (less elegant SQL) — acceptable while modular monolith; replaced by integration events when microservices extract

### Risks

- Risk: contributors smuggle cross-capability internal refs by mistake → mitigated by ArchTest CI gate (28 rules)
- Risk: Products layer "carve-out" (Wave 5) reveals more LankaEvents-specific code than expected → mitigated by Wave 4 untangling work; Wave 5 is mostly mechanical move

## Migration Path

Implemented across Waves 0-6 per blueprint §3. Notifications module (already extracted W3) retrofits in Wave 1 to cut its transitional debt edge to legacy `LankaConnect.Domain`.

## Status Update Log

- 2026-06-04: Accepted by founder after architect re-evaluation of W4.1.2 BLOCKED state.
