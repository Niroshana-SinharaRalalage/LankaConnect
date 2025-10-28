using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Domain.Events;

public record UserCreatedFromExternalProviderEvent(
    Guid UserId,
    string Email,
    string FullName,
    IdentityProvider IdentityProvider,
    string ExternalProviderId) : DomainEvent;
