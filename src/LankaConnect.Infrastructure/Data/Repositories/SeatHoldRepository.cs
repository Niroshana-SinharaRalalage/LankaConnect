using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Phase 2B: Repository implementation for SeatHold operations.
/// Supports the seat hold lifecycle and cleanup service queries.
/// </summary>
public class SeatHoldRepository : Repository<SeatHold>, ISeatHoldRepository
{
    private readonly ILogger<SeatHoldRepository> _repoLogger;

    public SeatHoldRepository(
        AppDbContext context,
        ILogger<SeatHoldRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    /// <inheritdoc />
    public async Task<SeatHold?> GetActiveHoldForSeatAsync(Guid seatId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetActiveHoldForSeat"))
        using (LogContext.PushProperty("EntityType", "SeatHold"))
        using (LogContext.PushProperty("SeatId", seatId))
        {
            try
            {
                return await _dbSet
                    .FirstOrDefaultAsync(
                        h => h.SeatId == seatId && h.Status == SeatHoldStatus.Active,
                        cancellationToken);
            }
            catch (Exception ex)
            {
                _repoLogger.LogError(ex,
                    "GetActiveHoldForSeatAsync FAILED: SeatId={SeatId}, Error={ErrorMessage}",
                    seatId, ex.Message);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SeatHold>> GetActiveHoldsBySessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetActiveHoldsBySession"))
        using (LogContext.PushProperty("EntityType", "SeatHold"))
        using (LogContext.PushProperty("SessionId", sessionId))
        {
            try
            {
                return await _dbSet
                    .Where(h => h.SessionId == sessionId && h.Status == SeatHoldStatus.Active)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _repoLogger.LogError(ex,
                    "GetActiveHoldsBySessionAsync FAILED: SessionId={SessionId}, Error={ErrorMessage}",
                    sessionId, ex.Message);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SeatHold>> GetActiveHoldsByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetActiveHoldsByUser"))
        using (LogContext.PushProperty("EntityType", "SeatHold"))
        using (LogContext.PushProperty("UserId", userId))
        {
            try
            {
                return await _dbSet
                    .Where(h => h.UserId == userId && h.Status == SeatHoldStatus.Active)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _repoLogger.LogError(ex,
                    "GetActiveHoldsByUserAsync FAILED: UserId={UserId}, Error={ErrorMessage}",
                    userId, ex.Message);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SeatHold>> GetExpiredActiveHoldsAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetExpiredActiveHolds"))
        using (LogContext.PushProperty("EntityType", "SeatHold"))
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var utcNow = DateTime.UtcNow;
                var holds = await _dbSet
                    .Where(h => h.Status == SeatHoldStatus.Active && h.ExpiresAt < utcNow)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                if (holds.Count > 0)
                {
                    _repoLogger.LogInformation(
                        "GetExpiredActiveHoldsAsync: Found {Count} expired holds to clean up, Duration={ElapsedMs}ms",
                        holds.Count,
                        stopwatch.ElapsedMilliseconds);
                }

                return holds;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetExpiredActiveHoldsAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SeatHold>> GetActiveHoldsForEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetActiveHoldsForEvent"))
        using (LogContext.PushProperty("EntityType", "SeatHold"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            try
            {
                // Join through seat → zone → layout to find holds for an event
                return await _context.Set<SeatHold>()
                    .Where(h => h.Status == SeatHoldStatus.Active)
                    .Join(
                        _context.Set<Seat>(),
                        h => h.SeatId,
                        s => s.Id,
                        (h, s) => new { Hold = h, Seat = s })
                    .Join(
                        _context.Set<VenueZone>(),
                        hs => hs.Seat.VenueZoneId,
                        z => z.Id,
                        (hs, z) => new { hs.Hold, Zone = z })
                    .Join(
                        _context.Set<VenueLayout>(),
                        hz => hz.Zone.VenueLayoutId,
                        l => l.Id,
                        (hz, l) => new { hz.Hold, Layout = l })
                    .Where(hl => hl.Layout.EventId == eventId)
                    .Select(hl => hl.Hold)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _repoLogger.LogError(ex,
                    "GetActiveHoldsForEventAsync FAILED: EventId={EventId}, Error={ErrorMessage}",
                    eventId, ex.Message);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetHeldSeatIdsAsync(IEnumerable<Guid> seatIds, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetHeldSeatIds"))
        using (LogContext.PushProperty("EntityType", "SeatHold"))
        {
            try
            {
                var seatIdList = seatIds.ToList();
                return await _dbSet
                    .Where(h => seatIdList.Contains(h.SeatId) && h.Status == SeatHoldStatus.Active)
                    .Select(h => h.SeatId)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _repoLogger.LogError(ex,
                    "GetHeldSeatIdsAsync FAILED: Error={ErrorMessage}", ex.Message);
                throw;
            }
        }
    }
}
