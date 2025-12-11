using MediatR;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Application.Users.Commands.UnlinkExternalProvider;

/// <summary>
/// Command to unlink an external OAuth provider from a user's account
/// Epic 1 Phase 2: Multi-Provider Social Login
/// Business Rule: Cannot unlink last authentication method (must have password or another provider)
/// </summary>
public record UnlinkExternalProviderCommand(
    Guid UserId,
    FederatedProvider Provider) : IRequest<Result<UnlinkExternalProviderResponse>>;
