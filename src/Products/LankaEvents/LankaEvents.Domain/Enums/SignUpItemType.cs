namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Type of sign-up item based on how quantities are tracked
/// Phase 6A.121: Slot-based signup items support
/// </summary>
public enum SignUpItemType
{
    /// <summary>
    /// Quantity-based items where organizer defines exact number of units needed
    /// Example: "Rice - 10 plates" or "Paper Plates - 50 pieces"
    /// Uses TargetQuantity field, tracks PhysicalQuantity in commitments
    /// </summary>
    Quantity = 0,

    /// <summary>
    /// Slot-based items where organizer defines number of slots/spots available
    /// Example: "Assorted Fruits - 3 slots" or "Homemade Desserts - 5 slots"
    /// Uses AvailableSlots field, tracks SlotsClaimed in commitments
    /// People can take 1 or more slots and specify what they're bringing in notes
    /// </summary>
    Slot = 1
}
