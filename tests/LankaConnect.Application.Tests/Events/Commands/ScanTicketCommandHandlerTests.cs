using System.Reflection;
using FluentAssertions;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Commands.ScanTicket;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.BuildingBlocks.Domain.Shared.Enums;
using LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects;
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
    private readonly Mock<IAddOnPurchaseRepository> _addOnPurchaseRepo = new();
    private readonly Mock<IAddOnDefinitionRepository> _addOnDefinitionRepo = new();
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
            _addOnPurchaseRepo.Object,
            _addOnDefinitionRepo.Object,
            _sigService.Object,
            _uow.Object,
            NullLogger<ScanTicketCommandHandler>.Instance);

    private Event BuildEvent(Guid eventId, string title = "Test Event", Guid? organizerId = null)
    {
        // Default to _scannerUserId as the organizer so the handler's auth check passes
        // in the happy-path tests. Tests that exercise the forbidden path can pass an
        // explicit organizerId.
        var t = LankaConnect.Products.LankaEvents.Domain.ValueObjects.EventTitle.Create(title).Value;
        var d = LankaConnect.Products.LankaEvents.Domain.ValueObjects.EventDescription.Create("desc").Value;
        var ev = Event.Create(t, d, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2),
            organizerId: organizerId ?? _scannerUserId, capacity: 100).Value;
        typeof(LegacyBaseEntity).GetProperty("Id")!.SetValue(ev, eventId);
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
    // UAT R3 helpers — build Event-with-VIP-tier and Registration-with-attendees
    // so the new per-attendee projection can be exercised end-to-end through the
    // handler. The domain factories require a fully-formed value-object graph, so
    // we set up a real (not Mocked) Event + Registration here.
    // ============================================================

    /// <summary>Returns an Event with a single VIP TicketTier injected via reflection
    /// (AddTicketTier requires TicketingMode=Tiered which adds setup noise — direct
    /// list push is cleaner for the projection test).</summary>
    private (Event ev, TicketTier vipTier) BuildEventWithVipTier(
        Guid eventId, decimal adultPrice = 100m, decimal childPrice = 50m)
    {
        var ev = BuildEvent(eventId);
        var adult = Money.Create(adultPrice, Currency.USD).Value;
        var child = Money.Create(childPrice, Currency.USD).Value;
        var tier = TicketTier.Create(eventId, "VIP", "VIP access",
            adultPrice: adult, childPrice: child, childAgeLimit: 12,
            capacity: 100, maxPerUser: 10, sortOrder: 1).Value;
        typeof(LegacyBaseEntity).GetProperty("Id")!.SetValue(tier, Guid.NewGuid());
        var tiersField = typeof(Event).GetField("_ticketTiers",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var list = (List<TicketTier>)tiersField!.GetValue(ev)!;
        list.Add(tier);
        return (ev, tier);
    }

    /// <summary>Builds a 2-attendee Registration (Adult VIP + Child VIP) tied to the
    /// given event and tier. Mirrors the printed-ticket data shape from UAT R3.</summary>
    private Registration BuildRegistrationWith2Attendees(Guid eventId, TicketTier vipTier)
    {
        var att1 = AttendeeDetails.Create("Niroshana Sinharage", AgeCategory.Adult,
            gender: Gender.Male, ticketTierId: vipTier.Id, ticketTierName: "VIP").Value;
        var att2 = AttendeeDetails.Create("Navya Sinharage", AgeCategory.Child,
            gender: Gender.Female, ticketTierId: vipTier.Id, ticketTierName: "VIP").Value;
        var contact = RegistrationContact.Create("test@example.com", "+15551234567", null).Value;
        var total = Money.Create(150m, Currency.USD).Value;
        var reg = Registration.CreateWithAttendees(eventId, _scannerUserId,
            new[] { att1, att2 }, contact, total, isPaidEvent: true).Value;
        typeof(LegacyBaseEntity).GetProperty("Id")!.SetValue(reg, _registrationId);
        return reg;
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

    // ============================================================
    // 7) UAT R3 — Accepted with multi-attendee registration projects the
    //    full per-attendee list with names, age, gender, tier, and prices
    // ============================================================
    [Fact]
    public async Task Handle_Accepted_MultiAttendee_ProjectsAllAttendeesWithPrices()
    {
        var qrPayload = BuildSignedQrPayload(_eventId, _registrationId, _ticketCode);
        var ticket = BuildTicket(_eventId, _ticketCode, qrPayload);
        var (ev, vipTier) = BuildEventWithVipTier(_eventId, adultPrice: 100m, childPrice: 50m);
        var registration = BuildRegistrationWith2Attendees(_eventId, vipTier);

        _eventRepo.Setup(r => r.GetByIdAsync(_eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ev);
        _ticketRepo.Setup(r => r.GetByTicketCodeAsync(_ticketCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        _ticketRepo.Setup(r => r.TryMarkScannedAsync(ticket.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _registrationRepo.Setup(r => r.GetByIdAsync(_registrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);

        var handler = BuildHandler();
        var result = await handler.Handle(new ScanTicketCommand(
            _eventId, _scannerUserId, "Sarah Organizer", qrPayload, null, null, null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Result.Should().Be("accepted");
        result.Value.Attendees.Should().NotBeNull();
        result.Value.Attendees!.Should().HaveCount(2, "registration has 2 attendees on the printed ticket");

        var adultAttendee = result.Value.Attendees!.Single(a => a.AgeCategory == "Adult");
        adultAttendee.Name.Should().Be("Niroshana Sinharage");
        adultAttendee.Gender.Should().Be("Male");
        adultAttendee.TicketTierName.Should().Be("VIP");
        adultAttendee.PriceAmount.Should().Be(100m, "adult VIP price from TicketTier.CalculatePriceForAttendee");
        adultAttendee.PriceCurrency.Should().Be("USD");

        var childAttendee = result.Value.Attendees!.Single(a => a.AgeCategory == "Child");
        childAttendee.Name.Should().Be("Navya Sinharage");
        childAttendee.Gender.Should().Be("Female");
        childAttendee.TicketTierName.Should().Be("VIP");
        childAttendee.PriceAmount.Should().Be(50m, "child VIP price via HasChildPricing branch");
        childAttendee.PriceCurrency.Should().Be("USD");

        // Backward compat: legacy aggregates still populated for fallback rendering.
        result.Value.AttendeeName.Should().Be("Niroshana Sinharage");
        result.Value.Tier.Should().Be("VIP");
        result.Value.AttendeeCount.Should().Be(2);
        result.Value.TierBreakdown.Should().NotBeNullOrEmpty();
    }

    // ============================================================
    // 8) UAT R3 — Already-scanned rejection projects the SAME per-attendee
    //    list (proves accepted + rejected paths share BuildAttendeeDetails)
    // ============================================================
    [Fact]
    public async Task Handle_AlreadyScanned_Rejects_ProjectsAttendeesIdenticallyToAcceptedPath()
    {
        var qrPayload = BuildSignedQrPayload(_eventId, _registrationId, _ticketCode);
        var ticket = BuildTicket(_eventId, _ticketCode, qrPayload);
        ticket.Validate(); // sets ValidatedAt — sync already-scanned branch fires
        var (ev, vipTier) = BuildEventWithVipTier(_eventId);
        var registration = BuildRegistrationWith2Attendees(_eventId, vipTier);

        _eventRepo.Setup(r => r.GetByIdAsync(_eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ev);
        _ticketRepo.Setup(r => r.GetByTicketCodeAsync(_ticketCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        _registrationRepo.Setup(r => r.GetByIdAsync(_registrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);
        _scanLogRepo.Setup(r => r.GetAcceptedSummaryForTicketAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcceptedCount: 1, LastScannerName: "Lakmal Silva", LastAcceptedAt: ticket.ValidatedAt));

        var handler = BuildHandler();
        var result = await handler.Handle(new ScanTicketCommand(
            _eventId, _scannerUserId, "Sarah", qrPayload, null, null, null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Result.Should().Be("rejected");
        result.Value.Reason.Should().Be(ReasonCode.AlreadyScanned);
        result.Value.Attendees.Should().NotBeNull();
        result.Value.Attendees!.Should().HaveCount(2,
            "rejection panel must show every attendee on the ticket, identical to accepted path");
        result.Value.Attendees!.Should().Contain(a => a.Name == "Niroshana Sinharage" && a.AgeCategory == "Adult");
        result.Value.Attendees!.Should().Contain(a => a.Name == "Navya Sinharage" && a.AgeCategory == "Child");
        result.Value.PreviousScanCount.Should().Be(1);
        result.Value.PreviousScannedBy.Should().Be("Lakmal Silva");
    }

    // ============================================================
    // 9) UAT R3 — Head-count-mode registration (no Attendees rows) leaves
    //    the Attendees DTO field null so UI falls back to aggregates
    // ============================================================
    [Fact]
    public async Task Handle_Accepted_HeadCount_AttendeesIsNullForFallback()
    {
        var qrPayload = BuildSignedQrPayload(_eventId, _registrationId, _ticketCode);
        var ticket = BuildTicket(_eventId, _ticketCode, qrPayload);

        _eventRepo.Setup(r => r.GetByIdAsync(_eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildEvent(_eventId));
        _ticketRepo.Setup(r => r.GetByTicketCodeAsync(_ticketCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        _ticketRepo.Setup(r => r.TryMarkScannedAsync(ticket.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        // Registration intentionally null — simulates pre-MultiAttendee data or a
        // head-count-mode registration that has no Attendees collection populated.
        _registrationRepo.Setup(r => r.GetByIdAsync(_registrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Registration?)null);

        var handler = BuildHandler();
        var result = await handler.Handle(new ScanTicketCommand(
            _eventId, _scannerUserId, "Sarah", qrPayload, null, null, null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Result.Should().Be("accepted");
        result.Value.Attendees.Should().BeNull(
            "head-count tickets must fall through to legacy aggregate display in the UI");
    }

    // ============================================================
    // 10) UAT R3 — Attendee whose TicketTierId doesn't match any tier on
    //     the event (data drift after tier deletion). Name still surfaces;
    //     price degrades to null without throwing.
    // ============================================================
    [Fact]
    public async Task Handle_Accepted_AttendeeWithMissingTier_PriceIsNullButNameRenders()
    {
        var qrPayload = BuildSignedQrPayload(_eventId, _registrationId, _ticketCode);
        var ticket = BuildTicket(_eventId, _ticketCode, qrPayload);
        var (ev, vipTier) = BuildEventWithVipTier(_eventId);

        // Build an attendee whose TicketTierId points at a tier that's NOT on the event.
        var orphanedTierId = Guid.NewGuid();
        var att = AttendeeDetails.Create("Orphan Tier Attendee", AgeCategory.Adult,
            gender: Gender.Other, ticketTierId: orphanedTierId, ticketTierName: "OldDeletedTier").Value;
        var contact = RegistrationContact.Create("orphan@example.com", "+15551234567", null).Value;
        var total = Money.Create(0m, Currency.USD).Value;
        var reg = Registration.CreateWithAttendees(_eventId, _scannerUserId,
            new[] { att }, contact, total, isPaidEvent: true).Value;
        typeof(LegacyBaseEntity).GetProperty("Id")!.SetValue(reg, _registrationId);

        _eventRepo.Setup(r => r.GetByIdAsync(_eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ev);
        _ticketRepo.Setup(r => r.GetByTicketCodeAsync(_ticketCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        _ticketRepo.Setup(r => r.TryMarkScannedAsync(ticket.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _registrationRepo.Setup(r => r.GetByIdAsync(_registrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reg);

        var handler = BuildHandler();
        var result = await handler.Handle(new ScanTicketCommand(
            _eventId, _scannerUserId, "Sarah", qrPayload, null, null, null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Result.Should().Be("accepted");
        result.Value.Attendees.Should().NotBeNull();
        result.Value.Attendees!.Single().Name.Should().Be("Orphan Tier Attendee",
            "name must still render even when the tier was deleted post-registration");
        result.Value.Attendees!.Single().PriceAmount.Should().BeNull(
            "missing tier → price degrades to null without throwing");
        result.Value.Attendees!.Single().PriceCurrency.Should().BeNull();
        result.Value.Attendees!.Single().TicketTierName.Should().Be("OldDeletedTier",
            "denormalized tier name on AttendeeDetails survives even when the live tier row is gone");
    }

    // ============================================================
    // 11) UAT R4 — Accepted scan surfaces bundled add-ons (Completed status,
    //     RegistrationId matches THIS registration). AddOnDefinition name resolved.
    // ============================================================
    [Fact]
    public async Task Handle_Accepted_WithBundledAddOn_ProjectsAddOnSummary()
    {
        var qrPayload = BuildSignedQrPayload(_eventId, _registrationId, _ticketCode);
        var ticket = BuildTicket(_eventId, _ticketCode, qrPayload);
        var (ev, vipTier) = BuildEventWithVipTier(_eventId);
        var registration = BuildRegistrationWith2Attendees(_eventId, vipTier);
        var addOnDefinitionId = Guid.NewGuid();
        // Build a Completed bundled add-on purchase tied to this registration.
        var unitPrice = Money.Create(5m, Currency.USD).Value;
        var purchase = AddOnPurchase.CreateBundledWithRegistration(
            eventId: _eventId,
            addOnDefinitionId: addOnDefinitionId,
            registrationId: _registrationId,
            buyerUserId: _scannerUserId,
            buyerName: "Niroshana Sinharage",
            buyerEmail: "niroshhh@example.com",
            buyerPhone: null,
            quantity: 1,
            unitPrice: unitPrice).Value;
        // Move it to Completed via the entity lifecycle so the filter accepts it.
        purchase.SetStripeCheckoutSession("cs_test_abc", DateTime.UtcNow.AddHours(1));
        purchase.CompletePayment("pi_test_xyz");
        // Build a matching AddOnDefinition so the name lookup succeeds.
        var defNameField = AddOnDefinition.Create(_eventId, "Dinner Add-on", "Extra", unitPrice, 100).Value;
        typeof(LegacyBaseEntity).GetProperty("Id")!.SetValue(defNameField, addOnDefinitionId);

        _eventRepo.Setup(r => r.GetByIdAsync(_eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ev);
        _ticketRepo.Setup(r => r.GetByTicketCodeAsync(_ticketCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        _ticketRepo.Setup(r => r.TryMarkScannedAsync(ticket.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _registrationRepo.Setup(r => r.GetByIdAsync(_registrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);
        _addOnPurchaseRepo.Setup(r => r.GetByUserIdAndEventIdAsync(_scannerUserId, _eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { purchase });
        _addOnDefinitionRepo.Setup(r => r.GetByEventIdAsync(_eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { defNameField });

        var handler = BuildHandler();
        var result = await handler.Handle(new ScanTicketCommand(
            _eventId, _scannerUserId, "Sarah", qrPayload, null, null, null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Result.Should().Be("accepted");
        result.Value.AddOns.Should().NotBeNull();
        result.Value.AddOns!.Should().HaveCount(1, "exactly one bundled add-on on this registration");
        var addOn = result.Value.AddOns!.Single();
        addOn.Name.Should().Be("Dinner Add-on", "definition name resolved via IAddOnDefinitionRepository");
        addOn.Quantity.Should().Be(1);
        addOn.UnitPrice.Should().Be(5m);
        addOn.TotalAmount.Should().Be(5m);
        addOn.Currency.Should().Be("USD");
    }

    // ============================================================
    // 12) UAT R4 — No add-ons → AddOns is null (UI omits the section).
    //     Also pins that non-bundled add-ons (different RegistrationId or
    //     Pending status) are filtered out.
    // ============================================================
    [Fact]
    public async Task Handle_Accepted_NoAddOns_AddOnsIsNull()
    {
        var qrPayload = BuildSignedQrPayload(_eventId, _registrationId, _ticketCode);
        var ticket = BuildTicket(_eventId, _ticketCode, qrPayload);

        _eventRepo.Setup(r => r.GetByIdAsync(_eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildEvent(_eventId));
        _ticketRepo.Setup(r => r.GetByTicketCodeAsync(_ticketCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        _ticketRepo.Setup(r => r.TryMarkScannedAsync(ticket.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _registrationRepo.Setup(r => r.GetByIdAsync(_registrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Registration?)null); // simulate no registration data
        // _addOnPurchaseRepo intentionally unmocked → default empty result

        var handler = BuildHandler();
        var result = await handler.Handle(new ScanTicketCommand(
            _eventId, _scannerUserId, "Sarah", qrPayload, null, null, null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Result.Should().Be("accepted");
        result.Value.AddOns.Should().BeNull("registration is null → no add-ons to enrich");
    }
}
