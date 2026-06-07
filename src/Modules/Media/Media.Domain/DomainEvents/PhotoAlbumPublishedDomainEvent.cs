using LankaConnect.Domain.Common;

namespace LankaConnect.Modules.Media.Domain.DomainEvents;

/// <summary>
/// Domain event raised when a photo album is published.
/// Used for logging/auditing. Email notification is decoupled (sent via SendAlbumNotificationCommand).
/// </summary>
public record PhotoAlbumPublishedDomainEvent(Guid AlbumId, Guid EventId, string EventTitle, string AlbumName) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
