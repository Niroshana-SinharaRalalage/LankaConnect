using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Phase 6A.89: Raised when an admin unlocks a user account.
/// Used to send notification email to the user and for audit logging.
/// </summary>
public record UserAccountUnlockedByAdminEvent(
    Guid UserId,
    string Email) : DomainEvent;
