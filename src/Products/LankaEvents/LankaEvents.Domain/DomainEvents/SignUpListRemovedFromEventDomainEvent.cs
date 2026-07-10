using LankaConnect.BuildingBlocks.Domain.Contracts;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

public record SignUpListRemovedFromEventDomainEvent(
    Guid EventId,
    Guid SignUpListId,
    DateTime OccurredAt) : IDomainEvent;
