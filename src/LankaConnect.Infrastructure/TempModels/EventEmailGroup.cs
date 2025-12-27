using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class EventEmailGroup
{
    public Guid EventId { get; set; }

    public Guid EmailGroupId { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual EmailGroup EmailGroup { get; set; } = null!;

    public virtual Event Event { get; set; } = null!;
}
