using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Events.Services;

/// <summary>
/// Phase 7G — durable self-healing fix for the "stuck refund" bug class.
///
/// Background context: when a buyer cancels a paid registration, we call
/// <c>RegistrationRefundService.ProcessRefundAsync</c> which (a) creates the
/// Stripe refund and (b) flips the registration to <c>RefundRequested</c>.
/// Stripe processes the refund asynchronously and credits the buyer's card.
/// A <c>charge.refunded</c> webhook is then expected to arrive and complete
/// the DB transition to <c>Refunded</c> via <c>Registration.CompleteRefund</c>.
///
/// The webhook can be missed when the API container restarts mid-delivery
/// (deploy windows are the common cause on staging). Stripe retries for ~3
/// days then gives up. Until this slice, the registration stayed locked in
/// <c>RefundRequested</c> forever — money returned to the card, but our
/// "Refund in Progress" UI remained stuck.
///
/// This service is the safety net: it scans for registrations that have been
/// in <c>RefundRequested</c> longer than a configured grace period, queries
/// Stripe directly for the refund's actual status using the persisted
/// <c>StripeRefundId</c>, and completes the DB transition for refunds Stripe
/// reports as <c>succeeded</c>. The work is idempotent — concurrent runs and
/// re-runs converge on the same state.
/// </summary>
public interface IRefundReconciliationService
{
    /// <summary>
    /// Reconciles every registration whose <c>RefundRequested</c> state has
    /// outlived the grace period.
    /// </summary>
    /// <param name="batchSize">
    /// Optional max number of stuck registrations to process this run.
    /// <c>null</c> uses the configured default (<c>RefundReconciliationSettings.BatchSize</c>).
    /// </param>
    /// <param name="cancellationToken">Cooperative cancellation token.</param>
    /// <returns>
    /// A summary of the reconciliation pass — number scanned, number fixed,
    /// number still pending at Stripe, number Stripe reports as failed/canceled,
    /// plus any human-readable warnings logged for visibility.
    /// Always returns Success unless the entire pass faulted before any work.
    /// </returns>
    Task<Result<RefundReconciliationResult>> ReconcileStuckRefundsAsync(
        int? batchSize = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome of a single reconciliation pass.
/// </summary>
public record RefundReconciliationResult(
    int ScannedCount,
    int ReconciledCount,
    int StillPendingCount,
    int FailedAtStripeCount,
    int MissingRefundIdCount,
    int StripeLookupFailedCount,
    IReadOnlyList<string> Warnings);
