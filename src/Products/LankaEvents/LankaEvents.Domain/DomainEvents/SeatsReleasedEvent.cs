using LankaConnect.Domain.Common;

namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Domain event raised when seats are released back to availability
/// (due to cancellation, refund, or hold expiry).
/// </summary>
public record SeatsReleasedEvent(
    Guid EventId,
    List<Guid> SeatIds,
    string Reason
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
