using LankaConnect.Domain.Common;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Common.Models;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Security;
using LankaConnect.Domain.Common.Recovery;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Domain.Common.Enums;
using MultiLanguageModels = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;

namespace LankaConnect.Application.Common.Interfaces;

public interface IJwtTokenService
{
    Task<Result<string>> GenerateAccessTokenAsync(User user);
    Task<Result<string>> GenerateRefreshTokenAsync();
    Task<Result<Guid>> ValidateTokenAsync(string token);
    Task<bool> IsTokenValidAsync(string token);
    Task<Result> InvalidateRefreshTokenAsync(string refreshToken);
    Task<Result> InvalidateAllUserTokensAsync(Guid userId);
}
