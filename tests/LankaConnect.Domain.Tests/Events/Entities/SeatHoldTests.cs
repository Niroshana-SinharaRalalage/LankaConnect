using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Tests.Events.Entities;

public class SeatHoldTests
{
    private readonly Guid _seatId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private const string SessionId = "session-abc-123";

    #region Create Tests

    [Fact]
    public void Create_WithValidData_Should_Return_Success()
    {
        var result = SeatHold.Create(_seatId, _userId, SessionId);

        result.IsSuccess.Should().BeTrue();
        var hold = result.Value;
        hold.SeatId.Should().Be(_seatId);
        hold.UserId.Should().Be(_userId);
        hold.SessionId.Should().Be(SessionId);
        hold.Status.Should().Be(SeatHoldStatus.Active);
        hold.HeldAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        hold.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.Add(SeatHold.HoldDuration), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithEmptySeatId_Should_Fail()
    {
        var result = SeatHold.Create(Guid.Empty, _userId, SessionId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Seat ID");
    }

    [Fact]
    public void Create_WithEmptyUserId_Should_Fail()
    {
        var result = SeatHold.Create(_seatId, Guid.Empty, SessionId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("User ID");
    }

    [Fact]
    public void Create_WithEmptySessionId_Should_Fail()
    {
        var result = SeatHold.Create(_seatId, _userId, "");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Session ID");
    }

    [Fact]
    public void Create_WithWhitespaceSessionId_Should_Fail()
    {
        var result = SeatHold.Create(_seatId, _userId, "   ");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Session ID");
    }

    [Fact]
    public void Create_WithTooLongSessionId_Should_Fail()
    {
        var longSession = new string('x', 101);
        var result = SeatHold.Create(_seatId, _userId, longSession);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("100 characters");
    }

    [Fact]
    public void Create_Should_Trim_SessionId()
    {
        var result = SeatHold.Create(_seatId, _userId, "  session-123  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.SessionId.Should().Be("session-123");
    }

    [Fact]
    public void HoldDuration_Should_Be_10_Minutes()
    {
        SeatHold.HoldDuration.Should().Be(TimeSpan.FromMinutes(10));
    }

    #endregion

    #region Expire Tests

    [Fact]
    public void Expire_WhenActive_Should_Succeed()
    {
        var hold = SeatHold.Create(_seatId, _userId, SessionId).Value;

        var result = hold.Expire();

        result.IsSuccess.Should().BeTrue();
        hold.Status.Should().Be(SeatHoldStatus.Expired);
    }

    [Fact]
    public void Expire_WhenAlreadyExpired_Should_Fail()
    {
        var hold = SeatHold.Create(_seatId, _userId, SessionId).Value;
        hold.Expire();

        var result = hold.Expire();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Expired");
    }

    [Fact]
    public void Expire_WhenConfirmed_Should_Fail()
    {
        var hold = SeatHold.Create(_seatId, _userId, SessionId).Value;
        hold.Confirm();

        var result = hold.Expire();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Confirmed");
    }

    #endregion

    #region Confirm Tests

    [Fact]
    public void Confirm_WhenActive_Should_Succeed()
    {
        var hold = SeatHold.Create(_seatId, _userId, SessionId).Value;

        var result = hold.Confirm();

        result.IsSuccess.Should().BeTrue();
        hold.Status.Should().Be(SeatHoldStatus.Confirmed);
    }

    [Fact]
    public void Confirm_WhenExpired_Should_Fail()
    {
        var hold = SeatHold.Create(_seatId, _userId, SessionId).Value;
        hold.Expire();

        var result = hold.Confirm();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Expired");
    }

    [Fact]
    public void Confirm_WhenReleased_Should_Fail()
    {
        var hold = SeatHold.Create(_seatId, _userId, SessionId).Value;
        hold.Release();

        var result = hold.Confirm();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Released");
    }

    #endregion

    #region Release Tests

    [Fact]
    public void Release_WhenActive_Should_Succeed()
    {
        var hold = SeatHold.Create(_seatId, _userId, SessionId).Value;

        var result = hold.Release();

        result.IsSuccess.Should().BeTrue();
        hold.Status.Should().Be(SeatHoldStatus.Released);
    }

    [Fact]
    public void Release_WhenConfirmed_Should_Fail()
    {
        var hold = SeatHold.Create(_seatId, _userId, SessionId).Value;
        hold.Confirm();

        var result = hold.Release();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Confirmed");
    }

    #endregion
}
