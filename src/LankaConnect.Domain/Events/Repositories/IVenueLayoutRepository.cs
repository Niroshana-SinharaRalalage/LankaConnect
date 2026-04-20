using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Domain.Events.Repositories;

/// <summary>
/// Repository interface for VenueLayout aggregate operations.
/// VenueLayout is loaded with its zones and seats (full aggregate).
/// </summary>
public interface IVenueLayoutRepository : IRepository<VenueLayout>
{
    /// <summary>
    /// Gets a venue layout by ID with all zones and seats eagerly loaded.
    /// </summary>
    Task<VenueLayout?> GetWithZonesAndSeatsAsync(Guid layoutId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the venue layout assigned to a specific event, with zones and seats.
    /// </summary>
    Task<VenueLayout?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all reusable template layouts created by a user.
    /// </summary>
    Task<IReadOnlyList<VenueLayout>> GetTemplatesByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a layout name is already in use for the given event.
    /// </summary>
    Task<bool> NameExistsForEventAsync(string name, Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Slice 5 Chunk 4: Overrides the tracked entity's <c>RowVersion</c> OriginalValue so
    /// EF Core includes the caller-supplied <paramref name="expectedRowVersion"/> in the
    /// UPDATE WHERE clause. On mismatch, <c>SaveChangesAsync</c> throws
    /// <see cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"/>, which the
    /// handler maps to <see cref="ErrorKind.Conflict"/> → HTTP 409. The layout MUST already
    /// be tracked by the context (i.e. loaded via <see cref="GetByIdAsync"/> without
    /// AsNoTracking); otherwise the call is a no-op.
    /// </summary>
    void SetOriginalRowVersion(VenueLayout layout, uint expectedRowVersion);
}
