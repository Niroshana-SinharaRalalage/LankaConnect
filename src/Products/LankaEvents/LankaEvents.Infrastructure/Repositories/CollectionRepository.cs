using LankaConnect.BuildingBlocks.Abstractions;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Products.LankaEvents.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Collection (event fund) operations.
/// Part of the standalone Collection system for events.
/// </summary>
[Wave6_5TransitionalException("Uses legacy AppDbContext + Repository<T> base via W5.0 transitional ProjectReference; Wave 6.5 will extract LankaEventsDbContext")]
public class CollectionRepository : Repository<Collection>, ICollectionRepository
{
    private readonly ILogger<CollectionRepository> _repoLogger;

    public CollectionRepository(
        AppDbContext context,
        ILogger<CollectionRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    /// <inheritdoc />
    public async Task<Collection?> GetByCheckoutSessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByCheckoutSessionId"))
        using (LogContext.PushProperty("EntityType", "Collection"))
        using (LogContext.PushProperty("SessionId", sessionId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByCheckoutSessionIdAsync START: SessionId={SessionId}",
                sessionId);

            try
            {
                var collection = await _dbSet
                    .FirstOrDefaultAsync(
                        c => c.StripeCheckoutSessionId == sessionId,
                        cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByCheckoutSessionIdAsync COMPLETE: SessionId={SessionId}, Found={Found}, Duration={ElapsedMs}ms",
                    sessionId,
                    collection != null,
                    stopwatch.ElapsedMilliseconds);

                return collection;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByCheckoutSessionIdAsync FAILED: SessionId={SessionId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    sessionId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Collection>> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEventId"))
        using (LogContext.PushProperty("EntityType", "Collection"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByEventIdAsync START: EventId={EventId}",
                eventId);

            try
            {
                var collections = await _dbSet
                    .AsNoTracking()
                    .Where(c => c.EventId == eventId)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEventIdAsync COMPLETE: EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId,
                    collections.Count,
                    stopwatch.ElapsedMilliseconds);

                return collections;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByEventIdAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Collection>> GetCompletedByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetCompletedByEventId"))
        using (LogContext.PushProperty("EntityType", "Collection"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetCompletedByEventIdAsync START: EventId={EventId}",
                eventId);

            try
            {
                var collections = await _dbSet
                    .AsNoTracking()
                    .Where(c => c.EventId == eventId && c.Status == CollectionStatus.Completed)
                    .OrderByDescending(c => c.PaymentCompletedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetCompletedByEventIdAsync COMPLETE: EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId,
                    collections.Count,
                    stopwatch.ElapsedMilliseconds);

                return collections;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetCompletedByEventIdAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalCollectedForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetTotalCollectedForEvent"))
        using (LogContext.PushProperty("EntityType", "Collection"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetTotalCollectedForEventAsync START: EventId={EventId}",
                eventId);

            try
            {
                var total = await _dbSet
                    .Where(c => c.EventId == eventId && c.Status == CollectionStatus.Completed)
                    .SumAsync(c => c.Amount.Amount, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetTotalCollectedForEventAsync COMPLETE: EventId={EventId}, Total={Total}, Duration={ElapsedMs}ms",
                    eventId,
                    total,
                    stopwatch.ElapsedMilliseconds);

                return total;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetTotalCollectedForEventAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<int> GetContributorCountForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetContributorCountForEvent"))
        using (LogContext.PushProperty("EntityType", "Collection"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetContributorCountForEventAsync START: EventId={EventId}",
                eventId);

            try
            {
                var count = await _dbSet
                    .Where(c => c.EventId == eventId && c.Status == CollectionStatus.Completed)
                    .CountAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetContributorCountForEventAsync COMPLETE: EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId,
                    count,
                    stopwatch.ElapsedMilliseconds);

                return count;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetContributorCountForEventAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Collection>> GetByUserIdAndEventIdAsync(
        Guid userId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByUserIdAndEventId"))
        using (LogContext.PushProperty("EntityType", "Collection"))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByUserIdAndEventIdAsync START: UserId={UserId}, EventId={EventId}",
                userId, eventId);

            try
            {
                var collections = await _dbSet
                    .AsNoTracking()
                    .Where(c => c.ContributorUserId == userId && c.EventId == eventId)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByUserIdAndEventIdAsync COMPLETE: UserId={UserId}, EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    userId,
                    eventId,
                    collections.Count,
                    stopwatch.ElapsedMilliseconds);

                return collections;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByUserIdAndEventIdAsync FAILED: UserId={UserId}, EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    userId,
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Collection>> GetExpiredPendingCollectionsAsync(
        DateTime cutoffTime,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetExpiredPendingCollections"))
        using (LogContext.PushProperty("EntityType", "Collection"))
        using (LogContext.PushProperty("CutoffTime", cutoffTime))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetExpiredPendingCollectionsAsync START: CutoffTime={CutoffTime}",
                cutoffTime);

            try
            {
                var collections = await _dbSet
                    .Where(c => c.Status == CollectionStatus.Pending &&
                                c.CheckoutExpiresAt.HasValue &&
                                c.CheckoutExpiresAt.Value < cutoffTime)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetExpiredPendingCollectionsAsync COMPLETE: CutoffTime={CutoffTime}, Count={Count}, Duration={ElapsedMs}ms",
                    cutoffTime,
                    collections.Count,
                    stopwatch.ElapsedMilliseconds);

                return collections;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetExpiredPendingCollectionsAsync FAILED: CutoffTime={CutoffTime}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    cutoffTime,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }
}
