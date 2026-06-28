using System.Reflection;
using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.BatchUpdateLayout;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Slice 5 Chunk 10: PUT /api/venue-layouts/{id}/batch — atomic full-layout replacement.
/// Covers authorization, early + late concurrency, structural guard on removals, add/update/
/// remove diffs across zones/tables/decorations, and layout-level Name + Canvas updates.
/// </summary>
public class BatchUpdateLayoutCommandHandlerTests
{
    private readonly Mock<ILayoutAuthorizationService> _mockAuth = new();
    private readonly Mock<IStructuralEditGuard> _mockGuard = new();
    private readonly Mock<IVenueLayoutRepository> _mockLayoutRepo = new();
    private readonly Mock<IEventRepository> _mockEventRepo = new();
    private readonly Mock<IUnitOfWork> _mockUow = new();
    private readonly Mock<ILayoutMetrics> _mockMetrics = new();
    private readonly BatchUpdateLayoutCommandHandler _sut;

    public BatchUpdateLayoutCommandHandlerTests()
    {
        // S8.8c: default tier query returns no tiers — every existing test
        // that doesn't exercise tier reconciliation is unaffected because
        // `payload.TierAssignments` defaults to null and the reconciler is
        // skipped entirely.
        _mockEventRepo
            .Setup(r => r.GetTicketTiersWithAssignmentsForEventAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TicketTier>)Array.Empty<TicketTier>());

