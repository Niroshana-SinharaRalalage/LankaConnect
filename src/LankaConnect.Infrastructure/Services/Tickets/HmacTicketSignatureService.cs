using System.Security.Cryptography;
using System.Text;
using LankaConnect.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Services.Tickets;

/// <summary>
/// Phase 6A.141 — HMAC-SHA256 implementation of <see cref="ITicketSignatureService"/>
/// with key-rotation grace via dual-key verify.
///
/// Configuration:
/// - <c>Tickets:QrSigningKey</c> — REQUIRED. Maps to Azure Key Vault secret
///   <c>TICKET-QR-SIGNING-KEY</c>. Used to <see cref="Sign"/> all newly generated QRs.
///   Also tried FIRST during <see cref="Verify"/>.
/// - <c>Tickets:QrSigningKeyPrevious</c> — OPTIONAL. Maps to Azure Key Vault secret
///   <c>TICKET-QR-SIGNING-KEY-PREVIOUS</c>. Tried as a FALLBACK during <see cref="Verify"/>.
///   Present only during a rotation grace window — set to the OLD key value when
///   provisioning a new <c>QrSigningKey</c>, so in-flight QRs minted under the old key
///   continue to scan until the grace window closes.
///
/// Rotation procedure (event-safe — no in-flight invalidation):
///   1. Generate new secret: <c>openssl rand -base64 32</c>
///   2. Set <c>TICKET-QR-SIGNING-KEY-PREVIOUS</c> = current value of <c>TICKET-QR-SIGNING-KEY</c>
///   3. Set <c>TICKET-QR-SIGNING-KEY</c> = new secret
///   4. Restart Container App revision — picks up new pair
///   5. After (eventEndDate + 30 days) for all events with active tickets, drop the previous key
///
/// Sign always uses the CURRENT key — the previous key is verify-only so an attacker
/// who exfiltrates a stale grace-window secret cannot mint forward-valid QRs with it.
/// </summary>
public class HmacTicketSignatureService : ITicketSignatureService
{
    private const string CurrentSecretConfigKey = "Tickets:QrSigningKey";
    private const string PreviousSecretConfigKey = "Tickets:QrSigningKeyPrevious";

    private readonly byte[] _currentSecret;
    private readonly byte[]? _previousSecret;
    private readonly ILogger<HmacTicketSignatureService> _logger;

    public HmacTicketSignatureService(IConfiguration configuration, ILogger<HmacTicketSignatureService> logger)
    {
        _logger = logger;

        var currentRaw = configuration[CurrentSecretConfigKey];
        if (string.IsNullOrWhiteSpace(currentRaw))
        {
            throw new InvalidOperationException(
                $"Ticket QR signing secret is not configured. Set '{CurrentSecretConfigKey}' (Azure Key Vault secret 'TICKET-QR-SIGNING-KEY') to a base64-encoded 32-byte random value.");
        }
        _currentSecret = DecodeSecret(currentRaw, CurrentSecretConfigKey);

        var previousRaw = configuration[PreviousSecretConfigKey];
        if (!string.IsNullOrWhiteSpace(previousRaw))
        {
            _previousSecret = DecodeSecret(previousRaw, PreviousSecretConfigKey);
            _logger.LogInformation(
                "Ticket QR signature service started in rotation-grace mode — verify will accept signatures from either the current or the previous key. Drop '{PrevKey}' from Key Vault once all in-flight tickets have been re-signed (lazy regeneration on next view) or after the longest-out event has ended.",
                PreviousSecretConfigKey);
        }
        else
        {
            _logger.LogInformation(
                "Ticket QR signature service started in single-key mode — verify accepts signatures from the current key only. Set '{PrevKey}' to enable rotation-grace verify.",
                PreviousSecretConfigKey);
        }
    }

    private byte[] DecodeSecret(string raw, string configKey)
    {
        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(raw);
        }
        catch (FormatException)
        {
            // Dev/test convenience: fall back to UTF-8 bytes if not base64. Logged as warning
            // so misconfig surfaces in container logs; production should always be a
            // base64-encoded 32-byte secret.
            _logger.LogWarning(
                "Ticket QR signing secret in config key '{Key}' is not base64-encoded; falling back to UTF-8 bytes. Production must use a base64-encoded 32-byte secret.",
                configKey);
            decoded = Encoding.UTF8.GetBytes(raw);
        }

        if (decoded.Length < 16)
        {
            throw new InvalidOperationException(
                $"Ticket QR signing secret '{configKey}' is too short ({decoded.Length} bytes). Use at least 16 bytes (32 recommended).");
        }

        return decoded;
    }

    /// <inheritdoc />
    public byte[] Sign(string bodyToSign)
    {
        if (bodyToSign is null)
            throw new ArgumentNullException(nameof(bodyToSign));

        try
        {
            using var hmac = new HMACSHA256(_currentSecret);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(bodyToSign));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign ticket payload body (length={Length})", bodyToSign.Length);
            throw;
        }
    }

    /// <inheritdoc />
    public TicketSignatureVerifyResult Verify(string bodyToSign, ReadOnlySpan<byte> signature)
    {
        if (bodyToSign is null) return TicketSignatureVerifyResult.Invalid;
        if (signature.Length != 32) return TicketSignatureVerifyResult.Invalid;

        // Capture the candidate bytes once; HMAC objects are short-lived per check.
        byte[] candidateBytes = signature.ToArray();

        try
        {
            // Try CURRENT key first — the common path. Always-checked so a present-key
            // verify isn't shortcut by a present previous-key check.
            using (var hmacCurrent = new HMACSHA256(_currentSecret))
            {
                var expectedCurrent = hmacCurrent.ComputeHash(Encoding.UTF8.GetBytes(bodyToSign));
                if (CryptographicOperations.FixedTimeEquals(expectedCurrent, candidateBytes))
                {
                    return TicketSignatureVerifyResult.VerifiedWithCurrent;
                }
            }

            // Fall back to PREVIOUS key if configured (rotation grace window).
            if (_previousSecret is not null)
            {
                using var hmacPrevious = new HMACSHA256(_previousSecret);
                var expectedPrevious = hmacPrevious.ComputeHash(Encoding.UTF8.GetBytes(bodyToSign));
                if (CryptographicOperations.FixedTimeEquals(expectedPrevious, candidateBytes))
                {
                    _logger.LogInformation(
                        "Ticket QR signature verified using the PREVIOUS key — this QR was minted before the most-recent rotation. The audit log will record UsedPreviousKey=true.");
                    return TicketSignatureVerifyResult.VerifiedWithPrevious;
                }
            }

            return TicketSignatureVerifyResult.Invalid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify ticket payload signature (body length={Length})", bodyToSign.Length);
            return TicketSignatureVerifyResult.Invalid;
        }
    }
}
