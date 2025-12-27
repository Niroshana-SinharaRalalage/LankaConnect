using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class Review
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public Guid ReviewerId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? ApprovedAt { get; set; }

    public string? ModerationNotes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Cons { get; set; }

    public string Content { get; set; } = null!;

    public string? Pros { get; set; }

    public int Rating { get; set; }

    public string Title { get; set; } = null!;

    public virtual Business Business { get; set; } = null!;
}
