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
    /// configured secret. Throws <see cref="InvalidOperationException"/> if the secret
    /// is not configured.
    /// </summary>
    byte[] Sign(string bodyToSign);

    /// <summary>
    /// Compares a candidate <paramref name="signature"/> against the HMAC of
    /// <paramref name="bodyToSign"/> using a constant-time comparison. Returns
    /// <c>true</c> iff the signature is valid for the body under the configured secret.
    /// </summary>
    bool Verify(string bodyToSign, ReadOnlySpan<byte> signature);
}
