using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Identity.Contracts.DTOs;

namespace LankaConnect.Modules.Identity.Contracts.Services;

/// <summary>
/// Cross-module JWT issuance + validation surface for the Identity module.
/// </summary>
/// <remarks>
/// Wave 8.5.a Part 1 (2026-07-17). Relocated from
/// <c>LankaConnect.BuildingBlocks.Application.Common.Interfaces</c> (physical
/// home: <c>src/LankaConnect.Application/Common/Interfaces/</c>) into
/// <c>Identity.Contracts/Services/</c> per Consult #15 PASS C (interfaces +
/// their DTO signatures live in Contracts). The
/// <see cref="GenerateAccessTokenAsync"/> signature reshape from
/// <c>User</c> (Identity.Domain aggregate) to
/// <see cref="AccessTokenClaims"/> (Contracts DTO) was ratified by Tech Lead
/// D-12 (2026-07-17) as Option (b) — the ONLY option that (a) keeps
/// Contracts free of Identity.Domain leakage and (b) does not require a new
/// csproj. Callers construct the DTO inline from their loaded <c>User</c>
/// aggregate immediately before invoking this service.
/// </remarks>
public interface IJwtTokenService
{
    /// <summary>
    /// Mints an access token (JWT) from the supplied claims projection.
    /// The claims record is populated by the caller from the authenticated
    /// User aggregate immediately before the call — <see cref="IJwtTokenService"/>
    /// itself never touches the Identity.Domain layer.
    /// </summary>
    Task<Result<string>> GenerateAccessTokenAsync(AccessTokenClaims claims);
    Task<Result<string>> GenerateRefreshTokenAsync();
    Task<Result<Guid>> ValidateTokenAsync(string token);
    Task<bool> IsTokenValidAsync(string token);
    Task<Result> InvalidateRefreshTokenAsync(string refreshToken);
    Task<Result> InvalidateAllUserTokensAsync(Guid userId);
}
