using LankaConnect.BuildingBlocks.Domain.Contracts;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

public record EventPublishedEvent(
    Guid EventId,
    DateTime PublishedAt,
    Guid PublishedBy
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}