using LankaConnect.Modules.Identity.Contracts;
using LankaConnect.BuildingBlocks.Application.Common;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.EventHandlers;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.SharedKernel.Money;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;
using LankaConnect.Modules.Communications.Contracts.Email.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.EventHandlers;

/// <summary>
/// Phase 6A.100: Unit tests for EventRejectedEventHandler with ITypedEmailService.
/// </summary>
public class EventRejectedEventHandlerTests
{
    private readonly Mock<ITypedEmailService> _typedEmailService;
    private readonly Mock<IIdentityQueries> _userRepository;
    private readonly Mock<IEventRepository> _eventRepository;
    private readonly Mock<ILogger<EventRejectedEventHandler>> _logger;
    private readonly EventRejectedEventHandler _handler;

    public EventRejectedEventHandlerTests()
    {
        _typedEmailService = new Mock<ITypedEmailService>();
        _userRepository = new Mock<IIdentityQueries>();
        _eventRepository = new Mock<IEventRepository>();
        _logger = new Mock<ILogger<EventRejectedEventHandler>>();

        _handler = new EventRejectedEventHandler(
            _typedEmailService.Object,
            _userRepository.Object,
            _eventRepository.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_ShouldSendTypedEmailToOrganizer()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var organizerEmail = "organizer@example.com";
        var eventTitle = "Test Event";
        var rejectionReason = "Event description needs more details";
        var rejectedAt = DateTime.UtcNow;

        var domainEvent = new EventRejectedEvent(eventId, adminId, rejectionReason, rejectedAt);
        var notification = new DomainEventNotification<EventRejectedEvent>(domainEvent);

        var mockEvent = CreateMockEvent(eventId, organizerId, eventTitle);
        var organizer = CreateTestUser(organizerId, organizerEmail, "John", "Organizer");

        _eventRepository.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEvent);
        _userRepository.Setup(x => x.GetUserByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizer);
        _typedEmailService.Setup(x => x.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Ok("test-correlation-id", 100));

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert - Phase 6A.100: Verify typed email service is used with correct parameters
        _typedEmailService.Verify(x => x.SendEmailAsync(
            It.Is<EventRejectedEmailParams>(p =>
                p.OrganizerEmail == organizerEmail &&
                p.OrganizerName == "John Organizer" &&
                p.EventTitle == eventTitle &&
                p.Reason == rejectionReason),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EventNotFound_ShouldNotSendEmail()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var domainEvent = new EventRejectedEvent(eventId, adminId, "Reason", DateTime.UtcNow);
        var notification = new DomainEventNotification<EventRejectedEvent>(domainEvent);

        _eventRepository.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _typedEmailService.Verify(x => x.SendEmailAsync(
            It.IsAny<IEmailParameters>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _userRepository.Verify(x => x.GetUserByIdAsync(
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
        var domainEvent = new EventRejectedEvent(eventId, adminId, "Reason", DateTime.UtcNow);
        var notification = new DomainEventNotification<EventRejectedEvent>(domainEvent);

        var mockEvent = CreateMockEvent(eventId, organizerId, "Test Event");

        _eventRepository.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEvent);
        _userRepository.Setup(x => x.GetUserByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSummaryDto?)null);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _typedEmailService.Verify(x => x.SendEmailAsync(
            It.IsAny<IEmailParameters>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailServiceFailure_ShouldNotThrow()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var domainEvent = new EventRejectedEvent(eventId, adminId, "Reason", DateTime.UtcNow);
        var notification = new DomainEventNotification<EventRejectedEvent>(domainEvent);

        var mockEvent = CreateMockEvent(eventId, organizerId, "Test Event");
        var organizer = CreateTestUser(organizerId, "organizer@example.com", "John", "Organizer");

        _eventRepository.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEvent);
        _userRepository.Setup(x => x.GetUserByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizer);
        _typedEmailService.Setup(x => x.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Fail("test-correlation-id", new List<string> { "Email service error" }));

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
        var domainEvent = new EventRejectedEvent(eventId, adminId, "Reason", DateTime.UtcNow);
        var notification = new DomainEventNotification<EventRejectedEvent>(domainEvent);

        _eventRepository.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act - Should not throw (fail-silent pattern)
        var act = async () => await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_EmailContainsRejectionReason_ShouldIncludeReasonInParams()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var rejectionReason = "The event description is too vague. Please add more details about the venue and activities.";
        var domainEvent = new EventRejectedEvent(eventId, adminId, rejectionReason, DateTime.UtcNow);
        var notification = new DomainEventNotification<EventRejectedEvent>(domainEvent);

        var mockEvent = CreateMockEvent(eventId, organizerId, "Test Event");
        var organizer = CreateTestUser(organizerId, "organizer@example.com", "John", "Organizer");

        _eventRepository.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEvent);
        _userRepository.Setup(x => x.GetUserByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizer);
        _typedEmailService.Setup(x => x.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Ok("test-correlation-id", 100));

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _typedEmailService.Verify(x => x.SendEmailAsync(
            It.Is<EventRejectedEmailParams>(p => p.Reason == rejectionReason),
            It.IsAny<CancellationToken>()), Times.Once);
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
        var idProperty = typeof(LegacyBaseEntity).GetProperty("Id");
        idProperty?.SetValue(eventObj, eventId);

        return eventObj;
    }

    private static UserSummaryDto CreateTestUser(Guid userId, string email, string firstName, string lastName)
    {
        return new UserSummaryDto(
            Id: userId,
            Email: email,
            FirstName: firstName,
            LastName: lastName,
            DisplayName: $"{firstName} {lastName}",
            Role: UserRoleDto.GeneralUser,
            Status: UserStatusDto.Active,
            EmailVerified: true,
            CreatedAt: System.DateTime.UtcNow,
            UpdatedAt: null);
    }
}
