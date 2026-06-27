using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Domain.Tests.Events.Entities;

/// <summary>
/// Tests for the Slice 2+3A table-based seat factory (<see cref="Seat.CreateAtTable"/>)
/// and the zone/table XOR invariant. The existing <see cref="SeatTests"/> file
/// continues to cover the zone-based factory to guarantee back-compat.
/// </summary>
public class SeatAtTableTests
{
    private readonly Guid _tableId = Guid.NewGuid();

    [Fact]
    public void CreateAtTable_WithValidData_Should_Succeed()
    {
        var result = Seat.CreateAtTable(
            venueTableId: _tableId,
            tableLabel: "T1",
            seatNumber: 1,
            label: "T1-S1",
            sortOrder: 0,
            angleDeg: 45);

        result.IsSuccess.Should().BeTrue();
        var seat = result.Value;
        seat.VenueTableId.Should().Be(_tableId);
        seat.VenueZoneId.Should().BeNull();
        seat.AngleDeg.Should().Be(45);
        seat.Row.Should().Be("T1");
        seat.Number.Should().Be(1);
        seat.Label.Should().Be("T1-S1");
        seat.IsTableSeat.Should().BeTrue();
        seat.IsZoneSeat.Should().BeFalse();
    }

    [Fact]
    public void CreateAtTable_WithEmptyTableId_Should_Fail()
    {
        var result = Seat.CreateAtTable(Guid.Empty, "T1", 1, "T1-S1", 0, 0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("table");
    }

    [Theory]
    [InlineData(-720, 0)]
    [InlineData(-360, 0)]
    [InlineData(-1, 359)]
    [InlineData(360, 0)]
    [InlineData(720, 0)]
    [InlineData(361, 1)]
    public void CreateAtTable_Should_Normalize_Angle_Into_Zero_To_360(double input, double expected)
    {
        var result = Seat.CreateAtTable(_tableId, "T1", 1, "T1-S1", 0, input);

        result.IsSuccess.Should().BeTrue();
        result.Value.AngleDeg.Should().BeApproximately(expected, 0.0001);
    }

    [Fact]
    public void CreateAtTable_WithNaN_Should_Fail()
    {
        var result = Seat.CreateAtTable(_tableId, "T1", 1, "T1-S1", 0, double.NaN);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("finite");
    }

    [Fact]
    public void CreateAtTable_WithInfinity_Should_Fail()
    {
        var result = Seat.CreateAtTable(_tableId, "T1", 1, "T1-S1", 0, double.PositiveInfinity);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void CreateAtTable_Trims_Label_And_TableLabel()
    {
        var result = Seat.CreateAtTable(_tableId, " T2 ", 1, " T2-S1 ", 0, 0);

        result.IsSuccess.Should().BeTrue();
        result.Value.Row.Should().Be("T2");
        result.Value.Label.Should().Be("T2-S1");
    }

    [Fact]
    public void ZoneSeat_And_TableSeat_Should_Be_Mutually_Exclusive()
    {
        var zoneSeat = Seat.Create(Guid.NewGuid(), "A", 1, "A1", 0).Value;
        var tableSeat = Seat.CreateAtTable(_tableId, "T1", 1, "T1-S1", 0, 0).Value;

        zoneSeat.IsZoneSeat.Should().BeTrue();
        zoneSeat.IsTableSeat.Should().BeFalse();
        zoneSeat.VenueTableId.Should().BeNull();

        tableSeat.IsTableSeat.Should().BeTrue();
        tableSeat.IsZoneSeat.Should().BeFalse();
        tableSeat.VenueZoneId.Should().BeNull();
    }
}
