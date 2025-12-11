using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Domain event raised when a user is added to the waiting list
/// Can be used for analytics and confirmation emails
/// </summary>
public record UserAddedToWaitingListDomainEvent(
    Guid EventId,
    Guid UserId,
    int Position,
    DateTime OccurredAt) : IDomainEvent;
