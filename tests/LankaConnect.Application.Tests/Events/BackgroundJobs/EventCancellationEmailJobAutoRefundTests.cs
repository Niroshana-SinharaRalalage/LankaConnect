using FluentAssertions;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.BackgroundJobs;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.BackgroundJobs;

/// <summary>
/// Phase 6A.92: TDD Tests for EventCancellationEmailJob auto-refund feature
/// Tests automatic refund processing when an event is cancelled
/// </summary>
public class EventCancellationEmailJobAutoRefundTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IRegistrationRepository> _mockRegistrationRepository;
    private readonly Mock<IEventNotificationRecipientService> _mockRecipientService;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IApplicationUrlsService> _mockUrlsService;
    private readonly Mock<IStripePaymentService> _mockStripePaymentService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<EventCancellationEmailJob>> _mockLogger;
    private readonly EventCancellationEmailJob _job;

    public EventCancellationEmailJobAutoRefundTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockRegistrationRepository = new Mock<IRegistrationRepository>();
        _mockRecipientService = new Mock<IEventNotificationRecipientService>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockEmailService = new Mock<IEmailService>();
        _mockUrlsService = new Mock<IApplicationUrlsService>();
        _mockStripePaymentService = new Mock<IStripePaymentService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<EventCancellationEmailJob>>();

        _mockUrlsService.Setup(x => x.FrontendBaseUrl).Returns("https://test.lankaconnect.com");

        _job = new EventCancellationEmailJob(
            _mockEventRepository.Object,
            _mockRegistrationRepository.Object,
            _mockRecipientService.Object,
            _mockUserRepository.Object,
            _mockEmailService.Object,
            _mockUrlsService.Object,
            _mockStripePaymentService.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    #region Helper Methods

    private Event CreateCancelledPaidEvent(Guid eventId, Guid organizerId)
    {
        var title = EventTitle.Create("Test Paid Event").Value;
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
        SetEntityId(@event, eventId);

        // Cancel the event
        @event.Cancel("Test cancellation");

        return @event;
    }

    private Event CreateCancelledFreeEvent(Guid eventId, Guid organizerId)
    {
        var @event = CreateCancelledPaidEvent(eventId, organizerId);
        // Free events have no ticket tiers set
        return @event;
    }

    private Registration CreatePaidConfirmedRegistration(Guid registrationId, Guid eventId, Guid userId, decimal amount, string paymentIntentId)
    {
        var contact = RegistrationContact.Create("test@test.com", "1234567890", null).Value;
        var attendees = new List<AttendeeDetails>
        {
            AttendeeDetails.Create("John Doe", AgeCategory.Adult).Value
        };
        var totalPrice = Money.Create(amount, Currency.USD).Value;

        var registration = Registration.CreateWithAttendees(
            eventId,
            userId,
            attendees,
            contact,
            totalPrice,
            isPaidEvent: true
        ).Value;

        // Set ID and payment info using reflection
        SetEntityId(registration, registrationId);
        SetPrivateProperty(registration, "Status", RegistrationStatus.Confirmed);
        SetPrivateProperty(registration, "PaymentStatus", PaymentStatus.Completed);
        SetPrivateProperty(registration, "StripePaymentIntentId", paymentIntentId);

        return registration;
    }

    private Registration CreateFreeConfirmedRegistration(Guid registrationId, Guid eventId, Guid userId)
    {
        var registration = Registration.Create(eventId, userId, 1).Value;
        SetEntityId(registration, registrationId);
        return registration;
    }

    private void SetEntityId<T>(T entity, Guid id) where T : class
    {
        var baseType = typeof(T);
        while (baseType != null && baseType.Name != "BaseEntity")
        {
            baseType = baseType.BaseType;
        }
        var idField = baseType?.GetField("<Id>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idField?.SetValue(entity, id);
    }

    private void SetPrivateProperty<T>(T obj, string propertyName, object value)
    {
        var property = typeof(T).GetProperty(propertyName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (property != null)
        {
            var backingField = typeof(T).GetField($"<{propertyName}>k__BackingField",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            backingField?.SetValue(obj, value);
        }
    }

    private void SetupEmptyRecipients()
    {
        _mockRecipientService
            .Setup(x => x.ResolveRecipientsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventNotificationRecipients(
                new HashSet<string>(),
                new RecipientBreakdown(0, 0, 0, 0, 0)));
    }

    #endregion

    #region Auto-Refund Tests

    [Fact]
    public async Task ExecuteAsync_PaidEventCancelled_ProcessesRefundsForPaidRegistrations()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var paymentIntentId = "pi_test_123";
        var refundId = "re_test_456";

        var @event = CreateCancelledPaidEvent(eventId, organizerId);
        var registration = CreatePaidConfirmedRegistration(registrationId, eventId, userId, 25.00m, paymentIntentId);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockRegistrationRepository
            .Setup(x => x.GetByEventAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Registration> { registration });

        _mockUserRepository
            .Setup(x => x.GetEmailsByUserIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { { userId, "test@test.com" } });

        _mockStripePaymentService
            .Setup(x => x.CreateRefundAsync(It.Is<CreateRefundRequest>(r => r.PaymentIntentId == paymentIntentId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StripeRefundResult>.Success(new StripeRefundResult
            {
                RefundId = refundId,
                Status = "succeeded",
                AmountRefunded = 2500,
                Currency = "usd",
                CreatedAt = DateTime.UtcNow
            }));

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        SetupEmptyRecipients();

        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _job.ExecuteAsync(eventId, "Event cancelled by organizer");

        // Assert
        _mockStripePaymentService.Verify(
            x => x.CreateRefundAsync(
                It.Is<CreateRefundRequest>(r =>
                    r.PaymentIntentId == paymentIntentId &&
                    r.AmountInCents == 2500 &&
                    r.Reason == "event_cancelled"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockRegistrationRepository.Verify(x => x.Update(It.Is<Registration>(r => r.Id == registrationId)), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_FreeEvent_DoesNotProcessRefunds()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();

        var @event = CreateCancelledFreeEvent(eventId, organizerId);
        var registration = CreateFreeConfirmedRegistration(registrationId, eventId, userId);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockRegistrationRepository
            .Setup(x => x.GetByEventAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Registration> { registration });

        _mockUserRepository
            .Setup(x => x.GetEmailsByUserIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { { userId, "test@test.com" } });

        SetupEmptyRecipients();

        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _job.ExecuteAsync(eventId, "Event cancelled by organizer");

        // Assert - Stripe should never be called for free events
        _mockStripePaymentService.Verify(
            x => x.CreateRefundAsync(It.IsAny<CreateRefundRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleRegistrations_ProcessesAllRefunds()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();

        var @event = CreateCancelledPaidEvent(eventId, organizerId);

        var registrations = new List<Registration>();
        for (int i = 0; i < 3; i++)
        {
            var reg = CreatePaidConfirmedRegistration(
                Guid.NewGuid(), eventId, Guid.NewGuid(),
                25.00m + i * 10, $"pi_test_{i}");
            registrations.Add(reg);
        }

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockRegistrationRepository
            .Setup(x => x.GetByEventAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registrations);

        _mockUserRepository
            .Setup(x => x.GetEmailsByUserIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(registrations.ToDictionary(r => r.UserId!.Value, r => $"user{r.UserId}@test.com"));

        _mockStripePaymentService
            .Setup(x => x.CreateRefundAsync(It.IsAny<CreateRefundRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StripeRefundResult>.Success(new StripeRefundResult
            {
                RefundId = $"re_test_{Guid.NewGuid()}",
                Status = "succeeded",
                AmountRefunded = 2500,
                Currency = "usd",
                CreatedAt = DateTime.UtcNow
            }));

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        SetupEmptyRecipients();

        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _job.ExecuteAsync(eventId, "Event cancelled by organizer");

        // Assert - Stripe should be called 3 times (once per paid registration)
        _mockStripePaymentService.Verify(
            x => x.CreateRefundAsync(It.IsAny<CreateRefundRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        _mockRegistrationRepository.Verify(x => x.Update(It.IsAny<Registration>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteAsync_RefundFails_ContinuesProcessingOtherRefunds()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();

        var @event = CreateCancelledPaidEvent(eventId, organizerId);

        var reg1 = CreatePaidConfirmedRegistration(Guid.NewGuid(), eventId, Guid.NewGuid(), 25.00m, "pi_test_1");
        var reg2 = CreatePaidConfirmedRegistration(Guid.NewGuid(), eventId, Guid.NewGuid(), 35.00m, "pi_test_2");
        var registrations = new List<Registration> { reg1, reg2 };

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockRegistrationRepository
            .Setup(x => x.GetByEventAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registrations);

        _mockUserRepository
            .Setup(x => x.GetEmailsByUserIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(registrations.ToDictionary(r => r.UserId!.Value, r => $"user{r.UserId}@test.com"));

        // First refund fails
        _mockStripePaymentService
            .Setup(x => x.CreateRefundAsync(It.Is<CreateRefundRequest>(r => r.PaymentIntentId == "pi_test_1"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StripeRefundResult>.Failure("Charge has already been refunded"));

        // Second refund succeeds
        _mockStripePaymentService
            .Setup(x => x.CreateRefundAsync(It.Is<CreateRefundRequest>(r => r.PaymentIntentId == "pi_test_2"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StripeRefundResult>.Success(new StripeRefundResult
            {
                RefundId = "re_test_2",
                Status = "succeeded",
                AmountRefunded = 3500,
                Currency = "usd",
                CreatedAt = DateTime.UtcNow
            }));

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        SetupEmptyRecipients();

        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _job.ExecuteAsync(eventId, "Event cancelled by organizer");

        // Assert - Both refunds should be attempted
        _mockStripePaymentService.Verify(
            x => x.CreateRefundAsync(It.IsAny<CreateRefundRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        // Only the second registration should be updated (the successful one)
        _mockRegistrationRepository.Verify(x => x.Update(It.Is<Registration>(r => r.Id == reg2.Id)), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_RegistrationWithoutPaymentIntentId_SkipsRefund()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();

        var @event = CreateCancelledPaidEvent(eventId, organizerId);

        // Create a registration without PaymentIntentId (legacy data)
        var contact = RegistrationContact.Create("test@test.com", "1234567890", null).Value;
        var attendees = new List<AttendeeDetails> { AttendeeDetails.Create("John Doe", AgeCategory.Adult).Value };
        var totalPrice = Money.Create(25.00m, Currency.USD).Value;
        var registration = Registration.CreateWithAttendees(eventId, userId, attendees, contact, totalPrice, isPaidEvent: true).Value;
        SetEntityId(registration, registrationId);
        SetPrivateProperty(registration, "Status", RegistrationStatus.Confirmed);
        SetPrivateProperty(registration, "PaymentStatus", PaymentStatus.Completed);
        // Note: No StripePaymentIntentId set

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockRegistrationRepository
            .Setup(x => x.GetByEventAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Registration> { registration });

        _mockUserRepository
            .Setup(x => x.GetEmailsByUserIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { { userId, "test@test.com" } });

        SetupEmptyRecipients();

        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _job.ExecuteAsync(eventId, "Event cancelled by organizer");

        // Assert - Stripe should not be called for registration without PaymentIntentId
        _mockStripePaymentService.Verify(
            x => x.CreateRefundAsync(It.IsAny<CreateRefundRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_EventNotFound_ExitsGracefully()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act
        await _job.ExecuteAsync(eventId, "Event cancelled");

        // Assert
        _mockStripePaymentService.Verify(
            x => x.CreateRefundAsync(It.IsAny<CreateRefundRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockEmailService.Verify(
            x => x.SendTemplatedEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_RefundMetadataIncludesEventInfo()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var paymentIntentId = "pi_test_metadata";

        var @event = CreateCancelledPaidEvent(eventId, organizerId);
        var registration = CreatePaidConfirmedRegistration(registrationId, eventId, userId, 50.00m, paymentIntentId);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockRegistrationRepository
            .Setup(x => x.GetByEventAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Registration> { registration });

        _mockUserRepository
            .Setup(x => x.GetEmailsByUserIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { { userId, "test@test.com" } });

        CreateRefundRequest? capturedRequest = null;
        _mockStripePaymentService
            .Setup(x => x.CreateRefundAsync(It.IsAny<CreateRefundRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateRefundRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(Result<StripeRefundResult>.Success(new StripeRefundResult
            {
                RefundId = "re_test",
                Status = "succeeded",
                AmountRefunded = 5000,
                Currency = "usd",
                CreatedAt = DateTime.UtcNow
            }));

        _mockUnitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        SetupEmptyRecipients();
        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _job.ExecuteAsync(eventId, "Event cancelled by organizer");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Metadata.Should().ContainKey("event_id");
        capturedRequest.Metadata.Should().ContainKey("event_title");
        capturedRequest.Metadata.Should().ContainKey("refund_type");
        capturedRequest.Metadata!["refund_type"].Should().Be("auto_refund_event_cancelled");
    }

    #endregion
}
