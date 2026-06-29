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
/// Repository implementation for Sponsor operations.
/// Part of the standalone Sponsor system for events.
/// </summary>
public class SponsorRepository : Repository<Sponsor>, ISponsorRepository
{
    private readonly ILogger<SponsorRepository> _repoLogger;

    public SponsorRepository(
        AppDbContext context,
        ILogger<SponsorRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    /// <inheritdoc />
    public async Task<Sponsor?> GetByCheckoutSessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByCheckoutSessionId"))
        using (LogContext.PushProperty("EntityType", "Sponsor"))
        using (LogContext.PushProperty("SessionId", sessionId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByCheckoutSessionIdAsync START: SessionId={SessionId}",
                sessionId);

            try
            {
                var sponsor = await _dbSet
                    .FirstOrDefaultAsync(
                        s => s.StripeCheckoutSessionId == sessionId,
                        cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByCheckoutSessionIdAsync COMPLETE: SessionId={SessionId}, Found={Found}, Duration={ElapsedMs}ms",
                    sessionId,
                    sponsor != null,
                    stopwatch.ElapsedMilliseconds);

                return sponsor;
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
    public async Task<IReadOnlyList<Sponsor>> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEventId"))
        using (LogContext.PushProperty("EntityType", "Sponsor"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByEventIdAsync START: EventId={EventId}",
                eventId);

            try
            {
                var sponsors = await _dbSet
                    .AsNoTracking()
                    .Where(s => s.EventId == eventId)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEventIdAsync COMPLETE: EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId,
                    sponsors.Count,
                    stopwatch.ElapsedMilliseconds);

                return sponsors;
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
    public async Task<IReadOnlyList<Sponsor>> GetByEventIdAndTypeAsync(
        Guid eventId,
        SponsorType type,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEventIdAndType"))
        using (LogContext.PushProperty("EntityType", "Sponsor"))
        using (LogContext.PushProperty("EventId", eventId))
        using (LogContext.PushProperty("SponsorType", type))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByEventIdAndTypeAsync START: EventId={EventId}, Type={SponsorType}",
                eventId, type);

            try
            {
                var sponsors = await _dbSet
                    .AsNoTracking()
                    .Where(s => s.EventId == eventId && s.Type == type)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEventIdAndTypeAsync COMPLETE: EventId={EventId}, Type={SponsorType}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId,
                    type,
                    sponsors.Count,
                    stopwatch.ElapsedMilliseconds);

                return sponsors;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByEventIdAndTypeAsync FAILED: EventId={EventId}, Type={SponsorType}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    eventId,
                    type,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Sponsor>> GetCompletedMoneySponsorsForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetCompletedMoneySponsorsForEvent"))
        using (LogContext.PushProperty("EntityType", "Sponsor"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetCompletedMoneySponsorsForEventAsync START: EventId={EventId}",
                eventId);

            try
            {
                var sponsors = await _dbSet
                    .AsNoTracking()
                    .Where(s => s.EventId == eventId &&
                                s.Type == SponsorType.Money &&
                                s.Status == SponsorStatus.Completed)
                    .OrderByDescending(s => s.PaymentCompletedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetCompletedMoneySponsorsForEventAsync COMPLETE: EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId,
                    sponsors.Count,
                    stopwatch.ElapsedMilliseconds);

                return sponsors;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetCompletedMoneySponsorsForEventAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Sponsor>> GetRecordedItemSponsorsForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetRecordedItemSponsorsForEvent"))
        using (LogContext.PushProperty("EntityType", "Sponsor"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetRecordedItemSponsorsForEventAsync START: EventId={EventId}",
                eventId);

            try
            {
                var sponsors = await _dbSet
                    .AsNoTracking()
                    .Where(s => s.EventId == eventId &&
                                s.Status == SponsorStatus.RecordedItem)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetRecordedItemSponsorsForEventAsync COMPLETE: EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId,
                    sponsors.Count,
                    stopwatch.ElapsedMilliseconds);

                return sponsors;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetRecordedItemSponsorsForEventAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalMoneySponsoredForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetTotalMoneySponsoredForEvent"))
        using (LogContext.PushProperty("EntityType", "Sponsor"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetTotalMoneySponsoredForEventAsync START: EventId={EventId}",
                eventId);

            try
            {
                var total = await _dbSet
                    .Where(s => s.EventId == eventId &&
                                s.Type == SponsorType.Money &&
                                s.Status == SponsorStatus.Completed)
                    .SumAsync(s => s.Amount!.Amount, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetTotalMoneySponsoredForEventAsync COMPLETE: EventId={EventId}, Total={Total}, Duration={ElapsedMs}ms",
                    eventId,
                    total,
                    stopwatch.ElapsedMilliseconds);

                return total;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetTotalMoneySponsoredForEventAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Sponsor>> GetByUserIdAndEventIdAsync(
        Guid userId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByUserIdAndEventId"))
        using (LogContext.PushProperty("EntityType", "Sponsor"))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByUserIdAndEventIdAsync START: UserId={UserId}, EventId={EventId}",
                userId, eventId);

            try
            {
                var sponsors = await _dbSet
                    .AsNoTracking()
                    .Where(s => s.SponsorUserId == userId && s.EventId == eventId)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByUserIdAndEventIdAsync COMPLETE: UserId={UserId}, EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    userId,
                    eventId,
                    sponsors.Count,
                    stopwatch.ElapsedMilliseconds);

                return sponsors;
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
}
