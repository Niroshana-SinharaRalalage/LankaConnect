using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Events.Entities;

/// <summary>
/// Phase 6A.141 — covers the new <c>ticketCode</c> + <c>qrCodeData</c> optional
/// parameters on <see cref="Ticket.Create"/> and <see cref="Ticket.CreateTiered"/>.
///
/// Two contracts under test:
///   1. <b>Backward compat (regression guard):</b> when both parameters are null, the
///      entity continues to auto-generate a ticket code and a legacy unsigned base64
///      QR payload — preserves Phase 6A.24 behaviour for unmigrated callers (test
///      suites that predate this phase).
///   2. <b>Phase 6A.141 forward path:</b> when a caller supplies the resolved ticketCode
///      (after DB-side collision resolution) and a v1 signed QR payload (built by
///      TicketService via ITicketSignatureService), the entity stores those values
///      verbatim — no double-encoding, no overwrite.
///
/// Plus one consistency invariant: a caller that supplies qrCodeData WITHOUT a
/// matching ticketCode is rejected, because the signature inside the payload binds
/// to a specific code and the entity must end up with that same code.
/// </summary>
public class Ticket_Phase6A141_CreateParamsTests
{
    private readonly Guid _registrationId = Guid.NewGuid();
    private readonly Guid _eventId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _eventEndDate = DateTime.UtcNow.AddDays(7);

    // ============================================================
    // Backward compat — Phase 6A.24 path still works
    // ============================================================

