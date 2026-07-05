using System.Globalization;
using System.Text;
namespace LankaConnect.Products.LankaEvents.Domain.ValueObjects;

/// <summary>
/// Phase 6A.141 — Paid-Event Ticket Check-in (QR Scanner) end-to-end.
///
/// Encoding and decoding of the on-the-wire QR payload format.
///
/// Two formats are supported:
/// - <see cref="PayloadVersion.V1"/>: <c>v1.&lt;base64url(body)&gt;.&lt;base64url(signature)&gt;</c>
///   where <c>body = "ticketCode|eventId|registrationId|iatUnixSeconds"</c> and the signature is an
///   HMAC-SHA256 of <c>"v1." + base64url(body)</c> computed externally by
///   <c>ITicketSignatureService</c> using a Key Vault-held secret. The VO does NOT
///   perform signing or verification — that's the signature service's concern so the
///   secret remains testable and rotatable.
/// - <see cref="PayloadVersion.Legacy"/>: the pre-141 format, a pure base64 of
///   <c>"ticketCode|eventId|registrationId"</c> with no version prefix and no signature.
///   Legacy payloads parse successfully but expose an empty <see cref="Signature"/> so
///   the verifier can flag them as reduced-trust during the grace window.
///
/// Forward-compat: the version detector is open — unknown <c>v{N}</c> prefixes return null
/// rather than falling through to legacy parsing, so a future v2 deployment can't be
/// misinterpreted by an old decoder.
/// </summary>
public sealed class TicketSignedPayload
{
    public enum PayloadVersion
    {
        Legacy = 0,
        V1 = 1,
    }

    private const string V1Prefix = "v1.";

    public PayloadVersion Version { get; }
    public string TicketCode { get; }
    public Guid EventId { get; }
    public Guid RegistrationId { get; }
    public long IssuedAtUnixSeconds { get; }
    public IReadOnlyList<byte> Signature { get; }

    /// <summary>
    /// The exact byte sequence that an <c>ITicketSignatureService</c> must HMAC over to
    /// produce a valid v1 signature. Stable across equivalent inputs — critical for
    /// signature roundtrip correctness.
    /// </summary>
    public string BodyToSign { get; }

    private TicketSignedPayload(
        PayloadVersion version,
        string ticketCode,
        Guid eventId,
        Guid registrationId,
        long issuedAtUnixSeconds,
        byte[] signature,
        string bodyToSign)
    {
        Version = version;
        TicketCode = ticketCode;
        EventId = eventId;
        RegistrationId = registrationId;
        IssuedAtUnixSeconds = issuedAtUnixSeconds;
        Signature = signature;
        BodyToSign = bodyToSign;
    }

    /// <summary>
    /// Creates an unsigned v1 payload struct. Caller must call <see cref="EncodeWithSignature"/>
    /// once they have the HMAC bytes from <c>ITicketSignatureService</c>.
    /// </summary>
    public static TicketSignedPayload CreateV1(
        string ticketCode,
        Guid eventId,
        Guid registrationId,
        long issuedAtUnixSeconds)
    {
        var body = string.Create(CultureInfo.InvariantCulture,
            $"{ticketCode}|{eventId}|{registrationId}|{issuedAtUnixSeconds}");
        var bodyB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(body));
        var bodyToSign = V1Prefix + bodyB64;

        return new TicketSignedPayload(
            PayloadVersion.V1,
            ticketCode,
            eventId,
            registrationId,
            issuedAtUnixSeconds,
            Array.Empty<byte>(),
            bodyToSign);
    }

    /// <summary>
    /// Produce the final on-the-wire string given the HMAC bytes returned by
    /// <c>ITicketSignatureService.Sign(payload.BodyToSign)</c>.
    /// </summary>
    public string EncodeWithSignature(byte[] signature)
    {
        if (Version != PayloadVersion.V1)
            throw new InvalidOperationException(
                "EncodeWithSignature is only valid for v1 payloads; legacy payloads cannot be re-emitted with a signature.");
        if (signature is null || signature.Length == 0)
            throw new ArgumentException("Signature must be non-empty", nameof(signature));

        return BodyToSign + "." + Base64UrlEncode(signature);
    }

    /// <summary>
    /// Try to parse an input string as either a v1 signed payload or a legacy base64
    /// payload. Returns <c>null</c> on any structural / format failure. Does NOT perform
    /// signature verification.
    /// </summary>
    public static TicketSignedPayload? TryParse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        // Versioned format detection — anything matching "vN." routes through the versioned
        // decoder. Unknown versions return null (won't fall through to legacy parsing).
        if (input.Length >= 3 && input[0] == 'v' && input.Contains('.'))
        {
            return TryParseVersioned(input);
        }

        return TryParseLegacy(input);
    }

    private static TicketSignedPayload? TryParseVersioned(string input)
    {
        // Must start with "v1." for this decoder. Anything else (v2.*, v0.* etc.) is rejected.
        if (!input.StartsWith(V1Prefix, StringComparison.Ordinal))
            return null;

        var parts = input.Split('.');
        // Exactly 3 segments: "v1", body, signature.
        if (parts.Length != 3)
            return null;
        if (parts[1].Length == 0 || parts[2].Length == 0)
            return null;

        byte[] bodyBytes;
        byte[] signatureBytes;
        try
        {
            bodyBytes = Base64UrlDecode(parts[1]);
            signatureBytes = Base64UrlDecode(parts[2]);
        }
        catch
        {
            return null;
        }
        if (signatureBytes.Length == 0)
            return null;

        var body = Encoding.UTF8.GetString(bodyBytes);
        var bodyFields = body.Split('|');
        if (bodyFields.Length != 4)
            return null;

        var ticketCode = bodyFields[0];
        if (string.IsNullOrWhiteSpace(ticketCode))
            return null;

        if (!Guid.TryParse(bodyFields[1], out var eventId))
            return null;
        if (!Guid.TryParse(bodyFields[2], out var registrationId))
            return null;
        if (!long.TryParse(bodyFields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var iat))
            return null;

        var bodyToSign = V1Prefix + parts[1];

        return new TicketSignedPayload(
            PayloadVersion.V1,
            ticketCode,
            eventId,
            registrationId,
            iat,
            signatureBytes,
            bodyToSign);
    }

    private static TicketSignedPayload? TryParseLegacy(string input)
    {
        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(input);
        }
        catch
        {
            return null;
        }

        var body = Encoding.UTF8.GetString(decoded);
        var parts = body.Split('|');
        if (parts.Length != 3)
            return null;

        var ticketCode = parts[0];
        if (string.IsNullOrWhiteSpace(ticketCode))
            return null;
        if (!Guid.TryParse(parts[1], out var eventId))
            return null;
        if (!Guid.TryParse(parts[2], out var registrationId))
            return null;

        return new TicketSignedPayload(
            PayloadVersion.Legacy,
            ticketCode,
            eventId,
            registrationId,
            issuedAtUnixSeconds: 0,
            Array.Empty<byte>(),
            bodyToSign: string.Empty);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        var rem = padded.Length % 4;
        if (rem == 2) padded += "==";
        else if (rem == 3) padded += "=";
        else if (rem == 1) throw new FormatException("Invalid base64url length");
        return Convert.FromBase64String(padded);
    }
}
