using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Domain event raised when a user requests a role upgrade (e.g., Event Organizer)
/// Phase 6A.0: Part of admin approval workflow
/// </summary>
public record UserRoleUpgradeRequestedEvent(
    Guid UserId,
    string Email,
    UserRole RequestedRole) : DomainEvent;
