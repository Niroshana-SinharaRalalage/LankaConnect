using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class Reply
{
    public Guid Id { get; set; }

    public Guid TopicId { get; set; }

    public string Content { get; set; } = null!;

    public Guid AuthorId { get; set; }

    public Guid? ParentReplyId { get; set; }

    public int HelpfulVotes { get; set; }

    public bool IsMarkedAsSolution { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Reply> InverseParentReply { get; set; } = new List<Reply>();

    public virtual Reply? ParentReply { get; set; }

    public virtual Topic Topic { get; set; } = null!;
}
