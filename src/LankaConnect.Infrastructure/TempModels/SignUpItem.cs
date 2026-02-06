using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class SignUpItem
{
    public Guid Id { get; set; }

    public Guid SignUpListId { get; set; }

    public string ItemDescription { get; set; } = null!;

    public int Quantity { get; set; }

    public int ItemCategory { get; set; }

    public int RemainingQuantity { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public virtual ICollection<SignUpCommitment> SignUpCommitments { get; set; } = new List<SignUpCommitment>();

    public virtual SignUpList SignUpList { get; set; } = null!;
}
