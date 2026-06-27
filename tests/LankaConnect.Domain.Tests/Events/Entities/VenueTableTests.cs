using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;

namespace LankaConnect.Domain.Tests.Events.Entities;

public class VenueTableTests
{
    private readonly Guid _layoutId = Guid.NewGuid();

    #region Create Tests

    [Fact]
    public void Create_WithValidData_Should_Succeed()
    {
        var result = VenueTable.Create(_layoutId, "T1", TableShape.Round, capacity: 8, sortOrder: 0);

        result.IsSuccess.Should().BeTrue();
        var table = result.Value;
        table.VenueLayoutId.Should().Be(_layoutId);
        table.Label.Should().Be("T1");
        table.Shape.Should().Be(TableShape.Round);
        table.Capacity.Should().Be(8);
        table.SortOrder.Should().Be(0);
        table.Geometry.Should().Be("{}");
        table.Seats.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithEmptyLayoutId_Should_Fail()
    {
        var result = VenueTable.Create(Guid.Empty, "T1", TableShape.Round, 8, 0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("layout");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankLabel_Should_Fail(string label)
    {
        var result = VenueTable.Create(_layoutId, label, TableShape.Round, 8, 0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("label");
    }

    [Fact]
    public void Create_WithCapacityOutOfRange_Should_Fail()
    {
        var below = VenueTable.Create(_layoutId, "T1", TableShape.Round, 0, 0);
        var above = VenueTable.Create(_layoutId, "T1", TableShape.Round, 31, 0);

        below.IsSuccess.Should().BeFalse();
        above.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_Square_WithNonMultipleOfFour_Should_Fail()
    {
        var result = VenueTable.Create(_layoutId, "T1", TableShape.Square, capacity: 6, sortOrder: 0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("multiple of 4");
    }

    [Fact]
    public void Create_Rect_WithAnyCapacity_Should_Succeed()
    {
        var result = VenueTable.Create(_layoutId, "Head", TableShape.Rect, capacity: 7, sortOrder: 0);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_InvalidGeometry_Falls_Back_To_Empty_Json()
    {
        var result = VenueTable.Create(_layoutId, "T1", TableShape.Round, 8, 0, geometry: "not-json");

        result.IsSuccess.Should().BeTrue();
        result.Value.Geometry.Should().Be("{}");
    }

    #endregion

    #region GenerateRoundTableSeats Tests

    [Fact]
    public void GenerateRoundTableSeats_Should_Produce_Capacity_Seats_With_Normalized_Angles()
    {
        var table = VenueTable.Create(_layoutId, "T1", TableShape.Round, capacity: 8, sortOrder: 0).Value;

        var result = table.GenerateRoundTableSeats();

        result.IsSuccess.Should().BeTrue();
        table.Seats.Should().HaveCount(8);

        // 8 seats starting at 0° stepping 45°
        var expectedAngles = new[] { 0.0, 45.0, 90.0, 135.0, 180.0, 225.0, 270.0, 315.0 };
        table.Seats.Select(s => s.AngleDeg).Should().Equal(expectedAngles.Cast<double?>());

        foreach (var seat in table.Seats)
        {
            seat.VenueTableId.Should().Be(table.Id);
            seat.VenueZoneId.Should().BeNull();
            seat.IsTableSeat.Should().BeTrue();
            seat.IsZoneSeat.Should().BeFalse();
        }
    }

    [Fact]
    public void GenerateRoundTableSeats_With_StartAngle_Should_Normalize_To_Zero_360_Range()
    {
        var table = VenueTable.Create(_layoutId, "T1", TableShape.Round, capacity: 4, sortOrder: 0).Value;

        var result = table.GenerateRoundTableSeats(startAngleDeg: 90);

        result.IsSuccess.Should().BeTrue();
        table.Seats.Select(s => s.AngleDeg).Should().Equal(new double?[] { 90.0, 180.0, 270.0, 0.0 });
    }

    [Fact]
    public void GenerateRoundTableSeats_OnNonRoundTable_Should_Fail()
    {
        var table = VenueTable.Create(_layoutId, "Head", TableShape.Rect, 6, 0).Value;

        var result = table.GenerateRoundTableSeats();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Round");
    }

    [Fact]
    public void GenerateRoundTableSeats_Labels_Use_Table_Prefix()
    {
        var table = VenueTable.Create(_layoutId, "T3", TableShape.Round, 4, 0).Value;

        table.GenerateRoundTableSeats();

        table.Seats.Select(s => s.Label).Should().Equal(new[] { "T3-S1", "T3-S2", "T3-S3", "T3-S4" });
    }

    [Fact]
    public void GenerateRoundTableSeats_Clears_Existing_Seats_Before_Regenerating()
    {
        var table = VenueTable.Create(_layoutId, "T1", TableShape.Round, 4, 0).Value;

        table.GenerateRoundTableSeats();
        table.Seats.Should().HaveCount(4);

        table.GenerateRoundTableSeats(startAngleDeg: 45);
        table.Seats.Should().HaveCount(4);
        table.Seats.First().AngleDeg.Should().Be(45.0);
    }

    #endregion

    #region GenerateRectTableSeats Tests

    [Fact]
    public void GenerateRectTableSeats_Square_Should_Distribute_Evenly_Across_Four_Sides()
    {
        var table = VenueTable.Create(_layoutId, "Sq", TableShape.Square, capacity: 8, sortOrder: 0).Value;

        var result = table.GenerateRectTableSeats();

        result.IsSuccess.Should().BeTrue();
        table.Seats.Should().HaveCount(8);

        // 8 seats, 4 sides, 2 per side — top 2, right 2, bottom 2, left 2
        table.Seats.Take(2).Select(s => s.AngleDeg).Should().Equal(new double?[] { 270.0, 270.0 });
        table.Seats.Skip(2).Take(2).Select(s => s.AngleDeg).Should().Equal(new double?[] { 0.0, 0.0 });
        table.Seats.Skip(4).Take(2).Select(s => s.AngleDeg).Should().Equal(new double?[] { 90.0, 90.0 });
        table.Seats.Skip(6).Take(2).Select(s => s.AngleDeg).Should().Equal(new double?[] { 180.0, 180.0 });
    }

    [Fact]
    public void GenerateRectTableSeats_Rect_Should_Allow_Odd_Capacity()
    {
        var table = VenueTable.Create(_layoutId, "Head", TableShape.Rect, capacity: 7, sortOrder: 0).Value;

        var result = table.GenerateRectTableSeats();

        result.IsSuccess.Should().BeTrue();
        table.Seats.Should().HaveCount(7);
    }

    [Fact]
    public void GenerateRectTableSeats_OnRoundTable_Should_Fail()
    {
        var table = VenueTable.Create(_layoutId, "R1", TableShape.Round, 8, 0).Value;

        var result = table.GenerateRectTableSeats();

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_Should_Modify_Properties_And_MarkUpdated()
    {
        var table = VenueTable.Create(_layoutId, "T1", TableShape.Round, 8, 0).Value;

        var result = table.Update("VIP-1", TableShape.Round, 10, 2, null,
            geometry: "{\"centerX\": 300, \"centerY\": 300, \"radius\": 40}");

        result.IsSuccess.Should().BeTrue();
        table.Label.Should().Be("VIP-1");
        table.Capacity.Should().Be(10);
        table.SortOrder.Should().Be(2);
        table.Geometry.Should().Contain("centerX");
        table.UpdatedAt.Should().NotBeNull();
    }

    #endregion
}
