using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class SignUpCommitment
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string ItemDescription { get; set; } = null!;

    public int Quantity { get; set; }

    public DateTime CommittedAt { get; set; }

    public Guid? SignUpListId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Notes { get; set; }

    public Guid? SignUpItemId { get; set; }

    public string? ContactEmail { get; set; }

    public string? ContactName { get; set; }

    public string? ContactPhone { get; set; }

    public virtual SignUpItem? SignUpItem { get; set; }

    public virtual SignUpList? SignUpList { get; set; }
}
