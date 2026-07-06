using LankaConnect.BuildingBlocks.Domain.Contracts;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

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
