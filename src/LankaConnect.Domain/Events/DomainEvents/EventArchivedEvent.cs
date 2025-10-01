using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

public record EventArchivedEvent(
    Guid EventId,
    DateTime ArchivedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}