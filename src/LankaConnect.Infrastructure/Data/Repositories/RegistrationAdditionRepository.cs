using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for RegistrationAddition operations.
/// Part of the Add-Only Attendees with Delta Payment feature.
/// </summary>
public class RegistrationAdditionRepository : Repository<RegistrationAddition>, IRegistrationAdditionRepository
{
    private readonly ILogger<RegistrationAdditionRepository> _repoLogger;

    public RegistrationAdditionRepository(
        AppDbContext context,
        ILogger<RegistrationAdditionRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    /// <inheritdoc />
    public async Task<RegistrationAddition?> GetPendingByRegistrationIdAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetPendingByRegistrationId"))
        using (LogContext.PushProperty("EntityType", "RegistrationAddition"))
        using (LogContext.PushProperty("RegistrationId", registrationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetPendingByRegistrationIdAsync START: RegistrationId={RegistrationId}",
                registrationId);

            try
            {
                var addition = await _dbSet
                    .FirstOrDefaultAsync(
                        ra => ra.RegistrationId == registrationId &&
                              ra.Status == RegistrationAdditionStatus.Pending,
                        cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetPendingByRegistrationIdAsync COMPLETE: RegistrationId={RegistrationId}, Found={Found}, Duration={ElapsedMs}ms",
                    registrationId,
                    addition != null,
                    stopwatch.ElapsedMilliseconds);

                return addition;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetPendingByRegistrationIdAsync FAILED: RegistrationId={RegistrationId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    registrationId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<RegistrationAddition?> GetByCheckoutSessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByCheckoutSessionId"))
        using (LogContext.PushProperty("EntityType", "RegistrationAddition"))
        using (LogContext.PushProperty("SessionId", sessionId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByCheckoutSessionIdAsync START: SessionId={SessionId}",
                sessionId);

            try
            {
                var addition = await _dbSet
                    .FirstOrDefaultAsync(
                        ra => ra.StripeCheckoutSessionId == sessionId,
                        cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByCheckoutSessionIdAsync COMPLETE: SessionId={SessionId}, Found={Found}, Duration={ElapsedMs}ms",
                    sessionId,
                    addition != null,
                    stopwatch.ElapsedMilliseconds);

                return addition;
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
    public async Task<RegistrationAddition?> GetByPaymentIntentIdAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByPaymentIntentId"))
        using (LogContext.PushProperty("EntityType", "RegistrationAddition"))
        using (LogContext.PushProperty("PaymentIntentId", paymentIntentId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByPaymentIntentIdAsync START: PaymentIntentId={PaymentIntentId}",
                paymentIntentId);

            try
            {
                var addition = await _dbSet
                    .FirstOrDefaultAsync(
                        ra => ra.StripePaymentIntentId == paymentIntentId,
                        cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByPaymentIntentIdAsync COMPLETE: PaymentIntentId={PaymentIntentId}, Found={Found}, Duration={ElapsedMs}ms",
                    paymentIntentId,
                    addition != null,
                    stopwatch.ElapsedMilliseconds);

                return addition;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByPaymentIntentIdAsync FAILED: PaymentIntentId={PaymentIntentId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    paymentIntentId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RegistrationAddition>> GetByEventIdAndStatusAsync(
        Guid eventId,
        RegistrationAdditionStatus status,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEventIdAndStatus"))
        using (LogContext.PushProperty("EntityType", "RegistrationAddition"))
        using (LogContext.PushProperty("EventId", eventId))
        using (LogContext.PushProperty("Status", status))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByEventIdAndStatusAsync START: EventId={EventId}, Status={Status}",
                eventId, status);

            try
            {
                var additions = await _dbSet
                    .AsNoTracking()
                    .Where(ra => ra.EventId == eventId && ra.Status == status)
                    .OrderByDescending(ra => ra.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEventIdAndStatusAsync COMPLETE: EventId={EventId}, Status={Status}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId,
                    status,
                    additions.Count,
                    stopwatch.ElapsedMilliseconds);

                return additions;
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
    public async Task<IReadOnlyList<RegistrationAddition>> GetByRegistrationIdAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByRegistrationId"))
        using (LogContext.PushProperty("EntityType", "RegistrationAddition"))
        using (LogContext.PushProperty("RegistrationId", registrationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByRegistrationIdAsync START: RegistrationId={RegistrationId}",
                registrationId);

            try
            {
                var additions = await _dbSet
                    .AsNoTracking()
                    .Where(ra => ra.RegistrationId == registrationId)
                    .OrderByDescending(ra => ra.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByRegistrationIdAsync COMPLETE: RegistrationId={RegistrationId}, Count={Count}, Duration={ElapsedMs}ms",
                    registrationId,
                    additions.Count,
                    stopwatch.ElapsedMilliseconds);

                return additions;
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
    public async Task<IReadOnlyList<RegistrationAddition>> GetExpiredPendingAdditionsAsync(
        DateTime cutoffTime,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetExpiredPendingAdditions"))
        using (LogContext.PushProperty("EntityType", "RegistrationAddition"))
        using (LogContext.PushProperty("CutoffTime", cutoffTime))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetExpiredPendingAdditionsAsync START: CutoffTime={CutoffTime}",
                cutoffTime);

            try
            {
                var additions = await _dbSet
                    .Where(ra => ra.Status == RegistrationAdditionStatus.Pending &&
                                 ra.CheckoutExpiresAt.HasValue &&
                                 ra.CheckoutExpiresAt.Value < cutoffTime)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetExpiredPendingAdditionsAsync COMPLETE: CutoffTime={CutoffTime}, Count={Count}, Duration={ElapsedMs}ms",
                    cutoffTime,
                    additions.Count,
                    stopwatch.ElapsedMilliseconds);

                return additions;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetExpiredPendingAdditionsAsync FAILED: CutoffTime={CutoffTime}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    cutoffTime,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasPendingAdditionAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "HasPendingAddition"))
        using (LogContext.PushProperty("EntityType", "RegistrationAddition"))
        using (LogContext.PushProperty("RegistrationId", registrationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "HasPendingAdditionAsync START: RegistrationId={RegistrationId}",
                registrationId);

            try
            {
                var exists = await _dbSet
                    .AnyAsync(
                        ra => ra.RegistrationId == registrationId &&
                              ra.Status == RegistrationAdditionStatus.Pending,
                        cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "HasPendingAdditionAsync COMPLETE: RegistrationId={RegistrationId}, HasPending={HasPending}, Duration={ElapsedMs}ms",
                    registrationId,
                    exists,
                    stopwatch.ElapsedMilliseconds);

                return exists;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "HasPendingAdditionAsync FAILED: RegistrationId={RegistrationId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    registrationId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }
}
