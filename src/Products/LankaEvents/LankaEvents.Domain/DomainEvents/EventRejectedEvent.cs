using LankaConnect.Domain.Common;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

public record EventRejectedEvent(
    Guid EventId,
    Guid RejectedByAdminId,
    string Reason,
    DateTime RejectedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
