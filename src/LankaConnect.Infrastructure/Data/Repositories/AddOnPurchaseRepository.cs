using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for AddOnPurchase operations.
/// Part of the standalone AddOn system for events.
/// </summary>
public class AddOnPurchaseRepository : Repository<AddOnPurchase>, IAddOnPurchaseRepository
{
    private readonly ILogger<AddOnPurchaseRepository> _repoLogger;

    public AddOnPurchaseRepository(
        AppDbContext context,
        ILogger<AddOnPurchaseRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    /// <inheritdoc />
    public async Task<AddOnPurchase?> GetByCheckoutSessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByCheckoutSessionId"))
        using (LogContext.PushProperty("EntityType", "AddOnPurchase"))
        using (LogContext.PushProperty("SessionId", sessionId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByCheckoutSessionIdAsync START: SessionId={SessionId}",
                sessionId);

            try
            {
                var purchase = await _dbSet
                    .FirstOrDefaultAsync(
                        p => p.StripeCheckoutSessionId == sessionId,
                        cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByCheckoutSessionIdAsync COMPLETE: SessionId={SessionId}, Found={Found}, Duration={ElapsedMs}ms",
                    sessionId,
                    purchase != null,
                    stopwatch.ElapsedMilliseconds);

                return purchase;
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
    public async Task<IReadOnlyList<AddOnPurchase>> GetAllByStripePaymentIntentIdAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetAllByStripePaymentIntentId"))
        using (LogContext.PushProperty("EntityType", "AddOnPurchase"))
        using (LogContext.PushProperty("PaymentIntentId", paymentIntentId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetAllByStripePaymentIntentIdAsync START: PaymentIntentId={PaymentIntentId}",
                paymentIntentId);

            try
            {
                if (string.IsNullOrWhiteSpace(paymentIntentId))
                    return Array.Empty<AddOnPurchase>();

                // Tracked — webhook will mutate Status; caller commits via _unitOfWork.
                var purchases = await _dbSet
                    .Where(p => p.StripePaymentIntentId == paymentIntentId)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetAllByStripePaymentIntentIdAsync COMPLETE: PaymentIntentId={PaymentIntentId}, Count={Count}, Duration={ElapsedMs}ms",
                    paymentIntentId, purchases.Count, stopwatch.ElapsedMilliseconds);

                return purchases;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex,
                    "GetAllByStripePaymentIntentIdAsync FAILED: PaymentIntentId={PaymentIntentId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    paymentIntentId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AddOnPurchase>> GetAllByCheckoutSessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetAllByCheckoutSessionId"))
        using (LogContext.PushProperty("EntityType", "AddOnPurchase"))
        using (LogContext.PushProperty("SessionId", sessionId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetAllByCheckoutSessionIdAsync START: SessionId={SessionId}",
                sessionId);

            try
            {
                var purchases = await _dbSet
                    .Where(p => p.StripeCheckoutSessionId == sessionId)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetAllByCheckoutSessionIdAsync COMPLETE: SessionId={SessionId}, Count={Count}, Duration={ElapsedMs}ms",
                    sessionId,
                    purchases.Count,
                    stopwatch.ElapsedMilliseconds);

                return purchases;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetAllByCheckoutSessionIdAsync FAILED: SessionId={SessionId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    sessionId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AddOnPurchase>> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEventId"))
        using (LogContext.PushProperty("EntityType", "AddOnPurchase"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByEventIdAsync START: EventId={EventId}",
                eventId);

            try
            {
                var purchases = await _dbSet
                    .AsNoTracking()
                    .Where(p => p.EventId == eventId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEventIdAsync COMPLETE: EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId,
                    purchases.Count,
                    stopwatch.ElapsedMilliseconds);

                return purchases;
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
    public async Task<IReadOnlyList<AddOnPurchase>> GetByDefinitionIdAsync(
        Guid definitionId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByDefinitionId"))
        using (LogContext.PushProperty("EntityType", "AddOnPurchase"))
        using (LogContext.PushProperty("DefinitionId", definitionId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByDefinitionIdAsync START: DefinitionId={DefinitionId}",
                definitionId);

            try
            {
                var purchases = await _dbSet
                    .AsNoTracking()
                    .Where(p => p.AddOnDefinitionId == definitionId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByDefinitionIdAsync COMPLETE: DefinitionId={DefinitionId}, Count={Count}, Duration={ElapsedMs}ms",
                    definitionId,
                    purchases.Count,
                    stopwatch.ElapsedMilliseconds);

                return purchases;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByDefinitionIdAsync FAILED: DefinitionId={DefinitionId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    definitionId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AddOnPurchase>> GetCompletedByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetCompletedByEventId"))
        using (LogContext.PushProperty("EntityType", "AddOnPurchase"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetCompletedByEventIdAsync START: EventId={EventId}",
                eventId);

            try
            {
                var purchases = await _dbSet
                    .AsNoTracking()
                    .Where(p => p.EventId == eventId && p.Status == AddOnPurchaseStatus.Completed)
                    .OrderByDescending(p => p.PaymentCompletedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetCompletedByEventIdAsync COMPLETE: EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId,
                    purchases.Count,
                    stopwatch.ElapsedMilliseconds);

                return purchases;
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
    public async Task<decimal> GetTotalPurchasesForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetTotalPurchasesForEvent"))
        using (LogContext.PushProperty("EntityType", "AddOnPurchase"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetTotalPurchasesForEventAsync START: EventId={EventId}",
                eventId);

            try
            {
                var total = await _dbSet
                    .Where(p => p.EventId == eventId && p.Status == AddOnPurchaseStatus.Completed)
                    .SumAsync(p => p.TotalAmount.Amount, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetTotalPurchasesForEventAsync COMPLETE: EventId={EventId}, Total={Total}, Duration={ElapsedMs}ms",
                    eventId,
                    total,
                    stopwatch.ElapsedMilliseconds);

                return total;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetTotalPurchasesForEventAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AddOnPurchase>> GetByUserIdAndEventIdAsync(
        Guid userId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByUserIdAndEventId"))
        using (LogContext.PushProperty("EntityType", "AddOnPurchase"))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByUserIdAndEventIdAsync START: UserId={UserId}, EventId={EventId}",
                userId, eventId);

            try
            {
                var purchases = await _dbSet
                    .AsNoTracking()
                    .Where(p => p.BuyerUserId == userId && p.EventId == eventId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByUserIdAndEventIdAsync COMPLETE: UserId={UserId}, EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    userId,
                    eventId,
                    purchases.Count,
                    stopwatch.ElapsedMilliseconds);

                return purchases;
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
    public async Task<IReadOnlyList<AddOnPurchase>> GetExpiredPendingPurchasesAsync(
        DateTime cutoffTime,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetExpiredPendingPurchases"))
        using (LogContext.PushProperty("EntityType", "AddOnPurchase"))
        using (LogContext.PushProperty("CutoffTime", cutoffTime))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetExpiredPendingPurchasesAsync START: CutoffTime={CutoffTime}",
                cutoffTime);

            try
            {
                var purchases = await _dbSet
                    .Where(p => p.Status == AddOnPurchaseStatus.Pending &&
                                p.CheckoutExpiresAt.HasValue &&
                                p.CheckoutExpiresAt.Value < cutoffTime)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetExpiredPendingPurchasesAsync COMPLETE: CutoffTime={CutoffTime}, Count={Count}, Duration={ElapsedMs}ms",
                    cutoffTime,
                    purchases.Count,
                    stopwatch.ElapsedMilliseconds);

                return purchases;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetExpiredPendingPurchasesAsync FAILED: CutoffTime={CutoffTime}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    cutoffTime,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AddOnPurchase>> GetByBuyerEmailAndEventIdAsync(
        string buyerEmail,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByBuyerEmailAndEventId"))
        using (LogContext.PushProperty("EntityType", "AddOnPurchase"))
        using (LogContext.PushProperty("BuyerEmail", buyerEmail))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetByBuyerEmailAndEventIdAsync START: BuyerEmail={BuyerEmail}, EventId={EventId}",
                buyerEmail, eventId);

            try
            {
                var normalizedEmail = buyerEmail.Trim().ToLowerInvariant();

                var purchases = await _dbSet
                    .AsNoTracking()
                    .Where(p => p.BuyerEmail == normalizedEmail && p.EventId == eventId
                                && (p.Status == AddOnPurchaseStatus.Completed || p.Status == AddOnPurchaseStatus.Pending))
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByBuyerEmailAndEventIdAsync COMPLETE: BuyerEmail={BuyerEmail}, EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    buyerEmail, eventId, purchases.Count, stopwatch.ElapsedMilliseconds);

                return purchases;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByBuyerEmailAndEventIdAsync FAILED: BuyerEmail={BuyerEmail}, EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    buyerEmail, eventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
