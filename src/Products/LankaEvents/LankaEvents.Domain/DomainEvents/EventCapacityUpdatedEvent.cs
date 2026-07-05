using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

public record EventCapacityUpdatedEvent(
    Guid EventId,
    int PreviousCapacity,
    int NewCapacity,
    DateTime UpdatedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}