    [Fact]
    public void Create_WithBothParamsNull_AutoGenerates_TicketCode_And_LegacyQrCodeData()
    {
        var result = Ticket.Create(_registrationId, _eventId, _userId, _eventEndDate);

        result.IsSuccess.Should().BeTrue();
        var ticket = result.Value;

        ticket.TicketCode.Should().StartWith("LC-", "the auto-generated ticket code uses the legacy LC-YYYY-XXXXXX format");
        ticket.QrCodeData.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Create_WithBothParamsNull_QrCodeData_IsLegacyParseable()
    {
        // The fallback path must still produce a payload that the unified parser
        // (TicketSignedPayload.TryParse) understands as the LEGACY format.
        var result = Ticket.Create(_registrationId, _eventId, _userId, _eventEndDate);

        var parsed = TicketSignedPayload.TryParse(result.Value.QrCodeData);

        parsed.Should().NotBeNull("the legacy decoder must still parse pre-141 QRs");
        parsed!.Version.Should().Be(TicketSignedPayload.PayloadVersion.Legacy);
        parsed.TicketCode.Should().Be(result.Value.TicketCode);
        parsed.EventId.Should().Be(_eventId);
        parsed.RegistrationId.Should().Be(_registrationId);
    }

    // ============================================================
    // Phase 6A.141 forward path — caller-supplied code + signed payload
    // ============================================================

    [Fact]
    public void Create_WithExplicit_TicketCode_And_SignedQrCodeData_StoresVerbatim()
    {
        // Simulate what TicketService.GenerateTicketAsync will do post-refactor:
        // resolve a unique ticket code, build a v1 signed payload using that code,
        // pass both into Ticket.Create.
        var providedCode = "LC-2026-PHASEC1";
        var providedSignedPayload = "v1.dGVzdC1ib2R5.dGVzdC1zaWc"; // synthetic; signature service tests cover the real format

        var result = Ticket.Create(
            _registrationId, _eventId, _userId, _eventEndDate,
            ticketCode: providedCode,
            qrCodeData: providedSignedPayload);

        result.IsSuccess.Should().BeTrue();
        result.Value.TicketCode.Should().Be(providedCode, "caller-supplied code must not be overridden");
        result.Value.QrCodeData.Should().Be(providedSignedPayload, "caller-supplied payload must be stored verbatim — re-encoding would break the signature");
    }

    [Fact]
    public void Create_WithExplicitTicketCodeOnly_AutoGenerates_LegacyQrCodeData_AroundProvidedCode()
    {
        // Edge case: caller provides a code but not a payload. The fallback legacy
        // QR is generated around the CALLER'S code, not a freshly-rolled one.
        var providedCode = "LC-2026-PHASEC2";

        var result = Ticket.Create(
            _registrationId, _eventId, _userId, _eventEndDate,
            ticketCode: providedCode);

        result.IsSuccess.Should().BeTrue();
        result.Value.TicketCode.Should().Be(providedCode);

        var parsed = TicketSignedPayload.TryParse(result.Value.QrCodeData);
        parsed.Should().NotBeNull();
        parsed!.TicketCode.Should().Be(providedCode, "the legacy fallback must wrap the CALLER'S code, not a regenerated one");
    }

    // ============================================================
    // Invariant: qrCodeData without ticketCode is invalid
    // ============================================================

    [Fact]
    public void Create_WithQrCodeData_ButNullTicketCode_Returns_Failure()
    {
        var signedPayload = "v1.dGVzdC1ib2R5.dGVzdC1zaWc";

        var result = Ticket.Create(
            _registrationId, _eventId, _userId, _eventEndDate,
            ticketCode: null,
            qrCodeData: signedPayload);

        result.IsFailure.Should().BeTrue(
            "a signed payload binds to a specific code; without that code the entity could end up with a different auto-generated code and break the signature");
        result.Error.Should().Contain("ticketCode must be supplied");
    }

    // ============================================================
    // CreateTiered parity
    // ============================================================

    [Fact]
    public void CreateTiered_WithExplicit_TicketCode_And_SignedQrCodeData_StoresVerbatim()
    {
        var providedCode = "LC-2026-TIERED1";
        var providedSignedPayload = "v1.YW5vdGhlci10ZXN0LWJvZHk.YW5vdGhlci10ZXN0LXNpZw";

        var result = Ticket.CreateTiered(
            _registrationId, _eventId, _userId, _eventEndDate,
            ticketTierName: "VIP",
            ticketCategory: TicketCategory.Master,
            attendeeIndex: null,
            attendeeNames: "Sample Lead",
            ticketCode: providedCode,
            qrCodeData: providedSignedPayload);

        result.IsSuccess.Should().BeTrue();
        result.Value.TicketCode.Should().Be(providedCode);
        result.Value.QrCodeData.Should().Be(providedSignedPayload);
        result.Value.TicketTierName.Should().Be("VIP");
        result.Value.TicketCategory.Should().Be(TicketCategory.Master);
    }

    [Fact]
    public void CreateTiered_WithoutNewParams_Still_AutoGenerates_LegacyFormat()
    {
        // Regression: existing CreateTiered callers (the 8 dead-code tests in
        // TicketTieredTests.cs) pass nothing for the new params; behaviour must be
        // identical to Phase 6A.121.
        var result = Ticket.CreateTiered(
            _registrationId, _eventId, _userId, _eventEndDate,
            ticketTierName: "General",
            ticketCategory: TicketCategory.Individual,
            attendeeIndex: 2);

        result.IsSuccess.Should().BeTrue();
        result.Value.TicketCode.Should().StartWith("LC-");

        var parsed = TicketSignedPayload.TryParse(result.Value.QrCodeData);
        parsed.Should().NotBeNull();
        parsed!.Version.Should().Be(TicketSignedPayload.PayloadVersion.Legacy);
    }

    [Fact]
    public void GenerateTicketCode_IsPublic_ProducesLcYearXxxxxxFormat()
    {
        // Public static method TicketService relies on for external code generation
        // (so it can resolve collisions against the DB BEFORE building the signed payload).
        var code = Ticket.GenerateTicketCode();

        code.Should().NotBeNullOrWhiteSpace();
        code.Should().StartWith($"LC-{DateTime.UtcNow.Year}-");
        code.Length.Should().Be(14, "format is LC-YYYY-XXXXXX: 'LC-' (3) + 'YYYY' (4) + '-' (1) + 'XXXXXX' (6) = 14");
    }
}
