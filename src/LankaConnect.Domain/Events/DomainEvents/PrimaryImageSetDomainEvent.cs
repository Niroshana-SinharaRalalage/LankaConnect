using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Domain event raised when an image is set as the primary/main thumbnail for an event
/// Phase 6A.13: Primary image selection feature
/// </summary>
public record PrimaryImageSetDomainEvent(Guid EventId, Guid ImageId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
