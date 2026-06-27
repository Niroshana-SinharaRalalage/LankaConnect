using LankaConnect.Domain.Common;

namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

public record SignUpListRemovedFromEventDomainEvent(
    Guid EventId,
    Guid SignUpListId,
    DateTime OccurredAt) : IDomainEvent;
