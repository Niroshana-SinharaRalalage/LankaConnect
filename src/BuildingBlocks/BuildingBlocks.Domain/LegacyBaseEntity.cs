using System.Diagnostics.CodeAnalysis;

namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// Transitional bridge for Wave 3 entity migrations. Inherits
/// <see cref="LankaConnect.BuildingBlocks.Domain.Entity{TId}"/> with
/// <see cref="System.Guid"/> identity, implements
/// <see cref="LankaConnect.BuildingBlocks.Domain.IAuditable"/>, and preserves the
/// legacy <see cref="BaseEntity"/> behavior of auto-initializing
/// <c>Id = Guid.NewGuid()</c> in the parameterless constructor — so 70+
/// entities can migrate via bulk sed (`: BaseEntity` → `: LegacyBaseEntity`)
/// without per-factory-ctor Id init edits.
/// </summary>
/// <remarks>
/// <para>
/// W3D/W3E/W3F bulk migration target. The pilots (W3A Notification, W3B User,
/// W3C Event/EventPass/TicketTier) derive directly from
/// <see cref="LankaConnect.BuildingBlocks.Domain.Entity{TId}"/> + <c>IAuditable</c>;
/// the remaining 70+ entities use this bridge to keep the migration mechanical.
/// </para>
/// <para>
/// Retire in W3G when all entities are off legacy <see cref="BaseEntity"/> AND
/// the codebase has standardized on either direct BB.Entity inheritance or this
/// bridge (Wave 4 capability extraction will likely polish this).
/// </para>
/// </remarks>
public abstract class LegacyBaseEntity
    : LankaConnect.BuildingBlocks.Domain.Entity<System.Guid>,
      LankaConnect.BuildingBlocks.Domain.IAuditable
{
    // IAuditable members — populated by BaseDbContext.AuditableInterceptor for
    // module-owned DbContexts. AppDbContext extends raw DbContext (not BaseDbContext),
    // so for AppDbContext-owned entities the ctor-set CreatedAt below is the
    // operational value; interceptor adoption for AppDbContext is a Wave 6.5
    // outbox-cutover concern.
    public new DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public new DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Default ctor — mirrors legacy <see cref="BaseEntity"/> behavior by
    /// initializing <c>Id = Guid.NewGuid()</c> and <c>CreatedAt = DateTime.UtcNow</c>.
    /// EF Core calls this for materialization and then overwrites both from the
    /// persisted row. For freshly-constructed entities saved through a
    /// BaseDbContext-derived module context, the AuditableInterceptor will
    /// overwrite <c>CreatedAt</c> with the injected <c>IClock.UtcNow</c> on
    /// SaveChanges; for AppDbContext (which does not run the interceptor),
    /// the ctor value below is what persists.
    /// </summary>
    /// <remarks>
    /// Transitional: LegacyBaseEntity ctor assigns Id = Guid.NewGuid() in body;
    /// this attribute annotates that truth for the C# 11 required-members analyzer
    /// (Entity&lt;T&gt;.Id is `required`). Consult #13 amendment 2026-07-06 authorised
    /// this ONE exception to Consult #12's "no [SetsRequiredMembers] anywhere" ban:
    /// scope-limited to LegacyBaseEntity, does not extend to Entity&lt;T&gt; or
    /// AggregateRoot&lt;T&gt;. Remove when LegacyBaseEntity is deleted post-Wave-6.5.
    /// See ADR-006 addendum.
    /// </remarks>
    [SetsRequiredMembers]
    protected LegacyBaseEntity()
    {
        Id = System.Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Explicit-id ctor for callers that pre-generate Ids.</summary>
    /// <remarks>
    /// See default ctor for [SetsRequiredMembers] rationale (Consult #13 amendment).
    /// </remarks>
    [SetsRequiredMembers]
    protected LegacyBaseEntity(System.Guid id) : base(id)
    {
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Backward-compat method for callers that used legacy
    /// <c>BaseEntity.GetDomainEvents()</c>. New code reads
    /// <see cref="LankaConnect.BuildingBlocks.Domain.Entity{TId}.DomainEvents"/>.
    /// </summary>
    public IReadOnlyList<LankaConnect.BuildingBlocks.Domain.Contracts.IDomainEvent> GetDomainEvents() => DomainEvents;
}
