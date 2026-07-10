using LankaConnect.BuildingBlocks.Domain.Contracts;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

public record RegistrationCancelledEvent(
    Guid EventId,
    Guid AttendeeId,
    DateTime CancelledAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}