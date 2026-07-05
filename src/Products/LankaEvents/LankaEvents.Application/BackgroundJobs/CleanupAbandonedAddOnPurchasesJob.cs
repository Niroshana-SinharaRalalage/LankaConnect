using System.Diagnostics;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.BackgroundJobs;

/// <summary>
/// Background job that runs hourly to mark expired Pending add-on purchases as Abandoned.
///
/// Purpose: Clean up add-on purchases where buyers started Stripe checkout but never completed payment.
/// - Finds Pending purchases with CheckoutExpiresAt older than now
/// - Marks them as Abandoned
/// - Restores reserved stock back to the add-on definition
///
/// Schedule: Hourly (Stripe checkout sessions expire at 24 hours)
/// </summary>
public class CleanupAbandonedAddOnPurchasesJob
{
    private readonly IAddOnPurchaseRepository _addOnPurchaseRepository;
    private readonly IAddOnDefinitionRepository _addOnDefinitionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CleanupAbandonedAddOnPurchasesJob> _logger;

    public CleanupAbandonedAddOnPurchasesJob(
        IAddOnPurchaseRepository addOnPurchaseRepository,
        IAddOnDefinitionRepository addOnDefinitionRepository,
        IUnitOfWork unitOfWork,
        ILogger<CleanupAbandonedAddOnPurchasesJob> logger)
    {
        _addOnPurchaseRepository = addOnPurchaseRepository;
        _addOnDefinitionRepository = addOnDefinitionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        using (LogContext.PushProperty("Operation", "CleanupAbandonedAddOnPurchases"))
        using (LogContext.PushProperty("EntityType", "AddOnPurchase"))
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid();

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                _logger.LogInformation(
                    "[AddOnCleanup] [Cleanup-Job-START] Beginning cleanup of abandoned add-on purchases - CorrelationId={CorrelationId}",
                    correlationId);

                try
                {
                    var now = DateTime.UtcNow;

                    // Get all pending purchases with expired checkout sessions
                    var expiredPurchases = await _addOnPurchaseRepository.GetExpiredPendingPurchasesAsync(now);

                    if (expiredPurchases.Count == 0)
                    {
                        stopwatch.Stop();
                        _logger.LogInformation(
                            "[AddOnCleanup] [Cleanup-Job-COMPLETE] No abandoned add-on purchases found - CorrelationId={CorrelationId}, Duration={ElapsedMs}ms",
                            correlationId, stopwatch.ElapsedMilliseconds);
                        return;
                    }

                    _logger.LogInformation(
                        "[AddOnCleanup] [Cleanup-Job-2] Found {Count} expired Pending add-on purchases - CorrelationId={CorrelationId}",
                        expiredPurchases.Count, correlationId);

                    var abandonedCount = 0;
                    var stockRestoredCount = 0;
                    var failedCount = 0;

                    foreach (var purchase in expiredPurchases)
                    {
                        try
                        {
                            purchase.MarkAsAbandoned();

                            // Restore reserved stock back to the add-on definition
                            var stockRestored = await _addOnDefinitionRepository.TryRestoreStockAsync(
                                purchase.AddOnDefinitionId, purchase.Quantity);

                            if (stockRestored)
                            {
                                stockRestoredCount++;
                                _logger.LogInformation(
                                    "[AddOnCleanup] [Cleanup-Job-3] Stock restored - PurchaseId={PurchaseId}, DefinitionId={DefinitionId}, Quantity={Quantity}",
                                    purchase.Id, purchase.AddOnDefinitionId, purchase.Quantity);
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "[AddOnCleanup] [Cleanup-Job-WARN] Stock restore failed - PurchaseId={PurchaseId}, DefinitionId={DefinitionId}, Quantity={Quantity}",
                                    purchase.Id, purchase.AddOnDefinitionId, purchase.Quantity);
                            }

                            _addOnPurchaseRepository.Update(purchase);
                            abandonedCount++;
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            _logger.LogError(ex,
                                "[AddOnCleanup] [Cleanup-Job-Exception] Error processing purchase - PurchaseId={PurchaseId}, Error={ErrorMessage}",
                                purchase.Id, ex.Message);
                        }
                    }

                    if (abandonedCount > 0)
                    {
                        await _unitOfWork.CommitAsync();
                    }

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "[AddOnCleanup] [Cleanup-Job-COMPLETE] Cleanup finished - CorrelationId={CorrelationId}, Duration={ElapsedMs}ms, TotalFound={Total}, Abandoned={Abandoned}, StockRestored={StockRestored}, Failed={Failed}",
                        correlationId, stopwatch.ElapsedMilliseconds, expiredPurchases.Count, abandonedCount, stockRestoredCount, failedCount);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();

                    _logger.LogError(ex,
                        "[AddOnCleanup] [Cleanup-Job-FAILED] Job execution failed - CorrelationId={CorrelationId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                        correlationId, stopwatch.ElapsedMilliseconds, ex.Message);

                    throw; // Re-throw for Hangfire retry mechanism
                }
            }
        }
    }
}
