using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events.Repositories;

/// <summary>
/// Phase 6A.148 — repository for the refund_requests aggregate-internal entity.
///
/// Separate from <c>IRegistrationRepository</c> so the organizer queue can be queried
/// with AsNoTracking projections without round-tripping through the full Registration
/// graph (architect recommendation §1 of review).
///
/// All command-side operations are performed via the Registration aggregate (which
/// owns the navigation); this repository provides read-side queries + the lookup
/// used by the application-layer command handlers to load the request for
/// Approve / Reject / Withdraw / BeginProcessing.
/// </summary>
public interface IRefundRequestRepository
{
    /// <summary>
    /// Loads a single request with its line items. Tracked (caller intends to mutate).
    /// </summary>
    Task<RefundRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all refund requests for a given registration, including all line items.
    /// Tracked. Used by the application layer when loading a registration's full refund
    /// history (e.g. for the attendee /me endpoint).
    /// </summary>
    Task<IReadOnlyList<RefundRequest>> GetByRegistrationIdAsync(
        Guid registrationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the attendee's most recent / current refund request for the given event,
    /// or null. Tracked = false (read-only projection for /me endpoint).
    /// </summary>
    Task<RefundRequest?> GetMyMostRecentForEventAsync(
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns refund requests for the given event, optionally filtered by status.
    /// Used by the organizer queue UI. Includes line items for the per-line summary card.
    /// Untracked (read-only projection).
    /// </summary>
    Task<IReadOnlyList<RefundRequest>> ListByEventAsync(
        Guid eventId,
        RefundRequestStatus? statusFilter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Architect F11: returns RefundRequests stuck in <see cref="RefundRequestStatus.Approved"/>
    /// for longer than the given threshold. Used by <c>RefundReconciliationService</c> to
    /// re-dispatch Stripe calls that were lost because the process crashed between Approve
    /// commit and RefundExecutionService dispatch.
    /// </summary>
    Task<IReadOnlyList<RefundRequest>> ListStuckApprovedAsync(
        DateTime olderThanUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.148.D9: returns true when the given Stripe sponsor refund was dispatched
    /// through the approval workflow (i.e. there exists a <see cref="RefundRequestLineItem"/>
    /// of type <see cref="RefundLineItemType.Sponsor"/> whose <c>ReferenceId</c> matches
    /// <paramref name="sponsorId"/> AND whose <c>StripeRefundId</c> matches
    /// <paramref name="stripeRefundId"/>).
    ///
    /// Used by <c>SponsorWebhookHandler</c> to suppress the legacy per-Sponsor "Sponsorship
    /// Refund Confirmation" email when the consolidated D8 decision email has already
    /// covered the attendee (operator UAT defect E3).
    ///
    /// Untracked AnyAsync — single index hit on (Type, ReferenceId, StripeRefundId).
    ///
    /// W4.D12 NOTE: superseded by the more general <see cref="GetWorkflowLineReferenceIdAsync"/>
    /// (which works for the AddOnPurchase cart case where the entity ID is not known
    /// until the lookup returns). Sponsor + Collection handlers may continue using this
    /// boolean shim, or migrate to the new method.
    /// </summary>
    Task<bool> ExistsWorkflowLineItemForSponsorAsync(
        Guid sponsorId,
        string stripeRefundId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.148.W4.D12 (G4 generalised dedupe): given a Stripe refund ID and a line
    /// item type, returns the <c>ReferenceId</c> of the workflow line that owns the
    /// refund, or <c>null</c> if no workflow line is found.
    ///
    /// Use cases:
    /// - <b>AddOnPurchase webhook</b>: for cart purchases where N AddOnPurchase rows
    ///   share one PaymentIntent, this narrows the webhook to the exact row.
    /// - <b>Sponsor / Collection / Registration webhooks</b>: dedupe guard — if the
    ///   returned ID matches the entity the handler is processing, the refund is
    ///   workflow-owned and the legacy per-entity email should be suppressed.
    ///
    /// Untracked single-row projection. The (Type, StripeRefundId) tuple is unique by
    /// construction (Stripe refund IDs are globally unique; one workflow line per refund).
    /// </summary>
    Task<Guid?> GetWorkflowLineReferenceIdAsync(
        RefundLineItemType type,
        string stripeRefundId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.148.W5.D2: load a single <see cref="RefundRequestLineItem"/> by Id without
    /// the parent <see cref="RefundRequest"/> aggregate.
    ///
    /// Used by <c>IRefundLineDispatcher</c> to update one line in a fresh scope without
    /// triggering EF's aggregate-wide change tracking on the Registration root. This is
    /// the core mechanism by which per-line Stripe dispatch survives concurrent
    /// Registration writes (the W5.D7 xmin clash root cause) — only the
    /// refund_request_line_items row is touched, no Registration / RefundRequest row
    /// writes, no xmin conflict surface.
    ///
    /// Tracked because the caller will mutate (MarkProcessing / MarkRefunded /
    /// MarkFailed) and SaveChangesAsync within the same scope.
    /// </summary>
    Task<RefundRequestLineItem?> GetLineItemByIdAsync(
        Guid lineItemId,
        CancellationToken cancellationToken = default);
}
