namespace LankaConnect.Modules.Identity.Contracts;

/// <summary>
/// Cross-module read-only projection of a user with a pending role-upgrade
/// request awaiting admin approval. Returned by
/// <see cref="IIdentityQueries.GetUsersWithPendingRoleUpgradesAsync"/>.
/// </summary>
/// <remarks>
/// Wave 4.6.a (2026-06-24). Carries enough context for the admin queue UI
/// without forcing a separate <see cref="UserDetailDto"/> fetch per row.
/// </remarks>
public sealed record UserPendingRoleUpgradeDto(
    Guid UserId,
    string Email,
    string DisplayName,
    UserRoleDto CurrentRole,
    UserRoleDto RequestedRole,
    string? UpgradeRequestReason,
    DateTime RequestedAt);
