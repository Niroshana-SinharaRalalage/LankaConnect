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

    // Background job queries (Epic 2 Phase 5 - Hangfire)
    Task<IReadOnlyList<Event>> GetEventsStartingInTimeWindowAsync(DateTime startTime, DateTime endTime, EventStatus[] statuses, CancellationToken cancellationToken = default);

    // Full-text search (Epic 2 - Full-Text Search)
    /// <summary>
    /// Search events using PostgreSQL full-text search with ranking
    /// Returns matching events ordered by relevance and pagination info
    /// </summary>
    Task<(IReadOnlyList<Event> Events, int TotalCount)> SearchAsync(
        string searchTerm,
        int limit,
        int offset,
        EventCategory? category = null,
        bool? isFreeOnly = null,
        DateTime? startDateFrom = null,
        CancellationToken cancellationToken = default);

    // Badge cleanup queries (Phase 6A.27)
    /// <summary>
    /// Gets all events that have a specific badge assigned
    /// Used by ExpiredBadgeCleanupJob to remove expired badges from events
    /// </summary>
    Task<IReadOnlyList<Event>> GetEventsWithBadgeAsync(Guid badgeId, CancellationToken cancellationToken = default);

    // Badge cleanup queries (Phase 6A.28)
    /// <summary>
    /// Gets all events that have at least one expired badge assignment (EventBadge.ExpiresAt &lt; now)
    /// Used by ExpiredBadgeCleanupJob to clean up expired EventBadge assignments
    /// </summary>
    Task<IReadOnlyList<Event>> GetEventsWithExpiredBadgesAsync(CancellationToken cancellationToken = default);

    // EF Core change tracking helpers (Session 33: Group Pricing Tier Update Fix)
    /// <summary>
    /// Explicitly marks the Pricing property as modified for EF Core change tracking
    /// Required for JSONB-stored owned entities that may not be automatically detected as modified
    /// </summary>
    void MarkPricingAsModified(Event @event);
}