using LankaConnect.Domain.Common;
namespace LankaConnect.Products.LankaEvents.Application.Services;

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
    /// <param name="ageThresholdMinutes">
    /// Optional override for the grace period before a row is considered stuck.
    /// Production background passes use the default (10 min) so the primary
    /// webhook gets a fair chance to arrive first. Operators triggering the
    /// reconciler manually during incident response can pass <c>0</c> to
    /// reconcile immediately — useful when staging deploy collisions create a
    /// known-broken row that the operator wants healed before the next pass.
    /// <c>null</c> uses the default. Negative values are clamped to <c>0</c>.
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
        int? ageThresholdMinutes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.148 (architect F11): re-dispatches refund requests that are stuck in
    /// <see cref="LankaConnect.Products.LankaEvents.Domain.Enums.RefundRequestStatus.Approved"/>.
    /// This state happens when the approve transaction commits but the post-commit
    /// <c>RefundExecutionService.DispatchAsync</c> call crashed before any line item
    /// reached Stripe (process restart, container OOM, etc.).
    ///
    /// Idempotent — dispatch skips line items not in Approved. Safe to call repeatedly
    /// from the same background tick.
    /// </summary>
    /// <param name="ageThresholdMinutes">
    /// Grace period before a row is considered stuck. Default 10 minutes to give the
    /// inline dispatch a fair chance to complete.
    /// </param>
    Task<Result<int>> ReconcileStuckApprovedRefundRequestsAsync(
        int? ageThresholdMinutes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.148.W5.5.D6.5 — heals registrations stuck in
    /// <see cref="LankaConnect.Products.LankaEvents.Domain.Enums.RegistrationStatus.Cancelled"/>
    /// whose workflow ticket-line refund has already settled at Stripe
    /// (<c>RefundRequestLineItem.Type=Ticket</c>, <c>Status=Refunded</c>,
    /// <c>StripeRefundId NOT NULL</c>) but whose registration row was never advanced
    /// to <c>Refunded</c>. The exact stuck pattern the operator UAT surfaced on
    /// 2026-05-22 (registration <c>8df17ec1</c>): a <c>charge.refunded</c> webhook
    /// for a multi-refund PI was misrouted (Bug 1) so the ticket refund's typed
    /// handler never ran, leaving the registration <c>Cancelled</c> with no
    /// <c>StripeRefundId</c> / <c>RefundCompletedAt</c>.
    ///
    /// W5.5.D4 fixes the routing for future webhooks. This method is the durable
    /// safety net for any rows that slip through despite the fix, AND backfills any
    /// rows that accumulated during the bug window before deploy. Calls
    /// <see cref="LankaConnect.Products.LankaEvents.Domain.Registration.CompleteRefundFromCancelled"/>
    /// per stuck row (the W5.D4 domain transition that permits Cancelled → Refunded).
    /// Idempotent — second runs on already-Refunded rows are no-ops via the domain
    /// method's same-SRI guard.
    /// </summary>
    /// <param name="ageThresholdMinutes">
    /// Grace period before a row is considered stuck. Default 10 minutes. Stripe
    /// webhook delivery latency is sub-second under normal conditions; 10 minutes
    /// generously covers webhook retry windows + dispatcher commit propagation.
    /// </param>
    Task<Result<int>> ReconcileStuckCancelledWithRefundedTicketAsync(
        int? ageThresholdMinutes = null,
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
