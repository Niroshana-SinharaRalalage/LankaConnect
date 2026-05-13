using System.Security.Cryptography;
using System.Text;
using LankaConnect.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Services.Tickets;

/// <summary>
/// Phase 6A.141 — HMAC-SHA256 implementation of <see cref="ITicketSignatureService"/>.
///
/// Secret is read at construction time from configuration key
/// <c>Tickets:QrSigningKey</c>. In Azure that key resolves to the Key Vault secret
/// <c>TICKET-QR-SIGNING-KEY</c>; in dev/test it can be set via appsettings or
/// environment variable. Treated as a hot path — the HMAC object is created per-call
/// (cheap; ~µs) so that a rotation via container restart is picked up without further
/// invalidation.
///
/// Rotation procedure: see <c>docs/RUNBOOK_TICKET_SIGNING_KEY_ROTATION.md</c>.
/// </summary>
public class HmacTicketSignatureService : ITicketSignatureService
{
    private const string SecretConfigKey = "Tickets:QrSigningKey";
    private readonly byte[] _secret;
    private readonly ILogger<HmacTicketSignatureService> _logger;

    public HmacTicketSignatureService(IConfiguration configuration, ILogger<HmacTicketSignatureService> logger)
    {
        _logger = logger;
        var configured = configuration[SecretConfigKey];
        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException(
                $"Ticket QR signing secret is not configured. Set '{SecretConfigKey}' (or Azure Key Vault secret 'TICKET-QR-SIGNING-KEY') to a base64-encoded 32-byte random value.");
        }

        try
        {
            _secret = Convert.FromBase64String(configured);
        }
        catch (FormatException)
        {
            // Fall back to UTF-8 bytes if the value isn't base64-encoded. Logged so misconfig
            // shows up in startup, but doesn't crash — some local dev setups may use a plain
            // string for convenience. Production should always be base64-encoded 32+ bytes.
            _logger.LogWarning(
                "Ticket QR signing secret in config key '{Key}' is not base64-encoded; falling back to UTF-8 bytes. This is acceptable for dev/test but production should use a base64-encoded 32-byte secret.",
                SecretConfigKey);
            _secret = Encoding.UTF8.GetBytes(configured);
        }

        if (_secret.Length < 16)
        {
            throw new InvalidOperationException(
                $"Ticket QR signing secret is too short ({_secret.Length} bytes). Use at least 16 bytes (32 recommended).");
        }
    }

    /// <inheritdoc />
    public byte[] Sign(string bodyToSign)
    {
        if (bodyToSign is null)
            throw new ArgumentNullException(nameof(bodyToSign));

        try
        {
            using var hmac = new HMACSHA256(_secret);
            var bytes = Encoding.UTF8.GetBytes(bodyToSign);
            return hmac.ComputeHash(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign ticket payload body (length={Length})", bodyToSign?.Length ?? 0);
            throw;
        }
    }

    /// <inheritdoc />
    public bool Verify(string bodyToSign, ReadOnlySpan<byte> signature)
    {
        if (bodyToSign is null) return false;
        if (signature.Length != 32)
        {
            // HMAC-SHA256 is exactly 32 bytes. Anything else is a structural reject.
            return false;
        }

        try
        {
            using var hmac = new HMACSHA256(_secret);
            var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(bodyToSign));
            // Constant-time comparison — important to avoid timing side channels that
            // would leak signature bytes one-at-a-time.
            return CryptographicOperations.FixedTimeEquals(expected, signature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify ticket payload signature (length={Length})", bodyToSign?.Length ?? 0);
            return false;
        }
    }
}
