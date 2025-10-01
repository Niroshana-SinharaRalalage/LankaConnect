namespace LankaConnect.Application.Auth.Commands.RefreshToken;

public record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime TokenExpiresAt);