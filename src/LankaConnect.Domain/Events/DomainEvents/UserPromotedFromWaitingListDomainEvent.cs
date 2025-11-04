using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Domain event raised when a user is promoted from waiting list to confirmed registration
/// Used for confirmation emails and analytics
/// </summary>
public record UserPromotedFromWaitingListDomainEvent(
    Guid EventId,
    Guid UserId,
    DateTime OccurredAt) : IDomainEvent;
