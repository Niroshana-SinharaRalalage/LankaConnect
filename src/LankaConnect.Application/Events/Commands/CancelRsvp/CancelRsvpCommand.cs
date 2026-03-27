using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.CancelRsvp;

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
    bool RefundSponsors = false
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
    List<string>? Warnings = null
);
