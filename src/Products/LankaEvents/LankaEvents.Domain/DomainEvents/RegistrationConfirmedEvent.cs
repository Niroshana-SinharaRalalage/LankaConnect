using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

public record RegistrationConfirmedEvent(
    Guid EventId,
    Guid AttendeeId,
    int Quantity,
    DateTime RegistrationDate
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}