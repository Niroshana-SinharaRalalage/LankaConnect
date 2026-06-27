using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Domain event raised when an event's location is set or updated
/// </summary>
public record EventLocationUpdatedEvent(
    Guid EventId,
    EventLocation Location,
    DateTime OccurredAt) : IDomainEvent;
