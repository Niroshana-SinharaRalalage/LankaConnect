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
/// Covers single-key and dual-key (rotation-grace) configurations.
///
/// The service must:
/// - sign deterministically (same body → same signature) so callers can compare
/// - verify a valid sign-then-verify roundtrip
/// - reject any tampered body
/// - reject any signature produced with a different secret
/// - constant-time compare (verified structurally via FixedTimeEquals; we don't time the call)
/// - refuse to construct without a configured current secret (fail-fast on misconfig)
/// - in rotation-grace mode: verify accepts signatures from EITHER current or previous key,
///   and report which one matched so the audit log can record it
/// - in rotation-grace mode: Sign STILL uses only the current key — the previous key is
///   verify-only so a stolen old secret cannot mint forward-valid QRs
/// </summary>
public class HmacTicketSignatureServiceTests
{
    // Three distinct 32-byte secrets (base64-encoded) for current / previous / unrelated.
    private const string SecretA = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
    private const string SecretB = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBA=";
    private const string SecretC = "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCA=";

    private static ITicketSignatureService BuildSingleKey(string secretBase64)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Tickets:QrSigningKey", secretBase64),
            })
            .Build();
        return new HmacTicketSignatureService(config, NullLogger<HmacTicketSignatureService>.Instance);
    }

    private static ITicketSignatureService BuildDualKey(string currentBase64, string previousBase64)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Tickets:QrSigningKey", currentBase64),
                new KeyValuePair<string, string?>("Tickets:QrSigningKeyPrevious", previousBase64),
            })
            .Build();
        return new HmacTicketSignatureService(config, NullLogger<HmacTicketSignatureService>.Instance);
    }

    // ============================================================
    // Single-key behaviour (sanity)
    // ============================================================

    [Fact]
    public void Sign_Then_Verify_Roundtrip_Succeeds()
    {
        var svc = BuildSingleKey(SecretA);
        var body = "v1.aGVsbG8tc2lnbi1tZQ";

        var sig = svc.Sign(body);

        sig.Should().NotBeNull();
        sig.Length.Should().Be(32, "HMAC-SHA256 produces exactly 32 bytes");
        var result = svc.Verify(body, sig);
        result.IsValid.Should().BeTrue();
        result.UsedPreviousKey.Should().BeFalse("single-key mode never reports previous-key use");
    }

    [Fact]
    public void Sign_Is_Deterministic_For_Same_Body_And_Secret()
    {
        var svc = BuildSingleKey(SecretA);
        var body = "v1.deterministic-test";

        var sig1 = svc.Sign(body);
        var sig2 = svc.Sign(body);

        sig1.Should().BeEquivalentTo(sig2);
    }

    [Fact]
    public void Verify_With_Tampered_Body_Returns_Invalid()
    {
        var svc = BuildSingleKey(SecretA);
        var sig = svc.Sign("v1.original-body");

        var result = svc.Verify("v1.tampered-body", sig);
        result.IsValid.Should().BeFalse();
        result.UsedPreviousKey.Should().BeFalse();
    }

    [Fact]
    public void Verify_With_Signature_From_Different_Secret_Returns_Invalid()
    {
        var svcA = BuildSingleKey(SecretA);
        var svcB = BuildSingleKey(SecretB);
        var body = "v1.same-body-different-keys";

        var sigA = svcA.Sign(body);
        var result = svcB.Verify(body, sigA);
        result.IsValid.Should().BeFalse(
            "a signature created with secret A must not validate under secret B");
    }

    [Fact]
    public void Verify_With_Wrong_Length_Signature_Returns_Invalid()
    {
        var svc = BuildSingleKey(SecretA);

        svc.Verify("v1.x", new byte[16]).IsValid.Should().BeFalse("16 bytes is not HMAC-SHA256 length");
        svc.Verify("v1.x", Array.Empty<byte>()).IsValid.Should().BeFalse("empty signature must be rejected");
        svc.Verify("v1.x", new byte[64]).IsValid.Should().BeFalse("64 bytes is HMAC-SHA512 length, not what we use");
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
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>(
                "Tickets:QrSigningKey", "this-is-a-plain-string-not-base64-but-long-enough") })
            .Build();

        var svc = new HmacTicketSignatureService(config, NullLogger<HmacTicketSignatureService>.Instance);

        var body = "v1.test";
        var sig = svc.Sign(body);
        svc.Verify(body, sig).IsValid.Should().BeTrue();
    }

    // ============================================================
    // Dual-key (rotation-grace) behaviour — F5
    // ============================================================

    [Fact]
    public void DualKey_Verify_With_Current_Key_Signature_Returns_VerifiedWithCurrent()
    {
        // Current = A, Previous = B (the OLD pre-rotation key).
        var svc = BuildDualKey(currentBase64: SecretA, previousBase64: SecretB);
        var body = "v1.post-rotation-payload";

        var sig = svc.Sign(body); // Signed with current = A
        var result = svc.Verify(body, sig);

        result.IsValid.Should().BeTrue();
        result.UsedPreviousKey.Should().BeFalse("a signature minted with the current key must NOT be flagged as previous-key use");
    }

    [Fact]
    public void DualKey_Verify_With_Previous_Key_Signature_Returns_VerifiedWithPrevious()
    {
        // Simulate a real rotation: the OLD service signed something with what's now the PREVIOUS key.
        var oldService = BuildSingleKey(SecretB);
        var legacySig = oldService.Sign("v1.pre-rotation-payload");

        // After rotation: current = A, previous = B (the value that just rotated out).
        var rotatedService = BuildDualKey(currentBase64: SecretA, previousBase64: SecretB);

        var result = rotatedService.Verify("v1.pre-rotation-payload", legacySig);

        result.IsValid.Should().BeTrue("the rotated service must accept signatures from the previous key during the grace window");
        result.UsedPreviousKey.Should().BeTrue("the audit log needs this flag to record that the QR was minted before the most-recent rotation");
    }

    [Fact]
    public void DualKey_Verify_With_Signature_From_Neither_Key_Returns_Invalid()
    {
        // A signature minted with SecretC — neither current (A) nor previous (B).
        var unrelatedService = BuildSingleKey(SecretC);
        var foreignSig = unrelatedService.Sign("v1.body");

        var rotatedService = BuildDualKey(currentBase64: SecretA, previousBase64: SecretB);

        var result = rotatedService.Verify("v1.body", foreignSig);

        result.IsValid.Should().BeFalse("a forged signature under an unrelated key must not validate");
        result.UsedPreviousKey.Should().BeFalse();
    }

    [Fact]
    public void DualKey_Sign_Always_Uses_Current_Key_Not_Previous()
    {
        // Even though previous = B is configured, Sign must produce a signature that
        // verifies under the CURRENT key — not the previous one. This is the security
        // invariant: stealing the rotated-out previous key cannot mint forward-valid QRs.
        var rotatedService = BuildDualKey(currentBase64: SecretA, previousBase64: SecretB);
        var body = "v1.what-key-am-i-signed-with";

        var sig = rotatedService.Sign(body);

        // Verify under a single-key service that only knows the current key (= A)
        // — should succeed, proving Sign used A.
        var currentOnly = BuildSingleKey(SecretA);
        currentOnly.Verify(body, sig).IsValid.Should().BeTrue();

        // And verify under a single-key service that only knows the previous key (= B)
        // — should FAIL, proving Sign did NOT use B.
        var previousOnly = BuildSingleKey(SecretB);
        previousOnly.Verify(body, sig).IsValid.Should().BeFalse(
            "Sign must use the current key only; the previous key is verify-only");
    }

    [Fact]
    public void DualKey_Construct_With_Both_Keys_Succeeds()
    {
        // Smoke test that the constructor accepts both configured keys without throwing.
        Action act = () =>
        {
            var _ = BuildDualKey(SecretA, SecretB);
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void DualKey_Construct_With_Previous_Key_Too_Short_Throws()
    {
        // The same length validation applies to BOTH keys — a sloppy too-short previous
        // is just as dangerous as a too-short current.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Tickets:QrSigningKey", SecretA),
                new KeyValuePair<string, string?>("Tickets:QrSigningKeyPrevious", "tooshort"), // 8 bytes
            })
            .Build();

        Action act = () => new HmacTicketSignatureService(config, NullLogger<HmacTicketSignatureService>.Instance);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*too short*");
    }
}
