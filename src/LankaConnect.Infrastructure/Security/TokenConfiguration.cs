using Microsoft.Extensions.Configuration;
using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Infrastructure.Security;

public class TokenConfiguration : ITokenConfiguration
{
    private readonly IConfiguration _configuration;

    public TokenConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public int AccessTokenExpirationMinutes => _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 15);

    public int RefreshTokenExpirationDays => _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7);
}