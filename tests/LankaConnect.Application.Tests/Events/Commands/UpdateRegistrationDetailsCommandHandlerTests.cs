using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.RsvpToEvent;
using LankaConnect.Application.Events.Commands.UpdateRegistrationDetails;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Phase 6A.14: TDD Tests for UpdateRegistrationDetailsCommand and Handler
/// Tests the application layer orchestration of registration updates
/// </summary>
public class UpdateRegistrationDetailsCommandHandlerTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly UpdateRegistrationDetailsCommandHandler _handler;

    public UpdateRegistrationDetailsCommandHandlerTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _handler = new UpdateRegistrationDetailsCommandHandler(
            _mockEventRepository.Object,
            _mockUnitOfWork.Object);
    }

    private Event CreatePublishedEventWithRegistration(Guid userId, out Registration registration)
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var organizerId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = DateTime.UtcNow.AddDays(7).AddHours(4);

        var eventResult = Event.Create(title, description, startDate, endDate, organizerId, 100);
        var @event = eventResult.Value;
        @event.Publish();

        // Register the user with multi-attendee format
        var attendees = new List<AttendeeDetails>
        {
            AttendeeDetails.Create("John Doe", 30).Value
        };
        var contact = RegistrationContact.Create("john@example.com", "555-1234", null).Value;
        @event.RegisterWithAttendees(userId, attendees, contact);

        registration = @event.Registrations.First(r => r.UserId == userId);
        return @event;
    }

    private Event CreatePublishedEventWithPaidRegistration(Guid userId, out Registration registration)
    {
        var title = EventTitle.Create("Paid Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var organizerId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = DateTime.UtcNow.AddDays(7).AddHours(4);

        var eventResult = Event.Create(title, description, startDate, endDate, organizerId, 100);
        var @event = eventResult.Value;

        // Set up pricing to make it a paid event
        var pricing = TicketPricing.CreateSinglePrice(Money.Create(50m, Currency.USD).Value).Value;
        @event.SetDualPricing(pricing);
        @event.Publish();

        // Register the user with multi-attendee format for paid event
        var attendees = new List<AttendeeDetails>
        {
            AttendeeDetails.Create("John Doe", 30).Value,
            AttendeeDetails.Create("Jane Doe", 28).Value
        };
        var contact = RegistrationContact.Create("john@example.com", "555-1234", null).Value;
        @event.RegisterWithAttendees(userId, attendees, contact);

        registration = @event.Registrations.First(r => r.UserId == userId);

        // Complete the payment
        registration.SetStripeCheckoutSession("cs_test_123");
        registration.CompletePayment("pi_test_123");

        return @event;
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateRegistration()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var @event = CreatePublishedEventWithRegistration(userId, out var registration);

        var command = new UpdateRegistrationDetailsCommand(
            @event.Id,
            userId,
            new List<AttendeeDto> { new("Jane Smith", 25) },
            "jane@example.com",
            "555-9999",
            "123 Main St");

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        registration.Attendees.Should().HaveCount(1);
        registration.Attendees[0].Name.Should().Be("Jane Smith");
        registration.Attendees[0].Age.Should().Be(25);
        registration.Contact!.Email.Should().Be("jane@example.com");
        registration.Contact.PhoneNumber.Should().Be("555-9999");
        registration.Contact.Address.Should().Be("123 Main St");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEventNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdateRegistrationDetailsCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new List<AttendeeDto> { new("John Doe", 30) },
            "john@example.com",
            "555-1234",
            null);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain($"Event with ID {command.EventId} not found");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotRegistered_ShouldReturnFailure()
    {
        // Arrange
        var registeredUserId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var @event = CreatePublishedEventWithRegistration(registeredUserId, out _);

        var command = new UpdateRegistrationDetailsCommand(
            @event.Id,
            differentUserId, // Different user who is not registered
            new List<AttendeeDto> { new("John Doe", 30) },
            "john@example.com",
            "555-1234",
            null);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("active registration"));
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var @event = CreatePublishedEventWithRegistration(userId, out _);

        var command = new UpdateRegistrationDetailsCommand(
            @event.Id,
            userId,
            new List<AttendeeDto> { new("John Doe", 30) },
            "invalid-email", // Invalid email
            "555-1234",
            null);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("email") || e.Contains("Email"));
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyAttendees_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var @event = CreatePublishedEventWithRegistration(userId, out _);

        var command = new UpdateRegistrationDetailsCommand(
            @event.Id,
            userId,
            new List<AttendeeDto>(), // Empty attendees
            "john@example.com",
            "555-1234",
            null);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("attendee"));
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PaidRegistration_SameAttendeeCount_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var @event = CreatePublishedEventWithPaidRegistration(userId, out var registration);

        // Same count (2) but different names
        var command = new UpdateRegistrationDetailsCommand(
            @event.Id,
            userId,
            new List<AttendeeDto>
            {
                new("John Smith", 31),
                new("Jane Smith", 29)
            },
            "smith@example.com",
            "555-8888",
            null);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue("same attendee count on paid registration should be allowed");
        registration.Attendees.Should().HaveCount(2);
        registration.Attendees[0].Name.Should().Be("John Smith");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PaidRegistration_ChangingAttendeeCount_ShouldFail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var @event = CreatePublishedEventWithPaidRegistration(userId, out _);

        // Trying to add a third attendee to paid registration
        var command = new UpdateRegistrationDetailsCommand(
            @event.Id,
            userId,
            new List<AttendeeDto>
            {
                new("John Doe", 30),
                new("Jane Doe", 28),
                new("New Person", 25) // Adding third attendee
            },
            "john@example.com",
            "555-1234",
            null);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("paid") || e.Contains("count"));
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_FreeEvent_AddingAttendees_WithCapacity_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var @event = CreatePublishedEventWithRegistration(userId, out var registration);

        // Adding more attendees to free event
        var command = new UpdateRegistrationDetailsCommand(
            @event.Id,
            userId,
            new List<AttendeeDto>
            {
                new("Person 1", 25),
                new("Person 2", 30),
                new("Person 3", 35)
            },
            "john@example.com",
            "555-1234",
            null);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue("free event should allow adding attendees");
        registration.Attendees.Should().HaveCount(3);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidAttendeeName_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var @event = CreatePublishedEventWithRegistration(userId, out _);

        var command = new UpdateRegistrationDetailsCommand(
            @event.Id,
            userId,
            new List<AttendeeDto> { new("", 30) }, // Empty name
            "john@example.com",
            "555-1234",
            null);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("name") || e.Contains("Name"));
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithMoreThan10Attendees_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var @event = CreatePublishedEventWithRegistration(userId, out _);

        var attendees = Enumerable.Range(1, 11)
            .Select(i => new AttendeeDto($"Person {i}", 25 + i))
            .ToList();

        var command = new UpdateRegistrationDetailsCommand(
            @event.Id,
            userId,
            attendees,
            "john@example.com",
            "555-1234",
            null);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("10") || e.Contains("maximum"));
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRegistrationCancelled_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var @event = CreatePublishedEventWithRegistration(userId, out var registration);
        registration.Cancel();

        var command = new UpdateRegistrationDetailsCommand(
            @event.Id,
            userId,
            new List<AttendeeDto> { new("John Doe", 30) },
            "john@example.com",
            "555-1234",
            null);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("active registration"));
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
