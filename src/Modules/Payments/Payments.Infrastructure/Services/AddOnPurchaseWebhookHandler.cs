using LankaConnect.Products.LankaEvents.Contracts.LegacyPromotions; // W4.4.d.2
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Modules.Payments.Infrastructure.Services;

/// <summary>
/// Phase 4: Handles Stripe webhook events for add-on purchase payments.
/// Supports both single-item and cart (multi-item) purchases.
/// Cart purchases share the same StripeCheckoutSessionId across N AddOnPurchase rows.
/// Errors are swallowed to prevent HTTP 500 to Stripe (purchase stays Pending; expiry cleanup handles it).
/// Phase 6A.148.W4.D11: Added HandleChargeRefundedAsync to close the F2 root cause — the
/// PaymentsController switch case for add_on refunds was a NO-OP, leaving AddOnPurchase rows
/// stuck in Completed even after Stripe successfully refunded. Cart-aware via the workflow
/// line-item's ReferenceId; legacy fallback refunds all purchases sharing the PaymentIntent.
/// </summary>
public class AddOnPurchaseWebhookHandler : IAddOnPurchaseWebhookHandler
{
    private readonly IAddOnPurchaseRepository _addOnPurchaseRepository;
    private readonly IAddOnDefinitionRepository _addOnDefinitionRepository;
    private readonly IRefundRequestRepository _refundRequestRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddOnPurchaseWebhookHandler> _logger;

