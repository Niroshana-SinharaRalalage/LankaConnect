using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Domain event raised when an image is removed from an event
/// Contains BlobName for cleanup in event handler
/// </summary>
public record ImageRemovedFromEventDomainEvent(Guid EventId, Guid ImageId, string BlobName) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
