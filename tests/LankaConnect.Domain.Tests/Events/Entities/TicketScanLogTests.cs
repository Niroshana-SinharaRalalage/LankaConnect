using FluentAssertions;
using LankaConnect.Domain.Events.Entities;
using Xunit;

namespace LankaConnect.Domain.Tests.Events.Entities;

/// <summary>
/// Phase 6A.141 — TicketScanLog entity factory tests.
///
/// Three factories: <see cref="TicketScanLog.Accepted"/>, <see cref="TicketScanLog.Rejected"/>,
/// <see cref="TicketScanLog.AdminUnmark"/>. Each enforces a different invariant set; tests
/// pin them down so callers can't bypass via constructor.
/// </summary>
public class TicketScanLogTests
{
    private readonly Guid _ticketId = Guid.NewGuid();
    private readonly Guid _eventId = Guid.NewGuid();
    private readonly Guid _scannerUserId = Guid.NewGuid();
    private const string _ticketCode = "LC-2026-TEST01";

    // ============================================================
    // Accepted factory
    // ============================================================

    [Fact]
    public void Accepted_SetsScanResultAcceptedAndAllFields()
    {
        var log = TicketScanLog.Accepted(
            _ticketId, _eventId, _ticketCode, _scannerUserId,
            scannerName: "Sarah Organizer",
            entryMethod: TicketScanLog.EntryMethodQr,
            verifiedWithPreviousKey: false,
            clientIp: "10.0.0.5",
            userAgent: "Mozilla/5.0");

        log.ScanResult.Should().Be(TicketScanLog.ScanResultAccepted);
        log.RejectionReason.Should().BeNull("accepted scans have no rejection reason");
        log.EntryMethod.Should().Be(TicketScanLog.EntryMethodQr);
        log.TicketId.Should().Be(_ticketId);
        log.EventId.Should().Be(_eventId);
        log.TicketCode.Should().Be(_ticketCode);
        log.ScannerUserId.Should().Be(_scannerUserId);
        log.ScannerName.Should().Be("Sarah Organizer");
        log.VerifiedWithPreviousKey.Should().BeFalse();
        log.ClientIp.Should().Be("10.0.0.5");
        log.UserAgent.Should().Be("Mozilla/5.0");
        log.Id.Should().NotBe(Guid.Empty);
        log.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Accepted_TracksVerifiedWithPreviousKey_True()
    {
        // The F5 audit flag — surfaces grace-window verifications so we can spot
        // when the previous key can safely be dropped.
        var log = TicketScanLog.Accepted(
            _ticketId, _eventId, _ticketCode, _scannerUserId,
            scannerName: null,
            entryMethod: TicketScanLog.EntryMethodQrLegacy,
            verifiedWithPreviousKey: true,
            clientIp: null,
            userAgent: null);

        log.VerifiedWithPreviousKey.Should().BeTrue();
    }

    [Fact]
    public void Accepted_WithBlankTicketCode_Throws()
    {
        Action act = () => TicketScanLog.Accepted(
            _ticketId, _eventId, ticketCode: "", _scannerUserId,
            scannerName: null,
            entryMethod: TicketScanLog.EntryMethodQr,
            verifiedWithPreviousKey: false,
            clientIp: null, userAgent: null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*ticketCode*");
    }

    [Fact]
    public void Accepted_WithUnknownEntryMethod_Throws()
    {
        Action act = () => TicketScanLog.Accepted(
            _ticketId, _eventId, _ticketCode, _scannerUserId,
            scannerName: null,
            entryMethod: "spaceship",
            verifiedWithPreviousKey: false,
            clientIp: null, userAgent: null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*entryMethod*");
    }

    // ============================================================
    // Rejected factory
    // ============================================================

    [Fact]
    public void Rejected_SetsScanResultRejectedAndReason()
    {
        var log = TicketScanLog.Rejected(
            ticketId: _ticketId,
            eventId: _eventId,
            ticketCode: _ticketCode,
            scannerUserId: _scannerUserId,
            scannerName: "Sam Staff",
            rejectionReason: TicketScanLog.ReasonAlreadyScanned,
            entryMethod: TicketScanLog.EntryMethodQr,
            verifiedWithPreviousKey: false,
            clientIp: "10.0.0.5",
            userAgent: null);

        log.ScanResult.Should().Be(TicketScanLog.ScanResultRejected);
        log.RejectionReason.Should().Be(TicketScanLog.ReasonAlreadyScanned);
    }

    [Fact]
    public void Rejected_FromInvalidSignature_PermitsNullTicketIdAndCode()
    {
        // Failed-signature scans have no resolved ticket — only an event in the URL
        // path and the operator who tried. Audit row should still record the attempt
        // so forgery sprees are visible.
        var log = TicketScanLog.Rejected(
            ticketId: null,
            eventId: _eventId,
            ticketCode: null,
            scannerUserId: _scannerUserId,
            scannerName: null,
            rejectionReason: TicketScanLog.ReasonInvalidSignature,
            entryMethod: TicketScanLog.EntryMethodQr,
            verifiedWithPreviousKey: false,
            clientIp: "10.0.0.5",
            userAgent: null);

        log.TicketId.Should().BeNull();
        log.TicketCode.Should().BeNull();
        log.RejectionReason.Should().Be(TicketScanLog.ReasonInvalidSignature);
    }

    [Fact]
    public void Rejected_WithBlankReason_Throws()
    {
        Action act = () => TicketScanLog.Rejected(
            ticketId: _ticketId,
            eventId: _eventId,
            ticketCode: _ticketCode,
            scannerUserId: _scannerUserId,
            scannerName: null,
            rejectionReason: "",
            entryMethod: TicketScanLog.EntryMethodQr,
            verifiedWithPreviousKey: false,
            clientIp: null, userAgent: null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*rejectionReason*");
    }

    // ============================================================
    // AdminUnmark factory
    // ============================================================

    [Fact]
    public void AdminUnmark_SetsScanResultUnmarkedAndCarriesReason()
    {
        var log = TicketScanLog.AdminUnmark(
            ticketId: _ticketId,
            eventId: _eventId,
            ticketCode: _ticketCode,
            scannerUserId: _scannerUserId,
            scannerName: "Admin User",
            reason: "Accidental scan during testing; user has not entered yet.",
            clientIp: null,
            userAgent: null);

        log.ScanResult.Should().Be(TicketScanLog.ScanResultUnmarked);
        log.EntryMethod.Should().Be(TicketScanLog.EntryMethodAdminUnmark);
        log.RejectionReason.Should().Contain("Accidental");
    }

    [Fact]
    public void AdminUnmark_WithBlankReason_Throws()
    {
        Action act = () => TicketScanLog.AdminUnmark(
            ticketId: _ticketId,
            eventId: _eventId,
            ticketCode: _ticketCode,
            scannerUserId: _scannerUserId,
            scannerName: null,
            reason: "",
            clientIp: null, userAgent: null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*reason*");
    }
}
