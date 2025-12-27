using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class Topic
{
    public Guid Id { get; set; }

    public string Content { get; set; } = null!;

    public Guid AuthorId { get; set; }

    public Guid ForumId { get; set; }

    public string Category { get; set; } = null!;

    public string Status { get; set; } = null!;

    public bool IsPinned { get; set; }

    public int ViewCount { get; set; }

    public string? LockReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string Title { get; set; } = null!;

    public virtual ICollection<Reply> Replies { get; set; } = new List<Reply>();
}
