namespace LankaConnect.Domain.Common;

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
    // IAuditable members — populated by BaseDbContext.AuditableInterceptor.
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Default ctor — mirrors legacy <see cref="BaseEntity"/> behavior by
    /// initializing <c>Id = Guid.NewGuid()</c>. EF Core calls this for
    /// materialization and then overwrites Id from the persisted row.
    /// </summary>
    protected LegacyBaseEntity()
    {
        Id = System.Guid.NewGuid();
    }

    /// <summary>Explicit-id ctor for callers that pre-generate Ids.</summary>
    protected LegacyBaseEntity(System.Guid id) : base(id)
    {
    }

    /// <summary>
    /// Backward-compat method for callers that used legacy
    /// <c>BaseEntity.GetDomainEvents()</c>. New code reads
    /// <see cref="LankaConnect.BuildingBlocks.Domain.Entity{TId}.DomainEvents"/>.
    /// </summary>
    public IReadOnlyList<LankaConnect.BuildingBlocks.Domain.IDomainEvent> GetDomainEvents() => DomainEvents;
}
