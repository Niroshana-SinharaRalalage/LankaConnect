namespace LankaConnect.Domain.Analytics;

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

/// <summary>
/// Entity representing a single view of an event
/// Used for detailed tracking and unique viewer calculation
/// </summary>
public class EventViewRecord
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid? UserId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime ViewedAt { get; set; }
}
