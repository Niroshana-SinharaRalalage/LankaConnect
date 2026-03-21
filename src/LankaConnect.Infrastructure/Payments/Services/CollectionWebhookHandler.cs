using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Payments.Services;

/// <summary>
/// Phase 4: Handles Stripe webhook events for collection (event fund) payments.
/// Follows DonationWebhookHandler pattern.
/// Errors are swallowed to prevent HTTP 500 to Stripe (collection stays Pending; expiry cleanup handles it).
/// </summary>
public class CollectionWebhookHandler : ICollectionWebhookHandler
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CollectionWebhookHandler> _logger;

    public CollectionWebhookHandler(
        ICollectionRepository collectionRepository,
        IUnitOfWork unitOfWork,
        ILogger<CollectionWebhookHandler> logger)
    {
        _collectionRepository = collectionRepository;
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
                "[Collection] [Webhook-Collection-1] Processing collection payment - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                correlationId, sessionId);

            // Load collection by checkout session (safer than metadata lookup)
            var collection = await _collectionRepository.GetByCheckoutSessionIdAsync(sessionId, ct);
            if (collection == null)
            {
                _logger.LogError(
                    "[Collection] [Webhook-Collection-ERROR] Collection not found by session - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, sessionId);
                return;
            }

            _logger.LogInformation(
                "[Collection] [Webhook-Collection-2] Collection loaded - CorrelationId: {CorrelationId}, CollectionId: {CollectionId}, Status: {Status}",
                correlationId, collection.Id, collection.Status);

            // Verify the collection is still pending (idempotency check)
            if (collection.Status != CollectionStatus.Pending)
            {
                _logger.LogWarning(
                    "[Collection] [Webhook-Collection-WARN] Collection not in Pending status (idempotent skip) - CorrelationId: {CorrelationId}, CollectionId: {CollectionId}, CurrentStatus: {Status}",
                    correlationId, collection.Id, collection.Status);
                return;
            }

            // Complete payment on the collection entity
            var completeResult = collection.CompletePayment(paymentIntentId);

            if (completeResult.IsFailure)
            {
                _logger.LogError(
                    "[Collection] [Webhook-Collection-ERROR] CompletePayment failed - CorrelationId: {CorrelationId}, CollectionId: {CollectionId}, Error: {Error}",
                    correlationId, collection.Id, completeResult.Error);
                return;
            }

            _logger.LogInformation(
                "[Collection] [Webhook-Collection-3] Payment completed on collection - CorrelationId: {CorrelationId}, CollectionId: {CollectionId}, PaymentIntentId: {PaymentIntentId}",
                correlationId, collection.Id, paymentIntentId);

            // Save changes and dispatch domain events
            _collectionRepository.Update(collection);
            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "[Collection] [Webhook-Collection-SUCCESS] Collection payment completed successfully - CorrelationId: {CorrelationId}, CollectionId: {CollectionId}, PaymentIntentId: {PaymentIntentId}",
                correlationId, collection.Id, paymentIntentId);
        }
        catch (Exception ex)
        {
            // Swallow collection errors: failures should NOT return HTTP 500
            // to Stripe (causes retry storms). Collection stays Pending; expiry cleanup handles it.
            _logger.LogError(ex,
                "[Collection] [Webhook-Collection-ERROR] Error handling collection checkout (swallowed) - CorrelationId: {CorrelationId}, Type: {ExceptionType}, Message: {Message}",
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
                "[Collection] [Webhook-Expired-Collection-1] Processing collection expiry - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                correlationId, sessionId);

            var collection = await _collectionRepository.GetByCheckoutSessionIdAsync(sessionId, ct);
            if (collection == null)
            {
                _logger.LogWarning(
                    "[Collection] [Webhook-Expired-Collection-WARN] Collection not found by session - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, sessionId);
                return;
            }

            if (collection.Status != CollectionStatus.Pending)
            {
                _logger.LogWarning(
                    "[Collection] [Webhook-Expired-Collection-WARN] Collection not in Pending status - CorrelationId: {CorrelationId}, CollectionId: {CollectionId}, Status: {Status}",
                    correlationId, collection.Id, collection.Status);
                return;
            }

            collection.MarkAsAbandoned();
            _collectionRepository.Update(collection);
            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "[Collection] [Webhook-Expired-Collection-SUCCESS] Collection abandoned - CorrelationId: {CorrelationId}, CollectionId: {CollectionId}",
                correlationId, collection.Id);
        }
        catch (Exception ex)
        {
            // Swallow: collection expiry failure should NOT return HTTP 500 to Stripe.
            // Collection stays Pending and can be cleaned up by background job.
            _logger.LogError(ex,
                "[Collection] [Webhook-Expired-Collection-ERROR] Error handling collection expiry (swallowed) - CorrelationId: {CorrelationId}, Type: {ExceptionType}, Message: {Message}",
                correlationId, ex.GetType().FullName, ex.Message);
        }
    }
}
