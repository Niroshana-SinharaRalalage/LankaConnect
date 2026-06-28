using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.UpdateEventOrganizerContact;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

public class UpdateEventOrganizerContactCommandHandlerTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<UpdateEventOrganizerContactCommandHandler>> _mockLogger;
    private readonly UpdateEventOrganizerContactCommandHandler _handler;

    public UpdateEventOrganizerContactCommandHandlerTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<UpdateEventOrganizerContactCommandHandler>>();
        _handler = new UpdateEventOrganizerContactCommandHandler(
            _mockEventRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_WithSingleContact_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();
        var eventId = @event.Id;

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var command = new UpdateEventOrganizerContactCommand(
            EventId: eventId,
            PublishOrganizerContact: true,
            Contacts: new List<OrganizerContactRequest>
            {
                new("John Organizer", "john@example.com", "+1-555-1234")
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.PublishOrganizerContact.Should().BeTrue();
        @event.OrganizerContacts.Should().HaveCount(1);
        @event.OrganizerContactName.Should().Be("John Organizer");
        @event.OrganizerContactPhone.Should().Be("+1-555-1234");
        @event.OrganizerContactEmail.Should().Be("john@example.com");

        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRequest_WithMultipleContacts_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();
        var eventId = @event.Id;

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var command = new UpdateEventOrganizerContactCommand(
            EventId: eventId,
            PublishOrganizerContact: true,
            Contacts: new List<OrganizerContactRequest>
            {
                new("Primary Contact", "primary@example.com", "+1-555-0001"),
                new("Secondary Contact", "secondary@example.com")
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContacts.Should().HaveCount(2);
        @event.OrganizerContacts[0].IsPrimary.Should().BeFalse("no contact explicitly marked primary");
        @event.OrganizerContacts[1].IsPrimary.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ValidRequest_WithOnlyEmail_ShouldSucceed()
    {
        // Arrange
        var @event = CreateValidEvent();
        var eventId = @event.Id;

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var command = new UpdateEventOrganizerContactCommand(
            EventId: eventId,
            PublishOrganizerContact: true,
            Contacts: new List<OrganizerContactRequest>
            {
                new("Jane Organizer", "jane@example.com")
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
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
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var command = new UpdateEventOrganizerContactCommand(
            EventId: eventId,
            PublishOrganizerContact: true,
            Contacts: new List<OrganizerContactRequest>
            {
                new("Bob Organizer", ContactPhone: "+1-555-9999")
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContactName.Should().Be("Bob Organizer");
        @event.OrganizerContactPhone.Should().Be("+1-555-9999");
        @event.OrganizerContactEmail.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UnpublishContact_ShouldClearAllContacts()
    {
        // Arrange
        var @event = CreateValidEvent();
        @event.SetOrganizerContacts(true,
            new List<(string, string?, string?)> { ("Existing Name", "existing@example.com", "+1-555-0000") });
        var eventId = @event.Id;

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var command = new UpdateEventOrganizerContactCommand(
            EventId: eventId,
            PublishOrganizerContact: false,
            Contacts: new List<OrganizerContactRequest>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.PublishOrganizerContact.Should().BeFalse();
        @event.OrganizerContacts.Should().BeEmpty();
        @event.OrganizerContactName.Should().BeNull();
    }

    [Fact]
    public async Task Handle_EventNotFound_ShouldReturnFailure()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var command = new UpdateEventOrganizerContactCommand(
            EventId: eventId,
            PublishOrganizerContact: true,
            Contacts: new List<OrganizerContactRequest>
            {
                new("Test", ContactPhone: "+1-555-0000")
            });

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
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var command = new UpdateEventOrganizerContactCommand(
            EventId: eventId,
            PublishOrganizerContact: true,
            Contacts: new List<OrganizerContactRequest>
            {
                new("", "test@example.com", "+1-555-0000")
            });

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
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var command = new UpdateEventOrganizerContactCommand(
            EventId: eventId,
            PublishOrganizerContact: true,
            Contacts: new List<OrganizerContactRequest>
            {
                new("Test Name")  // No email, no phone
            });

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
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var command = new UpdateEventOrganizerContactCommand(
            EventId: eventId,
            PublishOrganizerContact: true,
            Contacts: new List<OrganizerContactRequest>
            {
                new("Test Name", "invalid-email")
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email");

        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UpdateExistingContacts_ShouldReplaceAll()
    {
        // Arrange
        var @event = CreateValidEvent();
        @event.SetOrganizerContacts(true,
            new List<(string, string?, string?)> { ("Old Name", "old@example.com", "+1-555-0000") });
        var eventId = @event.Id;

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var command = new UpdateEventOrganizerContactCommand(
            EventId: eventId,
            PublishOrganizerContact: true,
            Contacts: new List<OrganizerContactRequest>
            {
                new("New Name", "new@example.com", "+1-555-9999"),
                new("Second Contact", "second@example.com")
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.OrganizerContacts.Should().HaveCount(2);
        @event.OrganizerContactName.Should().Be("New Name");
        @event.OrganizerContacts[1].ContactName.Should().Be("Second Contact");
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
            Guid.NewGuid(),
            100,
            null,
            EventCategory.Community
        );

        return eventResult.Value;
    }
}
