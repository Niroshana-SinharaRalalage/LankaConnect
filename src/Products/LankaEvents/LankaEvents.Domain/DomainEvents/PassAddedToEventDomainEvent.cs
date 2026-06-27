using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;

namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

public record PassAddedToEventDomainEvent(
    Guid EventId,
    Guid PassId,
    PassName PassName,
    DateTime OccurredAt) : IDomainEvent;
