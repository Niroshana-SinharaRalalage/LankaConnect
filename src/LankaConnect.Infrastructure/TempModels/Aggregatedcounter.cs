using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class Aggregatedcounter
{
    public long Id { get; set; }

    public string Key { get; set; } = null!;

    public long Value { get; set; }

    public DateTime? Expireat { get; set; }
}
