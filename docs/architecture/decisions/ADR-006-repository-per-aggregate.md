# ADR-010: Repository-per-Aggregate Pattern (Kill Generic IRepository<T>)

| | |
|---|---|
| **Status** | Accepted (2026-06-04) |
| **Date** | 2026-06-04 |
| **Decision Owner** | Niroshana (Founder/Architect) |
| **Reviewers** | system-architect agent |
| **Supersedes** | Legacy `LankaConnect.Infrastructure.Common.Repository<T>` generic base |
| **Related** | [ENTERPRISE_ARCHITECTURE_BLUEPRINT.md](./ENTERPRISE_ARCHITECTURE_BLUEPRINT.md) §2.D4 + §2.D9; ADR-006 (5-layer topology); ADR-007 (IAuditable) |

## Context

Today's codebase has:

- `IXxxRepository` interface in `LankaConnect.Domain/{Bounded Context}/Repositories/`
- `XxxRepository : Repository<Xxx>` impl in `LankaConnect.Infrastructure/Repositories/`
- Generic base `Repository<T>` in `LankaConnect.Infrastructure.Common.Repository.cs` with `GetAll()`, `FindAsync(predicate)`, `Add()`, `Update()`, `Delete()`

Problems:

1. `Repository<T>` is a transitional-debt edge for every module extraction (W3.4 documented this for Notifications)
2. Generic `FindAsync(Expression<Func<T, bool>>)` lets callers query across aggregate boundaries with arbitrary predicates — violates DDD
3. `GetAll()` is used in 60+ places; many should be specific queries (`FindActiveAsync`, `FindPendingByOwnerAsync` etc.) and become bottlenecks at production scale
4. Specifications pattern (`ISpecification<T>` in `Domain/Common/`) was added on top — encodes query logic in the wrong layer

The DDD canonical pattern is: **one repository per aggregate root, hand-written, returns aggregates only, named query methods (no generic predicates).**

## Decision

### Wave 1 (Repository abstraction)

Introduce a **marker interface** in `BuildingBlocks.Application`:

```csharp
namespace LankaConnect.BuildingBlocks.Application.Repositories;

public interface IAggregateRepository<TAggregate, TId>
    where TAggregate : class
{
    // intentionally empty — marker only
    // concrete repositories define named query methods
}
```

ArchTest enforces: every concrete repository in modules MUST implement this marker.

### Wave 4 (Module extractions — per-Capability)

Each capability writes **hand-rolled, per-aggregate repositories with NAMED query methods**:

```csharp
// Capabilities/Events/Events.Domain/Repositories/IEventRepository.cs
public interface IEventRepository : IAggregateRepository<Event, EventId>
{
    Task<Event?> GetByIdAsync(EventId id, CancellationToken ct);
    Task<IReadOnlyList<Event>> FindPublishedByOrganizerAsync(UserId organizerId, CancellationToken ct);
    Task<IReadOnlyList<Event>> FindUpcomingByMetroAreaAsync(MetroAreaId metroId, DateRange window, CancellationToken ct);
    Task AddAsync(Event aggregate, CancellationToken ct);
    Task<bool> ExistsByCanonicalNameAsync(string canonicalName, CancellationToken ct);
}
```

**NO** generic `FindAsync(predicate)`. **NO** `GetAll()`. Every query is a named method with explicit semantics.

### Cross-cutting concerns (kept as services, NOT repositories)

For TRULY generic operations:

- `IOutbox` (already in `BuildingBlocks.Application`)
- `IIdempotencyStore` (already in `BuildingBlocks.Application`)
- `IAuditLogger` (Wave 1A)

These are SERVICES, not repositories — they have no aggregate to encapsulate.

### Wave 4 cleanup

- DELETE `LankaConnect.Infrastructure.Common.Repository<T>` after all modules migrate
- DELETE `LankaConnect.Domain.Common.ISpecification<T>` (per blueprint §2.D9 — inlined as named repository methods)

## ArchTest Rule Specification

```csharp
[Fact]
public void Every_Capability_Repository_Implements_AggregateRepository_Marker() {
    var capabilityAssemblies = GetCapabilityDomainAssemblies();
    foreach (var asm in capabilityAssemblies) {
        var repoInterfaces = Types.InAssembly(asm)
            .That().HaveNameEndingWith("Repository")
            .And().AreInterfaces()
            .GetTypes();
        foreach (var t in repoInterfaces) {
            var implementsMarker = t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAggregateRepository<,>));
            Assert.True(implementsMarker, $"{t.Name} must implement IAggregateRepository<,>");
        }
    }
}

[Fact]
public void No_Generic_FindAsync_Predicate() {
    var capabilityAssemblies = GetCapabilityInfrastructureAssemblies();
    foreach (var asm in capabilityAssemblies) {
        var methods = asm.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.Name == "FindAsync")
            .Where(m => m.GetParameters().Any(p => p.ParameterType.IsAssignableTo(typeof(Expression))));
        Assert.Empty(methods);  // no generic predicate-based FindAsync allowed
    }
}

[Fact]
public void Specification_Pattern_Is_Removed() {
    // post-Wave-4: zero references to ISpecification<T> in any Capability assembly
    var hits = Types.InAssemblies(GetAllCapabilityAssemblies())
        .That().HaveDependencyOn("LankaConnect.Domain.Common.ISpecification")
        .GetTypes();
    Assert.Empty(hits);
}
```

## Migration Path

| Wave | Action |
|---|---|
| Wave 1C | Introduce `IAggregateRepository<TAggregate, TId>` marker in `BuildingBlocks.Application` |
| Wave 4 (per-capability) | Write hand-rolled per-aggregate repositories; replace legacy `XxxRepository : Repository<Xxx>` |
| Wave 4 cleanup | DELETE `Repository<T>` base + `ISpecification<T>` after all modules migrate |

**Per-capability work**: count aggregates, define repository interface per aggregate root, name each query method explicitly, write EF Core implementation. Estimate: 1-2 days per capability (Notifications has 1 aggregate; Events has ~6).

## Consequences

### Positive

- Aggregate boundaries respected (no cross-aggregate predicate queries)
- Queries become explicit + optimizable (each method has known SQL shape; DBAs can profile)
- Per-capability repository surface is documented IN CODE (interface = contract)
- Module extractions don't carry `Repository<T>` transitional debt
- `ISpecification<T>` complexity removed

### Negative / Trade-offs

- Less plumbing code reuse (each repository hand-written; ~30-50 LOC per aggregate)
- More query methods to maintain when domain queries multiply
- Requires DDD literacy from contributors

### Risks

- Risk: contributors copy `Repository<T>.GetAll()` pattern in new capabilities → mitigated by ArchTest `Every_Capability_Repository_Implements_AggregateRepository_Marker` + `No_Generic_FindAsync_Predicate`
- Risk: missed query methods discovered late in Wave 4 → mitigated by per-aggregate audit before extraction (count current usages of the legacy `Repository<T>.FindAsync(predicate)` per aggregate)

## Status Update Log

- 2026-06-04: Accepted by founder. Wave 1C introduces marker; Wave 4 per-capability extractions implement hand-rolled repositories; Wave 4 cleanup deletes legacy `Repository<T>` + `ISpecification<T>`.
