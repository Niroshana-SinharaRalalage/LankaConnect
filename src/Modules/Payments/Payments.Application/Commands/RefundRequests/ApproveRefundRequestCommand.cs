using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Shared.Enums;
namespace LankaConnect.Modules.Payments.Application.Commands.RefundRequests;

/// <summary>
/// Phase 6A.148: Organizer approves an attendee's pending refund request with
/// per-line approved amounts. Per architect F2, all-zero approvals are rejected
/// (returns 400 ValidationError); organizer must use Reject instead.
///
/// Concurrency: handler uses tracked load + SaveChangesAsync; the xmin token
/// catches simultaneous approves and surfaces as 409 Conflict.
/// </summary>
public record ApproveRefundRequestCommand(
    Guid EventId,
    Guid RefundRequestId,
    Guid CallerUserId,
    string? OrganizerNotes,
    IReadOnlyList<ApproveLineItemInputDto> PerLineApprovedAmounts
) : ICommand;

public record ApproveLineItemInputDto(
    Guid LineItemId,
    decimal ApprovedAmount,
    Currency Currency);
