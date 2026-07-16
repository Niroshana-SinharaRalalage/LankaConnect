using Microsoft.Extensions.Configuration;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces; // 4C.d.xiii: ITokenConfiguration lives in BB.Application

// Wave 8.5.b Part 5 (2026-07-16): relocated from LankaConnect.Infrastructure/Security/
// to Identity.Infrastructure/Security/ per DBCONTEXT_OWNERSHIP_MATRIX (Identity/Auth concern).
namespace LankaConnect.Modules.Identity.Infrastructure.Security;

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