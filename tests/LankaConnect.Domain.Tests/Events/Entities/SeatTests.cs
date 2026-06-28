using LankaConnect.Products.LankaEvents.Domain.Entities;

namespace LankaConnect.Domain.Tests.Events.Entities;

public class SeatTests
{
    private readonly Guid _zoneId = Guid.NewGuid();

    #region Create Tests

    [Fact]
    public void Create_WithValidData_Should_Return_Success()
    {
        // Act
        var result = Seat.Create(_zoneId, "A", 1, "A1", 0);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var seat = result.Value;
        seat.VenueZoneId.Should().Be(_zoneId);
        seat.Row.Should().Be("A");
        seat.Number.Should().Be(1);
        seat.Label.Should().Be("A1");
        seat.SortOrder.Should().Be(0);
        seat.IsEnabled.Should().BeTrue();
        seat.IsAccessible.Should().BeFalse();
    }

    [Fact]
    public void Create_WithAccessibleFlag_Should_Set_IsAccessible()
    {
        var result = Seat.Create(_zoneId, "A", 1, "A1", 0, isAccessible: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsAccessible.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyZoneId_Should_Return_Failure()
    {
        var result = Seat.Create(Guid.Empty, "A", 1, "A1", 0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Venue zone ID");
    }

    [Fact]
    public void Create_WithEmptyRow_Should_Return_Failure()
    {
        var result = Seat.Create(_zoneId, "", 1, "A1", 0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Row");
    }

    [Fact]
    public void Create_WithZeroNumber_Should_Return_Failure()
    {
        var result = Seat.Create(_zoneId, "A", 0, "A1", 0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("number");
    }

    [Fact]
    public void Create_WithNegativeNumber_Should_Return_Failure()
    {
        var result = Seat.Create(_zoneId, "A", -1, "A1", 0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("number");
    }

    [Fact]
    public void Create_WithEmptyLabel_Should_Return_Failure()
    {
        var result = Seat.Create(_zoneId, "A", 1, "", 0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("label");
    }

    [Fact]
    public void Create_WithNegativeSortOrder_Should_Return_Failure()
    {
        var result = Seat.Create(_zoneId, "A", 1, "A1", -1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Sort order");
    }

    [Fact]
    public void Create_Should_Trim_Row_And_Label()
    {
        var result = Seat.Create(_zoneId, " A ", 1, " A1 ", 0);

        result.IsSuccess.Should().BeTrue();
        result.Value.Row.Should().Be("A");
        result.Value.Label.Should().Be("A1");
    }

    #endregion

    #region Disable / Enable Tests

    [Fact]
    public void Disable_WhenEnabled_Should_Succeed()
    {
        var seat = Seat.Create(_zoneId, "A", 1, "A1", 0).Value;

        var result = seat.Disable();

        result.IsSuccess.Should().BeTrue();
        seat.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Disable_WhenAlreadyDisabled_Should_Fail()
    {
        var seat = Seat.Create(_zoneId, "A", 1, "A1", 0).Value;
        seat.Disable();

        var result = seat.Disable();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already disabled");
    }

    [Fact]
    public void Enable_WhenDisabled_Should_Succeed()
    {
        var seat = Seat.Create(_zoneId, "A", 1, "A1", 0).Value;
        seat.Disable();

        var result = seat.Enable();

        result.IsSuccess.Should().BeTrue();
        seat.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Enable_WhenAlreadyEnabled_Should_Fail()
    {
        var seat = Seat.Create(_zoneId, "A", 1, "A1", 0).Value;

        var result = seat.Enable();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already enabled");
    }

    #endregion

    #region Accessibility Tests

    [Fact]
    public void SetAccessible_Should_Update_Flag()
    {
        var seat = Seat.Create(_zoneId, "A", 1, "A1", 0).Value;
        seat.IsAccessible.Should().BeFalse();

        var result = seat.SetAccessible(true);

        result.IsSuccess.Should().BeTrue();
        seat.IsAccessible.Should().BeTrue();
    }

    [Fact]
    public void SetAccessible_ToFalse_Should_Clear_Flag()
    {
        var seat = Seat.Create(_zoneId, "A", 1, "A1", 0, isAccessible: true).Value;

        var result = seat.SetAccessible(false);

        result.IsSuccess.Should().BeTrue();
        seat.IsAccessible.Should().BeFalse();
    }

    #endregion
}
