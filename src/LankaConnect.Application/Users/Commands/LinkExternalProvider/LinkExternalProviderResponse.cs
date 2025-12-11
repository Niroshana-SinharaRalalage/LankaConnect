using LankaConnect.Domain.Users.Enums;
using System.Text.Json.Serialization;

namespace LankaConnect.Application.Users.Commands.LinkExternalProvider;

/// <summary>
/// Response DTO for LinkExternalProviderCommand
/// </summary>
public record LinkExternalProviderResponse(
    Guid UserId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    FederatedProvider Provider,
    string ProviderEmail,
    DateTime LinkedAt);
