# ADR-007: IAuditable + AuditableInterceptor Pattern

| | |
|---|---|
| **Status** | Accepted (2026-06-04) |
| **Date** | 2026-06-04 |
| **Decision Owner** | Niroshana (Founder/Architect) |
| **Reviewers** | system-architect agent |
| **Supersedes** | Legacy `BaseEntity.MarkAsUpdated()` manual audit pattern |
| **Related** | [ENTERPRISE_ARCHITECTURE_BLUEPRINT.md](./ENTERPRISE_ARCHITECTURE_BLUEPRINT.md) §2.D1; ADR-006 (5-layer topology) |

## Context

Legacy `LankaConnect.Domain.Common.BaseEntity` provides `Guid Id`, `DateTime CreatedAt`, `DateTime UpdatedAt`, and a `MarkAsUpdated()` method that the developer must call on every mutation. 79 entities derive from it; 64 call sites invoke `MarkAsUpdated()`. Problems:

1. Easy to forget calling `MarkAsUpdated()` → silent staleness in audit fields
2. `Id = Guid.NewGuid()` in base constructor is `protected set` — mutable, fragile
3. Forces audit-field columns on EVERY entity even when not audited (e.g., `OutboxMessage` semantics conflict — `ProcessedAt` is status, not audit)
4. Cannot be elevated to `BuildingBlocks.Domain.Entity<TId>` without breaking 200+ call sites

`BuildingBlocks.Domain.Entity<TId>` (shipped W2.3) is generic, `protected init` Id (immutable), no audit fields. The reconciliation is non-trivial.

## Decision

Adopt the **interface + EF interceptor** pattern:

1. **Interfaces in `BuildingBlocks.Domain`** (already shipped W2.5; refinements added per W1B):
   ```csharp
   public interface IAuditable {
       DateTime CreatedAt { get; set; }
       string? CreatedBy { get; set; }
       DateTime UpdatedAt { get; set; }
       string? UpdatedBy { get; set; }
   }

   public interface ISoftDeletable {
       bool IsDeleted { get; set; }
       DateTime? DeletedAt { get; set; }
       string? DeletedBy { get; set; }
   }

   // NEW per ADR-007
   public interface IConcurrencyToken {
       byte[] RowVersion { get; set; }
   }

   // NEW per ADR-007 (enforcement of ADR-002 tenancy)
   public interface IMultiTenant<TTenantId> {
       TTenantId TenantId { get; }
   }
   ```

2. **`AuditableEntityInterceptor` in `BuildingBlocks.Infrastructure`** (already in `BaseDbContext`): hooks `SaveChangesAsync`; for any tracked entity implementing `IAuditable`, sets `CreatedAt`/`CreatedBy` on Added and `UpdatedAt`/`UpdatedBy` on Modified. Uses `IClock` and `IUserContext` (from `BuildingBlocks.Abstractions` + `SharedKernel.Identity`).

3. **`MarkAsUpdated()` is DELETED**, not preserved as a no-op. No-ops are footguns; the interceptor handles it.

4. **Soft-delete is opt-in per aggregate root**. Not all entities are soft-delete candidates — `OutboxMessage.ProcessedAt` and `Notification.IsRead` are STATUS, not audit. Forcing `ISoftDeletable` on these corrupts semantics.

5. **`IConcurrencyToken` for optimistic concurrency** on entities with mutable critical state: Payments (charge transitions), SeatHold, Inventory (Phase 3 Commerce). Without this, phantom double-charges under load.

6. **`IMultiTenant<StorefrontId>` enforces ADR-002 tenancy**. `BaseDbContext` auto-applies a query filter for any entity implementing this interface. The previous "ArchTest enforces HasQueryFilter" guidance was hand-waving; this interface makes it mechanical.

7. **CreatedBy/UpdatedBy is `string?`** not `Guid?`. System actors are `"system"`, `"migration:Phase6A.148"`, `"webhook:stripe"` etc., not user GUIDs.

## ArchTest Rule Specification

```csharp
[Fact]
public void Auditable_Entities_DeclareInterface() { /* any entity with CreatedAt property must implement IAuditable */ }

[Fact]
public void Soft_Delete_Is_Opt_In() { /* any entity with IsDeleted property must implement ISoftDeletable */ }

[Fact]
public void MultiTenant_Entities_HaveQueryFilter() {
    // boot each Capability DbContext; for every IMultiTenant<T> entity in Model.GetEntityTypes(),
    // assert IQueryFilter != null
}

[Fact]
public void Concurrency_Token_For_Mutable_State() {
    // entities with Status enum mutations OR balance/amount property updates
    // must implement IConcurrencyToken
}
```

## Migration Path (Wave 3)

Per blueprint §3 Wave 3:

1. W3A — Notification pilot (1 entity) validates the mechanical sed pattern
2. W3B — User aggregate (high-risk, auth-critical)
3. W3C — Events batch 1 (Event, EventPass, TicketTier — highest traffic)
4. W3D — Events batch 2 (29 entities — mechanical)
5. W3E — Communications entities (15)
6. W3F — All remaining entities (Business, Enterprise, etc.)
7. W3G — DELETE `LankaConnect.Domain.Common.BaseEntity` — final cleanup

**Per entity**: change base class `: BaseEntity` → `: Entity<Guid>, IAuditable`; remove `Id = Guid.NewGuid()` from base constructor (handled by Entity ctor or factory); remove `MarkAsUpdated()` calls (interceptor handles).

**EF Core configuration verify**: `entity.Property(e => e.CreatedAt)` still works when CreatedAt comes from `IAuditable` interface; test EF migration generation per batch.

## Consequences

### Positive

- Audit semantics centralized; impossible to forget
- Domain code cleaner (no `MarkAsUpdated()` boilerplate)
- Future entities get audit for free by implementing `IAuditable`
- Multi-tenant query filter mechanism enforced, not hoped-for
- Optimistic concurrency available where needed (Payments)

### Negative / Trade-offs

- One-time 79-entity refactor (Wave 3 — 2 weeks)
- 64 `MarkAsUpdated()` call sites need removal
- Interceptor adds ~1ms to SaveChangesAsync (acceptable)

### Risks

- Risk: EF Core config compatibility — Property mapping when prop comes from interface → mitigated by per-batch EF migration generation test
- Risk: User aggregate refactor breaks auth → mitigated by W3B dedicated session + full auth test pass before next batch
- Risk: Tenancy query filter regression → mitigated by per-Capability ArchTest assertion (boots DbContext, reflects Model)

## Status Update Log

- 2026-06-04: Accepted by founder. Interceptor pattern already 70% shipped in W2.5; Wave 1B adds IConcurrencyToken + IMultiTenant<T>; Wave 3 executes the 79-entity migration.
