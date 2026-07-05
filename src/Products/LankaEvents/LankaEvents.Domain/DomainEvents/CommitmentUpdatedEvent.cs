using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Domain event raised when a user updates their commitment to a sign-up item.
/// Phase 6A.51+: Enables sending update confirmation emails to users
/// when they change their commitment quantity or details.
/// Phase 6A.121: Updated with dual nullable fields to support quantity-based and slot-based commitments
/// </summary>
public record CommitmentUpdatedEvent(
    Guid SignUpItemId,
    Guid UserId,
    int? OldPhysicalQuantity,   // For quantity-based items - old value
    int? NewPhysicalQuantity,   // For quantity-based items - new value
    int? OldSlotsClaimed,       // For slot-based items - old value
    int? NewSlotsClaimed,       // For slot-based items - new value
    string ItemDescription,
    DateTime OccurredAt) : IDomainEvent;
