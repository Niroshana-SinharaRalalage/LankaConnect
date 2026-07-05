using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Domain event raised when event images are reordered
/// </summary>
public record ImagesReorderedDomainEvent(Guid EventId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
