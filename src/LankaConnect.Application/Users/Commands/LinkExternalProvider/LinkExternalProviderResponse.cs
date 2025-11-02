using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Application.Users.Commands.LinkExternalProvider;

/// <summary>
/// Response DTO for LinkExternalProviderCommand
/// </summary>
public record LinkExternalProviderResponse(
    Guid UserId,
    FederatedProvider Provider,
    string ProviderEmail,
    DateTime LinkedAt);
