namespace LankaConnect.Modules.Identity.Contracts;

/// <summary>
/// Result payload from <see cref="IIdentityCommands.InitiateEmailVerificationAsync"/>.
/// Caller (Communications.SendEmailVerificationCommandHandler) uses the returned
/// token + recipient email + display name to dispatch the verification email.
/// </summary>
/// <remarks>
/// Wave 4.6.d.2.a (2026-06-24). Architect-mandated semantic-mutator contract per
/// the d.2.a ruling: the Identity-side adapter owns token generation, expiry,
/// and the User aggregate state transition. The caller receives just the
/// email-orchestration payload.
/// </remarks>
public sealed record EmailVerificationInitiatedDto(
    Guid UserId,
    string Email,
    string DisplayName,
    string EmailVerificationToken,
    DateTime TokenExpiresAt);
