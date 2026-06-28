using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Domain event raised when an image is replaced in an event
/// Contains the old blob name for cleanup purposes
/// </summary>
public record ImageReplacedInEventDomainEvent(
    Guid EventId,
    Guid ImageId,
    string OldBlobName,
    string NewImageUrl) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
