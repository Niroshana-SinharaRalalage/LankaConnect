using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.BackgroundJobs;
using LankaConnect.Application.Events.Repositories;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.BackgroundJobs;

public class EventReminderJobTests
{
    private readonly Mock<IEventRepository> _eventRepository;
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<IEmailUrlHelper> _emailUrlHelper;
    private readonly Mock<IEventReminderRepository> _eventReminderRepository;
    private readonly Mock<ILogger<EventReminderJob>> _logger;
    private readonly EventReminderJob _job;

    public EventReminderJobTests()
    {
        _eventRepository = new Mock<IEventRepository>();
        _userRepository = new Mock<IUserRepository>();
        _emailService = new Mock<IEmailService>();
        _emailUrlHelper = new Mock<IEmailUrlHelper>();
        _eventReminderRepository = new Mock<IEventReminderRepository>();
        _logger = new Mock<ILogger<EventReminderJob>>();

        // Phase 6A.71: Setup IEmailUrlHelper mock to return test URL
        _emailUrlHelper
            .Setup(x => x.BuildEventDetailsUrl(It.IsAny<Guid>()))
            .Returns((Guid eventId) => $"https://test.lankaconnect.com/events/{eventId}");

        // Phase 6A.71: Setup IEventReminderRepository mock to allow all sends by default
        _eventReminderRepository
            .Setup(x => x.IsReminderAlreadySentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _job = new EventReminderJob(
            _eventRepository.Object,
            _userRepository.Object,
            _emailService.Object,
            _emailUrlHelper.Object,
            _eventReminderRepository.Object,
            _logger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithAuthenticatedUserRegistration_ShouldSendEmailToUserEmail()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var userEmail = "attendee@test.com";
        var eventTitle = "Test Event";

        var mockEvent = CreateMockEventWithRegistration(eventId, organizerId, eventTitle, userId: userId);
        var user = CreateTestUser(userId, userEmail, "Jane", "Attendee");

        _eventRepository.Setup(x => x.GetEventsStartingInTimeWindowAsync(
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<EventStatus[]>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Event> { mockEvent });

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _emailService.Setup(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _job.ExecuteAsync();

        // Assert - Phase 6A.57: Now sends 3 reminders (7d, 2d, 1d) per registration
        _emailService.Verify(x => x.SendTemplatedEmailAsync(
            "event-reminder",
            userEmail,
            It.Is<Dictionary<string, object>>(p =>
                p.ContainsKey("AttendeeName") &&
                p["AttendeeName"].ToString() == "Jane Attendee" &&
                p.ContainsKey("EventTitle") &&
                p["EventTitle"].ToString() == eventTitle),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteAsync_WithAnonymousRegistrationUsingContact_ShouldSendEmailToContactEmail()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var contactEmail = "anonymous@test.com";
        var eventTitle = "Test Event";

        var mockEvent = CreateMockEventWithAnonymousRegistration(eventId, organizerId, eventTitle, contactEmail);

        _eventRepository.Setup(x => x.GetEventsStartingInTimeWindowAsync(
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<EventStatus[]>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Event> { mockEvent });

        _emailService.Setup(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _job.ExecuteAsync();

        // Assert - Phase 6A.57: Now sends 3 reminders (7d, 2d, 1d) per registration
        _emailService.Verify(x => x.SendTemplatedEmailAsync(
            "event-reminder",
            contactEmail,
            It.Is<Dictionary<string, object>>(p =>
                p.ContainsKey("EventTitle") &&
                p["EventTitle"].ToString() == eventTitle),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteAsync_WithNoEventsInTimeWindow_ShouldNotSendAnyEmails()
    {
        // Arrange
        _eventRepository.Setup(x => x.GetEventsStartingInTimeWindowAsync(
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<EventStatus[]>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Event>());

        // Act
        await _job.ExecuteAsync();

        // Assert
        _emailService.Verify(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_UserNotFound_ShouldSkipRegistrationAndContinue()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var mockEvent = CreateMockEventWithRegistration(eventId, organizerId, "Test Event", userId: userId);

        _eventRepository.Setup(x => x.GetEventsStartingInTimeWindowAsync(
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<EventStatus[]>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Event> { mockEvent });

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act - Should not throw
        var act = async () => await _job.ExecuteAsync();

        // Assert
        await act.Should().NotThrowAsync();
        _emailService.Verify(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_EmailServiceFailure_ShouldContinueWithOtherRegistrations()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var mockEvent = CreateMockEventWithMultipleRegistrations(eventId, organizerId, "Test Event", userId1, userId2);
        var user1 = CreateTestUser(userId1, "user1@test.com", "User", "One");
        var user2 = CreateTestUser(userId2, "user2@test.com", "User", "Two");

        _eventRepository.Setup(x => x.GetEventsStartingInTimeWindowAsync(
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<EventStatus[]>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Event> { mockEvent });

        _userRepository.Setup(x => x.GetByIdAsync(userId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user1);
        _userRepository.Setup(x => x.GetByIdAsync(userId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user2);

        // Phase 6A.57: First email fails, second should still be sent
        // With 3 time windows, we expect 3 calls per registration = 6 total
        var callCount = 0;
        _emailService.Setup(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? Result.Failure("Email failed") : Result.Success();
            });

        // Act
        await _job.ExecuteAsync();

        // Assert - Both registrations should be attempted (3 windows × 2 registrations = 6 calls)
        _emailService.Verify(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()), Times.Exactly(6));
    }

    [Fact]
    public async Task ExecuteAsync_ExceptionDuringProcessing_ShouldNotThrow()
    {
        // Arrange
        _eventRepository.Setup(x => x.GetEventsStartingInTimeWindowAsync(
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<EventStatus[]>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act - Should not throw
        var act = async () => await _job.ExecuteAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    private static Event CreateMockEventWithRegistration(Guid eventId, Guid organizerId, string title, Guid? userId = null)
    {
        var eventObj = Event.Create(
            EventTitle.Create(title).Value,
            EventDescription.Create("Test Description").Value,
            DateTime.UtcNow.AddHours(24),  // Event starting in 24 hours
            DateTime.UtcNow.AddHours(26),
            organizerId,
            100,
            null,  // location
            EventCategory.Cultural).Value;

        // Set the Id using reflection
        var idProperty = typeof(BaseEntity).GetProperty("Id");
        idProperty?.SetValue(eventObj, eventId);

        // Publish the event so registrations can be added
        eventObj.Publish();

        // Add registration if userId is provided
        if (userId.HasValue)
        {
            eventObj.Register(userId.Value, 1);
        }

        return eventObj;
    }

    private static Event CreateMockEventWithAnonymousRegistration(Guid eventId, Guid organizerId, string title, string contactEmail)
    {
        var eventObj = Event.Create(
            EventTitle.Create(title).Value,
            EventDescription.Create("Test Description").Value,
            DateTime.UtcNow.AddHours(24),
            DateTime.UtcNow.AddHours(26),
            organizerId,
            100,
            null,  // location
            EventCategory.Cultural).Value;

        var idProperty = typeof(BaseEntity).GetProperty("Id");
        idProperty?.SetValue(eventObj, eventId);

        // Publish the event so registrations can be added
        eventObj.Publish();

        // Add anonymous registration using attendee details
        var attendees = new[]
        {
            AttendeeDetails.Create("Anonymous Attendee", AgeCategory.Adult).Value
        };
        var contact = RegistrationContact.Create(contactEmail, "555-1234", null).Value;

        eventObj.RegisterWithAttendees(null, attendees, contact);

        return eventObj;
    }

    private static Event CreateMockEventWithMultipleRegistrations(Guid eventId, Guid organizerId, string title, Guid userId1, Guid userId2)
    {
        var eventObj = Event.Create(
            EventTitle.Create(title).Value,
            EventDescription.Create("Test Description").Value,
            DateTime.UtcNow.AddHours(24),
            DateTime.UtcNow.AddHours(26),
            organizerId,
            100,
            null,  // location
            EventCategory.Cultural).Value;

        var idProperty = typeof(BaseEntity).GetProperty("Id");
        idProperty?.SetValue(eventObj, eventId);

        // Publish the event so registrations can be added
        eventObj.Publish();

        eventObj.Register(userId1, 1);
        eventObj.Register(userId2, 1);

        return eventObj;
    }

    private static User CreateTestUser(Guid userId, string email, string firstName, string lastName)
    {
        var userEmail = Email.Create(email).Value;
        var user = User.Create(userEmail, firstName, lastName).Value;

        var idProperty = typeof(BaseEntity).GetProperty("Id");
        idProperty?.SetValue(user, userId);

        return user;
    }
}
