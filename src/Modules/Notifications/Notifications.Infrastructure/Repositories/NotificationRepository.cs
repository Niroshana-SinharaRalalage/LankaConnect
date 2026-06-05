using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.Modules.Notifications.Domain;
// W3.4 (2026-06-03) — transitional edge: AppDbContext lives in legacy
// LankaConnect.Infrastructure. Cut when W3.6/W3.7 dispatcher fully routes
// repository onto NotificationsDbContext.
using LankaConnect.Infrastructure.Data;

namespace LankaConnect.Modules.Notifications.Infrastructure.Repositories;

/// <summary>
/// Concrete repository for the <see cref="Notification"/> aggregate.
/// Hand-rolled per ADR-010 (Repository-per-Aggregate) — no generic
/// <c>Repository&lt;T&gt;</c> base. Each method has explicit query intent.
/// </summary>
/// <remarks>
/// W3A (2026-06-05): de-coupled from legacy <c>Repository&lt;T&gt;</c> base
/// after Notification migrated to <see cref="LankaConnect.BuildingBlocks.Domain.Entity{TId}"/>.
/// Methods previously inherited from <c>Repository&lt;T&gt;</c> (AddAsync,
/// GetByIdAsync, Update) are now explicit, using the injected DbContext directly.
/// </remarks>
public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<Notification> _dbSet;
    private readonly ILogger<NotificationRepository> _repoLogger;

    public NotificationRepository(
        AppDbContext context,
        ILogger<NotificationRepository> logger)
    {
        _context = context;
        _dbSet = context.Set<Notification>();
        _repoLogger = logger;
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetById"))
        using (LogContext.PushProperty("EntityType", nameof(Notification)))
        using (LogContext.PushProperty("EntityId", id))
        {
            return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        }
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "Add"))
        using (LogContext.PushProperty("EntityType", nameof(Notification)))
        using (LogContext.PushProperty("EntityId", notification.Id))
        {
            await _dbSet.AddAsync(notification, cancellationToken);
        }
    }

    public void Update(Notification notification)
    {
        using (LogContext.PushProperty("Operation", "Update"))
        using (LogContext.PushProperty("EntityType", nameof(Notification)))
        using (LogContext.PushProperty("EntityId", notification.Id))
        {
            _dbSet.Update(notification);
        }
    }

    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByUserId"))
        using (LogContext.PushProperty("EntityType", nameof(Notification)))
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

    public async Task<IReadOnlyList<Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetUnreadByUserId"))
        using (LogContext.PushProperty("EntityType", nameof(Notification)))
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

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetUnreadCount"))
        using (LogContext.PushProperty("EntityType", nameof(Notification)))
        using (LogContext.PushProperty("UserId", userId))
        {
            return await _dbSet
                .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
        }
    }

    public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetPagedByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetPagedByUserId"))
        using (LogContext.PushProperty("EntityType", nameof(Notification)))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("Page", page))
        using (LogContext.PushProperty("PageSize", pageSize))
        using (LogContext.PushProperty("UnreadOnly", unreadOnly))
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

            return (items, totalCount);
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "MarkAllAsRead"))
        using (LogContext.PushProperty("EntityType", nameof(Notification)))
        using (LogContext.PushProperty("UserId", userId))
        {
            var rowsAffected = await _dbSet
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(n => n.IsRead, true)
                        .SetProperty(n => n.ReadAt, DateTime.UtcNow)
                        .SetProperty(n => n.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);

            _repoLogger.LogInformation(
                "MarkAllAsReadAsync: UserId={UserId}, RowsAffected={RowsAffected}",
                userId, rowsAffected);
        }
    }

    public async Task DeleteOldReadNotificationsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "DeleteOldReadNotifications"))
        using (LogContext.PushProperty("EntityType", nameof(Notification)))
        using (LogContext.PushProperty("OlderThan", olderThan))
        {
            var rowsAffected = await _dbSet
                .Where(n => n.IsRead && n.CreatedAt < olderThan)
                .ExecuteDeleteAsync(cancellationToken);

            _repoLogger.LogInformation(
                "DeleteOldReadNotificationsAsync: OlderThan={OlderThan}, RowsDeleted={RowsDeleted}",
                olderThan, rowsAffected);
        }
    }
}
