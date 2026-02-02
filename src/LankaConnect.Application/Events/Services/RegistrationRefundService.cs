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

        // Phase 6A.94: Enhanced logging for refund observability
        _logger.LogInformation(
            "[RefundService] START ProcessRefundAsync - RegId={RegId}, CurrentStatus={Status}, " +
            "PaymentStatus={PaymentStatus}, PaymentIntentId={PaymentIntentId}, Amount=${Amount}, Reason={Reason}",
            registration.Id, registration.Status, registration.PaymentStatus,
            registration.StripePaymentIntentId ?? "NULL", registration.TotalPrice?.Amount ?? 0, reason);

        try
        {
            // Validate registration is eligible for refund
            if (registration.PaymentStatus != PaymentStatus.Completed)
            {
                var errorMsg = $"Cannot refund registration with PaymentStatus {registration.PaymentStatus}. Only Completed payments can be refunded.";
                _logger.LogWarning(
                    "[RefundService] VALIDATION FAILED - RegId={RegId}, Error={Error}",
                    registration.Id, errorMsg);
                return Result<RefundResult>.Failure(errorMsg);
            }

            if (string.IsNullOrWhiteSpace(registration.StripePaymentIntentId))
            {
                var errorMsg = "Cannot process refund: Missing StripePaymentIntentId. Payment information not found.";
                _logger.LogWarning(
                    "[RefundService] VALIDATION FAILED - RegId={RegId}, Error={Error}",
                    registration.Id, errorMsg);
                return Result<RefundResult>.Failure(errorMsg);
            }

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

            // Phase 6A.94: Log BEFORE Stripe API call with full request details
            _logger.LogInformation(
                "[RefundService] CALLING Stripe CreateRefundAsync - RegId={RegId}, PaymentIntentId={PaymentIntentId}, " +
                "AmountCents={AmountCents}, Reason={Reason}, MetadataKeys={MetadataKeys}",
                registration.Id, refundRequest.PaymentIntentId, amountInCents,
                reason, string.Join(",", metadata?.Keys ?? Enumerable.Empty<string>()));

            // Call Stripe API
            var stripeResult = await _stripePaymentService.CreateRefundAsync(refundRequest, cancellationToken);

            if (stripeResult.IsFailure)
            {
                stopwatch.Stop();
                _logger.LogError(
                    "[RefundService] STRIPE API FAILED - RegId={RegId}, PaymentIntentId={PaymentIntentId}, " +
                    "Error={Error}, Duration={ElapsedMs}ms. Check Stripe dashboard for details.",
                    registration.Id, registration.StripePaymentIntentId,
                    stripeResult.Error, stopwatch.ElapsedMilliseconds);
                return Result<RefundResult>.Failure($"Stripe refund failed: {stripeResult.Error}");
            }

            _logger.LogInformation(
                "[RefundService] STRIPE API SUCCESS - RegId={RegId}, StripeRefundId={RefundId}, " +
                "StripeStatus={Status}, Duration={ElapsedMs}ms",
                registration.Id, stripeResult.Value.RefundId, stripeResult.Value.Status, stopwatch.ElapsedMilliseconds);

            // Transition registration state to RefundRequested
            // The charge.refunded webhook will complete the transition to Refunded
            var statusBefore = registration.Status;
            var requestResult = registration.RequestRefund();

            if (requestResult.IsFailure)
            {
                _logger.LogWarning(
                    "[RefundService] RequestRefund STATE TRANSITION FAILED - RegId={RegId}, " +
                    "StatusBefore={StatusBefore}, StatusAfter={StatusAfter}, Error={Error}. " +
                    "Stripe refund was processed but registration state is inconsistent.",
                    registration.Id, statusBefore, registration.Status, requestResult.Error);
                // Note: Stripe refund was already processed. Log warning but continue.
                // The webhook will attempt to complete the refund when it arrives.
            }
            else
            {
                _logger.LogInformation(
                    "[RefundService] RequestRefund STATE TRANSITION SUCCESS - RegId={RegId}, " +
                    "StatusBefore={StatusBefore}, StatusAfter={StatusAfter}",
                    registration.Id, statusBefore, registration.Status);
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "[RefundService] END ProcessRefundAsync SUCCESS - RegId={RegId}, StripeRefundId={RefundId}, " +
                "Amount=${Amount}, FinalStatus={Status}, TotalDuration={ElapsedMs}ms. Webhook will complete the refund.",
                registration.Id, stripeResult.Value.RefundId,
                registration.TotalPrice?.Amount ?? 0, registration.Status, stopwatch.ElapsedMilliseconds);

            return Result<RefundResult>.Success(new RefundResult(
                stripeResult.Value.RefundId,
                registration.TotalPrice?.Amount ?? 0));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "[RefundService] EXCEPTION in ProcessRefundAsync - RegId={RegId}, PaymentIntentId={PaymentIntentId}, " +
                "Duration={ElapsedMs}ms, ExceptionType={ExceptionType}",
                registration.Id, registration.StripePaymentIntentId ?? "NULL",
                stopwatch.ElapsedMilliseconds, ex.GetType().Name);
            throw; // Re-throw to let caller handle
        }
    }
}
