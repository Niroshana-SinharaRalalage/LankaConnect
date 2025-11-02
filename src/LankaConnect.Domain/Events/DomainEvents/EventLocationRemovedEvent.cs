using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Domain event raised when an event's location is removed (e.g., converting to virtual event)
/// </summary>
public record EventLocationRemovedEvent(
    Guid EventId,
    DateTime OccurredAt) : IDomainEvent;
