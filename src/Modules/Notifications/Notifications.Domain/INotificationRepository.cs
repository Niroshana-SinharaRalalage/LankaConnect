using LankaConnect.Domain.Common;

namespace LankaConnect.Modules.Notifications.Domain;

/// <summary>
/// Repository interface for the <see cref="Notification"/> aggregate.
/// Originated in Phase 6A.6; moved into the Notifications module Domain layer
/// during Phase A W3.2 (2026-06-02) without surface change.
/// </summary>
/// <remarks>
/// Temporarily extends <c>LankaConnect.Domain.Common.IRepository&lt;T&gt;</c>
/// pending the BuildingBlocks elevation of <c>IRepository&lt;T&gt;</c>
/// (planned W4/W5). Concrete EF implementation lives in
/// <c>LankaConnect.Infrastructure.Data.Repositories.NotificationRepository</c>
/// today and moves to <c>Notifications.Infrastructure</c> in W3.4.
/// </remarks>
public interface INotificationRepository : IRepository<Notification>
{
    /// <summary>Get all notifications for a specific user.</summary>
    Task<IReadOnlyList<Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Get unread notifications for a specific user.</summary>
    Task<IReadOnlyList<Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Get count of unread notifications for a specific user.</summary>
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Get paginated notifications for a specific user.</summary>
    Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetPagedByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>Mark all notifications as read for a specific user.</summary>
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Delete old read notifications (cleanup task).</summary>
    Task DeleteOldReadNotificationsAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}
