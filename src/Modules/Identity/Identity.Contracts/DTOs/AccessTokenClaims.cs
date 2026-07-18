namespace LankaConnect.Modules.Identity.Contracts.DTOs;

/// <summary>
/// Contracts-layer projection of the fields required to mint an access token
/// (JWT) for an authenticated user. Deliberately Contracts-pure: primitives +
/// <see cref="UserRoleDto"/> only — NO <c>Identity.Domain.User</c> aggregate
/// leak. Consumed by <see cref="Services.IJwtTokenService"/>.
/// </summary>
/// <remarks>
/// Wave 8.5.a Part 1 (2026-07-17). Introduced per Tech Lead D-12 ruling
/// (Option (b) — DTO reshape) to unblock relocation of
/// <see cref="Services.IJwtTokenService"/> +
/// <see cref="Services.IEntraExternalIdService"/> out of the legacy
/// <c>LankaConnect.Application</c> csproj into
/// <c>Identity.Contracts/Services/</c>. Callers (LoginUserHandler,
/// LoginWithEntraCommandHandler, RefreshTokenHandler) construct the record
/// inline from their loaded <c>User</c> aggregate immediately before the
/// token-issuance call; the shape of the fields mirrors the JwtTokenService
/// claim-set exactly.
/// <para>
/// <see cref="FullName"/> is intentionally omitted from the record — it is a
/// derived <c>FirstName + " " + LastName</c> concatenation and lives in the
/// impl (mirrors <c>User.FullName</c> computed property).
/// </para>
/// </remarks>
public sealed record AccessTokenClaims(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    UserRoleDto Role,
    bool IsActive,
    bool IsEmailVerified,
    string? PhoneNumber = null);
