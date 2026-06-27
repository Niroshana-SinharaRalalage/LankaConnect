using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

public record EventActivatedEvent(
    Guid EventId,
    DateTime ActivatedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}