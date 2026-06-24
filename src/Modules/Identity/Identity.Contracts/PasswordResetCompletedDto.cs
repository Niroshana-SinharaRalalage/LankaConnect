namespace LankaConnect.Modules.Identity.Contracts;

/// <summary>
/// Result payload from <see cref="IIdentityCommands.CompletePasswordResetAsync"/>.
/// Caller (Communications.ResetPasswordCommandHandler) uses this to dispatch
/// the password-changed-confirmation email.
/// </summary>
/// <remarks>
/// Wave 4.6.a (2026-06-24). Architect-mandated contract per Risk #3 Option A
/// semantic-mutator ruling. The Identity-side adapter owns: token validation,
/// password hashing, refresh-token revocation (all active sessions invalidated
/// per security policy), and the User aggregate state transition.
/// </remarks>
public sealed record PasswordResetCompletedDto(
    Guid UserId,
    string Email,
    string DisplayName);
