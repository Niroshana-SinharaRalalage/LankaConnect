using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Application.Events.Commands.RefundRequests;

/// <summary>
/// Phase 6A.148: Application-layer DTO for a single refund line item, used in both
/// CreateRefundRequestCommand (attendee) and CreateOrganizerInitiatedRefundCommand.
/// Maps 1:1 to a domain <c>RefundRequestLineItemInput</c> after currency parsing.
/// </summary>
public record RefundLineItemInputDto(
    RefundLineItemType Type,
    Guid ReferenceId,
    decimal RequestedAmount,
    Currency Currency);
