using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

public record PassRemovedFromEventDomainEvent(
    Guid EventId,
    Guid PassId,
    DateTime OccurredAt) : IDomainEvent;
