using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.BuildingBlocks.Domain.ValueObjects;
using LankaConnect.BuildingBlocks.Domain.Enums;
using LankaConnect.Modules.Identity.Domain.Entities;
namespace LankaConnect.BuildingBlocks.Application.Common.Interfaces;

public interface IJwtTokenService
{
    Task<Result<string>> GenerateAccessTokenAsync(User user);
    Task<Result<string>> GenerateRefreshTokenAsync();
    Task<Result<Guid>> ValidateTokenAsync(string token);
    Task<bool> IsTokenValidAsync(string token);
    Task<Result> InvalidateRefreshTokenAsync(string refreshToken);
    Task<Result> InvalidateAllUserTokensAsync(Guid userId);
}
