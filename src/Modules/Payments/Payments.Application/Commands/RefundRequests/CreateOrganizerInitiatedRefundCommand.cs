using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Modules.Payments.Application.Commands.RefundRequests;

/// <summary>
/// Phase 6A.148: Organizer-initiated refund on behalf of an attendee. Skips
/// Pending — request is created directly in Approved and Stripe dispatch is
/// triggered via <c>RefundExecutionService</c> after the approval transaction
/// commits (architect F10).
///
/// Caller must be an organizer of the event. If <c>OverrideScanGuard</c> is true
/// and any ticket on the registration has been scanned, the override is honored
/// and <c>OrganizerNotes</c> (non-empty) is mandatory (architect F7).
/// </summary>
public record CreateOrganizerInitiatedRefundCommand(
    Guid EventId,
    Guid RegistrationId,
    Guid CallerUserId,
    string? OrganizerNotes,
    bool OverrideScanGuard,
    IReadOnlyList<RefundLineItemInputDto> LineItems
) : ICommand<CreateRefundRequestResult>;
