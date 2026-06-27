using LankaConnect.Domain.Common;

namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Domain event raised when a video is added to an event
/// </summary>
public record VideoAddedToEventDomainEvent(Guid EventId, Guid VideoId, string VideoUrl) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
