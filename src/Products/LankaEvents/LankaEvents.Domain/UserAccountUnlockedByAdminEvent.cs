using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain;

/// <summary>
/// Phase 6A.89: Raised when an admin unlocks a user account.
/// Used to send notification email to the user and for audit logging.
/// </summary>
public record UserAccountUnlockedByAdminEvent(
    Guid UserId,
    string Email) : DomainEvent;
