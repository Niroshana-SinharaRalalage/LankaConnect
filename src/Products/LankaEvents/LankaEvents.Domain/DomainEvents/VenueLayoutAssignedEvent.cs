using LankaConnect.Domain.Common;

namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Domain event raised when a venue layout is assigned to an event.
/// </summary>
public record VenueLayoutAssignedEvent(
    Guid EventId,
    Guid VenueLayoutId
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
