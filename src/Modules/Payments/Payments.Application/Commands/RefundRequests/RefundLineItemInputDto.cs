using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.BuildingBlocks.Domain.Shared.Enums;
namespace LankaConnect.Modules.Payments.Application.Commands.RefundRequests;

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
