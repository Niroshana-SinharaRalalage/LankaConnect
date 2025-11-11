using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Domain event raised when a role upgrade request is rejected by admin
/// Phase 6A.0: Part of admin approval workflow
/// </summary>
public record UserRoleUpgradeRejectedEvent(
    Guid UserId,
    string Email,
    UserRole RejectedRole,
    string? Reason = null) : DomainEvent;
