using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.EventHandlers;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Shared.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.EventHandlers;

/// <summary>
/// Phase 6A.75: Unit tests for EventApprovedEventHandler with templated email support.
/// </summary>
public class EventApprovedEventHandlerTests
{
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IEventRepository> _eventRepository;
    private readonly Mock<IEmailUrlHelper> _emailUrlHelper;
    private readonly Mock<ILogger<EventApprovedEventHandler>> _logger;
    private readonly EventApprovedEventHandler _handler;

    public EventApprovedEventHandlerTests()
    {
        _emailService = new Mock<IEmailService>();
        _userRepository = new Mock<IUserRepository>();
        _eventRepository = new Mock<IEventRepository>();
        _emailUrlHelper = new Mock<IEmailUrlHelper>();
        _logger = new Mock<ILogger<EventApprovedEventHandler>>();

        // Setup default URL helper behavior
        _emailUrlHelper.Setup(x => x.BuildEventDetailsUrl(It.IsAny<Guid>()))
            .Returns((Guid id) => $"https://lankaconnect.com/events/{id}");
        _emailUrlHelper.Setup(x => x.BuildEventManageUrl(It.IsAny<Guid>()))
            .Returns((Guid id) => $"https://lankaconnect.com/events/{id}/manage");

        _handler = new EventApprovedEventHandler(
            _emailService.Object,
            _userRepository.Object,
            _eventRepository.Object,
            _emailUrlHelper.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_ShouldSendTemplatedEmailToOrganizer()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var organizerEmail = "organizer@example.com";
        var eventTitle = "Test Event";
        var approvedAt = DateTime.UtcNow;

        var domainEvent = new EventApprovedEvent(eventId, adminId, approvedAt);
        var notification = new DomainEventNotification<EventApprovedEvent>(domainEvent);

        var mockEvent = CreateMockEvent(eventId, organizerId, eventTitle);
        var organizer = CreateTestUser(organizerId, organizerEmail, "John", "Organizer");

        _eventRepository.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEvent);
        _userRepository.Setup(x => x.GetByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizer);
        _emailService.Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert - Phase 6A.75: Verify templated email is used
        _emailService.Verify(x => x.SendTemplatedEmailAsync(
            "template-event-approval",
            organizerEmail,
            It.Is<Dictionary<string, object>>(p =>
                p["OrganizerName"].ToString() == "John Organizer" &&
                p["EventTitle"].ToString() == eventTitle),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_ShouldIncludeEventAndManageUrls()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var organizerEmail = "organizer@example.com";
        var eventTitle = "Test Event";
        var approvedAt = DateTime.UtcNow;

        var domainEvent = new EventApprovedEvent(eventId, adminId, approvedAt);
        var notification = new DomainEventNotification<EventApprovedEvent>(domainEvent);

        var mockEvent = CreateMockEvent(eventId, organizerId, eventTitle);
        var organizer = CreateTestUser(organizerId, organizerEmail, "John", "Organizer");

        _eventRepository.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEvent);
        _userRepository.Setup(x => x.GetByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizer);
        _emailService.Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert - Phase 6A.75: Verify URLs are included
        _emailUrlHelper.Verify(x => x.BuildEventDetailsUrl(eventId), Times.Once);
        _emailUrlHelper.Verify(x => x.BuildEventManageUrl(eventId), Times.Once);

        _emailService.Verify(x => x.SendTemplatedEmailAsync(
            "template-event-approval",
            organizerEmail,
            It.Is<Dictionary<string, object>>(p =>
                p.ContainsKey("EventUrl") &&
                p.ContainsKey("EventManageUrl")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EventNotFound_ShouldNotSendEmail()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var domainEvent = new EventApprovedEvent(eventId, adminId, DateTime.UtcNow);
        var notification = new DomainEventNotification<EventApprovedEvent>(domainEvent);

        _eventRepository.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _emailService.Verify(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _userRepository.Verify(x => x.GetByIdAsync(
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OrganizerNotFound_ShouldNotSendEmail()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var domainEvent = new EventApprovedEvent(eventId, adminId, DateTime.UtcNow);
        var notification = new DomainEventNotification<EventApprovedEvent>(domainEvent);

        var mockEvent = CreateMockEvent(eventId, organizerId, "Test Event");

        _eventRepository.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEvent);
        _userRepository.Setup(x => x.GetByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _emailService.Verify(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailServiceFailure_ShouldNotThrow()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var domainEvent = new EventApprovedEvent(eventId, adminId, DateTime.UtcNow);
        var notification = new DomainEventNotification<EventApprovedEvent>(domainEvent);

        var mockEvent = CreateMockEvent(eventId, organizerId, "Test Event");
        var organizer = CreateTestUser(organizerId, "organizer@example.com", "John", "Organizer");

        _eventRepository.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEvent);
        _userRepository.Setup(x => x.GetByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizer);
        _emailService.Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Email service error"));

        // Act - Should not throw (fail-silent pattern)
        var act = async () => await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_ExceptionDuringProcessing_ShouldNotThrow()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var domainEvent = new EventApprovedEvent(eventId, adminId, DateTime.UtcNow);
        var notification = new DomainEventNotification<EventApprovedEvent>(domainEvent);

        _eventRepository.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act - Should not throw (fail-silent pattern)
        var act = async () => await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    private static Event CreateMockEvent(Guid eventId, Guid organizerId, string title)
    {
        var eventObj = Event.Create(
            EventTitle.Create(title).Value,
            EventDescription.Create("Test Description").Value,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(7).AddHours(2),
            organizerId,
            100,
            null,  // location
            EventCategory.Cultural).Value;

        // Set the Id using reflection
        var idProperty = typeof(BaseEntity).GetProperty("Id");
        idProperty?.SetValue(eventObj, eventId);

        return eventObj;
    }

    private static User CreateTestUser(Guid userId, string email, string firstName, string lastName)
    {
        var userEmail = Email.Create(email).Value;
        var user = User.Create(userEmail, firstName, lastName).Value;

        // Set the Id using reflection
        var idProperty = typeof(BaseEntity).GetProperty("Id");
        idProperty?.SetValue(user, userId);

        return user;
    }
}
