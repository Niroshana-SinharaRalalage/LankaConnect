using LankaConnect.Domain.Analytics;
namespace LankaConnect.Products.LankaEvents.Domain.Repositories;

/// <summary>
/// Repository interface for EventViewRecord entity
/// Handles detailed view tracking for unique viewer calculations
/// </summary>
public interface IEventViewRecordRepository
{
    /// <summary>
    /// Add a new view record
    /// </summary>
    Task AddAsync(EventViewRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unique viewer count for an event
    /// Counts distinct user_id (for authenticated) or ip_address (for anonymous)
    /// </summary>
    Task<int> GetUniqueViewerCountAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a view exists within a time window (for deduplication)
    /// </summary>
    Task<bool> ViewExistsInWindowAsync(
        Guid eventId,
        Guid? userId,
        string ipAddress,
        DateTime windowStart,
        CancellationToken cancellationToken = default);
}

// EventViewRecord entity now lives at src/LankaConnect.Domain/Analytics/EventViewRecord.cs
// (Wave 5.4.a: only the interface moved; entity stays in LankaConnect.Domain.Analytics)
