using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Domain event raised when a video is removed from an event
/// Includes both video and thumbnail blob names for cleanup
/// </summary>
public record VideoRemovedFromEventDomainEvent(
    Guid EventId,
    Guid VideoId,
    string VideoBlobName,
    string ThumbnailBlobName) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
