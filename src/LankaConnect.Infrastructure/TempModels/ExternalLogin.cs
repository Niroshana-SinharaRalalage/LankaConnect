using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class ExternalLogin
{
    public Guid UserId { get; set; }

    public int Id { get; set; }

    public int Provider { get; set; }

    public string ExternalProviderId { get; set; } = null!;

    public string ProviderEmail { get; set; } = null!;

    public DateTime LinkedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
