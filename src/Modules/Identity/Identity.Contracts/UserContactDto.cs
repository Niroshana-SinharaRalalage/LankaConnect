namespace LankaConnect.Modules.Identity.Contracts;

/// <summary>
/// Cross-module read-only projection of a User aggregate at the
/// "contact / orchestration" tier - thicker than <see cref="UserSummaryDto"/>
/// (carries the email-orchestration flags consumers need) but thinner than
/// <see cref="UserDetailDto"/> (no admin-page detail like preferences,
/// federated provider, lockout timestamps). Returned by
/// <see cref="IIdentityQueries.GetContactInfoAsync"/>.
/// </summary>
/// <remarks>
/// Wave 4.6.d.2.a (2026-06-24). Architect-mandated DTO per the d.2.a ruling
/// to replace the 18+ read sites in legacy LankaConnect.Application that
/// fetch User just for email-orchestration purposes. Conflating these fields
/// onto <see cref="UserDetailDto"/> would leak admin-shape semantics into
/// orchestration code paths.
/// </remarks>
public sealed record UserContactDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string DisplayName,
    string? ProfilePhotoUrl,
    bool IsActive,
    bool IsEmailVerified,
    bool IsAccountLocked);
