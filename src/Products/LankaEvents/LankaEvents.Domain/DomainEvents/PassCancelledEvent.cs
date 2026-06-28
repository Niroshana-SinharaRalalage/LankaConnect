using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

public record PassCancelledEvent(
    Guid PurchaseId,
    Guid UserId,
    Guid EventId,
    int Quantity,
    DateTime OccurredAt) : IDomainEvent;
