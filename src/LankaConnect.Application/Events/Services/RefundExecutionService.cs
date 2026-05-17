using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Services;

/// <summary>
/// Phase 6A.148: Dispatches Stripe refund calls for an approved RefundRequest.
///
/// Invoked AFTER the approve transaction commits (architect F10). Iterates each line
/// item in <see cref="RefundLineItemStatus.Approved"/>, resolves the StripePaymentIntentId
/// from the corresponding underlying entity (RegistrationPayment / AddOnPurchase /
/// Collection / Sponsor), and calls <see cref="IStripePaymentService.CreateRefundAsync"/>
/// with the per-line ApprovedAmount.
///
/// Line-item state transitions: Approved → Processing on Stripe success,
/// Approved → Failed on Stripe failure. The webhook handlers (architect F4) later
/// transition Processing → Refunded. Idempotent at the line-item level — re-dispatch
/// from <c>RefundReconciliationService</c> (architect F11) skips lines not in Approved.
/// </summary>
public class RefundExecutionService : IRefundExecutionService
{
    private readonly IRefundRequestRepository _refundRepo;
    private readonly IRegistrationRepository _registrationRepo;
    private readonly IRegistrationPaymentRepository _paymentRepo;
    private readonly IAddOnPurchaseRepository _addOnRepo;
    private readonly ICollectionRepository _collectionRepo;
    private readonly ISponsorRepository _sponsorRepo;
    private readonly IStripePaymentService _stripe;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RefundExecutionService> _logger;

    public RefundExecutionService(
        IRefundRequestRepository refundRepo,
        IRegistrationRepository registrationRepo,
        IRegistrationPaymentRepository paymentRepo,
        IAddOnPurchaseRepository addOnRepo,
        ICollectionRepository collectionRepo,
        ISponsorRepository sponsorRepo,
        IStripePaymentService stripe,
        IUnitOfWork uow,
        ILogger<RefundExecutionService> logger)
    {
        _refundRepo = refundRepo;
        _registrationRepo = registrationRepo;
        _paymentRepo = paymentRepo;
        _addOnRepo = addOnRepo;
        _collectionRepo = collectionRepo;
        _sponsorRepo = sponsorRepo;
        _stripe = stripe;
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result> DispatchAsync(
        Guid refundRequestId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "RefundExecution.Dispatch"))
        using (LogContext.PushProperty("RefundRequestId", refundRequestId))
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation(
                "[6A.148 EXEC] DispatchAsync START: RrId={RrId}", refundRequestId);

