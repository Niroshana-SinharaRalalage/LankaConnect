using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Tests.Events;

/// <summary>
/// Phase 8 S8.2.A — tests for <see cref="Registration.SetPendingSeatAssignments"/>
/// and <see cref="Registration.ClearPendingSeatAssignments"/>.
///
/// The pending stash is set during the RSVP handler (before Stripe Checkout)
/// and cleared either by <see cref="Registration.ConfirmSeatAssignments"/>
/// after payment (success path) or by the checkout-expired webhook (timeout
/// path). The stash carries the buyer's intended seat assignments along
/// with the seat-hold session id so the webhook can still locate the
/// matching <see cref="SeatHold"/> rows minutes after RSVP.
/// </summary>
public class RegistrationPendingSeatAssignmentsTests
{
    private readonly Guid _eventId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private Registration BuildPreliminaryTwoAttendeeRegistration()
    {
        var aliceResult = AttendeeDetails.Create("Alice", AgeCategory.Adult, Gender.Female);
        var bobResult = AttendeeDetails.Create("Bob", AgeCategory.Adult, Gender.Male);
        var contact = RegistrationContact.Create(
            "alice@example.com", "8609780124", null, null, false).Value;
        var price = Money.Create(50m, Currency.USD).Value;

        return Registration.CreateWithAttendees(
            _eventId, _userId,
            new[] { aliceResult.Value, bobResult.Value },
            contact, price,
            isPaidEvent: true).Value;
    }

    [Fact]
    public void SetPendingSeatAssignments_HappyPath_StashesAssignmentsAndSessionId()
    {
        var reg = BuildPreliminaryTwoAttendeeRegistration();
        var seatA = Guid.NewGuid();
        var seatB = Guid.NewGuid();
        var sessionId = Guid.NewGuid().ToString();

        var result = reg.SetPendingSeatAssignments(sessionId, new[]
        {
            PendingSeatAssignment.Create(0, seatA, "A1").Value,
            PendingSeatAssignment.Create(1, seatB, "A2").Value,
        });

        result.IsSuccess.Should().BeTrue();
        reg.PendingSeatSessionId.Should().Be(sessionId);
        reg.PendingSeatAssignments.Should().HaveCount(2);
        reg.PendingSeatAssignments[0].SeatId.Should().Be(seatA);
        reg.PendingSeatAssignments[0].SeatLabel.Should().Be("A1");
        reg.PendingSeatAssignments[1].SeatId.Should().Be(seatB);
        reg.PendingSeatAssignments[1].SeatLabel.Should().Be("A2");
    }

    [Fact]
    public void SetPendingSeatAssignments_RejectsWhenStatusNotPreliminary()
    {
        // Build a Confirmed (free) registration — not paid, so status is Confirmed
        var contact = RegistrationContact.Create(
            "alice@example.com", "8609780124", null, null, false).Value;
        var price = Money.Create(0m, Currency.USD).Value;
        var aliceResult = AttendeeDetails.Create("Alice", AgeCategory.Adult, Gender.Female);
        var confirmed = Registration.CreateWithAttendees(
            _eventId, _userId, new[] { aliceResult.Value },
            contact, price, isPaidEvent: false).Value;
        confirmed.Status.Should().Be(RegistrationStatus.Confirmed);

        var result = confirmed.SetPendingSeatAssignments(
            "session-1",
            new[] { PendingSeatAssignment.Create(0, Guid.NewGuid(), "A1").Value });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Preliminary");
    }

    [Fact]
    public void SetPendingSeatAssignments_RejectsEmptySessionId()
    {
        var reg = BuildPreliminaryTwoAttendeeRegistration();

        var result = reg.SetPendingSeatAssignments(
            "",
            new[]
            {
                PendingSeatAssignment.Create(0, Guid.NewGuid(), "A1").Value,
                PendingSeatAssignment.Create(1, Guid.NewGuid(), "A2").Value,
            });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("session");
    }

