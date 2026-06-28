using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

public record EventApprovedEvent(
    Guid EventId,
    Guid ApprovedByAdminId,
    DateTime ApprovedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
