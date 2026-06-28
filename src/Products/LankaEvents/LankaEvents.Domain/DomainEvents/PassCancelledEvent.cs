using LankaConnect.Domain.Common;

namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

public record PassCancelledEvent(
    Guid PurchaseId,
    Guid UserId,
    Guid EventId,
    int Quantity,
    DateTime OccurredAt) : IDomainEvent;
