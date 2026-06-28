using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

public record EventCapacityUpdatedEvent(
    Guid EventId,
    int PreviousCapacity,
    int NewCapacity,
    DateTime UpdatedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}