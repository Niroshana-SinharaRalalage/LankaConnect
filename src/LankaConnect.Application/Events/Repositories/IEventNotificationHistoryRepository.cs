using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Application.Events.Repositories;

/// <summary>
/// Phase 6A.61: Repository for tracking manual event email notifications
/// Used in Communication tab history display
/// </summary>
public interface IEventNotificationHistoryRepository
{
    /// <summary>
    /// Gets a notification history record by ID
    /// </summary>
    Task<EventNotificationHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all notification history records for an event, ordered by sent_at DESC
    /// </summary>
    Task<List<EventNotificationHistory>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new notification history record
    /// </summary>
    Task<EventNotificationHistory> AddAsync(EventNotificationHistory history, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing notification history record (for statistics updates)
    /// </summary>
    void Update(EventNotificationHistory history);
}
