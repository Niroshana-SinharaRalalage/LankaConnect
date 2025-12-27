using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class SignUpList
{
    public Guid Id { get; set; }

    public string Category { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string SignUpType { get; set; } = null!;

    public Guid EventId { get; set; }

    public string PredefinedItems { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool HasMandatoryItems { get; set; }

    public bool HasPreferredItems { get; set; }

    public bool HasSuggestedItems { get; set; }

    public bool HasOpenItems { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual ICollection<SignUpCommitment> SignUpCommitments { get; set; } = new List<SignUpCommitment>();

    public virtual ICollection<SignUpItem> SignUpItems { get; set; } = new List<SignUpItem>();
}
