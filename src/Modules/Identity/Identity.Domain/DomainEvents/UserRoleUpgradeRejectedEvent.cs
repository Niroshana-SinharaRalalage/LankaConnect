using LankaConnect.Domain.Common;
using LankaConnect.Modules.Identity.Domain.Enums;

namespace LankaConnect.Modules.Identity.Domain.DomainEvents;

/// <summary>
/// Domain event raised when a role upgrade request is rejected by admin
/// Phase 6A.0: Part of admin approval workflow
/// </summary>
public record UserRoleUpgradeRejectedEvent(
    Guid UserId,
    string Email,
    UserRole RejectedRole,
    string? Reason = null) : DomainEvent;
