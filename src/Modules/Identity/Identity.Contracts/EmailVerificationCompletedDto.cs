namespace LankaConnect.Modules.Identity.Contracts;

/// <summary>
/// Result payload from <see cref="IIdentityCommands.CompleteEmailVerificationAsync"/>.
/// Caller (Communications.VerifyEmailCommandHandler) uses this to dispatch
/// the welcome email (or similar post-verification side effect).
/// </summary>
/// <remarks>
/// Wave 4.6.d.2.a (2026-06-24). Architect-mandated semantic-mutator contract.
/// The Identity-side adapter owns token validation + the User aggregate
/// MarkEmailAsVerified state transition.
/// </remarks>
public sealed record EmailVerificationCompletedDto(
    Guid UserId,
    string Email,
    string DisplayName);
