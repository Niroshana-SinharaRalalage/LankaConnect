namespace LankaConnect.Modules.Identity.Contracts;

/// <summary>
/// Result payload from <see cref="IIdentityCommands.InitiatePasswordResetAsync"/>.
/// Caller (Communications.SendPasswordResetCommandHandler) uses the returned
/// token + recipient email + display name to dispatch the password-reset email.
/// </summary>
/// <remarks>
/// Wave 4.6.a (2026-06-24). Architect-mandated contract per Risk #3 Option A
/// semantic-mutator ruling: the Identity-side adapter owns hashing, expiry,
/// throttle, and the 5-minute resend-throttle check. The caller receives just
/// the email-orchestration payload.
/// <para>
/// <see cref="WasThrottled"/> is <c>true</c> when an existing token was reused
/// (resend within 5 minutes of the previous request); the caller MUST send the
/// email anyway because we cannot tell if the previous one was delivered.
/// </para>
/// </remarks>
public sealed record PasswordResetInitiatedDto(
    Guid UserId,
    string Email,
    string DisplayName,
    string PasswordResetToken,
    DateTime TokenExpiresAt,
    bool WasThrottled);
