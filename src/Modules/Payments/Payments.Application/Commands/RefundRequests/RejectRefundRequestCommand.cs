using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Modules.Payments.Application.Commands.RefundRequests;

/// <summary>
/// Phase 6A.148: Organizer declines a pending refund request with a customer-facing reason.
/// The reason is sent to the attendee in the rejection email.
/// </summary>
public record RejectRefundRequestCommand(
    Guid EventId,
    Guid RefundRequestId,
    Guid CallerUserId,
    string RejectionReason
) : ICommand;
