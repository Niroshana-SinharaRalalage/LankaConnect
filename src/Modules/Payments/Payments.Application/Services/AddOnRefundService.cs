using LankaConnect.Products.LankaEvents.Contracts.Repositories;
using LankaConnect.Products.LankaEvents.Contracts.Services;
using LankaConnect.Products.LankaEvents.Contracts.DTOs;
using LankaConnect.Products.LankaEvents.Contracts.Shims; // Wave 6.5.g Day 5 // W4.4.c.4: interfaces stay in legacy (4 cross-module consumers; circular ref otherwise)
using LankaConnect.SharedKernel.Identity;
using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Modules.Payments.Application.Services;

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
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public AddOnRefundService(
        IAddOnPurchaseRepository purchaseRepository,
        IAddOnDefinitionRepository definitionRepository,
        IStripePaymentService stripePaymentService,
        ILogger<AddOnRefundService> logger,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _purchaseRepository = purchaseRepository;
        _definitionRepository = definitionRepository;
        _stripePaymentService = stripePaymentService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<Result<AddOnRefundResult>> RefundUserPurchasesAsync(
        Guid userId,
        Guid eventId,
        string reason,
        Dictionary<string, string> metadata,
        Guid? registrationId = null,
        bool isPreApproved = false,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "RefundUserAddOnPurchases"))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "AddOnRefundService START: UserId={UserId}, EventId={EventId}, IsPreApproved={IsPreApproved}",
                userId, eventId, isPreApproved);

            try
            {
                // Step 1: Find all purchases for this user+event (AsNoTracking query)
                var purchases = await _purchaseRepository.GetByUserIdAndEventIdAsync(
                    userId, eventId, cancellationToken);

                // Filter to only Completed purchases with a Stripe PaymentIntentId.
                // Phase 6A.137F-Fix4: When registrationId is provided, scope ONLY to current registration's
                // bundled purchases. Removes the `!p.RegistrationId.HasValue` fallback that incorrectly
                // included standalone purchases from old sessions and orphaned purchases from deleted registrations.
                var refundablePurchases = purchases
                    .Where(p => p.Status == AddOnPurchaseStatus.Completed
                             && !string.IsNullOrEmpty(p.StripePaymentIntentId))
                    .Where(p => registrationId == null
                             || p.RegistrationId == registrationId)
                    .ToList();

                if (refundablePurchases.Count == 0)
                {
                    stopwatch.Stop();

                    _logger.LogInformation(
                        "AddOnRefundService COMPLETE: No refundable purchases found - UserId={UserId}, EventId={EventId}, TotalPurchases={Total}, Duration={ElapsedMs}ms",
                        userId, eventId, purchases.Count, stopwatch.ElapsedMilliseconds);

                    return Result<AddOnRefundResult>.Success(new AddOnRefundResult(0, 0m, 0));
                }

                // 2026-06-28 fix: Phase 6A.148 approval-workflow interlock moved BELOW the
                // empty-purchases short-circuit. Previously this check fired before we
                // queried the DB, so cancelling a registration with ZERO add-on purchases
                // (e.g. every free event) returned a Failure result and surfaced an alarming
                // "Direct add-on refunds are blocked" warning to the user via the UI's
                // partial-failure summary. Founder caught this 2026-06-28 cancelling a
                // free event registration. With the check after the empty-list check, free
                // and no-add-on cancellations now return clean Success(0,0,0) without
                // touching the approval-workflow interlock.
                var approvalFlagOn = _configuration.GetValue<bool>("Refund:ApprovalWorkflow:Enabled");
                if (approvalFlagOn && !isPreApproved)
                {
                    _logger.LogWarning(
                        "[6A.148 INTERLOCK] BLOCKED RefundUserPurchasesAsync called without isPreApproved while approval workflow is ON. " +
                        "UserId={UserId} EventId={EventId} RefundableCount={RefundableCount} — caller must route via RefundExecutionService.",
                        userId, eventId, refundablePurchases.Count);
                    return Result<AddOnRefundResult>.Failure(
                        "Refund-approval workflow is enabled. Direct add-on refunds are blocked.");
                }

                _logger.LogInformation(
                    "AddOnRefundService: Found {RefundableCount} refundable purchases out of {TotalCount} - UserId={UserId}, EventId={EventId}",
                    refundablePurchases.Count, purchases.Count, userId, eventId);

                int refunded = 0;
                int failed = 0;
                decimal totalAmountRefunded = 0m;

                // Phase 6A.137F-Fix2: Group bundled purchases by PaymentIntentId.
                // Bundled purchases share a single PaymentIntent (Stripe charge).
                // Issuing individual refunds per purchase causes charge_already_refunded errors
                // because the 1st refund may consume the full charge. Instead, issue ONE partial
                // refund per PaymentIntent for the combined amount of all add-on purchases in that group.
                var groupedByPI = refundablePurchases
                    .GroupBy(p => p.StripePaymentIntentId!)
                    .ToList();

                foreach (var piGroup in groupedByPI)
                {
                    var paymentIntentId = piGroup.Key;
                    var purchasesInGroup = piGroup.ToList();
                    var isBundledGroup = purchasesInGroup.Any(p => p.IsBundled);
                    var groupTotal = purchasesInGroup.Sum(p => p.TotalAmount.Amount);

                    try
                    {
                        // Step 2a: Call Stripe refund — one call per PaymentIntent group
                        // Bundled: partial refund for combined add-on amount (other items on same PI are not refunded here)
                        // Standalone: full refund (null = refund entire PI)
                        var refundAmountInCents = isBundledGroup
                            ? (long?)(long)(groupTotal * 100)
                            : (long?)null;

                        var refundRequest = new CreateRefundRequest
                        {
                            PaymentIntentId = paymentIntentId,
                            RegistrationId = purchasesInGroup.First().Id, // Use first purchase ID for idempotency
                            AmountInCents = refundAmountInCents,
                            Reason = reason,
                            Metadata = new Dictionary<string, string>(metadata)
                            {
                                ["add_on_purchase_ids"] = string.Join(",", purchasesInGroup.Select(p => p.Id)),
                                ["refund_type"] = "add_on_cancellation",
                                ["purchase_count"] = purchasesInGroup.Count.ToString()
                            }
                        };

                        _logger.LogInformation(
                            "AddOnRefundService: Issuing {RefundType} refund for {Count} purchases - PaymentIntentId={PaymentIntentId}, Amount={Amount}",
                            isBundledGroup ? "partial" : "full", purchasesInGroup.Count, paymentIntentId, groupTotal);

                        var stripeResult = await _stripePaymentService.CreateRefundAsync(refundRequest);

                        if (stripeResult.IsFailure)
                        {
                            _logger.LogError(
                                "AddOnRefundService: Stripe refund failed - PaymentIntentId={PaymentIntentId}, PurchaseCount={Count}, Error={Error}",
                                paymentIntentId, purchasesInGroup.Count, stripeResult.Error);
                            failed += purchasesInGroup.Count;
                            continue;
                        }

                        _logger.LogInformation(
                            "AddOnRefundService: Stripe refund created - PaymentIntentId={PaymentIntentId}, RefundId={RefundId}, Status={Status}, PurchaseCount={Count}",
                            paymentIntentId, stripeResult.Value.RefundId, stripeResult.Value.Status, purchasesInGroup.Count);

                        // Step 2b: Mark all purchases in group as refunded + restore stock
                        foreach (var readOnlyPurchase in purchasesInGroup)
                        {
                            try
                            {
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

                                // Restore stock
                                var stockRestored = await _definitionRepository.TryRestoreStockAsync(
                                    purchase.AddOnDefinitionId, purchase.Quantity, cancellationToken);

                                if (!stockRestored)
                                {
                                    _logger.LogWarning(
                                        "AddOnRefundService: Stock restore failed (non-fatal) - PurchaseId={PurchaseId}, DefinitionId={DefinitionId}",
                                        purchase.Id, purchase.AddOnDefinitionId);
                                }

                                refunded++;
                                totalAmountRefunded += purchase.TotalAmount.Amount;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex,
                                    "AddOnRefundService: Error marking purchase as refunded - PurchaseId={PurchaseId}",
                                    readOnlyPurchase.Id);
                                failed++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "AddOnRefundService: Unexpected error processing PI group - PaymentIntentId={PaymentIntentId}, PurchaseCount={Count}",
                            paymentIntentId, purchasesInGroup.Count);
                        failed += purchasesInGroup.Count;
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
