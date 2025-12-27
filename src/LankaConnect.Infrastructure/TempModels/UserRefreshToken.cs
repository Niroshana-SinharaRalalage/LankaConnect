using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class UserRefreshToken
{
    public Guid UserId { get; set; }

    public int Id { get; set; }

    public string Token { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? RevokedByIp { get; set; }

    public string CreatedByIp { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
