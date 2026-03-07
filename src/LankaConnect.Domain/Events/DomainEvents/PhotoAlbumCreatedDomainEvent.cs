using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Domain event raised when a photo album is created for an event.
/// </summary>
public record PhotoAlbumCreatedDomainEvent(Guid AlbumId, Guid EventId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
