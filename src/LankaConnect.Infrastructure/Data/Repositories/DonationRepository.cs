using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Donation operations.
/// Part of the standalone Donation system for events.
/// </summary>
public class DonationRepository : Repository<Donation>, IDonationRepository
{
    private readonly ILogger<DonationRepository> _repoLogger;

    public DonationRepository(
        AppDbContext context,
        ILogger<DonationRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    /// <inheritdoc />
    public async Task<Donation?> GetByCheckoutSessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByCheckoutSessionId"))
        using (LogContext.PushProperty("EntityType", "Donation"))
        using (LogContext.PushProperty("SessionId", sessionId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByCheckoutSessionIdAsync START: SessionId={SessionId}",
                sessionId);

            try
            {
                var donation = await _dbSet
                    .FirstOrDefaultAsync(
                        d => d.StripeCheckoutSessionId == sessionId,
                        cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByCheckoutSessionIdAsync COMPLETE: SessionId={SessionId}, Found={Found}, Duration={ElapsedMs}ms",
                    sessionId,
                    donation != null,
                    stopwatch.ElapsedMilliseconds);

                return donation;
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
    public async Task<Donation?> GetByDonationIdAsync(
        Guid donationId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByDonationId"))
        using (LogContext.PushProperty("EntityType", "Donation"))
        using (LogContext.PushProperty("DonationId", donationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByDonationIdAsync START: DonationId={DonationId}",
                donationId);

            try
            {
                var donation = await _dbSet
                    .FirstOrDefaultAsync(d => d.Id == donationId, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByDonationIdAsync COMPLETE: DonationId={DonationId}, Found={Found}, Duration={ElapsedMs}ms",
                    donationId,
                    donation != null,
                    stopwatch.ElapsedMilliseconds);

                return donation;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByDonationIdAsync FAILED: DonationId={DonationId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    donationId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<Donation?> GetByRegistrationIdAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByRegistrationId"))
        using (LogContext.PushProperty("EntityType", "Donation"))
        using (LogContext.PushProperty("RegistrationId", registrationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByRegistrationIdAsync START: RegistrationId={RegistrationId}",
                registrationId);

            try
            {
                var donation = await _dbSet
                    .FirstOrDefaultAsync(
                        d => d.RegistrationId == registrationId,
                        cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByRegistrationIdAsync COMPLETE: RegistrationId={RegistrationId}, Found={Found}, Duration={ElapsedMs}ms",
                    registrationId,
                    donation != null,
                    stopwatch.ElapsedMilliseconds);

                return donation;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByRegistrationIdAsync FAILED: RegistrationId={RegistrationId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    registrationId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Donation>> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEventId"))
        using (LogContext.PushProperty("EntityType", "Donation"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByEventIdAsync START: EventId={EventId}",
                eventId);

            try
            {
                var donations = await _dbSet
                    .AsNoTracking()
                    .Where(d => d.EventId == eventId)
                    .OrderByDescending(d => d.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEventIdAsync COMPLETE: EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId,
                    donations.Count,
                    stopwatch.ElapsedMilliseconds);

                return donations;
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
    public async Task<IReadOnlyList<Donation>> GetCompletedByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetCompletedByEventId"))
        using (LogContext.PushProperty("EntityType", "Donation"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetCompletedByEventIdAsync START: EventId={EventId}",
                eventId);

            try
            {
                var donations = await _dbSet
                    .AsNoTracking()
                    .Where(d => d.EventId == eventId && d.Status == DonationStatus.Completed)
                    .OrderByDescending(d => d.PaymentCompletedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetCompletedByEventIdAsync COMPLETE: EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId,
                    donations.Count,
                    stopwatch.ElapsedMilliseconds);

                return donations;
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
    public async Task<decimal> GetTotalDonationsForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetTotalDonationsForEvent"))
        using (LogContext.PushProperty("EntityType", "Donation"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetTotalDonationsForEventAsync START: EventId={EventId}",
                eventId);

            try
            {
                var total = await _dbSet
                    .Where(d => d.EventId == eventId && d.Status == DonationStatus.Completed)
                    .SumAsync(d => d.Amount.Amount, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetTotalDonationsForEventAsync COMPLETE: EventId={EventId}, Total={Total}, Duration={ElapsedMs}ms",
                    eventId,
                    total,
                    stopwatch.ElapsedMilliseconds);

                return total;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetTotalDonationsForEventAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Donation>> GetExpiredPendingDonationsAsync(
        DateTime cutoffTime,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetExpiredPendingDonations"))
        using (LogContext.PushProperty("EntityType", "Donation"))
        using (LogContext.PushProperty("CutoffTime", cutoffTime))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetExpiredPendingDonationsAsync START: CutoffTime={CutoffTime}",
                cutoffTime);

            try
            {
                var donations = await _dbSet
                    .Where(d => d.Status == DonationStatus.Pending &&
                                d.CheckoutExpiresAt.HasValue &&
                                d.CheckoutExpiresAt.Value < cutoffTime)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetExpiredPendingDonationsAsync COMPLETE: CutoffTime={CutoffTime}, Count={Count}, Duration={ElapsedMs}ms",
                    cutoffTime,
                    donations.Count,
                    stopwatch.ElapsedMilliseconds);

                return donations;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetExpiredPendingDonationsAsync FAILED: CutoffTime={CutoffTime}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    cutoffTime,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Donation>> GetByEventIdAndStatusAsync(
        Guid eventId,
        DonationStatus status,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEventIdAndStatus"))
        using (LogContext.PushProperty("EntityType", "Donation"))
        using (LogContext.PushProperty("EventId", eventId))
        using (LogContext.PushProperty("Status", status))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByEventIdAndStatusAsync START: EventId={EventId}, Status={Status}",
                eventId, status);

            try
            {
                var donations = await _dbSet
                    .AsNoTracking()
                    .Where(d => d.EventId == eventId && d.Status == status)
                    .OrderByDescending(d => d.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEventIdAndStatusAsync COMPLETE: EventId={EventId}, Status={Status}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId,
                    status,
                    donations.Count,
                    stopwatch.ElapsedMilliseconds);

                return donations;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByEventIdAndStatusAsync FAILED: EventId={EventId}, Status={Status}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    eventId,
                    status,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Donation>> GetByUserIdAndEventIdAsync(
        Guid userId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByUserIdAndEventId"))
        using (LogContext.PushProperty("EntityType", "Donation"))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByUserIdAndEventIdAsync START: UserId={UserId}, EventId={EventId}",
                userId, eventId);

            try
            {
                var donations = await _dbSet
                    .AsNoTracking()
                    .Where(d => d.DonorUserId == userId && d.EventId == eventId)
                    .OrderByDescending(d => d.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByUserIdAndEventIdAsync COMPLETE: UserId={UserId}, EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    userId,
                    eventId,
                    donations.Count,
                    stopwatch.ElapsedMilliseconds);

                return donations;
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
