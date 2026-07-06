using LankaConnect.BuildingBlocks.Domain.Contracts;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Domain event raised when an event's location is set or updated
/// </summary>
public record EventLocationUpdatedEvent(
    Guid EventId,
    EventLocation Location,
    DateTime OccurredAt) : IDomainEvent;