        _sut = new BatchUpdateLayoutCommandHandler(
            _mockAuth.Object,
            _mockGuard.Object,
            _mockLayoutRepo.Object,
            _mockEventRepo.Object,
            _mockUow.Object,
            _mockMetrics.Object,
            Mock.Of<ILogger<BatchUpdateLayoutCommandHandler>>());
    }

    private static VenueLayout CreateLayout()
    {
        return VenueLayout.Create("Layout", LayoutType.Theater, Guid.NewGuid()).Value;
    }

    /// <summary>
    /// S8.8c fixture: builds a layout that's attached to an event so the
    /// tier-reconciliation block has a non-null EventId to query for tiers.
    /// </summary>
    private static VenueLayout CreateLayoutWithEvent(Guid eventId)
    {
        return VenueLayout.Create("Layout", LayoutType.Theater, Guid.NewGuid(), eventId).Value;
    }

    /// <summary>
    /// S8.8c fixture: builds a real <see cref="TicketTier"/> for the event so the
    /// reconciler can call <c>AssignToZone</c>/<c>AssignToTable</c>/<c>RemoveAssignment</c>
    /// and we can inspect the resulting <see cref="TicketTier.Assignments"/> collection.
    /// </summary>
    private static TicketTier CreateTier(Guid eventId, string name = "VIP")
    {
        var price = Money.Create(100m, Currency.USD).Value;
        return TicketTier.Create(eventId, name, $"{name} tier", price, null, null, 30, 10, 1).Value;
    }

    private static VenueZone AddZone(VenueLayout layout, string name)
    {
        var zoneResult = layout.AddZone(name, "#fff", sortOrder: 0);
        zoneResult.IsSuccess.Should().BeTrue();
        return zoneResult.Value;
    }

    private static VenueTable AddTable(VenueLayout layout, string label, int capacity = 4)
    {
        var tableResult = layout.GenerateRoundTable(label, capacity, sortOrder: 0);
        tableResult.IsSuccess.Should().BeTrue();
        return tableResult.Value;
    }

    private static VenueDecoration AddDecoration(VenueLayout layout, DecorationKind kind, string? label = null)
    {
        var decorationResult = layout.AddDecoration(kind, label, sortOrder: 0);
        decorationResult.IsSuccess.Should().BeTrue();
        return decorationResult.Value;
    }

    private static void SetBackingField<T>(T obj, string propertyName, object? value)
    {
        var backingFieldName = $"<{propertyName}>k__BackingField";
        var type = typeof(T);
        FieldInfo? field = null;
        while (type != null)
        {
            field = type.GetField(backingFieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) break;
            type = type.BaseType;
        }
        field!.SetValue(obj, value);
    }

    private static BatchLayoutPayload EmptyPayload() =>
        new(Name: null, Canvas: null, Zones: null, Tables: null, Decorations: null);

    [Fact]
    public async Task Handle_Should_Forward_Forbidden_From_Authorization()
    {
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Forbidden("denied"));

        var command = new BatchUpdateLayoutCommand(Guid.NewGuid(), 1u, EmptyPayload());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Forbidden);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockMetrics.Verify(m => m.StructuralEditRejected(
            It.IsAny<Guid>(), StructuralEditRejectionReason.AuthFailed), Times.Once);
        _mockMetrics.Verify(m => m.LayoutCanvasEditorSaved(
            It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Layout_Missing()
    {
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(CreateLayout()));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync((VenueLayout?)null);

        var command = new BatchUpdateLayoutCommand(Guid.NewGuid(), 1u, EmptyPayload());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockMetrics.Verify(m => m.LayoutCanvasEditorSaved(
            It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_On_Stale_RowVersion_EarlyCheck()
    {
        var layout = CreateLayout();
        SetBackingField(layout, nameof(VenueLayout.RowVersion), 5u);

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);

        // Expected = 99, actual on layout = 5 → Conflict before any mutation
        var command = new BatchUpdateLayoutCommand(layout.Id, 99u, EmptyPayload());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
        _mockGuard.Verify(g => g.CheckSeatsAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockMetrics.Verify(m => m.StructuralEditRejected(
            layout.Id, StructuralEditRejectionReason.ConcurrencyConflict), Times.Once);
        _mockMetrics.Verify(m => m.LayoutCanvasEditorSaved(
            It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_StructuralEditRejected_When_Guard_Fails_On_Removals()
    {
        var layout = CreateLayout();
        var zone = AddZone(layout, "Section A");
        layout.GenerateTheaterSeats(zone.Id, rows: 2, seatsPerRow: 2);

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockGuard.Setup(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.StructuralEditRejected("4 seats are held"));

        // Slice S2: payload EXPLICITLY deletes the zone via DeletedZoneIds → ambiguity
        // guard passes → structural guard runs → rejected because seats are held.
        // (Pre-S2 the omission path would have reached the guard implicitly; S2's
        // contract requires explicit opt-in to deletion.)
        var command = new BatchUpdateLayoutCommand(
            layout.Id, layout.RowVersion,
            new BatchLayoutPayload(Name: null, Canvas: null,
                Zones: new List<BatchZone>(),
                Tables: null,
                Decorations: null,
                TierAssignments: null,
                DeletedZoneIds: new List<Guid> { zone.Id }));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.StructuralEditRejected);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        layout.Zones.Should().HaveCount(1);   // unchanged
        _mockMetrics.Verify(m => m.StructuralEditRejected(
            layout.Id, StructuralEditRejectionReason.SeatsReserved), Times.Once);
        _mockMetrics.Verify(m => m.LayoutCanvasEditorSaved(
            It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Add_New_Zones_Tables_Decorations_When_Ids_Are_Null()
    {
        var layout = CreateLayout();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var payload = new BatchLayoutPayload(
            Name: null, Canvas: null,
            Zones: new List<BatchZone>
            {
                new(Id: null, Name: "Orchestra", Color: "#ff0", SortOrder: 0, Shape: ZoneShape.Rect, Geometry: null),
            },
            Tables: new List<BatchTable>
            {
                new(Id: null, Label: "Table A", Shape: TableShape.Round, Capacity: 8,
                    SortOrder: 0, ZoneId: null, Geometry: null),
            },
            Decorations: new List<BatchDecoration>
            {
                new(Id: null, Kind: DecorationKind.Stage, Label: "Main Stage",
                    SortOrder: 0, Geometry: null, Properties: null),
            });

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, payload);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        layout.Zones.Should().HaveCount(1);
        layout.Zones[0].Name.Should().Be("Orchestra");
        layout.Tables.Should().HaveCount(1);
        layout.Tables[0].Label.Should().Be("Table A");
        layout.Tables[0].Seats.Should().HaveCount(8);  // round-table seat gen applied
        layout.Decorations.Should().HaveCount(1);
        layout.Decorations[0].Kind.Should().Be(DecorationKind.Stage);
        _mockLayoutRepo.Verify(r => r.SetOriginalRowVersion(layout, layout.RowVersion), Times.Once);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Update_Existing_Items_When_Ids_Match()
    {
        var layout = CreateLayout();
        var zone = AddZone(layout, "Section A");
        var table = AddTable(layout, "Old Table");
        var decoration = AddDecoration(layout, DecorationKind.Stage, "Stage");

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var payload = new BatchLayoutPayload(
            Name: null, Canvas: null,
            Zones: new List<BatchZone>
            {
                new(Id: zone.Id, Name: "Section A Renamed", Color: "#123",
                    SortOrder: 2, Shape: ZoneShape.Curve,
                    Geometry: "{\"centerX\":100,\"centerY\":200,\"radius\":50,\"startAngleDeg\":0,\"sweepAngleDeg\":90,\"rowCount\":3}"),
            },
            Tables: new List<BatchTable>
            {
                new(Id: table.Id, Label: "New Label", Shape: TableShape.Round, Capacity: 4,
                    SortOrder: 5, ZoneId: null, Geometry: null),
            },
            Decorations: new List<BatchDecoration>
            {
                new(Id: decoration.Id, Kind: DecorationKind.DanceFloor, Label: "Floor",
                    SortOrder: 3, Geometry: null, Properties: null),
            });

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, payload);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        layout.Zones.Should().HaveCount(1);
        layout.Zones[0].Name.Should().Be("Section A Renamed");
        layout.Zones[0].Shape.Should().Be(ZoneShape.Curve);
        layout.Tables.Should().HaveCount(1);
        layout.Tables[0].Label.Should().Be("New Label");
        layout.Decorations.Should().HaveCount(1);
        layout.Decorations[0].Kind.Should().Be(DecorationKind.DanceFloor);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Remove_Items_When_Listed_In_DeletedIds()
    {
        // Slice S2 (Architect Rev 4 §A.3) — explicit deletion contract.
        // Pre-S2 this test asserted that omission alone removes items. That
        // silent-deletion behavior is now the bug class S2 closes — see
        // Handle_Should_Return_409_When_Payload_Omits_Zone_Without_DeletedZoneIds.
        var layout = CreateLayout();
        var keptZone = AddZone(layout, "Kept");
        var removedZone = AddZone(layout, "Removed");  // no seats → guard passes
        var decoration = AddDecoration(layout, DecorationKind.Stage);

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockGuard.Setup(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.Success());
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Payload keeps keptZone, explicitly deletes removedZone + the decoration.
        var payload = new BatchLayoutPayload(
            Name: null, Canvas: null,
            Zones: new List<BatchZone>
            {
                new(Id: keptZone.Id, Name: "Kept", Color: "#fff", SortOrder: 0,
                    Shape: ZoneShape.Rect, Geometry: null),
            },
            Tables: new List<BatchTable>(),
            Decorations: new List<BatchDecoration>(),
            TierAssignments: null,
            DeletedZoneIds: new List<Guid> { removedZone.Id },
            DeletedTableIds: null,
            DeletedDecorationIds: new List<Guid> { decoration.Id });

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, payload);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        layout.Zones.Should().HaveCount(1);
        layout.Zones[0].Id.Should().Be(keptZone.Id);
        layout.Decorations.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Return_409_When_Payload_Omits_Zone_Without_DeletedZoneIds()
    {
        // Slice S2: omitting a zone from the payload without listing it in
        // DeletedZoneIds is now an unambiguous error (was silent delete pre-S2).
        var layout = CreateLayout();
        var keptZone = AddZone(layout, "Kept");
        var ambiguouslyOmitted = AddZone(layout, "Omitted");

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);

        var payload = new BatchLayoutPayload(
            Name: null, Canvas: null,
            Zones: new List<BatchZone>
            {
                new(Id: keptZone.Id, Name: "Kept", Color: "#fff", SortOrder: 0,
                    Shape: ZoneShape.Rect, Geometry: null),
            },
            Tables: null,
            Decorations: null);  // no DeletedZoneIds → ambiguous

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, payload);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
        result.Error.Should().Contain(ambiguouslyOmitted.Id.ToString());
        // DB state unchanged — both zones still on the aggregate.
        layout.Zones.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_Should_Return_409_When_Payload_Omits_Table_Without_DeletedTableIds()
    {
        var layout = CreateLayout();
        var keptZone = AddZone(layout, "Z");
        var omittedTable = AddTable(layout, "Round 1", capacity: 8);

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);

        var payload = new BatchLayoutPayload(
            Name: null, Canvas: null,
            Zones: new List<BatchZone>
            {
                new(Id: keptZone.Id, Name: "Z", Color: "#fff", SortOrder: 0,
                    Shape: ZoneShape.Rect, Geometry: null),
            },
            Tables: new List<BatchTable>(),  // omits omittedTable
            Decorations: null);  // no DeletedTableIds → ambiguous

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, payload);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
        result.Error.Should().Contain(omittedTable.Id.ToString());
        layout.Tables.Should().HaveCount(1);  // unchanged
    }

    [Fact]
    public async Task Handle_Should_Return_409_When_Payload_Omits_Decoration_Without_DeletedDecorationIds()
    {
        var layout = CreateLayout();
        var keptZone = AddZone(layout, "Z");
        var omittedDeco = AddDecoration(layout, DecorationKind.Stage);

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);

        var payload = new BatchLayoutPayload(
            Name: null, Canvas: null,
            Zones: new List<BatchZone>
            {
                new(Id: keptZone.Id, Name: "Z", Color: "#fff", SortOrder: 0,
                    Shape: ZoneShape.Rect, Geometry: null),
            },
            Tables: null,
            Decorations: new List<BatchDecoration>());  // omits, no DeletedDecorationIds

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, payload);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
        result.Error.Should().Contain(omittedDeco.Id.ToString());
        layout.Decorations.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_Should_Update_Layout_Name_And_Canvas_When_Provided()
    {
        var layout = CreateLayout();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var payload = new BatchLayoutPayload(
            Name: "New Name",
            Canvas: new BatchCanvasConfig(Width: 1600, Height: 1000, Scale: 1.5, BackgroundColor: "#202020"),
            Zones: null, Tables: null, Decorations: null);

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, payload);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        layout.Name.Should().Be("New Name");
        layout.Canvas.Width.Should().Be(1600);
        layout.Canvas.Height.Should().Be(1000);
        layout.Canvas.Scale.Should().Be(1.5);
        layout.Canvas.BackgroundColor.Should().Be("#202020");
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Domain_Rejects_Update()
    {
        var layout = CreateLayout();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);

        // Zone with an ID the aggregate doesn't know → domain returns "Zone not found"
        var payload = new BatchLayoutPayload(
            Name: null, Canvas: null,
            Zones: new List<BatchZone>
            {
                new(Id: Guid.NewGuid(), Name: "Ghost", Color: "#000", SortOrder: 0,
                    Shape: ZoneShape.Rect, Geometry: null),
            },
            Tables: null, Decorations: null);

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, payload);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_On_DbUpdateConcurrencyException()
    {
        var layout = CreateLayout();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException("xmin stale"));

        var payload = new BatchLayoutPayload(
            Name: "X", Canvas: null, Zones: null, Tables: null, Decorations: null);

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, payload);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
        _mockMetrics.Verify(m => m.StructuralEditRejected(
            layout.Id, StructuralEditRejectionReason.ConcurrencyConflict), Times.Once);
        _mockMetrics.Verify(m => m.LayoutCanvasEditorSaved(
            It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Skip_Guard_When_No_Removals_Needed()
    {
        var layout = CreateLayout();
        var zone = AddZone(layout, "Keep");
        layout.GenerateTheaterSeats(zone.Id, rows: 2, seatsPerRow: 2);  // seats present but zone stays

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var payload = new BatchLayoutPayload(
            Name: "Updated",
            Canvas: null,
            Zones: new List<BatchZone>
            {
                new(Id: zone.Id, Name: "Keep", Color: "#fff", SortOrder: 0,
                    Shape: ZoneShape.Rect, Geometry: null),
            },
            Tables: null, Decorations: null);

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, payload);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _mockGuard.Verify(g => g.CheckSeatsAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        layout.Name.Should().Be("Updated");
    }

    /// <summary>
    /// Slice 8 S8.8a — `layout.canvas_editor_saved` is the 6th and final architect metric.
    /// On a successful save the handler must emit it exactly once with a `changesCount`
    /// equal to the total number of structural mutations applied to the aggregate
    /// (zone/table/decoration removals + updates + additions, plus +1 each for layout-level
    /// Name and Canvas updates when present). The dashboard divides this by
    /// <c>layout.canvas_editor_opened</c> to track editor abandonment + edit volume per session.
    /// </summary>
    [Fact]
    public async Task Handle_Should_Emit_LayoutCanvasEditorSaved_With_Aggregated_Change_Count_On_Success()
    {
        var layout = CreateLayout();
        var keptZone = AddZone(layout, "Keep");        // will be updated
        var removedZone = AddZone(layout, "Remove");   // will be removed (no seats → guard passes)
        var updatedDecoration = AddDecoration(layout, DecorationKind.Stage, "Stage");

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockGuard.Setup(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.Success());
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Mutations:
        //   - 1 zone removed (removedZone omitted)
        //   - 1 zone updated (keptZone present with new name)
        //   - 1 zone added (id null)
        //   - 1 table added (id null, no existing tables)
        //   - 1 decoration updated (kind change)
        //   - 1 layout Name update
        //   - 1 layout Canvas update
        // Total: 7
        var payload = new BatchLayoutPayload(
            Name: "Renamed Layout",
            Canvas: new BatchCanvasConfig(Width: 1600, Height: 1000, Scale: 1.0, BackgroundColor: "#101010"),
            Zones: new List<BatchZone>
            {
                new(Id: keptZone.Id, Name: "Keep Renamed", Color: "#fff", SortOrder: 0,
                    Shape: ZoneShape.Rect, Geometry: null),
                new(Id: null, Name: "Brand New Zone", Color: "#abc", SortOrder: 1,
                    Shape: ZoneShape.Rect, Geometry: null),
            },
            Tables: new List<BatchTable>
            {
                new(Id: null, Label: "T1", Shape: TableShape.Round, Capacity: 4,
                    SortOrder: 0, ZoneId: null, Geometry: null),
            },
            Decorations: new List<BatchDecoration>
            {
                new(Id: updatedDecoration.Id, Kind: DecorationKind.DanceFloor, Label: "Floor",
                    SortOrder: 0, Geometry: null, Properties: null),
            },
            TierAssignments: null,
            // Slice S2: explicit deletion opt-in for the "removed" zone.
            DeletedZoneIds: new List<Guid> { removedZone.Id });

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, payload);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockMetrics.Verify(m => m.LayoutCanvasEditorSaved(layout.Id, 7), Times.Once);
        _mockMetrics.Verify(m => m.StructuralEditRejected(
            It.IsAny<Guid>(), It.IsAny<StructuralEditRejectionReason>()), Times.Never);
    }

    // ─────────── S8.8c: tier-assignment reconciliation tests ───────────

    [Fact]
    public async Task Handle_Should_Skip_TierReconciliation_When_Payload_TierAssignments_Is_Null()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, EmptyPayload());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Reconciler must not query tiers when TierAssignments is null.
        _mockEventRepo.Verify(
            r => r.GetTicketTiersWithAssignmentsForEventAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Reject_TierAssignments_On_Template_Layout()
    {
        // Template layouts have no event; tier assignments should be rejected
        // because TicketTier belongs to the Event aggregate.
        var layout = CreateLayout();   // EventId == null

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);

        var payload = new BatchLayoutPayload(
            Name: null, Canvas: null, Zones: null, Tables: null, Decorations: null,
            TierAssignments: new List<BatchTierAssignment>
            {
                new(AssignableKind.Zone, Guid.NewGuid(), new List<Guid> { Guid.NewGuid() }),
            });

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, payload);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Validation);
        result.Error.Should().Contain("template");
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Add_TierAssignment_For_Existing_Zone_When_Desired_Includes_It()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);
        var zone = AddZone(layout, "Front");
        var tier = CreateTier(eventId);

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockEventRepo.Setup(r => r.GetTicketTiersWithAssignmentsForEventAsync(eventId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((IReadOnlyList<TicketTier>)new[] { tier });
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Desired: assign tier to zone (currently no assignments).
        var payload = new BatchLayoutPayload(
            Name: null, Canvas: null,
            Zones: new List<BatchZone>
            {
                new(Id: zone.Id, Name: zone.Name, Color: zone.Color, SortOrder: zone.SortOrder,
                    Shape: ZoneShape.Rect, Geometry: null),
            },
            Tables: null, Decorations: null,
            TierAssignments: new List<BatchTierAssignment>
            {
                new(AssignableKind.Zone, zone.Id, new List<Guid> { tier.Id }),
            });

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, payload);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tier.Assignments.Should().ContainSingle()
            .Which.Should().Match<TierAssignment>(a =>
                a.AssignableKind == AssignableKind.Zone && a.AssignableId == zone.Id);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        // Metric tag: zone update (1) + tier add (1) = 2.
        _mockMetrics.Verify(m => m.LayoutCanvasEditorSaved(layout.Id, 2), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Remove_TierAssignment_When_Desired_Drops_Existing_Tier()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);
        var zone = AddZone(layout, "Front");
        var tier = CreateTier(eventId);
        // Pre-existing assignment that the desired state will drop.
        tier.AssignToZone(zone.Id).IsSuccess.Should().BeTrue();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockEventRepo.Setup(r => r.GetTicketTiersWithAssignmentsForEventAsync(eventId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((IReadOnlyList<TicketTier>)new[] { tier });
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var payload = new BatchLayoutPayload(
            Name: null, Canvas: null,
            Zones: new List<BatchZone>
            {
                new(Id: zone.Id, Name: zone.Name, Color: zone.Color, SortOrder: zone.SortOrder,
                    Shape: ZoneShape.Rect, Geometry: null),
            },
            Tables: null, Decorations: null,
            TierAssignments: new List<BatchTierAssignment>
            {
                // Empty TierIds list for this zone → tier T removed.
                new(AssignableKind.Zone, zone.Id, new List<Guid>()),
            });

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, payload);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tier.Assignments.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Resolve_ClientId_To_Server_Guid_For_Newly_Added_Zone()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);
        var tier = CreateTier(eventId);
        var clientZoneId = Guid.NewGuid();   // client-side draft Guid

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockEventRepo.Setup(r => r.GetTicketTiersWithAssignmentsForEventAsync(eventId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((IReadOnlyList<TicketTier>)new[] { tier });
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // New zone with Id=null + ClientId set; tierAssignments references the client Guid.
        var payload = new BatchLayoutPayload(
            Name: null, Canvas: null,
            Zones: new List<BatchZone>
            {
                new(Id: null, Name: "Brand New", Color: "#fff", SortOrder: 0,
                    Shape: ZoneShape.Rect, Geometry: null, ClientId: clientZoneId),
            },
            Tables: null, Decorations: null,
            TierAssignments: new List<BatchTierAssignment>
            {
                new(AssignableKind.Zone, clientZoneId, new List<Guid> { tier.Id }),
            });

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, payload);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        layout.Zones.Should().ContainSingle();
        var serverZoneId = layout.Zones[0].Id;
        serverZoneId.Should().NotBe(clientZoneId);   // server assigned a different Guid
        tier.Assignments.Should().ContainSingle()
            .Which.AssignableId.Should().Be(serverZoneId);
    }

    [Fact]
    public async Task Handle_Should_Reject_TierAssignment_For_Unknown_Zone_Or_Table()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);
        var tier = CreateTier(eventId);
        var ghostZoneId = Guid.NewGuid();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockEventRepo.Setup(r => r.GetTicketTiersWithAssignmentsForEventAsync(eventId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((IReadOnlyList<TicketTier>)new[] { tier });

        var payload = new BatchLayoutPayload(
            Name: null, Canvas: null, Zones: null, Tables: null, Decorations: null,
            TierAssignments: new List<BatchTierAssignment>
            {
                new(AssignableKind.Zone, ghostZoneId, new List<Guid> { tier.Id }),
            });

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, payload);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Reject_When_Tier_Does_Not_Belong_To_Event()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);
        var zone = AddZone(layout, "Front");
        // Tier belongs to a *different* event — repo returns no tiers for THIS event,
        // and the handler validates desired tierIds against the event's tier list.
        var foreignTier = CreateTier(Guid.NewGuid());

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockEventRepo.Setup(r => r.GetTicketTiersWithAssignmentsForEventAsync(eventId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((IReadOnlyList<TicketTier>)Array.Empty<TicketTier>());

        var payload = new BatchLayoutPayload(
            Name: null, Canvas: null,
            Zones: new List<BatchZone>
            {
                new(Id: zone.Id, Name: zone.Name, Color: zone.Color, SortOrder: zone.SortOrder,
                    Shape: ZoneShape.Rect, Geometry: null),
            },
            Tables: null, Decorations: null,
            TierAssignments: new List<BatchTierAssignment>
            {
                new(AssignableKind.Zone, zone.Id, new List<Guid> { foreignTier.Id }),
            });

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, payload);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Validation);
        result.Error.Should().Contain("does not belong to this event");
    }

    [Fact]
    public async Task Handle_Should_Be_NoOp_When_Desired_TierAssignments_Match_Current()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);
        var zone = AddZone(layout, "Front");
        var tier = CreateTier(eventId);
        tier.AssignToZone(zone.Id).IsSuccess.Should().BeTrue();
        var assignmentsBefore = tier.Assignments.Count;

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockEventRepo.Setup(r => r.GetTicketTiersWithAssignmentsForEventAsync(eventId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((IReadOnlyList<TicketTier>)new[] { tier });
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Desired matches current exactly.
        var payload = new BatchLayoutPayload(
            Name: null, Canvas: null,
            Zones: new List<BatchZone>
            {
                new(Id: zone.Id, Name: zone.Name, Color: zone.Color, SortOrder: zone.SortOrder,
                    Shape: ZoneShape.Rect, Geometry: null),
            },
            Tables: null, Decorations: null,
            TierAssignments: new List<BatchTierAssignment>
            {
                new(AssignableKind.Zone, zone.Id, new List<Guid> { tier.Id }),
            });

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, payload);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tier.Assignments.Should().HaveCount(assignmentsBefore);   // unchanged
        // Metric tag: zone update (1) + tier no-op (0) = 1. Confirms the
        // reconciler doesn't double-count idempotent assignments.
        _mockMetrics.Verify(m => m.LayoutCanvasEditorSaved(layout.Id, 1), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Remove_Orphan_TierAssignment_When_Zone_Deleted_In_Same_Batch()
    {
        // The architect-flagged data integrity case: user deletes a zone *and*
        // the desired state (which excludes the deleted zone) should clean up
        // its tier assignment in the same transaction. Reconciler removes from
        // the current set anything not in the desired set.
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);
        var keptZone = AddZone(layout, "Kept");
        var droppedZone = AddZone(layout, "Dropped");   // no seats → guard passes
        var tier = CreateTier(eventId);
        tier.AssignToZone(keptZone.Id).IsSuccess.Should().BeTrue();
        tier.AssignToZone(droppedZone.Id).IsSuccess.Should().BeTrue();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockGuard.Setup(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.Success());
        _mockEventRepo.Setup(r => r.GetTicketTiersWithAssignmentsForEventAsync(eventId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((IReadOnlyList<TicketTier>)new[] { tier });
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Slice S2: payload explicitly deletes droppedZone via DeletedZoneIds and
        // only mentions keptZone in tierAssignments. The reconciler picks up the
        // orphan-tier-assignment cleanup as before.
        var payload = new BatchLayoutPayload(
            Name: null, Canvas: null,
            Zones: new List<BatchZone>
            {
                new(Id: keptZone.Id, Name: keptZone.Name, Color: keptZone.Color,
                    SortOrder: keptZone.SortOrder, Shape: ZoneShape.Rect, Geometry: null),
            },
            Tables: null, Decorations: null,
            TierAssignments: new List<BatchTierAssignment>
            {
                new(AssignableKind.Zone, keptZone.Id, new List<Guid> { tier.Id }),
            },
            DeletedZoneIds: new List<Guid> { droppedZone.Id });

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, payload);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        layout.Zones.Should().ContainSingle().Which.Id.Should().Be(keptZone.Id);
        // Orphan removed: only the keptZone assignment survives.
        tier.Assignments.Should().ContainSingle()
            .Which.AssignableId.Should().Be(keptZone.Id);
    }

    [Fact]
    public async Task Handle_Should_Treat_Empty_TierAssignments_List_As_Remove_All()
    {
        var eventId = Guid.NewGuid();
        var layout = CreateLayoutWithEvent(eventId);
        var zone = AddZone(layout, "Front");
        var tierA = CreateTier(eventId, "VIP");
        var tierB = CreateTier(eventId, "Plus");
        tierA.AssignToZone(zone.Id).IsSuccess.Should().BeTrue();
        tierB.AssignToZone(zone.Id).IsSuccess.Should().BeTrue();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockEventRepo.Setup(r => r.GetTicketTiersWithAssignmentsForEventAsync(eventId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((IReadOnlyList<TicketTier>)new[] { tierA, tierB });
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var payload = new BatchLayoutPayload(
            Name: null, Canvas: null,
            Zones: new List<BatchZone>
            {
                new(Id: zone.Id, Name: zone.Name, Color: zone.Color, SortOrder: zone.SortOrder,
                    Shape: ZoneShape.Rect, Geometry: null),
            },
            Tables: null, Decorations: null,
            // Empty list → reconcile to "no assignments" (remove all).
            TierAssignments: new List<BatchTierAssignment>());

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, payload);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tierA.Assignments.Should().BeEmpty();
        tierB.Assignments.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Emit_LayoutCanvasEditorSaved_With_Zero_Changes_On_Empty_Save()
    {
        // Edge case: organizer opens editor + clicks Save without making any changes.
        // The Save button should normally be disabled in this case (S8.8b), but if the
        // request reaches the backend we still emit the metric with count=0 so the
        // dashboard reflects an honest "open + close = 0 edits" abandonment data point.
        var layout = CreateLayout();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockLayoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(layout);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, EmptyPayload());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _mockMetrics.Verify(m => m.LayoutCanvasEditorSaved(layout.Id, 0), Times.Once);
    }
}
