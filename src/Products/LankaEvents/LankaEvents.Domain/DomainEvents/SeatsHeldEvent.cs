using LankaConnect.BuildingBlocks.Domain.Contracts;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Domain event raised when seats are temporarily held for a user during checkout.
/// </summary>
public record SeatsHeldEvent(
    Guid EventId,
    Guid UserId,
    List<Guid> SeatIds,
    string SessionId,
    DateTime ExpiresAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
