using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

public record EventPublishedEvent(
    Guid EventId,
    DateTime PublishedAt,
    Guid PublishedBy
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}