using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Tests.Events;

/// <summary>
/// Phase 8 S8.1 — tests for the new Registration.ConfirmSeatAssignments
/// aggregate method. Webhook-side seat-binding hands the registration a list
/// of (attendeeIndex, seatId, seatLabel) tuples; the method binds them to
/// the matching <see cref="AttendeeDetails"/> values via the new
/// <see cref="AttendeeDetails.WithSeat"/> method, raises
/// <see cref="SeatsReservedEvent"/>, and is idempotent on retry (no event,
/// no error if the same assignments are re-applied).
/// </summary>
public class RegistrationConfirmSeatAssignmentsTests
{
    private readonly Guid _eventId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private Registration BuildConfirmedTwoAttendeeRegistration()
    {
        var aliceResult = AttendeeDetails.Create("Alice", AgeCategory.Adult, Gender.Female);
        var bobResult = AttendeeDetails.Create("Bob", AgeCategory.Adult, Gender.Male);
        aliceResult.IsSuccess.Should().BeTrue();
        bobResult.IsSuccess.Should().BeTrue();

        var contact = RegistrationContact.Create(
            "alice@example.com", "8609780124", null, null, false).Value;
        var price = Money.Create(50m, Currency.USD).Value;

        var registrationResult = Registration.CreateWithAttendees(
            _eventId,
            _userId,
            new[] { aliceResult.Value, bobResult.Value },
            contact,
            price,
            isPaidEvent: true);
        registrationResult.IsSuccess.Should().BeTrue();
        var registration = registrationResult.Value;

        // Confirm payment so the registration is Status=Confirmed.
        var completed = registration.CompletePayment("pi_test_123");
        completed.IsSuccess.Should().BeTrue();
        registration.ClearDomainEvents(); // Drop CompletePayment's domain events for cleaner assertions.
        return registration;
    }

    [Fact]
    public void ConfirmSeatAssignments_HappyPath_BindsSeatsAndRaisesEvent()
    {
        var registration = BuildConfirmedTwoAttendeeRegistration();
        var seatA = Guid.NewGuid();
        var seatB = Guid.NewGuid();

        var result = registration.ConfirmSeatAssignments(new[]
        {
            (AttendeeIndex: 0, SeatId: seatA, SeatLabel: "A1"),
            (AttendeeIndex: 1, SeatId: seatB, SeatLabel: "A2"),
        });

        result.IsSuccess.Should().BeTrue();
        registration.Attendees[0].SeatId.Should().Be(seatA);
        registration.Attendees[0].SeatLabel.Should().Be("A1");
        registration.Attendees[1].SeatId.Should().Be(seatB);
        registration.Attendees[1].SeatLabel.Should().Be("A2");

        registration.DomainEvents.Should().ContainSingle(e => e is SeatsReservedEvent);
    }

    [Fact]
    public void ConfirmSeatAssignments_RejectsWhenStatusNotConfirmed()
    {
        // Build a Preliminary registration (paid, not yet completed)
        var aliceResult = AttendeeDetails.Create("Alice", AgeCategory.Adult, Gender.Female);
        var contact = RegistrationContact.Create(
            "alice@example.com", "8609780124", null, null, false).Value;
        var price = Money.Create(50m, Currency.USD).Value;
        var preliminary = Registration.CreateWithAttendees(
            _eventId, _userId, new[] { aliceResult.Value },
            contact, price, isPaidEvent: true).Value;
        preliminary.Status.Should().Be(RegistrationStatus.Preliminary);

        var result = preliminary.ConfirmSeatAssignments(new[]
        {
            (AttendeeIndex: 0, SeatId: Guid.NewGuid(), SeatLabel: "A1"),
        });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Confirmed");
    }

    [Fact]
    public void ConfirmSeatAssignments_RejectsCountMismatch_FewerAssignments()
    {
        var registration = BuildConfirmedTwoAttendeeRegistration(); // 2 attendees

        var result = registration.ConfirmSeatAssignments(new[]
        {
            (AttendeeIndex: 0, SeatId: Guid.NewGuid(), SeatLabel: "A1"),
        });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("count");
    }

    [Fact]
    public void ConfirmSeatAssignments_RejectsCountMismatch_MoreAssignments()
    {
        var registration = BuildConfirmedTwoAttendeeRegistration(); // 2 attendees

        var result = registration.ConfirmSeatAssignments(new[]
        {
            (AttendeeIndex: 0, SeatId: Guid.NewGuid(), SeatLabel: "A1"),
            (AttendeeIndex: 1, SeatId: Guid.NewGuid(), SeatLabel: "A2"),
            (AttendeeIndex: 2, SeatId: Guid.NewGuid(), SeatLabel: "A3"),
        });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("count");
    }

    [Fact]
    public void ConfirmSeatAssignments_RejectsDuplicateAttendeeIndex()
    {
        var registration = BuildConfirmedTwoAttendeeRegistration();

        var result = registration.ConfirmSeatAssignments(new[]
        {
            (AttendeeIndex: 0, SeatId: Guid.NewGuid(), SeatLabel: "A1"),
            (AttendeeIndex: 0, SeatId: Guid.NewGuid(), SeatLabel: "A2"), // duplicate index
        });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Duplicate");
    }

    [Fact]
    public void ConfirmSeatAssignments_RejectsAttendeeIndexOutOfRange()
    {
        var registration = BuildConfirmedTwoAttendeeRegistration();

        var result = registration.ConfirmSeatAssignments(new[]
        {
            (AttendeeIndex: 0, SeatId: Guid.NewGuid(), SeatLabel: "A1"),
            (AttendeeIndex: 5, SeatId: Guid.NewGuid(), SeatLabel: "A2"), // out of range
        });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("index");
    }

    [Fact]
    public void ConfirmSeatAssignments_RejectsEmptySeatId()
    {
        var registration = BuildConfirmedTwoAttendeeRegistration();

        var result = registration.ConfirmSeatAssignments(new[]
        {
            (AttendeeIndex: 0, SeatId: Guid.Empty, SeatLabel: "A1"),
            (AttendeeIndex: 1, SeatId: Guid.NewGuid(), SeatLabel: "A2"),
        });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Seat");
    }

    [Fact]
    public void ConfirmSeatAssignments_IsIdempotent_WhenAttendeesAlreadyHaveSameSeats()
    {
        // Webhook retries: applying the same assignments twice must succeed
        // and NOT raise a duplicate SeatsReservedEvent.
        var registration = BuildConfirmedTwoAttendeeRegistration();
        var seatA = Guid.NewGuid();
        var seatB = Guid.NewGuid();
        var assignments = new[]
        {
            (AttendeeIndex: 0, SeatId: seatA, SeatLabel: "A1"),
            (AttendeeIndex: 1, SeatId: seatB, SeatLabel: "A2"),
        };

        var first = registration.ConfirmSeatAssignments(assignments);
        first.IsSuccess.Should().BeTrue();
        registration.DomainEvents.Should().ContainSingle(e => e is SeatsReservedEvent);
        registration.ClearDomainEvents();

        var second = registration.ConfirmSeatAssignments(assignments);
        second.IsSuccess.Should().BeTrue();
        // Idempotent: second call did NOT raise the event again.
        registration.DomainEvents.Should().NotContain(e => e is SeatsReservedEvent);
        // State unchanged.
        registration.Attendees[0].SeatId.Should().Be(seatA);
        registration.Attendees[1].SeatId.Should().Be(seatB);
    }
}
