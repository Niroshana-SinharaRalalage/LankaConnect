using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class EventAnalytic
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public int TotalViews { get; set; }

    public int UniqueViewers { get; set; }

    public int RegistrationCount { get; set; }

    public DateTime? LastViewedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int ShareCount { get; set; }
}
