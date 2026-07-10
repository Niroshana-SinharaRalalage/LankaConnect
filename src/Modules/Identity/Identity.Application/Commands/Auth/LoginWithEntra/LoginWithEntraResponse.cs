using LankaConnect.Modules.Identity.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
namespace LankaConnect.Modules.Identity.Application.Commands.Auth.LoginWithEntra;

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
