using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Domain event raised when a user unlinks an external social provider from their account
/// </summary>
public record ExternalProviderUnlinkedEvent(
    Guid UserId,
    FederatedProvider Provider,
    string ExternalProviderId) : DomainEvent;