    public AddOnPurchaseWebhookHandler(
        IAddOnPurchaseRepository addOnPurchaseRepository,
        IAddOnDefinitionRepository addOnDefinitionRepository,
        IRefundRequestRepository refundRequestRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddOnPurchaseWebhookHandler> logger)
    {
        _addOnPurchaseRepository = addOnPurchaseRepository;
        _addOnDefinitionRepository = addOnDefinitionRepository;
        _refundRequestRepository = refundRequestRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleCheckoutCompletedAsync(
        string sessionId,
        string paymentIntentId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "[AddOnPurchase] [Webhook-AddOn-1] Processing add-on purchase payment - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                correlationId, sessionId);

            // Load ALL purchases for this session (supports both single and cart)
            var purchases = await _addOnPurchaseRepository.GetAllByCheckoutSessionIdAsync(sessionId, ct);
            if (purchases.Count == 0)
            {
                _logger.LogError(
                    "[AddOnPurchase] [Webhook-AddOn-ERROR] No purchases found by session - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, sessionId);
                return;
            }

            _logger.LogInformation(
                "[AddOnPurchase] [Webhook-AddOn-2] Loaded {PurchaseCount} purchase(s) for session - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                purchases.Count, correlationId, sessionId);

            var completedCount = 0;
            var skippedCount = 0;

            foreach (var purchase in purchases)
            {
                // Verify the purchase is still pending (idempotency check)
                if (purchase.Status != AddOnPurchaseStatus.Pending)
                {
                    _logger.LogWarning(
                        "[AddOnPurchase] [Webhook-AddOn-WARN] Purchase not in Pending status (idempotent skip) - CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}, CurrentStatus: {Status}",
                        correlationId, purchase.Id, purchase.Status);
                    skippedCount++;
                    continue;
                }

                // Complete payment on the purchase entity
                var completeResult = purchase.CompletePayment(paymentIntentId);

                if (completeResult.IsFailure)
                {
                    _logger.LogError(
                        "[AddOnPurchase] [Webhook-AddOn-ERROR] CompletePayment failed - CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}, Error: {Error}",
                        correlationId, purchase.Id, completeResult.Error);
                    continue;
                }

                _addOnPurchaseRepository.Update(purchase);
                completedCount++;

                _logger.LogInformation(
                    "[AddOnPurchase] [Webhook-AddOn-3] Payment completed on purchase - CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}, PaymentIntentId: {PaymentIntentId}",
                    correlationId, purchase.Id, paymentIntentId);
            }

            // Save all changes in a single commit
            if (completedCount > 0)
                await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "[AddOnPurchase] [Webhook-AddOn-SUCCESS] Add-on purchase(s) completed - CorrelationId: {CorrelationId}, Completed: {CompletedCount}, Skipped: {SkippedCount}, PaymentIntentId: {PaymentIntentId}",
                correlationId, completedCount, skippedCount, paymentIntentId);
        }
        catch (Exception ex)
        {
            // Swallow add-on purchase errors: failures should NOT return HTTP 500
            // to Stripe (causes retry storms). Purchase stays Pending; expiry cleanup handles it.
            _logger.LogError(ex,
                "[AddOnPurchase] [Webhook-AddOn-ERROR] Error handling add-on purchase checkout (swallowed) - CorrelationId: {CorrelationId}, Type: {ExceptionType}, Message: {Message}",
                correlationId, ex.GetType().FullName, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task HandleCheckoutExpiredAsync(
        string sessionId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "[AddOnPurchase] [Webhook-Expired-AddOn-1] Processing add-on purchase expiry - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                correlationId, sessionId);

            // Load ALL purchases for this session (supports both single and cart)
            var purchases = await _addOnPurchaseRepository.GetAllByCheckoutSessionIdAsync(sessionId, ct);
            if (purchases.Count == 0)
            {
                _logger.LogWarning(
                    "[AddOnPurchase] [Webhook-Expired-AddOn-WARN] No purchases found by session - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, sessionId);
                return;
            }

            _logger.LogInformation(
                "[AddOnPurchase] [Webhook-Expired-AddOn-2] Loaded {PurchaseCount} purchase(s) for expiry - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                purchases.Count, correlationId, sessionId);

            var abandonedCount = 0;

            foreach (var purchase in purchases)
            {
                if (purchase.Status != AddOnPurchaseStatus.Pending)
                {
                    _logger.LogWarning(
                        "[AddOnPurchase] [Webhook-Expired-AddOn-WARN] Purchase not in Pending status - CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}, Status: {Status}",
                        correlationId, purchase.Id, purchase.Status);
                    continue;
                }

                purchase.MarkAsAbandoned();

                // Restore reserved stock back to the add-on definition
                var stockRestored = await _addOnDefinitionRepository.TryRestoreStockAsync(
                    purchase.AddOnDefinitionId, purchase.Quantity, ct);

                if (stockRestored)
                {
                    _logger.LogInformation(
                        "[AddOnPurchase] [Webhook-Expired-AddOn-3] Stock restored - CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}, DefinitionId: {DefinitionId}, Quantity: {Quantity}",
                        correlationId, purchase.Id, purchase.AddOnDefinitionId, purchase.Quantity);
                }
                else
                {
                    _logger.LogWarning(
                        "[AddOnPurchase] [Webhook-Expired-AddOn-WARN] Stock restore failed - CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}, DefinitionId: {DefinitionId}, Quantity: {Quantity}",
                        correlationId, purchase.Id, purchase.AddOnDefinitionId, purchase.Quantity);
                }

                _addOnPurchaseRepository.Update(purchase);
                abandonedCount++;
            }

            if (abandonedCount > 0)
                await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "[AddOnPurchase] [Webhook-Expired-AddOn-SUCCESS] Add-on purchase(s) abandoned, stock restored - CorrelationId: {CorrelationId}, AbandonedCount: {AbandonedCount}",
                correlationId, abandonedCount);
        }
        catch (Exception ex)
        {
            // Swallow: add-on purchase expiry failure should NOT return HTTP 500 to Stripe.
            // Purchase stays Pending and can be cleaned up by background job.
            // Stock may not be restored - manual intervention may be needed.
            _logger.LogError(ex,
                "[AddOnPurchase] [Webhook-Expired-AddOn-ERROR] Error handling add-on purchase expiry (swallowed) - CorrelationId: {CorrelationId}, Type: {ExceptionType}, Message: {Message}",
                correlationId, ex.GetType().FullName, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task HandleChargeRefundedAsync(
        string paymentIntentId,
        string refundId,
        Guid correlationId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "[6A.148.W4.D11] [Webhook-AddOn-Refund-1] Processing add-on refund - CorrelationId: {CorrelationId}, PaymentIntentId: {PaymentIntentId}, RefundId: {RefundId}",
                correlationId, paymentIntentId, refundId);

            // Resolve the candidate AddOnPurchase rows. For carts, multiple rows share the
            // same PaymentIntent — but only ONE corresponds to this specific Stripe refund
            // (each line-item triggers its own refund call under the workflow path).
            var candidates = await _addOnPurchaseRepository
                .GetAllByStripePaymentIntentIdAsync(paymentIntentId, ct);

            if (candidates.Count == 0)
            {
                _logger.LogWarning(
                    "[6A.148.W4.D11] [Webhook-AddOn-Refund-WARN] No AddOnPurchase found for PaymentIntentId — CorrelationId: {CorrelationId}, PaymentIntentId: {PaymentIntentId}",
                    correlationId, paymentIntentId);
                return;
            }

            // Try to narrow to the exact purchase via the workflow line-item's ReferenceId.
            // Fail-OPEN: if the lookup throws (transient DB issue) we fall back to legacy
            // semantics rather than silencing the refund. The cost of accidentally marking
            // extra purchases Refunded is bounded — they're all on the same PaymentIntent
            // and the refund call already succeeded at Stripe.
            Guid? workflowReferenceId = null;
            try
            {
                workflowReferenceId = await _refundRequestRepository
                    .GetWorkflowLineReferenceIdAsync(
                        LankaConnect.Products.LankaEvents.Domain.Enums.RefundLineItemType.AddOn,
                        refundId,
                        ct);
            }
            catch (Exception lookupEx)
            {
                _logger.LogWarning(lookupEx,
                    "[6A.148.W4.D11] [Webhook-AddOn-Refund-WARN] Workflow line lookup threw — falling back to legacy cart semantics. CorrelationId: {CorrelationId}, RefundId: {RefundId}",
                    correlationId, refundId);
            }

            var targets = workflowReferenceId.HasValue
                ? candidates.Where(p => p.Id == workflowReferenceId.Value).ToList()
                : candidates.ToList();

            if (targets.Count == 0)
            {
                _logger.LogWarning(
                    "[6A.148.W4.D11] [Webhook-AddOn-Refund-WARN] Workflow ReferenceId={ReferenceId} did not match any candidate purchase under PaymentIntentId={PaymentIntentId} — CorrelationId: {CorrelationId}",
                    workflowReferenceId, paymentIntentId, correlationId);
                return;
            }

            var refundedCount = 0;
            var skippedCount = 0;

            foreach (var purchase in targets)
            {
                // Idempotency: skip purchases already in a terminal refund state.
                if (purchase.Status == AddOnPurchaseStatus.Refunded)
                {
                    _logger.LogInformation(
                        "[6A.148.W4.D11] [Webhook-AddOn-Refund-IDEMPOTENT] Purchase already Refunded (idempotent skip) — CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}",
                        correlationId, purchase.Id);
                    skippedCount++;
                    continue;
                }

                var refundResult = purchase.MarkAsRefunded();
                if (refundResult.IsFailure)
                {
                    _logger.LogWarning(
                        "[6A.148.W4.D11] [Webhook-AddOn-Refund-WARN] MarkAsRefunded failed — CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}, Status: {Status}, Error: {Error}",
                        correlationId, purchase.Id, purchase.Status, refundResult.Error);
                    skippedCount++;
                    continue;
                }

                _addOnPurchaseRepository.Update(purchase);
                refundedCount++;

                _logger.LogInformation(
                    "[6A.148.W4.D11] [Webhook-AddOn-Refund-2] AddOnPurchase marked Refunded — CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}, RefundId: {RefundId}, WorkflowOwned: {WorkflowOwned}",
                    correlationId, purchase.Id, refundId, workflowReferenceId.HasValue);
            }

            if (refundedCount > 0)
                await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "[6A.148.W4.D11] [Webhook-AddOn-Refund-SUCCESS] Add-on refund processed — CorrelationId: {CorrelationId}, Refunded: {Refunded}, Skipped: {Skipped}, WorkflowOwned: {WorkflowOwned}, RefundId: {RefundId}",
                correlationId, refundedCount, skippedCount, workflowReferenceId.HasValue, refundId);
        }
        catch (Exception ex)
        {
            // Swallow: returning HTTP 500 to Stripe causes retry storms and dropped events.
            // The AddOnPurchase stays Completed; manual reconciliation needed if this happens.
            _logger.LogError(ex,
                "[6A.148.W4.D11] [Webhook-AddOn-Refund-CRITICAL] Error handling add-on refund (swallowed) — CorrelationId: {CorrelationId}, PaymentIntentId: {PaymentIntentId}, RefundId: {RefundId}",
                correlationId, paymentIntentId, refundId);
        }
    }
}
