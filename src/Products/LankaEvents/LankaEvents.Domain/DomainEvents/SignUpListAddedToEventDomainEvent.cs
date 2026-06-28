using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

public record SignUpListAddedToEventDomainEvent(
    Guid EventId,
    Guid SignUpListId,
    string Category,
    DateTime OccurredAt) : IDomainEvent;
