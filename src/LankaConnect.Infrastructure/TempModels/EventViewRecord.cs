using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class EventViewRecord
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public Guid? UserId { get; set; }

    public string IpAddress { get; set; } = null!;

    public string? UserAgent { get; set; }

    public DateTime ViewedAt { get; set; }
}
