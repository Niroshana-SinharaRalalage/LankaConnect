using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Serilog.Context;

namespace LankaConnect.Products.LankaEvents.Infrastructure.Repositories;

/// <summary>
/// Phase 2B: Repository implementation for SeatReservation operations.
/// Reservations are created on payment completion and hard-deleted on cancellation (V1).
/// </summary>
public class SeatReservationRepository : Repository<SeatReservation>, ISeatReservationRepository
{
    private readonly ILogger<SeatReservationRepository> _repoLogger;

    public SeatReservationRepository(
        AppDbContext context,
        ILogger<SeatReservationRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    /// <inheritdoc />
    public async Task<SeatReservation?> GetBySeatIdAsync(Guid seatId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetBySeatId"))
        using (LogContext.PushProperty("EntityType", "SeatReservation"))
        {
            try
            {
                return await _dbSet
                    .FirstOrDefaultAsync(r => r.SeatId == seatId, cancellationToken);
            }
            catch (Exception ex)
            {
                _repoLogger.LogError(ex,
                    "GetBySeatIdAsync FAILED: SeatId={SeatId}, Error={ErrorMessage}",
                    seatId, ex.Message);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SeatReservation>> GetByRegistrationIdAsync(Guid registrationId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByRegistrationId"))
        using (LogContext.PushProperty("EntityType", "SeatReservation"))
        {
            try
            {
                return await _dbSet
                    .Where(r => r.RegistrationId == registrationId)
                    .OrderBy(r => r.AttendeeIndex)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _repoLogger.LogError(ex,
                    "GetByRegistrationIdAsync FAILED: RegistrationId={RegistrationId}, Error={ErrorMessage}",
                    registrationId, ex.Message);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SeatReservation>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEventId"))
        using (LogContext.PushProperty("EntityType", "SeatReservation"))
        {
            try
            {
                return await _dbSet
                    .Where(r => r.EventId == eventId)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _repoLogger.LogError(ex,
                    "GetByEventIdAsync FAILED: EventId={EventId}, Error={ErrorMessage}",
                    eventId, ex.Message);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetReservedSeatIdsAsync(IEnumerable<Guid> seatIds, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetReservedSeatIds"))
        using (LogContext.PushProperty("EntityType", "SeatReservation"))
        {
            try
            {
                var seatIdList = seatIds.ToList();
                return await _dbSet
                    .Where(r => seatIdList.Contains(r.SeatId))
                    .Select(r => r.SeatId)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _repoLogger.LogError(ex,
                    "GetReservedSeatIdsAsync FAILED: Error={ErrorMessage}", ex.Message);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task DeleteByRegistrationIdAsync(Guid registrationId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "DeleteByRegistrationId"))
        using (LogContext.PushProperty("EntityType", "SeatReservation"))
        using (LogContext.PushProperty("RegistrationId", registrationId))
        {
            try
            {
                var reservations = await _dbSet
                    .Where(r => r.RegistrationId == registrationId)
                    .ToListAsync(cancellationToken);

                if (reservations.Count > 0)
                {
                    _dbSet.RemoveRange(reservations);

                    _repoLogger.LogInformation(
                        "DeleteByRegistrationIdAsync: Removed {Count} seat reservations for RegistrationId={RegistrationId}",
                        reservations.Count,
                        registrationId);
                }
            }
            catch (Exception ex)
            {
                _repoLogger.LogError(ex,
                    "DeleteByRegistrationIdAsync FAILED: RegistrationId={RegistrationId}, Error={ErrorMessage}",
                    registrationId, ex.Message);
                throw;
            }
        }
    }
}
