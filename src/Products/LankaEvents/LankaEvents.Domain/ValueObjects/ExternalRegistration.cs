using System.Net;
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Phase 8X — External registration details for an <see cref="Enums.EventPaymentMode.ExternalPaid"/> event.
///
/// Validation rules (architect-locked 2026-05-08, Phase 8X.11 revision):
/// <list type="bullet">
///   <item>URL is OPTIONAL. When supplied, it must be HTTPS-only, parse as an absolute
///         <see cref="Uri"/>, max length 2048 chars, and the host must NOT be loopback,
///         RFC1918 private, or link-local (anti-SSRF / anti-phishing).</item>
///   <item>Instructions are optional, max 4000 chars when supplied; rendered as plain text on FE.</item>
///   <item>VendorName is optional, max 100 chars when supplied (e.g., "Eventbrite", "Humanitix").</item>
///   <item>If ALL three fields are null/empty, <see cref="Create"/> returns a failure — the application
///         layer treats this as "store <c>Event.ExternalRegistration = null</c>" rather than persisting
///         an empty value object. The public detail page renders a friendly "Contact organiser for
///         registration details" card in that case.</item>
/// </list>
/// </summary>
public class ExternalRegistration : ValueObject
{
    public const int MaxUrlLength = 2048;
    public const int MaxInstructionsLength = 4000;
    public const int MaxVendorNameLength = 100;

    /// <summary>
    /// Phase 8X.11 — URL is optional. Null when the organiser supplied only instructions
    /// (cash-at-door / bank-deposit / phone-only registration patterns).
    /// </summary>
    public string? Url { get; }
    public string? Instructions { get; }
    public string? VendorName { get; }

    private ExternalRegistration(string? url, string? instructions, string? vendorName)
    {
        Url = url;
        Instructions = instructions;
        VendorName = vendorName;
    }

    public static Result<ExternalRegistration> Create(string? url, string? instructions = null, string? vendorName = null)
    {
        var trimmedUrl = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        var trimmedInstructions = string.IsNullOrWhiteSpace(instructions) ? null : instructions;
        var trimmedVendor = string.IsNullOrWhiteSpace(vendorName) ? null : vendorName.Trim();

        // Phase 8X.11 — all-empty signals "no external registration details supplied".
        // Caller (handler) interprets this failure as "store ExternalRegistration = null on the event"
        // rather than rejecting the request, per architect verdict + product owner Q2 = B.
        if (trimmedUrl == null && trimmedInstructions == null && trimmedVendor == null)
        {
            return Result<ExternalRegistration>.Failure(
                "ExternalRegistration requires at least one of URL, Instructions, or VendorName " +
                "to be non-empty. (The application layer should treat this as a null VO, not a 400.)");
        }

        // URL validation only fires when URL is non-empty (Phase 8X.11 — URL optional).
        if (trimmedUrl != null)
        {
            if (trimmedUrl.Length > MaxUrlLength)
                return Result<ExternalRegistration>.Failure($"External registration URL cannot exceed {MaxUrlLength} characters");

            if (!Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var parsedUri))
                return Result<ExternalRegistration>.Failure("External registration URL is not a valid absolute URL");

            if (!string.Equals(parsedUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                return Result<ExternalRegistration>.Failure("External registration URL must use https");

            var hostRejection = ValidateHost(parsedUri);
            if (hostRejection != null)
                return Result<ExternalRegistration>.Failure(hostRejection);
        }

        if (trimmedInstructions != null && trimmedInstructions.Length > MaxInstructionsLength)
            return Result<ExternalRegistration>.Failure($"External registration instructions cannot exceed {MaxInstructionsLength} characters");

        if (trimmedVendor != null && trimmedVendor.Length > MaxVendorNameLength)
            return Result<ExternalRegistration>.Failure($"External registration vendor name cannot exceed {MaxVendorNameLength} characters");

        return Result<ExternalRegistration>.Success(new ExternalRegistration(trimmedUrl, trimmedInstructions, trimmedVendor));
    }

    /// <summary>
    /// Rejects URLs whose host points at the local machine, an internal-only address
    /// (RFC1918), or link-local space. Defends against SSRF-style misuse and phishing
    /// surfaces where an organiser pastes an internal URL by mistake.
    /// </summary>
    private static string? ValidateHost(Uri uri)
    {
        var host = uri.Host;
        if (string.IsNullOrWhiteSpace(host))
            return "External registration URL must include a host";

        var hostLower = host.ToLowerInvariant();
        if (hostLower == "localhost" || hostLower.EndsWith(".localhost"))
            return "External registration URL must not point to localhost";

        if (IPAddress.TryParse(host.Trim('[', ']'), out var ip))
        {
            if (IPAddress.IsLoopback(ip))
                return "External registration URL must not point to a loopback address";

            if (IsPrivateOrLinkLocal(ip))
                return "External registration URL must not point to a private or link-local address";
        }

        return null;
    }

    private static bool IsPrivateOrLinkLocal(IPAddress ip)
    {
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();
            if (bytes[0] == 10) return true;
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            if (bytes[0] == 169 && bytes[1] == 254) return true;
            if (bytes[0] == 0) return true;
        }
        else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal) return true;
            if (ip.IsIPv6SiteLocal) return true;
            var bytes = ip.GetAddressBytes();
            if (bytes[0] == 0xfc || bytes[0] == 0xfd) return true;
        }

        return false;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Url ?? string.Empty;
        yield return Instructions ?? string.Empty;
        yield return VendorName ?? string.Empty;
    }

    public override string ToString() => Url ?? VendorName ?? "(external registration)";
}
