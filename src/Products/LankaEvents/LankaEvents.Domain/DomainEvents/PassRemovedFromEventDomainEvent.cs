using LankaConnect.Domain.Common;

namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

public record PassRemovedFromEventDomainEvent(
    Guid EventId,
    Guid PassId,
    DateTime OccurredAt) : IDomainEvent;
