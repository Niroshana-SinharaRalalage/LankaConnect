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
}
