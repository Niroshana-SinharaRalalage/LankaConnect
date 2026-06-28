using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Domain event raised when a spot becomes available and the next person on the waiting list should be notified
/// Handled by email notification handler to send "Spot Available" email
/// </summary>
public record WaitingListSpotAvailableDomainEvent(
    Guid EventId,
    Guid UserId,
    DateTime OccurredAt) : IDomainEvent;
