using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Services;

/// <summary>
/// Phase 6A.92: Shared service for processing registration refunds.
/// Uses webhook-based approach: initiates Stripe refund and transitions to RefundRequested.
/// The charge.refunded webhook completes the transition to Refunded.
///
/// Consolidates refund logic used by:
/// - CancelRsvpCommandHandler (user-initiated cancellation)
/// - EventCancellationEmailJob (event cancellation by organizer)
/// </summary>
public class RegistrationRefundService : IRegistrationRefundService
{
    private readonly IStripePaymentService _stripePaymentService;
    private readonly ILogger<RegistrationRefundService> _logger;

    public RegistrationRefundService(
        IStripePaymentService stripePaymentService,
        ILogger<RegistrationRefundService> logger)
    {
        _stripePaymentService = stripePaymentService;
        _logger = logger;
    }

    public async Task<Result<RefundResult>> ProcessRefundAsync(
        Registration registration,
        string reason,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // Validate registration is eligible for refund
        if (registration.PaymentStatus != PaymentStatus.Completed)
        {
            return Result<RefundResult>.Failure(
                $"Cannot refund registration with PaymentStatus {registration.PaymentStatus}. Only Completed payments can be refunded.");
        }

        if (string.IsNullOrWhiteSpace(registration.StripePaymentIntentId))
        {
            return Result<RefundResult>.Failure(
                "Cannot process refund: Missing StripePaymentIntentId. Payment information not found.");
        }

        _logger.LogInformation(
            "[RefundService] Processing refund - RegId={RegId}, PaymentIntentId={PaymentIntentId}, Reason={Reason}",
            registration.Id, registration.StripePaymentIntentId, reason);

        // Calculate refund amount in cents
        var amountInCents = registration.TotalPrice != null
            ? (long)(registration.TotalPrice.Amount * 100)
            : (long?)null;

        // Create Stripe refund request
        var refundRequest = new CreateRefundRequest
        {
            PaymentIntentId = registration.StripePaymentIntentId,
            RegistrationId = registration.Id,
            AmountInCents = amountInCents,
            Reason = reason,
            Metadata = metadata
        };

        // Call Stripe API
        var stripeResult = await _stripePaymentService.CreateRefundAsync(refundRequest, cancellationToken);

        if (stripeResult.IsFailure)
        {
            stopwatch.Stop();
            _logger.LogError(
                "[RefundService] Stripe refund failed - RegId={RegId}, Error={Error}, Duration={ElapsedMs}ms",
                registration.Id, stripeResult.Error, stopwatch.ElapsedMilliseconds);
            return Result<RefundResult>.Failure($"Stripe refund failed: {stripeResult.Error}");
        }

        _logger.LogInformation(
            "[RefundService] Stripe refund succeeded - RegId={RegId}, StripeRefundId={RefundId}, Status={Status}",
            registration.Id, stripeResult.Value.RefundId, stripeResult.Value.Status);

        // Transition registration state to RefundRequested
        // The charge.refunded webhook will complete the transition to Refunded
        var requestResult = registration.RequestRefund();
        if (requestResult.IsFailure)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "[RefundService] RequestRefund state transition failed - RegId={RegId}, Error={Error}, Duration={ElapsedMs}ms",
                registration.Id, requestResult.Error, stopwatch.ElapsedMilliseconds);
            // Note: Stripe refund was already processed. Log warning but continue.
            // The registration state is inconsistent but refund was successful.
            // The webhook will attempt to complete the refund when it arrives.
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "[RefundService] Refund request completed - RegId={RegId}, StripeRefundId={RefundId}, Amount=${Amount}, Duration={ElapsedMs}ms. Webhook will complete the refund.",
            registration.Id, stripeResult.Value.RefundId,
            registration.TotalPrice?.Amount ?? 0, stopwatch.ElapsedMilliseconds);

        return Result<RefundResult>.Success(new RefundResult(
            stripeResult.Value.RefundId,
            registration.TotalPrice?.Amount ?? 0));
    }
}
