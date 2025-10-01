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
    DateTime TokenExpiresAt);