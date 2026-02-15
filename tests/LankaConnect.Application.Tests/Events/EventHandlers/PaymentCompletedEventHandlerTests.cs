using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.EventHandlers;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.EventHandlers;

/// <summary>
/// Phase 6A.X Issue #29: Unit tests for PaymentCompletedEventHandler.
/// Tests focus on ContactEmail validation and recovery for guest users.
/// Phase 6A.100: Updated to use ITypedEmailService.
/// </summary>
public class PaymentCompletedEventHandlerTests
{
    private readonly Mock<ITypedEmailService> _typedEmailService;
    private readonly Mock<ITicketService> _ticketService;
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IEventRepository> _eventRepository;
    private readonly Mock<IRegistrationRepository> _registrationRepository;
    private readonly Mock<IEventFormRepository> _eventFormRepository;
    private readonly Mock<IEmailUrlHelper> _emailUrlHelper;
    private readonly Mock<ILogger<PaymentCompletedEventHandler>> _logger;
    private readonly PaymentCompletedEventHandler _handler;

    public PaymentCompletedEventHandlerTests()
    {
        _typedEmailService = new Mock<ITypedEmailService>();
        _ticketService = new Mock<ITicketService>();
        _userRepository = new Mock<IUserRepository>();
        _eventRepository = new Mock<IEventRepository>();
        _registrationRepository = new Mock<IRegistrationRepository>();
        _eventFormRepository = new Mock<IEventFormRepository>();
        _emailUrlHelper = new Mock<IEmailUrlHelper>();
        _logger = new Mock<ILogger<PaymentCompletedEventHandler>>();

        // Phase 6A.112: Setup event form repository to return empty list by default
        _eventFormRepository.Setup(x => x.GetByEventIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LankaConnect.Domain.Events.Entities.EventForm>());

        // Setup default URL helper behavior
        _emailUrlHelper.Setup(x => x.BuildEventDetailsUrl(It.IsAny<Guid>()))
            .Returns((Guid id) => $"https://lankaconnect.com/events/{id}");
        _emailUrlHelper.Setup(x => x.BuildTicketViewUrl(It.IsAny<Guid>()))
            .Returns((Guid id) => $"https://lankaconnect.com/tickets/{id}");

        // Setup default typed email service success
        _typedEmailService.Setup(x => x.SendEmailAsync(
                It.IsAny<IEmailParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Ok(Guid.NewGuid().ToString(), 100));

        _typedEmailService.Setup(x => x.SendEmailWithAttachmentsAsync(
                It.IsAny<IEmailParameters>(),
                It.IsAny<List<EmailAttachmentDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Ok(Guid.NewGuid().ToString(), 100));

        // Setup default ticket generation success
        _ticketService.Setup(x => x.GenerateTicketAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TicketResult>.Success(new TicketResult
            {
                TicketId = Guid.NewGuid(),
                TicketCode = "TEST-TICKET-001",
                QrCodeData = "TEST-QR-DATA"
            }));

        _ticketService.Setup(x => x.GetTicketPdfAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<byte[]>.Success(new byte[] { 1, 2, 3 }));

        _handler = new PaymentCompletedEventHandler(
            _typedEmailService.Object,
            _ticketService.Object,
            _userRepository.Object,
            _eventRepository.Object,
            _registrationRepository.Object,
            _eventFormRepository.Object,
            _emailUrlHelper.Object,
            _logger.Object);
    }

    #region Issue #29: Guest User Email Validation Tests

    [Fact]
    public async Task Handle_GuestUser_WithValidContactEmail_ShouldSendEmail()
    {
        // Arrange - Issue #29: Guest user with valid ContactEmail in domain event
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var guestEmail = "guest@example.com";

        var domainEvent = CreatePaymentCompletedEvent(
            eventId, registrationId,
            userId: null,
            contactEmail: guestEmail);
        var notification = new DomainEventNotification<PaymentCompletedEvent>(domainEvent);

        var mockEvent = CreateMockEventWithRegistration(eventId, registrationId, guestEmail);

        _eventRepository.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEvent);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert - Email should be sent to guest email
        _typedEmailService.Verify(x => x.SendEmailWithAttachmentsAsync(
            It.Is<IEmailParameters>(p => p.RecipientEmail == guestEmail),
            It.IsAny<List<EmailAttachmentDto>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GuestUser_WithEmptyContactEmail_AndContactPopulated_ShouldRecoverEmail()
    {
        // Arrange - Issue #29: ContactEmail is empty in event but Contact has valid email
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var recoveryEmail = "recovery@example.com";

        var domainEvent = CreatePaymentCompletedEvent(
            eventId, registrationId,
            userId: null,
            contactEmail: string.Empty); // Empty email in event
        var notification = new DomainEventNotification<PaymentCompletedEvent>(domainEvent);

        var mockEvent = CreateMockEventWithRegistration(eventId, registrationId, recoveryEmail);

        _eventRepository.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEvent);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert - Email should be recovered from registration.Contact and email sent
        _typedEmailService.Verify(x => x.SendEmailWithAttachmentsAsync(
            It.Is<IEmailParameters>(p => p.RecipientEmail == recoveryEmail),
            It.IsAny<List<EmailAttachmentDto>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GuestUser_WithEmptyContactEmail_AndNoContact_ShouldNotSendEmail()
    {
        // Arrange - Issue #29: ContactEmail is empty AND no Contact on registration
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();

        var domainEvent = CreatePaymentCompletedEvent(
            eventId, registrationId,
            userId: null,
            contactEmail: string.Empty); // Empty email in event
        var notification = new DomainEventNotification<PaymentCompletedEvent>(domainEvent);

        // Create event with registration that has NO Contact info
        var mockEvent = CreateMockEventWithRegistrationNoContact(eventId, registrationId);

        _eventRepository.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEvent);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert - Email should NOT be sent
        _typedEmailService.Verify(x => x.SendEmailAsync(
            It.IsAny<IEmailParameters>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _typedEmailService.Verify(x => x.SendEmailWithAttachmentsAsync(
            It.IsAny<IEmailParameters>(),
            It.IsAny<List<EmailAttachmentDto>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_GuestUser_WithWhitespaceContactEmail_ShouldRecoverOrSkip()
    {
        // Arrange - Issue #29: ContactEmail is whitespace only
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();

        var domainEvent = CreatePaymentCompletedEvent(
            eventId, registrationId,
            userId: null,
            contactEmail: "   "); // Whitespace only
        var notification = new DomainEventNotification<PaymentCompletedEvent>(domainEvent);

        // Create event with registration that has NO Contact info
        var mockEvent = CreateMockEventWithRegistrationNoContact(eventId, registrationId);

        _eventRepository.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEvent);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert - Email should NOT be sent (whitespace treated as empty)
        _typedEmailService.Verify(x => x.SendEmailAsync(
            It.IsAny<IEmailParameters>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _typedEmailService.Verify(x => x.SendEmailWithAttachmentsAsync(
            It.IsAny<IEmailParameters>(),
            It.IsAny<List<EmailAttachmentDto>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Member User Tests

    [Fact]
    public async Task Handle_MemberUser_WithValidUserId_ShouldUseUserEmail()
    {
        // Arrange - Member user (has UserId)
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var memberEmail = "member@example.com";
        var contactEmail = "contact@example.com"; // Different from member email

        var domainEvent = CreatePaymentCompletedEvent(
            eventId, registrationId,
            userId: userId,
            contactEmail: contactEmail);
        var notification = new DomainEventNotification<PaymentCompletedEvent>(domainEvent);

        var mockEvent = CreateMockEventWithRegistration(eventId, registrationId, contactEmail);
        var mockUser = CreateTestUser(userId, memberEmail, "John", "Doe");

        _eventRepository.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEvent);
        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert - Email should be sent to member's email (from user record), not contactEmail
        _typedEmailService.Verify(x => x.SendEmailWithAttachmentsAsync(
            It.Is<IEmailParameters>(p => p.RecipientEmail == memberEmail),
            It.IsAny<List<EmailAttachmentDto>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Fail-Silent Pattern Tests

    [Fact]
    public async Task Handle_EmailServiceFailure_ShouldNotThrow()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var guestEmail = "guest@example.com";

        var domainEvent = CreatePaymentCompletedEvent(
            eventId, registrationId,
            userId: null,
            contactEmail: guestEmail);
        var notification = new DomainEventNotification<PaymentCompletedEvent>(domainEvent);

        var mockEvent = CreateMockEventWithRegistration(eventId, registrationId, guestEmail);

        _eventRepository.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEvent);
        _typedEmailService.Setup(x => x.SendEmailWithAttachmentsAsync(
                It.IsAny<IEmailParameters>(),
                It.IsAny<List<EmailAttachmentDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Fail(Guid.NewGuid().ToString(), new List<string> { "Email service error" }));

        // Act - Should not throw (fail-silent pattern)
        var act = async () => await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_EventNotFound_ShouldNotThrow()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();

        var domainEvent = CreatePaymentCompletedEvent(
            eventId, registrationId,
            userId: null,
            contactEmail: "guest@example.com");
        var notification = new DomainEventNotification<PaymentCompletedEvent>(domainEvent);

        _eventRepository.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act - Should not throw (fail-silent pattern)
        var act = async () => await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        _typedEmailService.Verify(x => x.SendEmailAsync(
            It.IsAny<IEmailParameters>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _typedEmailService.Verify(x => x.SendEmailWithAttachmentsAsync(
            It.IsAny<IEmailParameters>(),
            It.IsAny<List<EmailAttachmentDto>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RegistrationNotFoundInEvent_ShouldNotThrow()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var wrongRegistrationId = Guid.NewGuid(); // Different ID

        var domainEvent = CreatePaymentCompletedEvent(
            eventId, registrationId,
            userId: null,
            contactEmail: "guest@example.com");
        var notification = new DomainEventNotification<PaymentCompletedEvent>(domainEvent);

        // Create event with different registration ID
        var mockEvent = CreateMockEventWithRegistration(eventId, wrongRegistrationId, "other@example.com");

        _eventRepository.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEvent);

        // Act - Should not throw (fail-silent pattern)
        var act = async () => await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        _typedEmailService.Verify(x => x.SendEmailAsync(
            It.IsAny<IEmailParameters>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _typedEmailService.Verify(x => x.SendEmailWithAttachmentsAsync(
            It.IsAny<IEmailParameters>(),
            It.IsAny<List<EmailAttachmentDto>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Helper Methods

    private static PaymentCompletedEvent CreatePaymentCompletedEvent(
        Guid eventId,
        Guid registrationId,
        Guid? userId,
        string contactEmail)
    {
        return new PaymentCompletedEvent(
            eventId,
            registrationId,
            userId,
            contactEmail,
            "pi_test_payment_intent",
            50.00m,
            2,
            DateTime.UtcNow);
    }

    private static Event CreateMockEventWithRegistration(Guid eventId, Guid registrationId, string contactEmail)
    {
        var eventObj = Event.Create(
            EventTitle.Create("Test Paid Event").Value,
            EventDescription.Create("Test Description").Value,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(7).AddHours(2),
            Guid.NewGuid(), // organizerId
            100, // capacity
            null, // location
            EventCategory.Cultural).Value;

        // Set the Event Id using reflection
        var eventIdProperty = typeof(BaseEntity).GetProperty("Id");
        eventIdProperty?.SetValue(eventObj, eventId);

        // Mark as free event so registration doesn't require pricing config
        eventObj.SetAsFreeEvent();

        // Publish the event so we can register attendees
        eventObj.Publish();

        // Create a registration with Contact info
        var contact = RegistrationContact.Create(contactEmail, "555-1234", null).Value;
        var attendee = AttendeeDetails.Create("Guest User", AgeCategory.Adult, Gender.Male).Value;

        var result = eventObj.RegisterWithAttendees(null, new List<AttendeeDetails> { attendee }, contact);
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Failed to register attendee: {result.Error}");
        }

        // Set the Registration Id using reflection
        var registration = eventObj.Registrations.First();
        var registrationIdProperty = typeof(BaseEntity).GetProperty("Id");
        registrationIdProperty?.SetValue(registration, registrationId);

        return eventObj;
    }

    private static Event CreateMockEventWithRegistrationNoContact(Guid eventId, Guid registrationId)
    {
        var eventObj = Event.Create(
            EventTitle.Create("Test Paid Event").Value,
            EventDescription.Create("Test Description").Value,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(7).AddHours(2),
            Guid.NewGuid(), // organizerId
            100, // capacity
            null, // location
            EventCategory.Cultural).Value;

        // Set the Event Id using reflection
        var eventIdProperty = typeof(BaseEntity).GetProperty("Id");
        eventIdProperty?.SetValue(eventObj, eventId);

        // Mark as free event
        eventObj.SetAsFreeEvent();

        // Publish the event
        eventObj.Publish();

        // Use reflection to add a registration WITHOUT Contact info (edge case)
        // This simulates corrupted/legacy data where Contact is null
        var registrationType = typeof(Registration);
        var constructor = registrationType.GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, new Type[] { }, null);

        if (constructor != null)
        {
            var registration = (Registration)constructor.Invoke(null);

            // Set properties via reflection
            SetPrivateField(registration, nameof(Registration.EventId), eventId);
            SetPrivateProperty(registration, "Id", registrationId);
            SetPrivateField(registration, nameof(Registration.Status), RegistrationStatus.Confirmed);
            SetPrivateField(registration, nameof(Registration.PaymentStatus), PaymentStatus.Completed);
            // Contact is left null intentionally - this is the edge case we're testing

            // Add to event's registrations via reflection
            var registrationsField = typeof(Event).GetField("_registrations",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (registrationsField != null)
            {
                var registrations = (List<Registration>)registrationsField.GetValue(eventObj)!;
                registrations.Add(registration);
            }
        }

        return eventObj;
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetProperty(fieldName);
        field?.SetValue(obj, value);
    }

    private static void SetPrivateProperty(object obj, string propertyName, object value)
    {
        var property = typeof(BaseEntity).GetProperty(propertyName);
        property?.SetValue(obj, value);
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

    #endregion
}
