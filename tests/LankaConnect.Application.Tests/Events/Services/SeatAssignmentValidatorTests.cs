using FluentAssertions;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Events.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Services;

/// <summary>
/// Phase 8 S8.2.B — tests for the seat-assignment validator that the RSVP
/// handlers (auth + anonymous) call before stashing pending seat assignments
/// on the registration.
/// </summary>
public class SeatAssignmentValidatorTests
{
    private readonly Mock<IVenueLayoutRepository> _layoutRepo = new();
    private readonly Mock<ISeatHoldRepository> _holdRepo = new();
    private readonly Mock<ISeatReservationRepository> _reservationRepo = new();
    private readonly SeatAssignmentValidator _sut;

    private readonly Guid _eventId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly string _sessionId = "session-abc";

    public SeatAssignmentValidatorTests()
    {
        _sut = new SeatAssignmentValidator(
            _layoutRepo.Object,
            _holdRepo.Object,
            _reservationRepo.Object,
            Mock.Of<ILogger<SeatAssignmentValidator>>());
    }

    /// <summary>
    /// Builds a 1-zone Theater layout with the given seat IDs / labels.
    /// </summary>
    private VenueLayout BuildLayoutWithZoneSeats(params (Guid id, string label)[] seats)
    {
        var layout = VenueLayout.Create("Test Layout", LayoutType.Theater, _userId, _eventId).Value;
        var zone = layout.AddZone("Main", "#fff", 1).Value;
        // Generate enough seats for the supplied count, then we'll force the
        // IDs/labels via reflection so the validator's seatId lookup matches.
        layout.GenerateTheaterSeats(zone.Id, rows: 1, seatsPerRow: seats.Length);

        var seatList = layout.Zones[0].Seats.ToList();
        for (var i = 0; i < seats.Length; i++)
        {
            ForceSet(seatList[i], "Id", seats[i].id);
            ForceSet(seatList[i], "Label", seats[i].label);
        }
        return layout;
    }

    private static void ForceSet(object target, string property, object value)
    {
        var prop = target.GetType().GetProperty(property,
            System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Instance);
        prop!.SetValue(target, value);
    }

    private SeatHold BuildHold(Guid seatId, string sessionId)
    {
        return SeatHold.Create(seatId, _userId, sessionId).Value;
    }

    [Fact]
    public async Task Validate_HappyPath_BuildsPendingAssignmentsInOrder()
    {
        var seat1 = Guid.NewGuid();
        var seat2 = Guid.NewGuid();
        var layout = BuildLayoutWithZoneSeats((seat1, "A1"), (seat2, "A2"));
        _layoutRepo.Setup(r => r.GetAssignedLayoutForEventAsync(_eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(layout);
        _holdRepo.Setup(r => r.GetActiveHoldsBySessionAsync(_sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { BuildHold(seat1, _sessionId), BuildHold(seat2, _sessionId) });
        _reservationRepo.Setup(r => r.GetReservedSeatIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Guid>());

        var result = await _sut.ValidateAndBuildAssignmentsAsync(
            _eventId, _sessionId, new[] { seat1, seat2 }, attendeeCount: 2);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].AttendeeIndex.Should().Be(0);
        result.Value[0].SeatId.Should().Be(seat1);
        result.Value[0].SeatLabel.Should().Be("A1");
        result.Value[1].AttendeeIndex.Should().Be(1);
        result.Value[1].SeatId.Should().Be(seat2);
        result.Value[1].SeatLabel.Should().Be("A2");
    }

    [Fact]
    public async Task Validate_LayoutNotFound_Should_Fail()
    {
        _layoutRepo.Setup(r => r.GetAssignedLayoutForEventAsync(_eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VenueLayout?)null);

        var result = await _sut.ValidateAndBuildAssignmentsAsync(
            _eventId, _sessionId, new[] { Guid.NewGuid() }, attendeeCount: 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("layout");
    }

    [Fact]
    public async Task Validate_CountMismatch_Should_Fail()
    {
        var seat1 = Guid.NewGuid();
        var layout = BuildLayoutWithZoneSeats((seat1, "A1"));
        _layoutRepo.Setup(r => r.GetAssignedLayoutForEventAsync(_eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(layout);

        // 1 seat ID but 2 attendees
        var result = await _sut.ValidateAndBuildAssignmentsAsync(
            _eventId, _sessionId, new[] { seat1 }, attendeeCount: 2);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("count");
    }

    [Fact]
    public async Task Validate_SeatNotInLayout_Should_Fail()
    {
        var seat1 = Guid.NewGuid();
        var seatNotInLayout = Guid.NewGuid();
        var layout = BuildLayoutWithZoneSeats((seat1, "A1"));
        _layoutRepo.Setup(r => r.GetAssignedLayoutForEventAsync(_eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(layout);

        var result = await _sut.ValidateAndBuildAssignmentsAsync(
            _eventId, _sessionId, new[] { seat1, seatNotInLayout }, attendeeCount: 2);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("layout");
    }

    [Fact]
    public async Task Validate_SeatNotHeldInSession_Should_Fail()
    {
        // Seat is in the layout but the session doesn't hold it.
        var seat1 = Guid.NewGuid();
        var seat2 = Guid.NewGuid();
        var layout = BuildLayoutWithZoneSeats((seat1, "A1"), (seat2, "A2"));
        _layoutRepo.Setup(r => r.GetAssignedLayoutForEventAsync(_eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(layout);
        // Session only holds seat1 — seat2 is "borrowed" by the request.
        _holdRepo.Setup(r => r.GetActiveHoldsBySessionAsync(_sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { BuildHold(seat1, _sessionId) });

        var result = await _sut.ValidateAndBuildAssignmentsAsync(
            _eventId, _sessionId, new[] { seat1, seat2 }, attendeeCount: 2);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("held");
    }

    [Fact]
    public async Task Validate_SeatAlreadyReserved_Should_Fail()
    {
        var seat1 = Guid.NewGuid();
        var seat2 = Guid.NewGuid();
        var layout = BuildLayoutWithZoneSeats((seat1, "A1"), (seat2, "A2"));
        _layoutRepo.Setup(r => r.GetAssignedLayoutForEventAsync(_eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(layout);
        _holdRepo.Setup(r => r.GetActiveHoldsBySessionAsync(_sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { BuildHold(seat1, _sessionId), BuildHold(seat2, _sessionId) });
        // seat2 is already reserved by another buyer (defence in depth).
        _reservationRepo.Setup(r => r.GetReservedSeatIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { seat2 });

        var result = await _sut.ValidateAndBuildAssignmentsAsync(
            _eventId, _sessionId, new[] { seat1, seat2 }, attendeeCount: 2);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("reserved");
    }

    [Fact]
    public async Task Validate_EmptySeatIds_Should_Fail()
    {
        var result = await _sut.ValidateAndBuildAssignmentsAsync(
            _eventId, _sessionId, Array.Empty<Guid>(), attendeeCount: 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Validate_EmptySessionId_Should_Fail()
    {
        var result = await _sut.ValidateAndBuildAssignmentsAsync(
            _eventId, "", new[] { Guid.NewGuid() }, attendeeCount: 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("session");
    }
}
