using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class MetroArea
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string State { get; set; } = null!;

    public double CenterLatitude { get; set; }

    public double CenterLongitude { get; set; }

    public int RadiusMiles { get; set; }

    public bool IsStateLevelArea { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<UserPreferredMetroArea> UserPreferredMetroAreas { get; set; } = new List<UserPreferredMetroArea>();
}
