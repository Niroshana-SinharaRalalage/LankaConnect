using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Domain.Tests.Events.Entities;

public class SeatReservationTests
{
    private readonly Guid _seatId = Guid.NewGuid();
    private readonly Guid _registrationId = Guid.NewGuid();
    private readonly Guid _eventId = Guid.NewGuid();

    #region Create Tests

    [Fact]
    public void Create_WithValidData_Should_Return_Success()
    {
        var result = SeatReservation.Create(_seatId, _registrationId, _eventId, 0);

        result.IsSuccess.Should().BeTrue();
        var reservation = result.Value;
        reservation.SeatId.Should().Be(_seatId);
        reservation.RegistrationId.Should().Be(_registrationId);
        reservation.EventId.Should().Be(_eventId);
        reservation.AttendeeIndex.Should().Be(0);
    }

    [Fact]
    public void Create_WithHigherAttendeeIndex_Should_Succeed()
    {
        var result = SeatReservation.Create(_seatId, _registrationId, _eventId, 5);

        result.IsSuccess.Should().BeTrue();
        result.Value.AttendeeIndex.Should().Be(5);
    }

    [Fact]
    public void Create_WithEmptySeatId_Should_Fail()
    {
        var result = SeatReservation.Create(Guid.Empty, _registrationId, _eventId, 0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Seat ID");
    }

    [Fact]
    public void Create_WithEmptyRegistrationId_Should_Fail()
    {
        var result = SeatReservation.Create(_seatId, Guid.Empty, _eventId, 0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Registration ID");
    }

    [Fact]
    public void Create_WithEmptyEventId_Should_Fail()
    {
        var result = SeatReservation.Create(_seatId, _registrationId, Guid.Empty, 0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Event ID");
    }

    [Fact]
    public void Create_WithNegativeAttendeeIndex_Should_Fail()
    {
        var result = SeatReservation.Create(_seatId, _registrationId, _eventId, -1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Attendee index");
    }

    #endregion
}
