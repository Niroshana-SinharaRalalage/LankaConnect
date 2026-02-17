using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Phase 6A.121: Updated with dual nullable fields to support quantity-based and slot-based commitments
/// Exactly ONE of PhysicalQuantity or SlotsClaimed will be populated based on item type
/// </summary>
public record UserCommittedToSignUpEvent(
    Guid SignUpListId,
    Guid UserId,
    string ItemDescription,
    int? PhysicalQuantity,  // For quantity-based items (e.g., "5 plates")
    int? SlotsClaimed,      // For slot-based items (e.g., "2 slots")
    DateTime OccurredAt) : IDomainEvent;
