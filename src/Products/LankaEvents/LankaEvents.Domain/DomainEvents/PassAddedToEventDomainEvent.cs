using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Domain.Events.DomainEvents;

public record PassAddedToEventDomainEvent(
    Guid EventId,
    Guid PassId,
    PassName PassName,
    DateTime OccurredAt) : IDomainEvent;
