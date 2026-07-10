using LankaConnect.BuildingBlocks.Domain.Contracts;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

public record RegistrationQuantityUpdatedEvent(
    Guid EventId,
    Guid AttendeeId,
    int PreviousQuantity,
    int NewQuantity,
    DateTime UpdatedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