            try
            {
                // Load tracked refund request with line items.
                var req = await _refundRepo.GetByIdAsync(refundRequestId, cancellationToken);
                if (req is null)
                {
                    _logger.LogWarning("[6A.148 EXEC] RrId={RrId} not found", refundRequestId);
                    return Result.NotFound("Refund request not found");
                }

                if (req.Status != RefundRequestStatus.Approved &&
                    req.Status != RefundRequestStatus.Processing)
                {
                    _logger.LogInformation(
                        "[6A.148 EXEC] RrId={RrId} not in Approved/Processing (Status={Status}); skipping",
                        refundRequestId, req.Status);
                    return Result.Success();
                }

                // Load parent registration tracked so we can advance its status.
                var registration = await _registrationRepo.GetByIdAsync(
                    req.RegistrationId, cancellationToken);
                if (registration is null)
                {
                    _logger.LogError(
                        "[6A.148 EXEC] Parent registration {RegId} not found for RrId={RrId}",
                        req.RegistrationId, refundRequestId);
                    return Result.Failure("Parent registration not found");
                }

                var trackedRequest = registration.RefundRequests.FirstOrDefault(r => r.Id == refundRequestId)
                    ?? req;

                // Iterate each Approved line item and dispatch Stripe.
                var dispatchedAny = false;
                var firstError = (string?)null;

                foreach (var line in trackedRequest.LineItems
                             .Where(l => l.Status == RefundLineItemStatus.Approved && l.ApprovedAmount is not null && l.ApprovedAmount.Amount > 0)
                             .ToList())
                {
                    var dispatchResult = await DispatchLineAsync(line, registration, cancellationToken);
                    if (dispatchResult.IsSuccess)
                        dispatchedAny = true;
                    else
                        firstError ??= dispatchResult.Error;
                }

                if (dispatchedAny)
                {
                    // Transition request: Approved → Processing (idempotent).
                    var begin = trackedRequest.BeginProcessing();
                    if (begin.IsFailure)
                        _logger.LogWarning(
                            "[6A.148 EXEC] BeginProcessing failed for RrId={RrId}: {Error}",
                            refundRequestId, begin.Error);

                    // Transition registration: PendingRefundApproval → RefundRequested
                    // (only if still in PendingRefundApproval — idempotent).
                    if (registration.Status == RegistrationStatus.PendingRefundApproval)
                    {
                        var move = registration.MoveToRefundRequestedFromApproval();
                        if (move.IsFailure)
                            _logger.LogWarning(
                                "[6A.148 EXEC] Registration MoveToRefundRequestedFromApproval failed: {Error}",
                                move.Error);
                    }

                    // If every line settled synchronously (Stripe returned "succeeded"
                    // for all of them), the request can roll to Completed immediately —
                    // no need to wait for webhook delivery.
                    var allLinesTerminal = trackedRequest.LineItems.All(l =>
                        l.Status == RefundLineItemStatus.Refunded ||
                        l.Status == RefundLineItemStatus.Failed ||
                        l.Status == RefundLineItemStatus.Rejected);
                    if (allLinesTerminal)
                        trackedRequest.MarkCompletedIfAllSettled();

                    _registrationRepo.Update(registration);
                }

                await _uow.CommitAsync(cancellationToken);

                sw.Stop();
                _logger.LogInformation(
                    "[6A.148 EXEC] DispatchAsync COMPLETE: RrId={RrId} DispatchedAny={Dispatched} Duration={ElapsedMs}ms",
                    refundRequestId, dispatchedAny, sw.ElapsedMilliseconds);

                if (!dispatchedAny && firstError is not null)
                    return Result.Failure(firstError);

                return Result.Success();
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "[6A.148 EXEC] DispatchAsync EXCEPTION: RrId={RrId} Duration={ElapsedMs}ms",
                    refundRequestId, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }

