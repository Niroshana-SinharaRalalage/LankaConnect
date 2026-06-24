using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.Modules.Identity.Contracts;

namespace LankaConnect.Modules.Identity.Application.Mappings;

/// <summary>
/// Domain User -> Identity.Contracts DTO projections. Wave 4.6.b (2026-06-24).
/// Mirrors the W4.4 PaymentContractMappings + W5.4 EmailGroupContractMappings
/// pattern. Lives in Identity.Application because the source type
/// (<see cref="User"/>) lives in <c>LankaConnect.Domain.Users</c> until Wave
/// 4.6.d.2 (physical move).
/// </summary>
internal static class UserContractMappings
{
    public static UserSummaryDto ToSummaryDto(this User src) =>
        new(
            Id: src.Id,
            Email: src.Email.Value,
            FirstName: src.FirstName,
            LastName: src.LastName,
            DisplayName: src.FullName,
            Role: (UserRoleDto)(byte)src.Role,
            Status: src.ResolveStatus(),
            EmailVerified: src.IsEmailVerified,
            CreatedAt: src.CreatedAt,
            UpdatedAt: src.UpdatedAt);

    public static UserContactDto ToContactDto(this User src) =>
        new(
            Id: src.Id,
            Email: src.Email.Value,
            FirstName: src.FirstName,
            LastName: src.LastName,
            DisplayName: src.FullName,
            ProfilePhotoUrl: src.ProfilePhotoUrl,
            IsActive: src.IsActive,
            IsEmailVerified: src.IsEmailVerified,
            IsAccountLocked: src.IsAccountLocked);

    public static UserDetailDto ToDetailDto(this User src) =>
        new(
            Id: src.Id,
            Email: src.Email.Value,
            FirstName: src.FirstName,
            LastName: src.LastName,
            DisplayName: src.FullName,
            Role: (UserRoleDto)(byte)src.Role,
            Status: src.ResolveStatus(),
            EmailVerified: src.IsEmailVerified,
            Provider: (IdentityProviderDto)(byte)src.IdentityProvider,
            FederatedProvider: src.ExternalLogins.Count > 0
                ? (FederatedProviderDto?)(byte)src.ExternalLogins.First().Provider
                : null,
            PhoneNumber: src.PhoneNumber?.Value,
            ProfilePhotoUrl: src.ProfilePhotoUrl,
            Bio: src.Bio,
            Country: src.Location?.Country,
            City: src.Location?.City,
            LastLoginAt: src.LastLoginAt,
            CreatedAt: src.CreatedAt,
            UpdatedAt: src.UpdatedAt);

    public static UserPendingRoleUpgradeDto ToPendingRoleUpgradeDto(this User src) =>
        new(
            UserId: src.Id,
            Email: src.Email.Value,
            DisplayName: src.FullName,
            CurrentRole: (UserRoleDto)(byte)src.Role,
            RequestedRole: (UserRoleDto)(byte)(src.PendingUpgradeRole ?? src.Role),
            UpgradeRequestReason: null, // PendingUpgradeReason field doesn't exist on User
            RequestedAt: src.UpgradeRequestedAt ?? src.UpdatedAt ?? src.CreatedAt);

    private static UserStatusDto ResolveStatus(this User src)
    {
        if (!src.IsActive) return UserStatusDto.Deactivated;
        if (src.IsAccountLocked) return UserStatusDto.Locked;
        return UserStatusDto.Active;
    }

    public static UserRole FromDto(this UserRoleDto dto) => (UserRole)(byte)dto;
}
