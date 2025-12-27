using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class UserEmailPreference
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public bool AllowMarketing { get; set; }

    public bool AllowNotifications { get; set; }

    public bool AllowNewsletters { get; set; }

    public bool AllowTransactional { get; set; }

    public string? PreferredLanguage { get; set; }

    public string? Timezone { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
