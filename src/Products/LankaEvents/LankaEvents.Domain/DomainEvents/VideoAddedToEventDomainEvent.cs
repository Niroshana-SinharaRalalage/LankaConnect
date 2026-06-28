using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Domain event raised when a video is added to an event
/// </summary>
public record VideoAddedToEventDomainEvent(Guid EventId, Guid VideoId, string VideoUrl) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
