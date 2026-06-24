namespace LankaConnect.Modules.Identity.Contracts;

/// <summary>
/// Cross-module mutator API for the Identity module.
/// SCOPE: only operations invoked FROM other modules. Within-Identity mutators
/// (Create/Update/Delete User, role-upgrade workflow, profile edits, etc.) move
/// into Identity.Application in Wave 4.6.c.1-c.4 and are NOT exposed via this
/// contract -- they remain MediatR commands dispatched by the controllers that
/// already live inside the module.
/// </summary>
/// <remarks>
/// Wave 4.6.a (2026-06-24). Architect-mandated SEMANTIC mutators per Risk #3
/// Option A ruling (2026-06-24): the two cross-module entry points below own
/// hashing + expiry + throttle + refresh-token revocation + recently-sent check
/// on the Identity side. The architect's spec used <c>Task&lt;Result&lt;T&gt;&gt;</c>
/// returns; that's been simplified to nullable-return-or-throw because
/// <c>Result&lt;T&gt;</c> lives in <c>LankaConnect.Domain.Common</c>, not in
/// BuildingBlocks.Contracts (the only allowed Contracts dependency). Promoting
/// <c>Result&lt;T&gt;</c> to a Contracts citizen is deferred to a future cleanup
/// wave. Communications.SendPasswordResetCommandHandler +
/// Communications.ResetPasswordCommandHandler swap from <c>IUserRepository</c>
/// + <c>IPasswordHashingService</c> injection to <c>IIdentityCommands</c> at
/// Wave 4.6.d.1.
/// <para>
/// Do NOT expose raw mutators (<c>SetPasswordResetTokenAsync</c>,
/// <c>SetPasswordAsync</c>) here -- those would leak hashing-service + lifetime
/// + throttle business rules back to Communications. The Identity adapter
/// owns those concerns.
/// </para>
/// </remarks>
public interface IIdentityCommands
{
    /// <summary>
    /// Initiates a password reset for the user matching the supplied email.
    /// Generates a one-shot token, persists it on the User aggregate with the
    /// supplied expiry, and returns the payload Communications needs to
    /// dispatch the reset email.
    /// </summary>
    /// <remarks>
    /// Throttle behavior (architect-specified):
    /// - If the User already has a non-expired token AND the request is
    ///   within 5 minutes of the previous request AND
    ///   <paramref name="forceResend"/> is <c>false</c>: the existing token
    ///   is REUSED (no new token generated), <c>WasThrottled</c> is set to
    ///   <c>true</c>, and the caller MUST still dispatch the email (we
    ///   cannot tell if the previous one was delivered).
    /// - If <paramref name="forceResend"/> is <c>true</c>: always generates
    ///   a fresh token + new expiry.
    /// - If no user matches <paramref name="email"/>: returns failure (the
    ///   caller decides whether to surface "we have sent an email if the
    ///   address exists" to the UI to avoid enumeration leaks).
    /// </remarks>
    /// <remarks>
    /// Returns <c>null</c> when no user matches <paramref name="email"/> (caller
    /// should surface a generic "we have sent an email if the address exists"
    /// to avoid email-enumeration leaks). For all other failure modes, throws
    /// <see cref="InvalidOperationException"/> with a structured message --
    /// callers catch + log + present a generic error to the UI.
    /// </remarks>
    Task<PasswordResetInitiatedDto?> InitiatePasswordResetAsync(
        string email,
        TimeSpan tokenLifetime,
        bool forceResend,
        CancellationToken ct = default);

    /// <summary>
    /// Completes a password reset. Validates the supplied token (existence +
    /// expiry), hashes the new plaintext password, persists it on the User
    /// aggregate, revokes ALL active refresh tokens (security policy:
    /// password change invalidates all sessions), and returns the payload
    /// Communications needs to dispatch the password-changed-confirmation
    /// email.
    /// </summary>
    /// <remarks>
    /// <paramref name="emailFallback"/> is used only when the token is
    /// expired or absent and the caller has the user's email from a
    /// previous step (e.g. a "reset link expired -- here is a new one"
    /// flow). Pass <c>null</c> for the standard flow.
    /// </remarks>
    /// <remarks>
    /// Throws <see cref="InvalidOperationException"/> when the token is missing
    /// / expired / already used (caller catches + surfaces a generic "your
    /// reset link is invalid or expired -- request a new one" to the UI).
    /// </remarks>
    Task<PasswordResetCompletedDto> CompletePasswordResetAsync(
        string token,
        string? emailFallback,
        string newPlaintextPassword,
        CancellationToken ct = default);
}
