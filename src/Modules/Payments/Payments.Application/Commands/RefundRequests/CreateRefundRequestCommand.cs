using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.SharedKernel.Identity;
namespace LankaConnect.Modules.Payments.Application.Commands.RefundRequests;

/// <summary>
/// Phase 6A.148: Attendee-initiated refund request creation.
/// The caller's UserId must match the registration's UserId. Creates a Pending
/// request that an organizer must approve before any Stripe call is made.
/// </summary>
public record CreateRefundRequestCommand(
    Guid EventId,
    Guid CallerUserId,
    string? RequesterReason,
    IReadOnlyList<RefundLineItemInputDto> LineItems
) : ICommand<CreateRefundRequestResult>;

public record CreateRefundRequestResult(Guid RefundRequestId);
