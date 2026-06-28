using FluentAssertions;
using LankaConnect.Products.LankaEvents.Application.Commands.UpdateTable;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Slice 5 Chunk 6: PATCH /api/venue-layouts/{id}/tables/{tableId}.
/// Structural changes (shape / capacity / geometry) invoke the guard.
/// Non-structural changes (label / sort / zone attach) skip the guard.
/// </summary>
public class UpdateTableCommandHandlerTests
{
    private readonly Mock<ILayoutAuthorizationService> _mockAuth = new();
    private readonly Mock<IStructuralEditGuard> _mockGuard = new();
    private readonly Mock<IVenueLayoutRepository> _mockRepo = new();
    private readonly Mock<IUnitOfWork> _mockUow = new();
    private readonly Mock<ILayoutMetrics> _mockMetrics = new();
    private readonly UpdateTableCommandHandler _sut;

    public UpdateTableCommandHandlerTests()
    {
        _sut = new UpdateTableCommandHandler(
            _mockAuth.Object,
            _mockGuard.Object,
            _mockRepo.Object,
            _mockUow.Object,
            _mockMetrics.Object,
            Mock.Of<ILogger<UpdateTableCommandHandler>>());
    }

    private static (VenueLayout layout, VenueTable table) CreateLayoutWithTable()
    {
        var layout = VenueLayout.Create("Layout", LayoutType.Banquet, Guid.NewGuid()).Value;
        var table = layout.GenerateRoundTable("T1", 8, 0).Value;
        return (layout, table);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_All_Fields_Null()
    {
        var command = new UpdateTableCommand(
            Guid.NewGuid(), Guid.NewGuid(), 1u,
            null, null, null, null, null, false, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Validation);
        _mockAuth.Verify(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Forward_Forbidden_From_Authorization()
    {
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Forbidden("denied"));

        var command = new UpdateTableCommand(
            Guid.NewGuid(), Guid.NewGuid(), 1u,
            "NewLabel", null, null, null, null, false, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Forbidden);
        _mockMetrics.Verify(m => m.StructuralEditRejected(
            It.IsAny<Guid>(), StructuralEditRejectionReason.AuthFailed), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Table_Missing_In_Loaded_Layout()
    {
        var (layout, _) = CreateLayoutWithTable();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);

        var command = new UpdateTableCommand(
            layout.Id, Guid.NewGuid(), 1u,
            "NewLabel", null, null, null, null, false, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_Should_Skip_StructuralGuard_For_Label_Only_Update()
    {
        var (layout, table) = CreateLayoutWithTable();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new UpdateTableCommand(
            layout.Id, table.Id, 5u,
            "Renamed", null, null, null, null, false, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        table.Label.Should().Be("Renamed");
        _mockGuard.Verify(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepo.Verify(r => r.SetOriginalRowVersion(layout, 5u), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Run_StructuralGuard_When_Geometry_Supplied()
    {
        var (layout, table) = CreateLayoutWithTable();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockGuard.Setup(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.Success());
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var newGeometry = "{\"centerX\":10,\"centerY\":10,\"radius\":50}";
        var command = new UpdateTableCommand(
            layout.Id, table.Id, 3u,
            null, null, null, null, null, false, newGeometry);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        table.Geometry.Should().Be(newGeometry);
        _mockGuard.Verify(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Reject_With_StructuralEditRejected_When_Guard_Fails()
    {
        var (layout, table) = CreateLayoutWithTable();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockGuard.Setup(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.StructuralEditRejected("2 seat(s) held"));

        var command = new UpdateTableCommand(
            layout.Id, table.Id, 1u,
            null, TableShape.Square, 8, null, null, false, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.StructuralEditRejected);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockMetrics.Verify(m => m.StructuralEditRejected(
            layout.Id, StructuralEditRejectionReason.SeatsReserved), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Clear_ZoneId_When_ClearZoneId_True()
    {
        var layout = VenueLayout.Create("Layout", LayoutType.Banquet, Guid.NewGuid()).Value;
        var zone = layout.AddZone("Section A", "#fff", 0).Value;
        var table = layout.GenerateRoundTable("T1", 8, 0, zoneId: zone.Id).Value;

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        table.VenueZoneId.Should().Be(zone.Id);

        var command = new UpdateTableCommand(
            layout.Id, table.Id, 1u,
            null, null, null, null, null, ClearZoneId: true, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        table.VenueZoneId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_On_DbUpdateConcurrencyException()
    {
        var (layout, table) = CreateLayoutWithTable();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException("xmin mismatch"));

        var command = new UpdateTableCommand(
            layout.Id, table.Id, 9u,
            "Stale", null, null, null, null, false, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
        _mockMetrics.Verify(m => m.StructuralEditRejected(
            layout.Id, StructuralEditRejectionReason.ConcurrencyConflict), Times.Once);
    }
}
