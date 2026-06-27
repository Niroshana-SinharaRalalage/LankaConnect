using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

public record EventRejectedEvent(
    Guid EventId,
    Guid RejectedByAdminId,
    string Reason,
    DateTime RejectedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
