using LankaConnect.Domain.Common;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

public record EventApprovedEvent(
    Guid EventId,
    Guid ApprovedByAdminId,
    DateTime ApprovedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
