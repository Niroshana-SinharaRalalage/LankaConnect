namespace LankaConnect.Modules.Identity.Application.Commands.Auth.RefreshToken;

public record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime TokenExpiresAt);
