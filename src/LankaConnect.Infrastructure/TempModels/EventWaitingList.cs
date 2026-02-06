using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class EventWaitingList
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateTime JoinedAt { get; set; }

    public int Position { get; set; }

    public Guid EventId { get; set; }

    public virtual Event Event { get; set; } = null!;
}
