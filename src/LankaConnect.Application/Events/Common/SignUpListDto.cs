using System.Text.Json.Serialization;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Common;

public class SignUpListDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SignUpType SignUpType { get; set; }

    /// <summary>
    /// Phase 7D.1: Discriminator exposing whether this list is an Items collection
    /// (Kind=Items) or a Volunteer roster (Kind=Volunteers). Volunteer lists are
    /// always slot-based and never accept open items.
    /// </summary>
    public SignUpKind Kind { get; set; } = SignUpKind.Items;

    // Legacy fields (for Open/Predefined sign-ups)
    public List<string> PredefinedItems { get; set; } = new();
    public List<SignUpCommitmentDto> Commitments { get; set; } = new();
    public int CommitmentCount { get; set; }

    // New category-based fields
    public bool HasMandatoryItems { get; set; }
    public bool HasPreferredItems { get; set; }
    public bool HasSuggestedItems { get; set; }

    // Phase 6A.27: Open items flag - allows users to add their own items
    public bool HasOpenItems { get; set; }

    /// <summary>
    /// Phase 6A.121: Items collection now supports discriminated union types
    /// Each item will be either QuantityBasedItemDto or SlotBasedItemDto
    /// </summary>
    public List<ISignUpItemDto> Items { get; set; } = new();
}

/// <summary>
/// Phase 6A.121: Base interface for discriminated union of SignUpItem DTOs
/// Enables type-safe handling of quantity-based vs slot-based items.
/// Phase 6A.124: ItemType added to interface so System.Text.Json serializes
/// the discriminator field even when the property is declared as ISignUpItemDto.
/// Phase 6A.125: Added [JsonPolymorphic] so System.Text.Json serializes the RUNTIME
/// type (including type-specific fields like TargetQuantity/TotalSlots) rather than
/// only the interface-declared properties. Without this, quantity/slot fields were
/// silently dropped from the JSON response.
/// Note: "$type" discriminator is added to JSON but frontend ignores it (uses "itemType").
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(QuantityBasedItemDto), "quantity")]
[JsonDerivedType(typeof(SlotBasedItemDto), "slot")]
#pragma warning disable CS0618 // SignUpItemDto is obsolete but still used for legacy support
[JsonDerivedType(typeof(SignUpItemDto), "legacy")]
#pragma warning restore CS0618
public interface ISignUpItemDto
{
    Guid Id { get; }
    string ItemDescription { get; }
    SignUpItemCategory ItemCategory { get; }
    string? Notes { get; }
    List<SignUpCommitmentDto> Commitments { get; }
    Guid? CreatedByUserId { get; }
    bool IsFullyCommitted { get; }
    bool IsOpenItem { get; }

    /// <summary>
    /// Discriminator field: Quantity = 0, Slot = 1.
    /// MUST be on the interface so the JSON serializer includes it when
    /// the property is typed as ISignUpItemDto (System.Text.Json only
    /// serializes properties declared on the static/interface type).
    /// </summary>
    SignUpItemType ItemType { get; }
}

/// <summary>
/// Phase 6A.121: DTO for quantity-based signup items
/// Example: "Rice - 10 plates" or "Paper Plates - 50 pieces"
/// </summary>
public class QuantityBasedItemDto : ISignUpItemDto
{
    public Guid Id { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public SignUpItemCategory ItemCategory { get; set; }
    public string? Notes { get; set; }
    public List<SignUpCommitmentDto> Commitments { get; set; } = new();
    public Guid? CreatedByUserId { get; set; }

    // Phase 6A.123: Discriminator field so frontend isQuantityBased() type guard works
    public SignUpItemType ItemType { get; set; } = SignUpItemType.Quantity;

    // Quantity-based specific fields
    public int TargetQuantity { get; set; }
    public int CommittedQuantity { get; set; }
    public int RemainingQuantity { get; set; }

    // Computed properties
    public bool IsFullyCommitted => RemainingQuantity == 0;
    public bool IsOpenItem => ItemCategory == SignUpItemCategory.Open && CreatedByUserId.HasValue;
}

/// <summary>
/// Phase 6A.121: DTO for slot-based signup items
/// Example: "Assorted Fruits - 3 slots" or "Homemade Desserts - 5 slots"
/// </summary>
public class SlotBasedItemDto : ISignUpItemDto
{
    public Guid Id { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public SignUpItemCategory ItemCategory { get; set; }
    public string? Notes { get; set; }
    public List<SignUpCommitmentDto> Commitments { get; set; } = new();
    public Guid? CreatedByUserId { get; set; }

    // Phase 6A.123: Discriminator field so frontend isSlotBased() type guard works
    public SignUpItemType ItemType { get; set; } = SignUpItemType.Slot;

    // Slot-based specific fields
    public int TotalSlots { get; set; }
    public int FilledSlots { get; set; }
    public int RemainingSlots { get; set; }
    public int? SuggestedQuantityPerSlot { get; set; }
    public int? EstimatedTotalQuantity { get; set; }

    // Computed properties
    public bool IsFullyCommitted => RemainingSlots == 0;
    public bool IsOpenItem => ItemCategory == SignUpItemCategory.Open && CreatedByUserId.HasValue;
}

/// <summary>
/// Deprecated: Use QuantityBasedItemDto or SlotBasedItemDto instead
/// Kept for backward compatibility during migration
/// </summary>
[Obsolete("Use QuantityBasedItemDto or SlotBasedItemDto instead. This will be removed in Phase 7.")]
public class SignUpItemDto : ISignUpItemDto
{
    public Guid Id { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int RemainingQuantity { get; set; }
    public SignUpItemCategory ItemCategory { get; set; }
    public string? Notes { get; set; }
    public List<SignUpCommitmentDto> Commitments { get; set; } = new();
    public bool IsFullyCommitted => RemainingQuantity == 0;
    public int CommittedQuantity => Quantity - RemainingQuantity;
    public Guid? CreatedByUserId { get; set; }
    public bool IsOpenItem => ItemCategory == SignUpItemCategory.Open && CreatedByUserId.HasValue;
    // ISignUpItemDto.ItemType - legacy items are always quantity-based
    public SignUpItemType ItemType { get; set; } = SignUpItemType.Quantity;
}

public class SignUpCommitmentDto
{
    public Guid Id { get; set; }
    public Guid? SignUpItemId { get; set; } // Null for legacy Open sign-ups
    public Guid UserId { get; set; }
    public string ItemDescription { get; set; } = string.Empty;

    /// <summary>
    /// Phase 6A.121: Dual nullable fields for quantity-based vs slot-based commitments
    /// Exactly ONE of PhysicalQuantity or SlotsClaimed will be populated based on item type
    /// </summary>
    public int? PhysicalQuantity { get; set; }  // For quantity-based items (e.g., "5 plates")
    public int? SlotsClaimed { get; set; }      // For slot-based items (e.g., "2 slots")

    /// <summary>
    /// Deprecated: Use PhysicalQuantity or SlotsClaimed instead
    /// Kept for backward compatibility - returns whichever field is populated
    /// </summary>
    [Obsolete("Use PhysicalQuantity or SlotsClaimed instead. This will be removed in Phase 7.")]
    public int Quantity => PhysicalQuantity ?? SlotsClaimed ?? 0;

    public DateTime CommittedAt { get; set; }
    public string? Notes { get; set; }

    // Phase 2: Contact information for SignUpGenius-style feature
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}
