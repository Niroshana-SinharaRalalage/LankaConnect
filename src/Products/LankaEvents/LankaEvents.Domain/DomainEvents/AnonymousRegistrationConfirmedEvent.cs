using LankaConnect.Domain.Common;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

public record AnonymousRegistrationConfirmedEvent(
    Guid EventId,
    string AttendeeEmail,
    int Quantity,
    DateTime RegistrationDate
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
