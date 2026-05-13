namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// Opt-in marker for entities that carry audit fields populated automatically
/// by <c>BaseDbContext.SaveChangesAsync</c> in W2.5. Entities NOT implementing
/// this interface persist without audit columns.
/// </summary>
/// <remarks>
/// <para>
/// Setters are intentionally public so EF Core's change tracker can update
/// them via the interface in <c>BaseDbContext</c>. Domain code should treat
/// them as read-only (do not mutate audit fields from inside aggregate
/// behavior). The persistence boundary owns these values.
/// </para>
/// <para>
/// <see cref="CreatedBy"/> / <see cref="UpdatedBy"/> use the
/// <c>BuildingBlocks.Application.Abstractions.ICurrentActor.ActorId</c> at
/// SaveChanges time — typically the authenticated user's GUID, or
/// <c>null</c> for anonymous / system operations.
/// </para>
/// </remarks>
public interface IAuditable
{
    /// <summary>UTC timestamp when the entity row was first persisted.</summary>
    DateTime CreatedAt { get; set; }

    /// <summary>Actor (user or system principal) that created the row, or null for anonymous.</summary>
    string? CreatedBy { get; set; }

    /// <summary>UTC timestamp of the most recent update; null when the row has not been updated since creation.</summary>
    DateTime? UpdatedAt { get; set; }

    /// <summary>Actor that performed the most recent update, or null for anonymous.</summary>
    string? UpdatedBy { get; set; }
}
