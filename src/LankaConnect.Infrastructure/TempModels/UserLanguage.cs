using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class UserLanguage
{
    public int Id { get; set; }

    public string LanguageCode { get; set; } = null!;

    public int ProficiencyLevel { get; set; }

    public Guid UserId { get; set; }

    public virtual User User { get; set; } = null!;
}
