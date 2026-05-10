using FluentAssertions;
using LankaConnect.Domain.Events.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Events.ValueObjects;

/// <summary>
/// Phase 8X.1 — validation contract for <see cref="ExternalRegistration"/>.
/// Architect-locked rules (2026-05-07):
/// - HTTPS-only URL ≤2048 chars, must parse as absolute URI.
/// - Reject loopback, RFC1918, link-local hosts (anti-SSRF / anti-phishing defence).
/// - Optional Instructions ≤4000 chars, optional VendorName ≤100 chars.
/// - VO equality on (Url, Instructions, VendorName) tuple.
/// </summary>
public class ExternalRegistrationTests
{
    private const string ValidUrl = "https://eventbrite.com/e/sample-event-12345";

    // ─────────────────────────────────────────────────────────────────────────────
    //  Happy path
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidHttpsUrl_Succeeds()
    {
        var result = ExternalRegistration.Create(ValidUrl);

        result.IsSuccess.Should().BeTrue($"got error: {result.Error}");
        result.Value.Url.Should().Be(ValidUrl);
        result.Value.Instructions.Should().BeNull();
        result.Value.VendorName.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullInstructionsAndVendor_Succeeds()
    {
        var result = ExternalRegistration.Create(ValidUrl, instructions: null, vendorName: null);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithValidInstructionsAndVendor_Succeeds()
    {
        var result = ExternalRegistration.Create(
            ValidUrl,
            instructions: "Pay $25 at the door. Bring a copy of this email.",
            vendorName: "Eventbrite");

        result.IsSuccess.Should().BeTrue();
        result.Value.Instructions.Should().Be("Pay $25 at the door. Bring a copy of this email.");
        result.Value.VendorName.Should().Be("Eventbrite");
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  URL — Phase 8X.11: optional. The factory accepts null/empty URL when at least
    //  one of (instructions, vendor) is supplied. All-three-empty returns Failure
    //  (the application layer treats that as "store ExternalRegistration = null").
    //  Scheme + host validation still fires when URL is non-empty.
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithEmptyUrl_AndInstructionsOnly_Succeeds_StoresNullUrl()
    {
        var result = ExternalRegistration.Create(url: "", instructions: "Pay $25 cash at door");
        result.IsSuccess.Should().BeTrue($"got error: {result.Error}");
        result.Value.Url.Should().BeNull();
        result.Value.Instructions.Should().Be("Pay $25 cash at door");
    }

    [Fact]
    public void Create_WithNullUrl_AndVendorOnly_Succeeds_StoresNullUrl()
    {
        var result = ExternalRegistration.Create(url: null, instructions: null, vendorName: "Eventbrite");
        result.IsSuccess.Should().BeTrue();
        result.Value.Url.Should().BeNull();
        result.Value.VendorName.Should().Be("Eventbrite");
    }

    [Fact]
    public void Create_WithAllNullOrEmpty_Fails_SignalsCallerToStoreNullVo()
    {
        // Phase 8X.11 — all-null factory call signals the application layer to set
        // Event.ExternalRegistration = null (rather than persist an empty VO).
        var result = ExternalRegistration.Create(url: null, instructions: null, vendorName: null);
        result.IsFailure.Should().BeTrue();
        // Error message guides the caller — should mention at-least-one + null-VO.
        result.Error.Should().Contain("at least one");
    }

    [Fact]
    public void Create_WithWhitespaceUrl_AndInstructions_Succeeds_TreatsWhitespaceAsNull()
    {
        var result = ExternalRegistration.Create(url: "   ", instructions: "Call 555-0100");
        result.IsSuccess.Should().BeTrue();
        result.Value.Url.Should().BeNull();
    }

    [Fact]
    public void Create_WithHttpUrl_Fails()
    {
        var result = ExternalRegistration.Create("http://eventbrite.com/e/test");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("https");
    }

    [Fact]
    public void Create_WithMalformedUrl_Fails()
    {
        var result = ExternalRegistration.Create("not a url at all");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("URL");
    }

    [Fact]
    public void Create_WithUrlExceeding2048_Fails()
    {
        var longUrl = "https://example.com/" + new string('a', 2050);
        var result = ExternalRegistration.Create(longUrl);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("2048");
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  URL — host security (anti-SSRF / anti-phishing)
    // ─────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("https://localhost/event")]
    [InlineData("https://127.0.0.1/event")]
    [InlineData("https://[::1]/event")]
    public void Create_WithLoopbackHost_Fails(string url)
    {
        var result = ExternalRegistration.Create(url);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Match(e => e.Contains("loopback") || e.Contains("localhost"));
    }

    [Theory]
    [InlineData("https://10.0.0.1/event")]
    [InlineData("https://10.255.255.255/event")]
    [InlineData("https://172.16.0.1/event")]
    [InlineData("https://172.31.255.255/event")]
    [InlineData("https://192.168.1.1/event")]
    [InlineData("https://192.168.255.255/event")]
    public void Create_WithRfc1918Host_Fails(string url)
    {
        var result = ExternalRegistration.Create(url);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("private");
    }

    [Fact]
    public void Create_WithLinkLocalHost_Fails()
    {
        var result = ExternalRegistration.Create("https://169.254.0.1/event");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Match(e => e.Contains("private") || e.Contains("link-local"));
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  Optional fields — length caps
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithInstructionsExceeding4000_Fails()
    {
        var instructions = new string('x', 4001);
        var result = ExternalRegistration.Create(ValidUrl, instructions: instructions);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("4000");
    }

    [Fact]
    public void Create_WithVendorNameExceeding100_Fails()
    {
        var vendor = new string('y', 101);
        var result = ExternalRegistration.Create(ValidUrl, vendorName: vendor);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("100");
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  Equality contract (ValueObject)
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Equality_SameUrlAndInstructions_AreEqual()
    {
        var a = ExternalRegistration.Create(ValidUrl, "instructions", "Eventbrite").Value;
        var b = ExternalRegistration.Create(ValidUrl, "instructions", "Eventbrite").Value;

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentUrl_AreNotEqual()
    {
        var a = ExternalRegistration.Create("https://eventbrite.com/a").Value;
        var b = ExternalRegistration.Create("https://eventbrite.com/b").Value;

        a.Equals(b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }
}
