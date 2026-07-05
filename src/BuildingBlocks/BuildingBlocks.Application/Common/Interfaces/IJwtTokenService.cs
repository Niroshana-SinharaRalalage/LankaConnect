using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Enterprise;
using LankaConnect.BuildingBlocks.Domain.Models;
using LankaConnect.BuildingBlocks.Domain.Monitoring;
using LankaConnect.BuildingBlocks.Domain.ValueObjects;
using LankaConnect.BuildingBlocks.Domain.Security;
using LankaConnect.BuildingBlocks.Domain.Recovery;
using LankaConnect.BuildingBlocks.Domain.Database;
using LankaConnect.BuildingBlocks.Domain.Enums;
using MultiLanguageModels = LankaConnect.BuildingBlocks.Domain.Database.MultiLanguageRoutingModels;
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
