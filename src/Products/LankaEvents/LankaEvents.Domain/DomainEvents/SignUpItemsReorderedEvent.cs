using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Phase 6A.132: Raised when an organizer reorders the items within a sign-up list.
/// <paramref name="OrderedItemIds"/> is the full new ordering (first element = DisplayOrder 0).
/// Consumed by projections and audit logs; no side-effect handlers as of Phase 6A.132.
/// </summary>
public record SignUpItemsReorderedEvent(
    Guid SignUpListId,
    IReadOnlyList<Guid> OrderedItemIds,
    DateTime OccurredAt) : IDomainEvent;
