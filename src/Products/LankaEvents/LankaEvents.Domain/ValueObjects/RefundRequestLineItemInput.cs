using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects;
namespace LankaConnect.Products.LankaEvents.Domain.ValueObjects;

/// <summary>
/// Phase 6A.148: Input describing a single bucket the attendee (or organizer-on-behalf)
/// wants refunded. Aggregate validates uniqueness per (Type, ReferenceId) in
/// <c>Registration.CreateRefundRequest</c> (architect F9).
/// </summary>
public record RefundRequestLineItemInput(
    RefundLineItemType Type,
    Guid ReferenceId,
    Money RequestedAmount);
