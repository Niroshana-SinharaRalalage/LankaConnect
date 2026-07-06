using LankaConnect.BuildingBlocks.Domain.Contracts;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

public record UserCancelledSignUpCommitmentEvent(
    Guid SignUpListId,
    Guid UserId,
    DateTime OccurredAt) : IDomainEvent;
