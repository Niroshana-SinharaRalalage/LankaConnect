using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Domain event raised when a user's location is updated
/// </summary>
public record UserLocationUpdatedEvent(
    Guid UserId,
    string Email,
    string City,
    string State,
    string Country) : DomainEvent;
