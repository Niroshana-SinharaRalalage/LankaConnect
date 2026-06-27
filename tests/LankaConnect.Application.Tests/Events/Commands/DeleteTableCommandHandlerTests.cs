using FluentAssertions;
using LankaConnect.Application.Events.Commands.DeleteTable;
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
/// Slice 5 Chunk 6: DELETE /api/venue-layouts/{id}/tables/{tableId}.
/// Always structural; the guard always runs. 422 when seats held / reserved.
/// </summary>
public class DeleteTableCommandHandlerTests
{
    private readonly Mock<ILayoutAuthorizationService> _mockAuth = new();
    private readonly Mock<IStructuralEditGuard> _mockGuard = new();
    private readonly Mock<IVenueLayoutRepository> _mockRepo = new();
    private readonly Mock<IUnitOfWork> _mockUow = new();
    private readonly Mock<ILayoutMetrics> _mockMetrics = new();
    private readonly DeleteTableCommandHandler _sut;

    public DeleteTableCommandHandlerTests()
    {
        _sut = new DeleteTableCommandHandler(
            _mockAuth.Object,
            _mockGuard.Object,
            _mockRepo.Object,
            _mockUow.Object,
            _mockMetrics.Object,
            Mock.Of<ILogger<DeleteTableCommandHandler>>());
    }

    private static (VenueLayout layout, VenueTable table) CreateLayoutWithTable()
    {
        var layout = VenueLayout.Create("Layout", LayoutType.Banquet, Guid.NewGuid()).Value;
        var table = layout.GenerateRoundTable("T1", 8, 0).Value;
        return (layout, table);
    }

    [Fact]
    public async Task Handle_Should_Forward_Forbidden_From_Authorization()
    {
        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Forbidden("denied"));

        var command = new DeleteTableCommand(Guid.NewGuid(), Guid.NewGuid(), 1u);

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
                 .ReturnsAsync(Result<VenueLayout>.Success(CreateLayoutWithTable().layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((VenueLayout?)null);

        var command = new DeleteTableCommand(Guid.NewGuid(), Guid.NewGuid(), 1u);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Table_Missing_In_Loaded_Layout()
    {
        var (layout, _) = CreateLayoutWithTable();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);

        var command = new DeleteTableCommand(layout.Id, Guid.NewGuid(), 1u);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
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
                  .ReturnsAsync(Result.StructuralEditRejected("1 seat(s) reserved"));

        var command = new DeleteTableCommand(layout.Id, table.Id, 1u);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.StructuralEditRejected);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockMetrics.Verify(m => m.StructuralEditRejected(
            layout.Id, StructuralEditRejectionReason.SeatsReserved), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Remove_Table_On_Success()
    {
        var (layout, table) = CreateLayoutWithTable();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockGuard.Setup(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.Success());
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new DeleteTableCommand(layout.Id, table.Id, 11u);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        layout.Tables.Should().BeEmpty();
        _mockRepo.Verify(r => r.SetOriginalRowVersion(layout, 11u), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_On_DbUpdateConcurrencyException()
    {
        var (layout, table) = CreateLayoutWithTable();

        _mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Result<VenueLayout>.Success(layout));
        _mockRepo.Setup(r => r.GetWithZonesAndSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(layout);
        _mockGuard.Setup(g => g.CheckSeatsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Result.Success());
        _mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException("xmin mismatch"));

        var command = new DeleteTableCommand(layout.Id, table.Id, 9u);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
        _mockMetrics.Verify(m => m.StructuralEditRejected(
            layout.Id, StructuralEditRejectionReason.ConcurrencyConflict), Times.Once);
    }
}
