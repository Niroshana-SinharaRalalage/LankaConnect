using LankaConnect.Domain.Common;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Domain event raised when an image is added to an event
/// </summary>
public record ImageAddedToEventDomainEvent(Guid EventId, Guid ImageId, string ImageUrl) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
