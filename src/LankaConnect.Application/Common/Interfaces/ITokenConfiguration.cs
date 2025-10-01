namespace LankaConnect.Application.Common.Interfaces;

public interface ITokenConfiguration
{
    int AccessTokenExpirationMinutes { get; }
    int RefreshTokenExpirationDays { get; }
}