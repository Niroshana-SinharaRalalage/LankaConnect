using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

public record UserCommittedToSignUpEvent(
    Guid SignUpListId,
    Guid UserId,
    string ItemDescription,
    int Quantity,
    DateTime OccurredAt) : IDomainEvent;
