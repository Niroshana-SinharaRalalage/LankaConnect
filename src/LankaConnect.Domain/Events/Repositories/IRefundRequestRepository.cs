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
    /// </summary>
    Task<bool> ExistsWorkflowLineItemForSponsorAsync(
        Guid sponsorId,
        string stripeRefundId,
        CancellationToken cancellationToken = default);
}
