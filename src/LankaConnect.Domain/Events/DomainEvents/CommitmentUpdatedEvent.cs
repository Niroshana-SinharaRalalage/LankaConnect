using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Domain event raised when a user updates their commitment to a sign-up item.
/// Phase 6A.51+: Enables sending update confirmation emails to users
/// when they change their commitment quantity or details.
/// </summary>
public record CommitmentUpdatedEvent(
    Guid SignUpItemId,
    Guid UserId,
    int OldQuantity,
    int NewQuantity,
    string ItemDescription,
    DateTime OccurredAt) : IDomainEvent;
