namespace LankaConnect.Modules.Identity.Contracts;

/// <summary>
/// Cross-module read-only projection of a User aggregate WITHOUT detail
/// (no preferences, no external logins, no role-upgrade workflow state).
/// Returned by the most common <see cref="IIdentityQueries"/> methods.
/// </summary>
/// <remarks>
/// Wave 4.6.a (2026-06-24). Field surface tracks the User aggregate's
/// public state as of 2026-06-24. <see cref="DisplayName"/> mirrors
/// <c>User.FullName</c> (concatenation of FirstName + LastName).
/// </remarks>
public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string DisplayName,
    UserRoleDto Role,
    UserStatusDto Status,
    bool EmailVerified,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
