using FluentAssertions;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.BackgroundJobs;
using LankaConnect.Application.Events.Repositories;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.BackgroundJobs;

/// <summary>
/// Phase 6A.61: TDD Tests for EventNotificationEmailJob
/// Tests background email sending with recipient consolidation and history updates
/// </summary>
public class EventNotificationEmailJobTests
{
    private readonly Mock<IEventNotificationHistoryRepository> _mockHistoryRepository;
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IRegistrationRepository> _mockRegistrationRepository;
    private readonly Mock<IEventNotificationRecipientService> _mockRecipientService;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IEmailUrlHelper> _mockEmailUrlHelper;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<EventNotificationEmailJob>> _mockLogger;
    private readonly EventNotificationEmailJob _job;

    public EventNotificationEmailJobTests()
    {
        _mockHistoryRepository = new Mock<IEventNotificationHistoryRepository>();
        _mockEventRepository = new Mock<IEventRepository>();
        _mockRegistrationRepository = new Mock<IRegistrationRepository>();
        _mockRecipientService = new Mock<IEventNotificationRecipientService>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockEmailService = new Mock<IEmailService>();
        _mockEmailUrlHelper = new Mock<IEmailUrlHelper>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<EventNotificationEmailJob>>();

        _job = new EventNotificationEmailJob(
            _mockHistoryRepository.Object,
            _mockEventRepository.Object,
            _mockRegistrationRepository.Object,
            _mockRecipientService.Object,
            _mockUserRepository.Object,
            _mockEmailService.Object,
            _mockEmailUrlHelper.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    private Event CreateTestEvent(Guid id, Guid organizerId, bool isFree = true)
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);
        var address = Address.Create("123 Main St", "Test City", "CA", "90001", "USA").Value;
        var location = EventLocation.Create(address).Value;

        var @event = Event.Create(
            title,
            description,
            startDate,
            endDate,
            organizerId,
            100,
            location
        ).Value;

        // Set ID using reflection
        var idField = typeof(Event).BaseType?.GetField("<Id>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idField?.SetValue(@event, id);

        return @event;
    }

    private User CreateTestUser(Guid id, string email, string firstName, string lastName)
    {
        var emailVO = Email.Create(email).Value;
        var user = User.Create(emailVO, firstName, lastName).Value;

        // Set ID using reflection
        var idField = typeof(User).BaseType?.GetField("<Id>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idField?.SetValue(user, id);

        return user;
    }

    #region Success Cases

    [Fact]
    public async Task ExecuteAsync_WithValidHistory_ShouldSendEmailsAndUpdateStatistics()
    {
        // Arrange
        var historyId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var history = EventNotificationHistory.Create(eventId, organizerId, 0).Value;
        var @event = CreateTestEvent(eventId, organizerId);

        var user1 = CreateTestUser(userId1, "user1@test.com", "John", "Doe");
        var user2 = CreateTestUser(userId2, "user2@test.com", "Jane", "Smith");

        var registration1 = Registration.Create(eventId, userId1, 1).Value;
        var registration2 = Registration.Create(eventId, userId2, 1).Value;

        var recipients = new EventNotificationRecipients(
            new HashSet<string> { "group1@test.com", "group2@test.com" },
            new RecipientBreakdown(2, 0, 0, 0, 2)
        );

        _mockHistoryRepository
            .Setup(x => x.GetByIdAsync(historyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Phase 6A.61+ Fix: Mock now expects trackChanges: false parameter
        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockRecipientService
            .Setup(x => x.ResolveRecipientsAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipients);

        _mockRegistrationRepository
            .Setup(x => x.GetByEventAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Registration> { registration1, registration2 });

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user1);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user2);

        // Phase 6A.61+ Fix: Add mock for bulk user email fetch
        _mockUserRepository
            .Setup(x => x.GetEmailsByUserIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string>
            {
                { userId1, "user1@test.com" },
                { userId2, "user2@test.com" }
            });

        _mockEmailUrlHelper
            .Setup(x => x.BuildEventDetailsUrl(eventId))
            .Returns($"https://test.com/events/{eventId}");

        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _job.ExecuteAsync(historyId, CancellationToken.None);

        // Assert
        // Should send to 4 recipients (2 from groups + 2 from registrations)
        _mockEmailService.Verify(
            x => x.SendTemplatedEmailAsync(
                "template-event-details-publication",
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(4));

        // Phase 6A.61+ Concurrency Fix: Entity reload before update
        _mockHistoryRepository.Verify(x => x.Update(It.IsAny<EventNotificationHistory>()), Times.Once());
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedEmails_ShouldTrackFailureCount()
    {
        // Arrange
        var historyId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();

        var history = EventNotificationHistory.Create(eventId, organizerId, 0).Value;
        var @event = CreateTestEvent(eventId, organizerId);

        var recipients = new EventNotificationRecipients(
            new HashSet<string> { "success@test.com", "fail@test.com" },
            new RecipientBreakdown(2, 0, 0, 0, 2)
        );

        _mockHistoryRepository
            .Setup(x => x.GetByIdAsync(historyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Phase 6A.61+ Fix: Mock now expects trackChanges: false parameter
        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockRecipientService
            .Setup(x => x.ResolveRecipientsAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipients);

        _mockRegistrationRepository
            .Setup(x => x.GetByEventAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Registration>());

        _mockEmailUrlHelper
            .Setup(x => x.BuildEventDetailsUrl(eventId))
            .Returns($"https://test.com/events/{eventId}");

        // One succeeds, one fails
        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync("template-event-details-publication", "success@test.com", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync("template-event-details-publication", "fail@test.com", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("SMTP error"));

        // Act
        await _job.ExecuteAsync(historyId, CancellationToken.None);

        // Assert
        _mockEmailService.Verify(
            x => x.SendTemplatedEmailAsync(
                "template-event-details-publication",
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        // Phase 6A.61+ Concurrency Fix: Entity reload before update with final stats
        _mockHistoryRepository.Verify(
            x => x.Update(It.IsAny<EventNotificationHistory>()),
            Times.Once());

        // Final update should have 1 success and 1 failure
        _mockHistoryRepository.Verify(
            x => x.Update(It.Is<EventNotificationHistory>(h =>
                h.SuccessfulSends == 1 && h.FailedSends == 1)),
            Times.AtLeastOnce);
    }

    #endregion

    #region Error Cases

    [Fact]
    public async Task ExecuteAsync_HistoryNotFound_ShouldLogErrorAndReturn()
    {
        // Arrange
        var historyId = Guid.NewGuid();

        _mockHistoryRepository
            .Setup(x => x.GetByIdAsync(historyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventNotificationHistory?)null);

        // Act
        await _job.ExecuteAsync(historyId, CancellationToken.None);

        // Assert
        _mockEventRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_EventNotFound_ShouldLogErrorAndReturn()
    {
        // Arrange
        var historyId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();

        var history = EventNotificationHistory.Create(eventId, organizerId, 0).Value;

        _mockHistoryRepository
            .Setup(x => x.GetByIdAsync(historyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act
        await _job.ExecuteAsync(historyId, CancellationToken.None);

        // Assert
        _mockRecipientService.Verify(
            x => x.ResolveRecipientsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion
}
