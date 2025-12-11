using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Domain event raised when a user is removed from the waiting list
/// Used for confirmation emails and analytics
/// </summary>
public record UserRemovedFromWaitingListDomainEvent(
    Guid EventId,
    Guid UserId,
    DateTime OccurredAt) : IDomainEvent;
