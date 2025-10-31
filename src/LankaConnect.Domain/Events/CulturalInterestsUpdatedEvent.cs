using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.ValueObjects;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Domain event raised when user's cultural interests are updated
/// Architecture: Following architect guidance - only raised when setting interests, not when clearing
/// </summary>
public sealed record CulturalInterestsUpdatedEvent(
    Guid UserId,
    IReadOnlyCollection<CulturalInterest> Interests
) : DomainEvent;
