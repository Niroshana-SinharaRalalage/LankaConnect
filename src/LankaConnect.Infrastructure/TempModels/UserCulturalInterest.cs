using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class UserCulturalInterest
{
    public int Id { get; set; }

    public string InterestCode { get; set; } = null!;

    public Guid UserId { get; set; }

    public virtual User User { get; set; } = null!;
}
