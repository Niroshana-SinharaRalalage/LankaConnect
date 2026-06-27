using LankaConnect.Domain.Common;

namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

public record SignUpListAddedToEventDomainEvent(
    Guid EventId,
    Guid SignUpListId,
    string Category,
    DateTime OccurredAt) : IDomainEvent;
