using LankaConnect.Modules.Payments.Domain.Repositories; // W4.4.d.2: 3 repo interfaces moved here
using LankaConnect.Products.LankaEvents.Application.Services; // W4.4.c.4: interfaces stay in legacy (4 cross-module consumers; circular ref otherwise)
using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Modules.Payments.Application.Services;

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
    // Phase 6A.148.W5.D6: both required. Previous nullable+default-null pattern hid DI
    // misconfiguration behind a silent WARN log — operator UAT proved these MUST be wired,
    // and the prior W5.D7 stuck-Approved refund (RR 624b07c5) sat there for hours undetected
    // partly because the reconciler couldn't see it without these deps. Non-nullable here
    // forces the DI container to throw at boot if the registration is missing.
    private readonly LankaConnect.Modules.Payments.Domain.Repositories.IRefundRequestRepository _refundRequestRepository;
    private readonly IRefundExecutionService _refundExecutionService;

    public RefundReconciliationService(
        IRegistrationRepository registrationRepository,
        IStripePaymentService stripePaymentService,
        IUnitOfWork unitOfWork,
        ILogger<RefundReconciliationService> logger,
        LankaConnect.Modules.Payments.Domain.Repositories.IRefundRequestRepository refundRequestRepository,
        IRefundExecutionService refundExecutionService)
    {
        _registrationRepository = registrationRepository;
        _stripePaymentService = stripePaymentService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _refundRequestRepository = refundRequestRepository;
        _refundExecutionService = refundExecutionService;
    }

    /// <inheritdoc />
    public async Task<Result<int>> ReconcileStuckApprovedRefundRequestsAsync(
        int? ageThresholdMinutes = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveAgeMinutes = Math.Max(0, ageThresholdMinutes ?? DefaultAgeThresholdMinutes);
        var olderThanUtc = DateTime.UtcNow.AddMinutes(-effectiveAgeMinutes);

        try
        {
            var stuck = await _refundRequestRepository.ListStuckApprovedAsync(
                olderThanUtc, cancellationToken);

            if (stuck.Count == 0)
            {
                _logger.LogDebug(
                    "[6A.148 RECON] No stuck Approved refund requests older than {Threshold:o}", olderThanUtc);
                return Result<int>.Success(0);
            }

            _logger.LogInformation(
                "[6A.148 RECON] Found {Count} stuck Approved refund requests; re-dispatching",
                stuck.Count);

            var redispatched = 0;
            foreach (var req in stuck)
            {
                // W5.D6 defensive observability: log when re-dispatch is about to touch a
                // line that ALREADY has a stripe_refund_id. This indicates either
                //   (a) Stripe succeeded the FIRST attempt but the DB commit rolled back,
                //       leaving the partial state behind (W5.D7 root-cause scenario), OR
                //   (b) a webhook updated stripe_refund_id but the line.status didn't
                //       advance for some reason.
                // In both cases the W5.D1 stable IdempotencyKey makes the re-dispatch safe
                // (Stripe returns the prior refund, no duplicate charge), but the situation
                // is unusual and operator-visible — we want it surfaced in logs.
                var linesWithExistingRefundId = req.LineItems
                    .Where(l => !string.IsNullOrWhiteSpace(l.StripeRefundId))
                    .ToList();
                if (linesWithExistingRefundId.Count > 0)
                {
                    _logger.LogWarning(
                        "[6A.148.W5.D6 RECON] Stuck RrId={RrId} has {Count} line(s) with existing " +
                        "stripe_refund_id (likely prior dispatch's Stripe succeeded but DB " +
                        "transaction rolled back). W5.D1 idempotency key makes re-dispatch " +
                        "safe — Stripe returns prior refund, no duplicate charge. Line IDs: [{LineIds}]",
                        req.Id, linesWithExistingRefundId.Count,
                        string.Join(",", linesWithExistingRefundId.Select(l => l.Id.ToString("N"))));
                }

                try
                {
                    var dispatchResult = await _refundExecutionService.DispatchAsync(
                        req.Id, cancellationToken);
                    if (dispatchResult.IsSuccess)
                        redispatched++;
                    else
                        _logger.LogWarning(
                            "[6A.148 RECON] Re-dispatch failed for RrId={RrId} Error={Error}",
                            req.Id, dispatchResult.Error);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[6A.148 RECON] Re-dispatch EXCEPTION for RrId={RrId}", req.Id);
                }
            }

            _logger.LogInformation(
                "[6A.148 RECON] Stuck-Approved pass COMPLETE: re-dispatched {Count} of {Total}",
                redispatched, stuck.Count);
            return Result<int>.Success(redispatched);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[6A.148 RECON] ReconcileStuckApprovedRefundRequestsAsync EXCEPTION");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Result<int>> ReconcileStuckCancelledWithRefundedTicketAsync(
        int? ageThresholdMinutes = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveAgeMinutes = Math.Max(0, ageThresholdMinutes ?? DefaultAgeThresholdMinutes);
        var processedBefore = DateTime.UtcNow.AddMinutes(-effectiveAgeMinutes);

        try
        {
            var stuck = await _registrationRepository
                .GetStuckCancelledWithRefundedTicketAsync(
                    processedBefore, DefaultBatchSize, cancellationToken);

            if (stuck.Count == 0)
            {
                _logger.LogDebug(
                    "[6A.148.W5.5.D6.5 RECON] No stuck Cancelled-with-refunded-ticket registrations older than {Threshold:o}",
                    processedBefore);
                return Result<int>.Success(0);
            }

            _logger.LogInformation(
                "[6A.148.W5.5.D6.5 RECON] Found {Count} stuck Cancelled-with-refunded-ticket registrations; healing",
                stuck.Count);

            var healed = 0;
            foreach (var (registration, stripeRefundId) in stuck)
            {
                try
                {
                    var result = registration.CompleteRefundFromCancelled(stripeRefundId);
                    if (result.IsFailure)
                    {
                        _logger.LogWarning(
                            "[6A.148.W5.5.D6.5 RECON] CompleteRefundFromCancelled rejected for RegId={RegId} (already-transitioned race or guard hit): {Error}",
                            registration.Id, result.Error);
                        continue;
                    }

                    _registrationRepository.Update(registration);
                    await _unitOfWork.CommitAsync(cancellationToken);
                    healed++;

                    _logger.LogInformation(
                        "[6A.148.W5.5.D6.5 RECON] Healed stuck Cancelled→Refunded RegId={RegId} StripeRefundId={Sri}",
                        registration.Id, stripeRefundId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[6A.148.W5.5.D6.5 RECON] EXCEPTION healing RegId={RegId}; will retry next pass",
                        registration.Id);
                }
            }

            _logger.LogInformation(
                "[6A.148.W5.5.D6.5 RECON] Pass COMPLETE: healed {Count} of {Total}",
                healed, stuck.Count);
            return Result<int>.Success(healed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[6A.148.W5.5.D6.5 RECON] ReconcileStuckCancelledWithRefundedTicketAsync EXCEPTION");
            throw;
        }
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
