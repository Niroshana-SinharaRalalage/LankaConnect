using LankaConnect.Domain.Users.Enums;
using System.Text.Json.Serialization;

namespace LankaConnect.Application.Users.Queries.GetLinkedProviders;

/// <summary>
/// Response DTO for GetLinkedProvidersQuery
/// </summary>
public record GetLinkedProvidersResponse(
    Guid UserId,
    IReadOnlyList<LinkedProviderDto> LinkedProviders);

/// <summary>
/// DTO representing a linked external provider
/// </summary>
public record LinkedProviderDto(
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    FederatedProvider Provider,
    string ProviderDisplayName,
    string ExternalProviderId,
    string ProviderEmail,
    DateTime LinkedAt);
