using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class EventBadge
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public Guid BadgeId { get; set; }

    public DateTime AssignedAt { get; set; }

    public Guid AssignedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? DurationDays { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public virtual Badge Badge { get; set; } = null!;

    public virtual Event Event { get; set; } = null!;
}
