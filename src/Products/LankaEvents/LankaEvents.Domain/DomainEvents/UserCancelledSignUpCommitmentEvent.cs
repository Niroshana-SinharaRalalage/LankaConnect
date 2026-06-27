using LankaConnect.Domain.Common;

namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

public record UserCancelledSignUpCommitmentEvent(
    Guid SignUpListId,
    Guid UserId,
    DateTime OccurredAt) : IDomainEvent;
