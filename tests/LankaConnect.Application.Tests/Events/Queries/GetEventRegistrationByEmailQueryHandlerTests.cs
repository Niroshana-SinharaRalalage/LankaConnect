using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Queries.GetEventRegistrationByEmail;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.TestUtilities;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;

namespace LankaConnect.Application.Tests.Events.Queries;

/// <summary>
/// Tests for GetEventRegistrationByEmailQueryHandler
/// Phase 6A.15: Enhanced sign-up list UX with email validation
/// </summary>
public class GetEventRegistrationByEmailQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly GetEventRegistrationByEmailQueryHandler _handler;

    public GetEventRegistrationByEmailQueryHandlerTests()
    {
        _mockContext = new Mock<IApplicationDbContext>();
        _handler = new GetEventRegistrationByEmailQueryHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_EmailIsRegistered_ReturnsTrue()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var email = "test@example.com";
        var query = new GetEventRegistrationByEmailQuery(eventId, email);

        var registration = CreateRegistration(eventId, email);
        var registrations = new List<Registration> { registration }.AsQueryable().BuildMockDbSet();

        _mockContext.Setup(x => x.Registrations).Returns(registrations.Object);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_EmailIsNotRegistered_ReturnsFalse()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var email = "nonexistent@example.com";
        var query = new GetEventRegistrationByEmailQuery(eventId, email);

        var registration = CreateRegistration(eventId, "other@example.com");
        var registrations = new List<Registration> { registration }.AsQueryable().BuildMockDbSet();

        _mockContext.Setup(x => x.Registrations).Returns(registrations.Object);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NoRegistrationsForEvent_ReturnsFalse()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var email = "test@example.com";
        var query = new GetEventRegistrationByEmailQuery(eventId, email);

        // Empty list of registrations
        var registrations = new List<Registration>().AsQueryable().BuildMockDbSet();

        _mockContext.Setup(x => x.Registrations).Returns(registrations.Object);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DifferentEventWithSameEmail_ReturnsFalse()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var differentEventId = Guid.NewGuid();
        var email = "test@example.com";
        var query = new GetEventRegistrationByEmailQuery(eventId, email);

        var registration = CreateRegistration(differentEventId, email);
        var registrations = new List<Registration> { registration }.AsQueryable().BuildMockDbSet();

        _mockContext.Setup(x => x.Registrations).Returns(registrations.Object);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    private static Registration CreateRegistration(Guid eventId, string email)
    {
        var contact = RegistrationContact.Create(
            email: email,
            phoneNumber: "+94771234567",
            address: "Test Address"
        ).Value;

        var attendee = AttendeeDetails.Create(
            name: "Test User",
            age: 25
        ).Value;

        var totalPrice = Money.Create(0, Currency.LKR).Value;

        var registration = Registration.CreateWithAttendees(
            eventId: eventId,
            userId: Guid.NewGuid(),
            attendees: new[] { attendee },
            contact: contact,
            totalPrice: totalPrice,
            isPaidEvent: false
        ).Value;

        return registration;
    }
}
