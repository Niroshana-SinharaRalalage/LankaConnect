using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.ScanTicket;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Events.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Phase 6A.141 — ScanTicketCommandHandler unit tests.
///
/// Covers the 5 most-distinct paths via mocks; the remaining reason codes + the
/// race-condition path are exercised end-to-end in <c>LankaConnect.IntegrationTests</c>
/// (Phase F) against a real Postgres so the EF Core <c>ExecuteUpdateAsync</c> + explicit
/// transaction semantics are verified for real.
/// </summary>
public class ScanTicketCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<ITicketScanLogRepository> _scanLogRepo = new();
    private readonly Mock<IEventRepository> _eventRepo = new();
    private readonly Mock<IRegistrationRepository> _registrationRepo = new();
    private readonly Mock<ITicketSignatureService> _sigService = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private readonly Guid _eventId = Guid.NewGuid();
    private readonly Guid _scannerUserId = Guid.NewGuid();
    private readonly Guid _registrationId = Guid.NewGuid();
    private readonly DateTime _eventEndDate = DateTime.UtcNow.AddDays(7);
    private const string _ticketCode = "LC-2026-SCAN01";

    private ScanTicketCommandHandler BuildHandler() =>
        new(
            _ticketRepo.Object,
            _scanLogRepo.Object,
            _eventRepo.Object,
            _registrationRepo.Object,
            _sigService.Object,
            _uow.Object,
            NullLogger<ScanTicketCommandHandler>.Instance);

    private Event BuildEvent(Guid eventId, string title = "Test Event", Guid? organizerId = null)
    {
        // Default to _scannerUserId as the organizer so the handler's auth check passes
        // in the happy-path tests. Tests that exercise the forbidden path can pass an
        // explicit organizerId.
        var t = LankaConnect.Domain.Events.ValueObjects.EventTitle.Create(title).Value;
        var d = LankaConnect.Domain.Events.ValueObjects.EventDescription.Create("desc").Value;
        var ev = Event.Create(t, d, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2),
            organizerId: organizerId ?? _scannerUserId, capacity: 100).Value;
        typeof(BaseEntity).GetProperty("Id")!.SetValue(ev, eventId);
        return ev;
    }

    private Ticket BuildTicket(Guid eventId, string code, string qrCodeData)
    {
        // Caller-supplied code + qrCodeData (Phase 6A.141 path)
        var t = Ticket.Create(_registrationId, eventId, userId: null, _eventEndDate,
            ticketCode: code, qrCodeData: qrCodeData).Value;
        return t;
    }

    private string BuildSignedQrPayload(Guid eventId, Guid registrationId, string ticketCode)
    {
        // Build a v1 signed payload using the mocked signature service. The mock will
        // return a synthetic signature; the handler will call Verify() which the same
        // mock validates as true.
        var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = TicketSignedPayload.CreateV1(ticketCode, eventId, registrationId, iat);
        var signature = new byte[32]; // synthetic; signed and verified by mock
        Array.Fill(signature, (byte)0xAB);
        _sigService.Setup(s => s.Sign(payload.BodyToSign)).Returns(signature);
        _sigService.Setup(s => s.Verify(payload.BodyToSign, It.IsAny<byte[]>()))
            .Returns(new TicketSignatureVerifyResult(true, false));
        return payload.EncodeWithSignature(signature);
    }

    // ============================================================
    // 1) Happy path — valid v1 signed QR → accepted, audit row written
    // ============================================================
    [Fact]
    public async Task Handle_ValidV1Qr_MarksScanned_ReturnsAccepted()
    {
        var qrPayload = BuildSignedQrPayload(_eventId, _registrationId, _ticketCode);
        var ticket = BuildTicket(_eventId, _ticketCode, qrPayload);

        _eventRepo.Setup(r => r.GetByIdAsync(_eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildEvent(_eventId));
        _ticketRepo.Setup(r => r.GetByTicketCodeAsync(_ticketCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        _ticketRepo.Setup(r => r.TryMarkScannedAsync(ticket.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // race-winner
        _registrationRepo.Setup(r => r.GetByIdAsync(_registrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Registration?)null); // attendee details omitted is fine for the response

        var handler = BuildHandler();
        var result = await handler.Handle(new ScanTicketCommand(
            _eventId, _scannerUserId, "Sarah Organizer",
            QrPayload: qrPayload, TicketCode: null,
            ClientIp: "10.0.0.5", UserAgent: "Mozilla"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Result.Should().Be("accepted");
        result.Value.TicketCode.Should().Be(_ticketCode);
        result.Value.ScannedBy.Should().Be("Sarah Organizer");
        result.Value.UsedPreviousKey.Should().BeFalse();
        // Issue 1 UAT hotfix: the handler no longer wraps mark-scanned + audit in an
        // explicit transaction. The atomic UPDATE provides the race-safety guarantee
        // standalone; the audit insert runs in a separate CommitAsync. Trade-off: a
        // forensic gap on partial DB failure (UPDATE succeeded but audit insert failed)
        // — acceptable per architect review since the door correctly opens regardless.
        _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never,
            "transaction wrapper dropped to avoid the AppDbContext.CommitAsync (Phase 6A.74) " +
            "× EF Core 8 ExecuteUpdateAsync InvalidOperationException at runtime");
        _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once,
            "audit row is written via a single CommitAsync after the atomic UPDATE succeeded");
        _scanLogRepo.Verify(s => s.AddAsync(
            It.Is<TicketScanLog>(l => l.ScanResult == TicketScanLog.ScanResultAccepted),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ============================================================
    // 2) Invalid signature → rejected
    // ============================================================
    [Fact]
    public async Task Handle_InvalidSignature_Rejects()
    {
        var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = TicketSignedPayload.CreateV1(_ticketCode, _eventId, _registrationId, iat);
        var tamperedSig = new byte[32];
        Array.Fill(tamperedSig, (byte)0xFF);
        var qrPayload = payload.EncodeWithSignature(tamperedSig);

        _eventRepo.Setup(r => r.GetByIdAsync(_eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildEvent(_eventId));
        _sigService.Setup(s => s.Verify(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Returns(TicketSignatureVerifyResult.Invalid);

        var handler = BuildHandler();
        var result = await handler.Handle(new ScanTicketCommand(
            _eventId, _scannerUserId, "Sarah", qrPayload, null, "10.0.0.5", null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Result.Should().Be("rejected");
        result.Value.Reason.Should().Be(ReasonCode.InvalidSignature);
        _ticketRepo.Verify(r => r.GetByTicketCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never,
            "invalid signature must short-circuit BEFORE DB lookup");
        _ticketRepo.Verify(r => r.TryMarkScannedAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        _scanLogRepo.Verify(s => s.AddAsync(
            It.Is<TicketScanLog>(l => l.RejectionReason == ReasonCode.InvalidSignature),
            It.IsAny<CancellationToken>()), Times.Once,
            "forgery attempts must be audited even though they fail outside any transaction");
    }

    // ============================================================
    // 3) Ticket not found → rejected
    // ============================================================
    [Fact]
    public async Task Handle_TicketNotFound_Rejects()
    {
        _eventRepo.Setup(r => r.GetByIdAsync(_eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildEvent(_eventId));
        _ticketRepo.Setup(r => r.GetByTicketCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        var handler = BuildHandler();
        var result = await handler.Handle(new ScanTicketCommand(
            _eventId, _scannerUserId, "Sarah",
            QrPayload: null, TicketCode: "LC-2026-NOPE99",
            ClientIp: null, UserAgent: null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Result.Should().Be("rejected");
        result.Value.Reason.Should().Be(ReasonCode.TicketNotFound);
        _ticketRepo.Verify(r => r.TryMarkScannedAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ============================================================
    // 4) Wrong event → rejected, response includes the actual event title
    // ============================================================
    [Fact]
    public async Task Handle_WrongEvent_Rejects_WithWrongEventTitle()
    {
        var realTicketEventId = Guid.NewGuid();
        var qrPayload = BuildSignedQrPayload(realTicketEventId, _registrationId, _ticketCode);
        var ticket = BuildTicket(realTicketEventId, _ticketCode, qrPayload);

        _eventRepo.Setup(r => r.GetByIdAsync(_eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildEvent(_eventId, "Scanned-At Event"));
        _eventRepo.Setup(r => r.GetByIdAsync(realTicketEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildEvent(realTicketEventId, "Spring Gala 2026"));
        _ticketRepo.Setup(r => r.GetByTicketCodeAsync(_ticketCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var handler = BuildHandler();
        var result = await handler.Handle(new ScanTicketCommand(
            _eventId, _scannerUserId, "Sarah", qrPayload, null, null, null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Result.Should().Be("rejected");
        result.Value.Reason.Should().Be(ReasonCode.WrongEvent);
        result.Value.WrongEventTitle.Should().Be("Spring Gala 2026");
        result.Value.ReasonMessage.Should().Contain("Spring Gala 2026",
            "staff need to see the actual event title to redirect the attendee");
    }

    // ============================================================
    // 5a) UAT R2 Issue A — already_scanned (synchronous path) returns enriched
    //     attendee details + scan history (PreviousScanCount, PreviousScannedBy)
    // ============================================================
    [Fact]
    public async Task Handle_AlreadyScanned_Rejects_WithEnrichedAttendeeAndScanHistory()
    {
        var qrPayload = BuildSignedQrPayload(_eventId, _registrationId, _ticketCode);
        var ticket = BuildTicket(_eventId, _ticketCode, qrPayload);
        ticket.Validate(); // sets ValidatedAt — handler hits the synchronous already-scanned branch

        var originalScanAt = ticket.ValidatedAt;

        _eventRepo.Setup(r => r.GetByIdAsync(_eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildEvent(_eventId));
        _ticketRepo.Setup(r => r.GetByTicketCodeAsync(_ticketCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        _scanLogRepo.Setup(r => r.GetAcceptedSummaryForTicketAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcceptedCount: 1, LastScannerName: "Lakmal Silva", LastAcceptedAt: originalScanAt));

        var handler = BuildHandler();
        var result = await handler.Handle(new ScanTicketCommand(
            _eventId, _scannerUserId, "Sarah", qrPayload, null, null, null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Result.Should().Be("rejected");
        result.Value.Reason.Should().Be(ReasonCode.AlreadyScanned);
        result.Value.PreviousScanCount.Should().Be(1, "enrichment must surface accepted-count to the operator");
        result.Value.PreviousScannedBy.Should().Be("Lakmal Silva", "operator wants to see who originally admitted the attendee");
        result.Value.ScannedAt.Should().Be(originalScanAt, "amber panel needs the ORIGINAL accepted-at timestamp");
        result.Value.TicketCode.Should().Be(_ticketCode, "ticket code echoed so the operator can cross-check");
    }

    // ============================================================
    // 6) Race-lost (TryMarkScannedAsync returns 0) → rejected as already_scanned
    // ============================================================
    [Fact]
    public async Task Handle_RaceLost_Rejects_AsAlreadyScanned()
    {
        var qrPayload = BuildSignedQrPayload(_eventId, _registrationId, _ticketCode);
        var ticket = BuildTicket(_eventId, _ticketCode, qrPayload);

        _eventRepo.Setup(r => r.GetByIdAsync(_eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildEvent(_eventId));
        _ticketRepo.Setup(r => r.GetByTicketCodeAsync(_ticketCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        _ticketRepo.Setup(r => r.TryMarkScannedAsync(ticket.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0); // race-loser — someone else scanned between our read and our UPDATE

        var handler = BuildHandler();
        var result = await handler.Handle(new ScanTicketCommand(
            _eventId, _scannerUserId, "Sarah", qrPayload, null, null, null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Result.Should().Be("rejected");
        result.Value.Reason.Should().Be(ReasonCode.AlreadyScanned);
        // Issue 1 UAT hotfix: no transaction wrapper anymore — nothing to roll back.
        // The race-loser path is detected by ExecuteUpdateAsync returning RowsAffected==0;
        // no UPDATE happened, so no audit binding to the no-op update is needed.
        _uow.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _scanLogRepo.Verify(s => s.AddAsync(
            It.Is<TicketScanLog>(l => l.RejectionReason == ReasonCode.AlreadyScanned),
            It.IsAny<CancellationToken>()), Times.Once,
            "race-loser still gets its own audit row via TryWriteRejectionAuditAsync");
    }
}
