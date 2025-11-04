using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Analytics;

/// <summary>
/// Repository interface for EventAnalytics aggregate
/// Provides methods for analytics data access and unique viewer tracking
/// </summary>
public interface IEventAnalyticsRepository : IRepository<EventAnalytics>
{
    /// <summary>
    /// Get analytics for a specific event
    /// </summary>
    Task<EventAnalytics?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a view should be counted (deduplication logic)
    /// Returns true if this view should be counted, false if it's a duplicate within the window
    /// </summary>
    Task<bool> ShouldCountViewAsync(
        Guid eventId,
        Guid? userId,
        string ipAddress,
        TimeSpan deduplicationWindow,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the unique viewer count for an event
    /// Called asynchronously after view records are processed
    /// </summary>
    Task UpdateUniqueViewerCountAsync(Guid eventId, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get aggregated analytics for an organizer (all their events)
    /// Used for organizer dashboard
    /// </summary>
    Task<OrganizerDashboardData?> GetOrganizerDashboardDataAsync(
        Guid organizerId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Data transfer object for organizer dashboard
/// Contains aggregated statistics across all organizer's events
/// </summary>
public class OrganizerDashboardData
{
    public Guid OrganizerId { get; set; }
    public int TotalEvents { get; set; }
    public int TotalViews { get; set; }
    public int TotalUniqueViewers { get; set; }
    public int TotalRegistrations { get; set; }
    public decimal AverageConversionRate { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public List<EventAnalyticsSummary> TopEvents { get; set; } = new();
    public List<EventAnalyticsSummary> UpcomingEvents { get; set; } = new();
}

/// <summary>
/// Summary data for individual events in dashboard
/// </summary>
public class EventAnalyticsSummary
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int Views { get; set; }
    public int Registrations { get; set; }
    public decimal ConversionRate { get; set; }
}