    [Fact]
    public void SetPendingSeatAssignments_RejectsCountMismatch()
    {
        var reg = BuildPreliminaryTwoAttendeeRegistration(); // 2 attendees

        var result = reg.SetPendingSeatAssignments(
            "session-1",
            new[] { PendingSeatAssignment.Create(0, Guid.NewGuid(), "A1").Value });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("count");
    }

    [Fact]
    public void SetPendingSeatAssignments_RejectsDuplicateAttendeeIndex()
    {
        var reg = BuildPreliminaryTwoAttendeeRegistration();

        var result = reg.SetPendingSeatAssignments(
            "session-1",
            new[]
            {
                PendingSeatAssignment.Create(0, Guid.NewGuid(), "A1").Value,
                PendingSeatAssignment.Create(0, Guid.NewGuid(), "A2").Value, // duplicate index
            });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Duplicate");
    }

    [Fact]
    public void SetPendingSeatAssignments_RejectsAttendeeIndexOutOfRange()
    {
        var reg = BuildPreliminaryTwoAttendeeRegistration();

        var result = reg.SetPendingSeatAssignments(
            "session-1",
            new[]
            {
                PendingSeatAssignment.Create(0, Guid.NewGuid(), "A1").Value,
                PendingSeatAssignment.Create(5, Guid.NewGuid(), "A2").Value, // out of range
            });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("index");
    }

    [Fact]
    public void SetPendingSeatAssignments_ReplacesExistingStash()
    {
        // If the buyer re-tries RSVP (e.g., changes seats before Stripe redirect),
        // the second call should fully replace the stash, not append.
        var reg = BuildPreliminaryTwoAttendeeRegistration();
        var firstSession = "session-A";
        var secondSession = "session-B";

        reg.SetPendingSeatAssignments(firstSession, new[]
        {
            PendingSeatAssignment.Create(0, Guid.NewGuid(), "A1").Value,
            PendingSeatAssignment.Create(1, Guid.NewGuid(), "A2").Value,
        }).IsSuccess.Should().BeTrue();

        var newSeatA = Guid.NewGuid();
        var newSeatB = Guid.NewGuid();
        reg.SetPendingSeatAssignments(secondSession, new[]
        {
            PendingSeatAssignment.Create(0, newSeatA, "B5").Value,
            PendingSeatAssignment.Create(1, newSeatB, "B6").Value,
        }).IsSuccess.Should().BeTrue();

        reg.PendingSeatSessionId.Should().Be(secondSession);
        reg.PendingSeatAssignments.Should().HaveCount(2);
        reg.PendingSeatAssignments[0].SeatId.Should().Be(newSeatA);
        reg.PendingSeatAssignments[1].SeatId.Should().Be(newSeatB);
    }

    [Fact]
    public void ClearPendingSeatAssignments_HappyPath_RemovesStash()
    {
        var reg = BuildPreliminaryTwoAttendeeRegistration();
        reg.SetPendingSeatAssignments("session-1", new[]
        {
            PendingSeatAssignment.Create(0, Guid.NewGuid(), "A1").Value,
            PendingSeatAssignment.Create(1, Guid.NewGuid(), "A2").Value,
        });
        reg.PendingSeatAssignments.Should().HaveCount(2);
        reg.PendingSeatSessionId.Should().Be("session-1");

        reg.ClearPendingSeatAssignments();

        reg.PendingSeatAssignments.Should().BeEmpty();
        reg.PendingSeatSessionId.Should().BeNull();
    }

    [Fact]
    public void ClearPendingSeatAssignments_IsIdempotent_WhenNoStash()
    {
        var reg = BuildPreliminaryTwoAttendeeRegistration();
        reg.PendingSeatAssignments.Should().BeEmpty();

        // Should not throw
        reg.ClearPendingSeatAssignments();

        reg.PendingSeatAssignments.Should().BeEmpty();
        reg.PendingSeatSessionId.Should().BeNull();
    }
}
