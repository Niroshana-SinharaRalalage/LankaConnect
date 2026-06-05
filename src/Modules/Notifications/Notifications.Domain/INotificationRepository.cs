using LankaConnect.BuildingBlocks.Application.Abstractions;

namespace LankaConnect.Modules.Notifications.Domain;

/// <summary>
/// Repository interface for the <see cref="Notification"/> aggregate.
/// Originated in Phase 6A.6; moved into the Notifications module Domain layer
/// during Phase A W3.2 (2026-06-02); refactored from legacy
/// <c>LankaConnect.Domain.Common.IRepository&lt;T&gt;</c> to
/// <see cref="IAggregateRepository{TAggregate, TId}"/> in W3A (2026-06-05)
/// per ADR-010.
/// </summary>
/// <remarks>
/// <para>
/// Per ADR-010 (Repository-per-Aggregate): named query methods only. The legacy
/// <c>IRepository&lt;T&gt;</c>'s generic predicate-based queries
/// (<c>FindAsync(Expression&lt;Func&lt;T, bool&gt;&gt;)</c>) and <c>GetAll</c>
/// are forbidden because they let callers query across aggregate boundaries.
/// Each method below has explicit query intent.
/// </para>
/// <para>
/// Concrete EF implementation lives in
/// <c>LankaConnect.Modules.Notifications.Infrastructure.Repositories.NotificationRepository</c>.
/// </para>
/// </remarks>
public interface INotificationRepository : IAggregateRepository<Notification, Guid>
{
    /// <summary>Get a notification by its identifier.</summary>
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persist a new notification.</summary>
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>Track an existing notification as modified.</summary>
    void Update(Notification notification);

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
