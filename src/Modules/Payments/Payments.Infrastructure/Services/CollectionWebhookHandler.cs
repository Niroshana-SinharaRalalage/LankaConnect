using LankaConnect.Modules.Payments.Domain.Repositories; // W4.4.d.2
using LankaConnect.Products.LankaEvents.Application.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Modules.Payments.Infrastructure.Services;

/// <summary>
/// Phase 4: Handles Stripe webhook events for collection (event fund) payments.
/// Follows DonationWebhookHandler pattern.
/// Errors are swallowed to prevent HTTP 500 to Stripe (collection stays Pending; expiry cleanup handles it).
/// Phase 6A.137B2: Added refund email notification via fire-and-forget.
/// Phase 6A.148.W4.D12 (G4): Added workflow-owned dedupe guard — suppresses the legacy
/// per-Collection "Refund Confirmation" email when the consolidated D8 decision email
/// has already covered the attendee. Fail-OPEN on lookup exception (matches the D9
/// pattern from SponsorWebhookHandler).
/// </summary>
public class CollectionWebhookHandler : ICollectionWebhookHandler
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRefundRequestRepository _refundRequestRepository;
    private readonly ILogger<CollectionWebhookHandler> _logger;

    public CollectionWebhookHandler(
        ICollectionRepository collectionRepository,
        IUnitOfWork unitOfWork,
        IServiceScopeFactory scopeFactory,
        IRefundRequestRepository refundRequestRepository,
        ILogger<CollectionWebhookHandler> logger)
    {
        _collectionRepository = collectionRepository;
        _unitOfWork = unitOfWork;
        _scopeFactory = scopeFactory;
        _refundRequestRepository = refundRequestRepository;
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
            // Phase 6A.136D: Swallow collection errors to prevent Stripe retry storms (HTTP 500 → retries).
            // Collection stays Pending; expiry cleanup handles it.
            // CRITICAL level for monitoring/alerting on persistent issues.
            _logger.LogCritical(ex,
                "[Collection] [Webhook-Collection-CRITICAL] Error handling collection checkout (swallowed to prevent retry storm) - " +
                "CorrelationId: {CorrelationId}, SessionId: {SessionId}, Type: {ExceptionType}, Message: {Message}. " +
                "ACTION REQUIRED: Collection remains in Pending state, verify expiry cleanup will handle it.",
                correlationId, sessionId, ex.GetType().FullName, ex.Message);
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
                "[Phase 6A.136E] [Webhook-Collection-Refund-1] Processing collection refund - CorrelationId: {CorrelationId}, PaymentIntentId: {PaymentIntentId}, RefundId: {RefundId}",
                correlationId, paymentIntentId, refundId);

            var collection = await _collectionRepository.FindFirstAsync(
                c => c.StripePaymentIntentId == paymentIntentId, ct);

            if (collection == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.136E] [Webhook-Collection-Refund-WARN] Collection not found for PaymentIntentId - CorrelationId: {CorrelationId}, PaymentIntentId: {PaymentIntentId}",
                    correlationId, paymentIntentId);
                return;
            }

            var refundResult = collection.MarkAsRefunded();
            if (refundResult.IsFailure)
            {
                _logger.LogWarning(
                    "[Phase 6A.136E] [Webhook-Collection-Refund-WARN] MarkAsRefunded failed - CorrelationId: {CorrelationId}, CollectionId: {CollectionId}, Error: {Error}",
                    correlationId, collection.Id, refundResult.Error);
                return;
            }

            _collectionRepository.Update(collection);
            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "[Phase 6A.136E] [Webhook-Collection-Refund-SUCCESS] Collection marked as refunded - CorrelationId: {CorrelationId}, CollectionId: {CollectionId}, RefundId: {RefundId}",
                correlationId, collection.Id, refundId);

            // Phase 6A.148.W4.D12 (G4): Suppress the legacy per-Collection email when the
            // refund came through the approval workflow — the consolidated D8 decision email
            // already covered the attendee. Fail-OPEN: lookup throws → default to sending the
            // legacy email rather than silencing a legitimate notification.
            var isWorkflowOwnedRefund = false;
            try
            {
                var workflowReferenceId = await _refundRequestRepository
                    .GetWorkflowLineReferenceIdAsync(
                        LankaConnect.Products.LankaEvents.Domain.Enums.RefundLineItemType.Collection, refundId, ct);
                isWorkflowOwnedRefund = workflowReferenceId == collection.Id;
            }
            catch (Exception lookupEx)
            {
                _logger.LogWarning(lookupEx,
                    "[Phase 6A.148.W4.D12] Workflow-owned lookup threw — defaulting to send standalone collection email. CorrelationId: {CorrelationId}, CollectionId: {CollectionId}, RefundId: {RefundId}",
                    correlationId, collection.Id, refundId);
            }

            if (isWorkflowOwnedRefund)
            {
                _logger.LogInformation(
                    "[Phase 6A.148.W4.D12] Collection refund standalone email SUPPRESSED — workflow-owned. CorrelationId: {CorrelationId}, CollectionId: {CollectionId}, RefundId: {RefundId}",
                    correlationId, collection.Id, refundId);

                // Phase 6A.148.W5.6.B.OBS3 — durable audit row.
                try
                {
                    using var auditScope = _scopeFactory.CreateScope();
                    var auditService = auditScope.ServiceProvider
                        .GetRequiredService<Email.Services.IRefundDispatchAuditService>();
                    await auditService.WriteSuppressionAsync(
                        templateName: Shared.Email.Contracts.EmailTemplateContract.TemplateNames.CollectionRefund,
                        recipientEmail: collection.ContributorEmail,
                        recipientName: collection.ContributorName,
                        suppressionReason: "D12: workflow-owned refund — covered by consolidated decision email",
                        correlationId: correlationId,
                        refundRequestId: null,
                        entityType: "Collection",
                        entityId: collection.Id,
                        cancellationToken: ct);
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx,
                        "[Phase 6A.148.W5.6.B.OBS3] Failed to write collection suppression audit row — continuing. CorrelationId: {CorrelationId}",
                        correlationId);
                }

                return;
            }

            // Phase 6A.137B2: Fire-and-forget refund notification email
            var capturedContributorName = collection.ContributorName;
            var capturedContributorEmail = collection.ContributorEmail;
            var capturedEventId = collection.EventId;
            var capturedCollectionId = collection.Id;
            var capturedAmount = collection.Amount.Amount;
            var capturedCurrency = collection.Amount.Currency.ToString();
            var capturedRefundedAt = collection.RefundedAt ?? DateTime.UtcNow;
            var capturedPaymentIntentId = collection.StripePaymentIntentId ?? paymentIntentId;
            var capturedScopeFactory = _scopeFactory;

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = capturedScopeFactory.CreateScope();
                    var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                    var eventTitle = $"Event {capturedEventId:N}";
                    try
                    {
                        var @event = await eventRepository.GetByIdAsync(capturedEventId, trackChanges: false, CancellationToken.None);
                        if (@event != null)
                            eventTitle = @event.Title.Value;
                    }
                    catch (Exception titleEx)
                    {
                        _logger.LogWarning(titleEx,
                            "[Phase 6A.137B2] Collection refund email: Failed to load event title for EventId={EventId}",
                            capturedEventId);
                    }

                    var baseUrl = configuration["Application:FrontendBaseUrl"]
                        ?? configuration["FrontendBaseUrl"]
                        ?? "https://lankaconnect.com";
                    var eventDetailsUrl = $"{baseUrl}/events/{capturedEventId}";

                    var emailService = scope.ServiceProvider.GetRequiredService<ITypedEmailService>();
                    var emailParams = CollectionRefundEmailParams.Create(
                        contributorName: capturedContributorName,
                        contributorEmail: capturedContributorEmail,
                        eventTitle: eventTitle,
                        contributionAmount: capturedAmount,
                        currency: capturedCurrency,
                        refundedAt: capturedRefundedAt,
                        paymentIntentId: capturedPaymentIntentId,
                        eventDetailsUrl: eventDetailsUrl);

                    var result = await emailService.SendEmailAsync(emailParams, CancellationToken.None);

                    if (result.Success)
                    {
                        _logger.LogInformation(
                            "[Phase 6A.137B2] Collection refund EMAIL SENT: Email={Email}, CollectionId={CollectionId}, EventTitle={EventTitle}",
                            capturedContributorEmail, capturedCollectionId, eventTitle);
                    }
                    else
                    {
                        _logger.LogError(
                            "[Phase 6A.137B2] Collection refund EMAIL FAILED: Email={Email}, CollectionId={CollectionId}, Errors={Errors}",
                            capturedContributorEmail, capturedCollectionId, string.Join(", ", result.Errors));
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx,
                        "[Phase 6A.137B2] Collection refund EMAIL EXCEPTION: Email={Email}, CollectionId={CollectionId}",
                        capturedContributorEmail, capturedCollectionId);
                }
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "[Phase 6A.136E] [Webhook-Collection-Refund-CRITICAL] Error handling collection refund (swallowed) - CorrelationId: {CorrelationId}, PaymentIntentId: {PaymentIntentId}",
                correlationId, paymentIntentId);
        }
    }
}
