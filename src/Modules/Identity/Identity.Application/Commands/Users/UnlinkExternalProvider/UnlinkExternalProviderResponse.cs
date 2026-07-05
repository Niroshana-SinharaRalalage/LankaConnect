using LankaConnect.Modules.Identity.Domain.Enums;
using System.Text.Json.Serialization;
namespace LankaConnect.Modules.Identity.Application.Commands.Users.UnlinkExternalProvider;

/// <summary>
/// Response DTO for UnlinkExternalProviderCommand
/// </summary>
public record UnlinkExternalProviderResponse(
    Guid UserId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    FederatedProvider Provider,
    DateTime UnlinkedAt);
