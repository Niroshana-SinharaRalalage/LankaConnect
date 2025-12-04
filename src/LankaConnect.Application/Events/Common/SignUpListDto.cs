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
    public List<SignUpItemDto> Items { get; set; } = new();
}

public class SignUpItemDto
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
}

public class SignUpCommitmentDto
{
    public Guid Id { get; set; }
    public Guid? SignUpItemId { get; set; } // Null for legacy Open sign-ups
    public Guid UserId { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime CommittedAt { get; set; }
    public string? Notes { get; set; }

    // Phase 2: Contact information for SignUpGenius-style feature
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}
