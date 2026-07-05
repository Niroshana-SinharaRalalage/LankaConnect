using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

public record RegistrationConfirmedEvent(
    Guid EventId,
    Guid AttendeeId,
    int Quantity,
    DateTime RegistrationDate
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}