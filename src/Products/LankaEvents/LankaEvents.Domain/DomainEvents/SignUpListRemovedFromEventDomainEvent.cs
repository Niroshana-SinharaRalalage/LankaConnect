using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

public record SignUpListRemovedFromEventDomainEvent(
    Guid EventId,
    Guid SignUpListId,
    DateTime OccurredAt) : IDomainEvent;
