using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class UserPreferredMetroArea
{
    public Guid UserId { get; set; }

    public Guid MetroAreaId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual MetroArea MetroArea { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
