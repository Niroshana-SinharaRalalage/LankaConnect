using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Services;

/// <summary>
/// Processes add-on purchase refunds for a user during event cancellation.
/// Pattern mirrors RegistrationRefundService: Stripe refund → domain transition → stock restore.
/// Partial failure tolerant — continues processing if individual refunds fail.
/// </summary>
public class AddOnRefundService : IAddOnRefundService
{
    private readonly IAddOnPurchaseRepository _purchaseRepository;
    private readonly IAddOnDefinitionRepository _definitionRepository;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly ILogger<AddOnRefundService> _logger;

    public AddOnRefundService(
        IAddOnPurchaseRepository purchaseRepository,
        IAddOnDefinitionRepository definitionRepository,
        IStripePaymentService stripePaymentService,
        ILogger<AddOnRefundService> logger)
    {
        _purchaseRepository = purchaseRepository;
        _definitionRepository = definitionRepository;
        _stripePaymentService = stripePaymentService;
        _logger = logger;
    }

    public async Task<Result<AddOnRefundResult>> RefundUserPurchasesAsync(
        Guid userId,
        Guid eventId,
        string reason,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "RefundUserAddOnPurchases"))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "AddOnRefundService START: UserId={UserId}, EventId={EventId}",
                userId, eventId);

            try
            {
                // Step 1: Find all purchases for this user+event (AsNoTracking query)
                var purchases = await _purchaseRepository.GetByUserIdAndEventIdAsync(
                    userId, eventId, cancellationToken);

                // Filter to only Completed purchases with a Stripe PaymentIntentId
                var refundablePurchases = purchases
                    .Where(p => p.Status == AddOnPurchaseStatus.Completed
                             && !string.IsNullOrEmpty(p.StripePaymentIntentId))
                    .ToList();

                if (refundablePurchases.Count == 0)
                {
                    stopwatch.Stop();

                    _logger.LogInformation(
                        "AddOnRefundService COMPLETE: No refundable purchases found - UserId={UserId}, EventId={EventId}, TotalPurchases={Total}, Duration={ElapsedMs}ms",
                        userId, eventId, purchases.Count, stopwatch.ElapsedMilliseconds);

                    return Result<AddOnRefundResult>.Success(new AddOnRefundResult(0, 0m, 0));
                }

                _logger.LogInformation(
                    "AddOnRefundService: Found {RefundableCount} refundable purchases out of {TotalCount} - UserId={UserId}, EventId={EventId}",
                    refundablePurchases.Count, purchases.Count, userId, eventId);

                int refunded = 0;
                int failed = 0;
                decimal totalAmountRefunded = 0m;

                // Step 2: Process each refundable purchase
                foreach (var readOnlyPurchase in refundablePurchases)
                {
                    try
                    {
                        // Re-fetch with tracking so EF Core can persist status change
                        var purchase = await _purchaseRepository.GetByIdAsync(
                            readOnlyPurchase.Id, cancellationToken);

                        if (purchase == null)
                        {
                            _logger.LogWarning(
                                "AddOnRefundService: Could not re-fetch purchase with tracking - PurchaseId={PurchaseId}",
                                readOnlyPurchase.Id);
                            failed++;
                            continue;
                        }

                        // Step 2a: Call Stripe refund
                        // Phase 6A.135: Use purchase.Id as RegistrationId so the idempotency key
                        // ($"refund_{PaymentIntentId}") is unique per PaymentIntent.
                        // Previously RegistrationId=Guid.Empty caused global idempotency collision.
                        var refundRequest = new CreateRefundRequest
                        {
                            PaymentIntentId = purchase.StripePaymentIntentId!,
                            RegistrationId = purchase.Id, // Use purchase ID for metadata tracking
                            AmountInCents = null, // Full refund
                            Reason = reason,
                            Metadata = new Dictionary<string, string>(metadata)
                            {
                                ["add_on_purchase_id"] = purchase.Id.ToString(),
                                ["refund_type"] = "add_on_cancellation"
                            }
                        };

                        var stripeResult = await _stripePaymentService.CreateRefundAsync(refundRequest);

                        if (stripeResult.IsFailure)
                        {
                            _logger.LogError(
                                "AddOnRefundService: Stripe refund failed - PurchaseId={PurchaseId}, PaymentIntentId={PaymentIntentId}, Error={Error}",
                                purchase.Id, purchase.StripePaymentIntentId, stripeResult.Error);
                            failed++;
                            continue;
                        }

                        _logger.LogInformation(
                            "AddOnRefundService: Stripe refund created - PurchaseId={PurchaseId}, RefundId={RefundId}, Status={Status}",
                            purchase.Id, stripeResult.Value.RefundId, stripeResult.Value.Status);

                        // Step 2b: Mark purchase as refunded (domain transition)
                        var markResult = purchase.MarkAsRefunded();
                        if (markResult.IsFailure)
                        {
                            _logger.LogError(
                                "AddOnRefundService: Domain transition failed - PurchaseId={PurchaseId}, Error={Error}",
                                purchase.Id, markResult.Error);
                            failed++;
                            continue;
                        }

                        _purchaseRepository.Update(purchase);

                        // Step 2c: Restore stock
                        var stockRestored = await _definitionRepository.TryRestoreStockAsync(
                            purchase.AddOnDefinitionId, purchase.Quantity, cancellationToken);

                        if (!stockRestored)
                        {
                            _logger.LogWarning(
                                "AddOnRefundService: Stock restore failed (non-fatal) - PurchaseId={PurchaseId}, DefinitionId={DefinitionId}, Quantity={Quantity}",
                                purchase.Id, purchase.AddOnDefinitionId, purchase.Quantity);
                        }
                        else
                        {
                            _logger.LogInformation(
                                "AddOnRefundService: Stock restored - PurchaseId={PurchaseId}, DefinitionId={DefinitionId}, Quantity={Quantity}",
                                purchase.Id, purchase.AddOnDefinitionId, purchase.Quantity);
                        }

                        refunded++;
                        totalAmountRefunded += purchase.TotalAmount.Amount;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "AddOnRefundService: Unexpected error processing purchase - PurchaseId={PurchaseId}, Error={ErrorMessage}",
                            readOnlyPurchase.Id, ex.Message);
                        failed++;
                    }
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "AddOnRefundService COMPLETE: Refunded={Refunded}, Failed={Failed}, TotalAmount=${TotalAmount}, Duration={ElapsedMs}ms",
                    refunded, failed, totalAmountRefunded, stopwatch.ElapsedMilliseconds);

                return Result<AddOnRefundResult>.Success(
                    new AddOnRefundResult(refunded, totalAmountRefunded, failed));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "AddOnRefundService FAILED: UserId={UserId}, EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    userId, eventId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result<AddOnRefundResult>.Failure($"Add-on refund failed: {ex.Message}");
            }
        }
    }
}
