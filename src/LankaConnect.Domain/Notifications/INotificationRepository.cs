using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Notifications;

/// <summary>
/// Repository interface for Notification entity
/// Phase 6A.6: Notification System
/// </summary>
public interface INotificationRepository : IRepository<Notification>
{
    /// <summary>
    /// Get all notifications for a specific user
    /// </summary>
    Task<IReadOnlyList<Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unread notifications for a specific user
    /// </summary>
    Task<IReadOnlyList<Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get count of unread notifications for a specific user
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paginated notifications for a specific user
    /// </summary>
    Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetPagedByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark all notifications as read for a specific user
    /// </summary>
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete old read notifications (cleanup task)
    /// </summary>
    Task DeleteOldReadNotificationsAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}
