using LankaConnect.Modules.Identity.Domain.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
namespace LankaConnect.Modules.Identity.Application.Commands.Auth.LoginUser;

public record LoginUserResponse(
    Guid UserId,
    string Email,
    string FullName,
    string? PhoneNumber,                  // Phase 6A.X: Phone number for organizer contact auto-population
    UserRole Role,
    string AccessToken,
    string RefreshToken,
    DateTime TokenExpiresAt,
    bool IsEmailVerified,                 // FIX: Email verification status for UI display
    UserRole? PendingUpgradeRole = null,  // Phase 6A.7: Pending role upgrade (if user requested upgrade)
    DateTime? UpgradeRequestedAt = null,  // Phase 6A.7: When upgrade was requested
    string? ProfilePhotoUrl = null);      // Profile photo URL for header display