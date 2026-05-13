namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Phase 6A.141 — produces and verifies HMAC-SHA256 signatures over the body bytes of
/// a <c>TicketSignedPayload</c>. Backed by a secret stored in Azure Key Vault as
/// <c>TICKET-QR-SIGNING-KEY</c> in production / read from configuration in dev.
///
/// Separated from <c>TicketSignedPayload</c> (a pure value object) so that secret
/// lookup, rotation, and constant-time comparison live behind an interface that's
/// trivially mockable in unit tests and replaceable in infrastructure DI.
/// </summary>
public interface ITicketSignatureService
{
    /// <summary>
    /// Computes an HMAC-SHA256 signature over <paramref name="bodyToSign"/> using the
    /// configured CURRENT secret. Throws <see cref="InvalidOperationException"/> if the
    /// current secret is not configured.
    ///
    /// <c>Sign</c> never uses the previous key — that key only exists to verify in-flight
    /// signatures that were minted before a rotation.
    /// </summary>
    byte[] Sign(string bodyToSign);

    /// <summary>
    /// Compares a candidate <paramref name="signature"/> against the HMAC of
    /// <paramref name="bodyToSign"/> using a constant-time comparison.
    ///
    /// Implements key rotation grace: tries the CURRENT secret first, then if a PREVIOUS
    /// secret is configured, falls back to that. Returns a struct so callers can
    /// distinguish "verified with current" from "verified with previous" — the latter
    /// is a signal that the QR was minted before the most-recent rotation, useful for
    /// audit-log forensics.
    /// </summary>
    TicketSignatureVerifyResult Verify(string bodyToSign, ReadOnlySpan<byte> signature);
}

/// <summary>
/// Phase 6A.141 — outcome of an HMAC verification. Carries two bits of information:
/// whether the signature is valid at all, and (if so) which configured secret matched.
///
/// The audit log uses <see cref="UsedPreviousKey"/> to record that a QR was verified
/// against the post-rotation grace-window key, so we can spot tickets minted before
/// the rotation cut-over.
/// </summary>
public readonly record struct TicketSignatureVerifyResult(bool IsValid, bool UsedPreviousKey)
{
    public static TicketSignatureVerifyResult Invalid { get; } = new(false, false);
    public static TicketSignatureVerifyResult VerifiedWithCurrent { get; } = new(true, false);
    public static TicketSignatureVerifyResult VerifiedWithPrevious { get; } = new(true, true);
}
