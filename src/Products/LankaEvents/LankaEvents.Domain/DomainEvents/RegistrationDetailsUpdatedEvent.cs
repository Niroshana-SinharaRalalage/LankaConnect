using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Phase 6A.14: Domain event raised when registration details (attendees/contact) are updated
/// </summary>
public record RegistrationDetailsUpdatedEvent(
    Guid EventId,
    Guid RegistrationId,
    Guid? UserId,
    int AttendeeCount,
    DateTime UpdatedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
