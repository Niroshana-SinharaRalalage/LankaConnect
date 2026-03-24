using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Payments.Services;

/// <summary>
/// Phase 4: Handles Stripe webhook events for money sponsor payments.
/// Follows DonationWebhookHandler pattern.
/// Errors are swallowed to prevent HTTP 500 to Stripe (sponsor stays Pending; expiry cleanup handles it).
/// </summary>
public class SponsorWebhookHandler : ISponsorWebhookHandler
{
    private readonly ISponsorRepository _sponsorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SponsorWebhookHandler> _logger;

    public SponsorWebhookHandler(
        ISponsorRepository sponsorRepository,
        IUnitOfWork unitOfWork,
        ILogger<SponsorWebhookHandler> logger)
    {
        _sponsorRepository = sponsorRepository;
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
                "[Sponsor] [Webhook-Sponsor-1] Processing sponsor payment - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                correlationId, sessionId);

            // Load sponsor by checkout session
            var sponsor = await _sponsorRepository.GetByCheckoutSessionIdAsync(sessionId, ct);
            if (sponsor == null)
            {
                _logger.LogError(
                    "[Sponsor] [Webhook-Sponsor-ERROR] Sponsor not found by session - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, sessionId);
                return;
            }

            _logger.LogInformation(
                "[Sponsor] [Webhook-Sponsor-2] Sponsor loaded - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, Status: {Status}, Type: {Type}",
                correlationId, sponsor.Id, sponsor.Status, sponsor.Type);

            // Verify the sponsor is still pending and is a money sponsor (idempotency + type check)
            if (sponsor.Status != SponsorStatus.Pending)
            {
                _logger.LogWarning(
                    "[Sponsor] [Webhook-Sponsor-WARN] Sponsor not in Pending status (idempotent skip) - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, CurrentStatus: {Status}",
                    correlationId, sponsor.Id, sponsor.Status);
                return;
            }

            if (sponsor.Type != SponsorType.Money)
            {
                _logger.LogWarning(
                    "[Sponsor] [Webhook-Sponsor-WARN] Sponsor is not a money sponsor - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, Type: {Type}",
                    correlationId, sponsor.Id, sponsor.Type);
                return;
            }

            // Complete payment on the sponsor entity
            var completeResult = sponsor.CompletePayment(paymentIntentId);

            if (completeResult.IsFailure)
            {
                _logger.LogError(
                    "[Sponsor] [Webhook-Sponsor-ERROR] CompletePayment failed - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, Error: {Error}",
                    correlationId, sponsor.Id, completeResult.Error);
                return;
            }

            _logger.LogInformation(
                "[Sponsor] [Webhook-Sponsor-3] Payment completed on sponsor - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, PaymentIntentId: {PaymentIntentId}",
                correlationId, sponsor.Id, paymentIntentId);

            // Save changes and dispatch domain events
            _sponsorRepository.Update(sponsor);
            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "[Sponsor] [Webhook-Sponsor-SUCCESS] Sponsor payment completed successfully - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, PaymentIntentId: {PaymentIntentId}",
                correlationId, sponsor.Id, paymentIntentId);
        }
        catch (Exception ex)
        {
            // Swallow sponsor errors: failures should NOT return HTTP 500
            // to Stripe (causes retry storms). Sponsor stays Pending; expiry cleanup handles it.
            _logger.LogError(ex,
                "[Sponsor] [Webhook-Sponsor-ERROR] Error handling sponsor checkout (swallowed) - CorrelationId: {CorrelationId}, Type: {ExceptionType}, Message: {Message}",
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
                "[Sponsor] [Webhook-Expired-Sponsor-1] Processing sponsor expiry - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                correlationId, sessionId);

            var sponsor = await _sponsorRepository.GetByCheckoutSessionIdAsync(sessionId, ct);
            if (sponsor == null)
            {
                _logger.LogWarning(
                    "[Sponsor] [Webhook-Expired-Sponsor-WARN] Sponsor not found by session - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, sessionId);
                return;
            }

            if (sponsor.Status != SponsorStatus.Pending)
            {
                _logger.LogWarning(
                    "[Sponsor] [Webhook-Expired-Sponsor-WARN] Sponsor not in Pending status - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, Status: {Status}",
                    correlationId, sponsor.Id, sponsor.Status);
                return;
            }

            if (sponsor.Type != SponsorType.Money)
            {
                _logger.LogWarning(
                    "[Sponsor] [Webhook-Expired-Sponsor-WARN] Sponsor is not a money sponsor - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, Type: {Type}",
                    correlationId, sponsor.Id, sponsor.Type);
                return;
            }

            sponsor.MarkAsAbandoned();
            _sponsorRepository.Update(sponsor);
            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "[Sponsor] [Webhook-Expired-Sponsor-SUCCESS] Sponsor abandoned - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}",
                correlationId, sponsor.Id);
        }
        catch (Exception ex)
        {
            // Swallow: sponsor expiry failure should NOT return HTTP 500 to Stripe.
            // Sponsor stays Pending and can be cleaned up by background job.
            _logger.LogError(ex,
                "[Sponsor] [Webhook-Expired-Sponsor-ERROR] Error handling sponsor expiry (swallowed) - CorrelationId: {CorrelationId}, Type: {ExceptionType}, Message: {Message}",
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
                "[Phase 6A.136E] [Webhook-Sponsor-Refund-1] Processing sponsor refund - CorrelationId: {CorrelationId}, PaymentIntentId: {PaymentIntentId}, RefundId: {RefundId}",
                correlationId, paymentIntentId, refundId);

            var sponsor = await _sponsorRepository.FindFirstAsync(
                s => s.StripePaymentIntentId == paymentIntentId, ct);

            if (sponsor == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.136E] [Webhook-Sponsor-Refund-WARN] Sponsor not found for PaymentIntentId - CorrelationId: {CorrelationId}, PaymentIntentId: {PaymentIntentId}",
                    correlationId, paymentIntentId);
                return;
            }

            var refundResult = sponsor.MarkAsRefunded();
            if (refundResult.IsFailure)
            {
                _logger.LogWarning(
                    "[Phase 6A.136E] [Webhook-Sponsor-Refund-WARN] MarkAsRefunded failed - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, Error: {Error}",
                    correlationId, sponsor.Id, refundResult.Error);
                return;
            }

            _sponsorRepository.Update(sponsor);
            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "[Phase 6A.136E] [Webhook-Sponsor-Refund-SUCCESS] Sponsor marked as refunded - CorrelationId: {CorrelationId}, SponsorId: {SponsorId}, RefundId: {RefundId}",
                correlationId, sponsor.Id, refundId);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "[Phase 6A.136E] [Webhook-Sponsor-Refund-CRITICAL] Error handling sponsor refund (swallowed) - CorrelationId: {CorrelationId}, PaymentIntentId: {PaymentIntentId}",
                correlationId, paymentIntentId);
        }
    }
}
