using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Notifications;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Notification entity
/// Phase 6A.6: Notification System
/// </summary>
public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    private readonly ILogger<NotificationRepository> _repoLogger;

    public NotificationRepository(
        AppDbContext context,
        ILogger<NotificationRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    /// <summary>
    /// Get all notifications for a specific user
    /// </summary>
    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByUserId"))
        using (LogContext.PushProperty("EntityType", "Notification"))
        using (LogContext.PushProperty("UserId", userId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByUserIdAsync START: UserId={UserId}", userId);

            try
            {
                var notifications = await _dbSet
                    .AsNoTracking()
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByUserIdAsync COMPLETE: UserId={UserId}, Count={Count}, Duration={ElapsedMs}ms",
                    userId,
                    notifications.Count,
                    stopwatch.ElapsedMilliseconds);

                return notifications;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByUserIdAsync FAILED: UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    userId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Get unread notifications for a specific user
    /// </summary>
    public async Task<IReadOnlyList<Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetUnreadByUserId"))
        using (LogContext.PushProperty("EntityType", "Notification"))
        using (LogContext.PushProperty("UserId", userId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetUnreadByUserIdAsync START: UserId={UserId}", userId);

            try
            {
                var notifications = await _dbSet
                    .AsNoTracking()
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetUnreadByUserIdAsync COMPLETE: UserId={UserId}, Count={Count}, Duration={ElapsedMs}ms",
                    userId,
                    notifications.Count,
                    stopwatch.ElapsedMilliseconds);

                return notifications;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetUnreadByUserIdAsync FAILED: UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    userId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Get count of unread notifications for a specific user
    /// </summary>
    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetUnreadCount"))
        using (LogContext.PushProperty("EntityType", "Notification"))
        using (LogContext.PushProperty("UserId", userId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetUnreadCountAsync START: UserId={UserId}", userId);

            try
            {
                var count = await _dbSet
                    .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetUnreadCountAsync COMPLETE: UserId={UserId}, Count={Count}, Duration={ElapsedMs}ms",
                    userId,
                    count,
                    stopwatch.ElapsedMilliseconds);

                return count;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetUnreadCountAsync FAILED: UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    userId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Get paginated notifications for a specific user
    /// </summary>
    public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetPagedByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetPagedByUserId"))
        using (LogContext.PushProperty("EntityType", "Notification"))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("Page", page))
        using (LogContext.PushProperty("PageSize", pageSize))
        using (LogContext.PushProperty("UnreadOnly", unreadOnly))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetPagedByUserIdAsync START: UserId={UserId}, Page={Page}, PageSize={PageSize}, UnreadOnly={UnreadOnly}",
                userId, page, pageSize, unreadOnly);

            try
            {
                var query = _dbSet
                    .AsNoTracking()
                    .Where(n => n.UserId == userId);

                if (unreadOnly)
                {
                    query = query.Where(n => !n.IsRead);
                }

                var totalCount = await query.CountAsync(cancellationToken);

                var items = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetPagedByUserIdAsync COMPLETE: UserId={UserId}, Page={Page}, PageSize={PageSize}, UnreadOnly={UnreadOnly}, ItemCount={ItemCount}, TotalCount={TotalCount}, Duration={ElapsedMs}ms",
                    userId, page, pageSize, unreadOnly, items.Count, totalCount, stopwatch.ElapsedMilliseconds);

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetPagedByUserIdAsync FAILED: UserId={UserId}, Page={Page}, PageSize={PageSize}, UnreadOnly={UnreadOnly}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    userId, page, pageSize, unreadOnly, stopwatch.ElapsedMilliseconds, ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Mark all notifications as read for a specific user
    /// </summary>
    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "MarkAllAsRead"))
        using (LogContext.PushProperty("EntityType", "Notification"))
        using (LogContext.PushProperty("UserId", userId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("MarkAllAsReadAsync START: UserId={UserId}", userId);

            try
            {
                var rowsAffected = await _dbSet
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(n => n.IsRead, true)
                            .SetProperty(n => n.ReadAt, DateTime.UtcNow)
                            .SetProperty(n => n.UpdatedAt, DateTime.UtcNow),
                        cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "MarkAllAsReadAsync COMPLETE: UserId={UserId}, RowsAffected={RowsAffected}, Duration={ElapsedMs}ms",
                    userId,
                    rowsAffected,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "MarkAllAsReadAsync FAILED: UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    userId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    /// <summary>
    /// Delete old read notifications (cleanup task)
    /// </summary>
    public async Task DeleteOldReadNotificationsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "DeleteOldReadNotifications"))
        using (LogContext.PushProperty("EntityType", "Notification"))
        using (LogContext.PushProperty("OlderThan", olderThan))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("DeleteOldReadNotificationsAsync START: OlderThan={OlderThan}", olderThan);

            try
            {
                var rowsAffected = await _dbSet
                    .Where(n => n.IsRead && n.CreatedAt < olderThan)
                    .ExecuteDeleteAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "DeleteOldReadNotificationsAsync COMPLETE: OlderThan={OlderThan}, RowsDeleted={RowsDeleted}, Duration={ElapsedMs}ms",
                    olderThan,
                    rowsAffected,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "DeleteOldReadNotificationsAsync FAILED: OlderThan={OlderThan}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    olderThan,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }
}
