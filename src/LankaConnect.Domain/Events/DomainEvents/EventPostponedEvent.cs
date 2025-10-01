using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

public record EventPostponedEvent(
    Guid EventId,
    string Reason,
    DateTime PostponedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}