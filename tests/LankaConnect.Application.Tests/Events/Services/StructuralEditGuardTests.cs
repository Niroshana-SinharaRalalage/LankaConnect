using FluentAssertions;
using LankaConnect.Products.LankaEvents.Application.Services;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Services;

/// <summary>
/// Slice 5 Chunk 3: StructuralEditGuard blocks destructive layout changes when any
/// affected seat is currently held or reserved. The API maps the failure to HTTP 422
/// with title <c>layout.structural_edit_rejected</c> (see BaseController.BuildProblem).
/// Callers pass the set of affected seat IDs (zone-wide, table-wide, or layout-wide).
/// </summary>
public class StructuralEditGuardTests
{
    private readonly Mock<ISeatHoldRepository> _mockHolds = new();
    private readonly Mock<ISeatReservationRepository> _mockReservations = new();
    private readonly StructuralEditGuard _sut;

    public StructuralEditGuardTests()
    {
        _sut = new StructuralEditGuard(
            _mockHolds.Object,
            _mockReservations.Object,
            Mock.Of<ILogger<StructuralEditGuard>>());
    }

    [Fact]
    public async Task Check_Should_Succeed_When_Seat_Set_Is_Empty()
    {
        var result = await _sut.CheckSeatsAsync(Array.Empty<Guid>(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _mockHolds.Verify(r => r.GetHeldSeatIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockReservations.Verify(r => r.GetReservedSeatIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Check_Should_Succeed_When_No_Seats_Held_Or_Reserved()
    {
        var seatIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        _mockHolds.Setup(r => r.GetHeldSeatIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Array.Empty<Guid>());
        _mockReservations.Setup(r => r.GetReservedSeatIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(Array.Empty<Guid>());

        var result = await _sut.CheckSeatsAsync(seatIds, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Check_Should_Reject_With_StructuralEditRejected_When_Any_Seat_Held()
    {
        var heldSeat = Guid.NewGuid();
        var seatIds = new[] { heldSeat, Guid.NewGuid() };

        _mockHolds.Setup(r => r.GetHeldSeatIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new[] { heldSeat });
        _mockReservations.Setup(r => r.GetReservedSeatIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(Array.Empty<Guid>());

        var result = await _sut.CheckSeatsAsync(seatIds, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.StructuralEditRejected);
    }

    [Fact]
    public async Task Check_Should_Reject_With_StructuralEditRejected_When_Any_Seat_Reserved()
    {
        var reservedSeat = Guid.NewGuid();
        var seatIds = new[] { reservedSeat, Guid.NewGuid() };

        _mockHolds.Setup(r => r.GetHeldSeatIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Array.Empty<Guid>());
        _mockReservations.Setup(r => r.GetReservedSeatIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new[] { reservedSeat });

        var result = await _sut.CheckSeatsAsync(seatIds, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.StructuralEditRejected);
    }

    [Fact]
    public async Task Check_Should_Reject_When_Both_Held_And_Reserved()
    {
        var heldSeat = Guid.NewGuid();
        var reservedSeat = Guid.NewGuid();
        var seatIds = new[] { heldSeat, reservedSeat, Guid.NewGuid() };

        _mockHolds.Setup(r => r.GetHeldSeatIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new[] { heldSeat });
        _mockReservations.Setup(r => r.GetReservedSeatIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new[] { reservedSeat });

        var result = await _sut.CheckSeatsAsync(seatIds, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.StructuralEditRejected);
        result.Error.Should().Contain("held").And.Contain("reserved");
    }
}
