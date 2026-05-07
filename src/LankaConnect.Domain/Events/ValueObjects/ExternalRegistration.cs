using System.Net;
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Phase 8X — External registration details for an <see cref="Enums.EventPaymentMode.ExternalPaid"/> event.
///
/// Validation rules (architect-locked 2026-05-07):
/// <list type="bullet">
///   <item>URL is required, must be HTTPS-only (Phase 6A.81 security default — never silently fall back to http).</item>
///   <item>URL must parse as an absolute <see cref="Uri"/>; max length 2048 chars.</item>
///   <item>URL host must NOT be loopback (127.0.0.0/8, localhost, ::1), RFC1918 private (10/8, 172.16/12, 192.168/16),
///         or link-local (169.254/16, fe80::/10) — anti-SSRF and anti-phishing defence.</item>
///   <item>Instructions are optional, max 4000 chars when supplied; rendered as plain text on FE.</item>
///   <item>VendorName is optional, max 100 chars when supplied (e.g., "Eventbrite", "Humanitix").</item>
/// </list>
/// </summary>
public class ExternalRegistration : ValueObject
{
    public const int MaxUrlLength = 2048;
    public const int MaxInstructionsLength = 4000;
    public const int MaxVendorNameLength = 100;

    public string Url { get; }
    public string? Instructions { get; }
    public string? VendorName { get; }

    private ExternalRegistration(string url, string? instructions, string? vendorName)
    {
        Url = url;
        Instructions = instructions;
        VendorName = vendorName;
    }

    public static Result<ExternalRegistration> Create(string? url, string? instructions = null, string? vendorName = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Result<ExternalRegistration>.Failure("External registration URL is required");

        url = url.Trim();

        if (url.Length > MaxUrlLength)
            return Result<ExternalRegistration>.Failure($"External registration URL cannot exceed {MaxUrlLength} characters");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsedUri))
            return Result<ExternalRegistration>.Failure("External registration URL is not a valid absolute URL");

        if (!string.Equals(parsedUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return Result<ExternalRegistration>.Failure("External registration URL must use https");

        var hostRejection = ValidateHost(parsedUri);
        if (hostRejection != null)
            return Result<ExternalRegistration>.Failure(hostRejection);

        var trimmedInstructions = string.IsNullOrWhiteSpace(instructions) ? null : instructions;
        if (trimmedInstructions != null && trimmedInstructions.Length > MaxInstructionsLength)
            return Result<ExternalRegistration>.Failure($"External registration instructions cannot exceed {MaxInstructionsLength} characters");

        var trimmedVendor = string.IsNullOrWhiteSpace(vendorName) ? null : vendorName.Trim();
        if (trimmedVendor != null && trimmedVendor.Length > MaxVendorNameLength)
            return Result<ExternalRegistration>.Failure($"External registration vendor name cannot exceed {MaxVendorNameLength} characters");

        return Result<ExternalRegistration>.Success(new ExternalRegistration(url, trimmedInstructions, trimmedVendor));
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

        // Hostname-style checks — lowercased compare.
        var hostLower = host.ToLowerInvariant();
        if (hostLower == "localhost" || hostLower.EndsWith(".localhost"))
            return "External registration URL must not point to localhost";

        // IP-literal checks. Accept only if the host parses as an IPAddress and is NOT
        // a private / loopback / link-local address. Hostnames that don't parse as IPs
        // are accepted at this layer (DNS-time SSRF is out of scope; a moderator allow-list
        // is the Phase 8Y mitigation per architect verdict).
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
            // 10.0.0.0/8
            if (bytes[0] == 10) return true;
            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            // 169.254.0.0/16 (link-local)
            if (bytes[0] == 169 && bytes[1] == 254) return true;
            // 0.0.0.0/8 — invalid as a destination
            if (bytes[0] == 0) return true;
        }
        else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            // fe80::/10 link-local
            if (ip.IsIPv6LinkLocal) return true;
            // fc00::/7 unique-local
            if (ip.IsIPv6SiteLocal) return true;
            var bytes = ip.GetAddressBytes();
            if (bytes[0] == 0xfc || bytes[0] == 0xfd) return true;
        }

        return false;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Url;
        yield return Instructions ?? string.Empty;
        yield return VendorName ?? string.Empty;
    }

    public override string ToString() => Url;
}
