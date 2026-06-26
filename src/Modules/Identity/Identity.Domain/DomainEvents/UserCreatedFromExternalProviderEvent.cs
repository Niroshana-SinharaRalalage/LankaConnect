using LankaConnect.Domain.Common;
using LankaConnect.Modules.Identity.Domain.Enums;

namespace LankaConnect.Modules.Identity.Domain.DomainEvents;

public record UserCreatedFromExternalProviderEvent(
    Guid UserId,
    string Email,
    string FullName,
    IdentityProvider IdentityProvider,
    string ExternalProviderId) : DomainEvent;
