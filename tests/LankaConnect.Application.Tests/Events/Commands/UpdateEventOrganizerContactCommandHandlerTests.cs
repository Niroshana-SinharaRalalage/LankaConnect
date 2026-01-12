using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.UpdateEventOrganizerContact;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

public class UpdateEventOrganizerContactCommandHandlerTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly UpdateEventOrganizerContactCommandHandler _handler;

    public UpdateEventOrganizerContactCommandHandlerTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _handler = new UpdateEventOrganizerContactCommandHandler(
            _mockEventRepository.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_WithAllContactDetails_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();
        var eventId = @event.Id;

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var command = new UpdateEventOrganizerContactCommand(
            EventId: eventId,
            PublishOrganizerContact: true,
            OrganizerContactName: "John Organizer",
            OrganizerContactPhone: "+1-555-1234",
            OrganizerContactEmail: "john@example.com");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.PublishOrganizerContact.Should().BeTrue();
        @event.OrganizerContactName.Should().Be("John Organizer");
        @event.OrganizerContactPhone.Should().Be("+1-555-1234");
        @event.OrganizerContactEmail.Should().Be("john@example.com");

        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRequest_WithOnlyEmail_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();
        var eventId = @event.Id;

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var command = new UpdateEventOrganizerContactCommand(
            EventId: eventId,
            PublishOrganizerContact: true,
            OrganizerContactName: "Jane Organizer",
            OrganizerContactPhone: null,
            OrganizerContactEmail: "jane@example.com");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.PublishOrganizerContact.Should().BeTrue();
        @event.OrganizerContactName.Should().Be("Jane Organizer");
        @event.OrganizerContactPhone.Should().BeNull();
        @event.OrganizerContactEmail.Should().Be("jane@example.com");
    }

    [Fact]
    public async Task Handle_ValidRequest_WithOnlyPhone_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();
        var eventId = @event.Id;

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var command = new UpdateEventOrganizerContactCommand(
            EventId: eventId,
            PublishOrganizerContact: true,
            OrganizerContactName: "Bob Organizer",
            OrganizerContactPhone: "+1-555-9999",
            OrganizerContactEmail: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.PublishOrganizerContact.Should().BeTrue();
        @event.OrganizerContactName.Should().Be("Bob Organizer");
        @event.OrganizerContactPhone.Should().Be("+1-555-9999");
        @event.OrganizerContactEmail.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UnpublishContact_ShouldClearAllDetails()
    {
        // Arrange
        var @event = CreateValidEvent();
        @event.SetOrganizerContactDetails(true, "Existing Name", "+1-555-0000", "existing@example.com");
        var eventId = @event.Id;

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var command = new UpdateEventOrganizerContactCommand(
            EventId: eventId,
            PublishOrganizerContact: false,
            OrganizerContactName: null,
            OrganizerContactPhone: null,
            OrganizerContactEmail: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.PublishOrganizerContact.Should().BeFalse();
        @event.OrganizerContactName.Should().BeNull();
        @event.OrganizerContactPhone.Should().BeNull();
        @event.OrganizerContactEmail.Should().BeNull();
    }

    [Fact]
    public async Task Handle_EventNotFound_ShouldReturnFailure()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var command = new UpdateEventOrganizerContactCommand(
            EventId: eventId,
            PublishOrganizerContact: true,
            OrganizerContactName: "Test",
            OrganizerContactPhone: "+1-555-0000",
            OrganizerContactEmail: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event not found");

        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PublishWithoutName_ShouldReturnFailure()
    {
        // Arrange
        var @event = CreateValidEvent();
        var eventId = @event.Id;

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var command = new UpdateEventOrganizerContactCommand(
            EventId: eventId,
            PublishOrganizerContact: true,
            OrganizerContactName: null,  // Missing name
            OrganizerContactPhone: "+1-555-0000",
            OrganizerContactEmail: "test@example.com");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Contact name is required");

        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PublishWithoutContactMethod_ShouldReturnFailure()
    {
        // Arrange
        var @event = CreateValidEvent();
        var eventId = @event.Id;

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var command = new UpdateEventOrganizerContactCommand(
            EventId: eventId,
            PublishOrganizerContact: true,
            OrganizerContactName: "Test Name",
            OrganizerContactPhone: null,  // No phone
            OrganizerContactEmail: null);  // No email

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("At least one contact method");

        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PublishWithInvalidEmail_ShouldReturnFailure()
    {
        // Arrange
        var @event = CreateValidEvent();
        var eventId = @event.Id;

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var command = new UpdateEventOrganizerContactCommand(
            EventId: eventId,
            PublishOrganizerContact: true,
            OrganizerContactName: "Test Name",
            OrganizerContactPhone: null,
            OrganizerContactEmail: "invalid-email");  // Invalid email

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email");

        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UpdateExistingContact_ShouldOverwritePreviousData()
    {
        // Arrange
        var @event = CreateValidEvent();
        @event.SetOrganizerContactDetails(true, "Old Name", "+1-555-0000", "old@example.com");
        var eventId = @event.Id;

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var command = new UpdateEventOrganizerContactCommand(
            EventId: eventId,
            PublishOrganizerContact: true,
            OrganizerContactName: "New Name",
            OrganizerContactPhone: "+1-555-9999",
            OrganizerContactEmail: "new@example.com");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContactName.Should().Be("New Name");
        @event.OrganizerContactPhone.Should().Be("+1-555-9999");
        @event.OrganizerContactEmail.Should().Be("new@example.com");
    }

    private Event CreateValidEvent()
    {
        var titleResult = EventTitle.Create("Test Event");
        var descriptionResult = EventDescription.Create("Test Description");

        var eventResult = Event.Create(
            titleResult.Value,
            descriptionResult.Value,
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(2),
            Guid.NewGuid(), // organizerId
            100, // capacity
            null, // location
            EventCategory.Community
        );

        return eventResult.Value;
    }
}
