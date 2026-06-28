using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Domain event raised when an event's pricing is updated to dual pricing (adult/child)
/// Session 21: Dual Ticket Pricing feature
/// </summary>
public record EventPricingUpdatedEvent(
    Guid EventId,
    TicketPricing Pricing,
    DateTime OccurredAt) : IDomainEvent;
