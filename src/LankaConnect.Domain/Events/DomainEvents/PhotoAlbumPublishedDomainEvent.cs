using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Domain event raised when a photo album is published.
/// Triggers email notification to registered attendees.
/// </summary>
public record PhotoAlbumPublishedDomainEvent(Guid AlbumId, Guid EventId, string EventTitle) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
