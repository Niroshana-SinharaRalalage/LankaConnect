using LankaConnect.SPLIT_PER_ENTITY;
using LankaConnect.SPLIT_PER_ENTITY.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using System.Diagnostics;
using Serilog.Context;

using LankaConnect.BuildingBlocks.Abstractions;
namespace LankaConnect.Products.LankaEvents.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for RegistrationPayment operations.
/// Part of the Add-Only Attendees with Delta Payment feature.
/// </summary>
[Wave6_5TransitionalException("Inherits Repository<T> + uses AppDbContext via LankaConnect.Infrastructure.Data; cleared in Wave 6.5 LankaEventsDbContext extraction")]
public class RegistrationPaymentRepository : Repository<RegistrationPayment>, IRegistrationPaymentRepository
{
    private readonly ILogger<RegistrationPaymentRepository> _repoLogger;

    public RegistrationPaymentRepository(
        AppDbContext context,
        ILogger<RegistrationPaymentRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RegistrationPayment>> GetByRegistrationIdAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByRegistrationId"))
        using (LogContext.PushProperty("EntityType", "RegistrationPayment"))
        using (LogContext.PushProperty("RegistrationId", registrationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByRegistrationIdAsync START: RegistrationId={RegistrationId}",
                registrationId);

            try
            {
                var payments = await _dbSet
                    .AsNoTracking()
                    .Where(rp => rp.RegistrationId == registrationId)
                    .OrderBy(rp => rp.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByRegistrationIdAsync COMPLETE: RegistrationId={RegistrationId}, Count={Count}, Duration={ElapsedMs}ms",
                    registrationId,
                    payments.Count,
                    stopwatch.ElapsedMilliseconds);

                return payments;
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
    public async Task<RegistrationPayment?> GetByPaymentIntentIdAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByPaymentIntentId"))
        using (LogContext.PushProperty("EntityType", "RegistrationPayment"))
        using (LogContext.PushProperty("PaymentIntentId", paymentIntentId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByPaymentIntentIdAsync START: PaymentIntentId={PaymentIntentId}",
                paymentIntentId);

            try
            {
                var payment = await _dbSet
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        rp => rp.StripePaymentIntentId == paymentIntentId,
                        cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByPaymentIntentIdAsync COMPLETE: PaymentIntentId={PaymentIntentId}, Found={Found}, Duration={ElapsedMs}ms",
                    paymentIntentId,
                    payment != null,
                    stopwatch.ElapsedMilliseconds);

                return payment;
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
    public async Task<RegistrationPayment?> GetInitialPaymentAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetInitialPayment"))
        using (LogContext.PushProperty("EntityType", "RegistrationPayment"))
        using (LogContext.PushProperty("RegistrationId", registrationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetInitialPaymentAsync START: RegistrationId={RegistrationId}",
                registrationId);

            try
            {
                var payment = await _dbSet
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        rp => rp.RegistrationId == registrationId &&
                              rp.Type == PaymentType.Initial,
                        cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetInitialPaymentAsync COMPLETE: RegistrationId={RegistrationId}, Found={Found}, Duration={ElapsedMs}ms",
                    registrationId,
                    payment != null,
                    stopwatch.ElapsedMilliseconds);

                return payment;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetInitialPaymentAsync FAILED: RegistrationId={RegistrationId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    registrationId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RegistrationPayment>> GetAdditionPaymentsAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetAdditionPayments"))
        using (LogContext.PushProperty("EntityType", "RegistrationPayment"))
        using (LogContext.PushProperty("RegistrationId", registrationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetAdditionPaymentsAsync START: RegistrationId={RegistrationId}",
                registrationId);

            try
            {
                var payments = await _dbSet
                    .AsNoTracking()
                    .Where(rp => rp.RegistrationId == registrationId &&
                                 rp.Type == PaymentType.Addition)
                    .OrderBy(rp => rp.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetAdditionPaymentsAsync COMPLETE: RegistrationId={RegistrationId}, Count={Count}, Duration={ElapsedMs}ms",
                    registrationId,
                    payments.Count,
                    stopwatch.ElapsedMilliseconds);

                return payments;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetAdditionPaymentsAsync FAILED: RegistrationId={RegistrationId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    registrationId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalPaidAmountAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetTotalPaidAmount"))
        using (LogContext.PushProperty("EntityType", "RegistrationPayment"))
        using (LogContext.PushProperty("RegistrationId", registrationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetTotalPaidAmountAsync START: RegistrationId={RegistrationId}",
                registrationId);

            try
            {
                var totalAmount = await _dbSet
                    .AsNoTracking()
                    .Where(rp => rp.RegistrationId == registrationId &&
                                 rp.Status == PaymentStatus.Completed)
                    .SumAsync(rp => rp.Amount.Amount, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetTotalPaidAmountAsync COMPLETE: RegistrationId={RegistrationId}, TotalAmount={TotalAmount}, Duration={ElapsedMs}ms",
                    registrationId,
                    totalAmount,
                    stopwatch.ElapsedMilliseconds);

                return totalAmount;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetTotalPaidAmountAsync FAILED: RegistrationId={RegistrationId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    registrationId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> PaymentExistsAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "PaymentExists"))
        using (LogContext.PushProperty("EntityType", "RegistrationPayment"))
        using (LogContext.PushProperty("PaymentIntentId", paymentIntentId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "PaymentExistsAsync START: PaymentIntentId={PaymentIntentId}",
                paymentIntentId);

            try
            {
                var exists = await _dbSet
                    .AnyAsync(
                        rp => rp.StripePaymentIntentId == paymentIntentId,
                        cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "PaymentExistsAsync COMPLETE: PaymentIntentId={PaymentIntentId}, Exists={Exists}, Duration={ElapsedMs}ms",
                    paymentIntentId,
                    exists,
                    stopwatch.ElapsedMilliseconds);

                return exists;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "PaymentExistsAsync FAILED: PaymentIntentId={PaymentIntentId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    paymentIntentId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }
}
