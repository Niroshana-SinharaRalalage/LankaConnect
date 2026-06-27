using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

public record SignUpListUpdatedEvent(
    Guid SignUpListId,
    string Category,
    string Description,
    bool HasMandatoryItems,
    bool HasPreferredItems,
    bool HasSuggestedItems,
    DateTime OccurredAt) : IDomainEvent;
