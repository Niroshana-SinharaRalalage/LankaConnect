using LankaConnect.Domain.Common;

namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

public record PassPurchasedEvent(
    Guid PurchaseId,
    Guid UserId,
    Guid EventId,
    Guid EventPassId,
    int Quantity,
    DateTime OccurredAt) : IDomainEvent;
