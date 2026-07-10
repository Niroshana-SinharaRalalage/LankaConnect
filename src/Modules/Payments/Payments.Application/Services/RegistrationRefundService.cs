using LankaConnect.Products.LankaEvents.Contracts.LegacyPromotions; // Wave 6.5.g Day 5 // W4.4.c.4: interfaces stay in legacy (4 cross-module consumers; circular ref otherwise)
using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Modules.Payments.Application.Services;

/// <summary>
/// Phase 6A.92: Shared service for processing registration refunds.
/// Uses webhook-based approach: initiates Stripe refund and transitions to RefundRequested.
/// The charge.refunded webhook completes the transition to Refunded.
///
/// Phase 6A.X FIX: Now handles multiple payments per registration (Add-Only Attendees feature).
/// When attendees are added, there are multiple Stripe charges that all need to be refunded.
///
/// Consolidates refund logic used by:
/// - CancelRsvpCommandHandler (user-initiated cancellation)
/// - EventCancellationEmailJob (event cancellation by organizer)
/// </summary>
public class RegistrationRefundService : IRegistrationRefundService
{
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IRegistrationPaymentRepository _paymentRepository;
    private readonly ILogger<RegistrationRefundService> _logger;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public RegistrationRefundService(
        IStripePaymentService stripePaymentService,
        IRegistrationPaymentRepository paymentRepository,
        ILogger<RegistrationRefundService> logger,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _stripePaymentService = stripePaymentService;
        _paymentRepository = paymentRepository;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<Result<RefundResult>> ProcessRefundAsync(
        Registration registration,
        string reason,
        Dictionary<string, string> metadata,
        decimal additionalRefundAmount = 0m,
        bool isPreApproved = false,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // Phase 6A.148 ring-2 interlock (architect F5 defense-in-depth): if the approval
        // workflow flag is ON and the caller did NOT route through approval first, refuse
        // the refund. This guarantees the GATE holds even against future code that calls
        // this service directly.
        var approvalFlagOn = _configuration.GetValue<bool>("Refund:ApprovalWorkflow:Enabled");
        if (approvalFlagOn && !isPreApproved)
        {
            _logger.LogWarning(
                "[6A.148 INTERLOCK] BLOCKED ProcessRefundAsync called without isPreApproved while approval workflow is ON. " +
                "RegId={RegId} Caller must route via RefundExecutionService (organizer Approve) or pre-approve via ApprovedAuto.",
                registration.Id);
            return Result<RefundResult>.Failure(
                "Refund-approval workflow is enabled. Direct refunds are blocked — go through " +
                "the approval workflow (POST /api/events/{id}/refund-requests).");
        }

        // Phase 6A.94: Enhanced logging for refund observability
        _logger.LogInformation(
            "[RefundService] START ProcessRefundAsync - RegId={RegId}, CurrentStatus={Status}, " +
            "PaymentStatus={PaymentStatus}, LegacyPaymentIntentId={PaymentIntentId}, Amount=${Amount}, " +
            "Reason={Reason}, IsPreApproved={IsPreApproved}",
            registration.Id, registration.Status, registration.PaymentStatus,
            registration.StripePaymentIntentId ?? "NULL", registration.TotalPrice?.Amount ?? 0,
            reason, isPreApproved);

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

            // Phase 6A.X FIX: Get ALL payments for this registration from RegistrationPayments table
            // This handles the Add-Only Attendees feature where multiple payments exist
            var allPayments = await _paymentRepository.GetByRegistrationIdAsync(registration.Id, cancellationToken);

            _logger.LogInformation(
                "[RefundService] Retrieved payments from RegistrationPayments table - RegId={RegId}, PaymentCount={Count}",
                registration.Id, allPayments.Count);

            // Filter to only completed payments that haven't been refunded yet
            var completedPayments = allPayments
                .Where(p => p.Status == PaymentStatus.Completed)
                .ToList();

            _logger.LogInformation(
                "[RefundService] Completed payments to refund - RegId={RegId}, CompletedCount={Count}",
                registration.Id, completedPayments.Count);

            // Phase 6A.X: Handle case where we have multiple payments vs legacy single payment
            if (completedPayments.Count > 0)
            {
                // NEW PATH: Refund each payment individually
                return await ProcessMultiplePaymentRefundsAsync(
                    registration, completedPayments, reason, metadata, additionalRefundAmount, stopwatch, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(registration.StripePaymentIntentId))
            {
                // LEGACY PATH: Fall back to single payment using Registration.StripePaymentIntentId
                // This handles older registrations created before RegistrationPayments table
                _logger.LogInformation(
                    "[RefundService] No RegistrationPayments found, using legacy StripePaymentIntentId - RegId={RegId}",
                    registration.Id);
                return await ProcessLegacySinglePaymentRefundAsync(
                    registration, reason, metadata, additionalRefundAmount, stopwatch, cancellationToken);
            }
            else
            {
                var errorMsg = "Cannot process refund: No payment records found and no StripePaymentIntentId on registration.";
                _logger.LogWarning(
                    "[RefundService] VALIDATION FAILED - RegId={RegId}, Error={Error}",
                    registration.Id, errorMsg);
                return Result<RefundResult>.Failure(errorMsg);
            }
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

    /// <summary>
    /// Phase 6A.X: Process refunds for multiple payments (Add-Only Attendees feature).
    /// Each payment is refunded individually with its own PaymentIntentId and Amount.
    /// </summary>
    private async Task<Result<RefundResult>> ProcessMultiplePaymentRefundsAsync(
        Registration registration,
        List<RegistrationPayment> completedPayments,
        string reason,
        Dictionary<string, string> metadata,
        decimal additionalRefundAmount,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        var totalRefunded = 0m;
        var refundIds = new List<string>();
        var failedPayments = new List<(RegistrationPayment Payment, string Error)>();

        _logger.LogInformation(
            "[RefundService] Processing {Count} individual payment refunds - RegId={RegId}",
            completedPayments.Count, registration.Id);

        foreach (var payment in completedPayments)
        {
            try
            {
                var amountInCents = (long)(payment.Amount.Amount * 100);

                // Add payment-specific metadata including registration_id for webhook lookup.
                // Phase 6A.137F-Fix5f: registration_id was missing, causing charge.refunded webhook
                // to fail silently (couldn't find registration → stuck in RefundRequested forever).
                var paymentMetadata = new Dictionary<string, string>(metadata ?? new Dictionary<string, string>())
                {
                    ["registration_id"] = registration.Id.ToString(),
                    ["payment_id"] = payment.Id.ToString(),
                    ["payment_type"] = payment.Type.ToString()
                };

                var refundRequest = new CreateRefundRequest
                {
                    PaymentIntentId = payment.StripePaymentIntentId,
                    RegistrationId = registration.Id,
                    AmountInCents = amountInCents,
                    Reason = reason,
                    Metadata = paymentMetadata
                };

                _logger.LogInformation(
                    "[RefundService] Refunding payment {PaymentIndex}/{TotalPayments} - RegId={RegId}, " +
                    "PaymentId={PaymentId}, PaymentIntentId={PaymentIntentId}, Amount=${Amount}, Type={Type}",
                    completedPayments.IndexOf(payment) + 1, completedPayments.Count,
                    registration.Id, payment.Id, payment.StripePaymentIntentId, payment.Amount.Amount, payment.Type);

                var stripeResult = await _stripePaymentService.CreateRefundAsync(refundRequest, cancellationToken);

                if (stripeResult.IsFailure)
                {
                    _logger.LogError(
                        "[RefundService] Payment refund FAILED - RegId={RegId}, PaymentId={PaymentId}, " +
                        "PaymentIntentId={PaymentIntentId}, Error={Error}",
                        registration.Id, payment.Id, payment.StripePaymentIntentId, stripeResult.Error);
                    failedPayments.Add((payment, stripeResult.Error));
                    continue;
                }

                // Mark this payment as refunded
                var markResult = payment.MarkAsRefunded();
                if (markResult.IsFailure)
                {
                    _logger.LogWarning(
                        "[RefundService] Failed to mark payment as refunded - PaymentId={PaymentId}, Error={Error}",
                        payment.Id, markResult.Error);
                }

                totalRefunded += payment.Amount.Amount;
                refundIds.Add(stripeResult.Value.RefundId);

                _logger.LogInformation(
                    "[RefundService] Payment refund SUCCESS - RegId={RegId}, PaymentId={PaymentId}, " +
                    "StripeRefundId={RefundId}, Amount=${Amount}",
                    registration.Id, payment.Id, stripeResult.Value.RefundId, payment.Amount.Amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[RefundService] Exception refunding payment - RegId={RegId}, PaymentId={PaymentId}, " +
                    "PaymentIntentId={PaymentIntentId}",
                    registration.Id, payment.Id, payment.StripePaymentIntentId);
                failedPayments.Add((payment, ex.Message));
            }
        }

        // Check if any payments failed
        if (failedPayments.Count > 0)
        {
            stopwatch.Stop();
            var failedDetails = string.Join("; ", failedPayments.Select(f =>
                $"Payment {f.Payment.StripePaymentIntentId}: {f.Error}"));

            _logger.LogError(
                "[RefundService] Some payments failed to refund - RegId={RegId}, SuccessCount={Success}, " +
                "FailedCount={Failed}, TotalRefunded=${TotalRefunded}, FailedDetails={Details}, Duration={ElapsedMs}ms",
                registration.Id, completedPayments.Count - failedPayments.Count, failedPayments.Count,
                totalRefunded, failedDetails, stopwatch.ElapsedMilliseconds);

            // If ALL payments failed, return failure
            if (failedPayments.Count == completedPayments.Count)
            {
                return Result<RefundResult>.Failure($"All refunds failed: {failedDetails}");
            }

            // If some succeeded, still transition state but log warning
            _logger.LogWarning(
                "[RefundService] Partial refund - some payments refunded successfully - RegId={RegId}",
                registration.Id);
        }

        // Transition registration state to RefundRequested
        // Phase 6A.136C: Pass first StripeRefundId to prevent withdrawal race condition
        var statusBefore = registration.Status;
        var firstRefundId = refundIds.Count > 0 ? refundIds[0] : null;
        var requestResult = registration.RequestRefund(additionalRefundAmount, firstRefundId);

        if (requestResult.IsFailure)
        {
            _logger.LogWarning(
                "[RefundService] RequestRefund STATE TRANSITION FAILED - RegId={RegId}, " +
                "StatusBefore={StatusBefore}, StatusAfter={StatusAfter}, Error={Error}",
                registration.Id, statusBefore, registration.Status, requestResult.Error);
        }
        else
        {
            _logger.LogInformation(
                "[RefundService] RequestRefund STATE TRANSITION SUCCESS - RegId={RegId}, " +
                "StatusBefore={StatusBefore}, StatusAfter={StatusAfter}, AdditionalRefundAmount=${AdditionalRefundAmount}",
                registration.Id, statusBefore, registration.Status, additionalRefundAmount);
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "[RefundService] END ProcessMultiplePaymentRefundsAsync SUCCESS - RegId={RegId}, " +
            "PaymentsRefunded={Count}, TotalRefunded=${TotalRefunded}, RefundIds={RefundIds}, " +
            "FinalStatus={Status}, Duration={ElapsedMs}ms",
            registration.Id, refundIds.Count, totalRefunded, string.Join(",", refundIds),
            registration.Status, stopwatch.ElapsedMilliseconds);

        return Result<RefundResult>.Success(new RefundResult(
            string.Join(",", refundIds),
            totalRefunded));
    }

    /// <summary>
    /// Legacy refund processing for registrations without RegistrationPayments records.
    /// Uses the single StripePaymentIntentId stored on the Registration entity.
    /// </summary>
    private async Task<Result<RefundResult>> ProcessLegacySinglePaymentRefundAsync(
        Registration registration,
        string reason,
        Dictionary<string, string> metadata,
        decimal additionalRefundAmount,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        // Calculate refund amount in cents
        var amountInCents = registration.TotalPrice != null
            ? (long)(registration.TotalPrice.Amount * 100)
            : (long?)null;

        var refundRequest = new CreateRefundRequest
        {
            PaymentIntentId = registration.StripePaymentIntentId!,
            RegistrationId = registration.Id,
            AmountInCents = amountInCents,
            Reason = reason,
            Metadata = metadata
        };

        _logger.LogInformation(
            "[RefundService] LEGACY: Refunding single payment - RegId={RegId}, PaymentIntentId={PaymentIntentId}, " +
            "AmountCents={AmountCents}",
            registration.Id, refundRequest.PaymentIntentId, amountInCents);

        var stripeResult = await _stripePaymentService.CreateRefundAsync(refundRequest, cancellationToken);

        if (stripeResult.IsFailure)
        {
            stopwatch.Stop();
            _logger.LogError(
                "[RefundService] LEGACY: Stripe refund FAILED - RegId={RegId}, PaymentIntentId={PaymentIntentId}, " +
                "Error={Error}, Duration={ElapsedMs}ms",
                registration.Id, registration.StripePaymentIntentId, stripeResult.Error, stopwatch.ElapsedMilliseconds);
            return Result<RefundResult>.Failure($"Stripe refund failed: {stripeResult.Error}");
        }

        _logger.LogInformation(
            "[RefundService] LEGACY: Stripe refund SUCCESS - RegId={RegId}, StripeRefundId={RefundId}",
            registration.Id, stripeResult.Value.RefundId);

        // Transition registration state
        // Phase 6A.136C: Pass StripeRefundId to prevent withdrawal race condition
        var statusBefore = registration.Status;
        var requestResult = registration.RequestRefund(additionalRefundAmount, stripeResult.Value.RefundId);

        if (requestResult.IsFailure)
        {
            _logger.LogWarning(
                "[RefundService] LEGACY: RequestRefund STATE TRANSITION FAILED - RegId={RegId}, Error={Error}",
                registration.Id, requestResult.Error);
        }
        else
        {
            _logger.LogInformation(
                "[RefundService] LEGACY: RequestRefund STATE TRANSITION SUCCESS - RegId={RegId}, AdditionalRefundAmount=${AdditionalRefundAmount}",
                registration.Id, additionalRefundAmount);
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "[RefundService] LEGACY: END ProcessRefundAsync SUCCESS - RegId={RegId}, StripeRefundId={RefundId}, " +
            "Amount=${Amount}, Duration={ElapsedMs}ms",
            registration.Id, stripeResult.Value.RefundId, registration.TotalPrice?.Amount ?? 0, stopwatch.ElapsedMilliseconds);

        return Result<RefundResult>.Success(new RefundResult(
            stripeResult.Value.RefundId,
            registration.TotalPrice?.Amount ?? 0));
    }
}
