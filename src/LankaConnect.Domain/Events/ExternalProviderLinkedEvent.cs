using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Domain event raised when a user links an external social provider to their account
/// </summary>
public record ExternalProviderLinkedEvent(
    Guid UserId,
    FederatedProvider Provider,
    string ExternalProviderId,
    string ProviderEmail) : DomainEvent;
