using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Payments.Services;

/// <summary>
/// Phase 0: Handles Stripe webhook events for standalone donation payments.
/// Extracted from PaymentsController for separation of concerns.
/// Errors are swallowed to prevent HTTP 500 to Stripe (donation stays Pending; expiry cleanup handles it).
/// </summary>
public class DonationWebhookHandler : IDonationWebhookHandler
{
    private readonly IDonationRepository _donationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DonationWebhookHandler> _logger;

    public DonationWebhookHandler(
        IDonationRepository donationRepository,
        IUnitOfWork unitOfWork,
        ILogger<DonationWebhookHandler> logger)
    {
        _donationRepository = donationRepository;
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
                "[Donation] [Webhook-Donation-1] Processing standalone donation payment - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                correlationId, sessionId);

            // Extract donation_id metadata
            if (!metadata.TryGetValue("donation_id", out var donationIdStr) ||
                !Guid.TryParse(donationIdStr, out var donationId))
            {
                _logger.LogWarning(
                    "[Donation] [Webhook-Donation-ERROR] Missing donation_id - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, sessionId);
                return;
            }

            _logger.LogInformation(
                "[Donation] [Webhook-Donation-2] Metadata extracted - CorrelationId: {CorrelationId}, DonationId: {DonationId}",
                correlationId, donationId);

            // Load the donation (with tracking)
            var donation = await _donationRepository.GetByDonationIdAsync(donationId);
            if (donation == null)
            {
                _logger.LogError(
                    "[Donation] [Webhook-Donation-ERROR] Donation not found - CorrelationId: {CorrelationId}, DonationId: {DonationId}",
                    correlationId, donationId);
                return;
            }

            _logger.LogInformation(
                "[Donation] [Webhook-Donation-3] Donation loaded - CorrelationId: {CorrelationId}, DonationId: {DonationId}, Status: {Status}, Amount: {Amount}",
                correlationId, donationId, donation.Status, donation.Amount.Amount);

            // Verify the donation is still pending (idempotency check)
            if (donation.Status != Domain.Events.Enums.DonationStatus.Pending)
            {
                _logger.LogWarning(
                    "[Donation] [Webhook-Donation-WARN] Donation not in Pending status (idempotent skip) - CorrelationId: {CorrelationId}, DonationId: {DonationId}, CurrentStatus: {Status}",
                    correlationId, donationId, donation.Status);
                return;
            }

            // Complete payment on the donation entity
            var completeResult = donation.CompletePayment(paymentIntentId);

            if (completeResult.IsFailure)
            {
                _logger.LogError(
                    "[Donation] [Webhook-Donation-ERROR] CompletePayment failed - CorrelationId: {CorrelationId}, DonationId: {DonationId}, Error: {Error}",
                    correlationId, donationId, completeResult.Error);
                return;
            }

            _logger.LogInformation(
                "[Donation] [Webhook-Donation-4] Payment completed on donation - CorrelationId: {CorrelationId}, DonationId: {DonationId}, PaymentIntentId: {PaymentIntentId}",
                correlationId, donationId, paymentIntentId);

            // Save changes and dispatch DonationCompletedEvent
            _donationRepository.Update(donation);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation(
                "[Donation] [Webhook-Donation-SUCCESS] Standalone donation completed successfully - CorrelationId: {CorrelationId}, DonationId: {DonationId}, PaymentIntentId: {PaymentIntentId}, Amount: {Amount}",
                correlationId, donationId, paymentIntentId, donation.Amount.Amount);
        }
        catch (Exception ex)
        {
            // Phase 6A.136D: Swallow donation errors to prevent Stripe retry storms (HTTP 500 → retries).
            // Donation stays Pending; expiry cleanup handles it.
            // CRITICAL level for DB failures so monitoring/alerting catches persistent issues.
            _logger.LogCritical(ex,
                "[Donation] [Webhook-Donation-CRITICAL] Error handling donation checkout (swallowed to prevent retry storm) - " +
                "CorrelationId: {CorrelationId}, DonationId: {DonationId}, Type: {ExceptionType}, Message: {Message}. " +
                "ACTION REQUIRED: Donation remains in Pending state, verify expiry cleanup will handle it.",
                correlationId, metadata.GetValueOrDefault("donation_id", "unknown"), ex.GetType().FullName, ex.Message);
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
            if (!metadata.TryGetValue("donation_id", out var donationIdStr) ||
                !Guid.TryParse(donationIdStr, out var donationId))
            {
                _logger.LogWarning(
                    "[Donation] [Webhook-Expired-Standalone-WARN] Missing donation_id in metadata - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, sessionId);
                return;
            }

            _logger.LogInformation(
                "[Donation] [Webhook-Expired-Standalone-1] Processing standalone donation expiry - CorrelationId: {CorrelationId}, DonationId: {DonationId}",
                correlationId, donationId);

            var donation = await _donationRepository.GetByDonationIdAsync(donationId);
            if (donation == null)
            {
                _logger.LogWarning(
                    "[Donation] [Webhook-Expired-Standalone-WARN] Donation not found - CorrelationId: {CorrelationId}, DonationId: {DonationId}",
                    correlationId, donationId);
                return;
            }

            if (donation.Status != Domain.Events.Enums.DonationStatus.Pending)
            {
                _logger.LogWarning(
                    "[Donation] [Webhook-Expired-Standalone-WARN] Donation not in Pending status - CorrelationId: {CorrelationId}, DonationId: {DonationId}, Status: {Status}",
                    correlationId, donationId, donation.Status);
                return;
            }

            donation.MarkAsAbandoned();
            _donationRepository.Update(donation);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation(
                "[Donation] [Webhook-Expired-Standalone-SUCCESS] Standalone donation abandoned - CorrelationId: {CorrelationId}, DonationId: {DonationId}",
                correlationId, donationId);
        }
        catch (Exception ex)
        {
            // Swallow: donation expiry failure should NOT return HTTP 500 to Stripe.
            // Donation stays Pending and can be cleaned up by background job.
            _logger.LogError(ex,
                "[Donation] [Webhook-Expired-Standalone-ERROR] Error handling donation expiry (swallowed) - CorrelationId: {CorrelationId}, Type: {ExceptionType}, Message: {Message}",
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
                "[Phase 6A.136E] [Webhook-Donation-Refund-1] Processing donation refund - CorrelationId: {CorrelationId}, PaymentIntentId: {PaymentIntentId}, RefundId: {RefundId}",
                correlationId, paymentIntentId, refundId);

            var donation = await _donationRepository.FindFirstAsync(
                d => d.StripePaymentIntentId == paymentIntentId, ct);

            if (donation == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.136E] [Webhook-Donation-Refund-WARN] Donation not found for PaymentIntentId - CorrelationId: {CorrelationId}, PaymentIntentId: {PaymentIntentId}",
                    correlationId, paymentIntentId);
                return;
            }

            var refundResult = donation.MarkAsRefunded();
            if (refundResult.IsFailure)
            {
                _logger.LogWarning(
                    "[Phase 6A.136E] [Webhook-Donation-Refund-WARN] MarkAsRefunded failed - CorrelationId: {CorrelationId}, DonationId: {DonationId}, Error: {Error}",
                    correlationId, donation.Id, refundResult.Error);
                return;
            }

            _donationRepository.Update(donation);
            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "[Phase 6A.136E] [Webhook-Donation-Refund-SUCCESS] Donation marked as refunded - CorrelationId: {CorrelationId}, DonationId: {DonationId}, RefundId: {RefundId}",
                correlationId, donation.Id, refundId);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "[Phase 6A.136E] [Webhook-Donation-Refund-CRITICAL] Error handling donation refund (swallowed) - CorrelationId: {CorrelationId}, PaymentIntentId: {PaymentIntentId}",
                correlationId, paymentIntentId);
        }
    }
}
