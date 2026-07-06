using LankaConnect.BuildingBlocks.Domain.Contracts;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Domain event raised when an add-on purchase payment is completed via Stripe webhook.
/// Triggers:
/// - Sending add-on purchase receipt email to buyer
/// </summary>
public record AddOnPurchaseCompletedEvent(
    Guid EventId,
    Guid AddOnPurchaseId,
    Guid AddOnDefinitionId,
    Guid? BuyerUserId,
    string BuyerName,
    string BuyerEmail,
    string PaymentIntentId,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount,
    string Currency,
    DateTime PaymentCompletedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
