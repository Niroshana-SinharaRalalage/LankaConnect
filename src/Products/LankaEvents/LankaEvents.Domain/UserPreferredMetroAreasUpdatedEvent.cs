using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Domain event raised when user's preferred metro areas are updated
/// Phase 5A: User Preferred Metro Areas
/// Architecture: Following ADR-008 - only raised when setting preferences (not when clearing for privacy)
/// </summary>
public sealed record UserPreferredMetroAreasUpdatedEvent(
    Guid UserId,
    IReadOnlyList<Guid> MetroAreaIds
) : DomainEvent;
