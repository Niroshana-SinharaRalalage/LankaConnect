using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Application.Auth.Commands.LoginWithEntra;

/// <summary>
/// Response containing authentication tokens and user information after successful Entra login
/// </summary>
public record LoginWithEntraResponse(
    Guid UserId,
    string Email,
    string FullName,
    UserRole Role,
    string AccessToken,
    string RefreshToken,
    DateTime TokenExpiresAt,
    bool IsNewUser);
