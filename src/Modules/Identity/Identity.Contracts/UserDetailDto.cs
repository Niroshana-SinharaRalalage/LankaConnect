namespace LankaConnect.Modules.Identity.Contracts;

/// <summary>
/// Cross-module read-only projection of a User aggregate WITH full detail
/// (preferences, location, profile photo, identity provider info, last login).
/// Returned by <see cref="IIdentityQueries.GetUserDetailAsync"/>.
/// </summary>
/// <remarks>
/// Wave 4.6.a (2026-06-24). Sensitive fields (password hash, refresh tokens,
/// password-reset token) intentionally NOT exposed -- those are Identity-internal
/// and stay on the Identity-internal <c>IUserRepository</c>.
/// </remarks>
public sealed record UserDetailDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string DisplayName,
    UserRoleDto Role,
    UserStatusDto Status,
    bool EmailVerified,
    IdentityProviderDto Provider,
    FederatedProviderDto? FederatedProvider,
    string? PhoneNumber,
    string? ProfilePhotoUrl,
    string? Bio,
    string? Country,
    string? City,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
