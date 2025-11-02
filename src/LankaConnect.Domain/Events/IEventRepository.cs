using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events;

public interface IEventRepository : IRepository<Event>
{
    Task<IReadOnlyList<Event>> GetByOrganizerAsync(Guid organizerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Event>> GetUpcomingEventsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Event>> GetEventsByStatusAsync(EventStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Event>> GetEventsWithAvailableCapacityAsync(CancellationToken cancellationToken = default);
    Task<Event?> GetWithRegistrationsAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Event>> GetPublishedEventsAsync(CancellationToken cancellationToken = default);

    // Location-based queries (Epic 2 Phase 1 - PostGIS spatial queries)
    Task<IReadOnlyList<Event>> GetEventsByRadiusAsync(decimal latitude, decimal longitude, double radiusMiles, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Event>> GetEventsByCityAsync(string city, string? state = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Event>> GetNearestEventsAsync(decimal latitude, decimal longitude, int maxResults = 10, CancellationToken cancellationToken = default);
}