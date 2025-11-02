using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Application.Users.Commands.UnlinkExternalProvider;

/// <summary>
/// Response DTO for UnlinkExternalProviderCommand
/// </summary>
public record UnlinkExternalProviderResponse(
    Guid UserId,
    FederatedProvider Provider,
    DateTime UnlinkedAt);
