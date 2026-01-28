using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Phase 6A.89: Raised when an admin locks a user account.
/// Different from UserAccountLockedEvent which is raised after failed login attempts.
/// Used to send notification email to the user and for audit logging.
/// </summary>
public record UserAccountLockedByAdminEvent(
    Guid UserId,
    string Email,
    DateTime LockUntil,
    string? Reason) : DomainEvent;
