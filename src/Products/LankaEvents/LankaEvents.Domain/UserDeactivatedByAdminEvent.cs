using LankaConnect.BuildingBlocks.Domain.Contracts;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain;

/// <summary>
/// Phase 6A.89: Raised when an admin deactivates a user account.
/// Used to send notification email to the user and for audit logging.
/// </summary>
public record UserDeactivatedByAdminEvent(
    Guid UserId,
    string Email,
    string FullName) : DomainEvent;
