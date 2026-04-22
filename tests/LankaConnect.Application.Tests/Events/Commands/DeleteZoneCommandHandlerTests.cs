using FluentAssertions;
using LankaConnect.Application.Events.Commands.DeleteZone;
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
/// Slice 5 Chunk 5: DELETE /api/venue-layouts/{id}/zones/{zoneId}.
/// Always runs the structural guard. 422 when seats held/reserved. 409 on stale If-Match.
/// </summary>
public class DeleteZoneCommandHandlerTests
{
    private readonly Mock<ILayoutAuthorizationService> _mockAuth = new();
    private readonly Mock<IStructuralEditGuard> _mockGuard = new();
    private readonly Mock<IVenueLayoutRepository> _mockRepo = new();
    private readonly Mock<IUnitOfWork> _mockUow = new();
    private readonly Mock<ILayoutMetrics> _mockMetrics = new();
    private readonly DeleteZoneCommandHandler _sut;

    public DeleteZoneCommandHandlerTests()
    {
        _sut = new DeleteZoneCommandHandler(
            _mockAuth.Object,
            _mockGuard.Object,
            _mockRepo.Object,
            _mockUow.Object,
            _mockMetrics.Object,
            Mock.Of<ILogger<DeleteZoneCommandHandler>>());
    }

    private static (VenueLayout layout, VenueZone zone) CreateLayoutWithZone()
    {
        var layout = VenueLayout.Create("Layout", LayoutType.Theater, Guid.NewGuid()).Value;
        var zone = layout.AddZone("A", "#fff", 0).Value;
        return (layout, zone);
    }

    [Fact]
    public async Task Handle_Should_Forward_Forbidden_From_Authorization()
    {
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Forbidden("denied"));

        var command = new DeleteZoneCommand(Guid.NewGuid(), Guid.NewGuid(), 1u);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Forbidden);
        _mockMetrics.Verify(m => m.StructuralEditRejected(
            It.IsAny<Guid>(), StructuralEditRejectionReason.AuthFailed), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Layout_Missing()
    {
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(
                     VenueLayout.Create("L", LayoutType.Theater, Guid.NewGuid()).Value));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((VenueLayout?)null);

        var command = new DeleteZoneCommand(Guid.NewGuid(), Guid.NewGuid(), 1u);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Zone_Missing()
    {
        var (layout, _) = CreateLayoutWithZone();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);

        var command = new DeleteZoneCommand(layout.Id, Guid.NewGuid(), 1u);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_Should_Reject_When_StructuralGuard_Fails()
    {
        var (layout, zone) = CreateLayoutWithZone();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockGuard.Setup(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.StructuralEditRejected("seats reserved"));

        var command = new DeleteZoneCommand(layout.Id, zone.Id, 1u);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.StructuralEditRejected);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockMetrics.Verify(m => m.StructuralEditRejected(
            layout.Id, StructuralEditRejectionReason.SeatsReserved), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Remove_Zone_And_Commit_On_Happy_Path()
    {
        var (layout, zone) = CreateLayoutWithZone();
        layout.Zones.Should().ContainSingle();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockGuard.Setup(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.Success());
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new DeleteZoneCommand(layout.Id, zone.Id, 11u);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        layout.Zones.Should().BeEmpty();
        _mockRepo.Verify(r => r.SetOriginalRowVersion(layout, 11u), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_On_DbUpdateConcurrencyException()
    {
        var (layout, zone) = CreateLayoutWithZone();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockGuard.Setup(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.Success());
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException("xmin stale"));

        var command = new DeleteZoneCommand(layout.Id, zone.Id, 77u);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
        _mockMetrics.Verify(m => m.StructuralEditRejected(
            layout.Id, StructuralEditRejectionReason.ConcurrencyConflict), Times.Once);
    }
}
