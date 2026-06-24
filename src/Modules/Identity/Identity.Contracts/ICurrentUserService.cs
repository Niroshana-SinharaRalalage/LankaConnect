namespace LankaConnect.Modules.Identity.Contracts;

/// <summary>
/// Cross-module read-only service that exposes the current authenticated user's
/// identity primitives (extracted from JWT claims in HttpContext by the
/// Identity.Infrastructure.Security.CurrentUserService adapter).
/// </summary>
/// <remarks>
/// Wave 4.6.a (2026-06-24). Relocated from
/// <c>LankaConnect.Application.Common.Interfaces</c> to
/// <c>LankaConnect.Modules.Identity.Contracts</c> per architect Risk #2
/// Option C ruling (2026-06-24). This is the textbook cross-module Contracts
/// citizen: pure primitive surface (Guid, string?, bool) with no User-aggregate
/// type leak, consumed by 54+ files across 6 capability modules + the legacy
/// stack. Sibling port <c>IJwtTokenService</c> stays in legacy because its
/// <c>GenerateAccessTokenAsync(User user)</c> signature takes the User type.
/// <para>
/// Adapter implementation is <c>CurrentUserService</c> in Identity.Infrastructure
/// (relocated at Wave 4.6.c.5). The DI registration moves to
/// <c>Identity.Api.IdentityModule.AddIdentityModule</c> at the same time.
/// </para>
/// </remarks>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's ID.
    /// Returns <see cref="Guid.Empty"/> if the user is not authenticated.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Gets the current user's email.
    /// Returns <c>null</c> if the user is not authenticated.
    /// </summary>
    string? UserEmail { get; }

    /// <summary>
    /// Indicates whether the user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Indicates whether the current user is an admin (Phase 6A.25 — added for
    /// email-group access control and cross-cutting authorization checks).
    /// </summary>
    bool IsAdmin { get; }
}
