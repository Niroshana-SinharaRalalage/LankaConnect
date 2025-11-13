using LankaConnect.Domain.Users.Enums;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Auth.Commands.LoginUser;

public record LoginUserResponse(
    Guid UserId,
    string Email,
    string FullName,
    UserRole Role,
    string AccessToken,
    string RefreshToken,
    DateTime TokenExpiresAt,
    UserRole? PendingUpgradeRole = null,  // Phase 6A.7: Pending role upgrade (if user requested upgrade)
    DateTime? UpgradeRequestedAt = null); // Phase 6A.7: When upgrade was requested