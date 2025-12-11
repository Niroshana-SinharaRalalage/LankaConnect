using LankaConnect.Domain.Users.Enums;
using System.Text.Json.Serialization;

namespace LankaConnect.Application.Users.Commands.UnlinkExternalProvider;

/// <summary>
/// Response DTO for UnlinkExternalProviderCommand
/// </summary>
public record UnlinkExternalProviderResponse(
    Guid UserId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    FederatedProvider Provider,
    DateTime UnlinkedAt);
