using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Identity.Domain.Enums;

namespace LankaConnect.Modules.Identity.Domain.DomainEvents;

/// <summary>
/// Domain event raised when a user unlinks an external social provider from their account
/// </summary>
public record ExternalProviderUnlinkedEvent(
    Guid UserId,
    FederatedProvider Provider,
    string ExternalProviderId) : DomainEvent;
