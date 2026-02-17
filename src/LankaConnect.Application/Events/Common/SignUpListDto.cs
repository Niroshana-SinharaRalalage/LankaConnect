using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Common;

public class SignUpListDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SignUpType SignUpType { get; set; }

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
/// </summary>
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
    /// Phase 6A.124: [JsonIgnore] prevents JsonStringEnumConverter from serializing
    /// as "Quantity"/"Slot" string when STJ uses the interface type for List&lt;ISignUpItemDto&gt;.
    /// Interface still exposes the typed enum for C# code; JSON sees only ItemTypeValue below.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    SignUpItemType ItemType { get; }

    /// <summary>
    /// Serialized as "itemType": 0 or 1 (integer).
    /// Frontend isQuantityBased() checks item.itemType === 0 (numeric enum, NOT string).
    /// MUST be on the interface so STJ includes it when property is typed as ISignUpItemDto.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("itemType")]
    int ItemTypeValue { get; }
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

    // Phase 6A.124: [JsonIgnore] prevents JsonStringEnumConverter from serializing
    // as "Quantity" string. Interface contract still satisfied (returns SignUpItemType).
    [System.Text.Json.Serialization.JsonIgnore]
    public SignUpItemType ItemType { get; set; } = SignUpItemType.Quantity;

    // int bypasses JsonStringEnumConverter → sends "itemType": 0 (integer)
    // Frontend: isQuantityBased(item) checks item.itemType === 0 (SignUpItemType.Quantity)
    [System.Text.Json.Serialization.JsonPropertyName("itemType")]
    public int ItemTypeValue => (int)ItemType;

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

    // Phase 6A.124: [JsonIgnore] prevents JsonStringEnumConverter from serializing
    // as "Slot" string. Interface contract still satisfied (returns SignUpItemType).
    [System.Text.Json.Serialization.JsonIgnore]
    public SignUpItemType ItemType { get; set; } = SignUpItemType.Slot;

    // int bypasses JsonStringEnumConverter → sends "itemType": 1 (integer)
    // Frontend: isSlotBased(item) checks item.itemType === 1 (SignUpItemType.Slot)
    [System.Text.Json.Serialization.JsonPropertyName("itemType")]
    public int ItemTypeValue => (int)ItemType;

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
    // Phase 6A.124: [JsonIgnore] prevents JsonStringEnumConverter from serializing as "Quantity" string.
    [System.Text.Json.Serialization.JsonIgnore]
    public SignUpItemType ItemType { get; set; } = SignUpItemType.Quantity;

    // int bypasses JsonStringEnumConverter → sends "itemType": 0 (integer)
    [System.Text.Json.Serialization.JsonPropertyName("itemType")]
    public int ItemTypeValue => (int)ItemType;
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
