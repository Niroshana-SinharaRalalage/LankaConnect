using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Services;

/// <inheritdoc cref="IRefundReconciliationService"/>
public class RefundReconciliationService : IRefundReconciliationService
{
    /// <summary>
    /// Default grace period before the safety net touches a row — gives the
    /// primary <c>charge.refunded</c> webhook a fair chance to arrive on its
    /// own. Stripe usually delivers within seconds, occasionally a few minutes
    /// during retries; 10 minutes is a generous floor that still keeps the UI
    /// fix prompt for stuck rows.
    /// </summary>
    private const int DefaultAgeThresholdMinutes = 10;

    /// <summary>
    /// Default max rows to process per pass. Keeps the Stripe API call
    /// volume bounded and prevents one giant transaction from blocking
    /// other writers when a backlog accumulates.
    /// </summary>
    private const int DefaultBatchSize = 50;

    private readonly IRegistrationRepository _registrationRepository;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefundReconciliationService> _logger;

    public RefundReconciliationService(
        IRegistrationRepository registrationRepository,
        IStripePaymentService stripePaymentService,
        IUnitOfWork unitOfWork,
        ILogger<RefundReconciliationService> logger)
    {
        _registrationRepository = registrationRepository;
        _stripePaymentService = stripePaymentService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<RefundReconciliationResult>> ReconcileStuckRefundsAsync(
        int? batchSize = null,
        int? ageThresholdMinutes = null,
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();
        var effectiveBatchSize = Math.Max(1, batchSize ?? DefaultBatchSize);
        var effectiveAgeMinutes = Math.Max(0, ageThresholdMinutes ?? DefaultAgeThresholdMinutes);
        var requestedBefore = DateTime.UtcNow.AddMinutes(-effectiveAgeMinutes);

        using (LogContext.PushProperty("Operation", "RefundReconciliation"))
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            _logger.LogInformation(
                "[Phase 7G] [Reconcile-1] START - CorrelationId={CorrelationId}, BatchSize={BatchSize}, AgeThresholdMinutes={AgeThresholdMinutes}, RequestedBefore={RequestedBefore:o}",
                correlationId, effectiveBatchSize, effectiveAgeMinutes, requestedBefore);

            int reconciled = 0;
            int stillPending = 0;
            int failedAtStripe = 0;
            int missingRefundId = 0;
            int stripeLookupFailed = 0;
            var warnings = new List<string>();

            IReadOnlyList<Registration> stuck;
            try
            {
                stuck = await _registrationRepository.GetStuckRefundsAsync(
                    requestedBefore, effectiveBatchSize, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Phase 7G] [Reconcile-ERR] Failed to load stuck refunds - CorrelationId={CorrelationId}",
                    correlationId);
                return Result<RefundReconciliationResult>.Failure(
                    $"Failed to load stuck refunds: {ex.Message}");
            }

            if (stuck.Count == 0)
            {
                stopwatch.Stop();
                _logger.LogInformation(
                    "[Phase 7G] [Reconcile-2] No stuck refunds - CorrelationId={CorrelationId}, Duration={ElapsedMs}ms",
                    correlationId, stopwatch.ElapsedMilliseconds);
                return Result<RefundReconciliationResult>.Success(new RefundReconciliationResult(
                    ScannedCount: 0,
                    ReconciledCount: 0,
                    StillPendingCount: 0,
                    FailedAtStripeCount: 0,
                    MissingRefundIdCount: 0,
                    StripeLookupFailedCount: 0,
                    Warnings: Array.Empty<string>()));
            }

            _logger.LogInformation(
                "[Phase 7G] [Reconcile-2] Loaded {Count} stuck registrations - CorrelationId={CorrelationId}",
                stuck.Count, correlationId);

            foreach (var registration in stuck)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (LogContext.PushProperty("RegistrationId", registration.Id))
                using (LogContext.PushProperty("EventId", registration.EventId))
                {
                    var refundId = registration.StripeRefundId;
                    if (string.IsNullOrWhiteSpace(refundId))
                    {
                        missingRefundId++;
                        var warning =
                            $"Registration {registration.Id} is stuck in RefundRequested but has no StripeRefundId — manual intervention required.";
                        warnings.Add(warning);
                        _logger.LogWarning(
                            "[Phase 7G] [Reconcile-3a] Missing StripeRefundId - CorrelationId={CorrelationId}, RegistrationId={RegistrationId}, RefundRequestedAt={RefundRequestedAt:o}",
                            correlationId, registration.Id,
                            registration.RefundRequestedAt?.ToString("o") ?? "null");
                        continue;
                    }

                    Result<StripeRefundResult> lookup;
                    try
                    {
                        lookup = await _stripePaymentService.GetRefundStatusAsync(
                            refundId, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        stripeLookupFailed++;
                        _logger.LogError(ex,
                            "[Phase 7G] [Reconcile-3b] Stripe lookup threw - CorrelationId={CorrelationId}, RegistrationId={RegistrationId}, RefundId={RefundId}",
                            correlationId, registration.Id, refundId);
                        warnings.Add(
                            $"Stripe lookup faulted for registration {registration.Id} (refund {refundId}): {ex.Message}");
                        continue;
                    }

                    if (lookup.IsFailure)
                    {
                        stripeLookupFailed++;
                        _logger.LogWarning(
                            "[Phase 7G] [Reconcile-3c] Stripe lookup failed - CorrelationId={CorrelationId}, RegistrationId={RegistrationId}, RefundId={RefundId}, Error={Error}",
                            correlationId, registration.Id, refundId, lookup.Error);
                        warnings.Add(
                            $"Stripe lookup failed for registration {registration.Id} (refund {refundId}): {lookup.Error}");
                        continue;
                    }

                    var status = lookup.Value.Status?.ToLowerInvariant();
                    _logger.LogInformation(
                        "[Phase 7G] [Reconcile-4] Stripe status - CorrelationId={CorrelationId}, RegistrationId={RegistrationId}, RefundId={RefundId}, Status={Status}",
                        correlationId, registration.Id, refundId, status);

                    switch (status)
                    {
                        case "succeeded":
                            await TryReconcileSucceededAsync(
                                registration, refundId!, correlationId, cancellationToken,
                                onReconciled: () => reconciled++,
                                onWarning: warnings.Add);
                            break;

                        case "failed":
                        case "canceled":
                            failedAtStripe++;
                            warnings.Add(
                                $"Stripe reports refund {refundId} as {status} for registration {registration.Id} — manual intervention required.");
                            _logger.LogWarning(
                                "[Phase 7G] [Reconcile-5] Stripe terminal-failure status - CorrelationId={CorrelationId}, RegistrationId={RegistrationId}, RefundId={RefundId}, Status={Status}",
                                correlationId, registration.Id, refundId, status);
                            break;

                        case "pending":
                        case "requires_action":
                        default:
                            stillPending++;
                            _logger.LogInformation(
                                "[Phase 7G] [Reconcile-6] Refund still in flight at Stripe - CorrelationId={CorrelationId}, RegistrationId={RegistrationId}, RefundId={RefundId}, Status={Status}",
                                correlationId, registration.Id, refundId, status);
                            break;
                    }
                }
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "[Phase 7G] [Reconcile-7] DONE - CorrelationId={CorrelationId}, Scanned={Scanned}, Reconciled={Reconciled}, StillPending={Pending}, FailedAtStripe={Failed}, MissingRefundId={Missing}, StripeLookupFailed={LookupFailed}, Duration={ElapsedMs}ms",
                correlationId, stuck.Count, reconciled, stillPending, failedAtStripe,
                missingRefundId, stripeLookupFailed, stopwatch.ElapsedMilliseconds);

            return Result<RefundReconciliationResult>.Success(new RefundReconciliationResult(
                ScannedCount: stuck.Count,
                ReconciledCount: reconciled,
                StillPendingCount: stillPending,
                FailedAtStripeCount: failedAtStripe,
                MissingRefundIdCount: missingRefundId,
                StripeLookupFailedCount: stripeLookupFailed,
                Warnings: warnings.AsReadOnly()));
        }
    }

    private async Task TryReconcileSucceededAsync(
        Registration registration,
        string refundId,
        Guid correlationId,
        CancellationToken cancellationToken,
        Action onReconciled,
        Action<string> onWarning)
    {
        try
        {
            var transition = registration.CompleteRefund(refundId);
            if (transition.IsFailure)
            {
                // Most likely race — webhook arrived between our load and now,
                // already moved the row to Refunded. Treat as success-ish: the
                // outer count won't increment because we don't call onReconciled,
                // but we don't escalate either.
                _logger.LogInformation(
                    "[Phase 7G] [Reconcile-8a] CompleteRefund refused — likely already-reconciled race - CorrelationId={CorrelationId}, RegistrationId={RegistrationId}, Status={Status}, Error={Error}",
                    correlationId, registration.Id, registration.Status, transition.Error);
                return;
            }

            // Persist within its own commit so a failure on a later row doesn't
            // roll back this row. Mirrors the per-row commit pattern used by
            // other reconciliation jobs in this codebase.
            await _unitOfWork.CommitAsync(cancellationToken);
            onReconciled();

            _logger.LogInformation(
                "[Phase 7G] [Reconcile-8b] SUCCESS — registration transitioned RefundRequested→Refunded - CorrelationId={CorrelationId}, RegistrationId={RegistrationId}, RefundId={RefundId}, RefundCompletedAt={RefundCompletedAt:o}",
                correlationId, registration.Id, refundId,
                registration.RefundCompletedAt?.ToString("o") ?? "null");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 7G] [Reconcile-8c] Persist failed during reconciliation - CorrelationId={CorrelationId}, RegistrationId={RegistrationId}, RefundId={RefundId}",
                correlationId, registration.Id, refundId);
            onWarning(
                $"Failed to commit reconciliation for registration {registration.Id}: {ex.Message}");
        }
    }
}
