using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Domain.Entities;

/// <summary>
/// Polymorphic junction between a <see cref="TicketTier"/> and either a <see cref="VenueZone"/>
/// or a <see cref="VenueTable"/>. Replaces the direct <c>VenueZone.TicketTierId</c> foreign key so
/// that a single tier can be mapped to both theater zones and banquet tables without duplicating the model.
/// Composite primary key: (TierId, AssignableKind, AssignableId).
/// </summary>
public class TierAssignment
{
    /// <summary>FK to the ticket tier that owns this assignment.</summary>
    public Guid TierId { get; private set; }

    /// <summary>Discriminator: whether AssignableId points at a VenueZone or a VenueTable row.</summary>
    public AssignableKind AssignableKind { get; private set; }

    /// <summary>Polymorphic FK (no DB-level FK because the target table varies by kind).</summary>
    public Guid AssignableId { get; private set; }

    /// <summary>Creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    private TierAssignment() { }

    private TierAssignment(Guid tierId, AssignableKind assignableKind, Guid assignableId)
    {
        TierId = tierId;
        AssignableKind = assignableKind;
        AssignableId = assignableId;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Creates a new tier assignment after validating both identifiers.</summary>
    public static Result<TierAssignment> Create(Guid tierId, AssignableKind kind, Guid assignableId)
    {
        if (tierId == Guid.Empty)
            return Result<TierAssignment>.Failure("Tier ID is required");

        if (assignableId == Guid.Empty)
            return Result<TierAssignment>.Failure("Assignable ID is required");

        return Result<TierAssignment>.Success(new TierAssignment(tierId, kind, assignableId));
    }
}
