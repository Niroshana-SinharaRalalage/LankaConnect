using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

public record UserCancelledSignUpCommitmentEvent(
    Guid SignUpListId,
    Guid UserId,
    DateTime OccurredAt) : IDomainEvent;
