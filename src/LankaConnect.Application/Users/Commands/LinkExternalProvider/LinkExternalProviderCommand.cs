using MediatR;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Application.Users.Commands.LinkExternalProvider;

/// <summary>
/// Command to link an external OAuth provider (Facebook, Google, Apple) to a user's account
/// Epic 1 Phase 2: Multi-Provider Social Login
/// </summary>
public record LinkExternalProviderCommand(
    Guid UserId,
    FederatedProvider Provider,
    string ExternalProviderId,
    string ProviderEmail) : IRequest<Result<LinkExternalProviderResponse>>;
