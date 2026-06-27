using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

public record RegistrationCancelledEvent(
    Guid EventId,
    Guid AttendeeId,
    DateTime CancelledAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}