using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Products.LankaEvents.Application.Commands.CancelRsvp;

/// <summary>
/// Command to cancel a user's event registration
/// Phase 6A.28: Added DeleteSignUpCommitments parameter to give users choice
/// </summary>
/// <param name="EventId">The event to cancel registration for</param>
/// <param name="UserId">The user cancelling their registration</param>
/// <param name="DeleteSignUpCommitments">
/// If true, deletes all sign-up commitments and restores remaining quantities.
/// If false (default), keeps sign-up commitments intact.
/// </param>
public record CancelRsvpCommand(
    Guid EventId,
    Guid UserId,
    bool DeleteSignUpCommitments = false,
    bool DeleteFormResponses = false,
    bool RefundAddOnPurchases = false,
    // Phase 6A.137F: Collection and sponsor refund flags
    bool RefundCollections = false,
    bool RefundSponsors = false,
    // Phase 6A.148: Refund the ticket itself (default true to preserve legacy behavior
    // where paid registration cancellation always refunded the ticket). When the
    // approval workflow flag is ON, this signals whether to add a Ticket line item to
    // the RefundRequest. Attendees can opt out by unchecking the Ticket bucket.
    bool RefundTicket = true,
    // Phase 6A.148: Optional attendee-supplied reason for the refund request (shown
    // to the organizer at approval time). Empty/null is fine.
    string? RequesterReason = null
) : ICommand<CancelRsvpResult>;

/// <summary>
/// Result of a cancellation operation, including details about optional actions.
/// Enables the frontend to show what succeeded and what failed.
/// </summary>
public record CancelRsvpResult(
    bool RegistrationCancelled,
    bool? CommitmentsDeleted,
    bool? FormResponsesDeleted,
    int? FormResponsesDeletedCount,
    bool? AddOnRefundsProcessed,
    int? AddOnRefundedCount,
    int? AddOnFailedCount,
    decimal? AddOnRefundTotal,
    // Phase 6A.137F: Collection and sponsor refund results
    bool? CollectionRefundProcessed = null,
    decimal? CollectionRefundAmount = null,
    bool? SponsorRefundProcessed = null,
    decimal? SponsorRefundAmount = null,
    List<string>? Warnings = null,
    // Phase 6A.148: When the approval workflow flag is ON and the registration was
    // paid, a Pending RefundRequest is created instead of inline Stripe calls; this
    // is its ID so the FE can deep-link to the status banner.
    Guid? RefundRequestId = null
);
