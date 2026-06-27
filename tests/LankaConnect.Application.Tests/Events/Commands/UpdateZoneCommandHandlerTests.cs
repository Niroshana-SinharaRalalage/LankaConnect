using FluentAssertions;
using LankaConnect.Application.Events.Commands.UpdateZone;
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
/// Slice 5 Chunk 5: PATCH /api/venue-layouts/{id}/zones/{zoneId}.
/// Non-structural updates (name/color/sort) skip the guard. Structural updates
/// (shape/geometry) must run the guard → 422 when seats are held/reserved.
/// </summary>
public class UpdateZoneCommandHandlerTests
{
    private readonly Mock<ILayoutAuthorizationService> _mockAuth = new();
    private readonly Mock<IStructuralEditGuard> _mockGuard = new();
    private readonly Mock<IVenueLayoutRepository> _mockRepo = new();
    private readonly Mock<IUnitOfWork> _mockUow = new();
    private readonly Mock<ILayoutMetrics> _mockMetrics = new();
    private readonly UpdateZoneCommandHandler _sut;

    public UpdateZoneCommandHandlerTests()
    {
        _sut = new UpdateZoneCommandHandler(
            _mockAuth.Object,
            _mockGuard.Object,
            _mockRepo.Object,
            _mockUow.Object,
            _mockMetrics.Object,
            Mock.Of<ILogger<UpdateZoneCommandHandler>>());
    }

    private static (VenueLayout layout, VenueZone zone) CreateLayoutWithZone()
    {
        var layout = VenueLayout.Create("Layout 1", LayoutType.Theater, Guid.NewGuid()).Value;
        var zoneResult = layout.AddZone("Zone A", "#ff0000", 0);
        return (layout, zoneResult.Value);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_All_Fields_Null()
    {
        var command = new UpdateZoneCommand(Guid.NewGuid(), Guid.NewGuid(), 1u, null, null, null, null, null);

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

        var command = new UpdateZoneCommand(Guid.NewGuid(), Guid.NewGuid(), 1u, "New", null, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Forbidden);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockMetrics.Verify(m => m.StructuralEditRejected(
            It.IsAny<Guid>(), StructuralEditRejectionReason.AuthFailed), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Zone_Missing_In_Loaded_Layout()
    {
        var (layout, _) = CreateLayoutWithZone();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);

        var command = new UpdateZoneCommand(layout.Id, Guid.NewGuid(), 1u, "New", null, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_Should_Skip_StructuralGuard_For_Name_Only_Update()
    {
        var (layout, zone) = CreateLayoutWithZone();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new UpdateZoneCommand(layout.Id, zone.Id, 5u, "Renamed", null, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        zone.Name.Should().Be("Renamed");
        _mockGuard.Verify(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepo.Verify(r => r.SetOriginalRowVersion(layout, 5u), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Run_StructuralGuard_When_Geometry_Supplied()
    {
        var (layout, zone) = CreateLayoutWithZone();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockGuard.Setup(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.Success());
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var newGeometry = "{\"x\":10,\"y\":10,\"width\":100,\"height\":100}";
        var command = new UpdateZoneCommand(layout.Id, zone.Id, 3u, null, null, null, null, newGeometry);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        zone.Geometry.Should().Be(newGeometry);
        _mockGuard.Verify(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Reject_With_StructuralEditRejected_When_Guard_Fails()
    {
        var (layout, zone) = CreateLayoutWithZone();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockGuard.Setup(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.StructuralEditRejected("3 seat(s) held, 0 seat(s) reserved"));

        var command = new UpdateZoneCommand(layout.Id, zone.Id, 1u, null, null, null, ZoneShape.Curve, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.StructuralEditRejected);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockMetrics.Verify(m => m.StructuralEditRejected(
            layout.Id, StructuralEditRejectionReason.SeatsReserved), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_On_DbUpdateConcurrencyException()
    {
        var (layout, zone) = CreateLayoutWithZone();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException("xmin mismatch"));

        var command = new UpdateZoneCommand(layout.Id, zone.Id, 9u, "Stale", null, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
        _mockMetrics.Verify(m => m.StructuralEditRejected(
            layout.Id, StructuralEditRejectionReason.ConcurrencyConflict), Times.Once);
    }
}