    private async Task<Result> DispatchLineAsync(
        RefundRequestLineItem line, Registration registration, CancellationToken cancellationToken)
    {
        var paymentIntentLookup = await ResolvePaymentIntentAsync(line, cancellationToken);
        if (paymentIntentLookup.IsFailure)
        {
            _logger.LogError(
                "[6A.148 EXEC] Line {LineId} Type={Type} Ref={Ref}: PaymentIntent lookup failed: {Error}",
                line.Id, line.Type, line.ReferenceId, paymentIntentLookup.Error);
            line.MarkProcessing("pending_resolution", null);
            line.MarkFailed($"Unable to resolve Stripe charge: {paymentIntentLookup.Error}");
            return Result.Failure(paymentIntentLookup.Error);
        }

        var paymentIntentId = paymentIntentLookup.Value;

        // Stripe amounts are in smallest currency unit (cents). Money.Amount is decimal.
        var amountInCents = (long)Math.Round(line.ApprovedAmount!.Amount * 100m, MidpointRounding.AwayFromZero);

        var stripeReq = new CreateRefundRequest
        {
            PaymentIntentId = paymentIntentId,
            RegistrationId = registration.Id,
            AmountInCents = amountInCents,
            Reason = "requested_by_customer",
            Metadata = new Dictionary<string, string>
            {
                ["refund_request_id"] = line.RefundRequestId.ToString(),
                ["line_item_id"] = line.Id.ToString(),
                ["line_type"] = line.Type.ToString(),
                ["reference_id"] = line.ReferenceId.ToString()
            }
        };

        try
        {
            var stripeResult = await _stripe.CreateRefundAsync(stripeReq, cancellationToken);
            if (stripeResult.IsFailure)
            {
                _logger.LogWarning(
                    "[6A.148 EXEC] Stripe CreateRefund failed for Line {LineId}: {Error}",
                    line.Id, stripeResult.Error);
                // Mark Processing first (required by state machine), then Failed.
                line.MarkProcessing($"failed_dispatch_{Guid.NewGuid():N}", null);
                line.MarkFailed(stripeResult.Error);
                return Result.Failure(stripeResult.Error);
            }

            var processingResult = line.MarkProcessing(
                stripeResult.Value.RefundId, paymentIntentId);
            if (processingResult.IsFailure)
            {
                _logger.LogWarning(
                    "[6A.148 EXEC] MarkProcessing failed for Line {LineId}: {Error}",
                    line.Id, processingResult.Error);
                return Result.Failure(processingResult.Error);
            }

            // Stripe often returns "succeeded" inline for test-mode and many real refunds.
            // Mark Refunded immediately so the request can roll to Completed without
            // depending on the webhook (which we still listen to as the authoritative
            // source). Idempotent — webhook delivery later is a no-op.
            if (string.Equals(stripeResult.Value.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
            {
                line.MarkRefunded(DateTime.UtcNow);
            }

            _logger.LogInformation(
                "[6A.148 EXEC] Line {LineId} Type={Type} dispatched: StripeRefundId={Sri} StripeStatus={Status} AmountInCents={Amt}",
                line.Id, line.Type, stripeResult.Value.RefundId, stripeResult.Value.Status, amountInCents);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[6A.148 EXEC] Line {LineId} dispatch EXCEPTION: PaymentIntentId={Pii}",
                line.Id, paymentIntentId);
            line.MarkProcessing($"exception_{Guid.NewGuid():N}", null);
            line.MarkFailed($"Exception during dispatch: {ex.Message}");
            return Result.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Look up the Stripe PaymentIntent ID for a given line item based on its Type +
    /// ReferenceId. Each line maps to ONE underlying Stripe charge (architect F9 — no
    /// aggregation by type).
    /// </summary>
    private async Task<Result<string>> ResolvePaymentIntentAsync(
        RefundRequestLineItem line, CancellationToken cancellationToken)
    {
        switch (line.Type)
        {
            case RefundLineItemType.Ticket:
            {
                // Look up by the line's ReferenceId first (preferred — caller passed the
                // exact RegistrationPayment.Id). If that misses, fall back to the parent
                // registration's INITIAL RegistrationPayment, treating ReferenceId as the
                // RegistrationId. This is the MVP path the frontend takes today — the FE
                // surfaces RegistrationId rather than the (currently unexposed)
                // RegistrationPayment.Id.
                var payment = await _paymentRepo.GetByIdAsync(line.ReferenceId, cancellationToken);
                if (payment is null)
                {
                    var initial = await _paymentRepo.GetInitialPaymentAsync(line.ReferenceId, cancellationToken);
                    if (initial is null || string.IsNullOrWhiteSpace(initial.StripePaymentIntentId))
                        return Result<string>.Failure(
                            "RegistrationPayment not found by ReferenceId nor as initial-payment-of-registration");
                    return Result<string>.Success(initial.StripePaymentIntentId);
                }
                if (string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
                    return Result<string>.Failure("RegistrationPayment has no StripePaymentIntentId");
                return Result<string>.Success(payment.StripePaymentIntentId);
            }

            case RefundLineItemType.AddOn:
            {
                var purchase = await _addOnRepo.GetByIdAsync(line.ReferenceId, cancellationToken);
                if (purchase is null || string.IsNullOrWhiteSpace(purchase.StripePaymentIntentId))
                    return Result<string>.Failure("AddOnPurchase or its PaymentIntentId not found");
                return Result<string>.Success(purchase.StripePaymentIntentId);
            }

            case RefundLineItemType.Collection:
            {
                var collection = await _collectionRepo.GetByIdAsync(line.ReferenceId, cancellationToken);
                if (collection is null || string.IsNullOrWhiteSpace(collection.StripePaymentIntentId))
                    return Result<string>.Failure("Collection or its PaymentIntentId not found");
                return Result<string>.Success(collection.StripePaymentIntentId);
            }

            case RefundLineItemType.Sponsor:
            {
                var sponsor = await _sponsorRepo.GetByIdAsync(line.ReferenceId, cancellationToken);
                if (sponsor is null || string.IsNullOrWhiteSpace(sponsor.StripePaymentIntentId))
                    return Result<string>.Failure("Sponsor or its PaymentIntentId not found");
                return Result<string>.Success(sponsor.StripePaymentIntentId);
            }

            default:
                return Result<string>.Failure($"Unsupported line item type: {line.Type}");
        }
    }
}
