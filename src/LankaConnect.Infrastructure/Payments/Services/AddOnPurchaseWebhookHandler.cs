using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Payments.Services;

/// <summary>
/// Phase 4: Handles Stripe webhook events for add-on purchase payments.
/// Follows DonationWebhookHandler pattern with additional stock restoration on failure/expiry.
/// Errors are swallowed to prevent HTTP 500 to Stripe (purchase stays Pending; expiry cleanup handles it).
/// </summary>
public class AddOnPurchaseWebhookHandler : IAddOnPurchaseWebhookHandler
{
    private readonly IAddOnPurchaseRepository _addOnPurchaseRepository;
    private readonly IAddOnDefinitionRepository _addOnDefinitionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddOnPurchaseWebhookHandler> _logger;

    public AddOnPurchaseWebhookHandler(
        IAddOnPurchaseRepository addOnPurchaseRepository,
        IAddOnDefinitionRepository addOnDefinitionRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddOnPurchaseWebhookHandler> logger)
    {
        _addOnPurchaseRepository = addOnPurchaseRepository;
        _addOnDefinitionRepository = addOnDefinitionRepository;
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

            // Load purchase by checkout session
            var purchase = await _addOnPurchaseRepository.GetByCheckoutSessionIdAsync(sessionId, ct);
            if (purchase == null)
            {
                _logger.LogError(
                    "[AddOnPurchase] [Webhook-AddOn-ERROR] Purchase not found by session - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, sessionId);
                return;
            }

            _logger.LogInformation(
                "[AddOnPurchase] [Webhook-AddOn-2] Purchase loaded - CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}, Status: {Status}, Quantity: {Quantity}",
                correlationId, purchase.Id, purchase.Status, purchase.Quantity);

            // Verify the purchase is still pending (idempotency check)
            if (purchase.Status != AddOnPurchaseStatus.Pending)
            {
                _logger.LogWarning(
                    "[AddOnPurchase] [Webhook-AddOn-WARN] Purchase not in Pending status (idempotent skip) - CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}, CurrentStatus: {Status}",
                    correlationId, purchase.Id, purchase.Status);
                return;
            }

            // Complete payment on the purchase entity
            var completeResult = purchase.CompletePayment(paymentIntentId);

            if (completeResult.IsFailure)
            {
                _logger.LogError(
                    "[AddOnPurchase] [Webhook-AddOn-ERROR] CompletePayment failed - CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}, Error: {Error}",
                    correlationId, purchase.Id, completeResult.Error);
                return;
            }

            _logger.LogInformation(
                "[AddOnPurchase] [Webhook-AddOn-3] Payment completed on purchase - CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}, PaymentIntentId: {PaymentIntentId}",
                correlationId, purchase.Id, paymentIntentId);

            // Save changes and dispatch domain events
            _addOnPurchaseRepository.Update(purchase);
            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "[AddOnPurchase] [Webhook-AddOn-SUCCESS] Add-on purchase completed successfully - CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}, PaymentIntentId: {PaymentIntentId}",
                correlationId, purchase.Id, paymentIntentId);
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

            var purchase = await _addOnPurchaseRepository.GetByCheckoutSessionIdAsync(sessionId, ct);
            if (purchase == null)
            {
                _logger.LogWarning(
                    "[AddOnPurchase] [Webhook-Expired-AddOn-WARN] Purchase not found by session - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, sessionId);
                return;
            }

            if (purchase.Status != AddOnPurchaseStatus.Pending)
            {
                _logger.LogWarning(
                    "[AddOnPurchase] [Webhook-Expired-AddOn-WARN] Purchase not in Pending status - CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}, Status: {Status}",
                    correlationId, purchase.Id, purchase.Status);
                return;
            }

            purchase.MarkAsAbandoned();

            // Restore reserved stock back to the add-on definition
            var stockRestored = await _addOnDefinitionRepository.TryRestoreStockAsync(
                purchase.AddOnDefinitionId, purchase.Quantity, ct);

            if (stockRestored)
            {
                _logger.LogInformation(
                    "[AddOnPurchase] [Webhook-Expired-AddOn-2] Stock restored - CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}, DefinitionId: {DefinitionId}, Quantity: {Quantity}",
                    correlationId, purchase.Id, purchase.AddOnDefinitionId, purchase.Quantity);
            }
            else
            {
                _logger.LogWarning(
                    "[AddOnPurchase] [Webhook-Expired-AddOn-WARN] Stock restore failed - CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}, DefinitionId: {DefinitionId}, Quantity: {Quantity}",
                    correlationId, purchase.Id, purchase.AddOnDefinitionId, purchase.Quantity);
            }

            _addOnPurchaseRepository.Update(purchase);
            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "[AddOnPurchase] [Webhook-Expired-AddOn-SUCCESS] Add-on purchase abandoned, stock restored - CorrelationId: {CorrelationId}, PurchaseId: {PurchaseId}",
                correlationId, purchase.Id);
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
}
