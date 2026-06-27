using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Domain event raised when an image is added to an event
/// </summary>
public record ImageAddedToEventDomainEvent(Guid EventId, Guid ImageId, string ImageUrl) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
