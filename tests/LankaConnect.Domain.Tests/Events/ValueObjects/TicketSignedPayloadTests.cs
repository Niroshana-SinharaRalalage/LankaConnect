using FluentAssertions;
using LankaConnect.Domain.Events.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Events.ValueObjects;

/// <summary>
/// Phase 6A.141 — Paid-Event Ticket Check-in (QR Scanner) end-to-end.
///
/// <see cref="TicketSignedPayload"/> is the on-the-wire format for the QR code carried
/// on a paid-event ticket. Two formats are supported:
///
///   v1 (signed)  : "v1." + base64url(body) + "." + base64url(signature)
///                  body = "ticketCode|eventId|registrationId|iatUnixSeconds"
///                  signature = HMAC-SHA256(secret, "v1." + base64url(body))
///
///   legacy       : pure base64 of "ticketCode|eventId|registrationId" — the pre-141 shape.
///                  Kept readable for backward-compatibility during the grace window. The
///                  scanner endpoint will mark these scans with a "legacy" reason flag in
///                  the audit log but still accept them while real attendees are walking
///                  around with already-printed PDFs.
///
/// The VO has no I/O or signing capability of its own — it only encodes and decodes the
/// structure. Signing + verification live in the application/infrastructure
/// (<c>ITicketSignatureService</c>) so the secret lookup is testable and rotatable.
/// </summary>
public class TicketSignedPayloadTests
{
    private static readonly string _ticketCode = "LC-2026-EREJH9";
    private static readonly Guid _eventId = Guid.Parse("d543629f-a5ba-4475-b124-3d0fc5200f2f");
    private static readonly Guid _registrationId = Guid.Parse("ffeaa5e8-ab5d-4039-b3c4-86dced0ca862");
    private static readonly long _iat = 1747169000L;
    private static readonly byte[] _fakeSignature = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x00, 0x11 };

    // ============================================================
    // Encoding
    // ============================================================

    [Fact]
    public void Encode_v1_Returns_ThreePart_DotSeparated_String()
    {
        var payload = TicketSignedPayload.CreateV1(_ticketCode, _eventId, _registrationId, _iat);

        var encoded = payload.EncodeWithSignature(_fakeSignature);

        encoded.Should().StartWith("v1.");
        encoded.Split('.').Should().HaveCount(3, "v1 format is 'v1.<base64url(body)>.<base64url(sig)>'");
    }

    [Fact]
    public void Encode_v1_BodyToSign_Is_Stable_For_Same_Inputs()
    {
        var payload1 = TicketSignedPayload.CreateV1(_ticketCode, _eventId, _registrationId, _iat);
        var payload2 = TicketSignedPayload.CreateV1(_ticketCode, _eventId, _registrationId, _iat);

        payload1.BodyToSign.Should().Be(payload2.BodyToSign,
            "two payloads with identical inputs must produce identical bytes-to-sign — otherwise HMAC verification will fail intermittently");
    }

    [Fact]
    public void Encode_v1_BodyToSign_Contains_VersionPrefix()
    {
        var payload = TicketSignedPayload.CreateV1(_ticketCode, _eventId, _registrationId, _iat);

        payload.BodyToSign.Should().StartWith("v1.",
            "the signed bytes must include the version so a v1 signature can't be replayed against a hypothetical v2 verifier");
    }

    // ============================================================
    // Roundtrip — v1 signed
    // ============================================================

    [Fact]
    public void TryParse_RoundTrip_v1_Recovers_All_Fields()
    {
        var original = TicketSignedPayload.CreateV1(_ticketCode, _eventId, _registrationId, _iat);
        var encoded = original.EncodeWithSignature(_fakeSignature);

        var parsed = TicketSignedPayload.TryParse(encoded);

        parsed.Should().NotBeNull();
        parsed!.Version.Should().Be(TicketSignedPayload.PayloadVersion.V1);
        parsed.TicketCode.Should().Be(_ticketCode);
        parsed.EventId.Should().Be(_eventId);
        parsed.RegistrationId.Should().Be(_registrationId);
        parsed.IssuedAtUnixSeconds.Should().Be(_iat);
        parsed.Signature.Should().BeEquivalentTo(_fakeSignature);
        parsed.BodyToSign.Should().Be(original.BodyToSign);
    }

    [Fact]
    public void TryParse_v1_With_Tampered_Body_Still_Returns_Payload_With_Mismatched_Signature()
    {
        // The VO does NOT verify the HMAC — that's the signature service's job.
        // But TryParse must still expose the (now-tampered) body and the (now-mismatched)
        // original signature so the signature service can compute "expected vs got" and
        // reject. Confirm the bytes survive the round-trip intact.
        var original = TicketSignedPayload.CreateV1(_ticketCode, _eventId, _registrationId, _iat);
        var encoded = original.EncodeWithSignature(_fakeSignature);

        // Tamper: change one byte in the body segment.
        var parts = encoded.Split('.');
        var bodyBytes = Convert.FromBase64String(parts[1].Replace('-', '+').Replace('_', '/').PadRight((parts[1].Length + 3) / 4 * 4, '='));
        bodyBytes[0] ^= 0x01;
        parts[1] = Convert.ToBase64String(bodyBytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var tampered = string.Join('.', parts);

        var parsed = TicketSignedPayload.TryParse(tampered);

        parsed.Should().NotBeNull("the VO must still parse format-valid input — verification happens later");
        parsed!.Signature.Should().BeEquivalentTo(_fakeSignature,
            "the original signature bytes are preserved so the verifier can compute a mismatch");
    }

    // ============================================================
    // Legacy format
    // ============================================================

    [Fact]
    public void TryParse_Legacy_Base64_Format_Returns_Legacy_Variant()
    {
        // Pre-Phase-141 format used by every existing prod ticket — no version, no signature.
        var legacyBody = $"{_ticketCode}|{_eventId}|{_registrationId}";
        var legacyEncoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(legacyBody));

        var parsed = TicketSignedPayload.TryParse(legacyEncoded);

        parsed.Should().NotBeNull();
        parsed!.Version.Should().Be(TicketSignedPayload.PayloadVersion.Legacy);
        parsed.TicketCode.Should().Be(_ticketCode);
        parsed.EventId.Should().Be(_eventId);
        parsed.RegistrationId.Should().Be(_registrationId);
        parsed.Signature.Should().BeEmpty("legacy payloads have no signature — the verifier must treat them with reduced trust");
    }

    // ============================================================
    // Failure cases — malformed input
    // ============================================================

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("this-is-not-base64-and-not-versioned-either")]
    [InlineData("v1.")]                                 // missing body + sig
    [InlineData("v1.body-only")]                        // missing sig segment
    [InlineData("v1.a.b.c")]                            // too many segments
    [InlineData("v99.aaaa.bbbb")]                       // unknown version
    [InlineData("v1.!!!notbase64!!!.bbbb")]             // body not valid base64url
    [InlineData("v1.aaaa.!!!notbase64!!!")]             // sig not valid base64url
    public void TryParse_With_Malformed_Input_Returns_Null(string input)
    {
        var parsed = TicketSignedPayload.TryParse(input);

        parsed.Should().BeNull();
    }

    [Fact]
    public void TryParse_Null_Returns_Null()
    {
        TicketSignedPayload.TryParse(null!).Should().BeNull();
    }

    [Fact]
    public void TryParse_v1_Body_With_Wrong_Pipe_Count_Returns_Null()
    {
        // v1 format requires exactly 4 pipe-separated body fields. Anything else is invalid.
        var brokenBody = "ticketcode|onlytwofields";
        var bodyB64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(brokenBody))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var sigB64 = Convert.ToBase64String(_fakeSignature)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var encoded = $"v1.{bodyB64}.{sigB64}";

        var parsed = TicketSignedPayload.TryParse(encoded);

        parsed.Should().BeNull();
    }

    [Fact]
    public void TryParse_v1_Body_With_NonGuid_EventId_Returns_Null()
    {
        var brokenBody = $"{_ticketCode}|not-a-guid|{_registrationId}|{_iat}";
        var bodyB64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(brokenBody))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var sigB64 = Convert.ToBase64String(_fakeSignature)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var encoded = $"v1.{bodyB64}.{sigB64}";

        var parsed = TicketSignedPayload.TryParse(encoded);

        parsed.Should().BeNull();
    }

    [Fact]
    public void TryParse_v1_Body_With_NonNumeric_Iat_Returns_Null()
    {
        var brokenBody = $"{_ticketCode}|{_eventId}|{_registrationId}|not-a-number";
        var bodyB64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(brokenBody))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var sigB64 = Convert.ToBase64String(_fakeSignature)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var encoded = $"v1.{bodyB64}.{sigB64}";

        var parsed = TicketSignedPayload.TryParse(encoded);

        parsed.Should().BeNull();
    }
}
