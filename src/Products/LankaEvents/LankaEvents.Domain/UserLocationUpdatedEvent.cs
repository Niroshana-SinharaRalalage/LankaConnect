using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain;

/// <summary>
/// Domain event raised when a user's location is updated
/// </summary>
public record UserLocationUpdatedEvent(
    Guid UserId,
    string Email,
    string City,
    string State,
    string Country) : DomainEvent;
