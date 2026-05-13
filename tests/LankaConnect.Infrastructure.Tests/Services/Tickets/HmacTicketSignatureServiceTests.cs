using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Infrastructure.Services.Tickets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Services.Tickets;

/// <summary>
/// Phase 6A.141 — Paid-Event Ticket Check-in: signature-service tests.
///
/// The service must:
/// - sign deterministically (same body → same signature) so callers can compare
/// - verify a valid sign-then-verify roundtrip
/// - reject any tampered body
/// - reject any signature produced with a different secret
/// - constant-time compare (verified structurally via FixedTimeEquals; we don't time the call)
/// - refuse to construct without a configured secret (fail-fast on misconfig)
/// </summary>
public class HmacTicketSignatureServiceTests
{
    // Two distinct 32-byte secrets, base64-encoded.
    private const string SecretA = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
    private const string SecretB = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBA=";

    private static ITicketSignatureService BuildService(string secretBase64)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("Tickets:QrSigningKey", secretBase64) })
            .Build();
        return new HmacTicketSignatureService(config, NullLogger<HmacTicketSignatureService>.Instance);
    }

    [Fact]
    public void Sign_Then_Verify_Roundtrip_Succeeds()
    {
        var svc = BuildService(SecretA);
        var body = "v1.aGVsbG8tc2lnbi1tZQ";

        var sig = svc.Sign(body);

        sig.Should().NotBeNull();
        sig.Length.Should().Be(32, "HMAC-SHA256 produces exactly 32 bytes");
        svc.Verify(body, sig).Should().BeTrue();
    }

    [Fact]
    public void Sign_Is_Deterministic_For_Same_Body_And_Secret()
    {
        var svc = BuildService(SecretA);
        var body = "v1.deterministic-test";

        var sig1 = svc.Sign(body);
        var sig2 = svc.Sign(body);

        sig1.Should().BeEquivalentTo(sig2);
    }

    [Fact]
    public void Verify_With_Tampered_Body_Returns_False()
    {
        var svc = BuildService(SecretA);
        var sig = svc.Sign("v1.original-body");

        svc.Verify("v1.tampered-body", sig).Should().BeFalse();
    }

    [Fact]
    public void Verify_With_Signature_From_Different_Secret_Returns_False()
    {
        var svcA = BuildService(SecretA);
        var svcB = BuildService(SecretB);
        var body = "v1.same-body-different-keys";

        var sigA = svcA.Sign(body);
        svcB.Verify(body, sigA).Should().BeFalse(
            "a signature created with secret A must not validate under secret B");
    }

    [Fact]
    public void Verify_With_Wrong_Length_Signature_Returns_False()
    {
        var svc = BuildService(SecretA);

        svc.Verify("v1.x", new byte[16]).Should().BeFalse("16 bytes is not HMAC-SHA256 length");
        svc.Verify("v1.x", Array.Empty<byte>()).Should().BeFalse("empty signature must be rejected");
        svc.Verify("v1.x", new byte[64]).Should().BeFalse("64 bytes is HMAC-SHA512 length, not what we use");
    }

    [Fact]
    public void Construct_Without_Configured_Secret_Throws()
    {
        var emptyConfig = new ConfigurationBuilder().Build();

        Action act = () => new HmacTicketSignatureService(emptyConfig, NullLogger<HmacTicketSignatureService>.Instance);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TICKET-QR-SIGNING-KEY*");
    }

    [Fact]
    public void Construct_With_Too_Short_Secret_Throws()
    {
        // 8-byte UTF-8 string (decodes to 8 bytes via UTF-8 fallback path since not base64)
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("Tickets:QrSigningKey", "shorty01") })
            .Build();

        Action act = () => new HmacTicketSignatureService(config, NullLogger<HmacTicketSignatureService>.Instance);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*too short*");
    }

    [Fact]
    public void Construct_With_NonBase64_Secret_Falls_Back_To_Utf8_Bytes()
    {
        // 24-byte plain string (>=16 bytes so allowed); the service should log a warning
        // and still construct successfully.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>(
                "Tickets:QrSigningKey", "this-is-a-plain-string-not-base64-but-long-enough") })
            .Build();

        var svc = new HmacTicketSignatureService(config, NullLogger<HmacTicketSignatureService>.Instance);

        // If construction didn't throw, the service is usable.
        var body = "v1.test";
        var sig = svc.Sign(body);
        svc.Verify(body, sig).Should().BeTrue();
    }
}
