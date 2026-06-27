using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

public record RegistrationQuantityUpdatedEvent(
    Guid EventId,
    Guid AttendeeId,
    int PreviousQuantity,
    int NewQuantity,
    DateTime UpdatedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
