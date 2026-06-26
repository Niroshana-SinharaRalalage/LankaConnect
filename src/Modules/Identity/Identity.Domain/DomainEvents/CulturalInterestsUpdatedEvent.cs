using LankaConnect.Domain.Common;
using LankaConnect.Modules.Identity.Domain.ValueObjects;

namespace LankaConnect.Modules.Identity.Domain.DomainEvents;

/// <summary>
/// Domain event raised when user's cultural interests are updated
/// Architecture: Following architect guidance - only raised when setting interests, not when clearing
/// </summary>
public sealed record CulturalInterestsUpdatedEvent(
    Guid UserId,
    IReadOnlyCollection<CulturalInterest> Interests
) : DomainEvent;
