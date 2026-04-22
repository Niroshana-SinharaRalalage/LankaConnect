using System.Reflection;
using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.BatchUpdateLayout;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
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
    private readonly Mock<IUnitOfWork> _mockUow = new();
    private readonly Mock<ILayoutMetrics> _mockMetrics = new();
    private readonly BatchUpdateLayoutCommandHandler _sut;

    public BatchUpdateLayoutCommandHandlerTests()
    {
        _sut = new BatchUpdateLayoutCommandHandler(
            _mockAuth.Object,
            _mockGuard.Object,
            _mockLayoutRepo.Object,
            _mockUow.Object,
            _mockMetrics.Object,
            Mock.Of<ILogger<BatchUpdateLayoutCommandHandler>>());
    }

    private static VenueLayout CreateLayout()
    {
        return VenueLayout.Create("Layout", LayoutType.Theater, Guid.NewGuid()).Value;
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

        // Payload omits the zone → removal → guard runs → rejected
        var command = new BatchUpdateLayoutCommand(
            layout.Id, layout.RowVersion,
            new BatchLayoutPayload(Name: null, Canvas: null,
                Zones: new List<BatchZone>(),
                Tables: null,
                Decorations: null));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.StructuralEditRejected);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        layout.Zones.Should().HaveCount(1);   // unchanged
        _mockMetrics.Verify(m => m.StructuralEditRejected(
            layout.Id, StructuralEditRejectionReason.SeatsReserved), Times.Once);
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
    public async Task Handle_Should_Remove_Items_Missing_From_Payload_When_No_Seats_Held()
    {
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

        // Payload keeps keptZone + removes removedZone + removes the decoration (by omission)
        var payload = new BatchLayoutPayload(
            Name: null, Canvas: null,
            Zones: new List<BatchZone>
            {
                new(Id: keptZone.Id, Name: "Kept", Color: "#fff", SortOrder: 0,
                    Shape: ZoneShape.Rect, Geometry: null),
            },
            Tables: new List<BatchTable>(),
            Decorations: new List<BatchDecoration>());

        var command = new BatchUpdateLayoutCommand(layout.Id, layout.RowVersion, payload);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        layout.Zones.Should().HaveCount(1);
        layout.Zones[0].Id.Should().Be(keptZone.Id);
        layout.Decorations.Should().BeEmpty();
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
}
